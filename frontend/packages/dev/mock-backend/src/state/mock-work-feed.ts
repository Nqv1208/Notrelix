/**
 * The member's work feed in the 2026-10 mockups' world: what the Home, My work
 * and Inbox pages draw beyond the generated contract (relationships to Items,
 * approvals, mentions, the day's schedule, the inbox with its detail pane).
 *
 * None of it has a producer in the generated contract. It is the shape the
 * frontend needs, served by the mock under the gap ids registered in
 * `enabled-consumer-surface.ts` (CTR-GAP-WS-*), and specified for the backend
 * team in `docs/superpowers/be-blockers-2026-10.md` ("BE requirement from FE").
 *
 * The state holds only what the Items do not already carry (relationships,
 * approvals, mentions, events, inbox records). Everything about an Item (name,
 * key, Status, Priority, due, assignee) is read from the Item itself, so the
 * Board, My work, Home and Inbox agree.
 */
import type { MockStore } from "./mock-store";
import { hanoiDayAt } from "./clock";
import {
  ERP_COMMENTER,
  ERP_PEOPLE,
  erpItemId,
  hanoiInstant,
} from "./mock-erp-board";
import { departmentItemId } from "./mock-department-boards";
import { INVOICE_DOC_ID } from "./mock-docs-invoice";
import { mockIds } from "./mock-ids";

// ─── Public shapes (mirrors of the frontend's DTO types) ─────────────────────

export type WorkBucket = "todo" | "doing" | "review" | "done";
export type WorkPriority = "urgent" | "high" | "medium" | "low";

export interface PersonDto {
  id: string;
  name: string;
}

export interface StatusDto {
  optionId: string;
  label: string;
  bucket: WorkBucket;
}

export type MyWorkRelation = "assigned" | "following" | "approver";

export interface MyWorkItemExtDto {
  id: string;
  boardId: string;
  groupId: string;
  key: string | null;
  name: string;
  dueAt: string | null;
  startedAt: string | null;
  updatedAt: string | null;
  version: number;
  status: StatusDto | null;
  priority: WorkPriority | null;
  relations: MyWorkRelation[];
  /** The person in charge when it is not the member; `null` = the member or nobody. */
  assignee: PersonDto | null;
  approval: { requestedBy: PersonDto; requestedAt: string } | null;
  completedAt: string | null;
}

export interface MyWorkWaitingDto {
  itemId: string;
  boardId: string;
  key: string | null;
  title: string;
  owner: PersonDto;
  dueAt: string | null;
  note: string;
  lastRemindedAt: string | null;
}

export interface MyWorkExtendedDto {
  items: MyWorkItemExtDto[];
  done: MyWorkItemExtDto[];
  waiting: MyWorkWaitingDto[];
  /** Board id → department (top-level Space) id, for the boards above. */
  departments: Record<string, string | null>;
}

export interface HomeReplyDto {
  id: string;
  kind: "approval" | "mention";
  actor: PersonDto;
  createdAt: string;
  text: string;
  target: {
    kind: "item" | "doc";
    id: string;
    boardId: string | null;
    key: string | null;
  };
}

export interface CalendarEventDto {
  id: string;
  title: string;
  startsAt: string;
  endsAt: string;
  place: string | null;
  attendeeCount: number | null;
}

export interface HomeFeedDto {
  replies: HomeReplyDto[];
  events: CalendarEventDto[];
  summary: { approvalsPending: number; mentionsUnread: number };
}

export type InboxType =
  | "approval_requested"
  | "mention"
  | "overdue"
  | "assigned"
  | "changes_bundle"
  | "board_invited"
  | "followed_status"
  | "followed_comment"
  | "followed_assignee";

export type InboxNeed = "approve" | "reply" | "overdue";

export interface InboxTargetDto {
  kind: "item" | "doc" | "board";
  id: string | null;
  key: string | null;
  title: string;
  boardId: string | null;
  boardName: string | null;
  /** Top-level Space of the target; `null` = workspace level. */
  departmentId: string | null;
  /** The person in charge (items). */
  owner: PersonDto | null;
}

export interface InboxBundleChildDto {
  id: string;
  createdAt: string;
  actor: PersonDto;
  kind: "status" | "comment" | "due";
  itemKey: string;
  itemTitle: string;
  /** "Chờ duyệt", "10/10 → 14/10" or the comment's first line. */
  detail: string;
}

export interface InboxEntryDto {
  id: string;
  createdAt: string;
  unread: boolean;
  type: InboxType;
  actor: PersonDto | null;
  need: InboxNeed | null;
  following: boolean;
  muted: boolean;
  /** The member marked it done: only the Done list holds it. */
  done: boolean;
  snoozedUntil: string | null;
  target: InboxTargetDto;
  excerpt: string | null;
  dueAt: string | null;
  permission: "view" | "comment" | "edit" | null;
  bundle: {
    actors: PersonDto[];
    moreActors: number;
    statusChanges: number;
    comments: number;
    dueChanges: number;
    children: InboxBundleChildDto[];
  } | null;
  detail: {
    approval: {
      soleApprover: boolean;
      summary: string;
      changes: { label: string; value: string; changed?: string }[];
    } | null;
    item: {
      status: StatusDto | null;
      priority: WorkPriority | null;
      dueAt: string | null;
      commentCount: number;
    } | null;
    latestComment: {
      author: PersonDto;
      createdAt: string;
      text: string;
    } | null;
  } | null;
}

