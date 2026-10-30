import type { BoardAccess, BoardMember } from "@notrelix/work-management-core";

/**
 * What the Board share dialog draws (Final-Share). The host builds it from the
 * Board access read, the Board's members and the workspace policy; a part no
 * producer fills stays `null` and the dialog leaves it out.
 */

export type ShareLevel = "manage" | "edit" | "comment" | "view";

export type SharePersonTone = "blue" | "coral" | "gold" | "sage" | "ember";

export interface SharePersonView {
  readonly id: string;
  readonly name: string;
  readonly initials: string;
  readonly tone: SharePersonTone;
}

export interface ShareRowView {
  readonly id: string;
  readonly kind: "person" | "team" | "guest";
  readonly name: string;
  /** How the menu sentence calls the person ("Tuấn"); the full name when absent. */
  readonly shortName?: string;
  readonly person: SharePersonView | null;
  readonly isYou: boolean;
  /** The second line, already worded. */
  readonly detail: string | null;
  /** `null`: no producer says what the entry may do. */
  readonly level: ShareLevel | null;
  /** The level is the person's by role and cannot be changed (the owner). */
  readonly fixed: boolean;
  readonly badge: "guest" | "outside" | null;
  /** "31/01/2027"; `null` when the entry does not expire or the read has none. */
}

export type ShareGeneral =
  | {
      readonly kind: "space";
      readonly name: string;
      readonly count: number | null;
      readonly mark: { readonly initials: string; readonly tone: string };
    }
  | { readonly kind: "workspace" }
  | { readonly kind: "private" };

export interface ShareImpactPerson extends SharePersonView {
  readonly items: number;
  readonly overdue: number;
}

export interface ShareLogEntry {
  readonly id: string;
  readonly actor: SharePersonView | null;
  /** The sentence, with the strong parts marked by `**`. */
  readonly text: string;
  readonly when: string;
  readonly note: string | null;
}

export interface BoardShareView {
  readonly boardTitle: string;
  readonly space: {
    readonly name: string;
    readonly initials: string;
    readonly markTone: string;
  } | null;
  readonly workspaceName: string;
  readonly general: ShareGeneral;
  readonly rows: readonly ShareRowView[];
  /** Shown "Show n more" past this many rows; the mockup list caps at 248 px. */
  readonly policy: "off" | "on" | "unknown";
  readonly canManage: boolean;
  /**
   * Set when the producer discloses no entries to this reader: the list is not
   * drawn and the dialog says how many people and guests can open the Board.
   */
  readonly hiddenCounts?: {
    readonly people: number;
    readonly guests: number;
  } | null;
}

const LEVEL_OF_ROLE: Record<BoardMember["role"], ShareLevel> = {
  owner: "manage",
  editor: "edit",
  viewer: "view",
};

export function levelOfBoardRole(role: BoardMember["role"]): ShareLevel {
  return LEVEL_OF_ROLE[role] ?? "view";
}

const TONES: readonly SharePersonTone[] = [
  "blue",
  "coral",
  "gold",
  "sage",
  "ember",
];

export function shareToneFor(id: string): SharePersonTone {
  let hash = 0;
  for (let index = 0; index < id.length; index += 1) {
    hash = (hash * 31 + id.charCodeAt(index)) | 0;
  }
  return TONES[Math.abs(hash) % TONES.length] ?? "blue";
}

export interface ShareCopyWords {
  readonly owner: string;
  readonly team: (count: number) => string;
  readonly guestOnly: string;
  readonly teamName: string;
  readonly guests: (count: number) => string;
}

/**
 * The rows of "Permissions on this Board": the Board's members (the owner
 * first, with the level their role gives), then the teams and the guests the
 * access read lists. The access read has no level per entry, so a team or a
 * guest row has none.
 */
