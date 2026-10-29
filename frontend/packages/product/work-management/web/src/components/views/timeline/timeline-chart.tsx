import type { CSSProperties, DragEvent, PointerEvent } from "react";
import { Link2, Lock } from "lucide-react";
import { cn } from "@notrelix/ui-web";
import { BAR_TONE_CLASS, PROGRESS_TONE_CLASS } from "./timeline-chrome";
import type { TimelineCopy } from "./timeline.copy";
import {
  cellInView,
  fill,
  fitHeaderText,
  GROUP_ROW_HEIGHT,
  TASK_ROW_HEIGHT,
  type BarLayout,
  type LabelLayout,
  type TaskLayout,
  type TimelineHeader,
  type TimelineLayout,
  type TimelineScale,
} from "./timeline-model";

/** Width of one character of the 12 px semibold month label and of the 11 px week label. */
const MONTH_CHAR = 7;
const WEEK_CHAR = 6.4;

/** The hatch of an overdue stretch: the overdue fill in 4 px diagonal stripes. */
const LATE_HATCH =
  "repeating-linear-gradient(45deg, var(--calendar-event-overdue) 0 4px, transparent 4px 8px)";

export interface TimelineChartProps {
  scale: TimelineScale;
  ppd: number;
  barTop: number;
  header: TimelineHeader;
  layout: TimelineLayout;
  /**
   * The part of the chart in view, in chart pixels. Header text that a frame
   * edge would cut is moved inside it or left out.
   */
  visible: { readonly from: number; readonly to: number } | null;
  /** The row the pointer is over: its bar shows the resize handle. */
  hoverId: string | null;
  onHoverChange: (id: string | null) => void;
  /** The row being dragged; its handle stays while the pointer is away. */
  dragId: string | null;
  canEdit: boolean;
  onOpenItem: (id: string) => void;
  onBarPointerDown: (
    row: TaskLayout,
    mode: "move" | "resize",
    event: PointerEvent<HTMLElement>,
  ) => void;
  onBarPointerMove: (event: PointerEvent<HTMLElement>) => void;
  onBarPointerUp: (event: PointerEvent<HTMLElement>) => void;
  onBarPointerCancel: () => void;
  /** Link connectors; absent = none are drawn (a host without dependencies). */
  link?: TimelineChartLink;
  /** The undated item dragged over the chart, as a day column. */
  trayGhost?: { x: number; text: string; note: string } | null;
  onBodyDragOver?: (event: DragEvent<HTMLElement>) => void;
  onBodyDrop?: (event: DragEvent<HTMLElement>) => void;
  onBodyDragLeave?: () => void;
}

export interface TimelineChartLink {
  copy: TimelineCopy;
  /** False for a viewer: the connectors show disabled with the reason. */
  canEdit: boolean;
  /** The drag in flight: the line, and the bar under it. */
  drag: {
    fromId: string;
    from: { x: number; y: number };
    to: { x: number; y: number };
    target: {
      id: string;
      state: "ok" | "cycle" | "linked";
      text: string;
      note: string;
    } | null;
  } | null;
  onOpen: (row: TaskLayout) => void;
  onPointerDown: (row: TaskLayout, event: PointerEvent<HTMLElement>) => void;
  onPointerMove: (event: PointerEvent<HTMLElement>) => void;
  onPointerUp: (event: PointerEvent<HTMLElement>) => void;
  onPointerCancel: () => void;
}

/**
 * The right column of the Gantt: the sticky time header (months and days,
 * weeks or Monday ticks), the weekend bands, the today line, the dependency
 * arrows and one row of bars, dots, labels and markers per item.
 */
