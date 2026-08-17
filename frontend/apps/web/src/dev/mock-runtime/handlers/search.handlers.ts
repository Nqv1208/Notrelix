import type { SearchResult } from "@notrelix/features-search";
import type { MockHandler } from "../transport/mock-handler";

export const searchHandlers: readonly MockHandler[] = [
  {
    id: "search.query",
    matches: (request) => request.method === "GET" && request.url.startsWith("/api/v1/search?"),
    async handle(request, context) {
      const url = new URL(request.url, "http://mock.notrelix.local");
      const query = (url.searchParams.get("q") ?? "").trim().toLocaleLowerCase();
      const workspaceId = url.searchParams.get("workspaceId");
      if (!query || !workspaceId) return [];
      return context.store
        .getVisibleWorkspaces()
        .filter((workspace) => workspace.id === workspaceId && workspace.name.toLocaleLowerCase().includes(query))
        .map<SearchResult>((workspace) => ({
          id: workspace.id,
          type: "board",
          title: workspace.name,
          excerpt: workspace.description ?? "Workspace result",
          icon: workspace.icon,
          score: 1,
          group: "Boards",
        }));
    },
  },
];
