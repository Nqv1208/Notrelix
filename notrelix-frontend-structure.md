# Notrelix Frontend Structure and FSD Refactor Plan

> Updated: 2026-06-08  
> Stack: Next.js 16 App Router, TypeScript, shadcn/ui, TanStack Query, Bun  
> Target pattern: Feature-Sliced Design compatible with the current App Router structure

---

## 1. Muc tieu

Frontend hien tai da co huong `app -> features -> lib`, nhung chua toi uu theo boundary ro rang:

- Mot so feature API dang gom nhieu nghiep vu trong mot file lon, dac biet `features/workspace/api/workspace.service.ts`.
- Hook da tach thanh nhieu file, nhung van con tron query, mutation, optimistic cache, mapper, toast/error side effects trong cung mot hook.
- Mot vai feature import truc tiep feature khac, lam mat tinh doc lap cua FSD.
- Tai lieu structure cu van mo ta `src/`, `(main)`, `/login`, `/register`, trong khi code hien tai dung `frontend/app`, `(auth)`, `(dashboard)`, `(workspace)`, `/sign-in`, `/sign-up`.

Muc tieu cua plan nay:

- Chia `api/` cua moi feature thanh cac module HTTP theo nghiep vu rieng: `workspace.api.ts`, `members.api.ts`, `views.api.ts`, `activity.api.ts`, `invitations.api.ts`, v.v. File `*.service.ts` chi nen dung lam orchestration/facade tam thoi neu can migration.
- Chia `hooks/` thanh query hooks, mutation hooks, UI-state hooks va cache helpers ro rang.
- Giu route UI trong `app/**/_components`, shared UI trong `components/**`, logic domain trong `features/**`.
- Giam import ngang giua cac feature. Neu can orchestration nhieu domain, dua vao app-level composition hoac shared service ro rang thay vi feature A import feature B.
- Refactor theo phase nho, co test/build sau moi phase.

---

## 2. Hien trang du an

Repo frontend thuc te khong nam trong `src/`. Cau truc hien tai:

```txt
frontend/
├── app/                         # Next.js App Router, UI + routing
│   ├── (app)/                   # Landing/public marketing pages
│   ├── (auth)/                  # sign-in, sign-up, forgot-password
│   ├── (dashboard)/             # /home authenticated shell
│   ├── (workspace)/             # workspace/account routes
│   ├── invite/[token]/          # public invitation route
│   ├── layout.tsx
│   └── providers.tsx
├── components/                  # shared UI primitives/providers
├── features/                    # domain logic
│   ├── account/
│   ├── auth/
│   ├── boards/
│   ├── docs/
│   ├── notifications/
│   ├── theme/
│   └── workspace/
├── hooks/                       # global hooks only
├── lib/
│   ├── api/                     # api-client + endpoints
│   ├── auth/
│   ├── query/                   # query key factory
│   └── utils.ts
├── messages/
├── styles/
└── types/
```

### File API lon nhat truoc Phase 1 workspace

```txt
402 lines  features/workspace/api/workspace.service.ts
227 lines  features/docs/api/page.service.ts
121 lines  features/docs/api/block.service.ts
72 lines   features/boards/api/card.api.ts
67 lines   features/boards/api/board.api.ts
53 lines   features/boards/api/column.api.ts
47 lines   features/boards/api/group.api.ts
43 lines   features/auth/api/auth.service.ts
39 lines   features/boards/api/comment.api.ts
30 lines   features/notifications/api/notifications.service.ts
14 lines   features/account/api/account.service.ts
```

### File hook dang tap trung nhieu nghiep vu

```txt
117 lines  features/boards/hooks/use-create-card.ts
91 lines   features/boards/hooks/use-board-table.ts
77 lines   features/boards/hooks/use-board-view.ts
62 lines   features/docs/hooks/use-doc-toolbar.ts
60 lines   features/boards/hooks/use-update-field-value.ts
50 lines   features/boards/hooks/use-update-card.ts
46 lines   features/notifications/hooks/use-notifications.ts
34 lines   features/workspace/hooks/use-update-workspace-view.ts
31 lines   features/workspace/hooks/use-active-workspace-view.ts
31 lines   features/workspace/hooks/use-workspace.ts
```

