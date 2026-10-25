import { useCallback, useEffect, useMemo, useState } from "react";
import { CircleCheck } from "lucide-react";
import { Skeleton } from "@notrelix/ui-web";
import { formatCopy } from "../shell/shell-models";
import type { SectionState } from "../home/home-models";
import {
  InboxDayHeader,
  InboxListHeader,
  InboxRow,
  InboxTabs,
  InboxToolbar,
} from "./inbox-list";
import {
  INBOX_TABS,
  type InboxActions,
  type InboxItemView,
  type InboxTab,
} from "./inbox-models";
import { InboxRead, InboxReadEmpty } from "./inbox-read";
import type { WorkspaceInboxCopy } from "./workspace-inbox.copy";

/** The view state a story or test starts from; the page owns it afterwards. */
export interface InboxInitialView {
  readonly tab?: InboxTab;
  readonly unreadOnly?: boolean;
  readonly selectedId?: string;
  readonly openBundles?: readonly string[];
}

export interface WorkspaceInboxSurfaceProps {
  readonly copy: WorkspaceInboxCopy;
  /** Reference time for the day headers and the time of each row (epoch ms). */
  readonly now: number;
  readonly state: SectionState<readonly InboxItemView[]>;
  /**
   * The "Chỉ workspace X" line; `null` leaves it out (the notification read
   * is not scoped to a workspace, so the page cannot say it is).
   */
  readonly scope: { workspaceName: string; allHref: string | null } | null;
  /** Tabs other than All that have data behind them. */
  readonly tabsAvailable?: Partial<Record<Exclude<InboxTab, "all">, boolean>>;
  /** Counts from the producer; the surface counts the loaded items otherwise. */
  readonly counts?: Partial<Record<InboxTab, number>>;
  readonly settingsHref: string | null;
  readonly onOpenSettings: () => void;
  /** Marks one notification handled (read); `null` draws "Đã xử lý" inert. */
  readonly onMarkRead: ((id: string) => void) | null;
  /** Marks every notification read; `null` draws the button inert. */
  readonly onMarkAllRead: (() => void) | null;
  readonly onRetry: () => void;
  /** Older notifications the producer still holds; `null` or omitted draws no button. */
  readonly loadMore?: {
    readonly onLoadMore: () => void;
    readonly loading: boolean;
  } | null;
  /**
   * The Done filter: `active` lists the done entries instead of the inbox
   * (the host loads that list); `null` or omitted draws no toggle.
   */
  readonly done?: {
    readonly active: boolean;
    readonly onToggle: () => void;
  } | null;
  /** Approve, reply, handle, snooze and mute; omitted draws them inert. */
  readonly actions?: InboxActions;
  readonly initialView?: InboxInitialView;
}

const DAY = 24 * 60 * 60 * 1000;

function startOfLocalDay(time: number): number {
  const date = new Date(time);
  date.setHours(0, 0, 0, 0);
  return date.getTime();
}

function pad(value: number): string {
  return String(value).padStart(2, "0");
}

/** "12 phút", "1 giờ", then the clock time ("08:00"). */
export function inboxTimeLabel(
  copy: WorkspaceInboxCopy,
  at: string,
  now: number,
): string {
  const time = Date.parse(at);
  if (Number.isNaN(time)) return "";
  const diff = now - time;
  if (diff >= 0 && diff < 60 * 1000) return copy.time.now;
  if (diff >= 0 && diff < 60 * 60 * 1000)
    return formatCopy(copy.time.minutes, { count: Math.floor(diff / 60000) });
  if (diff >= 0 && diff < 2 * 60 * 60 * 1000)
    return formatCopy(copy.time.hours, { count: 1 });
  const date = new Date(time);
  return `${pad(date.getHours())}:${pad(date.getMinutes())}`;
}