export interface InboxDto {
  workspace: { id: string; name: string };
  items: InboxEntryDto[];
  counts: {
    need: number;
    mention: number;
    assigned: number;
    follow: number;
    all: number;
    unread: number;
  };
}

// ─── Records ─────────────────────────────────────────────────────────────────

interface ApprovalRecord {
  itemId: string;
  requestedById: string;
  requestedAt: string;
  /** What the requester wrote; Home draws it. */
  note: string;
  soleApprover: boolean;
  /** What is approved, as a phrase after "gửi bạn duyệt" ("cấu hình đăng nhập một lần"). */
  summary: string;
  changes: { label: string; value: string; changed?: string }[];
}

interface MentionRecord {
  id: string;
  actorId: string;
  createdAt: string;
  docId: string;
  text: string;
  excerpt: string;
  handled: boolean;
}

interface WaitingRecord {
  itemId: string;
  ownerId: string;
  note: string;
  lastRemindedAt: string | null;
}

interface EventRecord {
  id: string;
  title: string;
  startsAt: string;
  endsAt: string;
  place: string | null;
  attendeeCount: number | null;
}

interface InboxRecord {
  id: string;
  createdAt: string;
  unread: boolean;
  type: InboxType;
  actorId: string | null;
  need: InboxNeed | null;
  following: boolean;
  muted: boolean;
  itemId: string | null;
  docId: string | null;
  boardTitle: string | null;
  departmentId: string | null;
  excerpt: string | null;
  dueAt: string | null;
  permission: "view" | "comment" | "edit" | null;
  mentionId: string | null;
  snoozedUntil: string | null;
  done: boolean;
  bundle: {
    actorIds: string[];
    moreActors: number;
    children: {
      id: string;
      createdAt: string;
      actorId: string;
      kind: "status" | "comment" | "due";
      itemId: string;
      detail: string;
    }[];
  } | null;
}

const BUCKET_BY_CATEGORY: Record<string, WorkBucket> = {
  todo: "todo",
  in_progress: "doing",
  review: "review",
  done: "done",
};

const PRIORITY_BY_NAME: Record<string, WorkPriority> = {
  Khẩn: "urgent",
  Cao: "high",
  "Trung bình": "medium",
  Thấp: "low",
};

const DAY_MS = 24 * 60 * 60 * 1000;

/** Today (the mock clock's Hanoi day) at `hour:minute` Hanoi, as an instant. */
function todayAt(store: MockStore, hour: number, minute = 0, dayOffset = 0) {
  return hanoiDayAt(store.getClock(), hour, minute, dayOffset);
}

export class MockWorkFeedState {
  /** Items assigned to the member (their key is the relation set). */
  assigned = new Set<string>();
  following = new Set<string>();
  approvals = new Map<string, ApprovalRecord>();
  mentions: MentionRecord[] = [];
  waiting: WaitingRecord[] = [];
  events: EventRecord[] = [];
  inbox: InboxRecord[] = [];
  /** Items whose notifications the member turned off. */
  mutedItemIds = new Set<string>();

  clear(): void {
    this.assigned.clear();
    this.following.clear();
    this.approvals.clear();
    this.mentions = [];
    this.waiting = [];
    this.events = [];
    this.inbox = [];
    this.mutedItemIds.clear();
  }

  /** Every item the member's work lists hold, open or done. */
  relatedIds(): string[] {
    return [
      ...new Set([
        ...this.assigned,
        ...this.following,
        ...this.approvals.keys(),
      ]),
    ];
  }

  // ── Seed ───────────────────────────────────────────────────────────────

