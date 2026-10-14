import {
  useEffect,
  useMemo,
  useRef,
  useState,
  type CSSProperties,
  type PointerEvent,
} from "react";
import { cn } from "@notrelix/ui-web";
import {
  dayNumber,
  fromDayNumber,
  parseDayKey,
  type DayKey,
} from "../calendar/calendar-model";
import { timelinePanelClass } from "./timeline-chrome";
import { TimelineChart } from "./timeline-chart";
import { TimelineList } from "./timeline-list";
import { TimelineToolbar, type TimelineToolbarProps } from "./timeline-toolbar";
import {
  buildHeader,
  fill,
  itemSpan,
  layoutTimeline,
  scaleSpec,
  stepTimelineAnchor,
  type TaskLayout,
  type TimelineDrag,
  type TimelineGroup,
  type TimelineItem,
  type TimelineLink,
  type TimelineScale,
} from "./timeline-model";
import type { TimelineCopy } from "./timeline.copy";

/** What a drag or a keyboard step does to an item's days. */
export interface TimelineReschedule {
  /** `move` shifts the whole span, `resize` only the due day. */
  readonly mode: "move" | "resize";
  /** Whole days, negative for earlier. */
  readonly days: number;
}

export interface BoardTimelineSurfaceProps {
  copy: TimelineCopy;
  /** Locale of month names. */
  locale: string;
  boardTitle: string;
  scale: TimelineScale;
  onScaleChange: (scale: TimelineScale) => void;
  /** The day the window is built around (any day of the week or month). */
  anchorDay: DayKey;
  onAnchorChange: (day: DayKey) => void;
  /** Today in the view's zone, read once by the container. */
  today: DayKey;
  /** "giờ Hà Nội" or "giờ địa phương", lower case, for the legend. */
  zoneName: string;
  /** The dated items by group; items without a date are only counted. */
  groups: readonly TimelineGroup[];
  /** Finish-to-start dependencies; none exist in the board read today. */
  links: readonly TimelineLink[];
  undatedCount: number;
  undatedHref?: string;
  onOpenUndated?: () => void;
  /** False for a viewer: no drag, no keyboard move, no add. */
  canEdit: boolean;
  onOpenItem: (id: string) => void;
  onReschedule: (item: TimelineItem, change: TimelineReschedule) => void;
  onAddItem?: () => void;
  /** Starting states for stories and tests; the user's own actions replace them. */
  defaultDrag?: TimelineDrag | null;
  defaultCriticalPath?: boolean;
}

/** "d/M" of a day number (no padding, as the grid label of the design). */
function dmy(n: number): string {
  const { month, day } = parseDayKey(fromDayNumber(n));
  return `${day}/${month}`;
}

/**
 * The Timeline of a board (pure surface): the bar, the Gantt (list of items
 * on the left, bars on a day, week or month scale on the right) and the
 * legend. It owns only ephemeral interaction state (the drag in flight, the
 * hovered row, the dependency and critical-path toggles); the window, the
 * data and the writes come from the container.
 */
