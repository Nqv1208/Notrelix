# Notrelix Frontend — Enterprise System Blueprint

This document represents the absolute **Source of Truth (SSOT)** and target-state architecture blueprint for the Notrelix Enterprise Work Management Platform frontend. All developers, tech leads, and autonomous AI agents must follow these rules without exception.

---

## 1. Architectural Philosophy: Target-State First

Notrelix is designed as an enterprise-grade platform. We **reject** temporary shortcut philosophies such as:
*   *❌ "Let's keep it flat first, then modularize later."*
*   *❌ "Let's use safe alias paths as a long-term implementation."*
*   *❌ "Wait for the system to grow before refactoring."*
*   *❌ "Folderize only for cosmetic purposes."*
*   *❌ "Copy the backend Clean Architecture 1:1."*

### The Correct Mindset:
1.  **Target-State Architecture Immediately**: All code, whether new or refactored, must align with the target FSD (Feature-Sliced Design) modular structure.
2.  **Bounded-Context-Aligned & Frontend-Oriented**: We align with backend bounded contexts, but optimize folder groupings for frontend user experiences (e.g., combining boards, lists, and fields under `work-management`).
3.  **Product Capability Ownership**: Each feature slice owns its entire domain: from HTTP calls, hooks, schemas, and models to business UI components.
4.  **Route Layer is Pure Composition**: Page routes do not contain business logic, raw fetches, or state management. They only compose feature-level components.
5.  **Strict Architecture Enforcement**: Violations of import boundaries or naming conventions must be detected at compile-time and block PR merges.

---

## 2. Core Architecture Contract

The codebase is strictly segregated into layers. The following table defines the responsibility and boundaries of each directory:

| Layer | Path | Responsibility | Permitted Imports | Forbidden Imports |
| :--- | :--- | :--- | :--- | :--- |
| **App Routing** | `app/` | Routing, layouts, page composition, and server-side route guards. | `features/*` (via public API), `components/*`, `lib/*` | Internals of features (e.g., `features/auth/hooks/...`) |
| **Route-Private UI** | `app/**/_components/` | Visual-only layouts, composition structures unique to a specific route. | `features/*` (via public API), `components/*`, `lib/*` | Sibling route-private components, feature internals. |
| **Feature Slices** | `features/` | Product capabilities containing business UI, feature APIs, query/mutation hooks, domain models, caches, and schemas. | Sibling features (via **public API only**), `components/*`, `lib/*` | Sibling feature internals, `app/*` layer. |
| **UI Primitives** | `components/ui/` | Pure design system primitives (buttons, inputs, dialogs). Business-blind. | None (completely self-contained) | `features/*`, `app/*`, `lib/*` (except pure utils) |
| **Generic UI Blocks** | `components/<type>/` | Generic reusable UI blocks (`layout`, `feedback`, `data-display`, `forms`). Business-blind. | `components/ui/*`, `lib/utils` | `features/*`, `app/*` |
| **Infrastructure** | `lib/` | Cross-cutting technical infrastructure (API client, query client, websocket stream, auth helpers). | None (infrastructure is self-contained) | `features/*`, `app/*`, `components/*` |
| **Design System** | `styles/` | Global style sheets and Tailwind/CSS design token variables. | None | Any JS/TS modules |

---

## 3. Final Feature Map

To prevent fragmentation, all product capabilities are mapped to one of the following **13 frontend features**. The legacy `features/boards` folder is strictly a compatibility layer and will be removed once physical migration is completed.

```txt
features/
  ├── auth/               # Session management, authentication, login/register forms
  ├── account/            # User profile, account security, personal preferences
  ├── workspace/          # Workspace switcher, members management, invites, workspace settings
  ├── work-management/    # Boards, items, fields, groups, views, checklists, labels (Core capability)
  ├── docs/               # Notion-like document editor, page trees, block management
  ├── collaboration/      # Comments, mentions, reactions, presence, attachments
  ├── notifications/      # Notification bell, unread count, activity stream
  ├── search/             # Global quick-search, command palette, quick actions
  ├── billing/            # Plans, subscriptions, payment integration, entitlement UI
  ├── governance/         # Resource permission evaluation, audit trails
  ├── automation/         # Visual rule builder, trigger surfaces, execution logs
  ├── integrations/       # Webhooks, connection builders, integration catalog
  └── activity/           # Live workspace activity feed
```

---

## 4. WorkManagement Target Architecture

