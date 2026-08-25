#!/usr/bin/env python3
from __future__ import annotations

import importlib.util
import json
import os
import secrets
import subprocess
import tempfile
import unittest
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[1]

# Import image-info.py (hyphenated name requires importlib).
_spec = importlib.util.spec_from_file_location("image_info", str(HERE / "image-info.py"))
assert _spec is not None and _spec.loader is not None
_image_info = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(_image_info)


def ephemeral_compose_env() -> dict[str, str]:
    """Runtime-generated non-production values for Compose contract checks."""
    return {
        "POSTGRES_USER": "postgres",
        "POSTGRES_PASSWORD": secrets.token_hex(16),
        "POSTGRES_DB": "contract_check",
        "REDIS_PASSWORD": secrets.token_hex(16),
        "RABBITMQ_USER": "rabbitmq",
        "RABBITMQ_PASSWORD": secrets.token_hex(16),
        "RABBITMQ_VHOST": "/",
        "JWT_SECRET": secrets.token_hex(32),
        "JWT_ISSUER": "http://localhost",
        "JWT_AUDIENCE": "http://localhost",
        "CORS_ORIGIN": "http://localhost",
        "RESEND_API_KEY": secrets.token_hex(16),
        "CONNECTIONSTRINGS_NOTRELIXDB": f"Host=postgres;Port=5432;Database=contract_check;Username=postgres;Password={secrets.token_hex(16)}",
        "CONNECTIONSTRINGS_REDIS": f"redis:6379,password={secrets.token_hex(16)}",
        "BACKEND_NETWORK_SUBNET": "172.28.0.0/24",
        "HTTPS_REDIRECTION_ENABLED": "false",
        "HTTP_PORT": "8080",
    }


class DeliveryContractTests(unittest.TestCase):
    def test_unexpected_evidence_fails_exact_gate(self) -> None:
        with tempfile.TemporaryDirectory() as td:
            tmp = Path(td)
            plan = tmp / "plan.json"
            evidence = tmp / "evidence"
            output = tmp / "summary.json"
            evidence.mkdir()
            plan.write_text(json.dumps({"expected_proofs": ["proof:a"], "plan_sha256": "x"}), encoding="utf-8")
            base = {"source_sha": "local", "run_id": "local", "status": "passed"}
            (evidence / "a.json").write_text(json.dumps({**base, "proof_id": "proof:a"}), encoding="utf-8")
            (evidence / "extra.json").write_text(json.dumps({**base, "proof_id": "proof:extra"}), encoding="utf-8")
            proc = subprocess.run([
                "python3", str(HERE / "aggregate-evidence.py"),
                "--plan", str(plan), "--evidence-dir", str(evidence), "--output", str(output),
            ], cwd=ROOT, text=True, capture_output=True)
            self.assertNotEqual(proc.returncode, 0)
            summary = json.loads(output.read_text(encoding="utf-8"))
            self.assertEqual(summary["unexpected_proofs"], ["proof:extra"])
            self.assertEqual(summary["status"], "failed")

    def test_deployment_bundle_carries_schema_rollback_policy(self) -> None:
        with tempfile.TemporaryDirectory() as td:
            tmp = Path(td)
            manifest = tmp / "manifest.json"
            out = tmp / "bundle"
            manifest.write_text(json.dumps({
                "schema_version": 4,
                "source_sha": "a" * 40,
                "schema_change": True,
                "rollback_after_schema_change": "manual",
                "images": [{
                    "id": "backend-api",
                    "kind": "application",
                    "ref": "ghcr.io/example/backend@sha256:" + "1" * 64,
                    "deploy_env_var": "NOTRELIX_BACKEND_IMAGE",
                    "compose_service": "backend",
                    "stateful": False,
                }],
            }), encoding="utf-8")
            subprocess.run([
                "python3", str(HERE / "prepare-deployment-bundle.py"),
                "--manifest", str(manifest), "--environment", "staging", "--output-dir", str(out),
            ], cwd=ROOT, check=True, stdout=subprocess.DEVNULL)
            metadata = dict(
                line.split("=", 1)
                for line in (out / "metadata.env").read_text(encoding="utf-8").splitlines()
                if line
            )
            self.assertEqual(metadata["SCHEMA_CHANGE"], "true")
            self.assertEqual(metadata["RUN_MIGRATIONS"], "true")
            self.assertEqual(metadata["ROLLBACK_AFTER_SCHEMA_CHANGE"], "manual")
            self.assertEqual(metadata["STATEFUL_IMAGE_CHANGE_POLICY"], "explicit-override")
            self.assertEqual(metadata["PROMOTION_MODE"], "automatic-after-main-ci")

    def test_deployment_bundle_rejects_rollback_policy_drift(self) -> None:
        with tempfile.TemporaryDirectory() as td:
            tmp = Path(td)
            manifest = tmp / "manifest.json"
            manifest.write_text(json.dumps({
                "schema_version": 4,
                "source_sha": "a" * 40,
                "schema_change": True,
                "rollback_after_schema_change": "automatic",
                "images": [{
                    "id": "backend-api",
                    "kind": "application",
                    "ref": "ghcr.io/example/backend@sha256:" + "1" * 64,
                    "deploy_env_var": "NOTRELIX_BACKEND_IMAGE",
                    "compose_service": "backend",
                    "stateful": False,
                }],
            }), encoding="utf-8")
            proc = subprocess.run([
                "python3", str(HERE / "prepare-deployment-bundle.py"),
                "--manifest", str(manifest), "--environment", "staging", "--output-dir", str(tmp / "out"),
            ], cwd=ROOT, text=True, capture_output=True)
            self.assertNotEqual(proc.returncode, 0)
            self.assertIn("rollback policy drift", proc.stderr + proc.stdout)

    def test_release_overlay_removes_build_definitions_after_compose_merge(self) -> None:
        """Regression: application services must have no usable build path after merge.

        A plain `build: null` override survives Compose merging; only the
        `!reset null` tag deletes the inherited key. This test fails if any
        deployable application service still declares a build definition once
        base + prod + release overlay are merged.
        """
        with tempfile.TemporaryDirectory() as td:
            tmp = Path(td)
            manifest = tmp / "application-manifest.json"
            digest = "a" * 64
            manifest.write_text(json.dumps({
                "schema_version": 4,
                "artifacts": [
                    {"component_id": "backend-api", "image": f"ghcr.io/example/backend@sha256:{digest}"},
                    {"component_id": "web", "image": f"ghcr.io/example/web@sha256:{digest}"},
                    {"component_id": "marketing", "image": f"ghcr.io/example/marketing@sha256:{digest}"},
                ],
            }), encoding="utf-8")
            overlay = tmp / "release.generated.yml"
            subprocess.run([
                "python3", str(HERE / "render-release-compose.py"),
                "--application-manifest", str(manifest), "--output", str(overlay),
            ], cwd=ROOT, check=True, stdout=subprocess.DEVNULL)
            merged = subprocess.run(
                [
                    "docker", "compose",
                    "-f", "docker-compose.yml",
                    "-f", "docker-compose.prod.yml",
                    "-f", str(overlay),
                    "config", "--format", "json",
                ],
                cwd=ROOT, text=True, capture_output=True, check=True,
                env={**os.environ, **ephemeral_compose_env()},
            ).stdout
            services = json.loads(merged)["services"]
            for service in ("backend", "frontend-web", "frontend-marketing"):
                self.assertNotIn(
                    "build", services[service],
                    f"{service}: build definition survived Compose merge; "
                    "release overlay must use !reset semantics",
                )
                self.assertIn("@sha256:", services[service]["image"])

    def test_environment_promotion_modes_match_orchestration(self) -> None:
        subprocess.run([
            "python3", str(HERE / "environment-info.py"),
            "--environment", "staging", "--require-promotion-mode", "automatic-after-main-ci",
        ], cwd=ROOT, check=True, stdout=subprocess.DEVNULL)
        subprocess.run([
            "python3", str(HERE / "environment-info.py"),
            "--environment", "production", "--require-promotion-mode", "manual-promotion",
        ], cwd=ROOT, check=True, stdout=subprocess.DEVNULL)


