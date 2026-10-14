import { useState, type ReactNode } from "react";
import {
  Bell,
  Check,
  CalendarDays,
  CircleCheck,
  Clock,
  EyeOff,
  Info,
  Plus,
  Eye,
} from "lucide-react";
import { cn } from "@notrelix/ui-web";
import { initialsFor, type MarkToneId } from "../../core/model/navigation";
import { ShellLink } from "../shell/shell-link";
import {
  formatCopy,
  IDENTITY_MARK_CLASS,
  type ShellNavigation,
} from "../shell/shell-models";
import type { MyWorkDepartment, WorkStatusBucket } from "./home-models";
import type { MyWorkWaitingView } from "./my-work-types";
import type {
  MyWorkDepartmentCount,
  MyWorkOverdueSummary,
  MyWorkWeekDay,
} from "./my-work-model";
import { SMALL_OUTLINE_CLASS } from "./my-work-chrome";
import { DepartmentMark, StatusDot } from "./work-row-parts";
import type { WorkspaceHomeCopy } from "./workspace-home.copy";

const WEEKDAY_KEYS = ["sun", "mon", "tue", "wed", "thu", "fri", "sat"] as const;
const WEEKDAY_SHORT: Record<
  number,
  "su" | "mo" | "tu" | "we" | "th" | "fr" | "sa"
> = {
  0: "su",
  1: "mo",
  2: "tu",
  3: "we",
  4: "th",
  5: "fr",
  6: "sa",
};

/** The department's fill on the rail's bar: its mark tone, as a literal class. */
const MARK_FILL_CLASS: Record<MarkToneId, string> = {
  blue: "bg-mark-blue",
  gold: "bg-mark-gold",
  coral: "bg-mark-coral",
  sage: "bg-mark-sage",
  crimson: "bg-mark-crimson",
  violet: "bg-mark-violet",
};

const CARD_CLASS =
  "rounded-[12px] border border-border bg-popover px-3.5 py-3 dark:bg-card";
const CARD_HEADING_CLASS =
  "m-0 mb-2.5 flex items-center gap-2 text-nav leading-[1.45] font-[650]";
const CARD_SMALL_CLASS =
  "ml-auto text-xs leading-[1.45] font-medium text-meta-foreground";

