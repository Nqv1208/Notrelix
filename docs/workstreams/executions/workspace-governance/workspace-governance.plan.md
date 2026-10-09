---
document_id: WRK-PLAN-WORKSPACE-GOVERNANCE
document_type: workstream-plan
status: active
owner: workspace-governance-team
candidate_baseline:
  branch: develop
  sha: e74bbc75f3713d99cead9b535cc935f211a7c47e
  tree: d71809d04310e35d30483539a6396c1c859cabef
supersedes:
  - workspace-governance.plan.md (byte-identical spec copy)
applies_to:
  - backend
  - workspaces
  - governance
  - workspace
  - workspace-membership
  - invitations
  - teams
  - spaces
  - workspace-rules
  - provisioning
  - workspace-settings
  - resource-kind
  - resource-action
  - permissions
  - permission-rules
  - roles
  - policies
  - resource-permissions
  - share-links
  - governance-templates
  - authorization
  - audit-security-evidence
  - tenant-isolation
evidence:
  - docs/workstreams/executions/workspace-governance/workspace-governance.spec.md
  - docs/workstreams/executions/workspace-governance/workspace-governance.tests.md
  - docs/workstreams/executions/workspace-governance/workspace-governance.certification.md
  - docs/workstreams/executions/workspace-governance/decisions/PR-WG-00-semantic-inventory.md
  - backend/docs/architecture/security-tenancy-authorization.md
---

# PLAN — Workspace & Governance (P2): Milestone A → B Execution

## 1. Purpose

This is the normative **HOW** plan for certifying Workspace & Governance.

It converts the SPEC (`WRK-SPEC-WORKSPACE-GOVERNANCE`) and the certification
records (`WRK-CERT-WORKSPACE-GOVERNANCE`) into executable phases, work units,
and evidence packets.

It replaces the previous `plan.md`, which was a byte-identical copy of the SPEC
and therefore defined no implementation order, no work units, and no
`WG-CERT-HO-001` handoff artifact.

This revision targets **Milestone A — P2 PRODUCER CONTRACT VERIFIED** and
**Milestone B — P2 PROTECTED SLICE CERTIFIED**. Milestone C units are listed
with their real layer status so no referenced work unit remains undefined, but
Milestone C execution is out of scope for this revision.

## 2. Scope decision (this revision)

```text
IN SCOPE  Goal A: certify the producer contract (P2A-CERT-001..004)
IN SCOPE  Goal B: certify the protected slice (P2B-CERT-001..016)
IN SCOPE  close blockers WG-DEBT-008/009/010 + WG-TEST-GAP-001/002/003/005/007
IN SCOPE  recreate WG-CERT-HO-001 P3 handoff
NOT NOW   Milestone C secondary capabilities (CustomRole/Policy/Template/
          ShareLink-consumption/Audit runtime) — record honest layer status only
NOT NOW   frontend (backend-only execution package per spec applies_to)
NOT NOW   WG-ROLE-DEC-001 ManageBoard action matrix (needs WorkManagement decision)
```

## 3. Non-negotiable rules

- Do not mark a capability VERIFIED/STABLE from source presence, test-file
  existence, a prior SHA, or a green unrelated CI run (cert principle 1-3).
- Use one certified authorization path only:
  `AccessControlBehavior → IAccessFactsProvider → AccessFacts → IAccessPolicyEvaluator → AccessPolicyEngine → AccessDecision`.
- Preserve ownership split: Facts → resource owner; Policy → Governance;
  Enforcement → Application pipeline; Isolation → Infrastructure/RLS;
  Transport → API (cert principle 5-6).
- Do not treat RLS invisibility as proof of action authorization.
- Do not silently expand `AccessFactsQuery` foreign-table reads
  (`WG-DEBT-006`) and do not treat generic `ManageBoard` as D5
  (`WG-DEBT-007`); both MUST be named in the P3 handoff.
- Do not rewrite migration history; candidate has a single
  `20260702093805_SchemaBaseline` (148-table smoke expectation).
