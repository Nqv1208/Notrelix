---
document_id: SYS-EVENTS-REALTIME-DELIVERY
document_type: architecture
status: active
owner: system-architecture
applies_to:
  - repository
  - backend
  - frontend
  - domain-events
  - integration-events
  - messaging
  - realtime
  - activity
  - audit
evidence:
  - RULE.md
  - docs/architecture/system-overview.md
  - docs/architecture/bounded-context-map.md
  - docs/architecture/contract-boundaries.md
  - docs/architecture/data-ownership-and-consistency.md
  - backend/src/Notrelix.Domain/
  - backend/src/Notrelix.Application/
  - backend/src/Notrelix.Platform/
  - backend/tests/Notrelix.Platform.Tests/
  - backend/tests/Notrelix.Integration.Tests/
  - frontend/docs/architecture/realtime.md
  - frontend/docs/architecture/state-query-mutations.md
review_on:
  - domain-event-model-change
  - integration-event-change
  - message-envelope-change
  - realtime-protocol-change
  - outbox-or-delivery-change
  - audit-or-activity-model-change
  - consumer-identity-change
  - ordering-or-replay-change
  - service-extraction-change
---

# Events, Realtime, and Delivery Boundary

> **Events describe facts. Delivery moves facts. Realtime refreshes clients. Activity informs users. Audit proves governed actions.**
>
> These concerns are related, but they are not the same contract and they must not collapse into one generic “event system”.

This document is the canonical cross-stack owner for the semantic boundary between:

```text
Domain event
Integration/public event
Outbox record
Message envelope
Consumer delivery state
Realtime logical event
Activity
Audit
Notification
```

It defines what each concept means, who owns it, when one maps into another, and how identity, scope, ordering, retry, compatibility, replay, and recovery work across the system.

It does **not** own exact Domain dispatch implementation, exact Platform persistence/schema, broker topology, frontend connection library, notification UI, or audit storage/retention mechanics.

---

# 1. Why this boundary exists

One successful product operation can legitimately create several artifacts:

```text
business mutation
→ Domain event
→ Integration/public event
→ Outbox record
→ Message envelope
→ Consumer execution
→ Realtime notification
→ Activity
→ Audit where required
```

They may originate from one action, but they have different audiences, lifecycles, retention, reliability, and compatibility obligations.

A generic “event” abstraction must not erase those differences.

---

# 2. System taxonomy

| Concept | Primary purpose | Primary owner |
|---|---|---|
| Domain event | Completed business fact inside Domain/Application processing | Product context + Domain |
| Integration/public event | Durable cross-boundary fact for independent consumers | Product/system contract owner |
| Outbox record | Reliably enroll committed facts for later delivery | Platform delivery |
| Message envelope | Runtime metadata around a logical contract | Platform |
| Consumer delivery state | Dedup/retry/order/dead-letter execution state | Platform |
| Realtime logical event | Client freshness/reconciliation contract | System contract + frontend realtime |
| Activity | User-facing collaborative history/feed | Collaboration |
| Notification | User-facing attention semantic | Collaboration/product |
| Audit | Governed security/administrative evidence | Governance/security |
| Operational log/trace | Runtime diagnosis | Operations |

---

# 3. SYS-EVT-001 — Events describe facts, not hidden commands

Prefer past-tense facts:

```text
BoardItemMoved
WorkspaceMemberRemoved
IntegrationConnectionDisabled
SubscriptionPlanChanged
```

Avoid disguised commands:

```text
MoveBoardItem
DeleteWorkspace
SendEmailNow
```

unless the committed product fact is actually that a request now exists.

Commands ask for change. Events report facts.

---

# 4. Requested facts

Facts such as:

```text
ExportRequested
ApprovalRequested
SyncRequested
```

are valid when “request accepted/created” is itself durable product state.

The test is semantic, not grammatical:

> Did the system commit a fact that the request exists?

---

# 5. SYS-EVT-002 — Success event follows state success

A success event MUST NOT be emitted for:

- rejected mutation;
- failed authorization;
- failed invariant;
- rolled-back transaction;
- semantic no-op where no product fact changed.