/** The right rail: this week by due day, by department, and who is being waited on. */
export function MyWorkRail({
  copy,
  navigation,
  days,
  range,
  overdue,
  departments,
  total,
  waiting,
  dueOn,
  onDay,
  onRemind,
  remindingIds,
}: {
  copy: WorkspaceHomeCopy;
  navigation: ShellNavigation;
  days: readonly MyWorkWeekDay[];
  range: string;
  overdue: MyWorkOverdueSummary;
  departments: readonly MyWorkDepartmentCount[];
  total: number;
  waiting: readonly MyWorkWaitingView[];
  dueOn: string | null;
  onDay: (shortDate: string) => void;
  /** `undefined` draws "Nhắc" inert. */
  onRemind?: (entry: MyWorkWaitingView) => void;
  remindingIds?: ReadonlySet<string>;
}) {
  const max = Math.max(1, ...days.map((day) => day.count));
  const deptMax = Math.max(1, ...departments.map((entry) => entry.count));
  return (
    <aside
      data-f="rr"
      aria-label={copy.list.rail.label}
      className="flex w-[300px] shrink-0 flex-col gap-3.5 overflow-auto pt-1.5 pb-6"
    >
      <section
        data-f="rcard"
        aria-labelledby="my-work-week"
        className={CARD_CLASS}
      >
        <h2 id="my-work-week" data-f="rh2" className={CARD_HEADING_CLASS}>
          {copy.list.rail.week}
          <small className={CARD_SMALL_CLASS}>{range}</small>
        </h2>
        <div data-f="wk" className="grid grid-cols-7 gap-0.5">
          {days.map((day) => {
            const weekday =
              copy.work.weekdays[WEEKDAY_KEYS[day.weekday] ?? "sun"];
            const label = formatCopy(
              day.count > 0
                ? day.today
                  ? copy.list.rail.dayLabelToday
                  : copy.list.rail.dayLabel
                : day.today
                  ? copy.list.rail.dayNoneToday
                  : copy.list.rail.dayNone,
              { weekday, date: day.shortDate, count: day.count },
            );
            return (
              <button
                key={day.shortDate}
                data-f="day"
                type="button"
                aria-label={label}
                aria-pressed={dueOn === day.shortDate}
                onClick={() => onDay(day.shortDate)}
                className={cn(
                  "flex min-w-0 cursor-pointer flex-col items-center gap-0.5 rounded-[8px] border-0 bg-transparent pt-1.5 pb-[7px] text-meta-foreground outline-none hover:bg-state-hover focus-visible:ring-2 focus-visible:ring-focus-ring",
                  (day.today || dueOn === day.shortDate) && "bg-state-selected",
                )}
              >
                <span
                  data-f="dl"
                  className={cn(
                    "text-key leading-[1.45] font-sans font-semibold",
                    day.today && "text-state-selected-foreground",
                  )}
                >
                  {copy.list.rail.dayShort[WEEKDAY_SHORT[day.weekday] ?? "su"]}
                </span>
                <span
                  data-f="dd"
                  className={cn(
                    "text-crumb leading-[1.45] font-semibold tabular-nums",
                    day.today
                      ? "text-state-selected-foreground"
                      : "text-foreground",
                  )}
                >
                  {day.dayOfMonth}
                </span>
                <span
                  data-f="bv"
                  className="mx-0 my-[3px] mb-0.5 flex h-[30px] w-3.5 items-end"
                >
                  <i
                    aria-hidden="true"
                    className={cn(
                      "block w-full rounded-t-[3px] rounded-b-[1px]",
                      day.count > 0 ? "bg-primary" : "bg-line-strong",
                    )}
                    style={{
                      height:
                        day.count > 0 ? `${(day.count / max) * 28}px` : "2px",
                    }}
                  />
                </span>
                <span
                  data-f="dn"
                  className={cn(
                    "text-xs leading-[1.45] tabular-nums",
                    day.count > 0
                      ? "font-[650] text-foreground"
                      : "font-medium text-meta-foreground",
                  )}
                >
                  {day.count}
                </span>
              </button>
            );
          })}
        </div>
        {overdue.count > 0 ? (
          <p
            data-f="wknote"
            className="mt-2.5 mb-0 flex items-center gap-1.5 border-t border-border pt-2.5 text-crumb leading-[1.45] font-semibold text-danger-text"
          >
            <Clock aria-hidden="true" className="size-3.5" strokeWidth={1.8} />
            {formatCopy(copy.list.rail.overdue, { count: overdue.count })}
            {overdue.since ? (
              <span className="font-normal text-meta-foreground">
                {formatCopy(copy.list.rail.overdueSince, {
                  weekday:
                    copy.work.weekdays[
                      WEEKDAY_KEYS[overdue.since.weekday] ?? "sun"
                    ],
                  date: overdue.since.shortDate,
                })}
              </span>
            ) : null}
          </p>
        ) : null}
      </section>

      {departments.length > 0 ? (
        <section
          data-f="rcard"
          aria-labelledby="my-work-departments"
          className={CARD_CLASS}
        >
          <h2
            id="my-work-departments"
            data-f="rh2"
            className={CARD_HEADING_CLASS}
          >
            {copy.list.rail.byDepartment}
            <small className={CARD_SMALL_CLASS}>
              {pluralItems(copy, total)}
            </small>
          </h2>
          <div data-f="dep" className="flex flex-col gap-0.5">
            {departments.map((entry) => (
              <DepartmentRow
                key={entry.department.name}
                copy={copy}
                navigation={navigation}
                entry={entry}
                max={deptMax}
              />
            ))}
          </div>
        </section>
      ) : null}

      {waiting.length > 0 ? (
        <section
          data-f="rcard"
          aria-labelledby="my-work-waiting"
          className={CARD_CLASS}
        >
          <h2 id="my-work-waiting" data-f="rh2" className={CARD_HEADING_CLASS}>
            {copy.list.rail.waiting}
            <small className={CARD_SMALL_CLASS}>{waiting.length}</small>
          </h2>
          {waiting.map((entry) => (
            <div key={entry.id} data-f="wt" className="flex gap-2.5">
              <span
                aria-hidden="true"
                className={cn(
                  "grid size-[26px] shrink-0 place-items-center rounded-full text-avatar-md font-bold",
                  IDENTITY_MARK_CLASS[entry.ownerTone],
                )}
              >
                {initialsFor(entry.ownerName)}
              </span>
              <div className="min-w-0">
                <p className="m-0 text-nav leading-[1.4] font-medium">
                  <ShellLink
                    destination={entry.destination}
                    navigation={navigation}
                    className="no-underline outline-none hover:underline focus-visible:underline"
                  >
                    {entry.title}
                  </ShellLink>
                </p>
                <div className="mt-[3px] mb-2 text-xs leading-[1.45] text-meta-foreground">
                  <span className="font-mono">{entry.key}</span> · {entry.note}{" "}
                  ·{" "}
                  <b className="font-semibold whitespace-nowrap text-warning-text">
                    {entry.dueNote}
                  </b>
                </div>
                <button
                  type="button"
                  aria-disabled={
                    !onRemind || remindingIds?.has(entry.id) ? true : undefined
                  }
                  title={onRemind ? undefined : copy.list.bulk.unavailable}
                  onClick={
                    onRemind && !remindingIds?.has(entry.id)
                      ? () => onRemind(entry)
                      : undefined
                  }
                  className={SMALL_OUTLINE_CLASS}
                >
                  <Bell
                    aria-hidden="true"
                    className="size-3.5"
                    strokeWidth={1.8}
                  />
                  {formatCopy(copy.list.rail.remind, { name: entry.ownerName })}
                </button>
              </div>
            </div>
          ))}
        </section>
      ) : null}
    </aside>
  );
}

