---
document_id: SYS-BOUNDED-CONTEXT-MAP
document_type: architecture
status: active
owner: system-architecture
applies_to:
  - product
  - backend
  - frontend
  - cross-context-contracts
evidence:
  - PRODUCT.md
  - docs/product/
  - backend/src/Notrelix.Domain/
  - backend/src/Notrelix.Application/
  - backend/tests/
  - frontend/tooling/dependency-rules/src/architecture-manifest.ts
  - frontend/packages/
review_on:
  - bounded-context-owner-change
  - product-semantic-change
  - cross-context-workflow-change
  - context-merge-or-split
  - service-extraction-change
  - tenant-scope-change
---

# Bounded Context Map

> **This document defines the accepted business bounded contexts of Notrelix and the ownership relationships between them.**
>
> It owns the system-level context map.
>
> Detailed business semantics remain in:
>
> `docs/product/contexts/*.md`

A bounded context is:

> **a boundary within which business vocabulary, lifecycle, invariants, and mutation authority are coherent.**

It is also a potential future extraction seam.

It is **not automatically**:

- a backend project;
- a frontend package;
- a database/schema;
- a queue;
- a deployable service;
- a team.

---

# 1. Accepted business bounded contexts

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

These names are semantic product ownership categories.

They are not generated from folder names.

---

# 2. Context summary

| Context | Primary ownership | Explicitly does not own |
|---|---|---|
| **Accounts** | Account/customer administrative ownership boundary | Identity credentials, Workspace content, Billing provider mechanics |
| **Identity** | User/service identity, authentication/session/credential security lifecycle | Workspace membership policy, resource authorization |
| **Workspaces** | Workspace lifecycle, membership, invitations, collaboration-tenant organization | Board/Document internals, permission-policy engine |
| **Governance** | Resource permission/policy/sharing and governance/audit semantics | Protected resource business state |
| **Work Management** | Boards, dynamic fields, items, groups, views, structured-work semantics | Document block content, generic provider integrations, subscription state |
| **Documents** | Pages, blocks, hierarchy and document-owned content semantics | Board record schema, generic comments/notifications |
| **Collaboration** | Comments, mentions, reactions, notification/activity/presence collaboration semantics | Immutable security governance policy, source resource state |
| **Automation** | Automation definition, trigger/condition/action semantics, execution product lifecycle | Provider transports, generic retry runtime, source business-state ownership |
| **Integrations** | Provider connection/webhook/synchronization product state | Provider SDK mechanics as product semantics, source-context business state |
| **Billing** | Plans, subscriptions, entitlements, usage/commercial lifecycle | General resource authorization, provider SDK mechanics |
| **Analytics / Reporting** | Metric/report/dashboard/derived insight semantics | Source-context mutation authority |

---

# 3. SYS-CTX-001 — Context ownership follows semantic lifecycle, not storage topology

A table belongs conceptually to the context that owns:

- vocabulary;
- lifecycle;
- invariants;
- authorized mutation of its state.

Not to whichever table it references.

Example:

```text
Comment has workspace_id
```

does not make Comment a Workspaces-owned aggregate.

Workspace is scope.

Collaboration owns comment semantics.

---

# 4. SYS-CTX-002 — One authoritative owner per business fact

The same authoritative fact MUST NOT be independently mutable in two contexts.

Allowed:

```text
owner state
→ projection/cache/read copy
```

Forbidden:

```text
context A mutates fact
+
context B independently mutates duplicate fact
```

without one being explicitly derived/compatibility state.

---

# 5. SYS-CTX-003 — Context boundaries protect meaning, not folder symmetry

Context boundaries exist to prevent semantic coupling.

Do not create a context because:

- one database table exists;
- one route group exists;
- one frontend feature folder exists;
- one external provider exists;
- one team wants a namespace.

The proposed context must own a coherent business lifecycle.

---

# 6. SYS-CTX-004 — Cross-context references are contracts

One context may reference another context's root by:

- stable ID;
- approved immutable fact;
- read contract;
- integration/public event.

It MUST NOT require direct mutable aggregate navigation across context ownership.

---

# 7. SYS-CTX-005 — Cross-context writes preserve target ownership

If context A causes state in context B to change:

```text
A requests/reacts through B's contract
```

not:

```text
A directly edits B's persistence/aggregate
```

The target context remains responsible for its invariants.

---

# 8. Context categories

The eleven contexts are not ranked, but they play different architectural roles.

A useful relationship view is:

## Administrative / trust foundation

```text
Accounts
Identity
Workspaces
Governance
Billing
```

These establish administrative, principal, tenancy, permission, and commercial facts used by many product capabilities.

