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
- Delivery is backend-first (SPEC WM-V3-DEC-017): a unit that changes a producer operation re-exports OpenAPI and regenerates the frontend client in the same PR, keeps frontend typecheck green, and records the first-party calls it breaks; web adapters, the dev mock backend and its contract-conformance test migrate in WM-V3-FE-001.
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
- [x] P3 request manifest exists.
- [x] P3 versioned-request manifest exists.
- [x] No active unclassified duplicate command family.

### Status
`READY_FOR_CERTIFICATION`. The manifest is executable: `backend/tests/Notrelix.Architecture.Tests/WorkManagement/WorkP3ManifestArchitectureTests.cs` classifies every Work request (P3 modules: KEEP/HARDEN/RETIRE with owning unit; other modules OUT_OF_SCOPE), holds the P3 versioned-request target set (SPEC WM-V3-DEC-007), and records every ordering writer per scope, including `InitDb` seed and the automation path through `MoveBoardItemUseCase`. The three gates fail on an unclassified request, an unlisted versioned request or an unrecorded position writer (verified by deleting entries).

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
- [x] Every changed producer operation has a named consumer and mutation-intent owner.

### Status
Inventory recorded (the migration itself is WM-V3-FE-001). Enabled first-party consumers are the web adapters in `frontend/packages/product/work-management/state/src/api/` (`item.api.ts`, `group.api.ts`, `list.api.ts`, `field.api.ts`, `checklist.api.ts`) behind the hooks/mutations of the same package, plus the dev mock backend and its contract-conformance test; mobile is a non-consumer (no Work API calls under `apps/mobile/src`), so SPEC WM-V3-DEC-017 holds.

| Producer operation | Web consumer | Idempotency-Key | ExpectedVersion | Placement |
|---|---|---|---|---|
| Item create | `item.api.ts` create | per adapter call (`crypto.randomUUID()`) | n/a | append (no neighbors) |
| Item PATCH / move / field value / field values / clear | `item.api.ts` | missing (live 400, SPEC WM-V3-GAP-018) | missing (live 400, WM-V3-CONC-001) | move: append |
| Group create | `group.api.ts`, `list.api.ts` | per adapter call | n/a | append |
| Group PATCH / delete / move | `group.api.ts`, `list.api.ts` | missing | missing | move: sequential append of a full desired order |
| Field create / PATCH / delete / move | `field.api.ts` | create only | missing | move: sequential append |
| Option add / update / move / remove | none (no web caller) | — | — | — |
| Checklist create / PATCH / delete, item create / PATCH / delete | `checklist.api.ts` | create calls only | missing | append; no move caller |

Version origin: the producer now publishes `version` on every board/group/field/item/checklist read and on item create/move/value responses (WM-V3-CONC-002), but the web types in `work-management/core/src/types/board-types.ts` do not map it yet. Key creation: one key per adapter invocation for creates only; `foundation/query/src/optimistic-command.ts` defaults the key to its module-counter `commandId`, and `foundation/contracts/src/client/api-client.ts` sets `Idempotency-Key` from the request options and repeats the same request on CSRF/auth refresh without minting a new key. Evidence: `tooling/contracts/enabled-consumer-surface.ts` with `consumer-surface-closure.unit.test.ts` and `contract-conformance.unit.test.ts` (frontend `test:node` 401/401).

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
- [x] P3-A prerequisites VERIFIED or explicit dependency blocker recorded.

### Status
`VERIFIED`. Tenant/account/workspace containment and the active request transaction before handler execution, the RLS session lifecycle and the PostgreSQL test environment are covered by: `RlsRuntimeEnforcementTests`, `AuthorizationFailClosedFaultInjectionTests`, `ProductionGraphTests`, `WorkChildTableRlsIntegrationTests`, `WorkSoftDeleteLifecycleIntegrationTests` (39 Integration tests), `RlsSessionContextTests` and `EfRequestDataSessionExpectedVersionTests` (13 Infrastructure), `ExecutionContextBehaviorTests` and `PipelineOrderTests` (9 Application). The ordering lock refuses to run without the request transaction (`WorkOrderingScopeLockIntegrationTests`). No system-context bypass was introduced; restore re-applies the tenant predicate explicitly.

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
- [x] WG-DEBT-006 and WG-DEBT-007 dispositions recorded in CERTIFICATION.

