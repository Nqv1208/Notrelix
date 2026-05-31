<div align="center">

# 🚀 Notrelix

**The Ultimate Workspace for Modern Teams**

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Next.js](https://img.shields.io/badge/Next.js-16-000000?logo=next.js)](https://nextjs.org/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-336791?logo=postgresql)](https://www.postgresql.org/)
[![Redis](https://img.shields.io/badge/Redis-7-DC382D?logo=redis)](https://redis.io/)
[![Docker](https://img.shields.io/badge/Docker-Ready-2496ED?logo=docker)](https://www.docker.com/)

*Where structured thought meets vibrant action — Notion's editorial depth + Trello's kinetic energy*

[Features](#-features) • [Demo](#-demo) • [Quick Start](#-quick-start) • [Documentation](#-documentation) • [Contributing](#-contributing)

</div>

---

## 📖 About

**Notrelix** is a modern, full-stack SaaS workspace that seamlessly combines:

- 📝 **Notion-like Documents** — Block-based editor with rich content types
- 📋 **Trello-like Boards** — Kanban project management with cards and lists  
- 📅 **Calendar Sync** — Two-way synchronization with Google Calendar
- 🔗 **Bidirectional Linking** — Connect documents and tasks effortlessly
- 👥 **Real-time Collaboration** — Work together with your team in real-time

Built with enterprise-grade architecture and modern technologies, Notrelix provides a unified workspace where teams can plan, document, and execute their work.

## ✨ Features

### 📝 Document Management (Notion-like)

- **Block-based Editor** — 20+ block types (paragraphs, headings, lists, code, embeds)
- **Hierarchical Pages** — Nested page structure with drag-and-drop
- **Rich Content** — Images, videos, files, embeds, and more
- **Version History** — Track changes and restore previous versions
- **Templates** — Reusable page templates for common workflows
- **Card References** — Embed board cards directly in documents

### 📋 Project Management (Trello-like)

- **Kanban Boards** — Visual workflow management
- **Multiple Views** — Kanban, List, Calendar, Timeline
- **Cards & Lists** — Organize tasks with drag-and-drop
- **Labels & Members** — Color-coded tags and team assignments
- **Checklists** — Break down tasks into subtasks
- **Due Dates** — Set deadlines with calendar integration
- **Page Linking** — Attach detailed documentation to any card

### 📅 Calendar Integration

- **Two-way Sync** — Sync with Google Calendar (Outlook coming soon)
- **Automatic Updates** — Changes sync bidirectionally
- **Conflict Detection** — Smart conflict resolution
- **Unified View** — See all deadlines in one place

### 👥 Collaboration

- **Workspaces** — Multi-tenant architecture
- **Role-based Access** — Owner, Admin, Member, Guest roles
- **Invitations** — Email-based team invitations
- **Comments** — Discuss on cards and pages
- **Activity Logs** — Track all workspace activity
- **Notifications** — Real-time updates

### 🔒 Security & Performance

- **JWT Authentication** — Secure token-based auth
- **OAuth Support** — Google, GitHub SSO
- **Row-level Security** — PostgreSQL RLS ready
- **Redis Caching** — Fast data access
- **S3/R2 Storage** — Scalable file storage
- **Rate Limiting** — API protection

## 🎯 Tech Stack

### Backend

- **Framework:** .NET 8 / ASP.NET Core
- **Architecture:** Clean Architecture + CQRS + MediatR
- **Database:** PostgreSQL 16 with EF Core
- **Cache:** Redis 7
- **Storage:** S3/Cloudflare R2
- **Testing:** xUnit + FluentAssertions

### Frontend

- **Framework:** Next.js 16 (App Router)
- **Language:** TypeScript (strict mode)
- **UI Library:** React 19
- **Styling:** Tailwind CSS 4 + shadcn/ui
- **State Management:** TanStack Query v5
- **Forms:** React Hook Form + Zod
- **Icons:** Lucide React

### Infrastructure

- **Containerization:** Docker + Docker Compose
- **Reverse Proxy:** Nginx
- **CI/CD:** GitHub Actions (coming soon)
- **Monitoring:** (coming soon)

## 🚀 Quick Start

### Prerequisites

- **Docker & Docker Compose** (recommended)
- **OR** manually install:
  - .NET 8 SDK
  - Node.js 20+ (or Bun)
  - PostgreSQL 16
  - Redis 7

### Option 1: Docker (Recommended)

```bash
# Clone the repository
git clone https://github.com/Nqv1208/todo-app.git
cd todo-app

# Start all services
make dev-up

# View logs
make dev-logs

# Stop services
make dev-down
```

**Access the application:**
- Frontend: http://localhost:3000
- Backend API: http://localhost:5000
- PostgreSQL: localhost:5432
- Redis: localhost:6379

### Option 2: Manual Setup

#### Backend

```bash
cd backend

# Restore dependencies
dotnet restore

# Update database
dotnet ef database update --project Notrelix.Infrastructure --startup-project Notrelix.API

# Run the API
dotnet run --project Notrelix.API
```

#### Frontend

```bash
cd frontend

# Install dependencies
bun install

# Run dev server
bun run dev
```

### Environment Variables

#### Backend (`backend/Notrelix.API/appsettings.Development.json`)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=notrelix;Username=notrelix;Password=your_password"
  },
  "Redis": {
    "ConnectionString": "localhost:6379"
  },
  "Jwt": {
    "Secret": "your-256-bit-secret-key-here",
    "AccessTokenExpiry": "15m",
    "RefreshTokenExpiry": "30d"
  }
}
```

#### Frontend (`frontend/.env.local`)

```env
NEXT_PUBLIC_API_URL=http://localhost:5000/api
NEXT_PUBLIC_APP_URL=http://localhost:3000
```

## 📚 Documentation

### For Developers

- **[CLAUDE.md](CLAUDE.md)** — Quick start guide for Claude Code
- **[AGENTS.md](AGENTS.md)** — Comprehensive project rules and conventions
- **[DESIGN.md](DESIGN.md)** — Design system and UI guidelines
- **[Backend Structure](notrelix-backend-structure.md)** — Backend architecture details
- **[Frontend Structure](notrelix-frontend-structure.md)** — Frontend architecture details

### Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                         Frontend                             │
│  Next.js 16 App Router + React 19 + TanStack Query         │
└─────────────────────────────────────────────────────────────┘
                            ↓ HTTP/REST
┌─────────────────────────────────────────────────────────────┐
│                      Backend API                             │
│              ASP.NET Core + Minimal API                      │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│                    Application Layer                         │
│         CQRS (MediatR) + Commands/Queries + DTOs            │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│                     Domain Layer                             │
│        Entities + Value Objects + Domain Events             │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌──────────────────┬──────────────────┬──────────────────────┐
│   PostgreSQL 16  │     Redis 7      │    S3/R2 Storage     │
│   (Primary DB)   │   (Cache/Queue)  │   (File Storage)     │
└──────────────────┴──────────────────┴──────────────────────┘
```

### Domain Architecture

Notrelix is organized into **7 domains**:

1. **Identity & Auth** — Users, sessions, OAuth
2. **Workspace** — Multi-tenant workspaces and members
3. **Document** — Notion-like pages and blocks
4. **Board** — Trello-like boards, lists, and cards
5. **Calendar** — Two-way calendar synchronization
6. **Shared** — Comments, attachments, permissions, notifications
7. **Extensibility** — Webhooks, automations, integrations

## 🛠️ Development

### Available Commands

```bash
# Docker commands
make dev-up          # Start development stack
make dev-down        # Stop development stack
make dev-logs        # View logs
make dev-tools       # Add pgAdmin
make clean           # Remove all containers and volumes

# Backend commands
cd backend
dotnet build         # Build solution
dotnet test          # Run tests
dotnet ef migrations add {Name}  # Create migration
dotnet ef database update        # Apply migrations

# Frontend commands
cd frontend
bun run dev          # Start dev server
bun run build        # Build for production
bun run lint         # Run ESLint
bun run type-check   # TypeScript check
```

### Project Structure

```
todo-app/
├── backend/                    # .NET 8 Backend
│   ├── Notrelix.Domain/       # Domain entities and logic
│   ├── Notrelix.Application/  # CQRS commands/queries
│   ├── Notrelix.Infrastructure/ # EF Core, Redis, services
│   ├── Notrelix.API/          # API endpoints
│   └── Notrelix.Tests/        # Unit & integration tests
│
├── frontend/                   # Next.js 16 Frontend
│   ├── app/                   # App Router (pages & layouts)
│   ├── features/              # Business logic (hooks, API)
│   ├── components/            # Shared UI components
│   ├── lib/                   # Utilities and configs
│   └── public/                # Static assets
│
├── infra/                      # Infrastructure configs
│   ├── nginx/                 # Nginx configuration
│   └── postgres/              # PostgreSQL init scripts
│
├── .claude/                    # Claude Code integration
│   ├── skills/                # Development skills
│   ├── docs/                  # Quick references
│   └── templates/             # Code templates
│
├── docker-compose.yml          # Base Docker stack
├── docker-compose.dev.yml      # Development overrides
├── Makefile                    # Common commands
└── README.md                   # This file
```

### Coding Standards

- **Backend:** Follow Clean Architecture principles, CQRS pattern
- **Frontend:** Feature-sliced design, Server Components first
- **Database:** snake_case naming, soft deletes, fractional indexing
- **Git:** Conventional commits with domain prefix
- **Testing:** AAA pattern, FluentAssertions

See [AGENTS.md](AGENTS.md) for comprehensive guidelines.

## 🧪 Testing

### Backend Tests

```bash
cd backend

# Run all tests
dotnet test

# Run specific test class
dotnet test --filter "FullyQualifiedName~CreateCardCommandHandlerTests"

# Run with coverage
dotnet test /p:CollectCoverage=true
```

### Frontend Tests

```bash
cd frontend

# Run tests (when configured)
bun test

# Run with coverage
bun test --coverage
```

## 📦 Deployment

### Production Build

```bash
# Build and start production stack
make prod-up

# Or manually:
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

### Environment Setup

1. Set production environment variables
2. Configure SSL certificates
3. Set up database backups
4. Configure monitoring and logging
5. Set up CI/CD pipeline

See deployment documentation for details.

### Commit Convention

```
{type}({domain}): {description}

Types: feat, fix, refactor, chore, test, docs
Domains: auth, workspace, board, docs, calendar, shared

Examples:
feat(board): add card drag-and-drop
fix(docs): prevent race condition in block reorder
refactor(auth): extract token refresh logic
```

### Code Review Process

1. All PRs require review
2. Tests must pass
3. Code must follow conventions
4. Documentation must be updated

## 📧 Contact

- **Author:** Nguyen Quang Vinh
- **GitHub:** [@Nqv1208](https://github.com/Nqv1208)
- **Repository:** [todo-app](https://github.com/Nqv1208/todo-app)

## 📊 Project Status

![GitHub last commit](https://img.shields.io/github/last-commit/Nqv1208/todo-app)
![GitHub issues](https://img.shields.io/github/issues/Nqv1208/todo-app)
![GitHub pull requests](https://img.shields.io/github/issues-pr/Nqv1208/todo-app)

**Current Version:** 0.1.0 (Alpha)

**Status:** 🚧 Active Development

---

<div align="center">

**[⬆ Back to Top](#-notrelix)**

Made with ❤️ by [Nguyen Quang Vinh](https://github.com/Nqv1208)

</div>
