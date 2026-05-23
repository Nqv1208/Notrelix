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
    const [boards, pages] = await Promise.all([
      boardApi.getBoardsByWorkspaceId(workspaceId),
      pageService.getList(workspaceId),
    ])
    return createViews(workspaceId, boards, pages)
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
    return {
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
}
