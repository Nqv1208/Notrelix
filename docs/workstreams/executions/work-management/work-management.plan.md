---
document_id: WRK-PLAN-WORK-MANAGEMENT
document_type: workstream-plan
status: active
revision: final-audit-v3
owner: work-management-team
candidate_baseline:
  branch: develop
  sha: 35702d0fa9fb01ed68b0667bab500030d60bd028
supersedes: work-management.plan.v2.md
---

# PLAN — Work Management Transactional Core (Final-Audit V3)

## 1. Purpose

This is the final normative **HOW** plan for Work Management P3. It is designed so a coding agent does not need to invent ordering, concurrency, migration, RLS, idempotency or certification mechanics.

## 2. Non-negotiable implementation rules
- Do not add a production project/service.
- Do not introduce GenericRepository.
- Do not expand Application EF/provider coupling.
- Do not rewrite applied migration history.
- Do not use a client numeric/fractional position as authoritative state.
- Do not retry SaveChanges-time ordering conflicts inside a handler.
- Do not read ordering siblings before acquiring the target-scope transaction lock.
- Do not add unique ordering indexes before brownfield normalization.
- Do not treat `notrelix_worker` unrestricted policy as tenant-safe worker execution.
- Do not hand-edit generated OpenAPI/client output.
- Do not mark a work unit D5; work units become READY_FOR_CERTIFICATION and CERT alone grants VERIFIED/STABLE.

## 3. Final execution sequence
- Phase 0 — baseline/source/consumer manifests
- Phase 1 — P3-A and P3-B dependency gates
- Phase 2 — ordering infrastructure contract (lock/placement/conflict)
- Phase 3 — brownfield ordering inventory + normalization migration preparation
- Phase 4 — relational/RLS data safety
- Phase 5 — Domain core hardening
- Phase 6 — protected Application use cases
- Phase 7 — Field/FieldOption + Checklist closure
- Phase 8 — ExpectedVersion read/write propagation + idempotency ownership
- Phase 9 — events/cross-context/realtime
- Phase 10 — API/OpenAPI/first-party coordinated cutover
- Phase 11 — migration/RLS deployment execution
- Phase 12 — security/performance/observability
- Phase 13 — semantic traceability + exact-SHA certification


## WM-V3-INV-001 — Refresh candidate and P3 source manifest

**Phase:** Phase 0  
**SPEC:** WMREQ001–154  

### Source surfaces
- `develop ref`
- `Domain/WorkManagement/**`
- `Application/Features/WorkManagement/**`
- `API/Endpoints/WorkManagement/**`
- `Infrastructure/Data/Configurations/WorkManagement/**`
- `backend/tests/**`

### Required implementation
- Record exact candidate SHA.
- Generate explicit P3 mutation manifest and P3 versioned-request manifest.
- Classify every active handler KEEP/HARDEN/RETIRE/OUT_OF_SCOPE.
- Record every ordering scope and owning parent ID.

### Must not
- Do not let a meta inventory test substitute for semantic tests.

### Evidence
- manifest diff
- architecture manifest test

### Migration / deployment
None

### Exit
- [ ] P3 request manifest exists.
- [ ] P3 versioned-request manifest exists.
- [ ] No active unclassified duplicate command family.

## WM-V3-INV-002 — Refresh first-party consumer manifest

**Phase:** Phase 0  
**SPEC:** WMREQ130–143  

### Source surfaces
- `frontend/packages/product/work-management/state/src/api/item.api.ts`
- `group.api.ts`
- `field.api.ts`
- `checklist.api.ts`
- `frontend/packages/product/work-management/**/hooks/**`
- `frontend/packages/foundation/query/src/optimistic-command.ts`
- `frontend/packages/foundation/contracts/src/client/api-client.ts`

### Required implementation
- List each changed producer operation and all enabled handwritten/generated consumers.
- Record where current aggregate Version originates.
- Record where one canonical mutation-attempt/idempotency key is created.
- Record auth/CSRF/network retry path.

### Must not
- Do not assume API adapter invocation equals one canonical mutation attempt; unchanged-payload transport retry must reuse the same attempt/key.

### Evidence
- consumer surface manifest test

### Migration / deployment
None

### Exit
- [ ] Every changed producer operation has a named consumer and mutation-intent owner.

## WM-V3-GATE-001 — P3-A prerequisite verification

**Phase:** Phase 1  
**SPEC:** WMREQ080–104  

### Source surfaces
- `Identity/Accounts certified contracts`
- `Workspace/Governance contracts`
- `EfRequestDataSession`
- `RlsSessionContext`

### Required implementation
- Reverify tenant/account/workspace containment and active request transaction before handler execution.
- Verify RLS session lifecycle and PostgreSQL test environment.

### Must not
- Do not bypass tenant scope with unrestricted system context.

