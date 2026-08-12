---
document_id: SYS-OVERVIEW
document_type: architecture
status: active
owner: system-architecture
applies_to:
  - repository
  - backend
  - frontend
  - public-contracts
  - runtime
evidence:
  - PRODUCT.md
  - RULE.md
  - backend/backend.slnx
  - backend/src/
  - backend/tests/
  - frontend/pnpm-workspace.yaml
  - frontend/tooling/dependency-rules/src/architecture-manifest.ts
  - frontend/apps/
  - frontend/packages/
  - artifacts/contracts/
  - docker-compose.dev.yml
  - docker-compose.staging.yml
  - docker-compose.prod.yml
review_on:
  - bounded-context-owner-change
  - backend-production-project-change
  - frontend-host-model-change
  - cross-stack-contract-change
  - tenancy-model-change
  - deployment-runtime-change
  - service-extraction-change
  - authoritative-state-model-change
---

# System Overview

> **Notrelix is an enterprise work-management workspace operating system implemented as a modular-monolith backend, a multi-host frontend, and explicit cross-boundary contracts.**
>
> The system is optimized for:
>
> **strong semantic ownership now + clean extraction seams later, without paying distributed-system cost before an operational boundary justifies it.**

This document is the canonical owner for the **cross-stack architectural shape** of Notrelix.

It does not own:

- detailed product-context semantics;
- Domain modeling mechanics;
- Application pipeline order;
- persistence mappings;
- messaging algorithms;
- frontend package allow-lists;
- query-key implementation;
- literal design tokens;
- deployment command inventory.

Those belong to their project/product/runtime owners.

---

# 1. System mission

Notrelix provides a coherent product where teams can:

- organize Accounts and Workspaces;
- manage structured work;
- create and connect Documents;
- collaborate around resources;
- automate business workflows;
- integrate external providers;
- apply Governance and access policy;
- manage commercial entitlement through Billing;
- derive Analytics/Reporting without creating competing business truth.

The system architecture exists to preserve that product coherence while allowing many teams to develop capabilities in parallel.

---

# 2. Architectural thesis

The system is built around six architectural commitments.

```text
1. Business facts have explicit semantic owners.
2. The backend remains server-authoritative for protected durable business state.
3. Cross-boundary communication uses explicit contracts.
4. Strong consistency is local and intentionally bounded.
5. Async/realtime/client caches are projections or delivery mechanisms, not competing truth.
6. Deployment boundaries may evolve without redefining product semantics.
```

These commitments are cross-stack.

Detailed implementation belongs to the owning backend/frontend/product documents.

---

# 3. Current system shape

Conceptually:

```text
Users / External Clients
        │
        ├───────────────────────────────┐
        │                               │
        ▼                               ▼
Authenticated Product Clients      Public / Marketing
Web · Mobile                      Marketing host
        │
        │
        ▼
Public Contract Boundary
REST / OpenAPI / Realtime / Integration contracts
        │
        ▼
Backend API Host
        │
        ▼
Application Use Cases
authorization · orchestration · transaction ownership
        │
        ├───────────────┐
        ▼               ▼
Domain             Application-facing ports
business           external facts / capabilities
invariants              │
                         ▼
              Infrastructure / Platform
              persistence · providers
              cache · search · messaging
              delivery · idempotency · runtime
                         │
                         ▼
PostgreSQL · Redis · Messaging · Storage · External Providers
```

The diagram is conceptual.

It is not a project-reference graph.

Exact backend project references are backend-owned.

Exact frontend package edges are frontend-manifest-owned.

---

# 4. Current implementation planes

Notrelix currently has four major implementation planes.

## 4.1 Product semantics

Owned by:

```text
PRODUCT.md
docs/product/
```

Defines what the product means independently of technology.

---

## 4.2 Backend

Current backend production-project inventory:

```text
Notrelix.Domain
Notrelix.Application
Notrelix.Infrastructure
Notrelix.Platform
Notrelix.API
```

Exact project inventory is owned by:

```text
backend/backend.slnx
```