  seed(store: MockStore): void {
    this.clear();
    const erp = (key: string) => erpItemId(key);
    const dept = (key: string) => departmentItemId(key);
    const exists = (id: string) => store.getCard(id) !== undefined;
    const ifExists = (id: string) => (exists(id) ? [id] : []);

    for (const id of [
      ...ifExists(dept("KT-88")),
      ...ifExists(dept("KT-95")),
      ...ifExists(dept("KT-86")),
      ...ifExists(dept("MK-21")),
      ...ifExists(erp("ERP-147")),
      ...ifExists(erp("ERP-136")),
    ]) {
      this.assigned.add(id);
    }
    for (const id of [
      ...ifExists(erp("ERP-142")),
      ...ifExists(erp("ERP-144")),
    ]) {
      this.following.add(id);
    }

    const sso = erp("ERP-138");
    if (exists(sso)) {
      this.approvals.set(sso, {
        itemId: sso,
        requestedById: ERP_PEOPLE.MQ.id,
        requestedAt: todayAt(store, 10, 28),
        note: "Gửi duyệt việc thiết lập đăng nhập một lần (SSO) cho toàn công ty (ERP-138). Kế toán cần đăng nhập thử từ thứ Năm.",
        soleApprover: true,
        summary: "cấu hình đăng nhập một lần",
        changes: [
          {
            label: "Nhà cung cấp",
            value: "Microsoft Entra ID",
            changed: "Microsoft Entra ID",
          },
          {
            label: "Áp dụng cho",
            value: "Tất cả phòng ban, trừ khách bên ngoài",
            changed: "Tất cả phòng ban",
          },
          {
            label: "Bắt buộc từ",
            value: "Thứ Năm 15/10",
            changed: "Thứ Năm 15/10",
          },
          { label: "Đăng nhập bằng mật khẩu", value: "Tắt sau 30 ngày" },
        ],
      });
    }

    const hung = erp("ERP-144");
    if (exists(hung)) {
      this.waiting.push({
        itemId: hung,
        ownerId: ERP_PEOPLE.HP.id,
        note: "Chờ Phúc gửi kết quả",
        lastRemindedAt: null,
      });
    }

    this.events = [
      {
        id: "mock-event-standup",
        title: "Họp đứng nhóm ERP",
        startsAt: todayAt(store, 9, 0),
        endsAt: todayAt(store, 9, 15),
        place: "Phòng họp 3",
        attendeeCount: null,
      },
      {
        id: "mock-event-uat",
        title: "Demo UAT phân hệ bán hàng",
        startsAt: todayAt(store, 14, 0),
        endsAt: todayAt(store, 15, 0),
        place: "trực tuyến",
        attendeeCount: 6,
      },
      {
        id: "mock-event-1on1",
        title: "Họp 1-1 với Phan Ngọc Anh",
        startsAt: todayAt(store, 16, 30),
        endsAt: todayAt(store, 17, 0),
        place: null,
        attendeeCount: null,
      },
    ];

    const mentionId = "mock-mention-invoice";
    if (store.getPage(INVOICE_DOC_ID)) {
      this.mentions.push({
        id: mentionId,
        actorId: ERP_PEOPLE.LT.id,
        createdAt: todayAt(store, 9, 40),
        docId: INVOICE_DOC_ID,
        text: "@Hà chị xem giúp đề xuất 3 ngày đối chiếu cuối tháng trong quy trình nhập hoá đơn.",
        excerpt: "chị xem giúp đề xuất 3 ngày đối chiếu…",
        handled: false,
      });
    }

    const accountingId = mockIds.spaces.accounting;
    const hrId = mockIds.spaces.hr;
    const techId = mockIds.spaces.engineering;
    const rec = (
      value: Partial<InboxRecord> &
        Pick<InboxRecord, "id" | "createdAt" | "type">,
    ): InboxRecord => ({
      unread: false,
      actorId: null,
      need: null,
      following: false,
      muted: false,
      itemId: null,
      docId: null,
      boardTitle: null,
      departmentId: null,
      excerpt: null,
      dueAt: null,
      permission: null,
      mentionId: null,
      snoozedUntil: null,
      done: false,
      bundle: null,
      ...value,
    });
    const inbox: InboxRecord[] = [];
    if (exists(sso)) {
      inbox.push(
        rec({
          id: "mock-inbox-sso",
          createdAt: todayAt(store, 10, 28),
          type: "approval_requested",
          unread: true,
          actorId: ERP_PEOPLE.MQ.id,
          need: "approve",
          itemId: sso,
          departmentId: techId,
        }),
      );
    }
    if (this.mentions.length > 0) {
      inbox.push(
        rec({
          id: "mock-inbox-mention",
          createdAt: todayAt(store, 9, 40),
          type: "mention",
          unread: true,
          actorId: ERP_PEOPLE.LT.id,
          need: "reply",
          docId: INVOICE_DOC_ID,
          departmentId: accountingId,
          excerpt: this.mentions[0]!.excerpt,
          mentionId,
        }),
      );
    }
    if (exists(erp("ERP-142"))) {
      inbox.push(
        rec({
          id: "mock-inbox-overdue",
          createdAt: todayAt(store, 8, 0),
          type: "overdue",
          unread: true,
          need: "overdue",
          following: true,
          itemId: erp("ERP-142"),
          departmentId: techId,
          dueAt: hanoiInstant("10/10"),
        }),
      );
    }
    if (exists(dept("KT-88"))) {
      inbox.push(
        rec({
          id: "mock-inbox-assigned",
          createdAt: todayAt(store, 7, 45),
          type: "assigned",
          actorId: ERP_COMMENTER.id,
          itemId: dept("KT-88"),
          departmentId: accountingId,
          dueAt: hanoiInstant("12/10", 17),
        }),
      );
    }
    const children: NonNullable<InboxRecord["bundle"]>["children"] = [];
    const childSeed: [
      string,
      "status" | "comment" | "due",
      string,
      string,
      number,
      number,
    ][] = [
      ["ERP-146", "status", ERP_PEOPLE.HP.id, "Đang làm", 16, 52],
      ["ERP-144", "status", ERP_PEOPLE.HP.id, "Đang làm", 16, 20],
      ["ERP-147", "status", ERP_PEOPLE.VT.id, "Cần làm", 15, 47],
      [
        "ERP-146",
        "comment",
        ERP_PEOPLE.VT.id,
        "Đã nạp xong số tồn kho Bình Dương.",
        15,
        12,
      ],
      [
        "ERP-151",
        "comment",
        ERP_PEOPLE.NA.id,
        "Đã chốt ma trận quyền theo phòng ban.",
        14,
        31,
      ],
      ["ERP-140", "due", ERP_PEOPLE.TT.id, "14/10 → 15/10", 11, 5],
    ];
    childSeed.forEach(([key, kind, actorId, detail, hour, minute], index) => {
      if (!exists(erp(key))) return;
      children.push({
        id: `mock-inbox-change-${index + 1}`,
        createdAt: todayAt(store, hour, minute, -1),
        actorId,
        kind,
        itemId: erp(key),
        detail,
      });
    });
    if (children.length > 0) {
      inbox.push(
        rec({
          id: "mock-inbox-bundle",
          createdAt: todayAt(store, 17, 10, -1),
          type: "changes_bundle",
          boardTitle: "Triển khai ERP Q4",
          departmentId: techId,
          bundle: {
            actorIds: [ERP_PEOPLE.HP.id, ERP_PEOPLE.VT.id],
            moreActors: 2,
            children,
          },
        }),
      );
    }
    inbox.push(
      rec({
        id: "mock-inbox-invite",
        createdAt: todayAt(store, 10, 20, -1),
        type: "board_invited",
        actorId: ERP_PEOPLE.PH.id,
        boardTitle: "Tuyển dụng Q4",
        departmentId: hrId,
        permission: "view",
      }),
    );
    this.inbox = inbox;

    // KT-86 was completed at 10:05 today (the Done tab of the mockup).
    if (exists(dept("KT-86")))
      store.updateCard(dept("KT-86"), { completedAt: todayAt(store, 10, 5) });

    // Final-Home `xong-viec` (VITE_MOCK_STATE=done-today): today's two items
    // were completed at 10:05, and the earlier completion is not in the world.
    if (store.getConfig().state === "done-today") {
      this.assigned.delete(dept("KT-86"));
      for (const id of [dept("KT-88"), erp("ERP-144")]) {
        if (!exists(id)) continue;
        store.completeCard(id);
        store.updateCard(id, { completedAt: todayAt(store, 10, 5) });
      }
    }

    // "Tiếp tục" on Home: the board opened 20 minutes ago, the doc yesterday.
    const workspaceId = store.getBoard(mockIds.boards.erp)?.workspaceId;
    if (workspaceId) {
      store.seedRecentResource(
        workspaceId,
        "work-management.board",
        mockIds.boards.erp,
        store.getClock().offsetMinutes(-20),
      );
      if (store.getPage(INVOICE_DOC_ID)) {
        store.seedRecentResource(
          workspaceId,
          "documents.page",
          INVOICE_DOC_ID,
          store.getClock().offsetDays(-1),
        );
      }
    }
  }
}

