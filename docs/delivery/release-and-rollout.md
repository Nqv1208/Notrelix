---
document_id: DEL-RELEASE-ROLLOUT
document_type: delivery-policy
status: active
owner: engineering-delivery
applies_to:
  - repository
  - backend
  - frontend
  - mobile
  - workers
  - infrastructure
  - feature-flags
  - releases
evidence:
  - docs/delivery/change-classification.md
  - docs/delivery/contract-first-delivery.md
  - docs/delivery/definition-of-done.md
  - docs/delivery/migration-policy.md
  - docs/quality/engineering-quality-standard.md
  - docs/quality/security-quality-standard.md
  - docs/quality/performance-and-scalability.md
  - .github/workflows/be-ci.yml
  - .github/workflows/fe-ci.yml
review_on:
  - release-model-change
  - rollout-strategy-change
  - feature-flag-policy-change
  - environment-promotion-change
  - rollback-policy-change
  - release-certification-change
  - deployment-topology-change
---

# Release and Rollout

> **A release is a controlled promotion of a classified, evidenced change through valid production states.**
>
> Rollout is successful only when every deployed stage preserves compatibility, data ownership, security, tenant isolation, recoverability, and the product's user-visible contract.

This document owns repository-wide release and rollout policy.

`change-classification.md` decides the delivery obligations.

`contract-first-delivery.md` owns producer/consumer compatibility sequence.

`migration-policy.md` owns schema/data/backfill/cutover mechanics.

`definition-of-done.md` owns completion.

Operations owns concrete production SLOs, alerts, runbooks, and environment-specific incident procedures.

# 1. Release principles

A release should answer:

```text
what exact revision/artifact is being promoted?
which change classes and modifiers apply?
which environments/stages are involved?
which old/new versions can coexist?
which migration stages are active?
which exposure cohort receives the change?
what evidence allows expansion?
what condition stops/rolls back/rolls forward?
what proves rollout completion?
```

# 2. DEL-REL-001 — Every deployed stage is valid on its own

A rollout MUST NOT require an impossible instantaneous switch if production deploys components independently.

Each stage preserves:

```text
schema compatibility
contract compatibility
authorization
tenant isolation
data ownership
failure handling
```

# 3. Preferred staged evolution

For schema/contract/data changes, prefer where applicable:

```text
expand
→ deploy compatible producer/reader
→ migrate/backfill
→ switch behavior/authority
→ observe
→ contract/remove old path
```

This is a pattern, not a mandatory number of deployments.

# 4. DEL-REL-002 — Compatibility is checked across actual deployment units

Review combinations across:

```text
database
backend API
background workers
web
mobile
realtime consumers
provider callbacks
external integrations
```

One repository does not imply one runtime version.

# 5. Release unit

A release unit is an immutable revision/artifact or explicitly versioned set of artifacts.

Examples:

```text
backend image for SHA
web build for SHA
mobile release build/version
worker image for SHA
migration package/script for SHA
```

# 6. DEL-REL-003 — Release evidence refers to exact revision/artifact

Do not certify:

```text
branch name
latest main
PR number
"same code basically"
```

instead of the exact built/tested revision.

# 7. Artifact immutability

Promote the same tested artifact when the deployment platform permits it.

Do not rebuild materially different production binaries from the same source label without equivalent provenance.

# 8. DEL-REL-004 — Promotion does not silently change the artifact

Environment-specific configuration may vary by contract.

Compiled/application artifact should remain traceable to the tested source revision.

# 9. Build once versus rebuild

Where build-on-promotion is unavoidable, provenance must still establish:

- same source revision;
- locked dependencies;
- deterministic generator inputs;
- equivalent build configuration.

# 10. Release readiness

Before exposure:

```text
required CI green
contract/generated drift green
migration prerequisites satisfied
config validated
security concerns addressed
rollout/rollback decision stated
observability available
```

# 11. DEL-REL-005 — Green CI is necessary but not sufficient for material rollout