export function buildShareRows(
  members: readonly BoardMember[],
  access: BoardAccess | null,
  meId: string | null,
  words: ShareCopyWords,
  initialsOf: (name: string) => string,
): ShareRowView[] {
  const rows: ShareRowView[] = [];
  const seen = new Set<string>();
  const ordered = [...members].sort(
    (a, b) => Number(b.role === "owner") - Number(a.role === "owner"),
  );
  for (const member of ordered) {
    seen.add(member.userId);
    rows.push({
      id: member.userId,
      kind: "person",
      name: member.name,
      person: {
        id: member.userId,
        name: member.name,
        initials: member.initials || initialsOf(member.name),
        tone: shareToneFor(member.userId),
      },
      isYou: member.userId === meId,
      detail: member.role === "owner" ? words.owner : null,
      level: levelOfBoardRole(member.role),
      fixed: member.role === "owner",
      badge: null,
    });
  }
  for (const source of access?.sources ?? []) {
    if (source.kind === "team" && source.name) {
      rows.push({
        id: source.id ?? source.name,
        kind: "team",
        name: source.name,
        person: null,
        isYou: false,
        detail: words.team(source.count),
        level: null,
        fixed: false,
        badge: null,
      });
    } else if (source.kind === "user" && source.name) {
      if (source.id && seen.has(source.id)) continue;
      rows.push({
        id: source.id ?? source.name,
        kind: "person",
        name: source.name,
        person: {
          id: source.id ?? source.name,
          name: source.name,
          initials: initialsOf(source.name),
          tone: shareToneFor(source.id ?? source.name),
        },
        isYou: source.id === meId,
        detail: null,
        level: null,
        fixed: false,
        badge: null,
      });
    } else if (source.kind === "guest") {
      rows.push({
        id: source.id ?? "guests",
        kind: "guest",
        name: source.name ?? words.guests(source.count),
        person: source.name
          ? {
              id: source.id ?? source.name,
              name: source.name,
              initials: initialsOf(source.name),
              tone: shareToneFor(source.id ?? source.name),
            }
          : null,
        isYou: false,
        detail: words.guestOnly,
        level: null,
        fixed: false,
        badge: "guest",
      });
    }
  }
  return rows;
}

/** Board sharing as the producer reads it (`WorkManagement.Boards.GetSharing`). */
export type SharingLevel = "viewer" | "commenter" | "editor" | "manager";

/** How an entry holds its level: an explicit grant, a Board membership, or both. */
export type SharingSource = "grant" | "member" | "both";

/** The Board roles a person's membership can carry. */
export type BoardMemberRoleName =
  "Owner" | "Admin" | "Member" | "Observer" | "Guest";

export interface BoardSharingData {
  /** `Private`, `Workspace` or `PublicLink`, as the producer names it. */
  readonly visibility: string;
  readonly spaceName: string | null;
  readonly spacePeople: number | null;
  readonly canManage: boolean;
  /** Only a Board manager or member is given the entries; others get the counts. */
  readonly entriesDisclosed: boolean;
  readonly peopleCount: number;
  readonly guestCount: number;
  readonly entries: readonly {
    /** The person's user id or the Team's id: the key of the Board member writes. */
    readonly id: string;
    readonly kind: "person" | "team" | "guest";
    readonly name: string;
    readonly email: string | null;
    readonly you: boolean;
    readonly owner: boolean;
    /** A Guest is never above Editor; see `levelCapped`. */
    readonly level: SharingLevel;
    readonly levelCapped: boolean;
    readonly source: SharingSource;
    /** The explicit grant to revoke, when there is one. */
    readonly permissionId: string | null;
    /** That grant's version: a change of its level names it. */
    readonly permissionVersion?: number | null;
    readonly memberRole: BoardMemberRoleName | null;
    readonly spaceRole: "Admin" | "Member" | "Viewer" | null;
    readonly teamPeople: number | null;
    readonly outsideSpace: boolean;
  }[];
  readonly log: readonly {
    readonly id: string;
    readonly kind: "added" | "changed" | "removed" | "private" | "visibility";
    readonly actor: { readonly id: string; readonly name: string } | null;
    readonly target: string | null;
    readonly from: SharingLevel | null;
    readonly to: SharingLevel | null;
    readonly guest: boolean;
    readonly at: string;
  }[];
  /** Only a Board manager is given the impact of going Private. */
  readonly privateImpact: {
    readonly lose: number;
    readonly people: readonly {
      readonly userId: string;
      readonly name: string;
      readonly items: number;
      readonly overdue: number;
    }[];
    readonly keep: readonly string[];
  } | null;
}

