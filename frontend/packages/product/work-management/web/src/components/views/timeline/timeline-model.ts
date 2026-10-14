/**
 * Timeline view model: the visible window of each scale, the header (months,
 * days, weeks, month ticks, weekend bands), and the layout of every row
 * (bars, completion dots, labels, overdue hatch, milestone, drag preview) and
 * of the dependency arrows. Pure functions; the clock is always injected. A
 * day is a `DayKey`, a wall-clock day of the view's time zone; inside the
 * layout it is a day number (days since 1970-01-01, UTC), and every x
 * position goes through one `X()` function per scale, as in the design
 * (`Final-Timeline`).
 */
import {
  addMonths,
  dayNumber,
  fromDayNumber,
  parseDayKey,
  startOfWeek,
  toDayKey,
  type CalendarPerson,
  type DayKey,
} from "../calendar/calendar-model";
import type { TimelineCopy } from "./timeline.copy";

export type TimelineScale = "day" | "week" | "month";

/** The colour family of a bar: the Status bucket of its item. */
export type TimelineTone = "todo" | "doing" | "review" | "done";

/**
 * `item` is a board item. `phase` and `milestone` are the design's range and
 * point markers; the board read carries no such kind yet, so only a producer
 * that has one (stories, a later contract) emits them.
 */
export type TimelineItemKind = "item" | "phase" | "milestone";

export interface TimelineItem {
  readonly id: string;
  readonly kind: TimelineItemKind;
  /** Item key such as "ERP-142"; absent for phases and milestones. */
  readonly itemKey?: string | null;
  readonly title: string;
  readonly tone: TimelineTone;
  /** First and last day; either may be missing, both missing = undated. */
  readonly startDay: DayKey | null;
  readonly dueDay: DayKey | null;
  /** 0 to 100: done = 100, otherwise the child item progress. */
  readonly percent: number;
  readonly assignee?: CalendarPerson | null;
  /** On the critical path (drawn only when the critical path is shown). */
  readonly critical?: boolean;
  /** False when the bar cannot be rescheduled (no due day to move). */
  readonly movable?: boolean;
  /** Link target of the key and title. */
  readonly href?: string;
}

export interface TimelineGroup {
  readonly id: string;
  readonly title: string;
  readonly items: readonly TimelineItem[];
}

/** Finish-to-start: `to` waits for `from`. */
export interface TimelineLink {
  readonly from: string;
  readonly to: string;
}

/** A bar being resized (its due day) or moved (the whole span), `days` from where it was. */
export interface TimelineDrag {
  readonly id: string;
  readonly mode: "resize" | "move";
  readonly days: number;
}

export const GROUP_ROW_HEIGHT = 32;
export const TASK_ROW_HEIGHT = 36;
/** Width of one character of a 12px label, to size a label before it is drawn. */
const LABEL_CHAR = 6.6;
/** Gap kept between chart text and the edge of what is in view (the list divider on the left). */
const VIEW_PAD_LEFT = 12;
const VIEW_PAD_RIGHT = 4;
/** Narrowest room worth a truncated label: about "Abcd…". */
const MIN_TEXT_ROOM = 56;

export function fill(
  message: string,
  values: Readonly<Record<string, string | number>>,
): string {
  return message.replace(/\{\{(\w+)\}\}/g, (match, key: string) =>
    key in values ? String(values[key]) : match,
  );
}

/** `one` for 1, `other` otherwise. */
export function plural(one: string, other: string, count: number): string {
  return count === 1 ? one : other;
}

// ─── Window ────────────────────────────────────────────────────────────────

export interface ScaleSpec {
  /** First day (day number) and number of days of the chart. */
  readonly from: number;
  readonly days: number;
  readonly end: number;
  /** Pixels per day. */
  readonly ppd: number;
  /** Top of a bar in its row. */
  readonly barTop: number;
}

const PPD: Record<TimelineScale, number> = { day: 26, week: 19, month: 6 };
const BAR_TOP: Record<TimelineScale, number> = { day: 8, week: 8, month: 10 };

/**
 * The window around `anchor`: Day shows four weeks and a day from the Monday
 * before last, Week five weeks and four days from two Mondays back, Month
 * four months from the first of the previous one.
 */
export function scaleSpec(scale: TimelineScale, anchor: DayKey): ScaleSpec {
  let from: number;
  let days: number;
  if (scale === "month") {
    const start = addMonths(
      toDayKey(parseDayKey(anchor).year, parseDayKey(anchor).month, 1),
      -1,
    );
    from = dayNumber(start);
    days = dayNumber(addMonths(start, 4)) - from;
  } else if (scale === "week") {
    from = dayNumber(startOfWeek(anchor)) - 14;
    days = 39;
  } else {
    from = dayNumber(startOfWeek(anchor)) - 7;
    days = 29;
  }
  return {
    from,
    days,
    end: from + days,
    ppd: PPD[scale],
    barTop: BAR_TOP[scale],
  };
}

