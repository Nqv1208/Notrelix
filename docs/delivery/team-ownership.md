---
document_id: DEL-TEAM-OWNERSHIP
document_type: delivery-policy
status: active
owner: engineering-delivery
applies_to:
  - repository
  - product-contexts
  - backend
  - frontend
  - platform
  - quality
  - delivery
  - operations
  - infrastructure
evidence:
  - PRODUCT.md
  - RULE.md
  - AGENTS.md
  - CONTEXT-MAP.md
  - docs/governance/topic-authority-map.md
  - docs/governance/decision-and-exception-policy.md
  - docs/architecture/bounded-context-map.md
  - docs/delivery/change-classification.md
  - docs/delivery/definition-of-done.md
review_on:
  - ownership-model-change
  - bounded-context-change
  - platform-owner-change
  - review-routing-change
  - codeowners-introduction
  - team-reorganization
---

# Team and Capability Ownership

> **Logical ownership is durable; staffing is not architecture.**
>
> Notrelix organizes durable responsibility around product vocabulary, architecture mechanisms, and engineering capabilities. People, squads, reporting lines, and GitHub teams may change without redefining the product or code boundaries.

This document owns delivery-level responsibility semantics:

- how logical owners are interpreted;
- how cross-owner work is coordinated;
- how review ownership differs from semantic authority;
- how ambiguous ownership is escalated.

It does **not** replace `topic-authority-map.md`, which is the canonical topic → normative-document router.

It does **not** invent current GitHub handles, team names, or organizational charts.

# 1. Why ownership exists

Ownership answers:

```text
Who is accountable for preserving this capability's contract?
Who must understand the impact of a change?
Who should review a cross-boundary change?
Who owns the reusable mechanism?
Who operates the runtime?
```

Ownership does not mean unrestricted write access.

# 2. DEL-OWN-001 — Logical owner is stable; staffing is not architecture

Durable owner examples:

```text
work-management
documents
identity
backend-platform
frontend-architecture
engineering-quality
engineering-delivery
operations
infrastructure
```

Temporary squad names, contractor names, or reporting structures MUST NOT become package/context/domain boundaries merely because they reflect current staffing.

# 3. Semantic ownership

Semantic ownership means responsibility for the meaning of a product/system fact.

Examples:

```text
Work Management
→ Board / Field / Item semantics

Documents
→ Page / Block semantics

Governance
→ authorization policy semantics

Billing
→ Plan / Subscription / Entitlement / Usage semantics
```

# 4. DEL-OWN-002 — Business vocabulary chooses the semantic owner

When deciding where a capability belongs, ask:

```text
Which business language describes this fact?
Which context owns its lifecycle/invariants?
Which context remains authoritative if deployment topology changes?
```

Do not choose the owner from whichever team already has a convenient repository.

# 5. Mechanism ownership

Mechanism ownership covers reusable engineering capabilities such as:

```text
messaging
idempotency
outbox
realtime transport
frontend dependency architecture
test infrastructure
CI
observability
deployment
```

Mechanism owners provide reusable infrastructure and constraints.

They do not absorb product policy.

# 6. DEL-OWN-003 — Mechanism owner does not own business decisions

Examples:

```text
backend-platform
→ owns reliable messaging mechanism
≠ owns Work Management event semantics

frontend-architecture
→ owns package/state/realtime mechanism
≠ owns Documents product rules

engineering-quality
→ owns quality standards
≠ decides Billing commercial meaning
```

# 7. Vertical ownership

A product capability can span:

```text
Domain
Application
Infrastructure
API
contracts
frontend product state
web
mobile
tests
operations
```

These are implementation surfaces of one semantic capability.

# 8. DEL-OWN-004 — Layers do not become separate product owners

A “backend team” and “frontend team” may coordinate implementation, but:

```text
backend Work Management
+
frontend Work Management
```

remain one product semantic capability.

# 9. Vertical change example

Adding a new Work Management Field Type can require:

```text
Domain rules
Application use cases
persistence
API/OpenAPI
generated client
web/mobile renderer
Automation compatibility
Analytics compatibility
tests
docs
```

