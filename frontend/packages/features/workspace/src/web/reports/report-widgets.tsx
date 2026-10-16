import * as React from "react";
import { cn } from "@notrelix/ui-web";
import { formatCopy, isPlainClick } from "../shell/shell-models";
import type { WorkStatusBucket } from "../home/home-models";
import { ReportGlyph } from "./report-glyph";
import {
  dayLabel,
  decimalLabel,
  percentOf,
  rangeLabel,
  timeLabel,
  weekdayLabel,
  type ReportFormatters,
} from "./reports-format";
import {
  type ReportDueItem,
  type ReportGroupRow,
  type ReportWidgetDto,
} from "./reports-models";
import {
  LINK_BTN,
  NoneMark,
  PersonMark,
  PriorityFlag,
  ScopeMark,
  StatusDot,
  STATUS_BG,
} from "./reports-parts";
import type { ReportsCopy } from "./reports.copy";

/** A destination the host can open: a real href plus a router handoff. */
export interface ReportLink {
  readonly href: string;
  readonly onNavigate: () => void;
}

export interface ReportLinks {
  readonly list: ReportLink;
  readonly report: (reportId: string) => ReportLink;
  readonly board: (boardId: string) => ReportLink;
  readonly item: (boardId: string, itemId: string) => ReportLink;
}

/** An anchor whose plain clicks go through the host router. */
export function ReportAnchor({
  link,
  className,
  children,
  ...rest
}: {
  link: ReportLink;
  className?: string;
  children: React.ReactNode;
} & Omit<React.ComponentProps<"a">, "href" | "onClick" | "children">) {
  return (
    <a
      {...rest}
      href={link.href}
      className={className}
      onClick={(event) => {
        if (!isPlainClick(event)) return;
        event.preventDefault();
        link.onNavigate();
      }}
    >
      {children}
    </a>
  );
}

/** What every widget body needs from the page. */
export interface WidgetEnv {
  readonly copy: ReportsCopy;
  readonly formatters: ReportFormatters;
  readonly timeZone: string;
  readonly dataAsOf: string;
  readonly links: ReportLinks;
  /** The workload row whose list is open. */
  readonly drillKey: string | null;
  readonly onDrill?: (key: string) => void;
  readonly onReload?: () => void;
  readonly reloading?: boolean;
}

export function widgetTitle(widget: ReportWidgetDto, env: WidgetEnv): string {
  if (widget.title) return widget.title;
  const titles = env.copy.widget.titles as Record<string, string>;
  const template = titles[widget.titleKey ?? ""] ?? env.copy.widget.titles.note;
  const board = "boardName" in widget ? (widget.boardName ?? "") : "";
  return formatCopy(template, { board });
}

const WU =
  "m-0 mb-2.5 text-caption tracking-normal leading-[1.5] font-normal text-meta-foreground";
const AXIS =
  "grid gap-2 text-key leading-[1.45] text-meta-foreground";
const TRACK =
  "flex h-2.5 gap-0.5 overflow-hidden rounded-[3px] bg-state-hover-strong";

function Subtitle({ children }: { children: React.ReactNode }) {
  return (
    <p data-f="wu" className={WU}>
      {children}
    </p>
  );
}

function ratioWidth(count: number, max: number): string {
  return `${(count / Math.max(1, max)) * 100}%`;
}

// ── Status ──────────────────────────────────────────────────────────────

const BUCKETS: readonly WorkStatusBucket[] = [
  "todo",
  "doing",
  "review",
  "done",
];

function StatusBody({
  widget,
  env,
}: {
  widget: Extract<ReportWidgetDto, { kind: "status" }>;
  env: WidgetEnv;
}) {
  const { copy, formatters } = env;
  const parts = BUCKETS.map(
    (bucket) => `${copy.buckets[bucket]} ${widget.counts[bucket]}`,
  ).join(", ");
  const pct = percentOf(widget.counts.done, widget.total);
  return (
    <>
      <Subtitle>
        {formatCopy(copy.widget.status.subtitle, { total: widget.total })}
      </Subtitle>
      <div
        role="img"
        aria-label={formatCopy(copy.widget.status.aria, {
          parts,
          total: widget.total,
        })}
        data-f="sbar"
        className="flex h-4 gap-0.5"
      >
        {BUCKETS.map((bucket, index) => (
          <span
            key={bucket}
            className={cn(
              STATUS_BG[bucket],
              index === 0 && "rounded-l-[4px]",
              index === BUCKETS.length - 1 && "rounded-r-[4px]",
            )}
            style={{ flex: widget.counts[bucket] }}
          />
        ))}
      </div>
      <div data-f="slab" aria-hidden="true" className="mt-2 flex gap-0.5">
        {BUCKETS.map((bucket) => (
          <span
            key={bucket}
            className="flex min-w-0 flex-col"
            style={{ flex: widget.counts[bucket] }}
          >
            <b className="text-item-heading leading-5 font-[650] tabular-nums">
              {formatters.number(widget.counts[bucket])}
            </b>
            <small className="inline-flex items-center gap-[5px] text-caption tracking-normal leading-[1.45] whitespace-nowrap text-muted-foreground">
              <i
                className={cn("size-2 rounded-[2px] not-italic", STATUS_BG[bucket])}
              />
              {copy.buckets[bucket]}
            </small>
          </span>
        ))}
      </div>
      <p
        data-f="rate"
        className="mt-2 mb-0 text-caption tracking-normal leading-[1.45] text-meta-foreground"
      >
        <b className="font-semibold text-foreground">{pct}%</b>
        {formatCopy(copy.widget.status.rate, {
          done: widget.counts.done,
          total: widget.total,
        })}
      </p>
    </>
  );
}

