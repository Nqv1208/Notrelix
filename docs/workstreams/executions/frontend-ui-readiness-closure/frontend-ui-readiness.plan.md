---
document_id: FUIR-PLAN-V4
status: active
baseline_sha: f7936f336c01d8f413e9cf5b56f8b5e8a4de76d0
---

# Frontend UI Readiness Closure — PLAN

## 1. How the coding agent executes this PLAN

For every work unit:

1. read the listed source inputs at the current candidate SHA;
2. apply the locked implementation exactly;
3. run every listed test contract;
4. record evidence;
5. terminate with one legal terminal state;
6. do not open the next wave until the current wave exit gate passes.

A work unit marked `KEEP_EXISTING_VERIFIED` still requires its listed tests/evidence.

## 2. Execution sequence

```text
Wave 0  Candidate refresh and scope freeze
Wave 1  Manifest v2 and machine source inventory
Wave 2  Pure seams and action-contract closure
Wave 3  State semantics and deterministic verification data
Wave 4  Interaction behavior closure
Wave 5  Responsive/theme/a11y/zero-network closure
Wave 6  UI-only developer and CI lane
Wave 7  Final candidate-SHA certification
```

---

# Wave 0 — Candidate refresh and scope freeze

## FUIR-WU-001 — Pin candidate source

**Goal:** establish the exact source revision being certified.

**Inputs:** PR #115 metadata; current branch HEAD/base.

**Required implementation:** record candidate SHA/base SHA in execution evidence. If HEAD differs from SPEC baseline, update only evidence, not accepted architectural locks.

**Forbidden:** implementing source changes before SHA is recorded.

**Tests:** FUIR-TST-001.

**Evidence:** candidate SHA, base SHA, timestamp, changed-file count.

**Terminal:** `IMPLEMENTED_VERIFIED`.

## FUIR-WU-002 — Enumerate governed owner roots and current manifests

**Goal:** prove the execution sees the full current Web UI ownership surface.

**Inputs:** owner roots from SPEC §8.

**Required implementation:** enumerate owner packages, production `.tsx`, current manifests, stories, component tests, fixture/scenario/controller sources. Produce deterministic counts by owner.

**Forbidden:** treating the PR changed-file list as the complete production source inventory.

**Tests:** FUIR-TST-002, FUIR-TST-003.

**Evidence:** owner/count table.

**Terminal:** `IMPLEMENTED_VERIFIED`.

## FUIR-WU-003 — Revalidate corrected baseline findings

**Goal:** prevent stale debt descriptions from driving unnecessary refactors.

**Inputs:** exact files in SPEC §5.2.

**Required implementation:** verify board layout shell/toolbar/view-tabs are still presentation-only; verify workspace contextual toolbar is still presentation-only; verify board workspace composition still owns state/query. Record source evidence.

**Forbidden:** extracting a new seam from an already-pure baseline file.

**Tests:** FUIR-TST-004.

**Evidence:** classification row per baseline file.

**Terminal:** `IMPLEMENTED_VERIFIED` or `KEEP_EXISTING_VERIFIED` per fact.

## FUIR-WU-004 — Freeze deferred integration findings

**Goal:** keep UI closure independent from application integration debt.

**Inputs:** current mock/API/backend findings.

**Required implementation:** record them under `DEFERRED_APPLICATION_INTEGRATION`; no source edits for those findings in this execution.

**Forbidden:** editing backend/RLS/OpenAPI/mock consumer mapping solely to advance UI waves.

**Tests:** FUIR-TST-005.

**Evidence:** deferred finding list.

**Terminal:** `IMPLEMENTED_VERIFIED`.

**Wave 0 exit gate:** FUIR-TST-001..005 pass and no later work unit requires backend/API availability.

---

# Wave 1 — Manifest v2 and machine source inventory

## FUIR-WU-010 — Implement manifest v2 schema

**Goal:** create one strict machine contract for source, state, interaction, and visual coverage.

**Inputs:** `frontend/tooling/testing/src/ui-evidence-schema.ts`; SPEC §9; FUIR-LOCK-05..10.

**Required implementation:** replace schema v1 with v2 fields and validation exactly from SPEC §9. Add `Success`; reject unknown fields/unknown enum values where existing schema style permits strict validation; validate owner-local relative paths.

