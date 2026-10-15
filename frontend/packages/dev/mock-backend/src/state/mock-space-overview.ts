/**
 * The department page's summary (gap `CTR-GAP-WS-SPACE-OVERVIEW`, see
 * docs/superpowers/be-blockers-2026-10.md, "BE requirement from FE: Space"):
 * what the Boards, Docs and people of a Space add up to, which the Space,
 * Board and Doc producers do not report one by one. The viewer sees only the
 * Boards and Docs they may read; `asMember` previews a department member.
 */
import type { MockStore } from "./mock-store";
import { ERP_PEOPLE } from "./mock-erp-board";
import { mockIds } from "./mock-ids";

export interface OverviewPerson {
  readonly id: string;
  readonly name: string;
}

export interface SpaceOverviewDto {
  /** Everybody with access, teams expanded, each counted once. */
  readonly peopleTotal: number;
  /** The first people of the head's stack. */
  readonly people: readonly OverviewPerson[];
  readonly boardTotal: number;
  readonly boards: readonly {
    readonly id: string;
    readonly name: string;
    readonly kind: "kanban" | "table" | "inbox";
    readonly meta: string | null;
    readonly isPrivate: boolean;
    readonly addedCount: number | null;
    readonly progress:
      | { readonly kind: "bar"; readonly done: number; readonly total: number }
      | { readonly kind: "text"; readonly text: string }
      | null;
    readonly attention: {
      readonly tone: "over" | "warn" | "ok";
      readonly text: string;
    } | null;
    readonly people: readonly OverviewPerson[];
    readonly updatedAt: string | null;
  }[];
  readonly docTotal: number;
  readonly docs: readonly {
    readonly id: string;
    readonly meta: string | null;
    readonly editorName: string | null;
    readonly updatedAt: string | null;
  }[];
  /** Counts of the aside: overdue, unassigned, review, stale. */
  readonly attention: readonly {
    readonly id: "overdue" | "unassigned" | "review" | "stale";
    readonly count: number;
    readonly boardId: string | null;
  }[];
  readonly workload: readonly {
    readonly person: OverviewPerson;
    readonly active: number;
    readonly overdue: number;
  }[];
  readonly children: readonly {
    readonly id: string;
    readonly people: number | null;
    readonly boards: number | null;
  }[];
  readonly access: {
    readonly admins: readonly OverviewPerson[];
    readonly members: {
      readonly people: number;
      readonly teams: readonly {
        readonly name: string;
        readonly count: number;
      }[];
    };
    readonly viewers: {
      readonly people: number;
      readonly teams: readonly {
        readonly name: string;
        readonly count: number;
      }[];
    };
    /** Board guests who are not in the department. */
    readonly guests: readonly {
      readonly id: string;
      readonly name: string;
      readonly boards: readonly {
        readonly id: string;
        readonly name: string;
      }[];
    }[];
  };
  /** First-step templates of a department with nothing in it yet. */
  readonly templates: readonly {
    readonly id: string;
    readonly title: string;
    readonly hint: string;
    readonly icon: "contract" | "intake" | "calendar";
  }[];
}

const who = (person: { id: string; name: string }): OverviewPerson => ({
  id: person.id,
  name: person.name,
});

/** What the Board producers do not report: the mockup's figures for the department's Boards. */
interface BoardFigures {
  readonly kind: "kanban" | "table" | "inbox";
  readonly meta: string | null;
  readonly progress: SpaceOverviewDto["boards"][number]["progress"];
  readonly attention: SpaceOverviewDto["boards"][number]["attention"];
  readonly people: readonly OverviewPerson[];
  readonly updatedAt: string;
}