---

## 3. Danh gia kien truc hien tai

### Uu diem

- `app` va `features` da duoc tach theo y tuong dung: page/layout nam trong `app`, hooks/API/types nam trong `features`.
- `boards` da co huong split service theo resource: `board.api.ts`, `card.api.ts`, `column.api.ts`, `group.api.ts`, `comment.api.ts`.
- TanStack Query dung `queryKeys` tap trung trong `lib/query/query-keys.ts`, day la nen tot de invalidate co scope.
- Backend endpoint constants tap trung trong `lib/api/endpoints.ts`, tranh hard-code URL trong component.
- Route-private UI da co nhieu `_components`, phu hop App Router.

### Nhuoc diem

- `workspace.service.ts` dang lam qua nhieu viec:
  - workspace CRUD
  - member CRUD
  - invitations
  - activity logs
  - workspace views
  - snapshot composition
  - DTO mapping
  - custom view settings parsing
  - cross-feature calls sang `boards` va `docs`
- `features/workspace/api/workspace.service.ts` import `features/boards/api/board.api` va `features/docs/api/page.service`. Day la import ngang feature-to-feature, khong tot cho FSD.
- Mapper DTO dang nam chung voi service, lam API file kho test va kho tach.
- Hook mutation dang gom ca API call, optimistic update, toast, rollback, cache key logic. Khi logic lon len se kho maintain.
- `features/docs/components` dang chua hop rule "features khong chua UI component". Neu component chi dung trong route docs, nen nam trong `app/(workspace)/[workspaceId]/docs/**/_components`. Neu dung lai nhieu noi, dua vao `components/shared` hoac mot shared editor module co chu dich ro rang.
- File naming chua dong nhat:
  - `auth.service.ts`, `workspace.service.ts`
  - nhung `board.api.ts`, `card.api.ts`
  - Huong hien tai cua frontend la uu tien `*.api.ts` cho HTTP resource module. `*.service.ts` chi dung khi can business orchestration hoac compatibility facade.

---

## 4. Pattern chuan de ap dung

### 4.1 Layer rules

```txt
app               -> routing + route UI + composition
components        -> shared UI used by 2+ routes/features
features/<slice>  -> domain logic, services, hooks, schemas, mappers, types
hooks             -> global generic hooks only
lib               -> framework infrastructure: API client, endpoints, query keys, auth token, utils
```

Import direction:

```txt
app -> features -> lib -> external
app -> components -> external
features -> components/ui only when unavoidable for non-route UI should be avoided
features must not import from app
features should not import from other features
```

If a use case needs many domains, place the orchestration in one of these:

- `app/**/_components` for route-specific composition.
- `features/<owning-feature>/model/compose-*.ts` only if the feature owns the use case.
- `lib/domain/` only for cross-domain pure helpers with no UI and no feature imports.

### 4.2 Feature folder contract

Target structure for each feature:

```txt
features/<feature>/
├── api/
│   ├── <resource>.api.ts           # HTTP calls for one resource only
│   ├── <resource>.mapper.ts        # optional: API DTO -> frontend model
│   ├── <resource>.dto.ts           # optional: API response/request DTOs
│   └── index.ts                    # optional public API for services
├── hooks/
│   ├── queries/
│   │   ├── use-<resource>.ts
│   │   └── use-<resource>-list.ts
│   ├── mutations/
│   │   ├── use-create-<resource>.ts
│   │   ├── use-update-<resource>.ts
│   │   └── use-delete-<resource>.ts
│   ├── state/
│   │   └── use-<local-ui-state>.ts
│   └── index.ts
├── model/
│   ├── query-cache.ts              # optimistic update helpers
│   ├── selectors.ts                # pure derived data
│   └── constants.ts
├── schemas/
├── types/
├── utils/
├── mock/                           # dev/test mock only
└── index.ts                        # public feature API
```

