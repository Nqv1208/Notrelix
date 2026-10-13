import {
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
} from "react";
import { toast } from "sonner";
import {
  dropUnknownFilterIds,
  useBoardAccess,
  useBoardKanban,
  useCard,
  useCardActivity,
  useCardComments,
  useCardFiles,
  useCreateCardUpdate,
  useDeleteCard,
  useDeleteCardUpdate,
  useDuplicateCard,
  useUpdateCard,
  useUpdateCardUpdate,
} from "@notrelix/work-management-state";
import {
  generatePosition,
  type Board,
  type BoardGroup,
  type Card,
  type CardDetail,
  type KanbanStatusLane,
  type UpdateFieldValueInput,
} from "@notrelix/work-management-core";
import { BoardHeaderDataContext } from "../../board-layout/board-header-data-context";
import type {
  ItemSheetGroup,
  ItemSheetMark,
  TaskDetailCapabilities,
} from "../../card-detail/task-detail-types";
import { KanbanBoard } from "./kanban-board";
import { KanbanCardDetailPanel } from "./kanban-card-detail-panel";
import { fillCopy } from "./kanban-card.copy";
import type { KanbanLaneDrop } from "./kanban-dnd";
import {
  isKanbanCardOverdue,
  parseKanbanFilter,
  serializeKanbanFilter,
  type KanbanFilterState,
} from "./kanban-filter";
import type { KanbanLaneCopy } from "./kanban-lane.copy";
import { KanbanSkeleton } from "./kanban-skeleton";
import { KanbanToolbar } from "./kanban-toolbar";
import { KanbanUnavailableState } from "./kanban-unavailable-state";
import { boardCapabilities, buildCardLink } from "./kanban-view-helpers";
import { useKanbanCardCopy } from "./use-kanban-card-copy";
import { useKanbanLaneCopy } from "./use-kanban-lane-copy";
import { useKanbanToolbarCopy } from "./use-kanban-toolbar-copy";

type ParamChange = (
  value: string | undefined,
  options?: { replace?: boolean },
) => void;

export interface KanbanViewProps {
  boardId: string;
  workspaceId: string;
  /** Card id from the `item` search parameter; unknown ids show nothing. */
  selectedItemId?: string;
  /**
   * Opening passes the id (push). Closing, K/J and clearing a stale id pass
   * `replace` so Back does not step through every item.
   */
  onSelectedItemChange?: (
    id: string | undefined,
    options?: { replace?: boolean },
  ) => void;
  /** The route `filter` parameter (`serializeKanbanFilter` form). */
  filter?: string;
  /**
   * Writes the route `filter` parameter; `undefined` removes it. Without it
   * the view keeps the filter itself (stories, isolated tests).
   */
  onFilterChange?: ParamChange;
  /** The route `groupBy` parameter: a field id (Status by default). */
  groupBy?: string;
  onGroupByChange?: (fieldId: string) => void;
}

function laneName(
  lane: KanbanStatusLane,
  copy: KanbanLaneCopy,
  isStatus: boolean,
) {
  return lane.name || (isStatus ? copy.noStatus : copy.noValue);
}

/** The board group a new item of a lane goes into: the lane's last card's. */
function groupForNewItem(
  lane: KanbanStatusLane,
  groups: readonly BoardGroup[],
): string | undefined {
  const last = lane.cards.at(-1)?.listId;
  if (last) return last;
  return [...groups].sort((a, b) => a.position - b.position)[0]?.id;
}

/**
 * The Kanban of a board: status lanes (or lanes of the Select field named by
 * `groupBy`), the toolbar with the route filter, drag between lanes, add in
 * a lane and the non-modal item sheet. What the viewer may do comes from the
 * board access read: a commenter and a viewer get no drag and no add, and a
 * read-only sheet (a commenter keeps the composer).
 */
