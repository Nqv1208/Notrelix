import type { OperationResponse } from "@notrelix/contracts";
import type {
  ComputedValue,
  ActivityChange,
  ActivityChangeApi,
  ActivityLogResponseApi,
  AttachmentDtoApi,
  Board,
  BoardAccess,
  BoardAccessLevel,
  BoardAccessSource,
  BoardAccessSourceKind,
  BoardColumnDtoApi,
  BoardDtoApi,
  BoardGroup,
  BoardMember,
  Card,
  CardActivity,
  CardDetail,
  CardDtoApi,
  CardFile,
  CardCover,
  CardCoverColor,
  CardLabelDtoApi,
  CardMember,
  CardMemberDtoApi,
  CardSummaryDtoApi,
  CardRepeatRule,
  CardUpdate,
  Checklist,
  CommentDtoApi,
  FieldDefinition,
  FieldOption,
  FieldSemanticRole,
  FullBoardDtoApi,
  FullBoardResponse,
  ListDtoApi,
  WorkStatusCategory,
} from "@notrelix/work-management-core";
import {
  CARD_COVER_COLORS,
  CARD_REMINDER_DEFAULT,
  CARD_REPEAT_RULES,
  checklistProgressOf,
  personInitials,
} from "@notrelix/work-management-core";
import {
  findPriorityField,
  findStatusField,
  inferStatusCategory,
  isPriorityFieldName,
  normalizeName,
  orderByKey,
  priorityFromOptionName,
} from "@notrelix/work-management-core";

// ─── Board read contract ─────────────────────────────────────────────────────

/** `GET /boards/{id}/full` as generated from the backend contract. */
export type GeneratedFullBoardDto =
  OperationResponse<"WorkManagement.Boards.GetOverview">;
/** `GET /board-items/{id}` as generated from the backend contract. */
export type GeneratedBoardItemDto =
  OperationResponse<"WorkManagement.BoardItems.Get">;
/** `GET /boards/{id}/access` as generated from the backend contract. */
export type GeneratedBoardAccessDto =
  OperationResponse<"WorkManagement.Boards.GetAccess">;

type GeneratedField = NonNullable<GeneratedFullBoardDto["columns"]>[number];
type GeneratedGroup = NonNullable<GeneratedFullBoardDto["lists"]>[number];
type GeneratedItemSummary = NonNullable<GeneratedGroup["boardItems"]>[number];
type GeneratedItemMember = NonNullable<GeneratedItemSummary["members"]>[number];
type GeneratedItemLabel = NonNullable<GeneratedItemSummary["labels"]>[number];
type GeneratedChecklist = NonNullable<
  GeneratedBoardItemDto["checklists"]
>[number];

/**
 * The board read as the client receives it: the generated contract, or the
 * hand-written shape an older caller still declares (`lists[].cards`). Both
 * are normalised element by element.
 */
export type BoardReadDto = FullBoardDtoApi | GeneratedFullBoardDto;
/** The item read: generated (`name`, `groupId`) or hand-written (`title`). */
export type BoardItemReadDto = CardDtoApi | GeneratedBoardItemDto;

type AnyField = BoardColumnDtoApi | GeneratedField;
type AnyGroup = ListDtoApi | GeneratedGroup;
type AnyItemSummary = CardSummaryDtoApi | GeneratedItemSummary;

/** Item values the generated read carries in place of `fieldValues`. */
interface ItemReadValues {
  readonly version: number | null;
  readonly itemKey: string | null;
  readonly parentItemId: string | null;
  readonly statusOptionId: string | null;
  readonly priorityOptionId: string | null;
  readonly dueAt: string | null;
  readonly startedAt: string | null;
  readonly completedAt: string | null;
  readonly childProgress: { done: number; total: number } | null;
}

const statusOptions: FieldOption[] = [
  {
    id: "status-not-started",
    label: "Not Started",
    color: "var(--muted-foreground)",
  },
  { id: "status-working", label: "Working on it", color: "var(--primary)" },
  { id: "status-stuck", label: "Stuck", color: "var(--destructive)" },
  { id: "status-done", label: "Done", color: "var(--accent)" },
  { id: "status-completed", label: "Completed", color: "var(--primary)" },
];

const priorityOptions: FieldOption[] = [
  { id: "urgent", label: "Urgent", color: "var(--destructive)" },
  { id: "high", label: "High", color: "var(--primary)" },
  { id: "medium", label: "Medium", color: "var(--accent)" },
  { id: "low", label: "Low", color: "var(--muted-foreground)" },
];

