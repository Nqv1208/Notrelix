# ADR-006: Outbox and Event Reliability

**Date:** 2026-06-27
**Status:** Accepted
**Deciders:** Tech Lead

## Context

Domain events represent meaningful business facts (e.g., `BoardCreated`, `UserRegistered`). They must be dispatched reliably to enable:

- Cross-bounded-context side effects (workspace provisioning on user registration).
- External system notifications (webhooks, integrations).
- Search index updates.
- Audit trail recording.

Without an outbox pattern, dual-write problems can cause events to be lost or duplicated.

## Decision

### Event classification

| Type | Purpose | Persistence | Dispatch |
|------|---------|-------------|----------|
| `LocalDomainEvent` | In-process sync consistency | Not persisted | MediatR inline (after commit) |
| `DurableDomainEvent` | Important async internal side effect | Outbox table | Outbox dispatcher → MediatR notification |
| `IntegrationEvent` | Cross-boundary external contract | Outbox table | Outbox dispatcher → MassTransit publish |

Currently, all domain events in `DomainEventDispatchPolicy` use one of three modes: `Inline`, `Outbox`, or `Ignore`. The default is `Outbox`.

### Same-transaction persistence

`DomainEventInterceptor` (a `SaveChangesInterceptor`) captures domain events during `SaveChangesAsync`:

1. Extracts `IDomainEvent` from entity change tracker.
2. For `Outbox` mode events: serializes to `OutboxMessage` and adds to the `OutboxMessages` DbSet.
3. For `Inline` mode events: stores in `AsyncLocal<List>` for post-commit publish.
4. Clears domain events from entities.

This ensures outbox messages are committed atomically with business data — no dual-write problem.

### Outbox dispatcher

`OutboxDispatcher` (a `BackgroundService`) polls every 5 seconds:

1. Claims pending/failed messages using `FOR UPDATE SKIP LOCKED` (concurrency-safe).
2. Checks `ProcessedEvent` table for idempotency.
3. Deserializes and dispatches: domain events via MediatR, integration events via MassTransit.
4. On success: marks processed + writes `ProcessedEvent` record.
5. On failure: increments retry count with exponential backoff (2^n seconds, max 60s).
6. After max retries (5): moves to dead letter.

### Consumer idempotency

Each consumer records processed events in `ops.processed_events` with a unique constraint on `(EventId, ConsumerName)`. Before handling, the consumer checks if the event was already processed.

### Dead-letter observability

Dead-letter messages are visible via:
- `OutboxHealthCheck` (returns `Unhealthy` when dead-letter count ≥ threshold).
- `GET /admin/outbox/stats` endpoint.
- `MetricsService` gauges: `outbox.pending`, `outbox.failed`, `outbox.dead_letter`.

## Consequences

- Events are never lost after transaction commit.
- Duplicate delivery is handled by consumer idempotency.
- Dispatcher is horizontally scalable (FOR UPDATE SKIP LOCKED).
- Dead-letter requires operational attention — not silently ignored.

## Rejected alternatives

- **Direct publish from handlers:** Risk of dual-write (publish succeeds, commit fails or vice versa).
- **Message broker transactional outbox:** Deferred — modular monolith first, distributed later.
- **No outbox:** Unacceptable for enterprise reliability requirements.

## Verification

- Dispatch policy test: `AllDomainEvents_ShouldBeRegistered_InDispatchPolicy` (DispatchPolicyArchitectureTests.cs)
- Outbox reliability tests: pending in Slice 6
- Health check integration: `OutboxHealthCheck` registered with tags `["ready"]`
