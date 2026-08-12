---
document_id: OPS-SERVICE-DEGRADATION
document_type: operations-standard
status: active
owner: operations
applies_to:
  - runtime
  - api
  - database
  - cache
  - messaging
  - realtime
  - object-storage
  - integrations
  - frontend
  - mobile
evidence:
  - docs/operations/observability.md
  - docs/operations/incident-readiness.md
  - docs/operations/recovery-and-data-safety.md
  - docs/delivery/release-and-rollout.md
  - docs/quality/security-quality-standard.md
  - docs/quality/performance-and-scalability.md
  - docs/architecture/data-ownership-and-consistency.md
  - docs/architecture/events-realtime-and-delivery-boundary.md
  - backend/docs/architecture/infrastructure-and-data.md
  - backend/docs/architecture/platform-and-messaging.md
review_on:
  - dependency-change
  - degradation-policy-change
  - retry-policy-change
  - cache-policy-change
  - messaging-runtime-change
  - realtime-runtime-change
  - provider-integration-change
  - object-storage-change
  - incident-learnings-change
---

# Service Degradation

> **A degraded mode is an explicitly safer reduced capability, not a shortcut that bypasses authorization, tenant isolation, validation, durability, or product ownership.**
>
> When a dependency fails, Notrelix either continues through an authoritative safe path, reduces optional functionality, rejects work clearly, or pauses work for later recovery. It never invents business truth.

This document is the canonical repository-level owner for dependency-failure and degraded-service behavior.

It defines decision rules and restoration criteria, not vendor-specific commands or numerical thresholds.

---

# 1. Degradation model

For each dependency/capability determine:

```text
is it authoritative?
is it optional acceleration?
is it durable delivery?
is it external side effect?
can work be queued?
can reads remain safe?
can writes remain safe?
what user sees?
what restores normal mode?
```

---

# 2. OPS-DEG-001 — Correctness is preserved before throughput

During dependency failure, do not restore traffic by:

- bypassing authorization;
- disabling RLS;
- trusting stale permission cache;
- skipping provider validation;
- dropping idempotency;
- accepting writes that cannot be persisted durably.

---

# 3. Failure classes

Canonical classes:

```text
database
cache/Redis
message broker/background runtime
object storage
realtime
provider/integration
frontend/CDN/assets
authentication/identity dependency
search/analytics projection
capacity/saturation
```

Each has different degradation semantics.

---

# 4. OPS-DEG-002 — Dependency role determines degraded mode

Do not apply one generic:

```text
catch → retry forever
```

or:

```text
dependency unavailable → 500
```

policy across all dependencies.

---

# 5. Optional acceleration

Examples can include:

```text
cache
realtime freshness
search projection
some analytics
```

when authoritative source remains available.

---

# 6. OPS-DEG-003 — Optional acceleration may be bypassed only through authoritative safe path

Example:

```text
cache unavailable
→ direct tenant-authorized DB read
```

only if DB capacity and security remain safe.

---

# 7. Authoritative dependency

A database that owns durable state is not replaceable with guessed local memory.

---

# 8. OPS-DEG-004 — Authoritative-store failure cannot degrade to invented writable truth

If durable writes cannot be committed safely:

```text
reject
pause
read-only
queue only if durability/semantics are explicitly designed
```

Do not return false success.

---

# 9. User-visible degraded state

The product should distinguish:

```text
temporarily unavailable
read-only
pending
stale with timestamp
retrying
provider unavailable
sync delayed
```

as appropriate.

---

# 10. OPS-DEG-005 — Degraded UX states the real limitation

Do not show:

```text
Saved
Synced
Paid
Sent
```

when the operation is only queued/pending/unknown.

---

# 11. Database degradation

Signals can include:

```text
connection exhaustion
high query latency
lock/deadlock
write errors
storage pressure
migration lock
```

---

# 12. OPS-DEG-006 — Database degradation prioritizes correctness-critical work

Possible containment:

```text
shed optional analytics/reporting
pause noncritical backfills
reduce background concurrency
throttle expensive exports
pause optional sync
```

before compromising core authoritative operations.

---

# 13. DB retry

Database transient retry can be appropriate for known transient operations.

---

