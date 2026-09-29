---
document_id: WRK-TESTS-WORK-MANAGEMENT
document_type: workstream-tests
status: active
revision: final-audit-v3
owner: work-management-team
candidate_baseline:
  branch: develop
  sha: 35702d0fa9fb01ed68b0667bab500030d60bd028
supersedes: work-management.tests.v2.md
---

# TESTS — Work Management Transactional Core (Final-Audit V3)

## 1. Purpose

V3 restores the foundational semantic coverage that V2 accidentally deemphasized while retaining the audited correction tests. For WMREQ001–150, inventory/trace/CI tests do **not** count as semantic coverage.

## 2. Evidence rules
- Real PostgreSQL is mandatory for RLS, advisory locks, collation parity, unique constraints, migrations, transaction/outbox and real concurrency.
- Ordering lock proof must demonstrate lock acquisition before sibling read.
- ExpectedVersion tests do not substitute for same-gap ordering-scope concurrency tests.
- Read-side Version propagation is required evidence for every first-party versioned mutation.
- Full-list FieldOption reorder tests are maintenance/rebalance evidence only; normal drag is relative move.
- A green legacy test encoding retired behavior is replaced, not counted.
- API certification uses exact status/error contracts.
- Every WMREQ row below must reference at least one substantive family.

## 3. Substantive test family catalog

### API-ERROR-001 — Exact Work public error taxonomy

**Disposition:** `NEW`  
**Layer:** API  

**Proof:**
- validation/not-found/forbidden/precondition/stale-placement/ordering-conflict/idempotency-conflict distinct

**Target surfaces:**
- `WorkP3ErrorContractTests.cs`

### API-PLACEMENT-001 — Relative placement OpenAPI contract

**Disposition:** `NEW`  
**Layer:** API/OpenAPI  

**Proof:**
- previousSiblingId/nextSiblingId shapes
- both absent append
- no numeric/raw position

**Target surfaces:**
- `WorkOrderingContractTests.cs`

### API-TRANSPORT-001 — P3 endpoints remain transport-only

**Disposition:** `NEW`  
**Layer:** Architecture/API  

**Proof:**
- no business/persistence logic in endpoints
- explicit DTO mapping

**Target surfaces:**
- `WorkP3EndpointArchitectureTests.cs`

### API-UPDATES-001 — Exact Item/Field/Checklist update contracts

**Disposition:** `NEW`  
**Layer:** API/OpenAPI  

**Proof:**
- only implemented fields exposed
- Checklist desired state exact
- FieldType not ordinary patch

**Target surfaces:**
- `WorkP3UpdateContractTests.cs`

### API-VERSION-001 — ExpectedVersion/Version OpenAPI coherence

**Disposition:** `NEW`  
**Layer:** API/OpenAPI  

**Proof:**
- versioned writes require ExpectedVersion
- corresponding reads expose Version

**Target surfaces:**
- `WorkVersionApiContractTests.cs`

### APP-ARCH-001 — Application orchestration and EF no-growth

**Disposition:** `NEW`  
**Layer:** Architecture  

**Proof:**
- no new DbSet/provider/raw SQL coupling
- ordering lock is Application port with Infrastructure implementation
- no GenericRepository retrofit

**Target surfaces:**
- `WorkManagementEfBoundaryArchitectureTests.cs`

### AUTH-ARCH-001 — Canonical Governance authorization descriptors

**Disposition:** `NEW`  
**Layer:** Architecture  

**Proof:**
- P3 requests classified with correct principal/scope/action/resource
- no local role ladder

**Target surfaces:**
- `UseCaseSecurityClassificationTests.cs`
- `HandlerAuthorizationBypassArchitectureTests.cs`

### AUTH-AUT-001 — Automation target action reauthorization

**Disposition:** `NEW`  
**Layer:** Cross-context Integration  
**Runtime:** real PostgreSQL required  

**Proof:**
- explicit scope/actor/resource reconstructed
- current/captured authority policy respected
- no global bypass

**Target surfaces:**
- `AutomationWorkItemActionIntegrationTests.cs`

### AUTH-INT-001 — Protected Work allow/deny/cross-tenant/revoked matrix

**Disposition:** `NEW`  
**Layer:** Integration/Security  
**Runtime:** real PostgreSQL required  

**Proof:**
- authorized actor allowed
- outsider/cross-tenant/revoked denied
- denial leaves no mutation/outbox

**Target surfaces:**
- `WorkAuthorizationIntegrationTests.cs`

### BOARD-APP-001 — Board create/update exact contract

**Disposition:** `EXPAND`  
**Layer:** Application/API  

**Proof:**
- every accepted field maps to Domain behavior
- retry does not duplicate default schema

**Target surfaces:**
- `CreateBoardInWorkspaceTests.cs`
- `UpdateBoardTests.cs`
- `BoardContractTests.cs`

### BOARD-AUTH-001 — Board protected authorization

**Disposition:** `NEW`  
**Layer:** Integration/Security  
**Runtime:** real PostgreSQL required  

**Proof:**
- authorized create/update succeeds
- outsider/cross-tenant/revoked denied

**Target surfaces:**
- `WorkAuthorizationIntegrationTests.cs`

### BOARD-CONC-001 — Board optimistic concurrency

**Disposition:** `NEW`  
**Layer:** Integration  
**Runtime:** real PostgreSQL required  

**Proof:**
- read Version is returned
- stale ExpectedVersion fails precondition
- loser commits no side effects

**Target surfaces:**
- `BoardExpectedVersionConcurrencyTests.cs`

### BOARD-DOM-001 — Board identity, containment and lifecycle

**Disposition:** `KEEP/EXPAND`  
**Layer:** Domain  

**Proof:**
- identity survives rename/archive/restore
- Account/Workspace containment never mutates
- lifecycle no-op does not churn version/event

**Target surfaces:**
- `Notrelix.Domain.Tests/WorkManagement/Boards/BoardTests.cs`
- `BoardEventTests.cs`

### BOARD-DOM-002 — Board visibility/membership/default schema semantics

**Disposition:** `EXPAND`  
**Layer:** Domain/Application  

**Proof:**
- visibility remains a Work fact, not auth bypass
- resource-local membership does not replace Workspace membership
- default schema is deterministic

**Target surfaces:**
- `BoardTests.cs`
- `CreateBoardInWorkspaceTests.cs`

### BOARD-MAP-001 — Board persistence compatibility

**Disposition:** `NEW`  
**Layer:** Infrastructure/Integration  
**Runtime:** real PostgreSQL required  

**Proof:**
- Domain-valid description persists to 5000
- DB wider-than-Domain title remains safe

**Target surfaces:**
- `BoardMappingContractTests.cs`

### BOARD-SCHEMA-001 — Single BoardSchema read authority

**Disposition:** `NEW`  
**Layer:** Architecture/Application  

**Proof:**
- TODO skeleton removed or sole registered implementation behind Application port

**Target surfaces:**
- `BoardSchemaReadBoundaryArchitectureTests.cs`
- `GetBoardSchemaTests.cs`

### CERT-BLOCKER-001 — No material blocker hidden

**Disposition:** `NEW`  
**Layer:** Certification  

**Proof:**
- all V3 blockers closed or explicitly N/A with authority

**Target surfaces:**
- `certification blocker validator`

### CERT-SHA-001 — Exact candidate evidence identity

**Disposition:** `NEW`  
**Layer:** Certification  

**Proof:**
- source/tests/migrations/RLS/OpenAPI evidence all name same candidate SHA

