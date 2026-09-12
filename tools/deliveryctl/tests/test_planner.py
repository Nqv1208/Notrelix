import unittest,subprocess,tempfile
from pathlib import Path
from tools.deliveryctl.planner import build_plan,resolve_range
from tools.deliveryctl.runtime import ROOT
def _make_repo(tmp):
 def git(*args):
  r=subprocess.run(['git']+list(args),cwd=tmp,stdout=subprocess.PIPE,stderr=subprocess.PIPE,text=True)
  assert r.returncode==0,(r.stdout,r.stderr);return r.stdout.strip()
 git('init','-q','-b','main');git('config','user.email','a@b.c');git('config','user.name','t');git('commit','--allow-empty','-q','-m','root')
 return git
class T(unittest.TestCase):
 def test_full(self):
  p=build_plan(root=ROOT,event_name='workflow_dispatch',ref='refs/heads/main',source_sha='a'*40,explicit_changed=[],force_full=True);self.assertTrue(p['matrices']['containers']);self.assertIn('@sha256:',p['renderer']['ref']);self.assertTrue(p['expected_proofs']);self.assertEqual(p['infra_modes'],['assembled','topology']);self.assertTrue(p['delivery_platform_required']);self.assertIn('delivery:platform',p['ci_expected_proofs']);self.assertIn('docs:gate',p['ci_expected_proofs']);self.assertIn('infra:gate',p['ci_expected_proofs'])
 def test_main_release(self):
  p=build_plan(root=ROOT,event_name='push',ref='refs/heads/main',source_sha='b'*40,explicit_changed=['frontend/apps/web/src/a.ts']);self.assertTrue(p['release_candidate']);self.assertEqual(set(p['package_components']),{'backend-api','web','marketing'})
 def test_unknown(self):
  p=build_plan(root=ROOT,event_name='pull_request',ref='refs/pull/1/merge',source_sha='c'*40,explicit_changed=['new/unknown.file']);self.assertTrue(p['full_ci']);self.assertTrue(p['warnings']);self.assertIn('delivery:platform',p['ci_expected_proofs']);self.assertIn('docs:gate',p['ci_expected_proofs']);self.assertIn('infra:gate',p['ci_expected_proofs'])
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
 def test_images_lock_change_requires_runtime_image_security(self):
  p=self._plan('delivery/images.lock.toml')
  self.assertIn('runtime',p['security_domains']);self.assertIn('security:runtime',p['ci_expected_proofs'])
 def test_runtime_image_security_decision_output(self):
  from tools.deliveryctl.planner import github_outputs
  o=github_outputs(self._plan('delivery/images.lock.toml'))
  self.assertEqual(o['security_runtime_required'],'true')
 def test_backend_change_does_not_scan_runtime_images(self):
  p=self._plan('backend/src/Notrelix.Domain/Board.cs')
  self.assertNotIn('runtime',p['security_domains']);self.assertNotIn('security:runtime',p['ci_expected_proofs'])
 def test_full_ci_scans_runtime_images(self):
  p=build_plan(root=ROOT,event_name='workflow_dispatch',ref='refs/heads/main',source_sha='a'*40,explicit_changed=[],force_full=True)
  self.assertIn('runtime',p['security_domains']);self.assertIn('security:runtime',p['ci_expected_proofs'])
 def test_security_provider_owns_runtime_image_scans(self):
  sec=(ROOT/'.github/workflows/security-ci.yml').read_text()
  self.assertIn('security:runtime',sec);self.assertIn('scan_runtime',sec);self.assertNotIn('delivery/',sec)
  ci=(ROOT/'.github/workflows/ci.yml').read_text();self.assertIn('runtime_images_json',ci)
  cidef=(ROOT/'.github/workflows/ci-definition.yml').read_text();self.assertNotIn('trivy',cidef.lower())
 def test_frontend_gate_requires_ui_system(self):
  text=(ROOT/'.github/workflows/frontend-ci.yml').read_text();block=text.split('  frontend-gate:',1)[1].split('    runs-on:',1)[0]
  self.assertIn('ui-system',block)
  self.assertIn('mock-system',block)
 def test_change_range_explicit(self):
  p=build_plan(root=ROOT,event_name='pull_request',ref='refs/pull/9/merge',source_sha='M'*40,base_sha='B'*40,head_sha='H'*40,explicit_changed=['frontend/apps/web/src/a.ts'])
  self.assertEqual(p['source_sha'],'M'*40);self.assertEqual(p['change_range']['mode'],'explicit');self.assertEqual(p['change_range']['head_sha'],'H'*40);self.assertEqual(p['change_range']['base_sha'],'B'*40);self.assertEqual(p['change_range']['changed_files'],['frontend/apps/web/src/a.ts'])
  self.assertEqual(p['expected_proofs'],p['ci_expected_proofs'])
 def test_range_normal_pr(self):
  with tempfile.TemporaryDirectory() as tmp:
   g=_make_repo(tmp);(Path(tmp)/'a.txt').write_text('x');g('add','.');g('commit','-qm','a');base=g('rev-parse','HEAD')
   (Path(tmp)/'b.txt').write_text('y');g('add','.');g('commit','-qm','b');head=g('rev-parse','HEAD')
   r=resolve_range(tmp,'pull_request',base,head,'');self.assertFalse(r['full_ci']);self.assertEqual(r['changed_files'],['b.txt']);self.assertEqual(r['mode'],'pull_request');self.assertEqual(r['merge_base_sha'],base);self.assertEqual(r['head_sha'],head)
 def test_range_diverged_pr(self):
  with tempfile.TemporaryDirectory() as tmp:
   g=_make_repo(tmp);(Path(tmp)/'a.txt').write_text('x');g('add','.');g('commit','-qm','a');base=g('rev-parse','HEAD')
   g('checkout','-qb','feature');(Path(tmp)/'b.txt').write_text('y');g('add','.');g('commit','-qm','b');head=g('rev-parse','HEAD')
   g('checkout','-q','main');(Path(tmp)/'c.txt').write_text('z');g('add','.');g('commit','-qm','c')
   r=resolve_range(tmp,'pull_request',base,head,'');self.assertFalse(r['full_ci']);self.assertEqual(r['changed_files'],['b.txt'])
 def test_range_missing_pr_base_full_ci(self):
  with tempfile.TemporaryDirectory() as tmp:
   g=_make_repo(tmp);(Path(tmp)/'a.txt').write_text('x');g('add','.');g('commit','-qm','a');head=g('rev-parse','HEAD')
   r=resolve_range(tmp,'pull_request','0000000000000000000000000000000000000000',head,'');self.assertTrue(r['full_ci']);self.assertEqual(r['reason'],'missing PR range')
 def test_range_merge_group(self):
  with tempfile.TemporaryDirectory() as tmp:
   g=_make_repo(tmp);(Path(tmp)/'a.txt').write_text('x');g('add','.');g('commit','-qm','a');base=g('rev-parse','HEAD')
   (Path(tmp)/'b.txt').write_text('y');g('add','.');g('commit','-qm','b');head=g('rev-parse','HEAD')
   r=resolve_range(tmp,'merge_group',base,head,'');self.assertFalse(r['full_ci']);self.assertEqual(r['mode'],'merge_group');self.assertEqual(r['changed_files'],['b.txt'])
 def test_range_push(self):
  with tempfile.TemporaryDirectory() as tmp:
   g=_make_repo(tmp);(Path(tmp)/'a.txt').write_text('x');g('add','.');g('commit','-qm','a');before=g('rev-parse','HEAD')
   (Path(tmp)/'b.txt').write_text('y');g('add','.');g('commit','-qm','b');head=g('rev-parse','HEAD')
   r=resolve_range(tmp,'push','',head,before);self.assertFalse(r['full_ci']);self.assertEqual(r['mode'],'push');self.assertEqual(r['changed_files'],['b.txt']);self.assertEqual(r['base_sha'],before)
 def test_range_push_zero_before_full_ci(self):
  with tempfile.TemporaryDirectory() as tmp:
   g=_make_repo(tmp);(Path(tmp)/'a.txt').write_text('x');g('add','.');g('commit','-qm','a');head=g('rev-parse','HEAD')
   r=resolve_range(tmp,'push','',head,'0'*40);self.assertTrue(r['full_ci']);self.assertEqual(r['reason'],'unknown push range')
 def test_range_workflow_dispatch_full_ci(self):
  with tempfile.TemporaryDirectory() as tmp:
   _make_repo(tmp);r=resolve_range(tmp,'workflow_dispatch','','','');self.assertTrue(r['full_ci']);self.assertEqual(r['mode'],'workflow_dispatch')
 def test_w1_docs_only(self):
  p=self._plan('docs/architecture/ci.md');self.assertEqual(p['planes'],['docs']);self.assertEqual(p['matrices']['backend'],[]);self.assertFalse(p['matrices']['containers']);self.assertIn('docs:gate',p['ci_expected_proofs'])
 def test_w1_backend_source(self):
  p=self._plan('backend/Notrelix.Api/Program.cs');self.assertEqual([x['component_id'] for x in p['matrices']['backend']],['backend-api']);self.assertIn('backend:backend-api',p['ci_expected_proofs'])
 def test_w1_backend_migration(self):
  p=self._plan('backend/Notrelix.Infrastructure/Migrations/0001_init.cs');self.assertTrue(p['schema_change'])
 def test_w1_openapi_contract(self):
  p=self._plan('backend/contracts/openapi/v1/boards.yaml');self.assertIn('contract',p['capabilities']);self.assertIn('web-tests',p['capabilities']);self.assertIn('backend:backend-api',p['ci_expected_proofs'])
 def test_w1_web(self):
  p=self._plan('frontend/apps/web/src/App.tsx');self.assertIn('frontend:gate',p['ci_expected_proofs']);self.assertIn('@notrelix/app-web',p['frontend_filters'])
 def test_w1_marketing(self):
  p=self._plan('frontend/apps/marketing/src/index.ts');self.assertTrue(p['matrices']['frontend_hosts']);self.assertIn('frontend:gate',p['ci_expected_proofs'])
 def test_w1_mobile(self):
  p=self._plan('frontend/apps/mobile/src/index.ts');self.assertTrue(p['matrices']['mobile'])
 def test_w1_shared(self):
  p=self._plan('frontend/packages/product/work-management/src/durable-model.ts');self.assertIn('frontend:gate',p['ci_expected_proofs'])
 def test_w1_tooling(self):
  p=self._plan('frontend/tooling/eslint.config.mjs');self.assertIn('tooling-tests',p['capabilities']);self.assertIn('ui',p['capabilities'])
 def test_w1_mock_system(self):
  p=self._plan('frontend/packages/dev/mock-server/src/index.ts');self.assertIn('mock',p['capabilities'])
 def test_w1_dependencies_backend(self):
  p=self._plan('backend/Directory.Packages.props');self.assertIn('security:backend',p['ci_expected_proofs'])
 def test_w1_dependencies_frontend(self):
  p=self._plan('frontend/pnpm-lock.yaml');self.assertIn('security:frontend',p['ci_expected_proofs'])
 def test_w1_backend_dockerfile_containers_backend_only(self):
  p=self._plan('backend/Dockerfile');self.assertEqual([c['component_id'] for c in p['matrices']['containers']],['backend-api']);self.assertEqual(p['infra_modes'],['assembled','topology'])
 def test_w1_web_dockerfile_containers_web_only(self):
  p=self._plan('frontend/Dockerfile');self.assertEqual([c['component_id'] for c in p['matrices']['containers']],['web']);self.assertIn('infra',p['planes'])
 def test_w1_marketing_dockerfile_containers_marketing_only(self):
  p=self._plan('frontend/Dockerfile.marketing');self.assertEqual([c['component_id'] for c in p['matrices']['containers']],['marketing']);self.assertIn('infra',p['planes'])
 def test_w1_web_nginx(self):
  p=self._plan('frontend/apps/web/nginx.conf');self.assertEqual([c['component_id'] for c in p['matrices']['containers']],['web']);self.assertEqual(p['infra_modes'],['assembled','topology'])
 def test_w1_runtime_infra(self):
  p=self._plan('docker-compose.yml');self.assertIn('infra',p['planes']);self.assertIn('topology',p['infra_modes']);self.assertIn('assembled',p['infra_modes']);self.assertIn('security:backend',p['ci_expected_proofs']);self.assertIn('security:frontend',p['ci_expected_proofs']);self.assertEqual(set(p['package_components']),{'backend-api','web','marketing'})
 def test_w1_images_lock_runtime_infra(self):
  p=self._plan('delivery/images.lock.toml');self.assertIn('infra',p['planes']);self.assertEqual(p['infra_modes'],['assembled','topology'])
 def test_w1_delivery_tooling_full_ci_delivery_proof(self):
  p=self._plan('tools/deliveryctl/planner.py');self.assertTrue(p['full_ci']);self.assertTrue(p['delivery_platform_required']);self.assertIn('delivery:platform',p['ci_expected_proofs'])
 def test_w1_workflow_full_ci_delivery_proof(self):
  p=self._plan('.github/workflows/frontend-ci.yml');self.assertTrue(p['full_ci']);self.assertTrue(p['delivery_platform_required']);self.assertIn('delivery:platform',p['ci_expected_proofs'])
 def test_w1_infra_required_outputs(self):
  from tools.deliveryctl.planner import github_outputs
  o=github_outputs(self._plan('docker-compose.yml'));self.assertEqual(o['infra_required'],'true');self.assertEqual(o['infra_topology_required'],'true');self.assertEqual(o['infra_assembled_required'],'true');self.assertEqual(o['backend_required'],'false');self.assertEqual(o['frontend_required'],'false')
 def test_w1_pset_pr_no_release_proof(self):
  p=self._plan('backend/Notrelix.Api/Program.cs');self.assertNotIn('stack:release-candidate',p['ci_expected_proofs']);self.assertEqual(p['release_expected_proofs'],[])
 def test_w1_pset_release_proof_split(self):
  p=build_plan(root=ROOT,event_name='push',ref='refs/heads/main',source_sha='b'*40,explicit_changed=['backend/Notrelix.Api/Program.cs']);self.assertTrue(p['release_candidate']);self.assertNotIn('stack:release-candidate',p['ci_expected_proofs']);self.assertIn('stack:release-candidate',p['release_expected_proofs']);self.assertEqual(p['expected_proofs'],p['ci_expected_proofs'])
 def test_w1_planhash_deterministic(self):
  kw=dict(root=ROOT,event_name='pull_request',ref='refs/pull/1/merge',source_sha='d'*40,explicit_changed=['frontend/apps/web/src/a.ts'])
  self.assertEqual(build_plan(**kw)['plan_sha256'],build_plan(**kw)['plan_sha256'])
  self.assertNotEqual(build_plan(**kw)['plan_sha256'],build_plan(**{**kw,'explicit_changed':['frontend/apps/web/src/b.ts']})['plan_sha256'])
if __name__=='__main__':unittest.main()