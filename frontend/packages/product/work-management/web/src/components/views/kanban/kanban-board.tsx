import type { KanbanCardActions } from "./kanban-card-quick-actions";
import type { KanbanCardSelection } from "./kanban-selection";
import { useHoldRealtimeRefetch } from "@notrelix/work-management-state";
import { useEffect, useMemo, useRef, useState } from "react";
import {
  DndContext,
  DragOverlay,
  MeasuringStrategy,
  MouseSensor,
  TouchSensor,
  useSensor,
  useSensors,
  type DragEndEvent,
  type DragOverEvent,
  type DragStartEvent,
  type DropAnimation,
} from "@dnd-kit/core";
import {
  DEFAULT_KANBAN_LANE_CARD_LIMIT,
  type Card,
  type KanbanStatusLane,
} from "@notrelix/work-management-core";
import type { StatusOptionDraft } from "../../status-option/status-option-form";
import type { StatusOptionFormCopy } from "../../status-option/status-option-form.copy";
import {
  AddGroupPopover,
  type AddGroupDraft,
} from "../../status-option/add-group-popover";
import { cn } from "@notrelix/ui-web";
import { kanbanColumnWidthClass } from "./kanban-layout";
import { KanbanAddLane } from "./kanban-add-lane";
import { fillCopy, pluralCopy, type KanbanCardCopy } from "./kanban-card.copy";
import { KanbanCardOverlay } from "./kanban-card";
import { prefersReducedMotion } from "./kanban-chrome";
import {
  KANBAN_CARD_TYPE,
  KANBAN_LANE_TYPE,
  kanbanCollisionDetection,
} from "./kanban-collision";
import { KanbanColumn } from "./kanban-column";
import {
  arrangeKanbanLanes,
  getKanbanDrop,
  kanbanLaneKey,
  laneKeyOfCard,
  moveCardByKey,
  SILENT_DND_ANNOUNCEMENTS,
  moveCardInArrangement,
  type KanbanArrangement,
  type KanbanMoveDirection,
  type KanbanGroupOrder,
  type KanbanLaneDrop,
} from "./kanban-dnd";
import { KanbanEmptyState } from "./kanban-empty-state";
import type { KanbanLaneCopy } from "./kanban-lane.copy";

