# Notrelix Documentation Core Architecture Specification

**Status:** Proposed canonical replacement  
**Scope:** repository root, repository-level `docs/`, backend documentation, frontend documentation, agent documentation, generated documentation, documentation governance  
**Baseline:** `develop` branch as inspected on 2026-08-11  
**Intent:** make documentation a protected core subsystem of Notrelix, with explicit authority, ownership, lifecycle, references, executable evidence, and CI enforcement.

---

# 1. Executive architecture decision

Notrelix documentation is treated as a **core architecture subsystem**, not a collection of Markdown notes.

The documentation system MUST answer these questions deterministically:

1. What does Notrelix mean as a product?
2. Which bounded context owns a business concept?
3. Which backend layer/project owns a technical responsibility?
4. Which frontend host/package family owns a client responsibility?
5. Where is a cross-stack contract defined?
6. Which document is normative for a topic?
7. Which artifact only records why a decision was made?
8. Which artifact is generated evidence rather than human-authored truth?
9. Which source/test/manifest/CI gate proves the documented contract?
10. What must change when a contract changes?

A documentation architecture is invalid when the same topic can reasonably be interpreted as canonical in two different places.

The current `develop` branch violates that rule because backend/frontend canonical documents coexist with a second `docs/engineering/02-backend` and `docs/engineering/03-frontend` canonical layer. The target architecture removes that overlap.

---

# 2. Core design principles

## DOC-001 — One topic, one normative owner

Every normative topic MUST have exactly one canonical owner.

Other documents MAY summarize that topic for orientation but MUST:

- link to the canonical owner;
- avoid redefining the rule independently;
- avoid introducing stronger/weaker wording;
- avoid copying volatile implementation inventory.

Examples:

```text
Domain mutation semantics
→ backend/docs/architecture/domain-modeling.md

Frontend dependency graph principles
→ frontend/docs/architecture/dependency-boundaries.md

Exact frontend package graph
→ executable architecture manifest + generated package-boundaries.md

Work Management product semantics
→ docs/product/contexts/work-management.md
```

---

## DOC-002 — Product, system, backend and frontend are separate authority planes

Authority is divided by semantic level:

```text
ROOT
    repository constitution, orientation, agent contract, product/design entry points

docs/
    cross-stack system architecture
    product semantics
    repository-wide governance/quality/delivery/operations/infrastructure

backend/docs/
    backend implementation architecture and backend operational contracts

frontend/docs/
    frontend implementation architecture and generated frontend evidence

source/tests/manifests/CI
    executable evidence

ADRs
    historical rationale for consequential decisions
```

`docs/` MUST NOT redefine backend or frontend implementation architecture already owned below those projects.

---

## DOC-003 — Source is evidence, not automatic precedent

Existing source proves current behavior.

Existing source does NOT automatically prove intended architecture.

When source and canonical documentation disagree, the change owner MUST classify the mismatch:

- `DOC_STALE` — documentation is outdated;
- `SOURCE_DEBT` — source violates accepted architecture;
- `TRANSITION` — migration is intentionally in progress;
- `CONTRACT_CHANGE` — intended architecture itself is changing;
- `UNRESOLVED` — insufficient evidence; decision required.

No Coding Agent may silently choose the cleaner-looking side.

---

## DOC-004 — Generated facts must be generated

Machine-derivable inventories MUST NOT be manually maintained as architectural truth when a reliable producer exists.

Examples:

- backend project graph → `backend.slnx` + `.csproj`;
- frontend package graph → architecture manifest;
- OpenAPI contract → OpenAPI producer;
- route inventory → endpoint/OpenAPI generation;
- package-boundary table → dependency-rule generator;
- documentation index → documentation-index generator;
- rule index → rule-index generator.

Generated files MUST state:

- producer;
- regenerate command;
- do-not-edit warning;
- last generation mechanism, not manually entered package/project counts.

---

## DOC-005 — Plans, audits and freeze artifacts are not architecture

The following document classes MUST NOT be part of the active canonical reading path:

- roadmap;
- migration tracker;
- freeze checklist;
- implementation wave plan;
- one-time audit report;
- readiness snapshot;
- temporary baseline report.

Unresolved work belongs in an issue/project tracker.

Historical recovery belongs in Git history/tags.

A durable decision extracted from those artifacts belongs in a canonical document or ADR.

---

## DOC-006 — Documentation depth follows risk, not symmetry

Do not create a file because a folder exists.

A dedicated document is justified when at least one is true:

- it has a distinct semantic owner;
- it has a distinct lifecycle;
- it has distinct enforcement/evidence;
- it is large enough that combining it materially harms discoverability;
- a high-risk contract needs an explicit stable owner.

Therefore Notrelix does NOT require:

```text
every project → README + AGENTS + RULE + CONTEXT
every package → README
every bounded context → backend project
every architecture topic → separate tiny Markdown
```

---

## DOC-007 — Documentation is executable governance

Normative documentation MUST be protected by automated checks where practical.

At minimum CI MUST enforce:

- broken relative links = 0;
- absolute local `file:///` links = 0;
- duplicate ADR IDs = 0;
- duplicate rule IDs = 0;
- forbidden legacy authority paths = 0;
- generated documentation drift = 0;
- backend documented project set matches solution inventory;
- frontend documented package families match workspace/architecture manifest;
- no active canonical document uses branch-specific or freeze-version authority language;
- required canonical paths exist;
- no unexpected competing `RULE.md`/architecture index files are introduced.

---

# 3. Authority model

## 3.1 Authority classes

### A. Constitution

Defines constraints that apply across the entire repository.

Owner:

```text
/RULE.md
```

Examples:

- product semantics outrank implementation convenience;
- tenant isolation is correctness/security;
- server authorization is authoritative;
- breaking public/persisted contract requires migration;
- generated evidence cannot be manually falsified;
- required CI work must execute non-zero relevant work.

---

### B. Product constitution

Defines what the product means.

Owners:

```text
/PRODUCT.md
/docs/product/**
```

Root PRODUCT is the executive constitution.

Detailed bounded-context semantics live under `docs/product/contexts/`.

Backend/frontend docs may reference product semantics but may not redefine them.

---

### C. Design constitution

Defines product interaction/visual principles.

Owner:

```text
/DESIGN.md
```

Frontend implementation details such as tokens/component packages live in:

```text
/frontend/docs/architecture/ui-and-design-system.md
```

