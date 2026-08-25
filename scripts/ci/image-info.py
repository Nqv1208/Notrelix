#!/usr/bin/env python3
from __future__ import annotations
import argparse, json
from delivery_model import ROOT, load_images, github_output

def main() -> int:
    p=argparse.ArgumentParser()
    p.add_argument('--image', required=True)
    p.add_argument('--field', default='')
    p.add_argument('--github-output', action='store_true')
    args=p.parse_args()
    images=load_images(ROOT).get('images', {})
    if args.image not in images: raise SystemExit(f'unknown image lock: {args.image}')
    data={'name':args.image, **images[args.image]}
    if args.field:
        if args.field not in data: raise SystemExit(f'unknown field {args.field}')
        print(data[args.field]); return 0
    if args.github_output:
        for k,v in data.items(): github_output(k, str(v).lower() if isinstance(v,bool) else str(v))
    else: print(json.dumps(data, indent=2, sort_keys=True))
    return 0
if __name__=='__main__': raise SystemExit(main())
