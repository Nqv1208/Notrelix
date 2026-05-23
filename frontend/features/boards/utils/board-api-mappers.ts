import type {
  Board,
  BoardColumnDtoApi,
  BoardDtoApi,
  BoardMember,
  Card,
  CardDetail,
  CardDtoApi,
  CardMember,
  CardUpdate,
  CardFile,
  CardActivity,
  CardSummaryDtoApi,
  CommentDtoApi,
  FieldDefinition,
  FieldOption,
  FullBoardDtoApi,
  FullBoardResponse,
  ActivityLogResponseApi,
  AttachmentDtoApi,
  ListDtoApi,
} from "../types"

const statusOptions: FieldOption[] = [
  { id: "status-not-started", label: "Not Started", color: "var(--muted-foreground)" },
  { id: "status-working", label: "Working on it", color: "var(--primary)" },
  { id: "status-stuck", label: "Stuck", color: "var(--destructive)" },
  { id: "status-done", label: "Done", color: "var(--accent)" },
  { id: "status-completed", label: "Completed", color: "var(--primary)" },
]

const priorityOptions: FieldOption[] = [
  { id: "urgent", label: "Urgent", color: "var(--destructive)" },
  { id: "high", label: "High", color: "var(--primary)" },
  { id: "medium", label: "Medium", color: "var(--accent)" },
  { id: "low", label: "Low", color: "var(--muted-foreground)" },
]

const memberColors = ["var(--primary)", "var(--accent)", "var(--destructive)", "var(--muted-foreground)"]

export function mapBoardDto(dto: BoardDtoApi): Board {
  return {
    id: dto.id,
    workspaceId: dto.workspaceId,
    title: dto.title,
    description: dto.description ?? undefined,
    background: { type: "color", value: dto.background || "var(--background)" },
    visibility: normalizeVisibility(dto.visibility),
    isArchived: dto.isArchived,
    linkedPageId: undefined,
    fieldDefinitions: [],
    members: [],
    createdAt: dto.createdAt,
  }
}

export function mapFullBoardDto(dto: FullBoardDtoApi, context: { workspaceId: string }): FullBoardResponse {
  const columns = dto.columns ?? []
  const fieldDefinitions = columns.length > 0 ? columns.map(mapBoardColumnDto) : createDefaultFieldDefinitions(dto.id)
  const members = dto.members.map(mapBoardMember)
  const board: Board = {
    id: dto.id,
    workspaceId: context.workspaceId,
    title: dto.title,
    description: dto.description ?? undefined,
    background: { type: "color", value: dto.background || "var(--background)" },
    visibility: normalizeVisibility(dto.visibility),
    isArchived: false,
    linkedPageId: undefined,
    fieldDefinitions,
    members,
    createdAt: new Date(0).toISOString(),
  }

  return {
    board,
    fieldDefinitions,
    groups: dto.lists
      .filter((list) => !list.isArchived)
      .sort((a, b) => a.position - b.position)
      .map((list) => mapListDto(list, board, fieldDefinitions)),
  }
}

export function mapCardDto(dto: CardDtoApi): CardDetail {
  const members = dto.members.map(mapCardMember)
  const card: CardDetail = {
    id: dto.id,
    listId: dto.listId,
    boardId: dto.boardId,
    workspaceId: dto.workspaceId,
    title: dto.title,
    descriptionMd: dto.descriptionMd ?? "",
    linkedPageId: dto.linkedPageId ?? undefined,
    position: dto.position,
    priority: normalizePriority(dto.priority),
    status: normalizeStatus(dto.status),
    dueDate: dto.dueDate ?? undefined,
    startDate: dto.startDate ?? undefined,
    completedAt: dto.completedAt ?? undefined,
    isArchived: false,
    isDeleted: false,
    members,
    labels: dto.labels.map((label) => ({ id: label.labelId, name: label.name ?? "Label", color: label.color })),
    checklists: dto.checklists.map((checklist) => ({
      id: checklist.id,
      title: checklist.title,
      position: checklist.position,
      items: checklist.items.map((item) => ({
        id: item.id,
        title: item.title,
        isDone: item.isChecked,
        position: item.position,
      })),
    })),
    fieldValues: parseFieldValues(dto.fieldValues),
    _count: {
      comments: dto.commentCount,
      attachments: dto.attachmentCount,
      checklistItems: dto.checklists.reduce((count, checklist) => count + checklist.items.length, 0),
    },
    createdAt: dto.createdAt,
    updatedAt: dto.updatedAt ?? undefined,
    boardTitle: "",
    watchers: members,
    isWatched: false,
    updates: [],
    files: [],
    activity: [],
  }

  return card
}

