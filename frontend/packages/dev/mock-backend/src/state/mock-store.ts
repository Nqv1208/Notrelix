/**
 * MockStore — normalized in-memory database.
 *
 * Uses Maps + secondary indexes for O(1) scoped lookups.
 * Uses the deterministic clock — no uncontrolled new Date().
 * Factories generate records from seed + index — no Math.random().
 * Enforces relational integrity across parent-child resources.
 *
 * Plan: 01-FREEZE-SPEC.md §FZ-S07, §FZ-S08, §FZ-S10, §FZ-S11, §FZ-S13
 *       02-IMPLEMENTATION-PLAN.md §MFB-FZ-03, §MFB-FZ-04, §MFB-FZ-05
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
import type { MockBackendConfig, MockPersona } from "../config/mock-config";
import { defaultConfig } from "../config/mock-config";
import { createMockClock, type MockClock } from "./clock";
import { createFactories, type MockFactories } from "./factories";

export class MockSeedInvariantError extends Error {
  constructor(message: string) {
    super(`[MockSeedInvariantError] ${message}`);
    this.name = "MockSeedInvariantError";
  }
}

export class MockRelationalInvariantError extends Error {
  constructor(message: string) {
    super(`[MockRelationalInvariantError] ${message}`);
    this.name = "MockRelationalInvariantError";
  }
}

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

    // ── All 4 Persona Actors (Plan: 01-FREEZE-SPEC.md §FZ-S07) ───────────────
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
    const member: MockUserRecord = {
      id: mockIds.users.member,
      email: "member@notrelix.local",
      name: "Jordan Lee (Member)",
      avatarUrl: null,
    };
    const viewer: MockUserRecord = {
      id: mockIds.users.viewer,
      email: "viewer@notrelix.local",
      name: "Taylor Morgan (Viewer)",
      avatarUrl: null,
    };

    if (this.config.state === "new-user") {
      // In new-user scenario, only the configured persona actor exists with no workspaces/memberships
      const personaUserMap: Record<MockPersona, MockUserRecord> = {
        owner,
        admin,
        member,
        viewer,
      };
      const user = personaUserMap[this.config.persona] ?? owner;
      this.insertUser(user);
      return;
    }

    // Seed all 4 users for normal / other worlds
    this.insertUser(owner);
    this.insertUser(admin);
    this.insertUser(member);
    this.insertUser(viewer);

    // ── Workspaces ───────────────────────────────────────────────────────────
    const primaryWs: MockWorkspaceRecord = {
      id: mockIds.workspaces.primary,
      name: "Notrelix Product Lab",
      slug: mockIds.workspaces.primary,
      plan: "pro",
      icon: "Layout",
      isPersonal: false,
    };
    const secWs: MockWorkspaceRecord = {
      id: mockIds.workspaces.secondary,
      name: "Secondary Workspace",
      slug: mockIds.workspaces.secondary,
      plan: "free",
      icon: "Layers",
      isPersonal: false,
    };
    this.insertWorkspace(primaryWs);
    this.insertWorkspace(secWs);

    // ── Memberships (All 4 actors in primary workspace) ───────────────────────
    const memOwner: MockMembershipRecord = {
      id: "mem-0001",
      workspaceId: primaryWs.id,
      userId: owner.id,
      role: "owner",
      status: "active",
      workload: 3,
      color: "#1E90FF",
      joinedAt: this.clock.offsetDays(-30),
    };
    const memAdmin: MockMembershipRecord = {
      id: "mem-0002",
      workspaceId: primaryWs.id,
      userId: admin.id,
      role: "admin",
      status: "active",
      workload: 5,
      color: "#22C55E",
      joinedAt: this.clock.offsetDays(-20),
    };
    const memMember: MockMembershipRecord = {
      id: "mem-0003",
      workspaceId: primaryWs.id,
      userId: member.id,
      role: "member",
      status: "active",
      workload: 2,
      color: "#A855F7",
      joinedAt: this.clock.offsetDays(-15),
    };
    const memViewer: MockMembershipRecord = {
      id: "mem-0004",
      workspaceId: primaryWs.id,
      userId: viewer.id,
      role: "guest",
      status: "active",
      workload: 0,
      color: "#F59E0B",
      joinedAt: this.clock.offsetDays(-5),
    };
    this.insertMembership(memOwner);
    this.insertMembership(memAdmin);
    this.insertMembership(memMember);
    this.insertMembership(memViewer);

    // Secondary workspace membership (owner only)
    this.insertMembership({
      id: "mem-0005",
      workspaceId: secWs.id,
      userId: owner.id,
      role: "owner",
      status: "active",
      workload: 0,
      color: "#1E90FF",
      joinedAt: this.clock.offsetDays(-10),
    });

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
      // In empty-workspace, workspaces and memberships exist, but collections are empty
      return;
    }

    // ── Boards + Lists + Cards ────────────────────────────────────────────────
    const isUnicode = this.config.overlays.includes("unicode");
    const isLongTitles = this.config.overlays.includes("long-titles");

    let boardTitle = "Product Roadmap";
    if (isUnicode) boardTitle = "🚀 Product Roadmap (日本語・Üñîçødé)";
    if (isLongTitles)
      boardTitle =
        "Product Roadmap — Comprehensive Multi-Quarter Strategic Deliverables & Features";

    const board: MockBoardRecord = {
      id: mockIds.boards.roadmap,
      workspaceId: primaryWs.id,
      title: boardTitle,
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
      title: isUnicode ? "📋 To Do (準備中)" : "To Do",
      color: "#1E90FF",
      position: 0,
      isCollapsed: false,
    };
    const listInProgress: MockListRecord = {
      id: "list-inprogress",
      boardId: board.id,
      title: isUnicode ? "⚡ In Progress (進行中)" : "In Progress",
      color: "#FC744C",
      position: 1,
      isCollapsed: false,
    };
    const listDone: MockListRecord = {
      id: "list-done",
      boardId: board.id,
      title: isUnicode ? "✅ Done (完了)" : "Done",
      color: "#22C55E",
      position: 2,
      isCollapsed: false,
    };
    this.insertList(listTodo);
    this.insertList(listInProgress);
    this.insertList(listDone);

    if (this.config.overlays.includes("many-columns")) {
      for (let c = 3; c < 8; c++) {
        this.insertList({
          id: `list-col-${c}`,
          boardId: board.id,
          title: `Column ${c}`,
          color: "#A855F7",
          position: c,
          isCollapsed: false,
        });
      }
    }

    const densityCardCount = this.config.overlays.includes("many-cards")
      ? 100
      : this.densityCardCount();

    // Seed cards per list based on density
    for (let i = 0; i < densityCardCount; i++) {
      let cardTitle =
        i === 0 ? "Ship mock runtime" : `Task ${i}: Sample work item`;
      if (isUnicode) cardTitle = `[🚀 ✨] ${cardTitle} — 祝 100%`;
      if (isLongTitles)
        cardTitle = `${cardTitle} — Extended In-Depth Description of Engineering Requirements and Specifications`;

      const card = this.factories.card(i, board.id, listInProgress.id, {
        id: i === 0 ? "card-main-0001" : undefined,
        title: cardTitle,
        description:
          i === 0 ? "Notrelix mock runtime specification." : undefined,
      });
      this.insertCard(card);
    }

    // Seed todo cards
    const todoCount = Math.min(
      3,
      Math.max(1, Math.floor(densityCardCount / 4)),
    );
    for (let i = 0; i < todoCount; i++) {
      this.insertCard(
        this.factories.card(densityCardCount + i, board.id, listTodo.id),
      );
    }

    // Seed notifications for the active user
    const activeUser = this.getCurrentUser();
    this.seedNotifications(activeUser.id, this.densityNotificationCount());

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
    const isUnicode = this.config.overlays.includes("unicode");
    const isLongTitles = this.config.overlays.includes("long-titles");

    for (let i = 0; i < count; i++) {
      let title = i === 0 ? "Product specification" : `Page ${i}`;
      if (isUnicode) title = `📑 ${title} (ドキュメント)`;
      if (isLongTitles)
        title = `${title} — Architectural Specification & Engineering Requirements`;

      this.insertPage(
        this.factories.page(i, workspaceId, {
          id: i === 0 ? "mock-doc-product-spec" : undefined,
          title,
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
      case "tiny":
        return 2;
      case "normal":
        return 8;
      case "large":
        return 50;
      case "stress":
        return 200;
    }
  }

  private densityNotificationCount(): number {
    switch (this.config.density) {
      case "tiny":
        return 1;
      case "normal":
        return 5;
      case "large":
        return 20;
      case "stress":
        return 100;
    }
  }

  private densityPageCount(): number {
    switch (this.config.density) {
      case "tiny":
        return 1;
      case "normal":
        return 4;
      case "large":
        return 15;
      case "stress":
        return 50;
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
    this.sequenceCounters.clear();
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

  private addIndex(
    index: Map<string, Set<string>>,
    key: string,
    id: string,
  ): void {
    let set = index.get(key);
    if (!set) {
      set = new Set();
      index.set(key, set);
    }
    set.add(id);
  }

  private removeIndex(
    index: Map<string, Set<string>>,
    key: string,
    id: string,
  ): void {
    index.get(key)?.delete(id);
  }

  // ─── Repository Accessors ─────────────────────────────────────────────────

  getUser(id: string): MockUserRecord | undefined {
    return this.usersById.get(id);
  }

  getCurrentUser(): MockUserRecord {
    const personaMap: Record<MockPersona, string> = {
      owner: mockIds.users.owner,
      admin: mockIds.users.admin,
      member: mockIds.users.member,
      viewer: mockIds.users.viewer,
    };
    const targetId = personaMap[this.config.persona] ?? mockIds.users.owner;
    const user = this.usersById.get(targetId);
    if (!user) {
      throw new MockSeedInvariantError(
        `Configured persona "${this.config.persona}" (ID: "${targetId}") is not seeded in MockStore.`,
      );
    }
    return user;
  }

  getWorkspaces(): MockWorkspaceRecord[] {
    return Array.from(this.workspacesById.values());
  }

  getWorkspace(id: string): MockWorkspaceRecord | undefined {
    return this.workspacesById.get(id);
  }

  private sequenceCounters = new Map<string, number>();

  nextSequence(entity: string): number {
    const current = this.sequenceCounters.get(entity) ?? 0;
    const next = current + 1;
    this.sequenceCounters.set(entity, next);
    return next;
  }

  nextId(entity: string): string {
    const seq = this.nextSequence(entity);
    return `${entity}-${String(seq).padStart(5, "0")}`;
  }

  createWorkspaceForCurrentUser(input: {
    name?: string;
    slug?: string;
    isPersonal?: boolean;
  }): { workspace: MockWorkspaceRecord; membership: MockMembershipRecord } {
    const currentUser = this.getCurrentUser();
    const newId = this.nextId("ws");
    const workspace: MockWorkspaceRecord = {
      id: newId,
      name: input.name ?? "New Workspace",
      slug: input.slug ?? newId,
      plan: "free",
      icon: "Layout",
      isPersonal: input.isPersonal ?? false,
    };

    this.insertWorkspace(workspace);

    const membership: MockMembershipRecord = {
      id: this.nextId("mem"),
      workspaceId: workspace.id,
      userId: currentUser.id,
      role: "owner",
      status: "active",
      workload: 0,
      color: "#1E90FF",
      joinedAt: this.clock.isoNow(),
    };

    this.insertMembership(membership);
    return { workspace, membership };
  }

  createBoard(
    workspaceId: string,
    input: { title?: string; description?: string },
  ): MockBoardRecord {
    if (!this.workspacesById.has(workspaceId)) {
      throw new MockRelationalInvariantError(
        `Cannot create board: Workspace "${workspaceId}" does not exist.`,
      );
    }
    const factories = this.getFactories();
    const board = factories.board(
      this.getBoards(workspaceId).length,
      workspaceId,
      {
        id: this.nextId("board"),
        title: input.title ?? "New Board",
        description: input.description,
      },
    );
    this.insertBoard(board);
    return board;
  }

  createList(boardId: string, input: { title?: string }): MockListRecord {
    if (!this.boardsById.has(boardId)) {
      throw new MockRelationalInvariantError(
        `Cannot create list: Board "${boardId}" does not exist.`,
      );
    }
    const factories = this.getFactories();
    const list = factories.list(this.getLists(boardId).length, boardId, {
      id: this.nextId("list"),
      title: input.title ?? "New List",
    });
    this.insertList(list);
    return list;
  }

  createCard(
    boardId: string,
    listId: string,
    input: { title?: string; description?: string },
  ): MockCardRecord {
    const list = this.listsById.get(listId);
    if (!list) {
      throw new MockRelationalInvariantError(
        `Cannot create card: List "${listId}" does not exist.`,
      );
    }
    if (list.boardId !== boardId) {
      throw new MockRelationalInvariantError(
        `Cannot create card: List "${listId}" belongs to board "${list.boardId}", not "${boardId}".`,
      );
    }
    const factories = this.getFactories();
    const card = factories.card(this.getCards(listId).length, boardId, listId, {
      id: this.nextId("card"),
      title: input.title ?? "New Card",
      description: input.description,
    });
    this.insertCard(card);
    return card;
  }

  moveCard(cardId: string, targetListId: string, newPosition: number): boolean {
    const card = this.cardsById.get(cardId);
    if (!card) return false;

    const targetList = this.listsById.get(targetListId);
    if (!targetList) return false;

    // Relational check: card and target list must belong to the same board
    if (targetList.boardId !== card.boardId) {
      return false;
    }

    // Remove from old list index
    this.removeIndex(this.cardIdsByListId, card.listId, card.id);

    // Update record
    const updated: MockCardRecord = {
      ...card,
      listId: targetListId,
      position: newPosition,
      updatedAt: this.clock.isoNow(),
    };
    this.cardsById.set(cardId, updated);

    // Add to new list index
    this.addIndex(this.cardIdsByListId, targetListId, card.id);
    return true;
  }

  createPage(
    workspaceId: string,
    input: { title?: string; icon?: string; parentId?: string },
  ): MockPageRecord {
    if (!this.workspacesById.has(workspaceId)) {
      throw new MockRelationalInvariantError(
        `Cannot create page: Workspace "${workspaceId}" does not exist.`,
      );
    }
    const factories = this.getFactories();
    const page = factories.page(
      this.getPages(workspaceId).length,
      workspaceId,
      {
        id: this.nextId("page"),
        title: input.title ?? "New Page",
        icon: input.icon,
        parentId: input.parentId,
      },
    );
    this.insertPage(page);
    return page;
  }

  // ─── Query Accessors ──────────────────────────────────────────────────────

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

  getLists(boardId: string): MockListRecord[] {
    const ids = this.listIdsByBoardId.get(boardId);
    if (!ids) return [];
    return Array.from(ids)
      .map((id) => this.listsById.get(id))
      .filter((l): l is MockListRecord => l !== undefined)
      .sort((a, b) => a.position - b.position);
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

  // ─── Invariant Verification (Plan: 02-IMPLEMENTATION-PLAN.md §MFB-FZ-05) ───

  assertInvariants(): void {
    // 1. Memberships point to existing users and workspaces
    for (const m of this.membershipsById.values()) {
      if (!this.usersById.has(m.userId)) {
        throw new MockRelationalInvariantError(
          `Membership "${m.id}" references non-existent user "${m.userId}".`,
        );
      }
      if (!this.workspacesById.has(m.workspaceId)) {
        throw new MockRelationalInvariantError(
          `Membership "${m.id}" references non-existent workspace "${m.workspaceId}".`,
        );
      }
    }

    // 2. Boards point to existing workspaces
    for (const b of this.boardsById.values()) {
      if (!this.workspacesById.has(b.workspaceId)) {
        throw new MockRelationalInvariantError(
          `Board "${b.id}" references non-existent workspace "${b.workspaceId}".`,
        );
      }
    }

    // 3. Lists point to existing boards
    for (const l of this.listsById.values()) {
      if (!this.boardsById.has(l.boardId)) {
        throw new MockRelationalInvariantError(
          `List "${l.id}" references non-existent board "${l.boardId}".`,
        );
      }
    }

    // 4. Cards point to existing boards and lists, with boardId matching list.boardId
    for (const c of this.cardsById.values()) {
      const list = this.listsById.get(c.listId);
      if (!list) {
        throw new MockRelationalInvariantError(
          `Card "${c.id}" references non-existent list "${c.listId}".`,
        );
      }
      if (list.boardId !== c.boardId) {
        throw new MockRelationalInvariantError(
          `Card "${c.id}" has boardId "${c.boardId}" but its list "${c.listId}" belongs to board "${list.boardId}".`,
        );
      }
    }

    // 5. Pages point to existing workspaces
    for (const p of this.pagesById.values()) {
      if (!this.workspacesById.has(p.workspaceId)) {
        throw new MockRelationalInvariantError(
          `Page "${p.id}" references non-existent workspace "${p.workspaceId}".`,
        );
      }
    }

    // 6. Secondary indexes match primary records exactly
    for (const [wsId, memSet] of this.membershipIdsByWorkspaceId.entries()) {
      for (const memId of memSet) {
        const m = this.membershipsById.get(memId);
        if (!m || m.workspaceId !== wsId) {
          throw new MockRelationalInvariantError(
            `Secondary index membershipIdsByWorkspaceId corrupted for workspace "${wsId}", membership "${memId}".`,
          );
        }
      }
    }

    for (const [boardId, listSet] of this.listIdsByBoardId.entries()) {
      for (const listId of listSet) {
        const l = this.listsById.get(listId);
        if (!l || l.boardId !== boardId) {
          throw new MockRelationalInvariantError(
            `Secondary index listIdsByBoardId corrupted for board "${boardId}", list "${listId}".`,
          );
        }
      }
    }

    for (const [listId, cardSet] of this.cardIdsByListId.entries()) {
      for (const cardId of cardSet) {
        const c = this.cardsById.get(cardId);
        if (!c || c.listId !== listId) {
          throw new MockRelationalInvariantError(
            `Secondary index cardIdsByListId corrupted for list "${listId}", card "${cardId}".`,
          );
        }
      }
    }
  }

  getSnapshot(): Record<string, unknown[]> {
    return {
      users: Array.from(this.usersById.values()),
      workspaces: Array.from(this.workspacesById.values()),
      memberships: Array.from(this.membershipsById.values()),
      views: Array.from(this.viewsById.values()),
      boards: Array.from(this.boardsById.values()),
      lists: Array.from(this.listsById.values()),
      cards: Array.from(this.cardsById.values()),
      notifications: Array.from(this.notificationsById.values()),
      pages: Array.from(this.pagesById.values()),
    };
  }
}
