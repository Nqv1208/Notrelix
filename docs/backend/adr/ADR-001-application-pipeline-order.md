# ADR-001: Application Pipeline Order

**Date:** 2026-06-27
**Status:** Accepted
**Deciders:** Tech Lead

## Context

The Notrelix Application layer uses MediatR with 11 pipeline behaviors registered for cross-cutting concerns. The execution order of these behaviors determines when authorization, caching, idempotency, transactions, and side effects occur. Incorrect ordering can cause:

- Side effects running before transaction commit (data inconsistency)
- Cache serving stale results (read-before-write)
- Idempotency storing results for failed operations
- Authorization running after data has already been loaded

## Decision

MediatR behaviors execute in registration order (outermost first on the way in, innermost first on the way out). The registered order is:

```text
ExceptionMappingBehavior        ← outermost: catches unhandled exceptions
  LoggingBehavior               ← logs entry/exit + elapsed time
    ValidationBehavior          ← FluentValidation
      WorkspaceContextBehavior  ← resolves workspace ID + verifies membership
        AuthorizationBehavior   ← evaluates IRequirePermission
          CacheBehavior         ← serves from cache for ICacheableQuery
            IdempotencyBehavior ← acquires dedup lock for IIdempotentRequest
              EntitlementBehavior ← plan gating for IRequireEntitlement
                CacheInvalidationBehavior ← after-commit cache eviction
                  RealtimeBehavior        ← after-commit SignalR publish
                    TransactionBehavior   ← innermost: DB transaction + SaveChanges
```

**Key invariant:** `TransactionBehavior` must always be the innermost behavior. This ensures that all other behaviors execute outside the transaction boundary, and post-commit side effects (cache invalidation, realtime publish) happen after `SaveChangesAsync` + `CommitAsync`.

### Why side effects run after commit

For a transactional command, the execution trace is:

```text
TransactionBehavior.Handle
  → BeginTransaction
  → next() → [handler executes]
  → SaveChangesAsync
  → CommitAsync
  → return response
RealtimeBehavior receives response → publishes (AFTER commit)
CacheInvalidationBehavior receives response → invalidates cache (AFTER commit)
```

MediatR unwinds the stack after `next()` returns, so behaviors registered OUTSIDE `TransactionBehavior` execute their post-`next()` code only after the transaction has committed.

## Consequences

- Cache invalidation and realtime publish are guaranteed to happen after durable commit.
- If `TransactionBehavior` were moved to a non-innermost position, side effects would fire before commit — this is tested by `PipelineBehaviorOrder_ShouldHaveTransactionBehaviorAsInnermost`.
- Side-effect failures (cache invalidation, realtime) are caught and logged but do not roll back committed data. This is intentional best-effort behavior.

## Rejected alternatives

- **After-commit hook model:** Would require a shared `IAfterCommitActionQueue` abstraction. Current outer-behavior model achieves the same guarantee with less infrastructure. Can be revisited if more than 2 side-effect behaviors need shared commit-awareness.
- **Outbox for all side effects:** Overkill for best-effort cache invalidation and realtime broadcast. Reserve outbox for durable integration events.

## Verification

- Architecture test: `PipelineBehaviorOrder_ShouldHaveTransactionBehaviorAsInnermost` (ApplicationArchitectureTests.cs)
- Pipeline-order execution tests: pending in Slice 3
