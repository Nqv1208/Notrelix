---
document_id: FUIR-TESTS-V4
status: active
---

# Frontend UI Readiness Closure — TESTS

## 1. Test contract rules

Every test below is falsifiable. “File exists”, “looks correct”, “typecheck passes” or “test ran” is never sufficient when the protected property is more specific.

Required UI test files must not be skipped. Controlled negative fixtures/probes are required for machine gates.

## 2. Baseline and scope tests

### FUIR-TST-001 — Candidate identity

**Given:** execution starts on PR branch.
**Pass:** recorded candidate SHA equals `git rev-parse HEAD` / connector HEAD and base SHA is recorded.

### FUIR-TST-002 — Owner enumeration

**Pass:** all existing owner roots matching SPEC §8 are enumerated with deterministic sorted package names; count > 0.

### FUIR-TST-003 — UI artifact enumeration

**Pass:** production `.tsx`, manifest, story, component-test, fixture/scenario/controller counts are recorded by owner and repeated enumeration is identical.

### FUIR-TST-004 — Corrected baseline classification

**Pass:** source inspection proves board layout shell/toolbar/view-tabs and workspace contextual toolbar have no state/query/runtime/API imports, while board workspace content owns Work Management state/query at candidate SHA; any drift is recorded explicitly.

### FUIR-TST-005 — Deferred integration isolation

**Pass:** no Wave 1–7 prerequisite references backend, database, RLS, real auth, OpenAPI closure, mock consumer closure, mock E2E, or real E2E.

## 3. Manifest v2 and inventory tests

### FUIR-TST-010 — Schema v2 accepts canonical manifest

**Pass:** a full SPEC §9 manifest parses and returns normalized typed data.

### FUIR-TST-011 — Schema rejects v1 after migration

**Pass:** schemaVersion 1 fails once Wave 1 migration is active.

### FUIR-TST-012 — State enum semantics

**Pass:** `Success` and `Error` are accepted; unknown state and legacy semantic aliases outside the exact enum fail.

### FUIR-TST-013 — Path safety

**Pass:** absolute paths, `..` traversal, and cross-owner source paths fail.

### FUIR-TST-014 — Interaction/visual contract shape

**Pass:** invalid interactionCases or visualTargets (unknown viewport/theme, empty case ID, non-owner test path) fail.

### FUIR-TST-015 — Governed enumeration includes production TSX

**Pass:** synthetic owner production `src/a.tsx` and nested production TSX are enumerated.

### FUIR-TST-016 — Structural verification exclusions

**Pass:** story/test/spec/component-test/__tests__/verification TSX are not added to governed production source inventory.

### FUIR-TST-017 — Deterministic order

**Pass:** repeated enumeration returns byte-identical sorted path lists.

### FUIR-TST-018 — Path escape rejection

**Pass:** symlink/path traversal outside owner root fails closed.

### FUIR-TST-019 — Unclassified source fails

**Pass:** adding one governed TSX with no manifest classification makes `check:ui-evidence` non-zero and names the path.

### FUIR-TST-020 — Duplicate classification fails

**Pass:** one source listed under two surfaces/classifications fails and reports both owners/entries.

### FUIR-TST-021 — Stale classified path fails

**Pass:** deleted/missing pureEntry, coveredSource, or exclusion path fails.

### FUIR-TST-022 — Cross-owner covered source fails

**Pass:** one owner manifest cannot claim another owner's production source.

### FUIR-TST-023 — Empty exclusion reason fails

**Pass:** blank/generic invalid exclusion fails schema/checker.

### FUIR-TST-024 — Current source inventory closes

**Pass:** current candidate has zero unclassified, duplicate, stale, and cross-owner source classifications.

### FUIR-TST-025 — All active manifests are v2

**Pass:** discovery finds zero schemaVersion 1 active manifests.

### FUIR-TST-026 — Owner-local story/evidence binding survives migration

**Pass:** every required story ID is collected by Storybook index and belongs to its registered owner surface/state.

### FUIR-TST-027 — Manual inventory not required for gate

