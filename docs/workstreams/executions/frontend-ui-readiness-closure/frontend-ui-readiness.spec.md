---
document_id: FUIR-SPEC-V4
status: active
owner: frontend-platform
baseline_pr: 115
baseline_branch: feature/web-app
baseline_sha: f7936f336c01d8f413e9cf5b56f8b5e8a4de76d0
scope: ui-construction-and-ui-test-readiness
---

# Frontend UI Readiness Closure — SPEC

## 1. Purpose

This execution closes only the remaining gaps required for Notrelix teams to build, review, interact with, and regression-test governed Web UI without a running backend, real authentication, real API, database, RLS, or application mock-backend semantic parity.

The terminal outcome is:

```text
UI_READY_FOR_PRODUCT_DEVELOPMENT_AND_UI_TESTING
```

The certified path is:

```text
Product semantics
  -> Pure presentation surface
  -> Deterministic fixture
  -> Deterministic scenario
  -> Local interaction controller when stateful UI behavior is needed
  -> Owner-local Storybook story
  -> Component interaction evidence
  -> A11y / visual / zero-network evidence
  -> UI DONE
```

## 2. Authority and mandatory read order

A coding agent MUST read these files in this order before editing source:

1. `frontend-ui-readiness.spec.md`
2. `frontend-ui-readiness.decisions.md`
3. `frontend-ui-readiness.plan.md`
4. `frontend-ui-readiness.tests.md`
5. `frontend-ui-readiness.certification.md`

Authority precedence is:

```text
canonical repository architecture/docs
  > accepted DECISIONS
  > SPEC
  > PLAN
  > TESTS
  > CERTIFICATION
```

If candidate source materially contradicts a locked architectural direction and the conflict cannot be resolved mechanically, terminate the affected work unit as `BLOCKED` with source evidence. Do not invent a new architecture.

## 3. Execution terminal states

Every work unit terminates as exactly one of:

```text
IMPLEMENTED_VERIFIED
KEEP_EXISTING_VERIFIED
NOT_APPLICABLE
BLOCKED
```

Illegal terminal states include `PARTIAL`, `DONE_ENOUGH`, `FOLLOW_UP_LATER`, silent omission, or declaring a wave complete while a required work unit has no legal terminal state.

## 4. Scope

### 4.1 In scope

- machine-closed inventory of governed Web UI source;
- pure presentation boundaries;
- typed callback/action contracts for interactive UI;
- deterministic fixtures, scenarios, capability fixtures, fixed clocks, and local controllers;
- owner-local Storybook stories and evidence manifests;
- semantic state coverage;
- component interaction test coverage;
- runtime zero-network protection for pure UI;
- accessibility evidence;
- responsive visual evidence;
- application-theme visual evidence;
- UI-only developer validation command;
- UI-specific CI evidence;
- final candidate-SHA certification.

### 4.2 Explicitly out of scope

The following are `DEFERRED_APPLICATION_INTEGRATION` and MUST NOT block UI completion:

- backend implementation;
- backend authentication/session implementation;
- database or migrations;
- RLS/tenant-context changes;
- real HTTP/API availability;
- OpenAPI producer completion;
- generated client closure;
- production API mapper cleanup unrelated to a pure presentation boundary;
- `enabled-consumer-surface.ts` / `CTR-GAP-TODO` closure;
- mock-backend 403/authz parity;
- mock-backend full semantic parity;
- search mock transport closure;
- real-backend E2E;
- Docker backend stack;
- mock-real parity.

## 5. Baseline audit — PR #115 HEAD

Baseline HEAD is `f7936f336c01d8f413e9cf5b56f8b5e8a4de76d0`.

### 5.1 Architecture already good and must be preserved

The branch already has the correct foundation:

- UI construction is documented separately from application mock runtime.
- `@notrelix/work-management-testing` owns deterministic fixtures/scenarios/controllers rather than a product-local fake API service.
- `renderPureUi` renders without a QueryClient and installs a zero-network guard.
- Storybook discovers owner-local product/feature/UI stories.
- Storybook installs the pure UI fetch/XHR/WebSocket guard.
- `check-ui-purity.ts` walks static transitive imports and rejects state/runtime/contracts/dev-mock ownership.
- owner-local `ui-evidence.manifest.json` files bind surfaces, states, stories, interaction tests, a11y, visual, and purity.
- manifest-driven Playwright runners exist for a11y, visual, and zero-network evidence.
- `ui-foundation` CI already runs `check:ui-purity`, `check:ui-evidence`, and `test:ui:freeze`.
- a fixed UI test reference instant exists.

