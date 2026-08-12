---
document_id: PROD-WORK-MANAGEMENT
document_type: product-context
status: active
owner: work-management
applies_to:
  - work-management
  - boards
  - fields
  - items
  - groups
  - views
  - forms
  - relations
  - formulas
  - rollups
  - approvals
  - checklists
  - workload
evidence:
  - PRODUCT.md
  - docs/product/product-model.md
  - docs/product/product-experience.md
  - docs/product/contexts/workspaces.md
  - docs/product/contexts/governance.md
  - docs/architecture/bounded-context-map.md
  - docs/architecture/data-ownership-and-consistency.md
  - docs/architecture/events-realtime-and-delivery-boundary.md
  - backend/src/Notrelix.Domain/WorkManagement/
  - backend/tests/
  - frontend/packages/product/work-management/
review_on:
  - board-model-change
  - field-type-change
  - item-value-model-change
  - view-engine-change
  - ordering-model-change
  - relation-or-derived-field-change
  - forms-change
  - approval-or-checklist-change
  - workload-change
  - work-management-deletion-change
  - work-management-event-change
---

# Work Management Context

> **Work Management is the flexible structured-work database of Notrelix: one authoritative Board/Field/Item model, presented through many views and extended through relations, formulas, forms, approvals, checklists, workload, and other explicitly governed capabilities.**
>
> It is not a Kanban application with additional screens.

This document is the canonical product owner for Work Management semantics.

It intentionally distinguishes:

```text
authoritative work state
from
view configuration
from
derived values
from
cross-context collaboration/automation/integration state
```

---

# 1. Mission

Work Management enables teams to model and execute varied work without hard-coding one workflow shape.

A Board can represent:

- project delivery;
- issue tracking;
- CRM-like records;
- operations;
- service workflows;
- planning;
- custom structured work.

The core model remains stable:

```text
Board
BoardField
BoardItem
BoardGroup
BoardView
```

Higher-order capabilities extend this model rather than replace it.

---

# 2. Owns

Work Management owns product semantics for:

```text
Board lifecycle/configuration
Board field schema/type configuration
Board item lifecycle/value state
Board groups
ordering
Board views
relations/dependencies
formulas/rollups/derived work values
forms/submissions where they create/manage work input
work templates
labels where they are Work Management-owned
checklists
approval requests/decisions attached to work
workload/capacity semantics where product-approved
```

The exact current implementation depth varies by capability.

---

# 3. Does not own

```text
Workspace lifecycle/membership
→ Workspaces

permission/policy/share/audit
→ Governance

Page/Block content
→ Documents

comments/mentions/notifications/activity
→ Collaboration

automation rule/execution engine
→ Automation

provider connection/sync
→ Integrations

commercial entitlement
→ Billing

report/metric source of derived insight
→ Analytics
```

---

# 4. Ubiquitous language

**Board** — Workspace-scoped structured work collection/database.

**BoardField** — one stable schema field/column definition.

**BoardItem** — one authoritative work record. Product noun is **Item**, not “Card”.

**Field value** — one Item's value for one BoardField.

**BoardGroup** — structural organization section/group.

**BoardView** — saved projection/configuration over shared Board data.

**Ordering key** — deterministic sortable position identity.

**Relation** — explicit stable reference between work resources.

**Derived field/value** — computed value whose authority remains its source dependencies.

---

# 5. WM-001 — Work Management is not Kanban CRUD

Kanban is one presentation over the same Board/Item/Field state.

Do not shape the core model around:

```text
Card
Column
Lane
```

as the only runtime truth.

---

# 6. WM-002 — One work model, many views

Table, Kanban, Calendar, Timeline, Form, Dashboard, and future view types operate over shared work state.

No view owns a duplicate authoritative Item store.

---

# 7. Board

A Board is a stable Workspace-scoped structured-work resource.

It defines the boundary in which:

- field schema;
- Items;
- groups;
- views;
- work-specific configuration

are coordinated.

---

# 8. WM-003 — Board belongs to exactly one Workspace

Every Board has one authoritative Workspace scope.

Optional placement in Space/folder does not change tenant identity.

A bare Board ID cannot bypass Workspace/resource authorization.

---

# 9. Board identity

Board identity survives:

- rename;
- view changes;
- group changes;
- template choice;
- display configuration.

Do not use display name as durable identity.

---

# 10. Board lifecycle

A Board may support product lifecycle such as:

```text
active
archived
deleted
restored where allowed
```

The exact state machine must define:

- mutation availability;
- visibility;
- child Item behavior;
- references;
- Automation/Integrations behavior.

---

# 11. WM-004 — Board lifecycle is not view lifecycle

Deleting/archiving one View cannot delete/archive the Board or Items.

View configuration is subordinate presentation state.

---

# 12. Board type/template

A Board may be created from:

- template;
- use-case preset;
- board type/configuration.

