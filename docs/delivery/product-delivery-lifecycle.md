---
document_id: DEL-PRODUCT-LIFECYCLE
document_type: delivery-policy
status: active
owner: engineering-delivery
applies_to:
  - repository
  - use-case-catalog
  - releases
  - lanes
  - acceptance
evidence:
  - PRODUCT.md
  - RULE.md
  - docs/delivery/definition-of-done.md
  - docs/delivery/release-and-rollout.md
  - docs/delivery/ci-cd-architecture.md
  - docs/workstreams/use-cases/README.md
  - docs/workstreams/product-roadmap.md
  - docs/quality/testing-strategy.md
review_on:
  - acceptance-standard-change
  - release-lifecycle-change
  - lane-model-change
  - defect-severity-change
  - release-scope-model-change
---

# Product Delivery Lifecycle

> **A use case is DONE when its pull request proves four lines and the lead merges it. A version is released when the owner has run its journeys on the release candidate and no P0 or P1 defect is open.**

This document owns how product work moves from the use-case catalog to a released version: who decides what, when a use case is DONE, how work is split into lanes, and how a version is accepted.

It does not own:

| Topic | Owner |
|---|---|
| What each release contains, journeys, order, dates | [`product-roadmap.md`](../workstreams/product-roadmap.md) (plan, not authority) |
| Use-case state words and catalog layout | [`use-cases/README.md`](../workstreams/use-cases/README.md) §4 to §6 |
| Per-change completion: contracts, migrations, security, docs | [`definition-of-done.md`](definition-of-done.md) |
| Branch and tag mechanics, version numbers, CI jobs | [`ci-cd-architecture.md`](ci-cd-architecture.md) §13 |
| Deployment, rollout, rollback | [`release-and-rollout.md`](release-and-rollout.md) |
| Product meaning and vision | [`PRODUCT.md`](../../PRODUCT.md) |

---

# 1. DEL-001 — Four roles, one decision each

| Role | Decides | Does not |
|---|---|---|
| Owner | Product, money and legal decisions; release scope; release acceptance | Read diffs |
| Lead | Coordination; product decisions the owner delegated (recorded in the decisions file); reads every diff and its evidence and merges | Build use cases |
| Lane engineer | Builds one lane and proves each use case in it | Set release scope; merge |
| Security reading | The lead reads line by line any diff that touches authentication, authorization, RLS or tenant scope | Delegate that read |

A decision that `AGENTS.md` §13 names a stop condition (security principal, tenant scope, data destruction, ambiguous product ownership) belongs to the owner unless the owner recorded a delegation.

---

# 2. DEL-002 — A use case moves through the catalog states

The state words are fixed by [`use-cases/README.md`](../workstreams/use-cases/README.md) §6. This table fixes the entry condition and the exit evidence.

| State | Entered when | Left with |
|---|---|---|
| DEFINED | The use-case block is written and reviewed | Code for backend or frontend exists |
| IMPLEMENTING | Some code exists; no end-to-end run | Backend and frontend connect |
| INTEGRATING | Both sides connect on the real backend | A real run passes the main flow and every applicable state exists |
| HARDENING | Runs end to end; gaps remain in states, design, tests or security proof | Every line of DEL-003 holds |
| DONE | Set by the use case's pull request (DEL-005) | Never moves back except through a defect |
| BLOCKED | A named owner decision or dependency is missing | The decision is recorded; the state returns to the one it left |

Move a state only when its evidence changes, in the same pull request.

---

# 3. DEL-003 — DONE has four lines

A use case is DONE only when all four hold on the same commit. [`definition-of-done.md`](definition-of-done.md) still applies to the change that sets DONE.

