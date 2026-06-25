<div align="center">

# Notrelix

**Enterprise Work Management Platform**

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Next.js](https://img.shields.io/badge/Next.js-16-000000?logo=next.js)](https://nextjs.org/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-336791?logo=postgresql)](https://www.postgresql.org/)
[![Redis](https://img.shields.io/badge/Redis-7-DC382D?logo=redis)](https://redis.io/)
[![Docker](https://img.shields.io/badge/Docker-Ready-2496ED?logo=docker)](https://www.docker.com/)

[Architecture](#-architecture) • [Quick Start](#-quick-start) • [Development](#-development) • [Documentation](#-documentation)

</div>

---

## About

Notrelix is a **workspace operating system for teams** — a unified platform that replaces separate tools for project management, documentation, and collaboration.

Kanban, Calendar, Timeline, Table, Dashboard, and Form are **views over the same work data**, not separate data models.

---

## Features

### Project Management
- **Multiple Views** — Kanban, Calendar, Timeline, Table, Dashboard, Form
- **Dynamic Schema** — Custom fields per board (select, text, date, people, etc.)
- **Drag & Drop** — Reorder items, change status across views
- **Saved Views** — Filtered, sorted, grouped view configurations

### Documents
- **Block-based Editor** — 20+ block types (paragraphs, headings, code, embeds)
- **Hierarchical Pages** — Nested structure with drag-and-drop
- **Templates** — Reusable page templates for common workflows

### Collaboration
- **Workspaces & Spaces** — Multi-tenant with folder organization
- **Role-based Access** — Owner, Admin, Member, Guest
- **Comments & Mentions** — Inline discussion on any item
- **Real-time Activity** — Track all workspace changes

### Platform
- **JWT Authentication** — Secure token-based auth with refresh
- **OAuth SSO** — Google, GitHub
- **Audit Logging** — Full governance trail
- **Automation Rules** — Trigger-based workflows
- **Integrations** — Extensible provider system (Resend, N8n)
- **API-first** — All operations available via REST API

---

## Architecture

### Clean Architecture (4-layer)

```
┌──────────────────────────────────────────────────┐
│                    API Layer                       │
│         ASP.NET Core + Controllers/Minimal API    │
│         Auth, Validation, Versioning, OpenAPI      │
└──────────────────────┬───────────────────────────┘
                       │
┌──────────────────────▼───────────────────────────┐
│                Application Layer                   │
│    CQRS (MediatR) • Commands • Queries • DTOs     │
│    Authorization • Orchestration • Caching         │
└──────────────────────┬───────────────────────────┘
                       │
┌──────────────────────▼───────────────────────────┐
│                 Domain Layer                       │
│    Aggregate Roots • Entities • Value Objects     │
│    Domain Events • Business Invariants            │
└──────────────────────┬───────────────────────────┘
                       │
┌──────────────────────▼───────────────────────────┐
│              Infrastructure Layer                  │
│    EF Core / PostgreSQL • Redis • S3/R2 Storage   │
│    Outbox • Email • Search Indexing • Providers   │
└──────────────────────────────────────────────────┘
```

### Dependency Rule
```
API → Application → Domain
Infrastructure → Application → Domain
Domain → (nothing — no EF Core, HTTP, Redis)
```

### Bounded Contexts

| Context | Key Aggregates |
|---------|---------------|
| **Identity** | User, Session |
| **Workspaces** | Workspace, Space, Team, WorkspaceMember, WorkspaceInvitation |
| **Governance** | ResourcePermission, AuditLog |
| **Work Management** | Board, BoardField, BoardItem, BoardView |
| **Documents** | Page, Block |
| **Collaboration** | Comment, Notification |
| **Automation** | AutomationRule, AutomationExecution |
| **Integrations** | Connection |
| **Billing** | Plan, Subscription |

---

## Tech Stack

### Backend
- **Runtime:** .NET 9 / ASP.NET Core
- **Architecture:** Clean Architecture + CQRS + MediatR
- **Database:** PostgreSQL 16 with EF Core
- **Cache:** Redis 7
- **Storage:** S3 / Cloudflare R2
- **Email:** Resend (SMTP fallback)
- **Testing:** xUnit + FluentAssertions + Testcontainers

### Frontend
- **Framework:** Next.js 16 (App Router)
- **Language:** TypeScript (strict)
- **UI:** React 19 + Tailwind CSS 4 + Base UI + shadcn/ui
- **State:** TanStack Query v5 + Zustand
- **Forms:** React Hook Form + Zod
- **Charts:** Recharts

### Infrastructure
- **Containerization:** Docker + Docker Compose
- **Reverse Proxy:** Nginx
- **CI/CD:** GitHub Actions

### Toolchain

| Tool | Purpose |
|------|---------|
| `make` | Development workflow orchestration |
| `bun` | Frontend package manager & runner |
| `dotnet` | Backend build, test, migrations |
| `dotnet ef` | EF Core migrations CLI |
| `pg_dump` / `pg_restore` | Database backup & restore |

---

## Quick Start

### Prerequisites
- Docker & Docker Compose (recommended)
- OR manually: .NET 9 SDK, Node.js 20+, PostgreSQL 16, Redis 7

### Option 1: Docker (Recommended)

```bash
git clone https://github.com/Nqv1208/Notrelix.git
cd Notrelix

# Start full development stack
make dev-up

# View logs
make dev-logs

# Stop
make dev-down
```

**Access:**
- Frontend: http://localhost:3000
- Backend API: http://localhost:5000
- PostgreSQL: localhost:5432
- Redis: localhost:6379

### Option 2: Manual Setup

#### Backend

```bash
cd backend
dotnet restore
dotnet run --project src/Notrelix.API
```

#### Frontend

```bash
cd frontend
bun install
bun run dev
```

---

## Development Workflow

### Environment Setup

Copy environment template and fill in secrets:

```bash
cp .env.dev.example .env.dev
# Edit .env.dev with your secrets (JWT, DB password, API keys)
```

### Makefile Commands

```bash
make dev-up          # Start dev stack
make dev-down        # Stop dev stack
make dev-logs        # Follow all logs
make backend-logs    # Backend logs only
make dev-restart     # Restart dev stack
make dev-clean       # Stop + delete volumes
make dev-reset       # Clean + migrate + seed + start
make dev-tools       # Start pgAdmin

make db-up           # Start postgres + redis only
make db-migrate      # Run EF migrations
make db-seed         # Run seed data
make db-init         # Migrations + seed
make db-psql         # Open psql shell
```

### Backend Commands

```bash
dotnet build              # Build solution
dotnet test               # Run all tests
dotnet format             # Code style check
dotnet ef migrations add <Name>   # Create migration
dotnet ef database update         # Apply migrations
```

### Frontend Commands

```bash
bun run dev          # Start dev server
bun run build        # Build for production
bun run lint         # ESLint check
```

### Database Workflow

1. Make domain changes
2. `dotnet ef migrations add <Name>` — generate migration
3. `make db-migrate` — apply to dev DB
4. Verify with `make db-psql`

---

## Project Structure

```
notrelix/
├── backend/
│   ├── src/
│   │   ├── Notrelix.API           # HTTP boundary, controllers
│   │   ├── Notrelix.Application   # CQRS handlers, DTOs, auth
│   │   ├── Notrelix.Domain        # Aggregates, entities, value objects
│   │   └── Notrelix.Infrastructure# EF Core, Redis, email, providers
│   └── tests/
│       ├── Notrelix.Domain.Tests
│       ├── Notrelix.Application.Tests
│       ├── Notrelix.Infrastructure.Tests
│       ├── Notrelix.API.Tests
│       ├── Notrelix.Integration.Tests
│       └── Notrelix.Architecture.Tests
│
├── frontend/
│   ├── app/              # Next.js App Router
│   ├── features/         # Feature modules
│   ├── components/       # Shared UI components
│   └── lib/              # Utilities, configs
│
├── infra/
│   ├── nginx/            # Nginx configuration
│   └── postgres/         # Init scripts
│
├── config/
│   └── docker/           # Docker config files
│
├── docs/
│   ├── backend/          # Backend architecture docs
│   └── CONFIGURATION.md  # Configuration reference
│
├── docker-compose.yml          # Base stack
├── docker-compose.dev.yml      # Dev overrides
├── docker-compose.staging.yml  # Staging overrides
├── docker-compose.prod.yml     # Production overrides
├── Makefile                     # Command orchestration
└── README.md                   # This file
```

---

## Configuration

Notrelix uses a **3-layer configuration system**:

| Layer | Role | Contains |
|-------|------|----------|
| `appsettings*.json` | Application behavior | Feature flags, logging, non-secret defaults |
| `.env.*` files | Secrets | Passwords, API keys, tokens |
| `docker-compose*.yml` | Topology | Service definitions, env mappings, volumes |

**Key sections:** `Database`, `JwtSettings`, `Smtp`, `Email`, `N8n`, `Redis`, `DataProtection`, `SeedData`

See [docs/CONFIGURATION.md](docs/CONFIGURATION.md) for full reference.

---

## Testing

```bash
# Run all backend tests
dotnet test

# Run specific project
dotnet test tests/Notrelix.Domain.Tests

# Run with filter
dotnet test --filter "FullyQualifiedName~LoginCommandHandler"
```

Test projects are organized per layer: Domain, Application, Infrastructure, API, Integration, and Architecture tests.

---

## Contributing

### Commit Convention

```
{type}({domain}): {description}

Types: feat, fix, refactor, chore, test, docs
Domains: auth, workspace, board, docs, calendar, shared, infra

Examples:
  feat(board): add item drag-and-drop
  fix(auth): normalize email lookup
  refactor(infra): extract DataProtection options
```

### Code Review
- All PRs require review
- Tests must pass
- Code must follow conventions in [AGENTS.md](AGENTS.md)
- Documentation must be updated

---

## Documentation

- **[AGENTS.md](AGENTS.md)** — Project rules and conventions
- **[docs/CONFIGURATION.md](docs/CONFIGURATION.md)** — Configuration reference
- **[docs/backend/](docs/backend/)** — Backend architecture guides
- **[PRODUCT.md](PRODUCT.md)** — Product design principles
- **[DESIGN.md](DESIGN.md)** — Design system tokens and guidelines

---

## Project Status

**Current Version:** 0.1.0 (Alpha) — Active Development

![GitHub last commit](https://img.shields.io/github/last-commit/Nqv1208/Notrelix)
![GitHub issues](https://img.shields.io/github/issues/Nqv1208/Notrelix)

---

<div align="center">

**[⬆ Back to Top](#notrelix)**

</div>
