import type { OperationResponse } from "@notrelix/contracts";
import type {
  Board,
  BoardColumnDtoApi,
  BoardDtoApi,
  BoardGroup,
  BoardMember,
  Card,
  CardDetail,
  CardDtoApi,
  CardLabelDtoApi,
  CardMember,
  CardMemberDtoApi,
  CardUpdate,
  CardFile,
  CardActivity,
  CardSummaryDtoApi,
  Checklist,
  CommentDtoApi,
  FieldDefinition,
  FieldOption,
  FieldSemanticRole,
  FullBoardDtoApi,
  FullBoardResponse,
  ActivityLogResponseApi,
  AttachmentDtoApi,
  ListDtoApi,
  WorkStatusCategory,
} from "../types";
import type { KanbanStatusLane } from "../types/kanban-types";

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
type GeneratedChecklist = NonNullable<GeneratedBoardItemDto["checklists"]>[number];

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
    isArchived: false,
    linkedPageId: undefined,
    fieldDefinitions,
    members,
    createdAt: EPOCH_ISO,
  };

  const lists: AnyGroup[] = (dto.lists ?? []).filter(
    (list) => list.isArchived !== true,
  );
  return {
    board,
    fieldDefinitions,
    groups: orderByKey(lists, (list) => list.position).map((list, index) =>
      mapListDto(list, index, board, fieldDefinitions),
    ),
  };
}

export function mapCardDto(dto: BoardItemReadDto): CardDetail {
  const members = (dto.members ?? []).map(mapCardMember);
  const values = readItemValues(dto);
  const legacy = isLegacyItem(dto) ? dto : null;
  const status = legacy
    ? normalizeStatus(legacy.status)
    : (values.statusOptionId ?? "");
  const dueDate = legacy ? legacy.dueDate : values.dueAt;
  const checklists = (dto.checklists ?? []).map(mapChecklist);
  const card: CardDetail = {
    id: dto.id ?? "",
    listId: legacy ? legacy.listId : ((dto as GeneratedBoardItemDto).groupId ?? ""),
    boardId: dto.boardId ?? "",
    workspaceId: dto.workspaceId ?? "",
    title: itemTitle(dto),
    descriptionMd: legacy ? (legacy.descriptionMd ?? "") : "",
    linkedPageId: legacy ? (legacy.linkedPageId ?? undefined) : undefined,
    position: typeof dto.position === "number" ? dto.position : 0,
    priority: legacy ? normalizePriority(legacy.priority) : undefined,
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
    fieldValues: legacy ? parseFieldValues(legacy.fieldValues) : {},
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
    itemKey: values.itemKey,
    parentItemId: values.parentItemId,
    statusOptionId: legacy ? status || null : values.statusOptionId,
    dueAt: dueDate ?? null,
    childProgress: values.childProgress,
    boardTitle: "",
    watchers: members,
    isWatched: false,
    updates: [],
    files: [],
    activity: [],
  };

  return card;
}

// ─── Board access read ───────────────────────────────────────────────────────

export type BoardAccessLevel = "viewer" | "commenter" | "editor" | "manager";

/** Where the caller's access comes from (`mySource.kind`). */
export type BoardAccessSourceKind =
  | "owner"
  | "workspace-admin"
  | "space"
  | "team"
  | "explicit"
  | "guest"
  | "workspace";

/** A group of people who can open the board (`sources[]`). */
export interface BoardAccessSource {
  readonly kind: "space" | "team" | "user" | "guest";
  /** Present only when the caller may see the subject. */
  readonly id: string | null;
  /** Present only when the caller may see the subject. */
  readonly name: string | null;
  readonly count: number;
}

/**
 * The caller's access to a board and who else can open it. `peopleCount`
 * includes guests; when `peopleCountComplete` is false it is a lower bound
 * (sources the backend cannot enumerate yet are missing).
 */
