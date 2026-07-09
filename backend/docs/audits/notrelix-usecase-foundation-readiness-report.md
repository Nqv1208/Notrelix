# Use Case Foundation Readiness Report

**Date:** 2026-07-04
**Scope:** Phases 1-6 of the Notrelix Use Case Foundation hardening plan.
**Status:** 🟢 Ready for production hardening gaps to be addressed incrementally.

---

## Phase Summary

| Phase | Status | Deliverables |
|-------|--------|-------------|
| 1 — Security Classification Contracts | ✅ Complete | `UseCaseSecurityKind`, 5 marker interfaces, 6 architecture tests, 105 command allowlist |
| 2 — Authorization Default-Deny | ✅ Complete | `SecurityMisconfigurationException`, `AuthorizationBehavior` rewritten (fail-closed), 12 behavior tests |
| 3 — RLS Fail-Closed | ✅ Complete | `RlsOptionsValidator`, environment-aware validation (Development warns, others fail), 7 validator tests |
| 4 — Cache Scope Contract | ✅ Complete | `CacheScope` enum, `CacheKeyBuilder`, 7 key builder tests, global using added |
| 5 — System Context Guardrails | ✅ Complete | `ISystemOperation`, `SystemOperationReason`, `SystemContextScope`, `SystemOperationAuditBehavior`, `SystemContextUsageTests`, admin endpoint policy fix, 5 admin endpoint authorization tests |
| 6 — Bounded DbContext Boundary | ✅ Complete | `DbContextBoundaryTests` (3 tests), allowlisted 4 known cross-context violations |
| 7 — Use Case Template & Rules | ✅ Complete | 5 rule/checklist documents in `docs/rules/` |
| 8 — CI Verification | ✅ Complete | All non-Docker tests pass |
| 9 — Readiness Report | ✅ Complete | This document |

---

## Test Suite Status

| Project | Tests | Status |
|---------|-------|--------|
| `Notrelix.Architecture.Tests` | 126 | ✅ All pass |
| `Notrelix.Application.Tests` | 52 | ✅ All pass |
| `Notrelix.API.Tests` | 26 | ✅ All pass |
| `Notrelix.Infrastructure.Tests` | 25 pass, 9 fail | ⚠️ Failures are pre-existing Docker-dependent tests |
| `Notrelix.Domain.Tests` | ~1250 | ✅ All pass (unchanged) |

Total non-Docker: ~1484 pass.

---

## Key Metrics

- **Commands/queries with security classification**: 31 of 136 (22.8%)
- **Commands in KnownUnclassified allowlist**: 105 (to be migrated over time)
- **Cross-context DbContext injections**: 4 (allowlisted — known)
- **IgnoreQueryFilters bypasses**: 3 (seed, access resolver, restore — all allowlisted)
- **[Pipeline behaviors](./notrelix-usecase-security-matrix.md)**: 13 registered
- **Bounded-context interfaces**: 12 (`IApplicationDbContext` + 11 scoped interfaces)
- **Admin endpoints**: 4 (`/admin/outbox/*`) — now protected by `SystemAdmin` policy

---

## Remaining Gaps

These are tracked in the KnownUnclassified allowlist and burn-down over time:

1. **67 RISK-level use cases** in the security audit — workspace-scoped commands without `IWorkspaceRequest` or `IRequirePermission`.
2. **3 public cacheable queries** (`GetBoardItemsQuery`, `GetBoardQuery`, `GetBoardSchemaQuery`) cache before auth check.
3. **25 handlers** bypass pipeline authorization by injecting `IWorkspacePermissionService` directly.
4. **0 commands implement** `IAccountRequest`, `IRequireFeature`, `IRequireSubscription`, `IIdempotentRequest`.

---

## How to proceed

1. **Short term**: Migrate KnownUnclassified commands to proper security markers (prioritize RISK-level).
2. **Short term**: Convert public cacheable queries to `IAuthorizedCacheableRequest`.
3. **Medium term**: Replace direct `IWorkspacePermissionService` usage with `IRequirePermission` on requests.
4. **Medium term**: Add `IAccountRequest` to account-scoped commands.
5. **Ongoing**: Use the [usecase checklist](../rules/notrelix-usecase-checklist.md) for every new use case PR.
