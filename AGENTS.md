# AGENTS.md — Notrelix Project Rules

> **Mục đích:** File này định nghĩa toàn bộ conventions, rules và hướng dẫn cho AI agents (Claude, Copilot, Cursor...) khi làm việc với dự án Notrelix — một SaaS workspace kết hợp Notion-like docs và Trello-like boards với two-way calendar sync.
>
> **Đọc file này trước khi thực hiện bất kỳ thay đổi nào.**

---

## 1. Tổng quan dự án

```
Notrelix
├── Vision:     Workspace kết hợp Notion (docs) + Trello (boards) + Calendar sync
├── Backend:    .NET 10 / ASP.NET Core / Entity Framework Core / PostgreSQL
├── Frontend:   Next.js 15 App Router / TypeScript / shadcn/ui / TanStack Query
├── Cache:      Redis 7 (sessions + pub/sub + queue)
├── Storage:    S3/R2 (file attachments — không lưu binary vào DB)
├── Auth:       JWT (access token in-memory) + refresh token (httpOnly cookie)
└── Multi-tenant: workspace slug-based routing (/[workspaceSlug]/...)
```

---

## 2. Domain Architecture

Dự án có **7 domains** — mọi thay đổi phải nằm đúng domain:

```
Domain 1 — Identity & Auth     users, user_profiles, sessions, oauth_accounts
Domain 2 — Workspace           workspaces, workspace_members, workspace_invitations
Domain 3 — Document (Notion)   pages, blocks
Domain 4 — Board (Trello)      boards, board_members, board_views, lists, labels,
                                cards, card_members, card_labels, card_links,
                                checklists, checklist_items
Domain 5 — Calendar Sync       calendar_integrations, calendar_events
Domain 6 — Shared/Cross        comments, page_mentions, attachments, reactions,
                                permissions, notifications, activity_logs
Domain 7 — Extensibility       webhooks, automations, integrations, audit_snapshots
```

**Rule:** Không tạo table mới khi chưa xác định rõ domain. Polymorphic tables (comments, attachments, permissions, reactions) là shared — không tạo bản duplicate cho từng domain.

---

## 3. Backend Rules (.NET / ASP.NET Core)

### 3.1 Project Structure

```
Notrelix.sln
├── Notrelix.Domain/              # Entities, Value Objects, Domain Events
│   ├── Entities/
│   │   ├── Identity/            # User, UserProfile, Session, OAuthAccount
│   │   ├── Workspace/           # Workspace, WorkspaceMember, WorkspaceInvitation
│   │   ├── Document/            # Page, Block
│   │   ├── Board/               # Board, List, Card, Checklist, Label...
│   │   ├── Calendar/            # CalendarIntegration, CalendarEvent
│   │   └── Shared/              # Comment, Attachment, Permission, Notification...
│   ├── Enums/
│   ├── Events/                  # Domain events (CardCreated, PagePublished...)
│   └── Exceptions/
│
├── Notrelix.Application/         # Use cases, CQRS Commands/Queries, DTOs
│   ├── Common/
│   │   ├── Interfaces/          # IRepository, IUnitOfWork, ICurrentUser...
│   │   └── Behaviors/           # ValidationBehavior, LoggingBehavior...
│   ├── Identity/
│   ├── Workspace/
│   ├── Document/
│   ├── Board/
│   ├── Calendar/
│   └── Shared/
│
├── Notrelix.Infrastructure/      # EF Core, Repos, External services
│   ├── Data/
│   │   ├── ApplicationDbContext.cs
│   │   ├── Configurations/      # IEntityTypeConfiguration per entity
│   │   └── Migrations/
│   ├── Repositories/
│   ├── Services/
│   │   ├── CalendarSyncService.cs
│   │   ├── RedisService.cs
│   │   └── StorageService.cs    # S3/R2
│   └── BackgroundJobs/
│
└── Notrelix.Api/                 # Controllers, Minimal API endpoints
    ├── Endpoints/               # Minimal API grouped by domain
    ├── Middleware/
    └── Filters/
```

### 3.2 Naming Conventions

