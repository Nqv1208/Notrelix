---
document_id: BE-PLATFORM-MESSAGING
document_type: architecture
status: active
owner: backend-architecture
applies_to:
  - backend/src/Notrelix.Platform
  - backend/tests/Notrelix.Platform.Tests
  - backend/tests/Notrelix.Integration.Tests
evidence:
  - backend/src/Notrelix.Platform/Notrelix.Platform.csproj
  - backend/src/Notrelix.Platform/Messaging/
  - backend/tests/Notrelix.Platform.Tests/
  - backend/tests/Notrelix.Integration.Tests/
  - backend/docs/architecture/application-model.md
  - backend/docs/architecture/infrastructure-and-data.md
review_on:
  - message-envelope-change
  - consumer-identity-change
  - idempotency-change
  - ordering-change
  - retry-poison-change
  - replay-change
  - transport-abstraction-change
  - post-commit-delivery-change
---

# Platform and Messaging

> **Platform owns reusable delivery/runtime mechanics. Product contexts own the facts and effects being delivered. Application owns the use-case and transaction intent. Infrastructure owns provider-specific persistence/broker implementations.**
>
> The central reliability contract is: **committed source state is enrolled durably, delivered with stable logical identity, processed idempotently under explicit tenant scope, and only acknowledged/advanced after the consumer's approved success condition.**

This document is the canonical backend owner for:

- messaging/runtime contracts;
- logical event/message identity;
- event envelope semantics;
- post-commit delivery;
- consumer registration/host mechanics;
- idempotency/deduplication;
- ordering and sequence progression;
- retry/backoff;
- poison/dead-letter semantics;
- replay/recovery mechanics;
- transport abstraction;
- messaging observability;
- tenant/RLS context for background delivery.

It does **not** own product event meaning, Domain event modeling, Application authorization, provider-specific RabbitMQ configuration, or public HTTP contracts.

---

# 1. Platform purpose

Platform exists because reliable asynchronous delivery has reusable correctness concerns that should not be reimplemented independently by every bounded context.

Those concerns include:

```text
identity
envelope
compatibility
serialization
delivery
consumer lifecycle
idempotency
ordering
retry
poison detection
dead-lettering
replay
observability
tenant execution context
```

The business meaning remains elsewhere.

---

# 2. Current project closure

Current `Notrelix.Platform.csproj` references:

```text
Notrelix.Domain
Notrelix.Application
```

and package abstractions only for:

```text
Microsoft.Extensions.DependencyInjection.Abstractions
Microsoft.Extensions.Logging.Abstractions
```

Current Platform also exposes selected internals to:

```text
Notrelix.Platform.Tests
```

for test seams.

Platform does not currently require direct EF Core, RabbitMQ, MassTransit, Redis, ASP.NET Core, or provider SDK package references.

---

# 3. BE-PLT-001 — Platform stays mechanism-oriented

Platform MAY know:

```text
Application contracts
Domain event metadata
logical messaging contracts
delivery state
consumer identity
```

Platform MUST NOT own:

```text
Board Item move rule
Billing entitlement rule
Workspace membership rule
Document archive semantics
Automation trigger business policy
```

because multiple contexts use Platform.

---

# 4. Current source organization

Current `Messaging/` contains:

```text
Consumers
Contracts
Diagnostic
Observability
Operations
Reliability
Runtime
Transport
```

Representative current files include:

```text
ConsumerHost
EventEnvelope
EnvelopeBuilder
MessagingRuntime
OrderingEnforcer
PoisonDetector
RetryPolicy
CircuitBreaker
ReplayEngine
RabbitMqTransportAdapter
InMemoryTransportAdapter
NullTransportAdapter
```

These names are current source evidence.

The durable architecture is the responsibility contract, not the exact folder/class inventory.

---

# 5. Domain event versus delivery contract

A Domain event represents an owned completed fact.

Platform may map/describe/serialize it for delivery.

Do not assume:

```text
Domain CLR record
=
public integration message
```

A public/replayed contract requires stable logical identity and compatibility semantics.

---

# 6. BE-PLT-002 — Public message identity is logical, not CLR-location identity

Refactoring:

```text
namespace
class name
assembly
folder
```

MUST NOT automatically change a stable public/replayed event identity.

Use explicit descriptor/name/version contracts.

---

# 7. Event descriptor

Current Platform `Contracts` includes:

```text
EventDescriptor
EventDescriptorProvider
SchemaDefinition
IEventSerializer
IEventDescriptorProvider
ITopicResolver
ICanonicalizer
```

This supports explicit contract description rather than transport-by-reflection alone.

---

# 8. BE-PLT-003 — Event descriptor describes compatibility-relevant facts

A descriptor SHOULD make explicit where applicable:

```text
logical event name
version
schema
scope
topic/routing category
serialization rules
```

It MUST NOT redefine the business meaning owned by the producer context.

---

# 9. Event envelope

The envelope transports delivery metadata around the semantic event payload.

Representative envelope concerns:

```text
message/event ID
logical event name/version
producer
occurred time
correlation/causation
Account/Workspace/resource scope
payload
schema/serialization metadata
```