If input is normalized before commit, event payload must describe the normalized committed fact, not stale/raw input.

---

# 6. Domain event

A Domain event is:

> a business fact emitted by a successful Domain transition for local Domain/Application processing.

Properties:

- uses product/domain vocabulary;
- follows successful mutation;
- is internal to Domain/Application processing;
- may be mapped to other contracts;
- is not automatically durable/public;
- is not automatically the schema used by broker or client.

---

# 7. Domain event ownership

The product context owns the meaning.

Domain owns the local representation/emission semantics.

Example:

```text
Work Management
    owns the meaning of BoardItem moved

Domain
    owns local BoardItemMoved representation
```

Platform never becomes the semantic owner merely because it later transports bytes.

---

# 8. SYS-EVT-003 — Domain event is transport-free

Domain events MUST NOT depend on:

- broker envelope;
- queue/topic;
- websocket payload;
- provider DTO;
- retry/dead-letter metadata.

Transport adapters/mappers belong outside Domain.

---

# 9. Domain-event evolution

Domain event shapes may evolve with local Domain code when:

- external contracts are mapped separately;
- local consumers migrate coherently;
- no persisted/public contract depends on the internal type.

Do not freeze Domain modeling because one external consumer needs compatibility.

---

# 10. Semantic no-op

If an operation results in no product-visible state change:

```text
no authoritative mutation
no success Domain event
```

unless the product explicitly owns a separate “attempted/requested” fact.

---

# 11. Integration/public event

An Integration event is:

> a durable cross-boundary contract representing a committed product fact for independently changing consumers.

Typical consumers:

```text
another bounded context
Automation
Analytics
Integrations
background processor
external/extracted service
```

It has stronger compatibility obligations than a Domain event.

---

# 12. SYS-EVT-004 — Integration event is a governed contract

Integration/public events follow `contract-boundaries.md`.

They require:

```text
producer
semantic owner
known consumer classes
logical identity
version/compatibility
tenant/resource scope
stable message/event identity
backlog/replay implications
```

---

# 13. Mapping Domain to Integration

Allowed:

```text
Domain event
→ Application mapper/policy
→ Integration/public contract
```

Mapping may:

- rename;
- omit internals;
- add stable scope/resource IDs;
- add contract version;
- translate internal enums;
- aggregate several internal facts into a stable external fact.

Domain shape and public shape do not need to match.

---

# 14. No one-to-one requirement

A Domain event may have no Integration event.

An Integration event may represent an Application/workflow fact rather than one Domain event.

The requirement is stable semantic ownership, not one-to-one class mapping.

---

# 15. SYS-EVT-005 — Public event granularity follows stable product facts

Too coarse:

```text
EntityChanged
```

Too fine:

```text
every internal property setter becomes public event
```

Publish only facts with durable downstream meaning.

---

# 16. Outbox record

An outbox record is durable **delivery state**, not the product fact itself.

It may carry:

```text
contract payload
logical identity/version
message id
scope
claim state
delivery/retry metadata
```

Exact schema remains Platform/Infrastructure-owned.

---

# 17. SYS-EVT-006 — Outbox enrollment follows the source transaction contract

When downstream delivery must reliably represent committed state:

```text
business mutation
+
outbox enrollment
```

belong to the same durable transaction.

This avoids both:

```text
business committed
but delivery fact lost
```

and:

```text
delivery published
but business rolled back
```

---

# 18. Outbox is not event store

Outbox does not automatically provide:

- permanent event history;
- complete replayable business history;
- security audit;
- event sourcing;
- infinite retention.

Do not make product semantics depend on outbox retention by accident.

---

# 19. Message envelope

A message envelope carries runtime metadata around a logical contract.

Possible metadata:

```text
message id
logical contract name/version
producer identity
tenant/account/workspace scope
resource identity
correlation id
causation id
occurred/produced time
ordering stream/sequence
trace metadata
payload
```

Only fields required by the actual boundary should be mandatory.

---

# 20. SYS-EVT-007 — Logical identity is independent of class names