```csharp
// Entities — PascalCase, singular
public class WorkspaceMember { }
public class CalendarIntegration { }

// Properties — PascalCase
public Guid Id { get; set; }
public string Title { get; set; } = string.Empty;
public Guid? LinkedPageId { get; set; }   // nullable FK

// Enums — PascalCase value
public enum CardPriority { Urgent, High, Medium, Low }
public enum SyncDirection { Push, Pull, Both }
public enum BlockType { Paragraph, Heading1, Heading2, Heading3,
                        BulletedList, NumberedList, Toggle, Quote,
                        Callout, Code, Divider, Image, Video, File,
                        Embed, Bookmark, Table, TableRow, Todo,
                        CardRef, ChildPage, ColumnList, Column }

// Commands — Verb + Noun + Command
public record CreateCardCommand(string Title, Guid ListId) : IRequest<CardDto>;
public record LinkPageToCardCommand(Guid CardId, Guid PageId) : IRequest;

// Queries — Get + Noun + Query
public record GetPageBlocksQuery(Guid PageId) : IRequest<IEnumerable<BlockDto>>;

// DTOs — Noun + Dto
public record CardDto(Guid Id, string Title, Guid? LinkedPageId, ...);
```

### 3.3 Entity Rules

```csharp
// RULE: Mọi entity kế thừa từ BaseEntity, không khai báo lại các property đã có
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public abstract class AuditableEntity : BaseEntity
{
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
}

// RULE: Không override CreatedBy hay UpdatedBy trong subclass
// WRONG:
public class Card : AuditableEntity
{
    public Guid? CreatedBy { get; set; }  // CS0108 warning — KHÔNG làm thế này
}

// RULE: position luôn là double (fractional indexing)
public double Position { get; set; } = 0;

// RULE: soft delete phải có cả flag và timestamp
public bool IsDeleted { get; set; } = false;
public DateTime? DeletedAt { get; set; }
```

### 3.4 EF Core Configuration

```csharp
// RULE: Mỗi entity có file configuration riêng
public class CardConfiguration : IEntityTypeConfiguration<Card>
{
    public void Configure(EntityTypeBuilder<Card> builder)
    {
        builder.ToTable("cards");

        // RULE: Tên cột snake_case
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.LinkedPageId).HasColumnName("linked_page_id");
        builder.Property(x => x.Position).HasColumnType("float8");

        // RULE: JSONB cho properties/metadata
        builder.Property(x => x.Cover)
            .HasColumnType("jsonb")
            .HasDefaultValue("{}");

        // RULE: Index phải được define trong configuration, không trong migration
        builder.HasIndex(x => new { x.ListId, x.Position })
            .HasFilter("is_deleted = false")
            .HasDatabaseName("idx_cards_list_pos");

        builder.HasIndex(x => x.LinkedPageId)
            .HasFilter("linked_page_id IS NOT NULL")
            .HasDatabaseName("idx_cards_linked_page");
    }
}
```

### 3.5 API Conventions

```
// RULE: RESTful endpoints, grouped by domain
GET    /api/workspaces/{slug}
GET    /api/workspaces/{slug}/boards
POST   /api/boards
GET    /api/boards/{boardId}/full          // board + lists + cards
PATCH  /api/cards/{cardId}
PATCH  /api/cards/{cardId}/move            // { listId, position }
POST   /api/cards/{cardId}/link-page       // { pageId }
DELETE /api/cards/{cardId}/link-page

GET    /api/pages/{pageId}/blocks
PATCH  /api/blocks/{blockId}
POST   /api/blocks/reorder                 // batch position update

GET    /api/workspaces/{slug}/calendar     // unified calendar events
POST   /api/calendar/integrations          // connect Google Calendar
POST   /api/calendar/sync                  // manual trigger sync

// RULE: Response luôn wrap trong ApiResponse<T>
{
  "success": true,
  "data": { ... },
  "error": null
}

// RULE: Lỗi trả về problem details (RFC 7807)
{
  "type": "https://Notrelix.com/errors/not-found",
  "title": "Resource not found",
  "status": 404,
  "detail": "Page with id 'xxx' not found",
  "traceId": "..."
}
```

### 3.6 Database Rules