### Status
Dispositions are executable in `WorkPermissionActionMatrixArchitectureTests` (GOV-DEBT-001): the AccessFactsQuery read of Work persistence is frozen to `work.boards` and `work.board_members`, and every active P3 request has a pinned `PermissionAction`. `CreateBoardBySlugCommand` (SPEC WM-V3-GAP-027) is classified RETIRE instead of hardened; it had no endpoint and was deleted with WM-V3-APP-OPTION-001, together with its manifest, security-allowlist and API-factory entries. The P3-B allow/deny/cross-tenant/revoked matrix remains open and is still gated by Workspace & Governance Milestone B.

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
- [x] Every P3 soft-deletable aggregate restores from a fresh request against PostgreSQL.

### Status
`READY_FOR_CERTIFICATION`. `IsDeleted` is derived from `DeletedAt`; the two entities that still store `is_deleted` (`WorkspaceRoute`, `UserProfile`) map it through the `_isDeleted` backing field, so the EF model is unchanged (no migration). All six Work Restore handlers bypass the soft-delete filter with an explicit Account/Workspace predicate and are classified `Intentional` in both IgnoreQueryFilters architecture gates. Evidence: `WorkSoftDeleteLifecycleIntegrationTests` (5, PostgreSQL); 3 of them fail on the previous source.

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
- [x] DeleteBoardGroup can succeed; rename-only Field PATCH preserves settings.

### Status
`READY_FOR_CERTIFICATION` for the producer. `DELETE /api/v1/board-groups/{groupId}` now requires `expectedVersion` as a query parameter (the `DeleteSavedFilter` precedent) and passes it to the command; OpenAPI and the generated client were regenerated. The web adapter still cannot send it because `BoardGroupDto` exposes no `Version` until WM-V3-CONC-002, so first-party group delete remains blocked on that unit. `DeleteForm` has the same unreachable nullable→0 pattern; it is outside P3 and is recorded for the Views/Forms package. Evidence: `WorkMutationTransportRegressionTests` (2, API host).

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
- [x] Two concurrent requests for same ordering scope serialize before sibling read.
- [x] Different scopes are not globally serialized.

### Status
`READY_FOR_CERTIFICATION`. Port `Features/WorkManagement/Ordering/IWorkOrderingScopeLock`; implementation `Infrastructure/Data/WorkManagement/PostgresWorkOrderingScopeLock` runs `pg_advisory_xact_lock` on the request transaction and fails when no transaction is active. Scope kinds use spelled-out names so renaming an enum member cannot change keys. Evidence: `WorkOrderingScopeLockIntegrationTests` (4, PostgreSQL), including a fixed key test vector. No writer uses the port yet; adoption is APP-ITEM/GROUP/FIELD/OPTION/CHK.

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
- [x] One shared placement rule is used by all normal P3 ordering mutations.

### Status
`READY_FOR_CERTIFICATION` for the rule; the exit box is satisfied once the Phase 6–7 writers adopt it. `WorkPlacementResolver.ResolveAsync` acquires the lock before invoking the caller-supplied sibling loader, so the lock-before-read order is structural; siblings are sorted ordinally with a stable-id tie-break. Stale intents throw `ConflictException` with `work.ordering.stale-placement`. Evidence: `WorkPlacementResolverTests` (15).

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
- [x] SaveChanges-time collision cannot surface as opaque 500.

### Status
`READY_FOR_CERTIFICATION` for the mapping. `EfRequestDataSession` maps a unique violation of the six named constraints (`WorkOrderingConstraints`, matched by `PostgresException.ConstraintName`) to `ConflictException` with `work.ordering.conflict`; `ConflictException` now carries an optional stable code that `ProblemDetailsMapper` emits (uncoded conflicts keep `concurrency.conflict`). Evidence: `WorkOrderingConstraintsTests` (5), `ConflictProblemDetailsTests` (2). The end-to-end SaveChanges proof needs the named constraints and is delivered with DATA-002.

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
- [x] Operational rebalance path exists and preserves exact order.

### Status
`READY_FOR_CERTIFICATION` for the maintenance service. `WorkOrderingRebalancer` holds the scope lock, preserves logical order (ordinal, then stable id for equal legacy keys) and returns key-length statistics. Evidence: `WorkOrderingRebalancerTests` (2). Not yet delivered: the tenant-scoped maintenance entry point/runbook and the scope-level invalidation fact, which belongs to WM-V3-EVT-001.

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
- [x] Automation and HTTP writers share one lock, resolver and conflict contract.