Literal token values remain source-owned when implemented in token packages.

---

### D. Current repository context

Defines verified current-state facts and transitional caveats.

Owner:

```text
/CONTEXT.md
```

It MUST remain concise enough to update when architecture state changes.

It MUST NOT become a durable architecture handbook.

---

### E. Agent execution contract

Defines how Coding Agents read, investigate, change and verify.

Owners:

```text
/AGENTS.md
/backend/AGENTS.md
/frontend/AGENTS.md
/backend/tests/AGENTS.md   # allowed because test workflow materially differs
```

No new scoped AGENTS file may be added without an explicit local-workflow justification.

---

### F. Cross-stack architecture

Owner:

```text
/docs/architecture/**
```

Cross-stack only.

Examples:

- system context;
- bounded-context ownership;
- contract boundaries;
- consistency/data ownership;
- realtime/event boundary;
- capability extraction/seams.

---

### G. Backend architecture

Owner:

```text
/backend/docs/architecture/**
```

It is the sole canonical owner of backend implementation architecture.

---

### H. Frontend architecture

Owner:

```text
/frontend/docs/architecture/**
```

It is the sole canonical owner of frontend implementation architecture.

---

### I. ADR

Records why a consequential decision was made.

Owners:

```text
/docs/decisions/**
/backend/docs/decisions/**
/frontend/docs/decisions/**
```

ADRs do not replace current architecture documents.

---

### J. Generated evidence

Owners:

```text
/docs/generated/**
/backend/docs/generated/**
/frontend/docs/generated/**
```

Generated artifacts are exact inventories/evidence.

They are not rationale.

---

### K. Operational runbooks

Owners:

```text
/docs/operations/**
/backend/docs/operations/**
```

Runbooks describe operation/recovery/execution procedures.

They do not redefine architecture.

---

# 4. Instruction and conflict model

Do not model documentation as a naive precedence list where a nearer file can override a repository invariant.

Use the following model.

## Task intent

Explicit task/user instruction defines desired change.

If the task explicitly changes a frozen architecture/product contract, that is an architecture change request; it does not bypass the architecture change process.

## Repository constraints

`RULE.md` applies everywhere unless the task intentionally changes the rule itself through the required architecture/decision process.

## Execution specialization

Nearest applicable `AGENTS.md` may specialize how work is performed but MUST NOT contradict root RULE or canonical topic owners.

## Topic authority

The canonical document for the changed topic defines the current intended contract.

## Decision rationale

ADRs explain why accepted consequential choices were made.

## Evidence

Source/tests/manifests/contracts/migrations/CI prove current behavior.

### Conflict procedure

When two authoritative-looking artifacts disagree:

1. identify their document classes;
2. use the Topic Authority Map;
3. inspect evidence;
4. classify drift;
5. stop if the conflict changes product/security/public-contract semantics;
6. create/modify ADR when a consequential architecture decision changes;
7. update all summaries/references after canonical owner changes.

---

# 5. Target repository documentation tree

```text
/
├── README.md
├── AGENTS.md
├── RULE.md
├── PRODUCT.md
├── DESIGN.md
├── CONTEXT.md
├── CONTEXT-MAP.md
├── CLAUDE.md
│
├── docs/
│   ├── README.md
│   │
│   ├── governance/
│   │   ├── documentation-authority.md
│   │   ├── documentation-lifecycle.md
│   │   ├── topic-authority-map.md
│   │   ├── decision-and-exception-policy.md
│   │   └── documentation-quality-gates.md
│   │
│   ├── architecture/
│   │   ├── system-overview.md
│   │   ├── bounded-context-map.md
│   │   ├── contract-boundaries.md
│   │   ├── data-ownership-and-consistency.md
│   │   ├── events-realtime-and-delivery-boundary.md
│   │   └── capability-extraction-strategy.md
│   │
│   ├── product/
│   │   ├── README.md
│   │   ├── product-model.md
│   │   ├── product-experience.md
│   │   └── contexts/
│   │       ├── accounts.md
│   │       ├── identity.md
│   │       ├── workspaces.md
│   │       ├── governance.md
│   │       ├── work-management.md
│   │       ├── documents.md
│   │       ├── collaboration.md
│   │       ├── automation.md
│   │       ├── integrations.md
│   │       ├── billing.md
│   │       └── analytics.md
│   │
│   ├── quality/
│   │   ├── engineering-quality-standard.md
│   │   ├── testing-strategy.md
│   │   ├── security-quality-standard.md
│   │   ├── accessibility-standard.md
│   │   └── performance-and-scalability.md
│   │
│   ├── delivery/
│   │   ├── change-classification.md
│   │   ├── change-impact-and-migration.md
│   │   ├── definition-of-done.md
│   │   └── release-rollout-and-recovery.md
│   │
│   ├── operations/
│   │   ├── observability.md
│   │   ├── incident-readiness.md
│   │   ├── recovery-and-data-safety.md
│   │   └── service-degradation.md
│   │
│   ├── infrastructure/
│   │   ├── environment-model.md
│   │   ├── deployment-runtime.md
│   │   └── containerization-and-local-services.md
│   │
│   ├── decisions/
│   │   ├── README.md
│   │   └── SYS-ADR-*.md
│   │
│   ├── templates/
│   │   ├── adr-template.md
│   │   ├── architecture-change-template.md
│   │   ├── feature-spec-template.md
│   │   ├── migration-plan-template.md
│   │   ├── incident-template.md
│   │   └── pr-checklist.md
│   │
│   └── generated/
│       ├── document-index.md
│       └── rule-index.md
│
├── backend/
│   ├── README.md
│   ├── AGENTS.md
│   ├── CONTEXT.md
│   └── docs/
│       ├── README.md
│       ├── architecture/
│       │   ├── backend-overview.md
│       │   ├── domain-modeling.md
│       │   ├── application-model.md
│       │   ├── infrastructure-and-data.md
│       │   ├── platform-and-messaging.md
│       │   ├── api-and-contracts.md
│       │   ├── security-tenancy-authorization.md
│       │   └── testing-and-quality-gates.md
│       ├── operations/
│       │   ├── configuration-and-runtime.md
│       │   └── migrations-and-data-change.md
│       ├── decisions/
│       │   ├── README.md
│       │   └── ADR-*.md
│       └── generated/
│           └── project-map.md
│
├── frontend/
│   ├── README.md
│   ├── AGENTS.md
│   └── docs/
│       ├── README.md
│       ├── architecture/
│       │   ├── frontend-overview.md
│       │   ├── dependency-boundaries.md
│       │   ├── hosts-composition-routing.md
│       │   ├── api-and-contracts.md
│       │   ├── state-query-mutations.md
│       │   ├── realtime.md
│       │   ├── ui-and-design-system.md
│       │   ├── testing-and-quality-gates.md
│       │   └── architecture-change-policy.md
│       ├── decisions/
│       │   ├── README.md
│       │   └── FE-ADR-*.md
│       └── generated/
│           └── package-boundaries.md
│
└── .agents/
    └── skills/
        ├── README.md
        ├── architecture-review/SKILL.md
        ├── add-domain-capability/SKILL.md
        ├── implement-backend-use-case/SKILL.md
        ├── add-frontend-capability/SKILL.md
        ├── contract-change/SKILL.md
        ├── data-migration/SKILL.md
        └── freeze-certification/SKILL.md
```

