import unittest,hashlib,json,tempfile
from pathlib import Path
from tools.deliveryctl.planner import build_plan
from tools.deliveryctl.evidence import aggregate
from tools.deliveryctl.runtime import ROOT
class T(unittest.TestCase):
 def setUp(self):
  self.tmp=Path(tempfile.mkdtemp());self.plan=build_plan(root=ROOT,event_name='pull_request',ref='refs/pull/1/merge',source_sha='s'*40,explicit_changed=['docs/architecture/ci.md'])
  (self.tmp/'plan.json').write_text(json.dumps(self.plan))
  self.evi=self.tmp/'evidence';self.evi.mkdir();self.source='s'*40;self.run='123'
 def _record(self,proof,source=None,run_id=None,plan_hash=None,status='passed'):
  body={'api_version':'delivery.notrelix.dev/v1','kind':'EvidenceRecord','proof_id':proof,'component_id':'docs','status':status,'source_sha':source if source is not None else self.source,'run_id':run_id if run_id is not None else self.run,'run_attempt':'1','workflow':'Docs CI','job':'docs-gate','created_at':'2026-01-01T00:00:00Z','metadata':{}}
  if plan_hash is not False:body['plan_sha256']=self.plan['plan_sha256'] if plan_hash is None else plan_hash
  h=hashlib.sha256(json.dumps(body,separators=(',',':'),sort_keys=True).encode()).hexdigest()
  (self.evi/f'{proof.replace(":","-")}.json').write_text(json.dumps({**body,'record_sha256':h}))
 def test_aggregate_accepts_plan_bound_proofs(self):
  for proof in self.plan['ci_expected_proofs']:self._record(proof)
  out=aggregate(self.tmp/'plan.json',self.evi,self.tmp/'summary.json',expected_run_id=self.run)
  self.assertEqual(out['status'],'passed');self.assertEqual(out['plan_sha256'],self.plan['plan_sha256']);self.assertEqual(out['missing'],[])
 def test_aggregate_rejects_unbound_proof(self):
  self._record('docs:gate',plan_hash=False)
  with self.assertRaises(ValueError) as ctx:aggregate(self.tmp/'plan.json',self.evi,self.tmp/'summary.json')
  self.assertIn('unbound proof',str(ctx.exception))
 def test_aggregate_rejects_foreign_plan(self):
  self._record('docs:gate',plan_hash='f'*64)
  with self.assertRaises(ValueError) as ctx:aggregate(self.tmp/'plan.json',self.evi,self.tmp/'summary.json')
  self.assertIn('foreign plan proof',str(ctx.exception))
 def test_aggregate_rejects_stale_source(self):
  self._record('docs:gate',source='t'*40)
  with self.assertRaises(ValueError) as ctx:aggregate(self.tmp/'plan.json',self.evi,self.tmp/'summary.json')
  self.assertIn('stale proof',str(ctx.exception))
 def test_aggregate_rejects_missing_proof(self):
  with self.assertRaises(ValueError) as ctx:aggregate(self.tmp/'plan.json',self.evi,self.tmp/'summary.json')
  self.assertIn('missing proofs',str(ctx.exception))
 def _frontend_plan(self):
  self.plan=build_plan(root=ROOT,event_name='pull_request',ref='refs/pull/131/merge',source_sha='s'*40,explicit_changed=['frontend/apps/web/src/board.ts'])
  (self.tmp/'plan.json').write_text(json.dumps(self.plan));return self.plan
 def test_fe_evidence_01_full_baseline_frontend_proofs_aggregate(self):
  plan=self._frontend_plan()
  self.assertIn('frontend:gate',plan['ci_expected_proofs']);self.assertIn('ui:foundation',plan['ci_expected_proofs'])
  for proof in plan['ci_expected_proofs']:self._record(proof)
  out=aggregate(self.tmp/'plan.json',self.evi,self.tmp/'summary.json',expected_run_id=self.run)
  self.assertEqual(out['status'],'passed');self.assertEqual(out['missing'],[]);self.assertEqual(out['unexpected'],[])
 def test_fe_evidence_02_missing_ui_foundation_fails(self):
  plan=self._frontend_plan()
  for proof in plan['ci_expected_proofs']:
   if proof!='ui:foundation':self._record(proof)
  with self.assertRaises(ValueError) as ctx:aggregate(self.tmp/'plan.json',self.evi,self.tmp/'summary.json',expected_run_id=self.run)
  self.assertIn("missing proofs: ['ui:foundation']",str(ctx.exception))
 def test_fe_evidence_03_unexpected_frontend_proof_fails(self):
  plan=self._frontend_plan()
  for proof in plan['ci_expected_proofs']:self._record(proof)
  self._record('frontend:legacy')
  with self.assertRaises(ValueError) as ctx:aggregate(self.tmp/'plan.json',self.evi,self.tmp/'summary.json',expected_run_id=self.run)
  self.assertIn("unexpected proofs: ['frontend:legacy']",str(ctx.exception))
if __name__=='__main__':unittest.main()