# 14. OPS-DEG-007 — DB retry is bounded and transaction-aware

Do not retry blindly if:

- transaction outcome is unknown;
- operation is not idempotent;
- retry amplifies lock/saturation.

---

# 15. DB read-only mode

Read-only can be a safe degradation only if:

- read data is valid;
- tenant/RLS works;
- stale projections are handled;
- UI clearly prevents/communicates writes.

---

# 16. OPS-DEG-008 — Read-only mode is explicit across API and clients

Do not allow frontend optimistic writes that appear successful while backend discards them.

---

# 17. Database unavailable

If authoritative reads are impossible, cached data may be shown only if the product has an explicit safe stale-read contract.

---

# 18. OPS-DEG-009 — Stale cache is not an emergency source of truth by default

Permission/resource changes can make stale cached data unsafe.

Prefer unavailable over cross-tenant/unauthorized exposure.

---

# 19. Redis/cache degradation

Cache failure can increase origin load.

---

# 20. OPS-DEG-010 — Cache bypass is capacity-aware

Before bypassing cache broadly consider:

```text
DB capacity
request rate
expensive query fan-in
stampede protection
tenant fairness
```

---

# 21. Cache writes

Failure to populate optional cache should not fail the authoritative source operation unless the cache is explicitly required by architecture for correctness.

---

# 22. OPS-DEG-011 — Cache population is not transaction success authority

Source commit success remains determined by the authoritative persistence contract.

---

# 23. Permission cache

Permission-sensitive cache is special.

---

# 24. OPS-DEG-012 — Permission cache failure fails safe

If current authorization cannot be determined from authoritative policy/facts, do not reuse uncertain allow indefinitely.

---

# 25. Cache recovery

After cache restoration:

```text
invalidate stale generation
warm gradually if needed
monitor origin load
```

Do not assume existing cache entries are safe after policy/data incident.

---

# 26. Messaging/broker degradation

If broker is unavailable but source DB/outbox is available:

```text
source transaction may commit with durable outbox
downstream delivery becomes delayed
```

according to current architecture.

---

# 27. OPS-DEG-013 — Durable outbox allows delayed delivery, not lost delivery

User-visible semantics distinguish source success from downstream pending effect where necessary.

---

# 28. Outbox growth

Broker outage can grow outbox/backlog.

---

# 29. OPS-DEG-014 — Broker outage has backpressure/capacity plan

Monitor:

```text
oldest age
row/queue growth
storage
consumer catch-up capacity
```

and shed optional producers if needed.

---

# 30. Consumer failure

If one consumer fails:

- isolate that consumer;
- preserve message identity/dedup;
- keep unrelated consumers running when safe.

---

# 31. OPS-DEG-015 — One failing consumer does not poison unrelated delivery globally

Poison identity is scoped to logical message + consumer.

---

# 32. Retry storm

A downstream outage can make every consumer retry.

---

# 33. OPS-DEG-016 — Retry has bounded backoff and jitter where appropriate

Retry rate must not exceed dependency recovery capacity or create self-amplifying outage.

---

# 34. Poison message

Deterministic invalid message should stop hot-loop retry and move to diagnosable poison/dead-letter handling.

---

# 35. OPS-DEG-017 — Poison handling preserves ordering semantics

For ordered streams, do not skip the poison item without deciding whether later messages can be processed safely.

---

# 36. Messaging recovery

Reopen consumer throughput gradually.

Watch backlog age, failure rate, dependency capacity, and ordering.

---

# 37. Realtime degradation

Realtime is freshness, not authoritative persistence.

---

# 38. OPS-DEG-018 — Realtime outage does not block authoritative writes by default

Clients degrade to:

```text
refetch
poll
manual refresh
reconnect
```

where product UX supports it.

---

# 39. Realtime stale state

While disconnected, client may become stale.

---

# 40. OPS-DEG-019 — Realtime reconnect reconciles authoritative state

Do not replay local assumptions as truth.

Use revision/gap/refetch semantics.

---

# 41. Realtime authorization

During outage/reconnect, current permission is reevaluated.

---

# 42. OPS-DEG-020 — Realtime recovery does not restore revoked subscriptions blindly

Membership/share/policy changes during outage must take effect.

---

