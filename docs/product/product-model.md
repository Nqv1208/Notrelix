---
document_id: PROD-MODEL
document_type: product-context
status: active
owner: product
applies_to:
  - product
  - repository
  - all-bounded-contexts
evidence:
  - PRODUCT.md
  - DESIGN.md
  - docs/architecture/system-overview.md
  - docs/architecture/bounded-context-map.md
  - docs/architecture/contract-boundaries.md
  - docs/architecture/data-ownership-and-consistency.md
  - docs/architecture/events-realtime-and-delivery-boundary.md
  - backend/src/Notrelix.Domain/
  - backend/src/Notrelix.Application/
  - frontend/packages/
review_on:
  - product-constitution-change
  - bounded-context-owner-change
  - work-model-change
  - account-identity-workspace-scope-change
  - authorization-product-model-change
  - cross-context-workflow-change
  - lifecycle-or-deletion-semantics-change
  - supporting-capability-classification-change
---

# Product Model

> **Notrelix is an enterprise work-management workspace operating system in which business capabilities share one coherent product while preserving explicit semantic ownership.**
>
> The product model is designed so a user can move between work, documents, collaboration, automation, integrations, governance, and reporting without those capabilities becoming one undifferentiated domain.

This document owns **cross-context product semantics**.

Root `PRODUCT.md` remains the product constitution.

Detailed context lifecycle/invariants belong to `docs/product/contexts/*.md`.

System architecture owns consistency, delivery, contracts, and extraction mechanics.

---

# 1. Product thesis

Notrelix exists to let teams model and execute knowledge work in one coherent environment.

The product combines:

```text
administrative ownership
identity
collaboration tenants
governed access
structured work
documents
collaboration
automation
external integrations
commercial entitlement
analytics/reporting
```

without requiring each capability to invent its own duplicate user/workspace/work/resource model.

---

# 2. Product identity

Notrelix is not defined as a clone or visual synthesis of another product.

Comparable products may help explain familiar interaction patterns.

They do not own Notrelix semantics.

The product identity is:

```text
coherent
calm
focused
powerful
explicitly governed
designed for sustained work
```

Experience principles are defined by `DESIGN.md` and `product-experience.md`.

---

# 3. Product model principles

The cross-context model is governed by these principles:

```text
business meaning before implementation
one authoritative owner per fact
scope is explicit
views are projections, not duplicate truth
cross-context references preserve ownership
authorization is server-authoritative
commercial entitlement is distinct from permission
technical capabilities do not automatically become product contexts
async/provider uncertainty is truthful product state
deletion/retention are semantic lifecycle decisions
```

---

# 4. PROD-001 — Product semantics outrank implementation convenience

When a source structure, provider model, UI component, or database schema conflicts with approved product meaning, implementation convenience does not redefine the product automatically.

Changes to approved product semantics require product/change governance.

---

# 5. Canonical context set

The accepted business bounded contexts are:

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

This set is semantic.

It does not imply eleven services, eleven backend projects, or eleven frontend packages.

---

# 6. Product contexts are lifecycle boundaries

A bounded context owns concepts whose:

- vocabulary;
- lifecycle;
- invariants;
- mutation authority

are coherent.

If two concepts share an ID/table/screen but have different lifecycles, they may belong to different contexts.

---

# 7. PROD-002 — One authoritative owner per business fact

A business fact has one semantic mutation owner.

Other contexts may:

- reference;
- read;
- cache;
- project;
- react;
- aggregate.

They do not become co-owners.

Examples:

```text
Workspace membership
→ Workspaces

permission policy
→ Governance

BoardItem data
→ Work Management

Page/Block content
→ Documents

subscription/entitlement
→ Billing

metric definition
→ Analytics
```

---

# 8. Authoritative versus derived product state

Derived state can be important and durable.

Examples:

```text
search index
analytics aggregate
dashboard projection
notification projection
frontend query cache
```

Its importance does not make it source truth.

Every derived representation must retain a source owner.

---

# 9. PROD-003 — Foreign references do not transfer ownership

A resource may contain:

```text
AccountId
WorkspaceId
UserId
BoardId
PageId
```

without belonging to those referenced contexts.

