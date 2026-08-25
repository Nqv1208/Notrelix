#!/usr/bin/env python3
from __future__ import annotations

import json
import os
import subprocess
import tempfile
import unittest
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[1]


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

    def test_environment_promotion_modes_match_orchestration(self) -> None:
        subprocess.run([
            "python3", str(HERE / "environment-info.py"),
            "--environment", "staging", "--require-promotion-mode", "automatic-after-main-ci",
        ], cwd=ROOT, check=True, stdout=subprocess.DEVNULL)
        subprocess.run([
            "python3", str(HERE / "environment-info.py"),
            "--environment", "production", "--require-promotion-mode", "manual-promotion",
        ], cwd=ROOT, check=True, stdout=subprocess.DEVNULL)


if __name__ == "__main__":
    unittest.main(verbosity=2)