# 43. Object-storage degradation

Object storage may affect:

```text
uploads
downloads
attachments
exports
document/media assets
```

---

# 44. OPS-DEG-021 — Object-storage failure does not fabricate successful file state

If upload did not durably complete:

```text
do not commit final attachment/file status
or
represent pending/failed according to designed workflow
```

---

# 45. Upload transaction

If DB record + object upload are multi-step, failure state/cleanup is explicit.

---

# 46. OPS-DEG-022 — Orphan/missing object is reconciled

Recovery handles:

```text
DB row without object
object without committed DB reference
failed multipart/pending upload
```

according to storage lifecycle.

---

# 47. Download degradation

Unavailable storage returns safe unavailable state.

Do not expose alternate unprotected object URLs.

---

# 48. Provider/integration degradation

Provider failure categories include:

```text
timeout
rate limit
5xx
auth/revocation
schema incompatibility
webhook outage
unknown outcome
```

---

# 49. OPS-DEG-023 — Provider failure is classified before retry

Different classes require different action:

```text
rate limit → backoff
revoked auth → reauthorization
invalid schema → terminal/quarantine
timeout/unknown → reconcile
```

---

# 50. Provider rate limit

Respect provider `Retry-After`/known quota semantics.

Reduce concurrency/batch if needed.

---

# 51. OPS-DEG-024 — Provider outage does not become Notrelix retry storm

Bound per-provider/Connection concurrency and use backoff.

---

# 52. Provider unavailable

Notrelix source operation may remain valid while provider synchronization/action is pending/failed.

---

# 53. OPS-DEG-025 — Provider dependency does not own source transaction unless product explicitly requires it

Prefer:

```text
commit source fact
→ durable pending external work
```

where architecture permits.

---

# 54. Unknown provider outcome

---

# 55. OPS-DEG-026 — Unknown external outcome is a distinct degraded state

Do not report final failure/success until reconciled if provider may have committed.

---

# 56. Integration sync degradation

Connection can be valid while sync is degraded.

Show:

```text
last successful sync
lag/status
reauthorization if needed
```

---

# 57. OPS-DEG-027 — Sync degradation does not rewrite connection identity

Do not delete/recreate Connection for transient provider outage.

---

# 58. Webhook degradation

If Notrelix webhook endpoint is unavailable, provider may retry.

Ensure replay/dedup on restoration.

---

# 59. OPS-DEG-028 — Webhook recovery assumes duplicates

Do not treat repeated callbacks after outage as new facts automatically.

---

# 60. Authentication dependency degradation

Authentication/session path is security-critical.

---

# 61. OPS-DEG-029 — Authentication degradation fails closed for new authority

Do not grant access because identity provider/session validation is unavailable.

Existing sessions may continue only according to explicit Identity/security contract.

---

# 62. External IdP

Enterprise IdP outage behavior follows configured session/authentication semantics.

Do not invent emergency local bypass accounts.

---

# 63. OPS-DEG-030 — SSO outage never creates unauthorized fallback

Break-glass administration, if ever supported, requires separate approved security design.

---

# 64. Frontend/CDN degradation

Web asset failure can prevent new sessions while existing loaded app continues.

---

# 65. OPS-DEG-031 — Frontend delivery incident accounts for old loaded clients

Backend compatibility remains important because users can retain cached/loaded prior build.

---

# 66. Partial asset deployment

Mixed/chunk version errors can require cache/CDN invalidation and compatible asset strategy.

Do not weaken backend contracts to compensate for broken asset deployment.

---

# 67. Mobile delivery

Mobile binary cannot be instantly repaired server-side.

---

# 68. OPS-DEG-032 — Server degradation strategy respects supported mobile clients

Use server-side compatible disable/pending/read-only behavior rather than requiring instant app upgrade for core recovery unless support policy permits it.

---

# 69. Search degradation

Search is derived.

If search fails:

```text
direct navigation/listing may remain
search shows unavailable/stale
```

depending on product UX.

---

# 70. OPS-DEG-033 — Search failure does not change source data

Do not perform broad destructive reindex/repair against source without recovery plan.

---

# 71. Analytics degradation

Analytics/reporting may be delayed while core writes remain available.

---

