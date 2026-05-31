# Notrelix Domains Reference

Quick reference for the 7 domains in Notrelix architecture.

## Domain Overview

Notrelix is organized into 7 distinct domains, each with clear responsibilities and boundaries.

```
1. Identity & Auth
2. Workspace
3. Document (Notion-like)
4. Board (Trello-like)
5. Calendar Sync
6. Shared/Cross-cutting
7. Extensibility
```

---

## 1. Identity & Auth

**Purpose:** User authentication, authorization, and profile management

### Entities

- **User** — Core user account
  - `id`, `email`, `password_hash`, `name`, `avatar_url`
  - `email_verified`, `is_active`, `created_at`, `updated_at`

- **UserProfile** — Extended user information
  - `user_id`, `bio`, `timezone`, `language`, `theme`
  - `notification_preferences` (JSONB)

- **Session** — Active user sessions
  - `id`, `user_id`, `refresh_token`, `expires_at`
  - `ip_address`, `user_agent`, `created_at`

- **OAuthAccount** — Third-party OAuth connections
  - `id`, `user_id`, `provider`, `provider_user_id`
  - `access_token`, `refresh_token`, `expires_at`

### Key Operations

- Register user
- Login (email/password or OAuth)
- Refresh access token
- Logout (invalidate session)
- Update profile
- Forgot/reset password
- Verify email

### Backend Location

```
backend/Notrelix.Domain/Entities/Identity/
backend/Notrelix.Application/Features/Identity/
backend/Notrelix.API/Endpoints/Auth/
```

### Frontend Location

```
frontend/features/auth/
```

---

## 2. Workspace

**Purpose:** Multi-tenant workspace management and member collaboration

### Entities

- **Workspace** — Tenant container
  - `id`, `name`, `slug`, `icon`, `description`
  - `is_personal`, `created_at`, `updated_at`, `created_by`

- **WorkspaceMember** — User membership in workspace
  - `id`, `workspace_id`, `user_id`, `role`
  - `position`, `is_deleted`, `deleted_at`, `created_at`
  - **Roles:** `owner`, `admin`, `member`, `guest`

- **WorkspaceInvitation** — Pending invitations
  - `id`, `workspace_id`, `email`, `role`, `token`
  - `expires_at`, `accepted_at`, `created_by`, `created_at`

### Key Operations

- Create workspace
- Get user workspaces
- Invite member
- Accept invitation
- Update member role
- Remove member
- Update workspace settings

### Backend Location

```
backend/Notrelix.Domain/Entities/Workspace/
backend/Notrelix.Application/Features/Workspace/
backend/Notrelix.API/Endpoints/Workspaces/
```

### Frontend Location

```
frontend/features/workspace/
```

---

## 3. Document (Notion-like)

**Purpose:** Block-based document editing with hierarchical pages

### Entities

- **Page** — Document container
  - `id`, `workspace_id`, `parent_page_id`, `title`
  - `icon`, `cover` (JSONB), `is_published`, `deadline`
  - `position`, `is_deleted`, `deleted_at`, `created_at`, `updated_at`

- **Block** — Content block within page
  - `id`, `page_id`, `parent_block_id`, `type`, `content`
  - `properties` (JSONB), `position`
  - `is_deleted`, `deleted_at`, `created_at`, `updated_at`
  - **Types:** `paragraph`, `heading1`, `heading2`, `heading3`, `bulleted_list`, `numbered_list`, `toggle`, `quote`, `callout`, `code`, `divider`, `image`, `video`, `file`, `embed`, `bookmark`, `table`, `table_row`, `todo`, `card_ref`, `child_page`, `column_list`, `column`

### Key Operations

- Create page
- Get page tree (hierarchical)
- Get page blocks
- Create/update/delete block
- Move block (reorder)
- Publish page
- Set page deadline
- Link card to page (via `card_ref` block)

### Backend Location

```
backend/Notrelix.Domain/Entities/Document/
backend/Notrelix.Application/Features/Document/
backend/Notrelix.API/Endpoints/Document/
```

### Frontend Location

```
frontend/features/docs/
```

### Special Block Type

**`card_ref`** — Reference to a Board card
- Properties: `{ card_id: uuid, display: 'inline' | 'full' }`
- Bridges Document → Board domains

---

## 4. Board (Trello-like)

**Purpose:** Kanban project management with cards, lists, and labels

### Entities

- **Board** — Project board
  - `id`, `workspace_id`, `title`, `description`, `color`
  - `is_private`, `position`, `is_deleted`, `deleted_at`
  - `created_at`, `updated_at`, `created_by`

- **BoardMember** — User access to board
  - `id`, `board_id`, `user_id`, `role`
  - `created_at`

