---
document_id: DEL-CHANGE-CLASSIFICATION
document_type: delivery-policy
status: active
owner: engineering-delivery
applies_to:
  - repository
  - backend
  - frontend
  - contracts
  - data
  - infrastructure
  - documentation
evidence:
  - RULE.md
  - AGENTS.md
  - docs/governance/decision-and-exception-policy.md
  - docs/governance/topic-authority-map.md
  - docs/architecture/contract-boundaries.md
  - docs/architecture/data-ownership-and-consistency.md
  - docs/quality/engineering-quality-standard.md
  - docs/quality/testing-strategy.md
  - docs/quality/security-quality-standard.md
  - .github/workflows/be-ci.yml
  - .github/workflows/fe-ci.yml
review_on:
  - change-classification-change
  - delivery-model-change
  - contract-versioning-change
  - migration-policy-change
  - rollout-policy-change
  - feature-flag-policy-change
  - adr-policy-change
  - release-evidence-change
---

# Change Classification

> **A change is delivered according to the contracts it affects, not according to how small the diff looks.**
>
> A one-line rename can be breaking. A large internal refactor can be local. Classification follows semantic impact, consumers, data, security, deployment, and recovery.

This document owns repository-wide change classification and the delivery obligations that follow from each class.

It does not replace detailed rollout, migration, release, ownership, or operational runbooks. It decides **which of those obligations apply**.

# 1. Why classification exists

Before implementation, review, and rollout, classify what changes.

The purpose is to prevent:

```text
"just a refactor"
"just add a field"
"same PR so deployment is atomic"
"frontend and backend change together"
"we can rollback if needed"
```

from substituting for impact analysis.

# 2. DEL-CHG-001 — Analyze consumers before changing a contract

Inventory affected:

```text
source callers
REST/API
events/messages
realtime
generated clients
frontend/mobile
database/schema/data
cache/projections/search
background jobs
integrations/providers
tests
CI
operations
documentation
```

Search is required. Folder proximity is not enough.

# 3. Change model

Classify each change using:

```text
one or more primary change classes
+
zero or more risk modifiers
```

Classes are **not mutually exclusive**.

Example:

```text
rename persisted event identity
=
contract change
+
data migration
+
async/backlog modifier
```

# 4. DEL-CHG-002 — Delivery obligations are cumulative

If a change is simultaneously:

```text
schema
security
public contract
```

it must satisfy all applicable obligations.

Do not choose the easiest label.

# 5. Primary classes

Canonical primary classes:

```text
C0  local implementation
C1  additive compatible contract
C2  behavioral compatible semantic change
C3  breaking contract
C4  data/schema/persistence change
C5  architecture boundary/dependency change
C6  security/tenant/authorization change
C7  runtime/config/infrastructure change
C8  destructive/retention/commercially irreversible change
```

# 6. Risk modifiers

Examples:

```text
MOBILE_LAG
ASYNC_BACKLOG
PROVIDER_EXTERNAL
HIGH_CARDINALITY
HIGH_CONCURRENCY
CROSS_TENANT
GENERATED_ARTIFACT
FEATURE_FLAG
DATA_BACKFILL
ROLLBACK_UNSAFE
PUBLIC_SHARING
FINANCIAL
PRIVACY_RETENTION
```

Use modifiers to force overlooked concerns into the plan.

# 7. C0 — Local implementation

A C0 change alters implementation without changing externally observable semantic contract.

Examples:

- private helper refactor;
- equivalent algorithm replacement;
- local naming change not persisted/exported;
- internal file move preserving boundaries;
- performance improvement preserving semantics.

# 8. DEL-CHG-003 — Local implementation preserves all external and persisted semantics

C0 means no change to:

```text
public API
event/realtime contract
generated consumer
database persisted meaning
authorization/tenant policy
product behavior
configuration/deployment contract
architecture boundary
```

# 9. Refactor proof

C0 still needs relevant tests/architecture gates.

“Internal” is not “untested”.

# 10. Rename test

Before calling a rename C0, search whether the name appears in:

```text
database column/value
event name
JSON property
OpenAPI
route
config key
cache key
feature flag
metric key
provider mapping
generated artifact
```

# 11. DEL-CHG-004 — Persisted or public identity rename is not refactor-only

A rename crossing compatibility/data boundary is classified according to that boundary.

