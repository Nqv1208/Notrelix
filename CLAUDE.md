# Claude Code Guide for Notrelix

> Quick reference for using Claude Code with the Notrelix project. For comprehensive rules, see [AGENTS.md](./AGENTS.md).

## Project Overview

**Notrelix** is a SaaS workspace that combines:
- **Notion-like documents** — Block-based editor with rich content types
- **Trello-like boards** — Kanban project management with cards and lists
- **Calendar sync** — Two-way sync with Google Calendar for cards and pages

**Tech Stack:**
- Backend: .NET 8, ASP.NET Core, Entity Framework Core, PostgreSQL 16
- Frontend: Next.js 16 (App Router), React 19, TypeScript, TanStack Query, shadcn/ui
- Cache: Redis 7
- Storage: S3/R2 for attachments
- Auth: JWT (access token in-memory) + refresh token (httpOnly cookie)

## Quick Start

### Run Locally with Docker

```bash
# Start development stack (hot reload enabled)
make dev-up

# View logs
make dev-logs

# Stop stack
make dev-down

# Clean everything (containers + volumes)
make clean
```

**Services:**
- Frontend: http://localhost:3000
- Backend API: http://localhost:5000
- PostgreSQL: localhost:5432
- Redis: localhost:6379

### Run Backend Standalone

```bash
cd backend
dotnet restore
dotnet run --project Notrelix.API
```

### Run Frontend Standalone

```bash
cd frontend
bun install
bun run dev
```

## Project Structure

```
todo-app/
├── backend/                    # .NET 8 backend
│   ├── Notrelix.Domain/       # Entities, value objects, domain events
│   ├── Notrelix.Application/  # CQRS commands/queries, DTOs
│   ├── Notrelix.Infrastructure/ # EF Core, Redis, external services
│   ├── Notrelix.API/          # Minimal API endpoints
│   └── Notrelix.Tests/        # Unit + integration tests
│
├── frontend/                   # Next.js 16 frontend
│   ├── app/                   # App Router (routing + UI only)
│   ├── features/              # Business logic (hooks, schemas, types)
│   ├── lib/                   # Utilities (API client, query setup)
│   ├── components/            # Shared UI components
│   └── hooks/                 # Global React hooks
│
├── infra/                      # Infrastructure configs
│   ├── nginx/                 # Nginx reverse proxy
│   └── postgres/              # PostgreSQL init scripts
│
├── .claude/                    # Claude Code configuration
│   ├── skills/                # Custom skills for common tasks
│   ├── templates/             # Code generation templates
│   └── docs/                  # Quick reference guides
│
├── AGENTS.md                   # Comprehensive project rules (READ THIS!)
├── DESIGN.md                   # Design system and UI guidelines
├── docker-compose.yml          # Base Docker stack
├── docker-compose.dev.yml      # Development overrides
└── Makefile                    # Common Docker commands
```

## Architecture Quick Reference

### 7 Domains

The project is organized into 7 domains:

1. **Identity & Auth** — users, user_profiles, sessions, oauth_accounts
2. **Workspace** — workspaces, workspace_members, workspace_invitations
3. **Document (Notion)** — pages, blocks
4. **Board (Trello)** — boards, lists, cards, labels, checklists
5. **Calendar Sync** — calendar_integrations, calendar_events
6. **Shared/Cross** — comments, attachments, permissions, notifications, activity_logs
7. **Extensibility** — webhooks, automations, integrations, audit_snapshots

### Backend Architecture

**Dependency Flow (one-way):**
```
API → Application → Domain
Infrastructure → Application → Domain
```

**CQRS Pattern:**
- Commands: `CreateCardCommand`, `UpdatePageCommand`
- Queries: `GetPageBlocksQuery`, `GetBoardsQuery`
- Handlers: MediatR with validation, logging, authorization, caching behaviors

**Key Conventions:**
- Entities: PascalCase, singular (`Card`, `WorkspaceMember`)
- DB tables: snake_case, plural (`cards`, `workspace_members`)
- Commands: `{Verb}{Noun}Command` (`CreateCardCommand`)
- Queries: `Get{Noun}Query` (`GetPageBlocksQuery`)
- DTOs: `{Noun}Dto` (`CardDto`)

### Frontend Architecture

**Import Rules (one-way):**
```
app → features → lib → external
app → components → external
features CANNOT import from app
features CANNOT import from other features
```

**Feature-Sliced Design:**
- `features/` — Pure logic (hooks, schemas, types, utils) — NO UI
- `app/_components/` — Route-specific UI (private, co-located)
- `components/` — Shared UI (used in ≥2 places)

**Key Conventions:**
- Components: PascalCase (`SignInForm`, `PageTree`)
- Hooks: camelCase with `use` prefix (`useLogin`, `useBoard`)
- Files: kebab-case (`sign-in-form.tsx`, `page-tree.tsx`)
- Types: PascalCase (`CardDto`, `PageDto`)

## Common Tasks

### Backend Tasks

#### Add a new CQRS command/query

```bash
# Use the backend-cqrs skill
# This will generate command, handler, DTO, and API endpoint
```

