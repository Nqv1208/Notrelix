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
 *       04-MOCK-DATASET-SPEC.md §1-12
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
  MockBoardViewRecord,
  MockColumnRecord,
  MockLabelRecord,
  MockChecklistRecord,
  MockChecklistItemRecord,
  MockCommentRecord,
  MockUserPreferencesRecord,
  MockBlockRecord,
  MockPageCommentRecord,
  MockPageHistoryRecord,
} from "./records";
import { mockIds } from "./mock-ids";
import type { MockBackendConfig, MockPersona } from "../config/mock-config";
import { defaultConfig } from "../config/mock-config";
import { createMockClock, type MockClock } from "./clock";
import { createFactories, type MockFactories } from "./factories";
import { MOCK_DATASET_CARDINALITIES } from "./mock-dataset.manifest";

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
  private boardViewsByBoardId = new Map<string, MockBoardViewRecord>();
  private columnsById = new Map<string, MockColumnRecord>();
  private labelsById = new Map<string, MockLabelRecord>();
  private checklistsById = new Map<string, MockChecklistRecord>();
  private checklistItemsById = new Map<string, MockChecklistItemRecord>();
  private commentsById = new Map<string, MockCommentRecord>();
  private fieldValuesByCardId = new Map<string, Map<string, unknown>>();
  private blocksById = new Map<string, MockBlockRecord>();
  private pageCommentsById = new Map<string, MockPageCommentRecord>();
  private pageHistoryById = new Map<string, MockPageHistoryRecord>();
  private userPreferencesByUserId = new Map<
    string,
    MockUserPreferencesRecord
  >();
  private loggedOutUserIds = new Set<string>();

  // Secondary indexes (Plan: 03-MOCK-DATA-MODEL.md §Store structure)
  private membershipIdsByWorkspaceId = new Map<string, Set<string>>();
  private viewIdsByWorkspaceId = new Map<string, Set<string>>();
  private boardIdsByWorkspaceId = new Map<string, Set<string>>();
  private listIdsByBoardId = new Map<string, Set<string>>();
  private cardIdsByListId = new Map<string, Set<string>>();
  private notificationIdsByUserId = new Map<string, Set<string>>();
  private pageIdsByWorkspaceId = new Map<string, Set<string>>();
  private columnIdsByBoardId = new Map<string, Set<string>>();
  private labelIdsByBoardId = new Map<string, Set<string>>();
  private labelIdsByCardId = new Map<string, Set<string>>();
  private checklistIdsByCardId = new Map<string, Set<string>>();
  private checklistItemIdsByChecklistId = new Map<string, Set<string>>();
  private commentIdsByCardId = new Map<string, Set<string>>();

  private config: MockBackendConfig;
  private clock: MockClock;
  private factories: MockFactories;
  private sequenceCounters = new Map<string, number>();

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

  nextSequence(entity: string): number {
    const current = this.sequenceCounters.get(entity) ?? 0;
    const next = current + 1;
    this.sequenceCounters.set(entity, next);
    return next;
  }

  nextId(entity: string): string {
    const seq = this.nextSequence(entity);
    return `${entity}-m-${String(seq).padStart(5, "0")}`;
  }

  // ─── Seed / Reset ─────────────────────────────────────────────────────────

  seedBaseWorld(): void {
    this.clearAll();

    const isMissingAvatars = this.config.overlays.includes("missing-avatars");
    const isUnicode = this.config.overlays.includes("unicode");
    const isLongTitles = this.config.overlays.includes("long-titles");
    const manifest = MOCK_DATASET_CARDINALITIES[this.config.density];

    // ── Users ────────────────────────────────────────────────────────────────
    const owner = this.factories.user(0, {
      id: mockIds.users.owner,
      email: "ui-dev@notrelix.local",
      name: isUnicode ? "UI Developer (Owner) 🚀" : "UI Developer (Owner)",
      avatarUrl: isMissingAvatars ? null : "https://api.dicebear.com/7.x/avataaars/svg?seed=owner",
    });
    const admin = this.factories.user(1, {
      id: mockIds.users.admin,
      email: "admin@notrelix.local",
      name: isUnicode ? "Alex Rivera (Admin) ✨" : "Alex Rivera (Admin)",
      avatarUrl: isMissingAvatars ? null : "https://api.dicebear.com/7.x/avataaars/svg?seed=admin",
    });
    const member = this.factories.user(2, {
      id: mockIds.users.member,
      email: "member@notrelix.local",
      name: isUnicode ? "Jordan Lee (Member) 💼" : "Jordan Lee (Member)",
      avatarUrl: isMissingAvatars ? null : "https://api.dicebear.com/7.x/avataaars/svg?seed=member",
    });
    const viewer = this.factories.user(3, {
      id: mockIds.users.viewer,
      email: "viewer@notrelix.local",
      name: isUnicode ? "Taylor Morgan (Viewer) 👀" : "Taylor Morgan (Viewer)",
      avatarUrl: isMissingAvatars ? null : "https://api.dicebear.com/7.x/avataaars/svg?seed=viewer",
    });

    const actors = [owner, admin, member, viewer];
    
    if (this.config.state === "new-user") {
      const pMap: Record<string, any> = { owner, admin, member, viewer };
      const u = pMap[this.config.persona] || owner;
      this.insertUser(u);
      this.userPreferencesByUserId.set(u.id, { userId: u.id, theme: "system", colorTheme: "zinc", sidebarCollapsed: false, defaultView: "board" });
      return;
    }

    for (const u of actors) {
      this.insertUser(u);
      this.userPreferencesByUserId.set(u.id, { userId: u.id, theme: "system", colorTheme: "zinc", sidebarCollapsed: false, defaultView: "board" });
    }

    // ── Workspaces ──────────────────────────────────────────────────────────
    let primaryWsName = "Notrelix Product Lab";
    if (isUnicode) primaryWsName = "🪐 Notrelix Product Lab (プロダクト)";
    if (isLongTitles) primaryWsName = "Notrelix Product Lab — Enterprise Digital Workspace & Engineering Innovation Hub";

    for (let w = 0; w < manifest.workspacesVisibleToOwner; w++) {
      const isPrimary = w === 0;
      const isSecondary = w === 1;
      let wName = `Workspace ${w}`;
      let id = this.factories.workspace(w).id;
      
      if (isPrimary) {
        wName = primaryWsName;
        id = mockIds.workspaces.primary;
      } else if (isSecondary) {
        wName = "Secondary Workspace";
        id = mockIds.workspaces.secondary;
      }

      const ws = this.factories.workspace(w, { id, name: wName, slug: id, isPersonal: false });
      this.insertWorkspace(ws);

      if (isPrimary) {
        this.insertMembership(this.factories.membership(0, id, owner.id, { role: "owner" }));
        this.insertMembership(this.factories.membership(1, id, admin.id, { role: "admin" }));
        this.insertMembership(this.factories.membership(2, id, member.id, { role: "member" }));
        this.insertMembership(this.factories.membership(3, id, viewer.id, { role: "guest" }));
      } else {
        this.insertMembership(this.factories.membership(w * 10, id, owner.id, { role: "owner" }));
      }
    }

    // ── Views ──────────────────────────────────────────────────────────────
    const primaryWsId = mockIds.workspaces.primary;
    this.insertView({ id: mockIds.views.kanban, workspaceId: primaryWsId, name: "Product Roadmap", type: "kanban", icon: "Layout", description: "", visibility: "workspace", isDefault: true, position: 0, createdAt: this.clock.offsetDays(-14) });
    this.insertView({ id: mockIds.views.table, workspaceId: primaryWsId, name: "All Tasks", type: "table", icon: "BarChart", description: "", visibility: "workspace", isDefault: false, position: 1, createdAt: this.clock.offsetDays(-10) });

    if (this.config.state === "empty-workspace") return;

    // ── Boards ─────────────────────────────────────────────────────────────
    let primaryBoardTitle = "Product Roadmap";
    if (isUnicode) primaryBoardTitle = "🚀 Product Roadmap (日本語・Üñîçødé)";
    if (isLongTitles) primaryBoardTitle = "Product Roadmap — Comprehensive Multi-Quarter Strategic Deliverables & Features";

    for (let b = 0; b < manifest.boardsInPrimaryWorkspace; b++) {
      const isPrimaryBoard = b === 0;
      let bId = this.factories.board(b, primaryWsId).id;
      let bTitle = `Board ${b}`;
      
      if (isPrimaryBoard) {
        bId = "mock-board-roadmap";
        bTitle = primaryBoardTitle;
      }
      
      const board = this.factories.board(b, primaryWsId, { id: bId, title: bTitle });
      this.insertBoard(board);
      this.boardViewsByBoardId.set(bId, { boardId: bId, viewMode: "table", viewConfig: "{}" });

      if (isPrimaryBoard) {
        this.insertColumn({ id: "col-0001", boardId: bId, name: "Status", fieldType: "status", position: 1 });
        this.insertColumn({ id: "col-0002", boardId: bId, name: "Assignee", fieldType: "user", position: 2 });
        this.insertColumn({ id: "col-0003", boardId: bId, name: "Due Date", fieldType: "date", position: 3 });
        this.insertColumn({ id: "col-0004", boardId: bId, name: "Priority", fieldType: "select", position: 4 });
        this.insertLabel({ id: "lbl-0001", boardId: bId, name: "Bug", color: "#EF4444" });
        this.insertLabel({ id: "lbl-0002", boardId: bId, name: "Feature", color: "#1E90FF" });
        this.insertLabel({ id: "lbl-0003", boardId: bId, name: "Design", color: "#A855F7" });
      }

      // ── Lists ────────────────────────────────────────────────────────────
      const listCount = isPrimaryBoard ? manifest.listsPerPrimaryBoard : (this.config.density === "stress" ? 8 : 4);
      const isManyColumns = isPrimaryBoard && this.config.overlays.includes("many-columns");
      const actualListCount = isManyColumns ? Math.max(listCount, 8) : listCount;
      
      for (let l = 0; l < actualListCount; l++) {
        let lId = this.factories.list(l + (b * 100), bId).id; // Use offset so list IDs don't collide if not using board-specific padding
        let lTitle = `List ${l}`;
        
        if (isPrimaryBoard) {
          if (l === 0) { lId = "list-todo"; lTitle = isUnicode ? "📋 To Do (準備中)" : "To Do"; }
          if (l === 1) { lId = "list-inprogress"; lTitle = isUnicode ? "⚡ In Progress (進行中)" : "In Progress"; }
          if (l === 2) { lId = "list-done"; lTitle = isUnicode ? "✅ Done (完了)" : "Done"; }
          if (l >= 3 && isManyColumns && l < 8) { lId = `list-col-${l}`; lTitle = `Column ${l}`; }
        }

        const lst = this.factories.list(l + (b * 100), bId, { id: lId, title: lTitle });
        this.insertList(lst);

        // ── Cards ──────────────────────────────────────────────────────────
        let cardCount = manifest.cardsPerList;
        if (isPrimaryBoard && this.config.overlays.includes("many-cards") && l === 1) {
          cardCount = 100;
        }

        for (let c = 0; c < cardCount; c++) {
          let cTitle = `Task ${c}`;
          let cId = this.factories.card(c + (l * 1000) + (b * 100000), bId, lId).id;
          
          if (isPrimaryBoard && l === 1 && c === 0) {
            cId = "card-main-0001";
            cTitle = "Ship mock runtime";
          }
          if (isUnicode) cTitle = `[🚀] ${cTitle}`;
          if (isLongTitles) cTitle = `${cTitle} — Extended In-Depth Description`;

          const card = this.factories.card(c + (l * 1000) + (b * 100000), bId, lId, { id: cId, title: cTitle });
          this.insertCard(card);
        }
      }
    }

    // Extras for card-main-0001
    if (this.cardsById.has("card-main-0001")) {
      this.addIndex(this.labelIdsByCardId, "card-main-0001", "lbl-0002");
      this.insertChecklist({ id: "chk-0001", cardId: "card-main-0001", title: "Release Checklist", position: 1 });
      this.insertChecklistItem({ id: "chki-0001", checklistId: "chk-0001", title: "Contract tests green", isChecked: true });
      this.insertChecklistItem({ id: "chki-0002", checklistId: "chk-0001", title: "E2E verification complete", isChecked: false });
      this.insertComment({ id: "cmt-0001", cardId: "card-main-0001", userId: owner.id, contentMd: "Initial spec.", createdAt: this.clock.offsetDays(-2) });
    }

    // ── Notifications ──────────────────────────────────────────────────────
    this.seedNotifications(owner.id, manifest.notificationsPerCurrentActor);
    if (this.config.persona !== "owner") {
      const activeUser = this.getCurrentUser();
      this.seedNotifications(activeUser.id, manifest.notificationsPerCurrentActor);
    }

    // ── Pages ──────────────────────────────────────────────────────────────
    for (let p = 0; p < manifest.pagesInPrimaryWorkspace; p++) {
      let pTitle = p === 0 ? "Product specification" : `Page ${p}`;
      if (isUnicode) pTitle = `📑 ${pTitle} (ドキュメント)`;
      if (isLongTitles) pTitle = `${pTitle} — Architectural Specification & Engineering Requirements`;

      let pId = this.factories.page(p, primaryWsId).id;
      if (p === 0) pId = "mock-doc-product-spec";

      const page = this.factories.page(p, primaryWsId, { id: pId, title: pTitle });
      this.insertPage(page);

      // Representative blocks
      const blockCount = p === 0 ? manifest.blocksPerRepresentativePage : 2;
      for (let blk = 0; blk < blockCount; blk++) {
        this.createBlock(pId, {
          type: blk === 0 ? "heading_1" : "paragraph",
          properties: JSON.stringify({ text: blk === 0 ? page.title : `Block ${blk}` }),
          position: blk * 1000,
        });
      }

      this.createPageComment(pId, owner.id, "This is a seeded page comment.");
      this.createPageHistory(pId, owner.id, "created_page", page.title);
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

      const overrides: { id?: string; title: string } = { title };
      if (i === 0) {
        overrides.id = "mock-doc-product-spec";
      }

      const page = this.factories.page(i, workspaceId, overrides);
      this.insertPage(page);

      // Seed blocks
      this.createBlock(page.id, {
        type: "heading_1",
        properties: JSON.stringify({ text: page.title }),
        position: 0,
      });
      this.createBlock(page.id, {
        type: "paragraph",
        properties: JSON.stringify({ text: "This is a seeded mock document block." }),
        position: 65536,
      });

      // Seed comments
      this.createPageComment(page.id, mockIds.users.owner, "This is a seeded page comment.");

      // Seed history
      this.createPageHistory(page.id, mockIds.users.owner, "created_page", page.title);
    }
  }
  private densityCardCount(): number {
    return MOCK_DATASET_CARDINALITIES[this.config.density].cardsPerList;
  }

  private densityNotificationCount(): number {
    return MOCK_DATASET_CARDINALITIES[this.config.density]
      .notificationsPerCurrentActor;
  }

  private densityPageCount(): number {
    return MOCK_DATASET_CARDINALITIES[this.config.density]
      .pagesInPrimaryWorkspace;
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
    this.boardViewsByBoardId.clear();
    this.columnsById.clear();
    this.labelsById.clear();
    this.checklistsById.clear();
    this.checklistItemsById.clear();
    this.commentsById.clear();
    this.fieldValuesByCardId.clear();
    this.userPreferencesByUserId.clear();
    this.loggedOutUserIds.clear();

    this.membershipIdsByWorkspaceId.clear();
    this.viewIdsByWorkspaceId.clear();
    this.boardIdsByWorkspaceId.clear();
    this.listIdsByBoardId.clear();
    this.cardIdsByListId.clear();
    this.notificationIdsByUserId.clear();
    this.pageIdsByWorkspaceId.clear();
    this.columnIdsByBoardId.clear();
    this.labelIdsByBoardId.clear();
    this.labelIdsByCardId.clear();
    this.checklistIdsByCardId.clear();
    this.checklistItemIdsByChecklistId.clear();
    this.commentIdsByCardId.clear();
    this.sequenceCounters.clear();
  }

  // ─── Private Insertion Helpers (maintains primary + secondary indexes) ────

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

  private insertColumn(col: MockColumnRecord): void {
    this.columnsById.set(col.id, col);
    this.addIndex(this.columnIdsByBoardId, col.boardId, col.id);
  }

  private insertLabel(lbl: MockLabelRecord): void {
    this.labelsById.set(lbl.id, lbl);
    this.addIndex(this.labelIdsByBoardId, lbl.boardId, lbl.id);
  }

  private insertChecklist(chk: MockChecklistRecord): void {
    this.checklistsById.set(chk.id, chk);
    this.addIndex(this.checklistIdsByCardId, chk.cardId, chk.id);
  }

  private insertChecklistItem(item: MockChecklistItemRecord): void {
    this.checklistItemsById.set(item.id, item);
    this.addIndex(
      this.checklistItemIdsByChecklistId,
      item.checklistId,
      item.id,
    );
  }

  private insertComment(cmt: MockCommentRecord): void {
    this.commentsById.set(cmt.id, cmt);
    this.addIndex(this.commentIdsByCardId, cmt.cardId, cmt.id);
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

  // ─── Repository Accessors & Mutations ─────────────────────────────────────

  getUser(id: string): MockUserRecord | undefined {
    return this.usersById.get(id);
  }

  updateUserProfile(
    userId: string,
    patch: { name?: string; email?: string; avatarUrl?: string | null },
  ): MockUserRecord | undefined {
    const user = this.usersById.get(userId);
    if (!user) return undefined;
    const updated: MockUserRecord = {
      ...user,
      name: patch.name ?? user.name,
      email: patch.email ?? user.email,
      avatarUrl:
        patch.avatarUrl !== undefined ? patch.avatarUrl : user.avatarUrl,
    };
    this.usersById.set(userId, updated);
    return updated;
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

  logoutCurrentUser(): void {
    const user = this.getCurrentUser();
    this.loggedOutUserIds.add(user.id);
  }

  isCurrentUserLoggedOut(): boolean {
    try {
      const user = this.getCurrentUser();
      return (
        this.loggedOutUserIds.has(user.id) ||
        this.config.state === "expired-session"
      );
    } catch {
      return true;
    }
  }

  getUserPreferences(userId: string): MockUserPreferencesRecord {
    const prefs = this.userPreferencesByUserId.get(userId);
    if (prefs) return prefs;
    const fallback: MockUserPreferencesRecord = {
      userId,
      theme: "system",
      colorTheme: "zinc",
      sidebarCollapsed: false,
      defaultView: "board",
    };
    this.userPreferencesByUserId.set(userId, fallback);
    return fallback;
  }

  updateUserPreferences(
    userId: string,
    patch: Partial<MockUserPreferencesRecord>,
  ): MockUserPreferencesRecord {
    const current = this.getUserPreferences(userId);
    const updated: MockUserPreferencesRecord = {
      ...current,
      ...patch,
      userId,
    };
    this.userPreferencesByUserId.set(userId, updated);
    return updated;
  }

  getWorkspaces(): MockWorkspaceRecord[] {
    return Array.from(this.workspacesById.values());
  }

  getWorkspace(id: string): MockWorkspaceRecord | undefined {
    return this.workspacesById.get(id);
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

  getBoardView(boardId: string): MockBoardViewRecord | undefined {
    return this.boardViewsByBoardId.get(boardId);
  }

  saveBoardView(
    boardId: string,
    viewMode: string,
    viewConfig: string,
    filters?: string,
  ): void {
    if (!this.boardsById.has(boardId)) {
      throw new MockRelationalInvariantError(
        `Cannot save view: Board "${boardId}" does not exist.`,
      );
    }
    this.boardViewsByBoardId.set(boardId, {
      boardId,
      viewMode,
      viewConfig,
      filters,
    });
  }

  // ── Columns ──
  getColumns(boardId: string): MockColumnRecord[] {
    const ids = this.columnIdsByBoardId.get(boardId);
    if (!ids) return [];
    return Array.from(ids)
      .map((id) => this.columnsById.get(id))
      .filter((c): c is MockColumnRecord => c !== undefined)
      .sort((a, b) => a.position - b.position);
  }

  getColumn(id: string): MockColumnRecord | undefined {
    return this.columnsById.get(id);
  }

  createColumn(
    boardId: string,
    input: {
      name: string;
      fieldType: string;
      settings?: string;
      position?: number;
    },
  ): MockColumnRecord {
    if (!this.boardsById.has(boardId)) {
      throw new MockRelationalInvariantError(
        `Cannot create column: Board "${boardId}" does not exist.`,
      );
    }
    const existing = this.getColumns(boardId);
    const col: MockColumnRecord = {
      id: this.nextId("col"),
      boardId,
      name: input.name,
      fieldType: input.fieldType,
      settings: input.settings,
      position: input.position ?? existing.length + 1,
      isHidden: false,
    };
    this.insertColumn(col);
    return col;
  }

  updateColumn(
    columnId: string,
    patch: {
      name?: string;
      fieldType?: string;
      settings?: string;
      isHidden?: boolean;
    },
  ): boolean {
    const col = this.columnsById.get(columnId);
    if (!col) return false;
    const updated: MockColumnRecord = {
      ...col,
      name: patch.name ?? col.name,
      fieldType: patch.fieldType ?? col.fieldType,
      settings: patch.settings !== undefined ? patch.settings : col.settings,
      isHidden: patch.isHidden !== undefined ? patch.isHidden : col.isHidden,
    };
    this.columnsById.set(columnId, updated);
    return true;
  }

  deleteColumn(columnId: string): boolean {
    const col = this.columnsById.get(columnId);
    if (!col) return false;
    this.removeIndex(this.columnIdsByBoardId, col.boardId, col.id);
    this.columnsById.delete(columnId);
    return true;
  }

  reorderColumns(
    boardId: string,
    items: { id: string; newPosition: number }[],
  ): boolean {
    if (!this.boardsById.has(boardId)) return false;
    for (const item of items) {
      const col = this.columnsById.get(item.id);
      if (col && col.boardId === boardId) {
        this.columnsById.set(item.id, { ...col, position: item.newPosition });
      }
    }
    return true;
  }

  // ── Labels ──
  getBoardLabels(boardId: string): MockLabelRecord[] {
    const ids = this.labelIdsByBoardId.get(boardId);
    if (!ids) return [];
    return Array.from(ids)
      .map((id) => this.labelsById.get(id))
      .filter((l): l is MockLabelRecord => l !== undefined);
  }

  getLabel(id: string): MockLabelRecord | undefined {
    return this.labelsById.get(id);
  }

  createLabel(
    boardId: string,
    input: { name?: string; color: string },
  ): MockLabelRecord {
    if (!this.boardsById.has(boardId)) {
      throw new MockRelationalInvariantError(
        `Cannot create label: Board "${boardId}" does not exist.`,
      );
    }
    const lbl: MockLabelRecord = {
      id: this.nextId("lbl"),
      boardId,
      name: input.name ?? "New Label",
      color: input.color,
    };
    this.insertLabel(lbl);
    return lbl;
  }

  updateLabel(
    labelId: string,
    patch: { name?: string; color?: string },
  ): boolean {
    const lbl = this.labelsById.get(labelId);
    if (!lbl) return false;
    const updated: MockLabelRecord = {
      ...lbl,
      name: patch.name ?? lbl.name,
      color: patch.color ?? lbl.color,
    };
    this.labelsById.set(labelId, updated);
    return true;
  }

  deleteLabel(labelId: string): boolean {
    const lbl = this.labelsById.get(labelId);
    if (!lbl) return false;
    this.removeIndex(this.labelIdsByBoardId, lbl.boardId, lbl.id);
    this.labelsById.delete(labelId);
    for (const cardLabels of this.labelIdsByCardId.values()) {
      cardLabels.delete(labelId);
    }
    return true;
  }

  getCardLabels(cardId: string): MockLabelRecord[] {
    const ids = this.labelIdsByCardId.get(cardId);
    if (!ids) return [];
    return Array.from(ids)
      .map((id) => this.labelsById.get(id))
      .filter((l): l is MockLabelRecord => l !== undefined);
  }

  addLabelToCard(cardId: string, labelId: string): boolean {
    const card = this.cardsById.get(cardId);
    const label = this.labelsById.get(labelId);
    if (!card || !label) return false;
    this.addIndex(this.labelIdsByCardId, cardId, labelId);
    return true;
  }

  removeLabelFromCard(cardId: string, labelId: string): boolean {
    const card = this.cardsById.get(cardId);
    if (!card) return false;
    this.removeIndex(this.labelIdsByCardId, cardId, labelId);
    return true;
  }

  // ── Lists ──
  createList(
    boardId: string,
    input: { title?: string; color?: string; position?: number },
  ): MockListRecord {
    if (!this.boardsById.has(boardId)) {
      throw new MockRelationalInvariantError(
        `Cannot create list: Board "${boardId}" does not exist.`,
      );
    }
    const existing = this.getLists(boardId);
    const list: MockListRecord = {
      id: this.nextId("list"),
      boardId,
      title: input.title ?? "New List",
      color: input.color,
      position: input.position ?? existing.length,
      isCollapsed: false,
    };
    this.insertList(list);
    return list;
  }

  updateList(
    listId: string,
    patch: { title?: string; color?: string; isArchived?: boolean },
  ): boolean {
    const list = this.listsById.get(listId);
    if (!list) return false;
    const updated: MockListRecord = {
      ...list,
      title: patch.title ?? list.title,
      color: patch.color !== undefined ? patch.color : list.color,
    };
    this.listsById.set(listId, updated);
    return true;
  }

  deleteList(listId: string): boolean {
    const list = this.listsById.get(listId);
    if (!list) return false;

    // Delete all cards in this list
    const cardIds = Array.from(this.cardIdsByListId.get(listId) ?? []);
    for (const cardId of cardIds) {
      this.deleteCard(cardId);
    }

    this.removeIndex(this.listIdsByBoardId, list.boardId, list.id);
    this.cardIdsByListId.delete(listId);
    this.listsById.delete(listId);
    return true;
  }

  duplicateList(listId: string): MockListRecord | undefined {
    const list = this.listsById.get(listId);
    if (!list) return undefined;

    const newList = this.createList(list.boardId, {
      title: `${list.title} (Copy)`,
      color: list.color,
      position: list.position + 1,
    });

    const cards = this.getCards(listId);
    for (const card of cards) {
      this.createCard(list.boardId, newList.id, {
        title: card.title,
        description: card.description,
      });
    }

    return newList;
  }

  reorderLists(
    boardId: string,
    items: { id: string; newPosition: number }[],
  ): boolean {
    if (!this.boardsById.has(boardId)) return false;
    for (const item of items) {
      const list = this.listsById.get(item.id);
      if (list && list.boardId === boardId) {
        this.listsById.set(item.id, { ...list, position: item.newPosition });
      }
    }
    return true;
  }

  // ── Cards ──
  createCard(
    boardId: string,
    listId: string,
    input: { title?: string; description?: string; position?: number },
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
    const existing = this.getCards(listId);
    const card: MockCardRecord = {
      id: this.nextId("card"),
      boardId,
      listId,
      title: input.title ?? "New Card",
      description: input.description,
      position: input.position ?? existing.length,
      createdAt: this.clock.isoNow(),
      updatedAt: this.clock.isoNow(),
    };
    this.insertCard(card);
    return card;
  }

  createCardByListId(
    listId: string,
    input: { title?: string; description?: string; position?: number },
  ): MockCardRecord {
    const list = this.listsById.get(listId);
    if (!list) {
      throw new MockRelationalInvariantError(
        `Cannot create card: List "${listId}" does not exist.`,
      );
    }
    return this.createCard(list.boardId, listId, input);
  }

  updateCard(cardId: string, patch: Partial<MockCardRecord>): boolean {
    const card = this.cardsById.get(cardId);
    if (!card) return false;
    const updated: MockCardRecord = {
      ...card,
      ...patch,
      id: card.id,
      boardId: card.boardId,
      listId: patch.listId ?? card.listId,
      updatedAt: this.clock.isoNow(),
    };
    this.cardsById.set(cardId, updated);
    return true;
  }

  deleteCard(cardId: string): boolean {
    const card = this.cardsById.get(cardId);
    if (!card) return false;

    this.removeIndex(this.cardIdsByListId, card.listId, card.id);
    this.cardsById.delete(cardId);
    this.labelIdsByCardId.delete(cardId);
    this.fieldValuesByCardId.delete(cardId);

    // Delete checklists and their items
    const chkIds = Array.from(this.checklistIdsByCardId.get(cardId) ?? []);
    for (const chkId of chkIds) {
      this.deleteChecklist(chkId);
    }
    this.checklistIdsByCardId.delete(cardId);

    // Delete comments
    const cmtIds = Array.from(this.commentIdsByCardId.get(cardId) ?? []);
    for (const cmtId of cmtIds) {
      this.commentsById.delete(cmtId);
    }
    this.commentIdsByCardId.delete(cardId);

    return true;
  }

  duplicateCard(cardId: string): MockCardRecord | undefined {
    const card = this.cardsById.get(cardId);
    if (!card) return undefined;

    const newCard = this.createCard(card.boardId, card.listId, {
      title: `${card.title} (Copy)`,
      description: card.description,
      position: card.position + 1,
    });

    // Duplicate labels
    const labels = this.labelIdsByCardId.get(cardId);
    if (labels) {
      for (const lblId of labels) {
        this.addIndex(this.labelIdsByCardId, newCard.id, lblId);
      }
    }

    return newCard;
  }

  archiveCard(cardId: string): boolean {
    return this.cardsById.has(cardId);
  }

  updateFieldValue(
    cardId: string,
    fieldDefinitionId: string,
    value: unknown,
  ): boolean {
    const card = this.cardsById.get(cardId);
    if (!card) return false;
    let fieldMap = this.fieldValuesByCardId.get(cardId);
    if (!fieldMap) {
      fieldMap = new Map<string, unknown>();
      this.fieldValuesByCardId.set(cardId, fieldMap);
    }
    fieldMap.set(fieldDefinitionId, value);
    return true;
  }

  getFieldValues(cardId: string): Record<string, unknown> {
    const fieldMap = this.fieldValuesByCardId.get(cardId);
    if (!fieldMap) return {};
    const result: Record<string, unknown> = {};
    for (const [k, v] of fieldMap.entries()) {
      result[k] = v;
    }
    return result;
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

  // ── Checklists ──
  getCardChecklists(
    cardId: string,
  ): (MockChecklistRecord & { items: MockChecklistItemRecord[] })[] {
    const ids = this.checklistIdsByCardId.get(cardId);
    if (!ids) return [];
    return Array.from(ids)
      .map((id) => this.checklistsById.get(id))
      .filter((c): c is MockChecklistRecord => c !== undefined)
      .sort((a, b) => a.position - b.position)
      .map((chk) => {
        const itemIds = this.checklistItemIdsByChecklistId.get(chk.id);
        const items = itemIds
          ? Array.from(itemIds)
              .map((itemId) => this.checklistItemsById.get(itemId))
              .filter((i): i is MockChecklistItemRecord => i !== undefined)
          : [];
        return { ...chk, items };
      });
  }

  getChecklist(id: string): MockChecklistRecord | undefined {
    return this.checklistsById.get(id);
  }

  createChecklist(cardId: string, title: string): MockChecklistRecord {
    const card = this.cardsById.get(cardId);
    if (!card) {
      throw new MockRelationalInvariantError(
        `Cannot create checklist: Card "${cardId}" does not exist.`,
      );
    }
    const existing = this.getCardChecklists(cardId);
    const chk: MockChecklistRecord = {
      id: this.nextId("chk"),
      cardId,
      title: title || "Checklist",
      position: existing.length + 1,
    };
    this.insertChecklist(chk);
    return chk;
  }

  updateChecklist(
    checklistId: string,
    patch: { title?: string; position?: number },
  ): boolean {
    const chk = this.checklistsById.get(checklistId);
    if (!chk) return false;
    const updated: MockChecklistRecord = {
      ...chk,
      title: patch.title ?? chk.title,
      position: patch.position !== undefined ? patch.position : chk.position,
    };
    this.checklistsById.set(checklistId, updated);
    return true;
  }

  deleteChecklist(checklistId: string): boolean {
    const chk = this.checklistsById.get(checklistId);
    if (!chk) return false;

    const itemIds = Array.from(
      this.checklistItemIdsByChecklistId.get(checklistId) ?? [],
    );
    for (const itemId of itemIds) {
      this.checklistItemsById.delete(itemId);
    }
    this.checklistItemIdsByChecklistId.delete(checklistId);

    this.removeIndex(this.checklistIdsByCardId, chk.cardId, chk.id);
    this.checklistsById.delete(checklistId);
    return true;
  }

  createChecklistItem(
    checklistId: string,
    title: string,
  ): MockChecklistItemRecord {
    const chk = this.checklistsById.get(checklistId);
    if (!chk) {
      throw new MockRelationalInvariantError(
        `Cannot create checklist item: Checklist "${checklistId}" does not exist.`,
      );
    }
    const item: MockChecklistItemRecord = {
      id: this.nextId("chki"),
      checklistId,
      title: title || "New Item",
      isChecked: false,
      dueDate: null,
      assigneeId: null,
    };
    this.insertChecklistItem(item);
    return item;
  }

  updateChecklistItem(
    itemId: string,
    patch: {
      title?: string;
      isChecked?: boolean;
      dueDate?: string | null;
      assigneeId?: string | null;
    },
  ): boolean {
    const item = this.checklistItemsById.get(itemId);
    if (!item) return false;
    const updated: MockChecklistItemRecord = {
      ...item,
      title: patch.title ?? item.title,
      isChecked:
        patch.isChecked !== undefined ? patch.isChecked : item.isChecked,
      dueDate: patch.dueDate !== undefined ? patch.dueDate : item.dueDate,
      assigneeId:
        patch.assigneeId !== undefined ? patch.assigneeId : item.assigneeId,
    };
    this.checklistItemsById.set(itemId, updated);
    return true;
  }

  deleteChecklistItem(itemId: string): boolean {
    const item = this.checklistItemsById.get(itemId);
    if (!item) return false;
    this.removeIndex(
      this.checklistItemIdsByChecklistId,
      item.checklistId,
      item.id,
    );
    this.checklistItemsById.delete(itemId);
    return true;
  }

  // ── Comments ──
  getCardComments(cardId: string): MockCommentRecord[] {
    const ids = this.commentIdsByCardId.get(cardId);
    if (!ids) return [];
    return Array.from(ids)
      .map((id) => this.commentsById.get(id))
      .filter((c): c is MockCommentRecord => c !== undefined)
      .sort((a, b) => a.createdAt.localeCompare(b.createdAt));
  }

  createCardComment(
    cardId: string,
    userId: string,
    contentMd: string,
  ): MockCommentRecord {
    const card = this.cardsById.get(cardId);
    if (!card) {
      throw new MockRelationalInvariantError(
        `Cannot create comment: Card "${cardId}" does not exist.`,
      );
    }
    const cmt: MockCommentRecord = {
      id: this.nextId("cmt"),
      cardId,
      userId,
      contentMd,
      createdAt: this.clock.isoNow(),
    };
    this.insertComment(cmt);
    return cmt;
  }

  updateCardComment(commentId: string, contentMd: string): boolean {
    const cmt = this.commentsById.get(commentId);
    if (!cmt) return false;
    const updated: MockCommentRecord = {
      ...cmt,
      contentMd,
      updatedAt: this.clock.isoNow(),
    };
    this.commentsById.set(commentId, updated);
    return true;
  }

  deleteCardComment(commentId: string): boolean {
    const cmt = this.commentsById.get(commentId);
    if (!cmt) return false;
    this.removeIndex(this.commentIdsByCardId, cmt.cardId, cmt.id);
    this.commentsById.delete(commentId);
    return true;
  }

  // ── Pages ──
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

  updatePage(
    pageId: string,
    patch: { title?: string; icon?: string; parentId?: string },
  ): boolean {
    const page = this.pagesById.get(pageId);
    if (!page) return false;
    const updated: MockPageRecord = {
      ...page,
      title: patch.title ?? page.title,
      icon: patch.icon !== undefined ? patch.icon : page.icon,
      parentId: patch.parentId !== undefined ? patch.parentId : page.parentId,
      updatedAt: this.clock.isoNow(),
    };
    this.pagesById.set(pageId, updated);
    return true;
  }

  deletePage(pageId: string): boolean {
    const page = this.pagesById.get(pageId);
    if (!page) return false;
    this.removeIndex(this.pageIdsByWorkspaceId, page.workspaceId, page.id);
    this.pagesById.delete(pageId);
    return true;
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

  getList(id: string): MockListRecord | undefined {
    return this.listsById.get(id);
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
      .sort((a, b) => b.updatedAt.localeCompare(a.updatedAt));
  }

  getPage(id: string): MockPageRecord | undefined {
    return this.pagesById.get(id);
  }

  // ─── Invariant Verification (Plan: 02-IMPLEMENTATION-PLAN.md §MFB-FZ-05) ───


  // ─── Documents Methods ──────────────────────────────────────────────────────

  getPageBlocks(pageId: string): MockBlockRecord[] {
    return Array.from(this.blocksById.values())
      .filter((b) => b.pageId === pageId)
      .sort((a, b) => a.position - b.position);
  }

  createBlock(pageId: string, data: Partial<MockBlockRecord>): MockBlockRecord {
    const id = this.nextId("block");
    const block: MockBlockRecord = {
      id,
      pageId,
      type: data.type || "paragraph",
      properties: data.properties || "{}",
      position: data.position || 0,
      version: 1,
      createdByUserId: mockIds.users.owner,
      createdAt: this.clock.isoNow(),
    };
    this.blocksById.set(id, block);
    return block;
  }

  updateBlock(id: string, data: Partial<MockBlockRecord>): MockBlockRecord | null {
    const block = this.blocksById.get(id);
    if (!block) return null;
    if (data.type !== undefined) block.type = data.type;
    if (data.properties !== undefined) block.properties = data.properties;
    block.updatedAt = this.clock.isoNow();
    return block;
  }

  deleteBlock(id: string): boolean {
    return this.blocksById.delete(id);
  }

  getPageComments(pageId: string): MockPageCommentRecord[] {
    return Array.from(this.pageCommentsById.values())
      .filter((c) => c.pageId === pageId)
      .sort((a, b) => a.createdAt.localeCompare(b.createdAt));
  }

  createPageComment(pageId: string, userId: string, contentMd: string): MockPageCommentRecord {
    const id = this.nextId("pcomment");
    const comment: MockPageCommentRecord = {
      id,
      pageId,
      userId,
      contentMd,
      createdAt: this.clock.isoNow(),
    };
    this.pageCommentsById.set(id, comment);
    return comment;
  }

  updatePageComment(id: string, contentMd: string): MockPageCommentRecord | null {
    const comment = this.pageCommentsById.get(id);
    if (!comment) return null;
    comment.contentMd = contentMd;
    comment.updatedAt = this.clock.isoNow();
    return comment;
  }

  deletePageComment(id: string): boolean {
    return this.pageCommentsById.delete(id);
  }

  getPageHistory(pageId: string): MockPageHistoryRecord[] {
    return Array.from(this.pageHistoryById.values())
      .filter((h) => h.pageId === pageId)
      .sort((a, b) => b.createdAt.localeCompare(a.createdAt));
  }

  createPageHistory(pageId: string, userId: string, action: string, details?: string): MockPageHistoryRecord {
    const id = this.nextId("phist");
    const history: MockPageHistoryRecord = {
      id,
      pageId,
      actorId: userId,
      action,
      resourceTitle: details,
      createdAt: this.clock.isoNow(),
    };
    this.pageHistoryById.set(id, history);
    return history;
  }

  // ─── END Documents Methods ──────────────────────────────────────────────────


  getDatasetMetrics() {
    return {
      users: this.usersById.size,
      workspaces: this.workspacesById.size,
      memberships: this.membershipsById.size,
      boards: this.boardsById.size,
      lists: this.listsById.size,
      cards: this.cardsById.size,
      pages: this.pagesById.size,
      blocks: this.blocksById.size,
      notifications: this.notificationsById.size,
    };
  }

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

    // 5. Columns point to existing boards
    for (const col of this.columnsById.values()) {
      if (!this.boardsById.has(col.boardId)) {
        throw new MockRelationalInvariantError(
          `Column "${col.id}" references non-existent board "${col.boardId}".`,
        );
      }
    }

    // 6. Labels point to existing boards
    for (const lbl of this.labelsById.values()) {
      if (!this.boardsById.has(lbl.boardId)) {
        throw new MockRelationalInvariantError(
          `Label "${lbl.id}" references non-existent board "${lbl.boardId}".`,
        );
      }
    }

    // 7. Checklists point to existing cards
    for (const chk of this.checklistsById.values()) {
      if (!this.cardsById.has(chk.cardId)) {
        throw new MockRelationalInvariantError(
          `Checklist "${chk.id}" references non-existent card "${chk.cardId}".`,
        );
      }
    }

    // 8. Checklist items point to existing checklists
    for (const item of this.checklistItemsById.values()) {
      if (!this.checklistsById.has(item.checklistId)) {
        throw new MockRelationalInvariantError(
          `ChecklistItem "${item.id}" references non-existent checklist "${item.checklistId}".`,
        );
      }
    }

    // 9. Comments point to existing cards and existing users
    for (const cmt of this.commentsById.values()) {
      if (!this.cardsById.has(cmt.cardId)) {
        throw new MockRelationalInvariantError(
          `Comment "${cmt.id}" references non-existent card "${cmt.cardId}".`,
        );
      }
      if (!this.usersById.has(cmt.userId)) {
        throw new MockRelationalInvariantError(
          `Comment "${cmt.id}" references non-existent user "${cmt.userId}".`,
        );
      }
    }

    // 10. Pages point to existing workspaces
    for (const p of this.pagesById.values()) {
      if (!this.workspacesById.has(p.workspaceId)) {
        throw new MockRelationalInvariantError(
          `Page "${p.id}" references non-existent workspace "${p.workspaceId}".`,
        );
      }
    }

    // 11. Secondary indexes match primary records exactly (bidirectional)
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

    for (const [boardId, colSet] of this.columnIdsByBoardId.entries()) {
      for (const colId of colSet) {
        const col = this.columnsById.get(colId);
        if (!col || col.boardId !== boardId) {
          throw new MockRelationalInvariantError(
            `Secondary index columnIdsByBoardId corrupted for board "${boardId}", column "${colId}".`,
          );
        }
      }
    }

    for (const [boardId, lblSet] of this.labelIdsByBoardId.entries()) {
      for (const lblId of lblSet) {
        const lbl = this.labelsById.get(lblId);
        if (!lbl || lbl.boardId !== boardId) {
          throw new MockRelationalInvariantError(
            `Secondary index labelIdsByBoardId corrupted for board "${boardId}", label "${lblId}".`,
          );
        }
      }
    }

    for (const [cardId, chkSet] of this.checklistIdsByCardId.entries()) {
      for (const chkId of chkSet) {
        const chk = this.checklistsById.get(chkId);
        if (!chk || chk.cardId !== cardId) {
          throw new MockRelationalInvariantError(
            `Secondary index checklistIdsByCardId corrupted for card "${cardId}", checklist "${chkId}".`,
          );
        }
      }
    }

    for (const [
      chkId,
      itemSet,
    ] of this.checklistItemIdsByChecklistId.entries()) {
      for (const itemId of itemSet) {
        const item = this.checklistItemsById.get(itemId);
        if (!item || item.checklistId !== chkId) {
          throw new MockRelationalInvariantError(
            `Secondary index checklistItemIdsByChecklistId corrupted for checklist "${chkId}", item "${itemId}".`,
          );
        }
      }
    }

    for (const [cardId, cmtSet] of this.commentIdsByCardId.entries()) {
      for (const cmtId of cmtSet) {
        const cmt = this.commentsById.get(cmtId);
        if (!cmt || cmt.cardId !== cardId) {
          throw new MockRelationalInvariantError(
            `Secondary index commentIdsByCardId corrupted for card "${cardId}", comment "${cmtId}".`,
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
      boardViews: Array.from(this.boardViewsByBoardId.values()),
      columns: Array.from(this.columnsById.values()),
      labels: Array.from(this.labelsById.values()),
      lists: Array.from(this.listsById.values()),
      cards: Array.from(this.cardsById.values()),
      checklists: Array.from(this.checklistsById.values()),
      checklistItems: Array.from(this.checklistItemsById.values()),
      comments: Array.from(this.commentsById.values()),
      notifications: Array.from(this.notificationsById.values()),
      pages: Array.from(this.pagesById.values()),
      userPreferences: Array.from(this.userPreferencesByUserId.values()),
      blocks: Array.from(this.blocksById.values()),
      pageComments: Array.from(this.pageCommentsById.values()),
      pageHistory: Array.from(this.pageHistoryById.values()),
    };
  }
}