// ─── Reads ───────────────────────────────────────────────────────────────────

function person(
  store: MockStore,
  id: string | null | undefined,
): PersonDto | null {
  if (!id) return null;
  const user = store.getUser(id);
  return user ? { id: user.id, name: user.name } : null;
}

/** The top-level Space (department) of a Space id. */
function departmentOf(store: MockStore, spaceId: string | null | undefined) {
  let current = spaceId ? store.spaces.getSpace(spaceId) : undefined;
  const seen = new Set<string>();
  while (current?.parentId && !seen.has(current.id)) {
    seen.add(current.id);
    current = store.spaces.getSpace(current.parentId);
  }
  return current?.id ?? null;
}

function statusOf(store: MockStore, cardId: string): StatusDto | null {
  const card = store.getCard(cardId);
  if (!card) return null;
  const column = store
    .getColumns(card.boardId)
    .find((candidate) => candidate.fieldType === "status");
  if (!column) return null;
  const optionId = store.getFieldValues(cardId)[column.id];
  const option = column.options?.find((candidate) => candidate.id === optionId);
  if (!option) return null;
  return {
    optionId: option.id,
    label: option.name,
    bucket: BUCKET_BY_CATEGORY[option.category ?? "todo"] ?? "todo",
  };
}

function priorityOf(store: MockStore, cardId: string): WorkPriority | null {
  const card = store.getCard(cardId);
  if (!card) return null;
  const column = store
    .getColumns(card.boardId)
    .find((candidate) => candidate.name === "Ưu tiên");
  if (!column) return null;
  const optionId = store.getFieldValues(cardId)[column.id];
  const option = column.options?.find((candidate) => candidate.id === optionId);
  return option ? (PRIORITY_BY_NAME[option.name] ?? null) : null;
}

