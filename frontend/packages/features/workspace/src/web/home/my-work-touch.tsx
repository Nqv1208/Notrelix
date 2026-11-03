import { useId, type ComponentProps, type ReactNode } from "react";
import {
  Calendar,
  Check,
  ChevronDown,
  CircleCheck,
  Ellipsis,
  Eye,
  Flag,
  ListFilter,
  AlignLeft,
  Search,
  SquareCheck,
  User,
} from "lucide-react";
import { cn } from "@notrelix/ui-web";
import { describeMyWorkDue, type MyWorkDue } from "../../core/model/my-work";
import { initialsFor } from "../../core/model/navigation";
import {
  formatCopy,
  IDENTITY_MARK_CLASS,
  isPlainClick,
} from "../shell/shell-models";
import type { MyWorkItemView, MyWorkReason, WorkPriority } from "./home-models";
import { Anchor, GroupIcon, MyWorkCreate, POP_CLASS } from "./my-work-chrome";
import {
  MY_WORK_TABS,
  type MyWorkGroupBy,
  type MyWorkTab,
} from "./my-work-model";
import {
  DepartmentMark,
  dueWords,
  PRIORITY_TEXT_CLASS,
  STATUS_PILL_CLASS,
  StatusDot,
} from "./work-row-parts";
import type { WorkspaceHomeCopy } from "./workspace-home.copy";

/**
 * My work on a touch screen: `Final-T-MyWork` (tablet, 768 to 1279 px: a segmented tab control, a group and a
 * filter chip, rows of 56 px with the reason, the Status and the due day) and `Final-M-MyWork` (phone: chips for
 * the tabs, a card list whose rows give the due day and Priority on the right). Every target is 44 px. The rail
 * of the desktop is not drawn; the week and the departments belong to Home and the calendar.
 */

export type MyWorkTouchLayout = "tablet" | "phone";

const ICON_BUTTON_CLASS =
  "relative grid size-11 shrink-0 cursor-pointer place-items-center rounded-[10px] border-0 bg-transparent p-0 text-muted-foreground no-underline outline-none hover:bg-state-hover-strong hover:text-foreground focus-visible:ring-2 focus-visible:ring-focus-ring aria-disabled:cursor-not-allowed";
const CHIP_CLASS =
  "box-border inline-flex h-11 shrink-0 cursor-pointer items-center gap-[7px] rounded-[10px] border border-line-strong bg-popover px-3.5 text-sm leading-[1.45] font-medium whitespace-nowrap text-muted-foreground outline-none hover:bg-state-hover hover:text-foreground aria-expanded:bg-state-hover focus-visible:ring-2 focus-visible:ring-focus-ring dark:bg-card";
const TOUCH_BUTTON_CLASS =
  "box-border inline-flex h-11 shrink-0 cursor-pointer items-center justify-center gap-[7px] rounded-[10px] border border-line-strong bg-popover px-3.5 text-item-prose leading-[1.45] font-medium whitespace-nowrap text-foreground no-underline outline-none hover:bg-state-hover focus-visible:ring-2 focus-visible:ring-focus-ring aria-disabled:cursor-not-allowed dark:bg-card";
const TOUCH_PRIMARY_CLASS =
  "box-border inline-flex h-11 shrink-0 cursor-pointer items-center justify-center gap-[7px] rounded-[10px] border border-primary bg-primary px-3.5 text-item-prose leading-[1.45] font-semibold whitespace-nowrap text-primary-foreground outline-none hover:bg-primary/90 focus-visible:ring-2 focus-visible:ring-focus-ring aria-disabled:cursor-not-allowed";

type BarProps = ComponentProps<typeof import("./my-work-chrome").MyWorkBar>;

export interface MyWorkTouchChromeProps {
  readonly layout: MyWorkTouchLayout;
  readonly copy: WorkspaceHomeCopy;
  readonly subline: string;
  readonly calendarHref: string | null;
  readonly tab: MyWorkTab;
  readonly counts: Record<Exclude<MyWorkTab, "done">, number>;
  readonly available: Record<MyWorkTab, boolean>;
  readonly onTab: (tab: MyWorkTab) => void;
  readonly showCounts: boolean;
  readonly bar: BarProps;
}