---

## Primary work/content capabilities

```text
Work Management
Documents
```

These own major user-authored/product state.

---

## Cross-resource product capabilities

```text
Collaboration
Automation
Integrations
Analytics / Reporting
```

These often consume facts from several contexts while retaining their own semantic ownership.

This categorization is descriptive.

It does not create a new authority hierarchy.

---

# 9. Accounts

Canonical product owner:

```text
docs/product/contexts/accounts.md
```

## Owns

At system-map level:

- Account identity as administrative/customer ownership boundary;
- Account lifecycle;
- account-level administration;
- account-level ownership relationships where product semantics require them.

## Does not own

- authentication credentials;
- Workspace membership;
- resource authorization;
- subscription provider mechanics;
- Work Management/Document content.

---

# 10. Accounts relationships

Accounts commonly provides administrative scope to:

```text
Workspaces
Billing
Governance
Integrations
Analytics
```

depending on the product operation.

Identity may reference Account relationships without becoming Account-owned.

Billing may attach commercial state to Account.

The exact relation semantics belong to product context docs.

---

# 11. Accounts boundary test

Ask:

> If the fact describes customer/administrative ownership independent of one collaboration Workspace, is Accounts the owner?

If the fact describes:

```text
login/session
→ Identity

workspace membership
→ Workspaces

subscription/entitlement
→ Billing
```

then Accounts is not the owner merely because the fact is account-scoped.

---

# 12. Identity

Canonical product owner:

```text
docs/product/contexts/identity.md
```

## Owns

- user/principal identity;
- authentication lifecycle;
- sessions;
- credentials;
- MFA;
- OAuth/SSO-linked identity;
- API/service token identity/security lifecycle where product-owned.

## Does not own

- Workspace membership;
- permission policy;
- protected resource lifecycle;
- billing entitlement.

---

# 13. Identity relationships

Identity supplies principal identity to:

```text
Workspaces
Governance
Collaboration
Automation
Integrations
Billing
```

as required.

Consumers use stable identity references.

They do not mutate Identity internals to manage their own lifecycle.

---

# 14. Identity versus Workspaces

A person can exist independent of one Workspace.

Therefore:

```text
Identity
≠ Workspace child lifecycle
```

Workspaces owns membership/invitation.

Identity owns the principal being invited/authenticated.

---

# 15. Identity versus Governance

Identity answers:

```text
Who are you?
```

Governance answers:

```text
What are you allowed to do?
```

Do not merge these questions into generic roles attached to Identity.

---

# 16. Workspaces

Canonical product owner:

```text
docs/product/contexts/workspaces.md
```

## Owns

- Workspace;
- Workspace lifecycle;
- membership;
- invitations;
- collaboration-tenant organization;
- workspace selection/scope semantics.

## Does not own

- Board/Document internals;
- permission-policy engine;
- user credential lifecycle;
- billing provider state.

---

# 17. Workspace as tenant scope

Workspace is a core collaboration tenant scope for many resources.

This means many contexts may carry:

```text
WorkspaceId
```

It does not mean Workspaces owns those resources.

Example:

```text
Board
    scoped to Workspace
    owned by Work Management
```

---

# 18. Membership ownership

Workspace membership is owned by Workspaces.

Governance may consume membership facts when evaluating authorization.

Governance does not become membership owner merely because membership affects access.

---

# 19. Workspace lifecycle effects

When a Workspace lifecycle changes, dependent contexts may need to:

- disable access;
- stop processing;
- archive/project state;
- remove subscriptions;
- update indexes.

Those are cross-context workflows.

Workspaces must not directly delete internal state in every other context by object-graph cascade.

---

# 20. Governance

Canonical product owner:

```text
docs/product/contexts/governance.md
```

## Owns

At system level:

- permission semantics;
- resource policy;
- sharing;
- guest/share-link access semantics;
- governance/audit meaning;
- reusable authorization vocabulary.

## Does not own

- protected resource business state;
- Identity credential lifecycle;
- Workspace membership lifecycle;
- Billing subscription lifecycle.

---

# 21. Governance as cross-cutting authority

Governance is used across many product contexts.

That does not make Governance the owner of those resources.

Conceptually:

```text
Work Management owns BoardItem

Governance answers:
Can principal X perform action Y on BoardItem Z?
```

The BoardItem remains Work Management state.

---

# 22. Governance inputs

Authorization may consume facts from:

```text
Identity
Workspaces
Accounts
Billing
protected resource context
```

Examples:

- principal;
- membership;
- account policy;
- entitlement;
- resource identity/state.

Governance must preserve source ownership of those facts.