function itemDto(
  store: MockStore,
  feed: MockWorkFeedState,
  cardId: string,
): MyWorkItemExtDto | null {
  const card = store.getCard(cardId);
  if (!card || card.isArchived) return null;
  const me = store.getCurrentUser().id;
  const relations: MyWorkRelation[] = [];
  if (feed.assigned.has(cardId)) relations.push("assigned");
  if (feed.following.has(cardId)) relations.push("following");
  const approval = feed.approvals.get(cardId);
  if (approval) relations.push("approver");
  const owners = (card.assigneeUserIds ?? []).filter((id) => id !== me);
  const assignee =
    feed.assigned.has(cardId) || approval || owners.length === 0
      ? null
      : person(store, owners[0]);
  const requester = approval ? person(store, approval.requestedById) : null;
  return {
    id: card.id,
    boardId: card.boardId,
    groupId: card.listId,
    key: card.itemKey ?? null,
    name: card.title,
    dueAt: card.dueAt ?? null,
    startedAt: card.startedAt ?? null,
    updatedAt: card.updatedAt,
    version: card.version ?? 1,
    status: statusOf(store, cardId),
    priority: priorityOf(store, cardId),
    relations,
    assignee,
    approval:
      approval && requester
        ? { requestedBy: requester, requestedAt: approval.requestedAt }
        : null,
    completedAt: card.completedAt ?? null,
  };
}

function dueOrder(a: MyWorkItemExtDto, b: MyWorkItemExtDto): number {
  if (a.dueAt === b.dueAt) return a.id.localeCompare(b.id);
  if (a.dueAt === null) return 1;
  if (b.dueAt === null) return -1;
  return a.dueAt.localeCompare(b.dueAt) || a.id.localeCompare(b.id);
}

export function buildMyWorkExtended(
  store: MockStore,
  workspaceId: string,
): MyWorkExtendedDto {
  const feed = store.feed;
  const boardIds = new Set(
    store
      .getBoards(workspaceId)
      .filter((board) => !board.isArchived)
      .map((board) => board.id),
  );
  const all = feed
    .relatedIds()
    .map((id) => itemDto(store, feed, id))
    .filter(
      (item): item is MyWorkItemExtDto =>
        item !== null && boardIds.has(item.boardId),
    );
  const open = all.filter((item) => item.completedAt === null).sort(dueOrder);
  const done = all
    .filter((item) => item.completedAt !== null)
    .sort((a, b) => (b.completedAt ?? "").localeCompare(a.completedAt ?? ""));
  const waiting: MyWorkWaitingDto[] = [];
  for (const entry of feed.waiting) {
    const card = store.getCard(entry.itemId);
    const owner = person(store, entry.ownerId);
    if (!card || !owner || card.completedAt) continue;
    waiting.push({
      itemId: card.id,
      boardId: card.boardId,
      key: card.itemKey ?? null,
      title: card.title,
      owner,
      dueAt: card.dueAt ?? null,
      note: entry.note,
      lastRemindedAt: entry.lastRemindedAt,
    });
  }
  const departments: Record<string, string | null> = {};
  for (const item of [...open, ...done]) {
    if (item.boardId in departments) continue;
    departments[item.boardId] = departmentOf(
      store,
      store.getBoard(item.boardId)?.spaceId,
    );
  }
  return { items: open, done, waiting, departments };
}

function targetOf(store: MockStore, record: InboxRecord): InboxTargetDto {
  if (record.itemId) {
    const card = store.getCard(record.itemId);
    const board = card ? store.getBoard(card.boardId) : undefined;
    const owners = (card?.assigneeUserIds ?? []).filter(
      (id) => id !== store.getCurrentUser().id,
    );
    return {
      kind: "item",
      id: record.itemId,
      key: card?.itemKey ?? null,
      title: card?.title ?? "",
      boardId: card?.boardId ?? null,
      boardName: board?.title ?? null,
      departmentId: departmentOf(store, board?.spaceId),
      owner: person(store, owners[0]),
    };
  }
  if (record.docId) {
    const page = store.getPage(record.docId);
    return {
      kind: "doc",
      id: record.docId,
      key: null,
      title: page?.title ?? "",
      boardId: null,
      boardName: null,
      departmentId: record.departmentId,
      owner: null,
    };
  }
  return {
    kind: "board",
    id: null,
    key: null,
    title: record.boardTitle ?? "",
    boardId: null,
    boardName: record.boardTitle,
    departmentId: record.departmentId,
    owner: null,
  };
}

