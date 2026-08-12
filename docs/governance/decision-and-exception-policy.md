---
document_id: DOC-DECISION-EXCEPTION
document_type: governance
status: active
owner: documentation-governance
applies_to:
  - repository
evidence:
  - RULE.md
  - AGENTS.md
  - docs/governance/documentation-authority.md
  - docs/governance/documentation-lifecycle.md
  - docs/governance/topic-authority-map.md
  - backend/docs/decisions/
  - frontend/docs/decisions/
review_on:
  - adr-policy-change
  - architecture-exception-policy-change
  - decision-registry-change
  - repository-rule-change
  - canonical-topic-owner-change
---

# Decision and Exception Policy

> **This document governs consequential architectural/product decisions and temporary deviations from approved Notrelix contracts.**
>
> A decision changes or confirms architecture.
>
> An exception permits a bounded temporary deviation.
>
> They are not interchangeable.

This file defines:

- when an ADR is required;
- when an ADR is unnecessary;
- how ADR scope is selected;
- required ADR structure;
- decision statuses;
- supersession rules;
- same-change synchronization requirements;
- what an architecture exception is;
- where exceptions are recorded;
- exception admission, verification, expiry, removal, and escalation.

Documentation authority is defined by:

[`documentation-authority.md`](documentation-authority.md)

Documentation lifecycle is defined by:

[`documentation-lifecycle.md`](documentation-lifecycle.md)

Canonical topic ownership is defined by:

[`topic-authority-map.md`](topic-authority-map.md)

---

# 1. Core model

Notrelix distinguishes:

```text
Canonical contract
    What is approved now?

ADR
    Why was a consequential choice made?

Exception
    Why may a specific current scope temporarily violate the canonical contract?

Source/tests
    What currently exists and what is proven?
```

The system MUST never rely on an ADR or exception as the only current architecture handbook.

---

# 2. Decision principles

A consequential decision should be explicit when its consequences survive beyond one local implementation.

Typical characteristics:

- many future changes depend on it;
- changing it later has material migration cost;
- it changes semantic ownership;
- it changes a protected architecture boundary;
- it changes security/tenant behavior;
- it changes consistency or public compatibility;
- it creates a durable technology/runtime dependency;
- it resolves a contested architecture choice.

Routine implementation choices already determined by canonical architecture do not require ADRs.

---

# 3. ADR is not approval theater

Do not create ADRs merely because:

- a PR is large;
- a new class was added;
- a library has a configuration option;
- a feature needs a normal Domain/Application implementation;
- a standard architecture rule already determines the answer.

An ADR should capture a choice whose rationale will matter later.

Too many trivial ADRs hide important decisions.

Too few ADRs force future teams to reverse-engineer intent from source.

---

# 4. ADR-required decisions

An ADR is normally REQUIRED for a durable consequential change in any of the following categories.

## 4.1 Product ownership

Examples:

- create, merge, split, or retire a bounded context;
- move authoritative business ownership between contexts;
- redefine a core product concept across contexts;
- change a product-wide semantic invariant.

Use a system ADR unless the decision is truly confined to one implementation plane without product/system impact.

---

## 4.2 Repository/system architecture

Examples:

- change modular-monolith/service strategy;
- introduce a new cross-stack architectural mechanism;
- change cross-context consistency strategy;
- introduce a new system-wide contract/versioning model;
- change capability extraction criteria.

Use:

```text
docs/decisions/SYS-ADR-*.md
```

---

## 4.3 Backend architecture

Examples:

- add/remove a production project class;
- change project dependency direction;
- change Application pipeline boundary model;
- change transaction/SaveChanges ownership;
- change RLS/bootstrap architecture;
- change messaging identity/ordering/idempotency model;
- adopt a persistence/security mechanism with durable backend consequences.

Use:

```text
backend/docs/decisions/ADR-*.md
```

when the decision is backend-specific.

---

## 4.4 Frontend architecture

Examples:

- change host/framework split;
- change package-family/dependency architecture;
- change runtime-adapter model;
- change server-state ownership model;
- change package public-export strategy;
- change mobile/web safety model.

Use:

```text
frontend/docs/decisions/FE-ADR-*.md
```

when the decision is frontend-specific.

---

## 4.5 Security / tenancy

ADR is normally required for changes such as:

- changing the authoritative authorization model;
- changing tenant isolation architecture;
- changing security principal identity model;
- changing RLS role in defense;
- changing share/public-access security architecture;
- changing secret/key-management architecture.

Security bug fixes that restore already-approved behavior do not require new ADRs.

---

## 4.6 Public/persisted contract strategy

ADR is normally required when changing:

- API versioning/deprecation strategy;
- event compatibility/versioning model;
- persisted polymorphism/versioning strategy;
- generated-contract ownership model;
- durable provider synchronization conflict model.

A single additive endpoint/field following existing policy does not automatically require an ADR.

---

## 4.7 Durable technology choice

ADR is normally required when a technology choice:

- becomes foundational;
- has high migration cost;
- changes operating model;
- changes deployment architecture;
- changes data ownership/security assumptions;
- will be reused across many capabilities.

Do not ADR every library addition.

---

# 5. ADR-not-required changes

ADR is normally NOT required for:

- local refactor preserving all contracts;
- normal use-case implementation under existing architecture;
- bug fix restoring documented behavior;
- additive endpoint following existing API policy;
- new Domain entity/value object under an already-defined aggregate model;
- new frontend component following existing ownership;
- package upgrade with no material architecture effect;
- test additions;
- performance implementation that does not change public/semantic contract;
- documentation clarification with no semantic change.

If uncertainty remains, classify the decision impact rather than defaulting to an ADR.

---

# 6. Decision scope selection

Choose the narrowest decision registry that fully owns the consequences.

```text
System/repository
→ docs/decisions/
→ SYS-ADR-*

Backend-specific
→ backend/docs/decisions/
→ ADR-*

Frontend-specific
→ frontend/docs/decisions/
→ FE-ADR-*
```

Do not choose scope based on where the implementation diff is largest.

Choose scope based on the decision's semantic blast radius.

---

# 7. System ADR admission

Use a system ADR when the decision materially affects two or more of:

```text
product semantics
backend architecture
frontend architecture
public contracts
operations/deployment
repository governance
```

or changes a cross-stack contract owned by `docs/architecture/`.

---

# 8. Backend ADR admission

Use a backend ADR when:

- consequences are contained within backend architecture;
- system/product semantics remain unchanged;
- frontend only consumes the resulting stable contract rather than participating in the architectural choice.

If the backend decision changes a cross-stack contract strategy, elevate to system scope.

---

# 9. Frontend ADR admission

Use a frontend ADR when:

- consequences are contained within frontend architecture;
- backend/product semantics remain unchanged;
- the decision is about host/package/runtime/state/UI architecture.

If the frontend decision changes the system contract or product semantics, use system/product governance as well.

---

# 10. One decision may require one ADR, not three copies

Do not create:

```text
SYS-ADR-X
ADR-X
FE-ADR-X
```

all documenting the same decision.

Use one ADR at the scope that owns the decision.

Project canonical docs then document local consequences and reference the decision.

A separate project ADR is justified only if there is a distinct additional project-specific choice.

---

# 11. ADR identifiers

Required namespaces:

```text
SYS-ADR-001
ADR-001
FE-ADR-001
```

IDs are unique within their registry namespace.

IDs are never reused after deletion/rejection/supersession.

Do not renumber historical ADRs to remove gaps.

---

# 12. ADR filename

Preferred:

```text
SYS-ADR-001-short-decision-name.md
ADR-005-short-decision-name.md
FE-ADR-006-short-decision-name.md
```

The ID is stable.

The descriptive slug may be concise.

Do not include:

```text
final
v2
new
latest
approved
```

in the filename as lifecycle management.

---

# 13. ADR decision statuses

Supported decision statuses:

```text
Proposed
Accepted
Superseded
Rejected
Deprecated
```

These are ADR decision statuses.

They are not the same as documentation lifecycle metadata.

---

# 14. Proposed

Meaning:

> A decision is under review and is not current authority.

A Proposed ADR MUST NOT be used to silently override active canonical architecture.

Implementation exploration may occur where explicitly scoped.

Production architecture should not depend on the proposal before approval unless an explicit experimental/exception process allows it.

---

# 15. Accepted

Meaning:

> The decision has been approved and all affected canonical contracts must reflect it.

An Accepted ADR is not sufficient on its own.

Acceptance requires synchronization with the current architecture in the same controlled delivery transaction.