**Target surfaces:**
- `certification evidence validator`

### CHK-API-001 — Checklist transport exactness

**Disposition:** `NEW`  
**Layer:** API  

**Proof:**
- Title?/IsChecked?/ExpectedVersion only
- no accepted field dropped

**Target surfaces:**
- `ChecklistItemContractTests.cs`

### CHK-CONC-001 — Checklist parent concurrency

**Disposition:** `NEW`  
**Layer:** Integration  
**Runtime:** real PostgreSQL required  

**Proof:**
- Checklist Version returned
- stale child mutation using parent version fails

**Target surfaces:**
- `ChecklistConcurrencyTests.cs`

### CHK-DOM-001 — Checklist aggregate owns child semantics

**Disposition:** `EXPAND`  
**Layer:** Domain/Application  

**Proof:**
- child create/remove/title-rename/set-completion through Checklist
- parent version changes once per real child mutation

**Target surfaces:**
- `ChecklistTests.cs`
- `ChecklistAggregateOwnershipTests.cs`

### CHK-ORDER-001 — Checklist/ChecklistItem locked relative ordering

**Disposition:** `NEW`  
**Layer:** Application/Integration  
**Runtime:** real PostgreSQL required  

**Proof:**
- ItemChecklists and ChecklistItems scopes lock before sibling read
- normal move changes one ordered entity

**Target surfaces:**
- `ChecklistOrderingTests.cs`

### CHK-RLS-001 — ChecklistItem parent-derived app RLS

**Disposition:** `NEW`  
**Layer:** Integration/Security  
**Runtime:** real PostgreSQL required  

**Proof:**
- same-tenant CRUD allowed
- cross-tenant CRUD denied
- null scope denied

**Target surfaces:**
- `WorkChildTableRlsIntegrationTests.cs`

### CHK-STATE-001 — Checklist desired-state vs toggle semantics

**Disposition:** `NEW`  
**Layer:** Domain/Application/API  

**Proof:**
- set true/false is idempotent desired state
- toggle is separate inversion command

**Target surfaces:**
- `ChecklistItemDesiredStateTests.cs`
- `ToggleChecklistItemTests.cs`

### CROSS-ARCH-001 — No private cross-context persistence

**Disposition:** `NEW`  
**Layer:** Architecture  

**Proof:**
- Automation/Analytics/Collaboration/Billing/Integrations do not access Work private persistence and vice versa

**Target surfaces:**
- `WorkCrossContextOwnershipArchitectureTests.cs`

### DISCOVERY-001 — Mandatory groups execute non-zero

**Disposition:** `NEW`  
**Layer:** CI  

**Proof:**
- full projects and focused P3 groups discover/execute >0

**Target surfaces:**
- `P3V3TestDiscoveryTests.cs`

### EVT-CONTRACT-001 — Published event contract versioning

**Disposition:** `NEW`  
**Layer:** Application/Architecture  

**Proof:**
- name/version/scope/entity/revision/PII explicit
- same version cannot silently drift

**Target surfaces:**
- `WorkItemIntegrationEventMapperTests.cs`
- `WorkEventContractArchitectureTests.cs`

### EVT-DOM-001 — Work Domain events reflect semantic changes only

**Disposition:** `KEEP/EXPAND`  
**Layer:** Domain  

**Proof:**
- real changes emit facts
- semantic no-op does not duplicate events

**Target surfaces:**
- `BoardEventTests.cs`
- `BoardItemEventTests.cs`
- `BoardFieldEventTests.cs`
- `ChecklistEventTests.cs`

### FE-CHK-001 — Checklist first-party V3 contract

**Disposition:** `NEW`  
**Layer:** Frontend  

**Proof:**
- Checklist Version + desired-state + relative placement + idempotency

**Target surfaces:**
- `checklist.api.test.ts`

### FE-FIELD-001 — Field/Option first-party V3 contract

**Disposition:** `NEW`  
**Layer:** Frontend  

**Proof:**
- neighbor IDs + BoardField Version
- no legacy/raw position

**Target surfaces:**
- `field.api.test.ts`

### FE-GEN-001 — Generated consumer surface matches producer

**Disposition:** `NEW`  
**Layer:** Frontend Contract  

**Proof:**
- OpenAPI regeneration/typecheck/enabled surface green
- no any/manual generated edit

**Target surfaces:**
- `enabled-consumer-surface tests`
- `frontend typecheck`

### FE-GROUP-001 — Group first-party V3 contract

**Disposition:** `NEW`  
**Layer:** Frontend  

**Proof:**
- neighbor IDs + Group Version + logical idempotency key

**Target surfaces:**
- `group.api.test.ts`

### FE-ITEM-001 — Item first-party V3 contract

**Disposition:** `NEW`  
**Layer:** Frontend  

**Proof:**
- neighbor IDs + Version + idempotency key
- reload/rebase on conflict

**Target surfaces:**
- `item.api.test.ts`
- `item mutation tests`

### FIELD-APP-001 — Field create/update exact contract

**Disposition:** `NEW`  
**Layer:** Application/API  

**Proof:**
- unknown type fails closed
- ordinary patch contains `Name?`, `Settings?`, required `ExpectedVersion`
- `BoardField.Rename` enforces trim/max-100/no-op/version/event
- `FieldType` is absent

**Target surfaces:**
- `CreateBoardFieldTests.cs`
- `UpdateBoardFieldTests.cs`
- `BoardFieldContractTests.cs`

### FIELD-DOM-001 — Field identity/type/settings/system invariants

**Disposition:** `KEEP/EXPAND`  
**Layer:** Domain  

**Proof:**
- ID survives rename
- settings type-valid
- system restrictions enforced

**Target surfaces:**
- `BoardFieldTests.cs`
- `FieldValueValidatorContractTests.cs`

### FIELD-DOM-002 — Field option/default compatibility

**Disposition:** `KEEP/EXPAND`  
**Layer:** Domain  

**Proof:**
- option ID stable
- duplicate names rejected
- default references protected
- remove option compatibility explicit

**Target surfaces:**
- `BoardFieldDefaultValueTests.cs`
- `FieldOptionsReorderTests.cs`

### FIELD-MIG-001 — Field type change is not ordinary patch

**Disposition:** `NEW`  
**Layer:** API/Migration  
**Runtime:** real PostgreSQL required  

**Proof:**
- ordinary endpoint cannot type-migrate
- future type migration requires explicit compatibility path

**Target surfaces:**
- `BoardFieldContractTests.cs`
- `WorkManagementFieldCompatibilityTests.cs`

### HANDOFF-P4A-001 — P4A dependency gate

**Disposition:** `NEW`  
**Layer:** Certification/Handoff  

**Proof:**
- P4A only opens when producer thresholds are met

**Target surfaces:**
- `certification handoff validator`

### IDEMP-FE-001 — Logical-intent idempotency ownership

**Disposition:** `NEW`  
**Layer:** Frontend  

**Proof:**
- one canonical request attempt creates one key above adapter
- adapter receives key instead of minting it

**Target surfaces:**
- `WorkMutationIdempotencyIntentTests.ts`

### IDEMP-INT-001 — Same-key replay semantics

**Disposition:** `NEW`  
**Layer:** Integration  
**Runtime:** real PostgreSQL required  

**Proof:**
- same key same payload one effect/replay
- rollback does not complete identity

**Target surfaces:**
- `WorkIdempotencyIntegrationTests.cs`

### IDEMP-MISMATCH-001 — Same-key payload mismatch