Ownership is semantic, not determined by FK direction.

---

# 10. Core scope model

Notrelix distinguishes:

```text
Identity
Account
Workspace
Resource
```

These are not synonyms.

They may coexist in one operation.

---

# 11. Identity

Identity answers:

```text
Who/what is the principal?
How is it authenticated?
What credential/session security state exists?
```

Identity does not automatically own membership or permission.

---

# 12. Account

Account is an administrative/commercial ownership scope.

It may group:

- customer administration;
- Workspaces;
- subscription relationship;
- account-level policy/integration facts.

Account is not merely another name for Workspace.

---

# 13. Workspace

Workspace is a collaboration/work tenant scope.

It owns:

- Workspace lifecycle;
- membership;
- invitation.

Many resources are Workspace-scoped without being Workspaces-owned.

---

# 14. Resource

A resource is a protected product object owned by a context.

Examples:

```text
Board
BoardItem
Page
Automation rule
Integration connection
```

Resource scope participates in authorization.

The generic word does not erase specific context semantics.

---

# 15. PROD-004 — Account, Identity, and Workspace remain distinct

Do not:

- model Identity as mutable child of one Workspace;
- invent fake Workspace IDs for account/global operations;
- use Account and Workspace interchangeably;
- store product authorization solely as Identity-global roles.

This distinction must survive backend/frontend refactors.

---

# 16. Product ownership graph

At a high level:

```text
Identity
    supplies principal identity

Accounts
    supplies administrative/commercial ownership scope

Workspaces
    supplies collaboration tenant/membership

Governance
    supplies access policy/sharing/audit semantics

Work Management / Documents
    own core user work/content state

Collaboration
    attaches collaborative communication/history

Automation
    reacts and performs approved actions

Integrations
    connects/synchronizes external providers

Billing
    supplies commercial entitlement/usage

Analytics
    derives product metrics/reporting
```

This is a semantic graph, not runtime call graph.

---

# 17. Work Management model

Work Management owns structured work.

The core model includes:

```text
Board
BoardField
BoardItem
BoardGroup
BoardView
```

These concepts must not be reduced to a Kanban-only model.

---

# 18. PROD-005 — Work Management is not Kanban CRUD

A Board is a flexible work-data model.

Conceptually:

```text
Board
    contains schema + organization

BoardField
    defines work-data schema

BoardItem
    stores work records/values

BoardGroup
    structures/organizes items

BoardView
    configures how shared work data is presented
```

Kanban is one view of the same work model.

---

# 19. One work model, many views

Views include or may include:

```text
Table
Kanban
Calendar
Timeline
Form
Dashboard
```

They operate over shared underlying work data.

A view does not own a duplicate copy of BoardItems.

---

# 20. PROD-006 — Views do not own duplicate work data

Changing an item through one view updates the same authoritative item/field value.

Other views eventually converge to that state according to server/realtime consistency semantics.

Do not create:

```text
KanbanItem
TableRow
CalendarEvent
```

as independent authoritative copies of one BoardItem merely because presentation differs.

---

# 21. View configuration versus work data

A `BoardView` may own configuration such as:

- grouping;
- filtering;
- sorting;
- visible fields;
- visualization configuration.

It does not become owner of BoardItem business values.

---

# 22. BoardGroup

`BoardGroup` is a structural grouping concept.

It is not the universal Work Management status model.

---

# 23. PROD-007 — BoardGroup is not the universal Kanban status

Kanban columns may derive from a configured grouping field/value.

BoardGroup may represent board structure independently.

Do not collapse:

```text
group
status
kanban column
```

into one concept for UI convenience.

---

# 24. Dynamic fields

Work Management supports flexible field schema.

A field system should preserve:

- type meaning;
- validation;
- display/edit behavior;
- filter/sort semantics;
- formula/relation behavior where applicable;
- migration/evolution.

Dynamic does not mean semantically untyped.

---

# 25. Field value ownership

BoardItem field values belong to Work Management.

Analytics/search may project/index them.

Integrations may map external values.

Frontend may cache/render them.

Those consumers remain non-authoritative.

---

# 26. Forms

A Form is an input/view surface over Work Management schema/operations.

