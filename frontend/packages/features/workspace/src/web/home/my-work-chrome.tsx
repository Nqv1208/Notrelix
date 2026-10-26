import { useEffect, useRef, useState, type ReactNode } from "react";
import {
  AlignLeft,
  Calendar,
  Check,
  ChevronDown,
  CircleCheck,
  Filter,
  Plus,
  Search,
  SquareKanban,
} from "lucide-react";
import { cn } from "@notrelix/ui-web";
import type { MyWorkDepartment, WorkPriority } from "./home-models";
import {
  MY_WORK_TABS,
  type MyWorkGroupBy,
  type MyWorkTab,
} from "./my-work-model";
import { DepartmentMark } from "./work-row-parts";
import type { WorkspaceHomeCopy } from "./workspace-home.copy";

/**
 * The page chrome of My work (Final-MyWork): the title row, the five tabs,
 * and the bar with the group, the filters, the search and "Thêm việc", with
 * the three popovers that hang off them.
 */

export const OUTLINE_BUTTON_CLASS =
  "box-border inline-flex h-8 shrink-0 items-center gap-1.5 rounded-[8px] border border-line-strong bg-popover px-3 text-compact leading-[1.45] font-medium whitespace-nowrap text-foreground no-underline outline-none hover:bg-state-hover focus-visible:ring-2 focus-visible:ring-focus-ring aria-disabled:cursor-not-allowed dark:bg-card";
export const PRIMARY_BUTTON_CLASS =
  "box-border inline-flex h-8 shrink-0 items-center gap-1.5 rounded-[8px] border border-primary bg-primary px-3 text-compact leading-[1.45] font-semibold whitespace-nowrap text-primary-foreground outline-none hover:bg-primary/90 focus-visible:ring-2 focus-visible:ring-focus-ring aria-disabled:cursor-not-allowed";
export const SMALL_OUTLINE_CLASS =
  "box-border inline-flex h-7 shrink-0 items-center gap-1.5 rounded-[8px] border border-line-strong bg-popover px-2.5 text-crumb leading-[1.45] font-medium whitespace-nowrap text-foreground no-underline outline-none hover:bg-state-hover focus-visible:ring-2 focus-visible:ring-focus-ring aria-disabled:cursor-not-allowed dark:bg-card";
export const SMALL_PRIMARY_BUTTON_CLASS =
  "box-border inline-flex h-7 shrink-0 items-center gap-1.5 rounded-[8px] border border-primary bg-primary px-2.5 text-crumb leading-[1.45] font-semibold whitespace-nowrap text-primary-foreground outline-none hover:bg-primary/90 focus-visible:ring-2 focus-visible:ring-focus-ring aria-disabled:cursor-not-allowed";

const PILL_CLASS =
  "inline-flex h-8 shrink-0 cursor-pointer items-center gap-1.5 rounded-[7px] border-0 bg-transparent px-2.5 text-compact leading-[1.45] whitespace-nowrap text-muted-foreground outline-none hover:bg-state-hover-strong hover:text-foreground aria-expanded:bg-state-hover-strong aria-expanded:text-foreground focus-visible:ring-2 focus-visible:ring-focus-ring aria-disabled:cursor-not-allowed aria-disabled:hover:bg-transparent aria-disabled:hover:text-muted-foreground";

const POP_CLASS =
  "absolute z-dropdown box-content rounded-[10px] border border-line-strong bg-popover p-1.5 whitespace-normal text-foreground shadow-popover dark:bg-card";

/** A popover that closes on Escape and on a press outside it and its anchor. */
function usePopoverDismiss(open: boolean, onClose: () => void) {
  const ref = useRef<HTMLDivElement>(null);
  useEffect(() => {
    if (!open) return;
    const onKey = (event: KeyboardEvent) => {
      if (event.key === "Escape") onClose();
    };
    const onPointer = (event: PointerEvent) => {
      if (ref.current && !ref.current.contains(event.target as Node)) onClose();
    };
    window.addEventListener("keydown", onKey);
    window.addEventListener("pointerdown", onPointer);
    return () => {
      window.removeEventListener("keydown", onKey);
      window.removeEventListener("pointerdown", onPointer);
    };
  }, [open, onClose]);
  return ref;
}