### Status
`READY_FOR_CERTIFICATION`. The automation move runs through the shared `MoveBoardItemUseCase` (GroupItems lock inside the consumer transaction, resolver with an append intent; delivered with WM-V3-APP-ITEM-001). With the named constraints installed (WM-V3-DATA-002), `AutomationMoveItemUseCase` classifies a commit-time collision through the new `IOrderingConflictClassifier` port (Infrastructure: constraint-name based, shared with the HTTP mapping) and rethrows `ConflictException` with `work.ordering.conflict` before the technical-failure catch, so no second `SaveChanges` runs inside the aborted PostgreSQL transaction; the consumer transaction rolls back and redelivery re-resolves placement. Evidence: `AutomationMoveItemExecutorCompositionIntegrationTests.MoveItemComposition_OrderingCollisionAtCommit_PropagatesTheStableConflict_AndRedeliveryReResolves` (a lock-bypassing writer commits the resolved key before the automation commit; the execution stays Queued with no Work change, then redelivery succeeds).

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
- [x] Clean database passes the preflight; a duplicate-position database is rejected without mutation.

### Status
`READY_FOR_CERTIFICATION`. `WorkOrderingPreflight.Sql` is the first operation of migration `20261010065641_CanonicalizeWorkOrdering`: it checks every row of the six scopes against the Domain key grammar (`ValidKeyPredicate`, proven equal to `FractionalIndex.Create` on a valid/invalid corpus) and every active scope for duplicates, and raises `WM_ORDERING_PREFLIGHT_FAILED` naming each failing table, aborting the migration transaction. The reset path is recorded in `backend/docs/operations/migrations-and-data-change.md` §34. Evidence: `WorkOrderingPreflightIntegrationTests` (dedicated PostgreSQL migrated to the pre-cut-over schema: duplicate active group keys abort with the named error and leave column type, indexes, history and data unchanged; after the scope is made clean the same migration applies with stable keys and order), `WorkOrderingSchemaIntegrationTests.PreflightGrammarPredicate_AcceptsExactlyTheDomainGrammar`. Certification must still record that no production database existed at cut-over (WM-V3-CERT-001).

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
- [x] Database ORDER BY and Domain CompareTo produce identical order for canonical test corpus.

### Status
`READY_FOR_CERTIFICATION`. The six P3 position columns are `text COLLATE "C"` (`WorkOrderingConstraints.OrdinalCollation`; the `varchar(50)` limits are removed) and their ordering indexes are rebuilt by the same migration. Evidence: `WorkOrderingSchemaIntegrationTests.EveryP3PositionColumn_IsOrdinalText` (catalog), `DatabaseOrderBy_EqualsDomainCompareTo_ForTheBoundaryCorpus` (digits, upper/lower case, head-length and maximal integer keys). Zero pending model changes after the migration.

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
- [x] All six target scopes reject duplicate active position.

### Status
`READY_FOR_CERTIFICATION`. The six named unique indexes exist (`ux_work_board_items_group_position_active` filtered to active root items, the groups/fields/checklists indexes filtered to `deleted_at IS NULL`, and unfiltered checklist-item and option indexes); they replace the redundant non-unique position indexes. Installed only after every writer recorded by WM-V3-INV-001 is compliant and the retired writers are deleted (WM-V3-APP-OPTION-001), per SPEC WM-V3-DEC-014. Restore of a soft-deleted root item whose key was reissued meanwhile lands directly after the active holder of that key under the GroupItems lock instead of failing (SPEC WM-V3-DEC-019). Test fixtures that seeded sibling groups with the same key were corrected. Evidence: `WorkOrderingSchemaIntegrationTests.EachUniqueIndex_RejectsADuplicateActiveKey_WithItsConstraintName` (all six names, each recognized by `WorkOrderingConstraints.IsOrderingViolation`), `SoftDeletedAndChildItems_DoNotHoldTheActiveRootKey`, `WorkItemPlacementIntegrationTests.RestoreItem_WhoseKeyWasReissued_LandsRightAfterItsHolder_WithoutAConflict`.

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
- [x] App-role same-tenant allowed and cross-tenant denied for all six scope-less tables.
- [x] RLS verification fails closed on a deliberately policyless fixture table.

### Status
`READY_FOR_CERTIFICATION`. `ops.apply_scope_expression_policies` (004) applies the standard app/worker/support policy set with a parent-derived scope; 008 uses it for the six scope-less tables plus the four owned view-rule tables found by verification (SPEC WM-V3-GAP-026). `RlsPolicyApplier` now fails the apply when any table in an enforced schema (`work`) has RLS disabled or no policy. Evidence: `WorkChildTableRlsIntegrationTests` (5, dedicated PostgreSQL with the policy pack); cross-tenant read and write were reproduced before the change. Limits: the four view-rule tables are proven by the catalog gate only, not by a row-level probe; `automation.automation_execution_steps` is reported to the Automation package and is not in the enforced schema list.

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
- [x] Every Domain-valid boundary value persists.

