---
title: "Work Management Context"
document_class: constitution
normative: true
owner: work-management
maturity: FROZEN
conformance: CANONICAL
applies_to: work-management
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Work Management Context

## Mission

Work Management is the core flexible work-database capability: Boards define a workspace-scoped work collection and schema; BoardFields define dynamic columns; BoardItems are work records; BoardGroups organize table sections; BoardViews project/filter/sort/group the same underlying records. Higher-order capabilities such as relations, dependencies, formula/rollup and dashboards build on this model without turning the system into a fixed Kanban application.

### Owns
Board lifecycle/configuration; field schema/type configuration; item lifecycle/field values; board groups/ordering; view definitions; work relationships and derived semantics explicitly assigned here.

### Does not own
Workspace membership (Workspaces), permission policy/audit (Governance), document block content (Documents), comments/notifications/activity (Collaboration), automation execution engine (Automation), provider sync (Integrations), billing entitlements (Billing).

## Ubiquitous language

**Board**: workspace-scoped work database/table. **BoardField**: schema column with stable identity/key and type-specific contract. **BoardItem**: work record/row/task; the canonical domain noun is Item, not “Card”. **BoardGroup**: organizational section/group in the main table. **BoardView**: saved view configuration over board data. **Field value**: value of one BoardField for one Item. **FieldType**: typed value/configuration behavior. **Ordering key**: deterministic sortable position token. **Relation/Dependency**: explicit work references with target validation.

## Board contract

### WM-BOARD-101 — Board belongs to exactly one Workspace

A Board has authoritative workspace scope. Placement in a Space/folder may be optional, but cannot change tenant identity. Every read/write/list/query/realtime contract establishes workspace/resource authorization; a bare Board ID never bypasses scope.

### WM-BOARD-102 — Board is type-extensible, not hard-coded Kanban

A board may support general work-management/CRM/dev/service-style templates/configuration. A `board_type` or template discriminator can guide defaults/features, but MUST NOT change the core truth that fields/items/views define data. Kanban is a view.

### WM-BOARD-103 — Flexible settings are schema-validated

Opaque configuration/JSON is allowed where extensibility justifies it, but every typed configuration has discriminator/schema validation, unknown/invalid handling and version/evolution strategy. JSON is not a replacement for queryable/indexed data used at scale.

### WM-BOARD-104 — Template is creation input, not a second runtime model

A board template can provide initial schema/groups/views/settings. Instantiation creates normal Board-owned identities; runtime reads do not depend on mutating the template as shared state unless a future explicit template-link feature says otherwise.

## Field engine

### WM-FIELD-101 — BoardFields define schema; UI does not hard-code columns

Each BoardField has stable identity/key within its Board, display name, field type, type-specific settings/default, system/custom policy and ordering. The web/mobile renderer derives editable/display behavior from schema/registry contracts rather than a switch scattered across screens.

Stable keys/IDs survive display-label rename. Uniqueness is enforced in the appropriate active-field scope.

### WM-FIELD-102 — Each FieldType has one semantic contract

A supported field type defines, where applicable:
- settings schema and migration/version behavior;
- canonical value representation;
- default-value validation;
- input normalizer and value validator;
- equality/no-op semantics;
- supported filter operators;
- sort ordering and null behavior;
- grouping compatibility;
- frontend renderer/editor contract;
- import/export representation;
- automation compatibility;
- indexing/materialization needs.

The implementation may use registries/strategy objects/generated metadata, but MUST NOT implement the same type semantics with divergent `if/switch` logic across backend and multiple frontend components.

The minimum product taxonomy can include text/long text, number, checkbox, date/datetime, status/select/multiselect, people, file/link, priority, timeline/progress, dependency/relation, rollup/formula and system audit fields. Exact supported set is executable product evidence; adding a type requires the full contract above rather than only adding an enum member.

### WM-FIELD-103 — External-reference fields validate outside pure Domain I/O

People assignment validates referenced principal/user membership/activity through Application-provided facts. Relation/dependency validates target board/item existence, tenant scope and view permission through Application/Governance before Domain commits the normalized reference. Domain validates local shape/cycle/business constraints with supplied facts and never fetches the target itself.

### WM-FIELD-104 — System fields are protected by capability rules

CreatedAt/UpdatedAt/CreatedBy or other system-managed fields cannot be deleted/edited like arbitrary custom fields. Their mutability and derivation are explicit per type.

## Item and value model

### WM-ITEM-101 — BoardItem is the authoritative work record

An Item belongs to Board/workspace and may belong to a BoardGroup section. It owns/coordinates its field values according to aggregate design, name/title, ordering and lifecycle. `groupId` is not Kanban status.

### WM-ITEM-102 — Value must be compatible with field schema

Setting a value verifies the field belongs to the same Board, is active/editable, and the normalized value satisfies its FieldType/settings. Unknown field IDs or stale field configuration fail; they do not get stored as unvalidated arbitrary JSON.

### WM-ITEM-103 — Queryable values scale beyond a single JSON blob

Flexible JSON can be the canonical/transport representation for sparse values, but filter/sort/report/analytics at scale requires indexed/materialized value representation for query-heavy types (for example one item+field typed row/projection). The system MUST NOT scan/deserialise every Item's entire JSON values for large-board filtering/sorting/reporting.

A materialized value projection is derived from the authoritative item/field value and has a deterministic upsert/delete/rebuild contract. Not every field must be materialized in an early phase; the decision is driven by query use and documented.

### WM-ITEM-104 — Item mutation uses optimistic concurrency and semantic no-op

Stale expected version fails before commit; successful meaningful mutation increments once; semantic no-op changes neither audit/version/events. Bulk operations define per-item vs batch failure semantics explicitly instead of half-updating an unknown subset.

