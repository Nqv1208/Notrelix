import type { MarkToneId } from "../../core/model/navigation";
import type { WorkPriority, WorkStatusBucket } from "../home/home-models";

/**
 * The shape the Reports pages read (gap `CTR-GAP-REPORTS`, see
 * docs/superpowers/be-blockers-2026-10.md, "BE requirement from FE: Reports").
 * A report is read-only over boards: it counts what the viewer may see and
 * never changes an item. Days are Hanoi days as "YYYY-MM-DD"; moments are ISO
 * instants shown in the report's time zone.
 */

export interface ReportPersonDto {
  readonly id: string;
  readonly name: string;
  readonly tone: MarkToneId;
}

export interface ReportScopeDto {
  readonly id: string;
  readonly label: string;
  readonly tone: MarkToneId;
}

export type ReportAccess = "edit" | "view";
export type ReportWidgetSize = "wide" | "half" | "side";
export type ReportGroupBy =
  "assignee" | "status" | "priority" | "module" | "week";
export type ReportMetric = "count" | "overdue";
export type ReportFilterId =
  "not-done" | "overdue" | "assigned" | "unassigned" | "high-priority";
export type ReportWidgetType =
  | "number"
  | "columns"
  | "line"
  | "donut"
  | "progress"
  | "workload"
  | "table"
  | "note";

/** What is stored of a widget; its numbers are read, never stored. */
export interface ReportWidgetSpec {
  readonly id: string;
  readonly kind: string;
  readonly size: ReportWidgetSize;
  readonly title?: string | null;
  readonly titleKey?: string | null;
  readonly boardId?: string | null;
  readonly metric?: ReportMetric;
  readonly groupBy?: ReportGroupBy;
  readonly filters?: readonly ReportFilterId[];
  readonly text?: string | null;
  readonly authorId?: string | null;
  readonly writtenOn?: string | null;
}

export interface ReportGroupRow {
  readonly key: string;
  readonly label: string;
  readonly count: number;
  readonly person: ReportPersonDto | null;
  /** The row of items that have no value ("Chưa giao", "Chưa đặt"). */
  readonly none: boolean;
  readonly priority: WorkPriority | null;
  readonly bucket: WorkStatusBucket | null;
  readonly tone?: MarkToneId;
}

export interface ReportDueItem {
  readonly id: string;
  readonly key: string;
  readonly title: string;
  readonly boardId: string;
  readonly bucket: WorkStatusBucket;
  readonly statusLabel: string;
  readonly assignee: ReportPersonDto | null;
  readonly day: string;
  readonly priority: WorkPriority;
  readonly overdueDays: number;
  readonly module: string | null;
  readonly awaitingViewer: boolean;
}

export interface ReportVelocityWeek {
  readonly from: string;
  readonly to: string;
  readonly count: number | null;
  readonly state: "past" | "current" | "future";
}

interface WidgetBase {
  readonly id: string;
  readonly spec: ReportWidgetSpec;
  readonly size: ReportWidgetSize;
  readonly title: string | null;
  readonly titleKey: string | null;
  /** Set while the widget is recomputed: the moment its numbers are from. */
  readonly staleAsOf: string | null;
}

export type ReportWidgetDto = WidgetBase &
  (
    | {
        readonly kind: "status";
        readonly total: number;
        readonly counts: Readonly<Record<WorkStatusBucket, number>>;
      }
    | {
        readonly kind: "velocity";
        readonly weeks: readonly ReportVelocityWeek[];
        readonly past: readonly number[];
        readonly average: number;
        readonly pace: number | null;
        readonly openCount: number;
        readonly weeksLeft: number | null;
        readonly launchDate: string | null;
        readonly yMax: number;
        readonly verdict: "late" | "ontrack" | "none";
      }
    | {
        readonly kind: "bars";
        readonly variant: "module" | "priority" | "department";
        readonly subtitle: "open" | "all" | "week-done" | "week-overdue";
        readonly total: number;
        readonly max: number;
        readonly rows: readonly ReportGroupRow[];
        readonly tone?: "accent" | "danger";
        readonly weekFrom?: string;
        readonly weekTo?: string;
      }
    | {
        readonly kind: "due" | "table";
        readonly from?: string;
        readonly to?: string;
        readonly items: readonly ReportDueItem[];
        readonly total?: number;
      }
    | {
        readonly kind: "workload";
        readonly total: number;
        readonly max: number;
        readonly rows: readonly {
          readonly key: string;
          readonly person: ReportPersonDto | null;
          readonly open: number;
          readonly overdue: number;
        }[];
      }
    | {
        readonly kind: "note";
        readonly author: ReportPersonDto | null;
        readonly at: string | null;
        readonly paragraphs: readonly string[];
      }
    | {
        readonly kind: "progress";
        readonly done: number;
        readonly total: number;
        readonly boardId?: string | null;
        readonly boardName: string | null;
        readonly launchDate: string | null;
        readonly daysLeft: number | null;
        readonly overdue: number;
        readonly link: boolean;
      }
    | {
        readonly kind: "number";
        readonly value: number;
        readonly of: number;
        readonly metric: ReportMetric;
        readonly boardName: string | null;
        readonly total: number;
      }
    | {
        readonly kind: "columns" | "line" | "donut";
        readonly groupBy: ReportGroupBy;
        readonly rows: readonly ReportGroupRow[];
        readonly max: number;
        readonly metric: ReportMetric;
        readonly boardName: string | null;
        readonly total: number;
      }
  );

export interface ReportSummaryDto {
  readonly done: number;
  readonly total: number;
  readonly overdue: number;
  readonly dueSoon: number;
  readonly launchDate: string | null;
  readonly daysToLaunch: number | null;
}

