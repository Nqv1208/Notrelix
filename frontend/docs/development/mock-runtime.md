---
document_id: FE-DEV-MOCK-RUNTIME
document_type: development-guide
status: active
owner: frontend-platform
applies_to:
  - frontend-web-development
  - frontend-mock-runtime
evidence:
  - frontend/packages/dev/mock-backend/
  - frontend/playwright.mock.config.ts
  - frontend/e2e/mock/
  - frontend/scripts/assert-no-mock-artifact.mjs
  - frontend/scripts/assert-playwright-mock-count.mjs
  - frontend/scripts/check-mock-freeze.mjs
  - frontend/tooling/contracts/enabled-consumer-surface.ts
  - frontend/packages/dev/mock-backend/src/__tests__/dataset-cardinality.unit.test.ts
  - frontend/packages/dev/mock-backend/src/__tests__/consumer-surface-closure.unit.test.ts
  - frontend/packages/dev/mock-backend/src/__tests__/mutation-truth.unit.test.ts
review_on:
  - mock-backend-contract-change
  - mock-scenario-change
  - mock-browser-gate-change
---

# Web Mock Backend Architecture (v3)

The Notrelix Web application can run fully offline without a backend service by injecting `@notrelix/dev-mock-backend` at the composition root (`apps/web/src/main.tsx`). This is application simulation, not pure UI verification data authority.

```text
UI -> Features/Hooks -> NotrelixClient -> Injected MockFetch -> OperationRegistry -> Context Handlers -> MockStore
                                  RealtimeClient -> MockRealtimeTransport
```

## Package Architecture

- **Package**: `@notrelix/dev-mock-backend` (located in `frontend/packages/dev/mock-backend/`)
- **Layer**: `dev-support` (never imported by production runtime packages)
- **Production Isolation**: Dynamic import in `main.tsx` is guarded by `import.meta.env.DEV && runtimeEnvironment.mockApi`, allowing Vite/Rolldown to tree-shake all mock backend artifacts from production bundles. Verified via `pnpm check:production-mock-isolation`.

## Runtime Seams

1. **HTTP Seam**: Real `createNotrelixClient` from `@notrelix/contracts` configured with `fetchImpl: mockFetch(store)`.
2. **Realtime Seam**: Real `createAppRuntime` from `@notrelix/runtime-web` configured with `createRealtimeClient: () => createMockRealtimeTransport()`. Zero browser WebSockets are created.
3. **Transport Security**: Closed-world routing. Unmapped requests throw `MockUnhandledOperationError` and fail closed (never fallback to real network).

## Pure UI Construction Boundary

Pure UI construction uses `frontend/docs/development/ui-construction.md`.

```text
Fixture -> Scenario -> Local Interaction Controller -> Pure UI -> Storybook evidence
```

That path does not import `@notrelix/dev-mock-backend`, boot application services, create QueryClient/auth/runtime providers, or require backend/API availability. Application mock E2E is useful integration evidence after a container is wired, but it is not an exit gate for UI DONE.

## Environment Variables

| Variable             | Type      | Allowed Values                                                                    | Default                    | Description                                      |
| :------------------- | :-------- | :-------------------------------------------------------------------------------- | :------------------------- | :----------------------------------------------- |
| `VITE_MOCK_API`      | `boolean` | `true`, `false`                                                                   | `false`                    | Enables offline mock backend runtime in dev mode |
| `VITE_MOCK_PRESET`   | `enum`    | `default`, `fast`, `slow`, `demo`, `qa-stress`                                    | `default`                  | Config preset bundle                             |
| `VITE_MOCK_PERSONA`  | `enum`    | `owner`, `admin`, `member`, `viewer`                                              | `owner`                    | Active actor persona                             |
| `VITE_MOCK_STATE`    | `enum`    | `default`, `new-user`, `empty-workspace`, `permission-limited`, `expired-session` | `default`                  | Initial business scenario                        |
| `VITE_MOCK_DENSITY`  | `enum`    | `tiny`, `normal`, `large`, `stress`                                               | `normal`                   | Data density for boards/cards/pages              |
| `VITE_MOCK_OVERLAYS` | `list`    | `unicode`, `long-titles`, `many-columns`, `many-cards`                            | `[]`                       | Composable state modifiers                       |
| `VITE_MOCK_LATENCY`  | `enum`    | `instant`, `fast`, `normal`, `slow`                                               | `instant` (tests) / `fast` | Simulated network delay                          |
| `VITE_MOCK_SEED`     | `number`  | integer                                                                           | `1001`                     | PRNG seed for deterministic data                 |

_Note: Any invalid environment value fails immediately at startup with `MockConfigurationError`._

## Persona Table

| Persona  | Actor ID           | Primary Workspace Role | Email                   |
| :------- | :----------------- | :--------------------- | :---------------------- |
| `owner`  | `mock-user-owner`  | `owner`                | `ui-dev@notrelix.local` |
| `admin`  | `mock-user-admin`  | `admin`                | `admin@notrelix.local`  |
| `member` | `mock-user-member` | `member`               | `member@notrelix.local` |
| `viewer` | `mock-user-viewer` | `guest`                | `viewer@notrelix.local` |