export function BoardTimelineSurface({
  copy,
  locale,
  boardTitle,
  scale,
  onScaleChange,
  anchorDay,
  onAnchorChange,
  today,
  zoneName,
  groups,
  links,
  undatedCount,
  undatedHref,
  onOpenUndated,
  canEdit,
  onOpenItem,
  onReschedule,
  onAddItem,
  defaultDrag = null,
  defaultCriticalPath = false,
}: BoardTimelineSurfaceProps) {
  const [drag, setDrag] = useState<TimelineDrag | null>(defaultDrag);
  const [hoverId, setHoverId] = useState<string | null>(null);
  const [linksShown, setLinksShown] = useState(true);
  const [criticalShown, setCriticalShown] = useState(defaultCriticalPath);
  const scroller = useRef<HTMLDivElement>(null);
  const press = useRef<{
    id: string;
    mode: "move" | "resize";
    startX: number;
    minDays: number;
    moved: boolean;
  } | null>(null);

  const spec = useMemo(() => scaleSpec(scale, anchorDay), [scale, anchorDay]);
  const header = useMemo(
    () => buildHeader({ scale, spec, today, copy, locale }),
    [scale, spec, today, copy, locale],
  );
  // The part of the chart in view (chart pixels), so a label that would be cut
  // by the edge of the frame moves to the other side of its bar.
  const [visible, setVisible] = useState<{
    from: number;
    to: number;
  } | null>(null);
  const hasLinks = links.length > 0;
  const hasCritical =
    hasLinks && groups.some((g) => g.items.some((i) => i.critical));
  const layout = useMemo(
    () =>
      layoutTimeline({
        visible,
        scale,
        spec,
        groups,
        links: linksShown ? links : [],
        today,
        copy,
        criticalPath: criticalShown && hasCritical,
        drag,
      }),
    [
      scale,
      spec,
      groups,
      links,
      linksShown,
      today,
      copy,
      criticalShown,
      hasCritical,
      drag,
      visible,
    ],
  );
  const rowsById = useMemo(() => {
    const map = new Map<string, TaskLayout>();
    for (const row of layout.rows)
      if (row.type === "task") map.set(row.id, row);
    return map;
  }, [layout.rows]);

  // Keep today in view: when the chart is wider than the window, centre it
  // on today after a change of scale or window and on the "Hôm nay" button.
  // Where the list is not docked (below the desktop width) it scrolls away
  // with the chart, so only the button moves it.
  const [todayToken, setTodayToken] = useState(0);
  useEffect(() => {
    const element = scroller.current;
    if (!element) return;
    const left = element.querySelector<HTMLElement>('[data-tl="left"]');
    const listWidth = left?.offsetWidth ?? 0;
    const docked = left ? getComputedStyle(left).position === "sticky" : true;
    if (!docked && todayToken === 0) return;
    // The chart's left edge and the visible part of it, in scroll coordinates.
    const edge = docked ? 0 : listWidth;
    const region = element.clientWidth - (docked ? listWidth : 0);
    const visibleFrom = element.scrollLeft - edge;
    const visibleTo = visibleFrom + region;
    if (header.todayX >= visibleFrom + 24 && header.todayX <= visibleTo - 24)
      return;
    element.scrollLeft = Math.max(0, edge + header.todayX - region / 2);
  }, [header.todayX, scale, todayToken]);

  useEffect(() => {
    const element = scroller.current;
    if (!element) return undefined;
    let frame = 0;
    const measure = () => {
      frame = 0;
      if (element.clientWidth === 0) return;
      const left = element.querySelector<HTMLElement>('[data-tl="left"]');
      const listWidth = left?.offsetWidth ?? 0;
      const docked = left ? getComputedStyle(left).position === "sticky" : true;
      const edge = docked ? 0 : listWidth;
      const from = Math.round(element.scrollLeft - edge);
      const to = Math.round(
        element.scrollLeft -
          edge +
          element.clientWidth -
          (docked ? listWidth : 0),
      );
      setVisible((existing) =>
        existing && existing.from === from && existing.to === to
          ? existing
          : { from, to },
      );
    };
    const schedule = () => {
      if (frame === 0) frame = requestAnimationFrame(measure);
    };
    measure();
    element.addEventListener("scroll", schedule, { passive: true });
    const observer =
      typeof ResizeObserver === "undefined"
        ? null
        : new ResizeObserver(schedule);
    observer?.observe(element);
    return () => {
      element.removeEventListener("scroll", schedule);
      observer?.disconnect();
      if (frame !== 0) cancelAnimationFrame(frame);
    };
  }, [scale, header.chartWidth, todayToken]);

  // Escape puts a bar back while it is dragged.
  useEffect(() => {
    if (!drag) return undefined;
    const onKey = (event: KeyboardEvent) => {
      if (event.key !== "Escape") return;
      press.current = null;
      setDrag(null);
    };
    document.addEventListener("keydown", onKey);
    return () => document.removeEventListener("keydown", onKey);
  }, [drag]);

  const onBarPointerDown = (
    row: TaskLayout,
    mode: "move" | "resize",
    event: PointerEvent<HTMLElement>,
  ) => {
    if (!canEdit || event.button !== 0) return;
    event.stopPropagation();
    event.currentTarget.setPointerCapture?.(event.pointerId);
    const span = itemSpan(row.item);
    press.current = {
      id: row.id,
      mode,
      startX: event.clientX,
      // A due day cannot go before the start day.
      minDays:
        mode === "resize" ? (span.start ?? span.end) - span.end : -Infinity,
      moved: false,
    };
  };
  const daysFor = (
    current: { startX: number; minDays: number },
    event: PointerEvent<HTMLElement>,
  ): number =>
    Math.max(
      current.minDays,
      Math.round((event.clientX - current.startX) / spec.ppd),
    );
  const onBarPointerMove = (event: PointerEvent<HTMLElement>) => {
    const current = press.current;
    if (!current) return;
    if (!current.moved && Math.abs(event.clientX - current.startX) < 4) return;
    current.moved = true;
    const days = daysFor(current, event);
    setDrag((existing) =>
      existing &&
      existing.id === current.id &&
      existing.mode === current.mode &&
      existing.days === days
        ? existing
        : { id: current.id, mode: current.mode, days },
    );
  };
  const onBarPointerUp = (event: PointerEvent<HTMLElement>) => {
    const current = press.current;
    press.current = null;
    event.currentTarget.releasePointerCapture?.(event.pointerId);
    if (!current) return;
    const days = daysFor(current, event);
    setDrag(null);
    if (!current.moved) {
      if (current.mode === "move") onOpenItem(current.id);
      return;
    }
    const row = rowsById.get(current.id);
    if (row && days !== 0) onReschedule(row.item, { mode: current.mode, days });
  };
  const onBarPointerCancel = () => {
    press.current = null;
    setDrag(null);
  };

  const toolbar: TimelineToolbarProps = {
    copy,
    scale,
    onScaleChange,
    onToday: () => {
      onAnchorChange(today);
      setTodayToken((token) => token + 1);
    },
    onStep: (direction) =>
      onAnchorChange(stepTimelineAnchor(scale, anchorDay, direction)),
    links: {
      available: hasLinks,
      shown: linksShown,
      onToggle: () => setLinksShown((shown) => !shown),
    },
    criticalPath: {
      available: hasCritical,
      shown: criticalShown,
      onToggle: () => setCriticalShown((shown) => !shown),
    },
    undatedCount,
    undatedHref,
    onOpenUndated,
    canAdd: canEdit,
    onAddItem,
  };

  const gridLabel = fill(copy.gridLabel, {
    board: boardTitle,
    from: dmy(spec.from),
    to: dmy(spec.end - 1),
    year: parseDayKey(fromDayNumber(spec.end - 1)).year,
  });
  const todayN = dayNumber(today);
  const legendSwatch = "inline-block h-2.5 w-[18px] rounded-[3px]";
  const legendInset = (color: string): CSSProperties => ({
    boxShadow: `inset 0 0 0 1px var(${color})`,
  });

  return (
    <div
      data-timeline-view=""
      className="flex h-full min-h-0 flex-col bg-background font-body"
    >
      <TimelineToolbar {...toolbar} />

      <div
        ref={scroller}
        data-tl="wrap"
        className={cn(
          "mx-4 min-h-0 flex-1 overflow-auto rounded-[12px] border border-border select-none sm:mx-6",
          timelinePanelClass,
        )}
      >
        <div
          data-tl="gantt"
          role="grid"
          aria-label={gridLabel}
          className="flex min-w-max"
        >
          <TimelineList
            copy={copy}
            rows={layout.rows}
            canEdit={canEdit}
            onOpenItem={onOpenItem}
            onShiftItem={(item, mode, days) =>
              onReschedule(item, { mode, days })
            }
          />
          <TimelineChart
            scale={scale}
            ppd={spec.ppd}
            barTop={spec.barTop}
            header={header}
            layout={layout}
            hoverId={hoverId}
            onHoverChange={setHoverId}
            dragId={drag?.id ?? null}
            canEdit={canEdit}
            onOpenItem={onOpenItem}
            onBarPointerDown={onBarPointerDown}
            onBarPointerMove={onBarPointerMove}
            onBarPointerUp={onBarPointerUp}
            onBarPointerCancel={onBarPointerCancel}
          />
        </div>
        {layout.rows.length === 0 ? (
          <div
            role="status"
            className="sticky left-0 grid max-w-[min(100%,34rem)] gap-1 px-6 py-10 leading-[1.45]"
          >
            <b className="text-sm font-semibold">{copy.empty.title}</b>
            <p className="text-compact text-meta-foreground">
              {copy.empty.description}
            </p>
          </div>
        ) : null}
      </div>

      <div
        data-tl="legendbar"
        className="flex flex-wrap items-center gap-2 px-4 py-2.5 sm:px-6"
      >
        <div
          data-tl="legend"
          className="flex flex-wrap items-center gap-3.5 text-xs leading-[1.45] text-meta-foreground"
        >
          <span className="inline-flex items-center gap-1.5">
            <span
              data-tl="sw"
              className={cn(legendSwatch, "bg-timeline-bar-todo")}
              style={legendInset("--timeline-bar-todo-border")}
            />
            {copy.legend.todo}
          </span>
          <span className="inline-flex items-center gap-1.5">
            <span
              data-tl="sw"
              className={cn(legendSwatch, "bg-timeline-bar-doing")}
            />
            {copy.legend.doing}
          </span>
          <span className="inline-flex items-center gap-1.5">
            <span
              data-tl="sw"
              className={cn(legendSwatch, "bg-timeline-bar-review")}
            />
            {copy.legend.review}
          </span>
          <span className="inline-flex items-center gap-1.5">
            <span
              data-tl="sw"
              className={cn(legendSwatch, "bg-timeline-bar-done")}
            />
            {copy.legend.done}
          </span>
          <span className="inline-flex items-center gap-1.5">
            <span
              data-tl="sw"
              className={legendSwatch}
              style={{
                backgroundImage:
                  "repeating-linear-gradient(45deg, var(--calendar-event-overdue) 0 3px, transparent 3px 6px)",
                boxShadow: "inset 0 0 0 1px var(--danger-text)",
              }}
            />
            {copy.legend.overdue}
          </span>
          <span className="inline-flex items-center gap-1.5">
            <span className="inline-block size-[9px] rotate-45 rounded-[2px] bg-timeline-milestone" />
            {copy.legend.milestone}
          </span>
          <span className="inline-flex items-center gap-1.5">
            <span className="inline-block h-[1.5px] w-[18px] bg-timeline-link" />
            {copy.legend.link}
          </span>
          <span>
            {fill(copy.legend.today, {
              date: dmy(todayN),
              zone: zoneName,
            })}
          </span>
        </div>
      </div>
    </div>
  );
}
