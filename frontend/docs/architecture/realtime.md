---
document_id: FE-ARCH-REALTIME
document_type: architecture
status: active
owner: frontend-platform
applies_to:
  - frontend-realtime
  - realtime-transport
  - realtime-recovery
  - realtime-subscriptions
  - realtime-product-reconciliation
  - realtime-workspace-lifecycle
evidence:
  - artifacts/contracts/realtime.v1.json
  - frontend/packages/foundation/realtime/
  - frontend/packages/runtimes/web/
  - frontend/packages/runtimes/mobile/
  - frontend/apps/web/src/providers/realtime-lifecycle.tsx
  - frontend/apps/web/src/realtime/
  - frontend/packages/product/
  - frontend/packages/features/
review_on:
  - realtime-protocol-change
  - realtime-envelope-change
  - reconnect-policy-change
  - sequence-recovery-change
  - workspace-subscription-change
  - product-reconciliation-change
  - realtime-authentication-change
---

# Realtime

> **Realtime is a delivery and reconciliation mechanism, not an independent product database.**
>
> The generic realtime foundation owns connection/protocol mechanics. Host/runtime composition owns lifecycle. Product/feature adapters own event interpretation and cache/state reconciliation. Duplicate, stale, out-of-order, reconnect, gap, principal change, and Workspace transition are normal operating conditions and must be handled explicitly.

This document is the canonical frontend owner for:

- realtime transport responsibility;
- connection state;
- connection descriptors/authentication handoff;
- reconnect policy;
- heartbeat/liveness;
- message validation;
- event identity/deduplication;
- sequence tracking;
- gap detection and recovery;
- Workspace/subscription filtering;
- host lifecycle;
- product/feature realtime adapters;
- REST/query/realtime convergence;
- principal/Workspace transition;
- telemetry and failure behavior;
- realtime tests and stop conditions.

It does not own:

- backend event production/delivery internals;
- public event schema ownership;
- backend authorization;
- product business invariants;
- generic query-key architecture;
- UI presentation.

---

# 1. Realtime architecture objective

The intended direction is:

```text
backend/system realtime producer
        ↓
versioned realtime contract
        ↓
generic frontend protocol/transport
        ↓
host runtime lifecycle
        ↓
Workspace/subscription dispatch
        ↓
product/feature adapter
        ↓
owning query/state reconciliation
        ↓
UI
```

No layer should create a second authoritative resource model.

---

# 2. FE-RT-001 — Realtime supplements backend authority

A realtime message can:

```text
patch
invalidate
notify
trigger recovery
```

frontend state.

It does not make the client more authoritative than the backend resource contract.

---

# 3. Wire contract

Realtime wire shape is producer-owned.

Current generated contract input includes:

```text
artifacts/contracts/realtime.v1.json
```

through the frontend codegen pipeline.

---

# 4. FE-RT-002 — Event wire shape is generated/producer-owned

Do not invent a permanent client-only event type when the server producer contract should define it.

If a wire event is missing or ambiguous:

```text
stop
resolve producer contract
regenerate
```

---

# 5. Generic realtime foundation

Current package:

```text
@notrelix/realtime
```

exports:

```text
protocol
ReconnectPolicy
RealtimeClient
connection/subscription/recovery types
typed event contracts
```

as current implementation evidence.

---

# 6. FE-RT-003 — Foundation owns mechanism, not product mutation meaning

Generic realtime foundation MAY own:

```text
socket lifecycle
message parsing
deduplication
sequence detection
subscription filtering
heartbeat
reconnect
telemetry hooks
```

It MUST NOT own:

```text
how Board Item state changes
how Documents cache updates
how Billing entitlement refreshes
```

---

# 7. Runtime composition

`RealtimeClient` is intended to be instantiated by the application runtime composition root rather than as a module-level singleton.

Current package source says this explicitly.

---

# 8. FE-RT-004 — Realtime client is lifecycle-owned

Do not create independent realtime clients inside:

