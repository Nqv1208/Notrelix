---
title: "Deployment Topology and Promotion"
document_class: handbook
normative: true
owner: infrastructure
maturity: STABILIZING
conformance: CANONICAL
applies_to: infrastructure
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Deployment Topology and Promotion

## INFRA-DEPLOY-101 — Promote immutable revisions

The source SHA/build artifact that passed required checks is the artifact promoted. Environment configuration is injected separately. Deployment records correlate backend/frontend artifact identity, database migration level and relevant feature/config state.

## INFRA-DEPLOY-102 — Mixed-version compatibility is planned

Rolling deployment means old/new application instances may coexist. Expand schema before new readers/writers require it; do not contract old columns/types until old instances/consumers are gone and verification proves migration. REST/realtime/events likewise respect supported old clients and background consumers.

## Process ordering

Deployment planning includes API instances, background consumers/jobs, migrations and frontend clients. A new producer event cannot be emitted before deployed consumers can tolerate it unless additive compatibility guarantees that. A new consumer cannot assume a producer field/version absent from old messages/backlog.

## Promotion path

Lower-environment evidence validates startup, migration, critical API/product flow, dependencies and contract/codegen. Production can use instance/cohort/workspace/feature-flag rollout as available. Stop conditions are explicit: elevated error/concurrency failure, migration issue, tenant/security anomaly or unrecoverable backlog growth.

## Rollback and forward recovery

Binary rollback is supported only while schema/event/external side effects remain compatible. After irreversible migration/provider side effects, forward fix or targeted repair may be safer. Release notes/change plan state this before rollout rather than deciding during incident.

## Health

Readiness removes instances that cannot safely serve required operations. Liveness prevents permanent stuck processes but should not create restart loops during dependency outage. Degradable dependencies are surfaced as capability health and follow runbook semantics.

## Proof

Infrastructure diff/review, clean artifact build, migration test, staging/smoke evidence and exact-SHA CI. Exact orchestrator/cloud commands are owned by `infra/` and deployment automation; this document remains provider-neutral unless a provider choice itself is architectural.
