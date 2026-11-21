#!/usr/bin/env bash
# Scan every image in delivery/images.lock.toml with the release gate's Trivy settings (fixed HIGH/CRITICAL, os+library,
# no ignore file) in one pass, print one table, exit 1 if any image has findings. Run before cutting or pushing release/X.Y.
#   scripts/release-preflight.sh             scan all lock images
#   scripts/release-preflight.sh --list      print "id ref" per lock image
#   scripts/release-preflight.sh --self-test assert --list matches the lock
set -euo pipefail
cd "$(dirname "$0")/.."
LOCK=delivery/images.lock.toml
# trivy 0.76.2, used only when no local trivy is installed
TRIVY_IMAGE=aquasec/trivy@sha256:8f6bb947dbc528902e11f2f5be52f7b519f6a39d5d148f41166f2040499768fb

list() { python3 -I -c 'import sys,tomllib
for i,v in tomllib.load(open(sys.argv[1],"rb"))["images"].items(): print(i,v["ref"])' "$LOCK"; }

case "${1:-}" in
  --list) list; exit 0 ;;
  --self-test)
    n=$(list | wc -l | tr -d ' '); want=$(grep -c '^ref = ' "$LOCK")
    [[ "$n" -gt 0 && "$n" == "$want" ]] && list | awk 'NF!=2||$2!~/@sha256:[0-9a-f]{64}$/{bad=1} END{exit bad}' \
      || { echo "self-test FAIL: list=$n lock=$want or a ref is not digest-pinned" >&2; exit 1; }
    echo "self-test PASS ($n images)"; exit 0 ;;
  "") ;;
  *) echo "usage: $0 [--list|--self-test]" >&2; exit 2 ;;
esac

if command -v trivy >/dev/null; then trivy=(trivy)
else
  command -v docker >/dev/null || { echo "need trivy or docker" >&2; exit 2; }
  trivy=(docker run --rm -v "$HOME/.cache/trivy:/root/.cache/trivy" "$TRIVY_IMAGE")
  mkdir -p "$HOME/.cache/trivy"
fi

out=$(mktemp -d); trap 'rm -rf "$out"' EXIT
rc=0
while read -r id ref; do
  echo "scanning $id ..." >&2
  if ! "${trivy[@]}" image -q --timeout 15m --scanners vuln --pkg-types os,library --severity HIGH,CRITICAL --ignore-unfixed -f json "$ref" > "$out/$id.json" 2> "$out/$id.err"; then
    echo "scan error for $id:" >&2; tail -5 "$out/$id.err" >&2; echo '{"ScanError":true}' > "$out/$id.json"
  fi
done < <(list)

python3 -I - "$out" <<'EOF' || rc=$?
import json, sys, pathlib
rows, bad = [], False
for f in sorted(pathlib.Path(sys.argv[1]).glob("*.json")):
    d = json.loads(f.read_text())
    if d.get("ScanError"):
        rows.append((f.stem, "ERROR", "-", "-")); bad = True; continue
    v = [x for r in d.get("Results") or [] for x in r.get("Vulnerabilities") or []]
    c = sum(x["Severity"] == "CRITICAL" for x in v); h = sum(x["Severity"] == "HIGH" for x in v)
    pk = ",".join(sorted({x["PkgName"] for x in v})[:6])
    rows.append((f.stem, "FAIL" if v else "ok", f"{c}/{h}", pk)); bad |= bool(v)
print(f'{"image":<16}{"result":<8}{"crit/high":<11}packages')
for r in rows: print(f"{r[0]:<16}{r[1]:<8}{r[2]:<11}{r[3]}")
sys.exit(1 if bad else 0)
EOF
exit "$rc"
