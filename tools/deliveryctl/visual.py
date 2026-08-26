from __future__ import annotations
import json,re
from pathlib import Path
from .model import load_authorities,image
from .runtime import ROOT
LOCK=Path('frontend/e2e/ui/visual-baseline.lock.json')
def current(root=ROOT):
    a=load_authorities(root);r=image(a['images'],'playwright-ci');source=r['source'];m=re.fullmatch(r'v?(\d+\.\d+\.\d+)-([A-Za-z0-9._-]+)',source.rsplit(':',1)[-1])
    if not m:raise ValueError('renderer source version invalid')
    f=json.loads((root/'frontend/package.json').read_text());s=json.loads((root/'frontend/tooling/storybook/web/package.json').read_text());names=['storybook','@storybook/react','@storybook/react-vite','@storybook/addon-a11y']
    return {'api_version':'delivery.notrelix.dev/v1','kind':'VisualBaselineToolchain','renderer_ref':r['ref'],'renderer_source':source,'renderer_version':m.group(1),'renderer_variant':m.group(2),'playwright':f.get('devDependencies',{}).get('@playwright/test'),'storybook':{n:s.get('devDependencies',{}).get(n) for n in names},'config':'frontend/playwright.storybook.config.ts','snapshots':'frontend/e2e/ui/ui-foundation.visual.spec.ts-snapshots'}
def check(root=ROOT):
    expected=json.loads((root/LOCK).read_text());actual=current(root)
    if expected!=actual:raise ValueError('visual toolchain drift; baseline review required')
