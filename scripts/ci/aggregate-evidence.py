#!/usr/bin/env python3
from __future__ import annotations
import argparse, hashlib, json, os
from pathlib import Path


def main() -> int:
    p = argparse.ArgumentParser()
    p.add_argument("--plan", required=True, type=Path)
    p.add_argument("--evidence-dir", required=True, type=Path)
    p.add_argument("--output", required=True, type=Path)
    args = p.parse_args()
    plan = json.loads(args.plan.read_text(encoding="utf-8"))
    expected = set(plan.get("expected_proofs", []))
    source_sha = os.environ.get("GITHUB_SHA")
    run_id = os.environ.get("GITHUB_RUN_ID")
    records = []
    by_proof = {}
    for path in args.evidence_dir.rglob("*.json"):
        try:
            rec = json.loads(path.read_text(encoding="utf-8"))
        except json.JSONDecodeError:
            continue
        if "proof_id" not in rec:
            continue
        if source_sha and rec.get("source_sha") not in {source_sha, "local"}:
            raise SystemExit(f"stale evidence source SHA in {path}: {rec.get('source_sha')} != {source_sha}")
        if run_id and rec.get("run_id") not in {run_id, "local"}:
            raise SystemExit(f"foreign evidence run id in {path}: {rec.get('run_id')} != {run_id}")
        proof = rec["proof_id"]
        if proof in by_proof:
            raise SystemExit(f"duplicate evidence for proof {proof}")
        by_proof[proof] = rec
        records.append(rec)
    missing = sorted(expected - set(by_proof))
    failed = sorted(proof for proof in expected if by_proof.get(proof, {}).get("status") != "passed")
    unexpected = sorted(set(by_proof) - expected)
    summary = {
        "schema_version": 1,
        "source_sha": source_sha or "local",
        "run_id": run_id or "local",
        "plan_sha256": plan.get("plan_sha256"),
        "expected_proofs": sorted(expected),
        "observed_proofs": sorted(by_proof),
        "missing_proofs": missing,
        "failed_proofs": failed,
        "unexpected_proofs": unexpected,
        "status": "passed" if not missing and not failed and not unexpected else "failed",
    }
    canonical = json.dumps(summary, separators=(",", ":"), sort_keys=True)
    summary["summary_sha256"] = hashlib.sha256(canonical.encode()).hexdigest()
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(summary, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    print(json.dumps(summary, indent=2, sort_keys=True))
    if missing or failed or unexpected:
        raise SystemExit(1)
    return 0

if __name__ == "__main__":
    raise SystemExit(main())
