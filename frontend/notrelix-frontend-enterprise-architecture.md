# Notrelix Frontend Enterprise Architecture & Refactor Plan

> Scope: `frontend/` tại branch `refactor/frontend`, neo theo commit `a1396f1ab914169462a86711dff4a19e3c085c67`.
>
> Mục tiêu tài liệu: đánh giá lại tư duy/kế hoạch frontend, chốt kiến trúc mục tiêu, chuẩn hóa cấu trúc thư mục, chức năng từng thư mục, chức năng từng module trong `features/` và `app/`, đồng thời đưa ra roadmap refactor để frontend sạch, ổn định, dễ scale cho SaaS Enterprise.

---

## 1. Executive Summary

Frontend Notrelix hiện tại đã đi đúng hướng lớn: dùng Next.js App Router, TypeScript, Feature-Sliced Design, TanStack Query, Zustand, React Hook Form, Zod, Tailwind CSS, shadcn/Base UI và có tài liệu kiến trúc riêng cho frontend.

Tuy nhiên, hệ thống chưa nên được xem là “đã đóng kiến trúc”. Trạng thái đúng hơn là: **foundation đã tốt, nhưng migration chưa được đóng sổ và các boundary chưa đủ cứng để scale dài hạn**.

Các vấn đề trọng tâm:

1. `features/work-management` đã là owner chính của Boards/Items/Fields/Groups, nhưng docs vẫn còn dấu vết cũ về `features/boards`.
2. `app/(workspace)/[workspaceId]/_components` đang có nguy cơ trở thành vùng chứa shell business quá nặng.
3. `lib/api/endpoints.ts` vẫn là registry business endpoint tập trung, dễ biến `lib` thành nơi biết quá nhiều domain.
4. `lib/query/query-keys.ts` vẫn là query-key registry toàn cục cho hầu hết feature, làm cache ownership không thật sự thuộc về feature.
5. Public API của feature đã có, nhưng vẫn export hook cụ thể ra boundary khá rộng.
6. Cần chuẩn hóa lại folder contract: `api`, `query`, `mutations`, `model`, `schemas`, `ui`, `types` thay vì để `hooks` ôm quá nhiều nghĩa.
7. Cần có architecture rule tự động chặn deep import, chặn app gọi thẳng feature internals, chặn feature-to-feature import sai.

Kết luận: **không nên đập đi làm lại**, mà nên **hoàn tất refactor đang dở, sửa docs thành SSOT chính xác, và siết boundary bằng rule/test**.

---

## 2. Tự đánh giá lại kế hoạch trước đó

### 2.1. Phần đúng cần giữ

Kế hoạch trước đó đúng ở các điểm sau:

- Chọn hướng **Feature-Sliced Design + App Router composition** thay vì copy Clean Architecture backend 1:1.
- Giữ `app/` làm route/layout/composition, không để route page chứa API call, DTO mapping, mutation hoặc business decision.
- Xem `features/` là nơi sở hữu capability: API, query, mutation, schema, model, business UI, cache policy.
- Đặt `work-management` làm product capability lớn, bên trong mới có `boards`, `items`, `fields`, `groups`, `checklists`, `labels`.
- Board views như Table/Kanban/Calendar/Timeline là **presentation modes**, không phải top-level features.
- Phải có public API qua `index.ts`, tránh app hoặc feature khác import sâu vào internals.
- Phải chuẩn hóa permission UI qua capability check, không rải `role === 'admin'` trong component.

### 2.2. Phần cần sửa trong nhận định cũ

Nhận định cũ từng nói có nguy cơ duplicate `features/boards` top-level. Tại commit `a1396f1...`, điều này không còn đúng theo physical tree. `features/boards` đã không còn là module top-level active. Vấn đề thật không phải duplicate physical folder, mà là:

- Docs vẫn còn mâu thuẫn về legacy `features/boards`.
- Public API và query key vẫn còn compatibility alias.
- `work-management` đã là owner đúng, nhưng cấu trúc bên trong chưa thật sự target-state.

Do đó, priority đúng phải đổi từ “xoá duplicate board module” sang:

```txt
P0: Chốt migration state và sửa docs sai.
P0: Audit import còn sót từ legacy path.
P0: Siết app/feature boundary.
P1: Tách API/query/cache ownership về từng feature.
```

### 2.3. Phần cần cẩn trọng hơn

Không nên refactor quá cực đoan theo kiểu đổi toàn bộ trong một lần. Với frontend đang có nhiều module, nên làm theo nguyên tắc:

1. Không làm vỡ route đang chạy.
2. Không đổi folder nếu chưa có rule chặn regression.
3. Không xóa compatibility alias trước khi audit toàn bộ import.
4. Mỗi phase phải có output rõ: folder mới, public API mới, rule mới, migration xong, test pass.

---

## 3. Kiến trúc mục tiêu

### 3.1. Pattern tổng thể

Notrelix frontend nên chốt pattern:

```txt
Next.js App Router Composition
+ Feature-Sliced Design
+ Feature-owned API / Query / Mutation / Model / UI
+ Strict Public API Boundary
+ Design System primitives separated from Business UI
```

Không gọi đây là Clean Architecture frontend theo nghĩa backend. Frontend không nên có `Domain/Application/Infrastructure` y hệt backend. Frontend nên được tổ chức theo **screen, feature, state, query cache, form, view model, UI primitive**.

### 3.2. Dependency direction

```txt
app/
  -> features/* public API only
  -> components/*
  -> lib/*

features/<feature>/
  -> own internal files
  -> components/ui or components/generic
  -> lib technical utilities
  -> types shared generic only
  -> other features public API only, when truly needed

components/ui/
  -> external packages
  -> local primitive utilities only
  -> no features
  -> no app

components/generic/
  -> components/ui
  -> lib/utils only
  -> no business features

lib/
  -> external packages
  -> types shared generic only
  -> no app
  -> no features
  -> no components business
```

### 3.3. Ownership rule

Một file thuộc feature nào thì feature đó phải sở hữu đầy đủ:

- API endpoint wrapper.
- DTO type.
- Mapper DTO -> ViewModel.
- Query key.
- Query hook.
- Mutation hook.
- Cache invalidation.
- Optimistic update/rollback.
- Form schema.
- Business UI.
- Permission/entitlement presentation rule.

Nếu một logic biết `workspaceId`, `boardId`, `itemId`, `pageId`, `memberRole`, `subscriptionPlan`, `permission`, nó gần như chắc chắn **không thuộc `components/ui`**.

---

## 4. Cấu trúc thư mục frontend mục tiêu