```sql
-- RULE: Tên table snake_case, số nhiều
-- RULE: PK luôn là UUID, DEFAULT gen_random_uuid()
-- RULE: FK tên: {table_singular}_id
-- RULE: Timestamps suffix _at
-- RULE: Boolean prefix is_

-- RULE: position luôn là FLOAT8 (fractional indexing)
-- KHÔNG dùng INTEGER cho position
position FLOAT8 NOT NULL DEFAULT 0

-- RULE: JSONB cho flexible data, không tạo column riêng cho từng variant
properties JSONB NOT NULL DEFAULT '{}'

-- RULE: Soft delete phải có cả hai
is_deleted BOOLEAN NOT NULL DEFAULT false
deleted_at TIMESTAMPTZ

-- RULE: Index partial để loại deleted rows
CREATE INDEX idx_cards_list_pos ON cards(list_id, position)
    WHERE is_deleted = false;

-- RULE: Polymorphic tables (comments, attachments...) phải có workspace_id
-- để support Row Level Security trong tương lai
workspace_id UUID NOT NULL REFERENCES workspaces(id)

-- RULE: Không lưu file binary vào DB
-- attachments.url luôn trỏ về S3/R2, không phải base64

-- RULE: activity_logs phải là PARTITIONED table
-- Không được bỏ PARTITION BY RANGE(created_at)
```

### 3.7 Background Jobs & Redis

```csharp
// RULE: Calendar sync phải chạy async qua queue — không sync trong request
// WRONG:
public async Task<IActionResult> UpdateCard([FromBody] UpdateCardDto dto)
{
    await _calendarSyncService.SyncNow(card);  // BLOCK request
}

// CORRECT:
public async Task<IActionResult> UpdateCard([FromBody] UpdateCardDto dto)
{
    await _mediator.Send(new UpdateCardCommand(dto));
    await _queue.EnqueueAsync(new CalendarSyncJob(card.Id)); // fire and forget
}

// RULE: Notifications phải qua Redis pub/sub, không poll DB
// RULE: Cache key convention: {domain}:{id}:{field}
// Examples:
//   workspace:{id}:members          TTL 5m
//   user:{id}:workspaces            TTL 5m
//   page:{id}:blocks                TTL 30s
//   session:{token}                 TTL = expires_at
//   notification:{userId}:unread    TTL 0 (invalidate on write)
```

---

## 4. Frontend Rules (Next.js / TypeScript)

### 4.1 Project Structure

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
│   │   │   └── [workspaceSlug]/               # ── Dynamic workspace ──
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

### 4.2 Naming Conventions

```typescript
// Files — kebab-case
board-view-tabs.tsx
use-board-docs-panel.ts
docs-panel-skeleton.tsx

// Components — PascalCase
export function BoardViewTabs() {}
export function DocsPanel() {}

// Hooks — camelCase, prefix use
export function useBoardDocsPanel() {}
export function usePageBlocks(pageId: string) {}

// Types/Interfaces — PascalCase
interface WorkspaceMember { }
type CardPriority = 'urgent' | 'high' | 'medium' | 'low'
type BlockType = 'paragraph' | 'heading1' | 'card_ref' | ...

// Query keys — factory pattern
export const queryKeys = {
  boards: {
    list: (workspaceId: string) => ['boards', workspaceId] as const,
    detail: (boardId: string)   => ['boards', 'detail', boardId] as const,
    fullBoard: (boardId: string) => ['boards', boardId, 'full'] as const,
  },
  pages: {
    tree: (workspaceId: string) => ['pages', workspaceId, 'tree'] as const,
    blocks: (pageId: string)    => ['pages', pageId, 'blocks'] as const,
  },
  // ...
}

// Constants — SCREAMING_SNAKE_CASE
const MIN_PANEL_WIDTH = 320
const STORAGE_KEY = 'docs-panel-width'
```

### 4.3 Component Rules

```typescript
// RULE: Server Component là default — chỉ thêm 'use client' khi thật sự cần
// Cần 'use client' khi: useState, useEffect, event handlers, browser APIs, hooks

// RULE: Props interface đặt ngay trước component
interface Props {
  pageId:        string
  workspaceSlug: string
  compact?:      boolean
}

export function PageEditor({ pageId, workspaceSlug, compact }: Props) {}

// RULE: Async Server Components fetch data trực tiếp
export default async function PageDetailPage({ params }: { params: Promise<{ pageId: string }> }) {
  const { pageId } = await params  // PHẢI await params trong Next.js 15
  const queryClient = getQueryClient()
  await queryClient.prefetchQuery({
    queryKey: queryKeys.pages.detail(pageId),
    queryFn: () => apiClient.get<Page>(`/pages/${pageId}`).then(r => r.data),
  })
  return (
    <HydrationBoundary state={dehydrate(queryClient)}>
      <PageEditor pageId={pageId} />
    </HydrationBoundary>
  )
}

// RULE: Lazy load heavy components
const BlockEditor = dynamic(
  () => import('@/components/docs/block-editor').then(m => m.BlockEditor),
  { ssr: false, loading: () => <EditorSkeleton /> }
)

// RULE: Mỗi route có loading.tsx và error.tsx riêng
// loading.tsx — skeleton, không spinner đơn độc
// error.tsx   — phải là 'use client', có reset() function

// RULE: Không dùng HTML <form> tag trong React components
// ĐÚNG: <button onClick={handleSubmit}>
// SAI:  <form onSubmit={handleSubmit}><button type="submit">
```

