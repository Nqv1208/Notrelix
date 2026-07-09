# Pipeline Behavior Order

The MediatR pipeline behaviors execute in this order:

| # | Behavior | Marker/Trigger | Scope |
|---|---|---|---|
| 1 | `ExceptionMappingBehavior` | All requests | Outer |
| 2 | `ApplicationTracingBehavior` | All requests | Outer |
| 3 | `ValidationBehavior` | Requests with validator | Outer |
| 4 | `RequestContractGuardBehavior` | Requests implementing contract markers | Outer |
| 5 | `TenantBootstrapBehavior` | All requests | Outer |
| 6 | `ResourceScopeBehavior` | `IResourceScopedRequest` | Outer |
| 7 | `PostCommitScopeBehavior` | All requests (enable post-commit queue) | Outer |
| 8 | `PublicCacheBehavior` | `IPublicCacheableQuery` | Outer |
| 9 | `DbRequestScopeBehavior` | All requests (opens DB + RLS scope) | **DB boundary** |
| 10 | `AuthorizationBehavior` | `IRequirePermission` | Inside DB |
| 11 | `ConcurrencyBehavior` | `IExpectedVersionRequest` | Inside DB |
| 12 | `SubscriptionGateBehavior` | `IRequireSubscription` | Inside DB |
| 13 | `FeatureGateBehavior` | `IRequireFeature` | Inside DB |
| 14 | `IdempotencyBehavior` | `IIdempotentRequest` | Inside DB |
| 15 | `PostCommitEnqueueBehavior` | All requests (flush post-commit actions) | Inside DB |
| 16 | `AuthorizedCacheBehavior` | `IAuthorizedCacheableRequest` | Inside DB |

## Key Boundaries

- **Outer (1–8)**: No database connection. Validation, contract checks, tenant bootstrap, cache layer.
- **DB boundary (9)**: `DbRequestScopeBehavior` opens the database connection and sets RLS context.
- **Inside DB (10–16)**: All behaviors execute within the database scope, with RLS enforced.

## Notes

- `ConcurrencyBehavior` (11) runs after `AuthorizationBehavior` (10) — the resource must be authorized before its version is checked.
- `AuthorizedCacheBehavior` (16) runs last — it caches the handler response after all other behaviors have executed.
- Behaviors 12–16 run inside `DbRequestScope` which means they can safely depend on `ICurrentTenantContext`.
