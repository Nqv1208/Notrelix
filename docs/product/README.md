---
document_id: PROD-INDEX
document_type: index-router
status: active
owner: product
applies_to:
  - product
  - repository
evidence:
  - PRODUCT.md
  - DESIGN.md
  - docs/architecture/bounded-context-map.md
  - docs/product/product-model.md
  - docs/product/product-experience.md
  - docs/product/contexts/
review_on:
  - product-documentation-topology-change
  - bounded-context-owner-change
  - product-constitution-change
  - product-experience-owner-change
  - product-context-document-change
---

# Product Documentation

> **This directory defines Notrelix business meaning independently of backend/frontend implementation.**
>
> Read product documentation to determine **what a capability means, who owns it, what lifecycle and invariants apply, and how it relates to other product contexts** before choosing technical placement.

`docs/product/` is the canonical product-semantics layer below the repository-level product constitution.

It is not an implementation handbook.

---

# 1. Product authority model

The product documentation hierarchy is:

```text
PRODUCT.md
    repository-level product constitution
        ↓
docs/product/product-model.md
    cross-context product model and shared semantic rules
        ↓
docs/product/contexts/*.md
    detailed bounded-context semantics
        ↓
backend/frontend implementation docs + source/tests
    technical realization and evidence
```

Product experience is complementary:

```text
DESIGN.md
    repository-level design constitution
        ↓
docs/product/product-experience.md
    cross-capability product experience semantics
        ↓
frontend UI/design implementation docs + source/tests
```

These are distinct authority planes.

---

# 2. What `PRODUCT.md` owns

Root [`../../PRODUCT.md`](../../PRODUCT.md) owns stable product constitution such as:

- product thesis;
- target users;
- product-wide invariants;
- major capability/context set;
- high-level Work Management model;
- product lifecycle principles;
- product extension/change principles.

It intentionally does not contain every context's full semantic contract.

---

# 3. What `product-model.md` owns

[`product-model.md`](product-model.md) owns cross-context product semantics that are too detailed for `PRODUCT.md` but broader than one context.

Examples:

```text
one work model, many views
Account / Identity / Workspace distinction
one authoritative owner per business fact
cross-context reference/write rules
Governance/Billing/authorization relationship
Documents ↔ Work Management separation
Automation/Integrations reaction model
Analytics derived-state principle
supporting capability classification
product extension/admission test
```

It does not own one context's complete lifecycle.

---

# 4. What context documents own

Each file under:

```text
docs/product/contexts/
```

owns one bounded context's:

```text
mission
ubiquitous language
owned concepts
non-owned concepts
invariants
lifecycle
scope
authorization semantics
cross-context contracts
events/facts
deletion/retention
failure/conflict
product journeys
frontend implications
analytics implications
change impact
```

If a context document and `product-model.md` appear to define the same detailed rule independently, ownership must be clarified.

---

# 5. What `product-experience.md` owns

[`product-experience.md`](product-experience.md) owns cross-capability product-experience semantics:

- coherent work experience;
- product versus marketing register;
- calm density;
- language;
- async state truthfulness;
- permission/read-only states;
- accessibility expectations at the product level.

Literal component/token implementation remains frontend-owned.

---

# 6. What this README owns

This README owns only:

```text
navigation
reading paths
document roles
product documentation usage
```

It MUST NOT become another product constitution.

---

# 7. Canonical context set

Notrelix currently recognizes eleven business bounded contexts:

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

The system-level relationship map belongs to:

```text
docs/architecture/bounded-context-map.md
```

The detailed semantics belong here under `contexts/`.

---

# 8. Context files

Target context owners:

```text
contexts/accounts.md
contexts/identity.md
contexts/workspaces.md
contexts/governance.md
contexts/work-management.md
contexts/documents.md
contexts/collaboration.md
contexts/automation.md
contexts/integrations.md
contexts/billing.md
contexts/analytics.md
```

No context file should be added merely to mirror a frontend package, backend folder, table, provider, or team.

---

# 9. Product context versus technical capability

Not every important capability is a business bounded context.

Technical/supporting capabilities include by default:

```text
Search / indexing
Caching
Messaging transport
Realtime transport
Observability
Code generation
Storage
CI/CD
Gateway/proxy
Provider SDK runtime
```

They may become independent deployables.

They do not automatically become product contexts.

---

# 10. Recommended reading path — new product feature

For a new business capability:

```text
1. PRODUCT.md
2. docs/product/product-model.md
3. docs/architecture/bounded-context-map.md
4. owning context document
5. related context documents
6. relevant system architecture
7. backend/frontend implementation docs
8. source/tests/contracts
```

