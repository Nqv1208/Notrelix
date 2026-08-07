# FE-FZ-07 Realtime Protocol and Authentication Confirmation

## Status

`OPEN`

## Owner

Backend/API platform and realtime platform

## Phase

FE-FZ-07 — Realtime protocol and transport state machine

## Summary

The frontend has started FE-FZ-07 by removing browser WebSocket construction from foundation realtime, moving the browser socket factory to `runtime-web`, and hardening sequence handling so stale events and sequence gaps are not applied to subscribers.

The phase cannot be completed honestly until backend/realtime protocol decisions are confirmed. The plan explicitly requires a decision between SameSite/HttpOnly cookie handshake and short-lived single-use realtime tickets, plus server support for subscription/resume/resync control messages.

## Decisions Needed

### Authentication

Choose and document one supported realtime authentication mode:

1. Same-origin HttpOnly auth cookie for WebSocket handshake.
2. Short-lived single-use realtime ticket obtained over HTTPS before connecting.

The plan defaults to realtime ticket if product web and API are different origin. Current frontend must not invent this server contract without confirmation.

### Control Protocol

Confirm server support and exact wire shape for:

```text
subscribe
unsubscribe
resume
subscribed
resumed
resync-required
subscription-error
```

Required frontend behavior depends on these server messages:

- send subscription frame after local subscribe,
- restore subscriptions after reconnect,
- persist/use cursor per subscription,
- surface resync-required to app orchestration,
- reject invalid control messages without crashing listener loops.

## Frontend State After Current FE-FZ-07 Work

Completed frontend-side work:

- `@notrelix/realtime` foundation no longer constructs browser WebSocket by default.
- Browser WebSocket construction now lives in:

```text
frontend/packages/runtimes/web/src/realtime/browser-websocket-factory.ts
```

- `createAppRuntime` injects the browser socket factory into `RealtimeClient`.
- Sequence policy now:
  - ignores stale or duplicate sequence values,
  - detects gaps,
  - emits recovery notification,
  - does not apply gap events to subscribers,
  - does not move the sequence tracker forward on gaps.

## Required Follow-up

After backend/realtime decisions are confirmed:

1. Add a realtime connection descriptor provider matching the selected auth mode.
2. Extend control message types and validation to the confirmed wire contract.
3. Send subscribe/unsubscribe/resume frames from transport.
4. Restore subscriptions and cursors after reconnect.
5. Surface `resync-required` into application orchestration.
6. Add deterministic fake scheduler/socket tests for reconnect, heartbeat, subscription restore, resync, dispose cleanup, and wrong-workspace filtering.

## Current Verification

Current frontend verification passes:

```bash
rtk tsc --noEmit --project packages/foundation/realtime/tsconfig.json
rtk tsc --noEmit --project packages/runtimes/web/tsconfig.json
rtk pnpm test:node
```