---

# 16. Superseded

Meaning:

> A later accepted decision replaced this decision.

A superseded ADR remains historical evidence.

It MUST point to:

```text
Superseded By
```

The new ADR SHOULD point back through:

```text
Supersedes
```

Do not rewrite the old ADR to make its original decision match the new architecture.

---

# 17. Rejected

Meaning:

> The proposal was considered and intentionally not adopted.

Rejected ADRs may be useful when the rejected option is likely to recur.

They MUST NOT be treated as current architecture.

---

# 18. Deprecated

Meaning:

> The decision remains historically relevant/currently observable, but its chosen mechanism is intentionally being phased out.

Use this status sparingly.

If a replacement decision is already accepted, prefer:

```text
Superseded
```

`Deprecated` is useful during a deliberate removal window where the original decision has not yet been replaced by one singular new decision.

---

# 19. Required ADR structure

Every new ADR MUST include:

```text
ID
Status
Date
Owners
Context
Decision
Alternatives
Consequences
Compatibility / Migration
Evidence
Supersedes
Superseded By
```

Optional sections MAY include:

```text
Security / Tenant Impact
Operational Impact
Rollout
Open Questions
Related Rules
Related Canonical Docs
```

when materially useful.

Do not create empty filler sections for irrelevant concerns.

---

# 20. `ID`

The ADR ID MUST match:

- filename prefix;
- decision registry;
- ADR index.

Example:

```text
backend/docs/decisions/ADR-005-message-consumer-identity.md

ID:
ADR-005
```

---

# 21. `Status`

Use only the supported decision statuses.

Status changes require a real decision lifecycle event.

Do not set `Accepted` merely because implementation exists.

Accidental implementation does not retroactively approve architecture.

---

# 22. `Date`

Use the date of the decision state being recorded.

When status later changes, preserve the original decision date and record the status-change/supersession relationship as appropriate.

Do not use date as architecture version.

---

# 23. `Owners`

Owners identify the semantic reviewers/maintainers responsible for the decision.

Prefer team/architecture ownership labels over personal names when possible.

Examples:

```text
system-architecture
backend-architecture
frontend-architecture
security-architecture
product-work-management
```

---

# 24. `Context`

Context describes:

- the problem;
- forces/constraints;
- current state relevant to the choice;
- why a decision is needed.

Do not write the decision into Context.

Do not use Context as a full current architecture handbook.

---

# 25. `Decision`

Decision states exactly what was chosen.

It should be precise enough for future reviewers to distinguish it from rejected alternatives.

Do not encode implementation trivia unrelated to the durable choice.

---

# 26. `Alternatives`

Record credible alternatives actually considered.

For each important alternative, state why it was not selected.

Avoid fake alternatives added only to make the ADR appear thorough.

---

# 27. `Consequences`

Record both:

```text
benefits
costs / constraints / risks
```

A decision with only advantages documented is incomplete.

Consequences should include future architectural constraints when relevant.

---

# 28. `Compatibility / Migration`

State:

- whether current clients/data/source are compatible;
- whether staged migration is needed;
- what may coexist;
- removal/cleanup conditions.

Use:

```text
docs/delivery/change-impact-and-migration.md
```

for the full migration policy.

---

# 29. `Evidence`

Reference the strongest evidence that proves the decision is implemented/protected.

Examples:

```text
architecture tests
integration tests
project/package manifests
OpenAPI/contracts
migrations
generated checks
CI jobs
```

An ADR is rationale.

Evidence proves implementation.

---

# 30. `Supersedes` and `Superseded By`

Use explicit values:

```text
None
```

when no relationship exists.

Do not leave ambiguity.

A supersession chain should be mechanically traceable.

---

# 31. ADR template

The reusable template belongs in:

```text
docs/templates/adr-template.md
```

The template MUST implement this policy.

The template does not own the policy.

---

# 32. ADR registry indexes

Current decision registries:

```text
docs/decisions/README.md
backend/docs/decisions/README.md
frontend/docs/decisions/README.md
```

Each index MUST list its ADRs and their current decision status.

The index must not redefine current architecture.

---

# 33. Existing ADR normalization

Existing backend/frontend ADRs may predate this schema.

Do not rewrite their historical decisions merely to make formatting uniform.

During documentation migration:

- preserve original decision content;
- add missing structural fields only when meaning can be recovered safely;
- do not invent alternatives/rationale that were never recorded;
- mark unknown data explicitly rather than fabricating it;
- keep status/supersession truthful.

New ADRs MUST follow the full schema.

---

# 34. Accepted ADR synchronization rule

When an ADR changes current architecture, update in the same delivery transaction:

1. canonical topic owner;
2. affected root invariant/product constitution when applicable;
3. affected backend/frontend/system architecture docs;
4. source/config/manifests;
5. tests/architecture gates;
6. public/generated contracts;
7. migrations/compatibility;
8. task/topic routers when ownership changes;
9. decision registry index;
10. generated documentation indices.

An ADR MUST NOT be Accepted while canonical documentation deliberately describes the old architecture without an explicit migration/transition contract.

---

# 35. Decision implementation can be staged

Large changes may require staged implementation.

Accepted ADR may describe a target while old/new coexist only if:

- transition is explicitly defined;
- new-code rule is clear;
- compatibility is clear;
- source debt is not disguised;
- completion/removal conditions exist;
- active exceptions are recorded where actual code temporarily violates the current rule.

Use `CONTEXT.md` for current transition facts.

Use delivery migration docs for rollout policy.

---

# 36. Proposed ADR and implementation experiments

A proposed decision MAY have a proof-of-concept.

Experimental code MUST NOT become production precedent without:

- explicit scope;
- non-production/temporary status;
- cleanup path;
- approval before architecture adoption.

If production must temporarily carry the deviation, use an exception.

---

# 37. Decision reversal

Do not edit an Accepted ADR to reverse its decision.

Create a new ADR.

New ADR:

```text
Supersedes:
<old ID>
```

Old ADR:

```text
Status:
Superseded

Superseded By:
<new ID>
```

Update canonical architecture to the new decision.

---

# 38. Decision clarification

A typo/editorial clarification that does not change the historical meaning MAY edit the ADR in place.

A clarification that changes what the decision means is a new decision.

Use Git history for editorial traceability.

---

# 39. Decision evidence can evolve

Evidence references MAY be updated when:

- tests move;
- source paths move;
- architecture checks improve;
- generated producer changes without changing decision meaning.

Do not alter historical decision/rationale while updating evidence.

---

# 40. Architecture exception definition

An architecture exception is:

> **Explicit temporary permission for a defined scope to violate a specific active canonical rule or architecture contract.**

It is not:

- a second architecture;
- a TODO comment;
- an undocumented convention;
- an ADR replacement;
- a generic technical-debt label.

---

# 41. Exception admission principle

No architecture exception exists unless it is explicitly approved and recorded according to this policy.

A violation with no approved exception is:

```text
SOURCE_DEBT
```

or a defect—not an implicit exception.

---

# 42. When an exception is appropriate

An exception MAY be appropriate when:

- compliant migration cannot be completed atomically;
- external dependency blocks compliance;
- compatibility window requires legacy behavior;
- production incident mitigation needs a bounded temporary deviation;
- tool/platform limitation makes immediate compliance infeasible;
- staged extraction/migration temporarily violates target dependency rules.

---

# 43. When an exception is not appropriate

Do not approve an exception because:

- compliant implementation is more work;
- a framework encourages another pattern;
- existing code already violates the rule;
- deadline pressure exists without a removal strategy;
- a team prefers another architecture;
- tests are inconvenient;
- “we will fix it later” is the only plan.

If the alternative is actually the desired permanent architecture, make a decision and change the canonical contract.

---

# 44. Exception ownership location

Active exception details MUST live with the canonical contract being violated.

Preferred pattern:

```text
canonical architecture document
└── Active Exceptions
    └── EX-<scope>-<id>
```

Examples:

```text
backend/docs/architecture/application-model.md
frontend/docs/architecture/dependency-boundaries.md
docs/architecture/contract-boundaries.md
```

This prevents a separate exception handbook from becoming a second architecture system.

---

# 45. No permanent `CURRENT-EXCEPTIONS.md` authority

Notrelix does not require a manually maintained repository-wide current-exception handbook.

Reasons:

- it separates exception context from the violated rule;
- it becomes stale;
- it creates another mandatory reading path;
- local exceptions are easier to normalize into precedent accidentally.

If cross-repository discoverability is needed, prefer a **generated exception index** derived from structured exception records rather than a second authored authority.

