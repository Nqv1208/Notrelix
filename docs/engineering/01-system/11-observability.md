---
title: "Observability"
document_class: handbook
normative: true
owner: operations
maturity: STABILIZING
conformance: CANONICAL
applies_to: system
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Observability

Observability should let operators answer: what failed, for which tenant/resource/request/message, at which boundary, and whether retry/recovery is safe—without exposing secrets.

## Correlation

Propagate approved request/correlation/event/message identity across API, Application, outbox/consumer and provider work where feasible.

## Tenant data

Tenant/resource IDs may be operationally useful but logs must respect data classification. Never log raw secrets/tokens/credentials or document/content payloads by default.

## Required signals

- API request/error/latency by route class;
- database pool/query/migration/RLS failures;
- outbox backlog age/count and consumer failure/retry/poison state;
- cache availability/error ratio;
- realtime connection/reconnect/subscription failure;
- external provider latency/errors;
- frontend fatal/startup/contract failures where telemetry exists.

## Alert quality

Alert on user/business impact or leading failure signals with an actionable runbook. “A metric changed” is not enough. Numeric thresholds remain organizational SLO decisions unless approved.


## Structured event model

Operational events should carry operation name, outcome category, correlation/trace identity and the minimum resource/scope identifiers needed to join a request to its background consequences. Consumer telemetry additionally records logical event identity, consumer name, attempt/retry classification and processing latency/age. Provider adapters record provider operation/category without logging credential or full request payload by default.

## Cardinality and privacy

Do not turn unbounded resource IDs, free-form error text or user content into metric labels. High-cardinality identities belong in logs/traces; metrics use bounded dimensions. Sampling must not make security/tenant failure invisible. Redaction is centralized/testable where possible rather than relying on every log call to remember secrets.

## Change impact

A new asynchronous workflow/provider dependency or critical user journey declares the signals that distinguish success, retryable failure, poison/permanent failure and backlog/staleness before being considered operationally ready.
