# Notrelix Frontend — Architecture Notes

> Stack: Next.js App Router · TypeScript · TanStack Query · shadcn/ui · Bun  
> Pattern: Feature-Sliced Design + Co-location `_components`

---

## 1. Routing — Route thật vs View switcher

### Vấn đề

Nhầm lẫn giữa **route-based navigation** và **tab/view switching** trong cùng một board. Các view như Main Table, Kanban, Docs, Dashboard là tab để switch view — không phải route riêng.

### Sai

```
/[workspaceId]/boards/[boardId]/main-table   ← route riêng
/[workspaceId]/boards/[boardId]/kanban       ← route riêng
/[workspaceId]/boards/[boardId]/docs         ← route riêng
/[workspaceId]/boards/[boardId]/dashboard    ← route riêng
```

Hệ quả:
- Mỗi tab switch = re-fetch toàn bộ data
- Mất state khi switch tab
- Layout bị re-mount không cần thiết

### Đúng — 1 `page.tsx` duy nhất, view qua search param

```
/[workspaceId]/boards/[boardId]                              ← default: Main Table
/[workspaceId]/boards/[boardId]?view=kanban
/[workspaceId]/boards/[boardId]?view=calendar
/[workspaceId]/boards/[boardId]?view=timeline
/[workspaceId]/boards/[boardId]?view=kanban
```

```tsx
// app/(workspace)/[workspaceId]/boards/[boardId]/page.tsx
export default function BoardPage({ searchParams }) {
  const view  = searchParams?.view ?? 'table'
  const docId = searchParams?.doc

  return (
    <>
      <BoardViewTabs activeView={view} />
      {view === 'table'    && <BoardListView />}
      {view === 'kanban'   && <BoardKanban />}
      {view === 'calendar' && <BoardCalendarView />}
      {view === 'timeline' && <BoardTimelineView />}
    </>
  )
}
```

```tsx
// board-view-tabs.tsx — chỉ update searchParam, không navigate route
<Link href={`/boards/${boardId}?view=kanban`} replace>Kanban</Link>
```

### Route con thật sự — chỉ 3 thứ cần route riêng

| Route | Lý do |
|---|---|
| `/card/[cardId]` | Full-page card detail, URL riêng để share/deep-link |
| `@modal/(.)card/[cardId]` | Parallel route intercept, modal overlay khi mở từ board |
| `/docs/[pageId]` ở workspace level | Trang docs độc lập, không trong board |

Docs tab trong board chỉ toggle `?doc=pageId` để mở DocsPanel — không navigate ra ngoài.

---

## 2. URL Strategy — Có nên dùng `/b/[boardId]`?

### Tại sao các hệ thống lớn dùng path ngắn

Linear, Jira, Notion dùng `/b/`, `/p/`, `/t/` không phải để ẩn đi, mà vì:

1. **URL length** — khi nhiều nested segment, URL dài khi copy/share vào Slack, email
2. **Namespace separator** — ngăn conflict giữa entity type khi slug trùng nhau
3. **SEO không liên quan** — workspace app là authenticated, search engine không index

### Với Notrelix hiện tại — không nên đổi

- Đang ở giai đoạn build, chưa có user — chi phí refactor routes sau cao hơn
- Next.js App Router: đổi `/boards/` thành `/b/` chỉ là rename folder, không giải quyết vấn đề view-switching
- `/boards/[boardId]` tự document chính nó, dễ onboard người mới

### Khi nào NÊN dùng path ngắn

Chỉ đáng khi nested quá 3 cấp dynamic segment:

```
# Nested quá sâu — lúc này mới nên abbreviate
/[workspaceId]/projects/[projectId]/boards/[boardId]/card/[cardId]

# Rút gọn thành
/[workspaceId]/p/[projectId]/b/[boardId]/c/[cardId]
```

Notrelix hiện chỉ có 2 cấp dynamic (`workspaceId` + `boardId`), chưa đến ngưỡng.

### Nếu vẫn muốn đổi — làm đúng cách

