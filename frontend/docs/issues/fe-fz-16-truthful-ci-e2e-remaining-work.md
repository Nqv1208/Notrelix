# FE-FZ-16 Issue: Truthful CI and Critical E2E Remaining Work

## Scope

Frontend updated Playwright to start a real production preview server:

- default `baseURL`: `http://127.0.0.1:4173`
- `webServer.command`: `pnpm --filter @notrelix/app-web preview --host 127.0.0.1 --port 4173`
- `reuseExistingServer` follows CI mode
- app-web `preview` script now delegates to `vite preview`

## Remaining Work

### 1. Critical E2E suites are incomplete

FE-FZ-16 requires these named suites:

- `startup-smoke.e2e.spec.ts`
- `auth-lifecycle.e2e.spec.ts`
- `workspace-isolation.e2e.spec.ts`
- `realtime-lifecycle.e2e.spec.ts`
- `work-management-optimistic.e2e.spec.ts`
- `routing-authorization.e2e.spec.ts`

Some E2E tests exist, but they do not yet cover the full critical lifecycle matrix in the plan.

### 2. Mock API fixture needs scenario state

The plan requires HTTP fixtures that control route responses and scenario state through `page.route()`. Current fixtures need to be audited and expanded to cover:

- refresh success/failure
- concurrent 401 single expiration transition
- workspace A/B isolation
- optimistic command success/failure/conflict

### 3. Realtime fixture is not complete

The plan requires a real realtime fixture using Playwright WebSocket routing or a local mock server. Remaining scenarios:

- connect after auth
- disconnect on logout
- reconnect on network close
- duplicate ignored
- stale ignored
- gap triggers recovery
- wrong workspace ignored

### 4. CI topology is partial

Current FE CI has several gates, but FE-FZ-16 calls for explicit jobs:

- `contract-codegen`
- `architecture-check`
- `storybook-build`
- `production-startup`
- `critical-e2e`

`storybook-build`, explicit `production-startup`, and explicit `critical-e2e` still need to be added once Storybook and critical suites exist.

### 5. Coverage policy is not defined

The plan requires targeted thresholds for critical packages rather than one broad repo number. This policy is not yet implemented.

## Acceptance Criteria

- Critical E2E suites exist with the exact lifecycle coverage from FE-FZ-16.
- Mock API and realtime fixtures own scenario state.
- CI separates production startup from critical E2E.
- Storybook build is included after FE-FZ-14 Storybook work lands.
- Critical package coverage thresholds are documented and enforced.