export interface KanbanBoardProps {
  /** Lanes in option order, filter applied (`total` is the lane total). */
  lanes: readonly KanbanStatusLane[];
  /**
   * The board groups in board order (all cards): a reorder names its new
   * neighbours in the group's whole order, so the server keeps the order.
   */
  groupOrder?: readonly KanbanGroupOrder[];
  copy: KanbanLaneCopy;
  cardCopy?: KanbanCardCopy;
  /** Reference time for card due wording; read once by the container. */
  now?: Date;
  timeZone?: string;
  selectedItemId?: string;
  /** A filter or search narrows the board. */
  filterActive: boolean;
  /** "Thêm vào {lane}" is offered. */
  canAdd: boolean;
  /** Cards can be dragged between and within lanes (pointer and touch). */
  canDrag: boolean;
  /**
   * The grouping field's kind: names the lane without a value ("Không có
   * trạng thái" for Status, "Chưa chọn" for another Select field).
   */
  groupKind?: "status" | "select";
  /** Changing this number opens the add field of the first open lane. */
  addRequest?: number;
  onOpenDetails: (cardId: string) => void;
  /** A card was dropped: another lane's value, a place in a lane, or a reorder. */
  onDropCard: (drop: KanbanLaneDrop) => void;
  onCreateItem: (
    lane: KanbanStatusLane,
    title: string,
  ) => void | Promise<unknown>;
  /**
   * The status form's copy. With it (and the callbacks below) the viewer can
   * add a lane after the last one and rename or recolour a lane.
   */
  formCopy?: StatusOptionFormCopy;
  /** Add a lane: a new option of the grouping field. */
  onCreateLane?: (draft: StatusOptionDraft) => void;
  /** Rename or recolour a lane's option. */
  onUpdateLane?: (lane: KanbanStatusLane, draft: StatusOptionDraft) => void;
  /** A lane create is in flight. */
  lanePending?: boolean;
  /** Move a lane's option one place before or after its neighbour. */
  onMoveLane?: (lane: KanbanStatusLane, direction: "before" | "after") => void;
  /** Delete a lane's option; its items move to the replacement (null: lose the value). */
  onDeleteLane?: (
    lane: KanbanStatusLane,
    replacementOptionId: string | null,
  ) => void;
  /** A lane move or delete is in flight. */
  laneWritePending?: boolean;
  /**
   * "Add group": creates a group of the board (not a status). With it the slot
   * after the last lane offers it under "Add status".
   */
  onCreateGroup?: (draft: AddGroupDraft) => Promise<void> | void;
  /** Titles the board's groups carry (a new group may not repeat them). */
  groupTitles?: readonly string[];
  /** A group create is in flight. */
  groupPending?: boolean;
  /** Cards a lane shows before "Xem thêm N việc" (the view's `laneCardLimit`). */
  laneCardLimit?: number;
  /**
   * Alt+Arrow moves the focused card (the view's `keyboardMove`); it also
   * needs `canDrag`. Off: those keys do nothing.
   */
  keyboardMove?: boolean;
  /** Hover and focus actions of the cards; absent: none. */
  cardActions?: KanbanCardActions;
  /** Picking cards for the bulk bar. */
  cardSelection?: KanbanCardSelection;
  /** Lane value id to its WIP limit (a warning number); absent: no limits. */
  wipLimits?: Readonly<Record<string, number>>;
  /** Set or (null) remove a lane's limit; absent: the viewer cannot. */
  onWipLimitChange?: (lane: KanbanStatusLane, limit: number | null) => void;
  /** The header "+" and the empty lane's button: the full form for a lane. */
  onRequestCreate?: (lane: KanbanStatusLane) => void;
  /** Quick add handed over to the full form with the typed title. */
  onExpandCreate?: (lane: KanbanStatusLane, title: string) => void;
  /** A board read in pages: reads a lane's next page; `loading` and `failed` name lanes by key (`none` for no value). */
  lanePaging?: {
    readonly loadMore: (lane: KanbanStatusLane) => void;
    readonly loading: ReadonlySet<string>;
    readonly failed: ReadonlySet<string>;
  };
  /** The lanes collapsed when the board opens (the member's own preference). */
  initialCollapsed?: ReadonlySet<string>;
  /** The collapsed lanes changed: the container remembers them per member and view. */
  onCollapsedChange?: (collapsed: ReadonlySet<string>) => void;
}

function laneKey(lane: KanbanStatusLane): string {
  return kanbanLaneKey(lane.optionId);
}

interface DragState {
  readonly cardId: string;
  readonly arrangement: KanbanArrangement;
}

/** The overlay settles into the slot it is dropped in; reduced motion skips it. */
function dropAnimation(): DropAnimation | null {
  return prefersReducedMotion()
    ? null
    : { duration: 200, easing: "cubic-bezier(0.2, 0, 0, 1)" };
}

/**
 * The Kanban lanes, side by side. Collapsing and hiding a lane are view
 * conveniences kept on this screen only; "Hiện N cột đã ẩn" brings hidden
 * lanes back. A card is dragged by its whole body (mouse: after a few pixels,
 * touch: after a long press): a copy of it follows the pointer while the card
 * stays in the lane it would be dropped in as a gap of its own height, the
 * lane under the pointer is outlined and the other cards make room. Escape
 * puts everything back. The keyboard alternatives are the item sheet's status
 * pill and, when the view allows it, Alt+Arrow on a focused card: it goes
 * through the same drop as the pointer, is announced in a live region and
 * keeps focus on the card.
 */
