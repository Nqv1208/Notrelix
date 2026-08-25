#!/usr/bin/env python3
"""Shared delivery-model primitives for Notrelix CI/CD.

Keep this module stdlib-only so the change planner can run before any language
package manager is installed. Python 3.11+ is required for tomllib.
"""
from __future__ import annotations

import fnmatch
import json
import os
import re
import subprocess
import tomllib
from dataclasses import dataclass
from pathlib import Path, PurePosixPath
from typing import Any, Iterable

ROOT = Path(__file__).resolve().parents[2]
DELIVERY = ROOT / "delivery"
SAFE_ID = re.compile(r"^[a-z0-9][a-z0-9._-]*$")


PROOF_PLACEHOLDER = re.compile(r"\{([a-z_][a-z0-9_]*)\}")
ALLOWED_PROOF_PLACEHOLDERS = {"component_id"}


def proof_profile(policy: dict[str, Any], profile_name: str) -> dict[str, Any]:
    profiles = policy.get("proof_profiles", {})
    if profile_name not in profiles:
        raise KeyError(f"unknown proof profile: {profile_name}")
    profile = profiles[profile_name]
    if not isinstance(profile, dict):
        raise ValueError(f"proof profile {profile_name} must be a table")
    return profile


def resolve_proof_profile(
    policy: dict[str, Any],
    profile_name: str,
    *,
    component_id: str | None = None,
) -> list[str]:
    """Resolve one proof profile into concrete proof IDs.

    Proof IDs belong to policy.toml. The planner may select profiles, but it may
    not synthesize provider/product proof strings directly.
    """
    profile = proof_profile(policy, profile_name)
    required = profile.get("required", [])
    if not isinstance(required, list) or not required:
        raise ValueError(f"proof profile {profile_name} requires a non-empty required list")
    context = {"component_id": component_id or ""}
    resolved: list[str] = []
    for raw in required:
        if not isinstance(raw, str) or not raw.strip():
            raise ValueError(f"proof profile {profile_name} contains an invalid proof template")
        placeholders = set(PROOF_PLACEHOLDER.findall(raw))
        unknown = placeholders - ALLOWED_PROOF_PLACEHOLDERS
        if unknown:
            raise ValueError(f"proof profile {profile_name} uses unsupported placeholders: {sorted(unknown)}")
        if "component_id" in placeholders and not component_id:
            raise ValueError(f"proof profile {profile_name} requires component_id")
        proof = raw.format(**context).strip()
        if not proof or "{" in proof or "}" in proof:
            raise ValueError(f"proof profile {profile_name} resolved invalid proof id: {proof!r}")
        resolved.append(proof)
    return resolved


def bound_proof_profile(policy: dict[str, Any], binding_group: str, key: str) -> str:
    bindings = policy.get("proof_bindings", {}).get(binding_group, {})
    if not isinstance(bindings, dict) or key not in bindings:
        raise KeyError(f"missing proof binding: {binding_group}.{key}")
    value = bindings[key]
    if not isinstance(value, str) or not value:
        raise ValueError(f"invalid proof binding: {binding_group}.{key}")
    proof_profile(policy, value)
    return value


def load_toml(path: Path) -> dict[str, Any]:
    with path.open("rb") as handle:
        return tomllib.load(handle)


def load_catalog(root: Path = ROOT) -> dict[str, Any]:
    return load_toml(root / "delivery" / "catalog.toml")


def load_policy(root: Path = ROOT) -> dict[str, Any]:
    return load_toml(root / "delivery" / "policy.toml")


def load_environments(root: Path = ROOT) -> dict[str, Any]:
    return load_toml(root / "delivery" / "environments.toml")


def load_images(root: Path = ROOT) -> dict[str, Any]:
    return load_toml(root / "delivery" / "images.lock.toml")


def component(catalog: dict[str, Any], component_id: str) -> dict[str, Any]:
    if not SAFE_ID.fullmatch(component_id):
        raise ValueError(f"unsafe component id: {component_id!r}")
    try:
        return catalog["components"][component_id]
    except KeyError as exc:
        raise KeyError(f"unknown delivery component: {component_id}") from exc


def norm_path(path: str) -> str:
    value = path.replace("\\", "/").lstrip("./")
    while "//" in value:
        value = value.replace("//", "/")
    return value


def matches(path: str, pattern: str) -> bool:
    """Repository glob match with predictable ** behavior.

    fnmatch's `*` spans `/`, which is intentionally conservative for CI routing.
    A pattern ending in `/**` also matches the directory itself.
    """
    p = norm_path(path)
    pat = norm_path(pattern)
    if pat.endswith("/**"):
        prefix = pat[:-3].rstrip("/")
        if p == prefix or p.startswith(prefix + "/"):
            return True
    return fnmatch.fnmatchcase(p, pat)


def git(*args: str, cwd: Path = ROOT, check: bool = True) -> str:
    proc = subprocess.run(
        ["git", *args], cwd=cwd, text=True, stdout=subprocess.PIPE,
        stderr=subprocess.PIPE, check=False,
    )
    if check and proc.returncode != 0:
        raise RuntimeError(f"git {' '.join(args)} failed: {proc.stderr.strip()}")
    return proc.stdout.strip()


