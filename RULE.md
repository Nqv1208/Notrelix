# RULE.md — Notrelix Repository Constitution

> **This file defines repository-wide invariants for Notrelix.**
>
> These rules protect product semantics, architecture boundaries, tenant safety, contract evolution, consistency, reliability, client/server authority, quality, and documentation integrity across the entire repository.

`RULE.md` is normative.

Backend-specific implementation contracts live under [`backend/docs/`](backend/docs/).

Frontend-specific implementation contracts live under [`frontend/docs/`](frontend/docs/).

Detailed product semantics live in [`PRODUCT.md`](PRODUCT.md) and [`docs/product/`](docs/product/).

Product design semantics live in [`DESIGN.md`](DESIGN.md).

Current repository facts live in [`CONTEXT.md`](CONTEXT.md).

Agent execution procedure lives in [`AGENTS.md`](AGENTS.md).

---

# 1. Authority and usage

## 1.1 What this file owns

This constitution owns constraints that remain valid regardless of:

- backend project;
- frontend host;
- package;
- bounded context;
- transport;
- database;
- provider;
- runtime;
- framework;
- team.

A rule belongs here only when violating it can compromise repository-wide:

- product meaning;
- architecture integrity;
- tenant isolation;
- authorization;
- consistency;
- reliability;
- compatibility;
- data safety;
- quality;
- documentation authority.

---

## 1.2 What this file does not own

This file MUST NOT become:

- a complete Domain handbook;
- an Application pipeline specification;
- a database/RLS manual;
- a message-delivery state-machine specification;
- a frontend query/realtime implementation guide;
- a design-token catalog;
- a package dependency matrix;
- an API route catalog;
- a migration tracker;
- a freeze roadmap.

Those concerns have dedicated owners.

This constitution states the repository-wide invariant and routes detailed implementation to the correct canonical owner.

---

## 1.3 Normative language

The words:

- **MUST**
- **MUST NOT**
- **SHOULD**
- **SHOULD NOT**
- **MAY**

are normative.

A `MUST` or `MUST NOT` rule may be changed only through the architecture/product change process described later in this document.

---

## 1.4 Relationship to scoped documentation

A scoped document may specialize implementation.

It MUST NOT weaken or contradict this constitution.

For example:

```text
RULE.md
    says tenant-scoped state must remain isolated

backend security docs
    define server authorization/RLS implementation

frontend state docs
    define cache/query/realtime scope implementation
```

The scoped documents explain **how** the invariant is satisfied in their technology area.

They do not redefine **whether** the invariant applies.

---

## 1.5 Relationship to source

Source code is evidence of current behavior.

Source code is not automatic architectural precedent.

When current source conflicts with this constitution or the canonical detailed owner, classify the mismatch:

```text
DOC_STALE
SOURCE_DEBT
TRANSITION
CONTRACT_CHANGE
UNRESOLVED
```

Do not silently normalize the disagreement.

---

# 2. Rule summary

| Rule | Repository invariant |
|---|---|
| `NRX-001` | Product semantics outrank representation and implementation convenience |
| `NRX-002` | Architecture boundaries are executable contracts |
| `NRX-003` | Tenant isolation is correctness and security |
| `NRX-004` | Backend authorization is authoritative |
| `NRX-005` | Pure business/foundation layers remain deterministic and provider-free |
| `NRX-006` | Shared/common abstractions require stable ownership |
| `NRX-007` | Cross-boundary contracts are explicit, owned, and versionable |
| `NRX-008` | Breaking public or persisted changes are migrations |
| `NRX-009` | Consistency and transaction ownership are explicit |
| `NRX-010` | Retryable effects require stable identity and idempotency semantics |
| `NRX-011` | Lifecycle and destructive data operations require explicit product policy |
| `NRX-012` | Secrets and sensitive data are protected by default |
| `NRX-013` | Generated artifacts are producer-owned and drift-checked |
| `NRX-014` | Client state cannot become competing server truth |
| `NRX-015` | Accessibility and host safety are release-quality contracts |
| `NRX-016` | Required validation must execute meaningful non-zero work |
| `NRX-017` | Architecture exceptions are explicit, owned, and temporary/reviewable |
| `NRX-018` | Documentation, decisions, source evidence, and generated evidence remain coherent |

---

# 3. Repository invariants

# NRX-001 — Product Semantics Outrank Representation and Implementation Convenience

## Intent

Notrelix is a product model implemented through code.

It is not a database schema, REST shape, frontend component tree, or current folder layout that later gets interpreted as product meaning.

The same business capability may be represented differently by:

- Domain objects;
- persistence tables;
- API contracts;
- events;
- frontend query models;
- views;
- provider adapters.

Those representations exist to implement the product contract.

They do not define it independently.

## Rule

Before choosing storage, transport, event, route, package, component, or service representation, the owning product capability MUST define:

- ownership;
- vocabulary;
- lifecycle;
- invariant;
- tenant scope;
- authorization meaning;
- authoritative state;
- cross-context contracts;
- user-visible behavior where relevant.

Implementation MUST preserve that meaning.

## Required consequences

- A table is not automatically an aggregate.
- A route is not automatically a product capability.
- A frontend screen is not automatically a bounded context.
- A provider model is not automatically a product model.
- A Kanban card is a representation of `BoardItem`; it is not a second Work Management entity model.
- `BoardGroup` does not become status merely because a UI groups cards visually.
- Search/index documents remain projections unless a deliberate product decision creates new semantic ownership.
- Renaming source types or moving packages MUST NOT silently rename public/product concepts.
- Backend and frontend MUST reference the same product owner for a shared capability.

## Forbidden

- Designing a feature from database columns first and inferring product semantics afterward.
- Creating separate authoritative data models for Table, Kanban, Calendar, and Timeline.
- Treating a shared provider DTO as a domain model because several contexts use the provider.
- Treating historical source naming as product truth when the canonical product model has changed.
- Adding a bounded context solely because a module/folder/team exists.

## Typical violations

### Violation A — UI creates new semantics

