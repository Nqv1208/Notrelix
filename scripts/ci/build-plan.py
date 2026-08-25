#!/usr/bin/env python3
from __future__ import annotations

import argparse
import hashlib
import json
import os
from pathlib import Path
from typing import Any

from delivery_model import (
    ROOT, bound_proof_profile, changed_files_for_event, compact_json, component, consumers_of_package,
    deployable_components, discover_workspace_packages, frontend_components,
    github_output, load_catalog, load_policy, matches, matrix, norm_path,
    package_for_path, resolve_proof_profile,
)


def parser() -> argparse.ArgumentParser:
    p = argparse.ArgumentParser(description="Build the canonical Notrelix CI execution plan")
    p.add_argument("--event-name", default=os.environ.get("EVENT_NAME", "workflow_dispatch"))
    p.add_argument("--base-sha", default=os.environ.get("BASE_SHA", ""))
    p.add_argument("--head-sha", default=os.environ.get("HEAD_SHA", ""))
    p.add_argument("--before-sha", default=os.environ.get("BEFORE_SHA", ""))
    p.add_argument("--ref", default=os.environ.get("GITHUB_REF", ""))
    p.add_argument("--repo-root", type=Path, default=ROOT)
    p.add_argument("--output", type=Path, default=Path("artifacts/ci/execution-plan.json"))
    p.add_argument("--changed-file", action="append", default=[])
    p.add_argument("--full", action="store_true")
    return p


def add_components(target: set[str], values: list[str] | tuple[str, ...] | None, catalog: dict[str, Any]) -> None:
    for cid in values or []:
        component(catalog, cid)  # validate
        target.add(cid)