No root `SKILL.md`.

No root `MEMORY.md`.

No `docs/engineering/` after migration.

No backend `RULE.md`.

No frontend `RULES.md` or `ARCHITECTURE.md`.

---

# 6. Root document contracts

## 6.1 `/README.md`

### Role

Human repository entry point.

### Required content

1. Product summary.
2. Primary capabilities.
3. One-work-model/many-views Work Management summary.
4. Architecture-at-a-glance.
5. Correct backend dependency graph.
6. Correct frontend workspace shape.
7. Bounded-context list sourced from product canonical docs.
8. Repository structure.
9. Prerequisites.
10. Quick start.
11. Backend/frontend validation commands.
12. Documentation map.
13. Contribution entry path.
14. Current development status.

### Forbidden content

- hard-coded pipeline behavior count;
- manually maintained route inventory;
- stale exact package count;
- migration wave status;
- detailed Domain mutation rules;
- duplicated security rulebook;
- `Search` as a business bounded context unless product canonical docs explicitly make that decision;
- obsolete frontend paths;
- `SKILL.md` as root authority;
- misleading layered diagram that visually implies Domain depends on Infrastructure/Platform.

### References

Must link to:

```text
PRODUCT.md
DESIGN.md
RULE.md
AGENTS.md
CONTEXT.md
CONTEXT-MAP.md
docs/README.md
backend/README.md
frontend/README.md
```

---

## 6.2 `/RULE.md`

### Role

Repository constitution.

### Required normative groups

`NRX-*` stable IDs.

At minimum:

- `NRX-001` Product semantics outrank storage/UI convenience.
- `NRX-002` Architectural boundaries are contracts.
- `NRX-003` Tenant isolation is correctness/security.
- `NRX-004` Backend authorization is final for protected business operations.
- `NRX-005` Pure layers remain deterministic and infrastructure-free.
- `NRX-006` Shared/common code requires stable multi-owner semantics.
- `NRX-007` Cross-boundary contracts are explicit and versionable.
- `NRX-008` Breaking persisted/public contract change requires migration.
- `NRX-009` Transaction/consistency ownership is explicit.
- `NRX-010` Retryable side effects require identity/idempotency semantics.
- `NRX-011` Destructive lifecycle/data operations require explicit policy.
- `NRX-012` Secrets and sensitive tenant data never enter unsafe logs/contracts/docs.
- `NRX-013` Generated artifacts are producer-owned and drift-checked.
- `NRX-014` Client cache/local state cannot become competing server truth.
- `NRX-015` Accessibility and host safety are release-quality requirements.
- `NRX-016` Required gates must execute meaningful non-zero work.
- `NRX-017` Architecture exceptions are explicit, owned and reviewable.
- `NRX-018` Canonical documentation changes require evidence/reference updates.

Each rule MUST include:

```text
Intent
Rule
Required consequences
Forbidden consequences
Evidence classes
Canonical detailed owners
```

Do not put low-level implementation algorithms here.

---

## 6.3 `/AGENTS.md`

### Role

Repository-wide Coding Agent operating contract.

### Required content

- task intent vs repository constraints model;
- mandatory initial classification;
- reading protocol;
- Topic Authority Map usage;
- backend/frontend/cross-stack routing;
- source-evidence protocol;
- change-impact inventory;
- stop conditions;
- no-guess policy;
- implementation transaction;
- validation protocol;
- docs update triggers;
- completion report contract.

### Critical correction

Do not state that nearest AGENTS can override root RULE.

Use:

```text
Task intent
→ repository RULE constraints
→ root AGENTS execution contract
→ nearest AGENTS specialization
→ topic canonical docs
→ ADR rationale
→ executable evidence
```

with explicit architecture-change handling when task intent changes a rule.

---

## 6.4 `/PRODUCT.md`

### Role

Executive product constitution.

### Required content

- product thesis;
- users and usage context;
- one-work-model/many-views principle;
- product capability map;
- bounded-context summary;
- product-level invariants;
- product differentiation/non-goals;
- enterprise expectations;
- cross-context user journeys;
- references to `docs/product/**`.

It MUST replace stale Trello/Notion analogies as the primary product definition.

External analogies may be used only as explanatory comparisons, never as architecture definitions.

---

## 6.5 `/DESIGN.md`

### Role

Product design constitution.

### Required content

- `calm · focused · confident`;
- product vs marketing visual register;
- information density;
- visual hierarchy;
- typography semantics;
- color semantics;
- surface/elevation philosophy;
- interaction grammar;
- motion;
- keyboard/pointer/touch expectations;
- loading/empty/error/permission/conflict states;
- accessibility;
- responsive/mobile principles;
- component ownership;
- design-system source ownership;
- anti-patterns.

### Forbidden

Do not manually duplicate every current literal token value if those values are source-owned in UI token packages.

Root DESIGN defines semantic intent.

Frontend UI architecture defines implementation ownership.

---

## 6.6 `/CONTEXT.md`

### Role

Verified current repository snapshot.

### Required content

- current backend topology;
- current frontend topology;
- current public contract boundary;
- current documentation authority model;
- known active transitions;
- current generated authorities;
- known transitional source that must not be treated as precedent.

### Rules

- current facts only;
- no roadmap;
- no permanent architecture philosophy;
- update whenever a fact changes;
- keep materially shorter than canonical architecture corpus.

---

## 6.7 `/CONTEXT-MAP.md`

### Role

Task-to-authority router.

Must map change types to mandatory docs.