---

# 23. Governance audit versus Collaboration activity

Do not merge:

```text
security/governance audit
```

with:

```text
user-facing activity/feed/comment notification
```

merely because both record actions.

Governance/audit evidence has different trust and retention semantics from Collaboration activity.

---

# 24. Work Management

Canonical product owner:

```text
docs/product/contexts/work-management.md
```

## Owns

- Board;
- BoardField;
- BoardItem;
- BoardGroup;
- BoardView;
- dynamic field schema/value semantics;
- item/group ordering;
- structured-work lifecycle;
- Table/Kanban/Calendar/Timeline/Form/Dashboard view semantics;
- Work Management relations/formulas/rollups where product-owned.

## Does not own

- Document block content;
- generic Collaboration comments;
- provider connection lifecycle;
- commercial subscription;
- identity credentials.

---

# 25. One work model, many views

Work Management protects:

```text
BoardItems + BoardFields
→ authoritative work data

BoardViews
→ projections/configurations over that data
```

Kanban/Table/Calendar/Timeline/Form/Dashboard are not independent stores of duplicate work records.

---

# 26. BoardGroup distinction

`BoardGroup` is structural product organization.

It MUST NOT be treated as the universal Kanban status field.

Kanban grouping derives from configured grouping semantics.

This distinction is product-owned and survives frontend representation changes.

---

# 27. Work Management relationships

Common relationships:

```text
Workspaces
    provides collaboration tenant scope

Governance
    authorizes protected work operations

Collaboration
    attaches comments/activity to work resources

Automation
    reacts to work facts and invokes work actions

Integrations
    synchronizes approved work facts/actions

Analytics
    derives work metrics

Documents
    may link/embed/reference work resources
```

Work Management remains owner of work state.

---

# 28. Documents

Canonical product owner:

```text
docs/product/contexts/documents.md
```

## Owns

- Page;
- Block;
- document hierarchy;
- document-owned content;
- document lifecycle;
- document-specific links/embeds where owned.

## Does not own

- Board schema/items;
- generic comments/mentions;
- provider connection lifecycle;
- access-policy engine.

---

# 29. Documents versus Work Management

Documents and Work Management are intentionally separate.

Allowed:

```text
Page references Board
Block embeds/links work resource
BoardItem references Page via stable relation
```

Forbidden default:

```text
Document Block becomes Board row storage

Board item internals are stored as Document ownership
```

Cross-context relations must preserve each owner's lifecycle.

---

# 30. Documents relationships

Common:

```text
Workspaces
→ tenant scope

Governance
→ authorization

Collaboration
→ comments/mentions/presence

Automation
→ document triggers/actions

Integrations
→ provider import/export/sync

Analytics
→ derived document metrics
```

---

# 31. Collaboration

Canonical product owner:

```text
docs/product/contexts/collaboration.md
```

## Owns

- comments;
- threads;
- mentions;
- reactions;
- notification semantics;
- user-facing activity semantics;
- presence/cursor collaboration semantics where product-owned.

## Does not own

- source resource state;
- authorization policy;
- immutable security audit policy;
- identity credentials.

---

# 32. Collaboration target-resource model

Collaboration commonly attaches to resources owned elsewhere.

Conceptually:

```text
Collaboration target
→ stable resource identity
→ source context retains ownership
```

A comment on a BoardItem does not make Collaboration owner of the BoardItem.

---

# 33. Collaboration versus Governance

User-facing activity can answer:

```text
What happened for collaborators?
```

Governance/audit can answer:

```text
What security/administrative action must be reliably auditable?
```

These may originate from one operation but remain different semantic facts.

---

# 34. Collaboration versus Identity

Mentions/reference actors using stable Identity references.

Collaboration does not own user identity lifecycle.

If a user is disabled/deleted, Collaboration applies its own retention/display policy to historical references without mutating Identity.

---

# 35. Automation

Canonical product owner:

```text
docs/product/contexts/automation.md
```

## Owns

- automation rule/definition;
- trigger semantics;
- condition semantics;
- action semantics;
- scheduling product semantics;
- execution identity/lifecycle;
- automation-specific recursion/throttling policy where product-owned.

## Does not own

- source-context business state;
- provider SDK;
- generic message retry transport;
- authorization bypass.

---

# 36. Automation trigger relationship

Automation consumes approved facts from other contexts.

Examples:

```text
BoardItem changed
Document updated
Membership changed
Integration event received
```

The source context owns the fact.

Automation owns:

```text
whether/how an automation rule reacts
```

---

# 37. Automation action relationship

Automation invokes target context contracts.

Example:

```text
Automation action:
update BoardItem

Target owner:
Work Management
```

Automation does not directly update Work Management persistence.

Target Application/Domain invariants still execute.

---

# 38. Automation reliability

Execution may require:

- stable execution ID;
- idempotency;
- retry;
- provider operation identity;
- durable state.

Platform/Infrastructure may implement reliability mechanisms.

Automation owns execution product semantics.

---

# 39. Integrations

Canonical product owner:

```text
docs/product/contexts/integrations.md
```

## Owns

- provider connection product state;
- connection lifecycle;
- webhook registration/semantic intake;
- external mapping;
- synchronization product semantics;
- provider revision/conflict policy where product-owned.

## Does not own

- generic provider SDK/runtime implementation;
- source business state;
- user identity semantics merely because OAuth is used;
- generic retry infrastructure.

---

# 40. Integrations versus Identity

OAuth can serve two distinct product purposes.

```text
OAuth used to authenticate/link user identity
→ Identity

OAuth used to connect an external product provider
→ Integrations
```

Do not classify by protocol alone.

Classify by product purpose/lifecycle.

---

# 41. Integrations versus source contexts

An integration may synchronize:

```text
Work Management
Documents
Calendar-like temporal facts
other provider-backed capabilities
```

The source context remains owner of its product facts.

Integrations owns:

- connection;
- mapping;
- synchronization relationship;
- provider-specific product state.

---

# 42. Provider adapter mechanics

SDK clients, HTTP transport, credential storage mechanism, retry implementation belong to technical adapter/runtime owners.

They do not turn Infrastructure into the product Integrations context.

---

# 43. Billing

Canonical product owner:

```text
docs/product/contexts/billing.md
```

## Owns

- plan catalog semantics;
- subscription;
- entitlement;
- usage/commercial tracking;
- payment/commercial lifecycle;
- downgrade/limit behavior where Billing-owned.

## Does not own

- generic resource authorization;
- Workspace membership;
- source resource state;
- provider SDK mechanics.

---

# 44. Entitlement versus Governance

Billing says:

```text
what commercial capability/limit is available?
```

Governance says:

```text
who may perform an operation on a resource?
```

An authorization decision may consume entitlement facts.

Billing does not become the general authorization engine.

---

# 45. Billing provider relationship

External billing providers are integration/adapter concerns around Billing-owned commercial state.

Provider status names must not automatically become Notrelix business lifecycle without translation.

---

# 46. Analytics / Reporting

Canonical product owner:

```text
docs/product/contexts/analytics.md
```

## Owns

- metric definition;
- analytical aggregation semantics;
- report/dashboard/widget analytical meaning;
- analytical freshness semantics where product-owned;
- reporting views over product facts.

## Does not own

- mutation authority for source product state;
- arbitrary read-model implementation;
- raw event transport;
- source lifecycle.

---

# 47. Analytics derived-state rule

Analytics consumes source-owned facts.

It may materialize:

- projections;
- aggregates;
- snapshots;
- indexes.

These remain derived analytical state.

If a business workflow writes a source context based on an analytical result, it must call the source context contract.

---

# 48. Analytics versus operational telemetry

Product Analytics/Reporting is not the same as:

```text
logs
metrics
traces
system observability
```

Operational telemetry belongs to Operations.

Product metrics/reporting belong to Analytics.

They may use overlapping infrastructure but have different semantics.

---

# 49. Technical/supporting capabilities

The following are technical/supporting capabilities by default, not business bounded contexts.

```text
Search / indexing
Caching
Messaging transport
Outbox / delivery runtime
Realtime transport
Observability
Code generation
Contract generation
Object/file storage
Database migration tooling
CI/CD
Gateway/proxy
Provider SDK runtime
```

They may be significant architecture modules.

They do not automatically own product vocabulary/lifecycle.

---

# 50. Search / indexing

Search is a supporting capability unless a future approved product decision gives it an independent business lifecycle.

Search:

- projects source context data;
- applies tenant/security scope;
- may rank/query across allowed resources;
- may have freshness lag.

Search does not become authoritative because users query it.

---

# 51. Notifications

User-facing notification semantics belong to Collaboration where the product model defines them there.

Delivery mechanics such as:

- email;
- push;
- queue retry

remain technical/integration/runtime concerns.

Do not create a Notifications bounded context merely because a frontend feature/package has that name.

---

# 52. Activity

User-facing activity belongs to Collaboration when it represents collaborative product history/feed semantics.

Security/administrative audit belongs to Governance.

Operational logs belong to Operations.

One word “activity” must not collapse these meanings.

---

# 53. Storage

Object/file storage is a technical capability.

