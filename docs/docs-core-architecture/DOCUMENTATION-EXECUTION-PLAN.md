# Notrelix Documentation Core — Migration and Execution Plan

**Companion:** `DOCUMENTATION-CORE-SPEC.md`  
**Target branch:** a dedicated documentation architecture branch from current `develop`  
**Execution policy:** no destructive legacy deletion before knowledge migration is proven.

---

# 1. Objective

Transform the current `develop` documentation state into the documentation-core architecture defined in the companion specification.

This plan is intentionally implementation-oriented. A Coding Agent must not choose an alternative file topology or silently collapse/expand documents without an explicit architecture decision.

---

# 2. Current-state blockers that must be resolved first

1. `backend/docs` and `frontend/docs` already form a newer canonical architecture.
2. `docs/engineering/02-backend` and `docs/engineering/03-frontend` introduce duplicate backend/frontend canonical authority.
3. Current documentation governance script forbids `docs/engineering`, so source and governance disagree.
4. Root README contains stale paths/topology and a misleading backend diagram.
5. Root PRODUCT uses outdated primary product framing.
6. Root DESIGN duplicates implementation-level token detail.
7. Root MEMORY is a stale generated snapshot.
8. Root SKILL duplicates workflow authority now owned under `.agents/skills`.
9. Backend AGENTS references project-level AGENTS files that do not exist.
10. Backend PROJECT-MAP is only partially source-derived.
11. Application project README duplicates canonical Application architecture and contains migration phases.

These are Phase 0 acceptance blockers.

---

# 3. Execution phases

## Phase 0 — Baseline lock

Actions:

1. Create branch:
   `refactor/docs-core-architecture`.
2. Record exact `develop` SHA in PR description, not as permanent canonical frontmatter.
3. Run current docs checks and capture failures.
4. Inventory every current Markdown under:
   - root;
   - `docs/engineering`;
   - `docs-refoundation`;
   - `backend`;
   - `frontend`.
5. Freeze legacy docs from further feature edits while migration is in progress.

Deliverable:

```text
docs-migration/INVENTORY.md
```

Temporary only.

Exit:

- every current doc has an inventory row;
- current governance failures recorded.

---

## Phase 1 — Topic authority ledger

Create:

```text
docs-migration/TOPIC-AUTHORITY-LEDGER.md
```

For each topic:

```text
Topic
Current owners
Target owner
Evidence
Conflict
Migration actions
Deletion-ready
```

Mandatory topics:

- product model;
- bounded contexts;
- design;
- Domain;
- Application;
- Infrastructure/data;
- Platform/messaging;
- API;
- security/tenancy;
- backend testing;
- frontend overview;
- dependency boundaries;
- hosts;
- API client/contracts;
- state/query;
- realtime;
- UI;
- frontend testing;
- configuration;
- migrations;
- cross-stack contracts;
- events;
- delivery;
- operations.

No deletion until every topic has exactly one target owner.

---

## Phase 2 — Repository root rebuild

### Rewrite

```text
README.md
RULE.md
AGENTS.md
PRODUCT.md
DESIGN.md
CONTEXT.md
CONTEXT-MAP.md
```

### Keep/lightly update

```text
CLAUDE.md
```

### Delete

```text
SKILL.md
MEMORY.md
```

If tool compatibility requires MEMORY, replace with a <=30-line pointer explicitly marked non-normative.

### Root acceptance

- README uses current source paths.
- backend diagram matches reference direction.
- Product list does not call Search a business BC unless product docs do.
- README no longer points to old frontend docs.
- RULE includes complete NRX constitution.
- AGENTS does not allow scoped AGENTS to override RULE.
- PRODUCT aligned with Work Management field/view model.
- DESIGN separates semantic intent from literal tokens.
- CONTEXT only current facts.
- CONTEXT-MAP maps every primary change class.
- no root SKILL.

---

## Phase 3 — Rehome repository-level docs

Create target:

```text
docs/
    README.md
    governance/
    architecture/
    product/
    quality/
    delivery/
    operations/
    infrastructure/
    decisions/
    templates/
    generated/
```

Migrate from `docs/engineering`.

### Governance

Review every file under old `00-governance`.

Do not blindly move all files.

Consolidate overlapping files into the five target governance documents.

Delete old generated rule index after new generator exists.

### System

Migrate useful cross-stack knowledge from old `01-system` into six target architecture docs.

### Backend/frontend duplicates

For every file under:

```text
docs/engineering/02-backend
docs/engineering/03-frontend
```

compare rule-by-rule against:

```text
backend/docs/**
frontend/docs/**
```

Disposition each block:

```text
MIGRATE_TO_PROJECT
DUPLICATE
STALE
CROSS_STACK_REHOME
```

Then delete entire `02-backend` and `03-frontend`.

### Product

Migrate `08-product` into `docs/product`.

This is high-value knowledge and must be content-reviewed, not mechanically copied.

### Other sections

Rehome selected quality/delivery/operations/infrastructure docs.

Consolidate files that only repeat generic principles.

Exit:

- `docs/engineering` can be deleted;
- all retained knowledge has a target;
- no cross-stack file owns backend/frontend implementation rules.

---

## Phase 4 — Backend hardening

Do not rebuild topology.

Harden existing docs.

### `backend/AGENTS.md`

Remove nonexistent scoped AGENTS references.

Keep `backend/tests/AGENTS.md`.

### `backend/PROJECT-MAP.md`

Replace with generated:

```text
backend/docs/generated/project-map.md
```

Add generator.

### Application README

Delete or reduce to short orientation pointer.

No migration phases.