See: `.claude/skills/backend-cqrs.md`

#### Create a database migration

```bash
cd backend
dotnet ef migrations add AddCardLinkedPageId --project Notrelix.Infrastructure --startup-project Notrelix.API
dotnet ef database update --project Notrelix.Infrastructure --startup-project Notrelix.API
```

See: `.claude/skills/database-migration.md`

#### Run backend tests

```bash
cd backend
dotnet test
```

### Frontend Tasks

#### Add a new feature

```bash
# Use the frontend-feature skill
# This will generate api/, hooks/, schemas/, types/ structure
```

See: `.claude/skills/frontend-feature.md`

#### Create a new component

```bash
# Use the component-scaffold skill
# This will generate component with proper location and conventions
```

See: `.claude/skills/component-scaffold.md`

#### Run frontend dev server

```bash
cd frontend
bun run dev
```

### Database Tasks

#### Seed database with test data

```bash
# Edit backend/Notrelix.Infrastructure/Data/ApplicationDbContextInitialiser.cs
# Set SeedProfile: Small, Medium, or Large
# Then run:
make dev-down
make dev-up
```

#### Connect to PostgreSQL

```bash
# Using psql
psql -h localhost -p 5432 -U notrelix -d notrelix

# Or add pgAdmin with Docker
make dev-tools
# Access pgAdmin at http://localhost:5050
```

## Available Skills

Claude Code skills are located in `.claude/skills/`. Invoke them when you need to:

### Backend Skills

- **backend-cqrs** — Generate CQRS command/query with handler, DTO, and API endpoint
- **database-migration** — Create EF Core migration with proper configuration and indexes
- **testing** — Generate backend unit/integration tests

### Frontend Skills

- **frontend-feature** — Scaffold complete feature with API client, hooks, schemas, types
- **component-scaffold** — Generate React component following project conventions
- **testing** — Generate frontend API contract tests

## Key Files Reference

### Backend

- `backend/Notrelix.API/Program.cs` — DI setup, middleware pipeline
- `backend/Notrelix.Infrastructure/Data/ApplicationDbContext.cs` — EF Core context
- `backend/Notrelix.Infrastructure/Data/ApplicationDbContextInitialiser.cs` — DB init + seed
- `backend/Notrelix.API/appsettings.json` — Configuration
- `backend/Notrelix.Application/Common/Interfaces/` — Service interfaces

### Frontend

- `frontend/app/layout.tsx` — Root layout with providers
- `frontend/lib/api/api-client.ts` — Axios instance with interceptors
- `frontend/lib/api/endpoints.ts` — API endpoint definitions
- `frontend/lib/query/query-keys.ts` — TanStack Query keys factory
- `frontend/package.json` — Dependencies

### Infrastructure

- `docker-compose.yml` — Base stack definition
- `docker-compose.dev.yml` — Development overrides (hot reload)
- `Makefile` — Docker commands
- `infra/nginx/nginx.conf` — Nginx reverse proxy config

## Naming Conventions Cheat Sheet

### Backend (.NET)

```csharp
// Entities
public class WorkspaceMember { }

// Properties
public Guid Id { get; set; }
public string Title { get; set; } = string.Empty;
public Guid? LinkedPageId { get; set; }

// Enums
public enum CardPriority { Urgent, High, Medium, Low }

// Commands
public record CreateCardCommand(string Title, Guid ListId) : IRequest<CardDto>;

// Queries
public record GetPageBlocksQuery(Guid PageId) : IRequest<IEnumerable<BlockDto>>;

// DTOs
public record CardDto(Guid Id, string Title, Guid? LinkedPageId);
```

### Frontend (TypeScript)

```typescript
// Files
board-view-tabs.tsx
use-board-docs-panel.ts

// Components
export function BoardViewTabs() {}

// Hooks
export function useBoardDocsPanel() {}

// Types
interface WorkspaceMember { }
type CardPriority = 'urgent' | 'high' | 'medium' | 'low'

// Query keys
export const queryKeys = {
  boards: {
    list: (workspaceId: string) => ['boards', workspaceId] as const,
    detail: (boardId: string) => ['boards', 'detail', boardId] as const,
  },
}
```

### Database (PostgreSQL)

```sql
-- Tables: snake_case, plural
CREATE TABLE workspace_members (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  workspace_id UUID NOT NULL REFERENCES workspaces(id),
  user_id UUID NOT NULL REFERENCES users(id),
  role VARCHAR(50) NOT NULL,
  position FLOAT8 NOT NULL DEFAULT 0,  -- fractional indexing
  is_deleted BOOLEAN NOT NULL DEFAULT false,
  deleted_at TIMESTAMPTZ,
  created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  updated_at TIMESTAMPTZ
);

-- Indexes: partial to exclude deleted rows
CREATE INDEX idx_workspace_members_workspace
  ON workspace_members(workspace_id)
  WHERE is_deleted = false;
```

## Important Rules

### Backend

- **DO NOT** override properties from base classes (CS0108 warning)
- **ALWAYS** use `double` for position fields (fractional indexing)
- **ALWAYS** soft delete with `is_deleted` + `deleted_at`
- **NEVER** sync calendar in request/response cycle (use queue)
- **NEVER** store file binary in PostgreSQL (use S3/R2)

