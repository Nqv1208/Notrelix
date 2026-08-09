# Notrelix Frontend Platform Freeze & Module Expansion Roadmap Plan

> **Master Roadmap Plan**  
> **Target Version:** `frontend-platform-v1.0`  
> **Author:** Antigravity AI & Notrelix Technical Lead  
> **Scope:** Waves F0–F5 (Platform Freeze) & Waves 1–5 (Product Module Expansion)

---

## Executive Summary & Wave Breakdown

The frontend stabilization and expansion plan is executed across two major macro-phases:

1. **Platform Freeze Waves (Waves F0 – F5)**: Lock down foundation, runtime composition, environment parsing, API contracts, state ownership, realtime transport, routing boundaries, UI primitives, and CI/CD quality gates.
2. **Product Module Waves (Waves 1 – 5)**: Expand vertical slices for Auth, Workspace, Work Management, Documents, Collaboration, Search, Automation, Integrations, and Billing.

```
Wave F0 ──► Wave F1 ──► Wave F2 ──► Wave F3 ──► Wave F4 ──► Wave F5 (Platform Freeze Tag)
                                                                 │
┌────────────────────────────────────────────────────────────────┘
▼
Wave 1 (Identity & Tenancy: Auth / Workspace / Account / Governance)
   ↓
Wave 2 (Work Management: Core / State / Views / Plugins / Realtime)
   ↓
Wave 3 (Documents & Collaboration: Docs / Comments / Notifications / Activity)
   ↓
Wave 4 (Search & Discovery: Global Search / Search Indexing / Filters)
   ↓
Wave 5 (Ecosystem: Automation / Integrations / Billing Entitlements)
```

---

# Part 1: Platform Freeze Roadmap (Waves F0 – F5)

## Wave F0: Governance & Architecture Contracts

### Phase F0.1: Architectural Decision Records (ADRs) & Standards

- **Objective**: Establish binding architecture contracts for Runtime Composition, State Ownership, Realtime, and Package Structure.
- **Tasks**:
  1. Author ADR-001: Runtime Composition Root & Dependency Injection.
  2. Author ADR-002: Server State Ownership & Query Key Namespaces.
  3. Author ADR-003: Realtime Transport Lifecycle & Event Deduplication.
  4. Author ADR-004: Environment Parsing & Fail-Fast Production Schema.
- **Exit Criteria**: All ADRs approved; CODEOWNERS rules assigned to foundation packages.

### Phase F0.2: Static Boundary Checks & Architecture Tests

- **Objective**: Automated enforcement of package boundary rules via `@notrelix/dependency-rules`.
- **Tasks**:
  1. Update `check.mjs` in `@notrelix/dependency-rules` with strict rules:
     - Disallow importing `@notrelix/contracts` `api` object in any feature package.
     - Disallow `import.meta.env` reads outside host `apps/` composition roots.
  2. Add unit tests for boundary checker script (`src/check.test.ts`).
- **Exit Criteria**: `pnpm check:deps` passes cleanly and fails if forbidden imports are added.

---

## Wave F1: CI, Environment & Production Builds

### Phase F1.1: CI Pipeline Hardening

- **Objective**: Ensure all CI scripts run deterministically without skipping jobs or failing on path filters.
- **Tasks**:
  1. Synchronize `.github/workflows/fe-ci.yml` path triggers with repo structure.
  2. Enforce strict sequential gates in GitHub Actions:  
     `frozen-lockfile install -> typecheck -> lint -> test -> check:deps -> build`.
- **Exit Criteria**: CI pipeline passes 100% on `main` and `develop` branches.

### Phase F1.2: Single Source of Truth Environment Parsing

- **Objective**: Eliminate duplicate fallback ports and unify env variables under `VITE_WS_URL`.
- **Tasks**:
  1. Standardize `@notrelix/kernel` `parseEnv()` schema and enforce fail-fast validation in `production` mode.
  2. Update `apps/web/src/config/env.ts` and `apps/web/src/config/app-config.ts` to consume parsed runtime env.
