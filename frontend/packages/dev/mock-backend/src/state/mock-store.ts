/**
 * MockStore — normalized in-memory database.
 *
 * Uses Maps + secondary indexes for O(1) scoped lookups.
 * Uses the deterministic clock — no uncontrolled new Date().
 * Factories generate records from seed + index — no Math.random().
 *
 * Plan: 03-MOCK-DATA-MODEL.md §Store structure, §Clone policy,
 *       §Relational invariants, §Stateful mutation rule
 */

import type {
  MockUserRecord,
  MockWorkspaceRecord,
  MockMembershipRecord,
  MockWorkspaceViewRecord,
  MockBoardRecord,
  MockListRecord,
  MockCardRecord,
  MockNotificationRecord,
  MockPageRecord,
} from "./records";
import { mockIds } from "./mock-ids";
import type { MockBackendConfig } from "../config/mock-config";
import { defaultConfig } from "../config/mock-config";
import { createMockClock, defaultClock, type MockClock } from "./clock";
import { createFactories, defaultFactories, type MockFactories } from "./factories";

// ─── MockStore ────────────────────────────────────────────────────────────────

export class MockStore {
  // Primary Maps
  private usersById = new Map<string, MockUserRecord>();
  private workspacesById = new Map<string, MockWorkspaceRecord>();
  private membershipsById = new Map<string, MockMembershipRecord>();
  private viewsById = new Map<string, MockWorkspaceViewRecord>();
  private boardsById = new Map<string, MockBoardRecord>();
  private listsById = new Map<string, MockListRecord>();
  private cardsById = new Map<string, MockCardRecord>();
  private notificationsById = new Map<string, MockNotificationRecord>();
  private pagesById = new Map<string, MockPageRecord>();

  // Secondary indexes (Plan: 03-MOCK-DATA-MODEL.md §Store structure)
  private membershipIdsByWorkspaceId = new Map<string, Set<string>>();
  private viewIdsByWorkspaceId = new Map<string, Set<string>>();
  private boardIdsByWorkspaceId = new Map<string, Set<string>>();
  private listIdsByBoardId = new Map<string, Set<string>>();
  private cardIdsByListId = new Map<string, Set<string>>();
  private notificationIdsByUserId = new Map<string, Set<string>>();
  private pageIdsByWorkspaceId = new Map<string, Set<string>>();

  private config: MockBackendConfig;
  private clock: MockClock;
  private factories: MockFactories;

  constructor(config: MockBackendConfig = defaultConfig) {
    this.config = { ...config };
    this.clock = createMockClock(config.seed);
    this.factories = createFactories(this.clock);
    this.seedBaseWorld();
  }

  // ─── Config ────────────────────────────────────────────────────────────────

  getConfig(): MockBackendConfig {
    return { ...this.config };
  }

  updateConfig(patch: Partial<MockBackendConfig>): void {
    this.config = { ...this.config, ...patch };
    if (patch.seed !== undefined) {
      this.clock = createMockClock(this.config.seed);
      this.factories = createFactories(this.clock);
    }
    this.seedBaseWorld();
  }

  getClock(): MockClock {
    return this.clock;
  }

  getFactories(): MockFactories {
    return this.factories;
  }

  // ─── Seed / Reset ─────────────────────────────────────────────────────────