const memberColors = [
  "var(--primary)",
  "var(--accent)",
  "var(--destructive)",
  "var(--muted-foreground)",
];

const EPOCH_ISO = "1970-01-01T00:00:00.000Z";

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
    ...(typeof dto.canAddItems === "boolean"
      ? { canAddItems: dto.canAddItems }
      : {}),
  };
}

export function mapFullBoardDto(
  dto: BoardReadDto,
  context: { workspaceId: string },
): FullBoardResponse {
  const boardId = dto.id ?? "";
  const columns: AnyField[] = (dto.columns ?? []).filter(
    (column) => !("isDeleted" in column && column.isDeleted === true),
  );
  const mappedFields =
    columns.length > 0
      ? orderByKey(columns, (column) => column.position).map((column, index) =>
          mapBoardColumnDto(column, boardId, index),
        )
      : createDefaultFieldDefinitions(boardId);
  const fieldDefinitions = assignSemanticRoles(mappedFields);
  const members = (dto.members ?? []).map(mapBoardMember);
  const board: Board = {
    id: boardId,
    workspaceId: context.workspaceId,
    title: dto.title ?? "",
    description: dto.description ?? undefined,
    background: { type: "color", value: dto.background || "var(--background)" },
    visibility: normalizeVisibility(dto.visibility),
    isArchived: dto.isArchived === true,
    linkedPageId: undefined,
    fieldDefinitions,
    members,
    createdAt: EPOCH_ISO,
    ...(typeof dto.version === "number" ? { version: dto.version } : {}),
  };

  const lists: AnyGroup[] = (dto.lists ?? []).filter(
    (list) => list.isArchived !== true,
  );
  const archivedGroupIds = (dto.lists ?? [])
    .filter((list) => list.isArchived === true)
    .map((list) => list.id ?? "");
  return {
    board,
    fieldDefinitions,
    groups: orderByKey(lists, (list) => list.position).map((list, index) =>
      mapListDto(list, index, board, fieldDefinitions),
    ),
    ...(archivedGroupIds.length > 0 ? { archivedGroupIds } : {}),
  };
}

/** The cover, repeat rule and reminder of an item, as the item reads carry them (all optional). */
function readCardSettings(
  dto: unknown,
): Pick<Card, "cover" | "repeatRule" | "reminderOffsetMinutes"> {
  const raw = dto as {
    coverColor?: string | null;
    coverAttachmentId?: string | null;
    coverStyle?: string | null;
    repeatRule?: string | null;
    reminderOffsetMinutes?: number | null;
  };
  const color = (CARD_COVER_COLORS as readonly string[]).includes(
    raw.coverColor ?? "",
  )
    ? (raw.coverColor as CardCoverColor)
    : null;
  const attachmentId = raw.coverAttachmentId ?? null;
  const cover: CardCover | null =
    color || attachmentId
      ? {
          color,
          attachmentId: color ? null : attachmentId,
          style: raw.coverStyle === "full" ? "full" : "strip",
        }
      : null;
  const repeatRule = (CARD_REPEAT_RULES as readonly string[]).includes(
    raw.repeatRule ?? "",
  )
    ? (raw.repeatRule as CardRepeatRule)
    : null;
  return {
    cover,
    repeatRule,
    reminderOffsetMinutes:
      typeof raw.reminderOffsetMinutes === "number"
        ? raw.reminderOffsetMinutes
        : CARD_REMINDER_DEFAULT,
  };
}