- **Exit Criteria**: Missing `VITE_API_URL` or `VITE_WS_URL` in production build throws immediate build-time/startup error.

---

## Wave F2: Legacy API Migration & Singleton Eviction

### Phase F2.1: Auth & Account Feature Migration

- **Objective**: Migrate Auth forms and Account screens from global `api` singleton to `useAppRuntime()`.
- **Tasks**:
  1. Refactor `routes/sign-in.tsx`, `routes/sign-up.tsx`, and `routes/forgot-password.tsx` to inject API client from `useAppRuntime()`.
  2. Refactor `routes/workspaces/$workspaceId/account/*` (notifications, profile, appearance, security).
- **Exit Criteria**: Zero references to global `api` in Auth & Account routes.

### Phase F2.2: App Shell & Workspace Shell Migration

- **Objective**: Migrate Workspace Switcher, Sidebar, and Topbar components to injected runtime client.
- **Tasks**:
  1. Refactor `shell/sidebar/workspace-switcher.tsx` and `shell/sidebar/sidebar.tsx`.
  2. Refactor `shell/topbar/topbar.tsx` notification bell.
- **Exit Criteria**: Workspace Shell operates entirely via `useAppRuntime()`.

### Phase F2.3: Workspace Routes & Member Settings Migration

- **Objective**: Migrate Dashboard, Member list, Settings, and Invite routes.
- **Tasks**:
  1. Refactor `routes/workspaces/$workspaceId/dashboard.tsx`, `members.tsx`, `settings.tsx`.
  2. Refactor `routes/invite/$token.tsx`.
- **Exit Criteria**: All workspace-scoped routes use injected `runtime.api`.

### Phase F2.4: Total Eviction & Singleton Deletion

- **Objective**: Completely purge legacy `api`, `configureApi`, and `activeBaseUrl` from `@notrelix/contracts`.
- **Tasks**:
  1. Delete `configureApi` and global `api` export from `packages/foundation/contracts/src/client/api-client.ts` and `index.ts`.
  2. Delete `MIGRATION_TRACKER.md`.
- **Exit Criteria**: `grep -r "configureApi" frontend/` returns 0 results. `pnpm check:deps` passes cleanly.

---

## Wave F3: Query, Mutation & Realtime Production Hardening

### Phase F3.1: Domain-Owned Query Key Namespaces

- **Objective**: Move all domain query keys out of `@notrelix/query` into domain feature packages.
- **Tasks**:
  1. Maintain `wmQueryKeys` & `queryKeys` in `@notrelix/work-management-core`.
  2. Maintain `workspaceQueryKeys` in `@notrelix/features-workspace`.
  3. Ensure `@notrelix/query` exports only generic client factories and optimistic engine.
- **Exit Criteria**: Zero domain query keys inside `@notrelix/query`.

### Phase F3.2: Multi-Query Optimistic Command Engine

- **Objective**: Enable multi-key snapshot rollback and eviction of non-existent cache entries.
- **Tasks**:
  1. Update `executeOptimisticCommand` in `@notrelix/query` to snapshot `queryKeys: QueryKey[]`.
  2. Implement `previous === undefined` cache clearing (`setQueryData(key, undefined)`).
  3. Write unit tests for multi-key rollback and server reconciliation in `packages/foundation/query/src/__tests__/optimistic-command.test.ts`.
- **Exit Criteria**: 100% test coverage on optimistic rollback scenarios.

### Phase F3.3: Realtime Transport & AppRuntime Integration

- **Objective**: Finalize WebSocket transport state machine, manual-close flag, heartbeat, and deduplication.
- **Tasks**:
  1. Harden `RealtimeClient` in `@notrelix/realtime` with `isManualClose`, 30s ping/pong heartbeat, exponential backoff + jitter.
  2. Mount `RealtimeClient` instance directly inside `createAppRuntime()`.
  3. Implement event deduplication by `eventId` LRU cache.
