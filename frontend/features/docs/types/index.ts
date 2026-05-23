export type ID = string

export type DocsRole = "owner" | "editor" | "commenter" | "viewer"
export type PageStatus = "draft" | "review" | "published" | "archived"
export type PresenceStatus = "active" | "idle" | "offline"

export type BlockType =
  | "paragraph"
  | "heading_1"
  | "heading_2"
  | "heading_3"
  | "bulleted_list"
  | "numbered_list"
  | "todo"
  | "quote"
  | "divider"
  | "code"
  | "callout"
  | "toggle"
  | "image"
  | "embed"
  | "table"
  | "board_reference"
  | "page_reference"

export interface DocsUser {
  id: ID
  name: string
  email: string
  avatarUrl: string | null
  color: string
  role: DocsRole
}

export interface CollaborativeMetadata {
  version: number
  lockOwnerId: ID | null
  activeUserIds: ID[]
  lastSyncedAt: string
  realtimeChannel: string
  aiSummaryStatus: "idle" | "queued" | "ready"
}

export interface Mention {
  id: ID
  type: "user" | "page" | "task" | "board"
  targetId: ID
  label: string
}

export interface LinkedTask {
  id: ID
  title: string
  status: "todo" | "in_progress" | "done" | "blocked"
  dueDate: string | null
  assigneeId: ID | null
  boardId: ID
}

export interface LinkedBoard {
  id: ID
  name: string
  color: string
  openTasks: number
  doneTasks: number
}

export interface BlockProperties {
  text?: string
  checked?: boolean
  language?: string
  url?: string
  caption?: string
  color?: string
  icon?: string
  title?: string
  items?: string[]
  rows?: string[][]
  linkedPageId?: ID
  linkedBoardId?: ID
  linkedTaskId?: ID
  mentionIds?: ID[]
  align?: "left" | "center" | "right"
  fontFamily?: "inter" | "poppins" | "serif" | "mono"
  fontSize?: "sm" | "base" | "lg" | "xl"
  bold?: boolean
  italic?: boolean
  underline?: boolean
  strike?: boolean
  textColor?: "default" | "muted" | "primary" | "accent" | "destructive"
  highlight?: "none" | "muted" | "accent" | "primary"
  commentsCount?: number
}

export interface Block {
  id: ID
  pageId: ID
  type: BlockType
  properties: BlockProperties
  position: number
  parentId: ID | null
  children?: Block[]
  createdById: ID
  updatedById: ID
  createdAt: string
  updatedAt: string
}

export interface Page {
  id: ID
  workspaceId: ID
  workspaceSlug: string
  title: string
  icon: string | null
  coverUrl: string | null
  coverColor: string
  parentId: ID | null
  position: number
  status: PageStatus
  isPublished: boolean
  isFavorited: boolean
  isShared: boolean
  tags: string[]
  authorId: ID
  lastEditedById: ID
  lastEditedAt: string
  createdAt: string
  updatedAt: string
  collaboratorIds: ID[]
  metadata: CollaborativeMetadata
  linkedTaskIds: ID[]
  linkedBoardIds: ID[]
}

export interface PageTreeNode extends Page {
  children: PageTreeNode[]
  depth: number
}

export interface BreadcrumbItem {
  id: ID
  title: string
  icon: string | null
}

export interface PageDetail extends Page {
  blocks: Block[]
  breadcrumb: BreadcrumbItem[]
  collaborators: DocsUser[]
  linkedTasks: LinkedTask[]
  linkedBoards: LinkedBoard[]
}

export interface PageComment {
  id: ID
  pageId: ID
  blockId: ID | null
  authorId: ID
  body: string
  mentionIds: ID[]
  resolved: boolean
  createdAt: string
  updatedAt: string
}

export interface PageActivity {
  id: ID
  pageId: ID
  actorId: ID
  action: "created" | "edited" | "commented" | "shared" | "moved" | "published"
  targetLabel: string
  createdAt: string
}

export interface PageTemplate {
  id: ID
  name: string
  description: string
  icon: string
  accent: string
  blockTypes: BlockType[]
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

export interface CreatePagePayload {
  title: string
  workspaceId: ID
  workspaceSlug?: string
  parentId?: ID | null
  templateId?: ID
}

export interface UpdatePagePayload {
  title?: string
  icon?: string | null
  coverUrl?: string | null
  coverColor?: string
  status?: PageStatus
  isPublished?: boolean
  isFavorited?: boolean
  tags?: string[]
}

export interface CreateBlockPayload {
  type: BlockType
  properties?: BlockProperties
  position?: number
  parentId?: ID | null
}

export interface UpdateBlockPayload {
  type?: BlockType
  properties?: BlockProperties
  position?: number
  parentId?: ID | null
}

export interface CreateCommentPayload {
  pageId: ID
  blockId?: ID | null
  body: string
  mentionIds?: ID[]
}

export interface ReorderBlocksInput {
  pageId: ID
  orderedBlockIds: ID[]
}

export interface EditorSelectionState {
  activeBlockId: ID | null
  hasSelection: boolean
  selectionText: string
  selectionRect: DOMRect | null
}

export interface DocToolbarState {
  activeBlockId: ID | null
  activeBlockType: BlockType
  properties: BlockProperties
}

export interface SlashCommandItem {
  id: string
  type: BlockType
  label: string
  description: string
  keywords: string[]
}
