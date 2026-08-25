#!/usr/bin/env python3
"""Semantic validation for Notrelix container/Compose production topology.

This gate validates the *contracts* that are easy to accidentally break while
editing Docker/Compose files:
- one delivery image authority and exact-digest release overlay,
- RabbitMQ is mandatory for Staging/Production,
- backend proxy trust is restricted to the isolated backend network,
- production TLS is edge-terminated and forwarded as HTTPS by the rootless
  origin gateway,
- application containers keep least-privilege runtime settings.
"""
from __future__ import annotations

import json
import os
import re
import secrets
import subprocess
import tempfile
from pathlib import Path

from delivery_model import ROOT, deployable_components, load_catalog, load_images

ZERO = "0" * 64
# `build: !reset null` in the generated release overlay requires the Compose
# merge implementation that understands the !reset tag. Deployment hosts and
# CI runners must satisfy this floor before release rendering is trusted.
MIN_COMPOSE_VERSION = (2, 24, 4)


def _ephemeral_test_environment() -> dict[str, str]:
    """Runtime-generated non-production values; no committed credentials."""
    postgres_password = secrets.token_hex(16)
    redis_password = secrets.token_hex(16)
    rabbitmq_password = secrets.token_hex(16)
    return {
        "POSTGRES_USER": "postgres",
        "POSTGRES_PASSWORD": postgres_password,
        "POSTGRES_DB": "notrelix",
        "REDIS_PASSWORD": redis_password,
        "RABBITMQ_USER": "rabbitmq",
        "RABBITMQ_PASSWORD": rabbitmq_password,
        "RABBITMQ_VHOST": "/",
        "JWT_SECRET": secrets.token_hex(32),
        "JWT_ISSUER": "http://localhost",
        "JWT_AUDIENCE": "http://localhost",
        "CORS_ORIGIN": "http://localhost",
        "RESEND_API_KEY": secrets.token_hex(16),
        "CONNECTIONSTRINGS_NOTRELIXDB": f"Host=postgres;Port=5432;Database=notrelix;Username=postgres;Password={postgres_password}",
        "CONNECTIONSTRINGS_REDIS": f"redis:6379,password={redis_password}",
        "BACKEND_NETWORK_SUBNET": "172.28.0.0/24",
        "HTTPS_REDIRECTION_ENABLED": "false",
        "HTTP_PORT": "8080",
    }


def fail(msg: str) -> None:
    raise SystemExit(f"infra validation failed: {msg}")


def immutable_test_environment() -> dict[str, str]:
    env = _ephemeral_test_environment()
    catalog = load_catalog(ROOT)
    for cid in deployable_components(catalog):
        cfg = catalog["components"][cid]["container"]
        env[str(cfg["deploy_env_var"])] = f"ghcr.io/example/{cfg['image_name']}@sha256:{ZERO}"
    for image_id, lock in load_images(ROOT).get("images", {}).items():
        if lock.get("class") != "runtime":
            continue
        ref = str(lock.get("ref", ""))
        var = str(lock.get("deploy_env_var", ""))
        if "@sha256:" not in ref or not var:
            fail(f"invalid runtime image lock {image_id}")
        env[var] = ref
    return env


def compose(files: list[str], env: dict[str, str]) -> dict:
    process_env = os.environ.copy()
    process_env.update(env)
    args = ["docker", "compose"]
    for file in files:
        args += ["-f", file]
    args += ["config", "--format", "json"]
    out = subprocess.run(
        args, cwd=ROOT, env=process_env, check=True, text=True, capture_output=True
    ).stdout
    return json.loads(out)


def render_release_overlay(tmp: Path, env: dict[str, str]) -> Path:
    catalog = load_catalog(ROOT)
    artifacts = []
    for cid in deployable_components(catalog):
        cfg = catalog["components"][cid]["container"]
        artifacts.append({"component_id": cid, "image": env[str(cfg["deploy_env_var"])]})
    manifest = tmp / "application-manifest.json"
    manifest.write_text(
        json.dumps({"schema_version": 4, "artifacts": artifacts}, indent=2) + "\n",
        encoding="utf-8",
    )
    overlay = tmp / "release.generated.yml"
    subprocess.run(
        [
            "python3",
            "scripts/ci/render-release-compose.py",
            "--application-manifest",
            str(manifest),
            "--output",
            str(overlay),
        ],
        cwd=ROOT,
        check=True,
    )
    return overlay


def ports(service: dict) -> set[int]:
    result: set[int] = set()
    for port in service.get("ports", []) or []:
        if isinstance(port, dict) and port.get("target") is not None:
            result.add(int(port["target"]))
    return result


