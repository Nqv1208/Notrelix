# Notrelix Frontend Architecture Specification

> **Canonical Architecture Documentation for Notrelix Frontend Platform**  
> **Target Platform:** Web (`apps/web`)  
> **Technology Stack:** React 19, Vite, TanStack Router, TanStack Query v5, pnpm Workspace

---

## 1. Overview

Notrelix Frontend is organized as a monorepo containing application shells, shared packages, feature modules, and infrastructure tooling.

### Product Target Scope

- **Web Application (`apps/web`):** Primary production desktop and responsive web work-management OS built with Vite and TanStack Router.
- **Marketing Site (`apps/marketing`):** Next.js marketing site (has independent freeze lifecycle).
- **Mobile Client (`apps/mobile`):** React Native mobile client (EXCLUDED from `frontend-web-platform-v1.0.0` freeze certificate).

---

## 2. Canonical Architecture Map

```text
apps/web
  ├── composition      (AppRuntime, QueryClient, ApplicationServices, Router context)
  ├── router shell     (TanStack Router tree, guards, layout)
  └── routes           (Route components and search param schemas)

packages/features/*
  ├── core             (Pure TypeScript models & policies)
  ├── data             (DTO mappers, repositories, query options)
  ├── react            (Hooks, providers, controllers)
  └── web              (Web UI components)

packages/product/work-management
  ├── core             (Pure domain models & invariants)
  ├── state            (Data, query options, commands, realtime adapters)
  ├── plugins          (Extensible field & view plugins)
  └── web              (Board, Table, Kanban, Timeline UI)

packages/foundation
  ├── kernel           (Pure TypeScript utilities, result types, AppError)
  ├── contracts        (Generated REST/AsyncAPI contracts, NotrelixClient)
  ├── query            (Generic query primitives & optimistic engine)
  ├── realtime         (Protocol validator & transport state machine)
  └── observability    (Telemetry contracts & redaction)

packages/runtimes/web
  └── browser adapters (WebSocket factory, connectivity, session event bus, telemetry)
```

---

## 3. Core Architecture Invariants

1. **Composition Root:** Only `apps/web/src/composition` or `packages/runtimes/web` creates `NotrelixClient`, `QueryClient`, or `RealtimeClient`.
2. **Framework Purity:** `core` packages must NEVER import React, DOM, TanStack Query, or UI primitives.
3. **Data Boundary:** Components must interact with repositories and commands via `useApplicationServices()`, not direct HTTP calls.
4. **Realtime Isolation:** Components do not subscribe to raw WebSocket sockets directly; all events flow through registered `RealtimeModuleAdapter`s.
5. **Environment Access:** Direct `process.env` or `import.meta.env` reads are strictly restricted to host adapters.

---

## 4. Documentation Index

- [Dependency Model](file:///Users/nqvinh/Documents/projects/Notrelix/frontend/docs/client-architecture/dependency-model.md)
- [Application Composition](file:///Users/nqvinh/Documents/projects/Notrelix/frontend/docs/client-architecture/application-composition.md)
- [Module Template](file:///Users/nqvinh/Documents/projects/Notrelix/frontend/docs/client-architecture/module-template.md)
- [API and Contracts](file:///Users/nqvinh/Documents/projects/Notrelix/frontend/docs/client-architecture/api-and-contracts.md)
- [Query and Mutations](file:///Users/nqvinh/Documents/projects/Notrelix/frontend/docs/client-architecture/query-and-mutations.md)
- [Realtime Architecture](file:///Users/nqvinh/Documents/projects/Notrelix/frontend/docs/client-architecture/realtime.md)
- [Routing and Authorization](file:///Users/nqvinh/Documents/projects/Notrelix/frontend/docs/client-architecture/routing-and-authorization.md)
- [Testing Strategy](file:///Users/nqvinh/Documents/projects/Notrelix/frontend/docs/client-architecture/testing-strategy.md)
- [Freeze Governance](file:///Users/nqvinh/Documents/projects/Notrelix/frontend/docs/client-architecture/freeze-governance.md)