The `work-management` module is the core of Notrelix's productivity capabilities. It is structured as a nested modular context to support diverse view renderers (Table, Kanban, Calendar, Timeline) over a unified data model.

### Strict Directory Tree:
```txt
features/work-management/
  ├── boards/
  │     ├── api/                  # Board HTTP client operations (create, delete, list)
  │     ├── model/                # Board mappers and selectors
  │     ├── hooks/                # Query hooks: useFullBoard, useWorkspaceBoards
  │     ├── components/
  │     │     └── views/          # Renderers - Table, Kanban, Calendar, Timeline
  │     │           ├── table/    # Table presentation rendering
  │     │           ├── kanban/   # Kanban board view column rendering
  │     │           ├── calendar/ # Calendar schedule rendering
  │     │           └── timeline/ # Gantt/Timeline rendering
  │     ├── schemas/              # Board form validation schemas
  │     └── types/                # Board DTO and frontend interfaces
  │
  ├── items/                      # Individual work rows/cards
  │     ├── api/                  # Item-level operations (create, delete, duplicate)
  │     ├── model/                # Item-level DTO mappers
  │     ├── hooks/                # useCardDetail, useMoveCard, useUpdateCard
  │     ├── components/           # TaskDetailPanel, TaskRow, KanbanCard
  │     ├── schemas/              # Zod schemas for card title/description updates
  │     └── types/                # Item types and metadata
  │
  ├── fields/                     # Dynamic schema columns
  │     ├── api/                  # Field definition HTTP operations
  │     ├── model/                # Field type guards
  │     ├── hooks/                # useCreateColumn, useResizeColumn
  │     ├── components/
  │     │     ├── renderers/      # Cell renderers (StatusCell, ProgressCell, PersonCell)
  │     │     └── editors/        # Inline editors (SelectDropdown, DatePicker)
  │     ├── schemas/              # Field option validation schemas
  │     └── types/                # Field types (FieldDefinition, FieldType)
  │
  ├── groups/                     # Sections/Groups within a board
  │     ├── api/                  # Group HTTP client (create, reorder, delete)
  │     ├── model/                # Group selectors
  │     ├── hooks/                # useCreateGroup, useReorderGroups
  │     ├── components/           # GroupHeader, GroupSection
  │     ├── schemas/              # Group Zod schemas
  │     └── types/                # BoardGroup types
  │
  ├── checklists/                 # Checklist subtasks
  │     ├── api/                  # Checklist HTTP service
  │     ├── hooks/                # useCardChecklists mutation hooks
  │     ├── components/           # ChecklistContainer, ChecklistItemRow
  │     ├── schemas/              # Checklist schemas
  │     └── types/                # Checklist interfaces
  │
  ├── labels/                     # Tagging categorization
  │     ├── api/                  # Label API
  │     ├── hooks/                # useCardLabels hooks
  │     ├── components/           # LabelBadge, LabelSelector
  │     ├── schemas/              # Label schemas
  │     └── types/                # Label types
  │
  ├── cache/                      # Unified caching and optimistic state management
  │     ├── board-cache-updaters.ts  # TanStack Query local cache manipulators
  │     ├── optimistic-item.ts       # Optimistic updates for card moves and cell edits
  │     └── board-invalidation.ts    # Cache invalidation policies
  │
  ├── shared/                     # Utilities shared strictly within work-management
  │     ├── components/           # ViewToolbar, FilterMenu, SearchInput
  │     ├── hooks/                # useBoardView, useSelectedCardPanel
  │     ├── types/                # Shared view configurations (ViewConfig, SortConfig)
  │     └── utils/                # Position generator (fractional-index.ts)
  │
  └── index.ts                    # Explicit Public API for the entire work-management module
```

> [!IMPORTANT]
> **View Renderer Placement Rule**: Table, Kanban, Calendar, and Timeline are **not** top-level features. They are board presentation renderers and must reside under `features/work-management/boards/components/views/`. Do not create a top-level `features/work-management/views/` folder.

---

## 5. Other Feature Architectures

Every frontend feature must be structured according to FSD standards. Below is the blueprint for the other 12 features:

### 1. `auth`
*   **Responsibility**: Session lifecycle, login/signup, password reset flow, and token storage orchestration.
*   **Owned Screens/Components**: `LoginForm`, `RegisterForm`, `ForgotPasswordForm`, `ResetPasswordForm`.
*   **API/Query/Model/Cache**: `auth.service.ts`, `useAuthUser`, `useLogin`, `useLogout`. Clears all TanStack query caches upon logout.
*   **Public API Policy**: Exports authentication forms, session state hooks (`useAuthUser`), and logout mutation.
*   **Forbidden Imports**: Must not import from other business features (except `workspace` or `account` in public-facing setup flows).

### 2. `account`
*   **Responsibility**: User profile management, appearance settings (dark mode, layout density), and security credentials.
*   **Owned Screens/Components**: `ProfileForm`, `SecurityForm`, `AppearanceSelector`.
*   **API/Query/Model/Cache**: `account.service.ts`, `useUpdateProfile`, `useUpdatePassword`.
*   **Public API Policy**: Exports profile updating hooks and profile forms.
*   **Forbidden Imports**: Must not import from `work-management` or `docs`.

### 3. `workspace`
*   **Responsibility**: Workspace lifecycle, team membership, roles, and invitation workflows.
*   **Owned Screens/Components**: `WorkspaceSwitcher`, `WorkspaceManagementPanel`, `PendingInvitationsMenu`, `InviteMemberDialog`.
*   **API/Query/Model/Cache**: `workspace.api.ts`, `useWorkspaceSnapshot`, `useWorkspaceList`, `usePendingInvitations`.
*   **Public API Policy**: Exports workspace switcher, settings panel, and invitation acceptance hooks.
*   **Forbidden Imports**: Must not deep-import `auth` (must use `@/features/auth` public API).

### 4. `docs`
*   **Responsibility**: Notion-like collaborative document editing, page trees, and document hierarchy.
*   **Owned Screens/Components**: `DocumentEditor`, `PageTreeSidebar`, `BlockRenderer`, `TemplateSelector`.
*   **API/Query/Model/Cache**: `docs.api.ts`, `usePage`, `useUpdatePageTitle`, `useMovePage`.
*   **Public API Policy**: Exports page editor screens and navigation tree panels.
*   **Forbidden Imports**: Must not import directly from `work-management` (must use cross-linking helpers in `lib/`).

### 5. `collaboration`
*   **Responsibility**: Comments, user mentions, emoji reactions, presence indicators, and file attachments.
*   **Owned Screens/Components**: `CommentThread`, `MentionList`, `ReactionSelector`, `PresenceAvatarStack`.
*   **API/Query/Model/Cache**: `collaboration.api.ts`, `useComments`, `usePresenceStream`.
*   **Public API Policy**: Exports reusable comment threads and presence stacks.
*   **Forbidden Imports**: Must not import visual components from `work-management` or `docs`.

### 6. `notifications`
*   **Responsibility**: In-app inbox notifications, unread count badge, and visual notification streams.
*   **Owned Screens/Components**: `NotificationBell`, `NotificationList`, `UnreadBadge`.
*   **API/Query/Model/Cache**: `notifications.service.ts`, `useNotifications`, `useMarkRead`.
*   **Public API Policy**: Exports notification bell and badge components.
*   **Forbidden Imports**: Must not import from `governance` or `billing`.

### 7. `search`
*   **Responsibility**: Global full-text search, command palette, and quick-access palette.
*   **Owned Screens/Components**: `GlobalSearchDialog`, `CommandPalette`.
*   **API/Query/Model/Cache**: `search.api.ts`, `useGlobalSearch`.
*   **Public API Policy**: Exports search dialog and command palette.
*   **Forbidden Imports**: Must not import write-mutations from other features.

### 8. `billing`
*   **Responsibility**: Plan comparison matrices, payment forms, invoice history, and pricing cards.
*   **Owned Screens/Components**: `PricingMatrix`, `SubscriptionDetailsCard`, `PaymentMethodsList`.
*   **API/Query/Model/Cache**: `billing.api.ts`, `useSubscriptionDetails`, `usePaymentIntent`.
*   **Public API Policy**: Exports entitlement locks, billing details, and pricing cards.
*   **Forbidden Imports**: Must not import from `automation` or `integrations`.

### 9. `governance`
*   **Responsibility**: Audit logs, security policy editors, and workspace-level permission mapping.
*   **Owned Screens/Components**: `AuditLogTable`, `PermissionSettingsForm`, `GovernanceDashboard`.
*   **API/Query/Model/Cache**: `governance.api.ts`, `useAuditLogs`, `usePermissionsSchema`.
*   **Public API Policy**: Exports permission settings forms and audit logs.
*   **Forbidden Imports**: Must not import domain logic from `docs` or `work-management`.