function entryDto(
  store: MockStore,
  feed: MockWorkFeedState,
  record: InboxRecord,
): InboxEntryDto {
  const target = targetOf(store, record);
  const approval = record.itemId
    ? feed.approvals.get(record.itemId)
    : undefined;
  let detail: InboxEntryDto["detail"] = null;
  if (record.itemId) {
    const comments = store.getCardComments(record.itemId);
    const latest = comments[comments.length - 1];
    const author = latest ? person(store, latest.userId) : null;
    detail = {
      approval:
        record.type === "approval_requested" && approval
          ? {
              soleApprover: approval.soleApprover,
              summary: approval.summary,
              changes: approval.changes,
            }
          : null,
      item: {
        status: statusOf(store, record.itemId),
        priority: priorityOf(store, record.itemId),
        dueAt: store.getCard(record.itemId)?.dueAt ?? null,
        commentCount: comments.length,
      },
      latestComment:
        latest && author
          ? { author, createdAt: latest.createdAt, text: latest.contentMd }
          : null,
    };
  } else if (record.mentionId) {
    const mention = feed.mentions.find(
      (entry) => entry.id === record.mentionId,
    );
    const author = person(store, mention?.actorId);
    detail =
      mention && author
        ? {
            approval: null,
            item: null,
            latestComment: {
              author,
              createdAt: mention.createdAt,
              text: mention.text,
            },
          }
        : null;
  }
  const bundle = record.bundle
    ? (() => {
        const children = record.bundle.children
          .map((child): InboxBundleChildDto | null => {
            const actor = person(store, child.actorId);
            const card = store.getCard(child.itemId);
            if (!actor || !card) return null;
            return {
              id: child.id,
              createdAt: child.createdAt,
              actor,
              kind: child.kind,
              itemKey: card.itemKey ?? "",
              itemTitle: card.title,
              detail: child.detail,
            };
          })
          .filter((child): child is InboxBundleChildDto => child !== null);
        return {
          actors: record.bundle.actorIds
            .map((id) => person(store, id))
            .filter((entry): entry is PersonDto => entry !== null),
          moreActors: record.bundle.moreActors,
          statusChanges: children.filter((c) => c.kind === "status").length,
          comments: children.filter((c) => c.kind === "comment").length,
          dueChanges: children.filter((c) => c.kind === "due").length,
          children,
        };
      })()
    : null;
  return {
    id: record.id,
    createdAt: record.createdAt,
    unread: record.unread,
    type: record.type,
    actor: person(store, record.actorId),
    need: record.need,
    following:
      record.following ||
      (record.itemId ? feed.following.has(record.itemId) : false),
    muted: record.muted,
    done: record.done,
    snoozedUntil: record.snoozedUntil,
    target,
    excerpt: record.excerpt,
    dueAt: record.dueAt,
    permission: record.permission,
    bundle,
    detail,
  };
}

export type InboxListState = "inbox" | "done" | "snoozed";

export function buildInbox(
  store: MockStore,
  workspaceId: string,
  state: InboxListState = "inbox",
): InboxDto {
  const feed = store.feed;
  const now = store.getClock().isoNow();
  const snoozed = (record: InboxRecord) =>
    Boolean(record.snoozedUntil && record.snoozedUntil > now);
  const visible = feed.inbox
    .filter((record) =>
      state === "done"
        ? record.done
        : state === "snoozed"
          ? !record.done && snoozed(record)
          : !record.done && !snoozed(record),
    )
    .map((record) => entryDto(store, feed, record))
    .sort((a, b) => b.createdAt.localeCompare(a.createdAt));
  const count = (predicate: (entry: InboxEntryDto) => boolean) =>
    visible.filter(predicate).length;
  return {
    workspace: {
      id: workspaceId,
      name: store.getWorkspace(workspaceId)?.name ?? "",
    },
    items: visible,
    counts: {
      need: count((entry) => entry.need !== null),
      mention: count((entry) => entry.type === "mention"),
      assigned: count((entry) => entry.type === "assigned"),
      follow: count((entry) => entry.following),
      all: visible.length,
      unread: count((entry) => entry.unread),
    },
  };
}

export function buildHomeFeed(store: MockStore): HomeFeedDto {
  const feed = store.feed;
  const replies: HomeReplyDto[] = [];
  for (const approval of feed.approvals.values()) {
    const card = store.getCard(approval.itemId);
    const actor = person(store, approval.requestedById);
    if (!card || !actor || card.completedAt) continue;
    replies.push({
      id: `approval:${approval.itemId}`,
      kind: "approval",
      actor,
      createdAt: approval.requestedAt,
      text: approval.note,
      target: {
        kind: "item",
        id: card.id,
        boardId: card.boardId,
        key: card.itemKey ?? null,
      },
    });
  }
  for (const mention of feed.mentions) {
    const actor = person(store, mention.actorId);
    if (mention.handled || !actor) continue;
    replies.push({
      id: `mention:${mention.id}`,
      kind: "mention",
      actor,
      createdAt: mention.createdAt,
      text: mention.text,
      target: { kind: "doc", id: mention.docId, boardId: null, key: null },
    });
  }
  replies.sort((a, b) => b.createdAt.localeCompare(a.createdAt));
  const events = [...feed.events].sort((a, b) =>
    a.startsAt.localeCompare(b.startsAt),
  );
  return {
    replies,
    events,
    summary: {
      approvalsPending: replies.filter((r) => r.kind === "approval").length,
      mentionsUnread: replies.filter((r) => r.kind === "mention").length,
    },
  };
}

// ─── Writes ──────────────────────────────────────────────────────────────────

export type FeedWriteResult = "ok" | "not-found" | "invalid" | "forbidden";

