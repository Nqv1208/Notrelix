import { useItemRemoval } from "../../item-lifecycle/use-item-removal";
import {
  tableCollapsedGroupsKey,
  useKanbanCollapsedLanes,
} from "../kanban/use-kanban-collapsed-lanes";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { toast } from "sonner";
import { useIsMobile } from "@notrelix/ui-web";
import {
  useArchiveCard,
  useBulkUpdateItems,
  useAddFieldOption,
  useBoardTable,
  useMoveFieldOption,
  useUpdateFieldValue,
  useMoveItemToBoard,
  useRemoveFieldOption,
  useUpdateCard,
  useWorkspaceBoards,
  type KanbanWriteFailure,
  type BulkItemChange,
} from "@notrelix/work-management-state";
import {
  generatePosition,
  isOptimisticOptionId,
  orderByKey,
  groupableFields,
  NONE_VALUE,
  BULK_ITEM_LIMIT,
  projectTableLanes,
  serializeCompoundFilter,
  sortCardsByLevels,
  type Card,
  type FieldContext,
  type KanbanStatusLane,
  type UpdateFieldValueInput,
} from "@notrelix/work-management-core";
import type { ItemSheetGroup } from "../../card-detail/task-detail-types";
import { findItemFields } from "../../card-detail/item-field-editors";
import { useBoardCapabilities } from "../../shared/board-capabilities";
import {
  emptyKanbanFilter,
  isKanbanCardOverdue,
} from "../kanban/kanban-filter";
import { useBoardViewState } from "../../filters/use-board-view-state";
import { headerSortOf } from "./header-sort";
import { fillFiltersCopy } from "../../filters/filters.copy";
import { ViewStateBar } from "../../filters/view-state-bar";
import { KanbanSelectedItem } from "../kanban/kanban-view";
import { buildCardLink } from "../kanban/kanban-view-helpers";
import { useKanbanLaneCopy } from "../kanban/use-kanban-lane-copy";
import { CardCreatePopup } from "../../card-popup/create/card-create-popup";
import { MainTableError } from "./main-table-error";
import { fillTableCopy } from "./main-table.copy";
import { MainTableSkeleton } from "./main-table-skeleton";
import { MainTableSurface } from "./main-table-surface";
import { useNoGroupNotice } from "../../card-popup/create/use-no-group-notice";
import { TableSubItems } from "./table-sub-items";
import type { TableEditors } from "./table-cell-editors";
import type { TableFieldEditing } from "./table-field-cell";
import {
  buildTableGroup,
  tableStatusOptions,
  type TableGroupModel,
  type TableStatusOption,
} from "./table-model";
import { useAddGroup } from "../../status-option/use-add-group";
import { SavedFiltersControl } from "../../saved-filters/saved-filters-control";
import { useStatusOptionFormCopy } from "../../status-option/use-status-option-form-copy";
import type { TableGroupMenuModel } from "./table-group-menu";
import { useMainTableCopy } from "./use-main-table-copy";
import { useFieldCellWrites } from "./use-field-cell-writes";

type ParamChange = (
  value: string | undefined,
  options?: { replace?: boolean },
) => void;

/** How long "Hoàn tác" is offered before the archive is sent. */
export const ARCHIVE_UNDO_MS = 6000;

export interface MainTableViewProps {
  boardId: string;
  workspaceId: string;
  /** Card id from the `item` search parameter; unknown ids show nothing. */
  selectedItemId?: string;
  /** The signed-in member (from the host's session): offers "Giao cho tôi". */
  currentUserId?: string | null;
  onSelectedItemChange?: (
    id: string | undefined,
    options?: { replace?: boolean },
  ) => void;
  /** The route `filter` parameter (`serializeKanbanFilter` form). */
  filter?: string;
  /** Writes the route `filter` parameter; without it the view keeps the filter. */
  onFilterChange?: ParamChange;
  /** The route `groupBy` parameter: a field id (Status by default). */
  groupBy?: string;
  onGroupByChange?: (fieldId: string) => void;
}

/** The board group a new item of a group goes into. */
function listForNewItem(
  cards: readonly Card[],
  groups: readonly { id: string; position: number }[],
): string | undefined {
  const last = cards.at(-1)?.listId;
  if (last) return last;
  return [...groups].sort((a, b) => a.position - b.position)[0]?.id;
}

