---
skill: frontend-feature
description: Scaffold complete frontend feature with API client, hooks, schemas, and types for Notrelix
version: 1.0.0
---

# Frontend Feature Scaffolding

Generate a complete frontend feature following Notrelix's feature-sliced design pattern.

## When to Use

- Adding a new frontend feature/domain
- Need to create API integration with backend
- Want to follow established frontend architecture automatically
- Creating hooks for data fetching with TanStack Query

## What This Skill Does

1. Creates feature directory structure (api/, hooks/, schemas/, types/)
2. Generates API client functions with axios
3. Creates TanStack Query hooks (useQuery, useMutation)
4. Generates Zod schemas for validation
5. Creates TypeScript types/interfaces
6. Follows all naming conventions from AGENTS.md

## Prerequisites

Before using this skill, you should know:
- Which domain/feature this belongs to (auth, boards, docs, workspace, etc.)
- What API endpoints are available (check backend or `lib/api/endpoints.ts`)
- What data structures are returned from the API

## Feature Structure

```
frontend/features/{feature-name}/
├── api/
│   └── {feature}-api.ts          # API client functions (axios calls)
├── hooks/
│   ├── use-{feature}.ts          # Query hooks (useQuery)
│   ├── use-create-{entity}.ts    # Mutation hooks (useMutation)
│   ├── use-update-{entity}.ts
│   └── use-delete-{entity}.ts
├── schemas/
│   ├── {entity}.schema.ts        # Zod validation schemas
│   └── index.ts                  # Export all schemas
├── types/
│   └── index.ts                  # TypeScript types/interfaces
└── utils/
    └── {feature}-helpers.ts      # Feature-specific utilities (optional)
```

## Naming Conventions

### Files

- **API:** `{feature}-api.ts` (e.g., `boards-api.ts`, `cards-api.ts`)
- **Hooks:** `use-{action}-{entity}.ts` (e.g., `use-create-card.ts`, `use-board.ts`)
- **Schemas:** `{entity}.schema.ts` (e.g., `card.schema.ts`, `board.schema.ts`)
- **Types:** `index.ts` (all types in one file per feature)

### Functions

- **API functions:** `{verb}{Entity}` (e.g., `getBoard`, `createCard`, `updatePage`)
- **Hooks:** `use{Entity}` or `use{Verb}{Entity}` (e.g., `useBoard`, `useCreateCard`)
- **Schemas:** `{entity}Schema` (e.g., `cardSchema`, `createBoardSchema`)
- **Types:** `{Entity}` or `{Entity}Dto` (e.g., `Card`, `BoardDto`)

## Template: API Client

```typescript
// File: frontend/features/boards/api/boards-api.ts
import { apiClient } from '@/lib/api/api-client';
import { ENDPOINTS } from '@/lib/api/endpoints';
import type { Board, CreateBoardDto, UpdateBoardDto } from '../types';

export const boardsApi = {
  /**
   * Get all boards in a workspace
   */
  getBoards: async (workspaceId: string): Promise<Board[]> => {
    const response = await apiClient.get<{ data: Board[] }>(
      ENDPOINTS.boards.list(workspaceId)
    );
    return response.data.data;
  },

  /**
   * Get a single board by ID
   */
  getBoard: async (boardId: string): Promise<Board> => {
    const response = await apiClient.get<{ data: Board }>(
      ENDPOINTS.boards.detail(boardId)
    );
    return response.data.data;
  },

  /**
   * Get full board with lists and cards
   */
  getFullBoard: async (boardId: string): Promise<Board> => {
    const response = await apiClient.get<{ data: Board }>(
      ENDPOINTS.boards.full(boardId)
    );
    return response.data.data;
  },

  /**
   * Create a new board
   */
  createBoard: async (workspaceId: string, data: CreateBoardDto): Promise<Board> => {
    const response = await apiClient.post<{ data: Board }>(
      ENDPOINTS.boards.list(workspaceId),
      data
    );
    return response.data.data;
  },

  /**
   * Update a board
   */
  updateBoard: async (boardId: string, data: UpdateBoardDto): Promise<Board> => {
    const response = await apiClient.patch<{ data: Board }>(
      ENDPOINTS.boards.detail(boardId),
      data
    );
    return response.data.data;
  },

  /**
   * Delete a board (soft delete)
   */
  deleteBoard: async (boardId: string): Promise<void> => {
    await apiClient.delete(ENDPOINTS.boards.detail(boardId));
  },
};
```