// ── Velocity ────────────────────────────────────────────────────────────

function tickStep(yMax: number): number {
  return Math.max(1, Math.ceil(yMax / 5));
}

function VelocityBody({
  widget,
  env,
}: {
  widget: Extract<ReportWidgetDto, { kind: "velocity" }>;
  env: WidgetEnv;
}) {
  const { copy, formatters, timeZone, dataAsOf } = env;
  const c = copy.widget.velocity;
  const unit = 100 / widget.yMax;
  const step = tickStep(widget.yMax);
  const ticks = Array.from(
    { length: Math.floor(widget.yMax / step) + 1 },
    (_, index) => index * step,
  );
  const hasPace = widget.pace !== null && widget.launchDate !== null;
  const launch = widget.launchDate
    ? dayLabel(formatters, widget.launchDate)
    : "";
  const avgText = decimalLabel(formatters, widget.average);
  const paceText = widget.pace !== null ? decimalLabel(formatters, widget.pace) : "";
  const weekAria = widget.weeks
    .map((week) => {
      const range = rangeLabel(formatters, week.from, week.to);
      if (week.state === "future" || week.count === null) return null;
      return week.state === "current"
        ? formatCopy(c.ariaThisWeek, {
            range,
            count: week.count,
            asOf: timeLabel(formatters, dataAsOf, timeZone),
          })
        : `${range} ${week.count}`;
    })
    .filter((part): part is string => part !== null)
    .join(", ");
  const aria = formatCopy(c.aria, {
    weeks: weekAria,
    avg: avgText,
    pace: hasPace
      ? formatCopy(c.ariaPace, { day: launch, pace: paceText })
      : "",
  });
  const [a = 0, b = 0, d = 0] = widget.past;
  const late = widget.verdict === "late";
  return (
    <>
      <Subtitle>{c.unit}</Subtitle>
      <div
        role="img"
        aria-label={aria}
        className="grid grid-cols-[28px_minmax(0,1fr)] gap-x-1.5"
      >
        <div aria-hidden="true" className="relative h-[100px]">
          {ticks.map((tick) => (
            <span
              key={tick}
              className="absolute right-0 text-key leading-3 tabular-nums text-meta-foreground"
              style={{ bottom: tick * unit - 6 }}
            >
              {tick}
            </span>
          ))}
        </div>
        <div
          data-f="plot"
          aria-hidden="true"
          className="relative h-[101px] border-b border-line-strong"
        >
          {ticks.slice(1).map((tick) => (
            <span
              key={tick}
              className="absolute right-0 left-0 border-t border-line"
              style={{ bottom: tick * unit }}
            />
          ))}
          {hasPace ? (
            <div className="absolute top-0 right-0 bottom-0 left-1/2 border-l border-dashed border-line-strong bg-surface-canvas">
              <em className="absolute top-1 left-2 text-key leading-[1.45] not-italic text-meta-foreground">
                {c.future}
              </em>
            </div>
          ) : null}
          <div className="absolute inset-0 z-20 grid grid-cols-6">
            {widget.weeks.map((week) => (
              <span
                key={week.from}
                className="flex flex-col items-center justify-end"
              >
                {week.count !== null ? (
                  <>
                    <span
                      className={cn(
                        "mb-0.5 rounded-[3px] px-[3px] text-caption tracking-normal leading-[14px] font-semibold text-foreground tabular-nums",
                        week.state === "current"
                          ? "bg-surface-canvas"
                          : "bg-popover dark:bg-card",
                      )}
                    >
                      {week.count}
                    </span>
                    <span
                      data-f="vb"
                      className="w-[30px] rounded-t-[4px] bg-work-status-done"
                      style={{ height: week.count * unit }}
                    />
                  </>
                ) : null}
              </span>
            ))}
          </div>
          <span
            className="absolute right-1/2 left-0 z-10 border-t-2 border-dotted border-meta-foreground"
            style={{ bottom: widget.average * unit }}
          >
            <em className="absolute -top-[17px] right-1 rounded-[3px] bg-popover px-[3px] text-key leading-[1.45] not-italic text-muted-foreground dark:bg-card">
              {formatCopy(c.avg, { n: avgText })}
            </em>
          </span>
          {hasPace ? (
            <span
              className="absolute right-0 left-1/2 z-10 border-t-2 border-dashed border-foreground"
              style={{ bottom: (widget.pace ?? 0) * unit }}
            >
              <em className="absolute -top-[18px] right-1 rounded-[3px] bg-surface-canvas px-[3px] text-key leading-[1.45] font-semibold not-italic text-foreground">
                {formatCopy(c.pace, { n: paceText })}
              </em>
            </span>
          ) : null}
        </div>
        <div
          data-f="xax"
          aria-hidden="true"
          className="col-start-2 mt-[5px] grid grid-cols-6"
        >
          {widget.weeks.map((week, index) => (
            <span
              key={week.from}
              className={cn(
                "text-center text-micro leading-[15px] tabular-nums",
                week.state === "current"
                  ? "font-semibold text-foreground"
                  : "text-muted-foreground",
              )}
            >
              {rangeLabel(formatters, week.from, week.to)}
              {week.state === "current" ? (
                <small className="block text-key leading-[15px] font-normal text-meta-foreground">
                  {c.thisWeek}
                </small>
              ) : hasPace && index === widget.weeks.length - 1 ? (
                <small className="block text-key leading-[15px] font-normal text-meta-foreground">
                  {c.launch}
                </small>
              ) : null}
            </span>
          ))}
        </div>
      </div>
      <div
        data-f="lg"
        aria-hidden="true"
        className="mt-2 flex flex-wrap gap-x-3.5 gap-y-1 text-micro leading-[1.45] text-meta-foreground"
      >
        <span className="inline-flex items-center gap-1.5">
          <i className="size-2.5 rounded-[2px] bg-work-status-done" />
          {c.legendDone}
        </span>
        <span className="inline-flex items-center gap-1.5">
          <i className="h-0 w-4 border-t-2 border-dotted border-meta-foreground" />
          {c.legendAvg}
        </span>
        {hasPace ? (
          <span className="inline-flex items-center gap-1.5">
            <i className="h-0 w-4 border-t-2 border-dashed border-foreground" />
            {formatCopy(c.legendPace, { day: launch })}
          </span>
        ) : null}
      </div>
      {hasPace ? (
        <div
          role="note"
          data-f="obs"
          className={cn(
            "mt-2.5 flex items-start gap-[9px] rounded-[8px] px-3 py-2 text-crumb leading-[1.5] text-foreground",
            late ? "bg-warning-subtle" : "bg-success-subtle",
          )}
        >
          <ReportGlyph
            name={late ? "warn" : "check"}
            className={cn(
              "mt-0.5 size-[15px]",
              late ? "text-warning-text" : "text-success-text",
            )}
          />
          <span>
            <b
              className={cn(
                "font-[650]",
                late ? "text-warning-text" : "text-success-text",
              )}
            >
              {late ? c.late : c.ontrack}
            </b>
            {formatCopy(late ? c.lateBody : c.ontrackBody, {
              avg: avgText,
              a,
              b,
              c: d,
              open: widget.openCount,
              weeks: widget.weeksLeft ?? 0,
              day: launch,
              pace: paceText,
            })}
          </span>
        </div>
      ) : null}
    </>
  );
}

