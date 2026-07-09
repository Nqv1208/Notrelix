# Domain Events and Outbox Boundary

Domain events describe facts that happened in Domain. They are not commands and they are not integration contracts.

## Domain event types

Current Domain base primitives include:

```txt
IDomainEvent
IDurableDomainEvent
ILocalDomainEvent
DomainEvent
GlobalDomainEvent
AccountScopedDomainEvent
AccountRootDomainEvent
AccountScopedDomainEvent
BillingAccountScopedDomainEvent
```

Use the event type that matches the event's durability and scope.

## Event naming

Use past-tense fact names:

```txt
BoardCreatedDomainEvent
BoardRenamedDomainEvent
BoardSoftDeletedDomainEvent
ShareLinkCreatedEvent
ShareLinkDisabledEvent
```

Do not use command-style names:

```txt
CreateBoardEvent
RenameBoardEvent
DisableShareLinkCommandEvent
```

## Event payload rule

Domain event payload should include enough data for audit/projection/integration mapping, but not huge object graphs.

Good:

```txt
AccountId
WorkspaceId
AggregateId
ResourceId
ActorUserId
OccurredAt
Old value / new value when needed
```

Bad:

```txt
Entire aggregate object
EF navigation graph
Raw provider token
Large JSON snapshots unless explicitly required
```

## Event metadata rule

Events should carry standard metadata through `DomainEvent`/`IDomainEvent`:

```txt
EventId
EventVersion
OccurredAt
SourceContext
AggregateType
AggregateId
SubjectType
SubjectId
WorkspaceId?
ActorUserId?
CorrelationId?
CausationId?
```

If the event is workspace-scoped, workspace id must be provided. If it is actor-driven, actor user id should be provided.

## Raising events

Only aggregates/entities should raise domain events as part of state changes.

Good:

```csharp
Title = normalizedTitle;
SetAuditOnUpdate(updatedBy, updatedAt);
IncrementVersion();
AddDomainEvent(new BoardRenamedDomainEvent(...));
```

Bad:

```csharp
// Application handler manually creates BoardRenamedDomainEvent without calling Board.Rename.
```

## Durable vs local domain events

Use durable events for:

```txt
Outbox
Audit
Projections
Cross-context reactions
Integration event mapping
Any event whose processing must survive process restart
```

Use local events only for:

```txt
Pure in-process signals
No external side effects
No durability requirement
No outbox requirement
```

Current Infrastructure rejects inline domain event dispatch. Therefore, new events should normally be durable or explicitly covered by dispatch policy.

## Domain event vs integration event

Domain event:

```txt
Internal business fact inside the source bounded context.
May contain internal domain vocabulary.
Owned by Domain.
```

Integration event:

```txt
External/cross-context contract.
Versioned explicitly.
Mapped outside Domain.
Owned by Application/Infrastructure messaging boundary.
```

Domain must not create MassTransit messages or publish integration events directly.

## Event version rule

Every event should have an `EventVersion`. Start with `1`. If payload changes incompatibly, create a new version or explicit event type. Do not silently change payload semantics.

## EventId rule

A domain event must have a stable `EventId` for the lifetime of the raised event. Do not recreate domain events during dispatch in a way that changes `EventId` and breaks idempotency/audit correlation.

If event deserialization/replay is required, the event model must support preserving the original `EventId`.

## Event emission checklist

For each meaningful mutation, ask:

```txt
Does another part of the system need to know this happened?
Is audit/activity/projection affected?
Does this event cross bounded-context boundaries after mapping?
Does it need workspace/account/actor metadata?
Is this event raised only after actual state change?
```

If yes, raise a domain event from the aggregate method.
