#!/usr/bin/env python3
"""Validate reusable-workflow permission chains for the Notrelix repository.

Discovers every local reusable workflow call (uses: ./.github/workflows/*.yml),
computes the caller's effective permission map and the callee's required
permission ceiling, and rejects any edge where the caller grants fewer
permissions than the callee requires.

Exit code 0 = all edges valid, 1 = at least one violation.
"""
from __future__ import annotations
import re, sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
WORKFLOWS = ROOT / ".github/workflows"
LOCAL_USES_RE = re.compile(r"^\s*uses:\s*\./\.github/workflows/([^\s#]+\.yml)", re.MULTILINE)
VALID_SCOPES = {"contents", "actions", "packages", "id-token", "attestations",
                "artifact-metadata", "pull-requests", "checks", "statuses", "security-events"}
LEVELS = {"none": 0, "read": 1, "write": 2}
SCALAR_SHORTCUTS = {"read-all", "write-all"}


def _extract_permissions_section(text: str, key_pos: int) -> dict[str, str]:
    """Extract key-value pairs from a permissions block starting after the colon."""
    result: dict[str, str] = {}
    after = text[key_pos:]
    first_line = after.split("\n", 1)[0] if after else ""
    # Inline form on the same line: {contents: read, packages: read}
    m = re.search(r"\{([^}]+)\}", first_line)
    if m:
        for pair in m.group(1).split(","):
            pair = pair.strip()
            if ":" in pair:
                k, v = pair.split(":", 1)
                result[k.strip()] = v.strip()
        return result
    # Scalar on the same line: read-all / write-all
    m_scalar = re.match(r"\s*(read-all|write-all)\s*$", first_line)
    if m_scalar:
        result["__scalar__"] = m_scalar.group(1)
        return result
    # Block form
    for line in after.splitlines()[1:]:
        stripped = line.strip()
        if not stripped or stripped.startswith("#"):
            continue
        m = re.match(r"^(\w[\w-]*)\s*:\s*(.+)$", stripped)
        if m:
            k, v = m.group(1), m.group(2).strip()
            if k in VALID_SCOPES:
                result[k] = v
            else:
                break
        else:
            break
    return result


def _parse_permissions(text: str) -> dict[str, str]:
    """Find the top-level 'permissions:' key and parse its value."""
    m = re.search(r"^permissions\s*:", text, re.MULTILINE)
    if not m:
        return {}
    return _extract_permissions_section(text, m.end())


def _parse_job_permissions(text: str, job_name: str) -> dict[str, str]:
    """Find permissions block inside a specific job."""
    # Find "  job_name:" (indented under jobs:)
    pattern = re.compile(rf"^  {re.escape(job_name)}\s*:", re.MULTILINE)
    m = pattern.search(text)
    if not m:
        # Also try unindented
        pattern2 = re.compile(rf"^{re.escape(job_name)}\s*:", re.MULTILINE)
        m = pattern2.search(text)
    if not m:
        return {}
    # Search for 'permissions:' within the job block
    pos = m.end()
    # Scan forward until we hit the next top-level key or end
    lines = text[pos:].split("\n")
    for i, line in enumerate(lines):
        # Top-level key detection (no leading whitespace)
        if i > 0 and re.match(r"^\S", line) and line.strip():
            break
        perm_m = re.search(r"permissions\s*:", line)
        if perm_m:
            perm_pos = pos + sum(len(l) + 1 for l in lines[:i]) + perm_m.end()
            return _extract_permissions_section(text, perm_pos)
    return {}


def _find_local_reusable_calls(text: str) -> list[tuple[str, str]]:
    """Return list of (job_name, callee_workflow) for local reusable calls."""
    results = []
    current_job: str | None = None
    in_jobs = False
    for line in text.splitlines():
        if re.match(r"^jobs\s*:", line):
            in_jobs = True
            continue
        if in_jobs:
            # Job names are at indent 2 (directly under jobs:)
            m_job = re.match(r"^  (\w[\w-]*)\s*:", line)
            if m_job:
                current_job = m_job.group(1)
            m_uses = LOCAL_USES_RE.search(line)
            if m_uses and current_job:
                results.append((current_job, m_uses.group(1)))
    return results


