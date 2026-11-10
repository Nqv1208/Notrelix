import { unsentCommentOf } from "../../card-detail/update-composer";
import {
  FaceFieldsPopover,
  KanbanFaceProvider,
} from "../../card-popup/capabilities";
import { threadRefusalMessage } from "../../item-page/collaboration-refusal";
import { useItemPageCopy } from "../../item-page/use-item-page-copy";
import { CardCreatePopup } from "../../card-popup/create/card-create-popup";
import { useNoGroupNotice } from "../../card-popup/create/use-no-group-notice";
import { useAddGroup } from "../../status-option/use-add-group";
import {
  type ReactNode,
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
} from "react";
import { ChevronLeft, ChevronRight, Info, Lock } from "lucide-react";
import { toast } from "sonner";
import {
  TABLET_SLOT,
  TabletSlot,
  cn,
  useIsMobile,
  useIsTablet,
} from "@notrelix/ui-web";
import {
  useBoardLabels,
  useCardLabelWrites,
  useToggleCardLabel,
  useBoardKanban,
  useBoardView,
  useCard,
  useCardActivity,
  useCardComments,
  useCardFiles,
  useChecklistWrites,
  useItemApprovals,
  useItemLinks,
  usePeopleDirectory,
  useAddFieldOption,
  useCreateCardUpdate,
  useDeleteCardUpdate,
  useSetCardUpdateResolved,
  useToggleCommentReaction,
  useMoveFieldOption,
  useRemoveFieldOption,
  useUpdateCard,
  useUpdateCardUpdate,
  useUpdateFieldOption,
  useHoldRealtimeRefetch,
  type KanbanWriteFailure,
  classifyRestoreError,
  deletedItemOf,
  useRestoreDeletedItem,
} from "@notrelix/work-management-state";
import {
  generatePosition,
  groupCardsByField,
  groupableFields,
  resolveKanbanSettings,
  withReservedFieldValues,
  serializeCompoundFilter,
  type FilterField,
  type Board,
  type BoardGroup,
  type Card,
  type CardDetail,
  type FieldDefinition,
  type KanbanStatusLane,
  type UpdateFieldValueInput,
} from "@notrelix/work-management-core";
import type {
  ItemSheetGroup,
  ItemSheetLinkCandidate,
  TaskDetailCapabilities,
} from "../../card-detail/task-detail-types";
import type { StatusOptionDraft } from "../../status-option/status-option-form";
import { useStatusOptionFormCopy } from "../../status-option/use-status-option-form-copy";
import { findItemFields } from "../../card-detail/item-field-editors";
import { useItemSheetExtras } from "../../card-detail/use-item-sheet-extras";
import { useBoardViewerId } from "../../shared/board-viewer";
import { KanbanBoard } from "./kanban-board";
import { KanbanBulkBar } from "./kanban-bulk-bar";
import { useKanbanSelection } from "./kanban-selection";
import { prefersReducedMotion } from "./kanban-chrome";
import type { KanbanCardActions } from "./kanban-card-quick-actions";
import { CardPopupSurface } from "../../card-popup/card-popup-surface";
import {
  PopupDeletedNotice,
  PopupMovedNotice,
} from "../../card-popup/states/popup-states";
import { approvalFailure } from "../../card-popup/approval/approval-model";
import { KanbanCardDetailPanel } from "./kanban-card-detail-panel";
import { fillCopy } from "./kanban-card.copy";
import {
  arrangeKanbanLanes,
  getKanbanDrop,
  kanbanLaneKey,
  moveCardInArrangement,
  type KanbanLaneDrop,
} from "./kanban-dnd";
import type { CardPopupLanePicker } from "../../card-popup/card-popup-context";
import { emptyKanbanFilter, isKanbanCardOverdue } from "./kanban-filter";
import type { KanbanLaneCopy } from "./kanban-lane.copy";
import { KanbanSkeleton } from "./kanban-skeleton";
import { KanbanToolbar } from "./kanban-toolbar";
import { SavedFiltersControl } from "../../saved-filters/saved-filters-control";
import { fillFiltersCopy } from "../../filters/filters.copy";
import { useBoardViewState } from "../../filters/use-board-view-state";
import { FilterNoResults } from "../../filters/filter-no-results";
import { ViewStateBar } from "../../filters/view-state-bar";
import {
  KanbanSwimlaneBoard,
  type SwimlaneDrop,
  type SwimlaneRefusal,
} from "./kanban-swimlane-board";
import { KanbanUnavailableState } from "./kanban-unavailable-state";
import { buildCardLink } from "./kanban-view-helpers";
import {
  canManageStatuses,
  useBoardCapabilities,
  useItemCapabilities,
} from "../../shared/board-capabilities";
import { nextLabelColor } from "../../shared/item-display";
import {
  kanbanCollapsedLanesKey,
  useKanbanCollapsedLanes,
} from "./use-kanban-collapsed-lanes";
import { useItemRemoval } from "../../item-lifecycle/use-item-removal";
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
  /** The signed-in member (from the host's session): offers "Giao cho tôi". */
  currentUserId?: string | null;
  /**
   * Opening passes the id (push). Closing, K/J and clearing a stale id pass
   * `replace` so Back does not step through every item.
   */
  onSelectedItemChange?: (
    id: string | undefined,
    options?: { replace?: boolean },
  ) => void;
  /** Opens an item of any board (the toast after a move, the item that blocks a move); absent: only this board's items open, through `onSelectedItemChange`. */
  onOpenItem?: (boardId: string, itemId: string) => void;
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

