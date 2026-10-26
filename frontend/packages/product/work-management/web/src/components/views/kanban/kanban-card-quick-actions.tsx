import { useState } from "react";
import { CalendarDays, MoreHorizontal, UserRound } from "lucide-react";
import {
  Button,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
  Input,
  Popover,
  PopoverContent,
  PopoverTrigger,
  cn,
} from "@notrelix/ui-web";
import type { Card } from "@notrelix/work-management-core";
import type { KanbanCardCopy } from "./kanban-card.copy";

/**
 * What a card offers on hover and focus. Each action is present only when the
 * viewer may do it: a read-only board passes none, and a board that cannot
 * name the signed-in member passes no `onAssignMe`.
 */
export interface KanbanCardActions {
  /** The signed-in member, read from the host's session. */
  readonly currentUserId?: string | null;
  readonly onAssignMe?: (card: Card) => void;
  /** `day` is `YYYY-MM-DD`; `null` clears the due date. */
  readonly onSetDue?: (card: Card, day: string | null) => void;
  readonly onCopyLink?: (cardId: string) => void;
  readonly onDuplicate?: (cardId: string) => void;
  readonly onArchive?: (cardId: string) => void;
}

/** `YYYY-MM-DD` of the day the card is due (the backend keeps UTC days). */
export function cardDueDay(card: Card): string {
  const due = card.dueAt ?? card.dueDate;
  return typeof due === "string" ? due.slice(0, 10) : "";
}

/** Whether the signed-in member is already one of the card's assignees. */
export function isAssignedTo(card: Card, userId: string | null | undefined) {
  return Boolean(
    userId && (card.members ?? []).some((member) => member.userId === userId),
  );
}

const ACTION_CLASS =
  "size-6 rounded-[6px] text-meta-foreground hover:bg-state-hover-strong hover:text-foreground";

/**
 * The three quick actions of a card: assign to me, set the due date, and the
 * options menu (copy link, duplicate, archive). They show on hover and
 * whenever focus is inside the card, so the keyboard reaches them with Tab;
 * `A` and `D` on the focused card do the first two. They sit above the
 * card's open control and never start a drag.
 */
export function KanbanCardQuickActions({
  card,
  copy,
  actions,
  dueOpen,
  onDueOpenChange,
}: {
  card: Card;
  copy: KanbanCardCopy;
  actions: KanbanCardActions;
  dueOpen: boolean;
  onDueOpenChange: (open: boolean) => void;
}) {
  const [menuOpen, setMenuOpen] = useState(false);
  const { onAssignMe, onSetDue, onCopyLink, onDuplicate, onArchive } = actions;
  const canAssign =
    Boolean(onAssignMe) && !isAssignedTo(card, actions.currentUserId);
  const hasMenu = Boolean(onCopyLink || onDuplicate || onArchive);
  if (!canAssign && !onSetDue && !hasMenu) return null;
  const pinned = dueOpen || menuOpen;
  const day = cardDueDay(card);

  return (
    <span
      data-card-quick-actions=""
      onPointerDown={(event) => event.stopPropagation()}
      className={cn(
        "absolute top-1.5 right-1.5 z-20 flex items-center gap-0.5 rounded-[8px] border border-line-strong bg-popover p-0.5 shadow-card-hover transition-opacity",
        pinned
          ? "opacity-100"
          : "pointer-events-none opacity-0 group-focus-within:pointer-events-auto group-focus-within:opacity-100 group-hover:pointer-events-auto group-hover:opacity-100 focus-within:pointer-events-auto focus-within:opacity-100",
      )}
    >
      {canAssign ? (
        <Button
          type="button"
          variant="ghost"
          size="icon-sm"
          className={ACTION_CLASS}
          aria-label={copy.assignMe}
          aria-keyshortcuts="A"
          title={copy.assignMe}
          onClick={() => onAssignMe?.(card)}
        >
          <UserRound aria-hidden="true" className="size-3.5" />
        </Button>
      ) : null}
      {onSetDue ? (
        <Popover open={dueOpen} onOpenChange={onDueOpenChange}>
          <PopoverTrigger asChild>
            <Button
              type="button"
              variant="ghost"
              size="icon-sm"
              className={ACTION_CLASS}
              aria-label={copy.setDue}
              aria-keyshortcuts="D"
              title={copy.setDue}
            >
              <CalendarDays aria-hidden="true" className="size-3.5" />
            </Button>
          </PopoverTrigger>
          <PopoverContent
            align="end"
            className="w-56 gap-2 p-3"
            onClick={(event) => event.stopPropagation()}
          >
            <label className="flex flex-col gap-1.5 text-metadata font-semibold text-muted-foreground">
              {copy.dueDay}
              <Input
                type="date"
                value={day}
                onChange={(event) => {
                  if (!event.target.value) return;
                  onSetDue(card, event.target.value);
                  onDueOpenChange(false);
                }}
              />
            </label>
            {day ? (
              <Button
                type="button"
                variant="ghost"
                size="sm"
                className="justify-start"
                onClick={() => {
                  onSetDue(card, null);
                  onDueOpenChange(false);
                }}
              >
                {copy.clearDue}
              </Button>
            ) : null}
          </PopoverContent>
        </Popover>
      ) : null}
      {hasMenu ? (
        <DropdownMenu open={menuOpen} onOpenChange={setMenuOpen}>
          <DropdownMenuTrigger asChild>
            <Button
              type="button"
              variant="ghost"
              size="icon-sm"
              className={ACTION_CLASS}
              aria-label={copy.moreOptions}
              title={copy.moreOptions}
            >
              <MoreHorizontal aria-hidden="true" className="size-3.5" />
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end">
            {onCopyLink ? (
              <DropdownMenuItem onSelect={() => onCopyLink(card.id)}>
                {copy.copyLink}
              </DropdownMenuItem>
            ) : null}
            {onDuplicate ? (
              <DropdownMenuItem onSelect={() => onDuplicate(card.id)}>
                {copy.duplicate}
              </DropdownMenuItem>
            ) : null}
            {onArchive ? (
              <DropdownMenuItem onSelect={() => onArchive(card.id)}>
                {copy.archive}
              </DropdownMenuItem>
            ) : null}
          </DropdownMenuContent>
        </DropdownMenu>
      ) : null}
    </span>
  );
}
