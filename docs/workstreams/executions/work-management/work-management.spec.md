---
document_id: WRK-SPEC-WORK-MANAGEMENT
document_type: workstream-spec
status: active
revision: final-audit-v3
owner: work-management-team
candidate_baseline:
  branch: develop
  sha: 35702d0fa9fb01ed68b0667bab500030d60bd028
supersedes: work-management.spec.v2.md
---

# SPEC — Work Management Transactional Core (Final-Audit V3)

## 1. Purpose

This is the final normative **WHAT** contract for Work Management P3 after two independent audit passes. V3 keeps the V2 architecture direction but closes the remaining execution-level ambiguities: transaction-scoped ordering serialization, adjacency validation, ordinal database ordering, brownfield normalization, read-side version propagation, logical idempotency ownership, and non-meta semantic test traceability.

## 2. Candidate baseline

```text
Repository: Nqv1208/Notrelix
Branch: develop
SHA: 35702d0fa9fb01ed68b0667bab500030d60bd028
```

Final certification must use the post-implementation exact SHA, not this baseline SHA.

## 3. Architectural boundary

- Production remains Domain / Application / Infrastructure / Platform / API.
- Domain owns Work invariants and semantic state.
- Application owns use-case orchestration, target placement intent, authorization declarations and ports.
- Infrastructure owns PostgreSQL locking, EF persistence, migrations, RLS SQL application and provider exception mapping.
- API is transport-only.
- Governance remains the canonical authorization evaluator.
- Platform remains owner of generic messaging/realtime/background infrastructure.

## 4. P3 transactional spine

`Board → BoardItem → BoardField → FieldValue → BoardGroup → Ordering → Checklist`.

P4 Views and downstream consumers may consume this truth only after their dependency gates are certified.

## 5. Final audited mechanism decisions
### WM-V3-DEC-001 — Ordering serialization

Every normal ordering mutation acquires `IWorkOrderingScopeLock` before reading siblings. Infrastructure implements it with PostgreSQL `pg_advisory_xact_lock` inside the request transaction. The stable lock key is derived from `SHA-256("<scope-kind>:<parent-guid-N>")`, first 8 bytes interpreted as a signed 64-bit value. Hash collision only over-serializes; it cannot violate correctness.

### WM-V3-DEC-002 — Placement semantics

Both neighbor IDs absent = append. Previous-only = after current last sibling. Next-only = before current first sibling. Both present = insert between currently adjacent siblings. For a move, the moved entity is excluded before adjacency is evaluated.

### WM-V3-DEC-003 — Unexpected collision

Named unique ordering constraint violation is mapped by Infrastructure at SaveChanges to `work.ordering.conflict` / HTTP 409. Handler-level retry is forbidden because SaveChanges occurs after handler return.

### WM-V3-DEC-004 — Persistence ordering

Canonical `position` columns use PostgreSQL `text COLLATE "C"`; Domain continues ordinal string comparison. Normal indexes/unique indexes are rebuilt on the same collation.

### WM-V3-DEC-005 — Rebalance

Rebalance is an explicit tenant-scoped maintenance operation, not drag. It acquires the same scope lock and preserves exact logical order while regenerating canonical keys. It is required as the documented long-term key-density strategy even though normal columns are migrated away from varchar(50).

### WM-V3-DEC-006 — Brownfield normalization

Before uniqueness is added, duplicate/invalid keys are inventoried and normalized scope-by-scope with stable tie-breakers based on existing persisted order plus stable entity ID. Any ambiguous scope that cannot be deterministically recovered blocks migration.

### WM-V3-DEC-007 — ExpectedVersion read/write contract

Required ExpectedVersion is paired with read-side Version on the owning aggregate DTO. BoardField Version protects FieldOption mutation; Checklist Version protects ChecklistItem mutation; BoardItem Version protects FieldValue mutation.

### WM-V3-DEC-008 — Idempotency identity

The mutation layer creates one key per canonical request attempt. Low-level API adapters accept that key and transport retries reuse it while the payload is unchanged. A reload/rebase that changes canonical payload starts a new attempt and therefore a new key.

### WM-V3-DEC-009 — FieldOption authority

Only BoardField-owned FieldOption commands remain active. Legacy `Features/WorkManagement/FieldOptions/*` is retired.

### WM-V3-DEC-010 — Checklist desired state

PATCH sets desired child state. Toggle remains a separate explicit inversion command.

