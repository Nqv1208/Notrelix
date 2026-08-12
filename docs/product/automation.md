---
document_id: PROD-AUTOMATION
document_type: product-context
status: active
owner: automation
applies_to:
  - automation
  - rules
  - triggers
  - conditions
  - actions
  - executions
  - scheduling
  - automation-templates
evidence:
  - PRODUCT.md
  - docs/product/product-model.md
  - docs/product/product-experience.md
  - docs/product/contexts/governance.md
  - docs/product/contexts/work-management.md
  - docs/product/contexts/documents.md
  - docs/product/contexts/collaboration.md
  - docs/product/contexts/integrations.md
  - docs/product/contexts/billing.md
  - docs/architecture/events-realtime-and-delivery-boundary.md
  - docs/architecture/data-ownership-and-consistency.md
  - backend/src/Notrelix.Domain/Automation/
  - backend/tests/
  - frontend/packages/product/automation-core/
  - frontend/packages/product/automation-web/
  - frontend/packages/product/automation-mobile/
review_on:
  - automation-rule-model-change
  - trigger-contract-change
  - condition-model-change
  - action-model-change
  - execution-lifecycle-change
  - schedule-semantics-change
  - recursion-policy-change
  - automation-principal-change
  - automation-template-change
  - external-provider-action-change
---

# Automation Context

> **Automation owns user-defined and system-approved rules that react to stable product facts or schedules, evaluate explicit conditions, and invoke approved product actions through normal context contracts.**
>
> Automation is not a privileged backdoor around authorization, invariants, or target-context ownership.

This document is the canonical product owner for Automation semantics.

Platform owns delivery/scheduling mechanics.

Target contexts own the business state that Automation reads or mutates.

Governance owns authorization.

Integrations owns provider connections and provider-specific synchronization.

---

# 1. Mission

Automation allows users and product workflows to express:

```text
when something happens
or when a schedule fires
→ if conditions are satisfied
→ perform one or more approved actions
```

without embedding cross-context side effects inside the source aggregate transaction.

---

# 2. Owns

Automation owns product semantics for:

```text
AutomationRule
Trigger
Condition
Action
Rule lifecycle
Execution identity
Execution lifecycle
Action-step execution state
Schedule intent
Retry/recovery semantics at automation level
Recursion/causation policy
Automation templates
Automation-specific service principal behavior
```

Current source has dedicated `Actions`, `Conditions`, `Executions`, `Rules`, `Scheduled`, `Templates`, and `Triggers`, plus `RulesEngine` and `Agents`.

---

# 3. Does not own

```text
Board/Item/Field
→ Work Management

Page/Block
→ Documents

Comment/Notification
→ Collaboration

provider connection/sync/webhook
→ Integrations

resource permission/policy
→ Governance

commercial entitlement/limits
→ Billing

message broker/outbox/retry infrastructure
→ Platform
```

Automation can consume or invoke these owners through approved contracts.

---

# 4. Ubiquitous language

**Automation Rule** — durable definition combining trigger, optional conditions, ordered/defined actions, owner/scope, and lifecycle.

**Trigger** — stable event/time/manual source that can initiate a rule evaluation.

**Condition** — deterministic predicate over trigger payload plus explicitly loaded facts.

**Action** — approved semantic operation invoked when conditions pass.

**Execution** — durable identity/state for one logical run of one Rule.

**Action Step** — one action attempt/outcome inside an Execution.

**Schedule** — user/business intent describing when a trigger should fire.

**Automation Template** — reusable rule-creation input.

---

# 5. AUT-001 — Automation Rule has explicit valid configuration

A Rule defines:

```text
owner
Account/Workspace scope
trigger
conditions
actions
lifecycle
execution principal/policy
```

Configuration is validated by trigger/condition/action type.

---

# 6. Draft versus executable Rule

A draft may preserve incomplete configuration if the product supports drafting.

An enabled/executable Rule MUST have complete valid configuration.

---

# 7. AUT-002 — Invalid configuration cannot execute

An invalid or incomplete Rule cannot be enabled merely because the frontend hides the bad field.

