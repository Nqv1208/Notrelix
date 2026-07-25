export type SearchResultType = 'page' | 'block' | 'task' | 'board';

export interface SearchResult {
  id: string;
  type: SearchResultType;
  title: string;
  excerpt: string;
  icon: string | null;
  pageId?: string;
  score: number;
  group: 'Pages' | 'Blocks' | 'Tasks' | 'Boards';
}

export interface SearchFilters {
  types?: SearchResultType[];
  workspaceId: string;
  query: string;
}