### 10. `automation`
*   **Responsibility**: Rule builder, triggers, action composers, and execution history.
*   **Owned Screens/Components**: `RuleBuilder`, `TriggerSelector`, `ActionComposer`, `ExecutionHistoryList`.
*   **API/Query/Model/Cache**: `automation.api.ts`, `useAutomationRules`, `useExecutionLogs`.
*   **Public API Policy**: Exports automation settings and rule builders.
*   **Forbidden Imports**: Must not import internal components from `work-management`.

### 11. `integrations`
*   **Responsibility**: Webhook creators, third-party connection setups (Slack, GitHub, Calendar), and integration listings.
*   **Owned Screens/Components**: `WebhookManager`, `IntegrationsCatalog`, `ConnectionStatusCard`.
*   **API/Query/Model/Cache**: `integrations.api.ts`, `useWebhooks`, `useConnections`.
*   **Public API Policy**: Exports webhook managers and connection setups.
*   **Forbidden Imports**: Must not import database/query client configurations directly.

### 12. `activity`
*   **Responsibility**: Live feed of workspace activities and events.
*   **Owned Screens/Components**: `ActivityFeed`, `ActivityFeedItem`.
*   **API/Query/Model/Cache**: `activity.api.ts`, `useWorkspaceActivity`.
*   **Public API Policy**: Exports activity feeds.
*   **Forbidden Imports**: Must not import edit mutations from other features.

---

## 6. Route Architecture Matrix

Next.js App Router routes act as pure **composers**. They must map explicitly to the feature slices that own the capabilities.

| Route Path | Associated Feature | Page Component Composition |
| :--- | :--- | :--- |
| `/` | `marketing` (Shared) | Landing screen, features list, testimonial sliders |
| `/sign-in` | `auth` | `LoginForm` wrapped in `AuthShell` |
| `/sign-up` | `auth` | `RegisterForm` wrapped in `AuthShell` |
| `/forgot-password` | `auth` | `ForgotPasswordForm` wrapped in `AuthShell` |
| `/dashboard` | `dashboard` (Shared) | Composition of `WorkspaceList`, `NotificationBell`, and `RecentSection` |
| `/[workspaceId]` | `workspace` | `WorkspaceHomeScreen` composing recent boards + recent docs |
| `/[workspaceId]/boards/[boardId]` | `work-management` | `WorkspaceBoardShell` composing `WorkspaceViewTabs` and active view |
| `/[workspaceId]/docs/[pageId]` | `docs` | `DocsWorkspaceChrome` rendering `DocumentEditor` |
| `/[workspaceId]/settings/members` | `workspace` | `WorkspaceManagementPanel` with tab set to "members" |
| `/[workspaceId]/settings/billing` | `billing` | `WorkspaceManagementPanel` with tab set to "billing" |
| `/[workspaceId]/settings/permissions` | `governance` | `WorkspaceManagementPanel` with tab set to "permissions" |
| `/account/profile` | `account` | `AccountLayout` rendering `ProfileForm` |
| `/invite/[token]` | `auth` / `workspace` | `InviteClientPage` displaying workspace details and accept button |

> [!IMPORTANT]
> **View Routing Principle**: Board view is a presentation state. Separate routing paths (like `/[workspaceId]/boards/[boardId]/table`) are **strictly forbidden**. All board view switches (Table, Kanban, Calendar, Timeline) must resolve through the search query parameter `?view=...` on the base board route.

---

## 7. Data, Query & Permission Contracts

To enforce consistency across all features, the following software engineering contracts are established:

### 7.1. Naming Conventions
*   **DTOs (Data Transfer Objects)**: Suffix with `DtoApi` (e.g., `BoardDtoApi`, `CardSummaryDtoApi`). Represents the raw JSON payload from the backend.
*   **ViewModels (Frontend Models)**: Clean camelCase interfaces without the DTO suffix (e.g., `Board`, `Card`, `WorkspaceMember`). Represents the normalized data consumed by React.
*   **FormValues**: Suffix with `Request` or `FormData` (e.g., `LoginRequest`, `ForgotPasswordRequest`, `CreateWorkspaceInput`). Matches the payload sent to the API.