CI proves repository/build/test properties.

It does not automatically prove:

- production data migration complete;
- provider state reconciled;
- cohort health;
- old consumer floor reached.

# 12. Environment promotion

Typical environment chain may include:

```text
local/test
CI
staging/preproduction
production
```

Exact environments are infrastructure-owned.

The policy requirement is that material production-specific assumptions are tested at the closest realistic stage.

# 13. DEL-REL-006 — Staging is evidence only when it reproduces the property

A staging environment that differs from production in database/provider/security topology cannot be cited as proof for those omitted properties.

# 14. Deployment order

Deployment order follows compatibility, not organizational team order.

# 15. DEL-REL-007 — Producer-first and consumer-first are deliberate choices

Examples:

```text
additive optional API field
→ producer-first may be safe

remove old event field
→ consumers/dual support first

schema contraction
→ old readers/writers first removed
```

# 16. Rolling deployment

When old/new instances coexist, both must remain compatible with shared:

```text
database
messages
cache
config
public contract
```

for the overlap window.

# 17. DEL-REL-008 — Rolling deployment has mixed-instance compatibility

Do not introduce one process version that writes state another still-serving version cannot read unless routing/isolation proves they never overlap.

# 18. Database rollout

Database stages follow `migration-policy.md`.

Release policy decides when migration stage is promoted and when app versions switch.

# 19. DEL-REL-009 — Database contraction follows reader/writer migration

Drop/rename/removal happens only after evidence that old deployed code no longer requires the old representation.

# 20. Workers

Workers can lag API deployment and consume old backlog.

# 21. DEL-REL-010 — Worker rollout accounts for queued old work

Before deploying incompatible worker:

```text
old backlog is supported
or drained
or version-routed
```

New producers must not emit unsupported work to old consumers.

# 22. Mobile

Mobile distribution is asynchronous and may lag by days/weeks according to support policy.

# 23. DEL-REL-011 — Mobile rollout assumes supported old clients remain active

Backend/realtime contracts remain compatible with the supported mobile version floor until explicit retirement.

# 24. Web

Web usually updates faster than mobile but still overlaps rolling backend deployments and cached browser sessions.

# 25. DEL-REL-012 — Web deployment does not assume instant browser refresh

Old loaded web bundles can continue making requests after a new deployment.

Server contracts must tolerate the intended overlap.

# 26. Feature flags

Feature flags are temporary rollout controls, not permanent architecture.

# 27. DEL-REL-013 — Feature flag declares lifecycle

Each rollout flag declares:

```text
owner
purpose
eligible scope
safe default
exposure strategy
decision/health signals
security parity
removal condition
```

# 28. Flag types

Possible classes:

```text
release flag
experiment flag
operational kill switch
entitlement-backed product capability
```

Do not confuse them.

A Billing Entitlement is not a temporary release flag.

# 29. DEL-REL-014 — Release flag and product entitlement are different authorities

Release flag controls deployment exposure.

Entitlement controls commercial product availability.

Governance controls user/resource authorization.

# 30. Exposure units

Gradual exposure may use approved units such as:

```text
internal/test Account
specific Account/Workspace
explicit allowlist cohort
stable percentage bucket
environment
```

Use the smallest stable scope that fits the rollout.

# 31. DEL-REL-015 — Cohort assignment is stable

A user/resource should not randomly oscillate between old/new paths unless the rollout design explicitly supports that.

Stable hashing or explicit assignment is preferred for percentage cohorts.

# 32. Account versus User cohort

Choose cohort boundary based on state sharing.

For Workspace-scoped/persisted behavior, a per-user flag may be unsafe if two users mutate shared data through incompatible paths.

# 33. DEL-REL-016 — Cohort boundary follows shared-state compatibility

Do not split one shared resource across incompatible data semantics merely for a convenient percentage rollout.

# 34. Flagged persistence

Both flag paths must write/read compatible shared data or explicitly isolate/migrate the data.

