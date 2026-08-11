---
title: "Performance Engineering Contract"
document_class: handbook
normative: true
owner: engineering-quality
maturity: FROZEN
conformance: CANONICAL
applies_to: repository
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Performance Engineering Contract

Performance is a product and data-model property, not a late micro-optimization pass.

## QLT-PERF-101 — Avoid unbounded work on tenant-scale collections

List/search/filter/report endpoints and UI views use pagination/windowing/index-backed queries or bounded projections. Do not load an entire large Board/Document/activity history into memory merely because development data is small.

## QLT-PERF-102 — Optimize after ownership is correct

Caching, denormalization/materialized projections and local memoization must preserve tenant scope, invalidation semantics and authoritative ownership. Never bypass security/concurrency or duplicate business state solely for speed.

## QLT-PERF-103 — Query shape is reviewed with data shape

For hot queries inspect selected columns, joins/includes, indexes, sort/filter selectivity, N+1 behavior and cardinality. New JSON/polymorphic fields used for filtering at scale require an indexing/projection strategy rather than full scans.

## Frontend

Large lists use virtualization/windowing where required; bundle/chunk growth is reviewed for host impact; realtime handlers avoid fan-out invalidating the entire application; expensive renders are measured before adding memoization complexity.

## Evidence

Performance-critical changes include representative benchmark/query plan/load evidence proportional to risk. No universal latency number is invented here; SLOs are organization/runtime decisions captured under operations when approved.