Ownership of stored content follows the product context whose lifecycle the content participates in.

Do not create Storage as a business context by default.

---

# 54. Realtime

Realtime transport is technical/system architecture.

The business fact carried by realtime belongs to the source product context.

Frontend realtime owns client delivery/reconciliation behavior.

---

# 55. Context relationship types

Notrelix uses explicit relationship types.

```text
Scope / administrative fact
Read fact
Authorization fact
Entitlement fact
Stable reference
Synchronous use-case request
Integration/public event
Projection
Process manager / saga
Provider mapping
```

These should be named rather than hidden behind generic “dependency”.

---

# 56. Scope / administrative relationship

Example:

```text
Work Management resource
→ Workspace scope
```

Workspaces owns the Workspace.

Work Management owns the Board/Item.

---

# 57. Read-fact relationship

Consumer reads an external fact through an explicit contract.

Examples:

```text
Automation reads Board metadata
Governance reads membership
Billing reads Account identity
```

Freshness and failure semantics must be known.

---

# 58. Authorization relationship

Protected product context supplies:

- resource;
- action;
- context facts.

Governance/security architecture determines authorization.

Source context still owns business state.

---

# 59. Entitlement relationship

A product capability may require a Billing entitlement/limit.

The target capability still owns its business state.

Billing owns the commercial permission/limit fact.

---

# 60. Stable-reference relationship

A context may retain a stable ID to another context's root.

Example:

```text
Comment.TargetResourceId
```

The ID is a contract.

It is not mutable object ownership.

---

# 61. Synchronous request relationship

Use when:

- immediate response is required;
- target owner must enforce its invariant now;
- failure should block caller;
- coupling is accepted.

The target context's Application contract remains the mutation boundary.

---

# 62. Integration-event relationship

Use when:

- source can commit independently;
- downstream reaction may lag;
- retry is safe/idempotent;
- decoupling is valuable.

The event represents a stable fact, not an instruction to mutate internal storage directly.

---

# 63. Projection relationship

A context/system may derive a local projection.

Projection requires:

- source owner;
- freshness;
- rebuild strategy where relevant;
- scope/security.

Projection does not create co-ownership.

---

# 64. Process manager / saga relationship

Use for durable multi-step workflows spanning independent owners when:

- workflow state itself needs persistence;
- retries/compensation are explicit;
- one aggregate transaction is insufficient.

Do not introduce saga/process manager for ordinary request chaining.

---

# 65. Provider mapping relationship

Integrations may map:

```text
Notrelix resource ID
↔ provider resource ID
```

Integrations owns mapping/sync relation.

Source context owns Notrelix business state.

Provider owns external state.

---

# 66. High-level relationship matrix

Legend:

```text
S = scope/admin fact
R = reads facts
A = authorization/policy input
E = entitlement/commercial input
L = stable link/reference
V = reacts via event
I = invokes target contract
P = derives projection/analytics
X = provider synchronization
```

The matrix is descriptive, not an exhaustive runtime graph.

| Consumer \ Owner | Accounts | Identity | Workspaces | Governance | Work Mgmt | Documents | Collaboration | Automation | Integrations | Billing | Analytics |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Accounts | — | R | R | A |  |  |  |  | R | R | P |
| Identity | L | — |  |  |  |  |  |  | L |  | P |
| Workspaces | L | L | — | A |  |  |  |  |  | E | P |
| Governance | R | R | R | — | R | R | R | R | R | R | P |
| Work Management | S | R | S | A | — | L | I/V | V/I | X | E | P |
| Documents | S | R | S | A | L | — | I/V | V/I | X | E | P |
| Collaboration | S | R | S | A | L | L | — | V/I | X | E | P |
| Automation | S | R | S | A | R/I/V | R/I/V | R/I/V | — | I/X | E | P |
| Integrations | S | R | S | A | R/I/X | R/I/X | R/I/X | R/I | — | R/X | P |
| Billing | R | R | R | A |  |  |  |  | X | — | P |
| Analytics | S | L | S | A | R/P | R/P | R/P | R/P | R/P | R/P | — |

This matrix describes likely relationship classes.

Detailed product workflows remain product-context-owned.

---

# 67. Matrix interpretation

The matrix does NOT mean:

- every listed relationship is currently implemented;
- every operation requires every listed dependency;
- every relationship is synchronous;
- all contexts share one object graph.

It documents architectural relationship categories that are legitimate when product behavior requires them.

Current implementation must be verified from source/tests.

---

# 68. Context dependency direction

Context dependencies should minimize cyclic synchronous knowledge.

Preferred:

```text
stable IDs
read ports
events
target-owner commands
```

