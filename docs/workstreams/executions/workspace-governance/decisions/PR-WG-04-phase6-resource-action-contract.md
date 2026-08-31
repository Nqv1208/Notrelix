# PR-WG-04 — Phase 6 Resource/Action contract

Evidence record for PLAN §64–73 (WG-RES-001..009), SPEC resource/action requirements (WGREQ044–053, 094, 152), TESTS §51–60 (WG-TST-RES-*, WG-TST-ACT-*).

## Baseline

- Branch: `feature/workspace-governance` (working tree includes Phase 6 changes below, not yet committed).
- Audit date: 2026-08-31.
- Authority read: product contexts (`docs/product/workspaces.md`, `docs/product/governance.md`, `docs/product/accounts.md`, `docs/product/identity.md`), RULE.md, root `AGENTS.md`, `backend/AGENTS.md`, `backend/tests/AGENTS.md`, backend architecture topics (domain-modeling, application-model, security-tenancy-authorization, infrastructure-and-data, testing-and-quality-gates), execution docs for this workstream.
- Baseline (from PR-WG-03 close): Application 587, Domain 2576, Architecture 410, Integration 346, API 260, Infrastructure 134.

## Scope of this phase

Stabilize the **existing** resource authorization category (`ResourceKind`) and the resource-owner facts boundary, and prove the Account/Workspace pairing invariant — the D4 producer contract that unlocks WorkManagement and later Documents/Billing/Automation handshakes (PLAN §73). No forced rename, no new resource abstraction, no WorkManagement business use case changes this phase.

## Verdict ledger

| ID | Scope | Verdict | Evidence | Note |
|---|---|---|---|---|
| WG-RES-001 | Inventory existing resource abstractions | PASS | `Domain/SharedKernel/ResourceKind.cs` (open string-based `{context}.{resource}` record struct, 16 active kinds), `Domain/SharedKernel/ResourceRef.cs`, `Domain/Governance/Permissions/PermissionAction.cs` (31 members), `Application/Common/Requests/Security/IRequirePermission` (`PermissionAction Action` + `ResourceRef? Resource`), `Application/Common/Context/IResourceLocator`, `Infrastructure/Services/ResourceLocator` | No legacy `ResourceType`/`PermissionContext`/`enum Subject`/`ResourceScope`/`AuthorizationRequirement` in `backend/src` or `backend/tests`. Inventory recorded in PLAN §65 |
| WG-RES-002 | Canonical category without forced rename | PASS | ResourceKind is already the canonical source category; no rename performed | No rename proof required — source already equivalent to SPEC vocabulary (WGREQ045 the no-rename rule holds) |
| WG-RES-003 | Resource/action ownership | PASS | Resource teams own identity/lifecycle/Action (Board/Page/etc.); `PermissionTemplateEntry`/`PermissionRule` in Governance registry; `PermissionAction` is the single authoritative action vocabulary | No Governance-invented product action; `ManageBoard`/`ViewPage` etc. are emitted by resource-context commands, not fabricated by Governance |
| WG-RES-004 | Authorization FACTS contract | PASS | `Application/Common/Security/AccessFacts.cs` — all 16 fields are source-owned facts (UserExists, EmailVerified, AccountExists, AccountMemberRole, WorkspaceExists, WorkspaceMemberRole, ResourceExists, ResourceAudience, ResourceMemberRole, HasExplicitResourcePermission, PermissionRules, HasActiveSubscription, SubscriptionTier, FeatureEnabled, AccountOperational, UserOperational) | No `CanEdit/CanDelete/...` policy field; `PermissionRules` carries Governance policy separately (WG-RES-006) |
| WG-RES-005 | Provider ownership / adapter placement | PASS (D6-A) | `ResourceLocator` documented under §BE-SEC-011 as an approved cross-context read port: concentrates per-DbContext ownership reads for `ExecutionContextBehavior` scope resolution; performs no authz/mutation | Doc entry added to `backend/docs/architecture/security-tenancy-authorization.md`. No Governance class queries a downstream `DbContext` (see Governance coupling) |
| WG-RES-006 | Facts vs policy classification | PASS | AccessFacts exposed facts only; PermissionRule (Governance) is the policy carrier; no hidden second permission hierarchy introduced | WG-TST-RES-ARCH-001C satisfied by review |
| WG-RES-007 | Account/Workspace consistency | PASS (NEW negative proof) | `ExecutionContextBehavior` always derives Account from the owned workspace/resource row; API-token path additionally enforces `BoundAccountId == snapshot.AccountId`. New test: `ExecutionContextBehaviorTests.Workspace_request_with_api_token_bound_to_different_account_is_denied` | Account A + Workspace B pairing rejected (`ForbiddenException`, no snapshot). No client-supplied pairing ever trusted (BE-SEC-011) |
| WG-RES-008 | Transport-neutral evolution seam | PASS | `IResourceLocator` returns `ResourceLocation?` (no EF/gRPC/HTTP/broker types); `ResourceLocator` implementation may evolve independently | WG-TST-RES-ARCH-001B satisfied |
| WG-RES-009 | Module-first slice placement | PASS | No production use case added to deprecated legacy feature paths | Follows candidate-SHA canonical module-first layout |