Do not start from the nearest source folder and infer product semantics from it.

---

# 11. Recommended reading path — Work Management

```text
PRODUCT.md
→ product-model.md
→ contexts/work-management.md
→ bounded-context-map.md
→ relevant backend/frontend docs
```

When the change concerns Documents/Collaboration/Automation/Integrations as well, read those owning context docs too.

---

# 12. Recommended reading path — authentication/access

Distinguish first:

```text
Identity
    authentication/session/credential

Workspaces
    membership/invitation

Governance
    permission/policy/sharing

Billing
    entitlement/commercial limit
```

Then read the relevant implementation security docs.

Do not use one generic “auth” document to own all four concerns.

---

# 13. Recommended reading path — UI/experience

```text
DESIGN.md
→ docs/product/product-experience.md
→ owning context doc
→ frontend/docs/architecture/ui-and-design-system.md
→ frontend product/UI source
```

Product experience owns meaning and interaction expectations.

Frontend owns concrete implementation.

---

# 14. Product-to-system relationship

Product says:

```text
what the business means
```

System architecture says:

```text
how owners communicate and stay consistent across boundaries
```

Key system references:

```text
../architecture/system-overview.md
../architecture/bounded-context-map.md
../architecture/contract-boundaries.md
../architecture/data-ownership-and-consistency.md
../architecture/events-realtime-and-delivery-boundary.md
```

Product docs should not duplicate those implementation-neutral architecture mechanics unless the product semantic itself requires it.

---

# 15. Product-to-backend relationship

Backend docs answer:

```text
How are product semantics modeled/enforced in Domain/Application/etc.?
```

Product docs answer:

```text
What semantics must be preserved?
```

A Domain class does not become product authority because it exists.

A product document does not prescribe framework/ORM implementation unless product meaning requires a technical constraint.

---

# 16. Product-to-frontend relationship

Frontend docs answer:

```text
How are product capabilities represented/composed/state-managed across hosts?
```

Product docs answer:

```text
What behavior and semantic state must users experience?
```

A frontend package name does not create a bounded context.

---

# 17. Product-to-ADR relationship

Product/system ownership changes may require:

```text
docs/decisions/SYS-ADR-*.md
```

ADRs explain why.

Current product/context documents still own what the approved product contract is.

Do not use ADRs as daily product handbooks.

---

# 18. Product-to-current-context relationship

`CONTEXT.md` may record that source is transitional.

Context docs may still define the approved target semantics.

When they disagree, classify:

```text
DOC_STALE
SOURCE_DEBT
TRANSITION
CONTRACT_CHANGE
UNRESOLVED
```

Do not silently choose whichever side is easier.

---

# 19. Product documentation principles

Product documentation MUST be:

- semantic;
- ownership-driven;
- lifecycle-aware;
- implementation-independent where possible;
- explicit about cross-context responsibility;
- testable through implementation evidence;
- stable enough to survive refactors/deployment changes.

---

# 20. Product documentation must not be screen-first

Do not define a capability solely by:

- screen;
- route;
- modal;
- table;
- component.

Screens combine product concepts.

The product model must survive UI redesign.

---

# 21. Product documentation must not be database-first

Do not define ownership from:

- table;
- schema;
- FK;
- JSON column;
- index.

Storage supports product semantics.

It does not create them.

---

# 22. Product documentation must not be service-first

Do not define context boundaries from microservice candidates.

A service extraction should follow semantic ownership.

It should not invent it.

---

# 23. Product documentation must not be team-first

Teams may align to bounded contexts.

Teams can also change.

A context exists because product vocabulary/lifecycle/invariants are coherent, not because an org chart has a team box.

---

# 24. Context document admission

Before adding a new context file, prove:

```text
distinct business vocabulary
distinct lifecycle
distinct authoritative state
distinct invariants
distinct mutation authority
clear scope
clear relation to existing contexts
```

Then perform product/system decision governance.

---

# 25. Shared concept admission

Do not introduce global product concepts merely because names match.

Words such as:

```text
Status
Resource
Member
Owner
Permission
Connection
Item
```

can have context-specific meaning.

Share only when semantics are genuinely identical.

---

# 26. One fact, one owner

A context may consume another context's facts.

It may:

- reference;
- cache;
- project;
- react.

It does not become a second mutation authority.

This principle is detailed in `product-model.md` and system architecture.

---

# 27. Cross-context workflow

A multi-context workflow should identify:

```text
primary use-case owner
participant owners
authoritative facts
sync/async boundaries
failure/recovery
user-visible state
```

Do not solve it by shared aggregate object graph.

---

# 28. Product scope vocabulary

Notrelix distinguishes at least:

```text
Identity
Account
Workspace
Resource
```

These words are not interchangeable.

Every context should define which scopes apply.

---

# 29. Authorization vocabulary

Product context declares:

- protected resource;
- operation/action;
- business prerequisites.

Governance/security architecture evaluates access using:

- identity;
- membership;
- policy;
- entitlement;
- resource facts.

Product docs must avoid UI-only authorization assumptions.

---

# 30. Lifecycle vocabulary

Every important product concept should define:

```text
creation
active states
transitions
archive/delete
restore if applicable
terminal states
retention/reference impact
```

Avoid generic lifecycle inheritance across unrelated contexts.

---

# 31. Deletion vocabulary

Deletion is not a database operation alone.

Product docs should distinguish where relevant:

```text
archive
soft delete
hard delete
revoke
disconnect
cancel
disable
remove membership
```

Each has different semantics.

---

# 32. Failure vocabulary

Product contexts should define relevant failures such as:

```text
validation
permission denied
not found
conflict
concurrency
limit/entitlement
provider uncertainty
async pending/failure
```

Do not collapse all failures into generic error.

---

# 33. Async product semantics

If work continues after immediate request success, define user-visible states:

```text
pending
syncing
queued
retrying
failed
completed
```

where material.

Product docs own what those states mean.

Backend/frontend own implementation.

---

# 34. Concurrency product semantics

Where concurrent edits matter, define whether the product expects:

- conflict;
- merge;
- latest wins;
- field-level behavior;
- user intervention.

Infrastructure should not invent product conflict policy.

---

# 35. Realtime product semantics

Product docs define which facts users expect to converge quickly.

System/frontend docs define delivery/reconciliation mechanics.

Realtime must not create a separate lifecycle model.

---

# 36. Analytics relationship

Product contexts own source facts.

Analytics owns metrics/reporting semantics.

Source context docs should identify analytics-relevant facts where important, but should not own Analytics aggregation implementation.

---

# 37. Automation relationship

Source contexts own trigger facts/actions they expose.

Automation owns rule/trigger/condition/action product semantics.

Target context still validates automated mutations.

---

# 38. Integrations relationship

Source contexts own Notrelix product facts.

Integrations owns provider connection/mapping/sync semantics.

Provider vocabulary must be translated.

---

# 39. Collaboration relationship

Source resource context owns resource state.

Collaboration owns comments/activity/notification/presence semantics.

A comment target reference is not resource ownership.

---

# 40. Governance relationship

Governance owns policy/permission/sharing/audit semantics.

Protected contexts own resource state and business invariants.

Governance does not become a universal product aggregate.

---

# 41. Billing relationship

Billing owns subscription/entitlement/usage/commercial lifecycle.

A product context may require an entitlement.

That does not move its business state into Billing.

---

# 42. Product facts and events

Context docs should identify durable facts worth communicating.

They should not prescribe queue/broker/envelope implementation.

System event taxonomy owns that boundary.

---

# 43. Product facts and API

Context docs define semantic operations/resources.

Backend API docs own HTTP shape/conventions.

Do not make route design the product model.

---

# 44. Product facts and UI

Context docs define semantic state/actions.

UI can expose those via:

- table;
- board;
- form;
- timeline;
- editor;
- dashboard;
- mobile-native interaction.

Presentation choice does not create a second model.

---

# 45. Product evidence

Strong evidence includes:

```text
Domain behavior tests
Application/integration tests
public contracts
frontend capability tests
E2E workflows
architecture tests
```

Source/test evidence proves implementation.

Product docs define intended semantics.

---

# 46. Context-document expected structure

Each context document should usually include:

```text
1. Mission
2. Owns
3. Does not own
4. Scope / tenant model
5. Ubiquitous language
6. Core concepts
7. Invariants
8. Lifecycle
9. Authorization semantics
10. Cross-context relations
11. Product facts/events
12. Failure/conflict
13. Deletion/retention
14. Critical journeys
15. Frontend implications
16. Analytics/reporting implications
17. Change/migration
18. Evidence/testing
19. Stop conditions
```

Not every file requires equal length.

Depth follows semantic risk.

---

# 47. Context docs are not implementation inventories

Do not list every:

- command;
- endpoint;
- React component;
- table;
- event class

unless the item itself is part of stable product semantics.

Exact inventories belong to source/generated evidence.

---

# 48. Context docs and current incomplete features

A context document may define approved semantics before every feature is implemented.

It MUST distinguish product contract from current source state when this matters.

Do not downgrade the semantic model merely because current implementation is partial.

---

# 49. Context docs and future ideas

Do not place speculative roadmap features into active context semantics as if committed.

