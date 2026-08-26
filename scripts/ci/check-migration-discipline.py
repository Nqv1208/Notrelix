#!/usr/bin/env python3
import argparse,subprocess,sys
p=argparse.ArgumentParser();p.add_argument('--base-sha',default='');p.add_argument('--head-sha',default='HEAD');a=p.parse_args()
if not a.base_sha: print('migration discipline: no base SHA, skipping history comparison');raise SystemExit(0)
def git(*args):return subprocess.check_output(['git',*args],text=True).splitlines()
base=git('ls-tree','-r','--name-only',a.base_sha,'--','backend')
head=git('ls-tree','-r','--name-only',a.head_sha,'--','backend')
is_migration=lambda x:'/Migrations/' in x or '/migrations/' in x
removed=sorted({x for x in base if is_migration(x)}-{x for x in head if is_migration(x)})
if removed:print('append-only migration violation:',*removed,sep='\n  ',file=sys.stderr);raise SystemExit(1)
print('migration discipline PASS')
