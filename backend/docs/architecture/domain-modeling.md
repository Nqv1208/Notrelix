# Domain Modeling

## Scope

Backend Domain aggregates, entities, value objects, pure rules, lifecycle,
events, tenant scope, and mutation contracts.

## Responsibility / Ownership

Domain owns deterministic business invariants and state transitions. It does
not own persistence, authorization orchestration, provider calls, transport
contracts, background delivery, or current user/time discovery.

## Current Architecture

Domain source lives in `backend/src/Notrelix.Domain`. Behavior is proven by
`Notrelix.Domain.Tests` and guarded by architecture tests.

## Normative Contracts

- Aggregate roots are transactional consistency boundaries with independent
  lifecycle, invariants, identity, loading/concurrency needs and meaningful
  events.
- Child entities exist only inside their root and cannot bypass root-owned
  invariants.
- Value objects are immutable, validated at construction and deterministic.
- Domain receives external facts from Application: actor, time, authorization
  result, parent paths, counts, random input and provider facts.
- Mutations validate lifecycle, actor/IDs, business rules, normalize input,
  detect semantic no-op, prepare prospective state, mutate once, apply audit,
  increment version exactly once and raise approved events.
- Rejected mutations and semantic no-ops leave state, audit, version and
  pending events unchanged unless a specific documented contract says otherwise.
- Domain events represent completed facts, carry correct tenant scope, copy
  caller-owned collections, avoid secrets and preserve logical names/versions.
- Aggregate roots reference other roots by immutable IDs/context snapshots, not
  navigation objects.
- Deletion/lifecycle policy must use product language: archive, revoke, cancel,
  remove, resolve, reopen or tombstone. Soft delete is not default.
- SharedKernel/Common admission requires stable semantics across contexts.

## Allowed Design

- Pure cross-aggregate rule methods that consume immutable facts supplied by
  Application.
- Typed IDs where they protect public contracts or aggregate correctness.
- No-event contracts when a mutation is internal/root-owned and not consumed.

## Forbidden Design

- EF Core, DbContext, HTTP, Redis, provider SDKs, search/storage APIs, MediatR
  handlers, DTOs or controllers in Domain.
- `DateTime.UtcNow`, random generation, ambient culture or current-user access.
- Repository callbacks passed into Domain.
- Public child mutators that bypass the aggregate root.
- `Status = SoftDeleted` or hidden `_statusBeforeDeletion` repair state.
- Global events for workspace/account-scoped facts.

## Failure Modes

- Validation mutates owned state before a later rule throws.
- A no-op updates audit/version because timestamp validation happened too soon.
- A child entity protects a cross-child invariant without root participation.
- Events expose stale/unnormalized values or wrong scope.

## Change Impact Rules

Domain behavior changes require focused behavior tests for applicable success,
rejection, no-op, failure atomicity, audit/version/event, lifecycle and tenant
scope scenarios. Frozen public event/rule-code changes require contract review.

## Core Aggregate Coverage

These entries preserve the former core-aggregate audit gate inside the canonical
Domain architecture document. They are coverage anchors, not a replacement for
source tests.

## Aggregate: User

Identity aggregate covering authenticated user lifecycle and identity facts.

## Aggregate: Workspace

Workspace aggregate covering workspace lifecycle and root workspace facts.

## Aggregate: WorkspaceMember

Workspace membership aggregate covering member lifecycle and role participation.

## Aggregate: WorkspaceInvitation

Workspace invitation aggregate covering invite lifecycle and acceptance facts.

## Aggregate: Board

Work Management board aggregate covering work table/database lifecycle.

## Aggregate: BoardItem

Work Management item aggregate covering row/task/item state.

## Aggregate: BoardField

Work Management field aggregate covering dynamic schema field contracts.

## Aggregate: Page

Documents page aggregate covering document root lifecycle and hierarchy entry.

## Aggregate: Block

Documents block aggregate covering editable document content nodes.

## Aggregate: Comment

Collaboration comment aggregate covering scoped discussion facts.

## Aggregate: ResourcePermission

Governance permission aggregate covering explicit resource permission grants.

## Aggregate: CustomRole

Governance role aggregate covering workspace/account role definitions.

## Aggregate: ShareLink

Collaboration/governance sharing aggregate covering externally visible share
link lifecycle.

## Aggregate: Subscription

Billing subscription aggregate covering account billing lifecycle.

## Aggregate: Entitlement

Billing entitlement aggregate covering account capability access facts.

## Executable Evidence / Tests / Gates

- `backend/src/Notrelix.Domain`
- `backend/tests/Notrelix.Domain.Tests`
- `backend/tests/Notrelix.Architecture.Tests`
- `dotnet test tests/Notrelix.Domain.Tests/Notrelix.Domain.Tests.csproj`

## Related ADRs

See `../decisions/README.md`.

## Related Source Manifests

`backend/backend.slnx` and project references define compile-time boundaries.

## Non-responsibilities

Domain does not define API shape, persistence schema, RLS policy, cache keys,
message delivery, or frontend behavior.