The logical product owner remains Work Management.

# 10. Repository ownership

Repository-level concerns include:

```text
root governance
system architecture
documentation governance
delivery policy
quality standards
shared infrastructure
```

These have mechanism/policy owners rather than product-context owners.

# 11. DEL-OWN-005 — Repository policy cannot be silently overridden by local owner

A context/package/project owner cannot locally waive:

- dependency rules;
- tenant isolation;
- security standard;
- contract-first delivery;
- documentation governance.

Use the canonical decision/exception process.

# 12. Product-context ownership

The canonical business context set is defined by `bounded-context-map.md` and product context docs.

Current approved contexts:

```text
Accounts
Identity
Workspaces
Governance
Work Management
Documents
Collaboration
Automation
Integrations
Billing
Analytics / Reporting
```

# 13. DEL-OWN-006 — New folder/module/team does not create a new bounded context

Context creation/split/merge requires semantic evidence and architecture decision as appropriate.

Do not derive bounded contexts from:

- team boundaries;
- database tables;
- UI navigation;
- microservice desire;
- package count.

# 14. Supporting capabilities

Search, operations, infrastructure, platform transport, codegen, and testing are not automatically business bounded contexts.

# 15. DEL-OWN-007 — Technical capability has technical owner unless product semantics justify a context

This prevents architecture inflation.

# 16. Backend ownership

Backend architecture owns layer/project dependency rules.

Product contexts own business semantics implemented across those layers.

# 17. DEL-OWN-008 — Backend layer owner and product owner cooperate

A change can require both:

```text
product/context review
+
backend architecture/platform review
```

when it touches both semantic and structural contracts.

# 18. Platform ownership

Backend Platform owns reusable background-delivery mechanisms such as:

```text
messaging transport
consumer hosting
delivery reliability
ordering machinery
poison handling
serialization
```

according to backend canonical docs.

# 19. DEL-OWN-009 — Platform cannot define context event meaning by convenience

A transport-friendly generic event shape does not override product event contracts.

# 20. Infrastructure ownership

Infrastructure owns provider/persistence implementation mechanics.

It does not own Domain/business rules.

# 21. DEL-OWN-010 — Provider adapter owner preserves target semantic owner

EF, Redis, S3/R2, email, provider APIs, and search adapters translate mechanics without becoming product authority.

# 22. API ownership

API owns HTTP/transport/composition surface.

Product/Application owners define action/query semantics.

# 23. DEL-OWN-011 — HTTP endpoint location does not determine business owner

A request under one API folder can still invoke a capability owned by another context through explicit contract.

# 24. Frontend architecture ownership

Frontend architecture owns:

```text
package layers
dependency rules
runtime separation
state/query architecture
realtime client architecture
host composition
```

# 25. DEL-OWN-012 — Frontend architecture does not become product owner

The package graph constrains implementation.

Product context docs define Work Management/Documents/Automation semantics.

# 26. UI/design-system ownership

UI mechanism owner owns:

- tokens;
- primitives;
- accessibility baseline implementation;
- platform-specific UI foundation.

Feature/context owner owns product-specific composition/meaning.

# 27. DEL-OWN-013 — Shared UI primitive does not absorb feature behavior

A generic Select/Dialog/Grid should not gain Work Management/Billing-specific rules merely to reuse code.

# 28. Quality ownership

Engineering Quality owns:

```text
quality bar
testing strategy
security quality
accessibility
performance/scalability
```

# 29. DEL-OWN-014 — Quality owner defines proof obligations, not product meaning

Quality can reject insufficient evidence.

It does not redefine context semantics to make tests easier.

# 30. Delivery ownership

Engineering Delivery owns:

```text
change classification
contract-first sequencing
Definition of Done
release/rollout
migration policy
ownership coordination
local development quality
```

# 31. DEL-OWN-015 — Delivery owner does not become implementation owner for every change

Delivery policy governs how changes move.

The actual semantic/mechanism owners implement/review their surfaces.

# 32. Operations ownership

Operations owns runtime operating contracts such as:

```text
SLOs
alerts
incident response
runbooks
backup/restore operations
capacity/runtime health
```

# 33. DEL-OWN-016 — Operations feedback does not silently redefine product semantics

Operational constraints can trigger architecture/product change.

The change still follows the owning semantic/architecture process.

# 34. Infrastructure ownership

Infrastructure owns deployment/environment/resource topology as canonical infrastructure docs define.

Infrastructure resource location is not product data ownership.

# 35. DEL-OWN-017 — Deployment topology and semantic ownership are separate

A database/service/queue can host several capability concerns during a modular-monolith stage without owning their business meaning.

# 36. Documentation ownership

Every normative topic has one canonical document owner under governance.

This file does not duplicate the complete topic map.

# 37. DEL-OWN-018 — Document owner is accountable for coherence, not personal authorship

Anyone may propose changes.

The owner ensures:

```text
scope
semantics
references
evidence
lifecycle
```

remain coherent.

# 38. Code ownership

Code ownership can mean several things:

```text
semantic responsibility
review routing
repository write permission
incident responsibility
```

These MUST NOT be conflated.

# 39. DEL-OWN-019 — CODEOWNERS is review routing, not semantic authority

If `.github/CODEOWNERS` is introduced:

```text
CODEOWNERS
→ executable reviewer routing

topic-authority-map/context docs
→ semantic authority
```

A GitHub team entry cannot override the canonical architecture.

# 40. No invented handles

Until real handles/teams are approved, documentation uses logical owner identifiers only.

# 41. DEL-OWN-020 — Documentation does not invent people or GitHub teams

Do not create placeholder teams such as:

```text
@backend-team
@architecture-team
@work-management-team
```

unless they exist and are approved.

# 42. Review ownership

Reviewers are selected by affected properties.

A change may need multiple review perspectives.

# 43. DEL-OWN-021 — Review follows affected contracts

Examples:

```text
Work Management Field semantic change
→ Work Management owner

Field persistence/index migration
→ + backend data/infrastructure

Field visibility permission
→ + Governance/security

generated frontend contract
→ + frontend/codegen owner
```

# 44. Review is not veto ownership

Reviewers evaluate their protected contract.

They do not get unilateral authority over unrelated semantics.

# 45. Cross-owner changes

Cross-owner work identifies:

```text
initiating owner
affected owners
producer
consumers
migration owner
rollout owner
operational owner
```

as needed.

# 46. DEL-OWN-022 — Initiating owner coordinates; affected owners retain their authority

Example:

```text
Automation action updates Board Item

Automation
→ owns Rule/Execution/action intent

Work Management
→ owns Item mutation contract

Governance
→ owns permission

Platform
→ owns delivery mechanics
```

# 47. Cross-context writes

One owner must not directly mutate another owner's storage merely because both teams agree.

Use normal capability contracts.

# 48. DEL-OWN-023 — Collaboration between teams does not authorize boundary bypass

Organizational coordination cannot replace architecture contracts.

# 49. Producer/consumer ownership

Producer owns the contract it emits.

Consumer owns its interpretation/use under compatibility rules.

Neither may silently redefine the other's authority.

# 50. DEL-OWN-024 — Consumer convenience does not transfer source ownership

A reporting/frontend/integration consumer can request a suitable read contract.

It does not become owner of the source fact.

# 51. Migration ownership

For data/authority migration, declare:

```text
old owner
target owner
migration executor
cutover decision owner
cleanup owner
```

# 52. DEL-OWN-025 — Migration executor is not necessarily target semantic owner

A platform/infrastructure engineer can execute data movement.

The target context still owns the resulting semantics.

# 53. Incident ownership

Operational incident can span owners.

The incident coordinator is temporary coordination responsibility, not new architecture authority.

# 54. DEL-OWN-026 — Incident remediation returns durable knowledge to canonical owners

After incident:

```text
product semantics → context docs
architecture decision → ADR
quality rule → quality docs
runbook → operations
```

Do not leave durable policy only in incident notes/chat.

# 55. Team reorganization

People/team assignment may change.

Canonical logical owner names should change only if responsibility/architecture meaning changes.