A Kanban implementation stores client-only “column membership” unrelated to the configured grouping field.

Result:

- Table and Kanban disagree;
- realtime cannot converge safely;
- product semantics depend on the current view.

This violates `NRX-001`.

### Violation B — persistence defines aggregate boundary

A table gets a new aggregate root type solely because it has its own primary key.

Result:

- transactional ownership fragments;
- invariants move between unrelated roots;
- code architecture mirrors storage rather than business consistency.

This violates `NRX-001`.

## Evidence

Evidence may include:

- [`PRODUCT.md`](PRODUCT.md);
- owning `docs/product/contexts/*.md`;
- Domain behavior tests;
- API/contract tests;
- frontend product behavior tests;
- migration review when public/persisted meaning changes.

## Canonical detailed owners

- [`PRODUCT.md`](PRODUCT.md)
- `docs/product/product-model.md`
- `docs/product/contexts/*.md`

---

# NRX-002 — Architecture Boundaries Are Executable Contracts

## Intent

Architecture boundaries exist to prevent accidental ownership drift.

They are not suggestions that can be bypassed whenever a direct import/reference makes implementation easier.

## Rule

Production project/package dependency direction, public exports, bounded-context ownership, runtime separation, composition boundaries, and generated contract boundaries MUST be respected.

Where an architecture boundary is important enough to be stable, it SHOULD be enforced by executable tooling/tests rather than documentation alone.

## Required consequences

### Backend

- Domain MUST remain independent from Application, Infrastructure, Platform, and API.
- Application MUST NOT acquire provider/persistence/runtime dependencies that belong outside Application.
- Infrastructure and Platform MUST preserve their distinct responsibilities.
- API MUST remain a public/composition boundary rather than a business-rule owner.
- Bounded-context implementation placement MUST follow canonical ownership even when legacy folders show another precedent.

### Frontend

- Package imports MUST follow the architecture manifest.
- Deep internal imports MUST NOT bypass public package exports.
- Framework-neutral foundation code MUST remain framework-neutral unless explicitly classified otherwise.
- Web runtime dependencies MUST NOT leak into mobile production paths.
- Hosts compose capabilities; reusable product behavior MUST NOT be hidden in app composition code.

### Cross-stack

- Frontend types MUST derive from approved public contract producers instead of hand-copied backend DTOs.
- Cross-stack shortcuts MUST NOT introduce a hidden second contract.

## Forbidden

- Adding a project reference/import because “it compiles”.
- Weakening architecture tests to accommodate one feature.
- Moving code to `Common`, `Shared`, or `foundation` solely to solve a dependency cycle.
- Deep-importing a package because an export was inconvenient.
- Letting API endpoints or frontend apps become the only owner of reusable business behavior.

## Typical violations

```text
Domain → Infrastructure
Application handler → concrete DbContext/provider SDK
mobile product package → @notrelix/ui-web
feature package → app-specific internal module
frontend manually duplicates API DTO
```

## Evidence

- backend architecture tests;
- `backend/backend.slnx`;
- backend `.csproj` references;
- frontend architecture manifest;
- `pnpm check:architecture`;
- `pnpm check:architecture-docs`;
- public-export tests/checks;
- generated contract/codegen checks.

## Canonical detailed owners

- `docs/architecture/system-overview.md`
- `backend/docs/architecture/backend-overview.md`
- `frontend/docs/architecture/dependency-boundaries.md`

---

# NRX-003 — Tenant Isolation Is Correctness and Security

## Intent

Tenant scope is part of the identity and correctness of tenant-owned data.

Cross-tenant observation is not merely an authorization bug.

It is a violation of core product correctness.

## Rule

Account/workspace/resource-scoped data MUST carry enough immutable scope through every relevant boundary to prevent unauthorized cross-tenant observation or mutation.

This applies to:

- reads;
- writes;
- lists;
- searches;
- exports;
- caches;
- frontend query keys;
- realtime subscriptions;
- events/messages;
- background jobs;
- persistence;
- RLS;
- analytics/read models;
- audit facts;
- provider synchronization.

## Required consequences

- Resource IDs alone MUST NOT be trusted as sufficient tenant proof.
- Client “current workspace” state MUST NOT be authoritative tenant scope.
- Protected cache/query keys MUST include the scope required to prevent leakage.
- Tenant-scoped events/messages MUST carry explicit scope.
- Background consumers/jobs touching tenant data MUST establish tenant/RLS execution context.
- Search/index records MUST retain enough source scope for authorized filtering.
- Analytics projections MUST preserve tenant ownership.
- Share links/guests MUST NOT create transitive access to unrelated linked resources.

## Forbidden

- Cache keys like `resource:{id}` when the resource's visibility depends on tenant/user/permission scope and global uniqueness is not the complete security contract.
- A global realtime subscription carrying workspace data without verified scope.
- A consumer opening tenant data without establishing tenant/RLS context.
- Trusting workspace/account identifiers from client input without authoritative resolution.
- Search results filtered only after unauthorized records were already exposed to application/client logic.

## Typical violations

### Cross-workspace cache pollution

A user switches Workspace A → Workspace B, but the same query/cache key is reused.

Workspace A data appears in Workspace B.

### Background RLS bypass

A consumer resolves message payload data through a database connection that never establishes tenant context.

The HTTP path is secure; the background path is not.

## Evidence

- Application authorization tests;
- Infrastructure/RLS tests;
- integration tests;
- frontend query-key/workspace-transition tests;
- realtime subscription tests;
- search/index authorization tests where applicable;
- security review.

## Canonical detailed owners

- `docs/product/contexts/workspaces.md`
- `docs/product/contexts/governance.md`
- `backend/docs/architecture/security-tenancy-authorization.md`
- `frontend/docs/architecture/state-query-mutations.md`
- `frontend/docs/architecture/realtime.md`

---

# NRX-004 — Backend Authorization Is Authoritative

## Intent

Frontend authorization affordances improve usability.

They cannot enforce business security because client state is observable and mutable by the user.

RLS is defense in depth.