/** "YYYY-MM-DD" as an instant at 17:00 of that local day. */
function dueInstant(day: string): string {
  return new Date(`${day}T17:00:00`).toISOString();
}

/**
 * The table of a board: status groups (or groups of the Select field named
 * by `groupBy`) with the route filter, search, sort, hidden columns and row
 * height applied, inline status edit, bulk actions and the item side sheet.
 * What the viewer may do comes from the board access read. Archiving waits
 * for "Hoàn tác" to run out (the board has no restore write), then sends the
 * archive.
 */
export function MainTableView({
  boardId,
  workspaceId,
  selectedItemId,
  currentUserId,
  onSelectedItemChange,
  filter: filterParam,
  onFilterChange,
  groupBy: groupByParam,
  onGroupByChange,
}: MainTableViewProps) {
  // Read once so every row judges "overdue" and "updated" against one instant.
  const [now] = useState(() => new Date());
  const copy = useMainTableCopy();
  const laneCopy = useKanbanLaneCopy();

  const [localFilter, setLocalFilter] = useState<string | undefined>();
  const [localGroupBy, setLocalGroupBy] = useState<string | undefined>();
  const filterValue = onFilterChange ? filterParam : localFilter;
  const groupByValue = onGroupByChange ? groupByParam : localGroupBy;
  const setFilterValue: ParamChange = useCallback(
    (value, options) => {
      if (onFilterChange) onFilterChange(value, options);
      else setLocalFilter(value);
    },
    [onFilterChange],
  );
  const isOverdue = useCallback(
    (card: Card) => isKanbanCardOverdue(card, now),
    [now],
  );
  const setGroupByValue = useCallback(
    (fieldId: string) => {
      if (onGroupByChange) onGroupByChange(fieldId);
      else setLocalGroupBy(fieldId);
    },
    [onGroupByChange],
  );
  const noLegacyFilter = useMemo(() => emptyKanbanFilter(), []);
  const {
    copy: filtersCopy,
    view,
    kanbanView,
  } = useBoardViewState({
    boardId,
    workspaceId,
    routeFilter: filterValue,
    onRouteFilterChange: setFilterValue,
    routeGroupBy: groupByValue,
    onRouteGroupByChange: setGroupByValue,
  });

  const [showDone, setShowDone] = useState(false);
  const [createOpen, setCreateOpen] = useState(false);
  const [selection, setSelection] = useState<ReadonlySet<string>>(
    () => new Set(),
  );
  const collapsedGroups = useKanbanCollapsedLanes(
    tableCollapsedGroupsKey(workspaceId, boardId),
  );
  const [collapsed, setCollapsed] = useState<ReadonlySet<string>>(
    () => collapsedGroups.initial,
  );

  const titles = useRef(new Map<string, string>());
  // Each failure toast takes its own id: reusing one id swallows the next result toast.
  const toastAttempt = useRef(0);
  const onWriteError = useCallback(
    (failure: KanbanWriteFailure) => {
      const title =
        failure.title ?? titles.current.get(failure.cardId ?? "") ?? "";
      const message =
        failure.kind === "create"
          ? copy.failed.create
          : failure.kind === "status" || failure.kind === "move"
            ? copy.failed.status
            : copy.failed.save;
      toast.error(fillTableCopy(message, { title }), {
        id: `table-write-${failure.cardId ?? "create"}-${++toastAttempt.current}`,
      });
    },
    [copy],
  );

  const table = useBoardTable(boardId, workspaceId, {
    groupBy: view.applied.group?.fieldId,
    filter: noLegacyFilter,
    view: kanbanView,
    isOverdue,
    onWriteError,
  });
  const { capabilities } = useBoardCapabilities(boardId, workspaceId);
  const addOption = useAddFieldOption(boardId, workspaceId);
  const noGroupNotice = useNoGroupNotice(boardId, workspaceId);
  const { addGroup, isPending: addingGroup } = useAddGroup(
    boardId,
    workspaceId,
  );
  const optionFormCopy = useStatusOptionFormCopy();
  // The group a move or delete is about, named in the failure message.
  const optionWriteName = useRef("");
  const moveOption = useMoveFieldOption(boardId, workspaceId, {
    onError: () =>
      toast.error(
        fillTableCopy(copy.failed.moveGroup, { name: optionWriteName.current }),
        { id: `table-option-write-${++toastAttempt.current}` },
      ),
  });
  const removeOption = useRemoveFieldOption(boardId, workspaceId, {
    onError: () =>
      toast.error(
        fillTableCopy(copy.failed.deleteGroup, {
          name: optionWriteName.current,
        }),
        { id: "table-option-write" },
      ),
  });
  const updateCard = useUpdateCard(boardId, workspaceId);
  // Custom field cells keep their own saving, error and conflict state, so no toast here.
  const cellFieldValue = useUpdateFieldValue(boardId, workspaceId);
  const archiveCard = useArchiveCard(boardId, workspaceId, {
    onError: (_error, { cardId }) =>
      toast.error(
        fillTableCopy(copy.failed.archive, {
          title: titles.current.get(cardId) ?? "",
        }),
        { id: `table-write-${cardId}` },
      ),
  });
  const removal = useItemRemoval({
    workspaceId,
    boardId,
    titleOf: (itemId) =>
      allCards.find((candidate) => candidate.id === itemId)?.title ??
      titles.current.get(itemId) ??
      "",
    onGone: (itemId) => {
      if (selectedItemId === itemId)
        onSelectedItemChange?.(undefined, { replace: true });
    },
  });
  const bulkUpdate = useBulkUpdateItems(boardId, workspaceId);
  const bulkMutate = bulkUpdate.mutate;
  const moveItem = useMoveItemToBoard(boardId, workspaceId, {
    onError: (_error, { cardId }) =>
      toast.error(
        fillTableCopy(copy.failed.move, {
          title: titles.current.get(cardId) ?? "",
        }),
        { id: `table-write-${cardId}-${++toastAttempt.current}` },
      ),
  });
  const workspaceBoards = useWorkspaceBoards(workspaceId);
  const isNarrow = useIsMobile();
  const boardCovered = Boolean(selectedItemId) && isNarrow;

  const { board } = table;

  // Archive waits for the undo window: the rows leave the table at once, the
  // write goes out when the window ends, on the next archive, on close, or
  // when the table goes away.
  const [pendingArchive, setPendingArchive] = useState<readonly string[]>([]);
  const pendingRef = useRef<{ ids: string[]; timer: number } | null>(null);
  const archiveMutate = archiveCard.mutate;
  const flushArchive = useCallback(() => {
    const pending = pendingRef.current;
    if (!pending) return;
    window.clearTimeout(pending.timer);
    pendingRef.current = null;
    if (pending.ids.length > 1) {
      // Several rows go in one request; they stay hidden until it answers.
      bulkMutate(
        { itemIds: pending.ids, change: { type: "archive" } },
        {
          onError: () =>
            toast.error(copy.failed.bulk, {
              id: `table-bulk-${++toastAttempt.current}`,
            }),
          onSettled: () =>
            setPendingArchive((current) =>
              current === pending.ids ? [] : current,
            ),
        },
      );
      return;
    }
    for (const id of pending.ids) archiveMutate({ cardId: id });
    setPendingArchive([]);
  }, [archiveMutate, bulkMutate, copy]);
  useEffect(() => {
    window.addEventListener("pagehide", flushArchive);
    return () => {
      window.removeEventListener("pagehide", flushArchive);
      flushArchive();
    };
  }, [flushArchive]);
  const startArchive = (ids: string[]) => {
    flushArchive();
    if (ids.length === 0) return;
    const timer = window.setTimeout(flushArchive, ARCHIVE_UNDO_MS);
    pendingRef.current = { ids, timer };
    setPendingArchive(ids);
  };
  const undoArchive = () => {
    const pending = pendingRef.current;
    if (!pending) return;
    window.clearTimeout(pending.timer);
    pendingRef.current = null;
    setPendingArchive([]);
  };

  const statusField = table.statusField;
  const statusOptions = useMemo<TableStatusOption[]>(
    () =>
      statusField
        ? tableStatusOptions(
            orderByKey(statusField.options, (option) => option.position),
          )
        : [],
    [statusField],
  );
  const statusById = useMemo(
    () => new Map(statusOptions.map((option) => [option.id, option])),
    [statusOptions],
  );
  const statusRank = useCallback(
    (card: Card) =>
      statusOptions.findIndex((option) => option.id === card.statusOptionId),
    [statusOptions],
  );

  const isStatusGrouping =
    !table.flatGroupField &&
    (!table.groupByField || table.groupByField.id === table.statusField?.id);
  const groupField = table.groupByField ?? table.statusField;
  // The groups that are options of the grouped field, in the field's order
  // (search, filter and "hide finished" do not change which options exist).
  const optionLanes = useMemo(
    () => table.allLanes.filter((lane) => lane.optionId !== null),
    [table.allLanes],
  );
  const groupMenu = (
    group: TableGroupModel,
  ): TableGroupMenuModel | undefined => {
    if (!capabilities.canEditFields || !groupField || table.flatGroupField)
      return undefined;
    if (group.id === "none" || isOptimisticOptionId(group.id)) return undefined;
    const index = optionLanes.findIndex((lane) => lane.optionId === group.id);
    if (index < 0) return undefined;
    const subject = isStatusGrouping ? "status" : "value";
    const required = group.category === "todo" || group.category === "done";
    const hasSibling = optionLanes.some(
      (lane) => lane.optionId !== group.id && lane.category === group.category,
    );
    const blockedReason =
      subject === "value"
        ? optionLanes.length <= 1
          ? optionFormCopy.removeBlockedValue
          : undefined
        : required && !hasSibling
          ? optionFormCopy.removeBlockedStatus
          : undefined;
    return {
      formCopy: optionFormCopy,
      subject,
      canMoveUp: index > 0,
      canMoveDown: index < optionLanes.length - 1,
      pending: moveOption.isPending || removeOption.isPending,
      blockedReason,
      choices: optionLanes
        .filter((lane) => lane.optionId !== group.id)
        .map((lane) => ({
          id: lane.optionId!,
          name: lane.name,
          color: lane.color,
        })),
      itemCount: group.total,
      onMove: (direction) => {
        optionWriteName.current = group.name;
        moveOption.mutate({
          fieldId: groupField.id,
          optionId: group.id,
          direction,
        });
      },
      onDelete: (replacementOptionId) => {
        optionWriteName.current = group.name;
        removeOption.mutate({
          fieldId: groupField.id,
          optionId: group.id,
          replacementOptionId,
        });
      },
    };
  };
  const hiddenByArchive = useMemo(
    () => new Set(pendingArchive),
    [pendingArchive],
  );
  const lanes = useMemo<KanbanStatusLane[]>(
    () =>
      table.lanes.map((lane) => {
        const cards = lane.cards.filter(
          (card) => !hiddenByArchive.has(card.id),
        );
        const removed = lane.cards.length - cards.length;
        return { ...lane, cards, total: lane.total - removed };
      }),
    [table.lanes, hiddenByArchive],
  );
  // The header marks what orders the rows: the member's first sort level when
  // they have one (none when it is a field without a column), else the shared sort.
  const personalSorts = view.applied.sorts;
  const effectiveSort =
    personalSorts.length > 0
      ? headerSortOf(
          personalSorts[0]!,
          table.filterFields,
          new Set(table.customFields.map((field) => field.id)),
        )
      : table.sort;
  const sharedSortSummary = fillFiltersCopy(
    table.sort.direction === "asc"
      ? filtersCopy.sort.summaryAsc
      : filtersCopy.sort.summaryDesc,
    {
      field:
        table.customFields.find((field) => field.id === table.sort.key)?.name ??
        copy.columns[table.sort.key as keyof typeof copy.columns],
    },
  );
  const sortableFields = useMemo(() => {
    const people = table.board?.members ?? [];
    return new Map<string, FieldContext>(
      table.customFields.map((field) => [
        field.id,
        {
          field,
          memberName: (userId: string) =>
            people.find((member) => member.userId === userId)?.name ?? userId,
        },
      ]),
    );
  }, [table.customFields, table.board?.members]);
  const projection = useMemo(
    () =>
      projectTableLanes({
        lanes,
        sort: table.sort,
        hideDone: !showDone,
        statusRank,
        isOverdue,
        narrowed: table.filterActive,
        customFields: sortableFields,
        ...(view.applied.sorts.length > 0
          ? {
              orderCards: (cards: Card[]) =>
                sortCardsByLevels(
                  cards,
                  view.applied.sorts,
                  table.filterFields,
                  table.memberName,
                ),
            }
          : {}),
      }),
    [
      lanes,
      table.sort,
      showDone,
      statusRank,
      isOverdue,
      table.filterActive,
      sortableFields,
      view.applied.sorts,
      table.filterFields,
      table.memberName,
    ],
  );
  const groups = useMemo(
    () =>
      projection.groups.map((lane) =>
        buildTableGroup(
          lane,
          lane.name ||
            (isStatusGrouping ? copy.group.noStatus : copy.group.noValue),
          {
            now,
            copy,
            statuses: statusById,
            customFields: table.customFields,
          },
        ),
      ),
    [
      projection.groups,
      isStatusGrouping,
      copy,
      now,
      statusById,
      table.customFields,
    ],
  );

  const allCards = useMemo(
    () => table.allLanes.flatMap((lane) => lane.cards),
    [table.allLanes],
  );
  const visibleIds = useMemo(
    () => new Set(groups.flatMap((group) => group.rows.map((row) => row.id))),
    [groups],
  );
  const selectedIds = useMemo(
    () => new Set([...selection].filter((id) => visibleIds.has(id))),
    [selection, visibleIds],
  );
  const noteTitle = (cardId: string) => {
    const card = allCards.find((candidate) => candidate.id === cardId);
    if (card) titles.current.set(cardId, card.title);
  };

  const orderedGroupIds = useMemo(
    () =>
      [...table.groups]
        .sort((a, b) => a.position - b.position)
        .map((group) => ({ id: group.id, title: group.title })),
    [table.groups],
  );

  const sendCellValue = cellFieldValue.mutateAsync;
  const cellWrites = useFieldCellWrites(
    useCallback(
      (cardId: string, fieldDefinitionId: string, value: unknown) =>
        sendCellValue({ cardId, fieldDefinitionId, value }),
      [sendCellValue],
    ),
    table.refetch,
  );

  const tableRef = useRef<HTMLDivElement>(null);
  const restoreFocus = useCallback((cardId: string) => {
    const root = tableRef.current;
    if (!root) return;
    const opener = Array.from(
      root.querySelectorAll<HTMLElement>("[data-row-id]"),
    ).find((element) => element.dataset.rowId === cardId);
    (opener ?? root).focus();
  }, []);

  if (table.isLoading) return <MainTableSkeleton copy={copy} />;
  if (table.error || !board) return <MainTableError copy={copy} />;

  const writeStatus = (cardId: string, statusOptionId: string) => {
    noteTitle(cardId);
    table.updateStatus.mutate({ cardId, statusOptionId });
  };

  const selectedList = [...selectedIds];
  const writeToSelected = (write: (cardId: string) => void) => {
    for (const id of selectedList) write(id);
    setSelection(new Set());
  };
  // Status, due date and assignee of the selected rows: one request, all or none.
  const bulkWrite = (change: BulkItemChange) => {
    for (const id of selectedList) noteTitle(id);
    bulkMutate(
      { itemIds: selectedList, change },
      {
        onError: () =>
          toast.error(copy.failed.bulk, {
            id: `table-bulk-${++toastAttempt.current}`,
          }),
      },
    );
    setSelection(new Set());
  };

  const itemFields = findItemFields(board);
  const members = board.members.map((member) => ({
    userId: member.userId,
    name: member.name,
    initials: member.initials,
  }));
  const writeAssignee = (cardId: string, userId: string | null) => {
    if (!itemFields.person) return;
    noteTitle(cardId);
    table.updateFieldValue.mutate({
      cardId,
      fieldDefinitionId: itemFields.person.id,
      value: userId ? [userId] : [],
    });
  };
  const writeDue = (cardId: string, day: string | null) => {
    noteTitle(cardId);
    updateCard.mutate({
      cardId,
      patch: { dueDate: day ? dueInstant(day) : null },
    });
  };
  const editors: TableEditors | undefined = itemFields.person
    ? {
        members,
        priorities: (itemFields.priority?.options ?? []).map((option) => ({
          id: option.id,
          label: option.label,
        })),
        onAssignee: writeAssignee,
        onDue: writeDue,
        onPriority: (cardId, optionId) => {
          if (!itemFields.priority) return;
          noteTitle(cardId);
          table.updateFieldValue.mutate({
            cardId,
            fieldDefinitionId: itemFields.priority.id,
            value: optionId,
          });
        },
      }
    : undefined;
  const fieldEditing: TableFieldEditing = {
    canEdit: capabilities.canEditFields,
    members,
    writeOf: cellWrites.writeOf,
    onWrite: (cardId, fieldDefinitionId, value, stale) => {
      noteTitle(cardId);
      cellWrites.write(cardId, fieldDefinitionId, value, stale);
    },
    onRetry: cellWrites.retry,
    onKeepMine: cellWrites.keepMine,
    onUseTheirs: cellWrites.takeTheirs,
  };
  const moveTargets = (workspaceBoards.data ?? [])
    .filter(
      (candidate) =>
        candidate.id !== boardId &&
        !candidate.isArchived &&
        candidate.canAddItems !== false,
    )
    .map((candidate) => ({ id: candidate.id, title: candidate.title }));

  const personFieldId = table.filterFields.find(
    (candidate) => candidate.field.fieldType === "person",
  )?.id;
  const assigneeCondition = view.applied.filter.conditions.find(
    (condition) =>
      condition.fieldId === personFieldId &&
      condition.operator === "has_any" &&
      condition.values.length === 1 &&
      condition.values[0] !== NONE_VALUE,
  );
  const singleAssigneeName = assigneeCondition
    ? table.filterChoices.assignees.find(
        (choice) => choice.id === assigneeCondition.values[0],
      )?.name
    : undefined;

  const handleCreate = (groupId: string, title: string) => {
    if (!capabilities.canCreateCard) {
      noGroupNotice.noPermission();
      return;
    }
    const lane = table.lanes.find(
      (candidate) => (candidate.optionId ?? "none") === groupId,
    );
    const listId = lane ? listForNewItem(lane.cards, table.groups) : undefined;
    if (!lane || !listId) {
      noGroupNotice.noGroup();
      return;
    }
    const fieldValue =
      !isStatusGrouping &&
      !table.flatGroupField &&
      table.groupByField &&
      lane.optionId
        ? { fieldDefinitionId: table.groupByField.id, value: lane.optionId }
        : null;
    table.createItemInLane.mutate({
      listId,
      title,
      statusOptionId: isStatusGrouping ? lane.optionId : null,
      fieldValue,
    });
  };

  const handleSheetFieldValue = (payload: UpdateFieldValueInput) => {
    noteTitle(payload.cardId);
    if (
      payload.fieldDefinitionId === table.statusField?.id &&
      typeof payload.value === "string"
    ) {
      return table.updateStatus.mutateAsync({
        cardId: payload.cardId,
        statusOptionId: payload.value,
      });
    }
    return table.updateFieldValue.mutateAsync(payload);
  };

  const handleMoveToGroup = (cardId: string, groupId: string) => {
    const target = table.groups.find((group) => group.id === groupId);
    if (!target) return;
    const last = [...target.cards]
      .filter((card) => card.id !== cardId)
      .sort((a, b) => a.position - b.position)
      .at(-1);
    noteTitle(cardId);
    table.moveCard.mutate({
      cardId,
      listId: groupId,
      position: generatePosition(last?.position, undefined),
      previousSiblingId: last?.id ?? null,
      nextSiblingId: null,
    });
  };

  const handleCopyLink = (cardId: string) => {
    const link = buildCardLink(window.location, cardId);
    void Promise.resolve()
      .then(() => navigator.clipboard.writeText(link))
      .then(
        () => toast.success(laneCopy.linkCopied),
        () => toast.error(laneCopy.linkCopyFailed),
      );
  };

  const sheetGroups: ItemSheetGroup[] = orderedGroupIds;

  return (
    <div
      ref={tableRef}
      data-main-table=""
      tabIndex={-1}
      className="relative h-full min-h-0 focus:outline-hidden"
    >
      <div
        className="h-full"
        inert={boardCovered || undefined}
        aria-hidden={boardCovered || undefined}
      >
        <MainTableSurface
          onNoGroup={noGroupNotice.noGroup}
          renderSubItems={(rowId) => (
            <TableSubItems
              itemId={rowId}
              workspaceId={workspaceId}
              boardId={boardId}
              copy={copy}
              onOpen={(id) => onSelectedItemChange?.(id)}
            />
          )}
          boardTitle={board.title}
          toolbar={{
            copy,
            searchQuery: table.search.query,
            onSearchChange: table.search.setQuery,
            hideDone: !showDone,
            onShowDone: () => setShowDone(true),
            sort: effectiveSort,
            viewBar: (
              <ViewStateBar
                canSave={capabilities.canEditFields}
                copy={filtersCopy}
                view={view}
                fields={table.filterFields}
                memberName={table.memberName}
                people={table.filterChoices.assignees}
                counts={{
                  shown: table.visibleCount,
                  total: table.totalCount,
                }}
                now={now}
                sort
                sortFallback={sharedSortSummary}
                groupFields={groupableFields(table.filterFields)}
                groupFallback={
                  table.groupByField
                    ? { fieldId: table.groupByField.id }
                    : undefined
                }
                savedFilters={(save) => (
                  <SavedFiltersControl
                    workspaceId={workspaceId}
                    boardId={boardId}
                    filter={serializeCompoundFilter(view.applied.filter)}
                    groupBy={view.state.group?.fieldId ?? null}
                    canEdit={capabilities.canEditFields}
                    onApply={view.applyCriteria}
                    {...save}
                  />
                )}
              />
            ),
            customFields: table.customFields,
            columnOrder: table.columnOrder,
            onColumnOrderChange: table.setColumnOrder,
            columnState: {
              personal: table.columnsPersonal,
              canSaveToView: capabilities.canEditFields,
              savingToView: table.savingColumnsToView,
              onSaveToView: table.saveColumnsToView,
              onReset: table.resetColumns,
              saveFailed: table.columnsSaveFailed,
              onRetrySave: table.retryColumnsSave,
              saveToViewFailed: table.saveColumnsToViewFailed,
            },
            hiddenColumnIds: table.hiddenColumnIds,
            onColumnVisibleChange: table.setColumnVisible,
            density: table.density,
            onDensityChange: table.setDensity,
            canAdd: capabilities.canCreateCard,
          }}
          groups={groups}
          statusOptions={statusOptions}
          addGroup={
            capabilities.canEditFields && groupField && !table.flatGroupField
              ? {
                  formCopy: optionFormCopy,
                  kind: isStatusGrouping ? "status" : "value",
                  existingNames: groupField.options.map(
                    (option) => option.label,
                  ),
                  pending: addOption.isPending,
                  onSubmit: async (draft) => {
                    try {
                      await addOption.mutateAsync({
                        fieldId: groupField.id,
                        name: draft.name,
                        color: draft.color,
                        category: isStatusGrouping ? draft.category : undefined,
                      });
                    } catch (error) {
                      toast.error(
                        fillTableCopy(copy.failed.addGroup, {
                          name: draft.name,
                        }),
                      );
                      throw error;
                    }
                  },
                  boardGroup: capabilities.canCreateCard
                    ? {
                        existingTitles: table.groups.map(
                          (group) => group.title,
                        ),
                        pending: addingGroup,
                        onSubmit: (draft) => addGroup(draft),
                      }
                    : undefined,
                }
              : undefined
          }
          groupMenu={groupMenu}
          columnWidths={table.columnWidths}
          onColumnWidthChange={table.setColumnWidth}
          onColumnWidthReset={table.resetColumnWidth}
          selectedIds={selectedIds}
          collapsedGroupIds={collapsed}
          openItemId={selectedItemId}
          canEdit={capabilities.canEditFields}
          editors={editors}
          fieldEditing={fieldEditing}
          onRename={
            capabilities.canEditFields
              ? (cardId, title) => {
                  noteTitle(cardId);
                  updateCard.mutate({ cardId, patch: { title } });
                }
              : undefined
          }
          canCreate={capabilities.canCreateCard}
          onOpenCreateDialog={() => setCreateOpen(true)}
          canArchive={capabilities.canDelete}
          paging={
            table.paging
              ? {
                  boardTotal: table.boardTotals.total,
                  boardFiltered:
                    table.boardTotals.filtered < table.boardTotals.total
                      ? projection.visibleCount
                      : null,
                  loadMore: (groupId) =>
                    void table.loadMoreLane(
                      groupId === "none" ? null : groupId,
                    ),
                  loading: table.lanePages.loading,
                  failed: table.lanePages.failed,
                }
              : undefined
          }
          shown={projection.visibleCount}
          hidden={projection.hiddenCount}
          canShowDone={!showDone && projection.doneHiddenCount > 0}
          noResults={groups.length === 0 && projection.totalCount > 0}
          noResultsAssignee={singleAssigneeName}
          archivedNotice={
            pendingArchive.length > 0
              ? {
                  count: pendingArchive.length,
                  onUndo: undoArchive,
                  onClose: flushArchive,
                }
              : null
          }
          onRowSelectedChange={(id, selected) =>
            setSelection((current) => {
              const next = new Set(current);
              if (selected) next.add(id);
              else next.delete(id);
              return next;
            })
          }
          onSelectAllChange={(selectAll) =>
            setSelection(
              selectAll
                ? new Set([...visibleIds].slice(0, BULK_ITEM_LIMIT))
                : new Set(),
            )
          }
          onClearSelection={() => setSelection(new Set())}
          onGroupToggle={(groupId) => {
            const next = new Set(collapsed);
            if (next.has(groupId)) next.delete(groupId);
            else next.add(groupId);
            setCollapsed(next);
            collapsedGroups.onChange(next);
          }}
          onOpenItem={(id) => onSelectedItemChange?.(id)}
          onStatusChange={writeStatus}
          onCreateItem={handleCreate}
          onClearFilter={() => {
            view.setFilter({ ...view.state.filter, conditions: [] });
            table.search.setQuery("");
            setShowDone(true);
          }}
          onShowDone={() => setShowDone(true)}
          onBulkStatus={(optionId) =>
            bulkWrite({ type: "set-status", statusOptionId: optionId })
          }
          onBulkDue={(day) =>
            bulkWrite(
              day
                ? { type: "set-due-date", dueDate: dueInstant(day) }
                : { type: "clear-due-date" },
            )
          }
          rowActions={{
            onDuplicate: capabilities.canCreateCard
              ? removal.requestDuplicate
              : undefined,
            onCopyLink: handleCopyLink,
            // A row archives with the table's own undo window, not a dialog.
            onArchive: capabilities.canDelete
              ? (id) => {
                  noteTitle(id);
                  startArchive([id]);
                  setSelection(new Set());
                }
              : undefined,
            onDelete: capabilities.canDelete
              ? removal.requestDelete
              : undefined,
            onMoveToBoard: capabilities.canMoveCard
              ? removal.requestMove
              : undefined,
          }}
          bulkAssign={
            editors
              ? {
                  members,
                  onAssign: (userId) =>
                    bulkWrite(
                      userId
                        ? { type: "assign", userId }
                        : { type: "unassign-all" },
                    ),
                }
              : undefined
          }
          bulkMove={
            capabilities.canEditFields
              ? {
                  boards: moveTargets,
                  onMove: (targetBoardId) =>
                    writeToSelected((id) => {
                      noteTitle(id);
                      moveItem.mutate({ cardId: id, targetBoardId });
                    }),
                }
              : undefined
          }
          onBulkDelete={
            capabilities.canDelete
              ? () => bulkWrite({ type: "delete" })
              : undefined
          }
          bulkPriority={
            itemFields.priority
              ? {
                  options: itemFields.priority.options.map((option) => ({
                    id: option.id,
                    label: option.label,
                  })),
                  onPriority: (optionId) =>
                    bulkWrite({
                      type: "set-priority",
                      priorityOptionId: optionId,
                    }),
                }
              : undefined
          }
          onBulkArchive={() => {
            for (const id of selectedList) noteTitle(id);
            startArchive(selectedList);
            setSelection(new Set());
          }}
        />
      </div>

      {board && capabilities.canCreateCard ? (
        <CardCreatePopup
          open={createOpen}
          onOpenChange={setCreateOpen}
          workspaceId={workspaceId}
          board={board}
          groups={table.groups}
          defaultGroupId={table.groups[0]?.id ?? ""}
          defaultStatusId={null}
          currentUserId={currentUserId}
          onOpenItem={(id) => onSelectedItemChange?.(id)}
        />
      ) : null}
      {selectedItemId ? (
        <KanbanSelectedItem
          board={board}
          cardId={selectedItemId}
          capabilities={capabilities}
          groups={sheetGroups}
          onClose={() => onSelectedItemChange?.(undefined, { replace: true })}
          onRestoreFocus={restoreFocus}
          onCopyLink={handleCopyLink}
          onMoveToGroup={handleMoveToGroup}
          onUpdateFieldValue={handleSheetFieldValue}
          onDuplicate={removal.requestDuplicate}
        />
      ) : null}
      {removal.dialogs}
    </div>
  );
}
