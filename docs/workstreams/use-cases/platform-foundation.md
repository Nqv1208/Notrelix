---
document_id: WRK-UC-PLATFORM-FOUNDATION
document_type: workstream-use-case-catalog
status: active
owner: platform-foundation-team
applies_to:
  - platform
  - foundation
  - system-use-cases
  - use-case-catalog
  - capability-acceptance
evidence:
  - docs/workstreams/use-cases/README.md
  - docs/workstreams/teams/platform-foundation.md
  - docs/workstreams/executions/platform-foundation/platform-foundation.spec.md
  - docs/workstreams/executions/platform-foundation/platform-foundation.tests.md
  - docs/workstreams/executions/platform-foundation/platform-foundation.certification.md
  - docs/workstreams/executions/backend-team-architecture-closure/backend-team-architecture-closure.spec.md
  - docs/workstreams/executions/backend-team-architecture-closure/backend-team-architecture-closure.certification.md
  - docs/architecture/events-realtime-and-delivery-boundary.md
  - docs/architecture/data-ownership-and-consistency.md
  - backend/docs/architecture/platform-and-messaging.md
  - backend/src/Notrelix.API/Endpoints/Health/
  - backend/src/Notrelix.Platform/
  - backend/src/Notrelix.Infrastructure/Messaging/
  - frontend/packages/foundation/
  - frontend/packages/runtimes/
review_on:
  - messaging-delivery-change
  - tenancy-context-change
  - realtime-foundation-change
  - frontend-runtime-change
  - health-or-observability-change
  - capability-state-change
---

# Use cases: Platform and Foundation (system use cases)

Product meaning: Platform has no product bounded context (`PFREQ001`). Its meaning comes from [system overview](../../architecture/system-overview.md), [events, realtime and delivery boundary](../../architecture/events-realtime-and-delivery-boundary.md), [data ownership and consistency](../../architecture/data-ownership-and-consistency.md) and [platform and messaging](../../../backend/docs/architecture/platform-and-messaging.md). Team: [platform-foundation](../teams/platform-foundation.md). Execution packs: [spec](../executions/platform-foundation/platform-foundation.spec.md), [tests](../executions/platform-foundation/platform-foundation.tests.md), [certification](../executions/platform-foundation/platform-foundation.certification.md), and the backend closure [spec](../executions/backend-team-architecture-closure/backend-team-architecture-closure.spec.md) and [certification](../executions/backend-team-architecture-closure/backend-team-architecture-closure.certification.md). Context code: PLT. Requirement ids are `PFREQ001` to `PFREQ128`; message rules are the `BE-PLT` rules of the platform doc, written `BE-PLT-0nn`.

Actors here are the system, operators, background workers and the frontend runtime. A product user appears only where a mechanism is visible to them (sign-in, a stale screen).

# 1. Assessment

## 1.1 Capability map

Letters follow the capability lanes of the team spec (section 6): PF-01 is A, PF-02 is B, and so on to PF-16 which is P.

| Letter | Capability | Use cases | DEFINED | IMPLEMENTING | INTEGRATING | HARDENING | DONE | BLOCKED |
|---|---|---|---|---|---|---|---|---|
| A | Session and CSRF transport | 3 | 0 | 0 | 1 | 2 | 0 | 0 |
| B | Actor/account/workspace context propagation | 4 | 0 | 1 | 1 | 2 | 0 | 0 |
| C | Authorization enforcement pipeline | 3 | 0 | 1 | 0 | 2 | 0 | 0 |
| D | Persistence foundation (data session, RLS, cache) | 4 | 0 | 3 | 0 | 1 | 0 | 0 |
| E | Migration, runtime initialization and configuration | 3 | 0 | 3 | 0 | 0 | 0 | 0 |
| F | Idempotency | 2 | 0 | 2 | 0 | 0 | 0 | 0 |
| G | Outbox/inbox, message identity, background jobs | 8 | 0 | 6 | 0 | 2 | 0 | 0 |
| H | Ordered delivery and poison handling | 5 | 2 | 2 | 0 | 0 | 0 | 1 |
| I | Realtime connection and recovery | 5 | 0 | 3 | 2 | 0 | 0 | 0 |
| J | API, OpenAPI and generated contracts | 3 | 0 | 1 | 0 | 2 | 0 | 0 |
| K | Frontend query/server-state foundation | 3 | 0 | 1 | 0 | 2 | 0 | 0 |
| L | Frontend runtime and host composition | 4 | 1 | 2 | 0 | 1 | 0 | 0 |
| M | UI tokens and primitive foundation | 1 | 0 | 1 | 0 | 0 | 0 | 0 |
| N | Observability and health | 7 | 1 | 6 | 0 | 0 | 0 | 0 |
| O | Architecture/dependency enforcement | 1 | 0 | 1 | 0 | 0 | 0 | 0 |
| P | CI, container, security and packaging evidence | 2 | 0 | 2 | 0 | 0 | 0 | 0 |
| | Total | 58 | 4 | 35 | 4 | 14 | 0 | 1 |

No use case is DONE: none has an owner check, and design fidelity does not apply to most. State rules used here: HARDENING means a `frontend/e2e/real` spec exercises the mechanism, directly or because every real spec signs in (so sign-in, CSRF, context, authorization, idempotent writes and the outbox are exercised by all of them); IMPLEMENTING means the mechanism exists with its own unit, integration or architecture tests but no real spec shows it from outside, including tooling gates that are not browser subjects; INTEGRATING means backend and frontend both exist but end-to-end proof or a state is missing. Design columns are "n/a" for mechanisms without a screen.

## 1.2 Source posture today

The spec's preparation-time posture table (spec section 5) is a snapshot; where the source has moved, this catalog follows the source:

- Stale `Processing` claim recovery now exists: `MessageDeduplicationStore.TryReclaimStaleProcessingAsync`, `DeduplicationConsumeFilter` and the outbox claim lease (`OutboxClaimReclaimTests`). The spec lists it as the remaining PF-07 gap.
- The `@notrelix/ui-tokens` `./css` export now resolves: `frontend/packages/ui/tokens/src/css/index.css` and `build-css.ts` exist. The spec lists the absent path as a PF-13 gap.
- Ordering: no consumer is `OrderingRequired`. Every production consumer carries `OrderingNotApplicableAttribute` (an Architecture test enforces it). `Platform.ConsumerHost`, `OrderingEnforcer` and `PoisonDetector` are tested mechanism but are not in the production delivery graph (MassTransit plus the dedup filter). The outbox orders by stream version at claim time (`Claim_FailedEarlierVersion_BlocksLaterVersionOfSameStream`).
- Account scope in the browser is a documented TEMPORARY MODEL: Account equals the principal id (`frontend/packages/foundation/query/src/account-scope.ts`).

## 1.3 The ten most important gaps

1. FIXED: readiness and full health now answer 503 when a check is Unhealthy (Degraded stays 200), so a load balancer can take a broken instance out (`HealthEndpoints.cs`). N03, N04.
2. No offline behaviour in the web app beyond the realtime client's `offline` state: no banner, no query pause policy, no queued writes. L02.
3. Account is not a first-class id in the browser (TEMPORARY MODEL), so an account switch cannot be told apart from a user change. B04.
4. Ordered delivery is undecided for any consumer that needs it: no stream identity or contiguous sequence exists on integration events, and the Platform ordering and poison classes are not in the production graph. H03, H04.
5. Replay and forced replay (`ReplayEngine` and strategies) have no operator trigger, no audit and no production wiring. H05.
6. Dead-lettered outbox messages raise a health signal but there is no operator action to inspect, requeue or discard them. H02, H05.
7. No production realtime proof from a browser: gateway tests are backend-only; mock e2e covers network isolation; no real spec shows reconnect or gap recovery. I03, I04.
8. Workspace realtime recovery invalidates the whole workspace key prefix on any gap (`workspace-recovery-policy.ts`): correct but coarse; no checkpoint reconciliation test for repeated gaps (spec `AR` lane PF-09). I04.
9. Background jobs have no shared tenant-scope rule documented per job and no job run history; failure is only a log line. G06, G07.
10. Configuration validation covers CSRF, realtime, OAuth, MFA, email only; other options bind without `ValidateOnStart`. E03.

## 1.4 Defects in shipped code

- Readiness lies: `backend/src/Notrelix.API/Endpoints/Health/HealthEndpoints.cs` `Ready` returns `Results.Ok` for any report status; its `catch` never fires because `CheckHealthAsync` does not throw. An Unhealthy database still answers 200. `GetHealth` also answers 200 for Unhealthy.
- `GetHealth` is mapped with `MapInternalGet`; the readiness probe is also internal, so an external probe needs the internal credential. `Live` is public. Confirm the load-balancer contract before changing.
- Health report leaks no exception text, which is correct, but it returns check names and durations to any internal caller; acceptable, noted.
- No expected version or idempotency applies to health (read only).
- Outbox dispatcher constants (batch size 20, 60 second processing timeout, 5 retries, 60 second max backoff) are compile-time constants in `OutboxDispatcher.cs`, not options, so operators cannot tune them.
- Dead code: `DevNullRealtimePublisher.cs` and the whole `Notrelix.Platform` consumer, ordering, poison and replay classes are outside the production graph (Part 1.2).
- Mock-only frontend path: `frontend/e2e/mock/network-isolation.mock.e2e.spec.ts` is the only browser test of realtime isolation.
- Unbounded read: `OutboxHealthCheck` runs `MinAsync` and `CountAsync` over the outbox table on every readiness probe; fine with an index, but no index proof is cited in tests.

## 1.5 Enterprise baseline

Searched 2026-10-30. The search returned no usable vendor platform pages, so the baseline below is established practice, stated without URLs and to be confirmed against each vendor's status and engineering pages: the transactional outbox pattern with at-least-once delivery and consumer idempotency (Microsoft, "Enterprise Integration Patterns", Hohpe and Woolf); Kubernetes liveness, readiness and startup probes that return a non-2xx status when not ready; per-tenant isolation with database row-level security; WebSocket heartbeats with jittered reconnect and resync-on-gap; correlation ids on every request; structured, redacted logs; a public status page. Table-stakes use cases added as "(enterprise baseline)", state DEFINED: a startup probe (N06), operator dead-letter inspect and requeue (H05), offline banner and write queue policy (L02).

# 2. Index

