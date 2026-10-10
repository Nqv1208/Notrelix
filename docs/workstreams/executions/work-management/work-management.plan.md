---
document_id: WRK-PLAN-WORK-MANAGEMENT
document_type: workstream-plan
status: active
revision: v3.1-source-reconciled
owner: work-management-team
candidate_baseline:
  branch: develop
  sha: 8400a4c0
previous_baseline_sha: 35702d0fa9fb01ed68b0667bab500030d60bd028
supersedes: work-management.plan.v2.md
---

# PLAN — Work Management Transactional Core (V3.1)

## 1. Purpose

This is the normative **HOW** plan for Work Management P3. It is designed so a coding agent does not need to invent ordering, concurrency, migration, RLS, idempotency or certification mechanics.

V3.1 re-sequences V3 after source inspection at `8400a4c0` (see SPEC §1 and WM-V3-GAP-017..025): live defects move to the front, unique ordering indexes move after every compliant writer, brownfield normalization is replaced by the approved reset path, and the missing units for restore, unreachable mutations, the automation writer and inherited Governance debts are added.

## 2. Non-negotiable implementation rules
- Do not add a production project/service.
- Do not introduce GenericRepository.
- Do not expand Application EF/provider coupling.
- Do not rewrite applied migration history.
- Do not use a client numeric/fractional position as authoritative state.
- Do not retry SaveChanges-time ordering conflicts inside a handler.
- Do not read ordering siblings before acquiring the target-scope transaction lock.
- Do not add unique ordering indexes before the ordering preflight passes and every ordering writer (HTTP and automation) is compliant (SPEC WM-V3-DEC-014).
- Do not bypass the soft-delete filter without re-applying the tenant predicate explicitly (SPEC WM-V3-DEC-013).
- Every unit that changes a producer operation migrates the web adapters, the dev mock backend and its contract-conformance test in the same PR (SPEC WM-V3-DEC-017).
- Do not treat `notrelix_worker` unrestricted policy as tenant-safe worker execution.
- Do not hand-edit generated OpenAPI/client output.
- Do not mark a work unit D5; work units become READY_FOR_CERTIFICATION and CERT alone grants VERIFIED/STABLE.

## 3. Final execution sequence
V3.1 sequence. Unit IDs are unchanged from V3; each unit's **Phase** line below is authoritative.

| Phase | Scope | Units |
|---|---|---|
| 0 | baseline/source/consumer manifests | INV-001, INV-002 |
| 1 | P3-A/P3-B gates + inherited Governance debt disposition | GATE-001, GATE-002 |
| 2 | live-defect stabilization: soft-delete lifecycle, unreachable mutations, first-party idempotency keys | LIFE-001, FIX-001, IDEMP-001 |
| 3 | relational/RLS data safety for every `work` table + width/validator parity | DATA-003, DATA-004 |
| 4 | ordering infrastructure (lock/placement/conflict/rebalance/automation path) | ORDER-001..005 |
| 5 | Domain core hardening | DOM-BOARD-001, DOM-ITEM-001, DOM-FIELD-001, DOM-CHK-001 |
| 6 | protected Board/Item/Group use cases (compliant writers) | APP-BOARD-001, APP-ITEM-001, APP-ITEM-002, APP-GROUP-001 |
| 7 | Field/FieldOption + Checklist closure (compliant writers) | APP-FIELD-001, APP-OPTION-001, APP-CHK-001 |
| 8 | ordering persistence: reset path + preflight, collation/type, unique indexes | MIG-ORDER-001, DATA-001, DATA-002 |
| 9 | ExpectedVersion read/write propagation | CONC-001, CONC-002 |
| 10 | events/cross-context/realtime | EVT-001, X-001 |
| 11 | remaining API/OpenAPI/consumer cut-over + Application boundary | API-001, FE-001, APP-BOUNDARY-001/002 |
| 12 | deployment execution (relational + RLS pack) | DEPLOY-001, DEPLOY-002 |
| 13 | security/performance/observability | QUAL-001, QUAL-002 |
| 14 | semantic traceability + exact-SHA certification | TEST-001, CERT-001 |

Retired in V3.1: `WM-V3-MIG-ORDER-002` (brownfield normalization), replaced by the reset path in `WM-V3-MIG-ORDER-001` per SPEC WM-V3-DEC-006.


## WM-V3-INV-001 — Refresh candidate and P3 source manifest