### 7.2. Mapper Rules
*   All backend API payloads **must** pass through a mapper function (e.g., `mapBoardDto`) in the `model/` folder of the feature before being stored in the TanStack Query cache.
*   Direct consumption of raw DTOs in React components is **forbidden**.

### 7.3. Query Key Taxonomy
All query keys must be declared in `lib/query/query-keys.ts` using a structured key factory:
```ts
export const queryKeys = {
  auth: {
    profile: ["auth", "profile"] as const,
  },
  workspaces: {
    all: ["workspaces"] as const,
    detail: (id: string) => ["workspaces", id] as const,
  },
  boards: {
    detail: (id: string) => ["boards", id] as const,
  },
  // Hardcoded inline array keys in useQuery are forbidden.
}
```

### 7.4. Mutation Invalidation & Optimistic Updates
*   **Cache Invalidation**: Mutations must declare explicit cache invalidations on success rather than relying on global refetches:
    ```ts
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.boards.detail(boardId) })
    }
    ```
*   **Optimistic Updates**: High-frequency user interactions (e.g., moving a card in Kanban, editing a table cell) must implement optimistic updates by writing directly to the cache using `queryClient.setQueryData`, with full rollback handlers in `onError`.

### 7.5. Permission Evaluation & Entitlement Guards
*   All visual action triggers (buttons, menus) must be wrapped using the centralized `useCan` helper.
*   Features must not evaluate raw role strings locally.
*   Route-level authorization is handled in Next.js Middleware or Server Component wrappers.

### 7.6. Error, Loading & Access-Denied States
*   Each feature must export a loading skeleton (e.g., `BoardSkeleton`) and an error boundary state.
*   Forbidden actions must render a standardized `AccessDeniedState` from `components/feedback/`.

### 7.7. Realtime Event Handling
*   Realtime event listeners (WebSockets/SSE) must be declared in `useEffect` hooks and dispatch actions directly into the TanStack Query cache via `queryClient.setQueryData` to keep UI in sync across clients.

### 7.8. Performance Rules
*   Large list structures (e.g., Table view rows, page trees) **must** use virtual windowing (e.g., React Virtualized or simple CSS containment) if items exceed 200.
*   Prevent unnecessary re-renders in Table cells by memoizing column definition arrays and utilizing fine-grained Zustand selector states.

---

## 8. Testing & Observability Architecture

### 8.1. Testing Architecture
We employ a three-tier testing model:
1.  **Domain Tests (Unit)**: Testing mappers, utility functions (e.g., fractional indexing), and state hooks in isolation using `bun test`.
2.  **API Contract Tests**: Automated static analysis to verify that features do not import legacy modules and that API configurations remain strictly versioned.
3.  **End-to-End Tests (E2E)**: High-criticality flows (login, workspace creation, card movement) tested via Playwright.

### 8.2. Observability & Telemetry Rules
*   All fetch errors must be automatically logged by the Axios interceptor in `lib/api/api-client.ts` to Sentry/OpenTelemetry.
*   Crucial user actions (e.g., view changes, document export) must trigger a telemetry event using a centralized `trackEvent` helper in `lib/telemetry`.

---

## 9. Architecture Enforcement Rules (Quality Gate)

To prevent architectural drift, the following automated gates are set up:
1.  **ESLint Boundaries**: Using `eslint-plugin-import` to block cross-feature deep imports.
2.  **TypeScript Compilation**: `tsc --noEmit` must be run in pre-commit hooks.
3.  **CI Pipeline Gate**: Any PR that references forbidden imports, has circular dependencies, or skips DTO mappers will fail the build and be blocked from merging.

---

## 10. Enterprise Product Readiness & Policy

### 10.1. Production Readiness Definition
To be classified as **Production-Ready**, a feature slice must satisfy all the following rules:
*   **Backend Integrated**: Uses real Axios API clients to query PostgreSQL/Redis. No mock stubs in production critical paths.
*   **Permission-Aware**: UI access is governed strictly by capability checks via `useCan()` or `hasPermission()`. Zero raw role string checks.
*   **Entitlement-Aware**: High-tier features verify subscription boundaries prior to rendering actions.
*   **Error/Loading Resilient**: Implements standard loading skeletons, error fallbacks, and empty visual cards.
*   **Centralized Query Cache**: Leverages centralized `queryKeys` factories with strict invalidations scoped per mutation.
*   **Zero Quality Debt**: Compiles without warnings, has passing Vitest unit tests, and complies with architecture gates.