### Evidence
- P3-A prerequisite integration tests

### Migration / deployment
None

### Exit
- [ ] P3-A prerequisites VERIFIED or explicit dependency blocker recorded.

## WM-V3-GATE-002 — P3-B authorization prerequisite verification

**Phase:** Phase 1  
**SPEC:** WMREQ105–109,145  

### Source surfaces
- `AccessPolicyEngine`
- `PostgresAccessFactsProvider`
- `Work P3 request descriptors`

### Required implementation
- Verify canonical resource/action mapping for Board/Item/Field/Group/Checklist.
- Run allow/deny/cross-tenant/revoked reference slice.

### Must not
- Do not add local Work role ladders.

### Evidence
- authorization architecture + production-composition tests

### Migration / deployment
None

### Exit
- [ ] P3-B prerequisite VERIFIED before protected release cutover.

## WM-V3-ORDER-001 — Introduce ordering-scope lock port

**Phase:** Phase 2  
**SPEC:** WMREQ041–55  

### Source surfaces
- `Application/Features/WorkManagement/Ordering/IWorkOrderingScopeLock.cs (new)`
- `Infrastructure/Data/WorkManagement/PostgresWorkOrderingScopeLock.cs (new)`
- `Infrastructure DI`

### Required implementation
- Define `WorkOrderingScopeKind` for BoardGroups, BoardFields, GroupItems, FieldOptions, ItemChecklists, ChecklistItems.
- Application calls `AcquireAsync(kind,parentId,ct)` inside active request transaction.
- Infrastructure computes stable lock key from UTF-8 `${kind}:${parentId:N}`, SHA-256, first 8 bytes big-endian signed Int64, then executes `SELECT pg_advisory_xact_lock(@key)` on current transaction connection.
- Lock is transaction-scoped and has no manual release.

### Must not
- Do not use the TODO JobLockService.
- Do not use process-local Semaphore.
- Do not start a second transaction.

### Evidence
- real PostgreSQL serialization test
- architecture port ownership test

### Migration / deployment
None

### Exit
- [ ] Two concurrent requests for same ordering scope serialize before sibling read.
- [ ] Different scopes are not globally serialized.

## WM-V3-ORDER-002 — Introduce canonical placement resolver

**Phase:** Phase 2  
**SPEC:** WMREQ042–45,131–132  

### Source surfaces
- `Application/Features/WorkManagement/Ordering/WorkPlacementIntent.cs (new)`
- `WorkPlacementResolver.cs (new)`

### Required implementation
- Input is `PreviousSiblingId?`, `NextSiblingId?`.
- Acquire target-scope lock before querying siblings.
- For move, exclude moved ID from active siblings before evaluating intent.
- Both null => append after current last or initial key if empty.
- Previous-only => previous must be current last.
- Next-only => next must be current first.
- Both => IDs must be distinct and currently adjacent in that order.
- Generate exactly one candidate key with FractionalIndexGenerator.

### Must not
- Do not accept raw FractionalIndex or double.
- Do not accept merely `previous.Position < next.Position` as adjacency.

### Evidence
- placement resolver tests for empty/prepend/append/middle/stale adjacency

### Migration / deployment
None

### Exit
- [ ] One shared placement rule is used by all normal P3 ordering mutations.

## WM-V3-ORDER-003 — Map named ordering uniqueness conflicts

**Phase:** Phase 2  
**SPEC:** WMREQ050–52,114,138  

### Source surfaces
- `EfRequestDataSession SaveChanges exception mapping`
- `Infrastructure provider error classifier`

### Required implementation
- Define named constraint allowlist: `ux_work_board_items_group_position_active`, `ux_work_board_groups_board_position_active`, `ux_work_board_fields_board_position_active`, `ux_work_checklists_item_position_active`, `ux_work_checklist_items_checklist_position`, `ux_work_field_options_field_position`.
- Catch provider DbUpdateException after SaveChanges only for those named constraints.
- Map to typed `OrderingConflictException(code: work.ordering.conflict)`; API maps to HTTP 409.
- Do not retry inside handler; client reload/rebase may retry same logical operation.

### Must not
- Do not map unrelated unique violations to ordering conflict.
- Do not leak provider error text.

### Evidence
- named-constraint mapping tests
- API 409 contract

### Migration / deployment
None

### Exit
- [ ] SaveChanges-time collision cannot surface as opaque 500.

## WM-V3-ORDER-004 — Define explicit rebalance maintenance contract

**Phase:** Phase 2  
**SPEC:** WMREQ053–55  

### Source surfaces
- `Application Work ordering maintenance command/port`
- `Infrastructure implementation if bulk persistence is used`