# 12. C1 — Additive compatible contract

C1 adds a contract element while preserving old consumers.

Examples:

- optional response field;
- new event type;
- new endpoint/version;
- new optional capability;
- new generated contract field tolerated by existing consumers.

# 13. DEL-CHG-005 — Additive means existing consumers remain valid

Adding a field is not compatible if:

- consumer uses strict unknown-field parser;
- semantic default changes;
- payload size/required interpretation changes incompatibly;
- old client cannot handle new enum value.

Compatibility is consumer-specific.

# 14. Additive deployment

Preferred sequence:

```text
producer supports old + new
→ publish/generate
→ migrate consumers
→ observe
→ later remove old only in separate classified change
```

# 15. C2 — Behavioral compatible semantic change

Shape may remain compatible while meaning changes in a way consumers/tests must understand.

Examples:

- stricter validation within documented contract;
- changed default that remains contract-compatible;
- new retry semantics;
- change from immediate to explicit eventual outcome without wire break;
- permission behavior change that is intentionally compatible for allowed users.

# 16. DEL-CHG-006 — Shape compatibility does not prove semantic compatibility

Same JSON/schema can still be a behavioral contract change.

Review:

```text
meaning
timing
ordering
error behavior
idempotency
default
authorization
freshness
```

# 17. C3 — Breaking contract

A C3 change makes an existing consumer/data interpretation invalid without migration/version coordination.

Examples:

```text
remove/rename required field
change type
change enum meaning
change route incompatibly
change logical event identity
change requiredness/nullability
change ordering/retry guarantee
change package public export
```

# 18. DEL-CHG-007 — Breaking change has explicit compatibility strategy

Choose one:

```text
versioned parallel contract
coordinated deployment with proven atomicity
consumer-first migration
adapter/compatibility layer
deprecation window
```

Never rely on “same PR” alone.

# 19. Public versus private consumers

Inventory:

- web;
- mobile;
- external/API users;
- background consumers;
- stored messages/backlog;
- generated SDK/client.

Mobile/external consumers can lag deployment.

# 20. DEL-CHG-008 — Independent consumers define the mixed-version window

If producer and consumer cannot deploy atomically, delivery must support the overlap window.

# 21. C4 — Data/schema/persistence change

C4 changes durable representation or data semantics.

Examples:

- migration;
- new column/index/constraint;
- rename/drop;
- JSON schema change;
- data ownership move;
- backfill;
- new RLS policy;
- persisted discriminator/status/key change.

# 22. DEL-CHG-009 — Schema change includes existing production data

Migration proof asks:

```text
what old rows exist?
what null/legacy values exist?
how large is the table?
how long can migration run?
what concurrent app versions exist?
```

Empty database success is insufficient.

# 23. Expand/contract

Preferred compatible sequence where possible:

```text
expand
→ deploy compatible code
→ backfill/migrate
→ switch reads/writes
→ verify
→ contract/remove old
```

# 24. DEL-CHG-010 — Destructive contraction waits for completion proof

Do not remove old column/path/event until:

- writers stopped;
- readers migrated;
- backfill complete;
- backlog handled;
- telemetry/evidence confirms no use.

# 25. Backfill

A backfill defines:

```text
selection
batch
idempotency
checkpoint
concurrency
tenant scope
failure/retry
completion proof
```

# 26. DEL-CHG-011 — Backfill is production code

Treat migration/backfill with:

- review;
- tests;
- observability;
- safe restart;
- bounded resource use.

# 27. Data ownership migration

Moving semantic authority between contexts is not a simple table move.

Plan:

```text
new owner contract
dual-read/write if necessary
migration
cutover
consumer migration
old authority removal
```

# 28. DEL-CHG-012 — Dual write never means dual authority

During migration, declare which side is authoritative at each phase.

# 29. C5 — Architecture boundary/dependency change

C5 changes:

- bounded-context ownership;
- backend layer dependency;
- frontend package/layer graph;
- public module boundary;
- service/extraction boundary;
- shared foundation ownership.

# 30. DEL-CHG-013 — Architecture change requires decision impact review

If it is consequential and durable under governance policy, create/update an ADR before normalizing the new boundary.

Architecture tests/manifests/docs change atomically.

# 31. Project/package move