The backend is one modular-monolith deployment model today.

The five projects are technical responsibility boundaries, not five services.

---

## 4.3 Frontend

Current host model:

```text
apps/web
apps/mobile
apps/marketing
```

Current workspace families:

```text
packages/foundation/*
packages/runtimes/*
packages/ui/*
packages/product/*/*
packages/features/*
tooling/*
```

Exact package dependency authority is:

```text
frontend/tooling/dependency-rules/src/architecture-manifest.ts
```

Frontend package topology is not the product bounded-context map.

---

## 4.4 Runtime / external systems

Runtime infrastructure includes categories such as:

```text
relational persistence
cache
messaging transport
storage/search adapters
gateway/proxy
external provider APIs
email/provider integrations
observability
```

Concrete runtime topology is owned by infrastructure/operations source and documentation.

---

# 5. SYS-001 — Business semantics precede deployment topology

## Rule

A business capability MUST be modeled according to semantic ownership before deciding:

- project;
- package;
- database;
- queue;
- service;
- deployment unit.

Implementation topology must support product ownership.

It must not invent product ownership.

## Consequence

A bounded context may currently span several technical projects/packages.

That is expected.

## Forbidden inference

```text
folder
→ context

table
→ context

frontend feature package
→ context

queue
→ service owner
```

---

# 6. SYS-002 — Modular monolith is the backend deployment default

## Rule

The backend remains a modular monolith until an independently valuable operational boundary justifies extraction.

A bounded context is an extraction seam.

It is not a command to deploy a service now.

## Rationale

Premature service extraction introduces:

- network failure;
- distributed transactions;
- delivery semantics;
- independent deployment coordination;
- more observability;
- more security boundaries;
- more data migration complexity.

Those costs are justified only when the extracted capability gains meaningful independent value.

---

# 7. SYS-003 — Extraction readiness is designed before extraction

A bounded context should already have:

- explicit semantic ownership;
- stable public/application contracts;
- owned state;
- controlled cross-context references;
- explicit integration events where needed;
- explicit persistence access patterns;
- tests around its invariants and contracts.

Then service extraction can primarily change:

```text
deployment
transport
data placement
operational ownership
```

instead of requiring a semantic rewrite.

---

# 8. SYS-004 — Product capabilities are vertically owned

A business capability may legitimately require changes across:

```text
Domain
Application
Infrastructure
Platform
API
frontend product/feature packages
contracts
tests
```

Technical layers are responsibility boundaries.

They are not separate product owners.

A team implementing one capability must follow the same semantic owner through all affected layers.

---

# 9. SYS-005 — Backend is authoritative for protected durable business state

## Rule

Protected durable business truth is server-authoritative.

Examples include:

- resource state;
- lifecycle;
- membership;
- permissions;
- billing entitlement;
- authoritative work values;
- document state;
- automation definition/execution facts;
- integration connection state.

Frontend state may be:

- cached;
- optimistic;
- derived;
- offline-provisional;
- realtime-updated.

It must eventually reconcile to authoritative server state.

---

# 10. SYS-006 — Authentication, authorization, tenant scope are distinct concerns

The system distinguishes:

```text
Authentication
    Who/what principal is this?

Tenant/resource scope
    Which Account/Workspace/resource boundary is being addressed?

Authorization
    May this principal perform this operation on this resource?
```

They interact.

They are not interchangeable.

Frontend route visibility is not authoritative authorization.

A resource ID alone is not sufficient proof of tenant scope.

---

# 11. SYS-007 — Tenant scope travels across every relevant boundary

Scope must be preserved through:

```text
HTTP
Application use case
database/RLS context
cache
search/index
events/messages
background work
realtime
analytics
provider synchronization
```

The exact representation may vary.

The semantic scope may not disappear.

---

# 12. Trust boundaries

The system contains several trust boundaries.

## 12.1 User/client boundary

Web/mobile clients are untrusted with respect to server authorization and protected business invariants.

Client input must be validated and authorized server-side.

---

## 12.2 Public/marketing boundary

