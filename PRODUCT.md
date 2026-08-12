# Notrelix Product Constitution

> **Notrelix is an enterprise work-management workspace operating system.**
>
> It gives teams one coherent environment for structured work, documents, collaboration, automation, integrations, governance, billing, and operational insight without collapsing those capabilities into one generic data model.

This document is the **repository-level product constitution**.

It defines the product identity, semantic model, capability boundaries, product-wide invariants, critical user journeys, and explicit non-goals that backend, frontend, system architecture, and future product specifications MUST preserve.

It does **not** define backend implementation mechanics, frontend package structure, database schema, API shape, or literal design tokens.

Detailed product semantics belong to the owning product documents under [`docs/product/`](docs/product/).

---

## 1. Document authority

### 1.1 What this file owns

`PRODUCT.md` owns the repository-level answers to:

- What is Notrelix?
- Who is it for?
- What product problem does it solve?
- What are the stable product capabilities?
- What bounded contexts exist?
- What are the most important product-wide semantic invariants?
- What does Work Management fundamentally mean?
- How do Documents, Collaboration, Governance, Automation, Integrations, Billing, and Analytics relate to Work Management?
- What product models are explicitly forbidden?
- What questions must be answered before introducing a new capability or context?

### 1.2 What this file does not own

This file MUST NOT become the canonical source for:

- aggregate implementation;
- Application pipeline behavior;
- EF Core or database schema;
- message/outbox implementation;
- RLS mechanics;
- frontend query-cache mechanics;
- frontend package dependency rules;
- API/OpenAPI details;
- design-token literal values;
- delivery plans, freeze phases, or migration progress.

Those concerns belong to their technology/system owners.

### 1.3 Detailed semantic owners

The intended detailed product ownership is:

```text
docs/product/
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

This file may summarize those contexts, but MUST NOT diverge from them.

---

# 2. Product thesis

Notrelix exists to reduce fragmentation in how teams organize and execute work.

A team should not need one semantic model for tasks, another disconnected model for planning views, another tool for documents, another permission model for sharing, and another hidden system for automation and reporting.

Notrelix instead aims for:

> **coherent ownership, flexible representation, and explicit capability boundaries.**

The product is designed around three ideas.

### 2.1 Work is structured but not fixed

Teams model work differently.

A software team, operations team, marketing team, customer-success team, recruiting team, or internal service team may all require different fields, views, workflows, and reporting.

Therefore Work Management MUST provide a flexible schema and view model rather than encode one universal workflow such as Kanban status columns.

### 2.2 Different capabilities may collaborate without becoming one model

Documents can reference work.

Comments can target documents and work items.

Automation can react to work changes.

Analytics can derive insight from multiple capabilities.

Billing can enable or constrain capabilities.

Those relationships do not mean the capabilities should share one universal aggregate, table, entity, or frontend package.

### 2.3 Product coherence comes from contracts, not forced sameness

Notrelix should feel like one product because:

- terminology is coherent;
- interactions follow shared principles;
- cross-capability navigation is predictable;
- permissions are understandable;
- state converges consistently;
- related resources can be linked safely;
- errors and recovery behave coherently.

It should **not** feel coherent because unrelated business concepts were forced into one generic model.

---

# 3. Who Notrelix is for

Notrelix is designed for teams and organizations that coordinate structured work and knowledge over sustained periods.

Primary user groups include:

### Power users

Users who:

- organize workspaces and boards;
- create schemas and views;
- configure automation;
- operate across several workspaces;
- use keyboard-driven workflows;
- inspect dense tables or planning surfaces;
- need fast filtering, sorting, grouping, and bulk operations;
- expect predictable behavior under large data sets.

### Team contributors

Users who:

- update assigned work;
- collaborate through comments and mentions;
- write or consume documents;
- check priorities, dates, status, ownership, and dependencies;
- need clear product language without understanding system architecture.

### Occasional collaborators and guests

Users who:

- enter through an invitation or shared resource;
- need a constrained and understandable subset of the product;
- MUST NOT gain workspace-wide visibility merely because one resource is shared.

### Administrators and owners

Users responsible for:

- account/workspace lifecycle;
- membership and access;
- security;
- billing;
- integrations;
- governance;
- audit-sensitive operations.

Notrelix must serve these groups without forcing every user to experience the full complexity of the platform at all times.

---

# 4. Product operating context

Notrelix is primarily a **long-session work product**.

Authenticated product surfaces should support users who remain inside the application for sustained periods while:

- reviewing work;
- editing many records;
- writing documents;
- moving through projects;
- monitoring dates and dependencies;
- collaborating with teammates;
- responding to notifications;
- configuring workflows.

This affects product semantics as well as UI design.

The system must favor:

- predictable state;
- low cognitive switching cost;
- stable keyboard/pointer/touch behavior;
- recoverable errors;
- explicit permissions;
- durable user trust;
- efficient dense-data workflows.

A visually polished interface that loses or ambiguously mutates business state is a product failure.

---

# 5. Core product model

Notrelix separates product responsibilities into semantic bounded contexts.

The accepted business contexts are:

1. Accounts
2. Identity
3. Workspaces
4. Governance
5. Work Management
6. Documents
7. Collaboration
8. Automation
9. Integrations
10. Billing
11. Analytics / Reporting

These are **semantic ownership boundaries**.

They are also potential future extraction seams.

They are **not** a requirement to create:

- one .NET project per context;
- one database per context;
- one frontend package per context;
- one deployable service per context.

A bounded context may contain several aggregate roots and several product capabilities.

A technical support capability such as search/indexing, operations tooling, background processing, caching, or code generation does **not** become a business bounded context merely because it has a folder, module, table, team, or runtime.

---

# 6. Product-wide semantic invariants

## PROD-001 — One authoritative owner for each business fact

Every durable business fact has one authoritative semantic owner.

Examples:

- Workspace owns workspace membership facts.
- Governance owns authorization-policy semantics.
- Work Management owns Board/Field/Item/View work state.
- Documents owns Page/Block content.
- Billing owns subscription and entitlement state.
- Identity owns authentication/session security state.

Other contexts may:

- consume;
- cache;
- project;
- index;
- report;
- react to

those facts.

They MUST NOT silently become a second source of truth.

### Product consequence

A cache, analytics projection, realtime payload, frontend store, external provider copy, or generated report is not authoritative merely because it contains the same data.

---

## PROD-002 — One work model, many views

Work Management MUST NOT create separate data models for Table, Kanban, Calendar, Timeline, Form, Dashboard, or future compatible views.

The canonical model is:

```text
Board
├── BoardField
├── BoardItem
├── BoardGroup
└── BoardView
```

Where:

- `Board` owns the work collection and dynamic schema boundary;
- `BoardField` defines a stable field/column semantic contract;
- `BoardItem` is the authoritative work record;
- `BoardGroup` organizes work structurally;
- `BoardView` stores how shared work is projected, queried, grouped, sorted, or presented.

A view changes how the user sees and manipulates work.

A view does not create another authoritative copy of the work.

---

## PROD-003 — Kanban is a view, not the Work Management data model

Notrelix MUST NOT model Work Management as “cards in columns” at its foundation.

A Kanban view selects a compatible grouping field.

For example:

```text
BoardItem.Status = In Progress
```

may cause the Item to appear in the `In Progress` Kanban column.

Dragging the card to another Kanban column means changing the configured grouping-field value and relevant ordering semantics.

It MUST NOT merely move a visual card between independent client-side columns.

`BoardGroup` is not automatically a Kanban status column.

---

## PROD-004 — Dynamic schema is typed, not arbitrary

Flexible work data does not mean ungoverned JSON.

Every supported field type has a semantic contract.

Where relevant, a field type defines:

- settings;
- canonical value representation;
- normalization;
- validation;
- equality/no-op behavior;
- filtering;
- sorting;
- grouping compatibility;
- frontend rendering/editing;
- import/export representation;
- automation compatibility;
- indexing/materialization requirements;
- evolution/migration behavior.

Adding an enum member or a frontend renderer alone is not sufficient to add a field type.

---

## PROD-005 — Cross-context references preserve ownership

A Document can reference a Board or Item.

A Comment can target a Page or Item.

An Automation Rule can react to an Item update.

Analytics can aggregate Work Management data.

These relationships MUST use stable identities and explicit contracts.

They MUST NOT:

- embed another context's aggregate graph as owned state;
- mutate another context's persistence directly;
- infer access from the source resource alone;
- bypass the target context's lifecycle or authorization rules.

---

## PROD-006 — Account, Identity, Workspace, and Governance are distinct concepts

These concepts MUST NOT be collapsed for convenience.

### Account

Administrative/customer/commercial ownership scope.

### Identity

Who or what is authenticated and its security lifecycle.

### Workspace

Collaboration tenant and membership boundary for workspace-scoped product data.

### Governance

Permission, sharing, policy, and administrative/security audit semantics.

Examples of forbidden shortcuts:

- using a fake Workspace ID for an Account-level operation;
- encoding workspace permissions inside authentication credentials;
- treating account membership as automatic workspace membership;
- treating frontend role visibility as final authorization.

---

## PROD-007 — Protected business behavior is server-authoritative

Frontend permission checks improve UX.

They do not grant authority.

Protected product operations—including reads, lists, searches, exports, realtime subscriptions, and mutations—must be authorized by server-side policy using authoritative scope/resource facts.

The product MUST fail closed when required authorization facts are unavailable.

---

## PROD-008 — Tenant scope is part of business identity

For workspace-scoped data, workspace identity is not merely a filter option.

It is part of the product's security and consistency model.

A naked resource ID MUST NOT be sufficient to bypass:

- workspace membership;
- authorization;
- persistence scope;
- cache scope;
- realtime scope.

Client “current workspace” state is not a replacement for authoritative resource scope.

---

## PROD-009 — Cross-context workflows use explicit contracts

A bounded context MUST NOT enforce another context's invariants by reaching into that context's persistence model.

Cross-context workflows use appropriate mechanisms such as:

- synchronous Application-loaded facts;
- stable public IDs;
- explicit application contracts;
- integration/public events;
- outbox delivery;
- process managers/sagas when a durable multi-step workflow actually requires one.

Sharing one database does not justify hidden distributed ownership.

---

## PROD-010 — Strong consistency is narrow and intentional

Not every cross-context interaction belongs inside one transaction.

When strong consistency across owners is truly necessary, the design MUST identify:

- which facts require atomicity;
- why eventual consistency is insufficient;
- where the transaction boundary lives;
- what failure/recovery semantics apply.

A modular monolith MUST NOT accidentally turn every context into one giant transactional object graph.

---

## PROD-011 — Retryable effects require stable identity

Product flows involving retries, asynchronous delivery, provider callbacks, automation execution, or other side effects must have stable operation/message/execution identities where duplication would cause incorrect business effects.

The product must assume duplicate delivery can happen.

Retry MUST NOT create duplicate:

- memberships;
- notifications;
- provider operations;
- automation actions;
- invoices;
- subscriptions;
- work mutations

when the operation is defined to be idempotent.

---

## PROD-012 — User-visible state must converge to authoritative state

Optimistic UI, realtime updates, local projections, and cached data may improve responsiveness.

They MUST converge to backend-authoritative state.

The UI MUST NOT permanently represent a mutation as successful after the authoritative operation failed.

Workspace switching, reconnect, stale response, duplicate realtime event, and out-of-order delivery are product-state problems, not only frontend implementation details.

---

## PROD-013 — Concurrency conflicts are product behavior

When two users or processes edit the same protected mutable resource, stale writes MUST NOT silently overwrite newer durable state unless that resource has an explicitly approved conflict model.

Where optimistic concurrency is used, the product should distinguish:

- successful mutation;
- semantic no-op;
- validation failure;
- permission failure;
- stale/conflicting version;
- recoverable connectivity failure.

The user experience may differ for each case.

---

## PROD-014 — Lifecycle semantics are explicit

Not every resource needs generic soft delete.

Product vocabulary should express real lifecycle semantics such as:

- archive;
- revoke;
- disable;
- suspend;
- cancel;
- expire;
- remove;
- tombstone;
- purge.

Physical deletion, user-facing deletion, retention, archive, revocation, and anonymization are different concepts.

Cross-context cascade deletion is forbidden unless explicitly designed.

---

## PROD-015 — Enterprise flexibility must remain queryable

Flexible schema cannot justify unbounded data access.

The product MUST support enterprise-scale filtering, sorting, grouping, reporting, and analytics without requiring routine full-workspace/full-board deserialization and in-memory scans.

Where flexible canonical values need indexed/materialized representations, those representations remain derived from the authoritative owner.

A projection exists to make authoritative data usable at scale; it does not become editable competing truth.

---

## PROD-016 — Product semantics survive implementation refactors

A CLR rename, table rename, package move, frontend host migration, or service extraction MUST NOT silently change product meaning.

Stable public concepts and contract identities evolve through explicit compatibility/migration decisions.

Implementation architecture serves product semantics.

It does not redefine them accidentally.

---

# 7. Bounded-context summary

## 7.1 Accounts

### Mission

Accounts owns durable organization/customer administration above individual workspaces.

### Owns

- Account identity;
- Account lifecycle;
- Account-scoped administration;
- Account-level facts explicitly assigned to this context.

### Does not own

- authentication credentials;
- workspace membership internals;
- product resource state;
- authorization policy evaluation;
- commercial subscription truth.

### Key distinction

Account is not a Workspace parent object that should be loaded and mutated as one aggregate graph.

Account and Workspace may be related, but their semantics remain distinct.

Detailed owner:

[`docs/product/accounts.md`](docs/product/accounts.md)

---

## 7.2 Identity

### Mission

Identity proves who or what is acting and manages authentication/security lifecycle.

### Owns

Where supported:

- user identity;
- sessions;
- credentials;
- MFA;
- OAuth/SSO linking;
- API/service token identity and security state.

### Does not own

Whether an authenticated principal may access a Board, Workspace, billing page, document, or other product resource.

Authentication is not authorization.

Detailed owner:

[`docs/product/identity.md`](docs/product/identity.md)

---

## 7.3 Workspaces

### Mission

Workspaces owns the collaboration tenant structure in which most day-to-day product work occurs.

### Owns

- Workspace lifecycle;
- Workspace membership;
- invitations;
- workspace organizational containers where assigned.

### Does not own

- account-commercial truth;
- authentication credentials;
- operation-level authorization policy;
- Work Management or Document internals.

### Core principle

Workspace membership is a security boundary but not the complete authorization system.

Detailed owner:

[`docs/product/workspaces.md`](docs/product/workspaces.md)

---

## 7.4 Governance

### Mission

Governance owns permission-policy semantics, resource/subject access representation, sharing policy, and administrative/security audit semantics.

### Owns

- permission vocabulary;
- policy evaluation semantics;
- ACL/resource-access facts where applicable;
- sharing rules;
- audit/security evidence.

### Does not own

- credentials;
- membership lifecycle;
- product aggregate state;
- subscription state.

### Core principle

Backend authorization is final.

Detailed owner:

[`docs/product/governance.md`](docs/product/governance.md)

---

## 7.5 Work Management

### Mission

Work Management is the flexible structured-work capability.

It allows teams to define schema, store work records, organize them, and interact through multiple views over the same authoritative data.

### Core nouns

```text
Board
BoardField
BoardItem
BoardGroup
BoardView
FieldValue
FieldType
OrderingKey
Relation
Dependency
DerivedField
```

### Core promise

Teams may model many workflows without Notrelix becoming a hard-coded Kanban product or a schema-less JSON dump.

Detailed owner:

[`docs/product/work-management.md`](docs/product/work-management.md)

---

## 7.6 Documents

### Mission

Documents owns structured knowledge/content.

### Owns

- Pages;
- Blocks;
- document hierarchy;
- block-type content;
- document lifecycle;
- resource links/embeds where assigned.

### Does not own

- Board schema;
- Work Management records;
- comments/notifications;
- target resource authorization.

### Core principle

Documents and Work Management can link deeply without becoming one data model.

Detailed owner:

[`docs/product/documents.md`](docs/product/documents.md)

---

## 7.7 Collaboration

### Mission

Collaboration owns human interaction around product resources.

### Owns

As assigned:

- comments/threads;
- mentions;
- reactions;
- notifications;
- user-facing activity;
- attachment metadata;
- ephemeral presence/cursors.

### Does not own

The target Work Management, Document, Workspace, or Governance resource.

### Core principle

Collaboration targets stable scoped resources; it does not copy resource ownership.

Detailed owner:

[`docs/product/collaboration.md`](docs/product/collaboration.md)

---

## 7.8 Automation

### Mission

Automation owns rule definitions, trigger/condition/action configuration, execution identity/state, and scheduling intent.

### Core flow

```text
completed product fact
→ durable publication
→ automation matching
→ condition evaluation
→ execution/action state
→ normal capability or provider action
```

Automation MUST NOT become a backdoor that mutates product persistence while bypassing normal invariants, tenancy, or authorization.

Detailed owner:

[`docs/product/automation.md`](docs/product/automation.md)

---

## 7.9 Integrations

### Mission

Integrations owns external provider connections and anti-corruption boundaries.

### Owns

- provider connection lifecycle;
- credential references;
- inbound webhook handling;
- provider mapping;
- outbound synchronization operations.

### Core principle

Provider concepts are translated into Notrelix contracts.

Notrelix product domains do not become provider-specific domain models.

Detailed owner:

[`docs/product/integrations.md`](docs/product/integrations.md)

---

## 7.10 Billing

### Mission

Billing owns commercial product truth.

### Owns

- Plan catalog;
- subscription lifecycle;
- entitlement/limit semantics;
- billing periods;
- usage facts where assigned;
- invoice/payment references;
- commercial provider mapping.

### Core principle

Entitlement is a business contract, not scattered `if plan == Pro` checks.

Downgrade/payment failure policy MUST NOT destroy customer work merely to enforce a commercial transition.

Detailed owner:

[`docs/product/billing.md`](docs/product/billing.md)

---

## 7.11 Analytics / Reporting

### Mission

Analytics owns derived insight and metric semantics.

### Owns

- metric definitions;
- dashboard/widget configuration;
- analytical projections;
- snapshots;
- reporting semantics;
- freshness semantics.

### Does not own

Editable source business state.

### Core principle

Analytics is derived state.

A dashboard result does not become authoritative Work Management, Collaboration, Automation, or Billing truth.

Detailed owner:

[`docs/product/analytics.md`](docs/product/analytics.md)

---

# 8. Work Management product contract at a glance

Work Management is sufficiently central to Notrelix that its executive semantics are summarized here.

The detailed contract remains owned by the Work Management context document.

## 8.1 Board

A Board is a workspace-scoped work database/table.

It provides:

- stable identity;
- lifecycle;
- field schema;
- item collection;
- organization;
- view definitions;
- configuration.

A Board is not merely a Kanban board.

A template may initialize a Board but MUST NOT become a second runtime work model unless a future explicit linked-template capability is designed.

---

## 8.2 BoardField

A BoardField defines a column/schema concept with stable identity and type-specific semantics.

Renaming a display label MUST NOT silently change durable field identity.

Field behavior may include:

- validation;
- normalization;
- comparison;
- filtering;
- sorting;
- grouping;
- rendering;
- indexing;
- automation;
- analytics.

People and relation-style fields may require external facts from other owners, but the Work Management Domain itself must not perform external I/O to discover them.

---

## 8.3 BoardItem

A BoardItem is the authoritative work record.

An Item may contain values for BoardFields and may be structurally organized in a BoardGroup.

`BoardItem` is the canonical product noun.

Legacy vocabulary such as `Card` MUST NOT define the product model.

A view may render an Item as a card, row, calendar event, timeline bar, form response, or other representation.

The representation does not replace the Item.

---

## 8.4 BoardGroup

BoardGroup organizes Items in the Board's structural/table organization.

It is not universally:

- status;
- Kanban column;
- workflow state.

A BoardGroup may visually resemble a grouped section.

Kanban grouping uses the configured grouping field.

---

## 8.5 BoardView

A BoardView is saved presentation/query configuration over shared Board data.

Examples may include:

- Table;
- Kanban;
- Calendar;
- Timeline/Gantt;
- Form;
- Dashboard;
- future view types whose semantics are explicitly defined.

View configuration may include:

- visible fields;
- field order;
- filters;
- sort;
- grouping;
- compatible temporal fields;
- presentation settings;
- private/shared/default behavior.

A View MUST NOT own copied BoardItems.

---

# 9. View semantics

## 9.1 Table

Table is the most direct structured representation of BoardItems and BoardFields.

It should support efficient:

- scanning;
- editing;
- field configuration;
- grouping;
- filtering;
- sorting;
- bulk workflows.

Table structure must remain driven by Board schema rather than hard-coded frontend columns.

---

## 9.2 Kanban

Kanban groups Items by a compatible field.

Dragging an Item between columns mutates the configured grouping field and ordering.

It does not mutate an unrelated BoardGroup merely because the visual representations look similar.

---

## 9.3 Calendar

Calendar uses an explicitly compatible date/date-time field.

A view must not invent dates from arbitrary text or presentation-only client state.

---

## 9.4 Timeline / Gantt

Timeline-oriented views require explicit temporal/range semantics.

The product must define:

- start/end or timeline field meaning;
- invalid range handling;
- ordering/overlap behavior where applicable.

---

## 9.5 Form

A Form is an input experience backed by Board schema.

Submitting a form creates or updates authoritative Work Management state according to the configured form contract.

A Form does not own an independent form-response database unless a future product capability explicitly introduces one.

---

## 9.6 Dashboard

A Dashboard presents analytical/derived information from authoritative product data.

It may contain saved visualization configuration.

Metric/source semantics belong to appropriate product/Analytics contracts.

A chart is not a new source of truth.

---

# 10. Documents and structured knowledge

Documents is not an alternative representation of BoardItems.

Its canonical model is based on structured Pages and Blocks.

The product should support linking work and knowledge intentionally.

Examples:

- a Page references a Board;
- a document block references an Item;
- an Item links to supporting documentation;
- a workflow creates a document from approved capability actions.

Those links preserve each target's ownership and authorization.

Sharing a Page MUST NOT automatically expose an otherwise private Board referenced inside the Page.

---

# 11. Collaboration model

Collaboration exists **around resources**.

A comment, mention, activity item, or notification refers to another capability's resource through stable scoped identity.

The resource remains owned by its original context.

Important distinctions include:

### Activity vs Audit

User-facing activity is not security/compliance audit evidence.

They may share source facts but have different:

- audiences;
- retention;
- mutability;
- integrity requirements;
- presentation.

### Notification vs provider delivery

A durable notification can exist even if email/push delivery fails.

Provider delivery is a side effect of notification/product intent.

It is not the notification's source of truth.

### Presence vs durable state

Cursor, presence, and typing signals may be ephemeral.

They MUST NOT become durable content authority.

---

# 12. Governance and sharing

Notrelix should make sharing powerful without weakening tenant/resource boundaries.

A resource may support:

- workspace access;
- member/role-based access;
- resource-specific sharing;
- guest access;
- share links;
- private views/resources;
- future explicit policy types.

A share link is an explicit access capability/principal according to Governance semantics.

It does not transitively make every linked resource public.

Permission checks must apply to reads as well as writes.

Examples requiring authorization include:

- list;
- search;
- export;
- realtime subscription;
- dashboard/report access;
- comments;
- document embeds;
- history/audit access.

---

# 13. Automation model

Automation is event-driven product orchestration, not direct persistence scripting.

An Automation Rule contains approved semantics for:

- trigger;
- conditions;
- actions;
- lifecycle;
- scope;
- execution identity;
- authorization/execution principal.

Automation actions call normal product capability contracts.

Examples:

- update a field;
- assign a principal;
- create a notification;
- invoke an integration;
- schedule a product action.

Automation MUST preserve:

- product invariants;
- tenant scope;
- authorization policy;
- idempotency;
- lifecycle rules;
- recursion protection.

Arbitrary direct SQL/table mutation is not an automation capability.

---

# 14. Integration model

External systems are connected through anti-corruption boundaries.

Provider identity, webhooks, credentials, rate limits, synchronization revisions, and provider-specific limitations belong to integration/provider adapters.

Product contexts consume normalized facts and commands.

A product aggregate must not branch on provider brands such as:

```text
if provider == "Google"
if provider == "Slack"
```

unless that behavior genuinely belongs to a provider-facing integration boundary.

## Two-way synchronization

A two-way sync feature MUST define:

- source-of-truth direction per mapped field/concept;
- provider revision/version;
- deletion semantics;
- conflict policy;
- idempotent inbound handling;
- idempotent outbound handling;
- tenant routing;
- credential/security boundary.

“Last write wins” is not an acceptable implicit default.

---

# 15. Billing and entitlement model

Commercial capability must remain separate from product resource ownership.

Billing may answer:

- which plan applies;
- which entitlement is active;
- which usage limit exists;
- whether an account/workspace may create or use a capability.

Billing MUST NOT:

- become the owner of Board/Document state;
- directly delete customer work to enforce downgrade;
- scatter raw provider status across product contexts.

A downgrade or payment problem needs explicit product policy.

Possible policies may include:

- block new creation;
- make excess resources read-only;
- retain existing work;
- schedule enforcement;
- show upgrade/recovery flows.

The policy must be intentionally designed.

---

# 16. Analytics and reporting model

Analytics provides derived insight.

A metric is a semantic product contract.

It should define, where relevant:

- scope;
- source facts;
- filters;
- aggregation;
- time basis;
- time zone;
- null/missing behavior;
- version;
- freshness.

The same named metric MUST NOT have different formulas in different frontend widgets.

Analytical projections may be eventually consistent.

If freshness affects interpretation, the product must expose or respect that fact.

---

# 17. Search and indexing

Search is an important product capability, but it is not automatically a standalone business bounded context.

Search may index data from:

- Work Management;
- Documents;
- Collaboration;
- other approved owners.

Search results are projections.

Search MUST preserve:

- tenant scope;
- resource authorization;
- lifecycle visibility;
- source identity;
- freshness expectations.

A search index does not become authoritative business state.

Introducing Search as an independent bounded context would require an explicit architecture/product decision proving distinct semantic ownership beyond indexing/query mechanics.

---

# 18. Product-level lifecycle and deletion principles

Lifecycle behavior must use product vocabulary.

Examples include:

```text
Account       suspend / close / delete
Identity      revoke / disable / anonymize
Workspace     suspend / archive / delete
Board         archive / delete
BoardField    deactivate / delete / tombstone
BoardItem     archive / delete
View          archive / delete
Document      archive / delete
Automation    draft / enable / disable / archive
Integration   connect / expire / revoke / disconnect
Subscription  trial / active / past-due / cancel / expire
```

Exact lifecycle belongs to each context.

Cross-context physical cascade is not the default.

Deletion-sensitive design must consider:

- retention;
- references;
- audit;
- commercial/legal records;
- integrations;
- automation;
- analytics;
- user-visible recovery;
- anonymization;
- eventual purge.

---

# 19. Product-level failure semantics

Notrelix should distinguish failure classes when the distinction affects user recovery.

Typical classes:

### Validation failure

The requested state is invalid.

The user should receive actionable information and the durable business state remains unchanged.

### Authorization failure

The principal lacks permission or scope.

The product MUST NOT leak protected information through detailed error content.

### Concurrency conflict

The resource changed since the user's expected version.

The UI may need refetch/reconcile and present conflict/retry behavior.

### Connectivity/retryable failure

The authoritative result may be unknown or delayed.

The product should avoid pretending permanent success.

### Provider failure

External service side effects may have:

- failed;
- succeeded;
- timed out with unknown result.

Operation identity and reconciliation are required where duplication matters.

### Partial asynchronous failure

The authoritative local mutation may already be committed while downstream delivery failed.

The product should surface operational/retry state appropriately rather than attempting impossible cross-context rollback.

---

# 20. Critical user journeys

The following journeys are product-level integration tests for semantic coherence.

They are not exhaustive feature roadmaps.

## Journey A — Create a workspace and begin work

```text
authenticated principal
→ create/join Workspace
→ membership established
→ authorization facts available
→ create/open work resource
→ load scoped state
```

The journey validates the separation of Identity, Workspace membership, Governance, and product data.

---

## Journey B — Build a flexible Board

```text
create Board
→ create/configure BoardFields
→ create BoardItems
→ organize BoardGroups
→ create Views
→ use different views over the same Items
```

The product succeeds only if schema and data remain coherent across views.

---

## Journey C — Move an Item through Kanban

```text
open Kanban view
→ identify configured grouping field
→ drag Item to another column
→ authoritative field mutation
→ version/event/realtime consequence
→ Table and other views converge
```

A client-only card move is a product failure.

---

## Journey D — Link work and knowledge

```text
create/edit Page
→ reference Board/Item
→ target authorization checked
→ render linked resource
→ preserve independent lifecycle/ownership
```

Documents must not absorb Work Management state.

---

## Journey E — Collaborate around a protected resource

```text
open authorized resource
→ comment / mention / react
→ Collaboration validates target access
→ notification/activity produced as appropriate
→ realtime convergence
```

A collaborator with access to the comment does not automatically gain unrelated workspace access.

---

## Journey F — Automate a work change

```text
durable product change
→ automation trigger
→ condition evaluation
→ execution identity
→ approved capability action
→ retry/recovery if required
```

The automated action follows the same product invariants as a normal user/system action.

---

## Journey G — Synchronize with an external provider

```text
authorize connection
→ map provider resource
→ receive authenticated webhook or scheduled sync
→ deduplicate
→ translate to product contract
→ reconcile conflicts
→ record provider operation state
```

External provider identity never replaces Notrelix tenant/resource authority.

---

## Journey H — Apply commercial entitlement

```text
Account has subscription/entitlement
→ product operation asks entitlement contract
→ server enforces product/commercial rule
→ UI reflects availability/recovery
```

Entitlement may constrain product behavior.

It does not own product resources.

---

## Journey I — Derive insight

```text
authoritative product facts
→ analytical projection
→ metric definition
→ dashboard/report
→ visible freshness/authorization semantics
```

Analytics remains derived.

---

# 21. Product experience principles

Detailed design belongs to [`DESIGN.md`](DESIGN.md).

The following are product-level expectations.

## 21.1 The work is the priority

Application chrome should support the work, not visually dominate it.

Boards, Items, Documents, schedules, status, and user content carry the primary visual meaning.

---

## 21.2 Calm density

Notrelix is not a low-information consumer app.

Power users need dense information.

Density should be created through:

- hierarchy;
- alignment;
- typography;
- spacing;
- grouping;
- progressive disclosure;
- predictable controls.

Not through indiscriminate borders, nested panels, or modal layers.

---

## 21.3 Coherent but not artificially uniform

The same product should share:

- language;
- state patterns;
- tokens;
- interaction grammar;
- feedback;
- accessibility expectations.

That does not mean:

- mobile must render web primitives;
- Documents must behave like a table;
- marketing must have authenticated-app density;
- unrelated business concepts must use one generic component/model.

---

## 21.4 Plain, precise language

The product should tell users:

- what happened;
- what is required;
- why an action is unavailable when safely explainable;
- what they can do next.

Engineering/framework language should not leak into user-facing copy.

---

## 21.5 Accessibility is product quality

The baseline for new work is WCAG 2.2 AA.

Accessibility is not a post-feature polish step.

See [`DESIGN.md`](DESIGN.md) for detailed design/accessibility rules.

---

# 22. Enterprise product expectations

Notrelix is intended to evolve as an enterprise-capable SaaS platform.

Enterprise capability means more than adding administrative screens.

The product model must support:

- tenant isolation;
- authorization;
- audit-sensitive operations;
- lifecycle/retention;
- large data sets;
- queryability;
- concurrency;
- retry/idempotency;
- integration failures;
- controlled contract evolution;
- accessibility;
- operational visibility;
- migration safety.

These properties should be embedded in the product contracts early enough that later scale does not require redefining core semantics.

---

# 23. Product non-goals

The following are explicit non-goals for the foundational product architecture.

## 23.1 Not a Kanban clone

Kanban is one Work Management view.

It does not own the data model.

---

## 23.2 Not a CRUD generator over database tables

Database storage is implementation.

Tables do not define product semantics or bounded contexts.

---

## 23.3 Not a universal block/entity platform

Documents, Boards, Items, Accounts, Workspaces, Automation Rules, and other concepts do not need to become one generic object type.

Generic infrastructure is allowed.

Generic product meaning without real shared semantics is not.

---

## 23.4 Not microservice-first

Bounded contexts are semantic/extraction seams.

They do not require separate deployment units today.

A future service extraction must be justified by scale, reliability, security, operational, or organizational needs.

---

## 23.5 Not frontend-authoritative business state

Client state is not the durable business model.

Frontend responsiveness cannot bypass server authority.

---

## 23.6 Not JSON-as-architecture

Extensible JSON may be useful for selected typed configuration/value representations.

It is not permission to avoid:

- schema;
- validation;
- indexing;
- migration;
- compatibility;
- ownership.

---

## 23.7 Not provider-shaped product domains

Notrelix should not become Google-shaped, Slack-shaped, Stripe-shaped, or vendor-shaped internally.

Provider boundaries adapt external models to product contracts.

---

## 23.8 Not feature-package-per-screen/team

A new:

- screen;
- endpoint;
- table;
- team;
- package;
- route

is not sufficient evidence of a new product bounded context.

---

# 24. Product extension test

Before introducing a significant new capability, the proposal MUST answer:

### Meaning

- What user problem is being solved?
- What new vocabulary is introduced?
- Is the vocabulary genuinely new or part of an existing context?

### Ownership

- Which context owns the authoritative fact?
- Which lifecycle owns the fact?
- What explicitly does **not** belong to this capability?

### Scope

- Is the capability global, account-scoped, workspace-scoped, user-scoped, or resource-scoped?
- How does tenant identity propagate?

### State

- What is authoritative state?
- What is derived/projected state?
- What concurrency semantics apply?

### Authorization

- Which principal can perform/read it?
- What Governance/resource policy is required?

### Cross-context impact

- Which owners supply required external facts?
- Which contexts consume events/contracts?
- Are synchronous guards or asynchronous workflows required?

### Contracts

- Does it change REST, realtime, event, generated client, persisted, or package contracts?
- Is compatibility/migration required?

### Lifecycle

- How is it created?
- How does it change?
- How is it disabled/archived/deleted?
- What happens to references and historical evidence?

### Reliability

- Can it retry?
- Can it duplicate?
- Does it invoke external side effects?
- What identity/idempotency semantics apply?

### Experience

- How does web represent it?
- Does mobile support it?
- Does marketing mention it?
- What loading/error/permission/conflict states exist?

### Scale

- How is it queried?
- Which paths must be indexed/materialized?
- Can the design avoid full-tenant/full-board scans?

### Proof

- Which tests prove the product contract?
- Which architecture/security/contract gates protect it?

A proposed feature that cannot answer these questions is not sufficiently specified for implementation.

---

# 25. Product change classification

Changes to this file or detailed product contexts are not ordinary documentation edits when they change semantic meaning.

Examples of product-semantic changes:

- redefining BoardGroup as status;
- making a View own copied Items;
- merging Documents and Work Management into one generic model;
- moving membership authority from Workspaces;
- changing Account vs Workspace scope;
- introducing a new bounded context;
- changing deletion/lifecycle semantics;
- changing entitlement ownership;
- changing cross-context authority.

Such changes require:

1. explicit product decision;
2. impact analysis;
3. affected context-doc changes;
4. backend/frontend implementation impact;
5. contract/migration analysis where required;
6. test/gate updates;
7. ADR when the architecture consequence is significant.

---

# 26. Relationship to implementation maturity

This document defines intended product semantics.

It does **not** claim every described capability is fully implemented.

Current implementation status is determined by:

- source;
- tests;
- contracts;
- generated inventories;
- current repository [`CONTEXT.md`](CONTEXT.md);
- project-specific context files.

When source lacks an intended capability, that is an implementation/maturity fact.

When source contradicts this constitution, the discrepancy must be classified rather than silently normalizing either side:

```text
DOC_STALE
SOURCE_DEBT
TRANSITION
CONTRACT_CHANGE
UNRESOLVED
```

A Coding Agent MUST NOT treat transitional source structure as permission to redefine product semantics.

---

# 27. Current product evidence

At the time this product constitution is being established, the repository contains backend Domain ownership areas corresponding to:

```text
Accounts
Analytics
Automation
Billing
Collaboration
Documents
Governance
Identity
Integrations
WorkManagement
Workspaces
```

The frontend currently has product-package ownership for selected capabilities and feature packages for additional application workflows.

This source shape is evidence of implementation coverage.

It is **not** the authority for redefining bounded-context semantics.

Detailed current-state evidence belongs in `CONTEXT.md`, backend/frontend context documents, generated inventories, and source.

---

# 28. Relationship to other root documents

## `README.md`

Explains Notrelix and gets a contributor oriented/running.

It summarizes product semantics from this document.

It MUST NOT independently redefine them.

## `RULE.md`

Defines repository-wide engineering/product invariants that apply across implementations.

This PRODUCT constitution provides the business/product meaning those invariants protect.

## `DESIGN.md`

Defines product design and interaction semantics.

It must express this product model rather than invent a conflicting one.

## `CONTEXT.md`

Describes current implementation reality and transitions.

It may differ from target maturity but not silently redefine intended product semantics.

## `AGENTS.md`

Defines how Coding Agents discover and respect product ownership before implementation.

## `docs/product/**`

Owns detailed product/context semantics.

---

# 29. Product identity

Notrelix should be understood as:

> **A coherent enterprise workspace operating system where structured work, knowledge, collaboration, automation, governance, integrations, commercial capability, and insight can work together without losing clear semantic ownership.**

Its competitive value is not that every possible feature lives in one application.

Its value is that teams can model, view, discuss, automate, connect, govern, and understand their work while the system preserves a coherent source of truth.

The product wins on:

- coherence;
- flow;
- flexibility with semantics;
- trustworthy state;
- strong ownership boundaries;
- enterprise safety without unnecessary user complexity.

The product should remain:

> **calm · focused · confident**

while its underlying architecture remains explicit enough to support long-term parallel development and controlled evolution.