/** Title, tabs and the group and filter chips of the touch layouts. */
export function MyWorkTouchChrome(props: MyWorkTouchChromeProps) {
  const { layout, copy, bar } = props;
  const phone = layout === "phone";
  const calendar = props.calendarHref ? (
    <a
      data-f="hbtn"
      href={props.calendarHref}
      aria-label={copy.list.calendar}
      title={copy.list.calendar}
      className={ICON_BUTTON_CLASS}
    >
      <Calendar aria-hidden="true" className="size-5" strokeWidth={1.8} />
    </a>
  ) : (
    <button
      data-f="hbtn"
      type="button"
      aria-disabled="true"
      aria-label={copy.list.calendar}
      title={copy.list.calendarUnavailable}
      className={ICON_BUTTON_CLASS}
    >
      <Calendar aria-hidden="true" className="size-5" strokeWidth={1.8} />
    </button>
  );
  const create = (
    <MyWorkCreate
      copy={copy}
      menu={bar.menu}
      onMenu={bar.onMenu}
      createBoards={bar.createBoards}
      createBoardId={bar.createBoardId}
      onCreateBoard={bar.onCreateBoard}
      createName={bar.createName}
      onCreateName={bar.onCreateName}
      onCreate={bar.onCreate}
      onOpenForm={bar.onOpenForm}
      icon={phone ? "plain" : true}
    />
  );
  return (
    <>
      {phone ? (
        <div data-f="head" className="relative flex-none px-4 pt-0.5">
          <h1
            data-f="h1"
            className="m-0 text-item-title leading-[1.2] font-bold tracking-[-0.02em] text-foreground"
          >
            {copy.list.title}
          </h1>
          <p
            data-f="sub"
            className="mt-1 mb-0 text-compact leading-[1.45] font-normal text-muted-foreground"
          >
            {props.subline}
          </p>
          <div className="absolute -top-1.5 right-1.5 flex items-center">
            {bar.hasItems ? <FilterChip {...props} icon /> : null}
            {create}
          </div>
        </div>
      ) : (
        <div
          data-f="head"
          className="box-border flex h-14 flex-none items-center gap-1.5 border-b border-line py-0 pr-3 pl-4"
        >
          <div className="flex min-w-0 items-baseline gap-2.5">
            <h1
              data-f="h1"
              className="m-0 text-calendar-title leading-[1.45] font-[650] tracking-[-0.01em] whitespace-nowrap text-foreground"
            >
              {copy.list.title}
            </h1>
            <span
              data-f="sub"
              className="truncate text-item-ui leading-[1.45] text-meta-foreground"
            >
              {props.subline}
            </span>
          </div>
          <span className="flex-1" />
          {calendar}
          {create}
        </div>
      )}
      {phone ? (
        <Chips {...props} />
      ) : (
        <div
          data-f="bar"
          role="group"
          aria-label={copy.list.tabsLabel}
          className="flex h-[60px] flex-none items-center gap-2 overflow-x-auto px-4 [scrollbar-width:none]"
        >
          <Segment {...props} />
          <span className="flex-1" />
          {bar.hasItems ? (
            <>
              <GroupChip {...props} />
              <FilterChip {...props} />
            </>
          ) : null}
        </div>
      )}
    </>
  );
}

function tabLabel(copy: WorkspaceHomeCopy, id: MyWorkTab, phone: boolean) {
  if (phone && id === "approve") return copy.list.touch.tabApprove;
  if (phone && id === "follow") return copy.list.touch.tabFollow;
  return copy.list.tabs[id];
}

function tabProps(props: MyWorkTouchChromeProps, id: MyWorkTab) {
  const { copy } = props;
  const usable = props.available[id];
  return {
    id: `my-work-tab-${id}`,
    role: "tab" as const,
    type: "button" as const,
    "aria-selected": props.tab === id,
    "aria-controls": "mw-panel",
    "aria-disabled": usable ? undefined : true,
    title:
      usable || id === "all" || id === "mine"
        ? undefined
        : copy.list.tabUnavailable[id as "approve" | "follow" | "done"],
    tabIndex: props.tab === id ? 0 : -1,
    onClick: usable ? () => props.onTab(id) : undefined,
    count:
      id === "done" || !usable || !props.showCounts ? null : props.counts[id],
  };
}

/** The tablet's segmented control (`.t-seg .mw-seg`). */
function Segment(props: MyWorkTouchChromeProps) {
  return (
    <div
      data-f="tabs"
      role="tablist"
      aria-label={props.copy.list.tabsLabel}
      className="box-border flex h-12 flex-none items-center rounded-[12px] bg-state-hover-strong p-0.5"
    >
      {MY_WORK_TABS.map((id) => {
        const { count, ...rest } = tabProps(props, id);
        const on = props.tab === id;
        return (
          <button
            key={id}
            data-f="tab"
            {...rest}
            className={cn(
              "inline-flex h-11 items-center gap-[7px] rounded-[10px] border-0 px-3 text-sm leading-[1.45] whitespace-nowrap outline-none focus-visible:ring-2 focus-visible:ring-focus-ring",
              on
                ? "bg-popover font-[650] text-foreground shadow-[0_1px_3px_rgba(15,23,42,0.14)] dark:bg-card"
                : "bg-transparent font-medium text-muted-foreground",
              rest["aria-disabled"]
                ? "cursor-not-allowed"
                : "cursor-pointer hover:text-foreground",
            )}
          >
            {props.copy.list.tabs[id]}
            {count !== null ? (
              <span
                data-f="tabn"
                className={cn(
                  "text-crumb leading-[1.45] font-semibold tabular-nums",
                  on
                    ? "text-state-selected-foreground"
                    : "text-meta-foreground",
                )}
              >
                {count}
              </span>
            ) : null}
          </button>
        );
      })}
    </div>
  );
}