### Required implementation
- Rebalance is tenant-scoped, internal/maintenance-only and acquires same ordering scope lock.
- Load siblings in canonical database order, preserve exact stable identity order, generate N canonical keys.
- Emit one scope-level invalidation/rebuild signal if consumers cache opaque positions; do not model each rewritten key as a user reorder intent.
- Record before/after key statistics and affected scope.

### Must not
- Do not expose rebalance as normal drag.
- Do not silently alter logical sibling order.

### Evidence
- rebalance order-preservation test
- consumer invalidation/rebuild test

### Migration / deployment
None

### Exit
- [ ] Operational rebalance path exists and preserves exact order.

## WM-V3-MIG-ORDER-001 — Inventory brownfield ordering data

**Phase:** Phase 3  
**SPEC:** WMREQ055,098–102  

### Source surfaces
- `all Work ordering tables`
- `migration preflight tooling/script`

### Required implementation
- For each scope, detect invalid FractionalIndex grammar, duplicate positions, nulls and current max key length.
- Before changing collation, snapshot the **current production-effective database order** using the existing column collation and stable entity ID as the tie-breaker for equal position strings.
- Produce counts by table/scope and a stop list for relationally corrupt/unrecoverable data.

### Must not
- Do not add unique indexes before this report is clean/repairable.
- Do not silently choose arbitrary order for an ambiguous scope without recorded deterministic rule.

### Evidence
- preflight integration fixture

### Migration / deployment
Pre-migration data audit; no schema mutation yet.

### Exit
- [ ] All affected scopes are classified clean or deterministically normalizable.

## WM-V3-MIG-ORDER-002 — Normalize legacy ordering deterministically

**Phase:** Phase 3  
**SPEC:** WMREQ055,100–102  

### Source surfaces
- `forward migration/data migration step`
- `FractionalIndex normalization helper`

### Required implementation
- For each affected scope, preserve the preflight identity sequence exactly and assign `GenerateNKeysBetween(null,null,count)` results in that sequence.
- Stable entity ID is only the deterministic tie-breaker for rows whose prior position strings compare equal under the pre-migration database collation.
- Record changed row/scope counts.

### Must not
- Do not normalize clean scopes unnecessarily.
- Do not change logical order.

### Evidence
- upgrade fixture before/after identity-order assertion

### Migration / deployment
Forward data migration before unique-index creation.

### Stop conditions
- If parent/scope relations are corrupt or another condition prevents a deterministic preflight identity sequence, stop migration and require explicit operator resolution.

### Exit
- [ ] No invalid/duplicate canonical positions remain before uniqueness install.

## WM-V3-DATA-001 — Canonicalize ordering persistence representation

**Phase:** Phase 4  
**SPEC:** WMREQ046–47,098–104  

### Source surfaces
- `BoardItemConfiguration`
- `BoardGroupConfiguration`
- `BoardFieldConfiguration`
- `ChecklistConfiguration`
- `ChecklistItemConfiguration`
- `FieldOptionConfiguration`
- `EF migration`

### Required implementation
- Alter every canonical `position` column to PostgreSQL `text COLLATE "C"` or equivalent DDL proven to compare ordinally.
- Rebuild ordering indexes on same collation.
- Remove old varchar(50) constraints.

### Must not
- Do not rely on database default collation.
- Do not add a new Domain key grammar.

### Evidence
- DB-vs-Domain ordering parity tests with uppercase/lowercase/base62 boundary keys

### Migration / deployment
Forward EF migration after data normalization step is available.

### Exit
- [ ] Database ORDER BY and Domain CompareTo produce identical order for canonical test corpus.

## WM-V3-DATA-002 — Install unique active ordering indexes

**Phase:** Phase 4  
**SPEC:** WMREQ050,098–100  

### Source surfaces
- `same Work configurations/migration`

### Required implementation
- Create the six named unique constraints/indexes from WM-V3-ORDER-003.
- Use soft-delete filter where entity supports soft delete.

### Must not
- Do not install before WM-V3-MIG-ORDER-002 passes.

### Evidence
- real PostgreSQL uniqueness tests

### Migration / deployment
Forward EF migration; migration ordering is normalize → alter collation/type → unique indexes.

### Exit
- [ ] All six target scopes reject duplicate active position.

## WM-V3-DATA-003 — Implement explicit parent-derived app RLS

**Phase:** Phase 4  
**SPEC:** WMREQ080–93  

### Source surfaces
- `RlsSqlScripts/008_policies_workspace_scoped_domain.sql`
- `011_verification.sql`
- `RlsPolicyApplier`

### Required implementation
- FieldOption app policy derives through BoardField.
- ChecklistItem app policy derives through Checklist.
- BoardItemValue app write/read validates Item and Field are current tenant and same Board.
- Verification requires these explicit policies and cannot count generic-helper skip as coverage.

### Must not
- Do not add duplicate tenant columns.
- Do not redesign worker/support policy in P3.

