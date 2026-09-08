---
document_id: FUIR-CERTIFICATION-V4
status: active
---

# Frontend UI Readiness Closure — CERTIFICATION

## 1. Certification question

This document answers only:

> Can Notrelix teams develop and test governed Web UI with deterministic UI data and production-like presentation interaction without backend/API/auth availability?

It does not certify application integration, mock-backend parity, or backend correctness.

## 2. Re-audit verdict before execution

At baseline PR #115 HEAD `f7936f336c01d8f413e9cf5b56f8b5e8a4de76d0`:

```text
FOUNDATION_ARCHITECTURE: ACCEPTED
FULL_UI_READINESS: NOT YET CERTIFIED
```

### Accepted foundation

- pure UI/application integration lanes are separated;
- Storybook owner-local discovery exists;
- deterministic fixtures/scenarios/controllers exist across major owners;
- renderPureUi + runtime network guard exist;
- static transitive purity exists;
- manifest-driven a11y/visual/network runners exist;
- ui-foundation CI exists.

### Blocking UI-readiness gaps to close

1. source inventory is not machine complete;
2. baseline debt docs overstate structural debt for already-pure board layout/workspace toolbar files;
3. board workspace composition still needs a pure seam and has a typed-boundary defect (`props: any`);
4. visible dead enabled actions exist in board/workspace toolbar surfaces;
5. state taxonomy has semantic misuse in baseline manifests;
6. interaction coverage is file-level;
7. manifest visual runner is desktop-only;
8. theme evidence is background-only, not application theme semantics;
9. purity has dynamic/require/browser-side-effect gaps;
10. there is no UI-only developer command.

## 3. Mandatory final command

The certification owner runs:

```bash
cd frontend
pnpm validate:ui
```

`validate:ui` must recursively resolve to exactly the UI-scoped checks defined by SPEC §15 and no application integration commands.

## 4. Required evidence record

Final evidence records at minimum:

```text
candidate_sha
base_sha
validate_ui_exit_code
architecture_exit_code
ui_purity_exit_code
ui_actions_exit_code
ui_evidence_exit_code
typecheck_exit_code
lint_exit_code
format_check_exit_code
web_guarded_exit_code
ui_freeze_exit_code
owner_count
governed_source_count
surface_count
unclassified_source_count
duplicate_source_classification_count
stale_source_classification_count
cross_owner_classification_count
required_state_count
delegated_state_count
not_applicable_state_count
unaccounted_state_count
dead_action_count
interaction_case_count
missing_interaction_case_count
failed_required_interaction_case_count
skipped_required_interaction_case_count
visual_expected_target_count
visual_passing_target_count
visual_second_run_diff_count
visual_mobile_target_count
visual_tablet_target_count
visual_desktop_target_count
visual_dark_target_count
a11y_target_count
blocking_a11y_violation_count
network_target_count
network_violation_count
deferred_application_integration_findings
```

## 5. Hard gates

### C1 — Candidate identity

- final evidence SHA equals source HEAD;
- no evidence from older SHA is reused as terminal proof.

### C2 — Source classification

Require:

```text
unclassified_source_count = 0
duplicate_source_classification_count = 0
stale_source_classification_count = 0
cross_owner_classification_count = 0
```

### C3 — Pure ownership and action contract

Require:

```text
pnpm check:ui-purity = 0
pnpm check:ui-actions = 0
dead_action_count = 0
```

Dynamic import/require and browser-side-effect negative fixtures must also pass.

### C4 — Semantic state coverage

Require:

```text
unaccounted_state_count = 0
```

`EdgeData` may not be used as error/success alias. Every delegation and N/A record is valid.

### C5 — Deterministic verification data

Fixture/scenario/controller determinism/freshness/isolation tests pass for all owners that expose those authorities.

### C6 — Interaction behavior

Require:

```text
interaction_case_count > 0
missing_interaction_case_count = 0
failed_required_interaction_case_count = 0
skipped_required_interaction_case_count = 0
```

### C7 — Responsive/theme visual evidence

Require:

```text
visual_expected_target_count = visual_passing_target_count
visual_second_run_diff_count = 0
visual_mobile_target_count > 0
visual_tablet_target_count > 0
visual_desktop_target_count > 0
visual_dark_target_count > 0
```

Every visual story has desktop/light. Every responsive Default has mobile/tablet light. Every theme-aware Default has desktop/dark.

### C8 — Accessibility

Require:

```text
a11y_target_count > 0
blocking_a11y_violation_count = 0
```

### C9 — Zero network

Require:

```text
network_target_count > 0
network_violation_count = 0
```

A controlled network-attempt story must prove the guard rejects fetch/XHR/WebSocket.

### C10 — UI-only developer command

Require:

```text
pnpm validate:ui = 0
```

The resolved command graph includes required UI checks and excludes codegen/mock/real/backend commands.

### C11 — UI-specific CI independence

The existing `ui-foundation` job remains the UI evidence owner and has no direct application mock/real-backend dependency.

### C12 — Backend absence

Certification is reproducible with no:

```text
backend process
PostgreSQL
Redis
backend Docker stack
real auth credentials
real API endpoint availability
```

## 6. Required negative proofs

A hard gate is not accepted unless controlled fixtures/probes prove it rejects at least these violations:

1. unclassified production `.tsx`;
2. duplicate source classification;
3. dead enabled Button;
4. forbidden dynamic import;
5. invalid/missing state accounting;
6. missing interaction case marker;
7. invalid viewport/theme target;
8. pure story network attempt;
9. indirect forbidden command inside `validate:ui`.

## 7. Deferred findings

These are always visible but non-blocking for this certification:

```text
CTR-GAP-TODO / consumer surface mapping
application mock semantic parity
authorization 403 mock fidelity
production API adapter any/speculative mapping outside pure surfaces
search mock contract closure
real backend persistence/auth/RLS
real E2E
mock-real parity
```

Classification is:

```text
DEFERRED_APPLICATION_INTEGRATION
NON_BLOCKING_FOR_UI_READINESS
```

## 8. Final verdict

### PASS

```text
Verdict: UI_READY_FOR_PRODUCT_DEVELOPMENT_AND_UI_TESTING
Candidate SHA: <sha>
C1 Candidate identity: PASS
C2 Source classification: PASS
C3 Pure ownership/actions: PASS
C4 State semantics: PASS
C5 Deterministic verification data: PASS
C6 Interaction behavior: PASS
C7 Responsive/theme visual: PASS
C8 Accessibility: PASS
C9 Zero network: PASS
C10 validate:ui: PASS
C11 ui-foundation independence: PASS
C12 Backend absence: PASS
Deferred application integration findings: <count> NON_BLOCKING
```

### FAIL

```text
Verdict: NOT_UI_READY
Candidate SHA: <sha>
Failed gates: <C# list>
Failed tests: <FUIR-TST-* list>
Blocking paths: <paths>
```

No `mostly ready`, `ready except`, or `done enough` terminal verdict is legal.

## 9. Structural audit of this execution package

Before handing this package to a coding agent, the artifact producer must prove:

- exactly 5 authority files;
- all FUIR-REQ definitions are unique;
- all FUIR-LOCK definitions are unique;
- all FUIR-WU definitions are unique;
- all FUIR-TST definitions are unique;
- every referenced FUIR ID is defined;
- every requirement appears in PLAN traceability;
- every work unit contains Goal, Inputs/Required implementation, Forbidden, Tests, Evidence, Terminal fields;
- no active instruction delegates architecture choice with phrases such as `choose`, `as needed`, `where feasible`, or `decide`;
- no mandatory UI certification command invokes mock/real/backend integration lanes;
- ZIP integrity passes.

## 10. Artifact audit snapshot

```text
Authority files: 5
Requirements: 12
Decision locks: 29
Work units: 39
Test contracts: 100
Duplicate IDs: 0
Undefined references: 0
Work units missing required fields: 0
Requirements missing traceability: 0
Actionable architecture-choice phrases: 0
Forbidden integration commands in positive validate:ui graph: 0
Artifact audit: PASS
```

## 11. Execution evidence log

Candidate-bound evidence recorded per FUIR-PLAN-V4 wave. FUIR-LOCK-29 applies: no evidence from an older SHA certifies a newer candidate.

## 11.1 Wave 0 — Candidate refresh and scope freeze

```text
candidate_sha:          eabbe62f45ac (PR #115 branch feature/web-app HEAD after FUIR-PLAN-V4 execution start)
base_sha:               ad279ac43083 (merge-base with origin/develop)
spec_v4_baseline_sha:   f7936f336c01 (SPEC/PLAN/DECISIONS baseline — differs from candidate; updated evidence only, accepted locks unchanged)
timestamp:              2026-09-08
commits_ahead:          29
changed_file_count:     390

FUIR-WU-001 Terminal:   IMPLEMENTED_VERIFIED
FUIR-WU-002 Terminal:   IMPLEMENTED_VERIFIED
FUIR-WU-003 Terminal:   IMPLEMENTED_VERIFIED
FUIR-WU-004 Terminal:   IMPLEMENTED_VERIFIED
```