def _callee_required_permissions(callee_text: str) -> dict[str, str]:
    """Compute the callee's permission ceiling from workflow-level + all job-level."""
    wf = _parse_permissions(callee_text)
    if "__scalar__" in wf:
        return wf  # scalar shortcuts propagate
    ceiling: dict[str, str] = dict(wf)
    # Find job names (indented under 'jobs:')
    in_jobs = False
    for line in callee_text.splitlines():
        if re.match(r"^jobs\s*:", line):
            in_jobs = True
            continue
        if in_jobs:
            m = re.match(r"^\s+(\w[\w-]*)\s*:", line)
            if m:
                jp = _parse_job_permissions(callee_text, m.group(1))
                for scope, level in jp.items():
                    existing = ceiling.get(scope, "none")
                    if LEVELS.get(level, 0) > LEVELS.get(existing, 0):
                        ceiling[scope] = level
    return ceiling


def _validate_value(scope: str, value: str, path: str, errors: list[str]) -> None:
    if value in SCALAR_SHORTCUTS:
        errors.append(f"{path}: scalar permission shortcut '{value}' forbidden")
    elif value not in LEVELS:
        errors.append(f"{path}: unknown permission value '{value}' for scope '{scope}'")


def validate_repository(root: Path = ROOT) -> list[str]:
    """Validate all local reusable workflow call edges in the repository."""
    errors: list[str] = []
    workflows_dir = root / ".github" / "workflows"
    if not workflows_dir.exists():
        return ["workflows directory not found"]

    workflow_files = {
        f.name: f.read_text(encoding="utf-8")
        for f in sorted(workflows_dir.glob("*.yml"))
    }

    # Validate each workflow's permissions syntax
    for name, text in workflow_files.items():
        wf = _parse_permissions(text)
        if "__scalar__" in wf:
            errors.append(f"{name}: scalar permission shortcut '{wf['__scalar__']}' forbidden")
        for scope, value in wf.items():
            if scope == "__scalar__":
                continue
            if scope not in VALID_SCOPES:
                errors.append(f"{name}: unknown permission scope '{scope}'")
            _validate_value(scope, value, name, errors)
        # Job-level permissions
        in_jobs = False
        for line in text.splitlines():
            if re.match(r"^jobs\s*:", line):
                in_jobs = True
                continue
            if in_jobs:
                m = re.match(r"^\s+(\w[\w-]*)\s*:", line)
                if m:
                    jp = _parse_job_permissions(text, m.group(1))
                    for scope, value in jp.items():
                        if scope not in VALID_SCOPES:
                            errors.append(f"{name}:{m.group(1)}: unknown permission scope '{scope}'")
                        _validate_value(scope, value, f"{name}:{m.group(1)}", errors)

    # Validate caller→callee permission chains
    for caller_name, caller_text in workflow_files.items():
        calls = _find_local_reusable_calls(caller_text)
        if not calls:
            continue
        wf = _parse_permissions(caller_text)
        for job_name, callee_name in calls:
            # Caller effective = job-level if present, else workflow-level
            caller_effective = dict(wf)
            jp = _parse_job_permissions(caller_text, job_name)
            if jp:
                caller_effective = jp
            # Load callee
            callee_path = workflows_dir / callee_name
            if not callee_path.exists():
                errors.append(f"{caller_name}:{job_name} -> {callee_name}: callee not found")
                continue
            callee_text = callee_path.read_text(encoding="utf-8")
            callee_required = _callee_required_permissions(callee_text)
            for scope, required_level in callee_required.items():
                if scope == "__scalar__":
                    errors.append(f"{caller_name}:{job_name} -> {callee_name}: callee uses scalar shortcut")
                    continue
                caller_level = caller_effective.get(scope, "none")
                if LEVELS.get(caller_level, 0) < LEVELS.get(required_level, 0):
                    errors.append(
                        f"{caller_name}:{job_name} -> {callee_name}: "
                        f"caller grants '{scope}:{caller_level}' but callee requires '{scope}:{required_level}'"
                    )

    return errors


def main() -> int:
    errors = validate_repository()
    if errors:
        for e in errors:
            print(f"FAIL: {e}", file=sys.stderr)
        return 1
    print("Reusable workflow permission validation PASS")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
