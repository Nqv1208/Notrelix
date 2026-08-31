# PR-WG-05 — Phase 7 Permission model

Evidence record for PLAN §74–82 (WG-PERM-001..008), SPEC permission requirements (WGREQ054–062, 084–085, 153), TESTS §61–68 (WG-TST-PERM-*, WG-TST-PRULE-*).

> Scope note: PR-WG-05 covers the combined Permission (Phase 7) + built-in Role (Phase 8) PR lineage. This
> ledger records the **Phase 7 (Permission model)** portion. Phase 8 (built-in Role, PLAN §83–90, WGREQ063–070)
> continues in the same PR-WG-05 when executed.

## Baseline

- Branch: `feature/workspace-governance` (working tree includes the Phase 7 documentation-only changes below, not yet committed).
- Audit date: 2026-08-31.
- Authority read: product contexts, RULE.md, root `AGENTS.md`, `backend/AGENTS.md`, `backend/tests/AGENTS.md`, backend architecture topics (security-tenancy-authorization, infrastructure-and-data, domain-modeling, application-model, testing-and-quality-gates), execution docs for this workstream, Phase 2 semantic inventory (PR-WG-00, semantic-map section, lines ~100–133).
- Baseline (from Phase 6 close `2734ee4`): Application 588, Domain 2576, Architecture 410, Integration 346, API 260, Infrastructure 134.

## Scope of this phase

Establish the **single canonical permission model** (WG-PERM-001, WGREQ054) and prove it through verification +
documentation: deny precedence (WGREQ057), default deny (WGREQ058), tenant/resource scope isolation (WGREQ056),
stable persisted identity (WGREQ055), no runtime decision cache (WGREQ059), PermissionRule as the single canonical
rule model (WGREQ060/061), and client claims not trusted as rule facts (WGREQ062). The decision path
(`AccessPolicyEngine` + `AccessFacts` + `AccessFactsQuery` + `IRequirePermission`) already exists and is tested;
this phase produced **no production source change**. Phase-7 work = canonical documentation correction (§11
DOC_STALE) + permission-evaluation contract (§15a) + plan/tests evidence + one verify-and-defer index finding.

## Verdict ledger

| ID | Scope | Verdict | Evidence | Note |
|---|---|---|---|---|
| WG-PERM-001 | Canonical Permission model | PASS | Single decision path: `Application/Common/Security/AccessPolicyEngine.cs` (pure, architecture-tested — `AuthPipelineArchitectureTests` asserts no DbContext / no IAccessFactsProvider), `AccessFacts.cs` (16 source-fact fields), `Infrastructure/Data/Authz/AccessFactsQuery.cs` (single canonical SQL authority), `PostgresAccessFactsProvider.cs`, `IRequirePermission` (`PermissionAction Action` + `ResourceRef? Resource`) | §11 stale contract inventory removed (no `IPermissionService`/`IPermissionEvaluator`/`IWorkspacePermissionService`/`IResourceReferenceResolver`/`IResourceScopeResolver`/`IPermissionVersionProvider`/`IAuthorizationDecisionStore`/`PermissionContext`/`PermissionDecision` in `backend/src` or `backend/tests`) — DOC_STALE resolved |
| WG-PERM-002 | Permission identity / migration | PASS (documented) | `PermissionAction` enum members persist as `ToString()` (column `action`); `ResourceKind` `{context}.{resource}` record struct persists via `ResourceKindConverter` (column `resource_type`) | Renaming a stored action/`resource_type` value is a persisted-meaning change → data migration required, not in-place rewrite (documented §15a) |
| WG-PERM-003 | Tenant/resource scope | PASS | `AccessFactsQuery` binds `account_id` + `workspace_id` to owning workspace/request scope; `subject_type='User'`, `subject_id=@user_id`; `scope_type='Workspace'` or in-workspace resource match (AccessFactsQuery.cs lines 36–55) | Workspace A rule can never authorize Workspace B; proven by `EvaluateAsync_BoardFromAnotherWorkspace_IsHidden` + `CrossTenantIsolationTests` (WGREQ056/152) |
| WG-PERM-004 | Default deny | PASS | `AccessPolicyEngine.EvaluatePermission`: role null → Deny; no applicable allow band → tail default deny (only ViewWorkspace/ViewBoard/ViewMembers baseline), else Deny | Proven: `EvaluateAsync_ShouldDenyNonMembers`, `AccountScope_ShouldDenyNonMembers`, `AccountScope_ShouldDenySuspendedMembers`, `EvaluateAsync_InactiveOrOutOfWindowRule_IsIgnored` (WGREQ058/085) |
| WG-PERM-005 | Explicit deny precedence | PASS | Within min-priority rule band, any Deny denies before Allow (AccessPolicyEngine.cs lines 127–140); `AccessFactsQuery` returns rules `ORDER BY priority` | Proven: `EvaluateAsync_SamePriorityDenyOverridesAllow`, `AccountScope_ExplicitGovernanceDeny_OverridesAdminFallback`, `AccountScope_ExplicitGovernanceAllow_GrantsBaselineDeniedAction` (WGREQ057/084). Precedence documented §15a |
| WG-PERM-006 | PermissionRule integration | PASS | `PermissionRule` is the single canonical persisted action-level rule; `ResourcePermission` = subject→resource ACL (engine uses row-existence); `WorkspacePolicy` = secondary config not in evaluator; no duplicate DSL | Single-owner statement in §11/§15a; consistent with Phase 2 semantic map (PR-WG-00 §lines 100–133) |
| WG-PERM-007 | Permission cache | PASS (none) | No runtime permission-decision cache in the effective path — `AccessFacts` computed per protected request; persisted `resource_permission_inheritance_cache` projection is NOT on the decision path | Revocation effective next request, no cache security window (BE-SEC-024); `EvaluateAsync_RevokedPermissionsAreInvalid` proves revoked → deny (WGREQ059/153) |
| WG-PERM-008 | DB/index hardening | FINDING (verify + defer) | Existing `permission_rules` indexes: `idx_permission_rules_workspace_id`, `idx_permission_rules_scope_action` (ScopeType, Action), `idx_permission_rules_status`; no subject-aware / query-aligned index | See "WG-PERM-008 index finding" below. No DDL this phase |

