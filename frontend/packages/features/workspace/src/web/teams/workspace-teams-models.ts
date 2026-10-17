import {
  identityToneFor,
  markToneFor,
  type IdentityTone,
  type MarkToneId,
} from "../../core/model/navigation";
import type { Team } from "../../core/model/teams";
import { formatCopy } from "../shell/shell-models";
import type { WorkspaceTeamsCopy } from "./workspace-teams.copy";

/**
 * What the Teams pages draw beyond the Team producer's list (Final-Teams): the
 * extras the host reads (`CTR-GAP-WS-TEAMS-EXT`) and the pure rules that turn
 * them into rows: who is the lead, the avatar stack, where a Team is in use and
 * who loses what when it is archived.
 */

export type SpaceGrant = "Admin" | "Member" | "Viewer";
export type BoardGrant = "manager" | "editor" | "commenter" | "viewer";

export interface TeamExtPerson {
  readonly userId: string;
  readonly name: string;
  readonly email: string | null;
  readonly title: string | null;
  readonly role: "Lead" | "Member";
  readonly since: string;
}

export interface TeamExtUsage {
  readonly kind: "space" | "board";
  readonly id: string;
  readonly name: string;
  readonly spaceType: string | null;
  readonly grant: SpaceGrant | BoardGrant;
  readonly since: string | null;
  /** For a department: the people added to it directly, who keep it when the Team goes. */
  readonly direct?: readonly {
    readonly userId: string;
    readonly role: SpaceGrant;
  }[];
}

export interface TeamExt {
  readonly id: string;
  readonly createdAt: string;
  readonly createdBy: { readonly id: string; readonly name: string } | null;
  readonly archivedAt: string | null;
  readonly archivedBy: { readonly id: string; readonly name: string } | null;
  readonly people: readonly TeamExtPerson[];
  readonly usage: readonly TeamExtUsage[];
}

export interface TeamsExtData {
  readonly teams: readonly TeamExt[];
  readonly admins: readonly {
    readonly id: string;
    readonly name: string;
    readonly email: string | null;
    readonly role: "owner" | "admin";
  }[];
  /** Active people the workspace can put in a Team. */
  readonly directory: readonly {
    readonly id: string;
    readonly name: string;
    readonly email: string | null;
    readonly title: string | null;
  }[];
}

export interface TeamAvatarView {
  readonly id: string;
  readonly name: string;
  readonly tone: IdentityTone;
}

export interface TeamUsageView {
  readonly key: string;
  readonly text: string;
  /** A department's initials mark; `null` for a Board. */
  readonly mark: { readonly name: string; readonly tone: MarkToneId } | null;
  readonly destination:
    | { readonly section: "space"; readonly spaceId: string }
    | { readonly section: "board"; readonly boardId: string };
}

export interface TeamRowView {
  readonly id: string;
  readonly name: string;
  readonly description: string | null;
  readonly archived: boolean;
  readonly tone: IdentityTone;
  readonly people: {
    readonly avatars: readonly TeamAvatarView[];
    readonly more: number;
    readonly count: number;
  };
  readonly lead: TeamAvatarView | null;
  readonly usage: readonly TeamUsageView[];
  /** "Lưu trữ 20/09 · Nguyễn Thu Hà" for an archived Team. */
  readonly archivedLine: string | null;
  /** Everything a search may match: name, description, people. */
  readonly haystack: string;
}

export function plainText(value: string): string {
  return value
    .normalize("NFD")
    .replace(/[̀-ͯ]/g, "")
    .replace(/đ/gi, "d")
    .toLowerCase();
}

/** "Phòng Công nghệ" for a department, the name itself for anything else. */
export function spaceLabel(
  copy: WorkspaceTeamsCopy,
  name: string,
  spaceType: string | null,
): string {
  return spaceType === "Department"
    ? formatCopy(copy.page.departmentName, { name })
    : name;
}

export function usageText(
  copy: WorkspaceTeamsCopy,
  usage: TeamExtUsage,
): string {
  if (usage.kind === "space") {
    return formatCopy(copy.page.usageSpace, {
      name: spaceLabel(copy, usage.name, usage.spaceType),
      role: copy.page.spaceRoles[usage.grant as SpaceGrant],
    });
  }
  return formatCopy(copy.page.usageBoard, {
    name: usage.name,
    level: copy.page.boardLevels[usage.grant as BoardGrant],
  });
}