Moving code between projects/packages is architecture-changing when dependency/public ownership changes, even if runtime behavior is identical.

# 32. DEL-CHG-014 — Folder movement and architecture movement are distinguished

A file relocation inside the same authority can be C0.

Changing dependency/owner is C5.

# 33. C6 — Security/tenant/authorization change

C6 changes:

```text
authentication
authorization
tenant scope
RLS
share/public access
secrets
webhook verification
provider scope
file security
Billing/admin permission
```

# 34. DEL-CHG-015 — Security change requires negative-path evidence

Plan explicit adversarial tests:

- wrong tenant;
- insufficient permission;
- replay;
- revoked token/share;
- malformed provider payload;
- secret non-exposure;
- fail-closed config.

# 35. Security-compatible is still security-classified

A stricter permission change may not break authorized consumers, but it is still C6 and requires security review.

# 36. C7 — Runtime/config/infrastructure change

C7 changes:

- environment/config key;
- secret injection;
- deployment topology;
- runtime dependency;
- queue/cache/database configuration;
- feature flag;
- CI/release workflow;
- worker concurrency;
- scheduler.

# 37. DEL-CHG-016 — Configuration is a deployed contract

Changing config name/default/requiredness can break deployment even if source API is unchanged.

Review every environment and startup validation.

# 38. Infrastructure migration

If old and new infrastructure coexist:

```text
compatibility
data transfer
routing
failure
fallback
cutover
cleanup
```

must be explicit.

# 39. C8 — Destructive/retention/commercially irreversible change

C8 covers changes where failure is difficult or impossible to reverse.

Examples:

```text
hard delete/purge
Account/Workspace destructive lifecycle
retention shortening
irreversible migration
provider-side deletion
financial charge/correction
ownership transfer
security credential invalidation at scale
```

# 40. DEL-CHG-017 — Irreversible change requires forward-recovery reasoning

Do not promise rollback generically.

State:

```text
what can rollback?
what cannot?
what data/effect survives?
what compensating/forward fix exists?
```

# 41. Contract modifiers

A change may be wire-compatible but still carry:

```text
MOBILE_LAG
ASYNC_BACKLOG
GENERATED_ARTIFACT
```

These change rollout obligations.

# 42. MOBILE_LAG

Use when old mobile clients can remain active after backend/web deploy.

# 43. DEL-CHG-018 — Mobile lag requires backward-compatible server window

Do not assume App Store distribution is atomic with backend release.

# 44. ASYNC_BACKLOG

Use when old messages/events/jobs can remain queued/replayed.

# 45. DEL-CHG-019 — Backlog consumers must understand old in-flight contract

Before removing event/message compatibility, prove:

- old backlog drained;
- replay archives handled;
- old producer stopped;
- consumers migrated.

# 46. PROVIDER_EXTERNAL

Use when external provider state/callback/API behavior participates.

# 47. DEL-CHG-020 — External provider is an independently changing/deployed participant

Plan:

- provider version;
- rate limits;
- webhook retries;
- unknown outcomes;
- secret/scope migration;
- rollback limits.

# 48. HIGH_CARDINALITY / HIGH_CONCURRENCY

Use when migration/query/behavior has scale or race sensitivity.

Requires representative performance/concurrency evidence.

# 49. CROSS_TENANT

Use whenever new behavior spans or can accidentally leak Account/Workspace boundaries.

Requires explicit tenant threat and negative tests.

# 50. GENERATED_ARTIFACT

Use when contract/code generation is involved.

# 51. DEL-CHG-021 — Generated artifact changes through producer

Delivery sequence:

```text
source contract
→ generator
→ generated artifact
→ consumer build/test
→ drift gate
```

Never patch generated output as the primary change.

# 52. FEATURE_FLAG

Use for temporary rollout coexistence.

A flag is not its own architecture.

# 53. DEL-CHG-022 — Feature flag has removal contract

Flag declares:

```text
owner
purpose
eligible scope
safe default
metrics/decision criteria
security parity
removal condition
```

# 54. Flag data compatibility

Both paths may touch the same data/cache/realtime.

Persisted state must remain valid when a user crosses flag cohorts unless migration intentionally separates them.

# 55. ROLLBACK_UNSAFE

Use when deployed state can make previous binary invalid.

Examples:

- destructive schema contraction;
- new event emitted old consumer cannot parse;
- provider external side effect;
- irreversible data rewrite.

# 56. DEL-CHG-023 — Rollback capability is stated, not assumed

For material rollout record:

```text
binary rollback safe?
schema rollback safe?
data rollback safe?
external-effect rollback safe?
forward-fix strategy?
```

# 57. Consumer inventory

Before contract/data change, search:

```text
code references
OpenAPI/generated output
event names
database
frontend/mobile
tests
CI
docs
dashboards/alerts
provider config
```

# 58. DEL-CHG-024 — Search beats architectural guessing

Do not assume “only this module uses it” from folder structure.

Use repository search and generated/dependency evidence.

# 59. Current-state versus authority

Source can reveal consumers/debt.

Canonical docs decide intended semantic owner.

If they conflict, classify drift before implementing.

# 60. DEL-CHG-025 — Existing coupling is not automatic precedent

A change may need to remove/migrate source debt rather than document it as architecture.

# 61. Contract-first sequence

For REST/realtime/event/public package contracts:

```text
1. confirm product semantics/owner
2. define contract
3. classify compatibility
4. define mixed-version window
5. implement producer
6. generate/publish artifacts
7. update consumers
8. run compatibility/integration gates
9. remove old path only after proof
```

# 62. DEL-CHG-026 — Contract producer and consumer are not assumed atomic

One repository/PR can still deploy:

- backend;
- web;
- mobile;
- workers

at different times.

# 63. Error behavior

Changing error status/code/retry semantics can be C2/C3 even when success payload is unchanged.

# 64. DEL-CHG-027 — Failure contract is part of compatibility

Consumers may depend on:

```text
validation vs conflict
retryable vs terminal
not-found vs forbidden
pending vs succeeded
```

# 65. Timing/consistency

Changing synchronous completion to eventual/pending behavior is a semantic contract change.

# 66. DEL-CHG-028 — Consistency promise is classified

If users/consumers previously observed immediate final state and now observe pending/convergent state, update product/contract/UI/tests.

# 67. Idempotency

Changing operation identity/key/dedup scope can affect retried requests/messages.

Treat as C2/C3/C4 depending persisted/public identity.

# 68. Ordering

Changing ordering scope/sequence behavior is consumer-visible for systems that depend on it.

Classify accordingly.

# 69. Permission changes

Permission model changes can invalidate caches, active realtime subscriptions, frontend capability state, and stored roles.

C6 plus any data/contract modifier.

# 70. DEL-CHG-029 — Authorization migration includes stored policy and active state

Review:

```text
role/permission rows
cache
realtime
API
frontend
audit
```

# 71. Field/Block/Action type changes

Adding a type may be additive contract but can require:

- persistence;
- generated client;
- web/mobile support;
- Automation;
- Analytics;
- migration.

Classify all surfaces, not just enum addition.

# 72. Database rename

Prefer expand/copy/switch/drop over destructive rename when mixed-version rollout requires both names.

# 73. Event rename

Logical public event rename is breaking even if CLR class alias exists.

# 74. DEL-CHG-030 — Logical identity outranks implementation name

Review persisted/public identities separately from class/file names.

# 75. Feature removal

Removal requires consumer/usage inventory and deprecation/migration where applicable.

# 76. DEL-CHG-031 — Removal requires proof of non-use or completed migration

“Search found no source caller” may be insufficient for external/mobile/backlog/provider consumers.

# 77. Dependency upgrade

Classify as C0 only when verified behavior/contracts/runtime remain compatible.

Major/runtime/security-critical upgrades can be C2/C7/C6.

# 78. DEL-CHG-032 — Dependency version number does not determine semantic class alone

Read migration/release notes and test affected behavior.

# 79. Framework/runtime upgrade

Review:

```text
build
deployment image
serialization
database/provider
generated output
frontend host
CI toolchain
```

# 80. CI/gate change

Changing required CI can weaken proof without runtime code changes.

Classify as C7 plus quality governance impact.

# 81. DEL-CHG-033 — Removing/weakening a gate is a semantic delivery change

State which protected property moves to another proof owner or why it no longer exists.

# 82. Documentation-only change

A docs-only change can be:

- correction of stale evidence;
- canonical semantic change;
- architecture decision.

Classify by meaning, not file extension.

