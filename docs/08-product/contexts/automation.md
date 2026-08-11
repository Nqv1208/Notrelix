---
title: "Automation Context"
document_class: constitution
normative: true
owner: automation
maturity: FROZEN
conformance: CANONICAL
applies_to: automation
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Automation Context

## Mission

Automation owns rule definitions, trigger/condition/action configuration, execution identity/state and scheduling intent. It reacts to durable product facts and invokes approved capability actions asynchronously. It does not become a backdoor around normal authorization/business invariants.

## AUT-101 — Rules cannot enable with invalid configuration

A rule declares owner/scope, trigger type/config, conditions, ordered/defined actions and lifecycle. Configuration is schema-validated by trigger/action type. Enabling validates all required references/compatibility; a disabled draft may preserve incomplete configuration only if the product explicitly permits it and execution cannot observe it.

## AUT-102 — Trigger identity is stable and contract-driven

Triggers subscribe to logical business/integration event identities, not CLR class names or database table changes. Event version compatibility is explicit. Replaying the same durable source event must resolve to the same automation execution identity.

## AUT-103 — Execution is asynchronous and durable

Canonical flow:

```text
completed product event
→ outbox/integration publication
→ automation matcher
→ condition evaluation
→ execution/action state
→ capability/provider action through owned Application port
```

Do not execute arbitrary automation side effects inline inside the original product aggregate transaction.

## AUT-104 — Automation execution is idempotent

At minimum identity combines rule and source event (plus action step where needed). Retry cannot create duplicate assignment/update/notification/webhook side effects. Provider calls use idempotency/correlation keys where supported and action state records attempt/outcome safely.

## AUT-105 — Actions use normal capability contracts

`item.update_field`, assignment, notification and webhook-style actions call the same semantic Application APIs/commands and respect tenant, resource existence, field validation, lifecycle and authorization/service-principal policy. Automation MUST NOT update Work Management/Document tables directly.

## AUT-106 — Recursive automation is bounded

Automation-generated events can retrigger rules only under explicit origin/depth/dedup policy. Prevent infinite loops such as Rule A updates status → Rule B updates status → Rule A. Depth/time/causation metadata and rule-specific suppression are reviewable.

## Conditions and time

Condition evaluation is deterministic from source payload plus explicitly loaded facts at a defined consistency point. Scheduled/time triggers use infrastructure clocks/scheduler; Domain stores schedule intent/time-zone semantics without calling ambient time/provider APIs.

## Authorization

Define execution principal: original actor, rule owner, workspace automation service principal or explicit policy. It cannot silently escalate beyond configured/granted permissions. Permission changes can cause previously valid automation to fail/disable; failures are visible and auditable.

## Lifecycle

Rules support draft/enabled/disabled/archived as approved; disabling stops new execution but does not erase historical execution evidence. Editing an enabled rule defines whether in-flight executions use captured rule version or current config—never ambiguous.

## Forbidden designs

- inline side effects in original HTTP request/aggregate transaction;
- direct cross-context persistence writes;
- retry without execution identity;
- arbitrary user-provided code execution unless a separately sandboxed/security-designed capability exists;
- permission-management actions added casually;
- infinite recursive triggers;
- provider secrets stored in rule config visible to clients.

## Tests/change impact

Test configuration validation, enable guards, trigger matching/version, condition determinism, duplicate source event, action retry/partial failure, recursion guard, authorization revocation and execution history. New triggers/actions require event/contract owner review plus frontend registry/editor and operational runbook/metrics update.
