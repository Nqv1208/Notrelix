import { useState, type DragEvent } from "react";
import { ChevronUp, Lock } from "lucide-react";
import { Button, cn } from "@notrelix/ui-web";
import type { DayKey } from "../calendar/calendar-model";
import { fill } from "./timeline-model";
import type { TimelineCopy } from "./timeline.copy";

/** An item with no start and no due day. */
export interface TimelineUndated {
  readonly id: string;
  readonly itemKey?: string | null;
  readonly title: string;
}

export interface TimelineTrayProps {
  copy: TimelineCopy;
  items: readonly TimelineUndated[];
  open: boolean;
  onToggle: () => void;
  canEdit: boolean;
  onSchedule: (id: string, day: DayKey) => void;
  onDragStart: (item: TimelineUndated, event: DragEvent<HTMLElement>) => void;
  onDragEnd: () => void;
}

/**
 * "Chưa lên lịch": a 40px footer that never moves the chart. Open, its chips
 * float above the chart's foot as an overlay. A chip is dragged onto a day,
 * or "Đặt ngày" asks for one.
 */
export function TimelineTray({
  copy,
  items,
  open,
  onToggle,
  canEdit,
  onSchedule,
  onDragStart,
  onDragEnd,
}: TimelineTrayProps) {
  const text = copy.tray;
  const [picking, setPicking] = useState<string | null>(null);
  return (
    <div data-tl="tray" className="relative mx-4 sm:mx-6">
      {open ? (
        <div
          data-tl="tray-panel"
          role="region"
          aria-label={text.title}
          className="absolute right-0 bottom-full left-0 z-20 mb-1 grid gap-2 rounded-[12px] border border-border bg-popover p-3 shadow-popover dark:bg-card"
        >
          {!canEdit ? (
            <p className="flex items-center gap-2 rounded-[8px] bg-warning-subtle px-2.5 py-2 text-crumb leading-[1.45] text-foreground">
              <Lock className="size-3.5 flex-none" aria-hidden="true" />
              {text.readOnly}
            </p>
          ) : null}
          {items.length === 0 ? (
            <p className="text-compact text-meta-foreground">{text.empty}</p>
          ) : (
            <div className="grid gap-2 sm:grid-cols-2">
              {items.map((item) => {
                const name = item.itemKey ?? item.title;
                return (
                  <div
                    key={item.id}
                    data-tl-undated={item.id}
                    draggable={canEdit}
                    aria-label={
                      canEdit ? fill(text.dragLabel, { key: name }) : undefined
                    }
                    className={cn(
                      "flex min-w-0 items-center gap-2 rounded-[9px] border border-line-strong bg-popover px-2.5 py-1.5 text-compact dark:bg-card",
                      canEdit && "cursor-grab",
                    )}
                    onDragStart={(event) => onDragStart(item, event)}
                    onDragEnd={onDragEnd}
                  >
                    <span className="font-mono text-crumb">{item.itemKey}</span>
                    <span className="min-w-0 flex-1 truncate">
                      {item.title}
                    </span>
                    {!canEdit ? (
                      <Lock
                        className="size-3.5 flex-none text-meta-foreground"
                        aria-hidden="true"
                      />
                    ) : picking === item.id ? (
                      <input
                        type="date"
                        autoFocus
                        aria-label={fill(text.setDateLabel, { key: name })}
                        className="h-7 rounded-[6px] border border-line-strong bg-transparent px-1 text-crumb"
                        onChange={(event) => {
                          if (!event.target.value) return;
                          setPicking(null);
                          onSchedule(item.id, event.target.value as DayKey);
                        }}
                        onBlur={() => setPicking(null)}
                        onKeyDown={(event) => {
                          if (event.key === "Escape") {
                            event.stopPropagation();
                            setPicking(null);
                          }
                        }}
                      />
                    ) : (
                      <Button
                        variant="outline"
                        size="sm"
                        className="h-6.5 px-2 text-xs"
                        aria-label={fill(text.setDateLabel, { key: name })}
                        onClick={() => setPicking(item.id)}
                      >
                        {text.setDate}
                      </Button>
                    )}
                  </div>
                );
              })}
            </div>
          )}
        </div>
      ) : null}
      <div className="flex h-10 items-center">
        <button
          type="button"
          aria-expanded={open}
          aria-label={open ? text.close : text.open}
          className="inline-flex h-7 cursor-pointer items-center gap-2 rounded-[7px] px-2 text-compact font-semibold text-foreground focus-visible:outline-2 focus-visible:outline-ring"
          onClick={onToggle}
        >
          <ChevronUp
            className={cn("size-4 transition-transform", open && "rotate-180")}
            aria-hidden="true"
          />
          {text.title}
          <span className="rounded-full bg-state-hover-strong px-1.5 text-crumb">
            {items.length}
          </span>
        </button>
      </div>
    </div>
  );
}