- Do not weaken architecture tests to make evidence pass.
- Do not claim a zero-test or skipped-job gate as certification.

## 4. Execution phases

```text
Phase 0 — Baseline & PLAN artifact               (this document)
Phase 1 — Milestone A certification              (P2A-CERT-001..004)
Phase 2 — Milestone B blocker closure            (revocation first)
Phase 3 — Milestone B full evidence collection   (P2B-CERT-001..016 + CI)
Phase 4 — P3-B handoff packet                    (WG-CERT-HO-001)
```

## 5. Work unit inventory

The certification and test documents reference PLAN work units that the
previous plan.md never defined. Every referenced unit is defined below.

### 5.1 Phase 0 — baseline / plan

| Work unit | Name | Cert/CERT records | Tests |
|---|---|---|---|
| `WG-DOC-001` | Rebuild PLAN artifact and alignment | CERT-DOC-001, CI-001..009 | WG-TST-SYNC-ARCH-001, WG-TST-SYNC-EVIDENCE-001 |
| `WG-DOC-002` | Generated/public contract alignment | CERT-DOC-002 | WG-TST-API-OAS-001, WG-TST-EVT-CONTRACT-001 |
| `WG-DOC-003` | Workstream status sync | P2B-CERT-015, CERT-DOC-003 | WG-TST-SYNC-LAYER-001, WG-TST-SYNC-HANDOFF-001 |
| `WG-DOC-004` | Evidence/CI reproducibility note | CERT-CI-001..009 | WG-TST-SYNC-EVIDENCE-001 |

### 5.2 Phase 1 — Milestone A producer contract

| Work unit | Name | Cert record | Tests |
|---|---|---|---|
| `WG-WSP-001` | Workspace identity producer contract | P2A-CERT-001, P2B-CERT-001 | WG-TST-WSP-DOM-001, WG-TST-WSP-INT-001 |
| `WG-WSP-002` | Account containment producer contract | P2A-CERT-001, P2B-CERT-001 | WG-TST-WSP-DOM-001, WG-TST-P2-CORE-001 |
| `WG-RES-001` | ResourceKind / ResourceId producer | P2A-CERT-002 | WG-TST-RES-ARCH-001, WG-TST-RES-X-001 |
| `WG-RES-002` | PermissionAction producer | P2A-CERT-003 | WG-TST-ACT-ARCH-001 |
| `WG-RES-003` | Facts ownership boundary | P2A-CERT-002/004, P2B-CERT-007/012 | WG-TST-RES-X-001, WG-TST-OWN-ARCH-001, WG-TST-FACTS-INT-001 |
| `WG-RES-004` | P3-A cross-context ownership handoff | P2A-CERT-004 | WG-TST-SYNC-HANDOFF-001, WG-TST-OWN-ARCH-002 |
| `WG-WM-002` | WorkManagement action semantics used by producer contract | P2A-CERT-004 | WG-TST-WM-X-004, WG-TST-WM-CONTRACT-001 |
| `WG-GATE-001` | Milestone A gate decision | P2A-CERT-001..004 | all P2A tests |
| `WG-P1-001` | Upstream Identity/Accounts Actor contract use | WG-TST-UP-X-001 (/ PLAN: WG-P1-001, WG-P1-002) | WG-TST-UP-ARCH-001, WG-TST-UP-INT-001 |
| `WG-P1-002` | Upstream P1 producer contract compatibility | WG-TST-UP-X-001 | WG-TST-UP-INT-002, WG-TST-UP-INT-003 |

### 5.3 Phase 2 — Milestone B blocker closure (priority order)

