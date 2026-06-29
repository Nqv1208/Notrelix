import { isMockModeEnabled } from "@/lib/config/mock-mode"
import { mockSearchApi } from "./search.mock"
import type { SearchResult } from "../model/search.contract"

export const searchApi = {
  async search(workspaceId: string, query: string): Promise<SearchResult[]> {
    if (isMockModeEnabled("search")) {
      return mockSearchApi.search(workspaceId, query)
    }
    // Return empty array in production since global search is not integrated
    return []
  }
}