Owner enumeration (FUIR-WU-002, deterministic sorted package names):

| Owner root | Production `.tsx` | Surfaces | Stories | Interaction cases | Visual targets |
|---|---|---|---|---|---|
| packages/ui/web | 63 | 7 | 7 | 5 | 28 |
| packages/product/automation/web | 1 | 1 | 3 | 3 | 6 |
| packages/product/docs/web | 11 | 5 | 15 | 5 | 30 |
| packages/product/work-management/web | 42 | 16 | 38 | 29 | 74 |
| packages/features/auth | 8 | 3 | 9 | 5 | 13 |
| packages/features/billing | 1 | 1 | 3 | 2 | 6 |
| packages/features/collaboration | 2 | 1 | 3 | 3 | 6 |
| packages/features/notifications | 2 | 1 | 3 | 4 | 6 |
| packages/features/search | 2 | 1 | 3 | 4 | 6 |
| packages/features/workspace | 17 | 6 | 13 | 14 | 31 |
| **Total** | **149** | **42** | **97** | **74** | **206** |

Deterministic enumeration was verified byte-identical on repeated runs (governed-source-enumerator unit suite).

Corrected baseline source findings (FUIR-WU-003, satisfies FUIR-TST-004):

```text
board-layout-shell.tsx                    presentation-only (no state/query/router/API imports) — KEEP_EXISTING_VERIFIED
board-toolbar.tsx                         presentation-only; typed onFilter/onViewChange/onSearchChange present — KEEP_EXISTING_VERIFIED
view-tabs.tsx                             presentation/local-state (useState/useEffect local only) — KEEP_EXISTING_VERIFIED
workspace-contextual-toolbar.tsx          presentation-only; typed onAction/onSearchChange; aria-label present — KEEP_EXISTING_VERIFIED
board-workspace-view-content.tsx          owns useFullBoard + stateful child adaptation; BoardScreenProps typed (no props: any) — KEEP_EXISTING_VERIFIED
board-workspace-surface.tsx               pure presentation seam; no state/query/router/API imports — KEEP_EXISTING_VERIFIED
```

Deferred application integration findings (FUIR-WU-004, satisfies FUIR-TST-005, classified `DEFERRED_APPLICATION_INTEGRATION`):

```text
CTR-GAP-TODO / consumer surface mapping
application mock semantic parity
authorization 403 mock fidelity
production API adapter any/speculative mapping outside pure surfaces
search mock contract closure
real backend persistence/auth/RLS
real E2E
mock-real parity
```

## 11.2 Wave 1 checkpoint — Manifest v2 and machine source inventory

```text
commit: 55c092d
FUIR-WU-013 Terminal:   IMPLEMENTED_VERIFIED (all 10 active owner manifests schemaVersion 2 at candidate)
FUIR-WU-014 Terminal:   IMPLEMENTED_VERIFIED (manual inventory docs demoted to historical;
                        resolved debt rows marked removed)

Gate evidence:

check:ui-evidence:   valid, 42 surfaces, 96 required states  (FUIR-TST-025/027, current-source pass)
check:ui-purity:     valid, 42 entries
```

## 11.3 Wave 2 checkpoint — Pure seams and action-contract closure

```text
FUIR-WU-020 Terminal:   KEEP_EXISTING_VERIFIED (board layout shell/toolbar/view-tabs
                        registered at baseline, typed onFilter present)
FUIR-WU-021 Terminal:   IMPLEMENTED_VERIFIED (board-workspace-surface.tsx pure seam,
                        useFullBoard stays in board-workspace-view-content.tsx)
FUIR-WU-022 Terminal:   IMPLEMENTED_VERIFIED (workspace contextual toolbar typed
                        action/search contract)
FUIR-WU-023 Terminal:   IMPLEMENTED_VERIFIED (check:ui-actions at package.json:37)
FUIR-WU-024 Terminal:   IMPLEMENTED_VERIFIED (dynamic import/require traversal, side-effect
                        AST detection in check-ui-purity)

Gate evidence:

unit (node vitest):   check-ui-actions 4, check-ui-purity 9, check-fixture-determinism 2   = 15 passed
web vitest:           board-layout 9, kanban-board 2, kanban-column 2                    = 15 passed
check:ui-actions:     valid, 119 registered sources
check:ui-purity:      valid, 42 entries
check:ui-fixtures:    valid, 27 fixture/scenario/controller files
```