// ── Bars (module, priority, department) ─────────────────────────────────

function BarsBody({
  widget,
  env,
}: {
  widget: Extract<ReportWidgetDto, { kind: "bars" }>;
  env: WidgetEnv;
}) {
  const { copy, formatters, timeZone } = env;
  const b = copy.widget.bars;
  const dept = widget.variant === "department";
  const cols = dept
    ? "grid-cols-[130px_minmax(0,1fr)_22px]"
    : "grid-cols-[92px_minmax(0,1fr)_22px]";
  const labelOf = (row: ReportGroupRow) =>
    row.none
      ? b.moduleNone
      : row.priority
        ? copy.priorities[row.priority]
        : row.label;
  const parts = widget.rows
    .map((row) => `${labelOf(row)} ${row.count}`)
    .join(", ");
  const week =
    widget.weekFrom && widget.weekTo
      ? rangeLabel(formatters, widget.weekFrom, widget.weekTo)
      : "";
  const asOf = widget.staleAsOf
    ? timeLabel(formatters, widget.staleAsOf, timeZone)
    : timeLabel(formatters, env.dataAsOf, timeZone);
  const subtitle =
    widget.subtitle === "open"
      ? formatCopy(b.moduleSubtitle, { total: widget.total })
      : widget.subtitle === "all"
        ? formatCopy(b.prioritySubtitle, { total: widget.total })
        : widget.subtitle === "week-done"
          ? formatCopy(b.doneSubtitle, { range: week, total: widget.total })
          : b.overdueSubtitle;
  const aria =
    widget.subtitle === "open"
      ? formatCopy(b.moduleAria, { parts })
      : widget.subtitle === "all"
        ? formatCopy(b.priorityAria, { parts })
        : widget.subtitle === "week-done"
          ? formatCopy(b.doneAria, { range: week, parts })
          : formatCopy(b.overdueAria, { asOf, parts });
  const fill = widget.tone === "danger" ? "bg-danger" : "bg-primary";
  return (
    <>
      <Subtitle>{subtitle}</Subtitle>
      {widget.staleAsOf ? (
        <div
          role="status"
          className="mb-3 flex flex-wrap items-center gap-2 rounded-[8px] border border-line bg-surface-canvas px-2.5 py-[7px] text-caption tracking-normal leading-[1.45] text-muted-foreground"
        >
          <span
            aria-hidden="true"
            className="box-border size-[13px] shrink-0 rounded-full border-2 border-line-strong border-t-primary motion-safe:animate-spin"
          />
          <span>
            <b className="font-semibold text-foreground">{b.stale}</b>
            {formatCopy(b.staleBody, { asOf })}
          </span>
          {env.onReload ? (
            <button
              type="button"
              onClick={env.onReload}
              disabled={env.reloading}
              className={cn(LINK_BTN, "ml-auto")}
            >
              <ReportGlyph name="refresh" className="size-[13px]" />
              {copy.page.reload}
            </button>
          ) : null}
        </div>
      ) : null}
      <div
        role="img"
        aria-label={aria}
        className={cn(
          "flex flex-col",
          widget.subtitle === "week-done" ? "gap-[9px]" : "gap-[7px]",
        )}
      >
        {widget.rows.map((row) => (
          <div
            key={row.key}
            data-f="hr"
            className={cn(
              "grid items-center gap-2 text-crumb leading-[1.45]",
              cols,
            )}
          >
            <span
              className={cn(
                "inline-flex items-center gap-1.5 overflow-hidden whitespace-nowrap",
                row.none && "text-meta-foreground",
              )}
            >
              {row.priority ? <PriorityFlag priority={row.priority} /> : null}
              {dept && row.tone ? (
                <ScopeMark label={row.label} tone={row.tone} small />
              ) : null}
              {labelOf(row)}
            </span>
            <span className={TRACK}>
              {row.count > 0 ? (
                <span
                  className={cn(
                    "h-full",
                    row.none ? "bg-meta-foreground" : fill,
                  )}
                  style={{ width: ratioWidth(row.count, widget.max) }}
                />
              ) : null}
            </span>
            <span className="text-right font-semibold tabular-nums">
              {row.count}
            </span>
          </div>
        ))}
      </div>
      <div data-f="hax" aria-hidden="true" className={cn(AXIS, "mt-1", cols)}>
        <span />
        <span className="flex justify-between">
          <span>0</span>
          <span>
            {formatCopy(copy.widget.axisItems, { max: widget.max })}
          </span>
        </span>
        <span />
      </div>
      {widget.subtitle === "week-done" ? (
        <div className="mt-2.5 flex items-center gap-2 text-caption tracking-normal leading-[1.45] text-meta-foreground">
          {b.doneFoot}
        </div>
      ) : null}
    </>
  );
}