## WG-PERM-008 index finding — registered, deferred, measured promotion gate

**Finding.** The hot-path access-facts query filters `governance.permission_rules` by:

```text
account_id  workspace_id  action  subject_type  subject_id  scope_type  status + validity/resource predicates
```

Existing indexes (`PermissionRuleConfiguration.cs`):

```text
idx_permission_rules_workspace_id     (workspace_id)
idx_permission_rules_scope_action     (scope_type, action)
idx_permission_rules_status           (status)
```

Missing: a subject-aware / composite index aligned to the query's leading predicate. This is a
**performance finding only**.

```text
correctness/security blocker: NO
Phase-7 semantic blocker:      NO
performance finding:           YES
schema/migration if promoted:  YES (Class F / C4)
```

**Decision (user): verify in Phase 7, defer DDL.** Before choosing an index shape, a targeted
performance-hardening task must run `EXPLAIN (ANALYZE, BUFFERS)` against representative permission-rule
cardinalities and realistic Workspace/subject distributions. If the plan shows material scan/filter cost on the
protected-request hot path, promote to a Class-F schema change and add the index in the performance/migration
hardening phase. Do not pre-commit to `(workspace_id, action, subject_id)` until the measured plan confirms
ordering/selectivity; the better index may need `account_id`, `subject_type`, or a partial predicate such as
active rules.

Owner/follow-up: registered under the performance/migration hardening workstream. Non-blocking for Phase 7 close.

## Test coverage (TESTS §61–68)

| TEST ID | Verdict | Note |
|---|---|---|
| WG-TST-PERM-DOM-001 canonical Permission meaning | PASS | Single decision path documented; §11 stale inventory removed; architecture test proves engine purity |
| WG-TST-PERM-MIG-001 persisted identity stable | PASS | PermissionAction `ToString()` + ResourceKind `{context}.{resource}` string are stable persisted identities; no rename this phase |
| WG-TST-PERM-INT-001 Workspace A cannot authorize B | PASS | SQL account_id/workspace_id binding + `EvaluateAsync_BoardFromAnotherWorkspace_IsHidden` + CrossTenantIsolationTests |
| WG-TST-PERM-APP-001 explicit deny precedence | PASS | `EvaluateAsync_SamePriorityDenyOverridesAllow` + the two AccountScope deny/allow governance tests |
| WG-TST-PERM-APP-002 default deny | PASS | `ShouldDenyNonMembers`, `AccountScope_ShouldDenyNonMembers`, `AccountScope_ShouldDenySuspendedMembers`, `InactiveOrOutOfWindowRule_IsIgnored` |
| WG-TST-PERM-INT-002 permission cache revocation | PASS | No decision cache; `EvaluateAsync_RevokedPermissionsAreInvalid`; revocation effective next request |
| WG-TST-PRULE-APP-001 deterministic evaluation | PASS | SQL `ORDER BY priority` + min-priority band + deny-over-allow; no time/random input |
| WG-TST-PRULE-SEC-001 client claims not trusted | PASS (design + architecture) | Action from server-side `IRequirePermission`; all facts server-derived; client workspace/account are inputs not authority (BE-APP-013) |

