# Domain Event Classification

Domain events are business facts. They are not automatically audit logs,
activity feed entries, notifications, or integration contracts.

## Event Classes

| Class | Meaning | Default dispatch |
|---|---|---|
| Local domain event | In-process reaction only. | Inline or ignored by dispatch policy. |
| Durable domain event | Reliable internal side effect is needed. | Outbox when required. |
| Integration-event candidate | Fact may cross bounded context or external boundary after mapping. | Outbox plus mapper when approved. |
| Audit-only change | Compliance/security fact, not normal product event. | Audit/security pipeline, often ignored by domain dispatch. |
| Activity-only change | User-facing feed fact derived from domain event. | Projection, not direct aggregate responsibility. |
| No-event change | Internal state change or no-op. | None. |

## Naming Rules

- Domain event classes use `<Aggregate><PastTenseFact>DomainEvent` unless the
  event is explicitly classified as a security/audit/event-store record.
- Legacy events without the `DomainEvent` suffix must be allowlisted with a
  target state.
- Event names describe facts, not commands: `BoardRenamedDomainEvent`, not
  `RenameBoardDomainEvent`.
- Event names do not mention transport, queue, Redis, SignalR, email, or HTTP.

## Required Metadata

All domain events must derive from `DomainEvent` and therefore expose:

- `EventId`;
- `OccurredAt`;
- `EventVersion`;
- optional `WorkspaceId`;
- optional `ActorUserId`;
- optional correlation/causation identifiers.

Workspace-scoped aggregate events should pass `WorkspaceId`. User/system events
that are global may leave it null. Actor metadata should be present for
user-driven changes unless the event is a documented system transition.

## When Not To Create Events

Do not create events for:

- idempotent no-op calls;
- pure calculations;
- persistence mapping or cache normalization;
- provider retry state;
- presence heartbeat and realtime connection state;
- search indexing progress;
- permission cache recomputation;
- outbox/idempotency/job-lock state changes.

## Current Classification Starting Point

| Event family | Initial classification |
|---|---|
| `WorkspaceCreated`, `WorkspaceRenamed`, `WorkspaceArchived`, `WorkspaceSoftDeleted`, `WorkspaceRestored` | Durable domain event; integration candidate only after external contract exists. |
| `WorkspaceAssignedToAccount`, `WorkspaceSettingsUpdated` | Missing or unclassified; audit in D2 before adding events. |
| `Board*`, `BoardItem*`, `BoardField*` lifecycle/value events | Durable domain events for internal projections/search/activity candidates. |
| `Page*`, `Block*` durable content/tree events | Durable domain events; realtime editor operations remain out of Domain. |
| `Comment*` | Domain events; activity/notification candidates. |
| `UserPasswordChanged`, `UserLoggedIn`, OAuth/MFA security events | Audit/security-first; durable product dispatch requires explicit classification. |
| `Subscription*`, `Entitlement*` | Durable domain events; integration candidates after billing anti-corruption mapping. |
| `PresenceUpdated` | Runtime/presence classification risk; should not become durable business event by default. |
| `Notification*`, `UnreadCounter*`, `ActivityLogged` | Projection/delivery/activity classification risk; audit before expanding. |

## Event Classification Table Requirements

Each context rulebook or audit must record:

| Event | Context | Source aggregate | Class | Durable | Integration candidate | Consumer | Notes |
|---|---|---|---|---|---|---|---|

If a new event cannot fill this row, it must not be introduced.