export function KanbanView({
  boardId,
  workspaceId,
  selectedItemId,
  onSelectedItemChange,
  filter: filterParam,
  onFilterChange,
  groupBy: groupByParam,
  onGroupByChange,
}: KanbanViewProps) {
  // Read once so every card judges "overdue" against the same instant.
  const [now] = useState(() => new Date());
  const cardCopy = useKanbanCardCopy();
  const toolbarCopy = useKanbanToolbarCopy();
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

  const kanban = useBoardKanban(boardId, workspaceId, {
    groupBy: groupByValue,
    filter: parsedFilter,
    isOverdue,
  });
  const { access } = useBoardAccess(boardId, workspaceId);
  const capabilities = boardCapabilities(access?.myLevel);
  const duplicateCard = useDuplicateCard(boardId, workspaceId);
  const deleteCard = useDeleteCard(boardId, workspaceId);

  const { board, filterChoices } = kanban;
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
    if (canonical !== filterValue) {
      setFilterValue(canonical || undefined, { replace: true });
    }
  }, [board, filter, filterValue, setFilterValue]);

  const boardRef = useRef<HTMLDivElement>(null);
  const [addRequest, setAddRequest] = useState(0);

  const handleOpenDetail = useCallback(
    (cardId: string) => onSelectedItemChange?.(cardId),
    [onSelectedItemChange],
  );
  const handleClosePanel = useCallback(() => {
    onSelectedItemChange?.(undefined, { replace: true });
  }, [onSelectedItemChange]);

  // Radix returns focus to the element focused before the sheet opened, which
  // is lost on a deep link or a re-render. Put it back on the opening card, or
  // on the board region when that card is gone.
  const restoreFocus = useCallback((cardId: string) => {
    const root = boardRef.current;
    if (!root) return;
    const opener = Array.from(
      root.querySelectorAll<HTMLElement>("[data-card-id]"),
    ).find((element) => element.dataset.cardId === cardId);
    (opener ?? root).focus();
  }, []);

  const isStatusGrouping =
    !kanban.groupByField || kanban.groupByField.id === kanban.statusField?.id;
  const allCards = useMemo(
    () => kanban.allLanes.flatMap((lane) => lane.cards),
    [kanban.allLanes],
  );
  // K/J follow the lanes as shown: lane order, then card order.
  const laneOrder = useMemo(
    () => kanban.lanes.flatMap((lane) => lane.cards.map((card) => card.id)),
    [kanban.lanes],
  );

  if (kanban.isLoading) return <KanbanSkeleton />;
  if (kanban.error || !board) return <KanbanUnavailableState />;

  const lanesByCard = new Map<string, KanbanStatusLane>();
  for (const lane of kanban.allLanes) {
    for (const card of lane.cards) lanesByCard.set(card.id, lane);
  }

  const moveFailed = (cardId: string) => {
    const card = allCards.find((candidate) => candidate.id === cardId);
    const lane = lanesByCard.get(cardId);
    toast.error(
      fillCopy(laneCopy.moveFailed, {
        title: card?.title ?? "",
        lane: lane ? laneName(lane, laneCopy, isStatusGrouping) : "",
      }),
    );
  };

  /** Writes a lane's value for a card: Status, or the grouping Select field. */
  const writeLaneValue = (cardId: string, optionId: string) => {
    if (isStatusGrouping) {
      kanban.updateStatus.mutate(
        { cardId, statusOptionId: optionId },
        { onError: () => moveFailed(cardId) },
      );
      return;
    }
    if (!kanban.groupByField) return;
    kanban.updateFieldValue.mutate(
      { cardId, fieldDefinitionId: kanban.groupByField.id, value: optionId },
      { onError: () => moveFailed(cardId) },
    );
  };

  const handleDrop = (drop: KanbanLaneDrop) => {
    if (!capabilities.canMoveCard) return;
    if (drop.kind === "status") {
      writeLaneValue(drop.cardId, drop.optionId);
      return;
    }
    kanban.moveCard.mutate(
      { cardId: drop.cardId, listId: drop.listId, position: drop.position },
      { onError: () => moveFailed(drop.cardId) },
    );
  };

  const handleCreateItem = (lane: KanbanStatusLane, title: string) => {
    const listId = groupForNewItem(lane, kanban.groups);
    if (!listId || !capabilities.canCreateCard) return;
    const fieldValue =
      !isStatusGrouping && kanban.groupByField && lane.optionId
        ? { fieldDefinitionId: kanban.groupByField.id, value: lane.optionId }
        : null;
    kanban.createItemInLane.mutate(
      {
        listId,
        title,
        statusOptionId: isStatusGrouping ? lane.optionId : null,
        fieldValue,
      },
      {
        onError: () => toast.error(fillCopy(laneCopy.createFailed, { title })),
      },
    );
  };

  const handleSheetFieldValue = (payload: UpdateFieldValueInput) => {
    // The Status pill writes through the status endpoint, like a drag, so the
    // card moves to its lane at once (the keyboard alternative to dragging).
    if (
      payload.fieldDefinitionId === kanban.statusField?.id &&
      typeof payload.value === "string"
    ) {
      kanban.updateStatus.mutate(
        { cardId: payload.cardId, statusOptionId: payload.value },
        { onError: () => moveFailed(payload.cardId) },
      );
      return;
    }
    kanban.updateFieldValue.mutate(payload);
  };

  const handleMoveToGroup = (cardId: string, groupId: string) => {
    const target = kanban.groups.find((group) => group.id === groupId);
    if (!target) return;
    const lastPosition = [...target.cards]
      .sort((a, b) => a.position - b.position)
      .at(-1)?.position;
    kanban.moveCard.mutate(
      {
        cardId,
        listId: groupId,
        position: generatePosition(lastPosition, undefined),
      },
      { onError: () => moveFailed(cardId) },
    );
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

  const handleFilterChange = (next: KanbanFilterState) => {
    setFilterValue(serializeKanbanFilter(next) || undefined);
  };

  const selectedIndex = selectedItemId ? laneOrder.indexOf(selectedItemId) : -1;
  const previousId =
    selectedIndex > 0 ? laneOrder[selectedIndex - 1] : undefined;
  const nextId = selectedIndex >= 0 ? laneOrder[selectedIndex + 1] : undefined;
  const step = (id: string | undefined) =>
    id ? () => onSelectedItemChange?.(id, { replace: true }) : undefined;

  const departmentMark: ItemSheetMark | undefined = header?.space
    ? { initials: header.space.initials, tone: header.space.markTone }
    : undefined;
  const sheetGroups: ItemSheetGroup[] = [...kanban.groups]
    .sort((a, b) => a.position - b.position)
    .map((group) => ({ id: group.id, title: group.title }));

  return (
    <div
      ref={boardRef}
      data-kanban-board=""
      tabIndex={-1}
      className="relative flex h-full min-h-0 flex-col overflow-hidden bg-background font-body select-none focus:outline-hidden"
    >
      <div className="px-4 pt-3 sm:px-6">
        <KanbanToolbar
          copy={toolbarCopy}
          searchQuery={kanban.search.query}
          onSearchChange={kanban.search.setQuery}
          filter={filter}
          filterOptions={{
            statuses: [...filterChoices.statuses],
            assignees: [...filterChoices.assignees],
            labels: [...filterChoices.labels],
          }}
          onFilterChange={handleFilterChange}
          groupByFields={kanban.groupByFields}
          groupByFieldId={kanban.groupByField?.id ?? null}
          onGroupByChange={(fieldId) => {
            if (onGroupByChange) onGroupByChange(fieldId);
            else setLocalGroupBy(fieldId);
          }}
          canAdd={capabilities.canCreateCard}
          onAddItem={() => setAddRequest((count) => count + 1)}
          noResults={
            kanban.filterActive && kanban.visibleCount === 0
              ? { hiddenCount: kanban.totalCount }
              : null
          }
        />
      </div>

      <div className="min-h-0 flex-1 overflow-auto px-4 pt-3 pb-6 sm:px-6">
        <KanbanBoard
          lanes={kanban.lanes}
          copy={laneCopy}
          cardCopy={cardCopy}
          now={now}
          selectedItemId={selectedItemId}
          filterActive={kanban.filterActive}
          canAdd={
            capabilities.canCreateCard &&
            !(kanban.filterActive && kanban.visibleCount === 0)
          }
          canDrag={capabilities.canMoveCard}
          groupKind={isStatusGrouping ? "status" : "select"}
          addRequest={addRequest}
          onOpenDetails={handleOpenDetail}
          onDropCard={handleDrop}
          onCreateItem={handleCreateItem}
        />
      </div>

      {selectedItemId ? (
        <KanbanSelectedItem
          board={board}
          cardId={selectedItemId}
          capabilities={capabilities}
          departmentMark={departmentMark}
          groups={sheetGroups}
          onClose={handleClosePanel}
          onRestoreFocus={restoreFocus}
          onPrevious={step(previousId)}
          onNext={step(nextId)}
          onCopyLink={handleCopyLink}
          onMoveToGroup={handleMoveToGroup}
          onUpdateFieldValue={handleSheetFieldValue}
          onDuplicate={(cardId) => duplicateCard.mutate(cardId)}
          onArchive={(cardId) => deleteCard.mutate(cardId)}
        />
      ) : null}
    </div>
  );
}

