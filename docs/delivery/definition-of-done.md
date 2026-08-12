---
document_id: DEL-DEFINITION-OF-DONE
document_type: delivery-policy
status: active
owner: engineering-delivery
applies_to:
  - repository
  - backend
  - frontend
  - documentation
  - ci
evidence:
  - RULE.md
  - AGENTS.md
  - docs/delivery/change-classification.md
  - docs/delivery/contract-first-delivery.md
  - docs/quality/engineering-quality-standard.md
  - docs/quality/testing-strategy.md
  - docs/quality/security-quality-standard.md
  - docs/quality/accessibility-standard.md
  - docs/quality/performance-and-scalability.md
  - docs/governance/documentation-lifecycle.md
  - .github/workflows/be-ci.yml
  - .github/workflows/fe-ci.yml
review_on:
  - definition-of-done-change
  - required-proof-change
  - release-certification-change
  - ci-required-gate-change
  - delivery-policy-change
---

# Definition of Done

> **A change is Done when the intended behavior exists in the correct owner and every contract, migration, security, quality, consumer, operational, and documentation obligation implied by that change is proven on the exact revision.**
>
> “Code complete”, “PR approved”, “tests passed locally”, and “deployed once” are not equivalent to Done.

This document owns repository-wide completion criteria.

It does not prescribe every implementation task.

`change-classification.md` decides which obligations apply.

Quality documents decide the proof standard.

Release/migration/operations documents own specialized procedures.

---

# 1. Done is change-specific

A local implementation change may require:

```text
implementation
focused tests
architecture/type/lint/build
exact CI
```

A breaking schema/security change may additionally require:

```text
contract migration
backfill
negative security evidence
mixed-version rollout
observability
completion proof
old-path cleanup
```

---

# 2. DEL-DONE-001 — Definition of Done follows classified impact

Do not apply the smallest checklist to a large-risk change.

Do not impose migration/ADR ceremony on a truly local C0 change.

---

# 3. Completion surfaces

As applicable, Done spans:

```text
semantics
ownership/architecture
implementation
data/schema
security/tenant
contracts/generated artifacts
frontend/mobile/realtime
tests/quality
performance/accessibility
configuration/rollout
observability/recovery
documentation
CI
cleanup
```

---

# 4. DEL-DONE-002 — All applicable surfaces complete together

A feature is not Done if one required surface is intentionally left as:

```text
TODO
manual production step with no runbook
stale generated artifact
disabled test/gate
future authorization
untracked migration
temporary flag with no owner
```

unless an explicit governed exception/transition exists.

---

# 5. Semantic completion

The implemented behavior matches the canonical product/system/context contract.

If behavior changed, the normative owner is updated.

---

# 6. DEL-DONE-003 — Source and canonical semantics agree

If intentional future-state docs precede source, the repository must classify that mismatch as an explicit transition.

Do not claim current conformance prematurely.

---

# 7. Ownership completion

Implementation sits in the correct bounded context/layer/package/project.

No new accidental reverse dependency or duplicate authority.

---

# 8. DEL-DONE-004 — Correct result in wrong owner is not Done

Examples:

```text
frontend enforces server-only invariant
Automation writes Work Management DB directly
Analytics becomes writable source
Governance owns Workspace membership lifecycle
```

Functional behavior alone does not justify boundary violation.

---

# 9. Product behavior

User-visible states include as applicable:

```text
loading
empty
success
validation error
permission denied
conflict
pending
retry
terminal failure
unknown provider outcome
read-only
```

---

# 10. DEL-DONE-005 — Failure state is implemented, not only happy path

A feature that succeeds but has undefined failure/recovery behavior is incomplete.

---

# 11. Validation

Required validation exists at the correct authoritative boundary.

Frontend validation is UX, not substitute for server/Domain rules.

---

# 12. DEL-DONE-006 — Rejected input leaves state/effects consistent

Where contract requires atomic rejection, evidence shows no partial mutation or success side effect.

---

# 13. No-op

Semantic no-op behavior is implemented/tested when meaningful to events/version/history.

---

# 14. Data/schema completion

For C4 changes:

```text
migration exists
old data compatibility proven
indexes/constraints/RLS correct
backfill complete or governed staged
mixed-version path defined
```

---

# 15. DEL-DONE-007 — “Migration runs on empty DB” is not sufficient

Existing-data upgrade path is proven when production data shape matters.

---

# 16. Backfill completion

Backfill is Done only when:

```text
bounded/idempotent code exists
checkpoint/retry works
tenant scope is correct
completion metric/query exists
required production execution completed or rollout phase explicitly remains open
```

