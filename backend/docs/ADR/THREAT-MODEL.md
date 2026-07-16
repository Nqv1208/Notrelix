# Threat Model — Notrelix Backend

## Scope

This document identifies key threats to the Notrelix backend platform and maps mitigations to existing controls.

## Trust Boundaries

```
Internet → [CDN/Load Balancer] → [API Gateway] → [API Middleware Pipeline] → [Application Handlers] → [Infrastructure/DB]
```

1. **Internet ↔ API**: Transport security (HTTPS), rate limiting, CSRF protection, security headers
2. **API ↔ Application**: Authentication (JWT), authorization (pipeline behaviors), input validation
3. **Application ↔ Infrastructure**: RLS policies, EF Core parameterized queries, transaction isolation

## Threat Categories

### 1. Authentication & Identity

| Threat | Risk | Mitigation |
|--------|------|------------|
| JWT token theft | High | HttpOnly cookies, short expiry (15min), refresh token rotation |
| Brute force login | High | `AuthStrictByIp` rate limit (5 req/60s), `SecurityAuditMiddleware` logging |
| Account enumeration | Medium | Generic error messages, same response time for valid/invalid accounts |
| Session fixation | Medium | New session ID on login, `SameSite=Strict` cookies |
| OAuth state manipulation | High | `OAuthLoginState` with `state` + `nonce` + `code_verifier` (PKCE) |

### 2. Authorization & Access Control

| Threat | Risk | Mitigation |
|--------|------|------------|
| Horizontal privilege escalation | Critical | RLS policies enforce workspace/account isolation at DB level |
| Vertical privilege escalation | High | `AuthorizationBehavior` checks `PermissionLevel` via `IPermissionEvaluator` |
| Cross-tenant data access | Critical | RLS `app.current_account_id` session variable, checked by every policy |
| Permission inheritance bypass | High | `ResourcePermissionInheritanceCache` precomputed, checked in `PermissionService` |

### 3. Injection Attacks

| Threat | Risk | Mitigation |
|--------|------|------------|
| SQL injection | Critical | EF Core parameterized queries, no raw SQL in handlers |
| NoSQL injection | N/A | PostgreSQL only |
| XSS (stored) | High | Output encoding in frontend, CSP headers |
| Command injection | Low | No shell commands in application code |

### 4. CSRF & Session Attacks

| Threat | Risk | Mitigation |
|--------|------|------------|
| Cross-site request forgery | High | Double Submit Cookie pattern (`CsrfValidationMiddleware`), `SameSite=Strict` |
| Cross-site scripting (XSS) | High | CSP headers, `X-Content-Type-Options: nosniff`, HttpOnly cookies |
| Clickjacking | Medium | `X-Frame-Options: DENY`, CSP `frame-ancestors 'none'` |

### 5. Data Exposure

| Threat | Risk | Mitigation |
|--------|------|------------|
| Sensitive data in logs | Medium | Structured logging, no secrets in log output |
| Verbose error messages | Medium | `GlobalExceptionHandler` returns problem details, no stack traces |
| Server information leakage | Low | `X-Powered-By` removed, `Server` header suppressed |
| Response body caching | Medium | `Cache-Control: no-store` on authenticated responses |

### 6. Infrastructure

| Threat | Risk | Mitigation |
|--------|------|------------|
| Connection pool exhaustion | High | Scoped DbContext, `FOR UPDATE SKIP LOCKED` with explicit transaction |
| RLS bypass | Critical | `TenantBootstrapStore` sets session context before queries, `DbRequestScopeBehavior` overrides |
| Denial of service | Medium | 5-tier rate limiting (IP, User, Sensitive, Tenant, API Key) |
| Supply chain attack | Medium | Dependency scanning, pinned versions |
| Compromised admin | High | Audit log (`SecurityEvent`), workspace-scoped permissions, transfer workflow |

### 7. Concurrency

| Threat | Risk | Mitigation |
|--------|------|------------|
| Lost updates | High | `ConcurrencyBehavior` optimistic concurrency (ETag/If-Match) |
| Race conditions in outbox | High | `FOR UPDATE SKIP LOCKED` + explicit transaction in `OutboxDispatcher` |
| Duplicate idempotent requests | Medium | `IdempotencyBehavior` lock + post-commit result storage |

## Security Controls Summary

### API Layer (Middleware)
- `SecurityHeadersMiddleware` — Permissions-Policy, COEP, COOP, CSP, X-Frame-Options
- `CsrfValidationMiddleware` — Double Submit Cookie (feature-flagged)
- `SecurityAuditMiddleware` — Logs 401/403/429 to `SecurityEvent` table
- `PreAuthenticationRateLimitMiddleware` — Anonymous/IP rate limiting
- `AuthenticatedRateLimitMiddleware` — User/API Key rate limiting

### Application Layer (Behaviors)
- `TokenValidationBehavior` — Token presence validation for token-scoped requests
- `TenantBootstrapBehavior` — Workspace access + IsWorkspaceActive check
- `AuthorizationBehavior` — Permission level enforcement
- `ConcurrencyBehavior` — Optimistic concurrency
- `IdempotencyBehavior` — Duplicate request prevention (post-commit)
- `SystemOperationAuditBehavior` — System operation logging

### Infrastructure Layer
- `TenantBootstrapStore` — RLS session context setup before bootstrap queries
- `RlsSessionContext` — Full RLS context for transaction-scoped queries
- `OutboxDispatcher` — Transaction-wrapped dispatch with `FOR UPDATE SKIP LOCKED`
- `CookieService` — Secure cookie settings (HttpOnly, SameSite, Secure)

## Open Items

- [ ] Secret rotation: Move to deployment-level rotation (ADR-004 pending)
- [ ] API key lifecycle: Key generation, rotation, revocation
- [ ] Rate limit headers: Add `X-RateLimit-*` headers to all responses
- [ ] Audit log retention: Configure retention policy for `SecurityEvent` and `AuditLog`