## Groups and ordering

### WM-GROUP-101 — BoardGroup is a table organization construct

BoardGroup may organize items into sections similar to Monday-style groups. It does not represent the universal Kanban column/state. Moving an item between table groups updates group/ordering only; moving a Kanban card between view columns changes the configured grouping field value.

### WM-ORDER-101 — Ordering is deterministic and concurrency-aware

Board/group/field/item ordering uses the established fractional/indexing strategy or another canonical sortable key. Key generation handles adjacent/prefix/boundary cases deterministically, avoids duplicate ordering keys under supported concurrency, and has a rebalance/migration strategy if density/length limits require it. Floating-point midpoint hacks or client-only order authority are forbidden.

## View engine

### WM-VIEW-101 — View is configuration over shared data

Table, Kanban, Calendar, Timeline/Gantt, Form/Dashboard and future supported view types store display/query configuration—not copied Item data. Deleting/updating a View does not delete/update Items except through an explicit user action targeting Items.

### WM-VIEW-102 — View config validates against current Board schema

Before save, references such as visible/hidden fields, sort/filter fields, grouping, date fields and type-specific options must reference compatible active fields. Invalid stale config after schema evolution is migrated, repaired or reported explicitly; it is not silently trusted.

### WM-VIEW-103 — Kanban grouping is field-driven

Kanban declares a compatible column/grouping field (typically Status/Select and other explicitly supported groupable types such as People/Priority where semantics are defined). Dragging Item from one Kanban column to another means mutate that Item's grouping field value and ordering within the destination grouping; it MUST NOT only change BoardGroup.

### WM-VIEW-104 — Calendar/Timeline use compatible temporal fields

Calendar selects a Date/DateTime-compatible field. Timeline/Gantt defines start/end or Timeline-compatible field semantics and validates ranges. Rendering cannot infer dates from arbitrary text fields.

### WM-VIEW-105 — Private/default view semantics are explicit

A private view is visible to its owner/allowed subject according to Governance and must not leak config through board list/realtime. Default-view selection has a deterministic invariant (for example at most/exactly one active default as the product defines) and safe fallback on deletion.

## Relations, formulas and derived data

### WM-REL-101 — Cross-item relation is stable identity, not embedded object graph

Relations store stable target identities and required relation metadata; target authorization is re-evaluated when viewing/expanding. Deleting/archiving a target follows explicit dangling-link/tombstone/cleanup semantics.

### WM-DER-101 — Derived fields declare dependency graph

Formula/rollup/progress derived values declare source fields/relations, cycle rules, evaluation semantics and consistency/freshness strategy. A derived value must not become an independently editable competing source of truth. Expensive derived computation can use projections/background recomputation with visible freshness semantics.

## Authorization and tenancy

### WM-AUTH-101 — Board/resource visibility is evaluated server-side

Private/workspace/shared/public-link semantics pass through Governance/resource authorization. Listing/searching Boards/Items must filter unauthorized resources in the data-access path; fetch-all-then-hide is forbidden. Guest access is limited to explicitly shared resources and cannot enumerate the workspace.

Field-level restrictions, if introduced, are a distinct policy and must be modeled explicitly rather than inferred from hidden UI columns.

## Cross-context contracts

- **Workspaces:** workspace/space identity, membership facts.
- **Governance:** Board/Item/View operations and share/visibility authorization.
- **Collaboration:** comments/activity/notifications target Work Management resource IDs through stable target contracts.
- **Documents:** ResourceLink/embedding uses stable Board/Item identities; target access checked independently.
- **Automation:** durable Work Management events are triggers; automation actions call normal authorized/idempotent Application use cases rather than mutate tables.
- **Integrations:** provider sync maps external identity to Board/Item contracts and handles replay.
- **Analytics:** consumes read/event/projection contracts; never edits Work Management truth.
- **Billing:** entitlement may enable limits/features but does not own Board state.

## Events and realtime

Emit completed business facts justified by consumers: board/schema/item/view lifecycle, relevant field-value change, etc. Events carry workspace/resource identity, stable logical event name/version and enough changed data for consumer action without dumping full aggregate graphs.

Realtime events are compact convergence hints/projections (e.g. item updated/field value changed) and never replace authoritative Board schema/Item queries. Clients reconcile by version and owned query/cache rules; duplicate/out-of-order delivery is expected.

## Deletion/archive

Board/Item/Field/View lifecycle is explicit. A “deleted” Field with historical values may require tombstone/retention/migration; removing it must not reinterpret old data as another field. Board deletion/archive must define collaboration links, document references, automation triggers, integrations and retention. Generic cascade across contexts is forbidden.

## Forbidden designs

- fixed Kanban columns as the primary Board schema;
- BoardGroup == status/kanban column;
- view tables storing duplicate Items;
- hard-coded frontend columns rather than BoardField schema;
- arbitrary unvalidated `values/settings/config` JSON;
- People/Relation target validation inside Domain via repository/provider callback;
- full-board JSON scans for common filter/sort/report at enterprise scale;
- client-only authoritative reordering;
- automation/provider code mutating Work Management persistence directly;
- query endpoints that skip authorization because they “only read”.

## Testing and change impact

A FieldType addition covers backend settings/value/default validation, normalization/no-op, filter/sort/group contract, persistence round-trip/materialization if used, generated API contract, frontend renderer/editor and automation compatibility decision. A View type covers config validation, query interpretation, mutation mapping, permission/private behavior and web/mobile support decision.

Critical Item/Board mutations cover success, rejected unchanged state, no-op, expected version, tenant/authorization, events and realtime/cache consequence. Large-board query paths have integration/performance evidence for index/materialization strategy. Changes to field/value/view event identity require Automation/Analytics/Integrations and frontend consumers review.
