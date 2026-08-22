---
document_id: WRK-TEAM-PLATFORM-FOUNDATION
document_type: workstream-team-spec
status: active
owner: platform-foundation-team
applies_to:
  - backend-platform
  - backend-cross-cutting-foundation
  - frontend-foundation
  - frontend-runtime
  - frontend-ui-foundation
  - architecture-tooling
  - generated-contracts
  - ci-quality-gates
evidence:
  - docs/architecture/system-overview.md
  - docs/architecture/contract-boundaries.md
  - docs/architecture/data-ownership-and-consistency.md
  - docs/architecture/events-realtime-and-delivery-boundary.md
  - docs/delivery/team-ownership.md
  - docs/workstreams/capability-map.md
  - docs/workstreams/cross-team-dependencies.md
  - backend/docs/architecture/application-model.md
  - backend/docs/architecture/infrastructure-and-data.md
  - backend/docs/architecture/platform-and-messaging.md
  - backend/docs/architecture/security-tenancy-authorization.md
  - backend/docs/architecture/testing-and-quality-gates.md
  - backend/docs/generated/project-map.md
  - frontend/docs/architecture/dependency-boundaries.md
  - frontend/docs/architecture/api-and-contracts.md
  - frontend/docs/architecture/state-query-mutations.md
  - frontend/docs/architecture/realtime.md
  - frontend/docs/architecture/ui-and-design-system.md
  - frontend/docs/architecture/testing-and-quality-gates.md
  - frontend/docs/generated/package-boundaries.md
review_on:
  - shared-runtime-contract-change
  - authorization-enforcement-change
  - tenancy-context-change
  - messaging-delivery-change
  - idempotency-change
  - realtime-foundation-change
  - query-foundation-change
  - ui-foundation-change
  - architecture-gate-change
---

# Platform & Foundation Workstream

## 1. Purpose

Platform/Foundation owns reusable technical mechanisms required by every business team. It has no business bounded context.

Its goal is to make feature delivery secure, tenant-safe, observable, contract-driven, idempotent where required, recoverable under failure, and architecture-compliant without forcing feature teams to duplicate infrastructure.

This document is execution guidance. Product semantics remain in `docs/product/*`; structural rules remain in architecture docs and ADRs.

## 2. Core ownership model

```text
Business team
→ owns business meaning

Platform/Foundation
→ owns reusable mechanism

Architecture authority
→ owns structural constraints
```

Examples:

```text
Identity
→ owns session semantics

Platform/API/frontend foundation
→ owns generic cookie/header/CSRF transport
```

```text
Governance
→ owns permission semantics

Platform/Application pipeline
→ owns enforcement mechanism
```

```text
WorkManagement
→ owns BoardItemChanged meaning

Platform
→ owns outbox/delivery/retry
```

```text
Documents
→ owns required post-recovery document state

Platform
→ owns reconnect/gap recovery mechanism
```

If a shared abstraction needs business-context-specific branching, stop and review ownership.

## 3. Explicit non-scope

Platform/Foundation MUST NOT become the semantic owner of:

- Accounts;
- Identity;
- Workspaces;
- Governance;
- WorkManagement;
- Documents;
- Collaboration;
- Automation;
- Integrations;
- Billing;
- Analytics / Reporting.

Do not create generic business abstractions merely to avoid context contracts.

Bad examples:

```text
GenericBusinessEntity
GenericWorkspaceObject
GenericEntitlement
GenericWorkflowAggregate
```

Shared technical vocabulary is valid. Shared business vocabulary requires explicit architecture ownership.

## 4. Backend boundary

The backend remains the existing modular monolith:

```text
Notrelix.Domain
Notrelix.Application
Notrelix.Infrastructure
Notrelix.Platform
Notrelix.API
```

Platform work is expected mainly in Application, Infrastructure, Platform and API plus tests/tooling.

`Notrelix.Domain` is not a generic technical-extension project.

Exact project inventory remains generated in:

```text
backend/docs/generated/project-map.md
```

## 5. Frontend boundary

Platform/Foundation primarily owns mechanisms in architecture-approved families such as:

```text
packages/foundation/*
packages/runtimes/*
packages/ui/*
tooling/*
```

Exact package dependency authority remains:

```text
frontend/tooling/dependency-rules/src/architecture-manifest.ts
```

Generated evidence remains:

```text
frontend/docs/generated/package-boundaries.md
```

This workstream MUST NOT redefine exact package edges.

## 6. Capability lanes

Platform delivery is decomposed into:

```text
PF-01 Session and CSRF transport
PF-02 Actor/account/workspace context propagation
PF-03 Authorization enforcement pipeline
PF-04 Persistence foundation
PF-05 Migration/runtime database initialization
PF-06 Idempotency
PF-07 Outbox/inbox and message identity
PF-08 Ordered delivery and poison handling
PF-09 Realtime connection/recovery
PF-10 API/OpenAPI/generated contracts
PF-11 Frontend query/server-state foundation
PF-12 Frontend runtime/host composition
PF-13 UI token/primitive foundation
PF-14 Observability
PF-15 Architecture/dependency enforcement
PF-16 CI execution and packaging evidence
```

These lanes are separate workstreams. A team should not bundle unrelated platform changes into one "foundation cleanup" PR.

# PF-01 — Session and CSRF transport

## 7. Ownership split

Identity owns:

- authentication meaning;
- session lifecycle;
- session expiration;
- logout/invalidation semantics.

Platform/API/frontend foundation owns:

- cookie/header transport;
- credential mode;
- generic CSRF extraction/attachment;
- generic session-expired signaling.

## 8. Known source debt

Previous source review identified drift:

Backend:

```text
cookie: csrf_token
header: X-CSRF-Token
```

Frontend:

```text
cookie: XSRF-TOKEN
header: X-XSRF-TOKEN
```

This remains:

```text
SOURCE_DEBT
+
CONTRACT_CHANGE
```

until producer and consumer are aligned.

Documentation MUST NOT bless one side solely because it currently exists in source.

## 9. Execution plan

1. Resolve intended canonical contract from accepted architecture/ADR/API behavior.
2. Enumerate producer and consumer locations.
3. Select one canonical cookie/header pair.
4. Update backend and frontend atomically where possible.
5. Update tests.
6. Update public/generated contract evidence if applicable.
7. Prove valid protected browser mutations work.
8. Prove missing/invalid CSRF is rejected.
9. Prove session expiration follows typed frontend handling.

## 10. Forbidden fixes

Do not:

- disable CSRF;
- accept two conventions indefinitely without a migration reason;
- put feature-local CSRF logic in WorkManagement/Documents/etc.;
- move browser auth to JS bearer-token storage as a shortcut;
- hide mismatch behind frontend retry.

## 11. Required evidence

```text
valid session + valid CSRF → allowed
valid session + missing CSRF → rejected
valid session + invalid CSRF → rejected
expired session → typed session-expired behavior
```

Protected browser consumers require this contract at `D5 STABLE`.

# PF-02 — Actor/account/workspace context propagation

## 12. Context separation

The technical runtime must preserve distinct concepts:

```text
authenticated actor
selected account
selected workspace
```

Do not collapse them into one opaque "current context" if that obscures ownership.

## 13. Backend obligations

The mechanism must:

- resolve context consistently;
- reject spoofed tenant/account/workspace IDs;
- make missing context explicit;
- support background execution rules;
- avoid process-global mutable context;
- remain testable with production-like wiring.

## 14. Frontend obligations

Route navigation is not proof of server-state isolation.

Account/workspace transitions must coordinate:

- active context;
- query/cache partition/reset;
- realtime subscriptions;
- optimistic state;
- permission state.

# PF-03 — Authorization enforcement pipeline

## 15. Ownership split

Resource context owns:

```text
resource identity
resource lifecycle
business action meaning
```

Governance owns:

```text
role
permission
policy
```

Platform/Application owns:

```text
enforcement pipeline
```

## 16. Required invariant

Protected application operations MUST use the approved authorization pipeline.

Handler-local role/permission checks must not become a parallel policy engine.

## 17. Resource handoff

When a feature introduces a protected operation:

1. resource team defines resource kind;
2. resource team defines resource ID and action;
3. account/workspace scope is defined;
4. Governance maps policy;
5. Application declares authorization requirement;
6. pipeline enforces;
7. integration/API tests prove allow and deny.

If current resource/action semantics cannot represent the feature cleanly:

```text
STOP
→ classify architecture gap
→ do not implement local bypass
```

# PF-04 — Persistence foundation

## 18. Scope

Platform/Infrastructure may own:

- shared EF Core mechanisms;
- technical interceptors;
- transaction infrastructure;
- tenant-isolation mechanisms;
- connection/runtime setup;
- generic persistence helpers preserving context ownership.

## 19. Non-scope

Do not introduce:

- generic cross-context repositories exposing all entities;
- business-specific queries into Platform;
- lifecycle cascades across contexts as shared infrastructure;
- shared tables used merely to avoid defining contracts.

## 20. Consistency rule

A modular monolith makes cross-context DB access technically easy. That does not make it architecturally valid.

Prefer:

- explicit application contract;
- event;
- read model;
- approved orchestration.

Private-table access remains exceptional.

# PF-05 — Database migration and initialization

## 21. Required behavior

Own reliable:

- migration discovery;
- migration execution;
- environment initialization;
- startup behavior;
- failure visibility.

`PendingModelChangesWarning` is evidence of source/schema inconsistency. Do not suppress it merely to make startup pass.

## 22. Migration checklist

For schema-affecting work:

- migration exists;
- model and migration agree;
- clean DB startup passes;
- upgrade startup passes;
- tenant isolation preserved;
- rollback/forward-fix considered;
- production graph tested where required.

# PF-06 — Idempotency

## 23. Ownership split

Business team owns:

- which operation requires idempotency;
- business operation identity;
- expected duplicate result.

Platform owns:

- idempotency pipeline;
- technical key/state storage;
- duplicate request coordination;
- retry-safe mechanism.

## 24. Required identity

Technical identity must avoid collision across unrelated operations.

Where architecture requires it, identity includes:

```text
operation
tenant/account scope
client idempotency key
```

Two commands sharing the same raw key string MUST NOT accidentally share state.

## 25. Required tests

- first execution;
- duplicate while in progress;
- duplicate after success;
- same key, different operation;
- same key, different tenant/account;
- failure and retry;
- persisted response/result semantics where supported.

# PF-07 — Outbox / inbox / message identity

## 26. Scope

Own:

- persisted delivery record;
- message identity;
- consumer identity;
- deduplication;
- retry state;
- delivery state transition.

Producer contexts own event meaning. Consumer contexts own reaction.

## 27. Identity rule

Poison detection/deduplication must not be scoped only by event-name string if architecture requires true message identity.

Different messages of the same event type must remain distinguishable.

# PF-08 — Ordered delivery and poison handling

## 28. Ordering invariant

Sequence progression must follow the approved success semantics.

Do not advance ordering state before handler success if that can cause lost work.

## 29. Poison invariant

A repeatedly failing message must not poison every other message sharing its event name.

Poison identity must align with message/consumer identity.

## 30. Verification scenarios

- success;
- transient failure then success;
- terminal poison;
- next valid message;
- duplicate;
- same event type with different message IDs;
- consumer restart;
- ordered sequence after recovery.

# PF-09 — Realtime connection and recovery

## 31. Ownership split

Platform owns:

- connection;
- reconnect;
- resubscription;
- generic duplicate/out-of-order handling;
- gap detection;
- generic recovery mechanism.

Business context owns:

- acceptable staleness;
- resource-state semantics;
- rebase/reload behavior;
- conflict semantics.

## 32. Known unresolved gap

Prior review found invalidation behavior after realtime gaps but no proven:

```text
checkpoint
replay
rebase
sequence recovery
```

contract.

Until executable evidence exists, classify as:

```text
UNRESOLVED
```

## 33. Consumer recovery contract

Each consumer must define:

- subscription scope;
- event identity;
- sequence/checkpoint if used;
- duplicate behavior;
- out-of-order behavior;
- gap detection;
- recovery action;
- final post-recovery guarantee.

## 34. Forbidden assumption

```text
invalidate query
```

is not automatically equivalent to ordered-stream recovery.

For some data a reload may be sufficient. For ordered/collaborative state it may not be.

Critical consumers:

- WorkManagement;
- Documents;
- Collaboration;
- Automation execution/status UI.

# PF-10 — API/OpenAPI/generated contracts

## 35. Backend/frontend split

Backend API owns transport and OpenAPI exposure.

