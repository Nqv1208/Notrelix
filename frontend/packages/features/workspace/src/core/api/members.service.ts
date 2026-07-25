import type { WorkspaceMember } from '../types/workspace';
import type { WorkspaceApiClient } from './workspace.service';

export function createMembersService(
  _api: WorkspaceApiClient,
  options?: {
    mockMode?: boolean;
  },
) {
  const mockMode = options?.mockMode === true;

  return {
    // PENDING BACKEND: GET /workspaces/{workspaceId}/members is missing
    // See: docs/client/audits/backend-contract-gaps.md#1-workspace-members-crud
    async getList(_workspaceId: string): Promise<WorkspaceMember[]> {
      if (mockMode) {
        console.warn('GET /workspaces/{workspaceId}/members is pending backend validation.');
        return [];
      }
      throw new Error('Backend contract missing for workspaces.members');
    },

    // PENDING BACKEND: PUT /workspaces/{workspaceId}/members/{userId}/role is missing
    async updateRole(
      _workspaceId: string,
      _userId: string,
      _role: WorkspaceMember['role'],
    ): Promise<WorkspaceMember> {
      if (mockMode) {
        console.warn('PUT /workspaces/{workspaceId}/members/{userId}/role is pending backend validation.');
        // Return dummy role change
        return {
          id: 'dummy-member-id',
          userId: _userId,
          name: 'Dummy Member',
          initials: 'DM',
          role: _role,
          status: 'active',
          workload: 0,
          color: 'indigo',
        };
      }
      throw new Error('Endpoint not implemented by backend.');
    },

    // PENDING BACKEND: DELETE /workspaces/{workspaceId}/members/{userId} is missing
    async removeMember(_workspaceId: string, _userId: string): Promise<void> {
      if (mockMode) {
        console.warn('DELETE /workspaces/{workspaceId}/members/{userId} is pending backend validation.');
        return;
      }
      throw new Error('Endpoint not implemented by backend.');
    },
  };
}