def main() -> int:
    args = parser().parse_args()
    root = args.repo_root.resolve()
    catalog = load_catalog(root)
    policy = load_policy(root)
    components_cfg = catalog.get("components", {})
    all_components = set(components_cfg)
    deployables = set(deployable_components(catalog))
    all_frontend = set(frontend_components(catalog))
    all_frontend_deployables = {cid for cid in all_frontend if cid in deployables}

    if args.changed_file:
        changed = sorted({norm_path(p) for p in args.changed_file})
        force_full = args.full
        reason = "explicit changed-file test input"
    elif args.full:
        changed, force_full, reason = [], True, "explicit --full"
    else:
        changed, force_full, reason = changed_files_for_event(
            args.event_name, args.base_sha, args.head_sha, args.before_sha, root
        )

    packages = discover_workspace_packages(root)
    affected: set[str] = set()
    package_targets: set[str] = set()
    planes: set[str] = set()
    capabilities: set[str] = set()
    security_domains: set[str] = set()
    warnings: list[str] = []
    release_required = False
    full_ci = force_full
    full_frontend = False
    matched_rules: dict[str, list[str]] = {}
    runtime_changed_components: set[str] = set()

    rules = policy.get("change_rules", [])
    for path in changed:
        exclusive = False
        matched_any = False
        path_rule_ids: list[str] = []
        for rule in rules:
            if not any(matches(path, pattern) for pattern in rule.get("patterns", [])):
                continue
            matched_any = True
            path_rule_ids.append(rule.get("id", "unnamed"))
            add_components(affected, rule.get("components"), catalog)
            add_components(package_targets, rule.get("package_components"), catalog)
            planes.update(rule.get("planes", []))
            capabilities.update(rule.get("capabilities", []))
            security_domains.update(rule.get("security_domains", []))
            if rule.get("full_ci"):
                full_ci = True
            if rule.get("full_frontend"):
                full_frontend = True
            if rule.get("package_all_deployables"):
                package_targets.update(deployables)
            if rule.get("package_all_frontend_deployables"):
                package_targets.update(all_frontend_deployables)
            if rule.get("release") is True:
                release_required = True
            if rule.get("exclusive"):
                exclusive = True
        if path_rule_ids:
            matched_rules[path] = path_rule_ids
        if exclusive:
            continue

        # Direct component roots.
        direct: set[str] = set()
        for cid, cfg in components_cfg.items():
            if any(matches(path, pattern) for pattern in cfg.get("roots", [])):
                direct.add(cid)
        if direct:
            affected.update(direct)
            for cid in direct:
                cfg = components_cfg[cid]
                domain = cfg.get("security_domain")
                if domain:
                    security_domains.add(domain)
                if cfg.get("deployable"):
                    package_targets.add(cid)
                    runtime_changed_components.add(cid)
                    if cfg.get("release_on_runtime_change", True):
                        release_required = True
            continue

        # Dependency-aware frontend package routing.
        if path.startswith("frontend/"):
            pkg = package_for_path(path, packages)
            if pkg:
                consumers = consumers_of_package(pkg.name, catalog, packages)
                if consumers:
                    affected.update(consumers)
                    for cid in consumers:
                        cfg = components_cfg[cid]
                        domain = cfg.get("security_domain")
                        if domain:
                            security_domains.add(domain)
                        if cfg.get("deployable"):
                            package_targets.add(cid)
                            runtime_changed_components.add(cid)
                            release_required = release_required or cfg.get("release_on_runtime_change", True)
                else:
                    warnings.append(f"workspace {pkg.name} changed but has no registered consumer; full frontend fail-safe")
                    full_frontend = True
            elif not matched_any:
                warnings.append(f"unclassified frontend path {path}; full frontend fail-safe")
                full_frontend = True
            continue

        if path.startswith("backend/") and not matched_any:
            backend_ids = {cid for cid, cfg in components_cfg.items() if cfg.get("provider") == "backend"}
            if backend_ids:
                affected.update(backend_ids)
                package_targets.update(cid for cid in backend_ids if cid in deployables)
                security_domains.add("backend")
                release_required = True
            else:
                full_ci = True
            continue

        if not matched_any:
            # Unknown repository topology is a correctness risk. Full CI is an
            # intentional conservative fallback until the catalog is updated.
            warnings.append(f"unclassified repository path {path}; full CI fail-safe")
            full_ci = True

    if full_ci:
        affected.update(all_components)
        package_targets.update(deployables)
        planes.update({"docs", "infra"})
        security_domains.update({"backend", "frontend"})
        capabilities.update({"node-tests", "web-tests", "mobile-tests", "tooling-tests", "ui", "mock", "contract"})
    elif full_frontend:
        affected.update(all_frontend)
        package_targets.update(all_frontend_deployables)
        security_domains.add("frontend")
        capabilities.update({"node-tests", "web-tests", "mobile-tests", "tooling-tests", "ui", "mock", "contract"})

    if any(components_cfg[cid].get("provider") in {"frontend-host", "mobile"} for cid in affected):
        capabilities.update(policy.get("defaults", {}).get("frontend_default_capabilities", []))
        security_domains.add("frontend")

    # Capability fanout declared by affected components.
    for cid in affected:
        cfg = components_cfg[cid]
        capabilities.update(cfg.get("frontend", {}).get("capabilities", []))

    # Main releases are coherent snapshots: once any deployable/runtime topology
    # change requires release, build all deployables exactly once in the CI run.
    release_branch = catalog.get("repository", {}).get("release_branch", "main")
    is_main_push = args.event_name == "push" and args.ref == f"refs/heads/{release_branch}"
    release_candidate = bool(is_main_push and release_required)
    if release_candidate:
        package_targets = set(deployables)
        # Release-candidate packaging expands to the complete deployable set.
        # Concrete proof IDs remain owned by policy profiles.
        security_domains.update({"backend", "frontend"})

    backend_ids = sorted(cid for cid in affected if components_cfg[cid].get("provider") == "backend")
    host_ids = sorted(cid for cid in affected if components_cfg[cid].get("provider") == "frontend-host")
    mobile_ids = sorted(cid for cid in affected if components_cfg[cid].get("provider") == "mobile")
    frontend_required = bool(host_ids or mobile_ids or capabilities & {"node-tests", "web-tests", "mobile-tests", "tooling-tests", "ui", "mock", "contract"})

    # Turbo filters use registered host workspaces. Reverse-dependency selection is
    # delegated to Turbo from those hosts, keeping static analysis on affected closures.
    frontend_filters = sorted({components_cfg[cid]["workspace"] for cid in host_ids + mobile_ids if components_cfg[cid].get("workspace")})
    if "tooling-tests" in capabilities:
        frontend_filters.append("./tooling/**")

    # Proof IDs are resolved only from policy profiles. The planner decides what
    # needs proof; policy.toml owns the concrete proof vocabulary.
    expected: list[str] = []
    for cid in sorted(affected):
        profile_name = str(components_cfg[cid].get("proof_profile", ""))
        if not profile_name:
            raise SystemExit(f"component {cid} has no proof_profile")
        expected.extend(resolve_proof_profile(policy, profile_name, component_id=cid))
    for plane in sorted(planes):
        profile_name = bound_proof_profile(policy, "planes", plane)
        expected.extend(resolve_proof_profile(policy, profile_name))
    for domain in sorted(security_domains):
        profile_name = bound_proof_profile(policy, "security_domains", domain)
        expected.extend(resolve_proof_profile(policy, profile_name))
    packaging_profile = str(policy.get("proof_bindings", {}).get("packaging", {}).get("profile", ""))
    if package_targets and not packaging_profile:
        raise SystemExit("missing proof binding: packaging.profile")
    for cid in sorted(package_targets):
        expected.extend(resolve_proof_profile(policy, packaging_profile, component_id=cid))
    if release_candidate:
        release_profile = str(policy.get("proof_bindings", {}).get("release", {}).get("profile", ""))
        if not release_profile:
            raise SystemExit("missing proof binding: release.profile")
        expected.extend(resolve_proof_profile(policy, release_profile))
    expected = sorted(set(expected))

    deployment = policy.get("deployment", {})
    migration_paths = deployment.get("migration_paths", [])
    schema_change = any(
        any(matches(path, pattern) for pattern in migration_paths)
        for path in changed
    )
    rollback_after_schema_change = str(deployment.get("rollback_after_schema_change", ""))

    plan: dict[str, Any] = {
        "schema_version": 4,
        "event": args.event_name,
        "ref": args.ref,
        "reason": reason,
        "changed_files": changed,
        "full_ci": full_ci,
        "full_frontend": full_frontend,
        "affected_components": sorted(affected),
        "runtime_changed_components": sorted(runtime_changed_components),
        "package_components": sorted(package_targets),
        "planes": sorted(planes),
        "capabilities": sorted(capabilities),
        "security_domains": sorted(security_domains),
        "release_required": release_required,
        "release_candidate": release_candidate,
        "schema_change": schema_change,
        "rollback_after_schema_change": rollback_after_schema_change,
        "expected_proofs": expected,
        "matched_rules": matched_rules,
        "warnings": warnings,
        "matrices": {
            "backend": [{"component_id": cid} for cid in backend_ids],
            "frontend_hosts": [{"component_id": cid} for cid in host_ids],
            "mobile": [{"component_id": cid} for cid in mobile_ids],
            "containers": [{"component_id": cid} for cid in sorted(package_targets)],
        },
        "frontend_filters": frontend_filters,
    }
    canonical = compact_json(plan)
    plan["plan_sha256"] = hashlib.sha256(canonical.encode("utf-8")).hexdigest()

    out = args.output if args.output.is_absolute() else root / args.output
    out.parent.mkdir(parents=True, exist_ok=True)
    out.write_text(json.dumps(plan, indent=2, sort_keys=True) + "\n", encoding="utf-8")

    outputs = {
        "backend_matrix": matrix(plan["matrices"]["backend"]),
        "frontend_host_matrix": matrix(plan["matrices"]["frontend_hosts"]),
        "mobile_matrix": matrix(plan["matrices"]["mobile"]),
        "container_matrix": matrix(plan["matrices"]["containers"]),
        "backend_count": str(len(backend_ids)),
        "frontend_required": str(frontend_required).lower(),
        "host_count": str(len(host_ids)),
        "mobile_count": str(len(mobile_ids)),
        "container_count": str(len(package_targets)),
        "frontend_filters_json": compact_json(frontend_filters),
        "frontend_capabilities_json": compact_json(sorted(capabilities)),
        "mock_artifact_component": str(policy.get("mock", {}).get("artifact_component", "")),
        "docs_required": str("docs" in planes).lower(),
        "infra_required": str("infra" in planes).lower(),
        "security_backend": str("backend" in security_domains).lower(),
        "security_frontend": str("frontend" in security_domains).lower(),
        "packaging_required": str(bool(package_targets)).lower(),
        "release_required": str(release_required).lower(),
        "release_candidate": str(release_candidate).lower(),
        "schema_change": str(schema_change).lower(),
        "rollback_after_schema_change": rollback_after_schema_change,
        "expected_proofs_json": compact_json(expected),
        "plan_sha256": plan["plan_sha256"],
        "any": str(bool(expected or changed or full_ci)).lower(),
    }
    for key, value in outputs.items():
        github_output(key, value)

    print(json.dumps(plan, indent=2, sort_keys=True))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