| Work unit | Name | Closes | Cert record | Tests to write/extend |
|---|---|---|---|---|
| `WG-MEM-004` | Membership → access-grant projection/revocation | **WG-TEST-GAP-003 / WG-DEBT-008** | P2B-CERT-003, CERT-DATA-002 | `RlsRuntimeEnforcementTests`-family: suspend + remove + role-change grant revocation under app role |
| `WG-SEC-002` | RLS grant sync defense | WG-TEST-GAP-003 | P2B-CERT-003/009/014 | WG-TST-RLS-SEC-001..003, WG-TST-SYNC-RLS-001 |
| `WG-CONC-001` | Membership + owner concurrency | **WG-TEST-GAP-001 + WG-TEST-GAP-002 / WG-DEBT-009/010** | P2B-CERT-002, CERT-CONC-001 | WG-TST-CONC-MEM-001, WG-TST-CONC-OWNER-001 (DB-level) |
| `WG-WM-003` | Board representative cross-account negative gate | **WG-TEST-GAP-005** | P2B-CERT-011 | WG-TST-WM-X-001..004 |
| `WG-REL-001` | Authorization dependency fail-closed | **WG-TEST-GAP-007** | CERT-REL-001 | WG-TST-SEC-MASTER-003 |
| `WG-SEC-003` | Secret telemetry/security scan on candidate | **WG-TEST-GAP-008** | P2B-CERT-014 | scan gate on candidate |
| `WG-MIG-004` | Supported-upgrade fixture | **WG-TEST-GAP-009** | P2B-CERT-013, CERT-MIG-001 | WG-TST-MIG-DB-001..003 or NOT_CHANGED |

### 5.4 Phase 3 — Milestone B full evidence

#### Workspace lifecycle & membership

| Work unit | Name | Cert record | Tests |
|---|---|---|---|
| `WG-WSP-003` | Workspace settings/profile restrictions | P2B-CERT-001 | WG-TST-WSP-DOM-004, WG-TST-WSP-APP-001 |
| `WG-WSP-004` | Personal Workspace provisioning | P2B-CERT-001 | WG-TST-WSP-APP-002, WG-TST-WSP-INT-002 |
| `WG-MEM-001` | Membership identity/lifecycle | P2B-CERT-002 | WG-TST-MEM-DOM-001 |
| `WG-MEM-002` | Owner/admin safety | P2B-CERT-002 | WG-TST-MEM-DOM-002 |
| `WG-MEM-003` | Membership authorization facts | P2B-CERT-002 | WG-TST-MEM-INF-001, WG-TST-MEM-X-001 |
| `WG-MEM-005` | Membership historical attribution | P2B-CERT-002 | WG-TST-MEM-APP-001, WG-TST-MEM-INT-002 |
| `WG-PERM-001` | Permission / default-deny semantics | P2B-CERT-004 | WG-TST-PERM-APP-001, WG-TST-PRULE-APP-001 |
| `WG-PERM-002` | PermissionRule precedence/time-window | P2B-CERT-004 | WG-TST-AUTHZ-APP-001..004 |
| `WG-PERM-003` | PermissionRule edge matrix | P2B-CERT-004 | WG-TST-AUTHZ-API-001 |
| `WG-ROLE-001` | Built-in WorkspaceRole baseline | P2B-CERT-005 | WG-TST-ROLE-DOM-001 |
| `WG-ROLE-002` | Role-to-authority deterministic interpretation | P2B-CERT-005/010 | WG-TST-ROLE-SEC-001 |
| `WG-RPERM-001` | ResourcePermission management ceiling/rank | P2B-CERT-010 | WG-TST-RPERM-INT-001, WG-TST-CONC-RPERM-001 |

#### Authorization pipeline

