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


def _execution(paths, **kwargs):
  plan = _plan(paths, **kwargs)
  hosts = [c['component_id'] for c in plan['matrices']['frontend_hosts']]
  mobiles = [c['component_id'] for c in plan['matrices']['mobile']]
  return plan, hosts, mobiles


def _assert_full_baseline_execution(paths, **kwargs):
  plan, hosts, mobiles = _execution(paths, **kwargs)
  assert hosts == FULL['host_runtime'], f'{paths}: generated host runtime {hosts} != profile {FULL["host_runtime"]}'
  assert mobiles == [cid for cid in FULL['build'] if cid not in FULL['host_runtime']], f'{paths}: generated mobile {mobiles}'
  assert sorted(hosts + mobiles) == sorted(FULL['build']), f'{paths}: generated build {sorted(hosts + mobiles)} != profile {sorted(FULL["build"])}'
  assert plan['frontend_profile'] == FULL, f'{paths}: profile drift'
  assert 'frontend:gate' in plan['ci_expected_proofs'], f'{paths}: frontend:gate not expected'
  assert 'ui:foundation' in plan['ci_expected_proofs'], f'{paths}: ui:foundation not expected'
  return plan


class FrontendFullBaselineProfileTests(unittest.TestCase):
  def test_profile_01_web_source_selects_full_baseline(self):
    self.assertEqual(_profile(['frontend/apps/web/src/board.ts']), FULL)

  def test_profile_02_marketing_source_selects_full_baseline(self):
    self.assertEqual(_profile(['frontend/apps/marketing/src/page.tsx']), FULL)

  def test_profile_03_mobile_source_selects_full_baseline(self):
    self.assertEqual(_profile(['frontend/apps/mobile/src/app.tsx']), FULL)

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
    self.assertEqual(plan['affected_components'], ['web'])
    self.assertEqual(plan['frontend_profile'], FULL)

  def test_non_frontend_plan_has_no_profile(self):
    plan = _plan(['backend/src/Notrelix.Domain/Board.cs'])
    self.assertIsNone(plan['frontend_profile'])
    self.assertEqual(github_outputs(plan)['frontend_profile_json'], '')
    self.assertEqual(plan['matrices']['frontend_hosts'], [])
    self.assertEqual(plan['matrices']['mobile'], [])
    self.assertNotIn('frontend:gate', plan['ci_expected_proofs'])
    self.assertNotIn('ui:foundation', plan['ci_expected_proofs'])


class FrontendFullBaselineExecutionClosureTests(unittest.TestCase):
  """FE-PROFILE-01..08: the profile is an executable contract —
  generated build/host obligations and expected proofs must equal the profile."""

  def test_fe_profile_01_web_only_executes_full_baseline(self):
    _assert_full_baseline_execution(['frontend/apps/web/src/board.ts'])

  def test_fe_profile_02_marketing_only_executes_full_baseline(self):
    _assert_full_baseline_execution(['frontend/apps/marketing/src/page.tsx'])

  def test_fe_profile_03_mobile_only_executes_full_baseline(self):
    _assert_full_baseline_execution(['frontend/apps/mobile/src/app.tsx'])

  def test_fe_profile_04_shared_frontend_package_executes_full_baseline(self):
    _assert_full_baseline_execution(['frontend/packages/core/src/workspace.ts'])

  def test_fe_profile_05_frontend_tooling_executes_full_baseline(self):
    _assert_full_baseline_execution(['frontend/tooling/dependency-rules/src/rules.ts'])

  def test_fe_profile_06_backend_contract_executes_full_baseline(self):
    _assert_full_baseline_execution(['backend/contracts/openapi/v1/boards.yaml'])

  def test_fe_profile_07_capability_only_selection_executes_full_baseline(self):
    plan = _assert_full_baseline_execution(['frontend/e2e/ui/accessibility.spec.ts'])
    self.assertEqual(plan['affected_components'], [])
    self.assertIn('ui', plan['capabilities'])

  def test_fe_profile_08_deterministic_plan_hash(self):
    paths = ['frontend/apps/web/src/board.ts']
    first = _plan(paths)
    second = _plan(paths)
    self.assertEqual(first['plan_sha256'], second['plan_sha256'])
    self.assertEqual(first['ci_expected_proofs'], second['ci_expected_proofs'])
    self.assertEqual(first['matrices'], second['matrices'])


if __name__ == '__main__':
  unittest.main()
