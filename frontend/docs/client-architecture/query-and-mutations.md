# Query & Optimistic Mutation Specification

> **TanStack Query v5 Patterns, Optimistic Engine & Cache Reconciliation**

---

## 1. Query Key Factories

Query keys must be scoped deterministically using factory functions:

```ts
export const wmQueryKeys = {
  all: ['work-management'] as const,
  boards: (workspaceId: string) => [...wmQueryKeys.all, 'boards', workspaceId] as const,
  board: (boardId: string) => [...wmQueryKeys.all, 'board', boardId] as const,
  items: (boardId: string) => [...wmQueryKeys.all, 'items', boardId] as const,
};
```

---

## 2. Optimistic Mutation Transaction Engine

Optimistic updates are executed via `executeOptimisticCommand()` in `@notrelix/query`:
1. **Snapshot Capture:** Record prior cache state for all affected query keys.
2. **Optimistic Apply:** Mutate local cache state optimistically.
3. **Execution:** Execute server command.
4. **Reconciliation / Rollback:** On error, restore snapshots in inverse order and preserve original mutation error.