These mechanisms are extended, not replaced.

### 5.2 Corrected source findings from the re-audit

The previous debt description was too coarse. At the baseline SHA:

1. `board-layout-shell.tsx` is already a pure props/callback presentation component. It does not need a new container/presentation split.
2. `board-toolbar.tsx` is already presentation-only, but the visible Filter control has no callback contract; it needs action-contract closure and evidence, not a structural rewrite.
3. `view-tabs.tsx` is presentation/local-state UI and can be registered directly.
4. `workspace-contextual-toolbar.tsx` is already presentation-only, but many visible controls are currently dead actions with no callback contract. It requires callback/action semantics plus evidence, not a new state container.
5. `board-workspace-view-content.tsx` genuinely owns `useFullBoard`, renders stateful view containers, and still exposes `BoardScreen(props: any)`. This is the one known Work Management composition that requires an explicit pure presentation seam.
6. manifest visual evidence currently runs only at `1440x900`.
7. Storybook backgrounds do not apply the Web application's actual light/dark theme contract.
8. current interaction coverage proves that declared test files ran, not that every declared behavior case ran.
9. current manifest schema allows semantically weak state naming. Example baseline auth stories use `EdgeData` for submit error and `ReadOnly` for a forgot-password success state; schema v2 must make `Error` and `Success` first-class semantics.
10. current purity traversal does not fully close dynamic `import()` / CommonJS `require()` ownership bypasses.
11. `validate:fast` includes codegen and mock-freeze work; there is no UI-only developer command.
12. current manifest checker proves registered surfaces are valid but does not prove every governed production `.tsx` source has been classified.

## 6. Requirements

### FUIR-REQ-001 — Machine-closed governed source inventory

Every governed production Web `.tsx` file must be classified exactly once as a registered surface source, a source covered by one registered surface, or an explicit non-surface exclusion.

### FUIR-REQ-002 — Known baseline source debt must be closed accurately

The three baseline debt areas must be closed according to the corrected source findings in §5.2; pure files are registered and tested directly, while only the stateful board workspace composition receives a new presentation seam.

### FUIR-REQ-003 — Registered interactive UI must not contain dead enabled actions

A user-visible enabled action on a registered surface must either perform local presentation state behavior or emit a typed callback/action event. A visible control with no effect is not UI-ready.

### FUIR-REQ-004 — State evidence must use semantic states

State evidence must distinguish `Error`, `Success`, `EdgeData`, `Loading`, `Empty`, `ReadOnly`, and permission states rather than repurposing unrelated labels to make the manifest pass.

### FUIR-REQ-005 — Verification data must be deterministic and isolated

Fixtures/scenarios/controllers must have stable IDs/time/cardinality and fresh object graphs. No ambient random/time/network/application state is allowed.

### FUIR-REQ-006 — Pure UI ownership must fail closed

Static imports, dynamic imports, CommonJS requires, network primitives, storage, clipboard, cookie, router/navigation/history ownership, runtime, auth/session, contracts, product state, and dev mock backend are forbidden in registered pure graphs unless represented by typed presentation inputs/callbacks.

### FUIR-REQ-007 — Interaction proof must be behavior-case-level

A passing test file is insufficient. Every manifest-declared interaction case must appear as a passing test assertion for that exact surface/case ID.

### FUIR-REQ-008 — Visual evidence must prove responsive and theme behavior without exploding every permutation

Every visual story keeps a desktop/light baseline. Responsive surfaces additionally prove their Default state at mobile and tablet. Theme-aware surfaces additionally prove their Default state under dark application theme semantics.

### FUIR-REQ-009 — A11y and zero-network evidence remain mandatory

Required a11y targets must have zero serious/critical Axe violations. Pure Storybook targets must make zero fetch/XHR/WebSocket attempts.

### FUIR-REQ-010 — UI developers need one canonical scoped command

`pnpm validate:ui` must validate UI readiness without invoking codegen, application mock contract checks, mock E2E, real E2E, backend, Docker, or database work.

### FUIR-REQ-011 — UI CI evidence must remain independently observable

The existing `ui-foundation` job remains the UI-specific CI lane. Its direct command graph may validate UI readiness but must not depend on application mock/real-backend jobs.

### FUIR-REQ-012 — Backend/application integration debt remains visible but non-blocking

Deferred integration findings must be recorded in final certification but cannot cause UI certification failure.

## 7. Canonical UI architecture