export interface BoardAccess {
  readonly boardId: string;
  readonly version: number;
  readonly myLevel: BoardAccessLevel;
  readonly mySource: {
    readonly kind: BoardAccessSourceKind;
    readonly grantedBy: { readonly userId: string; readonly name: string } | null;
  };
  readonly peopleCount: number;
  readonly guestCount: number;
  readonly peopleCountComplete: boolean;
  readonly sources: readonly BoardAccessSource[];
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
      kind:
        ACCESS_SOURCE_KINDS.find((value) => value === kind) ?? "explicit",
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

// ─── Status lanes ────────────────────────────────────────────────────────────

/** The board's Status field: the mapped one, else a field named "Status". */
export function findStatusField(
  fields: readonly FieldDefinition[],
): FieldDefinition | undefined {
  return (
    fields.find((field) => field.semanticRole === "status") ??
    fields.find(
      (field) =>
        field.id.endsWith("field-status") ||
        normalizeName(field.name) === "status" ||
        normalizeName(field.name) === "trang_thai",
    )
  );
}

/** The board's priority field (first Select named like a priority). */
export function findPriorityField(
  fields: readonly FieldDefinition[],
): FieldDefinition | undefined {
  return (
    fields.find((field) => field.semanticRole === "priority") ??
    fields.find(
      (field) =>
        field.fieldType === "select" && PRIORITY_FIELD_NAMES.has(normalizeName(field.name)),
    )
  );
}

/**
 * Maps a priority option's name to the card's priority. Vietnamese and English
 * names, case- and diacritics-insensitive; anything else has no priority.
 */
export function priorityFromOptionName(
  name: string | null | undefined,
): Card["priority"] {
  if (!name) return undefined;
  return PRIORITY_BY_OPTION_NAME[normalizeName(name)];
}

/**
 * Groups the board's top-level cards into one lane per Status option, in the
 * options' order. Child items and archived items are never lanes' cards; a
 * lane for items without a known status is added last when there are any.
 */
export function groupCardsByStatus(board: {
  readonly fieldDefinitions: readonly FieldDefinition[];
  readonly groups: readonly BoardGroup[];
}): KanbanStatusLane[] {
  const statusField = findStatusField(board.fieldDefinitions);
  const options = statusField ? orderOptions(statusField.options) : [];
  const cardsByOption = new Map<string, Card[]>(
    options.map((option) => [option.id, []]),
  );
  const withoutStatus: Card[] = [];

  for (const group of board.groups) {
    for (const card of group.cards) {
      if (card.parentItemId || card.isArchived || card.isDeleted) continue;
      const optionId = card.statusOptionId ?? undefined;
      const lane = optionId === undefined ? undefined : cardsByOption.get(optionId);
      if (lane) lane.push(card);
      else withoutStatus.push(card);
    }
  }

  const lanes: KanbanStatusLane[] = options.map((option, index) => {
    const cards = cardsByOption.get(option.id) ?? [];
    return {
      optionId: option.id,
      name: option.label,
      category: option.category ?? inferStatusCategory(option.label, index),
      color: option.color || null,
      cards,
      total: cards.length,
    };
  });
  if (withoutStatus.length > 0) {
    lanes.push({
      optionId: null,
      name: "",
      category: null,
      color: null,
      cards: withoutStatus,
      total: withoutStatus.length,
    });
  }
  return lanes;
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
    attachmentIds: [],
    createdAt: dto.createdAt,
    updatedAt: dto.isEdited ? dto.createdAt : undefined,
  };
}

