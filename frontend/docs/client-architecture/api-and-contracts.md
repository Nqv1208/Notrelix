# API & Transport Contracts Specification

> **HTTP REST Client, Single-Flight Refresh, and Generated Contracts**

---

## 1. OpenAPI & AsyncAPI Contract Boundary

All REST and Realtime data transfers between Frontend and Backend are governed by authoritative contract artifacts:

- REST Contract Artifact: `artifacts/contracts/openapi.v1.json` -> `packages/foundation/contracts/src/generated/rest/`
- Realtime Contract Artifact: `artifacts/contracts/realtime.v1.json` -> `packages/foundation/contracts/src/generated/realtime/`

---

## 2. API Client Architecture

`createNotrelixClient` in `@notrelix/contracts` provides an instance-scoped HTTP client:

- **Instance-Closure Scope:** `refreshPromise` is kept inside closure scope, prohibiting global singleton mutations.
- **Single-Flight Refresh:** Concurrent 401 response bursts execute exactly one token refresh call.
- **Session Expiration Event:** Refresh failures emit a deterministic `SessionExpiredEvent` via `SessionEventBus`.