Public message/event identity MUST NOT depend only on CLR/TypeScript type names, namespace, or file path.

An internal refactor should not silently rename a public logical contract.

---

# 21. Producer identity

Producer identity should be stable enough for:

- routing;
- diagnosis;
- compatibility;
- extraction;
- security.

Prefer logical producer/context identity to transient process hostnames.

---

# 22. Message identity

Every retryable durable message needs stable identity.

Retries of the same logical delivery preserve that identity unless the product semantics create a genuinely new operation.

---

# 23. Consumer identity

Several consumers may process the same message independently.

Therefore consumer idempotency generally scopes by:

```text
message identity
+
consumer identity
```

not event name alone.

---

# 24. SYS-EVT-008 — Durable consumers assume duplicate delivery

At-least-once delivery is the safe baseline.

Consumers must tolerate:

```text
duplicate
retry
restart
redelivery
```

Exactly-once business effect is achieved through durable idempotent behavior, not broker optimism.

---

# 25. Delivery dedup versus business idempotency

These are different:

```text
delivery dedup
    suppress repeat processing of same delivered message

business idempotency
    suppress repeat side effect of same logical operation
```

One mechanism does not automatically solve both.

---

# 26. Ordering

Ordering is optional and scoped.

Possible streams:

```text
BoardItem
Workspace
provider connection
automation execution
subscription
```

Do not define global ordering where only per-resource order matters.

---

# 27. SYS-EVT-009 — Sequence advances after successful processing

When sequence state exists:

```text
receive N
→ validate
→ process and commit
→ advance to N
```

Never advance sequence before handler success.

---

# 28. Duplicate sequence

A successfully processed message received again:

- remains duplicate;
- must not repeat side effect;
- does not advance business state again.

---

# 29. Sequence gap

If the consumer expects `N` and receives `N+2`, certainty is lost.

Possible safe recovery:

```text
delay/replay
dead-letter
refetch authoritative state
rebuild projection
```

Do not silently continue when order is correctness-critical.

---

# 30. Order-independent consumers

Not every consumer needs sequence enforcement.

A latest-version projection may safely ignore old versions.

Do not pay global ordering cost when consumer semantics do not need it.

---

# 31. Failure taxonomy

Delivery failures should be classified:

```text
Transient
Terminal / deterministic invalid
Unknown / uncertain
Poison
Unsupported contract/version
```

Retry policy depends on the category.

---

# 32. Transient failure

Examples:

- temporary DB/network outage;
- provider throttling;
- temporary transport outage.

Retry may be appropriate with bounded backoff, idempotency, and observability.

---

# 33. Terminal failure

Examples:

- permanently invalid supported payload;
- removed target with no retry semantics;
- deterministic business rejection.

Retrying forever is not reliability.

---

# 34. Unknown external outcome

A provider timeout may mean:

```text
failure
or
provider accepted effect but response was lost
```

Unknown outcome is first-class.

Retry requires provider/business operation identity or reconciliation.

---

# 35. Poison message

A poison message is deterministically unprocessable **for a specific consumer** under current supported state/contract.

One consumer’s poison state does not automatically poison every other consumer.

---

# 36. SYS-EVT-010 — Poison handling does not replace compatibility governance

A wave of poison messages after deploy may mean:

- contract migration failure;
- unsupported backlog;
- consumer version drift.

Do not classify architecture incompatibility as ordinary poison traffic.

---

# 37. Dead-letter

Dead-letter records retain enough identity for safe diagnosis/replay:

```text
message
consumer
contract/version
scope
failure class
attempt metadata
correlation
```

Avoid storing raw secrets/sensitive payload unnecessarily.

---

# 38. Manual replay

Replay semantics must define:

- whether message identity is preserved;
- dedup reset/override;
- compatibility reader;
- tenant scope;
- external-side-effect safety.

Do not clone the message with a fresh ID merely to bypass idempotency.

---

# 39. Post-commit work

Effects requiring successful durable commit run only after commit.

Examples:

- integration publication;
- realtime signal;
- email/provider effect;
- downstream invalidation.