/** The anchor one step back or forward: a week, two weeks or a month. */
export function stepTimelineAnchor(
  scale: TimelineScale,
  anchor: DayKey,
  direction: -1 | 1,
): DayKey {
  if (scale === "month") return addMonths(anchor, direction);
  return fromDayNumber(
    dayNumber(anchor) + direction * (scale === "week" ? 14 : 7),
  );
}

// ─── Dates ─────────────────────────────────────────────────────────────────

function pad(value: number): string {
  return value < 10 ? `0${value}` : String(value);
}

function parts(n: number): { year: number; month: number; day: number } {
  return parseDayKey(fromDayNumber(n));
}

/** "dd/MM" of a day number. */
export function fmtDay(n: number): string {
  const { month, day } = parts(n);
  return `${pad(day)}/${pad(month)}`;
}

function weekdayOf(n: number): number {
  return (new Date(n * 86_400_000).getUTCDay() + 6) % 7;
}

/** ISO 8601 week number. */
function isoWeek(n: number): number {
  const thursday = n - weekdayOf(n) + 3;
  const jan1 = dayNumber(toDayKey(parts(thursday).year, 1, 1));
  return Math.floor((thursday - jan1) / 7) + 1;
}

function rangeOf(a: number, b: number): string {
  const first = parts(a);
  const last = parts(b);
  return `${first.month === last.month ? pad(first.day) : fmtDay(a)}–${fmtDay(b)}`;
}

function monthLabel(
  month: number,
  year: number,
  width: number,
  yearShown: boolean,
  copy: TimelineCopy,
  locale: string,
): { text: string; yearShown: boolean } {
  const date = new Date(Date.UTC(year, month - 1, 1));
  const name = new Intl.DateTimeFormat(locale, {
    month: "long",
    timeZone: "UTC",
  }).format(date);
  const short = new Intl.DateTimeFormat(locale, {
    month: "short",
    timeZone: "UTC",
  }).format(date);
  const values = {
    month,
    name: name.charAt(0).toLocaleUpperCase(locale) + name.slice(1),
    short: short.charAt(0).toLocaleUpperCase(locale) + short.slice(1),
  };
  let text = fill(width >= 70 ? copy.month.long : copy.month.short, values);
  let shown = yearShown;
  if (!yearShown && width >= 110) {
    text += fill(copy.month.year, { year });
    shown = true;
  }
  return { text, yearShown: shown };
}

// ─── Header ────────────────────────────────────────────────────────────────

export interface TimelineHeader {
  readonly chartWidth: number;
  readonly months: readonly {
    readonly width: number;
    readonly label: string;
    readonly separator: boolean;
  }[];
  readonly days: readonly {
    readonly n: number;
    readonly weekday: string;
    readonly weekend: boolean;
    readonly today: boolean;
  }[];
  readonly weeks: readonly {
    readonly width: number;
    readonly label: string;
    readonly full: string;
    readonly separator: boolean;
    readonly current: boolean;
  }[];
  readonly ticks: readonly {
    readonly x: number;
    readonly n: number;
    readonly full: string;
    readonly today: boolean;
  }[];
  readonly bands: readonly {
    readonly x: number;
    readonly width: number;
    readonly separator: boolean;
  }[];
  readonly todayX: number;
}

const WEEKDAY_KEYS = ["mon", "tue", "wed", "thu", "fri", "sat", "sun"] as const;

export function buildHeader({
  scale,
  spec,
  today,
  copy,
  locale,
}: {
  scale: TimelineScale;
  spec: ScaleSpec;
  today: DayKey;
  copy: TimelineCopy;
  locale: string;
}): TimelineHeader {
  const { from, end, ppd: P } = spec;
  const todayN = dayNumber(today);
  const X = (n: number) => (n - from) * P;
  const months: Array<TimelineHeader["months"][number]> = [];
  let yearShown = false;
  for (let n = from; n < end;) {
    const { month, year } = parts(n);
    const nextMonth = addMonths(toDayKey(year, month, 1), 1);
    const stop = Math.min(end, dayNumber(nextMonth));
    const width = (stop - n) * P;
    const label = monthLabel(month, year, width, yearShown, copy, locale);
    yearShown = label.yearShown;
    months.push({ width, label: label.text, separator: months.length > 0 });
    n = stop;
  }
  const days: Array<TimelineHeader["days"][number]> = [];
  const weeks: Array<TimelineHeader["weeks"][number]> = [];
  const ticks: Array<TimelineHeader["ticks"][number]> = [];
  const bands: Array<TimelineHeader["bands"][number]> = [];
  for (let n = from; n < end; n++) {
    const wd = weekdayOf(n);
    const { day } = parts(n);
    if (scale === "day") {
      days.push({
        n: day,
        weekday: copy.weekdays[WEEKDAY_KEYS[wd] ?? "mon"],
        weekend: wd >= 5,
        today: n === todayN,
      });
    }
    if (scale !== "month" && wd >= 5)
      bands.push({ x: X(n), width: P, separator: false });
    if (scale === "month" && n > from && day === 1)
      bands.push({ x: X(n), width: 1, separator: true });
    if (wd === 0 || n === from) {
      const monday = n - wd;
      const full = fill(copy.week.full, {
        week: isoWeek(n),
        range: rangeOf(monday, monday + 6),
      });
      if (scale === "week") {
        const width = (Math.min(end, monday + 7) - n) * P;
        weeks.push({
          width,
          full,
          label:
            width >= 120 ? full : fill(copy.week.short, { week: isoWeek(n) }),
          separator: weeks.length > 0,
          current: todayN >= n && todayN < monday + 7,
        });
      }
      if (scale === "month" && wd === 0) {
        ticks.push({ x: X(n), n: day, full, today: n === todayN });
      }
    }
  }
  return {
    chartWidth: spec.days * P,
    months,
    days,
    weeks,
    ticks,
    bands,
    todayX: Math.round(X(todayN) + P * 0.45),
  };
}

