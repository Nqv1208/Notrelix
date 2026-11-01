/**
 * The Board's own permissions as `WorkManagement.Boards.GetSharing` answers:
 * only the entries given on the Board itself, beside what its department gives
 * by role; the level of each and how it is held (an explicit grant, a Board
 * membership, or both); the history of the changes; and who loses access when
 * the Board goes Private. Entries are disclosed only to a Board manager or a
 * Board member; any other reader gets the counts. Nothing on a Board expires,
 * and a Guest is capped at Viewer.
 */
import type { OperationResponse } from "@notrelix/contracts";
import type { MockStore } from "./mock-store";
import { ERP_PEOPLE } from "./mock-erp-board";
import { mockIds } from "./mock-ids";
import { spaceMembersExtra } from "./mock-space-members";

export type ShareLevel = "viewer" | "commenter" | "editor" | "manager";

/** A Board role a membership can carry. */
export type MockBoardRoleName =
  "Owner" | "Admin" | "Member" | "Observer" | "Guest";

const ROLE_LEVEL: Record<MockBoardRoleName, ShareLevel> = {
  Owner: "manager",
  Admin: "manager",
  Member: "editor",
  Observer: "viewer",
  Guest: "viewer",
};

const LEVEL_RANK: Record<ShareLevel, number> = {
  viewer: 1,
  commenter: 2,
  editor: 3,
  manager: 4,
};

export interface MockShareEntry {
  /** The grant id (`permissionId`) of an entry held by grant. */
  id: string;
  /** The grant's version; a change of its level bumps it. */
  version?: number;
  kind: "person" | "team";
  userId: string | null;
  teamId: string | null;
  /** The effective stored level: the higher of the grant and the role's. */
  level: ShareLevel;
  /** The explicit grant's level, when the entry has one. */
  grantLevel: ShareLevel | null;
  /** The Board role, when the person is a Board member. */
  memberRole: MockBoardRoleName | null;
  /** A Workspace Guest (a membership fact, never an email domain). */
  guest: boolean;
  owner: boolean;
  addedAt: string;
}

/** The entry's level after its grant or role changed. */
export function effectiveLevel(entry: MockShareEntry): ShareLevel {
  const levels = [
    entry.grantLevel,
    entry.memberRole ? ROLE_LEVEL[entry.memberRole] : null,
  ].filter((level): level is ShareLevel => level !== null);
  return levels.reduce<ShareLevel>(
    (best, level) => (LEVEL_RANK[level] > LEVEL_RANK[best] ? level : best),
    "viewer",
  );
}

export function levelOfRole(role: MockBoardRoleName): ShareLevel {
  return ROLE_LEVEL[role];
}

export interface MockShareLog {
  id: string;
  kind: "added" | "changed" | "removed" | "private" | "visibility";
  actorId: string | null;
  /** The entry's person or team name at the time. */
  target: string | null;
  from: ShareLevel | null;
  to: ShareLevel | null;
  guest: boolean;
  at: string;
}

export type BoardSharingDto = OperationResponse<
  "WorkManagement.Boards.GetSharing",
  200
>;

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
  ): MockShareEntry => {
    const base: MockShareEntry = {
      id,
      userId: null,
      teamId: null,
      grantLevel: over.memberRole ? null : over.level,
      memberRole: null,
      guest: false,
      owner: false,
      addedAt: at,
      ...over,
    };
    return base;
  };
  sharing.setBoard(
    boardId,
    [
      entry("share-hp", {
        kind: "person",
        userId: ERP_PEOPLE.HP.id,
        level: "manager",
        memberRole: "Owner",
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
      }),
      entry("share-dt", {
        kind: "person",
        userId: ERP_PEOPLE.DT.id,
        level: "viewer",
        guest: true,
      }),
      entry("share-ha", {
        kind: "person",
        userId: mockIds.users.owner,
        level: "manager",
        memberRole: "Admin",
      }),
    ],
    [
      {
        id: "share-log-1",
        kind: "added",
        actorId: mockIds.users.owner,
        target: ERP_PEOPLE.DT.name,
        from: null,
        to: "viewer",
        guest: true,
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
        at: "2026-10-02T03:00:00.000Z",
      },
      {
        id: "share-log-3",
        kind: "visibility",
        actorId: ERP_PEOPLE.HP.id,
        target: null,
        from: null,
        to: null,
        guest: false,
        at: "2026-09-15T03:00:00.000Z",
      },
    ],
  );
}