```text
route components
product hooks
feature modules
```

unless a separate bounded connection is explicitly designed.

Host/runtime composition owns the normal application connection.

---

# 9. Connection states

Current transport models states:

```text
idle
connecting
connected
reconnecting
offline
closed
failed
```

The state machine is current implementation evidence.

---

# 10. FE-RT-005 — Connection state is explicit

UI/runtime behavior SHOULD distinguish at least meaningful states such as:

```text
connected
reconnecting
offline
failed
```

when product UX depends on freshness.

Do not reduce every non-connected state to a generic boolean if recovery semantics differ.

---

# 11. Connect context

Current connection context carries:

```text
sessionGeneration
```

rather than exposing credential storage directly to product subscribers.

---

# 12. FE-RT-006 — Connection authentication is runtime/session-owned

Product packages MUST NOT:

```text
read auth cookies
read refresh tokens
read secure mobile token storage
```

to create sockets directly.

Runtime/session composition supplies the approved connection descriptor/capability.

---

# 13. Connection descriptor

Current transport accepts a descriptor provider returning:

```text
url
protocols
```

for a connection context.

This keeps credential/transport construction replaceable.

---

# 14. FE-RT-007 — Connection descriptor is an outer adapter

The generic client asks for a connection descriptor.

It SHOULD NOT know host-specific credential storage or navigation.

---

# 15. Cookie web connection

Current default provider can use an explicit realtime URL with browser cookie-based connection behavior.

Mobile can use a different descriptor/provider mechanism.

---

# 16. FE-RT-008 — Web/mobile connection mechanism may differ while event semantics remain shared

Do not force browser-cookie connection code into native-safe product/foundation logic.

Share:

```text
protocol
event contract
subscription contract
reconciliation semantics
```

where safe.

---

# 17. Explicit URL

Current `RealtimeClient` requires an explicit realtime URL from runtime composition.

---

# 18. FE-RT-009 — Realtime endpoint is runtime configuration

Do not hardcode environment WebSocket URLs inside product/feature packages.

Host/runtime environment owns endpoint selection.

---

# 19. Socket factory

Current transport injects a WebSocket factory.

This supports host/runtime differences and deterministic tests.

---

# 20. FE-RT-010 — Transport construction has explicit test seams

Prefer injected:

```text
socket factory
clock
scheduler
descriptor provider
telemetry
```

over global monkey patches.

---

# 21. Reconnect

Current client implements bounded/configurable reconnect settings with exponential delay.

Another exported `ReconnectPolicy` also models configurable backoff.

The exact current numeric delays are implementation evidence, not architecture constants.

---

# 22. FE-RT-011 — Reconnect policy is bounded and observable

Reconnect behavior MUST define:

```text
when retry starts
delay/backoff
maximum/cap behavior
manual disconnect behavior
failed state
```

and emit enough diagnostics for operational analysis.

---

# 23. Deterministic reconnect

Current client intentionally uses deterministic exponential backoff without jitter in its freeze-v1 implementation.

That supports deterministic tests.

Production jitter policy is a mechanism decision, not product semantics.

---

# 24. FE-RT-012 — Reconnect tuning is not product state

Changing:

```text
initial delay
maximum delay
attempt count
jitter
```

does not change Board/Document semantics.

Keep it in transport/runtime policy.

---

# 25. Manual disconnect

Current `disconnect()` marks manual close, stops heartbeat/reconnect timer, closes socket, and transitions to closed.

---

# 26. FE-RT-013 — Manual lifecycle transition does not auto-reconnect

Logout, scope shutdown, disposal, or deliberate disconnect MUST NOT immediately trigger automatic reconnect using stale context.

---

# 27. Disposal

Current `dispose()`:

```text
disconnects
clears subscribers
clears state/recovery listeners
clears sequence tracker
clears dedup cache
```

as implementation evidence.

---

# 28. FE-RT-014 — Disposal removes retained realtime state

A disposed runtime MUST NOT retain:

```text
Workspace subscriptions
old sequence watermarks
event dedup memory
principal listeners
```

that can affect a future independently created runtime.

---

# 29. Heartbeat

Current transport uses ping/pong heartbeat and closes after configured missed-pong threshold.

---

# 30. FE-RT-015 — Liveness detection is transport responsibility

Product adapters do not implement their own ping timers.

Connection health remains in realtime transport/runtime.

---

# 31. Heartbeat timeout

Heartbeat timeout indicates transport liveness failure.

It does not imply a business operation failed.

---

# 32. FE-RT-016 — Connection failure and business failure are distinct

Do not show:

```text
"your mutation failed"
```

solely because the realtime heartbeat failed after the REST command may have committed.

Use authoritative command/query status.

---

# 33. Message validation

Current client parses incoming data through `parseRealtimeMessage()` before dispatch.

Invalid messages are reported and ignored.

---

# 34. FE-RT-017 — Untrusted realtime input is validated before product dispatch

Do not cast:

```ts
JSON.parse(data) as SomeEvent
```

and pass it directly into product state.

Validate envelope/control-message structure first.

---

# 35. Control messages

Current transport distinguishes:

```text
control
domain
```

messages and handles pong as a transport control message.

---

# 36. FE-RT-018 — Control protocol does not leak into product adapters

Product adapters consume domain envelopes.

They SHOULD NOT need to know ping/pong/reconnect transport control mechanics.

---

# 37. Event identity

Current envelope handling uses:

```text
eventId
```

for deduplication.

---

# 38. FE-RT-019 — Event identity is stable enough for duplicate suppression

Duplicate delivery is expected.

Do not apply the same event twice merely because the callback fired twice.

Use producer event identity according to contract.

---

# 39. Deduplication

Current client uses a bounded TTL cache for event IDs.

Current defaults are implementation tuning.

---

# 40. FE-RT-020 — Dedup memory is bounded

A long-lived client MUST NOT retain every historical event ID forever.

Bound by:

```text
time
count
or
another explicit strategy
```

while preserving required duplicate protection.

---

# 41. Dedup limitation

A bounded TTL cache only protects duplicates within its retention window.

Product reconciliation should still prefer idempotent transformations where possible.

---

# 42. FE-RT-021 — Product event handling remains idempotent where practical

Do not rely solely on transport dedup.

Repeated application of a product event SHOULD either:

```text
be harmless
be rejected by version/order
or
trigger authoritative revalidation
```

according to contract.

---

# 43. Sequence tracking

Current transport tracks sequence per:

```text
workspaceId
+
subscriptionId/default
```

when the envelope has a sequence.

---

# 44. FE-RT-022 — Sequence scope matches producer ordering scope

Do not compare sequence numbers across unrelated:

```text
Workspace
subscription/channel
```

unless producer contract says they share one sequence domain.

---

# 45. Stale sequence

Current client ignores envelopes with sequence:

```text
<= previous sequence
```

and emits telemetry.

---

# 46. FE-RT-023 — Stale sequence is not applied to product state

An event known to be older/equal to the accepted sequence watermark MUST NOT overwrite newer client state.

---

# 47. Gap detection

Current client reports a gap when:

```text
received > previous + 1
```

and calls recovery listeners instead of dispatching the gap event.

---

# 48. FE-RT-024 — Gap means local incremental history is incomplete

After a detected sequence gap, the client MUST NOT continue assuming its incremental state is complete.

It requires an explicit recovery path.

---

# 49. Recovery strategies

Valid recovery can include, depending on contract:

```text
authoritative query invalidation/refetch
snapshot reload
server replay from checkpoint
resubscription with new watermark
connection re-establishment
```

The chosen strategy must prove continuation correctness.

---

# 50. FE-RT-025 — Recovery re-establishes both data truth and sequence continuity

A recovery is complete only when:

```text
authoritative data is re-established
+
future realtime events can be accepted from a known continuation point
```