// ─── Rows ──────────────────────────────────────────────────────────────────

export interface BarLayout {
  readonly x: number;
  readonly width: number;
  readonly tone: TimelineTone;
  readonly clipStart: boolean;
  readonly clipEnd: boolean;
  readonly fade: boolean;
  readonly critical: boolean;
  readonly percent: number;
  /** Title (or key) inside the bar. */
  readonly label: string;
  readonly mono: boolean;
  /** Pixels the label moves right to stay in view when the bar starts left of it. */
  readonly labelShift: number;
  /** Room for the label inside the bar's visible part (the label truncates to it). */
  readonly labelMax: number;
}

export interface LabelLayout {
  readonly text: string;
  readonly left?: number;
  readonly right?: number;
  readonly maxWidth: number;
}

export interface TaskLayout {
  readonly type: "task";
  readonly item: TimelineItem;
  readonly id: string;
  readonly hasKey: boolean;
  /** "Giai đoạn" / "Mốc" tag of a phase or milestone. */
  readonly tag: string;
  /** The Hạn column. */
  readonly dates: string;
  readonly dateState: "" | "over" | "moved";
  readonly top: number;
  readonly cy: number;
  readonly bar: BarLayout | null;
  readonly fin: {
    readonly x: number;
    readonly fade: boolean;
    readonly critical: boolean;
  } | null;
  readonly label: LabelLayout | null;
  readonly late: {
    readonly x: number;
    readonly width: number;
    readonly textLeft: number;
    readonly text: string;
    /** Room for the note inside the view; 0 when there is none and it is left out. */
    readonly textMax: number;
  } | null;
  readonly milestone: {
    readonly x: number;
    /** Left of the label, or `labelRight` (the gap to the chart's right edge) when it flips to the diamond's left. */
    readonly labelLeft: number | undefined;
    readonly labelRight: number | undefined;
    /** Room for the title inside the view; 0 when there is none and it is left out. */
    readonly labelMax: number;
    readonly title: string;
    readonly aria: string;
  } | null;
  readonly ghost: { readonly x: number; readonly width: number } | null;
  readonly handle: { readonly x: number } | null;
  readonly tip: {
    readonly x: number;
    readonly text: string;
    readonly note: string;
  } | null;
  readonly aria: string;
  readonly title: string;
}

export interface GroupLayout {
  readonly type: "group";
  readonly id: string;
  readonly title: string;
  readonly count: number;
  readonly top: number;
}

export type RowLayout = GroupLayout | TaskLayout;

export interface LinkSegment {
  readonly x: number;
  readonly y: number;
  readonly w: number;
  readonly h: number;
  readonly critical: boolean;
}

export interface LinkHead {
  readonly x: number;
  readonly y: number;
  readonly down: boolean;
  readonly critical: boolean;
}

export interface TimelineLayout {
  readonly rows: readonly RowLayout[];
  readonly segments: readonly LinkSegment[];
  readonly heads: readonly LinkHead[];
  readonly bodyHeight: number;
  /** Items without a date; they are not drawn. */
  readonly undated: number;
}

interface Work {
  item: TimelineItem;
  group: TimelineGroup;
  s0: number | null;
  d0: number;
  es: number | null;
  ee: number;
  isMs: boolean;
  isFin: boolean;
  off: boolean;
  late: boolean;
  lateDays: number;
  critical: boolean;
  cy: number;
  top: number;
  fx1: number;
  fc: number;
  msC: number;
  x: number;
  w: number;
  label: LabelLayout | null;
  labelLeft: number;
  inLabel: string;
  mono: boolean;
  bar: boolean;
  clipStart: boolean;
  clipEnd: boolean;
  outsideAfter: boolean;
}