| ID | Use case | Capability | Actors | BE | FE | Design | Real e2e | State | Gap |
|---|---|---|---|---|---|---|---|---|---|
| UC-PLT-A01 | Issue a session and its CSRF pair at sign-in | A | Frontend runtime, API | yes | yes | n/a | yes | HARDENING | design n/a, no axe line |
| UC-PLT-A02 | Reject an unsafe browser request without a valid CSRF pair | A | API middleware | yes | yes | n/a | yes | HARDENING | invalid-pair path not e2e |
| UC-PLT-A03 | Handle an expired session in the browser | A | Frontend runtime | yes | yes | Final-States | no | INTEGRATING | no real e2e |
| UC-PLT-B01 | Resolve the request context from trusted credentials | B | API middleware | yes | n/a | n/a | yes | HARDENING | cross-tenant negative per route |
| UC-PLT-B02 | Rebuild tenant and principal for a background consumer | B | Consumer host | yes | n/a | n/a | no | IMPLEMENTING | system principal rules per job |
| UC-PLT-B03 | Switch workspace without leaking state | B | Frontend runtime | n/a | yes | n/a | yes | HARDENING | late-response proof |
| UC-PLT-B04 | Switch account or principal and reset the cache | B | Frontend runtime | no | yes | n/a | no | INTEGRATING | no Account id |
| UC-PLT-C01 | Run every protected request through the pipeline | C | Application pipeline | yes | n/a | n/a | yes | HARDENING | exact-candidate proof |
| UC-PLT-C02 | Answer a denied resource with the same NotFound | C | Application pipeline | yes | yes | n/a | yes | HARDENING | per-context coverage |
| UC-PLT-C03 | Run a worker as a bounded principal | C | Consumer host | yes | n/a | n/a | no | IMPLEMENTING | principal list undefined |
| UC-PLT-D01 | Open a tenant-scoped data session per request | D | Application pipeline | yes | n/a | n/a | no | IMPLEMENTING | runtime role proof per deploy |
| UC-PLT-D02 | Refuse to run with an unsafe database role | D | Host startup | yes | n/a | n/a | no | IMPLEMENTING | prod role not exercised |
| UC-PLT-D03 | Reject a stale write with 412 | D | Application pipeline | yes | yes | n/a | yes | HARDENING | not every write carries it |
| UC-PLT-D04 | Use the cache as derived state | D | Application | yes | n/a | n/a | no | IMPLEMENTING | no invalidation rule per use |
| UC-PLT-E01 | Apply migrations at deploy | E | Operator, CI | yes | n/a | n/a | no | IMPLEMENTING | upgrade-path proof per release |
| UC-PLT-E02 | Recover a dev database that failed a migration | E | Operator | yes | n/a | n/a | no | IMPLEMENTING | recreate procedure only |
| UC-PLT-E03 | Validate configuration at startup | E | Host startup | yes | n/a | n/a | no | IMPLEMENTING | partial coverage |
| UC-PLT-F01 | Replay the stored result for a retry with the same key | F | API, Application | yes | yes | n/a | no | IMPLEMENTING | not on every write |
| UC-PLT-F02 | Refuse a reused key with a different request | F | API, Application | yes | n/a | n/a | no | IMPLEMENTING | no frontend case |
| UC-PLT-G01 | Record a domain event in the outbox with the business change | G | Application, data session | yes | n/a | n/a | yes | HARDENING | real spec is indirect |
| UC-PLT-G02 | Dispatch outbox messages with retry and a wake signal | G | Outbox dispatcher | yes | n/a | n/a | yes | HARDENING | constants not options |
| UC-PLT-G03 | Deduplicate a consumer effect by event and consumer | G | Consumer filter | yes | n/a | n/a | no | IMPLEMENTING | per-effect identity per owner |
| UC-PLT-G04 | Recover a stale processing claim | G | Consumer filter, dispatcher | yes | n/a | n/a | no | IMPLEMENTING | no crash-point e2e |
| UC-PLT-G05 | Send queued email from the email outbox | G | Email dispatcher | yes | n/a | n/a | no | IMPLEMENTING | no real e2e |
| UC-PLT-G06 | Run housekeeping jobs under a tenant-safe scope | G | Background jobs | yes | n/a | n/a | no | IMPLEMENTING | no run history |
| UC-PLT-G07 | Sweep due-date and onboarding drafts | G | Background jobs | yes | n/a | n/a | no | IMPLEMENTING | no tenant matrix |
| UC-PLT-G08 | Realtime fan-out after commit | G | Realtime publisher | yes | n/a | n/a | no | IMPLEMENTING | publisher in dev is null |
| UC-PLT-H01 | Hold a later version behind a failed earlier one in a stream | H | Outbox dispatcher | yes | n/a | n/a | no | IMPLEMENTING | stream ids only on outbox |
| UC-PLT-H02 | Retry with backoff, then dead-letter | H | Outbox dispatcher | yes | n/a | n/a | no | IMPLEMENTING | no operator action |
| UC-PLT-H03 | Declare ordering for every consumer | H | Architecture test | yes | n/a | n/a | no | DEFINED | no consumer needs ordering |
| UC-PLT-H04 | Deliver in order a consumer that needs it | H | Platform | partial | n/a | n/a | no | BLOCKED | ordering owner and sequence undecided |
| UC-PLT-H05 | Inspect, requeue or replay dead letters (enterprise baseline) | H | Operator | no | no | n/a | no | DEFINED | no tool |
| UC-PLT-I01 | Open an authenticated realtime connection | I | Frontend runtime, gateway | yes | yes | n/a | no | INTEGRATING | no real browser spec |
| UC-PLT-I02 | Subscribe to a scope with authorization | I | Gateway | yes | yes | n/a | no | INTEGRATING | no real browser spec |
| UC-PLT-I03 | Keep the connection alive and reconnect | I | Gateway, client | yes | yes | n/a | no | IMPLEMENTING | no real e2e |
| UC-PLT-I04 | Recover from a gap, duplicate or reset | I | Client | n/a | yes | BC-Realtime-1440 | no | IMPLEMENTING | no checkpoint reconcile proof |
| UC-PLT-I05 | Close a subscription when permission is lost | I | Gateway | yes | yes | n/a | no | IMPLEMENTING | not shown in browser |
| UC-PLT-J01 | Export OpenAPI and regenerate the client | J | CI, developer | yes | yes | n/a | yes | HARDENING | drift check per release |
| UC-PLT-J02 | Return one error shape for every failure | J | API | yes | yes | n/a | yes | HARDENING | per-context catalogue |
| UC-PLT-J03 | Rate-limit before and after authentication | J | API middleware | yes | n/a | n/a | no | IMPLEMENTING | limits not in real e2e |
| UC-PLT-K01 | Scope every cached query by global, account or workspace | K | Frontend runtime | n/a | yes | n/a | yes | HARDENING | account key lacks id |
| UC-PLT-K02 | Roll back an optimistic mutation on rejection | K | Frontend runtime | n/a | yes | n/a | yes | HARDENING | per-feature coverage |
| UC-PLT-K03 | Ignore a late response from a previous scope | K | Frontend runtime | n/a | yes | n/a | no | IMPLEMENTING | no real e2e |
| UC-PLT-L01 | Bootstrap the session and providers | L | Frontend runtime | yes | yes | Final-Auth | yes | HARDENING | no 1-px or axe line |
| UC-PLT-L02 | Behave sensibly offline (enterprise baseline) | L | Frontend runtime | n/a | partial | Final-States | no | DEFINED | no policy |
| UC-PLT-L03 | Run the mobile runtime with bearer tokens | L | Mobile runtime | yes | partial | Final-M-Auth | no | IMPLEMENTING | mobile host not shipped |
| UC-PLT-L04 | Show a recoverable error instead of a blank screen | L | Frontend runtime | n/a | yes | Final-States | no | IMPLEMENTING | no real e2e |
| UC-PLT-M01 | Serve design tokens from one package | M | Frontend build | n/a | yes | Themes | no | IMPLEMENTING | no real e2e needed; token tests exist |
| UC-PLT-N01 | Carry a correlation id on every request and log | N | API middleware | yes | yes | n/a | no | IMPLEMENTING | no end-to-end trace proof |
| UC-PLT-N02 | Answer a liveness probe | N | Orchestrator | yes | n/a | n/a | no | IMPLEMENTING | no test of the route |
| UC-PLT-N03 | Answer a readiness probe | N | Orchestrator | yes | n/a | n/a | no | IMPLEMENTING | returns 200 when Unhealthy |
| UC-PLT-N04 | Show full health with each check | N | Operator | yes | n/a | n/a | no | IMPLEMENTING | returns 200 when Unhealthy |
| UC-PLT-N05 | Report outbox age and dead letters | N | Operator | yes | n/a | n/a | no | IMPLEMENTING | covered by integration tests; no alert rule |
| UC-PLT-N06 | Answer a startup probe (enterprise baseline) | N | Orchestrator | no | n/a | n/a | no | DEFINED | not specified |
| UC-PLT-N07 | Redact telemetry in the browser | N | Frontend runtime | n/a | yes | n/a | no | IMPLEMENTING | unit-tested, no real e2e |
| UC-PLT-O01 | Fail the build on a forbidden dependency | O | CI | yes | yes | n/a | no | IMPLEMENTING | not an e2e subject |
| UC-PLT-P01 | Run code checks before every push | P | Developer, hook | yes | yes | n/a | no | IMPLEMENTING | local only |
| UC-PLT-P02 | Produce container and packaging evidence | P | CI | yes | yes | n/a | no | IMPLEMENTING | not an e2e subject |

Endpoints with no use case: none. `GET /health`, `GET /health/live` and `GET /health/ready` appear in N02 to N04. The realtime route `GET /api/v1/realtime` (`RealtimeGatewayEndpoint.cs`, excluded from OpenAPI) appears in I01 to I05.

# 3. Use cases

Common evidence. Backend: `backend/src/Notrelix.API/Middleware/`, `backend/src/Notrelix.Application/Common/Behaviors/` (seven behaviors), `backend/src/Notrelix.Infrastructure/Messaging/`, `backend/src/Notrelix.Infrastructure/BackgroundJobs/`, `backend/src/Notrelix.Platform/`. Frontend: `frontend/packages/foundation/` (contracts, kernel, observability, platform, query, realtime), `frontend/packages/runtimes/web`, `frontend/packages/runtimes/mobile`, `frontend/apps/web/src/composition/`. Real e2e support: `frontend/e2e/real/support/real-session.ts` (sign-in used by every real spec). Design canvas has no Platform artboards; the artboards `Final-Auth`, `Final-States`, `BC-Realtime-1440` and `BC-Realtime-768-320` show the user-visible states.

## Capability A: Session and CSRF transport

