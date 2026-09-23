---
document_id: WRK-TESTS-AUTOMATION-INTEGRATIONS
document_type: workstream-test-plan
status: active
owner: automation-integrations-team
applies_to: [backend, automation, integrations, p5, testing]
evidence:
  - docs/workstreams/executions/automation-integrations/automation-integrations.spec.md
  - docs/workstreams/executions/automation-integrations/automation-integrations.plan.md
review_on: [p5-requirement-change]
---

# TESTS — P5 Automation & Integrations

## Automation Rule/Trigger

- `AI-TST-RULE-001` invalid Rule cannot execute.
- `AI-TST-RULE-002` disabled Rule creates no new execution.
- `AI-TST-RULE-003` execution uses explicit Rule revision/snapshot.
- `AI-TST-TRG-001` one logical source occurrence maps to one logical trigger.
- `AI-TST-TRG-CON-001` producer event compatibility.
- `AI-TST-TRG-ORDER-001` duplicate/out-of-order source delivery handled.

## Conditions/Actions

- `AI-TST-COND-001` deterministic condition evaluation.
- `AI-TST-COND-002` null/missing behavior explicit.
- `AI-TST-ACT-001` action uses target capability contract.
- `AI-TST-ACT-ARCH-001` no target private persistence.
- `AI-TST-ACT-ARCH-002` Automation does not instantiate provider SDK.

## Execution

- `AI-TST-EXE-001` stable execution identity.
- `AI-TST-EXE-CONC-001` concurrent claim cannot duplicate logical execution.
- `AI-TST-EXE-IDEMP-001` retry does not duplicate committed action.
- `AI-TST-EXE-UNK-001` unknown external outcome is represented.
- `AI-TST-EXE-REC-001` recursive/self-trigger chain is bounded.
- `AI-TST-EXE-HIST-001` history survives worker-attempt changes.

## Authorization

- `AI-TST-AUTHZ-001` execution principal semantics enforced.
- `AI-TST-AUTHZ-002` permission revocation behavior follows declared timing semantics.
- `AI-TST-AUTHZ-003` service/system principal is least privilege.
- `AI-TST-AUTHZ-004` cross-tenant action denied.

## Integration Connection/Secrets

- `AI-TST-CONN-001` connection lifecycle.
- `AI-TST-CONN-AUTHZ-001` provider consent does not bypass Governance.
- `AI-TST-SEC-001` reusable secret absent from Domain/events/logs.
- `AI-TST-SEC-002` secret rotation preserves logical Connection identity.

## Outbound/provider

- `AI-TST-OUT-001` stable provider operation identity.
- `AI-TST-OUT-IDEMP-001` retry idempotency.
- `AI-TST-OUT-UNK-001` unknown outcome reconciled before unsafe retry.
- `AI-TST-PROV-001` provider DTO/error translated at adapter boundary.

## Webhook

- `AI-TST-WH-SEC-001` invalid authentication/signature rejected.
- `AI-TST-WH-ROUTE-001` tenant routed from trusted connection mapping.
- `AI-TST-WH-IDEMP-001` duplicate delivery processed once.
- `AI-TST-WH-ORDER-001` out-of-order delivery does not regress product state.
- `AI-TST-WH-AUT-001` raw webhook does not bypass product translation into Automation.

## Sync/reconciliation

- `AI-TST-SYNC-001` explicit sync direction/source-of-truth.
- `AI-TST-SYNC-CUR-001` cursor advances after durable success only.
- `AI-TST-SYNC-BF-001` backfill does not duplicate live processing.
- `AI-TST-SYNC-DISC-001` disconnect handles queued work explicitly.

## Platform/reliability

- `AI-TST-MSG-001` message identity stable across retries.
- `AI-TST-POISON-001` poison path is diagnosable/recoverable.
- `AI-TST-OBS-001` correlation links source/execution/action/provider attempt without identity conflation.
- `AI-TST-RT-001` progress can recover from durable state.

## Certification

Each released trigger/action/provider path requires its specific producer contract evidence; one stable source does not certify unrelated triggers.