### 10.2. Feature Readiness Matrix

| Feature | Architecture Status | Backend Integration | UI Completeness | Test Coverage | Production Readiness | Notes |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **auth** | Aligned (Public API) | Real API (JWT/Cookie) | Completed | High | **Production-Ready** | Handles refresh token mutex. |
| **account** | Aligned (Public API) | Real API | Completed | Medium | **Production-Ready** | User preferences and settings. |
| **workspace** | Aligned (Public API) | Real API | Completed | High | **Production-Ready** | Members, switchers, and invites. |
| **work-management** | Aligned (Public API) | Real API | Completed | High | **Production-Ready** | Table, Kanban, Calendar, Timeline views. |
| **notifications** | Aligned (Public API) | Real API | Completed | Medium | **Integration-Ready** | Actionable activity updates feed. |
| **activity** | Aligned (Public API) | Real API | Completed | Medium | **Integration-Ready** | Audit logs feed integrated. |
| **docs** | Partial Barrel | Real API | Completed | Medium | **Quality-Debt** | Contains legacy deep imports in app layer. |
| **collaboration** | Aligned (Public API) | Stubbed | Completed | Low | **Architecture-Ready** | Mentions/comments structure defined. |
| **billing** | Aligned (Public API) | Stubbed | Completed | Low | **Contract-Ready** | Plan stubs defined in billingApi contract. |
| **search** | Aligned (Public API) | Stubbed | Completed | Low | **Contract-Ready** | Simulated search queries. |
| **governance** | Aligned (Public API) | None (UI-only) | Static Tabs | None | **Mock-Only** | Static tab in Workspace Settings. |
| **automation** | Aligned (Public API) | None (UI-only) | Static Tabs | None | **Mock-Only** | Switch-only rules stubs. |
| **integrations** | Aligned (Public API) | None (UI-only) | Static Tabs | None | **Mock-Only** | Static connections panel stubs. |

### 10.3. Composition Boundary Policy
*   **Settings Presentation Composition**: `WorkspaceManagementPanel` acts as a composite page rendering tabs from governance, automation, integrations, and activity. It must import them as isolated black-box UI components through their public barrels.
*   **Hook Decoupling**: Sibling features must not import each other's internal query hooks. For example, `useWorkspaceSnapshot` cannot import `useWorkspaceActivity` directly. The composition of multiple sibling states must happen at the `app/` page layer.

### 10.4. Mock/Stub Policy
*   **Allowed Contract Stubs**: Permitted when a backend endpoint does not exist yet. Must reside in a `mock/` subdirectory, match types defined in `types/`, and be excluded from production build paths.
*   **Forbidden Mocking**: Call mock hooks on critical paths that have active endpoints. Fake permission guards or fake feature entitlements without fallback guards.

### 10.5. Permission/Entitlement Policy
*   **No Raw Checks**: Do not write `member.role === 'admin'` or `subscription.plan === 'free'` in UI components.
*   **Centralized Guards**: Call `const { can } = useCan()` for actions, and `const { hasFeature } = useEntitlements()` for pricing limits.

### 10.6. Route Ownership Matrix

| Route | Owner Feature | Composition Dependencies |
| :--- | :--- | :--- |
| `/` | `marketing/app` | `auth` |
| `/sign-in` | `auth` | None |
| `/sign-up` | `auth` | None |
| `/home` | `dashboard` (composition) | `workspace`, `activity`, `work-management` |
| `/[workspaceId]` | `workspace` (composition) | `activity`, `work-management`, `docs` |
| `/[workspaceId]/boards/[boardId]` | `work-management` | `workspace` (via route-tabbed layout) |
| `/[workspaceId]/docs/[pageId]` | `docs` | `workspace` (via route-tabbed layout) |
| `/account/profile` | `account` | None |

### 10.7. Quality Debt Register
1.  **Tabbed Shell Coupling**: `work-management` views rely on `WorkspaceTabbedRouteFrame` layout. Will be resolved by moving the tabbed shell structure into `features/workspace` in the next phase.
2.  **Docs Deep Imports**: `app/` imports internal hooks and types from `features/docs/hooks/...` and `features/docs/types/...`. Resolving this requires implementing a complete public barrel for `docs` feature.
3.  **Color Theme Hydration Hack**: `useColorTheme` defers local storage state sync via `setTimeout` to bypass linter. Can be refactored via `next-themes` standard context.

