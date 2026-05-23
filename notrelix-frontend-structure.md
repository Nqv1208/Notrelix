# Notrelix — Frontend Structure
> Stack: Next.js 16 App Router · TypeScript · shadcn/ui · TanStack Query · next-intl · Bun
> Pattern: Feature-Sliced Design + Co-location _components

---

## Nguyên tắc cốt lõi

```
lib/api/      → HTTP wrapper + endpoints (axios instance, interceptors, base URLs)
features/     → Logic thuần: hooks, schemas, types, utils — KHÔNG có UI
app/*/_components/ → UI gắn với route cụ thể (private, co-located)
components/   → UI dùng chung ≥ 2 nơi (shared)
```

**Quy tắc import (một chiều, không đảo ngược):**
```
app → features → lib → (external)
app → components → (external)
features KHÔNG import từ app
features KHÔNG import từ features khác (dùng lib/query/query-keys.ts để share)
```

---

## Cấu trúc đầy đủ

```
frontend/
│
├── src/
│   │
│   ├── app/                                   # Next.js App Router — CHỈ có UI + routing
│   │   ├── layout.tsx                         # Root: Providers (Query+Theme+Intl+Toaster)
│   │   ├── page.tsx                           # / → redirect first workspace || /home
│   │   ├── not-found.tsx                      # 404
│   │   ├── error.tsx                          # 'use client' — global error boundary
│   │   ├── loading.tsx                        # Global loading
│   │   │
│   │   ├── (auth)/                            # ══ Unauthenticated only ══
│   │   │   ├── layout.tsx                     # Centered card, no sidebar
│   │   │   ├── _components/                   # UI private: chỉ auth routes dùng
│   │   │   │   ├── sign-in-form.tsx           # Form UI (gọi hook từ features/auth)
│   │   │   │   ├── sign-up-form.tsx
│   │   │   │   ├── forgot-password-form.tsx
│   │   │   │   └── oauth-buttons.tsx          # Google / GitHub SSO buttons
│   │   │   ├── sign-in/
│   │   │   │   ├── page.tsx
│   │   │   │   └── loading.tsx
│   │   │   ├── sign-up/
│   │   │   │   └── page.tsx
│   │   │   ├── forgot-password/
│   │   │   │   └── page.tsx
│   │   │   ├── reset-password/
│   │   │   │   └── page.tsx                   # ?token=xxx
│   │   │   ├── verify-email/
│   │   │   │   └── page.tsx
│   │   │   └── sso-callback/
│   │   │       └── page.tsx                   # OAuth redirect handler
│   │   │
│   │   ├── (dashboard)/                       # ══ App shell — authenticated ══
│   │   │   ├── layout.tsx                     # AppSidebar + AppHeader
│   │   │   ├── loading.tsx                    # Shell skeleton
│   │   │   ├── error.tsx                      # 'use client'
│   │   │   ├── _components/                   # UI private: shell components
│   │   │   │   ├── app-sidebar.tsx            # Workspace switcher + nav links
│   │   │   │   ├── app-header.tsx             # Breadcrumb + search + notif + avatar
│   │   │   │   ├── workspace-switcher.tsx     # Dropdown chọn workspace
│   │   │   │   ├── user-menu.tsx              # Avatar → profile/logout
│   │   │   │   └── notification-bell.tsx      # Unread count + popover
│   │   │   │
│   │   │   └── home/
│   │   │       ├── page.tsx                   # /home — recent docs, my cards, workspaces
│   │   │       ├── loading.tsx
│   │   │       └── _components/
│   │   │           ├── recent-pages.tsx
│   │   │           ├── my-cards.tsx
│   │   │           └── workspace-list.tsx
│   │   │
│   │   ├── (workspace)/                       # ══ Workspace context — authenticated ══
│   │   │   ├── layout.tsx                     # Pass-through (không add UI)
│   │   │   │
│   │   │   ├── account/                       # /account/* — không cần workspace context
│   │   │   │   ├── layout.tsx                 # Account settings tabs
│   │   │   │   ├── _components/
│   │   │   │   │   └── account-tabs.tsx
│   │   │   │   ├── profile/page.tsx
│   │   │   │   ├── security/page.tsx          # Password, 2FA, active sessions
│   │   │   │   ├── notifications/page.tsx     # Notification preferences
│   │   │   │   ├── appearance/page.tsx        # Theme, language, density
│   │   │   │   └── calendar-sync/page.tsx     # Google Calendar OAuth + iCal export
│   │   │   │
│   │   │   └── [workspaceId]/               # ── Dynamic workspace ──
│   │   │       ├── layout.tsx                 # Validate + WorkspaceProvider + CalendarSyncProvider
│   │   │       ├── page.tsx                   # /[ws] — pinned docs, active boards, deadlines
│   │   │       ├── loading.tsx
│   │   │       ├── error.tsx                  # 'use client' — workspace not found
│   │   │       ├── _components/               # UI private: workspace-level
│   │   │       │   ├── workspace-overview.tsx
│   │   │       │   └── workspace-nav.tsx      # Docs | Boards | Calendar sub-tabs
│   │   │       │
│   │   │       ├── settings/
│   │   │       │   ├── layout.tsx             # Settings tab navigation
│   │   │       │   ├── page.tsx               # redirect → /settings/general
│   │   │       │   ├── _components/
│   │   │       │   │   └── settings-tabs.tsx
│   │   │       │   ├── general/page.tsx       # Tên, icon, slug, is_personal, danger zone
│   │   │       │   ├── members/page.tsx       # Members + invitations
│   │   │       │   ├── permissions/page.tsx   # RBAC
│   │   │       │   ├── billing/page.tsx       # Plan, usage, invoices
│   │   │       │   └── integrations/page.tsx  # Google Calendar, Webhooks
│   │   │       │
│   │   │       ├── calendar/                  # Unified calendar (cards + pages)
│   │   │       │   ├── page.tsx
│   │   │       │   ├── loading.tsx
│   │   │       │   └── _components/
│   │   │       │       ├── unified-calendar.tsx   # Month/week/day grid
│   │   │       │       └── event-popover.tsx       # Click event → card/page preview
│   │   │       │
│   │   │       ├── docs/                      # ── Notion-like ──
│   │   │       │   ├── page.tsx               # /[ws]/docs — page tree list
│   │   │       │   ├── loading.tsx
│   │   │       │   ├── _components/
│   │   │       │   │   ├── page-tree.tsx      # Recursive sidebar tree
│   │   │       │   │   └── new-page-button.tsx
│   │   │       │   └── [pageId]/
│   │   │       │       ├── page.tsx           # Block editor — prefetch + HydrationBoundary
│   │   │       │       ├── loading.tsx        # Title + blocks skeleton
│   │   │       │       ├── error.tsx          # 'use client' — not found / no permission
│   │   │       │       ├── _components/
│   │   │       │       │   ├── page-header.tsx    # Cover + icon + title + breadcrumb
│   │   │       │       │   ├── page-toolbar.tsx   # Share, history, deadline, more
│   │   │       │       │   └── block-renderer.tsx # Render block list
│   │   │       │       └── history/
│   │   │       │           └── page.tsx       # Version history
│   │   │       │
│   │   │       └── boards/                    # ── Trello-like ──
│   │   │           ├── page.tsx               # /[ws]/boards — boards grid
│   │   │           ├── loading.tsx
│   │   │           ├── _components/
│   │   │           │   ├── board-card.tsx         # Board preview card
│   │   │           │   └── create-board-dialog.tsx
│   │   │           └── [boardId]/
│   │   │               ├── layout.tsx             # {children} + {modal} + DocsPanel(?doc=)
│   │   │               ├── page.tsx               # Kanban (default view)
│   │   │               ├── loading.tsx
│   │   │               ├── error.tsx              # 'use client'
│   │   │               ├── _components/           # UI private: board-level
│   │   │               │   ├── board-toolbar.tsx      # Title + filter + members
│   │   │               │   ├── board-view-tabs.tsx    # Kanban|List|Calendar|Timeline + Docs toggle
│   │   │               │   ├── board-kanban.tsx       # Kanban columns + DnD
│   │   │               │   ├── board-list-view.tsx    # Table-like list view
│   │   │               │   ├── board-calendar-view.tsx
│   │   │               │   ├── board-timeline-view.tsx
│   │   │               │   ├── kanban-column.tsx      # Single list column
│   │   │               │   ├── kanban-card.tsx        # Card preview in kanban
│   │   │               │   ├── docs-panel.tsx         # Resizable right panel (?doc=pageId)
│   │   │               │   └── docs-panel-skeleton.tsx
│   │   │               ├── list/page.tsx
│   │   │               ├── calendar/page.tsx
│   │   │               ├── timeline/page.tsx
│   │   │               ├── card/[cardId]/
│   │   │               │   ├── page.tsx               # Full-page card detail
│   │   │               │   ├── loading.tsx
│   │   │               │   ├── _components/
│   │   │               │   │   ├── card-detail.tsx    # Checklist, members, labels, dates
│   │   │               │   │   ├── card-sidebar.tsx   # Quick actions
│   │   │               │   │   └── card-linked-page.tsx # Linked doc preview + open
│   │   │               │   └── docs/[pageId]/
│   │   │               │       └── page.tsx           # Linked page in card context
│   │   │               └── @modal/                    # ◈ Parallel route — card modal
│   │   │                   ├── default.tsx            # null
│   │   │                   └── (.)card/[cardId]/
│   │   │                       └── page.tsx           # Intercepting: modal over board
│   │   │
│   │   ├── (admin)/                           # ══ Admin — role: admin only ══
│   │   │   ├── layout.tsx
│   │   │   ├── _components/
│   │   │   │   └── admin-sidebar.tsx
│   │   │   └── admin/
│   │   │       ├── page.tsx                   # /admin — metrics dashboard
│   │   │       ├── users/
│   │   │       │   ├── page.tsx
│   │   │       │   └── [userId]/page.tsx
│   │   │       ├── workspaces/
│   │   │       │   ├── page.tsx
│   │   │       │   └── [workspaceId]/page.tsx
│   │   │       ├── billing/page.tsx
│   │   │       └── audit-logs/page.tsx
│   │   │
│   │   ├── invite/[token]/page.tsx            # /invite/[token] — public
│   │   ├── 403/page.tsx                       # Forbidden
│   │   └── api/
│   │       ├── auth/route.ts                  # POST /api/auth/refresh
│   │       ├── calendar/route.ts              # Two-way sync webhook
│   │       └── webhooks/route.ts
│   │
│   ├── features/                              # Logic thuần — KHÔNG có UI component
│   │   │                                      # Mỗi feature = 1 domain
│   │   ├── auth/
│   │   │   ├── api/
│   │   │   │   ├── auth-api.ts                # login, register, logout, refresh, me
│   │   │   │   └── oauth-api.ts               # Google, GitHub SSO calls
│   │   │   ├── hooks/
│   │   │   │   ├── use-auth.ts                # useMe, useLogin, useLogout, useRegister
│   │   │   │   ├── use-session.ts             # Client-side session state
│   │   │   │   ├── use-forgot-password.ts
│   │   │   │   └── use-reset-password.ts
│   │   │   ├── schemas/
│   │   │   │   ├── sign-in.schema.ts          # zod: email + password
│   │   │   │   ├── sign-up.schema.ts          # zod: name + email + password + confirm
│   │   │   │   └── reset-password.schema.ts
│   │   │   ├── types/
│   │   │   │   └── index.ts                   # User, Session, OAuthAccount, JWTPayload
│   │   │   ├── utils/
│   │   │   │   └── token.ts                   # getAccessToken / setAccessToken / clearAccessToken (in-memory)
│   │   │   └── i18n/
│   │   │       ├── en.json
│   │   │       └── vi.json
│   │   │
│   │   ├── workspace/
│   │   │   ├── api/
│   │   │   │   ├── workspaces-api.ts          # CRUD workspace
│   │   │   │   └── members-api.ts             # members, invitations, roles
│   │   │   ├── hooks/
│   │   │   │   ├── use-workspace.ts           # useWorkspace(slug), useWorkspaceList
│   │   │   │   ├── use-workspace-members.ts
│   │   │   │   ├── use-create-workspace.ts
│   │   │   │   └── use-accept-invitation.ts
│   │   │   ├── schemas/
│   │   │   │   ├── create-workspace.schema.ts
│   │   │   │   └── invite-member.schema.ts
│   │   │   ├── types/
│   │   │   │   └── index.ts                   # Workspace, WorkspaceMember, WorkspaceInvitation
│   │   │   ├── utils/
│   │   │   │   └── workspace-slug.ts          # validateSlug, generateSlug
│   │   │   └── i18n/
│   │   │       ├── en.json
│   │   │       └── vi.json
│   │   │
│   │   ├── docs/
│   │   │   ├── api/
│   │   │   │   ├── pages-api.ts               # CRUD pages, move, publish
│   │   │   │   └── blocks-api.ts              # CRUD blocks, reorder, batch update
│   │   │   ├── hooks/
│   │   │   │   ├── use-page.ts                # usePage, usePageBreadcrumb
│   │   │   │   ├── use-page-tree.ts           # usePageTree(workspaceId)
│   │   │   │   ├── use-page-blocks.ts         # usePageBlocks(pageId)
│   │   │   │   ├── use-create-page.ts
│   │   │   │   ├── use-update-page.ts
│   │   │   │   ├── use-delete-page.ts
│   │   │   │   ├── use-move-page.ts
│   │   │   │   ├── use-create-block.ts
│   │   │   │   └── use-update-block.ts
│   │   │   ├── schemas/
│   │   │   │   ├── page.schema.ts
│   │   │   │   └── block.schema.ts
│   │   │   ├── types/
│   │   │   │   └── index.ts                   # Page, Block, BlockType, BlockProperties
│   │   │   └── utils/
│   │   │       ├── block-helpers.ts            # createBlock, getBlockText, ...
│   │   │       └── fractional-index.ts         # generatePosition(before, after)
│   │   │
│   │   ├── boards/
│   │   │   ├── api/
│   │   │   │   ├── boards-api.ts              # CRUD boards, board members
│   │   │   │   ├── lists-api.ts               # CRUD lists, reorder
│   │   │   │   ├── cards-api.ts               # CRUD cards, move, link-page, assign
│   │   │   │   ├── labels-api.ts
│   │   │   │   └── checklists-api.ts
│   │   │   ├── hooks/
│   │   │   │   ├── use-board.ts               # useBoard, useBoardList
│   │   │   │   ├── use-full-board.ts          # useFullBoard — board + lists + cards
│   │   │   │   ├── use-card.ts                # useCard, useCardChecklists
│   │   │   │   ├── use-my-cards.ts            # Cards assigned to current user
│   │   │   │   ├── use-create-board.ts
│   │   │   │   ├── use-create-card.ts
│   │   │   │   ├── use-update-card.ts
│   │   │   │   ├── use-move-card.ts           # Optimistic update DnD
│   │   │   │   ├── use-link-page.ts           # Link/unlink page to card
│   │   │   │   └── use-board-docs-panel.ts    # URL state ?doc=pageId
│   │   │   ├── schemas/
│   │   │   │   ├── create-board.schema.ts
│   │   │   │   └── create-card.schema.ts
│   │   │   ├── types/
│   │   │   │   └── index.ts                   # Board, List, Card, Label, Checklist, CardLink
│   │   │   └── utils/
│   │   │       └── card-helpers.ts            # getPriorityColor, getStatusLabel, ...
│   │   │
│   │   ├── calendar/
│   │   │   ├── api/
│   │   │   │   ├── calendar-api.ts            # getCalendarEvents, triggerSync
│   │   │   │   └── integrations-api.ts        # connect/disconnect Google Calendar
│   │   │   ├── hooks/
│   │   │   │   ├── use-calendar-events.ts     # Unified events (cards + pages)
│   │   │   │   ├── use-calendar-integration.ts
│   │   │   │   └── use-sync-calendar.ts       # Manual trigger sync mutation
│   │   │   ├── types/
│   │   │   │   └── index.ts                   # CalendarEvent, CalendarIntegration
│   │   │   └── utils/
│   │   │       └── calendar-helpers.ts        # formatEventDate, getEventColor, ...
│   │   │
│   │   ├── notifications/
│   │   │   ├── api/
│   │   │   │   └── notifications-api.ts       # list, markRead, markAllRead, unreadCount
│   │   │   ├── hooks/
│   │   │   │   ├── use-notifications.ts
│   │   │   │   ├── use-unread-count.ts        # refetchInterval: 15s
│   │   │   │   └── use-mark-read.ts
│   │   │   └── types/
│   │   │       └── index.ts                   # Notification, NotificationType
│   │   │
│   │   └── search/
│   │       ├── api/
│   │       │   └── search-api.ts              # search(workspaceId, query) → pages + cards
│   │       ├── hooks/
│   │       │   └── use-search.ts              # enabled khi query.length >= 2
│   │       └── types/
│   │           └── index.ts                   # SearchResult
│   │
│   ├── lib/                                   # Infrastructure — framework-level
│   │   ├── api/
│   │   │   ├── client.ts                      # Axios instance + auto refresh interceptor
│   │   │   └── endpoints.ts                   # Tất cả API URL constants
│   │   │   # Ví dụ endpoints.ts:
│   │   │   # export const ENDPOINTS = {
│   │   │   #   auth: { login: '/auth/login', me: '/auth/me', refresh: '/auth/refresh' },
│   │   │   #   pages: { list: (wsId) => `/workspaces/${wsId}/pages`, ... },
│   │   │   #   cards: { detail: (id) => `/cards/${id}`, move: (id) => `/cards/${id}/move` },
│   │   │   # }
│   │   ├── auth/
│   │   │   └── server-session.ts              # getSession() cho Server Components (cookies())
│   │   ├── query/
│   │   │   ├── query-keys.ts                  # Centralized query keys factory (toàn bộ dự án)
│   │   │   └── server-query-client.ts         # getQueryClient() cho server-side prefetch
│   │   └── utils/
│   │       ├── cn.ts                          # clsx + tailwind-merge
│   │       └── format.ts                      # formatDate, formatBytes, truncate
│   │
│   ├── components/                            # Shared UI — dùng ≥ 2 nơi khác nhau
│   │   ├── ui/                                # shadcn/ui primitives (Button, Dialog, ...)
│   │   ├── providers/
│   │   │   ├── query-provider.tsx             # TanStack QueryClientProvider
│   │   │   ├── theme-provider.tsx             # next-themes wrapper
│   │   │   └── workspace-provider.tsx         # WorkspaceContext cho client components
│   │   └── shared/
│   │       ├── notification-list.tsx          # Notification popover content
│   │       ├── search-command.tsx             # ⌘K command palette
│   │       ├── avatar-group.tsx               # Stack avatars (members)
│   │       ├── empty-state.tsx                # Empty placeholder với icon + action
│   │       ├── confirm-dialog.tsx             # Reusable confirm/delete dialog
│   │       ├── page-icon-picker.tsx           # Emoji / icon picker (docs + workspace)
│   │       └── rich-text-display.tsx          # Render markdown content read-only
│   │
│   ├── hooks/                                 # Global hooks — không thuộc feature nào
│   │   ├── use-mobile.ts                      # (đã có) useMediaQuery wrapper
│   │   ├── use-debounce.ts                    # Debounce value/callback
│   │   ├── use-local-storage.ts               # Type-safe localStorage hook
│   │   └── use-copy-to-clipboard.ts
│   │
│   ├── types/                                 # Global types — dùng xuyên suốt
│   │   ├── api.ts                             # ApiResponse<T>, PaginatedResponse<T>, ApiError
│   │   └── common.ts                          # ResourceType, ID, Nullable<T>, ...
│   │   # NOTE: Domain types (User, Card, Page...) đặt trong features/{domain}/types/
│   │
│   ├── i18n/                                  # next-intl config
│   │   └── request.ts                         # (đã có) getRequestConfig
│   │
│   ├── messages/                              # (đã có) i18n message files
│   │   ├── en.json                            # Global messages (nav, errors, common)
│   │   └── vi.json
│   │   # NOTE: Feature-specific messages đặt trong features/{domain}/i18n/
│   │
│   ├── styles/
│   │   └── globals.css
│   │
│   ├── config/
│   │   └── routes.ts                          # Route constants (lib/auth/routes.ts → đây)
│   │
│   └── middleware.ts                          # Auth guard + tenant resolve + RBAC
│
├── registry/new-york-v4/ui/                   # (đã có) shadcn registry
├── public/
├── messages/                                  # (nếu next-intl dùng root-level)
├── .env.local
├── .agent                                     # AI agent config (Cursor rules)
├── .agents                                    # Multi-agent config
├── AGENTS.md                                  # Project rules cho AI agents
├── bun.lock
└── next.config.ts
```

