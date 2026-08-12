---
document_id: DOC-AUTHORITY
document_type: governance
status: active
owner: documentation-governance
applies_to:
  - repository
evidence:
  - RULE.md
  - AGENTS.md
  - CONTEXT.md
  - CONTEXT-MAP.md
  - scripts/
  - backend/docs/
  - frontend/docs/
review_on:
  - documentation-authority-change
  - canonical-topic-owner-change
  - documentation-governance-change
  - repository-documentation-topology-change
---

# Documentation Authority

> **This document defines how documentation authority works in Notrelix.**
>
> It answers one question:
>
> **When several files, source artifacts, decisions, generated outputs, or scoped instructions discuss the same area, which artifact is allowed to define what?**

This file is normative for documentation ownership and authority.

It is not a backend architecture handbook.

It is not a frontend architecture handbook.

It is not a product-context specification.

It is the governance contract that prevents those documents from competing with each other.

---

# 1. Purpose

Notrelix is large enough that documentation failure can become architecture failure.

The dangerous failure mode is not simply “missing docs”.

The more dangerous failure modes are:

```text
two documents both claim to be canonical
a local README silently overrides a repository invariant
a stale code pattern becomes architecture precedent
an old ADR is treated as current handbook text
a generated inventory is manually edited
a roadmap/freeze plan remains active after implementation
a Coding Agent reads the nearest file and invents the missing authority
```

This governance model exists to prevent those failures.

The core rule is:

> **one topic → one canonical normative owner**

That rule does not mean one file contains the entire system.

It means each distinct decision surface has exactly one owner.

A cross-cutting change may update several owners because it crosses several distinct decision surfaces.

It must not copy the same normative definition into all of them.

---

# 2. Authority is not one flat precedence list

Documentation authority MUST NOT be modeled as:

```text
file A
> file B
> file C
> source
```

because different artifacts answer different questions.

Notrelix separates authority into four planes:

```text
Intent
Normative semantics
Decision history
Current evidence
Execution procedure
```

These planes interact.

They are not interchangeable.

---

# 3. Authority planes

## 3.1 Task intent

The explicit task defines:

```text
what outcome is requested
what scope is requested
what constraints the requester explicitly adds
```

Task intent does not automatically make the requested implementation compliant.

If the task explicitly requests changing an existing product or architecture contract, it initiates a **contract-change process**.

It does not silently make the old contract disappear.

Example:

```text
Task:
"Make Search a bounded context."

Meaning:
This is a request to evaluate/change product/system ownership.

Not:
Search is now automatically a bounded context before PRODUCT/system docs,
ADRs, migration impact, source, and tests are updated.
```

---

## 3.2 Normative semantics

Normative artifacts define intended current behavior.

They include:

```text
repository constitutions
canonical product-context documents
canonical system architecture documents
canonical backend architecture documents
canonical frontend architecture documents
repository standards/policies
active approved exceptions for their explicit scope
```

These artifacts answer:

> What is the approved system intended to do?

---

## 3.3 Decision history

ADRs record consequential decisions and rationale.

They answer:

> Why was this decision made?

ADRs do not replace the current canonical architecture document.

If an accepted ADR and the current canonical document disagree without a superseding decision, the repository is inconsistent.

The correct action is to investigate and resolve the inconsistency.

Do not silently choose whichever artifact is easier.

---

## 3.4 Current evidence

Current evidence includes:

```text
source
tests
project/package manifests
OpenAPI/contracts
migrations
generated inventories
CI configuration/results
CONTEXT files
```

These answer:

> What does the repository currently contain or do?

Current evidence can expose:

```text
correct implementation
documentation drift
transitional code
architecture debt
regression
partial migration
```

It is not automatic architectural precedent.

---

## 3.5 Execution procedure

Execution artifacts include:

```text
AGENTS.md
scoped AGENTS.md
.agents/skills/*/SKILL.md
provider compatibility routers such as CLAUDE.md
templates/checklists
```

These answer:

> How should the work be performed?

They MUST NOT redefine product or architecture semantics.

---

# 4. Repository authority planes

The repository uses these authority scopes.

```text
Root
    repository constitutions and execution/current-state routers

docs/
    cross-stack system
    product semantics
    governance
    repository quality
    delivery
    operations
    infrastructure
    system decisions
    templates
    repository generated evidence

backend/docs/
    backend implementation architecture
    backend operations
    backend decisions
    backend generated evidence

frontend/docs/
    frontend implementation architecture
    frontend decisions
    frontend generated evidence
```

The scope of a file is part of its authority.

A repository-level document MUST NOT absorb backend/frontend implementation detail simply because the detail affects the whole product indirectly.

---

# 5. Root authority

The root files have distinct roles.

| File | Authority |
|---|---|
| `README.md` | Orientation and entry point; summary only |
| `PRODUCT.md` | Repository-level product constitution |
| `DESIGN.md` | Repository-level product design constitution |
| `RULE.md` | Repository-wide invariant constitution |
| `AGENTS.md` | Repository-wide execution procedure |
| `CONTEXT.md` | Current repository facts; non-normative |
| `CONTEXT-MAP.md` | Task → canonical owner router |
| `CLAUDE.md` | Provider compatibility router only |

No root file should become a duplicate backend/frontend handbook.

---

# 6. Repository `docs/` authority

Repository `docs/` owns concerns that are genuinely repository-wide or cross-stack.

Target classes:

```text
docs/governance/
docs/architecture/
docs/product/
docs/quality/
docs/delivery/
docs/operations/
docs/infrastructure/
docs/decisions/
docs/templates/
docs/generated/
```

It MUST NOT contain a second backend implementation architecture tree.

It MUST NOT contain a second frontend implementation architecture tree.

Therefore these patterns are forbidden as canonical architecture:

```text
docs/backend/
docs/frontend/
docs/engineering/02-backend/
docs/engineering/03-frontend/
```

when backend/frontend canonical owners already exist under their project trees.

---

# 7. Backend documentation authority

Backend implementation architecture is owned under:

```text
backend/docs/
```

Primary current architecture owners include:

```text
backend/docs/architecture/backend-overview.md
backend/docs/architecture/domain-modeling.md
backend/docs/architecture/application-model.md
backend/docs/architecture/infrastructure-and-data.md
backend/docs/architecture/platform-and-messaging.md
backend/docs/architecture/api-and-contracts.md
backend/docs/architecture/security-tenancy-authorization.md
backend/docs/architecture/testing-and-quality-gates.md
```

Backend operations live under:

```text
backend/docs/operations/
```

Backend rationale lives under:

```text
backend/docs/decisions/
```

Exact current project inventory belongs to executable/generated evidence.

Repository-level docs may define cross-stack consequences.

They MUST route detailed backend implementation semantics back to these owners.

---

# 8. Frontend documentation authority

Frontend implementation architecture is owned under:

```text
frontend/docs/
```

Primary current architecture owners include:

```text
frontend/docs/architecture/frontend-overview.md
frontend/docs/architecture/dependency-boundaries.md
frontend/docs/architecture/hosts-composition-routing.md
frontend/docs/architecture/api-and-contracts.md
frontend/docs/architecture/state-query-mutations.md
frontend/docs/architecture/realtime.md
frontend/docs/architecture/ui-and-design-system.md
frontend/docs/architecture/testing-and-quality-gates.md
frontend/docs/architecture/architecture-change-policy.md
```

Frontend rationale lives under:

```text
frontend/docs/decisions/
```

Exact package dependency authority belongs to the executable architecture manifest and generated evidence.

Repository-level docs may define product/system requirements.

They MUST NOT duplicate exact frontend package rules.

---

# 9. Product authority

Repository-level product constitution:

```text
PRODUCT.md
```

Detailed business semantic owners:

```text
docs/product/contexts/*.md
```

Product docs own:

```text
meaning
vocabulary
ownership
lifecycle
business invariants
cross-context semantic responsibility
user-visible product behavior
```

Backend and frontend implement these semantics.

They MUST NOT independently redefine them.

Example:

```text
product/contexts/work-management.md
    owns the meaning of BoardGroup

frontend Kanban implementation
    may render grouping columns

frontend documentation
    cannot redefine BoardGroup as universal status
```

---

# 10. System architecture authority

Cross-stack/system concerns belong under:

```text
docs/architecture/
```

Examples:

```text
system-overview.md
bounded-context-map.md
contract-boundaries.md
data-ownership-and-consistency.md
events-realtime-and-delivery-boundary.md
capability-extraction-strategy.md
```

System architecture owns relationships that cannot be correctly defined by backend or frontend alone.

It does not own their detailed local mechanisms.

---

# 11. Quality authority

Repository-wide quality expectations belong under:

```text
docs/quality/
```

Examples:

```text
testing philosophy
security quality
accessibility
performance/scalability
engineering-quality standard
```

Project-specific exact test commands/gates remain project-owned.

Example:

```text
docs/quality/testing-strategy.md
    defines why and what categories of proof are required

backend testing-and-quality-gates.md
    defines backend-specific suite/gate contract

frontend testing-and-quality-gates.md
    defines frontend-specific suite/gate contract
```

---

# 12. Delivery authority

Repository-wide change evolution belongs under:

```text
docs/delivery/
```

It owns:

```text
change classification
impact analysis
migration responsibility
definition of done
rollout/recovery principles
```

It does not own:

- EF migration implementation;
- frontend package migration mechanics;
- provider-specific rollout internals.

Those route to local owners.

---

# 13. Operations and infrastructure authority

Operations owns operational behavior:

```text
observability
incidents
recovery
degradation
```

Infrastructure owns repository-level runtime/deployment environment:

```text
environment model
deployment runtime
containers/local services
```

These areas MUST NOT absorb backend `Notrelix.Infrastructure` implementation architecture.

The identical word “infrastructure” does not imply identical authority.

---

# 14. Document class registry

Every canonical file MUST have one primary class.

Supported primary classes are:

```text
constitution
architecture
product-context
governance
standard
delivery-policy
runbook
adr
template
generated
context
index-router
```

A document may reference several concerns.

Its primary class determines what it is allowed to own.

---

# 15. Constitution

Constitutions are high-stability normative contracts.

Examples:

```text
PRODUCT.md
DESIGN.md
RULE.md
```

A constitution may summarize downstream architecture.

It MUST NOT contain exact mutable implementation inventory unless that inventory itself is the constitution.

Changing a constitution is a high-impact product/architecture action.

---

# 16. Architecture document

Architecture documents define current intended engineering contracts.

They may own:

```text
responsibility
boundary
dependency direction
allowed/forbidden behavior
consistency model
failure model
contract surface
change impact
evidence requirements
```

They should be deep when the architecture is deep.

They should not be padded with generic enterprise prose.

---

# 17. Product-context document

A product-context document owns business semantics.

It may own:

```text
mission
ubiquitous language
business object meaning
invariants
lifecycle
scope/tenant meaning
authorization meaning
cross-context contracts
product failure semantics
user journeys
```

It should avoid implementation-specific framework/package rules unless needed only as evidence/reference.

---

# 18. Governance document

Governance documents define how repository truth is managed.

They may own:

```text
authority
lifecycle
decision policy
exception policy
documentation quality gates
topic ownership
```

They MUST NOT become alternate architecture owners.

---

# 19. Standard

A standard defines repository-wide quality expectations.

Examples:

```text
testing
security engineering
accessibility
performance/scalability
engineering quality
```

A standard says what level of quality/proof is required.

Project architecture says how that project satisfies it.

---

# 20. Delivery policy

A delivery policy defines change-handling requirements.

Examples:

```text
classification
migration
definition of done
rollout
recovery
```

It is normative for change process.

It is not a progress tracker.

---

# 21. Runbook

A runbook defines operational action.

It should answer:

```text
trigger
impact
diagnosis
actions
validation
escalation
recovery
post-incident
```

A runbook does not become system architecture rationale.

---

# 22. ADR

An ADR records a consequential decision.

It should include:

```text
context/problem
decision
alternatives
rationale
consequences
migration/compatibility where relevant
status
supersession
```

Accepted ADRs are historical truth.

They are not rewritten to pretend a later decision was always the original decision.

---

# 23. Template

A template provides structure.

It MUST NOT define new product/architecture rules.

Templates SHOULD reference canonical owners for normative requirements.

---

# 24. Generated document

Generated documents expose exact machine-derived facts.

They MUST include or have discoverable:

```text
producer
generation command
do-not-edit marker
drift check
```

Generated evidence does not own architecture rationale.

---

# 25. Context document

Context documents describe current repository facts.

Examples:

```text
CONTEXT.md
backend/CONTEXT.md
```

They are non-normative unless a specific field is explicitly defined otherwise.

A current source fact MUST NOT become architectural precedent solely because CONTEXT records it.

---

# 26. Index/router

Index/router documents provide navigation.

Examples:

```text
README.md
docs/README.md
CONTEXT-MAP.md
backend/docs/README.md
frontend/docs/README.md
```

They may summarize.

They MUST link to the owner rather than become a second detailed definition.

---

# 27. Documentation governance rules

The following rules are stable documentation-governance contracts.

---

# DOC-001 — One Topic Has One Canonical Normative Owner

## Rule

A distinct normative topic MUST have exactly one canonical owner.

Other documents may:

- summarize;
- reference;
- explain local consequences;
- provide a checklist.

They MUST NOT independently define the same normative detail.

## Example

Correct:

```text
backend/docs/architecture/application-model.md
    owns Application transaction contract

docs/architecture/data-ownership-and-consistency.md
    owns cross-context consistency principles
```

Incorrect:

```text
backend/docs/architecture/application-model.md
docs/architecture/backend-application.md
backend/RULE.md

all define the same Application pipeline/transaction rules
```

## Evidence

- topic authority map;
- link/reference scan;
- authority check;
- documentation review.

---

# DOC-002 — Scope Does Not Override Semantic Role

## Rule

A file closer to source MAY specialize local procedure.

It MUST NOT override a higher-scope semantic invariant merely because it is closer.

Example:

```text
backend/tests/AGENTS.md
```

may define local test workflow.

It cannot weaken:

```text
RULE.md NRX-016
```

A `README.md` in a project cannot redefine product semantics.

A local AGENTS file cannot change Domain architecture.

---

# DOC-003 — Summaries Route; They Do Not Re-Own

## Rule

README/index/router files MAY summarize architecture/product concepts to orient readers.

They MUST reference the canonical owner for detailed normative semantics.

If a summary grows into an independent detailed rulebook, move the normative detail to the correct owner and reduce the summary.

## Applies to

```text
README.md
docs/README.md
CONTEXT-MAP.md
backend/README.md
frontend/README.md
backend/docs/README.md
frontend/docs/README.md
```

---

# DOC-004 — Current Context Is Evidence, Not Durable Intent

## Rule

Current-state documents MAY record:

```text
existing dependencies
temporary topology
transition
known drift
current command
current runtime
```

They MUST NOT turn those facts into permanent architecture.

When current facts change, update context.

When architecture changes, update architecture owner.

Do not use context as a substitute for either.

---

# DOC-005 — Source, Tests, and CI Are Evidence, Not Automatic Precedent

## Rule

Existing code proves current behavior.

It does not automatically prove approved architecture.

When source and canonical docs disagree:

1. identify the exact conflicting claim;
2. inspect callers/consumers;
3. inspect tests;
4. inspect contracts/migrations/generated evidence;
5. inspect ADRs/exceptions;
6. classify the mismatch;
7. resolve intentionally.

Allowed classifications:

```text
DOC_STALE
SOURCE_DEBT
TRANSITION
CONTRACT_CHANGE
UNRESOLVED
```

Do not silently choose the cheaper side.

---

# DOC-006 — ADRs Preserve Decisions; Canonical Docs Preserve Current Contract

## Rule

ADRs and architecture handbooks have different responsibilities.

```text
ADR
    why the decision was made

canonical architecture
    what the approved current contract is
```

When a decision changes:

```text
old ADR
→ superseded

new ADR
→ accepted

canonical architecture
→ updated to current contract
```

Do not rewrite old accepted ADR content to hide history.

---

# DOC-007 — Exact Inventories Are Producer-Owned

