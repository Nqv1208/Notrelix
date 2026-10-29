import { useId, type ReactNode } from "react";
import {
  Archive,
  ArchiveRestore,
  ArrowLeftRight,
  Bell,
  CircleCheck,
  Copy,
  ExternalLink,
  Link2,
  RotateCcw,
  Trash2,
} from "lucide-react";
import { DropdownMenuItem, DropdownMenuSeparator } from "@notrelix/ui-web";
import type { ItemActionsCopy } from "./item-actions.copy";

export interface ItemActionsState {
  readonly itemId: string;
  readonly itemKey?: string | null;
  readonly archived: boolean;
  readonly completed: boolean;
}

export interface ItemActionsPermissions {
  /** May change the item (UpdateItem). */
  readonly edit: boolean;
  /** May create items on the board (duplicate). */
  readonly create: boolean;
  /** May move it to another board or group. */
  readonly move: boolean;
  /** May archive and delete it. */
  readonly remove: boolean;
}

/** One handler per entry; an entry with no handler is not drawn on this surface. */
export interface ItemActionsHandlers {
  readonly copyLink?: (itemId: string) => void;
  readonly openFull?: (itemId: string) => void;
  readonly complete?: (itemId: string) => void;
  readonly reopen?: (itemId: string) => void;
  readonly remind?: (itemId: string) => void;
  readonly duplicate?: (itemId: string) => void;
  readonly moveBoard?: (itemId: string) => void;
  readonly archive?: (itemId: string) => void;
  readonly unarchive?: (itemId: string) => void;
  readonly remove?: (itemId: string) => void;
}

function Entry({
  icon,
  label,
  hint,
  reason,
  destructive,
  onSelect,
}: {
  icon: ReactNode;
  label: string;
  hint?: string;
  /** Why the entry is unavailable; it stays visible, disabled, with this said. */
  reason?: string;
  destructive?: boolean;
  onSelect?: () => void;
}) {
  const reasonId = useId();
  return (
    <DropdownMenuItem
      variant={destructive ? "destructive" : undefined}
      disabled={Boolean(reason)}
      aria-describedby={reason ? reasonId : undefined}
      onSelect={() => onSelect?.()}
      className="items-start"
    >
      <span aria-hidden="true" className="mt-0.5 shrink-0">
        {icon}
      </span>
      <span className="min-w-0">
        <span className="block">{label}</span>
        {reason ? (
          <small
            id={reasonId}
            className="block text-metadata text-muted-foreground"
          >
            {reason}
          </small>
        ) : hint ? (
          <small className="block text-metadata text-muted-foreground">
            {hint}
          </small>
        ) : null}
      </span>
    </DropdownMenuItem>
  );
}

/**
 * The one actions menu of an item, in the same order on the card, the side
 * sheet, the item page and the table row: copy link, open page, complete or
 * reopen, remind, duplicate, move board, archive or unarchive, delete. An
 * entry the person cannot use, or that does not apply to the item's state,
 * stays visible, disabled, with the reason written under it.
 */
export function ItemActionsMenuItems({
  copy,
  item,
  can,
  on,
}: {
  copy: ItemActionsCopy;
  item: ItemActionsState;
  can: ItemActionsPermissions;
  on: ItemActionsHandlers;
}) {
  const { itemId, archived, completed } = item;
  const first = on.copyLink || on.openFull;
  const middle =
    on.complete || on.reopen || on.remind || on.duplicate || on.moveBoard;
  const last = on.archive || on.unarchive || on.remove;
  return (
    <>
      {on.copyLink ? (
        <Entry
          icon={<Link2 className="size-4" />}
          label={copy.copyLink}
          hint={item.itemKey ?? undefined}
          onSelect={() => on.copyLink?.(itemId)}
        />
      ) : null}
      {on.openFull ? (
        <Entry
          icon={<ExternalLink className="size-4" />}
          label={copy.openFull}
          onSelect={() => on.openFull?.(itemId)}
        />
      ) : null}
      {first && middle ? <DropdownMenuSeparator /> : null}
      {completed && on.reopen ? (
        <Entry
          icon={<RotateCcw className="size-4" />}
          label={copy.reopen}
          reason={can.edit ? undefined : copy.reasonEdit}
          onSelect={() => on.reopen?.(itemId)}
        />
      ) : null}
      {!completed && on.complete ? (
        <Entry
          icon={<CircleCheck className="size-4" />}
          label={copy.complete}
          reason={
            archived
              ? copy.reasonArchived
              : !can.edit
                ? copy.reasonEdit
                : undefined
          }
          onSelect={() => on.complete?.(itemId)}
        />
      ) : null}
      {on.remind ? (
        <Entry
          icon={<Bell className="size-4" />}
          label={copy.remind}
          reason={
            archived
              ? copy.reasonArchived
              : completed
                ? copy.reasonDone
                : undefined
          }
          onSelect={() => on.remind?.(itemId)}
        />
      ) : null}
      {on.duplicate ? (
        <Entry
          icon={<Copy className="size-4" />}
          label={copy.duplicate}
          reason={can.create ? undefined : copy.reasonCreate}
          onSelect={() => on.duplicate?.(itemId)}
        />
      ) : null}
      {on.moveBoard ? (
        <Entry
          icon={<ArrowLeftRight className="size-4" />}
          label={copy.moveBoard}
          reason={
            archived
              ? copy.reasonUnarchiveFirst
              : !can.move
                ? copy.reasonEdit
                : undefined
          }
          onSelect={() => on.moveBoard?.(itemId)}
        />
      ) : null}
      {(first || middle) && last ? <DropdownMenuSeparator /> : null}
      {archived && on.unarchive ? (
        <Entry
          icon={<ArchiveRestore className="size-4" />}
          label={copy.unarchive}
          onSelect={() => on.unarchive?.(itemId)}
        />
      ) : null}
      {!archived && on.archive ? (
        <Entry
          icon={<Archive className="size-4" />}
          label={copy.archive}
          reason={can.remove ? undefined : copy.reasonEdit}
          onSelect={() => on.archive?.(itemId)}
        />
      ) : null}
      {on.remove ? (
        <Entry
          destructive
          icon={<Trash2 className="size-4" />}
          label={copy.delete}
          reason={can.remove ? undefined : copy.reasonEdit}
          onSelect={() => on.remove?.(itemId)}
        />
      ) : null}
    </>
  );
}
