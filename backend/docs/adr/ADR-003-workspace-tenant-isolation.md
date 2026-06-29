# ADR-003: Workspace Tenant Isolation

**Date:** 2026-06-27
**Status:** Accepted
**Deciders:** Tech Lead

## Context

Notrelix is a multi-tenant SaaS where `Workspace` is the primary tenant boundary. Every user resource (boards, documents, comments, memberships) belongs to a workspace. A data leak across workspaces would be a critical security incident.

The backend currently uses EF Core global query filters, application-level workspace resolution, and permission evaluation. These mechanisms must be layered correctly to prevent cross-tenant access.

## Decision

### Defense-in-depth layers

1. **API route/header resolution:** `WorkspaceResolutionMiddleware` extracts workspace ID from `{workspaceId}` route or `X-Workspace-Id` header. It verifies the authenticated user has `ViewWorkspace` permission.
2. **Application marker:** Requests implementing `IWorkspaceRequest` carry `WorkspaceId`. `WorkspaceContextBehavior` validates it is not `Guid.Empty` and sets the ambient workspace context.
3. **Permission evaluation:** `AuthorizationBehavior` evaluates `IRequirePermission` against the workspace and resource.
4. **EF global query filter:** `ApplicationDbContext.OnModelCreating` applies workspace-scoped query filters on all entities with a `WorkspaceId` property.
5. **Database constraints:** FK constraints ensure referential integrity within workspace scope.

### Workspace context resolution

`ICurrentWorkspace` is a scoped service that holds the resolved workspace ID. It is set by `WorkspaceContextBehavior` for `IWorkspaceRequest` and by `WorkspaceResolutionMiddleware` at the HTTP boundary.

### System context

Some operations need to access resources across all workspaces (e.g., admin queries, background jobs, seed data). These use `ICurrentWorkspace.EnterSystemContext()` which bypasses workspace-scoped query filters.

### Trusted vs client-provided workspace IDs

- Workspace ID from the **route or header** is validated against the user's membership via `WorkspaceResolutionMiddleware`.
- Workspace ID from the **request body** is NOT used for authorization — it is ignored or overridden by the resolved workspace.
- For workspace creation (`CreateWorkspace`), no pre-existing workspace context is required — this is a global operation.

## Consequences

- Cross-workspace data leaks are prevented at multiple layers.
- If query filters fail (e.g., InMemory provider in tests), permission checks still block access.
- System context operations must be explicitly opted-in via `EnterSystemContext()`.
- EF query filter behavior must be verified with integration tests (Slice 5).

## Rejected alternatives

- **RLS only:** Row-level security is defense-in-depth, not primary. Deferred until core stabilizes.
- **Handler-level workspace checks:** Too easy to forget — centralize in behaviors.
- **No query filters:** Relies entirely on application-level filtering — too risky.

## Verification

- `WorkspaceContextBehavior` enforces workspace validation.
- Architecture tests verify workspace markers on requests.
- Cross-tenant integration tests: pending in Slice 5.
- EF query-filter tests: pending in Slice 5.
