---
title: "Background Jobs and Integrations"
document_class: handbook
normative: true
owner: backend-platform
maturity: STABILIZING
conformance: CANONICAL
applies_to: backend/runtime
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Background Jobs and Integrations

## Ownership

- Application defines business/process intent and ports.
- Infrastructure implements provider clients/persistence adapters.
- Platform/runtime hosts reusable scheduling/delivery mechanics.
- Product contexts own provider-independent business state.

## BE-JOB-101 — Background work carries actor/system and tenant scope explicitly

A job/consumer must know whether it runs as a user-triggered continuation, system-internal operation, account/workspace scoped process or global operation. Never default to empty tenant/user IDs to make APIs convenient.

## BE-JOB-102 — Job identity is stable under retry

The same logical operation must preserve operation/message identity so retries do not create duplicate provider effects.

## External provider calls

Define timeout, retry classification, idempotency/provider key, rate limit, credential source, telemetry and terminal-failure handling. Do not retry permanent 4xx/business rejection as transient infrastructure failure.

## Webhooks/inbound integrations

Authenticate/verify source, prevent replay where provider contract supports it, parse into stable Integration/Application contract, validate tenant/connection ownership, and process durable work idempotently.
