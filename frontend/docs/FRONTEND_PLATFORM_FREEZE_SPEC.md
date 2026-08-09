# Frontend Platform Freeze Specification

> **Document Status:** Active Specification  
> **Target Version:** `frontend-platform-v1.0`  
> **Scope:** Architecture Contracts, Composition Root, Environment, API Layer, Realtime, State Ownership, Routing, Observability, and UI Foundation.

---

## 1. Overview & Strategy

### 1.1 Definition of Frontend Platform Freeze

Frontend Platform Freeze is the architectural stabilization milestone where all foundational capabilities (Runtime, API Access, Auth Session, Workspace Isolation, Realtime Transport, State Ownership, Routing, and UI Primitives) are locked down so that **any new feature module can be built using a standardized, predictable template without modifying foundational packages or composition roots.**

Frontend Platform Freeze **does not** imply stopping UI additions, blocking new API endpoints, or freezing product features.

### 1.2 Three-Tier Freeze Hierarchy

| Freeze Level               | Scope & Description                                                                                                                                 | Target Outcome                             |
| :------------------------- | :-------------------------------------------------------------------------------------------------------------------------------------------------- | :----------------------------------------- |
| **Platform Freeze**        | Core runtime, API client, auth lifecycle, workspace context, query engine, realtime transport, routing boundaries, UI foundation, CI quality gates. | Foundation locked; zero legacy singletons. |
| **Module Contract Freeze** | Public API contracts, state ownership, query keys, typed routes, permissions, and initial vertical slice per module.                                | Individual module boundaries locked.       |
| **Release Freeze**         | Production release candidate lock (pre-deployment regression freeze).                                                                               | Final release staging check.               |

---

## 2. Core Architectural Principles & Boundaries

### 2.1 Package Dependency Matrix

```
Browser Environment / Vite Host
              ↓
  apps/web (Composition Root: main.tsx)
              ↓
      createAppRuntime()
              ↓
     AppRuntimeProvider
              ↓
┌─────────────────────────────────────────────────────────┐
│ @notrelix/runtime-web                                   │
│  ├── NotrelixClient (API)                               │
│  ├── RealtimeClient (WebSocket)                         │
│  ├── AuthSessionAdapter                                 │
│  ├── ClockPort / TelemetryPort                          │
│  └── ResolvedEnv                                        │
└─────────────────────────────────────────────────────────┘
              ↓
┌─────────────────────────────────────────────────────────┐
│ Feature Providers & Shell                               │
│  ├── AuthProvider & WorkspaceProvider                   │
│  ├── PermissionGuard & WorkspaceGuard                   │
│  └── TanStack Query & Global Error Boundaries           │
└─────────────────────────────────────────────────────────┘
              ↓
┌─────────────────────────────────────────────────────────┐
│ Product Modules (Vertical Slices)                       │
│  ├── Work Management (@notrelix/wm-core / state / web)  │
│  ├── Documents (@notrelix/docs-core / web)              │
│  ├── Collaboration / Notifications / Activity           │
│  ├── Search / Automation / Integrations                 │
│  └── Billing & Entitlements                             │
└─────────────────────────────────────────────────────────┘
```

### 2.2 Single Dependency Path Rule

No component or feature package may directly import or execute:

- `import.meta.env` or `process.env` (only `apps/web/src/main.tsx` and runtime factories read host environment).
- Legacy global API singletons (`import { api, configureApi } from '@notrelix/contracts'`).
- Un-injected WebSocket instances or ad-hoc `new WebSocket()` connections.

---

## 3. Detailed Specification by Architectural Area

### 3.1 Composition Root & AppRuntime Specification

#### Contract Definition

```ts
export interface AppRuntime {
  readonly api: NotrelixClient;
  readonly realtime: RealtimeClientPort;
  readonly auth: AuthSessionPort;
  readonly clock: ClockPort;
  readonly telemetry: TelemetryPort;
  readonly featureFlags: FeatureFlagsPort;
  readonly env: ResolvedEnv;
}
```

#### Verification & Exit Gate

- Zero references to `configureApi`, `global api`, or `activeBaseUrl` across the entire codebase.
- `MIGRATION_TRACKER.md` reaches 0 remaining legacy files.
- Architecture static checks fail CI if `@notrelix/contracts` `api` object is imported.

---

### 3.2 Single Source of Truth Environment Specification

#### Schema Input

```ts
export interface RuntimeEnvironmentInput {
  readonly mode: "development" | "test" | "production";
  readonly apiUrl: string;
  readonly realtimeUrl: string;
  readonly marketingUrl: string;
  readonly mockApi: boolean;
  readonly releaseVersion: string;
}
```

#### Production Rules

1. In `production`, missing `apiUrl` or `realtimeUrl` **must fail at build/startup immediately** (fail-fast).
2. Fallback to `localhost` is strictly forbidden in `production`.
3. Standardized variable names across all packages:
   - `VITE_API_URL`
   - `VITE_WS_URL`
   - `VITE_MARKETING_URL`
   - `VITE_MOCK_API`
   - `VITE_RELEASE_VERSION`