### UC-PLT-A01 Issue a session and its CSRF pair at sign-in
- Capability: A, PF-01 Session and CSRF transport
- Actors and permission: the browser runtime and the API; no permission (pre-authentication).
- Preconditions: valid credentials; CSRF options valid at startup (`CsrfOptionsValidator`).
- Input: sign-in request.
- Output: session cookie (HTTP only) and a CSRF token pair the client reads; token is never logged or stored in web storage.
- Main flow: 1. Identity authenticates. 2. API sets the cookie and CSRF pair. 3. Client keeps the token in memory.
- No-op / rejection / concurrency / retry: wrong credentials set nothing; a second sign-in replaces the pair.
- UI states: pending, validation error, success.
- Rules: PFREQ009, PFREQ011, PFREQ014; BE-PLT none (transport).
- Acceptance: Given valid credentials, When the user signs in, Then the next write carries the CSRF header and succeeds.
- Evidence: BE `backend/src/Notrelix.Infrastructure/Auth/Csrf/`, `CsrfValidationMiddleware.cs` · FE `frontend/packages/foundation/contracts/src/client/csrf.ts` · Design Final-Auth · Tests `csrf-transport.unit.test.ts`, `backend/tests/Notrelix.API.Tests/Contracts/CsrfEnabledApiFactory.cs` · Real e2e every spec via `real-session.ts`
- State: HARDENING — no axe or design diff recorded; the cross-origin production policy (PFREQ014) is not proven on a deployed host.

### UC-PLT-A02 Reject an unsafe browser request without a valid CSRF pair
- Capability: A, PF-01
- Actors and permission: API middleware.
- Preconditions: request uses an unsafe method with cookie authentication.
- Input: method, cookie, CSRF header.
- Output: 403 problem for a missing or wrong pair; no handler runs.
- Main flow: 1. Middleware classifies the request. 2. Compares the pair. 3. Passes or rejects.
- No-op / rejection / concurrency / retry: safe methods and bearer-token clients are exempt by the applicability inventory; a retry with a fresh pair succeeds.
- UI states: error with a prompt to retry, session-expired path (A03).
- Rules: PFREQ010, PFREQ012, PFREQ015.
- Acceptance: Given a valid cookie and no header, When a POST is sent, Then 403 and no change.
- Evidence: BE `CsrfValidationMiddleware.cs` · FE `csrf.ts` · Design n/a · Tests `backend/tests/Notrelix.Architecture.Tests/EndpointContracts/CsrfApplicabilityInventoryArchitectureTests.cs`, `csrf-transport.unit.test.ts` · Real e2e valid-pair path only
- State: HARDENING — the invalid-pair rejection has no real browser spec.

### UC-PLT-A03 Handle an expired session in the browser
- Capability: A, PF-01
- Actors and permission: frontend runtime.
- Preconditions: signed in; a request returns the terminal session-expired signal.
- Input: session-expired event with an id.
- Output: cache cleared, realtime disconnected, user sent to the signed-out route once.
- Main flow: 1. API client emits the event on the session bus. 2. Lifecycle handles it once per event id. 3. Navigates.
- No-op / rejection / concurrency / retry: a duplicate event id is ignored; a normal token refresh does not clear the cache.
- UI states: signed-out, pending.
- Rules: PFREQ013, PFREQ020.
- Acceptance: Given two expiry events with one id, When both arrive, Then the cache is cleared once and one navigation happens.
- Evidence: BE session endpoints (Identity owns meaning) · FE `frontend/apps/web/src/composition/application-lifecycle.ts`, `frontend/packages/runtimes/web/src/runtime/session-event-bus.ts` · Design Final-States · Tests `session-lifecycle.integration.test.ts` (LIFE-001 to LIFE-008) · Real e2e none
- State: INTEGRATING — no real spec forces an expiry.

## Capability B: Actor/account/workspace context propagation

### UC-PLT-B01 Resolve the request context from trusted credentials
- Capability: B, PF-02 Actor/account/workspace context propagation
- Actors and permission: API middleware.
- Preconditions: authenticated request.
- Input: credentials and route workspace id.
- Output: an immutable execution context: actor, account, workspace, credential kind; never taken from a client body.
- Main flow: 1. `HttpRequestContextMiddleware` resolves identities. 2. `ExecutionContextBehavior` snapshots them. 3. Handlers read the snapshot.
- No-op / rejection / concurrency / retry: a workspace id the actor does not belong to yields NotFound later in the pipeline.
- UI states: n/a.
- Rules: PFREQ016 to PFREQ018.
- Acceptance: Given a token for account A, When it names a workspace of account B, Then NotFound.
- Evidence: BE `HttpRequestContextMiddleware.cs`, `ExecutionContextBehavior.cs` · FE n/a · Design n/a · Tests `backend/tests/Notrelix.Application.Tests/Common/Behaviors/PipelineOrderTests.cs`, `PipelineRuntimeOrderTests.cs` · Real e2e `workspace-isolation.real.e2e.spec.ts`
- State: HARDENING — one cross-workspace real spec; no per-route cross-tenant matrix.

### UC-PLT-B02 Rebuild tenant and principal for a background consumer
- Capability: B, PF-02
- Actors and permission: consumer host acting as a worker principal.
- Preconditions: the event envelope carries tenant identity.
- Input: integration event.
- Output: tenant context restored before the handler runs; a missing tenant is a failure.
- Main flow: 1. `TenantContextConsumeFilter` reads the envelope. 2. `WorkerPrincipalConsumeFilter` sets the principal. 3. Handler runs under RLS.
- No-op / rejection / concurrency / retry: an envelope without tenant raises `IntegrationEventTenantEnvelopeException` and is not guessed.
- UI states: n/a.
- Rules: PFREQ019; BE-PLT-027, BE-PLT-048.
- Acceptance: Given an event for tenant A, When consumed, Then the handler sees only tenant A rows.
- Evidence: BE `TenantContextConsumeFilter.cs`, `WorkerPrincipalConsumeFilter.cs` · FE n/a · Design n/a · Tests `backend/tests/Notrelix.Infrastructure.Tests/Messaging/TenantContextConsumeFilterScopedEventTests.cs` · Real e2e none
- State: IMPLEMENTING — the worker principal and what each consumer may do is not listed in one place.

### UC-PLT-B03 Switch workspace without leaking state
- Capability: B, PF-02
- Actors and permission: frontend runtime; the user chooses the workspace.
- Preconditions: signed in; a different workspace selected.
- Input: next workspace id.
- Output: cache cleared, realtime for the old workspace disconnected with a switch reason, new scope initialized.
- Main flow: 1. Router changes workspace. 2. `prepareWorkspaceTransition`. 3. Queries refetch under the new key.
- No-op / rejection / concurrency / retry: switching to the same workspace is a no-op; A to B to C clears each time; dispose stops further transitions.
- UI states: loading, success.
- Rules: PFREQ020, PFREQ021; FE-STATE-038, FE-STATE-039.
- Acceptance: Given workspace A open, When B is selected, Then no A data shows in B and A realtime is closed.
- Evidence: BE n/a · FE `application-lifecycle.ts`, `frontend/apps/web/src/providers/workspace-provider.tsx` · Design n/a · Tests `workspace-isolation.integration.test.ts` (LIFE-020 to LIFE-026) · Real e2e `workspace-isolation.real.e2e.spec.ts`
- State: HARDENING — no test of a late response arriving after the switch (K03).

### UC-PLT-B04 Switch account or principal and reset the cache
- Capability: B, PF-02
- Actors and permission: frontend runtime.
- Preconditions: a different principal signs in.
- Input: next account scope id.
- Output: the whole cache and in-flight requests dropped.
- Main flow: 1. Auth state changes. 2. `prepareAccountTransition`. 3. `resetCacheForAccountTransition`.
- No-op / rejection / concurrency / retry: same principal is a no-op.
- UI states: loading.
- Rules: PFREQ020, PFREQ022.
- Acceptance: Given user A signs out and B signs in, When B opens a screen, Then no A cache entry exists.
- Evidence: BE none (no Account id exposed) · FE `frontend/packages/foundation/query/src/account-scope.ts`, `application-lifecycle.ts` · Design n/a · Tests `account-scope.unit.test.tsx` · Real e2e none
- State: INTEGRATING — TEMPORARY MODEL: Account equals the principal until the backend exposes an Account id and a switch signal.

## Capability C: Authorization enforcement pipeline

### UC-PLT-C01 Run every protected request through the pipeline
- Capability: C, PF-03 Authorization enforcement pipeline
- Actors and permission: Application pipeline; Governance owns the policy.
- Preconditions: a request marked `IRequirePermission`.
- Input: request, actor, resource.
- Output: handler runs only after tracing, request contract, exception mapping, execution context, access control, idempotency and data session behaviors in the frozen order.
- Main flow: 1. Behaviors run in order. 2. `AccessControlBehavior` asks `AccessPolicyEngine` with facts from `PostgresAccessFactsProvider`. 3. Handler runs.
- No-op / rejection / concurrency / retry: denial stops before the handler; order is frozen by tests.
- UI states: permission denied.
- Rules: PFREQ023 to PFREQ029; BE-PLT none (Application model).
- Acceptance: Given an actor without the permission, When the command is sent, Then the handler never runs.
- Evidence: BE `AccessControlBehavior.cs` · FE n/a · Design n/a · Tests `AccessControlBehaviorTests.cs`, `backend/tests/Notrelix.Architecture.Tests/Pipeline/PipelineFreezeArchitectureTests.cs`, `AuthPipelineArchitectureTests.cs`, `RenameAccountPipelineAuthorizationTests.cs` · Real e2e `guest-permission.real.e2e.spec.ts`
- State: HARDENING — exact-candidate proof per release is a certification item.

### UC-PLT-C02 Answer a denied resource with the same NotFound
- Capability: C, PF-03
- Actors and permission: Application pipeline.
- Preconditions: the actor may not see the resource.
- Input: resource id.
- Output: NotFound identical to a missing resource.
- Main flow: access facts yield no visibility; the pipeline maps to NotFound.
- No-op / rejection / concurrency / retry: timing and body match the missing case.
- UI states: permission denied or not found.
- Rules: PFREQ025, PFREQ026; GOV-029.
- Acceptance: Given a hidden board, When requested by id, Then the response equals one for a random id.
- Evidence: BE pipeline + visibility · FE not-found surfaces · Design Final-States · Tests `WorkReportQueryTests` (example) · Real e2e `guest-permission.real.e2e.spec.ts`
- State: HARDENING — covered per context by its own tests; no platform-wide inventory.