/** The phone's tab chips (`.chips .chip`): the chosen one is inverted. */
function Chips(props: MyWorkTouchChromeProps) {
  return (
    <div
      data-f="tabs"
      role="tablist"
      aria-label={props.copy.list.tabsLabel}
      className="mt-[18px] flex flex-none gap-2 overflow-x-auto px-4 [scrollbar-width:none]"
    >
      {MY_WORK_TABS.map((id) => {
        const { count, ...rest } = tabProps(props, id);
        const on = props.tab === id;
        return (
          <button
            key={id}
            data-f="tab"
            {...rest}
            className={cn(
              "box-border inline-flex h-11 flex-none items-center gap-1.5 rounded-[12px] border px-3.5 text-sm leading-[1.45] whitespace-nowrap outline-none focus-visible:ring-2 focus-visible:ring-focus-ring",
              on
                ? "border-foreground bg-foreground font-semibold text-background"
                : "border-line-strong bg-popover font-medium text-foreground dark:bg-card",
              rest["aria-disabled"] ? "cursor-not-allowed" : "cursor-pointer",
            )}
          >
            {tabLabel(props.copy, id, true)}
            {count !== null ? (
              <span
                data-f="tabn"
                className={cn(
                  "font-bold tabular-nums",
                  on ? "text-inherit" : "text-meta-foreground",
                )}
              >
                {count}
              </span>
            ) : null}
          </button>
        );
      })}
    </div>
  );
}

/** A 44 px option row of the group menu and the filter popover. */
function Option({
  label,
  selected,
  role,
  onToggle,
  leading,
  disabled,
  title,
}: {
  label: string;
  selected: boolean;
  role: "option" | "menuitem" | "menuitemradio";
  onToggle: () => void;
  leading?: ReactNode;
  disabled?: boolean;
  title?: string;
}) {
  return (
    <button
      type="button"
      role={role}
      aria-selected={role === "option" ? selected : undefined}
      aria-checked={role === "menuitemradio" ? selected : undefined}
      aria-disabled={disabled ? true : undefined}
      title={title}
      onClick={disabled ? undefined : onToggle}
      className={cn(
        "flex min-h-11 w-full items-center gap-3 rounded-[9px] border-0 bg-transparent px-3 text-left text-item-heading leading-[1.45] font-medium text-foreground outline-none hover:bg-state-hover-strong focus-visible:ring-2 focus-visible:ring-focus-ring",
        disabled
          ? "cursor-not-allowed text-muted-foreground"
          : "cursor-pointer",
      )}
    >
      {leading}
      <span className="min-w-0 flex-1 truncate">{label}</span>
      {selected ? (
        <Check
          aria-hidden="true"
          className="size-5 text-state-selected-foreground"
          strokeWidth={1.8}
        />
      ) : null}
    </button>
  );
}

function GroupChip(props: MyWorkTouchChromeProps) {
  const { copy, bar } = props;
  const open = bar.menu === "group";
  return (
    <Anchor open={open} onClose={() => bar.onMenu(null)}>
      <button
        data-f="pill"
        type="button"
        aria-haspopup="menu"
        aria-expanded={open}
        onClick={() => bar.onMenu(open ? null : "group")}
        className={CHIP_CLASS}
      >
        <AlignLeft
          aria-hidden="true"
          className="size-[15px] text-meta-foreground"
          strokeWidth={1.8}
        />
        {copy.list.touch.groupBy}{" "}
        <b className="font-[650] text-foreground">
          {copy.list.touch.groupValue[bar.groupBy]}
        </b>
      </button>
      {open ? (
        <div
          data-f="pop"
          role="menu"
          aria-label={copy.list.groupMenuLabel}
          className={cn(POP_CLASS, "top-12 right-0 w-[280px] rounded-[14px]")}
        >
          <GroupOptions {...props} />
        </div>
      ) : null}
    </Anchor>
  );
}

function GroupOptions(props: MyWorkTouchChromeProps) {
  const { copy, bar } = props;
  return (
    <>
      {(["due", "board", "status"] as const).map((id: MyWorkGroupBy) => {
        const usable = bar.groupAvailable[id];
        return (
          <Option
            key={id}
            role="menuitemradio"
            label={copy.list.groupBy[id]}
            selected={bar.groupBy === id}
            disabled={!usable}
            title={usable ? undefined : copy.list.groupUnavailable}
            leading={<GroupIcon id={id} />}
            onToggle={() => {
              bar.onGroup(id);
              bar.onMenu(null);
            }}
          />
        );
      })}
    </>
  );
}

