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
  Popover,
  PopoverAnchor,
  PopoverContent,
  cn,
} from "@notrelix/ui-web";
import {
  DEFAULT_KANBAN_LANE_CARD_LIMIT,
  isOptimisticOptionId,
  type KanbanStatusLane,
  type WorkStatusCategory,
} from "@notrelix/work-management-core";
import {
  StatusOptionForm,
  type StatusOptionDraft,
} from "../../status-option/status-option-form";
import type { StatusOptionFormCopy } from "../../status-option/status-option-form.copy";
import { KanbanAddCard } from "./kanban-add-card";
import { KanbanCard } from "./kanban-card";
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
  /** Rename and recolour from the lane menu; absent when the viewer cannot. */
  edit?: KanbanLaneEdit;
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
 * lane shows `laneCardLimit` cards (four by default), then "Xem thêm N việc"; the selected card is always
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
  edit,
  collapsed,
  openAddRequest,
  onCollapsedChange,
  onHide,
  onOpenDetails,
  onCreateItem,
}: KanbanColumnProps) {
  const sectionRef = useRef<HTMLElement | null>(null);
  const [expanded, setExpanded] = useState(false);
  const [editing, setEditing] = useState(false);
  // "Sửa trạng thái" was chosen: the form opens once the menu has closed (a
  // form that opens while the menu still holds focus is dismissed by it), and
  // the menu does not give focus back to its button.
  const openingForm = useRef(false);
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

  const count = lane.cards.length;
  const selectedIndex = selectedItemId
    ? lane.cards.findIndex((card) => card.id === selectedItemId)
    : -1;
  const draggedIndex = draggedCardId
    ? lane.cards.findIndex((card) => card.id === draggedCardId)
    : -1;
  const showAll =
    expanded ||
    count <= laneCardLimit ||
    selectedIndex >= laneCardLimit ||
    draggedIndex >= laneCardLimit;
  const visibleCards = showAll
    ? lane.cards
    : lane.cards.slice(0, laneCardLimit);
  const hiddenCount = count - visibleCards.length;
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
      <Popover open={editing} onOpenChange={setEditing}>
        <PopoverAnchor asChild>
          <div className="flex h-7.5 items-center gap-2 px-1.5">
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
              <DropdownMenuContent
                align="end"
                onCloseAutoFocus={(event) => {
                  if (openingForm.current) {
                    openingForm.current = false;
                    event.preventDefault();
                    setEditing(true);
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
                <DropdownMenuItem onSelect={() => onCollapsedChange(true)}>
                  {copy.collapse}
                </DropdownMenuItem>
                <DropdownMenuItem onSelect={onHide}>
                  {copy.hide}
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          </div>
        </PopoverAnchor>
        {canEdit ? (
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
                laneKey={laneKey}
                now={now}
                timeZone={timeZone}
                copy={cardCopy}
                selected={card.id === selectedItemId}
                dragDisabled={!canDrag}
                onMoveKey={onMoveCardByKey}
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
            focusFirstRevealed.current = lane.cards[laneCardLimit]?.id ?? null;
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
