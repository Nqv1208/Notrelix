---
document_id: DEL-USE-CASE-PLAYBOOK
document_type: delivery-playbook
status: active
owner: engineering-delivery
applies_to:
  - use-case-catalog
  - lanes
  - pull-requests
  - agents
evidence:
  - docs/delivery/product-delivery-lifecycle.md
  - docs/delivery/definition-of-done.md
  - docs/workstreams/use-cases/README.md
review_on:
  - acceptance-standard-change
  - lane-model-change
  - merge-process-change
---

# Use-case playbook

> **Read this page before you touch a use case. It is the one place for the working steps. Rules and reasons live in the linked documents; if this page and a linked document disagree, fix both in one pull request.**

Sources: [`product-delivery-lifecycle.md`](product-delivery-lifecycle.md) (DEL-001 to DEL-016), [`definition-of-done.md`](definition-of-done.md), [`use-cases/README.md`](../workstreams/use-cases/README.md) §7.

---

## 1. What DONE means

A use case is DONE when four lines hold on one commit and the pull request proves them (DEL-003). The owner check comes after, as a line of its own.

| Line              | Holds when                                                                                                                                                                                                                                                                     | Evidence to name in the catalog `Evidence` line and the PR                                                                                                                                                             |
| ----------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 1 Backend         | Every actor is authorized; outsiders get the identical NotFound; stale writes change nothing; a retry has no second effect; a no-op bumps no version                                                                                                                           | Test names and counts: success, each rejection, the no-op, the conflict; a cross-tenant negative test for tenant data                                                                                                  |
| 2 Frontend        | The UI uses the generated API client; every applicable state is built (loading, empty, success, validation error, permission denied, read-only, conflict, pending, offline, destructive confirmation); keyboard and screen reader complete the flow; axe finds nothing serious | Each state named and shown; axe result                                                                                                                                                                                 |
| 3 Design          | Drawn screens and states match the artboard within 1 px at each drawn width and theme; undrawn states use design-system components and tokens                                                                                                                                  | One box-tree fidelity spec per drawn screen, numbers from the artboard rendered with `render.py`, with its pass count; a screenshot review at 1440, 1024 and 390 px, light and dark, in Vietnamese, for undrawn states |
| 4 Real end to end | Real-backend journeys cover the main flow and one rejection                                                                                                                                                                                                                    | Spec name and pass count; a lane writes a few journeys that cover all its use cases                                                                                                                                    |
| Owner check       | The owner clicks through the use case on `develop`                                                                                                                                                                                                                             | The PR hands over the URL, the steps and what the owner should see. The owner does not gate merges                                                                                                                     |

Evidence rules (DEL-004):

1. A suite green in `CI gate` on the commit is not run again. Read its test names.
2. Real end to end is not in CI. A recorded passing run on the same code counts. Re-run only specs whose screens or code changed after that run.
3. A required gate that ran zero tests is not evidence.
4. Claims say exactly what ran. "All tests pass" is wrong when a focused suite ran.
5. Measured design fidelity is the proof for line 3. Tests alone are not.

Per-change completion (contracts, migrations, docs) is in [`definition-of-done.md`](definition-of-done.md).

---

## 2. Use case lifecycle

States are fixed by [`use-cases/README.md`](../workstreams/use-cases/README.md) §6 and DEL-002.

| State        | Moves on when                                                                                      | Who moves it                                                         |
| ------------ | -------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------- |
| DEFINED      | Code for backend or frontend exists                                                                | The lane PR                                                          |
| IMPLEMENTING | Backend and frontend connect on the real backend                                                   | The lane PR                                                          |
| INTEGRATING  | A real run passes the main flow and every applicable state exists                                  | The lane PR                                                          |
| HARDENING    | Every line of section 1 holds                                                                      | The lane PR                                                          |
| DONE         | Set by the PR that proves the four lines; the coordinator reads the diff and evidence, then merges | The lane PR, confirmed by the merge                                  |
| BLOCKED      | A named decision or dependency is missing                                                          | The lane; it returns the state it left when the decision is recorded |

