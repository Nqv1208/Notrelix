---
title: "Infrastructure Overview"
document_class: handbook
normative: true
owner: infrastructure
maturity: STABILIZING
conformance: CANONICAL
applies_to: infrastructure
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Infrastructure Overview

Infrastructure turns the repository into deployable runtime without erasing semantic boundaries. Exact cloud/provider resources are executable evidence under `infra/`; this handbook owns cross-environment invariants that remain valid if providers or topology change.

## INFRA-101 — Deployment packaging does not redefine bounded contexts

A modular monolith can deploy several contexts in one backend binary/database. That does not permit cross-context table access or shared business models. Database schema ownership, event identities, queue consumers, tenant/RLS policies and public contracts remain explicit so later extraction is possible without first discovering hidden coupling.

## INFRA-102 — Build, config and state are separated

Application artifacts are immutable/reproducible. Environment-specific endpoints, credentials, capacities and flags are injected via approved config/secret channels. Durable state lives in managed persistence/storage dependencies, not container filesystem or process memory unless explicitly ephemeral.

## INFRA-103 — Least privilege follows process responsibility

Runtime identities receive only database/schema, queue/topic, storage and provider permissions needed by their process role. Background workers should not require unrestricted administrative credentials merely because they ship from the same solution. Deployment/migration credentials can be distinct from steady-state application credentials where supported.

## Dependency classes

PostgreSQL is authoritative relational persistence and RLS defense-in-depth. Redis/cache accelerates approved state but cannot become authorization truth. Messaging/outbox/worker mechanisms carry durable async work. Object storage and external providers are accessed through adapters with scoped credentials. Frontend assets/hosts are separately deployable consumers of backend contracts.

## Resilience boundary

Infrastructure can implement retry, connection pools, health checks, scaling and circuit/backoff mechanics only within application semantics. It cannot “fix” failure by disabling RLS, serving another tenant's cache entry, acknowledging a message before durable success or retrying a non-idempotent provider call without identity.

## Proof

Infrastructure-as-code validation, container/build pipeline, integration tests for database/RLS/messaging and deployment smoke checks demonstrate concrete realization. Canonical docs do not invent resource counts/regions that should come from IaC.
