---
title: "Domain and Integration Event Model"
document_class: handbook
normative: true
owner: backend
maturity: FROZEN
conformance: CANONICAL
applies_to: events
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Domain and Integration Event Model

## Domain event

A Domain event represents a completed business fact emitted by a successful aggregate transition. It is internal to domain/application processing and may be mapped to other contracts.

## Integration event

An Integration event is a durable cross-boundary contract intended for independent consumers and at-least-once delivery.

## SYS-EVT-001 — Events describe facts, not commands

Use past-tense business semantics. Do not name an event after an imperative action or publish “requested” when the domain fact actually means “completed”, unless requested is itself the business fact.

## SYS-EVT-002 — Event emission follows state success

Rejected/no-op Domain mutations emit no success event. Event payload uses normalized committed business values and correct tenant/resource scope.

## SYS-EVT-003 — Durable event consumers assume duplication

Integration consumers are idempotent through the approved deduplication mechanism. Side effects cannot assume exactly-once broker delivery.

## Mapping

Domain event shape and integration/realtime event shape may differ. Application/event mappers adapt business facts into stable external payloads; do not make Domain depend on transport envelopes.