## Rule

When an exact inventory can be generated from an executable producer, the producer is authoritative.

Markdown is generated evidence.

Examples:

```text
backend project inventory
→ backend.slnx + csproj

frontend package dependency universe
→ architecture-manifest.ts

public frontend contract
→ API/OpenAPI/codegen producer
```

Hand-maintained exact duplicates are prohibited unless generation is impractical and explicitly justified.

---

# DOC-008 — Scoped Documentation Requires Distinct Local Responsibility

## Rule

Do not create scoped documentation by symmetry.

A scoped file is justified only when it has:

```text
distinct audience or execution behavior
distinct semantic responsibility
distinct lifecycle
distinct evidence
```

Valid example:

```text
backend/tests/AGENTS.md
```

because test execution responsibility differs materially.

Invalid justification:

```text
Domain has a folder
therefore Domain must have README + AGENTS + RULE + CONTEXT
```

---

# DOC-009 — Skills and Provider Routers Are Procedure, Not Architecture

## Rule

Files such as:

```text
.agents/skills/*/SKILL.md
CLAUDE.md
provider-specific compatibility files
```

may define workflow/tool usage.

They MUST route architecture to canonical docs.

They MUST NOT define a competing product or architecture contract.

---

# DOC-010 — Cross-Topic Changes May Update Several Owners Without Duplicating Ownership

## Rule

One feature change may affect several canonical owners.

That is valid when each owner answers a different question.

Example:

```text
New Work Management integration event

product/work-management
    what product fact occurred

system contract boundary
    compatibility/version semantics

backend Domain
    when event is raised

backend Platform
    delivery/idempotency

Automation
    consumer semantics

delivery
    migration/rollout
```

Do not copy the same event definition paragraph into every file.

---

# DOC-011 — Authority Migration Is Transactional

## Rule

Moving a topic from one canonical owner to another MUST be treated as an authority migration.

The migration is complete only when:

1. new owner contains the full retained durable knowledge;
2. topic-authority map points to new owner;
3. routers/readmes point to new owner;
4. references are migrated;
5. generated indices are updated;
6. old owner no longer claims authority;
7. old duplicate path is removed or explicitly non-authoritative;
8. docs governance passes.

Do not create new authority and leave the old one active “temporarily” without an explicit transition contract.

---

# DOC-012 — Normative Semantic Changes Are Product/Architecture Changes

## Rule

Changing wording is editorial only when meaning is unchanged.

Changing a normative statement that affects:

```text
product ownership
architecture boundary
security
tenant scope
consistency
contract compatibility
lifecycle
quality requirement
```

is a semantic change.

It requires the corresponding product/architecture/change process.

Do not merge a semantic rule change as “docs only” merely because source changes are deferred.

---

# DOC-013 — Historical Artifacts Do Not Remain Active Authority

## Rule

The following are not permanent architecture owners after their lifecycle:

```text
roadmap
freeze plan
wave plan
migration tracker
readiness report
one-time audit
implementation checklist
historical baseline
```

Extract durable knowledge into:

```text
canonical docs
ADRs
runbooks
tests/gates
```

Then remove the temporary artifact from the active authority path.

Git is the default archive.

---

# DOC-014 — Versioned/Final/Frozen Filename Generations Are Forbidden Authority Management

## Rule

Do not manage current architecture using filenames such as:

```text
architecture-v2.md
architecture-final.md
architecture-final-v4.md
freeze-version-3.md
rules-new.md
rules-old.md
```

Use:

```text
stable canonical path
ADR supersession
document lifecycle status
Git history
```

A protected/frozen architecture may still have an editable canonical document.

Architecture maturity and document lifecycle are separate.

---

# DOC-015 — Canonical References Are Repository-Relative and Resolvable

## Rule

Authored canonical docs MUST use repository-relative references for repository artifacts.

Forbidden:

```text
file:///Users/...
/home/<name>/...
C:\Users\...
```

A canonical relative link MUST resolve after the documentation migration is complete.

Temporary migration branches must either create the target path in the same change or stage the migration so governance remains truthful.

---

# DOC-016 — Canonical Metadata Is Minimal, Stable, and Machine-Useful

## Rule

Metadata exists only when it supports routing/governance/generation.

Canonical authored docs use a lightweight model such as:

```yaml
document_id
document_type
status
owner
applies_to
evidence
review_on
```

Do not add:

```text
decorative maturity scores
manual package counts
stale last-verified SHA badges
authoritative "final" labels
```

unless a real automated governance contract consumes them.

---

# DOC-017 — Authority Conflict Is a Stop Condition

## Rule

If two active artifacts appear to own the same normative topic and the conflict cannot be resolved by documented scope, stop.

Do not:

- pick the newest file;
- pick the longest file;
- pick the closest file;
- pick source automatically;
- pick the file with `CANONICAL` in frontmatter.

Investigate ownership/decision history and classify the conflict.

The final state MUST restore one owner.

---

# DOC-018 — Documentation Gates Are Required Executable Architecture

## Rule

Documentation governance is a required quality gate.

At minimum the target governance must verify:

```text
relative links
absolute workstation/file links
required canonical paths
forbidden duplicate/legacy authority
document/rule/ADR ID uniqueness
source/project/package inventory alignment
generated artifact drift
topic authority coherence
```

A required documentation check that executes no meaningful protected work is not valid evidence.

This rule extends repository `NRX-016` and `NRX-018`.

---

# 28. Semantic authority model

