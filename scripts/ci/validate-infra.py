#!/usr/bin/env python3
"""Semantic validation for the Notrelix Compose production topology.

Execution-plane helper only: it consumes planner-resolved JSON contracts and never
reads delivery/*.toml or imports deliveryctl.
"""
from __future__ import annotations

import argparse
import json
import os
import re
import secrets
import subprocess
import tempfile
from pathlib import Path
from typing import Any

ROOT = Path.cwd()
ZERO = "0" * 64
MIN_COMPOSE_VERSION = (2, 24, 4)


def fail(msg: str) -> None:
    raise SystemExit(f"infra validation failed: {msg}")


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--runtime-images", type=Path, required=True)
    parser.add_argument("--containers", type=Path, required=True)
    return parser.parse_args()


def load_contract(path: Path, kind: str) -> dict[str, Any]:
    data = json.loads(path.read_text(encoding="utf-8"))
    if data.get("api_version") != "delivery.notrelix.dev/v1" or data.get("kind") != kind:
        fail(f"{path} is not a {kind}")
    return data


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


def runtime_by_id(runtime: dict[str, Any]) -> dict[str, dict[str, Any]]:
    result: dict[str, dict[str, Any]] = {}
    for item in runtime.get("images", []):
        image_id = str(item.get("id", ""))
        ref = str(item.get("ref", ""))
        service = str(item.get("compose_service", ""))
        env_var = str(item.get("deploy_env_var", ""))
        if not image_id or not service or not env_var or "@sha256:" not in ref:
            fail(f"invalid resolved runtime image contract: {item!r}")
        if image_id in result:
            fail(f"duplicate runtime image id: {image_id}")
        result[image_id] = item
    for required in ("postgres", "redis", "rabbitmq", "nginx"):
        if required not in result:
            fail(f"runtime image set missing {required}")
    return result


def container_sets(containers: dict[str, Any]) -> tuple[dict[str, Any], list[dict[str, Any]]]:
    items = containers.get("containers", [])
    if not isinstance(items, list) or not items:
        fail("deployable container set is empty")
    backend = [x for x in items if x.get("provider") == "backend"]
    hosts = [x for x in items if x.get("provider") == "frontend-host"]
    if len(backend) != 1:
        fail(f"Compose adapter currently requires exactly one backend provider, got {len(backend)}")
    if not hosts:
        fail("Compose adapter requires at least one frontend-host deployable")
    services: set[str] = set()
    envs: set[str] = set()
    for item in items:
        for key in ("component_id", "compose_service", "deploy_env_var", "image_name", "runtime_port", "health_path"):
            if key not in item:
                fail(f"container contract missing {key}: {item!r}")
        service = str(item["compose_service"])
        env_var = str(item["deploy_env_var"])
        if service in services:
            fail(f"duplicate application compose service {service}")
        if env_var in envs:
            fail(f"duplicate application deploy env var {env_var}")
        services.add(service)
        envs.add(env_var)
    return backend[0], hosts


def ephemeral_environment(
    runtime: dict[str, dict[str, Any]],
    containers: dict[str, Any],
) -> dict[str, str]:
    postgres_password = secrets.token_hex(16)
    redis_password = secrets.token_hex(16)
    rabbitmq_password = secrets.token_hex(16)
    env = {
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
        "CONNECTIONSTRINGS_NOTRELIXDB": (
            f"Host={runtime['postgres']['compose_service']};Port=5432;Database=notrelix;"
            f"Username=postgres;Password={postgres_password}"
        ),
        "CONNECTIONSTRINGS_REDIS": (
            f"{runtime['redis']['compose_service']}:6379,password={redis_password}"
        ),
        "BACKEND_NETWORK_SUBNET": "172.28.0.0/24",
        "HTTPS_REDIRECTION_ENABLED": "false",
        "HTTP_PORT": "8080",
    }
    for item in runtime.values():
        env[str(item["deploy_env_var"])] = str(item["ref"])
    for item in containers.get("containers", []):
        env[str(item["deploy_env_var"])] = (
            f"ghcr.io/example/{item['image_name']}@sha256:{ZERO}"
        )
    return env


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