A generated exception index is optional and must be introduced through documentation-governance change if needed.

---

# 46. Exception identifiers

Use stable IDs:

```text
EX-SYS-001
EX-PROD-001
EX-BE-001
EX-FE-001
EX-DOC-001
```

A more specific readable suffix MAY be included:

```text
EX-BE-APP-EF-001
```

IDs must be unique repository-wide.

Resolved IDs are never reused.

---

# 47. Required exception record

Every active exception MUST include:

```text
ID
Status
Violated Rule / Canonical Contract
Scope
Reason
Risk
Compensating Controls
Owner
Approved By
Introduced
Review / Expiry Trigger
New Usage
Verification
Removal Plan
Removal Condition
Related Decision / Migration
```

If these cannot be filled meaningfully, the exception is not ready for approval.

---

# 48. Exception statuses

Supported exception statuses:

```text
Proposed
Active
Resolved
Expired
Rejected
```

These are exception lifecycle statuses.

They are not documentation lifecycle statuses.

---

# 49. Proposed exception

A Proposed exception is not permission to violate the architecture.

The violating change MUST NOT merge as compliant solely because an exception proposal exists.

---

# 50. Active exception

An Active exception authorizes only the exact recorded scope.

It does not authorize:

- new unrelated usage;
- wider package/project usage;
- new call sites;
- architectural replication.

Unless explicitly approved otherwise:

```text
New Usage:
Prohibited
```

is the default.

---

# 51. Resolved exception

When source becomes compliant:

- mark/remove the active exception block in the same change;
- preserve historical rationale through Git or ADR if materially useful;
- ensure gates no longer need exception allowance.

Do not keep resolved exception prose permanently in the canonical architecture file unless ongoing historical context is truly valuable.

---

# 52. Expired exception

An expired exception no longer grants permission.

If non-compliant source remains after expiry:

```text
merge/release blocker
```

until one of:

- compliance is restored;
- an explicit reviewed extension is approved;
- canonical architecture is intentionally changed.

Do not silently extend expiry dates.

---

# 53. Rejected exception

Rejected means the deviation was considered and not approved.

Rejected proposals need not remain in canonical docs.

Preserve meaningful rationale in PR/issue/decision history where appropriate.

---

# 54. `Violated Rule / Canonical Contract`

Reference the exact rule/topic.

Examples:

```text
NRX-002
BE-APP-...
FE-DEP-...
canonical section in application-model.md
```

Do not write:

```text
"architecture"
```

as the violated rule.

---

# 55. Exception `Scope`

Scope MUST be concrete.

Prefer:

```text
project
package
namespace
file
type
specific endpoint
specific legacy consumer
```

Avoid:

```text
backend
frontend
all integrations
temporary
```

unless that truly is the reviewed blast radius.

---

# 56. Exception `Reason`

Explain why the compliant path cannot be completed now.

Reason should identify a constraint, not merely a preference.

---

# 57. Exception `Risk`

State what can go wrong because the architecture is being violated.

Examples:

- dependency coupling;
- tenant leakage risk;
- duplicate side effect;
- migration lock-in;
- mobile-web contamination;
- stale cache;
- contract ambiguity.

Do not state only “low risk”.

Explain the risk.

---

# 58. Exception `Compensating Controls`

Controls reduce risk during the exception window.

Examples:

- architecture allow-list limited to one path;
- targeted test;
- runtime guard;
- read-only compatibility adapter;
- feature flag;
- monitoring/alert;
- explicit code owner review.

Compensating controls do not make the exception permanent architecture.

---

# 59. Exception `Owner`

Every Active exception needs a semantic owner responsible for removal/review.

No owner:

```text
no exception
```

---

# 60. Exception `Approved By`

Approval must match severity/scope.

At minimum it requires the owner of the violated canonical contract.

High-risk security/product/system exceptions require the relevant security/product/system owner as applicable.

---

# 61. Exception `Introduced`

Record the introduction date or change reference useful for review.

A commit/PR reference may be used as evidence.

Do not use introduction date as automatic expiry unless policy says so.

---

# 62. Exception review / expiry trigger

Prefer semantic triggers:

```text
migration phase complete
provider SDK upgrade
legacy consumer removed
package extraction complete
framework issue fixed
before next contract break
```