Example:

```text
Product semantic change
→ PRODUCT.md
→ docs/product/<owner>
→ docs/delivery/change-impact-and-migration.md

Domain mutation
→ backend/docs/architecture/domain-modeling.md
→ owning product context

Pipeline/transaction
→ backend/docs/architecture/application-model.md
→ backend ADR if relevant

Frontend query/realtime
→ frontend/docs/architecture/state-query-mutations.md
→ frontend/docs/architecture/realtime.md
```

It must not become another architecture handbook.

---

## 6.8 `/CLAUDE.md`

Provider compatibility router only.

Keep approximately 10–30 lines.

It MUST link to AGENTS/RULE/context routing and MUST NOT replicate architecture.

---

# 7. Repository-level `docs/` contracts

## 7.1 `/docs/README.md`

Repository documentation index.

Required sections:

- authority planes;
- reading paths by persona/task;
- document classes;
- generated vs authored;
- decision registries;
- product context index;
- backend/frontend canonical links;
- lifecycle rules;
- documentation governance command.

This is the only repository-level docs index.

---

# 8. Governance document contracts

## `docs/governance/documentation-authority.md`

Canonical owner of the documentation architecture itself.

Must define:

- document classes;
- authority planes;
- conflict model;
- summary vs canonical definition;
- source as evidence;
- generated evidence;
- forbidden competing authorities;
- local scoped-doc admission;
- authority change process.

Primary IDs:

```text
DOC-AUTH-*
```

---

## `documentation-lifecycle.md`

Must define lifecycle:

```text
DRAFT
ACTIVE
SUPERSEDED
GENERATED
```

Do not use `FROZEN` as a generic document lifecycle status.

Freeze is a contract maturity/change-management concept, not a claim that a Markdown file can never change.

Must define review triggers:

- project/package graph change;
- public contract change;
- bounded-context ownership change;
- migration/data model change;
- security model change;
- CI gate change;
- provider/host change.

---

## `topic-authority-map.md`

Machine-auditable table:

```text
Topic
Canonical document
Secondary summary locations
Executable evidence
Decision registry
Owner
```

Examples:

```text
Domain modeling
backend/docs/architecture/domain-modeling.md
backend/README.md summary only
Domain.Tests + Architecture.Tests
backend/docs/decisions/
Backend Architecture

Work Management semantics
docs/product/contexts/work-management.md
PRODUCT.md summary
Domain/Application/API/frontend tests as applicable
docs/decisions/
Product
```

There MUST NOT be two canonical document paths for one topic.

---

## `decision-and-exception-policy.md`

Must distinguish:

- normal implementation decision;
- architecture decision;
- product semantic decision;
- temporary architecture exception;
- operational exception.

Architecture exceptions require:

- owner;
- reason;
- scope;
- expiry/review trigger;
- compensating gates;
- removal plan.

---

## `documentation-quality-gates.md`

Defines what CI validates and why.

Must reference actual scripts.

No quality rule may exist only as prose if it is cheaply automatable.

---

# 9. Cross-stack architecture contracts

## `system-overview.md`

Must define:

- product/system boundary;
- modular monolith + multi-host frontend;
- external actors/systems;
- server-authoritative model;
- cross-stack integration shape;
- high-level trust boundaries;
- system non-goals.

Must reference backend/frontend overviews, not duplicate them.

---

## `bounded-context-map.md`

Canonical system ownership map.

Must define the accepted business contexts:

- Accounts;
- Identity;
- Workspaces;
- Governance;
- Work Management;
- Documents;
- Collaboration;
- Automation;
- Integrations;
- Billing;
- Analytics/Reporting.

Technical support capabilities such as Search/Operations MUST NOT be promoted to bounded contexts unless explicitly approved by product/system architecture.

For each context:

```text
Mission
Owns
Does not own
Primary business objects
Upstream/downstream dependencies
Public facts/events
Extraction considerations
Product-context document
```

---

## `contract-boundaries.md`

Must define contract types:

- REST;
- OpenAPI;
- realtime;
- events/messages;
- generated frontend clients/types;
- package exports;
- persisted schemas where external compatibility exists.

Must define:

- producer;
- consumers;
- identity/versioning;
- compatibility policy;
- additive vs breaking change;
- migration/rollout requirement;
- codegen drift;
- deprecation.

---

## `data-ownership-and-consistency.md`

Cross-context/system consistency model.

Must define:

- authoritative owner;
- aggregate/local transaction;
- cross-context eventual consistency;
- read models/projections;
- cache as projection;
- frontend cache as projection;
- idempotency/retry principles;
- saga/process-manager admission;
- no distributed transaction assumption unless explicitly designed.

---

## `events-realtime-and-delivery-boundary.md`

Cross-stack event taxonomy.

Must distinguish:

```text
Domain event
Integration/public event
Outbox record
Message envelope
Realtime notification
Frontend cache reconciliation
Audit event
Activity event
```

Must reference backend Platform details and frontend realtime details.

---

## `capability-extraction-strategy.md`

Must encode the actual Notrelix position:

- modular monolith now;
- bounded contexts are extraction seams, not current service boundaries;
- no per-context project/service by default;
- extraction trigger criteria;
- contract/data/operational prerequisites;
- forbidden premature split.

---

# 10. Product documentation contracts

`docs/product/` is the canonical owner of business semantics.

Each context document MUST contain:

```text
Mission
Owns
Does not own
Ubiquitous language
Core entities/aggregates/capabilities
Critical invariants
Lifecycle
Authorization/tenant implications at product level
Cross-context contracts
Events/facts exposed
User journeys
Deletion/archive semantics
Failure/conflict semantics
Frontend representation implications
Analytics/reporting implications
Forbidden semantic shortcuts
Testing/change-impact expectations
```

## Work Management must additionally define

- Board as work database/table;
- BoardField dynamic schema;
- BoardItem authoritative record;
- BoardGroup structural grouping distinct from status;
- BoardView as configuration over shared data;
- field-type contract;
- people/relation external validation;
- system fields;
- ordering/fractional ordering semantics;
- table/Kanban/calendar/timeline/form/dashboard semantics;
- Kanban movement modifies grouping field, not an independent card store;
- view validation against schema;
- relations/formulas/rollups dependency graph;
- lifecycle/deletion;
- realtime/event expectations.

This file should be deep. It is a core product semantic contract and should not be shortened to a one-page overview.

---

# 11. Repository-wide quality contracts