// ── Due list and table ──────────────────────────────────────────────────

function DueRow({
  item,
  env,
}: {
  item: ReportDueItem;
  env: WidgetEnv;
}) {
  const { formatters } = env;
  return (
    <ReportAnchor
      data-f="drrow"
      link={env.links.item(item.boardId, item.id)}
      className="-mx-2 grid grid-cols-[64px_minmax(0,1fr)_auto] items-center gap-2.5 rounded-[8px] border-t border-line px-2 py-2 first:border-t-0 hover:bg-state-hover"
    >
      <span className="font-mono text-caption tracking-normal leading-[1.45] text-meta-foreground">
        {item.key}
      </span>
      <span className="min-w-0">
        <b className="block truncate text-compact leading-[1.45] font-medium">
          {item.title}
        </b>
        <small className="flex items-center gap-[5px] text-caption tracking-normal leading-[1.45] text-meta-foreground">
          <StatusDot bucket={item.bucket} />
          {item.statusLabel}
          {item.assignee ? ` · ${item.assignee.name}` : null}
        </small>
      </span>
      {item.day ? (
        <span className="text-right text-crumb leading-[1.45] font-semibold whitespace-nowrap tabular-nums">
          {dayLabel(formatters, item.day)}
          <small className="block text-micro font-normal text-meta-foreground">
            {weekdayLabel(formatters, item.day)}
          </small>
        </span>
      ) : (
        <span />
      )}
    </ReportAnchor>
  );
}

