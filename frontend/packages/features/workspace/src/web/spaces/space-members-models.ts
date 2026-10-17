import type {
  IdentityTone,
  WorkspaceDestination,
} from "../../core/model/navigation";
import {
  spaceMemberCapabilities,
  spaceMemberKey,
  type SpaceMemberBlock,
  type SpaceMemberEntry,
  type SpaceRole,
  type SpaceSubjectType,
  type SpaceTeam,
} from "../../core/model/spaces";
import type { WorkspaceRole } from "../../core/types/workspace";
import { formatCopy } from "../shell/shell-models";
import type { SpaceAvatarView } from "./space-page-models";
import type { WorkspaceSpacesCopy } from "./workspace-spaces.copy";

/**
 * What the members tab of a department draws (Final-Members), and the pure
 * rules behind it: the rows, the filters, the summary line and what removing
 * a team takes away. The host supplies the directory and the team people; a
 * part the producers cannot supply is `null` and the surface leaves it out.
 */

export interface SpaceMemberCandidate {
  readonly id: string;
  readonly name: string;
  readonly avatarUrl: string | null;
  /** Used to match a typed email; absent when the read does not carry it. */
  readonly email?: string | null;
}

export interface AddSpaceMemberInput {
  readonly subjectType: SpaceSubjectType;
  readonly subjectId: string;
  readonly role: SpaceRole;
}

/** A workspace member known by id: where the second line comes from. */
export interface SpaceDirectoryPerson {
  readonly email: string | null;
  readonly workspaceRole: WorkspaceRole | null;
  /** A job line when the workspace has one (the mock world has none). */
  readonly title?: string | null;
  /** How the sentences of the role menu call the person ("Phúc"); the full name when absent. */
  readonly shortName?: string | null;
  /** Added to the first source chip ("người tạo phòng"). */
  readonly sourceNote?: string | null;
  /** "10 phút trước", "đang hoạt động"; the date the person was added when absent. */
  readonly activity?: string | null;
}

/** A guest of a single Board, who is not in the department. */
export interface SpaceGuestView {
  readonly id: string;
  readonly name: string;
  readonly tone: IdentityTone;
  /** "Ngoài công ty · Có thể sửa ở Triển khai ERP Q4 · hết quyền 31/01/2027". */
  readonly detail: string;
  /** Where the guest is shared (the Board); `null` when it cannot be opened. */
  readonly destination: WorkspaceDestination | null;
}

export interface TeamPersonView {
  readonly id: string;
  readonly name: string;
  readonly tone: IdentityTone;
  /** "Tổng giám đốc"; `null` when the read has none. */
  readonly note: string | null;
}

/** A workspace invitation that is waiting (the design shows it in the list). */
export interface SpacePendingInviteView {
  readonly id: string;
  readonly email: string;
  readonly name: string | null;
  /** "mời 2 ngày trước". */
  readonly sent: string;
  readonly role: SpaceRole;
}

export interface MemberSourceChip {
  readonly text: string;
  readonly icon: "team" | null;
}

export interface SpaceMemberRowView {
  readonly key: string;
  readonly entry: SpaceMemberEntry;
  readonly kind: "person" | "team";
  readonly name: string;
  readonly shortName: string;
  readonly avatar: SpaceAvatarView | null;
  readonly isYou: boolean;
  readonly detail: string | null;
  readonly sources: readonly MemberSourceChip[];
  readonly activity: string;
  /** The team's head count; `null` when the Team list is not readable. */
  readonly teamCount: number | null;
  readonly roles: readonly SpaceRole[];
  readonly canRemove: boolean;
  readonly blockedBy: SpaceMemberBlock | null;
  /** Another way the person joins, with the role it gives (story data). */
  readonly also: { readonly team: string; readonly role: SpaceRole } | null;
  readonly pending: boolean;
}

