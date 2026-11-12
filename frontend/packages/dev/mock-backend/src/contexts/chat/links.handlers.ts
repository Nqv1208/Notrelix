/**
 * The preview of a link to a Board item or a Page (UC-CHT-G05, CHT-023), as `Chat.Links.Resolve` serves it. The title is returned only
 * when the signed-in person can view the resource now: an Item on a Board they can see, a Page that is not in the trash and whose Space
 * they can see. Every other case (hidden, trashed, gone) is the same generic answer, `accessible: false` with no title. A product URL of
 * this Workspace or a ResourceRef names the link; any other URL is a 400. Resolving reads only: it never changes the resource.
 */

import { defineMockOperation } from "../../operations/types";
import { ok } from "../../transport/create-response";
import type { MockStore } from "../../state/mock-store";
import {
  callerIn,
  guarded,
  requireAction,
  validationProblem,
} from "./chat-access";

type WorkspaceParams = { workspaceId: string };

export const LINK_KIND = {
  boardItem: "work-management.board-item",
  page: "documents.page",
} as const;

/** The longest URL the resolver takes (ChatLinkTargets.MaxUrlLength). */
export const LINK_URL_MAX = 2048;

/** The mock's ids are not all GUIDs (the backend's are), so an id here is any short token of letters, digits and dashes. */
const GUID = /^[\w-]{1,64}$/;

type LinkTarget = { kind: string; id: string };

function isSupportedKind(kind: string | undefined): boolean {
  return kind === LINK_KIND.boardItem || kind === LINK_KIND.page;
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
  const page = store.getPage(target.id);
  if (!page || page.workspaceId !== workspaceId || page.trashedAt) return null;
  return spaceVisible(page.spaceId) ? page.title : null;
}

export const linksOperations = [
  defineMockOperation<WorkspaceParams>({
    id: "chat.links.resolve",
    contract: { kind: "openapi", operationId: "Chat.Links.Resolve" },
    method: "GET",
    route: "/workspaces/:workspaceId/chat/links/resolve",
    async handle({ params, query, store }) {
      return guarded(async () => {
        const caller = callerIn(store, params.workspaceId);
        requireAction(caller, "viewWorkspace");
        const url = query["url"];
        const kind = query["kind"];
        const resourceId = query["resourceId"];
        const named = kind !== undefined || resourceId !== undefined;
        if ((url !== undefined) === named) {
          validationProblem("Send either a url or a kind with a resourceId.");
        }
        let target: LinkTarget | null;
        if (url !== undefined) {
          if (url.length > LINK_URL_MAX) {
            validationProblem(
              `The url must be at most ${LINK_URL_MAX} characters.`,
            );
          }
          target = targetOfUrl(url, params.workspaceId);
        } else {
          if (!isSupportedKind(kind)) {
            validationProblem(
              "The kind must be work-management.board-item or documents.page.",
            );
          }
          if (!resourceId || !GUID.test(resourceId)) {
            validationProblem("The resourceId must be an id.");
          }
          target = { kind: kind!, id: resourceId! };
        }
        if (!target) {
          validationProblem(
            "This link is not a Board item or Page of this Workspace.",
          );
        }
        const title = viewableTitle(store, params.workspaceId, target);
        return ok({
          kind: target.kind,
          resourceId: target.id,
          accessible: title !== null,
          title,
        });
      });
    },
  }),
];