### Architecture content hardening

Expand:

```text
domain-modeling.md
application-model.md
infrastructure-and-data.md
platform-and-messaging.md
api-and-contracts.md
security-tenancy-authorization.md
testing-and-quality-gates.md
```

Use source types/tests as evidence.

Important: do not copy generic content from old `docs/engineering/02-backend` if current source contradicts it.

### Backend operations

Verify and harden configuration/migration docs.

### ADRs

Keep accepted history.

Normalize registry and references.

Exit:

- Coding Agent can implement a backend use case without needing old docs/engineering.
- exact project graph is generated.
- no broken AGENTS route.

---

## Phase 5 — Frontend hardening

Retain current topology.

### Dependency boundaries

Preserve architecture manifest as exact authority.

Do not replace this with manually maintained tables.

### Deepen state/realtime

The existing documents are correct in direction but insufficient in operational detail.

Add actual query-key/state/realtime protocol references from source.

### Host composition

Verify Vite/Expo/Next ownership and current provider/routing composition.

### UI

Reference actual token/UI package sources.

Do not duplicate literal token values unnecessarily.

### Testing

Create change-type → required-gates matrix.

### ADR

Keep FE ADR history and unique IDs.

Exit:

- app/package ownership is deterministic;
- no web dependency leaks into mobile production graph;
- server-state/realtime contract is implementable without guessing.

---

## Phase 6 — Documentation tooling rebuild

Create `scripts/docs`.

Implement in this order:

1. link checker;
2. authority checker;
3. metadata checker;
4. rule-ID checker;
5. source-inventory checker;
6. document-index generator;
7. rule-index generator;
8. backend project-map generator;
9. generated drift wrapper.

Update Makefile:

```text
docs-check
docs-generate
docs-check-generated
```

`docs-check` must be runnable from clean checkout after dependencies required by generated checks are installed.

Exit:

- new target tree passes;
- `docs/engineering` forbidden;
- root SKILL forbidden;
- duplicate authority names forbidden.

---

## Phase 7 — CI

Add/update docs governance workflow.

Required jobs:

```text
docs-static
docs-source-alignment
docs-generated
```

CI must fail if a required producer is unavailable rather than silently skipping.

Exit:

- clean checkout CI passes;
- intentional mutation tests prove each gate can fail.

---

## Phase 8 — Reference migration

Repository-wide search for old paths.

Mandatory patterns:

```text
docs/engineering
SKILL.md
MEMORY.md
backend/PROJECT-MAP.md
backend/src/Notrelix.Application/README.md
old frontend docs paths
nonexistent scoped backend AGENTS paths
```

Update:

- README;
- AGENTS;
- scripts;
- CI;
- comments only when comments claim authority;
- skill references;
- PR templates.

Exit:

`rg` finds no active legacy authority reference except migration evidence scheduled for deletion.

---

## Phase 9 — Delete migration artifacts

Delete:

```text
docs/engineering/
docs-refoundation/
docs-migration/
```

after evidence is captured in PR/commit history.

Delete root obsolete files.

Do not create `docs/archive`.

Git is the archive.

---

## Phase 10 — Final certification

Run:

### Documentation

```text
make docs-generate
make docs-check
make docs-check-generated
```

### Backend

```text
cd backend
dotnet restore backend.slnx
dotnet build backend.slnx
dotnet test backend.slnx
```

plus architecture/OpenAPI/integration critical gates used by CI.

### Frontend

```text
cd frontend
pnpm install --frozen-lockfile
pnpm check:architecture
pnpm check:architecture-docs
pnpm codegen:check
pnpm typecheck
pnpm lint
pnpm test
pnpm validate
```

Exit only when all relevant required work executes non-zero.

---

# 4. Commit strategy

Recommended sequence:

```text
1 docs: inventory current documentation authority
2 docs(root): rebuild repository documentation constitution
3 docs(system): establish cross-stack documentation owners
4 docs(product): establish canonical product context documentation
5 docs(backend): harden backend canonical architecture
6 docs(frontend): harden frontend canonical architecture
7 docs(tooling): add documentation generators and governance
8 ci(docs): enforce documentation core contracts
9 docs: migrate references and remove obsolete authority
10 docs: certify documentation core architecture
```

Do not combine knowledge migration and mass deletion in the same first commit.

---

# 5. Coding Agent stop conditions

Stop and report instead of guessing when:

- product context ownership is ambiguous;
- Search/Operations status conflicts across source/docs;
- an accepted ADR conflicts with source and has no superseding decision;
- backend pipeline ordering cannot be proven from source/tests;
- message identity/idempotency behavior is inconsistent across Platform code;
- frontend query-key owner differs between product packages;
- realtime protocol lacks a single producer/schema;
- a generated document has an unknown producer;
- security/tenant behavior differs between Application and RLS;
- a proposed docs deletion contains unique still-valid rationale.

---

# 6. Definition of done

The project documentation core is complete when:

- one canonical owner per mapped topic;
- no docs/engineering;
- no root SKILL;
- no stale snapshot MEMORY;
- root entry docs source-aligned;
- product contexts complete;
- backend implementation architecture deep and source-referenced;
- frontend implementation architecture deep and manifest-referenced;
- ADRs normalized;
- generated project/package inventories drift-checked;
- docs governance CI is required;
- no broken relative links;
- no absolute workstation links;
- no duplicate rule IDs;
- no duplicate ADR IDs;
- no active roadmap/freeze/migration tracker as authority;
- no project/package docs added only for symmetry;
- clean checkout validation passes.

The documentation subsystem may then be considered a protected core foundation.