Invalidating queries alone is insufficient proof if the sequence tracker remains on an unusable watermark.

---

# 51. Current web gap recovery

Current web lifecycle subscribes to transport recovery and calls `handleWorkspaceRecovery()`.

Current policy invalidates selected Workspace-related queries.

Current `RealtimeClient` source does not visibly reset/rebase the sequence tracker from that recovery callback.

---

# 52. FE-RT-026 — Current recovery continuation is UNRESOLVED until proven

Current evidence proves:

```text
gap detected
→ recovery callback
→ selected query invalidation
```

It does **not** by itself prove:

```text
sequence watermark rebased
or
missing sequence replayed
or
subscription re-established at a new checkpoint
```

Classification:

```text
UNRESOLVED
```

until an integration/unit test or additional source contract proves live continuation after a gap.

Do not certify gap recovery merely because invalidation executes.

---

# 53. Recovery query scope

Recovery invalidation must cover all product data that could be inconsistent due to the missed sequence domain.

A hard-coded partial set can become stale as event coverage grows.

---

# 54. FE-RT-027 — Recovery scope follows subscription/event ownership

If one subscription can mutate:

```text
Workspace members
abilities
notifications
Board state
```

recovery must revalidate every affected state owner or use a stronger authoritative snapshot contract.

Do not guess a permanent invalidation list at the host if product ownership changes.

---

# 55. Subscription filter

Current transport subscription filter includes:

```text
workspaceId
eventTypes?
subscriptionId?
```

---

# 56. FE-RT-028 — Workspace subscription is explicitly scoped

A subscriber MUST NOT receive/apply another Workspace's event merely because it shares an event type.

Workspace filtering is a correctness and tenant-safety property.

---

# 57. Subscription ID

A subscription/channel ID can further distinguish ordering/filter scope.

Use it according to producer contract.

---

# 58. FE-RT-029 — Subscription identity is not UI component identity

Do not generate arbitrary per-component subscription IDs if the producer semantics require a stable server channel/checkpoint identity.

---

# 59. Host lifecycle

Current web `RealtimeLifecycle`:

```text
observes auth/sessionGeneration
derives active Workspace from route
connects/disconnects runtime realtime
subscribes per active Workspace
registers recovery
dispatches through adapter registry
invalidates query keys
```

as current source evidence.

---

# 60. FE-RT-030 — Host coordinates connection/subscription lifecycle

The host owns:

```text
when authenticated runtime connects
which active Workspace is subscribed
when subscription is removed
when principal/scope transition rebinds
```

Product adapters own event meaning.

---

# 61. Session generation

Current web lifecycle reconnect effect depends on:

```text
isAuthenticated
sessionGeneration
workspaceId
```

and connection context carries session generation.

---

# 62. FE-RT-031 — Session generation prevents stale credential lifecycle reuse

When session identity/generation changes, realtime connection context MUST be re-established as required.

Do not continue a socket indefinitely under stale session assumptions.

---

# 63. Logout

Current lifecycle disconnects if no authenticated session/generation.

---

# 64. FE-RT-032 — Logout disconnects protected realtime before old principal state remains active

Protected Workspace subscriptions MUST NOT continue under a signed-out/new-principal UI.

Coordinate with state cache cleanup.

---

# 65. Workspace transition

Current web subscription effect is keyed by active Workspace.

Cleanup unsubscribes old event/recovery listeners.

---

# 66. FE-RT-033 — Workspace switch unbinds old Workspace subscriptions

After Workspace switch:

```text
old Workspace events
MUST NOT
mutate the newly active Workspace state
```

Transport filtering + lifecycle cleanup + scoped query keys work together.

---

# 67. Route-derived active Workspace

Current web host derives active Workspace from pathname.

This is navigation context, not server authorization.

---

# 68. FE-RT-034 — Active route scope is subscription input, not permission authority

Backend realtime connection/subscription authorization still controls what events may be delivered.

Frontend route-derived Workspace only selects intended client scope.

---

# 69. Module adapter registry