### 4.4 Data Fetching Rules

```typescript
// RULE: Query keys phải dùng factory từ query-keys.ts — không hardcode string
// SAI:
useQuery({ queryKey: ['boards', boardId], ... })
// ĐÚNG:
useQuery({ queryKey: queryKeys.boards.detail(boardId), ... })

// RULE: staleTime theo domain
// - User/workspace data:  5 * 60 * 1000  (5 phút)
// - Board/list/card:      30 * 1000       (30 giây)
// - Page blocks:          10 * 1000       (10 giây, edit thường xuyên)
// - Notifications unread: 15 * 1000       (15 giây)
// - Search results:       10 * 1000       (10 giây)

// RULE: invalidateQueries phải dùng đúng scope — không invalidate toàn bộ
// SAI:
queryClient.invalidateQueries()
queryClient.invalidateQueries({ queryKey: ['boards'] })  // quá rộng
// ĐÚNG:
queryClient.invalidateQueries({ queryKey: queryKeys.boards.list(workspaceId) })

// RULE: Mutation onSuccess phải update cache — không refetch nếu có thể
// Optimistic update cho UX smooth:
useMutation({
  mutationFn: cardsApi.move,
  onMutate: async (variables) => {
    await queryClient.cancelQueries({ queryKey: queryKeys.boards.fullBoard(boardId) })
    const previous = queryClient.getQueryData(queryKeys.boards.fullBoard(boardId))
    // update cache optimistically
    return { previous }
  },
  onError: (_, __, context) => {
    queryClient.setQueryData(queryKeys.boards.fullBoard(boardId), context?.previous)
  },
  onSettled: () => {
    queryClient.invalidateQueries({ queryKey: queryKeys.boards.fullBoard(boardId) })
  },
})

// RULE: Không poll DB cho notifications — dùng refetchInterval
// chỉ đến khi có WebSocket
useQuery({
  queryKey: queryKeys.notifications.unreadCount(),
  queryFn: notificationsApi.unreadCount,
  refetchInterval: 15 * 1000,
})
```

### 4.5 Routing Rules

```typescript
// RULE: URL state ưu tiên hơn React state cho data có thể share
// Docs panel: ?doc=[pageId] — shareable, browser back/forward hoạt động
// Card modal: intercepting route — URL thay đổi nhưng board vẫn render

// RULE: Board docs panel dùng searchParam, không parallel route @docs
// Lý do: @modal slot đã được dùng, thêm @docs sẽ conflict
// Xem: useBoardDocsPanel hook

// RULE: params trong Next.js 15 phải được await
// SAI:
export default function Page({ params }: { params: { boardId: string } }) {
  const { boardId } = params  // sync access
}
// ĐÚNG:
export default async function Page({ params }: { params: Promise<{ boardId: string }> }) {
  const { boardId } = await params
}

// RULE: router.push với { scroll: false } khi chỉ update searchParam
router.push(`${pathname}?${params.toString()}`, { scroll: false })

// RULE: Middleware xử lý auth ở Edge — không query DB, chỉ verify JWT
// Dùng jose (edge-compatible), không dùng jsonwebtoken
```

### 4.6 Token & Auth Rules

```typescript
// RULE: access_token lưu in-memory (không localStorage, không sessionStorage)
// RULE: refresh_token lưu httpOnly cookie — backend set, frontend không đọc được
// RULE: Axios interceptor tự động refresh khi nhận 401
// Xem: src/lib/api/client.ts

// RULE: getSession() chỉ dùng trong Server Components
// Client Components dùng useMe() hook

// RULE: Không expose JWT_SECRET ra client-side
// RULE: Không lưu sensitive data vào localStorage
```

### 4.7 TypeScript Rules