# 72. OPS-DEG-034 — Analytics freshness degradation is explicit

Show stale timestamp/status rather than pretending current data.

---

# 73. Automation degradation

Automation can be paused/throttled if delivery/provider dependencies fail.

---

# 74. OPS-DEG-035 — Automation pause preserves execution identity/history

Do not drop scheduled/event triggers silently.

Use durable pending/recovery semantics as designed.

---

# 75. Scheduled work

During outage, missed schedule catch-up follows Automation schedule policy.

Do not execute unbounded missed occurrences after recovery.

---

# 76. Billing degradation

Billing/payment provider failure can affect checkout/changes while existing customer work continues according to commercial policy.

---

# 77. OPS-DEG-036 — Payment-provider outage does not destroy existing product state

New commercial operation can be pending/unavailable while existing entitlements follow approved grace/current Billing state.

---

# 78. Entitlement service uncertainty

If authoritative current entitlement cannot be determined:

- fail closed for premium expansion where required;
- use explicit grace only if Billing contract defines it.

---

# 79. OPS-DEG-037 — Degradation cannot invent unlimited entitlement

Do not default paid limits to unlimited because Billing/provider is unavailable.

---

# 80. Background optional load

During saturation shed:

```text
analytics rebuild
large export
backfill
noncritical sync
index maintenance
optional precomputation
```

before correctness-critical writes where possible.

---

# 81. OPS-DEG-038 — Load shedding has priority order

Define which work is:

```text
critical
important
deferrable
optional
```

per capability.

Do not randomly reject based on arrival order alone when prioritization matters.

---

# 82. Tenant fairness

One noisy tenant can cause shared degradation.

---

# 83. OPS-DEG-039 — Degraded capacity preserves noisy-neighbor controls

Use tenant-aware limits/queues where architecture provides them.

Do not allow one tenant's retry/backfill to consume all shared recovery capacity.

---

# 84. Capacity scaling

Scaling out is useful only when bottleneck is horizontally scalable and dependency can tolerate it.

---

# 85. OPS-DEG-040 — Scale after identifying bottleneck

More workers/connections can worsen:

- DB lock;
- provider rate limit;
- queue poison;
- cache stampede.

---

# 86. Circuit breaker

A circuit breaker can protect a failing dependency when idempotency/failure semantics are understood.

It is not mandatory for every call.

---

# 87. OPS-DEG-041 — Circuit open state maps to explicit product outcome

Caller sees:

```text
pending
temporarily unavailable
degraded
```

according to contract, not generic hidden success.

---

# 88. Timeout

Every external/network operation should have bounded timeout appropriate to the operation.

---

# 89. OPS-DEG-042 — Timeout includes outcome semantics

For read:

```text
timeout → no result
```

can be clear.

For external write:

```text
timeout → outcome may be unknown
```

and requires reconciliation.

---

# 90. Bulkhead

Isolation of worker/provider/tenant resource pools can reduce cascading failure.

Adopt when workload justifies complexity.

---

# 91. OPS-DEG-043 — Failure isolation follows actual failure domain

Do not create arbitrary pools solely for architecture fashion.

---

# 92. Fallback

Fallback is valid only when result semantics remain true.

---

# 93. OPS-DEG-044 — Fallback does not fabricate equivalent behavior

Examples:

```text
search unavailable → show search unavailable
```

may be safer than:

```text
scan entire Workspace DB in memory as "fallback"
```

which can cause capacity/security issues.

---

# 94. Stale reads

Stale reads require an explicit freshness/security contract.

Good candidates can be:

- public/static reference data;
- approved derived analytics with timestamp.

Risky candidates:

- permissions;
- membership;
- revoked shares;
- financial state;
- rapidly mutable protected content.

---

# 95. OPS-DEG-045 — Security-sensitive state is not served stale casually

Current authorization/resource visibility should prefer fail-safe behavior.

---

# 96. Write queueing

Queueing user writes while DB is down changes contract substantially.

Do not introduce ad hoc in-memory “offline write queue” server-side.

---

# 97. OPS-DEG-046 — Deferred write requires durable accepted-work contract

If product supports accepted/pending:

```text
durable operation ID
authorization
tenant scope
idempotency
eventual result
user-visible state
```