export function TimelineChart({
  scale,
  ppd,
  barTop,
  header,
  layout,
  visible,
  hoverId,
  onHoverChange,
  dragId,
  canEdit,
  onOpenItem,
  onBarPointerDown,
  onBarPointerMove,
  onBarPointerUp,
  onBarPointerCancel,
  link,
  trayGhost = null,
  onBodyDragOver,
  onBodyDrop,
  onBodyDragLeave,
}: TimelineChartProps) {
  const compact = scale === "month";
  const barBox = compact
    ? { top: barTop, height: 16 }
    : { top: barTop, height: 20 };
  return (
    <div
      data-tl="right"
      role="rowgroup"
      className="relative shrink-0"
      style={{ width: header.chartWidth }}
    >
      <div
        data-tl="rh"
        role="row"
        className="sticky top-0 z-[5] box-content h-14 border-b border-line-strong bg-table-header"
      >
        <div role="columnheader" className="contents">
          <div
            data-tl="months"
            className="flex h-6 text-xs leading-[1.45] font-semibold text-muted-foreground"
          >
            {header.months.map((month, index) => {
              const x = header.months
                .slice(0, index)
                .reduce((sum, previous) => sum + previous.width, 0);
              const fit = fitHeaderText(
                x,
                month.width,
                month.label,
                MONTH_CHAR,
                8,
                visible,
              );
              return (
                <span
                  key={index}
                  data-tl="month"
                  className={cn(
                    "box-border px-2 py-1",
                    month.separator && "border-l border-border",
                  )}
                  style={{ width: month.width }}
                >
                  {fit === null ? null : fit.whole ? (
                    month.label
                  ) : (
                    <span
                      className="block truncate"
                      style={{ marginLeft: fit.shift, maxWidth: fit.max }}
                    >
                      {month.label}
                    </span>
                  )}
                </span>
              );
            })}
          </div>
          {scale === "day" ? (
            <div data-tl="days" className="flex h-8">
              {header.days.map((day, index) => (
                <span
                  key={index}
                  data-tl="day"
                  className={cn(
                    "flex w-[26px] flex-none flex-col items-center justify-center text-rail-label leading-[1.15] font-normal text-meta-foreground tabular-nums",
                    !cellInView(index * 26, 26, visible) && "invisible",
                  )}
                >
                  <b
                    data-tl="dayb"
                    className={cn(
                      "text-xs leading-[1.15] font-semibold text-foreground",
                      day.weekend && "font-medium text-meta-foreground",
                      day.today &&
                        "min-w-5 rounded-full bg-primary text-center font-semibold text-primary-foreground",
                    )}
                  >
                    {day.n}
                  </b>
                  {day.weekday}
                </span>
              ))}
            </div>
          ) : null}
          {scale === "week" ? (
            <div data-tl="weeks" className="relative flex h-8">
              {header.weeks.map((week, index) => {
                const x = header.weeks
                  .slice(0, index)
                  .reduce((sum, previous) => sum + previous.width, 0);
                const fit = fitHeaderText(
                  x,
                  week.width,
                  week.label,
                  WEEK_CHAR,
                  4,
                  visible,
                );
                return (
                  <span
                    key={index}
                    data-tl="wk"
                    title={week.full}
                    className={cn(
                      "box-border block h-8 flex-none overflow-hidden px-1 text-rail-label leading-[27px] font-medium text-ellipsis whitespace-nowrap text-meta-foreground tabular-nums",
                      week.separator && "border-l border-border",
                      week.current &&
                        "font-semibold text-state-selected-foreground",
                    )}
                    style={{
                      width: week.width,
                      backgroundImage:
                        "linear-gradient(to right, var(--line-strong) 1px, transparent 1px)",
                      backgroundRepeat: "repeat-x",
                      backgroundPosition: "left bottom",
                      backgroundOrigin: "border-box",
                      backgroundSize: `${ppd}px 5px`,
                    }}
                  >
                    {fit === null ? null : fit.whole ? (
                      week.label
                    ) : (
                      <span
                        className="block truncate"
                        style={{ marginLeft: fit.shift, maxWidth: fit.max }}
                      >
                        {week.label}
                      </span>
                    )}
                  </span>
                );
              })}
            </div>
          ) : null}
          {scale === "month" ? (
            <div data-tl="weeks" className="relative flex h-8">
              {header.ticks.map((tick, index) => (
                <span
                  key={index}
                  data-tl="mt"
                  title={tick.full}
                  className={cn(
                    "absolute inset-y-0 flex items-center border-l border-border pl-1 text-rail-label leading-[1.45] font-normal text-meta-foreground tabular-nums",
                    !cellInView(tick.x, 24, visible) && "invisible",
                  )}
                  style={{ left: tick.x }}
                >
                  <b
                    className={cn(
                      "font-medium",
                      tick.today &&
                        "rounded-full bg-primary px-[5px] font-semibold text-primary-foreground",
                    )}
                  >
                    {tick.n}
                  </b>
                </span>
              ))}
            </div>
          ) : null}
          {scale !== "day" ? (
            <span
              data-tl="hnow"
              aria-hidden="true"
              className="absolute bottom-0 h-2 w-0.5 bg-primary"
              style={{ left: header.todayX }}
            />
          ) : null}
        </div>
      </div>

      <div
        data-tl="body"
        className="relative"
        onDragOver={onBodyDragOver}
        onDrop={onBodyDrop}
        onDragLeave={onBodyDragLeave}
      >
        {link?.drag ? (
          <svg
            data-tl="linkdrag"
            aria-hidden="true"
            className="pointer-events-none absolute inset-0 z-[8] size-full overflow-visible"
          >
            <path
              d={`M ${link.drag.from.x} ${link.drag.from.y} C ${link.drag.from.x + 40} ${link.drag.from.y}, ${link.drag.to.x - 40} ${link.drag.to.y}, ${link.drag.to.x} ${link.drag.to.y}`}
              fill="none"
              strokeWidth="2"
              strokeDasharray="5 4"
              className={
                link.drag.target?.state === "cycle"
                  ? "stroke-danger-text"
                  : "stroke-primary"
              }
            />
          </svg>
        ) : null}
        {trayGhost ? (
          <>
            <span
              data-tl="trayghost"
              aria-hidden="true"
              className="absolute inset-y-0 z-[2] box-border border-x border-dashed border-primary bg-state-selected"
              style={{ left: trayGhost.x, width: ppd }}
            />
            <span
              data-tl="tip"
              role="status"
              className="absolute bottom-3 z-[7] rounded-[8px] bg-calendar-tip px-2.5 py-1.5 text-xs leading-[1.45] font-semibold whitespace-nowrap text-calendar-tip-foreground shadow-popover"
              style={{
                left: Math.max(
                  4,
                  Math.min(trayGhost.x, header.chartWidth - 260),
                ),
              }}
            >
              {trayGhost.text}
              <small className="block text-mark-sm leading-[1.45] font-medium text-timeline-tip-note">
                {trayGhost.note}
              </small>
            </span>
          </>
        ) : null}
        {header.bands.map((band, index) => (
          <span
            key={index}
            data-tl="band"
            aria-hidden="true"
            className={cn(
              "absolute inset-y-0",
              band.separator ? "bg-border" : "bg-calendar-weekend",
            )}
            style={{ left: band.x, width: band.width }}
          />
        ))}
        <span
          data-tl="todayline"
          aria-hidden="true"
          className="absolute inset-y-0 z-[3] w-0.5 bg-primary"
          style={{ left: header.todayX }}
        />
        {layout.segments.map((segment, index) => (
          <span
            key={`s${index}`}
            data-tl="dl"
            aria-hidden="true"
            className={cn(
              "absolute z-[3]",
              segment.critical ? "bg-danger-text" : "bg-timeline-link",
            )}
            style={{
              left: segment.x,
              top: segment.y,
              width: segment.w,
              height: segment.h,
            }}
          />
        ))}
        {layout.heads.map((head, index) => (
          <span
            key={`h${index}`}
            data-tl="ah"
            aria-hidden="true"
            className={cn(
              "absolute z-[3] size-0",
              head.down
                ? cn(
                    "border-x-4 border-t-[6px] border-x-transparent",
                    head.critical
                      ? "border-t-danger-text"
                      : "border-t-timeline-link",
                  )
                : cn(
                    "border-y-4 border-l-[6px] border-y-transparent",
                    head.critical
                      ? "border-l-danger-text"
                      : "border-l-timeline-link",
                  ),
            )}
            style={{ left: head.x, top: head.y }}
          />
        ))}
        {layout.rows.map((row) =>
          row.type === "group" ? (
            <div
              key={`g-${row.id}`}
              data-tl="tgrp"
              className="box-border border-b border-border"
              style={{ height: GROUP_ROW_HEIGHT }}
            />
          ) : (
            <TaskTrack
              key={row.id}
              row={row}
              compact={compact}
              barBox={barBox}
              chartWidth={header.chartWidth}
              hovered={hoverId === row.id || dragId === row.id}
              canEdit={canEdit}
              onHoverChange={onHoverChange}
              onOpenItem={onOpenItem}
              onBarPointerDown={onBarPointerDown}
              onBarPointerMove={onBarPointerMove}
              onBarPointerUp={onBarPointerUp}
              onBarPointerCancel={onBarPointerCancel}
              link={link}
            />
          ),
        )}
      </div>
    </div>
  );
}

