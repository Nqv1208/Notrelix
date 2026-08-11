---
title: "Environments and Configuration"
document_class: handbook
normative: true
owner: infrastructure
maturity: STABILIZING
conformance: CANONICAL
applies_to: infrastructure
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Environments and Configuration

Environments differ by credentials/endpoints/capacity/rollout policy, not by silent product semantics.

## INFRA-CFG-101 — Configuration is explicit, typed and validated

A required setting has owner, type/allowed values and failure behavior. Security/provider/database configuration that is invalid or missing fails startup or the affected capability explicitly; it does not fall back to a permissive default. Configuration binding/validation should happen near composition startup so failures are early and diagnosable.

## INFRA-CFG-102 — Secrets are not ordinary environment variables by convention alone

Use the deployment platform's approved secret channel and least-privilege runtime identity. Never commit production-like credentials, expose server secrets to Vite/Next public env prefixes, echo them in CI logs or inject them into generated frontend artifacts. Rotation must be possible without source changes.

## INFRA-CFG-103 — Environments have isolated identity/state

Production databases, queues/topics, storage buckets and provider credentials are not reused by lower environments unless an explicit security-controlled test process requires it. DNS/connection defaults must make accidental production access from local/test difficult. Copied production data requires approved sanitization/classification controls.

## Feature/config compatibility

A config rename/removal is a deployment contract change. During rolling deployment, old/new binaries may require overlapping config keys or coordinated rollout. Feature flags declare default/owner/removal and are evaluated with tenant/authorization-safe semantics.

## Local/staging parity

Lower environments should reproduce protocol semantics that matter—PostgreSQL/RLS, messaging/idempotency, object/provider boundaries—without pretending every provider needs identical scale. Mocks are useful for focused tests; integration/freeze evidence must exercise representative production graph where the protected property depends on it.

## Verification

Startup/config validation tests, secret scanning, environment deployment checks and infrastructure diff/review. Operational docs track truly external values such as numerical SLO/RPO/RTO rather than embedding fictional defaults.