# 35. DEL-REL-017 — Feature flag cannot create hidden dual authority

If old/new paths both write the same semantic fact, declare:

```text
one authority
compatibility
migration
cutover
```

A flag is not permission to maintain two competing models.

# 36. Flagged cache/realtime

Cache keys, query state, realtime messages, and generated contracts must remain valid when a user/resource moves between cohorts.

# 37. DEL-REL-018 — Cohort change is a supported state transition

A promoted/demoted user should not inherit invalid:

- cache;
- local client state;
- persisted config;
- realtime subscription;
- old-format data.

# 38. Security parity

Security and tenant guarantees on experimental/new path are identical or stricter than old path.

# 39. DEL-REL-019 — Rollout percentage never weakens authorization

“Only 1% exposed” is not a compensating control for an auth/tenant flaw.

# 40. Kill switch

A kill switch may disable a risky optional capability quickly.

It must define safe disabled behavior.

# 41. DEL-REL-020 — Kill switch is tested before relying on it

A flag that is intended as emergency control needs evidence that disabling it actually stops the risky path without corrupting shared state.

# 42. Flag removal

Remove release flag after rollout decision/completion.

# 43. DEL-REL-021 — Successful rollout includes flag cleanup

Completion proof should show:

```text
target cohort = intended final exposure
old path unused
data compatible/migrated
rollback window understood
flag references removable
```

# 44. Canary

A canary is a limited production exposure used to observe real behavior before wider rollout.

# 45. DEL-REL-022 — Canary has explicit expansion and abort criteria

Signals can include:

```text
error/failure rate
latency
queue lag
provider errors
data invariant checks
security signal
user-visible success
```

Exact thresholds belong to Operations/release plan.

# 46. Canary scope

Use cohort/routing that is representative enough to reveal the risk without accidentally crossing tenant/state boundaries.

# 47. Shadow mode

Shadow computation can compare new/old results without changing authoritative output where safe.

# 48. DEL-REL-023 — Shadow path has no unintended side effects

A shadow implementation MUST NOT:

- send provider effects;
- create duplicate events;
- charge Billing usage;
- mutate source state

unless explicitly designed as part of migration.

# 49. Dual run

If old/new processors run simultaneously, idempotency and single authority are mandatory.

# 50. Observability

Material rollout requires signals aligned to failure modes.

Examples:

```text
request/error
migration progress
consumer lag
dead-letter
provider unknown outcome
cache/realtime divergence
entitlement errors
security deny anomalies
```

# 51. DEL-REL-024 — Rollout telemetry uses logical operation/cohort identity

Operators should be able to compare old/new paths/cohorts without leaking sensitive data.

# 52. Health signals

A process health check only proves process-level readiness/liveness.

It may not prove product path correctness.

# 53. DEL-REL-025 — Health check does not replace functional smoke evidence

For a high-risk release, validate the relevant composed workflow/property after deployment.

# 54. Smoke testing

Post-deploy smoke should be:

- bounded;
- tenant-safe;
- non-destructive or purpose-built test data;
- representative of the changed property.

# 55. DEL-REL-026 — Smoke test cannot mutate real customer state casually

Use controlled test Account/Workspace/data or explicitly safe read checks.

# 56. Rollback

Rollback means returning runtime/artifact/config to an earlier compatible state.

It is not guaranteed for every change.

# 57. DEL-REL-027 — Rollback is analyzed per surface

State separately:

```text
binary rollback
schema rollback
data rollback
config rollback
message/event rollback
provider-effect rollback
mobile rollback
```

# 58. Binary rollback

Binary rollback is safe only if prior binary still understands current schema/data/messages/config.

# 59. DEL-REL-028 — A green previous binary is not automatically rollback-safe

New writes/migrations may make old code incompatible after rollout begins.

# 60. Schema rollback

Some additive schema changes can remain in place while binary rolls back.

Destructive migrations may not be reversible.

# 61. Data rollback

