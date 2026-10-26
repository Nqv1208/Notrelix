import { useEffect, useMemo, useRef, useState, type DragEvent } from "react";
import { Button } from "@notrelix/ui-web";
import { calendarOutlineButtonClass } from "./calendar-chrome";
import {
  emptyKanbanFilter,
  type KanbanFilterState,
} from "../kanban/kanban-filter";
import type { KanbanToolbarFilterOptions } from "../kanban/kanban-filter-menu";
import { CalendarAgenda } from "./calendar-agenda";
import { CalendarAside } from "./calendar-aside";
import { calendarGridLabel, calendarHeading } from "./calendar-format";
import {
  CalendarGrid,
  QuickCreate,
  type CalendarDragState,
} from "./calendar-grid";
import {
  addDays,
  monthWeeks,
  stepAnchor,
  weekDays,
  type CalendarDateBasis,
  type CalendarEntry,
  type CalendarRange,
  type CalendarUndated,
  type DayKey,
} from "./calendar-model";
import { CalendarToolbar } from "./calendar-toolbar";
import type { CalendarCopy } from "./calendar.copy";

function fill(
  message: string,
  values: Readonly<Record<string, string | number>>,
) {
  return message.replace(/\{\{(\w+)\}\}/g, (match, key: string) =>
    key in values ? String(values[key]) : match,
  );
}

export interface BoardCalendarSurfaceProps {
  copy: CalendarCopy;
  /** Locale of month and weekday names. */
  locale: string;
  range: CalendarRange;
  onRangeChange: (range: CalendarRange) => void;
  /** The day the view is centred on (any day of the month or week shown). */
  anchorDay: DayKey;
  onAnchorChange: (day: DayKey) => void;
  /** Today and the minutes of the day now, read once by the container. */
  today: DayKey;
  nowMinutes: number;
  /** "Giờ Hà Nội (GMT+7)" and its zone name ("Giờ Hà Nội"). */
  zoneLabel: string;
  zoneName: string;
  dateBasis: CalendarDateBasis;
  onDateBasisChange: (basis: CalendarDateBasis) => void;
  /** The dated entries the filter leaves. */
  entries: readonly CalendarEntry[];
  undatedTotal: number;
  undated: readonly CalendarUndated[];
  filter: KanbanFilterState;
  filterActive: boolean;
  filterOptions: KanbanToolbarFilterOptions;
  onFilterChange: (filter: KanbanFilterState) => void;
  /** Items the filter hides; with `entries` empty it shows "no results". */
  hiddenCount: number;
  /** False for a viewer: no add, no drag, no quick create. */
  canEdit: boolean;
  onOpenItem: (id: string) => void;
  /** An item was dropped on, or moved to, `toDay` (its due day). */
  onReschedule: (entry: CalendarEntry, toDay: DayKey) => void;
  /** An undated item got `day` as its due day. */
  onSetDue: (id: string, day: DayKey) => void;
  onCreateItem: (title: string, day: DayKey) => void;
  /**
   * The toolbar's "Add item" opens the full form with this day prefilled;
   * without it the toolbar adds inline on that day.
   */
  onAddItem?: (day: DayKey) => void;
  /** Starting states for stories and tests; the user's own actions replace them. */
  defaultDrag?: CalendarDragState | null;
  defaultQuickCreateDay?: DayKey | null;
}

/**
 * The Calendar of a board (pure surface): the bar, the month/week grid or the
 * day agenda, and the column of undated items and legend. It owns only
 * ephemeral interaction state (the drag in flight, the open quick-create);
 * navigation, data and writes come from the container.
 */