function FilterChip(props: MyWorkTouchChromeProps & { icon?: boolean }) {
  const { copy, bar, layout } = props;
  const phone = layout === "phone";
  const open = bar.menu === "filter";
  const titleId = useId();
  const active =
    bar.selectedDepartments.length +
    bar.selectedBoards.length +
    bar.selectedPriorities.length;
  const close = () => bar.onMenu(null);
  return (
    <Anchor open={open} onClose={close}>
      <button
        data-f="pill"
        type="button"
        aria-haspopup="dialog"
        aria-expanded={open}
        aria-label={props.icon ? copy.list.touch.filter : undefined}
        onClick={() => bar.onMenu(open ? null : "filter")}
        className={props.icon ? ICON_BUTTON_CLASS : CHIP_CLASS}
      >
        <ListFilter
          aria-hidden="true"
          className={props.icon ? "size-5" : "size-[15px] text-meta-foreground"}
          strokeWidth={1.8}
        />
        {props.icon ? null : copy.list.touch.filter}
        {active > 0 ? (
          <b className="font-[650] text-foreground">{active}</b>
        ) : null}
      </button>
      {open ? (
        <div
          data-f="filterpop"
          role="dialog"
          aria-modal="false"
          aria-labelledby={titleId}
          className={cn(
            POP_CLASS,
            phone
              ? "fixed top-[112px] right-4 left-4 flex max-h-[calc(100dvh-136px)] flex-col gap-1 overflow-y-auto rounded-[14px]"
              : "top-12 right-0 flex max-h-[70vh] w-[344px] flex-col gap-1 overflow-y-auto rounded-[14px]",
          )}
        >
          <h2
            id={titleId}
            className="m-0 px-2.5 pt-2 pb-2 text-item-heading leading-[1.45]"
          >
            {copy.list.touch.filterTitle}
          </h2>
          <label className="mx-1 mb-1.5 box-border flex h-11 items-center gap-2 rounded-[10px] border border-line-strong bg-background px-3 text-meta-foreground focus-within:border-primary dark:bg-card">
            <Search
              aria-hidden="true"
              className="size-5 shrink-0"
              strokeWidth={1.8}
            />
            <input
              type="search"
              value={bar.query}
              onChange={(event) => bar.onQuery(event.target.value)}
              placeholder={copy.list.searchPlaceholder}
              aria-label={copy.list.searchLabel}
              className="w-full min-w-0 border-0 bg-transparent p-0 text-item-prose leading-[1.45] text-foreground outline-none placeholder:text-meta-foreground"
            />
          </label>
          {phone ? (
            <>
              <Heading>{copy.list.groupMenuLabel}</Heading>
              <div role="menu" aria-label={copy.list.groupMenuLabel}>
                <GroupOptions {...props} />
              </div>
            </>
          ) : null}
          {bar.departments.length > 0 ? (
            <>
              <Heading>{copy.list.filters.department}</Heading>
              <div
                role="listbox"
                aria-multiselectable="true"
                aria-label={copy.list.filters.department}
              >
                {bar.departments.map((department) => (
                  <Option
                    key={department.name}
                    role="option"
                    label={department.name}
                    selected={bar.selectedDepartments.includes(department.name)}
                    onToggle={() => bar.onToggleDepartment(department.name)}
                    leading={
                      <DepartmentMark
                        small
                        name={department.name}
                        markTone={department.markTone}
                      />
                    }
                  />
                ))}
              </div>
            </>
          ) : null}
          {bar.boards.length > 0 ? (
            <>
              <Heading>{copy.list.filters.board}</Heading>
              <div
                role="listbox"
                aria-multiselectable="true"
                aria-label={copy.list.filters.board}
              >
                {bar.boards.map((board) => (
                  <Option
                    key={board.id}
                    role="option"
                    label={board.name}
                    selected={bar.selectedBoards.includes(board.id)}
                    onToggle={() => bar.onToggleBoard(board.id)}
                  />
                ))}
              </div>
            </>
          ) : null}
          {bar.onTogglePriority ? (
            <>
              <Heading>{copy.list.filters.priority}</Heading>
              <div
                role="listbox"
                aria-multiselectable="true"
                aria-label={copy.list.filters.priority}
              >
                {(["urgent", "high", "medium", "low"] as const).map(
                  (priority) => (
                    <Option
                      key={priority}
                      role="option"
                      label={copy.work.priority[priority]}
                      selected={bar.selectedPriorities.includes(priority)}
                      onToggle={() => bar.onTogglePriority?.(priority)}
                    />
                  ),
                )}
              </div>
            </>
          ) : null}
          <div className="mt-1 flex justify-end gap-2 border-t border-line px-1 pt-2">
            <button
              type="button"
              onClick={close}
              className={TOUCH_PRIMARY_CLASS}
            >
              {copy.list.touch.filterClose}
            </button>
          </div>
        </div>
      ) : null}
    </Anchor>
  );
}

function Heading({ children }: { children: ReactNode }) {
  return (
    <p
      aria-hidden="true"
      className="m-0 px-2.5 pt-2 pb-1 text-item-small leading-[1.45] font-semibold text-meta-foreground"
    >
      {children}
    </p>
  );
}