```txt
frontend/
  app/
    layout.tsx
    providers.tsx
    error.tsx
    loading.tsx
    not-found.tsx

    (app)/
      page.tsx
      _components/

    (auth)/
      layout.tsx
      sign-in/
        page.tsx
      sign-up/
        page.tsx
      forgot-password/
        page.tsx
      reset-password/
        page.tsx
      _components/

    (dashboard)/
      home/
        page.tsx
      _components/

    (workspace)/
      [workspaceId]/
        layout.tsx
        page.tsx
        loading.tsx
        error.tsx
        _components/
          shell/
          route-tabs/
        boards/
          [boardId]/
            page.tsx
        docs/
          [pageId]/
            page.tsx
        chat/
          page.tsx
        dashboard/
          page.tsx
        settings/
          page.tsx

    invite/
      [token]/
        page.tsx

  features/
    auth/
    account/
    workspace/
    work-management/
    docs/
    collaboration/
    notifications/
    search/
    billing/
    governance/
    automation/
    integrations/
    activity/

  components/
    ui/
    feedback/
    layout/
    data-display/
    forms/
    overlay/

  lib/
    api/
    query/
    routes/
    permissions/
    errors/
    theme/
    realtime/
    telemetry/
    config/
    utils.ts

  hooks/
    use-media-query.ts
    use-is-mounted.ts

  styles/
    globals.css
    tokens.css

  i18n/
  messages/
  public/
  scripts/
  types/
  docs/
```

---

## 5. Chức năng từng thư mục cấp root

### 5.1. `app/`

`app/` là Next.js App Router layer.

Được phép chứa:

- Route segments.
- Layouts.
- `page.tsx` composition.
- Route-specific loading/error/not-found boundary.
- Server component wrapper.
- Route guard ở mức route.
- `_components` chỉ phục vụ layout/composition riêng của route.

Không được chứa:

- API call trực tiếp.
- TanStack `useQuery` business trực tiếp nếu logic thuộc feature.
- Mutation business.
- DTO mapper.
- Permission business rule.
- Entitlement rule.
- Board/docs/workspace model transformation.

Route page đúng:

```tsx
import { BoardScreen } from "@/features/work-management"

export default async function BoardPage({ params, searchParams }: PageProps) {
  const { workspaceId, boardId } = await params
  const { view } = await searchParams

  return <BoardScreen workspaceId={workspaceId} boardId={boardId} view={view} />
}
```

Route page sai:

```tsx
import { api } from "@/lib/api/api-client"
import { mapBoardDto } from "@/features/work-management/boards/model/map-board-dto"

export default function Page() {
  // ❌ route tự fetch/mapping business
}
```

### 5.2. `features/`

`features/` chứa các vertical business capability. Đây là nơi “nghiệp vụ frontend” sống.

Mỗi feature nên có contract chuẩn:

```txt
features/<feature>/
  api/          # HTTP wrappers, DTO API types
  query/        # useQuery hooks, query keys
  mutations/    # useMutation hooks, invalidation, optimistic update
  model/        # mapper, selectors, view model, permission helpers

  ui/           # business components/screens
  types/        # public/internal feature types
  mock/         # contract stubs only, not production critical path
  index.ts      # public API only
```

Tên `hooks/` có thể tồn tại trong giai đoạn transition, nhưng target-state nên tách thành `query/`, `mutations/`, `model/`, vì `hooks/` quá rộng và dễ thành nơi chứa mọi thứ.

### 5.3. `components/`

`components/` là shared UI/business-blind layer.

Các nhóm nên có:

```txt
components/ui/             # Button, Input, Dialog, Dropdown, Tabs, Tooltip
components/feedback/       # LoadingState, EmptyState, ErrorState, AccessDeniedState
components/layout/         # PageShell, SplitLayout, Container, Stack, Grid
components/data-display/   # Generic table shell, metric card, timeline shell
components/forms/          # Generic field wrapper, form error, submit button
components/overlay/        # Generic modal/sheet/drawer wrappers
```

Không đưa vào `components/` những component biết business entity.

Ví dụ không thuộc `components/`:

```txt
WorkspaceSwitcher
BoardToolbar
BoardTableView
NotificationBell
BillingPlanCard
InviteMemberDialog
```

Những component này thuộc feature tương ứng.

### 5.4. `lib/`

`lib/` là technical infrastructure, không phải business registry.

Được chứa:

```txt
lib/api/api-client.ts          # transport, CSRF, credentials, refresh lock
lib/query/query-client.ts      # TanStack QueryClient config
lib/routes/                    # route builder, no business behavior
lib/permissions/               # generic permission evaluation client helper
lib/errors/                    # AppError, error mapping
lib/theme/                     # theme infra, color mode, token persistence
lib/realtime/                  # websocket/sse client infra
lib/telemetry/                 # trackEvent, instrumentation adapter
lib/config/                    # env parsing, public runtime config
lib/utils.ts                   # cn(), date utils generic only
```

Không nên chứa:

```txt
lib/api/endpoints.ts           # nếu chứa toàn bộ business endpoints
lib/query/query-keys.ts        # nếu chứa toàn bộ business cache keys
lib/domain/*                   # frontend không có domain model global kiểu backend
```

### 5.5. `hooks/`

Chỉ chứa generic React hooks không biết business:

```txt
use-media-query
use-is-mounted
use-debounce
use-click-outside
use-local-storage
```

Không chứa:

```txt
useCurrentWorkspace
useFullBoard
useNotifications
useBillingPlan
```

Những hook này thuộc `features/<feature>/query` hoặc `features/<feature>/mutations`.

### 5.6. `styles/`

Chứa global CSS và token.

Nên có:

```txt
styles/globals.css
styles/tokens.css
styles/theme.css
```

Không import JS/TS module vào style layer.

### 5.7. `types/`

Chỉ chứa shared generic types thật sự:

```txt
ApiPage<T>
ApiResult<T>
Nullable<T>
EntityId
ISODateString
```

Không chứa business type như `Board`, `Workspace`, `Invoice`, `PageBlock`. Những type này thuộc feature.

---

## 6. Kiến trúc `app/` chi tiết

### 6.1. `app/layout.tsx`

Trách nhiệm:

- Root HTML shell.
- Global metadata.
- Font/global style import.
- Bọc `children` bằng provider nếu cần.

Không chứa business fetching.

### 6.2. `app/providers.tsx`

Trách nhiệm:

- `QueryClientProvider`.
- Theme provider.
- Toast provider.
- Global auth/session failure listener.
- Realtime provider nếu ở mức app-wide.

Cần chú ý:

- Không hardcode route string như `"/sign-in"`; dùng route registry.
- Auth failure event nên là contract typed, không dùng string event rời rạc khắp nơi.