export function mapCardDto(dto: BoardItemReadDto): CardDetail {
  const members = (dto.members ?? []).map(mapCardMember);
  const values = readItemValues(dto);
  const legacy = isLegacyItem(dto) ? dto : null;
  // Item read values win over the hand-written words when both are present.
  const status =
    values.statusOptionId ?? (legacy ? normalizeStatus(legacy.status) : "");
  const dueDate = values.dueAt ?? (legacy ? legacy.dueDate : null);
  const checklists = (dto.checklists ?? []).map(mapChecklist);
  const card: CardDetail = {
    id: dto.id ?? "",
    listId: legacy
      ? legacy.listId
      : ((dto as GeneratedBoardItemDto).groupId ?? ""),
    boardId: dto.boardId ?? "",
    workspaceId: dto.workspaceId ?? "",
    title: itemTitle(dto),
    descriptionMd: legacy
      ? (legacy.descriptionMd ?? "")
      : ((dto as GeneratedBoardItemDto).descriptionMd ?? ""),
    linkedPageId: legacy ? (legacy.linkedPageId ?? undefined) : undefined,
    position: typeof dto.position === "number" ? dto.position : 0,
    // The hand-written read says "medium" for an item without a priority; the
    // detail keeps that as no priority and resolves the option id on the board.
    priority: legacy?.priority ? normalizePriority(legacy.priority) : undefined,
    priorityOptionId: values.priorityOptionId,
    status,
    dueDate: dueDate ?? undefined,
    startDate: legacy
      ? (legacy.startDate ?? undefined)
      : (values.startedAt ?? undefined),
    completedAt: legacy
      ? (legacy.completedAt ?? undefined)
      : (values.completedAt ?? undefined),
    isArchived: false,
    isDeleted: false,
    members,
    labels: (dto.labels ?? []).map(mapLabel),
    checklists,
    checklistProgress: checklistProgressOf(checklists),
    fieldValues: legacy
      ? parseFieldValues(legacy.fieldValues)
      : generatedFieldValues(dto),
    computedValues: generatedComputedValues(dto),
    _count: {
      comments: dto.commentCount ?? 0,
      attachments: dto.attachmentCount ?? 0,
      checklistItems: checklists.reduce(
        (count, checklist) => count + checklist.items.length,
        0,
      ),
    },
    createdAt: dto.createdAt ?? EPOCH_ISO,
    updatedAt: dto.updatedAt ?? undefined,
    version: values.version,
    itemKey: values.itemKey,
    parentItemId: values.parentItemId,
    statusOptionId: status || null,
    dueAt: dueDate ?? null,
    childProgress: values.childProgress,
    ...readCardSettings(dto),
    boardTitle: "",
    watchers: members,
    isWatched: false,
    updates: [],
    files: [],
    activity: [],
  };

  return card;
}

const ACCESS_LEVELS: readonly BoardAccessLevel[] = [
  "viewer",
  "commenter",
  "editor",
  "manager",
];
const ACCESS_SOURCE_KINDS: readonly BoardAccessSourceKind[] = [
  "owner",
  "workspace-admin",
  "space",
  "team",
  "explicit",
  "guest",
  "workspace",
];
const ACCESS_COUNT_KINDS: readonly BoardAccessSource["kind"][] = [
  "space",
  "team",
  "user",
  "guest",
];

export function mapBoardAccessDto(dto: GeneratedBoardAccessDto): BoardAccess {
  const level = dto.myLevel?.trim().toLowerCase();
  const kind = dto.mySource?.kind?.trim().toLowerCase();
  const grantedBy = dto.mySource?.grantedBy;
  return {
    boardId: dto.boardId ?? "",
    version: dto.version ?? 0,
    // Fail closed: a level this client does not know grants the least.
    myLevel: ACCESS_LEVELS.find((value) => value === level) ?? "viewer",
    mySource: {
      kind: ACCESS_SOURCE_KINDS.find((value) => value === kind) ?? "explicit",
      grantedBy:
        grantedBy?.userId && grantedBy.name
          ? { userId: grantedBy.userId, name: grantedBy.name }
          : null,
    },
    peopleCount: dto.peopleCount ?? 0,
    guestCount: dto.guestCount ?? 0,
    peopleCountComplete: dto.peopleCountComplete === true,
    sources: (dto.sources ?? []).flatMap((source) => {
      const sourceKind = ACCESS_COUNT_KINDS.find(
        (value) => value === source.kind?.trim().toLowerCase(),
      );
      if (!sourceKind) return [];
      return [
        {
          kind: sourceKind,
          id: source.id ?? null,
          name: source.name ?? null,
          count: source.count ?? 0,
        },
      ];
    }),
  };
}

export function mapCommentDtoToCardUpdate(
  dto: CommentDtoApi,
  cardId: string,
): CardUpdate {
  return {
    id: dto.id,
    cardId,
    author: {
      id: `cm-${dto.userId}`,
      userId: dto.userId,
      name: dto.userName,
      initials: getInitials(dto.userName),
      avatarUrl: dto.userAvatar ?? undefined,
      color: memberColors[0]!,
    },
    body: dto.contentMd,
    mentionUserIds: [],
    attachmentIds: dto.attachmentIds ?? [],
    createdAt: dto.createdAt,
    updatedAt: dto.isEdited ? dto.createdAt : undefined,
    parentId: dto.parentCommentId ?? undefined,
    reactions: dto.reactions ?? [],
  };
}