are explicit.

---

# 98. In-memory buffering

In-memory buffer can be lost on restart.

It is not durable acceptance.

---

# 99. OPS-DEG-047 — Volatile buffering cannot return durable success

At most it can be best-effort telemetry/noncritical data according to design.

---

# 100. Restoration

Returning to normal mode is a controlled transition.

---

# 101. OPS-DEG-048 — Dependency recovery is verified before full workload reopening

Verify:

```text
dependency health
error/latency
capacity
backlog catch-up
stale cache/subscriptions
representative product flow
tenant/security
```

---

# 102. Gradual reopen

After broad outage:

```text
restore dependency
→ reopen limited concurrency/cohort
→ observe
→ expand
```

when backlog/capacity risk warrants it.

---

# 103. OPS-DEG-049 — Recovery does not unleash backlog at maximum concurrency automatically

Catch-up rate is bounded by:

- DB;
- provider;
- downstream consumer;
- tenant fairness.

---

# 104. Cache restoration

Avoid simultaneous mass warmup if it can stampede origin.

---

# 105. Realtime restoration

Clients may reconnect in a surge.

Use reconnect/backoff and authoritative reconciliation.

---

# 106. OPS-DEG-050 — Reconnect storm is a capacity scenario

Server/client strategies should avoid synchronized aggressive reconnect loops.

---

# 107. Provider restoration

Provider may remain rate-limited after outage.

Ramp sync/action throughput gradually.

---

# 108. Messaging restoration

Monitor:

```text
oldest backlog age
processing rate
failure
poison/order
DB/provider saturation
```

until steady state.

---

# 109. User communication

If capability is degraded materially, surface accurate status in product/status communication as appropriate.

Do not expose internal dependency details unnecessarily.

---

# 110. OPS-DEG-051 — User messaging distinguishes unavailable, pending, and stale

These imply different expected behavior/recovery.

---

# 111. Status page

Public status communication, if adopted, belongs to Operations.

It should map to customer capabilities rather than only internal component names where possible.

---

# 112. Observability

Each degraded mode should have:

```text
trigger signal
current mode
affected capability
owner
recovery criterion
```

---

# 113. OPS-DEG-052 — Degraded mode is observable

Operators should know whether the system is currently:

```text
normal
cache-bypass
read-only
consumer-paused
provider-backoff
realtime-degraded
```

where such modes exist.

---

# 114. Manual degradation switch

If a manual kill/degrade flag exists, it must be:

- authorized;
- auditable;
- tested;
- reversible;
- documented.

---

# 115. OPS-DEG-053 — Manual degradation cannot become forgotten production config

After incident, reconcile temporary config/flags with canonical desired state.

---

# 116. Automatic degradation

Automatic circuit/fallback logic should have bounded state transition and observability.

Do not oscillate rapidly.

---

# 117. OPS-DEG-054 — Automatic degradation has hysteresis/recovery semantics where needed

Avoid flapping between:

```text
healthy
degraded
```

under threshold noise.

Exact thresholds remain operational configuration.

---

# 118. Failure isolation

Disable only affected subsystem where product dependencies permit it.

---

# 119. OPS-DEG-055 — Optional subsystem failure does not take down unrelated capabilities

Examples:

```text
Analytics failure
≠ login outage

one provider outage
≠ all Integrations

one Automation consumer poison
≠ all messaging
```

unless shared dependency itself is the failure.

---

# 120. Security during incident

Emergency changes do not disable:

- authorization;
- RLS;
- webhook validation;
- secret controls.

---

# 121. OPS-DEG-056 — Emergency availability shortcut cannot reduce tenant/security guarantee

If secure operation is impossible, prefer unavailable/limited mode.

---

# 122. Data-loss avoidance

Do not accept work that cannot be durably committed merely to keep API success rate high.

---

# 123. OPS-DEG-057 — Availability metric does not justify false success

A failed/pending write should be reported honestly even if this increases error/degraded metrics.

---

# 124. Service-dependency table

Each critical dependency should eventually have an operational table with:

```text
dependency
owner
product capabilities
authoritative/derived/optional role
failure signal
safe degraded behavior
unsafe shortcut
recovery criterion
runbook
```