### WM-V3-DEC-011 — RLS deployment

EF forward migrations own relational state; embedded RLS SQL + `RlsPolicyApplier` own policy state. Both are required in deployment/certification.

### WM-V3-DEC-012 — Worker boundary

P3 certifies app-role tenant isolation only. Worker tenant-scope semantics remain a Platform dependency and cannot be inferred from broad worker RLS policies.

## 6. Confirmed baseline source gaps
- **WM-V3-GAP-001** — Create/Move Item numeric Position drift.
- **WM-V3-GAP-002** — Duplicate Item/Group string ordering arithmetic.
- **WM-V3-GAP-003** — Update Item accepted-but-ignored fields.
- **WM-V3-GAP-004** — Create Field unknown-type fallback and Update Field ignored semantics.
- **WM-V3-GAP-005** — full-list/silent-skip Field reorder.
- **WM-V3-GAP-006** — duplicate FieldOption command families and raw/hard-coded option positions.
- **WM-V3-GAP-007** — Checklist direct child mutation, ignored PATCH fields and desired-state implemented as toggle.
- **WM-V3-GAP-008** — scope-less Work child tables skipped by generic RLS helper.
- **WM-V3-GAP-009** — ordering indexes are non-unique.
- **WM-V3-GAP-010** — position persistence has no explicit ordinal collation and several mappings use varchar(50).
- **WM-V3-GAP-011** — Board Description DB width is narrower than Domain.
- **WM-V3-GAP-012** — nullable ExpectedVersion is treated as required by pipeline.
- **WM-V3-GAP-013** — canonical Work read DTOs do not currently expose aggregate Version needed by versioned mutations.
- **WM-V3-GAP-014** — first-party group/checklist/field/item adapters retain old ordering and/or request-control semantics.
- **WM-V3-GAP-015** — BoardSchema read skeleton remains unresolved.
- **WM-V3-GAP-016** — broad IWorkManagementDbContext remains transitional EF debt.

# Normative requirements

## Board

### WMREQ001 — Board is the canonical structured-work container

A Board is owned by Work Management, belongs to exactly one Account and Workspace, and remains the same resource across rename/archive/restore.

### WMREQ002 — Board containment is server-derived

AccountId and WorkspaceId used for persistence/authorization come from trusted execution/resource facts, never from an untrusted client field.

### WMREQ003 — Board identity is stable

Rename, description, visibility and lifecycle transitions do not replace Board identity.

### WMREQ004 — Board lifecycle is semantic

Archive, unarchive, delete and restore are explicit Domain transitions; ORM cascade is not the product lifecycle.

### WMREQ005 — Board version is concurrency state

Material Board mutations use optimistic concurrency where the public operation exposes ExpectedVersion.

### WMREQ006 — Board visibility is not authorization

Visibility is a Work fact consumed by the Governance decision path and cannot bypass canonical authorization.

### WMREQ007 — Board membership is resource-local

Board-level membership/role may refine resource facts but never replaces Workspace membership or Governance policy.

### WMREQ008 — Board creation owns one default schema

Initial system fields/groups are created once according to one producer contract and are idempotency-safe.

### WMREQ009 — Board schema initialization is deterministic

Retry cannot duplicate default fields/groups or produce divergent system field identity.

### WMREQ010 — Board request fields are never silently ignored

Every accepted Board field mutates an owned semantic, is explicitly compatibility-only, or is removed/versioned.

## BoardItem

## BoardItem

### WMREQ011 — BoardItem is the authoritative work record

All views project the same BoardItem; Card/Table/Calendar/Timeline representations are not additional transactional models.

### WMREQ012 — Item containment is invariant

Item Account/Workspace/Board/Group scope must be internally consistent.

### WMREQ013 — Item hierarchy is scoped

Parent/child relationships cannot cross Board/Workspace boundaries or create invalid cycles/depth.

### WMREQ014 — Item lifecycle is explicit

Complete/reopen/archive/unarchive/delete/restore are distinct transitions with deterministic no-op behavior.

### WMREQ015 — Item identity differs from display key

Stable Item ID is the cross-context identity; human-facing sequence/key is presentation identity.

### WMREQ016 — Create Item is idempotent

A retried logical create with the same idempotency identity cannot create multiple authoritative Items.

### WMREQ017 — Create Item uses relative placement