export function mapAttachmentDtoToCardFile(
  dto: AttachmentDtoApi,
  cardId: string,
): CardFile {
  const name = dto.uploadedByName ?? "Unknown";
  // A stored file has no public address: its `url` is empty and it is
  // downloaded through the authorised content route.
  const isFile = dto.kind?.trim().toLowerCase() === "file";
  return {
    id: dto.id,
    cardId,
    name: dto.filename,
    size: dto.sizeBytes,
    contentType: dto.contentType,
    url: isFile ? "" : (dto.url ?? ""),
    source: isFile ? "upload" : normalizeFileSource(dto.source ?? ""),
    kind: isFile ? "file" : "link",
    createdBy: {
      id: `cm-${dto.uploadedBy}`,
      userId: dto.uploadedBy,
      name,
      initials: getInitials(name),
      color: memberColors[1]!,
    },
    createdAt: dto.createdAt,
  };
}

export function mapActivityResponse(
  dto: ActivityLogResponseApi,
  cardId: string,
): CardActivity[] {
  return dto.data.map((item) => ({
    id: item.id,
    cardId,
    actor: item.actorName ?? (item.automationName ? "Tự động hoá" : "Unknown"),
    action: item.action,
    type: item.automationName
      ? "automation"
      : item.kind === "created"
        ? "created"
        : normalizeActivityType(item.action),
    metadata: item.resourceTitle
      ? { resourceTitle: item.resourceTitle }
      : undefined,
    createdAt: item.createdAt,
    ...(item.automationName ? { automationName: item.automationName } : {}),
    ...(item.verb ? { verb: item.verb } : {}),
    ...(item.target ? { target: item.target } : {}),
    ...(item.notified ? { notified: item.notified } : {}),
    ...(item.detail ? { detail: item.detail } : {}),
    ...(item.compact ? { compact: item.compact } : {}),
    ...(item.type ? { code: item.type } : {}),
    ...(item.change ? { change: presentChange(item.change) } : {}),
  }));
}

/** Drops the sides the API sends as null, so the client reads them as absent. */
function presentChange(change: ActivityChangeApi): ActivityChange {
  return Object.fromEntries(
    Object.entries(change).filter(([, value]) => value != null),
  ) as ActivityChange;
}

// ─── Element mappers ─────────────────────────────────────────────────────────

function mapListDto(
  dto: AnyGroup,
  index: number,
  board: Board,
  fields: FieldDefinition[],
): BoardGroup {
  const id = dto.id ?? "";
  const items: AnyItemSummary[] =
    "cards" in dto && Array.isArray(dto.cards)
      ? dto.cards
      : (((dto as GeneratedGroup).boardItems ?? []) as AnyItemSummary[]);
  return {
    id,
    title: dto.title ?? "",
    color: dto.color ?? "var(--primary)",
    position: typeof dto.position === "number" ? dto.position : index + 1,
    isCollapsed: false,
    cards: orderByKey(items, (item) => item.position).map((card, cardIndex) =>
      mapCardSummaryDto(card, cardIndex, id, board, fields),
    ),
  };
}