---

## Thay đổi so với cấu trúc hiện tại

| Hiện tại | Sau refactor | Lý do |
|----------|-------------|-------|
| `(workspace)/board` | `(workspace)/[workspaceId]/boards/[boardId]` | Dynamic route đúng với multi-tenant |
| `(workspace)/workspace` | `(workspace)/[workspaceId]` | Bỏ folder trùng tên |
| `(workspace)/_components` | `(workspace)/[workspaceId]/_components` | Co-locate đúng cấp |
| `features/documents` | `features/docs` | Nhất quán với route `/docs` |
| `lib/api` + `features/auth/api` | `lib/api/client.ts` + `features/auth/api/auth-api.ts` | lib/api = wrapper, features/*/api = business calls |
| `lib/auth/routes.ts` | `src/config/routes.ts` | Routes không phải auth concern |
| `types/api.ts` (root) | `src/types/api.ts` | Đúng chỗ, không đổi |
| `features/auth/types` | Giữ nguyên, thêm các domain khác | Đúng rồi |
| `hooks/use-mobile.ts` | Giữ, thêm hooks global khác | Đúng rồi |
| `components/theme-provider.tsx` | `components/providers/theme-provider.tsx` | Gom providers lại |
| `providers.tsx` (trong app/) | Tách ra `components/providers/` | Tái sử dụng được |

---

## Import patterns

```typescript
// ✅ ĐÚNG — feature import lib
import { apiClient } from '@/lib/api/client'
import { ENDPOINTS } from '@/lib/api/endpoints'
import { queryKeys } from '@/lib/query/query-keys'

// ✅ ĐÚNG — app/_components import features
import { useLogin } from '@/features/auth/hooks/use-auth'
import { signInSchema } from '@/features/auth/schemas/sign-in.schema'

// ✅ ĐÚNG — app/_components import shared components
import { AvatarGroup } from '@/components/shared/avatar-group'

// ✅ ĐÚNG — features import types từ chính nó
import type { Card } from '@/features/boards/types'

// ✅ ĐÚNG — share types qua global types
import type { ApiResponse } from '@/types/api'

// ❌ SAI — features import từ app
import { BoardKanban } from '@/app/(workspace)/boards/_components/board-kanban'

// ❌ SAI — features import từ features khác
import { useWorkspace } from '@/features/workspace/hooks/use-workspace'
// → Thay bằng: nhận workspaceId qua prop hoặc context

// ❌ SAI — _components của route này import _components route khác
import { SignInForm } from '@/app/(auth)/_components/sign-in-form'
// → Nếu cần dùng nhiều nơi → promote lên components/shared/
```

---

## Query Keys Factory (lib/query/query-keys.ts)

```typescript
// File này là SINGLE SOURCE OF TRUTH cho tất cả query keys
// Tất cả features đều import từ đây — không hardcode string

export const queryKeys = {
  auth:          { me: () => ['auth', 'me'] as const },
  workspaces:    {
    list:        ()           => ['workspaces', 'list'] as const,
    detail:      (slug: string) => ['workspaces', slug] as const,
    members:     (id: string) => ['workspaces', id, 'members'] as const,
  },
  pages:         {
    tree:        (wsId: string)   => ['pages', wsId, 'tree'] as const,
    detail:      (id: string)     => ['pages', 'detail', id] as const,
    blocks:      (id: string)     => ['pages', id, 'blocks'] as const,
    breadcrumb:  (id: string)     => ['pages', id, 'breadcrumb'] as const,
  },
  boards:        {
    list:        (wsId: string)   => ['boards', wsId] as const,
    detail:      (id: string)     => ['boards', 'detail', id] as const,
    fullBoard:   (id: string)     => ['boards', id, 'full'] as const,
  },
  cards:         {
    detail:      (id: string)     => ['cards', 'detail', id] as const,
    checklists:  (id: string)     => ['cards', id, 'checklists'] as const,
    myCards:     (wsId: string)   => ['cards', 'my', wsId] as const,
  },
  calendar:      {
    events:      (wsId: string)   => ['calendar', wsId, 'events'] as const,
    integration: (userId: string) => ['calendar', userId, 'integration'] as const,
  },
  notifications: {
    list:        ()               => ['notifications'] as const,
    unreadCount: ()               => ['notifications', 'unread-count'] as const,
  },
  search:        {
    query:       (wsId: string, q: string) => ['search', wsId, q] as const,
  },
} as const
```

---

## Endpoints (lib/api/endpoints.ts)

```typescript
// Tất cả URL tập trung 1 nơi — features/*/api gọi qua đây
// lib/api/client.ts là axios instance, endpoints.ts là URL map

