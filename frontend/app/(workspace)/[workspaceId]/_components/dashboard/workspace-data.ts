export type WorkspaceMember = {
  id: string
  name: string
  initials: string
  role: string
  status: "active" | "idle" | "offline" | "in-call"
  workload: number
  color: string
}

export type WorkspaceAsset = {
  id: string
  type: "doc" | "board" | "dashboard"
  title: string
  icon: string
  updatedAt: string
  owner: string
  href: string
}

export const workspaceMembers: WorkspaceMember[] = [
  { id: "u-ana", name: "Ana Moreno", initials: "AM", role: "Product Lead", status: "active", workload: 76, color: "var(--primary)" },
  { id: "u-minh", name: "Minh Tran", initials: "MT", role: "Frontend", status: "in-call", workload: 88, color: "var(--color-surface-teal)" },
  { id: "u-sam", name: "Sam Carter", initials: "SC", role: "Design", status: "active", workload: 62, color: "var(--color-surface-sunset)" },
  { id: "u-ivy", name: "Ivy Chen", initials: "IC", role: "Research", status: "idle", workload: 41, color: "var(--color-brand-ocean)" },
]

export const workspaceAssets: WorkspaceAsset[] = [
  { id: "docs-mvp-spec", type: "doc", title: "Docs MVP specification", icon: "📝", updatedAt: "12m ago", owner: "Minh", href: "/docs/docs-mvp-spec" },
  { id: "q3-operating-plan", type: "doc", title: "Q3 operating plan", icon: "📈", updatedAt: "34m ago", owner: "Ana", href: "/docs/q3-operating-plan" },
  { id: "board-product", type: "board", title: "Product delivery", icon: "▦", updatedAt: "1h ago", owner: "Sam", href: "/boards/board-product" },
  { id: "board-roadmap", type: "board", title: "Roadmap planning", icon: "▦", updatedAt: "2h ago", owner: "Ana", href: "/boards/board-roadmap" },
  { id: "workspace-health", type: "dashboard", title: "Workspace health", icon: "◌", updatedAt: "Today", owner: "Team", href: "/" },
]

export const workspaceTasks = [
  { id: "t1", title: "Prototype block formatting toolbar", assignee: "Minh", status: "In progress", due: "Today", board: "Product delivery", color: "var(--primary)" },
  { id: "t2", title: "Review onboarding document structure", assignee: "Sam", status: "Review", due: "Tomorrow", board: "Roadmap planning", color: "var(--color-surface-sunset)" },
  { id: "t3", title: "Summarize customer interview notes", assignee: "Ivy", status: "Done", due: "May 13", board: "Research", color: "var(--color-success)" },
  { id: "t4", title: "Prepare sprint planning agenda", assignee: "Ana", status: "Blocked", due: "May 15", board: "Product delivery", color: "var(--destructive)" },
]

export const workspaceChatMessages = [
  {
    id: "chat-1",
    author: "Ana Moreno",
    initials: "AM",
    color: "var(--primary)",
    time: "09:12",
    body: "I moved the Docs MVP spec into review. Please keep product notes and board tasks linked before end of day.",
    channel: "Project room",
  },
  {
    id: "chat-2",
    author: "Minh Tran",
    initials: "MT",
    color: "var(--color-surface-teal)",
    time: "09:18",
    body: "Formatting toolbar is now the main editor focus. I am checking heading, font, size, color, and alignment interactions.",
    channel: "Docs",
  },
  {
    id: "chat-3",
    author: "Sam Carter",
    initials: "SC",
    color: "var(--color-surface-sunset)",
    time: "09:31",
    body: "Design review: keep the workspace calmer than a dashboard, but make status and ownership scan fast.",
    channel: "Design",
  },
]
