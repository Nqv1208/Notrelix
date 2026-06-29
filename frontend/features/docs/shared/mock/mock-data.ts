import type { Block } from "../../blocks/types/block.types"
import type { DocsWorkspaceSnapshot } from "../types/snapshot.types"
import type { Page, PageActivity } from "../../pages/types/page.types"
import type { PageComment } from "../../comments/types/comment.types"
import type { PageTemplate } from "../../templates/types/template.types"

const now = "2026-05-12T08:30:00.000Z"

const users = [
  {
    id: "u-ana",
    name: "Ana Moreno",
    email: "ana@notrelix.test",
    avatarUrl: null,
    color: "#6161ff",
    role: "owner" as const,
  },
  {
    id: "u-minh",
    name: "Minh Tran",
    email: "minh@notrelix.test",
    avatarUrl: null,
    color: "#2a9d99",
    role: "editor" as const,
  },
  {
    id: "u-sam",
    name: "Sam Carter",
    email: "sam@notrelix.test",
    avatarUrl: null,
    color: "#ff8940",
    role: "commenter" as const,
  },
  {
    id: "u-ivy",
    name: "Ivy Chen",
    email: "ivy@notrelix.test",
    avatarUrl: null,
    color: "#0075de",
    role: "viewer" as const,
  },
]

const page = (input: Partial<Page> & Pick<Page, "id" | "title" | "position">): Page => ({
  workspaceId: "notrelix-os",
  workspaceSlug: "notrelix-os",
  icon: "📄",
  coverUrl: null,
  coverColor: "#e7ecff",
  parentId: null,
  status: "draft",
  isPublished: false,
  isFavorited: false,
  isShared: false,
  tags: [],
  authorId: "u-ana",
  lastEditedById: "u-minh",
  lastEditedAt: now,
  createdAt: "2026-04-20T09:00:00.000Z",
  updatedAt: now,
  collaboratorIds: ["u-ana", "u-minh"],
  metadata: {
    version: 12,
    lockOwnerId: null,
    activeUserIds: ["u-ana", "u-minh", "u-sam"],
    lastSyncedAt: now,
    realtimeChannel: "docs:notrelix-os",
    aiSummaryStatus: "ready",
  },
  linkedTaskIds: [],
  linkedBoardIds: [],
  ...input,
})

export const mockPages: Page[] = [
  page({
    id: "strategy-hub",
    title: "Strategy hub",
    icon: "🧭",
    coverColor: "#e7ecff",
    position: 1,
    status: "published",
    isFavorited: true,
    isShared: true,
    tags: ["Leadership", "Planning"],
    linkedBoardIds: ["board-roadmap"],
  }),
  page({
    id: "q3-operating-plan",
    title: "Q3 operating plan",
    icon: "📈",
    parentId: "strategy-hub",
    coverColor: "#bcfe90",
    position: 1.1,
    status: "review",
    isFavorited: true,
    tags: ["OKR", "Finance"],
    linkedTaskIds: ["task-pricing", "task-launch"],
  }),
  page({
    id: "company-principles",
    title: "Company principles",
    icon: "✨",
    parentId: "strategy-hub",
    coverColor: "#eddff7",
    position: 1.2,
    status: "published",
    isShared: true,
    tags: ["Culture"],
  }),
  page({
    id: "product-specs",
    title: "Product specs",
    icon: "🧩",
    coverColor: "#abf0ff",
    position: 2,
    isFavorited: true,
    tags: ["Product", "Specs"],
    linkedBoardIds: ["board-product"],
  }),
  page({
    id: "docs-mvp-spec",
    title: "Docs MVP specification",
    icon: "📝",
    parentId: "product-specs",
    coverColor: "#d1faff",
    position: 2.1,
    status: "review",
    tags: ["Docs", "Editor"],
    linkedTaskIds: ["task-docs-editor"],
    linkedBoardIds: ["board-product"],
  }),
  page({
    id: "calendar-sync-brief",
    title: "Calendar sync brief",
    icon: "📅",
    parentId: "product-specs",
    coverColor: "#ffc95e",
    position: 2.2,
    tags: ["Calendar", "Integration"],
  }),
  page({
    id: "research-library",
    title: "Research library",
    icon: "🔎",
    coverColor: "#93beff",
    position: 3,
    isShared: true,
    tags: ["Research"],
  }),
  page({
    id: "customer-interviews",
    title: "Customer interviews",
    icon: "🎙️",
    parentId: "research-library",
    coverColor: "#ff83dd",
    position: 3.1,
    tags: ["Voice of customer"],
  }),
  page({
    id: "meeting-notes",
    title: "Meeting notes",
    icon: "📌",
    coverColor: "#f6f5f4",
    position: 4,
    tags: ["Meetings"],
  }),
]

