import { X } from "lucide-react";
import {
  DropdownMenuItem,
  DropdownMenuRadioGroup,
  DropdownMenuRadioItem,
  DropdownMenuSub,
  DropdownMenuSubContent,
  DropdownMenuSubTrigger,
  Sheet,
  SheetContent,
  SheetTitle,
  cn,
} from "@notrelix/ui-web";
import type { Card } from "@notrelix/work-management-core";
import { STATUS_DOT_CLASS } from "../../card-detail/item-field-editors";
import type { KanbanCardCopy } from "./kanban-card.copy";

/** One column the card can be moved to: the option of the field the view is grouped by. */
export interface KanbanMoveLane {
  readonly id: string;
  readonly title: string;
  readonly category: keyof typeof STATUS_DOT_CLASS | null;
  readonly color: string | null;
}

/**
 * "Chuyển sang" in a card's options menu: the columns of the board and the one the card is in.
 * `onMove` is the one write the drop and the popup picker run; a viewer gets `disabledReason` instead.
 */
export interface KanbanCardMove {
  readonly lanes: readonly KanbanMoveLane[];
  readonly currentLaneId: (card: Card) => string | null;
  readonly onMove: (card: Card, laneId: string) => void;
  readonly disabledReason?: string;
}

/** The menu's own item: 44 px, 15 px, 9 px radius, as drawn for the card menu. */
export const MENU_ITEM_CLASS =
  "min-h-11 gap-3 rounded-[9px] px-3 text-[15px] font-medium [&_svg:not([class*='size-'])]:size-5";
export const MENU_PANEL_CLASS =
  "rounded-[14px] border border-line-strong p-1.5 ring-0 shadow-popover";

function MoveIcon() {
  return (
    <svg
      viewBox="0 0 24 24"
      aria-hidden="true"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.8"
      strokeLinecap="round"
      strokeLinejoin="round"
      className="text-muted-foreground"
    >
      <path d="M4 8h13l-3-3M20 16H7l3 3" />
    </svg>
  );
}

/** The dot of a column: a ring for a to-do column, else a filled one in the category or option colour. */
function Dot({ lane, size }: { lane: KanbanMoveLane; size: string }) {
  return (
    <span
      aria-hidden="true"
      className={cn(
        "shrink-0 rounded-full",
        size,
        lane.category
          ? lane.category === "todo"
            ? "ring-2 ring-work-status-todo ring-inset"
            : STATUS_DOT_CLASS[lane.category]
          : "bg-meta-foreground",
      )}
      style={
        !lane.category && lane.color
          ? { backgroundColor: lane.color }
          : undefined
      }
    />
  );
}

function CurrentName({ move, card }: { move: KanbanCardMove; card: Card }) {
  const current = move.lanes.find(
    (lane) => lane.id === move.currentLaneId(card),
  );
  return (
    <span className="ml-auto text-[13.5px] font-medium text-meta-foreground">
      {current?.title ?? ""}
    </span>
  );
}

/** The item of the options menu; at 390 it opens the sheet, otherwise the submenu of columns. */
export function CardMoveMenuItem({
  card,
  move,
  copy,
  phone,
  onOpenSheet,
}: {
  card: Card;
  move: KanbanCardMove;
  copy: KanbanCardCopy;
  phone: boolean;
  onOpenSheet: () => void;
}) {
  if (move.disabledReason)
    return (
      <>
        <DropdownMenuItem
          disabled
          title={move.disabledReason}
          className={MENU_ITEM_CLASS}
        >
          <MoveIcon />
          {copy.moveTo}
        </DropdownMenuItem>
        <p className="mx-3 mb-1 text-[12.5px] leading-[1.45] text-meta-foreground">
          {move.disabledReason}
        </p>
      </>
    );
  if (phone)
    return (
      <DropdownMenuItem
        className={MENU_ITEM_CLASS}
        onSelect={() => onOpenSheet()}
      >
        <MoveIcon />
        {copy.moveTo}
        <CurrentName move={move} card={card} />
      </DropdownMenuItem>
    );
  const currentId = move.currentLaneId(card);
  return (
    <DropdownMenuSub>
      <DropdownMenuSubTrigger
        className={cn(
          MENU_ITEM_CLASS,
          "[&>svg:last-child]:-ml-2 [&>svg:last-child]:size-[13px] [&>svg:last-child]:text-muted-foreground",
        )}
      >
        <MoveIcon />
        {copy.moveTo}
        <CurrentName move={move} card={card} />
      </DropdownMenuSubTrigger>
      <DropdownMenuSubContent
        aria-label={copy.moveTo}
        sideOffset={13}
        alignOffset={-7}
        className={cn(MENU_PANEL_CLASS, "w-[210px]")}
        onClick={(event) => event.stopPropagation()}
      >
        <DropdownMenuRadioGroup
          value={currentId ?? ""}
          onValueChange={(laneId) => {
            if (laneId !== currentId) move.onMove(card, laneId);
          }}
        >
          {move.lanes.map((lane) => (
            <DropdownMenuRadioItem
              key={lane.id}
              value={lane.id}
              className={cn(
                MENU_ITEM_CLASS,
                "pr-3 [&>[data-slot=dropdown-menu-radio-item-indicator]]:right-3 [&>[data-slot=dropdown-menu-radio-item-indicator]]:text-brand-subtle-foreground [&>[data-slot=dropdown-menu-radio-item-indicator]_svg]:size-5",
              )}
            >
              <Dot lane={lane} size="size-2.5" />
              {lane.title}
            </DropdownMenuRadioItem>
          ))}
        </DropdownMenuRadioGroup>
      </DropdownMenuSubContent>
    </DropdownMenuSub>
  );
}

