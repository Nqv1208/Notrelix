import type {
  WorkspaceSummary,
  CreateWorkspaceInput,
  UpdateWorkspaceInput,
} from "../types/workspace";

export interface WorkspaceApiClient {
  get<T>(url: string): Promise<T>;
  post<T>(url: string, body: unknown): Promise<T>;
  put<T>(url: string, body: unknown): Promise<T>;
  patch<T>(url: string, body: unknown): Promise<T>;
  delete<T>(url: string): Promise<T>;
}

export interface WorkspaceEndpoints {
  workspaces: {
    list: string;
    detail: (workspaceId: string) => string;
  };
}

export function createWorkspaceService(
  api: WorkspaceApiClient,
  endpoints: WorkspaceEndpoints,
) {
  return {
    async getList(): Promise<WorkspaceSummary[]> {
      return api.get<WorkspaceSummary[]>(endpoints.workspaces.list);
    },

    async getDetail(id: string): Promise<WorkspaceSummary> {
      return api.get<WorkspaceSummary>(endpoints.workspaces.detail(id));
    },

    async create(input: CreateWorkspaceInput): Promise<WorkspaceSummary> {
      return api.post<WorkspaceSummary>(endpoints.workspaces.list, input);
    },

    async update(
      id: string,
      input: UpdateWorkspaceInput,
    ): Promise<WorkspaceSummary> {
      return api.patch<WorkspaceSummary>(
        endpoints.workspaces.detail(id),
        input,
      );
    },
  };
}
