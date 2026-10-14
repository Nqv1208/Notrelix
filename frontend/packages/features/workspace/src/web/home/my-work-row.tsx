import { Check, ChevronDown, Flag, RotateCcw } from "lucide-react";
import { cn } from "@notrelix/ui-web";
import { describeMyWorkDue, type MyWorkDue } from "../../core/model/my-work";
import { initialsFor } from "../../core/model/navigation";
import {
  formatCopy,
  IDENTITY_MARK_CLASS,
  isPlainClick,
} from "../shell/shell-models";
import type { MyWorkItemView, MyWorkReason, WorkPriority } from "./home-models";
import {
  DepartmentMark,
  dueWords,
  STATUS_PILL_CLASS,
  StatusDot,
} from "./work-row-parts";
import type { WorkspaceHomeCopy } from "./workspace-home.copy";

/**
 * One row of the My work list (Final-MyWork `.row`): select box, tick, title
 * with its Board line, Status, due day, Priority and the person in charge. A
 * column is drawn only when some row of the list has something for it, so a
 * list the host fills partly has no empty columns.
 */

export interface MyWorkColumns {
  readonly status: boolean;
  readonly priority: boolean;
  readonly assignee: boolean;
}

const REASON_CLASS: Record<MyWorkReason, string> = {
  follow: "bg-state-hover-strong text-muted-foreground",
  mine: "bg-state-selected text-state-selected-foreground",
  approve: "bg-status-chip-review text-warning-text",
};

const PRIORITY_CLASS: Record<WorkPriority, string> = {
  urgent: "font-semibold text-danger-text",
  high: "font-semibold text-priority-high-text",
  medium: "font-medium text-meta-foreground",
  low: "font-medium text-meta-foreground",
};

const SMALL_BUTTON_CLASS =
  "inline-flex h-7 shrink-0 items-center gap-1.5 rounded-[8px] border border-line-strong bg-popover px-2.5 text-crumb leading-[1.45] font-medium whitespace-nowrap text-foreground no-underline outline-none hover:bg-state-hover focus-visible:ring-2 focus-visible:ring-focus-ring aria-disabled:cursor-not-allowed dark:bg-card";
const SMALL_PRIMARY_CLASS =
  "inline-flex h-7 shrink-0 items-center gap-1.5 rounded-[8px] border border-primary bg-primary px-2.5 text-crumb leading-[1.45] font-semibold whitespace-nowrap text-primary-foreground outline-none hover:bg-primary/90 focus-visible:ring-2 focus-visible:ring-focus-ring aria-disabled:cursor-not-allowed";

function CellDue({
  copy,
  due,
  completedAt,
  now,
}: {
  copy: WorkspaceHomeCopy;
  due: MyWorkDue;
  completedAt?: MyWorkDue;
  now: number;
}) {
  void now;
  if (completedAt) {
    return (
      <span
        data-f="du"
        className="block text-crumb leading-[1.35] whitespace-nowrap text-muted-foreground tabular-nums"
      >
        <span className="sr-only">{copy.list.doneLabel}</span>
        {completedAt.kind === "today"
          ? (completedAt.time ?? copy.work.due.today)
          : `${copy.work.weekdays[WEEKDAY_KEYS[completedAt.weekday] ?? "sun"]} ${completedAt.shortDate}`}
        {completedAt.kind === "today" && completedAt.time ? (
          <small className="block text-micro leading-[1.35] font-normal text-meta-foreground">
            {copy.list.sinceToday}
          </small>
        ) : null}
      </span>
    );
  }
  const words = dueWords(copy, due, { timeOnToday: false });
  const sub =
    due.kind === "overdue"
      ? formatCopy(copy.list.dueOn, { date: due.shortDate })
      : due.kind === "today" && due.time
        ? due.time
        : null;
  return (
    <span
      data-f="du"
      className={cn(
        "block text-crumb leading-[1.35] whitespace-nowrap tabular-nums",
        words.tone === "over" && "font-semibold text-danger-text",
        words.tone === "today" && "font-semibold text-warning-text",
        words.tone === "normal" && "text-muted-foreground",
      )}
    >
      <span className="sr-only">{copy.list.dueLabel}</span>
      {words.text}
      {sub ? (
        <small className="block text-micro leading-[1.35] font-normal text-meta-foreground">
          {sub}
        </small>
      ) : null}
    </span>
  );
}