Domain/Application own semantics.

Frontend foundation owns generated-contract consumption mechanisms.

Features SHOULD NOT maintain handwritten DTOs that compete with generated contracts when generated coverage exists.

## 36. Contract gate

A contract change should trigger as applicable:

- backend build/tests;
- OpenAPI drift;
- codegen regeneration;
- frontend typecheck;
- consumer tests;
- migration/compatibility review.

Breaking producers MUST NOT merge ahead of consumers without explicit compatibility.

# PF-11 — Frontend query/server-state foundation

## 37. Ownership

Platform owns generic server-state mechanism.

Business teams own query identity and invalidation semantics for their data.

## 38. Known account-isolation concern

Prior review found `accountQueryKey` did not explicitly include account identity.

This is safe only if account transition reliably clears all account-scoped state before the next account activates.

Acceptable resolution:

```text
A. account ID participates in query identity
B. proven hard reset
C. equivalent formally verified partitioning
```

## 39. Required isolation test

```text
load Account A
cache A resources
switch to Account B
assert no A resource can be returned under B
switch back
verify correct lifecycle/reload behavior
```

Do not grow account-scoped feature usage on an unproven assumption.

# PF-12 — Frontend runtime and host composition

## 40. Scope

Own:

- runtime construction;
- environment wiring;
- host adapters;
- router/runtime integration mechanism;
- generic session-expired event plumbing.

## 41. Host boundary

Current hosts have distinct framework responsibilities:

```text
web
mobile
marketing
```

Reusable packages MUST NOT assume a host framework unless frontend architecture permits it.

## 42. Navigation ownership

Generic auth/session infrastructure should emit typed state/events.

App host owns navigation policy.

Do not hard-code application routes in generic auth infrastructure if accepted frontend ADRs assign routing to apps.

# PF-13 — UI token and primitive foundation

## 43. Classification

Before adding shared UI:

```text
generic token?
generic primitive?
product component?
host-specific component?
```

Product semantics stay outside shared UI.

## 44. Known export evidence issue

Previous review did not prove that the declared UI-token `./css` export resolved to real source/build output.

Verify:

- package exports;
- source path;
- build artifact;
- consuming import.

Do not fix docs by claiming an export source cannot provide.

## 45. Density

Generic density tokens may be foundational.

Board/table-specific density behavior can remain WorkManagement-owned while consuming generic tokens.

# PF-14 — Observability

## 46. Mechanism ownership

Platform owns:

- logging;
- tracing;
- metrics transport;
- correlation;
- error reporting;
- common context enrichment;
- batching/sampling mechanism.

Business teams own domain-specific meaning.

## 47. Secret safety

Never log:

- raw API keys;
- OAuth tokens;
- session secrets;
- provider credentials;
- sensitive authorization headers.

## 48. Critical signals

Shared mechanisms should expose, where appropriate:

- operation/consumer identity;
- safe account/tenant scope;
- correlation;
- latency;
- retry count;
- failure class;
- terminal failure.

# PF-15 — Architecture/dependency enforcement

## 49. Backend

Maintain gates for:

- Domain purity;
- layer dependencies;
- bounded-context isolation where governed;
- pipeline-owned authorization;
- critical platform invariants.

## 50. Frontend

Maintain executable dependency authority in:

```text
frontend/tooling/dependency-rules/src/architecture-manifest.ts
```

Generated package-boundary docs derive from it.

## 51. Gate rule

Do not weaken a gate because a feature conflicts with it.

A conflict means:

```text
implementation is wrong
OR
architecture is deliberately changing
```

not:

```text
test is inconvenient
```

# PF-16 — CI execution and packaging evidence

## 52. CI principle

A green aggregate gate must prove required suites actually executed.

Critical suites should verify non-zero execution.

## 53. Backend chain

Conceptually:

```text
quality
architecture
core
platform
api
integration
↓
docker-build
↓
final gate
```

If architecture fails, Docker may be skipped. Final gate should report that rather than reinterpret skipped as success.

## 54. Frontend container smoke

If a job is named "Container smoke", it should:

- start the built container;
- probe a health/HTTP route;
- fail if startup/health fails.

If it only builds, rename it as build evidence.

## 55. Generated docs

CI checks generated drift.

