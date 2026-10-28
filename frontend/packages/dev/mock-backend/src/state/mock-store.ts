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
  MockFieldOptionRecord,
  MockLabelRecord,
  MockChecklistRecord,
  MockChecklistItemRecord,
  MockAttachmentRecord,
  MockPageAttachmentRecord,
  MockCommentRecord,
  MockItemLinkRecord,
  MockUserPreferencesRecord,
  MockBlockRecord,
  MockPageCommentRecord,
  MockPageHistoryRecord,
  MockBoardMemberRecord,
  MockBoardGrantRecord,
  MockBoardAudienceRecord,
} from "./records";
import { mockIds } from "./mock-ids";
import type { MockBackendConfig, MockPersona } from "../config/mock-config";
import { defaultConfig } from "../config/mock-config";
import { createMockClock, hanoiShortDay, type MockClock } from "./clock";
import { createFactories, type MockFactories } from "./factories";
import { MOCK_DATASET_CARDINALITIES } from "./mock-dataset.manifest";
import {
  MockBoardSettingsState,
  erpSettingsRegistry,
} from "./mock-board-settings";
import { MockSpaceState, type MockSpaceViewer } from "./mock-spaces";
import { MockDocsHistoryState } from "./mock-docs-history";
import { MockWorkFeedState } from "./mock-work-feed";
import {
  defaultStatusOptions,
  ERP_AUDIENCE,
  ERP_BOARD_MEMBERS,
  ERP_COMMENTER,
  ERP_ITEMS,
  ERP_LABELS,
  ERP_GROUPS,
  erpGroupId,
  ERP_OWNER_COLUMN_ID,
  ERP_PEOPLE,
  ERP_PRIORITY_COLUMN_ID,
  ERP_PRIORITY_OPTIONS,
  ERP_STATUS_COLUMN_ID,
  ERP_STATUS_OPTIONS,
  erpAssigneeId,
  erpItemId,
  erpLabelId,
  erpPriorityOptionId,
  erpStatusOptionId,
  hanoiInstant,
} from "./mock-erp-board";
import {
  ENGINEERING_MEMBERS,
  ERP_ENGINEERS_TEAM,
  ACCOUNTING_PEOPLE,
  ACCOUNTING_TEAM,
  ONCALL_TEAM,
  ERP_TEAM_ONLY,
  EXECUTIVES,
  EXECUTIVE_TEAM,
  QA_PEOPLE,
  QA_TEAM,
} from "./mock-space-people";
import {
  ERP_ITEM_PERCENT,
  ERP_PLAN_GROUPS,
  ERP_PLAN_ITEMS,
  ERP_PLAN_LINKS,
  ERP_CHILD_ITEMS,
  ERP_COMMENTS,
  ERP_DESCRIPTIONS,
} from "./mock-erp-detail";
import {
  ERP_CUSTOM_VALUES,
  ERP_ESTIMATE_COLUMN_ID,
  ERP_FILES,
  ERP_FOLLOWERS,
  ERP_EXTRA_COMMENTS,
  ERP_HISTORY,
  ERP_ITEM_CREATED,
  ERP_MODULE_COLUMN_ID,
  ERP_MODULE_OPTIONS,
  ERP_RELATED_LINKS,
  ERP_REQUESTER_COLUMN_ID,
  ERP_REQUESTER_OPTIONS,
  type ErpHistoryCompact,
  type ErpHistoryDetail,
} from "./mock-item-detail";
import { MockAuthDirectory } from "./mock-auth-directory";
import { MockAccountPages } from "./mock-account-pages";
import { MockReports } from "./mock-reports";
import { MockBilling } from "./mock-billing";
import { MockIntegrations } from "./mock-integrations";
import { MockWorkspaceAutomations } from "./mock-workspace-automations";
import {
  ERP_AUTOMATION_EXECUTIONS,
  ERP_AUTOMATION_RULES,
} from "./mock-erp-automations";
import { MockOnboarding } from "./mock-onboarding";
import { ERP_STATE_ITEMS, erpLongChildTitles } from "./mock-item-states";
import { MockBoardSharing, seedErpSharing } from "./mock-board-sharing";
import { MockBoardSavedViews } from "./mock-board-saved-views";
import { LIBRARY_DOCS, SECONDARY_BOARD_TITLES } from "./mock-docs-library";
import {
  INVOICE_BLOCKS,
  INVOICE_COMMENTS,
  INVOICE_DOC_ID,
  INVOICE_FOLDER_ID,
  INVOICE_HISTORY,
} from "./mock-docs-invoice";
import {
  DEPARTMENT_BOARDS,
  TECH_SIDE_BOARDS,
  departmentItemId,
} from "./mock-department-boards";

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

// ─── Workspace experience records (WX-MEM-001, WX-SET-001) ──────────────────

export interface MockInvitationRecord {
  id: string;
  workspaceId: string;
  email: string;
  role: "admin" | "member" | "guest";
  createdAt: string;
  expiresAt: string;
  status: "pending" | "accepted" | "revoked";
}

export interface MockWorkspaceSettingsRecord {
  allowPublicSharing: boolean;
  enforceMfa: boolean;
  allowGuestInvites: boolean;
  defaultMemberRole: "Admin" | "Member" | "Guest";
  invitationExpiryDays: number;
  timeZone: string;
  weekStart: "monday" | "sunday";
}

/** Mirrors the producer's member-rule outcomes (WSP-008). */
export type MockMemberChange =
  "ok" | "not-found" | "last-owner" | "owner-direct" | "inactive";

/**
 * Outcome of a versioned workspace write (`expectedVersion`, WX-SET-001):
 * `stale` mirrors the producer's 412 when the version the member last read is
 * not the current one; nothing changes in that case.
 */
export type MockVersionedWrite = "ok" | "not-found" | "stale";

// ─── MockStore ────────────────────────────────────────────────────────────────

/** One line of an item's history (gap `CTR-GAP-TODO`, with structured fields). */
export interface MockActivityRecord {
  id: string;
  /** `null` = an automation. */
  actorId: string | null;
  action: string;
  createdAt: string;
  kind?: "created";
  automationName?: string;
  verb?: string;
  target?: string;
  notified?: string;
  detail?: ErpHistoryDetail;
  compact?: ErpHistoryCompact;
  /** The stable type and language-neutral change the real history read carries. */
  type?: string;
  change?: {
    fieldName?: string;
    from?: string;
    to?: string;
    personId?: string;
    personName?: string;
    itemKey?: string;
  };
}

/** A board's plan read (gap `CTR-GAP-WM-PLAN-ITEMS`). */
export interface MockBoardPlan {
  readonly items: readonly {
    readonly id: string;
    readonly kind: "phase" | "milestone";
    readonly title: string;
    readonly startDay: string;
    readonly endDay: string;
    readonly category: "todo" | "doing" | "review" | "done";
    readonly groupId: string;
    readonly percent: number;
    readonly index: number;
  }[];
  readonly links: readonly { readonly from: string; readonly to: string }[];
  readonly percents: Readonly<Record<string, number>>;
  /** Group id by item id, where the plan lists an item elsewhere. */
  readonly groups: Readonly<Record<string, string>>;
}

export class MockStore {
  /** Sign-in attempts, second steps and guest invitations (gap `CTR-GAP-AUTH-FLOW`). */
  readonly auth = new MockAuthDirectory();
  /** Setup drafts, getting-started lists and welcome profiles (gap `CTR-GAP-ONBOARD-FLOW`). */
  readonly onboarding = new MockOnboarding();
  readonly accountPages = new MockAccountPages();
  readonly reports = new MockReports();
  readonly billing = new MockBilling();
  readonly integrations = new MockIntegrations();
  /** The automation rules of each workspace. */
  readonly workspaceAutomations = new MockWorkspaceAutomations();
  // Primary Maps
  private usersById = new Map<string, MockUserRecord>();
  private workspacesById = new Map<string, MockWorkspaceRecord>();
  private membershipsById = new Map<string, MockMembershipRecord>();
  private viewsById = new Map<string, MockWorkspaceViewRecord>();
  private boardsById = new Map<string, MockBoardRecord>();
  /** Settings registry of the boards (the gap `CTR-GAP-WM-BOARD-SETTINGS`). */
  readonly settings = new MockBoardSettingsState(this);
  private listsById = new Map<string, MockListRecord>();
  private cardsById = new Map<string, MockCardRecord>();
  /** Soft-deleted cards (the 30 day trash); the mock keeps the card row only. */
  private trashedCardsById = new Map<
    string,
    { card: MockCardRecord; deletedAt: string; deletedBy: string }
  >();
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
  /** Per-member navigation state (WX-BE-001/002), keyed by workspace:user. */
  private navigationByMember = new Map<
    string,
    {
      recents: { kind: string; resourceId: string; at: string }[];
      pins: { kind: string; resourceId: string; at: string }[];
    }
  >();
  private navigationTick = 0;
  /** Membership access status (WX-MEM-001); absent = active. */
  private suspendedMembershipIds = new Set<string>();
  /** Workspace invitations (WX-MEM-001). */
  private invitationsById = new Map<string, MockInvitationRecord>();
  /** Workspace defaults (`Workspaces.Settings.*`). */
  private settingsByWorkspaceId = new Map<
    string,
    MockWorkspaceSettingsRecord
  >();
  /** Workspace aggregate version (`WorkspaceDto.version`); absent = 1. */
  private workspaceVersionById = new Map<string, number>();
  /** Workspace description (`WorkspaceDto.description`). */
  private workspaceDescriptionById = new Map<string, string>();
  /** Archived workspaces drop out of the list and detail reads. */
  private archivedWorkspaceIds = new Set<string>();
  /** Items completed through `WorkManagement.BoardItems.Complete`. */
  private completedCardIds = new Set<string>();
  /** The item history ("Lịch sử"): one line per recorded change, by card. */
  private activityByCardId = new Map<string, MockActivityRecord[]>();
  /** Files attached to an item, by card. */
  private attachmentsByCardId = new Map<string, MockAttachmentRecord[]>();
  /** Stored results of file uploads by Idempotency-Key (a replay adds no second file). */
  private uploadsByIdempotencyKey = new Map<string, MockAttachmentRecord>();
  /** Files attached to a Documents page, by page (the bytes are kept). */
  private attachmentsByPageId = new Map<string, MockPageAttachmentRecord[]>();
  /** Stored results of page file uploads by page and Idempotency-Key. */
  private pageUploadsByKey = new Map<string, MockPageAttachmentRecord>();
  /** Links between the items of a board (dependencies and relations). */
  private itemLinks: MockItemLinkRecord[] = [];
  /** People who follow an item besides the member, by card. */
  private followersByCardId = new Map<string, string[]>();
  /** Phases and milestones of a board (gap `CTR-GAP-WM-PLAN-ITEMS`), by board. */
  private planByBoardId = new Map<string, MockBoardPlan>();
  /** Board members (`/full` members), keyed by board. */
  private boardMembersByBoardId = new Map<string, MockBoardMemberRecord[]>();
  /** Explicit board access entries, keyed by board. */
  private boardGrantsByBoardId = new Map<string, MockBoardGrantRecord[]>();
  /** The Board permissions `WorkManagement.Boards.GetSharing` answers with. */
  readonly sharing = new MockBoardSharing();
  /** The saved views of the boards (shared with members or private). */
  readonly savedViews = new MockBoardSavedViews(() => this);
  /** Seeded board audiences for the access read, keyed by board. */
  private boardAudienceByBoardId = new Map<string, MockBoardAudienceRecord>();

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

  /** Space org tree, Space members and Teams (OT-5). */
  readonly spaces = new MockSpaceState();
  /** The pages' history entries and the workspace page templates. */
  readonly docsHistory = new MockDocsHistoryState(this);

  /** The member's work feed: relationships, approvals, mentions, inbox. */
  readonly feed = new MockWorkFeedState();

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

