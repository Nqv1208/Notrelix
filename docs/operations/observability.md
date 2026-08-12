---
document_id: OPS-OBSERVABILITY
document_type: operations-standard
status: active
owner: operations
applies_to:
  - runtime
  - api
  - background-processing
  - messaging
  - realtime
  - database
  - integrations
  - frontend
  - mobile
evidence:
  - docs/engineering/06-operations/01-operational-readiness.md
  - docs/engineering/06-operations/04-messaging-data-pipeline-runbook.md
  - docs/engineering/06-operations/06-sli-slo-alerting.md
  - docs/quality/engineering-quality-standard.md
  - docs/quality/security-quality-standard.md
  - docs/quality/performance-and-scalability.md
  - docs/delivery/release-and-rollout.md
  - docs/architecture/events-realtime-and-delivery-boundary.md
  - docs/architecture/data-ownership-and-consistency.md
review_on:
  - observability-model-change
  - telemetry-vendor-change
  - sli-or-slo-change
  - alerting-policy-change
  - new-critical-dependency
  - new-background-consumer
  - new-provider-integration
  - incident-learnings-change
---

# Observability

> **Observability exists to answer product and operational questions from runtime evidence without requiring operators to guess internal implementation state.**
>
> Logs, metrics, traces, health signals, and dashboards are evidence. They are not substitutes for product truth, security controls, or runbooks.

This document is the canonical repository-level owner for runtime observability, SLI/SLO modeling, alerting semantics, correlation, and operational-readiness evidence.

It intentionally does **not** choose an observability vendor or invent numerical SLO targets.

Concrete instrumentation packages, exporter endpoints, dashboards, and deployment configuration belong to backend/frontend/infrastructure/operations implementation docs.

---

# 1. Purpose

A production capability is operationally observable when operators can answer:

```text
what is failing?
who/what is affected?
when did it start?
which release/config/migration changed?
is the failure local or dependency-driven?
is data/security/tenant correctness at risk?
is work queued, stuck, duplicated, or lost?
is the service recovering?
```

without destructive experimentation.

---

# 2. OPS-OBS-001 — Observability follows semantic identifiers

Telemetry should carry stable identifiers appropriate to the operation, such as:

```text
correlation / trace
logical operation
Account / Workspace scope where policy permits
resource kind + resource ID
event/message identity
consumer identity
Automation execution
Integration connection/provider operation
migration/backfill job
release/cohort/version
```

Do not rely only on class names, thread IDs, or machine-local implementation details.

---

# 3. Correlation

Correlation should connect:

```text
HTTP request
→ Application operation
→ database transaction
→ outbox/message
→ consumer
→ provider/realtime side effect
```

when those boundaries participate in one logical flow.

---

# 4. OPS-OBS-002 — Correlation does not become authorization

A correlation ID or tenant tag in telemetry is diagnostic metadata.

It MUST NOT be accepted as proof of tenant scope or permission.

---

# 5. Telemetry classes

Canonical classes:

```text
logs
metrics
traces
health/readiness
product-visible durable state
```

Each answers different questions.

---

# 6. Logs

Logs record discrete diagnostic events.

Good structured log fields can include:

```text
operation
outcome
resource kind
safe resource identifier
consumer/event identity
attempt
provider
error category
correlation
release/version
```

---

# 7. OPS-OBS-003 — Logs are structured around operations, not prose archaeology

Prefer structured fields over free-form strings that require regex parsing to discover:

- tenant scope;
- provider;
- event type;
- retry state;
- failure class.

---

# 8. Sensitive logging

Security/privacy standards still apply.

Never log reusable secrets, raw payment data, OAuth tokens, session tokens, full private document/comment payloads, or provider credentials.

---

# 9. OPS-OBS-004 — Diagnostic usefulness does not justify sensitive payload dumping

Use stable identity, safe metadata, hashes/fingerprints where appropriate, and correlation instead of full confidential payload.

---

# 10. Error logs

An error log should classify enough to distinguish:

```text
validation/client rejection
authorization rejection
concurrency conflict
dependency outage
transient provider failure
terminal provider failure
unknown provider outcome
internal defect
```

where the difference changes response/retry.

---

# 11. OPS-OBS-005 — Expected client rejection is not server-failure noise

Valid validation/authorization/conflict outcomes should not inflate internal-failure metrics blindly.

They may have separate product/security monitoring where useful.

---

# 12. Metrics

Metrics describe rates, counts, distributions, saturation, backlog, lag, and state.

