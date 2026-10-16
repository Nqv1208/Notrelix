/**
 * The people a board's item can name (gap `CTR-GAP-WM-ITEM-STATES`, see
 * docs/superpowers/be-blockers-2026-10.md, "BE requirement from FE: States"):
 * the Assignee picker and the @mention list read it. Each person says whether
 * they see the board (so whether the item reaches them), their department and
 * how many open items they hold on it. A team says how many of its people
 * see the board. Pure projection of the store.
 */
import { mockIds } from "./mock-ids";
import type { MockStore } from "./mock-store";

export interface PeopleDirectoryDto {
  readonly people: readonly {
    readonly userId: string;
    readonly name: string;
    readonly department: string | null;
    /** Open items the person holds on this board. */
    readonly openCount: number;
    /** The person sees the board, so an item assigned or mentioned reaches them. */
    readonly onBoard: boolean;
    /** What the person may do on the board; null when they do not see it. */
    readonly level: "viewer" | "commenter" | "editor" | "manager" | null;
  }[];
  readonly teams: readonly {
    readonly id: string;
    readonly name: string;
    readonly memberCount: number;
    /** Members who see the board. */
    readonly viewableCount: number;
    /** Those members, so a mention of the team reaches each of them. */
    readonly viewableUserIds: readonly string[];
  }[];
  /** The caller manages the board: they may invite a person who does not see it. */
  readonly canInvite: boolean;
}

/** People whose department the user ids do not tell. */
const DEPARTMENT_OF_USER: Readonly<Record<string, string>> = {
  [mockIds.users.commenter]: "Kế toán",
};

const DEPARTMENT_OF_PREFIX: readonly (readonly [string, string])[] = [
  ["mock-user-kt-", "Kế toán"],
  ["mock-user-eng-", "Công nghệ"],
  ["mock-user-erp-", "Công nghệ"],
];

function departmentOf(userId: string): string | null {
  const named = DEPARTMENT_OF_USER[userId];
  if (named) return named;
  for (const [prefix, department] of DEPARTMENT_OF_PREFIX)
    if (userId.startsWith(prefix)) return department;
  return null;
}

export function peopleDirectory(
  store: MockStore,
  boardId: string,
  canInvite: boolean,
): PeopleDirectoryDto | null {
  const board = store.getBoard(boardId);
  if (!board) return null;
  const ROLE_LEVEL = {
    Owner: "manager",
    Admin: "manager",
    Member: "editor",
    Observer: "viewer",
    Guest: "viewer",
  } as const;
  // What each person may do here: an explicit grant, else their membership.
  const levelOf = new Map<
    string,
    "viewer" | "commenter" | "editor" | "manager"
  >();
  for (const member of store.getBoardMembers(boardId))
    levelOf.set(member.userId, ROLE_LEVEL[member.role]);
  for (const user of store.listUsers()) {
    const grant = store.getBoardGrant(boardId, user.id);
    if (grant) levelOf.set(user.id, grant.level);
  }
  const onBoard = new Set(levelOf.keys());
  const openByUser = new Map<string, number>();
  for (const list of store.getLists(boardId))
    for (const card of store.getCards(list.id)) {
      if (card.parentItemId || card.isArchived || card.completedAt) continue;
      for (const userId of card.assigneeUserIds ?? [])
        openByUser.set(userId, (openByUser.get(userId) ?? 0) + 1);
    }
  const people = store.listUsers().map((user) => ({
    userId: user.id,
    name: user.name,
    department:
      departmentOf(user.id) ?? (onBoard.has(user.id) ? "Công nghệ" : null),
    openCount: openByUser.get(user.id) ?? 0,
    onBoard: onBoard.has(user.id),
    level: levelOf.get(user.id) ?? null,
  }));
  const teams = store.spaces
    .teamsOf(board.workspaceId)
    .filter((team) => !team.archived && team.members.length > 0)
    .map((team) => ({
      id: team.id,
      name: team.name,
      memberCount: team.members.length,
      viewableCount: team.members.filter((member) => onBoard.has(member.userId))
        .length,
      viewableUserIds: team.members
        .filter((member) => onBoard.has(member.userId))
        .map((member) => member.userId),
    }));
  return { people, teams, canInvite };
}
