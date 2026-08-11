---
title: "Change Impact and Migration Analysis"
document_class: handbook
normative: true
owner: engineering-delivery
maturity: FROZEN
conformance: CANONICAL
applies_to: repository
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Change Impact and Migration Analysis

## DLV-CHG-101 — Analyze consumers before changing a contract

Inventory source callers, public APIs, events, persistence, generated clients, caches/projections, background jobs, tests and operational dashboards affected by a semantic change. Search is required; folder proximity is not enough.

## Change classes

- local implementation: no external semantic contract changed;
- additive compatible contract;
- behavioral compatible change requiring consumers/tests;
- breaking contract/data/schema change;
- architecture boundary change;
- security/tenant policy change.

## Required migration questions

1. Which old data/contracts remain in production?
2. Can old and new code run concurrently during rollout?
3. Is backfill required and bounded/idempotent?
4. What happens on partial failure?
5. What proves completion before old path removal?
6. Is rollback safe or is forward recovery required?
7. Which docs/ADR/rules must change?

A rename across persisted/event/public identity is not “refactor-only”.