export function mapCommentDtoToCardUpdate(dto: CommentDtoApi, cardId: string): CardUpdate {
  return {
    id: dto.id,
    cardId,
    author: {
      id: `cm-${dto.userId}`,
      userId: dto.userId,
      name: dto.userName,
      initials: getInitials(dto.userName),
      avatarUrl: dto.userAvatar ?? undefined,
      color: memberColors[0],
    },
    body: dto.contentMd,
    mentionUserIds: [],
    attachmentIds: [],
    createdAt: dto.createdAt,
    updatedAt: dto.isEdited ? dto.createdAt : undefined,
  }
}

export function mapAttachmentDtoToCardFile(dto: AttachmentDtoApi, cardId: string): CardFile {
  const name = dto.uploadedByName ?? "Unknown"
  return {
    id: dto.id,
    cardId,
    name: dto.filename,
    size: dto.sizeBytes,
    contentType: dto.contentType,
    url: dto.url,
    source: normalizeFileSource(dto.source),
    createdBy: {
      id: `cm-${dto.uploadedBy}`,
      userId: dto.uploadedBy,
      name,
      initials: getInitials(name),
      color: memberColors[1],
    },
    createdAt: dto.createdAt,
  }
}

export function mapActivityResponse(dto: ActivityLogResponseApi, cardId: string): CardActivity[] {
  return dto.data.map((item) => ({
    id: item.id,
    cardId,
    actor: item.actorName ?? "Unknown",
    action: item.action,
    type: normalizeActivityType(item.action),
    metadata: item.resourceTitle ? { resourceTitle: item.resourceTitle } : undefined,
    createdAt: item.createdAt,
  }))
}

function mapListDto(dto: ListDtoApi, board: Board, fields: FieldDefinition[]) {
  return {
    id: dto.id,
    title: dto.title,
    color: dto.color ?? "var(--primary)",
    position: dto.position,
    isCollapsed: false,
    cards: [...dto.cards]
      .sort((a, b) => a.position - b.position)
      .map((card) => mapCardSummaryDto(card, dto.id, board, fields)),
  }
}

function mapCardSummaryDto(dto: CardSummaryDtoApi, listId: string, board: Board, fields: FieldDefinition[]): Card {
  const priority = normalizePriority(dto.priority)
  const status = normalizeStatus(dto.status)
  const progress = dto.checklistTotal > 0 ? Math.round((dto.checklistProgress / dto.checklistTotal) * 100) : 0
  const customValues = parseFieldValues(dto.fieldValues)
  const semanticValues = Object.fromEntries(fields.map((field) => {
    const semantic = getSemanticField(field)
    if (semantic === "title") return [field.id, dto.title]
    if (semantic === "person") return [field.id, []]
    if (semantic === "status") return [field.id, normalizeOptionValue(dto.status, field.options, status)]
    if (semantic === "priority") return [field.id, normalizeOptionValue(priority, field.options, priority)]
    if (semantic === "due_date") return [field.id, dto.dueDate ?? undefined]
    if (semantic === "linked_page") return [field.id, undefined]
    if (semantic === "progress") return [field.id, progress]
    return [field.id, undefined]
  }))
  const fieldValues = { ...customValues, ...semanticValues }

  return {
    id: dto.id,
    listId,
    boardId: board.id,
    workspaceId: board.workspaceId,
    title: dto.title,
    descriptionMd: "",
    linkedPageId: undefined,
    position: dto.position,
    priority,
    status,
    dueDate: dto.dueDate ?? undefined,
    startDate: undefined,
    completedAt: undefined,
    isArchived: false,
    isDeleted: false,
    members: [],
    labels: [],
    checklists: [],
    fieldValues,
    _count: {
      comments: dto.commentCount,
      attachments: dto.attachmentCount,
      checklistItems: dto.checklistTotal,
    },
    createdAt: new Date(0).toISOString(),
    updatedAt: undefined,
  }
}

