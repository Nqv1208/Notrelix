import {
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
} from "react";
import { toast } from "sonner";
import { useIsMobile } from "@notrelix/ui-web";
import {
  dropUnknownFilterIds,
  useArchiveCard,
  useBoardAccess,
  useAddFieldOption,
  useBoardTable,
  useDuplicateCard,
  useMoveItemToBoard,
  useUpdateCard,
  useWorkspaceBoards,
  type KanbanWriteFailure,
} from "@notrelix/work-management-state";
import {
  generatePosition,
  orderByKey,
  projectTableLanes,
  type Card,
  type KanbanStatusLane,
  type UpdateFieldValueInput,
} from "@notrelix/work-management-core";
import { BoardHeaderDataContext } from "../../board-layout/board-header-data-context";
import type {
  ItemSheetGroup,
  ItemSheetMark,
} from "../../card-detail/task-detail-types";
import { findItemFields } from "../../card-detail/item-field-editors";
import { boardCapabilities } from "../../shared/board-capabilities";
import {
  isKanbanCardOverdue,
  parseKanbanFilter,
  serializeKanbanFilter,
  type KanbanFilterState,
} from "../kanban/kanban-filter";
import { KanbanSelectedItem } from "../kanban/kanban-view";
import { buildCardLink } from "../kanban/kanban-view-helpers";
import { useKanbanLaneCopy } from "../kanban/use-kanban-lane-copy";
import { MainTableError } from "./main-table-error";
import { fillTableCopy } from "./main-table.copy";
import { MainTableSkeleton } from "./main-table-skeleton";
import { MainTableSurface } from "./main-table-surface";
import { TableSubItems } from "./table-sub-items";
import type { TableEditors } from "./table-cell-editors";
import {
  buildTableGroup,
  tableStatusOptions,
  type TableStatusOption,
} from "./table-model";
import { useStatusOptionFormCopy } from "../../status-option/use-status-option-form-copy";
import { useMainTableCopy } from "./use-main-table-copy";

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
  const header = useContext(BoardHeaderDataContext);

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
  const parsedFilter = useMemo(
    () => parseKanbanFilter(filterValue),
    [filterValue],
  );
  const isOverdue = useCallback(
    (card: Card) => isKanbanCardOverdue(card, now),
    [now],
  );

  const [showDone, setShowDone] = useState(false);
  const [selection, setSelection] = useState<ReadonlySet<string>>(
    () => new Set(),
  );
  const [collapsed, setCollapsed] = useState<ReadonlySet<string>>(
    () => new Set(),
  );

  const titles = useRef(new Map<string, string>());
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
        id: `table-write-${failure.cardId ?? "create"}`,
      });
    },
    [copy],
  );

  const table = useBoardTable(boardId, workspaceId, {
    groupBy: groupByValue,
    filter: parsedFilter,
    isOverdue,
    onWriteError,
  });
  const { access } = useBoardAccess(boardId, workspaceId);
  const capabilities = boardCapabilities(access?.myLevel);
  const addOption = useAddFieldOption(boardId, workspaceId);
  const optionFormCopy = useStatusOptionFormCopy();
  const duplicateCard = useDuplicateCard(boardId, workspaceId);
  const updateCard = useUpdateCard(boardId, workspaceId);
  const archiveCard = useArchiveCard(boardId, workspaceId, {
    onError: (_error, cardId) =>
      toast.error(
        fillTableCopy(copy.failed.archive, {
          title: titles.current.get(cardId) ?? "",
        }),
        { id: `table-write-${cardId}` },
      ),
  });
  const moveItem = useMoveItemToBoard(boardId, workspaceId, {
    onError: (_error, { cardId }) =>
      toast.error(
        fillTableCopy(copy.failed.move, {
          title: titles.current.get(cardId) ?? "",
        }),
        { id: `table-write-${cardId}` },
      ),
  });
  const workspaceBoards = useWorkspaceBoards(workspaceId);
  const isNarrow = useIsMobile();
  const boardCovered = Boolean(selectedItemId) && isNarrow;

  const { board, filterChoices } = table;
  // The filter as it applies: ids the board no longer has are dropped, and
  // the route parameter is rewritten (history replace) to match.
  const filter = useMemo(
    () =>
      board ? dropUnknownFilterIds(parsedFilter, filterChoices) : parsedFilter,
    [board, parsedFilter, filterChoices],
  );
  useEffect(() => {
    if (!board || filterValue === undefined) return;
    const canonical = serializeKanbanFilter(filter);
    if (canonical !== filterValue)
      setFilterValue(canonical || undefined, { replace: true });
  }, [board, filter, filterValue, setFilterValue]);

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
    for (const id of pending.ids) archiveMutate(id);
    setPendingArchive([]);
  }, [archiveMutate]);
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
    !table.groupByField || table.groupByField.id === table.statusField?.id;
  const groupField = table.groupByField ?? table.statusField;
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
  const projection = useMemo(
    () =>
      projectTableLanes({
        lanes,
        sort: table.sort,
        hideDone: !showDone,
        statusRank,
        isOverdue,
        narrowed: table.filterActive,
      }),
    [lanes, table.sort, showDone, statusRank, isOverdue, table.filterActive],
  );
  const groups = useMemo(
    () =>
      projection.groups.map((lane) =>
        buildTableGroup(
          lane,
          lane.name ||
            (isStatusGrouping ? copy.group.noStatus : copy.group.noValue),
          { now, copy, statuses: statusById },
        ),
      ),
    [projection.groups, isStatusGrouping, copy, now, statusById],
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

  const handleFilterChange = (next: KanbanFilterState) =>
    setFilterValue(serializeKanbanFilter(next) || undefined);

  const writeStatus = (cardId: string, statusOptionId: string) => {
    noteTitle(cardId);
    table.updateStatus.mutate({ cardId, statusOptionId });
  };

  const selectedList = [...selectedIds];
  const writeToSelected = (write: (cardId: string) => void) => {
    for (const id of selectedList) write(id);
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
  const moveTargets = (workspaceBoards.data ?? [])
    .filter(
      (candidate) =>
        candidate.id !== boardId &&
        !candidate.isArchived &&
        candidate.canAddItems !== false,
    )
    .map((candidate) => ({ id: candidate.id, title: candidate.title }));

  const handleCreate = (groupId: string, title: string) => {
    if (!capabilities.canCreateCard) return;
    const lane = table.lanes.find(
      (candidate) => (candidate.optionId ?? "none") === groupId,
    );
    if (!lane) return;
    const listId = listForNewItem(lane.cards, table.groups);
    if (!listId) return;
    const fieldValue =
      !isStatusGrouping && table.groupByField && lane.optionId
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
      table.updateStatus.mutate({
        cardId: payload.cardId,
        statusOptionId: payload.value,
      });
      return;
    }
    table.updateFieldValue.mutate(payload);
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

  const order = groups.flatMap((group) => group.rows.map((row) => row.id));
  const selectedIndex = selectedItemId ? order.indexOf(selectedItemId) : -1;
  const previousId = selectedIndex > 0 ? order[selectedIndex - 1] : undefined;
  const nextId = selectedIndex >= 0 ? order[selectedIndex + 1] : undefined;
  const step = (id: string | undefined) =>
    id ? () => onSelectedItemChange?.(id, { replace: true }) : undefined;
  const departmentMark: ItemSheetMark | undefined = header?.space
    ? { initials: header.space.initials, tone: header.space.markTone }
    : undefined;
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
            filter,
            filterOptions: {
              statuses: [...filterChoices.statuses],
              assignees: [...filterChoices.assignees],
              labels: [...filterChoices.labels],
            },
            onFilterChange: handleFilterChange,
            groupByFields: table.groupByFields,
            groupByFieldId: table.groupByField?.id ?? null,
            onGroupByChange: (fieldId) => {
              if (onGroupByChange) onGroupByChange(fieldId);
              else setLocalGroupBy(fieldId);
            },
            sort: table.sort,
            onSortChange: table.setSort,
            hiddenColumnIds: table.hiddenColumnIds,
            onColumnVisibleChange: table.setColumnVisible,
            density: table.density,
            onDensityChange: table.setDensity,
            canAdd: capabilities.canCreateCard,
          }}
          groups={groups}
          statusOptions={statusOptions}
          addGroup={
            capabilities.canEditFields && groupField
              ? {
                  formCopy: optionFormCopy,
                  kind: isStatusGrouping ? "status" : "group",
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
                }
              : undefined
          }
          columnWidths={table.columnWidths}
          onColumnWidthChange={table.setColumnWidth}
          onColumnWidthReset={table.resetColumnWidth}
          selectedIds={selectedIds}
          collapsedGroupIds={collapsed}
          openItemId={selectedItemId}
          canEdit={capabilities.canEditFields}
          editors={editors}
          canCreate={capabilities.canCreateCard}
          canArchive={capabilities.canDelete}
          shown={projection.visibleCount}
          hidden={projection.hiddenCount}
          canShowDone={!showDone && projection.doneHiddenCount > 0}
          noResults={groups.length === 0 && projection.totalCount > 0}
          noResultsAssignee={
            filter.assigneeIds.length === 1
              ? filterChoices.assignees.find(
                  (choice) => choice.id === filter.assigneeIds[0],
                )?.name
              : undefined
          }
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
            setSelection(selectAll ? new Set(visibleIds) : new Set())
          }
          onClearSelection={() => setSelection(new Set())}
          onGroupToggle={(groupId) =>
            setCollapsed((current) => {
              const next = new Set(current);
              if (next.has(groupId)) next.delete(groupId);
              else next.add(groupId);
              return next;
            })
          }
          onOpenItem={(id) => onSelectedItemChange?.(id)}
          onStatusChange={writeStatus}
          onCreateItem={handleCreate}
          onClearFilter={() => {
            handleFilterChange({
              statusOptionIds: [],
              priorities: [],
              assigneeIds: [],
              labelIds: [],
              overdue: false,
            });
            table.search.setQuery("");
            setShowDone(true);
          }}
          onShowDone={() => setShowDone(true)}
          onBulkStatus={(optionId) =>
            writeToSelected((id) => writeStatus(id, optionId))
          }
          onBulkDue={(day) => writeToSelected((id) => writeDue(id, day))}
          rowActions={{
            onDuplicate: capabilities.canCreateCard
              ? (id) => duplicateCard.mutate(id)
              : undefined,
            onCopyLink: handleCopyLink,
            onArchive: capabilities.canDelete
              ? (id) => {
                  noteTitle(id);
                  startArchive([id]);
                  setSelection(new Set());
                }
              : undefined,
          }}
          bulkAssign={
            editors
              ? {
                  members,
                  onAssign: (userId) =>
                    writeToSelected((id) => writeAssignee(id, userId)),
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
          onBulkArchive={() => {
            for (const id of selectedList) noteTitle(id);
            startArchive(selectedList);
            setSelection(new Set());
          }}
        />
      </div>

      {selectedItemId ? (
        <KanbanSelectedItem
          board={board}
          cardId={selectedItemId}
          capabilities={capabilities}
          departmentMark={departmentMark}
          groups={sheetGroups}
          onClose={() => onSelectedItemChange?.(undefined, { replace: true })}
          onRestoreFocus={restoreFocus}
          onPrevious={step(previousId)}
          onNext={step(nextId)}
          onCopyLink={handleCopyLink}
          onMoveToGroup={handleMoveToGroup}
          onUpdateFieldValue={handleSheetFieldValue}
          onDuplicate={(cardId) => duplicateCard.mutate(cardId)}
          onArchive={(cardId) => {
            noteTitle(cardId);
            archiveCard.mutate(cardId);
          }}
        />
      ) : null}
    </div>
  );
}