1. **Backend.** The server authorizes every actor and returns the identical NotFound to people without access. Competing writes carry the expected version, and a stale write changes nothing. A retry with the same idempotency key has no second effect. A semantic no-op bumps no version and emits nothing. Tests cover success, each rejection, the no-op and the conflict. Tenant-scoped data has a cross-tenant negative test.
2. **Frontend.** The UI uses the generated API client. Every applicable state is built: loading, empty, success, validation error, permission denied, read-only, conflict, pending, offline and destructive confirmation. Optimistic writes roll back on rejection. Keyboard and screen reader complete the flow, and axe reports no serious violation.
3. **Design.** Every screen and state that has an artboard matches it within 1 px, at each width and theme the artboard draws. Every state or width the canvas does not draw is built from design-system components and tokens on the pattern of the nearest drawn state and passes a screenshot review at 1440, 1024 and 390 px, light and dark, in Vietnamese: no overflow, clipping or squeezed columns, 44 px targets at 390 px, no serious axe violation. The design canvas is the owner's artifact `18L4jUXGfdXh7Wn9YQvA2x`; artboard ids in the catalog refer to it.
4. **Real end to end.** Real-backend journeys cover the main flow and one rejection of the use case. A lane writes a few journeys that together cover its use cases; one journey may cover several.

---

# 4. DEL-004 — Evidence is proportionate and named

- The use case's Evidence line names: the real e2e spec and its pass count, the design specs, and the screenshot review for undrawn states.
- Design proof is one spec per drawn screen, measured as a box-tree comparison. It is not one spec per use case.
- A suite that is green in `CI gate` on the commit is not run again for acceptance.
- A required gate that ran zero tests is not evidence (`NRX-016`).
- Claims state exactly what ran. "All tests pass" is not allowed when a focused suite ran.

---

# 5. DEL-005 — The pull request sets DONE; the lead merges

- The lane's pull request moves each use case to DONE in the catalog when DEL-003 holds, and applies the use case's "Canonical updates required" text to the owning product document.
- The lead reads the diff and the Evidence lines, and merges. There is no separate acceptance round and no second agent re-runs the use case.
- If the lead finds a line that does not hold, the use case stays at its state, the lead says which line, and the lane fixes it in the same pull request.
- The owner does not gate individual use cases. The owner accepts versions (DEL-012).

---

# 6. DEL-006 — A lane owns one capability group and ships one pull request

- A lane is a set of use cases of one capability group, named in the roadmap by context and capability letters. One engineer builds it, front to back, in one worktree and one branch.
- A lane ships one pull request into `develop`. The pull request lists its use cases (DEL-016).
- Finish one lane to its DONE lines before the engineer starts another. A half-done lane is not handed over.
- Defects found inside the lane's capability are fixed in the lane.

---

# 7. DEL-007 — Lane limits

| Limit | Value |
|---|---|
| Lanes in parallel | At most 3 |
| Heavy runs (Testcontainers, compose stack, real e2e, full suites) | One at a time, through a shared lock directory (`mkdir` is the lock) |
| Context of one engineer | Hand over near the context limit: commit, push, write the handoff file `.superpowers/handoffs/<lane>.md` (untracked), report; a fresh engineer continues from the file |
| Two lanes touching one file | The lead orders them; the later lane merges the earlier one's `develop` result into its branch (no history rewrite) |

---

# 8. DEL-008 — Report daily against the plan

- At the end of each day the lead reports DONE use cases against the roadmap plan for the current release.
- When DONE falls behind the plan by more than 15 use cases, the lead proposes a scope cut and the owner chooses (DEL-010).

---

# 9. DEL-009 — A release has six stages

| Stage | What happens | Exit |
|---|---|---|
| R0 Scope | Owner confirms the release's allocation in [`product-roadmap.md`](../workstreams/product-roadmap.md) and records the owner decisions the release needs | Allocation and decisions recorded |
| R1 Build | Lanes build and merge use cases (DEL-005 to DEL-007) | Use cases DONE on `develop` |
| R2 Scope closed | Every use case in scope is DONE; catalog counts re-taken with awk; only fixes land after this | All in-scope use cases DONE, or moved by DEL-010 |
| R3 Candidate | `develop` merges to `main`; `release/X.Y` is cut; `vX.Y.Z-rc.N` is tagged; the full regression runs once on the candidate commit: `CI gate`, the whole real e2e suite, migration upgrade from the previous released version | Regression green |
| R4 Owner acceptance | The owner runs the release's journeys on the candidate (DEL-012) | Every journey passes; no P0 or P1 open |
| R5 Release | `vX.Y.Z` is tagged on the release branch | Tag exists |

