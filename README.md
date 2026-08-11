<div align="center">

# Notrelix

**Enterprise Work Management Platform**

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![React](https://img.shields.io/badge/React-19-61DAFB?logo=react)](https://react.dev/)
[![Vite](https://img.shields.io/badge/Vite-8-646CFF?logo=vite)](https://vitejs.dev/)
[![Next.js](https://img.shields.io/badge/Next.js-16-000000?logo=next.js)](https://nextjs.org/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-336791?logo=postgresql)](https://www.postgresql.org/)
[![Redis](https://img.shields.io/badge/Redis-7-DC382D?logo=redis)](https://redis.io/)
[![Docker](https://img.shields.io/badge/Docker-Ready-2496ED?logo=docker)](https://www.docker.com/)
[![pnpm](https://img.shields.io/badge/pnpm-10-F69220?logo=pnpm)](https://pnpm.io/)

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

### Backend: Modular Monolith (Clean Architecture + DDD)

```
┌──────────────────────────────────────────────────┐
│                    API Layer                       │
│         ASP.NET Core + Minimal API / Controllers  │
│         Auth, Validation, Versioning, OpenAPI      │
└──────────────────────┬───────────────────────────┘
                       │
┌──────────────────────▼───────────────────────────┐
│                Application Layer                   │
│    CQRS (MediatR) • Commands • Queries • DTOs     │
│    Authorization • Orchestration • Caching         │
│    Pipeline Behaviors (19) • Outbox Coordination   │
└──────────────────────┬───────────────────────────┘
                       │
┌──────────────────────▼───────────────────────────┐
│                 Domain Layer                       │
│    Aggregate Roots • Entities • Value Objects     │
│    Domain Events • Business Invariants             │
│    Tenant/Account/Workspace Scoping                │
└──────────────────────┬───────────────────────────┘
                       │
┌──────────────────────▼───────────────────────────┐
│              Infrastructure Layer                  │
│    EF Core / PostgreSQL • Redis • S3/R2 Storage   │
│    Outbox • Messaging (MassTransit/RabbitMQ)      │
│    Email (Resend) • Search • RLS Policies         │
└──────────────────────┬───────────────────────────┘
                       │
┌──────────────────────▼───────────────────────────┐
│                 Platform Layer                     │
│    Messaging Consumers • Transport • Reliability  │
│    Serialization • Configuration • Health         │
└──────────────────────────────────────────────────┘
```

### Dependency Rule

```
API → Application → Domain
Infrastructure → Application → Domain
Platform → Application → Domain
Domain → (nothing — no EF Core, HTTP, Redis, providers)
```

### Bounded Contexts

| Context | Key Aggregates |
|---------|---------------|
| **Accounts** | Account, User, Credential, OAuth |
| **Identity** | User, Session, ApiToken |
| **Workspaces** | Workspace, Space, Team, WorkspaceMember, WorkspaceInvitation |
| **Governance** | ResourcePermission, AuditLog, PermissionPolicy |
| **Work Management** | Board, BoardField, BoardItem, BoardGroup, BoardView |
| **Documents** | Page, Block, Template |
| **Collaboration** | Comment, Reaction, Notification, Presence, Watcher |
| **Automation** | AutomationRule, AutomationExecution |
| **Integrations** | Connection, Webhook, Provider |
| **Billing** | Plan, Subscription, Entitlement, Invoice |
| **Analytics** | Dashboard, Widget, Snapshot, Metric |
| **Search** | Index, Query, Permission |

### Test Projects

| Project | Purpose |
|---------|---------|
| `Notrelix.Domain.Tests` | Aggregate invariants, value objects, domain events |
| `Notrelix.Application.Tests` | Handlers, validators, behaviors, request contracts |
| `Notrelix.Infrastructure.Tests` | EF mappings, RLS/session, outbox, external adapters |
| `Notrelix.API.Tests` | Endpoint binding, response contracts, problem details |
| `Notrelix.Integration.Tests` | Full pipeline, DB, tenant isolation, cache/security |
| `Notrelix.Platform.Tests` | Messaging consumers, transport, reliability, serialization |
| `Notrelix.Architecture.Tests` | Dependency rules, folder rules, marker rules, forbidden deps |
| `Notrelix.Testing.{Core,Domain,Application,Integration}` | Test utilities, fixtures, builders, helpers |

---

### Frontend: pnpm Monorepo

```
apps/
├── marketing/          # Next.js App Router (SEO/public site)
├── web/                # Vite + React 19 + TanStack Router (product app)
└── mobile/             # Expo / React Native (placeholder)

packages/
├── foundation/         # contracts, kernel, platform, query, realtime, observability
├── runtimes/           # web, mobile adapters
├── ui/                 # tokens, web (shadcn), mobile, icons
├── product/            # work-management, docs, automation
└── features/           # auth, workspace, account, billing, etc.

tooling/                # eslint, typescript, dependency-rules, codegen, testing, storybook
```

### Tech Stack

| Layer | Technology |
|-------|------------|
| **Backend Runtime** | .NET 9 / ASP.NET Core |
| **Backend Architecture** | Clean Architecture + CQRS + MediatR + Modular Monolith |
| **Database** | PostgreSQL 16 with EF Core |
| **Cache** | Redis 7 |
| **Storage** | S3 / Cloudflare R2 |
| **Email** | Resend (SMTP fallback) |
| **Messaging** | MassTransit / RabbitMQ |
| **Search** | PostgreSQL Full-Text / Elasticsearch |
| **Frontend Framework** | Vite 8 + React 19 (web), Next.js 16 (marketing), Expo (mobile) |
| **Frontend Language** | TypeScript (strict) |
| **UI** | Tailwind CSS 4 + Base UI + shadcn/ui |
| **State** | TanStack Query v5 + Zustand |
| **Router** | TanStack Router |
| **Forms** | React Hook Form + Zod |
| **Charts** | Recharts |
| **Package Manager** | pnpm >= 10 (single root lockfile) |
| **Containerization** | Docker + Docker Compose |
| **Reverse Proxy** | Nginx |
| **CI/CD** | GitHub Actions |

---

## Quick Start

### Prerequisites
- Docker & Docker Compose (recommended)
- OR manually: .NET 9 SDK, Node.js 22+, PostgreSQL 16, Redis 7

### Option 1: Docker (Recommended)

```bash
git clone https://github.com/Nqv1208/Notrelix.git
cd Notrelix

# Start full development stack (backend + frontend + postgres + redis)
make dev-up

# View logs
make dev-logs

# Stop
make dev-down
```

**Access:**
- Frontend Web: http://localhost:3000
- Frontend Marketing: http://localhost:3001
- Backend API: http://localhost:5000
- PostgreSQL: localhost:5432
- Redis: localhost:6379
- RabbitMQ Management: http://localhost:15672

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
pnpm install
pnpm run dev          # Starts web (5173) + marketing (3001)
```

---

## Development Workflow

### Environment Setup

```bash
cp .env.dev.example .env.dev
# Edit .env.dev with your secrets (JWT, DB password, API keys)
```

### Makefile Commands

```bash
make dev-up              # Start dev stack
make dev-down            # Stop dev stack
make dev-logs            # Follow all logs
make backend-logs        # Backend logs only
make dev-restart         # Restart dev stack
make dev-clean           # Stop + delete volumes
make dev-reset           # Clean + migrate + seed + start
make dev-reset-full      # Clean + force restore + migrate + seed + start
make dev-tools           # Start pgAdmin profile
make messaging-up        # Start RabbitMQ (messaging profile)
make messaging-down      # Stop RabbitMQ

make db-up               # Start postgres + redis only
make db-migrate          # Run EF migrations
make db-seed             # Run seed data
make db-init             # Migrations + seed
make db-rls              # Apply RLS policies
make db-psql             # Open psql shell

make be-build            # Build backend inside container
make be-test             # Run backend tests inside container
make be-clean-nuget      # Clear NuGet caches
make be-shell            # Open shell inside backend container
make backend-image-build # Rebuild backend Docker image
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
pnpm run dev          # Start dev servers (web + marketing)
pnpm run build        # Build for production
pnpm run lint         # ESLint check
pnpm run typecheck    # Type-check all packages
pnpm run test         # Run unit & component tests
pnpm run validate     # Full local validation suite
pnpm run check:deps   # Run AST architecture checks
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
│   │   ├── Notrelix.API           # HTTP boundary, controllers/minimal API
│   │   ├── Notrelix.Application   # CQRS handlers, DTOs, auth, pipeline
│   │   ├── Notrelix.Domain        # Aggregates, entities, value objects, events
│   │   ├── Notrelix.Infrastructure# EF Core, Redis, email, storage, providers
│   │   └── Notrelix.Platform      # Messaging consumers, transport, reliability
│   └── tests/
│       ├── Notrelix.Domain.Tests
│       ├── Notrelix.Application.Tests
│       ├── Notrelix.Infrastructure.Tests
│       ├── Notrelix.API.Tests
│       ├── Notrelix.Integration.Tests
│       ├── Notrelix.Platform.Tests
│       ├── Notrelix.Architecture.Tests
│       ├── Notrelix.Testing.Core
│       ├── Notrelix.Testing.Domain
│       ├── Notrelix.Testing.Application
│       └── Notrelix.Testing.Integration
│
├── frontend/
│   ├── apps/
│   │   ├── marketing/             # Next.js App Router
│   │   ├── web/                   # Vite + React + TanStack Router
│   │   └── mobile/                # Expo placeholder
│   ├── packages/
│   │   ├── foundation/            # contracts, kernel, platform, query, realtime, observability
│   │   ├── runtimes/              # web, mobile adapters
│   │   ├── ui/                    # tokens, web, mobile, icons
│   │   ├── product/               # work-management, docs, automation
│   │   └── features/              # auth, workspace, account, billing, etc.
│   ├── tooling/                   # eslint, typescript, dependency-rules, codegen, testing, storybook
│   ├── docs/
│   │   ├── client/architecture/   # Canonical architecture spec
│   │   └── client/adr/            # Architecture Decision Records
│   ├── pnpm-workspace.yaml
│   ├── turbo.json
│   └── tsconfig.base.json
│
├── infra/
│   ├── nginx/                     # Nginx configuration
│   ├── postgres/                  # Init scripts
│   └── n8n/                       # N8n workflow configs
│
├── docs/
│   ├── agents/                    # Agent skills, triage labels, domain docs
│   └── products/                  # Product development thinking
│
├── docker-compose.yml          # Base stack
├── docker-compose.dev.yml      # Dev overrides
├── docker-compose.staging.yml  # Staging overrides
├── docker-compose.prod.yml     # Production overrides
├── Makefile                     # Command orchestration
├── backend.slnx                # Solution file
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

See [backend configuration and runtime](backend/docs/operations/configuration-and-runtime.md)
for the current runtime reference.

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

Test projects are organized per layer: Domain, Application, Infrastructure, API, Integration, Platform, Architecture, and Testing support.

### Frontend

```bash
pnpm run test         # Unit & component tests
pnpm run typecheck    # Type-check all packages
pnpm run lint         # ESLint
pnpm run check:deps   # Architecture boundary check
```

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

- **[AGENTS.md](AGENTS.md)** — Project rules and conventions (coding agent contract)
- **[RULE.md](RULE.md)** — Product/domain hard rules
- **[SKILL.md](SKILL.md)** — Execution mindset
- **[PRODUCT.md](PRODUCT.md)** — Product design principles
- **[DESIGN.md](DESIGN.md)** — Design system tokens and guidelines
- **[backend/docs/](backend/docs/)** — Backend architecture, operations and ADRs
- **[frontend/docs/](frontend/docs/)** — Frontend architecture, generated evidence and ADRs

---

## Project Status

**Current Version:** 0.1.0 (Alpha) — Active Development

![GitHub last commit](https://img.shields.io/github/last-commit/Nqv1208/Notrelix)
![GitHub issues](https://img.shields.io/github/issues/Nqv1208/Notrelix)

---

<div align="center">

**[⬆ Back to Top](#notrelix)**

</div>