These can guide defaults.

They do not create a second runtime model.

---

# 13. WM-005 — Template is creation input, not hidden shared runtime authority

Instantiating a template creates normal Board/Field/Group/View identities.

Later template changes must not silently rewrite instantiated Boards unless an explicit linked-template feature is designed.

---

# 14. Flexible settings

Flexible configuration/JSON is acceptable only where extensibility requires it.

Typed configuration must have:

```text
discriminator
schema
validation
unknown/invalid behavior
version/evolution
```

JSON is not a semantic escape hatch.

---

# 15. BoardField

A BoardField defines stable schema for one category of Item value.

Expected properties include:

```text
stable identity/key
display label
field type
settings
default
system/custom policy
ordering
active/lifecycle state
```

---

# 16. WM-006 — Field identity survives label rename

Display name is mutable presentation.

Field identity/key is the stable schema reference used by:

- values;
- views;
- filters;
- formulas;
- automation;
- integrations;
- API/contracts.

---

# 17. Field type

A Field Type is a semantic contract, not only an enum value.

It defines where applicable:

```text
settings schema
canonical value representation
normalization
validation
default semantics
equality/no-op
filter operators
sort order/null behavior
grouping support
renderer/editor behavior
import/export
automation compatibility
index/materialization needs
```

---

# 18. WM-007 — A Field Type has one semantic contract

Do not implement one Field Type independently through divergent `switch` logic in:

- Domain;
- API;
- web;
- mobile;
- Automation;
- import/export.

Implementation registries may differ by layer/host.

The semantic contract must remain coherent.

---

# 19. Field-type taxonomy

Potential product field categories can include:

```text
text / long text
number
checkbox
date / datetime
status/select
multi-select
people
file/link
priority
timeline
progress
relation/dependency
formula
rollup
system/audit-derived fields
```

The exact supported set is executable evidence.

Adding one type requires the full contract, not only a new enum member.

---

# 20. Field settings

Type-specific settings may define:

- options;
- allowed range;
- precision;
- date/time behavior;
- grouping;
- relation target;
- formula expression;
- rollup function.

Settings must be validated.

---

# 21. Field default

Default value must be valid under the current type/settings.

Changing the default does not necessarily rewrite existing Item values.

---

# 22. WM-008 — Default is creation behavior, not retroactive data mutation

Unless product explicitly defines bulk migration, changing a Field default affects new/unset values according to contract.

It must not silently rewrite historical work records.

---

# 23. System fields

System-managed fields such as creation/update metadata cannot be treated like arbitrary custom fields.

Their mutability, deletion, derivation, and visibility are explicit.

---

# 24. WM-009 — System fields are capability-protected

A UI/API cannot delete/edit a system-managed field merely because it shares the BoardField storage model.

---

# 25. Field lifecycle

Field archive/delete needs semantics for:

```text
existing values
views
filters/sorts
formulas
rollups
Automation
Integrations
Analytics
history
restore
```

Removing a schema field must not reinterpret historical values as a different field.

---

# 26. BoardItem

A BoardItem is the authoritative work record.

It belongs to:

```text
one Board
one Workspace through Board
optional BoardGroup structural section
```

and owns/coordinates its values according to aggregate/application design.

---

# 27. WM-010 — Item is the authoritative work record

Do not create separate authoritative:

```text
KanbanCard
TableRow
CalendarRecord
TimelineTask
```

for the same Item.

Views map to the Item.

---

# 28. Item title/name

If an Item has a primary name/title, its semantics should be stable enough for cross-view identity and user recognition.

It is not the durable technical ID.

---

# 29. Field value

One Item value must be compatible with:

- active field;
- same Board;
- field type;
- current settings;
- product constraints.

---

# 30. WM-011 — Value must match current field schema

Unknown Field ID, cross-Board field, incompatible type, stale configuration, or invalid reference must fail rather than persist unvalidated arbitrary data.

---

# 31. Normalization

Input is normalized before semantic equality and commit where the type requires it.

Examples:

- normalized number precision;
- canonical selection ID;
- normalized relation identity;
- normalized dates/time zones.

Event/output should reflect the committed normalized meaning.

---

# 32. Semantic no-op

Setting a value to its current semantic value is a no-op.

It should not cause:

- fake version increment;
- duplicate success event;
- misleading audit/activity;
- unnecessary Automation trigger

unless a separate product fact is intentionally defined.

---

# 33. WM-012 — Item mutation uses optimistic concurrency for competing writes

Where stale writes can overwrite meaningful work, mutation should carry/validate expected version or equivalent concurrency identity.

Stale conflict fails before authoritative commit.

---

# 34. Bulk mutation

Bulk operations must define:

```text
per-item atomicity
whole-batch atomicity if required
partial success representation
retry/idempotency
user-visible failure
```

Do not half-update an unknown subset with no product contract.