Public marketing UI is not an authenticated product authority.

Public endpoints/share surfaces require explicit security semantics.

---

## 12.3 API boundary

API translates external transport contracts into Application operations.

API authentication is part of trust establishment.

Business authorization remains Application-authoritative.

---

## 12.4 Persistence boundary

Database state is durable evidence.

Database shape does not define business ownership by itself.

RLS provides defense in depth for tenant isolation.

---

## 12.5 Messaging boundary

Message delivery is asynchronous and may duplicate, retry, reorder, or fail.

Consumers must not assume exactly-once transport.

---

## 12.6 Provider boundary

External provider APIs are outside Notrelix transactional authority.

A successful/failed HTTP interaction may not equal the business outcome unless provider semantics prove it.

Provider calls require explicit identity/retry/uncertainty handling.

---

# 13. SYS-008 — Cross-stack communication uses explicit contracts

Backend implementation types and frontend implementation types are not shared by source-level coupling.

Stable boundaries are explicit contract artifacts and compatibility rules.

Contract categories include:

```text
REST/OpenAPI
realtime payloads
integration/public events
provider/webhook contracts
generated client/types
package public exports where cross-package
```

Detailed contract policy:

```text
docs/architecture/contract-boundaries.md
```

---

# 14. Contract ownership model

Every cross-boundary contract needs:

```text
producer
consumer(s)
semantic owner
identity
scope
compatibility rule
version/deprecation rule when relevant
migration owner
evidence
```

A payload shape without ownership is not a complete contract.

---

# 15. SYS-009 — Internal refactors do not automatically break contracts

Renaming:

- CLR type;
- table;
- frontend package;
- internal class;
- internal folder

is not itself a reason to break a public contract.

Contract identity is logical and consumer-facing.

---

# 16. Authoritative-state model

The system separates:

```text
Authoritative state
Derived state
Cached state
Transport state
Presentation state
```

## Authoritative state

Owned by one product context.

## Derived state

Examples:

- search index;
- analytics projection;
- dashboard aggregate;
- read model.

## Cached state

Performance representation with explicit invalidation/freshness.

## Transport state

Delivery envelope, retry state, outbox state.

## Presentation state

UI loading, selection, route, local interaction state.

These must not be confused.

---

# 17. SYS-010 — One authoritative owner per business fact

A business fact must have one semantic owner.

Other components may:

- read;
- project;
- cache;
- aggregate;
- react.

They do not become co-owners.

Example:

```text
Workspace membership
→ Workspaces

permission/policy semantics
→ Governance

BoardItem field value
→ Work Management

subscription/entitlement
→ Billing
```

---

# 18. SYS-011 — Foreign keys do not decide business ownership

A resource containing:

```text
workspace_id
account_id
user_id
board_id
```

does not automatically belong to the referenced context.

Ownership follows:

- vocabulary;
- lifecycle;
- invariants;
- mutation authority.

Foreign keys are technical relationships.

---

# 19. SYS-012 — Cross-context references preserve ownership

Contexts refer to external owners through:

- stable IDs;
- immutable facts;
- explicit read contracts;
- explicit events/contracts.

They do not navigate and mutate another context's internal aggregate graph.

---

# 20. Cross-context read model

A context may need an external fact.

Allowed patterns include:

```text
Application query/read port
explicit projection
cache with freshness semantics
contract-provided immutable fact
```

The consumer must understand:

- owner;
- freshness;
- failure;
- authorization/scope.

---

# 21. Cross-context write model

A context must not perform another context's mutation by directly reaching into its persistence/aggregate internals.

Use one of:

```text
explicit Application orchestration
durable integration event
explicit consumer contract
process manager / saga
```

depending on consistency needs.

---

# 22. SYS-013 — Strong consistency is explicit and narrow

Sharing one database does not mean all contexts should participate in one broad transaction.

Strong consistency is justified when the business invariant truly requires atomicity.

Otherwise use explicit asynchronous consistency.

Every cross-context strongly consistent workflow must identify:

- why eventual consistency is unacceptable;
- transaction owner;
- rollback semantics;
- failure surface.

---

# 23. Local consistency

Within an aggregate/local owned transaction:

- invariants are evaluated before state becomes committed;
- rejected operations do not partially mutate authoritative state;
- transaction ownership is explicit.

Detailed Domain/Application semantics are backend-owned.

---

# 24. Eventual consistency

Eventual consistency is appropriate when:

- source context can commit independently;
- consumer reaction can be retried;
- temporary lag has explicit product semantics;
- consumer owns its own state.

Typical mechanism:

```text
source commit
+
outbox fact
→ delivery
→ idempotent consumer
→ consumer state
```

---

# 25. SYS-014 — Async delivery assumes retries and duplicates

Transport reliability must assume:

```text
duplicate delivery
retry
partial failure
consumer restart
poison message
ordering uncertainty where not explicitly guaranteed
```

Correctness may not depend on wishful exactly-once delivery.

Exact mechanisms are Platform-owned.

---

# 26. Stable identity

Retryable operations require stable identity appropriate to the boundary.

Examples:

```text
request operation identity
message identity
consumer identity
provider operation identity
automation execution identity
```

Identity allows idempotency/conflict handling.

---

# 27. Ordering

Ordering is a contract only where explicitly defined.

Consumers must not infer a global total order merely because events have timestamps.

Ordering semantics should identify:

- stream/resource scope;
- sequence identity;
- gap handling;
- failure behavior.

---

# 28. Realtime architecture

Realtime is a delivery/projection mechanism.

It is not persistent product truth.

Conceptually:

```text
authoritative server commit
→ approved realtime fact
→ client subscription
→ dedup/order/gap handling
→ cache reconciliation
```

If realtime certainty is lost, client recovers from authoritative query/API state.

---

# 29. SYS-015 — Client realtime and cache must converge

Client state must safely handle:

- duplicate event;
- out-of-order event;
- missed event/gap;
- reconnect;
- workspace switch;
- stale HTTP response;
- optimistic mutation race.

Detailed implementation is frontend-owned.

---

# 30. Frontend architecture relationship

System architecture assumes:

```text
apps = host composition roots
foundation = reusable neutral mechanisms
runtimes = host adapters
ui = design-system implementation
product packages = reusable capability implementation
features = application/cross-product feature packages
tooling = executable developer/architecture infrastructure
```

Exact allowed imports come from the frontend architecture manifest.

The system overview does not maintain a duplicate package matrix.

---

# 31. Backend architecture relationship

System architecture assumes:

```text
Domain
    business invariants/state transitions

Application
    use cases/authz/transactions/external facts

Infrastructure
    persistence/cache/providers/search/storage/RLS

Platform
    messaging/delivery/idempotency/runtime mechanisms

API
    HTTP/public composition boundary
```

Exact reference direction is backend-owned and architecture-tested.

---

# 32. SYS-016 — Technical layers do not become product ownership silos

Do not ask:

```text
Which layer owns Board?
```

Board is Work Management product semantics.

Layers answer:

```text
Which technical responsibility handles which part of Board behavior?
```

This distinction prevents layer-centric domain modeling.

---

# 33. Storage architecture

The system currently uses relational persistence as the primary durable business-state model.

Other storage/runtime mechanisms may include:

- cache;
- messaging;
- provider state;
- search/indexes;
- object/file storage where implemented.

Storage choice does not redefine product ownership.

---

# 34. SYS-017 — Cache is derived state

Cache MUST NOT become an independently writable business truth.

Every cached fact needs:

- authoritative source;
- scope;
- freshness/invalidation semantics;
- safe miss/failure behavior.

Permission-sensitive caches require security-aware scoping/versioning.

---

# 35. Search/index architecture

Search is a supporting capability unless product architecture explicitly promotes it to an independent business bounded context.

Search indexes:

- project source-owned business facts;
- must preserve tenant/security scope;
- may lag according to explicit freshness semantics.

Search ranking/index implementation does not own source business state.

---

# 36. Analytics architecture