function pluralItems(copy: WorkspaceHomeCopy, count: number): string {
  return formatCopy(
    count === 1
      ? copy.list.rail.itemsCount_one
      : copy.list.rail.itemsCount_other,
    { count },
  );
}

function DepartmentRow({
  copy,
  navigation,
  entry,
  max,
}: {
  copy: WorkspaceHomeCopy;
  navigation: ShellNavigation;
  entry: MyWorkDepartmentCount;
  max: number;
}) {
  const { department, count } = entry;
  const className =
    "-mx-2 grid grid-cols-[20px_1fr_auto] items-center gap-x-2.5 gap-y-[5px] rounded-[8px] px-2 py-1.5 text-nav leading-[1.45] font-medium text-foreground no-underline outline-none hover:bg-state-hover focus-visible:ring-2 focus-visible:ring-focus-ring";
  const body = (
    <>
      <DepartmentMark name={department.name} markTone={department.markTone} />
      <span>{department.name}</span>
      <span className="text-crumb leading-[1.45] font-normal text-muted-foreground tabular-nums">
        {pluralItems(copy, count)}
      </span>
      <span
        aria-hidden="true"
        data-f="trk"
        className="col-[2/4] block h-1 overflow-hidden rounded-[2px] bg-state-hover-strong"
      >
        <i
          className={cn(
            "block h-full rounded-[2px]",
            MARK_FILL_CLASS[department.markTone],
          )}
          style={{ width: `${(count / max) * 100}%` }}
        />
      </span>
    </>
  );
  const label = formatCopy(copy.list.rail.departmentLabel, {
    name: department.name,
    count,
  });
  return department.destination ? (
    <ShellLink
      data-f="deprow"
      destination={department.destination}
      navigation={navigation}
      aria-label={label}
      className={className}
    >
      {body}
    </ShellLink>
  ) : (
    <div data-f="deprow" role="group" aria-label={label} className={className}>
      {body}
    </div>
  );
}

