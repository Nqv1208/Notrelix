#!/usr/bin/env python3
"""Verify that required backend internal proofs actually executed in a TRX run.

Consumes the backend-owned proof registry (backend/tests/ci-proofs.json) and a
single TRX result file, and proves for every proof in the requested profile
that its mapped test selectors executed and passed. Registry/well-formedness
violations and unexecuted or failed proofs fail closed with a deterministic
JSON summary.
"""
from __future__ import annotations

import argparse
import json
import re
import sys
import xml.etree.ElementTree as ET

API_VERSION = "ci.notrelix.dev/backend-proofs/v1"
SELECTOR_RE = re.compile(r"^[A-Za-z_][A-Za-z0-9_]*(\.[A-Za-z_][A-Za-z0-9_]*)+$")


class VerifyError(Exception):
    pass


def _no_duplicate_keys(pairs):
    seen = {}
    for key, value in pairs:
        if key in seen:
            raise VerifyError(f"duplicate registry key: {key}")
        seen[key] = value
    return seen


def load_registry(path):
    try:
        with open(path, encoding="utf-8") as handle:
            registry = json.load(handle, object_pairs_hook=_no_duplicate_keys)
    except VerifyError:
        raise
    except (OSError, json.JSONDecodeError) as error:
        raise VerifyError(f"malformed registry: {error}") from error
    if not isinstance(registry, dict) or registry.get("api_version") != API_VERSION:
        raise VerifyError(f"registry api_version must be {API_VERSION}")
    profiles = registry.get("profiles")
    proofs = registry.get("proofs")
    if not isinstance(profiles, dict) or not isinstance(proofs, dict):
        raise VerifyError("registry must define profiles and proofs objects")
    if not profiles:
        raise VerifyError("registry defines no profiles")
    for name, proof_ids in profiles.items():
        if not isinstance(proof_ids, list) or not proof_ids:
            raise VerifyError(f"profile {name} is empty")
        for proof_id in proof_ids:
            if proof_id not in proofs:
                raise VerifyError(f"profile {name} references unknown proof {proof_id}")
    for proof_id, proof in proofs.items():
        tests = proof.get("tests") if isinstance(proof, dict) else None
        if not isinstance(tests, list) or not tests:
            raise VerifyError(f"proof {proof_id} has empty tests mapping")
        for selector in tests:
            if not isinstance(selector, str) or not SELECTOR_RE.fullmatch(selector):
                raise VerifyError(f"proof {proof_id} has malformed selector {selector!r}")
    return registry


def load_trx_results(path):
    try:
        root = ET.parse(path).getroot()
    except (OSError, ET.ParseError) as error:
        raise VerifyError(f"malformed TRX: {error}") from error
    results: dict[str, str] = {}
    for node in root.iter():
        if not node.tag.endswith("UnitTestResult"):
            continue
        name = node.attrib.get("testName")
        outcome = node.attrib.get("outcome")
        if not name or not outcome:
            raise VerifyError("malformed TRX: UnitTestResult missing testName/outcome")
        previous = results.get(name)
        if previous is not None and previous != outcome:
            raise VerifyError(f"ambiguous TRX result for {name}: {previous} vs {outcome}")
        results[name] = outcome
    return results


def verify(registry, profile, results):
    if profile not in registry["profiles"]:
        raise VerifyError(f"unknown profile: {profile}")
    proof_summary = {}
    matched_any = False
    failures: list[str] = []
    for proof_id in registry["profiles"][profile]:
        selectors = registry["proofs"][proof_id]["tests"]
        executed = 0
        status = "passed"
        for selector in selectors:
            matches = [
                (name, outcome)
                for name, outcome in results.items()
                if name == selector or name.startswith(selector + ".")
            ]
            if not matches:
                failures.append(f"{proof_id}: selector did not execute: {selector}")
                status = "failed"
                continue
            matched_any = True
            executed += len(matches)
            for name, outcome in matches:
                if outcome != "Passed":
                    failures.append(f"{proof_id}: selector failed ({outcome}): {name}")
                    status = "failed"
        proof_summary[proof_id] = {"selectors": len(selectors), "executed": executed, "status": status}
    if registry["profiles"][profile] and not matched_any:
        failures.append(f"profile {profile} executed zero required proofs")
    return proof_summary, failures


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--registry", required=True)
    parser.add_argument("--profile", required=True)
    parser.add_argument("--trx", required=True)
    parser.add_argument("--output", help="optional path for the JSON summary")
    args = parser.parse_args()

    try:
        registry = load_registry(args.registry)
        results = load_trx_results(args.trx)
        proof_summary, failures = verify(registry, args.profile, results)
    except VerifyError as error:
        print(f"::error::[verify-required-proofs-trx] {error}", file=sys.stderr)
        return 1

    summary = {
        "api_version": API_VERSION,
        "registry": args.registry,
        "profile": args.profile,
        "trx": args.trx,
        "proofs": dict(sorted(proof_summary.items())),
        "failures": sorted(failures),
        "ok": not failures,
    }
    rendered = json.dumps(summary, indent=2, sort_keys=True)
    if args.output:
        with open(args.output, "w", encoding="utf-8") as handle:
            handle.write(rendered + "\n")
    print(rendered)
    if failures:
        for failure in failures:
            print(f"::error::[verify-required-proofs-trx] {failure}", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
