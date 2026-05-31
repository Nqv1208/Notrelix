# Notrelix Naming Conventions

Quick reference for naming conventions across backend, frontend, and database.

## Backend (.NET / C#)

### Entities

```csharp
// Format: PascalCase, singular
public class Card { }
public class WorkspaceMember { }
public class CalendarIntegration { }
```

### Properties

```csharp
// Format: PascalCase
public Guid Id { get; set; }
public string Title { get; set; } = string.Empty;
public Guid? LinkedPageId { get; set; }  // Nullable FK
public DateTime CreatedAt { get; set; }
public bool IsDeleted { get; set; }
```

### Enums

```csharp
// Format: PascalCase for enum and values
public enum CardPriority
{
    Urgent,
    High,
    Medium,
    Low
}

public enum SyncDirection
{
    Push,
    Pull,
    Both
}

public enum BlockType
{
    Paragraph,
    Heading1,
    Heading2,
    BulletedList,
    NumberedList,
    CardRef,
    ChildPage
}
```

### Commands

```csharp
// Format: {Verb}{Noun}Command
public record CreateCardCommand(string Title, Guid ListId) : IRequest<CardDto>;
public record UpdatePageCommand(Guid Id, string Title) : IRequest<PageDto>;
public record LinkPageToCardCommand(Guid CardId, Guid PageId) : IRequest;
public record MoveCardCommand(Guid CardId, Guid ListId, double Position) : IRequest;

// Common verbs: Create, Update, Delete, Link, Unlink, Move, Archive, Publish, Assign
```

### Queries

```csharp
// Format: Get{Noun}Query or Get{Noun}{Suffix}Query
public record GetPageBlocksQuery(Guid PageId) : IRequest<IEnumerable<BlockDto>>;
public record GetBoardsQuery(Guid WorkspaceId) : IRequest<IEnumerable<BoardDto>>;
public record GetUserWorkspacesQuery(Guid UserId) : IRequest<IEnumerable<WorkspaceDto>>;
public record GetFullBoardQuery(Guid BoardId) : IRequest<BoardDto>;
```

### Handlers

```csharp
// Format: {CommandOrQueryName}Handler
public class CreateCardCommandHandler : IRequestHandler<CreateCardCommand, CardDto> { }
public class GetPageBlocksQueryHandler : IRequestHandler<GetPageBlocksQuery, IEnumerable<BlockDto>> { }
```

### DTOs

```csharp
// Format: {Noun}Dto
public record CardDto(Guid Id, string Title, Guid? LinkedPageId);
public record PageDto(Guid Id, string Title, Guid WorkspaceId);
public record WorkspaceDto(Guid Id, string Name, string Slug);
```

### Validators

```csharp
// Format: {CommandName}Validator
public class CreateCardCommandValidator : AbstractValidator<CreateCardCommand> { }
public class UpdatePageCommandValidator : AbstractValidator<UpdatePageCommand> { }
```

### API Endpoints

```csharp
// Format: {Domain}Endpoints
public static class CardEndpoints { }
public static class BoardEndpoints { }
public static class WorkspaceEndpoints { }
```

---

## Frontend (TypeScript / React)

### Files

```typescript
// Format: kebab-case
board-view-tabs.tsx
use-board-docs-panel.ts
docs-panel-skeleton.tsx
sign-in-form.tsx
```

### Components

```typescript
// Format: PascalCase
export function BoardViewTabs() {}
export function DocsPanel() {}
export function SignInForm() {}
```

### Hooks

```typescript
// Format: camelCase with 'use' prefix
export function useBoardDocsPanel() {}
export function usePageBlocks(pageId: string) {}
export function useCreateCard() {}
export function useUpdateBoard(boardId: string) {}
```

### Types/Interfaces

```typescript
// Format: PascalCase
interface WorkspaceMember {
  id: string;
  workspaceId: string;
  userId: string;
  role: string;
}

type CardPriority = 'urgent' | 'high' | 'medium' | 'low';
type BlockType = 'paragraph' | 'heading1' | 'card_ref';

// DTOs match backend
interface CardDto {
  id: string;
  title: string;
  linkedPageId: string | null;
}
```

### API Functions

```typescript
// Format: {verb}{Entity}
export const boardsApi = {
  getBoard: async (boardId: string) => {},
  getBoards: async (workspaceId: string) => {},
  createBoard: async (data: CreateBoardDto) => {},
  updateBoard: async (boardId: string, data: UpdateBoardDto) => {},
  deleteBoard: async (boardId: string) => {},
};
```