interface SelectedItemProps {
  board: Board;
  cardId: string;
  capabilities: TaskDetailCapabilities;
  departmentMark?: ItemSheetMark;
  groups: readonly ItemSheetGroup[];
  onClose: () => void;
  onRestoreFocus: (cardId: string) => void;
  onPrevious?: () => void;
  onNext?: () => void;
  onCopyLink: (cardId: string) => void;
  onMoveToGroup: (cardId: string, groupId: string) => void;
  onUpdateFieldValue: (payload: UpdateFieldValueInput) => void;
  onDuplicate: (cardId: string) => void;
  onArchive: (cardId: string) => void;
}

/**
 * Resolves the selected card and hands it to the sheet. An id that fails to
 * load, or resolves to a card outside this board and workspace, is stale:
 * nothing renders and the `item` parameter is cleared with history replace.
 * A cached card is never trusted once the query has errored (a deleted card
 * stays in the query cache). While the next card of K/J loads, the sheet
 * keeps showing the previous one, so it stays mounted and keeps focus.
 */
function KanbanSelectedItem({
  board,
  cardId,
  onClose,
  onRestoreFocus,
  ...rest
}: SelectedItemProps) {
  const { card, error } = useCard(cardId, board.workspaceId);
  const notFound =
    Boolean(error) ||
    (card !== undefined &&
      (card.boardId !== board.id || card.workspaceId !== board.workspaceId));
  const lastShown = useRef<CardDetail | null>(null);
  if (card && !notFound) lastShown.current = card;
  const shown = notFound ? null : (card ?? lastShown.current);

  const onCloseRef = useRef(onClose);
  useEffect(() => {
    onCloseRef.current = onClose;
  });
  useEffect(() => {
    if (notFound) onCloseRef.current();
  }, [notFound, cardId]);

  if (!shown) return null;
  return (
    <KanbanSelectedItemReady
      board={board}
      card={shown}
      onClose={onClose}
      onCloseAutoFocus={(event) => {
        event.preventDefault();
        onRestoreFocus(lastShown.current?.id ?? cardId);
      }}
      {...rest}
    />
  );
}