Exact current fields remain source evidence.

---

# 10. BE-PLT-004 — Envelope metadata and payload semantics remain separate

Do not encode business fields into generic envelope merely because many messages happen to carry them.

Envelope owns delivery/routing/trace metadata.

Payload owns event-specific fact.

---

# 11. Stable message identity

Each durable delivery occurrence requires stable logical identity.

Identity supports:

```text
dedup
retry
poison
dead-letter
replay
observability
ordering
```

Do not regenerate identity on every retry.

---

# 12. BE-PLT-005 — Retry preserves logical message identity

A retry of the same committed event/message MUST retain the identity needed by dedup/poison/order logic.

Creating a new ID per attempt converts one retry into many distinct logical messages and defeats idempotency.

---

# 13. Producer identity

Delivery should identify the logical producer/capability where needed for:

```text
routing
diagnosis
contract ownership
replay
compatibility
```

Do not use process/container hostname as the only producer identity.

---

# 14. BE-PLT-006 — Runtime instance identity is not semantic producer identity

A horizontally scaled API/worker can produce the same logical event type.

Stable producer identity describes the capability/contract, not the transient host instance.

---

# 15. Consumer identity

Each durable consumer has a stable logical identity independent of:

```text
pod/container
process restart
thread
host name
```

Current consumer infrastructure includes registration/host abstractions.

---

# 16. BE-PLT-007 — Consumer identity is stable across deploy/restart

Dedup, poison and replay state MUST follow the logical consumer.

Renaming/moving a class MUST NOT silently create a fresh consumer identity if that would replay all old messages unintentionally.

---

# 17. Consumer identity change

A deliberate consumer identity change is a migration.

Review:

```text
old dedup state
old poison/dead-letter state
ordering cursor
replay/backlog
whether old and new coexist
```

Do not change identity to “clear” broken state.

---

# 18. Canonicalization

Current Platform includes JSON canonicalization contracts.

Canonicalization can support:

```text
stable payload fingerprint
schema comparison
idempotency/conflict checks
```

when required.

---

# 19. BE-PLT-008 — Canonicalization is deterministic and compatibility-aware

Do not depend on incidental JSON property order or serializer runtime behavior when a stable fingerprint is part of correctness.

Canonicalization MUST NOT erase semantically meaningful differences.

---

# 20. Serialization

Serialization is a delivery mechanism.

It should support:

```text
stable schema
versioning
backward compatibility
safe unknown-field handling where approved
```

Do not expose provider-specific broker framing as the business contract.

---

# 21. BE-PLT-009 — Serialization change inventories persisted/backlogged messages

If messages can remain in:

```text
outbox
broker queue
dead-letter store
replay storage
```

a serializer/schema change MUST consider old bytes, not only newly produced messages.

---

# 22. Topic/routing

Topic resolution is a transport-facing contract.

A topic MAY derive from logical event identity.

Do not derive routing solely from CLR namespace when the namespace is not a stable public contract.

---

# 23. BE-PLT-010 — Topic change is compatibility work when old messages/consumers exist

Inventory:

```text
producer
old/new consumer
broker binding
backlog
replay
deployment order
```

before changing routing.

---

# 24. Post-commit enrollment

Application owns which use case needs durable post-commit work.

The source transaction MUST enroll required delivery state atomically where loss would violate correctness.

Conceptual:

```text
source mutation
+
outbox/durable enrollment
        ↓
single local commit
```

---

# 25. BE-PLT-011 — Publish-before-commit is forbidden for committed business facts

Do not deliver:

```text
ItemChanged
SubscriptionActivated
MembershipRevoked
```

before the source transaction commits.

A rolled-back transaction MUST NOT leave an authoritative committed-fact message in the world.

---

# 26. Best-effort post-commit

Not every callback needs durable delivery.

Classify each effect:

```text
must survive process crash
or
best effort/diagnostic only
```

Do not silently use best-effort in-memory post-commit for an effect whose loss breaks product correctness.

---

# 27. BE-PLT-012 — Durability class is explicit

For each post-commit effect state:

```text
durable?
retryable?
deduplicated?
ordered?
tenant-scoped?
provider-visible?
```

The mechanism follows the class.

---

# 28. Delivery claim

Outbox/durable message delivery usually requires claiming work across multiple workers.

Claim semantics must prevent:

```text
permanent double ownership
lost eligible work
infinite lock
starvation
```

while tolerating crash/recovery.

Exact persistence/claim SQL belongs to Infrastructure.

---

# 29. BE-PLT-013 — Claiming is lease/recovery aware

If a worker crashes after claim:

```text
work becomes eligible again
or
another recovery mechanism owns it
```

Do not leave durable work permanently invisible.

---

# 30. At-least-once posture

Messaging delivery should assume duplicates can happen.

Network/broker/process failures make exactly-once delivery claims unsafe unless backed by a very specific transactional system.

Default design:

```text
at-least-once delivery
+
idempotent consumer effect
```

---

# 31. BE-PLT-014 — Duplicate delivery is expected behavior, not exceptional corruption

Consumers MUST be safe against the duplicate conditions their contract allows.