# 56. DEL-OWN-027 — Reorg does not trigger package/context rewrite by default

Update review-routing metadata/CODEOWNERS when staffing changes.

Do not rename bounded contexts after departments.

# 57. Single team owning several contexts

Acceptable organizationally if logical boundaries remain clear.

# 58. Several teams contributing to one context

Also acceptable if one semantic owner contract remains clear.

# 59. DEL-OWN-028 — Team topology can be many-to-many with logical capabilities

Architecture is optimized for semantic cohesion/extraction, not org-chart mirroring.

# 60. Ownership matrix strategy

The legacy standalone ownership matrix contained useful routing knowledge.

The durable model is now:

```text
topic-authority-map.md
→ exact normative topic owner

team-ownership.md
→ owner/review/coordination semantics

CODEOWNERS if adopted
→ executable people/team review routing
```

Therefore a second authored `OWNERSHIP-MATRIX.md` is unnecessary.

# 61. DEL-OWN-029 — Ownership matrices do not duplicate the topic authority map

If a generated ownership index becomes useful, generate it from canonical metadata rather than maintaining another handwritten authority table.

# 62. Broad logical-owner classes

Stable classes include:

| Area | Logical responsibility |
|---|---|
| product context | product/context semantic owner |
| system architecture | architecture |
| backend layer architecture | backend architecture |
| backend messaging/reliability | backend-platform |
| frontend package/state/runtime | frontend-architecture |
| design/UI foundation | design / frontend UI owner |
| testing/quality | engineering-quality |
| security quality | engineering-security |
| delivery/release | engineering-delivery |
| runtime operations | operations |
| deployment/resources | infrastructure |
| documentation governance | documentation-governance |

Exact normative documents are routed by `topic-authority-map.md`.

# 63. Ownership discovery

Before changing unfamiliar code:

```text
1. identify product/mechanism vocabulary
2. read topic-authority-map
3. read local backend/frontend docs
4. inspect source evidence
5. classify change
6. identify affected owners
```

# 64. DEL-OWN-030 — Source folder is evidence, not final ownership answer

A misplaced class can be source debt.

Do not preserve wrong ownership just because code currently lives there.

# 65. Ambiguous ownership

When ownership is unclear:

```text
business vocabulary owner first
→ existing system/mechanism owner second
→ architecture decision if genuinely new boundary
```

# 66. DEL-OWN-031 — Ambiguity does not default to Shared/Common

Creating:

```text
Common
Shared
Utils
Core
Platform
```

is not a neutral answer.

Shared placement requires shared semantic/mechanism ownership.

# 67. New cross-cutting behavior

Ask whether it is:

```text
same business concept reused
→ keep owner + contract

reusable technical mechanism
→ platform/foundation

new business language/lifecycle
→ evaluate context boundary
```

# 68. DEL-OWN-032 — Cross-cutting use does not imply cross-cutting ownership

Many contexts can consume one owner through contracts.

# 69. SharedKernel

SharedKernel should contain only truly stable cross-context primitives with explicit ownership.

It is not a dumping ground for convenient Domain reuse.

# 70. Ownership and extraction

Future microservice extraction follows semantic ownership, data boundaries, contracts, and operational readiness.

Team preference alone is not sufficient.

# 71. DEL-OWN-033 — Service extraction does not require one current team per context

Semantic modularity precedes organizational/deployment independence.

# 72. Ownership and security

Security-sensitive changes can require security review regardless of the product owner.

# 73. DEL-OWN-034 — Security review augments, not replaces, semantic review

A security reviewer can reject insecure implementation.

The product owner still owns the intended product action/lifecycle.

# 74. Ownership and quality

Quality owner protects evidence/gates.

A context owner cannot declare a test unnecessary if the quality contract requires it without changing/excepting the quality rule.

# 75. DEL-OWN-035 — Protected property decides escalation path

Examples:

```text
semantic dispute
→ product/context owner

dependency dispute
→ architecture owner

security dispute
→ security + semantic owner

migration dispute
→ target semantic owner + delivery/data owner

runtime incident
→ operations + affected owner
```