const block = (
  pageId: string,
  id: string,
  type: Block["type"],
  position: number,
  text: string,
  properties: Block["properties"] = {}
): Block => ({
  id,
  pageId,
  type,
  properties: { text, ...properties },
  position,
  parentId: null,
  createdById: "u-ana",
  updatedById: "u-minh",
  createdAt: "2026-04-20T09:00:00.000Z",
  updatedAt: now,
})

export const mockBlocks: Record<string, Block[]> = {
  "docs-mvp-spec": [
    block("docs-mvp-spec", "b-1", "paragraph", 1, "This specification defines the first production-grade Docs experience for Notrelix: page hierarchy, collaborative metadata, block editing, references, comments, and workspace search."),
    block("docs-mvp-spec", "b-2", "heading_2", 2, "MVP outcomes"),
    block("docs-mvp-spec", "b-3", "todo", 3, "Ship workspace-level docs overview", { checked: true }),
    block("docs-mvp-spec", "b-4", "todo", 4, "Support block creation through slash commands", { checked: false }),
    block("docs-mvp-spec", "b-5", "todo", 5, "Connect page references to boards and tasks", { checked: false }),
    block("docs-mvp-spec", "b-6", "callout", 6, "Real API integration will replace the in-memory mock service without changing component contracts.", { icon: "💡", color: "#e7ecff" }),
    block("docs-mvp-spec", "b-7", "heading_2", 7, "Editor model"),
    block("docs-mvp-spec", "b-8", "paragraph", 8, "Blocks are polymorphic records with stable IDs, fractional positions, typed properties, and collaborative metadata. This keeps the editor compatible with future CRDT and AI operations."),
    block("docs-mvp-spec", "b-9", "table", 9, "", {
      rows: [
        ["Capability", "MVP", "Future"],
        ["Comments", "Thread panel", "Inline anchors"],
        ["Realtime", "Presence mock", "WebSocket + CRDT"],
        ["Search", "Client fuzzy", "Server-ranked"],
      ],
    }),
    block("docs-mvp-spec", "b-10", "board_reference", 10, "", { linkedBoardId: "board-product", title: "Product delivery board" }),
    block("docs-mvp-spec", "b-11", "code", 11, "GET /api/v1/pages/:id/blocks\nPATCH /api/v1/blocks/:blockId\nPOST /api/v1/blocks/reorder", { language: "http" }),
  ],
  "q3-operating-plan": [
    block("q3-operating-plan", "b-q3-1", "paragraph", 1, "Q3 focuses on activation, admin reliability, and calendar-led workflows for product teams."),
    block("q3-operating-plan", "b-q3-2", "heading_2", 2, "Operating bets"),
    block("q3-operating-plan", "b-q3-3", "numbered_list", 3, "Make docs the source of truth for project planning."),
    block("q3-operating-plan", "b-q3-4", "numbered_list", 4, "Connect every major project page to a board and calendar milestone."),
    block("q3-operating-plan", "b-q3-5", "quote", 5, "Calm tools win when they make complex work feel inspectable."),
  ],
  "strategy-hub": [
    block("strategy-hub", "b-s-1", "paragraph", 1, "A shared command center for leadership decisions, operating cadence, and cross-functional planning."),
    block("strategy-hub", "b-s-2", "page_reference", 2, "", { linkedPageId: "q3-operating-plan", title: "Q3 operating plan" }),
    block("strategy-hub", "b-s-3", "page_reference", 3, "", { linkedPageId: "company-principles", title: "Company principles" }),
  ],
}

