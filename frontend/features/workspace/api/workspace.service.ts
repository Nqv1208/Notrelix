"use client"

import { api } from "@/lib/api/api-client"
import { endpoints } from "@/lib/api/endpoints"
import type {
  CreateWorkspaceDto,
  UpdateWorkspaceDto,
  WorkspaceDtoApi,
  WorkspaceMemberDtoApi,
  CreateInvitationDto,
  WorkspaceInvitationDtoApi,
  WorkspaceActivityResponseApi,
} from "../types/dto"
import type {
  WorkspaceSummary,
  WorkspaceMember,
  WorkspaceInvitation,
  WorkspaceActivityItem,
  CreateWorkspaceViewInput,
  UpdateWorkspaceViewInput,
  WorkspaceView,
} from "../types"
import { parseSettings, stringifySettings } from "../utils/settings"

// ── Workspace Mappers ────────────────────────────────────────────────────────

const workspacePlans = ["free", "pro", "business", "enterprise"] as const

export function normalizePlan(plan: string): WorkspaceSummary["plan"] {
  const normalized = plan.trim().toLowerCase()
  return workspacePlans.includes(normalized as WorkspaceSummary["plan"])
    ? (normalized as WorkspaceSummary["plan"])
    : "free"
}

export function mapWorkspaceDto(workspace: WorkspaceDtoApi): WorkspaceSummary {
  return {
    id: workspace.id,
    slug: workspace.slug,
    name: workspace.name,
    description: workspace.description ?? undefined,
    icon: workspace.iconValue?.trim() || workspace.name.trim().charAt(0).toUpperCase() || "W",
    plan: normalizePlan(workspace.plan),
    memberCount: workspace.memberCount,
    isPersonal: workspace.isPersonal,
    settings: workspace.settings ?? undefined,
  }
}

// ── Members Mappers ─────────────────────────────────────────────────────────

const memberColors = ["var(--primary)", "var(--accent)", "var(--destructive)", "var(--muted-foreground)"]

export function normalizeMemberRole(value: string): WorkspaceMember["role"] {
  const normalized = value.trim().toLowerCase()
  if (normalized === "owner") return "owner"
  if (normalized === "admin") return "admin"
  if (normalized === "guest") return "guest"
  return "member"
}

export function getInitials(name: string) {
  const parts = name.trim().split(/\s+/).filter(Boolean)
  if (parts.length === 0) return "?"
  if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase()
  return `${parts[0][0]}${parts[parts.length - 1][0]}`.toUpperCase()
}

export function mapMemberDto(member: WorkspaceMemberDtoApi, index: number): WorkspaceMember {
  return {
    id: `wm-${member.userId}`,
    userId: member.userId,
    name: member.name,
    initials: getInitials(member.name),
    role: normalizeMemberRole(member.role),
    status: "active",
    workload: 0,
    color: memberColors[index % memberColors.length],
    avatarUrl: member.avatar ?? undefined,
  }
}

// ── Invitations Mappers ──────────────────────────────────────────────────────

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

// ── Activity Mappers ─────────────────────────────────────────────────────────

export function mapActivityResponse(response: WorkspaceActivityResponseApi): WorkspaceActivityItem[] {
  return response.data.map((item) => ({
    id: item.id,
    actor: "Workspace",
    action: item.action,
    target: item.resourceTitle ?? "item",
    createdAt: item.createdAt,
  }))
}

// ── Views Helper ─────────────────────────────────────────────────────────────

function createFallbackView(workspaceId: string, viewId: string): WorkspaceView {
  const now = new Date().toISOString()
  return {
    id: viewId,
    workspaceId,
    name: viewId,
    type: "table",
    icon: "table",
    description: "",
    target: {},
    config: {},
    visibility: "workspace",
    isDefault: false,
    position: Date.now(),
    createdAt: now,
  }
}

// ── Service Implementation ───────────────────────────────────────────────────