Normal create expresses placement using PreviousItemId/NextItemId or append intent; numeric/fractional client position is not authority.

### WMREQ018 — Move Item uses relative placement

Normal move expresses target Group plus PreviousItemId/NextItemId; Work validates neighbors and generates the persisted key.

### WMREQ019 — Move Item is producer-owned

HTTP and Automation/public-action callers converge on one Work-owned move use case.

### WMREQ020 — Update Item has no phantom fields

DescriptionMd/Priority/Cover are removed from the P3 update contract unless an explicit canonical Work semantic is implemented.

### WMREQ021 — Timeline semantics are explicit

Date-only and timestamp semantics are not conflated; serialization/storage convention is consistent.

### WMREQ022 — Assignment uses stable actor identity

Assignee/member checks use published Workspace/Identity facts; Work does not read peer private persistence.

### WMREQ023 — Labels remain Work-owned metadata

Label behavior does not replace Status/Select field or Collaboration tagging semantics.

### WMREQ024 — Single FieldValue mutation is atomic

Field existence/type/settings/scope are validated before Item mutation.

### WMREQ025 — Bulk FieldValue mutation is all-or-nothing

Unless an explicit partial-success API is introduced, one invalid entry prevents all authoritative changes.

## BoardField / FieldValue

## BoardField / FieldValue

### WMREQ026 — BoardField identity survives rename

Field ID is schema identity; label/name is mutable presentation metadata.

### WMREQ027 — Unknown FieldType fails closed

Creation never falls back an unrecognized type to Text.

### WMREQ028 — Field settings are type-validated

Settings JSON is validated by the current FieldType contract.

### WMREQ029 — System fields are protected

System field restrictions are Domain/Application invariants.

### WMREQ030 — Field PATCH supports Name and Settings exactly

P3 ordinary Field PATCH supports `Name?`, `Settings?` and required `ExpectedVersion`. Domain provides `BoardField.Rename(...)` and existing settings behavior. No accepted field is ignored.

### WMREQ031 — FieldType is immutable in ordinary PATCH

Changing FieldType is a compatibility/data-migration workflow, not a metadata update.

### WMREQ032 — Field type change is explicit migration

A future type migration defines value conversion/rejection, API and consumer impact.

### WMREQ033 — Field default is forward behavior

Changing default does not retroactively mutate existing values without explicit migration.

### WMREQ034 — FieldOption identity is stable

Option ID survives rename/color/order change.

### WMREQ035 — FieldOption deletion is compatibility-aware

Defaults and existing values referencing an option are validated before removal.

### WMREQ036 — FieldValue conforms to current schema

Unknown/cross-board/stale/incompatible values fail before persistence.

### WMREQ037 — FieldValue semantic no-op avoids churn

Equivalent normalized value does not increment version or emit duplicate change fact.

### WMREQ038 — Computed fields are non-writable

Formula/Rollup or other derived fields reject ordinary direct value mutation.

### WMREQ039 — Clear has one null semantic

ClearFieldValue uses one canonical empty/null representation.

### WMREQ040 — Field/value schema races fail safely

Concurrent schema/value changes cannot commit an invalid value under a newer schema.

## Ordering

## Ordering

### WMREQ041 — Ordering key is opaque producer state

Persisted `FractionalIndex` is generated by Work Management and is never accepted as authoritative client state. Database ordering must compare the persisted representation with the same ordinal semantics used by `FractionalIndex`.

### WMREQ042 — Normal ordering mutation is scope-serialized and neighbor-relative

Before reading placement neighbors, the use case acquires a transaction-scoped ordering lock for the target collection. Normal create/move then uses optional previous/next sibling IDs and generates exactly one new key for the new/moved entity.

### WMREQ043 — Neighbor intent has one deterministic contract

After acquiring the target-scope lock, the server loads active siblings excluding the moved entity when applicable. If both neighbor IDs are absent, placement means append. Previous-only requires that previous is the current last sibling; next-only requires that next is the current first sibling; both IDs require that previous and next are currently adjacent in that order. Otherwise the request fails with `work.ordering.stale-placement`.

### WMREQ044 — One normal drag mutates one ordered entity

A normal drag/create/move does not rewrite every sibling position. Multi-row position rewrite belongs only to explicit order-maintenance/rebalance.

### WMREQ045 — Canonical generator is unique key authority

All newly persisted ordering keys are produced by `FractionalIndexGenerator`; no endpoint, frontend helper, handler, migration or duplicate operation may invent a different key grammar.