It does not replace business authorization policy.

## Rule

Every protected business operation MUST be authorized server-side at the Application/public-use-case boundary using authoritative subject, tenant, resource, permission, and entitlement facts as required.

This applies to protected:

- commands;
- queries;
- list operations;
- search;
- export;
- realtime subscription;
- report/dashboard access;
- background/system actions where a principal/policy applies.

## Required consequences

- Authentication identifies a subject/session; authorization decides whether an action is allowed.
- API host authentication MUST NOT become ad-hoc business authorization.
- Application authorization MUST execute before protected effects.
- Resource scope MUST be resolved from authoritative state when policy requires it.
- Unsupported/unknown authorization facts MUST fail closed.
- Frontend hidden buttons/route guards MUST be treated as UX only.
- RLS MUST complement, not replace, Application authorization.
- Automation/background execution MUST define an execution principal/policy when business authorization is relevant.

## Forbidden

- `if (role == "admin")` scattered through handlers without canonical policy ownership.
- Trusting role/permission values supplied by the client.
- Allowing a query because it is “read only”.
- Using frontend route visibility as proof of authorization.
- Omitting authorization because RLS exists.
- Omitting RLS/tenant defense because Application authorization exists.

## Typical violations

- API endpoint loads and returns a protected resource before invoking Application authorization.
- Search endpoint filters tenant but not resource-level permission.
- Export job runs under service identity without an approved product authorization model.

## Evidence

- Application authorization tests;
- API protected-operation tests;
- tenant/resource-resolution tests;
- Infrastructure/RLS tests;
- frontend permission UX tests for UX behavior only.

## Canonical detailed owners

- `docs/product/contexts/governance.md`
- `backend/docs/architecture/security-tenancy-authorization.md`
- `backend/docs/architecture/application-model.md`

---

# NRX-005 — Pure Business and Foundation Layers Stay Deterministic and Provider-Free

## Intent

Core business reasoning must remain testable, reproducible, and independent of infrastructure/runtime side effects.

The same principle applies to framework-neutral frontend foundation code.

## Rule

Pure business/foundation code MUST receive external facts explicitly and MUST NOT reach outward to discover them implicitly.

External facts include, where relevant:

- current time;
- current actor;
- random input;
- cross-root existence;
- hierarchy/path information;
- provider data;
- network responses;
- filesystem;
- browser/native runtime APIs;
- environment;
- current tenant/session.

## Required consequences

### Backend Domain

- Domain receives actor/time/cross-aggregate/provider facts from Application.
- Domain invariants remain synchronous/deterministic unless an explicitly different architecture is approved.
- No repository/provider callbacks are passed into Domain merely to perform lookup during mutation.

### Frontend foundation

- framework-neutral packages MUST NOT import host/runtime APIs without explicit classification.
- host-specific capabilities belong in runtime/host adapters.

## Forbidden

Backend examples:

```text
DateTime.UtcNow in Domain mutation
Random.Shared in Domain
DbContext in Domain
HTTP call in aggregate
current-user accessor inside entity
repository callback supplied to aggregate
```

Frontend examples:

```text
window/document in framework-neutral core
React DOM implementation inside mobile-safe foundation
environment discovery hidden in reusable product logic
```

## Typical violations

A Domain rule needs to know whether a referenced user exists and queries a repository directly.

Correct pattern:

```text
Application loads authoritative fact
→ passes immutable fact/validated ID into Domain
→ Domain evaluates invariant deterministically
```

## Evidence

- Domain unit tests;
- backend architecture/dependency tests;
- frontend architecture manifest;
- foundation tests;
- determinism tests where applicable.

## Canonical detailed owners

- `backend/docs/architecture/domain-modeling.md`
- `frontend/docs/architecture/dependency-boundaries.md`
- `frontend/docs/architecture/frontend-overview.md`

---

# NRX-006 — Shared/Common Abstractions Require Stable Ownership

## Intent

Shared code reduces duplication only when the shared meaning is genuinely stable.

Premature sharing creates global coupling and turns common folders into uncontrolled ownership zones.

## Rule

An abstraction MAY move into shared/common/foundation code only when its:

- semantics;
- lifecycle;
- dependencies;
- change pressure;
- consumers

are sufficiently compatible to justify a shared owner.

“Used twice” is not enough.

## Required consequences

- SharedKernel types MUST represent genuinely stable cross-context semantics.
- Frontend foundation packages MUST own framework-neutral stable mechanisms, not arbitrary reusable feature code.
- UI primitives MAY be shared when their interaction semantics are product-wide.
- Provider-specific types SHOULD remain behind provider/integration boundaries.
- Cross-context shared abstractions SHOULD be smaller than the contexts they connect.

## Forbidden

- Moving code to `Common` to resolve circular dependency.
- Adding business-specific status/enum to SharedKernel because two modules currently use the same words.
- Putting product workflow into generic UI primitives.
- Putting app/host behavior into frontend foundation.
- Creating a generic `Entity`/`Resource` product abstraction that erases distinct lifecycle semantics without proven shared meaning.

## Typical violations

### Shared status enum

Two contexts both have `Active`.

One means “subscription currently billable”.

Another means “automation enabled”.

They are not the same semantic type.

Sharing them creates coupling.

## Evidence

- ownership review;
- dependency gates;
- public-export review;
- architecture docs;
- context-specific tests.

## Canonical detailed owners

- `backend/docs/architecture/domain-modeling.md`
- `frontend/docs/architecture/dependency-boundaries.md`
- `frontend/docs/architecture/ui-and-design-system.md`

---

# NRX-007 — Cross-Boundary Contracts Are Explicit, Owned, and Versionable

## Intent

Whenever data crosses an ownership/process boundary, consumers rely on a contract.

Implicit contracts are still contracts; they are simply harder to evolve safely.

## Rule

Public/cross-boundary contracts MUST have:

- an explicit producer/owner;
- stable identity;
- defined consumers;
- compatibility semantics;
- change/migration policy.

Applicable boundaries include:

- REST/OpenAPI;
- realtime payloads;
- integration/public events;
- message envelopes;
- generated frontend clients/types;
- package public exports;
- provider webhooks/mappings;
- persisted contract surfaces that require compatibility.

## Required consequences

- Backend DTOs MUST NOT be manually copied into frontend source as independent truth.
- Event/message logical identities MUST remain stable or migrate explicitly.
- Package consumers MUST use public exports.
- Provider contracts MUST be translated at integration boundaries.
- Contract deprecation MUST identify remaining consumers before removal.
- Generated contract outputs MUST be reproducible from their producer.
- Additive and breaking changes MUST be distinguished.

## Forbidden

- Renaming an event/message because a CLR class was renamed, without consumer analysis.
- Changing API response meaning while preserving shape and calling it non-breaking.
- Removing a package export because internal source no longer uses it without checking external workspace consumers.
- Hand-maintained client types that drift from OpenAPI/public contracts.

## Typical violations

### Shape-compatible semantic break

`status = "active"` used to mean “enabled and executable” but is changed to mean “not archived”.

JSON shape remains unchanged.

Contract meaning changed.

This is a breaking semantic change.

## Evidence

- OpenAPI/codegen checks;
- contract tests;
- generated artifact drift tests;
- package-export/dependency checks;
- consumer inventory;
- ADR/migration review when consequential.

## Canonical detailed owners

- `docs/architecture/contract-boundaries.md`
- `backend/docs/architecture/api-and-contracts.md`
- `backend/docs/architecture/platform-and-messaging.md`
- `frontend/docs/architecture/api-and-contracts.md`

---

# NRX-008 — Breaking Public or Persisted Changes Are Migrations

## Intent

A breaking change is not made safe by being committed atomically to Git.

Consumers, persisted data, asynchronous messages, old clients, and deployment order may span versions.

## Rule

Any change that breaks existing:

- public semantics;
- API contracts;
- realtime contracts;
- event/message contracts;
- package exports;
- persisted schema/meaning;
- lifecycle meaning;
- authorization scope;
- generated consumer assumptions

MUST be treated as a migration.

## Required consequences

A breaking change MUST identify, as applicable:

- producer;
- consumers;
- persisted records;
- compatibility window;
- rollout order;
- backfill/data transformation;
- dual-read/dual-write need;
- deprecation;
- rollback vs roll-forward;
- generated artifact update;
- tests/gates;
- docs/ADR update.

## Forbidden

- “Just regenerate the client.”
- Editing an old migration to represent new desired state after it has become part of shared history.
- Renaming public identifiers/events without compatibility handling.
- Dropping a column before all readers/writers stop depending on it.
- Removing old enum/status values while durable records/messages still contain them.
- Assuming frontend/backend deploy simultaneously unless the deployment architecture explicitly guarantees it.

## Typical violations

### Schema-first destructive rollout

Release A removes column `x`.

Release B is the first frontend/backend version that stops reading `x`.

Deployment order can break live traffic.

Correct approach requires a compatible expand/contract sequence.

## Evidence

- migration plan;
- EF migrations/schema tests;
- contract tests;
- consumer inventory;
- rollout/recovery plan;
- generated code checks.

## Canonical detailed owners

- `docs/delivery/change-impact-and-migration.md`
- `docs/architecture/contract-boundaries.md`
- `backend/docs/operations/migrations-and-data-change.md`
- `backend/docs/architecture/api-and-contracts.md`
- `frontend/docs/architecture/api-and-contracts.md`

---

# NRX-009 — Consistency and Transaction Ownership Are Explicit

## Intent

Notrelix has many operations spanning Domain, persistence, outbox, cache, realtime, and asynchronous delivery.

Correctness depends on knowing which state must change atomically and which state converges afterward.

## Rule

Every material state-changing workflow MUST have an explicit consistency owner and transaction boundary.

The design MUST distinguish:

- in-memory Domain consistency;
- local durable transaction;
- outbox enrollment;
- post-commit effects;
- asynchronous/eventual consistency;
- cross-context process coordination.

## Required consequences

### Domain

- Rejected mutation MUST leave protected in-memory state unchanged.
- Semantic no-op MUST NOT produce accidental version/audit/event changes unless explicitly contracted.
- Meaningful mutation SHOULD update version exactly according to the owning Domain contract.

### Application

- Transaction ownership MUST be defined.
- Handler behavior MUST NOT commit independently when the pipeline owns commit.
- Expected-version conflicts MUST fail before durable partial commit.
- Cross-context state changes require explicit transaction/process semantics.

### Platform

- Outbox enrollment belongs to the same durable commit as the authoritative state change.
- Post-commit work MUST NOT run before successful commit.
- Ordering state MUST NOT advance on failed handling when the ordering contract is success-based.

### Frontend

- Optimistic state is provisional.
- It MUST reconcile with authoritative operation outcome.

## Forbidden

- Mutating an aggregate then discovering a later validation failure without rollback/failure atomicity.
- `SaveChanges` in arbitrary handlers while pipeline also owns transactions.
- Publishing an integration/realtime fact before the state commit.
- Updating cache as authoritative success before transaction result is known.
- Hiding a multi-owner distributed workflow inside one repository helper.

## Typical violations

### Invalid mutation order

```text
mutate collection
→ validate invariant
→ throw
```

State was changed even though operation failed.

### Early publication

```text
save intent in memory
→ publish message
→ DB commit fails
```

Consumers observe a fact that never became durable.

## Evidence

- Domain success/reject/no-op/failure-atomicity tests;
- Application transaction tests;
- Integration tests;
- Platform delivery/outbox tests;
- frontend mutation reconciliation tests.

## Canonical detailed owners

- `docs/architecture/data-ownership-and-consistency.md`
- `backend/docs/architecture/domain-modeling.md`
- `backend/docs/architecture/application-model.md`
- `backend/docs/architecture/platform-and-messaging.md`
- `frontend/docs/architecture/state-query-mutations.md`

---

# NRX-010 — Retryable Effects Require Stable Identity and Idempotency Semantics

