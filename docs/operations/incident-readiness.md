---
document_id: OPS-INCIDENT-READINESS
document_type: operations-standard
status: active
owner: operations
applies_to:
  - runtime
  - incidents
  - production
  - security-events
  - data-integrity-events
  - release-failures
  - dependency-failures
evidence:
  - docs/engineering/06-operations/00-incident-response.md
  - docs/engineering/06-operations/01-operational-readiness.md
  - docs/engineering/06-operations/04-messaging-data-pipeline-runbook.md
  - docs/engineering/06-operations/05-contract-compatibility-runbook.md
  - docs/operations/observability.md
  - docs/delivery/release-and-rollout.md
  - docs/delivery/migration-policy.md
  - docs/quality/security-quality-standard.md
review_on:
  - incident-process-change
  - severity-model-change
  - escalation-model-change
  - new-critical-runtime
  - incident-learnings-change
  - release-recovery-change
---

# Incident Readiness

> **Incident response protects tenant safety, data integrity, confidentiality, and controlled recovery before it optimizes for a green dashboard.**
>
> The objective is to detect, contain, diagnose, recover, verify, and learn while preserving evidence and avoiding secondary damage.

This document is the canonical owner for repository-level incident-readiness and response semantics.

It does not invent an organization-specific phone tree, paging vendor, or personnel roster.

Concrete contact rotations/escalation destinations belong to operational configuration/runbooks.

---

# 1. Incident definition

An incident is an unplanned production condition requiring coordinated response because it threatens one or more:

```text
customer availability
data integrity
data confidentiality
tenant isolation
security
critical product correctness
commercial correctness
capacity/stability
external/provider consistency
```

---

# 2. OPS-INC-001 — Stabilize tenant/data/security safety first

On detection:

```text
establish incident coordination
identify affected capability/scope
assess tenant/data/security risk
contain unsafe writes/rollout/consumer if necessary
preserve evidence
```

Do not trade correctness for appearance of availability.

---

# 3. Incident versus ordinary defect

Not every bug is an incident.

Incident characteristics can include:

- active production impact;
- propagation;
- unknown blast radius;
- data/security risk;
- need for coordinated containment.

---

# 4. OPS-INC-002 — Severity follows impact, not visibility

A low-volume tenant leak or silent data corruption can be more severe than a highly visible cosmetic outage.

Assess independently:

```text
availability
data integrity
confidentiality
tenant isolation
financial impact
propagation
duration
workaround
```

---

# 5. Severity model

This document defines dimensions, not fixed organization labels such as SEV1/SEV2.

If numeric/severity levels are introduced operationally, map them to these dimensions and escalation expectations.

---

# 6. Incident roles

A material incident needs clear responsibilities such as:

```text
incident coordinator
technical investigators/owners
communications owner
scribe/timeline owner
```

One person may hold several roles in a small response.

---

# 7. OPS-INC-003 — Incident coordinator coordinates, not personally diagnoses everything

The coordinator maintains:

- shared state;
- priorities;
- decisions;
- ownership;
- communication;
- next checkpoint.

Subject-matter owners investigate.

---

# 8. Communication channel

Use one discoverable incident channel/timeline location.

Avoid fragmented private threads where decisions disappear.

---

# 9. OPS-INC-004 — Incident decisions are time-stamped

Record:

```text
detection
scope changes
containment
deploy/config action
hypothesis
recovery action
verification
closure
```

This supports safe reasoning and later learning.

---

# 10. First assessment

Immediately identify:

```text
what capability is failing?
when did it start?
who/which tenants are affected?
is data/security at risk?
recent release/config/migration?
dependency/provider state?
is impact growing?
```

---

# 11. OPS-INC-005 — Last-known-good is evidence, not automatic rollback target

Identify previous revision/config/schema.

Do not rollback until compatibility and data/external effects are understood.

---

# 12. Preserve evidence

Useful evidence can include:

```text
logs/traces/metrics
release SHA
config version
migration state
DB snapshots/checksums
message/backlog state
provider operation IDs
consumer/dedup state
```

---

# 13. OPS-INC-006 — Do not delete evidence to make monitoring green

Forbidden blind actions include:

```text
purge queue
delete poison record
clear dedup state
truncate error table
delete corrupted rows
wipe logs
```