Analytics/Reporting is a business context for metric/report semantics.

Its projections may consume facts from many contexts.

Derived analytical state MUST NOT silently become the mutation authority for source business state.

---

# 37. External integration architecture

Integrations separates product/provider semantics from provider SDK mechanics.

Conceptually:

```text
product integration state
→ Application contract
→ provider adapter
→ external system
```

Provider responses are translated through an anti-corruption boundary.

External provider model should not leak into business aggregates as the default language.

---

# 38. Automation architecture

Automation consumes approved product facts/triggers and performs approved actions through explicit use-case/integration contracts.

Automation does not bypass:

- authorization;
- tenant scope;
- context ownership;
- idempotency;
- provider safety.

Automation engine/runtime mechanics do not own the business state they modify.

---

# 39. Governance architecture

Governance owns reusable access/policy semantics.

Protected product contexts remain owners of their business state.

Conceptually:

```text
Product operation
→ resource/action identity
→ Application authorization
→ Governance/membership/entitlement facts
→ allow/deny
→ business mutation/query
```

Do not scatter role-string authorization independently across handlers/UI.

---

# 40. Billing architecture

Billing owns commercial facts such as plan/subscription/entitlement/usage semantics.

A product context may consume entitlement facts.

It does not become Billing-owned merely because the entitlement controls access.

Provider billing implementation remains an adapter concern.

---

# 41. Identity, Account, Workspace separation

The system intentionally distinguishes:

```text
Identity
    authenticatable principal/person/security identity

Account
    administrative/commercial ownership boundary

Workspace
    collaboration tenant/work scope
```

Do not use one concept as a generic substitute for another.

A user may interact with multiple Workspaces.

Workspace scope is not identity.

Account scope is not automatically Workspace scope.

---

# 42. Failure-domain model

Cross-stack design must identify relevant failure classes.

At minimum:

```text
1. authentication/authorization/tenant-scope failure
2. validation/business-rule rejection
3. optimistic-concurrency conflict
4. persistence/transaction failure
5. cache/search projection staleness
6. async delivery retry/duplicate/order failure
7. provider uncertainty/failure
8. public-contract/version mismatch
9. realtime connection/gap/stale-client failure
10. deployment/configuration/operational failure
```

A feature does not need all ten.

It must explicitly reason about the classes it can encounter.

---

# 43. SYS-018 — Failure semantics are part of architecture

Do not model only the successful path.

For each cross-boundary flow determine:

- authoritative commit point;
- what may already have happened on failure;
- retry safety;
- rollback/compensation;
- client-visible state;
- operational evidence.

---

# 44. Commit boundary

A use case should have a clear durable commit boundary.

Effects that require the business commit to succeed must not become externally visible prematurely.

Outbox/post-commit mechanisms exist to make this ordering explicit.

Detailed sequencing belongs to backend Application/Platform docs.

---

# 45. Provider uncertainty

A network timeout does not always mean:

```text
provider operation failed
```

It may mean:

```text
outcome unknown
```

Retry behavior must use provider/business operation identity when duplicate side effects are possible.

Client UX must not falsely display definitive failure when outcome is unknown.

---

# 46. Public API architecture

REST/API is a public boundary, not business ownership.

Endpoints:

- authenticate;
- translate contracts;
- invoke Application;
- map results/errors;
- expose versioned contracts.

They do not duplicate Domain/Application business policy.

---

# 47. Contract compatibility

Contract changes are classified by consumer impact.

Breaking public/persisted assumptions require migration.

Additive shape changes are not automatically harmless if semantics change.

Detailed rules:

```text
contract-boundaries.md
delivery/change-impact-and-migration.md
```

---

# 48. Deployment topology

Current system strategy:

```text
backend modular monolith deployment
+
independent frontend hosts/builds
+
shared external infrastructure/services as configured
```

Deployment source of truth is runtime/deployment configuration.

This document defines the architecture position, not exact container counts.

---

# 49. Web host

The web host is the primary authenticated desktop/browser product surface.

It composes approved product/feature/runtime/UI packages.

