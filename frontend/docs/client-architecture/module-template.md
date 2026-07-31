# Feature Module Structural Template

> **Standard Package Layout for Feature and Product Modules**

---

## 1. Feature Module Layout

All feature packages under `packages/features/*` follow a unified structural layout:

```text
packages/features/<module-name>/
  ├── src/
  │    ├── core/        (Pure domain models, value objects, invariants)
  │    ├── data/        (DTO mappers, repositories, query options)
  │    ├── react/       (React hooks, context providers, controllers)
  │    ├── web/         (Web UI components, forms, dialogs)
  │    └── testing/     (Fake repositories, test data builders)
  ├── package.json
  └── tsconfig.json
```

---

## 2. Layer Responsibilities

- **`core/`:** No dependencies on React, TanStack Query, DOM, or UI tokens. Pure TypeScript functions and type definitions only.
- **`data/`:** Defines repositories accepting `NotrelixClient` in factory functions. Converts DTOs to pure domain models.
- **`react/`:** Provides custom hooks (`useWorkspaceMembers()`) and local state controllers.
- **`web/`:** Presentational and container UI components built with `@notrelix/ui-web`.
- **`testing/`:** Builders and in-memory test doubles for unit and integration testing.