```typescript
// RULE: Không dùng 'any' — dùng 'unknown' nếu cần
// RULE: Tất cả types định nghĩa trong src/types/index.ts
// RULE: API response phải typed
// RULE: Không dùng type assertion (as Type) trừ khi cần thiết

// RULE: Enum mở rộng được dùng union type, không enum TypeScript
// ĐÚNG:
type BlockType = 'paragraph' | 'heading1' | 'card_ref' | 'todo'
// SAI:
enum BlockType { Paragraph = 'paragraph', ... }

// RULE: Optional chaining và nullish coalescing thay vì if null check
const title = page?.title ?? 'Untitled'
const count = unread?.count ?? 0
```

---

## 5. Shared Rules (cả Frontend và Backend)

### 5.1 Domain Boundaries

```
RULE: Không cross domain trực tiếp
- Document domain KHÔNG import từ Board domain
- Board domain có thể reference Page qua ID (linked_page_id)
  nhưng không import PageService vào CardService
- Calendar domain nhận events từ cả Board và Document
  qua domain events, không gọi trực tiếp

RULE: Block type 'card_ref' là cầu nối Notion → Trello
  properties: { card_id: uuid, display: 'inline'|'full' }
  Frontend fetch card data riêng khi render block này

RULE: cards.linked_page_id là cầu nối Trello → Notion
  Route: /[ws]/boards/[boardId]/card/[cardId]/docs/[pageId]
  Khi card bị xóa → linked_page_id SET NULL, page vẫn tồn tại
```

### 5.2 Calendar Sync Rules

```
RULE: Tất cả calendar sync phải async (queue-based)
  Không bao giờ call Google Calendar API trong request/response cycle

RULE: sync_hash phải được update sau mỗi lần sync thành công
  Hash = MD5({ title, due_date/deadline, description })
  Nếu hash khớp → skip sync, không call external API

RULE: Conflict detection:
  Nếu cả app và external đều thay đổi → tạo notification 'calendar.conflict'
  Không tự động resolve conflict — để user quyết định

RULE: Resource types được sync: 'card' (due_date) và 'page' (deadline)
  Không sync các fields khác (title, description) trong phase 1
```

### 5.3 Permission Resolution

```
RULE: Permission check theo thứ tự (application layer, không trong DB)
  1. Check permissions table cho resource_id cụ thể
  2. Nếu không có → walk up parent (page → parent page → workspace)
  3. Fallback về workspace_members.role

RULE: Cache permission result trong Redis
  Key: perm:{userId}:{resourceType}:{resourceId}
  TTL: 60 giây
  Invalidate khi: permissions table update

RULE: is_personal workspace = owner luôn có full quyền
  Middleware skip workspace_members check
```

### 5.4 Fractional Indexing

```
RULE: position luôn là FLOAT8/double
  Insert between 1.0 and 2.0 → 1.5
  Insert between 1.0 and 1.5 → 1.25

RULE: Khi precision quá nhỏ (< 1e-10 gap) → trigger rebalance
  Rebalance = batch update toàn bộ items trong container
  Assign lại position: 1.0, 2.0, 3.0, 4.0...

RULE: Áp dụng cho: pages, blocks, lists, cards, checklists, checklist_items
```

### 5.5 Soft Delete

```
RULE: Tất cả delete phải là soft delete trừ: sessions, reactions, card_labels, card_members
RULE: Soft delete = set is_deleted = true + deleted_at = now()
RULE: Mọi query mặc định filter WHERE is_deleted = false
RULE: Không cascade hard delete khi parent bị soft delete
  Ngoại lệ: blocks CASCADE khi page bị hard delete (chỉ xảy ra sau 30 ngày)
```

### 5.6 Activity Logging

```
RULE: Mọi write operation quan trọng phải log vào activity_logs
  Quan trọng = tạo, cập nhật, xóa, di chuyển, assign, publish

RULE: action format: '{domain}.{verb}'
  'card.created' | 'card.moved' | 'card.linked_page.set'
  'page.published' | 'page.archived' | 'page.deadline.set'
  'board.created' | 'board.archived'
  'calendar.synced' | 'calendar.conflict'
  'member.invited' | 'member.role.changed'

RULE: resource_title là snapshot — không JOIN sau này
  Lưu title tại thời điểm action, dù sau đó title thay đổi

RULE: activity_logs là append-only — không UPDATE, không soft delete
  DROP partition để purge data cũ
```

---

## 6. Git & Code Review Rules

### 6.1 Branch Naming

