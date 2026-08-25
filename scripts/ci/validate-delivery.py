#!/usr/bin/env python3
from __future__ import annotations

import json
import re
import sys
from pathlib import Path
from delivery_model import (
    ROOT, ALLOWED_PROOF_PLACEHOLDERS, PROOF_PLACEHOLDER, bound_proof_profile,
    component, load_catalog, load_environments, load_images, load_policy,
    proof_profile, resolve_proof_profile,
)

DIGEST_REF = re.compile(r"^[A-Za-z0-9._/:\-]+@sha256:[0-9a-f]{64}$")
ENV_NAME = re.compile(r"^[A-Z][A-Z0-9_]*$")
SERVICE_NAME = re.compile(r"^[A-Za-z0-9][A-Za-z0-9_.-]*$")
ALLOWED_PROVIDERS = {"backend", "frontend-host", "mobile"}


def fail(message: str, errors: list[str]) -> None:
    errors.append(message)


def main() -> int:
    errors: list[str] = []
    catalog = load_catalog(ROOT)
    policy = load_policy(ROOT)
    envs = load_environments(ROOT)
    images = load_images(ROOT)
    versions = {catalog.get("schema_version"), policy.get("schema_version"), envs.get("schema_version"), images.get("schema_version")}
    if versions != {4}:
        fail(f"delivery schema versions must all be 4, got {sorted(versions, key=str)}", errors)

    components = catalog.get("components", {})
    if not components:
        fail("delivery catalog has no components", errors)

    profiles = policy.get("proof_profiles", {})
    if not isinstance(profiles, dict) or not profiles:
        fail("policy.proof_profiles must be a non-empty table", errors)
    else:
        for profile_name, profile_cfg in profiles.items():
            if not isinstance(profile_cfg, dict):
                fail(f"proof profile {profile_name} must be a table", errors)
                continue
            unknown_profile_keys = set(profile_cfg) - {"required", "providers"}
            if unknown_profile_keys:
                fail(f"proof profile {profile_name} has unknown fields {sorted(unknown_profile_keys)}", errors)
            providers = profile_cfg.get("providers", [])
            if providers and (not isinstance(providers, list) or any(x not in ALLOWED_PROVIDERS for x in providers)):
                fail(f"proof profile {profile_name} has invalid providers {providers}", errors)
            required = profile_cfg.get("required", [])
            if not isinstance(required, list) or not required:
                fail(f"proof profile {profile_name} requires a non-empty required list", errors)
                continue
            for template in required:
                if not isinstance(template, str) or not template:
                    fail(f"proof profile {profile_name} contains an invalid proof template", errors)
                    continue
                unknown = set(PROOF_PLACEHOLDER.findall(template)) - ALLOWED_PROOF_PLACEHOLDERS
                if unknown:
                    fail(f"proof profile {profile_name} uses unsupported placeholders {sorted(unknown)}", errors)

    bindings = policy.get("proof_bindings", {})
    for group in ("planes", "security_domains"):
        table = bindings.get(group, {}) if isinstance(bindings, dict) else {}
        if not isinstance(table, dict) or not table:
            fail(f"proof_bindings.{group} must be non-empty", errors)
        else:
            for key, profile_name in table.items():
                if profile_name not in profiles:
                    fail(f"proof binding {group}.{key} references unknown profile {profile_name}", errors)
    for group in ("packaging", "release"):
        table = bindings.get(group, {}) if isinstance(bindings, dict) else {}
        profile_name = table.get("profile") if isinstance(table, dict) else None
        if not profile_name or profile_name not in profiles:
            fail(f"proof_bindings.{group}.profile must reference an existing profile", errors)
    workspaces: dict[str, str] = {}
    image_names: dict[str, str] = {}
    env_vars: dict[str, str] = {}
    compose_services: dict[str, str] = {}
    for cid, cfg in components.items():
        try:
            component(catalog, cid)
        except Exception as exc:
            fail(str(exc), errors); continue
        if cfg.get("provider") not in ALLOWED_PROVIDERS:
            fail(f"{cid}: unsupported provider {cfg.get('provider')}", errors)
        profile_name = str(cfg.get("proof_profile", ""))
        if not profile_name:
            fail(f"{cid}: proof_profile is required", errors)
        elif profile_name not in profiles:
            fail(f"{cid}: unknown proof_profile {profile_name}", errors)
        else:
            allowed_profile_providers = profiles[profile_name].get("providers", [])
            if allowed_profile_providers and cfg.get("provider") not in allowed_profile_providers:
                fail(
                    f"{cid}: proof_profile {profile_name} does not allow provider {cfg.get('provider')}",
                    errors,
                )
            try:
                resolve_proof_profile(policy, profile_name, component_id=cid)
            except Exception as exc:
                fail(f"{cid}: invalid proof profile resolution: {exc}", errors)
        roots = cfg.get("roots")
        if not isinstance(roots, list) or not roots:
            fail(f"{cid}: roots must be a non-empty list", errors)
        ws = cfg.get("workspace")
        if ws:
            if ws in workspaces:
                fail(f"workspace {ws} registered by both {workspaces[ws]} and {cid}", errors)
            workspaces[ws] = cid
        if cfg.get("deployable"):
            c = cfg.get("container") or {}
            for key in ("context", "dockerfile", "image_name", "deploy_env_var", "runtime_port", "health_path"):
                if not c.get(key): fail(f"{cid}: deployable component missing container.{key}", errors)
            name = c.get("image_name")
            if name in image_names: fail(f"image_name {name} reused by {image_names[name]} and {cid}", errors)
            image_names[name] = cid
            service = str(c.get("compose_service", ""))
            if not SERVICE_NAME.fullmatch(service): fail(f"{cid}: invalid/missing compose_service {service}", errors)
            elif service in compose_services: fail(f"compose_service {service} reused by {compose_services[service]} and {cid}", errors)
            else: compose_services[service] = cid
            var = c.get("deploy_env_var", "")
            if not ENV_NAME.fullmatch(str(var)): fail(f"{cid}: invalid deploy_env_var {var}", errors)
            if var in env_vars: fail(f"deploy env var {var} reused by {env_vars[var]} and {cid}", errors)
            env_vars[var] = cid
            dockerfile = ROOT / str(c.get("dockerfile", ""))
            # Bundle-only validation may not include every source file, but a declared
            # overlay Dockerfile must exist when its parent tree is present.
            if dockerfile.parent.exists() and not dockerfile.exists():
                fail(f"{cid}: Dockerfile does not exist: {dockerfile.relative_to(ROOT)}", errors)
            for arg, lock_name in c.get("build_arg_locks", {}).items():
                lock = images.get("images", {}).get(lock_name)
                if not lock: fail(f"{cid}: build arg {arg} references unknown lock {lock_name}", errors)
                elif lock.get("class") != "build": fail(f"{cid}: build arg {arg} references non-build lock {lock_name}", errors)

    runtime_envs: set[str] = set()
    for name, lock in images.get("images", {}).items():
        ref = str(lock.get("ref", ""))
        if not DIGEST_REF.fullmatch(ref):
            fail(f"image lock {name} is not immutable: {ref}", errors)
        if lock.get("class") not in {"build", "runtime", "tooling"}:
            fail(f"image lock {name} has invalid class {lock.get('class')}", errors)
        if lock.get("class") == "tooling" and "mcr.microsoft.com" not in ref and "docker.io" not in ref:
            # Tooling locks are CI renderer/runtime authorities; keep them on
            # first-party registries so provenance stays auditable.
            fail(f"tooling image {name} must reference an approved registry", errors)
        if lock.get("class") == "runtime":
            var = str(lock.get("deploy_env_var", ""))
            if not ENV_NAME.fullmatch(var): fail(f"runtime image {name} invalid deploy_env_var {var}", errors)
            if var in runtime_envs or var in env_vars: fail(f"duplicate runtime/application deploy env var {var}", errors)
            runtime_envs.add(var)
            service = str(lock.get("compose_service", ""))
            if not SERVICE_NAME.fullmatch(service): fail(f"runtime image {name} invalid/missing compose_service {service}", errors)
            elif service in compose_services: fail(f"compose_service {service} reused by {compose_services[service]} and runtime image {name}", errors)
            else: compose_services[service] = f"runtime:{name}"

    rule_ids: set[str] = set()
    for rule in policy.get("change_rules", []):
        rid = rule.get("id")
        if not rid or rid in rule_ids: fail(f"duplicate/empty change rule id: {rid}", errors)
        rule_ids.add(rid)
        for key in ("components", "package_components"):
            for cid in rule.get(key, []):
                if cid not in components: fail(f"rule {rid}: unknown {key} component {cid}", errors)
        for plane in rule.get("planes", []):
            try: bound_proof_profile(policy, "planes", str(plane))
            except Exception as exc: fail(f"rule {rid}: {exc}", errors)
        for domain in rule.get("security_domains", []):
            try: bound_proof_profile(policy, "security_domains", str(domain))
            except Exception as exc: fail(f"rule {rid}: {exc}", errors)
        if not rule.get("patterns"): fail(f"rule {rid}: no patterns", errors)


    deployment = policy.get("deployment", {})
    migration_component = deployment.get("migration_component", "")
    if migration_component and migration_component not in components:
        fail(f"deployment.migration_component is unknown: {migration_component}", errors)
    commands = deployment.get("migration_commands", [])
    if migration_component and (not isinstance(commands, list) or not commands):
        fail("deployment migration component requires non-empty migration_commands", errors)
    migration_paths = deployment.get("migration_paths", [])
    if migration_component and (not isinstance(migration_paths, list) or not migration_paths):
        fail("deployment migration component requires migration_paths for rollback safety", errors)
    if deployment.get("schema_change_policy") != "expand-contract":
        fail("deployment.schema_change_policy must be expand-contract", errors)
    if deployment.get("rollback_after_schema_change") != "manual":
        fail("deployment.rollback_after_schema_change must be manual", errors)

    environments = envs.get("environments", {})
    if "staging" not in environments or "production" not in environments:
        fail("staging and production environments are mandatory", errors)
    allowed_modes = {"automatic-after-main-ci", "manual-promotion"}
    allowed_stateful = {"explicit-override"}
    allowed_environment_keys = {
        "compose_overlay", "promotion_mode", "run_migrations", "smoke_profile",
        "stateful_image_change_policy",
    }
    for name, cfg in environments.items():
        unknown_keys = set(cfg) - allowed_environment_keys
        if unknown_keys:
            fail(f"environment {name} contains non-authoritative/unknown fields: {sorted(unknown_keys)}", errors)
        overlay = str(cfg.get("compose_overlay", ""))
        if not overlay:
            fail(f"environment {name} missing compose_overlay", errors)
        elif (ROOT / "docker-compose.yml").exists() and not (ROOT / overlay).exists():
            fail(f"environment {name} compose overlay does not exist: {overlay}", errors)
        if cfg.get("promotion_mode") not in allowed_modes:
            fail(f"environment {name} invalid promotion_mode {cfg.get('promotion_mode')}", errors)
        if not isinstance(cfg.get("run_migrations"), bool):
            fail(f"environment {name} run_migrations must be boolean", errors)
        if not cfg.get("smoke_profile"):
            fail(f"environment {name} missing smoke_profile", errors)
        if cfg.get("stateful_image_change_policy") not in allowed_stateful:
            fail(f"environment {name} invalid stateful_image_change_policy", errors)
    if environments.get("staging", {}).get("promotion_mode") != "automatic-after-main-ci":
        fail("staging promotion_mode must be automatic-after-main-ci", errors)
    if environments.get("production", {}).get("promotion_mode") != "manual-promotion":
        fail("production promotion_mode must be manual-promotion", errors)

    # Repository-aware checks activate after the overlay is applied to Notrelix.
    app_dir = ROOT / "frontend/apps"
    if app_dir.exists():
        for manifest in sorted(app_dir.glob("*/package.json")):
            try: pkg = json.loads(manifest.read_text(encoding="utf-8"))
            except json.JSONDecodeError:
                fail(f"invalid app package manifest: {manifest.relative_to(ROOT)}", errors); continue
            name = pkg.get("name")
            if name and name not in workspaces:
                fail(f"frontend app workspace {name} is not registered in delivery/catalog.toml", errors)
    global_json = ROOT / "backend/global.json"
    if global_json.exists():
        try:
            sdk = json.loads(global_json.read_text(encoding="utf-8"))["sdk"]["version"]
            sdk_source = images["images"]["dotnet-sdk"]["source"]
            if f":{sdk}" not in sdk_source:
                fail(f"dotnet-sdk image lock {sdk_source} does not match backend/global.json SDK {sdk}", errors)
        except (KeyError, json.JSONDecodeError):
            fail("backend/global.json is invalid or missing sdk.version", errors)

    if errors:
        print("Delivery model validation FAILED", file=sys.stderr)
        for err in errors: print(f" - {err}", file=sys.stderr)
        return 1
    print(f"Delivery model validation PASS: {len(components)} components, {len(rule_ids)} routing rules, {len(images.get('images', {}))} image locks")
    return 0

if __name__ == "__main__":
    raise SystemExit(main())