const WEEKDAY_KEYS = ["sun", "mon", "tue", "wed", "thu", "fri", "sat"] as const;

export interface MyWorkRowProps {
  readonly item: MyWorkItemView;
  readonly copy: WorkspaceHomeCopy;
  readonly now: number;
  readonly columns: MyWorkColumns;
  readonly narrow: boolean;
  /** The Board line is left out when the list is grouped by Board. */
  readonly showBoard: boolean;
  readonly selected: boolean;
  /** The row open in the sheet. */
  readonly current: boolean;
  readonly completing: boolean;
  /** `null` draws the tick inert. */
  readonly canComplete: boolean;
  /** `null` when the row's Status cannot be changed (no producer). */
  readonly statusPopOpen: boolean;
  readonly done: boolean;
  readonly onToggleSelect: () => void;
  readonly onComplete: () => void;
  readonly onOpen: () => void;
  readonly onStatusClick: (() => void) | null;
  readonly statusPop?: React.ReactNode;
  readonly href: string;
  /** Approval, reopen and the write in flight; `undefined` draws them inert. */
  readonly actions?: {
    readonly busy: boolean;
    readonly onApprove: () => void;
    readonly onRequestChanges: () => void;
    readonly onReopen: () => void;
  };
}

export function MyWorkRow(props: MyWorkRowProps) {
  const { item, copy, now, columns, narrow, done } = props;
  const due = describeMyWorkDue(item.dueAt, now);
  const completed = done
    ? describeMyWorkDue(item.completedAt ?? null, now)
    : undefined;
  const name = item.key ?? item.name;
  const reason = item.reason ?? null;
  const priority = item.priority ?? null;
  // An item waiting for the member's approval is decided, not ticked.
  const canTick = props.canComplete && reason !== "approve";
  const grid = done
    ? "grid-cols-[18px_22px_minmax(0,1fr)_112px_112px_132px]"
    : gridFor(columns, narrow);

  return (
    <li
      data-f="row"
      data-item-id={item.id}
      aria-current={props.current ? "true" : undefined}
      className={cn(
        "group/row relative box-border grid min-h-[60px] items-center gap-x-2.5 border-t border-border px-3.5 py-[9px] first:rounded-t-[11px] first:border-t-0 last:rounded-b-[11px]",
        grid,
        props.selected || props.current
          ? "bg-row-selected hover:bg-row-selected"
          : "hover:bg-state-hover",
        props.current && "shadow-[inset_0_0_0_1.5px_var(--color-primary)]",
      )}
    >
      <button
        data-f="cb"
        type="button"
        role="checkbox"
        aria-checked={props.selected}
        aria-label={formatCopy(copy.list.select, { name })}
        onClick={props.onToggleSelect}
        className={cn(
          "grid size-4 cursor-pointer place-items-center rounded-[4px] border-[1.5px] border-work-status-todo bg-popover p-0 text-transparent outline-none",
          "opacity-0 group-focus-within/row:opacity-100 group-hover/row:opacity-100 group-data-[selecting=true]/lcol:opacity-100 focus-visible:ring-2 focus-visible:ring-focus-ring",
          props.selected &&
            "border-primary bg-primary text-primary-foreground opacity-100",
        )}
      >
        <Check aria-hidden="true" className="size-[11px]" strokeWidth={3} />
      </button>
      {done ? (
        <span
          data-f="tick"
          role="img"
          aria-label={copy.list.doneStatus}
          className="grid size-5 place-items-center rounded-full border-[1.5px] border-work-status-done bg-work-status-done text-card"
        >
          <Check aria-hidden="true" className="size-3" strokeWidth={3} />
        </span>
      ) : (
        <button
          data-f="tick"
          type="button"
          aria-label={formatCopy(copy.list.complete, { name })}
          title={formatCopy(copy.list.complete, { name })}
          aria-disabled={!canTick || props.completing ? true : undefined}
          aria-busy={props.completing ? true : undefined}
          onClick={canTick && !props.completing ? props.onComplete : undefined}
          className={cn(
            "grid size-5 place-items-center rounded-full border-[1.5px] border-work-status-todo bg-transparent p-0 text-transparent outline-none focus-visible:ring-2 focus-visible:ring-focus-ring",
            canTick
              ? "cursor-pointer hover:border-work-status-done hover:text-work-status-done"
              : "cursor-not-allowed border-dashed",
            props.completing && "opacity-60",
          )}
        >
          <Check aria-hidden="true" className="size-3" strokeWidth={3} />
        </button>
      )}
      <div data-f="rm" className="min-w-0">
        <p
          data-f="rt"
          className={cn(
            "m-0 line-clamp-2 overflow-hidden text-label leading-[1.4] [text-wrap:pretty]",
            done ? "text-muted-foreground" : "text-foreground",
          )}
        >
          <a
            href={props.href}
            onClick={(event) => {
              if (!isPlainClick(event)) return;
              event.preventDefault();
              props.onOpen();
            }}
            className="no-underline outline-none hover:underline focus-visible:underline"
          >
            {item.name}
          </a>
        </p>
        <div
          data-f="rc"
          className="mt-[3px] flex flex-wrap items-center gap-1.5 text-xs leading-[1.45] text-meta-foreground"
        >
          {props.showBoard && item.department ? (
            <DepartmentMark
              small
              name={item.department.name}
              markTone={item.department.markTone}
            />
          ) : null}
          {props.showBoard && item.boardName
            ? item.key
              ? `${item.boardName} ·`
              : item.boardName
            : null}
          {item.key ? (
            <span data-f="key" className="font-mono text-micro">
              {item.key}
            </span>
          ) : null}
          {reason ? (
            <span
              data-f="why"
              className={cn(
                "inline-flex h-5 items-center rounded-full px-[7px] text-micro leading-[1.45] font-semibold whitespace-nowrap",
                REASON_CLASS[reason],
              )}
            >
              {copy.work.reasons[reason]}
            </span>
          ) : null}
        </div>
      </div>
      {columns.status || done ? (
        <div data-f="cst" className="relative">
          {done ? (
            <span
              data-f="stp"
              className="inline-flex h-[26px] items-center gap-1.5 rounded-full bg-status-chip-done pr-2.5 pl-[9px] text-crumb leading-[1.45] font-semibold whitespace-nowrap text-foreground"
            >
              <StatusDot bucket="done" />
              {copy.list.doneStatus}
            </span>
          ) : item.status ? (
            <button
              data-f="stp"
              type="button"
              aria-haspopup="listbox"
              aria-expanded={props.statusPopOpen}
              aria-disabled={props.onStatusClick ? undefined : true}
              title={
                props.onStatusClick ? undefined : copy.list.statusUnavailable
              }
              aria-label={formatCopy(copy.list.statusChange, {
                status: item.status.label,
                name,
              })}
              onClick={props.onStatusClick ?? undefined}
              className={cn(
                "inline-flex h-[26px] items-center gap-1.5 rounded-full border-0 pr-2 pl-[9px] text-crumb leading-[1.45] font-semibold whitespace-nowrap text-foreground outline-none focus-visible:ring-2 focus-visible:ring-focus-ring",
                STATUS_PILL_CLASS[item.status.bucket],
                props.onStatusClick ? "cursor-pointer" : "cursor-default",
                props.statusPopOpen &&
                  "shadow-[0_0_0_2px_var(--color-primary)]",
              )}
            >
              <StatusDot bucket={item.status.bucket} />
              {item.status.label}
              <ChevronDown
                aria-hidden="true"
                className="size-3 text-meta-foreground"
                strokeWidth={1.8}
              />
            </button>
          ) : null}
          {props.statusPop}
        </div>
      ) : null}
      <div data-f="cdu">
        <CellDue copy={copy} due={due} completedAt={completed} now={now} />
      </div>
      {!done && !narrow && columns.priority ? (
        <div data-f="cpr">
          {priority ? (
            <span
              data-f="pr"
              className={cn(
                "inline-flex items-center gap-[5px] text-crumb leading-[1.45] whitespace-nowrap",
                PRIORITY_CLASS[priority],
              )}
            >
              <Flag
                aria-hidden="true"
                className="size-[13px]"
                strokeWidth={1.8}
              />
              <span className="sr-only">
                {formatCopy(copy.work.priorityLabel, { priority: "" })}
              </span>
              {copy.work.priority[priority]}
            </span>
          ) : null}
        </div>
      ) : null}
      {!done && !narrow && columns.assignee ? (
        <div data-f="cav">
          {item.assignee ? (
            <span
              data-f="av"
              role="img"
              aria-label={formatCopy(copy.list.assignee, {
                name: item.assignee.name,
              })}
              title={item.assignee.name}
              className={cn(
                "grid size-6 place-items-center rounded-full text-mark-xs font-bold",
                IDENTITY_MARK_CLASS[item.assignee.tone],
              )}
            >
              {initialsFor(item.assignee.name)}
            </span>
          ) : null}
        </div>
      ) : null}
      {done ? (
        <div data-f="cre" className="flex justify-end">
          <button
            type="button"
            aria-disabled={
              !props.actions || props.actions.busy ? true : undefined
            }
            title={props.actions ? undefined : copy.list.bulk.unavailable}
            aria-label={formatCopy(copy.list.reopen, { name })}
            onClick={
              props.actions && !props.actions.busy
                ? props.actions.onReopen
                : undefined
            }
            className={SMALL_BUTTON_CLASS}
          >
            <RotateCcw
              aria-hidden="true"
              className="size-3.5"
              strokeWidth={1.8}
            />
            {copy.list.reopenAction}
          </button>
        </div>
      ) : null}
      {!done && item.approval ? (
        <div
          data-f="apr"
          className="col-[3/-1] mt-1.5 flex flex-wrap items-center gap-2 text-crumb leading-[1.45] text-muted-foreground"
        >
          <span
            aria-hidden="true"
            className={cn(
              "grid size-5 place-items-center rounded-full text-avatar-sm font-bold",
              IDENTITY_MARK_CLASS[item.approval.actorTone],
            )}
          >
            {initialsFor(item.approval.actorName)}
          </span>
          <span>
            <b className="font-semibold text-foreground">
              {item.approval.actorName}
            </b>{" "}
            {formatCopy(copy.list.approvalSent, { time: item.approval.time })}
          </span>
          <button
            type="button"
            aria-disabled={
              !props.actions || props.actions.busy ? true : undefined
            }
            title={props.actions ? undefined : copy.list.bulk.unavailable}
            aria-label={`${copy.list.approve} ${name}`}
            onClick={
              props.actions && !props.actions.busy
                ? props.actions.onApprove
                : undefined
            }
            className={SMALL_PRIMARY_CLASS}
          >
            {copy.list.approve}
          </button>
          <button
            type="button"
            aria-disabled={
              !props.actions || props.actions.busy ? true : undefined
            }
            title={props.actions ? undefined : copy.list.bulk.unavailable}
            aria-label={`${copy.list.requestChanges} ${name}`}
            onClick={
              props.actions && !props.actions.busy
                ? props.actions.onRequestChanges
                : undefined
            }
            className={SMALL_BUTTON_CLASS}
          >
            {copy.list.requestChanges}
          </button>
        </div>
      ) : null}
    </li>
  );
}

function gridFor(columns: MyWorkColumns, narrow: boolean): string {
  const status = columns.status;
  const priority = columns.priority && !narrow;
  const assignee = columns.assignee && !narrow;
  if (narrow) {
    return status
      ? "grid-cols-[18px_22px_minmax(0,1fr)_112px_104px]"
      : "grid-cols-[18px_22px_minmax(0,1fr)_104px]";
  }
  if (status && priority && assignee)
    return "grid-cols-[18px_22px_minmax(0,1fr)_112px_112px_96px_26px]";
  if (status && priority)
    return "grid-cols-[18px_22px_minmax(0,1fr)_112px_112px_96px]";
  if (status && assignee)
    return "grid-cols-[18px_22px_minmax(0,1fr)_112px_112px_26px]";
  if (status) return "grid-cols-[18px_22px_minmax(0,1fr)_112px_112px]";
  if (priority && assignee)
    return "grid-cols-[18px_22px_minmax(0,1fr)_112px_96px_26px]";
  if (priority) return "grid-cols-[18px_22px_minmax(0,1fr)_112px_96px]";
  if (assignee) return "grid-cols-[18px_22px_minmax(0,1fr)_112px_26px]";
  return "grid-cols-[18px_22px_minmax(0,1fr)_112px]";
}
