# FE-FZ-06 Workspace Membership Router Guard Blocker

## Status

`OPEN`

## Owner

Backend/API platform, then frontend router integration

## Phase

FE-FZ-06 — Router and authorization boundaries

## Summary

The frontend now has typed router context, route guard utilities, global route pending/error/not-found components, and a module-owned board search schema including `view`, `filter`, `sort`, `groupBy`, and `item`.

FE-FZ-06 cannot be fully completed yet because the required workspace membership snapshot cannot be loaded reliably before route component render. The current frontend source marks the workspace members backend contract as missing.

## Evidence

Current frontend source contains the blocker in:

```text
frontend/packages/features/workspace/src/core/api/members.service.ts
```

The service comments and runtime behavior indicate:

```text
GET /workspaces/{workspaceId}/members is pending backend validation.
Backend contract missing for workspaces.members
```

FE-FZ-06 requires workspace route `beforeLoad` to:

```text
Validate workspaceId.
Load membership snapshot via QueryClient ensureQueryData.
Set active workspace lifecycle.
Reject non-members before component render.
```

Only the workspaceId validation can currently be enforced honestly at route `beforeLoad`.

## Required Backend Work

1. Provide a stable membership snapshot endpoint suitable for router guards.
2. Endpoint must be workspace-scoped and return enough data to decide whether the current authenticated user is a member.
3. Recommended route shape:

```text
GET /api/v1/workspaces/{workspaceId}/membership
```

4. Response should include at minimum:

```text
workspaceId
userId
role
status
permissions or role-derived capability snapshot
```

5. Non-member behavior must be specified:
   - `404` if workspace existence must be hidden.
   - `403` if workspace existence may be revealed.

## Required Frontend Follow-up

After the backend contract exists:

1. Add a workspace membership query option factory that does not depend on React hooks.
2. Use `context.services.queryClient.ensureQueryData(...)` in workspace route `beforeLoad`.
3. Store a typed workspace snapshot in router context or an active workspace lifecycle service.
4. Replace render-time membership checks for workspace entry with route guard decisions.
5. Add tests for:
   - authenticated but non-member user,
   - member without permission,
   - member with permission but missing entitlement,
   - workspace switch cache scoping.

## Frontend State After Current FE-FZ-06 Work

Completed frontend-side work:

- Router moved to `apps/web/src/router/index.tsx` so guard utilities can live under `apps/web/src/router/guards`.
- Typed `AppRouterContext` added.
- Board route search schema moved to `apps/web/src/router/board-search-schema.ts` and now includes `groupBy`.
- Global pending/error/not-found route components are configured.
- Guard utilities added for auth, workspace id/membership, permission, entitlement, and feature flag checks.
- Unit tests cover guard utility failure modes and board search URL state fields.

Remaining blocker:

- Membership snapshot cannot be loaded in route `beforeLoad` without a confirmed backend/API contract.