Server-side enable validation is authoritative.

---

# 8. Rule identity

Rule identity remains stable across:

- rename;
- description edits;
- enable/disable;
- action reorder;
- configuration edits.

Execution history must not depend only on mutable Rule name.

---

# 9. Rule lifecycle

Product lifecycle can include:

```text
draft
enabled
disabled
archived
```

where approved.

Current source has first-class `AutomationRuleStatus`.

---

# 10. AUT-003 — Disabled Rule stops new executions without erasing history

Disabling prevents new matching/scheduled executions after the effective disable point.

Historical Executions remain durable according to retention policy.

---

# 11. Archive

Archive is different from disable if product semantics require:

- hidden from normal management;
- retained history;
- no new execution;
- restorable behavior.

Do not use archive as generic delete alias.

---

# 12. Rule edit while enabled

Editing an enabled Rule must define what happens to:

```text
already queued execution
currently running execution
future matching event
scheduled future fire
```

Ambiguity is forbidden.

---

# 13. AUT-004 — Execution uses an explicit Rule version/config snapshot

A logical Execution MUST know which Rule semantics it runs.

Supported strategies may include:

```text
capture immutable Rule revision/config at execution creation
or
bind to explicit version identifier
```

Do not let in-flight executions silently switch to a newly edited Rule.

---

# 14. Rule revision

A Rule revision/version is different from aggregate optimistic-concurrency Version.

The former explains execution semantics/history.

The latter prevents stale mutation.

---

# 15. Trigger

A Trigger describes the fact or time condition that initiates Rule evaluation.

Current source includes `AutomationTrigger`, `AutomationTriggerType`, and `TriggerConfig`.

---

# 16. AUT-005 — Trigger identity is stable and contract-driven

Event triggers subscribe to logical product/integration event identity and compatible version semantics.

They MUST NOT depend on:

- CLR class name;
- DB table update;
- internal namespace;
- one frontend event name.

---

# 17. Event trigger

Canonical flow:

```text
committed product fact
→ durable publication/delivery
→ Automation matcher
→ Rule evaluation
```

The source aggregate does not synchronously execute arbitrary Automation side effects.

---

# 18. AUT-006 — Source transaction does not execute Automation side effects inline

The original product operation must be able to commit according to its own contract.

Automation reactions occur after a committed durable fact unless the product explicitly owns a synchronous cross-context invariant, which is exceptional.

---

# 19. Trigger payload

Trigger payload should contain enough stable information for matching and condition evaluation without leaking unnecessary sensitive/internal data.

Automation may load additional current facts through Application contracts.

---

# 20. Trigger version compatibility

A Rule created against event contract version N must define behavior when producer evolves.

Possible:

```text
compatible reader
explicit migration
disable until upgraded
```

Silent reinterpretation is forbidden.

---

# 21. Manual trigger

A manual trigger, if supported, is an explicit Automation operation.

It still requires:

- permission;
- scope;
- Rule validity;
- execution identity;
- normal Action authorization.

---

# 22. Schedule trigger

Scheduled trigger is based on stored schedule intent and approved scheduler infrastructure.

Current source has `ScheduleDefinition`, `ScheduledJob`, and `ScheduledJobStatus`.

---

# 23. AUT-007 — Schedule intent is product state; clock/scheduler is infrastructure

Automation Domain may store:

```text
schedule definition
time zone
next/effective intent
status
```

It does not call ambient system clock/provider scheduler as hidden Domain dependency.

---

# 24. Schedule semantics

Schedule must distinguish where relevant:

```text
one-time
recurring
calendar-based
interval-based
time zone
DST behavior
missed-fire/catch-up behavior
```

---

# 25. AUT-008 — Time zone and daylight-saving semantics are explicit

A schedule such as:

```text
09:00 every weekday
```

is incomplete unless the relevant time zone and DST behavior are defined.

---

# 26. Missed schedule

If scheduler/service is unavailable at fire time, product policy decides:

```text
skip
fire once on recovery
catch up each missed occurrence
bounded catch-up
```

Do not invent behavior in the scheduler implementation.

