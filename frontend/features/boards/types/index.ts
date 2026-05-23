export type FieldType =
  | "text"
  | "number"
  | "checkbox"
  | "select"
  | "multi_select"
  | "date"
  | "timeline"
  | "person"
  | "linked_page"
  | "progress"
  | "relation"
  | "formula"

export interface FieldOption {
  id: string
  label: string
  color: string
}

export interface FieldDefinition {
  id: string
  boardId: string
  name: string
  fieldType: FieldType
  options: FieldOption[]
  position: number
  isHidden: boolean
  isSystemField: boolean
}

export interface FieldValue {
  cardId: string
  fieldDefinitionId: string
  value: unknown
}

export interface BoardMember {
  id: string
  userId: string
  name: string
  initials: string
  role: "owner" | "editor" | "viewer"
  avatarUrl?: string
  color: string
}

export interface CardMember {
  id: string
  userId: string
  name: string
  initials: string
  avatarUrl?: string
  color: string
}

export interface CardLabel {
  id: string
  name: string
  color: string
}

export interface ChecklistItem {
  id: string
  title: string
  isDone: boolean
  position: number
}

export interface Checklist {
  id: string
  title: string
  items: ChecklistItem[]
  position: number
}

export interface BoardGroup {
  id: string
  title: string
  color?: string
  position: number
  isCollapsed: boolean
  cards: Card[]
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

export interface Card {
  id: string
  listId: string
  boardId: string
  workspaceId: string
  title: string
  descriptionMd?: string
  linkedPageId?: string
  position: number
  priority?: "urgent" | "high" | "medium" | "low"
  status: string
  dueDate?: string
  startDate?: string
  completedAt?: string
  isArchived: boolean
  isDeleted: boolean
  members: CardMember[]
  labels: CardLabel[]
  checklists: Checklist[]
  fieldValues: Record<string, unknown>
  _count: { comments: number; attachments: number; checklistItems: number }
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

export interface DragItem {
  type: "card" | "column" | "group"
  id: string
  sourceGroupId: string
  sourceIndex: number
}

export interface BoardTableColumn {
  id: string
  field: FieldDefinition
  width: number
  minWidth: number
  isVisible: boolean
}

export interface BoardTableGroupState {
  id: string
  isCollapsed: boolean
}

export interface BoardTableSelectionState {
  selectedCardIds: string[]
  isAllSelected: boolean
}

export interface BoardTableDraftCard {
  listId: string
  title: string
}

export interface FullBoardResponse {
  board: Board
  groups: BoardGroup[]
  fieldDefinitions: FieldDefinition[]
}

export type CardDetailTab = "updates" | "files" | "activity" | "linked-docs" | "subtasks"

export interface CardComment {
  id: string
  cardId: string
  author: string
  body: string
  createdAt: string
}

export interface CardUpdate {
  id: string
  cardId: string
  author: CardMember
  body: string
  mentionUserIds: string[]
  attachmentIds: string[]
  createdAt: string
  updatedAt?: string
}

export interface CardActivity {
  id: string
  cardId: string
  actor: string
  action: string
  type?: "created" | "updated" | "commented" | "file" | "automation" | "system"
  metadata?: Record<string, unknown>
  createdAt: string
}

export interface CardFile {
  id: string
  cardId: string
  name: string
  size: number
  contentType: string
  url: string
  source: "upload" | "r2" | "s3" | "link"
  createdBy: CardMember
  createdAt: string
}

export interface CardDetail extends Card {
  boardTitle: string
  watchers: CardMember[]
  isWatched: boolean
  updates: CardUpdate[]
  files: CardFile[]
  activity: CardActivity[]
}

export type * from "./api-types"
