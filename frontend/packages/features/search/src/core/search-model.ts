export type SearchResultType = "page" | "block" | "task" | "board";

export interface SearchResult {
  id: string;
  type: SearchResultType;
  title: string;
  excerpt: string;
  icon: string | null;
  pageId?: string;
  score: number;
  group: "Pages" | "Blocks" | "Tasks" | "Boards";
}

export const RESULT_TYPES: { value: SearchResultType; label: string }[] = [
  { value: "page", label: "Pages" },
  { value: "block", label: "Blocks" },
  { value: "task", label: "Tasks" },
  { value: "board", label: "Boards" },
];