### 6.3. `app/(app)/`

Trách nhiệm:

- Public marketing landing.
- Public product pages.
- Pricing public nếu chưa login.
- Legal/static pages.

Nên dùng:

```txt
app/(app)/_components/marketing-hero.tsx
app/(app)/_components/landing-shell.tsx
```

Nếu marketing UI không dùng lại trong product, nên để route-private tại đây thay vì `components/marketing`.

### 6.4. `app/(auth)/`

Trách nhiệm:

- Route auth public: sign-in, sign-up, forgot/reset password.
- Compose form từ `features/auth`.
- Auth layout shell.

Không chứa:

- Login API call.
- Auth mutation.
- Token handling.
- Form schema.

Các phần đó thuộc `features/auth`.

### 6.5. `app/(dashboard)/`

Trách nhiệm:

- Authenticated home/dashboard composition.
- Compose workspace list, recent boards, recent docs, activity, notification.

Không nên sở hữu domain logic. Nếu dashboard cần “recent activity”, gọi public API từ `features/activity`; nếu cần “recent boards”, gọi public API từ `features/work-management`.

### 6.6. `app/(workspace)/[workspaceId]/`

Trách nhiệm:

- Workspace route boundary.
- Workspace layout.
- Workspace shell.
- Route-private tabbed frame nếu chỉ phụ thuộc route segment.
- Compose workspace home, boards, docs, chat, settings.

Hiện tại folder có `_components`, `boards`, `chat`, `dashboard`, `docs`, `layout.tsx`, `page.tsx`, `loading.tsx`, `error.tsx`. Đây là hợp lý ở mức route, nhưng cần audit `_components` để đảm bảo nó không chứa business logic nặng.

#### Rule cho `_components`

`app/(workspace)/[workspaceId]/_components` chỉ được chứa:

```txt
shell/
  workspace-route-shell.tsx
  workspace-sidebar-frame.tsx
  workspace-content-frame.tsx

route-tabs/
  workspace-route-tabs.tsx
  workspace-tab-link.tsx

layout-only/
  split-pane.tsx
  route-panel.tsx
```

Không nên chứa:

```txt
board data fetching
board view orchestration
card mutation
docs block mutation
workspace permission decision
notification query
```

Nếu có, chuyển về feature.

### 6.7. `app/(workspace)/[workspaceId]/boards/[boardId]/page.tsx`

Trách nhiệm:

- Parse `workspaceId`, `boardId`, `searchParams.view`, `searchParams.panel`.
- Render `BoardScreen` từ `features/work-management`.

Không tạo route con cho từng board view:

```txt
❌ /[workspaceId]/boards/[boardId]/table
❌ /[workspaceId]/boards/[boardId]/kanban
```

Dùng search param:

```txt
✅ /[workspaceId]/boards/[boardId]?view=table
✅ /[workspaceId]/boards/[boardId]?view=kanban
✅ /[workspaceId]/boards/[boardId]?view=calendar
✅ /[workspaceId]/boards/[boardId]?view=timeline
```

### 6.8. `app/(workspace)/[workspaceId]/docs/[pageId]/page.tsx`

Trách nhiệm:

- Parse route params.
- Compose `DocumentEditorScreen` hoặc `DocsPageScreen` từ `features/docs`.

Docs page là resource thật, nên route riêng là hợp lý.

### 6.9. `app/invite/[token]/`

Trách nhiệm:

- Public route nhận invitation.
- Compose `InviteAcceptScreen` từ `features/workspace` hoặc `features/auth` + `features/workspace`.

Business xử lý accept invitation thuộc feature, không thuộc route page.

---

## 7. Kiến trúc `features/` tổng thể

### 7.1. Feature folder contract chuẩn

Mỗi feature nên theo cấu trúc:

```txt
features/<feature>/
  api/
    <feature>.api.ts
    <feature>.dto.ts

  query/
    <feature>.keys.ts
    use-<resource>.ts
    use-<resource>-list.ts

  mutations/
    use-create-<resource>.ts
    use-update-<resource>.ts
    use-delete-<resource>.ts

  model/
    <feature>.mapper.ts
    <feature>.selectors.ts
    <feature>.permissions.ts
    <feature>.view-model.ts

  schemas/
    create-<resource>.schema.ts
    update-<resource>.schema.ts

  ui/
    <feature>-screen.tsx
    <resource>-form.tsx
    <resource>-list.tsx
    <resource>-empty-state.tsx

  types/
    <feature>.types.ts

  mock/
    <feature>.mock.ts

  index.ts
```

### 7.2. Public API rule

`index.ts` chỉ export boundary cần cho app hoặc feature khác.

Đúng:

```ts
export { BoardScreen } from "./boards/ui/board-screen"
export type { BoardViewType } from "./boards/types/board.types"
```

Sai:

```ts
export * from "./boards/api"
export * from "./boards/ui/views/table"
export * from "./boards/model"
```

### 7.3. DTO mapping rule

Không để React component dùng raw DTO.

Luồng đúng:

```txt
API response DTO
  -> feature mapper
  -> frontend ViewModel
  -> TanStack Query cache
  -> UI component
```

Ví dụ:

```txt
BoardDtoApi -> mapBoardDto -> Board
CardDtoApi -> mapCardDto -> BoardItem
ListDtoApi -> mapListDto -> BoardGroup
```

### 7.4. Query ownership rule

Target-state nên là feature-owned query key:

```txt
features/work-management/boards/query/board.keys.ts
features/workspace/query/workspace.keys.ts
features/docs/pages/query/page.keys.ts
features/billing/query/billing.keys.ts
```

`lib/query/query-client.ts` chỉ cấu hình retry/cache global. Không nên chứa full business key registry dài hạn.

### 7.5. API ownership rule

Target-state:

```txt
lib/api/api-client.ts                    # transport only
features/auth/api/auth.api.ts            # auth endpoints
features/workspace/api/workspace.api.ts  # workspace endpoints
features/docs/pages/api/pages.api.ts     # docs endpoints
features/work-management/boards/api/boards.api.ts
```

Không nên để `lib/api/endpoints.ts` biết toàn bộ auth/users/workspaces/pages/blocks/boards/cards/lists/checklists/notifications.

---

## 8. Chi tiết từng feature module

## 8.1. `features/auth`

### Trách nhiệm

- Sign in.
- Sign up.
- Forgot password.
- Reset password.
- Logout.
- Refresh/session lifecycle.
- Current authenticated user.
- Auth form validation.
- Auth error mapping.

### Cấu trúc hiện tại quan sát được

