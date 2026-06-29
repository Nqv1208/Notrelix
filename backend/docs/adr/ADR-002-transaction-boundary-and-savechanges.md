# ADR-002: Transaction Boundary and SaveChanges

**Date:** 2026-06-27
**Status:** Accepted
**Deciders:** Tech Lead

## Context

Multiple code paths can call `SaveChangesAsync` on the DbContext: handlers, behaviors, interceptors, and background services. Without a clear convention, the system risks:

- Duplicate `SaveChangesAsync` calls (double flush)
- Missing `SaveChangesAsync` (lost changes)
- Partial commits (change saved without outbox persistence)
- Race conditions between inline saves and transaction behavior saves

## Decision

### Transaction ownership

`TransactionBehavior` is the sole owner of `SaveChangesAsync` for `ITransactionalRequest` commands. The behavior:

1. Begins a database transaction.
2. Calls `next()` → handler executes.
3. Calls `SaveChangesAsync` → flushes all pending EF changes.
4. Calls `CommitAsync` → commits the transaction.

### Handler rules

- **Command handlers must NOT call `SaveChangesAsync` directly.** They load aggregates, call aggregate methods, and let the transaction behavior persist changes.
- **Query handlers must NOT call `SaveChangesAsync`.** They are read-only.
- **Event handlers** (`IDomainEventHandler`) are the only exception — they may call `SaveChangesAsync` when processing inline domain events that need to be persisted atomically with additional state.

### Architecture enforcement

The architecture test `CommandHandlers_ShouldNotCall_SaveChangesAsync` scans all files containing `IRequestHandler<>` and flags any that reference `SaveChangesAsync`. Two event handlers are explicitly allowlisted:

- `MemberInvitedEventHandler.cs`
- `N8nAutomationEventHandlers.cs`

### Outbox persistence

`DomainEventInterceptor` intercepts `SaveChangesAsync` to capture domain events from the change tracker and write them as `OutboxMessage` records. This happens within the same transaction as business data, ensuring atomicity.

## Consequences

- Handlers are decoupled from persistence mechanics — they can be tested without a real database.
- Transaction boundaries are explicit and testable via architecture tests.
- New handlers cannot accidentally introduce double-save or partial-commit bugs.
- The architecture test allowlist must be reviewed and reduced over time.

## Rejected alternatives

- **Handler-managed transactions:** Would require every handler to begin/commit/rollback correctly — too error-prone.
- **No transaction behavior:** Would leave each handler responsible for persistence — inconsistent across codebase.
- **Implicit EF auto-save:** Would hide when changes are actually persisted — makes outbox integration unreliable.

## Verification

- Architecture test: `CommandHandlers_ShouldNotCall_SaveChangesAsync` (ApplicationArchitectureTests.cs)
- Handler tests: all integration tests verify behavior without direct SaveChanges calls