export interface LayoutInput {
  scale: TimelineScale;
  spec: ScaleSpec;
  groups: readonly TimelineGroup[];
  links: readonly TimelineLink[];
  today: DayKey;
  copy: TimelineCopy;
  /** Draw the critical path: critical items and their links in danger, the rest faded. */
  criticalPath: boolean;
  drag?: TimelineDrag | null;
  /**
   * The part of the chart in view, in chart pixels; labels stay inside it.
   * The whole chart when omitted.
   */
  visible?: { readonly from: number; readonly to: number } | null;
}

/** First and last day (day numbers) of an item; `end` is null when it has no date. */
export function itemSpan(item: TimelineItem): {
  start: number | null;
  end: number;
} {
  const end = endOf(item) ?? 0;
  return { start: startOf(item, end), end };
}

function endOf(item: TimelineItem): number | null {
  const start = item.startDay ? dayNumber(item.startDay) : null;
  const due = item.dueDay ? dayNumber(item.dueDay) : null;
  if (start === null && due === null) return null;
  return Math.max(start ?? due ?? 0, due ?? start ?? 0);
}

function startOf(item: TimelineItem, end: number): number | null {
  const start = item.startDay ? dayNumber(item.startDay) : null;
  const due = item.dueDay ? dayNumber(item.dueDay) : null;
  if (item.tone === "done" && start === null) return null;
  return Math.min(start ?? end, due ?? end);
}

