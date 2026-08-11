---
title: "Configuration and Secrets Contract"
document_class: handbook
normative: true
owner: engineering-quality
maturity: FROZEN
conformance: CANONICAL
applies_to: repository
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Configuration and Secrets Contract

Configuration expresses environment/runtime policy without changing product semantics silently.

## QLT-CFG-101 — Configuration has owner, type and default semantics

Every non-trivial setting documents who reads it, allowed values, startup/runtime behavior and failure mode. Invalid security/tenant/provider configuration fails startup or operation explicitly rather than falling back to unsafe behavior.

## QLT-CFG-102 — Secrets are separate from ordinary config

Secret values are never committed, exposed in frontend bundles, copied to logs or written into documentation examples as realistic credentials. Use environment/secret manager injection appropriate to deployment.

## QLT-CFG-103 — Feature flags do not become permanent architecture forks

A flag declares owner, rollout purpose, default by environment, telemetry/decision criteria and removal condition. Both paths preserve security/tenant invariants. Long-lived product entitlements are modeled as product policy, not random feature flags.

## QLT-CFG-104 — Config changes are deployable changes

Breaking config renames/removals require compatibility/migration plan. Startup validation should detect missing required values before serving traffic.
