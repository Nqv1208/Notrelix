import type { IdentityTone, MarkToneId } from "../../core/model/navigation";
import type {
  SpaceMemberEntry,
  SpaceNode,
  SpaceRole,
  SpaceTeam,
} from "../../core/model/spaces";
import { formatCopy } from "../shell/shell-models";
import type { WorkspaceSpacesCopy } from "./workspace-spaces.copy";

/**
 * What the department (Space) page draws, in the shape the page needs. The
 * host builds these from the Space producer, the Board and Doc lists and the
 * member list; a part the producers cannot supply is `null` and the surface
 * leaves it out instead of inventing it.
 */

/** A person on an avatar: initials come from the name, the colour from the id. */
export interface SpaceAvatarView {
  readonly id: string;
  readonly name: string;
  readonly tone: IdentityTone;
}

export type SpaceBoardIcon = "kanban" | "table" | "inbox";

export type SpaceAttentionTone = "over" | "warn" | "ok";

export type SpaceProgressView =
  | { readonly kind: "bar"; readonly done: number; readonly total: number }
  | { readonly kind: "text"; readonly text: string };

/** A Board in the "running Boards" list. */
export interface SpaceBoardRowView {
  readonly id: string;
  readonly name: string;
  readonly icon: SpaceBoardIcon;
  /** Second line (view kind, go-live, guests); `null` leaves the line out. */
  readonly meta: string | null;
  readonly isPrivate: boolean;
  /** People added to a private Board; `null` when the read does not say. */
  readonly addedCount: number | null;
  /** The columns the Board producers cannot supply stay `null`. */
  readonly progress: SpaceProgressView | null;
  readonly attention: {
    readonly tone: SpaceAttentionTone;
    readonly text: string;
  } | null;
  readonly people: readonly SpaceAvatarView[] | null;
  readonly updatedAt: string | null;
}

export interface SpaceDocRowView {
  readonly id: string;
  readonly name: string;
  readonly meta: string | null;
  /** "Trần Minh Quân sửa"; `null` when the Doc list has no editor. */
  readonly editor: string | null;
  readonly updatedAt: string | null;
}

export interface SpaceContentView {
  readonly boards: readonly SpaceBoardRowView[];
  /** Boards the member can see in the Space, for "See all N Boards". */
  readonly boardTotal: number;
  readonly docs: readonly SpaceDocRowView[];
  readonly docTotal: number;
}

export interface SpaceChildView {
  readonly id: string;
  readonly name: string;
  readonly markTone: MarkToneId;
  readonly isPrivate: boolean;
  /** `null` when the placements are not loaded. */
  readonly boards: number | null;
  /** People in the child, when the read has it. */
  readonly people: number | null;
}

export interface SpaceHeadPeople {
  readonly avatars: readonly SpaceAvatarView[];
  /** Entries beyond the avatars ("+20"). */
  readonly more: number;
  /** Accessible label of the stack. */
  readonly label: string;
}

export interface SpaceAttentionRowView {
  readonly id: string;
  readonly tone: "over" | "warn" | "mute";
  readonly count: number;
  readonly label: string;
  readonly href: string | null;
}

export interface SpaceWorkloadRowView {
  readonly id: string;
  readonly person: SpaceAvatarView;
  readonly active: number;
  readonly overdue: number;
  /** Width of the bar's blue and red parts, 0 to 100. */
  readonly activePct: number;
  readonly overduePct: number;
}

export interface SpaceAccessRowView {
  readonly key: string;
  readonly label: string;
  readonly value: string;
  readonly note: string | null;
  readonly href: string | null;
  /** Nobody holds this role yet: the value reads muted. */
  readonly empty?: boolean;
}

/** The "who can open this department" card. */
export interface SpaceAccessView {
  readonly visibility: "Private" | "Workspace";
  /** "24 người: 2 quản trị · …"; `null` when members are not loaded. */
  readonly summary: string | null;
  readonly rows: readonly SpaceAccessRowView[];
}

/** One of the three first-step templates. */
export interface SpaceTemplateView {
  readonly id: string;
  readonly title: string;
  readonly hint: string;
  readonly icon: "contract" | "intake" | "calendar";
}

export const SPACE_ROLE_ORDER: readonly SpaceRole[] = [
  "Admin",
  "Member",
  "Viewer",
];

/** Entries (people and teams) of a role. */
export function entriesOfRole(
  members: readonly SpaceMemberEntry[],
  role: SpaceRole,
): SpaceMemberEntry[] {
  return members.filter((entry) => entry.role === role);
}

function pluralOf(
  copy: Record<string, string>,
  key: string,
  count: number,
): string {
  const one = copy[`${key}_one`];
  const other = copy[`${key}_other`];
  return formatCopy(count === 1 ? (one ?? "") : (other ?? ""), { count });
}

/**
 * The head's avatar stack: the people added directly (up to four) and how
 * many entries are beyond them. A team is an entry, not a person, so it
 * counts in "more" and the label says "people and teams".
 */