/** The 390 px form: a bottom sheet with the card's key and column, its title, and one radio row per column. */
export function CardMoveSheet({
  card,
  move,
  copy,
  open,
  onOpenChange,
  returnFocus,
}: {
  card: Card;
  move: KanbanCardMove;
  copy: KanbanCardCopy;
  open: boolean;
  onOpenChange: (open: boolean) => void;
  /** The control that opened the menu: it takes the focus back when the sheet closes. */
  returnFocus: () => void;
}) {
  const currentId = move.currentLaneId(card);
  const current = move.lanes.find((lane) => lane.id === currentId);
  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent
        side="bottom"
        showCloseButton={false}
        aria-describedby={undefined}
        onClick={(event) => event.stopPropagation()}
        onCloseAutoFocus={(event) => {
          event.preventDefault();
          returnFocus();
        }}
        className="max-h-[94%] gap-0 overflow-y-auto rounded-t-[14px] data-[side=bottom]:border-t-0 bg-popover px-0 pt-1.5 pb-[34px] text-[15px] text-popover-foreground"
      >
        <div
          aria-hidden="true"
          className="mx-auto mb-0.5 h-[5px] w-9 rounded-[3px] bg-line-strong"
        />
        <div className="flex items-start gap-2 pr-[8px] pl-5">
          <div className="min-w-0 flex-1 pt-2.5">
            <span className="block text-[12px] leading-[1.45] text-meta-foreground">
              {card.itemKey ? `${card.itemKey} · ` : ""}
              {current?.title ?? ""}
            </span>
            <SheetTitle className="m-0 text-[16px] leading-[1.35] font-[650]">
              {card.title}
            </SheetTitle>
          </div>
          <button
            type="button"
            aria-label={copy.close}
            onClick={() => onOpenChange(false)}
            className="mt-1 grid size-11 shrink-0 place-items-center rounded-[12px] border-0 bg-transparent text-foreground hover:bg-state-hover-strong focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary"
          >
            <X aria-hidden="true" className="size-5" />
          </button>
        </div>
        <p
          id={`move-${card.id}`}
          className="m-0 px-5 pt-1.5 pb-1 text-[13px] leading-[1.45] font-semibold text-meta-foreground"
        >
          {copy.moveTo}
        </p>
        <div role="radiogroup" aria-labelledby={`move-${card.id}`}>
          {move.lanes.map((lane) => {
            const selected = lane.id === currentId;
            return (
              <button
                key={lane.id}
                type="button"
                role="radio"
                aria-checked={selected}
                onClick={() => {
                  onOpenChange(false);
                  if (!selected) move.onMove(card, lane.id);
                }}
                className="flex min-h-[52px] w-full items-center gap-3 border-0 bg-transparent px-5 text-left text-[15px] text-foreground hover:bg-state-hover focus-visible:outline-2 focus-visible:-outline-offset-2 focus-visible:outline-primary"
              >
                <span className="grid w-9 shrink-0 place-items-center">
                  <Dot lane={lane} size="size-3" />
                </span>
                <span
                  className={cn(
                    "min-w-0 flex-1",
                    selected ? "font-[650]" : "font-medium",
                  )}
                >
                  {lane.title}
                </span>
                {selected ? (
                  <span className="inline-flex items-center gap-1.5 text-[13px] text-brand-subtle-foreground">
                    {copy.moveCurrent}
                    <svg
                      viewBox="0 0 24 24"
                      aria-hidden="true"
                      fill="none"
                      stroke="currentColor"
                      strokeWidth="1.8"
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      className="size-5"
                    >
                      <path d="m5 12 5 5L20 7" />
                    </svg>
                  </span>
                ) : null}
              </button>
            );
          })}
        </div>
      </SheetContent>
    </Sheet>
  );
}