## Governance coupling

`Messaging/Consumers/Governance/GovernanceStubConsumers.cs` contains three consumers
(`CustomRoleAssignedConsumer`, `ResourcePermissionGrantedConsumer`, `ResourcePermissionRevokedConsumer`) that
are pure logging stubs: no downstream DbContext/service/provider read or mutation, no persistence, no
cross-context side effects. Endpoints are independent (`notrelix-governance-*-v1`). This satisfies the
Phase 6 exit guard "no Governance private EF read of downstream contexts" and WG-TST-RES-ARCH-001A mandatory
Board negative guard (no `Notrelix.Infrastructure.Governance.*` references `IWorkManagementDbContext`).

## Test coverage (TESTS §51–60)

| TEST ID | Verdict | Note |
|---|---|---|
| WG-TST-RES-ARCH-001A facts adapter ownership | Covered (review + doc) | ResourceLocator documented as cross-context read port; GovernanceStubConsumers have zero downstream reads; no Governance class reads downstream DbContexts |
| WG-TST-RES-ARCH-001B transport-neutral facts provider | Covered (review) | IResourceLocator exposes no transport types |
| WG-TST-RES-ARCH-001C facts not a second policy engine | Covered (review) | AccessFacts all facts; PermissionRule is the single policy carrier |
| WG-TST-RES-DOM-001 category stability | Covered | ResourceKind kept; no rename test requires `ResourceType`→`ResourceKind` migration |
| WG-TST-RES-ARCH-002 no CLR-name auth identity | Covered (pre-existing) | ResourceKind is a stable `{context}.{resource}` string, not an assembly-qualified type name |
| WG-TST-RES-APP-001 resource ID opaque | Covered (pre-existing) | IRequirePermission carries `ResourceRef?`; scope resolved via IResourceLocator, not foreign parsing |
| WG-TST-RES-INT-001 Account/Workspace/resource scope consistency | Covered (NEW negative) | New `ExecutionContextBehaviorTests` pairing-denial test + existing `CrossTenantIsolationTests` (Workspace A ∌ Workspace B); structural derivation makes cross-account pairing impossible |
| WG-TST-RES-ARCH-003 resource registration explicit | Covered (pre-existing) | architecture/manifest inventory records request scope descriptors; frozen `request-execution-baseline.json` |
| WG-TST-RES-CONTRACT-001 owner controls action declaration | Covered (review) | PermissionAction emitted by resource-context commands only; Governance registry generic |
| WG-TST-ACT-DOM-001 stable business action identity | Covered | `PermissionAction` is the single canonical action enum (31 members) |
| WG-TST-ACT-MIG-001 persisted action rename requires migration | NOT IN PHASE | No action renamed this phase; rule documented for future (WG-ACT owners) |
| WG-TST-ACT-ARCH-001 HTTP verb not canonical action | Covered (architecture) | IRequirePermission.Action is semantic; not derived from HTTP verb |

## Suite evidence (all green, post-change, SDK 9.0.313)