before understanding consequences.

---

# 14. Containment

Containment limits new damage.

Examples:

```text
pause rollout
disable feature flag
stop one consumer
disable one provider integration
rate limit
switch capability read-only when product semantics support it
block one mutation path
```

---

# 15. OPS-INC-007 — Containment is as narrow as safely possible

If one consumer/provider/path is failing, avoid disabling unrelated product capability when they can remain safe.

---

# 16. Read-only containment

Read-only is valid only when reads remain correct and security-safe.

Do not serve stale/guessed writable truth because DB is unavailable.

---

# 17. OPS-INC-008 — Degraded mode preserves product invariants

Containment must not bypass:

- authorization;
- tenant isolation;
- idempotency;
- data ownership.

---

# 18. Rollout containment

When incident correlates with rollout:

```text
pause expansion
identify cohort/version
compare control/new
disable new path if safe
```

---

# 19. OPS-INC-009 — Rollout is stopped before broadening uncertain impact

Do not continue percentage expansion while the changed cohort has unexplained correctness/security failures.

---

# 20. Diagnosis loop

Use a controlled loop:

```text
observe
→ hypothesis
→ smallest safe test/change
→ observe result
```

Avoid changing many variables simultaneously.

---

# 21. OPS-INC-010 — One hypothesis-changing action at a time where practical

This preserves causal evidence and prevents compounded damage.

---

# 22. Recent changes

Review:

```text
deployments
feature flags
config
secret rotation
migration/backfill
dependency/provider incidents
capacity changes
```

---

# 23. Dependency diagnosis

Determine whether failure is:

```text
internal
database/cache/broker
provider
network
contract mismatch
capacity
security/config
```

Use dependency metrics as diagnostic evidence.

---

# 24. OPS-INC-011 — Dependency outage and product bug are distinguished

A provider outage may require degradation/retry.

An authorization bug may require immediate feature disable even if dependencies are healthy.

---

# 25. Contract compatibility incident

When API/generated client/realtime/event versions disagree:

```text
identify exact producer/consumer versions
identify contract artifact/version
determine additive/breaking mismatch
restore compatible pair/adapter
```

---

# 26. OPS-INC-012 — Do not patch generated files ad hoc during compatibility incident

Restore compatibility at the producer/contract source or deploy the correct generated consumer.

Keep canonical contract-first discipline during incident.

---

# 27. Old clients

Check:

```text
old mobile versions
cached web assets
old worker version
old queued message
```

before assuming current source versions are the only actors.

---

# 28. Messaging incident

For backlog/retry/poison/order problems diagnose:

```text
producer
event type
consumer
oldest age
growth rate
retry reason
poison identity
ordering key/cursor
dedup state
provider/dependency failure
tenant/RLS failure
```

---

# 29. OPS-INC-013 — Messaging recovery starts from logical identity

Do not replay/purge by broad queue scope before understanding:

```text
message identity
consumer identity
side effect
dedup
ordering
```

---

# 30. Poison message

A poison message can block an ordered stream.

Quarantine/skip requires semantic proof that downstream ordering remains valid.

---

# 31. OPS-INC-014 — Poison recovery is not “delete until green”

Record:

```text
message identity
failure reason
consumer
ordering impact
repair/skip rationale
```

---

# 32. Idempotency incident

If duplicate side effects are suspected, preserve:

```text
idempotency record
logical request
provider correlation
message attempt
target state
```

---

# 33. OPS-INC-015 — Never clear dedup/idempotency state blindly

Before forcing re-execution, prove whether prior business/provider effect happened.

---

# 34. Provider incident

Identify:

```text
provider/Connection
operation
correlation/idempotency key
request outcome
provider status
rate limit
webhook state
unknown result
```

---

# 35. OPS-INC-016 — Timeout is not proof that provider effect failed

If outcome is unknown:

```text
reconcile
lookup
correlate
```

before retrying a duplicate-sensitive external action.

---

# 36. Database/data incident

If corruption suspected:

```text
stop unsafe writer
identify first bad change/window
identify rows/tenants
preserve snapshot/evidence
choose targeted repair vs restore
```

Detailed recovery belongs to `recovery-and-data-safety.md`.

---

# 37. OPS-INC-017 — Database availability is not data recovery