For a product/architecture question, use this model:

```text
Repository constitution, when applicable
        ↓
Canonical topic owner
        ↓
Active scoped exception, only for its approved scope
```

Examples:

```text
RULE.md
    repository invariant

PRODUCT.md / product context
    product meaning

backend/frontend/system architecture file
    implementation/system contract

approved exception
    temporary/local deviation
```

An exception cannot silently become the new canonical rule.

---

# 29. Execution instruction model

Execution scope is resolved independently from semantic authority.

Typical procedure chain:

```text
root AGENTS.md
→ applicable scoped AGENTS.md
→ skill/checklist if task uses it
```

A scoped procedure may be more specific.

It cannot relax the normative semantic owner.

Example:

```text
backend/tests/AGENTS.md
    can define where test responsibility belongs

cannot say:
    "zero-test required suite is okay"

because RULE/NRX-016 forbids that
```

---

# 30. Decision-history model

Decision history is not a priority list.

Use:

```text
active current architecture
↔ accepted/superseded ADR history
```

They MUST be coherent.

If they are not:

```text
no superseding ADR
+
canonical docs disagree
```

then classify as documentation/decision drift and stop before inventing a rationale.

---

# 31. Evidence model

Evidence should be chosen according to the claim.

| Claim | Strong evidence |
|---|---|
| Current backend project set | `backend/backend.slnx` |
| Current backend package/reference graph | `*.csproj`, package props |
| Current frontend workspace | `pnpm-workspace.yaml` |
| Legal frontend package dependencies | architecture manifest |
| Current public API shape | OpenAPI/producer |
| Current schema | migrations + mappings + DB tests |
| Domain behavior | Domain source/tests |
| Application pipeline | registrations/behaviors/tests |
| RLS behavior | Infrastructure/integration tests |
| Message delivery semantics | Platform source/tests/integration |
| Frontend query behavior | source/tests |
| Current runtime services | Compose/Makefile |
| Product meaning | PRODUCT/product-context docs + behavior evidence |
| Historical rationale | ADR |

Do not cite a generated table as design rationale.

Do not cite a roadmap as current architecture.

---

# 32. Summary versus canonical definition

A summary is permitted to repeat concise high-level facts for orientation.

A summary becomes a problem when it contains enough normative detail that future changes must be made independently in both places.

Use this test:

> If a rule changes, would a reviewer need to edit both files to keep them semantically complete?

If **yes**, ownership is probably duplicated.

Preferred structure:

```text
summary:
    concise meaning
    link to owner

owner:
    full normative contract
```

---

# 33. Local consequence versus duplicated rule

Local documents MAY explain the consequence of a global rule.

Example:

Global:

```text
NRX-003
tenant isolation must hold across all boundaries
```

Frontend local consequence:

```text
workspace-scoped server-state keys must include scope sufficient
to prevent old/current workspace cache collision
```

This is legitimate because the frontend doc defines the implementation consequence for its scope.

It should reference the repository invariant.

It should not re-author the entire tenant isolation model.

---

# 34. Canonical owner admission test

Before creating a new canonical document, answer:

```text
What distinct question does it own?
Which existing owner cannot correctly own that question?
What is its semantic boundary?
Who consumes it?
What evidence proves it?
What is its lifecycle?
What existing content moves into it?
What content explicitly remains elsewhere?
```

If these questions cannot be answered, do not create a new canonical file.

---

# 35. Scoped README admission

A README is justified when the directory needs orientation for humans.

It MAY include:

```text
purpose
entry points
commands
local structure
links to canonical docs
```

It SHOULD NOT contain a second architecture handbook.

Do not create README files for every project/package automatically.

---

# 36. Scoped AGENTS admission

A scoped AGENTS file is justified when local execution behavior materially differs.

Examples:

```text
special test responsibility
generated-file workflow
deployment-sensitive operational area
```

It MUST state its scope.

It MUST route semantic architecture to the canonical owner.

It MUST NOT change repository invariants.

---

# 37. Scoped CONTEXT admission

Prefer executable/generated evidence over manual current-state snapshots.

Create a scoped CONTEXT only when local current-state facts are:

- materially useful;
- not efficiently generated;
- too detailed for root CONTEXT;
- expected to change independently.

Current example:

```text
backend/CONTEXT.md
```

Do not create CONTEXT files in every frontend package.

---

# 38. RULE admission

Repository-wide stable invariants belong in:

```text
RULE.md
```

Do not create:

```text
backend/RULE.md
frontend/RULE.md
package/RULE.md
```

to express local implementation architecture.

Local details belong in canonical architecture docs.

A local invariant can have a stable local rule ID inside the owning architecture file if cross-reference value justifies it.

---

# 39. Skills admission

A skill is useful when work has a repeatable workflow.

Examples:

```text
add-domain-capability
contract-change
data-migration
architecture-review
freeze-certification
```

A skill should specify:

```text
inputs
reading path
procedure
validation
stop conditions
output
```

It MUST NOT define product/architecture decisions that belong elsewhere.

---

# 40. Generated-document authority

Generated files have authority only for the exact facts derived from their producer.

Example:

```text
architecture-manifest.ts
→ exact package registration/allow-list

package-boundaries.md
→ human-readable generated evidence
```

The generated Markdown does not become the producer.

Do not change architecture by editing the generated representation.

---

# 41. Generated artifact header contract

A generated documentation file SHOULD expose:

```text
GENERATED — DO NOT EDIT

Producer:
<path>

Command:
<actual command>

Drift check:
<actual command>
```

