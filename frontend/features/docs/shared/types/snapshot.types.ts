import type { ID } from "./ids.types"
import type { DocsUser } from "./user.types"
import type { Page, PageActivity } from "../../pages/types/page.types"
import type { Block } from "../../blocks/types/block.types"
import type { PageComment } from "../../comments/types/comment.types"
import type { LinkedTask, LinkedBoard } from "./integration.types"
import type { PageTemplate } from "../../templates/types/template.types"

export interface DocsWorkspaceSnapshot {
  id: ID
  slug: string
  name: string
  icon: string
  users: DocsUser[]
  pages: Page[]
  blocks: Record<ID, Block[]>
  comments: Record<ID, PageComment[]>
  activity: Record<ID, PageActivity[]>
  tasks: LinkedTask[]
  boards: LinkedBoard[]
  templates: PageTemplate[]
  recentSearches: string[]
}
