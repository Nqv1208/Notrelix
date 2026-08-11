---
title: "Backend Capability Playbook"
document_class: handbook
normative: true
owner: backend
maturity: FROZEN
conformance: CANONICAL
applies_to: backend
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Backend Capability Playbook

Use this playbook when adding a new use case or module inside an existing bounded context.

## Step 1 — Product contract

Document/confirm:

```text
owner context/module
actor and tenant scope
resource authorization
lifecycle/invariants
aggregate consistency boundary
external/cross-aggregate facts
success/no-op/rejection semantics
concurrency/idempotency
REST/realtime/event effects
```

If these are unresolved, do not jump to schema/endpoints.

## Step 2 — Domain

Add/extend aggregate/value/rule/event only when business semantics require it. Prove mutation atomicity/no-op/version/event behavior.

## Step 3 — Application

Create module-first command/query; declare request markers; add request validator; load external facts; authorize through pipeline/service; call Domain.

## Step 4 — Persistence/runtime

Add Application port only when needed. Infrastructure implements persistence/provider mapping. Add migration/RLS/index/constraint. Add outbox/consumer/runtime behavior for durable cross-context/provider side effects.

## Step 5 — API/contracts

Add use-case endpoint/transport contract. Regenerate OpenAPI/frontend contracts. Preserve error/concurrency/idempotency semantics.

## Step 6 — Tests/gates

Run focused Domain/Application/Infrastructure/API/Platform tests plus integration/architecture gates implied by the change. Verify required suites actually execute.

## Step 7 — Change impact

Update canonical product/mechanism docs only if semantics/rules changed; do not create a new local RULE/CONTEXT file for the module.
