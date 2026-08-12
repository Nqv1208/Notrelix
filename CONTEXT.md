# CONTEXT.md — Notrelix Current Repository Context

> **Current-state evidence, not architecture constitution.**
>
> This file records repository facts, active structural transitions, source-derived authorities, and known drift that a contributor or Coding Agent needs to understand before changing Notrelix.

`CONTEXT.md` answers:

> **What is true in the repository now?**

It does not answer:

> **What should the architecture permanently be?**

Durable product meaning belongs to [`PRODUCT.md`](PRODUCT.md).

Repository invariants belong to [`RULE.md`](RULE.md).

Product design semantics belong to [`DESIGN.md`](DESIGN.md).

Execution procedure belongs to [`AGENTS.md`](AGENTS.md).

Detailed backend/frontend architecture belongs to their canonical project documentation.

---

# 1. Context contract

## 1.1 This file is non-normative current-state evidence

This file may document facts such as:

- a current project/package reference;
- a current folder layout;
- a current test topology;
- a current generated authority;
- a current migration/transition;
- a known source/docs mismatch;
- a legacy structure still present during migration.

Those facts MUST NOT automatically be treated as approved architectural precedent.

Example:

```text
Current fact:
Notrelix.Application references Microsoft.EntityFrameworkCore.

Not automatically implied:
New persistence implementation belongs in Application.
```

Another example:

```text
Current fact:
frontend/packages/features/search exists.

Not automatically implied:
Search is a business bounded context.
```

Another:

```text
Current fact:
docs/engineering currently exists in the checked-in repository.

Not automatically implied:
docs/engineering is an approved canonical authority layer.
```

---

## 1.2 How to use current facts

When current source and canonical intent differ, classify the discrepancy using the repository drift model:

```text
DOC_STALE
SOURCE_DEBT
TRANSITION
CONTRACT_CHANGE
UNRESOLVED
```

See [`AGENTS.md`](AGENTS.md).

Do not resolve drift by silently editing whichever side is easier.

---

## 1.3 Freshness model

This file intentionally does not use a permanent `last_verified_sha` as proof of truth.

The strongest evidence for each current fact is the executable source listed in the relevant section.

When a source-derived fact changes, this file should be updated in the same change if the fact is material for repository navigation or execution.

Exact inventories should be generated where practical rather than copied here indefinitely.

---

# 2. Repository shape

The checked-in repository currently contains the following major areas:

```text
/
├── .agents/skills
├── .claude
├── .github
├── artifacts/contracts
├── backend
├── backups
├── docs
├── docs-refoundation
├── frontend
├── infra
├── scripts
├── AGENTS.md
├── CLAUDE.md
├── CONTEXT-MAP.md
├── CONTEXT.md
├── DESIGN.md
├── MEMORY.md
├── PRODUCT.md
├── README.md
├── RULE.md
├── SKILL.md
├── Makefile
├── docker-compose.yml
├── docker-compose.dev.yml
├── docker-compose.staging.yml
└── docker-compose.prod.yml
```

Important:

- `docs-refoundation/`, root `MEMORY.md`, root `SKILL.md`, and the current `docs/engineering/` tree are part of an active documentation transition and are **not** intended to remain independent permanent authority layers.
- The target documentation architecture is being consolidated around root constitution files, repository cross-stack/product docs, `backend/docs`, and `frontend/docs`.
- Historical/transitional documentation is not product or architecture authority merely because it remains present in the current tree.

---

# 3. Product context

The accepted product-semantic ownership areas are:

```text
Accounts
Identity
Workspaces
Governance
Work Management
Documents
Collaboration
Automation
Integrations
Billing
Analytics / Reporting
```

These are semantic bounded contexts / ownership boundaries.

They are not a requirement for one:

- backend project;
- frontend package;
- database;
- deployable service

per context.

Technical capabilities such as:

```text
Search
Operations tooling
Caching
Messaging infrastructure
Code generation
Background processing
```

may exist as modules/packages/runtime capabilities without becoming independent business bounded contexts.