**Forbidden:** backward-compatible optional v1 parsing after Wave 1; catch-all `unknown` state labels; optional inventory escape hatch.

**Tests:** FUIR-TST-010, 011, 012, 013, 014.

**Evidence:** schema unit test output.

**Terminal:** `IMPLEMENTED_VERIFIED`.

## FUIR-WU-011 — Implement governed source enumerator

**Goal:** derive the source set that must be classified.

**Inputs:** existing manifest discovery/evidence tooling; SPEC §8.

**Required implementation:** enumerate `src/**/*.tsx` per active owner and apply only structural exclusions from SPEC §8. Normalize owner-relative POSIX paths, sort deterministically, reject path traversal/symlink escape.

**Forbidden:** manually maintained source list; excluding route/provider/container files automatically.

**Tests:** FUIR-TST-015, 016, 017, 018.

**Evidence:** enumerator unit tests plus current owner counts.

**Terminal:** `IMPLEMENTED_VERIFIED`.

## FUIR-WU-012 — Enforce exactly-one source classification

**Goal:** make unregistered UI source impossible to hide.

**Inputs:** `check-ui-evidence.ts`, manifest v2, source enumerator.

**Required implementation:** fail on unclassified, multiply classified, stale/missing classified path, cross-owner covered source, empty/invalid exclusion. Report owner + path + competing classifications.

**Forbidden:** warnings-only mode for CI.

**Tests:** FUIR-TST-019, 020, 021, 022, 023, 024.

**Evidence:** negative-fixture output proving each failure mode.

**Terminal:** `IMPLEMENTED_VERIFIED`.

## FUIR-WU-013 — Migrate every active owner manifest to v2

**Goal:** eliminate mixed manifest authority.

**Inputs:** all manifests discovered at WU-002.

**Required implementation:** migrate every active manifest to schemaVersion 2; populate owner, surfaceKind, source classification, stateCoverage, responsive/themeAware, visualTargets, checks, interactionCases.

**Forbidden:** leaving one active v1 manifest; inventing placeholder stories/sources to satisfy schema.

**Tests:** FUIR-TST-025, 026.

**Evidence:** migrated manifest list; `check:ui-evidence` current-source pass.

**Terminal:** `IMPLEMENTED_VERIFIED`.

## FUIR-WU-014 — Retire manual inventory as authority

**Goal:** prevent two competing coverage systems.

**Inputs:** `ui-critical-surface-inventory.md`, `ui-migration-debt-register.md`.

**Required implementation:** update docs to state that manifest v2 + checker are authoritative. Historical rationale may remain, but no manual table determines pass/fail.

**Forbidden:** maintaining a second required manual source classification table.

**Tests:** FUIR-TST-027.

**Evidence:** docs diff + checker green.

**Terminal:** `IMPLEMENTED_VERIFIED`.

**Wave 1 exit gate:** every active manifest is v2 and current governed source has zero unclassified/duplicate/stale classifications.

---

# Wave 2 — Pure seams and action-contract closure

## FUIR-WU-020 — Register existing Work Management board layout surfaces

**Goal:** close baseline board layout coverage without needless refactor.

**Inputs:** `board-layout-shell.tsx`, `board-toolbar.tsx`, `view-tabs.tsx`, Work Management manifest/testing authority.

**Required implementation:** register the existing components as owner-local surface evidence. Add deterministic stories/scenarios only where missing. Add typed `onFilter` (or same-owner equivalent callback following existing naming precedent) to `BoardToolbar` so the enabled Filter action is testable. Preserve `onViewChange`, `onSearchChange`, and AddView local interaction semantics.

**Forbidden:** replacing these components with new duplicate surfaces; introducing state/query/router ownership.

**Tests:** FUIR-TST-030, 031, 032.

**Evidence:** manifest rows, story IDs, interaction cases, purity pass.

**Terminal:** `IMPLEMENTED_VERIFIED` or `KEEP_EXISTING_VERIFIED` for already-present subparts.

## FUIR-WU-021 — Extract the one required board workspace presentation seam

**Goal:** make board workspace loading/error/unsupported/view selection renderable without `useFullBoard`.