/** Up to three avatars for a small Team; one and "+N" for a larger one (the mockup). */
export function avatarStack(people: readonly TeamExtPerson[]): {
  avatars: readonly TeamAvatarView[];
  more: number;
} {
  const view = (person: TeamExtPerson): TeamAvatarView => ({
    id: person.userId,
    name: person.name,
    tone: identityToneFor(person.userId),
  });
  if (people.length <= 4)
    return { avatars: people.slice(0, 3).map(view), more: 0 };
  return { avatars: people.slice(0, 1).map(view), more: people.length - 1 };
}

export function buildTeamRows(
  copy: WorkspaceTeamsCopy,
  teams: readonly Team[],
  ext: readonly TeamExt[] | null,
  formatDay: (iso: string) => string,
): TeamRowView[] {
  const extOf = new Map((ext ?? []).map((item) => [item.id, item]));
  return teams.map((team) => {
    const info = extOf.get(team.id) ?? null;
    const people = info?.people ?? [];
    const lead = people.find((person) => person.role === "Lead") ?? null;
    const archived = team.status === "Archived";
    const stack = avatarStack(people);
    const usage = (info?.usage ?? []).map((item): TeamUsageView => ({
      key: `${item.kind}:${item.id}`,
      // An archived Team's access is paused: the place is named, not the grant.
      text: archived
        ? spaceLabel(copy, item.name, item.spaceType)
        : usageText(copy, item),
      mark:
        item.kind === "space"
          ? { name: item.name, tone: markToneFor(item.id) }
          : null,
      destination:
        item.kind === "space"
          ? { section: "space", spaceId: item.id }
          : { section: "board", boardId: item.id },
    }));
    return {
      id: team.id,
      name: team.name,
      description: team.description,
      archived,
      tone: identityToneFor(team.id),
      people: { ...stack, count: team.memberCount },
      lead: lead
        ? {
            id: lead.userId,
            name: lead.name,
            tone: identityToneFor(lead.userId),
          }
        : null,
      usage,
      archivedLine:
        archived && info?.archivedAt
          ? formatCopy(copy.page.archivedLine, {
              date: formatDay(info.archivedAt),
              name: info.archivedBy?.name ?? "",
            })
          : null,
      haystack: plainText(
        [team.name, team.description ?? "", ...people.map((p) => p.name)].join(
          " ",
        ),
      ),
    };
  });
}

export function filterTeamRows(
  rows: readonly TeamRowView[],
  query: string,
): TeamRowView[] {
  const needle = plainText(query.trim());
  if (!needle) return [...rows];
  return rows.filter((row) => row.haystack.includes(needle));
}

export interface ArchiveLoss {
  /** The departments the Team gives a role in: where people lose access. */
  readonly where: readonly string[];
  readonly lose: readonly { id: string; name: string; tone: IdentityTone }[];
  readonly keep: readonly {
    id: string;
    name: string;
    tone: IdentityTone;
    role: SpaceGrant;
  }[];
}

/**
 * Who loses a department when the Team is archived, and who stays because they
 * were added to it directly (the department's own member entries).
 */
export function archiveLoss(
  copy: WorkspaceTeamsCopy,
  team: TeamExt,
): ArchiveLoss {
  const spaces = team.usage.filter((item) => item.kind === "space");
  const directRoles = new Map<string, SpaceGrant>();
  for (const space of spaces)
    for (const entry of space.direct ?? [])
      directRoles.set(entry.userId, entry.role);
  const where = spaces.map((item) =>
    spaceLabel(copy, item.name, item.spaceType),
  );
  const lose: { id: string; name: string; tone: IdentityTone }[] = [];
  const keep: ArchiveLoss["keep"][number][] = [];
  for (const person of team.people) {
    const direct = directRoles.get(person.userId);
    const view = {
      id: person.userId,
      name: person.name,
      tone: identityToneFor(person.userId),
    };
    if (direct) keep.push({ ...view, role: direct });
    else lose.push(view);
  }
  return { where, lose: spaces.length > 0 ? lose : [], keep };
}

/** The first two words after "Nhóm": what the Team tile takes its initials from. */
export function teamTileName(name: string): string {
  return name
    .replace(/^Nhóm\s+/i, "")
    .split(/\s+/)
    .slice(0, 2)
    .join(" ");
}

/** The first letter of each of the first two words after "Nhóm" ("Kỹ sư ERP" is "KS"). */
export function teamInitials(name: string): string {
  return Array.from(teamTileName(name).split(/\s+/))
    .map((word) => Array.from(word)[0] ?? "")
    .join("")
    .toUpperCase();
}
