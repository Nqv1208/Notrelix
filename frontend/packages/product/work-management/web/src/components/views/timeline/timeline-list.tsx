import type { KeyboardEvent, MouseEvent } from "react";
import { cn } from "@notrelix/ui-web";
import { timelinePanelClass } from "./timeline-chrome";
import {
  fill,
  type RowLayout,
  type TaskLayout,
  type TimelineItem,
} from "./timeline-model";
import type { TimelineCopy } from "./timeline.copy";

export interface TimelineListProps {
  copy: TimelineCopy;
  rows: readonly RowLayout[];
  canEdit: boolean;
  onOpenItem: (id: string) => void;
  /** Alt and the arrow keys: move the bar a day, with Shift change its due day. */
  onShiftItem: (
    item: TimelineItem,
    mode: "move" | "resize",
    days: number,
  ) => void;
}

/**
 * The left column of the Gantt: the sticky header (Mã, Công việc, Hạn), the
 * group rows and one row per dated item with its key, title, assignee and
 * due day. It stays in view while the chart scrolls sideways.
 */
export function TimelineList({
  copy,
  rows,
  canEdit,
  onOpenItem,
  onShiftItem,
}: TimelineListProps) {
  return (
    <div
      data-tl="left"
      className={cn(
        "z-[6] box-content w-72 shrink-0 border-r border-line-strong lg:sticky lg:left-0 lg:w-95",
        timelinePanelClass,
      )}
    >
      <div
        data-tl="lh"
        className="sticky top-0 z-[2] box-border flex h-14 items-end gap-1.5 border-b border-line-strong bg-table-header px-3 pb-2 text-xs leading-[1.45] font-semibold text-meta-foreground"
      >
        <span className="w-[62px]">{copy.head.key}</span>
        <span className="flex-1">{copy.head.title}</span>
        <span>{copy.head.due}</span>
      </div>
      {rows.map((row) =>
        row.type === "group" ? (
          <div
            key={`group-${row.id}`}
            role="row"
            data-tl="lgrp"
            className="box-border flex h-8 items-center gap-2 border-b border-border bg-table-group px-3 text-compact leading-[1.45] font-semibold"
          >
            {row.title}
            <span className="font-medium text-meta-foreground">
              {row.count}
            </span>
          </div>
        ) : (
          <TaskRow
            key={row.id}
            copy={copy}
            row={row}
            canEdit={canEdit}
            onOpenItem={onOpenItem}
            onShiftItem={onShiftItem}
          />
        ),
      )}
    </div>
  );
}

function TaskRow({
  copy,
  row,
  canEdit,
  onOpenItem,
  onShiftItem,
}: {
  copy: TimelineCopy;
  row: TaskLayout;
  canEdit: boolean;
  onOpenItem: (id: string) => void;
  onShiftItem: TimelineListProps["onShiftItem"];
}) {
  const { item } = row;
  const open = (event: MouseEvent<HTMLAnchorElement>) => {
    // A modified click keeps its own meaning (new tab, new window).
    if (event.metaKey || event.ctrlKey || event.shiftKey || event.button !== 0)
      return;
    event.preventDefault();
    onOpenItem(item.id);
  };
  const movable = canEdit && item.movable !== false && row.bar !== null;
  const onKeyDown = (event: KeyboardEvent<HTMLAnchorElement>) => {
    if (!movable || !event.altKey) return;
    const days =
      event.key === "ArrowRight" ? 1 : event.key === "ArrowLeft" ? -1 : 0;
    if (days === 0) return;
    event.preventDefault();
    onShiftItem(item, event.shiftKey ? "resize" : "move", days);
  };
  const keyLabel = item.itemKey ?? item.title;
  const linkProps = {
    href: item.href ?? "#",
    onClick: open,
    onKeyDown,
    "aria-keyshortcuts": movable ? "Alt+ArrowLeft Alt+ArrowRight" : undefined,
    "aria-description": movable ? copy.keyboard.reschedule : undefined,
  };
  return (
    <div
      role="row"
      data-tl="lrow"
      data-row-id={item.id}
      className="box-border grid h-9 grid-cols-[62px_minmax(0,1fr)_24px_50px] items-center gap-1.5 border-b border-border px-3 text-compact leading-[1.45]"
    >
      {row.hasKey ? (
        <>
          <span role="cell">
            <a
              {...linkProps}
              aria-label={fill(copy.rowLabel, {
                key: keyLabel,
                title: item.title,
              })}
              className="font-mono text-micro leading-[1.45] text-meta-foreground no-underline"
            >
              {item.itemKey}
            </a>
          </span>
          <span role="cell" className="min-w-0 truncate font-medium">
            <a
              {...linkProps}
              title={item.title}
              className="no-underline hover:underline"
            >
              {item.title}
            </a>
          </span>
        </>
      ) : (
        <>
          <span role="cell">
            <span
              data-tl="ph"
              className="rounded-[4px] border border-line-strong bg-table-group px-1 text-rail-label leading-4 font-semibold whitespace-nowrap text-muted-foreground"
            >
              {row.tag}
            </span>
          </span>
          <span
            role="cell"
            title={item.title}
            className="min-w-0 truncate font-medium"
          >
            {item.title}
          </span>
        </>
      )}
      <span role="cell">
        {item.assignee ? (
          <span
            title={item.assignee.name}
            className={cn(
              "grid size-5.5 shrink-0 place-items-center rounded-full text-avatar-sm font-bold",
              item.assignee.markClass,
            )}
          >
            {item.assignee.initials}
          </span>
        ) : null}
      </span>
      <span
        role="cell"
        className={cn(
          "text-right text-xs leading-[1.45] text-meta-foreground tabular-nums",
          row.dateState === "over" && "font-semibold text-danger-text",
          row.dateState === "moved" &&
            "font-semibold text-state-selected-foreground",
        )}
      >
        {row.dates}
      </span>
    </div>
  );
}
