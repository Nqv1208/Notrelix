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
  readonly expires: string | null;
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
      expires: null,
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
        expires: null,
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
        expires: null,
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
        expires: null,
      });
    }
  }
  return rows;
}

/** The producer's own-permissions read (gap `CTR-GAP-WM-BOARD-SHARING`). */
export type SharingLevel = "viewer" | "commenter" | "editor" | "manager";

export interface BoardSharingData {
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
    readonly spaceRole: "Admin" | "Member" | "Viewer" | null;
    readonly department: string | null;
    readonly teamPeople: number | null;
    readonly outsideSpace: boolean;
    readonly outsideCompany: boolean;
    readonly level: SharingLevel;
    readonly expiresAt: string | null;
  }[];
  readonly log: readonly {
    readonly id: string;
    readonly kind:
      | "added"
      | "changed"
      | "removed"
      | "expired"
      | "public-off"
      | "created"
      | "private";
    readonly actor: { readonly id: string; readonly name: string } | null;
    readonly target: string | null;
    readonly from: SharingLevel | null;
    readonly to: SharingLevel | null;
    readonly guest: boolean;
    readonly expiresAt: string | null;
    readonly at: string;
  }[];
  readonly privateImpact: {
    readonly lose: number;
    readonly people: readonly {
      readonly userId: string;
      readonly name: string;
      readonly items: number;
      readonly overdue: number;
    }[];
    readonly keep: readonly string[];
  };
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
  readonly department: (name: string) => string;
  readonly outsideSpace: string | null;
  readonly formatDay: (iso: string) => string;
}

/** "31/01/2027" of an end-of-day instant in the member's calendar. */
export function shareEndDate(iso: string): string {
  const date = new Date(iso);
  const pad = (value: number) => String(value).padStart(2, "0");
  return `${pad(date.getDate())}/${pad(date.getMonth() + 1)}/${date.getFullYear()}`;
}

/**
 * The rows of "Permissions on this Board" from the producer's own entries only
 * (what the department gives by role is not listed): the person's place in the
 * department or outside it, the level and when it ends.
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
            id: entry.userId ?? entry.id,
            name: entry.name,
            initials: initialsOf(entry.name),
            tone: shareToneFor(entry.userId ?? entry.id),
          };
    const detail =
      entry.kind === "team"
        ? [words.team(entry.teamPeople ?? 0), outside]
        : entry.kind === "guest"
          ? [entry.email, words.guestOnly]
          : [
              entry.owner ? words.owner : null,
              entry.spaceRole ? words.inSpace(entry.spaceRole) : null,
              entry.department ? words.department(entry.department) : null,
              entry.department ? outside : null,
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
      badge: entry.outsideCompany ? "outside" : null,
      expires: entry.expiresAt ? shareEndDate(entry.expiresAt) : null,
    } satisfies ShareRowView;
  });
}

export interface SharingLogWords {
  readonly levels: Record<ShareLevel, string>;
  readonly text: {
    readonly added: string;
    readonly addedGuest: string;
    readonly addedGuestForever: string;
    readonly addedPerson: string;
    readonly changed: string;
    readonly removed: string;
    readonly expired: string;
    readonly publicOff: string;
    readonly created: string;
    readonly madePrivate: string;
  };
  readonly expiredNote: string;
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
            ? entry.expiresAt
              ? f(words.text.addedGuest, {
                  level: level(entry.to),
                  date: shareEndDate(entry.expiresAt),
                })
              : f(words.text.addedGuestForever, { level: level(entry.to) })
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
      case "expired":
        text = f(words.text.expired, { target });
        break;
      case "public-off":
        text = words.text.publicOff;
        break;
      case "private":
        text = f(words.text.madePrivate, { actor });
        break;
      default:
        text = f(words.text.created, { actor });
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
      note: entry.kind === "expired" ? words.expiredNote : null,
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
} {
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
