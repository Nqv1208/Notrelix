import * as React from "react";
import { cn } from "@notrelix/ui-web";
import { formatCopy } from "../shell/shell-models";
import { BTN_PRIMARY, BTN_SM, FOCUS } from "../spaces/space-page-parts";
import type { SectionState } from "../home/home-models";
import { ReportGlyph } from "./report-glyph";
import { ReportAnchor, type ReportLinks } from "./report-widgets";
import {
  dayLabel,
  dayOfMoment,
  sameDay,
  timeLabel,
  type ReportFormatters,
} from "./reports-format";
import type { ReportListFilter, ReportListItemDto } from "./reports-models";
import { PersonMark, ScopeChip } from "./reports-parts";
import type { ReportsCopy } from "./reports.copy";

export interface ReportListSurfaceProps {
  readonly copy: ReportsCopy;
  readonly formatters: ReportFormatters;
  readonly links: ReportLinks;
  readonly workspaceName: string | null;
  readonly state: SectionState<readonly ReportListItemDto[]>;
  /** The "now" the "today" of an update is measured against (ISO). */
  readonly now: string;
  readonly timeZone: string;
  readonly filter: ReportListFilter;
  readonly onFilter: (filter: ReportListFilter) => void;
  readonly creating: boolean;
  readonly createFailed: boolean;
  readonly onCreate: () => void;
  readonly onRetry: () => void;
}

const COLS = "grid-cols-[minmax(0,1fr)_170px_190px_130px_120px]";
const FILTERS: readonly ReportListFilter[] = ["mine", "shared", "all"];

function subtitleOf(copy: ReportsCopy, report: ReportListItemDto): string {
  const c = copy.list;
  const parts: string[] = [];
  parts.push(
    report.multiSource
      ? c.sourceMany
      : report.sourceBoardName
        ? formatCopy(c.source, { board: report.sourceBoardName })
        : "",
  );
  if (report.widgetCount !== null)
    parts.push(formatCopy(c.widgets, { count: report.widgetCount }));
  if (report.excludedBoardCount > 0) {
    parts.push(
      formatCopy(
        report.excludedBoardCount === 1 ? c.excluded_one : c.excluded_other,
        { count: report.excludedBoardCount },
      ),
    );
  } else if (report.sharedWith.length > 0) {
    parts.push(
      formatCopy(report.includesViewer ? c.sharedWithYou : c.sharedWith, {
        names: report.sharedWith.join(", "),
      }),
    );
  }
  return parts.filter(Boolean).join(" · ");
}

