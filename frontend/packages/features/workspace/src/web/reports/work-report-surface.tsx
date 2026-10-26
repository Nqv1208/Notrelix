import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  cn,
} from "@notrelix/ui-web";
import type { FeatureAvailabilityCopy } from "../unavailable/feature-availability.copy";
import { formatCopy } from "../shell/shell-models";
import {
  BILLING_BTN,
  BOX,
  BOX_TEXT,
  BOX_TITLE,
  CARD,
  PageHead,
} from "../billing/billing-parts";

export interface WorkReportStatusRow {
  readonly optionId: string | null;
  readonly name: string | null;
  readonly color: string | null;
  /** `Todo`, `InProgress`, `Review`, `Done` or `None`. */
  readonly category: string;
  readonly count: number;
}

export interface WorkReportAssigneeRow {
  readonly userId: string | null;
  readonly name: string | null;
  readonly items: number;
  readonly open: number;
  readonly overdue: number;
}

export interface WorkReportWeekRow {
  /** ISO timestamp of the Monday the week starts. */
  readonly weekStart: string;
  readonly completed: number;
}

/** The read-only work measures of a set of boards, as the backend reports them. */
export interface WorkReportDto {
  readonly total: number;
  readonly open: number;
  readonly done: number;
  readonly overdue: number;
  readonly byStatus: readonly WorkReportStatusRow[];
  readonly byAssignee: readonly WorkReportAssigneeRow[];
  readonly completedPerWeek: readonly WorkReportWeekRow[];
  /** `null` when nothing is finished. */
  readonly averageCompletionSeconds: number | null;
}

export type WorkReportState =
  | { readonly status: "loading" }
  | { readonly status: "error" }
  | { readonly status: "forbidden" }
  | { readonly status: "ready"; readonly data: WorkReportDto };

/** What the report can be narrowed to. */
export interface WorkReportScope {
  readonly boards: readonly { readonly id: string; readonly title: string }[];
  readonly boardId: string | null;
  /** Departments; present only for people who may narrow by department. */
  readonly spaces?: readonly { readonly id: string; readonly name: string }[];
  readonly spaceId: string | null;
}

export interface WorkReportSurfaceProps {
  readonly copy: FeatureAvailabilityCopy;
  readonly state: WorkReportState;
  readonly scope: WorkReportScope;
  readonly onBoard: (boardId: string | null) => void;
  readonly onSpace: (spaceId: string | null) => void;
  readonly onRetry: () => void;
}

const SELECT = "min-w-44";

/** Radix items cannot carry an empty value, so "all" is a sentinel. */
const ALL = "__all__";

const CATEGORY_BG: Readonly<Record<string, string>> = {
  Todo: "bg-work-status-todo",
  InProgress: "bg-work-status-doing",
  Review: "bg-work-status-review",
  Done: "bg-work-status-done",
  None: "bg-state-hover-strong",
};

function percent(value: number, max: number): number {
  return max <= 0 ? 0 : Math.min(100, (value / max) * 100);
}

function durationLabel(
  copy: FeatureAvailabilityCopy["workReport"],
  seconds: number | null,
): string {
  if (seconds === null) return copy.averageNone;
  const days = seconds / 86_400;
  if (days >= 1) {
    return formatCopy(copy.days, { count: Math.round(days * 10) / 10 });
  }
  return formatCopy(copy.hours, {
    count: Math.round((seconds / 3_600) * 10) / 10,
  });
}

function dayLabel(iso: string): string {
  const date = new Date(iso);
  const day = String(date.getUTCDate()).padStart(2, "0");
  const month = String(date.getUTCMonth() + 1).padStart(2, "0");
  return `${day}/${month}`;
}