## Template: Query Hook

```typescript
// File: frontend/features/boards/hooks/use-board.ts
import { useQuery } from '@tanstack/react-query';
import { queryKeys } from '@/lib/query/query-keys';
import { boardsApi } from '../api/boards-api';

/**
 * Hook to fetch a single board by ID
 */
export function useBoard(boardId: string) {
  return useQuery({
    queryKey: queryKeys.boards.detail(boardId),
    queryFn: () => boardsApi.getBoard(boardId),
    staleTime: 30 * 1000, // 30 seconds
    enabled: !!boardId,
  });
}

/**
 * Hook to fetch all boards in a workspace
 */
export function useBoards(workspaceId: string) {
  return useQuery({
    queryKey: queryKeys.boards.list(workspaceId),
    queryFn: () => boardsApi.getBoards(workspaceId),
    staleTime: 30 * 1000, // 30 seconds
    enabled: !!workspaceId,
  });
}

/**
 * Hook to fetch full board with lists and cards
 */
export function useFullBoard(boardId: string) {
  return useQuery({
    queryKey: queryKeys.boards.fullBoard(boardId),
    queryFn: () => boardsApi.getFullBoard(boardId),
    staleTime: 30 * 1000, // 30 seconds
    enabled: !!boardId,
  });
}
```

## Template: Mutation Hook

```typescript
// File: frontend/features/boards/hooks/use-create-board.ts
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { queryKeys } from '@/lib/query/query-keys';
import { boardsApi } from '../api/boards-api';
import type { CreateBoardDto } from '../types';

/**
 * Hook to create a new board
 */
export function useCreateBoard(workspaceId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateBoardDto) => boardsApi.createBoard(workspaceId, data),
    onSuccess: (newBoard) => {
      // Invalidate boards list to refetch
      queryClient.invalidateQueries({
        queryKey: queryKeys.boards.list(workspaceId),
      });

      toast.success('Board created successfully');
    },
    onError: (error: Error) => {
      toast.error(error.message || 'Failed to create board');
    },
  });
}
```

## Template: Mutation Hook with Optimistic Update

```typescript
// File: frontend/features/boards/hooks/use-update-board.ts
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { queryKeys } from '@/lib/query/query-keys';
import { boardsApi } from '../api/boards-api';
import type { Board, UpdateBoardDto } from '../types';

/**
 * Hook to update a board with optimistic updates
 */
export function useUpdateBoard(boardId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: UpdateBoardDto) => boardsApi.updateBoard(boardId, data),
    
    // Optimistic update
    onMutate: async (newData) => {
      // Cancel outgoing refetches
      await queryClient.cancelQueries({
        queryKey: queryKeys.boards.detail(boardId),
      });

      // Snapshot previous value
      const previousBoard = queryClient.getQueryData<Board>(
        queryKeys.boards.detail(boardId)
      );

      // Optimistically update
      if (previousBoard) {
        queryClient.setQueryData<Board>(
          queryKeys.boards.detail(boardId),
          { ...previousBoard, ...newData }
        );
      }

      return { previousBoard };
    },

    // Rollback on error
    onError: (error: Error, _, context) => {
      if (context?.previousBoard) {
        queryClient.setQueryData(
          queryKeys.boards.detail(boardId),
          context.previousBoard
        );
      }
      toast.error(error.message || 'Failed to update board');
    },

    // Refetch on success or error
    onSettled: () => {
      queryClient.invalidateQueries({
        queryKey: queryKeys.boards.detail(boardId),
      });
    },

    onSuccess: () => {
      toast.success('Board updated successfully');
    },
  });
}
```