**Inputs:** `board-workspace-view-content.tsx`; child view surface contracts.

**Required implementation:** create `src/components/board-workspace-surface.tsx` exporting typed `BoardWorkspaceSurface`. It receives presentation state and typed renderable view content/callback inputs; it contains no state/query/router/API imports. Keep `board-workspace-view-content.tsx` as container/adaptor owning `useFullBoard` and stateful child containers. Replace `BoardScreen(props: any)` with a typed props interface while touching the boundary.

**Forbidden:** moving `useFullBoard` into fixture/controller; making Storybook boot QueryClient; duplicating Kanban/Table/Calendar/Timeline state hooks in the new surface.

**Tests:** FUIR-TST-033, 034, 035, 036.

**Evidence:** source graph, manifest row, stories for Default/Loading/Error/unsupported edge, interaction if applicable.

**Terminal:** `IMPLEMENTED_VERIFIED`.

## FUIR-WU-022 — Close Workspace contextual toolbar action contract

**Goal:** make the already-pure toolbar interactable like production UI without backend/router.

**Inputs:** `workspace-contextual-toolbar.tsx`; workspace UI types/fixtures.

**Required implementation:** keep the existing component as owner. Add typed controlled search input and a typed action event/callback contract for enabled toolbar actions. Preserve local ToggleGroup behavior. Every visible enabled action must either emit the typed action callback or perform an observable local UI transition.

**Forbidden:** API calls, router calls, fake services, leaving visible enabled no-op buttons.

**Tests:** FUIR-TST-037, 038, 039.

**Evidence:** component tests for search/action emission; manifest/story coverage.

**Terminal:** `IMPLEMENTED_VERIFIED`.

## FUIR-WU-023 — Add `check-ui-actions`

**Goal:** prevent future dead enabled buttons on registered surfaces.

**Inputs:** manifest v2 classified owner-local surface sources; TypeScript JSX AST tooling.

**Required implementation:** add `frontend/tooling/dependency-rules/src/check-ui-actions.ts` and package/root script `check:ui-actions`. Check owner-local source files classified to registered surfaces. Native `button` and imported `Button` controls fail when they are enabled and have no callback, submit, disabled, or link/asChild semantics. Provide explicit diagnostic path/line/control.

**Forbidden:** scanning node_modules; treating warnings as success; rejecting compound controls that have intrinsic behavior without a Button/button node.

**Tests:** FUIR-TST-040, 041, 042, 043.

**Evidence:** positive/negative AST fixtures.

**Terminal:** `IMPLEMENTED_VERIFIED`.

## FUIR-WU-024 — Complete dynamic/side-effect purity closure

**Goal:** eliminate ownership bypasses in pure graphs.

**Inputs:** `check-ui-purity.ts`; existing purity fixtures.

**Required implementation:** add literal dynamic import traversal, literal `require` traversal, fail closed on non-literal dynamic module ownership, and AST detection for direct `document.cookie`, clipboard, history navigation, window location mutation. Preserve current fetch/XHR/WebSocket/storage checks.

**Forbidden:** regex-only implementation when AST identity is available; suppressing unresolved forbidden imports.

**Tests:** FUIR-TST-044, 045, 046, 047, 048.

**Evidence:** negative-fixture failures plus current-source green run.

**Terminal:** `IMPLEMENTED_VERIFIED`.

**Wave 2 exit gate:** known baseline UI debt has correct pure seams/action semantics; `check:ui-actions` and expanded `check:ui-purity` pass current source and reject negative fixtures.

---

# Wave 3 — State semantics and deterministic verification data

## FUIR-WU-030 — Migrate semantic state misuse

**Goal:** make state labels describe actual UI meaning.

**Inputs:** all v2 manifests/stories; baseline auth manifest is mandatory audit target.

**Required implementation:** migrate submit error stories to `Error`; success/completion stories to `Success`; reserve `EdgeData` for data-shape stress. Reconcile story tags/IDs only where needed; keep stable IDs when semantic state can change without breaking ownership.

**Forbidden:** using `EdgeData` as generic error or `ReadOnly` as generic success.

**Tests:** FUIR-TST-050, 051.

**Evidence:** semantic state validation output.