| Suite | Result | Note |
|---|---|---|
| Backend per-project builds | 9 projects (src + tests), 0 errors | `backend.slnx` open fails in this environment on `RestoreEnablePackagePruning` eval (pre-existing toolchain artifact); CI used as gate authority |
| Application.Tests | 588 passed | baseline 587 → +1 (`Workspace_request_with_api_token_bound_to_different_account_is_denied`) |
| Domain.Tests | 2576 passed (Phase-5 baseline) | zero production Domain source change this phase |
| Architecture.Tests | 410 passed | re-run green after the canonical doc entry; no source architecture change |

No API/Integration/Infrastructure/frontend/OpenAPI change this phase → those suites return to the recorded
PR-WG-03 baseline without delta and are not re-run for this phase (no public/persistence contract touched).

## Phase 6 decisions

### D6-A — ResourceLocator stays an approved shared cross-context read port — DECIDED + DOCUMENTED

`ResourceLocator` (`Infrastructure/Services/ResourceLocator.cs`) reads 5 owner DbContexts and resolves the
ownership tuple `(ResourceId, AccountId, WorkspaceId)` for 16 ResourceKind values solely for the cross-cutting
`ExecutionContextBehavior` resource-scope path. It performs no authorization, no mutation, and no business
logic. Splitting it into per-feature-family fragments would still require the same 5 DbContext references
(WorkManagement must locate Boards referenced from Collaboration/Governance etc.) with no coupling reduction
(BE-INF-026).

Decision: keep it as a single Infrastructure adapter behind the transport-neutral `IResourceLocator` seam and
document it as an **approved cross-context tenant-scope read port** in
`backend/docs/architecture/security-tenancy-authorization.md` §BE-SEC-011. The doc explicitly states this is
not precedent for new handler-local cross-context reads. This resolves the WG-RES-005 placement question
without weakening the "no Governance private EF read of downstream contexts" guard, because `ResourceLocator`
is a scope-resolution adapter, not a Governance business consumer.

### D6-B — ResourceKind is the canonical category — DECIDED (no change)

The source already uses `ResourceKind` (not `ResourceType`) as a stable string-based `{context}.{resource}`
category. No rename, no migration, no ADR required. SPEC WGREQ045's no-forced-rename rule is satisfied by
keeping the current canonical name.

### D6-C — AccessFacts stays all-facts — DECIDED (no change)

Every `AccessFacts` field is a source-owned fact; Governance policy lives in `PermissionRule`. No `Can*`
policy projection was introduced, closing the WG-RES-006 stop condition (WG-PLAN-STOP-016 fact-becomes-policy).

### D6-D — Cross-account/workspace pairing denied — DECIDED + PROVEN

Account is always derived from the owned workspace/resource row (`ExecutionContextBehavior`), so Account A can
never be paired with Workspace B belonging to Account B. The API-token path doubly enforces bound-account
equality. New negative test proves the denial. BE-SEC-011/BE-SEC-012 satisfied.

## Follow-up findings

- **Carried (Phase 5 → 6, unchanged)** — D5-F (RLS exposure on by-id invitation acceptance, open), WG-FIND-501
  (mock-backend invitation routes), WG-FIND-502 (duplicate `WorkspaceInvitationDto` name), WG-FIND-503
  (OpenAPI `System.Void` placeholders), WG-RES/role owners via WG-ROLE-DEC-001.
- **New (Phase 6)** — Resource/action **persisted-authorization** consumers for WorkManagement/Board are still
  carried to the WorkManagement handshake phases (PR-WG-07); this phase locked the vocabulary and the
  facts/provider boundary but implemented no downstream permission enforcement. Registered, non-blocking.

## Phase 6 exit

The D4 producer contract (PLAN §73) is satisfied:

1. stable resource category using canonical current source naming — `ResourceKind` kept (D6-B);
2. stable resource-owned Action vocabulary — `PermissionAction` confirmed as the single canonical enum;
3. resource-owner facts lookup/provider ownership defined — `IResourceLocator` + `ResourceLocator` documented
   cross-context read port (D6-A);
4. no Governance private EF read of downstream contexts — confirmed (GovernanceStubConsumers pure stubs);
5. representative Board contract can be implemented without private-context coupling — ResourceLocator reads
   Boards only as the cross-cutting scope resolver (D6-A), not as a WorkManagement business use case;
6. P3-A Domain/Data work may open under the approved producer contract.

Recorded decisions D6-A..D6-D; carried findings above. Phase 6 CLOSED.