### Evidence
- real PostgreSQL app-role CRUD matrix
- policy catalog verification

### Migration / deployment
RLS embedded SQL deployment, separate from EF migration.

### Exit
- [ ] App-role same-tenant allowed and cross-tenant denied for all three child tables.

## WM-V3-DATA-004 — Fix Domain/storage/validator compatibility

**Phase:** Phase 4  
**SPEC:** WMREQ094–97  

### Source surfaces
- `BoardConfiguration`
- `Board.cs`
- `BoardField validators`
- `other touched P3 validators`

### Required implementation
- Increase Board Description persistence capacity to >=5000.
- Keep wider DB title capacity if safe.
- Align Application validation limits with Domain, including BoardField Name 100.

### Must not
- Do not loosen Domain to match DB.
- Do not shrink DB only for cosmetic equality.

### Evidence
- boundary persistence + validator parity tests

### Migration / deployment
Forward EF migration only for true persistence-width changes.

### Exit
- [ ] Every Domain-valid boundary value persists.

## WM-V3-DOM-BOARD-001 — Preserve and close Board Domain core

**Phase:** Phase 5  
**SPEC:** WMREQ001–10  

### Source surfaces
- `Domain/WorkManagement/Boards/**`

### Required implementation
- Retain identity/lifecycle/default schema semantics.
- Ensure no-op/version/event behavior remains exact.

### Must not
- Do not rewrite aggregate architecture.

### Evidence
- Board identity/lifecycle/event/default-schema domain tests

### Migration / deployment
None

### Exit
- [ ] Board Domain source READY_FOR_CERTIFICATION.

## WM-V3-DOM-ITEM-001 — Preserve and close BoardItem Domain core

**Phase:** Phase 5  
**SPEC:** WMREQ011–25  

### Source surfaces
- `Domain/WorkManagement/Items/**`

### Required implementation
- Retain scope/hierarchy/lifecycle/member/label/value invariants.
- Ensure move/update methods support one generated position and semantic no-op.

### Must not
- Do not move authorization into Domain.

### Evidence
- Item identity/scope/hierarchy/lifecycle/assignment/value tests

### Migration / deployment
None

### Exit
- [ ] BoardItem Domain source READY_FOR_CERTIFICATION.

## WM-V3-DOM-FIELD-001 — Close BoardField/FieldValue semantics

**Phase:** Phase 5  
**SPEC:** WMREQ026–40  

### Source surfaces
- `Domain/WorkManagement/Fields/**`

### Required implementation
- Retain type/settings/default/option/value invariants.
- Ordinary FieldType patch remains forbidden.
- Add/Move Option consumes already-resolved canonical position through aggregate.

### Must not
- Do not create second FieldOption aggregate authority.

### Evidence
- Field/settings/default/option/value semantic tests

### Migration / deployment
None

### Exit
- [ ] Field/Value Domain source READY_FOR_CERTIFICATION.

## WM-V3-DOM-CHK-001 — Close Checklist aggregate semantics

**Phase:** Phase 5  
**SPEC:** WMREQ056–68  

### Source surfaces
- `Domain/WorkManagement/Checklists/**`

### Required implementation
- Add/retain aggregate-owned child add/remove/rename/due/assignee/set-completion behaviors needed by final API.
- `SetItemCompletion(bool)` is no-op if already desired state.
- Toggle remains separate explicit inversion.

### Must not
- Do not mutate child directly from Application.

### Evidence
- Checklist ownership + desired-state matrix tests

### Migration / deployment
None

### Exit
- [ ] Checklist Domain source READY_FOR_CERTIFICATION.

## WM-V3-APP-BOARD-001 — Close protected Board use cases

**Phase:** Phase 6  
**SPEC:** WMREQ001–10,105–113  

### Source surfaces
- `Application Boards commands/queries`
- `API Board DTOs`

### Required implementation
- Map accepted fields exactly.
- Use required ExpectedVersion for selected versioned mutations.
- Return Board Version on canonical reads.

### Must not
- Do not release until P3-B gate passes.

### Evidence
- Board Application/API/auth/stale writer tests

### Migration / deployment
None

### Exit
- [ ] Board protected source READY_FOR_CERTIFICATION.

## WM-V3-APP-ITEM-001 — Replace Item create/move with locked relative placement

**Phase:** Phase 6  
**SPEC:** WMREQ011–25,041–55  

### Source surfaces
- `CreateBoardItem`
- `MoveBoardItem`
- `MoveBoardItemUseCase`
- `WorkItemActions`

### Required implementation
- Acquire GroupItems scope lock.
- Resolve adjacency with WM-V3-ORDER-002.
- Generate one key.
- Use same producer-local path for HTTP and Automation.

### Must not
- Do not full-list rewrite.
- Do not accept numeric Position.