- **BoardView** — Different board views
  - `id`, `board_id`, `type`, `name`, `config` (JSONB)
  - `is_default`, `created_at`, `updated_at`
  - **Types:** `kanban`, `list`, `calendar`, `timeline`

- **List** — Column in kanban board
  - `id`, `board_id`, `title`, `position`
  - `is_collapsed`, `is_deleted`, `deleted_at`
  - `created_at`, `updated_at`

- **Card** — Task/item in list
  - `id`, `list_id`, `title`, `description`
  - `position`, `due_date`, `linked_page_id`
  - `cover` (JSONB), `is_deleted`, `deleted_at`
  - `created_at`, `updated_at`, `created_by`

- **Label** — Color-coded tags
  - `id`, `board_id`, `name`, `color`
  - `created_at`, `updated_at`

- **CardLabel** — Many-to-many card ↔ label
  - `card_id`, `label_id`, `created_at`

- **CardMember** — Many-to-many card ↔ user
  - `card_id`, `user_id`, `created_at`

- **Checklist** — Todo list within card
  - `id`, `card_id`, `title`, `position`
  - `is_deleted`, `deleted_at`, `created_at`, `updated_at`

- **ChecklistItem** — Item in checklist
  - `id`, `checklist_id`, `content`, `is_checked`
  - `position`, `is_deleted`, `deleted_at`
  - `created_at`, `updated_at`

### Key Operations

- Create board
- Get boards in workspace
- Get full board (with lists and cards)
- Create/update/delete list
- Create/update/delete card
- Move card (between lists)
- Link page to card (`linked_page_id`)
- Assign member to card
- Add/remove label
- Create/update checklist

### Backend Location

```
backend/Notrelix.Domain/Entities/Board/
backend/Notrelix.Application/Features/Board/
backend/Notrelix.API/Endpoints/Boards/
backend/Notrelix.API/Endpoints/Cards/
backend/Notrelix.API/Endpoints/Lists/
backend/Notrelix.API/Endpoints/Labels/
backend/Notrelix.API/Endpoints/Checklists/
```

### Frontend Location

```
frontend/features/boards/
```

### Cross-Domain Link

**`linked_page_id`** — Reference to a Document page
- Bridges Board → Document domains
- When card deleted → `SET NULL` (page remains)

---

## 5. Calendar Sync

**Purpose:** Two-way synchronization with external calendars (Google Calendar)

### Entities

- **CalendarIntegration** — External calendar connection
  - `id`, `workspace_id`, `user_id`, `provider`
  - `provider_calendar_id`, `access_token`, `refresh_token`
  - `sync_direction`, `is_active`, `last_synced_at`
  - `created_at`, `updated_at`
  - **Providers:** `google`, `outlook`, `ical`
  - **Sync Directions:** `push`, `pull`, `both`

- **CalendarEvent** — Synced event record
  - `id`, `integration_id`, `resource_type`, `resource_id`
  - `provider_event_id`, `sync_hash`, `last_synced_at`
  - `created_at`, `updated_at`
  - **Resource Types:** `card`, `page`

### Key Operations

- Connect Google Calendar (OAuth)
- Disconnect calendar
- Trigger manual sync
- Handle webhook from provider
- Detect conflicts (both sides changed)

### Sync Rules

- **Cards:** Sync `due_date` to calendar event
- **Pages:** Sync `deadline` to calendar event
- **Conflict:** Create notification, let user decide
- **Hash:** MD5 of `{ title, date, description }` to detect changes

### Backend Location

```
backend/Notrelix.Domain/Entities/Calendar/
backend/Notrelix.Application/Features/Calendar/
backend/Notrelix.API/Endpoints/Calendar/
backend/Notrelix.Infrastructure/Services/CalendarSyncService.cs
```

### Frontend Location

```
frontend/features/calendar/
```

### Important Rules

- ❌ **NEVER** sync in request/response cycle (use queue)
- ✅ Always run sync jobs async via Redis queue
- ✅ Use `sync_hash` to avoid unnecessary API calls

---

## 6. Shared/Cross-Cutting

**Purpose:** Features used across multiple domains

### Entities

- **Comment** — Polymorphic comments
  - `id`, `workspace_id`, `resource_type`, `resource_id`
  - `content`, `created_by`, `is_deleted`, `deleted_at`
  - `created_at`, `updated_at`
  - **Resource Types:** `card`, `page`, `board`

- **Attachment** — File attachments
  - `id`, `workspace_id`, `resource_type`, `resource_id`
  - `file_name`, `file_size`, `mime_type`, `url`
  - `created_by`, `is_deleted`, `deleted_at`, `created_at`

