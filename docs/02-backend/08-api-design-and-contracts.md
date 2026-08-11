---
title: "API Design and Contracts"
document_class: handbook
normative: true
owner: backend-api
maturity: FROZEN
conformance: CANONICAL
applies_to: backend/api
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# API Design and Contracts

API is a thin transport and composition boundary.

## BE-API-101 — Endpoint maps one use case

An endpoint parses route/query/header/body/auth transport facts, constructs one approved Application command/query, sends it, and maps the typed result/error. Business branching belongs in Application/Domain.

## Contracts

API request/response types are transport models. Do not expose EF entities or broad Domain aggregates. Stable API IDs/version fields are included when consumers need them for later safe mutation.

## Error mapping

Canonical classes include validation, authentication, authorization, not-found, concurrency/conflict, idempotency conflict, rate/entitlement gates and unexpected server failure. Never leak provider/database exception details.

## Idempotency

When an operation declares idempotency, the API accepts/normalizes the approved key and passes it into Application execution. Endpoint code does not implement its own “check existing row” duplicate protocol.

## OpenAPI/codegen

Change API contract → update source/OpenAPI → regenerate frontend contracts → review diff → run API/OpenAPI/codegen drift tests. Hand-editing generated client contracts is forbidden.