## Intent

Distributed and asynchronous systems retry.

Providers retry.

Users repeat actions.

Networks time out after the server already committed.

Consumers may see duplicate delivery.

Correctness MUST NOT depend on exactly-once transport behavior unless an approved subsystem truly provides it.

## Rule

Any operation where duplicate execution can produce incorrect durable/business effects MUST define stable operation/message/execution identity and idempotency/dedup semantics appropriate to the boundary.

## Required consequences

Applicable workflows MAY include:

- API commands;
- provider callbacks;
- outbox delivery;
- consumers;
- automation executions;
- scheduled jobs;
- billing/provider operations;
- integration synchronization;
- notification/provider delivery.

The owner MUST define, where relevant:

- identity key;
- consumer identity;
- request hash/semantic equivalence;
- in-progress/completed/failed state;
- retry policy;
- conflict behavior;
- dedup retention;
- ordering relationship;
- crash recovery.

Platform-level consumer dedup MUST distinguish at least message identity and consumer identity when that is the accepted consumer contract.

## Forbidden

- Retry loop around non-idempotent provider action with no operation key/reconciliation.
- Consumer dedup by event type instead of message/event instance when individual deliveries matter.
- Global dedup key shared by unrelated consumers.
- Treating HTTP timeout as proof the server did not commit.
- Retrying poison/deterministic-invalid work forever.

## Typical violations

### Duplicate external side effect

A timeout occurs after the provider created a resource.

The retry creates a second resource because no stable provider operation identity or reconciliation exists.

## Evidence

- Platform tests;
- integration production-graph tests;
- API idempotency tests;
- automation/integration tests;
- provider contract tests.

## Canonical detailed owners

- `docs/architecture/data-ownership-and-consistency.md`
- `backend/docs/architecture/platform-and-messaging.md`
- `backend/docs/architecture/application-model.md`
- relevant product/integration/billing/automation context docs.

---

# NRX-011 — Lifecycle and Destructive Data Operations Require Explicit Product Policy

## Intent

“Delete” is not one universal business operation.

Different resources require different lifecycle, retention, revocation, archive, anonymization, audit, and purge behavior.

Generic deletion infrastructure must not invent product semantics.

## Rule

A destructive or visibility-removing operation MUST use the owning product vocabulary and define its lifecycle consequences explicitly.

The design MUST distinguish where relevant:

- archive;
- disable;
- revoke;
- suspend;
- cancel;
- expire;
- remove;
- tombstone;
- anonymize;
- physical purge.

## Required consequences

Before implementing destructive change, define:

- who can perform it;
- whether it is reversible;
- user-visible result;
- reference behavior;
- retention;
- audit behavior;
- cross-context effects;
- provider effects;
- analytics/history implications;
- eventual physical deletion if any.

Soft delete MUST NOT be used by default solely because a persistence framework supports it.

Cross-context physical cascade MUST NOT be assumed.

## Forbidden

- Universal `IsDeleted` applied to all product concepts without lifecycle semantics.
- Status enum value `SoftDeleted` used as generic technical state.
- Hidden `_statusBeforeDeletion` used to reconstruct product lifecycle.
- Cascading deletion into another context because foreign keys make it easy.
- Destroying customer work immediately because subscription entitlement changes.

## Typical violations

A canceled subscription directly hard-deletes work resources.

Billing is enforcing commercial state by taking ownership of Work Management lifecycle.

This violates `NRX-011` and `NRX-001`.

## Evidence

- product context lifecycle docs;
- Domain lifecycle tests;
- migration/data-retention tests;
- security/audit review;
- provider cleanup tests where applicable.

## Canonical detailed owners

- [`PRODUCT.md`](PRODUCT.md)
- `docs/product/contexts/*.md`
- `backend/docs/architecture/domain-modeling.md`
- `backend/docs/operations/migrations-and-data-change.md`

---

# NRX-012 — Secrets and Sensitive Data Are Protected by Default

## Intent

Notrelix processes tenant, identity, integration, billing, security, and potentially sensitive work content.

Observability and debugging must not create new unauthorized disclosure channels.

## Rule

Secrets, credentials, tokens, sensitive tenant data, and security-sensitive internal facts MUST NOT enter a less-protected boundary unless that boundary explicitly requires and protects them.

## Required consequences

- Secrets MUST NOT be committed to source or documentation examples as real values.
- Domain events/messages MUST avoid raw credentials/tokens.
- Logs MUST avoid raw secrets and unnecessarily sensitive payloads.
- Frontend/public contracts MUST expose only data required by the product operation.
- Provider credentials MUST remain behind approved integration/security boundaries.
- Test fixtures MUST use non-sensitive deterministic values.
- Audit evidence SHOULD identify action/subject/resource without leaking unrelated secret content.
- Generated artifacts MUST NOT accidentally serialize secret configuration.

## Forbidden

- Real API keys in committed test/docs examples.
- Access/refresh tokens in Domain events.
- Full webhook secret printed in logs.
- Returning provider credential metadata to frontend because the backend DTO already contains it.
- Logging entire request bodies by default on protected endpoints.

## Typical violations

A failed provider request logs the complete Authorization header.

The log system now becomes a credential store.

## Evidence

- secret scanning;
- security tests;
- logging review;
- contract tests;
- provider adapter tests;
- CI vulnerability/security checks.

## Canonical detailed owners

- `docs/quality/security-quality-standard.md`
- `backend/docs/architecture/security-tenancy-authorization.md`
- `backend/docs/operations/configuration-and-runtime.md`
- `frontend/docs/architecture/api-and-contracts.md`

---

# NRX-013 — Generated Artifacts Are Producer-Owned and Drift-Checked

## Intent

Exact inventories change frequently.

Manual duplication of machine-derivable facts creates stale documentation and false confidence.

## Rule

When an exact artifact can be reliably generated from an authoritative producer, the generated artifact MUST be producer-owned and drift-checked.

Humans MAY explain the architecture around it but MUST NOT manually maintain a competing exact copy.

