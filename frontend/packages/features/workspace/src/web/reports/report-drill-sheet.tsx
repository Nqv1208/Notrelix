import * as React from "react";
import { cn } from "@notrelix/ui-web";
import { formatCopy } from "../shell/shell-models";
import { ReportGlyph } from "./report-glyph";
import {
  ReportAnchor,
  type ReportLinks,
} from "./report-widgets";
import {
  dayLabel,
  timeLabel,
  weekdayLabel,
  type ReportFormatters,
} from "./reports-format";
import type { ReportDrillDto } from "./reports-models";
import {
  ICON_BTN,
  NoneMark,
  PersonMark,
  PriorityFlag,
  StatusDot,
} from "./reports-parts";
import { BTN, BTN_SM, FOCUS } from "../spaces/space-page-parts";
import type { ReportsCopy } from "./reports.copy";

export type ReportDrillState =
  | { readonly status: "loading" }
  | { readonly status: "error" }
  | { readonly status: "ready"; readonly data: ReportDrillDto };

export interface ReportDrillSheetProps {
  readonly copy: ReportsCopy;
  readonly formatters: ReportFormatters;
  readonly links: ReportLinks;
  readonly state: ReportDrillState;
  readonly onClose: () => void;
  readonly onRetry: () => void;
}

/**
 * The list of items behind one workload row (Final-Reports `khoan-sau`): a
 * side sheet that does not take focus away from the page, read-only.
 */
export function ReportDrillSheet({
  copy,
  formatters,
  links,
  state,
  onClose,
  onRetry,
}: ReportDrillSheetProps) {
  const c = copy.drill;
  const closeRef = React.useRef<HTMLButtonElement>(null);
  React.useEffect(() => {
    closeRef.current?.focus();
  }, []);
  const data = state.status === "ready" ? state.data : null;
  const title = data
    ? data.person
      ? formatCopy(c.title, {
          name: data.person.name,
          count: data.items.length,
        })
      : formatCopy(c.titleNone, { count: data.items.length })
    : c.loading;
  return (
    <aside
      role="dialog"
      aria-modal="false"
      aria-labelledby="report-drill-title"
      onKeyDown={(event) => {
        if (event.key === "Escape") onClose();
      }}
      className="absolute top-0 right-0 bottom-0 z-sticky flex w-[461px] max-w-full flex-col border-l border-line bg-popover shadow-sheet motion-safe:animate-in motion-safe:slide-in-from-right-6 dark:bg-card"
    >
      <div className="flex items-start gap-2.5 border-b border-line px-5 pt-[18px] pb-3.5">
        {data ? (
          data.person ? (
            <PersonMark
              person={data.person}
              className="size-8 text-mark-md"
            />
          ) : (
            <NoneMark className="size-8 text-mark-md" />
          )
        ) : null}
        <span>
          <h2
            id="report-drill-title"
            className="m-0 text-calendar-title leading-[1.45] font-[650]"
          >
            {title}
          </h2>
          {data ? (
            <small className="mt-[3px] block text-crumb leading-[1.5] text-meta-foreground">
              {formatCopy(c.sub, {
                board: data.board.name,
                asOf: timeLabel(formatters, data.dataAsOf, data.timeZone),
              })}
            </small>
          ) : null}
        </span>
        <button
          ref={closeRef}
          type="button"
          aria-label={c.close}
          onClick={onClose}
          className={cn(ICON_BTN, "ml-auto")}
        >
          <ReportGlyph name="x" />
        </button>
      </div>
      <div className="flex flex-1 flex-col gap-2.5 overflow-auto px-5 py-4">
        {state.status === "loading" ? (
          <p role="status" className="m-0 text-crumb text-meta-foreground">
            {c.loading}
          </p>
        ) : state.status === "error" ? (
          <div role="alert" className="flex flex-col items-start gap-2">
            <p className="m-0 text-crumb text-danger-text">{c.error}</p>
            <button type="button" onClick={onRetry} className={BTN_SM}>
              {c.retry}
            </button>
          </div>
        ) : (
          <>
            {state.data.items.length === 0 ? (
              <p className="m-0 text-crumb text-meta-foreground">{c.empty}</p>
            ) : null}
            {state.data.items.map((item) => (
              <div
                key={item.id}
                className="flex flex-col gap-1.5 rounded-[10px] border border-line px-3.5 py-3"
              >
                <div className="flex items-center gap-2 text-caption tracking-normal leading-[1.45]">
                  <span className="font-mono text-meta-foreground">
                    {item.key}
                  </span>
                  <span className="inline-flex items-center gap-[5px] text-caption tracking-normal leading-[1.45] font-medium text-muted-foreground">
                    <StatusDot bucket={item.bucket} className="size-2" />
                    {item.statusLabel}
                  </span>
                </div>
                <b className="text-body leading-[1.4] font-semibold">
                  {item.title}
                </b>
                <div className="flex flex-wrap items-center gap-x-3 gap-y-1 text-crumb leading-[1.45] text-muted-foreground">
                  {item.day ? (
                    item.overdueDays > 0 ? (
                      <span className="inline-flex items-center gap-1 font-semibold text-danger-text">
                        <ReportGlyph name="clock" className="size-[13px]" />
                        {formatCopy(
                          item.overdueDays === 1 ? c.overdue_one : c.overdue_other,
                          {
                            days: item.overdueDays,
                            day: dayLabel(formatters, item.day),
                          },
                        )}
                      </span>
                    ) : (
                      <span className="inline-flex items-center gap-1">
                        <ReportGlyph name="clock" className="size-[13px]" />
                        {formatCopy(c.due, {
                          weekday: weekdayLabel(formatters, item.day),
                          day: dayLabel(formatters, item.day),
                        })}
                      </span>
                    )
                  ) : null}
                  <span className="inline-flex items-center gap-1 font-semibold">
                    <PriorityFlag priority={item.priority} />
                    {copy.priorities[item.priority]}
                  </span>
                  {item.module ? (
                    <span>{formatCopy(c.module, { name: item.module })}</span>
                  ) : null}
                  {item.awaitingViewer ? <span>{c.awaiting}</span> : null}
                </div>
                <ReportAnchor
                  link={links.item(item.boardId, item.id)}
                  className={cn(BTN_SM, "mt-1 self-start", FOCUS)}
                >
                  {c.open}
                  <ReportGlyph name="out" />
                </ReportAnchor>
              </div>
            ))}
            <div className="flex items-start gap-[9px] rounded-[8px] border border-line bg-surface-canvas px-3 py-2.5 text-crumb leading-[1.5] text-muted-foreground">
              <ReportGlyph
                name="info"
                className="mt-px size-[15px] text-meta-foreground"
              />
              <span>
                <b className="font-semibold text-foreground">
                  {c.readOnly.trim()}
                </b>
                {c.readOnlyBody}
              </span>
            </div>
          </>
        )}
      </div>
      {data ? (
        <div className="flex gap-2 border-t border-line px-5 py-3">
          <ReportAnchor
            link={links.board(data.board.id)}
            className={cn(BTN, FOCUS)}
          >
            {c.footer}
          </ReportAnchor>
        </div>
      ) : null}
    </aside>
  );
}
