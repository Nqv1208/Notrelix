#!/usr/bin/env python3
"""Enforce append-only EF Core migration history for an explicit change range.

Consumes the resolved change range (explicit SHAs supplied by the caller; no
GitHub event interpretation happens here). New migration files may be added;
modifying, deleting or renaming an existing migration file fails. Unknown or
full ranges never silently skip: with a known schema change they hard-fail,
otherwise conservative chain validation runs at HEAD.
"""
from __future__ import annotations

import argparse
import json
import re
import subprocess
import sys

MIGRATION_PATHS = ("backend/**/Migrations/**", "backend/**/migrations/**")
MIGRATION_FILE_RE = re.compile(r"backend/.*/[Mm]igrations/.*")


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
        if not MIGRATION_FILE_RE.fullmatch(path) or path.lower().endswith(".designer.cs"):
            continue
        name = path.rsplit("/", 1)[-1]
        index = name.split("_", 1)[0]
        if index in seen:
            duplicates.append(f"{path} duplicates chain index of {seen[index]}")
        seen[index] = path
    return duplicates


def range_violations(base: str, head: str) -> tuple[list[str], list[str], list[str]]:
    args = ["git", "diff", "--name-status", base, head, "--", *MIGRATION_PATHS]
    try:
        output = sh(*args)
    except RuntimeError as error:
        raise RuntimeError(f"cannot diff migration range {base}..{head}: {error}") from error
    added: list[str] = []
    modified: list[str] = []
    deleted: list[str] = []
    for line in output.splitlines():
        if not line.strip():
            continue
        status, _, path = line.partition("\t")
        status = status.strip()
        if status.startswith("A") or status.startswith("C"):
            added.append(path)
        elif status.startswith("D"):
            deleted.append(path)
        elif status.startswith(("M", "R", "T")):
            modified.append(path)
    return added, modified, deleted


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
            print(json.dumps({"compared": False, "conservative": True, "ok": True, "added": [], "modified": [], "deleted": []}))
            return 0

        added, modified, deleted = range_violations(args.base_sha, args.head_sha)
        violations = modified + deleted
        if violations:
            for path in violations:
                print(f"::error::[migration-discipline] migration history is append-only; changed: {path}", file=sys.stderr)
            print(json.dumps({"compared": True, "ok": False, "added": added, "modified": modified, "deleted": deleted}))
            return 1
        print(json.dumps({"compared": True, "ok": True, "added": sorted(added), "modified": [], "deleted": []}))
        return 0
    except RuntimeError as error:
        print(f"::error::[migration-discipline] {error}", file=sys.stderr)
        print(json.dumps({"ok": False, "error": str(error)}))
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
