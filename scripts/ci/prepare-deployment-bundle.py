#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import re
from pathlib import Path

from delivery_model import ROOT, load_catalog, load_environments, load_policy

ENV = re.compile(r"^[A-Z][A-Z0-9_]*$")
SERVICE = re.compile(r"^[A-Za-z0-9][A-Za-z0-9_.-]*$")
SHA = re.compile(r"^[0-9a-fA-F]{40}$")


def bool_text(value: bool) -> str:
    return "true" if value else "false"


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--manifest", required=True, type=Path)
    parser.add_argument("--environment", required=True)
    parser.add_argument("--output-dir", required=True, type=Path)
    args = parser.parse_args()

    data = json.loads(args.manifest.read_text(encoding="utf-8"))
    environments = load_environments(ROOT).get("environments", {})
    if args.environment not in environments:
        raise SystemExit(f"unknown delivery environment: {args.environment}")
    if data.get("schema_version") != 4:
        raise SystemExit("release manifest schema must be 4")

    source = str(data.get("source_sha", ""))
    if not SHA.fullmatch(source):
        raise SystemExit(f"invalid source_sha: {source}")

    images = data.get("images", [])
    if not isinstance(images, list) or not images:
        raise SystemExit("manifest images[] required")

    seen_services: set[str] = set()
    seen_env: set[str] = set()
    image_lines: list[str] = []
    stateful: list[str] = []
    compose = ["# GENERATED immutable release overlay", "services:"]
    for item in sorted(images, key=lambda i: (i.get("kind", ""), i.get("id", ""))):
        ref = str(item.get("ref", ""))
        var = str(item.get("deploy_env_var", ""))
        service = str(item.get("compose_service", ""))
        image_id = str(item.get("id", ""))
        if "@sha256:" not in ref:
            raise SystemExit(f"mutable image: {image_id}={ref}")
        if not ENV.fullmatch(var):
            raise SystemExit(f"invalid deploy env var: {image_id}={var}")
        if not SERVICE.fullmatch(service):
            raise SystemExit(f"invalid compose service: {image_id}={service}")
        if service in seen_services or var in seen_env:
            raise SystemExit(f"duplicate service/env: {image_id}")
        seen_services.add(service)
        seen_env.add(var)
        image_lines.append(f"{var}={ref}")
        if item.get("stateful"):
            stateful.append(f"{var}={ref}")
        compose += [f"  {service}:", f"    image: {json.dumps(ref)}"]
        if item.get("kind") == "application":
            # Compose reset semantics: a plain `build: null` override does not
            # remove the inherited build definition; `!reset null` deletes the
            # key during merge so the rendered stack has no usable build path.
            compose.append("    build: !reset null")

    env_cfg = environments[args.environment]
    overlay = str(env_cfg.get("compose_overlay", ""))
    if not overlay:
        raise SystemExit("environment compose_overlay missing")
    run_migrations = env_cfg.get("run_migrations")
    if not isinstance(run_migrations, bool):
        raise SystemExit("environment run_migrations must be boolean")
    smoke_profile = str(env_cfg.get("smoke_profile", ""))
    if not smoke_profile:
        raise SystemExit("environment smoke_profile missing")
    stateful_policy = str(env_cfg.get("stateful_image_change_policy", ""))
    if stateful_policy != "explicit-override":
        raise SystemExit(f"unsupported stateful image change policy: {stateful_policy}")

    policy = load_policy(ROOT)
    catalog = load_catalog(ROOT)
    deployment = policy.get("deployment", {})
    migration_component = deployment.get("migration_component", "")
    migration_service = ""
    if migration_component:
        comp = catalog.get("components", {}).get(migration_component)
        if not comp:
            raise SystemExit(f"unknown migration_component: {migration_component}")
        migration_service = str(comp.get("container", {}).get("compose_service", ""))
    migration_commands = deployment.get("migration_commands", [])

    schema_change = bool(data.get("schema_change", False))
    manifest_rollback = str(data.get("rollback_after_schema_change", ""))
    policy_rollback = str(deployment.get("rollback_after_schema_change", ""))
    if manifest_rollback != policy_rollback:
        raise SystemExit(
            f"release rollback policy drift: manifest={manifest_rollback!r}, policy={policy_rollback!r}"
        )
    if schema_change and not run_migrations:
        raise SystemExit("schema-changing release cannot deploy to an environment with run_migrations=false")

    out = args.output_dir
    out.mkdir(parents=True, exist_ok=True)
    (out / "images.env").write_text("\n".join(image_lines) + "\n", encoding="utf-8")
    (out / "stateful.env").write_text("\n".join(sorted(stateful)) + "\n", encoding="utf-8")
    stateful_services = sorted(
        {str(item.get("compose_service", "")) for item in images if item.get("stateful")}
    )
    (out / "stateful.services").write_text("\n".join(stateful_services) + "\n", encoding="utf-8")
    (out / "migration.commands").write_text(
        "\n".join(str(item) for item in migration_commands) + "\n", encoding="utf-8"
    )
    (out / "release.generated.yml").write_text("\n".join(compose) + "\n", encoding="utf-8")
    (out / "manifest.json").write_text(
        json.dumps(data, indent=2, sort_keys=True) + "\n", encoding="utf-8"
    )
    metadata = {
        "RELEASE_SHA": source,
        "COMPOSE_OVERLAY": overlay,
        "ENVIRONMENT": args.environment,
        "MIGRATION_SERVICE": migration_service,
        "RUN_MIGRATIONS": bool_text(run_migrations),
        "SCHEMA_CHANGE": bool_text(schema_change),
        "ROLLBACK_AFTER_SCHEMA_CHANGE": policy_rollback,
        "STATEFUL_IMAGE_CHANGE_POLICY": stateful_policy,
        "SMOKE_PROFILE": smoke_profile,
        "PROMOTION_MODE": str(env_cfg.get("promotion_mode", "")),
    }
    (out / "metadata.env").write_text(
        "".join(f"{key}={value}\n" for key, value in metadata.items()), encoding="utf-8"
    )
    print(
        json.dumps(
            {
                "environment": args.environment,
                "release_sha": source,
                "compose_overlay": overlay,
                "images": len(images),
                "stateful": len(stateful),
                "schema_change": schema_change,
                "rollback_after_schema_change": policy_rollback,
                "promotion_mode": env_cfg.get("promotion_mode"),
            },
            sort_keys=True,
        )
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