- **Permission** — Granular permissions
  - `id`, `workspace_id`, `resource_type`, `resource_id`
  - `user_id`, `permission_type`, `created_at`
  - **Permission Types:** `view`, `edit`, `comment`, `admin`

- **Notification** — User notifications
  - `id`, `user_id`, `workspace_id`, `type`
  - `title`, `message`, `resource_type`, `resource_id`
  - `is_read`, `read_at`, `created_at`

- **ActivityLog** — Audit trail (append-only)
  - `id`, `workspace_id`, `user_id`, `action`
  - `resource_type`, `resource_id`, `resource_title`
  - `metadata` (JSONB), `created_at`
  - **Actions:** `card.created`, `page.published`, `member.invited`, etc.

- **Reaction** — Emoji reactions
  - `id`, `workspace_id`, `resource_type`, `resource_id`
  - `user_id`, `emoji`, `created_at`

### Key Operations

- Add comment
- Upload attachment (to S3/R2)
- Set permission
- Get notifications
- Mark notification as read
- Log activity

### Backend Location

```
backend/Notrelix.Domain/Entities/Shared/
backend/Notrelix.Application/Features/Shared/
backend/Notrelix.API/Endpoints/Comments/
backend/Notrelix.API/Endpoints/Activity/
```

### Frontend Location

```
frontend/features/notifications/
frontend/features/search/
```

### Important Rules

- ✅ All polymorphic tables include `workspace_id` (for RLS)
- ✅ Use `resource_type` + `resource_id` pattern
- ❌ Don't create duplicate tables per domain (use shared)
- ✅ ActivityLog is append-only (no UPDATE, no soft delete)

---

## 7. Extensibility

**Purpose:** Webhooks, automations, and third-party integrations

### Entities

- **Webhook** — Outgoing webhooks
  - `id`, `workspace_id`, `url`, `events` (array)
  - `secret`, `is_active`, `created_at`, `updated_at`

- **Automation** — Workflow automation rules
  - `id`, `workspace_id`, `name`, `trigger`, `actions` (JSONB)
  - `is_active`, `created_at`, `updated_at`

- **Integration** — Third-party app connections
  - `id`, `workspace_id`, `provider`, `config` (JSONB)
  - `is_active`, `created_at`, `updated_at`

- **AuditSnapshot** — Point-in-time snapshots
  - `id`, `workspace_id`, `resource_type`, `resource_id`
  - `snapshot_data` (JSONB), `created_at`, `created_by`

### Key Operations

- Create webhook
- Trigger webhook on events
- Create automation rule
- Execute automation
- Connect integration
- Create audit snapshot

### Backend Location

```
backend/Notrelix.Domain/Entities/Extensibility/
backend/Notrelix.Application/Features/Extensibility/
```

### Frontend Location

```
frontend/features/integrations/
```

---

## Domain Boundaries

### Rules

1. **No Direct Cross-Domain Imports**
   - Document domain CANNOT import from Board domain
   - Board domain CANNOT import from Document domain

2. **Use IDs for References**
   - Board → Document: `Card.linked_page_id` (nullable FK)
   - Document → Board: `Block.properties.card_id` (JSONB, no FK)

3. **Domain Events for Communication**
   - Calendar sync listens to `CardCreated`, `PagePublished` events
   - Don't call services directly across domains

4. **Shared Domain for Common Features**
   - Comments, attachments, permissions are in Shared domain
   - All domains can use Shared entities

### Cross-Domain Links

```
Board ──linked_page_id──> Document
  (Card)                    (Page)

Document ──card_id──> Board
  (Block type: card_ref)  (Card)

Calendar ──resource_id──> Board | Document
  (CalendarEvent)          (Card | Page)
```

---

## Quick Reference Table

| Domain | Entities | Key Features | Cross-Domain Links |
|--------|----------|--------------|-------------------|
| **Identity** | User, Session, OAuthAccount | Auth, profiles | None |
| **Workspace** | Workspace, Member, Invitation | Multi-tenancy | None |
| **Document** | Page, Block | Notion-like editor | → Board (card_ref) |
| **Board** | Board, List, Card, Label | Trello-like kanban | → Document (linked_page_id) |
| **Calendar** | Integration, Event | Two-way sync | → Board, Document |
| **Shared** | Comment, Attachment, Permission | Cross-cutting | All domains |
| **Extensibility** | Webhook, Automation | Integrations | All domains |

---

## See Also

- [AGENTS.md](../AGENTS.md) — Section 2: Domain Architecture
- [conventions.md](./conventions.md) — Naming conventions
- [api-patterns.md](./api-patterns.md) — Common API patterns
