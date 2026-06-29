import type { ID } from "../../shared/types/ids.types"
import type { Page } from "../../pages/types/page.types"

export interface PageTreeNode extends Page {
  children: PageTreeNode[]
  depth: number
}

export interface SearchResult {
  id: ID
  type: "page" | "block" | "task" | "board"
  title: string
  excerpt: string
  icon: string | null
  pageId?: ID
  score: number
  group: "Pages" | "Blocks" | "Tasks" | "Boards"
}
