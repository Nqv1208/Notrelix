import type {
  WorkspaceView,
  CreateWorkspaceViewInput,
  UpdateWorkspaceViewInput,
} from "../types/workspace";
import type { WorkspaceApiClient } from "./workspace.service";

export function createViewsService(api: WorkspaceApiClient) {
  return {
    async getList(workspaceId: string): Promise<WorkspaceView[]> {
      return api.get<WorkspaceView[]>(`/workspaces/${workspaceId}/views`);
    },

    async create(input: CreateWorkspaceViewInput): Promise<WorkspaceView> {
      return api.post<WorkspaceView>(
        `/workspaces/${input.workspaceId}/views`,
        input,
      );
    },

    async update(
      workspaceId: string,
      viewId: string,
      input: UpdateWorkspaceViewInput,
    ): Promise<WorkspaceView> {
      return api.patch<WorkspaceView>(
        `/workspaces/${workspaceId}/views/${viewId}`,
        input,
      );
    },

    async delete(workspaceId: string, viewId: string): Promise<void> {
      return api.delete<void>(`/workspaces/${workspaceId}/views/${viewId}`);
    },

    async reorder(workspaceId: string, viewIds: string[]): Promise<void> {
      return api.post<void>(`/workspaces/${workspaceId}/views/reorder`, {
        viewIds,
      });
    },
  };
}