Rolled-back state must not produce a success effect.

---

# 40. SYS-EVT-011 — Platform owns delivery mechanics, not business meaning

Platform may own:

```text
envelope
outbox
claim
retry
dedup
ordering
dead-letter
poison detection
background execution runtime
```

Product/Application owns:

```text
which fact matters
which operation is valid
consumer business behavior
provider/business retry admissibility
```

---

# 41. Tenant context in consumers

Tenant-scoped background consumers establish explicit tenant/RLS context before protected data access.

They must not depend on ambient HTTP request state.

---

# 42. Correlation and causation

Correlation/causation can connect execution:

```text
request
→ business fact
→ automation
→ provider effect
```

They support observability.

They are not business ownership.

---

# 43. Realtime

Realtime is:

> a freshness and interaction delivery channel.

It is not:

- authoritative persistent state;
- audit history;
- complete event log;
- replacement for query/API recovery.

---

# 44. SYS-RT-001 — Realtime logical identity is stable

Realtime names/versions are contracts independent of internal class names.

---

# 45. SYS-RT-002 — Subscription scope is explicit and narrow

Subscribe at the narrowest meaningful secure boundary:

```text
Account
Workspace
resource
document
board
user-notification stream
```

Do not deliver all workspace data globally then filter only in UI.

---

# 46. Subscription ownership

Runtime/foundation owns connection mechanisms.

Product packages own how relevant facts reconcile into product state.

A generic realtime runtime should not understand all product objects.

---

# 47. Realtime event minimum

A logical realtime contract normally needs:

```text
event/message id
logical name/version
scope
resource identity
payload
version/sequence when required
approved time metadata
```

Do not include raw secrets/provider internals.

---

# 48. SYS-RT-003 — Realtime clients assume duplicate/out-of-order delivery

Unless stronger contract exists, clients assume:

```text
duplicate
out-of-order
gap
reconnect
```

Handlers must remain idempotent/version-aware or fall back to invalidation/refetch.

---

# 49. Bounded client dedup

Dedup mechanisms must be bounded:

- recent ID window;
- sequence/version;
- state version.

Never retain every historical event ID indefinitely on the client.

---

# 50. Client ordering

If an event is proven older than current state:

```text
ignore
```

If order cannot be proven:

```text
invalidate/refetch
```

Correctness outranks speculative patching.

---

# 51. Gap recovery

A sequence gap means the client cannot prove completeness.

Safe recovery:

```text
mark uncertainty if user-visible
→ refetch authoritative state
→ re-establish subscription
```

---

# 52. Reconnect

Reconnect restores:

- auth;
- subscriptions;
- scope;
- cache reconciliation.

Reconnect can replay/duplicate messages, so handlers remain replay-safe.

---

# 53. Heartbeat

Heartbeat/liveness belongs to runtime.

Product code may consume high-level connection state, not low-level transport details.

---

# 54. Workspace transition

On Workspace change:

- old subscriptions are disposed/re-scoped;
- late old-workspace events cannot patch new-workspace cache;
- pending HTTP/realtime work preserves initiating scope.

---

# 55. Mobile lifecycle

Native realtime accounts for:

- background/suspend;
- reconnect;
- platform network changes;
- push/notification alternatives where applicable.

Do not reuse web-only runtime paths in mobile production.

---

# 56. SYS-RT-004 — Patch only when correctness can be proven

Patch query/cache state only when event carries enough:

```text
identity
scope
version/order
semantic payload
```

Otherwise invalidate/refetch.

---

# 57. Realtime invalidation hint

A valid realtime contract may simply mean:

```text
resource changed
→ refetch
```

Full snapshots are not required.

---

# 58. Realtime snapshot

Snapshot events may be used when:

- payload bounded;
- version semantics clear;
- sensitive fields controlled.

They still do not create persistent truth.

---

# 59. Optimistic mutation race

Typical race:

```text
optimistic patch
→ server commit
→ realtime arrives
→ HTTP result arrives
```

Frontend reconciliation must avoid double apply and stale rollback.

Exact mechanics remain frontend-owned.

---