**Disposition:** `NEW`  
**Layer:** Integration  
**Runtime:** real PostgreSQL required  

**Proof:**
- different payload conflicts and commits no second effect

**Target surfaces:**
- `WorkIdempotencyIntegrationTests.cs`

### IDEMP-RETRY-001 — Transport retries reuse same key

**Disposition:** `EXPAND`  
**Layer:** Frontend Foundation  

**Proof:**
- auth/CSRF/network retry carries identical key

**Target surfaces:**
- `api-client.unit.test.ts`

### ITEM-APP-001 — Item create/move producer semantics

**Disposition:** `NEW`  
**Layer:** Application  

**Proof:**
- create and move use locked relative placement
- HTTP and public Work action share one use case

**Target surfaces:**
- `CreateBoardItemRelativePlacementTests.cs`
- `MoveBoardItemRelativePlacementTests.cs`
- `WorkItemActionsTests.cs`

### ITEM-APP-002 — Item update/value semantics

**Disposition:** `NEW`  
**Layer:** Application  
**Runtime:** real PostgreSQL required  

**Proof:**
- no phantom update fields
- single value validates field scope/type
- bulk values all-or-nothing
- clear uses canonical null

**Target surfaces:**
- `UpdateBoardItemTests.cs`
- `UpdateBoardItemFieldValueTests.cs`
- `BulkFieldValueAtomicityIntegrationTests.cs`
- `ClearFieldValueTests.cs`

### ITEM-CONC-001 — Item stale-write semantics

**Disposition:** `NEW`  
**Layer:** Integration  
**Runtime:** real PostgreSQL required  

**Proof:**
- Version returned by read
- stale update/move fails before durable effect

**Target surfaces:**
- `BoardItemExpectedVersionConcurrencyTests.cs`

### ITEM-DOM-001 — Item identity/scope/hierarchy

**Disposition:** `KEEP/EXPAND`  
**Layer:** Domain  

**Proof:**
- stable ID distinct from display key
- Group/Board/Workspace scope consistent
- hierarchy cannot cross Board or cycle

**Target surfaces:**
- `BoardItemIdentityTests.cs`
- `BoardItemMoveToGroupTests.cs`
- `BoardItemHierarchyTests.cs`

### ITEM-DOM-002 — Item lifecycle/member/label semantics

**Disposition:** `KEEP/EXPAND`  
**Layer:** Domain  

**Proof:**
- complete/reopen/archive/delete/restore deterministic
- assignment uses stable actor identity semantics
- labels remain Work metadata

**Target surfaces:**
- `BoardItemArchivedTests.cs`
- `BoardItemEventTests.cs`
- `BoardItemMemberTests.cs`

### ITEM-IDEMP-001 — Item create idempotency

**Disposition:** `NEW`  
**Layer:** Integration  
**Runtime:** real PostgreSQL required  

**Proof:**
- same logical create produces one Item
- payload mismatch conflicts

**Target surfaces:**
- `CreateBoardItemIdempotencyIntegrationTests.cs`

### MAP-VALID-001 — Persistence accepts Domain-valid boundaries

**Disposition:** `NEW`  
**Layer:** Integration  
**Runtime:** real PostgreSQL required  

**Proof:**
- Board description 5000 persists
- validator limits do not exceed Domain

**Target surfaces:**
- `BoardMappingContractTests.cs`
- `WorkValidatorDomainParityTests.cs`

### MIG-CLEAN-001 — Clean relational migration

**Disposition:** `NEW`  
**Layer:** Migration  
**Runtime:** real PostgreSQL required  

**Proof:**
- clean database reaches V3 schema/collation/index state

**Target surfaces:**
- `WorkP3V3MigrationTests.cs`

### MIG-HISTORY-001 — Append-only migration discipline

**Disposition:** `NEW`  
**Layer:** Architecture/Migration  

**Proof:**
- SchemaBaseline/history unchanged
- V3 fixes are forward migrations

**Target surfaces:**
- `WorkMigrationDisciplineTests.cs`

### MIG-PENDING-001 — No pending model changes

**Disposition:** `NEW`  
**Layer:** Migration  
**Runtime:** real PostgreSQL required  

**Proof:**
- final EF model matches migration snapshot

**Target surfaces:**
- `PendingModelChangesTests.cs`

### MIG-UPGRADE-001 — Supported upgrade preserves IDs/logical order

**Disposition:** `NEW`  
**Layer:** Migration  
**Runtime:** real PostgreSQL required  

**Proof:**
- pre-V3 fixture upgrades
- stable IDs unchanged
- logical order unchanged

**Target surfaces:**
- `WorkP3V3MigrationTests.cs`

### OBS-001 — Failure and ordering-health telemetry

**Disposition:** `NEW`  
**Layer:** Observability Integration  
**Runtime:** real PostgreSQL required  

**Proof:**
- auth/precondition/stale-placement/order conflict/RLS/idempotency/outbox distinguishable
- max key length/rebalance metrics visible

**Target surfaces:**
- `WorkFailureTelemetryTests.cs`

### OPTION-ARCH-001 — Single FieldOption command authority

**Disposition:** `NEW`  
**Layer:** Architecture  

**Proof:**
- legacy Features.WorkManagement.FieldOptions has no active IRequest handlers
- normal UI option drag cannot call full-list reorder

**Target surfaces:**
- `FieldOptionAuthorityArchitectureTests.cs`

### ORDER-CONC-001 — Same-gap different-aggregate concurrency

**Disposition:** `NEW`  
**Layer:** Integration/Concurrency  
**Runtime:** real PostgreSQL required  

**Proof:**
- two entities cannot persist same position
- scope lock serializes compliant writers
- unique index catches legacy/unlocked collision

**Target surfaces:**
- `ConcurrentWorkOrderingTests.cs`

### ORDER-CONFLICT-001 — Named unique violation maps to stable 409

**Disposition:** `NEW`  
**Layer:** Integration/API  
**Runtime:** real PostgreSQL required  

**Proof:**
- only named ordering constraints map to work.ordering.conflict
- unrelated unique errors are not misclassified

**Target surfaces:**
- `WorkOrderingConflictContractTests.cs`

### ORDER-DB-001 — Database collation equals Domain ordinal order

**Disposition:** `NEW`  
**Layer:** Integration  
**Runtime:** real PostgreSQL required  

**Proof:**
- PostgreSQL ORDER BY position over canonical base62 corpus equals StringComparer.Ordinal order
- indexes use same collation

**Target surfaces:**
- `WorkOrderingCollationIntegrationTests.cs`

### ORDER-DB-002 — Canonical position storage representation

**Disposition:** `NEW`  
**Layer:** Integration/Migration  
**Runtime:** real PostgreSQL required  

**Proof:**
- position columns are text COLLATE C/equivalent
- old varchar(50) limit removed

**Target surfaces:**
- `WorkOrderingSchemaContractTests.cs`

### ORDER-DB-003 — Unique active ordering scopes

**Disposition:** `NEW`  
**Layer:** Integration  
**Runtime:** real PostgreSQL required  

**Proof:**
- six named unique ordering constraints exist
- soft-delete filters behave correctly

**Target surfaces:**
- `WorkOrderingUniqueIndexIntegrationTests.cs`

### ORDER-DOM-001 — FractionalIndex grammar and ordinal generation

**Disposition:** `KEEP`  
**Layer:** Domain  

**Proof:**
- generated key strictly between bounds
- canonical grammar/property tests remain green

**Target surfaces:**
- `FractionalIndexGeneratorTests.cs`
- `FractionalIndexPropertyTests.cs`