Repository product meaning is owned by [`PRODUCT.md`](PRODUCT.md) and the detailed product-context documentation as it is completed.

---

# 4. Backend toolchain

## 4.1 Target framework

The shared backend target framework is currently:

```text
net9.0
```

Source authority:

```text
backend/Directory.Build.props
```

---

## 4.2 SDK pin

The backend SDK pin is currently:

```text
9.0.313
```

with:

```text
rollForward: latestPatch
allowPrerelease: false
```

Source authority:

```text
backend/global.json
```

This is currently aligned with the `net9.0` target family.

---

# 5. Backend solution

The backend solution authority is:

```text
backend/backend.slnx
```

It currently includes five production projects:

```text
backend/src/Notrelix.Domain
backend/src/Notrelix.Application
backend/src/Notrelix.Infrastructure
backend/src/Notrelix.Platform
backend/src/Notrelix.API
```

The solution also includes seven primary test projects and four shared test-support projects.

---

# 6. Backend production-project evidence

## 6.1 `Notrelix.Domain`

Current source facts:

- SDK-style .NET project;
- no package references;
- test-only `InternalsVisibleTo` for `Notrelix.Domain.Tests`;
- no production project references.

Current project source:

```text
backend/src/Notrelix.Domain/Notrelix.Domain.csproj
```

Current interpretation:

- Domain is currently the purest backend production project by dependency evidence.
- New provider/persistence/runtime dependencies should not be inferred as acceptable.

Canonical intended contract:

```text
backend/docs/architecture/domain-modeling.md
backend/docs/architecture/backend-overview.md
```

---

## 6.2 `Notrelix.Application`

Current package references include:

```text
MediatR
FluentValidation
FluentValidation.DependencyInjectionExtensions
AutoMapper
Microsoft.EntityFrameworkCore
Microsoft.Extensions.Hosting
```

Current project reference:

```text
Notrelix.Application
→ Notrelix.Domain
```

Current source:

```text
backend/src/Notrelix.Application/Notrelix.Application.csproj
```

Important interpretation:

> EF Core package presence in Application is current source evidence, not permission to place arbitrary persistence ownership in Application.

New code remains constrained by the canonical Application and Infrastructure contracts.

Current structural direction recorded by backend context is module-first use-case placement:

```text
Features/{BoundedContext}/{Module}/Commands/{UseCase}
Features/{BoundedContext}/{Module}/Queries/{UseCase}
```

Legacy/alternate source layouts may still exist.

They are not automatically new-code precedent.

---

## 6.3 `Notrelix.Infrastructure`

Current project references:

```text
Notrelix.Infrastructure
├──→ Notrelix.Application
└──→ Notrelix.Domain
```

Current package evidence includes:

- EF Core;
- Npgsql;
- EF naming conventions;
- ASP.NET Identity persistence support;
- JWT/authentication packages;
- BCrypt;
- Redis;
- Serilog;
- MassTransit;
- RabbitMQ transport;
- Resend.

Infrastructure currently embeds RLS SQL resources under its data area.

Current source:

```text
backend/src/Notrelix.Infrastructure/Notrelix.Infrastructure.csproj
```

Current interpretation:

Infrastructure is the primary concrete adapter/persistence/provider project.

The exact business ownership of a feature still comes from Product/Domain/Application contracts.

---

## 6.4 `Notrelix.Platform`

Current project references:

```text
Notrelix.Platform
├──→ Notrelix.Application
└──→ Notrelix.Domain
```

Current package references are deliberately small:

```text
Microsoft.Extensions.DependencyInjection.Abstractions
Microsoft.Extensions.Logging.Abstractions
```

Platform currently exposes internals only to:

```text
Notrelix.Platform.Tests
```

for the documented test seam around internal platform behavior.

Current source:

```text
backend/src/Notrelix.Platform/Notrelix.Platform.csproj
```

Canonical detailed responsibility:

```text
backend/docs/architecture/platform-and-messaging.md
```

Do not infer Platform responsibility only from package count; inspect its source and canonical contract for messaging/reliability behavior.

---

## 6.5 `Notrelix.API`

Current project references:

```text
Notrelix.API
├──→ Notrelix.Infrastructure
└──→ Notrelix.Application
```

Current package evidence includes:

- API versioning;
- ASP.NET Core OpenAPI;
- Swashbuckle;
- JWT Bearer authentication;
- EF design-time package.

Current source:

```text
backend/src/Notrelix.API/Notrelix.API.csproj
```

Current interpretation:

API is a host/composition/public boundary.

Direct API reference to Infrastructure is a current composition fact and must not be interpreted as permission to move business behavior into API.

---

# 7. Backend test topology

`backend/backend.slnx` currently includes:

```text
tests/Notrelix.Architecture.Tests
tests/Notrelix.Domain.Tests
tests/Notrelix.Application.Tests
tests/Notrelix.Infrastructure.Tests
tests/Notrelix.API.Tests
tests/Notrelix.Integration.Tests
tests/Notrelix.Platform.Tests
```

Shared testing support:

```text
tests/Notrelix.Testing.Core
tests/Notrelix.Testing.Domain
tests/Notrelix.Testing.Application
tests/Notrelix.Testing.Integration
```

The exact suite/project inventory is solution-owned.

Repository quality rules require protected required suites/gates to execute meaningful non-zero work.

---

# 8. Backend canonical documentation entry points

Current backend entry points are:

```text
backend/README.md
backend/AGENTS.md
backend/CONTEXT.md
backend/docs/README.md
```

Canonical backend architecture is intended to remain under:

```text
backend/docs/architecture/
```

Current architecture documents include concerns for:

- backend overview;
- Domain modeling;
- Application model;
- Infrastructure/data;
- Platform/messaging;
- API/contracts;
- security/tenancy/authorization;
- testing/quality gates.

Backend operations documents cover configuration/runtime and data migration concerns.

Backend decision records live under the backend decision registry.

---

# 9. Frontend toolchain

The frontend workspace root is:

```text
frontend/
```

Current runtime/package-manager requirements:

```text
Node.js >= 22.0.0
pnpm >= 10.0.0
packageManager = pnpm@10.0.0
```

Source authority:

```text
frontend/package.json
```

The frontend uses Turborepo for workspace orchestration.

---

# 10. Frontend workspace families

`frontend/pnpm-workspace.yaml` currently includes:

```text
apps/*
packages/foundation/*
packages/runtimes/*
packages/ui/*
packages/product/*/*
packages/features/*
tooling/*
tooling/storybook/web
```

The workspace manifest is the package-discovery authority.

The architecture manifest is the exact registered package-policy/dependency authority for application/package architecture.

---

# 11. Frontend hosts

## 11.1 Web

Path:

```text
frontend/apps/web
```

Package:

```text
@notrelix/app-web
```

Current host facts:

- Vite development server;
- default development port `5173`;
- React;
- TanStack Query;
- TanStack Router;
- web runtime/UI/product/feature packages.

Current command:

```bash
cd frontend
pnpm dev:web
```

---

## 11.2 Marketing

Path:

```text
frontend/apps/marketing
```

Package:

```text
@notrelix/app-marketing
```

Current host facts:

- Next.js;
- default development/start port `3000`;
- imports shared token/web UI/icon packages;
- package currently declares several framework/dependency versions as `latest`, so the lockfile is required for exact resolved versions.

Current command:

```bash
cd frontend
pnpm dev:marketing
```

---

## 11.3 Mobile

Path:

```text
frontend/apps/mobile
```

Package:

```text
@notrelix/app-mobile
```

Current host facts:

- Expo;
- Expo Router entry;
- React Native;
- native runtime;
- native-safe product adapters/UI packages;
- several runtime package versions are currently declared as `latest`, so the lockfile is required for exact resolved versions.

Current command:

```bash
cd frontend
pnpm dev:mobile
```

---

# 12. Frontend executable architecture authority

The exact registered frontend package universe and internal import policy is currently owned by:

```text
frontend/tooling/dependency-rules/src/architecture-manifest.ts
```

The manifest explicitly describes itself as a **closed-world architecture manifest**.

Current checker semantics include:

- every package/app under the covered workspace source areas must appear exactly once;
- unregistered packages fail;
- stale manifest entries fail;
- duplicate package names/paths fail;
- unknown allowed imports fail;
- self-import policy entries fail;
- duplicate allowed-import entries fail.

The exact documentation table is generated from the manifest.

Do not hand-maintain a competing exact package matrix in root/context documentation.

---

# 13. Frontend architecture layers currently represented in the manifest

The manifest currently uses layer concepts including:

```text
foundation
runtime
ui
product-core
product-state
product-collaboration
product-plugin
product-adapter
product-testing
feature
app
```

These are executable frontend architecture classifications.

They are not the same thing as business bounded contexts.

---

# 14. Frontend foundation packages

The current architecture manifest includes foundation packages such as:

```text
@notrelix/contracts
@notrelix/kernel
@notrelix/platform
@notrelix/query
@notrelix/realtime
@notrelix/observability
```

Current path family:

```text
frontend/packages/foundation/*
```

Exact dependency policies are manifest-owned.

---

# 15. Frontend runtime packages

Current runtime packages include:

```text
@notrelix/runtime-web
@notrelix/runtime-mobile
```

Current paths:

```text
frontend/packages/runtimes/web
frontend/packages/runtimes/mobile
```

Runtime packages adapt host/platform concerns.

Their existence helps keep reusable foundation/product code from acquiring host-specific dependencies.

---

# 16. Frontend UI packages

Current UI packages include:

```text
@notrelix/ui-tokens
@notrelix/ui-web
@notrelix/ui-mobile
@notrelix/ui-icons
```

Current paths:

```text
frontend/packages/ui/tokens
frontend/packages/ui/web
frontend/packages/ui/mobile
frontend/packages/ui/icons
```

The token package is currently dependency-free inside the internal manifest.

`ui-web` and `ui-mobile` depend on shared tokens.

Root [`DESIGN.md`](DESIGN.md) owns semantic product design.

The frontend UI source/packages own literal implementation.

---

# 17. Frontend product-package areas

The architecture manifest currently contains dedicated product-package families for:

```text
Work Management
Documents
Automation
```

Current Work Management package roles include:

```text
work-management-core
work-management-state
work-management-plugins
work-management-web
work-management-mobile
work-management-testing
```

Current Documents package roles include:

```text
docs-core
docs-state
docs-collaboration
docs-web
docs-mobile
```

Current Automation package roles include:

```text
automation-core
automation-state
automation-web
automation-mobile
automation-testing
```

These package shapes are current implementation architecture.

They do not imply that other business bounded contexts are semantically less important.

Some product capabilities are currently represented through `features/*` instead.

---

# 18. Frontend feature packages

The current manifest includes feature packages such as:

```text
@notrelix/features-auth
@notrelix/features-workspace
@notrelix/features-account
@notrelix/features-billing
@notrelix/features-integrations
@notrelix/features-notifications
@notrelix/features-activity
@notrelix/features-governance
@notrelix/features-search
@notrelix/features-collaboration
```

Important interpretation:

- `feature` is a frontend architecture classification.
- A feature package is not automatically a business bounded context.
- `features-search` does not change the product bounded-context map.
- Package placement may evolve while product semantic ownership remains stable.

---

# 19. Frontend app composition evidence

The current architecture manifest permits the web app to compose a broad set of:

- foundation;
- runtime-web;
- UI;
- Work Management;
- Documents;
- Automation;
- feature

packages.

The marketing app is classified separately as:

```text
marketing-isolated
```

and currently has a narrow internal import set centered on:

```text
ui-tokens
ui-web
ui-icons
```

The mobile app currently composes:

```text
runtime-mobile
query
ui-mobile
ui-tokens
work-management-mobile
docs-mobile
automation-mobile
```

according to the architecture manifest.

This is current architecture coverage evidence.

Feature completeness must not be inferred from package registration alone.

---

# 20. Frontend validation commands

`frontend/package.json` currently defines:

```bash
pnpm typecheck
pnpm lint
pnpm format:check
pnpm test
```

Primary test composition:

```text
test
├── test:node
├── test:web
├── test:integration
└── test:mobile
```

Additional generator test:

```text
test:generators
```

Guarded variants currently exist for:

```text
node
web
integration
mobile
generators
```

Architecture and generated contract/document checks currently include:

```bash
pnpm check:architecture
pnpm check:architecture-docs
pnpm codegen:check
```

Validation aggregators:

```bash
pnpm validate:fast
pnpm validate
```

Additional UI/E2E support includes:

```bash
pnpm test:ui:a11y
pnpm test:ui:visual
pnpm test:ui:freeze
pnpm e2e
```

Exact script definitions remain owned by:

```text
frontend/package.json
```

---

# 21. Frontend canonical documentation entry points

Current frontend entry points are:

```text
frontend/README.md
frontend/AGENTS.md
frontend/docs/README.md
```

Canonical frontend implementation architecture is under:

```text
frontend/docs/architecture/
```

Current generated dependency evidence is under:

```text
frontend/docs/generated/
```

Current decision records are under:

```text
frontend/docs/decisions/
```

---

# 22. Cross-stack contract artifacts

The repository contains:

```text
artifacts/contracts/
```

for generated/versioned contract artifacts shared across stack/tooling flows.

Cross-stack contract changes may affect:

- backend public/API producer;
- OpenAPI;
- contract artifacts;
- frontend generated contract source;
- web/mobile consumers;
- integration/realtime consumers where applicable.

Exact producer/consumer mechanics belong to the contract architecture documents and source tooling.

Do not hand-copy backend DTOs into frontend source as an independent truth.

---

# 23. Development infrastructure

The current development Compose authority is:

```text
docker-compose.dev.yml
```

The root Makefile uses it directly for development commands.

The development stack currently defines:

```text
PostgreSQL 16
Redis 7
RabbitMQ 3.13             optional messaging profile
.NET SDK 9 backend
Node 22 marketing host
Node 22 web host
Nginx 1.27 gateway
pgAdmin 9                 optional tools profile
```

---

# 24. Current development ports

Default current development ports include:

```text
PostgreSQL           5432
Redis                6379
RabbitMQ             5672
RabbitMQ management  15672
Backend API          8000
Marketing            3000
Web                   5173
Gateway               3080
pgAdmin               5050
```

Environment variables may override these defaults.

Port defaults are operational current facts, not durable architecture semantics.

---

# 25. Development environment files

The root Makefile currently defaults to:

```text
.env.dev
.env.staging
.env.prod
```

for the respective environment commands.

The repository contains:

```text
.env.example
```

as the environment-variable discovery/template entry point.

Real secrets must not be committed.

---

# 26. Current Makefile command surface

The root Makefile currently provides command groups for:

- development stack;
- logs;
- tools profile;
- messaging profile;
- database restore/migrate/seed/RLS;
- backend build/test/shell;
- staging;
- production;
- Compose configuration;
- documentation check;
- cleanup.

Examples:

```bash
make dev-up
make dev-down
make dev-logs
make dev-tools
make messaging-up

make db-up
make db-migrate
make db-seed
make db-init
make db-rls

make be-build
make be-test

make config-dev
make docs-check
```

The Makefile is the exact authority for root make targets.

Read destructive targets before execution; several reset/clean targets remove Docker volumes.

---

# 27. Documentation current state

The repository is currently in an active documentation re-foundation.

The desired long-term authority model is:

```text
root constitution/router files
    +
docs/ cross-stack/product/governance
    +
backend/docs backend implementation architecture
    +
frontend/docs frontend implementation architecture
    +
generated evidence
    +
ADRs
```

However, the checked-in tree still contains artifacts from an overlapping documentation generation.

---

# 28. Current documentation conflict

The current repository still contains:

```text
docs/engineering/
docs-refoundation/
MEMORY.md
SKILL.md
```

At the same time, the current documentation checker contains `docs/engineering` in its forbidden legacy-authority path list.

It also treats references to `docs/engineering` as forbidden.

Therefore:

> **the checked-in documentation tree and its current governance script are not yet mutually consistent.**