# 60. Realtime authorization

Subscription and fan-out must enforce:

- principal;
- tenant/workspace;
- resource;
- current authorization policy.

Do not deliver unauthorized data and rely on UI filtering.

---

# 61. Deleted-resource realtime

Deletion/archive events need enough semantic identity to remove/invalidate client state even if the resource can no longer be fetched.

---

# 62. Activity

Activity is user-facing product history about meaningful collaborative actions.

It may be curated/aggregated.

It is not necessarily complete enough for compliance or replay.

Default product owner:

```text
Collaboration
```

---

# 63. SYS-ACT-001 — Activity is not transport history

Do not render raw outbox/broker retries as user history.

Technical duplicate delivery must not appear as repeated product action.

---

# 64. Activity source

Activity may derive from committed business facts or explicit post-commit activity recording.

Mapping must be product-aware.

---

# 65. Activity actor

Actor can be:

```text
user
automation
integration
system
```

Use product identity, not process hostname.

---

# 66. Activity target

Activity may reference resources across contexts by stable identity.

The source context retains resource ownership.

---

# 67. Notification

A notification is user-facing **attention semantic** caused by a relevant fact.

Delivery channels may be:

```text
in-app
email
push
```

The delivery mechanism does not define notification meaning.

---

# 68. SYS-NOTIF-001 — Notification semantic is separate from delivery provider

Example:

```text
Mention notification
```

is one product fact.

Email/push/in-app are delivery choices.

---

# 69. Notification idempotency

Technical retries must not create duplicate user attention.

Use stable notification/delivery identity.

---

# 70. Notification preference

Who should be notified is product/Application policy.

Platform only delivers an already-approved notification intent.

---

# 71. Audit

Audit is governed evidence for security-, administration-, policy-, or compliance-relevant actions/outcomes.

Default semantic owner:

```text
Governance/security policy
```

---

# 72. SYS-AUD-001 — Audit is not activity

Audit prioritizes governed completeness/integrity.

Activity prioritizes human usefulness.

They can derive from the same operation while remaining distinct contracts.

---

# 73. Audit outcome

Depending on policy, audit may record:

```text
attempted
allowed
denied
succeeded
failed
```

Domain success events normally describe successful facts only.

---

# 74. Audit retention

Audit may outlive:

- deleted resource;
- user-visible activity;
- source object.

Do not cascade-delete audit by convenience.

---

# 75. Operational logs/traces

Logs/traces diagnose execution.

They are not:

- Domain events;
- product activity;
- security audit by default.

Do not rely on ordinary log retention for audit compliance.

---

# 76. SYS-OBS-001 — Correlation links layers without merging them

Observability may correlate:

```text
request
Domain fact
integration message
consumer
realtime
provider call
```

This creates traceability, not one unified semantic contract.

---

# 77. Mapping matrix

| Source | May map to | Rule |
|---|---|---|
| Domain event | Integration event | Publish only stable downstream fact |
| Domain event | Activity | Human/product mapping |
| Domain/application outcome | Audit | Only governed actions/outcomes |
| Integration event | Consumer work | Durable cross-boundary reaction |
| Integration event | Realtime | Adapt if client contract differs |
| Outbox | Message envelope | Delivery representation |
| Provider webhook | Notrelix fact | Translate through Integrations |
| Realtime | Query/cache | Freshness/reconciliation |
| Activity | Notification | When attention semantics require it |

---

# 78. No blind generic fan-out

Do not automatically fan every Domain event to:

```text
broker
websocket
activity
audit
analytics
```

Each destination needs explicit mapping because payload, security, retention, and compatibility differ.

---

# 79. Payload design

Prefer:

- stable IDs;
- relevant product fact;
- enough state to consume safely.

Avoid:

- full aggregate dump;
- internal navigation objects;
- secrets;
- unnecessary PII;
- provider credentials.

---

# 80. Time semantics

Where needed distinguish:

```text
occurred time
produced time
delivered time
processed time
```

One timestamp must not be overloaded.

Wall-clock time is not global ordering unless contract says so.

---

# 81. Version semantics

