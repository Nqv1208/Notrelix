This is a [Next.js](https://nextjs.org) project bootstrapped with [`create-next-app`](https://nextjs.org/docs/app/api-reference/cli/create-next-app).

## Getting Started

First, run the development server:

```bash
npm run dev
# or
yarn dev
# or
pnpm dev
# or
bun dev
```

Open [http://localhost:3000](http://localhost:3000) with your browser to see the result.

You can start editing the page by modifying `app/page.tsx`. The page auto-updates as you edit the file.

This project uses [`next/font`](https://nextjs.org/docs/app/building-your-application/optimizing/fonts) to automatically optimize and load [Geist](https://vercel.com/font), a new font family for Vercel.

## Learn More

To learn more about Next.js, take a look at the following resources:

- [Next.js Documentation](https://nextjs.org/docs) - learn about Next.js features and API.
- [Learn Next.js](https://nextjs.org/learn) - an interactive Next.js tutorial.

You can check out [the Next.js GitHub repository](https://github.com/vercel/next.js) - your feedback and contributions are welcome!

## Deploy on Vercel

The easiest way to deploy your Next.js app is to use the [Vercel Platform](https://vercel.com/new?utm_medium=default-template&filter=next.js&utm_source=create-next-app&utm_campaign=create-next-app-readme) from the creators of Next.js.

Check out our [Next.js deployment documentation](https://nextjs.org/docs/app/building-your-application/deploying) for more details.


# Frontend Route Structure

## Stack
- **Framework**: Next.js 16 App Router
- **UI**: shadcn/ui + Tailwind CSS
- **Data fetching**: TanStack Query v5
- **HTTP client**: Axios (với auto refresh token interceptor)
- **Theme**: next-themes

---

## Cấu trúc thư mục đầy đủ

```
src/
├── app/
│   ├── (auth)/                          # Route group — không tạo URL segment
│   │   ├── layout.tsx                   # Centered card layout
│   │   ├── login/page.tsx               # /login
│   │   ├── register/page.tsx            # /register
│   │   ├── forgot-password/page.tsx     # /forgot-password
│   │   └── reset-password/page.tsx      # /reset-password?token=xxx
│   │
│   ├── (main)/                          # Route group — authenticated
│   │   ├── layout.tsx                   # App shell: SidebarProvider + AppSidebar + AppHeader
│   │   │
│   │   ├── home/page.tsx                # /home
│   │   │
│   │   ├── [workspaceSlug]/
│   │   │   ├── layout.tsx               # Validate workspace + WorkspaceProvider
│   │   │   ├── page.tsx                 # /[workspaceSlug]
│   │   │   │
│   │   │   ├── settings/
│   │   │   │   ├── layout.tsx           # Settings tabs nav
│   │   │   │   ├── page.tsx             # redirect → general
│   │   │   │   ├── general/page.tsx     # Tên, icon, slug
│   │   │   │   ├── members/page.tsx     # Members + invitations
│   │   │   │   ├── permissions/page.tsx # RBAC
│   │   │   │   └── billing/page.tsx     # Plan & billing
│   │   │   │
│   │   │   ├── docs/
│   │   │   │   ├── page.tsx             # /[workspaceSlug]/docs
│   │   │   │   └── [pageId]/
│   │   │   │       └── page.tsx         # /[workspaceSlug]/docs/[pageId]
│   │   │   │
│   │   │   └── boards/
│   │   │       ├── page.tsx             # /[workspaceSlug]/boards
│   │   │       └── [boardId]/
│   │   │           ├── page.tsx         # Kanban view (default)
│   │   │           ├── list/page.tsx    # List view
│   │   │           ├── calendar/page.tsx# Calendar view
│   │   │           └── card/[cardId]/
│   │   │               └── page.tsx     # Card detail (full-page)
│   │   │
│   │   └── account/
│   │       ├── layout.tsx               # Account settings tabs
│   │       ├── profile/page.tsx         # /account/profile
│   │       ├── security/page.tsx        # /account/security
│   │       └── notifications/page.tsx   # /account/notifications
│   │
│   ├── invite/[token]/page.tsx          # /invite/[token] — public
│   │
│   ├── layout.tsx                       # Root: providers (Query, Theme)
│   ├── page.tsx                         # / → redirect logic
│   ├── error.tsx                        # Global error boundary
│   └── not-found.tsx                    # 404
│
├── components/
│   ├── providers/
│   │   ├── query-provider.tsx           # TanStack QueryClientProvider
│   │   ├── theme-provider.tsx           # next-themes wrapper
│   │   └── workspace-provider.tsx       # Workspace context
│   ├── layout/
│   │   ├── app-sidebar.tsx              # Sidebar chính + workspace switcher
│   │   └── app-header.tsx               # Header + search + notifications
│   ├── auth/                            # LoginForm, RegisterForm, ...
│   ├── workspace/                       # WorkspaceSidebar, WorkspaceOverview, ...
│   ├── docs/                            # PageEditor, PageHeader, ...
│   ├── boards/                          # BoardKanban, CardDetail, ...
│   └── shared/                          # NotificationList, SearchCommand, ...
│
├── lib/
│   ├── api/
│   │   └── client.ts                    # Axios + refresh token interceptor
│   ├── auth/
│   │   └── session.ts                   # getSession() cho server components
│   ├── query/
│   │   └── server-query-client.ts       # getQueryClient() cho prefetch
│   └── hooks/
│       ├── query-keys.ts                # Centralized query keys factory
│       ├── use-auth.ts                  # useMe, useLogin, useLogout, ...
│       ├── use-workspaces.ts            # useWorkspace, useWorkspaceMembers, ...
│       ├── use-pages.ts                 # usePage, usePageBlocks, useCreatePage, ...
│       ├── use-boards.ts                # useBoard, useFullBoard, useCard, ...
│       └── use-shared.ts                # useComments, useNotifications, useSearch
│
├── types/
│   └── index.ts                         # Tất cả TypeScript types
│
└── middleware.ts                        # Auth guard + redirect logic
```

---

## URL Map

| URL | Mô tả |
|-----|-------|
| `/` | Redirect → workspace hoặc `/home` |
| `/login` | Đăng nhập |
| `/register` | Đăng ký |
| `/forgot-password` | Quên mật khẩu |
| `/reset-password?token=xxx` | Đặt lại mật khẩu |
| `/home` | Dashboard tổng quan |
| `/invite/[token]` | Chấp nhận lời mời (public) |
| `/[workspaceSlug]` | Workspace home |
| `/[workspaceSlug]/settings/general` | Cài đặt chung |
| `/[workspaceSlug]/settings/members` | Quản lý thành viên |
| `/[workspaceSlug]/settings/permissions` | Phân quyền |
| `/[workspaceSlug]/settings/billing` | Thanh toán |
| `/[workspaceSlug]/docs` | Danh sách pages |
| `/[workspaceSlug]/docs/[pageId]` | Notion-like editor |
| `/[workspaceSlug]/boards` | Danh sách boards |
| `/[workspaceSlug]/boards/[boardId]` | Kanban view |
| `/[workspaceSlug]/boards/[boardId]/list` | List view |
| `/[workspaceSlug]/boards/[boardId]/calendar` | Calendar view |
| `/[workspaceSlug]/boards/[boardId]/card/[cardId]` | Card detail |
| `/account/profile` | Hồ sơ cá nhân |
| `/account/security` | Bảo mật (sessions, password) |
| `/account/notifications` | Cài đặt thông báo |

---

## Quy tắc quan trọng

### Token flow
- `access_token` lưu **in-memory** (không localStorage) — bảo mật hơn
- `refresh_token` lưu **httpOnly cookie** — backend set
- Axios interceptor tự động refresh khi nhận 401

### Server vs Client components
- Layout + page mặc định là **Server Components** — fetch data trực tiếp
- Component có `useState`, `useEffect`, hooks → thêm `'use client'`
- Prefetch data bằng `getQueryClient()` ở server, truyền xuống qua `HydrationBoundary`

### Query invalidation
- Dùng `queryKeys` factory để invalidate chính xác, tránh over-fetch
- Sau mutation → `invalidateQueries` đúng scope, không `refetchAll`

### Middleware
- Tất cả routes trong `(main)` đều cần auth — middleware kiểm tra cookie
- `invite/[token]` là public — không cần login để xem invitation

---

## Setup

```bash
npm install
npm run dev
```

Cần thêm `.env.local`:
```
NEXT_PUBLIC_API_URL=http://localhost:5000/api
```