```txt
features/auth/
  api/
  components/
  hooks/
  i18n/
  schemas/
  types/
  utils/
  index.ts
```

### Target structure

```txt
features/auth/
  api/
    auth.api.ts
    auth.dto.ts

  query/
    auth.keys.ts
    use-auth-user.ts

  mutations/
    use-login.ts
    use-register.ts
    use-logout.ts
    use-forgot-password.ts
    use-reset-password.ts

  model/
    auth.mapper.ts
    session.model.ts
    auth-error.mapper.ts

  schemas/
    login.schema.ts
    register.schema.ts
    forgot-password.schema.ts
    reset-password.schema.ts

  ui/
    login-form.tsx
    register-form.tsx
    forgot-password-form.tsx
    reset-password-form.tsx
    auth-card.tsx

  i18n/
  types/
  index.ts
```

### Public API nên export

```ts
export { LoginForm } from "./ui/login-form"
export { RegisterForm } from "./ui/register-form"
export { useAuthUser } from "./query/use-auth-user"
export { useLogout } from "./mutations/use-logout"
export type { AuthUser } from "./types/auth.types"
```

### Không nên export

- Raw `authApi` trừ khi có lý do ở composition boundary.
- DTO API raw.
- Internal mapper.
- Internal token utils.

---

## 8.2. `features/account`

### Trách nhiệm

- User profile.
- Personal preferences.
- Account security.
- Password update.
- Appearance/density settings nếu là user preference.

### Cấu trúc hiện tại quan sát được

```txt
features/account/
  api/
  hooks/
  index.ts
```

### Target structure

```txt
features/account/
  api/
    account.api.ts
    account.dto.ts

  query/
    account.keys.ts
    use-account-profile.ts
    use-account-preferences.ts

  mutations/
    use-update-profile.ts
    use-update-password.ts
    use-update-preferences.ts

  model/
    account.mapper.ts
    account.selectors.ts

  schemas/
    update-profile.schema.ts
    update-password.schema.ts
    update-preferences.schema.ts

  ui/
    account-profile-screen.tsx
    profile-form.tsx
    security-form.tsx
    preferences-form.tsx

  types/
    account.types.ts

  index.ts
```

### Boundary rule

`account` không import `work-management`, `docs`, `billing` internals. Nếu cần user profile từ auth, dùng public API `features/auth` hoặc model được truyền xuống từ app composition.

---

## 8.3. `features/workspace`

### Trách nhiệm

- Workspace lifecycle.
- Workspace switcher.
- Current workspace snapshot.
- Members.
- Roles display.
- Invitations.
- Workspace settings shell-level business.
- Workspace-aware shell components.

### Cấu trúc hiện tại quan sát được

```txt
features/workspace/
  api/
  components/
  constants/
  hooks/
  mock/
  schemas/
  types/
  utils/
  index.ts
```

### Target structure

```txt
features/workspace/
  api/
    workspace.api.ts
    workspace.dto.ts
    invitations.api.ts

  query/
    workspace.keys.ts
    use-workspace-list.ts
    use-workspace-snapshot.ts
    use-workspace-members.ts
    use-pending-invitations.ts

  mutations/
    use-create-workspace.ts
    use-update-workspace.ts
    use-invite-member.ts
    use-accept-invitation.ts
    use-remove-member.ts

  model/
    workspace.mapper.ts
    workspace.selectors.ts
    workspace-permissions.ts
    workspace-navigation.model.ts

  schemas/
    create-workspace.schema.ts
    update-workspace.schema.ts
    invite-member.schema.ts

  ui/
    workspace-switcher.tsx
    workspace-shell-header.tsx
    workspace-members-panel.tsx
    invite-member-dialog.tsx
    pending-invitations-menu.tsx
    workspace-settings-panel.tsx

  types/
    workspace.types.ts
    workspace-member.types.ts

  mock/
    workspace.mock.ts

  index.ts
```

### Boundary rule

`workspace` có thể là composition partner cho nhiều route, nhưng không được biết internal implementation của `work-management`, `docs`, `billing`, `governance`. Settings page có thể compose tabs từ feature khác thông qua public API.

---

## 8.4. `features/work-management`

### Trách nhiệm

Core capability của sản phẩm:

- Boards.
- Items/cards/tasks.
- Groups/sections/lists.
- Fields/columns/custom properties.
- Views: Table, Kanban, Calendar, Timeline.
- Checklists.
- Labels/tags.
- Board-level cache and optimistic updates.
- Board view state.

### Cấu trúc hiện tại quan sát được

```txt
features/work-management/
  boards/
  cache/
  checklists/
  fields/
  groups/
  hooks/
  items/
  labels/
  mock/
  schemas/
  shared/
  types/
  MIGRATION.md
  index.ts
```

### Nhận định

Đây là hướng đúng vì `work-management` là product area lớn. Nhưng target-state nên giảm root-level `hooks`, `schemas`, `types` nếu chúng không thật sự shared trong toàn module. Những gì thuộc board thì đặt trong `boards`, thuộc item thì đặt trong `items`.

### Target structure

```txt
features/work-management/
  index.ts

  boards/
    index.ts
    api/
      boards.api.ts
      boards.dto.ts
    query/
      board.keys.ts
      use-full-board.ts
      use-workspace-boards.ts
      use-resolved-workspace-board.ts
    mutations/
      use-create-board.ts
      use-update-board.ts
      use-delete-board.ts
      use-archive-board.ts
      use-update-board-view.ts
    model/
      board.mapper.ts
      board.selectors.ts
      board-permissions.ts
      board-view-state.ts
      board-route-state.ts
    schemas/
      create-board.schema.ts
      update-board.schema.ts
      update-board-view.schema.ts
    ui/
      board-screen.tsx
      board-toolbar.tsx
      board-empty-state.tsx
      board-loading-state.tsx
      board-access-denied-state.tsx
      views/
        table/
          board-table-view.tsx
          board-table-row.tsx
          board-table-cell.tsx
        kanban/
          board-kanban-view.tsx
          kanban-column.tsx
          kanban-card.tsx
        calendar/
          board-calendar-view.tsx
        timeline/
          board-timeline-view.tsx
    types/
      board.types.ts
      board-view.types.ts

  items/
    index.ts
    api/
    query/
    mutations/
    model/
    schemas/
    ui/
    types/

  fields/
    index.ts
    api/
    query/
    mutations/
    model/
    schemas/
    ui/
      renderers/
      editors/
    types/

  groups/
    index.ts
    api/
    query/
    mutations/
    model/
    schemas/
    ui/
    types/

  checklists/
    index.ts
    api/
    query/
    mutations/
    model/
    schemas/
    ui/
    types/

  labels/
    index.ts
    api/
    query/
    mutations/
    model/
    schemas/
    ui/
    types/

  cache/
    board-cache-updaters.ts
    optimistic-item-updates.ts
    optimistic-field-updates.ts
    board-invalidation.ts
    realtime-cache-sync.ts

  shared/
    ui/
      view-toolbar.tsx
      filter-menu.tsx
      sort-menu.tsx
    model/
      view-config.types.ts
      sort-config.types.ts
      filter-config.types.ts
    utils/
      fractional-index.ts

  mock/
    work-management.mock.ts
```