### WMREQ046 — Database collation matches Domain comparison

All canonical Work `position` columns and indexes must use a PostgreSQL collation whose lexical order matches `StringComparer.Ordinal`; the target invariant is `COLLATE "C"`.

### WMREQ047 — Position storage cannot be narrower than Domain representation

Canonical ordering columns use `text COLLATE "C"` rather than arbitrary small varchar limits. Representation growth is monitored and handled by explicit rebalance; database width must not reject a Domain-valid key first.

### WMREQ048 — Duplicate Group generates only its own new group position

A duplicated Group receives a canonical key from the target Board scope. Its cloned Items may preserve the source Items' existing relative keys because they enter a new empty Group.

### WMREQ049 — Duplicate Item generates a canonical adjacent key

Duplicate Item participates in the same target Group ordering lock and neighbor-relative generation path as ordinary insertion.

### WMREQ050 — Ordering scope has a database collision guard

Active sibling scopes enforce a unique `(parent_scope_id, position)` invariant. The unique index is defense-in-depth behind scope serialization and protects against legacy/unlocked writers.

### WMREQ051 — Ordering scope is serialized before neighbor read

Application owns an `IWorkOrderingScopeLock` port. Infrastructure acquires PostgreSQL `pg_advisory_xact_lock` inside the already-open request transaction using a stable 64-bit key derived from `(scope kind, parent ID)`. The lock is acquired before loading active siblings and is released automatically at transaction end.

### WMREQ052 — Unexpected unique violation is a stable conflict, not an in-handler retry

Because `SaveChangesAsync` executes after the handler returns, normal handlers do not retry database unique violations. Infrastructure maps the named ordering-unique constraint violation to `work.ordering.conflict` / HTTP 409. The client reloads/rebases. If rebasing changes the canonical request payload (for example neighbor IDs or ExpectedVersion), the retry is a **new mutation attempt with a new idempotency key**.

### WMREQ053 — Rebalance is an explicit maintenance operation

Rebalance is not the normal drag API. It acquires the same ordering-scope lock, preserves the exact logical sibling order, rewrites keys with `GenerateNKeysBetween`, and is invoked only by an explicit tenant-scoped maintenance path/runbook when key-density/length monitoring requires it.

### WMREQ054 — Rebalance is representation-only

Rebalance must not change user-visible order. Downstream consumers that cache opaque positions are invalidated/rebuilt according to the Work producer contract; it is never represented as a sequence of user reorder intents.

### WMREQ055 — Ordering migration/rebalance preserves deterministic order

Brownfield normalization uses a stable tie-breaker for duplicate/invalid legacy positions, records affected scopes, preserves the pre-normalization logical order, then installs unique/index/collation invariants before normal V3 writes are enabled.

## Checklist

### WMREQ056 — Checklist is subordinate Work state

Checklist belongs to one BoardItem and is not a hidden second Board/Item engine.

### WMREQ057 — Checklist aggregate owns ChecklistItem mutation

Create/update/delete/toggle/set-completion of child items is performed through Checklist behavior.

### WMREQ058 — Checklist create uses server-owned placement

Multiple checklists under one Item receive unique relative ordering.

### WMREQ059 — ChecklistItem create uses server-owned placement

Multiple children under one Checklist receive unique relative ordering.

### WMREQ060 — ChecklistItem PATCH supports Title and desired completion exactly

P3 ChecklistItem PATCH contains `Title?`, `IsChecked?` and required parent `ExpectedVersion`. `DueDate` and `AssigneeId` are removed from this P3 PATCH because a nullable PATCH field cannot distinguish omission from explicit clear without an additional patch-value contract.

### WMREQ061 — Desired completion is set, not toggled

PATCH `isChecked=true` means completed=true; repeating it is a no-op.

### WMREQ062 — Toggle remains a separate command

A `/toggle` operation may invert state, but it is not reused to implement desired-state PATCH.

### WMREQ063 — ChecklistItem title mutation is aggregate-owned

P3 retains ChecklistItem title rename. `Checklist.RenameItem(...)` owns validation/audit/version/event semantics.

### WMREQ064 — Checklist due/assignee mutation is outside this P3 PATCH

ChecklistItem may continue to expose persisted due/assignee state on reads, but `DueDate` and `AssigneeId` are removed from the P3 generic PATCH. A future mutation contract must explicitly distinguish omitted versus clear/set values and must use Work/Identity published actor rules.