---

# 17. DEL-DONE-008 — Change is not globally Done while mandatory migration phase remains incomplete

A PR/implementation stage can be complete as a rollout stage.

The overall change is not fully Done until its required migration/cleanup completion criteria are met.

---

# 18. RLS/tenant completion

Tenant-sensitive data changes include:

```text
policy
query filter
background context
cache
search
analytics
realtime
```

as applicable.

---

# 19. DEL-DONE-009 — Tenant isolation has negative evidence

At least relevant wrong-tenant/cross-tenant scenarios are proven.

Happy path is not enough.

---

# 20. Authorization completion

Protected reads and writes use authoritative server-side authorization.

Stored roles/policies/cache/realtime effects are migrated/invalidate as needed.

---

# 21. DEL-DONE-010 — Authorization TODO means not Done

Do not merge product capability with:

```text
TODO authorize later
frontend-only guard
temporary admin bypass
```

without explicit exception.

---

# 22. Secret/security completion

For security-sensitive change:

- threat boundary reviewed;
- secret flow safe;
- negative/adversarial tests;
- fail-closed errors/config;
- audit where required.

---

# 23. DEL-DONE-011 — Security exception is explicit, bounded, and visible

Silent suppression/bypass is never completion.

---

# 24. Contract completion

For cross-boundary change:

```text
contract source updated
compatibility classified
producer implemented
generated artifacts synchronized
consumers migrated/compatible
drift gates green
```

---

# 25. DEL-DONE-012 — Generated contract drift means not Done

A backend API change with stale generated frontend/OpenAPI artifact is incomplete.

---

# 26. Breaking contract completion

Breaking change requires its explicit compatibility/migration window.

The new implementation alone is not Done while supported consumers still require old contract.

---

# 27. DEL-DONE-013 — Old contract removal and new contract introduction are independently proven

The overall migration is Done only after old-path removal criteria are met.

---

# 28. Mobile completion

If mobile is affected:

```text
supported-version compatibility
native-safe dependency
mobile tests/build
mobile UX/accessibility
```

must be addressed.

---

# 29. DEL-DONE-014 — Latest mobile source is not proof for installed old clients

Backend contract remains compatible with supported mobile floor until release policy permits removal.

---

# 30. Frontend state completion

Server-authoritative state, query keys, optimistic updates, scope transitions, and error rollback remain coherent.

---

# 31. DEL-DONE-015 — Optimistic UI reconciles after rejection/conflict

Client must not remain in impossible state after server denial, stale write, or realtime gap.

---

# 32. Realtime completion

If realtime affected:

- duplicate/out-of-order behavior;
- reconnect/gap;
- scope switch;
- permission revocation;
- authoritative refetch

are handled.

---

# 33. DEL-DONE-016 — Realtime feature is recoverable without perfect delivery

A missed socket event cannot permanently corrupt durable user-visible state.

---

# 34. Automation/message completion

For durable async:

```text
logical identity
idempotency
ordering where needed
retry
poison/dead-letter
unknown outcome
observability
```

are defined/proven.

---

# 35. DEL-DONE-017 — Retry without idempotency/reconciliation is incomplete

Especially for provider/external creates and user-visible side effects.

---

# 36. Provider/integration completion

Provider-facing feature includes:

- credential/Connection boundary;
- webhook verification;
- rate limits;
- mapping;
- retry/unknown outcome;
- disconnect/cleanup;
- safe logs.

---

# 37. DEL-DONE-018 — Provider happy path alone is not Done

At least relevant replay, rate-limit, invalid payload, revoke, duplicate, or unknown-result behavior is proven.

---

# 38. Billing completion

Commercial changes include:

- canonical state;
- provider mapping;
- idempotency;
- entitlement;
- usage;
- downgrade/grace;
- retention/authorization

as applicable.

---

# 39. Testing completion

Every changed contract has primary proof at the nearest trustworthy owner.

Critical composition has higher-level evidence when needed.

---

# 40. DEL-DONE-019 — Focused tests are reported as focused tests

Do not write:

```text
all tests passed
```

when only one project/package/file was run.

---

# 41. Required suite execution

Critical gates execute meaningful non-zero work.

CI filters/report verifiers fail if intended tests are not discovered.

---

# 42. DEL-DONE-020 — Green empty suite is not Done

A test command returning exit code 0 with zero intended tests provides no evidence.

---

# 43. Architecture test completion

Dependency/boundary changes update:

- source architecture;
- manifest/rules;
- architecture tests;
- docs/ADR

as applicable.

