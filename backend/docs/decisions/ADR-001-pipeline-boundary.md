# ADR-001: Pipeline Boundary Zones

## Status

Accepted

## Context

The MediatR pipeline in `Notrelix.Application` processes behaviors sequentially. Without explicit zone boundaries, behaviors can be accidentally reordered, causing runtime failures (e.g., RLS applied before tenant context is set, or post-commit side effects running inside a failed transaction).

The pipeline has evolved to 19 behaviors. Without a documented zone model, new behaviors are added at arbitrary positions, creating fragile implicit dependencies.

## Decision

We define **6 pipeline zones** with clear entry/exit conditions. Behaviors within a zone are order-independent relative to each other (unless noted). Zone transitions are hard boundaries enforced by architecture tests.

### Zone Model

```
┌─────────────────────────────────────────────────────┐
│ OUTER ZONE — Pre-DB, No Transaction                │
│                                                     │
│  ExceptionMappingBehavior    (outermost)            │
│  TracingBehavior                                    │
│  ValidationBehavior                                 │
│  RequestContractGuardBehavior                       │
│  TokenValidationBehavior                            │
│  TenantBootstrapBehavior                            │
│  SystemOperationAuditBehavior                       │
│  ResourceScopeBehavior                              │
│                                                     │
├─────────────────────────────────────────────────────┤
│ BOUNDARY — PostCommitScope                          │
│                                                     │
│  PostCommitScopeBehavior    (creates scope)         │
│  PublicCacheBehavior         (reads before DB)      │
│                                                     │
├─────────────────────────────────────────────────────┤
│ INNER ZONE — Inside DB Transaction                  │
│                                                     │
│  DbRequestScopeBehavior     (opens connection)      │
│  AuthorizationBehavior                              │
│  VerifiedEmailBehavior                              │
│  ConcurrencyBehavior                                │
│  SubscriptionGateBehavior                           │
│  FeatureGateBehavior                                │
│  IdempotencyBehavior                                │
│                                                     │
├─────────────────────────────────────────────────────┤
│ POST-COMMIT ZONE — After Transaction                │
│                                                     │
│  PostCommitEnqueueBehavior   (enqueues actions)     │
│                                                     │
├─────────────────────────────────────────────────────┤
│ CACHE ZONE — After Transaction                      │
│                                                     │
│  AuthorizedCacheBehavior     (writes cache)         │
│                                                     │
└─────────────────────────────────────────────────────┘
```

### Zone Rules

1. **Outer → Boundary**: `PostCommitScopeBehavior` creates the post-commit scope. Everything before it runs pre-transaction.

2. **Boundary → Inner**: `DbRequestScopeBehavior` opens the database connection and begins the transaction. Everything after it runs inside the transaction.

3. **Inner → Post-Commit**: The transaction commits before `PostCommitEnqueueBehavior` runs. Post-commit actions (outbox dispatch, idempotency side effects) execute after successful commit.

4. **Post-Commit → Cache**: `AuthorizedCacheBehavior` runs last. It can safely cache because the transaction committed and post-commit actions are enqueued.

5. **No backward dependencies**: A behavior in an outer zone must never depend on state set by an inner zone behavior.

6. **New behaviors**: Must be placed in the correct zone. If uncertain, place in the outer zone and request review.

### Zone Responsibilities

| Zone | Responsibilities |
|------|-----------------|
| Outer | Input validation, request classification, tenant context, tracing, exception mapping |
| Boundary | Post-commit scope creation, public cache read |
| Inner | DB access, authorization, business rules, concurrency, feature gates |
| Post-Commit | Side effects that must survive (outbox, idempotency tokens) |
| Cache | Response caching (must be last) |

## Consequences

- Architecture tests enforce zone order at build time
- New behaviors must declare their zone via comments or positioning
- Zone violations are caught by existing `PipelineRuntimeOrderTests`
- The `PostCommitScope` boundary is the critical inflection point: before = no DB, after = inside transaction

## Related

- `PipelineRuntimeOrderTests.cs` — runtime verification of 19-behavior order
- `ApplicationArchitectureTests.cs` — compile-time order verification
- `PostCommitScopeBehavior.cs` — creates the post-commit scope boundary
- `DbRequestScopeBehavior.cs` — opens DB connection and transaction