### Public API nên export

```ts
export { BoardScreen } from "./boards/ui/board-screen"
export type { Board, BoardViewType } from "./boards/types/board.types"
```

Chỉ export hook khi app thật sự cần orchestration. Nếu app chỉ render screen, không export `useFullBoard` ra root public API.

### Rule cho views

Views không phải feature riêng:

```txt
✅ features/work-management/boards/ui/views/table
✅ features/work-management/boards/ui/views/kanban
✅ features/work-management/boards/ui/views/calendar
✅ features/work-management/boards/ui/views/timeline

❌ features/table
❌ features/kanban
❌ features/work-management/views as top-level owner
```

### Route-state rule

Board view đổi bằng query param:

```txt
/[workspaceId]/boards/[boardId]?view=table
/[workspaceId]/boards/[boardId]?view=kanban
```

Không tạo route con cho view mode.

---

## 8.5. `features/docs`

### Trách nhiệm

- Document editor.
- Pages.
- Page tree.
- Blocks.
- Templates.
- Page comments if comment is doc-specific.
- Breadcrumb/history/search within docs.

### Cấu trúc hiện tại quan sát được

```txt
features/docs/
  blocks/
  comments/
  editor/
  pages/
  shared/
  templates/
  tree/
  index.ts
```

### Target structure

```txt
features/docs/
  index.ts

  pages/
    api/
      pages.api.ts
      pages.dto.ts
    query/
      page.keys.ts
      use-page.ts
      use-page-tree.ts
      use-page-breadcrumb.ts
    mutations/
      use-create-page.ts
      use-update-page-title.ts
      use-move-page.ts
      use-delete-page.ts
    model/
      page.mapper.ts
      page.selectors.ts
    schemas/
      create-page.schema.ts
      update-page.schema.ts
    ui/
      docs-page-screen.tsx
      page-title-editor.tsx
    types/

  blocks/
    api/
    query/
    mutations/
    model/
    schemas/
    ui/
      block-renderer.tsx
      block-editor.tsx
    types/

  editor/
    ui/
      document-editor.tsx
      editor-toolbar.tsx
      editor-shortcuts.tsx
    model/
      editor-state.ts
      editor-commands.ts

  tree/
    query/
    mutations/
    model/
    ui/
      page-tree-sidebar.tsx
      page-tree-item.tsx

  templates/
    api/
    query/
    mutations/
    model/
    ui/

  comments/
    # chỉ giữ nếu comments ở đây là docs-specific.
    # Nếu generic comments, chuyển về features/collaboration.

  shared/
    ui/
    model/
    utils/
```

### Boundary rule

`docs` không deep import `work-management`. Cross-link giữa docs và board nên đi qua route helper hoặc public API nhỏ, không dùng internal board model.

---

## 8.6. `features/collaboration`

### Trách nhiệm

- Comments generic.
- Mentions.
- Reactions.
- Attachments.
- Presence.
- Collaboration UI reusable across docs/boards/items.

### Target structure

```txt
features/collaboration/
  comments/
    api/
    query/
    mutations/
    model/
    schemas/
    ui/
      comment-thread.tsx
      comment-input.tsx
      comment-item.tsx
    types/

  mentions/
    api/
    query/
    model/
    ui/
      mention-list.tsx
      mention-token.tsx

  reactions/
    api/
    query/
    mutations/
    ui/
      reaction-picker.tsx
      reaction-summary.tsx

  attachments/
    api/
    query/
    mutations/
    ui/
      attachment-list.tsx
      attachment-uploader.tsx

  presence/
    realtime/
    model/
    ui/
      presence-avatar-stack.tsx

  index.ts
```

### Boundary rule

`collaboration` không được import visual component từ `work-management` hoặc `docs`. Nó nhận `resourceType/resourceId` và render generic collaboration UI.

---

## 8.7. `features/notifications`

### Trách nhiệm

- Notification bell.
- Notification list/inbox.
- Unread count.
- Mark read/read all.
- Notification stream.

### Cấu trúc hiện tại quan sát được

```txt
features/notifications/
  api/
  components/
  hooks/
  index.ts
```

### Target structure

```txt
features/notifications/
  api/
    notifications.api.ts
    notifications.dto.ts

  query/
    notification.keys.ts
    use-notifications.ts
    use-unread-count.ts

  mutations/
    use-mark-notification-read.ts
    use-mark-all-notifications-read.ts

  model/
    notification.mapper.ts
    notification-icons.ts
    notification-routing.ts

  ui/
    notification-bell.tsx
    notification-list.tsx
    notification-item.tsx
    notification-popover.tsx
    unread-badge.tsx

  types/
    notification.types.ts

  index.ts
```

### Boundary rule

`notifications` không biết internals của board/docs. Nếu notification click cần đi tới board/page, dùng route registry hoặc notification routing mapper.

---

## 8.8. `features/search`

### Trách nhiệm

- Global search.
- Command palette.
- Quick actions.
- Recent searches.

### Target structure

```txt
features/search/
  api/
    search.api.ts
    search.dto.ts

  query/
    search.keys.ts
    use-global-search.ts
    use-recent-searches.ts

  mutations/
    use-save-recent-search.ts

  model/
    search.mapper.ts
    search-result-routing.ts
    command-registry.ts

  ui/
    global-search-dialog.tsx
    command-palette.tsx
    search-result-list.tsx
    quick-action-item.tsx

  types/
    search.types.ts

  index.ts
```

### Boundary rule

Search có thể đọc public metadata từ các feature, nhưng không gọi write mutation của feature khác. Quick action write phải được thiết kế qua command contract rõ ràng.

---

## 8.9. `features/billing`

### Trách nhiệm

- Subscription details.
- Plan/pricing matrix.
- Invoices.
- Payment methods.
- Entitlement UI.
- Feature locks/upgrade prompts.

### Cấu trúc hiện tại quan sát được

```txt
features/billing/
  api/
  entitlements/
  model/
  index.ts
```

### Target structure

