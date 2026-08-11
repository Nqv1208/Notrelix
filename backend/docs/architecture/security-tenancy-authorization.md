# Security, Tenancy, and Authorization

## Scope

Threat boundaries, authentication vs authorization, account/workspace/resource
scope, Application authorization, RLS, tenant context propagation, cache
isolation, background consumer scope, CSRF/rate limiting, secrets, audit/activity
and permission invalidation.

## Responsibility / Ownership

Security is enforced across API, Application, Infrastructure and Platform. No
frontend affordance or database-only mechanism is sufficient by itself.

## Current Architecture

Authentication enters through API host integration. Authorization and resource
scope are Application concerns. RLS and persistence scoping are Infrastructure
defense in depth. Background execution must carry scope through Platform.

## Normative Contracts

- Authentication identifies the subject/session; authorization decides allowed
  actions for a resource.
- Account, workspace, resource and user scope must be explicit at public
  boundaries and immutable through persistence, cache, realtime and messaging.
- Application authorization is authoritative for protected reads and writes.
- RLS complements Application authorization and must fail closed.
- Tenant context propagation is required for request, background and consumer
  execution that touches tenant data.
- Cache keys for protected data include permission/resource/tenant scope and are
  invalidated on relevant permission/version changes.
- CSRF and rate-limit behavior are governed by accepted ADRs and host config.
- Secrets remain out of Domain, events, logs and client-visible contracts.
- Audit facts are append-only accountability records; activity/projections are
  product/read models and may have different retention semantics.

## Allowed Design

- Centralized authorization services/policies used by Application.
- RLS/session context helpers in Infrastructure.
- Security tests that verify request path, persistence isolation and background
  scope.

## Forbidden Design

- Trusting client-supplied tenant, role, membership or entitlement facts.
- Hidden-button frontend authorization as security proof.
- Global events/messages for tenant-scoped facts.
- Cache reuse across users/workspaces because IDs look globally unique.
- Raw secrets/tokens in Domain events or logs.

## Failure Modes

- Cross-tenant observation through list/search/cache/realtime.
- Background consumers run without tenant/RLS context.
- Permission changes leave stale cache entries.
- API endpoint bypasses Application authorization.

## Change Impact Rules

Auth, tenant scope, RLS, cache isolation, background scope, CSRF, rate limiting or
secret-handling changes require security-focused tests and architecture review.

## Executable Evidence / Tests / Gates

- Application authorization tests
- Infrastructure/RLS tests
- API authentication/CSRF/rate-limit tests
- Architecture tests for boundary violations

## Related ADRs

- `../decisions/ADR-002-rls-bootstrap-connection-lifecycle.md`
- `../decisions/ADR-003-csrf-protection.md`
- `../decisions/ADR-004-rate-limiting-architecture.md`

## Related Source Manifests

`backend/backend.slnx`, auth/RLS source, migrations and API host config.

## Non-responsibilities

This document does not define UI permission presentation or product-specific
role copy.