---

# 35. Queryable values

Flexible values need enterprise-scale query semantics.

A canonical sparse/flexible representation may coexist with typed materialized/indexed projections for filter/sort/report.

---

# 36. WM-013 — Query-heavy values cannot require full-Board arbitrary JSON scans

Common filtering, sorting, grouping, and reporting must have a scalable query/index/materialization strategy.

Do not deserialize every Item's full value blob for enterprise queries.

---

# 37. Materialized value projection

A derived value/index projection needs:

```text
source Item/Field
typed representation
upsert/delete
rebuild
tenant scope
freshness
```

It is not a second mutation authority.

---

# 38. BoardGroup

A BoardGroup organizes Items structurally in the Board/main table model.

It may have:

- stable identity;
- display name;
- ordering;
- lifecycle.

---

# 39. WM-014 — BoardGroup is not universal status

Moving Item between BoardGroups changes structural group/ordering.

It does not automatically mean status changed.

---

# 40. Kanban grouping

Kanban groups by a configured compatible field.

Typical examples may include:

```text
Status
Select
Priority
People
```

only where grouping semantics are defined.

---

# 41. WM-015 — Kanban move mutates grouping field, not BoardGroup by default

Dragging between Kanban columns updates the configured grouping field value and relevant order.

It does not merely change BoardGroup.

---

# 42. Ordering

Ordering applies to relevant structures such as:

- fields;
- groups;
- Items;
- per-view/per-group positions where product defines them.

---

# 43. WM-016 — Ordering is deterministic and concurrency-aware

Use a canonical sortable-key strategy such as approved fractional indexing.

Requirements include:

```text
deterministic adjacent insertion
prefix/boundary correctness
duplicate-key avoidance under supported concurrency
migration/rebalance strategy
server-authoritative validation
```

Floating-point midpoint hacks and client-only authority are forbidden.

---

# 44. Reorder no-op

Moving an object to its effective current position should be a semantic no-op where possible.

Avoid churn in version/events.

---

# 45. Rebalance

If ordering keys grow/densify, rebalance is an implementation/data operation that preserves user-visible relative order.

It must not create semantic reordering.

---

# 46. BoardView

A BoardView is saved configuration over shared Board data.

It may define:

```text
view type
filters
sorts
visible fields
grouping
layout
date/timeline fields
owner/private visibility
default state
type-specific options
```

---

# 47. WM-017 — View configuration never owns duplicate Items

Updating/deleting a View changes presentation/query configuration.

It cannot delete/update Items except through explicit Item actions.

---

# 48. View schema validation

Before save/use, View references must resolve to compatible active fields.

Stale configuration after schema evolution must be:

```text
migrated
repaired
reported
```

not silently trusted.

---

# 49. WM-018 — View configuration validates against current Board schema

Invalid sort/filter/group/date/visible-field references fail or degrade explicitly.

Do not leave corrupt hidden view state.

---

# 50. Table view

Table emphasizes:

- dense record/field display;
- sorting/filtering;
- inline editing;
- BoardGroup structural organization.

It uses the same Item/Field truth.

---

# 51. Kanban view

Kanban emphasizes field-driven grouping and ordering.

Column identity is derived/configured from grouping semantics, not a permanent duplicate Board schema.

---

# 52. Calendar view

Calendar requires a Date/DateTime-compatible field.

It must not infer dates from arbitrary text.

---

# 53. Timeline/Gantt view

Timeline requires defined start/end or compatible timeline semantics.

Invalid ranges need explicit validation.

---

# 54. Form view versus Form capability

A Board may have a Form presentation/input capability.

Current Domain also contains first-class `Form`, `FormQuestion`, `FormSubmission` concepts.

This indicates Forms may be more than a simple BoardView implementation detail.

---

# 55. WM-019 — Form submission maps to authoritative Work Management operations

A Form can collect input.

Submission must map through validated Board/Field/Item semantics.

Form answers do not become an independent competing work database.

---

# 56. Form

A Form may define:

```text
target Board/workflow
questions
question-to-field mapping
validation
status/publication
submission behavior
access policy
```

where current product supports it.

---

# 57. Form Question

A question is an input prompt/configuration.

Question type does not replace target BoardField type semantics when mapped to a field.

Mapping must validate compatibility.

---

# 58. Form Submission

A Form Submission is the captured submission/workflow record where product-approved.

Its relationship to created/updated Item must be explicit:

```text
submission history
versus
authoritative Item state
```

Do not let the two silently diverge.

---

# 59. Public forms

If anonymous/public forms exist, Governance must define the scoped capability.

Public submission does not imply public Board read access.

---

# 60. WM-020 — Public form capability is write-scoped, not Workspace visibility

A public form may permit constrained submission while keeping Board/Items private.

Do not expose Board enumeration through form access.

---

# 61. Default/private View