This canonical doc defines the schema; implementation docs can populate concrete dependencies.

---

# 125. Database checklist

```text
[ ] connection/latency/lock signal
[ ] recent migration/release
[ ] write safety
[ ] read-only feasibility
[ ] optional load shedding
[ ] bounded retry
[ ] RLS
[ ] reopen criterion
```

---

# 126. Cache checklist

```text
[ ] authoritative fallback exists?
[ ] DB capacity
[ ] permission-sensitive data
[ ] stampede risk
[ ] bypass mode
[ ] invalidate stale generation
[ ] gradual warmup
```

---

# 127. Messaging checklist

```text
[ ] broker/outbox state
[ ] oldest backlog age
[ ] consumer isolation
[ ] retry/backoff
[ ] poison/order
[ ] dedup
[ ] catch-up capacity
[ ] tenant fairness
```

---

# 128. Provider checklist

```text
[ ] provider/Connection
[ ] failure class
[ ] rate limit
[ ] auth/revocation
[ ] idempotency/correlation
[ ] unknown outcome
[ ] current sync lag
[ ] bounded backoff
[ ] restoration ramp
```

---

# 129. Realtime checklist

```text
[ ] authoritative writes remain?
[ ] fallback refetch/poll
[ ] stale UX
[ ] reconnect backoff
[ ] gap reconciliation
[ ] permission revalidation
[ ] reconnect surge
```

---

# 130. Restoration checklist

```text
[ ] dependency actually healthy
[ ] changed release/config known
[ ] degraded mode visible
[ ] backlog/stale state understood
[ ] capacity headroom
[ ] representative product flow
[ ] tenant/security verified
[ ] gradual reopen if needed
[ ] temporary flags/config cleaned
```

---

# 131. Current architecture alignment

Current backend Infrastructure architecture states:

```text
PostgreSQL/EF is authoritative persistence mechanism
Redis/cache is scoped acceleration/adapter
provider/storage/search are adapters
RLS complements Application authorization
```

Current Platform architecture states:

```text
outbox commits with source state
post-commit work follows successful commit
consumer idempotency/order/poison/retry are explicit
background tenant context is explicit
```

The degraded modes in this document preserve those contracts rather than replacing them.

---

# 132. Stop conditions

Stop and choose a safer degraded mode if:

- traffic can only be restored by bypassing auth/RLS;
- DB is unavailable and code is about to invent writable in-memory truth;
- stale permission/share cache is being served as emergency authority;
- broker outage will drop messages rather than preserve durable outbox;
- retry storm is increasing dependency failure;
- realtime outage blocks source writes unnecessarily;
- storage upload failure is reported as final success;
- provider timeout is blindly retried despite unknown outcome;
- auth/IdP failure is being bypassed with unauthorized fallback;
- search fallback proposes full unbounded tenant scan;
- Billing/provider outage defaults users to unlimited entitlement;
- backlog is reopened at unlimited concurrency;
- temporary degradation flag has no visible state/removal path.

---

# 133. Related canonical owners

```text
docs/operations/observability.md
docs/operations/incident-readiness.md
docs/operations/recovery-and-data-safety.md
docs/delivery/release-and-rollout.md
docs/quality/security-quality-standard.md
docs/quality/performance-and-scalability.md
docs/architecture/data-ownership-and-consistency.md
docs/architecture/events-realtime-and-delivery-boundary.md
backend/docs/architecture/infrastructure-and-data.md
backend/docs/architecture/platform-and-messaging.md
```

---

# 134. Final degradation rule

When a dependency degrades, answer:

```text
Is this dependency authoritative, derived, optional, or an external side effect?
Which product capabilities are affected?
Can we safely continue reads?
Can we safely continue writes?
Can work be durably queued?
What is the honest user-visible state?
Which retry/backoff/circuit behavior prevents amplification?
Which tenant/security properties must remain identical?
Which stale/cache/realtime state must be invalidated or reconciled?
What objective evidence permits normal workload to reopen?
```

The target is:

> **degraded operation that reduces capability without reducing truth: authoritative state stays authoritative, security stays intact, retries stay bounded, pending/unknown states stay honest, and recovery reopens workload only after dependencies and downstream state have actually converged.**