## Relational & Store Invariants

- Workspaces, Boards, Lists, Cards, and Pages maintain strict referential integrity.
- Unknown IDs return HTTP 404 (`notFound`) and never fall back to the first available resource.
- Workspace creation automatically adds the active user as `owner` membership.
- Card moves validate board parentage before mutating list positions.
- Secondary indexes (`boardsByWorkspaceId`, `listsByBoardId`, `cardsByListId`, `pagesByWorkspaceId`, `membersByWorkspaceId`) are verified via `store.assertInvariants()`.

## Verification Commands

- `pnpm test:node:guarded`: Runs all unit tests (including mock backend suite) with zero-test assertion guard.
- `pnpm mock:freeze:check`: Runs the consumer surface / registry closure gate.
- `pnpm mock:contract`: Runs `test:node:guarded` + `mock:freeze:check` combined (CI mock-contract job command).
- `pnpm e2e:mock`: Runs browser Playwright tests with mock backend active.
- `pnpm e2e:mock:count`: Asserts Playwright JSON results have zero failures and zero skips.
- `pnpm check:production-mock-isolation`: Verifies that `apps/web/dist` contains zero mock signatures.

## Work Management Transport Vocabulary

The mock backend uses Work Management operation IDs that match the OpenAPI schema (`WorkManagement.*`). Key operation families:

| Family                       | Operations                                                                                                                |
| :--------------------------- | :------------------------------------------------------------------------------------------------------------------------ |
| `WorkManagement.BoardViews`  | `Get` (GET), `Save` (PUT)                                                                                                 |
| `WorkManagement.BoardFields` | `GetSchema` (GET), `Create`, `Update`, `Delete`, `Reorder`                                                                |
| `WorkManagement.Labels`      | `List` (GET), `Create`, `Update`, `Delete`                                                                                |
| `WorkManagement.BoardGroups` | `Create`, `Update`, `Delete`, `Duplicate`, `Reorder`                                                                      |
| `WorkManagement.BoardItems`  | `List` (GET), `Create`, `Update`, `Delete`, `Archive`, `Duplicate`, `Move`, `UpdateFieldValue`, `AddLabel`, `RemoveLabel` |
| `WorkManagement.Checklists`  | `List` (GET), `Create`, `Update`, `Delete`, `CreateItemByChecklist`, `UpdateItem`, `DeleteItem`                           |
| `Collaboration.Comments`     | `GetBoardItemComments` (GET), `CreateBoardItemComment`, `Update`, `Delete`                                                |

## Dataset Density Semantics

Density is controlled via `VITE_MOCK_DENSITY`. All counts are exact and verified by `T-MFB-030`/`T-MFB-031`:

| Density  | Workspaces | Boards/WS | Lists/Board | Cards/List | Pages/WS | Notifications |
| :------- | :--------- | :-------- | :---------- | :--------- | :------- | :------------ |
| `tiny`   | 1          | 1         | 2           | 3          | 1        | 2             |
| `normal` | 1          | 3         | 4           | 5          | 5        | 8             |
| `large`  | 2          | 5         | 6           | 10         | 10       | 20            |
| `stress` | 3          | 8         | 10          | 50         | 30       | 50            |

The `many-cards` overlay doubles the cards-per-list count for the `normal` density.

## Compatibility Gaps Register

All compatibility gaps are declared in:

```text
frontend/tooling/contracts/enabled-consumer-surface.ts
```

Gap rows use `classification: "COMPATIBILITY_GAP_MOCKED"` and a `gapId: "CTR-GAP-TODO"` sentinel.

Current gap status:

- `CTR-GAP-TODO`: Shared sentinel for all in-progress gaps. Individual gaps will receive unique IDs when the consumer surface is fully mapped.

The mock registry provides handler stubs for all gap routes (so the UI doesn't receive network errors) but no real business logic is exercised.

## Search Behavior

Search (`/api/v1/search`) is **not implemented** in the mock backend. This is a `CONTRACT_BLOCKED_UI_DISABLED` row — the UI disables the search control when in mock mode. Zero network dispatch occurs for search queries.

## Permission-Limited Scenario

The `permission-limited` state uses the `viewer` persona. This triggers a purely view-only UI — mutations are disabled at the UI layer before being dispatched. No mock 403 responses are injected; the authorization gap is recorded as `AUTHZ-CONTRACT-GAP`.

## CI Required Commands (summary)

```bash
# Contract + freeze gate (mock-contract CI job)
pnpm mock:contract

# E2E mock suite (mock-e2e CI job, run per persona × state matrix)
VITE_MOCK_PERSONA=owner VITE_MOCK_STATE=default pnpm e2e:mock
pnpm e2e:mock:count

# Production artifact isolation (mock-artifact-isolation CI job)
pnpm check:production-mock-isolation
```