Default-view semantics should be deterministic.

Private views must not leak:

- configuration;
- filters;
- hidden fields;
- private content

to unauthorized users.

---

# 62. View deletion

Deleting the default/private View must define safe fallback.

No Item data is deleted.

---

# 63. Relations

Relations store stable references between work resources.

They can target:

- Item;
- Board;
- potentially other approved work resource types.

---

# 64. WM-021 — Relation is stable identity, not embedded aggregate graph

A relation stores target identity and metadata.

It does not embed/mutate the foreign target aggregate directly.

Target existence/scope/authorization is validated through Application/Governance facts.

---

# 65. Relation target validation

External-reference validation may require:

```text
target exists
same/allowed Workspace
target is accessible
relation type compatible
no forbidden self/cycle
```

Pure Domain must not fetch target via repository/provider callback.

---

# 66. Relation visibility

A relation being visible does not automatically mean the target is readable.

Target authorization can be evaluated when expanding/opening the relation.

---

# 67. Relation deletion

Deleting/archiving target must define:

```text
dangling relation
tombstone
cleanup
historical retention
```

Do not silently retarget to another Item.

---

# 68. Dependency

Dependency is a relation with additional directional/workflow semantics where supported.

It may affect:

- scheduling;
- blocked state;
- timeline;
- Automation.

Define semantics explicitly rather than treating every relation as dependency.

---

# 69. Formulas

Current Domain contains `FormulaExpression` and `FormulaReturnType`.

Formula is derived Work Management semantics computed from owned source values/relations.

---

# 70. WM-022 — Formula result is derived and non-authoritative

A formula value is not independently editable.

Its truth is the expression plus source dependencies.

---

# 71. Formula expression

A formula language must define:

```text
field references
operators/functions
types
null/error behavior
version/evolution
security/cost limits
```

Do not eval arbitrary user code.

---

# 72. Formula dependency graph

Formula references form a dependency graph.

Cycles must be rejected or handled by explicitly designed semantics.

---

# 73. Formula recomputation

Recomputation may be:

- synchronous;
- projected/background;
- cached.

Freshness must be truthful.

---

# 74. Rollups

Rollups aggregate values through relations/grouping according to explicit semantics.

Examples:

```text
count
sum
average
min/max
```

where compatible with source field types.

---

# 75. WM-023 — Rollup is derived projection, not editable source

Rollup output must not become a second writable field value.

---

# 76. Rollup authorization

If a relation points to an inaccessible resource, rollup/security behavior must avoid leaking protected target data.

Governance applies to derived visibility.

---

# 77. Progress

Progress may derive from:

- checklist completion;
- numeric/status fields;
- dependency state;
- explicit field semantics.

Do not create ambiguous competing progress truths.

---

# 78. Labels

Current Domain contains a Labels capability.

If Work Management Labels are retained, define whether labels are:

- Board-scoped;
- Workspace-scoped;
- reusable taxonomy;
- field-backed values.

Do not duplicate Status/Select field semantics without justification.

---

# 79. WM-024 — Labels must have distinct semantics from select/status fields

If Label and Select/Status both exist, their lifecycle/reuse/query behavior must justify the difference.

Name similarity or UI chips are not sufficient.

---

# 80. Checklists

Current Domain contains a first-class Checklist concept and checklist item status.

A Checklist represents lightweight sub-work attached to an owned work resource where product-approved.

---

# 81. WM-025 — Checklist is subordinate work, not a duplicate Board

Checklist items may have completion/order/text semantics.

Do not evolve Checklist into a hidden second Item/Field engine unless product deliberately promotes it.

---

# 82. Checklist target

A Checklist should attach to a clear owner such as Item.

Its target/resource authorization follows the parent Work Management resource.

---

# 83. Checklist completion

Completion contributes to derived progress only according to explicit product rules.

Changing a checklist item should not silently mutate unrelated Status fields.

---

# 84. Approvals

Current Domain contains:

```text
ApprovalRequest
ApprovalDecision
ApprovalStatus
```

This is evidence for first-class approval workflow semantics inside Work Management.

---

# 85. Approval Request

An Approval Request represents a governed request for one or more decision makers to approve/reject a work-related decision.

It must define:

```text
target resource
requester
approver(s)
status
decision
time/expiry where applicable
re-request/cancel behavior
```

---

# 86. WM-026 — Approval decision is explicit business state

Approval cannot be inferred from:

- a comment saying “approved”;
- a Status field label alone;
- UI button click without committed decision state.

If Approval capability is used, its decision lifecycle is authoritative for that approval workflow.

---

# 87. Approval authorization

Only valid approvers may decide.

Governance and Workspaces provide required identity/access facts.

---

# 88. Approval concurrency

Two concurrent decisions/cancel/expiry actions must resolve deterministically.

A terminal decision must not be overwritten silently.

---

# 89. Approval side effects

