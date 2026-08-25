#!/usr/bin/env python3
from __future__ import annotations

import json
import shutil
import subprocess
import tempfile
import unittest
from pathlib import Path

HERE = Path(__file__).resolve().parent
REPO = HERE.parents[1]


def write_json(path: Path, data: dict) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(data, indent=2) + "\n", encoding="utf-8")


class PlanTests(unittest.TestCase):
    def setUp(self) -> None:
        self.tmp = tempfile.TemporaryDirectory()
        self.root = Path(self.tmp.name)
        shutil.copytree(REPO / "delivery", self.root / "delivery")
        (self.root / "scripts/ci").mkdir(parents=True)
        for name in ("delivery_model.py", "build-plan.py"):
            shutil.copy2(HERE / name, self.root / "scripts/ci" / name)
        write_json(self.root / "frontend/apps/web/package.json", {
            "name": "@notrelix/app-web", "dependencies": {"@notrelix/foundation-core": "workspace:*", "@notrelix/ui-kit": "workspace:*"}
        })
        write_json(self.root / "frontend/apps/marketing/package.json", {
            "name": "@notrelix/app-marketing", "dependencies": {"@notrelix/ui-kit": "workspace:*"}
        })
        write_json(self.root / "frontend/apps/mobile/package.json", {
            "name": "@notrelix/app-mobile", "dependencies": {"@notrelix/foundation-core": "workspace:*"}
        })
        write_json(self.root / "frontend/packages/foundation/core/package.json", {"name": "@notrelix/foundation-core"})
        write_json(self.root / "frontend/packages/ui/kit/package.json", {"name": "@notrelix/ui-kit"})
        write_json(self.root / "frontend/packages/dev/orphan/package.json", {"name": "@notrelix/orphan"})
        (self.root / "backend/src").mkdir(parents=True)
        (self.root / "artifacts/contracts").mkdir(parents=True)

    def tearDown(self) -> None:
        self.tmp.cleanup()

    def plan(self, *changed: str, event: str = "pull_request", ref: str = "refs/pull/1/merge", full: bool = False) -> dict:
        output = self.root / "artifacts/ci/execution-plan.json"
        cmd = ["python3", str(self.root / "scripts/ci/build-plan.py"), "--repo-root", str(self.root), "--event-name", event, "--ref", ref, "--output", str(output)]
        for path in changed:
            cmd += ["--changed-file", path]
        if full:
            cmd.append("--full")
        subprocess.run(cmd, check=True, stdout=subprocess.DEVNULL)
        return json.loads(output.read_text(encoding="utf-8"))

    def test_backend_change_does_not_run_frontend(self) -> None:
        p = self.plan("backend/src/Foo.cs")
        self.assertEqual(p["affected_components"], ["backend-api"])
        self.assertEqual(p["package_components"], ["backend-api"])
        self.assertNotIn("frontend:gate", p["expected_proofs"])
        self.assertIn("backend:backend-api", p["expected_proofs"])

    def test_web_change_does_not_run_backend_or_other_hosts(self) -> None:
        p = self.plan("frontend/apps/web/src/foo.tsx")
        self.assertEqual(p["affected_components"], ["web"])
        self.assertEqual(p["package_components"], ["web"])
        self.assertNotIn("backend:backend-api", p["expected_proofs"])

    def test_shared_foundation_routes_only_consumers(self) -> None:
        p = self.plan("frontend/packages/foundation/core/src/index.ts")
        self.assertEqual(p["affected_components"], ["mobile", "web"])
        self.assertEqual(p["package_components"], ["web"])

    def test_ui_package_routes_web_marketing_and_ui_proof(self) -> None:
        p = self.plan("frontend/packages/ui/kit/src/button.tsx")
        self.assertEqual(p["affected_components"], ["marketing", "web"])
        self.assertIn("ui", p["capabilities"])
        self.assertEqual(p["package_components"], ["marketing", "web"])

    def test_docs_under_backend_are_exclusive(self) -> None:
        p = self.plan("backend/docs/architecture.md")
        self.assertEqual(p["affected_components"], [])
        self.assertEqual(p["planes"], ["docs"])
        self.assertFalse(p["release_required"])

    def test_contract_fans_out_to_consumers(self) -> None:
        p = self.plan("backend/contracts/openapi/notrelix.v1.json")
        self.assertEqual(p["affected_components"], ["backend-api", "marketing", "mobile", "web"])
        self.assertTrue(p["release_required"])
        self.assertIn("contract", p["capabilities"])

    def test_web_dockerfile_is_known_surface(self) -> None:
        p = self.plan("frontend/Dockerfile")
        self.assertEqual(p["affected_components"], ["web"])
        self.assertEqual(p["package_components"], ["web"])
        self.assertEqual(p["planes"], ["infra"])

    def test_ci_control_plane_runs_full_proof_without_release(self) -> None:
        p = self.plan(".github/workflows/ci.yml")
        self.assertTrue(p["full_ci"])
        self.assertEqual(set(p["affected_components"]), {"backend-api", "web", "marketing", "mobile"})
        self.assertEqual(set(p["package_components"]), {"backend-api", "web", "marketing"})
        self.assertFalse(p["release_required"])

    def test_runtime_infra_requires_all_deployable_container_proof(self) -> None:
        p = self.plan("infra/nginx/nginx.conf")
        self.assertEqual(set(p["package_components"]), {"backend-api", "web", "marketing"})
        self.assertTrue(p["release_required"])
        self.assertIn("infra", p["planes"])

    def test_orphan_workspace_fails_safe_to_full_frontend(self) -> None:
        p = self.plan("frontend/packages/dev/orphan/index.ts")
        self.assertTrue(p["full_frontend"])
        self.assertEqual(set(p["affected_components"]), {"web", "marketing", "mobile"})
        self.assertTrue(p["warnings"])

    def test_unknown_repository_path_fails_safe_to_full_ci(self) -> None:
        p = self.plan("new-runtime/service.foo")
        self.assertTrue(p["full_ci"])
        self.assertEqual(set(p["affected_components"]), {"backend-api", "web", "marketing", "mobile"})

    def test_main_runtime_change_seals_complete_candidate(self) -> None:
        p = self.plan("frontend/apps/web/src/foo.tsx", event="push", ref="refs/heads/main")
        self.assertTrue(p["release_candidate"])
        self.assertEqual(set(p["package_components"]), {"backend-api", "web", "marketing"})
        self.assertIn("stack:release-candidate", p["expected_proofs"])

    def test_pr_runtime_change_is_not_release_candidate(self) -> None:
        p = self.plan("frontend/apps/web/src/foo.tsx")
        self.assertFalse(p["release_candidate"])
        self.assertEqual(p["package_components"], ["web"])

    def test_manual_full_ci_does_not_implicitly_release(self) -> None:
        p = self.plan(event="workflow_dispatch", ref="refs/heads/main", full=True)
        self.assertTrue(p["full_ci"])
        self.assertFalse(p["release_required"])
        self.assertFalse(p["release_candidate"])

    def test_expected_container_evidence_is_component_generic(self) -> None:
        p = self.plan("frontend/apps/marketing/app/page.tsx")
        self.assertIn("container:marketing", p["expected_proofs"])
        self.assertNotIn("container:web", p["expected_proofs"])

    def test_frontend_filters_are_catalog_workspaces(self) -> None:
        p = self.plan("frontend/packages/ui/kit/src/button.tsx")
        self.assertEqual(p["frontend_filters"], ["@notrelix/app-marketing", "@notrelix/app-web"])

    def test_unknown_push_range_is_full_ci(self) -> None:
        subprocess.run(["git", "init", "-q"], cwd=self.root, check=True)
        subprocess.run(["git", "config", "user.email", "ci@example.invalid"], cwd=self.root, check=True)
        subprocess.run(["git", "config", "user.name", "CI"], cwd=self.root, check=True)
        subprocess.run(["git", "add", "."], cwd=self.root, check=True)
        subprocess.run(["git", "commit", "-qm", "initial"], cwd=self.root, check=True)
        head = subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=self.root, text=True).strip()
        output = self.root / "artifacts/ci/push-plan.json"
        subprocess.run([
            "python3", str(self.root / "scripts/ci/build-plan.py"), "--repo-root", str(self.root),
            "--event-name", "push", "--head-sha", head, "--before-sha", "0" * 40,
            "--ref", "refs/heads/develop", "--output", str(output)
        ], check=True, stdout=subprocess.DEVNULL)
        p = json.loads(output.read_text())
        self.assertTrue(p["full_ci"])
        self.assertIn("unknown push range", p["reason"])

    def test_merge_base_ignores_base_branch_advance(self) -> None:
        subprocess.run(["git", "init", "-q"], cwd=self.root, check=True)
        subprocess.run(["git", "config", "user.email", "ci@example.invalid"], cwd=self.root, check=True)
        subprocess.run(["git", "config", "user.name", "CI"], cwd=self.root, check=True)
        (self.root / "README-seed.txt").write_text("seed")
        subprocess.run(["git", "add", "."], cwd=self.root, check=True)
        subprocess.run(["git", "commit", "-qm", "base"], cwd=self.root, check=True)
        base_common = subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=self.root, text=True).strip()
        subprocess.run(["git", "branch", "feature"], cwd=self.root, check=True)
        # Advance base with an unrelated backend change.
        (self.root / "backend/src/base-only.cs").write_text("base")
        subprocess.run(["git", "add", "."], cwd=self.root, check=True)
        subprocess.run(["git", "commit", "-qm", "base backend change"], cwd=self.root, check=True)
        base_tip = subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=self.root, text=True).strip()
        # Feature is still based on common ancestor and changes only web.
        subprocess.run(["git", "checkout", "-q", "feature"], cwd=self.root, check=True)
        target = self.root / "frontend/apps/web/src/feature.ts"
        target.parent.mkdir(parents=True, exist_ok=True); target.write_text("feature")
        subprocess.run(["git", "add", "."], cwd=self.root, check=True)
        subprocess.run(["git", "commit", "-qm", "feature web"], cwd=self.root, check=True)
        head = subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=self.root, text=True).strip()
        output = self.root / "artifacts/ci/merge-base-plan.json"
        subprocess.run([
            "python3", str(self.root / "scripts/ci/build-plan.py"), "--repo-root", str(self.root),
            "--event-name", "pull_request", "--base-sha", base_tip, "--head-sha", head,
            "--ref", "refs/pull/1/merge", "--output", str(output)
        ], check=True, stdout=subprocess.DEVNULL)
        p = json.loads(output.read_text())
        self.assertEqual(p["affected_components"], ["web"])
        self.assertNotIn("backend-api", p["affected_components"])
        self.assertTrue(p["reason"].startswith("merge-base:"))
        self.assertEqual(subprocess.check_output(["git", "merge-base", base_tip, head], cwd=self.root, text=True).strip(), base_common)


    def test_component_proof_profile_is_runtime_authority(self) -> None:
        policy = self.root / "delivery/policy.toml"
        text = policy.read_text(encoding="utf-8")
        text = text.replace('required = ["backend:{component_id}"]', 'required = ["custom-backend:{component_id}"]', 1)
        policy.write_text(text, encoding="utf-8")
        p = self.plan("backend/src/Foo.cs")
        self.assertIn("custom-backend:backend-api", p["expected_proofs"])
        self.assertNotIn("backend:backend-api", p["expected_proofs"])

    def test_migration_path_marks_schema_change(self) -> None:
        p = self.plan("backend/src/Notrelix.Infrastructure/Data/Migrations/20260825_AddThing.cs")
        self.assertTrue(p["schema_change"])
        self.assertEqual(p["rollback_after_schema_change"], "manual")

    def test_normal_backend_change_is_not_schema_change(self) -> None:
        p = self.plan("backend/src/Notrelix.Application/Foo.cs")
        self.assertFalse(p["schema_change"])


if __name__ == "__main__":
    unittest.main(verbosity=2)
