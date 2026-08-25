#!/usr/bin/env python3
from __future__ import annotations
import argparse, json
from pathlib import Path
from delivery_model import ROOT, compact_json, component, load_catalog


def main() -> int:
    p = argparse.ArgumentParser()
    p.add_argument("--input-dir", required=True, type=Path)
    p.add_argument("--output", required=True, type=Path)
    p.add_argument("--require-all-deployables", action="store_true")
    args = p.parse_args()
    catalog = load_catalog(ROOT)
    records = []
    seen = set()
    for path in sorted(args.input_dir.rglob("container-result-*.json")):
        data = json.loads(path.read_text(encoding="utf-8"))
        cid = data.get("component_id")
        if cid in seen:
            raise SystemExit(f"duplicate container result for {cid}")
        cfg = component(catalog, cid)
        if not cfg.get("deployable"):
            raise SystemExit(f"container result for non-deployable component {cid}")
        if data.get("published") and "@sha256:" not in data.get("image", ""):
            raise SystemExit(f"published result for {cid} is not digest pinned")
        seen.add(cid)
        records.append(data)
    if args.require_all_deployables:
        required = {cid for cid, cfg in catalog["components"].items() if cfg.get("deployable")}
        missing = sorted(required - seen)
        if missing:
            raise SystemExit(f"release candidate missing deployables: {missing}")
    manifest = {"schema_version": 4, "artifacts": sorted(records, key=lambda x: x["component_id"])}
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(manifest, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    print(compact_json(manifest))
    return 0

if __name__ == "__main__":
    raise SystemExit(main())
