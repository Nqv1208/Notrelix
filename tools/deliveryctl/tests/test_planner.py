import unittest
from tools.deliveryctl.planner import build_plan
from tools.deliveryctl.runtime import ROOT
class T(unittest.TestCase):
 KW=dict(repository='Nqv1208/Notrelix',head_repository='Nqv1208/Notrelix',actor='testuser')
 def test_full(self):
  p=build_plan(root=ROOT,event_name='workflow_dispatch',source_sha='a'*40,explicit_changed=[],force_full=True,**self.KW);self.assertTrue(p['matrices']['containers']);self.assertIn('@sha256:',p['renderer']['ref']);self.assertTrue(p['expected_proofs'])
 def test_main_release(self):
  p=build_plan(root=ROOT,event_name='push',ref='refs/heads/main',source_sha='b'*40,explicit_changed=['frontend/apps/web/src/a.ts'],**self.KW);self.assertTrue(p['release_candidate']);self.assertEqual(p['release_execution_mode'],'release');self.assertEqual(p['publication_mode'],'release');self.assertEqual(set(p['package_components']),{'backend-api','web','marketing'})
 def test_unknown(self):
  p=build_plan(root=ROOT,event_name='pull_request',ref='refs/pull/1/merge',source_sha='c'*40,explicit_changed=['new/unknown.file'],**self.KW);self.assertTrue(p['full_ci'])
 def test_docs_pr_mode_none(self):
  p=build_plan(root=ROOT,event_name='pull_request',ref='refs/pull/1/merge',source_sha='d'*40,explicit_changed=['docs/guide.md'],**self.KW);self.assertEqual(p['release_execution_mode'],'none');self.assertFalse(p['release_rehearsal'])
 def test_releasable_pr_rehearsal(self):
  p=build_plan(root=ROOT,event_name='pull_request',ref='refs/pull/1/merge',source_sha='e'*40,explicit_changed=['backend/Dockerfile'],**self.KW);self.assertEqual(p['release_execution_mode'],'rehearsal');self.assertEqual(p['publication_mode'],'rehearsal');self.assertTrue(p['trusted_source']);self.assertTrue(p['rehearsal_publish_allowed']);self.assertEqual(p['rehearsal_blocked_reason'],'')
 def test_delivery_platform_pr_rehearsal(self):
  p=build_plan(root=ROOT,event_name='pull_request',ref='refs/pull/1/merge',source_sha='f'*40,explicit_changed=['tools/deliveryctl/planner.py'],**self.KW);self.assertEqual(p['release_execution_mode'],'rehearsal');self.assertTrue(p['rehearsal_required'])
 def test_fork_pr_trust_blocked(self):
  p=build_plan(root=ROOT,event_name='pull_request',ref='refs/pull/1/merge',source_sha='g'*40,explicit_changed=['backend/Dockerfile'],repository='Nqv1208/Notrelix',head_repository='attacker/Notrelix',actor='attacker');self.assertEqual(p['release_execution_mode'],'rehearsal');self.assertFalse(p['trusted_source']);self.assertFalse(p['rehearsal_publish_allowed']);self.assertEqual(p['publication_mode'],'none');self.assertEqual(p['rehearsal_blocked_reason'],'fork-pr-requires-trusted-source')
 def test_dependabot_pr_restricted(self):
  p=build_plan(root=ROOT,event_name='pull_request',ref='refs/pull/1/merge',source_sha='h'*40,explicit_changed=['backend/Dockerfile'],repository='Nqv1208/Notrelix',head_repository='Nqv1208/Notrelix',actor='dependabot[bot]');self.assertFalse(p['trusted_source']);self.assertFalse(p['rehearsal_publish_allowed']);self.assertEqual(p['rehearsal_blocked_reason'],'restricted-automation-pr-requires-trusted-source')
 def test_main_ci_only_push_none(self):
  p=build_plan(root=ROOT,event_name='push',ref='refs/heads/main',source_sha='i'*40,explicit_changed=['docs/guide.md'],**self.KW);self.assertEqual(p['release_execution_mode'],'none');self.assertFalse(p['release_candidate'])
 def test_develop_push_never_release(self):
  p=build_plan(root=ROOT,event_name='push',ref='refs/heads/develop',source_sha='j'*40,explicit_changed=['backend/Dockerfile'],**self.KW);self.assertFalse(p['release_candidate']);self.assertFalse(p['release_rehearsal'])
 def test_manual_auto_none(self):
  p=build_plan(root=ROOT,event_name='workflow_dispatch',source_sha='k'*40,explicit_changed=[],force_full=True,**self.KW);self.assertEqual(p['release_execution_mode'],'none')
 def test_manual_rehearsal(self):
  p=build_plan(root=ROOT,event_name='workflow_dispatch',source_sha='l'*40,explicit_changed=[],force_full=True,requested_release_mode='rehearsal',**self.KW);self.assertEqual(p['release_execution_mode'],'rehearsal');self.assertEqual(p['publication_mode'],'rehearsal')
 def test_rehearsal_on_push_rejected(self):
  with self.assertRaises(ValueError):build_plan(root=ROOT,event_name='push',ref='refs/heads/main',source_sha='m'*40,explicit_changed=['backend/Dockerfile'],requested_release_mode='rehearsal',**self.KW)
 def test_rehearsal_on_pr_rejected(self):
  with self.assertRaises(ValueError):build_plan(root=ROOT,event_name='pull_request',ref='refs/pull/1/merge',source_sha='n'*40,explicit_changed=['backend/Dockerfile'],requested_release_mode='rehearsal',**self.KW)
if __name__=='__main__':unittest.main()
