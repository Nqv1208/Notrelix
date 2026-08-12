---
document_id: TEMPLATE-INCIDENT-REPORT
document_type: template
status: active
owner: documentation-governance
applies_to:
  - incidents
  - production
  - security
  - data-integrity
  - availability
  - provider-failures
  - release-incidents
evidence:
  - docs/operations/incident-readiness.md
  - docs/operations/observability.md
  - docs/operations/recovery-and-data-safety.md
  - docs/operations/service-degradation.md
  - docs/delivery/release-and-rollout.md
  - docs/governance/decision-and-exception-policy.md
review_on:
  - incident-process-change
  - post-incident-review-change
  - severity-model-change
  - template-change
---

# Incident Report Template

> **An incident report preserves what happened, what impact occurred, how responders reasoned and recovered, which protective controls failed, and what durable corrections are required.**
>
> It is a factual operational history. It is not a blame document, a place to rewrite uncertain hypotheses as facts, or a permanent architecture handbook.

Use this template after or during a material production incident.

The canonical response policy remains:

```text
docs/operations/incident-readiness.md
```

This template records one concrete incident.

---

# 1. Core principles

A good incident report separates:

```text
fact
hypothesis
decision
action
result
```

and distinguishes:

```text
trigger
technical root cause
contributing factors
missing protective controls
```

It records user/tenant/data/security impact explicitly.

---

# 2. Incident report lifecycle

During active incident:

```text
summary
scope
timeline
containment
current hypotheses
recovery actions
```

may be updated continuously.

After recovery:

```text
root cause
control gaps
lessons
corrective actions
canonical knowledge updates
```

are completed.

Do not block urgent response because every post-incident section is not yet written.

---

# 3. Incident report versus canonical docs

Incident report preserves history.

If an incident reveals a durable rule:

```text
product rule
→ product/context docs

architecture choice
→ ADR + architecture docs

quality invariant
→ quality docs/gates

operations procedure
→ operations/runbook

migration requirement
→ migration plan/policy
```

Do not leave the durable fix documented only inside the incident report.

---

# 4. Incident report versus security/legal process

This template records engineering/operational incident facts.

Security/privacy/legal notifications, evidence handling, or regulatory obligations may require additional restricted processes.

Do not place sensitive investigative material into a broadly accessible report just because this template has a section for evidence.

---

# 5. Copy from here