**Phase:** Phase 0  
**SPEC:** WMREQ001–156  

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
- Record every ordering **writer** per scope, including non-HTTP paths (automation `AutomationMoveItemUseCase` → `IWorkActionPort` → `WorkItemActions`), as the input to the WM-V3-DEC-014 cut-over gate.
- Record every soft-deletable P3 aggregate and its Restore handler.

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
- `frontend/packages/product/work-management/state/src/mutations/**` (mutation owners)
- `frontend/packages/product/work-management/state/src/hooks/**`
- `frontend/packages/dev/mock-backend/**` (handlers + `__tests__/contract-conformance.unit.test.ts`)
- `frontend/packages/foundation/query/src/optimistic-command.ts`
- `frontend/packages/foundation/contracts/src/client/api-client.ts`

### Required implementation
- List each changed producer operation and all enabled handwritten/generated consumers.
- Record where current aggregate Version originates.
- Record where one canonical mutation-attempt/idempotency key is created.
- Record auth/CSRF/network retry path.
- Record which enabled calls currently omit a required `Idempotency-Key` or `ExpectedVersion` (WM-V3-GAP-018/019).
- Record mobile as a non-consumer (stub screens, no Work API calls) so WM-V3-DEC-017 stays valid.

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
- Disposition the inherited Governance debts (SPEC WMREQ155): `WG-DEBT-006` (AccessFactsQuery composite read of Work Board persistence) and `WG-DEBT-007` (`ManageBoard` broader than the Work command matrix: today 9 Board and 7 Group commands use it, and every Checklist mutation reuses `UpdateItem`). Record each one as closed by a named change or accepted with owner, risk and removal trigger.

### Must not
- Do not add local Work role ladders.
- Do not expand the AccessFactsQuery composite read or authorize a new command with `ManageBoard` only because it is broadest.

### Evidence
- authorization architecture + production-composition tests

### Migration / deployment
None

### Exit
- [ ] P3-B prerequisite VERIFIED before protected release cutover.
- [ ] WG-DEBT-006 and WG-DEBT-007 dispositions recorded in CERTIFICATION.

## WM-V3-LIFE-001 — Hydrate soft-delete lifecycle and make Restore reachable

**Phase:** Phase 2  
**SPEC:** WMREQ004, WMREQ014, WMAC015; WM-V3-DEC-013; WM-V3-GAP-017  

### Source surfaces
- `Domain/Common/SoftDeletableAggregateRoot.cs`, `Domain/Common/SoftDeletableEntity.cs`
- Work Restore handlers: `RestoreBoard`, `RestoreBoardItem`, `RestoreBoardView`, `RestoreForm`, `RestoreApprovalRequest`, `RestoreSavedFilter`
- Workspaces Restore handlers (`RestoreWorkspace`, `RestoreSpace`, `RestoreTeam`) as existing consumers of the same base type
- `Infrastructure/Data/Configurations/**` `Ignore(x => x.IsDeleted)` entries

### Required implementation
- Derive `IsDeleted` from `DeletedAt` (`public bool IsDeleted => DeletedAt is not null;`) and stop assigning it, so guards hold after reload. `WorkspaceRoute` maps its own `is_deleted` column and must be checked separately.
- Restore handlers load the target with `IgnoreQueryFilters()` **plus** an explicit Account/Workspace predicate from the resolved execution context.
- Restore of an active entity remains a semantic no-op; restore of an entity outside the tenant is NotFound.

### Must not
- Do not drop the tenant predicate when bypassing the soft-delete filter.
- Do not add a stored `is_deleted` column.

### Evidence
- PostgreSQL integration: delete then restore in separate DbContexts succeeds, bumps version once, raises one restored event; foreign-tenant restore is NotFound
- Domain tests unchanged and green

### Migration / deployment
None (no persisted shape change).

### Exit
- [ ] Every P3 soft-deletable aggregate restores from a fresh request against PostgreSQL.

## WM-V3-FIX-001 — Repair unreachable and lossy Work mutations

**Phase:** Phase 2  
**SPEC:** WMREQ010, WMREQ030, WMREQ110; WM-V3-GAP-020/021  

### Source surfaces
- `BoardGroups/Commands/DeleteBoardGroup`
- `API/Endpoints/WorkManagement/BoardFields/MapBoardFieldEndpoints.cs` (update/create mapping)