This is a known documentation-transition defect.

Do not resolve it by treating `docs/engineering` as canonical simply because the directory exists.

Do not resolve it by deleting unique knowledge before it has been migrated.

The re-foundation process must:

1. extract retained cross-stack/product/governance knowledge;
2. merge backend/frontend implementation knowledge into their project canonical owners;
3. establish the target repository `docs/` tree;
4. migrate references;
5. remove obsolete authority paths;
6. make documentation governance pass on the final tree.

---

# 29. Root `MEMORY.md` current status

A root `MEMORY.md` currently exists in the checked-in tree.

It is not part of the target permanent documentation authority.

Current-state facts belong in this `CONTEXT.md`.

Durable architectural rationale belongs in ADRs/canonical documents.

Historical project state belongs in Git/PR history.

Do not add new durable architecture rules to the legacy memory snapshot.

---

# 30. Root `SKILL.md` current status

A root `SKILL.md` currently exists in the checked-in tree.

Reusable Coding Agent workflows now live under:

```text
.agents/skills/
```

The root `SKILL.md` is therefore transitional/legacy and is not the intended permanent workflow authority.

Skills describe procedure.

They do not redefine architecture.

---

# 31. Agent skills current location

Current reusable agent skills are stored under:

```text
.agents/skills/
```

The exact current skill inventory should be read from that directory.

A skill may route to:

- product docs;
- backend/frontend canonical docs;
- migration docs;
- validation gates.

It must not override [`RULE.md`](RULE.md).

---

# 32. Current documentation governance script

Current root documentation check:

```bash
make docs-check
```

currently executes:

```text
node scripts/check-documentation.mjs
```

The script presently checks concerns including:

- `file:///` absolute links;
- broken relative Markdown links;
- forbidden legacy authority paths;
- forbidden legacy references;
- duplicate backend ADR IDs;
- duplicate frontend ADR IDs;
- required backend production projects in `backend.slnx`;
- backend overview coverage of those projects;
- required frontend workspace families;
- frontend overview coverage;
- frontend generated package-boundary drift;
- branch/freeze/version-style authority wording.

The documentation-core re-foundation plans to evolve this into stronger composable governance.

Current script behavior remains current evidence until that tooling is replaced.

---

# 33. Important current source facts that are not precedent

This section exists specifically to prevent Coding Agents from converting transitional source into architecture.

---

## 33.1 Application currently references EF Core

Fact:

```text
Notrelix.Application.csproj
→ Microsoft.EntityFrameworkCore
```

Interpretation:

- current source uses EF types/abstractions somewhere in Application;
- this fact must be considered before removing the package.

Not implied:

- arbitrary new DbContext/persistence code belongs in Application;
- Infrastructure ownership is obsolete.

Read the canonical Application/Infrastructure contract before new persistence placement.

---

## 33.2 Search exists in frontend architecture

Fact:

```text
@notrelix/features-search
```

is currently registered in the frontend architecture manifest.

Not implied:

```text
Search = business bounded context
```

Product ownership remains defined by Product/system docs.

---

## 33.3 Marketing/mobile use `latest` declarations

Fact:

some marketing/mobile framework/runtime dependencies are currently declared as:

```text
latest
```

Not implied:

- README/docs should guess exact resolved versions;
- uncontrolled upgrades are an architecture principle.

Exact resolved version requires the lockfile.

Dependency/version policy may be hardened separately.

---

## 33.4 Current documentation paths overlap

Fact:

```text
docs/engineering
backend/docs
frontend/docs
```

currently coexist.

Not implied:

all three are equal canonical owners.

This is an active documentation migration issue.

---

# 34. Current source-authority map

Use these sources when exact current facts matter.

## Backend projects

```text
backend/backend.slnx
backend/**/*.csproj
backend/global.json
backend/Directory.Build.props
backend/Directory.Packages.props
```

## Backend implementation

```text
backend/src/**
backend/tests/**
backend/contracts/**
backend/scripts/**
```

## Frontend workspace

```text
frontend/package.json
frontend/pnpm-workspace.yaml
frontend/pnpm-lock.yaml
frontend/**/package.json
frontend/turbo.json
```