---

# 27. Scheduled job identity

A scheduled occurrence must have stable identity sufficient to prevent duplicate logical Execution creation under retry/restart.

---

# 28. AUT-009 — One logical trigger occurrence creates one logical Execution

Duplicate delivery of the same source event or scheduled occurrence resolves to the same logical execution identity.

---

# 29. Conditions

Conditions decide whether a matched Rule proceeds.

Current source has a dedicated `Conditions` area.

---

# 30. AUT-010 — Condition evaluation is deterministic at a defined consistency point

Conditions evaluate from:

```text
trigger fact
+
explicitly loaded facts
+
Rule config
```

at a defined point.

They must not depend on hidden random time/global mutable process state.

---

# 31. Condition types

Potential conditions may evaluate:

- field values;
- resource status;
- actor/owner;
- membership/permission fact;
- time/window;
- relation state;
- provider/integration fact.

Each type needs explicit config/schema.

---

# 32. Condition null/missing behavior

Missing or inaccessible facts must define deterministic outcome:

```text
false
terminal configuration error
retryable evaluation
```

according to condition semantics.

Fail-open behavior must not be accidental.

---

# 33. AUT-011 — Condition failure is not execution failure

When a valid condition evaluates `false`, the Rule simply does not perform actions.

This is different from:

- condition evaluation error;
- missing dependency;
- permission failure.

---

# 34. Conditions and authorization

A Rule should not use a condition as a substitute for Action authorization.

Example:

```text
if user is admin
→ update resource
```

still requires normal target Action authorization.

---

# 35. Actions

Current source includes `AutomationAction`, `AutomationActionType`, and `ActionConfig`.

An Action is an approved product operation description, not arbitrary code execution.

---

# 36. AUT-012 — Actions use normal capability contracts

Actions invoke normal Application/use-case contracts for the target capability.

Examples:

```text
update Item field
assign member
create Comment/Notification
create/update Page where approved
invoke Integration/provider action
```

Automation MUST NOT directly update another context's persistence.

---

# 37. Action config

Each Action Type defines:

```text
config schema
required references
input mapping
validation
idempotency semantics
authorization model
retry classification
result/output
```

---

# 38. AUT-013 — Action configuration is typed, not arbitrary unvalidated JSON

Flexible serialization is acceptable behind a discriminator + schema + version contract.

A new Action Type is incomplete until execution semantics are defined.

---

# 39. Action ordering

If a Rule has multiple Actions, order/parallelism must be explicit.

Possible:

```text
ordered sequential
independent parallel
dependency graph
```

Do not rely on array iteration as undocumented product behavior.

---

# 40. AUT-014 — Action dependency semantics are explicit

If Action B depends on A's output, that relationship is part of the Rule semantics.

A retry must not accidentally re-run already committed prior Actions without policy.

---

# 41. Action result

An Action result may be:

```text
succeeded
failed-terminal
failed-retryable
unknown
skipped
```

Unknown is important for external side effects.

---

# 42. External provider Action

Provider-specific operations route through Integrations or another approved provider port.

Automation does not own provider credentials or SDK models.

---

# 43. AUT-015 — Automation does not absorb Integrations

An Action such as “send Slack message” or “create provider task” depends on an Integration connection/provider capability.

Automation owns Rule/Execution intent.

Integrations owns connection, provider translation, secret, rate limit, provider operation semantics.

---

# 44. Notification Action

If Automation creates a user Notification:

```text
Automation
→ invokes Collaboration notification contract
```

Collaboration remains Notification owner.

---

# 45. Work Management Action

Updating Board/Item uses Work Management semantics:

- field validation;
- expected version;
- target lifecycle;
- permission.

Automation is not allowed to bypass them.

---

# 46. Documents Action

Creating/updating Documents uses Page/Block contracts and current authorization.

Rule config cannot write arbitrary document persistence.

---

# 47. Execution

Current source contains first-class `AutomationExecution` and `AutomationExecutionStatus`.

Execution is durable product state representing one logical Rule run.

---

# 48. AUT-016 — Execution identity is stable and idempotent