| Work unit | Name | Cert record | Tests |
|---|---|---|---|
| `WG-AUTHZ-001` | AccessControlBehavior canonical handshake | P2B-CERT-006/012 | WG-TST-AUTHZ-APP-001, WG-TST-PIPE-ARCH-001 |
| `WG-AUTHZ-002` | One-security-mechanism architecture | P2B-CERT-006/012 | WG-TST-AUTHZ-ARCH-001, WG-TST-AUTHZ-ARCH-002 |
| `WG-AUTHZ-003` | AccessFacts composition | P2B-CERT-007/012 | WG-TST-FACTS-INT-001, WG-TST-FACTS-X-001, WG-TST-AUTHZ-SEC-001 |
| `WG-AUTHZ-004` | AccessPolicyEngine boundary | P2B-CERT-008/012 | WG-TST-AUTHZ-ARCH-002, WG-TST-HANDSHAKE-ARCH-002 |
| `WG-AUTHZ-006` | Approved System-internal authorization contract | P2B-CERT-016 | WG-TST-PIPE-SEC-001, WG-TST-SYNC-AUTHZ-001 |
| `WG-AUTHZ-010` | Background/automation principal scope | P2B-CERT-016 | WG-TST-AUTO-X-001, WG-TST-REL-AUTHZ-001 |
| `WG-SYNC-003` | System-internal contract classification | P2B-CERT-016 | WG-TST-SYNC-AUTHZ-001 |
| `WG-SEC-001` | RLS tenant isolation policies | P2B-CERT-009/014 | WG-TST-RLS-SEC-001..003, WG-TST-SEC-MASTER-002 |
| `WG-SEC-004` | Security baseline sweep (permission audit) | P2B-CERT-014 | WG-TST-SEC-MASTER-004, WG-TST-PIPE-SEC-001 |

#### Architecture / downstream handshake

| Work unit | Name | Cert record | Tests |
|---|---|---|---|
| `WG-WM-001` | Board representative protected flow | P2B-CERT-011 | WG-TST-WM-X-001, WG-TST-WM-X-002 |
| `WG-WM-004` | Board allow/deny/cross-tenant proof | P2B-CERT-011 | WG-TST-WM-X-003, WG-TST-WM-X-004, WG-TST-P2-CORE-006 |
| `WG-WM-005` | Board action vocabulary mapping | CERT-X-002 | WG-TST-WM-CONTRACT-001 |
| `WG-WM-006` | Board facts consumption (P3-B boundary) | CERT-X-002 | WG-TST-WM-CONTRACT-002 |
| `WG-WM-007` | Board P2 handshake usability | CERT-X-002 | WG-TST-WM-P2-001..004 |
| `WG-WM-008` | WorkManagement invalidating-change rules | CERT-X-002 | WG-TST-WM-P2-001, WG-TST-SYNC-HANDOFF-001 |
| `WG-WM-009` | ManageBoard classification debt | CERT-X-002 | WG-TST-WM-P2-001 |
| `WG-INV-004` | WorkManagement module boundary | P2B-CERT-012 | WG-TST-OWN-ARCH-001, WG-TST-OWN-ARCH-003 |
| `WG-INV-005` | Governance module boundary | P2B-CERT-012 | WG-TST-OWN-ARCH-002 |
| `WG-INV-006` | Persistence ownership boundary | P2B-CERT-012, CERT-DATA-001 | WG-TST-OWN-ARCH-004, WG-TST-SYNC-PERSIST-001 |
| `WG-INV-007` | Architecture manifest alignment | P2B-CERT-012 | WG-TST-SYNC-ARCH-001 |
| `WG-INV-008` | Invitation module boundary (invitation is not membership) | P2B-CERT-012 | WG-TST-OWN-ARCH-001 |
| `WG-INV-009` | Identity/accounts shared boundary | P2B-CERT-012 | WG-TST-OWN-ARCH-002, WG-TST-UP-ARCH-001 |
| `WG-INV-010` | Cross-stack boundary dependencies | P2B-CERT-012 | WG-TST-OWN-ARCH-004, WG-TST-SYNC-FACTS-001 |

#### Data / migration / API / events / reliability

