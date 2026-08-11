---
title: "Definition of Done"
document_class: handbook
normative: true
owner: engineering-delivery
maturity: FROZEN
conformance: CANONICAL
applies_to: repository
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Definition of Done

A change is done when the intended behavior is implemented and its system contracts remain proven.

## DLV-DONE-101 — Completion is multi-surface

As applicable, completion includes:
- product/canonical semantics updated when behavior changed;
- implementation in the correct owner;
- authorization and tenant scope;
- schema migration/RLS/index changes;
- REST/realtime/event/generated artifacts;
- frontend cache/realtime/host consequences;
- behavior/integration/architecture tests;
- rollout/config/feature flag handling;
- observability/operational readiness for new failure modes;
- documentation/ADR/exception updates;
- required CI green on the exact revision.

## Not done

A change is not complete when it requires manual DB edits, deep imports, disabled gates, undocumented feature flags, stale generated contracts, a TODO for authorization, or a migration that only works on an empty database.

## Evidence report

Every completed change records what was tested and what was intentionally not applicable. Do not claim “all tests” when only focused tests ran.