class RendererParserTests(unittest.TestCase):
    """Regression tests for renderer version/variant extraction."""

    def test_parse_playwright_tag_jammy(self) -> None:
        fields = _image_info._derive_tag_fields("mcr.microsoft.com/playwright:v1.61.1-jammy")
        self.assertEqual(fields["version"], "1.61.1")
        self.assertEqual(fields["variant"], "jammy")

    def test_parse_playwright_tag_noble(self) -> None:
        fields = _image_info._derive_tag_fields("mcr.microsoft.com/playwright:v1.61.1-noble")
        self.assertEqual(fields["version"], "1.61.1")
        self.assertEqual(fields["variant"], "noble")

    def test_parse_plain_version_no_variant(self) -> None:
        fields = _image_info._derive_tag_fields("ghcr.io/example/image:v2.0.0")
        self.assertEqual(fields["version"], "2.0.0")
        self.assertNotIn("variant", fields)

    def test_parse_non_version_tag_yields_empty(self) -> None:
        fields = _image_info._derive_tag_fields("nginx:alpine")
        self.assertEqual(fields, {})

    def test_production_lock_playwright_version_is_parseable(self) -> None:
        import tomllib
        lock = tomllib.loads((ROOT / "delivery" / "images.lock.toml").read_text())
        source = lock["images"]["playwright-ci"]["source"]
        fields = _image_info._derive_tag_fields(source)
        self.assertIn("version", fields, f"playwright-ci source {source!r} has no parseable version")
        self.assertEqual(fields["version"], "1.61.1")


class RuntimeAuthorityTests(unittest.TestCase):
    """Ensure .python-version and delivery_model.MIN_PYTHON are consistent."""

    def test_python_version_at_least_3_11(self) -> None:
        raw = (ROOT / ".python-version").read_text().strip()
        parts = [int(x) for x in raw.split(".")[:3]]
        self.assertGreaterEqual(tuple(parts[:2]), (3, 11), f".python-version {raw!r} below 3.11")

    def test_delivery_model_min_python_matches(self) -> None:
        from delivery_model import MIN_PYTHON
        self.assertGreaterEqual(MIN_PYTHON, (3, 11))


if __name__ == "__main__":
    unittest.main(verbosity=2)
