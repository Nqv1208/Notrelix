# ADR-004: 5-Tier Rate Limiting Architecture

## Status

Accepted

## Context

Notrelix serves multiple client types (web SPA, mobile apps, API integrations, system workers) with different trust levels. A single rate limit policy cannot adequately protect all scenarios. Auth endpoints need strict per-IP limits, while API integrations need higher throughput with API key partitioning.

## Decision

Rate limiting is split across two layers with 5 tiers:

### API Layer (Middleware)

| Tier | Policy | Partition | Limit | Use Case |
|------|--------|-----------|-------|----------|
| 1. Anonymous | `GeneralAnonymous` | IP | 60/min | Public endpoints (unauthenticated) |
| 2. Sensitive | `AuthStrictByIp` | IP | 5/60s | Login, register, password reset |
| 3. Authenticated | `GeneralAuthenticated` | UserId | 300/min (token bucket) | Standard authenticated requests |
| 4. API Key | `ApiKeyAuthenticated` | ApiKey | 500/min (token bucket) | API integrations with `X-API-Key` header |

### Application Layer (Behavior)

| Tier | Policy | Partition | Use Case |
|------|--------|-----------|----------|
| 5. Per Tenant | Configurable | AccountId/WorkspaceId | Workspace-scoped operations |

### Implementation

- `PreAuthenticationRateLimitMiddleware` — Runs before auth, handles IP-based limits.
- `AuthenticatedRateLimitMiddleware` — Runs after auth, handles UserId/ApiKey-based limits.
- `TenantAwareRateLimitBehavior` — Application pipeline behavior for account/workspace limits.
- `RateLimitPolicyAttribute` — Endpoint metadata for per-route policy selection.

### Partition Keys

```
Ip         → context.Connection.RemoteIpAddress
UserId     → JWT "sub" claim
AccountId  → ICurrentTenantContext.AccountId (Application layer only)
WorkspaceId → ICurrentTenantContext.WorkspaceId (Application layer only)
ApiKey     → X-API-Key header value
```

### Why Two Layers?

- **API middleware** can reject requests before they reach the application pipeline (lower latency for rejection).
- **Application behavior** has access to tenant context (AccountId/WorkspaceId) which isn't available at the middleware level.
- Separation of concerns: transport-level vs business-level rate limiting.

### Why Token Bucket for Authenticated?

- Token bucket allows bursts (e.g., initial page load triggers multiple requests) while maintaining average rate.
- Sliding window for anonymous/sensitive endpoints provides stricter control.

## Consequences

- Each endpoint declares its rate limit policy via `RateLimitPolicyAttribute`.
- Default policies configured in `appsettings.json` under `RateLimiting:Policies`.
- Rate limit headers (`X-RateLimit-*`) added to responses via `ProblemDetailsWriter`.
- Fail mode: `Open` (allow through on infrastructure failure) or `Closed` (reject on failure).
