---
title: "REST API Conventions"
document_class: handbook
normative: true
owner: api
maturity: FROZEN
conformance: CANONICAL
applies_to: rest-api
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# REST API Conventions

## Use-case orientation

Endpoints expose product use cases, not database tables. CRUD-shaped operations are acceptable only when they faithfully represent the business lifecycle.

## SYS-API-001 — Stable resource identity

Routes identify the resource needed by the use case and authorization. Do not add multiple redundant IDs whose relationship is not validated.

## SYS-API-002 — Errors are typed and machine-usable

Validation, unauthorized/forbidden, not-found, conflict/concurrency, idempotency conflict and unexpected failure map to a stable error model. Do not leak exception stack/provider messages.

## SYS-API-003 — Pagination is bounded

Collection endpoints use approved bounded pagination/cursor semantics appropriate to ordering. Do not expose unbounded board/workspace scans for convenience.

## SYS-API-004 — Concurrency/idempotency are contract metadata

When a use case is concurrency-sensitive, expected version participates in the API/Application request contract and conflict maps intentionally. Retriable create/command operations use approved idempotency key semantics rather than client-generated duplicate detection hacks.

## Response shape

Return the projection needed by the use case. Avoid serializing broad EF/aggregate graphs. Include stable IDs and version/state metadata needed for subsequent safe mutations.

## OpenAPI

OpenAPI is generated/verified as a contract artifact. Drift between API implementation and committed/generated contract fails certification.