export type ReportTimeFilter =
  | { readonly kind: "until"; readonly day: string }
  | { readonly kind: "week"; readonly from: string; readonly to: string };

export interface ReportFiltersDto {
  readonly board: {
    readonly selectedId: string;
    readonly options: readonly { id: string; name: string }[];
  } | null;
  readonly time: ReportTimeFilter | null;
  readonly assignee: {
    readonly options: readonly { id: string; name: string }[];
  } | null;
  readonly sources: { readonly departments: readonly string[] } | null;
}

export interface ReportDto {
  readonly id: string;
  /** null = a report nobody named yet. */
  readonly name: string | null;
  readonly scope: ReportScopeDto;
  readonly owner: ReportPersonDto | null;
  readonly sharedWith: readonly string[];
  readonly includesViewer: boolean;
  readonly access: ReportAccess;
  readonly createdAt: string;
  readonly updatedAt: string;
  readonly dataAsOf: string;
  readonly timeZone: string;
  readonly excludedBoardCount: number;
  readonly sourceBoard: { readonly id: string; readonly name: string } | null;
  readonly filters: ReportFiltersDto;
  readonly summary: ReportSummaryDto | null;
  readonly widgets: readonly ReportWidgetDto[];
}

export interface ReportListItemDto {
  readonly id: string;
  readonly name: string | null;
  readonly scope: ReportScopeDto;
  readonly owner: ReportPersonDto | null;
  readonly access: ReportAccess;
  readonly mine: boolean;
  readonly sourceBoardName: string | null;
  readonly multiSource: boolean;
  readonly widgetCount: number | null;
  readonly sharedWith: readonly string[];
  readonly includesViewer: boolean;
  readonly excludedBoardCount: number;
  readonly updatedAt: string;
}

export interface ReportDrillDto {
  readonly person: ReportPersonDto | null;
  readonly items: readonly ReportDueItem[];
  readonly board: { readonly id: string; readonly name: string };
  readonly dataAsOf: string;
  readonly timeZone: string;
}

export interface ReportExportJobDto {
  readonly id: string;
  readonly format: "pdf" | "csv";
  readonly status: "running" | "done";
  readonly startedAt: string;
  readonly filename: string;
  readonly downloadUrl: string | null;
  readonly expiresAt: string | null;
}

export interface ReportWidgetOptionsDto {
  readonly boards: readonly {
    readonly id: string;
    readonly name: string;
    readonly dimensions: readonly ReportGroupBy[];
  }[];
  readonly metrics: readonly ReportMetric[];
  readonly filters: readonly ReportFilterId[];
}

export interface ReportSharingDto {
  readonly targets: readonly ReportScopeDto[];
  readonly selected: readonly string[];
  readonly canEdit: boolean;
}

export type ReportLayoutId = "project" | "team" | "deadline";

export type ReportListFilter = "mine" | "shared" | "all";

export const DEFAULT_WIDGET_NAME_LIMIT = 80;

/** Widget types the add dialog offers, in the mockup's order. */
export const WIDGET_TYPES: readonly ReportWidgetType[] = [
  "number",
  "columns",
  "line",
  "donut",
  "progress",
  "workload",
  "table",
  "note",
];

/** The widget types that group by a dimension. */
export const GROUPED_TYPES: ReadonlySet<ReportWidgetType> = new Set([
  "columns",
  "line",
  "donut",
]);

export function widgetNeedsSource(type: ReportWidgetType): boolean {
  return type !== "note";
}

/** Moves the widget at `from` to `to`, shifting the ones between. */
export function moveWidget<T>(
  list: readonly T[],
  from: number,
  to: number,
): T[] {
  if (
    from === to ||
    from < 0 ||
    to < 0 ||
    from >= list.length ||
    to >= list.length
  )
    return [...list];
  const next = [...list];
  const [item] = next.splice(from, 1);
  next.splice(to, 0, item as T);
  return next;
}

/** The index of the next widget of the same column group in a direction. */
export function neighbourIndex(
  list: readonly { readonly size: ReportWidgetSize }[],
  index: number,
  direction: -1 | 1,
): number {
  const side = list[index]?.size === "side";
  for (
    let cursor = index + direction;
    cursor >= 0 && cursor < list.length;
    cursor += direction
  ) {
    if ((list[cursor]?.size === "side") === side) return cursor;
  }
  return index;
}

/** The two columns of the dashboard: wide and half widgets, then the side. */
export function splitColumns<T extends { readonly size: ReportWidgetSize }>(
  list: readonly T[],
): { main: T[]; side: T[] } {
  return {
    main: list.filter((widget) => widget.size !== "side"),
    side: list.filter((widget) => widget.size === "side"),
  };
}

export type ReportBlock<T> =
  | { readonly kind: "single"; readonly widget: T }
  | { readonly kind: "pair"; readonly widgets: readonly [T, T] };

/** Consecutive half widgets pair up; a lone half widget stays on its own. */
export function mainBlocks<T extends { readonly size: ReportWidgetSize }>(
  main: readonly T[],
): ReportBlock<T>[] {
  const blocks: ReportBlock<T>[] = [];
  for (let index = 0; index < main.length; index += 1) {
    const widget = main[index]!;
    const next = main[index + 1];
    if (widget.size === "half" && next?.size === "half") {
      blocks.push({ kind: "pair", widgets: [widget, next] });
      index += 1;
    } else {
      blocks.push({ kind: "single", widget });
    }
  }
  return blocks;
}

/** A person's two-letter mark. */
export function personInitials(name: string): string {
  const words = name.trim().split(/\s+/).filter(Boolean);
  const pick =
    words.length >= 2
      ? [words[words.length - 2], words[words.length - 1]]
      : words;
  const letters = pick.map((word) => Array.from(word ?? "")[0] ?? "");
  return letters.join("").toUpperCase();
}