Distinguish:

```text
contract schema version
aggregate/resource version
stream sequence
provider revision
```

They serve different purposes.

---

# 82. Deletion facts

A deletion event must reflect actual lifecycle:

```text
Deleted
Archived
Revoked
Disconnected
```

Do not call archive “deleted” for convenience.

Downstream consumers need reference/tombstone/query semantics.

---

# 83. Privacy

Events can propagate farther and live longer than request data.

Before adding payload fields review:

- consumer need;
- tenant isolation;
- sensitivity;
- backlog retention;
- deletion/privacy implications.

---

# 84. Provider webhook translation

Provider webhook payload is external contract.

Translate:

```text
provider event
→ validation/mapping
→ Integrations-owned fact
→ product propagation
```

Do not let provider schema become Notrelix ubiquitous language.

---

# 85. Analytics consumers

Analytics may consume stable product events.

Do not force source contexts to produce analytics-specific internal implementation events if durable business facts already exist.

---

# 86. Automation consumers

Automation triggers must depend on approved product/integration facts, not fragile internal Domain event shapes.

---

# 87. Collaboration consumers

Collaboration may map source facts into activity/notifications while preserving source resource ownership and security scope.

---

# 88. Service extraction

A stable logical event should survive moving producer from monolith to service.

Transport may change:

```text
in-process
→ broker/network
```

Product meaning should not.

---

# 89. In-process versus broker

Logical contract identity is independent from physical transport.

Do not name contracts after current queue/topic implementation.

---

# 90. Synchronous versus asynchronous

Use sync when target result is required now.

Use async when source can commit independently and lag/retry is acceptable.

Do not force every context interaction through events.

---

# 91. User-visible eventual work

Async business flow that matters to users must expose truthful state:

```text
pending
syncing
failed
completed
```

as appropriate.

Do not report final success before external/downstream effect is complete.

---

# 92. Backpressure

Plan for:

- queue backlog;
- provider throttling;
- consumer lag;
- bounded retry;
- rate limits.

A contract that requires instant processing needs an explicit reliability target.

---

# 93. Consumer lag

If lag can cause unsafe/stale decisions, define fallback or fail-closed behavior.

Especially avoid security authorization based on stale permissive projection without compensation.

---

# 94. Delivery observability

Critical delivery should expose states such as:

```text
enrolled
claimed
attempted
succeeded
retried
dead-lettered
replayed
```

with message + consumer identity.

---

# 95. Transport success versus business success

Message delivered does not mean business workflow succeeded.

Distinguish transport result from:

- authorization rejection;
- concurrency conflict;
- provider terminal failure;
- business no-op.

---

# 96. Contract compatibility

Integration/realtime evolution follows:

```text
docs/architecture/contract-boundaries.md
```

Do not break backlog/consumers by mutating meaning in place.

---

# 97. Data consistency

Outbox, dedup, ordering, projection consistency follows:

```text
docs/architecture/data-ownership-and-consistency.md
```

---

# 98. Backend ownership

Exact mechanics:

```text
backend/docs/architecture/domain-modeling.md
backend/docs/architecture/application-model.md
backend/docs/architecture/platform-and-messaging.md
backend/docs/architecture/security-tenancy-authorization.md
```

---

# 99. Frontend ownership

Exact mechanics:

```text
frontend/docs/architecture/realtime.md
frontend/docs/architecture/state-query-mutations.md
frontend/docs/architecture/api-and-contracts.md
```

---

# 100. Change classification

Changing any of these is potentially consequential:

```text
public event identity
message envelope
consumer identity
dedup scope
ordering stream
replay behavior
realtime logical contract
activity/audit mapping
```

Classify before implementation.

---

# 101. Integration/public event change

Review:

- producer;
- consumers;
- backlog;
- replay;
- Automation;
- Analytics;
- Integrations;
- compatibility/migration.

---

# 102. Envelope change

Review:

- every Platform consumer;
- versioning;
- idempotency;
- tenant scope;
- dead-letter/replay;
- observability.

Envelope version is not automatically event payload version.

---

# 103. Consumer identity change