### WMREQ065 — Checklist parent lifecycle is explicit

Archive/delete/restore of BoardItem defines checklist behavior; FK cascade is not the semantic decision.

### WMREQ066 — Checklist concurrency is parent-based

Child mutation uses Checklist version/concurrency authority where child state is aggregate-owned.

### WMREQ067 — Checklist child RLS is parent-derived

ChecklistItem tenancy is derived from Checklist scope.

### WMREQ068 — Checklist API does not accept ignored state

`UpdateChecklistItemRequest` and its Application command contain only `Title?`, `IsChecked?`, required `ExpectedVersion`, plus route identity. No DueDate/AssigneeId compatibility ghost fields remain.

## Application / legacy closure

## Application / legacy closure

### WMREQ069 — Application owns orchestration

Handlers coordinate Domain and ports; Infrastructure does not own Work business policy.

### WMREQ070 — Current EF exposure is transitional debt

IWorkManagementDbContext DbSet exposure is capped and not a precedent.

### WMREQ071 — No new EF coupling in P3

New/touched P3 use cases prefer narrow read/write ports when practical.

### WMREQ072 — BoardSchema has one read boundary

Unused TODO Infrastructure skeleton is retired or becomes the implementation of an Application-owned port.

### WMREQ073 — Duplicate FieldOption command family is retired

`Features/WorkManagement/FieldOptions/*` is legacy. It is removed from MediatR discovery and tests after caller inventory proves no active producer depends on it.

### WMREQ074 — Canonical FieldOption path is BoardField-owned

API uses the BoardField aggregate command family only.

### WMREQ075 — FieldOption normal move uses the canonical scope lock

FieldOption create/move uses the BoardField-owned path, acquires the Field ordering scope lock, validates adjacent Option neighbors, and never accepts a raw persisted fractional key from the client.

### WMREQ076 — Legacy command retirement is architecture-tested

Retired FieldOptions handlers are removed from MediatR discovery/test baseline and cannot reappear.

### WMREQ077 — Logging stubs are not capability evidence

Stub consumers may remain only if explicitly classified; they never satisfy product integration readiness.

### WMREQ078 — No GenericRepository abstraction is introduced

P3 uses use-case/domain-specific boundaries rather than a generic repository retrofit.

### WMREQ079 — Cross-context adapters stay consumer-owned

Producer public contracts and consumer ACL/adapters keep current ownership grammar.

## Data / RLS

## Data / RLS

### WMREQ080 — Work core tables are classified for tenancy

Every P3 table is direct-scope or parent-derived-scope.

### WMREQ081 — Direct-scope tables use canonical app RLS

Boards/Items/Fields/Groups/Checklists and other tenant-column tables remain FORCE-RLS protected.

### WMREQ082 — FieldOption uses parent-derived app RLS

Scope derives from BoardField.

### WMREQ083 — BoardItemValue uses dual-parent derived app RLS

Scope validates Item and Field are current tenant and same Board before write.

### WMREQ084 — ChecklistItem uses parent-derived app RLS

Scope derives from Checklist.

### WMREQ085 — Parent-derived policy is explicit SQL

Scope-less child tables do not use the generic helper that intentionally skips them.

### WMREQ086 — RLS policy deployment is distinct from EF schema migration

Relational schema/index/collation changes use forward EF migrations. RLS policy text remains in versioned embedded `RlsSqlScripts` and is applied/verified through `RlsPolicyApplier`; certification records both deployment channels separately.

### WMREQ087 — Relational schema changes use forward EF migration

Unique indexes/column length/index changes are append-only migrations.

### WMREQ088 — App-role isolation is the P3 certification target

P3 proves tenant isolation using `notrelix_app` and real PostgreSQL.

### WMREQ089 — Worker/support policy semantics remain Platform-owned

P3 preserves required compatibility but does not declare broad worker access safe for tenant work.

### WMREQ090 — Tenant-scoped workers cannot rely on unrestricted Work visibility

Any worker-dependent P3 path remains gated by Platform worker-scope certification.

### WMREQ091 — RLS session state does not leak

Pooled connection reuse does not carry previous Account/Workspace scope.

### WMREQ092 — No query-filter bypass is treated as RLS bypass

IgnoreQueryFilters cannot become a security mechanism.