It should create/update authoritative work state through Work Management contracts.

It is not an independent data store.

---

# 27. Dashboard

Dashboard may visualize:

- Work Management;
- Analytics;
- multiple contexts.

A dashboard widget does not automatically own source data.

If a dashboard action mutates a resource, it routes to the source owner.

---

# 28. Calendar/Timeline

Calendar/Timeline represent temporal semantics of underlying work records/views.

They must not create competing record identity.

Provider calendar synchronization belongs to Integrations.

---

# 29. Documents model

Documents owns:

```text
Page
Block
hierarchy
document content
document lifecycle
```

Documents is intentionally separate from Work Management.

---

# 30. PROD-008 — Documents and Work Management are separate semantic capabilities

Documents owns authored hierarchical content.

Work Management owns structured work records/schema.

They may link/embed/reference each other through stable contracts.

Do not merge by storing:

```text
BoardItem as document block
or
Document content as board internals
```

as the default product model.

---

# 31. Cross-capability links

Allowed examples:

```text
Page references Board
Block embeds work resource
BoardItem references Page
```

These are relationships between independently owned objects.

Deletion/reference semantics must remain explicit.

---

# 32. Collaboration model

Collaboration owns:

```text
comments
threads
mentions
reactions
notifications
user-facing activity
presence
```

where those semantics are product-approved.

It does not own the target resources.

---

# 33. PROD-009 — Collaboration attaches to resources without taking ownership

A Comment on a BoardItem is Collaboration state targeting a Work Management resource.

A Comment on a Page remains Collaboration state targeting a Documents resource.

Target references preserve source ownership.

---

# 34. Activity

User-facing activity answers:

```text
What happened that collaborators should understand?
```

It is not a security audit log.

---

# 35. Notification

Notification answers:

```text
Which product fact requires this user's attention?
```

Email/push/in-app are delivery channels.

Delivery mechanism is not the product notification semantic.

---

# 36. Presence

Presence is ephemeral collaboration state.

It should not become durable content truth.

Presence must remain resource/scope-aware.

---

# 37. Governance model

Governance owns:

```text
permission semantics
resource policy
sharing
guest/public access semantics
governance/security audit meaning
```

Protected contexts still own business state.

---

# 38. PROD-010 — Governance is cross-cutting authority, not scattered role checks

Product contexts declare:

- protected resources;
- operations/actions;
- relevant business preconditions.

Authorization evaluates:

- principal;
- membership;
- policy;
- entitlement;
- resource facts.

Do not encode a second permission model independently in each UI/handler.

---

# 39. UI permission state

Frontend permission guards improve:

- affordance;
- discoverability;
- read-only presentation.

They do not authorize the operation.

Server/Application remains authoritative.

---

# 40. Membership versus permission

Workspaces owns membership.

Governance owns permission/policy semantics.

Membership can be an input to permission.

They remain different facts.

---

# 41. Entitlement versus permission

Billing owns commercial entitlement/limit.

Governance owns resource/action access policy.

An operation may require both.

Do not merge them into one generic authorization table.

---

# 42. PROD-011 — Commercial availability and authorization are distinct

Ask separately:

```text
Is this capability available under the plan/usage limit?
```

and:

```text
May this principal perform this action on this resource?
```

Billing and Governance remain distinct owners.

---

# 43. Automation model

Automation owns:

```text
rule
trigger
condition
action
schedule
execution identity/lifecycle
```

Automation consumes product facts and invokes product actions.

---

# 44. PROD-012 — Automation reacts through approved contracts

A Work Management fact may trigger Automation.

Automation may later invoke Work Management, Documents, Collaboration, Integrations, etc.

The target context still validates the action.

Automation does not directly mutate foreign persistence.

---

# 45. Automation execution

Execution has its own product lifecycle when user-visible/operationally meaningful.

Possible states may include:

```text
scheduled
pending
running
succeeded
failed
cancelled
```

Exact semantics belong to Automation context.

---

# 46. Automation idempotency

A retry of one automation execution should not unintentionally duplicate a target effect.

Stable execution/operation identity is part of the product/reliability contract.

---

# 47. Automation recursion

