import type { WorkspaceDestination } from "@notrelix/features-workspace/core";

/**
 * Web host mapping between the shell's product-hierarchy destinations and
 * this app's routes. The shell never builds or parses URLs itself.
 */

export interface RouterTarget {
  readonly to: string;
  readonly params: Record<string, string>;
  readonly search?: Record<string, string>;
}

export function routerTarget(
  workspaceId: string,
  destination: WorkspaceDestination,
): RouterTarget {
  const params = { workspaceId };
  switch (destination.section) {
    case "home":
      return { to: "/workspaces/$workspaceId", params };
    case "my-work":
      return { to: "/workspaces/$workspaceId/my-work", params };
    case "inbox":
      return { to: "/workspaces/$workspaceId/inbox", params };
    case "search":
      return { to: "/workspaces/$workspaceId/search", params };
    case "library":
      return { to: "/workspaces/$workspaceId/library", params };
    case "reports":
      return { to: "/workspaces/$workspaceId/reports", params };
    case "members":
      return {
        to: "/workspaces/$workspaceId/members",
        params,
        search:
          destination.tab === "invitations"
            ? { tab: "invitations" }
            : undefined,
      };
    case "teams":
      return destination.teamId
        ? {
            to: "/workspaces/$workspaceId/teams/$teamId",
            params: { ...params, teamId: destination.teamId },
          }
        : {
            to: "/workspaces/$workspaceId/teams",
            params,
            search:
              destination.filter === "archived"
                ? { filter: "archived" }
                : undefined,
          };
    case "settings":
      return { to: "/workspaces/$workspaceId/settings", params };
    case "billing":
      return { to: "/workspaces/$workspaceId/billing", params };
    case "security":
      return { to: "/workspaces/$workspaceId/security", params };
    case "account":
      return destination.page === "appearance"
        ? { to: "/workspaces/$workspaceId/account/appearance", params }
        : { to: "/workspaces/$workspaceId/account/profile", params };
    case "board":
      return {
        to: "/workspaces/$workspaceId/boards/$boardId",
        params: { ...params, boardId: destination.boardId },
        search:
          destination.view || destination.item
            ? {
                ...(destination.view ? { view: destination.view } : {}),
                ...(destination.item ? { item: destination.item } : {}),
              }
            : undefined,
      };
    case "doc":
      return {
        to: "/workspaces/$workspaceId/docs/$docId",
        params: { ...params, docId: destination.docId },
      };
    case "space":
      return {
        to: "/workspaces/$workspaceId/spaces/$spaceId",
        params: { ...params, spaceId: destination.spaceId },
        search:
          destination.tab && destination.tab !== "overview"
            ? { tab: destination.tab }
            : undefined,
      };
  }
}

/** Builds the href for a destination from the same route templates. */
export function destinationHref(
  workspaceId: string,
  destination: WorkspaceDestination,
): string {
  const target = routerTarget(workspaceId, destination);
  let path = target.to;
  for (const [name, value] of Object.entries(target.params)) {
    path = path.replace(`$${name}`, encodeURIComponent(value));
  }
  const query = target.search ? `?${new URLSearchParams(target.search)}` : "";
  return `${path}${query}`;
}

export interface MatchedRoute {
  readonly routeId: string;
  readonly params: Record<string, string | undefined>;
  readonly search: unknown;
}

const SECTION_BY_ROUTE_ID: Readonly<Record<string, WorkspaceDestination>> = {
  "/workspaces/$workspaceId/": { section: "home" },
  "/workspaces/$workspaceId/dashboard": { section: "home" },
  "/workspaces/$workspaceId/my-work": { section: "my-work" },
  "/workspaces/$workspaceId/inbox": { section: "inbox" },
  "/workspaces/$workspaceId/search": { section: "search" },
  "/workspaces/$workspaceId/library": { section: "library" },
  "/workspaces/$workspaceId/reports": { section: "reports" },
  "/workspaces/$workspaceId/members": { section: "members" },
  "/workspaces/$workspaceId/teams": { section: "teams" },
  "/workspaces/$workspaceId/settings": { section: "settings" },
  "/workspaces/$workspaceId/billing": { section: "billing" },
  "/workspaces/$workspaceId/security": { section: "security" },
};

/**
 * The shell location from the router's matched route ids and params (the
 * deepest match wins). URL strings are never parsed.
 */
export function resolveShellLocation(
  matches: readonly MatchedRoute[],
): WorkspaceDestination | null {
  for (let index = matches.length - 1; index >= 0; index -= 1) {
    const match = matches[index]!;
    if (
      match.routeId === "/workspaces/$workspaceId/boards/$boardId" &&
      match.params.boardId
    ) {
      const view = (match.search as { view?: unknown } | undefined)?.view;
      return {
        section: "board",
        boardId: match.params.boardId,
        view: typeof view === "string" ? view : undefined,
      };
    }
    // The item page sits under its board: the shell keeps showing the board.
    if (
      match.routeId ===
        "/workspaces/$workspaceId/boards/$boardId/items/$itemId" &&
      match.params.boardId
    ) {
      return { section: "board", boardId: match.params.boardId };
    }
    if (
      match.routeId === "/workspaces/$workspaceId/docs/$docId" &&
      match.params.docId
    ) {
      return { section: "doc", docId: match.params.docId };
    }
    if (
      match.routeId === "/workspaces/$workspaceId/spaces/$spaceId" &&
      match.params.spaceId
    ) {
      return { section: "space", spaceId: match.params.spaceId };
    }
    if (
      match.routeId === "/workspaces/$workspaceId/teams/$teamId" &&
      match.params.teamId
    ) {
      return { section: "teams", teamId: match.params.teamId };
    }
    if (match.routeId.startsWith("/workspaces/$workspaceId/account")) {
      return { section: "account" };
    }
    const section = SECTION_BY_ROUTE_ID[match.routeId];
    if (section) return section;
  }
  return null;
}
