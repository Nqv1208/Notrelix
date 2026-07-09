import type { WorkspaceInvitation, CreateWorkspaceInvitationInput } from '../types/workspace';
import type { WorkspaceApiClient } from './workspace.service';

export interface InvitationsEndpoints {
  workspaces: {
    invitationByToken: (token: string) => string;
    acceptInvitation: (token: string) => string;
    pendingInvitations: string;
  };
}

export function createInvitationsService(
  api: WorkspaceApiClient,
  endpoints: InvitationsEndpoints,
  options?: {
    mockMode?: boolean;
  },
) {
  const mockMode = options?.mockMode === true;

  return {
    async getByToken(token: string): Promise<WorkspaceInvitation> {
      return api.get<WorkspaceInvitation>(endpoints.workspaces.invitationByToken(token));
    },

    async accept(token: string): Promise<void> {
      return api.post<void>(endpoints.workspaces.acceptInvitation(token), {});
    },

    async getPending(): Promise<WorkspaceInvitation[]> {
      return api.get<WorkspaceInvitation[]>(endpoints.workspaces.pendingInvitations);
    },

    // PENDING BACKEND: POST /workspaces/{workspaceId}/invitations is not yet in contracts
    // See: docs/client/audits/backend-contract-gaps.md
    async create(
      _workspaceId: string,
      _input: CreateWorkspaceInvitationInput,
    ): Promise<WorkspaceInvitation> {
      if (mockMode) {
        console.warn('POST /workspaces/{workspaceId}/invitations is pending backend validation.');
        return {
          id: 'stub-invitation',
          email: _input.email,
          role: _input.role as 'member',
          expiresAt: new Date(Date.now() + 86400000).toISOString(),
          isAccepted: false,
          createdAt: new Date().toISOString(),
        };
      }
      throw new Error('Endpoint not implemented by backend.');
    },

    // PENDING BACKEND: DELETE /workspaces/{workspaceId}/invitations/{invitationId}
    async delete(_workspaceId: string, _invitationId: string): Promise<void> {
      if (mockMode) {
        console.warn('DELETE /workspaces/{workspaceId}/invitations/{invitationId} is pending backend validation.');
        return;
      }
      throw new Error('Endpoint not implemented by backend.');
    },
  };
}