### WMREQ093 — RLS verification fails closed

Verification detects a named-but-skipped or policyless protected table.

## Mapping / migration

## Persistence / migration

### WMREQ094 — Database constraints must accept Domain-valid state

Persistence width/collation/constraints must accept every Domain-valid P3 value. A wider database representation is acceptable when Domain remains stricter.

### WMREQ095 — Board description storage accepts 5000

Current 1024 storage limit is expanded to at least the Domain 5000 limit.

### WMREQ096 — Board title 256 storage need not shrink

DB may be slightly wider than Domain; Domain 255 remains semantic authority.

### WMREQ097 — Validator limits align with Domain

Example: BoardField Name validator cannot allow 200 when Domain accepts only 100.

### WMREQ098 — Ordering unique indexes are scope-correct

Install unique active-position indexes for Item-by-Group, Group-by-Board, Field-by-Board, Checklist-by-Item, ChecklistItem-by-Checklist and FieldOption-by-Field after brownfield normalization. Filter soft-deleted rows where lifecycle requires reuse.

### WMREQ099 — Ordering columns use canonical persistence representation

All P3 ordering columns are migrated to `text COLLATE "C"` (or demonstrably equivalent ordinal ordering) before final certification; indexes are rebuilt on that representation.

### WMREQ100 — Brownfield ordering normalization precedes uniqueness

Before adding unique indexes, scan every ordering scope for invalid/duplicate keys. Normalize affected scopes under a deterministic stable tie-breaker and record counts. If logical order cannot be recovered deterministically, stop migration instead of silently reordering.

### WMREQ101 — Clean database path is certified

A clean PostgreSQL database applies relational migrations, then the versioned RLS policy pack, then passes policy/schema verification and application startup.

### WMREQ102 — Supported upgrade preserves stable identity and logical order

Existing data upgrades without stable-ID changes or user-visible ordering changes. Brownfield normalization evidence is part of the upgrade record.

### WMREQ103 — Applied migration history is append-only

Schema fixes are new forward migrations; applied baseline history is not rewritten.

### WMREQ104 — Pending model and policy deployment state are explicit

Final candidate has zero pending EF model changes and certification records the exact RLS script identity/hash applied after the relational migration.

## Authorization / concurrency / idempotency

### WMREQ105 — Authentication precedes Work authorization

API authenticates; Application resolves scope and authorizes.

### WMREQ106 — Governance owns policy evaluation

AccessPolicyEngine remains the single canonical evaluator.

### WMREQ107 — Resource facts are server-owned

Route IDs select resources but do not define authoritative tenant scope.

### WMREQ108 — No handler-local role ladder

Work handlers do not reinvent permission composition.

### WMREQ109 — Automation target actions are re-authorized

Delayed/current authority semantics are explicit for Work public actions.

### WMREQ110 — ExpectedVersion is a required positive public contract

Every P3 operation classified as optimistic-concurrency protected exposes a required positive non-null `ExpectedVersion`; public DTO, Application request, OpenAPI and enabled consumer agree.

### WMREQ111 — Read models expose the version needed for the next mutation

Every P3 resource whose mutation requires `ExpectedVersion` exposes its current aggregate `Version` through the canonical read model used by first-party callers. Nested board schema/item/checklist projections expose the owning aggregate version required by their mutation commands.

### WMREQ112 — ExpectedVersion target map is complete and P3-scoped

Every P3 request implementing `IExpectedVersionRequest` has exactly one aggregate/resource mapping. Architecture verification uses an explicit P3 versioned-request manifest so out-of-scope Views/Forms/Approvals are not accidentally pulled into this execution.

### WMREQ113 — Stale writer fails before durable side effects

A stale expected version produces the canonical precondition failure and commits neither authoritative state nor outbox/idempotency completion.

### WMREQ114 — Ordering collision is distinct from aggregate stale-write

Two different moved aggregates can both have valid versions. Correctness therefore relies on ordering-scope serialization plus database uniqueness, not only `ExpectedVersion`.

### WMREQ115 — Idempotency identity is owned by one canonical mutation attempt

For first-party callers, the idempotency key is created when one canonical request attempt is created at the mutation/command layer, not freshly inside each low-level API adapter invocation. The key identifies the exact canonical payload being attempted.

### WMREQ116 — Transport retries reuse the key only while canonical payload is unchanged