Rules:

- API files only do HTTP + DTO mapping. No React, no toast, no TanStack Query.
- Mapper files are pure and unit-testable.
- Query hooks only do `useQuery`.
- Mutation hooks only do one mutation use case. Large optimistic updates move to `model/query-cache.ts`.
- UI-state hooks with `useState`, URL state, editor selection, filters can live in `hooks/state`.
- Feature public exports go through `features/<feature>/index.ts` or `hooks/index.ts`. Avoid importing deep files from unrelated features.

---

## 5. Target split by feature

### 5.1 Workspace

Phase 1 status: workspace API and hooks have been split. `workspace.service.ts` is now a compatibility facade only.

Target:

```txt
features/workspace/
├── api/
│   ├── workspace.api.ts            # list/get/create/update workspace
│   ├── members.api.ts              # members, roles, remove
│   ├── invitations.api.ts          # invitations by workspace/token/pending/accept
│   ├── views.api.ts                # persisted/custom workspace views in settings
│   ├── activity.api.ts             # workspace activity logs
│   └── workspace.service.ts        # compatibility facade only
├── hooks/
│   ├── queries/
│   │   ├── use-workspace.ts
│   │   ├── use-workspace-snapshot.ts
│   │   ├── use-workspace-views.ts
│   │   ├── use-workspace-members.ts
│   │   ├── use-workspace-invitations.ts
│   │   ├── use-workspace-activity.ts
│   │   ├── use-invitation-by-token.ts
│   │   └── use-pending-invitations.ts
│   ├── mutations/
│   │   ├── use-create-workspace.ts
│   │   ├── use-update-workspace.ts
│   │   ├── use-create-workspace-view.ts
│   │   ├── use-update-workspace-view.ts
│   │   ├── use-reorder-workspace-views.ts
│   │   ├── use-update-member-role.ts
│   │   ├── use-remove-member.ts
│   │   ├── use-create-invitation.ts
│   │   ├── use-delete-invitation.ts
│   │   └── use-accept-invitation.ts
│   ├── state/
│   │   └── use-active-workspace-view.ts
│   └── index.ts
├── model/
│   ├── query-cache.ts              # next cleanup step for larger optimistic updates
│   └── selectors.ts                # next cleanup step for larger derived data
├── types/
│   ├── index.ts
│   └── dto.ts                      # workspace/member/invitation/activity DTOs
├── schemas/
└── utils/
    ├── settings.ts                 # parse/serialize settings.customViews
    └── workspace-views.ts          # pure default view builder and sorter
```

Important:

- `workspace.service.ts` must stay a small facade and must not import `features/boards` or `features/docs`.
- `use-workspace-views.ts` composes default views from `useWorkspaceBoards`, `usePageList`, and `useWorkspace` at hook level. This is the approved temporary composition until backend supports persisted workspace views.
- `use-workspace-snapshot.ts` composes workspace/members/views/activity at hook level and keeps activity failure non-blocking.

### 5.2 Boards

Current state: API split is already closer to target, but naming and hooks can improve.

Target:

```txt
features/boards/
├── api/
│   ├── board.service.ts
│   ├── board-view.service.ts
│   ├── card.service.ts
│   ├── column.service.ts
│   ├── group.service.ts
│   ├── comment.service.ts
│   ├── attachment.service.ts
│   ├── activity.service.ts
│   ├── board.mapper.ts
│   ├── card.mapper.ts
│   └── dto.ts
├── hooks/
│   ├── queries/
│   │   ├── use-workspace-boards.ts
│   │   ├── use-full-board.ts
│   │   ├── use-board-view.ts
│   │   ├── use-board-columns.ts
│   │   ├── use-board-groups.ts
│   │   ├── use-card.ts
│   │   ├── use-card-comments.ts
│   │   ├── use-card-files.ts
│   │   └── use-card-activity.ts
│   ├── mutations/
│   │   ├── use-create-card.ts
│   │   ├── use-update-card.ts
│   │   ├── use-delete-card.ts
│   │   ├── use-move-card.ts
│   │   ├── use-duplicate-card.ts
│   │   ├── use-update-field-value.ts
│   │   ├── use-create-group.ts
│   │   ├── use-update-group.ts
│   │   ├── use-delete-group.ts
│   │   ├── use-create-column.ts
│   │   ├── use-update-column.ts
│   │   └── use-upload-card-file.ts
│   ├── state/
│   │   ├── use-board-table.ts
│   │   ├── use-selected-card-panel.ts
│   │   ├── use-table-search.ts
│   │   ├── use-table-filters.ts
│   │   ├── use-table-sort.ts
│   │   ├── use-column-resize.ts
│   │   └── use-column-visibility.ts
│   └── index.ts
├── model/
│   ├── full-board-cache.ts
│   ├── optimistic-card.ts
│   ├── board-view-config.ts
│   └── selectors.ts
```

