# Notrelix Work Management Migration Plan

This document outlines the architectural migration from the legacy `features/boards` module to the new enterprise-standard `features/work-management` module.

## 1. Why Rename to "Work Management"?
Notrelix is not just a Trello clone or a simple Kanban tool; it is an enterprise workspace operating system.
*   **Product Identity**: Kanban, Table, Calendar, Timeline, and Dashboards are simply **views** rendering the same underlying work data. They are not separate databases or isolated features.
*   **Domain Alignment**: Renaming the module to `work-management` aligns the frontend with the product identity defined in `AGENTS.md` and `GEMINI.md`, avoiding legacy terms like "cards" and "lists" in favor of "items", "fields", and "groups" in the future.

---

## 2. Phase 4B Status: Public Alias (Scaffolding)
In **Phase 4B**, we introduce the new namespace without moving any physical files to prevent breaking changes and ensure smooth transition.
*   **Implementation**: Physical files remain in `features/boards`.
*   **Public Alias**: `features/work-management/index.ts` is created as a gateway that explicitly re-exports all public APIs, hooks, types, and utils from `features/boards`.
*   **Directional Flow**: The import direction is strictly **one-way**:
    $$\text{app} \longrightarrow \text{features/work-management} \longrightarrow \text{features/boards}$$
    *Warning: Sibling feature `features/boards` must NOT import from `features/work-management` in this phase to prevent circular dependencies.*
*   **Guideline**: All new app-level code and page composition layouts must import from `@/features/work-management` instead of `@/features/boards`.

---

## 3. Phase 4C: Physical File Migration Plan
In **Phase 4C**, files will be physically moved from `features/boards` to `features/work-management`. To ensure zero-breakage, the files must be moved in a strict, dependency-aware order:

1.  **Types & Interfaces** (`types/`):
    *   Move `types/api-types.ts` and `types/index.ts`.
    *   Since types have no runtime imports, they are the safest to migrate first.
2.  **Validation Schemas** (`schemas/`):
    *   Move form schemas and validation rules, updating their type imports to point locally.
3.  **Utility Functions & Constants** (`utils/`):
    *   Move helper algorithms (such as `fractional-index.ts`).
4.  **Data Mappers & Models** (`utils/board-api-mappers.ts`):
    *   Move the DTO-to-UI mappers.
5.  **API Services** (`api/`):
    *   Move the 8 HTTP client service files.
6.  **Query Hooks** (`hooks/queries/`):
    *   Move data-fetching query hooks.
7.  **Mutation Hooks** (`hooks/mutations/`):
    *   Move mutation hooks (including optimistic updates).
8.  **Cache Updaters & Client State Hooks** (`hooks/state/`):
    *   Move TanStack Query cache invalidators and UI state hooks.
9.  **Business UI Components**:
    *   Move board/table components currently in `app` layer into `features/work-management/components`.

---

## 4. Post-Migration Compatibility Layer
Once all physical files have been successfully relocated to `features/work-management`:
*   `features/boards/index.ts` will be updated to become the compatibility layer.
*   It will re-export symbols from the new `features/work-management` public API, ensuring legacy code or third-party integrations (if any) continue to compile without immediate rewrite.