Approval may trigger:

- Work Management state transition;
- Automation;
- notification/activity.

Those effects should follow the committed decision through explicit contracts.

---

# 90. Workload

Current Domain contains:

```text
WorkloadAllocation
WorkloadCapacity
WorkloadStatus
```

This is evidence for capacity/allocation semantics.

---

# 91. Workload Capacity

Capacity represents an approved workload limit/availability measure for a subject/time scope.

The exact unit/time model must be explicit.

---

# 92. Workload Allocation

Allocation represents how owned work contributes to capacity.

It may derive from:

- assignee/People field;
- estimates;
- time range;
- Item status/lifecycle.

---

# 93. WM-027 — Workload is derived from owned work facts unless explicitly edited as capacity policy

Allocation/usage should not create a second Item assignment truth.

Capacity may be independently configurable.

Allocation derives from authoritative work.

---

# 94. Workload freshness

Large workload projections may be eventually consistent.

User-facing planning surfaces should communicate relevant freshness/pending state.

---

# 95. Workload authorization

Workload views can aggregate member workload across a Workspace.

They must respect Governance/privacy rules.

---

# 96. Templates

Current Work Management source contains Templates.

A Work Management Template can seed:

- Board;
- Fields;
- Groups;
- Views;
- Forms/Automation hooks where approved.

---

# 97. WM-028 — Work template instantiation creates owned identities

Template instances become ordinary Work Management resources.

They must not remain mutable references to one shared template unless product explicitly supports linked templates.

---

# 98. Template versioning

Template evolution should not silently rewrite existing Boards.

Explicit re-apply/upgrade semantics are required if supported.

---

# 99. Approvals, Checklists, Forms, Workload and the core model

These capabilities are admitted only insofar as they preserve:

```text
Board/Item/Field source truth
stable ownership
explicit lifecycle
Governance authorization
cross-context boundaries
```

A folder existing in source is evidence, not enough reason to redefine the whole Work Management product model.

---

# 100. WM-029 — Extension capability must not create duplicate Item truth

Any new Work Management extension must identify whether it is:

```text
authoritative new work concept
configuration
derived projection
workflow state
subordinate content
```

and preserve one authoritative owner for Item/Field data.

---

# 101. Work Management and Workspaces

Workspaces owns:

- Workspace;
- membership;
- Spaces/Teams.

Work Management consumes Workspace scope and may reference Space placement where supported.

Board remains Work Management-owned.

---

# 102. Space placement

A Board may be placed in a Workspace Space/folder.

Space placement does not change Board tenant identity.

Cross-Workspace placement is invalid.

---

# 103. Work Management and Governance

Governance owns authorization/policy/sharing.

Work Management declares:

- resource kinds;
- actions;
- private/default View semantics;
- business prerequisites.

---

# 104. WM-030 — Every material Work Management operation is server-authorized

Protected operations include:

```text
Board read/list/create/edit/archive/delete
Field schema changes
Item reads/mutations
View read/private/manage
Form publish/respond where protected
Approval decision
bulk/export
relation expansion
```

Query endpoints are not exempt.

---

# 105. Guest access

Guests may access explicitly shared Work Management resources according to Governance.

They must not gain Workspace-wide Board/Item enumeration accidentally.

---

# 106. Public sharing

A shared/public Board or Form does not automatically make related:

- Documents;
- Boards;
- Items;
- relations

public.

Target authorization remains independent.

---

# 107. Work Management and Documents

Documents may link/embed Board/Item via stable resource identity.

Work Management does not own Page/Block content.

---

# 108. Work Management and Collaboration

Comments/activity/notifications target Work Management resource IDs.

Collaboration owns those artifacts.

---

# 109. Work Management and Automation

Work Management exposes stable trigger facts and target actions.

Automation calls normal authorized/idempotent Work Management use cases.

It never mutates Work Management tables directly.

---

# 110. Work Management and Integrations

Integrations owns:

- provider connection;
- external mapping;
- sync/replay.

Work Management owns Board/Item/Field values.

Provider model is translated.

---

# 111. Work Management and Analytics

Analytics consumes read/event/projection contracts.

It may derive metrics/dashboards.

It does not edit Work Management truth.

---

# 112. Work Management and Billing

Billing entitlement may govern:

- Board/member limits;
- premium views;
- Automation/form/advanced field capabilities.

Billing does not own Work Management state.

---

# 113. Events/facts

Potential stable facts include:

```text
BoardCreated/Archived/Deleted
FieldCreated/Changed/Removed
ItemCreated/Changed/Moved/Archived/Deleted
FieldValueChanged
GroupChanged
ViewCreated/Changed/Deleted
FormPublished/Submitted
ApprovalRequested/Decided
ChecklistChanged
RelationChanged
```

Publish only facts justified by consumers.

---

# 114. WM-031 — Public events expose stable product facts, not aggregate dumps

