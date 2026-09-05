# EXECUTION STATUS — Backend Boundary Execution V3

> Execution-time status record for `WRK-SPEC/PLAN/TESTS/CERT-BACKEND-BOUNDARIES-V3`.
> This is a working evidence record, not a competing canonical architecture owner.
> Authority remains with the four V3 workstream documents and `backend/AGENTS.md`.

---

## 1. Execution snapshot

```text
Candidate baseline SHA: ad068b740bdeae3c3be22f6835df57fd00df330d
Branch:                 architecture/backend-boundary-execution
Production diff:        none (tests-only enforcement bootstrap)
Files added:            backend/tests/Notrelix.Architecture.Tests/
                          Support/CrossContextBoundaryScanner.cs
                          DataAccess/CrossContextPersistenceBoundaryTests.cs        (ARCH-BC-001)
                          DataAccess/CrossContextCascadeArchitectureTests.cs        (ARCH-BC-004)
                          ApplicationLayer/CrossContextApplicationDependencyTests.cs (ARCH-BC-002/003)
                          ApplicationLayer/ApplicationTransportBoundaryTests.cs     (ARCH-BC-006)
                          Contracts/PublicSemanticContractArchitectureTests.cs      (ARCH-BC-005)
                          Events/IntegrationEventOwnershipArchitectureTests.cs      (ARCH-BC-007)
                          LayerRules/CommonEntitlementsAntiRegressionTests.cs       (ARCH-BC-008)
CI pin:                 .github/workflows/backend-ci.yml — 7 boundary gates added to
                        the architecture job's verify-required-tests-trx.py list
                        (FQNs verified against the produced TRX locally);
                        joins the existing backend CI lane per PLAN §120.
```

---

## 2. Milestone status (PLAN §4 master sequence)

| Milestone | Status | Evidence / activation condition |
|---|---|---|
| BND-M0 inventory | COMPLETE | 11 BCs inventoried; ownership/pipeline/persistence paths classified; this file + debt table |
| BND-M1 debt baseline | COMPLETE | 6 concrete entries (§4 below), R2 MIGRATE-ON-TOUCH, no wildcard |
| BND-M2 Wave 1 | COMPLETE | ARCH-BC-001/002/003 gates active, 4 precise baseline entries, self-tested |
| BND-M3 dependency spine | COMPLETE | No `IWorkspaceFacts` (pipeline owns workspace scope); governance seam hardened via gates; entitlement seam: zero live consumers, hotspot frozen |
| BND-M4 CreateBoardInWorkspace | COMPLETE | Source conforms — "no code change required" (CERT §127); 5 pipeline markers, owned persistence, zero foreign deps |
| BND-M5 Automation→Work mutation | **DEFERRED** | See §3. Do NOT treat as pass or NOT_APPLICABLE |
| BND-M6 events/refs | COMPLETE (applicable parts) | 42 outward events classified (38 PRODUCER_FACT, 4 AMBIGUOUS/MIGRATE-ON-TOUCH); EF model: zero cross-context navigation/cascade; ARCH-BC-007 ownership/smell gate active |
| BND-M7 rolling adoption | **ACTIVE NOW** | Protocol in §5 — applies to every new cross-context feature slice |
| BND-M8 Wave 2/3 | COMPLETE (004/005/006/007/008) | ARCH-BC-009 = NOT_APPLICABLE (no catalog admission need; TESTS §56) |
| BND-M9 extraction | NOT_STARTED | Conditional — requires extraction proposal + accepted ADR (CERT §75) |

**Global completion criteria (PLAN §123) are NOT fully satisfied**: the
`BND-M5 target-owned mutation slice proven` criterion remains open until the
first real Automation→Work mutation slice is executed under M7.

---

## 3. BND-M5 — DEFERRED (not NOT_APPLICABLE, not passed)

```text
Reason:
  No production Automation→Work mutation exists at candidate source.
  Automation/Engine action execution does not call WorkManagement yet; no
  IWorkActionPort, no foreign persistence, no internal MediatR dispatch.
  Do not invent a feature solely to satisfy architecture execution (PLAN §49).

Why DEFERRED, not NOT_APPLICABLE:
  Automation Domain already owns experimental action types
  (AutomationActionType.UpdateItem / CreateItem / MoveItem) and the workstream
  identifies Automation action execution as conceptually mutating
  WorkManagement items (SPEC §39, BOUND-CMD-*). The capability is designed
  but not yet due — the slice activates with real feature work, not with
  speculative architecture scaffolding.

Activation condition:
  When AUT action execution introduces a real WorkManagement mutation,
  execute BND-M5 exactly as planned (PLAN §48-59): Automation-owned
  IWorkActionPort → WorkManagement Public action → in-process adapter →
  producer mutation authority + idempotency identity + certification matrix
  (CERT §45-53).
```

