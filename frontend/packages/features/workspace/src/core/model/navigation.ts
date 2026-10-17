/**
 * Workspace navigation model (WX-SHELL-001/002, WX-DEC-011).
 *
 * Pure and framework-neutral. Recents and pins are per-member references
 * (`kind` + `resourceId`) owned by the Workspaces context; display names are
 * joined on the client from the authorized Board and Doc lists, and a
 * reference without a match is dropped so a resource the member can no longer
 * view never leaks its name.
 */

export const NAVIGATION_RESOURCE_KIND = {
  board: "work-management.board",
  doc: "documents.page",
} as const;

export type NavigationResourceKind =
  (typeof NAVIGATION_RESOURCE_KIND)[keyof typeof NAVIGATION_RESOURCE_KIND];

const KINDS: ReadonlySet<string> = new Set(
  Object.values(NAVIGATION_RESOURCE_KIND),
);

export function isNavigationResourceKind(
  value: unknown,
): value is NavigationResourceKind {
  return typeof value === "string" && KINDS.has(value);
}

/** One stored reference: when it was last opened (recents) or pinned (pins). */
export interface NavigationReference {
  readonly kind: NavigationResourceKind;
  readonly resourceId: string;
  readonly at: string;
}

/** A Board or Doc the member is authorized to see, from its owning list. */
export interface NavigationResourceSummary {
  readonly id: string;
  readonly name: string;
}

export interface NavigationResourceCatalog {
  readonly boards: readonly NavigationResourceSummary[];
  readonly docs: readonly NavigationResourceSummary[];
}

/** A resolved, displayable reference. */
export interface NavigationEntry {
  readonly kind: NavigationResourceKind;
  readonly resourceId: string;
  readonly name: string;
  readonly at: string;
  readonly pinned: boolean;
}

export function navigationEntryKey(
  kind: NavigationResourceKind,
  resourceId: string,
): string {
  return `${kind}:${resourceId}`;
}

/**
 * Joins references with the authorized catalog. Keeps the reference order,
 * drops references without a match and duplicate references.
 */
export function resolveNavigationEntries(
  references: readonly NavigationReference[],
  catalog: NavigationResourceCatalog,
  pinned: readonly NavigationReference[] = [],
): NavigationEntry[] {
  const names = new Map<string, string>();
  for (const board of catalog.boards) {
    names.set(
      navigationEntryKey(NAVIGATION_RESOURCE_KIND.board, board.id),
      board.name,
    );
  }
  for (const doc of catalog.docs) {
    names.set(
      navigationEntryKey(NAVIGATION_RESOURCE_KIND.doc, doc.id),
      doc.name,
    );
  }
  const pinnedKeys = new Set(
    pinned.map((reference) =>
      navigationEntryKey(reference.kind, reference.resourceId),
    ),
  );

  const seen = new Set<string>();
  const entries: NavigationEntry[] = [];
  for (const reference of references) {
    const key = navigationEntryKey(reference.kind, reference.resourceId);
    const name = names.get(key);
    if (name === undefined || seen.has(key)) continue;
    seen.add(key);
    entries.push({
      kind: reference.kind,
      resourceId: reference.resourceId,
      name,
      at: reference.at,
      pinned: pinnedKeys.has(key),
    });
  }
  return entries;
}

/**
 * Product-hierarchy destinations of the workspace shell (DESIGN.md §27).
 * Hosts map each destination to their own route.
 */
export type WorkspaceDestination =
  | { readonly section: "home" }
  | { readonly section: "my-work" }
  | { readonly section: "inbox" }
  | { readonly section: "search" }
  | { readonly section: "library" }
  | { readonly section: "reports" }
  | { readonly section: "members"; readonly tab?: "members" | "invitations" }
  | {
      readonly section: "teams";
      readonly teamId?: string;
      readonly filter?: "active" | "archived";
    }
  | { readonly section: "settings" }
  | { readonly section: "billing" }
  /** Sharing and security of "Thiết lập workspace". */
  | { readonly section: "security" }
  | { readonly section: "account"; readonly page?: "profile" | "appearance" }
  | {
      readonly section: "board";
      readonly boardId: string;
      readonly view?: string;
      /** An Item to open on the Board (route search `item`). */
      readonly item?: string;
    }
  | { readonly section: "doc"; readonly docId: string }
  | {
      readonly section: "space";
      readonly spaceId: string;
      readonly tab?: SpaceTab;
    };

/** Sections of a Space page (OT-5). */
export type SpaceTab = "overview" | "members" | "settings";

export type WorkspaceSection = WorkspaceDestination["section"];

export function destinationForEntry(
  entry: Pick<NavigationEntry, "kind" | "resourceId">,
): WorkspaceDestination {
  return entry.kind === NAVIGATION_RESOURCE_KIND.board
    ? { section: "board", boardId: entry.resourceId }
    : { section: "doc", docId: entry.resourceId };
}

/** Whether `destination` is the current location (resource-level match). */
export function isDestinationActive(
  destination: WorkspaceDestination,
  location: WorkspaceDestination | null,
): boolean {
  if (!location || destination.section !== location.section) return false;
  if (destination.section === "board" && location.section === "board") {
    return destination.boardId === location.boardId;
  }
  if (destination.section === "doc" && location.section === "doc") {
    return destination.docId === location.docId;
  }
  if (destination.section === "space" && location.section === "space") {
    return destination.spaceId === location.spaceId;
  }
  return true;
}