Dùng `config/routes.ts` centralized để không hardcode string rải rác:

```ts
export const routes = {
  board: (wsId: string, boardId: string, view?: string) =>
    `/${wsId}/b/${boardId}${view ? `?view=${view}` : ''}`,
  doc: (wsId: string, pageId: string) =>
    `/${wsId}/d/${pageId}`,
  card: (wsId: string, boardId: string, cardId: string) =>
    `/${wsId}/b/${boardId}/c/${cardId}`,
}
```

---

## 3. Notifications — Không nên polling ngắn

### Vấn đề với polling mỗi vài giây

```ts
// Anti-pattern
useQuery({
  queryKey: queryKeys.notifications.unreadCount(),
  refetchInterval: 3000,
})
```

Với 100 user online → **2,000 requests/phút** chỉ cho unread count, 99% là empty response.

### Lựa chọn theo độ phù hợp

#### SSE — Server-Sent Events ✅ Khuyên dùng

Một chiều server → client, HTTP thuần, không cần infrastructure mới.

```ts
// features/notifications/hooks/use-notification-stream.ts
export function useNotificationStream() {
  const queryClient = useQueryClient()

  useEffect(() => {
    const es = new EventSource('/api/notifications/stream', {
      withCredentials: true
    })

    es.addEventListener('notification', () => {
      queryClient.invalidateQueries({
        queryKey: queryKeys.notifications.unreadCount()
      })
      queryClient.invalidateQueries({
        queryKey: queryKeys.notifications.list()
      })
    })

    return () => es.close()
  }, [queryClient])
}
```

```ts
// app/api/notifications/stream/route.ts
export async function GET(req: Request) {
  const session = await getSession()

  const stream = new ReadableStream({
    start(controller) {
      const unsubscribe = notificationBus.subscribe(
        session.userId,
        (notification) => {
          controller.enqueue(
            `event: notification\ndata: ${JSON.stringify(notification)}\n\n`
          )
        }
      )
      req.signal.addEventListener('abort', () => {
        unsubscribe()
        controller.close()
      })
    }
  })

  return new Response(stream, {
    headers: {
      'Content-Type': 'text/event-stream',
      'Cache-Control': 'no-cache',
      'Connection': 'keep-alive',
    }
  })
}
```

#### WebSocket — chỉ khi cần real-time 2 chiều

Cần thiết khi có live collaboration (card move real-time, cursor presence). Overkill nếu chỉ để notifications. Nếu cần, dùng **Liveblocks** hoặc **Partykit** thay vì tự build.

#### Polling thông minh — fallback thực dụng

```ts
useQuery({
  queryKey: queryKeys.notifications.unreadCount(),
  refetchInterval: (query) => {
    if (document.visibilityState === 'hidden') return false

    const lastActive = useActivityStore.getState().lastActiveAt
    const idleMs = Date.now() - lastActive

    if (idleMs > 5 * 60_000) return false    // Idle 5 phút → dừng
    if (idleMs > 60_000)     return 60_000   // Idle 1 phút → 1 phút/lần
    return 30_000                             // Active → 30s/lần
  },
  refetchIntervalInBackground: false,
})
```

### Roadmap

| Giai đoạn | Approach |
|---|---|
| Ngắn hạn | Polling thông minh 30s + `refetchIntervalInBackground: false` |
| Trung hạn (production) | SSE cho notifications |
| Dài hạn (có collaboration) | WebSocket hoặc Liveblocks cho board real-time |

---

## 4. API Layer — Tách file theo domain

### Vấn đề: god file tăng trưởng theo chiều ngang

`workspace.service.ts` gom hết mọi API call vào 1 file. Khi thêm billing, integrations, webhooks... file sẽ có 40–50 functions.

### Nguyên tắc phân tầng

```
.api.ts      → HTTP calls thuần, return Promise<T>
               Không có state, không có side effect

.service.ts  → Business logic phức tạp, orchestrate nhiều api calls
               Ví dụ: createWorkspaceWithDefaults() gọi create() rồi createDefaultBoard()
               Chỉ cần khi logic thật sự phức tạp
```