**Pass:** checker result is derived from manifest/source, not parsing prose inventory/debt documents.

## 4. Pure seam and action-contract tests

### FUIR-TST-030 — Board layout surfaces purity

**Pass:** board layout shell/toolbar/view-tabs registered entries pass transitive purity with no replacement state container introduced.

### FUIR-TST-031 — Board toolbar filter action

**Pass:** clicking enabled Filter control invokes the declared presentation callback exactly once; test title contains the declared FUI case marker.

### FUIR-TST-032 — Board layout view/search interactions

**Pass:** view selection and controlled search emit declared callbacks and local AddView behavior remains observable.

### FUIR-TST-033 — Board workspace pure surface has no state ownership

**Pass:** new `board-workspace-surface.tsx` has no Work Management state/query/runtime/router/API imports and passes purity.

### FUIR-TST-034 — Board workspace state rendering

**Pass:** pure surface renders Default, Loading, Error, and unsupported/edge presentation states from props without QueryClient.

### FUIR-TST-035 — Stateful adapter remains owner

**Pass:** `board-workspace-view-content.tsx` continues to own `useFullBoard`/stateful child adaptation and passes typecheck.

### FUIR-TST-036 — BoardScreen typed boundary

**Pass:** no `BoardScreen(props: any)` remains at this boundary; exported props are explicit.

### FUIR-TST-037 — Workspace toolbar controlled search

**Pass:** typing in visible search emits controlled search callback/value semantics without network/router ownership.

### FUIR-TST-038 — Workspace toolbar action emission

**Pass:** every enabled action represented by the toolbar action contract emits a typed action identifier; at least one test covers each toolbar mode that exposes distinct actions.

### FUIR-TST-039 — Workspace toolbar local controls remain local

**Pass:** ToggleGroup/local UI state works without external application providers.

### FUIR-TST-040 — Dead native button negative fixture

**Pass:** registered surface `<button>` with no action/submit/disabled/link semantics makes `check:ui-actions` fail.

### FUIR-TST-041 — Dead UI Button negative fixture

**Pass:** registered `<Button>` with no valid semantics fails.

### FUIR-TST-042 — Legal submit/disabled/link controls pass

**Pass:** submit, disabled, and asChild/link fixtures do not produce false dead-action violations.

### FUIR-TST-043 — Current registered surfaces have zero dead actions

**Pass:** `check:ui-actions` exits 0 on candidate source.

### FUIR-TST-044 — Forbidden dynamic import fails

**Pass:** pure graph dynamic-importing product state/contracts/runtime/dev-mock fails with import chain diagnostic.

### FUIR-TST-045 — Forbidden require fails

**Pass:** literal CommonJS require of forbidden owner fails.

### FUIR-TST-046 — Non-literal module edge fails closed

**Pass:** dynamic/require non-literal module specifier in a registered pure graph is rejected rather than skipped.

### FUIR-TST-047 — Browser side-effect ownership fails

**Pass:** controlled fixtures for cookie, clipboard, history, and location mutation fail purity.

### FUIR-TST-048 — Existing fetch/XHR/WebSocket/storage purity remains green/negative-tested

**Pass:** existing negative fixtures still fail and current pure graph passes.

## 5. State and deterministic data tests

### FUIR-TST-050 — Auth error is Error

**Pass:** baseline login/register submit error evidence is classified `Error`, not `EdgeData`.

### FUIR-TST-051 — Forgot-password completion is Success

**Pass:** completion evidence is `Success` unless product authority proves no inline success state, in which case it is explicit N/A rather than `ReadOnly`.

### FUIR-TST-052 — Surface-kind policy completeness

**Pass:** every policy-universe state is exactly required, delegated, or N/A.

### FUIR-TST-053 — Missing policy state fails

**Pass:** remove one state from all three accounting buckets and checker fails.

### FUIR-TST-054 — Delegation validity

**Pass:** delegation to missing/cross-owner surface or absent target state fails; valid sibling delegation passes.

### FUIR-TST-055 — N/A rationale validity

**Pass:** N/A requires non-empty reason and authority; generic placeholder reason fails.

