# Authorized Cache

## Overview

The authorized cache system provides cache-aside caching for both public and authorized (permission-scoped) data. It consists of two pipeline behaviors:

- `PublicCacheBehavior` — caches responses for `IPublicCacheableQuery` requests
- `AuthorizedCacheBehavior` — caches responses for `IAuthorizedCacheableRequest` requests with permission-scoped versioning

## Cache Scopes

| Scope | Marker | Versioning | Use Case |
|---|---|---|---|
| Public | `IPublicCacheableQuery` | None (global key) | Public/global data, reference data |
| Workspace | `IAuthorizedCacheableRequest` + `AuthorizedCacheScope.Workspace` | Account + Workspace + User | Workspace-scoped queries |
| Account | `IAuthorizedCacheableRequest` + `AuthorizedCacheScope.Account` | Account + User | Account-level settings |
| User | `IAuthorizedCacheableRequest` + `AuthorizedCacheScope.User` | User | User preferences |
| Permissioned | `IAuthorizedCacheableRequest` + `AuthorizedCacheScope.Permissioned` | Account + Workspace + User + Permission version | Permission-sensitive queries |

## Permission Versioning

`IPermissionVersionProvider` computes a version string that changes when any permission or membership state changes for a user in a workspace.

Version format: `perm:{accountId}:{workspaceId}:{userId}:{maxUpdateTicks}`

The provider queries `MAX(updated_at)` across these tables:
- `workspace.workspace_members`
- `governance.member_role_assignments`
- `governance.custom_roles`
- `governance.resource_permissions`
- `governance.permission_rules`

Each query filters by `account_id`, `workspace_id`, and (for workspace_members) `user_id`.

## Cache Behavior

1. Request arrives with cache scope and optional TTL
2. For authorized scopes, behavior resolves current user and tenant context
3. Behavior computes cache key from scope + resource identifiers + permission version (if Permissioned)
4. Cache hit → return cached response (re-validated against authorization)
5. Cache miss → execute handler, cache response, return

## Rules

- Permissioned scope requires a valid `accountId`, `workspaceId`, `userId`, and `IPermissionVersionProvider`
- No cache key should bypass authorization — all cached authorized responses must still pass authorization check
- Never use `"default"` or `"unknown"` as permission version
- Cache invalidation is scope-aware — changing a resource permission invalidates the permissioned cache for all users
