"""Render a mockup .dc.html (from ./src) to a static HTML file for given props.

Usage (from anywhere):
    python3 -I docs/design/mockups/2026-10/render.py Final-Board.dc.html /tmp/board.html state=chi-xem mode=sang nav=an

Needs only the Python standard library and `node` on PATH. Reference tooling: the output is a
screenshot aid, never product code.
"""
import json
import os
import re
import subprocess
import sys
import tempfile

P = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'src') + os.sep

if len(sys.argv) < 3:
    sys.exit('usage: render.py <File.dc.html> <out.html> [key=val ...]')

f, out = sys.argv[1], sys.argv[2]
props = dict(a.split('=', 1) for a in sys.argv[3:])
with open(P + f, encoding='utf-8') as fh:
    s = fh.read()
m = re.search(r'<script type="text/x-dc"[^>]*data-props=\'([^\']*)\'>(.*?)</script>', s, re.S)
js = 'class DCLogic{}\n' + m.group(2) + f'\nconst c=new Component();c.props={json.dumps(props)};console.log(JSON.stringify(c.renderVals()));'

with tempfile.TemporaryDirectory() as tmpdir:
    tmp = os.path.join(tmpdir, 'render_%d.mjs' % os.getpid())
    with open(tmp, 'w', encoding='utf-8') as th:
        th.write(js)
    res = subprocess.run(['node', tmp], capture_output=True, text=True)
if res.returncode != 0:
    sys.exit('node failed:\n' + res.stderr)
v = json.loads(res.stdout)

head = re.search(r'<helmet>(.*?)</helmet>', s, re.S).group(1)
body = re.search(r'</helmet>(.*?)</x-dc>', s, re.S).group(1)


def get(expr, env):
    parts = expr.strip().split('.')
    cur = env.get(parts[0])
    for p in parts[1:]:
        cur = cur.get(p) if isinstance(cur, dict) else None
    return cur


def fmt(x):
    if isinstance(x, bool):
        return 'true' if x else 'false'
    return '' if x is None else str(x)


def render(t, env):
    # sc-for (outermost first, non-greedy matching by depth)
    while True:
        mm = re.search(r'<sc-for list="\{\{([\w.]+)\}\}" as="(\w+)"[^>]*>', t)
        if not mm:
            break
        depth, i = 1, mm.end()
        while depth:
            a = t.find('<sc-for', i)
            b = t.find('</sc-for>', i)
            if a != -1 and a < b:
                depth += 1
                i = a + 7
            else:
                depth -= 1
                i = b + 9
        inner = t[mm.end():i - 9]
        items = get(mm.group(1), env) or []
        t = t[:mm.start()] + ''.join(render(inner, dict(env, **{mm.group(2): it})) for it in items) + t[i:]
    pat = re.compile(r'<sc-if value="\{\{\s*([\w.!]+)\s*\}\}"[^>]*>((?:(?!<sc-if).)*?)</sc-if>', re.S)
    while True:
        n = pat.sub(lambda m: m.group(2) if get(m.group(1), env) else '', t)
        if n == t:
            break
        t = n
    return re.sub(r'\{\{\s*([\w.]+)\s*\}\}', lambda m: fmt(get(m.group(1), env)), t)


with open(out, 'w', encoding='utf-8') as oh:
    oh.write('<!doctype html><html><head><meta charset="utf-8">' + head + '</head><body>' + render(body, v) + '</body></html>')
