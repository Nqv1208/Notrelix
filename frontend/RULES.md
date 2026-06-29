# Notrelix Frontend — Development Rules & Checklist

> **Mindset:** Write predictable, clean, and bounded code.
> This document serves as a strict development contract for all developers and AI coding agents.

---

## 1. UI Placement Rules

Before creating a component, answer these questions in order:

*   **Does it know about business entities?** (e.g. references to `workspaceId`, `boardId`, `cardId`, `pageId`, `memberRole`, `notificationType`, or `permission` in its props or models).
    *   *Yes:* Put it in `features/<feature>/components/`.
*   **Does it call a feature-specific hook or mutation?** (e.g. `useCurrentWorkspace`, `useCreateCard`, `useUpdatePage`).
    *   *Yes:* Put it in `features/<feature>/components/`.
*   **Is it a route-specific layout or composition shell?** (e.g. `AuthShell`, `DashboardHomeScreen`, `WorkspaceHomeScreen` that purely coordinates multiple features for a single route).
    *   *Yes:* Put it in `app/**/_components/`.
*   **Is it a primitive, business-blind UI component?** (e.g. `Button`, `Dialog`, `Input`, `EmptyState`, `ConfirmDialog`, `DataTable`).
    *   *Yes:* Put it in `components/ui/` or generic folders like `components/feedback/` or `components/layout/`.

---

## 2. Import Boundary Rules

Strictly enforce the one-way dependency flow to prevent circular imports and architectural decay:

```txt
app/               → features/ (public API), components/, lib/
features/          → own files, components/ui/, lib/, types/
components/ui/     → external libraries only (NO features, NO lib, NO app)
lib/               → external libraries, types/ (NO features, NO app)
```

*   **Feature-to-Feature:** A feature **must not** deep-import the internals of another feature. All cross-feature imports must go through the sibling's public API (e.g., `@/features/auth` instead of `@/features/auth/components/sign-in-form`).
*   **No reverse dependencies:** `lib/` and `components/ui/` must never import from `features/` or `app/`.
*   **Explicit exports:** In `features/<feature>/index.ts`, explicitly export only what is necessary:
    ```ts
    // Good
    export { WorkspaceSwitcher } from "./components/workspace-switcher"
    export { useWorkspace } from "./hooks/queries/use-workspace"
    
    // Bad
    export * from "./components"
    export * from "./hooks"
    ```

---

## 3. API File Rules

API client modules under `features/<feature>/api/` must remain pure HTTP clients.

*   **API files must only:** Define endpoints, perform HTTP calls via the centralized client, and handle DTO mapping.
*   **API files must NOT:**
    *   Import React or render UI.
    *   Use React hooks (including TanStack Query hooks).
    *   Trigger side effects like showing toast notifications or calling the router.
    *   Directly update the QueryClient cache.
*   **DTO & Mappers:** Tách rõ ràng. DTOs go in `features/<feature>/api/*.dto.ts`. Mappers go in `features/<feature>/model/*.mapper.ts` and must be pure, synchronous, and unit-testable.

---

## 4. Query & Mutation Rules

*   **State Separation:** Server state must be managed by TanStack Query. Local UI state (e.g. open dialogs, hover) goes in `useState`. Shareable UI state (e.g. filters, active tabs) goes in the URL (search params). Global UI state (e.g. sidebar collapsed) goes in Zustand. Zustand must **never** store backend data cache.
*   **Query Key Factory:** All query hooks must consume keys from `lib/query/query-keys.ts`. Do not hardcode query key arrays in hooks.
*   **Scoped Invalidation:** Mutations must only invalidate the exact, affected scopes (e.g. updating a board item only invalidates the specific board, not the entire workspace list).
*   **Error Handling:** Do not hardcode toast messages or i18n translation calls inside deep hooks. Keep them in the calling component or orchestrator.

---

## 5. Form Rules

*   **Framework:** Always use **React Hook Form** + **Zod** for form management and validation.
*   **Structure:**
    *   Schemas: `features/<feature>/schemas/*.schema.ts`
    *   Types: `features/<feature>/types/*.types.ts`
    *   Components: `features/<feature>/components/*form.tsx`
*   **Validation:** Forms must validate on the client first using Zod. Server validation errors must be mapped back into React Hook Form field errors.

---

## 6. Permission Rules

*   **No Role Hardcoding:** Never write direct role-string checks in components (e.g. `user.role === 'Owner'`).
*   **Centralized Evaluation:** Always use the centralized `useCan` hook from `lib/permissions/use-can.ts` to evaluate permissions against a resource:
    ```tsx
    const canDelete = useCan("board.delete", { workspaceId, resourceId: boardId });
    return canDelete ? <DeleteButton /> : null;
    ```
*   **UX Only:** Remember that frontend permission checks are solely for improving UX (hiding/showing buttons). Security is enforced by the backend.

---

## 7. Route Rules

*   **Tabbed Views:** Do not create separate file-system routes for board views (e.g., `/kanban`, `/table`, `/calendar`). Instead, use the search parameter `?view=table` within a single board page.
*   **Centralized Route Registry:** All navigation paths must be resolved through the `routes` helper in `lib/routes/routes.ts`. Never hardcode raw link strings (e.g. `href="/[workspaceId]/boards/[boardId]"`) in components.

---

## 8. Pull Request (PR) Checklist

Before submitting a PR, verify:

- [ ] `bun run type-check` passes with no errors.
- [ ] `bun run lint` passes with no style violations.
- [ ] `bun run test` passes all unit and integration tests.
- [ ] No circular dependencies or forbidden imports (e.g. `lib` referencing `features`).
- [ ] No business components are placed in `components/ui`.
- [ ] All new page views are routed via query parameters, not separate file-system folders.
- [ ] No raw `DateTime.UtcNow` calls (on backend) or unsynchronized timestamps.
- [ ] No `any` type usage unless explicitly authorized in mappers.
- [ ] Centralized `queryKeys` and `routes` are used exclusively.
- [ ] The build (`bun run build`) completes successfully.