### Evidence
- create/move placement + auth + concurrency tests

### Migration / deployment
None

### Exit
- [ ] Item ordering source READY_FOR_CERTIFICATION.

## WM-V3-APP-ITEM-002 — Fix Item duplicate/update/value flows

**Phase:** Phase 6  
**SPEC:** WMREQ016,020,024–25,049  

### Source surfaces
- `DuplicateBoardItem`
- `UpdateBoardItem`
- `UpdateBoardItemFieldValue(s)`
- `ClearFieldValue`

### Required implementation
- Duplicate uses locked placement path.
- Remove unsupported UpdateItem fields.
- Single/bulk values validate before commit; bulk is all-or-nothing.
- Return Item Version in reads/summaries used by mutations.

### Must not
- Do not preserve phantom DTO fields.

### Evidence
- duplicate/update/value/no-op/atomicity tests

### Migration / deployment
None

### Exit
- [ ] Item mutation source READY_FOR_CERTIFICATION.

## WM-V3-APP-GROUP-001 — Replace Group normal reorder with locked relative move

**Phase:** Phase 6  
**SPEC:** WMREQ041–55  

### Source surfaces
- `ReorderBoardGroups or replacement MoveBoardGroup`
- `DuplicateBoardGroup`
- `Group reads/DTOs`

### Required implementation
- Acquire BoardGroups scope lock.
- Move one Group using adjacency resolver.
- Duplicate Group gets canonical group key; cloned Items preserve relative keys.
- Expose Group Version in mutation-facing read DTOs.

### Must not
- Do not full-list rewrite for drag.

### Evidence
- Group placement/duplicate/version tests

### Migration / deployment
None

### Exit
- [ ] Group source READY_FOR_CERTIFICATION.

## WM-V3-APP-FIELD-001 — Close Field create/update/move

**Phase:** Phase 7  
**SPEC:** WMREQ026–40,041–55  

### Source surfaces
- `CreateBoardField`
- `UpdateBoardField`
- `ReorderBoardFields/replacement`
- `BoardField DTOs`

### Required implementation
- Unknown type fails closed.
- Add `BoardField.Rename(string name, Guid updatedBy, DateTimeOffset updatedAt)` with trim, max-length 100, semantic no-op, audit/version update and the canonical BoardField-updated event.
- Final ordinary Field PATCH contains `Name?`, `Settings?`, required `ExpectedVersion`; `FieldType` is absent.
- Handler invokes `Rename` when Name is present and `UpdateSettings` when Settings is present.
- Move acquires BoardFields scope lock and uses adjacency resolver.
- Expose BoardField Version in all mutation-facing schema/read DTOs.

### Must not
- Do not full-list drag.
- Do not ordinary-PATCH FieldType.

### Evidence
- Field contract/placement/version tests

### Migration / deployment
None

### Exit
- [ ] Field source READY_FOR_CERTIFICATION.

## WM-V3-APP-OPTION-001 — Retire legacy FieldOption authority and close option move

**Phase:** Phase 7  
**SPEC:** WMREQ034–35,073–76  

### Source surfaces
- `Features/WorkManagement/FieldOptions/**`
- `BoardFields/Commands/*FieldOption*`
- `FieldOption API`

### Required implementation
- Remove legacy IRequest handlers after manifest proves no callers.
- Canonical BoardField-owned add/move acquires FieldOptions scope lock and uses adjacency resolver.
- Reclassify existing `BoardField.ReorderOptions()` as maintenance/rebalance-only or replace it; normal UI drag cannot call full-list reorder.

### Must not
- Do not leave two active option ordering semantics.

### Evidence
- legacy-handler absence test
- Option add/move/rebalance tests

### Migration / deployment
None

### Exit
- [ ] Exactly one FieldOption authority and one normal move mechanism remain.

## WM-V3-APP-CHK-001 — Route all child mutation through Checklist

**Phase:** Phase 7  
**SPEC:** WMREQ056–68  

### Source surfaces
- `Create/Delete/Update/Toggle ChecklistItem commands`
- `Checklist commands/queries/DTOs`

### Required implementation
- Load Checklist aggregate and mutate child through it.
- Add/retain `Checklist.RenameItem(itemId, title, actorId, now)` with title validation, semantic no-op, parent audit/version and canonical child-update event.
- Add/retain `Checklist.SetItemCompletion(itemId, desiredState, actorId, now)`; repeating the current state is a no-op.
- Final `UpdateChecklistItemRequest` contains only `Title?`, `IsChecked?`, required parent `ExpectedVersion`; remove `DueDate` and `AssigneeId`.
- Create/move ChecklistItem uses ChecklistItems scope lock.
- Create/move Checklist uses ItemChecklists scope lock.
- `/toggle` remains a separate inversion command and is never used to implement PATCH desired state.
- Expose Checklist Version in parent DTO and nested item read surfaces where client mutates a child.

