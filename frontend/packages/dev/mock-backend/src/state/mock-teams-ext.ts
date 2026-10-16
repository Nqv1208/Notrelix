/**
 * What the Teams pages add to the Team producer (gap `CTR-GAP-WS-TEAMS-EXT`,
 * see docs/superpowers/be-blockers-2026-10.md, "BE requirement from FE:
 * Teams"): each Team's leader and people with email, job line and the day they
 * joined, who made and who archived it, and where it is in use (departments
 * with the role it gives, Boards with the permission it gives, paused while
 * the Team is archived); and the workspace admins, for the page a member who
 * cannot manage Teams is sent to.
 */
import type { MockStore } from "./mock-store";
import { mockIds } from "./mock-ids";
import { profileOf } from "./mock-space-people";

export interface TeamsExtDto {
  readonly teams: readonly {
    readonly id: string;
    readonly createdAt: string;
    readonly createdBy: { readonly id: string; readonly name: string } | null;
    readonly archivedAt: string | null;
    readonly archivedBy: { readonly id: string; readonly name: string } | null;
    readonly people: readonly {
      readonly userId: string;
      readonly name: string;
      readonly email: string | null;
      readonly title: string | null;
      readonly role: "Lead" | "Member";
      readonly since: string;
    }[];
    readonly usage: readonly {
      readonly kind: "space" | "board";
      readonly id: string;
      readonly name: string;
      readonly spaceType: string | null;
      /** The department role or the Board permission the Team gives. */
      readonly grant:
        | "Admin"
        | "Member"
        | "Viewer"
        | "manager"
        | "editor"
        | "commenter"
        | "viewer";
      readonly since: string | null;
      /** For a department: the people added to it directly. */
      readonly direct?: readonly {
        readonly userId: string;
        readonly role: "Admin" | "Member" | "Viewer";
      }[];
    }[];
  }[];
  readonly admins: readonly {
    readonly id: string;
    readonly name: string;
    readonly email: string | null;
    readonly role: "owner" | "admin";
  }[];
  /** Active people a Team may take (company people; guests are not offered). */
  readonly directory: readonly {
    readonly id: string;
    readonly name: string;
    readonly email: string | null;
    readonly title: string | null;
  }[];
}

/** Where the mockup's Teams differ from their profile: email, job line, join day. */
const PERSON_NOTES: Readonly<
  Record<string, { email?: string; title?: string; since?: string }>
> = {
  "mock-user-erp-ngoc-anh": { title: "Kỹ sư triển khai" },
  "mock-user-eng-quoc-bao": {
    email: "bao.dang@minhan.vn",
    title: "Kỹ sư tích hợp",
  },
  "mock-user-eng-gia-minh": {
    email: "minh.lam@minhan.vn",
    title: "Kỹ sư dữ liệu",
    since: "2026-09-15T02:00:00.000Z",
  },
};

/** Who archived the paused team and when. */
const ARCHIVED: Readonly<Record<string, { at: string; byId: string }>> = {
  [mockIds.teams.oncall]: {
    at: "2026-09-20T02:00:00.000Z",
    byId: mockIds.users.owner,
  },
};

export function teamsExt(store: MockStore, workspaceId: string): TeamsExtDto {
  const nameOf = (id: string) => store.getUser(id)?.name ?? "";
  const who = (id: string | null) => (id ? { id, name: nameOf(id) } : null);
  const teams = store.spaces.teamsOf(workspaceId).map((team) => {
    const archived = ARCHIVED[team.id] ?? null;
    const people = team.members.map((member) => {
      const note = PERSON_NOTES[member.userId] ?? {};
      const user = store.getUser(member.userId);
      const profile = profileOf(member.userId, user?.email ?? "");
      return {
        userId: member.userId,
        name: user?.name ?? "",
        email: note.email ?? (profile.email || null),
        title: note.title ?? (profile.title || null),
        role: member.role,
        since: note.since ?? team.createdAt,
      };
    });
    const usage: TeamsExtDto["teams"][number]["usage"][number][] = [];
    for (const space of store.spaces.spacesOf(workspaceId)) {
      for (const entry of store.spaces.membersOf(space.id)) {
        if (entry.subjectType === "Team" && entry.subjectId === team.id) {
          usage.push({
            kind: "space",
            id: space.id,
            name: space.name,
            spaceType: space.type,
            grant: entry.role,
            since: entry.addedAt,
            direct: store.spaces
              .membersOf(space.id)
              .filter((other) => other.subjectType === "User")
              .map((other) => ({ userId: other.subjectId, role: other.role })),
          });
        }
      }
    }
    for (const board of store.getBoards(workspaceId)) {
      for (const entry of store.sharing.entriesOf(board.id)) {
        if (entry.kind === "team" && entry.teamId === team.id) {
          usage.push({
            kind: "board",
            id: board.id,
            name: board.title,
            spaceType: null,
            grant: entry.level,
            since: entry.addedAt,
          });
        }
      }
    }
    return {
      id: team.id,
      createdAt: team.createdAt,
      createdBy: who(mockIds.users.owner),
      archivedAt: archived?.at ?? null,
      archivedBy: who(archived?.byId ?? null),
      people,
      usage,
    };
  });
  const admins = store
    .getWorkspaceMembers(workspaceId)
    .filter((member) => member.role === "owner" || member.role === "admin")
    .map((member) => ({
      id: member.userId,
      name: nameOf(member.userId),
      email: store.getUser(member.userId)?.email ?? null,
      role: member.role as "owner" | "admin",
    }));
  const directory = store
    .listUsers()
    .filter((user) => (user.email ?? "").endsWith("@minhan.vn"))
    .map((user) => {
      const profile = profileOf(user.id, user.email ?? "");
      return {
        id: user.id,
        name: user.name,
        email: PERSON_NOTES[user.id]?.email ?? (user.email || null),
        title: PERSON_NOTES[user.id]?.title ?? (profile.title || null),
      };
    })
    .sort((a, b) => a.name.localeCompare(b.name, "vi"));
  return { teams, admins, directory };
}

export interface SecurityExtDto {
  /** Guests the workspace has now, across Boards and pages. */
  readonly guestCount: number;
  /** Days a new guest's access lasts unless the inviter changes it; `null` = no end. */
  readonly guestExpiryDays: number | null;
  /** Shown and suggested when inviting; never decides who is a guest. */
  readonly companyDomain: string;
  /** When the share links were turned off, while they are off. */
  readonly linksOffSince: string | null;
}

export function securityExt(
  store: MockStore,
  workspaceId: string,
): SecurityExtDto {
  const off =
    store.getWorkspaceSettings(workspaceId).allowPublicSharing === false;
  return {
    guestCount: 2,
    guestExpiryDays: 90,
    companyDomain: "minhan.vn",
    linksOffSince: off ? "2026-09-15T02:00:00.000Z" : null,
  };
}
