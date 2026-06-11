import re
from collections import defaultdict

errors = defaultdict(list)
with open('build_errors.txt', 'r') as f:
    for line in f:
        match = re.search(r'(/Users/nqvinh/[^:\s]+)\((\d+),\d+\):\s+(error|warning)\s+(\w+):\s+(.*)', line)
        if match:
            filepath, line_no, severity, code, msg = match.groups()
            # simplify path
            relpath = filepath.split('/backend/')[-1] if '/backend/' in filepath else filepath
            errors[relpath].append((line_no, severity, code, msg))

with open('inspect_results.txt', 'w') as out:
    for path in sorted(errors.keys()):
        out.write(f"=== {path} ===\n")
        for line_no, severity, code, msg in errors[path]:
            out.write(f"  Line {line_no} [{severity} {code}]: {msg}\n")

