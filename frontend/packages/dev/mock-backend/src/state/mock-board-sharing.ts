/**
 * The Board's own permissions as the share dialog draws them (gap
 * `CTR-GAP-WM-BOARD-SHARING`, see docs/superpowers/be-blockers-2026-10.md,
 * "BE requirement from FE: Share"): only the entries given on the Board itself,
 * beside what its department gives by role; the level and end date of each; the
 * history of the changes; and who loses access when the Board goes Private.
 */
import type { MockStore } from "./mock-store";
import { ERP_PEOPLE } from "./mock-erp-board";
import { mockIds } from "./mock-ids";
import { spaceMembersExtra } from "./mock-space-members";

export type ShareLevel = "viewer" | "commenter" | "editor" | "manager";

export interface MockShareEntry {
  id: string;
  kind: "person" | "team";
  userId: string | null;
  teamId: string | null;
  level: ShareLevel;
  expiresAt: string | null;
  owner: boolean;
  addedAt: string;
}

export interface MockShareLog {
  id: string;
  kind:
    | "added"
    | "changed"
    | "removed"
    | "expired"
    | "public-off"
    | "created"
    | "private";
  actorId: string | null;
  /** The entry's person or team name at the time. */
  target: string | null;
  from: ShareLevel | null;
  to: ShareLevel | null;
  guest: boolean;
  expiresAt: string | null;
  at: string;
}

export interface BoardSharingDto {
  readonly spaceName: string | null;
  readonly spacePeople: number | null;
  readonly canManage: boolean;
  readonly entries: readonly {
    readonly id: string;
    readonly kind: "person" | "team" | "guest";
    readonly userId: string | null;
    readonly name: string;
    readonly email: string | null;
    readonly you: boolean;
    readonly owner: boolean;
    /** The role the person has in the Board's department, when they are in it. */
    readonly spaceRole: "Admin" | "Member" | "Viewer" | null;
    /** The person's own department, when it is not the Board's. */
    readonly department: string | null;
    readonly teamPeople: number | null;
    readonly outsideSpace: boolean;
    readonly outsideCompany: boolean;
    readonly level: ShareLevel;
    readonly expiresAt: string | null;
    /** The explicit grant behind the entry (the producer's `permissionId`). */
    readonly permissionId: string;
    readonly source: "grant";
  }[];
  readonly log: readonly {
    readonly id: string;
    readonly kind: MockShareLog["kind"];
    readonly actor: { readonly id: string; readonly name: string } | null;
    readonly target: string | null;
    readonly from: ShareLevel | null;
    readonly to: ShareLevel | null;
    readonly guest: boolean;
    readonly expiresAt: string | null;
    readonly at: string;
  }[];
  /** Who loses access when the Board goes Private. */
  readonly privateImpact: {
    readonly lose: number;
    readonly people: readonly {
      readonly userId: string;
      readonly name: string;
      readonly items: number;
      readonly overdue: number;
    }[];
    /** Names of the entries that keep their access. */
    readonly keep: readonly string[];
  };
}

/** The mockup's unfinished-work figures of the people who would lose the Board. */
const ERP_WORK: Readonly<Record<string, { items: number; overdue: number }>> = {
  [ERP_PEOPLE.MQ.id]: { items: 2, overdue: 1 },
  [ERP_PEOPLE.NA.id]: { items: 1, overdue: 0 },
  [ERP_PEOPLE.VT.id]: { items: 1, overdue: 0 },
};

export class MockBoardSharing {
  private entries = new Map<string, MockShareEntry[]>();
  private logs = new Map<string, MockShareLog[]>();
  private counter = 0;

  clear(): void {
    this.entries.clear();
    this.logs.clear();
    this.counter = 0;
  }

  setBoard(
    boardId: string,
    entries: MockShareEntry[],
    log: MockShareLog[],
  ): void {
    this.entries.set(boardId, entries);
    this.logs.set(boardId, log);
  }

  entriesOf(boardId: string): MockShareEntry[] {
    return this.entries.get(boardId) ?? [];
  }

  logOf(boardId: string): MockShareLog[] {
    return this.logs.get(boardId) ?? [];
  }

  pushLog(boardId: string, entry: Omit<MockShareLog, "id">): void {
    this.counter += 1;
    const list = this.logs.get(boardId) ?? [];
    list.unshift({ id: `share-log-w${this.counter}`, ...entry });
    this.logs.set(boardId, list);
  }

  nextId(): string {
    this.counter += 1;
    return `share-w${this.counter}`;
  }
}