def env_map(service: dict) -> dict[str, str]:
    value = service.get("environment") or {}
    if isinstance(value, dict):
        return {str(k): "" if v is None else str(v) for k, v in value.items()}
    result: dict[str, str] = {}
    for item in value:
        if isinstance(item, str) and "=" in item:
            k, v = item.split("=", 1)
            result[k] = v
    return result


def network_names(service: dict) -> set[str]:
    value = service.get("networks") or {}
    if isinstance(value, dict):
        return set(value)
    return {str(item) for item in value}


def compose_version() -> tuple[int, ...]:
    out = subprocess.run(
        ["docker", "compose", "version", "--short"],
        check=True, text=True, capture_output=True,
    ).stdout.strip()
    numbers: list[int] = []
    for part in out.split("-")[0].split("."):
        if not part.isdigit():
            break
        numbers.append(int(part))
    while len(numbers) < 3:
        numbers.append(0)
    return tuple(numbers)


def main() -> int:
    version = compose_version()
    if version < MIN_COMPOSE_VERSION:
        fail(
            f"docker compose {version} is too old; release rendering requires "
            f">= {'.'.join(map(str, MIN_COMPOSE_VERSION))} for !reset merge semantics"
        )
    env = immutable_test_environment()
    with tempfile.TemporaryDirectory(prefix="notrelix-infra-") as temp:
        generated = render_release_overlay(Path(temp), env)
        prod = compose(["docker-compose.yml", "docker-compose.prod.yml"], env)
        staging = compose(["docker-compose.yml", "docker-compose.staging.yml"], env)
        release = compose(["docker-compose.yml", "docker-compose.prod.yml", str(generated)], env)

    services = prod["services"]
    networks = prod.get("networks", {})
    backend_network = networks.get("backend-network", {})
    if not backend_network.get("internal"):
        fail("backend-network must remain internal")
    ipam = backend_network.get("ipam", {}).get("config", []) or []
    if not any(str(item.get("subnet", "")) == env["BACKEND_NETWORK_SUBNET"] for item in ipam):
        fail("backend-network CIDR must match BACKEND_NETWORK_SUBNET")
    if "backend-egress-network" not in networks:
        fail("backend-egress-network is required for isolated outbound backend traffic")

    for name in ("postgres", "redis", "rabbitmq"):
        if ports(services[name]):
            fail(f"{name} must not publish host ports")
    if services["rabbitmq"].get("profiles"):
        fail("RabbitMQ must not be optional/profile-gated in staging/production topology")

    backend = services["backend"]
    if backend.get("read_only") is not True:
        fail("production backend must be read_only")
    if "ALL" not in (backend.get("cap_drop") or []):
        fail("production backend must cap_drop ALL")
    if backend.get("cap_add"):
        fail("backend listens on port 8000 and must not add Linux capabilities")
    if "no-new-privileges:true" not in (backend.get("security_opt") or []):
        fail("backend must enable no-new-privileges")
    backend_networks = network_names(backend)
    if "backend-network" not in backend_networks or "backend-egress-network" not in backend_networks:
        fail("backend must join backend-network and backend-egress-network")
    if "frontend-network" in backend_networks:
        fail("backend must not use frontend-network as its egress network")

    benv = env_map(backend)
    required_backend_env = {
        "Messaging__Transport": "RabbitMQ",
        "Messaging__RabbitMQ__Host": "rabbitmq",
        "Messaging__RabbitMQ__Port": "5672",
        "ForwardedHeaders__RequireKnownProxyInProduction": "true",
        "ForwardedHeaders__KnownNetworks__0": env["BACKEND_NETWORK_SUBNET"],
    }
    for key, expected in required_backend_env.items():
        if benv.get(key) != expected:
            fail(f"backend environment {key} must be {expected!r}, got {benv.get(key)!r}")
    for key in ("Messaging__RabbitMQ__Username", "Messaging__RabbitMQ__Password"):
        if not benv.get(key):
            fail(f"backend environment {key} is required")
    depends = backend.get("depends_on") or {}
    if "rabbitmq" not in depends:
        fail("backend must depend on healthy RabbitMQ")

    # Production origin gateway is rootless and listens on an unprivileged port.
    nginx = services["nginx"]
    nginx_ports = ports(nginx)
    if nginx_ports != {8080}:
        fail(f"production nginx must publish container port 8080 only, got {sorted(nginx_ports)}")
    if str(nginx.get("user", "")) != "nginx":
        fail("production nginx must run as the nginx user")
    if "ALL" not in (nginx.get("cap_drop") or []):
        fail("production nginx must cap_drop ALL")
    if nginx.get("cap_add"):
        fail("rootless production nginx must not add Linux capabilities")

    prod_conf = (ROOT / "infra/nginx/nginx.prod.conf").read_text(encoding="utf-8")
    if re.search(r"\blisten\s+443(?:\s|;)", prod_conf):
        fail("production origin nginx must not terminate TLS")
    if not re.search(r"\blisten\s+8080;", prod_conf):
        fail("production nginx must listen on unprivileged port 8080")
    if "proxy_set_header X-Forwarded-Proto https;" not in prod_conf:
        fail("production gateway must forward canonical public scheme https")
    if "proxy_set_header X-Forwarded-Port 443;" not in prod_conf:
        fail("production gateway must forward canonical public port 443")

    expected_upstreams = {
        "backend": "http://backend:8000",
        "frontend-web": "http://frontend-web:8080",
        "frontend-marketing": "http://frontend-marketing:3000",
    }
    for service, upstream in expected_upstreams.items():
        if service not in services:
            fail(f"nginx upstream service missing from Compose: {service}")
        if upstream not in prod_conf:
            fail(f"production nginx missing expected upstream {upstream}")

    web_dockerfile = (ROOT / "frontend/Dockerfile").read_text(encoding="utf-8")
    web_conf = (ROOT / "frontend/apps/web/nginx.conf").read_text(encoding="utf-8")
    if not re.search(r"^USER\s+nginx\s*$", web_dockerfile, re.MULTILINE):
        fail("frontend web image must run as nginx user")
    if not re.search(r"\blisten\s+8080;", web_conf):
        fail("frontend web nginx must listen on unprivileged port 8080")
    # The web image runs fully unprivileged: pid and writable temp paths must
    # live under /tmp, and the config must be mounted as the main nginx.conf.
    if not re.search(r"^pid\s+/tmp/nginx\.pid;", web_conf, re.MULTILINE):
        fail("frontend web nginx must write its pid under /tmp for rootless runtime")
    if not re.search(r"client_body_temp_path\s+/tmp/", web_conf):
        fail("frontend web nginx must redirect writable temp paths to /tmp")
    if "/etc/nginx/nginx.conf" not in web_dockerfile:
        fail("frontend web image must install the full nginx configuration authority")

    backend_dockerfile = (ROOT / "backend/Dockerfile").read_text(encoding="utf-8")
    if "--locked-mode ||" in backend_dockerfile:
        fail("backend Dockerfile must not use fail-open locked restore fallback")
    if not re.search(r"^USER\s+notrelix\s*$", backend_dockerfile, re.MULTILINE):
        fail("backend image must run as notrelix user")

    sensitive = ("PASSWORD", "SECRET", "APIKEY", "API_KEY", "CONNECTIONSTRING", "TOKEN")
    for service_name in ("frontend-web", "frontend-marketing", "nginx"):
        service_env = env_map(services[service_name])
        for key in service_env:
            if any(marker in key.upper() for marker in sensitive):
                fail(f"public service {service_name} receives sensitive env key {key}")

    for name in ("backend", "frontend-web", "frontend-marketing", "nginx", "postgres", "redis", "rabbitmq"):
        if name not in staging.get("services", {}):
            fail(f"staging missing service {name}")
    staging_backend = staging["services"]["backend"]
    if "rabbitmq" not in (staging_backend.get("depends_on") or {}):
        fail("staging backend must depend on RabbitMQ")
    if env_map(staging_backend).get("Messaging__RabbitMQ__Host") != "rabbitmq":
        fail("staging backend must use RabbitMQ compose service")
    # Non-development topologies must satisfy the application's fail-closed
    # startup validation: tenant isolation and persistent key ring.
    for key, expected in (
        ("Rls__Enabled", "true"),
        ("Rls__SetSessionContext", "true"),
        ("DataProtection__PersistKeys", "true"),
    ):
        if env_map(staging_backend).get(key) != expected:
            fail(f"staging backend {key} must be {expected!r}")

    release_services = release["services"]
    for name in ("postgres", "redis", "rabbitmq", "backend", "frontend-web", "frontend-marketing", "nginx"):
        image = release_services[name].get("image", "")
        if "@sha256:" not in image:
            fail(f"release service {name} is not digest-pinned")
    for name in ("backend", "frontend-web", "frontend-marketing"):
        # The merged Compose output must not carry a usable build definition at
        # all — a falsy-but-present `build` key would still be a build path.
        if release_services[name].get("build"):
            fail(f"release service {name} still contains a build definition")
        if "build" in release_services[name]:
            fail(f"release service {name} still declares a build key after merge")

    print("Notrelix infrastructure semantics: OK")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