Automation must define whether one automation-generated action can trigger another or itself.

Loop prevention/throttling is product semantics, not merely queue configuration.

---

# 48. Integrations model

Integrations owns:

```text
provider connection
connection lifecycle
webhook semantic intake
mapping
synchronization
provider revision/conflict relationship
```

It translates external provider semantics into Notrelix semantics.

---

# 49. PROD-013 — Provider vocabulary does not become Notrelix product vocabulary automatically

External provider:

```text
status
event
resource
permission
```

must be translated.

Provider SDK DTOs do not define Notrelix Domain models.

---

# 50. OAuth classification

OAuth used for:

```text
authenticating/linking user identity
→ Identity
```

OAuth used for:

```text
connecting external provider
→ Integrations
```

Protocol alone does not determine product owner.

---

# 51. Provider connection

A provider connection is product state.

Provider credentials/token storage mechanics are implementation/security concerns.

Do not confuse the two.

---

# 52. Synchronization

Sync relates independently authoritative systems.

For every synchronized fact, define where possible:

```text
Notrelix authoritative
provider authoritative
mergeable
conflict requiring user/policy
```

“Two-way sync” is incomplete without per-fact ownership.

---

# 53. Provider uncertainty

External result can be:

```text
success
known failure
unknown
```

Unknown should not be falsely shown as terminal failure when duplicate retry is possible.

---

# 54. Billing model

Billing owns:

```text
plan
subscription
entitlement
usage
commercial limit
commercial lifecycle
```

It does not own arbitrary protected product state.

---

# 55. PROD-014 — Billing controls commercial capability without taking product ownership

If a plan enables:

```text
Automation
Analytics
Integrations
```

the capability remains owned by its product context.

Billing owns the entitlement/limit fact.

---

# 56. Usage

Usage metrics used for commercial limits must have clear semantic definition.

Do not reuse arbitrary observability counters as billing truth without explicit contract.

---

# 57. Downgrade semantics

A downgrade may create product states such as:

- feature becomes read-only;
- new creation disabled;
- excess resources retained;
- scheduled automation paused.

These consequences must be product-defined.

Do not let provider subscription status directly decide deletion.

---

# 58. Payment failure

Commercial failure does not automatically mean immediate destructive resource deletion.

Billing context owns commercial lifecycle.

Affected product contexts define safe access/degradation implications with Billing/Governance policy.

---

# 59. Analytics / Reporting model

Analytics owns:

```text
metric definitions
aggregations
reports
analytical dashboard/widget semantics
freshness semantics
```

It consumes source-owned facts.

---

# 60. PROD-015 — Analytics is derived insight, not source mutation authority

Analytics may materialize durable projections.

Those projections remain derived.

A user action from a report/dashboard that changes a resource must invoke the source context.

---

# 61. Operational telemetry versus Analytics

Do not confuse:

```text
logs/metrics/traces
→ Operations
```

with:

```text
product metric/report
→ Analytics
```

Shared infrastructure does not merge semantics.

---

# 62. Search

Search/indexing is a supporting capability by default.

It may index several contexts.

Search result data remains derived.

---

# 63. PROD-016 — Search does not become a business context merely because a package/service exists

A frontend `features-search` package or future Search service is technical architecture evidence.

A business context requires distinct product vocabulary/lifecycle/invariants.

---

# 64. Supporting technical capabilities

By default these are not product contexts:

```text
Search/indexing
Caching
Messaging
Realtime transport
Observability
Codegen
Storage
CI/CD
Gateway
provider SDK runtime
```

They may be architecturally significant.

Product context admission remains semantic.

---

# 65. Product-level cross-context workflow

A workflow spanning contexts must identify:

```text
initiating product intent
primary use-case owner
participant owners
authoritative facts
required synchronous facts
eventual reactions
user-visible pending/failure semantics
```

---

# 66. PROD-017 — Cross-context write is never shared aggregate mutation

If Work Management causes Collaboration activity or Automation trigger:

```text
Work Management
→ commits owned fact
→ approved contract/event
→ Collaboration/Automation reacts
```

Do not share one mutable aggregate/object graph across contexts.

---

# 67. Synchronous external fact

