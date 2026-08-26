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
if __name__=='__main__':unittest.main()
