import type { DragEvent } from "react";
import { Button, cn } from "@notrelix/ui-web";
import type { CalendarCopy } from "./calendar.copy";
import { calendarOutlineButtonClass } from "./calendar-chrome";
import type { CalendarUndated } from "./calendar-model";

function fill(
  message: string,
  values: Readonly<Record<string, string | number>>,
) {
  return message.replace(/\{\{(\w+)\}\}/g, (match, key: string) =>
    key in values ? String(values[key]) : match,
  );
}

const BOX =
  "flex flex-col gap-2 rounded-[12px] border border-border bg-popover p-3 dark:bg-card";
const BOX_TITLE =
  "flex items-center gap-2 text-nav leading-[1.45] font-[650] text-foreground";

/** Six dots: the drag grip of an undated item. */
function Grip() {
  return (
    <svg
      viewBox="0 0 24 24"
      aria-hidden="true"
      className="mt-px size-4 flex-none fill-none stroke-current stroke-[1.8] text-calendar-faint [stroke-linecap:round] [stroke-linejoin:round]"
    >
      <circle cx="9" cy="6" r="1" />
      <circle cx="15" cy="6" r="1" />
      <circle cx="9" cy="12" r="1" />
      <circle cx="15" cy="12" r="1" />
      <circle cx="9" cy="18" r="1" />
      <circle cx="15" cy="18" r="1" />
    </svg>
  );
}

const LEGEND = [
  {
    key: "todo",
    swatch:
      "bg-calendar-event-todo shadow-[inset_0_0_0_1px_var(--line-strong)]",
  },
  { key: "doing", swatch: "bg-calendar-event-doing" },
  { key: "review", swatch: "bg-calendar-event-review" },
  { key: "done", swatch: "bg-calendar-event-done" },
  { key: "overdue", swatch: "bg-calendar-event-overdue" },
] as const;

export interface CalendarAsideProps {
  copy: CalendarCopy;
  /** Every undated item of the board, before the filter. */
  undatedTotal: number;
  /** The undated items the filter leaves. */
  undated: readonly CalendarUndated[];
  filterActive: boolean;
  canEdit: boolean;
  onSetDue: (id: string) => void;
  onDragStart: (item: CalendarUndated, event: DragEvent<HTMLElement>) => void;
  onDragEnd: () => void;
}

/**
 * The column beside the calendar: the items that have no date yet (drag one
 * into a day, or "Đặt hạn") and the colour legend.
 */
export function CalendarAside({
  copy,
  undatedTotal,
  undated,
  filterActive,
  canEdit,
  onSetDue,
  onDragStart,
  onDragEnd,
}: CalendarAsideProps) {
  const hidden = undatedTotal - undated.length;
  const title =
    undatedTotal === 0
      ? copy.undated.none
      : fill(
          undatedTotal === 1
            ? copy.undated.title_one
            : copy.undated.title_other,
          { count: undatedTotal },
        );
  return (
    <aside
      aria-label={copy.undated.region}
      className="flex w-full flex-none flex-col gap-2.5 lg:w-68"
    >
      <div className={BOX}>
        <h2 className={BOX_TITLE}>{title}</h2>
        {undated.length > 0 ? (
          <>
            {canEdit ? (
              <p className="text-crumb leading-[1.45] text-meta-foreground">
                {copy.undated.hint}
              </p>
            ) : null}
            {undated.map((item) => {
              const name = item.itemKey ?? item.title;
              return (
                <div
                  key={item.id}
                  data-calendar-undated={item.id}
                  draggable={canEdit}
                  aria-label={fill(copy.undated.dragLabel, { key: name })}
                  className={cn(
                    "flex items-start gap-2 rounded-[9px] border border-line-strong bg-popover px-2.5 py-[9px] hover:border-meta-foreground dark:bg-card",
                    canEdit && "cursor-grab",
                  )}
                  onDragStart={(event) => onDragStart(item, event)}
                  onDragEnd={onDragEnd}
                >
                  <Grip />
                  <div className="min-w-0">
                    <b className="block text-compact leading-[1.4] font-medium">
                      {item.title}
                    </b>
                    <small className="mt-[3px] flex items-center gap-1.5 text-micro leading-[1.45] text-meta-foreground">
                      {item.itemKey ? (
                        <span className="font-mono">{item.itemKey}</span>
                      ) : null}
                      {item.assigneeName ?? copy.undated.unassigned}
                    </small>
                  </div>
                  {canEdit ? (
                    <Button
                      variant="outline"
                      size="sm"
                      className={cn(
                        calendarOutlineButtonClass,
                        // The mockup lets the button shrink, so "Đặt hạn" wraps.
                        "ml-auto h-6.5 shrink self-center px-2 text-xs whitespace-normal",
                      )}
                      aria-label={fill(copy.undated.setDueLabel, {
                        key: name,
                      })}
                      onClick={() => onSetDue(item.id)}
                    >
                      {copy.undated.setDue}
                    </Button>
                  ) : null}
                </div>
              );
            })}
          </>
        ) : null}
        {filterActive && hidden > 0 ? (
          <p className="text-crumb leading-[1.45] text-meta-foreground">
            {hidden === 1
              ? copy.undated.hiddenByFilter_one
              : copy.undated.hiddenByFilter_other}
          </p>
        ) : null}
      </div>

      <div className={BOX}>
        <h2 className={BOX_TITLE}>{copy.legend.title}</h2>
        <div className="flex flex-col gap-1.5 text-crumb leading-[1.45] text-muted-foreground">
          {LEGEND.map((entry) => (
            <span key={entry.key} className="flex items-center gap-2">
              <span
                aria-hidden="true"
                className={cn(
                  "h-3 w-5.5 flex-none rounded-[4px]",
                  entry.swatch,
                )}
              />
              {copy.legend[entry.key]}
            </span>
          ))}
          <span className="flex items-center gap-2">
            <span
              aria-hidden="true"
              className="flex h-3 w-5.5 flex-none items-center gap-2 rounded-[4px] shadow-[inset_0_0_0_1px_var(--line-strong)]"
            >
              <span className="block size-1.5 rotate-45 rounded-[1px] bg-mark-crimson" />
            </span>
            {copy.legend.milestone}
          </span>
        </div>
      </div>
    </aside>
  );
}