### UC-PLT-C03 Run a worker as a bounded principal
- Capability: C, PF-03
- Actors and permission: worker principal.
- Preconditions: a consumer or job runs without a user.
- Input: tenant context.
- Output: the work runs with only the rights the job needs; revoked authority is never turned into permission by a retry (BE-PLT-048).
- Main flow: principal set by the consume filter; database role limited by RLS.
- No-op / rejection / concurrency / retry: a retry re-checks authority.
- UI states: n/a.
- Rules: PFREQ019; BE-PLT-027, BE-PLT-048.
- Acceptance: Given a consumer for a removed member, When it retries, Then it cannot act for that member.
- Evidence: BE `WorkerPrincipalConsumeFilter.cs`, `backend/src/Notrelix.Infrastructure/Data/DatabasePrincipal.cs` · FE n/a · Design n/a · Tests `RuntimeDatabaseRoleGuardTests.cs` · Real e2e none
- State: IMPLEMENTING — the list of worker principals and their grants is not decided in one document.

## Capability D: Persistence foundation

### UC-PLT-D01 Open a tenant-scoped data session per request
- Capability: D, PF-04 Persistence and tenant-isolation foundation
- Actors and permission: Application pipeline.
- Preconditions: execution context exists.
- Input: request.
- Output: one transaction with the tenant set for RLS; commit owned by the pipeline.
- Main flow: `DataSessionBehavior` opens `EfRequestDataSession`, sets tenant, handler runs, commit enrolls the outbox.
- No-op / rejection / concurrency / retry: a handler must not commit itself; failure rolls back business and outbox together.
- UI states: n/a.
- Rules: PFREQ030 to PFREQ036; BE-PLT-011.
- Acceptance: Given a failing handler, When the request ends, Then neither the change nor its outbox row exists.
- Evidence: BE `DataSessionBehavior.cs`, `backend/src/Notrelix.Infrastructure/Data/EfRequestDataSession.cs` · FE n/a · Design n/a · Tests `backend/tests/Notrelix.Integration.Tests/Rls/AppRoleConformanceTests.cs` · Real e2e none
- State: IMPLEMENTING — RLS is defense in depth and proven by integration tests, not from a browser.

### UC-PLT-D02 Refuse to run with an unsafe database role
- Capability: D, PF-04
- Actors and permission: host startup.
- Preconditions: connection configured.
- Input: configured database role.
- Output: the app refuses to serve when the runtime role can bypass RLS.
- Main flow: `RuntimeDatabaseRoleGuard` checks the role at start.
- No-op / rejection / concurrency / retry: fail closed; no retry loop hides it.
- UI states: n/a.
- Rules: PFREQ031; BE-PLT-056.
- Acceptance: Given an owner role, When the host starts, Then it stops with a clear message.
- Evidence: BE `backend/src/Notrelix.Infrastructure/Data/Rls/RuntimeDatabaseRoleGuard.cs` · FE n/a · Design n/a · Tests `RuntimeDatabaseRoleGuardTests.cs`, `RuntimeDatabaseRoleGuardIntegrationTests.cs` · Real e2e none
- State: IMPLEMENTING — dev only; production roles are not set up (development is not deployed).

### UC-PLT-D03 Reject a stale write with 412
- Capability: D, PF-04
- Actors and permission: Application pipeline.
- Preconditions: a competing write carries an expected version.
- Input: expected version.
- Output: a stale write changes nothing and returns precondition failed.
- Main flow: the data session compares versions at save.
- No-op / rejection / concurrency / retry: an unsupported lookup path fails closed, never skips the check.
- UI states: conflict.
- Rules: PFREQ034; AGENTS expected-version rule.
- Acceptance: Given version 4 stored, When a write arrives with 3, Then 412 and no change.
- Evidence: BE `EfRequestDataSession.cs` · FE conflict UI in feature packages · Design Final-States · Tests `EfRequestDataSessionExpectedVersionTests.cs`, `ExpectedVersionConcurrencyIntegrationTests.cs`, `ExpectedVersionArchitectureTests.cs`, `PreconditionFailedProblemTests.cs` · Real e2e `approvals.real.e2e.spec.ts` (indirect)
- State: HARDENING — not every competing write in every context passes a version (owned by each context).

### UC-PLT-D04 Use the cache as derived state
- Capability: D, PF-04
- Actors and permission: Application services.
- Preconditions: Redis available.
- Input: key, value, expiry.
- Output: faster reads; never the source of truth.
- Main flow: read through `IRedisCacheService`; on miss read the database; invalidate after commit.
- No-op / rejection / concurrency / retry: a cache failure falls back to the database; a key without tenant or audience is refused by the caller's rule.
- UI states: n/a.
- Rules: PFREQ035; data ownership doc (cache is derived).
- Acceptance: Given Redis down, When a read runs, Then it still answers from the database.
- Evidence: BE `backend/src/Notrelix.Infrastructure/Caching/RedisCacheService.cs`, `CacheRegistration.cs`, `RedisHealthCheck.cs` · FE n/a · Design n/a · Tests none specific found · Real e2e none
- State: IMPLEMENTING — no rule says which data may be cached or who invalidates it; callers decide.

## Capability E: Migration, runtime initialization and configuration

### UC-PLT-E01 Apply migrations at deploy
- Capability: E, PF-05 Migration and database initialization
- Actors and permission: operator or CI, using the owner role (the runtime role cannot migrate).
- Preconditions: migration files committed.
- Input: migration set.
- Output: schema at the latest version, RLS policies in place.
- Main flow: 1. CI or operator runs migrations. 2. Host starts with the runtime role.
- No-op / rejection / concurrency / retry: a failed migration rolls back as a whole; applied migrations are never edited.
- UI states: n/a.
- Rules: PFREQ037 to PFREQ043; `docs/` migration owner (backend operations).
- Acceptance: Given a database with policies, When the newest migration runs, Then it succeeds and policies remain.
- Evidence: BE `backend/src/Notrelix.Infrastructure/Data/Migrations/` · FE n/a · Design n/a · Tests the RLS upgrade-path guard in `backend/tests/Notrelix.Integration.Tests/Rls/` · Real e2e none
- State: IMPLEMENTING — supported-upgrade proof is per release boundary.

### UC-PLT-E02 Recover a dev database that failed a migration
- Capability: E, PF-05
- Actors and permission: developer or operator.
- Preconditions: a development database is stuck.
- Input: none.
- Output: a fresh database with all migrations.
- Main flow: recreate the database and re-run migrations (the failed migration rolled back as a whole).
- No-op / rejection / concurrency / retry: never used on shared data; development is not deployed.
- UI states: n/a.
- Rules: PFREQ040.
- Acceptance: Given a stuck database, When recreated, Then the app starts.
- Evidence: BE migration tooling · FE n/a · Design n/a · Tests none · Real e2e none
- State: IMPLEMENTING — a documented procedure, no automated check.

### UC-PLT-E03 Validate configuration at startup
- Capability: E, PF-05
- Actors and permission: host startup.
- Preconditions: options bound from configuration.
- Input: options for CSRF, realtime gateway, OAuth, MFA, email.
- Output: the host refuses to start on invalid options.
- Main flow: `IValidateOptions` and `ValidateOnStart` run before the first request.
- No-op / rejection / concurrency / retry: fail closed; message names the option, never the secret.
- UI states: n/a.
- Rules: PFREQ014, PFREQ040.
- Acceptance: Given an empty allowed-origin list with wildcard off, When started, Then the realtime options fail validation.
- Evidence: BE `CsrfOptionsValidator.cs`, `RealtimeGatewayOptionsValidator` (`RealtimeGatewayOptions.cs`), `OAuthRegistration.cs`, `MfaRegistration.cs`, `EmailRegistration.cs` · FE `frontend/apps/web/src/config/env.ts` · Design n/a · Tests `RealtimeGatewayUnitTests.cs` · Real e2e none
- State: IMPLEMENTING — messaging, outbox and rate-limit options bind without validation.

## Capability F: Idempotency

### UC-PLT-F01 Replay the stored result for a retry with the same key
- Capability: F, PF-06 Idempotency
- Actors and permission: API filter and Application behavior.
- Preconditions: an unsafe request carries one `Idempotency-Key`.
- Input: key header, request body.
- Output: the first result is returned again; no second effect.
- Main flow: 1. `HttpIdempotencyEndpointFilter` checks the header. 2. `IdempotencyBehavior` claims the key under the actor and partition. 3. Stores the result.
- No-op / rejection / concurrency / retry: a missing, empty or repeated header is a typed 4xx; an in-flight duplicate answers "retry shortly with the same key".
- UI states: pending, success.
- Rules: PFREQ044 to PFREQ050.
- Acceptance: Given a create with key K, When it is sent twice, Then one record exists and both calls return it.
- Evidence: BE `backend/src/Notrelix.API/Idempotency/HttpIdempotencyEndpointFilter.cs`, `IdempotencyBehavior.cs` · FE api client sends keys · Design n/a · Tests `backend/tests/Notrelix.API.Tests/Idempotency/IdempotencyEndpointContractTests.cs` · Real e2e none specific
- State: IMPLEMENTING — no real spec retries a request; coverage on every write endpoint is not proven.

### UC-PLT-F02 Refuse a reused key with a different request
- Capability: F, PF-06
- Actors and permission: Application behavior.
- Preconditions: key stored with a fingerprint.
- Input: same key, different body.
- Output: conflict problem, no effect.
- Main flow: compare the request fingerprint; mismatch rejects.
- No-op / rejection / concurrency / retry: identical body is F01.
- UI states: error.
- Rules: PFREQ046; BE-PLT-016.
- Acceptance: Given key K used for body A, When K is sent with body B, Then conflict.
- Evidence: BE `IdempotencyBehavior.cs`, `ProblemDetailsMapper.cs` · FE none · Design n/a · Tests `IdempotencyEndpointContractTests.cs` · Real e2e none
- State: IMPLEMENTING.

## Capability G: Outbox/inbox, message identity, background jobs

