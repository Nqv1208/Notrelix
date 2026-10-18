import {
  useCallback,
  useEffect,
  type KeyboardEvent as ReactKeyboardEvent,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from "react";
import { Check, ChevronDown } from "lucide-react";
import { Skeleton, cn } from "@notrelix/ui-web";
import { describeMyWorkDue } from "../../core/model/my-work";
import type { MyWorkWindow } from "../../core/model/my-work";
import { ShellLink } from "../shell/shell-link";
import { formatCopy, type ShellNavigation } from "../shell/shell-models";
import type {
  MyWorkDepartment,
  MyWorkItemView,
  SectionState,
  SurfaceFormatters,
  WorkPriority,
  WorkStatusBucket,
} from "./home-models";
import {
  MyWorkBar,
  MyWorkHeader,
  MyWorkTabs,
  SMALL_OUTLINE_CLASS,
  type MyWorkBoardOption,
  type MyWorkMenu,
} from "./my-work-chrome";
import {
  applyFilters,
  departmentCounts,
  groupByBoard,
  groupByDone,
  groupByDue,
  groupByStatus,
  NO_MY_WORK_FILTERS,
  overdueSummary,
  tabCounts,
  weekDays,
  weekRange,
  weekWindowEnd,
  type MyWorkFilters,
  type MyWorkGroupBy,
  type MyWorkTab,
} from "./my-work-model";
import { MyWorkRow, type MyWorkColumns } from "./my-work-row";
import {
  MyWorkBulkBar,
  MyWorkEmpty,
  MyWorkFoot,
  MyWorkNote,
  MyWorkRail,
} from "./my-work-side";
import type {
  MyWorkActions,
  MyWorkCreateBoard,
  MyWorkWaitingView,
} from "./my-work-types";
import { DepartmentMark, StatusDot } from "./work-row-parts";
import type { WorkspaceHomeCopy } from "./workspace-home.copy";

/** The view state a story or test starts from; the page owns it afterwards. */
export interface MyWorkInitialView {
  readonly tab?: MyWorkTab;
  readonly groupBy?: MyWorkGroupBy;
  readonly selectedIds?: readonly string[];
  readonly menu?: MyWorkMenu;
  /** The row whose Status popover is open. */
  readonly statusPopFor?: string | null;
  /** Show why "Stop following" is unavailable (the tooltip above it). */
  readonly unfollowTip?: boolean;
}

export interface WorkspaceMyWorkSurfaceProps {
  readonly copy: WorkspaceHomeCopy;
  readonly navigation: ShellNavigation;
  readonly formatters: SurfaceFormatters;
  /** Reference time for due words and the week (epoch ms). */
  readonly now: number;
  /** Every open item assigned to the member that has been loaded. */
  readonly state: SectionState<readonly MyWorkItemView[]>;
  /** When the list was last read (epoch ms); `null` while loading. */
  readonly updatedAt: number | null;
  /** More pages exist on the producer (cursor paging). */
  readonly hasMore: boolean;
  readonly loadingMore: boolean;
  readonly onLoadMore: () => void;
  readonly onRetry: () => void;
  readonly completingIds: ReadonlySet<string>;
  /** `null` draws every tick inert. */
  readonly onComplete: ((item: MyWorkItemView) => void) | null;
  /** The Done tab's items; `undefined` = no producer: the tab is drawn inert. */
  readonly done?: SectionState<readonly MyWorkItemView[]>;
  /** Tabs other than All and Assigned to me that have data behind them. */
  readonly tabsAvailable?: Partial<Record<"approve" | "follow", boolean>>;
  /** Who the member waits on; `undefined` = no producer: the card is not drawn. */
  readonly waiting?: readonly MyWorkWaitingView[];
  /** Where "Lịch của tôi" goes; `null` draws it inert. */
  readonly calendarHref?: string | null;
  /** Boards the member can add an item to. */
  readonly createBoards: readonly MyWorkCreateBoard[];
  /** Creates an item already assigned to the member; `null` = no write yet. */
  readonly onCreate:
    ((input: { boardId: string; name: string }) => void) | null;
  /** The row open in the sheet. */
  readonly openItemId: string | null;
  /** A plain click on a title: open the item (the host shows its sheet). */
  readonly onOpenItem: (item: MyWorkItemView) => void;
  /** The item sheet, already placed over the right edge. */
  readonly sheet?: ReactNode;
  /** The department offered when nothing is assigned. */
  readonly firstDepartment?: MyWorkDepartment | null;
  /** Status choices; `undefined` = none: the Status pill is inert. */
  readonly statusOptions?: readonly {
    readonly bucket: WorkStatusBucket;
    readonly label: string;
  }[];
  readonly onChangeStatus?:
    ((item: MyWorkItemView, bucket: WorkStatusBucket) => void) | null;
  /** Approval, reopen, remind and the bulk writes; omitted draws them inert. */
  readonly actions?: MyWorkActions;
  readonly initialView?: MyWorkInitialView;
}

function clockTime(time: number): string {
  const date = new Date(time);
  return `${String(date.getHours()).padStart(2, "0")}:${String(
    date.getMinutes(),
  ).padStart(2, "0")}`;
}

const WEEKDAY_KEYS = ["sun", "mon", "tue", "wed", "thu", "fri", "sat"] as const;

function RowsSkeleton({ rows = 4 }: { rows?: number }) {
  return (
    <section aria-busy="true" aria-hidden="true">
      <Skeleton className="mb-2 h-5 w-24 rounded-md" />
      <ul className="m-0 list-none rounded-[12px] border border-border bg-popover p-0 dark:bg-card">
        {Array.from({ length: rows }, (_, index) => (
          <li
            key={index}
            data-f="row"
            className="box-border grid min-h-[60px] grid-cols-[18px_22px_minmax(0,1fr)_112px_112px] items-center gap-x-2.5 border-t border-border px-3.5 py-[9px] first:border-t-0"
          >
            <span />
            <Skeleton className="size-5 rounded-full" />
            <div>
              <Skeleton className="h-3 w-3/5 rounded-md" />
              <Skeleton className="mt-2 h-2.5 w-2/5 rounded-md" />
            </div>
            <Skeleton className="h-[26px] w-24 rounded-full" />
            <Skeleton className="h-3 w-16 rounded-md" />
          </li>
        ))}
      </ul>
    </section>
  );
}

/**
 * My work (Final-MyWork): the member's open items by due window or by Board
 * with the tabs, the filters, the search, selection and a bulk bar, and a
 * right rail with the week by due day and the items by department. The page
 * owns only its view state (tab, grouping, filters, selection, popovers);
 * what each control does comes from the host: a control whose producer does
 * not exist is drawn like the design but inert, with the reason.
 */
export function WorkspaceMyWorkSurface(props: WorkspaceMyWorkSurfaceProps) {
  const { copy, navigation, now, state } = props;
  const initial = props.initialView;

  const [tab, setTab] = useState<MyWorkTab>(initial?.tab ?? "all");
  const [groupBy, setGroupBy] = useState<MyWorkGroupBy>(
    initial?.groupBy ?? "due",
  );
  const [filters, setFilters] = useState<MyWorkFilters>(NO_MY_WORK_FILTERS);
  const [selected, setSelected] = useState<ReadonlySet<string>>(
    () => new Set(initial?.selectedIds ?? []),
  );
  const [menu, setMenu] = useState<MyWorkMenu>(initial?.menu ?? null);
  const [collapsed, setCollapsed] = useState<ReadonlySet<string>>(
    () => new Set(),
  );
  const [statusPopFor, setStatusPopFor] = useState<string | null>(
    initial?.statusPopFor ?? null,
  );
  const [createName, setCreateName] = useState("");
  const [chosenBoardId, setCreateBoardId] = useState<string | null>(null);
  // The first Board until the member picks one (the list may load later).
  const createBoardId =
    props.createBoards.find((board) => board.id === chosenBoardId)?.id ??
    props.createBoards[0]?.id ??
    null;
  const panelRef = useRef<HTMLDivElement>(null);

  const allItems = useMemo<readonly MyWorkItemView[]>(
    () => (state.status === "ready" ? state.data : []),
    [state],
  );
  const hasItems = allItems.length > 0;
  const doneState = props.done;
  const doneItems =
    doneState?.status === "ready" ? doneState.data : ([] as const);
  const doneTab = tab === "done" && doneState !== undefined;

  const counts = useMemo(() => tabCounts(allItems), [allItems]);
  const visible = useMemo(
    () =>
      doneTab
        ? applyFilters(doneItems, "all", filters, now)
        : applyFilters(allItems, tab, filters, now),
    [allItems, doneItems, doneTab, filters, now, tab],
  );

  const columns = useMemo<MyWorkColumns>(
    () => ({
      status: visible.some((item) => item.status),
      priority: visible.some((item) => item.priority),
      assignee: visible.some((item) => item.assignee),
    }),
    [visible],
  );
  const narrow = props.sheet !== undefined && props.openItemId !== null;

  const departments = useMemo(() => departmentCounts(allItems), [allItems]);
  const departmentList = useMemo(
    () => departments.map((entry) => entry.department),
    [departments],
  );
  const boardOptions = useMemo(() => {
    const seen = new Map<string, string>();
    for (const item of allItems) {
      if (!seen.has(item.boardId) && item.boardName)
        seen.set(item.boardId, item.boardName);
    }
    return [...seen.entries()].map(([id, name]) => ({ id, name }));
  }, [allItems]);

  const groupAvailable = {
    due: true,
    board: true,
    status: allItems.some((item) => item.status),
  } as const;
  const tabAvailable: Record<MyWorkTab, boolean> = {
    all: true,
    mine: true,
    approve: props.tabsAvailable?.approve ?? false,
    follow: props.tabsAvailable?.follow ?? false,
    done: doneState !== undefined,
  };

  // ── Selection and keys ──────────────────────────────────────────
  const toggleSelect = useCallback((id: string) => {
    setSelected((previous) => {
      const next = new Set(previous);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  }, []);
  const clearSelection = useCallback(() => setSelected(new Set()), []);
  const selectedItems = visible.filter((item) => selected.has(item.id));

  const completeItem = props.onComplete;
  const canComplete = completeItem !== null;
  const completeSelected = () => {
    if (!completeItem) return;
    for (const item of selectedItems) {
      if (!props.completingIds.has(item.id)) completeItem(item);
    }
    clearSelection();
  };

  useEffect(() => {
    const links = () =>
      Array.from(
        panelRef.current?.querySelectorAll<HTMLAnchorElement>(
          '[data-f="rt"] a',
        ) ?? [],
      );
    const onKey = (event: KeyboardEvent) => {
      if (event.metaKey || event.ctrlKey || event.altKey) return;
      const target = event.target as HTMLElement | null;
      if (
        target &&
        (target.isContentEditable ||
          ["INPUT", "TEXTAREA", "SELECT"].includes(target.tagName))
      )
        return;
      const key = event.key.toLowerCase();
      if (key === "escape" && selected.size > 0) {
        clearSelection();
        return;
      }
      const list = links();
      const current = list.findIndex((link) => link === document.activeElement);
      if (key === "j" || key === "k") {
        event.preventDefault();
        const next =
          current === -1
            ? 0
            : Math.min(
                list.length - 1,
                Math.max(0, current + (key === "j" ? 1 : -1)),
              );
        list[next]?.focus();
        return;
      }
      const row = (list[current] ?? null)?.closest<HTMLElement>(
        '[data-f="row"]',
      );
      const id = row?.dataset.itemId;
      if (!id) return;
      if (key === "x") {
        event.preventDefault();
        toggleSelect(id);
      } else if (key === "e" && completeItem) {
        event.preventDefault();
        const item = visible.find((entry) => entry.id === id);
        if (item && !props.completingIds.has(item.id)) completeItem(item);
      }
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [
    clearSelection,
    completeItem,
    props.completingIds,
    selected.size,
    toggleSelect,
    visible,
  ]);

  // ── Header texts ────────────────────────────────────────────────
  const departmentTotal = departments.length;
  const subline =
    state.status === "loading"
      ? copy.list.sublineLoading
      : !hasItems
        ? formatCopy(copy.list.sublineEmpty, {
            time: props.updatedAt ? clockTime(props.updatedAt) : "",
          })
        : departmentTotal > 0
          ? formatCopy(
              departmentTotal === 1
                ? copy.list.sublineDepartments_one
                : copy.list.sublineDepartments_other,
              {
                count: `${allItems.length}${props.hasMore ? "+" : ""}`,
                departments: departmentTotal,
                time: props.updatedAt ? clockTime(props.updatedAt) : "",
              },
            )
          : formatCopy(copy.list.subline, {
              count: `${allItems.length}${props.hasMore ? "+" : ""}`,
              time: props.updatedAt ? clockTime(props.updatedAt) : "",
            });

  const week = useMemo(() => weekDays(allItems, now), [allItems, now]);
  const overdue = useMemo(() => overdueSummary(allItems, now), [allItems, now]);
  const windowEnd = weekWindowEnd(now);
  const todayDue = describeMyWorkDue(new Date(now).toISOString(), now);

  const subFor = (id: MyWorkWindow): string | null => {
    if (id === "today")
      return formatCopy(copy.list.windowSub.today, {
        weekday: copy.work.weekdays[WEEKDAY_KEYS[todayDue.weekday] ?? "sun"],
        date: todayDue.shortDate,
      });
    if (id === "this-week")
      return formatCopy(copy.list.windowSub["this-week"], {
        weekday: copy.work.weekdays[WEEKDAY_KEYS[windowEnd.weekday] ?? "sun"],
        date: windowEnd.shortDate,
      });
    if (id === "later") return copy.list.windowSub.later;
    return null;
  };

  // ── Groups ──────────────────────────────────────────────────────
  interface Group {
    id: string;
    name: string;
    count: number;
    sub: string | null;
    tone: "over" | "plain";
    board: {
      name: string;
      department: MyWorkDepartment | null;
      boardId: string;
    } | null;
    items: readonly MyWorkItemView[];
  }
  const groups: Group[] = doneTab
    ? groupByDone(visible, now).map((group) => ({
        id: `d-${group.id}`,
        name: copy.list.doneWindows[group.id],
        count: group.items.length,
        sub: null,
        tone: "plain",
        board: null,
        items: group.items,
      }))
    : groupBy === "status"
      ? groupByStatus(visible).map((group) => ({
          id: `s-${group.id}`,
          name: group.label,
          count: group.items.length,
          sub: null,
          tone: "plain" as const,
          board: null,
          items: group.items,
        }))
      : groupBy === "board"
        ? groupByBoard(visible).map((group) => ({
            id: group.id,
            name: group.name ?? "",
            count: group.items.length,
            sub: group.department
              ? formatCopy(copy.list.departmentOf, {
                  name: group.department.name,
                })
              : null,
            tone: "plain",
            board: group.name
              ? {
                  name: group.name,
                  department: group.department,
                  boardId: group.boardId,
                }
              : null,
            items: group.items,
          }))
        : groupByDue(visible, now).map((group) => ({
            id: group.id,
            name: copy.list.windows[group.id],
            count: group.items.length,
            sub: subFor(group.id),
            tone: group.id === "overdue" ? "over" : "plain",
            board: null,
            items: group.items,
          }));

  const statusPopItem = statusPopFor
    ? (visible.find((item) => item.id === statusPopFor) ?? null)
    : null;

  const filtersActive =
    filters.departments.length > 0 ||
    filters.boards.length > 0 ||
    filters.priorities.length > 0 ||
    filters.query.trim() !== "" ||
    filters.dueOn !== null;

  const selecting = selected.size > 0;
  const createBoards: MyWorkBoardOption[] = props.createBoards.map((board) => ({
    id: board.id,
    name: board.name,
    department:
      departmentList.find((d) => d.name === board.departmentName) ?? null,
    detail: board.detail,
  }));
  const panelLabel = doneTab
    ? copy.list.tabs.done
    : `${copy.list.tabs[tab]}, ${copy.list.groupBy[groupBy].toLowerCase()}`;

  // ── Unfollow tooltip (why the bulk action is unavailable) ───────
  const unfollowTip = (() => {
    if (!initial?.unfollowTip) return null;
    const mine = selectedItems.filter((item) => item.reason !== "follow");
    const follow = selectedItems.filter((item) => item.reason === "follow");
    const label = (item: MyWorkItemView) => item.key ?? item.name;
    return (
      <>
        <b className="font-semibold">{copy.list.bulk.unfollowWhy}</b>{" "}
        {formatCopy(copy.list.bulk.unfollowDetail, {
          mine: mine.map(label).join(", "),
          follow: follow.map(label).join(", "),
        })}
      </>
    );
  })();

  // The real reason, from the selection: only items the member follows (and
  // does not own) can be unfollowed.
  const unfollowWhy = (() => {
    const mine = selectedItems.filter((item) => item.reason !== "follow");
    if (mine.length === 0) return null;
    const follow = selectedItems.filter((item) => item.reason === "follow");
    const label = (item: MyWorkItemView) => item.key ?? item.name;
    return (
      <>
        <b className="font-semibold">{copy.list.bulk.unfollowWhy}</b>{" "}
        {follow.length > 0
          ? formatCopy(copy.list.bulk.unfollowDetail, {
              mine: mine.map(label).join(", "),
              follow: follow.map(label).join(", "),
            })
          : formatCopy(copy.list.bulk.unfollowNone, {
              mine: mine.map(label).join(", "),
            })}
      </>
    );
  })();

  return (
    <div
      data-f="main"
      className={cn(
        "relative box-border flex h-full min-h-0 flex-col text-sm leading-[1.45]",
        narrow && "pr-[560px]",
      )}
    >
      <MyWorkHeader
        copy={copy}
        subline={subline}
        calendarHref={props.calendarHref ?? null}
      />
      <MyWorkTabs
        copy={copy}
        tab={tab}
        counts={counts}
        available={tabAvailable}
        onTab={(next) => {
          setTab(next);
          clearSelection();
        }}
        showCounts={hasItems}
        narrow={narrow}
      />
      <MyWorkBar
        copy={copy}
        hasItems={hasItems}
        groupBy={groupBy}
        onGroup={setGroupBy}
        menu={menu}
        onMenu={setMenu}
        departments={departmentList}
        selectedDepartments={filters.departments}
        onToggleDepartment={(name) =>
          setFilters((previous) => ({
            ...previous,
            departments: toggled(previous.departments, name),
          }))
        }
        boards={boardOptions}
        selectedBoards={filters.boards}
        onToggleBoard={(id) =>
          setFilters((previous) => ({
            ...previous,
            boards: toggled(previous.boards, id),
          }))
        }
        selectedPriorities={filters.priorities}
        onTogglePriority={
          allItems.some((item) => item.priority)
            ? (priority: WorkPriority) =>
                setFilters((previous) => ({
                  ...previous,
                  priorities: toggled(previous.priorities, priority),
                }))
            : undefined
        }
        query={filters.query}
        onQuery={(query) => setFilters((previous) => ({ ...previous, query }))}
        createBoards={createBoards}
        createBoardId={createBoardId}
        onCreateBoard={setCreateBoardId}
        createName={createName}
        onCreateName={setCreateName}
        onCreate={
          props.onCreate && createBoardId && createName.trim()
            ? () => {
                props.onCreate?.({
                  boardId: createBoardId,
                  name: createName.trim(),
                });
                setCreateName("");
                setMenu(null);
              }
            : null
        }
        groupAvailable={groupAvailable}
      />

      <div data-f="body" className="flex min-h-0 flex-1 gap-6 px-7">
        <div
          ref={panelRef}
          id="mw-panel"
          data-f="lcol"
          data-selecting={selecting ? "true" : undefined}
          role="tabpanel"
          aria-label={panelLabel}
          className="group/lcol relative flex min-w-0 flex-1 flex-col"
        >
          {state.status === "loading" ? (
            <div
              data-f="scroll"
              className="flex min-h-0 flex-1 flex-col gap-[18px] overflow-auto px-0.5 pt-1.5 pb-[84px]"
            >
              <RowsSkeleton rows={3} />
              <RowsSkeleton rows={2} />
            </div>
          ) : state.status === "error" ? (
            <div
              role="alert"
              className="mt-2 flex flex-wrap items-center justify-between gap-2 rounded-[12px] border border-border bg-popover px-3.5 py-3 dark:bg-card"
            >
              <p className="m-0 text-compact text-muted-foreground">
                {copy.list.error}
              </p>
              <button
                type="button"
                className={SMALL_OUTLINE_CLASS}
                onClick={props.onRetry}
              >
                {copy.list.retry}
              </button>
            </div>
          ) : !doneTab && !hasItems ? (
            <MyWorkEmpty
              copy={copy}
              navigation={navigation}
              filtered={false}
              firstDepartment={props.firstDepartment ?? null}
              onCreate={() => setMenu("create")}
            />
          ) : (
            <>
              <div
                data-f="scroll"
                className="flex min-h-0 flex-1 flex-col gap-[18px] overflow-auto px-0.5 pt-1.5 pb-[84px]"
              >
                {doneTab ? <MyWorkNote>{copy.list.doneNote}</MyWorkNote> : null}
                {groups.length === 0 ? (
                  <MyWorkEmpty
                    copy={copy}
                    navigation={navigation}
                    filtered={filtersActive}
                    firstDepartment={null}
                    onCreate={() => setMenu("create")}
                  />
                ) : null}
                {groups.map((group) => {
                  const open = !collapsed.has(group.id);
                  return (
                    <section key={group.id} aria-labelledby={`g-${group.id}`}>
                      <h2
                        id={`g-${group.id}`}
                        data-f="gh"
                        className={cn(
                          "m-0 mb-2 flex min-h-6 items-center gap-2 text-sm leading-[1.45] font-[650]",
                          group.tone === "over" && "text-danger-text",
                        )}
                      >
                        <button
                          data-f="tog"
                          type="button"
                          aria-expanded={open}
                          aria-label={formatCopy(
                            open ? copy.list.collapse : copy.list.expand,
                            { name: group.name },
                          )}
                          onClick={() =>
                            setCollapsed((previous) => {
                              const next = new Set(previous);
                              if (next.has(group.id)) next.delete(group.id);
                              else next.add(group.id);
                              return next;
                            })
                          }
                          className="-ml-1 grid size-6 cursor-pointer place-items-center rounded-[6px] border-0 bg-transparent p-0 text-meta-foreground outline-none hover:bg-state-hover-strong hover:text-foreground focus-visible:ring-2 focus-visible:ring-focus-ring"
                        >
                          <ChevronDown
                            aria-hidden="true"
                            className={cn(
                              "size-3.5 transition-transform",
                              !open && "-rotate-90",
                            )}
                            strokeWidth={1.8}
                          />
                        </button>
                        {group.board ? (
                          <>
                            {group.board.department ? (
                              <DepartmentMark
                                name={group.board.department.name}
                                markTone={group.board.department.markTone}
                              />
                            ) : null}
                            <ShellLink
                              destination={{
                                section: "board",
                                boardId: group.board.boardId,
                              }}
                              navigation={navigation}
                              className="no-underline outline-none hover:underline focus-visible:underline"
                            >
                              {group.board.name}
                            </ShellLink>
                          </>
                        ) : (
                          group.name
                        )}
                        <span
                          data-f="gn"
                          className="font-medium text-meta-foreground tabular-nums"
                        >
                          {group.count}
                        </span>
                        {group.sub ? (
                          <small className="text-crumb leading-[1.45] font-normal text-meta-foreground">
                            {group.sub}
                          </small>
                        ) : null}
                      </h2>
                      {open ? (
                        <ul
                          data-f="rows"
                          className="m-0 list-none rounded-[12px] border border-border bg-popover p-0 dark:bg-card"
                        >
                          {group.items.map((item) => (
                            <MyWorkRow
                              key={item.id}
                              item={item}
                              copy={copy}
                              now={now}
                              columns={columns}
                              narrow={narrow}
                              showBoard={groupBy !== "board" || doneTab}
                              selected={selected.has(item.id)}
                              current={props.openItemId === item.id}
                              completing={props.completingIds.has(item.id)}
                              canComplete={canComplete}
                              statusPopOpen={statusPopFor === item.id}
                              done={doneTab}
                              href={navigation.getHref({
                                section: "board",
                                boardId: item.boardId,
                                item: item.id,
                              })}
                              actions={
                                props.actions
                                  ? {
                                      busy: props.actions.pendingIds.has(
                                        item.id,
                                      ),
                                      onApprove: () =>
                                        props.actions?.onApprove(item),
                                      onRequestChanges: () =>
                                        props.actions?.onRequestChanges(item),
                                      onReopen: () =>
                                        props.actions?.onReopen(item),
                                    }
                                  : undefined
                              }
                              onToggleSelect={() => toggleSelect(item.id)}
                              onComplete={() => completeItem?.(item)}
                              onOpen={() => props.onOpenItem(item)}
                              onStatusClick={
                                props.statusOptions && props.onChangeStatus
                                  ? () =>
                                      setStatusPopFor(
                                        statusPopFor === item.id
                                          ? null
                                          : item.id,
                                      )
                                  : props.statusOptions
                                    ? () =>
                                        setStatusPopFor(
                                          statusPopFor === item.id
                                            ? null
                                            : item.id,
                                        )
                                    : null
                              }
                              statusPop={
                                statusPopItem?.id === item.id &&
                                props.statusOptions ? (
                                  <StatusPop
                                    copy={copy}
                                    item={item}
                                    options={props.statusOptions}
                                    onPick={(bucket) => {
                                      props.onChangeStatus?.(item, bucket);
                                      setStatusPopFor(null);
                                    }}
                                    onClose={() => setStatusPopFor(null)}
                                  />
                                ) : null
                              }
                            />
                          ))}
                        </ul>
                      ) : null}
                    </section>
                  );
                })}
                {props.hasMore ? (
                  <div className="flex justify-center">
                    <button
                      type="button"
                      className={SMALL_OUTLINE_CLASS}
                      aria-disabled={props.loadingMore ? true : undefined}
                      onClick={props.loadingMore ? undefined : props.onLoadMore}
                    >
                      {props.loadingMore
                        ? copy.list.loadingMore
                        : copy.list.loadMore}
                    </button>
                  </div>
                ) : null}
              </div>
              {selecting ? (
                <MyWorkBulkBar
                  copy={copy}
                  count={selectedItems.length || selected.size}
                  canComplete={canComplete}
                  onComplete={completeSelected}
                  onClear={clearSelection}
                  unfollowTip={unfollowTip}
                  unfollowWhy={unfollowWhy}
                  onUnfollow={
                    props.actions && !unfollowWhy && !doneTab
                      ? () => {
                          props.actions?.onUnfollow(selectedItems);
                          clearSelection();
                        }
                      : null
                  }
                  onSetDue={
                    props.actions && !doneTab
                      ? (dueAt) => {
                          props.actions?.onSetDue(selectedItems, dueAt);
                          clearSelection();
                        }
                      : null
                  }
                  onSetStatus={
                    props.actions && !doneTab
                      ? (bucket) => {
                          props.actions?.onSetStatus(selectedItems, bucket);
                          clearSelection();
                        }
                      : null
                  }
                  statusOptions={props.statusOptions}
                  today={now}
                />
              ) : null}
            </>
          )}
        </div>

        {hasItems && !narrow ? (
          <MyWorkRail
            copy={copy}
            navigation={navigation}
            days={week}
            range={weekRange(week)}
            overdue={overdue}
            departments={departments}
            total={allItems.length}
            waiting={props.waiting ?? []}
            dueOn={filters.dueOn}
            onRemind={props.actions?.onRemind}
            remindingIds={props.actions?.pendingIds}
            onDay={(shortDate) =>
              setFilters((previous) => ({
                ...previous,
                dueOn: previous.dueOn === shortDate ? null : shortDate,
              }))
            }
          />
        ) : null}
      </div>

      <MyWorkFoot copy={copy} />
      {props.sheet}
    </div>
  );
}

function toggled<T extends string>(list: readonly T[], value: T): T[] {
  return list.includes(value)
    ? list.filter((entry) => entry !== value)
    : [...list, value];
}

/** The Status popover of a row (`.pop.st`): a search box and the choices. */
function StatusPop({
  copy,
  item,
  options,
  onPick,
  onClose,
}: {
  copy: WorkspaceHomeCopy;
  item: MyWorkItemView;
  options: readonly { bucket: WorkStatusBucket; label: string }[];
  onPick: (bucket: WorkStatusBucket) => void;
  onClose: () => void;
}) {
  const [query, setQuery] = useState("");
  const [active, setActive] = useState(() => {
    const current = options.findIndex(
      (option) => option.bucket === item.status?.bucket,
    );
    return current === -1 ? 0 : (current + 1) % options.length;
  });
  const inputRef = useRef<HTMLInputElement>(null);
  useEffect(() => {
    inputRef.current?.focus();
  }, []);
  const shown = options.filter((option) =>
    option.label.toLowerCase().includes(query.trim().toLowerCase()),
  );
  const activeIndex = Math.min(active, Math.max(0, shown.length - 1));
  const onKeyDown = (event: ReactKeyboardEvent<HTMLDivElement>) => {
    if (event.key === "Escape") {
      event.preventDefault();
      onClose();
    } else if (event.key === "ArrowDown" || event.key === "ArrowUp") {
      event.preventDefault();
      const step = event.key === "ArrowDown" ? 1 : -1;
      setActive((activeIndex + step + shown.length) % shown.length);
    } else if (event.key === "Enter") {
      event.preventDefault();
      const option = shown[activeIndex];
      if (option) onPick(option.bucket);
    } else if (/^[1-9]$/.test(event.key) && query === "") {
      const option = shown[Number(event.key) - 1];
      if (option) {
        event.preventDefault();
        onPick(option.bucket);
      }
    }
  };
  return (
    <div
      data-f="statuspop"
      onKeyDown={onKeyDown}
      role="group"
      aria-label={formatCopy(copy.list.statusPopLabel, {
        name: item.key ?? item.name,
      })}
      className="absolute top-8 left-0 z-dropdown box-content w-[208px] rounded-[10px] border border-line-strong bg-popover p-1.5 whitespace-normal text-foreground shadow-popover dark:bg-card"
    >
      <input
        ref={inputRef}
        type="text"
        value={query}
        onChange={(event) => setQuery(event.target.value)}
        placeholder={copy.list.statusSearch}
        aria-label={copy.list.statusSearch}
        className="mb-1 box-border h-[30px] w-full rounded-[7px] border border-line-strong bg-background px-[9px] text-compact leading-[1.45] text-foreground outline-none"
      />
      <div
        role="listbox"
        aria-label={formatCopy(copy.list.statusPopLabel, {
          name: item.key ?? item.name,
        })}
      >
        {shown.map((option, index) => {
          const current = option.bucket === item.status?.bucket;
          return (
            <div
              key={option.bucket}
              role="option"
              aria-selected={current}
              onClick={() => onPick(option.bucket)}
              onMouseEnter={() => setActive(index)}
              className={cn(
                "flex h-8 cursor-pointer items-center gap-2 rounded-[7px] px-2 text-compact leading-[1.45]",
                index === activeIndex && "bg-state-hover-strong font-semibold",
              )}
            >
              <StatusDot bucket={option.bucket} />
              {option.label}
              {current ? (
                <Check
                  aria-hidden="true"
                  className="ml-auto size-3.5 text-state-selected-foreground"
                  strokeWidth={1.8}
                />
              ) : (
                <span className="ml-auto rounded-[4px] border border-line-strong bg-keycap px-[5px] font-mono text-key leading-[18px] text-muted-foreground">
                  {index + 1}
                </span>
              )}
            </div>
          );
        })}
      </div>
    </div>
  );
}
