---
document_id: WRK-PLAN-WORKSPACE-EXPERIENCE
document_type: workstream-plan
status: active
revision: v1-design-brief-confirmed
owner: frontend-team
---

# PLAN — Workspace Experience Rebuild (V1)

Rebuild of the authenticated workspace surface — shell, Home, Boards/Docs directory, Members, Settings and Board view — on one token, theme and component system. This plan executes the design brief confirmed on 2026-10-10 and is subordinate to [`DESIGN.md`](../../../../DESIGN.md), [`PRODUCT.md`](../../../../PRODUCT.md), [`frontend/AGENTS.md`](../../../../frontend/AGENTS.md) and the frontend architecture documents. It does not redefine product semantics.

## 1. Confirmed decisions

| ID | Decision |
|---|---|
| WX-DEC-001 | Scope is the whole workspace surface, including Board view. Board view is the last phase because its table files carry another session's uncommitted work. |
| WX-DEC-002 | Home serves "resume work": recently touched Boards/Docs, my assigned Items, workspace activity. No metric cards or identical card grids (DESIGN.md §4.2). |
| WX-DEC-003 | Color strategy is Restrained. Light is the default mode; Dark and High contrast are complete modes, not inversions. |
| WX-DEC-004 | Brand palette comes from the Notrelix logo. Blue `#1E90FF` (OKLCH hue ≈255) is the default accent, split into `brand-mark` (L≈0.63, marks only), `brand-solid` (L≈0.53, filled actions with white text, ≥4.5:1) and `brand-ink` (L≈0.45, links). Dark mode uses L≈0.72 for text and links. |
| WX-DEC-005 | Theme has two independent axes: mode (Light / Dark / System / High contrast) and accent (Notrelix Blue default; Coral from `#FC744C`; Ember from `#FE3154`, kept distinct from error red; Sage from `#8CADA1`, deepened for contrast; Amber from `#F9C942`, dark text on fills). They replace the seven current accent themes. |
| WX-DEC-006 | The warm logo colors are identity colors (workspace, Board, avatar, chart series) only, never status colors (DESIGN.md §9.3). |
| WX-DEC-007 | One family: Inter with tabular numerals; fixed rem scale, ratio 1.125–1.2. |
| WX-DEC-008 | The UI is internationalized from the start: Vietnamese and English. |
| WX-DEC-009 | Backend-first: missing producer endpoints are delivered before the frontend phase that needs them. |
| WX-DEC-010 | Workspace views belong to Work Management (decided 2026-10-10). The mock-only workspace view tabs leave the workspace shell; cross-Board views are delivered by the work-management-views workstream. |
| WX-DEC-011 | Pins are personal. Recents keep the 30 most recent entries per member per workspace. Both are per-member navigation state owned by the Workspaces context and store only resource references (kind + id); display names are joined on the client from the authorized Board and Doc lists, so a reference to a resource the member can no longer view is dropped instead of leaking its name. |
| WX-DEC-012 | i18n library: i18next with react-i18next (decided 2026-10-10). |

## 2. Baseline findings (to be removed, not reused as precedent — FE-UI-078)

- Two shells (`apps/web/src/shell/sidebar` and `shell/home/app-sidebar.tsx`) with two workspace switchers; `GlobalSearch` mounted twice; the shadcn `Sidebar` primitive unused.
- Navigation contributions point six of seven entries at the workspace root; "Quick access" and "Team online" are not backed by real data.
- Hex and primitive colors (`#6161ff`, `bg-blue-500/10`, a copied avatar palette in three files), inline color styles, and arbitrary type sizes (`text-[9px]` to `text-[13px]`) across shell, members and home components.
- Duplicated live/"surface" component pairs in `@notrelix/features-workspace`.
- Hand-rolled search overlay (clickable div, no focus trap, no dialog role); hand-rolled skeletons and empty text instead of `Skeleton`/`Empty`.
- Dashboard wired to empty arrays; dead header buttons; nested full-height scroll (`h-screen` inside a scrolling `main`); silent redirect on lost access; `window.confirm` for destructive member actions; settings labels not bound to inputs and description never saved.
- Theme defects: the default theme preview is violet while `--primary` is blue; `--font-sans` references itself, so Inter never applies; Tailwind `@source` omits `features/*` and `product/*`.
- Backend gaps: no recent/favorite endpoints; workspace views exist only in the mock backend; the activity endpoint exists but has no frontend adapter; create-workspace and update-profile paths differ between frontend and backend; invitation create/revoke not wired on the frontend.

## 3. Architecture constraints every phase obeys

- Apps compose; reusable workspace UI lives in `@notrelix/features-workspace/web`. Board/Doc data enters workspace surfaces through props or app-level composition; the feature package's dependency allow-list is not widened without an architecture decision.
- Public exports only; explicit factory composition (`createUseX({ api, endpoints })`); workspace-scoped query keys via `workspaceQueryKey()`; a workspace switch is a coordinated state boundary.
- UI construction pipeline per surface: pure surface (typed props and callbacks) → fixtures and scenarios → story next to the owner (desktop/light plus dark and mobile where relevant) → interaction test → ui-evidence manifest row; `pnpm validate:ui` and `check:ui-actions` must pass. Live/"surface" duplicates are collapsed into one component.
- Generated contracts are regenerated from the backend producer, never hand-edited.

## 4. Phases and work units

### Phase 0 — Backend producer gaps (backend-first)

| Unit | Change | Evidence |
|---|---|---|
| WX-BE-001 | Recent resources: a per-user, workspace-scoped "recently opened" read (Boards and Docs) recorded on open, with a bounded list query. | Domain/Application tests, PostgreSQL integration incl. cross-workspace denial, OpenAPI |
| WX-BE-002 | Pins: per-user pin/unpin of Boards and Docs within a workspace, ordered by pin time; reorder deferred. | same, plus idempotency tests |
| WX-BE-003 | Workspace views: owned by Work Management (WX-DEC-010); no Workspaces endpoint is added and the workspace shell drops the mock-only view tabs. | decision record (WX-DEC-010) |
| WX-BE-004 | Path reconciliation: create workspace under the account route; update-profile path; frontend adapters follow the producer. | API tests, regenerated client |
| WX-BE-005 | My Work read: Items assigned to the current user across the workspace's Boards, grouped by due window, bounded and paginated. | integration and query-isolation tests |