Use:

- issue/roadmap;
- draft proposal;
- ADR/product decision process.

Active product docs describe approved product contract.

---

# 50. Product rule IDs

Stable cross-context product rules may use:

```text
PROD-*
```

Context-local stable rules may use a context-specific extension where useful.

Do not assign IDs to every sentence.

IDs exist for cross-reference and governance value.

---

# 51. Product change classification

A product semantic change includes:

- context ownership change;
- lifecycle change;
- invariant change;
- meaning of existing state;
- authorization meaning;
- deletion/retention meaning;
- cross-context fact ownership;
- user-visible consistency promise.

It is not “docs only” merely because code comes later.

---

# 52. Product change workflow

For material product semantics:

```text
identify owner
→ update product contract
→ ADR if consequential
→ assess backend/frontend/contracts/data
→ migration
→ tests/evidence
→ update routers/related docs
```

---

# 53. Context move

Moving a fact between contexts is high-risk.

Review:

```text
data
API
events
authorization
frontend cache/state
analytics
deletion
migration
service extraction
```

Use system ADR.

---

# 54. Context split/merge

Context split/merge changes product architecture.

It must not be hidden inside namespace/package refactor.

Update:

```text
PRODUCT.md if constitution changes
bounded-context-map
product-model
affected context docs
topic-authority-map
ADRs
implementation/migration
```

---

# 55. Product naming change

Renaming a product noun is not always editorial.

If user/business meaning changes or public contract depends on it, treat as semantic migration.

Internal class rename alone is implementation.

---

# 56. Legacy product documentation migration

The old product generation may contain durable knowledge.

Migration rule:

```text
retain durable semantics
→ move to product-model/context/product-experience owner
→ move system mechanics to docs/architecture
→ discard stale progress/authority metadata
→ remove old canonical path
```

Do not retain old and new product trees as parallel authority.

---

# 57. No permanent cross-context-workflows handbook by default

Cross-context workflow semantics are intentionally split by ownership:

```text
product-model.md
    product-level owner/fact relationships

bounded-context-map.md
    context relationships

data-ownership-and-consistency.md
    consistency/orchestration mechanics

events-realtime-and-delivery-boundary.md
    event/delivery semantics

context docs
    capability-specific workflow meaning
```

A separate generic workflow handbook would duplicate these owners unless a genuinely distinct topic emerges.

---

# 58. Product extension test

Before accepting a product feature, answer:

```text
Whose vocabulary is this?
Who owns lifecycle?
What state is authoritative?
Which Account/Workspace/resource scope applies?
What authorization applies?
Which existing context owns it?
What cross-context contracts are needed?
What failure/conflict states exist?
What deletion/retention applies?
What frontend owner/state exists?
What migration/events/realtime result?
What evidence proves correctness?
```

A new screen/table/team/package is not enough reason for a new context.

---

# 59. Product review stop conditions

Stop rather than invent if:

- two contexts claim the same authoritative fact;
- Account/Identity/Workspace meaning is ambiguous;
- deletion owner is unclear;
- UI screen is being used as product model;
- provider vocabulary is replacing Notrelix vocabulary;
- Governance/Billing are being used as generic owners;
- a supporting technical capability is being promoted to context without decision;
- product semantic change lacks migration impact;
- current source conflicts with active product contract and drift classification is unresolved.

---

# 60. Product documentation completion standard

The product documentation layer is healthy when:

- `PRODUCT.md` remains a concise constitution relative to detailed context docs;
- `product-model.md` owns cross-context product semantics;
- every accepted context has one detailed owner;
- experience semantics route through DESIGN/product-experience;
- implementation docs do not redefine business meaning;
- source/test drift is classified rather than normalized;
- technical capabilities are not mistaken for contexts;
- cross-context workflows preserve one authoritative owner;
- product changes have explicit migration/evidence.

---

# 61. Directory target

```text
docs/product/
├── README.md
├── product-model.md
├── product-experience.md
└── contexts/
    ├── accounts.md
    ├── identity.md
    ├── workspaces.md
    ├── governance.md
    ├── work-management.md
    ├── documents.md
    ├── collaboration.md
    ├── automation.md
    ├── integrations.md
    ├── billing.md
    └── analytics.md
```

This tree is semantic.

It is not required to mirror backend/frontend folder topology.

---

# 62. Final routing rule

When implementing a product change:

```text
do not ask first:
"where is the code?"

ask first:
"what does this mean and who owns it?"
```

Then use:

```text
PRODUCT.md
→ product-model
→ owning context
→ system architecture
→ project architecture
→ source/tests
```

That is the intended product-to-engineering reading path.
