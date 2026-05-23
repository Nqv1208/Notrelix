export type WorkspaceViewType =
  | "table"
  | "doc"
  | "kanban"
  | "calendar"
  | "timeline"
  | "dashboard"
  | "form"
  | "gallery"
  | "chart"
  | "gantt"

export type WorkspaceViewVisibility = "private" | "workspace" | "public"

export interface WorkspaceMember {
  id: string
  userId: string
  name: string
  initials: string
  role: "owner" | "admin" | "member" | "guest"
  status: "active" | "idle" | "offline" | "in-call"
  workload: number
  color: string
  avatarUrl?: string
}

export interface WorkspaceSummary {
  id: string
  slug: string
  name: string
  description?: string
  icon: string
  plan: "free" | "pro" | "business" | "enterprise"
  memberCount: number
  isPersonal: boolean
}

export interface WorkspaceViewTarget {
  boardId?: string
  pageId?: string
  calendarId?: string
  dashboardId?: string
}

export interface WorkspaceViewConfig {
  groupBy?: string
  hiddenFields?: string[]
  columnOrder?: string[]
  density?: "compact" | "default" | "comfortable"
  filters?: Array<{ fieldId: string; operator: string; value: unknown }>
  sortBy?: Array<{ fieldId: string; direction: "asc" | "desc" }>
}

export interface WorkspaceView {
  id: string
  workspaceId: string
  name: string
  type: WorkspaceViewType
  icon: string
  description: string
  target: WorkspaceViewTarget
  config: WorkspaceViewConfig
  visibility: WorkspaceViewVisibility
  isDefault: boolean
  position: number
  createdAt: string
  updatedAt?: string
}

export interface WorkspaceFavorite {
  id: string
  title: string
  type: "view" | "doc" | "board" | "dashboard"
  icon: string
  href: string
}

export interface WorkspaceRecentItem {
  id: string
  title: string
  type: "view" | "doc" | "board" | "task" | "chat"
  icon: string
  href: string
  updatedAt: string
}

export interface WorkspaceActivityItem {
  id: string
  actor: string
  action: string
  target: string
  createdAt: string
}

export interface WorkspaceSnapshot {
  workspace: WorkspaceSummary
  members: WorkspaceMember[]
  views: WorkspaceView[]
  favorites: WorkspaceFavorite[]
  recent: WorkspaceRecentItem[]
  activity: WorkspaceActivityItem[]
}

export interface CreateWorkspaceViewInput {
  workspaceId: string
  name: string
  type: WorkspaceViewType
  target?: WorkspaceViewTarget
}

export interface UpdateWorkspaceViewInput {
  name?: string
  icon?: string
  config?: Partial<WorkspaceViewConfig>
  position?: number
}
