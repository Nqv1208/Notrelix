---
document_id: FUIR-DECISIONS-V4
status: accepted
baseline_sha: f7936f336c01d8f413e9cf5b56f8b5e8a4de76d0
---

# Frontend UI Readiness Closure — DECISIONS

These decisions are implementation locks. Syntax-level details follow existing same-owner precedent; architecture and mechanism are fixed by these locks.

## FUIR-LOCK-01 — Keep exactly five execution authorities

The only execution authorities are SPEC, DECISIONS, PLAN, TESTS, and CERTIFICATION. Do not create new architectural decision documents, side plans, or alternate checklists for this execution.

## FUIR-LOCK-02 — UI-only scope

This execution ends at UI construction/test readiness. Application integration and backend parity are deferred and non-blocking.

## FUIR-LOCK-03 — Preserve the two-lane frontend architecture

Keep:

```text
UI lane: Fixture -> Scenario -> Local Controller -> Pure UI -> Storybook/Test
Application lane: Container -> State -> Service -> Client -> Mock/Real transport
```

Never move transport/state/auth semantics into the UI lane.

## FUIR-LOCK-04 — No product-local fake service for UI

Do not recreate `mock-service`, fake repository, fake API client, MSW application transport, or product-local HTTP handler to make Storybook work.

## FUIR-LOCK-05 — Owner-local manifest v2 is the single machine authority

Upgrade every active `verification/ui-evidence.manifest.json` to schema version 2 in the same wave. No mixed v1/v2 state is allowed after Wave 1.

Do not introduce a second global surface manifest.

## FUIR-LOCK-06 — Governed source roots are fixed

Enumerate production `.tsx` under each manifest owner `src/` tree for:

```text
packages/ui/web
packages/product/*/web
packages/features/*
```

Only the structural verification/test/story patterns listed in SPEC §8 are automatically ignored.

## FUIR-LOCK-07 — Source classification is exactly-one

Every governed source is exactly one of:

```text
pureEntry
coveredSources
inventory.excludedSources
```

Zero classifications or multiple classifications are hard errors.

## FUIR-LOCK-08 — Manifest v2 shape is fixed by SPEC §9

Implement SPEC §9 as written: owner, inventory exclusions, surfaceKind, pureEntry, coveredSources, stories with visualTargets, stateCoverage required/delegated/notApplicable, responsive, themeAware, checks, and interactionCases.

Do not add optional escape hatches that allow source/state/interaction/visual coverage to be omitted silently.

## FUIR-LOCK-09 — Semantic state vocabulary is fixed

Use exactly:

```text
Default Loading Empty Error Success Unavailable ReadOnly PermissionLimited EdgeData HighDensity
```

Migrate baseline semantic misuse rather than retaining aliases. In particular, auth submit error is `Error`, not `EdgeData`; forgot-password completion is `Success`, not `ReadOnly`.

## FUIR-LOCK-10 — State delegation is explicit

A main data surface may delegate Loading/Error/etc. only to an existing sibling registered surface. Delegation records target surface and target state and is machine validated.

## FUIR-LOCK-11 — Baseline board layout files are registered, not re-architected

At baseline:

```text
board-layout-shell.tsx
board-toolbar.tsx
view-tabs.tsx
```

are already presentation components. Do not extract replacement containers/surfaces for them solely for this execution. Register/test the existing components and add missing typed action callbacks where required.

## FUIR-LOCK-12 — Board workspace composition gets one explicit seam

Create:

```text
frontend/packages/product/work-management/web/src/components/board-workspace-surface.tsx
```

`BoardWorkspaceSurface` owns presentation selection/loading/error/unsupported rendering from typed props. `board-workspace-view-content.tsx` remains the stateful adapter and owns `useFullBoard`/stateful child containers.

Replace `BoardScreen(props: any)` with typed props while touching this boundary.

Do not move query ownership into the new surface.

## FUIR-LOCK-13 — Workspace contextual toolbar stays one surface