function mapCardSummaryDto(
  dto: AnyItemSummary,
  index: number,
  listId: string,
  board: Board,
  fields: FieldDefinition[],
): Card {
  const legacy = isLegacyItem(dto) ? dto : null;
  const values = readItemValues(dto);
  const statusField = findStatusField(fields);
  const priorityField = findPriorityField(fields);
  const customValues = legacy
    ? parseFieldValues(legacy.fieldValues)
    : generatedFieldValues(dto);
  const title = itemTitle(dto);

  // Legacy reads carry a status/priority word; generated reads carry option ids.
  const status = legacy
    ? normalizeStatus(legacy.status)
    : (values.statusOptionId ?? "");
  const legacyPriority = legacy
    ? normalizePriority(legacy.priority)
    : undefined;
  const priorityOptionId = legacy
    ? normalizeOptionValue(
        legacyPriority,
        priorityField?.options ?? [],
        legacyPriority,
      )
    : values.priorityOptionId;
  const priority = legacy
    ? legacyPriority
    : priorityFromOptionName(
        priorityField?.options.find((option) => option.id === priorityOptionId)
          ?.label,
      );
  const statusOptionId = legacy
    ? (normalizeOptionValue(
        legacy.status,
        statusField?.options ?? [],
        status,
      ) ?? null)
    : values.statusOptionId;
  const statusOption = statusField?.options.find(
    (option) => option.id === statusOptionId,
  );
  const statusCategory = statusOption
    ? (statusOption.category ??
      inferStatusCategory(
        statusOption.label,
        statusField!.options.indexOf(statusOption),
      ))
    : null;
  const dueDate = legacy ? (legacy.dueDate ?? null) : values.dueAt;
  const checklistProgress = dto.checklistProgress ?? 0;
  const checklistTotal = dto.checklistTotal ?? 0;
  const progress =
    checklistTotal > 0
      ? Math.round((checklistProgress / checklistTotal) * 100)
      : 0;

  // Card members come from the item's members, else from a person field value.
  const personField = fields.find((f) => getSemanticField(f) === "person");
  const itemMembers = dto.members ?? [];
  const personUserIds =
    itemMembers.length > 0
      ? itemMembers.map((m) => m.userId ?? "")
      : personField && Array.isArray(customValues[personField.id])
        ? (customValues[personField.id] as string[])
        : [];
  const members =
    itemMembers.length > 0
      ? itemMembers.map((m, memberIndex) => mapCardMember(m, memberIndex))
      : personUserIds
          .map((userId) => {
            const bm = board.members.find((m) => m.userId === userId);
            if (!bm) return null;
            return {
              id: `cm-${dto.id}-${bm.userId}`,
              userId: bm.userId,
              name: bm.name,
              initials: bm.initials,
              avatarUrl: bm.avatarUrl,
              color: bm.color,
            };
          })
          .filter((m): m is NonNullable<typeof m> => m !== null);

  const semanticValues = Object.fromEntries(
    fields.map((field) => {
      if (field.id === statusField?.id)
        return [field.id, statusOptionId ?? undefined];
      if (field.id === priorityField?.id)
        return [field.id, priorityOptionId ?? undefined];
      const semantic = getSemanticField(field);
      if (semantic === "title") return [field.id, title];
      if (semantic === "person") return [field.id, personUserIds];
      if (semantic === "due_date") return [field.id, dueDate ?? undefined];
      if (semantic === "linked_page") return [field.id, undefined];
      if (semantic === "progress") return [field.id, progress];
      return [field.id, customValues[field.id]];
    }),
  );

  return {
    id: dto.id ?? "",
    listId,
    boardId: board.id,
    workspaceId: board.workspaceId,
    title,
    descriptionMd: "",
    linkedPageId: undefined,
    position: typeof dto.position === "number" ? dto.position : index + 1,
    priority,
    status,
    dueDate: dueDate ?? undefined,
    startDate: values.startedAt ?? undefined,
    completedAt: values.completedAt ?? undefined,
    isArchived: false,
    isDeleted: false,
    members,
    labels: (dto.labels ?? []).map(mapLabel),
    checklists: [],
    fieldValues: { ...customValues, ...semanticValues },
    computedValues: generatedComputedValues(dto),
    _count: {
      comments: dto.commentCount ?? 0,
      attachments: dto.attachmentCount ?? 0,
      checklistItems: checklistTotal,
    },
    checklistProgress:
      checklistTotal > 0
        ? {
            done: Math.min(checklistProgress, checklistTotal),
            total: checklistTotal,
          }
        : null,
    createdAt:
      (legacy ? undefined : (dto as GeneratedItemSummary).createdAt) ??
      EPOCH_ISO,
    updatedAt:
      (legacy ? undefined : (dto as GeneratedItemSummary).updatedAt) ??
      undefined,
    version: values.version,
    itemKey: values.itemKey,
    parentItemId: values.parentItemId,
    statusOptionId,
    statusCategory,
    dueAt: dueDate,
    childProgress: values.childProgress,
    openBlockerCount: legacy
      ? 0
      : ((dto as GeneratedItemSummary).openBlockerCount ?? 0),
    ...readCardSettings(dto),
  };
}

function mapBoardColumnDto(
  dto: AnyField,
  boardId: string,
  index: number,
): FieldDefinition {
  const settings = parseSettings(dto.settings);
  const name = dto.name ?? "";
  const generatedOptions = "options" in dto ? dto.options : undefined;
  const options = generatedOptions
    ? orderByKey(generatedOptions, (option) => option.position).map(
        (option, optionIndex): FieldOption => ({
          id: option.id ?? "",
          label: option.name ?? "",
          color:
            option.color ?? memberColors[optionIndex % memberColors.length]!,
          ...(option.position != null ? { position: option.position } : {}),
          ...(option.category != null
            ? { category: normalizeCategory(option.category) }
            : {}),
        }),
      )
    : "isSystemField" in dto
      ? parseOptions(settings.options, name)
      : [];
  const rawType = dto.fieldType ?? "";
  return {
    id: dto.id ?? "",
    boardId: dto.boardId ?? boardId,
    name,
    fieldType: normalizeFieldType(rawType),
    options,
    position: typeof dto.position === "number" ? dto.position : index + 1,
    isHidden: "isHidden" in dto ? dto.isHidden : false,
    isSystemField:
      "isSystemField" in dto ? dto.isSystemField : (dto.isSystem ?? false),
    ...(typeof settings.unit === "string" ? { unit: settings.unit } : {}),
    ...(settings.display === "mark" ? { display: "mark" as const } : {}),
    ...(normalizeName(rawType) === "status"
      ? { semanticRole: "status" as const }
      : {}),
    ...("version" in dto && typeof dto.version === "number" && dto.version > 0
      ? { version: dto.version }
      : {}),
  };
}

