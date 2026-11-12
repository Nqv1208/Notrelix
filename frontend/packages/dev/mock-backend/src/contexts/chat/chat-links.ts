/**
 * The link rules the Chat handlers share (UC-CHT-G05, CHT-023, H10): the kinds a link can name, the product address of a resource, and
 * the title the signed-in person may see. Used by the link resolver, by a channel's own link and by the reverse lookup, so all three
 * answer the same for a hidden, a trashed and a missing resource.
 */

import type { MockStore } from "../../state/mock-store";
import type { ChatSpaceRecord } from "./chat-model";
import type { MockChatState } from "./chat-state";

export const LINK_KIND = {
  boardItem: "work-management.board-item",
  board: "work-management.board",
  page: "documents.page",
  team: "governance.team",
} as const;

/** The longest URL the resolver takes (ChatLinkTargets.MaxUrlLength). */
export const LINK_URL_MAX = 2048;

/** The mock's ids are not all GUIDs (the backend's are), so an id here is any short token of letters, digits and dashes. */
export const LINK_ID = /^[\w-]{1,64}$/;
const GUID = LINK_ID;

export type LinkTarget = { kind: string; id: string };

/** The kinds the resolver previews (a Team has no preview yet). */
export function isSupportedKind(kind: string | undefined): boolean {
  return (
    kind === LINK_KIND.boardItem ||
    kind === LINK_KIND.board ||
    kind === LINK_KIND.page
  );
}

/** The kinds a channel can be linked to by the app (a Team is not linkable yet). */
export function isLinkableKind(kind: string | undefined): boolean {
  return kind === LINK_KIND.board || kind === LINK_KIND.page;
}

/** The reference a product URL of this Workspace names, or null (another Workspace, a Board without an item, another site). */
export function targetOfUrl(
  rawUrl: string,
  workspaceId: string,
): LinkTarget | null {
  const url = rawUrl.trim();
  let path: string;
  let query: string;
  if (url.startsWith("/") && !url.startsWith("//")) {
    const withoutFragment = url.split("#")[0] ?? "";
    const at = withoutFragment.indexOf("?");
    path = at >= 0 ? withoutFragment.slice(0, at) : withoutFragment;
    query = at >= 0 ? withoutFragment.slice(at) : "";
  } else {
    let parsed: URL;
    try {
      parsed = new URL(url);
    } catch {
      return null;
    }
    if (parsed.protocol !== "http:" && parsed.protocol !== "https:") {
      return null;
    }
    path = parsed.pathname;
    query = parsed.search;
  }
  const segments = path.split("/").filter((part) => part.length > 0);
  const [root, inWorkspace, route, routeId] = segments;
  if (
    segments.length !== 4 ||
    root !== "workspaces" ||
    !inWorkspace ||
    !GUID.test(inWorkspace) ||
    inWorkspace.toLowerCase() !== workspaceId.toLowerCase() ||
    !routeId ||
    !GUID.test(routeId)
  ) {
    return null;
  }
  if (route === "docs") return { kind: LINK_KIND.page, id: routeId };
  if (route === "boards") {
    const item = new URLSearchParams(query).get("item");
    return item && GUID.test(item)
      ? { kind: LINK_KIND.boardItem, id: item }
      : null;
  }
  return null;
}

/**
 * The title the signed-in person may see, or null. A private Board shows its Items to its members only; a Board or Page placed in a
 * Space follows that Space's access; a trashed Page and a trashed or missing Item have no title.
 */
export function viewableTitle(
  store: MockStore,
  workspaceId: string,
  target: LinkTarget,
): string | null {
  const viewer = store.spaceViewer(workspaceId);
  const spaceVisible = (spaceId: string | null | undefined) =>
    !spaceId || store.spaces.accessFor(spaceId, viewer) !== null;

  if (target.kind === LINK_KIND.boardItem) {
    const card = store.getCard(target.id);
    const board = card ? store.getBoard(card.boardId) : undefined;
    if (!card || !board || board.workspaceId !== workspaceId) return null;
    const member = store
      .getBoardMembers(board.id)
      .some((row) => row.userId === viewer.userId);
    if (board.visibility === "private" && !member) return null;
    return spaceVisible(board.spaceId) ? card.title : null;
  }
  if (target.kind === LINK_KIND.board) {
    const board = store.getBoard(target.id);
    if (!board || board.workspaceId !== workspaceId) return null;
    const member = store
      .getBoardMembers(board.id)
      .some((row) => row.userId === viewer.userId);
    if (board.visibility === "private" && !member) return null;
    return spaceVisible(board.spaceId) ? board.title : null;
  }
  if (target.kind !== LINK_KIND.page) return null;
  const page = store.getPage(target.id);
  if (!page || page.workspaceId !== workspaceId || page.trashedAt) return null;
  return spaceVisible(page.spaceId) ? page.title : null;
}

/**
 * ChannelLinkDto as the signed-in person sees it (backend ChatResourceViews): the title only when they can view the resource now,
 * otherwise the generic `accessible: false`; a Team (no visibility fact yet) is always generic. Null when the channel has no link.
 */
export function linkViewOf(state: MockChatState, space: ChatSpaceRecord) {
  if (!space.link) return null;
  const title = viewableTitle(state.store(), space.workspaceId, {
    kind: space.link.kind,
    id: space.link.resourceId,
  });
  return {
    kind: space.link.kind,
    resourceId: space.link.resourceId,
    accessible: title !== null,
    title,
  };
}