### UC-PLT-G01 Record a domain event in the outbox with the business change
- Capability: G, PF-07 Outbox, consumer deduplication and message identity
- Actors and permission: Application pipeline and `DomainEventInterceptor`.
- Preconditions: an aggregate raised a domain event in a committed transaction.
- Input: domain events.
- Output: a `MessagingOutboxMessage` row with a logical event id, in the same transaction; nothing is published before commit.
- Main flow: the interceptor maps events with `IntegrationEventCatalog`; the row is saved with the change.
- No-op / rejection / concurrency / retry: rollback drops both; the event id is stable across retries.
- UI states: n/a.
- Rules: PFREQ051, PFREQ052; BE-PLT-002, BE-PLT-005, BE-PLT-011, BE-PLT-055.
- Acceptance: Given a committed change, When the transaction ends, Then one outbox row with a stable id exists.
- Evidence: BE `backend/src/Notrelix.Infrastructure/Data/Interceptors/DomainEventInterceptor.cs`, `MessagingOutboxMessage.cs` · FE n/a · Design n/a · Tests `backend/tests/Notrelix.Integration.Tests/Data/OutboxDispatchContractTests.cs` · Real e2e `follow-notifications.real.e2e.spec.ts` (indirect)
- State: HARDENING — the real spec reaches it only through a notification.

### UC-PLT-G02 Dispatch outbox messages with retry and a wake signal
- Capability: G, PF-07
- Actors and permission: `OutboxDispatcher` (background worker).
- Preconditions: pending rows.
- Input: batch of 20, poll every second or on the wake signal.
- Output: rows marked dispatched; failures retried with backoff.
- Main flow: 1. Claim a batch with a lease. 2. Publish. 3. Mark done or schedule retry.
- No-op / rejection / concurrency / retry: up to 5 retries with backoff to 60 seconds; broker outage does not erase committed intent.
- UI states: n/a.
- Rules: PFREQ053; BE-PLT-018, BE-PLT-019, BE-PLT-055.
- Acceptance: Given a broker outage, When it ends, Then pending rows are delivered once.
- Evidence: BE `backend/src/Notrelix.Infrastructure/BackgroundJobs/OutboxDispatcher.cs`, `Messaging/OutboxWakeSignal.cs` · FE n/a · Design n/a · Tests `OutboxDispatchContractTests.cs`, `OutboxClaimReclaimTests.cs`, `ProductionGraphTests.cs` · Real e2e `follow-notifications.real.e2e.spec.ts` (indirect)
- State: HARDENING — batch size, timeout and retry limits are constants, not options.

### UC-PLT-G03 Deduplicate a consumer effect by event and consumer
- Capability: G, PF-07
- Actors and permission: `DeduplicationConsumeFilter`.
- Preconditions: a consumer with a stable name.
- Input: message with event id.
- Output: effect runs once per `(EventId, ConsumerName)`.
- Main flow: claim in `MessagingProcessedEvent`; run; mark processed; a duplicate finds it processed and skips.
- No-op / rejection / concurrency / retry: duplicate delivery is expected, not corruption; a claim in flight raises `MessageClaimInFlightException` and retries; an identity conflict is distinct from a duplicate.
- UI states: n/a.
- Rules: PFREQ054 to PFREQ056; BE-PLT-007, BE-PLT-014 to BE-PLT-017.
- Acceptance: Given the same event twice, When consumed, Then the effect happens once.
- Evidence: BE `DeduplicationConsumeFilter.cs`, `MessageDeduplicationStore.cs` · FE n/a · Design n/a · Tests `backend/tests/Notrelix.Integration.Tests/Messaging/DeduplicationConsumeFilterIntegrationTests.cs`, `DeduplicationConsumeFilterClaimLeaseTests.cs` · Real e2e none
- State: IMPLEMENTING — external provider effects need their own claim (`IProviderEffectClaimStore`), owner-specific.

### UC-PLT-G04 Recover a stale processing claim
- Capability: G, PF-07
- Actors and permission: consumer filter and dispatcher.
- Preconditions: a worker crashed holding a claim.
- Input: claim older than its lease.
- Output: the claim is taken over once; an active lease is never taken.
- Main flow: `TryReclaimStaleProcessingAsync` updates only a stale `Processing` row, one winner.
- No-op / rejection / concurrency / retry: concurrent reclaimers affect zero rows except one.
- UI states: n/a.
- Rules: BE-PLT-013; PFREQ057.
- Acceptance: Given a claim older than the lease, When two workers race, Then exactly one resumes it.
- Evidence: BE `MessageDeduplicationStore.cs` · FE n/a · Design n/a · Tests `OutboxClaimReclaimTests.cs` (`Claim_ReclaimsExpiredLease_ButNeverActiveLease`), `DeduplicationConsumeFilterClaimLeaseTests.cs` · Real e2e none
- State: IMPLEMENTING — the spec's preparation gap is closed in source; no crash-point test through the real broker.

### UC-PLT-G05 Send queued email from the email outbox
- Capability: G, PF-07
- Actors and permission: `EmailDispatcher`.
- Preconditions: `EmailOutboxMessage` rows.
- Input: pending email rows.
- Output: sent mail and delivery attempts recorded.
- Main flow: claim, send, record the attempt, retry on failure.
- No-op / rejection / concurrency / retry: a repeated send is guarded by the row state.
- UI states: n/a.
- Rules: BE-PLT-018, BE-PLT-045.
- Acceptance: Given a failing mail server, When it recovers, Then the mail is sent once.
- Evidence: BE `BackgroundJobs/EmailDispatcher.cs`, `Data/Notifications/EmailOutboxMessage.cs` · FE n/a · Design n/a · Tests none specific found · Real e2e `email-confirmation.real.e2e.spec.ts` (indirect)
- State: IMPLEMENTING.

### UC-PLT-G06 Run housekeeping jobs under a tenant-safe scope
- Capability: G, PF-07 (background work, BE-PLT-027)
- Actors and permission: hosted services: attachment and avatar object reapers, security activity retention.
- Preconditions: services registered in `BackgroundJobsRegistration.cs`.
- Input: schedule.
- Output: orphan objects and expired security activity removed.
- Main flow: each job scans in bounded batches and deletes by policy.
- No-op / rejection / concurrency / retry: nothing to do is a no-op; a failure is logged and the next tick retries; scale-out needs occurrence identity (BE-PLT-040).
- UI states: n/a.
- Rules: BE-PLT-027, BE-PLT-040, BE-PLT-041.
- Acceptance: Given an orphan object older than its grace period, When the job runs, Then it is deleted and a live object is kept.
- Evidence: BE `AttachmentObjectReaperService.cs`, `AvatarObjectReaperService.cs`, `SecurityActivityRetention.cs` · FE n/a · Design n/a · Tests none specific found · Real e2e none
- State: IMPLEMENTING — no job run history; double run on two instances is not designed.

### UC-PLT-G07 Sweep due-date and onboarding drafts
- Capability: G, PF-07
- Actors and permission: `DueDateNotificationService`, `OnboardingDraftSweepService`.
- Preconditions: items with due dates; abandoned drafts.
- Input: schedule.
- Output: due-date notifications created once; stale drafts removed.
- Main flow: scan per tenant, create notification through the owning context's port, mark done.
- No-op / rejection / concurrency / retry: the same occurrence is created once (occurrence identity); a failure retries.
- UI states: notification shows in the inbox.
- Rules: BE-PLT-027, BE-PLT-040.
- Acceptance: Given an item due tomorrow, When the sweep runs twice, Then one notification exists.
- Evidence: BE `DueDateNotificationSweep.cs`, `OnboardingDraftSweep.cs` · FE inbox · Design Final-Inbox · Tests none specific found · Real e2e none
- State: IMPLEMENTING — no tenant matrix test.

### UC-PLT-G08 Realtime fan-out after commit
- Capability: G, PF-07
- Actors and permission: realtime publisher.
- Preconditions: a committed fact worth notifying.
- Input: notification with stream key and version.
- Output: a notify-only message to connected clients; no resource data.
- Main flow: after commit the publisher writes to Redis; the gateway delivers to authorized subscribers.
- No-op / rejection / concurrency / retry: a publish failure never changes source truth (BE-PLT-059); clients recover by REST.
- UI states: live, stale.
- Rules: BE-PLT-059; PFREQ065.
- Acceptance: Given Redis down, When a change commits, Then the change stands and clients recover on reconnect.
- Evidence: BE `backend/src/Notrelix.Infrastructure/Realtime/RedisRealtimePublisher.cs`, `Messaging/DevNullRealtimePublisher.cs` · FE realtime client · Design n/a · Tests `backend/tests/Notrelix.API.Tests/Realtime/RealtimeGatewayEndpointTests.cs` · Real e2e none
- State: IMPLEMENTING.

## Capability H: Ordered delivery and poison handling

### UC-PLT-H01 Hold a later version behind a failed earlier one in a stream
- Capability: H, PF-08 Ordered delivery and poison handling
- Actors and permission: outbox dispatcher.
- Preconditions: messages carry a stream and a version.
- Input: claim attempt.
- Output: a failed earlier version blocks later versions of the same stream; other streams continue.
- Main flow: the claim query skips a stream with an earlier failed or unprocessed version.
- No-op / rejection / concurrency / retry: processed earlier versions release the stream.
- UI states: n/a.
- Rules: PFREQ058 to PFREQ064; BE-PLT-023, BE-PLT-024.
- Acceptance: Given version 1 failed, When version 2 is ready, Then version 2 waits.
- Evidence: BE `OutboxDispatcher.cs` claim · FE n/a · Design n/a · Tests `OutboxClaimReclaimTests.cs` (`Claim_FailedEarlierVersion_BlocksLaterVersionOfSameStream`, `Claim_ProcessedEarlierVersion_ReleasesLaterVersion_AndUnrelatedStreams`) · Real e2e none
- State: IMPLEMENTING — this covers the outbox leg only, not broker consumers.

### UC-PLT-H02 Retry with backoff, then dead-letter
- Capability: H, PF-08
- Actors and permission: outbox dispatcher.
- Preconditions: a message fails.
- Input: failure.
- Output: retries up to the limit, then `Failed` with `RetryCount` at the limit (dead letter); it is kept, not deleted.
- Main flow: transient failures retry; exhausted rows stay as evidence.
- No-op / rejection / concurrency / retry: a retry keeps the logical id; the circuit breaker is degraded, not fake success.
- UI states: n/a.
- Rules: BE-PLT-018 to BE-PLT-022.
- Acceptance: Given a permanent failure, When retries are exhausted, Then the row is dead-lettered and visible to health.
- Evidence: BE `OutboxDispatcher.cs`, `OutboxHealthCheck.cs` · FE n/a · Design n/a · Tests `OutboxDispatchContractTests.cs` · Real e2e none
- State: IMPLEMENTING — no operator action to inspect or requeue (H05).

