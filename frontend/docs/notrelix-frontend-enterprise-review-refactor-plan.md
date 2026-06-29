# Notrelix Frontend — Enterprise Review & Refactor Plan

> Version: 2026-06-26  
> Scope: `frontend/` của Notrelix  
> Perspective: Frontend Tech Lead / Enterprise SaaS Architecture  
> Goal: Đánh giá khách quan hiện trạng frontend và đưa ra hướng refactor có kiểm soát, tránh lệch kiến trúc khi hệ thống mở rộng.

---

## 1. Executive Summary

Frontend Notrelix đã có nền móng tốt cho một SaaS/product workspace lớn: Next.js App Router, TypeScript strict, TanStack Query, Zustand, React Hook Form, Zod, Tailwind CSS 4, shadcn/Base UI, route groups, feature modules và query key factory.

Tuy nhiên, hệ thống hiện tại chưa nên xem là “enterprise-ready” ở frontend. Nó đang ở trạng thái:

```txt
Good foundation, incomplete architecture hardening.
```

Các vấn đề chính không nằm ở việc chọn sai stack, mà nằm ở:

- Boundary giữa `app`, `features`, `components`, `lib` chưa được chốt cứng.
- Business UI vẫn có nguy cơ bị đặt quá nhiều trong `app/**/_components` hoặc `components/` chung.
- Một số feature còn có service/API file ôm nhiều trách nhiệm.
- Hook mutation có nguy cơ trộn API call, optimistic update, toast, cache update và rollback.
- `boards` đang là feature riêng, nhưng về domain dài hạn nên được nâng thành `work-management`.
- Auth/API client/query client cần hardening cho production.
- Testing, boundary enforcement và quality gate còn thiếu so với chuẩn enterprise.
- Tài liệu frontend cũ có phần lệch với code thật, gây rủi ro lớn khi dùng AI coding agent hoặc team mở rộng.

Đánh giá tổng thể hiện tại:

| Nhóm | Điểm | Nhận xét |
|---|---:|---|
| Stack lựa chọn | 8.5/10 | Hiện đại, phù hợp SaaS workspace |
| Folder direction | 7/10 | Đã có `app`, `features`, `components`, `lib` nhưng chưa chốt rule đủ cứng |
| Feature ownership | 6.5/10 | Có feature modules nhưng UI/business logic còn cần phân ranh rõ |
| Data fetching | 7/10 | Có TanStack Query + query keys, nhưng QueryClient chưa đủ policy |
| API/Auth | 6/10 | Có wrapper và refresh, nhưng thiếu refresh mutex/CSRF/session hardening |
| Design system | 8/10 | Token/UI foundation tốt, cần giữ primitive/business UI boundary |
| Testing/quality gate | 4.5/10 | Thiếu test scripts/type-check/e2e/boundary tests chuẩn |
| Enterprise readiness | 6.5/10 | Nền tốt, cần hardening + refactor theo phase |

Kết luận: **không cần đập đi làm lại**. Nên refactor theo hướng enterprise bằng các phase nhỏ, giữ hệ thống chạy được sau mỗi phase.

---

## 2. Hiện trạng kiến trúc frontend

Cấu trúc hiện tại theo repo:

```txt
frontend/
  app/                  # Next.js App Router
    (app)/
    (auth)/
    (dashboard)/
    (workspace)/
    invite/[token]/
    layout.tsx
    providers.tsx

  components/           # shared components / UI primitives / providers
  features/             # feature modules hiện tại
    account/
    auth/
    boards/
    docs/
    notifications/
    theme/
    workspace/

  hooks/                # global hooks only
  i18n/
  lib/
    api/
    auth/
    query/
    utils.ts
  messages/
  public/
  styles/
  types/
```

Stack hiện tại:

```txt
Next.js 16 App Router
React 19
TypeScript
Tailwind CSS 4
Base UI / shadcn/ui
TanStack Query v5
Zustand
React Hook Form
Zod
next-intl
next-themes
Recharts
Dnd Kit
```

Các điểm đang làm tốt:

- App Router đã dùng route groups rõ: `(app)`, `(auth)`, `(dashboard)`, `(workspace)`.
- Có `features/` để chứa domain/product modules.
- Có `lib/api` cho API client và endpoint constants.
- Có `lib/query/query-keys.ts` làm query key factory tập trung.
- Có `components/ui`/shared components cho design primitives.
- Có Tailwind/OKLCH token system và design system foundation.
- Product direction đúng: board views là view trên cùng work data, không phải data model riêng.

