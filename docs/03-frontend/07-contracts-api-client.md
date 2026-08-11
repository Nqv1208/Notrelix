---
title: "Contracts and API Client"
document_class: handbook
normative: true
owner: frontend-architecture
maturity: FROZEN
conformance: CANONICAL
applies_to: frontend
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Contracts and API Client

Frontend/backend integration is contract-driven. Handwritten shapes that “look like” API payloads are compatibility debt.

## FE-CONTRACT-101 — Generated contract types are authoritative at transport boundary

OpenAPI/realtime contract artifacts produce or validate the transport types consumed by frontend adapters. A product package may map transport DTOs to capability-friendly types, but MUST NOT duplicate transport contracts by hand.

## FE-CONTRACT-102 — Mapping isolates transport evolution

Generated DTOs remain transport-facing. Product core/state may use adapters when naming, nullability, dates, discriminated unions or domain-friendly semantics differ. Mapping code must be explicit and tested for compatibility-sensitive transformations.

## FE-CONTRACT-103 — No untyped success path

Avoid `any`, unchecked `unknown` casts or silent fallback defaults at contract boundaries. Unexpected enum/discriminator/schema values are handled deliberately: supported forward-compatible fallback, typed unknown state, or fail-closed error as the contract requires.

## FE-CONTRACT-104 — Error contracts remain distinguishable

Authentication, authorization, validation, concurrency, rate-limit, not-found and transient failures must not be collapsed before the owning capability can choose recovery. User-facing copy may be generalized, but application behavior needs typed categories/codes.

## Client ownership

Foundation/platform may own base HTTP mechanics: base URL, credentials/session attachment, correlation/trace headers, serialization and generated client configuration. Product state owns product endpoint usage and cache consequences. Apps do not call arbitrary endpoints directly when a capability owner exists.

## Contract change workflow

1. change backend contract and artifact under the versioning policy;
2. run OpenAPI/realtime drift checks;
3. regenerate frontend client/types through the canonical generator;
4. adapt product mappings/state;
5. run consumer tests and compatibility checks;
6. update canonical semantics when behavior—not just shape—changed.

Generated files MUST NOT be edited manually. Generator input/template/version changes are the source change.