A restored/reachable DB still requires:

```text
schema correctness
RLS
aggregate invariants
outbox/consumer reconciliation
provider side-effect reconciliation
```

---

# 38. Security incident

Security/tenant/confidentiality suspicion escalates even if visible availability impact is low.

Follow security/incident-specific procedures and preserve evidence.

---

# 39. OPS-INC-018 — Security containment can override availability optimization

Examples:

- revoke credential;
- disable public share;
- stop affected integration;
- block vulnerable endpoint.

Do not keep exposure active only to avoid downtime.

---

# 40. Financial/Billing incident

Prevent duplicate charges/entitlements/usage mutation.

Preserve provider event IDs, invoice/payment refs, idempotency records, and affected Account scope.

---

# 41. Capacity incident

Capacity pressure diagnosis includes:

```text
traffic mix
tenant distribution
queue lag
DB saturation
provider limit
retry amplification
new release
```

Do not scale blindly when retry storm/data defect is the real cause.

---

# 42. OPS-INC-019 — Scaling is containment only when it addresses the bottleneck safely

More workers against a rate-limited provider or locked DB can worsen impact.

---

# 43. Recovery options

Canonical categories:

```text
rollback runtime/config
disable/degrade
forward fix
data repair
replay/reconciliation
provider compensation
restore from backup/PITR
```

---

# 44. OPS-INC-020 — Recovery method follows irreversible state

Choose based on what already escaped:

```text
data writes
events
provider effects
mobile clients
schema changes
```

Do not default to binary rollback.

---

# 45. Rollback

Before rollback verify old binary can read:

```text
current schema
current data
current messages
current config
```

---

# 46. OPS-INC-021 — Unsafe rollback is not mitigation

If rollback would corrupt/reject current state, use safe disable/forward recovery.

---

# 47. Forward fix

Forward fix is appropriate after irreversible migration/public events/provider effects or when previous version is incompatible.

---

# 48. Data repair

Targeted repair is preferred over broad restore when blast radius is known and repair semantics are reliable.

---

# 49. Replay

Replay is bounded by stable event/message identity/range.

Verify dedup and external side effects first.

---

# 50. OPS-INC-022 — Replay is controlled reconstruction, not mass resend

Define:

```text
source range
consumer
ordering
idempotency
expected side effects
stop condition
```

---

# 51. Verification

Recovery is not complete when metrics turn green.

Verify:

```text
user-visible behavior
tenant isolation
data invariants
queue/backlog
realtime/projection convergence
provider state
error recurrence
```

---

# 52. OPS-INC-023 — Verify the original failure mode directly

If incident was:

```text
cross-tenant read
```

verify isolation.

If it was:

```text
stuck Automation
```

verify execution/backlog and target side effects.

Generic health check is insufficient.

---

# 53. Data verification

Sample/check affected and neighboring tenants/resources.

Do not assume count parity means semantic correctness.

---

# 54. Message verification

Confirm:

```text
oldest age falling
retry loop stopped
ordering advancing
dedup stable
projection converging
```

---

# 55. Provider verification

Confirm provider-side reality rather than only Notrelix local state after unknown/external effects.

---

# 56. Monitoring recurrence

After mitigation, continue observation over a window appropriate to event frequency/risk.

Exact duration is operational, not hardcoded here.

---

# 57. OPS-INC-024 — Incident closure requires stable recovery evidence

Do not close immediately after one successful request if the failure was intermittent/backlog-based.

---

# 58. Customer impact scope

Track:

```text
which capability
which Accounts/Workspaces
start/end
data affected
workaround
permanent loss/change
```

as safely as possible.

---

# 59. Communication

Communication should distinguish:

```text
known
suspected
unknown
mitigated
recovered
```

Avoid certainty not supported by evidence.

---

# 60. OPS-INC-025 — External/internal communication does not invent root cause early

State current impact and actions before speculative attribution.

---

# 61. Security/privacy communication

Do not disclose another tenant's identity/data in incident updates.

Security incident communication follows applicable policy/legal requirements.

---

# 62. Incident timeline

Timeline captures significant events and decisions.

It is evidence, not a blame document.

---

# 63. OPS-INC-026 — Timeline uses factual observations and decisions

Separate:

```text
fact
hypothesis
action
result
```

where useful.

---

