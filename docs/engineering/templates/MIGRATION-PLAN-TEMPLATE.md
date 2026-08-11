---
title: "Data and Contract Migration Plan Template"
document_class: template
normative: false
owner: architecture
maturity: FROZEN
conformance: CANONICAL
applies_to: repository
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Data and Contract Migration Plan Template

## Change
Old state/contract → target state/contract and reason.

## Compatibility matrix
Old binary/new binary vs old/expanded/target schema or producer/consumer versions.

## Phases
Expand → backfill/migrate → switch reads/writes → verify → contract/remove. Include deployment order.

## Backfill
Selection/range, batch size/cancellation, idempotency, resumability, tenant scope, progress metrics and failure retry.

## Safety
Locks/table rewrite, indexes/RLS/constraints, backup/restore assumptions, rollback vs forward recovery.

## Verification
Counts/invariants/checksums/sample reads, app behavior, queue/outbox effects, exact gate/test.

## Removal
Condition proving old column/endpoint/event path can be deleted.