/** Positions every row and every dependency arrow of the window. */
export function layoutTimeline(input: LayoutInput): TimelineLayout {
  const { scale, spec, groups, links, copy, criticalPath } = input;
  const { from, end: windowEnd, ppd: P, barTop } = spec;
  const chartWidth = spec.days * P;
  const todayN = dayNumber(input.today);
  const X = (n: number) => (n - from) * P;
  const drag = input.drag ?? null;

  const works: Work[] = [];
  const byId = new Map<string, Work>();
  const rows: Array<
    | { type: "group"; group: TimelineGroup; works: Work[] }
    | { type: "task"; work: Work }
  > = [];
  let undated = 0;
  let y = 0;
  for (const group of groups) {
    const members: Work[] = [];
    for (const item of group.items) {
      const d0 = endOf(item);
      if (d0 === null) {
        undated += 1;
        continue;
      }
      const s0 = item.kind === "milestone" ? d0 : startOf(item, d0);
      const work: Work = {
        item,
        group,
        s0,
        d0,
        es: s0,
        ee: d0,
        isMs: item.kind === "milestone",
        isFin: false,
        off: false,
        late: false,
        lateDays: 0,
        critical: Boolean(item.critical),
        cy: 0,
        top: 0,
        fx1: 0,
        fc: 0,
        msC: 0,
        x: 0,
        w: 0,
        label: null,
        labelLeft: 0,
        inLabel: "",
        mono: false,
        bar: false,
        clipStart: false,
        clipEnd: false,
        outsideAfter: false,
      };
      members.push(work);
      works.push(work);
      byId.set(item.id, work);
    }
    if (members.length === 0) continue;
    rows.push({ type: "group", group, works: members });
    for (const work of members) rows.push({ type: "task", work });
  }

  // The dragged span, and the items that wait for it (they move back so they
  // still start after it ends).
  const moved = new Map<string, number>();
  if (drag) {
    const target = byId.get(drag.id);
    if (target && target.s0 !== null) {
      if (drag.mode === "move") {
        target.es = target.s0 + drag.days;
        target.ee = target.d0 + drag.days;
      } else {
        target.ee = Math.max(target.s0, target.d0 + drag.days);
      }
      moved.set(target.item.id, 0);
      const queue = [target];
      while (queue.length > 0) {
        const current = queue.shift();
        if (!current) break;
        for (const link of links) {
          if (link.from !== current.item.id) continue;
          const dependent = byId.get(link.to);
          if (
            !dependent ||
            dependent.s0 === null ||
            moved.has(dependent.item.id)
          )
            continue;
          const shift = Math.max(0, current.ee + 1 - dependent.s0);
          if (shift === 0) continue;
          dependent.es = dependent.s0 + shift;
          dependent.ee = dependent.d0 + shift;
          moved.set(dependent.item.id, shift);
          queue.push(dependent);
        }
      }
    }
  }

  // Row tops.
  for (const row of rows) {
    if (row.type === "group") {
      y += GROUP_ROW_HEIGHT;
      continue;
    }
    row.work.top = y;
    row.work.cy = y + TASK_ROW_HEIGHT / 2;
    y += TASK_ROW_HEIGHT;
  }
  const bodyHeight = y;

  const nameOf = (work: Work) =>
    work.item.kind === "item"
      ? `${work.item.itemKey ? `${work.item.itemKey} ` : ""}${work.item.title}`
      : `${copy.kind[work.item.kind]} ${work.item.title}`;
  const ariaOf = new Map<Work, string>();

  for (const work of works) {
    const { item } = work;
    const start = work.s0;
    const name = nameOf(work);
    if (start === null) {
      // Only a finish date is known (a done item): a dot on that day, or a
      // note when it is before the window.
      if (work.d0 < from) {
        work.off = true;
        work.labelLeft = 14;
        work.label = {
          text: fill(copy.finished.before, { date: fmtDay(work.d0) }),
          left: 14,
          maxWidth: 280,
        };
        ariaOf.set(
          work,
          fill(copy.finished.beforeLabel, { name, date: fmtDay(work.d0) }),
        );
      } else if (work.d0 >= windowEnd) {
        work.off = true;
        work.outsideAfter = true;
        work.label = {
          text: fill(copy.finished.after, { date: fmtDay(work.d0) }),
          right: 6,
          maxWidth: 280,
        };
        ariaOf.set(
          work,
          fill(copy.outside.afterLabel, { name, date: fmtDay(work.d0) }),
        );
      } else {
        work.isFin = true;
        const c = Math.round(X(work.d0) + P / 2);
        work.fx1 = c + 5;
        work.fc = c;
        work.labelLeft = c + 11;
        work.label = {
          text: fill(copy.finished.note, { date: fmtDay(work.d0) }),
          left: c + 11,
          maxWidth: 280,
        };
        ariaOf.set(
          work,
          fill(copy.finished.label, { name, date: fmtDay(work.d0) }),
        );
      }
      continue;
    }
    const lateDays = todayN - work.d0;
    work.late = !work.isMs && item.tone !== "done" && work.d0 < todayN;
    work.lateDays = lateDays;
    const visibleFrom = Math.max(start, from);
    const visibleTo = Math.min(work.d0 + 1, windowEnd);
    if (visibleTo <= visibleFrom) {
      // Wholly outside the window: a note at the edge.
      work.off = true;
      if (work.d0 < from) {
        work.label = {
          text: fill(copy.outside.before, { date: fmtDay(work.d0) }),
          left: 14,
          maxWidth: 280,
        };
        work.labelLeft = 14;
        ariaOf.set(
          work,
          fill(copy.outside.beforeLabel, { name, date: fmtDay(work.d0) }),
        );
      } else {
        work.outsideAfter = true;
        work.label = {
          text: fill(copy.outside.after, { date: fmtDay(start) }),
          right: 6,
          maxWidth: 280,
        };
        ariaOf.set(
          work,
          fill(copy.outside.afterLabel, { name, date: fmtDay(start) }),
        );
      }
      work.late = false;
      continue;
    }
    work.clipStart = start < from;
    work.clipEnd = work.d0 + 1 > windowEnd;
    const x = X(visibleFrom);
    const w = X(visibleTo) - x;
    work.x = x + 1;
    work.w = w - 2;
    work.bar = !work.isMs;
    const startText = fmtDay(start);
    const dueText = fmtDay(work.d0);
    const pct = `${item.percent}%`;
    if (scale === "month") {
      // Very compact bars: the title lives in the tooltip and the label.
      if (!work.isMs && item.title.length * 6.6 + 6 <= w - 2) {
        work.inLabel = item.title;
      } else if (
        !work.isMs &&
        item.kind === "item" &&
        item.itemKey &&
        item.itemKey.length * 6.3 + 6 <= w - 2
      ) {
        work.inLabel = item.itemKey;
        work.mono = true;
      }
    } else if (!work.isMs) {
      if (w >= 120) work.inLabel = item.title;
      else
        work.label = {
          text: item.title,
          left: 0,
          maxWidth: 280,
        };
    }
    work.labelLeft = X(work.ee + 1) + 6;
    if (work.isMs) {
      const c = Math.round(X(work.d0) + P / 2);
      work.msC = c;
      work.labelLeft = c + 13;
    }
    const notes =
      (work.late
        ? fill(plural(copy.bar.overdue_one, copy.bar.overdue_other, lateDays), {
            days: lateDays,
          })
        : "") + (work.clipStart ? copy.bar.clipped : "");
    ariaOf.set(
      work,
      work.isMs
        ? fill(copy.milestone.label, { title: item.title, date: dueText })
        : fill(copy.bar.label, {
            name,
            from: startText,
            to: dueText,
            percent: pct,
            notes,
          }),
    );
  }

  // Dependency arrows (finish to start), through the same X().
  const segN: Array<{
    x: number;
    y: number;
    w: number;
    h: number;
    critical: boolean;
  }> = [];
  const headN: LinkHead[] = [];
  const spanOf = (work: Work) => ({
    es: work.es ?? work.d0,
    ee: work.ee,
  });
  for (const link of links) {
    const f = byId.get(link.from);
    const t = byId.get(link.to);
    if (!f || !t) continue;
    if (t.off || (t.s0 === null && !t.isFin)) continue;
    const critical = criticalPath && f.critical && t.critical;
    const x1 = f.off
      ? 0
      : f.isFin
        ? f.fx1
        : (f.late ? X(todayN + 1) : X(f.ee + 1)) - 1;
    const tSpan = spanOf(t);
    const xs = X(Math.max(tSpan.es, from));
    const x2 = xs + (t.isMs ? 4 : 0);
    const y1 = f.cy;
    const y2 = t.cy;
    if (x2 - x1 >= 14 && !t.isMs) {
      const mid = x1 + 6;
      segN.push({ x: x1, y: y1, w: mid - x1, h: 1.5, critical });
      segN.push({
        x: mid,
        y: Math.min(y1, y2),
        w: 1.5,
        h: Math.abs(y2 - y1),
        critical,
      });
      segN.push({ x: mid, y: y2, w: x2 - mid - 6, h: 1.5, critical });
      headN.push({ x: x2 - 6, y: y2 - 3, down: false, critical });
    } else {
      // No room for an elbow: drop straight onto the top edge of the next bar.
      const xv = t.isMs ? t.msC : Math.max(x1 + 6, x2 + 5);
      const top = y2 - 18 + (t.isMs ? 7 : barTop);
      if (xv > x1) segN.push({ x: x1, y: y1, w: xv - x1, h: 1.5, critical });
      segN.push({ x: xv, y: y1, w: 1.5, h: top - 6 - y1, critical });
      headN.push({ x: xv - 3, y: top - 6, down: true, critical });
    }
  }

  // Outside labels start clear of the arrow lines in their own row and stay
  // inside what the reader can see (the chart, or the part of it scrolled
  // into view). With no room on the right of a bar or dot the label moves to
  // its left, so it is never cut by an edge.
  const todayX = Math.round(X(todayN) + P * 0.45);
  const seeFrom = Math.max(0, input.visible?.from ?? 0);
  const seeTo = Math.min(chartWidth, input.visible?.to ?? chartWidth);
  const wide = (text: string) => text.length * LABEL_CHAR;
  for (const work of works) {
    const label = work.label;
    if (!label) continue;
    if (work.off) {
      work.label = fitLabel(label, chartWidth, seeFrom, seeTo, true);
      continue;
    }
    let lx = work.labelLeft;
    let again = true;
    while (again) {
      again = false;
      for (const g of segN) {
        const inRow = g.y < work.cy + 9 && g.y + g.h > work.cy - 9;
        if (inRow && g.x <= lx + 4 && g.x + g.w > lx - 6) {
          lx = g.x + g.w + 6;
          again = true;
        }
      }
    }
    lx = Math.round(lx);
    const place = (text: string): LabelLayout | null => {
      const need = Math.min(280, wide(text));
      const room = Math.min(280, seeTo - lx - 4);
      if (work.isFin) {
        // A done note reads best after its dot; it goes to the left of the dot
        // when it would not fit or would sit across the today line.
        const crosses = todayX >= lx - 2 && todayX <= lx + wide(text) + 2;
        if (room >= need && !crosses) return { text, left: lx, maxWidth: room };
        const anchor = work.fc - 11;
        const roomLeft = Math.min(280, anchor - seeFrom - 4);
        const leftCrosses =
          todayX >= anchor - wide(text) - 2 && todayX <= anchor + 2;
        if (roomLeft >= need && !leftCrosses)
          return { text, right: chartWidth - anchor, maxWidth: roomLeft };
        return null;
      }
      if (room >= need) return { text, left: lx, maxWidth: room };
      const barLeft = work.x - 6;
      const roomLeft = Math.min(280, barLeft - seeFrom - 4);
      const onToday =
        todayX >= barLeft - wide(text) - 2 && todayX <= barLeft + 2;
      const left = { text, right: chartWidth - barLeft, maxWidth: roomLeft };
      // Left of the bar when the whole title fits there without crossing the
      // today line; otherwise the right side, cut short with an ellipsis.
      if (roomLeft >= need && !onToday) return left;
      if (room >= 90) return { text, left: lx, maxWidth: room };
      if (roomLeft >= 90) return left;
      return null;
    };
    let placed = place(label.text);
    if (!placed && work.isFin) {
      // Neither side takes the whole note: keep the date next to the dot.
      placed = place(fill(copy.finished.short, { date: fmtDay(work.d0) }));
    }
    work.label = placed ? fitLabel(placed, chartWidth, seeFrom, seeTo) : null;
  }

  const out: RowLayout[] = [];
  for (const row of rows) {
    if (row.type === "group") {
      const top = row.works[0]?.top ?? 0;
      out.push({
        type: "group",
        id: row.group.id,
        title: row.group.title,
        count: row.works.length,
        top: top - GROUP_ROW_HEIGHT,
      });
      continue;
    }
    const w = row.work;
    const { item } = w;
    const shift = moved.get(item.id) ?? 0;
    const dragged = drag?.id === item.id;
    const effectiveStart = w.es ?? w.d0;
    const changed = w.ee !== w.d0 || (w.s0 !== null && effectiveStart !== w.s0);
    const crit = criticalPath;
    const bar: BarLayout | null =
      w.bar && !w.off
        ? {
            x: w.x,
            width: w.w,
            tone: item.tone,
            clipStart: w.clipStart,
            clipEnd: w.clipEnd,
            fade: crit && !w.critical,
            critical: crit && w.critical,
            percent: item.percent,
            ...innerLabel(
              w.inLabel,
              w.mono,
              w.x,
              w.w,
              scale === "month",
              seeFrom,
              seeTo,
            ),
          }
        : null;

    let ghost: TaskLayout["ghost"] = null;
    let handle: TaskLayout["handle"] = null;
    let tip: TaskLayout["tip"] = null;
    if (dragged && w.s0 !== null) {
      if (drag?.mode === "resize") {
        const lo = Math.min(w.d0, w.ee);
        const hi = Math.max(w.d0, w.ee);
        ghost =
          hi > lo ? { x: X(lo + 1) - 1, width: X(hi + 1) - X(lo + 1) } : null;
        handle = { x: X(w.ee + 1) - 4 };
        tip = {
          x: X(w.d0 + 1) - 60,
          text: fill(copy.tip.resize, { from: fmtDay(w.d0), to: fmtDay(w.ee) }),
          note: cascadeNote(works, moved, item.id, copy),
        };
      } else {
        ghost = { x: X(effectiveStart), width: w.w + 2 };
        handle = null;
        tip = {
          x: X(effectiveStart) - 60,
          text: fill(copy.tip.move, {
            from: rangeOf(w.s0, w.d0),
            to: rangeOf(effectiveStart, w.ee),
          }),
          note: cascadeNote(works, moved, item.id, copy),
        };
      }
    } else if (shift > 0 && w.s0 !== null) {
      ghost = { x: X(effectiveStart), width: w.w + 2 };
    }

    out.push({
      type: "task",
      item,
      id: item.id,
      hasKey: item.kind === "item",
      tag: item.kind === "item" ? "" : copy.kind[item.kind],
      dates: fmtDay(w.ee),
      dateState: w.late ? "over" : changed ? "moved" : "",
      top: w.top,
      cy: w.cy,
      bar,
      fin: w.isFin
        ? {
            x: w.fc - 5,
            fade: crit && !w.critical,
            critical: crit && w.critical,
          }
        : null,
      label: w.label,
      late:
        w.late && !w.off
          ? lateNote(
              {
                x: X(w.d0 + 1) - 1,
                width: X(todayN + 1) - X(w.d0 + 1),
                textLeft: X(todayN + 1) + 14,
                text: fill(plural(copy.late_one, copy.late_other, w.lateDays), {
                  days: w.lateDays,
                }),
              },
              chartWidth,
              seeFrom,
              seeTo,
            )
          : null,
      milestone:
        w.isMs && !w.off
          ? {
              x: w.msC - 7,
              ...milestoneLabel(w.msC, item.title, chartWidth, seeFrom, seeTo),
              title: fill(copy.milestone.title, {
                title: item.title,
                date: fmtDay(w.d0),
              }),
              aria: ariaOf.get(w) ?? "",
            }
          : null,
      ghost,
      handle,
      tip,
      aria: ariaOf.get(w) ?? "",
      title: item.title,
    });
  }

  return {
    rows: out,
    segments: segN,
    heads: headN,
    bodyHeight,
    undated,
  };
}

