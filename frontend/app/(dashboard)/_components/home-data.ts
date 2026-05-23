export const recentWorkspaces = [
  { id: "workspace-notrelix-os", slug: "notrelix-os", name: "Notrelix OS", icon: "N", color: "#6161ff", members: 12, updatedAt: "12m ago" },
  { id: "workspace-growth-lab", slug: "growth-lab", name: "Growth Lab", icon: "G", color: "#2a9d99", members: 8, updatedAt: "1h ago" },
  { id: "workspace-design-studio", slug: "design-studio", name: "Design Studio", icon: "D", color: "#ff8940", members: 5, updatedAt: "Yesterday" },
]

export const recentDocs = [
  { id: "docs-mvp-spec", workspaceId: "workspace-notrelix-os", title: "Docs MVP specification", icon: "📝", updatedAt: "12m ago", owner: "Minh" },
  { id: "q3-operating-plan", workspaceId: "workspace-notrelix-os", title: "Q3 operating plan", icon: "📈", updatedAt: "34m ago", owner: "Ana" },
  { id: "customer-interviews", workspaceId: "workspace-notrelix-os", title: "Customer interviews", icon: "🎙️", updatedAt: "2h ago", owner: "Ivy" },
]

export const recentBoards = [
  { id: "board-product", workspaceId: "workspace-notrelix-os", title: "Product delivery", color: "#6161ff", progress: 68, updatedAt: "28m ago" },
  { id: "board-roadmap", workspaceId: "workspace-notrelix-os", title: "Roadmap planning", color: "#2a9d99", progress: 43, updatedAt: "1h ago" },
  { id: "board-design", workspaceId: "workspace-design-studio", title: "Design QA", color: "#ff8940", progress: 81, updatedAt: "Yesterday" },
]

export const homeActivity = [
  { actor: "Ana", action: "published", target: "Q3 operating plan", time: "12m ago" },
  { actor: "Minh", action: "moved 3 tasks in", target: "Product delivery", time: "28m ago" },
  { actor: "Sam", action: "commented on", target: "Docs MVP specification", time: "1h ago" },
]