over:

```text
shared mutable entities
cross-context repositories
bidirectional aggregate references
```

Some product workflows are inherently bidirectional at semantic level.

Implementation still must preserve independent mutation ownership.

---

# 69. Cross-context query

A query may combine facts from several contexts for a read model.

The read composition layer must identify:

- source owners;
- tenant/security scope;
- freshness;
- pagination/performance;
- whether data is live or projected.

A cross-context report does not become a new owner of source facts.

---

# 70. Cross-context command

A command must identify one primary business use-case owner.

If it causes multiple contexts to change:

- define orchestration;
- define commit boundaries;
- define events/process manager;
- define failure/compensation.

Do not hide a distributed workflow inside one “manager/service” class.

---

# 71. Primary-owner test

For a proposed capability ask:

```text
Whose vocabulary names the capability?
Who defines valid lifecycle?
Who may mutate the authoritative state?
Which context would still own it if storage/UI/provider changed?
```

That context is the likely primary owner.

---

# 72. Lifecycle test

Ask:

> Can this concept be created, changed, archived/deleted independently according to context-specific rules?

If yes, it may indicate a distinct context-owned concept.

If its lifecycle only exists as an implementation artifact of another context, it probably is not a new bounded context.

---

# 73. Invariant test

Ask:

> Which context defines what makes this state valid?

The owner of the invariant is strong evidence for context ownership.

---

# 74. Language test

If two capabilities use the same word with different meanings, they may require separate context vocabulary.

Example:

```text
Status
```

may mean:

- work field value;
- subscription lifecycle;
- automation execution state;
- integration connection state.

Do not create one universal `Status`.

---

# 75. Mutation-authority test

Ask:

> Which context is allowed to perform the authoritative mutation?

Reading/projecting a fact does not make a context owner.

---

# 76. Deletion test

Deletion/archive often reveals ownership.

Ask:

- who decides deletion is allowed?
- what happens to references?
- what must be retained?
- what is anonymized/tombstoned?
- which downstream contexts react?

The context answering these questions owns the lifecycle.

---

# 77. Service-extraction test

A context is a plausible extraction seam when:

- ownership is coherent;
- cross-context contracts are explicit;
- direct persistence coupling is controlled;
- independent data boundary is understandable;
- operational reasons justify extraction.

Do not split into a service to “create” the boundary.

Create the semantic boundary first.

---

# 78. Frontend-package test

A frontend package does not define context ownership.

Examples:

```text
features-search
features-auth
features-workspace
```

are frontend architecture classifications.

They may implement product capabilities from one/more contexts.

The product context map remains independent.

---

# 79. Backend-folder test

A Domain/Application folder can reflect a context.

But folder presence alone is evidence, not the authority.

The context map is owned here/product docs.

Architecture tests/source should converge to the accepted map.

---

# 80. Database-schema test

A schema/table grouping may follow context ownership for clarity.

It does not define ownership.

Shared physical DB does not erase boundaries.

Future data split should follow semantic ownership.

---

# 81. Team-ownership test

Teams may organize around contexts.

Organizational team topology may change.

The business context should remain stable if semantic ownership remains stable.

Do not redefine product context solely because team structure changes.

---

# 82. Context merge criteria

Merging contexts is a product/system architecture change.

Consider merge only when:

- vocabulary is truly one;
- lifecycle/invariants converge;
- independent ownership provides no value;
- cross-context contract overhead is artificial.

Migration requires:

- product docs;
- data ownership;
- API/events;
- backend/frontend structure;
- tests;
- ADR.

---

# 83. Context split criteria

Split when one context contains clearly independent semantic models with:

- different vocabulary;
- different lifecycle;
- different mutation authority;
- different scaling/security/operational pressures;
- excessive internal coupling.

Do not split merely because the source folder is large.

---

# 84. Context rename criteria

Rename only when the current name materially miscommunicates the ubiquitous language.

A rename may affect:

- public contracts;
- package/namespace naming;
- docs;
- events;
- analytics.

Semantic compatibility must be assessed.

---

# 85. New-context admission checklist

A proposed bounded context MUST answer:

```text
[ ] distinct business vocabulary
[ ] distinct lifecycle
[ ] distinct authoritative state
[ ] distinct invariants
[ ] mutation authority
[ ] tenant/account scope
[ ] authorization relationship
[ ] upstream/downstream contracts
[ ] why existing context cannot own it
[ ] data/extraction seam
[ ] frontend/backend representation impact
[ ] migration impact
[ ] ADR rationale
```

A new screen/table/package is insufficient.

---

# 86. Context-removal checklist

