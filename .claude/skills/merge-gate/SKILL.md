---
name: merge-gate
description: Notrelix merge decision for a pull request into develop. Use before merging any PR, when CI on a PR is red or still running, or when develop goes red right after a merge.
---

# Merge gate

The owner's rule (2026-11-17): read CI before you merge.
- If the heavy checks passed and only light ones failed, fix the light ones and merge at once with `--admin`.
- If a heavy check failed, wait for a green re-run.
- If two PRs are each green but break develop together, open a fix-forward PR.

**Light** means a rule or formatting check that a small edit fixes:
- `dotnet format`, including IDE0005 unused usings
- `format:check` / prettier
- lint
- `check:i18n`
- `check:architecture`
- `check:mock-consumer-surface`
- `codegen:check`
- OpenAPI drift

**Heavy** means a check that proves behaviour:
- typecheck and the backend build
- `test:node`, `test:web` and the app build
- backend unit tests
- every Integration shard
- the dependency audit

The single source of truth for this split is the `light_re` pattern in `ci-triage.sh`. Edit it there.

## Steps

1. **Read the diff.** Read all of it. Read closely anything that touches auth, RLS, tenant scope, migrations or tests. A weakened assertion blocks the merge whatever CI says.
   Done when you can name what the PR changes and every test it alters.

2. **Triage CI.** Run `.claude/skills/merge-gate/ci-triage.sh <pr>`. It lists every step as PASS/FAIL/SKIP and LIGHT/HEAVY, then prints one verdict.
   Done when you have the verdict for the PR's current head.

3. **Act on the verdict.**

   | Verdict | Action |
   |---|---|
   | `GREEN` | Merge. |
   | `LIGHT-ONLY` | Fix the light steps on the branch and run each one locally, recording its exit code. Push, then merge with `--admin` without waiting for the re-run. |
   | `HEAVY-UNPROVEN` | A light failure skipped a heavy step, so that heavy step proved nothing. Run the skipped heavy steps (the SKIP lines) locally and record their exit codes. If all are 0, treat it as `LIGHT-ONLY`; otherwise as `HEAVY-FAILED`. |
   | `HEAVY-FAILED` | Wait. Fix the cause, push, and let CI re-run. Merge only on `GREEN` or `LIGHT-ONLY`. |
   | `RUNNING` | Wait for the run. Spend the time on step 1. |

   Done when the PR is merged, or is explicitly waiting on a named heavy failure.

4. **Merge.** Run `gh pr merge <pr> --squash --admin --delete-branch --match-head-commit <sha>`. Give it a Conventional Commits subject and no AI trailers. Then fast-forward develop in the main checkout, remove the PR's worktree and local branch, and start the next task.

5. **Watch develop.** After the merge, triage develop's own run (`gh run list --branch develop --workflow "Notrelix CI" --limit 1`).
   If develop is red although each merged PR was green on its own, two PRs conflict in meaning. Open one fix-forward PR at once:
   - name both PRs in its body;
   - fix the code, never the test's intent;
   - merge it through this same gate.

   Done when develop's latest run is green, or a fix-forward PR is open for every red step.

## Local proof a lane brings

A PR arrives with exit codes for the checks it can affect. If it touches OpenAPI, the mock backend or `enabled-consumer-surface.ts`, that proof includes:
- the full `pnpm test:node`
- `check:mock-consumer-surface`
- `codegen:check`

These are the checks where light-only local runs have missed contract tests before.