function mapBoardColumnDto(dto: BoardColumnDtoApi): FieldDefinition {
  const settings = parseSettings(dto.settings)
  return {
    id: dto.id,
    boardId: dto.boardId,
    name: dto.name,
    fieldType: normalizeFieldType(dto.fieldType),
    options: parseOptions(settings.options, dto.name),
    position: dto.position,
    isHidden: dto.isHidden,
    isSystemField: dto.isSystemField,
  }
}

function parseFieldValues(value: CardSummaryDtoApi["fieldValues"] | CardDtoApi["fieldValues"]) {
  if (!value) return {}
  if (typeof value === "object") return value
  try {
    const parsed = JSON.parse(value)
    return parsed && typeof parsed === "object" ? parsed : {}
  } catch {
    return {}
  }
}

function createDefaultFieldDefinitions(boardId: string): FieldDefinition[] {
  return [
    createField(boardId, "field-title", "Task", "text", 1),
    createField(boardId, "field-person", "Owner", "person", 2),
    createField(boardId, "field-status", "Status", "select", 3, statusOptions),
    createField(boardId, "field-priority", "Priority", "select", 4, priorityOptions),
    createField(boardId, "field-due-date", "Due date", "date", 5),
    createField(boardId, "field-linked-page", "Linked doc", "linked_page", 6),
    createField(boardId, "field-progress", "Progress", "progress", 7),
  ]
}

function createField(
  boardId: string,
  suffix: string,
  name: string,
  fieldType: FieldDefinition["fieldType"],
  position: number,
  options: FieldOption[] = []
): FieldDefinition {
  return {
    id: `${boardId}-${suffix}`,
    boardId,
    name,
    fieldType,
    options,
    position,
    isHidden: false,
    isSystemField: true,
  }
}

function mapBoardMember(member: FullBoardDtoApi["members"][number], index: number): BoardMember {
  return {
    id: `bm-${member.userId}`,
    userId: member.userId,
    name: member.name,
    initials: getInitials(member.name),
    role: normalizeMemberRole(member.role),
    avatarUrl: member.avatar ?? undefined,
    color: memberColors[index % memberColors.length],
  }
}

function mapCardMember(member: CardDtoApi["members"][number], index: number): CardMember {
  return {
    id: `cm-${member.userId}`,
    userId: member.userId,
    name: member.name,
    initials: getInitials(member.name),
    avatarUrl: member.avatar ?? undefined,
    color: memberColors[index % memberColors.length],
  }
}

function normalizeVisibility(value: string): Board["visibility"] {
  const normalized = value.trim().toLowerCase()
  if (normalized === "private" || normalized === "workspace" || normalized === "public") return normalized
  return "workspace"
}

function normalizePriority(value?: string | null): Card["priority"] {
  const normalized = value?.trim().toLowerCase()
  if (normalized === "urgent" || normalized === "high" || normalized === "medium" || normalized === "low") return normalized
  return "medium"
}

function normalizeStatus(value: string) {
  const normalized = value.trim().toLowerCase().replaceAll("_", "-").replaceAll(" ", "-")
  if (statusOptions.some((option) => option.id === normalized)) return normalized
  if (normalized.includes("working") || normalized.includes("progress")) return "status-working"
  if (normalized.includes("stuck") || normalized.includes("blocked")) return "status-stuck"
  if (normalized.includes("completed")) return "status-completed"
  if (normalized.includes("done")) return "status-done"
  return "status-not-started"
}