### Status
`READY_FOR_CERTIFICATION`. Forward migration `WidenBoardDescription` widens `work.boards.description` from 1024 to 5000; Board title/description validators move to the Domain limits (255/5000) and BoardField name validators to 100. Evidence: `BoardMappingContractTests` (PostgreSQL, failed with 22001 before the change) and `WorkValidatorDomainParityTests` (8).

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
- [x] Board Domain source READY_FOR_CERTIFICATION.

### Status
`READY_FOR_CERTIFICATION` (verified, no source change). Every Board mutation (Rename, UpdateDescription, UpdateBackground, ChangeVisibility, Archive/Unarchive, Delete/Restore, SetDefaultGroup) normalizes, returns before audit/version/event on a semantic no-op, and otherwise increments the version once and raises one event; identity and containment are never reassigned. Evidence: `BoardTests`, `BoardEventTests`, `BoardIdempotencyTests` (default schema created once), `WorkSoftDeleteLifecycleIntegrationTests` (lifecycle on reloaded state).

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
- [x] BoardItem Domain source READY_FOR_CERTIFICATION.

### Status
`READY_FOR_CERTIFICATION`. `MoveToGroup` takes one resolved key, rejects cross-workspace/board groups, is a no-op for the same group and key, and its event carries the final group, key and version. Field values: the canonical cleared value (JSON null) is now accepted for option-backed fields (`FieldValue.IsEmpty`; previously Clear failed on every Select/MultiSelect/Status field), while a non-empty value must still name a configured option. Live defects fixed in the Application handlers (SPEC WM-V3-GAP-032). Evidence: `BoardItemTests`, `BoardItemMoveToGroupTests`, `BoardItemEventTests`, `BoardItemHierarchyTests`, `BoardItemFieldValueClearTests` (3), `WorkItemFieldValueIntegrationTests` (3, PostgreSQL, fresh request per write).

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
- [x] Field/Value Domain source READY_FOR_CERTIFICATION.

### Status
`READY_FOR_CERTIFICATION`. FieldType is not patchable (WM-V3-APP-FIELD-001); `AddOption` returns the option and `MoveOption` consumes an already-resolved key through the aggregate (WM-V3-APP-OPTION-001); `ReorderOptions` is maintenance-only; the BoardField-owned family is the only option authority. Evidence: `BoardFieldTests`, `BoardFieldDefaultValueTests`, `BoardFieldRenameTests`, `FieldOptionMoveTests`, `FieldValueValidatorContractTests`, `WorkP3ManifestArchitectureTests.RetiredWorkAuthorities_AreAbsent`.

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
- [x] Checklist Domain source READY_FOR_CERTIFICATION.

### Status
`READY_FOR_CERTIFICATION` with WM-V3-APP-CHK-001: aggregate-owned add/remove/rename/move/set-completion/toggle, `SetItemCompletion` no-op on the desired state, toggle a separate inversion. Due date and assignee child mutations are not part of the final API (removed from PATCH), so no Domain behavior is added for them. Evidence: `ChecklistChildMutationTests` (14).

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
- [x] Board protected source READY_FOR_CERTIFICATION.

### Status
`READY_FOR_CERTIFICATION`. `UpdateBoardCommand` maps exactly Title/Description/Background/Visibility, each to an owned Domain method (WMREQ010), with the required `ExpectedVersion` (WM-V3-CONC-001); Board reads return `Version` (WM-V3-CONC-002); create keeps the single default schema. Evidence: `WorkExpectedVersionIntegrationTests.UpdateBoard_MapsEveryAcceptedField_AndRejectsAStaleWriter`, `CreateBoardInWorkspacePipelineTests` (owner allowed, outsider forbidden through the real pipeline), `WorkPermissionActionMatrixArchitectureTests`.

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
- [x] Item ordering source READY_FOR_CERTIFICATION.

### Status
`READY_FOR_CERTIFICATION` for placement. `CreateBoardItemCommand` and `MoveBoardItemCommand` take `PreviousSiblingId?`/`NextSiblingId?` instead of the ignored `double Position`; both resolve placement through `WorkPlacementResolver` over the GroupItems scope (root, non-deleted items, `GroupItemSiblings`), and a move to the item's current place is a semantic no-op. The automation path uses the same `MoveBoardItemUseCase` with an append intent. Evidence: `WorkItemPlacementIntegrationTests` (5, PostgreSQL with the real lock). OpenAPI and the generated client are regenerated; the web adapter now omits the position (append) until WM-V3-FE-001 sends neighbor ids, and the mock backend still reads the old field. Still open in this area: `MoveBoardItemCommand` ExpectedVersion (WM-V3-CONC-001) and Duplicate (WM-V3-APP-ITEM-002).

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
- [x] Item mutation source READY_FOR_CERTIFICATION.