# 76. Ownership and generated artifacts

Generated artifact does not have an independent semantic owner.

Its producer owns the fact; generator/tooling owner owns generation mechanism.

# 77. DEL-OWN-036 — Generator owner cannot redefine producer semantics

Codegen translates.

It does not invent public contract fields to satisfy a consumer.

# 78. Ownership and tests

Test project/package ownership follows protected property.

Shared test infrastructure owns mechanism, not test semantics.

# 79. DEL-OWN-037 — Test helper owner cannot weaken product setup silently

A generic fixture cannot auto-grant permission/tenant state in a way that hides real invariants.

# 80. Ownership handoff

A handoff occurs only if semantic/mechanism responsibility genuinely changes.

It requires:

```text
new canonical owner
contract/data migration
docs
review routing
operations
old owner cleanup
```

# 81. DEL-OWN-038 — Ownership handoff is architecture/data change, not ticket reassignment

Changing Jira/GitHub assignee alone does not transfer semantic authority.

# 82. Temporary implementation delegation

One team can implement a change for another owner.

Review/semantic authority remains with owner.

# 83. DEL-OWN-039 — Contributor and owner are distinct

Ownership should enable contribution, not create code silos.

# 84. Bus factor

Critical mechanisms should not depend on undocumented knowledge held by one person.

Repository docs/runbooks/tests carry durable knowledge.

# 85. DEL-OWN-040 — Durable ownership is repository-discoverable

A new engineer should be able to determine:

```text
canonical owner
contracts
tests/gates
operational route
```

without private chat history.

# 86. Review request checklist

```text
[ ] semantic owner identified
[ ] mechanism owner identified if changed
[ ] change classification
[ ] producer/consumer owners
[ ] security/tenant owner if affected
[ ] migration/release owner if affected
[ ] operations owner if runtime behavior changes
[ ] no invented team/handle
```

# 87. New capability checklist

```text
[ ] business vocabulary
[ ] existing context fit
[ ] source-of-truth
[ ] lifecycle/invariants
[ ] consumers
[ ] technical mechanisms
[ ] architecture dependency
[ ] review owners
[ ] ADR needed?
```

# 88. Ownership-handoff checklist

```text
[ ] old owner
[ ] new owner
[ ] semantic rationale
[ ] data/contract migration
[ ] authorization
[ ] events/consumers
[ ] docs
[ ] tests/gates
[ ] CODEOWNERS/review routing if applicable
[ ] old path removal
```

# 89. Stop conditions

Stop rather than guess if:

- ownership is chosen from current staffing;
- a new team name is about to become a context/package boundary;
- shared/platform code starts absorbing business policy;
- a product owner bypasses backend/frontend architecture rules;
- a mechanism owner rewrites product semantics for convenience;
- cross-owner work directly writes foreign persistence;
- an ambiguous concept is dumped into Common/Shared without decision;
- CODEOWNERS is treated as architecture authority;
- a reorg triggers context/service split without semantic rationale;
- ownership transfer has no data/contract migration;
- current source placement conflicts with canonical semantic owner and the drift is not classified.

# 90. Related canonical owners

```text
docs/governance/topic-authority-map.md
docs/governance/decision-and-exception-policy.md
docs/architecture/bounded-context-map.md
docs/architecture/capability-extraction-strategy.md
docs/delivery/change-classification.md
docs/delivery/contract-first-delivery.md
docs/delivery/definition-of-done.md
docs/delivery/migration-policy.md
docs/quality/engineering-quality-standard.md
```

# 91. Final ownership rule

For every material change, answer:

```text
Which logical owner owns the product meaning?
Which mechanism owners are affected?
Which architecture owners protect dependency boundaries?
Which consumers/producers require review?
Which security/quality/delivery/operations owners are affected?
Is this review routing or actual semantic ownership?
Would the same owner still make sense after a team reorganization?
Is any Shared/Common placement hiding unresolved ownership?
```

The target is:

> **stable logical ownership that follows product and system meaning, while staffing, contributors, review teams, and deployment responsibilities remain changeable coordination mechanisms rather than accidental architecture.**
