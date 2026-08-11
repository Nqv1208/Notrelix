---
title: "Backend Coding Standards"
document_class: handbook
normative: true
owner: backend
maturity: FROZEN
conformance: CANONICAL
applies_to: backend
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Backend Coding Standards

## Naming by responsibility

Use product language in Domain/Application modules. Avoid generic `Manager`, `Helper`, `Util`, `Processor`, `Service` unless the role is narrow and explicit.

Commands/queries describe use cases (`CreateBoardInWorkspace`, `GetFullBoard`) rather than table CRUD when lifecycle semantics are richer.

## Null/empty identity

`Guid.Empty` or equivalent is not a valid required business identity. Optional IDs distinguish absence from invalid empty value at boundaries.

## Collections

Protect aggregate/event/value-object collections from caller mutation; do not store an external mutable list reference directly.

## Exceptions/results

Use approved Domain/Application error/result taxonomy. Do not use generic exceptions to signal normal validation/authorization/conflict behavior, and do not expose provider exceptions through API.

## Async/I/O

Pass cancellation tokens through I/O boundaries. Do not make sync-over-async provider/database calls. Pure Domain methods are synchronous/deterministic unless an explicit design changes the boundary.

## Comments

Comments explain non-obvious invariant/compatibility rationale, not restate code. If an architectural exception requires a comment, also track it in governance.