export function KanbanBoard({
  lanes,
  groupOrder,
  copy,
  cardCopy,
  now,
  timeZone,
  selectedItemId,
  filterActive,
  canAdd,
  canDrag,
  groupKind = "status",
  addRequest,
  onOpenDetails,
  onDropCard,
  onCreateItem,
  formCopy,
  onCreateLane,
  onUpdateLane,
  lanePending = false,
  onMoveLane,
  onDeleteLane,
  laneWritePending = false,
  onCreateGroup,
  groupTitles = [],
  groupPending = false,
  laneCardLimit = DEFAULT_KANBAN_LANE_CARD_LIMIT,
  keyboardMove = true,
  cardActions,
  cardSelection,
  wipLimits,
  onWipLimitChange,
  onRequestCreate,
  onExpandCreate,
  lanePaging,
  initialCollapsed,
  onCollapsedChange,
}: KanbanBoardProps) {
  const [collapsed, setCollapsed] = useState<ReadonlySet<string>>(
    () => initialCollapsed ?? new Set(),
  );
  const [hidden, setHidden] = useState<ReadonlySet<string>>(new Set());
  const [drag, setDrag] = useState<DragState | null>(null);
  useHoldRealtimeRefetch(drag !== null);
  // The arrangement of a card just dropped, kept on screen until the lanes
  // as stored show it too: the write lands in the cache a moment after the
  // drop, and without this the card would sit back in its old place until then.
  const [settling, setSettling] = useState<DragState | null>(null);
  const sensors = useSensors(
    useSensor(MouseSensor, { activationConstraint: { distance: 5 } }),
    useSensor(TouchSensor, {
      activationConstraint: { delay: 220, tolerance: 8 },
    }),
  );
  const lanesRef = useRef<HTMLDivElement | null>(null);
  // The card Alt+Arrow just moved: shown even past the lane limit and given
  // focus again once it is in its new lane (it is a new element there).
  const [movedId, setMovedId] = useState<string | null>(null);
  const refocusId = useRef<string | null>(null);
  const [announcement, setAnnouncement] = useState("");

  const cardsById = useMemo(() => {
    const map = new Map<string, Card>();
    for (const lane of lanes) {
      for (const card of lane.cards) map.set(card.id, card);
    }
    return map;
  }, [lanes]);

  // While a card is dragged the lanes show the arrangement the drag made, not
  // the lanes as stored.
  const shownLanes = useMemo(() => {
    const shown = drag ?? settling;
    if (!shown) return lanes;
    return lanes.map((lane) => {
      const ids = shown.arrangement.get(laneKey(lane)) ?? [];
      const cards = ids
        .map((id) => cardsById.get(id))
        .filter((card): card is Card => card !== undefined);
      return { ...lane, cards };
    });
  }, [lanes, drag, settling, cardsById]);

  function handleDragStart(event: DragStartEvent) {
    if (event.active.data.current?.type !== KANBAN_CARD_TYPE) return;
    setDrag({
      cardId: String(event.active.id),
      arrangement: arrangeKanbanLanes(lanes),
    });
  }

  /**
   * The lane key and the index at which the dragged card goes, over `over`
   * (an index into the lane without the dragged card). Inside its own lane
   * the card takes the place of the card it is over, like the sortable's own
   * animation shows; in another lane it goes before or after the card it is
   * over, by which half of it the dragged card is on. `null`: no change.
   */
  function placeOver(
    arrangement: KanbanArrangement,
    event: DragOverEvent | DragEndEvent,
  ): { key: string; index: number } | null {
    const { active, over } = event;
    if (!over || over.id === active.id) return null;
    const activeId = String(active.id);
    const activeKey = laneKeyOfCard(arrangement, activeId);
    if (over.data.current?.type === KANBAN_LANE_TYPE) {
      const key = over.data.current?.laneKey as string;
      if (key === activeKey) return null;
      const ids = (arrangement.get(key) ?? []).filter((id) => id !== activeId);
      return { key, index: ids.length };
    }
    const key = laneKeyOfCard(arrangement, String(over.id));
    if (key === undefined) return null;
    const overIndex = (arrangement.get(key) ?? []).indexOf(String(over.id));
    if (key === activeKey) return { key, index: overIndex };
    const translated = active.rect.current.translated;
    const below =
      translated !== null &&
      translated.top + translated.height / 2 >
        over.rect.top + over.rect.height / 2;
    return { key, index: overIndex + (below ? 1 : 0) };
  }

  function handleDragOver(event: DragOverEvent) {
    setDrag((current) => {
      if (!current) return current;
      const at = placeOver(current.arrangement, event);
      if (!at) return current;
      const key = laneKeyOfCard(current.arrangement, current.cardId);
      // A move inside the lane is the sortable's own animation; the board
      // only re-arranges when the card changes lane.
      if (key === at.key) return current;
      const arrangement = moveCardInArrangement(
        current.arrangement,
        current.cardId,
        at.key,
        at.index,
      );
      return arrangement === current.arrangement
        ? current
        : { ...current, arrangement };
    });
  }

  function handleDragEnd(event: DragEndEvent) {
    const current = drag;
    setDrag(null);
    if (!current) return;
    let arrangement = current.arrangement;
    const at = placeOver(arrangement, event);
    if (at) {
      arrangement = moveCardInArrangement(
        arrangement,
        current.cardId,
        at.key,
        at.index,
      );
    }
    const drop = getKanbanDrop({
      lanes,
      arrangement,
      cardId: current.cardId,
      groups: groupOrder,
    });
    if (drop) {
      setSettling({ ...current, arrangement });
      onDropCard(drop);
    }
  }

  function handleMoveByKey(cardId: string, direction: KanbanMoveDirection) {
    if (!canDrag || !keyboardMove || drag) return;
    const keys = shownLanes
      .filter(
        (lane) => !hidden.has(laneKey(lane)) && !collapsed.has(laneKey(lane)),
      )
      .map(laneKey);
    const base = settling?.arrangement ?? arrangeKanbanLanes(lanes);
    const moved = moveCardByKey(base, keys, cardId, direction);
    if (!moved) return;
    // The same read of a finished move as a pointer drop.
    const drop = getKanbanDrop({
      lanes,
      arrangement: moved.arrangement,
      cardId,
      groups: groupOrder,
    });
    if (!drop) return;
    const target = lanes.find((lane) => laneKey(lane) === moved.laneKey);
    const laneLabel =
      target?.name || (groupKind === "status" ? copy.noStatus : copy.noValue);
    const title = cardsById.get(cardId)?.title ?? "";
    setAnnouncement(
      direction === "left" || direction === "right"
        ? fillCopy(copy.keyboardMoved, { title, lane: laneLabel })
        : fillCopy(copy.keyboardReordered, {
            title,
            lane: laneLabel,
            position: moved.index + 1,
            total: moved.arrangement.get(moved.laneKey)?.length ?? 0,
          }),
    );
    refocusId.current = cardId;
    setMovedId(cardId);
    setSettling({ cardId, arrangement: moved.arrangement });
    onDropCard(drop);
  }

  const toggle = (setter: typeof setCollapsed) => (key: string, on: boolean) =>
    setter((current) => {
      const next = new Set(current);
      if (on) next.add(key);
      else next.delete(key);
      return next;
    });
  const setLaneCollapsed = (key: string, on: boolean) => {
    const next = new Set(collapsed);
    if (on) next.add(key);
    else next.delete(key);
    setCollapsed(next);
    onCollapsedChange?.(next);
  };
  const setLaneHidden = toggle(setHidden);

  // A lane that appears while its popover closes (a new status) keeps nothing
  // of the drag; a drag that outlives its card (a refetch removed it) ends.
  useEffect(() => {
    if (drag && !cardsById.has(drag.cardId)) setDrag(null);
  }, [drag, cardsById]);
  // The stored lanes now show the dropped card where the drag put it (or the
  // write was refused and put it back, or never arrived): stop holding it.
  useEffect(() => {
    if (!settling) return;
    const stored = arrangeKanbanLanes(lanes);
    const key = laneKeyOfCard(settling.arrangement, settling.cardId);
    const same = (
      a: readonly string[] | undefined,
      b: readonly string[] | undefined,
    ) =>
      a !== undefined &&
      b !== undefined &&
      a.length === b.length &&
      a.every((id, i) => id === b[i]);
    const landed =
      key !== undefined && same(stored.get(key), settling.arrangement.get(key));
    if (landed) {
      setSettling(null);
      return;
    }
    const timer = window.setTimeout(() => setSettling(null), 1500);
    return () => window.clearTimeout(timer);
  }, [settling, lanes]);
  useEffect(() => {
    const cardId = refocusId.current;
    if (!cardId) return;
    const target = Array.from(
      lanesRef.current?.querySelectorAll<HTMLElement>("[data-card-id]") ?? [],
    ).find((element) => element.dataset.cardId === cardId);
    if (!target) return;
    refocusId.current = null;
    if (document.activeElement !== target) target.focus();
  });
  const overlayCard = drag ? cardsById.get(drag.cardId) : undefined;
  const currentLaneKey = drag
    ? laneKeyOfCard(drag.arrangement, drag.cardId)
    : undefined;

  if (lanes.length === 0) {
    return <KanbanEmptyState message={copy.empty} />;
  }

  const noValueName = groupKind === "status" ? copy.noStatus : copy.noValue;
  const shown = shownLanes.filter((lane) => !hidden.has(laneKey(lane)));
  const hiddenCount = lanes.length - shown.length;
  const addLane = shown.find((lane) => !collapsed.has(laneKey(lane)));
  const laneNames = lanes
    .filter((lane) => lane.optionId !== null)
    .map((lane) => lane.name);
  const canManageLanes = formCopy !== undefined && onCreateLane !== undefined;
  const subject = groupKind === "status" ? "status" : "value";
  // The lanes that are options of the field, in the field's order.
  const optionLanes = lanes.filter((lane) => lane.optionId !== null);
  const blockedReasonFor = (lane: KanbanStatusLane): string | undefined => {
    if (!formCopy) return undefined;
    if (subject === "value") {
      return optionLanes.length <= 1 ? formCopy.removeBlockedValue : undefined;
    }
    const required = lane.category === "todo" || lane.category === "done";
    const other = optionLanes.some(
      (candidate) =>
        candidate.optionId !== lane.optionId &&
        candidate.category === lane.category,
    );
    return required && !other ? formCopy.removeBlockedStatus : undefined;
  };

  return (
    <DndContext
      sensors={sensors}
      collisionDetection={kanbanCollisionDetection}
      measuring={{ droppable: { strategy: MeasuringStrategy.Always } }}
      autoScroll={{
        threshold: { x: 0.12, y: 0.18 },
        acceleration: 12,
        interval: 5,
      }}
      accessibility={{ announcements: SILENT_DND_ANNOUNCEMENTS }}
      onDragStart={handleDragStart}
      onDragOver={handleDragOver}
      onDragEnd={handleDragEnd}
      onDragCancel={() => setDrag(null)}
    >
      <div
        ref={lanesRef}
        onFocus={(event) => {
          // Focus left the moved card: it no longer needs to stay shown.
          if (movedId && event.target.getAttribute("data-card-id") !== movedId)
            setMovedId(null);
        }}
        data-kanban-lanes=""
        data-dragging={drag ? "" : undefined}
        className="flex min-w-min items-start gap-3"
      >
        {shown.map((lane) => {
          const key = laneKey(lane);
          const original = lanes.find(
            (candidate) => laneKey(candidate) === key,
          );
          return (
            <KanbanColumn
              key={key}
              lane={lane}
              name={lane.name || noValueName}
              copy={copy}
              cardCopy={cardCopy}
              now={now}
              timeZone={timeZone}
              selectedItemId={selectedItemId}
              filterActive={filterActive}
              canAdd={canAdd}
              canDrag={canDrag}
              dropTarget={drag !== null && currentLaneKey === key}
              laneCardLimit={laneCardLimit}
              onLoadMore={
                lanePaging ? () => lanePaging.loadMore(lane) : undefined
              }
              loadingMore={lanePaging?.loading.has(key) ?? false}
              loadFailed={lanePaging?.failed.has(key) ?? false}
              draggedCardId={drag?.cardId ?? movedId ?? undefined}
              cardActions={cardActions}
              cardSelection={cardSelection}
              onMoveCardByKey={
                canDrag && keyboardMove ? handleMoveByKey : undefined
              }
              edit={
                canManageLanes && onUpdateLane && original
                  ? {
                      formCopy,
                      subject,
                      otherNames: laneNames.filter(
                        (laneName) => laneName !== lane.name,
                      ),
                      onSubmit: (draft) => onUpdateLane(original, draft),
                      move: onMoveLane
                        ? {
                            canMoveLeft:
                              optionLanes.findIndex(
                                (candidate) =>
                                  candidate.optionId === lane.optionId,
                              ) > 0,
                            canMoveRight:
                              optionLanes.findIndex(
                                (candidate) =>
                                  candidate.optionId === lane.optionId,
                              ) <
                              optionLanes.length - 1,
                            pending: laneWritePending,
                            onMove: (direction) =>
                              onMoveLane(original, direction),
                          }
                        : undefined,
                      remove: onDeleteLane
                        ? {
                            blockedReason: blockedReasonFor(original),
                            choices: optionLanes
                              .filter(
                                (candidate) =>
                                  candidate.optionId !== lane.optionId,
                              )
                              .map((candidate) => ({
                                id: candidate.optionId!,
                                name: candidate.name,
                                color: candidate.color,
                              })),
                            pending: laneWritePending,
                            onConfirm: (replacementId) =>
                              onDeleteLane(original, replacementId),
                          }
                        : undefined,
                    }
                  : undefined
              }
              wipLimit={lane.optionId ? wipLimits?.[lane.optionId] : undefined}
              onWipLimitChange={
                onWipLimitChange && lane.optionId
                  ? (limit) => onWipLimitChange(original ?? lane, limit)
                  : undefined
              }
              onRequestCreate={
                onRequestCreate
                  ? () => onRequestCreate(original ?? lane)
                  : undefined
              }
              onExpandCreate={
                onExpandCreate
                  ? (title) => onExpandCreate(original ?? lane, title)
                  : undefined
              }
              collapsed={collapsed.has(key)}
              openAddRequest={lane === addLane ? addRequest : undefined}
              onCollapsedChange={(on) => setLaneCollapsed(key, on)}
              onHide={() => setLaneHidden(key, true)}
              onOpenDetails={onOpenDetails}
              onCreateItem={(title) => onCreateItem(original ?? lane, title)}
            />
          );
        })}
        {canManageLanes ? (
          onCreateGroup ? (
            <div className="flex shrink-0 flex-col gap-2">
              <KanbanAddLane
                copy={copy}
                formCopy={formCopy}
                subject={subject}
                existingNames={laneNames}
                pending={lanePending}
                onCreate={onCreateLane}
              />
              <AddGroupPopover
                copy={formCopy}
                label={copy.addGroup}
                existingTitles={groupTitles}
                pending={groupPending}
                onSubmit={onCreateGroup}
                triggerClassName={cn(
                  "flex h-9 shrink-0 cursor-pointer items-center gap-1.5 rounded-xl border-0 bg-transparent px-4 text-compact font-medium text-muted-foreground transition-colors hover:bg-state-hover hover:text-foreground aria-expanded:bg-state-hover aria-expanded:text-foreground",
                  kanbanColumnWidthClass,
                )}
              />
            </div>
          ) : (
            <KanbanAddLane
              copy={copy}
              formCopy={formCopy}
              subject={subject}
              existingNames={laneNames}
              pending={lanePending}
              onCreate={onCreateLane}
            />
          )
        ) : null}
        {hiddenCount > 0 ? (
          <button
            type="button"
            onClick={() => setHidden(new Set())}
            className="h-8 shrink-0 rounded-lg border border-dashed border-border px-3 text-metadata font-medium text-muted-foreground hover:bg-state-hover hover:text-foreground focus-visible:outline-hidden focus-visible:ring-2 focus-visible:ring-focus-ring"
          >
            {pluralCopy(
              { one: copy.showHidden_one, other: copy.showHidden_other },
              hiddenCount,
            )}
          </button>
        ) : null}
      </div>
      <div
        role="status"
        aria-live="polite"
        data-kanban-announcer=""
        className="sr-only"
      >
        {announcement}
      </div>
      <DragOverlay dropAnimation={dropAnimation()} zIndex={1500}>
        {overlayCard ? (
          <KanbanCardOverlay
            card={overlayCard}
            now={now}
            timeZone={timeZone}
            copy={cardCopy}
          />
        ) : null}
      </DragOverlay>
    </DndContext>
  );
}