A calendar date MAY be used when risk requires a hard review deadline.

Best practice for material exceptions:

```text
semantic removal condition
+
calendar review backstop
```

---

# 63. Exception `New Usage`

Allowed values:

```text
Prohibited
Explicitly Approved Within Scope
```

Default:

```text
Prohibited
```

If new usage is allowed, exact boundaries must be recorded.

---

# 64. Exception `Verification`

Every material exception must have a proof that:

- the allowed scope does not spread;
- compensating controls still work;
- the exception remains bounded.

Prefer executable proof.

Examples:

```text
architecture test
dependency allow-list
integration test
CI check
static search/check
```

Manual review alone is acceptable only when automation is impractical and the rationale is explicit.

---

# 65. Exception `Removal Plan`

State concrete steps to restore compliance.

Avoid:

```text
refactor later
cleanup
tech debt
```

Use:

```text
move abstraction
migrate consumers
remove compatibility type
update manifest
remove exception allow-list
run affected gates
```

---

# 66. Exception `Removal Condition`

State the observable condition proving the exception is no longer needed.

Example:

```text
all consumers use generated v2 contract and v1 adapter is removed
```

This differs from the plan.

Plan = how.

Condition = when done.

---

# 67. Related decision / migration

Link:

- ADR if exception arises from staged architecture change;
- migration plan/issue if a staged rollout owns removal;
- product/contract change where relevant.

The exception itself should remain concise enough to stay with the canonical owner.

---

# 68. Exception example

```text
### EX-BE-APP-001 — Temporary dependency compatibility

Status:
Active

Violated Rule / Canonical Contract:
BE Application dependency contract

Scope:
<exact project/type/file>

Reason:
<external or migration constraint>

Risk:
<architectural risk>

Compensating Controls:
<test/gate/allow-list>

Owner:
backend-architecture

Approved By:
backend-architecture

Introduced:
<date/change>

Review / Expiry Trigger:
<semantic trigger + optional date>

New Usage:
Prohibited

Verification:
<command/test>

Removal Plan:
<steps>

Removal Condition:
<observable end state>

Related Decision / Migration:
<ADR/issue if applicable>
```

This example is structure only.

Do not copy placeholder values into a real exception.

---

# 69. Exception and architecture tests

Architecture tests SHOULD encode exception boundaries narrowly.

Bad:

```text
ignore Application dependency rule
```

Better:

```text
allow exact known dependency/type
and fail any additional usage
```

The gate should make spread harder.

---

# 70. Exception and CI

A required exception verification MUST run in CI when the risk is architecture/security/contract critical.

If the verification runs zero relevant work:

```text
exception proof is invalid
```

Apply `NRX-016`.

---

# 71. Exception and security

Security/tenant exceptions have a higher bar.

They must include:

- threat/risk statement;
- compensating control;
- explicit security owner approval;
- executable protection where possible;
- hard review/removal trigger.

Do not use exception policy to bypass tenant isolation casually.

---

# 72. Exception and public contracts

Compatibility adapters may require temporary exceptions.

Record:

- old/new consumer scope;
- compatibility window;
- version/deprecation relation;
- migration completion condition.

Do not remove compatibility while active consumers remain.

Do not keep it indefinitely after consumers migrate.

---

# 73. Exception and generated architecture

If the architecture manifest/generator supports allow-lists, exception allowances should reference the exception ID.

Example concept:

```text
temporaryAllowedDependency:
    exception: EX-FE-DEP-001
```

This makes the exception traceable.

Do not add anonymous allow-list entries.

---

# 74. Exception and current context

`CONTEXT.md` MAY summarize significant active exceptions when they materially affect repository reasoning.

The canonical exception record remains with the violated architecture owner.

CONTEXT must not become a second detailed exception registry.

---

# 75. Exception and topic authority

An exception does not change `topic-authority-map.md`.

The canonical owner remains the same.

If the deviation should become permanent:

```text
decision
→ canonical owner change/update
→ topic map update if ownership changes
→ exception resolved
```

---

# 76. Exception extensions

An extension is a new approval event.

Review:

- reason still valid;
- risk changed;
- controls still valid;
- removal progress;
- new expiry/review trigger.

Do not auto-renew.

Repeated extensions are a signal that the architecture may need a real decision or the migration is not being owned.

---

# 77. Maximum exception scope

