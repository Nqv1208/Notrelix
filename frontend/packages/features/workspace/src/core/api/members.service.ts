import type { WorkspaceMember } from "../types/workspace";
import type { WorkspaceApiClient } from "./workspace.service";

export function createMembersService(api: WorkspaceApiClient) {
  return {
    async getList(workspaceId: string): Promise<WorkspaceMember[]> {
      return api.get<WorkspaceMember[]>(`/workspaces/${workspaceId}/members`);
    },

    async updateRole(
      workspaceId: string,
      userId: string,
      role: WorkspaceMember["role"],
    ): Promise<WorkspaceMember> {
      return api.put<WorkspaceMember>(
        `/workspaces/${workspaceId}/members/${userId}/role`,
        { role },
      );
    },

    async removeMember(workspaceId: string, userId: string): Promise<void> {
      return api.delete<void>(`/workspaces/${workspaceId}/members/${userId}`);
    },
  };
}