Retention and visibility are fixed by WX-DEC-011.

### Status

Delivered on 2026-10-10 (uncommitted working tree on `develop`, baseline `6224020c`).

| Unit | Status | Delivered | Evidence |
|---|---|---|---|
| WX-BE-001 | Done | `POST /api/v1/workspaces/{workspaceId}/recents` with `{ kind, resourceId }` (`Idempotency-Key` required, 204) and `GET …/recents` returning `[{ kind, resourceId, lastOpenedAt }]`, newest first. Kinds: `work-management.board`, `documents.page`. The resource is located through `IResourceLocator` and must be in the caller's Account and Workspace, otherwise 404. Upsert of `lastOpenedAt`, trim to the 30 newest per member per Workspace. Table `workspace.recent_resources`, unique (workspace, user, kind, resource). | `WorkspaceNavigationTests` (Domain, 18), `WorkspaceNavigationHandlerTests` (Application, 21, shared with WX-BE-002), `WorkspaceNavigationIntegrationTests` (PostgreSQL, 9, shared), `RlsRuntimeEnforcementTests.AppRole_NavigationState_IsVisibleOnlyToItsOwnerInsideGrantedWorkspaces` (1), OpenAPI regenerated. |
| WX-BE-002 | Done | `PUT` and `DELETE /api/v1/workspaces/{workspaceId}/pins/{kind}/{resourceId}` (204, naturally idempotent: re-pin and unpin-when-absent are no-op successes) and `GET …/pins` returning `[{ kind, resourceId, pinnedAt }]` ordered by pin time, newest first. Cap of 50 pins per member per Workspace; over the cap fails with rule code `Workspaces_Navigation_PinLimitReached`. Table `workspace.pinned_resources`. Unpin does not require the resource to still exist, so a pin to a deleted Board or Doc can be removed and stops counting against the cap. | Same classes as WX-BE-001, including two concurrent pins at the cap boundary (exactly one admitted) and concurrent first opens of one resource (one row). |
| WX-BE-005 | Done | `GET /api/v1/workspaces/{workspaceId}/my-work/items?cursor=&limit=` returns `CursorPage<MyWorkItemDto>` (`items`, `nextCursor`, `hasNextPage`); each item carries `id`, `boardId`, `groupId`, `name`, `dueAt`, `startedAt`, `updatedAt`, `version`. Default page size 50, maximum 100. Keyset order: due date ascending, undated last, ties by id. Open items only (completed, archived, deleted items and items on archived or deleted Boards are excluded). `GetMyBoardItemsQuery` had no HTTP route before, so the shape change has no deployed consumer. | `GetMyBoardItemsTests` (Application, 14), `WorkspaceNavigationIntegrationTests.MyWork_PagesByDueDate_AndReturnsOnlyTheCallersOpenItemsInTheWorkspace` (PostgreSQL keyset paging and per-user, per-Workspace isolation). |

Shared mechanics: one migration `AddWorkspaceNavigationState` (additive, two new tables); RLS on both tables combines Workspace access with row ownership (`ops.has_workspace_access(account_id, workspace_id) AND user_id = ops.current_user_id()`), and the handlers also filter by the current user. Concurrent writes of one member are serialized with a transaction-scoped advisory lock keyed by (workspace, user).

Open items:

- My Work applies Workspace-level authorization only, like `GetBoardsQuery`; it does not filter by per-Board visibility. If private Boards must hide assigned Items from a member who lost Board access, that needs a Work Management decision.
- `IResourceLocator` also locates soft-deleted Boards and Docs, so a reference to a deleted resource can be recorded. The client drops references it cannot resolve (WX-DEC-011); a server-side lifecycle filter is not part of this unit.
- WX-BE-004 (path reconciliation) is not part of this delivery.

### Phase 1 — Foundation: tokens, themes, typography, i18n

| Unit | Change | Evidence |
|---|---|---|
| WX-FND-001 | Rebuild the `@notrelix/ui-tokens` color layer in OKLCH: brand ramp from WX-DEC-004, tinted neutrals (chroma ≈0.006 toward hue 255), surface tiers (canvas, sidebar, panel, raised), the semantic state set (hover, focus, selected, disabled, success, warning, danger, info) and the identity palette from the logo. CSS variables are generated from tokens; hard-coded values in `globals.css` are deleted. | token unit tests; automated contrast matrix for every mode × accent (text ≥4.5:1, focus ring and icons ≥3:1, High contrast body ≥7:1) |
| WX-FND-002 | Theme runtime: two axes (mode, accent) with system sync and no flash on load; stored preferences migrate from the seven old themes to the nearest new accent. | theme provider tests, no-flash script test, Storybook theme matrix |
| WX-FND-003 | Typography: fix the font variable chain so Inter applies; tabular numerals for data; semantic text roles (DESIGN.md §8.1) as utilities; arbitrary pixel sizes removed. | lint rule banning arbitrary text sizes and hex/primitive colors in product code |
| WX-FND-004 | Tailwind `@source` covers every package that ships classes. | build check that a class used only in a product package is generated |
| WX-FND-005 | Elevation, radius, spacing and a semantic z-index scale (dropdown → sticky → backdrop → modal → toast → tooltip); motion tokens of 150–200 ms ease-out with reduced-motion fallbacks. | token tests |
| WX-FND-006 | i18n foundation: library chosen through an ADR (candidates: i18next/react-i18next, Lingui); locale detection and switching; message catalogs per package; Vietnamese and English; pluralization and date/number formatting; missing-key check in CI. | ADR, catalog completeness check, locale-switch test |

### Status

Delivered on 2026-10-10 (uncommitted working tree on `develop`).

