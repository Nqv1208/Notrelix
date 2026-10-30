import { useItemPageCopy } from "../../item-page/use-item-page-copy";
import { ItemCreateHost } from "../../item-create/item-create-host";
import { useNoGroupNotice } from "../../item-create/use-no-group-notice";
import { useAddGroup } from "../../status-option/use-add-group";
import {
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
  type CSSProperties,
} from "react";
import { ChevronLeft, ChevronRight } from "lucide-react";
import { toast } from "sonner";
import { cn, useIsMobile } from "@notrelix/ui-web";
import {
  useBoardLabels,
  useCardLabelWrites,
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
  useToggleCommentReaction,
  useMoveFieldOption,
  useRemoveFieldOption,
  useUpdateCard,
  useUpdateCardUpdate,
  useUpdateFieldOption,
  type KanbanWriteFailure,
} from "@notrelix/work-management-state";
import {
  generatePosition,
  groupCardsByField,
  groupableFields,
  resolveKanbanSettings,
  serializeCompoundFilter,
  type FilterField,
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
  ItemSheetLinkCandidate,
  ItemSheetMark,
  TaskDetailCapabilities,
} from "../../card-detail/task-detail-types";
import type { StatusOptionDraft } from "../../status-option/status-option-form";
import { useStatusOptionFormCopy } from "../../status-option/use-status-option-form-copy";
import { findItemFields } from "../../card-detail/item-field-editors";
import { useItemSheetExtras } from "../../card-detail/use-item-sheet-extras";
import { useBoardViewerId } from "../../shared/board-viewer";
import { KanbanBoard } from "./kanban-board";
import { prefersReducedMotion } from "./kanban-chrome";
import type { KanbanCardActions } from "./kanban-card-quick-actions";
import { KanbanCardDetailPanel } from "./kanban-card-detail-panel";
import { fillCopy } from "./kanban-card.copy";
import type { KanbanLaneDrop } from "./kanban-dnd";
import { emptyKanbanFilter, isKanbanCardOverdue } from "./kanban-filter";
import type { KanbanLaneCopy } from "./kanban-lane.copy";
import { KanbanSkeleton } from "./kanban-skeleton";
import { KanbanToolbar } from "./kanban-toolbar";
import { SavedFiltersControl } from "../../saved-filters/saved-filters-control";
import { fillFiltersCopy } from "../../filters/filters.copy";
import { useBoardViewState } from "../../filters/use-board-view-state";
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
  /**
   * Opens the selected item's own page (the sheet's "Mở toàn trang"). Without
   * it the sheet shows that button disabled with the reason.
   */
  onOpenItemPage?: (itemId: string) => void;
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