Do not “fix” duplicates only by hoping the broker never redelivers.

---

# 32. Idempotency

Consumer idempotency state is keyed by at least:

```text
logical message identity
+
logical consumer identity
```

and, where needed, scope/version/fingerprint.

---

# 33. BE-PLT-015 — Idempotency is per consumer effect

One consumer succeeding does not imply every other consumer succeeded.

Do not use a global message-only “processed” flag that prevents another independent consumer from processing the same event.

---

# 34. Same identity / conflicting payload

If one logical identity arrives with semantically different payload:

```text
do not silently accept as duplicate
```

This indicates:

```text
identity collision
producer bug
corrupt replay
contract misuse
```

and should be diagnosable/poisoned according to policy.

---

# 35. BE-PLT-016 — Duplicate and identity conflict are distinct

Same ID + same canonical request/event may be duplicate.

Same ID + different semantic payload is a conflict.

Do not normalize both to success.

---

# 36. Consumer success condition

A consumer defines when its business effect is complete.

Success may mean:

```text
Application command committed
projection updated
provider outcome durably recorded
```

depending on consumer contract.

---

# 37. BE-PLT-017 — Acknowledge/advance only after approved consumer success

Do not advance:

```text
broker ack
ordering cursor
consumer completion
```

before the handler/effect satisfies its success contract.

This prevents lost work hidden behind an advanced cursor.

---

# 38. Consumer failure

Failure classification should distinguish at minimum:

```text
transient
deterministic invalid/contract
authorization/tenant failure
dependency unavailable
provider unknown outcome
programming defect
```

Not every failure should retry.

---

# 39. BE-PLT-018 — Retry policy is failure-class aware

Retry transient failures.

Do not hot-retry deterministic:

```text
schema invalid
malformed contract
revoked access
missing required semantic field
```

until the retry budget is exhausted pointlessly.

---

# 40. Retry budget

Retry must be bounded.

Parameters may include:

```text
attempt count
backoff
jitter
max delay
time horizon
```

Exact numbers are runtime/configuration decisions, not universal architecture constants.

---

# 41. BE-PLT-019 — Retry cannot create an amplification storm

During provider/DB/broker degradation:

```text
backoff
jitter
concurrency limits
circuit breaker
```

should prevent retries from overwhelming the failing dependency.

---

# 42. Circuit breaker

Current Platform Reliability contains `CircuitBreaker`.

Circuit breaking is a dependency-protection mechanism.

It does not declare a business operation successful.

---

# 43. BE-PLT-020 — Open circuit is a degraded failure mode, not fake success

The consumer/use case still needs a semantic state:

```text
pending
retry scheduled
failed
unknown
```

as owned by Application/Product.

Platform only controls execution pressure.

---

# 44. Poison detection

Poison means a logical message/consumer path is repeatedly/deterministically unable to succeed under current conditions.

Current Platform has `PoisonDetector`.

Poison detection should support diagnosis and bounded containment.

---

# 45. BE-PLT-021 — Poison identity is scoped to message + consumer

A message can be poison for one consumer and valid for another.

Do not poison all consumers globally because one consumer fails.

A broader scope requires an explicit ordering/contract invariant.

---

# 46. Poison versus transient

Examples:

```text
invalid schema
→ likely deterministic poison

database unavailable
→ transient

invalid tenant scope encoded in message
→ deterministic/security issue

provider 429
→ transient/rate limited
```

Classify deliberately.

---

# 47. Dead-letter

Dead-letter is durable failure evidence/recovery input.

It should preserve enough information to identify:

```text
message
consumer
event contract
failure class
attempt history
tenant/resource scope where safe
correlation
```

Do not store secrets/private raw payload unnecessarily.

## Outbox terminal failure state

For the durable outbox, the retry-exhausted `Failed` row **is** the
dead-letter terminal state:

```text
status = 'Failed' with retry_count >= max_retries
+ last_error_code / error_message diagnostics
+ bounded exponential backoff history
```

No separate dead-letter queue/table infrastructure exists or is required.
The outbox row itself preserves message identity, attempt history, and
diagnostic metadata in place, which satisfies the recovery-input contract
above.

Invariants that remain binding:

```text
bounded retry (max_retries), no infinite hot retry
diagnostic metadata preserved on the row
failed event blocks later versions of the same ordered stream
unrelated streams are never blocked
operational recovery/re-drive path operates on these rows directly
```

---

# 48. BE-PLT-022 — Dead-letter is not deletion

Moving work to dead-letter does not mean the business effect is resolved.

Recovery/replay/repair must decide what to do next.

Do not purge dead-letter solely to make backlog dashboards green.

---

# 49. Ordering

Ordering is required only when a business/mechanism invariant needs it.

Possible key scopes:

```text
aggregate/resource
Workspace
provider connection
workflow execution
```

Avoid global ordering.

---

# 50. BE-PLT-023 — Ordering scope is the smallest scope that preserves the invariant

Broader ordering reduces throughput and failure isolation.

Do not serialize unrelated tenants/resources for convenience.

---

# 51. Sequence validation

Current Reliability contains `OrderingEnforcer`.

Sequence state/cursor must identify:

```text
ordering key
expected/current sequence
message identity
consumer
```

as required.

---

# 52. BE-PLT-024 — Sequence advancement occurs after handler success

Do not update cursor/sequence before business processing completes.

If processing fails:

```text
cursor remains at last completed sequence
```

unless an explicit skip/quarantine procedure is approved.

---

# 53. Ordering gap

When sequence gap occurs:

```text
wait/retry
reconcile
replay missing item
quarantine by approved policy
```

Do not blindly skip the gap if later messages depend on it.

---

# 54. BE-PLT-025 — Ordering gap is observable

Operators need:

```text
ordering key
expected sequence
received sequence
oldest blocked age
blocked consumer
```

without dumping sensitive payload.

---

# 55. Independent ordering keys

A poison/gap in key A should not block key B unless a broader invariant requires it.

Test this behavior.

---

# 56. Consumer host

Current Platform includes `ConsumerHost`, registrations and options.

Host owns generic lifecycle:

```text
receive
establish execution context
validate contract
apply reliability policy
invoke handler
ack/fail
observe
```

Business decision remains in handler/Application.

---

# 57. BE-PLT-026 — Consumer host is not a business service layer

Do not add:

```text
if event is Billing then ...
if role is Admin then ...
```

policy to generic host.

Use registered handlers/contracts.

---

# 58. Tenant context in consumer

Tenant-scoped messages must provide enough trusted scope to reconstruct server-side execution context.

Do not rely on ambient HTTP context.

---

# 59. BE-PLT-027 — Background work is explicitly tenant-scoped

Before tenant data read/write:

```text
identify Account/Workspace/resource
establish approved DB/RLS scope
authorize/system-operation semantics where required
```

Do not run ordinary tenant work under unrestricted system context.

---

# 60. Tenant scope trust

Scope in message is a committed producer fact/contract input.

Consumer should validate consistency when it loads target data.

Do not treat arbitrary externally supplied queue payload as trusted internal scope unless authenticity/trust boundary is established.

---

# 61. BE-PLT-028 — External inbound messages cross a trust boundary

Webhook/external broker input requires:

```text
authentication/signature
schema validation
replay protection
provider identity
```

before mapping into trusted Platform/Application delivery.

Infrastructure/API integration adapters own those verification mechanisms.

---

# 62. Correlation and causation

Message metadata should preserve:

```text
correlation
causation
source operation
```

where useful.

Do not use correlation ID as idempotency key automatically; they have different semantics.

---

# 63. BE-PLT-029 — Correlation, causation, message ID, idempotency ID are distinct

They may be related but answer different questions:

```text
message ID
→ this logical message

causation
→ which prior operation/message caused it

correlation
→ which broader flow

idempotency
→ which logical effect must not duplicate
```

Do not collapse them without an explicit contract.

---

# 64. Compatibility evaluator

Current Runtime includes:

```text
BackwardCompatibilityEvaluator
FullCompatibilityEvaluator
SchemaValidationRule
```

This supports explicit contract compatibility checks.

---

# 65. BE-PLT-030 — Compatibility is evaluated against supported consumers/backlog, not only newest producer

A schema can compile in new code while breaking:

```text
old worker
old queued message
replay archive
external consumer
```

Contract change inventory is required.

---

# 66. Additive contract change

An additive field can be backward compatible if:

```text
old consumers ignore unknown field
new consumers tolerate absence/default appropriately
```

Do not label “additive” automatically safe without serializer/consumer evidence.

---

# 67. Breaking contract change

Examples:

```text
remove/rename required field
change semantic meaning
change event identity
change routing/topic
change ordering key
change tenant scope
```

require C3-style compatibility/migration analysis.

---

# 68. BE-PLT-031 — Event version increases when incompatible semantic contract changes require it

Do not increment version for every code refactor.

Do not avoid version bump when old/new consumers cannot safely coexist.

---

# 69. Replay

Current Platform Operations includes:

```text
ReplayEngine
ReplayRequest
ReplayResult
CheckpointReplayStrategy
LatestReplayStrategy
SnapshotReplayStrategy
TimeWindowReplayStrategy
ReplayThrottle
IReplayCheckpointStore
IReplayAuditLog
```

Replay is a controlled recovery/reprocessing operation.

---

# 70. BE-PLT-032 — Replay is not “republish everything”

A replay request MUST define bounded identity/range/strategy and understand:

```text
consumer
dedup
ordering
provider side effects
tenant scope
rate/capacity
```

Do not run unbounded replay to repair one consumer.

---

# 71. Replay checkpoint

Long replay should be resumable.

Checkpoint tracks progress without becoming semantic product state.

---

# 72. BE-PLT-033 — Replay checkpoint advances after replayed unit success

Do not record a unit complete before the target consumer/effect reaches approved success.

Crash/restart should not skip unprocessed work.

---

# 73. Replay idempotency

Replay may intentionally re-deliver the same message.

Consumer idempotency/replay policy determines whether effect is:

```text
skipped as already processed
recomputed safely
explicitly forced under controlled semantics
```

Do not disable dedup globally to “make replay work”.

---

# 74. BE-PLT-034 — Forced replay is a governed operation