function labelStyle(label: LabelLayout): CSSProperties {
  return {
    ...(label.left !== undefined ? { left: label.left } : {}),
    ...(label.right !== undefined ? { right: label.right } : {}),
    maxWidth: label.maxWidth,
  };
}

function TaskTrack({
  row,
  compact,
  barBox,
  chartWidth,
  hovered,
  canEdit,
  onHoverChange,
  onOpenItem,
  onBarPointerDown,
  onBarPointerMove,
  onBarPointerUp,
  onBarPointerCancel,
  link,
}: {
  link?: TimelineChartLink;
  row: TaskLayout;
  compact: boolean;
  barBox: { top: number; height: number };
  chartWidth: number;
  hovered: boolean;
  canEdit: boolean;
  onHoverChange: (id: string | null) => void;
  onOpenItem: (id: string) => void;
  onBarPointerDown: TimelineChartProps["onBarPointerDown"];
  onBarPointerMove: TimelineChartProps["onBarPointerMove"];
  onBarPointerUp: TimelineChartProps["onBarPointerUp"];
  onBarPointerCancel: TimelineChartProps["onBarPointerCancel"];
}) {
  const { bar, fin, label, late, milestone, ghost, handle, tip } = row;
  const draggable = canEdit && row.item.movable !== false && bar !== null;
  const hoverHandle =
    hovered && draggable && handle === null && bar !== null
      ? { x: bar.x + bar.width - 3 }
      : null;
  const shownHandle = handle ?? hoverHandle;
  return (
    <div
      data-tl="trow"
      role="row"
      data-row-id={row.id}
      className="relative box-border border-b border-border"
      style={{ height: TASK_ROW_HEIGHT }}
      onPointerEnter={() => onHoverChange(row.id)}
      onPointerLeave={() => onHoverChange(null)}
    >
      <div role="gridcell" className="contents">
        {bar ? (
          <Bar
            row={row}
            bar={bar}
            compact={compact}
            barBox={barBox}
            draggable={draggable}
            onOpenItem={onOpenItem}
            onBarPointerDown={onBarPointerDown}
            onBarPointerMove={onBarPointerMove}
            onBarPointerUp={onBarPointerUp}
            onBarPointerCancel={onBarPointerCancel}
          />
        ) : null}
        {link && bar && row.item.kind === "item" ? (
          <Connector row={row} bar={bar} link={link} visible={hovered} />
        ) : null}
        {link?.drag?.target?.id === row.id ? (
          <>
            <span
              data-tl="linktarget"
              data-state={link.drag.target.state}
              aria-hidden="true"
              className={cn(
                "pointer-events-none absolute z-[5] box-border border-2",
                link.drag.target.state === "ok"
                  ? "border-primary"
                  : "border-danger-text",
                compact
                  ? "top-2.5 h-4 rounded-[4px]"
                  : "top-2 h-5 rounded-[6px]",
              )}
              style={{ left: bar?.x ?? 0, width: bar?.width ?? 0 }}
            />
            <span
              data-tl="tip"
              role="status"
              className={cn(
                "absolute top-10 z-[9] rounded-[8px] px-2.5 py-1.5 text-xs leading-[1.45] font-semibold whitespace-nowrap shadow-popover",
                link.drag.target.state === "ok"
                  ? "bg-calendar-tip text-calendar-tip-foreground"
                  : "bg-danger-text text-background",
              )}
              style={{
                left: Math.max(4, Math.min(bar?.x ?? 0, chartWidth - 260)),
              }}
            >
              {link.drag.target.text}
              <small className="block text-mark-sm leading-[1.45] font-medium opacity-80">
                {link.drag.target.note}
              </small>
            </span>
          </>
        ) : null}
        {fin ? (
          <span
            data-tl="fin"
            role="img"
            aria-label={row.aria}
            title={row.aria}
            className={cn(
              "absolute top-[13px] z-[4] size-2.5 rounded-full bg-timeline-progress-done",
              fin.fade && "opacity-35",
              fin.critical && "ring-2 ring-danger-text",
            )}
            style={{ left: fin.x }}
          />
        ) : null}
        {label ? (
          <span
            data-tl="lab"
            className="absolute top-[9px] z-[4] truncate text-xs leading-[1.45] font-medium whitespace-nowrap text-muted-foreground"
            style={labelStyle(label)}
          >
            {label.text}
          </span>
        ) : null}
        {late ? (
          <>
            <span
              data-tl="late"
              aria-hidden="true"
              className={cn(
                "absolute z-[4] box-border border border-dashed border-danger-text",
                compact
                  ? "top-2.5 h-4 rounded-r-[4px]"
                  : "top-2 h-5 rounded-r-[6px]",
              )}
              style={{
                left: late.x,
                width: late.width,
                backgroundImage: LATE_HATCH,
              }}
            />
            {late.textMax > 0 ? (
              <span
                data-tl="latel"
                aria-hidden="true"
                className="absolute top-[9px] z-[4] truncate text-xs leading-[1.45] font-bold whitespace-nowrap text-danger-text"
                style={{ left: late.textLeft, maxWidth: late.textMax }}
              >
                {late.text}
              </span>
            ) : null}
          </>
        ) : null}
        {milestone ? (
          <>
            <span
              data-tl="ms"
              role="img"
              aria-label={milestone.aria}
              title={milestone.title}
              className="absolute top-[11px] z-[4] size-3.5 rotate-45 rounded-[2px] bg-timeline-milestone"
              style={{ left: milestone.x }}
            />
            {milestone.labelMax > 0 ? (
              <span
                data-tl="lab"
                className="absolute top-[9px] z-[4] truncate text-xs leading-[1.45] font-bold whitespace-nowrap text-foreground"
                style={{
                  ...(milestone.labelRight !== undefined
                    ? { right: milestone.labelRight }
                    : { left: milestone.labelLeft }),
                  maxWidth: milestone.labelMax,
                }}
              >
                {row.title}
              </span>
            ) : null}
          </>
        ) : null}
        {ghost ? (
          <span
            data-tl="ghost"
            aria-hidden="true"
            className={cn(
              "absolute z-[4] box-border border-[1.5px] border-dashed border-primary bg-state-selected",
              compact ? "top-2.5 h-4 rounded-[4px]" : "top-2 h-5 rounded-[6px]",
            )}
            style={{ left: ghost.x, width: ghost.width }}
          />
        ) : null}
        {shownHandle ? (
          <span
            data-tl="handle"
            aria-hidden="true"
            className={cn(
              "absolute z-[6] w-1.5 cursor-ew-resize rounded-[3px] bg-primary",
              compact ? "top-2 h-5" : "top-1.5 h-6",
            )}
            style={{ left: shownHandle.x, touchAction: "none" }}
            onPointerDown={(event) => onBarPointerDown(row, "resize", event)}
            onPointerMove={onBarPointerMove}
            onPointerUp={onBarPointerUp}
            onPointerCancel={onBarPointerCancel}
          />
        ) : null}
        {tip ? (
          <span
            data-tl="tip"
            role="status"
            className="absolute -top-12 z-[7] rounded-[8px] bg-calendar-tip px-2.5 py-1.5 text-xs leading-[1.45] font-semibold whitespace-nowrap text-calendar-tip-foreground shadow-popover"
            style={{ left: Math.max(4, Math.min(tip.x, chartWidth - 220)) }}
          >
            {tip.text}
            {tip.note ? (
              <small className="block text-mark-sm leading-[1.45] font-medium text-timeline-tip-note">
                {tip.note}
              </small>
            ) : null}
          </span>
        ) : null}
      </div>
    </div>
  );
}