### Must not
- Do not direct DbSet.Add/Remove ChecklistItem.
- Do not implement desired state with Toggle.

### Evidence
- Checklist ownership/placement/PATCH/version tests

### Migration / deployment
None

### Exit
- [ ] Checklist source READY_FOR_CERTIFICATION.

## WM-V3-CONC-001 — Normalize P3 ExpectedVersion write contract

**Phase:** Phase 8  
**SPEC:** WMREQ110–14,137  

### Source surfaces
- `P3 versioned-request manifest`
- `commands/requests`
- `ExpectedVersionTargetMap`

### Required implementation
- Change selected P3 ExpectedVersion fields to required positive non-null long.
- Remove nullable→0 sentinel pattern from P3 requests.
- Ensure target map contains exactly each manifest entry.

### Must not
- Do not sweep out-of-scope Forms/Views/Approvals into P3.

### Evidence
- manifest-driven architecture test
- stale writer integration

### Migration / deployment
None

### Exit
- [ ] Every manifest request has required version and one target mapping.

## WM-V3-CONC-002 — Propagate Version through canonical reads

**Phase:** Phase 8  
**SPEC:** WMREQ111,137,139–142  

### Source surfaces
- `BoardDtos.cs`
- `BoardItemDtos.cs`
- `BoardFieldDtos.cs`
- `BoardGroupDtos.cs`
- `ChecklistDtos.cs`
- `GetBoard/GetFullBoard/GetBoardItem/GetBoardItems/GetBoardSchema/GetChecklists`

### Required implementation
- Add `Version` to BoardDto/FullBoardDto.
- Add `Version` to BoardItemDto, BoardItemSummaryDto and mutation-facing slim/list DTOs.
- Add `Version` to BoardFieldDto/BoardFieldSchemaDto.
- Add `Version` to BoardGroupDto/BoardGroupSchemaDto.
- Add parent Checklist `Version` to ChecklistDto; ChecklistItem mutations use that parent version.
- Project actual AggregateRoot.Version in every corresponding query.

### Must not
- Do not invent ChecklistItem or FieldOption versions when parent aggregate is the concurrency authority.

### Evidence
- read DTO projection tests
- OpenAPI response schema tests

### Migration / deployment
None

### Exit
- [ ] Every first-party versioned mutation can obtain its ExpectedVersion from an authoritative read.

## WM-V3-IDEMP-001 — Move idempotency-key ownership to mutation intent

**Phase:** Phase 8  
**SPEC:** WMREQ115–17,143  

### Source surfaces
- `frontend Work mutation hooks/commands`
- `item/group/field/checklist api adapters`
- `foundation api-client`

### Required implementation
- Mutation/command layer creates one UUID/key for one canonical request payload attempt.
- Pass key into adapter request options.
- Adapter never creates a replacement key during auth/CSRF/network retry of that unchanged payload.
- If stale-placement/order-conflict/precondition requires reload/rebase and changes neighbor IDs, ExpectedVersion or another canonical field, create a **new attempt and new key**.
- A new user action also gets a new key.

### Must not
- Do not remove server idempotency.
- Do not generate key independently inside each retryable adapter call.

### Evidence
- frontend intent/retry unit tests
- server replay integration

### Migration / deployment
None

### Exit
- [ ] One canonical mutation attempt has one idempotency identity end-to-end; changed-payload rebase starts a new attempt.

## WM-V3-EVT-001 — Reverify P3 events/outbox after contract changes

**Phase:** Phase 9  
**SPEC:** WMREQ118–126  

### Source surfaces
- `Domain events`
- `integration event mappers`
- `outbox tests`
- `realtime mapper`

### Required implementation
- Inventory event schema/version/revision.
- Ensure move facts carry authoritative final scope/position/revision.
- Ensure outbox transaction fate remains correct.
- Ensure rebalance uses one maintenance invalidation/rebuild fact if consumers require opaque-position refresh.

### Must not
- Do not represent maintenance reindex as N user moves.

### Evidence
- event mapper/outbox/realtime post-commit tests

### Migration / deployment
None

### Exit
- [ ] Event source READY_FOR_CERTIFICATION.

## WM-V3-X-001 — Reverify Automation/Analytics/Collaboration boundaries

**Phase:** Phase 9  
**SPEC:** WMREQ127–129  

### Source surfaces
- `WorkItemActions`
- `WorkItemProjectionSource`
- `consumer adapters`

### Required implementation
- Automation target move accepts semantic placement intent, never raw key.
- Analytics rebuild/snapshot remains producer-owned and handles maintenance invalidation.
- Collaboration remains private-persistence free.

### Must not
- Do not expose Work DbSet across contexts.

### Evidence
- cross-context architecture/integration tests

