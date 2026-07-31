import type { NotrelixClient } from '@notrelix/contracts';
import type { QueryClient } from '@tanstack/react-query';
import type { AppRuntime } from '@notrelix/runtime-web';

export interface ItemRepository {
  getItem(id: string, signal?: AbortSignal): Promise<unknown>;
  createItem(command: { boardId: string; title: string; groupId?: string }, signal?: AbortSignal): Promise<unknown>;
}

export interface BoardRepository {
  getBoard(id: string, signal?: AbortSignal): Promise<unknown>;
}

export interface WorkManagementServices {
  readonly items: ItemRepository;
  readonly boards: BoardRepository;
}

export interface WebApplicationServices {
  readonly runtime: AppRuntime;
  readonly queryClient: QueryClient;
  readonly apiClient: NotrelixClient;
  readonly workManagement: WorkManagementServices;
  dispose(): Promise<void>;
}

export function createWorkManagementServices(client: NotrelixClient): WorkManagementServices {
  return {
    items: {
      async getItem(id: string, signal?: AbortSignal) {
        return client.api.get(`/items/${id}`, { signal });
      },
      async createItem(command: { boardId: string; title: string; groupId?: string }, signal?: AbortSignal) {
        return client.api.post('/items', command, { signal });
      },
    },
    boards: {
      async getBoard(id: string, signal?: AbortSignal) {
        return client.api.get(`/boards/${id}`, { signal });
      },
    },
  };
}

export function createWebApplicationServices(deps: {
  runtime: AppRuntime;
  queryClient: QueryClient;
  apiClient: NotrelixClient;
}): WebApplicationServices {
  const workManagement = createWorkManagementServices(deps.apiClient);

  return {
    runtime: deps.runtime,
    queryClient: deps.queryClient,
    apiClient: deps.apiClient,
    workManagement,
    async dispose() {
      await deps.runtime.dispose();
    },
  };
}