```markdown
---
document_id: <INCIDENT-ID>
document_type: incident-report
status: active
owner: operations
applies_to:
  - <affected capability>
evidence:
  - <incident telemetry/runbook/release references>
review_on:
  - incident-recovered
  - root-cause-confirmed
  - corrective-actions-complete
---

# <INCIDENT-ID>: <Concise incident title>

## 1. Incident summary

### Status

`<Investigating | Contained | Recovering | Monitoring | Resolved>`

These are report workflow labels, not global governance document statuses.

### Start time

`YYYY-MM-DD HH:MM <timezone>`

### Detection time

`YYYY-MM-DD HH:MM <timezone>`

### Containment time

`YYYY-MM-DD HH:MM <timezone or Unknown>`

### Recovery time

`YYYY-MM-DD HH:MM <timezone or In progress>`

### End / resolved time

`YYYY-MM-DD HH:MM <timezone or In progress>`

### Incident coordinator

`<logical/person assignment for this incident>`

### Affected capabilities

- ...

### Impact summary

<One factual paragraph describing user-visible impact.>

### Tenant/account scope

- known affected:
- potentially affected:
- confirmed unaffected:
- unknown:

### Data integrity impact

`<None known / Suspected / Confirmed>`

Details:

- ...

### Confidentiality / security impact

`<None known / Suspected / Confirmed>`

Details:

- ...

### Financial/commercial impact

`<None known / Suspected / Confirmed>`

Details:

- ...

### Current user-visible behavior

- ...

### Permanent data loss

`<None known / Unknown / Confirmed>`

If confirmed:

- interval:
- affected resources:
- repair/communication status:

---

## 2. Severity assessment

Do not invent a numeric SEV taxonomy if the organization has not approved one.

Assess these dimensions:

| Dimension | Assessment | Evidence |
|---|---|---|
| availability | ... | ... |
| data integrity | ... | ... |
| confidentiality | ... | ... |
| tenant isolation | ... | ... |
| financial impact | ... | ... |
| affected scope | ... | ... |
| propagation risk | ... | ... |
| workaround | ... | ... |
| duration | ... | ... |

### Operational severity label

`<Use approved severity model, or "Not formally classified">`

### Escalation reason

- ...

---

## 3. Detection

### Detection source

- alert:
- customer report:
- operator:
- automated gate:
- provider notification:
- security signal:
- other:

### First signal

- ...

### First observable symptom

- ...

### What actually indicated user impact

- ...

### Detection delay

If impact began before detection:

- estimated delay:
- why not detected earlier:

### Detection quality

What worked:

- ...

What did not:

- ...

---

## 4. Incident roles and communication

### Roles

| Responsibility | Assignment |
|---|---|
| incident coordination | ... |
| technical investigation | ... |
| communications | ... |
| timeline/scribe | ... |
| security/privacy if applicable | ... |
| provider/vendor coordination | ... |

### Shared incident channel / location

- ...

### External/customer communication

- not required / required:
- status:
- factual messages sent:

Do not include customer-private data from other tenants.

---

## 5. Timeline

Use absolute timestamps with timezone.

| Time | Type | Observation / decision / action | Owner | Result |
|---|---|---|---|---|
| ... | fact | ... | ... | ... |
| ... | hypothesis | ... | ... | ... |
| ... | decision | ... | ... | ... |
| ... | action | ... | ... | ... |
| ... | verification | ... | ... | ... |

Include significant:

```text
detection
declaration
scope change
rollout pause
feature/config change
consumer stop
hypothesis
data/provider evidence
rollback/forward fix
replay/repair
verification
reopen
resolution
```

Do not reconstruct false precision after the fact.

If timestamp is approximate, mark it.

---

## 6. Recent changes / candidate triggers

### Releases

| Revision/artifact | Deploy time | Scope/cohort | Relevance |
|---|---|---|---|
| ... | ... | ... | ... |

### Configuration / feature flags

- ...

### Migrations / backfills

- ...

### Dependency/provider incidents

- ...

### Capacity/workload changes

- ...

### Secret/credential rotations

- ...

Do not assume the latest deployment caused the incident merely because it was recent.

---

## 7. Blast radius

### Affected resources

- contexts:
- services/processes:
- Accounts/Workspaces:
- resource types:
- providers:
- regions/environments:

### Data window

- first bad write/event:
- last bad write/event:
- uncertainty:

### Async scope

- outbox:
- queues:
- DLQ:
- consumer:
- backlog:
- ordering:

### Client scope

- web versions:
- mobile versions:
- old loaded bundles:
- external consumers:

### External state

- provider objects/effects:
- object storage:
- payments/emails/calendar/etc.:

---

## 8. Containment

### Containment objective

<What additional damage was being stopped?>

### Actions

| Action | Reason | Time | Expected safety property | Result |
|---|---|---|---|---|
| pause rollout | ... | ... | ... | ... |
| disable flag | ... | ... | ... | ... |
| stop consumer | ... | ... | ... | ... |
| read-only | ... | ... | ... | ... |
| throttle | ... | ... | ... | ... |

### Why containment was safe

- tenant/security:
- data:
- compatibility:
- external side effects:

### Known residual risk after containment

- ...

### Containment actions explicitly avoided

Examples:

```text
queue purge
dedup deletion
RLS disable
blind DB restore
provider blind retry
```

Reason:

- ...

---

## 9. Investigation

### Initial hypothesis

- ...

### Evidence supporting/refuting it

- ...

### Hypothesis log

| Hypothesis | Evidence for | Evidence against | Action/test | Outcome |
|---|---|---|---|---|
| ... | ... | ... | ... | ... |

### Correlation identifiers

- release SHA:
- operation IDs:
- event/message IDs:
- provider IDs:
- migration job ID:
- safe tenant/resource IDs:

### Dependencies

- DB:
- Redis/cache:
- broker:
- object storage:
- provider:
- realtime:
- frontend delivery:

### Security/tenant analysis

- ...

### Data-integrity analysis

- ...

---

## 10. Root cause

Complete only when evidence supports it.

### Trigger

<The event/change that initiated the failure.>

### Technical root cause

<The defect/failure mechanism that produced impact.>

### Why the system allowed it

<Missing/weak protective property.>

Examples:

```text
missing authorization gate
migration allowed old writer to create invalid rows
consumer advanced ordering cursor before handler success
retry lacked idempotency
release allowed incompatible old mobile consumer
alert observed dependency, not user impact
```

### Root-cause confidence

`<Confirmed / High confidence / Still under investigation>`

### Evidence

- ...

Do not write “human error” as the final technical root cause.

---

## 11. Contributing factors

Separate from root cause.

Examples:

- test fixture hid multi-tenant behavior;
- noisy alert delayed detection;
- stale documentation;
- rollout too broad;
- provider rate limit;
- missing recovery exercise;
- high-cardinality tenant;
- old mobile client;
- ambiguous ownership.

### Factor C-01

- description:
- contribution:
- evidence:

### Factor C-02

- ...

---

## 12. Why protective controls did not prevent/limit it

### Product invariant

- expected:
- missing/failed:

### Architecture boundary

- expected:
- missing/failed:

### Authorization / tenant isolation

- ...

### Validation / concurrency

- ...

### Idempotency / ordering

- ...

### Migration / compatibility

- ...

### Test coverage / quality gate

- ...

### CI

- ...

### Release/canary

- ...

### Observability / alerting

- ...

### Runbook / recovery readiness

- ...

For each gap, identify whether the control:

```text
did not exist
existed but did not cover this case
existed but was disabled
existed but did not execute
alerted but was ignored/noisy
```

---

## 13. Recovery

### Recovery strategy

`<rollback / forward fix / data repair / replay / provider reconciliation / restore / combination>`

### Why this strategy was chosen

- ...

### Binary rollback

- safe?:
- used?:
- evidence:

### Schema/data rollback

- safe?:
- used?:
- evidence:

### Forward fix

- ...

### Data repair

- affected rows/resources:
- method:
- idempotency:
- tenant safeguards:
- audit:

### Replay

- bounded identity/range:
- consumer:
- dedup:
- ordering:
- external effects:

### Provider reconciliation

- provider IDs:
- unknown outcomes:
- compensation:
- final provider reality:

### Cache/search/projection

- invalidated/rebuilt:
- verification:

### Client/realtime convergence

- ...

---

## 14. Recovery verification

Recovery is not proven by a green process/dashboard alone.

### Original failure scenario

- reproduction/verification:
- result:

### User-visible flow

- ...

### Tenant isolation

- wrong-tenant test/query:
- result:

### Data invariants

- ...

### RLS

- ...

### Outbox / queue / backlog

- oldest age:
- retry:
- poison:
- ordering:

### Provider state

- ...

### Realtime/projection

- ...

### Client compatibility

- current:
- oldest supported:

### Recurrence monitoring

- signal:
- observation window:
- result:

Do not invent a universal monitoring duration.

---

## 15. Permanent loss / manual correction

### Permanent data loss

`<None / Confirmed / Unknown>`

### Permanently inconsistent external effects

- ...

### Manual repairs

| Resource/range | Repair | Evidence | Audit/reference |
|---|---|---|---|
| ... | ... | ... | ... |

### Customer-visible correction

- ...

Do not silently normalize lost rows/counts.

---

## 16. User / customer impact

### What users experienced

- ...

### What users did not experience

- ...

### Workarounds

- ...

### Communication

- ...

### Follow-up communication required

- ...

Keep statements factual and tenant-safe.

---

## 17. What went well

Focus on protective controls/processes that materially reduced impact.

- ...

Examples:

```text
canary limited blast radius
outbox preserved committed work
RLS prevented cross-tenant access
provider idempotency prevented duplicate effect
alert identified backlog age quickly
```

Avoid generic praise.

---

## 18. What made the incident harder

- ...

Examples:

```text
no correlation across provider
missing old-client version telemetry
stale runbook
migration had no progress metric
false pages obscured real alert
```

---

## 19. Detection and observability gaps

### SLI gap

- ...

### Alert gap

- ...

### Logging/correlation gap

- ...

### Dashboard/query gap

- ...

### Backlog/freshness gap

- ...

### Release/cohort attribution gap

- ...

### Sensitive-data concern

- ...

Each proposed observability change should answer a real future operational question.

---

## 20. Test / gate gaps

For every missing regression/protective proof:

| Gap | Protected property | Correct test/gate layer | Required regression |
|---|---|---|---|
| ... | ... | ... | ... |

Do not answer every incident by adding an E2E test.

Use the cheapest reliable proof for the failed property.

---

## 21. Documentation / ownership gaps

### Canonical doc missing/stale

- ...

### Ownership ambiguity

- ...

### Runbook gap

- ...

### ADR gap

- ...

### Generated evidence drift

- ...

Do not create a duplicate permanent handbook inside the incident report.

---

## 22. Corrective actions

Every action requires:

```text
protected property
logical owner
priority
acceptance proof
```

### P0/P1-style labels

Use only an approved priority scheme if one exists.

Otherwise use explicit impact/ordering rather than inventing organization-wide priority terminology.

| ID | Action | Protected property | Owner | Completion evidence | Status |
|---|---|---|---|---|---|
| ACT-01 | ... | ... | ... | ... | open |
| ACT-02 | ... | ... | ... | ... | open |

Bad action:

```text
Be more careful.
Monitor more.
Add more tests.
```

Good action:

```text
Add Architecture test rejecting Application handlers that bypass pipeline authorization;
the gate must execute in required Backend CI and fail on a deliberate fixture violation.
```

---

## 23. Corrective-action categories

Classify each action if useful:

```text
product semantics
architecture
security
quality/test
migration
release
observability
runbook/recovery
infrastructure
provider
documentation
```

This helps route durable knowledge.

---

## 24. Canonical knowledge updates

### Product/context docs

- ...

### Architecture docs

- ...

### ADR

- ...

### Quality standards/gates

- ...

### Delivery policy/template

- ...

### Operations/runbook

- ...

### Infrastructure

- ...

### Generated docs/evidence

- ...

### No canonical update required

If none, explain why the incident was fully covered by existing rules and only implementation violated them.

---

## 25. Evidence references

Link safe/relevant:

```text
CI runs
release SHA
dashboards
logs/query snapshots
migration records
provider incidents
PRs
ADRs
tests
runbooks
```

Do not include raw secrets/private customer payloads.

Restricted evidence can be referenced through an approved restricted location rather than copied.

---

## 26. Decision log

Record consequential response decisions that future reviewers may question.

| Time | Decision | Alternatives | Reason | Owner |
|---|---|---|---|---|
| ... | stop consumer | purge queue / continue | preserve ordering/data | ... |
| ... | forward fix | rollback | old binary incompatible with schema | ... |

This is incident history.

If a decision creates durable architecture, create/update the appropriate ADR separately.

---

## 27. Incident closure criteria

The incident can be marked Resolved when:

```text
[ ] unsafe propagation stopped
[ ] original user-visible failure recovered
[ ] tenant/security scope verified
[ ] data invariants verified
[ ] backlog/projections reconciled
[ ] provider/external effects reconciled
[ ] recovery stable over appropriate observation
[ ] permanent loss identified
[ ] required customer/status communication done
[ ] corrective actions owned
```

Corrective actions do not all need to be completed before incident resolution, but they must be owned and tracked.

---

## 28. Post-incident completion criteria

The report is complete when:

```text
[ ] timeline factual
[ ] root cause confidence stated
[ ] trigger separated from root cause
[ ] contributing factors recorded
[ ] control gaps identified
[ ] recovery verification recorded
[ ] permanent loss/manual repairs recorded
[ ] corrective actions have owner + acceptance proof
[ ] durable knowledge rehomed/linked
[ ] sensitive evidence handled safely
```

---

## 29. Stop conditions

Do not finalize the report if:

- root cause is still presented as fact without evidence;
- severity is based only on number of visible users;
- cross-tenant/security impact is unassessed;
- evidence was deleted/purged without being recorded;
- “rollback” is claimed without checking schema/data/provider compatibility;
- data repair was performed with no audit/evidence;
- provider unknown outcome remains unresolved but retry is called successful;
- recovery verification only says “dashboard green”;
- corrective actions are vague and ownerless;
- the report attempts to redefine architecture without ADR/current-doc update;
- sensitive customer/security evidence is pasted into broad-access text unnecessarily.

---

## 30. Final summary

### What happened

...

### Why it happened

...

### What stopped it

...

### How correctness was verified

...

### What will prevent or reduce recurrence

...

### Remaining risk

...

### Canonical updates

...
```