### Required implementation
- `DeleteBoardGroup` carries a real `ExpectedVersion` consistent with its target-map entry, or stops implementing `IExpectedVersionRequest` until CONC-001 fixes its contract; it must not hard-code `0`.
- Field PATCH must not substitute `"{}"` for omitted settings; omission means unchanged.
- Field create must not depend on culture-sensitive `double.ToString()`; ordering input is replaced fully by APP-FIELD-001.

### Must not
- Do not widen these fixes into the APP-FIELD-001 contract redesign.

### Evidence
- Application/Integration regression tests for each defect

### Migration / deployment
None

### Exit
- [ ] DeleteBoardGroup can succeed; rename-only Field PATCH preserves settings.

## WM-V3-ORDER-001 — Introduce ordering-scope lock port

**Phase:** Phase 4  
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
- Precedent for port shape: `Infrastructure/Workspaces/Members/WorkspaceOwnerUpdateLocker.cs` (lock on the request transaction).

### Must not
- Do not use the table-lease `JobLockManager` (`ops.job_locks`); it is a job lease, not a transaction-scoped lock.
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

**Phase:** Phase 4  
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

**Phase:** Phase 4  
**SPEC:** WMREQ050–52,114,138  

### Source surfaces
- `EfRequestDataSession SaveChanges exception mapping`
- `Infrastructure provider error classifier`

### Required implementation
- Define named constraint allowlist: `ux_work_board_items_group_position_active`, `ux_work_board_groups_board_position_active`, `ux_work_board_fields_board_position_active`, `ux_work_checklists_item_position_active`, `ux_work_checklist_items_checklist_position`, `ux_work_field_options_field_position`.
- Catch the provider `DbUpdateException` in `EfRequestDataSession` beside the existing `DbUpdateConcurrencyException` catch, and match only those named constraints via `PostgresException.ConstraintName` (SPEC WM-V3-DEC-015).
- Add an optional stable error code to `ConflictException` and emit it from `ProblemDetailsMapper` (uncoded conflicts keep `concurrency.conflict`); throw it with `work.ordering.conflict`. `ExceptionMappingBehavior` passes typed application exceptions through, so no eighth behavior is needed.
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

**Phase:** Phase 4  
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

## WM-V3-ORDER-005 — Bring the automation writer under the ordering contract

**Phase:** Phase 4  
**SPEC:** WMREQ019, WMREQ156; WM-V3-GAP-024  

### Source surfaces
- `Automation ... AutomationMoveItemUseCase`
- `WorkManagement/Public/ItemMovement/IWorkItemActions` / `WorkItemActions`
- `BoardItems/Services/MoveBoardItemUseCase`
- `Infrastructure/Messaging/DeduplicationConsumeFilter.cs` (consumer transaction owner)

### Required implementation
- The automation move acquires the GroupItems scope lock inside the consumer transaction and uses the shared placement resolver with semantic placement intent.
- Its `SaveChangesAsync` call maps the named ordering constraints to the same stable conflict as WM-V3-ORDER-003, classified as a deterministic (non-retryable within the same transaction) failure.
- The catch path must not attempt a second `SaveChanges` inside the aborted PostgreSQL transaction.

### Must not
- Do not route automation around the lock to avoid contention.

### Evidence
- PostgreSQL integration: concurrent HTTP move and automation move into the same gap serialize; injected collision yields the stable conflict without a second write attempt

### Migration / deployment
None

### Exit
- [ ] Automation and HTTP writers share one lock, resolver and conflict contract.

## WM-V3-MIG-ORDER-001 — Reset path and fail-closed ordering preflight

**Phase:** Phase 8  
**SPEC:** WMREQ055, WMREQ100–102; WM-V3-DEC-006  

### Source surfaces
- `ordering forward EF migration (preflight step)`
- `backend/docs/operations/migrations-and-data-change.md` (reset path)

### Required implementation
- Development/staging databases are reset before this migration is applied (approved V3.1 decision; no production database exists).
- The migration begins with a preflight that aborts with a named, operator-readable error if any active ordering scope contains duplicate, null or grammar-invalid positions. It never reorders data.
- Record in CERTIFICATION that no production database existed at cut-over; if one exists, stop (SPEC WMSTOP004) and restore the V3 normalization units.

### Must not
- Do not add unique indexes before the preflight passes.
- Do not normalize or reorder data silently.