### Migration / deployment
None

### Exit
- [ ] Named consumer contracts READY_FOR_CERTIFICATION.

## WM-V3-API-001 — Cut producer contract to V3 shapes

**Phase:** Phase 10  
**SPEC:** WMREQ130–138  

### Source surfaces
- `P3 request/response DTOs/endpoints`
- `OpenAPI`

### Required implementation
- Relative neighbor request shapes.
- Required ExpectedVersion.
- Read response Version.
- Exact Update Item/Field/Checklist fields.
- Exact 409 stale-placement/ordering-conflict and precondition/idempotency errors.

### Must not
- Do not keep compatibility DTO fields silently ignored.

### Evidence
- exact API/OpenAPI tests

### Migration / deployment
None

### Exit
- [ ] Producer OpenAPI READY_FOR_CERTIFICATION.

## WM-V3-FE-001 — Migrate first-party Work consumers

**Phase:** Phase 10  
**SPEC:** WMREQ139–143  

### Source surfaces
- `item.api.ts`
- `group.api.ts`
- `field.api.ts`
- `checklist.api.ts`
- `Work mutation hooks/state`

### Required implementation
- Use generated V3 request/response types.
- Use server Version values for ExpectedVersion.
- Compute neighbor IDs from current UI ordering; never numeric persisted position.
- Pass logical idempotency key into adapters.
- On stale-placement/order-conflict/precondition, invalidate/reload/rebase according to error contract; if the rebuilt canonical payload changes, create a new mutation-attempt idempotency key.

### Must not
- Do not use `any` to bypass contract.
- Do not keep persisted midpoint as hidden compatibility field.

### Evidence
- frontend typecheck/consumer surface/interaction tests

### Migration / deployment
None

### Exit
- [ ] Enabled first-party consumer READY_FOR_CERTIFICATION.

## WM-V3-APP-BOUNDARY-001 — Cap Application EF exception

**Phase:** Phase 10  
**SPEC:** WMREQ069–79  

### Source surfaces
- `IWorkManagementDbContext`
- `touched handlers`
- `Application EF approved baseline`

### Required implementation
- No new DbSet/provider/raw SQL exposure.
- Ordering lock is an Application port implemented in Infrastructure.
- Use-case-specific ports introduced only where touched logic materially benefits.

### Must not
- Do not repository-per-entity refactor the whole context.

### Evidence
- architecture no-growth test

### Migration / deployment
None

### Exit
- [ ] Application framework coupling does not grow.

## WM-V3-APP-BOUNDARY-002 — Close BoardSchema read skeleton

**Phase:** Phase 10  
**SPEC:** WMREQ072  

### Source surfaces
- `BoardSchema query`
- `Infrastructure BoardSchemaReadService`

### Required implementation
- Keep exactly one executable Application-owned read contract and registered Infrastructure implementation; otherwise delete unused skeleton.

### Must not
- Do not preserve duplicate authorities.

### Evidence
- read-boundary architecture test

### Migration / deployment
None

### Exit
- [ ] One BoardSchema read authority remains.

## WM-V3-DEPLOY-001 — Execute relational upgrade in fixed order

**Phase:** Phase 11  
**SPEC:** WMREQ094–104  

### Source surfaces
- `forward EF migration(s)`
- `preflight/normalization tooling`

### Required implementation
- Upgrade order: preflight → normalize affected ordering scopes → alter position type/collation → add unique indexes → widen Board Description → update snapshot.
- Record row/scope counts and constraint names.

### Must not
- Do not make index creation depend on undefined existing order.

### Evidence
- clean DB
- supported upgrade
- pending model

### Migration / deployment
Forward EF relational migration/data migration.

### Exit
- [ ] Clean and supported upgrade paths pass deterministically.

## WM-V3-DEPLOY-002 — Apply and verify RLS pack separately

**Phase:** Phase 11  
**SPEC:** WMREQ082–93,104  

### Source surfaces
- `embedded RLS scripts`
- `RlsPolicyApplier`
- `deployment command`

### Required implementation
- After relational migration, apply exact RLS pack.
- Record script hashes/version.
- Run catalog verification and app-role runtime CRUD matrix.

### Must not
- Do not infer policy deployment from EF migration success.

### Evidence
- RLS apply + runtime tests

### Migration / deployment
Versioned SQL policy deployment.

### Exit
- [ ] Relational and RLS states are both explicitly verified.

## WM-V3-QUAL-001 — Security closure

**Phase:** Phase 12  
**SPEC:** WMREQ146  

### Source surfaces
- `field/settings/value validators`
- `query authorization`

### Required implementation
- Bound flexible JSON size/depth/shape.
- Prove list/query routes cannot leak cross-tenant/resource data.

### Must not
- Do not log raw flexible payload.

### Evidence
- security tests

