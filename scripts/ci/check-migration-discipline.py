#!/usr/bin/env python3
from __future__ import annotations
import argparse, os, subprocess
from pathlib import Path

def git(root: Path,*args:str)->str:
    return subprocess.check_output(['git',*args],cwd=root,text=True,stderr=subprocess.DEVNULL).strip()

def exists(root:Path,sha:str)->bool:
    if not sha:return False
    return subprocess.run(['git','cat-file','-e',f'{sha}^{{commit}}'],cwd=root,stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL).returncode==0

def main()->int:
    p=argparse.ArgumentParser(description='Forbid rewriting already-existing EF migration source files')
    p.add_argument('--base-sha',default=os.getenv('BASE_SHA',''))
    p.add_argument('--head-sha',default=os.getenv('HEAD_SHA',os.getenv('GITHUB_SHA','HEAD')))
    p.add_argument('--repo-root',type=Path,default=Path(__file__).resolve().parents[2])
    args=p.parse_args(); root=args.repo_root.resolve(); head=args.head_sha or 'HEAD'; base=args.base_sha
    if not exists(root,head): head='HEAD'
    if base and exists(root,base): start=git(root,'merge-base',base,head)
    else:
        # Push/manual without a trustworthy base cannot prove history safely. This
        # guard is a PR/review invariant; migration smoke still runs independently.
        print('Migration discipline: no trustworthy base SHA; history rewrite check skipped.'); return 0
    out=git(root,'diff','--name-status','--find-renames',start,head,'--','backend')
    violations=[]
    for line in out.splitlines():
        if not line.strip():continue
        parts=line.split('\t'); status=parts[0]; paths=parts[1:]
        for path in paths:
            normalized=path.replace('\\','/')
            if '/Migrations/' not in normalized or not normalized.endswith('.cs'):continue
            if normalized.endswith('ModelSnapshot.cs'):continue
            # New migrations are append-only and valid; pre-existing migrations
            # must not be modified, deleted, copied over or renamed.
            if not status.startswith('A'):
                violations.append(f'{status}\t{normalized}')
    if violations:
        print('Existing migration source files are immutable after introduction. Add a new migration instead:')
        print('\n'.join(violations)); return 1
    print('Migration discipline: append-only history verified.'); return 0
if __name__=='__main__': raise SystemExit(main())