def render_release_overlay(
    path: Path,
    runtime: dict[str, dict[str, Any]],
    containers: dict[str, Any],
    env: dict[str, str],
) -> dict[str, str]:
    expected: dict[str, str] = {}
    lines = ["services:"]
    for item in runtime.values():
        service = str(item["compose_service"])
        ref = str(item["ref"])
        expected[service] = ref
        lines += [f"  {service}:", f"    image: {ref}"]
    for item in containers.get("containers", []):
        service = str(item["compose_service"])
        ref = env[str(item["deploy_env_var"])]
        expected[service] = ref
        lines += [
            f"  {service}:",
            f"    image: {ref}",
            "    build: !reset null",
        ]
    path.write_text("\n".join(lines) + "\n", encoding="utf-8")
    return expected


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
    args = parse_args()
    version = compose_version()
    if version < MIN_COMPOSE_VERSION:
        fail(
            f"docker compose {version} is too old; release rendering requires >= "
            f"{'.'.join(map(str, MIN_COMPOSE_VERSION))} for !reset merge semantics"
        )

    runtime_contract = load_contract(args.runtime_images, "RuntimeImageSet")
    container_contract = load_contract(args.containers, "DeployableContainerSet")
    runtime = runtime_by_id(runtime_contract)
    backend_contract, host_contracts = container_sets(container_contract)

    runtime_services = {str(x["compose_service"]) for x in runtime.values()}
    app_services = {str(x["compose_service"]) for x in container_contract["containers"]}
    if runtime_services & app_services:
        fail(f"runtime/application compose service collision: {sorted(runtime_services & app_services)}")

    env = ephemeral_environment(runtime, container_contract)
    with tempfile.TemporaryDirectory(prefix="notrelix-infra-") as temp:
        generated = Path(temp) / "release.generated.yml"
        expected_images = render_release_overlay(generated, runtime, container_contract, env)
        prod = compose([ROOT / "docker-compose.yml", ROOT / "docker-compose.prod.yml"], env)
        staging = compose([ROOT / "docker-compose.yml", ROOT / "docker-compose.staging.yml"], env)
        release = compose(
            [ROOT / "docker-compose.yml", ROOT / "docker-compose.prod.yml", generated],
            env,
        )

    services = prod["services"]
    networks = prod.get("networks", {})
    backend_service = str(backend_contract["compose_service"])
    postgres_service = str(runtime["postgres"]["compose_service"])
    redis_service = str(runtime["redis"]["compose_service"])
    rabbitmq_service = str(runtime["rabbitmq"]["compose_service"])
    nginx_service = str(runtime["nginx"]["compose_service"])

    all_required = runtime_services | app_services
    missing = sorted(all_required - set(services))
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

    for name in (postgres_service, redis_service, rabbitmq_service):
        if ports(services[name]):
            fail(f"{name} must not publish host ports")
    if services[rabbitmq_service].get("profiles"):
        fail("RabbitMQ must not be optional/profile-gated in staging/production")

    backend = services[backend_service]
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
        "Messaging__RabbitMQ__Host": rabbitmq_service,
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
    if rabbitmq_service not in (backend.get("depends_on") or {}):
        fail("backend must depend on healthy RabbitMQ")

    nginx = services[nginx_service]
    if ports(nginx) != {8080}:
        fail(f"production nginx must publish container port 8080 only, got {sorted(ports(nginx))}")
    if str(nginx.get("user", "")) != "nginx":
        fail("production nginx must run as nginx user")
    if "ALL" not in (nginx.get("cap_drop") or []):
        fail("production nginx must cap_drop ALL")
    if nginx.get("cap_add"):
        fail("production nginx must not add Linux capabilities")

    prod_conf = (ROOT / "infra/nginx/nginx.prod.conf").read_text(encoding="utf-8")
    if re.search(r"\blisten\s+443(?:\s|;)", prod_conf):
        fail("production origin nginx must not terminate TLS")
    if not re.search(r"\blisten\s+8080;", prod_conf):
        fail("production nginx must listen on 8080")
    if "proxy_set_header X-Forwarded-Proto https;" not in prod_conf:
        fail("production gateway must forward canonical HTTPS scheme")
    if "proxy_set_header X-Forwarded-Port 443;" not in prod_conf:
        fail("production gateway must forward public port 443")

    expected_upstreams = {
        backend_service: f"http://{backend_service}:{int(backend_contract['runtime_port'])}",
        **{
            str(item["compose_service"]): f"http://{item['compose_service']}:{int(item['runtime_port'])}"
            for item in host_contracts
        },
    }
    for service, upstream in expected_upstreams.items():
        if upstream not in prod_conf:
            fail(f"production nginx missing upstream {upstream} for {service}")

    # Preserve current rootless web/container invariants. These are provider-specific
    # execution checks, not delivery authority reads.
    web_dockerfile = (ROOT / "frontend/Dockerfile").read_text(encoding="utf-8")
    web_conf = (ROOT / "frontend/apps/web/nginx.conf").read_text(encoding="utf-8")
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

    backend_dockerfile = (ROOT / str(backend_contract["dockerfile"])).read_text(encoding="utf-8")
    if "--locked-mode ||" in backend_dockerfile:
        fail("backend Dockerfile must not use fail-open restore fallback")
    if not re.search(r"^USER\s+notrelix\s*$", backend_dockerfile, re.MULTILINE):
        fail("backend image must run as notrelix user")

    sensitive = ("PASSWORD", "SECRET", "APIKEY", "API_KEY", "CONNECTIONSTRING", "TOKEN")
    public_services = [str(x["compose_service"]) for x in host_contracts] + [nginx_service]
    for service_name in public_services:
        for key in env_map(services[service_name]):
            if any(marker in key.upper() for marker in sensitive):
                fail(f"public service {service_name} receives sensitive env key {key}")

    staging_services = staging.get("services", {})
    missing_staging = sorted(all_required - set(staging_services))
    if missing_staging:
        fail(f"staging missing services: {missing_staging}")
    staging_backend = staging_services[backend_service]
    if rabbitmq_service not in (staging_backend.get("depends_on") or {}):
        fail("staging backend must depend on RabbitMQ")
    if env_map(staging_backend).get("Messaging__RabbitMQ__Host") != rabbitmq_service:
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

    staging_nginx = staging_services[nginx_service]
    if str(staging_nginx.get("user", "")) != "nginx":
        fail("staging nginx must run as nginx user")
    if staging_nginx.get("read_only") is not True:
        fail("staging nginx must be read_only")
    if ports(staging_nginx) != {8080}:
        fail("staging nginx must publish only container port 8080")
    staging_conf = (ROOT / "infra/nginx/nginx.conf").read_text(encoding="utf-8")
    if not re.search(r"^pid\s+/tmp/nginx\.pid;", staging_conf, re.MULTILINE):
        fail("staging nginx pid must live under /tmp")
    if not re.search(r"\blisten\s+8080;", staging_conf):
        fail("staging nginx must listen on 8080")

    release_services = release["services"]
    for service, expected in expected_images.items():
        actual = str(release_services.get(service, {}).get("image", ""))
        if actual != expected or "@sha256:" not in actual:
            fail(f"release image mismatch/non-immutable for {service}: {actual!r}")
    for item in container_contract["containers"]:
        service = str(item["compose_service"])
        if "build" in release_services[service]:
            fail(f"release service {service} still declares build after !reset merge")

    print("Notrelix infrastructure semantics: OK")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
