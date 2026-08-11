---
title: "Messaging and Data Pipeline Runbook"
document_class: handbook
normative: true
owner: operations
maturity: FROZEN
conformance: CANONICAL
applies_to: runtime
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Messaging and Data Pipeline Runbook

Use for outbox backlog, consumer retry/poison growth, ordering blockage, projection lag or duplicate side effects.

## First principle
Do not purge/replay blindly. Message identity, consumer dedup state, ordering cursor and external side-effect idempotency must be understood before action.

## Diagnose
Identify producer/event type/consumer, oldest age and growth rate; database/outbox dispatch state; retry reason distribution; poison identity; ordering key blockage; consumer deployment/config; RLS/tenant failures; external provider failure; dedup claims stuck/incomplete.

## Contain
Pause the specific failing consumer or producer path when replay would amplify damage. Leave unrelated consumers operating when safe. If one poison message blocks an ordered stream, quarantine/skip only through an approved semantic procedure that records why downstream order remains valid.

## Recover
Fix handler/provider/config/schema, deploy, replay bounded identity/range, observe dedup and ordering advancement, reconcile projections/external effects. Never alter consumer dedup records merely to force execution without proving prior side effects did or did not occur.

## Verify
Backlog returns to normal, oldest age falls, no retry loop, ordering cursors advance, projection counts/sample invariants reconcile and user-visible data converges.
