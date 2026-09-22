#!/usr/bin/env python3
"""Enforce append-only EF Core migration history for an explicit change range.

Consumes the resolved change range (explicit SHAs supplied by the caller; no
GitHub event interpretation happens here). The Migrations directory holds two
artifact kinds with different lifecycles:

- Migration history entries: timestamped <index>_<Name>.cs plus their
  <index>_<Name>.Designer.cs. These are immutable history: adding is allowed;
  modifying, renaming, copying or deleting an existing entry fails.
- ApplicationDbContextModelSnapshot.cs: EF's cumulative current model state.
  EF rewrites it whenever a migration is appended, so a snapshot modification
  or addition is allowed only when the same range also adds at least one
  migration definition; a snapshot-only edit fails. Deleting, renaming or
  type-changing the snapshot always fails.

Unknown or full ranges never silently skip: with a known schema change they
hard-fail, otherwise conservative chain validation runs at HEAD.

Dev-stage re-baseline exception: while the project has no production
database, the chain may be consolidated into the single
20260702093805_SchemaBaseline migration (see
backend/docs/operations/migrations-and-data-change.md BE-OPS-DATA-004 and
docs/delivery/migration-policy.md DEL-MIG-028). Such a range is recognized
only when the HEAD chain consists of exactly that one migration; history
rewrites and deletions are then downgraded to warnings. Removal condition:
restore strict append-only handling once a production database exists.
"""
from __future__ import annotations

import argparse
import json
import re
import subprocess
import sys
from pathlib import Path

MIGRATION_PATHS = ("backend/**/Migrations/**", "backend/**/migrations/**")
MIGRATION_FILE_RE = re.compile(r"backend/.*/[Mm]igrations/.*")
SNAPSHOT_SUFFIX = "ModelSnapshot.cs"
DESIGNER_SUFFIX = ".Designer.cs"
DEV_BASELINE_NAME = "20260702093805_SchemaBaseline.cs"


def is_snapshot(path: str) -> bool:
    return Path(path).name.endswith(SNAPSHOT_SUFFIX)


def is_designer(path: str) -> bool:
    return path.endswith(DESIGNER_SUFFIX)


def is_migration_definition(path: str) -> bool:
    return path.endswith(".cs") and not is_designer(path) and not is_snapshot(path)