function DueBody({
  widget,
  env,
}: {
  widget: Extract<ReportWidgetDto, { kind: "due" | "table" }>;
  env: WidgetEnv;
}) {
  const { copy, formatters } = env;
  const c = copy.widget.due;
  const range =
    widget.from && widget.to ? rangeLabel(formatters, widget.from, widget.to) : "";
  return (
    <>
      <Subtitle>
        {widget.kind === "due"
          ? formatCopy(c.subtitle, { range, count: widget.items.length })
          : formatCopy(c.tableSubtitle, { count: widget.items.length })}
      </Subtitle>
      {widget.items.length === 0 ? (
        <p className="m-0 text-crumb leading-[1.5] text-meta-foreground">
          {c.empty}
        </p>
      ) : (
        <div className="flex flex-col">
          {widget.items.map((item) => (
            <DueRow key={item.id} item={item} env={env} />
          ))}
        </div>
      )}
    </>
  );
}

// ── Workload ────────────────────────────────────────────────────────────

const WK_COLS = "grid-cols-[22px_112px_minmax(0,1fr)_84px]";

export function workloadKey(row: { key: string }): string {
  return row.key;
}

function WorkloadBody({
  widget,
  env,
}: {
  widget: Extract<ReportWidgetDto, { kind: "workload" }>;
  env: WidgetEnv;
}) {
  const { copy } = env;
  const c = copy.widget.workload;
  const ticks = Array.from(
    { length: Math.min(widget.max, 5) },
    (_, index) => index,
  );
  return (
    <>
      <Subtitle>{formatCopy(c.subtitle, { total: widget.total })}</Subtitle>
      <div
        data-f="wkh"
        aria-hidden="true"
        className={cn(AXIS, "px-1.5 pb-1", WK_COLS)}
      >
        <span />
        <span />
        <span className="flex justify-between">
          {ticks.map((tick) => (
            <span key={tick}>{tick}</span>
          ))}
          <span>{formatCopy(c.itemsAxis, { max: widget.max })}</span>
        </span>
        <span />
      </div>
      <div role="list" aria-label={c.label}>
        {widget.rows.map((row) => {
          const name = row.person?.name ?? copy.widget.unassigned;
          const selected = env.drillKey === row.key;
          return (
            <div key={row.key} role="listitem">
              <button
                type="button"
                data-f="wk"
                aria-expanded={selected}
                aria-label={
                  row.person
                    ? formatCopy(c.aria, {
                        name,
                        count: row.open,
                        overdue:
                          row.overdue > 0
                            ? formatCopy(c.ariaOverdue, { count: row.overdue })
                            : "",
                      })
                    : formatCopy(c.ariaNone, { count: row.open })
                }
                onClick={() => env.onDrill?.(row.key)}
                className={cn(
                  "grid min-h-7 w-full cursor-pointer items-center gap-2 rounded-[7px] border-0 bg-transparent px-1.5 text-left text-compact leading-[1.45] text-foreground hover:bg-state-hover",
                  WK_COLS,
                  selected &&
                    "bg-brand-subtle shadow-[inset_0_0_0_1px_var(--color-primary)] hover:bg-brand-subtle",
                )}
              >
                {row.person ? (
                  <PersonMark
                    person={row.person}
                    className="size-[22px] text-avatar-sm"
                  />
                ) : (
                  <NoneMark />
                )}
                <span
                  className={cn(
                    "truncate",
                    !row.person && "text-muted-foreground",
                  )}
                >
                  {name}
                </span>
                <span className={TRACK}>
                  {row.open - row.overdue > 0 ? (
                    <span
                      className={cn(
                        "h-full",
                        row.person ? "bg-primary" : "bg-meta-foreground",
                      )}
                      style={{
                        width: `calc(${ratioWidth(row.open - row.overdue, widget.max)} - ${row.overdue > 0 ? 1 : 0}px)`,
                      }}
                    />
                  ) : null}
                  {row.overdue > 0 ? (
                    <span
                      className="h-full bg-danger"
                      style={{
                        width: `calc(${ratioWidth(row.overdue, widget.max)} - 1px)`,
                      }}
                    />
                  ) : null}
                </span>
                <span className="text-right text-caption tracking-normal leading-[1.45] whitespace-nowrap text-meta-foreground tabular-nums">
                  <b className="font-semibold text-foreground">{row.open}</b>
                  {row.overdue > 0 ? (
                    <>
                      {" · "}
                      <span className="font-semibold text-danger-text">
                        {formatCopy(c.overdue, { count: row.overdue })}
                      </span>
                    </>
                  ) : null}
                </span>
              </button>
            </div>
          );
        })}
      </div>
      <div
        data-f="lg"
        aria-hidden="true"
        className="mt-2 flex flex-wrap gap-x-3.5 gap-y-1 text-micro leading-[1.45] text-meta-foreground"
      >
        <span className="inline-flex items-center gap-1.5">
          <i className="size-2.5 rounded-[2px] bg-primary" />
          {c.legendOpen}
        </span>
        <span className="inline-flex items-center gap-1.5">
          <i className="size-2.5 rounded-[2px] bg-danger" />
          {c.legendOverdue}
        </span>
        <span className="inline-flex items-center gap-1.5">
          <i className="size-2.5 rounded-[2px] bg-meta-foreground" />
          {c.legendNone}
        </span>
      </div>
    </>
  );
}

