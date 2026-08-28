#!/usr/bin/env python3
"""Semantic validation for the Notrelix Compose production/staging topology.

Standalone static validator: it renders the production and staging Compose trees
directly from the repository compose files (no delivery contract, no planner) and
asserts the Notrelix topology, security, and rootless invariants.
"""
from __future__ import annotations

import json
import os
import re
import secrets
import subprocess
from pathlib import Path
from typing import Any

ROOT = Path.cwd()
MIN_COMPOSE_VERSION = (2, 24, 4)

POSTGRES_SERVICE = "postgres"
REDIS_SERVICE = "redis"
RABBITMQ_SERVICE = "rabbitmq"
NGINX_SERVICE = "nginx"
BACKEND_SERVICE = "backend"
PUBLIC_SERVICES = ("frontend-web", "frontend-marketing", NGINX_SERVICE)
ALL_REQUIRED_SERVICES = (
    POSTGRES_SERVICE,
    REDIS_SERVICE,
    RABBITMQ_SERVICE,
    BACKEND_SERVICE,
    "frontend-web",
    "frontend-marketing",
    NGINX_SERVICE,
)

PROD_FILES = (ROOT / "docker-compose.yml", ROOT / "docker-compose.prod.yml")
STAGING_FILES = (ROOT / "docker-compose.yml", ROOT / "docker-compose.staging.yml")
PROD_NGINX_CONF = ROOT / "infra/nginx/nginx.prod.conf"
STAGING_NGINX_CONF = ROOT / "infra/nginx/nginx.conf"
WEB_NGINX_CONF = ROOT / "frontend/apps/web/nginx.conf"
WEB_DOCKERFILE = ROOT / "frontend/Dockerfile"
BACKEND_DOCKERFILE = ROOT / "backend/Dockerfile"

EXPECTED_UPSTREAMS = {
    BACKEND_SERVICE: "http://backend:8000",
    "frontend-web": "http://frontend-web:8080",
    "frontend-marketing": "http://frontend-marketing:3000",
}


def fail(msg: str) -> None:
    raise SystemExit(f"infra validation failed: {msg}")


def compose_version() -> tuple[int, ...]:
    out = subprocess.run(
        ["docker", "compose", "version", "--short"],
        check=True,
        text=True,
        capture_output=True,
    ).stdout.strip()
    numbers: list[int] = []
    for part in out.split("-")[0].split("."):
        if not part.isdigit():
            break
        numbers.append(int(part))
    while len(numbers) < 3:
        numbers.append(0)
    return tuple(numbers)


def ephemeral_environment() -> dict[str, str]:
    """Ephemeral credentials for rendering; never rendered by prod itself."""
    postgres_password = secrets.token_hex(16)
    redis_password = secrets.token_hex(16)
    return {
        "POSTGRES_USER": "postgres",
        "POSTGRES_PASSWORD": postgres_password,
        "POSTGRES_DB": "notrelix",
        "REDIS_PASSWORD": redis_password,
        "RABBITMQ_USER": "rabbitmq",
        "RABBITMQ_PASSWORD": secrets.token_hex(16),
        "RABBITMQ_VHOST": "/",
        "JWT_SECRET": secrets.token_hex(32),
        "JWT_ISSUER": "http://localhost",
        "JWT_AUDIENCE": "http://localhost",
        "CORS_ORIGIN": "http://localhost",
        "RESEND_API_KEY": secrets.token_hex(16),
        "CONNECTIONSTRINGS_NOTRELIXDB": (
            f"Host={POSTGRES_SERVICE};Port=5432;Database=notrelix;"
            f"Username=postgres;Password={postgres_password}"
        ),
        "CONNECTIONSTRINGS_REDIS": f"{REDIS_SERVICE}:6379,password={redis_password}",
        "BACKEND_NETWORK_SUBNET": "172.28.0.0/24",
        "HTTPS_REDIRECTION_ENABLED": "false",
        "HTTP_PORT": "8080",
    }