/** The anchor of a popover: relative, closes the popover on dismissal. */
function Anchor({
  open,
  onClose,
  children,
}: {
  open: boolean;
  onClose: () => void;
  children: ReactNode;
}) {
  const ref = usePopoverDismiss(open, onClose);
  return (
    <div ref={ref} className="relative inline-flex">
      {children}
    </div>
  );
}

export function MyWorkHeader({
  copy,
  subline,
  calendarHref,
}: {
  copy: WorkspaceHomeCopy;
  subline: string;
  calendarHref: string | null;
}) {
  const calendar = (
    <>
      <Calendar aria-hidden="true" className="size-4" strokeWidth={1.8} />
      {copy.list.calendar}
    </>
  );
  return (
    <div data-f="head" className="flex items-end gap-3 px-7 pt-[22px]">
      <div className="min-w-0 flex-1">
        <h1 data-f="h1" className="m-0 text-list-title text-foreground">
          {copy.list.title}
        </h1>
        <p
          data-f="sub"
          className="mt-1 mb-0 text-nav leading-[1.45] font-normal text-muted-foreground"
        >
          {subline}
        </p>
      </div>
      {calendarHref ? (
        <a data-f="hbtn" href={calendarHref} className={OUTLINE_BUTTON_CLASS}>
          {calendar}
        </a>
      ) : (
        <button
          data-f="hbtn"
          type="button"
          aria-disabled="true"
          title={copy.list.calendarUnavailable}
          className={OUTLINE_BUTTON_CLASS}
        >
          {calendar}
        </button>
      )}
    </div>
  );
}