Keep `workspace-contextual-toolbar.tsx` as the presentation owner. Add typed search/action callbacks to existing enabled controls and register it directly. Do not create a new container solely for Storybook.

## FUIR-LOCK-14 — No dead enabled action

Add `check-ui-actions` under existing frontend tooling. For registered owner-local surface sources, native `<button>` and `@notrelix/ui-web` `<Button>` are valid only when they have one of:

```text
onClick / equivalent callback
form submit semantics
disabled / aria-disabled
asChild/link semantics
```

Known Radix/compound controls with intrinsic local behavior are validated by interaction tests rather than rejected mechanically.

## FUIR-LOCK-15 — Pure side effects become callbacks

Clipboard, router/history navigation, storage, cookie, API, auth, and persistence effects are container/host responsibilities. Pure surfaces emit typed callbacks/actions.

## FUIR-LOCK-16 — Verification data uses existing owner authority

Reuse existing product testing packages when they exist. Otherwise keep verification fixtures in the owning package's existing `src/verification` convention. Do not create a new shared product-fixture package.

## FUIR-LOCK-17 — Verification data is deterministic

No `Math.random`, `crypto.randomUUID`, `Date.now`, argument-less `new Date`, module-level mutable scenario state, or network primitives in fixture/scenario/controller code.

## FUIR-LOCK-18 — Controllers clone before mutation

Controller instances deep-clone scenario input and never share mutable state. Controllers model presentation transitions only.

## FUIR-LOCK-19 — Purity traversal is transitive and import-form complete

`check-ui-purity.ts` must apply the same forbidden ownership rules to static imports/exports, literal dynamic `import()`, and literal CommonJS `require()`.

Non-literal dynamic module specifiers inside a registered pure graph fail closed.

## FUIR-LOCK-20 — Runtime network guard remains

Keep `installPureUiNetworkGuard` for fetch/XHR/WebSocket. Static purity and runtime network evidence are both required.

## FUIR-LOCK-21 — Interaction coverage is case-level

Use `FUI[<surfaceId>:<caseId>]` markers and require every manifest case to have a passing Vitest assertion. File-level execution alone never satisfies interaction closure.

## FUIR-LOCK-22 — Visual target policy is minimal but sufficient

Do not generate every state × viewport × theme permutation. Enforce SPEC §14:

- all visual stories: desktop/light;
- responsive Default: mobile/light + tablet/light;
- theme-aware Default: desktop/dark.

## FUIR-LOCK-23 — Theme evidence uses application theme semantics

Create one Storybook theme adapter in existing Storybook tooling that applies the same Web light/dark class/data contract. Background color controls are not certification evidence.

## FUIR-LOCK-24 — `validate:ui` is exact and additive

Add the exact command composition in SPEC §15. Do not alter `validate` or `validate:fast` behavior.

## FUIR-LOCK-25 — Keep `ui-foundation` as the CI owner

Do not redesign the Frontend gate or delivery routing. Update only the existing `ui-foundation` job/scripts required to execute the new UI contract.

## FUIR-LOCK-26 — UI CI cannot call application integration lanes

`ui-foundation` and `validate:ui` must not invoke mock contract/freeze, mock E2E, real E2E, backend, Docker, or database commands.

## FUIR-LOCK-27 — No synthetic placeholder UI for N/A packages

If an owner has no production Web `.tsx` source at candidate SHA, record `NOT_APPLICABLE` with source enumeration evidence. Do not manufacture UI to satisfy coverage.

## FUIR-LOCK-28 — Deferred integration remains visible

Record application integration findings in final certification under `DEFERRED_APPLICATION_INTEGRATION`. Do not silently call them complete and do not let them fail UI certification.

## FUIR-LOCK-29 — Evidence is candidate-SHA bound

Every wave evidence record contains candidate SHA, changed paths, commands, exit codes/counts, and terminal state. Evidence from another SHA cannot certify the current candidate.