---

# 6. Timeline quality

The legacy incident template correctly required timestamps for:

```text
detection
containment
hypotheses
mitigations
recovery
verification
```

The expanded template additionally tags whether each row is a:

```text
fact
hypothesis
decision
action
verification
```

to prevent hindsight from rewriting uncertain observations as facts.

---

# 7. Root-cause quality

Use this structure:

```text
Trigger
→ event/change that activated the problem

Technical root cause
→ defect/failure mechanism

Why system allowed it
→ missing/weak protective control

Contributing factors
→ conditions that increased likelihood/impact
```

Example:

```text
Trigger:
new release enabled a changed ordering path

Technical root cause:
consumer advanced cursor before handler success

Why allowed:
critical ordering behavior lacked regression/architecture proof

Contributing:
retry load made the stall appear intermittent
```

Avoid:

```text
Root cause: developer mistake.
```

That does not describe the failed system property.

---

# 8. Impact quality

Report separately:

```text
availability
data integrity
confidentiality
tenant isolation
financial/commercial impact
```

A security or corruption incident can be severe with low visible traffic.

Do not rank impact by page-view count only.

---

# 9. Recovery quality

The legacy template explicitly required:

```text
rollback / forward fix / data repair
+
verification of correctness and tenant safety
```

The expanded report must also capture:

```text
message/dedup/order
provider reality
object/projection/cache reconciliation
old client compatibility
```

when affected.

A DB or process returning healthy is not sufficient.

---

# 10. Corrective-action quality

Each action should make a future failure class harder or faster to detect/recover.

A useful action specifies:

```text
what protective property?
where should the control live?
what evidence proves completion?
```

If the action only says:

```text
document this
```

ask whether the real missing control is code, test, architecture, alert, runbook, or ownership.

---

# 11. Blameless does not mean vague

Do not assign moral blame.

Do identify:

```text
which implementation was wrong
which review/gate was missing
which decision was ambiguous
which process allowed unsafe change
```

Precision is required for learning.

---

# 12. Incident-report access

Some incident reports may need restricted access because they contain:

```text
security indicators
customer data
provider account information
credential exposure
legal/privacy details
```

The engineering template does not override access-control policy.

Use a redacted broad report plus restricted evidence reference if necessary.

---

# 13. Post-incident knowledge migration

The report is historical evidence.

After corrective work:

```text
new durable invariant
→ canonical owner

new architecture choice
→ ADR

new test requirement
→ quality/testing + executable gate

new recovery procedure
→ operations

new deployment requirement
→ infrastructure/delivery
```

Do not make responders reread old incidents to learn current architecture.

---

# 14. Incident-report quality test

A reader who did not participate should be able to answer:

```text
What user/tenant/data/security impact happened?
When did it start, get detected, contained, recover, and end?
What facts and hypotheses existed during response?
What was the technical root cause?
Why did existing controls not prevent/limit it?
What irreversible effects escaped?
How was recovery performed?
How was the original failure directly verified?
What permanent loss/manual repairs occurred?
Which concrete corrective actions exist and how will they be proven?
Which canonical docs/gates/runbooks now carry the durable learning?
```

If the report cannot answer those questions, it is not complete.