/** The producer's sharing read, every field optional as the contract draws it. */
export interface BoardSharingWire {
  readonly canManage?: boolean;
  readonly entriesDisclosed?: boolean;
  readonly visibility?: string | null;
  readonly spaceName?: string | null;
  readonly spacePeople?: number | null;
  readonly peopleCount?: number;
  readonly guestCount?: number;
  readonly entries?:
    | readonly {
        readonly id?: string;
        readonly kind?: string | null;
        readonly name?: string | null;
        readonly email?: string | null;
        readonly you?: boolean;
        readonly owner?: boolean;
        readonly level?: string | null;
        readonly levelCapped?: boolean;
        readonly source?: string | null;
        readonly permissionId?: string | null;
        readonly permissionVersion?: number | null;
        readonly memberRole?: string | null;
        readonly spaceRole?: string | null;
        readonly teamPeople?: number | null;
        readonly outsideSpace?: boolean;
      }[]
    | null;
  readonly log?:
    | readonly {
        readonly id?: string;
        readonly kind?: string | null;
        readonly actor?: {
          readonly userId?: string;
          readonly name?: string | null;
        } | null;
        readonly target?: string | null;
        readonly from?: string | null;
        readonly to?: string | null;
        readonly guest?: boolean;
        readonly at?: string;
      }[]
    | null;
  readonly privateImpact?: {
    readonly lose?: number;
    readonly people?:
      | readonly {
          readonly userId?: string;
          readonly name?: string | null;
          readonly items?: number;
          readonly overdue?: number;
        }[]
      | null;
    readonly keep?: readonly string[] | null;
  } | null;
}

const SHARING_LEVELS: readonly SharingLevel[] = [
  "viewer",
  "commenter",
  "editor",
  "manager",
];
const MEMBER_ROLES: readonly BoardMemberRoleName[] = [
  "Owner",
  "Admin",
  "Member",
  "Observer",
  "Guest",
];
const SPACE_ROLES = ["Admin", "Member", "Viewer"] as const;

function levelFrom(value: string | null | undefined): SharingLevel {
  const key = (value ?? "").toLowerCase();
  return SHARING_LEVELS.find((level) => level === key) ?? "viewer";
}

function levelOrNull(value: string | null | undefined): SharingLevel | null {
  return value ? levelFrom(value) : null;
}

/** The one place the producer's sharing read becomes the dialog's model. */
export function toBoardSharingData(wire: BoardSharingWire): BoardSharingData {
  return {
    visibility: wire.visibility ?? "Workspace",
    spaceName: wire.spaceName ?? null,
    spacePeople: wire.spacePeople ?? null,
    canManage: wire.canManage ?? false,
    entriesDisclosed: wire.entriesDisclosed ?? false,
    peopleCount: wire.peopleCount ?? 0,
    guestCount: wire.guestCount ?? 0,
    entries: (wire.entries ?? []).map((entry) => {
      const kind = (entry.kind ?? "").toLowerCase();
      const source = (entry.source ?? "").toLowerCase();
      return {
        id: entry.id ?? "",
        kind: kind === "team" ? "team" : kind === "guest" ? "guest" : "person",
        name: entry.name ?? "",
        email: entry.email ?? null,
        you: entry.you ?? false,
        owner: entry.owner ?? false,
        level: levelFrom(entry.level),
        levelCapped: entry.levelCapped ?? false,
        source: source === "member" || source === "both" ? source : "grant",
        permissionId: entry.permissionId ?? null,
        permissionVersion: entry.permissionVersion ?? null,
        memberRole:
          MEMBER_ROLES.find((role) => role === entry.memberRole) ?? null,
        spaceRole: SPACE_ROLES.find((role) => role === entry.spaceRole) ?? null,
        teamPeople: entry.teamPeople ?? null,
        outsideSpace: entry.outsideSpace ?? false,
      };
    }),
    log: (wire.log ?? []).map((entry) => {
      const kind = (entry.kind ?? "").toLowerCase();
      return {
        id: entry.id ?? "",
        kind:
          kind === "added" ||
          kind === "changed" ||
          kind === "removed" ||
          kind === "private"
            ? kind
            : "visibility",
        actor: entry.actor
          ? { id: entry.actor.userId ?? "", name: entry.actor.name ?? "" }
          : null,
        target: entry.target ?? null,
        from: levelOrNull(entry.from),
        to: levelOrNull(entry.to),
        guest: entry.guest ?? false,
        at: entry.at ?? "",
      };
    }),
    privateImpact: wire.privateImpact
      ? {
          lose: wire.privateImpact.lose ?? 0,
          people: (wire.privateImpact.people ?? []).map((person) => ({
            userId: person.userId ?? "",
            name: person.name ?? "",
            items: person.items ?? 0,
            overdue: person.overdue ?? 0,
          })),
          keep: wire.privateImpact.keep ?? [],
        }
      : null,
  };
}