Current web uses a `ModuleAdapterRegistry` that registers module adapters and dispatches an envelope to every adapter whose `supports()` returns true.

---

# 70. FE-RT-035 — Product adapters are explicit dispatch boundaries

A product/feature realtime adapter SHOULD declare:

```text
which events it supports
how it validates them
which owner it invalidates/updates
```

The host SHOULD NOT contain a growing `switch(eventType)` for every product context.

---

# 71. Adapter overlap

Current registry can dispatch one envelope to multiple supporting adapters.

This may be valid for cross-cutting consequences.

It requires intentional ownership.

---

# 72. FE-RT-036 — Multiple adapters handling one event must not create competing truth

If two adapters support one event, each should own distinct consequences such as:

```text
product cache
notification count
analytics signal
```

Do not let both independently mutate the same canonical resource representation.

---

# 73. Unknown event

Current web lifecycle records telemetry when no adapter handles an event.

Unknown events can occur during mixed-version rollout.

---

# 74. FE-RT-037 — Unknown event is observable and safely ignored/recovered

A newer server event SHOULD NOT crash the entire client.

Record enough diagnostic identity and use forward-compatible behavior.

If omission can make state unsafe, trigger broader revalidation according to contract.

---

# 75. Adapter validation failure

Adapter dispatch errors are reported by host telemetry.

One adapter exception should not corrupt transport state silently.

---

# 76. FE-RT-038 — Product adapter failure is isolated and observable

Do not allow one malformed event handler to:

```text
tear down all subscribers
silently stop future realtime
```

without explicit failure policy.

---

# 77. Query invalidation

Current adapter context exposes:

```text
invalidateQueries(keys)
```

rather than exposing arbitrary application internals.

This is a narrow reconciliation capability.

---

# 78. FE-RT-039 — Realtime adapter invalidates/updates through state-owner contracts

Avoid passing the entire host `QueryClient` to every product adapter if a narrower owner capability can preserve boundaries.

The current invalidation callback pattern is preferable to direct host coupling.

---

# 79. Direct patch versus invalidation

Event handlers may:

```text
patch cache
invalidate/refetch
```

depending on event completeness and ordering guarantees.

---

# 80. FE-RT-040 — Invalidate when event payload cannot prove a complete safe patch

Prefer authoritative refetch when:

```text
payload is partial
permissions may change
multiple resources changed
ordering/gap uncertainty exists
```

Do not guess missing state.

---

# 81. Event patch

A complete typed event with version semantics can patch the owning cache efficiently.

---

# 82. FE-RT-041 — Event patch observes version/order contract

Do not overwrite a newer REST/refetch result with an older realtime event.

Use:

```text
sequence
resource version
event timestamp only if authoritative
or
refetch
```

according to producer contract.

---

# 83. REST mutation overlap

A local mutation response and its realtime event can arrive in either order.

---

# 84. FE-RT-042 — REST and realtime converge on one state owner

Do not maintain:

```text
mutation-local resource
+
realtime resource
```

as separate long-lived truths.

Both reconcile the same scoped query/state owner.

---

# 85. Optimistic overlap

Optimistic state can exist when realtime event arrives.

Event application must not corrupt rollback or duplicate a command effect.

---

# 86. FE-RT-043 — Optimistic command and realtime event share logical identity where contract permits

Use:

```text
event ID
operation/command ID
resource version
idempotency correlation
```

where producer exposes them.

If reliable linkage is unavailable, prefer authoritative refetch over fragile dedup guesses.

---

# 87. Presence

Presence is often ephemeral and not durable resource truth.

It can use realtime-specific state.

---

# 88. FE-RT-044 — Presence state is explicitly ephemeral

Do not persist presence as durable product state unless product contract says so.

Presence expiration/disconnect semantics differ from document/comment persistence.

---

# 89. Collaboration

Documents/other collaborative capabilities can have product-specific realtime collaboration packages.

Generic transport remains foundation-owned.

---