```text
                       product semantics
                              |
                              v
                    +--------------------+
                    | pure UI surface    |
                    | props + callbacks  |
                    +----------+---------+
                               |
             +-----------------+------------------+
             |                                    |
             v                                    v
    deterministic data                    capability inputs
 fixture -> scenario                             |
             |                                    |
             +-----------------+------------------+
                               v
                    local UI controller
                    only when interaction
                    needs local state
                               |
                               v
                  Storybook / renderPureUi
                               |
          +----------+---------+---------+----------+
          |          |                   |          |
          v          v                   v          v
     interaction    a11y              visual    zero-network
          \          |                   |          /
           +---------+-------------------+---------+
                               |
                               v
                            UI DONE
```

Application integration remains a separate lane:

```text
container -> state/query -> service -> NotrelixClient -> mock/real transport
```

The second lane is not a prerequisite for the first lane.

## 8. Governed source inventory

Manifest discovery governs these owner roots:

```text
frontend/packages/ui/web
frontend/packages/product/*/web
frontend/packages/features/*
```

Within each owner, enumerate production `src/**/*.tsx`.

The enumerator structurally ignores only:

```text
**/__tests__/**
**/*.test.tsx
**/*.component.test.tsx
**/*.spec.tsx
**/*.stories.tsx
**/verification/**
```

Every remaining file must be classified exactly once by manifest v2.

## 9. Manifest v2 semantic model

Manifest v2 is owner-local and has one source of truth per package.

Canonical shape:

```json
{
  "schemaVersion": 2,
  "owner": "@notrelix/work-management-web",
  "inventory": {
    "excludedSources": [
      {
        "path": "src/components/example-container.tsx",
        "category": "container",
        "reason": "Owns query state and adapts it into registered pure surfaces."
      }
    ]
  },
  "surfaces": [
    {
      "surfaceId": "wm.example.surface",
      "surfaceKind": "data",
      "pureEntry": "src/components/example-surface.tsx",
      "coveredSources": [],
      "stories": [
        {
          "id": "work-management-example--default",
          "state": "Default",
          "visualTargets": [
            { "viewport": "desktop", "theme": "light" },
            { "viewport": "mobile", "theme": "light" },
            { "viewport": "tablet", "theme": "light" },
            { "viewport": "desktop", "theme": "dark" }
          ]
        }
      ],
      "stateCoverage": {
        "required": ["Default"],
        "delegated": [],
        "notApplicable": []
      },
      "responsive": true,
      "themeAware": true,
      "checks": ["interaction", "a11y", "visual", "purity"],
      "interactionCases": [
        {
          "id": "submit",
          "testFile": "src/components/__tests__/example.interaction.component.test.tsx"
        }
      ]
    }
  ]
}
```

### 9.1 Source classification

A governed production source is classified by exactly one of:

- `pureEntry` of one surface;
- `coveredSources` of one surface;
- `inventory.excludedSources`.

`excludedSources.category` is one of:

```text
container
route
provider
application-adapter
nonvisual-composition
platform-specific
```

An exclusion has a non-empty concrete reason. “Not critical”, “not needed”, or “covered elsewhere” without a surface ID is invalid.

### 9.2 Canonical states

Schema v2 states are exactly:

```text
Default
Loading
Empty
Error
Success
Unavailable
ReadOnly
PermissionLimited
EdgeData
HighDensity
```

`EdgeData` is data-shape stress only: long text, Unicode, absent optional values, overflow-prone values. It is never an alias for error or success.

### 9.3 State coverage accounting

For each state in the policy universe of a `surfaceKind`, the manifest must account for it exactly once as:

- `required` — the same surface has a story for that state;
- `delegated` — a dedicated sibling surface owns that state; or
- `notApplicable` — includes `reason` and `authority`.

`delegated` records:

```json
{ "state": "Loading", "surfaceId": "wm.kanban.loading", "targetState": "Loading" }
```

Surface-kind policy universes:

| surfaceKind | state universe |
|---|---|
| `data` | Default, Loading, Empty, Error, EdgeData, HighDensity, ReadOnly, PermissionLimited |
| `form` | Default, Loading, Error, Success, EdgeData, ReadOnly |
| `navigation` | Default, EdgeData, ReadOnly |
| `shell` | Default, Loading, Error, EdgeData |
| `composition` | Default, Loading, Error, EdgeData |
| `feedback` | Default |
| `primitive` | Default, EdgeData |

A delegated state must point to an existing owner-local registered surface whose `required` states contain `targetState`.

## 10. Interactive action contract

For registered surfaces:

