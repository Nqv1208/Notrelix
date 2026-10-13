import { useEffect, useRef, useState } from "react";
import { ChevronDown, Plus } from "lucide-react";
import { cn } from "@notrelix/ui-web";
import {
  fillTableCopy,
  pluralTableCopy,
  type MainTableCopy,
} from "./main-table.copy";
import { tablePanelClass } from "./table-chrome";
import type { TableGroupModel } from "./table-model";
import { StatusDot } from "./table-parts";

/** The group header: toggle, status dot, name, count and the overdue note. */
export function TableGroupHeader({
  copy,
  group,
  collapsed,
  onToggle,
}: {
  copy: MainTableCopy;
  group: TableGroupModel;
  collapsed: boolean;
  onToggle: () => void;
}) {
  return (
    <div
      role="row"
      className="flex h-9.75 items-center border-b border-line-strong bg-table-group"
    >
      <span
        role="cell"
        className="sticky left-0 inline-flex items-center gap-2 pl-2.5 text-nav font-semibold text-foreground"
      >
        <button
          type="button"
          aria-expanded={!collapsed}
          aria-label={fillTableCopy(
            collapsed ? copy.group.expand : copy.group.collapse,
            { name: group.name },
          )}
          onClick={onToggle}
          className="grid size-6 cursor-pointer place-items-center rounded-[5px] border-0 bg-transparent p-0 text-meta-foreground focus-visible:outline-2 focus-visible:outline-ring"
        >
          <ChevronDown
            className={cn("size-3.5", collapsed && "-rotate-90")}
            aria-hidden="true"
          />
        </button>
        <StatusDot category={group.category} />
        {group.name}
        <span className="font-medium text-meta-foreground">
          {group.rows.length}
        </span>
        {group.overdueCount > 0 ? (
          <span className="text-crumb font-semibold text-danger-text">
            ·{" "}
            {pluralTableCopy(
              {
                one: copy.group.overdue_one,
                other: copy.group.overdue_other,
              },
              group.overdueCount,
            )}
          </span>
        ) : null}
      </span>
    </div>
  );
}

/**
 * "Thêm vào {group}": a row under the group that turns into a title field.
 * Enter adds the item and keeps the field for the next one; Escape or
 * leaving an empty field closes it.
 */
export function TableAddRow({
  copy,
  group,
  openRequest = 0,
  onCreate,
}: {
  copy: MainTableCopy;
  group: TableGroupModel;
  /** Changing this number opens the field (the toolbar's "Thêm công việc"). */
  openRequest?: number;
  onCreate: (title: string) => void;
}) {
  const [open, setOpen] = useState(false);
  useEffect(() => {
    if (openRequest > 0) setOpen(true);
  }, [openRequest]);
  const [title, setTitle] = useState("");
  const inputRef = useRef<HTMLInputElement>(null);
  useEffect(() => {
    if (open) inputRef.current?.focus();
  }, [open]);

  const submit = () => {
    const trimmed = title.trim();
    if (!trimmed) return;
    onCreate(trimmed);
    setTitle("");
  };

  return (
    <div
      role="row"
      className={cn(
        "flex h-8.75 items-center border-b border-border text-compact text-meta-foreground",
        tablePanelClass,
      )}
    >
      <span
        role="cell"
        className="sticky left-0 inline-flex items-center gap-1.5 pl-13.5 leading-[1.45]"
      >
        {open ? (
          <form
            className="inline-flex items-center gap-1.5"
            onSubmit={(event) => {
              event.preventDefault();
              submit();
            }}
          >
            <Plus className="size-3.5" aria-hidden="true" />
            <input
              ref={inputRef}
              value={title}
              onChange={(event) => setTitle(event.target.value)}
              aria-label={fillTableCopy(copy.group.newItemLabel, {
                name: group.name,
              })}
              placeholder={copy.group.newItemPlaceholder}
              onBlur={() => {
                if (title.trim() === "") setOpen(false);
              }}
              onKeyDown={(event) => {
                if (event.key === "Escape") {
                  event.preventDefault();
                  setTitle("");
                  setOpen(false);
                }
              }}
              className="h-6.5 w-72 rounded-[7px] border border-line-strong bg-popover px-2 text-compact text-foreground outline-none placeholder:text-meta-foreground focus-visible:ring-2 focus-visible:ring-focus-ring dark:bg-card"
            />
          </form>
        ) : (
          <button
            type="button"
            onClick={() => setOpen(true)}
            className="inline-flex cursor-pointer items-center gap-1.5 border-0 bg-transparent p-0 text-compact leading-[1.45] text-meta-foreground hover:text-foreground focus-visible:outline-2 focus-visible:outline-ring"
          >
            <Plus className="size-3.5" aria-hidden="true" />
            {fillTableCopy(copy.group.addTo, { name: group.name })}
          </button>
        )}
      </span>
    </div>
  );
}