Before mutation, a context may synchronously require a fact such as:

```text
membership
permission
entitlement
target existence
```

The owning context still owns that fact.

The consumer must not edit it.

---

# 68. Asynchronous downstream reaction

Use when source success can stand independently and downstream result may lag.

Example:

```text
BoardItem changed
→ Automation reacts later
```

The product must define what users observe during lag if material.

---

# 69. PROD-018 — Strong cross-context consistency is exceptional

When one operation truly needs atomic facts from multiple owners, document:

```text
business invariant
transaction owner
why eventual consistency is invalid
failure/rollback
future extraction impact
```

Shared database convenience is not sufficient.

---

# 70. Process manager

A durable multi-context workflow may require explicit process state.

Process manager owns workflow progress.

It does not own participant business state.

---

# 71. Compensation

Compensation is a new business action after prior commit.

It is not magical rollback of another context.

Product semantics determine whether compensation exists.

---

# 72. Example — comment on BoardItem

Owners:

```text
BoardItem
→ Work Management

Comment
→ Collaboration

Identity
→ Identity

Membership
→ Workspaces

Permission
→ Governance
```

A comment workflow validates target/access then creates Collaboration-owned state.

---

# 73. Example — automation from BoardItem change

```text
Work Management
    commits BoardItem change

Automation
    consumes approved trigger fact

Automation execution
    owns rule/execution lifecycle

target action
    invokes target context contract
```

No foreign repository mutation.

---

# 74. Example — provider sync

```text
Integrations
    owns connection/mapping/sync

Work Management
    owns BoardItem

provider
    owns external object
```

Sync conflict policy must state which side is authoritative per fact.

---

# 75. Example — entitlement-protected action

```text
Billing
    entitlement

Workspaces
    membership

Governance
    permission

Work Management
    resource/action
```

All can participate without merging ownership.

---

# 76. Example — Analytics dashboard

```text
Work Management
Documents
Collaboration
Automation
Billing
    source facts

Analytics
    derives metrics/report model
```

Dashboard remains derived unless it invokes source actions.

---

# 77. Product lifecycle principles

Each owner defines lifecycle.

Cross-context product model enforces:

```text
no universal Status
no universal deletion state
no hidden provider lifecycle
no UI-only lifecycle
```

---

# 78. PROD-019 — Lifecycle names are context language

The word `Status` may mean different things in:

- Work Management field value;
- Billing subscription;
- Automation execution;
- Integration connection.

Do not create one shared global status enum.

---

# 79. Creation

Creation must establish:

- owner;
- required scope;
- required identity/access;
- initial valid state.

A database insert alone is not the product definition of creation.

---

# 80. Update

Update must respect:

- invariants;
- authorization;
- concurrency;
- lifecycle;
- entitlement where relevant.

Frontend optimistic update is provisional until server commit.

---

# 81. Archive

Archive means resource remains retained but becomes inactive/hidden/restricted according to context policy.

Not every context needs archive.

---

# 82. Soft delete

Soft delete is implementation/product lifecycle only where the context defines it.

Do not impose universal `SoftDeleted` state.

---

# 83. Hard delete

Hard delete is irreversible data removal and requires explicit product/retention/reference policy.

Do not use raw cascade behavior as product semantics.

---

# 84. PROD-020 — Deletion policy belongs to semantic owner

For each owned resource, define:

```text
who may delete/archive
what references do
what history/audit remains
whether restore exists
what downstream contexts do
```

Database mechanics implement the policy.

---

# 85. Cross-context deletion

Deleting a Workspace, user, Board, Page, etc. can affect many contexts.

The source owner does not simply mutate every downstream aggregate.

Use explicit workflow/reaction/retention semantics.

---

# 86. User deletion

Identity owns Identity lifecycle.

Other contexts decide their own reaction:

```text
Workspaces
    membership/reference

Collaboration
    historical attribution

Governance
    audit/policy references

Integrations
    user-linked provider state

Accounts/Billing
    administrative/commercial references
```

---

# 87. Workspace deletion/archive

Workspaces owns Workspace lifecycle.

Scoped contexts define whether their data:

- archives;
- deletes;
- retains;
- becomes inaccessible;
- triggers downstream cleanup.