### Status
`READY_FOR_CERTIFICATION`. Done: Duplicate places the copy directly after its source through `WorkPlacementResolver.ResolveAfterAsync` under the GroupItems lock; `UpdateBoardItemCommand` no longer carries `DescriptionMd`/`Priority`/`Cover`; `UpdateBoardItemFieldValues` validates every field and field type before applying any value (SPEC WM-V3-GAP-029). Evidence: `WorkItemPlacementIntegrationTests` (duplicate and bulk cases; the bulk case fails on the previous handler). Closed later: single-value writes and Clear now load the item's values and the field's options (SPEC WM-V3-GAP-032), Clear stores the canonical JSON null for option fields, and Item `Version` is on reads (WM-V3-CONC-002).

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
- [x] Group source READY_FOR_CERTIFICATION.

### Status
`READY_FOR_CERTIFICATION` for placement. `CreateBoardGroup` takes neighbor ids instead of a client position (SPEC WM-V3-GAP-028); the new `MoveBoardGroupCommand` (`POST /api/v1/board-groups/{groupId}/move`) moves one group under the BoardGroups lock with the semantic no-op rule; `DuplicateBoardGroup` places the copy directly after its source and keeps the cloned items' relative keys. `ReorderBoardGroupsCommand` (full-list drag) was retired and deleted with WM-V3-APP-OPTION-001, with its endpoint; until WM-V3-FE-001 the web adapters apply a complete desired order as sequential append moves. Evidence: `WorkItemPlacementIntegrationTests` (group case). Open: Group `Version` on reads (WM-V3-CONC-002).

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
- [x] Field source READY_FOR_CERTIFICATION.

### Status
`READY_FOR_CERTIFICATION` except the version contract. Done: `BoardField.Rename` (trim, max 100, no-op, one version, `BoardFieldRenamedDomainEvent`, rejected on a deleted field); unknown or numeric field types fail closed in the validator and the handler (`FieldTypeNames`); create takes neighbor ids under the BoardFields lock and no longer forces `"{}"` settings (so the Status default applies); ordinary PATCH is `Name?`/`Settings?` with `FieldType` removed; the new `MoveBoardFieldCommand` (`POST /api/v1/boards/{boardId}/fields/{fieldId}/move`, `PermissionAction.UpdateField`) moves one field with the no-op rule; `ReorderBoardFieldsCommand` was retired and deleted with WM-V3-APP-OPTION-001 (route `POST /boards/{boardId}/fields/reorder` removed). Evidence: `BoardFieldRenameTests` (6), `FieldTypeNamesTests` (8), `WorkItemPlacementIntegrationTests` field cases (2). Open: required `ExpectedVersion` and BoardField `Version` on reads (WM-V3-CONC-001/002). Decision to review: field move uses `UpdateField` instead of the reorder's `ManageBoard`, narrowing per WG-DEBT-007.

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
- [x] Exactly one FieldOption authority and one normal move mechanism remain.

### Status
`READY_FOR_CERTIFICATION` except the version contract. Done: the legacy `Features/WorkManagement/FieldOptions/**` family and both full-list `ReorderFieldOptionsCommand`s are deleted with their route (`POST .../options/reorder`), so the BoardField-owned family is the only option authority; `AddFieldOptionCommand` takes neighbor ids instead of a raw `Position` string and resolves placement under the FieldOptions lock (`FieldOptionSiblings` reads the option set from storage under the lock); the new `MoveFieldOptionCommand` (`POST /api/v1/boards/{boardId}/fields/{fieldId}/options/{optionId}/move`, `PermissionAction.UpdateField`) moves one option through `BoardField.MoveOption` (one key rewritten, semantic no-op, one version, `FieldOptionMovedDomainEvent`); `BoardField.ReorderOptions` remains as the maintenance rebalance primitive with no user-facing caller. Live defect fixed (SPEC WM-V3-GAP-030): no option handler loaded `Options`, so update/remove from a fresh request always failed with OptionNotFound and the duplicate-name rule never saw persisted options. The same commit deletes the other pending RETIRE requests (`CreateBoardBySlug`, `ReorderBoardGroups`, `ReorderBoardFields`), which unblocks WM-V3-DATA-002 for the BoardGroups, BoardFields and FieldOptions scopes. Evidence: `FieldOptionMoveTests` (5), `WorkP3ManifestArchitectureTests.RetiredWorkAuthorities_AreAbsent`, `WorkItemPlacementIntegrationTests` option cases (2, PostgreSQL with the real lock; the fresh-request case fails on the previous handlers), `BoardFieldsEndpointTests` (retired routes unmapped, move routed). Open: required `ExpectedVersion` on option mutations (WM-V3-CONC-001).

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
- [x] Checklist source READY_FOR_CERTIFICATION.

