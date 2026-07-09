# Caching Rules

## 1. Cache is a security boundary

In a multi-tenant SaaS, cache key mistakes can leak data.

Therefore:

```txt
Queries must not build raw cache keys.
Cache key dimensions must be enforced by CacheKeyFactory.
Authorized cache must run only after authorization.
Public cache must never be tenant-scoped.
```

## 2. Public cache

Use `IPublicCacheableQuery` only for data that is:

```txt
Not tenant-scoped
Not user-specific
Not permission-specific
Safe for all users
```

Examples:

```txt
Public plan catalog
Static country list
Public app metadata
```

Forbidden:

```txt
Board data
Workspace settings
User profile
Billing subscription
Permissioned resources
```

## 3. Authorized cache

Use `IAuthorizedCacheableRequest` for private data.

Request declares:

```txt
AuthorizedCacheScope CacheScope
object CacheIdentity
TimeSpan? CacheTtl
```

Do not expose raw `CacheKey` property.

## 4. Cache scopes

### Account

Key requires:

```txt
accountId
requestName
requestHash
```

### Workspace

Key requires:

```txt
accountId
workspaceId
requestName
requestHash
```

### User

Key requires:

```txt
accountId
workspaceId
userId
requestName
requestHash
```

### Permissioned

Key requires:

```txt
accountId
workspaceId
userId
permissionVersion
requestName
requestHash
```

Permission version must come from `IPermissionVersionProvider`.

## 5. Permissioned cache rules

Never use:

```txt
default
unknown
v1
static string
```

as permission version.

Version string should include:

```txt
accountId
workspaceId
userId
real permission version stamp
```

Example:

```txt
perm:{accountId}:{workspaceId}:{userId}:{ticks}
```

Version must change when any of these change:

```txt
Workspace membership
Role assignment
Role permission
Resource permission
Permission inheritance version
User disabled/removed from workspace
```

## 6. CacheIdentity rules

Good:

```csharp
public sealed record GetBoardCacheIdentity(Guid BoardId, bool IncludeFields);
public object CacheIdentity => new GetBoardCacheIdentity(BoardId, IncludeFields);
```

Bad:

```csharp
public object CacheIdentity => this;
public string AuthorizedCacheKey => $"board:{BoardId}";
```

Rules:

```txt
Use record/POCO identity for non-trivial queries.
Only include data that changes response.
Do not include account/workspace/user if behavior already includes those dimensions.
Sort collections if order is not meaningful.
Avoid Dictionary unless canonical serializer sorts keys.
```

## 7. Authorized cache pipeline rule

Authorized cache hit must happen after permission check.

Test required:

```txt
User A authorized -> cache populated.
User B denied same resource -> does not get cached response.
```

## 8. Cache invalidation

If TTL-only strategy is active:

```txt
Document allowed stale window.
Do not promise immediate read-after-write freshness from cached query.
```

If explicit invalidation is added later:

```txt
Invalidation must run after commit, never before commit.
```