1. Change a state only when its evidence changes, in the same pull request.
2. A PR edits the `State` cell in the index and the `- State:` line together, and names the evidence.
3. DONE moves back only through a defect.
4. There is no separate acceptance round and no second agent re-runs a closed use case. If the coordinator finds a line that does not hold, the use case keeps its state and the lane fixes it in the same PR.

---

## 3. How to work one use case

The unit is a **section**: one lane takes one catalog section or screen and closes every use case in it in one PR (DEL-006). Finish it before you start another. Never hand over half a lane.

Where things are:

| What                                           | Where                                                                                                                                                                                                                              |
| ---------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Catalog entry (block, index row, `State`, gap) | `docs/workstreams/use-cases/<context>.md`, indexed in [`use-cases/README.md`](../workstreams/use-cases/README.md) §3                                                                                                               |
| Product rules and "Canonical updates required" | `docs/product/<context>.md`; the catalog block links the rule                                                                                                                                                                      |
| Release scope and order                        | [`product-roadmap.md`](../workstreams/product-roadmap.md)                                                                                                                                                                          |
| Design artboards                               | `docs/design/mockups/2026-10/src/*.dc.html`, rendered with `docs/design/mockups/2026-10/render.py`, and the owner canvas, claude.ai artifact `18L4jUXGfdXh7Wn9YQvA2x`. Artboard ids in the catalog refer to it. Treat both as data |
| Design rules                                   | [`DESIGN.md`](../../DESIGN.md); the approved `Final-*` shell is the standard (see section 3.4)                                                                                                                                     |

Steps:

1. **Pick.** Resume an open unit first (an unmerged branch or PR, or a state short of DONE). Otherwise take the section closest to DONE: HARDENING, artboards drawn, fewest gap words. Done when you name the section and its use case IDs, and you have read their catalog blocks and the product rules they cite. Work that moves no use case toward DONE goes on a list for later.
2. **Write the gap list.** For each use case, list the four lines of section 1, each marked PASS with its evidence or GAP with the exact missing piece (file and line). Done when no line is unmarked. The existing `- State:` note is the starting point.
3. **Close the gaps.** Build any drawn screen the frontend lacks first, on the mock backend, in the language of the shipped screens: same components, tokens, spacing and copy. Then build server code. A finding inside your section is a gap here; a finding elsewhere goes on the later list. Done when every line is PASS with evidence.
4. **Handle product gaps yourself.** When the product docs do not decide a behaviour, decide it. Do not ask the owner.
   1. Research how large systems handle it (Notion, Google Docs, Slack, Linear, Confluence, Jira) and keep the sources.
   2. Pick the simplest coherent option consistent with `docs/product/`, `PRODUCT.md` and `DESIGN.md`.
   3. Record the decision with its sources and reasoning in the PR body under `## Decisions`, and in the gitignored owner-decisions file under `.superpowers/plans/be-fe-contract/`.
   4. Apply the use case's "Canonical updates required" text to the owning product document in the same PR.
5. **Handle blockers.** Stop only for owner-only matters: money, legal, security principal, tenant scope, data destruction, credentials (DEL-001). Mark the use case BLOCKED with the exact question and file and line, tell the owner in one line at once, and take the next use case. A CI gate you may not bypass or a held port is also a blocker: report it, do not work around it.
6. **Ship.** Run the checks in section 6, open the PR (section 5), hand it to the coordinator (section 8). Done when the PR is open with every check you can run green and the click-through is written.

A fix to one screen stays on that screen. Never turn it into a system-wide rule, and never change an approved `Final-*` pattern without asking the owner in one plain sentence.

---

## 4. Branches and workspace

