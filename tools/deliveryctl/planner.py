from __future__ import annotations
import hashlib, json, re, subprocess
from pathlib import Path
from typing import Any
from .model import compact,component,image,load_authorities,matches,normalize_path,resolve_proof_profile
from .runtime import ROOT
PLAN_API='delivery.notrelix.dev/v1'; TAG_RE=re.compile(r'^v?(\d+\.\d+\.\d+)(?:-([A-Za-z0-9][\w.-]*))?$')
def _git(root,*args):
    p=subprocess.run(['git',*args],cwd=root,text=True,stdout=subprocess.PIPE,stderr=subprocess.PIPE)
    if p.returncode: raise RuntimeError(p.stderr.strip())
    return p.stdout.strip()
def _exists(root,sha):
    if not sha or set(sha)=={'0'}: return False
    return subprocess.run(['git','cat-file','-e',f'{sha}^{{commit}}'],cwd=root,stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL).returncode==0
def changed_files(root,event_name,base_sha,head_sha,before_sha):
    head=head_sha or _git(root,'rev-parse','HEAD')
    if event_name in {'pull_request','merge_group'}:
        if not (_exists(root,base_sha) and _exists(root,head)): return [],True,'missing PR range'
        mb=_git(root,'merge-base',base_sha,head); out=_git(root,'diff','--name-only','--diff-filter=ACMRDTUXB',mb,head)
        return sorted({normalize_path(x) for x in out.splitlines() if x.strip()}),False,f'merge-base:{mb}'
    if event_name=='push':
        if _exists(root,before_sha) and _exists(root,head):
            out=_git(root,'diff','--name-only','--diff-filter=ACMRDTUXB',before_sha,head)
            return sorted({normalize_path(x) for x in out.splitlines() if x.strip()}),False,f'push:{before_sha}..{head}'
        return [],True,'unknown push range'
    return [],True,f'{event_name}: full CI'
def renderer(images):
    cfg=image(images,'playwright-ci'); source=str(cfg['source']); m=TAG_RE.fullmatch(source.rsplit(':',1)[-1])
    if not m: raise ValueError('playwright-ci tag must be semantic')
    return {'image_id':'playwright-ci','source':source,'ref':str(cfg['ref']),'version':m.group(1),'variant':m.group(2) or ''}
def runtime_images(images):
    return [{'id':iid,'kind':'infrastructure','ref':str(cfg['ref']),'compose_service':str(cfg['compose_service']),'deploy_env_var':str(cfg['deploy_env_var']),'stateful':bool(cfg.get('stateful'))}
      for iid,cfg in sorted(images.get('images',{}).items()) if cfg.get('class')=='runtime']
def host_contract(cid,cfg):
    f=cfg['frontend'];return {'component_id':cid,'workspace':cfg['workspace'],'runtime':f.get('runtime',''),'build_script':f['build_script'],'e2e_script':f['e2e_script'],'artifact_paths_json':compact(f['artifact_paths']),'artifact_name':f'frontend-host-build-{cid}','archive_file':f'{cid}.tar.gz','manifest_file':f'{cid}.manifest.json'}
def mobile_contract(cid,cfg):return {'component_id':cid,'workspace':cfg['workspace'],'build_script':cfg['frontend']['build_script']}
def container_contract(cid,cfg,images):
    cc=cfg['container'];return {'component_id':cid,'provider':cfg['provider'],'container_context':cc['context'],'dockerfile':cc['dockerfile'],'image_name':cc['image_name'],'compose_service':cc['compose_service'],'deploy_env_var':cc['deploy_env_var'],'runtime_port':int(cc['runtime_port']),'health_path':cc['health_path'],'health_scheme':cc['health_scheme'],'build_args':{k:image(images,v)['ref'] for k,v in cc.get('build_arg_locks',{}).items()},'smoke_dependencies':{k:image(images,v)['ref'] for k,v in cc.get('smoke_dependency_locks',{}).items()},'stateful':False}