/**
 * Marks the Status field (by type; for older reads by name) and the priority
 * field (first Select named priority / ưu tiên / độ ưu tiên / mức ưu tiên).
 */
function assignSemanticRoles(fields: FieldDefinition[]): FieldDefinition[] {
  const statusId = findStatusField(fields)?.id;
  const priorityId = fields.find(
    (field) =>
      field.id !== statusId &&
      field.fieldType === "select" &&
      isPriorityFieldName(field.name),
  )?.id;
  return fields.map((field) => {
    const role: FieldSemanticRole | undefined =
      field.id === statusId
        ? "status"
        : field.id === priorityId
          ? "priority"
          : undefined;
    return role ? { ...field, semanticRole: role } : field;
  });
}

/** The checklists of one item as the list endpoint or the item read carries them. */
export function mapChecklists(
  dtos: readonly (CardDtoApi["checklists"][number] | GeneratedChecklist)[],
): Checklist[] {
  return dtos.map(mapChecklist);
}

function mapChecklist(
  checklist: CardDtoApi["checklists"][number] | GeneratedChecklist,
  index: number,
): Checklist {
  return {
    id: checklist.id ?? "",
    title: checklist.title ?? "",
    position:
      typeof checklist.position === "number" ? checklist.position : index + 1,
    // The checklist is the consistency boundary: every write names its version.
    ...("version" in checklist && typeof checklist.version === "number"
      ? { version: checklist.version }
      : {}),
    items: (checklist.items ?? []).map((item, itemIndex) => ({
      id: item.id ?? "",
      title: item.title ?? "",
      isDone:
        "isChecked" in item
          ? item.isChecked
          : ["done", "completed", "checked"].includes(
              (item.status ?? "").trim().toLowerCase(),
            ),
      position:
        typeof item.position === "number" ? item.position : itemIndex + 1,
      ...("assigneeUserId" in item && item.assigneeUserId
        ? { assigneeId: item.assigneeUserId }
        : {}),
      ...("dueAt" in item && item.dueAt ? { dueAt: item.dueAt } : {}),
    })),
  };
}

function mapLabel(label: CardLabelDtoApi | GeneratedItemLabel) {
  return {
    id: label.labelId ?? "",
    name: label.name ?? "Label",
    color: label.color ?? memberColors[0]!,
  };
}

/** Older reads name the item `title`; the generated read names it `name`. */
function isLegacyItem<T extends object>(
  dto: T,
): dto is Extract<T, { title: string }> {
  return "title" in dto && typeof dto.title === "string";
}

function itemTitle(dto: AnyItemSummary | BoardItemReadDto): string {
  if (isLegacyItem(dto)) return dto.title;
  return (dto as GeneratedItemSummary).name ?? "";
}

type ItemReadSource = Pick<
  GeneratedItemSummary,
  | "version"
  | "itemKey"
  | "parentItemId"
  | "statusOptionId"
  | "priorityOptionId"
  | "dueAt"
  | "startedAt"
  | "completedAt"
  | "childProgress"
>;

/** The item values of the generated read; absent on older reads. */
function readItemValues(dto: object): ItemReadValues {
  const generated = dto as ItemReadSource;
  const progress = generated.childProgress;
  return {
    version: typeof generated.version === "number" ? generated.version : null,
    itemKey: generated.itemKey ?? null,
    parentItemId: generated.parentItemId ?? null,
    statusOptionId: generated.statusOptionId ?? null,
    priorityOptionId: generated.priorityOptionId ?? null,
    dueAt: generated.dueAt ?? null,
    startedAt: generated.startedAt ?? null,
    completedAt: generated.completedAt ?? null,
    childProgress: progress
      ? { done: progress.done ?? 0, total: progress.total ?? 0 }
      : null,
  };
}