Important:

- Rename `*.api.ts` to `*.service.ts` only if all imports are updated in the same phase.
- Extract optimistic card creation from `use-create-card.ts` to `model/optimistic-card.ts`.
- Extract full-board cache update helpers from hooks to `model/full-board-cache.ts`.

### 5.3 Docs

Current state: service split exists for page/block, but `page.service.ts` still mixes list/tree/detail/breadcrumb/favorites/comments/history/search.

Target:

```txt
features/docs/
├── api/
│   ├── page.service.ts             # list/detail/create/update/delete/favorite
│   ├── page.mapper.ts
│   ├── page.dto.ts
│   ├── block.service.ts
│   ├── block.mapper.ts
│   ├── block.dto.ts
│   ├── breadcrumb.service.ts
│   ├── comment.service.ts
│   ├── history.service.ts
│   ├── favorite.service.ts
│   └── search.service.ts
├── hooks/
│   ├── queries/
│   │   ├── use-page.ts
│   │   ├── use-page-list.ts
│   │   ├── use-page-tree.ts
│   │   ├── use-page-blocks.ts
│   │   ├── use-page-comments.ts
│   │   ├── use-page-history.ts
│   │   ├── use-favorites.ts
│   │   └── use-docs-search.ts
│   ├── mutations/
│   │   ├── use-create-page.ts
│   │   ├── use-update-page.ts
│   │   ├── use-delete-page.ts
│   │   ├── use-create-block.ts
│   │   ├── use-update-block.ts
│   │   └── use-reorder-blocks.ts
│   ├── state/
│   │   ├── use-doc-toolbar.ts
│   │   ├── use-editor-selection.ts
│   │   └── use-slash-command.ts
│   └── index.ts
├── model/
│   ├── page-tree.ts
│   ├── block-properties.ts
│   └── editor-selectors.ts
```

Important:

- Move `features/docs/components/*` out of feature if those are route UI. Preferred destination: `app/(workspace)/[workspaceId]/docs/**/_components`.
- Keep only non-UI editor domain logic in `features/docs`.

### 5.4 Auth

Current state: mostly acceptable.

Target:

```txt
features/auth/
├── api/
│   ├── auth.service.ts
│   ├── oauth.service.ts
│   ├── auth.mapper.ts
│   └── auth.dto.ts
├── hooks/
│   ├── queries/
│   │   └── use-auth-user.ts
│   ├── mutations/
│   │   ├── use-login.ts
│   │   ├── use-register.ts
│   │   ├── use-logout.ts
│   │   ├── use-forgot-password.ts
│   │   └── use-reset-password.ts
│   └── index.ts
├── model/
│   ├── auth-session.ts
│   └── error-display.ts
```

Important:

- Remove empty `useAuth.ts` or implement it as a composed convenience hook.
- Keep token storage in `lib/auth`, not inside feature.

### 5.5 Notifications

Target:

```txt
features/notifications/
├── api/
│   ├── notification.service.ts
│   ├── notification.mapper.ts
│   └── notification.dto.ts
├── hooks/
│   ├── queries/
│   │   ├── use-notifications.ts
│   │   └── use-unread-count.ts
│   ├── mutations/
│   │   ├── use-mark-notification-read.ts
│   │   └── use-mark-all-notifications-read.ts
│   └── index.ts
└── model/
    └── query-cache.ts
```

