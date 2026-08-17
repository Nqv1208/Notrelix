/**
 * Search context handler.
 *
 * Operations:
 *   search.query — GET /search?q=...&workspaceId=...
 *
 * Search is performed against the normalized MockStore so mutations
 * are immediately reflected in search results.
 *
 * Plan: 06-HANDLERS-PROJECTIONS.md §Search, 03-MOCK-DATA-MODEL.md §Search
 */

import { defineMockOperation } from "../../operations/types";
import { ok } from "../../transport/create-response";

interface SearchResultItem {
  type: "board" | "card" | "page";
  id: string;
  title: string;
  workspaceId: string;
  url?: string;
}

interface SearchResponse {
  results: SearchResultItem[];
}

export const searchOperations = [
  defineMockOperation<Record<string, never>, never, SearchResponse>({
    id: "search.query",
    method: "GET",
    route: "/search",
    async handle({ query, store }) {
      const q = (query["q"] ?? "").toLowerCase().trim();
      const workspaceId = query["workspaceId"];

      if (!q) return ok<SearchResponse>({ results: [] });

      const results: SearchResultItem[] = [];

      const workspaces = workspaceId
        ? [store.getWorkspace(workspaceId)].filter(Boolean)
        : store.getWorkspaces();

      for (const ws of workspaces) {
        if (!ws) continue;

        // Search boards
        for (const board of store.getBoards(ws.id)) {
          if (board.title.toLowerCase().includes(q)) {
            results.push({ type: "board", id: board.id, title: board.title, workspaceId: ws.id });
          }

          // Search cards within board lists
          for (const list of store.getLists(board.id)) {
            for (const card of store.getCards(list.id)) {
              if (card.title.toLowerCase().includes(q)) {
                results.push({ type: "card", id: card.id, title: card.title, workspaceId: ws.id });
              }
            }
          }
        }

        // Search pages
        for (const page of store.getPages(ws.id)) {
          if (page.title.toLowerCase().includes(q)) {
            results.push({ type: "page", id: page.id, title: page.title, workspaceId: ws.id });
          }
        }
      }

      return ok<SearchResponse>({ results: results.slice(0, 50) });
    },
  }),
];