export function buildHeadPeople(
  copy: WorkspaceSpacesCopy,
  members: readonly SpaceMemberEntry[],
  toneOf: (id: string) => IdentityTone,
  shown = 4,
): SpaceHeadPeople {
  const people = members.filter((entry) => entry.subjectType === "User");
  const avatars = people.slice(0, shown).map((entry) => ({
    id: entry.subjectId,
    name: entry.displayName,
    tone: toneOf(entry.subjectId),
  }));
  return {
    avatars,
    more: Math.max(0, members.length - avatars.length),
    label: formatCopy(copy.page.peopleLabelEntries, { count: members.length }),
  };
}

/**
 * The access card: the visibility, the head count by role and who holds each
 * role. Names come from the member entries; "includes team X (n people)"
 * needs the Team list, so it is left out when the member cannot read it.
 */
export function buildAccessView(
  copy: WorkspaceSpacesCopy,
  node: Pick<SpaceNode, "visibility">,
  members: readonly SpaceMemberEntry[] | null,
  teams: readonly SpaceTeam[] | null,
  options: { readonly meId: string | null },
): SpaceAccessView {
  const a = copy.aside;
  if (members === null) {
    return { visibility: node.visibility, summary: null, rows: [] };
  }
  const teamById = new Map((teams ?? []).map((team) => [team.id, team]));
  const people = members.filter((entry) => entry.subjectType === "User");
  const groups = members.filter((entry) => entry.subjectType === "Team");
  const countOf = (role: SpaceRole) => entriesOfRole(members, role).length;

  const peoplePart = pluralOf(a, "summaryPeople", people.length);
  const summaryHead =
    groups.length > 0
      ? `${peoplePart}${a.summaryAnd}${pluralOf(a, "summaryTeams", groups.length)}`
      : peoplePart;
  const summary = formatCopy(a.summary, {
    people: summaryHead,
    admins: formatCopy(a.summaryAdmins, { count: countOf("Admin") }),
    members: formatCopy(a.summaryMembers, { count: countOf("Member") }),
    viewers: formatCopy(a.summaryViewers, { count: countOf("Viewer") }),
  });

  const rows: SpaceAccessRowView[] = [];
  const admins = entriesOfRole(members, "Admin");
  rows.push({
    key: "admins",
    label: a.admins,
    value:
      admins.length > 0
        ? admins.map((entry) => entry.displayName).join(", ")
        : a.nobody,
    note: null,
    href: null,
    empty: admins.length === 0,
  });
  for (const role of ["Member", "Viewer"] as const) {
    const entries = entriesOfRole(members, role);
    const direct = entries.filter((entry) => entry.subjectType === "User");
    const roleTeams = entries.filter((entry) => entry.subjectType === "Team");
    const label = role === "Member" ? a.members : a.viewers;
    if (entries.length === 0) {
      rows.push({
        key: role,
        label,
        value: a.nobody,
        note: null,
        href: null,
        empty: true,
      });
      continue;
    }
    const value =
      direct.length > 0
        ? pluralOf(a, "people", direct.length)
        : pluralOf(a, "teams", roleTeams.length);
    const noted = roleTeams
      .map((entry) => {
        const team = teamById.get(entry.subjectId);
        return team
          ? formatCopy(a.includesTeam, {
              name: entry.displayName,
              count: team.memberCount,
            })
          : null;
      })
      .filter((text): text is string => text !== null);
    rows.push({
      key: role,
      label,
      value,
      note: noted.length > 0 ? noted.join(" · ") : null,
      href: null,
    });
  }
  void options;
  return { visibility: node.visibility, summary, rows };
}

/** "2 giờ trước", "hôm qua", "5 ngày trước", then "03/10". */
export function spaceWhenLabel(
  copy: WorkspaceSpacesCopy,
  at: string | null,
  now: number,
): string {
  if (!at) return "";
  const time = Date.parse(at);
  if (Number.isNaN(time)) return "";
  const w = copy.overview.when;
  const diff = now - time;
  if (diff < 0) return "";
  const minutes = Math.floor(diff / 60000);
  if (minutes < 1) return w.now;
  if (minutes < 60) return formatCopy(w.minutes, { count: minutes });
  const hours = Math.floor(minutes / 60);
  if (hours < 24) return formatCopy(w.hours, { count: hours });
  const startOfDay = (value: number) => {
    const date = new Date(value);
    date.setHours(0, 0, 0, 0);
    return date.getTime();
  };
  const days = Math.round((startOfDay(now) - startOfDay(time)) / 86_400_000);
  if (days <= 1) return w.yesterday;
  if (days < 7) return formatCopy(w.days, { count: days });
  const date = new Date(time);
  const pad = (value: number) => String(value).padStart(2, "0");
  return `${pad(date.getDate())}/${pad(date.getMonth() + 1)}`;
}