### UC-PLT-H03 Declare ordering for every consumer
- Capability: H, PF-08
- Actors and permission: Architecture test (CI).
- Preconditions: every `IConsumer<T>` in Infrastructure.
- Input: source.
- Output: the build fails when a consumer lacks `OrderingNotApplicableAttribute` with a reason.
- Main flow: the architecture test scans consumers.
- No-op / rejection / concurrency / retry: a consumer that needs ordering cannot use the attribute (H04).
- UI states: n/a.
- Rules: PFREQ058; BE-PLT-023.
- Acceptance: Given a new consumer without the attribute, When CI runs, Then it fails.
- Evidence: BE `OrderingNotApplicableAttribute.cs` · FE n/a · Design n/a · Tests Architecture tests for consumers (`backend/tests/Notrelix.Architecture.Tests/Events/PublicEventContractArchitectureTests.cs` and neighbours) · Real e2e none
- State: DEFINED — the rule is stated in the attribute; the exact enforcing test name was not confirmed in this review.

### UC-PLT-H04 Deliver in order a consumer that needs it
- Capability: H, PF-08
- Actors and permission: Platform.
- Preconditions: a consumer whose invariant depends on order.
- Input: stream identity and a contiguous sequence.
- Output: in-order effect; gaps observable.
- Main flow: not decided: `OrderingEnforcer` exists but is outside the production graph and events carry no contiguous sequence.
- No-op / rejection / concurrency / retry: not decided.
- UI states: n/a.
- Rules: BE-PLT-023 to BE-PLT-025; PFREQ059.
- Acceptance: Given the decision, When events arrive out of order, Then the effect is applied in order or the gap is raised.
- Evidence: BE `backend/src/Notrelix.Platform/Messaging/Reliability/OrderingEnforcer.cs` (not in production graph) · FE n/a · Design n/a · Tests `backend/tests/Notrelix.Platform.Tests/` · Real e2e none
- State: BLOCKED — decide the ordering owner, the stream identity and who produces the sequence, for the first consumer that needs it.

### UC-PLT-H05 Inspect, requeue or replay dead letters (enterprise baseline)
- Capability: H, PF-08
- Actors and permission: platform operator with an audited system principal.
- Preconditions: dead-lettered rows exist.
- Input: selection, action (requeue, discard with reason, replay window).
- Output: rows handled; every action audited; replay is not "republish everything".
- Main flow: operator lists by age and consumer, inspects metadata before payload, acts.
- No-op / rejection / concurrency / retry: requeue keeps the logical id; a replay advances its checkpoint only after success; forced replay is a governed operation.
- UI states: operator tooling (no product UI).
- Rules: BE-PLT-032 to BE-PLT-035, BE-PLT-044, BE-PLT-053.
- Acceptance: Given a dead letter, When requeued, Then it is delivered once and the action is audited.
- Evidence: BE `backend/src/Notrelix.Platform/Messaging/Operations/ReplayEngine.cs` (not wired) · FE none · Design n/a · Tests `backend/tests/Notrelix.Platform.Tests/` · Real e2e none
- State: DEFINED — no operator entry point.

## Capability I: Realtime connection and recovery

### UC-PLT-I01 Open an authenticated realtime connection
- Capability: I, PF-09 Realtime transport and recovery
- Actors and permission: web or mobile runtime and the gateway (`GET /api/v1/realtime`, authenticated, WebSocket).
- Preconditions: signed in; origin allowed; per-user connection limit (8) not exceeded.
- Input: WebSocket upgrade with credentials.
- Output: a `ready` frame; notify-only frames after that.
- Main flow: 1. Origin policy. 2. Credential validation. 3. Limit lease. 4. `ready`.
- No-op / rejection / concurrency / retry: a bad origin is refused; bad credentials close with 4401; over the limit refuses.
- UI states: connecting, connected, offline.
- Rules: PFREQ065, PFREQ066; realtime doc.
- Acceptance: Given a disallowed origin, When it upgrades, Then it is refused.
- Evidence: BE `backend/src/Notrelix.API/Realtime/RealtimeGatewayEndpoint.cs`, `RealtimeConnection.cs` · FE `frontend/packages/foundation/realtime/src/transport/`, `frontend/packages/runtimes/web/src/realtime/` · Design BC-Realtime-1440 · Tests `backend/tests/Notrelix.API.Tests/Realtime/RealtimeGatewayEndpointTests.cs`, `RealtimeConnectionTests.cs`, `realtime-gateway.unit.test.ts` · Real e2e none
- State: INTEGRATING — no real browser spec connects.

### UC-PLT-I02 Subscribe to a scope with authorization
- Capability: I, PF-09
- Actors and permission: client; gateway authorizes each subscription.
- Preconditions: connection ready.
- Input: `subscribe` frame with id and scope.
- Output: `subscribed`, or `subscription-error` with a code (`subscription_denied`, `unsupported_scope`, `subscription_limit`).
- Main flow: authorizer checks the scope; the subscription is added; events arrive.
- No-op / rejection / concurrency / retry: a duplicate subscription id conflicts; limit 50 per connection.
- UI states: subscribed, denied.
- Rules: PFREQ067; BE-PLT-059.
- Acceptance: Given a workspace the user cannot read, When subscribed, Then `subscription_denied`.
- Evidence: BE `RealtimeAuthorization.cs`, `RealtimeProtocol.cs` · FE `frontend/apps/web/src/providers/realtime-lifecycle.tsx` · Design BC-Realtime-1440 · Tests `RealtimeGatewayHarness.cs`, `RealtimeGatewayUnitTests.cs` · Real e2e none
- State: INTEGRATING.

### UC-PLT-I03 Keep the connection alive and reconnect
- Capability: I, PF-09
- Actors and permission: gateway and client.
- Preconditions: connected.
- Input: pings every 25 seconds, idle timeout 75 seconds, lifetime 1 hour.
- Output: a live connection, or a close code (1013 slow consumer, 4408 heartbeat timeout) followed by a reconnect.
- Main flow: client answers pings; on close it reconnects with a jittered backoff.
- No-op / rejection / concurrency / retry: reconnect resubscribes and then resyncs (I04).
- UI states: connected, reconnecting, offline.
- Rules: PFREQ068, PFREQ069.
- Acceptance: Given a dropped network, When it returns, Then the client reconnects and resubscribes.
- Evidence: BE `RealtimeGatewayOptions.cs`, `RealtimeConnection.cs` · FE `frontend/packages/foundation/realtime/src/connection/reconnect-policy.ts` · Design BC-Realtime-768-320 · Tests `reconnect-policy.unit.test.ts`, `realtime-client.unit.test.ts` · Real e2e none
- State: IMPLEMENTING.

### UC-PLT-I04 Recover from a gap, duplicate or reset
- Capability: I, PF-09
- Actors and permission: client.
- Preconditions: the client keeps the last `streamVersion` per `streamKey`.
- Input: an event whose version is not previous plus one, or a resubscription ack.
- Output: the workspace scope is revalidated over REST; a duplicate is ignored.
- Main flow: 1. Checkpoint compares versions. 2. Gap or reset triggers recovery. 3. `handleWorkspaceRecovery` invalidates the workspace query prefix and the unread count.
- No-op / rejection / concurrency / retry: realtime is never durable truth; permission loss shows as a forbidden REST answer.
- UI states: stale, reconnecting, refreshed.
- Rules: PFREQ070, PFREQ071; FE-RT-027.
- Acceptance: Given version 5 then 8, When the gap is seen, Then the workspace refetches and shows current data.
- Evidence: BE `RealtimeProtocol.cs` · FE `frontend/apps/web/src/realtime/workspace-recovery-policy.ts`, `stream-checkpoint` · Design BC-Realtime-1440 · Tests `stream-checkpoint.unit.test.ts` · Real e2e none
- State: IMPLEMENTING — the whole workspace prefix refetches on every gap, and repeated-gap convergence is not tested (spec `PF-09` gap).

### UC-PLT-I05 Close a subscription when permission is lost
- Capability: I, PF-09
- Actors and permission: gateway.
- Preconditions: an active subscription.
- Input: re-authorization every 60 seconds.
- Output: `subscription_revoked` for a lost scope; credential failure closes the connection.
- Main flow: periodic re-check; revoke frames; client recovers by REST.
- No-op / rejection / concurrency / retry: authorization unavailable gives `authorization_unavailable`, not an open door.
- UI states: denied, stale.
- Rules: PFREQ067; BE-PLT-048.
- Acceptance: Given a removed member, When the next check runs, Then their subscription is revoked.
- Evidence: BE `RealtimeAuthorization.cs`, `RealtimeConnection.cs` · FE recovery policy · Design BC-Realtime-1440 · Tests `RealtimeConnectionTests.cs` · Real e2e none
- State: IMPLEMENTING — shown in no browser test.

## Capability J: API, OpenAPI and generated contracts

### UC-PLT-J01 Export OpenAPI and regenerate the client
- Capability: J, PF-10 API, OpenAPI and generated contracts
- Actors and permission: CI and developers.
- Preconditions: an endpoint change.
- Input: source.
- Output: tracked OpenAPI artifact and a regenerated `frontend/packages/foundation/contracts/src/generated/rest/schema.ts`; a drift check fails when they differ.
- Main flow: export, generate, check drift.
- No-op / rejection / concurrency / retry: generated files are never hand-edited.
- UI states: n/a.
- Rules: PFREQ072 to PFREQ078; NRX-008.
- Acceptance: Given a new route, When the producer is regenerated, Then the frontend type exists and the drift check passes.
- Evidence: BE `backend/tests/Notrelix.Architecture.Tests/EndpointContracts/OpenApiConventionTests.cs` · FE `pnpm codegen:check` · Design n/a · Tests `backend/tests/Notrelix.API.Tests/Contracts/` · Real e2e used by all (typed client)
- State: HARDENING — drift check is run per release candidate.

### UC-PLT-J02 Return one error shape for every failure
- Capability: J, PF-10
- Actors and permission: API.
- Preconditions: any failure.
- Input: exception or validation result.
- Output: a problem-details body with a stable code; no stack or secret.
- Main flow: `GlobalExceptionHandler` and `ProblemDetailsMapper` map; the client maps to a toast or an inline state.
- No-op / rejection / concurrency / retry: 412 and idempotency conflicts have typed problems.
- UI states: error, conflict, permission denied.
- Rules: PFREQ074.
- Acceptance: Given a validation failure, When returned, Then it lists field errors in problem-details.
- Evidence: BE `backend/src/Notrelix.API/ErrorHandling/ProblemDetailsMapper.cs` · FE `frontend/packages/foundation/contracts/src/client/api-client.ts`, `frontend/apps/web/src/providers/query-error-policy.ts` · Design Final-States · Tests `GlobalExceptionHandlerTests.cs`, `api-client.unit.test.ts` · Real e2e every spec indirectly
- State: HARDENING.