Prefer bounded-cardinality labels.

---

# 13. OPS-OBS-006 — Metric labels have controlled cardinality

Avoid labels such as:

```text
raw URL with IDs
full exception message
email
document title
arbitrary user input
provider payload
```

that create unbounded cardinality or privacy risk.

---

# 14. Metric identity

Metric names/labels should represent stable operational meaning, not transient implementation folder names.

---

# 15. Traces

Tracing connects latency and causality across relevant synchronous/distributed boundaries.

Use sampling appropriate to production cost and diagnostic need.

---

# 16. OPS-OBS-007 — Trace propagation respects trust boundaries

Incoming trace/correlation headers from untrusted clients may be accepted/rebased according to instrumentation policy, but cannot be trusted as security context.

---

# 17. Health signals

Health is layered:

```text
process alive
process ready
critical dependency reachable
capability functioning
user journey succeeding
```

These are not equivalent.

---

# 18. OPS-OBS-008 — Liveness is not product health

A process returning `200 /health` while:

- DB writes fail;
- consumers are stuck;
- auth is broken;
- provider sync is poisoned;

is not evidence that affected capability is healthy.

---

# 19. Readiness

Readiness should indicate whether an instance can safely receive its intended traffic/work.

Do not make a non-critical optional dependency prevent all traffic unless architecture requires it.

---

# 20. Dependency observability

For each critical dependency identify:

```text
availability
latency
timeout
retry
rate limit
connection pool/saturation
error category
degradation behavior
```

---

# 21. OPS-OBS-009 — Dependency metrics are diagnostics, not user-impact SLI replacements

High Redis CPU matters only insofar as it threatens capability.

A healthy dependency does not prove users can complete a workflow.

---

# 22. Capability view

Observability should group signals by user/product capability, for example:

```text
authentication/session
Workspace access
Work Management mutations/queries
document editing
collaboration
Automation execution
Integration sync
Billing administration
analytics freshness
```

not only by infrastructure component.

---

# 23. SLI

A Service Level Indicator measures a defined user-visible or service capability property.

Candidate SLIs can include:

```text
successful authenticated API operation rate
critical mutation latency
critical user journey success
realtime convergence delay
background job age
message backlog age
Integration sync freshness
Analytics data freshness
```

Exact approved indicators depend on capability.

---

# 24. OPS-OBS-010 — SLI follows user-visible capability

Do not define service quality only as:

```text
CPU < X
memory < Y
pod count healthy
```

when the product action can still fail.

---

# 25. SLI definition

An SLI definition states:

```text
scope/capability
measurement source
numerator
denominator
valid exclusions
aggregation
window
dimensions
```

---

# 26. OPS-OBS-011 — SLI exclusion is explicit

Examples:

```text
valid client validation rejection
user cancellation
intentional authorization denial
maintenance window if policy permits
```

must not be silently removed from denominators.

---

# 27. Latency SLI

Use distributions/percentiles when tail latency matters.

Averages alone can hide severe tail failures.

---

# 28. OPS-OBS-012 — Critical latency objectives do not rely on average alone

When latency matters operationally, report distributions/percentiles consistent with approved SLO design.

---

# 29. Availability SLI

Availability should reflect whether the intended operation succeeds for valid requests.

Separate dependency availability from product operation availability.

---

# 30. Freshness SLI

Derived systems such as:

```text
realtime projections
search
analytics
integration sync
```

may need freshness/lag indicators rather than only request success.

---

# 31. OPS-OBS-013 — Eventual consistency has observable lag

If a capability promises eventual convergence, operators need a signal for:

```text
age
backlog
latest processed revision
reconciliation delay
```

appropriate to the system.

---

# 32. SLO

An SLO is an approved objective for an SLI over a window.

This document defines the model, **not arbitrary numerical objectives**.

---

# 33. OPS-OBS-014 — Numerical SLOs remain explicit TBD until approved

Do not invent enterprise-looking values for:

```text
availability
latency
error budget
RPO
RTO
paging threshold
```

without product/operations/business approval and deployment evidence.

---

# 34. SLO approval

When a numerical SLO is introduced, record:

```text
capability
SLI definition
objective
window
measurement source
owner
consumer/product rationale
review cadence
```

---

# 35. Error budget

If an error-budget model is adopted, it guides release/risk decisions.

It does not authorize known security/data correctness defects.

---

# 36. OPS-OBS-015 — Security and data-integrity incidents are not budgeted away

