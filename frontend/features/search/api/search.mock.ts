import type { SearchResult } from "../model/search.contract"

export const mockSearchApi = {
  async search(workspaceId: string, query: string): Promise<SearchResult[]> {
    if (!query) return []
    return [
      { id: "board-1", title: "Project Board", type: "board", url: `/${workspaceId}/boards/board-1` },
      { id: "page-1", title: "Meeting Notes", type: "page", url: `/${workspaceId}/docs/page-1` }
    ]
  }
}
