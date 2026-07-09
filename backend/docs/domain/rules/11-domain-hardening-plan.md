# Domain Hardening Plan

This file records the remaining Domain weaknesses to harden before the Domain base is considered fully locked.

## Current strengths

The current Domain foundation has several strong points:

```txt
Domain project is isolated from outer layers.
Common primitives exist for Entity, AggregateRoot, DomainEvent, scoping, soft delete, audit, guard.
SharedKernel contains useful value objects such as Email, ResourceRef, FractionalIndex, Money, Slug, Url.
Core WorkManagement aggregates such as Board and BoardField use factories, private setters, guards, audit, version increments, and domain events.
Governance ShareLink encodes important business rule: public share links must expire.
Domain tests are organized by bounded context.
```

## Weakness 1 — Version/event consistency may not be complete across all aggregates

Some aggregates have strong version/event discipline, but the rule must be enforced across every bounded context.

### Risk

```txt
Optimistic concurrency becomes unreliable.
Audit/projection/event consumers miss meaningful changes.
Different contexts mutate state with different discipline.
```

### Fix

```txt
Add architecture/test coverage for mutation methods on AggregateRoot.
Audit all aggregate methods for SetAuditOnUpdate + IncrementVersion + domain event.
Document explicit exceptions for no-op/internal-only methods.
```

### Rule

Every meaningful aggregate mutation must update audit, increment version, and raise an event when external/audit/projection consumers need the fact.

## Weakness 2 — DomainEvent EventId preservation for replay/deserialization must be confirmed

The base `DomainEvent` currently creates `EventId` in the constructor. This is fine for newly raised events, but replay/deserialization must preserve original event identity if domain events are ever dispatched from serialized payloads.

### Risk

```txt
EventId changes during deserialization.
Idempotency breaks.
Audit/outbox correlation breaks.
```

### Fix

```txt
Confirm outbox dispatch does not deserialize DomainEvent in a way that regenerates EventId.
If replay is required, add constructor/factory support for preserved EventId.
Prefer integration-event contracts for external dispatch.
```

## Weakness 3 — SharedKernel may grow too large

`ResourceType` is broad and shared across the system. This is useful for security/governance, but adding to it should be controlled.

### Risk

```txt
SharedKernel becomes dumping ground.
Context-specific concepts leak globally.
Security/resource resolver misses new resource types.
```

### Fix

```txt
Require checklist for every new ResourceType.
Update permission resolver/version reader/resource scope resolver when needed.
Add architecture tests for resource coverage.
```

## Weakness 4 — Cross-context object references must remain controlled

Domain contexts should coordinate through IDs and shared primitives, not direct object graphs.

### Risk

```txt
Large aggregate graphs.
Hidden coupling between bounded contexts.
Persistence mapping becomes fragile.
```

### Fix

```txt
Architecture tests against cross-context navigation properties.
Prefer ResourceRef/IDs.
Application orchestrates multi-context use cases.
```

## Weakness 5 — Domain tests should become invariant-driven, not only happy-path

Tests exist by bounded context, but coverage quality should be measured by invariant coverage.

### Required maturity matrix

For each core aggregate:

```txt
Factory validation
Mutation validation
No-op behavior
Version increment
Audit update
Domain event metadata
Soft delete/restore
Invalid state transitions
```

Priority aggregates:

```txt
Workspace
WorkspaceMember
Board
BoardField
BoardItem
ShareLink
Comment
Page
Subscription
Entitlement
OAuthAccount
```

## Hardening execution order

```txt
1. Add RULE-domain-layer-patch.md to backend/RULE.md.
2. Add architecture gates for Domain purity and public setters.
3. Audit AggregateRoot mutation methods for version/audit/event consistency.
4. Add invariant test matrix for priority aggregates.
5. Confirm DomainEvent EventId preservation in outbox/replay path.
6. Add ResourceType addition checklist and resolver coverage tests.
7. Add cross-context navigation/property architecture tests.
```

## Definition of Done for Domain foundation lock

Domain is considered locked when:

```txt
All Domain purity architecture tests pass.
All priority aggregates have invariant tests.
No aggregate has public setters for mutable business state.
All meaningful mutations update audit/version and raise events where required.
Soft delete/restore behavior is explicit and tested.
ResourceType additions require resolver/test updates.
Domain events preserve identity across the outbox path if replayed.
No Domain code references Application/Infrastructure/API packages.
```