Backfilled/transformed data may be difficult/impossible to reconstruct to old semantics.

Plan forward recovery when appropriate.

# 62. Event rollback

Events already published cannot be “unpublished”.

Old consumers may have acted.

# 63. DEL-REL-029 — Published facts require forward/compensating recovery

Do not promise rollback for already-delivered external/integration events as if they were local memory.

# 64. Provider rollback

External provider actions may be irreversible or only compensatable.

Examples:

- email sent;
- external issue created;
- payment;
- third-party deletion.

# 65. DEL-REL-030 — External side effect rollback is operation-specific

State whether it can be:

```text
reversed
compensated
reconciled
left as accepted external history
```

# 66. Mobile rollback

Released mobile binaries cannot be instantaneously recalled from every device.

Server compatibility remains the main recovery lever.

# 67. Forward recovery

Forward recovery means deploying a correction/migration/compensation that restores valid state without reverting every artifact.

# 68. DEL-REL-031 — Forward recovery is first-class

Choose it explicitly when:

- schema contraction occurred;
- data transformed irreversibly;
- provider effect escaped;
- public event consumed;
- mobile contract already shipped.

# 69. Rollback trigger

A release plan defines which observed failures justify halt/rollback/forward fix.

Do not improvise under incident pressure where foreseeable.

# 70. DEL-REL-032 — Rollback decision protects data/security before availability convenience

If rollback would reintroduce data corruption/security weakness, prefer a safe disabled mode or forward repair.

# 71. Release pause

Rollout can pause at a valid stage while investigation occurs.

A paused stage remains compatible and operationally supportable.

# 72. DEL-REL-033 — Pause does not mean half-applied invalid migration

Each stage must have a clear stable boundary/checkpoint.

# 73. Configuration rollout

Config changes are deployed contracts.

Use typed/validated config and environment inventory.

# 74. DEL-REL-034 — Config default change is rollout-impacting

Review what happens to instances/environments that omit the new key.

Fail-safe defaults are required for security-sensitive config.

# 75. Secret rotation

Secret rotation may require overlapping validity/new-old credential sequencing.

Do not rotate producer/consumer credential halves incoherently.

# 76. DEL-REL-035 — Credential rotation has overlap/revocation sequence

Plan:

```text
introduce new
→ verify consumers
→ switch
→ revoke old
```

where the provider/security mechanism permits it.

# 77. Dependency/runtime release

Runtime/framework changes can alter startup, serialization, TLS/provider behavior, image size, or platform compatibility.

Rollout with representative evidence.

# 78. Scheduled rollout

Time-based rollout must avoid uncontrolled catch-up/cron duplication across old/new workers.

# 79. DEL-REL-036 — Scheduler ownership prevents duplicate occurrence during rollout

If two versions can schedule the same logical job, idempotency/leader/claim semantics must preserve one logical occurrence.

# 80. Release notes

Material user/admin-visible changes should have product/release communication where needed.

This document does not mandate public notes for every internal release.

# 81. Release certification

Certification ties:

```text
exact revision/artifact
required CI evidence
migration stage
contract compatibility
release plan
post-deploy validation
```

# 82. DEL-REL-037 — Release certification is revision-specific

A release is not certified by historical green CI for another SHA.

# 83. CI evidence

Current CI may produce final backend/frontend gate success for exact SHA.

Deployment pipeline can consume that evidence.

# 84. DEL-REL-038 — Release does not bypass required gates by manual deployment

Emergency procedure, if any, belongs to incident/operations governance and requires explicit follow-up evidence.

# 85. Post-deploy verification

Verify affected properties, not generic homepage uptime only.

Examples:

```text
migration state
API contract
background processing
provider callback
realtime
frontend critical flow
```

# 86. DEL-REL-039 — Expansion waits for relevant health evidence

A time delay alone is not rollout proof if the changed path received no meaningful traffic.

# 87. Low-traffic capability

For low traffic, use controlled synthetic/test Account activity or a longer evidence window rather than assuming no errors means healthy.