def sh(*args: str) -> str:
    result = subprocess.run(args, text=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
    if result.returncode != 0:
        raise RuntimeError(result.stderr.strip() or f"command failed: {args}")
    return result.stdout


def head_chain_duplicates() -> list[str]:
    """Conservative chain validation: every migration must have a unique index.

    EF Core pairs each migration with a generated <index>_<Name>.Designer.cs
    sharing the same index; designer files are excluded from the check.
    """
    try:
        files = sh("git", "ls-files", "backend").splitlines()
    except RuntimeError as error:
        return [f"cannot enumerate migration chain: {error}"]
    seen: dict[str, str] = {}
    duplicates: list[str] = []
    for path in files:
        if not MIGRATION_FILE_RE.fullmatch(path) or is_designer(path) or is_snapshot(path):
            continue
        name = path.rsplit("/", 1)[-1]
        index = name.split("_", 1)[0]
        if index in seen:
            duplicates.append(f"{path} duplicates chain index of {seen[index]}")
        seen[index] = path
    return duplicates


def head_chain_migration_names() -> list[str]:
    """Migration definition file names tracked in the working tree (HEAD)."""
    try:
        files = sh("git", "ls-files", "backend").splitlines()
    except RuntimeError:
        return []
    names = []
    for path in files:
        if not MIGRATION_FILE_RE.fullmatch(path):
            continue
        if is_designer(path) or is_snapshot(path) or not path.endswith(".cs"):
            continue
        names.append(path.rsplit("/", 1)[-1])
    return names


def head_is_single_dev_baseline() -> bool:
    """True when HEAD's chain is exactly the single SchemaBaseline migration.

    This is the dev-stage re-baseline condition: while the project has no
    production database the whole chain may be consolidated into this one
    migration, so folding later entries into it (deleting them, rewriting the
    baseline and its designer) is a governed exception, not an append-only
    violation. Once any real migration is appended after the baseline the
    head chain is no longer a single entry and strict append-only history is
    enforced again.
    """
    return head_chain_migration_names() == [DEV_BASELINE_NAME]


def range_changes(base: str, head: str) -> dict[str, list[str]]:
    args = ["git", "diff", "--name-status", base, head, "--", *MIGRATION_PATHS]
    try:
        output = sh(*args)
    except RuntimeError as error:
        raise RuntimeError(f"cannot diff migration range {base}..{head}: {error}") from error
    changes: dict[str, list[str]] = {
        "added_migrations": [],
        "modified_history": [],
        "deleted_history": [],
        "snapshot_added": [],
        "snapshot_modified": [],
        "snapshot_removed": [],
    }
    for line in output.splitlines():
        if not line.strip():
            continue
        parts = line.split("\t")
        status = parts[0].strip()
        if status.startswith(("R", "C")):
            old_path, new_path = parts[1], parts[2]
        else:
            old_path = new_path = parts[1]
        if status.startswith("D"):
            bucket = "snapshot_removed" if is_snapshot(old_path) else "deleted_history"
            changes[bucket].append(old_path)
        elif status.startswith("R"):
            # A rename rewrites the history location (or relocates the snapshot);
            # treat either side as a removal+addition pair of the same artifact.
            if is_snapshot(old_path) or is_snapshot(new_path):
                changes["snapshot_removed"].append(f"{old_path} -> {new_path}")
            else:
                changes["modified_history"].append(f"{old_path} -> {new_path}")
        elif status.startswith("C"):
            # Copying a history entry duplicates it (chain check would also flag
            # the index); copying the snapshot is not an EF append workflow.
            if is_snapshot(old_path) or is_snapshot(new_path):
                changes["snapshot_removed"].append(f"{old_path} -> {new_path}")
            else:
                changes["modified_history"].append(f"{old_path} -> {new_path}")
        elif status.startswith("T"):
            bucket = "snapshot_removed" if is_snapshot(old_path) else "modified_history"
            changes[bucket].append(old_path)
        elif status.startswith("A"):
            bucket = "snapshot_added" if is_snapshot(new_path) else "added_migrations"
            changes[bucket].append(new_path)
        else:
            bucket = "snapshot_modified" if is_snapshot(old_path) else "modified_history"
            changes[bucket].append(old_path)
    return changes


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--base-sha", default="")
    parser.add_argument("--head-sha", default="HEAD")
    parser.add_argument("--full-range", action="store_true", help="range is unknown/full; compare impossible")
    parser.add_argument("--schema-change", action="store_true", help="plan reports a schema change in this range")
    args = parser.parse_args()

    try:
        duplicates = head_chain_duplicates()
        if duplicates:
            for duplicate in duplicates:
                print(f"::error::[migration-discipline] {duplicate}", file=sys.stderr)
            print(json.dumps({"compared": False, "conservative": True, "ok": False, "duplicates": duplicates}))
            return 1

        if args.full_range or not args.base_sha:
            if args.schema_change:
                print(
                    "::error::[migration-discipline] schema change present but change range is unknown; "
                    "cannot verify append-only migration discipline",
                    file=sys.stderr,
                )
                print(json.dumps({"compared": False, "conservative": False, "ok": False, "reason": "unknown range with schema change"}))
                return 1
            print(
                "::warning::[migration-discipline] unknown/full change range; conservative chain validation only",
                file=sys.stderr,
            )
            print(json.dumps({"compared": False, "conservative": True, "ok": True, "added_migrations": [], "snapshot_modified": [], "violations": []}))
            return 0

        changes = range_changes(args.base_sha, args.head_sha)
        violations: list[str] = []
        warnings: list[str] = []
        dev_rebaseline = head_is_single_dev_baseline()
        for path in changes["modified_history"]:
            if dev_rebaseline:
                warnings.append(f"dev-stage rebaseline rewrites the single-baseline history: {path}")
                continue
            violations.append(f"migration history is append-only; changed: {path}")
        for path in changes["deleted_history"]:
            if dev_rebaseline:
                warnings.append(f"dev-stage rebaseline consolidates history: {path}")
                continue
            violations.append(f"migration history is append-only; deleted: {path}")
        for path in changes["snapshot_removed"]:
            violations.append(f"model snapshot cannot be deleted, renamed or type-changed: {path}")
        if not changes["added_migrations"]:
            if dev_rebaseline:
                for path in changes["snapshot_modified"] + changes["snapshot_added"]:
                    warnings.append(f"dev-stage rebaseline rewrites the model snapshot: {path}")
            else:
                for path in changes["snapshot_modified"]:
                    violations.append(f"model snapshot modified without appending a migration: {path}")
                for path in changes["snapshot_added"]:
                    violations.append(f"model snapshot added without appending a migration: {path}")
        result = {
            "compared": True,
            "ok": not violations,
            "dev_rebaseline": dev_rebaseline,
            "added_migrations": sorted(changes["added_migrations"]),
            "snapshot_modified": sorted(changes["snapshot_modified"]),
            "violations": sorted(violations),
            "warnings": sorted(warnings),
        }
        if warnings:
            for warning in result["warnings"]:
                print(f"::warning::[migration-discipline] {warning}", file=sys.stderr)
        if violations:
            for violation in result["violations"]:
                print(f"::error::[migration-discipline] {violation}", file=sys.stderr)
            print(json.dumps(result))
            return 1
        print(json.dumps(result))
        return 0
    except RuntimeError as error:
        print(f"::error::[migration-discipline] {error}", file=sys.stderr)
        print(json.dumps({"ok": False, "error": str(error)}))
        return 1


if __name__ == "__main__":
    sys.exit(main())