### Status
`READY_FOR_CERTIFICATION` except the required version contract. Done: every ChecklistItem mutation loads the Checklist aggregate with its items (`ChecklistAggregateLoader`) and mutates through it — `AddItem` (returns the item), `RemoveItem`, `ToggleItem` (the `/toggle` inversion), `SetItemCompletion` (desired state, repeat is a no-op, never implemented with toggle), `RenameItem` (trim, max 200, no-op, `ChecklistItemRenamedDomainEvent`) and `MoveItem` (one key, no-op, `ChecklistItemMovedDomainEvent`); each versions the parent once. PATCH `/checklist-items/{id}` is `Title?`/`IsChecked?` (`DueDate`/`AssigneeId` removed); PATCH `/checklists/{id}` is `Title?` only. Create checklist / create item take neighbor ids and resolve placement under the ItemChecklists / ChecklistItems locks; the new `MoveChecklistCommand` (`POST /api/v1/checklists/{id}/move`) and `MoveChecklistItemCommand` (`POST /api/v1/checklist-items/{id}/move`) move one entry relative to its neighbors. `DeleteChecklist` is the aggregate's soft delete (`ChecklistDeletedDomainEvent`) instead of removing the checklist and its item rows. Checklist rename/move now raise `ChecklistRenamedDomainEvent` / `ChecklistMovedDomainEvent`. `ChecklistDto` carries the Checklist `Version` (read from `GetChecklists` and `GetBoardItem`), and `GetChecklists` loads items in one query and orders ordinally with an id tie-break. Live defects fixed (SPEC WM-V3-GAP-031). An architecture gate forbids `ChecklistItems.Add/Remove/...` anywhere in Application. Evidence: `ChecklistChildMutationTests` (14), checklist Application tests (rewritten to assert state/version/events), `WorkChecklistIntegrationTests` (4, PostgreSQL with the real lock, every request in a fresh context), `ChecklistItems_AreNeverAddedOrRemovedOutsideTheChecklistAggregate`. Open: required parent `ExpectedVersion` on child mutations (WM-V3-CONC-001); the web adapter no longer sends checklist `position` (it was already ignored) and adopts the move endpoints in WM-V3-FE-001.

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
- [x] Every manifest request has required version and one target mapping.

### Status
`READY_FOR_CERTIFICATION` for the producer. All 22 requests of the P3 versioned-request manifest declare `IExpectedVersionRequest` with a required non-nullable `long ExpectedVersion` constructor parameter (the nullable→0 sentinel is gone from UpdateBoard/UpdateBoardField/UpdateBoardItem) and a validator rule `MustBeAnAuthoritativeVersion` (> 0, HTTP 400 for a missing version instead of a guaranteed 412); `ExpectedVersionTargetMap` holds exactly one entry each. ChecklistItem mutations declare the `checklist-item` resource for authorization and bind to the owning Checklist through the new `TargetDefinition.OwnsResource` matcher (SPEC WM-V3-DEC-007). Transport: bodies carry `expectedVersion`; DELETE/remove-option/clear-value/toggle take `?expectedVersion=`. The automation move builds `MoveBoardItemCommand` only as an authorization descriptor and is not version-guarded. Evidence: `WorkP3ManifestArchitectureTests.EveryVersionedTarget_RequiresAPositiveExpectedVersion_AndHasExactlyOneTargetMapping` (verified to fail when one validator rule is removed), `ExpectedVersionArchitectureTests` inventory (+18), `WorkExpectedVersionIntegrationTests` (3, real handlers inside the real `EfRequestDataSession` on PostgreSQL: read version commits, stale version 412 with full rollback, stale no-op still 412, ChecklistItem bound to the Checklist version), `WorkMutationTransportRegressionTests.VersionedMutation_WithoutExpectedVersion_IsRejectedAsAClientError`, request-execution baseline (18 requests now `expectedVersion: true`). Breaking for first-party callers until WM-V3-FE-001: the web adapters do not send `expectedVersion`, so these mutations now return 400 at runtime; the OpenAPI schema does not mark the field required (no request contract in the repository declares requiredness), so the generated client does not flag the omission at compile time (WM-V3-API-001).

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
- [x] Every first-party versioned mutation can obtain its ExpectedVersion from an authoritative read.