// ── Note, progress ──────────────────────────────────────────────────────

function NoteBody({
  widget,
  env,
}: {
  widget: Extract<ReportWidgetDto, { kind: "note" }>;
  env: WidgetEnv;
}) {
  const c = env.copy.widget.note;
  return (
    <>
      {widget.author && widget.at ? (
        <p
          data-f="wu"
          className="m-0 mb-2 text-nav leading-[1.6] font-normal text-foreground"
        >
          {formatCopy(c.by, {
            name: widget.author.name,
            day: dayLabel(env.formatters, widget.at),
          })}
        </p>
      ) : null}
      {widget.paragraphs.length === 0 ? (
        <p className="m-0 text-crumb leading-[1.5] text-meta-foreground">
          {c.empty}
        </p>
      ) : (
        widget.paragraphs.map((paragraph, index) => (
          <p
            key={index}
            className="m-0 mb-2 text-nav leading-[1.6] text-foreground last:mb-0"
          >
            {paragraph}
          </p>
        ))
      )}
    </>
  );
}

function ProgressBody({
  widget,
  env,
}: {
  widget: Extract<ReportWidgetDto, { kind: "progress" }>;
  env: WidgetEnv;
}) {
  const { copy, formatters, timeZone, dataAsOf } = env;
  const c = copy.widget.progress;
  const pct = percentOf(widget.done, widget.total);
  const asOf = timeLabel(formatters, dataAsOf, timeZone);
  const board = widget.boardName ?? "";
  return (
    <>
      <Subtitle>
        {formatCopy(widget.link ? c.subtitle : c.subtitleBare, {
          board,
          asOf,
        })}
      </Subtitle>
      <div
        role="img"
        aria-label={formatCopy(c.aria, {
          done: widget.done,
          total: widget.total,
          pct,
        })}
        className="flex items-center gap-2.5 text-compact leading-[1.45]"
      >
        <span className={cn(TRACK, "flex-1")}>
          <span
            className="h-full rounded-[3px] bg-work-status-done"
            style={{ width: `${pct}%` }}
          />
        </span>
        <b className="font-semibold tabular-nums">
          {widget.done}/{widget.total} · {pct}%
        </b>
      </div>
      {widget.launchDate !== null || widget.link ? (
        <div className="mt-2.5 flex items-center gap-2 text-caption tracking-normal leading-[1.45] text-meta-foreground">
          {widget.launchDate !== null && widget.daysLeft !== null ? (
            <span>
              {formatCopy(c.foot, {
                day: dayLabel(formatters, widget.launchDate),
                days: widget.daysLeft,
              })}
              {widget.overdue > 0 ? (
                <span className="font-semibold text-danger-text">
                  {formatCopy(c.footOverdue, { count: widget.overdue })}
                </span>
              ) : null}
            </span>
          ) : null}
          {widget.link && widget.boardId ? (
            <ReportAnchor
              link={env.links.board(widget.boardId)}
              className="ml-auto text-crumb leading-[1.45] font-semibold text-brand-ink hover:underline"
            >
              {c.open}
            </ReportAnchor>
          ) : null}
        </div>
      ) : null}
    </>
  );
}

// ── Custom widgets: number, columns, line, donut ────────────────────────

function groupLabel(row: ReportGroupRow, env: WidgetEnv): string {
  if (row.none) return env.copy.widget.unassigned;
  if (row.priority) return env.copy.priorities[row.priority];
  if (row.bucket) return env.copy.buckets[row.bucket];
  return row.label;
}

function metricLabel(
  metric: "count" | "overdue",
  env: WidgetEnv,
): string {
  return env.copy.add.metrics[metric];
}

