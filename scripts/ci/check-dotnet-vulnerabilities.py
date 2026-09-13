#!/usr/bin/env python3
"""Structured backend vulnerability checker for captured dotnet output.

Judges the captured text of `dotnet list package --vulnerable
--include-transitive`. The command exits 0 even when vulnerable packages are
reported, so the checker decides from the structured report content: a
vulnerability block fails the check with the affected project/package pairs,
a missing/unreadable report fails closed, and a clean report passes. The
caller is responsible for failing the shell step when dotnet itself fails.
"""
from __future__ import annotations

import argparse
import json
import re
import sys

PROJECT_RE = re.compile(r"Project\s+`?([^`\n']+?)`?\s+has the following vulnerabilit", re.IGNORECASE)
PACKAGE_RE = re.compile(r"^\s*>\s+(\S+)\s+(\S+)\s*$", re.MULTILINE)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--report", required=True, help="captured dotnet list package --vulnerable output")
    parser.add_argument("--output", help="optional path for the JSON summary")
    args = parser.parse_args()

    try:
        text = open(args.report, encoding="utf-8").read()
    except OSError as error:
        print(f"::error::[check-dotnet-vulnerabilities] cannot read report: {error}", file=sys.stderr)
        print(json.dumps({"ok": False, "reason": "unreadable report"}))
        return 1

    projects = PROJECT_RE.findall(text)
    packages = [f"{name} {version}" for name, version in PACKAGE_RE.findall(text)]

    if projects:
        for project in projects:
            print(f"::error::[check-dotnet-vulnerabilities] vulnerable packages in project {project}", file=sys.stderr)
        if packages:
            for package in packages:
                print(f"::error::[check-dotnet-vulnerabilities] vulnerable package: {package}", file=sys.stderr)
        summary = {"ok": False, "vulnerable_projects": sorted(set(projects)), "packages": packages}
        if args.output:
            open(args.output, "w", encoding="utf-8").write(json.dumps(summary, indent=2, sort_keys=True) + "\n")
        print(json.dumps(summary, sort_keys=True))
        return 1

    summary = {"ok": True, "vulnerable_projects": [], "packages": []}
    if args.output:
        open(args.output, "w", encoding="utf-8").write(json.dumps(summary, indent=2, sort_keys=True) + "\n")
    print(json.dumps(summary, sort_keys=True))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
