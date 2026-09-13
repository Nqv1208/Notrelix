from __future__ import annotations
import json,re
from pathlib import Path
SHA40=re.compile(r'^[0-9a-f]{40}$')
def validate_manifest(m,allow_staging_verified=True):
    kinds={'ReleaseCandidate','StagingVerifiedRelease'} if allow_staging_verified else {'ReleaseCandidate'}
    if m.get('api_version')!='delivery.notrelix.dev/v1' or m.get('kind') not in kinds:raise ValueError('invalid release manifest identity')
    if not SHA40.fullmatch(str(m.get('source_sha',''))):raise ValueError('invalid source_sha')
    images=m.get('images');
    if not isinstance(images,list) or not images:raise ValueError('release manifest images required')
    seen=set()
    for item in images:
        if '@sha256:' not in str(item.get('ref','')):raise ValueError(f"un-pinned release image: {item.get('ref')}")
        if str(item.get('ref',''))!=str(item.get('ref','')).lower():raise ValueError(f"non-lowercase release image ref: {item.get('ref')}")
        service=str(item.get('compose_service',''))
        if not service or service in seen:raise ValueError(f'invalid/duplicate compose service: {service}')
        seen.add(service)
    return m
def build_candidate(plan,evidence_summary,images_dir,run_id):
    if plan.get('kind')!='ExecutionPlan':raise ValueError('plan is not an ExecutionPlan')
    if not plan.get('release_candidate'):raise ValueError('plan is not a release-candidate plan')
    source=str(plan.get('source_sha',''))
    if not SHA40.fullmatch(source):raise ValueError('invalid plan source_sha')
    contract=plan.get('release_contract')
    if not isinstance(contract,dict):raise ValueError('plan release_contract required')
    summary=str(evidence_summary.get('summary_sha256',''))
    if not summary:raise ValueError('evidence summary_sha256 required')
    containers=plan.get('deployment_containers')
    if not isinstance(containers,list) or not containers:raise ValueError('plan deployment_containers required')
    by_id={c.get('component_id'):c for c in containers if isinstance(c,dict)}
    d=Path(images_dir)
    if not d.is_dir():raise ValueError(f'release-image manifests dir missing: {d}')
    manifests=[]
    for f in sorted(d.glob('release-image-*.json')):
        m=json.loads(f.read_text())
        if m.get('kind')!='ReleaseImage' or not m.get('component_id'):raise ValueError(f'invalid release-image manifest: {f.name}')
        manifests.append(m)
    if not manifests:raise ValueError('no release-image manifests found')
    images=[];published=set()
    for m in manifests:
        cid=str(m['component_id']);published.add(cid);c=by_id.get(cid)
        if not c:raise ValueError(f'no container contract for published component {cid}')
        ref=str(m.get('image',{}).get('ref',''));dig=str(m.get('image',{}).get('digest',''))
        if not ref or not dig.startswith('sha256:') or len(dig)!=71:raise ValueError(f'incomplete image identity for {cid}')
        images.append({'id':cid,'kind':'application','ref':f'{ref}@{dig}','compose_service':str(c.get('compose_service','')),'deploy_env_var':str(c.get('deploy_env_var','')),'stateful':bool(c.get('stateful'))})
    missing={str(c.get('component_id')) for c in containers}-published
    if missing:raise ValueError(f'container without published image: {sorted(missing)}')
    runtime=plan.get('runtime_images')
    if runtime:images+=runtime
    candidate={'api_version':'delivery.notrelix.dev/v1','kind':'ReleaseCandidate','source_sha':source,'ci_run_id':str(run_id),'schema_change':bool(plan.get('schema_change')),'plan_sha256':str(plan.get('plan_sha256','')),'release_contract':contract,'evidence':{'summary_sha256':summary,'run_id':str(run_id)},'images':images}
    validate_manifest(candidate,False)
    return candidate
def staging_verified(candidate,release_run_id):
    validate_manifest(candidate,False);out=dict(candidate);out['kind']='StagingVerifiedRelease';out['release_run_id']=str(release_run_id);out['staging_verified']=True;return out