Tenant leak, secret exposure, corruption, or unauthorized access can require immediate action regardless of availability error budget.

---

# 37. Alerting

Alerts exist to cause an actionable response.

Not every anomalous metric should page.

---

# 38. OPS-OBS-016 — Paging alert is actionable

A paging alert has:

```text
affected capability
owner
runbook/first actions
diagnostic dashboard/query
severity/impact context
```

If nobody can act, it should not page in that form.

---

# 39. Symptom versus cause

Prefer user-impact/burn/symptom alerts for paging.

Use infrastructure cause metrics for diagnosis and capacity trending.

---

# 40. OPS-OBS-017 — Raw resource threshold is not automatically a paging alert

High CPU, memory, queue depth, or DB connections can be:

- page;
- ticket;
- capacity signal;

depending on user impact and failure risk.

---

# 41. Alert severity

Severity considers:

```text
user impact
data integrity/confidentiality
tenant scope
duration
propagation
workaround
recovery risk
```

Incident severity is owned by `incident-readiness.md`.

---

# 42. Alert ownership

Every alert has a logical owner/capability route.

Do not depend on a current individual's name in canonical policy.

---

# 43. OPS-OBS-018 — Alert ownership follows capability, not dashboard author

Changing the person/team maintaining a dashboard does not change semantic service ownership.

---

# 44. Alert noise

Review:

```text
false positives
duplicates
non-actionable pages
stale alerts
alert storms
```

after incidents and architecture changes.

---

# 45. OPS-OBS-019 — Noisy alert is operational debt

Do not train operators to ignore recurring pages.

Fix threshold/signal/runbook/root cause.

---

# 46. Alert deduplication

Many instances failing for the same dependency should not necessarily create hundreds of independent pages.

Use grouping appropriate to failure domain.

---

# 47. Alert testability

Where practical, test alerts with:

- synthetic traffic;
- staging;
- controlled failure;
- replayed telemetry.

---

# 48. OPS-OBS-020 — Paging alerts are verified

A critical alert that has never been exercised should not be assumed reliable indefinitely.

---

# 49. Dashboard

Dashboard is an operational navigation surface, not canonical truth.

It should answer a concrete diagnostic question.

---

# 50. OPS-OBS-021 — Dashboard has a stated operational question

Examples:

```text
Are Work Management writes failing?
Is Automation backlog growing?
Which provider is rate limited?
Is realtime convergence delayed?
Did release cohort regress?
```

Avoid decorative metric walls.

---

# 51. Dashboard hierarchy

Useful levels:

```text
capability overview
dependency detail
consumer/job detail
release/cohort detail
tenant-safe drill-down
```

---

# 52. Release observability

Material releases should be traceable by:

```text
release/SHA
artifact version
feature flag/cohort
migration phase
```

where instrumentation supports it.

---

# 53. OPS-OBS-022 — Rollout health is attributable to the changed version/cohort

Operators should be able to compare:

```text
old vs new
control vs cohort
before vs after
```

without guessing deployment timestamps manually.

---

# 54. Migration observability

Large migration/backfill exposes:

```text
processed
remaining
failed/quarantined
checkpoint
rate
age
mismatch
```

---

# 55. OPS-OBS-023 — Migration completion is observable

“Process running” or “job exited 0” is not enough.

The migration policy owns semantic completion proof; observability exposes the evidence.

---

# 56. Messaging observability

For outbox/message consumers track as relevant:

```text
published/claimed/processed
oldest backlog age
queue depth
attempt/retry
poison/dead-letter
consumer identity
processing latency
dedup outcome
ordering blockage
```

---

# 57. OPS-OBS-024 — Message backlog age matters more than raw count alone

A large healthy burst can have high count but low age.

A small permanently stuck set can have low count but severe correctness impact.

Use both as appropriate.

---

# 58. Consumer identity

Metrics/logs should distinguish logical consumer.

Do not aggregate unrelated consumers so completely that one stuck path is invisible.

---

# 59. OPS-OBS-025 — Retry reasons are classified

Operators should distinguish:

```text
transient dependency
rate limit
poison/invalid
authorization/tenant
schema/contract
unknown external outcome
```

rather than one generic retry counter.

---

# 60. Ordering observability

Ordered consumers need:

```text
ordering key/stream category
last successful sequence/cursor
blocked message identity
age
```

without exposing sensitive payload.

---

# 61. OPS-OBS-026 — Ordering stall is diagnosable without advancing cursor unsafely

Observability should reveal blockage so operators do not “fix” by blindly skipping/altering sequence state.