# 88. Monitoring window

Exact duration is release-plan/Operations-specific.

It follows event frequency and risk.

# 89. Release failure classes

Examples:

```text
binary/startup
schema/data
compatibility
security/tenant
performance/capacity
provider
message/backlog
client/realtime
```

Recovery choice follows class.

# 90. DEL-REL-040 — Recovery targets root failure surface

Do not rollback frontend for a broken DB migration if that cannot repair the database.

Do not revert schema if a safe feature disable resolves an optional UI defect.

# 91. Incident escalation

Material security/data-integrity incident during rollout halts expansion and follows incident management.

# 92. Cleanup

After successful rollout:

```text
remove temporary flags
remove compatibility adapter
remove old config
remove migration-only code
update docs/current evidence
```

only when removal proof exists.

# 93. DEL-REL-041 — Cleanup does not erase useful historical decision evidence

Remove runtime/dead transitional code.

Keep accepted ADR/history and required migration/audit evidence according to policy.

# 94. Release plan template

A material release plan should contain:

```text
change classes/modifiers
artifacts/revisions
prerequisites
deployment stages
mixed-version matrix
migration stage
exposure/cohort
health/expansion criteria
abort criteria
rollback/forward recovery
post-deploy proof
cleanup criteria
owner
```

# 95. Feature flag checklist

```text
[ ] owner
[ ] purpose
[ ] flag type
[ ] scope/cohort
[ ] stable assignment
[ ] safe default
[ ] security parity
[ ] persisted/cache/realtime compatibility
[ ] health/decision signal
[ ] kill-switch behavior if claimed
[ ] final exposure
[ ] removal condition
```

# 96. Rollback checklist

```text
[ ] previous binary reads current schema/data?
[ ] DB rollback safe?
[ ] transformed data reversible?
[ ] old worker reads current messages?
[ ] provider effects reversible?
[ ] mobile already shipped?
[ ] config/secret rollback safe?
[ ] forward-recovery alternative?
```

# 97. Release certification checklist

```text
[ ] exact artifacts/revisions
[ ] required CI green
[ ] contract/generated synchronized
[ ] migration prerequisite complete
[ ] config validated
[ ] rollout plan
[ ] observability
[ ] post-deploy smoke/health
[ ] expansion/abort criteria
[ ] cleanup owner
```

# 98. Stop conditions

Stop rollout if:

- a deployed stage requires incompatible old/new code with no isolation;
- schema contraction precedes reader/writer migration;
- supported old mobile client is broken;
- feature flag paths have different tenant/authz guarantees;
- cohort crossing corrupts cache/persisted state;
- canary has no measurable expansion/abort criteria;
- old/new shadow or dual-run creates duplicate external effects;
- rollback is claimed but old binary cannot read current data;
- provider/public events already escaped and plan still assumes full rollback;
- required config is missing/ambiguous;
- exact release artifact has not passed required evidence;
- migration stage has no stable checkpoint;
- post-deploy expansion is based only on “no alert” with no changed-path traffic.

# 99. Related canonical owners

```text
docs/delivery/change-classification.md
docs/delivery/contract-first-delivery.md
docs/delivery/definition-of-done.md
docs/delivery/migration-policy.md
docs/quality/engineering-quality-standard.md
docs/quality/security-quality-standard.md
docs/quality/performance-and-scalability.md
docs/operations/
```

# 100. Final release rule

Before expanding a release, answer:

```text
Which exact artifact/revision is live?
Which old/new versions coexist?
Is the current DB/data/message contract compatible?
Who is exposed and how stable is the cohort?
What signals prove the changed path is healthy?
What condition pauses/aborts expansion?
What can really be rolled back?
What requires forward recovery?
What migration/cleanup stage remains?
What objective evidence permits the next stage?
```

The target is:

> **release as a sequence of valid, observable, reversible-or-recoverable production states—not an assumption that merging, deploying, and hoping are one atomic operation.**