### Status
`READY_FOR_CERTIFICATION`. `Version` (the aggregate version) is projected in `BoardDto`, `FullBoardDto`, `BoardSchemaDto`, `BoardGroupDto`, `BoardGroupSchemaDto`, `BoardFieldDto`, `BoardFieldSchemaDto`, `BoardItemDto`, `BoardItemSummaryDto` and `BoardItemSlimDto` (also on create/move/field-value mutation responses), and `ChecklistDto` carries the parent Checklist version (WM-V3-APP-CHK-001); ChecklistItem and FieldOption carry none, their parent is the concurrency authority. The synthetic title column of `GetFullBoard` reports 0 (not mutable). Evidence: the read version is the one `WorkExpectedVersionIntegrationTests` writes with; OpenAPI and the generated client regenerated.

## WM-V3-IDEMP-001 — Move idempotency-key ownership to mutation intent

**Phase:** Phase 2  
**SPEC:** WMREQ115–17,143  

### Source surfaces
- `frontend Work mutation hooks/commands`
- `item/group/field/checklist api adapters`
- `foundation api-client`

### Required implementation
- Backend-first (SPEC WM-V3-DEC-017): the server-side replay/mismatch proofs (IDEMP-INT-001, IDEMP-MISMATCH-001) are delivered with the backend units; the frontend key ownership below is delivered together with WM-V3-FE-001. Missing keys remain a live 400 defect (SPEC WM-V3-GAP-018) until then.
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

### Status
Server side `READY_FOR_CERTIFICATION`; frontend key ownership remains with WM-V3-FE-001 (backend-first, SPEC WM-V3-DEC-017). Evidence: `WorkIdempotencyIntegrationTests` (3, real pipeline with `IdempotencyBehavior` and the PostgreSQL store): same key + same payload has one effect and replays the first result; same key + different payload raises `IdempotencyPayloadMismatchException` with no second effect; a failed (stale-placement) attempt rolls back without completing the key, so the same-key retry executes. The exit box stays open until the web adapters own one key per canonical attempt.

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
- [x] Event source READY_FOR_CERTIFICATION.

### Status
`READY_FOR_CERTIFICATION`. Inventory: P3 integration contracts are the 18 mappings of `BoardEventMapper` (board created/renamed/archived/unarchived, item created v2/renamed/moved v2/archived v2/field-value-changed/member-assigned, field created/updated/deleted, label created/updated, checklist created, checklist-item toggled; views are P4); names, versions and schemas are pinned by `backend/contracts/events/notrelix.events.json` and its drift gate. Move facts: `board_item.moved` v2 now also carries `newPosition` (additive, nullable; schema regenerated) next to `oldGroupId`/`newGroupId`/`revision`, so the fact names the authoritative final scope, key and version. Group, field, option and checklist moves stay Domain-only facts (`*ReorderedDomainEvent`/`*MovedDomainEvent`) with no outward contract or realtime mapping; first-party clients refetch. Rebalance has no production entry point (WM-V3-ORDER-004) and no consumer caches opaque positions (the Analytics projection reads no position), so no maintenance invalidation fact is required yet. Outbox fate is unchanged: the item-move outbox and recovery runtime tests pass. Evidence: `WorkItemIntegrationEventMapperTests` (asserts `NewPosition`), `PublicEventContractArchitectureTests.CanonicalManifest_MatchesGeneratedSourceShape`, `WorkIntegrationEventPinningArchitectureTests`, `BoardItemMovedOutboxRuntimeTests`, `BoardItemMovedPlacementFailureRecoveryRuntimeTests`, Messaging/Automation integration groups (82).

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
- [x] Named consumer contracts READY_FOR_CERTIFICATION.

### Status
`READY_FOR_CERTIFICATION`. Automation: `IWorkActionPort.MoveItemAsync(itemId, targetGroupId, executionId, principal)` accepts semantic intent only (no key); the Work side places with an append intent under the GroupItems lock and surfaces ordering conflicts as `work.ordering.conflict` (WM-V3-ORDER-005). Analytics: the projection source adapter is producer-owned and reads no ordering key, so rebalance needs no invalidation. Collaboration: reads Work through its read boundary only. Evidence: `CrossContextPersistenceBoundaryTests`, `CrossContextCascadeArchitectureTests`, `CollaborationReadBoundaryArchitectureTests`, `AnalyticsProjectionSourceOwnershipArchitectureTests`, `PublicSemanticContractArchitectureTests`, `DbContextBoundaryTests` (101 tests), automation composition/chain integration tests.

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
- [x] Producer OpenAPI READY_FOR_CERTIFICATION.

