from __future__ import annotations
import argparse,json,os,sys
from pathlib import Path
from .runtime import ROOT,require_runtime
from .model import compact,validate_authorities
from .planner import build_plan,github_outputs
from .evidence import aggregate
from .environment import resolve
from .bundle import materialize
from .architecture import check as architecture_check
from .visual import check as visual_check

def gout(values):
    target=os.getenv('GITHUB_OUTPUT')
    if not target:
        for k,v in values.items():print(f'{k}={v}')
    else:
        with open(target,'a') as h:
            for k,v in values.items():h.write(f'{k}={v}\n')
def main():
    require_runtime();p=argparse.ArgumentParser();s=p.add_subparsers(dest='cmd',required=True);s.add_parser('validate');s.add_parser('architecture-check');
    q=s.add_parser('plan');q.add_argument('--event-name',required=True);q.add_argument('--ref',default='');q.add_argument('--source-sha',required=True);q.add_argument('--base-sha',default='');q.add_argument('--head-sha',default='');q.add_argument('--before-sha',default='');q.add_argument('--changed-file',action='append');q.add_argument('--full',action='store_true');q.add_argument('--output',type=Path,required=True);q.add_argument('--github-output',action='store_true');q.add_argument('--repository',required=True);q.add_argument('--head-repository',required=True);q.add_argument('--actor',required=True);q.add_argument('--requested-release-mode',default='auto',choices=['auto','rehearsal'])
    e=s.add_parser('evidence-aggregate');e.add_argument('--plan',type=Path,required=True);e.add_argument('--evidence-dir',type=Path,required=True);e.add_argument('--output',type=Path,required=True);e.add_argument('--run-id',default=os.getenv('GITHUB_RUN_ID',''))
    en=s.add_parser('environment');en.add_argument('--name',required=True);en.add_argument('--require-promotion-mode',default='');en.add_argument('--github-output',action='store_true')
    b=s.add_parser('bundle');b.add_argument('--manifest',type=Path,required=True);b.add_argument('--environment',type=Path,required=True);b.add_argument('--output-dir',type=Path,required=True)
    v=s.add_parser('visual');v.add_argument('--check',action='store_true')
    a=p.parse_args()
    try:
        if a.cmd=='validate':validate_authorities(ROOT);print('delivery authorities PASS')
        elif a.cmd=='architecture-check':validate_authorities(ROOT);architecture_check(ROOT);print('architecture PASS')
        elif a.cmd=='plan':
            validate_authorities(ROOT);plan=build_plan(root=ROOT,event_name=a.event_name,ref=a.ref,source_sha=a.source_sha,base_sha=a.base_sha,head_sha=a.head_sha,before_sha=a.before_sha,explicit_changed=a.changed_file,force_full=a.full,repository=a.repository,head_repository=a.head_repository,actor=a.actor,requested_release_mode=a.requested_release_mode);a.output.parent.mkdir(parents=True,exist_ok=True);a.output.write_text(json.dumps(plan,indent=2,sort_keys=True)+'\n');gout(github_outputs(plan)) if a.github_output else None;print(a.output)
        elif a.cmd=='evidence-aggregate':aggregate(a.plan,a.evidence_dir,a.output,a.run_id or None);print(a.output)
        elif a.cmd=='environment':
            validate_authorities(ROOT);c=resolve(a.name,ROOT)
            if a.require_promotion_mode and c['promotion_mode']!=a.require_promotion_mode:raise ValueError('promotion mode mismatch')
            gout({'contract_json':compact(c)}) if a.github_output else print(json.dumps(c,indent=2))
        elif a.cmd=='bundle':materialize(json.loads(a.manifest.read_text()),json.loads(a.environment.read_text()),a.output_dir);print(a.output_dir)
        elif a.cmd=='visual':visual_check(ROOT);print('visual baseline contract PASS')
        return 0
    except Exception as e:print(f'deliveryctl: {e}',file=sys.stderr);return 1