# 64. Root cause

Root cause analysis identifies:

```text
trigger
technical defect
contributing conditions
missing protective controls
why detection/containment failed
```

Avoid simplistic “human error” as sufficient explanation.

---

# 65. OPS-INC-027 — Corrective action targets protective property

Examples:

```text
missing authz gate
→ add canonical policy + regression + architecture proof

message ordering bug
→ fix commit-after-success + reliability test

silent migration drift
→ add migration/gate/runbook
```

Do not only patch one observed row/request.

---

# 66. Contributing factors

May include:

- unclear ownership;
- noisy alert;
- missing negative test;
- unsafe rollout;
- stale docs;
- insufficient capacity;
- vendor behavior.

---

# 67. Action items

Follow-up item has:

```text
owner
protected property
priority
completion evidence
```

Avoid vague:

```text
monitor more
be careful
improve tests
```

---

# 68. OPS-INC-028 — Incident follow-up returns durable knowledge to canonical owners

Examples:

```text
product rule → product/context docs
architecture choice → ADR
quality gap → quality standard/gate
runbook/recovery → operations
```

Incident document itself is not permanent architecture authority.

---

# 69. Incident artifact lifecycle

Incident timeline/report may be retained as historical evidence according to operations/security policy.

Canonical changes are separately updated.

---

# 70. Readiness before launch

A new material capability/failure mode should define before production:

```text
owner
health signals
dependency failure behavior
logs/correlation
critical metrics
alert/runbook
disable/degrade path
migration/recovery path
capacity risk
```

---

# 71. OPS-INC-029 — Recovery path exists before high-risk launch

Do not first design:

- how to stop consumer;
- how to disable provider action;
- how to recover migration;

during the incident when foreseeable.

---

# 72. Game day / recovery exercise

Critical recovery/alert paths can be exercised in controlled non-production/staging conditions where practical.

---

# 73. OPS-INC-030 — Untested recovery assumption is operational risk

Backups, kill switches, alerts, and runbooks should be periodically validated according to risk.

Exact cadence belongs to Operations.

---

# 74. Runbook qualities

A useful runbook contains:

```text
when to use
first principle
diagnose
contain
recover
verify
stop/escalate conditions
```

Vendor-specific commands can be implementation-level.

---

# 75. OPS-INC-031 — Runbook does not authorize destructive shortcut

A runbook must not instruct:

```text
purge queue
delete data
clear dedup
disable RLS
```

without explicit semantic/safety conditions.

---

# 76. Incident handoff

If responders change, transfer:

```text
current impact
timeline
active hypotheses
actions in flight
risks
next checkpoint
```

---

# 77. OPS-INC-032 — Handoff does not restart diagnosis from zero

Maintain a shared incident state.

---

# 78. Incident fatigue

Long incidents may require role rotation.

The coordinator preserves continuity.

---

# 79. False alarm

If alert was false/no action required, still review why it paged if the alert is expected to be actionable.

---

# 80. OPS-INC-033 — False page is observability feedback

Fix noisy/stale alert/runbook instead of accepting recurring false alarms.

---

# 81. Multi-incident handling

If one dependency outage affects many capabilities, coordinate shared root incident while retaining capability-specific impact/owners.

---

# 82. OPS-INC-034 — Shared cause does not erase separate data/security impact

One provider/database incident can have different consequences across Billing, Automation, Integrations, etc.

Verify each affected contract.

---

# 83. Incident command and ownership

Logical capability owners remain authoritative for semantic decisions.

Incident coordinator manages response sequencing.

---

# 84. OPS-INC-035 — Incident urgency does not transfer semantic ownership

Operations can stop/contain unsafe paths.

Permanent business/architecture decisions still return to canonical owners after stabilization.

---

# 85. Database emergency changes

Emergency manual repair, if unavoidable, must be:

- scoped;
- reviewed/authorized;
- logged/audited;
- reproducible afterward;
- reconciled with migrations/source.

---

# 86. OPS-INC-036 — Emergency manual fix creates follow-up source-of-truth repair

Production-only state must not remain unknowable to repository tooling.

---

# 87. Configuration emergency

Temporary config/flag changes must be recorded and later reconciled into canonical config state or reverted.

---

# 88. OPS-INC-037 — Incident config drift is closed after recovery