| Work unit | Name | Cert record | Tests |
|---|---|---|---|
| `WG-MIG-001` | Migration baseline/model sync | P2B-CERT-013, CERT-DATA-001, CERT-MIG-001 | WG-TST-MIG-DB-001, WG-TST-MIG-MEM-001 |
| `WG-MIG-002` | Membership persistence compatibility | P2B-CERT-013, CERT-MIG-001 | WG-TST-MIG-MEM-001 |
| `WG-MIG-003` | Resource/action/role persisted identifier compatibility | P2B-CERT-013, CERT-MIG-001 | WG-TST-MIG-RES-001, WG-TST-MIG-ACT-001, WG-TST-MIG-ROLE-001 |
| `WG-MIG-005` | Policy/link/history persistence | CERT-DATA-001, CERT-MIG-001 | WG-TST-MIG-POL-001, WG-TST-MIG-LINK-001 |
| `WG-MIG-006` | Startup/health validation | CERT-DATA-001 | WG-TST-SYNC-PERSIST-001 |
| `WG-MIG-007` | Tenant-scoped seed/RLS migration | CERT-DATA-001 | WG-TST-MIG-DB-002 |
| `WG-MIG-008` | Outbox/event migration | CERT-DATA-001 | WG-TST-SYNC-PERSIST-001 |
| `WG-MIG-009` | Rollback/roll-forward classification | CERT-DATA-001 | WG-TST-MIG-DB-003 |
| `WG-API-001` | Workspace/Governance API contract producers | CERT-API-001 | WG-TST-API-WSP-001, WG-TST-API-OAS-001 |
| `WG-API-002` | API surface inventory + OpenAPI | CERT-API-001 | WG-TST-API-GOV-001, WG-TST-API-OAS-001 |
| `WG-EVT-001` | Workspace event ownership | CERT-EVT-001 | WG-TST-EVT-WSP-001 |
| `WG-EVT-002` | Event delivery/outbox contract | CERT-EVT-001 | WG-TST-REL-EVT-001 |
| `WG-REL-002` | Provisioning/outbox recovery | CERT-REL-002 | WG-TST-REL-PROV-001, WG-TST-REL-EVT-001 |

#### Milestone B security / observability / performance / cross-context

| Work unit | Name | Cert record | Tests |
|---|---|---|---|
| `WG-CONC-002` | Invitation concurrency | CERT-CONC-002 | WG-TST-INV-CONC-001 |
| `WG-CONC-003` | Governance mutation concurrency | CERT-CONC-003 | WG-TST-CONC-RPERM-001, WG-TST-CONC-POL-001, WG-TST-CONC-SHARE-001 |
| `WG-OBS-001` | Authorization observability | CERT-OBS-001 | WG-TST-OBS-INT-001, WG-TST-OBS-SEC-001 |
| `WG-OBS-002` | Governance observability | CERT-OBS-001 | WG-TST-OBS-INT-002, WG-TST-OBS-METRIC-001 |
| `WG-PERF-001` | Authorization hot path | CERT-PERF-001 | WG-TST-PERF-AUTHZ-001, WG-TST-PERF-MEM-001, WG-TST-PERF-PERM-001 |
| `WG-PERF-002` | List/cache performance | CERT-PERF-001 | WG-TST-PERF-CACHE-001, WG-TST-PERF-LIST-001 |
| `WG-X-001` | Identity & Accounts upstream | CERT-X-001 | WG-TST-UP-ARCH-001, WG-TST-UP-INT-001..003 |
| `WG-X-002` | WorkManagement downstream | CERT-X-002 | WG-TST-WM-CONTRACT-001..002, WG-TST-WM-P2-001..004 |
| `WG-X-003` | Documents/Collaboration boundaries | CERT-X-003 | WG-TST-DOC-X-001 |
| `WG-X-004` | Collaboration boundary | CERT-X-003 | WG-TST-COL-X-001 |
| `WG-X-005` | Billing boundary | CERT-X-004 | WG-TST-BILL-X-001 |
| `WG-X-006` | Automation/Integrations boundary | CERT-X-005 | WG-TST-INTG-X-001, WG-TST-AUTO-X-001 |
| `WG-X-007` | Analytics boundary | CERT-X-006 | WG-TST-ANA-X-001 |
| `WG-CERT-HO-001` | P3 handoff packet | P2B-CERT-015, CERT-CI-001..009 | WG-TST-SYNC-HANDOFF-001, WG-TST-SYNC-EVIDENCE-001 |
| `WG-GATE-002` | Milestone B gate decision | P2B-CERT-001..016 | all P2B tests |