## Required consequences

Examples:

```text
backend project graph
→ backend.slnx + csproj producer
→ generated project map

frontend package graph
→ architecture-manifest.ts
→ generated package-boundaries.md

public API types
→ OpenAPI/codegen producer
→ generated frontend contracts

documentation index
→ canonical doc metadata
→ generated document index
```

Every generated document/artifact MUST identify:

- producer;
- regenerate command;
- do-not-edit status;
- drift check.

## Forbidden

- Hand-editing generated package boundaries.
- Manually maintaining route dump when OpenAPI can produce it.
- Static project-count claims in canonical prose when solution files are authoritative.
- Copy/pasting generated API types into handwritten frontend types.
- Marking generated evidence as architectural rationale.

## Typical violations

A new frontend package is added.

The architecture manifest is updated but a hand-maintained Markdown matrix is forgotten.

Two “truths” now exist.

The correct architecture uses one producer and generated evidence.

## Evidence

- generation commands;
- drift checks;
- CI;
- producer tests;
- generated-file headers.

## Canonical detailed owners

- `docs/governance/documentation-authority.md`
- `frontend/docs/architecture/dependency-boundaries.md`
- `frontend/docs/generated/package-boundaries.md`
- `backend/docs/generated/project-map.md`

---

# NRX-014 — Client State Cannot Become Competing Server Truth

## Intent

Frontend responsiveness depends on caching, optimistic mutation, derived state, and realtime updates.

Those mechanisms are projections of authoritative server/business state.

They must not silently become a second durable business model.

## Rule

Durable server state remains backend-authoritative.

Frontend query caches, optimistic updates, realtime reconciliation, and local state MUST preserve enough identity/scope/version information to converge safely to authoritative state.

## Required consequences

- Query keys MUST include tenant/workspace/resource scope required by the server-state contract.
- Each server-state area MUST have a clear frontend owner.
- Permanent server-state duplication in local stores MUST be avoided.
- Optimistic update MUST define admission, snapshot/rollback, authoritative result reconciliation, and conflict handling.
- Workspace/account transitions MUST prevent old-scope requests/events/cache from overwriting new scope.
- Stale responses MUST NOT overwrite newer authoritative state.
- Duplicate/out-of-order realtime events MUST be safe.
- Sequence uncertainty SHOULD trigger recovery/refetch rather than guessing.
- Realtime MAY accelerate convergence; it MUST NOT be the only persistent truth source.

## Forbidden

- Tenant-blind query keys.
- Component-local ad-hoc cache identity.
- Optimistic success that remains after server rejection.
- Global workspace realtime subscription patching current workspace cache without scope validation.
- Local Zustand/React state permanently mirroring server entities with independent mutation.
- Realtime event directly overwriting newer versioned cache state.

## Typical violations

### Workspace switch race

```text
Workspace A request starts
→ user switches to Workspace B
→ B state loads
→ late A response resolves
→ shared cache key overwrites B state
```

The frontend must scope/cancel/reject stale ownership correctly.

## Evidence

- frontend query-key tests;
- mutation rollback tests;
- workspace-transition tests;
- realtime duplicate/order tests;
- integration/host tests.

## Canonical detailed owners

- `frontend/docs/architecture/state-query-mutations.md`
- `frontend/docs/architecture/realtime.md`
- `frontend/docs/architecture/api-and-contracts.md`

---

# NRX-015 — Accessibility and Host Safety Are Release-Quality Contracts

## Intent

Notrelix is multi-host and enterprise-facing.

Accessibility and runtime safety are functional quality, not optional visual polish.

## Rule

Supported user-facing product behavior MUST satisfy the accessibility and host/runtime constraints applicable to its target surface.

## Required consequences

### Accessibility

New/changed user-facing behavior MUST account for, where applicable:

- keyboard access;
- focus order/visibility;
- semantic roles/structure;
- screen-reader naming;
- contrast;
- non-color-only state meaning;
- touch targets;
- reduced motion;
- loading/empty/error/permission/conflict feedback.

The product target is WCAG 2.2 AA unless a documented exception applies.

### Host safety

- Shared frontend code MUST respect its declared host/runtime compatibility.
- Mobile production code MUST NOT acquire DOM/web-only dependencies.
- Marketing host behavior MUST NOT become an authenticated-app state owner.
- Web-only runtime adapters MUST remain isolated from native-safe packages.

## Forbidden

- Disabling keyboard behavior because drag-and-drop works with pointer.
- Shipping a status that is communicated only by color.
- Importing web UI/runtime into mobile product package.
- Adding DOM assumptions to framework-neutral foundation.
- Treating accessibility tests as optional because visual snapshots pass.

## Typical violations

A custom drag-only Kanban movement has no keyboard alternative.

The business action exists but is inaccessible.

This is a functional release defect.

## Evidence

- frontend architecture checks;
- mobile tests;
- accessibility/component tests;
- E2E;
- design review.

## Canonical detailed owners

- [`DESIGN.md`](DESIGN.md)
- `docs/quality/accessibility-standard.md`
- `frontend/docs/architecture/dependency-boundaries.md`
- `frontend/docs/architecture/ui-and-design-system.md`

---

# NRX-016 — Required Validation Must Execute Meaningful Non-Zero Work

## Intent

A green CI job proves nothing if it silently skipped the protected behavior.

Notrelix relies on architecture, contract, security, migration, generated, and integration gates as executable architecture.

## Rule

Every required validation gate MUST execute the relevant intended work and MUST fail when the required work cannot be executed.

“Command exited 0” is not sufficient evidence by itself.

## Required consequences

- Guarded test suites MUST fail if zero relevant tests execute when the suite is required.
- Generated checks MUST fail on drift or unavailable required producer.
- Architecture checks MUST cover the intended project/package universe.
- CI filters MUST not accidentally exclude all protected tests.
- A skipped required integration graph MUST not be reported as certification.
- Documentation checks MUST fail when canonical paths/authority rules are violated.
- Developers/agents MUST report unrun required checks explicitly.

