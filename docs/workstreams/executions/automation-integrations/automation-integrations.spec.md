---
document_id: WRK-SPEC-AUTOMATION-INTEGRATIONS
document_type: workstream-specification
status: active
owner: automation-integrations-team
applies_to: [backend, automation, integrations, p5, rules, executions, connections, webhooks, provider-adapters]
evidence:
  - docs/product/automation.md
  - docs/product/integrations.md
  - docs/workstreams/backend-roadmap.md
  - docs/workstreams/teams/automation-integrations.md
  - docs/workstreams/cross-team-dependencies.md
  - backend/docs/architecture/application-model.md
  - backend/docs/architecture/platform-and-messaging.md
  - backend/docs/architecture/security-tenancy-authorization.md
  - docs/delivery/contract-first-delivery.md
review_on: [p5-scope-change, trigger-contract-change, execution-model-change, integration-provider-change]
---

# SPEC — P5 Automation & Integrations

## 1. Objective

P5 consumes already-owned business facts and executes bounded automated/integration behavior without moving source ownership into Automation or Integrations.

Canonical direction:

```text
source-owned business fact
→ stable source event/contract
→ Automation trigger/rule
→ Automation execution/action
→ Integration operation contract when external provider is required
→ provider adapter
```

## 2. Incremental entry rule

A trigger may begin when its specific producer contract is D4+.

P5 does not wait for every product context to finish.

Examples:

- WorkManagement event D4+ enables corresponding WorkManagement trigger;
- Documents event D4+ enables corresponding document trigger.

## 3. Platform prerequisites

Before hardening:

| Contract | Required |
|---|---|
| messaging delivery | D5 |
| message identity | D5 |
| idempotency | D5 |
| ordered delivery where required | D4+ |
| poison handling | D4+ |
| observability | D4+ |
| secret mechanism | D5 for credentials |

## 4. Automation rule

Automation Rule has stable identity, explicit valid configuration and lifecycle.

Required:

- draft/executable distinction where applicable;
- explicit version/revision snapshot;
- enable/disable semantics;
- typed trigger/condition/action configuration;
- no arbitrary executable code by default;
- bounded recursion/causation behavior.

## 5. Trigger

Trigger identity is stable and contract-driven.

A source transaction must not execute Automation side effects inline.

One logical trigger occurrence creates at most one logical Automation Execution.

## 6. Condition

Condition evaluation is deterministic at a documented consistency point.

Missing/null semantics are explicit.

Condition failure is not equivalent to execution infrastructure failure.

## 7. Action

Actions invoke normal capability contracts.

Automation must not directly modify another bounded context's persistence.

External provider actions route through Integrations.

## 8. Execution

Execution has stable logical identity independent of worker attempt.

Lifecycle must distinguish at least:

- queued;
- running;
- succeeded;
- failed;
- cancelled/aborted where supported;
- retryable state;
- unknown/pending external outcome where applicable.

History is product evidence, not broker log.

## 9. Retry/idempotency

Retry never means blindly repeating side effects.

Each action class must define:

- logical operation identity;
- idempotency boundary;
- retryable failures;
- terminal failures;
- unknown outcome handling;
- reconciliation/compensation if required.

## 10. Authorization principal

Automation never silently escalates privilege.

Execution authority must define whether it uses:

- initiating user;
- captured user authority;
- service/system principal;
- delegated resource authority.

Permission revocation/time semantics must be explicit.

## 11. Integration connection

Integrations owns logical Connection identity, provider mapping, credential reference and lifecycle.

Provider account identity does not create Notrelix membership.

Provider consent does not bypass Governance.

## 12. Secret boundary

Reusable provider credentials are isolated behind secret mechanism/adapters.

Automation configuration stores references/configuration, not reusable provider secrets.

Secrets must not enter logs/events.

## 13. Webhooks

Inbound provider webhook must:

- authenticate/verify raw request as required;
- route tenant/connection from trusted mapping;
- never trust provider JSON alone for tenant routing;
- process idempotently;
- tolerate replay/out-of-order delivery;
- translate provider schema before invoking product behavior.

Raw webhook does not directly become Automation trigger.

## 14. Sync/reconciliation

Integration sync has explicit direction, source-of-truth and cursor semantics.

Cursor advances only after durable successful processing.

Unknown provider outcomes reconcile before unsafe retry.

## 15. Provider anti-corruption layer

Provider DTOs, enums, errors and rate-limit states are translated into Integration-owned semantics.

Provider limitations stay in Integration boundary.

## 16. Cross-context writes

Correct:

```text
Automation/Integration
→ target-context command/port
→ target context validates + authorizes + mutates
```

Forbidden:

```text
Automation/Integration
→ target private DbSet/table
```

## 17. Realtime/observability

Execution/sync progress may be realtime, but durable Automation/Integration state remains recoverable authority.

Correlation must connect source event, execution, action, provider operation and retry attempt without conflating their identities.

## 18. Exit gate

P5 capabilities are stable when the producer contract, message identity, execution idempotency, bounded authority, provider secret boundary and retry/reconciliation semantics are proven for each released trigger/action/integration.
