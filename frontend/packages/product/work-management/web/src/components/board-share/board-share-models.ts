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