---

# 44. DEL-DONE-021 — Architecture gate is not disabled to complete delivery

Either conform, change the canonical architecture through governance, or use explicit bounded exception.

---

# 45. Accessibility completion

User-facing change satisfies accessibility contract.

For complex interactions, automated scan alone may not be sufficient.

---

# 46. DEL-DONE-022 — Critical inaccessible workflow is not Done

Examples:

- drag-only core action;
- unlabeled form control;
- inaccessible modal trap;
- payment/security flow unusable by keyboard/assistive tech.

---

# 47. Performance completion

Performance-sensitive change has representative evidence proportional to risk.

No obvious unbounded work/fan-out/query regression.

---

# 48. DEL-DONE-023 — Performance claim states workload assumptions

“Faster” without representative data/concurrency/method is not completion evidence.

---

# 49. Observability completion

New failure mode/process has enough:

```text
logs
metrics
correlation
status
alert/runbook route where required
```

to diagnose/recover.

---

# 50. DEL-DONE-024 — Durable async/external failure must be diagnosable

A user/provider job that can fail after source commit cannot disappear silently into a queue.

---

# 51. Configuration completion

New config defines:

```text
owner
type
default
validation
environments
failure mode
secret handling
```

---

# 52. DEL-DONE-025 — Manual undocumented environment configuration means not Done

Required configuration must be represented in deployment/config contracts and documentation as appropriate.

---

# 53. Feature flag completion

A rollout flag includes:

```text
owner
purpose
scope/cohort
safe default
observability/decision criteria
security parity
removal condition
```

---

# 54. DEL-DONE-026 — Feature flag is not Done until lifecycle is defined

Introducing a flag without removal/ownership creates permanent hidden architecture debt.

---

# 55. Rollout completion

A material rollout defines valid deployment stages.

Each stage preserves:

- compatibility;
- tenant/security;
- data ownership;
- recovery.

---

# 56. DEL-DONE-027 — Production stage validity is part of Done

A PR is incomplete if its only safe state assumes simultaneous deployment that production cannot guarantee.

---

# 57. Rollback/recovery

Material change states:

```text
binary rollback?
schema rollback?
data rollback?
external-effect rollback?
forward-fix?
```

---

# 58. DEL-DONE-028 — “Can rollback” requires evidence

Irreversible data/provider effects must have forward-recovery/compensation instead of generic rollback claim.

---

# 59. Documentation completion

Canonical docs update when semantics/architecture/quality/operations change.

Generated facts regenerate from producers.

---

# 60. DEL-DONE-029 — Documentation is not post-merge cleanup for semantic change

If a change alters canonical behavior, the normative owner is updated in the same governed change/stage unless transition explicitly says otherwise.

---

# 61. ADR completion

Consequential durable architecture decisions have required ADR.

Routine implementation within accepted architecture does not need an ADR.

---

# 62. DEL-DONE-030 — Missing required decision record means not Done

Do not encode a new architecture solely in source and ask future maintainers to infer it.

---

# 63. Exception completion

Temporary violations are documented through exception policy with:

- exact rule;
- scope;
- owner;
- expiry/review/removal.

---

# 64. DEL-DONE-031 — Exception is not invisible debt

A TODO/comment alone is not a governed exception.

---

# 65. Cleanup completion

Temporary compatibility code, migration columns, old APIs/events, feature flags, and transitional adapters are removed only after completion proof.

---

# 66. DEL-DONE-032 — Transitional path has explicit end state

Overall change remains in transition until required cleanup criteria are satisfied.

---

# 67. Dead code

Old unused implementation is deleted after migration proof.

Do not keep duplicate paths indefinitely “just in case”.

---

# 68. DEL-DONE-033 — Git is history; dead commented code is not

Delete obsolete code after safe removal.

Keep rationale in ADR/docs/commit history, not commented implementation.

---

# 69. CI completion

Required CI is green on exact revision.

Backend/frontend/docs required gates relevant to the change must succeed.

---

# 70. DEL-DONE-034 — Exact SHA is completion evidence

A previously green commit does not certify later modifications.

---

# 71. Branch status

Branch “green” is shorthand only when it refers to the current exact SHA and required check set.

---

# 72. Local evidence

Local focused runs accelerate work.

They do not replace required CI/integration environment evidence.

---

# 73. DEL-DONE-035 — Local environment cannot certify properties it does not reproduce

Examples:

```text
PostgreSQL RLS
production build
mobile bundle
container graph
provider protocol fixture
CI non-zero gate
```

---

# 74. Evidence report

Completed change records:

```text
change classification
risk modifiers
implemented behavior
tests/gates run
migrations/backfills
contract/generated changes
security/accessibility/performance evidence
rollout/recovery
docs/ADR/exception
intentionally not applicable items
exact CI revision
```

---

# 75. DEL-DONE-036 — Evidence report distinguishes verified from assumed

Do not list an item as passed if it was not run/proven.

Use:

```text
verified
not applicable + reason
pending rollout phase
```

---

# 76. “Not applicable”

Not-applicable is valid when justified by change class.

Example C0 private helper:

```text
DB migration: not applicable — no persistence contract change
```

Do not fill every section with fake evidence.

---

# 77. DEL-DONE-037 — Not-applicable requires a reason for material checklists

This prevents accidental omissions from being mistaken for deliberate scope decisions.

---

# 78. Partial implementation

A feature can be deliberately staged.

A stage can be Done while the entire feature is not.

State the scope explicitly.

---

# 79. DEL-DONE-038 — Stage Done and Feature Done are distinct

Example:

```text
Stage A Done:
compatible schema expansion deployed

Overall migration:
not Done until backfill + old column removal
```

---

# 80. Prototype/spike

A spike is not production Done unless promoted through normal quality/delivery requirements.

Prototype code can be discarded.

---

# 81. DEL-DONE-039 — Prototype success does not waive production obligations

A working demo does not prove:

- tenancy;
- authorization;
- migration;
- retries;
- accessibility;
- operations.

---

# 82. Manual DB change

Production-required manual DB mutation with no migration/runbook/governance is not completion.

---

# 83. DEL-DONE-040 — Production state change is reproducible

Schema/config/deployment transformation should be represented in versioned automation/migration/runbook according to its owner.

---

# 84. Manual cache flush/restart

If rollout requires one-time operational action, document/automate it in release/runbook.

Do not leave tribal knowledge.

---

# 85. Data repair

A one-time repair script must be:

- reviewed;
- bounded;
- idempotent where feasible;
- tenant-safe;
- observable;
- archived/removed according to policy.

---

# 86. DEL-DONE-041 — One-time repair is governed production code

It is not a shell snippet pasted into production without evidence.

---

# 87. Security remediation

If a change fixes a vulnerability, Done includes:

- vulnerable path removed;
- regression evidence;
- secret rotation/data remediation if compromise requires;
- dependency/gate update if recurring.

---

# 88. Accessibility remediation

Done includes regression test in component/flow where deterministic.

Do not rely only on a comment saying issue fixed.

---

# 89. Performance remediation

Done includes proof against the actual workload/property that regressed.

Do not optimize unrelated micro-benchmark.

---

# 90. Incident follow-up

If incident reveals missing systemic guard, Done may require:

- code fix;
- regression;
- observability;
- runbook;
- architecture/quality rule update.

---

# 91. DEL-DONE-042 — Fix root protective property, not only observed symptom

Avoid one-off patch when the same failure class remains possible elsewhere.

---

# 92. Current CI alignment

Current backend delivery evidence includes:

```text
quality/security
architecture
core tests
Platform
API/OpenAPI
integration
Docker
final gate
```

Current frontend delivery evidence includes:

```text
codegen/architecture/taxonomy
type/lint/format
guarded test categories
UI accessibility/visual
builds
production E2E
final gate
```

Exact topology can evolve while the protected completion properties remain.

---

# 93. C0 Done

Typical C0 completion:

```text
[ ] behavior preserved
[ ] implementation owner unchanged/correct
[ ] focused tests
[ ] affected architecture/type/lint/build
[ ] no contract/data/security drift
[ ] exact required CI
```

---

# 94. C1 Done

Add:

```text
[ ] additive compatibility proven
[ ] producer contract source
[ ] generated artifact
[ ] old consumer remains valid
[ ] new consumer behavior tested
```

---

# 95. C2 Done

Add:

```text
[ ] semantic behavior documented
[ ] affected consumer tests
[ ] error/timing/default changes handled
[ ] migration of expectations/config if needed
```

---

# 96. C3 Done

Add:

```text
[ ] explicit version/compatibility strategy
[ ] every supported consumer migrated
[ ] mobile/backlog handled
[ ] old contract removal proof
```

---

# 97. C4 Done

Add:

```text
[ ] migration
[ ] existing-data proof
[ ] RLS/index/constraint
[ ] backfill/checkpoint
[ ] mixed-version DB compatibility
[ ] completion verification
[ ] cleanup/contraction
```

---

# 98. C5 Done

Add:

```text
[ ] canonical architecture updated
[ ] ADR if consequential
[ ] architecture manifests/tests
[ ] dependency/public boundaries
[ ] migration of old ownership
```

---

# 99. C6 Done

Add:

```text
[ ] threat review
[ ] negative authz/tenant evidence
[ ] secrets/config
[ ] cache/realtime/background scope
[ ] revocation/audit
```

---

# 100. C7 Done

Add:

```text
[ ] config/deployment contract
[ ] environment compatibility
[ ] startup validation
[ ] rollout/recovery
[ ] CI/build/deploy evidence
```

---

# 101. C8 Done

Add:

```text
[ ] explicit irreversible impact
[ ] approval/authorization
[ ] backup/export/retention policy
[ ] rollback impossibility stated
[ ] forward recovery/compensation
[ ] audit
```

---

# 102. Pull request review checklist

```text
[ ] classification matches real impact
[ ] semantic owner correct
[ ] no duplicate authority
[ ] consumers inventoried
[ ] failure paths implemented
[ ] security/tenant checked
[ ] data/contracts/generated synchronized
[ ] tests meaningful/non-zero
[ ] rollout/recovery sufficient
[ ] docs/ADR/exception sufficient
```

---

# 103. Pre-merge checklist

```text
[ ] no unresolved TODO for required correctness
[ ] no disabled/broadened gate without governance
[ ] no stale generated output
[ ] no forgotten migration
[ ] no undocumented flag/config
[ ] no unsupported deep import
[ ] no accidental secret
[ ] relevant CI green on current SHA
```

---

# 104. Post-deploy completion

Some material changes require post-deploy proof:

```text
backfill complete
error rate healthy
old path unused
consumer migration complete
flag cohort complete
provider sync healthy
```

The implementation PR can be complete as a stage while the overall migration remains open.

---

# 105. DEL-DONE-043 — Production completion can require evidence after deployment

Do not mark a staged migration fully Done before its objective completion condition exists.

---

# 106. Cleanup issue/task

If cleanup is deferred by design, it must be a governed tracked transition with:

- owner;
- trigger;
- completion condition.

Do not leave “cleanup later” unowned.

---

# 107. DEL-DONE-044 — Cleanup debt is explicit and bounded

A compatibility path without a removal owner is permanent architecture by accident.

---

# 108. Definition of Done checklist

```text
[ ] change class + modifiers
[ ] semantics
[ ] correct owner
[ ] success/failure/no-op
[ ] auth/tenant
[ ] schema/data/migration
[ ] contracts/generated/consumers
[ ] client/realtime/mobile
[ ] tests/architecture/integration
[ ] security
[ ] accessibility
[ ] performance
[ ] configuration/flag
[ ] observability/recovery
[ ] docs/ADR/exception
[ ] cleanup/removal condition
[ ] exact CI SHA
[ ] evidence report
```

---

# 109. Stop conditions

A change is not Done if:

- authorization is deferred;
- tenant isolation lacks required negative proof;
- migration works only from empty DB;
- required backfill has no completion proof;
- generated API/client contract is stale;
- old supported consumer is knowingly broken;
- required suite ran zero meaningful tests;
- architecture gate was disabled instead of resolved;
- feature flag has no owner/removal condition;
- external retry can duplicate side effect with no reconciliation;
- inaccessible critical interaction remains;
- performance-sensitive hot path is knowingly unbounded;
- required production config/manual step is undocumented;
- exact current revision has not passed required CI;
- evidence report claims checks that were not run.

---

# 110. Related canonical owners

```text
docs/delivery/change-classification.md
docs/delivery/contract-first-delivery.md
docs/delivery/release-and-rollout.md
docs/delivery/migration-policy.md
docs/quality/engineering-quality-standard.md
docs/quality/testing-strategy.md
docs/quality/security-quality-standard.md
docs/quality/accessibility-standard.md
docs/quality/performance-and-scalability.md
docs/governance/decision-and-exception-policy.md
```

---

# 111. Final Definition of Done rule

Before calling a change Done, answer:

```text
What classified behavior was completed?
Does source match the canonical owner?
Are data/contracts/generated consumers synchronized?
Are security/tenant/failure paths proven?
Are client/realtime/mobile consequences correct?
Were performance/accessibility/operations obligations handled?
What migration/rollout phase remains?
What objective proof permits cleanup/removal?
Which exact tests/gates ran?
Which exact revision is green?
What was intentionally not applicable, and why?
```

The target is:

> **Done as a provable repository and production state—not a subjective moment when code compiles, a PR is approved, or the happy path appears to work.**