### FUIR-TST-056 — No ambient random IDs

**Pass:** fixture/scenario/controller authorities contain no unapproved `Math.random` or `crypto.randomUUID`.

### FUIR-TST-057 — No ambient wall clock

**Pass:** same sources contain no unapproved `Date.now` or argument-less `new Date()`.

### FUIR-TST-058 — Fresh fixture objects

**Pass:** repeated fixture calls are deep-equal where defaults are stable but do not share mutable object/array identity.

### FUIR-TST-059 — Empty semantics

**Pass:** every `Empty` scenario is empty according to its surface collection semantics.

### FUIR-TST-060 — EdgeData semantics

**Pass:** EdgeData contains long text and Unicode and, where the model has optional presentation values, at least one absent/null optional value.

### FUIR-TST-061 — HighDensity exact cardinality

**Pass:** every required HighDensity scenario documents and repeatedly produces the same exact counts.

### FUIR-TST-062 — Story data authority

**Pass:** non-trivial product/feature stories consume owner fixture/scenario authority rather than duplicating application mock data inline.

### FUIR-TST-063 — Controller source scenario unchanged

**Pass:** controller mutation does not mutate the input scenario object.

### FUIR-TST-064 — Controller instance isolation

**Pass:** two controller instances from the same scenario do not observe each other's mutations.

## 6. Interaction behavior tests

### FUIR-TST-070 — Interaction requires cases

**Pass:** surface with `interaction` and zero `interactionCases` fails schema/checker.

### FUIR-TST-071 — Non-interaction surface rejects cases

**Pass:** cases on a surface without `interaction` fail.

### FUIR-TST-072 — Duplicate/invalid case IDs fail

**Pass:** duplicate, blank, or invalid case IDs fail.

### FUIR-TST-073 — Passing marker satisfies case

**Pass:** a passing assertion titled with exact `FUI[surface:case]` satisfies one declared case.

### FUIR-TST-074 — Missing/failed/skipped case fails

**Pass:** each of missing, failed, and skipped required case fixtures makes coverage non-zero.

### FUIR-TST-075 — Unknown marker fails

**Pass:** undeclared surface/case marker in report is reported as drift.

### FUIR-TST-076 — Passing unrelated test is insufficient

**Pass:** file with green unrelated assertion but missing required marker fails.

### FUIR-TST-077 — Synthetic complete report passes

**Pass:** report containing exactly all declared passing markers exits 0.

### FUIR-TST-078 — Current interaction contract closes

**Pass:** candidate `test:web:guarded` reports zero missing/failed/skipped interaction cases.

### FUIR-TST-079 — Mandatory baseline behavior cases exist

**Pass:** board layout, workspace contextual toolbar, Kanban/Table/auth and other existing interactive owners have explicit user-visible case IDs matching their exposed action contracts.

### FUIR-TST-080 — Keyboard-operable controls have keyboard evidence

**Pass:** representative buttons/tabs/forms/menus are operable with native/Radix keyboard semantics and declared case markers.

### FUIR-TST-081 — No artificial keyboard handlers

**Pass:** tests rely on native/component semantics; source does not add redundant custom keyboard handlers solely for test satisfaction.

## 7. Visual, theme, a11y, and network tests

### FUIR-TST-090 — Canonical viewport registry

**Pass:** registry contains exactly mobile 390x844, tablet 768x1024, desktop 1440x900.

### FUIR-TST-091 — Canonical theme registry

**Pass:** registry contains exactly light/dark and rejects unknown theme IDs.

### FUIR-TST-092 — Storybook light theme semantics

**Pass:** rendered iframe root has the same light theme root contract as Web app.

### FUIR-TST-093 — Storybook dark theme semantics

**Pass:** dark target applies the same Web root dark class/data contract and changes theme tokens without relying only on background parameter.

### FUIR-TST-094 — Desktop/light baseline required

**Pass:** every visual story has desktop/light target; remove one and checker fails.

### FUIR-TST-095 — Responsive Default coverage

**Pass:** every `responsive=true` surface Default story has mobile/light and tablet/light targets.