/**
 * Where a header label (a month, a week) is drawn when only part of its cell
 * is in view: it follows the view's left edge while the cell has room for
 * all of it, and is left out (`null`) once the visible part is too short, so
 * the header never shows a half-cut word. `whole` is a cell fully in view,
 * which draws as it always does.
 */
export function fitHeaderText(
  x: number,
  width: number,
  text: string,
  charWidth: number,
  pad: number,
  visible: { readonly from: number; readonly to: number } | null | undefined,
): { whole: boolean; shift: number; max: number } | null {
  const natural = Math.max(0, width - 2 * pad);
  if (!visible || (x >= visible.from && x + width <= visible.to))
    return { whole: true, shift: 0, max: natural };
  const need = Math.min(text.length * charWidth, natural);
  const left = Math.max(x + pad, visible.from + pad);
  const right = Math.min(x + width - pad, visible.to - pad);
  const room = right - left;
  if (room < need) return null;
  return {
    whole: false,
    shift: left - (x + pad),
    max: Math.min(natural, room),
  };
}

/** Whether a header cell of `width` at `x` is wholly in view (else its text is left out). */
export function cellInView(
  x: number,
  width: number,
  visible: { readonly from: number; readonly to: number } | null | undefined,
): boolean {
  return !visible || (x >= visible.from && x + width <= visible.to);
}