| Unit | Status | Delivered | Evidence |
|---|---|---|---|
| WX-FND-001 | Done | `@notrelix/ui-tokens` color layer in OKLCH (`src/color/`): neutrals at hue 255 (chroma ≤ 0.01), four surface tiers, accent tones (mark / solid / ink / subtle / ring) per appearance, status pairs, state set, identity palette from the logo. `src/css/index.css` (the `./css` export) is generated by `pnpm --filter @notrelix/ui-tokens generate:css`; `globals.css` imports it and only maps variables into Tailwind (all literal values and the seven `.theme-*` blocks removed). shadcn variable names are kept. | `contrast-matrix.unit.test.ts`: 15 appearance × accent combinations, OKLCH → sRGB, text ≥ 4.5:1 (high contrast ≥ 7:1), ring/icons/marks ≥ 3:1, in-gamut check, Ember–danger ΔE_OK ≥ 0.08; `tokens.unit.test.ts` (generated-CSS drift, role coverage). |
| WX-FND-002 | Done | `ThemeProvider` with `mode` (light default / dark / system / high-contrast) and `accent` (blue / coral / ember / sage / amber), system sync, legacy keys and the seven old accent ids migrated; generated pre-paint `themeInitScript` (subpath `@notrelix/ui-web/theme-init-script`) inlined in `apps/web/index.html`; `ThemePicker` (copy via props) on the appearance page, user menu and topbar on the new API, translated vi/en. | `theme-model.unit.test.ts`, `theme-provider.component.test.tsx`, `theme-init-script.component.test.ts` (ui-web and an `index.html` parity test in apps/web); Storybook `UI Web/Theme › Mode Accent Matrix`. |
| WX-FND-003 | Done | Font chain fixed: Inter self-hosted (`@fontsource-variable/inter`), Poppins and Google Fonts removed; `text-<role>` utilities for the DESIGN.md §8.1 roles; `tabular-nums` guidance (`text-metadata` is tabular). `pnpm check:design-tokens` (run by `check:ui-purity`) flags hex literals, primitive palette colors and arbitrary text sizes in `apps/web/src` and `packages/features/*/src` against a ratchet baseline. | `check-design-tokens.unit.test.ts`; baseline `tooling/dependency-rules/design-token-baseline.json`: 32 files, 250 violations (hex 34, palette 162, text size 54). |
| WX-FND-004 | Done | `@source` covers `packages/ui/web`, `packages/features` and `packages/product`. | `apps/web/scripts/check-css-sources.mjs` after `pnpm --filter @notrelix/app-web build`: package-only sentinel classes present (`bg-table-bg` from Work Management, `divide-border/40` from features-workspace). |
| WX-FND-005 | Done | Radius roles, spacing/size/layout roles, per-appearance elevation (`shadow-raised/overlay/modal`), z-index roles (`z-dropdown` … `z-tooltip`), motion 150/180/200 ms with ease-out curves, collapsed under `prefers-reduced-motion`. | `tokens.unit.test.ts` (duration range, ease-out, z-order, reduced-motion block). |
| WX-FND-006 | Done | FE-ADR-006; `@notrelix/i18n` (foundation, no internal imports) with `I18nProvider`, `useT`, `useLocale`, `useFormatters`, `defineCatalog`, `Intl` helpers; detection stored → browser → `vi`; wired into `apps/web` providers; `appearance` catalog (vi/en). `pnpm check:i18n` in CI (repository-integrity) and `validate:fast`. | `packages/foundation/i18n/src/__tests__` (detection, catalog completeness, plurals, formatting, locale switch, instance isolation), `check-i18n-catalogs.unit.test.ts`. |

Open items:

- Visual baselines are linux-pinned and every color changed: regenerate the `ui-foundation` baselines in CI (`visual-baseline.lock.json`) before `test:ui:freeze` can pass.
- `test:ui:a11y` fails on `work-management-table-main-table--default` (`text-muted-foreground/70` at 3.2:1, already failing before this change at 3.2:1); the fix is in `packages/product/work-management/web` (`table-add-task-row.tsx`), outside this phase's write scope.
- Amber's light fill carries dark text, so `text-primary` used as text is illegible under Amber; ui-web link variants now use `text-link`, but other `text-primary` text usages in apps, features and product packages need migrating to `text-link` / `text-brand-ink`.
- Overlay primitives still use Tailwind `z-50`; moving them to the z-index roles belongs to the shell phase.
- `apps/marketing` keeps its own `globals.css` and does not consume the generated tokens yet.
- Legacy aliases kept for current consumers: `--app-*`, `--table-*`, `--badge-*` (Work Management), and the `font-display` utility (now Inter). Remove them when those consumers move to surface/status roles.

### Phase 2 — Shell

| Unit | Change | Evidence |
|---|---|---|
| WX-SHELL-001 | One sidebar on the shadcn `Sidebar` primitive: workspace switcher; Home, My work, Inbox, Search; pinned and recent Boards and Docs (collapsible); Members and Settings; user card. Every entry routes to a real destination; the home shell's duplicate sidebar and switcher are removed. | interaction tests, keyboard navigation, mobile drawer story |
| WX-SHELL-002 | Topbar: breadcrumb from the product hierarchy (Workspace › Board › View), command trigger, notifications. | story and a test that the breadcrumb does not derive from URL guessing |
| WX-SHELL-003 | Command palette on `Dialog` + cmdk: Boards, Docs, Items, members and actions; one global ⌘K registration; results navigate to the real resource. | a11y test (focus trap, roles, labels), interaction test |
| WX-SHELL-004 | Scope transitions: a workspace switch shows the new scope without stale data; lost access shows an explicit forbidden state instead of a silent redirect; one scroll container. | scope-switch race test, forbidden story |
| WX-SHELL-005 | Keyboard shortcuts (G H, G I, …) with a discoverable shortcut sheet. | interaction tests |

### Status

Delivered on 2026-10-10 (uncommitted working tree on `develop`, baseline `e8a5876c`).