SQL cascade is not the product contract.

---

# 88. Failure model

Important product failures include:

```text
validation
permission denied
not found
conflict
concurrency conflict
entitlement/limit
provider known failure
provider unknown outcome
async terminal failure
temporary connectivity/retry
```

Different failures can require different UX and recovery.

---

# 89. PROD-021 — Failure semantics are user/product semantics

Do not collapse all failures into generic “Something went wrong” when recovery differs.

Product context should define enough semantics for backend/frontend to present correct next action.

---

# 90. Validation failure

Means proposed state violates input/business precondition before commit.

The UI should normally preserve user work and point to correctable input.

---

# 91. Permission denied

Means the principal cannot perform the operation.

Do not show creation/edit controls as if available when known unavailable.

Still enforce server-side.

---

# 92. Not found

May mean:

- resource absent;
- inaccessible resource intentionally indistinguishable for security.

API/frontend semantics should preserve the approved security policy.

---

# 93. Conflict

Can mean:

- lifecycle conflict;
- uniqueness;
- provider conflict;
- business competing state.

Do not equate every conflict with HTTP/DB technical conflict.

---

# 94. Concurrency conflict

Represents stale competing write where product does not permit silent overwrite.

Product decides whether user:

- refreshes;
- retries;
- merges;
- resolves conflict.

---

# 95. Entitlement/limit

Can mean:

- feature unavailable;
- usage limit reached;
- plan downgrade constraint.

It is distinct from permission denied.

---

# 96. Provider unknown outcome

Product may need:

```text
pending reconciliation
```

rather than success/failure.

This is important for external side effects.

---

# 97. Async pending

When work is accepted for later processing, accepted does not necessarily mean completed.

The product should name the state honestly.

---

# 98. Retryable versus terminal

Product/Integration semantics decide whether retry can change the result safely.

Transport layer should not guess this solely from exception type.

---

# 99. Product consistency promise

A product operation should define what the user may assume after success.

Examples:

```text
resource durable now
downstream notification pending
provider sync pending
analytics may lag
```

This prevents false instant-consistency promises.

---

# 100. PROD-022 — User-visible consistency matches actual consistency

If downstream work is eventual, the product must not imply every dependent system is already final.

Use pending/syncing/reconnecting/stale states where material.

---

# 101. Realtime convergence

Realtime improves freshness.

It does not change ownership.

If realtime is uncertain, query state restores authoritative truth.

---

# 102. Offline/provisional client state

A client may retain provisional local state where product supports it.

The context must define conflict/reconciliation expectations.

Offline state must not silently bypass server authorization or invariants.

---

# 103. Product authorization model

Authorization combines several distinct facts:

```text
principal identity
membership
resource scope
permission/policy
entitlement where relevant
resource lifecycle/business constraint
```

No single generic role string defines all product access.

---

# 104. PROD-023 — Product context declares resource/action vocabulary

Each protected context should define its important:

- resource kinds;
- actions;
- ownership/relationship facts.

Security implementation then evaluates them.

This keeps authorization aligned with product language.

---

# 105. Sharing

Sharing may include:

- member access;
- guest access;
- resource-specific policy;
- public/share-link behavior.

Governance owns access semantics.

Source context remains resource owner.

---

# 106. Public/share link

Public access must be an explicit product/security state.

Do not treat possession of raw resource ID as public access.

---

# 107. Plan gating

Plan gating should not leak commercial provider details into every product context.

Product feature checks consume Billing entitlement semantics.

---

# 108. Product extension categories

A proposed addition generally falls into one of:

```text
new concept inside existing context
new cross-context workflow
new view/presentation
new supporting technical capability
new bounded context
new integration/provider
new commercial entitlement
new analytical projection
```

Classify before implementation.

---

# 109. New concept inside existing context

Default when vocabulary/lifecycle/invariants clearly belong to one current owner.

Do not create a new context simply because concept is substantial.

---

# 110. New view

A new view over existing Work Management data generally remains:

```text
BoardView / Work Management presentation
```

unless it introduces independent business state/lifecycle.

---

# 111. New supporting capability