const SPACE_ROLE_RANK = { Admin: 3, Member: 2, Viewer: 1 } as const;

/** Whether the viewer is shown who holds access: a Board manager or member. */
function entriesDisclosed(
  entries: readonly MockShareEntry[],
  viewerId: string,
  canManage: boolean,
): boolean {
  return (
    canManage ||
    entries.some((item) => item.userId === viewerId && item.memberRole !== null)
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
        if (!current || SPACE_ROLE_RANK[entry.role] > SPACE_ROLE_RANK[current])
          spaceRoles.set(entry.subjectId, entry.role);
      }
    }
  }
  const list = store.sharing.entriesOf(boardId);
  const disclosed = entriesDisclosed(list, me, canManage);
  // Email is for a workspace Owner or Admin, and only of people who are not Guests.
  const role = store.getCurrentMemberRole(board.workspaceId);
  const seesEmail = role === "owner" || role === "admin";

  const entries = list.map((item) => {
    const base = {
      owner: item.owner,
      level: item.level,
      addedAt: item.addedAt,
      permissionId: item.grantLevel === null ? null : item.id,
      permissionVersion: item.grantLevel === null ? null : (item.version ?? 1),
      memberRole: item.memberRole,
      source:
        item.grantLevel !== null && item.memberRole !== null
          ? "both"
          : item.memberRole !== null
            ? "member"
            : "grant",
    };
    if (item.kind === "team") {
      const team = store.spaces.getTeam(item.teamId ?? "");
      return {
        ...base,
        id: item.teamId ?? item.id,
        kind: "team",
        name: team?.name ?? "",
        email: null,
        you: false,
        levelCapped: false,
        spaceRole: null,
        outsideSpace: true,
        teamPeople: team?.members.length ?? null,
      };
    }
    const user = store.getUser(item.userId ?? "");
    const spaceRole = spaceRoles.get(item.userId ?? "") ?? null;
    // The producer caps a Guest at Viewer whatever the stored level (outside people are view-only).
    const capped = item.guest && item.level !== "viewer";
    return {
      ...base,
      id: item.userId ?? item.id,
      kind: item.guest ? "guest" : "person",
      name: user?.name ?? "",
      email: seesEmail && !item.guest ? (user?.email ?? null) : null,
      you: item.userId === me,
      level: capped ? "viewer" : item.level,
      levelCapped: capped,
      spaceRole,
      outsideSpace: spaceRole === null,
      teamPeople: null,
    };
  });

  const kept = new Set(
    list.map((item) => item.userId).filter((id): id is string => Boolean(id)),
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
  const guests = list.filter((item) => item.guest).length;
  return {
    boardId,
    canManage,
    entriesDisclosed: disclosed,
    visibility: board.visibility === "private" ? "Private" : "Workspace",
    spaceId: space?.id ?? null,
    spaceName: space?.name ?? null,
    spacePeople,
    peopleCount: (spacePeople ?? 0) + list.length - guests,
    guestCount: guests,
    peopleCountComplete: true,
    // The version the member writes name (adding or removing a member, changing a role).
    version: board.version ?? 1,
    entries: disclosed ? entries : [],
    log: canManage
      ? store.sharing.logOf(boardId).map((item) => ({
          id: item.id,
          kind: item.kind,
          actor: item.actorId
            ? {
                userId: item.actorId,
                name: store.getUser(item.actorId)?.name ?? "",
              }
            : undefined,
          target: item.target,
          from: item.from,
          to: item.to,
          guest: item.guest,
          at: item.at,
        }))
      : [],
    privateImpact: canManage
      ? { lose, people, keep: entries.map((item) => item.name) }
      : undefined,
  };
}