const TECH_FIGURES: Readonly<Record<string, BoardFigures>> = {
  [mockIds.boards.erp]: {
    kind: "kanban",
    meta: null,
    progress: null,
    attention: { tone: "over", text: "1 quá hạn" },
    people: [who(ERP_PEOPLE.MQ), who(ERP_PEOPLE.HP), who(ERP_PEOPLE.NA)],
    updatedAt: "2026-10-12T01:40:00.000Z",
  },
  "mock-board-it-infra": {
    kind: "table",
    meta: "Bảng · nhóm Hạ tầng",
    progress: { kind: "bar", done: 41, total: 57 },
    attention: { tone: "warn", text: "1 chờ duyệt" },
    people: [who(ERP_PEOPLE.VT), who(ERP_PEOPLE.HP)],
    updatedAt: "2026-10-11T09:00:00.000Z",
  },
  "mock-board-it-support": {
    kind: "inbox",
    meta: "Bảng · nhận yêu cầu từ mọi phòng ban",
    progress: { kind: "text", text: "23 yêu cầu mở" },
    attention: { tone: "warn", text: "6 chưa ai nhận" },
    people: [who(ERP_PEOPLE.LT), who(ERP_PEOPLE.VT)],
    updatedAt: "2026-10-12T03:30:00.000Z",
  },
  "mock-board-it-budget": {
    kind: "table",
    meta: null,
    progress: { kind: "bar", done: 3, total: 12 },
    attention: { tone: "ok", text: "Không có" },
    people: [who(ERP_PEOPLE.HP), who(ERP_PEOPLE.HA)],
    updatedAt: "2026-10-07T02:00:00.000Z",
  },
};

const TECH_DOC_NOTES: Readonly<Record<string, string>> = {
  "mock-doc-data-migration-plan": "Gắn với 6 việc trong Triển khai ERP Q4",
  "mock-doc-oncall-handbook": "Lịch trực, quy trình xử lý sự cố",
  "mock-doc-product-spec": "Phạm vi, phân hệ và mốc triển khai ERP",
};

const TECH_HEAD_ORDER: string[] = [
  ERP_PEOPLE.HP.id,
  ERP_PEOPLE.HA.id,
  ERP_PEOPLE.MQ.id,
  ERP_PEOPLE.NA.id,
];

/** Boards of the department that are finished and no longer "running". */
const TECH_FINISHED_BOARDS = 2;

const TECH_ATTENTION: SpaceOverviewDto["attention"] = [
  { id: "overdue", count: 3, boardId: mockIds.boards.erp },
  { id: "unassigned", count: 6, boardId: "mock-board-it-support" },
  { id: "review", count: 5, boardId: mockIds.boards.erp },
  { id: "stale", count: 1, boardId: "mock-board-it-budget" },
];

const TECH_WORKLOAD: SpaceOverviewDto["workload"] = [
  { person: who(ERP_PEOPLE.MQ), active: 7, overdue: 2 },
  { person: who(ERP_PEOPLE.NA), active: 5, overdue: 0 },
  { person: who(ERP_PEOPLE.VT), active: 4, overdue: 1 },
  { person: who(ERP_PEOPLE.HP), active: 3, overdue: 0 },
  { person: who(ERP_PEOPLE.LT), active: 2, overdue: 0 },
];

/** The sub-departments' own people and Boards (their pages are not read here). */
const TECH_CHILDREN: Readonly<
  Record<string, { people: number; boards: number }>
> = {
  [mockIds.spaces.itInfra]: { people: 6, boards: 2 },
  [mockIds.spaces.itApps]: { people: 9, boards: 3 },
};

const TEMPLATES: SpaceOverviewDto["templates"] = [
  {
    id: "contract",
    title: "Theo dõi hợp đồng",
    hint: "Soạn thảo, rà soát, ký, lưu hồ sơ · trường Đối tác, Giá trị, Ngày hết hạn",
    icon: "contract",
  },
  {
    id: "intake",
    title: "Tiếp nhận yêu cầu pháp lý",
    hint: "Các phòng ban gửi yêu cầu qua biểu mẫu, Pháp chế nhận và trả lời",
    icon: "intake",
  },
  {
    id: "calendar",
    title: "Lịch hạn giấy phép và văn bản",
    hint: "Lịch nhắc trước 30 ngày khi giấy phép sắp hết hạn",
    icon: "calendar",
  },
];