def object_exists(sha: str, cwd: Path = ROOT) -> bool:
    if not sha or set(sha) == {"0"}:
        return False
    proc = subprocess.run(
        ["git", "cat-file", "-e", f"{sha}^{{commit}}"], cwd=cwd,
        stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, check=False,
    )
    return proc.returncode == 0


def changed_files_for_event(
    event_name: str,
    base_sha: str,
    head_sha: str,
    before_sha: str,
    root: Path = ROOT,
) -> tuple[list[str], bool, str]:
    """Return changed files, full-CI fail-safe flag and routing reason."""
    head = head_sha or git("rev-parse", "HEAD", cwd=root)
    event = event_name.strip()

    if event in {"pull_request", "merge_group"}:
        if not (object_exists(base_sha, root) and object_exists(head, root)):
            return [], True, "missing PR/merge-group commit range"
        merge_base = git("merge-base", base_sha, head, cwd=root)
        output = git("diff", "--name-only", "--diff-filter=ACMRDTUXB", merge_base, head, cwd=root)
        return sorted({norm_path(line) for line in output.splitlines() if line.strip()}), False, f"merge-base:{merge_base}"

    if event == "push":
        if object_exists(before_sha, root) and object_exists(head, root):
            output = git("diff", "--name-only", "--diff-filter=ACMRDTUXB", before_sha, head, cwd=root)
            return sorted({norm_path(line) for line in output.splitlines() if line.strip()}), False, f"push-range:{before_sha}..{head}"
        # New branch/force-push/missing-before must never guess HEAD^: that can
        # omit earlier commits and create a false-green pipeline.
        return [], True, "unknown push range; fail-safe full CI"

    # workflow_dispatch and unknown events are explicit operator intent. Running
    # full CI is preferable to an under-specified partial proof.
    return [], True, f"event:{event or 'unknown'} full CI"


@dataclass(frozen=True)
class WorkspacePackage:
    name: str
    root: str
    dependencies: frozenset[str]


def discover_workspace_packages(root: Path = ROOT) -> dict[str, WorkspacePackage]:
    frontend = root / "frontend"
    result: dict[str, WorkspacePackage] = {}
    if not frontend.exists():
        return result
    for manifest in frontend.rglob("package.json"):
        if "node_modules" in manifest.parts or ".next" in manifest.parts or "dist" in manifest.parts:
            continue
        try:
            data = json.loads(manifest.read_text(encoding="utf-8"))
        except (OSError, json.JSONDecodeError):
            continue
        name = data.get("name")
        if not isinstance(name, str) or not name:
            continue
        deps: set[str] = set()
        for key in ("dependencies", "devDependencies", "peerDependencies", "optionalDependencies"):
            section = data.get(key, {})
            if isinstance(section, dict):
                deps.update(str(dep) for dep in section)
        rel = manifest.parent.relative_to(root).as_posix()
        result[name] = WorkspacePackage(name=name, root=rel, dependencies=frozenset(deps))
    names = set(result)
    return {
        name: WorkspacePackage(pkg.name, pkg.root, frozenset(dep for dep in pkg.dependencies if dep in names))
        for name, pkg in result.items()
    }


def package_for_path(path: str, packages: dict[str, WorkspacePackage]) -> WorkspacePackage | None:
    p = norm_path(path)
    best: WorkspacePackage | None = None
    for pkg in packages.values():
        if p == pkg.root or p.startswith(pkg.root + "/"):
            if best is None or len(pkg.root) > len(best.root):
                best = pkg
    return best


def transitive_dependencies(name: str, packages: dict[str, WorkspacePackage]) -> set[str]:
    seen: set[str] = set()
    stack = [name]
    while stack:
        current = stack.pop()
        if current in seen:
            continue
        seen.add(current)
        pkg = packages.get(current)
        if pkg:
            stack.extend(dep for dep in pkg.dependencies if dep not in seen)
    return seen


def consumers_of_package(
    changed_package: str,
    catalog: dict[str, Any],
    packages: dict[str, WorkspacePackage],
) -> set[str]:
    consumers: set[str] = set()
    for cid, cfg in catalog.get("components", {}).items():
        workspace = cfg.get("workspace")
        if not workspace or workspace not in packages:
            continue
        if changed_package in transitive_dependencies(workspace, packages):
            consumers.add(cid)
    return consumers


def deployable_components(catalog: dict[str, Any]) -> list[str]:
    return sorted(cid for cid, cfg in catalog.get("components", {}).items() if cfg.get("deployable") is True)


def frontend_components(catalog: dict[str, Any]) -> list[str]:
    return sorted(
        cid for cid, cfg in catalog.get("components", {}).items()
        if cfg.get("provider") in {"frontend-host", "mobile"}
    )


def matrix(items: Iterable[dict[str, Any]]) -> str:
    return json.dumps({"include": list(items)}, separators=(",", ":"), sort_keys=True)


def compact_json(value: Any) -> str:
    return json.dumps(value, separators=(",", ":"), sort_keys=True)


def github_output(name: str, value: str) -> None:
    target = os.environ.get("GITHUB_OUTPUT")
    if not target:
        print(f"{name}={value}")
        return
    with open(target, "a", encoding="utf-8") as handle:
        handle.write(f"{name}={value}\n")