# 83. DEL-CHG-034 — Canonical documentation change can require implementation follow-up

If docs intentionally change the contract before source, mark implementation drift/transition explicitly rather than pretending repository already conforms.

# 84. ADR threshold

Use System/Backend/Frontend ADR when the decision is consequential and durable according to decision policy.

Examples:

- new dependency direction;
- service extraction;
- persistence technology;
- contract/versioning strategy;
- tenant/security foundation.

# 85. DEL-CHG-035 — ADR records the decision, not the implementation task list

Plans/checklists remain delivery artifacts.

ADR captures choice, rationale, alternatives, consequences.

# 86. Exception

Temporary rule violation uses exception governance, not a permanent change class.

An exception must not spread through unrelated work.

# 87. Rollout sequence

For each material change describe deployed stages.

Example:

```text
Stage A: backward-compatible producer/schema
Stage B: consumer migration
Stage C: data backfill
Stage D: switch authority/read path
Stage E: observe completion
Stage F: remove old path
```

# 88. DEL-CHG-036 — Every deployed stage is valid on its own

Do not design a rollout where intermediate production state violates:

- schema compatibility;
- authorization;
- tenant isolation;
- data ownership;
- consumer compatibility.

# 89. Partial failure

Migration/rollout asks:

```text
what if stage fails halfway?
what was already committed?
can rerun?
what is checkpoint?
what user impact?
```

# 90. Completion proof

Old path removal requires objective proof.

Examples:

```text
backfill count complete
zero old-format producers
consumer version floor reached
queue/backlog drained
feature-flag cohort fully switched
telemetry shows no old path
```

# 91. DEL-CHG-037 — Cleanup is a separate proven phase

Do not combine:

```text
introduce replacement
+
destroy old path
```

in one risky deployment unless true atomic compatibility is proven.

# 92. Definition of done relation

A classified change is done only when all class obligations are satisfied:

```text
semantics
implementation
migration
contracts
tests
rollout
operations
docs
exact CI revision
```

# 93. Evidence report

PR/change record should state:

```text
primary classes
risk modifiers
affected owners/consumers
migration/rollout
tests/gates run
not-applicable evidence
remaining follow-up/cleanup
```

# 94. DEL-CHG-038 — “All tests passed” is not sufficient evidence report

Name the relevant suites/gates or CI revision.

Do not claim broad proof from focused tests.

# 95. Small changes

Small C0 changes should stay lightweight.

Classification exists to avoid over-process as well as under-process.

# 96. DEL-CHG-039 — Process depth follows change impact

Do not require a migration plan/ADR for a private equivalent helper refactor.

Do require them when the affected contract/risk demands them.

# 97. Coding-agent preflight

Before editing, an agent should produce internally/explicitly as task requires:

```text
owner
current contract
primary change class(es)
risk modifiers
consumer inventory
evidence plan
stop conditions
```

# 98. DEL-CHG-040 — Agent does not invent missing migration semantics

If a material choice remains unresolved:

- authority owner;
- compatibility;
- destructive policy;
- rollback;
- security boundary;

stop under AGENTS/governance rules rather than silently choose a new architecture.

# 99. Example — private refactor

```text
replace private parser implementation
same accepted/rejected values
same public output/errors
no persistence change

Classification:
C0
```

Evidence:

- closest behavior tests;
- architecture/type/lint/build as applicable.

# 100. Example — optional API field

```text
add optional response field
old web ignores it
new web consumes it

Classification:
C1 + GENERATED_ARTIFACT
```

Evidence:

- OpenAPI;
- regenerated client;
- old/new consumer compatibility;
- API tests.

# 101. Example — event field removal

```text
remove event field
old mobile/background consumer still reads it

Classification:
C3 + ASYNC_BACKLOG + MOBILE_LAG
```

Plan:

- parallel/versioned event;
- migrate consumers;
- drain backlog;
- prove consumer floor;
- remove old field later.

# 102. Example — new required DB column

```text
new non-null semantic field for existing rows

Classification:
C4 + DATA_BACKFILL
```

Plan:

```text
nullable/default-safe expansion
→ deploy writer
→ bounded backfill
→ verify
→ enforce constraint
```

# 103. Example — move authorization to new policy model

```text
resource access semantics change
stored roles/rules migrate

Classification:
C6 + C4 + C2
```

Evidence:

- policy migration;
- negative authz;
- cache/realtime invalidation;
- compatibility/UI behavior.

# 104. Example — extract Integrations worker service

```text
deployment topology + network boundary
same product semantics

Classification:
C5 + C7 + PROVIDER_EXTERNAL
```

Needs:

- System ADR;
- explicit contracts;
- data/secret ownership;
- rollout/rollback;
- observability;
- service extraction strategy.

# 105. Example — remove old feature flag

```text
new path fully adopted
old data path no longer used

Classification:
C0/C7 cleanup
```

only after:

- cohort complete;
- compatibility verified;
- old path unused;
- flag/data cleanup safe.

# 106. Classification checklist

```text
[ ] semantic owner known
[ ] primary class(es)
[ ] risk modifiers
[ ] consumers searched
[ ] public/persisted identities reviewed
[ ] data migration/backfill?
[ ] mixed-version window?
[ ] mobile/backlog/provider?
[ ] security/tenant impact?
[ ] architecture/ADR impact?
[ ] feature flag?
[ ] rollback/forward recovery?
[ ] completion proof?
[ ] docs/tests/CI owners?
```

# 107. Breaking-change checklist

```text
[ ] producer
[ ] every consumer class
[ ] old/new compatibility
[ ] version/adapter
[ ] deployment sequence
[ ] mobile lag
[ ] queue/backlog
[ ] generated artifacts
[ ] errors/timing semantics
[ ] removal proof
```

# 108. Data-migration checklist

```text
[ ] old data shape
[ ] target data shape
[ ] expansion
[ ] reader/writer compatibility
[ ] batch/backfill
[ ] idempotency/checkpoint
[ ] tenant isolation
[ ] performance/locking
[ ] completion verification
[ ] contraction
[ ] rollback/forward recovery
```

# 109. Security-change checklist

```text
[ ] threat boundary
[ ] tenant/resource scope
[ ] authn/authz
[ ] RLS/cache/realtime/background
[ ] secrets
[ ] negative tests
[ ] stored policy migration
[ ] revocation
[ ] audit
```

# 110. Rollout checklist

```text
[ ] valid stage sequence
[ ] mixed-version behavior
[ ] feature flag/cohort if needed
[ ] observability
[ ] partial failure
[ ] rollback safe?
[ ] forward recovery
[ ] completion metrics
[ ] old path removal
```

# 111. Stop conditions

Stop rather than implement/merge if:

- a persisted/public rename is being labeled refactor-only;
- consumer inventory is based only on folder proximity;
- old/new independently deployed versions cannot coexist and no coordinated plan exists;
- migration ignores existing production data;
- backfill has no idempotency/checkpoint/completion proof;
- dual-write phase has no declared authority;
- destructive contraction occurs before migration proof;
- security change has no negative-path evidence;
- feature flag has no owner/removal condition;
- rollback is promised despite irreversible schema/provider/data effects;
- mobile/backlog/provider lag is ignored;
- an architecture boundary changes without required ADR/gate updates;
- generated output is patched without producer change;
- old path is removed in the same step that first introduces its replacement without proven atomicity.

# 112. Related canonical owners

```text
docs/governance/decision-and-exception-policy.md
docs/architecture/contract-boundaries.md
docs/architecture/data-ownership-and-consistency.md
docs/architecture/capability-extraction-strategy.md
docs/quality/engineering-quality-standard.md
docs/quality/testing-strategy.md
docs/quality/security-quality-standard.md
docs/quality/performance-and-scalability.md
docs/delivery/contract-first-delivery.md
docs/delivery/definition-of-done.md
docs/delivery/release-and-rollout.md
docs/delivery/migration-policy.md
```

# 113. Final change-classification rule

Before changing code/data/contracts, answer:

```text
What semantic contract changes?
Which primary class(es) apply?
Which risk modifiers apply?
Who consumes the old behavior/data?
Can old and new versions coexist?
What migration/backfill is required?
Which stage becomes authoritative when?
What security/tenant risks change?
What evidence proves each stage?
Can rollback really restore prior state, or is forward recovery required?
What proves the old path can finally be removed?
```

The target is:

> **delivery in which the migration, rollout, testing, ADR, compatibility, and recovery burden follows the real semantic impact of the change—not the file count, diff size, or the engineer's preferred label.**
