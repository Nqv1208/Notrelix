# Slice 4 — After-Commit Model: Keep Pipeline Ordering

**Date:** 2026-06-28
**Decision:** Keep current MediatR pipeline ordering as after-commit mechanism
**Status:** Approved

---

## Context

Phase 2 §2.2 of the enterprise roadmap proposed replacing the current pipeline-based after-commit ordering with an explicit `IAfterCommitActionQueue` abstraction. Slice 3 tests proved the current pipeline ordering already guarantees side effects execute after transaction commit.

## Decision

Keep the current MediatR pipeline ordering. No new abstractions.

## Rationale

### How it works today

MediatR behaviors execute outermost-first on the way in, innermost-first on the way out:

```
Validation → WorkspaceContext → Authorization → Cache → Idempotency → Entitlement → CacheInvalidation → Realtime → Transaction → Handler
```

`TransactionBehavior` is innermost: it wraps the handler. When it commits and returns, control flows back to `RealtimeBehavior` (which then publishes), then to `CacheInvalidationBehavior` (which then invalidates). This is correct by construction.

### Why explicit queue was rejected

1. **No benefit** — Slice 3 proves the ordering is already correct
2. **Added complexity** — new interfaces (`IAfterCommitAction`, `IAfterCommitActionQueue`), new wiring, new tests
3. **More moving parts** — queue needs error handling, ordering guarantees, DI registration
4. **Pipeline ordering is MediatR's design** — fighting it adds maintenance cost

### What protects against regression

- `PipelineExecutionTests` — 21 tests proving behavior ordering
- `ApplicationArchitectureTests` — registration order validation
- New architecture tests added in this slice

## Scope

### New files

| File | Purpose |
|------|---------|
| `docs/backend/adr/ADR-007-after-commit-side-effect-model.md` | Formal decision record |

### New tests in `PipelineExecutionTests.cs`

| Test | Proves |
|------|--------|
| `Pipeline_SideEffectsCannotRunBeforeCommit` | Cache/realtime mock calls happen after transaction mock commit |
| `Pipeline_TransactionCommitHappensBeforeSideEffects` | `CommitAsync` called before `RemoveAsync`/`PublishAsync` |

### Updated files

| File | Change |
|------|--------|
| `docs/backend/notrelix-backend-enterprise-hardening-report.md` | Mark Slice 4 complete |

## Verification

1. All 49 architecture tests pass
2. All 51 application tests pass (including new after-commit tests)
3. ADR-007 documents the decision
