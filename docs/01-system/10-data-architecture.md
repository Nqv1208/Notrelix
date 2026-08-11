---
title: "Data Architecture"
document_class: handbook
normative: true
owner: data
maturity: FROZEN
conformance: CANONICAL
applies_to: data
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Data Architecture

## Source of truth vs projection

A table/index/cache/search document/read model is a source of truth only when the owning context defines its lifecycle/invariants. Projection/storage optimization does not transfer domain ownership.

## SYS-DATA-001 — Relational constraints protect durable invariants

Use database uniqueness/FK/check/concurrency/index/RLS constraints where they provide durable protection or performance. Application prechecks improve error quality but do not replace race-safe constraints.

## JSON/typed data

JSONB is acceptable for legitimately flexible/polymorphic configuration, not as a replacement for every queryable model. Typed configuration/value models require discriminator/schema-version strategy when persisted polymorphism must evolve.

## Work Management dynamic values

Board item flexible values may have a canonical flexible representation plus query-optimized typed index rows/projections. Do not force every dynamic field into a fixed column, and do not run large filter/report workloads solely by loading/parsing arbitrary JSON when an index model is required.

## Migration

Database migration is production code and participates in compatibility, rollback/forward recovery and staged deployment planning.
