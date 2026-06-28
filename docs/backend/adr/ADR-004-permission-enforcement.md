# ADR-004: Permission Enforcement

**Date:** 2026-06-27
**Status:** Accepted
**Deciders:** Tech Lead

## Context

Notrelix has a resource-level permission system (`IRequirePermission`, `PermissionService`, `PermissionContext`). Commands and queries that mutate or read protected resources must declare their required permission via the `IRequirePermission` marker interface. `AuthorizationBehavior` evaluates this in the pipeline.

Without enforcement, a handler could forget to declare permissions, allowing unauthorized access.

## Decision

### Marker requirement

Every request that mutates or reads a protected resource must implement `IRequirePermission` with:

```csharp
PermissionAction Action { get; }     // e.g., ViewBoard, EditItem
ResourceRef Resource { get; }        // resource type + ID
```

`AuthorizationBehavior` evaluates the permission before the handler executes. If denied, it throws `ForbiddenException` or `NotFoundException` (to avoid leaking resource existence).

### When permission can be skipped

A request may skip `IRequirePermission` if:

- It is a **public query** (e.g., health check, bootstrap, public share link).
- It is a **system/background operation** running in system context.
- It operates on a **global resource** (not workspace-scoped).
- It is an **authentication flow** (login, register, refresh token) — identity is not yet established.

### Architecture enforcement

`CreateUpdateDeleteCommands_ShouldImplement_IRequirePermission` tests that all CRUD commands implement the marker. Known violations are tracked in a classified allowlist.

### Allowlist classification

Every allowlist entry must have:

- Request type name
- Classification: `Intentional`, `LegacyGap`, `FalsePositive`, `SystemCommand`, `PublicCommand`, `MigrationPending`
- Reason string
- Recommended target state

No new unclassified entries are allowed.

## Consequences

- Permission checks are centralized in `AuthorizationBehavior` — handlers don't need to check manually.
- The architecture test catches missing markers at build time.
- Allowlist reduction requires understanding each gap — not just removing entries blindly.
- Large current allowlist (57 commands) indicates a significant security gap that must be burned down.

## Rejected alternatives

- **Handler-level permission checks:** Easy to forget, inconsistent, not testable at architecture level.
- **Endpoint-level authorization only:** Insufficient — bypassed if handler is called directly (e.g., from a consumer or background job).
- **No permission system:** Unacceptable for enterprise SaaS.

## Verification

- Architecture test: `CreateUpdateDeleteCommands_ShouldImplement_IRequirePermission`
- Architecture test: `CommandsImplementingITransactionalRequest_WithWorkspaceId_ShouldAlsoImplement_IWorkspaceRequest`
- Integration tests: `PermissionServiceTests` (currently 8 failures — being investigated)
