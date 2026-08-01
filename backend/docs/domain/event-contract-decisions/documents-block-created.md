# Event Contract Decision — `documents.block-created`

**Date:** FZ67 phase (domain freeze final completion plan)
**Aggregate:** `Block`
**Event:** `BlockCreatedDomainEvent` (`[EventName("documents.block-created")]`, version 1)

## Decision

Branch B — the event is a **creation notification** and is intentionally left
unchanged. `ParentId` is deliberately **absent** from the payload.

## Rationale

Evidence inventory (src, tests, frontend) classifies every consumer:

| Consumer | Classification |
| --- | --- |
| `Block.CreateRoot` / `Block.CreateChild` (raises the event) | notification |
| `BlockTests` | tests only |
| Application / Infrastructure / API | no consumers |
| Realtime editor | no in-repo consumer |
| Integration events | none mapped (outbox dispatch is attribute-driven) |

There are no hierarchy consumers of `documents.block-created` in the repository.

Hierarchy facts are already conveyed by:

1. The persisted `Block.ParentId` property, set by the factory
   (`CreateRoot` → `null`, `CreateChild` → `parentPath.TargetParentId`).
2. `BlockMovedDomainEvent` (`documents.block-moved`), which carries
   `OldParentId` / `NewParentId` for every hierarchy change.

Adding `ParentId` to the created event would bump the contract to v2 with zero
in-repo consumers, which is churn without a projection/realtime requirement.

## Contract

- Event is a creation notification, not a hierarchy projection source.
- Hierarchy consumers must query the persisted `Block.ParentId` or subscribe to
  `documents.block-moved`.
- `ParentId` is intentionally absent; do not add it without a documented
  projection/realtime consumer.

## Lock

`BlockTests.BlockCreated_ShouldBeNotificationOnly_WithoutParentId` asserts the
created-event contract excludes `ParentId`. Changing the contract requires
updating this decision document first.