def build_plan(root=ROOT,event_name='workflow_dispatch',ref='',source_sha='',base_sha='',head_sha='',before_sha='',explicit_changed=None,force_full=False):
    a=load_authorities(root);c,p,imgs=a['catalog'],a['policy'],a['images']; comps=c['components']; all_ids=set(comps);deployables={i for i,v in comps.items() if v.get('deployable')};front={i for i,v in comps.items() if v.get('provider') in {'frontend-host','mobile'}};front_dep=front&deployables
    if explicit_changed is not None: changed=sorted({normalize_path(x) for x in explicit_changed});full=force_full;reason='explicit'
    elif force_full:changed=[];full=True;reason='full'
    else:changed,full,reason=changed_files(root,event_name,base_sha,head_sha,before_sha)
    affected=set();packages=set();planes=set();caps=set();security=set();runtime_changed=set();release_required=False;full_front=False;warnings=[]
    for path in changed:
        matched=False;exclusive=False
        for rule in p.get('change_rules',[]):
            if not any(matches(path,pat) for pat in rule.get('patterns',[])):continue
            matched=True;exclusive|=bool(rule.get('exclusive'));affected.update(rule.get('components',[]));packages.update(rule.get('package_components',[]));planes.update(rule.get('planes',[]));caps.update(rule.get('capabilities',[]));security.update(rule.get('security_domains',[]));full|=bool(rule.get('full_ci'));full_front|=bool(rule.get('full_frontend'));release_required|=rule.get('release') is True
            if rule.get('package_all_deployables'):packages.update(deployables)
            if rule.get('package_all_frontend_deployables'):packages.update(front_dep)
        if exclusive:continue
        direct={cid for cid,cfg in comps.items() if any(matches(path,pat) for pat in cfg.get('roots',[]))}
        if direct:
            affected.update(direct)
            for cid in direct:
                cfg=comps[cid];security.add(cfg.get('security_domain')) if cfg.get('security_domain') else None
                if cfg.get('deployable'):packages.add(cid);runtime_changed.add(cid);release_required|=cfg.get('release_on_runtime_change',True)
        elif not matched:warnings.append(f'unclassified path {path}; full fail-safe');full=True;release_required=True
    if full and event_name=='push' and reason.startswith('unknown push'):release_required=True
    if full:
        affected.update(all_ids);packages.update(deployables);planes.update({'docs','infra'});security.update({'backend','frontend'});caps.update(p['defaults']['full_frontend_capabilities'])
    elif full_front:
        affected.update(front);packages.update(front_dep);security.add('frontend');caps.update(p['defaults']['full_frontend_capabilities'])
    if affected&front:caps.update(p['defaults']['frontend_default_capabilities']);security.add('frontend')
    for cid in affected&front:caps.update(comps[cid].get('frontend',{}).get('capabilities',[]))
    release_candidate=event_name=='push' and ref==f"refs/heads/{c['repository']['release_branch']}" and release_required
    if release_candidate:packages=set(deployables);security.update({'backend','frontend'})
    backend=[{'component_id':cid,'redis_image':image(imgs,'redis')['ref']} for cid in sorted(affected) if comps[cid].get('provider')=='backend']
    hosts=[host_contract(cid,comps[cid]) for cid in sorted(affected) if comps[cid].get('provider')=='frontend-host']
    mobiles=[mobile_contract(cid,comps[cid]) for cid in sorted(affected) if comps[cid].get('provider')=='mobile']
    containers=[container_contract(cid,comps[cid],imgs) for cid in sorted(packages)]
    deployable_contracts=[container_contract(cid,comps[cid],imgs) for cid in sorted(deployables)]
    filters=sorted({comps[cid]['workspace'] for cid in affected&front if comps[cid].get('workspace')});
    if 'tooling-tests' in caps:filters.append('./tooling/**')
    expected=[]
    for cid in sorted(affected):expected+=resolve_proof_profile(p,comps[cid]['proof_profile'],component_id=cid)
    b=p['proof_bindings']
    for plane in sorted(planes):expected+=resolve_proof_profile(p,b['planes'][plane])
    for dom in sorted(security):expected+=resolve_proof_profile(p,b['security_domains'][dom])
    for cap in sorted(caps):
        if cap in b.get('capabilities',{}):expected+=resolve_proof_profile(p,b['capabilities'][cap])
    for cid in sorted(packages):expected+=resolve_proof_profile(p,b['packaging']['profile'],component_id=cid)
    if release_candidate:expected+=resolve_proof_profile(p,b['release']['profile'])
    dep=p['deployment'];migration_component=dep['migration_component'];migration_service=comps[migration_component]['container']['compose_service']
    release_contract={'schema_change_policy':dep['schema_change_policy'],'rollback_after_schema_change':dep['rollback_after_schema_change'],'migration_component':migration_component,'migration_service':migration_service,'migration_commands':dep['migration_commands'],'stack_health_url':dep['stack_health_url'],'stack_smoke_urls':dep['stack_smoke_urls']}
    schema_change=any(any(matches(path,pat) for pat in dep['migration_paths']) for path in changed)
    mock=host_contract(p['mock']['artifact_component'],comps[p['mock']['artifact_component']])
    plan={'api_version':PLAN_API,'kind':'ExecutionPlan','source_sha':source_sha,'event':event_name,'ref':ref,'reason':reason,'changed_files':changed,'full_ci':full,'affected_components':sorted(affected),'package_components':sorted(packages),'planes':sorted(planes),'capabilities':sorted(caps),'security_domains':sorted(security),'release_candidate':release_candidate,'schema_change':schema_change,'expected_proofs':sorted(set(expected)),'warnings':warnings,'renderer':renderer(imgs),'runtime_images':runtime_images(imgs),'deployment_containers':deployable_contracts,'release_contract':release_contract,'mock_artifact':mock,'frontend_filters':filters,'matrices':{'backend':backend,'frontend_hosts':hosts,'mobile':mobiles,'containers':containers}}
    plan['plan_sha256']=hashlib.sha256(compact(plan).encode()).hexdigest();return plan
