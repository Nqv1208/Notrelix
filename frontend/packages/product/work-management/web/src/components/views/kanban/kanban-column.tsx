import { useEffect, useRef, useState } from "react";
import { useDroppable } from "@dnd-kit/core";
import {
  SortableContext,
  verticalListSortingStrategy,
} from "@dnd-kit/sortable";
import { MoreHorizontal } from "lucide-react";
import {
  Button,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
  cn,
} from "@notrelix/ui-web";
import type {
  KanbanStatusLane,
  WorkStatusCategory,
} from "@notrelix/work-management-core";
import { KanbanAddCard } from "./kanban-add-card";
import { KanbanCard } from "./kanban-card";
import { fillCopy, pluralCopy, type KanbanCardCopy } from "./kanban-card.copy";
import { kanbanLaneDropId } from "./kanban-dnd";
import { kanbanColumnWidthClass } from "./kanban-layout";
import type { KanbanLaneCopy } from "./kanban-lane.copy";

/** Cards a lane shows before "Xem thêm N việc". */
export const KANBAN_LANE_VISIBLE_CARDS = 4;

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
  collapsed: boolean;
  /** Changing this number opens the lane's add field. */
  openAddRequest?: number;
  onCollapsedChange: (collapsed: boolean) => void;
  onHide: () => void;
  onOpenDetails: (cardId: string) => void;
  onCreateItem: (title: string) => void;
}

/**
 * One Kanban lane: the cards whose grouping value is the lane's option.
 * Header: dot, name, count (the lane's cards under the current filter) and
 * "Tuỳ chọn cột {lane}" (collapse or hide the lane on this view only). The
 * lane shows four cards, then "Xem thêm N việc"; the selected card is always
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
  collapsed,
  openAddRequest,
  onCollapsedChange,
  onHide,
  onOpenDetails,
  onCreateItem,
}: KanbanColumnProps) {
  const sectionRef = useRef<HTMLElement | null>(null);
  const [expanded, setExpanded] = useState(false);
  const focusFirstRevealed = useRef<string | null>(null);
  const { setNodeRef, isOver } = useDroppable({
    id: kanbanLaneDropId(lane.optionId),
    data: { type: "kanban-lane", lane },
    disabled: !canDrag,
  });

  const count = lane.cards.length;
  const selectedIndex = selectedItemId
    ? lane.cards.findIndex((card) => card.id === selectedItemId)
    : -1;
  const showAll =
    expanded ||
    count <= KANBAN_LANE_VISIBLE_CARDS ||
    selectedIndex >= KANBAN_LANE_VISIBLE_CARDS;
  const visibleCards = showAll
    ? lane.cards
    : lane.cards.slice(0, KANBAN_LANE_VISIBLE_CARDS);
  const hiddenCount = count - visibleCards.length;
  const tint = lane.category
    ? LANE_TINT_CLASS[lane.category]
    : LANE_TINT_CLASS.todo;
  const dot = lane.category
    ? LANE_DOT_CLASS[lane.category]
    : "bg-muted-foreground";
  const countText = pluralCopy(
    { one: copy.count_one, other: copy.count_other },
    count,
  );

  useEffect(() => {
    const cardId = focusFirstRevealed.current;
    if (!cardId || !expanded) return;
    focusFirstRevealed.current = null;
    const target = Array.from(
      sectionRef.current?.querySelectorAll<HTMLElement>("[data-card-id]") ?? [],
    ).find((element) => element.dataset.cardId === cardId);
    target?.focus();
  }, [expanded]);

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
      className={cn(
        "flex shrink-0 flex-col gap-2 rounded-xl p-2 transition-shadow duration-150",
        kanbanColumnWidthClass,
        tint,
        isOver && "ring-2 ring-inset ring-primary",
      )}
    >
      <div className="flex h-7.5 items-center gap-2 px-1.5">
        <span
          aria-hidden="true"
          className={cn("size-2.25 shrink-0 rounded-full", dot)}
        />
        <h2 className="min-w-0 truncate text-nav font-semibold text-foreground">
          {name}
        </h2>
        <span
          aria-hidden="true"
          className="grid h-5.5 min-w-5.5 shrink-0 place-items-center rounded-[11px] bg-popover px-1.5 text-caption font-semibold dark:bg-card text-muted-foreground"
        >
          {count}
        </span>
        <span className="sr-only">{countText}</span>
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button
              variant="ghost"
              size="icon-sm"
              className="ml-auto size-7 rounded-[7px] text-meta-foreground hover:bg-state-hover-strong"
              aria-label={fillCopy(copy.laneOptions, { lane: name })}
            >
              <MoreHorizontal aria-hidden="true" className="size-4" />
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end">
            <DropdownMenuItem onSelect={() => onCollapsedChange(true)}>
              {copy.collapse}
            </DropdownMenuItem>
            <DropdownMenuItem onSelect={onHide}>{copy.hide}</DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </div>

      <SortableContext
        items={visibleCards.map((card) => card.id)}
        strategy={verticalListSortingStrategy}
      >
        {visibleCards.length > 0 ? (
          <div className="flex flex-col gap-2">
            {visibleCards.map((card) => (
              <KanbanCard
                key={card.id}
                card={card}
                now={now}
                timeZone={timeZone}
                copy={cardCopy}
                selected={card.id === selectedItemId}
                dragDisabled={!canDrag}
                onOpenDetails={onOpenDetails}
              />
            ))}
          </div>
        ) : null}
      </SortableContext>

      {count === 0 ? (
        <p className="rounded-lg border border-dashed border-line-strong px-2 py-3.5 text-center text-crumb text-meta-foreground">
          {filterActive ? copy.emptyFiltered : copy.empty}
        </p>
      ) : null}

      {hiddenCount > 0 ? (
        <button
          type="button"
          onClick={() => {
            focusFirstRevealed.current =
              lane.cards[KANBAN_LANE_VISIBLE_CARDS]?.id ?? null;
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
        />
      ) : null}
    </section>
  );
}