function Bar({
  value,
  max,
  label,
  tone,
}: {
  value: number;
  max: number;
  label: string;
  tone: string;
}) {
  return (
    <div
      role="meter"
      aria-valuemin={0}
      aria-valuemax={max}
      aria-valuenow={value}
      aria-label={label}
      className="flex h-2 overflow-hidden rounded-full bg-state-hover-strong shadow-[inset_0_0_0_1px_var(--color-line)]"
    >
      <span
        style={{ width: `${percent(value, max)}%` }}
        className={cn("block h-full", tone)}
      />
    </div>
  );
}

function Tile({ label, value }: { label: string; value: string }) {
  return (
    <div className={cn(CARD, "flex min-w-32 flex-col gap-1 p-3.5")}>
      <span className="text-crumb text-meta-foreground">{label}</span>
      <span className="text-section-heading leading-[1.2] font-[650] text-foreground">
        {value}
      </span>
    </div>
  );
}

function Report({
  copy,
  data,
}: {
  copy: FeatureAvailabilityCopy["workReport"];
  data: WorkReportDto;
}) {
  if (data.total === 0) {
    return (
      <div role="status" className={cn(BOX, "max-w-[520px]")}>
        <p className={BOX_TEXT}>{copy.empty}</p>
      </div>
    );
  }
  const statusRows = data.byStatus.filter(
    (row) => row.count > 0 || row.optionId !== null || row.category !== "None",
  );
  const statusMax = Math.max(1, ...statusRows.map((row) => row.count));
  const assigneeMax = Math.max(1, ...data.byAssignee.map((row) => row.items));
  const weekMax = Math.max(1, ...data.completedPerWeek.map((w) => w.completed));

  return (
    <div className="flex flex-col gap-4">
      <div className="flex flex-wrap gap-2.5">
        <Tile label={copy.totals.total} value={String(data.total)} />
        <Tile label={copy.totals.open} value={String(data.open)} />
        <Tile label={copy.totals.done} value={String(data.done)} />
        <Tile label={copy.totals.overdue} value={String(data.overdue)} />
        <Tile
          label={copy.totals.average}
          value={durationLabel(copy, data.averageCompletionSeconds)}
        />
      </div>

      <section
        aria-label={copy.statusTitle}
        className={cn(BOX, "max-w-[640px]")}
      >
        <h3 className={BOX_TITLE}>{copy.statusTitle}</h3>
        <ul className="m-0 flex list-none flex-col gap-2 p-0">
          {statusRows.map((row) => {
            const name =
              row.name ?? copy.statusLabels[row.category] ?? row.category;
            return (
              <li
                key={`${row.optionId ?? row.category}`}
                className="flex flex-col gap-1"
              >
                <div className="flex items-baseline gap-3 text-compact text-foreground">
                  <span>{name}</span>
                  <span className="grow" />
                  <span className="text-meta-foreground">{row.count}</span>
                </div>
                <Bar
                  value={row.count}
                  max={statusMax}
                  label={name}
                  tone={CATEGORY_BG[row.category] ?? CATEGORY_BG.None!}
                />
              </li>
            );
          })}
        </ul>
      </section>

      <section
        aria-label={copy.assigneeTitle}
        className={cn(BOX, "max-w-[640px]")}
      >
        <h3 className={BOX_TITLE}>{copy.assigneeTitle}</h3>
        <ul className="m-0 flex list-none flex-col gap-2 p-0">
          {data.byAssignee.map((row) => {
            const name = row.name ?? copy.unassigned;
            return (
              <li
                key={row.userId ?? "unassigned"}
                className="flex flex-col gap-1"
              >
                <div className="flex items-baseline gap-3 text-compact text-foreground">
                  <span>{name}</span>
                  <span className="grow" />
                  <span className="text-crumb text-meta-foreground">
                    {formatCopy(copy.assigneeCount, {
                      items: row.items,
                      overdue: row.overdue,
                    })}
                  </span>
                </div>
                <Bar
                  value={row.items}
                  max={assigneeMax}
                  label={name}
                  tone="bg-primary"
                />
              </li>
            );
          })}
        </ul>
      </section>

      <section
        aria-label={copy.weeksTitle}
        className={cn(BOX, "max-w-[640px]")}
      >
        <h3 className={BOX_TITLE}>{copy.weeksTitle}</h3>
        <ul className="m-0 flex h-36 list-none items-end gap-2 p-0">
          {data.completedPerWeek.map((week) => (
            <li
              key={week.weekStart}
              title={formatCopy(copy.weekOf, {
                day: dayLabel(week.weekStart),
                count: week.completed,
              })}
              aria-label={formatCopy(copy.weekOf, {
                day: dayLabel(week.weekStart),
                count: week.completed,
              })}
              className="flex h-full flex-1 flex-col items-center justify-end gap-1"
            >
              <span className="text-crumb text-meta-foreground">
                {week.completed}
              </span>
              <span
                aria-hidden="true"
                style={{ height: `${percent(week.completed, weekMax) * 0.8}%` }}
                className="block min-h-px w-full rounded-t-[4px] bg-primary"
              />
              <span
                aria-hidden="true"
                className="text-crumb text-meta-foreground"
              >
                {dayLabel(week.weekStart)}
              </span>
            </li>
          ))}
        </ul>
      </section>
    </div>
  );
}