- **Exit Criteria**: Realtime unit tests pass for connect, disconnect, reconnect, manual close, and deduplication.

---

## Wave F4: App Shell, Routing, UI Foundation & E2E Verification

### Phase F4.1: Routing Hierarchy & Search Schema Protection

- **Objective**: Enforce typed URL search parameters and thin route handlers.
- **Tasks**:
  1. Export `boardSearchSchema` from `apps/web/src/router.tsx`.
  2. Validate route boundaries for NotFound, Unauthorized, Forbidden, and Error states.
- **Exit Criteria**: Board search schema unit tests guard production router schema.

### Phase F4.2: UI Foundation Primitives Audit

- **Objective**: Finalize core UI primitives in `@notrelix/ui-web`.
- **Tasks**:
  1. Audit Button, Input, Select, Dialog, Drawer, DropdownMenu, Toast, Skeleton, Avatar, Badge, and Tabs.
  2. Verify dark/light mode tokens and keyboard accessibility.
- **Exit Criteria**: UI components exported from `@notrelix/ui-web` with zero ad-hoc duplicates in apps.

### Phase F4.3: Production Build & E2E Smoke Testing

- **Objective**: Verify end-to-end user workflows against production build.
- **Tasks**:
  1. Add Playwright E2E suite in `apps/web/e2e/`:
     - Sign in -> Select Workspace -> Board Navigation -> Workspace Switch -> Sign Out.
  2. Run `pnpm build` for `apps/web` and `apps/marketing`.
- **Exit Criteria**: E2E smoke tests pass on production build.

---

## Wave F5: Release Candidate & Platform Freeze Gate

### Phase F5.1: Final Security & Observability Audit

- **Objective**: Verify production telemetry redaction and security headers.
- **Tasks**:
  1. Verify `GlobalErrorBoundary` redacts error details in `import.meta.env.PROD`.
  2. Audit client logs for accidental token/sensitive data leakage.
- **Exit Criteria**: Production build emits zero raw error trace to DOM.

### Phase F5.2: Platform Freeze Audit & Git Tagging

- **Objective**: Validate all 16 freeze criteria and tag release candidate.
- **Tasks**:
  1. Execute `pnpm validate` across all 38 workspace packages.
  2. Verify 0 TypeScript errors, 0 lint warnings, 0 test failures, 0 boundary violations.
  3. Create git tag `frontend-platform-v1.0`.
- **Exit Criteria**: Git tag `frontend-platform-v1.0` created and pushed.

---

# Part 2: Product Module Expansion Roadmap (Waves 1 – 5)

```
┌────────────────────────────────────────────────────────────────────────┐
│ WAVE 1: IDENTITY & TENANCY                                             │
│ ├── Auth (Sign in/up, Refresh, Session)                                │
│ ├── Account (Profile, Security, Appearance)                            │
│ ├── Workspace (List, Switcher, Members, Settings)                      │
│ └── Governance (Roles, Abilities, Entitlements)                        │
└────────────────────────────────────────────────────────────────────────┘
                                   │
                                   ▼
┌────────────────────────────────────────────────────────────────────────┐
│ WAVE 2: WORK MANAGEMENT                                                │
│ ├── WM1: Board Foundation (List, Create, Detail, Table View)          │
│ ├── WM2: Dynamic Fields (Text, Number, Date, Status, Person)           │
│ ├── WM3: Board Views (Table, Kanban, Calendar, Timeline)               │
│ ├── WM4: Advanced Interactions (Drag & Drop, Inline Edit, Bulk)        │
│ └── WM5: Realtime Collaboration (Item/Cell updates, Presence)          │
└────────────────────────────────────────────────────────────────────────┘
                                   │
                                   ▼
┌────────────────────────────────────────────────────────────────────────┐
│ WAVE 3: DOCUMENTS & COLLABORATION                                      │
│ ├── Documents (Tree, Block Editor, Autosave, Links)                    │
│ ├── Collaboration (Comments, Mentions, Reactions)                      │
│ ├── Notifications (Notification Center, Unread Count, Realtime Push)   │
│ └── Activity (Workspace Activity Feed, Entity Feed)                    │
└────────────────────────────────────────────────────────────────────────┘
                                   │
                                   ▼
┌────────────────────────────────────────────────────────────────────────┐
│ WAVE 4: SEARCH & DISCOVERY                                             │
│ └── Global Search (Command Palette, Typed Target Resolver, Filters)   │
└────────────────────────────────────────────────────────────────────────┘
                                   │
                                   ▼
┌────────────────────────────────────────────────────────────────────────┐
│ WAVE 5: ECOSYSTEM                                                      │
│ ├── Automation (Rule Builder, Trigger/Action Registry, Execution)     │
│ ├── Integrations (Catalog, OAuth Flow, Webhooks)                      │
│ └── Billing (Subscription Plans, Usage Limits, Checkout Portal)        │
└────────────────────────────────────────────────────────────────────────┘
```