```txt
features/billing/
  api/
    billing.api.ts
    billing.dto.ts

  query/
    billing.keys.ts
    use-subscription.ts
    use-invoices.ts
    use-entitlements.ts
    use-usage.ts

  mutations/
    use-change-plan.ts
    use-cancel-subscription.ts
    use-create-payment-intent.ts

  model/
    billing.mapper.ts
    plan.model.ts
    entitlement.model.ts
    usage-limit.model.ts

  entitlements/
    query/
    model/
      entitlement-checks.ts
    ui/
      entitlement-lock.tsx
      upgrade-prompt.tsx

  ui/
    billing-settings-panel.tsx
    pricing-matrix.tsx
    subscription-card.tsx
    invoice-history.tsx
    payment-methods-list.tsx

  types/
    billing.types.ts

  index.ts
```

### Boundary rule

Feature khác không tự kiểm tra `plan === 'free'`. Dùng public guard/hook từ billing nếu cần UI entitlement presentation.

---

## 8.10. `features/governance`

### Trách nhiệm

- Permission settings.
- Role matrix.
- Audit logs UI.
- Security policy UI.
- Resource permission presentation.

### Target structure

```txt
features/governance/
  api/
    governance.api.ts
    governance.dto.ts

  query/
    governance.keys.ts
    use-roles.ts
    use-permissions.ts
    use-audit-logs.ts

  mutations/
    use-update-role-permissions.ts
    use-create-role.ts
    use-delete-role.ts

  model/
    permission.mapper.ts
    role.mapper.ts
    audit-log.mapper.ts
    permission-policy.model.ts

  schemas/
    role.schema.ts
    permission-policy.schema.ts

  ui/
    governance-settings-panel.tsx
    permission-matrix.tsx
    role-editor.tsx
    audit-log-table.tsx
    security-policy-form.tsx

  types/
    governance.types.ts

  index.ts
```

### Boundary rule

`governance` không import domain logic từ `docs` hoặc `work-management`. Nó làm permission UI, không trở thành nơi chứa business behavior của resource.

---

## 8.11. `features/automation`

### Trách nhiệm

- Rule builder.
- Triggers.
- Conditions.
- Actions.
- Execution logs.
- Automation settings.

### Target structure

```txt
features/automation/
  api/
    automation.api.ts
    automation.dto.ts

  query/
    automation.keys.ts
    use-automation-rules.ts
    use-execution-logs.ts

  mutations/
    use-create-rule.ts
    use-update-rule.ts
    use-enable-rule.ts
    use-disable-rule.ts
    use-delete-rule.ts

  model/
    automation.mapper.ts
    rule-builder.model.ts
    trigger-catalog.ts
    action-catalog.ts

  schemas/
    automation-rule.schema.ts
    trigger.schema.ts
    action.schema.ts

  ui/
    automation-settings-panel.tsx
    rule-builder.tsx
    trigger-selector.tsx
    condition-builder.tsx
    action-composer.tsx
    execution-history-list.tsx

  types/
    automation.types.ts

  index.ts
```

### Boundary rule

Automation không deep import board/docs internals. Nếu automation cần chọn resource, tạo resource picker contract qua public API hoặc generic resource selector.

---

## 8.12. `features/integrations`

### Trách nhiệm

- Integration catalog.
- Webhook manager.
- Third-party connections.
- Connection status.
- Integration settings.

### Target structure

```txt
features/integrations/
  api/
    integrations.api.ts
    integrations.dto.ts
    webhooks.api.ts

  query/
    integrations.keys.ts
    use-connections.ts
    use-webhooks.ts
    use-integration-catalog.ts

  mutations/
    use-create-webhook.ts
    use-delete-webhook.ts
    use-connect-provider.ts
    use-disconnect-provider.ts

  model/
    integration.mapper.ts
    connection-status.model.ts
    provider-catalog.ts

  schemas/
    webhook.schema.ts
    provider-connection.schema.ts

  ui/
    integrations-settings-panel.tsx
    integrations-catalog.tsx
    connection-status-card.tsx
    webhook-manager.tsx
    webhook-form.tsx

  types/
    integrations.types.ts

  index.ts
```

### Boundary rule

Không import query client config trực tiếp. Không import internal API của feature khác.

---

## 8.13. `features/activity`

### Trách nhiệm

- Workspace activity feed.
- Activity item rendering.
- Live activity stream.
- Audit-like feed nếu ở mức product feed, không phải governance audit policy.

### Cấu trúc hiện tại quan sát được

```txt
features/activity/
  feed/
  index.ts
```

### Target structure

```txt
features/activity/
  feed/
    api/
      activity.api.ts
      activity.dto.ts
    query/
      activity.keys.ts
      use-workspace-activity.ts
    model/
      activity.mapper.ts
      activity-icon.mapper.ts
      activity-resource-routing.ts
    ui/
      activity-feed.tsx
      activity-feed-item.tsx
      activity-feed-skeleton.tsx
    types/
      activity.types.ts

  index.ts
```

### Boundary rule

Activity feed không import edit mutations từ other features. Click navigation dùng route registry.

---

## 9. Components architecture

### 9.1. `components/ui`

Chỉ chứa primitives:

```txt
button.tsx
input.tsx
textarea.tsx
select.tsx
dialog.tsx
sheet.tsx
dropdown-menu.tsx
tabs.tsx
tooltip.tsx
badge.tsx
avatar.tsx
separator.tsx
skeleton.tsx
```

Rules:

- Không import `features/*`.
- Không import `app/*`.
- Không import `lib/permissions`.
- Không biết `workspace`, `board`, `page`, `billing`.

### 9.2. `components/feedback`

Generic UX states:

```txt
loading-state.tsx
empty-state.tsx
error-state.tsx
access-denied-state.tsx
not-found-state.tsx
mock-disabled-state.tsx
```

Các component này nhận title/description/action qua props, không hardcode business text.

### 9.3. `components/layout`

Business-blind layout blocks:

```txt
page-shell.tsx
page-header.tsx
content-container.tsx
split-pane.tsx
sidebar-frame.tsx
responsive-grid.tsx
```

Không chứa `WorkspaceSidebar` nếu sidebar biết workspace/nav business. Cái đó thuộc `features/workspace` hoặc `app/(workspace)/_components/shell` tùy mức route-specific.

### 9.4. `components/marketing`

Hiện repo có `components/marketing`. Về target-state cần quyết định:

- Nếu chỉ dùng ở landing route: chuyển vào `app/(app)/_components`.
- Nếu marketing là module có nhiều trang public dùng lại: tạo `features/marketing` hoặc `app/(app)/_components/marketing`.
- Không để marketing component nằm trong shared nếu nó không thật sự generic.