## `engineering-quality-standard.md`

Must define:

- clean-code expectations;
- ownership clarity;
- complexity policy;
- dependency hygiene;
- no dead compatibility layers;
- observability/error handling;
- review expectations;
- documentation-as-code expectations.

---

## `testing-strategy.md`

Cross-stack testing philosophy only.

Backend/frontend exact test topology remains in project docs.

Must define:

- test pyramid/contract;
- behavior vs implementation tests;
- architecture tests;
- contract tests;
- integration/E2E;
- deterministic fixtures;
- required non-zero-work principle;
- flaky-test policy;
- when a failure blocks merge.

---

## `security-quality-standard.md`

Repository-wide secure-engineering expectations:

- secrets;
- tenant data;
- logging;
- dependencies;
- auth/authz separation;
- security tests;
- vulnerability handling;
- generated code review boundaries.

Backend-specific RLS/auth pipeline details remain backend-owned.

---

## `accessibility-standard.md`

Must define WCAG target, keyboard behavior, focus, semantic structure, contrast, motion, touch target, screen-reader behavior, loading/error states, and test expectations.

---

## `performance-and-scalability.md`

Must define:

- bounded queries;
- pagination;
- avoiding whole-board/workspace in-memory filtering;
- index/queryability expectations;
- cache invalidation responsibility;
- payload size;
- realtime fan-out;
- frontend rendering/windowing;
- performance evidence;
- no invented numeric SLO where organization has not decided one.

---

# 12. Delivery contracts

## `change-classification.md`

Classify:

```text
local refactor
behavior change
product semantic change
public contract change
schema/data change
architecture change
security change
operational change
```

Each class maps to required docs/tests/ADR/migration.

---

## `change-impact-and-migration.md`

Must define consumer inventory and migration protocol for:

- API;
- event;
- schema;
- package export;
- product semantics;
- authorization;
- frontend server-state contract.

---

## `definition-of-done.md`

Must be evidence-oriented.

A change is not done because code compiles.

Must address:

- intended behavior;
- tests;
- architecture/security gates;
- docs;
- migration;
- generated artifacts;
- rollout;
- observability;
- cleanup of transitional code.

---

## `release-rollout-and-recovery.md`

Must define generic release decision model, flags, compatibility windows, rollback/roll-forward, data migrations, and recovery ownership.

No fabricated production SLOs.

---

# 13. Operations/infrastructure contracts

These documents cover cross-stack runtime/operational concerns only.

Do not duplicate backend configuration details.

Required topics:

- environment naming/separation;
- secrets and config flow;
- deployment topology;
- containers;
- local dependency services;
- observability;
- incident roles;
- degradation modes;
- data recovery;
- backup/restore contract;
- runbook ownership.

Numeric RPO/RTO/SLO values remain explicit organizational decisions if not currently approved.

---

# 14. Backend documentation contracts

Current backend topology is worth retaining, but the documents require content hardening.

## `backend/README.md`

Keep concise.

Role:

- orientation;
- five production projects;
- prerequisites;
- commands;
- docs routing.

Do not turn it into a second architecture handbook.

---

## `backend/AGENTS.md`

Must route by concern.

It MUST NOT reference nonexistent project-level AGENTS files.

Required routing:

```text
Domain
→ domain-modeling.md

Application/pipeline/transaction
→ application-model.md

EF/Postgres/RLS/cache/providers
→ infrastructure-and-data.md

Messaging/outbox/background
→ platform-and-messaging.md

HTTP/OpenAPI
→ api-and-contracts.md

Security/tenant/authz
→ security-tenancy-authorization.md

Testing
→ testing-and-quality-gates.md
```

Only `backend/tests/AGENTS.md` remains as a justified scoped specialization unless another scope later proves a materially different workflow.

---

## `backend/CONTEXT.md`

Keep as backend current-state dossier.

Required:

- exact production projects;
- current notable transitional layouts;
- current generated/source authorities;
- known intentional exceptions.

No permanent rule definitions.

---

## `backend/docs/architecture/backend-overview.md`

Expand current file with:

- explicit project reference graph;
- composition root;
- context placement;
- dependency-zone rationale;
- Platform vs Infrastructure boundary;
- Application vs Platform post-commit boundary;
- cross-context communication model;
- extraction seams;
- architectural exceptions.

Evidence:

```text
backend/backend.slnx
*.csproj
Architecture.Tests
```

---

## `domain-modeling.md`

Current file is a strong seed but must become a complete Domain contract.

Required detailed sections:

### Aggregate admission

A type is an aggregate root only when it owns an independent consistency/lifecycle boundary.

Do not map aggregate roots one-to-one with tables.

### Mutation protocol

Canonical sequence:

```text
lifecycle eligibility
→ validate input/identity
→ validate business invariants
→ normalize
→ semantic no-op detection
→ prepare prospective state
→ commit in-memory mutation once
→ audit
→ version once
→ domain event(s)
```

Document when a mutation legitimately deviates.

### Failure atomicity

Rejected mutations must leave:

- business fields;
- owned collections;
- lifecycle;
- audit;
- version;
- pending events

unchanged.

### Semantic no-op

No meaningful change → no audit/version/event change unless explicitly defined otherwise.

### External facts

Time, actor, cross-root existence/hierarchy/provider facts come from Application.

### ID policy

Typed IDs for aggregate/public contract correctness, not table-per-ID ceremony.

### Events

Completed facts, scope, payload safety, immutable copies, version/name stability, consumer justification.

### Lifecycle/deletion

Archive/revoke/cancel/remove/tombstone semantics; soft delete not default.

### SharedKernel admission

Require stable semantics and multiple legitimate owners.

### Ordering/hierarchy

Deterministic and concurrency-aware.

### Testing matrix

Success/reject/no-op/failure atomicity/version/audit/event/lifecycle/tenant scope.

### Remove from this file

The current shallow "Core Aggregate Coverage" one-line list should be moved to product-context semantics or generated inventory. It does not provide enough architectural value to justify its current form.

---

## `application-model.md`

Must be significantly deeper than current form.

Required:

### Canonical vertical-slice layout

Define current new-code path.

### Request contract taxonomy

Document actual marker/interfaces used for:

- transaction;
- authorization;
- resource scope;
- tenant scope;
- expected version;
- idempotency;
- post-commit;
- cache/realtime where applicable.

Reference real source types.

### Pipeline order

List actual pipeline order from code, not an aspirational ordering.