### UC-PLT-J03 Rate-limit before and after authentication
- Capability: J, PF-10
- Actors and permission: API middleware.
- Preconditions: any request.
- Input: client address, credential, policy tier.
- Output: 429 with retry information above the limit.
- Main flow: `PreAuthenticationRateLimitMiddleware` then `AuthenticatedRateLimitMiddleware`.
- No-op / rejection / concurrency / retry: retry after the window.
- UI states: error with wait time.
- Rules: PFREQ076.
- Acceptance: Given the limit exceeded, When another request arrives, Then 429.
- Evidence: BE `Middleware/PreAuthenticationRateLimitMiddleware.cs`, `AuthenticatedRateLimitMiddleware.cs` · FE error policy · Design Final-States · Tests `backend/tests/Notrelix.API.Tests/RateLimiting/RateLimitTiersTests.cs`, `RateLimitPolicyCatalogTests.cs` · Real e2e none
- State: IMPLEMENTING — real e2e runs with limits relaxed.

## Capability K: Frontend query/server-state foundation

### UC-PLT-K01 Scope every cached query by global, account or workspace
- Capability: K, PF-11 Frontend query/server-state foundation
- Actors and permission: frontend runtime.
- Preconditions: a query is defined.
- Input: owner, scope, resource.
- Output: a key with the scope; workspace keys include the workspace id.
- Main flow: helpers `workspaceQueryKey` and account helpers build keys.
- No-op / rejection / concurrency / retry: a key without scope is a lint or test failure.
- UI states: n/a.
- Rules: PFREQ079 to PFREQ085; FE-STATE rules.
- Acceptance: Given two workspaces, When the same list is loaded, Then entries are separate.
- Evidence: BE n/a · FE `frontend/packages/foundation/query/src/query-key-scope.ts` · Design n/a · Tests `query-key-scope.unit.test.ts` · Real e2e `workspace-isolation.real.e2e.spec.ts`
- State: HARDENING — account keys lack the account id (B04).

### UC-PLT-K02 Roll back an optimistic mutation on rejection
- Capability: K, PF-11
- Actors and permission: frontend runtime.
- Preconditions: a mutation eligible for optimism.
- Input: snapshot, patch, request.
- Output: patch applied, then reconciled on success or rolled back on rejection or conflict.
- Main flow: `optimistic-command.ts` snapshots, patches, sends, reconciles.
- No-op / rejection / concurrency / retry: not every mutation is optimistic; a conflict refetches.
- UI states: pending, conflict, error.
- Rules: PFREQ082; NRX-014.
- Acceptance: Given a rejected write, When the response arrives, Then the UI returns to the server state.
- Evidence: BE n/a · FE `frontend/packages/foundation/query/src/optimistic-command.ts` · Design n/a · Tests `optimistic-command.unit.test.ts` · Real e2e `item-lifecycle.real.e2e.spec.ts` (indirect)
- State: HARDENING — per-feature coverage is owned by each context.

### UC-PLT-K03 Ignore a late response from a previous scope
- Capability: K, PF-11
- Actors and permission: frontend runtime.
- Preconditions: a request is in flight during a scope change.
- Input: a late response or event.
- Output: it does not overwrite the new scope.
- Main flow: transitions cancel in-flight requests and drop the cache; keys carry the scope.
- No-op / rejection / concurrency / retry: a late realtime event for the old workspace is ignored.
- UI states: n/a.
- Rules: PFREQ020, PFREQ021.
- Acceptance: Given a slow response for workspace A after switching to B, When it arrives, Then B shows no A data.
- Evidence: BE n/a · FE `application-lifecycle.ts`, query keys · Design n/a · Tests `workspace-isolation.integration.test.ts` (cancel and clear) · Real e2e none
- State: IMPLEMENTING — no test delivers a late response on purpose.

## Capability L: Frontend runtime and host composition

### UC-PLT-L01 Bootstrap the session and providers
- Capability: L, PF-12 Frontend runtime and host composition
- Actors and permission: web runtime.
- Preconditions: the page loads.
- Input: cookie session.
- Output: runtime started, session profile loaded, providers mounted, router ready, or the signed-out route.
- Main flow: 1. `app-runtime.tsx` builds services. 2. Auth provider verifies the session. 3. Workspace provider selects the workspace. 4. Realtime lifecycle starts.
- No-op / rejection / concurrency / retry: a failed profile query shows a recoverable state, not a blank page.
- UI states: verifying session, signed out, error, success.
- Rules: PFREQ086 to PFREQ092.
- Acceptance: Given a valid session, When the app loads, Then the shell shows without a second sign-in.
- Evidence: BE session profile endpoint (Identity) · FE `frontend/packages/runtimes/web/src/runtime/app-runtime.tsx`, `frontend/apps/web/src/providers/app-providers.tsx` · Design Final-Auth · Tests `application-services.unit.test.ts` · Real e2e every spec via sign-in
- State: HARDENING — no design diff or axe line for the verifying state.

### UC-PLT-L02 Behave sensibly offline (enterprise baseline)
- Capability: L, PF-12
- Actors and permission: frontend runtime.
- Preconditions: the browser reports offline or requests fail on the network.
- Input: connectivity change.
- Output: a visible offline state, paused refetch, a clear retry on return; no silent loss of an edit.
- Main flow: detect, show the state, keep reads from cache, block or queue writes by a stated rule.
- No-op / rejection / concurrency / retry: mobile is read-cache only offline (owner decision 47); the web policy for writes is a Lead decision.
- UI states: offline, reconnecting, pending.
- Rules: README section 7 line 2 (offline state); FE-RT rules.
- Acceptance: Given the network drops, When the user edits, Then a message says the edit was not saved and offers retry.
- Evidence: BE n/a · FE only the realtime client's `offline` state in `realtime-client.ts` · Design Final-States · Tests none · Real e2e none
- State: DEFINED — see Part 4 for the rule.

### UC-PLT-L03 Run the mobile runtime with bearer tokens
- Capability: L, PF-12
- Actors and permission: mobile runtime.
- Preconditions: the mobile client header selects bearer mode.
- Input: rotating refresh token in the body; tokens in secure storage.
- Output: a signed-in native session; workspace lifecycle like web.
- Main flow: native client signs in, stores tokens in Keychain or Keystore, refreshes.
- No-op / rejection / concurrency / retry: rotation replaces the token; reuse revokes the chain (Identity rule).
- UI states: signed out, offline read cache.
- Rules: PFREQ086; owner decision 47.
- Acceptance: Given a refresh, When used twice, Then the second is refused.
- Evidence: BE Identity token endpoints · FE `frontend/packages/runtimes/mobile/src/runtime/mobile-workspace-lifecycle.ts` · Design Final-M-Auth · Tests `mobile-runtime.mobile.test.ts` · Real e2e none
- State: IMPLEMENTING — no mobile host app ships.

### UC-PLT-L04 Show a recoverable error instead of a blank screen
- Capability: L, PF-12
- Actors and permission: frontend runtime.
- Preconditions: a render or query error.
- Input: error.
- Output: an error boundary with retry and a safe message.
- Main flow: `runtime-error-boundary.tsx` and `global-error-boundary.tsx` catch and render.
- No-op / rejection / concurrency / retry: retry remounts the subtree; the error is reported with redaction (N07).
- UI states: error.
- Rules: PFREQ090.
- Acceptance: Given a thrown render error, When it happens, Then the page shows an error state with retry.
- Evidence: BE n/a · FE `frontend/apps/web/src/providers/runtime-error-boundary.tsx` · Design Final-States · Tests component tests · Real e2e none
- State: IMPLEMENTING.

## Capability M: UI tokens and primitive foundation

### UC-PLT-M01 Serve design tokens from one package
- Capability: M, PF-13 UI tokens and primitive foundation
- Actors and permission: frontend build.
- Preconditions: tokens defined in `frontend/packages/ui/tokens`.
- Input: token source.
- Output: `./css` export (`src/css/index.css`) built by `build-css.ts`, consumed by hosts; light and dark.
- Main flow: build tokens to CSS; hosts import the export.
- No-op / rejection / concurrency / retry: hosts do not define their own copies.
- UI states: light, dark.
- Rules: PFREQ093 to PFREQ099.
- Acceptance: Given the build, When a host imports `@notrelix/ui-tokens/css`, Then the tokens resolve.
- Evidence: BE n/a · FE `frontend/packages/ui/tokens/package.json` export `./css`, `src/css/build-css.ts` · Design Themes · Tests `frontend/packages/ui/tokens/src/__tests__/tokens.unit.test.ts` · Real e2e none
- State: IMPLEMENTING — the packaging gap in the spec is closed in source; no primitive inventory proof here.

## Capability N: Observability and health

### UC-PLT-N01 Carry a correlation id on every request and log
- Capability: N, PF-14 Observability
- Actors and permission: API middleware and frontend client.
- Preconditions: any request.
- Input: optional incoming id.
- Output: one correlation id in logs, traces, problem bodies; kept distinct from message id and idempotency key.
- Main flow: `CorrelationIdMiddleware` assigns; behaviors tag spans.
- No-op / rejection / concurrency / retry: an unsafe incoming id is replaced.
- UI states: error pages may show the id.
- Rules: PFREQ100 to PFREQ106; BE-PLT-029, BE-PLT-042.
- Acceptance: Given a request, When it fails, Then the problem body and log share one id.
- Evidence: BE `Middleware/CorrelationIdMiddleware.cs`, `ApplicationTracingBehavior.cs` · FE `frontend/packages/foundation/observability/src/tracing.ts` · Design n/a · Tests `PipelineObservabilityTests.cs`, `PipelineTelemetryIntegrationTests.cs` · Real e2e none
- State: IMPLEMENTING.

### UC-PLT-N02 Answer a liveness probe
- Capability: N, PF-14
- Actors and permission: orchestrator, public (no credentials).
- Preconditions: process running.
- Input: `GET /health/live`.
- Output: 200 `{status: alive, timestamp}`.
- Main flow: returns without touching any dependency.
- No-op / rejection / concurrency / retry: never fails because a dependency is down.
- UI states: n/a.
- Rules: PFREQ100.
- Acceptance: Given the process runs, When probed, Then 200.
- Evidence: BE `backend/src/Notrelix.API/Endpoints/Health/HealthEndpoints.cs` `Live` (`MapPublicGet`) · FE n/a · Design n/a · Tests none for the route · Real e2e none
- State: IMPLEMENTING — no test of the route.