**Terminal:** `IMPLEMENTED_VERIFIED`.

## FUIR-WU-031 — Enforce surfaceKind state policy

**Goal:** prevent owners from declaring too few states.

**Inputs:** schema v2 policy universes from SPEC §9.3.

**Required implementation:** for every surface-kind universe state, require exactly one of required/delegated/N/A. Validate delegation target existence/state and N/A reason+authority.

**Forbidden:** empty generic N/A reasons; cross-owner delegation.

**Tests:** FUIR-TST-052, 053, 054, 055.

**Evidence:** policy unit tests + current manifest pass.

**Terminal:** `IMPLEMENTED_VERIFIED`.

## FUIR-WU-032 — Audit fixture/scenario determinism across all owners

**Goal:** eliminate ambient randomness/time/shared mutable data.

**Inputs:** product testing packages and `src/verification` data authorities discovered in W0.

**Required implementation:** scan fixture/scenario/controller sources for forbidden nondeterminism; replace with existing fixed-clock/stable factory conventions. Preserve production source unless it is a registered pure entry.

**Forbidden:** seedless random generation; module singleton mutable scenario state.

**Tests:** FUIR-TST-056, 057, 058.

**Evidence:** deterministic scan + unit tests.

**Terminal:** `IMPLEMENTED_VERIFIED`.

## FUIR-WU-033 — Prove scenario freshness/cardinality/edge semantics

**Goal:** ensure scenario names have deterministic meaning.

**Inputs:** every data/form owner with scenarios.

**Required implementation:** prove repeated calls return fresh graphs; Empty is empty; EdgeData contains long/Unicode/optional-edge values appropriate to the model; HighDensity has documented exact counts where required.

**Forbidden:** inline story-only fake data for non-trivial product surfaces.

**Tests:** FUIR-TST-059, 060, 061, 062.

**Evidence:** owner test results and documented cardinalities.

**Terminal:** `IMPLEMENTED_VERIFIED`.

## FUIR-WU-034 — Prove controller isolation

**Goal:** guarantee local UI simulation cannot leak between stories/tests.

**Inputs:** every registered surface using a controller.

**Required implementation:** instantiate two controllers from the same scenario and prove mutations are isolated and source scenario unchanged.

**Forbidden:** module-level sequence/state shared across instances.

**Tests:** FUIR-TST-063, 064.

**Evidence:** controller isolation tests.

**Terminal:** `IMPLEMENTED_VERIFIED` or `NOT_APPLICABLE` for owners with zero controllers, with source count evidence.

**Wave 3 exit gate:** semantic state policy and deterministic data/controller checks pass all active owners.

---

# Wave 4 — Interaction behavior closure

## FUIR-WU-040 — Migrate interactionTests to interactionCases

**Goal:** make each required behavior independently provable.

**Inputs:** all surfaces with `interaction` check; existing component tests.

**Required implementation:** replace file-only declarations with `interactionCases[{id,testFile}]`. Case IDs describe user-visible behavior. One test file may satisfy multiple cases.

**Forbidden:** case IDs for hook/query implementation details.

**Tests:** FUIR-TST-070, 071, 072.

**Evidence:** manifest case count by owner.

**Terminal:** `IMPLEMENTED_VERIFIED`.

## FUIR-WU-041 — Upgrade Vitest interaction coverage parser

**Goal:** prove exact behavior case execution.

**Inputs:** `assert-ui-interaction-coverage.mjs`; Vitest JSON output.

**Required implementation:** parse passing assertion titles for exact `FUI[surfaceId:caseId]` markers; fail missing, failed, skipped, duplicate-declared, unknown-surface, or unknown-case markers.

**Forbidden:** treating file execution as sufficient.

**Tests:** FUIR-TST-073, 074, 075, 076, 077.

**Evidence:** synthetic report fixtures proving pass/fail modes.

**Terminal:** `IMPLEMENTED_VERIFIED`.

## FUIR-WU-042 — Migrate current component tests to case markers

**Goal:** make current UI behavior evidence satisfy the new contract.

**Inputs:** all manifest-declared component tests.

**Required implementation:** add exact FUI markers and fill missing behavior assertions. Mandatory baseline cases include board layout view/filter/search, workspace contextual search/action, Kanban create/move/open where exposed, Table primary interactions, auth submit states, and existing feature action contracts.

