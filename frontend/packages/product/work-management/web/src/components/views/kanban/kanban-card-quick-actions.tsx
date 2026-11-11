import { useRef, useState } from "react";
import {
  Archive,
  FolderInput,
  CalendarDays,
  Copy,
  Link2,
  MoreHorizontal,
  Trash2,
  UserRound,
} from "lucide-react";
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
  useMediaQuery,
} from "@notrelix/ui-web";
import type { Card } from "@notrelix/work-management-core";
import {
  CardMoveMenuItem,
  CardMoveSheet,
  MENU_ITEM_CLASS,
  MENU_PANEL_CLASS,
  type KanbanCardMove,
} from "./kanban-card-move-menu";
import type { KanbanCardCopy } from "./kanban-card.copy";
import {
  KanbanCardQuickEdit,
  type KanbanQuickEdit,
} from "./kanban-card-quick-edit";

/**
 * What a card offers on hover and focus. Each action is present only when the
 * viewer may do it: a read-only board passes none, and a board that cannot
 * name the signed-in member passes no `onAssignMe`.
 */
/** A refused move shown on the card itself. */
export interface KanbanCardFailure {
  readonly message: string;
  readonly onRetry: () => void;
}

export interface KanbanCardActions {
  /** The signed-in member, read from the host's session. */
  readonly currentUserId?: string | null;
  readonly onAssignMe?: (card: Card) => void;
  /** `day` is `YYYY-MM-DD`; `null` clears the due date. */
  readonly onSetDue?: (card: Card, day: string | null) => void;
  /** Edit the card in place (name, labels, people, dates) without opening the popup. */
  readonly quickEdit?: KanbanQuickEdit;
  readonly onCopyLink?: (cardId: string) => void;
  readonly onDuplicate?: (cardId: string) => void;
  readonly onArchive?: (cardId: string) => void;
  readonly onDelete?: (cardId: string) => void;
  readonly onMoveToBoard?: (cardId: string) => void;
  /** "Chuyển sang": the columns and the one write that moves the card to one of them. */
  readonly move?: KanbanCardMove;
  /** The refused move of a card, when its last write failed; absent: none. */
  readonly failureOf?: (cardId: string) => KanbanCardFailure | undefined;
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

/** Where the options button is always on the card (as drawn for the 390 board): a phone width or a coarse pointer. */
export const MORE_ALWAYS_QUERY = "(max-width: 767px), (pointer: coarse)";

/** Whether the actions give the card an options menu. */
export function hasCardMenu(actions: KanbanCardActions): boolean {
  return Boolean(
    actions.onCopyLink ||
    actions.onDuplicate ||
    actions.onArchive ||
    actions.onDelete ||
    actions.onMoveToBoard ||
    actions.move,
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
  const [moveOpen, setMoveOpen] = useState(false);
  const [editOpen, setEditOpen] = useState(false);
  const moreRef = useRef<HTMLButtonElement>(null);
  const phone = useMediaQuery("(max-width: 767px)");
  const always = useMediaQuery(MORE_ALWAYS_QUERY);
  const {
    onAssignMe,
    onSetDue,
    onCopyLink,
    onDuplicate,
    onArchive,
    onDelete,
    onMoveToBoard,
    move,
  } = actions;
  // On a touch screen the hover actions are not reachable: only the options button stays, always visible.
  // On a pointer device a card that can be edited shows two buttons: Edit, which holds every other action, and Archive.
  const merged = !always && actions.quickEdit !== undefined;
  const canAssign =
    !always &&
    !merged &&
    Boolean(onAssignMe) &&
    !isAssignedTo(card, actions.currentUserId);
  const hasMenu = hasCardMenu(actions) && !merged;
  const editRows = [
    onDuplicate && {
      key: "copy",
      label: copy.copyCard,
      icon: <Copy aria-hidden="true" className="size-4" />,
      run: () => onDuplicate(card.id),
    },
    onCopyLink && {
      key: "link",
      label: copy.copyLink,
      icon: <Link2 aria-hidden="true" className="size-4" />,
      run: () => onCopyLink(card.id),
    },
    onMoveToBoard && {
      key: "board",
      label: copy.moveToBoard,
      icon: <FolderInput aria-hidden="true" className="size-4" />,
      run: () => onMoveToBoard(card.id),
    },
    onDelete && {
      key: "delete",
      label: copy.delete,
      icon: <Trash2 aria-hidden="true" className="size-4" />,
      run: () => onDelete(card.id),
    },
  ].filter((row): row is NonNullable<typeof row> => Boolean(row));
  const canEdit = merged;
  if (!canAssign && !(onSetDue && !always && !merged) && !hasMenu && !canEdit)
    return null;
  // At phone width a tap on the options button opens the move sheet (the menu's actions follow the columns).
  const sheetMode = phone && move !== undefined && !move.disabledReason;
  const sheetRows = [
    onCopyLink && { label: copy.copyLink, run: () => onCopyLink(card.id) },
    onDuplicate && { label: copy.duplicate, run: () => onDuplicate(card.id) },
    onMoveToBoard && {
      label: copy.moveToBoard,
      run: () => onMoveToBoard(card.id),
    },
    onArchive && { label: copy.archive, run: () => onArchive(card.id) },
    onDelete && {
      label: copy.delete,
      run: () => onDelete(card.id),
      destructive: true,
    },
  ].filter((row): row is NonNullable<typeof row> => Boolean(row));
  const pinned = dueOpen || menuOpen || editOpen;
  const day = cardDueDay(card);

  const moreButton = (
    <Button
      ref={moreRef}
      type="button"
      variant="ghost"
      size="icon-sm"
      className={
        always
          ? "size-11 rounded-[12px] text-meta-foreground hover:bg-state-hover-strong hover:text-foreground"
          : ACTION_CLASS
      }
      aria-label={copy.moreOptions}
      aria-haspopup={sheetMode ? "dialog" : undefined}
      title={copy.moreOptions}
      onClick={sheetMode ? () => setMoveOpen(true) : undefined}
    >
      <MoreHorizontal
        aria-hidden="true"
        className={always ? "size-5" : "size-3.5"}
      />
    </Button>
  );

  return (
    <span
      data-card-quick-actions=""
      onPointerDown={(event) => event.stopPropagation()}
      className={cn(
        "absolute z-20 flex items-center",
        always
          ? "top-px right-[5px]"
          : "top-1.5 right-1.5 gap-0.5 rounded-[8px] border border-line-strong bg-popover p-0.5 shadow-card-hover transition-opacity",
        !always &&
          (pinned
            ? "opacity-100"
            : "pointer-events-none opacity-0 group-has-[:focus-visible]:pointer-events-auto group-has-[:focus-visible]:opacity-100 group-hover:pointer-events-auto group-hover:opacity-100 has-[:focus-visible]:pointer-events-auto has-[:focus-visible]:opacity-100"),
      )}
    >
      {canEdit && actions.quickEdit ? (
        <KanbanCardQuickEdit
          card={card}
          copy={copy}
          edit={actions.quickEdit}
          open={editOpen || dueOpen}
          onOpenChange={(next) => {
            setEditOpen(next);
            onDueOpenChange(next);
          }}
          move={move && !move.disabledReason ? move : undefined}
          rows={editRows}
          className={ACTION_CLASS}
        />
      ) : null}
      {canEdit && onArchive ? (
        <Button
          type="button"
          variant="ghost"
          size="icon-sm"
          className={ACTION_CLASS}
          aria-label={`${copy.archive}: ${card.title}`}
          title={copy.archive}
          onClick={() => onArchive(card.id)}
        >
          <Archive aria-hidden="true" className="size-3.5" />
        </Button>
      ) : null}
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
      {onSetDue && !always && !merged ? (
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
        sheetMode ? (
          moreButton
        ) : (
          <DropdownMenu open={menuOpen} onOpenChange={setMenuOpen}>
            <DropdownMenuTrigger asChild>{moreButton}</DropdownMenuTrigger>
            <DropdownMenuContent
              align="end"
              className={cn(MENU_PANEL_CLASS, "w-[272px]")}
            >
              {move ? (
                <CardMoveMenuItem card={card} move={move} copy={copy} />
              ) : null}
              {onCopyLink ? (
                <DropdownMenuItem
                  className={MENU_ITEM_CLASS}
                  onSelect={() => onCopyLink(card.id)}
                >
                  {copy.copyLink}
                </DropdownMenuItem>
              ) : null}
              {onDuplicate ? (
                <DropdownMenuItem
                  className={MENU_ITEM_CLASS}
                  onSelect={() => onDuplicate(card.id)}
                >
                  {copy.duplicate}
                </DropdownMenuItem>
              ) : null}
              {onMoveToBoard ? (
                <DropdownMenuItem
                  className={MENU_ITEM_CLASS}
                  onSelect={() => onMoveToBoard(card.id)}
                >
                  {copy.moveToBoard}
                </DropdownMenuItem>
              ) : null}
              {onArchive ? (
                <DropdownMenuItem
                  className={MENU_ITEM_CLASS}
                  onSelect={() => onArchive(card.id)}
                >
                  {copy.archive}
                </DropdownMenuItem>
              ) : null}
              {onDelete ? (
                <DropdownMenuItem
                  variant="destructive"
                  className={MENU_ITEM_CLASS}
                  onSelect={() => onDelete(card.id)}
                >
                  {copy.delete}
                </DropdownMenuItem>
              ) : null}
            </DropdownMenuContent>
          </DropdownMenu>
        )
      ) : null}
      {sheetMode ? (
        <CardMoveSheet
          card={card}
          move={move}
          copy={copy}
          open={moveOpen}
          onOpenChange={setMoveOpen}
          returnFocus={() => moreRef.current?.focus()}
          rows={sheetRows}
        />
      ) : null}
    </span>
  );
}