### Frontend

- **ALWAYS** use query keys factory from `lib/query/query-keys.ts`
- **NEVER** hardcode query key strings
- **ALWAYS** await `params` in Next.js 16 page components
- **DO NOT** use HTML `<form>` tag (use button onClick instead)
- **ALWAYS** use Server Components by default (add 'use client' only when needed)

### Database

- **ALWAYS** use FLOAT8 for position columns (not INTEGER)
- **ALWAYS** use JSONB for flexible properties
- **ALWAYS** add partial indexes to exclude `is_deleted = true`
- **NEVER** create duplicate polymorphic tables (use shared tables)

## Quick Reference Docs

For more detailed information, see:

- `.claude/docs/domains.md` — 7 domains with entities and responsibilities
- `.claude/docs/conventions.md` — Comprehensive naming conventions
- `.claude/docs/api-patterns.md` — Common API patterns and examples
- `.claude/docs/troubleshooting.md` — Common issues and solutions

## Comprehensive Documentation

- **[AGENTS.md](./AGENTS.md)** — **READ THIS FIRST!** Comprehensive project rules (1,089 lines)
- **[DESIGN.md](./DESIGN.md)** — Design system, colors, typography, components (716 lines)
- **[notrelix-backend-structure.md](./notrelix-backend-structure.md)** — Detailed backend architecture
- **[notrelix-frontend-structure.md](./notrelix-frontend-structure.md)** — Detailed frontend architecture

## Troubleshooting

### Docker issues

```bash
# Clean everything and restart
make clean
make dev-up

# View logs for specific service
docker-compose -f docker-compose.yml -f docker-compose.dev.yml logs -f backend
```

### Database migration issues

```bash
# Reset database (WARNING: deletes all data)
make dev-down
docker volume rm todo-app_postgres_data
make dev-up
```

### Frontend build issues

```bash
cd frontend
rm -rf node_modules .next
bun install
bun run build
```

### Backend build issues

```bash
cd backend
dotnet clean
dotnet restore
dotnet build
```

## Git Workflow

### Branch Naming

```
feature/{domain}/{description}    # feature/board/card-link-page
fix/{domain}/{description}        # fix/calendar/sync-conflict-detection
refactor/{scope}/{description}    # refactor/db/add-board-views-table
chore/{description}               # chore/update-dependencies
```

### Commit Messages

```
{type}({domain}): {description}

feat(board): add linked_page_id to cards
fix(docs): prevent race condition in block reorder
refactor(auth): extract token refresh logic to interceptor
chore(db): add migration for board_views table
```

## Environment Variables

### Backend (.env or appsettings.json)

```
CONNECTION_STRING=Host=localhost;Database=notrelix;Username=...;Password=...
REDIS_CONNECTION=localhost:6379
JWT_SECRET=<256-bit secret>
JWT_ACCESS_TOKEN_EXPIRY=15m
JWT_REFRESH_TOKEN_EXPIRY=30d
S3_BUCKET=notrelix-attachments
S3_ENDPOINT=https://<account>.r2.cloudflarestorage.com
GOOGLE_CLIENT_ID=...
GOOGLE_CLIENT_SECRET=...
```

### Frontend (.env.local)

```
NEXT_PUBLIC_API_URL=http://localhost:5000/api
NEXT_PUBLIC_APP_URL=http://localhost:3000
```

## Performance Tips

- **Backend:** Use `Include()` for eager loading to avoid N+1 queries
- **Backend:** Cache hot data in Redis (workspace members, permissions)
- **Backend:** Use pagination for all list endpoints
- **Frontend:** Prefetch data in Server Components with `HydrationBoundary`
- **Frontend:** Use `dynamic()` import for heavy components (BlockEditor)
- **Frontend:** Invalidate specific query keys, not entire cache
- **Database:** Ensure all WHERE clauses have corresponding indexes

## Need Help?

1. Check [AGENTS.md](./AGENTS.md) for comprehensive rules
2. Check `.claude/docs/troubleshooting.md` for common issues
3. Review existing code in the same domain for patterns
4. Check git history for similar changes: `git log --grep="keyword"`


## Agent skills

### Issue tracker

Issues and specs live as GitHub issues (`Nqv1208/Notrelix`). See [`docs/agents/issue-tracker.md`](file:///Users/nqvinh/Documents/projects/todo-app/docs/agents/issue-tracker.md).

### Triage labels

Canonical triage roles mapped to repo labels (`needs-triage`, `needs-info`, `ready-for-agent`, `ready-for-human`, `wontfix`). See [`docs/agents/triage-labels.md`](file:///Users/nqvinh/Documents/projects/todo-app/docs/agents/triage-labels.md).

### Domain docs

Multi-context layout using `CONTEXT-MAP.md` at root pointing to per-context `CONTEXT.md` files. See [`docs/agents/domain.md`](file:///Users/nqvinh/Documents/projects/todo-app/docs/agents/domain.md).

---

*Last updated: 2026-08-08*