Auth refresh, CSRF retry and network retry of the same canonical request payload reuse the same idempotency key. If a semantic conflict requires reload/rebase and changes neighbor IDs, ExpectedVersion or any canonical payload field, the client creates a new mutation attempt with a new idempotency key.

### WMREQ117 — Same key semantics are exact

Same key + same canonical payload yields one logical effect/replay; same key + different payload conflicts. Scope/operation partition prevents cross-tenant or cross-operation key collision.

## Events / cross-context / realtime

### WMREQ118 — Domain events are owned facts

Only completed Work semantic changes raise Work Domain facts.

### WMREQ119 — Integration events are explicit published contracts

Outward facts are mapped/versioned separately from Domain internals.

### WMREQ120 — Outbox enrollment is transactional

Source commit and outward intent share transaction fate.

### WMREQ121 — Broker failure does not roll back committed Work state

Durable outbox recovery handles transport failure.

### WMREQ122 — Consumer failure does not duplicate Work mutation

Retry is consumer-side/idempotent.

### WMREQ123 — Stub consumer is not business-effect evidence

Logging receipt proves transport only.

### WMREQ124 — Realtime notification is post-commit

Realtime never announces authoritative success before commit.

### WMREQ125 — Realtime payload carries stable identity/revision

Consumers can invalidate/reconcile without using realtime as truth.

### WMREQ126 — Realtime recovery remains Platform-owned

Work owns final state after recovery, not reconnect/gap protocol.

### WMREQ127 — Automation consumes Work action/event contracts

No private Work persistence mutation by Automation.

### WMREQ128 — Analytics consumes Work snapshot/event contracts

Projection rebuild source remains Work-owned.

### WMREQ129 — Collaboration target/reference does not transfer Work ownership

Cross-context target lookup/read uses approved published/consumer contracts.

## API / first-party consumers

## API / first-party consumers

### WMREQ130 — Endpoints are transport adapters

No Work business rule/persistence logic moves into API.

### WMREQ131 — Create/move ordering DTOs use neighbor-relative placement

Normal create/move DTOs expose `previousSiblingId?` and `nextSiblingId?`; both absent means append. The server validates first/last/adjacent semantics after acquiring the target-scope lock.

### WMREQ132 — Group/Field/Option/Checklist normal move uses the same relative contract

Normal drag for every P3 ordered collection uses the same neighbor-relative semantics and never a full ordered list or numeric midpoint.

### WMREQ133 — Rebalance is internal/maintenance, not drag

If exposed operationally, rebalance is separately named, tenant-scoped and privileged. Normal product drag cannot call it.

### WMREQ134 — Update Item DTO contains only implemented fields

No DescriptionMd/Priority/Cover phantom contract in P3.

### WMREQ135 — Update Field DTO contains only mutable semantics

Type is not ordinary patch.

### WMREQ136 — ChecklistItem DTO equals implemented semantics

ChecklistItem PATCH exposes only Title/IsChecked plus required parent ExpectedVersion; due/assignee mutation is outside this P3 PATCH.

### WMREQ137 — ExpectedVersion and Version are coherent in OpenAPI

Versioned mutation requests mark `ExpectedVersion` required, while corresponding canonical read responses expose the current `Version` consumed by first-party clients.

### WMREQ138 — Error taxonomy is exact

Validation, not-found, forbidden, precondition, ordering conflict and idempotency conflict are distinguishable.

### WMREQ139 — Frontend item adapter consumes server ordering and version truth

Item create/move sends neighbor IDs, supplies current aggregate version when required, and never sends a persisted numeric/fractional key.

### WMREQ140 — Frontend group adapter consumes server ordering and version truth

Group drag sends neighbor IDs and current Group version where required; it does not submit full-list `newPosition` values.

### WMREQ141 — Frontend field/option adapter consumes server ordering and version truth

Field/Option drag uses neighbor IDs; Field mutations use BoardField version; no legacy FieldOption command or raw position survives.

### WMREQ142 — Frontend checklist adapter consumes desired state, version and server ordering

Checklist/ChecklistItem mutations use Checklist version, desired-state PATCH semantics, relative placement and producer-required idempotency metadata.

## Quality / handoff

### WMREQ143 — First-party idempotency compliance is executable

Each enabled P3 mutation endpoint requiring idempotency is invoked with a canonical-attempt-owned key. Transport retries of unchanged payload reuse it; semantic rebase with changed payload creates a new key.