### ORDER-DUP-001 — Duplicate Item/Group ordering

**Disposition:** `NEW`  
**Layer:** Application  

**Proof:**
- Duplicate Item uses canonical placement
- Duplicate Group uses canonical Group key
- cloned Items preserve relative keys in new empty Group

**Target surfaces:**
- `DuplicateBoardItemTests.cs`
- `DuplicateBoardGroupTests.cs`

### ORDER-LOCK-001 — PostgreSQL ordering-scope transaction lock

**Disposition:** `NEW`  
**Layer:** Integration  
**Runtime:** real PostgreSQL required  

**Proof:**
- same scope serializes before sibling read
- different scopes can proceed independently
- lock uses current request transaction

**Target surfaces:**
- `WorkOrderingScopeLockIntegrationTests.cs`

### ORDER-NORM-001 — Brownfield normalization preserves deterministic order

**Disposition:** `NEW`  
**Layer:** Migration  
**Runtime:** real PostgreSQL required  

**Proof:**
- affected scopes normalized using stable identity tie-break
- clean scopes untouched
- ambiguous unrecoverable scope blocks

**Target surfaces:**
- `WorkOrderingNormalizationMigrationTests.cs`

### ORDER-PLACE-001 — Canonical neighbor-relative placement semantics

**Disposition:** `NEW`  
**Layer:** Application  

**Proof:**
- both absent append
- previous-only current-last
- next-only current-first
- both currently adjacent
- moved entity excluded before adjacency

**Target surfaces:**
- `WorkPlacementResolverTests.cs`

### ORDER-PREFLIGHT-001 — Brownfield ordering preflight

**Disposition:** `NEW`  
**Layer:** Migration  
**Runtime:** real PostgreSQL required  

**Proof:**
- invalid/duplicate/null/max-length keys reported by scope
- clean and ambiguous scopes distinguished

**Target surfaces:**
- `WorkOrderingMigrationPreflightTests.cs`

### ORDER-REBAL-001 — Explicit rebalance preserves logical order

**Disposition:** `NEW`  
**Layer:** Integration  
**Runtime:** real PostgreSQL required  

**Proof:**
- scope lock acquired
- identity order exactly preserved
- canonical keys regenerated
- one maintenance invalidation/rebuild signal if required

**Target surfaces:**
- `WorkOrderingRebalanceTests.cs`

### ORDER-STALE-001 — Stale adjacency fails closed

**Disposition:** `NEW`  
**Layer:** Application/API  

**Proof:**
- non-adjacent previous/next returns work.ordering.stale-placement
- no mutation occurs

**Target surfaces:**
- `WorkPlacementResolverTests.cs`
- `WorkOrderingErrorContractTests.cs`

### OUTBOX-001 — Transactional outbox fate

**Disposition:** `KEEP/EXPAND`  
**Layer:** Integration  
**Runtime:** real PostgreSQL required  

**Proof:**
- commit => source+outbox
- rollback => neither

**Target surfaces:**
- `BoardItemMovedOutboxRuntimeTests.cs`

### OUTBOX-RECOVERY-001 — Downstream failure isolation/recovery

**Disposition:** `KEEP/EXPAND`  
**Layer:** Integration/Reliability  
**Runtime:** real PostgreSQL required  

**Proof:**
- consumer failure does not rollback Work
- retry does not duplicate source mutation

**Target surfaces:**
- `BoardItemMovedPlacementFailureRecoveryRuntimeTests.cs`

### PERF-AUTH-001 — Authorization facts avoid N+1

**Disposition:** `NEW`  
**Layer:** Performance Integration  
**Runtime:** real PostgreSQL required  

**Proof:**
- facts lookup not per returned Item

**Target surfaces:**
- `WorkAuthorizationQueryShapeTests.cs`

### PERF-BOARD-001 — Large Board query bounds

**Disposition:** `NEW`  
**Layer:** Performance Integration  
**Runtime:** real PostgreSQL required  

**Proof:**
- pagination bounded
- normal drag updates one row
- query budget measured

**Target surfaces:**
- `WorkBoardQueryPerformanceTests.cs`

### RLS-CHK-001 — ChecklistItem parent-derived RLS

**Disposition:** `NEW`  
**Layer:** Integration/Security  
**Runtime:** real PostgreSQL required  

**Proof:**
- scope derives through Checklist

**Target surfaces:**
- `WorkChildTableRlsIntegrationTests.cs`

### RLS-DEPLOY-001 — RLS policy pack deployment/verification

**Disposition:** `NEW`  
**Layer:** Integration/Deployment  
**Runtime:** real PostgreSQL required  

**Proof:**
- EF migration alone insufficient
- RlsPolicyApplier applies exact scripts
- catalog verification detects missing policy

**Target surfaces:**
- `WorkRlsDeploymentTests.cs`

### RLS-DIRECT-001 — Direct-scope Work app RLS

**Disposition:** `NEW`  
**Layer:** Integration/Security  
**Runtime:** real PostgreSQL required  

**Proof:**
- Board/Item/Field/Group/Checklist same-tenant allowed and cross-tenant denied

**Target surfaces:**
- `WorkCoreRlsIntegrationTests.cs`

### RLS-OPTION-001 — FieldOption parent-derived RLS

**Disposition:** `NEW`  
**Layer:** Integration/Security  
**Runtime:** real PostgreSQL required  

**Proof:**
- scope derives through BoardField

**Target surfaces:**
- `WorkChildTableRlsIntegrationTests.cs`

### RLS-SESSION-001 — RLS pooled session hygiene

**Disposition:** `NEW`  
**Layer:** Integration/Security  
**Runtime:** real PostgreSQL required  

**Proof:**
- reused connection does not inherit prior Account/Workspace context

**Target surfaces:**
- `RlsRuntimeEnforcementTests.cs`

### RLS-VALUE-001 — BoardItemValue dual-parent RLS

**Disposition:** `NEW`  
**Layer:** Integration/Security  
**Runtime:** real PostgreSQL required  

**Proof:**
- Item and Field current tenant and same Board required

**Target surfaces:**
- `WorkChildTableRlsIntegrationTests.cs`

### RLS-WORKER-001 — Worker scope boundary remains Platform-owned

**Disposition:** `NEW`  
**Layer:** Architecture  

**Proof:**
- P3 does not certify p_worker_all=true as tenant safety
- tenant-scoped worker path requires Platform evidence

**Target surfaces:**
- `WorkWorkerPolicyBoundaryTests.cs`

### RT-001 — Realtime post-commit non-authoritative handoff

**Disposition:** `NEW`  
**Layer:** Integration  
**Runtime:** real PostgreSQL required  

**Proof:**
- only committed state notified
- stable identity/revision allows invalidate/reconcile
- Platform owns reconnect/gap recovery

**Target surfaces:**
- `WorkRealtimePostCommitIntegrationTests.cs`

### SEC-JSON-001 — Flexible JSON safety

**Disposition:** `NEW`  
**Layer:** Security  

**Proof:**
- size/depth/shape bounded
- malformed unsupported settings/value rejected
- raw content not logged

**Target surfaces:**
- `WorkFlexiblePayloadSecurityTests.cs`

### SEC-QUERY-001 — Query authorization isolation

**Disposition:** `NEW`  
**Layer:** Integration/Security  
**Runtime:** real PostgreSQL required  

**Proof:**
- list/search cannot leak cross-tenant/resource state

**Target surfaces:**
- `WorkQueryAuthorizationIntegrationTests.cs`