At minimum, identity must encode or derive from:

```text
Rule identity/version
source event or scheduled occurrence identity
```

plus action-step identity where needed.

Duplicate source delivery cannot create duplicate logical execution effects.

---

# 49. Execution lifecycle

Potential lifecycle:

```text
created/pending
running
succeeded
failed
cancelled
```

plus partial/awaiting retry if product needs them.

Exact current statuses are executable evidence.

---

# 50. Execution started

Starting Execution must be retry-safe.

A duplicate worker claim must not create two independent running business executions.

---

# 51. AUT-017 — Execution state advances after durable action outcome

Do not mark an Action/Execution successful before the target operation/provider effect is durably known successful.

---

# 52. Partial failure

For a multi-action Rule, product must define:

```text
stop on first failure
continue independent actions
retry failed steps only
compensate where supported
```

No hidden arbitrary policy.

---

# 53. Compensation

Compensation is an explicit new Action/workflow.

It is not magical rollback of already committed foreign-context effects.

---

# 54. Retry

Retry depends on Action failure category.

Retryable examples:

- transient network;
- provider rate limit;
- temporary service outage.

Terminal examples:

- invalid target;
- invalid config;
- revoked permission;
- incompatible field.

---

# 55. AUT-018 — Retry never means repeat side effect blindly

Every retryable Action requires stable operation identity/idempotency or a reconciliation strategy.

Especially for:

- external creates;
- notifications;
- assignments;
- provider messages;
- document creation.

---

# 56. Unknown outcome

If an external call times out after provider may have committed the effect, mark outcome uncertain and reconcile.

Do not convert timeout directly into safe retry without idempotency evidence.

---

# 57. Execution history

Users/operators may need:

```text
trigger/source
Rule revision
conditions result
action steps
attempts/outcomes
failure reason
timing
principal
correlation
```

with safe redaction.

---

# 58. AUT-019 — Execution history is product evidence, not transport log

Broker delivery attempts and internal stack traces are operational details.

Execution history presents stable automation meaning.

---

# 59. Execution retention

Historical Executions may outlive disabled/archived Rule according to retention policy.

Rule deletion must not silently erase required history.

---

# 60. Authorization principal

Automation execution uses an explicit security principal/policy.

Possible models:

```text
original initiating actor
Rule owner
Workspace automation service principal
explicit delegated capability
```

The chosen model must be stable and reviewable.

---

# 61. AUT-020 — Automation never silently escalates privilege

Creating a Rule while authorized does not guarantee future Action remains authorized forever.

Permission/membership/resource changes can make later execution fail.

---

# 62. Permission revocation

If Action permission is revoked:

- future Action fails/Rule may disable depending policy;
- failure is visible;
- Automation must not use old cached allow indefinitely.

---

# 63. Service principal

If a Workspace automation principal exists, it has bounded capabilities and scope.

It is not unrestricted system authority.

---

# 64. Audit

High-impact Rule create/edit/enable/disable and sensitive executions may require Governance audit.

Execution history is not Governance Audit.

---

# 65. AUT-021 — Automation history and Audit remain distinct

Execution history explains Automation behavior.

Audit proves governed actions/changes under security policy.

---

# 66. Recursion

Automation-generated target changes can produce new events that match Rules.

This is valid only under bounded recursion policy.

---

# 67. AUT-022 — Recursive Automation is bounded

Use explicit origin/causation/depth/dedup/suppression semantics to prevent infinite loops.

Example:

```text
Rule A changes Status
→ Rule B changes Status
→ Rule A ...
```

must terminate or be rejected by policy.

---

# 68. Causation

Execution should retain enough causation identity to determine:

- source event;
- source Automation execution;
- recursion depth;
- linked downstream effects.

---

# 69. Self-trigger

A Rule may or may not be allowed to trigger from its own Action-produced event.

This is explicit per policy/trigger/action, not accidental.

---

# 70. Rule-to-rule chain

Cross-Rule chaining can be powerful but needs:

```text
max depth
dedup
loop detection
operational visibility
```

---

# 71. Runaway protection

Automation may enforce:

- execution-rate limits;
- recursion depth;
- per-Rule concurrency;
- Workspace quotas;
- Billing limits.

These are different mechanisms and owners.

---

# 72. Billing relation

Billing can own Automation entitlement/usage limits.

Automation owns Rule/Execution state.

Plan downgrade may:

- block new Rule creation;
- disable premium triggers/actions;
- preserve history;
- pause execution.

Product policy must define it.

---

# 73. AUT-023 — Billing limit does not own Automation lifecycle

Billing supplies entitlement/usage facts.

Automation decides safe Rule/Execution behavior under those facts.

---

# 74. Concurrency

Potential concurrency concerns:

```text
same source event delivered twice
same schedule claimed twice
Rule edited while executing
Execution retried by several workers
two actions update same target
```

All require deterministic semantics.

---

# 75. AUT-024 — Execution claim concurrency cannot duplicate logical work

Worker/runtime mechanics must ensure one logical Action effect despite duplicate claim/processing.

Idempotency remains required even with locking.

---

# 76. Conditions and current state race

A condition may evaluate true, then target state changes before Action.

Target Action still validates current invariants/concurrency.

Condition result is not a lock on foreign state.

---

# 77. AUT-025 — Target context revalidates at action time

Automation cannot treat a prior condition check as permanent authority or invariant proof.

---

# 78. Templates

Current source has a `Templates` area.

Automation Template is reusable Rule-creation input.

---

# 79. AUT-026 — Automation Template is creation input, not live shared Rule authority

Instantiating a template creates ordinary Rule identity/config.

Later template changes do not silently mutate existing Rules unless linked templates are explicitly designed.

---

# 80. Template compatibility

A template must validate all referenced Trigger/Condition/Action types and required config under current product contracts.

---

# 81. Agents source area

Current Automation source contains an `Agents` folder.

Its existence is implementation evidence only.

It MUST NOT automatically redefine Automation as an autonomous-agent platform.

---

# 82. AUT-027 — Agent capability requires explicit product admission

Before an Agent concept becomes canonical Automation product semantics, define:

```text
purpose
authority
planning/action boundary
tool/action set
memory/state
human approval
security
cost/limits
determinism expectations
failure/recovery
audit
```

Until then, Rule/Trigger/Condition/Action/Execution remains the canonical core.

---

# 83. Arbitrary code

User-provided arbitrary code execution is forbidden by default.

A future scripting capability would require a separate security/sandbox architecture.

---

# 84. AUT-028 — Automation actions are allow-listed capabilities

The Rule engine executes known typed Actions.

It does not evaluate arbitrary server-side code from user config.

---

# 85. Secrets

Rule config may reference a provider connection/secret identity.

It MUST NOT contain raw OAuth/API secrets exposed to client or Domain event.

---

# 86. AUT-029 — Automation configuration never stores reusable provider secrets

Secrets stay in Integrations/security infrastructure and are referenced by safe connection identity.

---

# 87. Events/facts

Potential stable Automation facts:

```text
AutomationRuleCreated
AutomationRuleEnabled/Disabled/Archived
AutomationExecutionCreated/Started/Succeeded/Failed/Cancelled
AutomationActionSucceeded/Failed
ScheduledOccurrenceCreated/Fired
```

Publish only where stable consumers exist.

---

# 88. AUT-030 — Execution facts use logical identity, not worker attempt identity

One logical execution may have several technical attempts.

Downstream product facts must not multiply because transport/workers retried.

---

# 89. Realtime

Realtime can surface:

- Rule status;
- Execution status;
- action progress;
- validation/failure changes.

Durable query remains authoritative.

---

# 90. AUT-031 — Realtime progress is recoverable from durable Automation state

A missed websocket message cannot permanently hide or duplicate an Execution result.

---

# 91. Notifications/activity

Automation execution may create user-facing Activity/Notification through Collaboration.

Technical retries do not create repeated attention for one logical outcome.

---

# 92. Analytics

Analytics may derive:

- execution success rate;
- execution duration;
- Rule usage;
- trigger/action volume.

