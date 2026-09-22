---
document_id: ADR-007
document_type: architecture-decision
status: Accepted
owner: backend-architecture
applies_to:
  - backend
  - backend-application
  - application-pipeline
  - governance-context
evidence:
  - backend/src/Notrelix.Application/Features/Governance/Authorization/AccessPolicyEngine.cs
  - backend/src/Notrelix.Application/Common/Security/IAccessPolicyEvaluator.cs
  - backend/src/Notrelix.Application/Common/Behaviors/AccessControlBehavior.cs
  - backend/src/Notrelix.Application/DependencyInjection.cs
  - backend/tests/Notrelix.Architecture.Tests/Authorization/AuthPipelineArchitectureTests.cs
  - backend/tests/Notrelix.Architecture.Tests/LayerRules/CommonSemanticNoGrowthArchitectureTests.cs
review_on:
  - access-control-ownership-change
  - authorization-model-change
  - application-pipeline-behavior-set-change
---

# ADR-007: Governance-Owned Authorization Evaluator Behind a Neutral Common Pipeline Seam

## ID

`ADR-007`

## Status

`Accepted` — 2026-09-14 (backend architecture closure residual-debt remediation, Wave 3;
reviewer-approved thin-seam direction with locked guardrails).

## Context

`AccessPolicyEngine` lived in `Notrelix.Application/Common/Security`. It is the single canonical
policy evaluator required by BE-SEC-013, but its body encodes Governance business semantics:
`PermissionAction` handling, the role ladder (Owner/Admin/Member/Guest/Observer), resource-permission
rank ceilings (`ManagerRank`/`OwnerRank`), resource-kind lifecycle policy (board/page),
`ManagePagePermission`/`ArchivePage`/`ManageIntegrations`/`CreatePage`/`CreateComment` intended
policies, plus explicit allow/deny rule precedence. That made Common the owner of permission
semantics, which is the residual `SOURCE_DEBT` this wave closes ("Common owns pipeline mechanics
only; Governance owns permission semantics").

Constraints that must not be traded away:

- exactly ONE evaluator authority (BE-SEC-013 forbids competing authorization authorities);
- exactly ONE access-control stage in the frozen seven-behavior pipeline (ADR-006);
- no behavior regression for any protected operation;
- no new generic framework: the suite explicitly forbids growing this into a policy bus,
  registry, handler abstraction, or service locator.

## Decision

Relocate the single canonical evaluator to the Governance Application module and keep the
pipeline seam neutral:

```text
Notrelix.Application/Common (mechanics — unchanged)
    IAccessPolicyEvaluator   neutral decision seam (Evaluate(descriptor, context, facts, request))
    AccessFacts / AccessDecision / AccessPermissionRule
    RequestDescriptor / ExecutionContextSnapshot
    AccessControlBehavior    the one access-control pipeline stage
    IRequirePermission family  request execution markers (see Consequences re DEBT-COMMON-001)

Notrelix.Application/Features/Governance/Authorization (semantics — relocated)
    AccessPolicyEngine       the one IAccessPolicyEvaluator implementation
```

DI in `AddApplicationServices` binds `IAccessPolicyEvaluator → AccessPolicyEngine` as before
(singleton, same instance lifetime). The engine code is a pure move: no rule, ordering, or
message changed. Dependency direction is inward-safe: the Governance evaluator references only
Common mechanics types and `Domain.Governance` vocabulary; `Application/Common` references no
`Features.Governance.*` type.

## Guardrails (what this seam must never become)

Executable gates in `AuthPipelineArchitectureTests` pin these:

1. `EvaluatorSeam_HasExactlyOneImplementation` — a second `IAccessPolicyEvaluator`
   implementation fails the build gate; new authorization logic extends the one engine or the
   owning BC's facts, never a new evaluator.
2. `Common_MustNotDepend_On_GovernanceApplicationTypes` — no public Common type may expose a
   `Notrelix.Application.Features.Governance.*` type in its signature graph; Common may not
   import the concrete engine back.
3. `AccessControlBehavior_ReferencesOnlyTheSeam_NotTheConcreteEngine` — the pipeline stage
   depends on the interface only.
4. `AccessPolicyEngine_IsGovernanceOwned` — the evaluator lives in the Governance module; it
   may not return to Common.
5. The seam stays minimal and use-case-shaped. A generic `IPolicyEngine`, permission-rule
   registry, dynamic policy DSL, or service-locator indirection over this seam is a new
   architecture decision requiring a superseding ADR, not an implementation convenience.
6. `AuthorizationPolicyEngine_MustNotReadPersistence` (pre-existing, path updated) — the
   evaluator remains pure; datastore facts arrive only through `IAccessFactsProvider`.

## Rejected alternatives

- **Stop at classification only**: leaves the known ownership violation in place without
  remediating the boundary (reviewer-directed against; ownership was already unambiguous).
- **Bridge interface that copies Governance vocabulary into Common**: would relocate names
  without relocating ownership; explicitly forbidden by this wave's stop conditions.
- **Splitting the engine across BCs (billing tiers → Billing, page policy → Documents)**:
  would create multiple evaluators, violating BE-SEC-013's single authoritative path and
  ADR-006's single stage.
- **A new generic authorization framework**: violates the no-speculative-abstraction rule
  (NRX-006) and this decision's guardrail 5.

## Retained exception

`IRequirePermission -> PermissionAction` (`DEBT-COMMON-001`, baselined in TAC-GATE-022 /
`CommonSemanticNoGrowthArchitectureTests.KnownVocabularyDebt`) remains an explicitly approved
governed exception: the request marker must name the action the caller intends, and every
protected command across all BCs implements it. It may only be removed through a governed
permission-marker migration that changes all consumers atomically; this ADR neither reclassifies
it as clean nor permits growth.

## Consequences

- Permission semantics are now certified at their owner: Governance Application.
- Common shrinks toward mechanics only; the remaining vocabulary debt is exactly one
  baselined, documented exception.
- Authorization behavior is unchanged; existing characterization suites
  (`AccessPolicyEngineCharacterizationTests`, `GovernanceResourcePermissionFlowTests`,
  pipeline/behavior/API authorization tests) remain the behavioral authority and all run green.
- Canonical docs updated to point at the Governance-owned single evaluator
  (`security-tenancy-authorization.md` §11/§26, `application-model.md` §14/§15).
- ADR-006 `review_on: access-control-ownership-change` is exercised by this decision; the
  seven-behavior set and order are untouched.
