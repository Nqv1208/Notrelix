<div align="center">

# Notrelix

**Enterprise Work-Management Platform · Workspace Operating System**

A modular SaaS platform for structured work, documents, collaboration, automation, governance, integrations, billing, and operational insight.

[Product](#product) · [Tech Stack](#tech-stack) · [Quick Start](#quick-start) · [Architecture](#architecture) · [Development](#development) · [Documentation](#documentation)

</div>

---

# Tech Stack

Notrelix is a multi-stack repository: a .NET modular-monolith backend, a pnpm/Turborepo multi-host frontend, PostgreSQL/Redis/RabbitMQ-backed runtime infrastructure, and generated contracts connecting backend and frontend.

The table below is intentionally near the top of the repository entry point. It gives contributors a fast, source-aligned view of the technologies they are about to work with.

| Area | Current technology / role |
|---|---|
| **Backend target framework** | .NET 9 / ASP.NET Core |
| **Backend architecture** | Modular Monolith · DDD · CQRS-style Application use cases |
| **Application orchestration** | MediatR · FluentValidation |
| **Persistence** | EF Core · Npgsql / PostgreSQL |
| **Database** | PostgreSQL 16 |
| **Tenant data defense** | Application authorization + PostgreSQL RLS defense-in-depth |
| **Cache** | Redis 7 · StackExchange.Redis |
| **Messaging transport** | MassTransit · RabbitMQ 3.13 |
| **Authentication** | ASP.NET Core JWT Bearer; OAuth provider integrations |
| **API contracts** | ASP.NET Core OpenAPI · Swashbuckle · versioned/generated contract artifacts |
| **Logging / observability adapters** | Serilog-based backend logging; frontend observability package boundary |
| **Email adapter** | Resend; SMTP fallback path |
| **Frontend workspace** | pnpm 10 · Turborepo |
| **Frontend runtime requirement** | Node.js ≥ 22 |
| **Frontend language** | TypeScript, strict workspace conventions |
| **Web application** | React 19 · Vite 8 · TanStack Router |
| **Marketing application** | Next.js, version resolved by the frontend lockfile |
| **Mobile application** | Expo · React Native · Expo Router, versions resolved by the frontend lockfile |
| **Server-state/query** | TanStack Query v5 |
| **Web styling** | Tailwind CSS 4 · shared Notrelix tokens |
| **Web UI ecosystem** | Notrelix UI primitives · shadcn-derived components · Radix primitives · Lucide |
| **Charts / visualization** | Recharts where appropriate |
| **Validation / schemas** | Zod in frontend contract/form boundaries where used |
| **Frontend tests** | Vitest · Playwright · axe-based accessibility checks |
| **Containers** | Docker · Docker Compose |
| **Gateway / reverse proxy** | Nginx |
| **CI/CD** | GitHub Actions |
| **Repository documentation** | Markdown-as-code · canonical authority + generated evidence + docs governance |

## Version authority

Do not treat this table as the exact authority for every dependency version.

Exact versions are owned by executable manifests and lockfiles:

```text
backend/global.json
backend/Directory.Build.props
backend/Directory.Packages.props
backend/**/*.csproj

frontend/package.json
frontend/pnpm-workspace.yaml
frontend/pnpm-lock.yaml
frontend/**/package.json

docker-compose*.yml
```

When README and an executable manifest disagree, investigate the drift rather than silently copying one side.

## Current backend SDK alignment note

The backend currently targets `net9.0`, and the development Compose backend image uses the .NET 9 SDK.

At the same time, `backend/global.json` currently requests SDK `10.0.201`.

That is a toolchain-alignment issue, not a reason to redefine the backend product/runtime as .NET 10.

Before relying on a host-installed SDK or certifying the Docker backend workflow, reconcile the SDK pin with the intended target/tooling policy.

---

# Product

Notrelix is a **workspace operating system for teams**.

It brings structured work, documents, collaboration, automation, governance, integrations, billing, and derived insight into one coherent product without forcing all of those capabilities into one universal object model.

The product is built around two complementary ideas:

> **One authoritative owner for each business fact.**

and, for Work Management:

> **One work model, many views.**

The repository-level product constitution is [`PRODUCT.md`](PRODUCT.md).

The design constitution is [`DESIGN.md`](DESIGN.md).

---

## Work Management at a glance

The central Work Management model is:

```text
Board
├── BoardField       dynamic typed schema
├── BoardItem        authoritative work record
├── BoardGroup       structural organization
└── BoardView        presentation/query configuration
```

Views such as:

```text
Table
Kanban
Calendar
Timeline
Form
Dashboard
```

operate over the same authoritative Board data.

A view does not create an independent copy of BoardItems.

A Kanban card is a representation of a `BoardItem`.

A Kanban column corresponds to the configured grouping-field semantics.

`BoardGroup` is not automatically a status column.

---

## Product capabilities

Notrelix currently organizes business semantics around these bounded contexts:

| Context | Product responsibility |
|---|---|
| **Accounts** | Account/customer-level administration and ownership facts |
| **Identity** | User identity, authentication, sessions, credentials, security lifecycle |
| **Workspaces** | Collaboration tenancy, membership, invitations, workspace organization |
| **Governance** | Permissions, sharing policy, resource-access semantics, administrative/security audit |
| **Work Management** | Boards, dynamic fields, items, groups, views, structured-work semantics |
| **Documents** | Pages, blocks, hierarchy, document-owned content |
| **Collaboration** | Comments, mentions, reactions, notifications, presence/activity semantics |
| **Automation** | Rules, triggers, conditions, actions, execution/scheduling semantics |
| **Integrations** | Provider connections, webhooks, synchronization and anti-corruption boundaries |
| **Billing** | Plans, subscriptions, entitlements, usage/commercial lifecycle |
| **Analytics / Reporting** | Metrics, dashboards and derived insight without taking ownership of source business state |

A bounded context is a **semantic ownership boundary and future extraction seam**.

It is not automatically:

- a .NET project;
- a database;
- a frontend package;
- a deployable service.

Technical capabilities such as search/indexing, background processing, operations tooling, caching, and code generation do not become business bounded contexts merely because they have modules or runtime infrastructure.

Detailed product semantics live under:

```text
docs/product/
```

---

# Product principles

Repository-wide product and engineering invariants are defined in [`RULE.md`](RULE.md).

At a high level, Notrelix protects the following ideas:

- product semantics outrank storage and presentation convenience;
- tenant scope is correctness and security;
- backend authorization is authoritative for protected business operations;
- cross-boundary contracts are explicit and versionable;
- breaking public/persisted changes are migrations;
- consistency and transaction ownership are explicit;
- retryable effects require stable identity/idempotency semantics;
- client caches and realtime projections cannot become competing server truth;
- generated artifacts are producer-owned and drift-checked;
- required validation must execute meaningful work.

README summarizes these principles.

It does not redefine the detailed contracts.

---

# Architecture

Notrelix is currently built as:

```text
modular-monolith backend
+
multi-host frontend
+
explicit public/generated contracts
+
shared runtime infrastructure
```

The architecture is designed so bounded-context ownership can remain explicit without prematurely splitting the product into microservices.

The objective is:

> **preserve extraction seams without paying distributed-systems cost before there is a real reason to extract.**

---

# Backend Architecture

The backend solution contains five production projects:

```text
Notrelix.Domain
Notrelix.Application
Notrelix.Infrastructure
Notrelix.Platform
Notrelix.API
```

The backend solution inventory is owned by:

```text
backend/backend.slnx
```

## Dependency direction

```text
Notrelix.API ───────────────→ Notrelix.Application ─────→ Notrelix.Domain

Notrelix.Infrastructure ────→ Notrelix.Application ─────→ Notrelix.Domain

Notrelix.Platform ──────────→ Notrelix.Application ─────→ Notrelix.Domain
```

This is **not** a vertical runtime stack in which Domain depends on Infrastructure or Platform.

Project responsibilities are separate.

A complete business capability may legitimately require a vertical change across multiple projects while each project preserves its responsibility.

---

## Domain

`Notrelix.Domain` owns:

- aggregates;
- entities;
- value objects;
- deterministic invariants;
- lifecycle;
- Domain events;
- in-memory consistency.

Domain should not own:

- EF Core;
- HTTP;
- Redis;
- provider SDKs;
- current-user lookup;
- network I/O;
- cross-aggregate repository lookup during mutation.

External facts are supplied by Application.

Canonical backend owner:

[`backend/docs/architecture/domain-modeling.md`](backend/docs/architecture/domain-modeling.md)

---

## Application

`Notrelix.Application` owns:

- use-case orchestration;
- commands/queries;
- authorization policy execution;
- tenant/resource resolution;
- external facts required by Domain;
- transaction coordination;
- concurrency/expected-version contract;
- post-commit orchestration contracts;
- Application-facing ports.

Application should not become the concrete owner of:

- provider SDK behavior;
- persistence implementation;
- business invariants already owned by Domain.

Canonical backend owner:

[`backend/docs/architecture/application-model.md`](backend/docs/architecture/application-model.md)

---

## Infrastructure

`Notrelix.Infrastructure` owns concrete adapters such as:

- EF Core/PostgreSQL;
- migrations;
- RLS implementation;
- Redis/cache implementation;
- provider adapters;
- storage/search implementations;
- email adapter;
- persistence mappings.

Infrastructure implements contracts.

It does not define product semantics.

Canonical backend owner:

[`backend/docs/architecture/infrastructure-and-data.md`](backend/docs/architecture/infrastructure-and-data.md)

---

## Platform

`Notrelix.Platform` owns reusable runtime mechanisms such as:

- messaging delivery;
- consumer execution;
- message identity;
- idempotency/dedup;
- ordering;
- retry/dead-letter;
- poison detection;
- background/runtime mechanisms.

Platform must remain business-agnostic enough to be reusable across product contexts.

Canonical backend owner:

[`backend/docs/architecture/platform-and-messaging.md`](backend/docs/architecture/platform-and-messaging.md)

---

## API

`Notrelix.API` is the HTTP/public host boundary.

It owns:

- endpoint composition;
- authentication integration;
- request/result translation;
- public API exposure;
- OpenAPI exposure;
- API versioning integration.

Protected business authorization remains Application-authoritative.

Canonical backend owner:

[`backend/docs/architecture/api-and-contracts.md`](backend/docs/architecture/api-and-contracts.md)

---

# Backend test topology

The backend solution includes dedicated test projects:

```text
Notrelix.Domain.Tests
Notrelix.Application.Tests
Notrelix.Infrastructure.Tests
Notrelix.Platform.Tests
Notrelix.API.Tests
Notrelix.Integration.Tests
Notrelix.Architecture.Tests
```

and shared test-support libraries:

```text
Notrelix.Testing.Core
Notrelix.Testing.Domain
Notrelix.Testing.Application
Notrelix.Testing.Integration
```

A required test/gate that executes zero relevant work is not valid evidence.

Canonical test/gate contract:

[`backend/docs/architecture/testing-and-quality-gates.md`](backend/docs/architecture/testing-and-quality-gates.md)

---

# Frontend Architecture

The frontend is a **pnpm/Turborepo multi-host workspace**.

Workspace patterns are defined by:

```text
frontend/pnpm-workspace.yaml
```

Current package families:

```text
frontend/
├── apps/*
├── packages/
│   ├── foundation/*
│   ├── runtimes/*
│   ├── ui/*
│   ├── product/*/*
│   └── features/*
├── tooling/*
└── tooling/storybook/web
```

The exact package graph and allowed dependency boundaries are executable architecture.

Do not infer legal imports from physical reachability alone.

---

## Hosts

### Web

```text
frontend/apps/web
```

Primary authenticated product application.

Current host stack includes:

- React 19;
- Vite 8;
- TanStack Router;
- TanStack Query;
- shared runtime/product/UI packages.

Development:

```bash
cd frontend
pnpm dev:web
```

The web app currently runs on Vite port `5173` by default.

---

### Marketing

```text
frontend/apps/marketing
```

Public marketing/acquisition surface.

Current host stack includes:

- Next.js;
- React;
- shared Notrelix tokens/web UI where appropriate.

Development:

```bash
cd frontend
pnpm dev:marketing
```

The marketing app currently runs on port `3000`.

Marketing is more expressive visually but does not own authenticated product state.

---

### Mobile

```text
frontend/apps/mobile
```

Native/mobile host.

Current host stack includes:

- Expo;
- React Native;
- Expo Router;
- mobile runtime/UI packages;
- native-safe product packages.

Development:

```bash
cd frontend
pnpm dev:mobile
```

Mobile production paths must remain free from accidental DOM/web-only dependencies.

---

# Frontend package responsibilities

## `foundation/*`

Stable, reusable foundational mechanisms/contracts.

Examples include areas such as:

- contracts;
- kernel;
- query;
- realtime;
- observability;
- shared platform abstractions.

Foundation must not become a dumping ground for feature logic.

---

## `runtimes/*`

Host/runtime adapters.

Examples:

```text
runtime-web
runtime-mobile
```

Runtime-specific environment/platform behavior belongs here rather than contaminating framework-neutral foundation or product logic.

---

## `ui/*`

Design-system implementation.

Conceptually includes:

- shared tokens;
- web primitives;
- mobile primitives;
- icons.

Root [`DESIGN.md`](DESIGN.md) owns semantic product design.

UI packages own implementation.

---

## `product/*/*`

Bounded product capability packages.

Current families include product-specific capability implementations such as:

- Work Management;
- Documents;
- Automation.

Product packages own reusable capability behavior rather than app-specific composition.

---

## `features/*`

Application/cross-product features such as:

- authentication;
- account;
- workspace;
- billing;
- notifications;
- search

where current architecture assigns them.

A `feature` package is not automatically a product bounded context.

---

## `tooling/*`

Developer/build/architecture infrastructure, including concerns such as:

- dependency rules;
- code generation;
- testing;
- ESLint;
- TypeScript configuration;
- Storybook.

Tooling is not product business ownership.

---

# Frontend state model

Durable protected business state remains backend-authoritative.

Frontend uses:

- query cache;
- optimistic updates;
- local UI state;
- realtime events;
- derived projections

to create a responsive experience.

Those mechanisms must converge to authoritative state.

Key principles:

- query keys are scope-aware;
- tenant/workspace transitions cannot leak old-scope state;
- optimistic failure rolls back/reconciles;
- stale responses cannot overwrite newer scope/state;
- realtime duplicate/out-of-order delivery is safe;
- loss of ordering certainty recovers from authoritative data.

Canonical owners:

- [`frontend/docs/architecture/state-query-mutations.md`](frontend/docs/architecture/state-query-mutations.md)
- [`frontend/docs/architecture/realtime.md`](frontend/docs/architecture/realtime.md)

---

# Backend ↔ Frontend Contract Boundary

Backend and frontend communicate through explicit public contracts.

Generated/versioned contract artifacts live under:

```text
artifacts/contracts/
```

Frontend code must not create an independently maintained copy of backend DTO contracts.

A cross-stack contract change must identify:

- producer;
- consumers;
- compatibility;
- generated artifacts;
- rollout/migration when breaking.

Canonical system owner:

[`docs/architecture/contract-boundaries.md`](docs/architecture/contract-boundaries.md)

Backend owner:

[`backend/docs/architecture/api-and-contracts.md`](backend/docs/architecture/api-and-contracts.md)

Frontend owner:

[`frontend/docs/architecture/api-and-contracts.md`](frontend/docs/architecture/api-and-contracts.md)

---

# Repository Structure

Target documentation and source structure:

```text
Notrelix/
├── README.md
├── PRODUCT.md
├── DESIGN.md
├── RULE.md
├── AGENTS.md
├── CONTEXT.md
├── CONTEXT-MAP.md
│
├── docs/
│   ├── README.md
│   ├── governance/
│   ├── architecture/
│   ├── product/
│   ├── quality/
│   ├── delivery/
│   ├── operations/
│   ├── infrastructure/
│   ├── decisions/
│   ├── templates/
│   └── generated/
│
├── backend/
│   ├── README.md
│   ├── AGENTS.md
│   ├── CONTEXT.md
│   ├── backend.slnx
│   ├── src/
│   │   ├── Notrelix.Domain/
│   │   ├── Notrelix.Application/
│   │   ├── Notrelix.Infrastructure/
│   │   ├── Notrelix.Platform/
│   │   └── Notrelix.API/
│   ├── tests/
│   └── docs/
│       ├── README.md
│       ├── architecture/
│       ├── operations/
│       ├── decisions/
│       └── generated/
│
├── frontend/
│   ├── README.md
│   ├── AGENTS.md
│   ├── package.json
│   ├── pnpm-workspace.yaml
│   ├── turbo.json
│   ├── apps/
│   │   ├── web/
│   │   ├── marketing/
│   │   └── mobile/
│   ├── packages/
│   ├── tooling/
│   └── docs/
│       ├── README.md
│       ├── architecture/
│       ├── decisions/
│       └── generated/
│
├── artifacts/
│   └── contracts/
│
├── infra/
├── scripts/
├── .agents/
│   └── skills/
├── .github/
├── docker-compose.yml
├── docker-compose.dev.yml
├── docker-compose.staging.yml
├── docker-compose.prod.yml
└── Makefile
```

The repository tree is an orientation aid.

Executable manifests remain the exact inventory authority.

---

# Quick Start

## Prerequisites

For the current development environment:

- Git;
- Docker;
- Docker Compose;
- Node.js **22 or newer** for local frontend development;
- pnpm **10 or newer**;
- .NET SDK compatible with the backend's active SDK/target configuration for local backend development.

The backend currently has an SDK-pin mismatch described in [Current backend SDK alignment note](#current-backend-sdk-alignment-note). Resolve that before treating host-SDK behavior as certified.

---

## 1. Clone

```bash
git clone https://github.com/Nqv1208/Notrelix.git
cd Notrelix
```

---

## 2. Create development environment file

The root repository includes:

```text
.env.example
```

Create a development environment file:

```bash
cp .env.example .env.dev
```

Populate required values.

At minimum, development Compose requires appropriate database/Redis/JWT values.

Do not commit real secrets.

---

## 3. Start the development stack with Docker

The root Makefile currently defines:

```bash
make dev-up
```

which runs the development Compose stack.

The current `docker-compose.dev.yml` includes:

- PostgreSQL;
- Redis;
- backend;
- marketing frontend;
- web frontend;
- Nginx gateway;
- optional RabbitMQ profile;
- optional pgAdmin profile.

Start:

```bash
make dev-up
```

Inspect:

```bash
make dev-logs
```

Stop:

```bash
make dev-down
```

---

## Development endpoints

Default development Compose ports currently include:

| Service | Default |
|---|---|
| **Gateway** | `http://localhost:3080` |
| **Marketing host** | `http://localhost:3000` |
| **Web host** | `http://localhost:5173` |
| **Backend API** | `http://localhost:8000` |
| **PostgreSQL** | `localhost:5432` |
| **Redis** | `localhost:6379` |
| **RabbitMQ** | `localhost:5672` when messaging profile is enabled |
| **RabbitMQ management** | `http://localhost:15672` when enabled |
| **pgAdmin** | `http://localhost:5050` when tools profile is enabled |

Environment variables may override these defaults.

The Nginx gateway is the preferred integrated entry point when running the complete Docker development stack.

---

## Optional development services

RabbitMQ:

```bash
make messaging-up
```

Stop RabbitMQ:

```bash
make messaging-down
```

pgAdmin/tools:

```bash
make dev-tools
```

---

# Manual Backend Development

From the backend directory:

```bash
cd backend
```

The solution authority is:

```text
backend.slnx
```

Typical commands:

```bash
dotnet restore backend.slnx
dotnet build backend.slnx
dotnet test backend.slnx
```

Run API:

```bash
dotnet run --project src/Notrelix.API/Notrelix.API.csproj
```

Use the exact active SDK policy after resolving the current `global.json`/target-framework mismatch.

---

# Manual Frontend Development

From:

```bash
cd frontend
```

Install:

```bash
pnpm install
```

Run web:

```bash
pnpm dev:web
```

Run marketing:

```bash
pnpm dev:marketing
```

Run mobile:

```bash
pnpm dev:mobile
```

Run all workspace development tasks:

```bash
pnpm dev
```

`frontend/package.json` is the command authority.

---

# Development

## Root Makefile

Use:

```bash
make help
```

to inspect current command support.

Important development targets include:

```bash
make dev-up
make dev-down
make dev-restart
make dev-logs
make backend-logs

make dev-tools

make messaging-up
make messaging-down

make db-up
make db-restore
make db-restore-force
make db-migrate
make db-seed
make db-init
make db-rls
make db-psql

make be-build
make be-test
make be-shell

make config-dev
make docs-check
```

Some reset/clean commands delete Docker volumes.

Read the Makefile before running destructive operations.

---

# Backend Development Model

A complete backend capability may cross:

```text
Product semantics
→ Domain
→ Application
→ Infrastructure / Platform
→ API
→ contracts/tests
```

That does not mean every feature must change every project.

Change only the owners required for a complete correct behavior.

Coding Agents must read:

[`backend/AGENTS.md`](backend/AGENTS.md)

Backend canonical documentation starts at:

[`backend/docs/README.md`](backend/docs/README.md)

---

## Domain changes

Read:

[`backend/docs/architecture/domain-modeling.md`](backend/docs/architecture/domain-modeling.md)

Before implementation, establish:

- aggregate/consistency owner;
- lifecycle;
- external facts;
- semantic no-op;
- failure atomicity;
- version/audit/event behavior.

---

## Application changes

Read:

[`backend/docs/architecture/application-model.md`](backend/docs/architecture/application-model.md)

Establish:

- use-case owner;
- authorization;
- tenant/resource resolution;
- transaction;
- expected version;
- idempotency;
- post-commit behavior.

---

## Infrastructure/data changes

Read:

- [`backend/docs/architecture/infrastructure-and-data.md`](backend/docs/architecture/infrastructure-and-data.md)
- [`backend/docs/operations/migrations-and-data-change.md`](backend/docs/operations/migrations-and-data-change.md)

---

## Platform/messaging changes

Read:

[`backend/docs/architecture/platform-and-messaging.md`](backend/docs/architecture/platform-and-messaging.md)

Pay particular attention to:

- message identity;
- consumer identity;
- dedup/idempotency;
- ordering;
- poison handling;
- retry/dead-letter;
- commit timing;
- tenant context.

---

## API changes

Read:

[`backend/docs/architecture/api-and-contracts.md`](backend/docs/architecture/api-and-contracts.md)

Public contract changes may require frontend codegen/consumer migration.

---

# Frontend Development Model

Coding Agents must read:

[`frontend/AGENTS.md`](frontend/AGENTS.md)

Frontend canonical documentation starts at:

[`frontend/docs/README.md`](frontend/docs/README.md)

---

## Package boundaries

Read:

[`frontend/docs/architecture/dependency-boundaries.md`](frontend/docs/architecture/dependency-boundaries.md)

The exact package graph is not hand-maintained in README.

The architecture manifest/generator owns exact package relationships.

Do not deep-import package internals.

Do not modify architecture rules merely to make an import compile.

---

## Server state and mutations

Read:

[`frontend/docs/architecture/state-query-mutations.md`](frontend/docs/architecture/state-query-mutations.md)

Server state remains backend-authoritative.

---

## Realtime

Read:

[`frontend/docs/architecture/realtime.md`](frontend/docs/architecture/realtime.md)

Realtime complements query/server authority.

It must handle duplicate/out-of-order/reconnect/scope-transition behavior safely.

---

## UI and design system

Read:

- [`DESIGN.md`](DESIGN.md)
- [`frontend/docs/architecture/ui-and-design-system.md`](frontend/docs/architecture/ui-and-design-system.md)

Literal token/component implementation belongs to frontend source.

Root DESIGN owns semantic product design.

---

# Validation

Validation is part of the architecture.

A green command that executes no relevant protected work is not sufficient evidence.

See [`RULE.md`](RULE.md), especially `NRX-016`.

---

## Backend validation

Common full-solution commands:

```bash
cd backend

dotnet restore backend.slnx
dotnet build backend.slnx
dotnet test backend.slnx
```

During implementation, prefer focused tests first, then broaden according to risk.

Important protected categories include:

- Domain;
- Application;
- Infrastructure;
- Platform;
- API;
- Integration;
- Architecture;
- OpenAPI/contract drift where configured.

Canonical owner:

[`backend/docs/architecture/testing-and-quality-gates.md`](backend/docs/architecture/testing-and-quality-gates.md)

---

## Frontend validation

From:

```bash
cd frontend
```

Basic checks:

```bash
pnpm typecheck
pnpm lint
pnpm format:check
pnpm test
```

Architecture/contracts:

```bash
pnpm check:architecture
pnpm check:architecture-docs
pnpm codegen:check
```

Fast validation:

```bash
pnpm validate:fast
```

Full workspace certification:

```bash
pnpm validate
```

The full frontend `test` command currently runs:

```text
node
web
integration
mobile
```

test configurations.

Guarded validation also verifies required test counts for protected suites.

Additional UI/E2E commands include:

```bash
pnpm test:ui:a11y
pnpm test:ui:visual
pnpm test:ui:freeze
pnpm e2e
```

Use them when the changed surface requires them.

Canonical owner:

[`frontend/docs/architecture/testing-and-quality-gates.md`](frontend/docs/architecture/testing-and-quality-gates.md)

---

# Database and Data Changes

Data changes are migrations, not source-only edits.

Use:

[`backend/docs/operations/migrations-and-data-change.md`](backend/docs/operations/migrations-and-data-change.md)

Consider:

- persisted meaning;
- expand/contract;
- backfill;
- RLS;
- indexes;
- deployment order;
- compatibility;
- rollback/roll-forward;
- destructive behavior.

Root Makefile helpers currently include:

```bash
make db-migrate
make db-seed
make db-init
make db-rls
make db-psql
```

Do not edit historical migrations casually after they have become shared history.

---

# Configuration

The root repository provides:

```text
.env.example
```

for environment-variable discovery.

Backend runtime/configuration semantics are documented in:

[`backend/docs/operations/configuration-and-runtime.md`](backend/docs/operations/configuration-and-runtime.md)

Current development infrastructure uses:

```text
docker-compose.dev.yml
```

and environment file:

```text
.env.dev
```

by default through the root Makefile.

Secrets must not be committed.

---

# Documentation

Documentation is a protected core subsystem.

The repository uses distinct document classes rather than one giant handbook.

---

## Root documents

| File | Responsibility |
|---|---|
| [`README.md`](README.md) | Repository/product onboarding and operational entry point |
| [`PRODUCT.md`](PRODUCT.md) | Repository-level product constitution |
| [`DESIGN.md`](DESIGN.md) | Product design constitution |
| [`RULE.md`](RULE.md) | Repository-wide invariants |
| [`AGENTS.md`](AGENTS.md) | Coding Agent execution contract |
| [`CONTEXT.md`](CONTEXT.md) | Current repository facts and active transitions |
| [`CONTEXT-MAP.md`](CONTEXT-MAP.md) | Task → canonical authority routing |

There is intentionally no root architecture handbook duplicating backend/frontend implementation details.

---

## Repository-level documentation

```text
docs/
├── governance/
├── architecture/
├── product/
├── quality/
├── delivery/
├── operations/
├── infrastructure/
├── decisions/
├── templates/
└── generated/
```

Repository `docs/` owns:

- cross-stack architecture;
- detailed product semantics;
- repository governance;
- repository-wide quality/delivery/operations concerns.

It must not duplicate backend/frontend implementation architecture.

---

## Backend documentation

```text
backend/docs/
├── README.md
├── architecture/
├── operations/
├── decisions/
└── generated/
```

Backend implementation architecture belongs here.

---

## Frontend documentation

```text
frontend/docs/
├── README.md
├── architecture/
├── decisions/
└── generated/
```

Frontend implementation architecture belongs here.

---

## Agent skills

Reusable Coding Agent workflows live under:

```text
.agents/skills/<workflow>/SKILL.md
```

Skills describe **procedure**.

They do not redefine architecture.

There is intentionally no root `SKILL.md` in the target documentation architecture.

---

# Documentation Authority

The repository follows:

> **one topic → one canonical normative owner**

Other files may summarize and link.

They must not independently redefine the same contract.

Examples:

```text
Work Management semantics
→ docs/product/contexts/work-management.md

Domain modeling
→ backend/docs/architecture/domain-modeling.md

Frontend dependency boundaries
→ frontend/docs/architecture/dependency-boundaries.md

Exact frontend package graph
→ executable architecture manifest
→ generated package-boundaries.md
```

See:

- `docs/governance/documentation-authority.md`
- `docs/governance/topic-authority-map.md`

---

# Generated Evidence

Generated artifacts describe exact machine-derived inventory.

Examples include:

```text
backend/docs/generated/project-map.md
frontend/docs/generated/package-boundaries.md
docs/generated/document-index.md
docs/generated/rule-index.md
```

Generated files must identify their producer.

Do not hand-edit generated evidence.

---

# Architecture Decisions

Consequential decisions are recorded as ADRs.

Scopes:

```text
docs/decisions/
backend/docs/decisions/
frontend/docs/decisions/
```

ADRs explain why a decision was made.

Canonical architecture documents explain the current contract.

Do not silently rewrite accepted historical decisions.

---

# Contributing

Before changing code:

1. identify product owner;
2. read applicable `NRX-*` rules;
3. read root [`AGENTS.md`](AGENTS.md);
4. read backend or frontend scoped `AGENTS.md`;
5. read canonical topic owner;
6. inspect source/tests/manifests/contracts;
7. classify compatibility/migration;
8. implement the smallest complete change;
9. run focused proof;
10. run broader required gates;
11. update docs/generated evidence when contract changed.

If you know the task but not the owner, start with:

[`CONTEXT-MAP.md`](CONTEXT-MAP.md)

---

# Change Safety

A local refactor may preserve all externally meaningful contracts.

A material contract change may affect:

- product semantics;
- API;
- realtime;
- events/messages;
- package exports;
- persisted meaning;
- tenant scope;
- authorization;
- transaction/retry guarantees;
- design-system behavior.

Such changes require impact/migration handling rather than being labeled “refactor”.

Canonical delivery owner:

`docs/delivery/change-impact-and-migration.md`

---

# Repository Quality Expectations

A Notrelix change is expected to preserve, as applicable:

- semantic ownership;
- architecture boundaries;
- tenant isolation;
- backend authorization;
- deterministic business reasoning;
- consistency/transaction correctness;
- contract compatibility;
- migration safety;
- retry/idempotency;
- server-state authority;
- realtime convergence;
- web/mobile host safety;
- accessibility;
- generated artifact coherence;
- observability/security boundaries;
- meaningful CI evidence.

The target is not maximum ceremony.

The target is **controlled parallel development without architectural guessing**.

---

# Product and Design Expectations

The product identity is:

> **calm · focused · confident**

Authenticated work surfaces should prioritize:

- work;
- state;
- hierarchy;
- useful density;
- predictable interaction;
- accessibility;
- recovery.

Marketing may be more expressive without becoming a disconnected brand or misrepresenting product capability.

See [`DESIGN.md`](DESIGN.md).

---

# Project Status

**Active development / foundation hardening.**

Notrelix is evolving toward stable, protected capability contracts.

“Frozen” does not mean immutable forever.

A frozen foundation means:

- ownership is explicit;
- public/semantic contract is stable enough for parallel teams to rely on;
- tests/architecture gates protect accidental breakage;
- changes require intentional compatibility/migration handling.

Current implementation facts and active transitions belong in:

[`CONTEXT.md`](CONTEXT.md)

Do not infer project maturity from historical roadmaps, freeze checklists, or migration trackers.

---

# Starting Points

## New human contributor

```text
README.md
→ PRODUCT.md
→ RULE.md
→ backend/README.md or frontend/README.md
→ canonical topic docs
→ source/tests
```

Read `DESIGN.md` when changing user-facing behavior.

---

## Coding Agent

```text
RULE.md
→ AGENTS.md
→ scoped AGENTS.md
→ owning product/topic docs
→ source/tests/manifests/contracts
→ evidence/gates
```

Use [`CONTEXT-MAP.md`](CONTEXT-MAP.md) to route unfamiliar changes.

---

## Product change

```text
PRODUCT.md
→ docs/product/<owner>
→ affected backend/frontend canonical docs
→ delivery/migration docs
```

---

## Architecture change

```text
RULE.md
→ canonical architecture owner
→ ADR/exception policy
→ architecture tests/manifests
→ migration
```

---

# Repository Philosophy

Notrelix is not trying to maximize:

- number of services;
- number of projects;
- number of packages;
- number of abstractions;
- number of Markdown files.

It is trying to maximize:

- semantic clarity;
- ownership clarity;
- correctness;
- safe parallel development;
- evolvability;
- reliable product behavior.

The architecture should make the correct path obvious enough that a developer or Coding Agent does not need to invent foundational decisions while implementing ordinary capabilities.

---

<div align="center">

**Notrelix**

**Structured work, knowledge, collaboration, and automation — with one coherent source of product truth.**

</div>