| Unit | Status | Delivered | Evidence |
|---|---|---|---|
| WX-SHELL-001 | Done | `WorkspaceSidebarSurface` on the `Sidebar` primitive: switcher (identity mark, name, role, create), Home, My work, Inbox, Search, Pinned and Recent (Boards and Docs mixed, collapsible, row menu Open/Pin/Unpin), Members, Settings, user card. Every entry has a real `href`; new routes `/workspaces/$id/my-work` and `/workspaces/$id/inbox` show honest empty states. Recents and pins come from the Workspaces producer (`createUseWorkspaceRecents/Pins`, `createUseTogglePin` optimistic with rollback); names are joined from the authorized Board and Doc lists, and unmatched references are dropped (`resolveNavigationEntries`). A recent is recorded when a Board or Doc route opens (`createUseRecordRecent`: one idempotency key per open, fire-and-forget). Mobile renders the primitive's drawer. `/home` no longer renders a second shell: it lands on the first workspace, or shows an onboarding state with one create-workspace dialog. "Team online" and "Quick access" are gone. | `workspace-shell.interaction.component.test.tsx` (`workspace.sidebar:*`, 8 cases incl. keyboard order), `workspace-navigation-state.component.test.tsx` (rollback, record, in-flight-read race), `workspace-shell-model.unit.test.ts`, stories `Workspace/Shell › Sidebar*` (mobile drawer in Default), mock e2e `auth-workspace` |
| WX-SHELL-002 | Done | `WorkspaceTopbarSurface`: breadcrumb from `buildWorkspaceBreadcrumb` (Workspace › Board/Doc › View) fed by matched route ids/params (`resolveShellLocation`) and loaded names, command trigger with platform chord, `@notrelix/features-notifications` bell, theme toggle with `aria-label`. | `workspace.topbar:breadcrumb`, `breadcrumb-unknown` (a resource missing from the authorized list never shows its id), `actions`; `workspace-shell-routing.unit.test.ts` |
| WX-SHELL-003 | Done | `WorkspaceCommandPaletteSurface` on `Dialog` + cmdk: navigation, Boards, Docs, people and actions (theme, shortcuts, create workspace) from the already-loaded lists, label-only diacritic-insensitive matching, "search the workspace" hand-off to the Search route. One global ⌘K/Ctrl+K registration (`useShellShortcuts`); `GlobalSearch` is no longer mounted. | `workspace.command-palette:a11y/navigate/search/actions/empty`; axe on all palette stories |
| WX-SHELL-004 | Done | `WorkspaceScopeBoundary` + `WorkspaceScopeStateSurface` replace the silent redirect: loading skeleton, forbidden (not listed, 403, 404) and error states with retry and a way out. Navigation keys are workspace-scoped (`workspaceQueryKeys.navigation/recents/pins`); the layout keys the provider by workspace. One scroll container (`main#workspace-main`); the nested `h-screen` frame and the account layout's inner scroll are gone. The mock-only view tabs left the shell (WX-DEC-010); `WorkspaceProvider` no longer loads views. | `workspace.scope-state:*`, scope-switch race test, `resolveWorkspaceScope` unit tests |
| WX-SHELL-005 | Done | G H / G M / G I, ⌘K, `?`; sequences pause while typing; `WorkspaceShortcutSheetSurface` is reachable from `?`, the palette and the user card. | `use-shell-shortcuts.component.test.tsx`, `createShortcutMatcher` unit tests, `workspace.shortcut-sheet:render` |

Shared changes: `@notrelix/features-workspace` may import `@notrelix/i18n` (catalog ownership, FE-ADR-006; manifest, ARCH-016 test and generated package-boundary doc updated). The `Sidebar` primitive omits `data-active` when inactive (Tailwind v4 matched `data-active="false"`), accepts `defaultOpenMobile` and translated drawer labels. The mock backend serves the five navigation operations (consumer surface rows added).

Open items:

- Dead on disk, unmounted, to delete with `shell/sidebar/` once its pending work lands: `apps/web/src/shell/sidebar/sidebar.tsx`, `sidebar/workspace-switcher.tsx`, `topbar/topbar.tsx`, `global-search.tsx`, `guards/workspace-guard.tsx`, `navigation-contributions.ts`, `home-shell.tsx`, `home/app-header.tsx`, `home/app-sidebar.tsx`, `home/types.ts`, `home/user-menu.tsx`, and the stub `workspace-tabbed-frame.tsx`. Unused exports of `@notrelix/features-workspace`: `WorkspaceViewTabs`, `WorkspaceAddViewMenu`, `createUseWorkspaceViews`, `createUseWorkspaceShellData`, `WorkspaceCompactHeader`, `WorkspaceContextualToolbar`, `WorkspaceDirectory` (the old sidebar still imports the shell-data hook).
- My work and Inbox are empty-state destinations; My work content is WX-HOME-001; Inbox has no producer yet.
- `/home` lands on the first listed workspace; remembering the last-active workspace needs a stored preference.
- Overlay primitives still use `z-50`; moving them to the z-index roles needs a nested-overlay rule (a popover inside a dialog must stack above it) and is left for WX-QUAL-001.
- Visual baselines for the 15 new stories must be generated on Linux CI with the Phase 1 regeneration.

### Phase 3 — Home ("resume work")

| Unit | Change | Evidence |
|---|---|---|
| WX-HOME-001 | Main column: "Continue" (5–8 recent Boards/Docs as a dense list with identity icon, name, location and updated time) and "My work" (Items grouped overdue / today / this week, completable inline). Side column: workspace activity and pending invitations (admins only). | stories for default, empty, loading, error and partial data |
| WX-HOME-002 | Teaching empty states for a new workspace (create a Board, create a Doc, invite people). | onboarding story |

### Status

Delivered on 2026-10-10 (uncommitted working tree on `develop`, baseline `16544e64`).

