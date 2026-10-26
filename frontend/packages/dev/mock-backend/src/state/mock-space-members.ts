/**
 * What the department's Members page adds to the Space member list (gap
 * `CTR-GAP-WS-MEMBERS-EXT`, see docs/superpowers/be-blockers-2026-10.md,
 * "BE requirement from FE: Members"): the head count by role with the teams
 * expanded, each person's email, job line and last activity, the waiting
 * workspace invitations and the Board guests who are not in the department.
 */
import type { MockStore } from "./mock-store";
import { PENDING_INVITES, profileOf } from "./mock-space-people";

export interface SpaceMembersExtraDto {
  readonly summary: {
    readonly admins: number;
    readonly members: number;
    readonly viewers: number;
    readonly invites: number;
  };
  /** Everybody with access through the department, teams expanded. */
  readonly people: readonly {
    readonly userId: string;
    readonly email: string | null;
    readonly title: string | null;
    readonly lastActiveAt: string | null;
    readonly activeNow: boolean;
    /** Teams of the department the person is also in. */
    readonly viaTeams: readonly string[];
    /** The person made the department. */
    readonly creator: boolean;
  }[];
  readonly invitations: readonly {
    readonly id: string;
    readonly email: string;
    readonly name: string | null;
    readonly sentAt: string;
    readonly role: "Admin" | "Member" | "Viewer";
  }[];
  readonly guests: readonly {
    readonly userId: string;
    readonly name: string;
    readonly email: string | null;
    /** From outside the company (another email domain). */
    readonly outside: boolean;
    readonly boardId: string;
    readonly boardName: string;
    readonly level: "viewer" | "commenter" | "editor" | "manager";
  }[];
}

const RANK = { Admin: 3, Member: 2, Viewer: 1 } as const;

export function spaceMembersExtra(
  store: MockStore,
  workspaceId: string,
  spaceId: string,
  canReadBoard: (boardId: string) => boolean,
): SpaceMembersExtraDto | null {
  const viewer = store.spaceViewer(workspaceId);
  const space = store.spaces.getSpace(spaceId);
  if (
    !space ||
    space.workspaceId !== workspaceId ||
    store.spaces.accessFor(spaceId, viewer) === null
  ) {
    return null;
  }
  const roleOf = new Map<string, keyof typeof RANK>();
  const viaTeams = new Map<string, string[]>();
  const raise = (userId: string, role: keyof typeof RANK) => {
    const current = roleOf.get(userId);
    if (!current || RANK[role] > RANK[current]) roleOf.set(userId, role);
  };
  for (const entry of store.spaces.membersOf(spaceId)) {
    if (entry.subjectType === "User") {
      raise(entry.subjectId, entry.role);
      continue;
    }
    const team = store.spaces.getTeam(entry.subjectId);
    if (!team || team.archived) continue;
    for (const member of team.members) {
      raise(member.userId, entry.role);
      viaTeams.set(member.userId, [
        ...(viaTeams.get(member.userId) ?? []),
        team.name,
      ]);
    }
  }
  const count = (role: keyof typeof RANK) =>
    [...roleOf.values()].filter((value) => value === role).length;
  const invites = PENDING_INVITES[spaceId] ?? [];
  const me = store.getCurrentUser().id;
  const people = [...roleOf.keys()].map((userId) => {
    const user = store.getUser(userId);
    const profile = profileOf(userId, user?.email ?? "");
    return {
      userId,
      email: profile.email || null,
      title: profile.title || null,
      lastActiveAt: profile.lastActiveAt || null,
      activeNow: profile.activeNow === true,
      viaTeams: viaTeams.get(userId) ?? [],
      // The owner made every seeded department.
      creator: userId === me && roleOf.get(userId) === "Admin",
    };
  });

  const guests: SpaceMembersExtraDto["guests"][number][] = [];
  const domain = (email: string | null) => email?.split("@")[1] ?? "";
  const home = domain(store.getUser(me)?.email ?? null);
  for (const board of store.getBoards(workspaceId)) {
    if (
      board.spaceId !== spaceId ||
      board.isArchived ||
      !canReadBoard(board.id)
    )
      continue;
    for (const member of store.getBoardMembers(board.id)) {
      if (member.role !== "Guest" || roleOf.has(member.userId)) continue;
      const user = store.getUser(member.userId);
      const grant = store.getBoardGrant(board.id, member.userId);
      guests.push({
        userId: member.userId,
        name: user?.name ?? "",
        email: user?.email ?? null,
        outside: domain(user?.email ?? null) !== "minhan.vn" && home !== "",
        boardId: board.id,
        boardName: board.title,
        level: grant?.level ?? "viewer",
      });
    }
  }
  return {
    summary: {
      admins: count("Admin"),
      members: count("Member"),
      viewers: count("Viewer"),
      invites: invites.length,
    },
    people,
    invitations: invites.map((invite) => ({ ...invite })),
    guests,
  };
}