Before retiring/merging a context:

```text
[ ] all owned facts have new owner
[ ] lifecycle migration defined
[ ] references migrated
[ ] API/events migrated
[ ] stored data migrated
[ ] backend/frontend owners migrated
[ ] analytics/projections updated
[ ] authorization/tenant implications resolved
[ ] old context docs superseded/deleted
[ ] topic authority map updated
[ ] ADR accepted
```

---

# 87. Context-specific product docs

This map is intentionally not the full business specification.

Detailed docs should define:

```text
mission
owns
does not own
ubiquitous language
core concepts
invariants
lifecycle
tenant/authorization semantics
cross-context contracts
events
journeys
failure/conflict
deletion/archive
frontend implications
analytics implications
testing/change impact
```

under:

```text
docs/product/contexts/
```

---

# 88. Cross-context workflow owner

A multi-context workflow needs one orchestration owner.

That owner may be:

- an Application use case in a primary initiating context;
- Automation when it is automation-defined;
- an explicit process manager/saga for durable workflow state.

The orchestration owner does not acquire mutation ownership of every participant.

---

# 89. Example — Work item comment

```text
BoardItem
→ Work Management

Comment
→ Collaboration

Principal
→ Identity

Workspace membership
→ Workspaces

Permission
→ Governance
```

Flow:

```text
authenticate principal
→ establish Workspace/resource
→ authorize comment action
→ validate target reference
→ Collaboration creates Comment
```

No need to move BoardItem into Collaboration.

---

# 90. Example — Board change triggers automation

```text
BoardItem mutation
→ Work Management owns commit

approved event/fact
→ Automation trigger input

Automation execution
→ Automation owns execution lifecycle

automation action updates another BoardItem
→ invokes Work Management contract
```

Automation does not mutate Work Management storage directly.

---

# 91. Example — Provider sync

```text
Provider connection
→ Integrations

provider resource mapping
→ Integrations

Notrelix BoardItem
→ Work Management

sync operation
→ Integrations coordinates/mapping
→ Work Management validates/applies its mutation
```

Provider field model does not become Work Management ubiquitous language automatically.

---

# 92. Example — Entitlement protects capability

```text
Subscription/entitlement
→ Billing

resource action policy
→ Governance

Workspace membership
→ Workspaces

BoardItem mutation
→ Work Management
```

Authorization may require both Governance and Billing facts.

Neither becomes owner of BoardItem state.

---

# 93. Example — Analytics dashboard

```text
source work facts
→ Work Management

source document facts
→ Documents

collaboration facts
→ Collaboration

metric definition / aggregation
→ Analytics
```

Dashboard data may be materialized.

Source facts remain source-context-owned.

---

# 94. Example — User deletion

Identity owns the identity lifecycle.

Effects may require reactions from:

```text
Workspaces
→ membership/invitation handling

Governance
→ policy/audit handling

Collaboration
→ historical attribution/display policy

Integrations
→ user-linked provider connection handling

Billing/Accounts
→ administrative/commercial references where applicable
```

Identity must not directly edit every downstream aggregate.

The deletion workflow needs explicit contracts and retention semantics.

---

# 95. Example — Workspace deletion/archive

Workspaces owns Workspace lifecycle.

Dependent contexts may need to:

- deny new operations;
- archive/retain scoped resources;
- stop automation/integration work;
- adjust indexes;
- preserve compliance/audit records.

The exact policy belongs to each context plus the cross-context workflow.

A database cascade is not the product policy.

---

# 96. Context and tenant isolation

Many contexts are Workspace-scoped.

Some operations are Account/global/identity-scoped.

Do not invent fake Workspace IDs for operations whose semantic scope is not Workspace.

Every context should explicitly define its scope model in its product doc.

---

# 97. Context and authorization

Each context declares:

- protected resource identity;
- operations/actions;
- business preconditions.

Governance/security architecture applies authorization policy.

The context still enforces business invariants after authorization.

---

# 98. Context and events

A source context may publish facts about committed state.

Events must not expose another context's internals as if owned locally.

Consumer contexts treat events as contracts.

Event taxonomy/details belong to:

```text
docs/architecture/events-realtime-and-delivery-boundary.md
```

---

# 99. Context and realtime

Realtime presentation may fan out facts from many contexts.

The realtime channel does not own those facts.

Clients reconcile with authoritative owners.

---

# 100. Context and migration

Moving a fact between contexts is one of the highest-risk migrations.

It may require:

- persisted data migration;
- API compatibility;
- event compatibility;
- authorization changes;
- package/project movement;
- analytics changes;
- dual read/write period;
- explicit cutover.