### 5.5 Milestone C secondary units — real layer status (not executing now)

| Work unit | Name | Cert record | Current codebase layer (checked at candidate) |
|---|---|---|---|
| `WG-INVITE-001` | Invitation lifecycle | FULL-CERT-INV-001 | FULL (Domain+App+API+tests) |
| `WG-INVITE-002` | Invitation token/expiry/replay | FULL-CERT-INV-001 | FULL |
| `WG-INVITE-003` | Invitation security/hash | FULL-CERT-INV-001 | FULL |
| `WG-PROV-001` | Provisioning consumer | FULL-CERT-PROV-001 | FULL (Infrastructure consumer + tests) |
| `WG-SET-001` | Workspace settings | FULL-CERT-SET-001 | FULL (App+API) |
| `WG-HOME-001` | WorkspaceHome | FULL-CERT-HOME-001 | DOMAIN_PRESENT / partial |
| `WG-TEAM-001` | Teams | FULL-CERT-TEAM-001 | FULL |
| `WG-SPACE-001` | Spaces | FULL-CERT-SPACE-001 | FULL |
| `WG-RULE-001` | Workspace Rules boundary | FULL-CERT-RULE-001 | DOMAIN_PRESENT + arch tests |
| `WG-CROLE-001` | CustomRole | FULL-CERT-CROLE-001 | DOMAIN_PRESENT only (no App/API) |
| `WG-POL-001` | WorkspacePolicy | FULL-CERT-POL-001 | DOMAIN_PRESENT only (no runtime composition) |
| `WG-RPERM-002` | ResourcePermission full lifecycle | FULL-CERT-RPERM-001 | PARTIAL (grant/revoke/ceiling present) |
| `WG-SHARE-001` | ShareLink lifecycle | FULL-CERT-SHARE-001 | PARTIAL (create/disable only) |
| `WG-SHARE-002` | ShareLink capability-token consumption | FULL-CERT-SHARE-001 | BLOCKED (no consume path) |
| `WG-TPL-001` | PermissionTemplate | FULL-CERT-TPL-001 | DOMAIN_PRESENT only |
| `WG-AUD-001` | Audit ownership | FULL-CERT-AUD-001 | BLOCKED (no SecurityEvent/Audit mechanism) |
| `WG-SEC-EVT-001` | Security-event product mechanism | FULL-CERT-AUD-001 | BLOCKED (no SecurityEvent) |

## 6. Phase 0 — Baseline

- Candidate: `e74bbc75f3713d99cead9b535cc935f211a7c47e` (develop).
- Tree: `d71809d04310e35d30483539a6396c1c859cabef`.
- Migration: single `20260702093805_SchemaBaseline`; migration smoke expects 148 tables.
- Unrelated worktree files: `docs/workstreams/completion-assessment.md`, `tmp-tac-m12-record.md` (neither is part of this plan; do not commit the tmp record).
- Preparation-time CI (from cert): HEAD PR run skipped Backend CI; push run had Backend CI green but aggregate failed. Neither is candidate evidence.

## 7. Phase 1 — Milestone A (producer contract)

Executes and records `P2A-CERT-001..004` → gate `WG-GATE-001`.

Evidence packet (all exist at candidate — confirmed):

- `Notrelix.Domain.Tests/Workspaces/Workspaces/WorkspaceTests.cs`
- `Notrelix.Domain.Tests/Workspaces/WorkspaceTenantOwnershipTests.cs`
- `Notrelix.Application.Tests/Features/Workspaces/Workspaces/...` pipeline auth tests
- `Notrelix.Integration.Tests/Workspaces/WorkspaceCreationPipelineAuthorizationTests.cs`
- `Notrelix.Integration.Tests/Governance/PostgresAccessFactsProviderTests.cs`
- `Notrelix.Architecture.Tests/Authorization/AuthPipelineArchitectureTests.cs`
- `Notrelix.Architecture.Tests/Authorization/UseCaseSecurityClassificationTests.cs`
- `Notrelix.Architecture.Tests/DataAccess/DbContextBoundaryArchitectureTests.cs`