/** The floating bar over the list while rows are selected. */
export function MyWorkBulkBar({
  copy,
  count,
  canComplete,
  onComplete,
  onClear,
  unfollowTip,
  unfollowWhy = null,
  onUnfollow = null,
  onSetDue = null,
  onSetStatus = null,
  statusOptions = [],
  today = Date.now(),
}: {
  copy: WorkspaceHomeCopy;
  count: number;
  canComplete: boolean;
  onComplete: () => void;
  onClear: () => void;
  /** The reason "Bỏ theo dõi" is unavailable, shown above it (stories). */
  unfollowTip: ReactNode | null;
  /** The same reason computed from the selection; shown on hover or focus. */
  unfollowWhy?: ReactNode | null;
  /** `null` draws "Bỏ theo dõi" inert. */
  onUnfollow?: (() => void) | null;
  /** `null` draws "Đổi hạn" inert; `null` as the argument clears the date. */
  onSetDue?: ((dueAt: string | null) => void) | null;
  onSetStatus?: ((bucket: WorkStatusBucket) => void) | null;
  statusOptions?: readonly { bucket: WorkStatusBucket; label: string }[];
  /** Reference time for "Hôm nay", "Ngày mai", "Sau 1 tuần". */
  today?: number;
}) {
  const [menu, setMenu] = useState<"due" | "status" | null>(null);
  const [hint, setHint] = useState(false);
  const buttonClass =
    "inline-flex h-8 cursor-pointer items-center gap-1.5 rounded-[8px] border-0 bg-transparent px-2.5 text-compact leading-[1.45] font-medium text-inverse-foreground outline-none hover:bg-inverse-foreground/10 focus-visible:ring-2 focus-visible:ring-focus-ring aria-disabled:cursor-not-allowed aria-disabled:text-inverse-muted aria-disabled:hover:bg-transparent";
  const popClass =
    "absolute bottom-[calc(100%+12px)] left-0 z-dropdown box-content w-[200px] rounded-[10px] border border-line-strong bg-popover p-1.5 whitespace-normal text-foreground shadow-popover dark:bg-card";
  const itemClass =
    "flex h-8 w-full cursor-pointer items-center gap-2 rounded-[7px] border-0 bg-transparent px-2 text-left text-compact leading-[1.45] text-foreground outline-none hover:bg-state-hover-strong focus-visible:ring-2 focus-visible:ring-focus-ring";
  const dayAt = (offset: number): string => {
    const date = new Date(today);
    date.setDate(date.getDate() + offset);
    date.setHours(0, 0, 0, 0);
    return date.toISOString();
  };
  const blocked = unfollowWhy !== null || onUnfollow === null;
  const tip = unfollowTip ?? (hint ? unfollowWhy : null);
  return (
    <div
      data-f="bulk"
      role="toolbar"
      aria-label={formatCopy(copy.list.bulk.label, { count })}
      className="absolute bottom-4 left-1/2 z-20 flex -translate-x-1/2 items-center gap-1 rounded-[12px] bg-inverse-surface py-1.5 pr-1.5 pl-3.5 text-compact leading-[1.45] whitespace-nowrap text-inverse-foreground shadow-popover"
    >
      <b className="mr-2 font-semibold">
        {formatCopy(copy.list.bulk.selected, { count })}
      </b>
      <span className="relative inline-flex">
        <button
          type="button"
          aria-haspopup="menu"
          aria-expanded={menu === "due"}
          aria-disabled={onSetDue ? undefined : true}
          title={onSetDue ? undefined : copy.list.bulk.unavailable}
          onClick={
            onSetDue ? () => setMenu(menu === "due" ? null : "due") : undefined
          }
          className={buttonClass}
        >
          <CalendarDays
            aria-hidden="true"
            className="size-4"
            strokeWidth={1.8}
          />
          {copy.list.bulk.due}
        </button>
        {menu === "due" && onSetDue ? (
          <div
            role="menu"
            aria-label={copy.list.bulk.dueMenu}
            className={popClass}
          >
            {(
              [
                [copy.list.bulk.dueToday, dayAt(0)],
                [copy.list.bulk.dueTomorrow, dayAt(1)],
                [copy.list.bulk.dueNextWeek, dayAt(7)],
                [copy.list.bulk.dueClear, null],
              ] as const
            ).map(([label, value]) => (
              <button
                key={label}
                type="button"
                role="menuitem"
                onClick={() => {
                  setMenu(null);
                  onSetDue(value);
                }}
                className={itemClass}
              >
                {label}
              </button>
            ))}
            <label className="mt-1 flex flex-col gap-1 px-2 text-crumb leading-[1.45] text-meta-foreground">
              {copy.list.bulk.duePick}
              <input
                type="date"
                aria-label={copy.list.bulk.duePick}
                onChange={(event) => {
                  const picked = event.target.value;
                  if (!picked) return;
                  const [y, m, d] = picked.split("-").map(Number);
                  setMenu(null);
                  onSetDue(
                    new Date(y ?? 0, (m ?? 1) - 1, d ?? 1).toISOString(),
                  );
                }}
                className="h-8 rounded-[7px] border border-line-strong bg-background px-2 text-compact text-foreground"
              />
            </label>
          </div>
        ) : null}
      </span>
      <span className="relative inline-flex">
        <button
          type="button"
          aria-haspopup="menu"
          aria-expanded={menu === "status"}
          aria-disabled={
            onSetStatus && statusOptions.length > 0 ? undefined : true
          }
          title={
            onSetStatus && statusOptions.length > 0
              ? undefined
              : copy.list.bulk.unavailable
          }
          onClick={
            onSetStatus && statusOptions.length > 0
              ? () => setMenu(menu === "status" ? null : "status")
              : undefined
          }
          className={buttonClass}
        >
          <span
            aria-hidden="true"
            className="inline-block size-2 rounded-full bg-work-status-doing"
          />
          {copy.list.bulk.status}
        </button>
        {menu === "status" && onSetStatus ? (
          <div
            role="menu"
            aria-label={copy.list.bulk.statusMenu}
            className={popClass}
          >
            {statusOptions.map((option) => (
              <button
                key={option.bucket}
                type="button"
                role="menuitem"
                onClick={() => {
                  setMenu(null);
                  onSetStatus(option.bucket);
                }}
                className={itemClass}
              >
                <StatusDot bucket={option.bucket} />
                {option.label}
              </button>
            ))}
          </div>
        ) : null}
      </span>
      <span
        className="relative inline-flex"
        onMouseEnter={() => setHint(true)}
        onMouseLeave={() => setHint(false)}
        onFocus={() => setHint(true)}
        onBlur={() => setHint(false)}
      >
        <button
          type="button"
          aria-disabled={blocked ? true : undefined}
          aria-describedby={tip ? "my-work-unfollow-why" : undefined}
          title={tip || unfollowWhy ? undefined : copy.list.bulk.unavailable}
          onClick={onUnfollow && unfollowWhy === null ? onUnfollow : undefined}
          className={buttonClass}
        >
          <EyeOff aria-hidden="true" className="size-4" strokeWidth={1.8} />
          {copy.list.bulk.unfollow}
        </button>
        {tip ? (
          <span
            id="my-work-unfollow-why"
            role="tooltip"
            data-f="tip"
            className="absolute bottom-[calc(100%+12px)] left-1/2 box-content w-[272px] -translate-x-1/2 rounded-[9px] border border-line-strong bg-popover px-[11px] py-[9px] text-crumb leading-[1.5] whitespace-normal text-foreground shadow-popover dark:bg-card"
          >
            {tip}
          </span>
        ) : null}
      </span>
      <button
        type="button"
        aria-disabled={canComplete ? undefined : true}
        onClick={canComplete ? onComplete : undefined}
        className={cn(buttonClass, "text-inverse-success")}
      >
        <Check aria-hidden="true" className="size-4" strokeWidth={1.8} />
        {copy.list.bulk.done}
      </button>
      <span
        aria-hidden="true"
        className="mx-1 h-[18px] w-px bg-inverse-foreground/20"
      />
      <button type="button" onClick={onClear} className={buttonClass}>
        {copy.list.bulk.clear}
        <span className="rounded-[4px] border border-inverse-foreground/30 bg-transparent px-[5px] font-mono text-key leading-[18px] text-inverse-foreground/80">
          Esc
        </span>
      </button>
    </div>
  );
}