export function seedErpSharing(sharing: MockBoardSharing): void {
  const boardId = mockIds.boards.erp;
  const at = "2026-09-01T02:00:00.000Z";
  const entry = (
    id: string,
    over: Partial<MockShareEntry> & Pick<MockShareEntry, "kind" | "level">,
  ): MockShareEntry => ({
    id,
    userId: null,
    teamId: null,
    expiresAt: null,
    owner: false,
    addedAt: at,
    ...over,
  });
  sharing.setBoard(
    boardId,
    [
      entry("share-hp", {
        kind: "person",
        userId: ERP_PEOPLE.HP.id,
        level: "manager",
        owner: true,
      }),
      entry("share-kt", {
        kind: "team",
        teamId: "mock-team-erp-accounting",
        level: "viewer",
      }),
      entry("share-kl", {
        kind: "person",
        userId: mockIds.users.commenter,
        level: "commenter",
        expiresAt: "2026-12-31T16:59:59.000Z",
      }),
      entry("share-dt", {
        kind: "person",
        userId: ERP_PEOPLE.DT.id,
        level: "editor",
        expiresAt: "2027-01-31T16:59:59.000Z",
      }),
      entry("share-ha", {
        kind: "person",
        userId: mockIds.users.owner,
        level: "manager",
      }),
    ],
    [
      {
        id: "share-log-1",
        kind: "added",
        actorId: mockIds.users.owner,
        target: ERP_PEOPLE.DT.name,
        from: null,
        to: "editor",
        guest: true,
        expiresAt: "2027-01-31T16:59:59.000Z",
        at: "2026-10-08T07:12:00.000Z",
      },
      {
        id: "share-log-2",
        kind: "changed",
        actorId: ERP_PEOPLE.HP.id,
        target: "Nhóm Kế toán tổng hợp",
        from: "editor",
        to: "viewer",
        guest: false,
        expiresAt: null,
        at: "2026-10-02T03:00:00.000Z",
      },
      {
        id: "share-log-3",
        kind: "expired",
        actorId: null,
        target: "Nguyễn Văn Bình",
        from: null,
        to: null,
        guest: false,
        expiresAt: null,
        at: "2026-09-30T03:00:00.000Z",
      },
      {
        id: "share-log-4",
        kind: "public-off",
        actorId: null,
        target: null,
        from: null,
        to: null,
        guest: false,
        expiresAt: null,
        at: "2026-09-15T03:00:00.000Z",
      },
      {
        id: "share-log-5",
        kind: "created",
        actorId: ERP_PEOPLE.HP.id,
        target: null,
        from: null,
        to: null,
        guest: false,
        expiresAt: null,
        at: "2026-09-01T02:00:00.000Z",
      },
    ],
  );
}

export function boardSharing(
  store: MockStore,
  boardId: string,
  canManage: boolean,
): BoardSharingDto | null {
  const board = store.getBoard(boardId);
  if (!board) return null;
  const me = store.getCurrentUser().id;
  const space = board.spaceId ? store.spaces.getSpace(board.spaceId) : null;
  const spaceRoles = new Map<string, "Admin" | "Member" | "Viewer">();
  let spacePeople: number | null = null;
  if (space) {
    const extra = spaceMembersExtra(
      store,
      board.workspaceId,
      space.id,
      () => true,
    );
    spacePeople = extra?.people.length ?? null;
    for (const entry of store.spaces.membersOf(space.id)) {
      if (entry.subjectType === "User") {
        const current = spaceRoles.get(entry.subjectId);
        const rank = { Admin: 3, Member: 2, Viewer: 1 } as const;
        if (!current || rank[entry.role] > rank[current])
          spaceRoles.set(entry.subjectId, entry.role);
      }
    }
  }
  // The department a person belongs to by their own entry.
  const homeOf = (userId: string): string | null => {
    for (const candidate of store.spaces.spacesOf(board.workspaceId)) {
      if (candidate.parentId) continue;
      if (
        store.spaces
          .membersOf(candidate.id)
          .some(
            (entry) =>
              entry.subjectType === "User" && entry.subjectId === userId,
          )
      )
        return candidate.name;
    }
    return null;
  };
  const entries = store.sharing.entriesOf(boardId).map((item) => {
    if (item.kind === "team") {
      const team = store.spaces.getTeam(item.teamId ?? "");
      return {
        id: item.teamId ?? item.id,
        permissionId: item.id,
        source: "grant" as const,
        kind: "team" as const,
        userId: null,
        name: team?.name ?? "",
        email: null,
        you: false,
        owner: false,
        spaceRole: null,
        department: null,
        teamPeople: team?.members.length ?? null,
        outsideSpace: true,
        outsideCompany: false,
        level: item.level,
        expiresAt: item.expiresAt,
      };
    }
    const user = store.getUser(item.userId ?? "");
    const email = user?.email ?? null;
    const role = spaceRoles.get(item.userId ?? "") ?? null;
    // Somebody in the department works for the company whatever their address.
    const outsideCompany =
      role === null && !(email ?? "").endsWith("@minhan.vn");
    const home = homeOf(item.userId ?? "");
    return {
      id: item.userId ?? item.id,
      permissionId: item.id,
      source: "grant" as const,
      kind: outsideCompany ? ("guest" as const) : ("person" as const),
      userId: item.userId,
      name: user?.name ?? "",
      email,
      you: item.userId === me,
      owner: item.owner,
      spaceRole: role,
      department: role ? null : home,
      teamPeople: null,
      outsideSpace: role === null,
      outsideCompany,
      level: item.level,
      expiresAt: item.expiresAt,
    };
  });

  const kept = new Set(
    store.sharing
      .entriesOf(boardId)
      .map((item) => item.userId)
      .filter((id): id is string => Boolean(id)),
  );
  const extra = space
    ? spaceMembersExtra(store, board.workspaceId, space.id, () => true)
    : null;
  const lose = extra
    ? extra.people.filter((person) => !kept.has(person.userId)).length
    : 0;
  const people = Object.entries(ERP_WORK)
    .filter(([id]) => boardId === mockIds.boards.erp && !kept.has(id))
    .map(([id, work]) => ({
      userId: id,
      name: store.getUser(id)?.name ?? "",
      ...work,
    }));
  return {
    spaceName: space?.name ?? null,
    spacePeople,
    canManage,
    entries,
    log: store.sharing.logOf(boardId).map((item) => ({
      id: item.id,
      kind: item.kind,
      actor: item.actorId
        ? { id: item.actorId, name: store.getUser(item.actorId)?.name ?? "" }
        : null,
      target: item.target,
      from: item.from,
      to: item.to,
      guest: item.guest,
      expiresAt: item.expiresAt,
      at: item.at,
    })),
    privateImpact: {
      lose,
      people,
      keep: entries.map((item) => item.name),
    },
  };
}