/** One group heading: the 44 px toggle with its count (tablet), or the plain heading of the card list (phone). */
export function MyWorkTouchGroupHead({
  layout,
  id,
  name,
  count,
  over,
  open,
  toggleLabel,
  onToggle,
  children,
}: {
  layout: MyWorkTouchLayout;
  id: string;
  name: ReactNode;
  count: number;
  over: boolean;
  open: boolean;
  toggleLabel: string;
  onToggle: () => void;
  children?: ReactNode;
}) {
  if (layout === "phone") {
    return (
      <h2
        id={id}
        data-f="gh"
        className={cn(
          "m-0 mb-1 flex min-h-10 items-center gap-2 text-calendar-title leading-[1.45] font-[650] tracking-[-0.01em]",
          over ? "text-danger-text" : "text-foreground",
        )}
      >
        {name}
        <span
          data-f="gn"
          className="text-item-heading leading-[1.45] font-medium text-meta-foreground tabular-nums"
        >
          {count}
        </span>
        {children}
      </h2>
    );
  }
  return (
    <h2 id={id} data-f="gh" className="m-0 text-[length:inherit]">
      <button
        data-f="tog"
        type="button"
        aria-expanded={open}
        aria-label={toggleLabel}
        onClick={onToggle}
        className={cn(
          "-ml-2 inline-flex h-11 cursor-pointer items-center gap-2 rounded-[10px] border-0 bg-transparent py-0 pr-3 pl-2 text-item-heading leading-[1.45] font-[650] outline-none hover:bg-state-hover-strong focus-visible:ring-2 focus-visible:ring-focus-ring",
          over ? "text-danger-text" : "text-foreground",
        )}
      >
        <ChevronDown
          aria-hidden="true"
          className={cn(
            "size-[15px] text-meta-foreground transition-transform",
            !open && "-rotate-90",
          )}
          strokeWidth={1.8}
        />
        {name}
        <span
          data-f="gn"
          className={cn(
            "box-border inline-grid h-6 min-w-6 place-items-center rounded-xl px-[7px] text-crumb leading-[1.45] font-semibold tabular-nums",
            over
              ? "bg-danger-subtle text-danger-text"
              : "bg-state-hover-strong text-muted-foreground",
          )}
        >
          {count}
        </span>
      </button>
      {children}
    </h2>
  );
}

const REASON_CLASS: Record<MyWorkReason, string> = {
  follow: "bg-state-hover-strong text-muted-foreground",
  mine: "bg-state-selected text-state-selected-foreground",
  approve: "bg-warning-subtle text-warning-text",
};

const REASON_ICON = { follow: Eye, mine: User, approve: CircleCheck } as const;

/** Only urgent and high flag a row; a medium or low Priority is the quiet default. */
function showsPriority(
  priority: WorkPriority | null | undefined,
): priority is WorkPriority {
  return priority === "urgent" || priority === "high";
}

function dueSubline(copy: WorkspaceHomeCopy, due: MyWorkDue): string | null {
  if (due.kind === "overdue")
    return formatCopy(copy.list.dueOn, { date: due.shortDate });
  if (due.kind === "today" && due.time) return due.time;
  return null;
}

export interface MyWorkTouchRowProps {
  readonly layout: MyWorkTouchLayout;
  readonly item: MyWorkItemView;
  readonly copy: WorkspaceHomeCopy;
  readonly now: number;
  readonly showBoard: boolean;
  readonly current: boolean;
  readonly completing: boolean;
  readonly canComplete: boolean;
  readonly done: boolean;
  readonly href: string;
  readonly menuOpen: boolean;
  readonly onMenu: () => void;
  readonly onComplete: () => void;
  readonly onOpen: () => void;
  readonly actions?: {
    readonly busy: boolean;
    readonly onApprove: () => void;
    readonly onRequestChanges: () => void;
    readonly onReopen: () => void;
  };
}

function PriorityMark({
  copy,
  priority,
  className,
}: {
  copy: WorkspaceHomeCopy;
  priority: WorkPriority;
  className?: string;
}) {
  return (
    <span
      data-f="pr"
      className={cn(
        "inline-flex flex-none items-center gap-[3px] leading-[inherit] font-semibold",
        PRIORITY_TEXT_CLASS[priority],
        className,
      )}
    >
      <Flag aria-hidden="true" className="size-[13px]" strokeWidth={1.8} />
      <span className="sr-only">
        {formatCopy(copy.work.priorityLabel, { priority: "" })}
      </span>
      {copy.work.priority[priority]}
    </span>
  );
}