function KanbanSelectedItemReady({
  board,
  card,
  capabilities,
  departmentMark,
  groups,
  onClose,
  onCloseAutoFocus,
  onPrevious,
  onNext,
  onCopyLink,
  onMoveToGroup,
  onUpdateFieldValue,
  onDuplicate,
  onArchive,
}: Omit<SelectedItemProps, "cardId" | "onRestoreFocus"> & {
  card: CardDetail;
  onCloseAutoFocus: (event: Event) => void;
}) {
  const files = useCardFiles(card.id, card.workspaceId);
  const activity = useCardActivity(card.id, card.workspaceId);
  const updates = useCardComments(card.id, card.workspaceId);
  const updateCard = useUpdateCard(card.boardId, card.workspaceId);
  const createUpdate = useCreateCardUpdate(card.id, card.workspaceId);
  const updateUpdate = useUpdateCardUpdate(card.id, card.workspaceId);
  const deleteUpdate = useDeleteCardUpdate(card.id, card.workspaceId);

  return (
    <KanbanCardDetailPanel
      board={board}
      card={card}
      capabilities={capabilities}
      departmentMark={departmentMark}
      groups={groups}
      onCloseAutoFocus={onCloseAutoFocus}
      detailData={{
        files: files.data ?? card.files,
        filesLoading: files.isLoading,
        activity: activity.data ?? card.activity,
        activityLoading: activity.isLoading,
        activityFetching: activity.isFetching,
        updates: updates.data ?? card.updates,
        updatesLoading: updates.isLoading,
      }}
      callbacks={{
        onClose,
        onRenameTitle: (cardId, patch) => updateCard.mutate({ cardId, patch }),
        onToggleWatch: () => undefined,
        onDuplicate,
        onArchive: (cardId) => {
          onArchive(cardId);
          onClose();
        },
        onUpdateFieldValue,
        onRefreshActivity: () => {
          void activity.refetch();
        },
        onCreateUpdate: (input, options) =>
          createUpdate.mutate(input, {
            onSuccess: options?.onSuccess,
            onSettled: options?.onSettled,
          }),
        onUpdateUpdate: (updateId, body) =>
          updateUpdate.mutate({ updateId, body }),
        onDeleteUpdate: (updateId) => deleteUpdate.mutate(updateId),
        onSelectTab: () => undefined,
        onPrevious,
        onNext,
        onCopyLink,
        onMoveToGroup,
      }}
    />
  );
}