### STUB-ARCH-001 — Stub consumers are not capability evidence

**Disposition:** `NEW`  
**Layer:** Architecture  

**Proof:**
- logging-only consumers are marked stub/retired and excluded from readiness

**Target surfaces:**
- `WorkEventConsumerMaturityArchitectureTests.cs`

### VALUE-CONC-001 — Field schema/value race safety

**Disposition:** `NEW`  
**Layer:** Integration/Concurrency  
**Runtime:** real PostgreSQL required  

**Proof:**
- schema change vs value write cannot commit invalid combination

**Target surfaces:**
- `FieldSchemaValueConcurrencyTests.cs`

### VALUE-DOM-001 — FieldValue canonical shape/no-op/derived rules

**Disposition:** `EXPAND`  
**Layer:** Domain  

**Proof:**
- shape follows type/settings
- semantic equal write no-op
- computed fields non-writable
- clear/null canonical

**Target surfaces:**
- `FieldValueValidatorContractTests.cs`
- `BoardItemFieldValueNoOpTests.cs`
- `ClearFieldValueTests.cs`

### VER-ARCH-001 — P3 versioned-request manifest completeness

**Disposition:** `NEW`  
**Layer:** Architecture  

**Proof:**
- every manifest request has required non-null ExpectedVersion
- one target mapping
- out-of-scope Forms/Views/Approvals excluded

**Target surfaces:**
- `WorkExpectedVersionContractArchitectureTests.cs`

### VER-READ-001 — Read-side Version propagation

**Disposition:** `NEW`  
**Layer:** Application/API  

**Proof:**
- Board/Item/Field/Group/Checklist mutation-facing reads project owner Version

**Target surfaces:**
- `WorkReadVersionProjectionTests.cs`
- `WorkVersionApiContractTests.cs`

### VER-STALE-001 — Representative stale-write failures

**Disposition:** `NEW`  
**Layer:** Integration/Concurrency  
**Runtime:** real PostgreSQL required  

**Proof:**
- Board/Item/Field/Checklist stale version returns precondition failure
- no durable loser effect

**Target surfaces:**
- `WorkExpectedVersionConcurrencyIntegrationTests.cs`

### X-AR-001 — Analytics snapshot/rebuild remains Work-owned

**Disposition:** `EXPAND`  
**Layer:** Cross-context  
**Runtime:** real PostgreSQL required  

**Proof:**
- projection source private-read ownership correct
- rebuild converges
- rebalance invalidation/rebuild handled

**Target surfaces:**
- `WorkspacePlacementProjectionIntegrationTests.cs`

### X-AUT-001 — Automation consumes Work producer action/event

**Disposition:** `NEW`  
**Layer:** Cross-context  
**Runtime:** real PostgreSQL required  

**Proof:**
- no private persistence
- semantic placement intent only
- reauthorization/idempotency preserved

**Target surfaces:**
- `AutomationWorkItemActionIntegrationTests.cs`

### X-BILL-001 — Billing boundary is neutral entitlement facts

**Disposition:** `NEW`  
**Layer:** Architecture  

**Proof:**
- no plan-name logic/private Billing persistence in Work

**Target surfaces:**
- `WorkBillingBoundaryArchitectureTests.cs`

### X-COLLAB-001 — Collaboration target/read boundary

**Disposition:** `KEEP/EXPAND`  
**Layer:** Cross-context  

**Proof:**
- no private persistence ownership transfer
- target identity stable

**Target surfaces:**
- `WorkManagementCollaborationReadAdapterTests.cs`

### X-INT-001 — Integrations do not own Work persistence

**Disposition:** `NEW`  
**Layer:** Architecture  

**Proof:**
- integration adapters use public Work contracts/events only

**Target surfaces:**
- `WorkIntegrationBoundaryArchitectureTests.cs`

## 4. Requirement → substantive evidence matrix