  seedBaseWorld(): void {
    this.clearAll();

    if (this.config.state === "new-user") {
      this.insertUser({
        id: mockIds.users.owner,
        email: "ui-dev@notrelix.local",
        name: "UI Developer (Owner)",
        avatarUrl: null,
      });
      return;
    }

    // ── Users ────────────────────────────────────────────────────────────────
    const owner: MockUserRecord = {
      id: mockIds.users.owner,
      email: "ui-dev@notrelix.local",
      name: "UI Developer (Owner)",
      avatarUrl: null,
    };
    const admin: MockUserRecord = {
      id: mockIds.users.admin,
      email: "admin@notrelix.local",
      name: "Alex Rivera (Admin)",
      avatarUrl: null,
    };
    this.insertUser(owner);
    this.insertUser(admin);

    // ── Workspaces ───────────────────────────────────────────────────────────
    const primaryWs: MockWorkspaceRecord = {
      id: mockIds.workspaces.primary,
      name: "Notrelix UI Lab",
      slug: "dev-workspace",
      plan: "pro",
      icon: "Layout",
      isPersonal: false,
    };
    const secWs: MockWorkspaceRecord = {
      id: mockIds.workspaces.secondary,
      name: "Secondary Workspace",
      slug: "dev-workspace-secondary",
      plan: "free",
      icon: "Layers",
      isPersonal: false,
    };
    this.insertWorkspace(primaryWs);
    this.insertWorkspace(secWs);

    // ── Memberships ──────────────────────────────────────────────────────────
    const mem1: MockMembershipRecord = {
      id: "mem-0001",
      workspaceId: primaryWs.id,
      userId: owner.id,
      role: "owner",
      status: "active",
      workload: 3,
      color: "#1E90FF",
      joinedAt: this.clock.offsetDays(-30),
    };
    const mem2: MockMembershipRecord = {
      id: "mem-0002",
      workspaceId: primaryWs.id,
      userId: admin.id,
      role: "admin",
      status: "active",
      workload: 5,
      color: "#22C55E",
      joinedAt: this.clock.offsetDays(-20),
    };
    this.insertMembership(mem1);
    this.insertMembership(mem2);

    // ── Views ─────────────────────────────────────────────────────────────────
    const view1: MockWorkspaceViewRecord = {
      id: mockIds.views.kanban,
      workspaceId: primaryWs.id,
      name: "Product Roadmap",
      type: "kanban",
      icon: "Layout",
      description: "Primary product roadmap board view",
      visibility: "workspace",
      isDefault: true,
      position: 0,
      createdAt: this.clock.offsetDays(-14),
    };
    const view2: MockWorkspaceViewRecord = {
      id: mockIds.views.table,
      workspaceId: primaryWs.id,
      name: "All Tasks",
      type: "table",
      icon: "BarChart",
      description: "Table view of all tasks",
      visibility: "workspace",
      isDefault: false,
      position: 1,
      createdAt: this.clock.offsetDays(-10),
    };
    this.insertView(view1);
    this.insertView(view2);

    if (this.config.state === "empty-workspace") {
      // Seed pages only — no boards/cards
      this.seedPages(primaryWs.id, 2);
      return;
    }

    // ── Boards + Lists + Cards ────────────────────────────────────────────────
    const board: MockBoardRecord = {
      id: mockIds.boards.roadmap,
      workspaceId: primaryWs.id,
      title: "Product Roadmap",
      description: "Main workspace product features and tasks",
      background: { type: "color", value: "#1E90FF" },
      visibility: "workspace",
      isArchived: false,
      createdAt: this.clock.offsetDays(-14),
      updatedAt: this.clock.offsetDays(-1),
    };
    this.insertBoard(board);

    const listTodo: MockListRecord = {
      id: "list-todo",
      boardId: board.id,
      title: "To Do",
      color: "#1E90FF",
      position: 0,
      isCollapsed: false,
    };
    const listInProgress: MockListRecord = {
      id: "list-inprogress",
      boardId: board.id,
      title: "In Progress",
      color: "#FC744C",
      position: 1,
      isCollapsed: false,
    };
    const listDone: MockListRecord = {
      id: "list-done",
      boardId: board.id,
      title: "Done",
      color: "#22C55E",
      position: 2,
      isCollapsed: false,
    };
    this.insertList(listTodo);
    this.insertList(listInProgress);
    this.insertList(listDone);

    const densityCardCount = this.densityCardCount();

    // Seed cards per list based on density
    for (let i = 0; i < densityCardCount; i++) {
      const card = this.factories.card(i, board.id, listInProgress.id, {
        id: i === 0 ? "card-main-0001" : undefined,
        title:
          i === 0
            ? "Redesign Auth UI with Core Brand Tokens"
            : undefined,
        description:
          i === 0
            ? "Migrate auth layout and form controls to Notrelix Core Brand palette."
            : undefined,
      });
      this.insertCard(card);
    }

    // Seed a few todo cards
    const todoCount = Math.min(3, Math.max(1, Math.floor(densityCardCount / 4)));
    for (let i = 0; i < todoCount; i++) {
      this.insertCard(
        this.factories.card(densityCardCount + i, board.id, listTodo.id),
      );
    }

    // Seed notifications
    this.seedNotifications(owner.id, this.densityNotificationCount());

    // Seed pages
    this.seedPages(primaryWs.id, this.densityPageCount());

    // Extra boards for large/stress density
    if (this.config.density === "large" || this.config.density === "stress") {
      this.seedExtraBoards(primaryWs.id);
    }
  }

  // ─── Private seed helpers ─────────────────────────────────────────────────

  private seedNotifications(userId: string, count: number): void {
    for (let i = 0; i < count; i++) {
      this.insertNotification(this.factories.notification(i, userId));
    }
  }

  private seedPages(workspaceId: string, count: number): void {
    for (let i = 0; i < count; i++) {
      this.insertPage(
        this.factories.page(i, workspaceId, {
          id: i === 0 ? "page-product-spec" : undefined,
          title: i === 0 ? "Product Specification" : undefined,
        }),
      );
    }
  }

