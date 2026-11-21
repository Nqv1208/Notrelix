#!/usr/bin/env bash
# ci-triage.sh <pr-number> — triage the latest "Notrelix CI" run on a PR's head.
# Prints every job and step as PASS / FAIL / SKIP / WAIT, tags each step LIGHT or HEAVY,
# and ends with one verdict line: GREEN, LIGHT-ONLY, HEAVY-FAILED, HEAVY-UNPROVEN or RUNNING.
set -euo pipefail
pr="${1:?usage: ci-triage.sh <pr-number>}"

head=$(gh pr view "$pr" --json headRefOid -q .headRefOid)
run=$(gh run list --commit "$head" --workflow "Notrelix CI" --limit 1 --json databaseId -q '.[0].databaseId')
[ -n "$run" ] || { echo "VERDICT: RUNNING (no CI run on ${head:0:9} yet)"; exit 0; }

# LIGHT = a rule or formatting check fixed by a small edit. Everything else is HEAVY.
light_re='format|prettier|lint|i18n|mock-consumer-surface|codegen:check|OpenAPI contract has no drift|check:architecture'

gh run view "$run" --json status,jobs -q '
  "STATUS \(.status)",
  (.jobs[] | select(.name != "CI gate") | . as $j
    | "JOB \($j.conclusion | if . == "" or . == null then "running" else . end) \($j.name)",
      ($j.steps[] | select(.name | test("^(Set up|Post |Complete job|Run actions/|Checkout)") | not)
        | "STEP \(.conclusion | if . == "" or . == null then "pending" else . end) \($j.name) :: \(.name)"))' |
awk -v re="$light_re" '
  /^STATUS/ { status=$2; next }
  /^JOB/    { print; next }
  /^STEP/   {
    c=$2; line=$0; sub(/^STEP [a-z_]+ /, "", line)
    kind = (line ~ re) ? "LIGHT" : "HEAVY"
    tag  = (c=="success") ? "PASS" : (c=="failure") ? "FAIL" : (c=="skipped") ? "SKIP" : "WAIT"
    printf "  %-4s %-5s %s\n", tag, kind, line
    if (tag=="FAIL" && kind=="HEAVY") hf++
    if (tag=="FAIL" && kind=="LIGHT") lf++
    if (tag=="SKIP" && kind=="HEAVY") hs++
    if (tag=="WAIT") w++
  }
  END {
    if (status != "completed" || w) v="RUNNING (wait for the run to finish)"
    else if (hf)       v="HEAVY-FAILED (wait: fix, push, let CI re-run)"
    else if (lf && hs) v="HEAVY-UNPROVEN (a light fail skipped heavy steps: run them locally or re-run CI)"
    else if (lf)       v="LIGHT-ONLY (fix the light steps, prove them locally, merge)"
    else               v="GREEN (merge)"
    print "VERDICT: " v
  }'