/** The round tick: a 44 px target around the 22 px ring, a done mark, or the dashed ring of an approval. */
function Tick(props: MyWorkTouchRowProps & { name: string; canTick: boolean }) {
  const { copy, item, name } = props;
  if (props.done)
    return (
      <span
        data-f="tick"
        role="img"
        aria-label={copy.list.doneStatus}
        className="grid size-11 flex-none place-items-center"
      >
        <span className="grid size-[22px] place-items-center rounded-full bg-work-status-done text-card">
          <Check aria-hidden="true" className="size-3.5" strokeWidth={3} />
        </span>
      </span>
    );
  if (item.reason === "approve")
    return (
      <span
        data-f="tick"
        aria-hidden="true"
        className="grid size-11 flex-none place-items-center"
      >
        <span className="box-border size-[22px] rounded-full border-[1.75px] border-dashed border-work-status-review" />
      </span>
    );
  return (
    <button
      data-f="tick"
      type="button"
      aria-label={formatCopy(copy.list.complete, { name })}
      title={formatCopy(copy.list.complete, { name })}
      aria-disabled={!props.canTick || props.completing ? true : undefined}
      aria-busy={props.completing ? true : undefined}
      onClick={
        props.canTick && !props.completing ? props.onComplete : undefined
      }
      className={cn(
        "relative z-20 grid size-11 flex-none place-items-center rounded-[12px] border-0 bg-transparent p-0 outline-none hover:bg-state-hover-strong focus-visible:ring-2 focus-visible:ring-focus-ring",
        props.canTick ? "cursor-pointer" : "cursor-not-allowed",
        props.completing && "opacity-60",
      )}
    >
      <span
        className={cn(
          "box-border size-[22px] rounded-full border-[1.75px] border-line-strong",
          !props.canTick && "border-dashed",
        )}
      />
    </button>
  );
}

/** The ⋯ menu of a row: open the item, or tick it. */
function RowMenu(props: MyWorkTouchRowProps & { name: string }) {
  const { copy, name } = props;
  const label = formatCopy(copy.list.touch.more, { name });
  return (
    <Anchor
      open={props.menuOpen}
      onClose={() => props.menuOpen && props.onMenu()}
    >
      <button
        data-f="more"
        type="button"
        aria-haspopup="menu"
        aria-expanded={props.menuOpen}
        aria-label={label}
        onClick={props.onMenu}
        className={cn(
          ICON_BUTTON_CLASS,
          "z-20 rounded-[12px] text-meta-foreground",
        )}
      >
        <Ellipsis aria-hidden="true" className="size-5" strokeWidth={1.8} />
      </button>
      {props.menuOpen ? (
        <div
          data-f="pop"
          role="menu"
          aria-label={label}
          className={cn(POP_CLASS, "top-11 right-0 z-dropdown w-[220px]")}
        >
          <Option
            role="menuitem"
            label={copy.list.touch.open}
            selected={false}
            onToggle={() => {
              props.onMenu();
              props.onOpen();
            }}
          />
          {!props.done && props.item.reason !== "approve" ? (
            <Option
              role="menuitem"
              label={copy.list.touch.done}
              selected={false}
              disabled={!props.canComplete}
              leading={
                <SquareCheck
                  aria-hidden="true"
                  className="size-5 text-meta-foreground"
                  strokeWidth={1.8}
                />
              }
              onToggle={() => {
                props.onMenu();
                props.onComplete();
              }}
            />
          ) : null}
        </div>
      ) : null}
    </Anchor>
  );
}

function TitleLink(
  props: MyWorkTouchRowProps & { className: string; stretch: boolean },
) {
  return (
    <a
      href={props.href}
      onClick={(event) => {
        if (!isPlainClick(event)) return;
        event.preventDefault();
        props.onOpen();
      }}
      className={cn(
        "text-foreground no-underline outline-none",
        props.stretch &&
          "after:absolute after:inset-0 after:content-[''] focus-visible:after:rounded-[10px] focus-visible:after:outline-2 focus-visible:after:-outline-offset-[3px] focus-visible:after:outline-focus-ring",
        props.className,
      )}
    >
      {props.item.name}
    </a>
  );
}

/** The board and key line under a title (`.mr-m` / `.lr-m`). */
function MetaLine({
  props,
  tablet,
}: {
  props: MyWorkTouchRowProps;
  tablet: boolean;
}) {
  const { item } = props;
  return (
    <div
      data-f="rc"
      className="mt-[3px] flex min-w-0 items-center gap-1.5 text-item-small leading-[1.45] whitespace-nowrap text-meta-foreground"
    >
      {props.showBoard && item.department ? (
        <DepartmentMark
          small
          name={item.department.name}
          markTone={item.department.markTone}
          className={tablet ? "size-[18px] rounded-[5px]" : undefined}
        />
      ) : null}
      {props.showBoard && item.boardName ? (
        <span className="min-w-0 truncate">{item.boardName}</span>
      ) : null}
      {item.key ? (
        <span
          data-f="key"
          className={cn(
            "flex-none font-mono",
            tablet ? "text-item-note" : "text-item-note",
          )}
        >
          {item.key}
        </span>
      ) : null}
      {tablet && showsPriority(item.priority) && !props.done ? (
        <PriorityMark
          copy={props.copy}
          priority={item.priority}
          className="text-crumb"
        />
      ) : null}
    </div>
  );
}