If an operator must bypass normal dedup:

```text
scope
reason
consumer
message range
external-effect safety
audit
```

must be explicit.

No generic “clear all processed keys”.

---

# 75. Replay throttle

Replay competes with live traffic.

Throttle/fairness prevents recovery work from becoming another outage.

Exact rate is runtime/operations-owned.

---

# 76. BE-PLT-035 — Recovery traffic preserves tenant and live-work fairness

A single large tenant/replay MUST NOT starve normal delivery if the mechanism can enforce fair bounds.

---

# 77. Transport abstraction

Current Transport includes:

```text
ITransportAdapter
IConnectionManager
ITransportPolicy
DefaultTransportPolicy
RabbitMqTransportAdapter
InMemoryTransportAdapter
NullTransportAdapter
```

Platform contract should not require RabbitMQ semantics where generic transport semantics suffice.

---

# 78. BE-PLT-036 — Transport adapter is replaceable behind Platform contract

RabbitMQ/InMemory/Null are mechanisms.

Do not encode product behavior based on:

```text
exchange name
queue implementation
broker SDK type
```

outside transport-specific configuration/adapters.

---

# 79. InMemory transport

InMemory transport can support:

```text
tests
local development
mechanism simulation
```

It cannot prove real broker semantics such as:

```text
network failure
broker ack/redelivery
durability
RabbitMQ routing
connection recovery
```

---

# 80. BE-PLT-037 — InMemory success is not broker reliability proof

Use production-graph integration/real transport evidence when the protected property depends on broker behavior.

---

# 81. Null transport

A null transport may be valid for explicitly disabled/no-op runtime configurations.

Do not allow it silently in an environment where required durable delivery is part of correctness.

---

# 82. BE-PLT-038 — Required delivery fails configuration/readiness if transport is unavailable by policy

Do not silently discard required messages because a transport adapter was misconfigured to null.

Safe degradation depends on the declared durability contract.

---

# 83. Infrastructure broker adapter boundary

Infrastructure currently contains provider integration packages such as MassTransit/RabbitMQ.

The durable ownership split is:

```text
Platform
→ logical delivery semantics

Infrastructure
→ provider persistence/broker wiring where current architecture places it
```

Do not duplicate retry/order/poison policy separately in broker middleware and Platform without one owner.

---

# 84. BE-PLT-039 — Provider broker retry cannot violate Platform retry/idempotency semantics

If broker-level retries exist, account for them in:

```text
attempt budget
dedup
poison
observability
ordering
```

Avoid stacked retry layers multiplying attempts unexpectedly.

---

# 85. Scheduler/background jobs

Platform mechanisms for scheduled/background execution should create stable logical occurrence identity.

Scaling workers/schedulers must not duplicate one logical occurrence.

---

# 86. BE-PLT-040 — Schedule occurrence identity survives scale-out

For one scheduled occurrence:

```text
1 scheduler → N scheduler instances
```

MUST NOT mean:

```text
one logical occurrence → N business executions
```

Use claims/idempotency/occurrence identity.

---

# 87. Delayed work

If delayed/retry scheduling exists, persist enough identity/scope to survive process restart.

Do not use only in-memory timer for required long-lived work.

---

# 88. BE-PLT-041 — Durable delay uses durable state

In-memory delay is acceptable only for non-durable best-effort work whose loss is explicitly acceptable.

---

# 89. Message lifecycle observability

Operators should be able to answer:

```text
Was source committed?
Was message enrolled?
Was it published?
Which consumer received it?
How many attempts?
Is it blocked by order?
Is it poison/dead-letter?
Was it replayed?
```

without reconstructing from arbitrary logs alone.

---

# 90. BE-PLT-042 — Observability follows semantic identities

Record safely:

```text
message/event identity
consumer
event name/version
tenant/resource scope
attempt
ordering key
correlation/causation
outcome
```

Avoid raw payload dumps.

---

# 91. Backlog metrics

Raw queue count alone is weak.

Prefer also:

```text
oldest backlog age
throughput
retry rate
poison/dead-letter count
ordering blocked age
```

because age/freshness maps more directly to user impact.

---

# 92. BE-PLT-043 — Oldest backlog age is first-class delivery signal

A stable queue count with growing oldest age can indicate a stuck partition/consumer.

Do not rely on average throughput only.

---

# 93. Retry observability

Retry metrics should classify:

```text
transient dependency
contract/poison
rate limit
unknown provider outcome
```

when possible.

Do not aggregate all failures into one “retry count” if actionability differs.

---

# 94. Dead-letter observability

Dead-letter alerts/runbooks must route to the logical owner:

```text
event producer
consumer
contract
tenant scope
```

Platform provides identity.

Product/application owner investigates semantic invalidity where needed.

---

# 95. BE-PLT-044 — Platform diagnostic tooling cannot mutate product state as a shortcut

Diagnostics can inspect/trace/replay through controlled mechanisms.

Do not add generic “fix payload/skip invariant” operations to Platform tooling.

---

# 96. Data persistence for Platform state

Infrastructure may persist:

```text
outbox
consumer idempotency
ordering cursor
poison/dead-letter
replay checkpoint
```