- local presentation toggles are legal when they only affect local UI state;
- external effects are exposed as typed callbacks or typed action events;
- enabled native `<button>` or `@notrelix/ui-web` `<Button>` controls may not be dead;
- submit buttons are legal without `onClick` only when `type="submit"` and the form exposes `onSubmit`;
- disabled controls must be explicitly disabled and remain accessible;
- router navigation, clipboard, browser history, storage, API, auth, and persistence effects remain outside the pure surface.

Known baseline remediation:

- `BoardToolbar` must expose the Filter action instead of rendering an enabled dead Filter button.
- `WorkspaceContextualToolbar` must expose typed UI action/search callbacks for enabled controls instead of rendering no-op actions.

## 11. Deterministic verification-data contract

Fixtures:

- return fresh objects;
- accept typed overrides;
- use stable IDs;
- use fixed timestamps/reference dates;
- contain no network/application provider dependency.

Scenarios:

- compose render-complete surface data;
- return fresh object graphs;
- have explicit deterministic cardinality;
- use semantic state names;
- do not encode HTTP/backend behavior.

Local controllers:

- deep-clone scenario data before mutation;
- keep mutation state instance-local;
- expose the same presentation callback contract used by the production container;
- do not implement retry, cache invalidation, auth refresh, HTTP statuses, RLS, persistence, or realtime transport.

Forbidden in fixture/scenario/controller code:

```text
Math.random()
crypto.randomUUID()
Date.now()
argument-less new Date()
module-level mutable scenario state
fetch/XMLHttpRequest/WebSocket
```

## 12. Pure UI ownership contract

Registered pure graphs must fail on:

- product state packages;
- `@notrelix/contracts`;
- runtime packages;
- `@notrelix/dev-mock-backend`;
- auth/session ownership;
- QueryClient creation;
- fetch/XHR/WebSocket;
- localStorage/sessionStorage/indexedDB;
- `document.cookie`;
- `navigator.clipboard`;
- router ownership / direct history navigation;
- static forbidden imports;
- dynamic forbidden imports;
- CommonJS forbidden requires.

## 13. Interaction evidence contract

Each surface with `interaction` check declares one or more `interactionCases`.

Each required test assertion title contains exactly:

```text
FUI[<surfaceId>:<caseId>]
```

Examples:

```text
FUI[wm.kanban.board:create-card]
FUI[wm.board-layout.toolbar:filter]
FUI[workspace.contextual-toolbar:search]
FUI[workspace.contextual-toolbar:action]
```

The coverage checker proves every declared case has a passing assertion. A passing file with missing case markers fails.

## 14. Visual evidence contract

Canonical viewports:

```text
mobile   390x844
tablet   768x1024
desktop  1440x900
```

Canonical themes:

```text
light
dark
```

Rules:

1. every visual story includes `desktop/light`;
2. if `responsive=true`, the surface's `Default` story also includes `mobile/light` and `tablet/light`;
3. if `themeAware=true`, the surface's `Default` story also includes `desktop/dark`;
4. visual runner executes exactly manifest-declared targets;
5. snapshot name is `<storyId>--<viewport>--<theme>.png`;
6. Linux Chromium snapshots are authoritative CI evidence;
7. Storybook applies the same Web theme class/data contract, not a background-only approximation.

This proves responsive/theme behavior without forcing every state through every permutation.

## 15. Canonical UI-only validation command

Root script must be exactly the UI-scoped composition:

```text
validate:ui =
  check:architecture
  + check:ui-purity
  + check:ui-actions
  + check:ui-evidence
  + typecheck
  + lint
  + format:check
  + test:web:guarded
  + test:ui:freeze
```

It must not execute:

```text
codegen:check
mock:freeze:check
mock:contract
e2e:mock
e2e:real
backend/docker/database commands
```

Existing `validate` and `validate:fast` semantics remain unchanged.

## 16. UI Definition of Done

A governed surface is UI DONE only when:

1. source inventory is machine-classified;
2. pure entry and covered sources are explicit;
3. pure graph passes ownership checks;
4. enabled actions are not dead;
5. fixture/scenario data is deterministic when data-bearing;
6. local controller exists when stateful interaction simulation is required;
7. semantic state coverage is complete by required/delegated/N/A accounting;
8. every interaction case passes;
9. required a11y evidence passes;
10. required zero-network evidence passes;
11. declared visual targets pass;
12. no backend/auth/API availability is required.

System terminal certification additionally requires all governed sources and all manifests to meet these rules at one candidate SHA.
