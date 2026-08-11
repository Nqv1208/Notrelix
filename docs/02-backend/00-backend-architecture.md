---
title: "Backend Architecture"
document_class: constitution
normative: true
owner: backend
maturity: FROZEN
conformance: CANONICAL
applies_to: backend
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Backend Architecture

## Production projects

```text
Notrelix.Domain
Notrelix.Application
Notrelix.Infrastructure
Notrelix.Platform
Notrelix.API
```

The project model is frozen as the backend foundation. A new bounded context does **not** get a new .NET project by default.

## BE-ARCH-101 — Project boundaries are responsibility boundaries

- **Domain** owns business state validity.
- **Application** owns use cases, authorization/orchestration and ports.
- **Infrastructure** owns provider/persistence implementations.
- **Platform** owns reusable runtime mechanisms with no bounded-context policy.
- **API** owns HTTP/host composition.

A business capability is delivered vertically through these projects as required.

## BE-ARCH-102 — Bounded context is not project boundary

Contexts are semantic ownership boundaries inside the modular monolith. Keeping them inside the five projects enables one deployment/transaction boundary while preserving future extraction seams.

## BE-ARCH-103 — New production project requires ADR

A new project must solve a durable dependency/deployment/runtime ownership problem. “This module is large” or “another team owns it” is insufficient.

## Dependency proof

Architecture tests should reject prohibited project references and forbidden namespace/dependency leakage. Project files are current evidence; the canonical rule determines whether a new reference is allowed.