Platform defines the logical state machine/invariants.

Infrastructure maps it to PostgreSQL/broker.

---

# 97. BE-PLT-045 — Delivery state schema follows logical identity

A schema optimization MUST preserve:

```text
message identity
consumer identity
ordering scope
retry/poison state
```

Do not collapse keys because current deployment has one consumer only.

---

# 98. Retention

Delivery metadata may have retention.

Retention policy must account for:

```text
duplicate/retry horizon
replay
incident diagnosis
contract support
audit needs
```

Exact duration is an approved operations/data policy, not invented here.

---

# 99. BE-PLT-046 — Retention cannot delete dedup/replay evidence before supported horizon

Premature deletion can cause:

```text
old duplicate re-execution
unrecoverable replay
lost incident evidence
```

Coordinate with Operations/Delivery.

---

# 100. Message payload privacy

Message payload may contain tenant/private business data.

Transport/log/dead-letter storage must minimize exposure.

Do not include secrets.

---

# 101. BE-PLT-047 — Delivery diagnostics use metadata before payload

For ordinary diagnosis prefer:

```text
IDs
event name/version
tenant/resource
failure code
```

rather than full serialized business content.

Restricted payload inspection requires explicit operational/security need.

---

# 102. Security-sensitive events

Events representing permission/member/share changes require careful:

```text
scope
ordering
realtime invalidation
consumer authorization context
```

because stale propagation can create access windows.

Platform delivers; security semantics remain Application/Governance-owned.

---

# 103. BE-PLT-048 — Delivery retry never converts revoked authority into permission

A delayed message must not execute an operation under authority that is no longer valid when the product contract requires current authorization.

The consumer/Application decides whether authority is:

```text
captured-at-commit fact
or
must be re-evaluated at execution
```

Platform does not guess.

---

# 104. Eventual consistency

Asynchronous consumers create eventual convergence.

Define:

```text
source authority
expected lag
reconciliation
user-visible stale/pending state
```

outside Platform as product/application contract.

Platform exposes delivery status/freshness signals.

---

# 105. BE-PLT-049 — Platform does not hide eventual consistency

Do not report delivery success merely because message entered queue.

Consumer completion/freshness is a separate state.

---

# 106. Message evolution

When producer evolves:

```text
contract-first
→ compatibility matrix
→ producer/consumer rollout
→ backlog/replay compatibility
→ remove old version
```

Do not rely on simultaneous deployment.

Public integration-event versioning contract (accepted via the Identity &
Accounts closure, ADR-governed where noted):

```text
contract key:
EventContractKey = (Name, Version) — the runtime resolution identity;
  same Name with different Versions coexist in registries/catalogs;
  duplicate (Name, Version) registration fails deterministically;
  unknown/unregistered Version lookup fails deterministically with no
  implicit latest/default fallback.

coexistence:
v1 and v2 of one logical event may be produced and consumed concurrently
during migration; producers/consumers declare explicit versions; envelope
metadata carries the version end-to-end so callers never guess.

schema baseline:
each Version pins its payload schema; changing a payload shape under the
SAME Version is a compatibility violation rejected by gates
(CanonicalManifest_MatchesGeneratedSourceShape /
ManifestComparator_RejectsSameVersionSchemaChange).

consumer maturity:
every registered consumer entry declares explicit maturity
(Implemented / Stub / None) alongside its consumed (Name, Version);
stub consumers are recorded AS STUB until their owning context implements
them — maturity is registry metadata, not an implicit assumption.

producer/consumer rollout:
add new Version → deploy consumers able to read it → migrate producers →
drain old versions before retirement (below).

retirement:
an old Version may be removed only after outbox and DLQ/backlog drain is
verified per the owning context's migration plan; queued messages are
deployed consumers (BE-PLT-050).
```

The canonical event manifest (`backend/contracts/events/notrelix.events.json`)
is generated from the production public registry through repository tooling
and drift-checked by executable gates; unrelated bounded contexts' semantic
contract data must not change silently when shared generator mechanics evolve.

---

# 107. BE-PLT-050 — Old queued messages are deployed consumers

Treat a queue/replay archive as an independent compatibility surface.

A new worker must either read old messages or wait until old backlog is drained/migrated according to plan.

---

# 108. Consumer evolution

New consumer logic may change:

```text
dedup key
ordering key
success condition
side effect
```

These are architecture/reliability changes even if event schema is unchanged.

---

# 109. BE-PLT-051 — Consumer success-condition change reviews replay/idempotency state

If old “processed” state no longer means new success condition, migration/reconciliation is required.

Do not reuse incompatible completion records blindly.

---

# 110. Delivery no-op

A duplicate can be a delivery no-op when already processed.

A product event itself is not a no-op simply because payload resembles prior event.

Keep delivery idempotency separate from Domain semantic no-op.

---

# 111. BE-PLT-052 — Delivery dedup does not erase legitimate repeated business occurrences

Two separate business events may carry identical payload but different EventIds.

Do not dedup solely by payload equality unless the product operation identity explicitly requires it.

---

# 112. Poison recovery

