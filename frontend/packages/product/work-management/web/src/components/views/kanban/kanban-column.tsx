import { useEffect, useRef, useState } from "react";
import { useDroppable } from "@dnd-kit/core";
import {
  SortableContext,
  verticalListSortingStrategy,
} from "@dnd-kit/sortable";
import {
  ArrowLeft,
  ArrowRight,
  ChevronLeft,
  Inbox,
  MoreHorizontal,
  Plus,
  TriangleAlert,
} from "lucide-react";
import {
  Button,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
  Input,
  Popover,
  PopoverAnchor,
  PopoverContent,
  cn,
} from "@notrelix/ui-web";
import {
  DEFAULT_KANBAN_LANE_CARD_LIMIT,
  MAX_KANBAN_WIP_LIMIT,
  MIN_KANBAN_WIP_LIMIT,
  isOptimisticOptionId,
  type KanbanStatusLane,
  type WorkStatusCategory,
} from "@notrelix/work-management-core";
import {
  StatusOptionForm,
  type StatusOptionDraft,
} from "../../status-option/status-option-form";
import {
  RemoveOptionDialog,
  type RemoveOptionChoice,
} from "../../status-option/remove-option-dialog";
import type { StatusOptionFormCopy } from "../../status-option/status-option-form.copy";
import { KanbanAddCard } from "./kanban-add-card";
import { KanbanCard } from "./kanban-card";
import type { KanbanCardActions } from "./kanban-card-quick-actions";
import type { KanbanCardSelection } from "./kanban-selection";
import { fillCopy, pluralCopy, type KanbanCardCopy } from "./kanban-card.copy";
import { KANBAN_LANE_TYPE } from "./kanban-collision";
import {
  kanbanLaneDropId,
  kanbanLaneKey,
  type KanbanMoveDirection,
} from "./kanban-dnd";
import { kanbanColumnWidthClass } from "./kanban-layout";
import type { KanbanLaneCopy } from "./kanban-lane.copy";

/** Lane tint by status category; lanes without a category use the neutral tint. */
const LANE_TINT_CLASS: Readonly<Record<WorkStatusCategory, string>> = {
  todo: "bg-work-status-todo-lane",
  in_progress: "bg-work-status-doing-lane",
  review: "bg-work-status-review-lane",
  done: "bg-work-status-done-lane",
};

/** Lane header dot: an outline for "to do", filled for the others. */
const LANE_DOT_CLASS: Readonly<Record<WorkStatusCategory, string>> = {
  todo: "border-2 border-work-status-todo",
  in_progress: "bg-work-status-doing",
  review: "bg-work-status-review",
  done: "bg-work-status-done",
};

/** What the lane menu needs to rename or recolour the lane's option. */
export interface KanbanLaneEdit {
  readonly formCopy: StatusOptionFormCopy;
  /** Status lanes are statuses (with a group), other Select lanes are values. */
  readonly subject: "status" | "value";
  /** The other lanes' names (this lane's own name is not a duplicate). */
  readonly otherNames: readonly string[];
  readonly onSubmit: (draft: StatusOptionDraft) => void;
  /** One place left or right in the field's order; absent = the viewer cannot reorder. */
  readonly move?: {
    readonly canMoveLeft: boolean;
    readonly canMoveRight: boolean;
    /** A move is in flight: the entries wait so a stale version is never sent. */
    readonly pending: boolean;
    readonly onMove: (direction: "before" | "after") => void;
  };
  /** Delete with a replacement choice; absent = the viewer cannot delete. */
  readonly remove?: {
    /** Why the lane cannot be deleted (the last Not started / Done status, the last value); absent = it can. */
    readonly blockedReason?: string;
    /** The other options the items can move to. */
    readonly choices: readonly RemoveOptionChoice[];
    readonly pending: boolean;
    readonly onConfirm: (replacementOptionId: string | null) => void;
  };
}

