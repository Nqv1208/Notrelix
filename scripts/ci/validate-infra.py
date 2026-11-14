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
PGBOUNCER_SERVICE = "pgbouncer"
BACKEND_SERVICE = "backend-api"
REALTIME_SERVICE = "backend-realtime"
WORKER_SERVICE = "backend-worker"
BACKEND_ROLES = {BACKEND_SERVICE: "api", REALTIME_SERVICE: "realtime", WORKER_SERVICE: "worker"}
PUBLIC_SERVICES = ("frontend-web", "frontend-marketing", NGINX_SERVICE)
ALL_REQUIRED_SERVICES = (
    POSTGRES_SERVICE,
    REDIS_SERVICE,
    RABBITMQ_SERVICE,
    PGBOUNCER_SERVICE,
    BACKEND_SERVICE,
    REALTIME_SERVICE,
    WORKER_SERVICE,
    "frontend-web",
    "frontend-marketing",
    NGINX_SERVICE,
)
DATA_PROTECTION_KEYS_PATH = "/var/lib/notrelix/dataprotection-keys"

PROD_FILES = (ROOT / "docker-compose.yml", ROOT / "docker-compose.prod.yml")
STAGING_FILES = (ROOT / "docker-compose.yml", ROOT / "docker-compose.staging.yml")
PROD_NGINX_CONF = ROOT / "infra/nginx/nginx.prod.conf"
STAGING_NGINX_CONF = ROOT / "infra/nginx/nginx.conf"
WEB_NGINX_CONF = ROOT / "frontend/apps/web/nginx.conf"
WEB_DOCKERFILE = ROOT / "frontend/Dockerfile"
BACKEND_DOCKERFILE = ROOT / "backend/Dockerfile"