### Khi nào giữ 1 file, khi nào tách

| Giữ 1 file | Tách ra |
|---|---|
| < 5 functions | ≥ 5 functions |
| Cùng 1 entity thật sự | Nhiều sub-domain rõ ràng |
| Feature nhỏ như `auth`, `theme` | Feature lớn như `workspace`, `boards` |

### Cấu trúc đúng cho workspace

```
features/workspace/api/
├── workspace.api.ts      ← CRUD workspace core + mapWorkspaceDto
├── members.api.ts        ← list, updateRole, remove + mapMemberDto
├── invitations.api.ts    ← list, create, delete, accept, byToken
└── activity.api.ts       ← getLogs + mapActivity
```

Mapper functions (`mapWorkspaceDto`, `mapMemberDto`) đi cùng api file của chúng — không tách riêng vì chỉ dùng nội bộ trong api call đó.

---

## 5. `workspace.service.ts` — Phân tích chi tiết

Có 4 vấn đề xếp chồng trong file thực tế:

### Vấn đề A: DTO type sống sai chỗ

`WorkspaceDtoApi`, `WorkspaceMemberDtoApi`, `WorkspaceActivityResponseApi` đang khai báo local trong file service. Cần tách ra nhưng không export ra ngoài feature:

```ts
// types/dto.ts — internal only, KHÔNG export ra ngoài feature
// Chỉ dùng để type shape từ API backend, không validate bằng Zod
type WorkspaceDtoApi = {
  id: string
  name: string
  slug: string
  isPersonal: boolean
  plan: string
  iconType?: string | null
  iconValue?: string | null
  settings?: string | null
}

type WorkspaceMemberDtoApi = {
  userId: string
  name: string
  avatar?: string | null
  role: string
  joinedAt: string
}
```

DTO không cần Zod — đây là response từ backend đã được validate. Zod ở đây chỉ tăng bundle size không có benefit.

### Vấn đề B: `settings: string` — nghiêm trọng nhất

```ts
// Hiện tại — JSON.parse() lặp lại 4-5 lần trong service
const settingsObj = JSON.parse(workspace.settings)
settingsObj.customViews = currentViews
await api.patch(..., { settings: JSON.stringify(settingsObj) })
```

Backend đang lưu object dưới dạng JSON string thay vì column riêng hoặc JSONB. Hệ quả:
- `any` type rải rác (`settingsObj: any`)
- `try/catch` parse lặp đi lặp lại
- Cross-feature data (views config) nhét vào workspace settings

**Giải pháp tốt nhất:** Backend tạo endpoint riêng `PATCH /workspaces/:id/views`.

**Giải pháp tạm thời nếu không sửa được backend ngay:**

```ts
// utils/settings.ts
type WorkspaceSettings = {
  customViews?: WorkspaceView[]
  customViewsOrder?: string[]
}

export function parseSettings(raw?: string | null): WorkspaceSettings {
  if (!raw) return {}
  try { return JSON.parse(raw) } catch { return {} }
}

export function stringifySettings(settings: WorkspaceSettings): string {
  return JSON.stringify(settings)
}
```

### Vấn đề C: Cross-feature import vi phạm FSD

```ts
// workspace.service.ts — HIỆN TẠI, vi phạm nguyên tắc
import { boardApi } from "@/features/boards/api/board.api"
import { pageService } from "@/features/docs/api/page.service"
```

`getViews()` đang gọi sang `boards` và `docs` feature.

**Cách 1 — Backend trả về đủ data** (tốt nhất): `GET /workspaces/:id/views` trả về views đã resolved với `boardId` và `pageId` sẵn.

**Cách 2 — Đẩy assembly lên tầng hook:**