The web host is a composition root.

It does not become owner of reusable product semantics.

---

# 50. Mobile host

The mobile host is a native-safe product surface.

It may expose a subset/different interaction model while preserving product semantics.

Native safety is architecture.

DOM/web-only dependencies must not leak into mobile production paths.

---

# 51. Marketing host

Marketing is a public acquisition/brand surface.

It may share:

- design tokens;
- reusable web primitives;
- brand/product truth.

It does not own authenticated product state or business authorization.

---

# 52. Runtime dependencies

Runtime dependencies should be treated by role.

Examples:

```text
PostgreSQL
    durable relational state

Redis
    cache/ephemeral coordination where used

RabbitMQ or other transport
    asynchronous transport where enabled

Nginx/gateway
    ingress/proxy composition where deployed

External providers
    integration boundary
```

Exact configured versions/ports belong to runtime manifests and current context.

---

# 53. Observability boundary

Cross-stack operations require enough evidence to correlate:

- request;
- user/principal;
- tenant/workspace where safe;
- operation;
- message;
- provider interaction;
- failure.

Sensitive information must not be exposed merely for observability convenience.

Detailed policy belongs to `docs/operations/observability.md`.

---

# 54. Security architecture relationship

System security is layered:

```text
identity/authentication
→ tenant/resource resolution
→ Application authorization
→ Domain/business invariant
→ persistence/RLS defense
→ cache/search/realtime scope
→ audit/operational evidence
```

No single layer is assumed sufficient for every security property.

---

# 55. SYS-019 — Defense in depth does not duplicate ownership

RLS can protect tenant rows.

It does not replace business authorization.

Frontend permission guards improve UX.

They do not replace server authorization.

Domain invariants protect business state.

They do not authenticate the caller.

Each layer protects a distinct property.

---

# 56. Data classification / sensitive data

Sensitive data treatment is cross-stack.

Do not place secrets or sensitive payloads into:

- events;
- logs;
- generated client artifacts;
- frontend bundles;
- test fixtures

without explicit policy.

Detailed security-quality rules belong to quality/security docs.

---

# 57. Environment separation

Development, staging, and production are distinct operating environments.

Configuration must be environment-specific without changing product semantics.

Secrets remain externalized.

Exact environment model belongs to:

```text
docs/infrastructure/environment-model.md
```

---

# 58. System non-goals

The current architecture does NOT aim to:

- create one service per bounded context immediately;
- create one backend project per bounded context;
- create one frontend package per business context by symmetry;
- use distributed transactions across contexts;
- share mutable aggregate object graphs between contexts;
- make client cache authoritative;
- treat realtime as persistent truth;
- put every reusable concept into Common/Shared;
- hard-code current deployment topology as permanent product semantics.

---

# 59. Extraction strategy

Detailed extraction policy belongs to:

```text
docs/architecture/capability-extraction-strategy.md
```

At system overview level, extraction requires evidence that the capability benefits from independent:

- scaling;
- deployment cadence;
- reliability isolation;
- security boundary;
- data sovereignty;
- team ownership;
- technology/runtime choice.

“Bounded context exists” is necessary semantic structure, not sufficient operational justification.

---

# 60. Shared database does not erase boundaries

Current modular-monolith contexts may share one physical relational database.

This does not authorize:

- direct cross-context persistence mutation;
- broad joins becoming semantic contracts;
- shared entity graphs;
- arbitrary transaction coupling.

Logical ownership exists before physical database separation.

---

# 61. Future database separation

If a context is extracted later, data separation should follow already-defined ownership.

The migration may require:

- replication/backfill;
- contract switch;
- event/outbox adaptation;
- compatibility window.

If ownership is ambiguous before extraction, extraction is premature.

---

# 62. Shared kernel/foundation

Shared concepts are admitted only when:

- meaning is genuinely identical;
- lifecycle is compatible;
- dependency direction remains safe;
- consumers are stable.

Name similarity is insufficient.

Examples of dangerous false sharing:

```text
Status
Resource
Entity
Permission
User
```