/**
 * The title inside a bar, kept inside the part of the bar the reader sees:
 * it follows the view's left edge when the bar starts behind it and stops
 * short of the right edge. It is shown whole, or truncated with an ellipsis
 * on the day and week scales; with less room than "Abcd…" (or, on the month
 * scale, less than the whole text) it is left out.
 */
function innerLabel(
  text: string,
  mono: boolean,
  x: number,
  width: number,
  compact: boolean,
  seeFrom: number,
  seeTo: number,
): Pick<BarLayout, "label" | "mono" | "labelShift" | "labelMax"> {
  const pad = compact ? 3 : 8;
  const natural = Math.max(0, width - 2 * pad);
  const left = Math.max(x + pad, seeFrom + pad);
  const right = Math.min(x + width - pad, seeTo - pad);
  const room = Math.max(0, right - left);
  const need = text.length * (mono ? 6.3 : LABEL_CHAR);
  const fits = compact ? room >= need : room >= Math.min(need, MIN_TEXT_ROOM);
  if (text === "" || !fits)
    return { label: "", mono, labelShift: 0, labelMax: natural };
  return {
    label: text,
    mono,
    labelShift: left - (x + pad),
    labelMax: Math.min(natural, room),
  };
}

/**
 * Keeps a label inside what the reader sees: a gap off the list divider and
 * off the right edge. A label that runs past either edge is shortened (the
 * view cuts it with an ellipsis) and one with less room than "Abcd…" is
 * dropped, so no glyph is ever cut by a frame edge. A label belongs to the
 * bar, dot or milestone it stands by: once that has scrolled past an edge the
 * label goes with it, except an edge note (`hug`), which is about what lies
 * outside the window and so keeps to the edge it names.
 */
