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

export interface BoardColumnDtoApi {
  id: string
  boardId: string
  name: string
  fieldType: string
  settings?: Record<string, unknown> | string | null
  position: number
  isHidden: boolean
  isSystemField: boolean
}

export interface BoardMemberDtoApi {
  userId: string
  name: string
  avatar?: string | null
  role: string
  joinedAt: string
}

export interface ListDtoApi {
  id: string
  title: string
  color?: string | null
  position: number
  isArchived: boolean
  cards: CardSummaryDtoApi[]
}

export interface CardSummaryDtoApi {
  id: string
  title: string
  priority?: string | null
  status: string
  dueDate?: string | null
  cover?: string | null
  memberCount: number
  members: CardMemberDtoApi[]
  labels: CardLabelDtoApi[]
  checklistProgress: number
  checklistTotal: number
  commentCount: number
  attachmentCount: number
  position: number
  fieldValues?: Record<string, unknown> | string | null
}

export interface CardDtoApi {
  id: string
  boardId: string
  workspaceId: string
  listId: string
  title: string
  descriptionMd?: string | null
  linkedPageId?: string | null
  priority?: string | null
  status: string
  dueDate?: string | null
  startDate?: string | null
  completedAt?: string | null
  cover?: string | null
  position: number
  members: CardMemberDtoApi[]
  labels: CardLabelDtoApi[]
  checklists: ChecklistDtoApi[]
  commentCount: number
  attachmentCount: number
  fieldValues?: Record<string, unknown> | string | null
  createdAt: string
  updatedAt?: string | null
}

export interface CardMemberDtoApi {
  userId: string
  name: string
  avatar?: string | null
  assignedAt: string
}

export interface CardLabelDtoApi {
  labelId: string
  name?: string | null
  color: string
}

export interface ChecklistDtoApi {
  id: string
  title: string
  position: number
  items: ChecklistItemDtoApi[]
}

export interface ChecklistItemDtoApi {
  id: string
  title: string
  isChecked: boolean
  dueDate?: string | null
  assigneeId?: string | null
  position: number
}

export interface CommentDtoApi {
  id: string
  userId: string
  userName: string
  userAvatar?: string | null
  contentMd: string
  parentCommentId?: string | null
  isEdited: boolean
  resolvedAt?: string | null
  createdAt: string
}

export interface ActivityLogDtoApi {
  id: string
  actorId: string
  actorName?: string | null
  action: string
  resourceType: string
  resourceId: string
  resourceTitle?: string | null
  createdAt: string
}

export interface ActivityLogResponseApi {
  data: ActivityLogDtoApi[]
  total: number
  page: number
  pageSize: number
}

export interface AttachmentDtoApi {
  id: string
  resourceId: string
  filename: string
  url: string
  sizeBytes: number
  contentType: string
  source: string
  uploadedBy: string
  uploadedByName?: string | null
  createdAt: string
}

export interface BoardViewDtoApi {
  viewMode?: string | null
  filters?: string | Record<string, unknown> | null
  config?: string | Record<string, unknown> | null
}