export function BoardCalendarSurface({
  copy,
  locale,
  range,
  onRangeChange,
  anchorDay,
  onAnchorChange,
  today,
  nowMinutes,
  zoneLabel,
  zoneName,
  dateBasis,
  onDateBasisChange,
  entries,
  undatedTotal,
  undated,
  filter,
  filterActive,
  filterOptions,
  onFilterChange,
  hiddenCount,
  canEdit,
  onOpenItem,
  onReschedule,
  onSetDue,
  onCreateItem,
  onAddItem,
  defaultDrag = null,
  defaultQuickCreateDay = null,
}: BoardCalendarSurfaceProps) {
  const [drag, setDrag] = useState<CalendarDragState | null>(defaultDrag);
  const [quickCreateDay, setQuickCreateDay] = useState<DayKey | null>(
    defaultQuickCreateDay,
  );
  const [focusDay, setFocusDay] = useState<DayKey | null>(null);
  const root = useRef<HTMLDivElement>(null);

  const shownEntries = useMemo(
    () =>
      dateBasis === "due"
        ? entries.map((entry) => ({ ...entry, startDay: entry.endDay }))
        : entries,
    [entries, dateBasis],
  );
  const byId = useMemo(
    () => new Map(shownEntries.map((entry) => [entry.id, entry])),
    [shownEntries],
  );

  useEffect(() => {
    if (!focusDay) return;
    root.current
      ?.querySelector<HTMLElement>(`[data-day="${focusDay}"]`)
      ?.focus();
    setFocusDay(null);
  }, [focusDay, anchorDay, range]);

  const weeks =
    range === "month" ? monthWeeks(anchorDay) : [weekDays(anchorDay)];
  const noResults = filterActive && entries.length === 0 && hiddenCount > 0;

  const endDrag = () => setDrag(null);
  const dropOn = (day: DayKey) => {
    const current = drag;
    setDrag(null);
    if (!current || !canEdit) return;
    if (current.source === "undated") {
      onSetDue(current.id, day);
      return;
    }
    const entry = byId.get(current.id);
    if (entry && day !== entry.endDay) onReschedule(entry, day);
  };

  const startEntryDrag = (
    entry: CalendarEntry,
    event: DragEvent<HTMLElement>,
  ) => {
    event.dataTransfer.effectAllowed = "move";
    event.dataTransfer.setData("text/plain", entry.id);
    setQuickCreateDay(null);
    setDrag({
      source: "entry",
      id: entry.id,
      text: entry.itemKey ? `${entry.itemKey} ${entry.title}` : entry.title,
      assignee: entry.assignee,
      fromDay: entry.endDay,
      overDay: null,
    });
  };
  const startUndatedDrag = (
    item: CalendarUndated,
    event: DragEvent<HTMLElement>,
  ) => {
    event.dataTransfer.effectAllowed = "move";
    event.dataTransfer.setData("text/plain", item.id);
    setQuickCreateDay(null);
    setDrag({
      source: "undated",
      id: item.id,
      text: item.itemKey ? `${item.itemKey} ${item.title}` : item.title,
      overDay: null,
    });
  };

  const addItem = () => {
    if (onAddItem) {
      onAddItem(anchorDay);
      return;
    }
    setQuickCreateDay(anchorDay);
  };
  const submitQuickCreate = (title: string) => {
    if (!quickCreateDay) return;
    onCreateItem(title, quickCreateDay);
    setQuickCreateDay(null);
  };

  const heading = calendarHeading(range, anchorDay, copy, locale);
  const gridLabel = calendarGridLabel(
    range,
    anchorDay,
    copy,
    locale,
    zoneName.toLocaleLowerCase(locale),
  );

  return (
    <div
      ref={root}
      data-calendar-view=""
      className="flex h-full min-h-0 flex-col bg-background font-body select-none"
    >
      <CalendarToolbar
        copy={copy}
        heading={heading}
        range={range}
        onRangeChange={onRangeChange}
        onToday={() => onAnchorChange(today)}
        onStep={(direction) =>
          onAnchorChange(stepAnchor(range, anchorDay, direction))
        }
        dateBasis={dateBasis}
        onDateBasisChange={onDateBasisChange}
        filter={filter}
        filterOptions={filterOptions}
        onFilterChange={onFilterChange}
        zoneLabel={zoneLabel}
        canAdd={canEdit}
        onAddItem={addItem}
      />

      <div className="flex min-h-0 flex-1 flex-col gap-4 overflow-y-auto px-4 pb-5 sm:px-6 lg:flex-row lg:overflow-visible">
        <div
          role="grid"
          aria-label={gridLabel}
          className="relative flex min-h-[520px] min-w-0 flex-1 flex-col overflow-hidden rounded-[12px] border border-border bg-popover lg:min-h-0 dark:bg-card"
        >
          {range === "day" ? (
            <>
              {quickCreateDay ? (
                <div className="border-b border-border px-3 py-2.5">
                  <QuickCreate
                    copy={copy}
                    day={quickCreateDay}
                    onSubmit={submitQuickCreate}
                    onCancel={() => setQuickCreateDay(null)}
                  />
                </div>
              ) : null}
              <CalendarAgenda
                copy={copy}
                day={anchorDay}
                today={today}
                nowMinutes={nowMinutes}
                entries={shownEntries}
                onOpenItem={onOpenItem}
              />
            </>
          ) : (
            <CalendarGrid
              copy={copy}
              range={range}
              weeks={weeks}
              monthOf={anchorDay}
              anchorDay={anchorDay}
              today={today}
              entries={shownEntries}
              canEdit={canEdit}
              drag={drag}
              quickCreateDay={quickCreateDay}
              onOpenItem={onOpenItem}
              onShift={(entry, days) =>
                onReschedule(entry, addDays(entry.endDay, days))
              }
              onDragStartEntry={startEntryDrag}
              onDragEnd={endDrag}
              onDragOverDay={(day) =>
                setDrag((current) =>
                  current && current.overDay !== day
                    ? { ...current, overDay: day }
                    : current,
                )
              }
              onDropDay={dropOn}
              onRequestQuickCreate={setQuickCreateDay}
              onSubmitQuickCreate={submitQuickCreate}
              onCancelQuickCreate={() => setQuickCreateDay(null)}
              onOpenDay={(day) => {
                onAnchorChange(day);
                onRangeChange("day");
              }}
              onMoveFocus={(day) => {
                onAnchorChange(day);
                setFocusDay(day);
              }}
            />
          )}
          {noResults ? (
            <div className="pointer-events-none absolute inset-x-0 top-8 bottom-0 z-20 grid place-items-center">
              <div
                role="status"
                className="pointer-events-auto box-content flex max-w-85 flex-col items-center gap-2.5 rounded-[12px] border border-line-strong bg-popover px-5 py-4 text-center shadow-popover dark:bg-card"
              >
                <b className="text-sm leading-[1.45] font-semibold">
                  {copy.noResults.title}
                </b>
                <p className="text-compact leading-[1.45] text-meta-foreground">
                  {fill(copy.noResults.description, {
                    range: copy.noResults.range[range],
                  })}
                </p>
                <Button
                  variant="outline"
                  size="sm"
                  className={calendarOutlineButtonClass}
                  onClick={() => onFilterChange(emptyKanbanFilter())}
                >
                  {copy.noResults.clear}
                </Button>
              </div>
            </div>
          ) : null}
        </div>

        <CalendarAside
          copy={copy}
          undatedTotal={undatedTotal}
          undated={undated}
          filterActive={filterActive}
          canEdit={canEdit}
          onSetDue={(id) => onSetDue(id, anchorDay)}
          onDragStart={startUndatedDrag}
          onDragEnd={endDrag}
        />
      </div>
    </div>
  );
}
