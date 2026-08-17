import type { MockPersona, MockScenario } from "../config/mock-runtime-config";
import type { MockDatabase } from "../state/mock-database";
import { mockIds } from "../state/mock-ids";

const users: MockDatabase["users"] = [
  { id: mockIds.users.owner, email: "owner@notrelix.local", name: "Morgan Owner", avatarUrl: null },
  { id: mockIds.users.admin, email: "admin@notrelix.local", name: "Alex Admin", avatarUrl: null },
  { id: mockIds.users.member, email: "member@notrelix.local", name: "Taylor Member", avatarUrl: null },
  { id: mockIds.users.viewer, email: "viewer@notrelix.local", name: "Jordan Viewer", avatarUrl: null },
];

const workspaces: MockDatabase["workspaces"] = [
  { id: mockIds.workspaces.primary, slug: "notrelix-product-lab", name: "Notrelix Product Lab", description: "Deterministic frontend development workspace", icon: "Layout", plan: "pro", memberCount: 4, isPersonal: false },
];

const timestamp = "2026-08-01T09:00:00.000Z";

export function createMockDatabase(
  persona: MockPersona,
  scenario: MockScenario,
): MockDatabase {
  const database: MockDatabase = {
    users: structuredClone(users),
    workspaces: structuredClone(workspaces),
    memberships: [
      { userId: mockIds.users.owner, workspaceId: mockIds.workspaces.primary, role: "owner" },
      { userId: mockIds.users.admin, workspaceId: mockIds.workspaces.primary, role: "admin" },
      { userId: mockIds.users.member, workspaceId: mockIds.workspaces.primary, role: "member" },
      { userId: mockIds.users.viewer, workspaceId: mockIds.workspaces.primary, role: "guest" },
    ],
    boards: [{ id: mockIds.boards.roadmap, workspaceId: mockIds.workspaces.primary, title: "Product Roadmap", description: "Product delivery plan", background: "#1e90ff", visibility: "workspace", isArchived: false, memberCount: 4, listCount: 3, createdAt: timestamp }],
    boardViews: { [mockIds.boards.roadmap]: { viewMode: "kanban", config: JSON.stringify({ groupBy: "list", hiddenFields: [], columnOrder: [], columnWidths: {}, collapsedGroups: {}, filters: [], sortBy: [] }) } },
    lists: [
      { boardId: mockIds.boards.roadmap, id: mockIds.groups.todo, title: "To do", color: "#64748b", position: 0, isArchived: false, cards: [] },
      { boardId: mockIds.boards.roadmap, id: mockIds.groups.doing, title: "In progress", color: "#3b82f6", position: 1, isArchived: false, cards: [] },
      { boardId: mockIds.boards.roadmap, id: mockIds.groups.done, title: "Done", color: "#22c55e", position: 2, isArchived: false, cards: [] },
    ],
    cards: [
      { id: mockIds.cards.launch, boardId: mockIds.boards.roadmap, workspaceId: mockIds.workspaces.primary, listId: mockIds.groups.doing, title: "Ship mock runtime", descriptionMd: "Certify the production-equivalent frontend path.", priority: "high", status: "in_progress", dueDate: "2026-08-20T09:00:00.000Z", position: 0, members: [], labels: [], checklists: [], commentCount: 0, attachmentCount: 0, createdAt: timestamp },
      { id: mockIds.cards.research, boardId: mockIds.boards.roadmap, workspaceId: mockIds.workspaces.primary, listId: mockIds.groups.todo, title: "Review contract gaps", priority: "medium", status: "todo", position: 0, members: [], labels: [], checklists: [], commentCount: 0, attachmentCount: 0, createdAt: timestamp },
    ],
    pages: [
      { id: mockIds.documents.productSpec, workspaceId: mockIds.workspaces.primary, title: "Product specification", iconValue: "FileText", position: 0, depth: 0, isTemplate: false, isArchived: false, createdAt: timestamp, updatedAt: timestamp },
      { id: mockIds.documents.meetingNotes, workspaceId: mockIds.workspaces.primary, title: "Meeting notes", iconValue: "Notebook", position: 1, depth: 0, isTemplate: false, isArchived: false, createdAt: timestamp, updatedAt: timestamp },
    ],
    blocks: [
      { id: mockIds.blocks.intro, pageId: mockIds.documents.productSpec, type: "paragraph", properties: { text: "Notrelix mock runtime specification." }, position: 0, version: 1, createdByUserId: mockIds.users.owner, createdAt: timestamp, updatedAt: timestamp },
      { id: mockIds.blocks.goals, pageId: mockIds.documents.productSpec, type: "heading_2", properties: { text: "Goals" }, position: 1, version: 1, createdByUserId: mockIds.users.owner, createdAt: timestamp, updatedAt: timestamp },
    ],
    notifications: [
      { id: mockIds.notifications.mention, workspaceId: mockIds.workspaces.primary, userId: mockIds.users.owner, type: "mention", title: "You were mentioned", body: "Review the product specification.", isRead: false, isArchived: false, createdAt: timestamp },
      { id: mockIds.notifications.assignment, workspaceId: mockIds.workspaces.primary, userId: mockIds.users.owner, type: "assignment", title: "Task assigned", body: "Ship mock runtime", isRead: false, isArchived: false, createdAt: timestamp },
    ],
    invitations: [{ id: mockIds.invitations.primary, token: "mock-invitation-token", email: "invitee@notrelix.local", role: "member", expiresAt: "2026-09-01T09:00:00.000Z", isAccepted: false, createdAt: timestamp, workspaceId: mockIds.workspaces.primary, workspaceSlug: "notrelix-product-lab", workspaceName: "Notrelix Product Lab", inviterName: "Morgan Owner" }],
    cardComments: [],
    pageComments: [],
    columns: [],
    labels: [{ id: "mock-label-priority", boardId: mockIds.boards.roadmap, name: "Priority", color: "#ff1e56" }],
  };

  if (scenario === "new-user") {
    database.memberships = database.memberships.filter(
      (membership) => membership.userId !== mockIds.users[persona],
    );
  }
  if (scenario === "large") {
    for (let index = 1; index <= 24; index += 1) {
      const id = `mock-workspace-large-${String(index).padStart(2, "0")}`;
      database.workspaces.push({ id, slug: id, name: `Planning Workspace ${index}`, icon: "Layout", plan: "pro", memberCount: 12, isPersonal: false });
      database.memberships.push({ userId: mockIds.users[persona], workspaceId: id, role: "member" });
    }
  }
  if (scenario === "empty") {
    database.boards = [];
    database.lists = [];
    database.cards = [];
    database.pages = [];
    database.blocks = [];
    database.notifications = [];
    database.cardComments = [];
    database.pageComments = [];
    database.columns = [];
    database.labels = [];
  }
  return database;
}