export const ENDPOINTS = {
  auth: {
    login:          '/auth/login',
    register:       '/auth/register',
    logout:         '/auth/logout',
    refresh:        '/auth/refresh',
    me:             '/auth/me',
    forgotPassword: '/auth/forgot-password',
    resetPassword:  '/auth/reset-password',
  },
  workspaces: {
    list:           '/workspaces',
    detail:         (slug: string)  => `/workspaces/${slug}`,
    members:        (id: string)    => `/workspaces/${id}/members`,
    invitations:    (id: string)    => `/workspaces/${id}/invitations`,
    invite:         (token: string) => `/invitations/${token}/accept`,
  },
  pages: {
    tree:           (wsId: string)  => `/workspaces/${wsId}/pages/tree`,
    list:           (wsId: string)  => `/workspaces/${wsId}/pages`,
    detail:         (id: string)    => `/pages/${id}`,
    blocks:         (id: string)    => `/pages/${id}/blocks`,
    breadcrumb:     (id: string)    => `/pages/${id}/breadcrumb`,
    move:           (id: string)    => `/pages/${id}/move`,
  },
  boards: {
    list:           (wsId: string)  => `/workspaces/${wsId}/boards`,
    detail:         (id: string)    => `/boards/${id}`,
    full:           (id: string)    => `/boards/${id}/full`,
    members:        (id: string)    => `/boards/${id}/members`,
  },
  lists: {
    create:         '/lists',
    update:         (id: string)    => `/lists/${id}`,
  },
  cards: {
    list:           (listId: string) => `/lists/${listId}/cards`,
    detail:         (id: string)    => `/cards/${id}`,
    move:           (id: string)    => `/cards/${id}/move`,
    linkPage:       (id: string)    => `/cards/${id}/link-page`,
    members:        (id: string)    => `/cards/${id}/members`,
    checklists:     (id: string)    => `/cards/${id}/checklists`,
    myCards:        (wsId: string)  => `/workspaces/${wsId}/cards/my`,
  },
  calendar: {
    events:         (wsId: string)  => `/workspaces/${wsId}/calendar`,
    integrations:   '/calendar/integrations',
    sync:           '/calendar/sync',
  },
  notifications: {
    list:           '/notifications',
    unreadCount:    '/notifications/unread-count',
    markRead:       (id: string)    => `/notifications/${id}/read`,
    markAllRead:    '/notifications/read-all',
  },
  search:           (wsId: string)  => `/workspaces/${wsId}/search`,
} as const
```