## Suite evidence (documentation-only phase, no source change)

| Suite | Result | Note |
|---|---|---|
| Application.Tests | 588 (baseline) | no source change this phase; re-run green |
| Domain.Tests | 2576 (baseline) | no source change this phase |
| Architecture.Tests | 410 (baseline) | no source change this phase |

No API/Integration/Infrastructure/frontend/OpenAPI/schema change this phase → those suites return to the
PR-WG-03/04 baseline without delta and are not re-run (no public/persistence contract touched). No new
migration, so Infrastructure/Integration schema suites unaffected.

## Phase 7 decisions

### D7-A — Canonical permission model is the existing single decision path — DECIDED (no new code)

The canonical model is already implemented and tested: `AccessPolicyEngine` (pure) + `AccessFacts` +
`AccessFactsQuery` + `IRequirePermission`. Phase 7 established one semantic meaning by correcting the stale
canonical contract inventory (§11) and writing the concrete decision contract (§15a). No second
PermissionService/IPermissionEvaluator exists. WG-PERM-001/006 satisfied without production change.

### D7-B — Deny precedence is the min-priority-band deny-over-allow rule — DECIDED + TESTED

The engine considers only the min-priority rule band (rules already action/workspace/subject-filtered in SQL);
within that band any Deny denies before Allow. This is the documented, tested precedence (WGREQ057). No new
deny DSL added.

### D7-C — No runtime permission-decision cache — DECIDED (none to harden)

There is no decision cache in the effective path; AccessFacts is computed per protected request. Revocation is
effective immediately on the next request (BE-SEC-024). If a decision cache is ever introduced it must carry
tenant/resource/principal or a permission-version key and an explicit invalidation path (§BE-SEC-023).

### D7-D — WG-PERM-008 index deferred with measured promotion gate — DECIDED

See "WG-PERM-008 index finding". No DDL in Phase 7; a performance-hardening task will run EXPLAIN(ANALYZE,
BUFFERS) at representative cardinality before any index shape is chosen.

## Follow-up findings

- **Carried (Phase 5/6 → 7, unchanged)** — D5-F (RLS exposure on by-id invitation acceptance), WG-FIND-501
  (mock-backend invitation routes), WG-FIND-502 (duplicate `WorkspaceInvitationDto` name), WG-FIND-503
  (OpenAPI `System.Void` placeholders), resource/action persisted-authorization consumers carried to the
  WorkManagement handshake (PR-WG-07).
- **New (Phase 7)** — WG-PERM-008 index finding (deferred DDL, owner/measurement registered above).

## Phase 7 exit

Permission semantics are D4/D5-ready (PLAN §82):

1. one canonical Permission meaning — single `AccessPolicyEngine` decision path, documented §11/§15a (D7-A);
2. stable persisted permission identity documented (WG-PERM-002);
3. tenant/resource scope isolation confirmed via SQL binding + negative proofs (WG-PERM-003);
4. default deny confirmed by tests and engine semantics (WG-PERM-004);
5. explicit deny precedence documented and tested (WG-PERM-005, D7-B);
6. PermissionRule is the single canonical rule model, no duplicate DSL (WG-PERM-006);
7. no runtime decision cache; revocation immediate (WG-PERM-007, D7-C);
8. DB/index hardening = verify + defer with measured promotion gate, no DDL (WG-PERM-008, D7-D);
9. client claims not trusted as rule facts (WGREQ062).

Recorded decisions D7-A..D7-D; carried + new findings above. **Phase 7 CLOSED.** Phase 8 (built-in Role,
PLAN §83–90) continues PR-WG-05 when it opens.