---

## 10. Lib architecture

### 10.1. `lib/api`

Target:

```txt
lib/api/
  api-client.ts
  csrf.ts
  request-id.ts
  api-error.ts
  problem-details.ts
```

Chỉ làm transport:

- Base URL.
- Credentials.
- CSRF.
- Refresh token lock.
- Request ID.
- JSON parse.
- AppError mapping.
- Retry policy nếu ở transport level.

Không chứa toàn bộ business endpoints dài hạn.

### 10.2. `lib/query`

Target:

```txt
lib/query/
  query-client.ts
  query-error-policy.ts
  query-utils.ts
```

Không chứa full business query key registry dài hạn. Query keys thuộc feature.

### 10.3. `lib/routes`

Target:

```txt
lib/routes/
  index.ts
  routes.ts
  route-params.ts
```

Route builder là technical helper nhưng phải typed.

Ví dụ:

```ts
routes.workspace.board({ workspaceId, boardId, view: "kanban" })
```

Không hardcode:

```tsx
<Link href={`/${workspaceId}/boards/${boardId}?view=kanban`} />
```

### 10.4. `lib/permissions`

Trách nhiệm:

- Generic permission evaluation helper.
- `useCan`.
- `PermissionGate` generic.

Không chứa rule cụ thể như “board owner được archive board”. Rule source of truth vẫn là backend. Frontend chỉ presentation guard.

### 10.5. `lib/theme`

Theme là UI infrastructure, không phải product feature, trừ khi có một màn hình settings cho user đổi theme. Ngay cả khi user đổi theme trong account settings, state/persistence infra vẫn ở `lib/theme`; form/settings UI thuộc `features/account`.

### 10.6. `lib/realtime`

Chỉ chứa SSE/WebSocket client generic:

```txt
realtime-client.ts
realtime-event.types.ts
use-realtime-connection.ts
```

Feature tự đăng ký event mapping và cache update của mình.

---

## 11. Data-fetching, cache và mutation pattern

### 11.1. Query pattern

```ts
export function useFullBoard(boardId: string) {
  return useQuery({
    queryKey: boardKeys.full(boardId),
    queryFn: async () => {
      const dto = await boardsApi.getFullBoard(boardId)
      return mapFullBoardDto(dto)
    },
  })
}
```

Không để UI gọi `api.get` trực tiếp.

### 11.2. Mutation pattern

```ts
export function useUpdateBoard(boardId: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: boardsApi.updateBoard,
    onSuccess: (dto) => {
      const board = mapBoardDto(dto)
      queryClient.setQueryData(boardKeys.detail(boardId), board)
      queryClient.invalidateQueries({ queryKey: boardKeys.full(boardId) })
    },
  })
}
```

### 11.3. Optimistic update rule

Mọi optimistic update phải có rollback rõ:

```txt
onMutate -> snapshot previous cache
onError  -> restore previous cache
onSettled -> targeted invalidate
```

Không dùng global refetch bừa bãi cho high-frequency interactions như drag/drop, inline cell edit.

### 11.4. Cache ownership

- Board cache do `features/work-management/cache` hoặc `features/work-management/boards/cache` quản.
- Docs cache do `features/docs` quản.
- Notifications cache do `features/notifications` quản.
- Workspace cache do `features/workspace` quản.

Không để `app` tự invalidate business cache trừ global session/logout boundary.

---

## 12. Form pattern

Mỗi form gồm:

```txt
schema -> form component -> mutation hook -> API -> server error mapping
```

Ví dụ:

```txt
features/workspace/schemas/invite-member.schema.ts
features/workspace/ui/invite-member-dialog.tsx
features/workspace/mutations/use-invite-member.ts
features/workspace/api/workspace.api.ts
```

Rules:

- Dùng React Hook Form + Zod cho form phức tạp.
- Server validation error 400/422 phải map vào field error.
- Submit button disable khi pending.
- Không double-toast nếu form đã hiển thị error.

---

## 13. Permission & entitlement pattern

### 13.1. Permission

Frontend permission chỉ là presentation guard:

```tsx
const canArchive = useCan("board.archive", { workspaceId, boardId })

<Button disabled={!canArchive}>Archive</Button>
```

Backend vẫn enforce thật.

Không viết:

```tsx
if (member.role === "Owner" || member.role === "Admin")
```

### 13.2. Entitlement

Feature tier/plan UI dùng billing public API:

```tsx
const enabled = useEntitlement("automation.rules")
```

Không viết:

```tsx
if (subscription.plan === "free")
```

---

## 14. Routing & navigation rules

### 14.1. Resource route vs presentation state

Resource thật nên có route riêng:

```txt
/[workspaceId]/boards/[boardId]
/[workspaceId]/docs/[pageId]
/[workspaceId]/items/[itemId]
```

Presentation mode dùng query param:

```txt
?view=table
?view=kanban
?view=calendar
?view=timeline
```

### 14.2. Route registry

Mọi URL điều hướng nên đi qua `lib/routes`.

Không hardcode route string trong component.

### 14.3. Route-private components

Component chỉ dùng cho route group đặt trong:

```txt
app/<route>/_components
```

Nhưng nếu nó biết business và dùng lại được ngoài route đó, đưa vào feature.

---

## 15. Import boundary rules cần tự động hóa

### 15.1. Rule bắt buộc

```txt
1. app/** không được import feature internals.
2. app/** chỉ import từ features/<feature>/index.ts hoặc alias public API.
3. features/A không được import features/B/internal-path.
4. features/A chỉ được import features/B qua public API.
5. components/ui không được import features, app, lib business.
6. components/feedback/layout/forms không được import features.
7. lib không được import app, features, components business.
8. Không export * ở feature public API.
9. Không inline queryKey array trong useQuery/useMutation.
10. Không hardcode route string trong Link/router.push.
```

### 15.2. Suggested arch-check scripts

Nên có script kiểm tra:

```txt
scripts/architecture/check-import-boundaries.ts
scripts/architecture/check-no-deep-feature-import.ts
scripts/architecture/check-no-export-star.ts
scripts/architecture/check-no-hardcoded-routes.ts
scripts/architecture/check-no-inline-query-keys.ts
scripts/architecture/check-no-lib-business-endpoints.ts
```

### 15.3. CI quality gate

```txt
bun run type-check
bun run lint
bun run test
bun run arch-check
bun run build
```

PR fail nếu vi phạm architecture rule.

---

## 16. Refactor roadmap

## Phase 0 — Chốt SSOT tài liệu

Mục tiêu: docs không mâu thuẫn với code.

Tasks:

1. Cập nhật `frontend/ARCHITECTURE.md` để bỏ câu “legacy features/boards compatibility layer” nếu physical folder đã xóa.
2. Cập nhật hoặc xóa `features/work-management/MIGRATION.md` nếu không còn đúng.
3. Ghi rõ `features/work-management` là canonical owner.
4. Ghi rõ `features/boards` bị cấm tạo lại.
5. Ghi ADR: `WorkManagement owns Boards/Items/Fields/Groups/Views`.

Output:

```txt
frontend/docs/architecture/frontend-architecture.md
frontend/docs/architecture/frontend-boundary-rules.md
frontend/docs/architecture/frontend-refactor-roadmap.md
```

## Phase 1 — Siết public API và import boundary

Tasks:

1. Audit mọi import từ `features/work-management/...` trong `app`.
2. App chỉ import `features/work-management` root public API.
3. Tách public API cấp root và submodule.
4. Thêm arch-check chặn deep import.
5. Cấm `export *` trong feature index.

Output:

```txt
features/work-management/index.ts          # screen-level public API
features/work-management/boards/index.ts   # board capability public API
```

## Phase 2 — Dọn `app/(workspace)/[workspaceId]/_components`

Tasks:

1. Phân loại từng component:
   - route shell,
   - route composition,
   - business UI,
   - feature screen.
2. Chuyển board-specific UI về `features/work-management`.
3. Chuyển docs-specific UI về `features/docs`.
4. Chuyển workspace-aware reusable UI về `features/workspace`.
5. Giữ lại trong app chỉ shell/layout composition.

Output target:

```txt
app/(workspace)/[workspaceId]/_components/shell
app/(workspace)/[workspaceId]/_components/route-tabs
```

## Phase 3 — Feature-owned API

Tasks:

1. Tạo API files trong từng feature.
2. Move endpoint functions từ `lib/api/endpoints.ts` sang feature API.
3. Giữ compatibility wrapper tạm thời nếu cần.
4. Sau khi audit import, xóa central business endpoints.

Output:

```txt
features/auth/api/auth.api.ts
features/workspace/api/workspace.api.ts
features/docs/pages/api/pages.api.ts
features/work-management/boards/api/boards.api.ts
features/notifications/api/notifications.api.ts
```

## Phase 4 — Feature-owned query keys

Tasks:

1. Tạo query key factory trong feature.
2. Move `queryKeys.workManagement` vào `work-management`.
3. Move `queryKeys.docs` vào `docs`.
4. Move `queryKeys.billing`, `governance`, `automation`, `integrations` vào feature tương ứng.
5. Giữ `lib/query/query-client.ts` là global infrastructure.

Output:

```txt
features/work-management/boards/query/board.keys.ts
features/docs/pages/query/page.keys.ts
features/workspace/query/workspace.keys.ts
```

## Phase 5 — Rename `hooks` thành `query/mutations/model`

Tasks:

1. `hooks/queries` -> `query`.
2. `hooks/mutations` -> `mutations`.
3. Non-query hooks chuyển vào `model` hoặc `ui` tùy bản chất.
4. Update import qua public API.

Output:

```txt
features/<feature>/query
features/<feature>/mutations
features/<feature>/model
```

## Phase 6 — Chuẩn hóa mock/stub policy

Tasks:

1. Mock chỉ nằm trong `mock/`.
2. Không mock critical path đã có backend endpoint.
3. Mock phải có flag disable.
4. Production build không phụ thuộc mock.

## Phase 7 — Testing & architecture enforcement

Tasks:

1. Mapper unit tests.
2. Query/mutation tests với mock API adapter.
3. Component tests cho feature screen critical.
4. Playwright smoke: login, workspace switcher, board view switch, docs editor, notification popover, access denied.
5. Arch-check chạy trong CI.

---

## 17. Decision records cần tạo

Nên tạo ADR trong `frontend/docs/architecture/adr/`:

```txt
0001-use-fsd-not-clean-architecture-frontend.md
0002-work-management-owns-boards-items-fields-groups.md
0003-board-views-are-query-param-presentation-state.md
0004-feature-owned-api-and-query-keys.md
0005-app-router-pages-are-composition-only.md
0006-theme-is-lib-infrastructure-not-feature.md
0007-no-deep-feature-imports.md
0008-public-api-index-policy.md
```

---

## 18. Final target checklist

Một frontend module được xem là sạch khi đạt checklist:

```txt
[ ] Có owner feature rõ.
[ ] Có api/query/mutations/model/schemas/ui/types rõ ràng.
[ ] Không để route page gọi API trực tiếp.
[ ] Không dùng raw DTO trong component.
[ ] Query key thuộc feature.
[ ] Mutation có invalidation rõ.
[ ] Optimistic update có rollback.
[ ] Permission dùng useCan/guard, không raw role check.
[ ] Entitlement dùng billing guard, không raw plan check.
[ ] UI primitive không biết business.
[ ] Feature không deep import feature khác.
[ ] Public API không export tràn lan.
[ ] Có loading/error/empty/access-denied state.
[ ] Có test mapper hoặc hook critical.
[ ] Pass arch-check trong CI.
```

---

## 19. Kết luận

Frontend Notrelix hiện tại không đi sai hướng. Vấn đề là kiến trúc đang ở trạng thái “đúng hướng nhưng chưa khóa lại”. Nếu tiếp tục thêm module khi chưa siết boundary, hệ thống sẽ nhanh chóng rơi vào tình trạng:

```txt
app chứa business shell
lib chứa registry domain toàn cục
features import chéo internals
hooks trở thành thư mục chứa mọi thứ
query cache không có owner rõ
UI shared bị nhiễm business
```

Hướng chuẩn là:

```txt
app = route/layout/composition only
features = business capability owner
components = business-blind reusable UI
lib = technical infrastructure only
styles = design tokens/global style only
public API = cách duy nhất để đi qua boundary
```

Ưu tiên triển khai ngay:

```txt
P0: Sửa docs/SSOT và xóa dấu vết legacy sai.
P0: Audit app _components và chuyển business về feature.
P0: Thêm arch-check chặn deep import.
P1: Tách API endpoint ownership về feature.
P1: Tách query key ownership về feature.
P1: Chuẩn hóa hooks -> query/mutations/model.
P2: Dọn mock, marketing components, route registry alias.
P2: Thêm test và E2E smoke cho flow critical.
```

Nếu làm đúng roadmap này, frontend Notrelix sẽ có nền tảng sạch cho SaaS Enterprise: module rõ, ownership rõ, ít refactor lặp lại, dễ mở rộng thêm billing/governance/automation/integrations mà không làm vỡ Work Management hoặc Workspace shell.
