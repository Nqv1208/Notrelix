import type {
  WorkspaceInvitation,
  CreateWorkspaceInvitationInput,
} from "../types/workspace";
import type { WorkspaceApiClient } from "./workspace.service";

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
) {
  return {
    async getByToken(token: string): Promise<WorkspaceInvitation> {
      return api.get<WorkspaceInvitation>(
        endpoints.workspaces.invitationByToken(token),
      );
    },

    async accept(token: string): Promise<void> {
      return api.post<void>(endpoints.workspaces.acceptInvitation(token), {});
    },

    async getPending(): Promise<WorkspaceInvitation[]> {
      return api.get<WorkspaceInvitation[]>(
        endpoints.workspaces.pendingInvitations,
      );
    },

    async create(
      _workspaceId: string,
      _input: CreateWorkspaceInvitationInput,
    ): Promise<WorkspaceInvitation> {
      throw new Error("Endpoint not implemented by backend.");
    },

    async delete(_workspaceId: string, _invitationId: string): Promise<void> {
      throw new Error("Endpoint not implemented by backend.");
    },
  };
}