export const workspaceService = {
  // ── Workspace ──

  async listWorkspaces(): Promise<WorkspaceSummary[]> {
    const workspaces = await api.get<WorkspaceDtoApi[]>(endpoints.workspaces.list)
    return workspaces.map(mapWorkspaceDto)
  },

  async getWorkspace(workspaceId: string): Promise<WorkspaceSummary> {
    const workspace = await api.get<WorkspaceDtoApi>(endpoints.workspaces.detail(workspaceId))
    return mapWorkspaceDto(workspace)
  },

  async createWorkspace(input: CreateWorkspaceDto): Promise<WorkspaceSummary> {
    const workspace = await api.post<WorkspaceDtoApi>(endpoints.workspaces.list, input)
    return mapWorkspaceDto(workspace)
  },

  async updateWorkspace(workspaceId: string, input: UpdateWorkspaceDto): Promise<WorkspaceSummary> {
    const workspace = await api.patch<WorkspaceDtoApi>(endpoints.workspaces.detail(workspaceId), input)
    return mapWorkspaceDto(workspace)
  },

  // ── Members ──

  async getMembers(workspaceId: string): Promise<WorkspaceMember[]> {
    const members = await api.get<WorkspaceMemberDtoApi[]>(`${endpoints.workspaces.detail(workspaceId)}/members`)
    return members.map(mapMemberDto)
  },

  async updateMemberRole(workspaceId: string, userId: string, role: string): Promise<WorkspaceMember> {
    const member = await api.patch<WorkspaceMemberDtoApi>(
      `${endpoints.workspaces.detail(workspaceId)}/members/${userId}`,
      { role }
    )
    return mapMemberDto(member, 0)
  },

  async removeMember(workspaceId: string, userId: string): Promise<void> {
    await api.delete<void>(`${endpoints.workspaces.detail(workspaceId)}/members/${userId}`)
  },

  // ── Invitations ──

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

  // ── Activity ──

  async getActivityLogs(workspaceId: string, page = 1, pageSize = 20): Promise<WorkspaceActivityItem[]> {
    const activity = await api.get<WorkspaceActivityResponseApi>(
      `${endpoints.workspaces.detail(workspaceId)}/activity?page=${page}&pageSize=${pageSize}`
    )
    return mapActivityResponse(activity)
  },

  // ── Views ──

  async reorderViews(workspaceId: string, orderedViewIds: string[]): Promise<void> {
    const workspace = await this.getWorkspace(workspaceId)
    const settings = parseSettings(workspace.settings)
    settings.customViewsOrder = orderedViewIds

    await api.patch(endpoints.workspaces.detail(workspaceId), {
      settings: stringifySettings(settings),
    })
  },

  async createView(input: CreateWorkspaceViewInput): Promise<WorkspaceView> {
    const now = new Date().toISOString()
    const view: WorkspaceView = {
      id: `${input.type}-${Date.now()}`,
      workspaceId: input.workspaceId,
      name: input.name,
      type: input.type,
      icon: input.type,
      description: "",
      target: input.target ?? {},
      config: {},
      visibility: "workspace",
      isDefault: false,
      position: Date.now(),
      createdAt: now,
      updatedAt: now,
    }

    const workspace = await this.getWorkspace(input.workspaceId)
    const settings = parseSettings(workspace.settings)
    settings.customViews = [...(settings.customViews ?? []), view]

    await api.patch(endpoints.workspaces.detail(input.workspaceId), {
      settings: stringifySettings(settings),
    })

    return view
  },

  async updateView(workspaceId: string, viewId: string, input: UpdateWorkspaceViewInput): Promise<WorkspaceView> {
    const workspace = await this.getWorkspace(workspaceId)
    const settings = parseSettings(workspace.settings)
    const customViews = settings.customViews ?? []
    const current = customViews.find((view) => view.id === viewId)

    const updated: WorkspaceView = {
      ...(current ?? createFallbackView(workspaceId, viewId)),
      ...input,
      id: viewId,
      workspaceId,
      config: { ...(current?.config ?? {}), ...input.config },
      updatedAt: new Date().toISOString(),
    }

    settings.customViews = current
      ? customViews.map((view) => (view.id === viewId ? updated : view))
      : [...customViews, updated]

    await api.patch(endpoints.workspaces.detail(workspaceId), {
      settings: stringifySettings(settings),
    })

    return updated
  },
}