### WMREQ144 — OpenAPI diff is reviewed

Generated diff is a compatibility decision, not a CI silencer.

### WMREQ145 — P3-B gate precedes protected Application/API cutover

Domain/data work may proceed earlier; protected release work is blocked until auth prerequisites pass.

### WMREQ146 — Flexible JSON is bounded

Settings/value payload size/depth/shape are constrained.

### WMREQ147 — Large Board reads are bounded

Released query paths paginate and avoid full-board materialization.

### WMREQ148 — Authorization does not create N+1 per Item

Facts/query shape are bounded under representative Board size.

### WMREQ149 — Observability distinguishes core failure classes

Authorization, precondition, ordering collision, RLS, idempotency and delivery failures are diagnosable.

### WMREQ150 — Telemetry is safe

Raw flexible content/secrets are not logged by default.

### WMREQ151 — Exact candidate SHA is mandatory

Final executable evidence names one candidate.

### WMREQ152 — Zero discovered tests is failure

A green job with zero relevant Work P3 tests cannot certify.

### WMREQ153 — P4A consumes P3 truth only

Views cannot open until Board/Item/order/auth contracts meet dependency gate.

### WMREQ154 — D5 requires no open material blocker

Source, tests, migrations/RLS pack, API/generated contracts and exact-SHA CI must agree.

# Acceptance criteria
- **WMAC001** — No normal Work ordering mutation reads siblings before acquiring its target-scope transaction lock.
- **WMAC002** — Previous/next neighbor requests are rejected when they are no longer first/last/adjacent as declared.
- **WMAC003** — Database `ORDER BY position` is ordinal-compatible with Domain `FractionalIndex.CompareTo`.
- **WMAC004** — No active ordering column is constrained by the old varchar(50) representation.
- **WMAC005** — Brownfield normalization precedes unique ordering indexes and records every modified scope.
- **WMAC006** — Normal drag mutates one ordered entity; rebalance is explicit maintenance.
- **WMAC007** — Legacy FieldOption command family is absent from active request discovery.
- **WMAC008** — Checklist PATCH desired-state matrix passes and no accepted child field is ignored.
- **WMAC009** — Every P3 versioned mutation has a read path exposing the owning aggregate Version.
- **WMAC010** — Every enabled first-party idempotent mutation uses one logical-intent-owned key across retries.
- **WMAC011** — Parent-derived app RLS passes real-PostgreSQL CRUD isolation.
- **WMAC012** — EF relational migration and RLS policy-pack deployment both have separate evidence.
- **WMAC013** — Semantic TESTS mapping covers WMREQ001–154 without relying on inventory/trace meta-tests.
- **WMAC014** — Exact-SHA full and focused CI pass with zero material blocker.

# Stop conditions
- **WMSTOP001** — ordering scope lock cannot be acquired within the active request transaction;
- **WMSTOP002** — placement neighbors are not current first/last/adjacent but mutation would proceed;
- **WMSTOP003** — database collation cannot be proven ordinal-compatible;
- **WMSTOP004** — brownfield duplicate ordering data cannot be deterministically normalized;
- **WMSTOP005** — unique ordering constraint is added before normalization;
- **WMSTOP006** — handler-level retry is proposed for a SaveChanges-time collision;
- **WMSTOP007** — read DTO cannot supply required ExpectedVersion to first-party mutation;
- **WMSTOP008** — idempotency key is minted inside each retryable adapter invocation;
- **WMSTOP009** — scope-less child RLS remains unverified;
- **WMSTOP010** — worker broad policy is used as tenant-safety evidence;
- **WMSTOP011** — semantic test coverage has any WMREQ mapped only to meta-tests;
- **WMSTOP012** — migration/OpenAPI/architecture gate must be weakened to pass.

# Definition of Done
- [ ] WMREQ001–154 are implemented or explicitly NOT_APPLICABLE with authority.
- [ ] All WM-V3-GAP entries are closed or accepted as documented non-blocking debt outside P3.
- [ ] Ordering lock, adjacency, collation, normalization, uniqueness and maintenance strategy are executable.
- [ ] Version and idempotency identities propagate end-to-end.
- [ ] RLS/app authorization/event/migration/API boundaries remain canonical.
- [ ] TESTS V3 provides substantive semantic coverage for every WMREQ.
- [ ] CERTIFICATION V3 reaches STABLE/D5 on one exact candidate SHA.