For each behavior:

```text
purpose
input contract
preconditions
failure semantics
side effects
ordering dependency
tests
```

### Handler contract

Allowed:

- orchestrate;
- load;
- supply external facts;
- invoke Domain;
- map result.

Forbidden:

- ad-hoc auth;
- direct provider/DbContext when boundary forbids;
- commit when pipeline owns commit;
- duplicate Domain invariant.

### Transactions

Define exact transaction owner and `SaveChanges` ownership.

### Expected version

Fail closed.

Unsupported lookup MUST NOT silently skip concurrency.

### Authorization

Pipeline-owned when request declares protected resource.

### Cache

Permission-sensitive key/version, post-commit invalidation.

### Cross-context writes

Explicit use case or event/process boundary.

### Idempotency

Operation identity, request hash/semantics, completion/failure states as implemented.

### Testing

Behavior/gate matrix tied to actual Application tests.

---

## `infrastructure-and-data.md`

Must define:

- DbContext strategy;
- context/project ownership;
- EF configurations;
- converters;
- migrations;
- PostgreSQL;
- RLS;
- Redis;
- provider adapters;
- storage/search;
- repositories/read models;
- caching mechanics;
- connection/transaction lifecycle;
- indexes/queryability;
- no business-rule ownership.

Reference migration folders and integration tests.

---

## `platform-and-messaging.md`

Must be deep.

Required:

- Platform mission;
- Platform vs Infrastructure;
- envelope;
- message/event identity;
- consumer identity;
- dedup key;
- idempotency record lifecycle;
- ordering stream identity;
- exact sequence advancement rule;
- poison identity granularity;
- retry classification;
- dead letter;
- outbox claim;
- delivery state;
- post-commit work;
- tenant/RLS context;
- background scopes;
- observability;
- shutdown/recovery;
- critical test cases.

The document MUST use actual implemented terminology and source references, not only principles.

---

## `api-and-contracts.md`

Must define:

- API thin boundary;
- endpoint conventions;
- auth integration;
- Application authorization relationship;
- resource identity;
- request/result mapping;
- errors/problem details;
- pagination/filter/sort;
- idempotency headers/keys;
- OpenAPI;
- contract generation;
- REST/realtime relationship;
- version/deprecation;
- contract drift gates.

OpenAPI is exact API evidence.

Do not maintain manual route dump.

---

## `security-tenancy-authorization.md`

Must unify:

- authentication vs authorization;
- account/workspace/resource/user scope;
- resource resolution;
- Application policy;
- RLS defense in depth;
- RLS transaction/session lifecycle;
- permission version/cache invalidation;
- background consumer tenant context;
- secret handling;
- CSRF;
- rate limiting;
- audit vs user activity;
- share links;
- security tests.

Reference ADRs instead of copying ADR rationale.

---

## `testing-and-quality-gates.md`

Must map every test project in `backend.slnx` to:

- responsibility;
- what must never be mocked away;
- critical protected scenarios;
- focused command;
- CI job;
- zero-work guard.

Include architecture/OpenAPI/RLS/outbox/idempotency/production-graph evidence.

---

## Backend operations docs

### `configuration-and-runtime.md`

Source-verified config precedence, options, secrets, Docker/local dependencies, environment selection, seed/reset safety.

### `migrations-and-data-change.md`

EF migration rules, pending-model-change rule, expand/contract, backfill, RLS, index changes, deploy ordering, rollback/roll-forward, irreversible migration handling.

---

## Backend generated project map

Move manual `backend/PROJECT-MAP.md` responsibility to:

```text
backend/docs/generated/project-map.md
```

Generate from `backend.slnx` and `.csproj`.

It should include:

- project;
- type;
- project references;
- test relationship;
- generator command.

Do not leave rows saying "inspect csproj later".

---

# 15. Frontend documentation contracts

The current frontend documentation skeleton is structurally sound and should be hardened, not replaced wholesale.

## `frontend/README.md`

Keep concise orientation/commands.

Must reflect `package.json` and workspace manifests.

---

## `frontend/AGENTS.md`

Keep/harden.

Must route by ownership:

```text
host
foundation
runtime
UI
product
feature
tooling
contract
state/query
realtime
```

Must require architecture manifest check before package-boundary changes.

---

## `frontend-overview.md`

Expand with:

- host responsibilities;
- package family responsibilities;
- product vs feature definition;
- foundation admission;
- runtime adaptation;
- UI ownership;
- app composition root;
- contract consumption;
- server authority;
- explicit non-goals.

---

## `dependency-boundaries.md`

Retain current executable-authority model.

Add:

- exact family dependency principles;
- package admission procedure;
- public export policy;
- deep-import policy;
- no circular ownership;
- mobile native-safety rationale;
- generator lifecycle;
- examples of allowed/forbidden dependency changes.

Executable authority remains architecture manifest.

---

## `hosts-composition-routing.md`

Must define per host:

### Web

Vite/React/TanStack Router composition, provider/bootstrap ownership, browser runtime adapters.

### Mobile

Expo/React Native composition, native-safe runtime, no DOM production path.

### Marketing

Next.js/SSR/SEO/public acquisition role, isolation from authenticated app state.

Cross-host:

- session/auth composition;
- route ownership;
- shell;
- environment loading;
- product packages do not own app router.

---

## `api-and-contracts.md`

Must define:

- contract artifact producer;
- generated REST types/client;
- no copied backend DTOs;
- client abstraction;
- error normalization;
- auth/session integration;
- idempotency;
- versioning;
- codegen drift;
- realtime public contract relation.

---

## `state-query-mutations.md`

Must be deep.

Required:

- backend/server authority;
- canonical query-key factory;
- global/account/workspace/resource key scope;
- query ownership by product capability;
- mutation lifecycle;
- optimistic update admission criteria;
- snapshot/rollback;
- patch vs invalidate decision;
- workspace transition protocol;
- request cancellation;
- stale response protection;
- concurrent mutation reconciliation;
- permission-version/cache effect;
- local UI state vs server state;
- offline assumptions;
- tests.

---

## `realtime.md`

Required:

- realtime role;
- connection state machine;
- subscription ownership;
- auth/session dependency;
- message identity;
- sequence/version;
- duplicate handling;
- out-of-order handling;
- gap recovery;
- heartbeat;
- reconnect/backoff;
- bounded dedup;
- cache patch/invalidate rules;
- workspace transition teardown;
- mobile lifecycle;
- tests.