Durable consumer identity changes can cause old messages to process again or ordering/dedup state to reset.

Treat as state migration where relevant.

---

# 104. Ordering change

Changing stream key/sequence can cause:

- false gaps;
- stale ordering;
- blocked delivery;
- replay differences.

Persisted sequence state requires migration analysis.

---

# 105. Realtime change

Review:

- web;
- mobile;
- mixed client versions;
- reconnect;
- cache reconciliation;
- scope/security.

---

# 106. Activity/audit mapping change

May affect:

- user-visible history;
- notification volume;
- security/compliance evidence;
- retention.

Not a cosmetic rename.

---

# 107. Event design checklist

```text
[ ] stable product fact
[ ] semantic owner
[ ] artifact class
[ ] logical identity
[ ] tenant/resource scope
[ ] payload minimized
[ ] sensitive data reviewed
[ ] version/order semantics
[ ] consumers known
[ ] compatibility/backlog reviewed
[ ] no hidden command
```

---

# 108. Delivery checklist

```text
[ ] outbox/durable enrollment need
[ ] commit ordering
[ ] message identity
[ ] producer identity
[ ] consumer identity
[ ] tenant context
[ ] idempotency
[ ] retry category
[ ] ordering scope
[ ] poison/dead-letter
[ ] replay
[ ] observability
[ ] external side-effect duplication safe
```

---

# 109. Realtime checklist

```text
[ ] stable logical identity
[ ] narrow subscription scope
[ ] authz
[ ] duplicate handling
[ ] out-of-order handling
[ ] version/sequence
[ ] gap recovery
[ ] reconnect
[ ] workspace transition
[ ] optimistic race
[ ] bounded dedup
[ ] mobile lifecycle
[ ] authoritative refetch
```

---

# 110. Activity checklist

```text
[ ] source fact committed
[ ] user-facing relevance
[ ] actor
[ ] target
[ ] delivery duplicates suppressed
[ ] retention/privacy
[ ] not confused with audit
```

---

# 111. Audit checklist

```text
[ ] governed action
[ ] actor
[ ] tenant/resource target
[ ] action/outcome
[ ] time/correlation
[ ] integrity/retention
[ ] sensitive data minimization
[ ] not reliant on activity completeness
```

---

# 112. Stop conditions

Stop rather than guess if:

- Domain event is exposed directly as public contract without compatibility review;
- event has no semantic owner;
- outbox publishes before commit;
- duplicate delivery can repeat external side effect;
- consumer identity is ambiguous;
- ordering is required but stream undefined;
- poison is scoped only by event name;
- realtime uses global workspace subscription;
- client has no gap recovery;
- activity is being used as audit;
- provider webhook schema leaks into product language;
- breaking event change ignores old backlog.

---

# 113. Related canonical owners

```text
docs/architecture/system-overview.md
docs/architecture/bounded-context-map.md
docs/architecture/contract-boundaries.md
docs/architecture/data-ownership-and-consistency.md
docs/architecture/capability-extraction-strategy.md

backend/docs/architecture/domain-modeling.md
backend/docs/architecture/application-model.md
backend/docs/architecture/platform-and-messaging.md
backend/docs/architecture/security-tenancy-authorization.md

frontend/docs/architecture/realtime.md
frontend/docs/architecture/state-query-mutations.md

docs/product/contexts/collaboration.md
docs/product/contexts/governance.md
docs/product/contexts/automation.md
docs/product/contexts/integrations.md
```

---

# 114. Final event boundary rule

For every “event-like” artifact, answer:

```text
What artifact class is it?
What fact does it represent?
Who owns the fact?
Is it internal/public?
Is it durable?
Can it duplicate/order/replay?
What scope does it carry?
Who consumes it?
How is compatibility handled?
How is recovery handled?
Is it activity, audit, notification, realtime, or transport state?
```

If the only answer is:

```text
"it is an event"
```

the architecture is underspecified.

The target is:

> **business facts remain semantically owned, delivery remains mechanically reliable, realtime remains recoverable, and user/audit histories remain intentionally distinct.**