export const mockBoards = [
  { id: "board-product", name: "Product delivery", color: "#6161ff", openTasks: 18, doneTasks: 42 },
  { id: "board-roadmap", name: "Roadmap planning", color: "#2a9d99", openTasks: 9, doneTasks: 21 },
]

export const mockTasks = [
  { id: "task-docs-editor", title: "Prototype block editor interactions", status: "in_progress" as const, dueDate: "2026-05-28", assigneeId: "u-minh", boardId: "board-product" },
  { id: "task-pricing", title: "Finalize pricing packaging", status: "blocked" as const, dueDate: "2026-06-03", assigneeId: "u-ana", boardId: "board-roadmap" },
  { id: "task-launch", title: "Prepare beta launch checklist", status: "todo" as const, dueDate: "2026-06-10", assigneeId: "u-sam", boardId: "board-roadmap" },
]

export const mockComments: Record<string, PageComment[]> = {
  "docs-mvp-spec": [
    {
      id: "c-1",
      pageId: "docs-mvp-spec",
      blockId: "b-8",
      authorId: "u-sam",
      body: "Can we keep this API-shaped so mobile can reuse the same page payload?",
      mentionIds: ["u-minh"],
      resolved: false,
      createdAt: "2026-05-12T06:10:00.000Z",
      updatedAt: "2026-05-12T06:10:00.000Z",
    },
    {
      id: "c-2",
      pageId: "docs-mvp-spec",
      blockId: null,
      authorId: "u-ana",
      body: "Design review approved for MVP scope. Keep comments visible in the right rail.",
      mentionIds: [],
      resolved: true,
      createdAt: "2026-05-11T15:40:00.000Z",
      updatedAt: "2026-05-11T15:40:00.000Z",
    },
  ],
}

export const mockActivity: Record<string, PageActivity[]> = {
  "docs-mvp-spec": [
    { id: "a-1", pageId: "docs-mvp-spec", actorId: "u-minh", action: "edited", targetLabel: "Editor model", createdAt: "2026-05-12T08:10:00.000Z" },
    { id: "a-2", pageId: "docs-mvp-spec", actorId: "u-sam", action: "commented", targetLabel: "API migration note", createdAt: "2026-05-12T06:10:00.000Z" },
    { id: "a-3", pageId: "docs-mvp-spec", actorId: "u-ana", action: "shared", targetLabel: "Product leadership", createdAt: "2026-05-11T16:00:00.000Z" },
  ],
}

export const mockTemplates: PageTemplate[] = [
  { id: "tpl-spec", name: "Product spec", description: "Problem, decisions, scope, rollout.", icon: "🧩", accent: "#abf0ff", blockTypes: ["heading_2", "paragraph", "todo", "table"] },
  { id: "tpl-meeting", name: "Meeting notes", description: "Agenda, decisions, owners.", icon: "📌", accent: "#ffc95e", blockTypes: ["heading_2", "todo", "paragraph"] },
  { id: "tpl-retro", name: "Team retro", description: "Wins, gaps, actions.", icon: "🔁", accent: "#eddff7", blockTypes: ["callout", "bulleted_list", "todo"] },
]

export const mockDocsWorkspace: DocsWorkspaceSnapshot = {
  id: "notrelix-os",
  slug: "notrelix-os",
  name: "Notrelix OS",
  icon: "◇",
  users,
  pages: mockPages,
  blocks: mockBlocks,
  comments: mockComments,
  activity: mockActivity,
  tasks: mockTasks,
  boards: mockBoards,
  templates: mockTemplates,
  recentSearches: ["docs mvp", "calendar sync", "q3"],
}
