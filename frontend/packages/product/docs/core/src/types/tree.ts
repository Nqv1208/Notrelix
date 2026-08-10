import type { ID } from "./ids";
import type { Page } from "./page";

export interface PageTreeNode extends Page {
  children: PageTreeNode[];
  depth: number;
}

export interface SearchResult {
  id: ID;
  type: "page" | "block" | "task" | "board";
  title: string;
  excerpt: string;
  icon: string | null;
  pageId?: ID;
  score: number;
  group: "Pages" | "Blocks" | "Tasks" | "Boards";
}