/** One call the producer's existing operations answer a sharing change with. */
export type SharingWrite =
  | {
      readonly kind: "grant";
      readonly subjectType: "User" | "Team";
      readonly subjectId: string;
      readonly level: SharingLevel;
      /** The version of the grant being changed; absent for a new grant. */
      readonly expectedVersion?: number | null;
    }
  | { readonly kind: "revoke"; readonly permissionId: string }
  | {
      readonly kind: "member-role";
      readonly userId: string;
      readonly role: BoardMemberRoleName;
    }
  | { readonly kind: "member-remove"; readonly userId: string };

type SharingEntry = BoardSharingData["entries"][number];

/** The Board role that gives a level (a role has no Can comment: that is a grant). */
const ROLE_OF_LEVEL: Record<SharingLevel, BoardMemberRoleName> = {
  manager: "Admin",
  editor: "Member",
  commenter: "Observer",
  viewer: "Observer",
};

/** Board roles a Member write may set; Owner and Guest are never written. */
const WRITABLE_ROLES: readonly BoardMemberRoleName[] = [
  "Admin",
  "Member",
  "Observer",
];

/**
 * The writes that give an entry a new level. A Team and a person who holds the
 * level only by grant are changed by grant; a Board member's role changes with
 * the member operation, and a grant too when the entry has one or the level is
 * Can comment, which no Board role gives. The owner and the caller are fixed.
 */
export function planLevelChange(
  entry: SharingEntry,
  level: SharingLevel,
): readonly SharingWrite[] {
  if (entry.owner || entry.you) return [];
  const grant: SharingWrite = {
    kind: "grant",
    subjectType: entry.kind === "team" ? "Team" : "User",
    subjectId: entry.id,
    level,
    expectedVersion: entry.permissionVersion ?? null,
  };
  const role = entry.memberRole;
  if (
    entry.kind === "person" &&
    role !== null &&
    WRITABLE_ROLES.includes(role)
  ) {
    const writes: SharingWrite[] = [];
    const next = ROLE_OF_LEVEL[level];
    if (next !== role)
      writes.push({ kind: "member-role", userId: entry.id, role: next });
    if (entry.permissionId !== null || level === "commenter")
      writes.push(grant);
    return writes;
  }
  return [grant];
}

/** The writes that take an entry's access away: its grant and its membership. */
export function planRemoval(entry: SharingEntry): readonly SharingWrite[] {
  if (entry.owner || entry.you) return [];
  const writes: SharingWrite[] = [];
  if (entry.permissionId !== null)
    writes.push({ kind: "revoke", permissionId: entry.permissionId });
  if (
    entry.kind !== "team" &&
    entry.memberRole !== null &&
    entry.memberRole !== "Owner"
  )
    writes.push({ kind: "member-remove", userId: entry.id });
  return writes;
}

const LEVEL_OF_SHARING: Record<SharingLevel, ShareLevel> = {
  manager: "manage",
  editor: "edit",
  commenter: "comment",
  viewer: "view",
};

export function shareLevelOf(level: SharingLevel): ShareLevel {
  return LEVEL_OF_SHARING[level];
}

export const SHARING_LEVEL_OF: Record<ShareLevel, SharingLevel> = {
  manage: "manager",
  edit: "editor",
  comment: "commenter",
  view: "viewer",
};

export interface SharingWords {
  readonly owner: string;
  readonly team: (count: number) => string;
  readonly guestOnly: string;
  readonly inSpace: (role: "Admin" | "Member" | "Viewer") => string;
  readonly outsideSpace: string | null;
  /** Said of a Guest whose stored level is above Can edit. */
  readonly levelCapped: string;
}

/**
 * The rows of "Permissions on this Board" from the producer's own entries only
 * (what the department gives by role is not listed): the person's place in the
 * department or outside it, and the level. Nothing on a Board expires.
 */