function optionIdFor(
  store: MockStore,
  cardId: string,
  bucket: WorkBucket,
): string | null {
  const card = store.getCard(cardId);
  if (!card) return null;
  const column = store
    .getColumns(card.boardId)
    .find((candidate) => candidate.fieldType === "status");
  const option = column?.options?.find(
    (candidate) => BUCKET_BY_CATEGORY[candidate.category ?? "todo"] === bucket,
  );
  return option?.id ?? null;
}

export function setItemBucket(
  store: MockStore,
  cardId: string,
  bucket: WorkBucket,
): FeedWriteResult {
  const optionId = optionIdFor(store, cardId, bucket);
  if (!store.getCard(cardId)) return "not-found";
  if (!optionId) return "invalid";
  return store.updateItemStatus(cardId, optionId);
}

/** Approve or ask for changes on an item the member must approve. */
export function decideApproval(
  store: MockStore,
  cardId: string,
  decision: "approve" | "request-changes",
  comment: string | null,
): FeedWriteResult {
  const feed = store.feed;
  const approval = feed.approvals.get(cardId);
  if (!store.getCard(cardId)) return "not-found";
  if (!approval) return "invalid";
  const result = setItemBucket(
    store,
    cardId,
    decision === "approve" ? "done" : "doing",
  );
  if (result !== "ok") return result;
  feed.approvals.delete(cardId);
  if (comment && comment.trim()) {
    store.createCardComment(cardId, store.getCurrentUser().id, comment.trim());
  }
  for (const record of feed.inbox) {
    if (record.itemId === cardId && record.type === "approval_requested") {
      record.need = null;
      record.unread = false;
    }
  }
  return "ok";
}

export function reopenItem(store: MockStore, cardId: string): FeedWriteResult {
  const card = store.getCard(cardId);
  if (!card) return "not-found";
  // Reopening an item that is not completed changes nothing, as on the producer.
  if (!card.completedAt) return "ok";
  const result = setItemBucket(store, cardId, "doing");
  if (result !== "ok") return result;
  store.reopenCard(cardId);
  return "ok";
}

export function setFollowing(
  store: MockStore,
  cardId: string,
  following: boolean,
): FeedWriteResult {
  if (!store.getCard(cardId)) return "not-found";
  const feed = store.feed;
  if (following) feed.following.add(cardId);
  else {
    // Assigned items cannot be unfollowed: the member is on them anyway.
    if (feed.assigned.has(cardId)) return "invalid";
    feed.following.delete(cardId);
  }
  return "ok";
}

export function remindOwner(store: MockStore, cardId: string): FeedWriteResult {
  const entry = store.feed.waiting.find(
    (candidate) => candidate.itemId === cardId,
  );
  if (!entry) return "not-found";
  entry.lastRemindedAt = store.getClock().isoNow();
  return "ok";
}

export type BulkAction =
  | { type: "set-due"; dueAt: string | null }
  | { type: "set-status"; bucket: WorkBucket }
  | { type: "unfollow" }
  | { type: "assign"; assigneeId: string };

export interface BulkResult {
  itemId: string;
  ok: boolean;
  error: "not-found" | "invalid" | null;
}

export function bulkWrite(
  store: MockStore,
  itemIds: readonly string[],
  action: BulkAction,
): BulkResult[] {
  return itemIds.map((itemId): BulkResult => {
    const card = store.getCard(itemId);
    if (!card) return { itemId, ok: false, error: "not-found" };
    let outcome: FeedWriteResult = "ok";
    switch (action.type) {
      case "set-due":
        store.updateCard(itemId, { dueAt: action.dueAt });
        break;
      case "set-status":
        outcome = setItemBucket(store, itemId, action.bucket);
        break;
      case "unfollow":
        outcome = setFollowing(store, itemId, false);
        break;
      case "assign": {
        if (!store.getUser(action.assigneeId)) {
          outcome = "invalid";
          break;
        }
        const feed = store.feed;
        feed.assigned.delete(itemId);
        store.updateCard(itemId, { assigneeUserIds: [action.assigneeId] });
        if (action.assigneeId === store.getCurrentUser().id)
          feed.assigned.add(itemId);
        break;
      }
    }
    return outcome === "ok"
      ? { itemId, ok: true, error: null }
      : {
          itemId,
          ok: false,
          error: outcome === "not-found" ? "not-found" : "invalid",
        };
  });
}

/** Creates an item assigned to the member in the first group of a board. */
export function createAssignedItem(
  store: MockStore,
  boardId: string,
  name: string,
): { id: string; key: string | null } | "not-found" {
  const lists = store.getLists(boardId);
  const group = lists[0];
  if (!group) return "not-found";
  const card = store.createCard(boardId, group.id, { title: name });
  store.updateCard(card.id, {
    assigneeUserIds: [store.getCurrentUser().id],
  });
  const optionId = optionIdFor(store, card.id, "todo");
  if (optionId) store.updateItemStatus(card.id, optionId);
  store.feed.assigned.add(card.id);
  return { id: card.id, key: card.itemKey ?? null };
}

