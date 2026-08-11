---
title: "Release, Rollout and Feature Flags"
document_class: handbook
normative: true
owner: engineering-delivery
maturity: FROZEN
conformance: CANONICAL
applies_to: repository
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Release, Rollout and Feature Flags

## DLV-REL-101 — Rollout preserves compatibility at every deployed step

For schema/contract changes, identify order across database, backend producers/consumers, frontend versions and background workers. Prefer expand → migrate/backfill → switch → contract/removal.

## Feature flags

A temporary rollout flag declares owner, purpose, eligible scope, safe default, metrics/decision criteria and removal condition. Security/tenant authorization MUST be identical or stricter on both paths. A flag is not an excuse to keep two architectures indefinitely.

## Rollback vs forward recovery

Database/event changes can make binary rollback unsafe. Every material rollout states whether rollback is supported, which migrations are reversible and when forward-fix is the recovery strategy. Do not promise rollback generically.

## Exposure

Gradual rollout may use internal/test account, cohort, workspace/account percentage or entitlement as approved. Ensure cache/realtime keys and persisted data remain valid when a user crosses flag states.