EXPECTED_UPSTREAMS = {
    BACKEND_SERVICE: "http://backend-api:8000",
    REALTIME_SERVICE: "http://backend-realtime:8000",
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
    app_password = secrets.token_hex(16)
    worker_password = secrets.token_hex(16)
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
        "NOTRELIX_APP_DB_PASSWORD": app_password,
        "NOTRELIX_WORKER_DB_PASSWORD": worker_password,
        "CONNECTIONSTRINGS_NOTRELIXMIGRATIONDB": (
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


def connection_user(connection_string: str) -> str:
    for part in connection_string.split(";"):
        key, _, value = part.partition("=")
        if key.strip().lower() in ("username", "user id", "user"):
            return value.strip()
    return ""


def check_database_logins(label: str, backend_env: dict[str, str]) -> None:
    """Requests, background work and migrations use three different logins; enforcement is never relaxed."""
    expected = {
        "ConnectionStrings__NotrelixDb": "notrelix_app",
        "ConnectionStrings__NotrelixWorkerDb": "notrelix_worker",
    }
    for key, user in expected.items():
        actual = connection_user(backend_env.get(key, ""))
        if actual != user:
            fail(f"{label} backend {key} must connect as {user!r}, got {actual!r}")
    owner = connection_user(backend_env.get("ConnectionStrings__NotrelixMigrationDb", ""))
    if not owner or owner in expected.values():
        fail(f"{label} backend ConnectionStrings__NotrelixMigrationDb must connect as the database owner, got {owner!r}")
    if backend_env.get("Rls__RoleEnforcement") != "Enforce":
        fail(f"{label} backend Rls__RoleEnforcement must be 'Enforce', got {backend_env.get('Rls__RoleEnforcement')!r}")


def connection_endpoint(connection_string: str) -> tuple[str, str]:
    """Host and port of a key=value connection string (Npgsql keys, case-insensitive)."""
    host = port = ""
    for part in connection_string.split(";"):
        key, _, value = part.partition("=")
        name = key.strip().lower()
        if name in ("host", "server"):
            host = value.strip()
        elif name == "port":
            port = value.strip()
    return host, port


def check_backend_fleet(label: str, services: dict[str, Any], *, read_only_required: bool) -> None:
    """One image, three roles. Each role has its own network reach; every application login goes through pgbouncer."""
    images: set[str] = set()
    for name, role in BACKEND_ROLES.items():
        if name not in services:
            fail(f"{label} Compose is missing {name}")
        service = services[name]
        env = env_map(service)
        if env.get("Runtime__Roles") != role:
            fail(f"{label} {name} must set Runtime__Roles={role!r}, got {env.get('Runtime__Roles')!r}")
        images.add(str(service.get("image", "")))
        if read_only_required and service.get("read_only") is not True:
            fail(f"{label} {name} must be read_only")
        if "ALL" not in (service.get("cap_drop") or []):
            fail(f"{label} {name} must cap_drop ALL")
        if service.get("cap_add"):
            fail(f"{label} {name} must not add Linux capabilities")
        if "no-new-privileges:true" not in (service.get("security_opt") or []):
            fail(f"{label} {name} must enable no-new-privileges")

        nets = network_names(service)
        if "backend-network" not in nets:
            fail(f"{label} {name} must join backend-network")
        if "frontend-network" in nets:
            fail(f"{label} {name} must not use frontend-network")
        if name == REALTIME_SERVICE and "backend-egress-network" in nets:
            fail(f"{label} {name} must not have outbound egress (realtime needs no external integration)")
        if name != REALTIME_SERVICE and "backend-egress-network" not in nets:
            fail(f"{label} {name} must join backend-egress-network for approved integrations")

        for key, expected in {
            "Messaging__Transport": "RabbitMQ",
            "Messaging__RabbitMQ__Host": RABBITMQ_SERVICE,
            "Messaging__RabbitMQ__Port": "5672",
            "ForwardedHeaders__RequireKnownProxyInProduction": "true",
        }.items():
            if env.get(key) != expected:
                fail(f"{label} {name} environment {key} must be {expected!r}, got {env.get(key)!r}")
        for key in ("Messaging__RabbitMQ__Username", "Messaging__RabbitMQ__Password"):
            if not env.get(key):
                fail(f"{label} {name} environment {key} is required")

        depends = service.get("depends_on") or {}
        for dependency in (PGBOUNCER_SERVICE, RABBITMQ_SERVICE, REDIS_SERVICE):
            if dependency not in depends:
                fail(f"{label} {name} must depend on {dependency}")

        app_host, app_port = connection_endpoint(env.get("ConnectionStrings__NotrelixDb", ""))
        worker_host, worker_port = connection_endpoint(env.get("ConnectionStrings__NotrelixWorkerDb", ""))
        for key, host, port in (
            ("ConnectionStrings__NotrelixDb", app_host, app_port),
            ("ConnectionStrings__NotrelixWorkerDb", worker_host, worker_port),
        ):
            if host != PGBOUNCER_SERVICE or port != "6432":
                fail(f"{label} {name} {key} must connect through {PGBOUNCER_SERVICE}:6432, got {host}:{port}")
        migration_host, _ = connection_endpoint(env.get("ConnectionStrings__NotrelixMigrationDb", ""))
        if migration_host != POSTGRES_SERVICE:
            fail(f"{label} {name} ConnectionStrings__NotrelixMigrationDb must connect straight to {POSTGRES_SERVICE}")
        check_database_logins(f"{label} {name}", env)

        if env.get("DataProtection__PersistKeys") != "true":
            fail(f"{label} {name} must persist Data Protection keys so all roles share one key ring")
        if env.get("DataProtection__KeysPath") != DATA_PROTECTION_KEYS_PATH:
            fail(f"{label} {name} DataProtection__KeysPath must be {DATA_PROTECTION_KEYS_PATH!r}")
        targets = {str(mount.get("target")) for mount in service.get("volumes", []) or [] if isinstance(mount, dict)}
        if DATA_PROTECTION_KEYS_PATH not in targets:
            fail(f"{label} {name} must mount the shared Data Protection key volume at {DATA_PROTECTION_KEYS_PATH}")

    if len(images) != 1 or "" in images:
        fail(f"{label} backend roles must run one shared image, got {sorted(images)}")


def check_pgbouncer(label: str, services: dict[str, Any], *, read_only_required: bool) -> None:
    service = services[PGBOUNCER_SERVICE]
    if ports(service):
        fail(f"{label} pgbouncer must not publish host ports")
    if read_only_required and service.get("read_only") is not True:
        fail(f"{label} pgbouncer must be read_only")
    if "ALL" not in (service.get("cap_drop") or []):
        fail(f"{label} pgbouncer must cap_drop ALL")
    nets = network_names(service)
    if nets != {"backend-network"}:
        fail(f"{label} pgbouncer must join only backend-network, got {sorted(nets)}")
    if POSTGRES_SERVICE not in (service.get("depends_on") or {}):
        fail(f"{label} pgbouncer must depend on postgres")
    pooler_image = str(service.get("image", ""))
    if not pooler_image:
        fail(f"{label} pgbouncer must have a pinned image")


def check_gateway_routes(label: str, conf: str) -> None:
    """/api/v1/realtime goes to the realtime role; the rest of /api goes to the api role."""
    routes = {
        "/api/v1/realtime": REALTIME_SERVICE,
        "/api/": BACKEND_SERVICE,
    }
    for path, service in routes.items():
        match = re.search(rf"location\s+\^~\s+{re.escape(path)}\s*\{{(.*?)\}}", conf, re.DOTALL)
        if not match:
            fail(f"{label} nginx must route {path} explicitly")
        if f"http://{service}:8000" not in match.group(1):
            fail(f"{label} nginx {path} must proxy to {service}")
    realtime_block = re.search(r"location\s+\^~\s+/api/v1/realtime\s*\{(.*?)\}", conf, re.DOTALL)
    if realtime_block and "proxy_set_header Upgrade $http_upgrade;" not in realtime_block.group(1):
        fail(f"{label} nginx realtime route must forward the WebSocket upgrade")


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

    check_backend_fleet("production", services, read_only_required=True)
    check_pgbouncer("production", services, read_only_required=True)

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
    check_gateway_routes("production", prod_conf)

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
    check_backend_fleet("staging", staging_services, read_only_required=False)
    check_pgbouncer("staging", staging_services, read_only_required=True)

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
    check_gateway_routes("staging", staging_conf)

    print("Notrelix infrastructure semantics: OK")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())