# 90. FE-RT-045 — Collaboration package consumes realtime; realtime foundation does not import collaboration

Dependency direction remains inward.

---

# 91. Reconnect and resubscription

A socket reconnect does not automatically prove all server subscriptions/checkpoints were restored.

The protocol/runtime must define restoration.

---

# 92. FE-RT-046 — Reconnect is complete only after subscription/recovery contract is restored

`connected` transport state alone does not prove local product state is current.

Rebind/revalidate as required.

---

# 93. Offline

Current state machine supports `offline`.

Network-offline state can pause/retry connection.

---

# 94. FE-RT-047 — Offline is a freshness condition

UI MAY show stale cached data with degraded/reconnecting indication according to product UX.

Do not present offline stale data as freshly confirmed server state.

---

# 95. Background mobile lifecycle

Mobile can suspend networking when backgrounded.

Runtime-mobile owns platform lifecycle adaptation.

---

# 96. FE-RT-048 — Native lifecycle does not leak into product adapters

Product adapters SHOULD receive:

```text
events
recovery/revalidation signals
```

not Expo AppState APIs.

---

# 97. Telemetry

Current realtime client tracks/report errors for:

```text
duplicate ignored
stale sequence
heartbeat timeout
socket error
parse failure
listener failure
```

and web lifecycle tracks unknown event/recovery errors.

---

# 98. FE-RT-049 — Realtime telemetry uses safe identifiers

Useful fields include:

```text
event type
event ID
Workspace ID when allowed
expected/received sequence
connection state
```

Do not log:

```text
auth token
private document payload
API key
raw sensitive event body
```

by default.

---

# 99. Listener isolation

Current client catches listener exceptions and reports telemetry.

---

# 100. FE-RT-050 — Subscriber exception does not stop unrelated subscribers

Each listener/adapter failure should be isolated where feasible.

Realtime fan-out must remain resilient.

---

# 101. Event filtering

Current transport filters by:

```text
Workspace
subscription ID
event types
```

before invoking listener.

---

# 102. FE-RT-051 — Filter is a client optimization/correctness guard, not server authorization

The server MUST still authorize event delivery/subscription.

Client filtering does not make an over-broad stream secure.

---

# 103. Permission revocation

A user can lose permission while connected.

The frontend must handle server disconnect/event/query authorization changes.

---

# 104. FE-RT-052 — Permission change invalidates previously visible state

On authoritative revocation signal/result:

```text
stop applying protected events
invalidate/clear protected data
update navigation/UX
```

as applicable.

Do not leave stale protected data indefinitely.

---

# 105. Workspace removal

If current user loses Workspace membership, current subscription/route becomes invalid.

Host/state/realtime must coordinate exit.

---

# 106. FE-RT-053 — Membership loss is a scope transition

Do not continue reconnecting/subscribing to a Workspace the user no longer owns/accesses.

Resolve current Workspace and route through authoritative state.

---

# 107. Event schema evolution

New optional event fields/types can appear during mixed-version rollout.

Adapters should validate supported versions/types.

---

# 108. FE-RT-054 — Forward compatibility avoids total client failure on unknown event type

Unknown event:

```text
telemetry
safe ignore or revalidation
```

according to risk.

Do not throw at the transport root for every unknown type.

---

# 109. Breaking event change

Removing/renaming required event fields or changing semantics is contract breaking.

---

# 110. FE-RT-055 — Realtime contract migration considers mixed client/server versions

Old client/new server and new client/old server compatibility must be assessed.

Realtime can stay connected across deployment boundaries.

---

# 111. Event replay

If server supports replay, checkpoint semantics must be explicit.

Do not infer replay support from sequence number existence alone.

---

# 112. FE-RT-056 — Sequence does not automatically imply replay

A gap callback must know whether the system can:

```text
request missing events
or
must reload authoritative snapshot
```

from producer contract.

---

# 113. Sequence reset

Workspace/new subscription/server epoch may reset sequence.