def github_outputs(plan):
    matrix=lambda x:compact({'include':x});m=plan['matrices'];mock=plan['mock_artifact'];caps=plan['capabilities']
    return {'backend_matrix':matrix(m['backend']),'backend_count':str(len(m['backend'])),'frontend_host_matrix':matrix(m['frontend_hosts']),'host_count':str(len(m['frontend_hosts'])),'mobile_matrix':matrix(m['mobile']),'mobile_count':str(len(m['mobile'])),'container_matrix':matrix(m['containers']),'container_count':str(len(m['containers'])),'frontend_required':str(bool(m['frontend_hosts'] or m['mobile'] or caps)).lower(),'frontend_filters_json':compact(plan['frontend_filters']),'frontend_capabilities_json':compact(caps),'renderer_ref':plan['renderer']['ref'],'renderer_version':plan['renderer']['version'],'runtime_images_json':compact({'api_version':PLAN_API,'kind':'RuntimeImageSet','images':plan['runtime_images']}),'deployable_containers_json':compact({'api_version':PLAN_API,'kind':'DeployableContainerSet','containers':plan['deployment_containers']}),'release_contract_json':compact(plan['release_contract']),'mock_artifact_component':mock['component_id'],'mock_artifact_name':mock['artifact_name'],'mock_archive_file':mock['archive_file'],'mock_manifest_file':mock['manifest_file'],'docs_required':str('docs' in plan['planes']).lower(),'infra_required':str('infra' in plan['planes']).lower(),'security_backend':str('backend' in plan['security_domains']).lower(),'security_frontend':str('frontend' in plan['security_domains']).lower(),'packaging_required':str(bool(m['containers'])).lower(),'release_candidate':str(bool(plan['release_candidate'])).lower(),'schema_change':str(bool(plan['schema_change'])).lower(),'expected_proofs_json':compact(plan['expected_proofs']),'plan_sha256':plan['plan_sha256']}
