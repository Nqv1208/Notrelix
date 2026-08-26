from __future__ import annotations
import hashlib,json
from pathlib import Path
def aggregate(plan_path:Path,evidence_dir:Path,output_path:Path,expected_run_id=None):
    plan=json.loads(plan_path.read_text());expected=set(plan['expected_proofs']);source=plan['source_sha'];records={};problems=[]
    for path in sorted(evidence_dir.rglob('*.json')) if evidence_dir.exists() else []:
        d=json.loads(path.read_text()); proof=str(d.get('proof_id',''))
        if d.get('api_version')!='delivery.notrelix.dev/v1' or d.get('kind')!='EvidenceRecord':problems.append(f'{path}: invalid evidence');continue
        if not proof:problems.append(f'{path}: missing proof');continue
        if proof in records:problems.append(f'duplicate proof: {proof}');continue
        if d.get('status')!='passed':problems.append(f'failed proof: {proof}')
        if d.get('source_sha')!=source:problems.append(f'stale proof: {proof}')
        if expected_run_id and str(d.get('run_id'))!=str(expected_run_id):problems.append(f'foreign proof: {proof}')
        h=d.get('record_sha256');body=dict(d);body.pop('record_sha256',None);actual=hashlib.sha256(json.dumps(body,separators=(',',':'),sort_keys=True).encode()).hexdigest()
        if h!=actual:problems.append(f'tampered proof: {proof}')
        records[proof]=d
    actual=set(records);missing=sorted(expected-actual);unexpected=sorted(actual-expected)
    if missing:problems.append(f'missing proofs: {missing}')
    if unexpected:problems.append(f'unexpected proofs: {unexpected}')
    result={'api_version':'delivery.notrelix.dev/v1','kind':'EvidenceSummary','source_sha':source,'expected_proofs':sorted(expected),'actual_proofs':sorted(actual),'missing':missing,'unexpected':unexpected,'problems':problems,'status':'passed' if not problems else 'failed'}
    result['summary_sha256']=hashlib.sha256(json.dumps(result,separators=(',',':'),sort_keys=True).encode()).hexdigest();output_path.parent.mkdir(parents=True,exist_ok=True);output_path.write_text(json.dumps(result,indent=2,sort_keys=True)+'\n')
    if problems:raise ValueError('\n'.join(problems))
    return result
