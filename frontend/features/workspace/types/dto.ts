import type { WorkspaceMember } from "./index"

export type WorkspaceDtoApi = {
  id: string
  name: string
  slug: string
  description?: string | null
  isPersonal: boolean
  plan: string
  iconType?: string | null
  iconValue?: string | null
  coverUrl?: string | null
  isArchived: boolean
  memberCount: number
  createdAt: string
  settings?: string | null
}

export type WorkspaceMemberDtoApi = {
  userId: string
  name: string
  avatar?: string | null
  role: string
  joinedAt: string
}

export type WorkspaceInvitationDtoApi = {
  id: string
  token?: string
  email: string
  role: string
  expiresAt: string
  isAccepted: boolean
  isExpired?: boolean
  createdAt: string
  workspaceId?: string
  workspaceSlug?: string
  workspaceName?: string
  inviterName?: string
}

export type WorkspaceActivityResponseApi = {
  data: Array<{
    id: string
    actorId: string
    action: string
    resourceTitle?: string | null
    createdAt: string
  }>
}

export type CreateWorkspaceDto = {
  name: string
  slug: string
  isPersonal: boolean
}

export type UpdateWorkspaceDto = {
  name?: string
  slug?: string
  settings?: string
}

export type CreateInvitationDto = {
  email: string
  role: WorkspaceMember["role"] | string
}
