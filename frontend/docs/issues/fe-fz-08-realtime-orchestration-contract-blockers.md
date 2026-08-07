# FE-FZ-08 Realtime Orchestration Contract Blockers

## Status

`OPEN`

## Owner

Backend/API platform, realtime platform, then frontend module owners

## Phase

FE-FZ-08 — Realtime application orchestration and module adapters

## Summary

The frontend now has local realtime orchestration for application lifecycle:

- authenticated users connect using an auth snapshot generation value,
- anonymous users disconnect,
- active workspace is derived from workspace-scoped routes,
- workspace-scoped local realtime events dispatch through a module adapter registry,
- workspace sequence gaps trigger cache recovery invalidation,
- unknown event types are reported to runtime telemetry instead of crashing.

The full phase cannot be completed until backend/realtime contracts are confirmed and module event schemas exist.

## Blockers

### Authoritative Session Generation

Current frontend can only derive a fallback generation from the authenticated user id:

```text
user:{userId}
```

FE-FZ-08 requires a generation value that changes when:

```text
login account changes
session refresh generation changes
session is revoked
logout + login occurs
```

This requires a backend/session contract. User id alone is not enough to represent refresh/revoke generation.

### Server Subscription Protocol

Frontend local subscription exists, but server-side workspace subscribe/unsubscribe/resume depends on FE-FZ-07 protocol confirmation:

```text
subscribe
unsubscribe
resume
subscribed
resumed
resync-required
subscription-error
cursor
```

Until this is confirmed, frontend cannot honestly guarantee:

- Workspace A to B sends unsubscribe A before subscribe B.
- Reconnect restores subscriptions with cursor.
- Recovery resumes from an authoritative cursor.

### Module Event Contracts

Only a minimal workspace adapter exists. Work Management, Docs, Notifications and Activity event contracts are not yet confirmed enough to build production adapters that validate payloads and reconcile query cache.

Needed module contracts:

```text
eventType naming
payload schema
workspaceId isolation guarantees
aggregate id/version fields
correlationId echo policy
recovery snapshot endpoint per module
```

## Frontend State After Current FE-FZ-08 Work

Completed frontend-side work:

- `AuthContextType` exposes `sessionGeneration`.
- `RealtimeLifecycle` connects/disconnects from auth state.
- `RealtimeLifecycle` tracks active workspace from route pathname.
- Workspace-scoped realtime events dispatch through `ModuleAdapterRegistry`.
- Workspace recovery gaps invalidate workspace, members, abilities and unread-count caches.
- Unknown event types are reported to telemetry.
- Unit tests cover active workspace extraction and module registry dispatch/unhandled behavior.

## Required Follow-up

1. Add authoritative session generation to auth/profile/bootstrap response.
2. Confirm FE-FZ-07 server subscription/cursor protocol.
3. Implement server subscribe/unsubscribe/resume in realtime transport.
4. Add module event schemas and adapters for:

```text
Work Management
Docs
Notifications
Activity
```

5. Add deterministic tests for:

```text
login connects with backend generation
logout disconnects and does not reconnect
workspace A to B unsubscribes A before subscribing B
event workspace A cannot update workspace B cache
Work Management gap fetches authoritative board snapshot
unknown event type reports telemetry without crash
```