---

# 62. Outbox

Observe:

```text
undispatched age
claim/reclaim
dispatch failures
producer context
```

---

# 63. Idempotency

Observe exceptional states:

```text
duplicate replay
conflicting key/request
stuck in-progress
recovered/expired record
```

Avoid high-cardinality raw request keys in metrics.

---

# 64. Realtime

Observe:

```text
connected clients
subscription failures
publish failures
reconnect
gap/reconciliation triggers
delivery/convergence lag
```

where useful.

---

# 65. OPS-OBS-027 — Realtime health includes convergence, not socket count only

Many connected sockets do not prove clients receive current authorized state.

---

# 66. Integrations

Per provider/Connection class observe:

```text
webhook acceptance/rejection
signature/replay failures
sync lag
rate limit
reauthorization required
provider operation outcome
unknown outcome
mapping conflict
```

---

# 67. OPS-OBS-028 — Provider telemetry distinguishes provider failure from Notrelix defect

This enables correct containment/degradation decisions.

---

# 68. Automation

Observe:

```text
trigger-to-execution delay
execution success/failure
retry
stuck running
schedule lag
recursion/runaway protection
provider action failure
```

---

# 69. OPS-OBS-029 — Automation retries do not hide repeated business failure

A job that eventually succeeds after excessive retries can still indicate degraded service.

Expose attempt distribution/age as needed.

---

# 70. Billing

Observe safely:

```text
provider callback failures
subscription reconciliation
usage ingestion failures
invoice/payment workflow health
entitlement-resolution failures
```

Never expose payment secrets.

---

# 71. Analytics/search

Observe:

```text
projection lag
snapshot/report generation failure
index freshness
query latency
rebuild/backfill progress
```

---

# 72. Database

Operational DB signals can include:

```text
connection saturation
query latency
locks
deadlocks
migration duration
replication/backup health where applicable
storage
```

These are diagnostics for affected capabilities.

---

# 73. OPS-OBS-030 — DB availability does not prove data correctness

After migration/recovery, verify application-level invariants and tenant/RLS behavior, not only DB connectivity.

---

# 74. Cache

Observe:

```text
hit/miss where useful
latency/error
eviction/cardinality
origin amplification
```

Do not use cache success as product truth.

---

# 75. Frontend observability

Client signals can include:

```text
critical journey failure
API error category
runtime exception
route/build version
realtime reconnect
performance experience
```

subject to privacy and sampling.

---

# 76. OPS-OBS-031 — Frontend telemetry is privacy-minimized

Do not record:

- document/comment content;
- credentials;
- sensitive form values;
- broad DOM snapshots;

without explicit product/privacy design.

---

# 77. Mobile

Mobile telemetry should preserve:

```text
app version
platform
critical journey
network/realtime failure class
```

because client versions can lag.

---

# 78. OPS-OBS-032 — Client version is operationally visible for compatibility incidents

Operators should be able to distinguish failures concentrated in old supported mobile/web builds.

---

# 79. Operational readiness

Before a new capability/failure mode is production-ready, define:

```text
health/dependency signals
structured logs/correlation
critical metrics
alert owner
safe disable/degrade mechanism
migration/backfill signal
capacity risk
recovery path
```

---

# 80. OPS-OBS-033 — New durable consumer has backlog/retry/poison observability

A background consumer is not operationally ready if operators cannot detect:

- stuck;
- growing;
- retry-looping;
- poison-blocked.

---

# 81. Dependency table

For critical dependencies record:

```text
dependency
capability
can degrade?
user-visible behavior
timeout
retry/backoff
capacity signal
runbook
```

The service-degradation owner will define detailed degradation policy.

---

# 82. OPS-OBS-034 — “Degradable” still has a correctness story

Example:

```text
Redis unavailable
→ bypass cache through authoritative tenant-safe DB reads
```

may be valid.

```text
Database unavailable
→ invent writable guessed state
```

is not.

---

# 83. Telemetry failure

Observability infrastructure itself can fail.

The application should degrade according to approved policy rather than block critical product writes merely because telemetry export is unavailable, unless compliance/security requirements say otherwise.

---

# 84. OPS-OBS-035 — Telemetry exporter is not product transaction authority

Instrumentation should not become an unbounded synchronous dependency on critical request/transaction path.

---

# 85. Sampling

Sampling decisions balance cost and diagnosis.

Do not sample away every rare failure class.

---