---

### 3.3 API Client & Contract Strategy

#### DTO & Contract Flow

```
Backend OpenAPI Spec -> Codegen -> @notrelix/contracts -> Feature Adapters -> View Model
```

#### Error Envelope Contract

All API errors returned to application code must normalize into `AppError`:

```ts
export interface AppErrorPayload {
  readonly kind:
    | "network"
    | "auth"
    | "validation"
    | "server"
    | "forbidden"
    | "not_found"
    | "conflict";
  readonly status: number;
  readonly message: string;
  readonly correlationId: string;
  readonly details?: unknown;
  readonly validationErrors?: Record<string, string[]>;
}
```

#### Key Capabilities

- Single-flight 401 token refresh queue.
- CSRF token header auto-attachment for unsafe HTTP methods (`POST`, `PUT`, `PATCH`, `DELETE`).
- `X-Correlation-ID` header injected on every request.

---

### 3.4 Auth, Session & Workspace Isolation Specification

#### Auth Lifecycle States

```
Unauthenticated -> Authenticating -> Authenticated -> Refreshing -> Expired / SigningOut
```

#### Workspace Context Isolation Rules

When a user switches workspaces:

1. Active TanStack Query cache entries scoped to the previous `workspaceId` must be invalidated or evicted.
2. Active Realtime subscriptions for the previous `workspaceId` channels must be cleanly unsubscribed.
3. Subscriptions for the new `workspaceId` channels must be established with the current active token.
4. User permissions/abilities for the new workspace must be re-fetched and updated in context.

---

### 3.5 State Ownership Matrix

| State Type                           | Owned By                          | Implementation Tool                                      |
| :----------------------------------- | :-------------------------------- | :------------------------------------------------------- |
| **Component UI State**               | Local Component                   | `useState` / `useReducer`                                |
| **Form State**                       | Form Component                    | React Hook Form / Local Reducer                          |
| **Server State (CRUD)**              | Domain Feature Package            | TanStack Query (`useQuery` / `useMutation`)              |
| **Runtime & Session**                | Platform Foundation               | `AppRuntimeContext` / `AuthContext` / `WorkspaceContext` |
| **URL State (Filter/Sort/View)**     | Router                            | TanStack Router search params schema                     |
| **Work Management Normalized State** | `@notrelix/work-management-state` | Dedicated State Store / Engine                           |
| **Realtime Transport State**         | `@notrelix/realtime`              | Connection State Machine                                 |

---

### 3.6 Realtime Transport Specification

#### Connection State Machine

```
DISCONNECTED -> CONNECTING -> CONNECTED -> RECONNECTING -> DISCONNECTED
```

#### Technical Guarantees

- **Manual Close Protection**: Invoking `realtime.disconnect()` sets `isManualClose = true`, permanently preventing reconnection loops upon user sign-out or component unmount.
- **Backoff & Jitter**: Reconnection backoff uses exponential curve plus randomized jitter:  
  `delay = min(maxDelay, baseDelay * 2^attempt) + randomJitter`.
- **Heartbeat Ping/Pong**: Periodic ping sent every 30 seconds; missing 2 consecutive pong frames triggers connection drop and reconnect.
- **Message Deduplication**: Events tracked by unique `eventId` with LRU deduplication window.

---

### 3.7 Observability & Production Redaction Specification

1. **Telemetry Envelope**:
   - `releaseVersion`
   - `environment`
   - `workspaceId`
   - `correlationId`
   - `route`
   - `errorCode`
2. **Production Redaction**:
   - `GlobalErrorBoundary` must redact raw JavaScript stack traces and unhandled `error.message` strings in production builds (`import.meta.env.PROD`), rendering a user-friendly fallback while sending full error details to telemetry.

---

## 4. Definition of Done (DoD) for Platform Freeze

A Platform Freeze release candidate (`frontend-platform-v1.0`) is valid **only** when all of the following conditions are true:

- [ ] **Migration Tracker**: `MIGRATION_TRACKER.md` total remaining legacy files equals `0`.
- [ ] **Legacy Code Eviction**: `configureApi`, `global api`, and `activeBaseUrl` completely deleted from codebase.
- [ ] **Environment Unity**: Single `RuntimeEnvironmentInput` parsed at host composition root.
- [ ] **Contract Compliance**: All OpenAPI generated contracts checked without drift.
- [ ] **Auth & Workspace Isolation**: Clean cache eviction & socket resubscription verified on workspace switch.
- [ ] **Realtime Transport**: Manual disconnect, token refresh reconnect, and ping/pong verified under tests.
- [ ] **CI Quality Gates**: `pnpm validate` (typecheck, lint, vitest unit tests, check:deps) passes cleanly with 0 errors across all 38 workspace packages.
- [ ] **Production Build & E2E**: `apps/web` and `apps/marketing` build cleanly in production mode with Playwright E2E smoke tests passing.