**Forbidden:** asserting internal hook calls instead of user-visible callback/output behavior.

**Tests:** FUIR-TST-078, 079.

**Evidence:** `test:web:guarded` JSON coverage report; zero missing/skipped cases.

**Terminal:** `IMPLEMENTED_VERIFIED`.

## FUIR-WU-043 — Keyboard interaction closure for registered interaction surfaces

**Goal:** ensure UI testing is not mouse-only for keyboard-operable controls.

**Inputs:** interaction surfaces with buttons/tabs/forms/menus.

**Required implementation:** add keyboard assertions for Enter/Space/tab selection/Escape where the underlying control semantics support them. Use accessible roles/names.

**Forbidden:** adding custom key handlers where native/Radix behavior already provides semantics solely to satisfy tests.

**Tests:** FUIR-TST-080, 081.

**Evidence:** keyboard case markers in passing tests.

**Terminal:** `IMPLEMENTED_VERIFIED`.

**Wave 4 exit gate:** every declared interaction case has a passing assertion marker; zero dead action violations.

---

# Wave 5 — Responsive/theme/a11y/zero-network closure

## FUIR-WU-050 — Add canonical viewport/theme registries

**Goal:** centralize renderer identities.

**Inputs:** Storybook UI evidence support/tooling.

**Required implementation:** define exactly mobile 390x844, tablet 768x1024, desktop 1440x900 and light/dark theme IDs in shared UI evidence tooling. Runners consume IDs, not ad hoc numbers/strings.

**Forbidden:** per-test viewport constants outside the registry for manifest evidence.

**Tests:** FUIR-TST-090, 091.

**Evidence:** registry unit tests.

**Terminal:** `IMPLEMENTED_VERIFIED`.

## FUIR-WU-051 — Implement application-semantic Storybook theme adapter

**Goal:** make dark/light screenshots reflect real Web theme semantics.

**Inputs:** Web theme application contract; Storybook preview.

**Required implementation:** add shared adapter applying the same root class/data attribute used by Web application. Storybook test URL/fixture selects theme deterministically.

**Forbidden:** using `backgrounds` alone as theme proof.

**Tests:** FUIR-TST-092, 093.

**Evidence:** DOM theme attribute/class assertions.

**Terminal:** `IMPLEMENTED_VERIFIED`.

## FUIR-WU-052 — Upgrade visual runner to manifest visualTargets

**Goal:** prove required responsive/theme targets without full Cartesian explosion.

**Inputs:** `ui-manifest.visual.spec.ts`; manifest v2.

**Required implementation:** run exactly each story's declared targets; enforce SPEC §14 policy; name snapshots `<storyId>--<viewport>--<theme>.png`; Linux Chromium remains CI authority.

**Forbidden:** desktop-only hardcode; generating undeclared target permutations.

**Tests:** FUIR-TST-094, 095, 096, 097.

**Evidence:** expected/passing target counts by viewport/theme.

**Terminal:** `IMPLEMENTED_VERIFIED`.

## FUIR-WU-053 — Rebaseline authoritative snapshots once

**Goal:** create authoritative snapshots for the new target identity.

**Inputs:** migrated manifests; visual runner.

**Required implementation:** generate Linux-authoritative snapshots after structural UI checks pass; inspect diffs caused by viewport/theme changes; rerun without update and require zero diff.

**Forbidden:** blanket snapshot update before reviewing unexpected layout regressions.

**Tests:** FUIR-TST-098, 099.

**Evidence:** snapshot count and clean second run.

**Terminal:** `IMPLEMENTED_VERIFIED`.

## FUIR-WU-054 — Preserve and count a11y evidence

**Goal:** retain existing Axe gate while making target count part of certification.

**Inputs:** `ui-manifest.a11y.spec.ts`.

**Required implementation:** preserve serious/critical blocking policy; expose deterministic target count/results for certification.

**Forbidden:** disabling Axe for product surfaces to make certification pass.

**Tests:** FUIR-TST-100, 101.

**Evidence:** target count; zero serious/critical violations.

**Terminal:** `IMPLEMENTED_VERIFIED`.

