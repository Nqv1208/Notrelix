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
review_on:
  - mock-backend-contract-change
  - mock-scenario-change
  - mock-browser-gate-change
---

# Web Mock Backend Architecture (v3)

The Notrelix Web application can run fully offline without a backend service by injecting `@notrelix/dev-mock-backend` at the composition root (`apps/web/src/main.tsx`).

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
- `pnpm e2e:mock`: Runs browser Playwright tests with mock backend active.
- `pnpm check:production-mock-isolation`: Verifies that `apps/web/dist` contains zero mock signatures.