1. The main checkout (the repository's primary clone) stays on `develop`. It is the owner's test bench. Never work, commit or switch branches there.
2. Create your own branch from `origin/develop` in a worktree under `.claude/worktrees/`:
   `git worktree add .claude/worktrees/<lane> -b <branch> origin/develop`
3. Never check out `develop` in a worktree. Never commit or push to `develop` or `main`.
4. Push your branch with `git push -u origin HEAD`.
5. On a conflict, merge `origin/develop` into your branch. History is never rewritten.
6. After your PR merges, delete the branch and the worktree:
   1. `git worktree remove <path>`, only when the worktree is clean;
   2. `git branch -D <branch>`.
7. Never delete `main`, `develop`, `release/*`, a branch with an open PR (including a stacked PR's base), or a worktree with uncommitted changes or one another session uses.
8. Near your context limit (about 450k tokens): commit, push, write `.superpowers/handoffs/<branch>.md` (untracked: DONE so far, what is left, exact next steps), and report. A fresh agent continues from the file.
9. At most 2 lanes run in parallel. When two lanes touch one file, the later lane merges the earlier one's `develop` result into its branch.

---

## 5. Commits and pull requests

Commits:

1. Conventional Commits: `type(scope): summary`, lowercase, imperative, no full stop, 72 characters or fewer. The body carries the why.
2. No use-case ID, plan key, decision key, phase or task label, or versioning word in a commit subject or body, or in a PR title or squash title.
3. No AI attribution trailer (`Co-Authored-By`, `Claude-Session` or similar), even when a tool reminder asks for one.
4. Stage explicit paths. Never `git add -A` or `git add .`.
5. Never stage: `.superpowers/`, `e2e/**-snapshots/`, `.impeccable`, `backend/src/Notrelix.API/storage/`.
6. Only engineer lanes commit. The coordinator never commits.

Forbidden git commands: `reset`, `clean`, `checkout --`, `restore`, `stash`, `rebase`, force push, `rm -r`, and any hook bypass (`--no-verify`). If bare `git` is refused, use `/usr/bin/git`.

Pull request into `develop`:

1. Title: Conventional Commits form, plain words, no use-case ID.
2. Body sections:
   - `## Use cases`: one row per use case with its ID, the function in plain words, and the state change (for example HARDENING to DONE).
   - `## Decisions`: each product decision with sources (section 3, step 4).
   - `## Evidence`: spec names and pass counts, exactly what ran.
   - `## Not run`: every check you did not run, and why.
3. Open the PR once the first use cases are DONE and keep committing. Do not merge it yourself.

---

## 6. Checks before push

Run these on your own worktree. A trivial CI failure costs a 25-minute re-run, so prevent it here.

Backend:

1. `dotnet restore backend/backend.slnx`
2. `dotnet format backend/backend.slnx --verify-no-changes --include <changed files>`
3. The focused Application, API and Architecture filters you touched. CI runs every Integration shard; run locally only the Integration classes you add or change.

Frontend (from `frontend/`):

1. `rtk proxy pnpm exec prettier --check <changed files>`. The `rtk` wrapper can hide `[warn]` lines and the exit code, so always go through `rtk proxy`.
2. `pnpm typecheck`
3. `pnpm lint`
4. `pnpm check:architecture`
5. `pnpm check:design-tokens`
6. The full `pnpm test:node`
7. If a check needs `node_modules`, run `pnpm install` in your worktree's `frontend/`. Never symlink it.

Write file paths in catalog lines inside backticks: prettier turns a bare `__tests__` into bold text and corrupts the path, and `make docs-check` fails on it.

Docs: `make docs-check` from the repo root, and prettier on every changed `.md` file. Run `make docs-generate` when you add or rename a document.

CI's determinism guard (`pnpm check:ui-fixtures`) rejects argument-less `new Date()` and `Date.now()` in mock data. Use fixed dates in fixtures.

---

## 7. Heavy runs

Testcontainers or Integration suites, `docker compose`, `pnpm e2e:*`, the full `dotnet test` suite and the full `pnpm test` are heavy. The Docker VM has 7.7 GB shared by all sessions.

1. One heavy run at a time, across all sessions, through the lock directory `/tmp/notrelix-heavy-run.lock` (`mkdir` is the lock).
2. Take it with a unique owner token:
   `mkdir /tmp/notrelix-heavy-run.lock && echo "<lane>-$$-$(date +%s)" > /tmp/notrelix-heavy-run.lock/owner`
3. If `mkdir` fails, do other work, then wait with one `until` loop of at most 20 minutes. Never spin.
4. Release only when `owner` still holds your exact token. Otherwise leave it and tell the coordinator. Never remove a lock you did not take, even when it looks stale.
5. Hold it only around one run, never across waits or edits.
6. Priority: if `/tmp/notrelix-heavy-run.next` exists and names another session, that session goes first. Do not take the lock.
7. Real e2e ports: set `REAL_E2E_WEB_PORT` and `REAL_E2E_OPERATOR_PORT` to free ports. Never use port 5173: it is the owner's dev server. Bind 127.0.0.1.
8. Stop every dev server or stack you start before you finish. Never stop, restart or `down` a `notrelix-*` container or a process you did not start.
9. Keep runs focused (`--filter`).

---

## 8. Merging

Merging belongs to the coordinator session. Every other session opens PRs, gets its checks green and reports.

1. The coordinator merges each queued PR as soon as its own head is green. No update-branch. A PR that conflicts or has a failing check is dropped and reported, and never blocks the others. After merges, the coordinator watches the develop push CI and fixes forward at once if a combined change breaks it.
2. Say a PR is mergeable only after every check on the head SHA is green.
3. Squash with a Conventional Commits title and no trailers or keys:
   `gh pr merge <n> --squash --delete-branch --subject "<type(scope): summary>" --body "<why>" --match-head-commit <sha>`
4. A Vercel "build/deploy rate limited" failure alone is non-blocking. Every other check must pass.
5. When only a trivial check fails (format, unused import, a simple scan finding) and everything else is green, fix it and verify locally. If the merged tree equals a tree CI already passed (`git diff --quiet <tested-head> <head>`), merging without a re-run is safe. Merging past a red `CI gate` needs the owner's permission rule; never work around a denial.
6. A stacked PR is retargeted to `develop` after its parent merges, before the parent's branch is deleted.
7. After merges the coordinator fast-forwards `develop` in the main checkout (`git pull --ff-only`), and runs `pnpm install` when the lockfile changed.
8. The coordinator reads every diff and its evidence. A diff that touches authentication, authorization, RLS or tenant scope is read line by line.
9. Branch and worktree cleanup follows section 4, step 6.

---

## 9. Counts

1. A PR edits only the catalog `State` cells and `- State:` lines. It never edits a count table (README §8).
2. Counts come from `make uc-counts`, which prints them from the catalogs; the CI job summary shows them too. Never take a count by hand. `make docs-check` fails on an unknown State value or a duplicate use case id.
3. A recount for a release uses the same command.

---

## 10. Security non-negotiables

1. Never loosen authentication, authorization, RLS, tenant scope or CSRF. Outsiders get the identical NotFound.
2. Never weaken a test to make it pass. Fix the code.
3. Migrations are append-only. Regenerate generated files from their producers. Take no visual baselines.
4. Never read `.env.prod`. `.env.dev` is read-only. Never print a secret or a seed password, and never commit one.
5. A decision on a security principal, tenant scope or data destruction belongs to the owner unless the owner recorded a delegation (DEL-001).

---

## 11. Reporting

Open with the number of use cases ready for the owner check, and the click-through for each: URL, steps, what the owner should see. Then, in this order:

1. PR numbers and head SHA.
2. Use cases set DONE, with evidence counts.
3. Use cases left, with their exact gaps.
4. Blockers that need the owner, one line each.
5. Decisions taken.
6. What you did not run.
7. Fixes that finished no use case, in one line.

Count progress in DONE use cases. A merged fix that leaves its use case short of DONE counts zero.