Use system ADR + delivery migration policy.

---

# 101. Context and shared concepts

A concept may become SharedKernel/foundation only when:

```text
same meaning
same lifecycle expectations
stable dependency direction
multiple legitimate consumers
```

Cross-context duplication may sometimes be safer than false semantic sharing.

---

# 102. Context and provider models

External provider entities remain outside Notrelix ubiquitous language unless deliberately translated.

Use anti-corruption boundaries.

Example:

```text
Stripe subscription status
≠ automatically Billing lifecycle enum

Google event object
≠ automatically Work Management item model
```

---

# 103. Context and UI

A screen may combine several contexts.

Example:

```text
Board item details
    Work Management data
    Collaboration comments
    Governance permission state
    Automation actions
```

The screen is not a new context.

Frontend composition can combine several owners while preserving their state contracts.

---

# 104. Context and API

A route group may expose one or multiple contexts.

Route grouping is transport organization.

It does not define bounded-context ownership.

---

# 105. Context and reporting

A report may join many contexts.

Read composition is allowed.

Do not let report queries become an unofficial mutation/service boundary.

---

# 106. Context and background processing

A background job executes on behalf of a product/system owner.

The job scheduler is technical infrastructure.

The business operation still belongs to its product context.

---

# 107. Context and cache

A cache entry belongs semantically to the fact it represents.

Cache implementation may be shared.

Cache invalidation must respect the source owner and security scope.

---

# 108. Context and audit

Separate:

```text
business activity
security/audit evidence
operational logs
```

Collaboration, Governance, and Operations respectively may own different forms of “what happened”.

Do not collapse them into one generic event table/semantics by convenience.

---

# 109. Evidence

This map should remain coherent with:

## Product

```text
PRODUCT.md
docs/product/
```

## Backend

```text
backend/src/Notrelix.Domain/
backend/src/Notrelix.Application/
backend/tests/
```

## Frontend

```text
frontend/tooling/dependency-rules/src/architecture-manifest.ts
frontend/packages/
frontend/apps/
```

Current implementation structure is evidence.

It is not the sole source of semantic ownership.

---

# 110. Drift handling

If source places a concept in a different module/package than this map implies:

Do not immediately rewrite the context map.

Classify:

```text
DOC_STALE
SOURCE_DEBT
TRANSITION
CONTRACT_CHANGE
UNRESOLVED
```

through AGENTS/CONTEXT governance.

---

# 111. Decision requirements

Changing this context map requires:

- product review;
- system architecture review;
- affected context docs;
- system ADR for consequential ownership change;
- source/data/contract migration;
- backend/frontend impact;
- tests/gates;
- topic authority update.

Context changes are not routine folder refactors.

---

# 112. Forbidden context shortcuts

Do not:

- define context from table name;
- define context from frontend package name;
- define context from provider;
- make `Common` a context;
- make Search a context without product decision;
- create one service per context by default;
- allow one context to edit another's aggregate;
- use one universal `Status`;
- share mutable cross-context entity graphs;
- make Analytics source truth;
- make Governance own all business state;
- make Workspace own every workspace-scoped resource.

---

# 113. Architecture-review checklist

For a cross-context feature:

```text
[ ] primary owner identified
[ ] external facts identified
[ ] stable references identified
[ ] tenant scope explicit
[ ] authorization owner explicit
[ ] read dependencies explicit
[ ] write dependencies explicit
[ ] sync vs async justified
[ ] commit boundary explicit
[ ] events/contracts explicit
[ ] retry/idempotency explicit where needed
[ ] projections/cache ownership explicit
[ ] frontend composition preserves ownership
[ ] migration impact assessed
[ ] tests/evidence identified
```

---

# 114. Stop conditions

Stop rather than guess if:

- two contexts claim mutation authority;
- one fact has two lifecycles;
- a proposed context has no distinct vocabulary/invariant;
- strong consistency spans contexts without explicit reason;
- a direct cross-context repository mutation appears necessary;
- tenant/account scope is ambiguous;
- provider model conflicts with Notrelix product meaning;
- deletion/retention owner is unclear;
- context split/merge has no migration plan.

Use product/system decision governance.

---

# 115. Final context rule

The bounded-context map succeeds when every important business fact can answer:

```text
Who names me?
Who defines my lifecycle?
Who validates my invariants?
Who may mutate me?
How may other contexts reference me?
How do I communicate outward?
What happens if my implementation is later extracted?
```

The correct context boundary should survive:

- UI redesign;
- database change;
- package/project refactor;
- provider change;
- service extraction.

That is the standard:

> **semantic ownership first; technical topology follows.**
