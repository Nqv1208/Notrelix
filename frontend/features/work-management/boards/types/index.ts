import type { FieldDefinition, BoardColumnDtoApi } from "../../fields/types"
import type { BoardGroup, ListDtoApi } from "../../groups/types"

export interface BoardMember {
  id: string
  userId: string
  name: string
  initials: string
  role: "owner" | "editor" | "viewer"
  avatarUrl?: string
  color: string
}

export interface Board {
  id: string
  workspaceId: string
  title: string
  description?: string
  background: { type: "color" | "image"; value: string }
  visibility: "private" | "workspace" | "public"
  isArchived: boolean
  linkedPageId?: string
  fieldDefinitions: FieldDefinition[]
  members: BoardMember[]
  createdAt: string
  updatedAt?: string
}

export type ViewMode = "table" | "kanban" | "calendar" | "timeline"

export interface ViewConfig {
  groupBy: "list" | "status" | "priority" | "assignee" | string
  hiddenFields: string[]
  columnOrder: string[]
  columnWidths: Record<string, number>
  collapsedGroups: Record<string, boolean>
  filters: FilterConfig[]
  sortBy: SortConfig[]
  searchQuery?: string
}

export interface FilterConfig {
  fieldId: string
  operator: "is" | "is_not" | "contains" | "is_empty" | "is_not_empty"
  value: unknown
}

export interface SortConfig {
  fieldId: string
  direction: "asc" | "desc"
}

export interface FullBoardResponse {
  board: Board
  groups: BoardGroup[]
  fieldDefinitions: FieldDefinition[]
}

export interface BoardViewDtoApi {
  viewMode?: string | null
  filters?: string | Record<string, unknown> | null
  config?: string | Record<string, unknown> | null
}

export interface BoardDtoApi {
  id: string
  workspaceId: string
  title: string
  description?: string | null
  background: string
  visibility: string
  isArchived: boolean
  memberCount: number
  listCount: number
  createdAt: string
}

export interface FullBoardDtoApi {
  id: string
  title: string
  description?: string | null
  background: string
  visibility: string
  columns?: BoardColumnDtoApi[]
  lists: ListDtoApi[]
  members: BoardMemberDtoApi[]
}

export interface BoardMemberDtoApi {
  userId: string
  name: string
  avatar?: string | null
  role: string
  joinedAt: string
}