function Bar({
  row,
  bar,
  compact,
  barBox,
  draggable,
  onOpenItem,
  onBarPointerDown,
  onBarPointerMove,
  onBarPointerUp,
  onBarPointerCancel,
}: {
  row: TaskLayout;
  bar: BarLayout;
  compact: boolean;
  barBox: { top: number; height: number };
  draggable: boolean;
  onOpenItem: (id: string) => void;
  onBarPointerDown: TimelineChartProps["onBarPointerDown"];
  onBarPointerMove: TimelineChartProps["onBarPointerMove"];
  onBarPointerUp: TimelineChartProps["onBarPointerUp"];
  onBarPointerCancel: TimelineChartProps["onBarPointerCancel"];
}) {
  return (
    <span
      data-tl="gb"
      role="img"
      aria-label={row.aria}
      title={row.aria}
      className={cn(
        "absolute z-[4] box-border flex items-center overflow-hidden text-xs leading-[1.45] font-semibold whitespace-nowrap text-foreground",
        compact
          ? "rounded-[4px] px-[3px] text-rail-label"
          : "rounded-[6px] px-2",
        BAR_TONE_CLASS[bar.tone],
        bar.clipStart && "rounded-l-none",
        bar.clipEnd && "rounded-r-none",
        bar.fade && "opacity-35",
        bar.critical && "ring-2 ring-danger-text",
        draggable
          ? "cursor-grab touch-none active:cursor-grabbing"
          : "cursor-pointer",
      )}
      style={{
        left: bar.x,
        width: bar.width,
        top: barBox.top,
        height: barBox.height,
      }}
      onPointerDown={
        draggable ? (event) => onBarPointerDown(row, "move", event) : undefined
      }
      onPointerMove={draggable ? onBarPointerMove : undefined}
      onPointerUp={draggable ? onBarPointerUp : undefined}
      onPointerCancel={draggable ? onBarPointerCancel : undefined}
      onClick={draggable ? undefined : () => onOpenItem(row.id)}
    >
      <span
        data-tl="gt"
        aria-hidden="true"
        className={cn("min-w-0 truncate", bar.mono && "font-mono font-medium")}
        style={{ marginLeft: bar.labelShift, maxWidth: bar.labelMax }}
      >
        {bar.label}
      </span>
      <span
        data-tl="pg"
        className={cn(
          "absolute bottom-0 left-0 h-[3px]",
          PROGRESS_TONE_CLASS[bar.tone],
        )}
        style={{ width: `${bar.percent}%` }}
      />
    </span>
  );
}

