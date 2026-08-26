from __future__ import annotations
import re
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
        service=str(item.get('compose_service',''))
        if not service or service in seen:raise ValueError(f'invalid/duplicate compose service: {service}')
        seen.add(service)
    return m
def staging_verified(candidate,release_run_id):
    validate_manifest(candidate,False);out=dict(candidate);out['kind']='StagingVerifiedRelease';out['release_run_id']=str(release_run_id);out['staging_verified']=True;return out