export function buildSharingRows(
  data: BoardSharingData,
  words: SharingWords,
  initialsOf: (name: string) => string,
): ShareRowView[] {
  return data.entries.map((entry) => {
    const outside = entry.outsideSpace ? words.outsideSpace : null;
    const person: SharePersonView | null =
      entry.kind === "team"
        ? null
        : {
            id: entry.id,
            name: entry.name,
            initials: initialsOf(entry.name),
            tone: shareToneFor(entry.id),
          };
    const detail =
      entry.kind === "team"
        ? [words.team(entry.teamPeople ?? 0), outside]
        : entry.kind === "guest"
          ? [
              entry.email,
              words.guestOnly,
              entry.levelCapped ? words.levelCapped : null,
            ]
          : [
              entry.owner ? words.owner : null,
              entry.spaceRole ? words.inSpace(entry.spaceRole) : null,
            ];
    return {
      id: entry.id,
      kind: entry.kind,
      name: entry.name,
      shortName: entry.name.split(/\s+/).pop(),
      person,
      isYou: entry.you,
      detail: detail.filter(Boolean).join(" · ") || null,
      level: shareLevelOf(entry.level),
      fixed: entry.owner || entry.you,
      badge: entry.kind === "guest" ? "outside" : null,
    } satisfies ShareRowView;
  });
}

export interface SharingLogWords {
  readonly levels: Record<ShareLevel, string>;
  readonly text: {
    readonly added: string;
    readonly addedGuest: string;
    readonly addedPerson: string;
    readonly changed: string;
    readonly removed: string;
    readonly madePrivate: string;
    readonly visibility: string;
  };
  readonly fill: (
    message: string,
    values: Record<string, string | number>,
  ) => string;
  /** "08/10 · 14:12" within a week, "02/10" after. */
  readonly when: (iso: string) => string;
}

export function buildSharingLog(
  data: BoardSharingData,
  words: SharingLogWords,
  initialsOf: (name: string) => string,
): ShareLogEntry[] {
  const f = words.fill;
  return data.log.map((entry) => {
    const actor = entry.actor?.name ?? "";
    const target = entry.target ?? "";
    const level = (value: SharingLevel | null) =>
      value ? words.levels[shareLevelOf(value)] : "";
    let text: string;
    switch (entry.kind) {
      case "added":
        text = f(words.text.added, {
          actor,
          target,
          extra: entry.guest
            ? f(words.text.addedGuest, { level: level(entry.to) })
            : f(words.text.addedPerson, { level: level(entry.to) }),
        });
        break;
      case "changed":
        text = f(words.text.changed, {
          actor,
          target,
          from: level(entry.from),
          to: level(entry.to),
        });
        break;
      case "removed":
        text = f(words.text.removed, { actor, target });
        break;
      case "private":
        text = f(words.text.madePrivate, { actor });
        break;
      default:
        text = f(words.text.visibility, { actor });
    }
    return {
      id: entry.id,
      actor: entry.actor
        ? {
            id: entry.actor.id,
            name: entry.actor.name,
            initials: initialsOf(entry.actor.name),
            tone: shareToneFor(entry.actor.id),
          }
        : null,
      text,
      when: words.when(entry.at),
      note: null,
    };
  });
}

/** Who loses the Board when it goes Private, and the entries that stay. */
export function buildSharingImpact(
  data: BoardSharingData,
  words: {
    readonly keepInSpace: (names: string) => string;
    readonly keepGuest: (name: string) => string;
    readonly and: string;
  },
  initialsOf: (name: string) => string,
): {
  readonly lose: number;
  readonly people: readonly ShareImpactPerson[];
  readonly keepText: string;
} | null {
  if (!data.privateImpact) return null;
  const inSpace = data.entries.filter((entry) => entry.spaceRole !== null);
  const others = data.entries.filter((entry) => entry.spaceRole === null);
  const parts: string[] = [];
  if (inSpace.length > 0)
    parts.push(
      words.keepInSpace(inSpace.map((entry) => entry.name).join(", ")),
    );
  for (const entry of others.filter((item) => item.kind !== "guest"))
    parts.push(`**${entry.name}**`);
  const guests = others.filter((entry) => entry.kind === "guest");
  const guestParts = guests.map((entry) => words.keepGuest(entry.name));
  const all = [...parts, ...guestParts];
  const keepText =
    all.length > 1
      ? `${all.slice(0, -1).join(", ")}${words.and}${all[all.length - 1]}.`
      : `${all[0] ?? ""}.`;
  return {
    lose: data.privateImpact.lose,
    people: data.privateImpact.people.map((person) => ({
      id: person.userId,
      name: person.name,
      initials: initialsOf(person.name),
      tone: shareToneFor(person.userId),
      items: person.items,
      overdue: person.overdue,
    })),
    keepText,
  };
}