Events should carry:

- Workspace/resource identity;
- logical event identity/version;
- relevant changed fact.

Avoid full Board/Item graphs and protected data leakage.

---

# 115. Realtime

Realtime may signal:

```text
Item updated
Field value changed
View changed
Schema changed
```

Clients reconcile by version/query ownership.

Realtime does not replace authoritative Board/Item queries.

---

# 116. WM-032 — Realtime assumes duplicate/out-of-order delivery

Client behavior must be idempotent/version-aware or invalidate/refetch when uncertain.

A missed realtime message must not permanently corrupt work state.

---

# 117. Schema change and realtime

Field/View schema changes can invalidate client editors/filters.

Realtime/schema versioning should cause safe refetch/reconciliation.

---

# 118. Automation trigger stability

Automation should depend on stable logical Work Management facts, not internal Domain class names.

Changing event semantics requires consumer review.

---

# 119. Analytics event stability

Metrics may depend on Item/Field lifecycle events.

Semantic changes to event identity/meaning require Analytics review.

---

# 120. Provider sync stability

Integrations may replay/duplicate provider updates.

Work Management operations must preserve idempotency/concurrency semantics.

---

# 121. Deletion — Board

Board delete/archive must define:

```text
Items
Fields
Views
Forms
relations
checklists/approvals
Collaboration links
Documents links
Automation triggers
Integrations
Analytics
retention
```

---

# 122. WM-033 — Board deletion is explicit product lifecycle, not cascade symmetry

Cross-context data must not be destroyed merely because a database FK cascades.

---

# 123. Deletion — Item

Item deletion/archive should define:

- relation targets;
- comments/activity;
- approval/checklist history;
- Automation;
- provider mappings;
- Analytics;
- restore.

---

# 124. Deletion — Field

Removing a Field must not reinterpret historical values as another field.

Dependent:

- View config;
- formulas;
- rollups;
- forms;
- Automation;
- Integrations

must be migrated/rejected explicitly.

---

# 125. Deletion — View

View deletion removes only view configuration unless an explicit user action separately mutates Items.

---

# 126. Deletion — derived state

Formula/rollup/materialized projections can be rebuilt/removed according to source state.

Derived-state deletion is not source-data deletion.

---

# 127. Conflict model

Work Management conflicts may include:

```text
stale version
schema changed
field removed
relation target unavailable
ordering collision
approval terminal-state race
bulk partial failure
provider conflict
```

Each needs explicit recovery.

---

# 128. WM-034 — Conflicts fail without partial hidden mutation

Rejected mutation should preserve:

```text
authoritative state
version
events
ordering
audit/activity success facts
```

unless the operation contract explicitly supports partial success.

---

# 129. Stale schema

A client editing with an old Field/View schema must not persist invalid values/configuration silently.

Return conflict/validation and reload/migrate as appropriate.

---

# 130. Ordering collision

If concurrent insertion/reorder produces duplicate/invalid key, server conflict/retry/rebalance semantics must preserve deterministic order.

Client position is not final authority.

---

# 131. Relation target loss

If a target disappears between validation and commit, operation must fail or apply approved tombstone semantics.

Do not keep a falsely valid reference.

---

# 132. Bulk operations

Product should make partial success visible where supported.

Retries must avoid duplicating already-successful mutations.

---

# 133. Import

Import maps external tabular/provider input through Field schema and Item operations.

It must not bulk-insert arbitrary JSON bypassing validation/authorization.

---

# 134. WM-035 — Import uses Work Management semantics

Import validates:

```text
Board scope
Field mapping
types
required values
relations
permissions
idempotency
```

Migration tooling is a separate governed path.

---

# 135. Export

Export respects:

- Board/Item permissions;
- field-level permissions if used;
- selected View/filter semantics where requested;
- sensitive fields;
- large-query behavior.

---

# 136. Search

Search can index Board/Item text/fields.

Search remains derived and authorization-filtered.

---

# 137. Analytics/reporting queryability

Large-board reporting should use scalable projections/indexes.

Do not mutate source modeling solely to optimize one report without preserving semantic truth.

---

# 138. Time zones

Date/DateTime fields and Calendar/Timeline views must define:

- date-only versus instant;
- timezone;
- display/normalization.

Do not silently convert date-only business meaning into UTC instant.

---

# 139. People field

People assignment references stable principal/member identity.

Application/Governance validates:

- membership/activity;
- scope;
- access.

Domain does not fetch Identity/Workspaces repositories.

---

# 140. WM-036 — External-reference validation uses supplied facts/contracts

This applies to:

```text
People
Relations
Dependencies
external mappings
```

Domain validates local business shape with facts supplied from Application.

---

# 141. File/link field

File/link field semantics should distinguish:

- URL/reference;
- uploaded file/resource identity;
- display metadata;
- access/security.

