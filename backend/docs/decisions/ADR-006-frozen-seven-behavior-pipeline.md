---
document_id: ADR-006
document_type: architecture-decision
status: Accepted
owner: backend-architecture
applies_to:
  - backend
  - backend-application
  - application-pipeline
evidence:
  - backend/docs/architecture/application-model.md
  - backend/src/Notrelix.Application/DependencyInjection.cs
  - backend/src/Notrelix.Application/Common/Behaviors/
  - backend/src/Notrelix.Application/Common/Diagnostics/PipelineActivitySource.cs
  - backend/tests/Notrelix.Architecture.Tests/ApplicationLayer/ApplicationArchitectureTests.cs
  - backend/tests/Notrelix.Architecture.Tests/LayerRules/CommonFolderArchitectureTests.cs
review_on:
  - application-pipeline-behavior-set-change
  - data-session-boundary-change
  - access-control-ownership-change
  - idempotency-consistency-model-change
---

# ADR-006: Frozen Seven-Behavior Application Pipeline

## ID

`ADR-006`

## Status

Accepted

## Date

`2026-08-24`

## Owners

- `backend-architecture`

## Context

ADR-001 formalized the MediatR pipeline as ordered boundary zones after the
behavior list had grown to nineteen classes. The zone model fixed ordering
correctness but kept nineteen separate orchestration components, several of
which existed only to shuttle state between zones:

```text
TenantBootstrapBehavior
TokenValidationBehavior
SystemOperationAuditBehavior
ResourceScopeBehavior
PostCommitScopeBehavior
PublicCacheBehavior / AuthorizedCacheBehavior
DbRequestScopeBehavior
AuthorizationBehavior
VerifiedEmailBehavior
FeatureGateBehavior
SubscriptionGateBehavior
ConcurrencyBehavior
```

The pipeline-freeze execution collapsed these into a minimal behavior set with
explicitly owned responsibilities. The resulting composition is smaller,
machine-gated, and observable end to end. This ADR records the final accepted
shape so the behavior count and ownership boundaries are durable decisions, not
incidental source state.

## Decision

The Application request pipeline consists of **exactly seven behaviors**,
registered outermost-to-innermost:

```text
ExceptionMappingBehavior
ApplicationTracingBehavior
RequestContractBehavior
ExecutionContextBehavior
DataSessionBehavior
AccessControlBehavior
IdempotencyBehavior
```

There is no eighth orchestration behavior: request validation executes
inside `RequestContractBehavior`, never as a separate pipeline stage.

Ownership boundaries of the frozen pipeline:

```text
DataSession owns the DB/RLS/transaction boundary via IRequestDataSession;
Application declares when/why, Infrastructure implements how.

AccessControl = AccessFacts resolution + pure policy evaluation;
gates (IRequire*) declare requirements; no snapshot stores, no
permission services scattered across Infrastructure.

Idempotency outcome is atomic with the business mutation/outbox
inside the DataSession transaction; same identity with a different
request hash fails payload-mismatch in every prior state.

Concurrency is persistence-level optimistic concurrency inside the data
session: every declared `IExpectedVersionRequest` constraint binds to exactly
one tracked aggregate through an exhaustive infrastructure target map and
fails closed as a server misconfiguration when binding is impossible.

Durable async effects flow through the outbox/broker; there is no
synchronous realtime/post-commit network work inside the pipeline and
no generic response-cache behavior.
```

### Superseded architecture

ADR-001's six-zone model with nineteen behaviors is superseded. Its ordering
insight (dependency-driven order, outer zone free of transactional state)
survives as invariants of the frozen set; its component inventory does not.

## Consequences

```text
Cross-cutting concerns are added inside the seven owners or as declared
ports — never as a new pipeline behavior without superseding this ADR.

Telemetry is first-class: root activity `pipeline.request` plus canonical
stage spans (`request.contract`, `execution_context.resolve`,
`data_session.open|rls|save_changes|commit|rollback`, `access_facts.query`,
`policy.evaluate`, `idempotency.acquire|replay|complete`, `handler.execute`)
are part of the pipeline contract and proven by integration evidence.

Response serialization telemetry belongs to transport observability and is
explicitly out of scope for the application boundary.

`PipelineFreezeArchitectureTests` freezes registration and topology;
any eighth orchestration behavior fails architecture CI.
```

## Frozen invariants

```text
exactly seven production pipeline behaviors; validation executes inside RequestContractBehavior (no eighth validation stage)
DataSession owns DB/RLS/transaction boundary
AccessControl owns access facts + policy evaluation
no generic response-cache behavior
no synchronous realtime/post-commit network work
concurrency enforced atomically at persistence
idempotency outcome atomic with business mutation/outbox
durable async effects flow through outbox/broker
```

## Supersedes

`ADR-001` (pipeline boundary zones with nineteen behaviors). The zone-ordering
rationale is preserved as frozen invariants above.

## Superseded By

`None`
