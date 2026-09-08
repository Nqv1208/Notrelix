import unittest
from tools.deliveryctl.planner import build_plan
from tools.deliveryctl.runtime import ROOT
class T(unittest.TestCase):
 def test_full(self):
  p=build_plan(root=ROOT,event_name='workflow_dispatch',ref='refs/heads/main',source_sha='a'*40,explicit_changed=[],force_full=True);self.assertTrue(p['matrices']['containers']);self.assertIn('@sha256:',p['renderer']['ref']);self.assertTrue(p['expected_proofs'])
 def test_main_release(self):
  p=build_plan(root=ROOT,event_name='push',ref='refs/heads/main',source_sha='b'*40,explicit_changed=['frontend/apps/web/src/a.ts']);self.assertTrue(p['release_candidate']);self.assertEqual(set(p['package_components']),{'backend-api','web','marketing'})
 def test_unknown(self):
  p=build_plan(root=ROOT,event_name='pull_request',ref='refs/pull/1/merge',source_sha='c'*40,explicit_changed=['new/unknown.file']);self.assertTrue(p['full_ci'])
 def _plan(self,*changed):
  return build_plan(root=ROOT,event_name='pull_request',ref='refs/pull/1/merge',source_sha='d'*40,explicit_changed=list(changed))
 def test_ui_story_selects_ui(self):
  p=self._plan('frontend/packages/product/work-management/web/src/components/views/kanban/kanban-board.stories.tsx')
  self.assertIn('ui',p['capabilities']);self.assertIn('ui:foundation',p['expected_proofs'])
 def test_ui_manifest_selects_ui(self):
  p=self._plan('frontend/packages/product/work-management/web/verification/ui-evidence.manifest.json')
  self.assertIn('ui',p['capabilities']);self.assertIn('ui:foundation',p['expected_proofs'])
 def test_ui_verification_source_selects_ui(self):
  p=self._plan('frontend/packages/features/auth/src/verification/auth-form-surfaces.tsx')
  self.assertIn('ui',p['capabilities'])
 def test_ui_tooling_selects_ui(self):
  p=self._plan('frontend/tooling/testing/src/pure-ui-network-guard.ts')
  self.assertIn('ui',p['capabilities']);self.assertIn('ui:foundation',p['expected_proofs'])
 def test_ui_e2e_selects_ui_without_backend(self):
  p=self._plan('frontend/e2e/ui/ui-manifest.a11y.spec.ts')
  self.assertIn('ui',p['capabilities']);self.assertIn('ui:foundation',p['expected_proofs']);self.assertEqual(p['matrices']['backend'],[])
 def test_backend_change_does_not_select_ui(self):
  p=self._plan('backend/contracts/openapi/v1/boards.yaml')
  self.assertNotIn('ui',p['capabilities'])
 def test_frontend_gate_requires_ui_foundation(self):
  text=(ROOT/'.github/workflows/frontend-ci.yml').read_text();block=text.split('  frontend-gate:',1)[1].split('    runs-on:',1)[0]
  self.assertIn('ui-foundation',block)
if __name__=='__main__':unittest.main()