export function MyWorkTabs({
  copy,
  tab,
  counts,
  available,
  onTab,
  showCounts,
  narrow,
}: {
  copy: WorkspaceHomeCopy;
  tab: MyWorkTab;
  counts: Record<Exclude<MyWorkTab, "done">, number>;
  /** Tabs whose data the host has; the others are drawn inert with the reason. */
  available: Record<MyWorkTab, boolean>;
  onTab: (tab: MyWorkTab) => void;
  showCounts: boolean;
  narrow: boolean;
}) {
  return (
    <div
      data-f="tabs"
      role="tablist"
      aria-label={copy.list.tabsLabel}
      className="flex flex-wrap items-end gap-0.5 border-b border-border px-7 pt-3"
    >
      {MY_WORK_TABS.map((id) => {
        const on = tab === id;
        const usable = available[id];
        const count = id === "done" ? null : counts[id];
        return (
          <button
            key={id}
            data-f="tab"
            type="button"
            role="tab"
            id={`my-work-tab-${id}`}
            aria-selected={on}
            aria-controls="mw-panel"
            aria-disabled={usable ? undefined : true}
            title={
              usable || id === "all" || id === "mine"
                ? undefined
                : copy.list.tabUnavailable[id]
            }
            tabIndex={on ? 0 : -1}
            onClick={usable ? () => onTab(id) : undefined}
            className={cn(
              "-mb-px inline-flex h-9 items-center gap-[7px] border-0 border-b-2 border-transparent bg-transparent text-nav leading-[1.45] whitespace-nowrap outline-none focus-visible:ring-2 focus-visible:ring-focus-ring",
              narrow ? "px-2" : "px-3",
              on
                ? "border-primary font-semibold text-foreground"
                : "text-muted-foreground",
              usable
                ? "cursor-pointer hover:text-foreground"
                : "cursor-not-allowed",
            )}
          >
            {copy.list.tabs[id]}
            {showCounts && usable && count !== null ? (
              <span
                data-f="tabn"
                className={cn(
                  "box-border min-w-[18px] rounded-full px-1.5 text-center text-xs leading-[18px] font-semibold tabular-nums",
                  on
                    ? "bg-state-selected text-state-selected-foreground"
                    : "bg-state-hover-strong text-muted-foreground",
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

export type MyWorkMenu =
  "group" | "department" | "board" | "priority" | "create" | null;

export interface MyWorkBoardOption {
  readonly id: string;
  readonly name: string;
  readonly department: MyWorkDepartment | null;
  readonly detail: string | null;
}

export function MyWorkBar({
  copy,
  hasItems,
  groupBy,
  menu,
  onMenu,
  departments,
  selectedDepartments,
  onToggleDepartment,
  boards,
  selectedBoards,
  onToggleBoard,
  selectedPriorities,
  onTogglePriority,
  query,
  onQuery,
  createBoards,
  createBoardId,
  onCreateBoard,
  createName,
  onCreateName,
  onCreate,
  groupAvailable,
  onGroup,
}: {
  copy: WorkspaceHomeCopy;
  hasItems: boolean;
  groupBy: MyWorkGroupBy;
  onGroup: (id: MyWorkGroupBy) => void;
  menu: MyWorkMenu;
  onMenu: (menu: MyWorkMenu) => void;
  departments: readonly MyWorkDepartment[];
  selectedDepartments: readonly string[];
  onToggleDepartment: (name: string) => void;
  boards: readonly { id: string; name: string }[];
  selectedBoards: readonly string[];
  onToggleBoard: (id: string) => void;
  selectedPriorities: readonly WorkPriority[];
  /** `undefined` draws the Priority filter inert. */
  onTogglePriority?: (priority: WorkPriority) => void;
  query: string;
  onQuery: (value: string) => void;
  createBoards: readonly MyWorkBoardOption[];
  createBoardId: string | null;
  onCreateBoard: (id: string) => void;
  createName: string;
  onCreateName: (value: string) => void;
  /** `null`: no name or no Board chosen yet, so nothing can be created. */
  onCreate: (() => void) | null;
  groupAvailable: Record<MyWorkGroupBy, boolean>;
}) {
  const close = () => onMenu(null);
  const groupLabel = copy.list.groupBy[groupBy];
  const [moreBoards, setMoreBoards] = useState(false);
  const shownBoards = moreBoards ? createBoards : createBoards.slice(0, 3);
  return (
    <div
      data-f="bar"
      className="flex flex-wrap items-center gap-1.5 px-7 py-2.5"
    >
      {hasItems ? (
        <>
          <Anchor open={menu === "group"} onClose={close}>
            <button
              data-f="pill"
              type="button"
              aria-haspopup="menu"
              aria-expanded={menu === "group"}
              onClick={() => onMenu(menu === "group" ? null : "group")}
              className={PILL_CLASS}
            >
              <AlignLeft
                aria-hidden="true"
                className="size-4"
                strokeWidth={1.8}
              />
              {copy.list.groupLabel}{" "}
              <b className="font-semibold text-foreground">{groupLabel}</b>
              <ChevronDown
                aria-hidden="true"
                className="size-[13px]"
                strokeWidth={1.8}
              />
            </button>
            {menu === "group" ? (
              <div
                data-f="pop"
                role="menu"
                aria-label={copy.list.groupMenuLabel}
                className={cn(POP_CLASS, "top-[38px] left-0 w-[236px]")}
              >
                <p
                  aria-hidden="true"
                  className="mx-2 mt-1 mb-1.5 text-xs leading-[1.45] font-semibold text-meta-foreground"
                >
                  {copy.list.groupMenuLabel}
                </p>
                {(["due", "board", "status"] as const).map((id) => {
                  const usable = groupAvailable[id];
                  return (
                    <button
                      key={id}
                      data-f="mi"
                      type="button"
                      role="menuitemradio"
                      aria-checked={groupBy === id}
                      aria-disabled={usable ? undefined : true}
                      title={usable ? undefined : copy.list.groupUnavailable}
                      onClick={
                        usable
                          ? () => {
                              onGroup(id);
                              onMenu(null);
                            }
                          : undefined
                      }
                      className={cn(
                        "flex h-[34px] w-full items-center gap-[9px] rounded-[7px] border-0 bg-transparent px-2 text-left text-compact leading-[1.45] text-foreground outline-none hover:bg-state-hover-strong focus-visible:ring-2 focus-visible:ring-focus-ring",
                        groupBy === id && "bg-state-hover-strong",
                        !usable && "cursor-not-allowed text-muted-foreground",
                      )}
                    >
                      <GroupIcon id={id} />
                      {copy.list.groupBy[id]}
                      {groupBy === id ? (
                        <Check
                          aria-hidden="true"
                          className="ml-auto size-4 text-state-selected-foreground"
                          strokeWidth={1.8}
                        />
                      ) : null}
                    </button>
                  );
                })}
              </div>
            ) : null}
          </Anchor>
          <span
            aria-hidden="true"
            className="mx-1 h-[18px] w-px bg-line-strong"
          />
          <FilterPill
            copy={copy}
            label={copy.list.filters.department}
            icon={
              <Filter aria-hidden="true" className="size-4" strokeWidth={1.8} />
            }
            open={menu === "department"}
            onOpen={() => onMenu(menu === "department" ? null : "department")}
            onClose={close}
            disabledReason={
              departments.length === 0
                ? copy.list.filters.priorityUnavailable
                : null
            }
            active={selectedDepartments.length}
          >
            {departments.map((department) => (
              <FilterOption
                key={department.name}
                label={department.name}
                selected={selectedDepartments.includes(department.name)}
                onToggle={() => onToggleDepartment(department.name)}
                leading={
                  <DepartmentMark
                    small
                    name={department.name}
                    markTone={department.markTone}
                  />
                }
              />
            ))}
          </FilterPill>
          <FilterPill
            copy={copy}
            label={copy.list.filters.board}
            open={menu === "board"}
            onOpen={() => onMenu(menu === "board" ? null : "board")}
            onClose={close}
            disabledReason={
              boards.length === 0 ? copy.list.filters.priorityUnavailable : null
            }
            active={selectedBoards.length}
          >
            {boards.map((board) => (
              <FilterOption
                key={board.id}
                label={board.name}
                selected={selectedBoards.includes(board.id)}
                onToggle={() => onToggleBoard(board.id)}
              />
            ))}
          </FilterPill>
          <FilterPill
            copy={copy}
            label={copy.list.filters.priority}
            open={menu === "priority"}
            onOpen={() => onMenu(menu === "priority" ? null : "priority")}
            onClose={close}
            disabledReason={
              onTogglePriority ? null : copy.list.filters.priorityUnavailable
            }
            active={selectedPriorities.length}
          >
            {(["urgent", "high", "medium", "low"] as const).map((priority) => (
              <FilterOption
                key={priority}
                label={copy.work.priority[priority]}
                selected={selectedPriorities.includes(priority)}
                onToggle={() => onTogglePriority?.(priority)}
              />
            ))}
          </FilterPill>
        </>
      ) : null}
      <span className="flex-1" />
      <label
        data-f="field"
        className="box-border flex h-8 w-[220px] max-w-full items-center gap-2 rounded-[7px] border border-line-strong bg-popover px-2.5 text-meta-foreground focus-within:border-primary focus-within:shadow-[0_0_0_2px_var(--color-state-selected)] dark:bg-card"
      >
        <Search
          aria-hidden="true"
          className="size-4 shrink-0"
          strokeWidth={1.8}
        />
        <input
          type="search"
          value={query}
          onChange={(event) => onQuery(event.target.value)}
          placeholder={copy.list.searchPlaceholder}
          aria-label={copy.list.searchLabel}
          className="w-full min-w-0 border-0 bg-transparent px-0.5 py-px text-compact leading-[1.45] text-foreground outline-none placeholder:text-meta-foreground"
        />
      </label>
      <Anchor open={menu === "create"} onClose={close}>
        <button
          data-f="add"
          type="button"
          aria-haspopup="dialog"
          aria-expanded={menu === "create"}
          onClick={() => onMenu(menu === "create" ? null : "create")}
          className={PRIMARY_BUTTON_CLASS}
        >
          <Plus aria-hidden="true" className="size-4" strokeWidth={1.8} />
          {copy.list.add}
        </button>
        {menu === "create" ? (
          <div
            data-f="createpop"
            role="dialog"
            aria-modal="false"
            aria-labelledby="my-work-create-title"
            className={cn(
              POP_CLASS,
              "top-10 right-0 flex w-[340px] flex-col gap-3 p-3.5",
            )}
          >
            <h2 id="my-work-create-title" className="m-0 text-item-heading">
              {copy.list.addTitle}
            </h2>
            <label className="flex flex-col gap-1.5 text-crumb leading-[1.45] font-semibold text-muted-foreground">
              {copy.list.addName}
              <input
                type="text"
                value={createName}
                onChange={(event) => onCreateName(event.target.value)}
                placeholder={copy.list.addNamePlaceholder}
                className="box-content h-[34px] rounded-[8px] border border-primary bg-popover px-2.5 text-nav leading-[1.45] font-normal text-foreground shadow-[0_0_0_2px_var(--color-state-selected)] outline-none placeholder:text-meta-foreground dark:bg-card"
              />
            </label>
            <div className="flex flex-col gap-1.5 text-crumb leading-[1.45] font-semibold text-muted-foreground">
              <span id="my-work-create-board">{copy.list.addBoard}</span>
              <div
                role="radiogroup"
                aria-labelledby="my-work-create-board"
                className="flex flex-col gap-0.5 rounded-[9px] border border-border p-1"
              >
                {shownBoards.map((board) => {
                  const on = createBoardId === board.id;
                  return (
                    <div
                      key={board.id}
                      role="radio"
                      aria-checked={on}
                      tabIndex={on ? 0 : -1}
                      onClick={() => onCreateBoard(board.id)}
                      onKeyDown={(event) => {
                        if (event.key === " " || event.key === "Enter") {
                          event.preventDefault();
                          onCreateBoard(board.id);
                        }
                      }}
                      className={cn(
                        "box-content flex min-h-[38px] cursor-pointer items-center gap-[9px] rounded-[7px] px-2 py-1 text-compact leading-[1.45] font-medium text-foreground outline-none hover:bg-state-hover focus-visible:ring-2 focus-visible:ring-focus-ring",
                        on && "bg-state-selected",
                      )}
                    >
                      {board.department ? (
                        <DepartmentMark
                          small
                          name={board.department.name}
                          markTone={board.department.markTone}
                        />
                      ) : (
                        <span
                          aria-hidden="true"
                          className="grid size-4 place-items-center text-meta-foreground"
                        >
                          <SquareKanban className="size-4" strokeWidth={1.8} />
                        </span>
                      )}
                      <span>
                        {board.name}
                        {board.detail ? (
                          <small
                            className={cn(
                              "block text-micro leading-[1.45] font-normal",
                              on
                                ? "text-state-selected-foreground"
                                : "text-meta-foreground",
                            )}
                          >
                            {board.detail}
                          </small>
                        ) : null}
                      </span>
                      <span
                        aria-hidden="true"
                        className={cn(
                          "ml-auto box-border size-4 shrink-0 rounded-full border-[1.5px] border-work-status-todo",
                          on && "border-[5px] border-primary",
                        )}
                      />
                    </div>
                  );
                })}
                {createBoards.length > shownBoards.length ? (
                  <button
                    type="button"
                    onClick={() => setMoreBoards(true)}
                    className="flex min-h-[38px] w-full cursor-pointer items-center rounded-[7px] border-0 bg-transparent px-2 py-1 text-left text-compact leading-[1.45] font-semibold text-state-selected-foreground outline-none hover:bg-state-hover focus-visible:ring-2 focus-visible:ring-focus-ring"
                  >
                    {copy.list.addBoardOther}
                  </button>
                ) : null}
              </div>
            </div>
            <p className="m-0 text-crumb leading-[1.5] font-normal text-meta-foreground">
              {copy.list.addNote}
            </p>
            <div className="flex justify-end gap-1.5">
              <button
                type="button"
                onClick={close}
                className={SMALL_OUTLINE_CLASS}
              >
                {copy.list.addCancel}
              </button>
              <button
                type="button"
                aria-disabled={onCreate ? undefined : true}
                title={onCreate ? undefined : copy.list.addUnavailable}
                onClick={onCreate ?? undefined}
                className={SMALL_PRIMARY_BUTTON_CLASS}
              >
                {copy.list.addSubmit}
                <span className="rounded-[4px] border border-current bg-transparent px-[5px] font-mono text-key leading-[18px] text-inherit">
                  ↵
                </span>
              </button>
            </div>
          </div>
        ) : null}
      </Anchor>
    </div>
  );
}

function GroupIcon({ id }: { id: MyWorkGroupBy }) {
  const Icon =
    id === "due" ? Calendar : id === "board" ? SquareKanban : CircleCheck;
  return (
    <Icon
      aria-hidden="true"
      className="size-4 text-meta-foreground"
      strokeWidth={1.8}
    />
  );
}

function FilterPill({
  copy,
  label,
  icon,
  open,
  onOpen,
  onClose,
  disabledReason,
  active,
  children,
}: {
  copy: WorkspaceHomeCopy;
  label: string;
  icon?: ReactNode;
  open: boolean;
  onOpen: () => void;
  onClose: () => void;
  disabledReason: string | null;
  active: number;
  children: ReactNode;
}) {
  return (
    <Anchor open={open} onClose={onClose}>
      <button
        data-f="pill"
        type="button"
        aria-haspopup="listbox"
        aria-expanded={open}
        aria-disabled={disabledReason ? true : undefined}
        title={disabledReason ?? undefined}
        onClick={disabledReason ? undefined : onOpen}
        className={PILL_CLASS}
      >
        {icon}
        {label}
        {active > 0 ? (
          <b className="font-semibold text-foreground">{active}</b>
        ) : null}
        <ChevronDown
          aria-hidden="true"
          className="size-[13px]"
          strokeWidth={1.8}
        />
      </button>
      {open ? (
        <div
          data-f="pop"
          role="listbox"
          aria-label={label}
          aria-multiselectable="true"
          className={cn(POP_CLASS, "top-[38px] left-0 w-[236px]")}
        >
          {children}
          <p className="sr-only">{copy.list.filters.all}</p>
        </div>
      ) : null}
    </Anchor>
  );
}

function FilterOption({
  label,
  selected,
  onToggle,
  leading,
}: {
  label: string;
  selected: boolean;
  onToggle: () => void;
  leading?: ReactNode;
}) {
  return (
    <div
      role="option"
      aria-selected={selected}
      tabIndex={0}
      onClick={onToggle}
      onKeyDown={(event) => {
        if (event.key === " " || event.key === "Enter") {
          event.preventDefault();
          onToggle();
        }
      }}
      className="flex h-8 cursor-pointer items-center gap-2 rounded-[7px] px-2 text-compact leading-[1.45] text-foreground outline-none hover:bg-state-hover focus-visible:ring-2 focus-visible:ring-focus-ring"
    >
      {leading}
      <span className="truncate">{label}</span>
      {selected ? (
        <Check
          aria-hidden="true"
          className="ml-auto size-3.5 text-state-selected-foreground"
          strokeWidth={1.8}
        />
      ) : null}
    </div>
  );
}