Các điểm chưa ổn:

- `frontend/README.md` vẫn còn dấu vết create-next-app và mô tả cấu trúc cũ như `src/`, `(main)`, `/login`, `/register`, trong khi code thật dùng `frontend/app`, `(auth)`, `(dashboard)`, `(workspace)`, `/sign-in`, `/sign-up`.
- Một số tài liệu cũ còn khuyến nghị feature không chứa UI, nhưng với frontend enterprise, feature nên chứa business UI thuộc feature.
- `app/**/_components` đang đúng với Next.js, nhưng cần giới hạn ở route-private composition, không để business UI lớn bị khóa vào route.
- `features/boards` nên được nâng thành `features/work-management` nếu muốn align với backend bounded context và product scope dài hạn.
- API client có retry refresh token nhưng cần refresh mutex để tránh refresh storm khi nhiều request cùng 401.
- QueryProvider đang tạo QueryClient đơn giản, chưa có default staleTime, retry policy, gcTime, error policy.
- `package.json` thiếu `type-check`, `test`, `test:e2e`, `quality` scripts.

---

## 3. Đánh giá theo layer

## 3.1. `app/` layer

Vai trò đúng:

```txt
app/ = routing, layouts, page composition, route-private UI.
```

Điểm tốt:

- Route groups đã đúng hướng.
- `_components` trong route là pattern hợp lý của App Router.
- Có thể dùng layout-level composition để ghép nhiều feature.

Vấn đề:

- Nếu business UI như `BoardShell`, `CardDetail`, `WorkspaceSwitcher`, `NotificationBell`, `InviteMemberDialog`, `PageEditor` nằm quá nhiều trong `app/**/_components`, hệ thống sẽ bị route-coupled.
- Page/component trong `app` không nên gọi API client trực tiếp.
- App layer không nên chứa business rules, mapper, cache updater, permission evaluator.

Rule cần chốt:

```txt
app/**/page.tsx
  = compose screen only

app/**/layout.tsx
  = compose layout, providers, shell

app/**/_components
  = route-private composition components only

app layer được import features public API, components, lib.
app layer không chứa API implementation, mapper, domain state, business mutation logic.
```

Ví dụ đúng:

```tsx
// app/(workspace)/[workspaceId]/boards/[boardId]/page.tsx
import { BoardScreen } from "@/features/work-management"

export default function BoardPage() {
  return <BoardScreen />
}
```

Ví dụ không nên:

```tsx
// app/(workspace)/[workspaceId]/boards/[boardId]/page.tsx
import { api } from "@/lib/api"

// Không viết fetch + map + permission + optimistic logic trực tiếp trong page.
```

---

## 3.2. `features/` layer

Vai trò đúng:

```txt
features/ = product/business modules aligned with bounded context and user experience.
```

Feature không chỉ chứa logic. Feature nên chứa cả:

```txt
api/
hooks/
model/
cache/
schemas/
types/
components/
utils/
index.ts
```

Điểm quan trọng: **business UI thuộc feature nên nằm trong feature**.

Ví dụ:

```txt
features/workspace/components/workspace-switcher.tsx
features/workspace/components/invite-member-dialog.tsx
features/work-management/components/board-screen.tsx
features/work-management/components/kanban-view.tsx
features/docs/components/page-editor.tsx
features/notifications/components/notification-bell.tsx
```

Không nên để các component trên trong `components/ui` vì chúng biết domain.

Không nên để tất cả trong `app/**/_components` vì chúng sẽ bị route-coupled và khó tái sử dụng.

---

## 3.3. `components/` layer

Vai trò đúng:

```txt
components/ui       = primitive UI, không biết business
components/layout   = generic shell/layout primitives
components/marketing = marketing sections / landing reusable UI
components/feedback = generic error/empty/loading states
```

Không được để business UI vào `components/ui`.

Ví dụ đúng:

```txt
components/ui/button.tsx
components/ui/dialog.tsx
components/ui/input.tsx
components/ui/dropdown-menu.tsx
components/ui/table.tsx
components/layout/app-shell.tsx
components/feedback/empty-state.tsx
```

Ví dụ sai:

```txt
components/ui/workspace-switcher.tsx
components/ui/board-card.tsx
components/ui/notification-bell.tsx
components/ui/member-role-select.tsx
```

Vì các component này biết nghiệp vụ.

---

## 3.4. `lib/` layer

Vai trò đúng:

```txt
lib/ = frontend infrastructure, framework glue, generic utilities.
```

Nên có:

```txt
lib/api/
lib/query/
lib/auth/
lib/routes/
lib/errors/
lib/permissions/
lib/realtime/
lib/config/
lib/telemetry/
```

Rule:

```txt
lib không được import features.
lib không biết Workspace, Board, Card, Page, Notification business component.
lib có thể chứa generic type/helper, API client, query client, routes builder, permission evaluator generic.
```

---

## 4. Vấn đề kiến trúc cần xử lý

## 4.1. Business UI placement chưa được chuẩn hóa

Hiện tại `app/**/_components` là hợp lý cho route-private UI. Nhưng cần chốt rõ:

```txt
app/**/_components
= route composition only

features/**/components
= business UI owned by feature
```

Nếu không, hệ thống sẽ gặp các lỗi sau:

- Business UI bị khóa vào route cụ thể.
- Không tái sử dụng được component ở dashboard, modal, command palette, notification popover.
- Logic feature bị phân tán giữa `app` và `features`.
- Developer/AI agent không biết sửa ở đâu.
- `features/` chỉ còn api/hooks/types, mất vertical ownership.

Khuyến nghị:

- Giữ `AuthShell`, `AuthVisual`, `DashboardHomeScreen`, `WorkspaceHomeScreen` trong `app/**/_components` nếu chúng chỉ compose layout/sections.
- Chuyển form/use-case/business UI vào feature.

Ví dụ:

```txt
app/(auth)/_components/auth-shell.tsx                  # giữ ở app
features/auth/components/sign-in-form.tsx              # đưa vào feature
features/auth/components/sign-up-form.tsx              # đưa vào feature

app/(workspace)/[workspaceId]/_components/home.tsx     # composition đa feature
features/workspace/components/workspace-switcher.tsx   # business UI
features/work-management/components/recent-boards.tsx  # business UI
features/docs/components/recent-pages.tsx              # business UI
```

---

## 4.2. `boards` nên tiến hóa thành `work-management`

Hiện tại có `features/boards`. Giai đoạn đầu như vậy ổn. Nhưng với Notrelix, board chỉ là một phần của Work Management.

Work Management dài hạn sẽ gồm:

```txt
board
board item / card
field
group
view
checklist
label
status
assignment
timeline
calendar
kanban
table
form view
automation trigger surface
```

Nếu để `features/boards` phình to, nó sẽ trở thành bounded context trá hình.

Khuyến nghị:

```txt
features/boards
  -> features/work-management
```

Cấu trúc đích:

```txt
features/work-management/
  api/
    board.api.ts
    board-item.api.ts
    board-field.api.ts
    board-view.api.ts
    group.api.ts
    checklist.api.ts
    label.api.ts

  hooks/
    queries/
    mutations/
    state/

  model/
    board.mapper.ts
    board-item.mapper.ts
    board-view.mapper.ts
    selectors.ts

  cache/
    board-cache-updaters.ts
    optimistic-item.ts
    invalidation.ts

  components/
    board-screen/
    board-shell/
    board-toolbar/
    table-view/
    kanban-view/
    calendar-view/
    timeline-view/
    card-detail/
    field-renderers/

  schemas/
  types/
  utils/
  index.ts
```

Không cần đổi ngay toàn bộ trong một PR. Có thể làm compatibility exports:

```ts
// features/boards/index.ts
export * from "@/features/work-management"
```

Sau đó migration dần import paths.

---

## 4.3. API client cần hardening

Hiện API client có `fetch`, `credentials: include`, parse JSON, `ApiError`, và retry refresh token khi 401. Đây là nền đúng.

Nhưng cần bổ sung:

- Refresh mutex để tránh nhiều request cùng refresh.
- CSRF strategy nếu backend dùng cookie auth.
- Correlation/request id nếu backend hỗ trợ.
- Chuẩn hóa base URL: browser nên gọi same-origin `/api/v1`, Next rewrite sang backend.
- Không để lúc thì gọi `NEXT_PUBLIC_API_URL`, lúc thì đi qua rewrite không thống nhất.

Pattern đề xuất:

```ts
let refreshPromise: Promise<void> | null = null

async function refreshOnce() {
  if (!refreshPromise) {
    refreshPromise = doRefresh().finally(() => {
      refreshPromise = null
    })
  }

  return refreshPromise
}
```

API config đề xuất:

```env
NEXT_PUBLIC_API_BASE=/api/v1
BACKEND_ORIGIN=http://localhost:8000
```