/** "Hôm nay", "Hôm qua" or `dd/MM` for the day of a notification. */
export function inboxDayLabel(
  copy: WorkspaceInboxCopy,
  at: string,
  now: number,
): string {
  const time = Date.parse(at);
  if (Number.isNaN(time)) return "";
  const today = startOfLocalDay(now);
  const day = startOfLocalDay(time);
  if (day >= today) return copy.days.today;
  if (day >= today - DAY) return copy.days.yesterday;
  const date = new Date(time);
  return `${pad(date.getDate())}/${pad(date.getMonth() + 1)}`;
}

function inTab(item: InboxItemView, tab: InboxTab): boolean {
  switch (tab) {
    case "need":
      return item.need !== null;
    case "mention":
      return item.kind === "mention";
    case "assigned":
      return item.kind === "assign";
    case "follow":
      return item.following;
    case "all":
      return true;
  }
}

function ListSkeleton({ label }: { label: string }) {
  return (
    <div role="status" aria-label={label} className="flex-1 overflow-hidden">
      {[0, 1, 2, 3].map((index) => (
        <div
          key={index}
          data-f="it"
          className="flex gap-[11px] border-b border-border px-4 py-[11px]"
        >
          <Skeleton className="size-[30px] shrink-0 rounded-full" />
          <div className="min-w-0 flex-1">
            <Skeleton className="h-3 w-4/5 rounded-md" />
            <Skeleton className="mt-2 h-3 w-3/5 rounded-md" />
            <Skeleton className="mt-2 h-2.5 w-2/5 rounded-md" />
          </div>
          <Skeleton className="h-3 w-8 rounded-md" />
        </div>
      ))}
    </div>
  );
}

/**
 * The Inbox (Final-Inbox): the notifications by day on the left with their
 * tabs and the unread toggle, the selected one in the reading pane on the
 * right. The page owns only its view state (tab, unread toggle, selection,
 * opened bundles); what "Đã xử lý" and "Đánh dấu tất cả đã đọc" do comes from
 * the host. A control or part with no producer is drawn inert with the reason
 * or left out.
 */