| Requirement | Substantive test family/families |
|---|---|
| `WMREQ001` | `BOARD-DOM-001`, `BOARD-APP-001` |
| `WMREQ002` | `BOARD-DOM-001`, `BOARD-APP-001` |
| `WMREQ003` | `BOARD-DOM-001`, `BOARD-APP-001` |
| `WMREQ004` | `BOARD-DOM-001`, `BOARD-APP-001` |
| `WMREQ005` | `BOARD-DOM-001`, `BOARD-APP-001`, `BOARD-CONC-001` |
| `WMREQ006` | `BOARD-DOM-001`, `BOARD-APP-001`, `BOARD-AUTH-001` |
| `WMREQ007` | `BOARD-DOM-001`, `BOARD-APP-001`, `BOARD-AUTH-001` |
| `WMREQ008` | `BOARD-DOM-001`, `BOARD-APP-001`, `BOARD-DOM-002` |
| `WMREQ009` | `BOARD-DOM-001`, `BOARD-APP-001`, `BOARD-DOM-002` |
| `WMREQ010` | `BOARD-DOM-001`, `BOARD-APP-001` |
| `WMREQ011` | `ITEM-DOM-001`, `ITEM-APP-002` |
| `WMREQ012` | `ITEM-DOM-001`, `ITEM-APP-002` |
| `WMREQ013` | `ITEM-DOM-001`, `ITEM-APP-002` |
| `WMREQ014` | `ITEM-DOM-001`, `ITEM-APP-002`, `ITEM-DOM-002` |
| `WMREQ015` | `ITEM-DOM-001`, `ITEM-APP-002` |
| `WMREQ016` | `ITEM-DOM-001`, `ITEM-APP-002`, `ITEM-IDEMP-001` |
| `WMREQ017` | `ITEM-DOM-001`, `ITEM-APP-002`, `ITEM-APP-001`, `ORDER-LOCK-001` |
| `WMREQ018` | `ITEM-DOM-001`, `ITEM-APP-002`, `ITEM-APP-001`, `ORDER-LOCK-001` |
| `WMREQ019` | `ITEM-DOM-001`, `ITEM-APP-002`, `ITEM-APP-001`, `X-AUT-001` |
| `WMREQ020` | `ITEM-DOM-001`, `ITEM-APP-002`, `API-UPDATES-001` |
| `WMREQ021` | `ITEM-DOM-001`, `ITEM-APP-002`, `ITEM-DOM-002` |
| `WMREQ022` | `ITEM-DOM-001`, `ITEM-APP-002`, `ITEM-DOM-002` |
| `WMREQ023` | `ITEM-DOM-001`, `ITEM-APP-002`, `ITEM-DOM-002` |
| `WMREQ024` | `ITEM-DOM-001`, `ITEM-APP-002` |
| `WMREQ025` | `ITEM-DOM-001`, `ITEM-APP-002` |
| `WMREQ026` | `FIELD-DOM-001`, `VALUE-DOM-001` |
| `WMREQ027` | `FIELD-DOM-001`, `VALUE-DOM-001`, `FIELD-APP-001` |
| `WMREQ028` | `FIELD-DOM-001`, `VALUE-DOM-001` |
| `WMREQ029` | `FIELD-DOM-001`, `VALUE-DOM-001` |
| `WMREQ030` | `FIELD-DOM-001`, `VALUE-DOM-001`, `FIELD-APP-001` |
| `WMREQ031` | `FIELD-DOM-001`, `VALUE-DOM-001`, `FIELD-MIG-001` |
| `WMREQ032` | `FIELD-DOM-001`, `VALUE-DOM-001`, `FIELD-MIG-001` |
| `WMREQ033` | `FIELD-DOM-001`, `VALUE-DOM-001`, `FIELD-DOM-002` |
| `WMREQ034` | `FIELD-DOM-001`, `VALUE-DOM-001`, `FIELD-DOM-002` |
| `WMREQ035` | `FIELD-DOM-001`, `VALUE-DOM-001`, `FIELD-DOM-002` |
| `WMREQ036` | `FIELD-DOM-001`, `VALUE-DOM-001` |
| `WMREQ037` | `FIELD-DOM-001`, `VALUE-DOM-001` |
| `WMREQ038` | `FIELD-DOM-001`, `VALUE-DOM-001` |
| `WMREQ039` | `FIELD-DOM-001`, `VALUE-DOM-001` |
| `WMREQ040` | `FIELD-DOM-001`, `VALUE-DOM-001`, `VALUE-CONC-001` |
| `WMREQ041` | `ORDER-PLACE-001`, `ORDER-DOM-001` |
| `WMREQ042` | `ORDER-PLACE-001`, `ORDER-DOM-001`, `ORDER-LOCK-001` |
| `WMREQ043` | `ORDER-PLACE-001`, `ORDER-DOM-001`, `ORDER-STALE-001` |
| `WMREQ044` | `ORDER-PLACE-001`, `ORDER-DOM-001` |
| `WMREQ045` | `ORDER-PLACE-001`, `ORDER-DOM-001` |
| `WMREQ046` | `ORDER-PLACE-001`, `ORDER-DOM-001`, `ORDER-DB-001` |
| `WMREQ047` | `ORDER-PLACE-001`, `ORDER-DOM-001`, `ORDER-DB-002` |
| `WMREQ048` | `ORDER-PLACE-001`, `ORDER-DOM-001`, `ORDER-DUP-001` |
| `WMREQ049` | `ORDER-PLACE-001`, `ORDER-DOM-001`, `ORDER-DUP-001` |
| `WMREQ050` | `ORDER-PLACE-001`, `ORDER-DOM-001`, `ORDER-DB-003`, `ORDER-CONC-001` |
| `WMREQ051` | `ORDER-PLACE-001`, `ORDER-DOM-001`, `ORDER-LOCK-001` |
| `WMREQ052` | `ORDER-PLACE-001`, `ORDER-DOM-001`, `ORDER-CONFLICT-001` |
| `WMREQ053` | `ORDER-PLACE-001`, `ORDER-DOM-001`, `ORDER-REBAL-001` |
| `WMREQ054` | `ORDER-PLACE-001`, `ORDER-DOM-001`, `ORDER-REBAL-001` |
| `WMREQ055` | `ORDER-PLACE-001`, `ORDER-DOM-001`, `ORDER-PREFLIGHT-001`, `ORDER-NORM-001` |
| `WMREQ056` | `CHK-DOM-001`, `CHK-API-001` |
| `WMREQ057` | `CHK-DOM-001`, `CHK-API-001` |
| `WMREQ058` | `CHK-DOM-001`, `CHK-API-001`, `CHK-ORDER-001` |
| `WMREQ059` | `CHK-DOM-001`, `CHK-API-001`, `CHK-ORDER-001` |
| `WMREQ060` | `CHK-DOM-001`, `CHK-API-001` |
| `WMREQ061` | `CHK-DOM-001`, `CHK-API-001`, `CHK-STATE-001` |
| `WMREQ062` | `CHK-DOM-001`, `CHK-API-001`, `CHK-STATE-001` |
| `WMREQ063` | `CHK-DOM-001`, `CHK-API-001` |
| `WMREQ064` | `CHK-DOM-001`, `CHK-API-001` |
| `WMREQ065` | `CHK-DOM-001`, `CHK-API-001` |
| `WMREQ066` | `CHK-DOM-001`, `CHK-API-001`, `CHK-CONC-001` |
| `WMREQ067` | `CHK-DOM-001`, `CHK-API-001`, `CHK-RLS-001` |
| `WMREQ068` | `CHK-DOM-001`, `CHK-API-001` |
| `WMREQ069` | `APP-ARCH-001` |
| `WMREQ070` | `APP-ARCH-001` |
| `WMREQ071` | `APP-ARCH-001` |
| `WMREQ072` | `APP-ARCH-001`, `BOARD-SCHEMA-001` |
| `WMREQ073` | `APP-ARCH-001`, `OPTION-ARCH-001` |
| `WMREQ074` | `APP-ARCH-001`, `OPTION-ARCH-001` |
| `WMREQ075` | `APP-ARCH-001`, `OPTION-ARCH-001`, `ORDER-LOCK-001` |
| `WMREQ076` | `APP-ARCH-001`, `OPTION-ARCH-001` |
| `WMREQ077` | `APP-ARCH-001`, `STUB-ARCH-001` |
| `WMREQ078` | `APP-ARCH-001` |
| `WMREQ079` | `APP-ARCH-001`, `CROSS-ARCH-001` |
| `WMREQ080` | `RLS-DIRECT-001`, `RLS-OPTION-001`, `RLS-VALUE-001`, `RLS-CHK-001` |
| `WMREQ081` | `RLS-DIRECT-001` |
| `WMREQ082` | `RLS-DIRECT-001`, `RLS-OPTION-001` |
| `WMREQ083` | `RLS-DIRECT-001`, `RLS-VALUE-001` |
| `WMREQ084` | `RLS-DIRECT-001`, `RLS-CHK-001` |
| `WMREQ085` | `RLS-DIRECT-001`, `RLS-OPTION-001`, `RLS-VALUE-001`, `RLS-CHK-001` |
| `WMREQ086` | `RLS-DIRECT-001`, `RLS-DEPLOY-001` |
| `WMREQ087` | `RLS-DIRECT-001`, `MIG-HISTORY-001` |
| `WMREQ088` | `RLS-DIRECT-001` |
| `WMREQ089` | `RLS-DIRECT-001`, `RLS-WORKER-001` |
| `WMREQ090` | `RLS-DIRECT-001`, `RLS-WORKER-001` |
| `WMREQ091` | `RLS-DIRECT-001`, `RLS-SESSION-001` |
| `WMREQ092` | `RLS-DIRECT-001` |
| `WMREQ093` | `RLS-DIRECT-001`, `RLS-DEPLOY-001` |
| `WMREQ094` | `MIG-CLEAN-001`, `MIG-UPGRADE-001`, `MAP-VALID-001` |
| `WMREQ095` | `MIG-CLEAN-001`, `MIG-UPGRADE-001`, `MAP-VALID-001` |
| `WMREQ096` | `MIG-CLEAN-001`, `MIG-UPGRADE-001`, `MAP-VALID-001` |
| `WMREQ097` | `MIG-CLEAN-001`, `MIG-UPGRADE-001`, `MAP-VALID-001` |
| `WMREQ098` | `MIG-CLEAN-001`, `MIG-UPGRADE-001`, `ORDER-DB-003` |
| `WMREQ099` | `MIG-CLEAN-001`, `MIG-UPGRADE-001`, `ORDER-DB-001`, `ORDER-DB-002` |
| `WMREQ100` | `MIG-CLEAN-001`, `MIG-UPGRADE-001`, `ORDER-PREFLIGHT-001`, `ORDER-NORM-001` |
| `WMREQ101` | `MIG-CLEAN-001`, `MIG-UPGRADE-001`, `RLS-DEPLOY-001` |
| `WMREQ102` | `MIG-CLEAN-001`, `MIG-UPGRADE-001`, `ORDER-NORM-001` |
| `WMREQ103` | `MIG-CLEAN-001`, `MIG-UPGRADE-001`, `MIG-HISTORY-001` |
| `WMREQ104` | `MIG-CLEAN-001`, `MIG-UPGRADE-001`, `MIG-PENDING-001`, `RLS-DEPLOY-001` |
| `WMREQ105` | `AUTH-ARCH-001`, `AUTH-INT-001` |
| `WMREQ106` | `AUTH-ARCH-001`, `AUTH-INT-001` |
| `WMREQ107` | `AUTH-ARCH-001`, `AUTH-INT-001` |
| `WMREQ108` | `AUTH-ARCH-001`, `AUTH-INT-001` |
| `WMREQ109` | `AUTH-ARCH-001`, `AUTH-INT-001`, `AUTH-AUT-001` |
| `WMREQ110` | `VER-ARCH-001`, `VER-STALE-001`, `API-VERSION-001` |
| `WMREQ111` | `VER-ARCH-001`, `VER-STALE-001`, `VER-READ-001` |
| `WMREQ112` | `VER-ARCH-001`, `VER-STALE-001` |
| `WMREQ113` | `VER-ARCH-001`, `VER-STALE-001` |
| `WMREQ114` | `VER-ARCH-001`, `VER-STALE-001`, `ORDER-CONC-001` |
| `WMREQ115` | `IDEMP-FE-001`, `IDEMP-INT-001` |
| `WMREQ116` | `IDEMP-FE-001`, `IDEMP-INT-001`, `IDEMP-RETRY-001` |
| `WMREQ117` | `IDEMP-FE-001`, `IDEMP-INT-001`, `IDEMP-MISMATCH-001` |
| `WMREQ118` | `EVT-CONTRACT-001`, `EVT-DOM-001` |
| `WMREQ119` | `EVT-CONTRACT-001` |
| `WMREQ120` | `EVT-CONTRACT-001`, `OUTBOX-001` |
| `WMREQ121` | `EVT-CONTRACT-001`, `OUTBOX-RECOVERY-001` |
| `WMREQ122` | `EVT-CONTRACT-001`, `OUTBOX-RECOVERY-001` |
| `WMREQ123` | `EVT-CONTRACT-001`, `STUB-ARCH-001` |
| `WMREQ124` | `EVT-CONTRACT-001`, `RT-001` |
| `WMREQ125` | `EVT-CONTRACT-001`, `RT-001` |
| `WMREQ126` | `EVT-CONTRACT-001`, `RT-001` |
| `WMREQ127` | `EVT-CONTRACT-001`, `X-AUT-001` |
| `WMREQ128` | `EVT-CONTRACT-001`, `X-AR-001` |
| `WMREQ129` | `EVT-CONTRACT-001`, `X-COLLAB-001` |
| `WMREQ130` | `API-TRANSPORT-001` |
| `WMREQ131` | `API-TRANSPORT-001`, `API-PLACEMENT-001` |
| `WMREQ132` | `API-TRANSPORT-001`, `API-PLACEMENT-001` |
| `WMREQ133` | `API-TRANSPORT-001`, `ORDER-REBAL-001`, `API-PLACEMENT-001` |
| `WMREQ134` | `API-TRANSPORT-001`, `API-UPDATES-001` |
| `WMREQ135` | `API-TRANSPORT-001`, `API-UPDATES-001` |
| `WMREQ136` | `API-TRANSPORT-001`, `API-UPDATES-001`, `CHK-API-001` |
| `WMREQ137` | `API-TRANSPORT-001`, `API-VERSION-001`, `VER-READ-001` |
| `WMREQ138` | `API-TRANSPORT-001`, `API-ERROR-001` |
| `WMREQ139` | `FE-GEN-001`, `FE-ITEM-001` |
| `WMREQ140` | `FE-GEN-001`, `FE-GROUP-001` |
| `WMREQ141` | `FE-GEN-001`, `FE-FIELD-001` |
| `WMREQ142` | `FE-GEN-001`, `FE-CHK-001` |
| `WMREQ143` | `FE-GEN-001`, `IDEMP-FE-001`, `IDEMP-RETRY-001` |
| `WMREQ144` | `SEC-JSON-001`, `FE-GEN-001` |
| `WMREQ145` | `SEC-JSON-001`, `AUTH-INT-001` |
| `WMREQ146` | `SEC-JSON-001` |
| `WMREQ147` | `SEC-JSON-001`, `PERF-BOARD-001` |
| `WMREQ148` | `SEC-JSON-001`, `PERF-AUTH-001` |
| `WMREQ149` | `SEC-JSON-001`, `OBS-001` |
| `WMREQ150` | `SEC-JSON-001`, `OBS-001` |
| `WMREQ151` | `CERT-SHA-001` |
| `WMREQ152` | `DISCOVERY-001` |
| `WMREQ153` | `HANDOFF-P4A-001` |
| `WMREQ154` | `CERT-BLOCKER-001` |

