---
title: "Frontend Architecture"
document_class: handbook
normative: true
owner: frontend-architecture
maturity: FROZEN
conformance: CANONICAL
applies_to: frontend
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Frontend Architecture

The frontend is a product monorepo with three composition hosts—web, mobile and marketing—and package families that separate business capability, reusable application behavior, runtime adapters, UI and low-level mechanisms. Architecture is optimized for parallel feature development without letting app folders or generic shared packages become a second business layer.

## FE-ARCH-101 — Dependency direction is closed-world

**Rule.** A package may import only dependency classes explicitly admitted by the dependency rules. Deep imports, source-relative escape paths, tsconfig aliases or test-only shortcuts MUST NOT be used to bypass the public graph.

**Protected property.** A package's public dependency list remains an executable architectural contract; a future package split/refactor can reason from declared edges.

**Proof.** `tooling/dependency-rules` architecture check, package manifests, TypeScript build and lint.

## Responsibility layers

```text
Host / composition      apps/web | apps/mobile | apps/marketing
          ↓
Runtime adapters        packages/runtimes/*
          ↓
Application capability packages/features/*
Product capability      packages/product/<capability>/{core,state,web,mobile,...}
          ↓
Foundation mechanisms  packages/foundation/*
UI foundation           packages/ui/*
Tooling / certification tooling/*
```

This diagram expresses responsibility, not a permission for every upper layer to import every lower layer. The executable dependency manifest is authoritative for exact edges.

## FE-ARCH-102 — Business ownership follows capability, not framework

Work-management behavior belongs to the Work Management product capability even when rendered by React or persisted in TanStack Query. Documents behavior belongs to Documents. Cross-product workflows belong to a feature package only when they genuinely coordinate multiple product capabilities or application-level concerns.

**Forbidden.** `apps/web/features/*` becoming the real business layer; `ui/*` importing product packages; `foundation/*` depending on product concepts; a generic `shared` package accumulating arbitrary code.

## FE-ARCH-103 — Host-specific effects stay at host/runtime boundaries

Browser globals, Expo/React Native APIs, storage engines, native modules, URL/navigation integration and host lifecycle hooks stay in a host or runtime adapter. Framework-neutral core packages consume explicit ports/contracts.

## FE-ARCH-104 — Public exports are architectural API

Each package exposes intended surfaces through its package exports/index. Consumers MUST NOT deep-import internal source because an export is inconvenient. A needed cross-package symbol either becomes an intentional public API, moves to its true owner, or the design is revised.

## Change test

Before adding a package or dependency, answer:

1. Which capability or mechanism owns the behavior?
2. Is the behavior host-neutral or host-specific?
3. Is it server-state, client-state, UI or orchestration?
4. Does the dependency preserve the closed graph?
5. Can web/mobile consume the same semantic core without importing each other's host code?
6. What gate proves the new edge remains valid?

A team boundary, ticket boundary or temporary implementation convenience is not sufficient reason for a package boundary.