## Template: Zod Schema

```typescript
// File: frontend/features/boards/schemas/board.schema.ts
import { z } from 'zod';

/**
 * Schema for creating a board
 */
export const createBoardSchema = z.object({
  title: z
    .string()
    .min(1, 'Title is required')
    .max(100, 'Title must not exceed 100 characters'),
  description: z
    .string()
    .max(500, 'Description must not exceed 500 characters')
    .optional(),
  color: z
    .string()
    .regex(/^#[0-9A-F]{6}$/i, 'Invalid color format')
    .optional(),
  isPrivate: z.boolean().default(false),
});

/**
 * Schema for updating a board
 */
export const updateBoardSchema = createBoardSchema.partial();

/**
 * Type inference from schema
 */
export type CreateBoardInput = z.infer<typeof createBoardSchema>;
export type UpdateBoardInput = z.infer<typeof updateBoardSchema>;
```

## Template: Types

```typescript
// File: frontend/features/boards/types/index.ts

/**
 * Board entity
 */
export interface Board {
  id: string;
  workspaceId: string;
  title: string;
  description: string | null;
  color: string | null;
  isPrivate: boolean;
  position: number;
  createdAt: string;
  updatedAt: string | null;
  createdBy: string;
  
  // Relations (optional, loaded with includes)
  lists?: List[];
  members?: BoardMember[];
  views?: BoardView[];
}

/**
 * List entity
 */
export interface List {
  id: string;
  boardId: string;
  title: string;
  position: number;
  isCollapsed: boolean;
  createdAt: string;
  updatedAt: string | null;
  
  // Relations
  cards?: Card[];
}

/**
 * Card entity
 */
export interface Card {
  id: string;
  listId: string;
  title: string;
  description: string | null;
  position: number;
  dueDate: string | null;
  linkedPageId: string | null;
  createdAt: string;
  updatedAt: string | null;
  
  // Relations
  labels?: Label[];
  members?: CardMember[];
  checklists?: Checklist[];
}

/**
 * DTO for creating a board
 */
export interface CreateBoardDto {
  title: string;
  description?: string;
  color?: string;
  isPrivate?: boolean;
}

/**
 * DTO for updating a board
 */
export interface UpdateBoardDto {
  title?: string;
  description?: string;
  color?: string;
  isPrivate?: boolean;
}
```

## Important Rules

### DO

- ✅ Use TanStack Query for all data fetching
- ✅ Use query keys from `lib/query/query-keys.ts` (never hardcode)
- ✅ Set appropriate `staleTime` based on data type
- ✅ Use `enabled` option to prevent unnecessary fetches
- ✅ Invalidate queries after mutations
- ✅ Use optimistic updates for better UX (when appropriate)
- ✅ Show toast notifications for mutations
- ✅ Export all functions/hooks from feature index
- ✅ Use Zod for form validation
- ✅ Type all API responses

### DON'T

- ❌ Don't hardcode query keys (use factory)
- ❌ Don't fetch data in components (use hooks)
- ❌ Don't import from other features (use lib/)
- ❌ Don't put UI components in features/ (use app/ or components/)
- ❌ Don't forget error handling
- ❌ Don't forget loading states
- ❌ Don't use `any` type
- ❌ Don't invalidate entire query cache (be specific)

## StaleTime Guidelines

Set `staleTime` based on how frequently data changes:

```typescript
// User/workspace data (changes infrequently)
staleTime: 5 * 60 * 1000  // 5 minutes

// Board/list/card data (moderate changes)
staleTime: 30 * 1000       // 30 seconds

// Page blocks (frequent edits)
staleTime: 10 * 1000       // 10 seconds

// Notifications unread count (very frequent)
staleTime: 15 * 1000       // 15 seconds

// Search results (ephemeral)
staleTime: 10 * 1000       // 10 seconds
```

## Query Keys Pattern

Always use the centralized query keys factory:

```typescript
// File: frontend/lib/query/query-keys.ts
export const queryKeys = {
  boards: {
    all: ['boards'] as const,
    lists: () => [...queryKeys.boards.all, 'list'] as const,
    list: (workspaceId: string) => [...queryKeys.boards.lists(), workspaceId] as const,
    details: () => [...queryKeys.boards.all, 'detail'] as const,
    detail: (boardId: string) => [...queryKeys.boards.details(), boardId] as const,
    fullBoard: (boardId: string) => [...queryKeys.boards.all, boardId, 'full'] as const,
  },
  // ... other domains
};
```

## Common Patterns

### Pagination

```typescript
export function useBoards(workspaceId: string, page = 1, limit = 20) {
  return useQuery({
    queryKey: queryKeys.boards.list(workspaceId, page, limit),
    queryFn: () => boardsApi.getBoards(workspaceId, { page, limit }),
    staleTime: 30 * 1000,
    keepPreviousData: true, // Keep old data while fetching new page
  });
}
```

### Infinite Query

```typescript
export function useInfiniteBoards(workspaceId: string) {
  return useInfiniteQuery({
    queryKey: queryKeys.boards.list(workspaceId),
    queryFn: ({ pageParam = 1 }) => 
      boardsApi.getBoards(workspaceId, { page: pageParam }),
    getNextPageParam: (lastPage, pages) => 
      lastPage.hasMore ? pages.length + 1 : undefined,
    staleTime: 30 * 1000,
  });
}
```

### Dependent Queries

```typescript
export function useBoardWithWorkspace(boardId: string) {
  // First, get the board
  const { data: board } = useBoard(boardId);
  
  // Then, get the workspace (only if board is loaded)
  const { data: workspace } = useWorkspace(board?.workspaceId ?? '', {
    enabled: !!board?.workspaceId,
  });
  
  return { board, workspace };
}
```

## Checklist

When generating frontend feature code, ensure:

- [ ] API client functions use `apiClient` from `lib/api/api-client.ts`
- [ ] Endpoints use constants from `lib/api/endpoints.ts`
- [ ] Query hooks use query keys from `lib/query/query-keys.ts`
- [ ] Appropriate `staleTime` set based on data type
- [ ] Mutation hooks invalidate relevant queries
- [ ] Toast notifications for success/error
- [ ] Optimistic updates for better UX (when appropriate)
- [ ] Zod schemas for form validation
- [ ] TypeScript types/interfaces defined
- [ ] All exports added to feature index files
- [ ] Error handling included
- [ ] Loading states considered

## Examples

### Example 1: Simple Feature

**User Request:** "Add frontend feature for labels"

**Generated Files:**

1. `frontend/features/labels/api/labels-api.ts`
2. `frontend/features/labels/hooks/use-labels.ts`
3. `frontend/features/labels/hooks/use-create-label.ts`
4. `frontend/features/labels/schemas/label.schema.ts`
5. `frontend/features/labels/types/index.ts`

### Example 2: Complex Feature with Relations

**User Request:** "Add frontend feature for cards with checklists"

**Generated Files:**

1. `frontend/features/cards/api/cards-api.ts`
2. `frontend/features/cards/hooks/use-card.ts`
3. `frontend/features/cards/hooks/use-create-card.ts`
4. `frontend/features/cards/hooks/use-update-card.ts`
5. `frontend/features/cards/hooks/use-move-card.ts`
6. `frontend/features/cards/hooks/use-card-checklists.ts`
7. `frontend/features/cards/schemas/card.schema.ts`
8. `frontend/features/cards/types/index.ts`

## Related Skills

- `component-scaffold` — Create UI components that use these hooks
- `backend-cqrs` — Create backend endpoints that these API clients call

## References

- [AGENTS.md](../../AGENTS.md) — Section 4: Frontend Rules
- [notrelix-frontend-structure.md](../../notrelix-frontend-structure.md) — Detailed architecture
- [TanStack Query Docs](https://tanstack.com/query/latest/docs/react/overview)