Search, cache, realtime gateway, etc. can become technically substantial without becoming business contexts.

Route architecture to system/project docs.

---

# 112. New provider integration

A new external provider normally extends Integrations plus affected source contexts.

The provider itself does not become a bounded context automatically.

---

# 113. New bounded context

Requires strong evidence of:

```text
distinct vocabulary
lifecycle
authoritative state
invariants
mutation authority
cross-context boundary
```

and system ADR/change migration.

---

# 114. PROD-024 — New screen/table/package/team is not context evidence

These are implementation/organization artifacts.

They may support a context.

They do not define one.

---

# 115. Product extension test

Before approving a new capability, answer:

```text
Whose vocabulary names it?
Which context owns lifecycle?
What is authoritative state?
Which Account/Workspace/resource scope applies?
What authorization applies?
What commercial entitlement applies?
What external facts are required?
What cross-context writes/events exist?
What failure/conflict states exist?
What delete/retention semantics apply?
What frontend state owner exists?
What migration/realtime/analytics implications exist?
What evidence proves correctness?
```

---

# 116. Product experience relationship

Product model owns semantic behavior.

`product-experience.md` owns cross-capability experience quality.

`DESIGN.md` owns repository design constitution.

Example:

```text
Work Management
    defines concurrency conflict semantics

product-experience
    defines conflict must be presented truthfully/recoverably

frontend
    implements exact dialog/inline UI
```

---

# 117. Language

User-facing product language should use context vocabulary.

Avoid exposing:

- CLR class names;
- EF terms;
- queue concepts;
- provider-specific jargon;
- internal permission identifiers

unless they are intentionally user concepts.

---

# 118. Calm complexity

Notrelix may have enterprise-level complexity.

Product semantics should remain understandable.

Do not hide ambiguity behind generic “advanced settings”.

Precise language is part of correctness.

---

# 119. Product facts and contracts

A product context may expose facts through:

```text
REST
integration event
realtime
generated client
```

The context owns meaning.

System contract docs own compatibility/transport boundaries.

---

# 120. Stable product identity

Internal code names can evolve.

Stable public product noun meanings should not change accidentally with refactors.

Renaming a CLR type is not a product migration.

---

# 121. Product facts and analytics

A source context should expose/retain enough stable facts for Analytics where product need exists.

Do not emit implementation-noise events solely because metrics may someday need them.

---

# 122. Product facts and automation

A source context should expose approved triggers/actions, not internal setter events.

Automation should depend on stable product facts.

---

# 123. Product facts and Collaboration

Collaboration can target stable product resource identities.

Source context must not expose full aggregate internals just to support comments/activity.

---

# 124. Product facts and Integrations

Integrations should use stable operations/contracts rather than direct persistence.

Provider mappings may outlive implementation refactors.

---

# 125. Product facts and Governance

Protected contexts must supply enough resource/action identity for authorization.

Do not let Governance infer business operations from endpoint names alone.

---

# 126. Product facts and Billing

Contexts requiring paid capability consume stable entitlement/usage semantics.

Do not query provider billing SDK directly from business aggregates.

---

# 127. Migration of product semantics

Material product changes may require migration of:

```text
stored data
API
events
frontend state
permissions
entitlements
analytics
provider mapping
documentation
```

A semantic rename can be more expensive than a schema rename.

---

# 128. Product compatibility

First-party web may update quickly.

Mobile/external/provider consumers may not.

Product semantics exposed publicly need compatibility windows aligned with consumers.

---

# 129. Transitional source

A context may be semantically correct in docs while source placement is transitional.

New work should generally follow approved owner unless an active exception/transition says otherwise.

Do not use transitional source as precedent.

---

# 130. Product evidence model

Evidence should prove the product contract at appropriate levels.

Examples:

```text
Domain tests
    invariants/lifecycle

Application/integration tests
    authorization/workflow/transaction

API/contract tests
    public behavior

frontend tests
    user state/interaction

E2E
    critical journeys

architecture tests
    ownership/dependency constraints
```

---

# 131. Product tests are not only happy path

Critical semantics should test:

- invalid transition;
- authorization denial;
- concurrency;
- deletion/reference;
- retry/idempotency;
- async failure;
- provider uncertainty;
- cross-context ownership.

