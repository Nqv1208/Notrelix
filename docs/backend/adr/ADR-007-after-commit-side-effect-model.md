# ADR-007: After-Commit Side-Effect Model

**Status:** Accepted
**Date:** 2026-06-28
**Deciders:** Tech Lead

---

## Context

Enterprise systems must guarantee that external or observable side effects (cache invalidation, realtime publishing, email, webhooks) do not execute before the database transaction is durable. Phase 2 §2.2 of the enterprise roadmap proposed replacing the current pipeline-based ordering with an explicit `IAfterCommitActionQueue` abstraction.

Slice 3 tests proved the current MediatR pipeline ordering already guarantees this invariant.

## Decision

Keep the current MediatR pipeline ordering as the after-commit mechanism. Do not introduce an explicit action queue.

## How it works

MediatR behaviors execute outermost-first on the way in, innermost-first on the way out:

```
Validation → WorkspaceContext → Authorization → Cache → Idempotency → Entitlement → CacheInvalidation → Realtime → Transaction → Handler
```

`TransactionBehavior` is innermost. When it commits and returns, control flows back through the stack:

1. `TransactionBehavior` calls `SaveChangesAsync` + `CommitAsync`
2. Returns to `RealtimeBehavior` — executes `PublishAsync`
3. Returns to `CacheInvalidationBehavior` — executes `RemoveAsync`
4. Returns to `EntitlementBehavior`
5. ...and so on outward

Side effects execute after commit by construction.

## Consequences

### Positive

- No new abstractions required
- Pipeline ordering is MediatR's native mechanism
- Slice 3 tests (21 tests) prove the ordering
- Architecture tests prevent regression

### Negative

- Ordering depends on DI registration sequence
- If someone reorders behaviors in `DependencyInjection.cs`, the invariant breaks
- Mitigated by architecture tests that validate registration order

## Rejected alternatives

### Explicit IAfterCommitActionQueue

```csharp
public interface IAfterCommitAction
{
    Task ExecuteAsync(CancellationToken cancellationToken);
}
```

Rejected because:
- No benefit over current working approach
- Adds new interfaces, DI wiring, error handling
- More moving parts = more failure modes

### TransactionBehavior-owned action list

TransactionBehavior collects side-effect delegates during pipeline, executes after commit.

Rejected because:
- Couples TransactionBehavior to knowledge of side effects
- Breaks single-responsibility principle
- Current approach lets each behavior own its own side-effect logic

## Verification

- `PipelineExecutionTests.FullPipeline_SideEffectsRunAfterTransaction`
- `PipelineExecutionTests.Pipeline_SideEffectsCannotRunBeforeCommit`
- `PipelineExecutionTests.Pipeline_TransactionCommitHappensBeforeSideEffects`
