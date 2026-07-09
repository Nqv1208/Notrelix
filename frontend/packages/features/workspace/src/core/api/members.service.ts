import type { WorkspaceMember } from '../types/workspace';
import type { WorkspaceApiClient } from './workspace.service';

export function createMembersService(
  _api: WorkspaceApiClient,
) {
  return {
    // PENDING BACKEND: GET /workspaces/{workspaceId}/members is missing
    // See: docs/client/audits/backend-contract-gaps.md#1-workspace-members-crud
    async getList(_workspaceId: string): Promise<WorkspaceMember[]> {
      console.warn('GET /workspaces/{workspaceId}/members is pending backend validation.');
      return [];
    },

    // PENDING BACKEND: PUT /workspaces/{workspaceId}/members/{userId}/role is missing
    async updateRole(
      _workspaceId: string,
      _userId: string,
      _role: WorkspaceMember['role'],
    ): Promise<WorkspaceMember> {
      console.warn('PUT /workspaces/{workspaceId}/members/{userId}/role is pending backend validation.');
      throw new Error('Endpoint not implemented by backend.');
    },

    // PENDING BACKEND: DELETE /workspaces/{workspaceId}/members/{userId} is missing
    async removeMember(_workspaceId: string, _userId: string): Promise<void> {
      console.warn('DELETE /workspaces/{workspaceId}/members/{userId} is pending backend validation.');
      throw new Error('Endpoint not implemented by backend.');
    },
  };
}