| Unit | Status | Delivered | Evidence |
|---|---|---|---|
| WX-HOME-001 | Done | `WorkspaceHomeSurface` replaces the dashboard (metric tiles and the five `[]`-wired widgets are deleted). Main column: Continue (up to 8 recents joined with the authorized Board/Doc lists, resource mark, location, "Opened …" via `useFormatters`) and a My work preview grouped overdue / today / this week / later / no due date (`groupMyWorkItems`, local calendar days) with inline completion and item links (`?item=`). Side column: workspace activity and, for admins, pending invitations. Each section loads, fails and retries on its own. `/workspaces/$id/my-work` now renders the full list (`WorkspaceMyWorkSurface`, cursor paging). Producers: `WorkManagement.BoardItems.ListMyWork`, `WorkManagement.BoardItems.Complete` (one `Idempotency-Key` per attempt, not optimistic), `Workspaces.Activity.GetWorkspaceActivity` (adapter wired from the contract), `Workspaces.Invitations.ListWorkspaceInvitations`. | `workspace.home` (8 cases) and `workspace.my-work` (6 cases) interaction tests; stories Default (light/dark/mobile/tablet), Loading, Error, EdgeData (partial data, Vietnamese long names), Empty; model tests in `workspace-pages-model.unit.test.ts`; mock e2e `workspace-pages` (Home). |
| WX-HOME-002 | Done | `WorkspaceOnboardingSurface` replaces Continue in a workspace with no Boards and no Docs: create a Board (`WorkManagement.Boards.Create`, idempotency key per submit), write a Doc (Documents page create), invite people (Members › Invitations). Steps the member cannot take are explained, not shown (guests cannot create; only admins invite). `CreateResourceDialog` names the resource with a bound label and inline errors, then navigates to it. | `workspace.onboarding` (2 cases), `workspace.create-resource` (2 cases); onboarding stories Default and PermissionLimited. |

Open items:

- UNRESOLVED (WX-BE-006, item 5): the activity producer returns `{ data: [], total }` with an untyped entry (`object`), so Home always shows the empty activity state; the adapter keeps only entries carrying every displayed field until an entry contract exists. A real source exists — the Collaboration-owned `WorkspaceActivityLogRecord` projection, written by the BoardCreated, CommentCreated, MentionCreated and WorkspaceMemberAdded consumers — but it is not wired, because wiring needs a decision recorded in §6 (feed visibility and owner).
- The My Work and complete adapters live in `apps/web/src/shell/workspace/work-management-bridge.ts` as a recorded transition: they belong to `@notrelix/work-management-state`, which this phase could not edit. Move them with WX-BOARD-003.
- The Board page does not read the `item` search parameter yet, so an item link opens its Board; the Item side sheet is WX-BOARD-002.
- `@notrelix/docs-state` `createUseCreatePage` passes `pageApi.create` unbound (`this.getDetail` throws after a successful POST); the app calls `create` as a method. Fix in the Documents package.
- The mock clock is fixed at 2025-01-15, so mock My work items render as overdue.

### Phase 4 — Boards and Docs directory

| Unit | Change | Evidence |
|---|---|---|
| WX-DIR-001 | One dense table of Boards and Docs with filter, sort, pin, bulk actions and keyboard selection; sticky name column on narrow screens; virtualization above the threshold. | interaction, a11y and performance tests (300+ rows) |

### Status

Delivered on 2026-10-10 (uncommitted working tree on `develop`).

| Unit | Status | Delivered | Evidence |
|---|---|---|---|
| WX-DIR-001 | Done | Route `/workspaces/$id/library` ("Boards & Docs") in the sidebar and the command palette. `WorkspaceLibrarySurface`: one table (name with resource mark, type, location, updated, pinned), filter by type and by diacritic-insensitive text, sortable columns with `aria-sort`, pin per row and bulk pin/unpin of the selection, roving-focus keyboard rows (↑ ↓ Home End, Space selects, Enter opens), sticky select + name columns with horizontal scroll on narrow screens. Above 100 rows the table windows its rows in its own scroll region (`libraryWindow`, fixed 40 px rows, `aria-rowcount`/`aria-rowindex`); no virtualization library was added (none existed). Pins write one resource at a time through the idempotent pin producer, so a partial bulk failure leaves the saved pins accurate. | `workspace.library` (7 cases incl. 320-row windowing with keyboard End to row 320 and a render-time budget); stories Default, Loading, Empty, Error, EdgeData, HighDensity (320 rows), ReadOnly, PermissionLimited; model tests for filter, sort and window. |

Open items:

- The Board list DTO has no last-change time, so Boards show "—" in Updated (Docs use `updatedAt`).

### Phase 5 — Members and Settings

| Unit | Change | Evidence |
|---|---|---|
| WX-MEM-001 | Members table with inline role edit, an invitations tab (create and revoke wired to the backend), suspend/activate, `AlertDialog` confirmations and precise error toasts; respects the last-owner invariant (WSP-008). | interaction and permission-state tests |
| WX-SET-001 | Settings in grouped sections (Profile, Defaults, Danger zone) with bound labels, per-section save, a saved description and an explicit destructive flow. | form tests |

### Status

Delivered on 2026-10-10 (uncommitted working tree on `develop`).

| Unit | Status | Delivered | Evidence |
|---|---|---|---|
| WX-MEM-001 | Done | `WorkspaceMembersSurface` replaces the 383-line members route. Members tab: name filter, inline role select (Owner is never offered — ownership moves by transfer; the last owner is never offered demotion, suspension or removal, with the reason shown; admins cannot change owners; nobody changes themselves), suspend and reactivate, removal; suspend and remove confirm in an `AlertDialog`. Invitations tab (admins): invite by e-mail with role, inline address validation and the producer's message next to the field, pending and expired list, revoke with confirmation. Non-admins get a read-only explanation. Toasts name the member and carry the producer's rule message when it sends one. Adapters follow the producer: role change is `PATCH …/members/{userId}` with the role enum value (the old adapter used `PUT …/role`), invite is `POST …/members`, revoke is `DELETE …/invitations/{id}`, suspend/activate are `POST …/suspend|activate`; the invitations query now lists the workspace's invitations instead of the caller's own. | `workspace.members` (12 cases incl. last-owner, AlertDialog confirm/cancel, invite validation and producer error); stories Default, Loading, Error, Success (invitations), Empty, EdgeData, ReadOnly, PermissionLimited; mock e2e role change and confirmed removal. |
| WX-SET-001 | Done | `WorkspaceSettingsSurface` with Profile (name, description with counter), Defaults (default role, invitation expiry 1–30 days, guest invites, public links, two-step verification) and Danger zone (archive, confirmed by typing the workspace name; permanent delete is not offered). Labels are bound (`Field`, `Textarea`, `Switch`), each section saves on its own with inline validation, a saved state and conflict recovery ("Load the latest"). The adapter writes `PATCH /workspaces/{id}/profile` with `name`, `description` and `expectedVersion` (the producer accepts the description), `PUT …/settings` and `POST …/archive`. | `workspace.settings` (9 cases); stories Default, Loading, Error (conflict), Success, EdgeData, ReadOnly, Unavailable. |