The protocol must define reset/rebase semantics.

---

# 114. FE-RT-057 — Sequence epoch changes are explicit

Do not compare a new server/subscription sequence epoch to an old retained watermark indefinitely.

Reset/rebase at the defined boundary.

---

# 115. Event time

Timestamps are useful diagnostics/order metadata only if producer contract defines their ordering authority.

---

# 116. FE-RT-058 — Wall-clock timestamp is not sequence substitute by default

Clock skew can exist.

Prefer sequence/version/checkpoint for ordering when provided.

---

# 117. Duplicate command/event

A server can publish an event after idempotent command replay.

Event identity should remain stable according to backend delivery contract where possible.

---

# 118. FE-RT-059 — Client dedup uses event identity, not payload equality

Do not stringify/compare full payloads as duplicate identity.

Use the producer's event ID.

---

# 119. Recovery UX

Gap/reconnect can trigger temporary stale/recovering UI.

Do not surface low-level sequence numbers to end users unless support UX explicitly needs them.

---

# 120. FE-RT-060 — Recovery UX describes freshness, not transport jargon

Prefer:

```text
"Reconnecting…"
"Refreshing latest changes…"
```

over internal protocol error details.

Keep detailed telemetry for diagnostics.

---

# 121. Realtime testing

Transport tests should cover:

```text
state transitions
manual disconnect
reconnect
heartbeat
parse rejection
dedup
stale sequence
gap
filtering
dispose
```

---

# 122. FE-RT-061 — Transport properties have deterministic unit tests

Injected clock/scheduler/socket factory SHOULD be used to prove timing/state without flaky real-time waits.

---

# 123. Adapter tests

Product adapters should test:

```text
supports
validation
cache/state outcome
unknown/malformed event
duplicate/order where relevant
```

---

# 124. FE-RT-062 — Adapter test proves product consequence, not only handler call

Assert final query/state invalidation/patch.

Do not stop at:

```text
callback invoked
```

---

# 125. Recovery tests

Gap recovery requires a continuation test.

---

# 126. FE-RT-063 — Gap recovery test proves next valid live event can be consumed

A complete test should demonstrate:

```text
accepted event N
gap detected at N+k
recovery executes
authoritative state restored
continuation watermark established
later event accepted exactly once
```

Without this, recovery is not fully certified.

---

# 127. Workspace transition tests

Test old Workspace event after switch.

---

# 128. FE-RT-064 — Cross-Workspace negative test is mandatory for lifecycle changes

Prove:

```text
switch A → B
late A event
→ B cache unchanged
```

Positive B updates alone are insufficient.

---

# 129. Session transition tests

Test sign-out/session generation replacement against connection/subscription lifecycle.

---

# 130. FE-RT-065 — Old-session socket cannot remain authoritative

After session transition:

```text
old connection/subscription
→ disconnected/ignored
```

and new session establishes its own context.

---

# 131. Integration/E2E

Use integration/E2E when proving actual backend realtime compatibility.

Unit transport tests cannot prove server contract delivery.

---

# 132. FE-RT-066 — Mock socket success does not prove backend realtime compatibility

Claim scope accurately.

A fake socket test proves client behavior only.

---

# 133. Realtime architecture change

Changes to:

```text
connection ownership
ordering/gap model
subscription scope
event identity
product adapter model
recovery strategy
```

can be consequential architecture changes.

---

# 134. FE-RT-067 — Recovery/order architecture change is not a local refactor

Update:

```text
canonical docs
producer/client contract if affected
source
tests
telemetry
migration/compatibility
```

and ADR if required.

---

# 135. Source drift

Source is evidence, not automatic precedent.

If current recovery/adapter behavior conflicts with this architecture, classify before copying it.

---

# 136. FE-RT-068 — Existing realtime shortcut is not precedent

Examples:

```text
host hard-coded invalidation
missing sequence reset
broad unvalidated cast
```

must be reviewed as possible source debt/unresolved behavior.

---

# 137. New realtime event checklist