def compose(files: list[Path], env: dict[str, str]) -> dict[str, Any]:
    process_env = os.environ.copy()
    process_env.update(env)
    args = ["docker", "compose"]
    for file in files:
        args += ["-f", str(file)]
    args += ["config", "--format", "json"]
    out = subprocess.run(
        args,
        cwd=ROOT,
        env=process_env,
        check=True,
        text=True,
        capture_output=True,
    ).stdout
    return json.loads(out)


def ports(service: dict[str, Any]) -> set[int]:
    result: set[int] = set()
    for port in service.get("ports", []) or []:
        if isinstance(port, dict) and port.get("target") is not None:
            result.add(int(port["target"]))
    return result


def env_map(service: dict[str, Any]) -> dict[str, str]:
    value = service.get("environment") or {}
    if isinstance(value, dict):
        return {str(k): "" if v is None else str(v) for k, v in value.items()}
    result: dict[str, str] = {}
    for item in value:
        if isinstance(item, str) and "=" in item:
            key, val = item.split("=", 1)
            result[key] = val
    return result


def network_names(service: dict[str, Any]) -> set[str]:
    value = service.get("networks") or {}
    if isinstance(value, dict):
        return set(value)
    return {str(item) for item in value}


def main() -> int:
    version = compose_version()
    if version < MIN_COMPOSE_VERSION:
        fail(
            f"docker compose {version} is too old; semantics require >= "
            f"{'.'.join(map(str, MIN_COMPOSE_VERSION))}"
        )

    env = ephemeral_environment()
    prod = compose(list(PROD_FILES), env)
    staging = compose(list(STAGING_FILES), env)

    services = prod["services"]
    networks = prod.get("networks", {})

    missing = sorted(set(ALL_REQUIRED_SERVICES) - set(services))
    if missing:
        fail(f"production Compose missing services: {missing}")

    backend_network = networks.get("backend-network", {})
    if not backend_network.get("internal"):
        fail("backend-network must remain internal")
    ipam = backend_network.get("ipam", {}).get("config", []) or []
    if not any(str(item.get("subnet", "")) == env["BACKEND_NETWORK_SUBNET"] for item in ipam):
        fail("backend-network CIDR must match BACKEND_NETWORK_SUBNET")
    if "backend-egress-network" not in networks:
        fail("backend-egress-network is required")

    for name in (POSTGRES_SERVICE, REDIS_SERVICE, RABBITMQ_SERVICE):
        if ports(services[name]):
            fail(f"{name} must not publish host ports")
    if services[RABBITMQ_SERVICE].get("profiles"):
        fail("RabbitMQ must not be optional/profile-gated in staging/production")

    backend = services[BACKEND_SERVICE]
    if backend.get("read_only") is not True:
        fail("production backend must be read_only")
    if "ALL" not in (backend.get("cap_drop") or []):
        fail("production backend must cap_drop ALL")
    if backend.get("cap_add"):
        fail("backend must not add Linux capabilities")
    if "no-new-privileges:true" not in (backend.get("security_opt") or []):
        fail("backend must enable no-new-privileges")
    bnet = network_names(backend)
    if not {"backend-network", "backend-egress-network"}.issubset(bnet):
        fail("backend must join backend-network and backend-egress-network")
    if "frontend-network" in bnet:
        fail("backend must not use frontend-network as egress")

    benv = env_map(backend)
    required_backend_env = {
        "Messaging__Transport": "RabbitMQ",
        "Messaging__RabbitMQ__Host": RABBITMQ_SERVICE,
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
    if RABBITMQ_SERVICE not in (backend.get("depends_on") or {}):
        fail("backend must depend on healthy RabbitMQ")

    nginx = services[NGINX_SERVICE]
    if ports(nginx) != {8080}:
        fail(f"production nginx must publish container port 8080 only, got {sorted(ports(nginx))}")
    if str(nginx.get("user", "")) != "nginx":
        fail("production nginx must run as nginx user")
    if "ALL" not in (nginx.get("cap_drop") or []):
        fail("production nginx must cap_drop ALL")
    if nginx.get("cap_add"):
        fail("production nginx must not add Linux capabilities")

    prod_conf = PROD_NGINX_CONF.read_text(encoding="utf-8")
    if re.search(r"\blisten\s+443(?:\s|;)", prod_conf):
        fail("production origin nginx must not terminate TLS")
    if not re.search(r"\blisten\s+8080;", prod_conf):
        fail("production nginx must listen on 8080")
    if "proxy_set_header X-Forwarded-Proto https;" not in prod_conf:
        fail("production gateway must forward canonical HTTPS scheme")
    if "proxy_set_header X-Forwarded-Port 443;" not in prod_conf:
        fail("production gateway must forward public port 443")

    for service, upstream in EXPECTED_UPSTREAMS.items():
        if upstream not in prod_conf:
            fail(f"production nginx missing upstream {upstream} for {service}")

    web_dockerfile = WEB_DOCKERFILE.read_text(encoding="utf-8")
    web_conf = WEB_NGINX_CONF.read_text(encoding="utf-8")
    if not re.search(r"^USER\s+nginx\s*$", web_dockerfile, re.MULTILINE):
        fail("frontend web image must run as nginx user")
    if not re.search(r"\blisten\s+8080;", web_conf):
        fail("frontend web nginx must listen on port 8080")
    if not re.search(r"^pid\s+/tmp/nginx\.pid;", web_conf, re.MULTILINE):
        fail("frontend web nginx pid must live under /tmp")
    if not re.search(r"client_body_temp_path\s+/tmp/", web_conf):
        fail("frontend web nginx temp paths must be under /tmp")
    if "/etc/nginx/nginx.conf" not in web_dockerfile:
        fail("frontend web image must install full nginx configuration")

    backend_dockerfile = BACKEND_DOCKERFILE.read_text(encoding="utf-8")
    if "--locked-mode ||" in backend_dockerfile:
        fail("backend Dockerfile must not use fail-open restore fallback")
    if not re.search(r"^USER\s+notrelix\s*$", backend_dockerfile, re.MULTILINE):
        fail("backend image must run as notrelix user")

    sensitive = ("PASSWORD", "SECRET", "APIKEY", "API_KEY", "CONNECTIONSTRING", "TOKEN")
    for service_name in PUBLIC_SERVICES:
        for key in env_map(services[service_name]):
            if any(marker in key.upper() for marker in sensitive):
                fail(f"public service {service_name} receives sensitive env key {key}")

    staging_services = staging.get("services", {})
    missing_staging = sorted(set(ALL_REQUIRED_SERVICES) - set(staging_services))
    if missing_staging:
        fail(f"staging missing services: {missing_staging}")
    staging_backend = staging_services[BACKEND_SERVICE]
    if RABBITMQ_SERVICE not in (staging_backend.get("depends_on") or {}):
        fail("staging backend must depend on RabbitMQ")
    if env_map(staging_backend).get("Messaging__RabbitMQ__Host") != RABBITMQ_SERVICE:
        fail("staging backend must use resolved RabbitMQ compose service")
    for key, expected in (
        ("Rls__Enabled", "true"),
        ("Rls__SetSessionContext", "true"),
        ("DataProtection__PersistKeys", "true"),
    ):
        if env_map(staging_backend).get(key) != expected:
            fail(f"staging backend {key} must be {expected!r}")
    keys_path = env_map(staging_backend).get("DataProtection__KeysPath", "")
    if not keys_path or keys_path.startswith("/root"):
        fail("staging backend must persist DataProtection keys under writable non-root path")

    staging_nginx = staging_services[NGINX_SERVICE]
    if str(staging_nginx.get("user", "")) != "nginx":
        fail("staging nginx must run as nginx user")
    if staging_nginx.get("read_only") is not True:
        fail("staging nginx must be read_only")
    if ports(staging_nginx) != {8080}:
        fail("staging nginx must publish only container port 8080")
    staging_conf = STAGING_NGINX_CONF.read_text(encoding="utf-8")
    if not re.search(r"^pid\s+/tmp/nginx\.pid;", staging_conf, re.MULTILINE):
        fail("staging nginx pid must live under /tmp")
    if not re.search(r"\blisten\s+8080;", staging_conf):
        fail("staging nginx must listen on 8080")

    print("Notrelix infrastructure semantics: OK")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())