## Forbidden

- Changing a test filter so zero tests run while CI stays green.
- Catching generator errors and returning success.
- Disabling architecture rules to unblock a PR without approved exception.
- Claiming “tests pass” when only a subset unrelated to the changed contract ran.
- Using an optional local environment failure as justification to silently skip a required CI proof.

## Typical violations

```text
dotnet test --filter FullyQualifiedName~CriticalSuite
```

returns success with zero matching tests.

If that suite is required evidence, the gate is invalid unless it verifies non-zero execution.

## Evidence

- CI workflow logs;
- guarded suite scripts;
- architecture test counts;
- generation drift checks;
- docs governance checks.

## Canonical detailed owners

- `docs/quality/testing-strategy.md`
- `docs/delivery/definition-of-done.md`
- `backend/docs/architecture/testing-and-quality-gates.md`
- `frontend/docs/architecture/testing-and-quality-gates.md`

---

# NRX-017 — Architecture Exceptions Are Explicit, Owned, and Temporary or Reviewable

## Intent

Real systems sometimes need transitional or exceptional architecture.

Undocumented exceptions become accidental precedents and permanently weaken boundaries.

## Rule

Any intentional deviation from a protected architecture contract MUST be explicit.

The exception MUST define:

- violated/protected rule;
- scope;
- reason;
- owner;
- risk;
- compensating controls;
- review/expiry trigger;
- removal or normalization plan.

## Required consequences

- Transitional legacy code MUST NOT become precedent for new code.
- New code SHOULD follow canonical target architecture unless the exception explicitly includes it.
- Architecture tests MAY encode approved exceptions, but the exception identity/owner MUST remain discoverable.
- Exception removal MUST update code, tests, docs, and allow-lists.
- A permanent change to the architecture is not an “exception”; it requires changing the canonical architecture/ADR.

## Forbidden

- “Temporary” allow-list entry with no owner/review condition.
- Copying legacy pattern because it already exists.
- Suppressing architecture analyzer warning without tracking rationale.
- Creating one-off dependency edge that effectively changes package/project ownership without ADR/review.

## Typical violations

A frontend package temporarily imports an app internal during migration.

Six months later new features copy the same import because “there is precedent”.

The original exception has become de facto architecture.

## Evidence

- exception registry/policy;
- architecture allow-lists;
- ADRs;
- issue/removal plan;
- architecture tests.

## Canonical detailed owners

- `docs/governance/decision-and-exception-policy.md`
- relevant backend/frontend architecture document;
- relevant ADR registry.

---

# NRX-018 — Documentation, Decisions, Source Evidence, and Generated Evidence Remain Coherent

## Intent

Documentation is a protected part of the Notrelix architecture.

A detailed but stale architecture document is more dangerous than a concise accurate one because humans and Coding Agents will confidently implement the wrong contract.

## Rule

Every canonical topic MUST have one normative owner, and that owner MUST remain coherent with:

- accepted decisions;
- current source evidence;
- tests/gates;
- generated evidence;
- references from summaries/routers.

Documentation architecture MUST NOT accumulate parallel generations of canonical truth.

## Required consequences

### Authority

- One topic → one canonical normative owner.
- Root summaries may reference but MUST NOT independently redefine project-level details.
- Backend implementation architecture belongs to `backend/docs`.
- Frontend implementation architecture belongs to `frontend/docs`.
- Repository `docs/` owns cross-stack/product/governance, not duplicate backend/frontend handbooks.

### Decisions

- ADRs preserve why consequential decisions were made.
- Accepted ADR history MUST NOT be silently rewritten to match a later choice.
- Superseding decisions create superseding ADRs.

### Current facts

- `CONTEXT.md` contains current repository facts and transitions.
- It MUST NOT become a durable architecture rulebook.

### Historical/project artifacts

Roadmaps, audits, freeze specs, migration trackers, and temporary readiness reports MUST NOT be used as active architecture authority after their lifecycle ends.

### Generated evidence

Generated files MUST remain producer-owned.

### References

Canonical references MUST use repository-relative paths.

Absolute workstation links such as `file:///Users/...` are forbidden.

## Forbidden

- `architecture-v2`, `architecture-final-v4`, and current architecture all active together.
- Backend rules duplicated under root/system docs and backend docs.
- Frontend package matrix manually copied next to generated manifest output.
- Stale branch/SHA metadata used to imply current verification without current evidence.
- Provider-specific instruction file redefining product/architecture.
- Keeping obsolete docs “just in case” on the normal reading path when Git already preserves history.

## Typical violations

### Parallel canonical generations

```text
backend/docs/architecture/application-model.md
docs/engineering/02-backend/application.md
backend/RULE.md
```

all independently claim current Application architecture.

A Coding Agent cannot know which one wins.

That documentation system violates `NRX-018`.

## Evidence

At minimum:

```bash
make docs-check
```

Target documentation governance SHOULD also include:

- link validation;
- authority validation;
- metadata validation;
- rule/ADR ID uniqueness;
- source inventory alignment;
- generated drift checks.

## Canonical detailed owners

- `docs/governance/documentation-authority.md`
- `docs/governance/documentation-lifecycle.md`
- `docs/governance/topic-authority-map.md`
- `docs/governance/documentation-quality-gates.md`

---

# 4. Cross-rule interactions

Repository invariants are not independent checkboxes.

Important combinations include the following.

## 4.1 Product semantics + architecture boundary

```text
NRX-001
+
NRX-002
```

Product owner decides meaning.

Architecture decides where that meaning may be implemented.

A technically “clean” dependency structure that implements the wrong product owner is still wrong.

---

## 4.2 Tenant isolation + authorization

```text
NRX-003
+
NRX-004
```

Tenant scope and authorization are related but not identical.

Example:

- Workspace membership may prove tenant participation.
- Governance still decides whether a member can read a private Board.
- RLS still protects persistence isolation.

No one mechanism replaces all others.

---

## 4.3 Consistency + retry

```text
NRX-009
+
NRX-010
```

Transaction boundaries protect one attempt.