After code/contract fix, poison item can be retried/replayed deliberately.

Recovery should preserve:

```text
same logical message
same consumer identity
ordering relationship
audit
```

unless an approved migration transforms the contract.

---

# 113. BE-PLT-053 — Poison recovery does not create a new fake event to bypass poison state

If transformation is necessary, record the migration/replay lineage.

Do not hide the original failure.

---

# 114. Skip/quarantine

Skipping an ordered message is a semantic decision when later work depends on it.

Platform may provide mechanism.

The owning product/consumer contract authorizes whether skip is safe.

---

# 115. BE-PLT-054 — Platform cannot authorize semantic skip on its own

No generic operator button:

```text
skip and continue
```

for all ordered workflows.

Require explicit runbook/owner criteria.

---

# 116. Broker outage

If source transaction + outbox commit succeeds while broker is down:

```text
source state remains committed
delivery becomes delayed
outbox/backlog remains recoverable
```

Do not roll back already-committed source merely because broker is unavailable later.

---

# 117. BE-PLT-055 — Broker outage does not erase committed outbox intent

The dispatcher retries/degrades.

User-facing state may need pending/freshness semantics by feature.

---

# 118. Database outage

If outbox/idempotency/order state shares PostgreSQL, DB outage may block delivery coordination.

Do not switch to an uncoordinated in-memory path that loses durable identity just to keep consumers “running”.

---

# 119. BE-PLT-056 — Reliability state failure fails safely

When durable coordination state is unavailable:

```text
pause/retry/degrade
```

rather than execute side effects without idempotency/order protection.

---

# 120. Provider outage from consumer

A consumer triggering provider action must combine:

```text
message idempotency
provider operation identity
retry/backoff
unknown outcome reconciliation
```

Platform handles delivery mechanics; Application/Infrastructure handle provider semantics.

---

# 121. BE-PLT-057 — Consumer retry and provider retry are one end-to-end attempt model

Avoid:

```text
Platform retries 10x
× provider adapter retries 10x
= 100 uncontrolled attempts
```

Design total retry amplification intentionally.

---

# 122. Backpressure

Consumer concurrency and claim size must respect:

```text
DB capacity
provider limits
tenant fairness
ordering scope
```

Exact values are runtime configuration.

---

# 123. BE-PLT-058 — More workers does not automatically increase safe throughput

Scale only while downstream capacity/idempotency/order remain safe.

Observe saturation before increasing concurrency.

---

# 124. Realtime relationship

Realtime may be one post-commit delivery path.

It is not the same contract as durable integration messaging necessarily.

Both should describe the same committed business fact where they represent the same event.

---

# 125. BE-PLT-059 — Realtime delivery failure does not change source truth

Clients reconcile from authoritative API/state.

Do not use websocket delivery ack as source transaction commit.

---

# 126. Notifications relationship

Notification generation/delivery may consume committed facts.

Platform transports reliably where required.

Notification product context owns recipient/lifecycle/preferences.

---

# 127. Automation relationship

Automation can consume events and execute actions.

Platform guarantees delivery mechanics.

Automation owns rule/execution semantics.

Target contexts own the mutations Automation requests.

---

# 128. BE-PLT-060 — Automation event consumption preserves source event identity and target authorization semantics

Do not let an Automation consumer mutate target tables directly because it received a trusted event.

It invokes the target Application use case.

---

# 129. Billing relationship

Billing/usage events may have financial consequences.

Require strong:

```text
idempotency
stable operation identity
ordering where necessary
reconciliation
```

Do not infer billable usage from duplicate message delivery.

---

# 130. BE-PLT-061 — Financial effect is deduplicated by business operation, not transport attempt

A broker redelivery must not create duplicate usage/charge.

---

# 131. Testing

Primary:

```text
backend/tests/Notrelix.Platform.Tests
```

Use for:

```text
identity
envelope
compatibility
consumer host
idempotency
ordering
retry
poison
replay
transport policy
```

Use Integration for commit + real persistence/transport graph as required.

---

# 132. BE-PLT-062 — Platform change requires production-graph proof when source transaction matters

For changes to:

```text
outbox enrollment
claiming
consumer DB scope
idempotency persistence
ordering persistence
```

unit tests alone are insufficient.

Include at least one realistic cross-layer integration proof.

---

# 133. Ordering test matrix

Applicable tests include:

```text
same key in order
same key out of order
gap
handler failure
retry
poison
different key progress
cursor after success only
```

---

# 134. Idempotency test matrix

Applicable:

```text
first process
duplicate same payload
same ID conflicting payload
concurrent duplicate
retry after transient
consumer A versus consumer B
retention/replay edge
```

---

# 135. Poison test matrix

Applicable:

```text
deterministic invalid
transient recovery
retry exhaustion
consumer-scoped poison
dead-letter identity
recovery/replay
```

---

# 136. Replay test matrix

Applicable:

```text
bounded range
checkpoint resume
already-processed item
forced replay
ordering
throttle
provider side-effect safety
tenant scope
audit
```

---

# 137. BE-PLT-063 — Test a deliberate failure path, not only happy delivery

Reliability architecture is defined by failure behavior.

