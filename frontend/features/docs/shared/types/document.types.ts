export type BlockType =
  | "paragraph"
  | "heading_1"
  | "heading_2"
  | "heading_3"
  | "bulleted_list"
  | "numbered_list"
  | "to_do"
  | "quote"
  | "divider"
  | "code"
  | "callout"
  | "image"
  | "toggle"

export type Block = {
  id: string
  type: BlockType
  content: string
  properties?: BlockProperties
  children?: Block[]
  parentId?: string | null
  position: number
  createdAt: string
  updatedAt: string
}

export type BlockProperties = {
  checked?: boolean
  language?: string
  emoji?: string
  level?: number
  url?: string
  caption?: string
  expanded?: boolean
  color?: string
}

export type Page = {
  id: string
  workspaceId: string
  parentId: string | null
  title: string
  icon: string
  coverUrl?: string
  position: number
  isDeleted: boolean
  isFavorite: boolean
  children?: Page[]
  createdAt: string
  updatedAt: string
}

export type Workspace = {
  id: string
  name: string
  icon: string
  color: string
  pages: Page[]
}

export type SlashCommandItem = {
  id: string
  label: string
  description: string
  icon: string
  type: BlockType
  shortcut?: string
}