CI should not silently regenerate and commit canonical generated artifacts.

# Delivery management

## 56. Dependency readiness targets

| Platform contract | Consumer | Target |
|---|---|---|
| session/CSRF | Identity + protected browser features | D5 |
| actor/account/workspace context | all tenant-scoped teams | D5 |
| authorization enforcement | all protected teams | D5 |
| idempotency | governed command consumers | D4-D5 |
| messaging delivery | async consumers | D5 |
| realtime recovery | WM/Documents/Collaboration | D4+ per consumer |
| account state isolation | frontend tenant-scoped teams | D5 |
| generated API contract flow | frontend teams | D5 |
| UI export evidence | UI consumers | D4 |
| architecture/CI gates | all teams | D5 |

## 57. Platform execution waves

### PF Wave 0 — security and tenancy blockers

```text
PF-01 Session/CSRF
PF-02 Context propagation
PF-03 Authorization enforcement
PF-11 Query isolation
```

### PF Wave 1 — async and realtime reliability

```text
PF-06 Idempotency
PF-07 Message identity
PF-08 Ordering/poison
PF-09 Realtime recovery
```

### PF Wave 2 — developer throughput

```text
PF-10 Generated contracts
PF-12 Runtime
PF-13 UI foundation
PF-14 Observability
```

### PF Wave 3 — continuous governance

```text
PF-15 Architecture tooling
PF-16 CI/packaging
```

Persistence/migration work runs alongside the wave that requires it.

## 58. Cross-team handoff contract

For a shared mechanism change record:

```text
Platform capability:
Current contract:
Target contract:
Affected teams:
Breaking/additive:
Migration strategy:
Feature-team action:
Platform action:
Verification:
Required readiness:
Rollback/forward-fix:
```

Consumers must not reverse-engineer a changed platform contract from source.

## 59. Team-local decisions

May decide locally:

- private implementation shape;
- internal data structures;
- test fixtures;
- performance changes preserving contracts.

Must escalate:

- new cross-cutting framework;
- new production project/service;
- authorization architecture change;
- message-delivery guarantee change;
- frontend architecture-manifest semantic change;
- new global state architecture;
- repository-wide dependency adoption;
- security weakening.

## 60. Stop conditions

Stop and escalate when:

- shared abstraction needs business-specific branching;
- resource/action model cannot represent a protected feature;
- tenant/account ownership is ambiguous;
- messaging fix changes delivery guarantees;
- realtime requires business-specific conflict policy;
- shared frontend package needs a forbidden edge;
- a new service/project is proposed;
- a security mechanism must be weakened;
- generated contract/source ownership is unclear.

## 61. Verification matrix

Backend as applicable:

- unit;
- architecture;
- Application;
- Infrastructure;
- Platform;
- API;
- integration;
- migration;
- production-graph tests.

Frontend as applicable:

- package tests;
- dependency architecture;
- typecheck;
- format/lint;
- build;
- generated-doc check;
- runtime;
- query isolation;
- realtime recovery;
- critical E2E.

Cross-stack as applicable:

- session/CSRF;
- OpenAPI/codegen;
- session-expired behavior;
- account switching;
- realtime reconnect/gap recovery.

## 62. Definition of Done

A Platform capability is `DONE` only when:

- mechanism ownership is clear;
- business semantics remain outside Platform;
- producer/consumer contract is explicit;
- security and tenant isolation hold;
- failure/recovery semantics are defined;
- critical evidence exists;
- generated contracts/docs are current;
- architecture gates remain intact;
- downstream consumers can adopt without guessing;
- migration/rollout is safe.

## 63. Platform foundation exit criteria

Broad parallel feature delivery is supported when:

- session/CSRF is D5;
- context propagation is D5;
- authorization enforcement is D5;
- critical idempotency/messaging paths are verified;
- realtime recovery contract exists for realtime-heavy consumers;
- account-scoped frontend state isolation is proven;
- generated API/package evidence is reliable;
- UI token exports are source-backed;
- CI proves required suites execute;
- no business semantics have leaked into shared infrastructure.

## 64. Service extraction role

Platform supports extraction with transport, observability, messaging, deployment/security and migration mechanisms.

It does not select service boundaries.

The bounded-context team owns business boundary and contract meaning.

System architecture authority approves extraction.