### UC-PLT-N03 Answer a readiness probe
- Capability: N, PF-14
- Actors and permission: orchestrator with the internal credential (`MapInternalGet`).
- Preconditions: checks tagged `ready`: database, Redis, outbox.
- Input: `GET /health/ready`.
- Output: 200 when ready; Lead decision: 503 when any ready check is Unhealthy (Part 4).
- Main flow: run the `ready` checks and return status.
- No-op / rejection / concurrency / retry: Degraded stays 200 with the status in the body.
- UI states: n/a.
- Rules: PFREQ100; BE-PLT-038.
- Acceptance: Given the database down, When probed, Then 503.
- Evidence: BE `HealthEndpoints.cs` `Ready`, `DatabaseHealthCheck.cs`, `RedisHealthCheck.cs`, `OutboxHealthCheck.cs`, registration in `backend/src/Notrelix.API/DependencyInjection.cs` · FE n/a · Design n/a · Tests `HealthEndpointTests.cs` (`Probe_ShouldMapReportStatusToHttpStatus`, `/health/ready` rows) · Real e2e none
- State: IMPLEMENTED — 503 when Unhealthy, 200 when Healthy or Degraded.

### UC-PLT-N04 Show full health with each check
- Capability: N, PF-14
- Actors and permission: platform operator (internal credential).
- Preconditions: signed internal call.
- Input: `GET /health`.
- Output: overall status and each check's name, status and duration.
- Main flow: run all checks and list them.
- No-op / rejection / concurrency / retry: unauthenticated callers get 401 (tested).
- UI states: n/a.
- Rules: PFREQ100.
- Acceptance: Given an unauthenticated call, When made, Then 401. Given Redis down, When called by an operator, Then the body shows Redis Unhealthy.
- Evidence: BE `HealthEndpoints.cs` `GetHealth` · FE n/a · Design n/a · Tests `backend/tests/Notrelix.API.Tests/Contracts/HealthEndpointTests.cs` (`GetHealth_WhenNotAuthenticated_ShouldReturnUnauthorized`, `Probe_ShouldMapReportStatusToHttpStatus`) · Real e2e none
- State: IMPLEMENTED — 503 when Unhealthy, 200 when Healthy or Degraded.

### UC-PLT-N05 Report outbox age and dead letters
- Capability: N, PF-14
- Actors and permission: operator through health and metrics.
- Preconditions: outbox in use.
- Input: none.
- Output: Degraded after 5 minutes of oldest pending, Unhealthy after 15 minutes or on any dead letter; backlog counts as metrics.
- Main flow: `OutboxHealthCheck` and `OutboxDiagnosticsService` read the table and publish `MetricsService` gauges.
- No-op / rejection / concurrency / retry: read only; metadata, never payload.
- UI states: n/a.
- Rules: BE-PLT-043, BE-PLT-047.
- Acceptance: Given a pending row 20 minutes old, When checked, Then Unhealthy.
- Evidence: BE `Observability/HealthChecks/OutboxHealthCheck.cs`, `OutboxDiagnosticsService.cs`, `Metrics/MetricsService.cs` · FE n/a · Design n/a · Tests `PipelineTelemetryIntegrationTests.cs` (related) · Real e2e none
- State: IMPLEMENTING — no alert rule or dashboard consumes the signal.

### UC-PLT-N06 Answer a startup probe (enterprise baseline)
- Capability: N, PF-14
- Actors and permission: orchestrator.
- Preconditions: slow start (migration check, role guard).
- Input: `GET /health/startup`.
- Output: non-2xx until startup guards have passed, then 200.
- Main flow: a flag set after the role guard and option validation.
- No-op / rejection / concurrency / retry: none.
- UI states: n/a.
- Rules: BE-PLT-038.
- Acceptance: Given the role guard has not finished, When probed, Then not ready.
- Evidence: BE none · FE n/a · Design n/a · Tests none · Real e2e none
- State: DEFINED.

### UC-PLT-N07 Redact telemetry in the browser
- Capability: N, PF-14
- Actors and permission: frontend runtime.
- Preconditions: telemetry enabled.
- Input: events, errors.
- Output: events without tokens, emails or content.
- Main flow: the observability package redacts before sending.
- No-op / rejection / concurrency / retry: unknown fields are dropped.
- UI states: n/a.
- Rules: PFREQ105; AGENTS observability protocol.
- Acceptance: Given an error holding a token, When reported, Then the token is masked.
- Evidence: BE n/a · FE `frontend/packages/foundation/observability/src/telemetry/` · Design n/a · Tests `observability.unit.test.ts` · Real e2e none
- State: IMPLEMENTING.

## Capability O: Architecture/dependency enforcement

### UC-PLT-O01 Fail the build on a forbidden dependency
- Capability: O, PF-15 Architecture and dependency enforcement
- Actors and permission: CI.
- Preconditions: a code change.
- Input: source and manifests.
- Output: failing check for a forbidden backend reference or frontend package edge.
- Main flow: `Notrelix.Architecture.Tests` and `pnpm check:architecture`.
- No-op / rejection / concurrency / retry: generated boundaries are regenerated, not hand-edited.
- UI states: n/a.
- Rules: PFREQ107 to PFREQ113; NRX-006.
- Acceptance: Given Domain referencing Infrastructure, When CI runs, Then the architecture test fails.
- Evidence: BE `backend/tests/Notrelix.Architecture.Tests/` · FE `frontend/docs/generated/package-boundaries.md` · Design n/a · Tests as listed · Real e2e none
- State: IMPLEMENTING — mapping of tests to requirements is a certification task.

## Capability P: CI, container, security and packaging evidence

### UC-PLT-P01 Run code checks before every push
- Capability: P, PF-16 CI, container, security and packaging evidence
- Actors and permission: developer and agent through the pre-push hook.
- Preconditions: hook installed (`make hooks-install`).
- Input: pushed commits.
- Output: the touched stacks' CI code checks run locally; a failure blocks the push.
- Main flow: `scripts/pre-push.sh` runs checks per touched folder.
- No-op / rejection / concurrency / retry: untouched stacks are skipped.
- UI states: n/a.
- Rules: PFREQ114.
- Acceptance: Given a failing frontend check, When pushing, Then the push is blocked.
- Evidence: BE n/a · FE n/a · Design n/a · Tests `scripts/pre-push.sh` · Real e2e none
- State: IMPLEMENTING — local only; the exact-head CI result is separate evidence.

### UC-PLT-P02 Produce container and packaging evidence
- Capability: P, PF-16
- Actors and permission: CI.
- Preconditions: a release candidate.
- Input: candidate commit.
- Output: CodeQL, CI, container build and package checks on that commit.
- Main flow: workflows run on the head commit.
- No-op / rejection / concurrency / retry: a skipped job is reported as unrun, not green.
- UI states: n/a.
- Rules: PFREQ114 to PFREQ120; NRX-016.
- Acceptance: Given a candidate, When CI ends, Then each required job has a result on that commit.
- Evidence: BE CI workflows · FE CI workflows · Design n/a · Tests CI · Real e2e none
- State: IMPLEMENTING — available evidence, not a formal PF-16 certification.

# 4. Product decisions applied

- Product decision (owner, 2026-10-26): mobile uses a bearer token and a rotating refresh token in the body, selected by `X-Notrelix-Client: mobile`; the web stays on cookie plus CSRF; tokens live only in Keychain or Keystore; offline on mobile is a read cache; Reports and Automation are read-only on mobile. Used by L03, L02.
- Lead decision (2026-10-30): readiness (`/health/ready`) answers 503 when any check tagged `ready` is Unhealthy and 200 for Healthy or Degraded; `/health` answers the same status code; `/health/live` never depends on a dependency (N02, N03, N04).
- Lead decision (2026-10-30): a startup probe `/health/startup` is added and answers non-2xx until the role guard and option validation pass (N06).
- Lead decision (2026-10-30): outbox batch size, processing timeout, retry limit and backoff cap become validated options with the current values as defaults (G02, E03).
- Lead decision (2026-10-30): operator actions on dead letters (list, requeue, discard with a reason, bounded replay) run under a platform operator system principal, are audited, and are never exposed to workspace users (H05).
- Lead decision (2026-10-30): web offline policy: reads stay on cached data with an offline banner; writes are not queued in v1; a write attempted offline fails with a clear message and keeps the user's input for retry (L02).
- Lead decision (2026-10-30): a consumer that needs ordered delivery is blocked until its owner decides a stream identity and a sequence producer; the default stays "order does not matter" with a written reason (H03, H04).
- Lead decision (2026-10-30): every background job lists its tenant scope, its batch bound and its occurrence identity in its own class comment, and logs a start and end record with counts (G06, G07).

# 5. Canonical updates required

For `backend/docs/architecture/platform-and-messaging.md`:

- State the readiness, full-health and startup status-code contract of Lead decision 2 and 3.
- State that outbox dispatch settings are validated options.
- State the operator dead-letter contract of Lead decision 5 under the dead-letter and replay sections.
- State that each background job documents tenant scope, batch bound and occurrence identity.

For `frontend/docs/architecture/realtime.md` and `frontend/docs/architecture/state-query-mutations.md`: state the web offline policy of Lead decision 6.

For `docs/workstreams/executions/platform-foundation/platform-foundation.spec.md` (posture table, section 5): mark the stale processing claim recovery and the `ui-tokens` `./css` export as present in source.

# 6. Not covered

| Source section | Reason |
|---|---|
| Team spec sections 1 to 5 (purpose, ownership, non-scope, boundaries) | Context definition; no behaviour. |
| Team spec "Known source debt", "Forbidden fixes", "Required evidence" per lane | Execution guidance, not use cases; their debts appear as gaps above. |
| PF-04 aggregate and persistence details beyond the data session | Owned by each context's persistence; D01 to D04 cover the shared mechanism. |
| Transport adapters (`InMemoryTransportAdapter`, `NullTransportAdapter`), compatibility evaluators, upcasters, governance policies in `Notrelix.Platform` | Outside the production delivery graph today (Part 1.2); they become use cases when wired. |
| Replay throttle and schedule occurrence identity | Part of H05 and G06; no separate behaviour exists yet. |
| UI primitive inventory (PF-13 beyond tokens) | Visual primitives are covered by design fidelity work of each product context, not a system use case. |
| Backend closure pack (`tac-v26.execution-status.md`, plan, tests) | Historical execution evidence; the closure spec and certification are cited, the status log is not turned into use cases. |
| Account, workspace and session product meaning | Owned by [identity](identity.md), [accounts](accounts.md) and workspaces catalogs; Platform covers transport only. |