Browser gọi:

```txt
/api/v1/...
```

Next rewrite/proxy:

```txt
/api/:path* -> BACKEND_ORIGIN/api/:path*
```

---

## 4.4. QueryClient cần policy mặc định

Hiện `QueryClient` được tạo đơn giản. Enterprise frontend cần default options.

Đề xuất:

```ts
export function createQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: {
        staleTime: 30_000,
        gcTime: 5 * 60_000,
        refetchOnWindowFocus: false,
        retry: (failureCount, error) => {
          if (isNonRetryableApiError(error)) return false
          return failureCount < 2
        },
      },
      mutations: {
        retry: false,
      },
    },
  })
}
```

Rule:

```txt
Query hooks dùng queryKeys factory.
Mutation hooks invalidate đúng scope.
Optimistic update phải có rollback.
Không invalidate broad key nếu không cần.
Không lưu server state vào Zustand.
```

---

## 4.5. Mutation hooks đang có nguy cơ quá tải

Anti-pattern:

```txt
useCreateCard
  = gọi API
  + build optimistic card
  + update board cache
  + rollback
  + toast
  + invalidate
  + derive status/group logic
```

Khi use case lớn, hook sẽ khó test và khó maintain.

Tách ra:

```txt
features/work-management/cache/optimistic-item.ts
features/work-management/cache/board-cache-updaters.ts
features/work-management/cache/invalidation.ts
features/work-management/hooks/mutations/use-create-item.ts
```

Hook chỉ orchestration:

```ts
useMutation({
  mutationFn,
  onMutate: optimisticCreateItem(...),
  onError: rollbackBoard(...),
  onSuccess: replaceOptimisticItem(...),
  onSettled: invalidateBoardScope(...),
})
```

---

## 4.6. Notifications không nên polling ngắn

Không nên:

```ts
useQuery({
  queryKey: queryKeys.notifications.unreadCount(),
  refetchInterval: 3000,
})
```

Lý do: với 100 user online, unread count polling 3 giây có thể tạo hàng nghìn request/phút cho dữ liệu phần lớn không đổi.

Roadmap đúng:

```txt
Short term: smart polling 30s, disable in background/idle
Medium term: SSE for notifications
Long term: WebSocket/SignalR/Liveblocks for presence and collaboration
```

Frontend event nên invalidate query, không mutate mọi thứ thủ công nếu payload không đủ.

---

## 4.7. Permission layer chưa nên để rải trong UI

Không nên:

```tsx
{user.role === "Owner" && <DeleteButton />}
```

Nên:

```tsx
const canDeleteBoard = useCan("board.delete", { workspaceId, resourceId: boardId })

return canDeleteBoard ? <DeleteBoardButton /> : null
```

Frontend permission chỉ để UX. Backend vẫn là source of truth.

Cần có:

```txt
lib/permissions/
  permissions.ts
  ability.ts
  use-can.ts
  permission-guard.tsx
```

---

## 5. Refactor roadmap

## Phase 0 — Safety baseline

Mục tiêu: biết hệ thống đang pass/fail gì trước khi refactor.

Việc cần làm:

```txt
cd frontend
bun install
bun run lint
bun run build
bun run type-check    # sau khi thêm script
bun test              # sau khi thêm test setup
```

Nếu hiện chưa có test, ghi nhận là thiếu, không giả vờ pass.

Deliverables:

- Thêm scripts `type-check`, `test`, `quality`.
- Ghi lại lỗi build/lint hiện có nếu có.
- Không refactor lớn khi baseline chưa rõ.

---

## Phase 1 — Architecture documentation sync

Mục tiêu: tài liệu không còn lệch code.

Việc cần làm:

- Viết lại `frontend/README.md` theo cấu trúc thật.
- Thêm `frontend/ARCHITECTURE.md`.
- Thêm `frontend/RULES.md` hoặc hợp nhất vào architecture file.
- Xóa/chỉnh các mô tả cũ như `src/`, `(main)`, `/login`, `/register` nếu code thật không còn dùng.

Acceptance criteria:

```txt
README mô tả đúng app route groups hiện tại.
Architecture docs nói rõ app/components/features/lib boundary.
AI agent đọc docs không bị hướng dẫn sai.
```

---

## Phase 2 — Frontend infrastructure hardening

Mục tiêu: API/query/auth đủ chắc để mở rộng.

Việc cần làm:

```txt
lib/api/refresh-lock.ts
lib/api/csrf.ts nếu backend cần
lib/query/query-client.ts
lib/routes/routes.ts
lib/errors/api-error-map.ts
lib/permissions/use-can.ts
```

Cập nhật:

```txt
app/providers.tsx -> dùng createQueryClient()
api-client.ts -> thêm refresh mutex
logout mutation -> clear query cache
next.config.ts/env -> chuẩn hóa API base/rewrite
```

Acceptance criteria:

```txt
Không có refresh storm khi nhiều request 401.
QueryClient có defaultOptions.
Logout clear cache.
Browser gọi API theo một convention duy nhất.
```

---

## Phase 3 — Business UI placement cleanup

Mục tiêu: phân ranh `app/_components` và `features/**/components`.

Rule áp dụng:

```txt
Route visual/layout composition -> app/**/_components
Business UI owned by feature -> features/**/components
UI primitive -> components/ui
Shared generic UI -> components/layout|feedback|data-display
```

Thứ tự xử lý:

```txt
1. Auth forms
2. Workspace switcher/member/invite components
3. Board shell/view/card detail
4. Docs page tree/editor
5. Notification bell/list
6. Collaboration comments/mentions/reactions
```

Acceptance criteria:

```txt
Business component không nằm trong components/ui.
Route-private UI không bị chuyển sai vào feature.
Feature UI không import app.
```

---

## Phase 4 — `boards` -> `work-management`

Mục tiêu: align với domain dài hạn.

Thực hiện theo bước:

```txt
1. Tạo features/work-management
2. Move boards/api -> work-management/api
3. Move boards/hooks -> work-management/hooks
4. Move board components -> work-management/components
5. Move cache/model helpers -> work-management/cache|model
6. Tạo features/work-management/index.ts
7. Giữ features/boards compatibility exports tạm thời
8. Update imports theo từng route/feature
```

Không nên làm toàn bộ trong một PR nếu code lớn.

Acceptance criteria:

```txt
Board/Card/View/Group/Field không còn phát triển trực tiếp dưới features/boards.
features/boards nếu còn tồn tại chỉ là compatibility layer có deadline xóa.
```

---

## Phase 5 — Feature API/hook cleanup

Mục tiêu: feature module có structure nhất quán.

Target:

```txt
features/<feature>/
  api/
  hooks/
    queries/
    mutations/
    state/
  model/
  cache/
  schemas/
  types/
  components/
  utils/
  index.ts
```

Việc cần làm:

- Tách API theo resource/use case.
- Không để một service ôm nhiều subdomain.
- Mapper DTO -> model tách khỏi component.
- Query hooks chỉ `useQuery`.
- Mutation hooks chỉ một use case.
- Cache update helpers nằm trong `cache/` hoặc `model/query-cache.ts`.
- Toast/i18n message không hard-code sâu trong hook nếu có thể tránh.

Acceptance criteria:

```txt
Không có feature API file quá nhiều trách nhiệm.
Không có mutation hook phình thành nơi chứa toàn bộ business/cache/toast logic.
DTO/mapper có thể test độc lập.
```

---

## Phase 6 — Collaboration/Notifications frontend vertical slice

Mục tiêu: chuẩn bị cho enterprise collaboration.

Tạo hoặc chuẩn hóa:

```txt
features/collaboration/
  comments/
  mentions/
  reactions/
  watchers/
  attachments/
  presence/

features/notifications/
  api/
  hooks/
  components/
  model/
  types/
```

Flow ưu tiên:

```txt
Comment -> Mention -> Notification -> Activity -> Realtime invalidation
```

Acceptance criteria:

```txt
CommentThread reusable cho Card/Page.
NotificationBell không polling ngắn.
Mention parsing/selection không bị duplicate trong nhiều feature.
Presence/realtime không trộn vào server state store.
```

---

## Phase 7 — Boundary enforcement + tests

Mục tiêu: không để kiến trúc lệch dần theo thời gian.

Thêm kiểm tra:

```txt
features không import app
lib không import features
components/ui không import features
features không import sibling feature deep path
app page không gọi api.get/post trực tiếp
server state không lưu vào Zustand
```

Có thể dùng:

- ESLint import rules.
- Custom boundary test bằng script đọc import graph.
- `dependency-cruiser` nếu muốn nghiêm túc hơn.
- Vitest tests cho mappers/cache/permissions.
- Playwright cho critical flows.

Acceptance criteria:

```txt
bun run quality pass.
Boundary violations fail CI.
New feature PR phải theo structure chuẩn.
```

---

## 6. Anti-patterns cần tránh

## 6.1. Biến `components/` thành bãi rác

Không để:

```txt
components/board-shell.tsx
components/card-detail.tsx
components/workspace-switcher.tsx
components/notification-bell.tsx
components/page-editor.tsx
```

Đưa về feature tương ứng.

---

## 6.2. Biến `app/_components` thành nơi chứa toàn bộ business UI

Không để:

```txt
app/(workspace)/[workspaceId]/boards/[boardId]/_components/kanban-view.tsx
app/(workspace)/[workspaceId]/boards/[boardId]/_components/card-detail.tsx
```

Nếu đây là business UI dùng lại hoặc thuộc Work Management, đưa về:

```txt
features/work-management/components/
```

---

## 6.3. Feature import chéo feature khác bằng deep path

Không để:

```ts
import { pageService } from "@/features/docs/api/page.service"
import { boardApi } from "@/features/work-management/api/board.api"
```

trong một feature khác.

Nếu cần composition nhiều domain, đưa lên `app/**/_components` hoặc tạo backend endpoint đủ data.

---

## 6.4. Service ôm mọi thứ

Không để:

```txt
workspace.service.ts
  workspace CRUD
  members
  invitations
  activity
  views
  snapshot
  mapping
  parsing settings
  call boards/docs
```

Tách theo resource:

```txt
workspace.api.ts
members.api.ts
invitations.api.ts
activity.api.ts
views.api.ts
```

---

## 6.5. Hook mutation chứa toàn bộ logic

Không để mutation hook vừa gọi API, vừa build optimistic object, vừa update cache phức tạp, vừa toast, vừa derive business rules.

Tách cache/model helpers.

---

## 6.6. Hard-code role trong UI

Không để:

```tsx
user.role === "Owner"
```

Dùng `useCan()`.

---

## 6.7. Copy backend bounded context 1:1 một cách máy móc

Frontend nên align domain language với backend, nhưng vẫn phải theo product experience.

Ví dụ:

```txt
Dashboard, AppShell, CommandPalette, WorkspaceHome
```

là composition UI, không nhất thiết là bounded context backend.

---

## 6.8. Tách route cho board views

Không tạo route riêng cho table/kanban/calendar/timeline nếu chúng là view switcher cùng một board.

Đúng:

```txt
/[workspaceId]/boards/[boardId]?view=table
/[workspaceId]/boards/[boardId]?view=kanban
```

Sai:

```txt
/[workspaceId]/boards/[boardId]/table
/[workspaceId]/boards/[boardId]/kanban
```

Route riêng chỉ cho resource có identity riêng như card detail, docs page, settings page.

---

## 7. Ưu tiên triển khai gần nhất

Nếu chỉ chọn 10 việc quan trọng nhất, làm theo thứ tự:

1. Viết lại `frontend/README.md` đúng code thật.
2. Thêm `frontend/ARCHITECTURE.md` và `frontend/RULES.md`.
3. Thêm `type-check`, `test`, `quality` scripts.
4. Tạo `lib/query/query-client.ts` với default options.
5. Thêm refresh mutex cho `lib/api/api-client.ts`.
6. Chuẩn hóa env/API base/rewrite.
7. Chốt rule `app/_components` vs `features/**/components`.
8. Bắt đầu chuyển auth forms/workspace business UI về feature.
9. Tạo `features/work-management` và migration từ `features/boards`.
10. Thêm boundary tests để không cho architecture lệch lại.

---

## 8. Kết luận

Frontend Notrelix có nền tảng tốt, nhưng chưa thể gọi là enterprise-ready nếu chưa có rule và enforcement rõ ràng.

Hướng refactor đúng không phải là “chuyển hết component vào feature” hay “giữ hết trong app”. Hướng đúng là phân tầng:

```txt
app/**/_components       = route-private composition
features/**/components   = business UI owned by feature
components/ui            = primitive UI only
components/layout        = generic layout shell
lib                      = frontend infrastructure
```

Đồng thời, hệ thống cần tiến hóa từ:

```txt
features/boards
```

sang:

```txt
features/work-management
```

và bổ sung frontend infrastructure hardening: API refresh mutex, QueryClient policy, permission layer, realtime strategy, testing và import boundary enforcement.

Nếu triển khai theo phase nhỏ, Notrelix frontend có thể đạt kiến trúc enterprise mà không phải refactor lại liên tục.