---

## `ui-and-design-system.md`

Frontend implementation contract:

- token source;
- web/mobile primitive ownership;
- accessibility implementation;
- shadcn/vendor code policy;
- product component vs primitive;
- density implementation;
- states;
- keyboard/pointer/touch;
- Storybook/gallery expectations;
- no business workflow hidden in primitives.

Root DESIGN remains semantic design constitution.

---

## `testing-and-quality-gates.md`

Map actual scripts/suites:

- typecheck;
- lint;
- format;
- node tests;
- web tests;
- mobile tests;
- integration tests;
- E2E;
- architecture;
- architecture-docs;
- codegen;
- packaging/container smoke where present;
- `validate:fast`;
- `validate`.

For each change category, define required gate set.

---

## `architecture-change-policy.md`

Permanent replacement for freeze-specific governance.

Define:

- protected foundations;
- normal capability change;
- package graph change;
- host/runtime change;
- architecture ADR trigger;
- contract migration;
- rollout;
- tests/evidence;
- approval expectations.

No fixed freeze version or package count.

---

# 16. ADR architecture

## Namespaces

Repository:

```text
SYS-ADR-001-...
```

Backend:

```text
ADR-001-...
```

Frontend:

```text
FE-ADR-001-...
```

IDs are immutable.

Renaming filename for spelling may not change ID.

## Status

Allowed:

```text
Proposed
Accepted
Superseded
Rejected
Deprecated
```

## Required sections

```text
ID
Status
Date
Owners
Context
Decision
Alternatives Considered
Consequences
Compatibility / Migration
Evidence
Supersedes
Superseded By
```

Do not silently rewrite an accepted ADR to describe a later choice.

Create a superseding ADR.

---

# 17. Canonical document metadata

Canonical Markdown under `docs/`, `backend/docs/architecture`, `backend/docs/operations`, `frontend/docs/architecture` MUST use lightweight metadata.

Example:

```yaml
---
document_id: BE-APP-ARCH
document_type: architecture
status: active
owner: backend-architecture
applies_to:
  - backend/src/Notrelix.Application
evidence:
  - backend/src/Notrelix.Application
  - backend/tests/Notrelix.Application.Tests
review_on:
  - pipeline-contract-change
  - project-reference-change
  - authorization-model-change
---
```

Required fields:

- `document_id`;
- `document_type`;
- `status`;
- `owner`;
- `applies_to`;
- `evidence`;
- `review_on`.

Do NOT require `last_verified_sha`.

Rationale:

A stale SHA creates a false binary signal and requires mechanical churn. CI should validate source-derived claims directly when possible.

Generated files have generated metadata instead.

Root README/AGENTS/RULE/PRODUCT/DESIGN/CONTEXT/CLAUDE do not require frontmatter.

---

# 18. Normative language and rule IDs

Use:

- `MUST`;
- `MUST NOT`;
- `SHOULD`;
- `SHOULD NOT`;
- `MAY`.

Stable rule IDs are required only for important cross-referenceable invariants.

Prefixes:

```text
NRX-*       repository constitution
DOC-*       documentation governance
SYS-*       cross-stack architecture
PROD-*      product semantics
BE-DOM-*    Domain
BE-APP-*    Application
BE-INF-*    Infrastructure/data
BE-PLT-*    Platform/messaging
BE-API-*    API/contracts
BE-SEC-*    security/tenancy
BE-TST-*    backend test gates
FE-ARCH-*   frontend architecture
FE-DEP-*    package boundaries
FE-STATE-*  query/state
FE-RT-*     realtime
FE-UI-*     UI system
FE-TST-*    frontend gates
QLT-*       quality
DEL-*       delivery
OPS-*       operations
INFRA-*     deployment/infrastructure
```

Do not assign IDs to every paragraph.

---

# 19. Reference rules

All authored docs MUST use repository-relative Markdown links.

Forbidden:

```text
file:///Users/...
absolute developer workstation path
unlinked "see docs somewhere"
```

Canonical references SHOULD target document path + section where useful.

Rule references SHOULD include both rule ID and link when outside the owning document.

Example:

```markdown
See [BE-DOM-104 — failure atomicity](../../backend/docs/architecture/domain-modeling.md#failure-atomicity).
```

Source references use repository paths and symbol/type names.

Generated references name producer command.

---

# 20. Skills architecture

Root `SKILL.md` MUST be removed.

Reusable workflows live only in:

```text
.agents/skills/<workflow>/SKILL.md
```

A skill:

- defines procedure;
- links to architecture;
- does not define architecture itself;
- stops when a product/security/public-contract decision is unresolved.

`.agents/skills/README.md` is a registry, not an authority layer.

---

# 21. Files to remove or replace from current develop

## Root

### Delete

```text
SKILL.md
MEMORY.md
```

`MEMORY.md` current generated snapshot is obsolete as durable authority. Long-term rationale belongs in ADRs; current facts belong in CONTEXT.

If a tool requires MEMORY compatibility, replace it with a short non-normative pointer, not a snapshot handbook.

### Rewrite

```text
README.md
PRODUCT.md
DESIGN.md
RULE.md
AGENTS.md
CONTEXT.md
CONTEXT-MAP.md
```

### Keep/lightly update

```text
CLAUDE.md
```

---

## Repository docs

Remove the `docs/engineering/` namespace entirely after migration.

### Migrate/rehome

```text
docs/engineering/00-governance
→ docs/governance

docs/engineering/01-system
→ docs/architecture

docs/engineering/04-quality
→ docs/quality

docs/engineering/05-delivery
→ docs/delivery

docs/engineering/06-operations
→ docs/operations

docs/engineering/07-infrastructure
→ docs/infrastructure

docs/engineering/08-product
→ docs/product

docs/engineering/adr
→ docs/decisions

docs/engineering/templates
→ docs/templates
```

### Delete after knowledge comparison

```text
docs/engineering/02-backend
docs/engineering/03-frontend
```

Their valid knowledge must be merged into existing backend/frontend canonical docs.

Do not retain both.

---

## Temporary migration evidence

After final certification:

```text
docs-refoundation/
```

must be removed from active tree unless an explicit governance record is retained.

Git/PR history is the default archive.

---

## Backend

### Keep and harden

```text
backend/README.md
backend/AGENTS.md
backend/CONTEXT.md
backend/docs/**
backend/tests/AGENTS.md
```

