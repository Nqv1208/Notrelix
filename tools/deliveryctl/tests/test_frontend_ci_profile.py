import json
import unittest

from tools.deliveryctl.model import compact
from tools.deliveryctl.planner import FRONTEND_FULL_BASELINE, build_plan, github_outputs

FULL = FRONTEND_FULL_BASELINE


def _plan(paths, **kwargs):
  args = dict(event_name='pull_request', ref='refs/pull/131/merge', source_sha='M' * 40, base_sha='B' * 40, head_sha='H' * 40, explicit_changed=list(paths))
  args.update(kwargs)
  return build_plan(**args)


def _profile(paths, **kwargs):
  output = github_outputs(_plan(paths, **kwargs))
  return json.loads(output['frontend_profile_json'])


class FrontendFullBaselineProfileTests(unittest.TestCase):
  def test_profile_01_web_source_selects_full_baseline(self):
    profile = _profile(['frontend/apps/web/src/board.ts'])
    self.assertEqual(profile, FULL)

  def test_profile_02_marketing_source_selects_full_baseline(self):
    profile = _profile(['frontend/apps/marketing/src/page.tsx'])
    self.assertEqual(profile, FULL)

  def test_profile_03_mobile_source_selects_full_baseline(self):
    profile = _profile(['frontend/apps/mobile/src/app.tsx'])
    self.assertEqual(profile, FULL)

  def test_profile_04_shared_ui_mock_tooling_paths_select_full_baseline(self):
    for path in (
      'frontend/packages/core/src/workspace.ts',
      'frontend/packages/ui/src/button.tsx',
      'frontend/packages/dev/mock-backend/handlers.ts',
      'frontend/tooling/dependency-rules/src/rules.ts',
      'frontend/pnpm-lock.yaml',
    ):
      with self.subTest(path=path):
        self.assertEqual(_profile([path]), FULL)

  def test_profile_05_contract_change_and_forced_full_ci_select_full_baseline(self):
    self.assertEqual(_profile(['backend/contracts/openapi/v1/boards.yaml']), FULL)
    forced = _plan(['README.md'], force_full=True)
    self.assertEqual(github_outputs(forced)['frontend_profile_json'], compact(FULL))

  def test_profile_06_affected_metadata_cannot_reduce_profile(self):
    plan = _plan(['frontend/apps/web/src/board.ts'])
    self.assertEqual(plan['capabilities'], ['node-tests', 'web-tests'])
    self.assertEqual(plan['frontend_profile'], FULL)
    self.assertEqual(plan['frontend_profile']['workspace'], FULL['workspace'])
    self.assertEqual(plan['frontend_profile']['build'], FULL['build'])
    self.assertEqual(plan['frontend_profile']['host_runtime'], FULL['host_runtime'])

  def test_non_frontend_plan_has_no_profile(self):
    plan = _plan(['backend/src/Notrelix.Domain/Board.cs'])
    self.assertIsNone(plan['frontend_profile'])
    self.assertEqual(github_outputs(plan)['frontend_profile_json'], '')


if __name__ == '__main__':
  unittest.main()