### Schemas (Zod)

```typescript
// Format: {entity}Schema or {action}{Entity}Schema
export const cardSchema = z.object({ /* ... */ });
export const createBoardSchema = z.object({ /* ... */ });
export const updatePageSchema = z.object({ /* ... */ });
```

### Query Keys

```typescript
// Format: Factory pattern with nested structure
export const queryKeys = {
  boards: {
    all: ['boards'] as const,
    lists: () => [...queryKeys.boards.all, 'list'] as const,
    list: (workspaceId: string) => [...queryKeys.boards.lists(), workspaceId] as const,
    details: () => [...queryKeys.boards.all, 'detail'] as const,
    detail: (boardId: string) => [...queryKeys.boards.details(), boardId] as const,
    fullBoard: (boardId: string) => [...queryKeys.boards.all, boardId, 'full'] as const,
  },
  pages: {
    all: ['pages'] as const,
    tree: (workspaceId: string) => [...queryKeys.pages.all, workspaceId, 'tree'] as const,
    blocks: (pageId: string) => [...queryKeys.pages.all, pageId, 'blocks'] as const,
  },
};
```

### Constants

```typescript
// Format: SCREAMING_SNAKE_CASE
const MIN_PANEL_WIDTH = 320;
const MAX_PANEL_WIDTH = 800;
const STORAGE_KEY = 'docs-panel-width';
const DEFAULT_STALE_TIME = 30 * 1000;
```

---

## Database (PostgreSQL)

### Tables

```sql
-- Format: snake_case, plural
CREATE TABLE workspace_members (
  -- ...
);

CREATE TABLE card_labels (
  -- ...
);

CREATE TABLE calendar_integrations (
  -- ...
);
```

### Columns

```sql
-- Format: snake_case
id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
workspace_id UUID NOT NULL REFERENCES workspaces(id),
user_id UUID NOT NULL REFERENCES users(id),
linked_page_id UUID REFERENCES pages(id),
is_deleted BOOLEAN NOT NULL DEFAULT false,
deleted_at TIMESTAMPTZ,
created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
updated_at TIMESTAMPTZ
```

### Column Naming Patterns

```sql
-- IDs: {table_singular}_id
workspace_id
user_id
parent_page_id
linked_page_id

-- Booleans: is_{adjective}
is_deleted
is_active
is_published
is_personal
is_collapsed

-- Timestamps: {action}_at
created_at
updated_at
deleted_at
expires_at
last_synced_at

-- Foreign keys: {referenced_table_singular}_id
workspace_id  -- references workspaces(id)
board_id      -- references boards(id)
```

### Indexes

```sql
-- Format: idx_{table}_{columns}
CREATE INDEX idx_cards_list_pos ON cards(list_id, position)
  WHERE is_deleted = false;

CREATE INDEX idx_cards_linked_page ON cards(linked_page_id)
  WHERE linked_page_id IS NOT NULL AND is_deleted = false;

CREATE INDEX idx_workspace_members_workspace ON workspace_members(workspace_id)
  WHERE is_deleted = false;
```

### Foreign Keys

```sql
-- Format: fk_{table}_{referenced_table} (auto-generated by EF Core)
-- Usually don't need to specify manually
```

---

## API Routes

### RESTful Conventions

```
GET    /api/v1/workspaces/{workspaceId}
GET    /api/v1/workspaces/{workspaceId}/boards
POST   /api/v1/workspaces/{workspaceId}/boards
GET    /api/v1/boards/{boardId}
GET    /api/v1/boards/{boardId}/full
PATCH  /api/v1/boards/{boardId}
DELETE /api/v1/boards/{boardId}

POST   /api/v1/cards/{cardId}/move
POST   /api/v1/cards/{cardId}/link-page
DELETE /api/v1/cards/{cardId}/link-page

GET    /api/v1/pages/{pageId}/blocks
PATCH  /api/v1/blocks/{blockId}
POST   /api/v1/blocks/reorder
```

### Route Patterns

- Use plural for collections: `/boards`, `/cards`, `/pages`
- Use singular for single resource: `/boards/{boardId}`
- Use kebab-case for multi-word: `/workspace-members`, `/calendar-integrations`
- Use nested routes for relationships: `/workspaces/{id}/boards`
- Use action verbs for non-CRUD: `/cards/{id}/move`, `/blocks/reorder`

---

## Git Conventions

### Branch Names