function normalizeFieldType(value: string): FieldDefinition["fieldType"] {
  const normalized = value.trim().toLowerCase().replaceAll("-", "_")
  if (normalized === "people" || normalized === "user" || normalized === "users") return "person"
  if (normalized === "linked_doc" || normalized === "page") return "linked_page"
  if (
    normalized === "text" ||
    normalized === "number" ||
    normalized === "checkbox" ||
    normalized === "select" ||
    normalized === "multi_select" ||
    normalized === "date" ||
    normalized === "timeline" ||
    normalized === "person" ||
    normalized === "linked_page" ||
    normalized === "progress" ||
    normalized === "relation" ||
    normalized === "formula"
  ) return normalized
  return "text"
}

function parseSettings(value: BoardColumnDtoApi["settings"]): Record<string, unknown> {
  if (!value) return {}
  if (typeof value === "object") return value
  try {
    const parsed = JSON.parse(value)
    return parsed && typeof parsed === "object" ? parsed : {}
  } catch {
    return {}
  }
}

function parseOptions(value: unknown, fieldName: string): FieldOption[] {
  if (!Array.isArray(value)) {
    const normalized = fieldName.trim().toLowerCase()
    if (normalized.includes("status")) return statusOptions
    if (normalized.includes("priority")) return priorityOptions
    return []
  }

  return value.map((item, index) => {
    if (typeof item === "string") return optionFromLabel(item, index)
    if (item && typeof item === "object") {
      const option = item as Partial<FieldOption> & { value?: string }
      const label = option.label ?? option.value ?? option.id ?? `Option ${index + 1}`
      return {
        id: option.id ?? normalizeOptionId(label),
        label,
        color: option.color ?? memberColors[index % memberColors.length],
      }
    }
    return optionFromLabel(`Option ${index + 1}`, index)
  })
}

function optionFromLabel(label: string, index: number): FieldOption {
  return {
    id: normalizeOptionId(label),
    label,
    color: memberColors[index % memberColors.length],
  }
}

function normalizeOptionId(value: string) {
  return value.trim().toLowerCase().replaceAll("_", "-").replace(/\s+/g, "-")
}

function normalizeOptionValue(value: string | undefined, options: FieldOption[], fallback: string | undefined) {
  if (!value) return fallback
  const normalized = normalizeOptionId(value)
  return options.find((option) => normalizeOptionId(option.id) === normalized || normalizeOptionId(option.label) === normalized)?.id ?? fallback
}

function getSemanticField(field: FieldDefinition) {
  const normalizedName = field.name.trim().toLowerCase()
  if (normalizedName === "task" || normalizedName === "title" || normalizedName === "name") return "title"
  if (normalizedName.includes("owner") || normalizedName.includes("assignee") || field.fieldType === "person") return "person"
  if (normalizedName.includes("status")) return "status"
  if (normalizedName.includes("priority")) return "priority"
  if (normalizedName.includes("due") && normalizedName.includes("date")) return "due_date"
  if (normalizedName.includes("linked") && (normalizedName.includes("doc") || normalizedName.includes("page"))) return "linked_page"
  if (normalizedName.includes("progress")) return "progress"
  return field.fieldType
}

function normalizeFileSource(value: string): CardFile["source"] {
  const normalized = value.trim().toLowerCase()
  if (normalized === "upload" || normalized === "r2" || normalized === "s3" || normalized === "link") return normalized
  return "link"
}

function normalizeActivityType(action: string): CardActivity["type"] {
  const normalized = action.trim().toLowerCase()
  if (normalized.includes("comment")) return "commented"
  if (normalized.includes("file") || normalized.includes("attachment")) return "file"
  if (normalized.includes("create")) return "created"
  if (normalized.includes("automation")) return "automation"
  if (normalized.includes("update") || normalized.includes("move")) return "updated"
  return "system"
}

function normalizeMemberRole(value: string): BoardMember["role"] {
  const normalized = value.trim().toLowerCase()
  if (normalized === "owner" || normalized === "admin") return "owner"
  if (normalized === "editor" || normalized === "member") return "editor"
  return "viewer"
}

function getInitials(name: string) {
  const parts = name.trim().split(/\s+/).filter(Boolean)
  if (parts.length === 0) return "?"
  if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase()
  return `${parts[0][0]}${parts[parts.length - 1][0]}`.toUpperCase()
}
