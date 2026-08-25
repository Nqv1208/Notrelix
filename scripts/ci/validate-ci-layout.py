#!/usr/bin/env python3
from __future__ import annotations

import re
import sys
from pathlib import Path
from delivery_model import ROOT, load_catalog

WORKFLOWS = ROOT / ".github/workflows"
SHA40 = re.compile(r"^[0-9a-f]{40}$")
USES = re.compile(r"^\s*uses:\s*([^\s#]+)", re.MULTILINE)
RUNS_ON_LATEST = re.compile(r"runs-on:\s*[^\n]*latest", re.I)
LEGACY = {"be-ci.yml", "fe-ci.yml", "fe-packaging.yml", "docs-governance.yml", "deploy-production.yml"}
REQUIRED = {
    "ci.yml", "ci-definition.yml", "backend-ci.yml", "frontend-ci.yml", "docs-ci.yml",
    "infra-ci.yml", "container-ci.yml", "stack-smoke.yml", "security-ci.yml", "codeql.yml",
    "release.yml", "deploy.yml", "promote-release.yml", "security-scheduled.yml",
}


def main() -> int:
    errors: list[str] = []
    existing = {p.name for p in WORKFLOWS.glob("*.yml")}
    missing = sorted(REQUIRED - existing)
    if missing: errors.append(f"missing required workflows: {missing}")
    present_legacy = sorted(LEGACY & existing)
    if present_legacy: errors.append(f"legacy workflows must be removed: {present_legacy}")

    for path in sorted(WORKFLOWS.glob("*.yml")):
        text = path.read_text(encoding="utf-8")
        if "permissions:" not in text:
            errors.append(f"{path.name}: explicit permissions block required")
        if RUNS_ON_LATEST.search(text):
            errors.append(f"{path.name}: mutable *-latest runner is forbidden; pin runner OS")
        for raw in USES.findall(text):
            ref = raw.strip("'\"")
            if ref.startswith("./") or ref.startswith("docker://"):
                continue
            if "@" not in ref:
                errors.append(f"{path.name}: external uses without ref: {ref}"); continue
            target, revision = ref.rsplit("@", 1)
            if not SHA40.fullmatch(revision):
                errors.append(f"{path.name}: external action/reusable workflow must pin full SHA: {ref}")

    ci = (WORKFLOWS / "ci.yml").read_text(encoding="utf-8") if (WORKFLOWS / "ci.yml").exists() else ""
    if re.search(r"\n\s+paths(?:-ignore)?:", ci):
        errors.append("ci.yml: workflow-level paths filters are forbidden on the required orchestrator")
    if "scripts/ci/build-plan.py" not in ci:
        errors.append("ci.yml: canonical execution planner not invoked")
    if "Notrelix CI Gate" not in ci:
        errors.append("ci.yml: stable final Notrelix CI Gate missing")
    # Closed-core invariant: product component IDs belong in the catalog, never the orchestrator.
    catalog = load_catalog(ROOT)
    for cid in catalog.get("components", {}):
        if re.search(rf"(?<![A-Za-z0-9_-]){re.escape(cid)}(?![A-Za-z0-9_-])", ci):
            errors.append(f"ci.yml: component id {cid!r} is hard-coded; consume provider matrices instead")


    # Single-authority invariant: V4 must not carry pre-control-plane lock/overlay files.
    for legacy_path in (ROOT / "infra/images.lock.env", ROOT / "docker-compose.release.yml"):
        if legacy_path.exists():
            errors.append(f"legacy delivery authority must be removed: {legacy_path.relative_to(ROOT)}")

    planner = ROOT / "scripts/ci/build-plan.py"
    if planner.exists():
        planner_text = planner.read_text(encoding="utf-8")
        for forbidden in (
            'backend:{', 'frontend:gate', 'docs:gate', 'infra:gate',
            'security:backend', 'security:frontend', 'container:{', 'stack:release-candidate',
        ):
            if forbidden in planner_text:
                errors.append(
                    f"build-plan.py constructs proof vocabulary {forbidden!r}; proof IDs must live only in delivery/policy.toml"
                )

    deploy = WORKFLOWS / "deploy.yml"
    if deploy.exists() and "DEPLOY_RUN_MIGRATIONS" in deploy.read_text(encoding="utf-8"):
        errors.append("deploy.yml: migration execution policy must come from environments.toml, not DEPLOY_RUN_MIGRATIONS")

    release_text = "\n".join((WORKFLOWS / name).read_text(encoding="utf-8") for name in ("release.yml", "deploy.yml", "promote-release.yml") if (WORKFLOWS / name).exists())
    if re.search(r"\bdocker\s+build\b|\bdocker\s+compose[^\n;&|]*\sbuild(?:\s|$)", release_text):
        errors.append("release/deploy workflows must never rebuild artifacts")
    for legacy_var in ("BACKEND_IMAGE", "WEB_IMAGE", "MARKETING_IMAGE"):
        if legacy_var in release_text:
            errors.append(f"release/deploy workflow hard-codes component image variable {legacy_var}; use manifest renderer")

    codeowners = ROOT / ".github/CODEOWNERS"
    if not codeowners.exists():
        errors.append(".github/CODEOWNERS missing; CI/CD control-plane changes need trusted review ownership")
    else:
        owner_text = codeowners.read_text(encoding="utf-8")
        for critical in ("/.github/workflows/", "/scripts/ci/", "/delivery/"):
            if critical not in owner_text:
                errors.append(f"CODEOWNERS missing critical path {critical}")

    if errors:
        print("CI/CD architecture validation FAILED", file=sys.stderr)
        for err in errors: print(f" - {err}", file=sys.stderr)
        return 1
    print(f"CI/CD architecture validation PASS: {len(existing)} workflows, closed-core catalog routing, immutable external action refs")
    return 0

if __name__ == "__main__":
    raise SystemExit(main())