```
feature/{domain}/{description}
fix/{domain}/{description}
refactor/{scope}/{description}
chore/{description}

Examples:
feature/board/card-link-page
fix/calendar/sync-conflict-detection
refactor/db/add-board-views-table
chore/update-dependencies
```

### Commit Messages

```
{type}({domain}): {description}

Types: feat, fix, refactor, chore, test, docs

Examples:
feat(board): add linked_page_id to cards
fix(docs): prevent race condition in block reorder
refactor(auth): extract token refresh logic to interceptor
chore(db): add migration for board_views table
test(board): add unit tests for card move command
```

---

## Special Cases

### Fractional Indexing

```csharp
// C#: Use double
public double Position { get; set; } = 0;

// SQL: Use FLOAT8
position FLOAT8 NOT NULL DEFAULT 0
```

### JSONB Properties

```csharp
// C#: Use object or specific type
public object Properties { get; set; } = new();
public CoverProperties Cover { get; set; } = new();

// SQL: Use JSONB
properties JSONB NOT NULL DEFAULT '{}'
cover JSONB NOT NULL DEFAULT '{}'
```

### Timestamps

```csharp
// C#: Use DateTime
public DateTime CreatedAt { get; set; }
public DateTime? UpdatedAt { get; set; }

// SQL: Use TIMESTAMPTZ (with timezone)
created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
updated_at TIMESTAMPTZ
```

### Soft Delete

```csharp
// C#: Both fields required
public bool IsDeleted { get; set; } = false;
public DateTime? DeletedAt { get; set; }

// SQL: Both fields required
is_deleted BOOLEAN NOT NULL DEFAULT false
deleted_at TIMESTAMPTZ
```

---

## Common Mistakes to Avoid

### Backend

❌ **Wrong:**
```csharp
public class card { }  // lowercase
public string title;   // field instead of property
public int Position;   // int instead of double
```

✅ **Correct:**
```csharp
public class Card { }
public string Title { get; set; }
public double Position { get; set; }
```

### Frontend

❌ **Wrong:**
```typescript
// File: BoardViewTabs.tsx (PascalCase file)
export default function boardViewTabs() {}  // default export, lowercase

// Hardcoded query key
useQuery({ queryKey: ['boards', boardId], ... })
```

✅ **Correct:**
```typescript
// File: board-view-tabs.tsx (kebab-case file)
export function BoardViewTabs() {}  // named export, PascalCase

// Query key from factory
useQuery({ queryKey: queryKeys.boards.detail(boardId), ... })
```

### Database

❌ **Wrong:**
```sql
CREATE TABLE WorkspaceMembers (  -- PascalCase
  Id UUID,                       -- PascalCase
  WorkspaceId UUID,              -- PascalCase
  Position INTEGER               -- INTEGER instead of FLOAT8
);
```

✅ **Correct:**
```sql
CREATE TABLE workspace_members (  -- snake_case, plural
  id UUID,                        -- snake_case
  workspace_id UUID,              -- snake_case
  position FLOAT8                 -- FLOAT8 for fractional indexing
);
```

---

## Quick Reference Cheat Sheet

| Context | Format | Example |
|---------|--------|---------|
| **C# Class** | PascalCase, singular | `Card`, `WorkspaceMember` |
| **C# Property** | PascalCase | `Title`, `LinkedPageId` |
| **C# Command** | `{Verb}{Noun}Command` | `CreateCardCommand` |
| **C# Query** | `Get{Noun}Query` | `GetPageBlocksQuery` |
| **C# DTO** | `{Noun}Dto` | `CardDto`, `PageDto` |
| **TS File** | kebab-case | `board-view-tabs.tsx` |
| **TS Component** | PascalCase | `BoardViewTabs` |
| **TS Hook** | `use{Action}{Entity}` | `useCreateCard` |
| **TS Type** | PascalCase | `CardDto`, `WorkspaceMember` |
| **SQL Table** | snake_case, plural | `workspace_members` |
| **SQL Column** | snake_case | `workspace_id`, `is_deleted` |
| **SQL Index** | `idx_{table}_{cols}` | `idx_cards_list_pos` |
| **API Route** | kebab-case | `/workspace-members` |
| **Git Branch** | `{type}/{domain}/{desc}` | `feature/board/card-link` |
| **Commit** | `{type}({domain}): {desc}` | `feat(board): add cards` |

---

## See Also

- [AGENTS.md](../AGENTS.md) — Comprehensive conventions
- [domains.md](./domains.md) — Domain structure
- [api-patterns.md](./api-patterns.md) — API patterns
