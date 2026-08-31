import type {
  WorkspaceInvitation,
  PendingWorkspaceInvitation,
  InvitationPreview,
  AcceptInvitationResult,
  CreateWorkspaceInvitationInput,
} from "../types/workspace";
import type { WorkspaceApiClient } from "./workspace.service";

export interface InvitationsEndpoints {
  workspaces: {
    invitationList: (workspaceId: string) => string;
    cancelInvitation: (workspaceId: string, invitationId: string) => string;
    invitationPreview: string;
    acceptInvitation: string;
    acceptInvitationById: (invitationId: string) => string;
    pendingInvitations: string;
  };
}

export function createInvitationsService(
  api: WorkspaceApiClient,
  endpoints: InvitationsEndpoints,
) {
  return {
    async getPreview(token: string): Promise<InvitationPreview> {
      return api.post<InvitationPreview>(
        endpoints.workspaces.invitationPreview,
        { token },
      );
    },

    async accept(token: string): Promise<AcceptInvitationResult> {
      return api.post<AcceptInvitationResult>(
        endpoints.workspaces.acceptInvitation,
        { token },
      );
    },

    async acceptById(invitationId: string): Promise<AcceptInvitationResult> {
      return api.post<AcceptInvitationResult>(
        endpoints.workspaces.acceptInvitationById(invitationId),
        {},
      );
    },

    async getPending(): Promise<PendingWorkspaceInvitation[]> {
      return api.get<PendingWorkspaceInvitation[]>(
        endpoints.workspaces.pendingInvitations,
      );
    },

    async listForWorkspace(
      workspaceId: string,
    ): Promise<WorkspaceInvitation[]> {
      return api.get<WorkspaceInvitation[]>(
        endpoints.workspaces.invitationList(workspaceId),
      );
    },

    async cancel(workspaceId: string, invitationId: string): Promise<void> {
      return api.delete<void>(
        endpoints.workspaces.cancelInvitation(workspaceId, invitationId),
      );
    },

    async create(
      _workspaceId: string,
      _input: CreateWorkspaceInvitationInput,
    ): Promise<WorkspaceInvitation> {
      throw new Error("Endpoint not implemented by backend.");
    },
  };
}