```text
[ ] producer event contract
[ ] generated type
[ ] event identity
[ ] Workspace/subscription scope
[ ] ordering/version semantics
[ ] product/feature owner
[ ] adapter supports/validation
[ ] query/state consequence
[ ] optimistic overlap
[ ] duplicate handling
[ ] gap/recovery impact
[ ] tests
```

---

# 138. Reconnect change checklist

```text
[ ] connect context
[ ] retry/backoff
[ ] manual disconnect
[ ] offline behavior
[ ] session generation
[ ] Workspace subscription restoration
[ ] sequence/checkpoint restoration
[ ] heartbeat
[ ] telemetry
[ ] deterministic tests
```

---

# 139. Recovery change checklist

```text
[ ] gap scope
[ ] affected state owners
[ ] authoritative rehydrate
[ ] sequence/checkpoint rebase
[ ] subscription continuation
[ ] duplicate handling
[ ] pending mutation overlap
[ ] test next event after recovery
```

---

# 140. Stop conditions

Stop implementation if:

- product code creates its own app-wide socket;
- a raw unvalidated socket message mutates product state;
- duplicate event IDs can be applied repeatedly;
- stale sequence can overwrite newer state;
- gap is logged but incremental processing continues as if complete;
- recovery invalidates data but continuation watermark/checkpoint is unknown;
- Workspace switch leaves old subscription active with no isolation proof;
- realtime code reads auth secrets from product packages;
- host registry becomes one giant product event switch;
- two adapters own competing copies of the same resource;
- reconnect marks state “current” without subscription/recovery restoration;
- event timestamp is used as ordering authority without producer contract;
- an unknown event crashes the entire realtime transport;
- server authorization is replaced by client Workspace filtering.

---

# 141. Executable evidence

Primary current evidence:

```text
frontend/packages/foundation/realtime/
frontend/apps/web/src/providers/realtime-lifecycle.tsx
frontend/apps/web/src/realtime/
frontend/packages/runtimes/web/
frontend/packages/runtimes/mobile/
frontend/packages/product/
frontend/packages/features/
artifacts/contracts/realtime.v1.json
frontend realtime tests
```

Current `RealtimeClient` visibly implements:

```text
connection state
heartbeat
dedup
sequence tracking
gap callback
subscription filtering
listener isolation
dispose
```

Current web lifecycle visibly implements:

```text
session-driven connect/disconnect
Workspace subscription
adapter dispatch
query invalidation
recovery callback
```

---

# 142. Related architecture

Read:

```text
api-and-contracts.md
state-query-mutations.md
hosts-composition-routing.md
dependency-boundaries.md
testing-and-quality-gates.md
architecture-change-policy.md
```

---

# 143. Related backend/system authority

Read:

```text
docs/architecture/events-realtime-and-delivery-boundary.md
docs/architecture/data-ownership-and-consistency.md
backend/docs/architecture/platform-and-messaging.md
backend/docs/architecture/api-and-contracts.md
```

for producer/delivery semantics.

---

# 144. Explicit non-responsibilities

This document does not define:

```text
backend outbox/message delivery implementation
business aggregate event creation
exact product query keys
visual reconnect component
server authorization policy
```

It defines client realtime transport/lifecycle/reconciliation boundaries.

---

# 145. Final realtime model

The target client model is:

```text
SERVER CONTRACT
        ↓
validated envelope
        ↓
dedup + sequence/gap guard
        ↓
Workspace/subscription filter
        ↓
product/feature adapter
        ↓
owning scoped state/cache
        ↓
UI
```

with lifecycle:

```text
session established
→ connect
→ subscribe active scope
→ apply/reconcile events
→ detect duplicate/stale/gap
→ recover authoritatively
→ re-establish continuation
→ unsubscribe/disconnect on scope/principal change
```

Realtime is successful when it makes the product feel live without creating a second source of truth, leaking events across Workspaces, hiding gaps, or assuming transport arrival order is business truth.
