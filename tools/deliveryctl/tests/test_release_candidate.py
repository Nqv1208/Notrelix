import json,tempfile,unittest
from pathlib import Path
from tools.deliveryctl.release import build_candidate

SHA40='a'*40
PLAN={
 'api_version':'delivery.notrelix.dev/v1','kind':'ExecutionPlan','source_sha':SHA40,'event':'push','ref':'refs/heads/main','release_candidate':True,'schema_change':False,'plan_sha256':'9'*64,
 'release_contract':{'migration_component':'backend-api','migration_service':'backend','migration_commands':['--migrate']},
 'deployment_containers':[
  {'component_id':'backend-api','compose_service':'backend','deploy_env_var':'NOTRELIX_BACKEND_IMAGE','stateful':False},
  {'component_id':'web','compose_service':'frontend-web','deploy_env_var':'NOTRELIX_WEB_IMAGE','stateful':False},
  {'component_id':'marketing','compose_service':'frontend-marketing','deploy_env_var':'NOTRELIX_MARKETING_IMAGE','stateful':False},
 ],
 'runtime_images':[
  {'id':'postgres','kind':'infrastructure','ref':'postgres@sha256:'+'2'*64,'compose_service':'postgres','deploy_env_var':'POSTGRES_IMAGE','stateful':True},
  {'id':'redis','kind':'infrastructure','ref':'redis@sha256:'+'3'*64,'compose_service':'redis','deploy_env_var':'REDIS_IMAGE','stateful':True},
 ],
}
EVIDENCE={'api_version':'delivery.notrelix.dev/v1','kind':'EvidenceSummary','summary_sha256':'8'*64,'status':'passed'}
IMAGE_REFS={'backend-api':('ghcr.io/nqv1208/notrelix-backend','sha256:'+'1'*64),'web':('ghcr.io/nqv1208/notrelix-web','sha256:'+'1'*64),'marketing':('ghcr.io/nqv1208/notrelix-marketing','sha256:'+'1'*64)}

class ReleaseCandidateTests(unittest.TestCase):
 def _manifest(self,component):
  ref,dig=IMAGE_REFS[component]
  return {'api_version':'delivery.notrelix.dev/v1','kind':'ReleaseImage','component_id':component,'image':{'ref':ref,'digest':dig}}
 def _dir(self,manifests):
  d=Path(tempfile.mkdtemp())
  for cid,m in manifests.items():
   if m is not None:(d/f'release-image-{cid}.json').write_text(json.dumps(m))
  return d
 def _full(self,images=None):
  return self._dir(images if images is not None else self._full_images())
 def _full_images(self):
  return {cid:self._manifest(cid) for cid in IMAGE_REFS}
 def test_happy_path(self):
  c=build_candidate(PLAN,EVIDENCE,self._full(),'42')
  self.assertEqual(c['kind'],'ReleaseCandidate');self.assertEqual(c['source_sha'],SHA40);self.assertEqual(c['ci_run_id'],'42')
  self.assertTrue(c['evidence']['summary_sha256']);self.assertEqual(c['schema_change'],False)
  apps=[i for i in c['images'] if i['kind']=='application'];infra=[i for i in c['images'] if i['kind']=='infrastructure']
  self.assertEqual({i['id'] for i in apps},{'backend-api','web','marketing'})
  self.assertTrue(all('@sha256:' in i['ref'] and i['ref']==i['ref'].lower() for i in c['images']))
  self.assertEqual(len(infra),2);self.assertEqual(len(c['images']),5)
  services=[i['compose_service'] for i in c['images']];self.assertEqual(len(set(services)),len(services))
 def test_unpinned_manifest_rejected(self):
  m=self._manifest('backend-api');m['image']['digest']=''
  with self.assertRaises(ValueError):build_candidate(PLAN,EVIDENCE,self._full({'backend-api':m}),'42')
 def test_missing_manifest_rejected(self):
  with self.assertRaises(ValueError):build_candidate(PLAN,EVIDENCE,self._dir({'backend-api':self._manifest('backend-api')}),'42')
 def test_non_lowercase_ref_rejected(self):
  m=self._manifest('backend-api');m['image']['ref']='ghcr.io/Nqv1208/notrelix-backend'
  with self.assertRaises(ValueError):build_candidate(PLAN,EVIDENCE,self._full({'backend-api':m}),'42')
 def test_duplicate_compose_service_rejected(self):
  d=self._full()
  (d/'release-image-backend-api.json').write_text(json.dumps({'api_version':'delivery.notrelix.dev/v1','kind':'ReleaseImage','component_id':'backend-api','image':{'ref':'ghcr.io/nqv1208/notrelix-backend','digest':'sha256:'+'1'*64}}))
  plan=json.loads(json.dumps(PLAN));plan['deployment_containers'][0]['compose_service']='frontend-web'
  with self.assertRaises(ValueError):build_candidate(plan,EVIDENCE,d,'42')
 def test_schema_change_binding(self):
  plan=json.loads(json.dumps(PLAN));plan['schema_change']=True
  c=build_candidate(plan,EVIDENCE,self._full(),'42');self.assertEqual(c['schema_change'],True)
 def test_non_release_plan_rejected(self):
  plan=json.loads(json.dumps(PLAN));plan['release_candidate']=False
  with self.assertRaises(ValueError):build_candidate(plan,EVIDENCE,self._full(),'42')
 def test_no_summary_sha_rejected(self):
  with self.assertRaises(ValueError):build_candidate(PLAN,{'kind':'EvidenceSummary'},self._full(),'42')
if __name__=='__main__':unittest.main()