### Replace

```text
backend/PROJECT-MAP.md
→ backend/docs/generated/project-map.md
```

### Delete or reduce to a short pointer

```text
backend/src/Notrelix.Application/README.md
```

Remove migration phases and duplicated canonical layout.

No new project-level README files are required by symmetry.

---

## Frontend

Keep current:

```text
frontend/README.md
frontend/AGENTS.md
frontend/docs/**
```

Harden content.

No app/package README/AGENTS proliferation unless local workflow materially differs.

---

# 22. Documentation CI architecture

Replace the current monolithic contradictory check with composable scripts.

Target:

```text
scripts/docs/
├── check-links.mjs
├── check-authority.mjs
├── check-metadata.mjs
├── check-rule-ids.mjs
├── check-source-inventory.mjs
├── check-generated.mjs
├── generate-document-index.mjs
├── generate-rule-index.mjs
└── generate-backend-project-map.mjs
```

## `check-authority.mjs`

Enforce allowed authority files/directories.

Reject:

- `docs/engineering`;
- root `SKILL.md`;
- unexpected backend `RULE.md`;
- frontend `RULES.md`/`ARCHITECTURE.md`;
- duplicate topic owners;
- active roadmap/freeze/migration tracker inside canonical trees.

## `check-links.mjs`

All Markdown links.

## `check-metadata.mjs`

Canonical docs metadata only.

## `check-rule-ids.mjs`

Unique stable IDs.

## `check-source-inventory.mjs`

Verify:

- backend production project set;
- frontend workspace families;
- required hosts;
- required canonical docs;
- ADR registry integrity.

## `check-generated.mjs`

Run frontend dependency docs check and backend project-map generation drift.

## Generated indices

`docs/generated/document-index.md`:

```text
Document ID
Type
Owner
Status
Path
Applies to
```

`docs/generated/rule-index.md`:

```text
Rule ID
Title
Owner document
```

Never hand edit.

---

# 23. CI workflow

Create:

```text
.github/workflows/docs-governance.yml
```

Trigger when:

- `**/*.md`;
- `scripts/docs/**`;
- backend solution/project files;
- frontend workspace/package/architecture manifest;
- contract producer files;
- ADR directories;
- documentation workflow itself.

Jobs:

```text
docs-static
    links
    authority
    metadata
    rule IDs

docs-source-alignment
    backend project inventory
    frontend package/host inventory

docs-generated
    document index drift
    rule index drift
    backend project-map drift
    frontend package-boundary drift
```

A documentation check that skips required work MUST fail rather than silently pass.

---

# 24. Documentation change protocol

## Normal implementation change

If behavior stays inside current contract:

- code/tests;
- update docs only if explanatory/current-state content changes.

## Contract change

Must update in same PR:

- canonical owner;
- tests/evidence;
- consumers;
- generated artifacts;
- migration/rollout;
- ADR if consequential.

## Architecture change

Must include:

1. current problem;
2. affected authority owner;
3. alternatives;
4. decision;
5. ADR;
6. migration;
7. architecture tests/gates;
8. reference graph update.

## Product semantic change

Must update product context before/with implementation.

Backend/frontend must consume the semantic change; they do not redefine it independently.

---

# 25. Review standard for canonical documents

Every canonical architecture/product document must answer:

- What does this topic own?
- What does it explicitly not own?
- Why does the boundary exist?
- What are the invariants?
- What is allowed?
- What is forbidden?
- What are common failure modes?
- What happens under concurrency/retry/failure where relevant?
- What public/persisted contracts exist?
- Which other areas depend on it?
- What source/tests prove it?
- What changes require ADR/migration?
- What unresolved decisions remain?

If a document cannot answer those questions because the topic does not need them, omit irrelevant sections explicitly rather than filling template prose.

---

# 26. Anti-patterns prohibited after migration

Notrelix MUST NOT recreate:

```text
RULE-v2
RULE-final
architecture-v3
architecture-final-v4
target-standard
freeze-spec-as-canonical
migration-tracker-as-architecture
roadmap-as-product-contract
manual route dump
manual package count
duplicated backend canonical tree at repository docs level
duplicated frontend canonical tree at repository docs level
provider-specific architecture handbook
```

Do not use "enterprise", "final", or version suffixes in filenames as a substitute for lifecycle governance.

---

# 27. Freeze/certification criteria for documentation core

Documentation architecture is freezeable only when:

## Authority

- exactly one canonical owner for every mapped topic;
- no `docs/engineering`;
- no duplicate backend/frontend implementation authority;
- no root SKILL;
- current-state MEMORY snapshot removed.

## Root

- README source-aligned;
- PRODUCT semantic model aligned with product contexts;
- DESIGN semantic, not duplicated token source;
- RULE complete repository constitution;
- AGENTS no broken scoped references;
- CONTEXT current;
- CONTEXT-MAP deterministic;
- CLAUDE router-only.

## Backend

- five-project topology source-aligned;
- Domain/Application/Infrastructure/Platform/API contracts deep enough for coding-agent execution;
- security/RLS/authorization model explicit;
- test/gate mapping explicit;
- project map generated.

## Frontend

- Vite/Expo/Next host model current;
- package architecture manifest remains executable authority;
- state/query/realtime contracts hardened;
- mobile safety explicit;
- generated boundary docs drift-checked;
- test/gate matrix explicit.

## Cross-stack

- product contexts canonical;
- bounded-context map canonical;
- contract/change migration canonical;
- event/realtime taxonomy canonical.

## Governance

- docs CI green;
- broken links 0;
- duplicate rule/ADR IDs 0;
- generated drift 0;
- forbidden authority paths 0;
- source inventory checks green;
- all temporary migration artifacts removed.

---

# 28. Final architecture statement

Notrelix documentation should be large where the system is semantically deep and small where the role is only routing.

The correct optimization is not:

> fewer files, fewer lines.

The correct optimization is:

> minimal ambiguity, sufficient depth, one owner, executable evidence, controlled evolution.

For Notrelix:

```text
root
    explains the project and repository constitution

docs/
    owns cross-stack system/product/governance

backend/docs/
    owns backend implementation architecture

frontend/docs/
    owns frontend implementation architecture

ADRs
    preserve decision history

generated docs
    expose exact machine-derived inventory

source/tests/CI
    prove current behavior

Git/issues/projects
    preserve history and track unfinished work
```

That authority model is the core contract of the documentation subsystem.