Do not leave hidden production-only toggles indefinitely.

---

# 89. Contract incident verification

Exercise oldest supported and current consumer against repaired producer where compatibility caused the incident.

---

# 90. OPS-INC-038 — Contract recovery verifies error/auth/tenant semantics too

Do not verify only happy JSON field presence.

---

# 91. Incident data classification

Incident evidence can itself contain sensitive information.

Access/retention follows security/privacy rules.

---

# 92. OPS-INC-039 — Incident evidence is protected

Do not paste secrets/customer content into broad channels merely for debugging.

---

# 93. Incident metrics

Useful response-process measures can include:

```text
time to detect
time to contain
time to recover
recurrence
alert quality
```

if Operations chooses to track them.

Do not optimize metrics at the expense of accurate response.

---

# 94. OPS-INC-040 — Response metrics are learning signals, not blame targets

Avoid incentives to prematurely declare containment/recovery.

---

# 95. Incident readiness checklist

```text
[ ] capability owner
[ ] incident coordination route
[ ] health/SLI
[ ] logs/correlation
[ ] dependencies
[ ] containment/disable
[ ] rollback/forward recovery
[ ] migration/data safety
[ ] message/provider reconciliation
[ ] verification path
[ ] runbook
[ ] safe communications/evidence
```

---

# 96. First-15-actions checklist

Without assigning fixed minutes, early response should usually establish:

```text
[ ] incident coordinator
[ ] start timestamp
[ ] affected capability
[ ] affected tenant/data/security scope
[ ] recent changes
[ ] stop unsafe rollout
[ ] preserve evidence
[ ] dependency/provider state
[ ] containment candidate
[ ] next verification checkpoint
```

---

# 97. Messaging incident checklist

```text
[ ] producer/event
[ ] consumer
[ ] oldest backlog age
[ ] retry reason
[ ] poison identity
[ ] ordering cursor
[ ] dedup/idempotency
[ ] provider side effect
[ ] bounded replay plan
[ ] convergence verification
```

---

# 98. Contract incident checklist

```text
[ ] producer SHA/version
[ ] consumer versions
[ ] contract artifact
[ ] old mobile/web/workers
[ ] additive/breaking mismatch
[ ] compatibility adapter/version
[ ] deploy order
[ ] representative old+new verification
```

---

# 99. Closure checklist

```text
[ ] unsafe propagation stopped
[ ] original failure verified recovered
[ ] tenant/security checked
[ ] data invariants checked
[ ] backlog/projections/provider reconciled
[ ] recurrence monitored
[ ] timeline complete
[ ] root/contributing factors
[ ] follow-up owners/evidence
[ ] canonical docs/gates/runbooks updated as required
```

---

# 100. Stop conditions

Do not declare recovery/closure if:

- dashboard is green because evidence/queue/data was deleted;
- rollback safety was not checked against current schema/data/messages;
- duplicate external effect remains unknown but retry is continuing blindly;
- cross-tenant/security impact is still unassessed;
- database is reachable but application invariants/RLS are unverified;
- ordered stream was skipped with no semantic proof;
- old supported consumer is still incompatible;
- feature remains exposed while suspected security/data corruption continues;
- incident config/manual DB changes are undocumented;
- only one successful request proves an intermittent/backlog problem “fixed”.

---

# 101. Related canonical owners

```text
docs/operations/observability.md
docs/operations/recovery-and-data-safety.md
docs/operations/service-degradation.md
docs/delivery/release-and-rollout.md
docs/delivery/migration-policy.md
docs/quality/security-quality-standard.md
docs/architecture/events-realtime-and-delivery-boundary.md
```

---

# 102. Final incident-readiness rule

During an incident, continuously answer:

```text
What user/tenant/data/security property is at risk?
What evidence proves the current scope?
What must be stopped first to prevent more damage?
Which recent release/config/migration/provider could explain it?
What is the smallest safe containment?
What irreversible effects already escaped?
Is rollback truly safe, or is forward recovery required?
How do we verify the original failure and downstream consistency?
What durable protective control must change afterward?
```

The target is:

> **incident response that contains damage before optimizing appearances, preserves evidence, reasons from logical product/runtime state, recovers through safe rollback or forward repair, and returns every learned invariant to its canonical owner.**