function normalizeCategory(value: string): WorkStatusCategory | null {
  const normalized = value.trim().toLowerCase();
  if (
    normalized === "todo" ||
    normalized === "in_progress" ||
    normalized === "review" ||
    normalized === "done"
  )
    return normalized;
  return null;
}
/** The stored custom values the board and item reads carry, by field id. */
export function generatedFieldValues(dto: unknown): Record<string, unknown> {
  const value = (dto as { fieldValues?: unknown }).fieldValues;
  return value && typeof value === "object" && !Array.isArray(value)
    ? { ...(value as Record<string, unknown>) }
    : {};
}

/**
 * The Formula and Rollup results of a read, by field id. A result of an
 * unknown shape is dropped (the cell then shows nothing), never guessed.
 */
export function generatedComputedValues(
  dto: unknown,
): Record<string, ComputedValue> {
  const raw = (dto as { computedValues?: unknown }).computedValues;
  const result: Record<string, ComputedValue> = {};
  if (!raw || typeof raw !== "object") return result;
  for (const [fieldId, entry] of Object.entries(raw)) {
    const item = entry as {
      state?: string;
      value?: unknown;
      errorCode?: string | null;
    } | null;
    if (item?.state === "ok")
      result[fieldId] = { state: "ok", value: item.value };
    else if (item?.state === "empty") result[fieldId] = { state: "empty" };
    else if (item?.state === "error")
      result[fieldId] = {
        state: "error",
        errorCode: item.errorCode ?? "unknown",
      };
  }
  return result;
}

function parseFieldValues(
  value: CardSummaryDtoApi["fieldValues"] | CardDtoApi["fieldValues"],
): Record<string, unknown> {
  if (!value) return {};
  if (typeof value === "object") return value;
  try {
    const parsed = JSON.parse(value);
    return parsed && typeof parsed === "object" ? parsed : {};
  } catch {
    return {};
  }
}

function createDefaultFieldDefinitions(boardId: string): FieldDefinition[] {
  return [
    createField(boardId, "field-title", "Task", "text", 1),
    createField(boardId, "field-person", "Owner", "person", 2),
    createField(boardId, "field-status", "Status", "select", 3, statusOptions),
    createField(
      boardId,
      "field-priority",
      "Priority",
      "select",
      4,
      priorityOptions,
    ),
    createField(boardId, "field-due-date", "Due date", "date", 5),
    createField(boardId, "field-linked-page", "Linked doc", "linked_page", 6),
    createField(boardId, "field-progress", "Progress", "progress", 7),
  ];
}

function createField(
  boardId: string,
  suffix: string,
  name: string,
  fieldType: FieldDefinition["fieldType"],
  position: number,
  options: FieldOption[] = [],
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
  };
}

function mapBoardMember(
  member: NonNullable<BoardReadDto["members"]>[number],
  index: number,
): BoardMember {
  const name = member.name ?? "";
  return {
    id: `bm-${member.userId}`,
    userId: member.userId ?? "",
    name,
    initials: getInitials(name),
    role: normalizeMemberRole(member.role ?? ""),
    avatarUrl: member.avatar ?? undefined,
    color: memberColors[index % memberColors.length]!,
  };
}

function mapCardMember(
  member: CardMemberDtoApi | GeneratedItemMember,
  index: number,
): CardMember {
  const name = member.name ?? "";
  return {
    id: `cm-${member.userId}`,
    userId: member.userId ?? "",
    name,
    initials: getInitials(name),
    avatarUrl: member.avatar ?? undefined,
    color: memberColors[index % memberColors.length]!,
  };
}

function normalizeVisibility(value?: string | null): Board["visibility"] {
  const normalized = (value ?? "").trim().toLowerCase();
  if (
    normalized === "private" ||
    normalized === "workspace" ||
    normalized === "public"
  )
    return normalized;
  return "workspace";
}

function normalizePriority(value?: string | null): Card["priority"] {
  const normalized = value?.trim().toLowerCase();
  if (
    normalized === "urgent" ||
    normalized === "high" ||
    normalized === "medium" ||
    normalized === "low"
  )
    return normalized;
  return "medium";
}

function normalizeStatus(value: string) {
  const normalized = value
    .trim()
    .toLowerCase()
    .replaceAll("_", "-")
    .replaceAll(" ", "-");
  if (statusOptions.some((option) => option.id === normalized))
    return normalized;
  if (normalized.includes("working") || normalized.includes("progress"))
    return "status-working";
  if (normalized.includes("stuck") || normalized.includes("blocked"))
    return "status-stuck";
  if (normalized.includes("completed")) return "status-completed";
  if (normalized.includes("done")) return "status-done";
  return "status-not-started";
}