export function replyToMention(
  store: MockStore,
  mentionId: string,
  text: string,
): FeedWriteResult {
  const feed = store.feed;
  const mention = feed.mentions.find((entry) => entry.id === mentionId);
  if (!mention) return "not-found";
  const page = store.getPage(mention.docId);
  if (page) {
    store.createPageComment(page.id, store.getCurrentUser().id, text);
  }
  mention.handled = true;
  for (const record of feed.inbox) {
    if (record.mentionId === mentionId) {
      record.need = null;
      record.unread = false;
    }
  }
  return "ok";
}

export function markInbox(
  store: MockStore,
  id: string,
  change:
    | { type: "handled" }
    | { type: "done" }
    | { type: "reopen" }
    | { type: "snooze"; until: string }
    | { type: "mute"; muted?: boolean }
    | { type: "reply"; text: string },
): FeedWriteResult {
  const feed = store.feed;
  const record = feed.inbox.find((entry) => entry.id === id);
  if (!record) return "not-found";
  switch (change.type) {
    case "handled":
      record.unread = false;
      record.need = null;
      break;
    case "done":
      record.done = true;
      record.unread = false;
      record.need = null;
      record.snoozedUntil = null;
      break;
    case "reopen":
      record.done = false;
      record.snoozedUntil = null;
      break;
    case "snooze":
      // A snooze hides the entry until its time, then it is back and unread.
      record.snoozedUntil = change.until;
      record.done = false;
      record.unread = true;
      break;
    case "mute":
      record.muted = change.muted !== false;
      if (record.itemId) {
        if (record.muted) feed.mutedItemIds.add(record.itemId);
        else feed.mutedItemIds.delete(record.itemId);
      }
      break;
    case "reply": {
      if (record.mentionId) {
        const outcome = replyToMention(store, record.mentionId, change.text);
        if (outcome !== "ok") return outcome;
      } else if (record.itemId) {
        store.createCardComment(
          record.itemId,
          store.getCurrentUser().id,
          change.text,
        );
        record.need = record.need === "reply" ? null : record.need;
      } else return "invalid";
      record.unread = false;
      break;
    }
  }
  return "ok";
}

export function markAllInboxRead(store: MockStore): void {
  const now = store.getClock().isoNow();
  for (const record of store.feed.inbox) {
    // Done and snoozed entries are not in the inbox, so read-all leaves them.
    if (record.done || (record.snoozedUntil && record.snoozedUntil > now))
      continue;
    record.unread = false;
  }
}

export const DAY = DAY_MS;

export interface InboxSummaryDto {
  perWorkspace: { workspaceId: string; unread: number }[];
  total: number;
}

/**
 * Unread inbox entries per workspace of the member and in total (the rail's
 * badges). Only the primary workspace has a seeded inbox; the secondary
 * workspace reports the two unread entries the mockup's rail shows.
 */
export function buildInboxSummary(store: MockStore): InboxSummaryDto {
  const me = store.getCurrentUser().id;
  const perWorkspace = store
    .getWorkspaces()
    .filter((workspace) =>
      store.getWorkspaceMembers(workspace.id).some((m) => m.userId === me),
    )
    .map((workspace) => ({
      workspaceId: workspace.id,
      unread:
        workspace.id === mockIds.workspaces.primary
          ? store.feed.inbox.filter(
              (record) =>
                record.unread &&
                (!record.snoozedUntil ||
                  record.snoozedUntil <= store.getClock().isoNow()),
            ).length
          : workspace.id === mockIds.workspaces.secondary
            ? 2
            : 0,
    }));
  return {
    perWorkspace,
    total: perWorkspace.reduce((sum, entry) => sum + entry.unread, 0),
  };
}

export interface ChildItemDto {
  id: string;
  key: string | null;
  title: string;
  done: boolean;
  dueAt: string | null;
  assigneeUserId: string | null;
  assigneeName: string | null;
  position: string;
  version: number;
}

/** The child items of an item ("Việc con"), in position order. */
export function listChildItems(
  store: MockStore,
  itemId: string,
): ChildItemDto[] | "not-found" {
  if (!store.getCard(itemId)) return "not-found";
  return store.getChildCards(itemId).map((child) => childItemDto(store, child));
}

/** A child item as the contract lists it. */
export function childItemDto(
  store: MockStore,
  child: ReturnType<MockStore["getChildCards"]>[number],
): ChildItemDto {
  const owner = person(store, child.assigneeUserIds?.[0]);
  return {
    id: child.id,
    key: child.itemKey ?? null,
    title: child.title,
    done: statusOf(store, child.id)?.bucket === "done" || !!child.completedAt,
    dueAt: child.dueAt ?? null,
    assigneeUserId: owner?.id ?? null,
    assigneeName: owner?.name ?? null,
    position: String(child.position),
    version: 1,
  };
}

/** Whether the member follows an item (the sheet's "Đang theo dõi" chip). */
export function isFollowing(
  store: MockStore,
  itemId: string,
): boolean | "not-found" {
  if (!store.getCard(itemId)) return "not-found";
  return store.feed.following.has(itemId) || store.feed.assigned.has(itemId);
}