If the generator is not yet implemented, do not label a manually written file as generated.

---

# 42. Current context authority

`CONTEXT.md` should explicitly distinguish:

```text
Fact
Not implied
```

for transitional facts likely to become accidental precedent.

Examples:

```text
Application currently references EF Core
≠ persistence architecture belongs in Application

features-search exists
≠ Search is automatically a bounded context
```

This is an approved current-context pattern.

---

# 43. Topic authority map

Canonical topic ownership is recorded in:

```text
docs/governance/topic-authority-map.md
```

The map should include, at minimum:

```text
topic
canonical owner
scope
evidence
decision registry
```

It SHOULD be machine-checkable/generatable where practical.

It must remain coherent with:

```text
CONTEXT-MAP.md
docs/README.md
```

---

# 44. Task router versus topic map

These files intentionally answer different questions.

```text
CONTEXT-MAP.md
    "I am doing task X. What should I read?"

topic-authority-map.md
    "Topic Y is owned by which canonical document?"
```

Do not merge them into one giant document if doing so makes navigation or governance weaker.

Do not let them disagree.

---

# 45. Documentation metadata contract

Canonical authored files under `docs/` SHOULD use:

```yaml
---
document_id: <stable-id>
document_type: <class>
status: <lifecycle>
owner: <semantic-owner>
applies_to:
  - <scope>
evidence:
  - <repository path>
review_on:
  - <semantic trigger>
---
```

Fields may be extended only when governance/tooling has a real use.

---

# 46. `document_id`

Requirements:

- stable;
- unique;
- semantic;
- not branch-specific;
- not filename-version-specific.

Examples:

```text
DOC-AUTHORITY
SYS-OVERVIEW
PROD-WORK-MANAGEMENT
QLT-TESTING
DEL-MIGRATION
OPS-OBSERVABILITY
INFRA-ENVIRONMENT
```

Do not encode dates or commit SHA.

---

# 47. `document_type`

Allowed values are governed by the document-class registry.

Do not invent synonyms such as:

```text
handbook-final
constitution-v2
architecture-rulebook
guide-canonical
```

when an existing class applies.

---

# 48. `status`

Lifecycle is owned by:

```text
documentation-lifecycle.md
```

Target authored lifecycle:

```text
draft
active
superseded
```

Generated:

```text
generated
```

Do not use status to express architecture maturity.

---

# 49. `owner`

Owner identifies semantic maintenance responsibility.

Examples:

```text
documentation-governance
system-architecture
product-work-management
backend-architecture
frontend-architecture
security-quality
```

Owner does not need to be a named individual.

Team/codeowner integration may map semantic owner to people elsewhere.

---

# 50. `applies_to`

`applies_to` describes the semantic/scope boundary.

Examples:

```text
repository
backend
frontend
work-management
backend-application
frontend-state
```

It should not become a file glob inventory unless governance needs that.

---

# 51. `evidence`

Evidence lists the strongest repository artifacts that prove or constrain the document's current claims.

It is not a complete source inventory.

Use:

```text
source roots
tests
manifests
generated producers
contracts
```

when they materially support the document.

---

# 52. `review_on`

`review_on` records semantic triggers.

Examples:

```text
bounded-context-owner-change
application-pipeline-change
message-identity-change
frontend-package-model-change
documentation-authority-change
```

Do not use calendar dates as the only freshness mechanism for architecture.

---

# 53. No required `last_verified_sha`

A commit SHA may be useful in an audit artifact.

It is not required metadata for canonical documentation.

A stale SHA can create false confidence.

Canonical source alignment should instead be protected through:

```text
review triggers
source references
architecture tests
generated checks
documentation governance
```

---

# 54. Link authority

A link is not proof of correctness.

But canonical docs must remain navigable.

Repository links should be relative.

External references may use stable HTTPS URLs when genuinely needed.

Do not link to:

- local workstation;
- ephemeral temporary file;
- unavailable private path

as canonical authority.

---

# 55. External references

External references are allowed when they provide:

```text
standards
protocol specifications
framework/vendor contracts
security guidance
product/legal requirements
```

The Notrelix canonical document must still state the project-specific decision.

Do not outsource architecture to a generic external article.

Example:

Correct:

```text
Notrelix adopts WCAG 2.2 AA.
Reference: W3C WCAG 2.2.
```

Incorrect:

```text
Accessibility:
See random blog post.
```

---

# 56. Source reference style

Prefer repository paths and useful symbols.

Example:

```text
Evidence:
- backend/src/Notrelix.Application/Common/Behaviors/...
- backend/tests/Notrelix.Application.Tests/...
```

Do not copy large source snippets into canonical docs when a concise example plus source reference is enough.

Use code examples when they clarify an invariant, not to mirror implementation line-for-line.

---

# 57. Historical knowledge migration

Before removing a legacy document:

1. classify each durable claim;
2. identify target canonical owner;
3. migrate the claim;
4. migrate rationale to ADR when appropriate;
5. migrate procedure to runbook/skill when appropriate;
6. migrate exact inventory to generator if possible;
7. drop stale progress/history;
8. update references;
9. remove legacy file/path.

The rule is:

> **migrate knowledge, replace authority**

not:

> copy every old paragraph into the new tree.

---

# 58. Duplicate-knowledge test

Duplicate prose is acceptable only when the repeated text is intentionally short orientation.

A likely authority duplication exists when:

- both copies contain MUST/MUST NOT rules;
- both copies define the same lifecycle;
- both copies define exact pipeline/order;
- both copies define the same contract schema;
- both copies must be updated together after every semantic change.

When that happens, select one owner and convert the other copy to a consequence/reference.

---

# 59. Authority conflict classes

Documentation conflicts should be classified.

## `DUPLICATE_OWNER`

Two active docs claim the same normative topic.

## `STALE_OWNER`

The correct owner exists but is stale relative to an approved change.

## `MISSCOPED_RULE`

A rule exists in the wrong authority plane.

## `GENERATED_OVERRIDE`

A generated representation has been manually treated as producer.

## `HISTORICAL_OVERRIDE`

Roadmap/audit/ADR/history is being used as current handbook.

## `PROCEDURE_OVERRIDE`

AGENTS/skill/provider file is redefining architecture.

## `SOURCE_PRECEDENT_ERROR`

Incidental/transitional source pattern is being treated as approved architecture.

Each conflict must be resolved to one coherent authority model.

---

# 60. Conflict resolution algorithm

When two artifacts disagree:

1. identify the exact question;
2. classify each artifact by document class;
3. identify semantic scope;
4. look up topic owner;
5. inspect accepted/superseded ADRs;
6. inspect active exceptions;
7. inspect source/tests/contracts/migrations/manifests;
8. determine whether the discrepancy is:
   - docs stale;
   - source debt;
   - transition;
   - contract change;
   - unresolved;
9. update the single correct canonical owner;
10. update implementation/evidence as required;
11. reduce/remove competing authority;
12. run documentation governance.

Do not resolve by file timestamp alone.

---

# 61. Explicit task conflict

If an explicit task requests behavior that contradicts current architecture:

- do not ignore the task;
- do not silently violate architecture.

Classify it as a requested product/architecture change.

Then:

```text
identify affected invariant
→ identify owner
→ evaluate migration/security/compatibility
→ update decision if approved
→ update canonical docs
→ update implementation/gates
```

Task intent is honored through controlled contract change.

---

# 62. Active exception authority

An approved architecture exception MAY temporarily allow a scoped deviation.

The exception must define:

```text
rule/owner being deviated from
scope
reason
risk
compensating control
owner
review/expiry trigger
removal plan
```

The canonical architecture remains the default for new unaffected code.

Exception source does not become precedent.

---

# 63. Permanent change versus exception

If the desired behavior is permanent, do not maintain it as an exception.

Update:

```text
canonical owner
ADR if consequential
tests/gates
consumers
migration
```

and close the exception.

---

# 64. Documentation authority migration example

Suppose old structure contains:

```text
docs/engineering/02-backend/04-application-pipeline.md
```

and target owner is:

```text
backend/docs/architecture/application-model.md
```

Correct migration:

```text
audit old pipeline knowledge
→ merge durable unique content into application-model.md
→ preserve important rationale in backend ADR if needed
→ update CONTEXT-MAP/topic authority
→ update references
→ remove old engineering backend file/tree
→ run docs-check
```

Incorrect migration:

```text
keep both
mark both canonical
tell readers to "use whichever is more detailed"
```

---

# 65. Product migration example

Suppose old docs describe:

```text
BoardGroup = Kanban column/status
```

but canonical product semantics define:

```text
BoardGroup = structural grouping
Kanban column = configured grouping-field value
```

Do not average the two definitions.

Product owner resolves meaning.

Backend/frontend docs and source then migrate to that meaning.

Historical wording may remain only in Git/decision history if useful.

---

# 66. Generated inventory example

Frontend package architecture:

```text
Producer:
frontend/tooling/dependency-rules/src/architecture-manifest.ts

Generated evidence:
frontend/docs/generated/package-boundaries.md
```

If a new package is added:

Correct:

```text
update valid architecture/manifest
→ regenerate docs
→ run architecture/docs checks
```

Incorrect:

```text
edit package-boundaries.md manually
```

---

# 67. Current-state example

If `Notrelix.Application` currently references EF Core:

`CONTEXT.md` may record:

```text
Fact:
Application references EF Core.

Not implied:
New persistence ownership belongs in Application.
```

New implementation placement still follows:

```text
application-model.md
infrastructure-and-data.md
architecture tests
```

unless an approved architecture change says otherwise.

---

# 68. Router example

`CONTEXT-MAP.md` may say:

```text
Application transaction
→ backend/docs/architecture/application-model.md
```

It should not reproduce the full transaction contract.

If it starts doing so, reduce it back to routing.

---

# 69. Documentation lifecycle relationship

This file owns authority.

Lifecycle is owned separately by:

```text
docs/governance/documentation-lifecycle.md
```

Authority asks:

> Who owns the topic?

Lifecycle asks:

> Is this document draft, active, superseded, or generated?

Do not combine those into one `CANONICAL/FROZEN` badge system.

---

# 70. Decision policy relationship

Decision/exception rules are owned by:

```text
docs/governance/decision-and-exception-policy.md
```

This file only establishes that ADRs/exceptions cannot replace canonical topic ownership.

---

# 71. Documentation gate relationship

Executable enforcement is owned by:

```text
docs/governance/documentation-quality-gates.md
```

This file defines what must be protected.

The gate document defines how CI/tooling proves it.

---

# 72. Topic map relationship

Exact topic-owner registry is owned by:

```text
docs/governance/topic-authority-map.md
```

This file defines the rules for that registry.

The topic map contains the actual mapped topics.

---

# 73. `docs/README.md` relationship