/** The empty page: no items at all, or none that match the filters. */
export function MyWorkEmpty({
  copy,
  navigation,
  filtered,
  firstDepartment,
  onCreate,
}: {
  copy: WorkspaceHomeCopy;
  navigation: ShellNavigation;
  filtered: boolean;
  firstDepartment: MyWorkDepartment | null;
  onCreate: () => void;
}) {
  return (
    <div
      data-f="emp"
      className="mx-auto mt-14 flex max-w-[640px] flex-col items-center gap-2.5 text-center"
    >
      <span
        data-f="eic"
        className="grid size-[52px] place-items-center rounded-[14px] bg-state-selected text-state-selected-foreground"
      >
        <CircleCheck aria-hidden="true" className="size-6" strokeWidth={1.8} />
      </span>
      <h2 className="mt-1.5 mb-0 text-empty-title">
        {filtered ? copy.list.empty.filtered : copy.list.empty.title}
      </h2>
      <p className="m-0 max-w-[52ch] leading-[1.55] text-muted-foreground">
        {filtered ? copy.list.empty.filteredText : copy.list.empty.text}
      </p>
      {filtered ? null : (
        <div
          data-f="sugg"
          className="mt-4 grid w-full grid-cols-3 gap-2.5 text-left"
        >
          {firstDepartment?.destination ? (
            <Suggestion
              title={formatCopy(copy.list.empty.openDepartment, {
                name: firstDepartment.name,
              })}
              text={copy.list.empty.openDepartmentText}
              icon={
                <DepartmentMark
                  name={firstDepartment.name}
                  markTone={firstDepartment.markTone}
                />
              }
              href={navigation.getHref(firstDepartment.destination)}
              onNavigate={() =>
                navigation.onNavigate(firstDepartment.destination!)
              }
            />
          ) : null}
          <Suggestion
            title={copy.list.empty.follow}
            text={copy.list.empty.followText}
            icon={
              <Eye aria-hidden="true" className="size-4" strokeWidth={1.8} />
            }
            disabled
            reason={copy.list.tabUnavailable.follow}
          />
          <Suggestion
            title={copy.list.empty.create}
            text={copy.list.empty.createText}
            icon={
              <Plus aria-hidden="true" className="size-4" strokeWidth={1.8} />
            }
            onClick={onCreate}
          />
        </div>
      )}
    </div>
  );
}