Exit: `P2A-CERT-001..004 = VERIFIED or STABLE`; `WG-DEBT-006/007` named for P3-A.

## 8. Phase 2 — Milestone B blocker closure (priority: revocation first)

### 8.1 WG-MEM-004 / WG-SEC-002 — suspend/remove → grant revocation (WG-TEST-GAP-003, WG-DEBT-008)

Mechanism exists at candidate (verified): `AccessGrantProjectionService.RevokeWorkspaceMemberGrantAsync`
wired from `RemoveMember` and `SuspendMember` handlers. Missing proof is a runtime
test under the RLS app role. Add integration test(s) proving:

1. suspend member → access grant revoked → member row no longer visible under app role with stale grant;
2. remove member → access grant revoked → same;
3. role/member-state change → effective grant updated, not stale;
4. RLS context stays transaction-local after the mutation commits.

Evidence record: `P2B-CERT-003`, `CERT-DATA-002`.

### 8.2 WG-CONC-001 — membership + owner concurrency (WG-TEST-GAP-001/002, WG-DEBT-009/010)

- Duplicate-membership race: DB unique index `idx_workspace_members_workspace_user`
  exists (`WorkspaceMemberConfiguration.cs:25`). Add a DB-level concurrent
  add/accept test proving one succeeds and the other fails without duplicate rows.
- Last-owner race: add concurrent demote/remove-owner test proving the workspace
  can never end with zero active owners.

Evidence record: `P2B-CERT-002`, `CERT-CONC-001`.

### 8.3 WG-WM-003 — Board cross-account negative gate (WG-TEST-GAP-005)

Add explicit negative: a Board owned by another account is not reachable for a
granted user (NotFoundException) even when a grant row exists elsewhere.

Evidence record: `P2B-CERT-011`.

### 8.4 WG-REL-001 — fail-closed fault injection (WG-TEST-GAP-007)

Prove authorization dependency failure (facts provider / policy evaluator throws)
yields deny/fail-closed, never allow.

Evidence record: `CERT-REL-001`.

### 8.5 WG-SEC-003 — secret telemetry scan (WG-TEST-GAP-008)

Run the secret-scan gate on the candidate and record result.

Evidence record: `P2B-CERT-014`.

Result: **PARTIAL**. The slice-owned runtime redaction property is proven by
`WorkspaceInvitationSecretTelemetryTests` (2/2, real PostgreSQL, real token issuer /
encryptor / event collector) and is disarm-regression proven. A repository
secret-scan gate PASS is **not** claimed: no source-tree gate exists in the
repository, and no scanner is available locally, so the image-scoped Trivy gate
remains Delivery/CI-owned and unrun. This is sufficient for the Milestone B
slice-owned security claim but not for a full security certification.

### 8.6 WG-MIG-004 — supported-upgrade fixture (WG-TEST-GAP-009)

Because the candidate has a single baseline migration with no prior supported
schema (148-table smoke covers clean path), assess and record `NOT_CHANGED` or a
compatibility fixture; must not invent a synthetic upgrade chain.

Evidence record: `P2B-CERT-013`, `CERT-MIG-001`.

## 9. Phase 3 — Milestone B full evidence (P2B-CERT-001..016)

Collect and record each record with:

```text
Candidate SHA: e74bbc75f3713d99cead9b535cc935f211a7c47e
Migration impact: ...
Architecture evidence: ...
Security evidence: ...
Integration evidence: ...
CI evidence: ...
Blocking debt: ...
Status: VERIFIED | STABLE | PARTIALLY_VERIFIED | BLOCKED | NOT_APPLICABLE
Reviewer: ...
Decision date: ...
```

Minimum downstream maturity per cert §21 gate table: STABLE/D5 for the protected
slice, VERIFIED/D4 minimum for built-in role (state which role surface is
excluded if D4).

All evidence is executed on the candidate SHA; focused → project suite → CI gate
progression. Every record references executed tests with non-zero counts.

## 10. Phase 4 — P3-B handoff (WG-CERT-HO-001)