Analytics does not own Execution lifecycle.

---

# 93. Integrations

Provider Actions rely on Integration connection and operation semantics.

Provider webhook facts may also trigger Automation after Integrations authenticates/translates them.

---

# 94. AUT-032 — Raw provider webhook does not trigger product Automation directly

Canonical flow:

```text
provider request
→ Integrations authenticates/deduplicates/translates
→ stable integration/product fact
→ Automation trigger
```

Do not let unverified provider JSON become Automation authority.

---

# 95. Work Management

Work Management facts can trigger Automation; Automation Actions can invoke Work Management.

Both directions use stable contracts.

---

# 96. Documents

Document facts can trigger Rules; actions can create/update documents through normal Documents contracts.

---

# 97. Collaboration

Comments/Mentions can trigger Automation where approved.

Automation-generated Notification/Comment remains Collaboration-owned.

---

# 98. Governance

Governance protects Rule management and target Actions.

Permission management Actions are high risk and forbidden by default unless explicitly product/security-designed.

---

# 99. AUT-033 — Automation cannot casually modify authorization policy

Actions that grant roles, permissions, share links, or security policy require explicit approved product capability and stronger review.

---

# 100. Deleting Rule

Deleting/archiving Rule must define:

- scheduled jobs;
- pending Executions;
- in-flight Executions;
- history retention.

Do not delete dependent execution evidence blindly.

---

# 101. Deleting target resource

A Rule referencing deleted Board/Field/Page/etc. becomes:

```text
invalid
disabled
or
action-failing
```

according to explicit reference policy.

Do not silently retarget another resource.

---

# 102. AUT-034 — Broken Automation reference is explicit

Field/resource/schema changes that invalidate Rule configuration must be detected and surfaced.

Do not keep a Rule “enabled” while it can never execute correctly.

---

# 103. Schema evolution

Adding/removing Trigger/Condition/Action types requires:

```text
config schema/version
migration
frontend editor/registry
execution compatibility
history reader
tests
```

---

# 104. Deleting Field used by Rule

Work Management Field deletion should detect Automation references.

Policy may:

- block deletion;
- disable affected Rules;
- require migration.

Silent breakage is forbidden.

---

# 105. Schedule deletion

Removing scheduled Rule/job stops future occurrences but preserves historical Executions.

---

# 106. Failure UX

Users should distinguish:

```text
Rule invalid
Rule disabled
condition false
execution running
action retrying
permission revoked
target deleted
provider unavailable
provider outcome unknown
terminal failed
```

---

# 107. AUT-035 — Execution failure reason is actionable and safely redacted

Expose product-relevant reason and next step without leaking secrets/internal stack traces.

---

# 108. Current source alignment

Current Automation Domain contains:

```text
Actions
Agents
Conditions
Executions
Rules
RulesEngine
Scheduled
Templates
Triggers
```

Current source has first-class `AutomationAction`, `AutomationActionType`, `ActionConfig`, `AutomationExecution`, `AutomationExecutionStatus`, `AutomationRule`, `AutomationRuleStatus`, `ScheduleDefinition`, `ScheduledJob`, `ScheduledJobStatus`, `AutomationTrigger`, `AutomationTriggerType`, and `TriggerConfig`.

This supports a mature Rule/Trigger/Condition/Action/Execution/Schedule model.

---

# 109. Current ambiguity watch

Do not normalize:

```text
Agents folder
→ autonomous agent product model

RulesEngine
→ Domain may perform provider I/O

ScheduledJob
→ scheduler owns schedule semantics

ActionConfig JSON
→ schema-less arbitrary action

Rule owner
→ permanent target authorization

execution retry
→ safe repeat of external effect
```

---

# 110. Change impact — Trigger

Review:

```text
producer event owner
event version/backlog
Rules
condition payload
frontend editor
execution identity
tests
```

---

# 111. Change impact — Action

Review:

```text
target context
Governance
idempotency
retry/unknown outcome
config schema
frontend editor
history
Billing entitlement
```

---

# 112. Change impact — Schedule

Review:

```text
time zone/DST
scheduler
missed-fire
idempotency
deployment/recovery
frontend
```

---

# 113. Change impact — Execution

Review:

```text
durable state
retry
worker concurrency
realtime
history
Analytics
operations
```

---

# 114. Rule checklist

```text
[ ] owner/scope
[ ] lifecycle
[ ] trigger type/config
[ ] conditions typed
[ ] actions typed/ordered
[ ] valid references
[ ] principal policy
[ ] entitlement
[ ] recursion behavior
[ ] revision/config snapshot
```

---

# 115. Execution checklist

```text
[ ] stable logical identity
[ ] source event/occurrence
[ ] Rule revision
[ ] condition result
[ ] action-step identities
[ ] idempotency
[ ] retry categories
[ ] unknown outcome
[ ] permission revalidation
[ ] durable result/history
[ ] realtime recoverability
```

---

# 116. Trigger checklist

```text
[ ] stable logical trigger identity
[ ] producer owner
[ ] version compatibility
[ ] payload schema
[ ] dedup identity
[ ] tenant/resource scope
[ ] replay behavior
```

---

# 117. Action checklist

```text
[ ] target context
[ ] approved Application contract
[ ] config schema/version
[ ] authorization
[ ] idempotency
[ ] retry/unknown semantics
[ ] result/output
[ ] no direct persistence
[ ] no secret leakage
```

---

# 118. Schedule checklist

```text
[ ] time zone
[ ] recurrence semantics
[ ] DST
[ ] missed-fire
[ ] occurrence identity
[ ] disable/archive behavior
[ ] deployment/restart safety
```

---

# 119. Testing/evidence

Critical evidence should cover:

```text
draft/enable validation
Rule lifecycle
Rule revision/in-flight edit
trigger matching/version
duplicate source event
schedule occurrence idempotency
condition determinism
Action config validation
Action ordering/dependency
execution lifecycle
duplicate worker/retry
partial failure
provider unknown outcome
permission revocation
recursion loops/depth
broken field/resource reference
template instantiation
secrets non-exposure
realtime/history
```

---

# 120. Stop conditions

Stop rather than guess if:

- an Automation Action writes another context's DB directly;
- Rule config executes arbitrary user code;
- event trigger uses CLR class/DB table identity;
- duplicate source event creates duplicate Execution;
- retry repeats external create with no idempotency/reconciliation;
- Rule owner is treated as permanent unrestricted authority;
- recursive Rules have no loop/depth policy;
- provider secrets appear in Rule config/client/event;
- Raw provider webhook triggers Automation before Integrations verification;
- schedule lacks time-zone/missed-fire semantics;
- enabled Rule references deleted/incompatible field/resource with no invalidation policy;
- Agents source is promoted to product authority without explicit product/security design.

# 121. Related canonical owners

```text
PRODUCT.md
docs/product/product-model.md
docs/product/product-experience.md
docs/product/contexts/governance.md
docs/product/contexts/work-management.md
docs/product/contexts/documents.md
docs/product/contexts/collaboration.md
docs/product/contexts/integrations.md
docs/product/contexts/billing.md
docs/product/contexts/analytics.md
docs/architecture/events-realtime-and-delivery-boundary.md
docs/architecture/data-ownership-and-consistency.md
docs/architecture/contract-boundaries.md
backend/docs/architecture/platform-and-messaging.md
```

# 122. Final Automation rule

For every Automation capability, answer:

```text
What Rule/trigger causes this?
What stable source fact or schedule occurrence identifies it?
What Rule revision/config applies?
Which conditions are deterministic?
Which target context owns each Action?
Which principal authorizes the Action now?
How is the execution idempotent?
What happens on duplicate, retry, partial failure, or unknown outcome?
Can the action recursively trigger more Automation?
What limits recursion/runaway execution?
What happens if referenced schema/resource/permission changes?
What history/realtime state lets users understand the outcome?
```

The target is:

> **durable, typed, observable automation that reacts to stable product facts, invokes ordinary product capabilities safely, and remains correct under duplicate delivery, retries, permission changes, recursion, scheduling, and provider uncertainty.**