/**
 * The 16px circle (in a 24px hit area) 6px after the bar's due end. Drag it
 * to another bar, or Tab to it and press Enter to open "Liên kết với…".
 */
function Connector({
  row,
  bar,
  link,
  visible,
}: {
  row: TaskLayout;
  bar: BarLayout;
  link: TimelineChartLink;
  visible: boolean;
}) {
  const text = link.copy.link;
  const key = row.item.itemKey ?? row.item.title;
  const disabled = !link.canEdit;
  return (
    <button
      type="button"
      data-tl="connector"
      aria-label={fill(text.connector, { key })}
      aria-disabled={disabled ? "true" : undefined}
      aria-haspopup={disabled ? undefined : "dialog"}
      title={
        disabled ? text.readOnly : `${text.connectorTip}, ${text.connectorNote}`
      }
      className={cn(
        "group/conn absolute top-1.5 z-[6] grid size-6 touch-none place-items-center rounded-full focus-visible:opacity-100 focus-visible:outline-2 focus-visible:outline-ring",
        visible || link.drag?.fromId === row.id ? "opacity-100" : "opacity-0",
        disabled ? "cursor-not-allowed" : "cursor-crosshair",
      )}
      style={{ left: bar.x + bar.width + 6 - 4 }}
      onClick={disabled ? undefined : () => link.onOpen(row)}
      onPointerDown={
        disabled ? undefined : (event) => link.onPointerDown(row, event)
      }
      onPointerMove={disabled ? undefined : link.onPointerMove}
      onPointerUp={disabled ? undefined : link.onPointerUp}
      onPointerCancel={disabled ? undefined : link.onPointerCancel}
    >
      <span
        aria-hidden="true"
        className={cn(
          "grid size-4 place-items-center rounded-full border-2 bg-popover",
          disabled
            ? "border-line-strong text-meta-foreground"
            : "border-primary text-primary",
        )}
      >
        {disabled ? (
          <Lock className="size-2.5" />
        ) : (
          <Link2 className="size-2.5" />
        )}
      </span>
    </button>
  );
}