export function WorkspaceInboxSurface(props: WorkspaceInboxSurfaceProps) {
  const { copy, now, state } = props;
  const items = useMemo<readonly InboxItemView[]>(
    () => (state.status === "ready" ? state.data : []),
    [state],
  );
  const available: Record<InboxTab, boolean> = {
    need: props.tabsAvailable?.need ?? false,
    mention: props.tabsAvailable?.mention ?? false,
    assigned: props.tabsAvailable?.assigned ?? false,
    follow: props.tabsAvailable?.follow ?? false,
    all: true,
  };
  const defaultTab: InboxTab = available.need ? "need" : "all";
  const [tab, setTab] = useState<InboxTab>(
    props.initialView?.tab ?? defaultTab,
  );
  const [unreadOnly, setUnreadOnly] = useState<boolean>(
    props.initialView?.unreadOnly ??
      (props.initialView?.tab ?? defaultTab) !== "all",
  );
  const [selectedId, setSelectedId] = useState<string | null>(
    props.initialView?.selectedId ?? null,
  );
  const [openBundles, setOpenBundles] = useState<ReadonlySet<string>>(
    () => new Set(props.initialView?.openBundles ?? []),
  );

  const counts = useMemo<Partial<Record<InboxTab, number>>>(() => {
    const derived: Partial<Record<InboxTab, number>> = {};
    for (const id of INBOX_TABS) {
      // The design counts three tabs; "Following" and "All" carry no number.
      if (id === "all" || id === "follow") continue;
      derived[id] = items.filter((item) => inTab(item, id)).length;
    }
    return { ...derived, ...props.counts };
  }, [items, props.counts]);

  const doneActive = props.done?.active === true;
  const shown = useMemo(
    () =>
      doneActive
        ? items
        : items.filter(
            (item) => inTab(item, tab) && (!unreadOnly || item.unread),
          ),
    [doneActive, items, tab, unreadOnly],
  );
  const selectable = shown.filter((item) => item.bundle === null);
  const selected =
    selectable.find((item) => item.id === selectedId) ?? selectable[0] ?? null;
  const position = selected
    ? selectable.findIndex((item) => item.id === selected.id)
    : -1;

  const select = useCallback((id: string) => setSelectedId(id), []);
  const move = useCallback(
    (step: 1 | -1) => {
      const next = selectable[position + step];
      if (next) setSelectedId(next.id);
    },
    [position, selectable],
  );
  const [snoozeOpen, setSnoozeOpen] = useState(false);
  const actions = props.actions;
  const canHandle = selected && (selected.unread || selected.need !== null);
  const handled =
    selected && canHandle
      ? actions
        ? () => actions.onHandled(selected)
        : props.onMarkRead
          ? () => props.onMarkRead?.(selected.id)
          : null
      : null;

  useEffect(() => {
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
      if (key === "j") {
        event.preventDefault();
        move(1);
      } else if (key === "k") {
        event.preventDefault();
        move(-1);
      } else if (key === "e" && handled) {
        event.preventDefault();
        handled();
      } else if (key === "h" && actions && selected) {
        event.preventDefault();
        setSnoozeOpen(true);
      }
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [actions, handled, move, selected]);

  // Day groups, in the order the rows come.
  const days: { label: string; rows: InboxItemView[] }[] = [];
  for (const item of shown) {
    const label = inboxDayLabel(copy, item.at, now);
    const last = days[days.length - 1];
    if (last && last.label === label) last.rows.push(item);
    else days.push({ label, rows: [item] });
  }

  const loading = state.status === "loading";
  const failed = state.status === "error";
  const nothing = state.status === "ready" && shown.length === 0;
  const allEmpty = state.status === "ready" && items.length === 0;

  return (
    <div data-f="page" className="flex h-full min-h-0 text-sm leading-[1.45]">
      <section
        data-f="list"
        aria-label={copy.listLabel}
        className="box-content flex h-full min-h-0 w-[440px] flex-none flex-col border-r border-border bg-popover dark:bg-card"
      >
        <InboxListHeader
          copy={copy}
          onMarkAllRead={props.onMarkAllRead}
          settingsHref={props.settingsHref}
          onOpenSettings={props.onOpenSettings}
          scope={props.scope}
        />
        <InboxTabs
          copy={copy}
          tab={tab}
          counts={counts}
          available={available}
          onTab={(next) => {
            setTab(next);
            setUnreadOnly(next !== "all");
          }}
        />
        <InboxToolbar
          copy={copy}
          unreadOnly={unreadOnly}
          onToggleUnread={() => setUnreadOnly((value) => !value)}
          done={props.done}
        />
        <div
          id="inbox-panel"
          role="tabpanel"
          aria-labelledby={`inbox-tab-${tab}`}
          className="flex min-h-0 flex-1 flex-col"
        >
          {loading ? (
            <ListSkeleton label={copy.loading} />
          ) : failed ? (
            <div
              role="alert"
              className="m-3 flex flex-wrap items-center justify-between gap-2 rounded-[10px] border border-border px-3 py-2.5"
            >
              <p className="m-0 text-compact text-muted-foreground">
                {copy.error}
              </p>
              <button
                type="button"
                onClick={props.onRetry}
                className="inline-flex h-7 cursor-pointer items-center rounded-[8px] border border-line-strong bg-popover px-2.5 text-crumb font-medium text-foreground outline-none hover:bg-state-hover focus-visible:ring-2 focus-visible:ring-focus-ring dark:bg-card"
              >
                {copy.retry}
              </button>
            </div>
          ) : nothing ? (
            <div
              data-f="empty"
              className="flex flex-1 flex-col items-center justify-center gap-2.5 p-10 text-center text-muted-foreground"
            >
              <span
                data-f="eic"
                className="grid size-[52px] place-items-center rounded-[14px] bg-status-chip-done text-work-status-done"
              >
                <CircleCheck
                  aria-hidden="true"
                  className="size-6"
                  strokeWidth={1.8}
                />
              </span>
              <b className="text-base leading-[1.45] font-semibold text-foreground">
                {doneActive
                  ? copy.doneEmpty.title
                  : allEmpty && tab === "all"
                    ? copy.allEmpty.title
                    : unreadOnly && tab === "all"
                      ? copy.filteredEmpty.title
                      : copy.empty.title}
              </b>
              <p className="m-0 max-w-[44ch] text-nav leading-[1.45] font-normal">
                {doneActive
                  ? copy.doneEmpty.text
                  : allEmpty && tab === "all"
                    ? copy.allEmpty.text
                    : unreadOnly && tab === "all"
                      ? copy.filteredEmpty.text
                      : copy.empty.text}
              </p>
              {unreadOnly && tab === "all" && !allEmpty ? (
                <button
                  type="button"
                  onClick={() => setUnreadOnly(false)}
                  className="inline-flex h-8 cursor-pointer items-center rounded-[7px] border border-line-strong bg-popover px-3 text-compact font-medium text-foreground outline-none hover:bg-state-hover focus-visible:ring-2 focus-visible:ring-focus-ring dark:bg-card"
                >
                  {copy.filteredEmpty.showAll}
                </button>
              ) : !allEmpty || tab !== "all" ? (
                <button
                  type="button"
                  aria-disabled={props.settingsHref ? undefined : true}
                  title={
                    props.settingsHref ? undefined : copy.settingsUnavailable
                  }
                  onClick={
                    props.settingsHref ? props.onOpenSettings : undefined
                  }
                  className="inline-flex h-8 cursor-pointer items-center rounded-[7px] border border-line-strong bg-popover px-3 text-compact leading-[1.45] font-medium text-foreground outline-none hover:bg-state-hover focus-visible:ring-2 focus-visible:ring-focus-ring aria-disabled:cursor-not-allowed dark:bg-card"
                >
                  {copy.empty.settings}
                </button>
              ) : null}
            </div>
          ) : (
            <div data-f="items" className="flex-1 overflow-auto">
              {days.map((day) => (
                <div key={day.label}>
                  <InboxDayHeader>{day.label}</InboxDayHeader>
                  {day.rows.map((item) => (
                    <InboxRow
                      key={item.id}
                      item={item}
                      copy={copy}
                      selected={selected?.id === item.id}
                      timeLabel={inboxTimeLabel(copy, item.at, now)}
                      expanded={openBundles.has(item.id)}
                      onSelect={() => select(item.id)}
                      onToggleBundle={() =>
                        setOpenBundles((previous) => {
                          const next = new Set(previous);
                          if (next.has(item.id)) next.delete(item.id);
                          else next.add(item.id);
                          return next;
                        })
                      }
                    />
                  ))}
                </div>
              ))}
              {props.loadMore ? (
                <div className="flex justify-center p-3">
                  <button
                    data-f="more"
                    type="button"
                    disabled={props.loadMore.loading}
                    onClick={props.loadMore.onLoadMore}
                    className="inline-flex h-8 cursor-pointer items-center rounded-[7px] border border-line-strong bg-popover px-3 text-compact font-medium text-foreground outline-none hover:bg-state-hover focus-visible:ring-2 focus-visible:ring-focus-ring disabled:cursor-not-allowed disabled:opacity-60 dark:bg-card"
                  >
                    {props.loadMore.loading ? copy.loadingMore : copy.loadMore}
                  </button>
                </div>
              ) : null}
            </div>
          )}
        </div>
      </section>

      {selected ? (
        <InboxRead
          copy={copy}
          item={selected}
          position={position}
          total={selectable.length}
          onPrevious={() => move(-1)}
          onNext={() => move(1)}
          onHandled={handled}
          actions={actions}
          snoozeOpen={snoozeOpen}
          onSnoozeOpen={setSnoozeOpen}
        />
      ) : (
        <InboxReadEmpty
          copy={copy}
          title={copy.empty.readTitle}
          text={copy.empty.readText}
        />
      )}
    </div>
  );
}