function fitLabel(
  label: LabelLayout,
  chartWidth: number,
  seeFrom: number,
  seeTo: number,
  hug = false,
): LabelLayout | null {
  const need = Math.min(label.maxWidth, label.text.length * LABEL_CHAR);
  const floor = seeFrom + VIEW_PAD_LEFT;
  const ceil = seeTo - VIEW_PAD_RIGHT;
  if (label.left !== undefined) {
    if (!hug && label.left < seeFrom) return null;
    const left = Math.max(label.left, floor);
    const room = Math.min(label.maxWidth, ceil - left);
    return room >= Math.min(need, MIN_TEXT_ROOM)
      ? { text: label.text, left, maxWidth: room }
      : null;
  }
  const anchor = chartWidth - (label.right ?? 0);
  if (!hug && anchor > seeTo) return null;
  const edge = Math.min(anchor, ceil);
  const room = Math.min(label.maxWidth, edge - floor);
  return room >= Math.min(need, MIN_TEXT_ROOM)
    ? { text: label.text, right: chartWidth - edge, maxWidth: room }
    : null;
}

/** The milestone's label sits right of the diamond, or left of it with no room on the right. */
function milestoneLabel(
  centre: number,
  title: string,
  chartWidth: number,
  seeFrom: number,
  seeTo: number,
): {
  labelLeft: number | undefined;
  labelRight: number | undefined;
  labelMax: number;
} {
  const need = Math.min(280, title.length * LABEL_CHAR * 1.08);
  const left = centre + 13;
  const anchor = centre - 13;
  const onRight = seeTo - left - 4 >= need;
  const onLeft = !onRight && anchor - seeFrom - 4 >= need;
  const label: LabelLayout = onLeft
    ? { text: title, right: chartWidth - anchor, maxWidth: 280 }
    : { text: title, left, maxWidth: 280 };
  const fit = fitLabel(label, chartWidth, seeFrom, seeTo);
  return {
    labelLeft: fit ? fit.left : undefined,
    labelRight: fit ? fit.right : undefined,
    labelMax: fit?.maxWidth ?? 0,
  };
}

/** The overdue note stays in view; with less room than "Abcd…" it is left out. */
function lateNote<T extends { textLeft: number; text: string }>(
  late: T,
  chartWidth: number,
  seeFrom: number,
  seeTo: number,
): T & { textMax: number } {
  const fit = fitLabel(
    { text: late.text, left: late.textLeft, maxWidth: 200 },
    chartWidth,
    seeFrom,
    seeTo,
  );
  return {
    ...late,
    textLeft: fit?.left ?? late.textLeft,
    textMax: fit?.maxWidth ?? 0,
  };
}

function cascadeNote(
  works: readonly Work[],
  moved: ReadonlyMap<string, number>,
  draggedId: string,
  copy: TimelineCopy,
): string {
  const first = works.find(
    (work) => work.item.id !== draggedId && (moved.get(work.item.id) ?? 0) > 0,
  );
  if (!first) return "";
  return fill(copy.tip.cascade, {
    title: first.item.title,
    days: moved.get(first.item.id) ?? 0,
    date: fmtDay(first.es ?? first.ee),
  });
}