### Evidence
- preflight integration fixture: clean scope passes; duplicate scope aborts with the named error and leaves the schema unchanged

### Migration / deployment
Pre-migration guard inside the forward migration; environments reset beforehand.

### Exit
- [ ] Clean database passes the preflight; a duplicate-position database is rejected without mutation.

## WM-V3-DATA-001 — Canonicalize ordering persistence representation

**Phase:** Phase 8  
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
Forward EF migration after the WM-V3-MIG-ORDER-001 preflight.

### Exit
- [ ] Database ORDER BY and Domain CompareTo produce identical order for canonical test corpus.

## WM-V3-DATA-002 — Install unique active ordering indexes

**Phase:** Phase 8  
**SPEC:** WMREQ050,098–100  

### Source surfaces
- `same Work configurations/migration`

### Required implementation
- Create the six named unique constraints/indexes from WM-V3-ORDER-003.
- Use soft-delete filter where entity supports soft delete; the item index is additionally filtered to root items (`parent_item_id IS NULL`, SPEC WM-V3-DEC-016).
- Index naming follows the existing `ux_` convention; the plan's `ux_work_*` names are authoritative for these six.

### Must not
- Do not install before WM-V3-MIG-ORDER-001 passes and every writer recorded by INV-001 is compliant (SPEC WM-V3-DEC-014).

### Evidence
- real PostgreSQL uniqueness tests

### Migration / deployment
Forward EF migration; migration ordering is preflight → alter collation/type → unique indexes.

### Exit
- [ ] All six target scopes reject duplicate active position.

## WM-V3-DATA-003 — Implement explicit parent-derived app RLS

**Phase:** Phase 3  
**SPEC:** WMREQ080–93  

### Source surfaces
- `RlsSqlScripts/008_policies_workspace_scoped_domain.sql`
- `011_verification.sql`
- `RlsPolicyApplier`

### Required implementation
- FieldOption app policy derives through BoardField.
- ChecklistItem app policy derives through Checklist.
- BoardItemValue app write/read validates Item and Field are current tenant and same Board.
- BoardMember app policy derives through Board (`board_members` is currently absent from every RLS script and holds authorization data).
- ApprovalStep and RelationFieldConfig app policies derive through their parent; they are outside P3 feature scope but inside the `work` schema RLS invariant (SPEC WMREQ080).
- Verification requires these explicit policies and cannot count generic-helper skip as coverage. `RlsPolicyApplier` must evaluate `011_verification.sql` results and fail when any RLS_DISABLED / NO_POLICY row exists; today the rows are discarded.

### Must not
- Do not add duplicate tenant columns.
- Do not redesign worker/support policy in P3.

### Evidence
- real PostgreSQL app-role CRUD matrix
- policy catalog verification

### Migration / deployment
RLS embedded SQL deployment, separate from EF migration.

### Exit
- [ ] App-role same-tenant allowed and cross-tenant denied for all six scope-less tables.
- [ ] RLS verification fails closed on a deliberately policyless fixture table.

## WM-V3-DATA-004 — Fix Domain/storage/validator compatibility

**Phase:** Phase 3  
**SPEC:** WMREQ094–97  

### Source surfaces
- `BoardConfiguration`
- `Board.cs`
- `BoardField validators`
- `other touched P3 validators`

### Required implementation
- Increase Board Description persistence capacity to >=5000 (today EF caps it at 1024 while Domain allows 5000).
- Keep wider DB title capacity if safe.
- Align Application validation limits with Domain, including BoardField Name 100 and the three divergent Board Description validator limits (2000 / 2000 / 500).

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
- Use same producer-local path for HTTP and Automation (automation lock/conflict behavior is owned by WM-V3-ORDER-005).
- Replace the ignored `double Position` contract with neighbor IDs and migrate the web adapter and mock backend handler in the same PR.

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
- Do not keep the reorder request body bound directly to the Application command (today it leaks `action`/`resource` into OpenAPI).

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

**Phase:** Phase 9  
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

**Phase:** Phase 9  
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

**Phase:** Phase 2  
**SPEC:** WMREQ115–17,143  

### Source surfaces
- `frontend Work mutation hooks/commands`
- `item/group/field/checklist api adapters`
- `foundation api-client`

