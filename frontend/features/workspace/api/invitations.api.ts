import { api } from "@/lib/api/api-client"
import { endpoints } from "@/lib/api/endpoints"
import type { CreateInvitationDto, WorkspaceInvitationDtoApi } from "../types/dto"
import type { WorkspaceInvitation } from "../types"
import { normalizeMemberRole } from "./members.api"

export interface AcceptInvitationResponse {
  workspaceSlug: string
  workspaceId: string
}

export function mapInvitationDto(invitation: WorkspaceInvitationDtoApi): WorkspaceInvitation {
  return {
    id: invitation.id,
    token: invitation.token,
    email: invitation.email,
    role: normalizeMemberRole(invitation.role),
    expiresAt: invitation.expiresAt,
    isAccepted: invitation.isAccepted,
    isExpired: invitation.isExpired,
    createdAt: invitation.createdAt,
    workspaceId: invitation.workspaceId,
    workspaceSlug: invitation.workspaceSlug,
    workspaceName: invitation.workspaceName,
    inviterName: invitation.inviterName,
  }
}

export const invitationsApi = {
  async getInvitations(workspaceId: string): Promise<WorkspaceInvitation[]> {
    const invitations = await api.get<WorkspaceInvitationDtoApi[]>(
      `${endpoints.workspaces.detail(workspaceId)}/invitations`
    )
    return invitations.map(mapInvitationDto)
  },

  async createInvitation(workspaceId: string, input: CreateInvitationDto): Promise<WorkspaceInvitation> {
    const invitation = await api.post<WorkspaceInvitationDtoApi>(
      `${endpoints.workspaces.detail(workspaceId)}/invitations`,
      input
    )
    return mapInvitationDto(invitation)
  },

  async deleteInvitation(workspaceId: string, invitationId: string): Promise<void> {
    await api.delete<void>(`${endpoints.workspaces.detail(workspaceId)}/invitations/${invitationId}`)
  },

  async getInvitationByToken(token: string): Promise<WorkspaceInvitation> {
    const invitation = await api.get<WorkspaceInvitationDtoApi>(endpoints.workspaces.invitationByToken(token))
    return mapInvitationDto(invitation)
  },

  async acceptInvitation(token: string): Promise<AcceptInvitationResponse> {
    return api.post<AcceptInvitationResponse>(endpoints.workspaces.acceptInvitation(token), {})
  },

  async getPendingInvitations(): Promise<WorkspaceInvitation[]> {
    const invitations = await api.get<WorkspaceInvitationDtoApi[]>(endpoints.workspaces.pendingInvitations)
    return invitations.map(mapInvitationDto)
  },
}