## 5. Semantic coverage rule

The certification parser MUST compute:

```text
semantic_coverage = requirements whose mapping contains >=1 substantive family
meta families do not satisfy WMREQ001–150
required result = 154 / 154
```

For WMREQ151–154 the substantive behavior is itself certification/discovery/handoff governance, represented by `CERT-SHA-001`, `DISCOVERY-001`, `HANDOFF-P4A-001`, `CERT-BLOCKER-001`.

## 6. Mandatory concurrency scenarios
- same Board stale writer;
- same Item stale writer;
- same Field stale writer;
- same Checklist parent stale writer;
- two different Items insert/move into same Group gap;
- two different Groups move into same Board gap;
- two different Fields move into same Board gap;
- Field schema change vs Item value write;
- archive/delete vs ordinary mutation;
- idempotency first execution vs concurrent replay.

## 7. Mandatory ordering-lock scenarios
- same scope: request B cannot evaluate siblings until request A transaction releases advisory lock;
- different parent scopes: requests can proceed concurrently;
- move excludes moved entity before adjacency check;
- previous-only not current-last => stale-placement;
- next-only not current-first => stale-placement;
- previous+next not adjacent => stale-placement;
- both absent appends under lock;
- named unique constraint collision maps to 409 without handler retry.

## 8. Ordinal persistence corpus

DB/domain parity tests MUST include canonical keys spanning digits, uppercase and lowercase prefixes/segments so PostgreSQL `ORDER BY position COLLATE "C"` is proven identical to `StringComparer.Ordinal` for the actual FractionalIndex grammar.

## 9. Brownfield migration scenarios
- clean scope with valid unique keys remains unchanged;
- duplicate keys are normalized preserving stable preflight order;
- invalid legacy keys normalize deterministically when recoverable;
- ambiguous unrecoverable scope blocks migration;
- after normalization, position type/collation conversion succeeds;
- only then are unique indexes created;
- supported upgrade preserves stable entity IDs and logical order;
- clean install reaches same final schema.

## 10. Version propagation matrix

| Mutation authority | Read-side version source |
|---|---|
| Board | `BoardDto / FullBoardDto.Version` |
| BoardItem + FieldValue | `BoardItemDto / Summary / mutation-facing list Version` |
| BoardGroup | `BoardGroupDto / BoardGroupSchemaDto.Version` |
| BoardField + FieldOption | `BoardFieldDto / BoardFieldSchemaDto.Version` |
| Checklist + ChecklistItem | `ChecklistDto.Version` |

## 11. Idempotency ownership matrix

```text
canonical mutation attempt
→ create idempotency key once
→ call Work API adapter with key
→ api-client request
→ auth/CSRF/network retry reuses same request key
→ server idempotency store
```