export function mapAttachmentDtoToCardFile(
  dto: AttachmentDtoApi,
  cardId: string,
): CardFile {
  const name = dto.uploadedByName ?? "Unknown";
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
    actor: item.actorName ?? "Unknown",
    action: item.action,
    type: normalizeActivityType(item.action),
    metadata: item.resourceTitle
      ? { resourceTitle: item.resourceTitle }
      : undefined,
    createdAt: item.createdAt,
  }));
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
  const customValues = legacy ? parseFieldValues(legacy.fieldValues) : {};
  const title = itemTitle(dto);

  // Legacy reads carry a status/priority word; generated reads carry option ids.
  const status = legacy
    ? normalizeStatus(legacy.status)
    : (values.statusOptionId ?? "");
  const legacyPriority = legacy ? normalizePriority(legacy.priority) : undefined;
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
    ? (normalizeOptionValue(legacy.status, statusField?.options ?? [], status) ??
      null)
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
      if (field.id === statusField?.id) return [field.id, statusOptionId ?? undefined];
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
    _count: {
      comments: dto.commentCount ?? 0,
      attachments: dto.attachmentCount ?? 0,
      checklistItems: checklistTotal,
    },
    createdAt:
      (legacy ? undefined : (dto as GeneratedItemSummary).createdAt) ??
      EPOCH_ISO,
    updatedAt:
      (legacy ? undefined : (dto as GeneratedItemSummary).updatedAt) ??
      undefined,
    itemKey: values.itemKey,
    parentItemId: values.parentItemId,
    statusOptionId,
    statusCategory,
    dueAt: dueDate,
    childProgress: values.childProgress,
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
    ...(normalizeName(rawType) === "status"
      ? { semanticRole: "status" as const }
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
      PRIORITY_FIELD_NAMES.has(normalizeName(field.name)),
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

function mapChecklist(
  checklist: CardDtoApi["checklists"][number] | GeneratedChecklist,
  index: number,
): Checklist {
  return {
    id: checklist.id ?? "",
    title: checklist.title ?? "",
    position:
      typeof checklist.position === "number" ? checklist.position : index + 1,
    items: (checklist.items ?? []).map((item, itemIndex) => ({
      id: item.id ?? "",
      title: item.title ?? "",
      isDone:
        "isChecked" in item
          ? item.isChecked
          : ["done", "completed", "checked"].includes(
              (item.status ?? "").trim().toLowerCase(),
            ),
      position: typeof item.position === "number" ? item.position : itemIndex + 1,
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

/**
 * Orders by a position key. Generated reads carry fractional-index strings
 * (compared by code unit, as the backend orders them); older reads carry
 * numbers. Ties keep the incoming order.
 */
function orderByKey<T>(
  values: readonly T[],
  keyOf: (value: T) => string | number | null | undefined,
): T[] {
  return values
    .map((value, index) => ({ value, index, key: keyOf(value) }))
    .sort((a, b) => compareKeys(a.key, b.key) || a.index - b.index)
    .map((entry) => entry.value);
}

function compareKeys(
  a: string | number | null | undefined,
  b: string | number | null | undefined,
): number {
  if (a == null || b == null) return a == null ? (b == null ? 0 : 1) : -1;
  if (typeof a === "number" && typeof b === "number") return a - b;
  const left = String(a);
  const right = String(b);
  return left < right ? -1 : left > right ? 1 : 0;
}

function orderOptions(options: readonly FieldOption[]): FieldOption[] {
  return orderByKey(options, (option) => option.position);
}

const PRIORITY_FIELD_NAMES: ReadonlySet<string> = new Set([
  "priority",
  "uu_tien",
  "do_uu_tien",
  "muc_uu_tien",
]);

const PRIORITY_BY_OPTION_NAME: Readonly<
  Record<string, NonNullable<Card["priority"]>>
> = {
  khan: "urgent",
  urgent: "urgent",
  cao: "high",
  high: "high",
  trung_binh: "medium",
  medium: "medium",
  thap: "low",
  low: "low",
};

/** Lower-case, without diacritics, words joined by "_". */
function normalizeName(value: string): string {
  return value
    .normalize("NFD")
    .replace(/[̀-ͯ]/g, "")
    .replace(/đ/gi, "d")
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "_")
    .replace(/^_+|_+$/g, "");
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

/** Category of a status option without one (older reads), from its name. */
function inferStatusCategory(label: string, index: number): WorkStatusCategory {
  const name = normalizeName(label);
  if (/(^|_)(done|completed?|hoan_thanh|xong)($|_)/.test(name)) return "done";
  if (/(^|_)(review|cho_duyet|dang_duyet)($|_)/.test(name)) return "review";
  if (index === 0 || /(not_started|to_do|todo|can_lam)/.test(name))
    return "todo";
  return "in_progress";
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
  if (normalized === "longtext") return "text";
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
  const parts = name.trim().split(/\s+/).filter(Boolean);
  if (parts.length === 0) return "?";
  if (parts.length === 1) return parts[0]!.slice(0, 2).toUpperCase();
  return `${parts[0]![0]}${parts[parts.length - 1]![0]}`.toUpperCase();
}