function normalizeFieldType(value: string): FieldDefinition["fieldType"] {
  const normalized = value.trim().toLowerCase().replaceAll("-", "_");
  if (
    normalized === "people" ||
    normalized === "user" ||
    normalized === "users"
  )
    return "person";
  if (normalized === "linked_doc" || normalized === "page")
    return "linked_page";
  // Backend field types: a Status field is option-backed like a Select.
  if (normalized === "status") return "select";
  if (normalized === "multiselect") return "multi_select";
  if (normalized === "longtext") return "long_text";
  if (normalized === "link" || normalized === "rollup" || normalized === "vote")
    return normalized;
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
  )
    return normalized;
  return "text";
}

function parseSettings(
  value: BoardColumnDtoApi["settings"],
): Record<string, unknown> {
  if (!value) return {};
  if (typeof value === "object") return value;
  try {
    const parsed = JSON.parse(value);
    return parsed && typeof parsed === "object" ? parsed : {};
  } catch {
    return {};
  }
}

function parseOptions(value: unknown, fieldName: string): FieldOption[] {
  if (!Array.isArray(value)) {
    const normalized = fieldName.trim().toLowerCase();
    if (normalized.includes("status")) return statusOptions;
    if (normalized.includes("priority")) return priorityOptions;
    return [];
  }

  return value.map((item, index) => {
    if (typeof item === "string") return optionFromLabel(item, index);
    if (item && typeof item === "object") {
      const option = item as Partial<FieldOption> & { value?: string };
      const label =
        option.label ?? option.value ?? option.id ?? `Option ${index + 1}`;
      return {
        id: option.id ?? normalizeOptionId(label),
        label,
        color: option.color ?? memberColors[index % memberColors.length]!,
      };
    }
    return optionFromLabel(`Option ${index + 1}`, index);
  });
}

function optionFromLabel(label: string, index: number): FieldOption {
  return {
    id: normalizeOptionId(label),
    label,
    color: memberColors[index % memberColors.length]!,
  };
}

function normalizeOptionId(value: string) {
  return value.trim().toLowerCase().replaceAll("_", "-").replace(/\s+/g, "-");
}

function normalizeOptionValue(
  value: string | undefined,
  options: FieldOption[],
  fallback: string | undefined,
) {
  if (!value) return fallback;
  const normalized = normalizeOptionId(value);
  return (
    options.find(
      (option) =>
        normalizeOptionId(option.id) === normalized ||
        normalizeOptionId(option.label) === normalized,
    )?.id ?? fallback
  );
}

function getSemanticField(field: FieldDefinition) {
  const normalizedName = field.name.trim().toLowerCase();
  if (
    normalizedName === "task" ||
    normalizedName === "title" ||
    normalizedName === "name"
  )
    return "title";
  if (
    normalizedName.includes("owner") ||
    normalizedName.includes("assignee") ||
    field.fieldType === "person"
  )
    return "person";
  if (normalizedName.includes("status")) return "status";
  if (normalizedName.includes("priority")) return "priority";
  if (normalizedName.includes("due") && normalizedName.includes("date"))
    return "due_date";
  if (
    normalizedName.includes("linked") &&
    (normalizedName.includes("doc") || normalizedName.includes("page"))
  )
    return "linked_page";
  if (normalizedName.includes("progress")) return "progress";
  return field.fieldType;
}

function normalizeFileSource(value: string): CardFile["source"] {
  const normalized = value.trim().toLowerCase();
  if (
    normalized === "upload" ||
    normalized === "r2" ||
    normalized === "s3" ||
    normalized === "link"
  )
    return normalized;
  return "link";
}

function normalizeActivityType(action: string): CardActivity["type"] {
  const normalized = action.trim().toLowerCase();
  if (normalized.includes("comment")) return "commented";
  if (normalized.includes("file") || normalized.includes("attachment"))
    return "file";
  if (normalized.includes("create")) return "created";
  if (normalized.includes("automation")) return "automation";
  if (normalized.includes("update") || normalized.includes("move"))
    return "updated";
  return "system";
}

function normalizeMemberRole(value: string): BoardMember["role"] {
  const normalized = value.trim().toLowerCase();
  if (normalized === "owner" || normalized === "admin") return "owner";
  if (normalized === "editor" || normalized === "member") return "editor";
  return "viewer";
}

function getInitials(name: string) {
  return personInitials(name);
}