  private seedExtraBoards(workspaceId: string): void {
    const extraBoardCount = this.config.density === "stress" ? 10 : 4;
    const listsPerBoard = this.config.density === "stress" ? 8 : 4;
    const cardsPerList = this.config.density === "stress" ? 50 : 20;

    for (let b = 1; b <= extraBoardCount; b++) {
      const board = this.factories.board(b, workspaceId);
      this.insertBoard(board);

      for (let l = 0; l < listsPerBoard; l++) {
        const list = this.factories.list(l, board.id);
        this.insertList(list);

        for (let c = 0; c < cardsPerList; c++) {
          this.insertCard(this.factories.card(c, board.id, list.id));
        }
      }
    }
  }

  private densityCardCount(): number {
    switch (this.config.density) {
      case "tiny": return 2;
      case "normal": return 8;
      case "large": return 50;
      case "stress": return 200;
    }
  }

  private densityNotificationCount(): number {
    switch (this.config.density) {
      case "tiny": return 1;
      case "normal": return 5;
      case "large": return 20;
      case "stress": return 100;
    }
  }

  private densityPageCount(): number {
    switch (this.config.density) {
      case "tiny": return 1;
      case "normal": return 4;
      case "large": return 15;
      case "stress": return 50;
    }
  }

  private clearAll(): void {
    this.usersById.clear();
    this.workspacesById.clear();
    this.membershipsById.clear();
    this.viewsById.clear();
    this.boardsById.clear();
    this.listsById.clear();
    this.cardsById.clear();
    this.notificationsById.clear();
    this.pagesById.clear();

    this.membershipIdsByWorkspaceId.clear();
    this.viewIdsByWorkspaceId.clear();
    this.boardIdsByWorkspaceId.clear();
    this.listIdsByBoardId.clear();
    this.cardIdsByListId.clear();
    this.notificationIdsByUserId.clear();
    this.pageIdsByWorkspaceId.clear();
  }

  // ─── Primary insert helpers (maintain indexes) ────────────────────────────

  private insertUser(u: MockUserRecord): void {
    this.usersById.set(u.id, u);
  }

  private insertWorkspace(w: MockWorkspaceRecord): void {
    this.workspacesById.set(w.id, w);
  }

  private insertMembership(m: MockMembershipRecord): void {
    this.membershipsById.set(m.id, m);
    this.addIndex(this.membershipIdsByWorkspaceId, m.workspaceId, m.id);
  }

  private insertView(v: MockWorkspaceViewRecord): void {
    this.viewsById.set(v.id, v);
    this.addIndex(this.viewIdsByWorkspaceId, v.workspaceId, v.id);
  }

  private insertBoard(b: MockBoardRecord): void {
    this.boardsById.set(b.id, b);
    this.addIndex(this.boardIdsByWorkspaceId, b.workspaceId, b.id);
  }

  private insertList(l: MockListRecord): void {
    this.listsById.set(l.id, l);
    this.addIndex(this.listIdsByBoardId, l.boardId, l.id);
  }

  private insertCard(c: MockCardRecord): void {
    this.cardsById.set(c.id, c);
    this.addIndex(this.cardIdsByListId, c.listId, c.id);
  }

  private insertNotification(n: MockNotificationRecord): void {
    this.notificationsById.set(n.id, n);
    this.addIndex(this.notificationIdsByUserId, n.userId, n.id);
  }

  private insertPage(p: MockPageRecord): void {
    this.pagesById.set(p.id, p);
    this.addIndex(this.pageIdsByWorkspaceId, p.workspaceId, p.id);
  }

  private addIndex(index: Map<string, Set<string>>, key: string, id: string): void {
    let set = index.get(key);
    if (!set) {
      set = new Set();
      index.set(key, set);
    }
    set.add(id);
  }

  private removeIndex(index: Map<string, Set<string>>, key: string, id: string): void {
    index.get(key)?.delete(id);
  }

  // ─── Repository Accessors ─────────────────────────────────────────────────

  getCurrentUser(): MockUserRecord {
    const personaMap: Record<string, string> = {
      owner: mockIds.users.owner,
      admin: mockIds.users.admin,
      member: mockIds.users.member,
      viewer: mockIds.users.viewer,
    };
    const targetId = personaMap[this.config.persona] ?? mockIds.users.owner;
    const fallback: MockUserRecord = {
      id: mockIds.users.owner,
      email: "ui-dev@notrelix.local",
      name: "UI Developer (Owner)",
      avatarUrl: null,
    };
    return (
      this.usersById.get(targetId) ??
      Array.from(this.usersById.values())[0] ??
      fallback
    );
  }

