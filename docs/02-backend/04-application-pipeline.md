---
title: "Application Pipeline Contract"
document_class: handbook
normative: true
owner: backend-application
maturity: FROZEN
conformance: CANONICAL
applies_to: backend/application
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Application Pipeline Contract

Pipeline behavior owns cross-cutting request execution policy. Handlers declare semantics through approved request contracts; they do not reimplement those concerns ad hoc.

## Request classification

Current source exposes request contracts around command/query, execution profile, security, scoping, gates, transactions, expected version, idempotency, cache and realtime. Use the canonical interfaces already in `Application/Common/Requests`; do not create duplicate markers in feature folders.

## BE-PIPE-101 — Fail closed on unsupported execution semantics

If a request declares concurrency/authorization/tenant/idempotency semantics and the pipeline cannot resolve the required resource/strategy, fail explicitly. Do not catch “unsupported” and silently skip protection.

### Expected version

For `IExpectedVersionRequest`-style contracts:

- non-positive expected version → validation failure;
- target resource missing → not-found;
- unsupported resource type/version lookup → explicit unsupported/configuration failure;
- mismatch → concurrency conflict;
- check MUST NOT silently no-op.

## BE-PIPE-102 — Commit is centralized

Ordinary transactional handlers do not call `SaveChangesAsync`. Transaction/commit behavior owns commit ordering so post-commit actions, outbox and realtime cannot run before durable state succeeds.

Expected conceptual order:

```text
validate/classify
→ authenticate/authorize/tenant gates
→ concurrency/idempotency preconditions
→ begin/participate transaction
→ handler/domain mutation
→ persist + outbox in transaction
→ commit
→ post-commit dispatch/realtime/cache invalidation as specified
```

Exact implementation may have nested behaviors, but externally observable ordering must preserve these properties.

## BE-PIPE-103 — Cross-cutting behavior is single-owned

Do not duplicate permission checks, transaction start, idempotency, expected-version verification or generic cache/realtime dispatch in each handler when pipeline contracts already own them.

## Validation boundary

Request validators handle syntactic/request-level shape: required, range, format, enum, cross-field consistency. They do not perform authorization, external provider I/O or replace aggregate transition invariants.

## Tests

Pipeline tests must prove order-sensitive behavior, fail-closed unsupported classification, no handler execution after failed gate, and post-commit effects not occurring after transaction failure.