### FUIR-TST-096 — Theme-aware Default coverage

**Pass:** every `themeAware=true` surface Default story has desktop/dark target.

### FUIR-TST-097 — Runner executes exact target list

**Pass:** expected target count equals Playwright visual test count; no undeclared target is run.

### FUIR-TST-098 — Snapshot identity stable

**Pass:** snapshot path uses `<storyId>--<viewport>--<theme>.png` and duplicate identities fail.

### FUIR-TST-099 — Clean second visual run

**Pass:** after approved baseline generation, second non-update run has zero unexpected diffs/missing snapshots.

### FUIR-TST-100 — A11y target count non-zero

**Pass:** manifest-driven a11y runner executes at least one target and count equals expected a11y targets.

### FUIR-TST-101 — No serious/critical Axe violation

**Pass:** blocking Axe violation count is zero.

### FUIR-TST-102 — Network target count non-zero

**Pass:** purity/network runner executes expected registered targets.

### FUIR-TST-103 — Pure network attempt fails

**Pass:** controlled story attempting fetch/XHR/WebSocket fails with PureUiNetworkAccessError evidence.

### FUIR-TST-104 — Guard restore isolation

**Pass:** repeated install/render/unmount cycles restore original globals and do not leak guard wrappers.

## 8. Developer workflow and CI tests

### FUIR-TST-110 — `validate:ui` exact positive graph

**Pass:** recursively resolved script graph contains all commands required by SPEC §15.

### FUIR-TST-111 — `validate:ui` forbidden graph absence

**Pass:** recursively resolved graph contains none of codegen/mock/real/backend forbidden commands.

### FUIR-TST-112 — `validate:ui` current source passes

**Pass:** command exits 0 on candidate SHA.

### FUIR-TST-113 — Developer guide matches machine contract

**Pass:** guide names manifest v2, semantic state coverage, action contract, FUI case markers, responsive/theme policy, `validate:ui`, and application-integration deferral.

### FUIR-TST-114 — `ui-foundation` keeps UI owner

**Pass:** existing workflow still has one `ui-foundation` job using the pinned renderer and UI checks; no second UI workflow/job authority is created.

### FUIR-TST-115 — CI UI lane has no application integration dependency

**Pass:** job needs/direct script graph contains no mock-contract/mock-e2e/real-e2e/backend/Docker/database commands.

### FUIR-TST-116 — Negative command indirection is caught

**Pass:** a synthetic package script that indirectly invokes a forbidden command causes the static command-graph checker to fail.

## 9. Final certification tests

### FUIR-TST-120 — Final UI command pass

**Pass:** `pnpm validate:ui` exits 0 on final candidate SHA.

### FUIR-TST-121 — Source coverage zero-debt

**Pass:** unclassified, duplicate, stale, and cross-owner classification counts are all zero.

### FUIR-TST-122 — State/action/interaction zero-debt

**Pass:** unaccounted state count, dead action count, missing interaction case count, failed required case count, and skipped required case count are all zero.

### FUIR-TST-123 — Visual evidence complete

**Pass:** expected visual target count equals passing target count and clean second-run diff count is zero.

### FUIR-TST-124 — A11y evidence complete

**Pass:** a11y target count > 0 and blocking violation count = 0.

### FUIR-TST-125 — Network evidence complete

**Pass:** network target count > 0 and network violation count = 0.

### FUIR-TST-126 — Backend-absent certification

**Pass:** final UI command/evidence runs with no backend/Postgres/Redis/backend Docker stack/real auth credentials/real API availability.

### FUIR-TST-127 — Deferred findings remain non-blocking and visible

**Pass:** current integration findings are listed as `DEFERRED_APPLICATION_INTEGRATION / NON_BLOCKING_FOR_UI_READINESS`.

### FUIR-TST-128 — Final verdict legality

**Pass:** verdict is exactly `UI_READY_FOR_PRODUCT_DEVELOPMENT_AND_UI_TESTING` only when all hard gates pass; otherwise exactly `NOT_UI_READY` with failed gate/test IDs.