A low-level Work API adapter generating a new UUID during unchanged-payload transport retry fails `IDEMP-FE-001`. Conversely, reusing the old key after semantic rebase changes the canonical payload also fails because it would trigger payload-mismatch semantics.

## 12. PLAN work unit → test family matrix

| PLAN work unit | Required test families |
|---|---|
| `WM-V3-INV-001` | `OPTION-ARCH-001`, `VER-ARCH-001`, `CROSS-ARCH-001` |
| `WM-V3-INV-002` | `FE-GEN-001`, `IDEMP-FE-001`, `VER-READ-001` |
| `WM-V3-GATE-001` | `RLS-SESSION-001`, `RLS-DIRECT-001` |
| `WM-V3-GATE-002` | `AUTH-ARCH-001`, `AUTH-INT-001` |
| `WM-V3-ORDER-001` | `ORDER-LOCK-001` |
| `WM-V3-ORDER-002` | `ORDER-PLACE-001`, `ORDER-STALE-001` |
| `WM-V3-ORDER-003` | `ORDER-CONFLICT-001` |
| `WM-V3-ORDER-004` | `ORDER-REBAL-001` |
| `WM-V3-MIG-ORDER-001` | `ORDER-PREFLIGHT-001` |
| `WM-V3-MIG-ORDER-002` | `ORDER-NORM-001`, `MIG-UPGRADE-001` |
| `WM-V3-DATA-001` | `ORDER-DB-001`, `ORDER-DB-002` |
| `WM-V3-DATA-002` | `ORDER-DB-003`, `ORDER-CONC-001` |
| `WM-V3-DATA-003` | `RLS-OPTION-001`, `RLS-VALUE-001`, `RLS-CHK-001`, `RLS-DEPLOY-001` |
| `WM-V3-DATA-004` | `MAP-VALID-001` |
| `WM-V3-DOM-BOARD-001` | `BOARD-DOM-001`, `BOARD-DOM-002` |
| `WM-V3-DOM-ITEM-001` | `ITEM-DOM-001`, `ITEM-DOM-002` |
| `WM-V3-DOM-FIELD-001` | `FIELD-DOM-001`, `FIELD-DOM-002`, `VALUE-DOM-001` |
| `WM-V3-DOM-CHK-001` | `CHK-DOM-001`, `CHK-STATE-001` |
| `WM-V3-APP-BOARD-001` | `BOARD-APP-001`, `BOARD-CONC-001`, `BOARD-AUTH-001` |
| `WM-V3-APP-ITEM-001` | `ITEM-APP-001`, `ORDER-LOCK-001`, `ORDER-PLACE-001` |
| `WM-V3-APP-ITEM-002` | `ITEM-APP-002`, `ORDER-DUP-001`, `ITEM-IDEMP-001` |
| `WM-V3-APP-GROUP-001` | `ORDER-PLACE-001`, `ORDER-DUP-001`, `VER-READ-001` |
| `WM-V3-APP-FIELD-001` | `FIELD-APP-001`, `ORDER-PLACE-001`, `VER-READ-001` |
| `WM-V3-APP-OPTION-001` | `OPTION-ARCH-001`, `ORDER-PLACE-001`, `RLS-OPTION-001` |
| `WM-V3-APP-CHK-001` | `CHK-DOM-001`, `CHK-ORDER-001`, `CHK-API-001`, `CHK-CONC-001` |
| `WM-V3-CONC-001` | `VER-ARCH-001`, `VER-STALE-001`, `API-VERSION-001` |
| `WM-V3-CONC-002` | `VER-READ-001`, `API-VERSION-001` |
| `WM-V3-IDEMP-001` | `IDEMP-FE-001`, `IDEMP-RETRY-001`, `IDEMP-INT-001`, `IDEMP-MISMATCH-001` |
| `WM-V3-EVT-001` | `EVT-DOM-001`, `EVT-CONTRACT-001`, `OUTBOX-001`, `OUTBOX-RECOVERY-001`, `RT-001` |
| `WM-V3-X-001` | `X-AUT-001`, `X-AR-001`, `X-COLLAB-001`, `X-BILL-001`, `X-INT-001` |
| `WM-V3-API-001` | `API-TRANSPORT-001`, `API-PLACEMENT-001`, `API-UPDATES-001`, `API-VERSION-001`, `API-ERROR-001` |
| `WM-V3-FE-001` | `FE-ITEM-001`, `FE-GROUP-001`, `FE-FIELD-001`, `FE-CHK-001`, `FE-GEN-001` |
| `WM-V3-APP-BOUNDARY-001` | `APP-ARCH-001` |
| `WM-V3-APP-BOUNDARY-002` | `BOARD-SCHEMA-001` |
| `WM-V3-DEPLOY-001` | `MIG-CLEAN-001`, `MIG-UPGRADE-001`, `MIG-HISTORY-001`, `MIG-PENDING-001` |
| `WM-V3-DEPLOY-002` | `RLS-DEPLOY-001`, `RLS-DIRECT-001`, `RLS-OPTION-001`, `RLS-VALUE-001`, `RLS-CHK-001` |
| `WM-V3-QUAL-001` | `SEC-JSON-001`, `SEC-QUERY-001` |
| `WM-V3-QUAL-002` | `PERF-BOARD-001`, `PERF-AUTH-001`, `OBS-001` |
| `WM-V3-TEST-001` | `CERT-SHA-001` |
| `WM-V3-CERT-001` | `DISCOVERY-001`, `CERT-SHA-001`, `CERT-BLOCKER-001`, `HANDOFF-P4A-001` |

## 13. PostgreSQL-only evidence
- advisory ordering lock;
- collation parity;
- ordering unique constraints;
- same-gap concurrency;
- RLS CRUD and session hygiene;
- ExpectedVersion database concurrency;
- idempotency transaction behavior;
- outbox transaction fate;
- clean/upgrade migrations;
- RLS policy-pack application.

## 14. Exact API error matrix
- `validation` → documented 4xx validation status/code;
- `not found` → canonical not-found;
- `forbidden` → canonical authorization failure;
- `precondition` → stale ExpectedVersion;
- `work.ordering.stale-placement` → 409 reload/rebase;
- `work.ordering.conflict` → 409 database defense-in-depth collision;
- `idempotency payload mismatch` → canonical conflict.

## 15. Required test execution groups
- Domain full suite;
- Application full suite;
- Infrastructure full suite;
- API full suite;
- Architecture full suite;
- Integration full suite;
- focused Work Board/Item/Field/Checklist;
- focused ordering lock/placement/collation/unique/concurrency/rebalance;
- focused RLS direct/child/session/deployment;
- focused authorization/version/idempotency;
- focused events/outbox/cross-context/realtime;
- focused migration clean/upgrade/normalization;
- frontend Work consumer/idempotency/typecheck;
- OpenAPI/generated consumer drift;
- semantic traceability 154/154.

## 16. Final TESTS Definition of Done
- [ ] all 154 WMREQ rows are present;
- [ ] WMREQ001–150 each map to substantive behavior tests, never only meta tests;
- [ ] ordering serialization/adjacency/collation/normalization are executable;
- [ ] foundational V1 Board/Item/Field/Checklist evidence is retained where still valid;
- [ ] retired V1 behaviors are explicitly replaced;
- [ ] read Version/write ExpectedVersion and logical idempotency ownership are tested end-to-end;
- [ ] all PostgreSQL-only properties run on PostgreSQL;
- [ ] mandatory groups execute non-zero on final candidate.