At least the changed failure mode must be reproduced.

---

# 138. Architecture tests

Useful gates can enforce:

```text
Platform project dependency closure
business-context dependency restrictions
stable consumer registration contracts
critical classes included in expected test/gate
```

Do not encode broker-specific implementation detail as a universal Platform rule.

---

# 139. Observability tests

Where critical, verify emitted diagnostic dimensions exist and are bounded/safe.

Do not snapshot entire log text as a durable contract unless necessary.

---

# 140. Change classification

Examples:

```text
new consumer following existing semantics
→ C1/C2

message schema/event identity
→ C3

delivery state schema/migration
→ C4

Platform abstraction/ordering model
→ C5

tenant execution/security
→ C6

transport/runtime dependency
→ C7

financial/destructive replay
→ C8
```

Modifiers:

```text
ASYNC_BACKLOG
PROVIDER_EXTERNAL
CROSS_TENANT
ROLLBACK_UNSAFE
```

often apply.

---

# 141. ADR trigger

ADR may be required for:

```text
new delivery model
consumer identity foundation
ordering architecture
retry/poison architecture
replay model
transport abstraction
outbox/transaction architecture
```

Routine consumer registration does not need an ADR.

---

# 142. Platform review checklist

```text
[ ] source semantic owner
[ ] logical event/message identity
[ ] producer identity
[ ] consumer identity
[ ] tenant/resource scope
[ ] schema/version compatibility
[ ] outbox/post-commit
[ ] idempotency
[ ] ordering
[ ] retry budget
[ ] poison/dead-letter
[ ] replay/backlog
[ ] provider external effects
[ ] observability
[ ] production-graph proof
```

---

# 143. Consumer review checklist

```text
[ ] trusted contract
[ ] tenant context
[ ] handler/Application boundary
[ ] success condition
[ ] idempotency key
[ ] ordering scope
[ ] retryable failures
[ ] poison failures
[ ] ack after success
[ ] provider reconciliation
[ ] duplicate side-effect protection
```

---

# 144. Event-contract review checklist

```text
[ ] logical name
[ ] version
[ ] owner
[ ] scope
[ ] immutable payload
[ ] no secret
[ ] old consumer
[ ] old backlog
[ ] replay
[ ] routing/topic
[ ] generated/external consumers
```

---

# 145. Replay review checklist

```text
[ ] exact consumer
[ ] exact message/range
[ ] tenant scope
[ ] checkpoint
[ ] idempotency
[ ] ordering
[ ] external effect
[ ] throttle
[ ] audit
[ ] completion verification
```

---

# 146. Stop conditions

Stop Platform implementation if:

- product meaning is being moved into generic delivery code;
- logical message identity is unresolved;
- retry generates a new message identity;
- consumer identity changes only to clear state;
- idempotency is global message-only despite independent consumers;
- cursor/ack advances before handler success;
- poison scope is broader than justified;
- ordered skip has no semantic owner approval;
- replay requires clearing dedup globally;
- background work has no explicit tenant/RLS context;
- provider retries can multiply with consumer retries without a total budget;
- old backlog/replay compatibility is unknown;
- required delivery is silently discarded by null/best-effort path.

---

# 147. Executable evidence

Current source:

```text
backend/src/Notrelix.Platform/Messaging
```

Current tests:

```text
backend/tests/Notrelix.Platform.Tests
backend/tests/Notrelix.Integration.Tests
```

Focused:

```bash
cd backend
dotnet test tests/Notrelix.Platform.Tests/Notrelix.Platform.Tests.csproj
```

Material Platform reliability changes also require the classified integration/architecture gates.

---

# 148. Related canonical owners

Backend:

```text
application-model.md
infrastructure-and-data.md
security-tenancy-authorization.md
testing-and-quality-gates.md
```

Repository:

```text
../../../docs/architecture/events-realtime-and-delivery-boundary.md
../../../docs/delivery/contract-first-delivery.md
../../../docs/delivery/migration-policy.md
../../../docs/operations/observability.md
../../../docs/operations/recovery-and-data-safety.md
../../../docs/operations/service-degradation.md
```

---

# 149. Non-responsibilities

Platform does not own:

```text
Domain business invariant
resource authorization policy
HTTP endpoint shape
PostgreSQL RLS SQL
RabbitMQ provider credentials
frontend cache/query state
Billing price/entitlement semantics
Automation rule meaning
```

It owns the reusable reliability mechanism those owners use.

---

# 150. Final Platform rule

A healthy delivery flow can be stated as:

```text
committed source fact
        ↓
stable logical event/message identity
        ↓
durable enrollment
        ↓
transport
        ↓
stable logical consumer identity
        ↓
explicit tenant execution scope
        ↓
idempotent handler
        ↓
ordered success where required
        ↓
ack/cursor after success
```

with:

```text
bounded retry
consumer-scoped poison
diagnosable dead-letter
controlled replay
backlog freshness visibility
transport replaceability
```

and without:

```text
business policy in Platform
publish-before-commit
new ID per retry
global processed flag
cursor-before-handler
blind ordered skip
tenantless worker
unbounded replay
broker-specific business contract
```