```ts
// hooks/use-workspace-views.ts — hook tự assemble, không vi phạm FSD
export function useWorkspaceViews(workspaceId: string) {
  const { data: boards }    = useBoardsByWorkspace(workspaceId)
  const { data: pages }     = usePageList(workspaceId)
  const { data: workspace } = useWorkspace(workspaceId)

  return useMemo(() =>
    createViews(workspaceId, boards ?? [], pages ?? []),
    [workspaceId, boards, pages]
  )
}
```

### Vấn đề D: `invitations` dùng `any[]`

```ts
async getInvitations(workspaceId: string): Promise<any[]>  // ← any
async createInvitation(...): Promise<any>                   // ← any
async getInvitationByToken(...): Promise<any>              // ← any
```

Cần type đúng trong `types/index.ts`:

```ts
export type WorkspaceInvitation = {
  id: string
  email: string
  role: WorkspaceMember['role']
  expiresAt: string
  isAccepted: boolean
  createdAt: string
}
```

---

## 6. Cấu trúc workspace feature sau khi refactor

```
features/workspace/
├── api/
│   ├── workspace.api.ts      ← CRUD + mapWorkspaceDto + normalizePlan
│   ├── members.api.ts        ← list, updateRole, remove + mapMemberDto
│   ├── invitations.api.ts    ← list, create, delete, accept, byToken
│   └── activity.api.ts       ← getLogs + mapActivity
│
├── types/
│   ├── index.ts              ← exported domain types
│   │                           WorkspaceSummary, WorkspaceMember,
│   │                           WorkspaceView, WorkspaceSnapshot,
│   │                           WorkspaceInvitation, WorkspaceActivityItem
│   └── dto.ts                ← internal DTO types (KHÔNG export ra ngoài)
│                               WorkspaceDtoApi, WorkspaceMemberDtoApi
│
├── schemas/
│   ├── index.ts              ← createWorkspaceSchema, updateWorkspaceSchema,
│   │                           inviteMemberSchema
│   └── workspace-view.schema.ts
│
├── utils/
│   ├── settings.ts           ← parseSettings, stringifySettings
│   └── index.ts              ← getInitials, normalizePlan,
│                               normalizeMemberRole, memberColors
│
└── hooks/
    ├── use-workspace.ts
    ├── use-workspace-views.ts    ← assembly từ boards + docs hooks (không vi phạm FSD)
    ├── use-workspace-members.ts
    ├── use-workspace-invitations.ts
    └── ...
```

---

## 7. Schema — Dùng ở đâu, không dùng ở đâu

Schema (Zod) = validate **input từ user**. DTO type = shape **response từ API**.

```ts
// schemas/index.ts — validate form input
export const createWorkspaceSchema = z.object({
  name: z.string().min(1).max(100),
  slug: z.string().min(1).regex(/^[a-z0-9-]+$/),
  isPersonal: z.boolean(),
})

export const inviteMemberSchema = z.object({
  email: z.string().email(),
  role: z.enum(['admin', 'member', 'guest']),
})
```

| | Schema (Zod) | Type only |
|---|---|---|
| Form input từ user | ✅ | — |
| URL search params | ✅ | — |
| API request body | ✅ | — |
| API response (DTO) | ❌ | ✅ |
| Domain model | ❌ | ✅ |

---

## 8. Import rules — FSD một chiều

```ts
// ✅ ĐÚNG
import { apiClient } from '@/lib/api/client'
import { queryKeys } from '@/lib/query/query-keys'
import { useWorkspace } from '@/features/workspace/hooks/use-workspace'
import { workspaceApi } from '../api/workspace.api'   // relative trong cùng feature

// ❌ SAI — features import từ features khác (trong service layer)
import { boardApi } from '@/features/boards/api/board.api'

// ❌ SAI — features import từ app
import { BoardKanban } from '@/app/(workspace)/boards/_components/board-kanban'

// ❌ SAI — _components của route này import _components route khác
import { SignInForm } from '@/app/(auth)/_components/sign-in-form'
```

Cross-feature data phải đi qua **hook ở tầng app** hoặc **shared lib**, không đi thẳng giữa hai feature.