export interface MemberRowOptions {
  readonly directory: ReadonlyMap<string, SpaceDirectoryPerson>;
  readonly teams: readonly SpaceTeam[] | null;
  readonly meId: string | null;
  readonly canManage: boolean;
  readonly pendingKeys: ReadonlySet<string>;
  readonly toneOf: (id: string) => IdentityTone;
  /** dd/MM of an ISO time. */
  readonly formatDay: (iso: string) => string;
  readonly also?: ReadonlyMap<string, { team: string; role: SpaceRole }>;
}

const ROLE_RANK: Record<SpaceRole, number> = { Admin: 0, Member: 1, Viewer: 2 };

/** Teams first, then people by role (Admin, Member, Viewer); ties keep the producer's order. */
export function sortMemberEntries(
  entries: readonly SpaceMemberEntry[],
): SpaceMemberEntry[] {
  return [...entries].sort((a, b) => {
    if (a.subjectType !== b.subjectType)
      return a.subjectType === "Team" ? -1 : 1;
    return ROLE_RANK[a.role] - ROLE_RANK[b.role];
  });
}

export function buildMemberRows(
  copy: WorkspaceSpacesCopy,
  entries: readonly SpaceMemberEntry[],
  options: MemberRowOptions,
): SpaceMemberRowView[] {
  const m = copy.members;
  const teamById = new Map(
    (options.teams ?? []).map((team) => [team.id, team]),
  );
  return sortMemberEntries(entries).map((entry) => {
    const key = spaceMemberKey(entry);
    const capabilities = spaceMemberCapabilities(
      entry,
      entries,
      options.canManage,
    );
    const since = entry.addedAt
      ? formatCopy(m.since, { date: options.formatDay(entry.addedAt) })
      : "";
    if (entry.subjectType === "Team") {
      const team = teamById.get(entry.subjectId);
      return {
        key,
        entry,
        kind: "team",
        name: entry.displayName,
        shortName: entry.displayName,
        avatar: null,
        isYou: false,
        detail: team
          ? formatCopy(m.teamMeta, { count: team.memberCount })
          : m.teamMetaNoCount,
        sources: [{ text: m.directTeam, icon: null }],
        activity: since,
        teamCount: team ? team.memberCount : null,
        roles: capabilities.roles,
        canRemove: capabilities.canRemove,
        blockedBy: capabilities.blockedBy,
        also: null,
        pending: options.pendingKeys.has(key),
      };
    }
    const person = options.directory.get(entry.subjectId);
    const detail = [
      person?.email ?? null,
      person?.title ??
        (person?.workspaceRole ? m.workspaceRole[person.workspaceRole] : null),
    ]
      .filter((part): part is string => Boolean(part))
      .join(" · ");
    return {
      key,
      entry,
      kind: "person",
      name: entry.displayName,
      shortName: person?.shortName ?? entry.displayName,
      avatar: {
        id: entry.subjectId,
        name: entry.displayName,
        tone: options.toneOf(entry.subjectId),
      },
      isYou: options.meId === entry.subjectId,
      detail: detail || null,
      sources: [
        {
          text: person?.sourceNote
            ? `${m.direct} · ${person.sourceNote}`
            : m.direct,
          icon: null,
        },
        ...(options.also?.get(entry.subjectId)
          ? [
              {
                // "Nhóm Kỹ sư ERP" reads "Kỹ sư ERP" in the short chip.
                text: (options.also.get(entry.subjectId)?.team ?? "").replace(
                  /^Nhóm\s+/i,
                  "",
                ),
                icon: "team" as const,
              },
            ]
          : []),
      ],
      activity: person?.activity || since,
      teamCount: null,
      roles: capabilities.roles,
      canRemove: capabilities.canRemove,
      blockedBy: capabilities.blockedBy,
      also: options.also?.get(entry.subjectId) ?? null,
      pending: options.pendingKeys.has(key),
    };
  });
}

export type MemberRoleFilter = SpaceRole | "all";
export type MemberViaFilter = "any" | "direct" | "team";