/**
 * The read-only work report of the boards the caller can read: items by status and assignee, overdue,
 * what was finished in each of the last eight weeks and how long finishing takes. A board narrows it;
 * people who manage spaces can also narrow it to a department.
 */
export function WorkReportSurface({
  copy,
  state,
  scope,
  onBoard,
  onSpace,
  onRetry,
}: WorkReportSurfaceProps) {
  const text = copy.workReport;
  let body: React.ReactNode;
  if (state.status === "loading") {
    body = (
      <p role="status" className="m-0 text-compact text-meta-foreground">
        {text.loading}
      </p>
    );
  } else if (state.status === "error") {
    body = (
      <div role="alert" className={cn(BOX, "max-w-[520px]")}>
        <p className={BOX_TEXT}>{text.error}</p>
        <button
          type="button"
          onClick={onRetry}
          className={cn(BILLING_BTN, "self-start")}
        >
          {text.retry}
        </button>
      </div>
    );
  } else if (state.status === "forbidden") {
    body = (
      <div role="status" className={cn(BOX, "max-w-[520px]")}>
        <h3 className={BOX_TITLE}>{text.forbiddenTitle}</h3>
        <p className={BOX_TEXT}>{text.forbiddenBody}</p>
      </div>
    );
  } else {
    body = <Report copy={text} data={state.data} />;
  }

  return (
    <section aria-labelledby="work-report" className="flex flex-col gap-4">
      <PageHead id="work-report" title={text.title} lede={text.lede} />
      <div
        role="group"
        aria-label={text.filtersLabel}
        className="flex flex-wrap items-end gap-3"
      >
        <label className="flex flex-col gap-1 text-crumb text-meta-foreground">
          {text.board}
          <Select
            value={scope.boardId ?? ALL}
            onValueChange={(value) => onBoard(value === ALL ? null : value)}
          >
            <SelectTrigger size="sm" className={SELECT}>
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value={ALL}>{text.allBoards}</SelectItem>
              {scope.boards.map((board) => (
                <SelectItem key={board.id} value={board.id}>
                  {board.title}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </label>
        {scope.spaces ? (
          <label className="flex flex-col gap-1 text-crumb text-meta-foreground">
            {text.space}
            <Select
              value={scope.spaceId ?? ALL}
              disabled={scope.boardId !== null}
              onValueChange={(value) => onSpace(value === ALL ? null : value)}
            >
              <SelectTrigger size="sm" className={SELECT}>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value={ALL}>{text.allSpaces}</SelectItem>
                {scope.spaces.map((space) => (
                  <SelectItem key={space.id} value={space.id}>
                    {space.name}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </label>
        ) : null}
      </div>
      {body}
    </section>
  );
}
