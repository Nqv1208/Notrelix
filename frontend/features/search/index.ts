// Public API for the search feature slice.
// Explicit exports only.

export type SearchResult = {
  id: string
  title: string
  type: "board" | "item" | "page"
  url: string
}

// Minimal correct contracts for search
export const searchApi = {
  async search(workspaceId: string, query: string): Promise<SearchResult[]> {
    if (!query) return []
    // Simulated search results
    return [
      { id: "board-1", title: "Project Board", type: "board", url: `/${workspaceId}/boards/board-1` },
      { id: "page-1", title: "Meeting Notes", type: "page", url: `/${workspaceId}/docs/page-1` }
    ]
  }
}
