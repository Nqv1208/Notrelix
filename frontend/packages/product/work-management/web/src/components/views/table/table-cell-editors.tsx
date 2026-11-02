import { useState, type ReactNode } from "react";
import {
  Calendar,
  calendarLocale,
  cn,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@notrelix/ui-web";
import { identityMarkClass } from "../../shared/item-display";
import type { MainTableCopy } from "./main-table.copy";

/** A person the table can assign an item to. */
export interface TableEditorMember {
  readonly userId: string;
  readonly name: string;
  readonly initials: string;
}

/** A priority option of the board's Priority field. */
export interface TablePriorityOption {
  readonly id: string;
  readonly label: string;
}

/**
 * What the table offers to edit in place: the assignee, the due date and the
 * priority of a row. Absent: those cells stay read-only.
 */
export interface TableEditors {
  readonly members: readonly TableEditorMember[];
  readonly priorities: readonly TablePriorityOption[];
  /** `null` = nobody. */
  readonly onAssignee: (rowId: string, userId: string | null) => void;
  /** "YYYY-MM-DD", or `null` to clear the date. */
  readonly onDue: (rowId: string, day: string | null) => void;
  readonly onPriority: (rowId: string, optionId: string) => void;
}

/** "YYYY-MM-DD" of a calendar pick, in the viewer's own day. */
export function dayOf(date: Date): string {
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  return `${date.getFullYear()}-${month}-${day}`;
}

/** The trigger look: the cell's own content, a soft hover and no shift. */
export const EDIT_TRIGGER_CLASS =
  "-mx-1.5 flex min-w-0 max-md:min-h-11 cursor-pointer items-center gap-[7px] rounded-md border-0 bg-transparent px-1.5 py-0.5 text-left font-[inherit] text-[length:inherit] text-inherit hover:bg-state-hover-strong focus-visible:outline-2 focus-visible:outline-ring";

export function AssigneeEditor({
  copy,
  members,
  currentId,
  label,
  onPick,
  children,
}: {
  copy: MainTableCopy;
  members: readonly TableEditorMember[];
  currentId: string | null;
  label: string;
  onPick: (userId: string | null) => void;
  children: ReactNode;
}) {
  const [open, setOpen] = useState(false);
  const pick = (userId: string | null) => {
    setOpen(false);
    onPick(userId);
  };
  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <button type="button" aria-label={label} className={EDIT_TRIGGER_CLASS}>
          {children}
        </button>
      </PopoverTrigger>
      <PopoverContent
        align="start"
        aria-label={copy.row.assigneeList}
        className="w-64 gap-0 p-1.5"
      >
        <ul className="flex flex-col gap-0.5">
          <li>
            <PickRow
              pressed={currentId === null}
              onClick={() => pick(null)}
              mark={
                <span
                  aria-hidden="true"
                  className="flex size-6 items-center justify-center rounded-full text-avatar-md font-bold text-meta-foreground shadow-[inset_0_0_0_1.5px_var(--line-strong)]"
                >
                  +
                </span>
              }
            >
              {copy.row.unassigned}
            </PickRow>
          </li>
          {members.map((member) => (
            <li key={member.userId}>
              <PickRow
                pressed={currentId === member.userId}
                onClick={() => pick(member.userId)}
                mark={
                  <span
                    aria-hidden="true"
                    className={cn(
                      "flex size-6 items-center justify-center rounded-full text-avatar-md",
                      identityMarkClass(member.userId),
                    )}
                  >
                    {member.initials}
                  </span>
                }
              >
                {member.name}
              </PickRow>
            </li>
          ))}
        </ul>
      </PopoverContent>
    </Popover>
  );
}

export function PickRow({
  pressed,
  mark,
  onClick,
  children,
}: {
  pressed: boolean;
  mark?: ReactNode;
  onClick: () => void;
  children: ReactNode;
}) {
  return (
    <button
      type="button"
      aria-pressed={pressed}
      onClick={onClick}
      className={cn(
        "flex h-8 max-md:h-11 w-full cursor-pointer items-center gap-2 rounded-md border-0 bg-transparent px-2 text-left text-compact text-foreground hover:bg-state-hover-strong focus-visible:outline-2 focus-visible:outline-ring",
        pressed && "bg-state-hover font-semibold",
      )}
    >
      {mark}
      <span className="min-w-0 flex-1 truncate">{children}</span>
    </button>
  );
}

export function DueEditor({
  copy,
  dueAt,
  label,
  onPick,
  children,
}: {
  copy: MainTableCopy;
  dueAt: string | null;
  label: string;
  onPick: (day: string | null) => void;
  children: ReactNode;
}) {
  const [open, setOpen] = useState(false);
  const pick = (day: string | null) => {
    setOpen(false);
    onPick(day);
  };
  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <button type="button" aria-label={label} className={EDIT_TRIGGER_CLASS}>
          {children}
        </button>
      </PopoverTrigger>
      <PopoverContent align="start" aria-label={label} className="w-auto p-0">
        <Calendar
          mode="single"
          locale={calendarLocale(copy.row.pickerLocale)}
          selected={dueAt ? new Date(dueAt) : undefined}
          defaultMonth={dueAt ? new Date(dueAt) : undefined}
          onSelect={(date: Date | undefined) => pick(date ? dayOf(date) : null)}
        />
        {dueAt ? (
          <div className="flex justify-end border-t border-border p-2">
            <button
              type="button"
              onClick={() => pick(null)}
              className="h-8 max-md:h-11 cursor-pointer rounded-[7px] border-0 bg-transparent px-3 text-compact font-medium text-danger-text hover:bg-state-hover-strong"
            >
              {copy.row.clearDue}
            </button>
          </div>
        ) : null}
      </PopoverContent>
    </Popover>
  );
}

export function PriorityEditor({
  options,
  currentLabel,
  label,
  onPick,
  children,
}: {
  options: readonly TablePriorityOption[];
  currentLabel: string | null;
  label: string;
  onPick: (optionId: string) => void;
  children: ReactNode;
}) {
  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <button type="button" aria-label={label} className={EDIT_TRIGGER_CLASS}>
          {children}
        </button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="start" className="w-44">
        {options.map((option) => (
          <DropdownMenuItem
            key={option.id}
            className={cn(
              "max-md:min-h-11",
              option.label === currentLabel && "font-semibold",
            )}
            onSelect={() => onPick(option.id)}
          >
            {option.label}
          </DropdownMenuItem>
        ))}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