  /** The clock's current time as an ISO string. */
  now(): string {
    return this.clock.isoNow();
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
      name: isUnicode ? "Nguyễn Thu Hà 🚀" : "Nguyễn Thu Hà",
      avatarUrl: isMissingAvatars
        ? null
        : "https://api.dicebear.com/7.x/avataaars/svg?seed=owner",
    });
    const admin = this.factories.user(1, {
      id: mockIds.users.admin,
      email: "admin@notrelix.local",
      name: isUnicode ? "Alex Rivera (Admin) ✨" : "Alex Rivera (Admin)",
      avatarUrl: isMissingAvatars
        ? null
        : "https://api.dicebear.com/7.x/avataaars/svg?seed=admin",
    });
    const member = this.factories.user(2, {
      id: mockIds.users.member,
      email: "member@notrelix.local",
      name: isUnicode ? "Jordan Lee (Member) 💼" : "Jordan Lee (Member)",
      avatarUrl: isMissingAvatars
        ? null
        : "https://api.dicebear.com/7.x/avataaars/svg?seed=member",
    });
    const viewer = this.factories.user(3, {
      id: mockIds.users.viewer,
      email: "viewer@notrelix.local",
      name: isUnicode ? "Taylor Morgan (Viewer) 👀" : "Taylor Morgan (Viewer)",
      avatarUrl: isMissingAvatars
        ? null
        : "https://api.dicebear.com/7.x/avataaars/svg?seed=viewer",
    });

    // Đỗ Khánh Linh (persona `commenter`): a workspace member only in her own
    // world, so the other personas' member lists stay as they are.
    const commenter = this.factories.user(4, {
      id: ERP_COMMENTER.id,
      email: ERP_COMMENTER.email,
      name: ERP_COMMENTER.name,
      avatarUrl: null,
    });

    const actors = [owner, admin, member, viewer, commenter];

    if (this.config.state === "new-user") {
      const pMap: Record<string, any> = {
        owner,
        admin,
        member,
        viewer,
        commenter,
      };
      const u = pMap[this.config.persona] || owner;
      this.insertUser(u);
      this.userPreferencesByUserId.set(u.id, {
        userId: u.id,
        theme: "system",
        colorTheme: "zinc",
        sidebarCollapsed: false,
        defaultView: "board",
      });
      return;
    }

    for (const u of actors) {
      this.insertUser(u);
      this.userPreferencesByUserId.set(u.id, {
        userId: u.id,
        theme: "system",
        colorTheme: "zinc",
        sidebarCollapsed: false,
        defaultView: "board",
      });
    }

    // ── Workspaces ──────────────────────────────────────────────────────────
    let primaryWsName = "Minh An Group";
    if (isUnicode) primaryWsName = "🪐 Minh An Group (プロダクト)";
    if (isLongTitles)
      primaryWsName =
        "Minh An Group — Enterprise Digital Workspace & Engineering Innovation Hub";

    for (let w = 0; w < manifest.workspacesVisibleToOwner; w++) {
      const isPrimary = w === 0;
      const isSecondary = w === 1;
      let wName = `Workspace ${w}`;
      let id = this.factories.workspace(w).id;

      if (isPrimary) {
        wName = primaryWsName;
        id = mockIds.workspaces.primary;
      } else if (isSecondary) {
        wName = "Đà Nẵng";
        id = mockIds.workspaces.secondary;
      } else if (w === 2) {
        wName = "Tân Lộc";
      }

      const ws = this.factories.workspace(w, {
        id,
        name: wName,
        slug: id,
        isPersonal: false,
      });
      this.insertWorkspace(ws);

      if (isPrimary) {
        this.insertMembership(
          this.factories.membership(0, id, owner.id, { role: "owner" }),
        );
        this.insertMembership(
          this.factories.membership(1, id, admin.id, { role: "admin" }),
        );
        this.insertMembership(
          this.factories.membership(2, id, member.id, { role: "member" }),
        );
        this.insertMembership(
          this.factories.membership(3, id, viewer.id, { role: "guest" }),
        );
        if (this.config.persona === "commenter") {
          this.insertMembership(
            this.factories.membership(4, id, commenter.id, { role: "member" }),
          );
        }
      } else {
        this.insertMembership(
          this.factories.membership(w * 10, id, owner.id, { role: "owner" }),
        );
        // The third workspace has a second owner, so the persona may leave it
        // while the others still show the last-owner rule.
        if (w === 2) {
          this.insertMembership(
            this.factories.membership(w * 10 + 1, id, admin.id, {
              role: "owner",
            }),
          );
        }
      }
    }

    // ── Views ──────────────────────────────────────────────────────────────
    const primaryWsId = mockIds.workspaces.primary;
    this.insertView({
      id: mockIds.views.kanban,
      workspaceId: primaryWsId,
      name: "Product Roadmap",
      type: "kanban",
      icon: "Layout",
      description: "",
      visibility: "workspace",
      isDefault: true,
      position: 0,
      createdAt: this.clock.offsetDays(-14),
    });
    this.insertView({
      id: mockIds.views.table,
      workspaceId: primaryWsId,
      name: "All Tasks",
      type: "table",
      icon: "BarChart",
      description: "",
      visibility: "workspace",
      isDefault: false,
      position: 1,
      createdAt: this.clock.offsetDays(-10),
    });

    if (this.config.state === "empty-workspace") return;

    this.seedSpaces(primaryWsId);

    // ── Boards ─────────────────────────────────────────────────────────────
    let primaryBoardTitle = "Product Roadmap";
    if (isUnicode) primaryBoardTitle = "🚀 Product Roadmap (日本語・Üñîçødé)";
    if (isLongTitles)
      primaryBoardTitle =
        "Product Roadmap — Comprehensive Multi-Quarter Strategic Deliverables & Features";

    for (let b = 0; b < manifest.boardsInPrimaryWorkspace; b++) {
      const isPrimaryBoard = b === 0;
      let bId = this.factories.board(b, primaryWsId).id;
      let bTitle = SECONDARY_BOARD_TITLES[b - 1] ?? `Board ${b}`;

      if (isPrimaryBoard) {
        bId = "mock-board-roadmap";
        bTitle = primaryBoardTitle;
      }

      const board = this.factories.board(b, primaryWsId, {
        id: bId,
        title: bTitle,
      });
      this.insertBoard(board);
      this.boardViewsByBoardId.set(bId, {
        boardId: bId,
        viewMode: "table",
        viewConfig: "{}",
      });

      if (isPrimaryBoard) {
        this.insertColumn({
          id: "col-0001",
          boardId: bId,
          name: "Status",
          fieldType: "status",
          position: 1,
          options: defaultStatusOptions("mock-status"),
        });
        this.insertColumn({
          id: "col-0002",
          boardId: bId,
          name: "Assignee",
          fieldType: "user",
          position: 2,
        });
        this.insertColumn({
          id: "col-0003",
          boardId: bId,
          name: "Due Date",
          fieldType: "date",
          position: 3,
        });
        this.insertColumn({
          id: "col-0004",
          boardId: bId,
          name: "Priority",
          fieldType: "select",
          position: 4,
        });
        this.insertLabel({
          id: "lbl-0001",
          boardId: bId,
          name: "Bug",
          color: "#EF4444",
        });
        this.insertLabel({
          id: "lbl-0002",
          boardId: bId,
          name: "Feature",
          color: "#1E90FF",
        });
        this.insertLabel({
          id: "lbl-0003",
          boardId: bId,
          name: "Design",
          color: "#A855F7",
        });
      }

      // ── Lists ────────────────────────────────────────────────────────────
      const listCount = isPrimaryBoard
        ? manifest.listsPerPrimaryBoard
        : this.config.density === "stress"
          ? 8
          : 4;
      const isManyColumns =
        isPrimaryBoard && this.config.overlays.includes("many-columns");
      const actualListCount = isManyColumns
        ? Math.max(listCount, 8)
        : listCount;

      for (let l = 0; l < actualListCount; l++) {
        let lId = this.factories.list(l + b * 100, bId).id; // Use offset so list IDs don't collide if not using board-specific padding
        let lTitle = `List ${l}`;

        if (isPrimaryBoard) {
          if (l === 0) {
            lId = "list-todo";
            lTitle = isUnicode ? "📋 To Do (準備中)" : "To Do";
          }
          if (l === 1) {
            lId = "list-inprogress";
            lTitle = isUnicode ? "⚡ In Progress (進行中)" : "In Progress";
          }
          if (l === 2) {
            lId = "list-done";
            lTitle = isUnicode ? "✅ Done (完了)" : "Done";
          }
          if (l >= 3 && isManyColumns && l < 8) {
            lId = `list-col-${l}`;
            lTitle = `Column ${l}`;
          }
        }

        const lst = this.factories.list(l + b * 100, bId, {
          id: lId,
          title: lTitle,
        });
        this.insertList(lst);

        // ── Cards ──────────────────────────────────────────────────────────
        let cardCount = manifest.cardsPerList;
        if (
          isPrimaryBoard &&
          this.config.overlays.includes("many-cards") &&
          l === 1
        ) {
          cardCount = 100;
        }

        for (let c = 0; c < cardCount; c++) {
          let cTitle = `Task ${c}`;
          let cId = this.factories.card(c + l * 1000 + b * 100000, bId, lId).id;

          if (isPrimaryBoard && l === 1 && c === 0) {
            cId = "card-main-0001";
            cTitle = "Ship mock runtime";
          }
          if (isUnicode) cTitle = `[🚀] ${cTitle}`;
          if (isLongTitles)
            cTitle = `${cTitle} — Extended In-Depth Description`;

          const card = this.factories.card(
            c + l * 1000 + b * 100000,
            bId,
            lId,
            { id: cId, title: cTitle },
          );
          this.insertCard(card);
        }
      }
    }

    this.seedRoadmapStatuses();
    this.seedErpBoard(primaryWsId);
    this.seedDepartmentBoards(primaryWsId);
    this.seedInvoiceDoc(primaryWsId);
    this.feed.seed(this);

    // Extras for card-main-0001
    if (this.cardsById.has("card-main-0001")) {
      this.addIndex(this.labelIdsByCardId, "card-main-0001", "lbl-0002");
      this.insertChecklist({
        id: "chk-0001",
        cardId: "card-main-0001",
        title: "Release Checklist",
        position: 1,
      });
      this.insertChecklistItem({
        id: "chki-0001",
        checklistId: "chk-0001",
        title: "Contract tests green",
        isChecked: true,
      });
      this.insertChecklistItem({
        id: "chki-0002",
        checklistId: "chk-0001",
        title: "E2E verification complete",
        isChecked: false,
      });
      this.insertComment({
        id: "cmt-0001",
        cardId: "card-main-0001",
        userId: owner.id,
        contentMd: "Initial spec.",
        createdAt: this.clock.offsetDays(-2),
      });
    }

    // ── Notifications ──────────────────────────────────────────────────────
    this.seedNotifications(owner.id, manifest.notificationsPerCurrentActor);
    if (this.config.persona !== "owner") {
      const activeUser = this.getCurrentUser();
      this.seedNotifications(
        activeUser.id,
        manifest.notificationsPerCurrentActor,
      );
    }

    // ── Pages ──────────────────────────────────────────────────────────────
    for (let p = 0; p < manifest.pagesInPrimaryWorkspace; p++) {
      const curated = LIBRARY_DOCS[p];
      let pTitle = curated?.title ?? `Page ${p}`;
      if (isUnicode) pTitle = `📑 ${pTitle} (ドキュメント)`;
      if (isLongTitles)
        pTitle = `${pTitle} — Architectural Specification & Engineering Requirements`;

      let pId = this.factories.page(p, primaryWsId).id;
      if (curated) pId = curated.id;

      const page = this.factories.page(p, primaryWsId, {
        id: pId,
        title: pTitle,
        ...(curated
          ? {
              spaceId: curated.spaceId,
              parentId: curated.parentId,
              createdAt: curated.createdAt,
              updatedAt: curated.updatedAt,
            }
          : {}),
      });
      this.insertPage(page);

      // Representative blocks
      const blockCount = curated
        ? curated.blocks.length
        : p === 0
          ? manifest.blocksPerRepresentativePage
          : 2;
      for (let blk = 0; blk < blockCount; blk++) {
        const source = curated ? curated.blocks[blk]! : `Block ${blk}`;
        const isSection = source.startsWith("## ");
        const text = blk === 0 ? page.title : source.replace(/^## /, "");
        this.createBlock(pId, {
          type: blk === 0 ? "heading_1" : isSection ? "heading_2" : "paragraph",
          properties: JSON.stringify({ text }),
          position: blk * 1000,
        });
      }

      if (curated) {
        this.createPageHistory(
          pId,
          curated.editorId,
          "created_page",
          page.title,
          curated.createdAt,
        );
        continue;
      }
      this.createPageComment(pId, owner.id, "This is a seeded page comment.");
      this.createPageHistory(pId, owner.id, "created_page", page.title);
    }
  }

  // ─── Private seed helpers ─────────────────────────────────────────────────

  /**
   * Department Spaces of the primary workspace (OT-5): Engineering (Private,
   * with an open Platform folder), Accounting (Private) and Marketing (open).
   * The owner created them (Space Admin); Jordan (member) is a Member of
   * Engineering; the Helpdesk team (the owner as Lead, Jordan and Taylor) views
   * Accounting.
   */
  private seedSpaces(workspaceId: string): void {
    const at = this.clock.offsetDays(-20);
    const space = (
      id: string,
      name: string,
      position: string,
      extra: Partial<Parameters<MockSpaceState["insertSpace"]>[0]> = {},
    ) =>
      this.spaces.insertSpace({
        id,
        workspaceId,
        parentId: null,
        name,
        description: null,
        type: "Department",
        visibility: "Private",
        archived: false,
        position,
        version: 2,
        createdAt: at,
        ...extra,
      });
    space(mockIds.spaces.engineering, "Công nghệ", "a0000", {
      color: "blue",
      description:
        "Vận hành hạ tầng, phần mềm nội bộ và triển khai ERP cho toàn tập đoàn.",
    });
    space(mockIds.spaces.itInfra, "Nhóm Hạ tầng", "a0001", {
      parentId: mockIds.spaces.engineering,
      type: "Folder",
      visibility: "Workspace",
    });
    space(mockIds.spaces.itApps, "Nhóm Ứng dụng", "a0002", {
      parentId: mockIds.spaces.engineering,
      type: "Folder",
    });
    space(mockIds.spaces.accounting, "Kế toán", "a0001", { color: "gold" });
    space(mockIds.spaces.marketing, "Marketing", "a0002", {
      color: "coral",
      visibility: "Workspace",
    });
    // The one department with nothing in it, as the Space mockup shows.
    space(mockIds.spaces.hr, "Nhân sự", "a0003", {
      color: "sage",
    });
    space(mockIds.spaces.executive, "Ban giám đốc", "a0004", {
      color: "crimson",
    });
    // Lê Hoàng Phúc heads Công nghệ: listed before the owner.
    this.spaces.insertMember({
      spaceId: mockIds.spaces.engineering,
      subjectType: "User",
      subjectId: ERP_PEOPLE.HP.id,
      role: "Admin",
      addedAt: at,
    });
    for (const id of Object.values(mockIds.spaces)) {
      this.spaces.insertMember({
        spaceId: id,
        subjectType: "User",
        subjectId: mockIds.users.owner,
        role: "Admin",
        addedAt: at,
      });
    }
    // Đỗ Khánh Linh (persona `commenter`) administers the Accounting department.
    this.spaces.insertMember({
      spaceId: mockIds.spaces.accounting,
      subjectType: "User",
      subjectId: mockIds.users.commenter,
      role: "Admin",
      addedAt: at,
    });
    // The Công nghệ department of the Space mockup: 24 people (see
    // `mock-space-people.ts`): 2 admins, 19 members and 3 viewers.
    const engineering = (
      subjectType: "User" | "Team",
      subjectId: string,
      role: "Admin" | "Member" | "Viewer",
      addedAt = at,
    ) =>
      this.spaces.insertMember({
        spaceId: mockIds.spaces.engineering,
        subjectType,
        subjectId,
        role,
        addedAt,
      });
    for (const id of [
      ERP_PEOPLE.MQ.id,
      ERP_PEOPLE.VT.id,
      ERP_PEOPLE.LT.id,
      ERP_PEOPLE.TT.id,
      ERP_PEOPLE.PH.id,
      mockIds.users.member,
      ...ENGINEERING_MEMBERS.map((entry) => entry.id),
    ]) {
      engineering("User", id, "Member");
    }
    engineering(
      "Team",
      ERP_ENGINEERS_TEAM.id,
      "Member",
      "2026-09-01T02:00:00.000Z",
    );
    engineering(
      "Team",
      EXECUTIVE_TEAM.id,
      "Viewer",
      "2026-08-15T02:00:00.000Z",
    );
    for (const person of [
      ...ENGINEERING_MEMBERS,
      ...ERP_TEAM_ONLY,
      ...EXECUTIVES,
      ...QA_PEOPLE,
      ...ACCOUNTING_PEOPLE,
    ]) {
      if (!this.usersById.has(person.id)) {
        this.insertUser({
          id: person.id,
          email: person.email,
          name: person.name,
          avatarUrl: null,
        });
      }
    }
    for (const team of [
      ERP_ENGINEERS_TEAM,
      EXECUTIVE_TEAM,
      ACCOUNTING_TEAM,
      QA_TEAM,
      ONCALL_TEAM,
    ]) {
      this.spaces.insertTeam({
        id: team.id,
        workspaceId,
        name: team.name,
        description: team.description,
        archived: team.archived,
        version: 1,
        members: team.members.map((member) => ({ ...member })),
        createdAt: team.createdAt,
      });
    }
    // Ban giám đốc is also a viewer of Kế toán (the Teams mockup).
    this.spaces.insertMember({
      spaceId: mockIds.spaces.accounting,
      subjectType: "Team",
      subjectId: EXECUTIVE_TEAM.id,
      role: "Viewer",
      addedAt: "2026-08-15T02:00:00.000Z",
    });
  }

  /**
   * The roadmap's Status values follow its groups: To Do → Cần làm,
   * In Progress → Đang làm, Done → Hoàn thành, any other group → Chờ duyệt.
   * Items created later have no status until one is set.
   */
  private seedRoadmapStatuses(): void {
    const statusByList: Record<string, string> = {
      "list-todo": "mock-status-todo",
      "list-inprogress": "mock-status-doing",
      "list-done": "mock-status-done",
    };
    for (const list of this.getLists(mockIds.boards.roadmap)) {
      const optionId = statusByList[list.id] ?? "mock-status-review";
      for (const card of this.getCards(list.id)) {
        this.updateFieldValue(card.id, "col-0001", optionId);
      }
    }
  }

  /**
   * The Accounting document of the 2026-10 Docs mockup (see
   * `mock-docs-invoice.ts`): a folder page and the page inside it, with
   * blocks that mention ERP board items, two comments and the edit history.
   */
  private seedInvoiceDoc(workspaceId: string): void {
    const created = "2026-10-05T07:20:00.000Z";
    this.insertPage({
      id: INVOICE_FOLDER_ID,
      workspaceId,
      title: "Quy trình",
      icon: "Folder",
      spaceId: mockIds.spaces.accounting,
      position: 0,
      createdAt: created,
      updatedAt: created,
    });
    this.insertPage({
      id: INVOICE_DOC_ID,
      workspaceId,
      title: "Quy trình nhập hoá đơn đầu vào",
      navTitle: "Nhập hoá đơn đầu vào",
      icon: "FileText",
      parentId: INVOICE_FOLDER_ID,
      spaceId: mockIds.spaces.accounting,
      position: 0,
      deadline: "2026-11-01T00:00:00.000Z",
      status: "review",
      activeUserIds: [mockIds.users.commenter, "mock-user-erp-trang"],
      createdAt: created,
      updatedAt: "2026-10-11T09:40:00.000Z",
    });
    const invoiceBlockIds = INVOICE_BLOCKS.map(
      (block, index) =>
        this.createBlock(INVOICE_DOC_ID, {
          type: block.type as MockBlockRecord["type"],
          properties: JSON.stringify(block.properties),
          position: (index + 1) * 1000,
        }).id,
    );
    for (const comment of INVOICE_COMMENTS) {
      const target = comment.anchor
        ? INVOICE_BLOCKS[comment.anchor.blockIndex]
        : undefined;
      const start = comment.anchor
        ? String(target?.properties.text ?? "").indexOf(comment.anchor.quote)
        : -1;
      this.createPageComment(
        INVOICE_DOC_ID,
        comment.userId,
        comment.body,
        comment.at,
        comment.anchor && start >= 0
          ? {
              blockId: invoiceBlockIds[comment.anchor.blockIndex] as string,
              quote: comment.anchor.quote,
              start,
              end: start + comment.anchor.quote.length,
            }
          : undefined,
      );
    }
    [...INVOICE_HISTORY].reverse().forEach((entry) => {
      this.createPageHistory(
        INVOICE_DOC_ID,
        entry.userId,
        entry.action,
        entry.detail,
        entry.at,
        entry.versionName,
      );
    });
    this.docsHistory.seedInvoiceHistory();
  }

  /**
   * The ERP board of the 2026-10 mockups (see `mock-erp-board.ts`), placed
   * in the Engineering Space, viewed as a Kanban.
   */
  private seedErpBoard(workspaceId: string): void {
    const boardId = mockIds.boards.erp;
    for (const person of Object.values(ERP_PEOPLE)) {
      if (!this.usersById.has(person.id)) {
        this.insertUser({
          id: person.id,
          email: person.email,
          name: person.name,
          avatarUrl: null,
        });
      }
    }
    this.insertBoard({
      id: boardId,
      workspaceId,
      title: "Triển khai ERP Q4",
      description:
        "Đưa phân hệ Kế toán, Kho và Mua hàng lên ERP mới, go-live 01/11/2026.",
      background: { type: "color", value: "#1E6FD0" },
      visibility: "workspace",
      isArchived: false,
      createdAt: "2026-09-15T02:00:00.000Z",
      updatedAt: "2026-10-12T03:28:00.000Z",
      spaceId: mockIds.spaces.engineering,
      version: 1,
      itemKeyPrefix: "ERP",
    });
    this.boardViewsByBoardId.set(boardId, {
      boardId,
      viewMode: "kanban",
      viewConfig: "{}",
    });
    this.insertColumn({
      id: ERP_STATUS_COLUMN_ID,
      boardId,
      name: "Trạng thái",
      fieldType: "status",
      position: 1,
      options: ERP_STATUS_OPTIONS.map((option) => ({ ...option })),
    });
    this.insertColumn({
      id: ERP_PRIORITY_COLUMN_ID,
      boardId,
      name: "Ưu tiên",
      fieldType: "select",
      position: 2,
      options: ERP_PRIORITY_OPTIONS.map((option) => ({ ...option })),
    });
    this.insertColumn({
      id: ERP_OWNER_COLUMN_ID,
      boardId,
      name: "Người thực hiện",
      fieldType: "user",
      position: 3,
    });
    // The custom fields the Detail mockup shows in the right column.
    this.insertColumn({
      id: ERP_MODULE_COLUMN_ID,
      boardId,
      name: "Phân hệ ERP",
      fieldType: "select",
      position: 4,
      options: ERP_MODULE_OPTIONS.map(
        (option, index): MockFieldOptionRecord => ({
          id: option.id,
          name: option.name,
          color: "#64748B",
          position: `a${String(index).padStart(4, "0")}`,
        }),
      ),
    });
    this.insertColumn({
      id: ERP_ESTIMATE_COLUMN_ID,
      boardId,
      name: "Ước lượng",
      fieldType: "number",
      settings: JSON.stringify({ unit: "giờ" }),
      position: 5,
    });
    this.insertColumn({
      id: ERP_REQUESTER_COLUMN_ID,
      boardId,
      name: "Phòng ban yêu cầu",
      fieldType: "select",
      settings: JSON.stringify({ display: "mark" }),
      position: 6,
      options: ERP_REQUESTER_OPTIONS.map(
        (option, index): MockFieldOptionRecord => ({
          id: option.id,
          name: option.name,
          color: option.color,
          position: `a${String(index).padStart(4, "0")}`,
        }),
      ),
    });
    this.settings.set(
      boardId,
      erpSettingsRegistry({
        status: ERP_STATUS_COLUMN_ID,
        priority: ERP_PRIORITY_COLUMN_ID,
        owner: ERP_OWNER_COLUMN_ID,
        module: ERP_MODULE_COLUMN_ID,
        estimate: ERP_ESTIMATE_COLUMN_ID,
        requester: ERP_REQUESTER_COLUMN_ID,
      }),
    );
    for (const label of ERP_LABELS) this.insertLabel({ ...label });
    ERP_GROUPS.forEach((group, index) => {
      this.insertList({
        id: group.id,
        boardId,
        title: group.title,
        color: ["#1E6FD0", "#0E7490", "#5532A8", "#9A6A12"][index] ?? "#1E6FD0",
        position: index + 1,
        isCollapsed: false,
      });
    });
    ERP_ITEMS.forEach((item, index) => {
      const id = erpItemId(item.key);
      this.insertCard({
        id,
        boardId,
        listId: erpGroupId(item.group),
        title: item.title,
        position: index + 1,
        createdAt: ERP_ITEM_CREATED[item.key]?.at ?? "2026-09-21T02:00:00.000Z",
        updatedAt: "2026-10-12T01:40:00.000Z",
        itemKey: item.key,
        description: ERP_DESCRIPTIONS[item.key],
        parentItemId: null,
        dueAt: item.due ? hanoiInstant(item.due, item.dueHour) : null,
        startedAt: item.start ? hanoiInstant(item.start, 9) : null,
        completedAt: item.completed ? hanoiInstant(item.completed, 16) : null,
        assigneeUserIds: item.assignee ? [erpAssigneeId(item.assignee)] : [],
        childProgress: item.children ? { ...item.children } : null,
      });
      this.updateFieldValue(
        id,
        ERP_STATUS_COLUMN_ID,
        erpStatusOptionId(item.status),
      );
      this.updateFieldValue(
        id,
        ERP_PRIORITY_COLUMN_ID,
        erpPriorityOptionId(item.priority),
      );
      this.addIndex(this.labelIdsByCardId, id, erpLabelId(item.label));
    });
    // Child items ("Việc con"): real cards under their parent, so the sheet
    // lists them, ticks them and the parent's progress follows.
    ERP_CHILD_ITEMS.forEach((child, index) => {
      const parent = ERP_ITEMS.find((item) => item.key === child.parentKey);
      if (!parent) return;
      const id = `${erpItemId(child.parentKey)}-child-${index + 1}`;
      this.insertCard({
        id,
        boardId,
        listId: erpGroupId(parent.group),
        title: child.title,
        position: index + 1,
        createdAt: "2026-09-21T02:00:00.000Z",
        updatedAt: "2026-10-12T01:40:00.000Z",
        itemKey: child.key,
        parentItemId: erpItemId(child.parentKey),
        dueAt: hanoiInstant(child.due),
        startedAt: null,
        completedAt: child.done ? hanoiInstant(child.due, 16) : null,
        assigneeUserIds: [erpAssigneeId(child.assignee)],
        childProgress: null,
      });
      this.updateFieldValue(
        id,
        ERP_STATUS_COLUMN_ID,
        erpStatusOptionId(child.done ? "done" : "todo"),
      );
    });
    // Items of the States mockup: opened by address, left out of board reads.
    for (const item of ERP_STATE_ITEMS) {
      const id = erpItemId(item.key);
      const archived = item.archived;
      this.insertCard({
        id,
        boardId,
        listId: erpGroupId(item.group),
        title: item.title,
        position: 100 + this.cardsById.size,
        createdAt: item.createdAt,
        updatedAt: archived?.at ?? item.createdAt,
        itemKey: item.key,
        description: item.description,
        parentItemId: null,
        dueAt: null,
        startedAt: null,
        completedAt: null,
        assigneeUserIds: item.assignee ? [erpAssigneeId(item.assignee)] : [],
        childProgress: item.children
          ? { done: 12, total: item.children }
          : null,
        unlisted: true,
        ...(archived
          ? {
              isArchived: true,
              archivedAt: archived.at,
              archivedByUserId: archived.byUserId,
            }
          : {}),
      });
      this.updateFieldValue(
        id,
        ERP_STATUS_COLUMN_ID,
        erpStatusOptionId(item.status),
      );
      this.updateFieldValue(
        id,
        ERP_PRIORITY_COLUMN_ID,
        erpPriorityOptionId(item.priority),
      );
      this.recordActivity(id, ERP_PEOPLE.HA.id, "tạo việc", item.createdAt, {
        kind: "created",
        verb: "tạo việc",
      });
      erpLongChildTitles()
        .slice(0, item.children ?? 0)
        .forEach((title, index) => {
          const done = index === 0 || (index >= 2 && index <= 12);
          const childId = `${id}-child-${index + 1}`;
          this.insertCard({
            id: childId,
            boardId,
            listId: erpGroupId(item.group),
            title,
            position: index + 1,
            createdAt: item.createdAt,
            updatedAt: item.createdAt,
            itemKey: `${item.key}.${index + 1}`,
            parentItemId: id,
            dueAt: null,
            startedAt: null,
            completedAt: done ? hanoiInstant("11/10", 16) : null,
            assigneeUserIds: [],
            childProgress: null,
            unlisted: true,
          });
          this.updateFieldValue(
            childId,
            ERP_STATUS_COLUMN_ID,
            erpStatusOptionId(done ? "done" : "todo"),
          );
        });
    }
    const planId = (ref: string) =>
      ref.startsWith("ERP-") ? erpItemId(ref) : ref;
    this.planByBoardId.set(boardId, {
      items: ERP_PLAN_ITEMS.map(({ groupKey, ...item }) => ({
        ...item,
        groupId: erpGroupId(groupKey),
      })),
      links: [],
      groups: Object.fromEntries(
        Object.entries(ERP_PLAN_GROUPS).map(([key, group]) => [
          erpItemId(key),
          erpGroupId(group),
        ]),
      ),
      percents: Object.fromEntries(
        Object.entries(ERP_ITEM_PERCENT).map(([key, value]) => [
          erpItemId(key),
          value,
        ]),
      ),
    });
    this.itemLinks = [
      ...ERP_PLAN_LINKS.map((link, index) => ({
        id: `link-dep-${index + 1}`,
        boardId,
        from: planId(link.from),
        to: planId(link.to),
        kind: "dependency" as const,
      })),
      ...ERP_RELATED_LINKS.map((link, index) => ({
        id: `link-rel-${index + 1}`,
        boardId,
        from: planId(link.from),
        to: planId(link.to),
        kind: "related" as const,
      })),
    ];
    for (const entry of ERP_HISTORY) {
      this.recordActivity(
        erpItemId(entry.itemKey),
        entry.userId,
        entry.action,
        entry.createdAt,
        {
          kind: entry.kind,
          automationName: entry.automationName,
          verb: entry.verb,
          target: entry.target,
          notified: entry.notified,
          detail: entry.detail,
          compact: entry.compact,
        },
      );
    }
    for (const [key, values] of Object.entries(ERP_CUSTOM_VALUES)) {
      for (const [columnId, value] of Object.entries(values))
        this.updateFieldValue(erpItemId(key), columnId, value);
    }
    for (const [key, userIds] of Object.entries(ERP_FOLLOWERS))
      this.followersByCardId.set(erpItemId(key), [...userIds]);
    for (const file of ERP_FILES) {
      const list = this.attachmentsByCardId.get(erpItemId(file.itemKey)) ?? [];
      list.push({
        id: file.id,
        cardId: erpItemId(file.itemKey),
        filename: file.filename,
        sizeBytes: file.sizeBytes,
        contentType: file.contentType,
        uploadedBy: file.userId,
        createdAt: file.createdAt,
      });
      this.attachmentsByCardId.set(erpItemId(file.itemKey), list);
    }
    for (const comment of ERP_COMMENTS) {
      this.insertComment({
        id: this.nextId("cmt"),
        cardId: erpItemId(comment.itemKey),
        userId: comment.userId,
        contentMd: comment.contentMd,
        createdAt: comment.createdAt,
      });
    }
    for (const comment of ERP_EXTRA_COMMENTS) {
      this.insertComment({
        id: this.nextId("cmt"),
        cardId: erpItemId(comment.itemKey),
        userId: comment.userId,
        contentMd: comment.contentMd,
        createdAt: comment.createdAt,
        ...("attachmentFileId" in comment
          ? { attachmentIds: [comment.attachmentFileId] }
          : {}),
      });
    }
    this.boardMembersByBoardId.set(
      boardId,
      ERP_BOARD_MEMBERS.map((member) => ({ ...member })),
    );
    this.boardGrantsByBoardId.set(boardId, [
      {
        boardId,
        userId: ERP_COMMENTER.id,
        level: "commenter",
        grantedByUserId: ERP_PEOPLE.HP.id,
      },
      // Đặng Minh Tuấn, from a partner company, edits here until 31/01/2027.
      {
        boardId,
        userId: ERP_PEOPLE.DT.id,
        level: "editor",
        grantedByUserId: ERP_PEOPLE.HP.id,
      },
      // The viewer persona (a workspace guest) can only view this board.
      {
        boardId,
        userId: mockIds.users.viewer,
        level: "viewer",
        grantedByUserId: ERP_PEOPLE.HP.id,
      },
    ]);
    seedErpSharing(this.sharing);
    this.workspaceAutomations.seed(
      ERP_AUTOMATION_RULES.map((rule) => ({
        ...rule,
        actions: rule.actions.map((action) => ({ ...action })),
      })),
      ERP_AUTOMATION_EXECUTIONS.map((execution) => ({
        ...execution,
        steps: execution.steps.map((step) => ({ ...step })),
      })),
    );
    this.boardAudienceByBoardId.set(boardId, {
      ...ERP_AUDIENCE,
      sources: ERP_AUDIENCE.sources.map((source) => ({ ...source })),
    });
  }

  private seedDepartmentBoards(workspaceId: string): void {
    for (const dept of [...DEPARTMENT_BOARDS, ...TECH_SIDE_BOARDS]) {
      this.insertBoard({
        id: dept.id,
        workspaceId,
        title: dept.title,
        description: dept.description,
        background: { type: "color", value: dept.color },
        visibility: dept.visibility ?? "workspace",
        isArchived: false,
        createdAt: "2026-09-15T02:00:00.000Z",
        updatedAt: "2026-10-12T01:00:00.000Z",
        spaceId: dept.spaceId,
        version: 1,
        itemKeyPrefix: dept.prefix,
      });
      this.boardViewsByBoardId.set(dept.id, {
        boardId: dept.id,
        viewMode: "table",
        viewConfig: "{}",
      });
      this.insertColumn({
        id: dept.statusColumnId,
        boardId: dept.id,
        name: "Trạng thái",
        fieldType: "status",
        position: 1,
        options: defaultStatusOptions(dept.statusPrefix),
      });
      this.insertColumn({
        id: dept.priorityColumnId,
        boardId: dept.id,
        name: "Ưu tiên",
        fieldType: "select",
        position: 2,
        options: ERP_PRIORITY_OPTIONS.map((option) => ({
          ...option,
          id: option.id.replace(
            "mock-erp-priority",
            `${dept.statusPrefix}-priority`,
          ),
        })),
      });
      this.insertList({
        id: dept.listId,
        boardId: dept.id,
        title: dept.listTitle,
        color: dept.color,
        position: 1,
        isCollapsed: false,
      });
      dept.items.forEach((item, index) => {
        const id = departmentItemId(item.key);
        this.insertCard({
          id,
          boardId: dept.id,
          listId: dept.listId,
          title: item.title,
          description: item.description,
          position: index + 1,
          createdAt: "2026-09-28T02:00:00.000Z",
          updatedAt: "2026-10-12T01:00:00.000Z",
          itemKey: item.key,
          parentItemId: null,
          dueAt: item.due ? hanoiInstant(item.due, item.dueHour) : null,
          startedAt: null,
          completedAt: item.completed
            ? hanoiInstant(item.completed, item.completedHour)
            : null,
          assigneeUserIds: item.assigneeId ? [item.assigneeId] : [],
          childProgress: null,
        });
        this.updateFieldValue(
          id,
          dept.statusColumnId,
          `${dept.statusPrefix}-${item.status}`,
        );
        this.updateFieldValue(
          id,
          dept.priorityColumnId,
          `${dept.statusPrefix}-priority-${item.priority}`,
        );
      });
    }
    // Hạ tầng & bảo mật gave the on-call team the right to view it (paused with the team).
    this.sharing.setBoard(
      "mock-board-it-infra",
      [
        {
          id: "share-infra-oncall",
          kind: "team",
          userId: null,
          teamId: mockIds.teams.oncall,
          level: "viewer",
          grantLevel: "viewer",
          memberRole: null,
          guest: false,
          owner: false,
          addedAt: "2026-08-20T02:00:00.000Z",
        },
      ],
      [],
    );
    // Ngân sách CNTT 2027 is private: four people were added to it.
    this.boardMembersByBoardId.set(
      "mock-board-it-budget",
      (
        [
          [ERP_PEOPLE.HP.id, "Owner"],
          [ERP_PEOPLE.HA.id, "Admin"],
          [ERP_PEOPLE.MQ.id, "Member"],
          [ENGINEERING_MEMBERS[0]!.id, "Member"],
        ] as const
      ).map(([userId, role]) => ({
        boardId: "mock-board-it-budget",
        userId,
        role,
        joinedAt: "2026-09-15T02:00:00.000Z",
      })) as MockBoardMemberRecord[],
    );
  }

  /** Members of a board, in seeded order. */
  getBoardMembers(boardId: string): MockBoardMemberRecord[] {
    return [...(this.boardMembersByBoardId.get(boardId) ?? [])];
  }

  /** The explicit access entry of a user on a board, when there is one. */
  getBoardGrant(
    boardId: string,
    userId: string,
  ): MockBoardGrantRecord | undefined {
    return this.boardGrantsByBoardId
      .get(boardId)
      ?.find((grant) => grant.userId === userId);
  }

  /** Sets (or adds) a user's explicit level on a board. */
  setBoardGrant(
    boardId: string,
    userId: string,
    level: MockBoardGrantRecord["level"],
    grantedByUserId: string,
  ): void {
    const list = this.boardGrantsByBoardId.get(boardId) ?? [];
    const existing = list.find((grant) => grant.userId === userId);
    if (existing) existing.level = level;
    else list.push({ boardId, userId, level, grantedByUserId });
    this.boardGrantsByBoardId.set(boardId, list);
  }

  /** Removes a user's explicit entry on a board. */
  removeBoardGrant(boardId: string, userId: string): void {
    const list = this.boardGrantsByBoardId.get(boardId);
    if (!list) return;
    this.boardGrantsByBoardId.set(
      boardId,
      list.filter((grant) => grant.userId !== userId),
    );
  }

  /** Switches who can open a board (the share dialog's "Private"). */
  setBoardVisibility(
    boardId: string,
    visibility: "private" | "workspace",
  ): void {
    const board = this.boardsById.get(boardId);
    if (board) board.visibility = visibility;
  }

  /** The seeded audience of a board, when the world defines one. */
  getBoardAudience(boardId: string): MockBoardAudienceRecord | undefined {
    return this.boardAudienceByBoardId.get(boardId);
  }

  /** The current member as a Space access subject. */
  spaceViewer(workspaceId: string): MockSpaceViewer {
    return {
      userId: this.getCurrentUser().id,
      workspaceRole: this.getCurrentMemberRole(workspaceId),
    };
  }

  /** Non-deleted Boards and Pages placed in a Space (WSP-022 archive check). */
  countPlacedInSpace(spaceId: string): { boards: number; pages: number } {
    let boards = 0;
    let pages = 0;
    for (const board of this.boardsById.values()) {
      if (board.spaceId === spaceId && !board.isArchived) boards += 1;
    }
    for (const page of this.pagesById.values()) {
      if (page.spaceId === spaceId) pages += 1;
    }
    return { boards, pages };
  }

  getBoardVersion(boardId: string): number {
    return this.boardsById.get(boardId)?.version ?? 1;
  }

  /** Places a Board in a Space (or at the workspace level), versioned. */
  placeBoard(
    boardId: string,
    spaceId: string | null,
    expectedVersion: unknown,
  ): MockVersionedWrite {
    const board = this.boardsById.get(boardId);
    if (!board) return "not-found";
    const version = board.version ?? 1;
    if (expectedVersion !== version) return "stale";
    if ((board.spaceId ?? null) === spaceId) return "ok";
    board.spaceId = spaceId;
    board.version = version + 1;
    return "ok";
  }

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
        properties: JSON.stringify({
          text: "This is a seeded mock document block.",
        }),
        position: 65536,
      });

      // Seed comments
      this.createPageComment(
        page.id,
        mockIds.users.owner,
        "This is a seeded page comment.",
      );

      // Seed history
      this.createPageHistory(
        page.id,
        mockIds.users.owner,
        "created_page",
        page.title,
      );
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
    this.settings.clear();
    this.listsById.clear();
    this.cardsById.clear();
    this.notificationsById.clear();
    this.pagesById.clear();
    this.boardViewsByBoardId.clear();
    this.savedViews.clear();
    this.columnsById.clear();
    this.labelsById.clear();
    this.checklistsById.clear();
    this.checklistItemsById.clear();
    this.commentsById.clear();
    this.fieldValuesByCardId.clear();
    this.userPreferencesByUserId.clear();
    this.loggedOutUserIds.clear();
    this.auth.reset();
    this.onboarding.reset();
    this.accountPages.reset();
    this.reports.reset();
    this.billing.reset();
    this.integrations.reset();
    this.workspaceAutomations.reset();
    this.navigationByMember.clear();
    this.navigationTick = 0;
    this.suspendedMembershipIds.clear();
    this.invitationsById.clear();
    this.settingsByWorkspaceId.clear();
    this.workspaceVersionById.clear();
    this.workspaceDescriptionById.clear();
    this.archivedWorkspaceIds.clear();
    this.boardMembersByBoardId.clear();
    this.boardGrantsByBoardId.clear();
    this.sharing.clear();
    this.boardAudienceByBoardId.clear();
    this.spaces.clear();
    this.docsHistory.clear();
    this.feed.clear();
    this.completedCardIds.clear();
    this.activityByCardId.clear();
    this.attachmentsByCardId.clear();
    this.uploadsByIdempotencyKey.clear();
    this.attachmentsByPageId.clear();
    this.pageUploadsByKey.clear();
    this.itemLinks = [];
    this.followersByCardId.clear();
    this.planByBoardId.clear();

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

  /** Every user the world knows (the mock's workspace people directory). */
  listUsers(): MockUserRecord[] {
    return [...this.usersById.values()];
  }

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
      commenter: mockIds.users.commenter,
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

  /** A sign-in succeeded: the persona's session is open again. */
  loginCurrentUser(): void {
    this.loggedOutUserIds.delete(this.getCurrentUser().id);
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
    input: { title?: string; description?: string; spaceId?: string | null },
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
    this.insertBoard({ ...board, spaceId: input.spaceId ?? null, version: 1 });
    return this.boardsById.get(board.id)!;
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

  /**
   * Adds an option to a Status or Select field and bumps the field version
   * (the backend's `AddFieldOption`): appended after the last option, or
   * between the named neighbours. `stale` = the expected version is behind.
   */
  addFieldOption(
    columnId: string,
    input: {
      name: string;
      color: string;
      category?: MockFieldOptionRecord["category"];
      expectedVersion: number;
    },
  ):
    | { result: "ok"; option: MockFieldOptionRecord }
    | { result: "not-found" | "invalid" | "stale" } {
    const col = this.columnsById.get(columnId);
    if (!col) return { result: "not-found" };
    if (col.fieldType !== "status" && col.fieldType !== "select") {
      return { result: "invalid" };
    }
    if ((col.version ?? 1) !== input.expectedVersion) {
      return { result: "stale" };
    }
    const options = [...(col.options ?? [])].sort((a, b) =>
      a.position < b.position ? -1 : a.position > b.position ? 1 : 0,
    );
    const last = options.at(-1)?.position;
    const option: MockFieldOptionRecord = {
      id: this.nextId("opt"),
      name: input.name,
      color: input.color,
      // Appending "0" to the last key sorts after it and before any longer key.
      position: last === undefined ? "a0" : `${last}0`,
      ...(col.fieldType === "status"
        ? { category: input.category ?? "in_progress" }
        : {}),
    };
    this.columnsById.set(columnId, {
      ...col,
      options: [...options, option],
      version: (col.version ?? 1) + 1,
    });
    return { result: "ok", option };
  }

  /** Renames or recolours an option and bumps the field version. */
  updateFieldOption(
    columnId: string,
    optionId: string,
    patch: {
      name?: string;
      color?: string;
      category?: MockFieldOptionRecord["category"];
      expectedVersion: number;
    },
  ): "ok" | "not-found" | "stale" {
    const col = this.columnsById.get(columnId);
    const option = col?.options?.find((candidate) => candidate.id === optionId);
    if (!col || !option) return "not-found";
    if ((col.version ?? 1) !== patch.expectedVersion) return "stale";
    this.columnsById.set(columnId, {
      ...col,
      options: (col.options ?? []).map((candidate) =>
        candidate.id === optionId
          ? {
              ...candidate,
              name: patch.name ?? candidate.name,
              color: patch.color ?? candidate.color,
              ...(col.fieldType === "status" && patch.category
                ? { category: patch.category }
                : {}),
            }
          : candidate,
      ),
      version: (col.version ?? 1) + 1,
    });
    return "ok";
  }

  /**
   * Removes an option as the producer does: a Status always keeps one Not
   * started and one Done option, the items holding it move to the replacement
   * (a Status needs one when any item holds the option; a Select may be cleared).
   */
  removeFieldOption(
    columnId: string,
    optionId: string,
    input: { expectedVersion: number; replacementOptionId?: string | null },
  ):
    | "ok"
    | "not-found"
    | "stale"
    | "replacement-required"
    | "replacement-invalid"
    | "keeps-todo-and-done" {
    const col = this.columnsById.get(columnId);
    const option = col?.options?.find((candidate) => candidate.id === optionId);
    if (!col || !option) return "not-found";
    if ((col.version ?? 1) !== input.expectedVersion) return "stale";
    const singleValued =
      col.fieldType === "status" || col.fieldType === "select";
    const replacement = input.replacementOptionId
      ? col.options?.find(
          (candidate) =>
            candidate.id === input.replacementOptionId &&
            candidate.id !== optionId,
        )
      : undefined;
    if (input.replacementOptionId && (!singleValued || !replacement)) {
      return "replacement-invalid";
    }
    const holders = singleValued
      ? Array.from(this.cardsById.values()).filter(
          (card) =>
            card.boardId === col.boardId &&
            this.fieldValuesByCardId.get(card.id)?.get(columnId) === optionId,
        )
      : [];
    if (holders.length > 0 && col.fieldType === "status" && !replacement) {
      return "replacement-required";
    }
    if (col.fieldType === "status" && option.category) {
      const category = option.category;
      const remaining = (col.options ?? []).filter(
        (candidate) =>
          candidate.id !== optionId && candidate.category === category,
      );
      if (
        (category === "todo" || category === "done") &&
        remaining.length === 0
      ) {
        return "keeps-todo-and-done";
      }
    }
    for (const card of holders) {
      const fieldMap = this.fieldValuesByCardId.get(card.id);
      if (replacement) fieldMap?.set(columnId, replacement.id);
      else fieldMap?.delete(columnId);
    }
    this.columnsById.set(columnId, {
      ...col,
      options: (col.options ?? []).filter(
        (candidate) => candidate.id !== optionId,
      ),
      version: (col.version ?? 1) + 1,
    });
    return "ok";
  }

  /** Neighbour-relative option move; the options are re-keyed in the new order. */
  moveFieldOption(
    columnId: string,
    optionId: string,
    input: {
      expectedVersion: number;
      previousSiblingId?: string | null;
      nextSiblingId?: string | null;
    },
  ): "ok" | "not-found" | "stale" {
    const col = this.columnsById.get(columnId);
    const options = [...(col?.options ?? [])].sort((a, b) =>
      a.position < b.position ? -1 : a.position > b.position ? 1 : 0,
    );
    const moved = options.find((candidate) => candidate.id === optionId);
    if (!col || !moved) return "not-found";
    if ((col.version ?? 1) !== input.expectedVersion) return "stale";
    const rest = options.filter((candidate) => candidate.id !== optionId);
    const after = input.previousSiblingId
      ? rest.findIndex((candidate) => candidate.id === input.previousSiblingId)
      : -1;
    const before = input.nextSiblingId
      ? rest.findIndex((candidate) => candidate.id === input.nextSiblingId)
      : -1;
    const at = after >= 0 ? after + 1 : before >= 0 ? before : rest.length;
    rest.splice(at, 0, moved);
    this.columnsById.set(columnId, {
      ...col,
      options: rest.map((candidate, index) => ({
        ...candidate,
        position: `a${String(index).padStart(3, "0")}`,
      })),
      version: (col.version ?? 1) + 1,
    });
    return "ok";
  }

  deleteColumn(columnId: string): boolean {
    const col = this.columnsById.get(columnId);
    if (!col) return false;
    this.removeIndex(this.columnIdsByBoardId, col.boardId, col.id);
    this.columnsById.delete(columnId);
    return true;
  }

  /**
   * Neighbor-relative placement over numeric mock positions, mirroring the
   * backend move contract: no neighbors appends, otherwise place between them.
   */
  private placeBetween(
    siblings: { id: string; position: number }[],
    previousSiblingId?: string | null,
    nextSiblingId?: string | null,
  ): number {
    const byId = new Map(siblings.map((s) => [s.id, s.position]));
    const max = siblings.reduce((m, s) => Math.max(m, s.position), 0);
    const prev = previousSiblingId ? byId.get(previousSiblingId) : undefined;
    const next = nextSiblingId ? byId.get(nextSiblingId) : undefined;
    if (prev === undefined && next === undefined) return max + 1;
    if (prev !== undefined && next === undefined) return prev + 1;
    if (prev === undefined && next !== undefined) return next - 1;
    return ((prev as number) + (next as number)) / 2;
  }

  moveColumn(
    boardId: string,
    columnId: string,
    previousSiblingId?: string | null,
    nextSiblingId?: string | null,
  ): boolean {
    const col = this.columnsById.get(columnId);
    if (!col || col.boardId !== boardId) return false;
    const siblings = Array.from(this.columnsById.values()).filter(
      (c) => c.boardId === boardId && c.id !== columnId,
    );
    const position = this.placeBetween(
      siblings,
      previousSiblingId,
      nextSiblingId,
    );
    this.columnsById.set(columnId, { ...col, position });
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

  moveList(
    listId: string,
    previousSiblingId?: string | null,
    nextSiblingId?: string | null,
  ): boolean {
    const list = this.listsById.get(listId);
    if (!list) return false;
    const siblings = Array.from(this.listsById.values()).filter(
      (l) => l.boardId === list.boardId && l.id !== listId,
    );
    const position = this.placeBetween(
      siblings,
      previousSiblingId,
      nextSiblingId,
    );
    this.listsById.set(listId, { ...list, position });
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
    const itemKey = this.nextItemKey(boardId);
    if (itemKey) card.itemKey = itemKey;
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

  /** The phases, milestones, dependencies and completions of a board's plan. */
  getPlan(boardId: string): MockBoardPlan {
    const plan = this.planByBoardId.get(boardId) ?? {
      items: [],
      links: [],
      percents: {},
      groups: {},
    };
    return {
      ...plan,
      links: this.itemLinks
        .filter(
          (link) => link.boardId === boardId && link.kind === "dependency",
        )
        .map((link) => ({ from: link.from, to: link.to })),
    };
  }

  /** Records one line of an item's history ("đổi hạn từ 08/10 sang 10/10"). */
  recordActivity(
    cardId: string,
    actorId: string | null,
    action: string,
    createdAt?: string,
    extra: Omit<
      MockActivityRecord,
      "id" | "actorId" | "action" | "createdAt"
    > = {},
  ): void {
    const list = this.activityByCardId.get(cardId) ?? [];
    list.push({
      id: `act-${cardId}-${list.length + 1}`,
      actorId,
      action,
      createdAt: createdAt ?? this.clock.isoNow(),
      ...extra,
    });
    this.activityByCardId.set(cardId, list);
  }

  getActivity(cardId: string) {
    return [...(this.activityByCardId.get(cardId) ?? [])].sort((a, b) =>
      a.createdAt.localeCompare(b.createdAt),
    );
  }

  private shortDay(iso: string | null | undefined): string {
    return iso ? hanoiShortDay(iso) : "không có hạn";
  }

  updateCard(cardId: string, patch: Partial<MockCardRecord>): boolean {
    const card = this.cardsById.get(cardId);
    if (!card) return false;
    if ("dueAt" in patch && (patch.dueAt ?? null) !== (card.dueAt ?? null)) {
      this.recordActivity(
        cardId,
        this.getCurrentUser().id,
        `đổi hạn từ ${this.shortDay(card.dueAt)} sang ${this.shortDay(patch.dueAt)}`,
        undefined,
        {
          verb: "đổi Hạn chót",
          type: "work.item-field-changed",
          change: {
            fieldName: "Hạn chót",
            from: card.dueAt ? hanoiShortDay(card.dueAt) : "—",
            to: patch.dueAt ? hanoiShortDay(patch.dueAt) : "—",
          },
          detail: {
            kind: "diff",
            from: card.dueAt ? hanoiShortDay(card.dueAt) : "—",
            to: patch.dueAt ? hanoiShortDay(patch.dueAt) : "—",
          },
        },
      );
    }
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

    if (!card.parentItemId) {
      this.trashedCardsById.set(cardId, {
        card,
        deletedAt: this.clock.isoNow(),
        deletedBy: this.getCurrentUser().id,
      });
    }
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

    // The copy keeps the item's values: status, priority, due date, people.
    const values = this.fieldValuesByCardId.get(cardId);
    if (values) this.fieldValuesByCardId.set(newCard.id, new Map(values));
    const current = this.cardsById.get(newCard.id);
    if (current) {
      this.cardsById.set(newCard.id, {
        ...current,
        dueAt: card.dueAt ?? null,
        startedAt: card.startedAt ?? null,
        assigneeUserIds: [...(card.assigneeUserIds ?? [])],
      });
    }

    // Duplicate labels
    const labels = this.labelIdsByCardId.get(cardId);
    if (labels) {
      for (const lblId of labels) {
        this.addIndex(this.labelIdsByCardId, newCard.id, lblId);
      }
    }

    return newCard;
  }

  /** Archives an item: it leaves the board read and keeps its data. */
  archiveCard(cardId: string): boolean {
    const card = this.cardsById.get(cardId);
    if (!card) return false;
    this.cardsById.set(cardId, {
      ...card,
      isArchived: true,
      archivedAt: this.clock.isoNow(),
      archivedByUserId: this.getCurrentUser().id,
      version: (card.version ?? 1) + 1,
      updatedAt: this.clock.isoNow(),
    });
    return true;
  }

  /** Brings an archived item back to its own place on the board. */
  /** The board's deleted items of the last 30 days, newest first. */
  getTrashedCards(boardId: string): {
    card: MockCardRecord;
    deletedAt: string;
    deletedBy: string;
  }[] {
    return [...this.trashedCardsById.values()]
      .filter((entry) => entry.card.boardId === boardId)
      .sort((a, b) => b.deletedAt.localeCompare(a.deletedAt));
  }

  /** Brings a deleted card back to its list; false when it is not in the trash. */
  restoreDeletedCard(cardId: string): boolean {
    const entry = this.trashedCardsById.get(cardId);
    if (!entry || !this.listsById.has(entry.card.listId)) return false;
    this.trashedCardsById.delete(cardId);
    this.cardsById.set(cardId, {
      ...entry.card,
      version: (entry.card.version ?? 1) + 1,
      updatedAt: this.clock.isoNow(),
    });
    this.addIndex(this.cardIdsByListId, entry.card.listId, cardId);
    return true;
  }

  restoreCard(cardId: string): boolean {
    const card = this.cardsById.get(cardId);
    if (!card || !card.isArchived) return false;
    this.cardsById.set(cardId, {
      ...card,
      isArchived: false,
      archivedAt: null,
      archivedByUserId: null,
      version: (card.version ?? 1) + 1,
      updatedAt: this.clock.isoNow(),
    });
    return true;
  }

  /** Adds a person to a board as a member; false when they already are. */
  addBoardMember(boardId: string, userId: string): boolean {
    const members = this.boardMembersByBoardId.get(boardId) ?? [];
    if (members.some((member) => member.userId === userId)) return false;
    this.boardMembersByBoardId.set(boardId, [
      ...members,
      { boardId, userId, role: "Member", joinedAt: this.clock.isoNow() },
    ]);
    return true;
  }

  /**
   * `WorkManagement.BoardItems.Move`, like the server: the item's version must
   * match `expectedVersion` ("stale" otherwise); it is placed between the
   * given neighbours of the target group (both `null` appends) and its
   * version goes up.
   */
  moveItem(
    cardId: string,
    groupId: string,
    placement: {
      readonly expectedVersion: number;
      readonly previousSiblingId: string | null;
      readonly nextSiblingId: string | null;
    },
  ):
    | {
        readonly result: "ok";
        readonly version: number;
        readonly card: MockCardRecord;
      }
    | { readonly result: "not-found" | "invalid" | "stale" } {
    const card = this.cardsById.get(cardId);
    if (!card) return { result: "not-found" };
    const group = this.listsById.get(groupId);
    if (!group || group.boardId !== card.boardId) return { result: "invalid" };
    if ((card.version ?? 1) !== placement.expectedVersion) {
      return { result: "stale" };
    }
    const others = this.getCards(groupId).filter(
      (other) => other.id !== cardId,
    );
    const previous = placement.previousSiblingId
      ? others.find((other) => other.id === placement.previousSiblingId)
      : undefined;
    const next = placement.nextSiblingId
      ? others.find((other) => other.id === placement.nextSiblingId)
      : undefined;
    if (
      (placement.previousSiblingId && !previous) ||
      (placement.nextSiblingId && !next)
    ) {
      return { result: "invalid" };
    }
    const at = (index: number) => others[index]?.position;
    const afterPrevious = previous
      ? at(others.indexOf(previous) + 1)
      : undefined;
    const beforeNext = next ? at(others.indexOf(next) - 1) : undefined;
    const between = (low: number, high: number) => (low + high) / 2;
    const position =
      previous && next
        ? between(previous.position, next.position)
        : previous
          ? afterPrevious !== undefined
            ? between(previous.position, afterPrevious)
            : previous.position + 1
          : next
            ? beforeNext !== undefined
              ? between(beforeNext, next.position)
              : next.position - 1
            : (others.at(-1)?.position ?? 0) + 1;
    this.moveCard(cardId, groupId, position);
    const version = (card.version ?? 1) + 1;
    const moved = { ...this.cardsById.get(cardId)!, version };
    this.cardsById.set(cardId, moved);
    return { result: "ok", version, card: moved };
  }

  /**
   * `WorkManagement.BoardItems.UpdateStatus`: sets the item's option of its
   * board's Status column, like the server. Moving into a Done-category
   * option marks the item completed; moving out clears it. Returns
   * "not-found" for an unknown item and "invalid" when the board has no
   * Status column or the option is not one of it.
   */
  updateItemStatus(
    cardId: string,
    optionId: string,
  ): "ok" | "not-found" | "invalid" {
    const card = this.cardsById.get(cardId);
    if (!card) return "not-found";
    const statusColumn = this.getColumns(card.boardId).find(
      (column) => column.fieldType === "status",
    );
    const option = statusColumn?.options?.find(
      (candidate) => candidate.id === optionId,
    );
    if (!statusColumn || !option) return "invalid";
    const previousId = this.getFieldValues(cardId)[statusColumn.id];
    const previous = statusColumn.options?.find(
      (candidate) => candidate.id === previousId,
    );
    if (previous && previous.id !== option.id) {
      this.recordActivity(
        cardId,
        this.getCurrentUser().id,
        `đổi trạng thái từ ${previous.name} sang ${option.name}`,
        undefined,
        {
          verb: "đổi Trạng thái",
          type: "work.item-status-changed",
          change: {
            fieldName: "Trạng thái",
            from: previous.name,
            to: option.name,
          },
          detail: { kind: "diff", from: previous.name, to: option.name },
        },
      );
    }
    this.updateFieldValue(cardId, statusColumn.id, optionId);
    const done = option.category === "done";
    this.cardsById.set(cardId, {
      ...card,
      version: (card.version ?? 1) + 1,
      completedAt: done ? (card.completedAt ?? this.clock.isoNow()) : null,
      updatedAt: this.clock.isoNow(),
    });
    return "ok";
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
    // A person field's value is the item's assignee set (the item's members).
    const field = this.getColumns(card.boardId).find(
      (column) => column.id === fieldDefinitionId,
    );
    if (
      (field?.fieldType === "user" || field?.fieldType === "person") &&
      Array.isArray(value)
    ) {
      this.cardsById.set(cardId, {
        ...card,
        assigneeUserIds: value.filter(
          (id): id is string => typeof id === "string",
        ),
        updatedAt: this.clock.isoNow(),
      });
    }
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

  /**
   * Moves an item (and its child items) to another board: it lands at the end
   * of the target group with the same name as its current group, else of the
   * target's first group, and takes the next item key of the target board.
   * Gap operation `CTR-GAP-WM-TABLE-BULK`.
   */
  moveItemToBoard(
    cardId: string,
    targetBoardId: string,
  ):
    | { readonly result: "ok"; readonly card: MockCardRecord }
    | { readonly result: "not-found" | "invalid" } {
    const card = this.cardsById.get(cardId);
    const target = this.boardsById.get(targetBoardId);
    if (!card || !target) return { result: "not-found" };
    if (card.boardId === targetBoardId || card.parentItemId)
      return { result: "invalid" };
    const targetLists = this.getLists(targetBoardId);
    if (targetLists.length === 0) return { result: "invalid" };
    const sourceName = this.listsById.get(card.listId)?.title;
    const list =
      targetLists.find((candidate) => candidate.title === sourceName) ??
      targetLists[0]!;
    const now = this.clock.isoNow();
    const position = this.getCards(list.id).length;
    const itemKey = this.nextItemKey(targetBoardId);
    const relocate = (record: MockCardRecord, key: string | null) => {
      this.removeIndex(this.cardIdsByListId, record.listId, record.id);
      this.cardsById.set(record.id, {
        ...record,
        boardId: targetBoardId,
        listId: list.id,
        position,
        itemKey: key,
        version: (record.version ?? 1) + 1,
        updatedAt: now,
      });
      this.addIndex(this.cardIdsByListId, list.id, record.id);
    };
    const children = [...this.cardsById.values()].filter(
      (candidate) => candidate.parentItemId === cardId,
    );
    relocate(card, itemKey);
    this.recordActivity(
      cardId,
      this.getCurrentUser().id,
      `chuyển việc sang board ${target.title}`,
      undefined,
      {
        type: "work.item-moved-to-board",
        change: { to: target.title, ...(itemKey ? { itemKey } : {}) },
      },
    );
    children.forEach((child, index) =>
      relocate(child, itemKey ? `${itemKey}.${index + 1}` : null),
    );
    return { result: "ok", card: this.cardsById.get(cardId)! };
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
    this.feed.noteReplyOn({ itemId: cardId });
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
    input: {
      title?: string;
      icon?: string;
      parentId?: string;
      spaceId?: string | null;
    },
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
    // A nested page follows its parent's Space (OT-4).
    const spaceId = input.parentId
      ? (this.pagesById.get(input.parentId)?.spaceId ?? null)
      : (input.spaceId ?? null);
    this.insertPage({ ...page, spaceId });
    return this.pagesById.get(page.id)!;
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
      version: (page.version ?? 1) + 1,
      updatedAt: this.clock.isoNow(),
    };
    this.pagesById.set(pageId, updated);
    return true;
  }

  getPageVersion(pageId: string): number {
    return this.pagesById.get(pageId)?.version ?? 1;
  }

  /**
   * Places a root page in a Space (or at the workspace level) with its whole
   * subtree, versioned like the producer (`Documents.Pages.MoveToSpace`): a
   * nested page follows its root and is refused (docs/product/workspaces.md
   * §79), a stale version changes nothing, the same place is a no-op.
   */
  placePage(
    pageId: string,
    spaceId: string | null,
    expectedVersion: unknown,
  ): MockVersionedWrite | "nested" {
    const page = this.pagesById.get(pageId);
    if (!page) return "not-found";
    if (page.parentId) return "nested";
    const version = page.version ?? 1;
    if (expectedVersion !== version) return "stale";
    if ((page.spaceId ?? null) === spaceId) return "ok";
    const moving = new Set([pageId]);
    let grew = true;
    while (grew) {
      grew = false;
      for (const other of this.pagesById.values()) {
        if (
          other.parentId &&
          moving.has(other.parentId) &&
          !moving.has(other.id)
        ) {
          moving.add(other.id);
          grew = true;
        }
      }
    }
    for (const id of moving) {
      const record = this.pagesById.get(id)!;
      this.pagesById.set(id, { ...record, spaceId });
    }
    this.pagesById.set(pageId, {
      ...this.pagesById.get(pageId)!,
      version: version + 1,
    });
    return "ok";
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

  // ─── Navigation state (recents and pins, WX-BE-001/002) ─────────────────

  private memberNavigation(workspaceId: string) {
    const key = `${workspaceId}:${this.getCurrentUser().id}`;
    let state = this.navigationByMember.get(key);
    if (!state) {
      state = { recents: [], pins: [] };
      this.navigationByMember.set(key, state);
    }
    return state;
  }

  private nextNavigationTimestamp(): string {
    this.navigationTick += 1;
    return this.clock.offsetSeconds(this.navigationTick);
  }

  /**
   * The workspace's member total as `WorkspaceDto.memberCount` reports it. The
   * primary workspace is the mockups' Minh An Group (86 people) while its
   * member list stays small; every other workspace reports its list.
   */
  getWorkspaceMemberCount(workspaceId: string): number {
    const listed = this.getWorkspaceMembers(workspaceId).length;
    return workspaceId === mockIds.workspaces.primary
      ? Math.max(listed, 86)
      : listed;
  }

  /** True when the current member belongs to the workspace. */
  isCurrentMemberOf(workspaceId: string): boolean {
    const userId = this.getCurrentUser().id;
    return this.getWorkspaceMembers(workspaceId).some(
      (m) => m.userId === userId,
    );
  }

  getRecentResources(workspaceId: string) {
    return [...this.memberNavigation(workspaceId).recents];
  }

  recordRecentResource(
    workspaceId: string,
    kind: string,
    resourceId: string,
  ): void {
    const state = this.memberNavigation(workspaceId);
    const rest = state.recents.filter(
      (entry) => !(entry.kind === kind && entry.resourceId === resourceId),
    );
    state.recents = [
      { kind, resourceId, at: this.nextNavigationTimestamp() },
      ...rest,
    ].slice(0, 30);
  }

  /** Seeds one recent at a stated time (the world's history), newest first. */
  seedRecentResource(
    workspaceId: string,
    kind: string,
    resourceId: string,
    at: string,
  ): void {
    const state = this.memberNavigation(workspaceId);
    state.recents = [...state.recents, { kind, resourceId, at }].sort((a, b) =>
      b.at.localeCompare(a.at),
    );
  }

  getPinnedResources(workspaceId: string) {
    return [...this.memberNavigation(workspaceId).pins];
  }

  /** Returns false when the 50-pin cap is reached. Re-pin is a no-op. */
  pinResource(workspaceId: string, kind: string, resourceId: string): boolean {
    const state = this.memberNavigation(workspaceId);
    if (
      state.pins.some((p) => p.kind === kind && p.resourceId === resourceId)
    ) {
      return true;
    }
    if (state.pins.length >= 50) return false;
    state.pins = [
      { kind, resourceId, at: this.nextNavigationTimestamp() },
      ...state.pins,
    ];
    return true;
  }

  unpinResource(workspaceId: string, kind: string, resourceId: string): void {
    const state = this.memberNavigation(workspaceId);
    state.pins = state.pins.filter(
      (p) => !(p.kind === kind && p.resourceId === resourceId),
    );
  }

  // ─── Members, invitations, settings (WX-MEM-001, WX-SET-001) ────────────

  private findMembership(
    workspaceId: string,
    userId: string,
  ): MockMembershipRecord | undefined {
    return this.getWorkspaceMembers(workspaceId).find(
      (m) => m.userId === userId,
    );
  }

  /** The current member's role in a workspace, when a member. */
  getCurrentMemberRole(
    workspaceId: string,
  ): MockMembershipRecord["role"] | null {
    return (
      this.findMembership(workspaceId, this.getCurrentUser().id)?.role ?? null
    );
  }

  isMembershipSuspended(membershipId: string): boolean {
    return this.suspendedMembershipIds.has(membershipId);
  }

  private activeOwnerCount(workspaceId: string): number {
    return this.getWorkspaceMembers(workspaceId).filter(
      (m) => m.role === "owner" && !this.suspendedMembershipIds.has(m.id),
    ).length;
  }

  changeMemberRole(
    workspaceId: string,
    userId: string,
    role: MockMembershipRecord["role"],
  ): MockMemberChange {
    const member = this.findMembership(workspaceId, userId);
    if (!member) return "not-found";
    if (role === "owner") return "owner-direct";
    if (this.suspendedMembershipIds.has(member.id)) return "inactive";
    if (member.role === "owner" && this.activeOwnerCount(workspaceId) <= 1) {
      return "last-owner";
    }
    member.role = role;
    return "ok";
  }

  removeMember(workspaceId: string, userId: string): MockMemberChange {
    const member = this.findMembership(workspaceId, userId);
    if (!member) return "not-found";
    if (member.role === "owner" && this.activeOwnerCount(workspaceId) <= 1) {
      return "last-owner";
    }
    this.membershipsById.delete(member.id);
    this.membershipIdsByWorkspaceId.get(workspaceId)?.delete(member.id);
    this.suspendedMembershipIds.delete(member.id);
    return "ok";
  }

  setMemberSuspended(
    workspaceId: string,
    userId: string,
    suspended: boolean,
  ): MockMemberChange {
    const member = this.findMembership(workspaceId, userId);
    if (!member) return "not-found";
    if (
      suspended &&
      member.role === "owner" &&
      !this.suspendedMembershipIds.has(member.id) &&
      this.activeOwnerCount(workspaceId) <= 1
    ) {
      return "last-owner";
    }
    if (suspended) this.suspendedMembershipIds.add(member.id);
    else this.suspendedMembershipIds.delete(member.id);
    return "ok";
  }

  getInvitations(workspaceId: string): MockInvitationRecord[] {
    return Array.from(this.invitationsById.values())
      .filter((invitation) => invitation.workspaceId === workspaceId)
      .sort((a, b) => b.createdAt.localeCompare(a.createdAt));
  }

  /** Returns the new id, or `null` when a pending invitation already exists. */
  createInvitation(
    workspaceId: string,
    email: string,
    role: MockInvitationRecord["role"],
  ): string | null {
    const clean = email.trim().toLowerCase();
    if (
      this.getInvitations(workspaceId).some(
        (invitation) =>
          invitation.email === clean && invitation.status === "pending",
      )
    ) {
      return null;
    }
    const id = this.nextId("invitation");
    const days = this.getWorkspaceSettings(workspaceId).invitationExpiryDays;
    this.navigationTick += 1;
    this.invitationsById.set(id, {
      id,
      workspaceId,
      email: clean,
      role,
      createdAt: this.clock.offsetSeconds(this.navigationTick),
      expiresAt: this.clock.offsetDays(days),
      status: "pending",
    });
    return id;
  }

  /** Changes the role a waiting invitation offers; `null` when it is not found. */
  changeInvitationRole(
    workspaceId: string,
    invitationId: string,
    role: MockInvitationRecord["role"],
  ): boolean | null {
    const invitation = this.invitationsById.get(invitationId);
    if (!invitation || invitation.workspaceId !== workspaceId) return null;
    if (invitation.status !== "pending") return false;
    invitation.role = role;
    return true;
  }

  /**
   * A resend re-issues the link: the invitation keeps its id and waits seven
   * more days. Only an invitation that waits or has run out can be sent again.
   */
  resendInvitation(workspaceId: string, invitationId: string): boolean | null {
    const invitation = this.invitationsById.get(invitationId);
    if (!invitation || invitation.workspaceId !== workspaceId) return null;
    if (invitation.status !== "pending") return false;
    invitation.expiresAt = this.clock.offsetDays(7);
    return true;
  }

  revokeInvitation(workspaceId: string, invitationId: string): boolean {
    const invitation = this.invitationsById.get(invitationId);
    if (!invitation || invitation.workspaceId !== workspaceId) return false;
    if (invitation.status === "pending") invitation.status = "revoked";
    return true;
  }

  getWorkspaceSettings(workspaceId: string): MockWorkspaceSettingsRecord {
    let settings = this.settingsByWorkspaceId.get(workspaceId);
    if (!settings) {
      settings = {
        allowPublicSharing: false,
        enforceMfa: false,
        allowGuestInvites: true,
        defaultMemberRole: "Member",
        invitationExpiryDays: 7,
        timeZone: "Asia/Ho_Chi_Minh",
        weekStart: "monday",
      };
      this.settingsByWorkspaceId.set(workspaceId, settings);
    }
    return settings;
  }

  // ─── Versioned workspace writes (WX-SET-001) ─────────────────────────────

  getWorkspaceVersion(workspaceId: string): number {
    return this.workspaceVersionById.get(workspaceId) ?? 1;
  }

  getWorkspaceDescription(workspaceId: string): string | null {
    return this.workspaceDescriptionById.get(workspaceId) ?? null;
  }

  isWorkspaceArchived(workspaceId: string): boolean {
    return this.archivedWorkspaceIds.has(workspaceId);
  }

  /** Runs `apply` only when `expectedVersion` is current, then advances it. */
  private versionedWorkspaceWrite(
    workspaceId: string,
    expectedVersion: unknown,
    apply: () => void,
  ): MockVersionedWrite {
    if (
      !this.getWorkspace(workspaceId) ||
      this.isWorkspaceArchived(workspaceId)
    )
      return "not-found";
    if (expectedVersion !== this.getWorkspaceVersion(workspaceId))
      return "stale";
    apply();
    this.workspaceVersionById.set(
      workspaceId,
      this.getWorkspaceVersion(workspaceId) + 1,
    );
    return "ok";
  }

  updateWorkspaceProfile(
    workspaceId: string,
    expectedVersion: unknown,
    profile: { name?: string | null; description?: string | null },
  ): MockVersionedWrite {
    return this.versionedWorkspaceWrite(workspaceId, expectedVersion, () => {
      const workspace = this.getWorkspace(workspaceId)!;
      if (typeof profile.name === "string") workspace.name = profile.name;
      if (typeof profile.description === "string")
        this.workspaceDescriptionById.set(workspaceId, profile.description);
    });
  }

  updateWorkspaceSettings(
    workspaceId: string,
    expectedVersion: unknown,
    settings: MockWorkspaceSettingsRecord,
  ): MockVersionedWrite {
    return this.versionedWorkspaceWrite(workspaceId, expectedVersion, () => {
      this.settingsByWorkspaceId.set(workspaceId, { ...settings });
    });
  }

  archiveWorkspace(
    workspaceId: string,
    expectedVersion: unknown,
  ): MockVersionedWrite {
    return this.versionedWorkspaceWrite(workspaceId, expectedVersion, () => {
      this.archivedWorkspaceIds.add(workspaceId);
    });
  }

  // ─── My work (WX-BE-005) ─────────────────────────────────────────────────

  /**
   * Open Items "assigned" to the current member. The mock dataset's
   * assignees are the board people, so the member's work is a curated set of
   * the real Items of the 2026-10 mockups (the ERP Board's and the
   * Accounting and Marketing Boards'): each name, Group and dates come from
   * the Item itself, so My work and the Board agree. Ordered like the
   * producer: due date ascending, undated last, then id.
   */
  getMyWorkItems(workspaceId: string) {
    const wanted = new Set(this.myWorkItemIds());
    const boardIds = new Set(
      this.getBoards(workspaceId)
        .filter((board) => !board.isArchived)
        .map((board) => board.id),
    );
    const items: {
      id: string;
      boardId: string;
      groupId: string;
      name: string;
      dueAt: string | null;
      startedAt: string | null;
      updatedAt: string;
      version: number;
    }[] = [];
    for (const id of wanted) {
      const card = this.cardsById.get(id);
      if (!card || !boardIds.has(card.boardId)) continue;
      if (this.completedCardIds.has(id) || card.completedAt || card.isArchived)
        continue;
      items.push({
        id: card.id,
        boardId: card.boardId,
        groupId: card.listId,
        name: card.title,
        dueAt: card.dueAt ?? null,
        startedAt: card.startedAt ?? null,
        updatedAt: card.updatedAt,
        version: card.version ?? 1,
      });
    }
    return items.sort((a, b) => {
      if (a.dueAt === b.dueAt) return a.id.localeCompare(b.id);
      if (a.dueAt === null) return 1;
      if (b.dueAt === null) return -1;
      return a.dueAt.localeCompare(b.dueAt) || a.id.localeCompare(b.id);
    });
  }

  /** The Items the 2026-10 mockups list as the member's work. */
  private myWorkItemIds(): string[] {
    return this.feed.relatedIds();
  }

  completeCard(cardId: string): boolean {
    const card = this.cardsById.get(cardId);
    if (!card) return false;
    this.completedCardIds.add(cardId);
    const statusColumn = this.getColumns(card.boardId).find(
      (column) => column.fieldType === "status",
    );
    const done = statusColumn?.options?.find(
      (option) => option.category === "done",
    );
    const before = this.childProgressOf(card.parentItemId);
    if (done) this.updateItemStatus(cardId, done.id);
    else this.updateCard(cardId, { completedAt: this.clock.isoNow() });
    this.recordChildTick(cardId, before, "đánh dấu xong việc con");
    return true;
  }

  /** Done and total child items of a parent, as a percent of done. */
  private childProgressOf(parentId: string | null | undefined): number | null {
    if (!parentId) return null;
    const children = this.getChildCards(parentId);
    if (children.length === 0) return null;
    const done = children.filter((child) => this.isCardDone(child)).length;
    return Math.round((done / children.length) * 100);
  }

  private isCardDone(card: MockCardRecord): boolean {
    const statusColumn = this.getColumns(card.boardId).find(
      (column) => column.fieldType === "status",
    );
    const value = this.getFieldValues(card.id)[statusColumn?.id ?? ""];
    const option = statusColumn?.options?.find(
      (candidate) => candidate.id === value,
    );
    return option?.category === "done" || Boolean(card.completedAt);
  }

  /** Records on the parent that a child item was ticked or reopened. */
  private recordChildTick(
    childId: string,
    before: number | null,
    verb: string,
  ): void {
    const child = this.cardsById.get(childId);
    if (!child?.parentItemId || before === null) return;
    const after = this.childProgressOf(child.parentItemId);
    if (after === null || after === before) return;
    this.recordActivity(
      child.parentItemId,
      this.getCurrentUser().id,
      `${verb} ${child.itemKey ?? child.title}`,
      undefined,
      {
        verb,
        target: child.itemKey ?? child.title,
        detail: {
          kind: "diff",
          label: "Tiến độ",
          from: `${before}%`,
          to: `${after}%`,
          note: "tự tính",
        },
        compact: {
          verb: verb === "mở lại việc con" ? "mở lại" : "đánh dấu xong",
          target: child.title,
          hideDetail: true,
        },
      },
    );
  }

  /** Adds a child item (a sub-item) under an item; it starts open and unassigned. */
  createChildItem(parentId: string, title: string): MockCardRecord | undefined {
    const parent = this.cardsById.get(parentId);
    if (!parent || parent.parentItemId) return undefined;
    const siblings = this.getChildCards(parentId);
    const before = this.childProgressOf(parentId);
    const key = parent.itemKey
      ? `${parent.itemKey}.${siblings.length + 1}`
      : null;
    const statusColumn = this.getColumns(parent.boardId).find(
      (column) => column.fieldType === "status",
    );
    const todo = statusColumn?.options?.find(
      (option) => option.category === "todo",
    );
    const card: MockCardRecord = {
      id: this.nextId("card"),
      boardId: parent.boardId,
      listId: parent.listId,
      title,
      position: siblings.length + 1,
      createdAt: this.clock.isoNow(),
      updatedAt: this.clock.isoNow(),
      itemKey: key,
      parentItemId: parentId,
      dueAt: null,
      startedAt: null,
      completedAt: null,
      assigneeUserIds: [],
      childProgress: null,
    };
    this.insertCard(card);
    if (statusColumn && todo)
      this.updateFieldValue(card.id, statusColumn.id, todo.id);
    this.recordActivity(
      parentId,
      this.getCurrentUser().id,
      `thêm việc con ${key ?? title}`,
      undefined,
      {
        verb: "thêm việc con",
        target: key ?? title,
        ...(before === null
          ? {}
          : {
              detail: {
                kind: "diff" as const,
                label: "Tiến độ",
                from: `${before}%`,
                to: `${this.childProgressOf(parentId) ?? 0}%`,
                note: "tự tính",
              },
            }),
      },
    );
    return card;
  }

  /** Reopens a completed item: it is open again, as it was before. */
  reopenCard(cardId: string): boolean {
    const card = this.cardsById.get(cardId);
    if (!card) return false;
    const before = this.childProgressOf(card.parentItemId);
    this.completedCardIds.delete(cardId);
    this.updateCard(cardId, { completedAt: null });
    this.recordChildTick(cardId, before, "mở lại việc con");
    return true;
  }

  // ── Links, files and followers of an item ──

  /** The links of an item as the Detail page groups them. */
  getItemLinks(cardId: string): {
    id: string;
    relation: "blocked-by" | "blocks" | "related";
    card: MockCardRecord;
  }[] {
    const out: {
      id: string;
      relation: "blocked-by" | "blocks" | "related";
      card: MockCardRecord;
    }[] = [];
    for (const link of this.itemLinks) {
      if (link.from !== cardId && link.to !== cardId) continue;
      const otherId = link.from === cardId ? link.to : link.from;
      const other = this.cardsById.get(otherId);
      if (!other || other.isArchived) continue;
      out.push({
        id: link.id,
        relation:
          link.kind === "related"
            ? "related"
            : link.to === cardId
              ? "blocked-by"
              : "blocks",
        card: other,
      });
    }
    return out;
  }

  /** Links two items of one board; `from` is what `to` waits for. */
  addItemLink(
    cardId: string,
    otherId: string,
    relation: "blocked-by" | "blocks" | "related",
  ): "ok" | "not-found" | "invalid" {
    const card = this.cardsById.get(cardId);
    const other = this.cardsById.get(otherId);
    if (!card || !other) return "not-found";
    if (card.id === other.id || card.boardId !== other.boardId)
      return "invalid";
    const from = relation === "blocked-by" ? other.id : card.id;
    const to = relation === "blocked-by" ? card.id : other.id;
    const kind = relation === "related" ? "related" : "dependency";
    const exists = this.itemLinks.some(
      (link) =>
        link.kind === kind &&
        ((link.from === from && link.to === to) ||
          (kind === "related" && link.from === to && link.to === from)),
    );
    if (exists) return "invalid";
    this.itemLinks.push({
      id: this.nextId("link"),
      boardId: card.boardId,
      from,
      to,
      kind,
    });
    const word =
      relation === "blocked-by"
        ? "Đang chờ"
        : relation === "blocks"
          ? "Đang chặn"
          : "Liên quan";
    this.recordActivity(
      cardId,
      this.getCurrentUser().id,
      `thêm liên kết ${word} ${other.itemKey ?? other.title}`,
      undefined,
      {
        verb: "thêm liên kết",
        detail: {
          kind: "link",
          relation: word,
          key: other.itemKey ?? "",
          title: other.title,
        },
      },
    );
    return "ok";
  }

  removeItemLink(linkId: string): boolean {
    const before = this.itemLinks.length;
    this.itemLinks = this.itemLinks.filter((link) => link.id !== linkId);
    return this.itemLinks.length < before;
  }

  getAttachments(cardId: string): MockAttachmentRecord[] {
    return [...(this.attachmentsByCardId.get(cardId) ?? [])];
  }

  getUploadByKey(
    cardId: string,
    key: string,
  ): MockAttachmentRecord | undefined {
    return this.uploadsByIdempotencyKey.get(`${cardId}:${key}`);
  }

  rememberUpload(
    cardId: string,
    key: string,
    record: MockAttachmentRecord,
  ): void {
    this.uploadsByIdempotencyKey.set(`${cardId}:${key}`, record);
  }

  /** The files of a page, newest first (like the producer's list). */
  getPageAttachments(pageId: string): MockPageAttachmentRecord[] {
    return [...(this.attachmentsByPageId.get(pageId) ?? [])].reverse();
  }

  getPageAttachment(
    pageId: string,
    attachmentId: string,
  ): MockPageAttachmentRecord | undefined {
    return this.attachmentsByPageId
      .get(pageId)
      ?.find((file) => file.id === attachmentId);
  }

  getPageUploadByKey(
    pageId: string,
    key: string,
  ): MockPageAttachmentRecord | undefined {
    return this.pageUploadsByKey.get(`${pageId}:${key}`);
  }

  addPageAttachment(
    pageId: string,
    file: {
      filename: string;
      contentType: string;
      bytes: Uint8Array;
      idempotencyKey: string;
    },
  ): MockPageAttachmentRecord | undefined {
    if (!this.pagesById.has(pageId)) return undefined;
    const record: MockPageAttachmentRecord = {
      id: this.nextId("file"),
      pageId,
      filename: file.filename,
      sizeBytes: file.bytes.byteLength,
      contentType: file.contentType,
      uploadedBy: this.getCurrentUser().id,
      createdAt: this.clock.isoNow(),
      bytes: file.bytes,
    };
    const list = this.attachmentsByPageId.get(pageId) ?? [];
    list.push(record);
    this.attachmentsByPageId.set(pageId, list);
    this.pageUploadsByKey.set(`${pageId}:${file.idempotencyKey}`, record);
    return record;
  }

  removePageAttachment(pageId: string, attachmentId: string): boolean {
    const list = this.attachmentsByPageId.get(pageId) ?? [];
    const next = list.filter((file) => file.id !== attachmentId);
    if (next.length === list.length) return false;
    this.attachmentsByPageId.set(pageId, next);
    return true;
  }

  getAttachment(
    cardId: string,
    attachmentId: string,
  ): MockAttachmentRecord | undefined {
    return this.attachmentsByCardId
      .get(cardId)
      ?.find((file) => file.id === attachmentId);
  }

  addAttachment(
    cardId: string,
    file: { filename: string; sizeBytes: number; contentType: string },
  ): MockAttachmentRecord | undefined {
    if (!this.cardsById.has(cardId)) return undefined;
    const record: MockAttachmentRecord = {
      id: this.nextId("file"),
      cardId,
      filename: file.filename,
      sizeBytes: file.sizeBytes,
      contentType: file.contentType,
      uploadedBy: this.getCurrentUser().id,
      createdAt: this.clock.isoNow(),
    };
    const list = this.attachmentsByCardId.get(cardId) ?? [];
    list.push(record);
    this.attachmentsByCardId.set(cardId, list);
    this.recordActivity(
      cardId,
      this.getCurrentUser().id,
      `thêm tệp ${file.filename}`,
      undefined,
      {
        verb: "thêm tệp",
        detail: { kind: "file", name: file.filename },
      },
    );
    return record;
  }

  /** People who follow an item besides the member. */
  getFollowerIds(cardId: string): readonly string[] {
    return this.followersByCardId.get(cardId) ?? [];
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

  /** Settings write: changes the general facts of a board and bumps its version. */
  patchBoard(
    boardId: string,
    patch: Partial<
      Pick<
        MockBoardRecord,
        "title" | "description" | "background" | "itemKeyPrefix" | "isArchived"
      >
    >,
  ): boolean {
    const board = this.boardsById.get(boardId);
    if (!board) return false;
    this.boardsById.set(boardId, {
      ...board,
      ...patch,
      updatedAt: this.clock.isoNow(),
      version: (board.version ?? 1) + 1,
    });
    return true;
  }

  /** Settings write: removes a board with its lists, items and fields. */
  deleteBoard(boardId: string): boolean {
    const board = this.boardsById.get(boardId);
    if (!board) return false;
    for (const list of this.getLists(boardId)) this.deleteList(list.id);
    for (const column of this.getColumns(boardId)) this.deleteColumn(column.id);
    this.removeIndex(this.boardIdsByWorkspaceId, board.workspaceId, boardId);
    this.boardsById.delete(boardId);
    return true;
  }

  /** Settings write: replaces the options of a select field (keeps order given). */
  setColumnOptions(
    columnId: string,
    options: MockFieldOptionRecord[],
  ): boolean {
    const col = this.columnsById.get(columnId);
    if (!col) return false;
    this.columnsById.set(columnId, {
      ...col,
      options,
      version: (col.version ?? 1) + 1,
    });
    return true;
  }

  getLists(boardId: string): MockListRecord[] {
    const ids = this.listIdsByBoardId.get(boardId);
    if (!ids) return [];
    return Array.from(ids)
      .map((id) => this.listsById.get(id))
      .filter((l): l is MockListRecord => l !== undefined)
      .sort((a, b) => a.position - b.position);
  }

  /** Settings write: places a group among its siblings. */
  setListPosition(listId: string, position: number): boolean {
    const list = this.listsById.get(listId);
    if (!list) return false;
    this.listsById.set(listId, { ...list, position });
    return true;
  }

  getList(id: string): MockListRecord | undefined {
    return this.listsById.get(id);
  }

  getCards(listId: string): MockCardRecord[] {
    const ids = this.cardIdsByListId.get(listId);
    if (!ids) return [];
    return Array.from(ids)
      .map((id) => this.cardsById.get(id))
      .filter((c): c is MockCardRecord => c !== undefined && !c.unlisted)
      .sort((a, b) => a.position - b.position);
  }

  getCard(id: string): MockCardRecord | undefined {
    return this.cardsById.get(id);
  }

  /** Child items of an item (never top-level cards of a group). */
  getChildCards(parentItemId: string): MockCardRecord[] {
    return Array.from(this.cardsById.values())
      .filter((card) => card.parentItemId === parentItemId)
      .sort((a, b) => a.position - b.position || a.id.localeCompare(b.id));
  }

  /** Next item key of a board with a key prefix ("ERP-154"); else none. */
  private nextItemKey(boardId: string): string | null {
    const prefix = this.boardsById.get(boardId)?.itemKeyPrefix;
    if (!prefix) return null;
    let highest = 0;
    for (const card of this.cardsById.values()) {
      // The States mockup items are numbered apart from the board's own run.
      if (card.boardId !== boardId || !card.itemKey || card.unlisted) continue;
      const number = Number(
        card.itemKey.slice(card.itemKey.lastIndexOf("-") + 1),
      );
      if (Number.isFinite(number)) highest = Math.max(highest, number);
    }
    return `${prefix}-${highest + 1}`;
  }

  getNotifications(userId: string): MockNotificationRecord[] {
    const ids = this.notificationIdsByUserId.get(userId);
    if (!ids) return [];
    return Array.from(ids)
      .map((id) => this.notificationsById.get(id))
      .filter((n): n is MockNotificationRecord => n !== undefined)
      .sort((a, b) => b.createdAt.localeCompare(a.createdAt));
  }

  /** A notification a background job leaves in a person's inbox. */
  pushNotification(record: MockNotificationRecord): void {
    this.insertNotification(record);
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
    return this.pagesOf(workspaceId).filter((p) => !p.trashedAt);
  }

  /** The workspace's pages in the trash, newest edit first. */
  getTrashedPages(workspaceId: string): MockPageRecord[] {
    return this.pagesOf(workspaceId).filter((p) => Boolean(p.trashedAt));
  }

  private pagesOf(workspaceId: string): MockPageRecord[] {
    const ids = this.pageIdsByWorkspaceId.get(workspaceId);
    if (!ids) return [];
    return Array.from(ids)
      .map((id) => this.pagesById.get(id))
      .filter((p): p is MockPageRecord => p !== undefined)
      .sort((a, b) => b.updatedAt.localeCompare(a.updatedAt));
  }

  /** Moves a page to the trash (`Documents.Pages.ArchivePage`) or brings it back. */
  setPageTrashed(pageId: string, trashed: boolean): boolean {
    const page = this.pagesById.get(pageId);
    if (!page) return false;
    this.pagesById.set(pageId, {
      ...page,
      trashedAt: trashed ? this.clock.isoNow() : null,
      version: (page.version ?? 1) + 1,
    });
    return true;
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
    const id = data.id ?? this.nextId("block");
    const block: MockBlockRecord = {
      id,
      pageId,
      parentBlockId: data.parentBlockId ?? null,
      type: data.type || "paragraph",
      properties: data.properties || "{}",
      position: data.position ?? 0,
      version: 1,
      createdByUserId: mockIds.users.owner,
      createdAt: this.clock.isoNow(),
    };
    this.blocksById.set(id, block);
    return block;
  }

  updateBlock(
    id: string,
    data: Partial<MockBlockRecord>,
  ): MockBlockRecord | null {
    const block = this.blocksById.get(id);
    if (!block) return null;
    if (data.type !== undefined) block.type = data.type;
    if (data.properties !== undefined) block.properties = data.properties;
    if (data.parentBlockId !== undefined) {
      block.parentBlockId = data.parentBlockId;
    }
    if (data.position !== undefined) block.position = data.position;
    block.version += 1;
    block.updatedAt = this.clock.isoNow();
    return block;
  }

  deleteBlock(id: string): boolean {
    return this.blocksById.delete(id);
  }

  /** Puts a page's blocks back as they were (a refused batch leaves no trace). */
  restorePageBlocks(pageId: string, snapshot: readonly MockBlockRecord[]) {
    for (const block of this.getPageBlocks(pageId)) {
      this.blocksById.delete(block.id);
    }
    for (const block of snapshot) this.blocksById.set(block.id, { ...block });
  }

  getPageComments(pageId: string): MockPageCommentRecord[] {
    return Array.from(this.pageCommentsById.values())
      .filter((c) => c.pageId === pageId)
      .sort((a, b) => a.createdAt.localeCompare(b.createdAt));
  }

  createPageComment(
    pageId: string,
    userId: string,
    contentMd: string,
    createdAt?: string,
    anchor?: MockPageCommentRecord["anchor"],
  ): MockPageCommentRecord {
    const id = this.nextId("pcomment");
    const comment: MockPageCommentRecord = {
      id,
      pageId,
      userId,
      contentMd,
      createdAt: createdAt ?? this.clock.isoNow(),
      ...(anchor ? { anchor } : {}),
    };
    this.pageCommentsById.set(id, comment);
    this.feed.noteReplyOn({ docId: pageId });
    return comment;
  }

  updatePageComment(
    id: string,
    contentMd: string,
  ): MockPageCommentRecord | null {
    const comment = this.pageCommentsById.get(id);
    if (!comment) return null;
    comment.contentMd = contentMd;
    comment.updatedAt = this.clock.isoNow();
    return comment;
  }

  resolvePageComment(id: string): MockPageCommentRecord | null {
    const comment = this.pageCommentsById.get(id);
    if (!comment) return null;
    comment.resolvedAt ??= this.clock.isoNow();
    return comment;
  }

  /** A resolved thread becomes active again; reopening an active one changes nothing. */
  reopenPageComment(id: string): MockPageCommentRecord | null {
    const comment = this.pageCommentsById.get(id);
    if (!comment) return null;
    comment.resolvedAt = null;
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

  createPageHistory(
    pageId: string,
    userId: string,
    action: string,
    details?: string,
    createdAt?: string,
    versionName?: string,
  ): MockPageHistoryRecord {
    const id = this.nextId("phist");
    const history: MockPageHistoryRecord = {
      id,
      pageId,
      actorId: userId,
      action,
      resourceTitle: details,
      ...(versionName ? { versionName } : {}),
      createdAt: createdAt ?? this.clock.isoNow(),
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
      ...this.docsHistory.snapshot(),
      boardMembers: Array.from(this.boardMembersByBoardId.values()).flat(),
      boardGrants: Array.from(this.boardGrantsByBoardId.values()).flat(),
      boardAudiences: Array.from(this.boardAudienceByBoardId.values()),
      ...this.spaces.snapshot(),
    };
  }
}