### Migration / deployment
None

### Exit
- [ ] Security source READY_FOR_CERTIFICATION.

## WM-V3-QUAL-002 — Performance and ordering-health closure

**Phase:** Phase 12  
**SPEC:** WMREQ147–150  

### Source surfaces
- `large Board queries`
- `authorization facts`
- `ordering metrics`

### Required implementation
- Verify pagination and bounded auth query shape.
- Expose metrics for max/order-key length, stale-placement conflicts, ordering unique conflicts and rebalance runs.
- Verify normal drag updates one entity.

### Must not
- Do not solve query cost by duplicating Work truth.

### Evidence
- performance + telemetry tests

### Migration / deployment
None

### Exit
- [ ] Performance/observability source READY_FOR_CERTIFICATION.

## WM-V3-TEST-001 — Enforce semantic WMREQ coverage

**Phase:** Phase 13  
**SPEC:** WMREQ001–154  

### Source surfaces
- `work-management.tests.v3.md`
- `traceability verification`

### Required implementation
- Maintain one row for each WMREQ001–154 mapping to at least one substantive test family.
- Meta families `BASE`, `TRACE`, `CI`, `INVENTORY` do not count as semantic coverage.
- CI fails if any requirement lacks substantive mapping.

### Must not
- Do not claim 154/154 because one meta-test references the whole range.

### Evidence
- traceability parser test

### Migration / deployment
None

### Exit
- [ ] Semantic coverage = 154/154.

## WM-V3-CERT-001 — Run exact-SHA certification

**Phase:** Phase 13  
**SPEC:** WMREQ151–154  

### Source surfaces
- `all P3 source/tests/docs/migrations/RLS/OpenAPI/frontend`

### Required implementation
- Run restore/format/build.
- Run every backend test project non-zero.
- Run focused semantic families non-zero.
- Run clean+upgrade+RLS deployment tests.
- Run OpenAPI/frontend contract/typecheck.
- Populate Certification V3 using same candidate SHA.

### Must not
- Do not use historical CI as final evidence.
- Do not skip critical PostgreSQL groups.

### Evidence
- full evidence packet

### Migration / deployment
None

### Exit
- [ ] No blocking V3 gap remains.
- [ ] CERT final status STABLE/D5.

# 4. PR decomposition
| PR | Scope |
|---|---|
| `PR-WM-00` | baseline manifests + decision closure |
| `PR-WM-01` | ordering lock/placement/conflict + brownfield preflight |
| `PR-WM-02` | ordering normalization + canonical collation/type + unique indexes + mapping width |
| `PR-WM-03` | parent-derived RLS + deployment verification |
| `PR-WM-04` | Board/Item protected core + relative ordering |
| `PR-WM-05` | Group/Field/FieldOption + legacy option retirement |
| `PR-WM-06` | Checklist aggregate + desired-state PATCH + relative ordering |
| `PR-WM-07` | ExpectedVersion read/write propagation + idempotency-intent ownership |
| `PR-WM-08` | events/cross-context + BoardSchema/boundary cleanup |
| `PR-WM-09` | API/OpenAPI/frontend coordinated cutover |
| `PR-WM-10` | quality + semantic traceability + exact-SHA certification |

# 5. Deployment order
```text
code capable of scope-lock/relative placement
→ ordering preflight
→ deterministic normalization
→ ALTER position => text COLLATE "C"
→ add named unique ordering indexes
→ other relational fixes
→ apply RLS policy pack
→ policy/runtime verification
→ producer API cutover
→ first-party consumer cutover
→ exact candidate certification
```

# 6. Global stop conditions
- P3-A/P3-B required dependency is not verified.
- Current transaction is unavailable when ordering lock port is called.
- Ordering scope contains ambiguous brownfield data that cannot be deterministically normalized.
- Database collation parity with Domain cannot be demonstrated.
- Read model cannot expose concurrency Version before write contract becomes required.
- Enabled first-party consumer cannot coordinate breaking API cutover.
- Worker-dependent tenant path needs unrestricted visibility without Platform decision.
- Architecture baseline must grow or weaken unexpectedly.
- Any WMREQ lacks substantive semantic test mapping.

# 7. PLAN Definition of Done
- [ ] Every material implementation decision is fixed by SPEC/PLAN.
- [ ] Ordering uses transaction-scope lock + exact adjacency + one-entity normal mutation.
- [ ] Brownfield normalization/collation/uniqueness order is explicit.
- [ ] Read-side Version and write-side ExpectedVersion are paired.
- [ ] Idempotency identity is owned above retryable API adapter.
- [ ] RLS relational/policy deployment channels are separate.
- [ ] Every work unit has source/change/must-not/evidence/exit.
- [ ] All work units end READY_FOR_CERTIFICATION rather than self-awarding D5.
