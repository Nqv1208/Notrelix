# Application Execution Contract

> Frozen foundation. Changes require architecture review.

## Pipeline behavior order (outermost → innermost)

| # | Behavior | Zone |
|---|---|---|
| 1 | ExceptionMappingBehavior | Outer: exception normalization |
| 2 | ApplicationTracingBehavior | Outer: tracing/telemetry |
| 3 | ValidationBehavior | Outer: FluentValidation |
| 4 | RequestContractGuardBehavior | Outer: marker conflict detection |
| 5 | TokenValidationBehavior | Outer: token-scoped validation |
| 6 | TenantBootstrapBehavior | Outer: tenant context bootstrap |
| 7 | SystemOperationAuditBehavior | Outer: system operation audit |
| 8 | ResourceScopeBehavior | Outer: resource scope resolution |
| 9 | PostCommitScopeBehavior | Wraps DB scope; flushes after commit |
| 10 | PublicCacheBehavior | Cache-first for public queries |
| 11 | DbRequestScopeBehavior | DB/RLS/Transaction boundary |
| 12 | AuthorizationBehavior | Inner: permission checks |
| 13 | VerifiedEmailBehavior | Inner: email verification gate |
| 14 | ConcurrencyBehavior | Inner: optimistic version check |
| 15 | SubscriptionGateBehavior | Inner: subscription gate |
| 16 | FeatureGateBehavior | Inner: feature flag gate |
| 17 | IdempotencyBehavior | Inner: idempotency lock/replay |
| 18 | PostCommitEnqueueBehavior | Inner: enqueues side effects |
| 19 | AuthorizedCacheBehavior | Inner: private cache |

## Request execution profile

Every concrete MediatR request is classified by `RequestExecutionClassifier` into:

- **PrincipalKind**: Anonymous / Authenticated / System
- **ScopeKind**: Global / Account / Workspace / Resource / Token
- **DataAccess**: None / ReadOnly / Transactional
- **Flags**: Permission, VerifiedEmail, Subscription, Feature, ExpectedVersion, Idempotent, PublicCache, AuthorizedCache, Realtime

## Forbidden marker combinations

| Rule | Reason |
|---|---|
| Anonymous + SystemInternal | Contradictory principal |
| Global + TenantScoped | Contradictory scope |
| Global + RequiresPermission | No tenant for permission |
| RlsRead + !TenantScoped | RLS requires tenant context |
| TokenScoped + TenantScoped | Token proves identity via ownership |
| Anonymous + TenantScoped | No tenant for anonymous |
| PublicCache + TenantScoped | Public cache is tenant-free |
| PublicCache + AuthorizedCache | Mutually exclusive cache modes |
| AuthorizedCache + Realtime | Cache HIT skips handler, stale realtime |

## Post-commit semantics

Post-commit actions may perform:
- Cache invalidation
- Realtime notification
- Best-effort telemetry
- Enqueueing already-durable work

Post-commit actions must NOT be responsible for:
- Persisting business state
- Creating the only outbox record
- Completing idempotency
- Required financial operations

## New use-case convention

```
Features/<BoundedContext>/<Capability>/<UseCase>/
  Command.cs or Query.cs
  Handler.cs
  Validator.cs
  ResultDto.cs
```

- One use case per folder
- Application DTOs do not expose Domain entities
- New code must not be added to legacy root Commands/Queries folders

## Determinism

Domain business behavior must not read ambient process state.
Pass explicitly from Application: current time, generated identifiers,
random result/strategy, culture, environment-derived configuration.

This is a design and review rule, not a mathematically complete proof.
