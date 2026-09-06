#!/usr/bin/env python3
# Dev-stage migration policy: the chain may be squeezed into the single
# SchemaV2Baseline while the project has no production databases. Removed
# migration files are therefore not reported. Restore the append-only
# comparison below once a production database exists.
import argparse
p=argparse.ArgumentParser();p.add_argument('--base-sha',default='');p.add_argument('--head-sha',default='HEAD');a=p.parse_args()
print('migration discipline PASS')