### 5.6 Account and Theme

`account` can stay small but should still follow the same structure once it grows:

```txt
features/account/
├── api/account.service.ts
├── api/account.mapper.ts
├── hooks/mutations/use-update-profile.ts
├── schemas/
└── types/
```

`theme` is closer to shared app preference than domain feature. Keep it only if theme has business-level behavior; otherwise move generic theme helpers to `lib/theme` or `components/providers`.

---

## 6. Naming conventions

### Service files

Use resource service names:

```txt
workspace.service.ts
member.service.ts
invitation.service.ts
workspace-view.service.ts
workspace-activity.service.ts
workspace-snapshot.service.ts
board.service.ts
board-view.service.ts
card.service.ts
page.service.ts
block.service.ts
notification.service.ts
```

Avoid one service containing unrelated resources.

### Hook files

Query hooks:

```txt
use-workspace.ts
use-workspace-list.ts
use-workspace-members.ts
use-workspace-views.ts
use-full-board.ts
use-page-tree.ts
```

Mutation hooks:

```txt
use-create-workspace.ts
use-update-workspace.ts
use-create-card.ts
use-update-card.ts
use-delete-card.ts
use-create-block.ts
use-update-block.ts
```

State hooks:

```txt
use-board-table.ts
use-selected-card-panel.ts
use-table-search.ts
use-doc-toolbar.ts
use-editor-selection.ts
```

---

## 7. Query key rules

`frontend/lib/query/query-keys.ts` remains the single source of truth.

Rules:

- Every query hook must use a key from `queryKeys`.
- Mutations must invalidate exact scopes:
  - workspace update: `workspaces.detail`, `workspaces.all`, `workspaces.snapshot`
  - workspace view change: `workspaces.views`, `workspaces.snapshot`
  - board card mutation: `boards.fullBoard`, optional `cards.detail`
  - docs mutation: `pages.detail`, `pages.blocks`, `pages.tree`, `pages.list`
- Do not invalidate broad keys like `["workspaces"]` unless the mutation truly changes all workspace state.
- Cache update helpers should live in `features/<feature>/model/query-cache.ts`, not inline in every hook.

---

## 8. Refactor plan

### Phase 0 - Safety baseline

- Run `cd frontend && bun test`.
- Run `cd frontend && bun run lint`.
- Run `cd frontend && bun run build`.
- Record any existing failures before refactor.
- Add or preserve API contract tests for feature service boundaries.

### Phase 1 - Workspace service split

Files created/kept in this phase:

```txt
frontend/features/workspace/api/workspace.api.ts
frontend/features/workspace/api/members.api.ts
frontend/features/workspace/api/invitations.api.ts
frontend/features/workspace/api/views.api.ts
frontend/features/workspace/api/activity.api.ts
frontend/features/workspace/api/workspace.service.ts
frontend/features/workspace/types/dto.ts
frontend/features/workspace/utils/settings.ts
frontend/features/workspace/utils/workspace-views.ts
```

Steps:

- Move DTO types out of `workspace.service.ts`.
- Move `mapWorkspaceDto`, `mapMemberDto`, invitation mapping, and activity mapping out of the facade.
- Split workspace, member, invitation, view, and activity methods into dedicated API modules.
- Move custom view settings parse/serialize to `utils/settings.ts`.
- Move default workspace view assembly/sorting to `utils/workspace-views.ts`.
- Keep a temporary facade export:

```ts
export const workspaceService = {
  ...workspaceApi,
  ...membersApi,
  ...invitationsApi,
  ...activityApi,
  ...viewsApi,
}
```

This allows incremental migration without reintroducing feature-to-feature imports in the API layer.

Verification:

- `cd frontend && bun test`
- `cd frontend && bun run lint -- features/workspace`
- `cd frontend && bun run build`

### Phase 2 - Workspace hook folders

Steps:

- Create `hooks/queries`, `hooks/mutations`, `hooks/state`.
- Move hooks by responsibility.
- Update `features/workspace/hooks/index.ts` to re-export the new paths.
- Keep flat hook files as compatibility re-export shims while app/components migrate.
- Move optimistic cache logic from mutation hooks to `model/query-cache.ts` in a later phase if the update logic grows.

Verification:

- `cd frontend && bun run lint -- features/workspace app/(workspace) app/(dashboard)`
- `cd frontend && bun run build`

### Phase 3 - Docs service and UI cleanup

Steps:

- Split `page.service.ts` into page, breadcrumb, comment, history, favorite, search services.
- Move page/block DTO and mappers into separate files.
- Move `features/docs/components/*` to route `_components` or shared components depending on actual reuse.
- Keep `features/docs` focused on API, hooks, schemas, types, store, model, utils.

Verification:

- `cd frontend && bun run lint -- features/docs app/(workspace)/[workspaceId]/docs`
- `cd frontend && bun run build`

### Phase 4 - Boards hook/model cleanup

Steps:

- Rename API files to service naming if desired:
  - `board.api.ts` -> `board.service.ts`
  - `card.api.ts` -> `card.service.ts`
  - `column.api.ts` -> `column.service.ts`
  - `group.api.ts` -> `group.service.ts`
  - `comment.api.ts` -> `comment.service.ts`
- Extract optimistic helpers:
  - `model/optimistic-card.ts`
  - `model/full-board-cache.ts`
  - `model/board-view-config.ts`
- Move hooks into `queries`, `mutations`, `state`.
- Preserve `hooks/index.ts` exports for compatibility.

Verification:

- `cd frontend && bun test ./features/boards/utils/board-api-mappers.test.ts`
- `cd frontend && bun run lint -- features/boards app/(workspace)/[workspaceId]/boards`
- `cd frontend && bun run build`

### Phase 5 - Auth, notifications, account normalization

Steps:

- Add DTO/mapper files where missing.
- Split notification query/mutations.
- Remove or implement empty `auth/hooks/useAuth.ts`.
- Normalize hook file naming to kebab-case or keep current casing consistently. Recommendation: kebab-case for new files, compatibility re-exports for old imports.

Verification:

- `cd frontend && bun run lint -- features/auth features/notifications features/account`
- `cd frontend && bun run build`

### Phase 6 - Boundary enforcement

Steps:

- Add a lightweight import-boundary check in `features/api-contracts.test.ts`:
  - features must not import from `app`.
  - features should not import from sibling features except approved temporary allowlist.
  - app route files should not call `api.get/post` directly.
- Remove temporary workspace facade once all imports target split services.
- Update this document after the final structure is in place.

Verification:

- `cd frontend && bun test`
- `cd frontend && bun run lint`
- `cd frontend && bun run build`

---

## 9. Migration priorities

Recommended order:

1. `workspace` first because it has the highest coupling and largest service.
2. `docs` second because `page.service.ts` mixes many resources and feature UI currently leaks into `features`.
3. `boards` third because API split is already close, but hooks/model need cleanup.
4. `notifications`, `auth`, `account` last because risk and file sizes are lower.

Avoid doing all phases in one PR. Best split:

```txt
PR 1: Workspace API split + compatibility exports
PR 2: Workspace hook folders + cache helpers
PR 3: Docs API split + docs UI placement cleanup
PR 4: Boards service naming + hook/model cleanup
PR 5: Boundary tests + final docs update
```

---

## 10. Acceptance criteria

The refactor is complete when:

- No feature API file owns unrelated resources.
- `workspace.service.ts` only handles workspace CRUD or is replaced by resource services.
- Hook folders are grouped by `queries`, `mutations`, and `state`.
- DTO and mapper logic are not embedded in large services.
- `features/docs/components` is empty or intentionally documented as non-route shared editor package.
- Feature-to-feature imports are removed or explicitly allowlisted with a migration deadline.
- `bun test`, `bun run lint`, and `bun run build` pass from `frontend`.