function NumberBody({
  widget,
  env,
}: {
  widget: Extract<ReportWidgetDto, { kind: "number" }>;
  env: WidgetEnv;
}) {
  const c = env.copy.widget.custom;
  return (
    <>
      <Subtitle>
        {formatCopy(c.subtitle, {
          metric: metricLabel(widget.metric, env),
          total: widget.value,
          asOf: timeLabel(env.formatters, env.dataAsOf, env.timeZone),
        })}
      </Subtitle>
      <p className="m-0 flex items-baseline gap-2 text-foreground">
        <b className="text-item-title leading-[1.15] font-[650] tabular-nums">
          {env.formatters.number(widget.value)}
        </b>
        <span className="text-crumb leading-[1.45] text-meta-foreground">
          {formatCopy(c.numberOf, { total: widget.of })}
        </span>
      </p>
    </>
  );
}

function rowName(
  row: ReportGroupRow,
  groupBy: string,
  env: WidgetEnv,
): string {
  if (groupBy === "week")
    return rangeLabel(
      env.formatters,
      row.key,
      new Date(Date.parse(`${row.key}T00:00:00Z`) + 6 * 86_400_000)
        .toISOString()
        .slice(0, 10),
    );
  return groupLabel(row, env);
}

function GroupedBody({
  widget,
  env,
}: {
  widget: Extract<ReportWidgetDto, { kind: "columns" | "line" | "donut" }>;
  env: WidgetEnv;
}) {
  const c = env.copy.widget.custom;
  const rows = widget.rows;
  const unit = 80 / Math.max(1, widget.max);
  const parts = rows
    .map((row) => `${rowName(row, widget.groupBy, env)} ${row.count}`)
    .join(", ");
  const subtitle = formatCopy(c.subtitle, {
    metric: metricLabel(widget.metric, env),
    total: widget.total,
    asOf: timeLabel(env.formatters, env.dataAsOf, env.timeZone),
  });
  if (widget.kind === "donut") {
    const total = Math.max(1, rows.reduce((sum, row) => sum + row.count, 0));
    const radius = 15.9155;
    let offset = 0;
    return (
      <>
        <Subtitle>{subtitle}</Subtitle>
        <div role="img" aria-label={parts} className="flex items-center gap-4">
          <svg viewBox="0 0 36 36" className="size-[120px] shrink-0 -rotate-90">
            <circle
              cx="18"
              cy="18"
              r={radius}
              fill="none"
              strokeWidth="5"
              className="stroke-state-hover-strong"
            />
            {rows.map((row, index) => {
              const share = (row.count / total) * 100;
              const dash = (
                <circle
                  key={row.key}
                  cx="18"
                  cy="18"
                  r={radius}
                  fill="none"
                  strokeWidth="5"
                  strokeDasharray={`${share} ${100 - share}`}
                  strokeDashoffset={-offset}
                  className={SLICE_STROKE[index % SLICE_STROKE.length]}
                />
              );
              offset += share;
              return dash;
            })}
          </svg>
          <ul className="m-0 flex min-w-0 list-none flex-col gap-1 p-0 text-crumb leading-[1.45]">
            {rows.map((row, index) => (
              <li key={row.key} className="flex items-center gap-1.5">
                <i
                  aria-hidden="true"
                  className={cn(
                    "size-2.5 shrink-0 rounded-[2px]",
                    SLICE_FILL[index % SLICE_FILL.length],
                  )}
                />
                <span className="truncate">
                  {rowName(row, widget.groupBy, env)}
                </span>
                <b className="ml-auto font-semibold tabular-nums">
                  {row.count}
                </b>
              </li>
            ))}
          </ul>
        </div>
      </>
    );
  }
  if (widget.kind === "line") {
    const width = 300;
    const step = rows.length > 1 ? width / (rows.length - 1) : width;
    const points = rows
      .map(
        (row, index) =>
          `${index * step},${90 - (row.count / Math.max(1, widget.max)) * 80}`,
      )
      .join(" ");
    return (
      <>
        <Subtitle>{subtitle}</Subtitle>
        {rows.length < 2 ? (
          <p className="m-0 text-crumb leading-[1.5] text-meta-foreground">
            {c.lineEmpty}
          </p>
        ) : (
          <div role="img" aria-label={parts}>
            <svg viewBox="-8 0 316 100" className="h-[110px] w-full">
              <line
                x1="-8"
                x2="308"
                y1="90"
                y2="90"
                className="stroke-line-strong"
                strokeWidth="1"
              />
              <polyline
                points={points}
                fill="none"
                strokeWidth="2"
                strokeLinejoin="round"
                strokeLinecap="round"
                className="stroke-primary"
              />
              {rows.map((row, index) => (
                <circle
                  key={row.key}
                  cx={index * step}
                  cy={90 - (row.count / Math.max(1, widget.max)) * 80}
                  r="3"
                  className="fill-primary"
                />
              ))}
            </svg>
            <div
              aria-hidden="true"
              className="mt-1 grid text-micro leading-[15px] text-muted-foreground"
              style={{
                gridTemplateColumns: `repeat(${rows.length}, minmax(0, 1fr))`,
              }}
            >
              {rows.map((row) => (
                <span key={row.key} className="text-center tabular-nums">
                  {rowName(row, widget.groupBy, env)}
                  <b className="block text-crumb font-semibold text-foreground">
                    {row.count}
                  </b>
                </span>
              ))}
            </div>
          </div>
        )}
      </>
    );
  }
  return (
    <>
      <Subtitle>{subtitle}</Subtitle>
      <div role="img" aria-label={parts} className="grid grid-cols-[16px_minmax(0,1fr)] gap-x-1.5">
        <div aria-hidden="true" className="relative h-24">
          <span
            className="absolute right-0 text-key leading-[11px] tabular-nums text-meta-foreground"
            style={{ bottom: -5 }}
          >
            0
          </span>
          <span
            className="absolute right-0 text-key leading-[11px] tabular-nums text-meta-foreground"
            style={{ bottom: 80 - 5 }}
          >
            {widget.max}
          </span>
        </div>
        <div
          aria-hidden="true"
          className="relative grid h-24 items-end border-b border-line-strong"
          style={{
            gridTemplateColumns: `repeat(${Math.max(1, rows.length)}, minmax(0, 1fr))`,
          }}
        >
          <span
            className="absolute right-0 left-0 border-t border-line"
            style={{ bottom: 80 }}
          />
          {rows.map((row) => (
            <span
              key={row.key}
              className="relative z-10 flex h-full flex-col items-center justify-end"
            >
              <span className="mb-0.5 text-key leading-[1.45] tabular-nums">
                {row.count}
              </span>
              <span
                className={cn(
                  "w-4",
                  row.none
                    ? "bg-meta-foreground"
                    : widget.metric === "overdue"
                      ? "bg-danger"
                      : "bg-primary",
                )}
                style={{ height: row.count * unit }}
              />
            </span>
          ))}
        </div>
        <div
          aria-hidden="true"
          className="col-start-2 mt-[5px] grid justify-items-center"
          style={{
            gridTemplateColumns: `repeat(${Math.max(1, rows.length)}, minmax(0, 1fr))`,
          }}
        >
          {rows.map((row) =>
            widget.groupBy === "assignee" ? (
              row.person ? (
                <PersonMark
                  key={row.key}
                  person={row.person}
                  className="size-5 text-mark-3xs"
                />
              ) : (
                <NoneMark key={row.key} className="size-5 text-mark-3xs" />
              )
            ) : (
              <span
                key={row.key}
                className="max-w-full truncate text-key leading-[1.45] text-muted-foreground"
              >
                {rowName(row, widget.groupBy, env)}
              </span>
            ),
          )}
        </div>
      </div>
    </>
  );
}