---

# 132. Product change review questions

```text
Did business meaning change?
Did owner change?
Did lifecycle change?
Did scope change?
Did permission/entitlement meaning change?
Did consistency promise change?
Did deletion/retention change?
Did public contract meaning change?
Did provider mapping change?
Did client conflict/recovery change?
```

---

# 133. Product-model stop conditions

Stop rather than guess when:

- one fact has two owners;
- Account/Workspace/Identity scope is collapsed;
- BoardView is becoming a duplicate data store;
- BoardGroup is treated as universal status;
- Documents and Work Management are being merged for convenience;
- Governance is being made owner of protected business state;
- Billing entitlement is treated as permission;
- provider schema is becoming Domain language;
- Analytics/Search is being used as source truth;
- cross-context writes require direct repository access;
- deletion is defined only by DB cascade;
- a new context is proposed only because a package/service exists.

---

# 134. Context relationship summary

| Context | Supplies/owns | Common consumers |
|---|---|---|
| Accounts | administrative/commercial account scope | Workspaces, Billing, Governance, Integrations |
| Identity | principal/authentication identity | all protected contexts |
| Workspaces | collaboration tenant/membership | work/content/collaboration/governance |
| Governance | permission/policy/sharing/audit | all protected operations |
| Work Management | structured work facts | Collaboration, Automation, Integrations, Analytics |
| Documents | document facts | Collaboration, Automation, Integrations, Analytics |
| Collaboration | comments/activity/notification/presence | users, Automation/Analytics where approved |
| Automation | automation definition/execution | target context actions |
| Integrations | provider connection/mapping/sync | source contexts/providers |
| Billing | commercial entitlement/usage | Governance/product capability gating |
| Analytics | metrics/reporting projections | users/product surfaces |

This is semantic orientation, not a call graph.

---

# 135. Detailed context owners

```text
docs/product/contexts/accounts.md
docs/product/contexts/identity.md
docs/product/contexts/workspaces.md
docs/product/contexts/governance.md
docs/product/contexts/work-management.md
docs/product/contexts/documents.md
docs/product/contexts/collaboration.md
docs/product/contexts/automation.md
docs/product/contexts/integrations.md
docs/product/contexts/billing.md
docs/product/contexts/analytics.md
```

These files own the detailed semantics intentionally omitted here.

---

# 136. Relationship to bounded-context map

`product-model.md` explains cross-context product meaning.

`bounded-context-map.md` formalizes system-level ownership relations and context boundary tests.

They must remain coherent.

Neither replaces the detailed context docs.

---

# 137. Relationship to cross-context system architecture

Mechanics belong to:

```text
data-ownership-and-consistency.md
events-realtime-and-delivery-boundary.md
contract-boundaries.md
```

This document states the product facts those mechanics must preserve.

---

# 138. Relationship to implementation

Backend/frontend implementations may choose different technical structures while preserving the same product model.

Examples:

```text
one context spans several backend layers
one product capability spans several frontend packages
technical Search service exists
```

None changes semantic owner by itself.

---

# 139. Product-model change protocol

Changing a `PROD-*` invariant requires:

1. identify affected contexts;
2. update `PRODUCT.md` if constitution-level;
3. update detailed context docs;
4. update bounded-context map if ownership changes;
5. use SYS-ADR when consequential;
6. assess data/contracts/events/frontend;
7. define migration;
8. update evidence/tests.

Do not silently revise product meaning in one implementation PR.

---

# 140. Final product-model rule

For every new feature or behavior, Notrelix must be able to answer:

```text
What product fact/concept is this?
Which context owns it?
Which scope contains it?
What lifecycle/invariant defines it?
Who may mutate it?
How is access/entitlement decided?
Which contexts may reference/react?
What is authoritative versus derived?
What happens on conflict/failure/delete?
What does the user observe while work is pending?
How would the meaning survive a UI rewrite, database change, or service extraction?
```

If the answer depends mainly on:

```text
which folder/table/component currently exists
```

the product model is not yet sufficiently defined.

The target is:

> **one coherent product, many explicit semantic owners, and no hidden duplicate truth.**
