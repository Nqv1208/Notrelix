import type { ChatLinkKind, ChatLinkRef } from "../types/chat";

/** The most links of one message that show a preview; the rest stay plain text. */
export const CHAT_LINK_PREVIEWS_MAX = 3;

/** A product link found in a message's text. */
export interface ChatProductLink {
  /** The text of the link as the author wrote it. */
  readonly text: string;
  /** Where it leads: the path and query only (same origin), never another site. */
  readonly href: string;
  readonly kind: ChatLinkKind;
  readonly resourceId: string;
  /** What names it to the resolver (the same key for the same resource). */
  readonly ref: ChatLinkRef;
}

/** An id is a token of letters, digits and dashes (the backend's are GUIDs; the mock's are shorter words). */
const GUID = "[\\w-]{1,64}";
const LINK_PATTERN = new RegExp(
  `(?:https?://[^\\s/<>"')]{1,255})?/workspaces/(${GUID})/(docs|boards)/(${GUID})(\\?[^\\s<>"')]{0,512})?`,
  "g",
);

/** A trailing sentence mark is not part of the link. */
function trimTrail(text: string): string {
  // A scan from the end, not a regex: `[..]+$` backtracks quadratically on a long run of marks.
  let end = text.length;
  while (end > 0 && ".,;:!?".includes(text.charAt(end - 1))) end -= 1;
  return text.slice(0, end);
}

function itemOf(search: string | undefined): string | null {
  if (!search) return null;
  const value = new URLSearchParams(search).get("item");
  return value && new RegExp(`^${GUID}$`).test(value) ? value : null;
}

/**
 * The Board item and Page links of one Workspace in a message's text (UC-CHT-G05, CHT-023): `/workspaces/{id}/docs/{id}` and
 * `/workspaces/{id}/boards/{id}?item={id}`, relative or on any host. A link of another Workspace, a Board without an item or any
 * other address is not a product link here. Each resource appears once, in reading order, up to `max`. Pure: the preview of
 * each is resolved by the server with the reader's own access.
 */
export function findProductLinks(
  content: string,
  workspaceId: string,
  max: number = CHAT_LINK_PREVIEWS_MAX,
): ChatProductLink[] {
  const found: ChatProductLink[] = [];
  const seen = new Set<string>();
  for (const match of content.matchAll(LINK_PATTERN)) {
    if (found.length >= max) break;
    const text = trimTrail(match[0]);
    const [, inWorkspace, route, routeId, search] = match;
    if (!inWorkspace || inWorkspace.toLowerCase() !== workspaceId.toLowerCase())
      continue;
    const kind: ChatLinkKind | null =
      route === "docs"
        ? "documents.page"
        : itemOf(search)
          ? "work-management.board-item"
          : null;
    const resourceId = route === "docs" ? routeId : itemOf(search);
    if (!kind || !resourceId) continue;
    const key = `${kind}:${resourceId.toLowerCase()}`;
    if (seen.has(key)) continue;
    seen.add(key);
    const at = text.indexOf("/workspaces/");
    found.push({
      text,
      href: text.slice(at),
      kind,
      resourceId,
      ref: { kind, resourceId },
    });
  }
  return found;
}

/** The query-key part that names a link to resolve. */
export function linkRefKey(ref: ChatLinkRef): string {
  return "url" in ref ? `url:${ref.url}` : `${ref.kind}:${ref.resourceId}`;
}

/** The kinds a channel can be linked to by the app: a Board and a Page (a Team has no visibility fact yet, so it is never offered). */
export const CHAT_LINKABLE_KINDS = [
  "work-management.board",
  "documents.page",
] as const satisfies readonly ChatLinkKind[];

export type ChatLinkableKind = (typeof CHAT_LINKABLE_KINDS)[number];

export function isLinkableKind(kind: ChatLinkKind): kind is ChatLinkableKind {
  return (CHAT_LINKABLE_KINDS as readonly string[]).includes(kind);
}

const BARE_ID = new RegExp(`^${GUID}$`);

/**
 * What a manager pasted into the "Link resource" dialog, as a reference the resolver takes: a product address of this Workspace
 * (`/workspaces/{id}/boards/{id}` or `/docs/{id}`, relative or on any host; a Board address with an item still names the Board)
 * or a bare id with the kind chosen by the manager. Null when it is neither (another Workspace, another site, an unknown route).
 * Pure: the server resolves what the person may see.
 */
export function parseLinkInput(
  input: string,
  workspaceId: string,
  fallbackKind: ChatLinkableKind,
): { readonly kind: ChatLinkableKind; readonly resourceId: string } | null {
  const text = input.trim();
  if (text.length === 0 || text.length > 2048) return null;
  if (BARE_ID.test(text)) return { kind: fallbackKind, resourceId: text };
  const pattern = new RegExp(
    `^(?:https?://[^\\s/<>"')]{1,255})?/workspaces/(${GUID})/(docs|boards)/(${GUID})(?:[?#][^\\s<>"')]{0,512})?$`,
  );
  const match = pattern.exec(text);
  if (!match) return null;
  const [, inWorkspace, route, resourceId] = match;
  if (
    !inWorkspace ||
    !resourceId ||
    inWorkspace.toLowerCase() !== workspaceId.toLowerCase()
  ) {
    return null;
  }
  return {
    kind: route === "docs" ? "documents.page" : "work-management.board",
    resourceId,
  };
}

/** Where a linked resource opens in the product (the same-origin route), or null for a kind with no route here. */
export function resourceHref(
  kind: ChatLinkKind,
  workspaceId: string,
  resourceId: string,
): string | null {
  const ws = encodeURIComponent(workspaceId);
  const id = encodeURIComponent(resourceId);
  if (kind === "documents.page") return `/workspaces/${ws}/docs/${id}`;
  if (kind === "work-management.board") return `/workspaces/${ws}/boards/${id}`;
  return null;
}
