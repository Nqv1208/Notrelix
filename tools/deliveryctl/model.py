from __future__ import annotations
import fnmatch, json, re, tomllib
from pathlib import Path
from typing import Any
from .runtime import ROOT
SCHEMA_VERSION=5
SAFE_ID=re.compile(r"^[a-z0-9][a-z0-9._-]*$")
PROOF_PLACEHOLDER=re.compile(r"\{([a-z_][a-z0-9_]*)\}")
def compact(v:Any)->str: return json.dumps(v,separators=(",",":"),sort_keys=True)
def load_toml(p:Path)->dict[str,Any]:
    with p.open('rb') as h: return tomllib.load(h)
def load_authorities(root:Path=ROOT):
    d=root/'delivery'; return {k:load_toml(d/f) for k,f in {
      'catalog':'catalog.toml','policy':'policy.toml','environments':'environments.toml','images':'images.lock.toml'}.items()}
def normalize_path(p:str)->str:
    p=p.replace('\\','/')
    # Strip only literal './' prefixes; never consume dotfiles like '.github/…'.
    while p.startswith('./'): p=p[2:]
    while '//' in p:p=p.replace('//','/')
    return p
def matches(path:str,pattern:str)->bool:
    p,pat=normalize_path(path),normalize_path(pattern)
    if pat.endswith('/**'):
        prefix=pat[:-3].rstrip('/')
        if p==prefix or p.startswith(prefix+'/'): return True
    return fnmatch.fnmatchcase(p,pat)
def component(catalog,cid):
    if not SAFE_ID.fullmatch(cid): raise ValueError(f'unsafe component id: {cid!r}')
    try:return catalog['components'][cid]
    except KeyError as e:raise KeyError(f'unknown component: {cid}') from e
def image(images,iid):
    try:v=images['images'][iid]
    except KeyError as e:raise KeyError(f'unknown image lock: {iid}') from e
    if '@sha256:' not in str(v.get('ref','')): raise ValueError(f'image {iid} is not immutable')
    return v
def resolve_proof_profile(policy,name,component_id=None):
    try:req=policy['proof_profiles'][name]['required']
    except KeyError as e:raise KeyError(f'unknown proof profile: {name}') from e
    out=[]
    for raw in req:
        placeholders=set(PROOF_PLACEHOLDER.findall(raw))
        if placeholders-{'component_id'}: raise ValueError(f'unsupported placeholders in {name}')
        if 'component_id' in placeholders and not component_id: raise ValueError(f'{name} requires component_id')
        out.append(raw.format(component_id=component_id or ''))
    return out
def validate_authorities(root:Path=ROOT):
    a=load_authorities(root); errors=[]
    for n,d in a.items():
        if d.get('schema_version')!=SCHEMA_VERSION: errors.append(f'{n}: schema_version must be {SCHEMA_VERSION}')
    c,p,imgs,envs=a['catalog'],a['policy'],a['images'],a['environments']
    providers={'backend','frontend-host','mobile'}; deploy_envs=set(); services=set()
    for cid,cfg in c.get('components',{}).items():
        if not SAFE_ID.fullmatch(cid): errors.append(f'unsafe component id {cid}')
        if cfg.get('provider') not in providers: errors.append(f'{cid}: invalid provider')
        if cfg.get('proof_profile') not in p.get('proof_profiles',{}): errors.append(f'{cid}: invalid proof profile')
        if cfg.get('deployable'):
            cc=cfg.get('container',{})
            for f in ('context','dockerfile','image_name','compose_service','deploy_env_var','runtime_port','health_path','health_scheme'):
                if f not in cc: errors.append(f'{cid}: container missing {f}')
            if cc.get('deploy_env_var') in deploy_envs: errors.append(f'{cid}: duplicate deploy env')
            if cc.get('compose_service') in services: errors.append(f'{cid}: duplicate compose service')
            deploy_envs.add(cc.get('deploy_env_var'));services.add(cc.get('compose_service'))
            for lock in list(cc.get('build_arg_locks',{}).values())+list(cc.get('smoke_dependency_locks',{}).values()):
                try:image(imgs,str(lock))
                except Exception as e: errors.append(f'{cid}: {e}')
        if cfg.get('provider') in {'frontend-host','mobile'} and not cfg.get('workspace'): errors.append(f'{cid}: workspace missing')
    for iid,cfg in imgs.get('images',{}).items():
        if cfg.get('class') not in {'build','runtime','tooling'}:errors.append(f'image {iid}: invalid class')
        try:image(imgs,iid)
        except Exception as e:errors.append(str(e))
        if cfg.get('class')=='runtime':
            for f in ('compose_service','deploy_env_var','stateful'):
                if f not in cfg:errors.append(f'image {iid}: missing {f}')
    for name,cfg in envs.get('environments',{}).items():
        merged={**envs.get('defaults',{}),**cfg}
        for f in ('deployment_adapter','rollback_after_schema_change','stateful_image_change_policy','smoke_profile','compose_overlay','promotion_mode','run_migrations','rollout_strategy','concurrency_group'):
            if f not in merged:errors.append(f'environment {name}: missing {f}')
    vocab=set(p['defaults']['frontend_default_capabilities']+p['defaults']['full_frontend_capabilities'])
    infra_modes=set(p.get('infra',{}).get('modes',[]))
    profiles=p['proof_profiles']
    bindings=p.get('proof_bindings',{})
    buse=set()
    for nr,r in enumerate(p.get('change_rules',[])):
        rid=r.get('id')
        if not rid: errors.append(f'change rule #{nr}: missing id'); continue
        if rid in buse: errors.append(f'change rule {rid}: duplicate id')
        buse.add(rid)
        for key in ('exclusive','full_ci','delivery_platform','release','full_frontend','package_all_deployables','package_all_frontend_deployables'):
            if key in r and not isinstance(r[key],bool): errors.append(f'change rule {rid}: {key} must be boolean')
        for f in ('components','package_components'):
            for cid in r.get(f,[]):
                if cid not in c.get('components',{}): errors.append(f'change rule {rid}: unknown {f[:-1]} {cid}')
        for cap in r.get('capabilities',[]):
            if cap not in vocab: errors.append(f'change rule {rid}: unknown capability {cap}')
        for mode in r.get('infra_modes',[]):
            if mode not in infra_modes: errors.append(f'change rule {rid}: unknown infra mode {mode}')
        if 'infra' in r.get('planes',[]) and not r.get('infra_modes'): errors.append(f'change rule {rid}: infra plane without infra_modes')
        if r.get('delivery_platform') and 'delivery-platform' not in profiles: errors.append(f'change rule {rid}: delivery_platform without delivery-platform profile')
    for section,claims in (('planes',bindings.get('planes',{})),('security_domains',bindings.get('security_domains',{})),('capabilities',bindings.get('capabilities',{}))):
        for key,prof in claims.items():
            if prof not in profiles: errors.append(f'proof_bindings.{section}.{key}: unknown profile {prof}')
            if section=='capabilities' and key not in vocab: errors.append(f'proof_bindings.capabilities.{key}: unknown capability')
    for section in ('packaging','release','delivery'):
        prof=bindings.get(section,{}).get('profile')
        if prof not in profiles: errors.append(f'proof_bindings.{section}: unknown profile')
    if 'delivery-platform' not in profiles: errors.append('missing delivery-platform proof profile')
    if errors: raise ValueError('\n'.join(errors))
    return a