  getWorkspaces(): MockWorkspaceRecord[] {
    return Array.from(this.workspacesById.values());
  }

  getWorkspace(id: string): MockWorkspaceRecord | undefined {
    return this.workspacesById.get(id);
  }

  addWorkspace(record: MockWorkspaceRecord): void {
    this.insertWorkspace(record);
  }

  getWorkspaceMembers(workspaceId: string): MockMembershipRecord[] {
    const ids = this.membershipIdsByWorkspaceId.get(workspaceId);
    if (!ids) return [];
    return Array.from(ids)
      .map((id) => this.membershipsById.get(id))
      .filter((m): m is MockMembershipRecord => m !== undefined);
  }

  getWorkspaceViews(workspaceId: string): MockWorkspaceViewRecord[] {
    const ids = this.viewIdsByWorkspaceId.get(workspaceId);
    if (!ids) return [];
    return Array.from(ids)
      .map((id) => this.viewsById.get(id))
      .filter((v): v is MockWorkspaceViewRecord => v !== undefined)
      .sort((a, b) => a.position - b.position);
  }

  getBoards(workspaceId: string): MockBoardRecord[] {
    const ids = this.boardIdsByWorkspaceId.get(workspaceId);
    if (!ids) return [];
    return Array.from(ids)
      .map((id) => this.boardsById.get(id))
      .filter((b): b is MockBoardRecord => b !== undefined);
  }

  getBoard(id: string): MockBoardRecord | undefined {
    return this.boardsById.get(id);
  }

  addBoard(record: MockBoardRecord): void {
    this.insertBoard(record);
  }

  getLists(boardId: string): MockListRecord[] {
    const ids = this.listIdsByBoardId.get(boardId);
    if (!ids) return [];
    return Array.from(ids)
      .map((id) => this.listsById.get(id))
      .filter((l): l is MockListRecord => l !== undefined)
      .sort((a, b) => a.position - b.position);
  }

  addList(record: MockListRecord): void {
    this.insertList(record);
  }

  getCards(listId: string): MockCardRecord[] {
    const ids = this.cardIdsByListId.get(listId);
    if (!ids) return [];
    return Array.from(ids)
      .map((id) => this.cardsById.get(id))
      .filter((c): c is MockCardRecord => c !== undefined)
      .sort((a, b) => a.position - b.position);
  }

  getCard(id: string): MockCardRecord | undefined {
    return this.cardsById.get(id);
  }

  addCard(record: MockCardRecord): void {
    this.insertCard(record);
  }

  moveCard(cardId: string, newListId: string, newPosition: number): boolean {
    const card = this.cardsById.get(cardId);
    if (!card) return false;

    // Remove from old list index
    this.removeIndex(this.cardIdsByListId, card.listId, card.id);

    // Update record
    const updated: MockCardRecord = {
      ...card,
      listId: newListId,
      position: newPosition,
      updatedAt: this.clock.isoNow(),
    };
    this.cardsById.set(cardId, updated);

    // Add to new list index
    this.addIndex(this.cardIdsByListId, newListId, card.id);
    return true;
  }

  getNotifications(userId: string): MockNotificationRecord[] {
    const ids = this.notificationIdsByUserId.get(userId);
    if (!ids) return [];
    return Array.from(ids)
      .map((id) => this.notificationsById.get(id))
      .filter((n): n is MockNotificationRecord => n !== undefined)
      .sort((a, b) => b.createdAt.localeCompare(a.createdAt));
  }

  markNotificationRead(id: string): boolean {
    const n = this.notificationsById.get(id);
    if (!n) return false;
    this.notificationsById.set(id, { ...n, isRead: true });
    return true;
  }

  markAllNotificationsRead(userId: string): void {
    const ids = this.notificationIdsByUserId.get(userId);
    if (!ids) return;
    for (const id of ids) {
      const n = this.notificationsById.get(id);
      if (n && !n.isRead) {
        this.notificationsById.set(id, { ...n, isRead: true });
      }
    }
  }

  getPages(workspaceId: string): MockPageRecord[] {
    const ids = this.pageIdsByWorkspaceId.get(workspaceId);
    if (!ids) return [];
    return Array.from(ids)
      .map((id) => this.pagesById.get(id))
      .filter((p): p is MockPageRecord => p !== undefined)
      .sort((a, b) => a.title.localeCompare(b.title));
  }

  getPage(id: string): MockPageRecord | undefined {
    return this.pagesById.get(id);
  }

  addPage(record: MockPageRecord): void {
    this.insertPage(record);
  }
}

export const globalMockStore = new MockStore();