export interface MemberFilters {
  readonly query: string;
  readonly role: MemberRoleFilter;
  readonly via: MemberViaFilter;
}

export const NO_MEMBER_FILTERS: MemberFilters = {
  query: "",
  role: "all",
  via: "any",
};

export function plain(value: string): string {
  return value
    .normalize("NFD")
    .replace(/[̀-ͯ]/g, "")
    .replace(/đ/gi, "d")
    .toLowerCase();
}

export function filterMemberRows(
  rows: readonly SpaceMemberRowView[],
  filters: MemberFilters,
): SpaceMemberRowView[] {
  const query = plain(filters.query.trim());
  return rows.filter((row) => {
    if (filters.role !== "all" && row.entry.role !== filters.role) return false;
    if (filters.via === "direct" && row.kind !== "person") return false;
    if (filters.via === "team" && row.kind !== "team") return false;
    if (!query) return true;
    return plain(`${row.name} ${row.detail ?? ""}`).includes(query);
  });
}

/**
 * "2 quản trị · 19 thành viên · 3 người xem" over the entries, or over the
 * head counts the producer reports (teams expanded) when it has them.
 */
export function memberSummary(
  copy: WorkspaceSpacesCopy,
  entries: readonly SpaceMemberEntry[],
  counts?: {
    readonly admins: number;
    readonly members: number;
    readonly viewers: number;
  } | null,
): string {
  const count = (role: SpaceRole) =>
    counts
      ? role === "Admin"
        ? counts.admins
        : role === "Member"
          ? counts.members
          : counts.viewers
      : entries.filter((entry) => entry.role === role).length;
  return formatCopy(copy.members.summary, {
    admins: formatCopy(copy.aside.summaryAdmins, { count: count("Admin") }),
    members: formatCopy(copy.aside.summaryMembers, { count: count("Member") }),
    viewers: formatCopy(copy.aside.summaryViewers, { count: count("Viewer") }),
  });
}

export interface TeamRemoval {
  readonly lose: readonly TeamPersonView[];
  /** People who stay because they were added directly, with their role. */
  readonly keep: readonly {
    readonly person: TeamPersonView;
    readonly role: SpaceRole;
  }[];
}

/** Who loses access when a team entry goes, and who stays by a direct entry. */
export function teamRemoval(
  people: readonly TeamPersonView[],
  entries: readonly SpaceMemberEntry[],
): TeamRemoval {
  const direct = new Map(
    entries
      .filter((entry) => entry.subjectType === "User")
      .map((entry) => [entry.subjectId, entry.role]),
  );
  const lose: TeamPersonView[] = [];
  const keep: { person: TeamPersonView; role: SpaceRole }[] = [];
  for (const person of people) {
    const role = direct.get(person.id);
    if (role) keep.push({ person, role });
    else lose.push(person);
  }
  return { lose, keep };
}

export type TypedToken =
  | { readonly kind: "person"; readonly candidate: SpaceMemberCandidate }
  | { readonly kind: "team"; readonly team: SpaceTeam }
  | { readonly kind: "outsider"; readonly email: string };

export function isEmailLike(value: string): boolean {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value.trim());
}

/** The people and teams a typed text could mean (never an existing entry). */
export function suggestTokens(
  text: string,
  users: readonly SpaceMemberCandidate[],
  teams: readonly SpaceTeam[],
  taken: ReadonlySet<string>,
  limit = 6,
): TypedToken[] {
  const query = plain(text.trim());
  if (!query) return [];
  const out: TypedToken[] = [];
  for (const user of users) {
    if (taken.has(`User:${user.id}`)) continue;
    if (plain(`${user.name} ${user.email ?? ""}`).includes(query))
      out.push({ kind: "person", candidate: user });
  }
  for (const team of teams) {
    if (taken.has(`Team:${team.id}`)) continue;
    if (plain(team.name).includes(query)) out.push({ kind: "team", team });
  }
  return out.slice(0, limit);
}
