import { api } from "@/lib/api/api-client"
import { endpoints } from "@/lib/api/endpoints"
import { boardApi } from "@/features/boards/api/board.api"
import { pageService } from "@/features/docs/api/page.service"
import type {
  CreateWorkspaceViewInput,
  UpdateWorkspaceViewInput,
  WorkspaceActivityItem,
  WorkspaceMember,
  WorkspaceSnapshot,
  WorkspaceSummary,
  WorkspaceView,
} from "../types"

type WorkspaceDtoApi = {
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

type WorkspaceMemberDtoApi = {
  userId: string
  name: string
  avatar?: string | null
  role: string
  joinedAt: string
}

type WorkspaceActivityResponseApi = {
  data: Array<{
    id: string
    actorId: string
    action: string
    resourceTitle?: string | null
    createdAt: string
  }>
}

const workspacePlans = ["free", "pro", "business", "enterprise"] as const
const memberColors = ["var(--primary)", "var(--accent)", "var(--destructive)", "var(--muted-foreground)"]

function normalizePlan(plan: string): WorkspaceSummary["plan"] {
  const normalized = plan.trim().toLowerCase()
  return workspacePlans.includes(normalized as WorkspaceSummary["plan"])
    ? (normalized as WorkspaceSummary["plan"])
    : "free"
}

function mapWorkspaceDto(workspace: WorkspaceDtoApi): WorkspaceSummary {
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

function mapMemberDto(member: WorkspaceMemberDtoApi, index: number): WorkspaceMember {
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

function normalizeMemberRole(value: string): WorkspaceMember["role"] {
  const normalized = value.trim().toLowerCase()
  if (normalized === "owner") return "owner"
  if (normalized === "admin") return "admin"
  if (normalized === "guest") return "guest"
  return "member"
}

function getInitials(name: string) {
  const parts = name.trim().split(/\s+/).filter(Boolean)
  if (parts.length === 0) return "?"
  if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase()
  return `${parts[0][0]}${parts[parts.length - 1][0]}`.toUpperCase()
}

function createViews(workspaceId: string, boards: Awaited<ReturnType<typeof boardApi.getBoardsByWorkspaceId>>, pages: Awaited<ReturnType<typeof pageService.getList>>): WorkspaceView[] {
  const now = new Date().toISOString()
  const firstBoard = boards[0]
  const firstPage = pages[0]
  const views: WorkspaceView[] = []

  if (firstBoard) {
    views.push({
      id: "table",
      workspaceId,
      name: "Main Table",
      type: "table",
      icon: "Table",
      description: "Workspace tasks in table form",
      target: { boardId: firstBoard.id },
      config: {},
      visibility: "workspace",
      isDefault: true,
      position: 1,
      createdAt: now,
    })
    views.push({
      id: "kanban",
      workspaceId,
      name: "Kanban",
      type: "kanban",
      icon: "Kanban",
      description: "Board cards grouped by list",
      target: { boardId: firstBoard.id },
      config: {},
      visibility: "workspace",
      isDefault: false,
      position: 2,
      createdAt: now,
    })
  }

  if (firstPage) {
    views.push({
      id: "docs",
      workspaceId,
      name: "Docs",
      type: "doc",
      icon: "FileText",
      description: "Workspace documentation",
      target: { pageId: firstPage.id },
      config: {},
      visibility: "workspace",
      isDefault: views.length === 0,
      position: 3,
      createdAt: now,
    })
  }

  views.push({
    id: "dashboard",
    workspaceId,
    name: "Dashboard",
    type: "dashboard",
    icon: "Gauge",
    description: "Workspace overview",
    target: {},
    config: {},
    visibility: "workspace",
    isDefault: views.length === 0,
    position: 99,
    createdAt: now,
  })

  return views
}

function mapActivity(response: WorkspaceActivityResponseApi): WorkspaceActivityItem[] {
  return response.data.map((item) => ({
    id: item.id,
    actor: "Workspace",
    action: item.action,
    target: item.resourceTitle ?? "item",
    createdAt: item.createdAt,
  }))
}

export const workspaceService = {
  async listWorkspaces(): Promise<WorkspaceSummary[]> {
    const workspaces = await api.get<WorkspaceDtoApi[]>(endpoints.workspaces.list)
    return workspaces.map(mapWorkspaceDto)
  },

  async getWorkspace(workspaceId: string): Promise<WorkspaceSummary> {
    const workspace = await api.get<WorkspaceDtoApi>(endpoints.workspaces.detail(workspaceId))
    return mapWorkspaceDto(workspace)
  },

  async getMembers(workspaceId: string): Promise<WorkspaceMember[]> {
    const members = await api.get<WorkspaceMemberDtoApi[]>(`${endpoints.workspaces.detail(workspaceId)}/members`)
    return members.map(mapMemberDto)
  },

  async getViews(workspaceId: string): Promise<WorkspaceView[]> {
    const [boards, pages, workspace] = await Promise.all([
      boardApi.getBoardsByWorkspaceId(workspaceId),
      pageService.getList(workspaceId),
      this.getWorkspace(workspaceId),
    ])
    const defaultViews = createViews(workspaceId, boards, pages)
    
    let customViews: WorkspaceView[] = []
    let orderIds: string[] = []
    
    if (workspace.settings) {
      try {
        const settingsObj = JSON.parse(workspace.settings)
        if (settingsObj.customViews) {
          customViews = settingsObj.customViews
        }
        if (settingsObj.customViewsOrder) {
          orderIds = settingsObj.customViewsOrder
        }
      } catch (e) {
        console.error("Lỗi đọc custom views từ settings:", e)
      }
    }
    
    const allViews = [...defaultViews, ...customViews]
    
    if (orderIds.length > 0) {
      allViews.sort((a, b) => {
        const indexA = orderIds.indexOf(a.id)
        const indexB = orderIds.indexOf(b.id)
        if (indexA !== -1 && indexB !== -1) {
          return indexA - indexB
        }
        if (indexA !== -1) return -1
        if (indexB !== -1) return 1
        return (a.position ?? 0) - (b.position ?? 0)
      })
    } else {
      allViews.sort((a, b) => (a.position ?? 0) - (b.position ?? 0))
    }
    
    return allViews
  },

  async reorderViews(workspaceId: string, orderedViewIds: string[]): Promise<void> {
    const workspace = await this.getWorkspace(workspaceId)
    let settingsObj: any = {}
    if (workspace.settings) {
      try {
        settingsObj = JSON.parse(workspace.settings)
      } catch (e) {
        console.error("Lỗi parse settings:", e)
      }
    }
    settingsObj.customViewsOrder = orderedViewIds
    await api.patch(endpoints.workspaces.detail(workspaceId), {
      settings: JSON.stringify(settingsObj)
    })
  },

  async getSnapshot(workspaceId: string): Promise<WorkspaceSnapshot> {
    const [workspace, members, views, activity] = await Promise.all([
      this.getWorkspace(workspaceId),
      this.getMembers(workspaceId),
      this.getViews(workspaceId),
      api.get<WorkspaceActivityResponseApi>(`${endpoints.workspaces.detail(workspaceId)}/activity`).then(mapActivity).catch(() => []),
    ])

    return {
      workspace,
      members,
      views,
      favorites: views.slice(0, 3).map((view) => ({
        id: view.id,
        title: view.name,
        type: "view",
        icon: view.icon,
        href: `/${workspaceId}?view=${view.id}`,
      })),
      recent: views.slice(0, 5).map((view) => ({
        id: view.id,
        title: view.name,
        type: "view",
        icon: view.icon,
        href: `/${workspaceId}?view=${view.id}`,
        updatedAt: view.updatedAt ?? view.createdAt,
      })),
      activity,
    }
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
    let settingsObj: any = {}
    if (workspace.settings) {
      try {
        settingsObj = JSON.parse(workspace.settings)
      } catch (e) {
        console.error("Lỗi parse settings:", e)
      }
    }
    
    const currentViews: WorkspaceView[] = settingsObj.customViews || []
    currentViews.push(view)
    settingsObj.customViews = currentViews
    
    await api.patch(endpoints.workspaces.detail(input.workspaceId), {
      settings: JSON.stringify(settingsObj)
    })
    return view
  },

  async updateView(workspaceId: string, viewId: string, input: UpdateWorkspaceViewInput): Promise<WorkspaceView> {
    const views = await this.getViews(workspaceId)
    const current = views.find((view) => view.id === viewId)
    return {
      ...(current ?? views[0]),
      id: viewId,
      workspaceId,
      ...input,
      config: { ...(current?.config ?? {}), ...input.config },
      updatedAt: new Date().toISOString(),
    }
  },

  async createWorkspace(input: { name: string; slug: string; isPersonal: boolean }): Promise<WorkspaceSummary> {
    const res = await api.post<WorkspaceDtoApi>(endpoints.workspaces.list, input)
    return mapWorkspaceDto(res)
  },

  async updateWorkspace(workspaceId: string, input: { name?: string; slug?: string; settings?: string }): Promise<WorkspaceSummary> {
    const res = await api.patch<WorkspaceDtoApi>(endpoints.workspaces.detail(workspaceId), input)
    return mapWorkspaceDto(res)
  },

  async updateMemberRole(workspaceId: string, userId: string, role: string): Promise<WorkspaceMember> {
    const res = await api.patch<WorkspaceMemberDtoApi>(`${endpoints.workspaces.detail(workspaceId)}/members/${userId}`, { role })
    return mapMemberDto(res, 0)
  },

  async removeMember(workspaceId: string, userId: string): Promise<void> {
    await api.delete<void>(`${endpoints.workspaces.detail(workspaceId)}/members/${userId}`)
  },

  async getInvitations(workspaceId: string): Promise<any[]> {
    const invitations = await api.get<any[]>(`${endpoints.workspaces.detail(workspaceId)}/invitations`)
    return invitations.map((inv) => ({
      id: inv.id,
      email: inv.email,
      role: inv.role,
      expiresAt: inv.expiresAt,
      isAccepted: inv.isAccepted,
      createdAt: inv.createdAt,
    }))
  },

  async createInvitation(workspaceId: string, email: string, role: string): Promise<any> {
    const res = await api.post<any>(`${endpoints.workspaces.detail(workspaceId)}/invitations`, { email, role })
    return res
  },

  async deleteInvitation(workspaceId: string, invitationId: string): Promise<void> {
    await api.delete<void>(`${endpoints.workspaces.detail(workspaceId)}/invitations/${invitationId}`)
  },

  async getActivityLogs(workspaceId: string, page = 1, pageSize = 20): Promise<WorkspaceActivityItem[]> {
    const res = await api.get<WorkspaceActivityResponseApi>(`${endpoints.workspaces.detail(workspaceId)}/activity?page=${page}&pageSize=${pageSize}`)
    return mapActivity(res)
  },

  async getInvitationByToken(token: string): Promise<any> {
    const res = await api.get<any>(`/api/v1/workspaces/invitations/by-token/${token}`)
    return res
  },

  async acceptInvitation(token: string): Promise<{ workspaceSlug: string; workspaceId: string }> {
    const res = await api.post<{ workspaceSlug: string; workspaceId: string }>(`/api/v1/workspaces/invitations/accept/${token}`, {})
    return res
  },

  async getPendingInvitations(): Promise<any[]> {
    const res = await api.get<any[]>("/api/v1/workspaces/invitations/pending")
    return res
  },
}