/** What opened the full form: the toolbar, a lane's "+", or a lane's quick add. */
type CreateRequest =
  | { readonly kind: "toolbar" }
  | {
      readonly kind: "lane" | "quick";
      readonly lane: KanbanStatusLane;
      readonly title: string;
    };

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
  currentUserId,
  onSelectedItemChange,
  onOpenItem,
  filter: filterParam,
  onFilterChange,
  groupBy: groupByParam,
  onGroupByChange,
}: KanbanViewProps) {
  // Read once so every card judges "overdue" against the same instant.
  const [now] = useState(() => new Date());
  // A lifted card holds realtime refetches so the lanes do not reshuffle under the pointer.
  const [dragging, setDragging] = useState(false);
  useHoldRealtimeRefetch(dragging);
  const cardCopy = useKanbanCardCopy();
  const toolbarCopy = useKanbanToolbarCopy();
  const laneCopy = useKanbanLaneCopy();
  const statusFormCopy = useStatusOptionFormCopy();

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
    timeZone,
    timeZoneIsDefault,
  } = useBoardViewState({
    boardId,
    workspaceId,
    routeFilter: filterValue,
    onRouteFilterChange: setFilterValue,
    routeGroupBy: groupByValue,
    onRouteGroupByChange: setGroupByValue,
  });
  // The Kanban's columns are a Status or Select field: no person or date split.
  const columnView = useMemo(
    () => ({ ...kanbanView, flatGroups: false }),
    [kanbanView],
  );
  const isOverdue = useCallback(
    (card: Card) => isKanbanCardOverdue(card, now),
    [now],
  );

  // Where each card was when its write started: a failure toast names the
  // lane the card goes back to. Read by hook-level error handlers, which run
  // for every failed write (also after a later write or an unmount).
  const writeOrigins = useRef(
    new Map<string, { title: string; lane: string; target?: string }>(),
  );
  // A refused move shows on the card with a retry; the retry runs the same write again.
  const retryWrites = useRef(new Map<string, () => void>());
  const [failedMoves, setFailedMoves] = useState<ReadonlyMap<string, string>>(
    new Map(),
  );
  const clearFailedMove = (cardId: string) =>
    setFailedMoves((current) => {
      if (!current.has(cardId)) return current;
      const next = new Map(current);
      next.delete(cardId);
      return next;
    });
  const laneCopyRef = useRef(laneCopy);
  laneCopyRef.current = laneCopy;
  const cardCopyRef = useRef(cardCopy);
  cardCopyRef.current = cardCopy;
  // Set once the board is read: who last changed a card and the lane it is in.
  const conflictRef = useRef<
    ((card: Card) => { actor: string; lane: string }) | undefined
  >(undefined);
  const onWriteError = useCallback((failure: KanbanWriteFailure) => {
    const copy = laneCopyRef.current;
    // A failed quick add shows inline in the lane's add field, with a retry.
    if (failure.kind === "create") return;
    const cardId = failure.cardId ?? "";
    const origin = writeOrigins.current.get(cardId);
    writeOrigins.current.delete(cardId);
    // Lost to someone else's change: name who, and where the card now is.
    const seen = failure.conflict
      ? conflictRef.current?.(failure.conflict.card)
      : undefined;
    if (seen) {
      const values = {
        actor: seen.actor || copy.someone,
        title: origin?.title ?? failure.conflict?.card.title ?? "",
        lane: seen.lane,
      };
      toast.error(
        fillCopy(
          failure.kind !== "field" && seen.lane
            ? copy.conflictMoved
            : copy.conflictChanged,
          values,
        ),
        { id: `kanban-write-${cardId}` },
      );
      return;
    }
    const message =
      failure.kind === "field"
        ? fillCopy(copy.saveFailed, { title: origin?.title ?? "" })
        : fillCopy(copy.moveFailed, {
            title: origin?.title ?? "",
            lane: origin?.lane ?? "",
          });
    // One toast per card: a second failure of the same card replaces it.
    toast.error(message, { id: `kanban-write-${cardId}` });
    if (
      failure.kind !== "field" &&
      origin?.target &&
      retryWrites.current.has(cardId)
    )
      setFailedMoves((current) =>
        new Map(current).set(
          cardId,
          fillCopy(cardCopyRef.current.moveFailed, {
            target: origin.target ?? "",
            lane: origin.lane,
          }),
        ),
      );
  }, []);

  const kanban = useBoardKanban(boardId, workspaceId, {
    groupBy: view.applied.group?.fieldId,
    filter: noLegacyFilter,
    view: columnView,
    isOverdue,
    onWriteError,
  });
  const { access, capabilities } = useBoardCapabilities(boardId, workspaceId);
  const noGroupNotice = useNoGroupNotice(boardId, workspaceId);
  // The view's Kanban settings (lane limit, who sees the status controls,
  // keyboard move). A view saved without them gives today's behaviour. The
  // role setting only shapes the controls: the server still decides every
  // write, so a viewer the setting hides them from is not made safer by it.
  const {
    viewConfig,
    viewId: savedViewId,
    updateViewConfig,
  } = useBoardView(boardId, workspaceId);
  const kanbanSettings = useMemo(
    () => resolveKanbanSettings(viewConfig),
    [viewConfig],
  );
  const showStatusControls =
    capabilities.canEditFields &&
    canManageStatuses(access?.myLevel, kanbanSettings.statusManageRoles);
  const openItem = (targetBoardId: string, itemId: string) => {
    if (onOpenItem) onOpenItem(targetBoardId, itemId);
    else if (targetBoardId === boardId) onSelectedItemChange?.(itemId);
  };
  const removal = useItemRemoval({
    workspaceId,
    boardId,
    titleOf: (itemId) =>
      kanban.groups
        .flatMap((group) => group.cards)
        .find((candidate) => candidate.id === itemId)?.title ?? "",
    onGone: (itemId) => {
      if (selectedItemId === itemId) onSelectedItemChange?.(undefined);
    },
    onOpenItem: openItem,
  });
  const updateCardDetails = useUpdateCard(boardId, workspaceId);
  const boardLabels = useBoardLabels(boardId, workspaceId);
  const toggleCardLabel = useToggleCardLabel(boardId, workspaceId);
  // Adding a lane and renaming or recolouring one write an option of the field
  // the board is grouped by; a failure puts the lane back and says so.
  const addOption = useAddFieldOption(boardId, workspaceId, {
    onError: (_error) =>
      toast.error(
        fillCopy(laneCopyRef.current.createOptionFailed, {
          name: pendingOptionName.current,
        }),
        { id: "kanban-option-write" },
      ),
  });
  const updateOption = useUpdateFieldOption(boardId, workspaceId, {
    onError: () =>
      toast.error(
        fillCopy(laneCopyRef.current.updateOptionFailed, {
          name: pendingOptionName.current,
        }),
        { id: "kanban-option-write" },
      ),
  });
  const moveOption = useMoveFieldOption(boardId, workspaceId, {
    onError: () =>
      toast.error(
        fillCopy(laneCopyRef.current.moveOptionFailed, {
          name: pendingOptionName.current,
        }),
        { id: "kanban-option-write" },
      ),
  });
  const removeOption = useRemoveFieldOption(boardId, workspaceId, {
    onError: () =>
      toast.error(
        fillCopy(laneCopyRef.current.deleteOptionFailed, {
          name: pendingOptionName.current,
        }),
        { id: "kanban-option-write" },
      ),
  });
  const { addGroup, isPending: addingGroup } = useAddGroup(
    boardId,
    workspaceId,
  );
  // Below the tablet width the sheet covers the board: the board behind is
  // taken out of the tab order and the accessibility tree while it is open.
  const isNarrow = useIsMobile();
  const tablet = useIsTablet();
  const boardCovered = Boolean(selectedItemId) && isNarrow;

  const { board } = kanban;

  const boardRef = useRef<HTMLDivElement>(null);
  const scrollerRef = useRef<HTMLDivElement>(null);
  // Which sides of the lanes are cut off: a fade and an arrow offer the scroll.
  const [edges, setEdges] = useState({ left: false, right: false });
  const measureEdges = useCallback(() => {
    const scroller = scrollerRef.current;
    if (!scroller) return;
    const left = scroller.scrollLeft > 4;
    const right =
      scroller.scrollLeft + scroller.clientWidth < scroller.scrollWidth - 4;
    setEdges((current) =>
      current.left === left && current.right === right
        ? current
        : { left, right },
    );
  }, []);
  const pendingOptionName = useRef("");
  // The toolbar's "Add item" opens the create dialog; the per-lane composers open from their own rows.
  const [addRequest, setAddRequest] = useState(0);
  // The full form: from the toolbar (no lane), a lane's "+" or a lane's quick add.
  const [create, setCreate] = useState<CreateRequest | null>(null);
  const [filterOpen, setFilterOpen] = useState(false);
  const createOpen = create !== null;
  const canCreate = capabilities.canCreateCard;
  const collapsedLanes = useKanbanCollapsedLanes(
    kanbanCollapsedLanesKey(workspaceId, savedViewId ?? boardId),
  );
  const collapsedSwimlanes = useKanbanCollapsedLanes(
    `${kanbanCollapsedLanesKey(workspaceId, savedViewId ?? boardId)}.swimlanes`,
  );

  // C opens the full form, N the quick add of the first open lane; neither
  // fires while typing, with a modifier, or while a dialog is already open.
  useEffect(() => {
    if (!canCreate) return;
    const onKey = (event: KeyboardEvent) => {
      if (event.metaKey || event.ctrlKey || event.altKey) return;
      const key = event.key.toLowerCase();
      if (key !== "c" && key !== "n") return;
      const target = event.target as HTMLElement | null;
      if (
        target?.closest(
          "input, textarea, select, [contenteditable=''], [contenteditable='true'], [role='dialog'], [role='menu'], [role='listbox']",
        )
      ) {
        return;
      }
      if (document.querySelector("[role='dialog'][data-state='open']")) return;
      event.preventDefault();
      if (key === "c") setCreate({ kind: "toolbar" });
      else setAddRequest((current) => current + 1);
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [canCreate]);

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
  conflictRef.current = (card) => {
    const optionId = isStatusGrouping
      ? card.statusOptionId
      : kanban.groupByField
        ? card.fieldValues[kanban.groupByField.id]
        : null;
    const lane = kanban.allLanes.find((entry) => entry.optionId === optionId);
    return {
      actor: card.updatedByName ?? "",
      lane: lane ? laneName(lane, laneCopyRef.current, isStatusGrouping) : "",
    };
  };
  const allCards = useMemo(
    () => kanban.allLanes.flatMap((lane) => lane.cards),
    [kanban.allLanes],
  );
  // Hooks sit above the loading and error returns below, so the order never changes between renders.
  const faceFieldDefinitions = useMemo(
    () =>
      kanban.filterFields
        .filter((entry) => !entry.builtin)
        .map((entry) => entry.field),
    [kanban.filterFields],
  );
  // Cards archived from the bulk bar leave the board while their undo window
  // runs (`KanbanBulkBar`), as the Table's rows do.
  const [archivingIds, setArchivingIds] = useState<readonly string[]>([]);
  const shownLanes = useMemo<KanbanStatusLane[]>(() => {
    if (archivingIds.length === 0) return kanban.lanes;
    const hidden = new Set(archivingIds);
    return kanban.lanes.map((lane) => {
      const cards = lane.cards.filter((card) => !hidden.has(card.id));
      const removed = lane.cards.length - cards.length;
      return removed === 0
        ? lane
        : { ...lane, cards, total: lane.total - removed };
    });
  }, [kanban.lanes, archivingIds]);
  const orderedCardIds = useMemo(
    () => shownLanes.flatMap((lane) => lane.cards.map((card) => card.id)),
    [shownLanes],
  );
  const picking = useKanbanSelection(orderedCardIds);
  const lanesReady = !kanban.isLoading && Boolean(board);
  // A board that was drawn as skeletons opens its lanes one after the other when the data arrives;
  // later changes (a filter, a new lane, a refresh) do not replay it.
  const sawSkeleton = useRef(false);
  if (kanban.isLoading) sawSkeleton.current = true;
  const reveal = lanesReady && sawSkeleton.current;
  useEffect(() => {
    if (!reveal) return;
    const timer = setTimeout(() => {
      sawSkeleton.current = false;
    }, 2500);
    return () => clearTimeout(timer);
  }, [reveal]);
  useEffect(() => {
    const scroller = scrollerRef.current;
    if (!lanesReady || !scroller) return;
    measureEdges();
    const observer =
      typeof ResizeObserver === "undefined"
        ? null
        : new ResizeObserver(measureEdges);
    observer?.observe(scroller);
    if (scroller.firstElementChild)
      observer?.observe(scroller.firstElementChild);
    return () => observer?.disconnect();
  }, [lanesReady, measureEdges, kanban.lanes.length]);

  const columnFields = groupableFields(kanban.filterFields).filter((field) =>
    kanban.groupByFields.some((candidate) => candidate.id === field.id),
  );
  const toolbar = (
    <KanbanToolbar
      copy={toolbarCopy}
      endActions={
        <FaceFieldsPopover
          fields={faceFieldDefinitions}
          value={{
            cardFieldIds: kanbanSettings.cardFieldIds ?? [],
            cardHideEmpty: kanbanSettings.cardHideEmpty ?? [],
          }}
          canEdit={capabilities.canEditFields}
          onChange={(next) => updateViewConfig(next)}
        />
      }
      searchQuery={kanban.search.query}
      onSearchChange={kanban.search.setQuery}
      viewBar={
        <ViewStateBar
          canSave={capabilities.canEditFields}
          copy={filtersCopy}
          view={view}
          filterOpen={filterOpen}
          onFilterOpenChange={setFilterOpen}
          fields={kanban.filterFields}
          memberName={kanban.memberName}
          people={kanban.filterChoices.assignees}
          counts={{
            shown: kanban.visibleCount,
            total: kanban.totalCount,
          }}
          now={now}
          timeZone={timeZone}
          timeZoneIsDefault={timeZoneIsDefault}
          groupFields={columnFields}
          groupFallback={
            kanban.groupByField
              ? { fieldId: kanban.groupByField.id }
              : undefined
          }
          afterGroup={
            <TabletSlot
              slot={TABLET_SLOT.toolbarStack}
              className="hidden items-center tablet:flex"
            />
          }
          laneFields={groupableFields(kanban.filterFields)}
          blockedLaneFieldId={kanban.groupByField?.id}
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
      }
      canAdd={capabilities.canCreateCard}
      onAddItem={() => setCreate({ kind: "toolbar" })}
    />
  );
  // While the board loads the header, tabs and toolbar stay and the lanes are skeletons.
  if (kanban.isLoading)
    return (
      <div
        data-kanban-board=""
        className="relative flex h-full min-h-0 flex-col overflow-hidden bg-background font-body select-none"
      >
        <div className="px-4 sm:px-6">{toolbar}</div>
        <KanbanSkeleton />
      </div>
    );
  const boardEmpty = !kanban.filterActive && kanban.totalCount === 0;
  if (kanban.error || !board) return <KanbanUnavailableState />;

  const lanesByCard = new Map<string, KanbanStatusLane>();
  for (const lane of kanban.allLanes) {
    for (const card of lane.cards) lanesByCard.set(card.id, lane);
  }

  /** Remembers where the card is before a write, for its failure toast. */
  const noteOrigin = (
    cardId: string,
    move?: { optionId: string; retry: () => void },
  ) => {
    const card = allCards.find((candidate) => candidate.id === cardId);
    const lane = lanesByCard.get(cardId);
    writeOrigins.current.set(cardId, {
      title: card?.title ?? "",
      lane: lane ? laneName(lane, laneCopy, isStatusGrouping) : "",
      target: move ? columnLabel(move.optionId) : undefined,
    });
    if (move) retryWrites.current.set(cardId, move.retry);
    clearFailedMove(cardId);
  };

  /** Writes a lane's value for a card: Status, or the grouping Select field. */
  const writeLaneValue = (
    cardId: string,
    optionId: string,
    onSuccess?: () => void,
  ) => {
    noteOrigin(cardId, {
      optionId,
      retry: () => writeLaneValue(cardId, optionId, onSuccess),
    });
    if (isStatusGrouping) {
      kanban.updateStatus.mutate(
        { cardId, statusOptionId: optionId },
        { onSuccess },
      );
      return;
    }
    if (!kanban.groupByField) return;
    kanban.updateFieldValue.mutate(
      {
        cardId,
        fieldDefinitionId: kanban.groupByField.id,
        value: optionId,
      },
      { onSuccess },
    );
  };

  /**
   * Once the server has kept the move, says where the card went and keeps "Hoàn tác" for a few seconds:
   * it puts the lane's value back. A refused drop says nothing here (its failure toast does).
   */
  const announceLaneMove = (cardId: string, toOptionId: string) => {
    const from = lanesByCard.get(cardId)?.optionId;
    if (!from || from === toOptionId) return () => undefined;
    const t = filtersCopy.lane.toast;
    return () =>
      toast(
        fillFiltersCopy(t.movedColumn, {
          title:
            allCards.find((candidate) => candidate.id === cardId)?.title ?? "",
          column: columnLabel(toOptionId),
        }),
        {
          action: {
            label: t.undo,
            onClick: () => writeLaneValue(cardId, from),
          },
        },
      );
  };

  const handleDrop = (drop: KanbanLaneDrop) => {
    if (!capabilities.canMoveCard) return;
    if (drop.kind === "status") {
      writeLaneValue(
        drop.cardId,
        drop.optionId,
        announceLaneMove(drop.cardId, drop.optionId),
      );
      return;
    }
    if (drop.kind === "place") {
      if (!kanban.groupByField) return;
      noteOrigin(drop.cardId, {
        optionId: drop.optionId,
        retry: () => handleDrop(drop),
      });
      kanban.dropItemInLane.mutate(
        {
          cardId: drop.cardId,
          lane: {
            fieldDefinitionId: kanban.groupByField.id,
            optionId: drop.optionId,
          },
          placement: {
            listId: drop.listId,
            position: drop.position,
            previousSiblingId: drop.previousSiblingId,
            nextSiblingId: drop.nextSiblingId,
          },
        },
        { onSuccess: announceLaneMove(drop.cardId, drop.optionId) },
      );
      return;
    }
    noteOrigin(drop.cardId);
    kanban.moveCard.mutate({
      cardId: drop.cardId,
      listId: drop.listId,
      position: drop.position,
      previousSiblingId: drop.previousSiblingId,
      nextSiblingId: drop.nextSiblingId,
    });
  };

  // Swimlanes: rows of a second field under the columns. The lane field can
  // never be the column field.
  const laneRef = view.applied.lane;
  const laneField: FilterField | undefined =
    laneRef && laneRef.fieldId !== kanban.groupByField?.id
      ? kanban.filterFields.find((field) => field.id === laneRef.fieldId)
      : undefined;
  const swimlanes: KanbanStatusLane[] = laneField
    ? groupCardsByField(
        shownLanes.flatMap((lane) => lane.cards),
        laneField,
        laneRef?.bucket,
        { person: kanban.memberName, bucket: kanbanView.bucketLabel },
      )
    : [];
  const swimlaneLabel = (lane: KanbanStatusLane | undefined) =>
    lane?.name ||
    (laneField?.field.fieldType === "person"
      ? filtersCopy.lane.unassigned
      : filtersCopy.lane.none_);
  const columnLabel = (optionId: string | null) => {
    const column = kanban.allLanes.find((lane) => lane.optionId === optionId);
    return column ? laneName(column, laneCopy, isStatusGrouping) : "";
  };

  /**
   * Writes the column and/or the lane value of a card. The two are separate
   * writes, one after the other: the second names the version the first
   * answered. Resolves false when one failed (its failure toast is shown).
   */
  const writeCell = async (
    cardId: string,
    column: { set: boolean; value: string | null },
    lane: { set: boolean; value: unknown },
    /** The column the card was in: put back when the lane write fails after the column write kept (both or neither). */
    columnBefore?: string | null,
  ): Promise<boolean> => {
    noteOrigin(cardId);
    let columnKept = false;
    try {
      if (column.set) {
        if (!isStatusGrouping && kanban.groupByField)
          await kanban.updateFieldValue.mutateAsync({
            cardId,
            fieldDefinitionId: kanban.groupByField.id,
            value: column.value,
          });
        else if (column.value !== null)
          await kanban.updateStatus.mutateAsync({
            cardId,
            statusOptionId: column.value,
          });
        columnKept = true;
      }
      if (lane.set && laneField)
        await kanban.updateFieldValue.mutateAsync({
          cardId,
          fieldDefinitionId: laneField.id,
          value: lane.value,
        });
      return true;
    } catch {
      if (columnKept && columnBefore !== undefined)
        await writeCell(
          cardId,
          { set: true, value: columnBefore },
          { set: false, value: null },
        );
      return false;
    }
  };

  const handleSwimlaneDrop = ({
    card,
    plan,
    toColumn,
    toLane,
  }: SwimlaneDrop) => {
    if (!capabilities.canMoveCard || !laneField) return;
    const previous = {
      column: lanesByCard.get(card.id)?.optionId ?? null,
      lane: laneField.read(card),
    };
    const t = filtersCopy.lane.toast;
    const values = {
      title: card.title,
      column: columnLabel(toColumn),
      field: laneField.name,
      value: swimlaneLabel(swimlanes.find((lane) => lane.optionId === toLane)),
    };
    void writeCell(
      card.id,
      { set: plan.columnChanged, value: toColumn },
      { set: plan.laneChanged, value: plan.lane },
      previous.column,
    ).then((kept) => {
      if (!kept) return;
      toast(
        fillFiltersCopy(
          plan.columnChanged && plan.laneChanged
            ? t.moved
            : plan.laneChanged
              ? t.movedLane
              : t.movedColumn,
          values,
        ),
        {
          action: {
            label: t.undo,
            onClick: () =>
              void writeCell(
                card.id,
                { set: plan.columnChanged, value: previous.column },
                { set: plan.laneChanged, value: previous.lane },
              ),
          },
        },
      );
    });
  };

  const handleSwimlaneRefused = ({
    reason,
    toColumnName,
    limit,
  }: SwimlaneRefusal) => {
    toast.error(
      `${filtersCopy.lane.refused}. ${fillFiltersCopy(
        filtersCopy.lane.refusal[reason],
        {
          field: laneField?.name ?? "",
          column: toColumnName,
          limit: limit ?? "",
        },
      )}`,
    );
  };

  const handleCreateLane = (draft: StatusOptionDraft) => {
    if (!kanban.groupByField) return;
    pendingOptionName.current = draft.name;
    addOption.mutate({ fieldId: kanban.groupByField.id, ...draft });
  };

  const handleUpdateLane = (
    lane: KanbanStatusLane,
    draft: StatusOptionDraft,
  ) => {
    if (!kanban.groupByField || !lane.optionId) return;
    pendingOptionName.current = draft.name;
    updateOption.mutate({
      fieldId: kanban.groupByField.id,
      optionId: lane.optionId,
      ...draft,
    });
  };

  const handleMoveLane = (
    lane: KanbanStatusLane,
    direction: "before" | "after",
  ) => {
    if (!kanban.groupByField || !lane.optionId) return;
    pendingOptionName.current = lane.name;
    moveOption.mutate({
      fieldId: kanban.groupByField.id,
      optionId: lane.optionId,
      direction,
    });
  };

  const handleDeleteLane = (
    lane: KanbanStatusLane,
    replacementOptionId: string | null,
  ) => {
    if (!kanban.groupByField || !lane.optionId) return;
    pendingOptionName.current = lane.name;
    removeOption.mutate({
      fieldId: kanban.groupByField.id,
      optionId: lane.optionId,
      replacementOptionId,
    });
  };

  const handleCreateItem = (lane: KanbanStatusLane, title: string) => {
    if (!capabilities.canCreateCard) {
      noGroupNotice.noPermission();
      return;
    }
    const listId = groupForNewItem(lane, kanban.groups);
    if (!listId) {
      noGroupNotice.noGroup();
      return;
    }
    const fieldValue =
      !isStatusGrouping && kanban.groupByField && lane.optionId
        ? { fieldDefinitionId: kanban.groupByField.id, value: lane.optionId }
        : null;
    return kanban.createItemInLane.mutateAsync({
      listId,
      title,
      statusOptionId: isStatusGrouping ? lane.optionId : null,
      fieldValue,
    });
  };

  /** Sets or removes one lane's WIP limit on the view (a warning, never a rule). */
  const handleWipLimitChange = (
    lane: KanbanStatusLane,
    limit: number | null,
  ) => {
    if (!lane.optionId) return;
    const next = { ...kanbanSettings.wipLimits };
    if (limit === null) delete next[lane.optionId];
    else next[lane.optionId] = limit;
    updateViewConfig({ wipLimits: next });
  };

  const handleSheetFieldValue = (payload: UpdateFieldValueInput) => {
    // The Status pill writes through the status endpoint, like a drag, so the
    // card moves to its lane at once (the keyboard alternative to dragging).
    if (
      payload.fieldDefinitionId === kanban.statusField?.id &&
      typeof payload.value === "string"
    ) {
      noteOrigin(payload.cardId);
      return kanban.updateStatus.mutateAsync({
        cardId: payload.cardId,
        statusOptionId: payload.value,
      });
    }
    noteOrigin(payload.cardId);
    return kanban.updateFieldValue.mutateAsync(payload);
  };

  const handleMoveToGroup = (cardId: string, groupId: string) => {
    const target = kanban.groups.find((group) => group.id === groupId);
    if (!target) return;
    const last = [...target.cards]
      .filter((card) => card.id !== cardId)
      .sort((a, b) => a.position - b.position)
      .at(-1);
    noteOrigin(cardId);
    // "Chuyển nhóm" appends the item to the end of the chosen group.
    return kanban.moveCard.mutateAsync({
      cardId,
      listId: groupId,
      position: generatePosition(last?.position, undefined),
      previousSiblingId: last?.id ?? null,
      nextSiblingId: null,
    });
  };

  /**
   * The popup's column picker: the lanes' counts and limits, and a move to a lane at its top or bottom,
   * read like a drag (`getKanbanDrop`) so the same writes run.
   */
  const lanePicker: CardPopupLanePicker | undefined = kanban.groupByField
    ? {
        counts: Object.fromEntries(
          kanban.allLanes.flatMap((lane) =>
            lane.optionId ? [[lane.optionId, lane.total]] : [],
          ),
        ),
        wipLimits: kanbanSettings.wipLimits,
        placeableLanes: (cardId) => {
          const lanes = kanban.allLanes;
          const listId = lanes
            .flatMap((lane) => lane.cards)
            .find((card) => card.id === cardId)?.listId;
          return lanes.flatMap((lane) =>
            lane.optionId &&
            lane.cards.some(
              (card) => card.id !== cardId && card.listId === listId,
            )
              ? [lane.optionId]
              : [],
          );
        },
        move: (cardId, optionId, position) => {
          const field = kanban.groupByField!;
          const lanes = kanban.allLanes;
          const drop = getKanbanDrop({
            lanes,
            arrangement: moveCardInArrangement(
              arrangeKanbanLanes(lanes),
              cardId,
              kanbanLaneKey(optionId),
              position === "top" ? 0 : Number.MAX_SAFE_INTEGER,
            ),
            cardId,
            groups: kanban.groups,
          });
          noteOrigin(cardId, {
            optionId,
            retry: () => {
              lanePicker?.move(cardId, optionId, position).catch(() => {
                // The write hook has already shown the failure.
              });
            },
          });
          if (drop?.kind === "place")
            return kanban.dropItemInLane.mutateAsync({
              cardId,
              lane: {
                fieldDefinitionId: field.id,
                optionId,
              },
              placement: {
                listId: drop.listId,
                position: drop.position,
                previousSiblingId: drop.previousSiblingId,
                nextSiblingId: drop.nextSiblingId,
              },
            });
          if (drop?.kind === "reorder")
            return kanban.moveCard.mutateAsync({
              cardId,
              listId: drop.listId,
              position: drop.position,
              previousSiblingId: drop.previousSiblingId,
              nextSiblingId: drop.nextSiblingId,
            });
          // Only the lane's value changes (its natural place), or the card is not in the lanes shown.
          return isStatusGrouping
            ? kanban.updateStatus.mutateAsync({
                cardId,
                statusOptionId: optionId,
              })
            : kanban.updateFieldValue.mutateAsync({
                cardId,
                fieldDefinitionId: field.id,
                value: optionId,
              });
        },
      }
    : undefined;

  const handleCopyLink = (cardId: string) => {
    const link = buildCardLink(window.location, cardId);
    void Promise.resolve()
      .then(() => navigator.clipboard.writeText(link))
      .then(
        () => toast.success(laneCopy.linkCopied),
        () => toast.error(laneCopy.linkCopyFailed),
      );
  };

  /** The hover and focus actions of a card, each only when the viewer may do it. */
  const personField = board ? findItemFields(board).person : undefined;
  // Picking cards: only an editor can change several items at once.
  const cardSelection = capabilities.canEditFields
    ? picking.selection
    : undefined;
  const cardActions: KanbanCardActions | undefined = capabilities.canEditFields
    ? {
        currentUserId,
        failureOf: (cardId) => {
          const message = failedMoves.get(cardId);
          return message
            ? { message, onRetry: () => retryWrites.current.get(cardId)?.() }
            : undefined;
        },
        onAssignMe:
          currentUserId && personField
            ? (card) => {
                noteOrigin(card.id);
                kanban.updateFieldValue.mutate({
                  cardId: card.id,
                  fieldDefinitionId: personField.id,
                  value: [
                    ...(card.members ?? [])
                      .map((member) => member.userId)
                      .filter((id): id is string => typeof id === "string"),
                    currentUserId,
                  ],
                });
              }
            : undefined,
        onSetDue: (card, day) => {
          noteOrigin(card.id);
          updateCardDetails.mutate(
            {
              cardId: card.id,
              patch: { dueDate: day === null ? null : `${day}T00:00:00.000Z` },
            },
            {
              onError: () =>
                toast.error(
                  fillCopy(laneCopyRef.current.saveFailed, {
                    title: card.title,
                  }),
                  { id: `kanban-write-${card.id}` },
                ),
            },
          );
        },
        quickEdit: {
          labels: boardLabels.data ?? [],
          members: (board?.members ?? []).filter(
            (member) => member.role !== "viewer",
          ),
          onRename: (card, title) => {
            noteOrigin(card.id);
            updateCardDetails.mutate(
              { cardId: card.id, patch: { title } },
              {
                onError: () =>
                  toast.error(
                    fillCopy(laneCopyRef.current.saveFailed, {
                      title: card.title,
                    }),
                    { id: `kanban-write-${card.id}` },
                  ),
              },
            );
          },
          onToggleLabel: (card, labelId, on) =>
            toggleCardLabel.mutate({ cardId: card.id, labelId, on }),
          onToggleMember: (card, userId, on) => {
            if (!personField) return;
            const current = (card.members ?? [])
              .map((member) => member.userId)
              .filter((id): id is string => typeof id === "string");
            noteOrigin(card.id);
            kanban.updateFieldValue.mutate({
              cardId: card.id,
              fieldDefinitionId: personField.id,
              value: on
                ? [...current.filter((id) => id !== userId), userId]
                : current.filter((id) => id !== userId),
            });
          },
          onSetDates: (card, dates) => {
            noteOrigin(card.id);
            updateCardDetails.mutate({
              cardId: card.id,
              patch: {
                startDate: dates.start ? `${dates.start}T00:00:00.000Z` : null,
                dueDate: dates.due ? `${dates.due}T00:00:00.000Z` : null,
              },
            });
          },
          onOpen: (cardId) => handleOpenDetail(cardId),
        },
        onCopyLink: handleCopyLink,
        onDuplicate: capabilities.canCreateCard
          ? removal.requestDuplicate
          : undefined,
        onArchive: capabilities.canDelete ? removal.requestArchive : undefined,
        onDelete: capabilities.canDelete ? removal.requestDelete : undefined,
        onMoveToBoard: capabilities.canMoveCard
          ? removal.requestMove
          : undefined,
        // The same one write the drop and the popup picker run; a refusal reports itself through the write hook.
        move: lanePicker
          ? {
              lanes: kanban.allLanes.flatMap((lane) =>
                lane.optionId
                  ? [
                      {
                        id: lane.optionId,
                        title: laneName(lane, laneCopy, isStatusGrouping),
                        category: lane.category,
                        color: lane.color,
                      },
                    ]
                  : [],
              ),
              currentLaneId: (card) =>
                lanesByCard.get(card.id)?.optionId ?? null,
              onMove: (card, optionId) => {
                lanePicker.move(card.id, optionId, "bottom").catch(() => {
                  // The write hook has already shown the failure and rolled the card back.
                });
              },
              disabledReason: capabilities.canMoveCard
                ? undefined
                : cardCopy.moveDenied,
            }
          : undefined,
      }
    : // View only: no quick actions, the options button still opens the menu (BC-Kanban kbf-0).
      { onCopyLink: handleCopyLink };

  const linkCandidates: ItemSheetLinkCandidate[] = kanban.groups
    .flatMap((group) => group.cards)
    .filter((entry) => entry.id !== selectedItemId && !entry.parentItemId)
    .map((entry) => ({
      id: entry.id,
      key: entry.itemKey ?? null,
      title: entry.title,
      statusCategory: entry.statusCategory ?? null,
      statusName:
        kanban.statusField?.options.find(
          (option) => option.id === entry.fieldValues?.[kanban.statusField!.id],
        )?.label ?? null,
    }));
  const sheetGroups: ItemSheetGroup[] = [...kanban.groups]
    .sort((a, b) => a.position - b.position)
    .map((group) => ({ id: group.id, title: group.title }));

  return (
    <KanbanFaceProvider
      fields={faceFieldDefinitions}
      cardFieldIds={kanbanSettings.cardFieldIds ?? []}
      cardHideEmpty={kanbanSettings.cardHideEmpty ?? []}
    >
      <div
        ref={boardRef}
        data-kanban-board=""
        tabIndex={-1}
        onKeyDown={(event) => {
          if (event.key === "Escape" && picking.selectedIds.size > 0)
            picking.clear();
        }}
        className="relative flex h-full min-h-0 flex-col overflow-hidden bg-background font-body select-none focus:outline-hidden"
      >
        <div
          className={tablet ? "px-4" : "px-4 sm:px-6"}
          inert={boardCovered || undefined}
          aria-hidden={boardCovered || undefined}
        >
          {laneField && !capabilities.canMoveCard ? (
            // WM-Swimlanes "Người chỉ xem": a warning band between the header and the toolbar.
            <div
              role="note"
              data-lane-viewer-note=""
              className="mt-2.5 flex items-start gap-2 rounded-[9px] bg-warning-subtle px-3 py-[9px] text-crumb text-warning-subtle-foreground ring-1 ring-warning/50 ring-inset"
            >
              <Lock className="size-4 shrink-0" aria-hidden="true" />
              <span>{filtersCopy.lane.viewerNote}</span>
            </div>
          ) : null}
          {toolbar}
        </div>

        <div
          className="relative min-h-0 flex-1"
          inert={boardCovered || undefined}
          aria-hidden={boardCovered || undefined}
        >
          <div
            ref={scrollerRef}
            data-kanban-scroller=""
            onScroll={measureEdges}
            className={cn(
              "h-full min-h-0 overflow-auto px-4 pt-1 pb-6",
              !tablet && "sm:px-6",
            )}
          >
            {kanban.filterActive && kanban.visibleCount === 0 ? (
              <FilterNoResults
                copy={filtersCopy}
                conditions={view.applied.filter.conditions.length}
                total={kanban.totalCount}
                onEdit={() => setFilterOpen(true)}
                onClear={() => {
                  view.setFilter({ ...view.state.filter, conditions: [] });
                  if (kanban.search.query !== "") kanban.search.setQuery("");
                }}
              />
            ) : laneField ? (
              <>
                <KanbanSwimlaneBoard
                  initialCollapsed={collapsedSwimlanes.initial}
                  onCollapsedChange={collapsedSwimlanes.onChange}
                  copy={filtersCopy}
                  cardCopy={cardCopy}
                  now={now}
                  columns={shownLanes}
                  swimlanes={swimlanes}
                  laneField={laneField}
                  columnName={(column) =>
                    laneName(column, laneCopy, isStatusGrouping)
                  }
                  laneName={swimlaneLabel}
                  wipLimits={kanbanSettings.wipLimits}
                  selectedItemId={selectedItemId}
                  filterActive={kanban.filterActive}
                  canDrag={capabilities.canMoveCard}
                  cardActions={cardActions}
                  cardSelection={cardSelection}
                  onOpenDetails={handleOpenDetail}
                  onDrop={handleSwimlaneDrop}
                  onRefused={handleSwimlaneRefused}
                />
              </>
            ) : (
              <KanbanBoard
                reveal={reveal}
                onDraggingChange={setDragging}
                lanes={shownLanes}
                groupOrder={kanban.groups}
                copy={laneCopy}
                cardCopy={cardCopy}
                now={now}
                selectedItemId={selectedItemId}
                filterActive={kanban.filterActive}
                boardEmpty={boardEmpty}
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
                formCopy={
                  showStatusControls && kanban.groupByField
                    ? statusFormCopy
                    : undefined
                }
                onCreateLane={handleCreateLane}
                onUpdateLane={handleUpdateLane}
                lanePending={addOption.isPending}
                onMoveLane={handleMoveLane}
                onDeleteLane={handleDeleteLane}
                laneWritePending={
                  moveOption.isPending || removeOption.isPending
                }
                onCreateGroup={
                  capabilities.canCreateCard
                    ? (draft) => addGroup(draft)
                    : undefined
                }
                groupTitles={kanban.groups.map((group) => group.title)}
                groupPending={addingGroup}
                laneCardLimit={kanbanSettings.laneCardLimit}
                lanePaging={
                  kanban.paging
                    ? {
                        loadMore: (lane) =>
                          void kanban.loadMoreLane(lane.optionId),
                        loading: kanban.lanePages.loading,
                        failed: kanban.lanePages.failed,
                      }
                    : undefined
                }
                keyboardMove={kanbanSettings.keyboardMove}
                cardActions={cardActions}
                cardSelection={cardSelection}
                wipLimits={kanbanSettings.wipLimits}
                onWipLimitChange={
                  showStatusControls ? handleWipLimitChange : undefined
                }
                onRequestCreate={
                  capabilities.canCreateCard
                    ? (lane) => setCreate({ kind: "lane", lane, title: "" })
                    : undefined
                }
                onExpandCreate={
                  capabilities.canCreateCard
                    ? (lane, title) => setCreate({ kind: "quick", lane, title })
                    : undefined
                }
                initialCollapsed={collapsedLanes.initial}
                onCollapsedChange={collapsedLanes.onChange}
              />
            )}
            {boardEmpty && !laneField ? (
              <div
                role="status"
                data-kanban-empty-note=""
                className="mt-3.5 flex items-start gap-2.5 rounded-[10px] bg-brand-subtle px-3.5 py-2.5 text-[13px] leading-[1.4] text-brand-subtle-foreground dark:bg-[rgba(90,169,255,0.16)] dark:text-[#7dbbff]"
              >
                <Info aria-hidden="true" className="mt-0.5 size-4 shrink-0" />
                <span className="flex min-w-0 flex-1 flex-col gap-px">
                  <b className="font-semibold text-foreground">
                    {laneCopy.emptyBoardNote}
                  </b>
                  <span>{laneCopy.emptyBoardNoteBody}</span>
                </span>
              </div>
            ) : null}
          </div>
          {(["left", "right"] as const).map((side) =>
            edges[side] ? (
              <div
                key={side}
                data-scroll-edge={side}
                className={cn(
                  "pointer-events-none absolute inset-y-0 z-10 flex w-14 items-center",
                  side === "right"
                    ? "right-0 justify-end bg-gradient-to-l from-background via-background/70 to-transparent pr-2"
                    : "left-0 justify-start bg-gradient-to-r from-background via-background/70 to-transparent pl-2",
                )}
              >
                <button
                  type="button"
                  tabIndex={-1}
                  aria-label={
                    side === "right"
                      ? laneCopy.scrollRight
                      : laneCopy.scrollLeft
                  }
                  onClick={() =>
                    scrollerRef.current?.scrollBy({
                      left:
                        (side === "right" ? 1 : -1) *
                        Math.max(
                          280,
                          Math.round(
                            (scrollerRef.current?.clientWidth ?? 600) * 0.7,
                          ),
                        ),
                      behavior: prefersReducedMotion() ? "auto" : "smooth",
                    })
                  }
                  className="pointer-events-auto flex size-8 items-center justify-center rounded-full border border-line-strong bg-popover text-muted-foreground shadow-card-hover hover:text-foreground"
                >
                  {side === "right" ? (
                    <ChevronRight aria-hidden="true" className="size-4" />
                  ) : (
                    <ChevronLeft aria-hidden="true" className="size-4" />
                  )}
                </button>
              </div>
            ) : null,
          )}
        </div>

        {board && cardSelection ? (
          <KanbanBulkBar
            boardId={boardId}
            workspaceId={workspaceId}
            board={board}
            capabilities={capabilities}
            selectedIds={picking.selectedIds}
            onClear={picking.clear}
            onPendingArchiveChange={setArchivingIds}
          />
        ) : null}

        {board && capabilities.canCreateCard ? (
          <CardCreatePopup
            open={createOpen}
            onOpenChange={(next) => {
              if (!next) setCreate(null);
            }}
            workspaceId={workspaceId}
            board={board}
            currentUserId={currentUserId}
            groups={kanban.groups}
            defaultGroupId={
              create && create.kind !== "toolbar"
                ? (groupForNewItem(create.lane, kanban.groups) ??
                  kanban.groups[0]?.id ??
                  "")
                : (kanban.groups[0]?.id ?? "")
            }
            defaultStatusId={
              create && create.kind !== "toolbar" && isStatusGrouping
                ? create.lane.optionId
                : null
            }
            defaultTitle={
              create && create.kind !== "toolbar" ? create.title : undefined
            }
            laneValue={
              create &&
              create.kind !== "toolbar" &&
              !isStatusGrouping &&
              kanban.groupByField &&
              create.lane.optionId
                ? {
                    fieldDefinitionId: kanban.groupByField.id,
                    value: create.lane.optionId,
                  }
                : null
            }
            onOpenItem={(itemId) => {
              setCreate(null);
              onSelectedItemChange?.(itemId);
            }}
          />
        ) : null}
        {selectedItemId ? (
          <KanbanSelectedItem
            board={board}
            onOpenItem={openItem}
            cardId={selectedItemId}
            capabilities={capabilities}
            groups={sheetGroups}
            groupField={kanban.groupByField}
            linkCandidates={linkCandidates}
            onClose={handleClosePanel}
            onRestoreFocus={restoreFocus}
            onCopyLink={handleCopyLink}
            onMoveToGroup={handleMoveToGroup}
            onUpdateFieldValue={handleSheetFieldValue}
            lanePicker={lanePicker}
            onDuplicate={removal.requestDuplicate}
          />
        ) : null}
        {removal.dialogs}
      </div>
    </KanbanFaceProvider>
  );
}

export interface SelectedItemProps {
  board: Board;
  cardId: string;
  capabilities: TaskDetailCapabilities;
  groups: readonly ItemSheetGroup[];
  /** Items a new link can point at (the board's items that are not sub-items). */
  linkCandidates?: readonly ItemSheetLinkCandidate[];
  onClose: () => void;
  onRestoreFocus: (cardId: string) => void;
  onCopyLink: (cardId: string) => void;
  onMoveToGroup: (cardId: string, groupId: string) => void | Promise<unknown>;
  /** The Select or Status field the view lays its columns by: the popup's column picker changes it. */
  groupField?: FieldDefinition;
  onUpdateFieldValue: (
    payload: UpdateFieldValueInput,
  ) => void | Promise<unknown>;
  onDuplicate: (cardId: string) => void;
  /** Opens an item of a board (the item a move landed on, the item that blocks one). */
  onOpenItem?: (boardId: string, itemId: string) => void;
  /** The column picker's counts, limits and positioned move; absent: it moves to the end of a lane as a field write. */
  lanePicker?: CardPopupLanePicker;
  /** A dialog over the page (the default) or a pane beside the view (the tablet Table's split). */
  presentation?: "modal" | "pane";
}

/** The refusal key of the deleted notice for a failed restore. */
function restoreReasonOf(error: unknown): string {
  switch (classifyRestoreError(error)) {
    case "windowClosed":
      return "restoreWindowClosed";
    case "parentDeleted":
      return "restoreParentDeleted";
    case "denied":
      return "restoreDenied";
    default:
      return "restoreFailed";
  }
}

/**
 * Resolves the selected card and hands it to the sheet. An id that fails to
 * load, or resolves to a card outside this board and workspace, is stale:
 * nothing renders and the `item` parameter is cleared with history replace.
 * A cached card is never trusted once the query has errored (a deleted card
 * stays in the query cache). While the next card of K/J loads, the sheet
 * keeps showing the previous one, so it stays mounted and keeps focus.
 */
export function KanbanSelectedItem({
  board,
  cardId,
  onClose,
  onRestoreFocus,
  presentation,
  ...rest
}: SelectedItemProps) {
  const { card, error, isLoading, refetch } = useCard(
    cardId,
    board.workspaceId,
  );
  // A card that is gone, moved to another board, or not for this viewer reads
  // the same: one generic message, nothing about what or why.
  const kind = (error as { kind?: string } | null)?.kind;
  const otherWorkspace =
    card !== undefined && card.workspaceId !== board.workspaceId;
  // A card the person can still read on another board of this workspace was moved: it says where.
  const moved =
    card !== undefined && !otherWorkspace && card.boardId !== board.id;
  const gone = kind === "not_found" || kind === "forbidden" || otherWorkspace;
  // A deleted card answers not-found with its own code to a person who reads the board: it says who deleted it.
  const deleted = deletedItemOf(error);
  const failed = Boolean(error) && !gone;
  const lastShown = useRef<CardDetail | null>(null);
  if (card && !gone && !moved) lastShown.current = card;
  const restoreDeleted = useRestoreDeletedItem(board.workspaceId, board.id);
  const shown =
    gone || moved ? null : failed && !card ? null : (card ?? lastShown.current);
  const restore = (event: Event) => {
    event.preventDefault();
    onRestoreFocus(lastShown.current?.id ?? cardId);
  };
  const withValues = useMemo(
    () =>
      shown ? withReservedFieldValues(shown, board.fieldDefinitions) : null,
    [shown, board.fieldDefinitions],
  );

  // BC-Notices: a card deleted or moved while it was open keeps its last read under the notice (nothing saves);
  // a card the person may no longer read keeps the one generic message.
  const covered =
    (deleted || moved) && lastShown.current
      ? withReservedFieldValues(lastShown.current, board.fieldDefinitions)
      : null;
  if (covered)
    return (
      <KanbanSelectedItemReady
        board={board}
        card={covered}
        onClose={onClose}
        onCloseAutoFocus={restore}
        presentation={presentation}
        {...rest}
        notice={
          deleted ? (
            <PopupDeletedNotice
              by={deleted.byName ?? null}
              draft={unsentCommentOf(cardId) || undefined}
              onClose={onClose}
              onRestore={
                rest.capabilities.canDelete
                  ? async () => {
                      try {
                        await restoreDeleted.mutateAsync(cardId);
                        await refetch();
                      } catch (failure) {
                        throw restoreReasonOf(failure);
                      }
                    }
                  : undefined
              }
            />
          ) : (
            <PopupMovedNotice
              boardTitle={card?.boardTitle ?? ""}
              draft={unsentCommentOf(cardId) || undefined}
              onOpen={
                rest.onOpenItem && card
                  ? () => rest.onOpenItem?.(card.boardId, card.id)
                  : undefined
              }
              onClose={onClose}
            />
          )
        }
      />
    );
  if (!withValues)
    return (
      <CardPopupSurface
        status={
          deleted
            ? "deleted"
            : moved
              ? "moved"
              : gone
                ? "gone"
                : failed
                  ? "error"
                  : isLoading
                    ? "loading"
                    : "loading"
        }
        deletedBy={deleted?.byName ?? null}
        onRestoreDeleted={
          deleted && rest.capabilities.canDelete
            ? async () => {
                try {
                  await restoreDeleted.mutateAsync(cardId);
                  await refetch();
                } catch (failure) {
                  throw restoreReasonOf(failure);
                }
              }
            : undefined
        }
        movedTo={
          moved && card
            ? {
                boardTitle: card.boardTitle,
                onOpen: rest.onOpenItem
                  ? () => rest.onOpenItem?.(card.boardId, card.id)
                  : undefined,
              }
            : undefined
        }
        keptDraft={
          deleted || gone || moved ? unsentCommentOf(cardId) : undefined
        }
        onClose={onClose}
        onRetry={refetch}
        onCloseAutoFocus={restore}
        presentation={presentation}
      />
    );
  return (
    <KanbanSelectedItemReady
      board={board}
      card={withValues}
      onClose={onClose}
      onCloseAutoFocus={restore}
      presentation={presentation}
      {...rest}
    />
  );
}

function KanbanSelectedItemReady({
  board,
  card,
  capabilities: boardCapabilitiesOfViewer,
  groups,
  groupField,
  linkCandidates,
  onClose,
  onCloseAutoFocus,
  onCopyLink,
  onMoveToGroup,
  onUpdateFieldValue,
  onOpenItem,
  lanePicker,
  presentation,
  notice,
}: Omit<SelectedItemProps, "cardId" | "onRestoreFocus"> & {
  card: CardDetail;
  onCloseAutoFocus: (event: Event) => void;
  /** The deleted or moved notice over the card's last read: the card reads and nothing saves. */
  notice?: ReactNode;
}) {
  // An item inside an archived group stays readable and takes no write.
  const capabilities = useItemCapabilities(
    boardCapabilitiesOfViewer,
    card.boardId,
    card.workspaceId,
    card.listId,
  );
  const files = useCardFiles(card.id, card.workspaceId);
  const activity = useCardActivity(card.id, card.workspaceId);
  const updates = useCardComments(card.id, card.workspaceId);
  const checklistWrites = useChecklistWrites(
    card.id,
    card.boardId,
    card.workspaceId,
    card.checklists,
  );
  const updateCard = useUpdateCard(card.boardId, card.workspaceId);
  const boardLabels = useBoardLabels(card.boardId, card.workspaceId);
  const labelWrites = useCardLabelWrites(
    card.id,
    card.boardId,
    card.workspaceId,
  );
  const createUpdate = useCreateCardUpdate(card.id, card.workspaceId);
  const updateUpdate = useUpdateCardUpdate(card.id, card.workspaceId);
  const deleteUpdate = useDeleteCardUpdate(card.id, card.workspaceId);
  const toggleReaction = useToggleCommentReaction(card.id, card.workspaceId);
  const setUpdateResolved = useSetCardUpdateResolved(card.id, card.workspaceId);
  const itemLinks = useItemLinks(card.id, card.workspaceId, card.boardId);
  const approvals = useItemApprovals(card.id, card.workspaceId);
  const extras = useItemSheetExtras(card);
  // "Add with details" in the sub-items: the full create dialog with this item as the fixed parent.
  const [subItemOpen, setSubItemOpen] = useState(false);
  const { directory } = usePeopleDirectory(card.boardId, card.workspaceId);
  const viewerUserId = useBoardViewerId();
  const linksCopy = useItemPageCopy();
  const removal = useItemRemoval({
    workspaceId: card.workspaceId,
    boardId: card.boardId,
    titleOf: () => card.title,
    onGone: () => onClose(),
    onOpenItem,
  });

  return (
    <>
      <KanbanCardDetailPanel
        board={board}
        card={card}
        capabilities={capabilities}
        groups={groups}
        groupField={groupField}
        lanePicker={lanePicker}
        presentation={presentation}
        readOnly={notice ? "gone" : undefined}
        slots={notice ? { coverBanner: notice } : undefined}
        onCloseAutoFocus={onCloseAutoFocus}
        detailData={{
          files: files.data ?? card.files,
          filesLoading: files.isLoading,
          activity: activity.data ?? card.activity,
          activityLoading: activity.isLoading,
          activityFetching: activity.isFetching,
          activityError: activity.isError && activity.data === undefined,
          updates: updates.data ?? card.updates,
          updatesLoading: updates.isLoading,
          updatesError: updates.isError && updates.data === undefined,
          // One "show more" at the foot of the feed reads the next page of the comments and of the history.
          updatesMore:
            updates.hasNextPage || activity.hasNextPage
              ? {
                  loading:
                    updates.isFetchingNextPage || activity.isFetchingNextPage,
                  onMore: () => {
                    if (updates.hasNextPage) void updates.fetchNextPage();
                    if (activity.hasNextPage) void activity.fetchNextPage();
                  },
                }
              : undefined,
          ...extras.detailData,
          checklists: {
            ...checklistWrites,
            checklists: card.checklists,
            canEdit: capabilities.canEditFields,
            canConvert: !card.parentItemId,
          },
          boardLabels: boardLabels.data,
          links: itemLinks.data,
          linkCandidates,
          linksCopy,
          viewerUserId,
          approvals: approvals.approvals?.map((approval) => ({
            id: approval.id,
            status: approval.status,
            approverUserId: approval.approverUserId,
            approverName:
              directory?.people.find(
                (person) => person.userId === approval.approverUserId,
              )?.name ??
              board.members.find(
                (member) => member.userId === approval.approverUserId,
              )?.name ??
              null,
            decisionNote: approval.decisionNote,
            requestedByUserId: approval.requestedByUserId,
            requestedByName:
              directory?.people.find(
                (person) => person.userId === approval.requestedByUserId,
              )?.name ??
              board.members.find(
                (member) => member.userId === approval.requestedByUserId,
              )?.name ??
              null,
            note: approval.note,
            requestedAt: approval.requestedAt,
            decidedAt: approval.decidedAt,
            previousRequestId: approval.previousRequestId,
          })),
          approvalAnswering: approvals.answering,
          approvalsLoading: approvals.loading,
          approvalsFailed: approvals.loadFailed,
          approvalAnswerError: approvalFailure(approvals.answerError),
          approvalError: approvals.error
            ? approvalFailure(approvals.error) === "conflict"
              ? "alreadyAsked"
              : "failed"
            : null,
        }}
        callbacks={{
          onClose,
          onAddLink: (itemId, relation) =>
            itemLinks.addLink({ itemId, relation }),
          onRemoveLink: (linkId) => itemLinks.removeLink(linkId),
          onReloadApprovals: approvals.reload,
          onRequestApproval: (approverUserId) =>
            approvals.request({ approverUserId, note: null }),
          onDecideApproval: (requestId, decision, comment) =>
            approvals.decide({ requestId, decision, comment }),
          onCancelApproval: (requestId) => approvals.cancel(requestId),
          onRenameTitle: (cardId, patch) =>
            updateCard.mutate({ cardId, patch }),
          onUpdateItem: (cardId, patch) =>
            updateCard.mutateAsync({ cardId, patch }),
          onToggleLabel: (labelId, on) => labelWrites.mutate({ labelId, on }),
          onCreateLabel: (name, color) =>
            labelWrites
              .createAndApply(
                name,
                color ?? nextLabelColor(boardLabels.data?.length ?? 0),
              )
              .then(
                () => true,
                () => false,
              ),
          ...extras.callbacks,
          // One level only: a sub-item takes no sub-items of its own, with or without details.
          onAddChildItem: card.parentItemId
            ? undefined
            : extras.callbacks.onAddChildItem,
          onAddChildItemWithDetails: card.parentItemId
            ? undefined
            : () => setSubItemOpen(true),
          onDuplicate: removal.requestDuplicate,
          onArchive: removal.requestArchive,
          onUnarchive: removal.unarchiveItem,
          onComplete: removal.completeItem,
          onReopen: removal.reopenItem,
          onRemind: removal.requestRemind,
          onUpdateFieldValue,
          onRefreshActivity: () => {
            void activity.refetch();
          },
          onRefreshUpdates: () => {
            void updates.refetch();
          },
          onCreateUpdate: (input, options) =>
            createUpdate.mutate(input, {
              onSuccess: options?.onSuccess,
              onSettled: options?.onSettled,
            }),
          onToggleReaction: (commentId, emoji, on) =>
            toggleReaction.mutate({ commentId, emoji, on }),
          onUpdateUpdate: (updateId, body, expectedVersion) =>
            updateUpdate.mutate({ updateId, body, expectedVersion }),
          onDeleteUpdate: (updateId, expectedVersion) =>
            deleteUpdate.mutate({ updateId, expectedVersion }),
          onSetUpdateResolved: (updateId, resolved) =>
            setUpdateResolved.mutate(
              { updateId, resolved },
              {
                onError: (error) =>
                  toast.error(threadRefusalMessage(error, linksCopy.thread)),
              },
            ),
          onSelectTab: () => undefined,
          onCopyLink,
          onMoveToGroup,
          onDelete: removal.requestDelete,
          onMoveToBoard: removal.requestMove,
        }}
      />
      {subItemOpen ? (
        <CardCreatePopup
          open={subItemOpen}
          onOpenChange={(next) => {
            if (!next) setSubItemOpen(false);
          }}
          workspaceId={card.workspaceId}
          board={board}
          // The sub-item's group is its parent's: the dialog offers that one group only.
          groups={groups.filter((group) => group.id === card.listId)}
          defaultGroupId={card.listId}
          defaultStatusId={null}
          currentUserId={viewerUserId}
          parent={{
            itemId: card.id,
            key: card.itemKey,
            title: card.title,
            groupId: card.listId,
          }}
          onOpenItem={
            onOpenItem
              ? (itemId: string) => onOpenItem(board.id, itemId)
              : undefined
          }
        />
      ) : null}
      {removal.dialogs}
    </>
  );
}
