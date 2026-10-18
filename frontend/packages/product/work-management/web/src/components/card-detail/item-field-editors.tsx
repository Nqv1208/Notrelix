import type { ReactNode } from "react";
import {
  Avatar,
  AvatarFallback,
  Button,
  Calendar,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@notrelix/ui-web";
import {
  findPriorityField,
  findStatusField,
  type Board,
  type CardDetail,
  type FieldDefinition,
} from "@notrelix/work-management-core";

/**
 * The property editors the item side sheet and the item page share: the menu
 * or popover behind a property, with the trigger passed in. A read-only
 * viewer, or a board without the field, gets the trigger alone, so each host
 * keeps its own look for the control and the editing behaviour stays one.
 */

export type ItemStatusCategory = NonNullable<CardDetail["statusCategory"]>;

/** Status tint = the lane tint of the status category. */
export const STATUS_LANE_CLASS: Readonly<Record<ItemStatusCategory, string>> = {
  todo: "bg-work-status-todo-lane",
  in_progress: "bg-work-status-doing-lane",
  review: "bg-work-status-review-lane",
  done: "bg-work-status-done-lane",
};

export const STATUS_DOT_CLASS: Readonly<Record<ItemStatusCategory, string>> = {
  todo: "bg-work-status-todo",
  in_progress: "bg-work-status-doing",
  review: "bg-work-status-review",
  done: "bg-work-status-done",
};

export type ItemPriority = NonNullable<CardDetail["priority"]>;

export const PRIORITY_CLASS: Readonly<Record<ItemPriority, string>> = {
  urgent: "text-danger-text",
  high: "text-priority-high-text",
  medium: "text-muted-foreground",
  low: "text-muted-foreground",
};

/**
 * Board fields an item's properties edit. Status and priority are resolved by
 * their role on the board read (`findStatusField`, `findPriorityField`), which
 * also accept the older fixture ids.
 */
export function findItemFields(board: Board) {
  const fields = board.fieldDefinitions;
  const byId = (suffix: string) =>
    fields.find((field) => field.id.endsWith(suffix));
  return {
    status: findStatusField(fields),
    priority: findPriorityField(fields) ?? byId("field-priority"),
    person:
      fields.find((field) => field.fieldType === "person") ??
      byId("field-person"),
    due:
      fields.find((field) => field.fieldType === "date") ??
      byId("field-due-date"),
  };
}

export function isItemDone(card: CardDetail): boolean {
  return card.statusCategory === "done" || Boolean(card.completedAt);
}

/** Status of a card: its option, the category and the tint to show. */
export function describeItemStatus(
  card: CardDetail,
  statusField: FieldDefinition | undefined,
) {
  const statusId = card.statusOptionId ?? card.status;
  const option = statusField?.options.find((entry) => entry.id === statusId);
  const category: ItemStatusCategory | null =
    card.statusCategory ?? option?.category ?? null;
  return { option, category };
}

/** A select field's options as a menu; the trigger is shown alone when it cannot edit. */
export function ItemOptionMenu({
  field,
  readOnly,
  onSelect,
  children,
}: {
  field: FieldDefinition | undefined;
  readOnly: boolean;
  onSelect: (optionId: string) => void;
  children: ReactNode;
}) {
  if (readOnly || !field) return <>{children}</>;
  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>{children}</DropdownMenuTrigger>
      <DropdownMenuContent align="start">
        {field.options.map((option) => (
          <DropdownMenuItem
            key={option.id}
            onSelect={() => onSelect(option.id)}
          >
            {option.label}
          </DropdownMenuItem>
        ))}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}

/** The board members as a toggle list; a pick sends the whole assignee set. */
export function ItemAssigneePopover({
  board,
  card,
  label,
  readOnly,
  onChange,
  children,
}: {
  board: Board;
  card: CardDetail;
  label: string;
  readOnly: boolean;
  onChange: (userIds: string[]) => void;
  children: ReactNode;
}) {
  if (readOnly) return <>{children}</>;
  const selectedUserIds = new Set(card.members.map((member) => member.userId));
  return (
    <Popover>
      <PopoverTrigger asChild>{children}</PopoverTrigger>
      <PopoverContent align="start" className="w-64 p-1">
        <ul aria-label={label} className="flex flex-col gap-0.5">
          {board.members.map((member) => {
            const selected = selectedUserIds.has(member.userId);
            return (
              <li key={member.id}>
                <button
                  type="button"
                  aria-pressed={selected}
                  className="flex w-full items-center gap-2 rounded-md px-2 py-1.5 text-left text-sm hover:bg-state-hover"
                  onClick={() => {
                    const next = new Set(selectedUserIds);
                    if (selected) next.delete(member.userId);
                    else next.add(member.userId);
                    onChange(Array.from(next));
                  }}
                >
                  <Avatar className="size-6">
                    <AvatarFallback className="bg-foreground text-caption font-bold text-background">
                      {member.initials}
                    </AvatarFallback>
                  </Avatar>
                  <span className="min-w-0 flex-1 truncate">{member.name}</span>
                </button>
              </li>
            );
          })}
        </ul>
      </PopoverContent>
    </Popover>
  );
}

/** A due-date calendar with a clear action; `null` clears the date. */
export function ItemDuePopover({
  dueAt,
  clearLabel,
  readOnly,
  onChange,
  children,
}: {
  dueAt: string | null;
  clearLabel: string;
  readOnly: boolean;
  onChange: (value: string | null) => void;
  children: ReactNode;
}) {
  if (readOnly) return <>{children}</>;
  return (
    <Popover>
      <PopoverTrigger asChild>{children}</PopoverTrigger>
      <PopoverContent align="start" className="w-auto p-0">
        <Calendar
          mode="single"
          selected={dueAt ? new Date(dueAt) : undefined}
          onSelect={(date: Date | undefined) =>
            onChange(date ? date.toISOString() : null)
          }
        />
        {dueAt ? (
          <div className="flex justify-end border-t border-border p-2">
            <Button
              type="button"
              variant="ghost"
              size="sm"
              className="text-danger-text"
              onClick={() => onChange(null)}
            >
              {clearLabel}
            </Button>
          </div>
        ) : null}
      </PopoverContent>
    </Popover>
  );
}
