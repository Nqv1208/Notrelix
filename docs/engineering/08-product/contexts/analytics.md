---
title: "Analytics Context"
document_class: constitution
normative: true
owner: analytics
maturity: STABILIZING
conformance: CANONICAL
applies_to: analytics
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Analytics Context

## Mission

Analytics owns read-oriented metrics, dashboards/widgets, snapshots/projections and semantic metric definitions. It turns events/queryable product data into insight without becoming the write-side source of truth for Work Management/Documents/Billing/etc.

## ANA-101 — Analytics is derived state

Dashboards/snapshots/projections may be rebuilt from authoritative sources/events according to retention/freshness limits. A user editing an analytics widget changes visualization configuration, not underlying product facts unless an explicit product command is invoked.

## ANA-102 — Metric definition is versioned business semantics

A metric defines name, scope, source facts, filters, aggregation, time basis/time zone, null/missing behavior and version. The frontend must not invent a different formula for the same named metric. Breaking formula changes create a new version/semantic migration where historical comparability matters.

## ANA-103 — Tenant scope is preserved through aggregation

Every projection/job/query carries authoritative account/workspace scope. Cross-tenant aggregation is allowed only for explicitly global/administrative analytics with privacy/authorization controls; workspace dashboards cannot leak another tenant through shared cache/materialized views.

## ANA-104 — Freshness is explicit

Realtime-ish counters, delayed projections and periodic snapshots have different consistency. API/UI expose or internally respect freshness/last-updated semantics where it affects interpretation. Do not pretend an async projection is transactionally current.

## Data architecture

Use typed/indexed/materialized data for recurring high-cardinality aggregation. Avoid scanning/deserializing all Board Item JSON for every dashboard. Analytics may consume Work Management materialized values/events, Collaboration activity, Automation execution, Billing usage, etc. through approved contracts.

## Authorization/privacy

Analytics queries apply resource/tenant authorization before returning grouped data; aggregation does not erase confidentiality. Small-group or sensitive identity analytics may require suppression/data-classification policy. Export follows Governance authorization/audit.

## Lifecycle

Dashboard/widget configuration has owner/scope/private/shared lifecycle. Snapshot retention follows product/privacy requirements. Deleting source product data defines whether historical aggregates are retained, recomputed/anonymized or purged according to approved retention—not guessed by a SQL cascade.

## Forbidden designs

- analytics table becoming editable business source;
- same metric calculated independently in several UI widgets;
- cache key without tenant/metric-version/time range;
- claiming synchronous accuracy from lagging async projection;
- per-request full scans of flexible JSON at enterprise scale;
- cross-tenant reporting without explicit admin/privacy authority.

## Tests/change impact

Test metric definitions/edge cases/time zone, projection idempotency/rebuild, tenant isolation, freshness, authorization and large-query strategy. Source event/schema changes require impact review for projection compatibility/backfill and historical metric meaning.