### Required implementation
- This unit is delivered in Phase 2 because missing keys are a live 400 defect (SPEC WM-V3-GAP-018), not only an ownership refactor.
- Every enabled Work call to an endpoint marked `.WithIdempotencyKey()` sends a key.
- Mutation/command layer creates one random UUID key for one canonical request payload attempt (SPEC WM-V3-DEC-018); replace the module-counter `commandId` used as key by `optimistic-command.ts`.
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

**Phase:** Phase 10  
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

**Phase:** Phase 10  
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

**Phase:** Phase 11  
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

**Phase:** Phase 11  
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

**Phase:** Phase 11  
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

**Phase:** Phase 11  
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

**Phase:** Phase 12  
**SPEC:** WMREQ094–104  

### Source surfaces
- `forward EF migration(s)`
- `ordering preflight (WM-V3-MIG-ORDER-001)`

### Required implementation
- Upgrade order: reset pre-production database → preflight → alter position type/collation → add unique indexes → widen Board Description → update snapshot.
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

**Phase:** Phase 12  
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

**Phase:** Phase 13  
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

**Phase:** Phase 13  
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

**Phase:** Phase 14  
**SPEC:** WMREQ001–156  

### Source surfaces
- `work-management.tests.md`
- `traceability verification`

### Required implementation
- Maintain one row for each WMREQ001–156 mapping to at least one substantive test family.
- Meta families `BASE`, `TRACE`, `CI`, `INVENTORY` do not count as semantic coverage.
- CI fails if any requirement lacks substantive mapping.

### Must not
- Do not claim 156/156 because one meta-test references the whole range.

### Evidence
- traceability parser test

### Migration / deployment
None

### Exit
- [ ] Semantic coverage = 156/156.

## WM-V3-CERT-001 — Run exact-SHA certification

**Phase:** Phase 14  
**SPEC:** WMREQ151–156  

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
| `PR-WM-00` | V3.1 docs reconciliation + baseline/consumer/writer manifests + gate and Governance-debt disposition |
| `PR-WM-01` | soft-delete lifecycle + restore (LIFE-001) + unreachable/lossy mutation repair (FIX-001) |
| `PR-WM-02` | first-party idempotency keys (IDEMP-001) |
| `PR-WM-03` | RLS for every `work` table + fail-closed verification + width/validator parity |
| `PR-WM-04` | ordering lock/placement/conflict/rebalance + automation writer path |
| `PR-WM-05` | Domain hardening + Board/Item/Group compliant writers with API + web + mock cut-over |
| `PR-WM-06` | Field/FieldOption/Checklist compliant writers + legacy option retirement + desired-state PATCH |
| `PR-WM-07` | ordering persistence: preflight + collation/type + unique indexes (only after PR-WM-04..06) |
| `PR-WM-08` | ExpectedVersion read/write propagation |
| `PR-WM-09` | events/cross-context + remaining API/consumer cut-over + BoardSchema/boundary cleanup |
| `PR-WM-10` | quality + semantic traceability + exact-SHA certification |

# 5. Deployment order
```text
lifecycle/idempotency/RLS stabilization
→ code capable of scope-lock/relative placement on every writer (HTTP + automation)
→ reset pre-production databases
→ ordering preflight (fail closed)
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
- A production database exists while the V3.1 reset decision (SPEC WM-V3-DEC-006) is in force.
- A unique ordering index would deploy while a non-compliant ordering writer is reachable.
- Restore or any other filter bypass would require dropping the tenant predicate.
- Database collation parity with Domain cannot be demonstrated.
- Read model cannot expose concurrency Version before write contract becomes required.
- Enabled first-party consumer cannot coordinate breaking API cutover.
- Worker-dependent tenant path needs unrestricted visibility without Platform decision.
- Architecture baseline must grow or weaken unexpectedly.
- Any WMREQ lacks substantive semantic test mapping.

# 7. PLAN Definition of Done
- [ ] Every material implementation decision is fixed by SPEC/PLAN.
- [ ] Ordering uses transaction-scope lock + exact adjacency + one-entity normal mutation.
- [ ] Reset/preflight/collation/uniqueness order is explicit and writers precede uniqueness.
- [ ] Read-side Version and write-side ExpectedVersion are paired.
- [ ] Idempotency identity is owned above retryable API adapter.
- [ ] RLS relational/policy deployment channels are separate.
- [ ] Every work unit has source/change/must-not/evidence/exit.
- [ ] All work units end READY_FOR_CERTIFICATION rather than self-awarding D5.