Exceptions should be as narrow as possible.

A repository-wide exception to a core `NRX-*` rule is extraordinary.

For foundational/security/tenant invariants, prefer:

- explicit architecture change;
- staged migration with bounded adapters;
- stronger compensating controls.

---

# 78. No exception for unknown ownership

If the actual product/architecture owner is unresolved, do not create an exception against a guessed rule.

Classify:

```text
UNRESOLVED
```

and resolve ownership first.

---

# 79. Decision versus exception test

Ask:

```text
Do we want this behavior permanently?
```

If yes:

```text
Decision / contract change
```

If no, and compliant state is still the target:

```text
Exception
```

If unclear:

```text
UNRESOLVED
```

Do not use an exception to postpone deciding permanent architecture indefinitely.

---

# 80. Decision-versus-debt test

Existing violation:

```text
Was it explicitly approved?
```

No:

```text
SOURCE_DEBT / defect
```

Yes, temporary:

```text
Active exception
```

Yes, permanent:

```text
Canonical architecture should already reflect a decision
```

---

# 81. Same-transaction requirement for permanent decisions

A permanent architecture decision is not complete until:

```text
ADR
+
canonical docs
+
source
+
tests/gates
+
migration/compatibility
+
generated artifacts
+
routers/indexes when applicable
```

are coherent.

Do not merge an ADR-only “decision” and leave architecture ambiguous for future agents.

---

# 82. Same-transaction requirement for exceptions

An Active exception is not complete until:

```text
exception record
+
source deviation
+
bounded verification
+
compensating controls
+
removal ownership
```

are coherent.

An exception record added after the violating code merged is debt remediation, not evidence the original change was compliant.

---

# 83. Decision review checklist

```text
[ ] decision is consequential enough for ADR
[ ] correct registry scope selected
[ ] ID unique
[ ] status correct
[ ] context explains problem
[ ] decision precise
[ ] real alternatives recorded
[ ] consequences include costs
[ ] compatibility/migration analyzed
[ ] security/tenant impact analyzed when relevant
[ ] evidence identified
[ ] supersession links correct
[ ] canonical owners updated
[ ] tests/gates updated
[ ] registry index updated
```

---

# 84. Exception review checklist

```text
[ ] exact violated rule identified
[ ] scope narrow and concrete
[ ] reason is a real constraint
[ ] risk explicit
[ ] compensating controls concrete
[ ] owner exists
[ ] correct approver exists
[ ] introduced reference recorded
[ ] review/expiry trigger exists
[ ] new usage policy explicit
[ ] verification prevents spread
[ ] removal plan actionable
[ ] removal condition observable
[ ] migration/ADR linked if relevant
[ ] no permanent architecture is being disguised
```

---

# 85. Resolution checklist

When resolving an exception:

```text
[ ] source compliant
[ ] compatibility consumers migrated
[ ] temporary allow-list removed
[ ] exception-specific gates removed or normalized
[ ] canonical docs no longer need active exception block
[ ] CONTEXT summary updated if present
[ ] generated evidence regenerated
[ ] full architecture/security gates pass
```

---

# 86. Decision-policy governance

Changing this policy itself is a documentation/system governance change.

Review:

```text
RULE.md
AGENTS.md
documentation-authority.md
documentation-lifecycle.md
topic-authority-map.md
documentation-quality-gates.md
ADR templates
decision registries
CI/checkers
```

Do not introduce a new ADR/exception status locally.

---

# 87. Current ADR migration expectations

Existing decision records remain valid historical evidence even when their format predates this policy.

Migration priorities:

1. preserve decision content;
2. preserve IDs;
3. preserve status truth;
4. add registry status visibility;
5. add supersession links where known;
6. add missing evidence/related-doc links when safely recoverable;
7. do not invent lost rationale.

New ADRs use the complete schema from day one.

---

# 88. Final decision rule

Use an ADR when future engineers need to know:

> **Why did Notrelix choose this durable architecture instead of a credible alternative?**

Use an exception when future engineers need to know:

> **Why is this exact scope temporarily allowed to violate the architecture, how is the risk bounded, and what removes the deviation?**

Never use either mechanism to hide ambiguity.

The final state must remain:

```text
one canonical contract
+
explicit rationale
+
bounded temporary exceptions only where necessary
+
executable evidence
```
