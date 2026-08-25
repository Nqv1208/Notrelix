#!/usr/bin/env python3
from __future__ import annotations
import argparse, hashlib, json, os, re
from datetime import datetime, timezone
from pathlib import Path
from delivery_model import ROOT, github_output

SAFE = re.compile(r"[^A-Za-z0-9._-]+")


def slug(value: str) -> str:
    return SAFE.sub("-", value).strip("-").lower()


def main() -> int:
    p = argparse.ArgumentParser()
    p.add_argument("--proof-id", required=True)
    p.add_argument("--component-id", default="repository")
    p.add_argument("--status", choices=["passed", "failed"], default="passed")
    p.add_argument("--metadata", action="append", default=[])
    p.add_argument("--output-dir", type=Path, default=Path("artifacts/ci/evidence"))
    args = p.parse_args()
    meta = {}
    for item in args.metadata:
        if "=" not in item:
            raise SystemExit(f"metadata must be key=value: {item}")
        key, value = item.split("=", 1)
        meta[key] = value
    source_sha = os.environ.get("GITHUB_SHA", "local")
    run_id = os.environ.get("GITHUB_RUN_ID", "local")
    attempt = os.environ.get("GITHUB_RUN_ATTEMPT", "1")
    record = {
        "schema_version": 1,
        "proof_id": args.proof_id,
        "component_id": args.component_id,
        "status": args.status,
        "source_sha": source_sha,
        "run_id": run_id,
        "run_attempt": attempt,
        "workflow": os.environ.get("GITHUB_WORKFLOW", "local"),
        "job": os.environ.get("GITHUB_JOB", "local"),
        "created_at": datetime.now(timezone.utc).isoformat(),
        "metadata": meta,
    }
    canonical = json.dumps(record, separators=(",", ":"), sort_keys=True)
    record["record_sha256"] = hashlib.sha256(canonical.encode()).hexdigest()
    outdir = args.output_dir if args.output_dir.is_absolute() else ROOT / args.output_dir
    outdir.mkdir(parents=True, exist_ok=True)
    name = f"{slug(args.proof_id)}--{slug(args.component_id)}.json"
    path = outdir / name
    path.write_text(json.dumps(record, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    github_output("path", str(path))
    github_output("artifact_name", f"ci-evidence-{slug(args.proof_id)}-{slug(args.component_id)}")
    print(path)
    return 0

if __name__ == "__main__":
    raise SystemExit(main())