/** Identity palette from the logo (WX-DEC-006): identity only, never status. */
export const IDENTITY_TONES = [
  "blue",
  "coral",
  "gold",
  "sage",
  "ember",
] as const;
export type IdentityTone = (typeof IDENTITY_TONES)[number];

function toneHash(id: string): number {
  let hash = 0;
  for (let index = 0; index < id.length; index += 1) {
    hash = (hash * 31 + id.charCodeAt(index)) | 0;
  }
  return Math.abs(hash);
}

/** Deterministic identity tone for a member or resource id. */
export function identityToneFor(id: string): IdentityTone {
  return IDENTITY_TONES[toneHash(id) % IDENTITY_TONES.length] ?? "blue";
}

/**
 * Mark tones for workspace and department (Space) initials chips
 * (WX-DEC-014; mirrors `MARK_TONE_IDS` in `@notrelix/ui-tokens`). Never
 * status colours (WX-DEC-006).
 */
export const MARK_TONE_IDS = [
  "blue",
  "gold",
  "coral",
  "sage",
  "crimson",
  "violet",
] as const;
export type MarkToneId = (typeof MARK_TONE_IDS)[number];

/**
 * Deterministic mark tone for a workspace or Space id (same hash as
 * {@link identityToneFor}, modulo six). Until Spaces store a chosen colour
 * (gap register), the tone is derived from the id.
 */
export function markToneFor(id: string): MarkToneId {
  return (
    chosenMarkTones.get(id) ??
    MARK_TONE_IDS[toneHash(id) % MARK_TONE_IDS.length] ??
    "blue"
  );
}

/**
 * Colours the producer sent for a workspace or Space (gap field `color`,
 * CTR-GAP-WS-COLOR). TRANSITION: the generated contract has no colour, so the
 * API parsers remember what the mock (later the producer) sends here and
 * {@link markToneFor} prefers it to the id hash.
 */
const chosenMarkTones = new Map<string, MarkToneId>();

export function rememberMarkTone(id: string, value: unknown): void {
  if ((MARK_TONE_IDS as readonly unknown[]).includes(value))
    chosenMarkTones.set(id, value as MarkToneId);
}

/**
 * One or two uppercase initials of a person (or any identity mark), as the
 * design draws Vietnamese names: the initials of the given name, the last two
 * words ("Trần Minh Quân" is "MQ", "Đỗ Khánh Linh" is "KL"); a "Thu" before the
 * last word belongs to the family-given pair, so the first and last word are
 * used ("Lý Thu Trang" is "LT"). Two words are both used, one word gives one.
 */
export function initialsFor(name: string): string {
  const words = name
    .replace(/\s*\([^)]*\)\s*$/, "")
    .trim()
    .split(/\s+/)
    .filter(Boolean);
  const plainLast = (words[words.length - 1] ?? "")
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "");
  // A given name of two letters or fewer ("Hà") is drawn with both letters.
  if (words.length >= 2 && Array.from(plainLast).length <= 2)
    return plainLast.toUpperCase();
  const picked =
    words.length >= 3
      ? words[words.length - 2]
          ?.normalize("NFD")
          .replace(/[\u0300-\u036f]/g, "")
          .toLowerCase() === "thu"
        ? [words[0], words[words.length - 1]]
        : words.slice(-2)
      : words;
  const letters = picked
    .map((part) => (part ? Array.from(part)[0] : undefined))
    .filter(
      (char): char is string =>
        char !== undefined && /[\p{L}\p{N}]/u.test(char),
    );
  return letters.length === 0 ? "?" : letters.join("").toUpperCase();
}

/** Words that only describe the kind of mark ("Nhóm Hạ tầng", "Minh An Group"). */
const GENERIC_MARK_WORDS = new Set([
  "nhom",
  "ban",
  "phong",
  "team",
  "group",
  "department",
]);

/**
 * Initials of a workspace or department mark, as the design draws them: the
 * first letters of the first two words that name it, without a leading or
 * trailing kind word, and without tone marks on the vowels ("Minh An Group" is
 * "MA", "Nhóm Hạ tầng" is "HT", "Nhóm Ứng dụng" is "UD", "Ban giám đốc" is
 * "GĐ", "Đà Nẵng" is "ĐN").
 */
export function markInitialsFor(name: string): string {
  const words = name.trim().split(/\s+/).filter(Boolean);
  const plain = (word: string) => normalizeSearchText(word);
  const named =
    words.length <= 2
      ? words
      : words.filter((word, index) => {
          const generic = GENERIC_MARK_WORDS.has(plain(word));
          return !(generic && (index === 0 || index === words.length - 1));
        });
  const use = (named.length >= 2 ? named : words).slice(0, 2);
  const letters = use
    .map(
      (word) =>
        Array.from(word.normalize("NFD").replace(/[\u0300-\u036f]/g, ""))[0],
    )
    .filter(
      (char): char is string =>
        char !== undefined && /[\p{L}\p{N}]/u.test(char),
    );
  return letters.length === 0 ? "?" : letters.join("").toUpperCase();
}

/** Case- and diacritic-insensitive text for matching ("Đội" ~ "doi"). */
export function normalizeSearchText(value: string): string {
  return value
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .replace(/đ/gi, "d")
    .toLowerCase();
}