export interface KanbanColumnProps {
  lane: KanbanStatusLane;
  /** The lane's display name (the view names the lane without a value). */
  name: string;
  copy: KanbanLaneCopy;
  cardCopy?: KanbanCardCopy;
  /** Reference time for card due wording; read once by the container. */
  now?: Date;
  timeZone?: string;
  /** The card whose item sheet is open. */
  selectedItemId?: string;
  /** A filter or search narrows the board: empty lanes say so. */
  filterActive: boolean;
  canAdd: boolean;
  canDrag: boolean;
  /** The dragged card is over this lane: it is where the card would land. */
  dropTarget?: boolean;
  /**
   * Cards the lane shows before "Xem thêm N việc" (the view's
   * `laneCardLimit`); the lane's own default is the stored default.
   */
  laneCardLimit?: number;
  /** The dragged (or just moved) card: it is always shown (the lane never hides it). */
  draggedCardId?: string;
  /** Alt+Arrow moves a card; absent when the view has it off or the viewer cannot move. */
  onMoveCardByKey?: (cardId: string, direction: KanbanMoveDirection) => void;
  /** Hover and focus actions of the cards. */
  cardActions?: KanbanCardActions;
  /** Picking cards for the bulk bar. */
  cardSelection?: KanbanCardSelection;
  /** Rename and recolour from the lane menu; absent when the viewer cannot. */
  edit?: KanbanLaneEdit;
  collapsed: boolean;
  /** Changing this number opens the lane's add field. */
  openAddRequest?: number;
  /** The lane's work-in-progress limit: a warning number, never a write rule. */
  wipLimit?: number;
  /** Set (or with null, remove) the lane's WIP limit; absent: the viewer cannot. */
  onWipLimitChange?: (limit: number | null) => void;
  onCollapsedChange: (collapsed: boolean) => void;
  onHide: () => void;
  onOpenDetails: (cardId: string) => void;
  onCreateItem: (title: string) => void | Promise<unknown>;
  /** Opens the full form for this lane (the header "+" and the empty lane's button). */
  onRequestCreate?: () => void;
  /** Quick add handed over to the full form with the typed title. */
  onExpandCreate?: (title: string) => void;
  /** A board read in pages: reads the lane's next page from the server. */
  onLoadMore?: () => void;
  /** The lane's next page is being read. */
  loadingMore?: boolean;
  /** The lane's next page did not come; the cards already loaded stay. */
  loadFailed?: boolean;
}

/** Cards each "Hiện thêm" of a lane read in pages adds (the first window is the lane's card limit). */
export const KANBAN_LANE_PAGE = 50;
/** Cards of one lane drawn at once; the others stand in as spacers of their estimated height. */
export const KANBAN_LANE_DOM_CAP = 80;
const KANBAN_CARD_ESTIMATE = 96;

/**
 * One Kanban lane: the cards whose grouping value is the lane's option.
 * Header: dot, name, count (the lane's cards under the current filter) and
 * "Tuỳ chọn cột {lane}" (collapse or hide the lane on this view only). The
 * lane shows `laneCardLimit` cards (three by default; the Done lane one fewer), then "Xem thêm N việc"; the selected card is always
 * shown. The lane is the drop target for a card dragged from another lane.
 */