/** The colour of a lane's dot, as the lane header draws it. */
function laneDot(lane: KanbanStatusLane): {
  dotClassName: string;
  dotStyle?: CSSProperties;
} {
  const base =
    lane.category === "todo"
      ? "border-2 border-work-status-todo"
      : lane.category === "in_progress"
        ? "bg-work-status-doing"
        : lane.category === "review"
          ? "bg-work-status-review"
          : lane.category === "done"
            ? "bg-work-status-done"
            : "bg-muted-foreground";
  if (!lane.color) return { dotClassName: base };
  return lane.category === "todo"
    ? { dotClassName: "border-2", dotStyle: { borderColor: lane.color } }
    : { dotClassName: "", dotStyle: { backgroundColor: lane.color } };
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
  currentUserId,
  onSelectedItemChange,
  filter: filterParam,
  onFilterChange,
  groupBy: groupByParam,
  onGroupByChange,
  onOpenItemPage,
}: KanbanViewProps) {
  // Read once so every card judges "overdue" against the same instant.
  const [now] = useState(() => new Date());
  const cardCopy = useKanbanCardCopy();
  const toolbarCopy = useKanbanToolbarCopy();
  const laneCopy = useKanbanLaneCopy();
  const statusFormCopy = useStatusOptionFormCopy();
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
    new Map<string, { title: string; lane: string }>(),
  );
  const laneCopyRef = useRef(laneCopy);
  laneCopyRef.current = laneCopy;
  const onWriteError = useCallback((failure: KanbanWriteFailure) => {
    const copy = laneCopyRef.current;
    if (failure.kind === "create") {
      toast.error(fillCopy(copy.createFailed, { title: failure.title ?? "" }));
      return;
    }
    const cardId = failure.cardId ?? "";
    const origin = writeOrigins.current.get(cardId);
    writeOrigins.current.delete(cardId);
    const message =
      failure.kind === "field"
        ? fillCopy(copy.saveFailed, { title: origin?.title ?? "" })
        : fillCopy(copy.moveFailed, {
            title: origin?.title ?? "",
            lane: origin?.lane ?? "",
          });
    // One toast per card: a second failure of the same card replaces it.
    toast.error(message, { id: `kanban-write-${cardId}` });
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
  });
  const updateCardDetails = useUpdateCard(boardId, workspaceId);
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
  const createOpen = create !== null;
  const canCreate = capabilities.canCreateCard;
  const collapsedLanes = useKanbanCollapsedLanes(
    kanbanCollapsedLanesKey(workspaceId, savedViewId ?? boardId),
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
  const allCards = useMemo(
    () => kanban.allLanes.flatMap((lane) => lane.cards),
    [kanban.allLanes],
  );
  // K/J follow the lanes as shown: lane order, then card order.
  const laneOrder = useMemo(
    () => kanban.lanes.flatMap((lane) => lane.cards.map((card) => card.id)),
    [kanban.lanes],
  );

  const lanesReady = !kanban.isLoading && Boolean(board);
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

  if (kanban.isLoading) return <KanbanSkeleton />;
  if (kanban.error || !board) return <KanbanUnavailableState />;

  const lanesByCard = new Map<string, KanbanStatusLane>();
  for (const lane of kanban.allLanes) {
    for (const card of lane.cards) lanesByCard.set(card.id, lane);
  }

  /** Remembers where the card is before a write, for its failure toast. */
  const noteOrigin = (cardId: string) => {
    const card = allCards.find((candidate) => candidate.id === cardId);
    const lane = lanesByCard.get(cardId);
    writeOrigins.current.set(cardId, {
      title: card?.title ?? "",
      lane: lane ? laneName(lane, laneCopy, isStatusGrouping) : "",
    });
  };

  /** Writes a lane's value for a card: Status, or the grouping Select field. */
  const writeLaneValue = (cardId: string, optionId: string) => {
    noteOrigin(cardId);
    if (isStatusGrouping) {
      kanban.updateStatus.mutate({ cardId, statusOptionId: optionId });
      return;
    }
    if (!kanban.groupByField) return;
    kanban.updateFieldValue.mutate({
      cardId,
      fieldDefinitionId: kanban.groupByField.id,
      value: optionId,
    });
  };

  const handleDrop = (drop: KanbanLaneDrop) => {
    if (!capabilities.canMoveCard) return;
    if (drop.kind === "status") {
      writeLaneValue(drop.cardId, drop.optionId);
      return;
    }
    if (drop.kind === "place") {
      if (!kanban.groupByField) return;
      noteOrigin(drop.cardId);
      kanban.dropItemInLane.mutate({
        cardId: drop.cardId,
        lane: {
          fieldDefinitionId: kanban.groupByField.id,
          optionId: drop.optionId,
          viaStatusEndpoint: isStatusGrouping,
        },
        placement: {
          listId: drop.listId,
          position: drop.position,
          previousSiblingId: drop.previousSiblingId,
          nextSiblingId: drop.nextSiblingId,
        },
      });
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
  const columnFields = groupableFields(kanban.filterFields).filter((field) =>
    kanban.groupByFields.some((candidate) => candidate.id === field.id),
  );
  const laneRef = view.applied.lane;
  const laneField: FilterField | undefined =
    laneRef && laneRef.fieldId !== kanban.groupByField?.id
      ? kanban.filterFields.find((field) => field.id === laneRef.fieldId)
      : undefined;
  const swimlanes: KanbanStatusLane[] = laneField
    ? groupCardsByField(
        kanban.lanes.flatMap((lane) => lane.cards),
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

  /** Writes the column and/or the lane value of a card; the two are separate writes. */
  const writeCell = (
    cardId: string,
    column: { set: boolean; value: string | null },
    lane: { set: boolean; value: unknown },
  ) => {
    noteOrigin(cardId);
    if (column.set) {
      if (column.value !== null) writeLaneValue(cardId, column.value);
      else if (!isStatusGrouping && kanban.groupByField)
        kanban.updateFieldValue.mutate({
          cardId,
          fieldDefinitionId: kanban.groupByField.id,
          value: null,
        });
    }
    if (lane.set && laneField)
      kanban.updateFieldValue.mutate({
        cardId,
        fieldDefinitionId: laneField.id,
        value: lane.value,
      });
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
    writeCell(
      card.id,
      { set: plan.columnChanged, value: toColumn },
      { set: plan.laneChanged, value: plan.lane },
    );
    const t = filtersCopy.lane.toast;
    const values = {
      title: card.title,
      column: columnLabel(toColumn),
      field: laneField.name,
      value: swimlaneLabel(swimlanes.find((lane) => lane.optionId === toLane)),
    };
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
            writeCell(
              card.id,
              { set: plan.columnChanged, value: previous.column },
              { set: plan.laneChanged, value: previous.lane },
            ),
        },
      },
    );
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
    kanban.createItemInLane.mutate({
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
      kanban.updateStatus.mutate({
        cardId: payload.cardId,
        statusOptionId: payload.value,
      });
      return;
    }
    noteOrigin(payload.cardId);
    kanban.updateFieldValue.mutate(payload);
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
    kanban.moveCard.mutate({
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

  /** The hover and focus actions of a card, each only when the viewer may do it. */
  const personField = board ? findItemFields(board).person : undefined;
  const cardActions: KanbanCardActions | undefined = capabilities.canEditFields
    ? {
        currentUserId,
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
        onCopyLink: handleCopyLink,
        onDuplicate: capabilities.canCreateCard
          ? removal.requestDuplicate
          : undefined,
        onArchive: capabilities.canDelete ? removal.requestArchive : undefined,
        onDelete: capabilities.canDelete ? removal.requestDelete : undefined,
        onMoveToBoard: capabilities.canMoveCard
          ? removal.requestMove
          : undefined,
      }
    : undefined;

  const selectedIndex = selectedItemId ? laneOrder.indexOf(selectedItemId) : -1;
  const previousId =
    selectedIndex > 0 ? laneOrder[selectedIndex - 1] : undefined;
  const nextId = selectedIndex >= 0 ? laneOrder[selectedIndex + 1] : undefined;
  const step = (id: string | undefined) =>
    id ? () => onSelectedItemChange?.(id, { replace: true }) : undefined;

  const departmentMark: ItemSheetMark | undefined = header?.space
    ? { initials: header.space.initials, tone: header.space.markTone }
    : undefined;
  const linkCandidates: ItemSheetLinkCandidate[] = kanban.groups
    .flatMap((group) => group.cards)
    .filter((entry) => entry.id !== selectedItemId && !entry.parentItemId)
    .map((entry) => ({
      id: entry.id,
      key: entry.itemKey ?? null,
      title: entry.title,
      statusCategory: entry.statusCategory ?? null,
    }));
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
      <div
        className="px-4 sm:px-6"
        inert={boardCovered || undefined}
        aria-hidden={boardCovered || undefined}
      >
        <KanbanToolbar
          copy={toolbarCopy}
          searchQuery={kanban.search.query}
          onSearchChange={kanban.search.setQuery}
          onClearFilter={() =>
            view.setFilter({ ...view.state.filter, conditions: [] })
          }
          viewBar={
            <ViewStateBar
              canSave={capabilities.canEditFields}
              copy={filtersCopy}
              view={view}
              fields={kanban.filterFields}
              memberName={kanban.memberName}
              people={kanban.filterChoices.assignees}
              counts={{
                shown: kanban.visibleCount,
                total: kanban.totalCount,
              }}
              now={now}
              groupFields={columnFields}
              groupFallback={
                kanban.groupByField
                  ? { fieldId: kanban.groupByField.id }
                  : undefined
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
          noResults={
            kanban.filterActive && kanban.visibleCount === 0
              ? { hiddenCount: kanban.totalCount }
              : null
          }
        />
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
          className="h-full min-h-0 overflow-auto px-4 pt-1 pb-6 sm:px-6"
        >
          {laneField ? (
            <>
              {capabilities.canMoveCard ? null : (
                <p className="mb-2 text-compact text-muted-foreground">
                  {filtersCopy.lane.viewerNote}
                </p>
              )}
              <KanbanSwimlaneBoard
                copy={filtersCopy}
                cardCopy={cardCopy}
                now={now}
                columns={kanban.lanes}
                swimlanes={swimlanes}
                laneField={laneField}
                columnName={(column) =>
                  laneName(column, laneCopy, isStatusGrouping)
                }
                laneName={swimlaneLabel}
                wipLimits={kanbanSettings.wipLimits}
                selectedItemId={selectedItemId}
                canDrag={capabilities.canMoveCard}
                cardActions={cardActions}
                onOpenDetails={handleOpenDetail}
                onDrop={handleSwimlaneDrop}
                onRefused={handleSwimlaneRefused}
              />
            </>
          ) : (
            <KanbanBoard
              lanes={kanban.lanes}
              groupOrder={kanban.groups}
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
              laneWritePending={moveOption.isPending || removeOption.isPending}
              onCreateGroup={
                capabilities.canCreateCard
                  ? (draft) => addGroup(draft)
                  : undefined
              }
              groupTitles={kanban.groups.map((group) => group.title)}
              groupPending={addingGroup}
              laneCardLimit={kanbanSettings.laneCardLimit}
              keyboardMove={kanbanSettings.keyboardMove}
              cardActions={cardActions}
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
                  side === "right" ? laneCopy.scrollRight : laneCopy.scrollLeft
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

      {board && capabilities.canCreateCard ? (
        <ItemCreateHost
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
          hideGroup={isStatusGrouping}
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
          focusDescription={create?.kind === "quick"}
          context={
            create && create.kind !== "toolbar"
              ? {
                  kind: create.kind,
                  name:
                    create.lane.name ||
                    (isStatusGrouping ? laneCopy.noStatus : laneCopy.noValue),
                  ...laneDot(create.lane),
                }
              : undefined
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
          cardId={selectedItemId}
          capabilities={capabilities}
          departmentMark={departmentMark}
          groups={sheetGroups}
          linkCandidates={linkCandidates}
          onClose={handleClosePanel}
          onRestoreFocus={restoreFocus}
          onPrevious={step(previousId)}
          onNext={step(nextId)}
          onCopyLink={handleCopyLink}
          onOpenFullPage={onOpenItemPage}
          onMoveToGroup={handleMoveToGroup}
          onUpdateFieldValue={handleSheetFieldValue}
          onDuplicate={removal.requestDuplicate}
        />
      ) : null}
      {removal.dialogs}
    </div>
  );
}

export interface SelectedItemProps {
  board: Board;
  cardId: string;
  capabilities: TaskDetailCapabilities;
  departmentMark?: ItemSheetMark;
  groups: readonly ItemSheetGroup[];
  /** Items a new link can point at (the board's items that are not sub-items). */
  linkCandidates?: readonly ItemSheetLinkCandidate[];
  onClose: () => void;
  onRestoreFocus: (cardId: string) => void;
  onPrevious?: () => void;
  onNext?: () => void;
  onCopyLink: (cardId: string) => void;
  onOpenFullPage?: (cardId: string) => void;
  onMoveToGroup: (cardId: string, groupId: string) => void;
  onUpdateFieldValue: (payload: UpdateFieldValueInput) => void;
  onDuplicate: (cardId: string) => void;
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
  linkCandidates,
  onClose,
  onCloseAutoFocus,
  onPrevious,
  onNext,
  onCopyLink,
  onOpenFullPage,
  onMoveToGroup,
  onUpdateFieldValue,
}: Omit<SelectedItemProps, "cardId" | "onRestoreFocus"> & {
  card: CardDetail;
  onCloseAutoFocus: (event: Event) => void;
}) {
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
  const itemLinks = useItemLinks(card.id, card.workspaceId, card.boardId);
  const approvals = useItemApprovals(card.id, card.workspaceId);
  const extras = useItemSheetExtras(card);
  const { directory } = usePeopleDirectory(card.boardId, card.workspaceId);
  const viewerUserId = useBoardViewerId();
  const linksCopy = useItemPageCopy();
  const removal = useItemRemoval({
    workspaceId: card.workspaceId,
    boardId: card.boardId,
    titleOf: () => card.title,
    onGone: () => onClose(),
  });

  return (
    <>
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
          activityError: activity.isError && activity.data === undefined,
          updates: updates.data ?? card.updates,
          updatesLoading: updates.isLoading,
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
          })),
          approvalAnswerError: approvals.answerError !== null,
        }}
        callbacks={{
          onClose,
          onAddLink: (itemId, relation) =>
            itemLinks.addLink({ itemId, relation }),
          onRemoveLink: (linkId) => itemLinks.removeLink(linkId),
          onDecideApproval: (requestId, decision, comment) =>
            approvals.decide({ requestId, decision, comment }),
          onCancelApproval: (requestId) => approvals.cancel(requestId),
          onRenameTitle: (cardId, patch) =>
            updateCard.mutate({ cardId, patch }),
          onUpdateItem: (cardId, patch) => updateCard.mutate({ cardId, patch }),
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
          onCreateUpdate: (input, options) =>
            createUpdate.mutate(input, {
              onSuccess: options?.onSuccess,
              onSettled: options?.onSettled,
            }),
          onToggleReaction: (commentId, emoji, on) =>
            toggleReaction.mutate({ commentId, emoji, on }),
          onUpdateUpdate: (updateId, body) =>
            updateUpdate.mutate({ updateId, body }),
          onDeleteUpdate: (updateId) => deleteUpdate.mutate(updateId),
          onSelectTab: () => undefined,
          onPrevious,
          onNext,
          onCopyLink,
          onOpenFullPage,
          onMoveToGroup,
          onDelete: removal.requestDelete,
          onMoveToBoard: removal.requestMove,
        }}
      />
      {removal.dialogs}
    </>
  );
}