Publish handoff packet containing:

```text
candidate SHA
Workspace contract
membership contract
built-in role baseline
ResourceKind/PermissionAction
AccessControlBehavior contract
AccessFacts fields used by WorkManagement
AccessPolicyEngine policy semantics
RLS assumptions
Board representative allow/deny/cross-tenant tests
migration/OpenAPI compatibility
known debt (WG-DEBT-006, WG-DEBT-007, and any remaining)
D5 invalidation rules
```

WorkManagement MAY depend on the named stable contracts; it MUST NOT depend on
`IGovernanceDbContext`, Governance EF entities, `AccessFactsQuery` SQL details,
private PermissionRule/ResourcePermission table layouts, `AccessPolicyEngine`
internals, or the Workspace private DbContext.

### 10.1 Phase 4 execution status

```text
WG-CERT-HO-001 = NOT PUBLISHED
Reason: Milestone B gate (WG-GATE-002) = NOT MET.
```

Phase 3 completed: `P2B-CERT-001..014` and `P2B-CERT-016` are recorded from executed
local evidence on candidate `e74bbc75f3713d99cead9b535cc935f211a7c47e`
(5149 tests, 0 failures across all seven backend projects, plus no-pending-model-changes
and `make docs-check` 6/6). All 31 `CERT-*` records now carry an honest disposition:
8 VERIFIED, 13 PARTIALLY_VERIFIED, 9 NOT_EVALUATED (all `CERT-CI-*`, blocked on CI),
1 migration record VERIFIED-clean-DB / NOT_APPLICABLE-upgrade.

Phase 2 blocker closure is complete. `WG-TEST-GAP-001/002/003/005/007` are CLOSED by executed
runtime evidence, and the matching semantic debts `WG-DEBT-008/009/010` are recorded as
CLOSED BY EXECUTED EVIDENCE. `WG-TEST-GAP-008` remains PARTIAL because no repository
secret-scan gate exists (§8.5) and `WG-TEST-GAP-009` remains NOT-CHANGED because the baseline
is greenfield (§8.6), both exactly as this plan already allows.

The tested code tree is now frozen as immutable candidate `4878dd0a`, verified on a clean
detached worktree at 5152/5152 local tests with 0 failures (the intermediate frozen candidate was
`34b51bfc`; `4878dd0a` added only the `PermissionActionInventoryTests` architecture test). Every
commit after it is docs-only and does not change the tested code tree, so `4878dd0a` is the SHA
an aggregate CI run must target.

Phase 4 is deliberately **not** published. The gate requires a CI candidate-gate PASS and
STABLE/D5 maturity; `P2B-CERT-015` is BLOCKED because no CI run exists for this SHA, and no
record can honestly be lifted to STABLE/D5 without it. Publishing a stable handoff now would
assert a downstream contract that the gate has not admitted. The packet becomes publishable
immediately after the aggregate required gate runs green on this candidate.

See the record-by-record verdicts in `workspace-governance.certification.md` (`# Milestone B
checklist — Protected Slice`, `# Current-source synchronization certification`, and each
`## CERT-*` block), the blocking reasons in `# Preparation-time source-debt register`, and the
CI ownership in `# CI certification`.

## 11. Validation & completion

- Focused: Domain/Application/Integration tests for touched units.
- Broad: project suites (Domain.Tests, Application.Tests, Infrastructure.Tests,
  Architecture.Tests, Integration.Tests, API.Tests) with recorded non-zero counts.
- Gates: architecture tests, Rls architecture/policy tests, migration smoke,
  API/OpenAPI, and CI exact-candidate gate (no skipped required jobs).

## 12. Defect register for this plan revision

| Item | Resolution |
|---|---|
| plan.md was byte-identical to spec.md | Replaced by this PLAN (this file) |
| `WG-CERT-HO-001` referenced but undefined | Defined in §10 |
| 98+ work units referenced but undefined | Defined in §5 |
| Milestone C units had no status | Recorded real layer status in §5.5 (not executing now) |