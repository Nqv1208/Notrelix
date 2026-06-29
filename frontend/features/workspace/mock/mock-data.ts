import type { WorkspaceSnapshot, WorkspaceView, WorkspaceViewType } from "../types"

const workspaceSlug = "notrelix-os"
const workspaceId = "workspace-notrelix-os"

function view(input: Omit<WorkspaceView, "workspaceId" | "visibility" | "createdAt">): WorkspaceView {
  return {
    workspaceId,
    visibility: "workspace",
    createdAt: "2026-05-01T08:00:00.000Z",
    ...input,
  }
}

export const mockWorkspaceViews = [
  view({
    id: "table",
    name: "Main table",
    type: "table",
    icon: "▦",
    description: "Grouped task table powered by board lists and custom fields.",
    target: { boardId: "board-product" },
    config: { groupBy: "list", density: "default" },
    isDefault: true,
    position: 1,
    updatedAt: "2026-05-13T08:00:00.000Z",
  }),
  view({
    id: "doc",
    name: "Doc",
    type: "doc",
    icon: "□",
    description: "Project source-of-truth page with blocks, comments, and linked work.",
    target: { pageId: "docs-mvp-spec" },
    config: {},
    isDefault: false,
    position: 2,
    updatedAt: "2026-05-13T08:10:00.000Z",
  }),
  view({
    id: "kanban",
    name: "Kanban",
    type: "kanban",
    icon: "▥",
    description: "Status-oriented board view for moving cards across lists.",
    target: { boardId: "board-product" },
    config: { groupBy: "list", density: "compact" },
    isDefault: false,
    position: 3,
    updatedAt: "2026-05-13T08:20:00.000Z",
  }),
  view({
    id: "calendar",
    name: "Calendar",
    type: "calendar",
    icon: "◇",
    description: "Unified workspace schedule for tasks, docs, and milestones.",
    target: { calendarId: "workspace-calendar" },
    config: {},
    isDefault: false,
    position: 4,
    updatedAt: "2026-05-13T08:30:00.000Z",
  }),
  view({
    id: "timeline",
    name: "Timeline",
    type: "timeline",
    icon: "═",
    description: "Gantt-like timeline that reads dates from cards and milestones.",
    target: { boardId: "board-product" },
    config: {},
    isDefault: false,
    position: 5,
    updatedAt: "2026-05-13T08:40:00.000Z",
  }),
  view({
    id: "dashboard",
    name: "Dashboard",
    type: "dashboard",
    icon: "◌",
    description: "Workspace pulse with team workload, activity, and delivery signals.",
    target: { dashboardId: "workspace-health" },
    config: {},
    isDefault: false,
    position: 6,
    updatedAt: "2026-05-13T08:50:00.000Z",
  }),
] satisfies WorkspaceView[]

export const mockWorkspaceSnapshot: WorkspaceSnapshot = {
  workspace: {
    id: workspaceId,
    slug: workspaceSlug,
    name: "Notrelix OS",
    description: "Plan, write, track, discuss, and ship from one workspace.",
    icon: "N",
    plan: "business",
    memberCount: 4,
    isPersonal: false,
  },
  members: [
    { id: "wm-ana", userId: "u-ana", name: "Ana Moreno", initials: "AM", role: "owner", status: "active", workload: 76, color: "var(--primary)" },
    { id: "wm-minh", userId: "u-minh", name: "Minh Tran", initials: "MT", role: "member", status: "in-call", workload: 88, color: "var(--color-surface-teal)" },
    { id: "wm-sam", userId: "u-sam", name: "Sam Carter", initials: "SC", role: "member", status: "active", workload: 62, color: "var(--color-surface-sunset)" },
    { id: "wm-ivy", userId: "u-ivy", name: "Ivy Chen", initials: "IC", role: "guest", status: "idle", workload: 41, color: "var(--color-brand-ocean)" },
  ],
  views: mockWorkspaceViews,
  favorites: [
    { id: "fav-table", title: "Product delivery", type: "view", icon: "▦", href: `/${workspaceId}/boards/board-product?view=table` },
    { id: "fav-doc", title: "Docs MVP specification", type: "doc", icon: "□", href: `/${workspaceId}/docs/docs-mvp-spec` },
    { id: "fav-dashboard", title: "Workspace health", type: "dashboard", icon: "◌", href: `/${workspaceId}/dashboard` },
  ],
  recent: [
    { id: "recent-table", title: "Main table", type: "view", icon: "▦", href: `/${workspaceId}/boards/board-product?view=table`, updatedAt: "12m ago" },
    { id: "recent-chat", title: "Project room", type: "chat", icon: "#", href: `/${workspaceId}/chat`, updatedAt: "18m ago" },
    { id: "recent-roadmap", title: "Roadmap planning", type: "board", icon: "▥", href: `/${workspaceId}/boards/board-roadmap?view=table`, updatedAt: "1h ago" },
    { id: "recent-q3", title: "Q3 operating plan", type: "doc", icon: "□", href: `/${workspaceId}/docs/q3-operating-plan`, updatedAt: "2h ago" },
  ],
  activity: [
    { id: "activity-1", actor: "Ana", action: "published", target: "Q3 operating plan", createdAt: "12m ago" },
    { id: "activity-2", actor: "Minh", action: "moved", target: "3 cards in Product delivery", createdAt: "28m ago" },
    { id: "activity-3", actor: "Sam", action: "commented on", target: "Docs MVP specification", createdAt: "1h ago" },
    { id: "activity-4", actor: "Ivy", action: "summarized", target: "Customer interviews", createdAt: "2h ago" },
  ],
}

export { workspaceViewTemplates } from "../constants/view-templates"