when contexts attach different semantics.

---

# 63. SYS-020 — Technical sharing may not erase semantic ownership

A reusable primitive can be shared.

The product fact using the primitive remains context-owned.

Example:

```text
typed identifier primitive
may be shared

specific BoardId semantics
remain Work Management
```

---

# 64. Change classification

System architecture changes include changes to:

- bounded-context ownership;
- backend deployment/project architecture;
- frontend host/package architecture;
- cross-stack contracts;
- consistency model;
- tenant/security architecture;
- extraction strategy;
- authoritative-state model.

Use delivery change classification and ADR policy.

---

# 65. Required impact analysis

A consequential system change should identify impact on:

```text
product semantics
context ownership
backend
frontend
public contracts
data/migration
security/tenant
async/realtime
runtime/operations
tests/gates
documentation
```

“No code change in one layer” does not mean no impact.

---

# 66. Evidence

System architecture must remain grounded in executable/current evidence.

Primary evidence classes:

## Backend

```text
backend/backend.slnx
backend/**/*.csproj
backend/src/
backend/tests/
```

## Frontend

```text
frontend/pnpm-workspace.yaml
frontend/tooling/dependency-rules/src/architecture-manifest.ts
frontend/apps/
frontend/packages/
frontend tests
```

## Contracts

```text
artifacts/contracts/
OpenAPI/codegen producers
realtime/message contract source
```

## Runtime

```text
docker-compose*.yml
Makefile
infra/
CI
```

---

# 67. Evidence is not precedent

A source pattern may be:

- approved;
- legacy;
- transitional;
- debt.

If source contradicts this architecture, classify through AGENTS/CONTEXT drift handling.

Do not automatically rewrite system architecture to match accidental implementation.

---

# 68. System architecture tests

Executable protection should exist in the owning project/tooling.

Examples:

```text
backend Architecture.Tests
frontend architecture manifest/checks
contract drift checks
integration tests
docs authority/source-alignment checks
```

This system overview does not reimplement those gates.

---

# 69. Related canonical owners

Product:

```text
PRODUCT.md
docs/product/
```

Bounded contexts:

```text
docs/architecture/bounded-context-map.md
```

Contracts:

```text
docs/architecture/contract-boundaries.md
```

Consistency:

```text
docs/architecture/data-ownership-and-consistency.md
```

Events/realtime:

```text
docs/architecture/events-realtime-and-delivery-boundary.md
```

Extraction:

```text
docs/architecture/capability-extraction-strategy.md
```

Backend:

```text
backend/docs/architecture/
```

Frontend:

```text
frontend/docs/architecture/
```

---

# 70. Architecture-review questions

Before changing a cross-stack capability, answer:

```text
What product context owns the fact?
What tenant/account/workspace scope applies?
What state is authoritative?
Where is the durable commit point?
What cross-boundary contracts exist?
What authorization is required?
What consistency level is required?
What retries/duplicates can occur?
What client projection/realtime behavior is required?
What provider uncertainty exists?
What migration/compatibility is required?
What executable evidence protects the result?
```

If those answers are missing, architecture is incomplete.

---

# 71. System change stop conditions

Stop rather than invent when:

- two contexts appear to own the same fact;
- tenant scope is ambiguous;
- protected operation has no authorization owner;
- cross-context atomicity is assumed but not justified;
- provider side-effect identity is undefined;
- public contract has multiple competing meanings;
- client state owner is unclear;
- source and accepted decision conflict without supersession;
- service extraction is proposed without data/contract ownership.

Use decision/exception governance.

---

# 72. Final system rule

Notrelix should be able to evolve:

```text
module
→ independently owned capability
→ extracted service if justified
```

without changing what the business concept means.

The system succeeds when:

```text
product ownership remains stable
technical responsibilities remain explicit
contracts remain intentional
consistency remains bounded
tenant/security scope remains preserved
clients converge to server truth
operations can observe failures
deployment can evolve without semantic rewrite
```

That—not the number of projects, packages, services, or Markdown files—is the architecture target.