function Suggestion({
  title,
  text,
  icon,
  href,
  onNavigate,
  onClick,
  disabled,
  reason,
}: {
  title: string;
  text: string;
  icon: ReactNode;
  href?: string;
  onNavigate?: () => void;
  onClick?: () => void;
  disabled?: boolean;
  reason?: string;
}) {
  const className =
    "flex cursor-pointer flex-col items-start gap-1 rounded-[12px] border border-border bg-popover p-3.5 text-left text-foreground no-underline outline-none hover:border-line-strong hover:bg-state-hover focus-visible:ring-2 focus-visible:ring-focus-ring aria-disabled:cursor-not-allowed dark:bg-card";
  const body = (
    <>
      <span
        data-f="sic"
        className="mb-1.5 box-content grid size-[30px] place-items-center rounded-[8px] border border-border bg-background text-muted-foreground"
      >
        {icon}
      </span>
      <b className="text-nav leading-[1.45] font-semibold">{title}</b>
      <small className="text-crumb leading-[1.45] text-meta-foreground">
        {text}
      </small>
    </>
  );
  if (href) {
    return (
      <a
        data-f="sg"
        href={href}
        onClick={(event) => {
          if (
            event.button !== 0 ||
            event.metaKey ||
            event.ctrlKey ||
            event.shiftKey ||
            event.altKey
          )
            return;
          event.preventDefault();
          onNavigate?.();
        }}
        className={className}
      >
        {body}
      </a>
    );
  }
  return (
    <button
      data-f="sg"
      type="button"
      aria-disabled={disabled ? true : undefined}
      title={disabled ? reason : undefined}
      onClick={disabled ? undefined : onClick}
      className={className}
    >
      {body}
    </button>
  );
}

/** The keyboard hint under the list. */
export function MyWorkFoot({ copy }: { copy: WorkspaceHomeCopy }) {
  const key =
    "rounded-[4px] border border-line-strong bg-keycap px-[5px] font-mono text-key leading-[18px] text-muted-foreground";
  return (
    <div
      data-f="foot"
      aria-label={copy.list.shortcuts.label}
      className="flex flex-none flex-wrap items-center gap-1.5 border-t border-border px-7 pt-[9px] pb-[11px] text-crumb leading-[1.45] text-meta-foreground"
    >
      <span className={key}>J</span>
      <span className={key}>K</span>
      {copy.list.shortcuts.move}
      <span className="w-2.5" />
      <span className={key}>X</span>
      {copy.list.shortcuts.select}
      <span className="w-2.5" />
      <span className={key}>E</span>
      {copy.list.shortcuts.done}
      <span className="w-2.5" />
      <span className={key}>⌘K</span>
      {copy.list.shortcuts.command}
    </div>
  );
}

export function MyWorkNote({ children }: { children: ReactNode }) {
  return (
    <p
      data-f="note"
      className="m-0 flex items-center gap-2.5 rounded-[10px] border border-border bg-popover px-3 py-[9px] text-compact leading-[1.45] text-muted-foreground dark:bg-card"
    >
      <Info
        aria-hidden="true"
        className="size-4 shrink-0 text-meta-foreground"
        strokeWidth={1.8}
      />
      {children}
    </p>
  );
}
