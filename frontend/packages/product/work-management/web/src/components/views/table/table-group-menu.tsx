import { useRef, useState } from "react";
import { ArrowDown, ArrowUp, MoreHorizontal } from "lucide-react";
import {
  Button,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@notrelix/ui-web";
import {
  RemoveOptionDialog,
  type RemoveOptionChoice,
} from "../../status-option/remove-option-dialog";
import type { StatusOptionFormCopy } from "../../status-option/status-option-form.copy";
import { fillTableCopy, type MainTableCopy } from "./main-table.copy";

/** What the group menu of one table group needs; the container builds it per group. */
export interface TableGroupMenuModel {
  readonly formCopy: StatusOptionFormCopy;
  /** Status groups are statuses (items must move), other Select groups are values. */
  readonly subject: "status" | "value";
  readonly canMoveUp: boolean;
  readonly canMoveDown: boolean;
  /** A move or delete is in flight: the entries wait so a stale version is never sent. */
  readonly pending: boolean;
  /** Why the group cannot be deleted; absent = it can. */
  readonly blockedReason?: string;
  /** The other options the items can move to. */
  readonly choices: readonly RemoveOptionChoice[];
  /** How many items the group holds (before search and filter). */
  readonly itemCount: number;
  readonly onMove: (direction: "before" | "after") => void;
  readonly onDelete: (replacementOptionId: string | null) => void;
}

/**
 * The "…" of a group header: move the group (its option of the grouped
 * field) up or down, delete it. Delete asks where the group's items go
 * first. The dialog opens once the menu has closed, so the menu does not
 * dismiss it.
 */
export function TableGroupMenu({
  copy,
  name,
  menu,
}: {
  copy: MainTableCopy;
  name: string;
  menu: TableGroupMenuModel;
}) {
  const [removing, setRemoving] = useState(false);
  const openingRemove = useRef(false);
  const forValue = menu.subject === "value";

  return (
    <>
      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button
            variant="ghost"
            size="icon-sm"
            className="ml-1 size-6 rounded-[5px] text-meta-foreground hover:bg-state-hover-strong"
            aria-label={fillTableCopy(copy.group.menu, { name })}
          >
            <MoreHorizontal aria-hidden="true" className="size-3.5" />
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent
          align="start"
          onCloseAutoFocus={(event) => {
            if (openingRemove.current) {
              openingRemove.current = false;
              event.preventDefault();
              setRemoving(true);
            }
          }}
        >
          <DropdownMenuItem
            disabled={!menu.canMoveUp || menu.pending}
            onSelect={() => menu.onMove("before")}
          >
            <ArrowUp aria-hidden="true" />
            {copy.group.moveUp}
          </DropdownMenuItem>
          <DropdownMenuItem
            disabled={!menu.canMoveDown || menu.pending}
            onSelect={() => menu.onMove("after")}
          >
            <ArrowDown aria-hidden="true" />
            {copy.group.moveDown}
          </DropdownMenuItem>
          <DropdownMenuSeparator />
          <DropdownMenuItem
            variant="destructive"
            disabled={menu.blockedReason !== undefined}
            title={menu.blockedReason}
            onSelect={() => {
              openingRemove.current = true;
            }}
          >
            {forValue ? copy.group.deleteValue : copy.group.deleteStatus}
          </DropdownMenuItem>
        </DropdownMenuContent>
      </DropdownMenu>
      {removing ? (
        <RemoveOptionDialog
          copy={menu.formCopy}
          subject={menu.subject}
          name={name}
          itemCount={menu.itemCount}
          choices={menu.choices}
          pending={menu.pending}
          onCancel={() => setRemoving(false)}
          onConfirm={(replacementId) => {
            menu.onDelete(replacementId);
            setRemoving(false);
          }}
        />
      ) : null}
    </>
  );
}