## FUIR-WU-055 — Strengthen zero-network evidence

**Goal:** prove pure stories cannot silently dispatch network.

**Inputs:** `pure-ui-network-guard.ts`, Storybook preview, `ui-manifest.network.spec.ts`.

**Required implementation:** keep guard installed for every Storybook target; network test records target count and rejects PureUiNetworkAccessError/attempt evidence. Add repeated install/restore isolation test.

**Forbidden:** allowing requests to `.invalid` endpoints as a substitute for zero-network.

**Tests:** FUIR-TST-102, 103, 104.

**Evidence:** network target count; zero violations.

**Terminal:** `IMPLEMENTED_VERIFIED`.

**Wave 5 exit gate:** visual responsive/theme, a11y, and zero-network evidence all pass with non-zero target counts.

---

# Wave 6 — UI-only developer and CI lane

## FUIR-WU-060 — Add exact `validate:ui`

**Goal:** give UI developers one scoped local command.

**Inputs:** root `frontend/package.json`; SPEC §15.

**Required implementation:** add `check:ui-actions` and `validate:ui` exact composition from SPEC. Preserve `validate` and `validate:fast` unchanged.

**Forbidden:** codegen/mock/real/backend commands in the `validate:ui` dependency graph.

**Tests:** FUIR-TST-110, 111, 112.

**Evidence:** script dependency inspection + successful local command.

**Terminal:** `IMPLEMENTED_VERIFIED`.

## FUIR-WU-061 — Update UI construction developer guide

**Goal:** make the developer workflow match actual machine gates.

**Inputs:** `frontend/docs/development/ui-construction.md`.

**Required implementation:** document manifest v2, source classification, state semantics, action contract, FUI case markers, responsive/theme target policy, `pnpm validate:ui`, and explicit separation from application mock integration.

**Forbidden:** instructing UI developers to start backend/mock E2E for UI DONE.

**Tests:** FUIR-TST-113.

**Evidence:** docs check/manual content assertion.

**Terminal:** `IMPLEMENTED_VERIFIED`.

## FUIR-WU-062 — Update `ui-foundation` only

**Goal:** make CI execute the same UI contract.

**Inputs:** `.github/workflows/frontend-ci.yml`; current `ui-foundation` job.

**Required implementation:** keep the job owner and renderer pin. Ensure it runs `check:ui-purity`, `check:ui-actions`, `check:ui-evidence`, `test:web:guarded` if case-level interaction coverage is not already guaranteed by an upstream required job for the same SHA, and `test:ui:freeze`. Do not add mock/real-backend needs.

**Forbidden:** redesigning global Frontend gate/routing; adding a second UI workflow.

**Tests:** FUIR-TST-114, 115.

**Evidence:** workflow static test + CI job result.

**Terminal:** `IMPLEMENTED_VERIFIED`.

## FUIR-WU-063 — Prove UI lane independence by negative command graph

**Goal:** prevent future scope regression.

**Inputs:** `validate:ui`, `ui-foundation` workflow.

**Required implementation:** add a static test/check asserting forbidden application integration commands do not appear in their direct/known composed command graph.

**Forbidden:** string-only check that misses root script indirection; resolve package scripts recursively.

**Tests:** FUIR-TST-116.

**Evidence:** negative fixture/probe.

**Terminal:** `IMPLEMENTED_VERIFIED`.

**Wave 6 exit gate:** one local `validate:ui` and one CI `ui-foundation` lane prove the same UI-only contract without application integration dependencies.

---

# Wave 7 — Final candidate-SHA certification

## FUIR-WU-070 — Run full UI-only candidate validation

**Goal:** establish one clean candidate-SHA evidence set.

**Inputs:** final source after Waves 0–6.

**Required implementation:** run `pnpm validate:ui` from clean dependencies/build artifacts appropriate to existing CI setup.

**Forbidden:** using a previous SHA's output.

**Tests:** FUIR-TST-120.

**Evidence:** command, SHA, exit code.

**Terminal:** `IMPLEMENTED_VERIFIED` only on exit 0.

## FUIR-WU-071 — Verify source/state/action/interaction counts

**Goal:** prove no hidden coverage debt remains.

