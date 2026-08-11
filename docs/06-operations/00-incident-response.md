---
title: "Incident Response"
document_class: handbook
normative: true
owner: operations
maturity: FROZEN
conformance: CANONICAL
applies_to: runtime
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Incident Response

## OPS-INC-101 — Stabilize service and tenant safety first

On detection: establish incident owner/communication channel, define affected capability/tenants/data risk, stop unsafe rollout/writes if necessary, preserve evidence and choose containment that does not violate tenant/security boundaries.

## Severity assessment

Assess customer impact, data integrity/confidentiality, scope, duration, workaround and propagation risk. Security/data-loss suspicion escalates independently of visible availability impact.

## Response loop

1. detect and timestamp;
2. identify last known-good/recent deployments/config/migrations;
3. contain blast radius (disable flag, stop consumer/job, scale/throttle, route read-only only if semantics support it);
4. collect correlation IDs, health/metrics/logs and dependency state;
5. form/test hypotheses one change at a time;
6. recover via rollback only when safe, otherwise forward-fix;
7. verify user-visible behavior, queue/backlog/data consistency and tenant isolation;
8. monitor recurrence;
9. record timeline/root cause/contributing controls and follow-up owners.

Never delete queues/data/log evidence to make dashboards green. Any destructive recovery requires explicit data-safety assessment.