const SLICE_STROKE = [
  "stroke-primary",
  "stroke-work-status-done",
  "stroke-work-status-review",
  "stroke-danger",
  "stroke-meta-foreground",
  "stroke-work-status-doing",
] as const;
const SLICE_FILL = [
  "bg-primary",
  "bg-work-status-done",
  "bg-work-status-review",
  "bg-danger",
  "bg-meta-foreground",
  "bg-work-status-doing",
] as const;

/** The body of any widget kind. */
export function ReportWidgetBody({
  widget,
  env,
}: {
  widget: ReportWidgetDto;
  env: WidgetEnv;
}) {
  switch (widget.kind) {
    case "status":
      return <StatusBody widget={widget} env={env} />;
    case "velocity":
      return <VelocityBody widget={widget} env={env} />;
    case "bars":
      return <BarsBody widget={widget} env={env} />;
    case "due":
    case "table":
      return <DueBody widget={widget} env={env} />;
    case "workload":
      return <WorkloadBody widget={widget} env={env} />;
    case "note":
      return <NoteBody widget={widget} env={env} />;
    case "progress":
      return <ProgressBody widget={widget} env={env} />;
    case "number":
      return <NumberBody widget={widget} env={env} />;
    case "columns":
    case "line":
    case "donut":
      return <GroupedBody widget={widget} env={env} />;
  }
}

/** How a widget's numbers are counted, for the definition dialog. */
export function widgetDefinition(
  widget: ReportWidgetDto,
  env: WidgetEnv,
): string {
  const defs = env.copy.widget.definitions;
  switch (widget.kind) {
    case "status":
      return defs.status;
    case "velocity":
      return defs.velocity;
    case "due":
      return defs.due;
    case "workload":
      return defs.workload;
    case "note":
      return defs.note;
    case "progress":
      return defs.erpProgress;
    case "bars":
      return widget.variant === "module"
        ? defs.module
        : widget.variant === "priority"
          ? defs.priority
          : widget.subtitle === "week-done"
            ? defs.deptDone
            : defs.deptOverdue;
    default:
      return defs.custom;
  }
}