export function MyWorkTouchRow(props: MyWorkTouchRowProps) {
  const { item, copy, now, done, layout } = props;
  const due = describeMyWorkDue(item.dueAt, now);
  const completed = done
    ? describeMyWorkDue(item.completedAt ?? null, now)
    : undefined;
  const name = item.key ?? item.name;
  const canTick = props.canComplete && item.reason !== "approve";
  const words = dueWords(copy, completed ?? due, { timeOnToday: false });
  const sub = completed
    ? completed.kind === "today" && completed.time
      ? copy.list.sinceToday
      : null
    : dueSubline(copy, due);
  const dueText = completed
    ? completed.kind === "today"
      ? (completed.time ?? copy.work.due.today)
      : words.text
    : words.text;
  const toneClass =
    !completed && words.tone === "over"
      ? "text-danger-text"
      : !completed && words.tone === "today"
        ? "text-warning-text"
        : "text-muted-foreground";
  const approve = !done && item.approval ? item.approval : null;
  const rowProps = { ...props, name, canTick };

  const approvalButtons = approve ? (
    <>
      <button
        type="button"
        aria-disabled={!props.actions || props.actions.busy ? true : undefined}
        title={props.actions ? undefined : copy.list.bulk.unavailable}
        aria-label={`${copy.list.approve} ${name}`}
        onClick={
          props.actions && !props.actions.busy
            ? props.actions.onApprove
            : undefined
        }
        className={cn(
          TOUCH_PRIMARY_CLASS,
          "relative z-20",
          layout === "phone" && "px-4 text-sm",
        )}
      >
        {layout === "tablet" ? (
          <Check aria-hidden="true" className="size-[15px]" strokeWidth={2} />
        ) : null}
        {copy.list.approve}
      </button>
      <button
        type="button"
        aria-disabled={!props.actions || props.actions.busy ? true : undefined}
        title={props.actions ? undefined : copy.list.bulk.unavailable}
        aria-label={`${copy.list.requestChanges} ${name}`}
        onClick={
          props.actions && !props.actions.busy
            ? props.actions.onRequestChanges
            : undefined
        }
        className={cn(
          TOUCH_BUTTON_CLASS,
          "relative z-20",
          layout === "phone" && "px-4 text-sm",
        )}
      >
        {copy.list.requestChanges}
      </button>
    </>
  ) : null;
  const reopen = done ? (
    <button
      type="button"
      aria-disabled={!props.actions || props.actions.busy ? true : undefined}
      title={props.actions ? undefined : copy.list.bulk.unavailable}
      aria-label={formatCopy(copy.list.reopen, { name })}
      onClick={
        props.actions && !props.actions.busy
          ? props.actions.onReopen
          : undefined
      }
      className={cn(TOUCH_BUTTON_CLASS, "relative z-20 justify-self-start")}
    >
      {copy.list.reopenAction}
    </button>
  ) : null;

  if (layout === "phone") {
    const who =
      item.reason === "follow" && item.assignee
        ? formatCopy(copy.list.touch.inCharge, { name: item.assignee.name })
        : item.reason === "approve" && item.approval
          ? `${item.approval.actorName} ${formatCopy(copy.list.approvalSent, { time: item.approval.time })}`
          : (item.status?.label ?? null);
    const why =
      item.reason === "follow"
        ? copy.list.touch.tabFollow
        : item.reason
          ? copy.work.reasons[item.reason]
          : "";
    const Icon = item.reason ? REASON_ICON[item.reason] : null;
    return (
      <li
        data-f="row"
        data-item-id={item.id}
        aria-current={props.current ? "true" : undefined}
        className="relative list-none overflow-hidden border-t border-line first:border-t-0"
      >
        <div className="relative z-10 flex items-start gap-0 bg-popover py-1.5 pr-0 pl-0.5 dark:bg-card">
          <Tick {...rowProps} />
          <div className="relative flex min-h-11 min-w-0 flex-1 flex-wrap items-start gap-x-2.5 py-[5px]">
            <div data-f="rm" className="min-w-0 flex-1">
              <p
                data-f="rt"
                className={cn(
                  "m-0 line-clamp-2 overflow-hidden text-item-heading leading-[1.36] font-medium",
                  done && "text-muted-foreground",
                )}
              >
                <TitleLink {...props} stretch className="" />
              </p>
              <MetaLine props={props} tablet={false} />
            </div>
            <span
              data-f="cdu"
              className={cn(
                "flex w-[70px] flex-none flex-col items-end gap-1 pt-px text-right text-item-small leading-[1.3] tabular-nums",
                toneClass,
                !completed && words.tone !== "normal" && "font-semibold",
              )}
            >
              <span className="sr-only">
                {completed ? copy.list.doneLabel : copy.list.dueLabel}
              </span>
              {dueText}
              {!completed && due.kind === "today" && due.time
                ? ` ${due.time}`
                : null}
              {showsPriority(item.priority) && !done ? (
                <PriorityMark
                  copy={copy}
                  priority={item.priority}
                  className="text-item-note"
                />
              ) : null}
            </span>
            {Icon && !done ? (
              <span
                data-f="why"
                className={cn(
                  "mt-[3px] flex basis-full items-start gap-[5px] text-item-small leading-[1.35]",
                  item.reason === "approve"
                    ? "font-medium text-warning-text"
                    : "text-muted-foreground",
                )}
              >
                <Icon
                  aria-hidden="true"
                  className="mt-px size-3.5 flex-none"
                  strokeWidth={1.8}
                />
                <span>{[why, who].filter(Boolean).join(" · ")}</span>
              </span>
            ) : null}
          </div>
          {done ? reopen : <RowMenu {...rowProps} />}
        </div>
        {approve ? (
          <div className="relative z-10 flex gap-2 bg-popover px-3 pr-3 pb-3 pl-[46px] dark:bg-card">
            {approvalButtons}
          </div>
        ) : null}
      </li>
    );
  }

  const ReasonIcon = item.reason ? REASON_ICON[item.reason] : null;
  return (
    <li
      data-f="row"
      data-item-id={item.id}
      aria-current={props.current ? "true" : undefined}
      className="relative list-none overflow-hidden border-t border-line first:border-t-0"
    >
      <div
        className={cn(
          "relative z-10 box-border grid min-h-14 grid-cols-[52px_minmax(0,1fr)_156px_118px_128px_50px] items-center bg-popover py-1.5 dark:bg-card",
          props.current && "bg-state-selected dark:bg-state-selected",
        )}
      >
        <div className="ml-1.5">
          <Tick {...rowProps} />
        </div>
        <div data-f="rm" className="relative min-w-0 py-[3px] pr-3.5 pl-0.5">
          <p
            data-f="rt"
            className={cn(
              "m-0 line-clamp-2 overflow-hidden text-item-heading leading-[1.38] font-medium [text-wrap:balance]",
              done && "text-muted-foreground",
            )}
          >
            <TitleLink {...props} stretch className="" />
          </p>
          <MetaLine props={props} tablet />
        </div>
        <span data-f="why" className="justify-self-start">
          {item.reason && ReasonIcon ? (
            <span
              className={cn(
                "inline-flex h-7 items-center gap-1.5 rounded-full px-2.5 text-item-small leading-[1.45] font-semibold whitespace-nowrap",
                REASON_CLASS[item.reason],
                item.reason === "follow" && item.assignee && "pr-1",
              )}
            >
              <ReasonIcon
                aria-hidden="true"
                className="size-3.5"
                strokeWidth={1.8}
              />
              {copy.work.reasons[item.reason]}
              {item.reason === "follow" && item.assignee ? (
                <span
                  role="img"
                  aria-label={formatCopy(copy.list.assignee, {
                    name: item.assignee.name,
                  })}
                  title={item.assignee.name}
                  className={cn(
                    "grid size-[22px] place-items-center rounded-full text-avatar-sm font-bold",
                    IDENTITY_MARK_CLASS[item.assignee.tone],
                  )}
                >
                  {initialsFor(item.assignee.name)}
                </span>
              ) : null}
            </span>
          ) : null}
        </span>
        <span data-f="cst" className="justify-self-start">
          {done ? (
            reopen
          ) : item.status ? (
            <span
              data-f="stp"
              className={cn(
                "inline-flex h-7 items-center gap-1.5 rounded-full px-2.5 text-item-small leading-[1.45] font-semibold whitespace-nowrap text-foreground",
                STATUS_PILL_CLASS[item.status.bucket],
              )}
            >
              <StatusDot bucket={item.status.bucket} />
              {item.status.label}
            </span>
          ) : null}
        </span>
        <span
          data-f="cdu"
          className={cn(
            "flex flex-col text-left leading-[1.25] whitespace-nowrap tabular-nums",
            toneClass,
          )}
        >
          <span className="sr-only">
            {completed ? copy.list.doneLabel : copy.list.dueLabel}
          </span>
          <b
            className={cn(
              "text-sm leading-[1.25]",
              !completed && words.tone !== "normal"
                ? "font-[650]"
                : "font-medium",
            )}
          >
            {completed && completed.kind === "today" && completed.time
              ? completed.time
              : words.text}
          </b>
          {sub ? (
            <small className="text-item-note leading-[1.25] font-semibold">
              {sub}
            </small>
          ) : null}
        </span>
        <RowMenu {...rowProps} />
        {approve ? (
          <div
            data-f="apr"
            className="col-[2/-1] flex flex-wrap items-center gap-2 py-1 pr-3 pl-0.5"
          >
            <span className="min-w-0 flex-1 text-item-ui leading-[1.45] text-muted-foreground">
              <b className="font-semibold text-foreground">
                {approve.actorName}
              </b>{" "}
              {formatCopy(copy.list.approvalSent, { time: approve.time })}
            </span>
            {approvalButtons}
          </div>
        ) : null}
      </div>
    </li>
  );
}