Object-storage mechanics do not become Work Management semantics.

---

# 142. Select/status option identity

Option display label/color may change.

Stable option identity should preserve historical value/reference.

Do not use label text as durable value identity.

---

# 143. WM-037 — Option identity survives rename

Changing “In Progress” label/color should not turn all existing values into unrelated data.

---

# 144. Multi-select

Multi-select defines set semantics:

- duplicate prevention;
- ordering if meaningful;
- removed option behavior.

---

# 145. Number

Number field defines:

- decimal/precision;
- range;
- null;
- formatting versus stored value.

UI formatting does not alter canonical numeric meaning.

---

# 146. Date/DateTime

Date-only and timestamp are different semantics.

Field Type must define which is used.

---

# 147. Priority/status

Priority and Status may be specialized select-like types with stronger semantics.

Do not create universal status across contexts.

---

# 148. WM-038 — Work Status is context-local field semantics

A Work Management Status value is not:

- Account status;
- subscription status;
- Automation execution status;
- Integration status.

No shared global Status enum.

---

# 149. Formula/rollup errors

Derived value can be:

- valid;
- pending/stale;
- error.

Do not persist misleading previous value as if current without freshness/error semantics.

---

# 150. View filters

Filter operators depend on Field Type.

Do not allow arbitrary operators that produce undefined semantics.

---

# 151. Sorting

Sort order must define:

- nulls;
- text collation;
- numeric;
- date;
- select/status option ordering;
- derived values.

Frontend/server behavior must agree.

---

# 152. Grouping

Grouping compatibility is Field-Type semantics.

Not every field type is groupable.

---

# 153. Private View

A private View belongs to its allowed subject/owner.

Private config must not appear in:

- Board view list;
- realtime;
- shared API

for unauthorized users.

---

# 154. Default View

If the product defines a default, the invariant should be deterministic:

```text
at most one
or
exactly one active default
```

with safe fallback on deletion.

---

# 155. Workload versus Analytics

Workload planning is Work Management product semantics when it directly expresses assignment/capacity planning.

Broader workforce analytics/reporting belongs to Analytics.

---

# 156. Approval versus Governance

Approval is a business workflow decision.

Governance determines who may decide.

Governance does not own the approval business state.

---

# 157. Checklist versus Documents

A lightweight work checklist attached to an Item can remain Work Management.

Rich hierarchical authored content remains Documents.

Do not use Checklist as a generic document editor.

---

# 158. Form versus Documents

Form questions/submissions are structured work intake.

Long-form rich page content remains Documents.

---

# 159. Labels versus Collaboration

Labels classify work.

Mentions/reactions/activity remain Collaboration.

---

# 160. Current source alignment

Current `Domain/WorkManagement` contains:

```text
Approvals
BoardGroups
Boards
Checklists
Fields
Forms
Formulas
Items
Labels
Relations
Rollups
Templates
Views
Workload
```

and current source includes first-class `ApprovalRequest`, `ApprovalDecision`, `Checklist`, `Form`, `FormQuestion`, `FormSubmission`, `FormulaExpression`, `WorkloadAllocation`, and `WorkloadCapacity`. citeturn543802view1turn851615view2turn851615view3turn851615view4turn851615view5turn851615view6

This demonstrates that current implementation already extends beyond the narrower Board/Field/Item/View core.

The core model remains the organizing authority.

---

# 161. Current ambiguity watch

Current source capabilities must not be normalized into duplicate semantics accidentally:

```text
Labels
vs Status/Select

Workload
vs Analytics

Forms
vs BoardView

Checklists
vs Documents

Approvals
vs Governance

Templates
vs live Board state
```

Each boundary above is explicitly defined in this document.

---

# 162. Change impact — Field Type

Adding/changing a Field Type requires review of:

```text
settings schema
value/default normalization
validation
filter/sort/group
persistence
query materialization
API/generated contracts
web/mobile renderer/editor
Forms
Automation
Integrations
Analytics
import/export
```

---

# 163. Change impact — View Type

Review:

```text
configuration schema
field compatibility
query interpretation
mutation mapping
authorization/private semantics
realtime
web/mobile support
```

---

# 164. Change impact — Item/value

Review:

```text
concurrency
events
Automation
Integrations
Analytics
realtime/cache
search
history
```

---

# 165. Change impact — relation/formula/rollup

Review:

```text
dependency graph
cycles
target authorization
derived freshness
deletion
query performance
Automation/Analytics
```

---

# 166. Change impact — Form

Review:

```text
public/private access
field mapping
submission lifecycle
Item creation/update semantics
spam/abuse/security
Automation
Analytics
```

---

# 167. Change impact — Approval

Review:

```text
approver identity
Governance
terminal states
concurrency
notifications
Automation
audit/activity
```

---

# 168. Change impact — Workload

Review:

```text
People assignment
capacity source
date/time semantics
derived projections
privacy/authorization
Analytics overlap
```

---

# 169. Core Board checklist

```text
[ ] one Workspace
[ ] stable Board ID
[ ] lifecycle explicit
[ ] schema via BoardFields
[ ] Items remain authoritative
[ ] groups are structural
[ ] views do not duplicate data
[ ] ordering canonical
[ ] authorization server-side
[ ] events/realtime stable
[ ] deletion/reference policy explicit
```

---

# 170. Field-Type checklist

```text
[ ] settings schema
[ ] canonical value type
[ ] normalization
[ ] validation
[ ] default
[ ] equality/no-op
[ ] filter operators
[ ] sort/null behavior
[ ] grouping support
[ ] frontend display/editor
[ ] import/export
[ ] Automation
[ ] indexing/materialization
[ ] migration/version
```

---

# 171. Item mutation checklist

```text
[ ] Workspace/Board scope
[ ] permission
[ ] field compatibility
[ ] external-reference facts
[ ] expected version
[ ] semantic no-op
[ ] ordering
[ ] event
[ ] realtime/cache
[ ] Automation/Integrations/Analytics impact
```

---

# 172. View checklist

```text
[ ] shared Item source
[ ] type
[ ] active compatible fields
[ ] filter/sort/group semantics
[ ] private/default behavior
[ ] view-specific mutation mapping
[ ] mobile/web support
[ ] no duplicate Items
```

---

# 173. Derived-value checklist

```text
[ ] source dependencies
[ ] types
[ ] cycle rule
[ ] evaluation semantics
[ ] freshness
[ ] error state
[ ] rebuild
[ ] target authorization
[ ] non-editable derived authority
```

---

# 174. Extension capability checklist

```text
[ ] clear product purpose
[ ] clear owner
[ ] lifecycle
[ ] relation to Board/Item/Field
[ ] not duplicate existing concept
[ ] Governance
[ ] cross-context relations
[ ] consistency
[ ] deletion
[ ] events
[ ] frontend semantics
[ ] test evidence
```

---

# 175. Testing/evidence

Critical evidence should include:

```text
Board create/lifecycle
Field schema/type validation
Item create/update/value normalization
semantic no-op
optimistic concurrency
Group/order
fractional-index boundary/prefix/duplicate cases
View config/schema validation
Kanban field-driven mutation
Calendar/Timeline field compatibility
relation target/cycle/security
formula/rollup dependency behavior
Form mapping/submission
Approval lifecycle/concurrency
Checklist semantics
Workload/capacity
tenant/authz
events/realtime
deletion/reference
large-board query/index strategy
```

---

# 176. Stop conditions

Stop rather than guess if:

- a feature models Work Management as fixed Kanban;
- BoardGroup is used as universal status;
- a View begins storing duplicate Items;
- frontend hard-codes columns instead of schema;
- values/settings/config accept arbitrary unvalidated JSON;
- Domain fetches external references through repositories/providers;
- common queries require full-Board JSON scans at scale;
- client is final ordering authority;
- Automation/Integrations mutate Work Management persistence directly;
- read/search endpoints skip authorization;
- Form creates a second source of Item truth;
- Approval is inferred from comments/status only;
- Workload duplicates assignment truth;
- Label duplicates Status/Select semantics without justification;
- Field deletion silently corrupts views/formulas/history.

---

# 177. Related canonical owners

```text
PRODUCT.md
docs/product/product-model.md
docs/product/product-experience.md
docs/product/contexts/workspaces.md
docs/product/contexts/governance.md
docs/product/contexts/documents.md
docs/product/contexts/collaboration.md
docs/product/contexts/automation.md
docs/product/contexts/integrations.md
docs/product/contexts/billing.md
docs/product/contexts/analytics.md

docs/architecture/data-ownership-and-consistency.md
docs/architecture/events-realtime-and-delivery-boundary.md
docs/architecture/contract-boundaries.md

backend/docs/architecture/domain-modeling.md
backend/docs/architecture/infrastructure-and-data.md
frontend/docs/architecture/state-query-mutations.md
frontend/docs/architecture/realtime.md
```

---

# 178. Final Work Management rule

For every Work Management capability, Notrelix must be able to answer:

```text
What is authoritative Board/Item/Field state?
Which Workspace owns the scope?
Which Governance action protects it?
Is this state, configuration, workflow, or derived projection?
Which Field Type semantics apply?
How do views map to shared data?
How are ordering/concurrency/no-op handled?
Which external references must be validated?
Which Automation/Integrations/Analytics consumers exist?
What happens on archive/delete/schema change?
How does web/mobile render the same semantic model?
Can the design scale without full JSON scans or client-only truth?
```

The target is:

> **a flexible structured-work engine with one authoritative data model, many coherent views, explicit extension semantics, scalable queryability, and no hidden duplicate truth.**