/** The reports a person sees (Final-Reports `danh-sach`). */
export function ReportListSurface({
  copy,
  formatters,
  links,
  workspaceName,
  state,
  now,
  timeZone,
  filter,
  onFilter,
  creating,
  createFailed,
  onCreate,
  onRetry,
}: ReportListSurfaceProps) {
  const c = copy.list;
  const all = state.status === "ready" ? state.data : [];
  const counts = {
    mine: all.filter((report) => report.mine).length,
    shared: all.filter((report) => !report.mine).length,
    all: all.length,
  } as const;
  const rows = all.filter((report) =>
    filter === "mine" ? report.mine : filter === "shared" ? !report.mine : true,
  );
  return (
    <div
      data-f="reports-list"
      className="flex h-full min-h-0 flex-col bg-surface-canvas text-body leading-[1.45] font-normal text-foreground"
    >
      <div className="flex-none px-7 pt-5">
        <div className="flex flex-wrap items-center gap-3.5">
          <div className="flex flex-col">
            <small className="text-crumb leading-[1.45] text-meta-foreground">
              {workspaceName}
            </small>
            <h1 className="m-0 text-page-title leading-[1.25] font-[650] tracking-[-0.015em]">
              {c.title}
            </h1>
          </div>
          <div className="ml-auto flex items-center gap-2">
            <button
              type="button"
              aria-busy={creating ? true : undefined}
              aria-disabled={creating ? true : undefined}
              onClick={() => {
                if (!creating) onCreate();
              }}
              className={BTN_PRIMARY}
            >
              <ReportGlyph name="plus" />
              {c.create}
            </button>
          </div>
        </div>
        <p className="mt-1.5 mb-0 max-w-[80ch] text-nav leading-[1.45] font-normal text-muted-foreground">
          {c.lede}
        </p>
      </div>
      <div className="flex flex-none flex-wrap items-center gap-2 px-7 pt-4 pb-2.5">
        <div
          role="group"
          aria-label={c.filterLabel}
          className="inline-flex gap-0.5 rounded-[9px] bg-state-hover-strong p-[3px]"
        >
          {FILTERS.map((item) => (
            <button
              key={item}
              type="button"
              aria-pressed={filter === item}
              onClick={() => onFilter(item)}
              className={cn(
                "inline-flex h-7 cursor-pointer items-center gap-1.5 rounded-[7px] border-0 bg-transparent px-3 text-compact leading-[1.45] font-medium text-muted-foreground",
                filter === item &&
                  "bg-popover font-semibold text-foreground shadow-segment dark:bg-card",
                FOCUS,
              )}
            >
              {c.tabs[item]}
              <span
                className={cn(
                  "font-normal",
                  filter === item
                    ? "text-meta-foreground"
                    : "text-meta-foreground",
                )}
              >
                {state.status === "ready" ? counts[item] : ""}
              </span>
            </button>
          ))}
        </div>
        <span className="ml-auto inline-flex items-center gap-1.5 text-caption tracking-normal leading-[1.45] text-meta-foreground">
          {c.basis}
        </span>
      </div>
      <div className="flex min-h-0 flex-1 flex-col gap-3.5 overflow-auto px-7 pt-1 pb-7">
        {state.status === "loading" ? (
          <p role="status" className="m-0 text-compact text-meta-foreground">
            {c.loading}
          </p>
        ) : state.status === "error" ? (
          <div
            role="alert"
            className="flex flex-col items-start gap-2 rounded-[12px] border border-line bg-popover p-4 dark:bg-card"
          >
            <b className="text-nav font-semibold">{c.errorTitle}</b>
            <span className="text-compact text-meta-foreground">
              {c.errorBody}
            </span>
            <button type="button" onClick={onRetry} className={BTN_SM}>
              {c.retry}
            </button>
          </div>
        ) : rows.length === 0 ? (
          <div className="flex flex-col items-start gap-2 rounded-[12px] border border-dashed border-line-strong bg-popover p-5 dark:bg-card">
            <b className="text-nav font-semibold">
              {all.length === 0 ? c.emptyTitle : c.emptyFilter}
            </b>
            {all.length === 0 ? (
              <span className="max-w-[60ch] text-compact leading-[1.5] text-meta-foreground">
                {c.emptyBody}
              </span>
            ) : null}
          </div>
        ) : (
          <div
            role="table"
            aria-label={c.tableLabel}
            className="overflow-hidden rounded-[12px] border border-line bg-popover dark:bg-card"
          >
            <div
              role="row"
              className={cn(
                "grid h-[34px] items-center gap-3.5 border-b border-line bg-surface-canvas px-4 text-micro leading-[1.45] font-semibold text-meta-foreground",
                COLS,
              )}
            >
              <span role="columnheader">{c.columns.report}</span>
              <span role="columnheader">{c.columns.scope}</span>
              <span role="columnheader">{c.columns.owner}</span>
              <span role="columnheader">{c.columns.access}</span>
              <span role="columnheader" className="text-right">
                {c.columns.updated}
              </span>
            </div>
            {rows.map((report) => (
              <ReportAnchor
                key={report.id}
                link={links.report(report.id)}
                role="row"
                className={cn(
                  "box-content grid min-h-16 items-center gap-3.5 border-t border-line px-4 first-of-type:border-t-0 hover:bg-surface-canvas",
                  COLS,
                  FOCUS,
                )}
              >
                <span role="cell" className="flex min-w-0 items-center gap-2.5">
                  <span
                    aria-hidden="true"
                    className="grid size-8 shrink-0 place-items-center rounded-[8px] bg-brand-subtle text-brand-ink"
                  >
                    <ReportGlyph name="bars" />
                  </span>
                  <span className="min-w-0">
                    <b className="block truncate text-body leading-[1.45] font-semibold">
                      {report.name ?? c.unnamed}
                    </b>
                    <small className="block text-caption tracking-normal leading-[1.45] text-meta-foreground">
                      {subtitleOf(copy, report)}
                    </small>
                  </span>
                </span>
                <span role="cell">
                  <ScopeChip
                    label={report.scope.label}
                    tone={report.scope.tone}
                  />
                </span>
                <span
                  role="cell"
                  className="flex items-center gap-2 text-compact leading-[1.45]"
                >
                  {report.owner ? (
                    <>
                      <PersonMark
                        person={report.owner}
                        className="size-[22px] text-avatar-sm"
                      />
                      {report.owner.name}
                    </>
                  ) : null}
                </span>
                <span role="cell">
                  <span
                    className={cn(
                      "inline-flex items-center gap-[5px] text-crumb leading-[1.45] font-medium text-muted-foreground",
                      report.access === "edit" &&
                        "font-semibold text-brand-ink",
                    )}
                  >
                    <ReportGlyph
                      name={report.access === "edit" ? "edit" : "eye"}
                      className="size-[13px]"
                    />
                    {report.access === "edit" ? c.canEdit : c.viewOnly}
                  </span>
                </span>
                <span
                  role="cell"
                  className="text-right text-caption tracking-normal leading-[1.45] text-meta-foreground tabular-nums"
                >
                  {sameDay(report.updatedAt, now, timeZone)
                    ? formatCopy(c.updatedToday, {
                        time: timeLabel(formatters, report.updatedAt, timeZone),
                      })
                    : dayLabel(
                        formatters,
                        dayOfMoment(report.updatedAt, timeZone),
                      )}
                </span>
              </ReportAnchor>
            ))}
          </div>
        )}
        {createFailed ? (
          <p role="alert" className="m-0 text-crumb text-danger-text">
            {c.createFailed}
          </p>
        ) : null}
        <p className="m-0 text-crumb leading-[1.55] text-meta-foreground">
          {c.note}
        </p>
      </div>
    </div>
  );
}
