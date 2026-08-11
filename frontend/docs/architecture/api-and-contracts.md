# API and Contracts

## Scope

Generated public contracts, API client construction, error/result mapping,
auth/session integration, DTO drift, versioning, idempotency and REST/realtime
relationship.

## Responsibility / Ownership

Frontend consumes backend-owned contracts through generated artifacts and
approved client/runtime boundaries.

## Current Architecture

Contract code is generated under the frontend foundation contract package and
checked by `pnpm codegen:check`.

## Normative Contracts

- Public DTOs come from generated contracts or explicit backend-owned types.
- API clients are constructed by runtime/foundation owners.
- Auth/session integration is host/runtime composition.
- Error/result mapping must preserve backend semantics.
- Version changes follow backend compatibility rules.
- Idempotency keys are supplied only for use cases that define them.
- Realtime messages complement REST/server state.
- No handwritten DTO drift.

## Allowed Design

Thin typed client wrappers and product-owned hooks over generated clients.

## Forbidden Design

No component-local hard-coded DTO clients, fake backend semantics in frontend
tests, or realtime-only state that cannot be reconciled with server truth.

## Failure Modes

Stale generated contracts, duplicated auth/session token behavior, inconsistent
error behavior.

## Change Impact Rules

Contract generation, client construction, auth/session mapping or error/result
semantics require codegen drift checks and affected product tests.

## Executable Evidence / Tests / Gates

`pnpm codegen:check`, contract package source and product/feature API tests.

## Related ADRs

`FE-ADR-005`.

## Related Source Manifests

Generated contract files and package manifests.

## Explicit Non-responsibilities

This document does not decide backend public API shape or server authorization.
