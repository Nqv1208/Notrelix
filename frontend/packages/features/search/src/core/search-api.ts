import type { SearchResult, SearchResultType } from "./search-model";

export interface SearchApi {
  search(input: {
    workspaceId: string;
    query: string;
    types: readonly SearchResultType[];
  }): Promise<readonly SearchResult[]>;
}

export function createUnavailableSearchApi(): SearchApi {
  return {
    async search() {
      return [];
    },
  };
}