### Status
`READY_FOR_CERTIFICATION`. The P3 producer contract is cut to V3: neighbor-relative placement with no raw key (create/move of items, groups, fields, options, checklists, checklist items); `expectedVersion` on all 22 versioned mutations (17 in the body, 5 as `?expectedVersion=`); PATCH bodies hold exactly their accepted fields, and Board/Item PATCH now bind dedicated `UpdateBoardRequest`/`UpdateBoardItemRequest` instead of Application commands whose route ids the endpoint silently overwrote; read and DTO-returning endpoints now declare their response types (`.Produces<T>()`), so `version` is part of the published contract (it was invisible before: those responses had no schema); create-board rejects an unknown or numeric visibility with 400 instead of a 500 from `Enum.Parse`; the public errors are exactly `work.ordering.stale-placement` / `work.ordering.conflict` (409), `common.precondition-failed` (412) and `idempotency_payload_mismatch`. Evidence: `WorkP3OpenApiShapeTests` (9, over the checked-in contract that CI regenerates and diffs), `ConflictProblemDetailsTests` (exact status and code per error), `WorkMutationTransportRegressionTests` (missing version 400, unknown visibility 400), `IdempotencyEndpointContractTests`; OpenAPI and the generated client regenerated, frontend typecheck green. Not expressed: OpenAPI requiredness (no request contract in the repository declares it).

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
- [x] Application framework coupling does not grow.

### Status
`READY_FOR_CERTIFICATION`. P3 added no DbSet, raw SQL or database facade to the Work port; ordering serialization is the `IWorkOrderingScopeLock` Application port implemented in Infrastructure, and commit-time conflict classification is the `IOrderingConflictClassifier` port. Evidence: `WorkP3ManifestArchitectureTests.WorkManagementPersistencePort_DoesNotGrow` (exact 32-DbSet baseline, no methods), `RawSqlArchitectureTests`, `DbContextBoundaryArchitectureTests`, `HandlerDataPortGateTests`.

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
- [x] One BoardSchema read authority remains.

### Status
`READY_FOR_CERTIFICATION` (verified, no source change). The only BoardSchema read authority is `GetBoardSchemaQuery` and its handler; no Infrastructure `BoardSchemaReadService` or unused read port exists in source.

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
- [x] Clean and supported upgrade paths pass deterministically.

### Status
`READY_FOR_CERTIFICATION`. Relational order: `20261010022529_WidenBoardDescription` (independent column, shipped first) → `20261010065641_CanonicalizeWorkOrdering` (preflight → position `text COLLATE "C"` → the six `ux_work_*` unique indexes; snapshot updated). Pre-production databases are reset first (SPEC WM-V3-DEC-006); constraint names are the six recorded in `WorkOrderingConstraints`; no row counts apply after a reset. The development seed (`InitDb`) writes distinct keys in every scope, so it satisfies the indexes. Evidence: clean path — every Integration test container migrates an empty database through the full chain; supported upgrade — `WorkOrderingPreflightIntegrationTests` (pre-cut-over schema, duplicate abort, clean upgrade with stable keys); `dotnet ef migrations has-pending-model-changes`: none.

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
- [x] Relational and RLS states are both explicitly verified.

### Status
`READY_FOR_CERTIFICATION`. The RLS pack is a separate channel from the EF migration (`DatabaseStartupExtensions`: migrate and apply-RLS are separate switches; `RlsPolicyApplier.ApplyAsync` runs the embedded scripts in `000_apply_order.sql` order and then fails closed through `VerifyEnforcedSchemasAsync` if any `work` table has RLS disabled or no policy). Script identity at this revision (SHA-256, first 16 hex): `000_apply_order` 01a2ee604349289b, `001_roles` b999fac5d02b6bf3, `002_context_helpers` d0ae44b70d72bd6d, `003_authz_access_helpers` 9f78e72759509a60, `004_policy_runtime` 6da0df38a5a0416a, `005_grants` f75896e762d43c01, `006_policies_identity` ada77768c95bc66c, `007_policies_platform` 059f8e7aa208be61, `008_policies_workspace_scoped_domain` cf7562b4e626b245, `009_policies_notifications_activity_search` 8997e57ac2d081aa, `010_policies_events_messaging_audit_ops` 1f522d25746ca568, `011_verification` f9181970934ca7b7. Evidence: `WorkChildTableRlsIntegrationTests` (migrations then the pack on a dedicated PostgreSQL; allowed and denied tenant reads/writes as `notrelix_app`; catalog verification; fail-closed on an unprotected table), `RlsRuntimeEnforcementTests`.

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