```
feature/{domain}/{description}    feature/board/card-link-page
fix/{domain}/{description}        fix/calendar/sync-conflict-detection
refactor/{scope}/{description}    refactor/db/add-board-views-table
chore/{description}               chore/update-dependencies
```

### 6.2 Commit Message

```
{type}({domain}): {description}

feat(board):     add linked_page_id to cards
feat(calendar):  implement two-way google calendar sync
fix(docs):       prevent race condition in block reorder
refactor(auth):  extract token refresh logic to interceptor
chore(db):       add migration for board_views table
test(board):     add unit tests for card move command
```

### 6.3 PR Checklist

```
□ Migration file được tạo đúng convention (snake_case, có index)
□ Không có CS0108 warning (hidden member)
□ position field dùng double/FLOAT8, không int/INTEGER
□ Soft delete đúng pattern (is_deleted + deleted_at)
□ Calendar sync chạy async qua queue
□ Query keys dùng factory, không hardcode string
□ params trong Next.js 15 được await
□ Không có 'any' type trong TypeScript
□ activity_logs được update cho write operations quan trọng
□ Redis cache invalidated khi data thay đổi
```

---

## 7. Môi trường và biến Environment

### Backend (.NET)

```
# Database
CONNECTION_STRING=Host=localhost;Database=Notrelix;Username=...;Password=...

# Redis
REDIS_CONNECTION=localhost:6379

# JWT
JWT_SECRET=<256-bit secret>
JWT_ACCESS_TOKEN_EXPIRY=15m
JWT_REFRESH_TOKEN_EXPIRY=30d

# S3/R2
S3_BUCKET=Notrelix-attachments
S3_REGION=auto
S3_ACCESS_KEY=...
S3_SECRET_KEY=...
S3_ENDPOINT=https://<account>.r2.cloudflarestorage.com

# Google Calendar OAuth
GOOGLE_CLIENT_ID=...
GOOGLE_CLIENT_SECRET=...
GOOGLE_REDIRECT_URI=https://app.Notrelix.com/api/calendar/callback

# App
ROOT_DOMAIN=Notrelix.com
APP_URL=https://app.Notrelix.com
```

### Frontend (Next.js)

```
NEXT_PUBLIC_API_URL=http://localhost:5000/api
NEXT_PUBLIC_APP_URL=http://localhost:3000
NEXT_PUBLIC_ROOT_DOMAIN=localhost:3000

# Không được có NEXT_PUBLIC_ prefix cho secrets
JWT_SECRET=<same as backend>
```

---

## 8. Performance Checklist

```
□ DB: Mọi query có WHERE clause phải có index tương ứng
□ DB: N+1 query — dùng Include() trong EF Core hoặc JOIN
□ DB: Không query trong loop
□ DB: Pagination cho mọi list endpoint (cursor-based cho realtime data)
□ FE: Server Components fetch data, không để client fetch rồi waterfall
□ FE: Prefetch data trong layout/page với HydrationBoundary
□ FE: Heavy components (BlockEditor) phải dynamic import với ssr: false
□ FE: Images phải dùng next/image với proper sizing
□ FE: Không invalidate toàn bộ query cache — invalidate đúng scope
□ Redis: Cache hot data (workspace members, permissions, session)
□ Redis: Queue cho async jobs (calendar sync, email notifications)
□ S3: Presigned URL cho file upload — không proxy qua server
```

---

## 9. Điều tuyệt đối KHÔNG làm

```
❌ Lưu file binary (image, pdf...) vào PostgreSQL
❌ Sync calendar trong request/response cycle (phải async)
❌ Poll DB cho notifications (dùng Redis pub/sub)
❌ Dùng INTEGER cho position (phải FLOAT8)
❌ Hardcode query key string (dùng queryKeys factory)
❌ Gọi DB trong middleware (chỉ verify JWT)
❌ Tạo duplicate polymorphic tables (dùng shared comments/attachments)
❌ Cross-domain service calls trực tiếp (dùng domain events)
❌ Lưu access_token vào localStorage (in-memory only)
❌ Override properties từ base class (CS0108)
❌ Đồng bộ hóa block edits qua HTTP (cần WebSocket + CRDT)
❌ Thêm NoSQL DB khi chưa có bottleneck thực tế
❌ Tách microservices trước khi đạt 500k users
```

---

*Cập nhật lần cuối: reflect schema refactored 28 tables, 7 domains*
*Stack: .NET 10 / Next.js 15 / PostgreSQL 16 / Redis 7 / S3*