---

## 4. Debt baseline (BND-M1) — frozen at ad068b74

| DebtId | RuleId | Risk | Consumer→Producer | Source | Classification |
|---|---|---|---|---|---|
| BND-DEBT-001 | ARCH-BC-001 / BOUND-DATA-002 | R2 | Common→Identity | `Common/Tenancy/IActorLookupService.cs`, `Common/Security/Auth/AuthSessionIssuer.cs` (inject `IIdentityDbContext`) | MIGRATE-ON-TOUCH |
| BND-DEBT-002 | ARCH-BC-002/003, BOUND-DOMAIN-001 | R2 | Workspaces→Accounts/Identity | `Features/Workspaces/.../AcceptInvitation.cs` (`Domain.Accounts` enum + foreign Abstractions ports) | MIGRATE-ON-TOUCH |
| BND-DEBT-003 | ARCH-BC-008 / BOUND-DOMAIN-001 | R2 | Common→Accounts | `Common/Tenancy/IAccessGrantProjectionService.cs` (exposes `AccountRole`) | MIGRATE-ON-TOUCH |
| BND-DEBT-004 | BOUND-COMMON-002 / ARCH-BC-008 | R2 | Common→Billing | `Common/Entitlements/*` tier vocabulary; `IEntitlementChecker`/`FeatureCode` zero live consumers | MIGRATE-ON-TOUCH |
| BND-DEBT-005 | ARCH-BC-003 (extended scope) | R2 | Identity→Accounts | 2 files inject `Features.Accounts.Provisioning` (service-namespace injection — beyond Wave-1 forbidden set) | MIGRATE-ON-TOUCH |
| BND-DEBT-006 | GlobalUsings | R2 | all→all | `Application/GlobalUsings.cs` global imports flatten context boundaries | MIGRATE-ON-TOUCH |

Machine baselines (precise, non-wildcard) live in the gate test files:
- `CrossContextApplicationDependencyTests` — 1 source entry + 3 signature entries (AcceptInvitation)
- `CommonEntitlementsAntiRegressionTests` — frozen hotspot type set (4 types)
- `IntegrationEventOwnershipArchitectureTests` — 1 reviewed consumer-coupled event entry

Growth policy: baseline may shrink only with violation removal + passing gates;
growth requires reviewed change (TESTS §20).

---

## 5. BND-M7 — rolling adoption protocol (starts now)

Every material cross-context feature slice, before coding:

1. Resolve the Use-Case Boundary Card (PLAN §34 / CERT §64): OwningBC,
   WorkflowOwner, MutationAuthorities, foreign semantic needs, mechanism
   (pipeline-owned vs use-case-owned), Producer Public vs Consumer Port, ACL,
   adapter, transaction, failures, idempotency.
2. Never duplicate frozen-pipeline concerns into handlers (PLAN §24 matrix).
3. No speculative folders/ports/adapters (SPEC §43; certification fails
   empty scaffolding — CERT §93).
4. Migrate boundary debt directly on the touched path (PLAN §83).
5. Prove with applicable gates + tests (TESTS §97 per-mechanism matrix).
6. Merge gate: reviewer answers CERT §65 questions; cross-context PR is
   BOUNDARY-VERIFIED before merge (CERT §138).

When the first real Automation→Work mutation slice appears, run it as the
BND-M5 reference slice (§3 activation condition) — that closes the last open
PLAN §123 criterion.

---

## 6. Verification evidence (exact candidate)

```text
dotnet build backend.slnx                     → 17 projects, 0 errors, 0 warnings
dotnet test Notrelix.Architecture.Tests       → 446/446 pass (incl. 25 new gate/self-tests)
dotnet test Notrelix.Application.Tests        → 571/571 pass
verify-required-tests-trx.py (local TRX)      → required test execution PASS
                                                (all 16 pinned gates, incl. 7 boundary)
CI wiring: backend-ci.yml architecture job    → runs full Architecture.Tests
                                                project + verifies required names;
                                                no parallel CI universe (PLAN §120)
Not run: Integration.Tests (Testcontainers)   → not required: tests-only diff +
                                                CI-workflow pin only, no production
                                                or persistence change
```

Remaining known limitation (documented, accepted for Wave 1/2):
body-level usage of foreign Domain types that arrive only through project-wide
`GlobalUsings.cs` (BND-DEBT-006) is not machine-detected without a semantic
model; signature-level and explicit-import levels are enforced. Escalation
path: Roslyn semantic-model gate if drift is observed.