Branch, tag and version-number mechanics are in [`ci-cd-architecture.md`](ci-cd-architecture.md) §13. Deployment of the candidate to staging follows [`release-and-rollout.md`](release-and-rollout.md); while continuous delivery is dormant the candidate is the tagged commit run on a stack the owner can reach.

---

# 10. DEL-010 — Scope changes are the owner's

- The allocation of a use case to a release changes only by an owner decision, recorded in the roadmap in the same pull request as the change.
- A use case that is BLOCKED at R0 because its owner decision is still missing moves to the next release. It does not hold the release.
- A use case outside the scope never blocks a release.
- A use case that fails DEL-003 at R2 and cannot be fixed in time moves to the next release by owner decision. It does not ship at a lower state.

---

# 11. DEL-011 — A release candidate takes fixes only

- `release/X.Y` takes fixes, never capability.
- A fix lands on `main` first and is cherry-picked into the release branch, so `main` never lacks a fix the release has.
- After a fix a new `rc.N` is tagged. The owner re-runs the failed journeys; the lead re-runs the full regression.

---

# 12. DEL-012 — The owner accepts a version by journeys

- Each release has a list of acceptance journeys in the roadmap. The owner runs them on the candidate and records pass or fail for each journey.
- A failed journey is a defect with a severity (DEL-013).
- A version is released when every journey passes and no P0 or P1 defect is open, and when the readiness list in DEL-014 holds.
- The owner may run extra exploratory checks. What they find is a defect, not a new gate.

---

# 13. DEL-013 — Defects have four severities

| Severity | Meaning | Effect |
|---|---|---|
| P0 | Leaks data across tenants or permissions; loses data; loses a concurrent write; a security hole; sign-in or the main flow of the release cannot be used | Blocks the tag |
| P1 | A journey step cannot be completed and there is no workaround; a wrong business result | Blocks the tag |
| P2 | A journey completes with a workaround or a visible flaw | Recorded for the next release |
| P3 | Cosmetic, or a rare edge | Recorded for the next release |

The owner assigns severity at acceptance; the lead assigns it before the candidate. Leaks, data loss and lost writes are fixed before new capability in the same context, at any time, not only at R4.

---

# 14. DEL-014 — Release readiness

A version is released when, on its release branch:

1. every use case in its scope is DONE under DEL-003;
2. no P0 or P1 defect is open;
3. `CI gate` is green, the whole real e2e suite is green on the candidate commit, and migrations upgrade from the previous released version;
4. the owner passed every acceptance journey on the candidate;
5. the catalog counts in [`use-cases/README.md`](../workstreams/use-cases/README.md) §8 were re-taken for the release.

This list is the only definition of release readiness. [`ci-cd-architecture.md`](ci-cd-architecture.md) §13.2 points here.

---

# 15. DEL-015 — Entitlement and later-context defaults

A use case of an earlier release that consumes a later context runs against that context's default or fail-open contract, and its proof says so. Example: no plan gate applies in Work Management before the Billing release, so the entitlement port allows by default. The later release replaces the default and proves the real contract; it does not change the earlier use case's state.

---

# 16. DEL-016 — Commits and pull requests

- Commit messages are Conventional Commits, lowercase, 72 characters or fewer, and carry no use-case id or plan key.
- The pull request title is plain words and carries no use-case id.
- The pull request body has a `## Use cases` section with one row per use case: id, function in plain words, state change.
- The body also states the evidence run (spec names, pass counts) and the checks not run.

---

# 17. Maintenance

- Change this document with the owner's approval; it fixes who accepts what.
- Update [`product-roadmap.md`](../workstreams/product-roadmap.md) for scope; never copy scope or counts here.
- Do not restate DEL-003 in the catalog; the catalog points here.
