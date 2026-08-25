#!/usr/bin/env python3
from __future__ import annotations
import argparse, json, shutil, tarfile
from pathlib import Path
from delivery_model import ROOT, component, load_catalog


def main() -> int:
    p = argparse.ArgumentParser()
    p.add_argument("--component", required=True)
    p.add_argument("--output", required=True, type=Path)
    args = p.parse_args()
    cfg = component(load_catalog(ROOT), args.component)
    paths = cfg.get("frontend", {}).get("artifact_paths", [])
    if not paths:
        raise SystemExit(f"component {args.component} has no frontend artifact_paths")
    frontend = ROOT / "frontend"
    missing = [p for p in paths if not (frontend / p).exists()]
    if missing:
        raise SystemExit(f"missing build artifact paths for {args.component}: {missing}")
    args.output.parent.mkdir(parents=True, exist_ok=True)
    with tarfile.open(args.output, "w:gz") as tar:
        for rel in paths:
            src = frontend / rel
            tar.add(src, arcname=rel)
    print(args.output)
    return 0

if __name__ == "__main__":
    raise SystemExit(main())
