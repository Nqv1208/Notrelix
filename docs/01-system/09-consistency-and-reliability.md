---
title: "Consistency and Reliability"
document_class: handbook
normative: true
owner: architecture
maturity: FROZEN
conformance: CANONICAL
applies_to: system
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Consistency and Reliability

Choose consistency boundary before implementation.

## Transactional consistency

An aggregate protects its own invariants in one business transaction. Multi-root same-database operations may be orchestrated transactionally when the use case truly requires atomicity and ownership remains clear.

## Eventual consistency

Cross-context/provider side effects should prefer outbox/durable asynchronous processing when atomic coupling is not required.

## SYS-REL-001 — Commit before irreversible external effect

Do not call an irreversible external provider inside a database transaction unless the specific protocol is designed for it. Persist intent/outbox first, commit, then deliver/retry.

## SYS-REL-002 — At-least-once means idempotent consumers

Retries and duplicate delivery are normal. Consumer identity and operation identity must be stable enough to deduplicate correctly.

## Ordering

Guarantee only the ordering the business contract needs—typically per aggregate/resource/stream—not global event-type ordering.

## Failure recovery

Every asynchronous mechanism defines:

```text
retryable vs terminal errors
max/backoff policy
poison/dead-letter identity
deduplication state
operational visibility
manual replay/recovery safety
```