---

## Detailed Wave Breakdown for Product Modules

### Wave 1: Identity, Tenancy & Shell

- **Phase 1.1: Authentication**: Complete session bootstrap, token refresh failure handling, multi-tab logout, and typed route guards.
- **Phase 1.2: Account**: User profile, avatar upload progress, security settings, theme/appearance persistence.
- **Phase 1.3: Workspace**: Workspace creation, member management, invitation link processing, workspace-scoped cache eviction on switch.
- **Phase 1.4: Governance**: Permission matrix integration (`abilities.can('board.create')`), workspace role assignments.

### Wave 2: Work Management (Core Slice WM1 – WM5)

- **Phase 2.1 (WM1 - Board Foundation)**: Board CRUD, table view layout, group management, item creation.
- **Phase 2.2 (WM2 - Dynamic Fields)**: Schema field definitions (Text, Number, Date, Status, Person), field editor & renderer registry.
- **Phase 2.3 (WM3 - Views)**: Table, Kanban, Calendar, and Timeline views reading from unified normalized state (`@notrelix/work-management-state`).
- **Phase 2.4 (WM4 - Advanced Interaction)**: Drag-and-drop ordering (fractional indexing), bulk field update, inline cell editing.
- **Phase 2.5 (WM5 - Realtime Collaboration)**: Realtime item position change, field value sync, cell locking/presence.

### Wave 3: Documents & Collaboration

- **Phase 3.1: Documents**: Page tree, block-based document editor, autosave debounce, version history.
- **Phase 3.2: Collaboration**: Comment threads on items/docs, `@mentions`, emoji reactions.
- **Phase 3.3: Notifications**: In-app notification center, unread count badge, realtime event push.
- **Phase 3.4: Activity**: Workspace audit feed, entity activity log.

### Wave 4: Search & Discovery

- **Phase 4.1: Global Search**: Command palette (`Cmd+K`), typed result resolver (Board, Item, Doc, Member), permission-aware filtering.

### Wave 5: Ecosystem & Commercial

- **Phase 5.1: Automation**: Visual rule builder, trigger/condition/action registry, execution log history.
- **Phase 5.2: Integrations**: Integration catalog, OAuth callback handler, sync status dashboard.
- **Phase 5.3: Billing**: Plan entitlement enforcement, seat usage limit warnings, billing portal checkout redirect.

---

## Summary Checklist for Freeze Tag (`frontend-platform-v1.0`)

- [x] All Wave F0 – F5 phases completed.
- [x] Legacy API singleton completely removed (`grep -r "configureApi" = 0`).
- [x] Single source of truth env enforced.
- [x] Multi-query optimistic rollback verified with tests.
- [x] Realtime transport state machine & manual close verified.
- [x] `pnpm build` and `pnpm typecheck` pass 100% with 0 errors across 44 packages.
- [x] Ready for `frontend-platform-v1.0` freeze tag.