## Frontend architecture

```text
frontend/tooling/dependency-rules/src/architecture-manifest.ts
frontend/docs/generated/package-boundaries.md
```

## Frontend implementation

```text
frontend/apps/**
frontend/packages/**
frontend/tooling/**
frontend/e2e/**
```

## Runtime

```text
docker-compose*.yml
Makefile
infra/**
.env.example
```

## Cross-stack contracts

```text
artifacts/contracts/**
backend contract/OpenAPI producers
frontend codegen consumers
```

## Documentation governance

```text
scripts/check-documentation.mjs
Makefile
.github/**
```

---

# 35. What must not be inferred from this file

Do not infer:

- feature completeness from package existence;
- bounded-context maturity from directory count;
- service boundaries from bounded contexts;
- product semantics from table names;
- authorization from frontend routes/components;
- architecture legitimacy from legacy code;
- exact dependency versions from prose;
- current test health merely from test-project existence;
- successful documentation governance merely because `make docs-check` exists;
- release readiness from the word “freeze” in historical tooling/artifacts.

This is a current-state dossier.

It is not a maturity certificate.

---

# 36. Active documentation migration expectations

While the documentation-core work is being completed:

- new root constitution files should be reviewed one by one;
- product semantics should be stabilized before dependent backend/frontend prose;
- current legacy knowledge should be treated as evidence, not authority;
- competing backend/frontend definitions under repository-level engineering docs should be merged into project owners rather than retained;
- generated inventories should be preserved through their producers;
- transition-only files should be deleted only after knowledge/reference migration is complete.

This section is temporary context.

It should be removed or rewritten after the documentation re-foundation is certified.

---

# 37. Context update triggers

Update this file when any of the following material facts change.

## Backend

- production project set;
- solution file/location;
- target framework/SDK family;
- major project-reference direction;
- canonical source layout transition;
- test-project topology;
- significant intentional transitional dependency.

## Frontend

- host set;
- workspace family set;
- package architecture producer;
- runtime/package-manager minimum;
- current product-package family shape when materially relevant;
- primary validation command model.

## Runtime

- primary development Compose model;
- primary local service set;
- environment-file convention;
- material developer entry points.

## Documentation

- authority topology;
- migration/legacy path status;
- generated authority producer;
- docs governance command/model.

Do not update CONTEXT for trivial internal renames that do not affect contributor understanding.

---

# 38. Relationship to `CONTEXT-MAP.md`

This file answers:

> What exists now?

[`CONTEXT-MAP.md`](CONTEXT-MAP.md) answers:

> Given a task, what authority should I read?

Do not duplicate the entire routing table here.

Examples:

```text
Need current backend project set?
→ CONTEXT.md
→ backend/backend.slnx

Need Domain rule?
→ CONTEXT-MAP.md
→ backend/docs/architecture/domain-modeling.md

Need current frontend package permission?
→ architecture manifest

Need Work Management meaning?
→ PRODUCT.md
→ product-context owner
```

---

# 39. Relationship to project contexts

Backend has a scoped current-state context:

```text
backend/CONTEXT.md
```

Root CONTEXT SHOULD summarize only facts useful across the repository.

Backend-specific package/reference/transitional detail may live in backend CONTEXT.

Do not create a separate CONTEXT file in every backend project.

Frontend exact package architecture is better represented by executable manifest/generated evidence than by another manually maintained frontend CONTEXT snapshot.

---

# 40. Current-context completion standard

This file is healthy when:

- it reflects source-derived current repository facts;
- it identifies active transitions without blessing them as permanent architecture;
- it avoids duplicating exact generated inventories;
- it routes exact facts to executable producers;
- it distinguishes business bounded contexts from technical modules;
- it distinguishes implementation evidence from intended architecture;
- it contains no roadmap promises;
- it contains no fake maturity/freeze certification;
- it can be updated without rewriting product/architecture constitutions.

The intended result is:

> **A contributor can understand the repository state they are actually standing in without confusing that state with the architecture the project is protecting.**