export function spaceOverview(
  store: MockStore,
  workspaceId: string,
  spaceId: string,
  options: {
    readonly canReadBoard: (boardId: string) => boolean;
    readonly asMember: boolean;
  },
): SpaceOverviewDto | null {
  const viewer = store.spaceViewer(workspaceId);
  const space = store.spaces.getSpace(spaceId);
  if (
    !space ||
    space.workspaceId !== workspaceId ||
    store.spaces.accessFor(spaceId, viewer) === null
  ) {
    return null;
  }
  const isTech = spaceId === mockIds.spaces.engineering;
  const now = store.getClock().now().getTime();

  // ── Boards ─────────────────────────────────────────────────────────
  const boards: SpaceOverviewDto["boards"][number][] = [];
  const overdueByUser = new Map<string, { active: number; overdue: number }>();
  const placed = store
    .getBoards(workspaceId)
    .filter(
      (board) =>
        board.spaceId === spaceId &&
        !board.isArchived &&
        options.canReadBoard(board.id),
    );
  for (const board of placed) {
    const isPrivate = board.visibility === "private";
    if (isPrivate && options.asMember) continue;
    const figures = TECH_FIGURES[board.id];
    const cards = store
      .getLists(board.id)
      .flatMap((list) => store.getCards(list.id))
      .filter((card) => !card.isArchived && !card.parentItemId);
    const statusColumn = store
      .getColumns(board.id)
      .find((column) => column.fieldType === "status");
    const categoryOf = (cardId: string) =>
      statusColumn?.options?.find(
        (option) => option.id === store.getFieldValues(cardId)[statusColumn.id],
      )?.category;
    let done = 0;
    let overdue = 0;
    let review = 0;
    const active = new Map<string, number>();
    for (const card of cards) {
      const category = categoryOf(card.id);
      if (category === "done") {
        done += 1;
        continue;
      }
      if (category === "review") review += 1;
      const late = Boolean(card.dueAt && Date.parse(card.dueAt) < now);
      if (late) overdue += 1;
      for (const userId of card.assigneeUserIds ?? []) {
        active.set(userId, (active.get(userId) ?? 0) + 1);
        const entry = overdueByUser.get(userId) ?? { active: 0, overdue: 0 };
        entry.active += 1;
        if (late) entry.overdue += 1;
        overdueByUser.set(userId, entry);
      }
    }
    const view = store.getBoardView(board.id)?.viewMode;
    const kind = figures?.kind ?? (view === "kanban" ? "kanban" : "table");
    const guests = store
      .getBoardMembers(board.id)
      .filter((member) => member.role === "Guest").length;
    const meta =
      figures?.meta ??
      [
        kind === "kanban" ? "Kanban" : "Bảng",
        ...(board.id === mockIds.boards.erp ? ["Go-live 01/11"] : []),
        ...(guests > 0 ? [`có ${guests} khách`] : []),
      ].join(" · ");
    const topPeople = [...active.entries()]
      .sort((a, b) => b[1] - a[1])
      .slice(0, 3)
      .map(([userId]) => userId);
    boards.push({
      id: board.id,
      name: board.title,
      kind,
      meta: isPrivate ? null : meta || (kind === "kanban" ? "Kanban" : "Bảng"),
      isPrivate,
      addedCount: isPrivate ? store.getBoardMembers(board.id).length : null,
      progress: figures?.progress ?? {
        kind: "bar",
        done,
        total: cards.length,
      },
      attention:
        figures?.attention ??
        (overdue > 0
          ? { tone: "over", text: `${overdue} quá hạn` }
          : review > 0
            ? { tone: "warn", text: `${review} chờ duyệt` }
            : { tone: "ok", text: "Không có" }),
      people:
        figures?.people ??
        topPeople.map((userId) =>
          who({ id: userId, name: store.getUser(userId)?.name ?? "" }),
        ),
      updatedAt: figures?.updatedAt ?? board.updatedAt ?? null,
    });
  }
  // The mockup's order: the figures' table order, then the rest by name.
  const figureOrder = Object.keys(TECH_FIGURES);
  boards.sort(
    (a, b) =>
      (figureOrder.indexOf(a.id) + 1 || 99) -
        (figureOrder.indexOf(b.id) + 1 || 99) ||
      a.name.localeCompare(b.name, "vi"),
  );
  const boardTotal = boards.length + (isTech ? TECH_FINISHED_BOARDS : 0);

  // ── Docs ───────────────────────────────────────────────────────────
  const pages = store
    .getPages(workspaceId)
    .filter((page) => page.spaceId === spaceId && !page.parentId)
    .sort((a, b) => (b.updatedAt ?? "").localeCompare(a.updatedAt ?? ""));
  const docs = pages.slice(0, 2).map((page) => {
    const last = [...store.getPageHistory(page.id)].sort((a, b) =>
      b.createdAt.localeCompare(a.createdAt),
    )[0];
    return {
      id: page.id,
      meta: TECH_DOC_NOTES[page.id] ?? null,
      editorName: last ? (store.getUser(last.actorId)?.name ?? null) : null,
      updatedAt: page.updatedAt ?? null,
    };
  });

  // ── Children ───────────────────────────────────────────────────────
  const children = store.spaces
    .spacesOf(workspaceId)
    .filter((child) => child.parentId === spaceId && !child.archived)
    .filter(
      (child) =>
        store.spaces.accessFor(child.id, viewer) !== null &&
        !(options.asMember && child.visibility === "Private"),
    )
    .map((child) => ({
      id: child.id,
      people: TECH_CHILDREN[child.id]?.people ?? null,
      boards: TECH_CHILDREN[child.id]?.boards ?? null,
    }));

  // ── People ─────────────────────────────────────────────────────────
  const rank = { Admin: 3, Member: 2, Viewer: 1 } as const;
  const roleOf = new Map<string, keyof typeof rank>();
  const teamsOfRole = {
    Member: [] as { name: string; count: number }[],
    Viewer: [] as { name: string; count: number }[],
  };
  const raise = (userId: string, role: keyof typeof rank) => {
    const current = roleOf.get(userId);
    if (!current || rank[role] > rank[current]) roleOf.set(userId, role);
  };
  for (const entry of store.spaces.membersOf(spaceId)) {
    if (entry.subjectType === "User") {
      raise(entry.subjectId, entry.role);
      continue;
    }
    const team = store.spaces.getTeam(entry.subjectId);
    if (!team || team.archived) continue;
    for (const member of team.members) raise(member.userId, entry.role);
    if (entry.role !== "Admin") {
      teamsOfRole[entry.role].push({
        name: team.name,
        count: team.members.length,
      });
    }
  }
  const count = (role: keyof typeof rank) =>
    [...roleOf.values()].filter((value) => value === role).length;
  const nameOf = (userId: string) => store.getUser(userId)?.name ?? "";
  const admins = [...roleOf.entries()]
    .filter(([, role]) => role === "Admin")
    .map(([userId]) => ({ id: userId, name: nameOf(userId) }))
    .sort((a, b) => a.name.localeCompare(b.name, "vi"));
  const head = (isTech ? TECH_HEAD_ORDER : [])
    .concat(admins.map((admin) => admin.id))
    .concat([...roleOf.keys()]);
  const seen = new Set<string>();
  const people: OverviewPerson[] = [];
  for (const userId of head) {
    if (seen.has(userId) || !roleOf.has(userId)) continue;
    seen.add(userId);
    people.push({ id: userId, name: nameOf(userId) });
    if (people.length === 4) break;
  }
  const guests = new Map<
    string,
    { id: string; name: string; boards: { id: string; name: string }[] }
  >();
  for (const board of placed) {
    for (const member of store.getBoardMembers(board.id)) {
      if (member.role !== "Guest" || roleOf.has(member.userId)) continue;
      const entry = guests.get(member.userId) ?? {
        id: member.userId,
        name: nameOf(member.userId),
        boards: [],
      };
      entry.boards.push({ id: board.id, name: board.title });
      guests.set(member.userId, entry);
    }
  }

  const empty =
    boards.length === 0 && docs.length === 0 && children.length === 0;
  return {
    peopleTotal: roleOf.size,
    people,
    boardTotal,
    boards,
    docTotal: pages.length,
    docs,
    attention: isTech ? TECH_ATTENTION : [],
    workload: isTech
      ? TECH_WORKLOAD
      : [...overdueByUser.entries()]
          .sort((a, b) => b[1].active - a[1].active)
          .slice(0, 5)
          .map(([userId, value]) => ({
            person: { id: userId, name: nameOf(userId) },
            ...value,
          })),
    children,
    access: {
      admins,
      members: { people: count("Member"), teams: teamsOfRole.Member },
      viewers: { people: count("Viewer"), teams: teamsOfRole.Viewer },
      guests: [...guests.values()],
    },
    templates: empty ? TEMPLATES : [],
  };
}