Idempotency protects repeated attempts.

A durable transaction does not by itself prevent duplicate provider effects after retry.

---

## 4.4 Contract change + migration

```text
NRX-007
+
NRX-008
```

The existence of a contract implies an evolution responsibility.

If consumers may span versions, compatibility must be designed rather than assumed.

---

## 4.5 Server authority + realtime

```text
NRX-014
+
NRX-009
```

Realtime can communicate completed facts faster.

It does not create durable truth.

Frontend reconciliation must respect authoritative version/state.

---

## 4.6 Architecture exception + documentation coherence

```text
NRX-017
+
NRX-018
```

An exception is safe only when its existence is discoverable.

Undocumented architecture debt is not an approved exception.

---

# 5. Change classification

Before implementation, classify a material change.

## 5.1 Local implementation/refactor

Examples:

- internal rename;
- private helper extraction;
- performance optimization preserving contracts.

Requirements:

- preserve all applicable `NRX-*` rules;
- focused tests;
- no ADR unless architecture meaning changes.

---

## 5.2 Product semantic change

Examples:

- changing BoardGroup meaning;
- changing lifecycle;
- introducing a bounded context;
- redefining entitlement behavior.

Requirements:

- owning product doc update;
- backend/frontend impact;
- public/persisted compatibility review;
- migration when applicable;
- ADR if architecture consequence is significant.

---

## 5.3 Architecture change

Examples:

- new backend project dependency;
- new frontend package-family edge;
- changing transaction owner;
- changing Platform/Infrastructure responsibility;
- replacing query-state ownership model.

Requirements:

- architecture owner update;
- affected tests/gates;
- ADR for consequential choice;
- migration/exception handling;
- reference update.

---

## 5.4 Public contract change

Examples:

- REST;
- realtime;
- event/message;
- package export;
- generated client.

Requirements:

- `NRX-007`;
- `NRX-008` if breaking;
- producer/consumer inventory;
- compatibility/version/migration;
- generated artifact update.

---

## 5.5 Data/schema change

Requirements:

- authoritative owner;
- migration;
- rollout ordering;
- RLS/index implications;
- backward compatibility;
- data safety;
- rollback/roll-forward strategy.

---

## 5.6 Security/tenant change

Requirements:

- security owner review;
- threat/tenant/authorization analysis;
- protected reads and writes;
- background paths;
- cache/realtime/search effects;
- security tests.

---

# 6. Architecture exception protocol

An exception request MUST state:

```text
Rule:
Canonical owner:
Affected scope:
Reason:
Why compliant design is currently not viable:
Risk:
Compensating controls:
Owner:
Review/expiry trigger:
Removal/normalization plan:
Tests/gates:
```

The exception MUST NOT be hidden only in code comments.

If the exception changes architecture permanently, update canonical architecture and ADR instead.

---

# 7. Rule-change protocol

Changing a repository invariant is a high-impact architecture/product action.

A `NRX-*` change MUST include:

1. reason for change;
2. affected product/system contracts;
3. affected backend/frontend owners;
4. compatibility/migration impact;
5. security/tenant impact;
6. test/gate updates;
7. documentation/reference updates;
8. ADR when consequential;
9. removal/update of superseded implementation assumptions.

Do not weaken a repository rule because one implementation is inconvenient.

---

# 8. Evidence standard

No single evidence class is sufficient for every rule.

Use the strongest combination appropriate to the change.

## Product evidence

- `PRODUCT.md`;
- product context docs;
- user-visible behavior tests.

## Architecture evidence

- backend architecture docs;
- frontend architecture docs;
- dependency/project/package manifests;
- architecture tests.

## Behavioral evidence

- Domain/Application/frontend tests;
- integration tests;
- E2E.

## Contract evidence

- OpenAPI;
- generated clients/types;
- contract tests;
- package exports;
- message/event schemas.

## Data evidence

- migrations;
- mappings;
- RLS/integration tests;
- index/query tests.

## Reliability evidence

- Platform tests;
- retry/idempotency tests;
- production-graph integration tests.

## Documentation evidence

- relative link checks;
- authority checks;
- generated drift;
- rule/ADR uniqueness;
- source inventory checks.

A document claiming compliance MUST NOT substitute for executable evidence when executable evidence is practical.

---

# 9. Coding Agent requirements

Coding Agents MUST treat this file as a repository constraint.

Before implementing a material change they MUST identify:

- applicable `NRX-*` rules;
- owning product capability/context;
- backend/frontend architecture owner;
- tenant/security scope;
- authoritative state owner;
- contracts changed;
- migration implications;
- required evidence.

Agents MUST stop instead of inventing a decision when:

- product ownership is ambiguous;
- security/tenant behavior is unresolved;
- accepted ADR conflicts with current source without a superseding decision;
- multiple active public contract meanings exist;
- generated producer is unknown;
- destructive migration behavior has not been approved;
- implementing the task requires weakening a repository invariant without an explicit architecture/product change request.

See [`AGENTS.md`](AGENTS.md) for the execution protocol.

---

# 10. Repository constitution definition of done

This constitution is considered correctly applied when:

- product semantics have one owner;
- project/package boundaries are enforced;
- tenant scope cannot leak across relevant boundaries;
- protected operations are authorized server-side;
- pure layers remain deterministic;
- shared code has legitimate stable ownership;
- public/cross-boundary contracts are explicit;
- breaking changes are migrated;
- transaction/consistency ownership is explicit;
- retryable effects have stable identity/idempotency;
- lifecycle/destructive semantics are explicit;
- secrets/sensitive data stay protected;
- generated evidence is producer-owned;
- frontend state converges to server authority;
- accessibility/host safety are tested as product quality;
- required gates execute meaningful work;
- exceptions are explicit and reviewable;
- canonical docs/source/ADRs/generated evidence do not form competing truths.

The objective is not architectural ceremony.

The objective is to let multiple teams and Coding Agents evolve Notrelix in parallel without silently changing the product, security model, consistency guarantees, or foundational architecture.
