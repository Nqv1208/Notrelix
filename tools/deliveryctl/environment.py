from .model import load_authorities
from .runtime import ROOT
def resolve(name,root=ROOT):
    a=load_authorities(root)['environments'];
    if name not in a.get('environments',{}):raise ValueError(f'unknown environment: {name}')
    return {'name':name,**a.get('defaults',{}),**a['environments'][name]}