`docs/README.md` is the repository documentation index.

It may summarize:

- tree;
- classes;
- reading paths;
- governance entry points.

This file is the canonical authority for authority semantics.

If README and this file conflict on authority rules, fix README to route here.

---

# 74. `RULE.md` relationship

`RULE.md` defines repository-wide invariants, including documentation coherence.

This governance file specializes those invariants for documentation.

It cannot weaken:

```text
NRX-018
NRX-016
```

or other repository rules.

---

# 75. `AGENTS.md` relationship

`AGENTS.md` defines execution behavior.

It should tell agents to:

```text
identify owner
read canonical docs
classify drift
stop on unresolved conflict
report evidence
```

This file defines what “canonical owner” means.

---

# 76. Provider compatibility files

Provider-specific instruction files are adapters.

They should be thin.

Correct pattern:

```text
CLAUDE.md
→ AGENTS.md
→ RULE.md
→ CONTEXT-MAP.md
```

Incorrect:

```text
CLAUDE.md contains a separate Domain architecture
```

Provider files must never become the only place an important architecture rule exists.

---

# 77. Tool/skill documentation

Tool instructions may legitimately include tool syntax that architecture docs should not contain.

Example:

```text
how to run an architecture review skill
how to regenerate contracts
how to execute a migration workflow
```

They must link to semantic owners for decisions.

---

# 78. Ownership review questions

When reviewing a new/editing doc, ask:

```text
What exact question does this file own?
What questions does it explicitly not own?
Who changes when this rule changes?
What source/tests/manifests prove current behavior?
Is the same rule already defined elsewhere?
Could this content be generated?
Is this current fact rather than architecture?
Is this historical rationale rather than current contract?
Is this procedure rather than semantics?
```

If the answers are unclear, the file's authority is not well-defined.

---

# 79. Authority smells

The following are warnings:

```text
"single source of truth" appears in many files
"canonical" appears on overlapping trees
same MUST paragraph appears in root/backend/system docs
local README is longer than canonical architecture owner
generated table has no producer
ADRs contain current implementation checklists
roadmaps are linked from AGENTS as required architecture
new folder automatically receives RULE/AGENTS/CONTEXT
file name contains final-v4
frontmatter has old verification SHA but still says FROZEN
```

These should trigger governance review.

---

# 80. Documentation review severity

Authority defects should be classified by impact.

## Blocker

Examples:

- two active canonical owners conflict;
- security/product ownership is contradictory;
- generated source is being manually overridden;
- required router points to removed/wrong owner;
- docs governance would direct Coding Agents to stale architecture.

## Major

Examples:

- summary duplicates large normative detail;
- scoped file can be misread as architecture owner;
- lifecycle/ADR status ambiguous;
- unique durable legacy knowledge not yet migrated.

## Minor

Examples:

- missing non-critical reference;
- local wording duplication without semantic risk;
- metadata description inconsistency.

Authority ambiguity affecting architecture is not “documentation polish”.

---

# 81. Authority-change checklist

Before changing canonical ownership:

```text
[ ] old owner identified
[ ] new owner justified
[ ] durable knowledge inventory complete
[ ] product/architecture decision impact understood
[ ] consumers/readers identified
[ ] topic-authority map updated
[ ] CONTEXT-MAP updated
[ ] docs/README or project indexes updated
[ ] ADR/exception implications handled
[ ] generated indices updated
[ ] old canonical claim removed
[ ] legacy references removed
[ ] docs governance passes
```

---

# 82. New canonical-document checklist

Before adding a canonical file:

```text
[ ] distinct topic exists
[ ] no existing owner already fits
[ ] class selected
[ ] document_id selected
[ ] owner selected
[ ] applies_to defined
[ ] evidence identified
[ ] review triggers defined
[ ] non-responsibilities defined
[ ] router/topic map updated if required
[ ] no symmetry-only motivation
```

---

# 83. Canonical-document deletion checklist

Before deleting a canonical/legacy file:

```text
[ ] durable knowledge classified
[ ] unique current semantics migrated
[ ] important rationale migrated/preserved in ADR
[ ] operational procedure migrated if needed
[ ] generated facts moved to producer if appropriate
[ ] references migrated
[ ] topic map updated
[ ] no active consumer relies on old path
[ ] old authority removed completely
[ ] docs governance passes
```

---

# 84. Documentation completion standard

Documentation authority is healthy when:

- each mapped topic has one canonical owner;
- root files have distinct roles;
- scoped docs specialize without overriding constitutions;
- product semantics are independent from implementation;
- system cross-stack docs do not duplicate project internals;
- backend implementation docs exist only under backend authority;
- frontend implementation docs exist only under frontend authority;
- current facts are distinguished from normative intent;
- ADRs preserve rationale;
- generated docs have producers;
- historical plans are not active architecture;
- skills/provider files are procedural only;
- authority migration removes old competing owners;
- documentation gates enforce the model.

---

# 85. Final authority rule

When adding a statement to Notrelix documentation, ask:

```text
What question does this statement answer?
What semantic scope does it apply to?
What document class is it?
Who owns that question?
Is the statement:
    normative intent?
    current evidence?
    historical rationale?
    execution procedure?
    generated fact?
```

Then place the statement in the single correct authority plane.

If two places appear equally correct, the topic boundary is not sufficiently defined.

Resolve the ownership before adding another copy.

The documentation system is correct when a human or Coding Agent can ask:

> **“Who is allowed to define this?”**

and receive one deterministic answer.