**Inputs:** final manifest v2 checker output, `check:ui-actions` output, `test:web:guarded` interaction coverage output.

**Required implementation:** record governed source count, unclassified/duplicate/stale count, surface count, required/delegated/N/A state count, dead action count, interaction case count/missing/skipped count.

**Forbidden:** deriving terminal counts from manual prose tables or a previous SHA.

**Tests:** FUIR-TST-121, 122.

**Evidence:** machine-readable checker outputs or parseable logs.

**Terminal:** `IMPLEMENTED_VERIFIED` only with all blocking debt counts zero.

## FUIR-WU-072 — Verify visual/a11y/network evidence counts

**Goal:** prove render evidence is non-zero and complete.

**Inputs:** final `test:ui:freeze` results, manifest visual target inventory, authoritative Linux snapshot inventory.

**Required implementation:** record expected/passing visual target counts by viewport/theme, second-run visual diff count, a11y target/blocking count, network target/violation count.

**Forbidden:** counting skipped tests or local Darwin snapshots as authoritative passing targets.

**Tests:** FUIR-TST-123, 124, 125.

**Evidence:** Playwright results + snapshot inventory.

**Terminal:** `IMPLEMENTED_VERIFIED` only when all expected targets pass and violation counts are zero.

## FUIR-WU-073 — Prove backend absence

**Goal:** demonstrate UI certification does not require backend infrastructure.

**Inputs:** final `validate:ui` command graph and a clean environment with backend services intentionally absent.

**Required implementation:** run UI certification with no backend/Postgres/Redis/Docker backend stack and no real auth/API credentials. No real API request may escape because Storybook pure guard rejects it.

**Forbidden:** starting backend services or injecting real credentials to make UI certification pass.

**Tests:** FUIR-TST-126.

**Evidence:** environment/process statement plus command result.

**Terminal:** `IMPLEMENTED_VERIFIED`.

## FUIR-WU-074 — Record deferred application integration findings

**Goal:** retain visibility without scope creep.

**Inputs:** WU-004 deferred list plus any new integration-only findings discovered during UI work.

**Required implementation:** record current deferred findings and label all `NON_BLOCKING_FOR_UI_READINESS`.

**Forbidden:** converting a deferred integration finding into a UI blocking gate unless it directly violates a pure UI requirement in SPEC.

**Tests:** FUIR-TST-127.

**Evidence:** deferred finding list.

**Terminal:** `IMPLEMENTED_VERIFIED`.

## FUIR-WU-075 — Issue final verdict

**Goal:** produce the only legal readiness verdict.

**Inputs:** candidate-bound evidence from WU-070..074 and CERTIFICATION hard gates.

**Required implementation:** apply CERTIFICATION gates exactly.

**Forbidden:** issuing a qualified completion verdict such as `mostly ready`, `ready except`, or ignoring a failed hard gate.

**Tests:** FUIR-TST-128.

**Evidence:** final certification block.

**Terminal:** `IMPLEMENTED_VERIFIED` only when verdict is `UI_READY_FOR_PRODUCT_DEVELOPMENT_AND_UI_TESTING`; otherwise `BLOCKED` with failed gates.

---

# 3. Requirement traceability

| Requirement | Primary work units | Primary tests |
|---|---|---|
| FUIR-REQ-001 | WU-010..014 | TST-010..027 |
| FUIR-REQ-002 | WU-020..022 | TST-030..039 |
| FUIR-REQ-003 | WU-020, WU-022, WU-023 | TST-031, TST-038..043 |
| FUIR-REQ-004 | WU-030, WU-031 | TST-050..055 |
| FUIR-REQ-005 | WU-032..034 | TST-056..064 |
| FUIR-REQ-006 | WU-024 | TST-044..048 |
| FUIR-REQ-007 | WU-040..043 | TST-070..081 |
| FUIR-REQ-008 | WU-050..053 | TST-090..099 |
| FUIR-REQ-009 | WU-054, WU-055 | TST-100..104 |
| FUIR-REQ-010 | WU-060 | TST-110..112 |
| FUIR-REQ-011 | WU-062, WU-063 | TST-114..116 |
| FUIR-REQ-012 | WU-004, WU-074 | TST-005, TST-127 |
