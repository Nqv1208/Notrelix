---
title: "System Overview"
document_class: constitution
normative: true
owner: architecture
maturity: FROZEN
conformance: CANONICAL
applies_to: system
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# System Overview

Notrelix is a multi-tenant enterprise work-management platform implemented as a backend modular monolith and a multi-host frontend monorepo. The system is deliberately optimized for **modular extraction later without premature distributed-system cost now**.

## Architectural shape

```text
Clients
  ├─ Web app (Vite/React)
  ├─ Mobile app (Expo/React Native)
  └─ Marketing/public surfaces
        │
        ▼
Versioned REST + realtime contract boundary
        │
        ▼
API host
  → Application use cases / authorization / orchestration
      → Domain invariants and business transitions
      → Infrastructure persistence/providers
      → Platform reliable runtime mechanisms
        │
        ▼
PostgreSQL / Redis / storage / messaging / providers
```

## SYS-ARCH-001 — Modular monolith is the deployment default

**Rule.** Bounded contexts remain explicit inside one backend deployment until an independently valuable operational boundary justifies service extraction.

**Why.** Network boundaries add distributed consistency, deployment, observability and ownership costs. Source-folder symmetry alone does not justify them.

**Extraction readiness.** A bounded context should already own its domain state, Application contracts, integration events and persistence access patterns so extraction is a deployment change rather than a semantic rewrite.

## SYS-ARCH-002 — Business capabilities are vertically owned

A capability may touch Domain/Application/Infrastructure/API/Platform and one or more frontend packages. Those layers are technical responsibility boundaries, not separate product ownership silos.

## SYS-ARCH-003 — Cross-stack communication uses contracts

Backend implementation types and frontend implementation types are not shared directly. The stable boundary is contract artifacts plus versioning/compatibility rules.

## Failure domains

Think explicitly about five failure classes:

1. authorization/tenant-boundary failure;
2. transactional/persistence conflict;
3. asynchronous delivery/retry/duplication;
4. contract/version mismatch;
5. client stale-state/realtime ordering.

Every cross-stack feature should identify which classes apply before implementation.
