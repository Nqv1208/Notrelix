---
title: "Contract Versioning and Compatibility"
document_class: handbook
normative: true
owner: architecture
maturity: FROZEN
conformance: CANONICAL
applies_to: contracts
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Contract Versioning and Compatibility

## Compatibility classes

- **additive:** old consumers continue to work without semantic reinterpretation;
- **behavioral-compatible:** shape stable but semantics clarified/tightened within prior contract;
- **breaking:** old consumer may fail or behave incorrectly;
- **new version:** intentionally parallel contract identity while migration occurs.

## SYS-VER-001 — Semantic break counts as breaking

Keeping the same JSON fields while changing meaning, scope, enum semantics, ordering or lifecycle can be a breaking change. Compatibility is behavioral, not only structural.

## SYS-VER-002 — Rollout order is designed

For independently deployed producer/consumer, define which side can roll first and what mixed-version window is safe.

## Integration/realtime events

Stable logical names are not renamed for CLR style. Breaking payload/semantic changes require version strategy and backlog/replay consideration.

## REST

Prefer additive response fields and tolerant readers. Requests are stricter: adding required input is breaking unless default/compatibility is explicitly defined.

## Persistence

Schema compatibility is a deployment contract during rolling/staged release. Prefer expand → migrate/backfill → switch reads/writes → contract/remove.
