from __future__ import annotations
import json,re
from pathlib import Path
from .release import validate_manifest
SAFE_ENV=re.compile(r'^[A-Z][A-Z0-9_]*$');SAFE_SERVICE=re.compile(r'^[A-Za-z0-9][A-Za-z0-9_.-]*$')
def materialize(manifest,environment,output_dir:Path):
    validate_manifest(manifest);required=('name','deployment_adapter','rollback_after_schema_change','stateful_image_change_policy','smoke_profile','compose_overlay','promotion_mode','run_migrations','rollout_strategy','concurrency_group')
    missing=[x for x in required if x not in environment]
    if missing:raise ValueError(f'environment missing {missing}')
    if environment['deployment_adapter']!='compose-ssh':raise ValueError('unsupported deployment adapter')
    output_dir.mkdir(parents=True,exist_ok=True);images=manifest['images'];lines=['services:'];env=[];stateful=[];services=[]
    for item in sorted(images,key=lambda x:x['compose_service']):
        service=str(item['compose_service']);ref=str(item['ref']);key=str(item['deploy_env_var'])
        if not SAFE_SERVICE.fullmatch(service) or not SAFE_ENV.fullmatch(key):raise ValueError('unsafe deployment identifiers')
        lines += [f'  {service}:',f'    image: {ref}'];env.append(f'{key}={ref}')
        if item.get('stateful'):stateful.append(f'{key}={ref}');services.append(service)
    (output_dir/'release.generated.yml').write_text('\n'.join(lines)+'\n');(output_dir/'images.env').write_text('\n'.join(sorted(env))+'\n');(output_dir/'stateful.env').write_text('\n'.join(sorted(stateful))+'\n');(output_dir/'stateful.services').write_text('\n'.join(sorted(services))+'\n')
    rc=manifest.get('release_contract',{});(output_dir/'migration.commands').write_text('\n'.join(map(str,rc.get('migration_commands',[])))+'\n')
    meta={'COMPOSE_OVERLAY':environment['compose_overlay'],'PROMOTION_MODE':environment['promotion_mode'],'RUN_MIGRATIONS':str(bool(environment['run_migrations'])).lower(),'ROLLOUT_STRATEGY':environment['rollout_strategy'],'SMOKE_PROFILE':environment['smoke_profile'],'STATEFUL_IMAGE_CHANGE_POLICY':environment['stateful_image_change_policy'],'ROLLBACK_AFTER_SCHEMA_CHANGE':environment['rollback_after_schema_change'],'SCHEMA_CHANGE':str(bool(manifest.get('schema_change'))).lower(),'RELEASE_SHA':manifest['source_sha'],'MIGRATION_SERVICE':rc.get('migration_service','')}
    (output_dir/'metadata.env').write_text(''.join(f'{k}={v}\n' for k,v in meta.items()));(output_dir/'manifest.json').write_text(json.dumps(manifest,indent=2,sort_keys=True)+'\n')
