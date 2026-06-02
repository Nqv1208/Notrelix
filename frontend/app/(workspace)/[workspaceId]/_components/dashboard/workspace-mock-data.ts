// Static mock data for workspace overview — designed for 1:1 swap with TanStack Query hooks

export type WorkspaceInfo = {
  id: string
  name: string
  slug: string
  icon: string
  plan: "free" | "pro" | "enterprise"
  memberCount: number
}

export type PinnedDoc = {
  id: string
  title: string
  icon: string
  updatedAt: string
  updatedBy: string
  status: "published" | "draft"
}

export type ActiveBoard = {
  id: string
  title: string
  accentColor: string
  totalCards: number
  completedCards: number
  memberAvatars: string[]
  dueDate: string | null
}

export type UpcomingDeadline = {
  id: string
  title: string
  dueDate: string
  priority: "urgent" | "high" | "medium" | "low"
  source: string
  sourceType: "board" | "doc"
  assignee: string
}

export type ActivityItem = {
  id: string
  actor: string
  actorInitials: string
  action: string
  target: string
  timestamp: string
}

export type WorkspaceStats = {
  totalPages: number
  activeBoards: number
  pendingTasks: number
  teamMembers: number
}

// ── Mock Data ──

export const WORKSPACE: WorkspaceInfo = {
  id: "ws-001",
  name: "Notrelix HQ",
  slug: "notrelix-hq",
  icon: "🚀",
  plan: "pro",
  memberCount: 12,
}

export const WORKSPACE_STATS: WorkspaceStats = {
  totalPages: 47,
  activeBoards: 5,
  pendingTasks: 23,
  teamMembers: 12,
}

export const PINNED_DOCS: PinnedDoc[] = [
  {
    id: "p-001",
    title: "Product Roadmap Q3 2026",
    icon: "🗺️",
    updatedAt: "2h ago",
    updatedBy: "Anna",
    status: "published",
  },
  {
    id: "p-002",
    title: "Engineering Design Spec",
    icon: "⚙️",
    updatedAt: "4h ago",
    updatedBy: "Michael",
    status: "published",
  },
  {
    id: "p-003",
    title: "Brand Guidelines v2",
    icon: "🎨",
    updatedAt: "1d ago",
    updatedBy: "Sophie",
    status: "draft",
  },
  {
    id: "p-004",
    title: "Sprint Retrospective Notes",
    icon: "📝",
    updatedAt: "2d ago",
    updatedBy: "David",
    status: "published",
  },
  {
    id: "p-005",
    title: "Onboarding Playbook",
    icon: "📚",
    updatedAt: "3d ago",
    updatedBy: "Emma",
    status: "published",
  },
]

export const ACTIVE_BOARDS: ActiveBoard[] = [
  {
    id: "b-001",
    title: "Sprint 14 — Core Features",
    accentColor: "#6161ff",
    totalCards: 18,
    completedCards: 11,
    memberAvatars: ["A", "M", "S"],
    dueDate: "May 15",
  },
  {
    id: "b-002",
    title: "Design System v2",
    accentColor: "#9450fd",
    totalCards: 12,
    completedCards: 5,
    memberAvatars: ["S", "E"],
    dueDate: "May 22",
  },
  {
    id: "b-003",
    title: "Marketing Launch",
    accentColor: "#ff8940",
    totalCards: 9,
    completedCards: 2,
    memberAvatars: ["D", "A", "J"],
    dueDate: "Jun 1",
  },
  {
    id: "b-004",
    title: "Infrastructure & DevOps",
    accentColor: "#2a9d99",
    totalCards: 7,
    completedCards: 4,
    memberAvatars: ["M", "K"],
    dueDate: null,
  },
]

export const UPCOMING_DEADLINES: UpcomingDeadline[] = [
  {
    id: "d-001",
    title: "Finalize API contracts",
    dueDate: "Today",
    priority: "urgent",
    source: "Sprint 14",
    sourceType: "board",
    assignee: "M",
  },
  {
    id: "d-002",
    title: "Design review — Card detail",
    dueDate: "Today",
    priority: "high",
    source: "Design System v2",
    sourceType: "board",
    assignee: "S",
  },
  {
    id: "d-003",
    title: "Write onboarding flow copy",
    dueDate: "Tomorrow",
    priority: "medium",
    source: "Marketing Launch",
    sourceType: "board",
    assignee: "A",
  },
  {
    id: "d-004",
    title: "Publish Brand Guidelines",
    dueDate: "Tomorrow",
    priority: "medium",
    source: "Brand Guidelines v2",
    sourceType: "doc",
    assignee: "S",
  },
  {
    id: "d-005",
    title: "Set up CI/CD pipeline",
    dueDate: "May 12",
    priority: "high",
    source: "Infrastructure",
    sourceType: "board",
    assignee: "K",
  },
  {
    id: "d-006",
    title: "Landing page A/B test",
    dueDate: "May 14",
    priority: "low",
    source: "Marketing Launch",
    sourceType: "board",
    assignee: "D",
  },
]

export const ACTIVITY_FEED: ActivityItem[] = [
  {
    id: "a-001",
    actor: "Anna",
    actorInitials: "AN",
    action: "updated",
    target: "Product Roadmap Q3 2026",
    timestamp: "12m ago",
  },
  {
    id: "a-002",
    actor: "Michael",
    actorInitials: "MI",
    action: "moved 3 cards in",
    target: "Sprint 14",
    timestamp: "28m ago",
  },
  {
    id: "a-003",
    actor: "Sophie",
    actorInitials: "SO",
    action: "completed checklist in",
    target: "Design System v2",
    timestamp: "1h ago",
  },
  {
    id: "a-004",
    actor: "David",
    actorInitials: "DA",
    action: "created new board",
    target: "Marketing Launch",
    timestamp: "2h ago",
  },
  {
    id: "a-005",
    actor: "Emma",
    actorInitials: "EM",
    action: "commented on",
    target: "Onboarding Playbook",
    timestamp: "3h ago",
  },
  {
    id: "a-006",
    actor: "Kevin",
    actorInitials: "KE",
    action: "deployed changes to",
    target: "Infrastructure & DevOps",
    timestamp: "4h ago",
  },
  {
    id: "a-007",
    actor: "Anna",
    actorInitials: "AN",
    action: "pinned",
    target: "Sprint Retrospective Notes",
    timestamp: "5h ago",
  },
]

export const MEMBER_INITIALS = ["AN", "MI", "SO", "DA", "EM", "KE", "JA", "LI", "TO", "RO", "CH", "MA"]