# 86. OPS-OBS-036 — Errors and critical security/data signals have deliberate sampling policy

Uniform low-rate sampling can make rare severe incidents invisible.

---

# 87. Retention

Telemetry retention follows operational/privacy/legal needs.

Do not keep raw detailed logs forever by default.

---

# 88. OPS-OBS-037 — Telemetry retention matches diagnostic value and data sensitivity

High-volume/debug payload retention should be bounded and justified.

---

# 89. Operational queries

Important runbooks should include enough query/navigation guidance that operators can reach the relevant telemetry.

Vendor-specific syntax can live in implementation/runbook docs.

---

# 90. OPS-OBS-038 — Canonical observability policy is vendor-neutral

Do not encode architecture around one dashboard/vendor query language unless a separate infrastructure decision explicitly chooses it.

---

# 91. SLO/change relationship

New SLOs may force architecture/capacity/rollout changes.

They are not merely dashboard configuration.

---

# 92. OPS-OBS-039 — Approved SLO is a product/operations contract

Changing numerical objective/window materially requires owner review and corresponding capacity/alerting/release impact assessment.

---

# 93. Error-budget relationship

If used, release policy can reduce rollout risk when budget is heavily consumed.

Security/data incidents remain independently actionable.

---

# 94. Synthetic checks

Synthetic flows are useful for:

```text
login
critical API
Workspace bootstrap
core work mutation
```

when deterministic and safe.

---

# 95. OPS-OBS-040 — Synthetic check uses controlled test identity/data

Do not mutate arbitrary real customer state to prove uptime.

---

# 96. Operational readiness checklist

```text
[ ] capability owner
[ ] user-impact SLI candidate
[ ] dependency signals
[ ] correlation identifiers
[ ] logs safe/structured
[ ] metrics bounded-cardinality
[ ] background backlog/retry if applicable
[ ] provider/realtime/migration signals
[ ] alert owner/runbook
[ ] disable/degrade path
[ ] release/cohort attribution
[ ] no invented SLO numbers
```

---

# 97. SLI/SLO checklist

```text
[ ] capability
[ ] measurement source
[ ] numerator
[ ] denominator
[ ] exclusions
[ ] latency/freshness distribution
[ ] window
[ ] objective approved or TBD
[ ] owner
[ ] alert relationship
```

---

# 98. Paging-alert checklist

```text
[ ] actionable symptom
[ ] affected capability
[ ] owner
[ ] runbook
[ ] diagnostic dashboard/query
[ ] dedup/grouping
[ ] tested
[ ] noise reviewed
```

---

# 99. Messaging checklist

```text
[ ] oldest backlog age
[ ] queue/outbox depth
[ ] consumer identity
[ ] retry reasons
[ ] poison identity
[ ] dedup state
[ ] ordering cursor/blockage
[ ] provider/downstream failure
[ ] recovery verification
```

---

# 100. Stop conditions

Stop and fix observability design if:

- logs require secret/private payload dumps to diagnose normal failures;
- metric labels include unbounded resource/user text;
- SLO numbers are invented without approval;
- paging is based only on noisy CPU/memory with no actionability;
- a durable consumer has no backlog age/retry/poison signal;
- provider failures cannot be distinguished from internal failures;
- rollout health cannot be attributed to version/cohort;
- migration/backfill has no progress/completion telemetry;
- realtime health is inferred only from connection count;
- telemetry exporter can block critical product transaction indefinitely;
- dashboards exist but no operator can explain the question each answers.

---

# 101. Related canonical owners

```text
docs/operations/incident-readiness.md
docs/operations/recovery-and-data-safety.md
docs/operations/service-degradation.md
docs/delivery/release-and-rollout.md
docs/quality/security-quality-standard.md
docs/quality/performance-and-scalability.md
docs/architecture/events-realtime-and-delivery-boundary.md
```

---

# 102. Final observability rule

For every production capability, answer:

```text
Which user-visible property tells us it is healthy?
Which semantic identifiers correlate the flow?
Which logs/metrics/traces diagnose failure safely?
Which backlog/freshness/lag matters?
Which dependencies can be distinguished?
Which alert is actionable and who owns it?
Which SLO is approved, or explicitly TBD?
How is release/cohort/migration state visible?
Can operators verify recovery without changing/deleting evidence?
```

The target is:

> **runtime evidence organized around product capability and logical work, with actionable alerts and explicitly approved objectives, so operators can diagnose and recover without guessing, leaking sensitive data, or mistaking infrastructure activity for user-visible correctness.**