Producer gaps found by this phase and their closure (WX-BE-006, uncommitted working tree on `develop`, baseline `84100b9a`). All contract changes are additive; the generated client and `backend/contracts/openapi/notrelix.v1.json` were regenerated.

| Gap | Status | Change | Evidence |
|---|---|---|---|
| `GetWorkspace` returned no `version`, while profile, settings, archive and delete require `expectedVersion > 0` | Closed | `WorkspaceDto.version` (the Workspace aggregate version) on `GetWorkspace`, `ListUserWorkspaces`, `GetAccountWorkspaces` and `ResolveSlug`. The Settings page now saves and archives: it sends the version it read, a stale one is a 412 shown as "Load the latest". The adapter also reads the version on each list row. | `WorkspaceAdministrationReadIntegrationTests.GetWorkspace_ReportsTheVersionThatWritesAccept_AndAStaleVersionIsRejectedWithoutChange` (PostgreSQL: profile and settings saved with the read version, stale profile and archive rejected with no change, archive with the current version); `WorkspaceAdministrationReadHandlerTests`; mock `T-MFB-028` (save, 412, archive). |
| `ListMembers` had no status and listed removed members | Closed | `WorkspaceMemberDto.status` (`Active` or `Suspended`, the domain enum name). Removed members are not listed: removal ends the participation (docs/product/workspaces.md §13, §20). `memberCount` counts the same members. The row offers Suspend only for an active member and Reactivate only for a suspended one; with no reported status it offers neither. | `ListMembers_ReportsStatus_AndNeverListsARemovedMember`, `ListMembers_IsDeniedToANonMember` (PostgreSQL); handler and model tests; `T-MFB-028`. |
| `ListWorkspaceInvitations` had no status, so revoked invitations listed as pending | Closed | `WorkspaceInvitationDto.status` (`Pending`, `Accepted`, `Revoked`, `Expired`, `Declined`). Rows stay listed (the lifecycle exposes status; expired invitations can be resent); the adapter drops revoked and declined rows. | `ListWorkspaceInvitations_ReportsEachInvitationsStatus` (PostgreSQL); handler and model tests; `T-MFB-028`. |
| Duplicate-invite message hard-coded in Vietnamese | Closed | The invite form checks the listed invitations first (`pendingInvitationFor`, same rule as the producer: one pending, unexpired invitation per address) and shows `invitations.alreadyPending` in vi and en. The producer's message is now English like every other producer message; its failure category is unchanged. | model test `finds the invitation already waiting at an address`; `pnpm check:i18n`. |
| ProblemDetails carries no domain rule code | UNRESOLVED | Not changed: no context surfaces rule codes in ProblemDetails today (`ProblemDetailsMapper` and `EndpointExtensions.TypedFailure` drop `BusinessRuleException.RuleCode` and `ApplicationError.Code`), so there is no mechanism to follow. See §6. | — |
| Workspaces responses typed `System.Void` | Closed for every Workspaces operation that returns a body | `Produces<T>` on 19 endpoints (workspace, member, invitation, settings, Space and Team reads; `Guid` for resend; `201` + `Guid` for create workspace, invite, create Space and create Team, which already returned 201). | `WorkspacesOpenApiShapeTests` (22 cases); OpenAPI re-export `cmp` clean. |
| Teams: `TeamDto` had no status or version, no read listed a Team's members, Team writes ignored `expectedVersion` and accepted any role string, and `IsArchived` duplicated the status | Closed in Phase 9 (commits `b56b832e`, `a5531bac`, `a919a8ae`) | Class E contract change, shape-breaking for `IsArchived` only; the generated client and the Team adapter were the only consumers and were migrated in the same phase. Reads: `TeamDto.status` and `TeamDto.version`; new `GET /api/v1/workspaces/{workspaceId}/teams/{teamId}/members` returning `TeamMemberDto` (active members only); `memberCount` counts active members only; `IsArchived` removed; `GetTeam` filters `DeletedAt`. Writes: the 8 Team writes bind `expectedVersion` and answer 204; an unknown Team role is a 400. | backend `GetTeamMembersQueryHandlerTests`, `TeamAdministrationReadIntegrationTests` (PostgreSQL), `TeamWriteConcurrencyIntegrationTests` (PostgreSQL, a stale version is rejected without change), `WorkspacesOpenApiShapeTests`; OpenAPI and generated client regenerated (`a919a8ae`). |

Remaining open items:

- `GetWorkspaceActivity` stays untyped (`object`) until the activity decision in §6 is made.
- Workspaces commands that return 204 (`ToNoContentResult`) are still documented as `200` with `System.Void`; correcting them changes the documented status of about 30 operations and was left out of this change.
- `GetUserWorkspaces` lists every workspace in which the caller has a membership row, including a removed one, so a removed member can still see the workspace in the switcher (opening it is decided by authorization). Not changed here: it is a scope rule for Workspaces/Governance to confirm.
- Not touched: invitation-link adapters (`/workspaces/invitations/by-token|accept|pending` versus the producer's `/invitations/preview|accept|pending`) and create-workspace (`POST /workspaces` versus `/accounts/{accountId}/workspaces`); neither surface is part of these phases.

Shared changes (Phases 3–5): `@notrelix/ui-web` `cn()` now knows the semantic text roles, so `text-metadata` is no longer dropped when a text color follows it in one `cn()` call (this also fixes the size of the Phase 2 small identity marks). Removed: the dashboard widgets (`active-boards`, `activity-feed`, `pinned-docs`, `upcoming-deadlines`, `workspace-overview`, `dashboard/index`), `workspace-dashboard-surface` and its manifest row, stories and test, the dashboard route component, and the superseded hooks `use-update-workspace`, `use-update-member-role`, `use-remove-member`, `use-delete-invitation`. The mock backend serves the new operations with producer shapes (members, invitations, activity, settings, My work, complete; Board create returns the id). Design-token baseline: 250 → 200 violations.

### Phase 6 — Board view

Starts only after the other session's table work is committed or explicitly superseded.

| Unit | Change | Evidence |
|---|---|---|
| WX-BOARD-001 | Board header and view bar inside the new shell; contextual navigation only (DESIGN.md §27.2). | stories |
| WX-BOARD-002 | Table and Kanban on the new tokens: dense editing, sticky regions, virtualization (DESIGN.md §17–18); Item detail in a side sheet. | interaction, a11y and performance tests |
| WX-BOARD-003 | First-party Work consumer cutover (WM-V3-FE-001): server `version` as `expectedVersion`, neighbor ids for placement, one idempotency key per canonical mutation attempt, conflict and stale-placement recovery. | consumer surface manifest, contract-conformance and retry tests |

### Phase 7 — Quality closure

| Unit | Change | Evidence |
|---|---|---|
| WX-QUAL-001 | Audit (a11y, responsive, performance) and polish pass across every surface; all superseded components and their stories removed. | `pnpm validate`; visual snapshots in light, dark and High contrast × desktop, tablet and mobile |

### Phase 8 — Space org tree (OT-5)

Frontend of the department org tree (docs/product/workspaces.md WSP-021..025, docs/product/governance.md GOV-025..029, ADR-009); the backend producer is OT-0..OT-4.

### Status

Delivered on 2026-10-11 (baseline `870e47c8`).

| Unit | Status | Delivered | Evidence |
|---|---|---|---|
| WX-SPACE-001 | Done | Space adapters on the generated contract (`createSpacesService`: tree, create with parent/type/visibility/neighbors, rename, description, type, visibility, archive, unarchive, move with `expectedVersion`, members list/add/change role/remove for User and Team subjects, teams list). Workspace-scoped keys (`workspaceQueryKeys.spaces/spaceTree/spaceMembers/teams`). Only a move is optimistic (rolled back on rejection); every write refreshes the tree so the next write carries the current version; a 409/412 is shown as a conflict, never retried. | `workspace-spaces-model.unit.test.ts`, `space-state.component.test.tsx` (rollback, versioned member writes, per-workspace keys, expansion storage), mock `T-MFB-029`. |
| WX-SPACE-002 | Done | `SpaceTreeNav` in the workspace sidebar: WAI-ARIA navigation tree (roving focus, ↑ ↓ → ← Home End, type-ahead), unlimited depth with only expanded subtrees rendered and windowing above 150 visible rows, Private marker, archived Spaces leave the sidebar, "New Space" only for workspace Owner/Admin. Expansion is a per-member, per-workspace preference in `localStorage` (guarded; blocked storage falls back to memory) and the ancestors of the current Space open automatically. | `workspace.space-tree` (7 cases incl. 525-row windowing), `workspace.sidebar:spaces`. |
| WX-SPACE-003 | Done | Space page `/workspaces/$id/spaces/$spaceId` (`WorkspaceSpaceSurface`): path breadcrumb (also in the topbar), child Spaces, Boards and Docs placed in the Space; Members tab (Users and Teams, Admin/Member/Viewer, last-Admin protection with the reason, removal in an `AlertDialog`, add a person or a team); Settings tab (name, type, description, visibility saved field by field; archive unavailable with the reason while visible content remains, the producer's refusal shown when hidden content blocks it; unarchive). Actions follow the tree's `accessLevel` (Manager / Editor / Viewer). | `workspace.space-page` (13 cases), stories Default/Members/Settings/PermissionLimited/Loading/Error/Empty/ReadOnly/EdgeData/Unavailable. |
| WX-SPACE-004 | Done | Create in a Space (Board and Doc dialogs say where the resource goes; `spaceId` sent to `WorkManagement.Boards.Create` and `Documents.Pages.CreatePage`), `CreateSpaceDialog` (Department → Private by default, other types open, WSP-023), `MoveToSpaceDialog` with a searchable tree picker for Spaces (never into their own subtree) and Boards (Space page and Boards & Docs rows), Boards & Docs filter by Space (a Space includes the Spaces inside it). | `workspace.create-space`, `workspace.move-to-space`, `workspace.library:space-filter`/`move`, mock e2e `workspace-spaces`. |

Open items:

- Done — Doc move between Spaces (WX-SPACE-004 closure). Producer (additive, Class E): `PageDto.Version` and `PageTreeItemDto.Version` (the Page aggregate version) on `Documents.Pages.GetPage`, `ListWorkspacePages`, `GetPageTree` and `SearchPages`, which now declare `Produces<T>` (the contract had `System.Void` on all four). Consumer: `@notrelix/docs-core` `Page.version`, `@notrelix/docs-state` `createUseMovePageToSpace` (optimistic on the page list with rollback; every outcome refreshes the list, the tree and the page) over `Documents.Pages.MoveToSpace`; the `MoveToSpaceDialog` takes a `page` subject; the Space page Docs rows and the Boards & Docs root-Doc rows offer "Move" (a nested Doc follows its root, docs/product/workspaces.md §79). Evidence: backend `PageReadVersionTests` (5), `SpacePlacementIntegrationTests.PageReads_ExposeTheVersion_MoveToSpaceAccepts_AndAStaleVersionIsRejectedWithoutMoving` (PostgreSQL; stale version → `common.precondition-failed`, the API's 412), `DocumentsOpenApiShapeTests` (9); frontend `workspace.move-to-space:page`, `workspace.space-page:actions`, `workspace.library:move`, docs-state `page-move-to-space.unit.test.ts`, mock `T-MFB-037` (412, subtree, nested refusal), mock e2e `workspace-spaces` (Doc created in a Space and moved).
- Closed in commit `3056966b`: `RemoveSpaceMemberEndpoint` (and `DeleteWorkspace`) bind `[FromQuery] long expectedVersion`; the OpenAPI document lists the required query parameter (`WorkspacesOpenApiShapeTests.VersionedDelete_ReadsExpectedVersionFromTheQuery`).
- TRANSITION: Board placement (`spaceId`, `version`) is read and moved in `apps/web/src/shell/workspace/board-placement-bridge.ts` because the `@notrelix/work-management-state` Board model drops both fields and that package carries another session's uncommitted work; it moves with WX-BOARD-003. The placement read is a second call to the Board list until then.
- Closed in Phase 9: Teams UI (list, create, detail, members, lifecycle; WX-TEAM-001 and WX-TEAM-002).
- Closed in Phase 9: the Space tree picker in the move dialog is windowed above 150 visible rows (WX-SPACE-005).
- Correction (commit `bf9ed268`, Class F, NRX-002): the Space org-tree work added database foreign keys `fk_boards_space` (`work.boards.space_id`) and `fk_pages_space` (`docs.pages.space_id`) to the Workspaces-owned `workspace.spaces`, and left `SpacePathRecord` unregistered; the EF ownership gate forbids foreign keys across owner boundaries and requires one owner per entity. Space containment is not a cross-context object graph (docs/product/workspaces.md §79), so Boards and Pages reference a Space by scalar id only and the target Space is validated in Application (`IBoardSpacePlacement` / `IPageSpacePlacement.EnsureActiveTargetAsync`). Migration `DropCrossContextSpaceForeignKeys` drops the two constraints (the columns and their indexes stay; no data change) and `SpacePathRecord` is registered as a Workspaces-owned projection. Evidence: `EfOwnershipGateTests` green, `SpaceHierarchyIntegrationTests` extended.

### Phase 9 — Teams and picker windowing

Delivered on 2026-10-11 (baseline `15fa4bc9`). Product owner: Workspaces (docs/product/contexts/workspaces.md, Teams). Applicable rules: NRX-003, NRX-004 (Team writes are Workspace-administered), NRX-007, NRX-008 (Team contract change), NRX-014 (workspace-scoped query keys), NRX-016 (non-zero evidence). Backend producer changes are recorded in the Phase 5 gap table (Teams row).

| Unit | Status | Delivered | Evidence |
|---|---|---|---|
| WX-TEAM-001 | Done | Team model, capabilities and service on the generated contract (`eaec6ca8`); Team query and mutation hooks with workspace-scoped keys (`d787e969`); Teams list, create dialog and Team detail surfaces: Team name, description and status, member list with add, role change and removal, archive and restore, rename and description edit (`ed9048c0`). An archived Team blocks rename and description edits (`9726310a`). Every write sends the Team `version` as `expectedVersion`; a stale version is shown as a conflict and is never retried. | interaction markers `FUI[workspace.teams:*]`, `FUI[workspace.create-team:*]`, `FUI[workspace.team:*]`; model and service unit tests; stories for the Teams list, create dialog and Team detail; backend evidence in the Phase 5 gap table. |
| WX-TEAM-002 | Done | App composition: Teams routes, sidebar navigation and breadcrumb (`e94acf06`); Team writes are serialized on the Team page (`1a2fdb13`) and member rows are locked while a Team write is in flight (`4153ad11`); mock-backend Team writes and members with version semantics (`275f85b0`). | mock conformance `T-MFB-038`; mock e2e `workspace-teams`; `workspace.sidebar` navigation cases. |
| WX-SPACE-005 | Done | The Space picker of `MoveToSpaceDialog` windows its rows above 150 visible rows (`7701094c`), like the sidebar tree; a filter still narrows it. | `FUI[workspace.move-to-space:windowing]`; story `MoveToSpaceHighDensity`. |

Open items:

- Team member e-mail is `null` from the producer (`TeamMemberDto` carries no e-mail), so the member list shows the display name only. Needs a producer and privacy decision (which Team members' addresses a Team administrator may see) before any field is added.
- The Team page breadcrumb has no Team name and no back link to the Teams list.
- Visual baselines for the new Teams stories and `MoveToSpaceHighDensity`, and the roughly 2 px row shift in the move dialog stories caused by windowing, must be regenerated in Linux CI with the pending baseline regeneration (`visual-baseline.lock.json`).
- `RestoreTeam` still documents a phantom `200` (it answers 204), like the other 204 commands listed under the Phase 5 remaining items.

## 5. Sequencing

```text
Phase 0 (backend gaps)  ─┐
Phase 1 (foundation)    ─┴→ Phase 2 (shell) → Phase 3 (Home) → Phase 4 (directory)
                                            → Phase 5 (members/settings)
                                            → Phase 6 (Board view, after the table work lands)
                                            → Phase 7 (quality closure)
```

Phase 0 and Phase 1 run in parallel; Phase 3 depends on WX-BE-001, WX-BE-002 and WX-BE-005.

## 6. Open decisions

WX-DEC-010 to WX-DEC-012 closed the brief's open questions. The i18n ADR in WX-FND-006 records the i18next choice and its integration rules. Two decisions are UNRESOLVED (WX-BE-006):

| ID | Decision needed | Owner | Why it is not inferred |
|---|---|---|---|
| WX-OPEN-001 | Workspace activity feed: which activity a member sees in a workspace-wide feed, and who serves it. The Collaboration projection `WorkspaceActivityLogRecord` records Board creation, comments, mentions and member additions, all with `Visibility = "Workspace"`, whatever the access of the Board or Doc they concern. A feed read without a per-entry resource authorization rule would show activity on private Boards and restricted Docs to every member. Needed: the visibility rule (per-entry resource authorization, or a visibility set at projection time), the entry contract, and whether the read moves from `Workspaces.Activity` to Collaboration (docs/product/collaboration.md §60–65, docs/product/workspaces.md §81). | Collaboration with Governance | Product and security semantics (NRX-003, NRX-004); no product document defines feed visibility. |
| WX-OPEN-002 | Whether ProblemDetails exposes the domain rule code (for example `ruleCode` next to `errorCode`) for every context, so clients can localize rule failures. Domain rule codes exist (`WorkspaceRuleCodes`) and backend/docs/architecture/domain-modeling.md says API and frontend may map them to localized text, but no context exposes them today. | API contract owner (backend/docs/architecture/api-and-contracts.md §43–46) | A repository-wide public error contract change (NRX-007), not a Workspaces-local one. |