export function KanbanColumn({
  lane,
  name,
  copy,
  cardCopy,
  now,
  timeZone,
  selectedItemId,
  filterActive,
  canAdd,
  canDrag,
  dropTarget = false,
  laneCardLimit = DEFAULT_KANBAN_LANE_CARD_LIMIT,
  draggedCardId,
  onMoveCardByKey,
  cardActions,
  cardSelection,
  edit,
  collapsed,
  openAddRequest,
  wipLimit,
  onWipLimitChange,
  onCollapsedChange,
  onHide,
  onOpenDetails,
  onCreateItem,
  onRequestCreate,
  onExpandCreate,
  onLoadMore,
  loadingMore = false,
  loadFailed = false,
}: KanbanColumnProps) {
  const sectionRef = useRef<HTMLElement | null>(null);
  const [expanded, setExpanded] = useState(false);
  const [editing, setEditing] = useState(false);
  const [removing, setRemoving] = useState(false);
  const [settingWip, setSettingWip] = useState(false);
  const [wipText, setWipText] = useState("");
  const [wipBad, setWipBad] = useState(false);
  // "Sửa trạng thái" was chosen: the form opens once the menu has closed (a
  // form that opens while the menu still holds focus is dismissed by it), and
  // the menu does not give focus back to its button.
  const openingForm = useRef(false);
  const openingWip = useRef(false);
  const openingRemove = useRef(false);
  const focusFirstRevealed = useRef<string | null>(null);
  const laneKey = kanbanLaneKey(lane.optionId);
  const { setNodeRef } = useDroppable({
    id: kanbanLaneDropId(lane.optionId),
    data: { type: KANBAN_LANE_TYPE, lane, laneKey },
    // A lane that exists only locally has no option id the server knows yet.
    disabled: !canDrag || isOptimisticOptionId(lane.optionId),
  });
  const canEdit =
    edit !== undefined &&
    lane.optionId !== null &&
    !isOptimisticOptionId(lane.optionId);

  // The Done lane is the quietest: it shows one card fewer (Final-Board shows
  // three To do cards and two Done cards before "Xem thêm").
  const limit =
    lane.category === "done" ? Math.max(1, laneCardLimit - 1) : laneCardLimit;
  const loaded = lane.cards.length;
  // A lane read in pages counts the cards that pass the filter, loaded or not.
  const paged = lane.filteredTotal !== undefined;
  const count = lane.filteredTotal ?? loaded;
  const [pageShown, setPageShown] = useState(limit);
  // The window of drawn cards inside the shown ones: where it starts, and how tall a card is on average.
  const [top, setTop] = useState(0);
  const cardHeight = useRef(KANBAN_CARD_ESTIMATE);
  const cardsRef = useRef<HTMLDivElement | null>(null);
  const selectedIndex = selectedItemId
    ? lane.cards.findIndex((card) => card.id === selectedItemId)
    : -1;
  const draggedIndex = draggedCardId
    ? lane.cards.findIndex((card) => card.id === draggedCardId)
    : -1;
  const showAll =
    expanded ||
    loaded <= limit ||
    selectedIndex >= limit ||
    draggedIndex >= limit;
  // A paged lane draws a window of its loaded cards (the selected and the dragged card stay in it).
  const pagedWindow = Math.max(
    pageShown,
    Math.max(selectedIndex, draggedIndex) + 1,
  );
  const visibleCards = paged
    ? lane.cards.slice(0, pagedWindow)
    : showAll
      ? lane.cards
      : lane.cards.slice(0, limit);
  const hiddenCount = paged ? 0 : loaded - visibleCards.length;
  // A lane that shows more than the cap draws a window of it; a drag keeps the window where it is.
  const windowed = paged && visibleCards.length > KANBAN_LANE_DOM_CAP;
  const windowTop = windowed
    ? Math.min(top, visibleCards.length - KANBAN_LANE_DOM_CAP)
    : 0;
  const drawnCards = windowed
    ? visibleCards.slice(windowTop, windowTop + KANBAN_LANE_DOM_CAP)
    : visibleCards;
  const spaceAbove = windowTop * cardHeight.current;
  const spaceBelow = windowed
    ? (visibleCards.length - windowTop - drawnCards.length) * cardHeight.current
    : 0;
  const moveWindow = (element: HTMLDivElement) => {
    if (!windowed || draggedCardId) return;
    const drawn = element.querySelectorAll("[data-card-id]");
    if (drawn.length > 0) {
      const real = element.scrollHeight - spaceAbove - spaceBelow - 4; // paddings
      cardHeight.current = Math.max(40, real / drawn.length);
    }
    const first = Math.floor(element.scrollTop / cardHeight.current);
    const wanted = Math.max(
      0,
      Math.min(
        first - Math.floor(KANBAN_LANE_DOM_CAP / 4),
        visibleCards.length - KANBAN_LANE_DOM_CAP,
      ),
    );
    if (Math.abs(wanted - windowTop) >= KANBAN_LANE_DOM_CAP / 8) setTop(wanted);
  };
  const pagedMore =
    paged && (visibleCards.length < loaded || lane.hasMore === true);
  const tint = lane.category
    ? LANE_TINT_CLASS[lane.category]
    : LANE_TINT_CLASS.todo;
  // The dot is the colour the status was given; without one it falls back to its group's tone.
  const dotColor = lane.color;
  const dotStyle = dotColor
    ? lane.category === "todo"
      ? { borderColor: dotColor }
      : { backgroundColor: dotColor }
    : undefined;
  const dot = dotColor
    ? lane.category === "todo"
      ? "border-2"
      : ""
    : lane.category
      ? LANE_DOT_CLASS[lane.category]
      : "bg-muted-foreground";
  const countText = pluralCopy(
    { one: copy.count_one, other: copy.count_other },
    count,
  );

  // WIP: at the limit the pill turns amber, over it red; a drop that would go
  // over says so. It warns only, a card is never refused.
  const wipState =
    wipLimit === undefined
      ? null
      : count > wipLimit
        ? "over"
        : count === wipLimit
          ? "full"
          : null;
  const wipNote =
    wipLimit === undefined
      ? null
      : dropTarget && count > wipLimit
        ? fillCopy(copy.wipDrop, { limit: wipLimit })
        : wipState === "over"
          ? fillCopy(copy.wipOver, { count, limit: wipLimit })
          : wipState === "full"
            ? fillCopy(copy.wipFull, { limit: wipLimit })
            : null;
  const saveWip = () => {
    const text = wipText.trim();
    if (text === "") {
      onWipLimitChange?.(null);
      setSettingWip(false);
      return;
    }
    const value = Number(text);
    if (
      !Number.isInteger(value) ||
      value < MIN_KANBAN_WIP_LIMIT ||
      value > MAX_KANBAN_WIP_LIMIT
    ) {
      setWipBad(true);
      return;
    }
    onWipLimitChange?.(value);
    setSettingWip(false);
  };

  useEffect(() => {
    const cardId = focusFirstRevealed.current;
    if (!cardId || !(expanded || paged)) return;
    const target = Array.from(
      sectionRef.current?.querySelectorAll<HTMLElement>("[data-card-id]") ?? [],
    ).find((element) => element.dataset.cardId === cardId);
    if (!target) return; // a page still on its way: the focus waits for its cards
    focusFirstRevealed.current = null;
    target.focus();
  }, [expanded, paged, pageShown, loaded]);

  const setRefs = (element: HTMLElement | null) => {
    sectionRef.current = element;
    setNodeRef(element);
  };

  if (collapsed) {
    return (
      <section
        ref={setRefs}
        aria-label={name}
        data-lane-count={count}
        className={cn("flex w-11 shrink-0 flex-col rounded-xl p-1", tint)}
      >
        <button
          type="button"
          onClick={() => onCollapsedChange(false)}
          aria-label={fillCopy(copy.expand, { lane: name })}
          className="flex flex-col items-center gap-2 rounded-lg py-2 text-nav font-semibold text-foreground hover:bg-state-hover focus-visible:outline-hidden focus-visible:ring-2 focus-visible:ring-focus-ring"
        >
          <span
            aria-hidden="true"
            className={cn("size-2.25 rounded-full", dot)}
            style={dotStyle}
          />
          <span aria-hidden="true" style={{ writingMode: "vertical-rl" }}>
            {name}
          </span>
          <span
            aria-hidden="true"
            className="grid h-5.5 min-w-5.5 place-items-center rounded-[11px] bg-popover px-1.5 text-caption font-semibold dark:bg-card text-muted-foreground"
          >
            {count}
          </span>
        </button>
      </section>
    );
  }

  return (
    <section
      ref={setRefs}
      aria-label={name}
      data-lane-count={count}
      data-drop-target={dropTarget ? "" : undefined}
      className={cn(
        "flex shrink-0 flex-col gap-2 rounded-xl p-2 transition-shadow duration-150",
        kanbanColumnWidthClass,
        tint,
        dropTarget && "ring-2 ring-inset ring-primary",
      )}
    >
      <Popover
        open={editing || settingWip}
        onOpenChange={(next) => {
          if (!next) {
            setEditing(false);
            setSettingWip(false);
          }
        }}
      >
        <PopoverAnchor asChild>
          <div className="flex min-h-8 items-center gap-1.5 pr-0 pl-1.5">
            <span
              aria-hidden="true"
              className={cn("size-2.25 shrink-0 rounded-full", dot)}
              style={dotStyle}
            />
            <h2 className="min-w-0 truncate text-nav font-semibold text-foreground">
              {name}
            </h2>
            <span
              aria-hidden="true"
              data-wip={wipState ?? undefined}
              title={
                wipLimit === undefined
                  ? undefined
                  : fillCopy(copy.wipLimitTitle, { limit: wipLimit })
              }
              className={cn(
                "grid h-5.5 min-w-5.5 shrink-0 place-items-center rounded-[11px] px-1.5 text-caption font-semibold tabular-nums",
                wipState === "over"
                  ? "bg-danger-subtle text-danger-subtle-foreground"
                  : wipState === "full"
                    ? "bg-warning-subtle text-warning-subtle-foreground"
                    : "bg-popover text-muted-foreground dark:bg-card",
              )}
            >
              {wipLimit === undefined ? count : `${count} / ${wipLimit}`}
            </span>
            <span className="sr-only">
              {countText}
              {wipLimit === undefined
                ? ""
                : `, ${fillCopy(copy.wipLimitTitle, { limit: wipLimit })}`}
            </span>
            {canAdd && onRequestCreate ? (
              <Button
                variant="ghost"
                size="icon-sm"
                className="ml-auto size-7 rounded-[7px] text-meta-foreground hover:bg-state-hover-strong"
                aria-label={fillCopy(copy.addToLane, { lane: name })}
                onClick={onRequestCreate}
              >
                <Plus aria-hidden="true" className="size-4" />
              </Button>
            ) : null}
            <Button
              variant="ghost"
              size="icon-sm"
              className={cn(
                "size-7 rounded-[7px] text-meta-foreground hover:bg-state-hover-strong",
                !(canAdd && onRequestCreate) && "ml-auto",
              )}
              aria-label={fillCopy(copy.collapseLane, { lane: name })}
              onClick={() => onCollapsedChange(true)}
            >
              <ChevronLeft aria-hidden="true" className="size-4" />
            </Button>
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button
                  variant="ghost"
                  size="icon-sm"
                  className="size-7 rounded-[7px] text-meta-foreground hover:bg-state-hover-strong"
                  aria-label={fillCopy(copy.laneOptions, { lane: name })}
                >
                  <MoreHorizontal aria-hidden="true" className="size-4" />
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent
                align="end"
                onCloseAutoFocus={(event) => {
                  if (openingForm.current) {
                    openingForm.current = false;
                    event.preventDefault();
                    setEditing(true);
                  } else if (openingWip.current) {
                    openingWip.current = false;
                    event.preventDefault();
                    setSettingWip(true);
                  } else if (openingRemove.current) {
                    openingRemove.current = false;
                    event.preventDefault();
                    setRemoving(true);
                  }
                }}
              >
                {canEdit ? (
                  <DropdownMenuItem
                    onSelect={() => {
                      openingForm.current = true;
                    }}
                  >
                    {edit.subject === "status"
                      ? copy.editStatus
                      : copy.editValue}
                  </DropdownMenuItem>
                ) : null}
                {onWipLimitChange ? (
                  <DropdownMenuItem
                    onSelect={() => {
                      setWipText(
                        wipLimit === undefined ? "" : String(wipLimit),
                      );
                      setWipBad(false);
                      openingWip.current = true;
                    }}
                  >
                    {copy.wipSet}
                  </DropdownMenuItem>
                ) : null}
                {canEdit && edit.move ? (
                  <>
                    <DropdownMenuItem
                      disabled={!edit.move.canMoveLeft || edit.move.pending}
                      onSelect={() => edit.move?.onMove("before")}
                    >
                      <ArrowLeft aria-hidden="true" />
                      {copy.moveLeft}
                    </DropdownMenuItem>
                    <DropdownMenuItem
                      disabled={!edit.move.canMoveRight || edit.move.pending}
                      onSelect={() => edit.move?.onMove("after")}
                    >
                      <ArrowRight aria-hidden="true" />
                      {copy.moveRight}
                    </DropdownMenuItem>
                  </>
                ) : null}
                <DropdownMenuItem onSelect={() => onCollapsedChange(true)}>
                  {copy.collapse}
                </DropdownMenuItem>
                <DropdownMenuItem onSelect={onHide}>
                  {copy.hide}
                </DropdownMenuItem>
                {canEdit && edit.remove ? (
                  <>
                    <DropdownMenuSeparator />
                    <DropdownMenuItem
                      variant="destructive"
                      disabled={edit.remove.blockedReason !== undefined}
                      title={edit.remove.blockedReason}
                      onSelect={() => {
                        openingRemove.current = true;
                      }}
                    >
                      {edit.subject === "status"
                        ? copy.deleteStatus
                        : copy.deleteValue}
                    </DropdownMenuItem>
                  </>
                ) : null}
              </DropdownMenuContent>
            </DropdownMenu>
          </div>
        </PopoverAnchor>
        {settingWip ? (
          <PopoverContent align="start" className="w-72 gap-2">
            <form
              className="flex flex-col gap-2"
              onSubmit={(event) => {
                event.preventDefault();
                saveWip();
              }}
            >
              <label
                htmlFor={`${laneKey}-wip`}
                className="text-label font-semibold text-foreground"
              >
                {fillCopy(copy.wipLabel, { lane: name })}
              </label>
              <Input
                id={`${laneKey}-wip`}
                inputMode="numeric"
                autoFocus
                value={wipText}
                aria-invalid={wipBad ? true : undefined}
                onChange={(event) => {
                  setWipText(event.target.value);
                  setWipBad(false);
                }}
              />
              <p
                role={wipBad ? "alert" : undefined}
                className={cn(
                  "m-0 text-crumb",
                  wipBad ? "text-danger-text" : "text-meta-foreground",
                )}
              >
                {wipBad ? copy.wipInvalid : copy.wipHint}
              </p>
              <div className="flex items-center gap-1.5">
                <Button type="submit" size="sm">
                  {copy.wipSave}
                </Button>
                {wipLimit !== undefined ? (
                  <Button
                    type="button"
                    size="sm"
                    variant="ghost"
                    onClick={() => {
                      onWipLimitChange?.(null);
                      setSettingWip(false);
                    }}
                  >
                    {copy.wipClear}
                  </Button>
                ) : null}
              </div>
            </form>
          </PopoverContent>
        ) : canEdit ? (
          <PopoverContent align="start" className="w-72 gap-0">
            <StatusOptionForm
              copy={edit.formCopy}
              mode="edit"
              subject={edit.subject}
              initial={{
                name: lane.name,
                color: lane.color ?? undefined,
                category:
                  lane.category === "todo" ||
                  lane.category === "in_progress" ||
                  lane.category === "done"
                    ? lane.category
                    : undefined,
              }}
              existingNames={edit.otherNames}
              onCancel={() => setEditing(false)}
              onSubmit={(draft) => {
                edit.onSubmit(draft);
                setEditing(false);
              }}
            />
          </PopoverContent>
        ) : null}
      </Popover>

      {removing && canEdit && edit.remove ? (
        <RemoveOptionDialog
          copy={edit.formCopy}
          subject={edit.subject}
          name={name}
          itemCount={lane.total}
          choices={edit.remove.choices}
          pending={edit.remove.pending}
          onCancel={() => setRemoving(false)}
          onConfirm={(replacementId) => {
            edit.remove?.onConfirm(replacementId);
            setRemoving(false);
          }}
        />
      ) : null}

      {wipNote ? (
        <p
          data-wip-note={wipState ?? "drop"}
          className={cn(
            "m-0 -mt-0.5 flex items-center gap-1.25 px-1.5 text-metadata font-medium",
            wipState === "over" || (dropTarget && count > (wipLimit ?? 0))
              ? "text-danger-text"
              : "text-warning-subtle-foreground",
          )}
        >
          <TriangleAlert aria-hidden="true" className="size-3.25 shrink-0" />
          {wipNote}
        </p>
      ) : null}

      <SortableContext
        items={drawnCards.map((card) => card.id)}
        strategy={verticalListSortingStrategy}
      >
        {visibleCards.length > 0 ? (
          <div
            data-lane-cards=""
            ref={cardsRef}
            onScroll={(event) => moveWindow(event.currentTarget)}
            className="-mx-1 -my-0.5 flex max-h-[calc(100dvh-17rem)] min-h-12 flex-col gap-2 overflow-y-auto px-1 py-0.5 [scrollbar-width:thin]"
          >
            {spaceAbove > 0 ? (
              <div
                aria-hidden="true"
                style={{ height: spaceAbove }}
                className="shrink-0"
              />
            ) : null}
            {drawnCards.map((card) => (
              <KanbanCard
                key={card.id}
                card={card}
                laneKey={laneKey}
                now={now}
                timeZone={timeZone}
                copy={cardCopy}
                selected={card.id === selectedItemId}
                dragDisabled={!canDrag}
                onMoveKey={onMoveCardByKey}
                actions={cardActions}
                selection={cardSelection}
                onOpenDetails={onOpenDetails}
              />
            ))}
            {spaceBelow > 0 ? (
              <div
                aria-hidden="true"
                style={{ height: spaceBelow }}
                className="shrink-0"
              />
            ) : null}
          </div>
        ) : null}
      </SortableContext>

      {count === 0 ? (
        filterActive && lane.total > 0 ? (
          <div
            data-lane-empty="filtered"
            className="flex flex-col items-center gap-2 rounded-[11px] border-[1.5px] border-dashed border-line-strong px-3 py-[22px] text-center text-crumb text-meta-foreground"
          >
            <b className="text-label font-semibold text-muted-foreground">
              {copy.emptyFiltered}
            </b>
            {pluralCopy(
              {
                one: copy.filteredHidden_one,
                other: copy.filteredHidden_other,
              },
              lane.total,
            )}
          </div>
        ) : (
          <div
            data-lane-empty="empty"
            className="flex flex-col items-center gap-2 rounded-[11px] border-[1.5px] border-dashed border-line-strong px-3 py-[22px] text-center text-crumb text-meta-foreground"
          >
            <Inbox aria-hidden="true" className="size-5.5" />
            <b className="text-label font-semibold text-muted-foreground">
              {copy.emptyTitle}
            </b>
            {copy.emptyHint}
            {canAdd && onRequestCreate ? (
              <Button
                type="button"
                variant="outline"
                size="sm"
                className="mt-0.5"
                onClick={onRequestCreate}
              >
                <Plus aria-hidden="true" className="size-4" />
                {copy.emptyAdd}
              </Button>
            ) : null}
          </div>
        )
      ) : null}

      {pagedMore && loadFailed && lane.hasMore ? (
        <div
          role="alert"
          className="flex items-start gap-2.5 rounded-[10px] bg-danger-subtle px-3.5 py-2.5 text-label text-danger-text"
        >
          <TriangleAlert
            aria-hidden="true"
            className="mt-0.5 size-4 shrink-0"
          />
          <span className="flex min-w-0 flex-1 flex-col">
            <b className="font-semibold text-foreground">
              {copy.loadMoreFailed}
            </b>
            <span>{copy.loadMoreFailedBody}</span>
          </span>
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={onLoadMore}
          >
            {copy.loadMoreRetry}
          </Button>
        </div>
      ) : pagedMore ? (
        <div className="flex flex-wrap items-center justify-center gap-3 px-6 py-2.5 text-crumb text-muted-foreground">
          {fillCopy(copy.shownOf, {
            shown: visibleCards.length,
            total: count,
          })}
          <Button
            type="button"
            variant="outline"
            size="sm"
            disabled={loadingMore}
            aria-busy={loadingMore || undefined}
            onClick={() => {
              focusFirstRevealed.current =
                lane.cards[visibleCards.length]?.id ?? null;
              if (visibleCards.length >= loaded) onLoadMore?.();
              setPageShown(visibleCards.length + KANBAN_LANE_PAGE);
            }}
          >
            {pluralCopy(
              { one: copy.pageMore_one, other: copy.pageMore_other },
              Math.min(KANBAN_LANE_PAGE, count - visibleCards.length),
            )}
          </Button>
        </div>
      ) : null}

      {hiddenCount > 0 ? (
        <button
          type="button"
          onClick={() => {
            focusFirstRevealed.current = lane.cards[limit]?.id ?? null;
            setExpanded(true);
          }}
          className="h-7.5 rounded-md bg-popover px-2 text-left dark:bg-card text-crumb font-medium text-muted-foreground hover:text-foreground focus-visible:outline-hidden focus-visible:ring-2 focus-visible:ring-focus-ring"
        >
          {pluralCopy(
            { one: copy.showMore_one, other: copy.showMore_other },
            hiddenCount,
          )}
        </button>
      ) : null}

      {canAdd ? (
        <KanbanAddCard
          laneName={name}
          copy={copy}
          openRequest={openAddRequest}
          onSubmit={onCreateItem}
          onExpand={onExpandCreate}
        />
      ) : null}
    </section>
  );
}
