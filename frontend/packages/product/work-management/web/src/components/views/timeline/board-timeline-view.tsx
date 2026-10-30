import { useCallback, useContext, useMemo, useRef, useState } from "react";
import { toast } from "sonner";
import { useLocale } from "@notrelix/i18n";
import {
  useBoardKanban,
  useBoardPlanItems,
  useTimelineDependencies,
  useUpdateCard,
} from "@notrelix/work-management-state";
import {
  serializeCompoundFilter,
  type Card,
  type WorkStatusCategory,
} from "@notrelix/work-management-core";
import { ItemCreateHost } from "../../item-create/item-create-host";
import { BoardHeaderDataContext } from "../../board-layout/board-header-data-context";
import type {
  ItemSheetGroup,
  ItemSheetMark,
} from "../../card-detail/task-detail-types";
import { useBoardCapabilities } from "../../shared/board-capabilities";
import { HANOI_ZONES, runtimeZone, useNow } from "../../shared/board-clock";
import { calendarParts, identityMarkClass } from "../../shared/item-display";
import { useItemSheetActions } from "../../shared/use-item-sheet-actions";
import {
  cardDue,
  dayOf,
  shiftInstant,
  zonedInstant,
} from "../calendar/calendar-entries";
import { toDayKey, type DayKey } from "../calendar/calendar-model";
import { useBoardViewState } from "../../filters/use-board-view-state";
import { ViewStateBar } from "../../filters/view-state-bar";
import { SavedFiltersControl } from "../../saved-filters/saved-filters-control";
import { emptyKanbanFilter } from "../kanban/kanban-filter";
import { KanbanSelectedItem } from "../kanban/kanban-view";
import { buildCardLink } from "../kanban/kanban-view-helpers";
import { BoardTimelineSurface } from "./board-timeline-surface";
import type { TimelineUndated } from "./timeline-tray";
import type {
  TimelineGroup,
  TimelineLink,
  TimelineItem,
  TimelineScale,
  TimelineTone,
} from "./timeline-model";
import { criticalIds, fill } from "./timeline-model";
import { useTimelineCopy } from "./use-timeline-copy";

const NEVER_OVERDUE = () => false;
let linkToasts = 0;

export interface BoardTimelineViewProps {
  boardId: string;
  workspaceId: string;
  /** The item the board route's `item` parameter names; its sheet opens. */
  selectedItemId?: string;
  /**
   * Opening passes the id (push). Closing passes `replace` so Back does not
   * step through every item.
   */
  onSelectedItemChange?: (
    id: string | undefined,
    options?: { replace?: boolean },
  ) => void;
  /** The signed-in member (from the host's session): offers "Giao cho tôi". */
  currentUserId?: string | null;
  /** The route `filter` parameter (compound or older facet form), shared with the other views. */
  filter?: string | undefined;
  onFilterChange?: (
    value: string | undefined,
    options?: { replace?: boolean },
  ) => void;
  /** Switches the board to the Calendar, where an item gets its first date. */
  onOpenCalendar?: () => void;
  /** The IANA time zone of the days; the runtime zone when omitted. */
  timeZone?: string;
  /** Injected clock; the system clock, once a minute, when omitted. */
  now?: Date;
}

function toneOf(category: WorkStatusCategory | null): TimelineTone {
  if (category === "done") return "done";
  if (category === "review") return "review";
  if (category === "in_progress") return "doing";
  return "todo";
}

function percentOf(
  card: Card,
  tone: TimelineTone,
  planned: number | undefined,
): number {
  if (tone === "done") return 100;
  if (planned !== undefined) return planned;
  const progress = card.childProgress;
  if (!progress || progress.total <= 0) return 0;
  return Math.round((progress.done / progress.total) * 100);
}

/**
 * The Timeline of a board: dated items as bars from start to due on a day,
 * week or month scale, grouped by the board's groups, with the item side
 * sheet shared with the other views, the board's phases and milestones, and
 * the finish-to-start links with the critical path derived from them (the
 * plan read, gap `CTR-GAP-WM-PLAN-ITEMS`). What the viewer
 * may do comes from the board access read: only an editor drags a bar.
 */
export function BoardTimelineView({
  boardId,
  workspaceId,
  selectedItemId,
  currentUserId,
  onSelectedItemChange,
  filter: filterParam,
  onFilterChange,
  onOpenCalendar,
  timeZone: timeZoneProp,
  now: nowProp,
}: BoardTimelineViewProps) {
  const copy = useTimelineCopy();
  const header = useContext(BoardHeaderDataContext);
  const rootRef = useRef<HTMLDivElement>(null);
  const { locale } = useLocale();
  const now = useNow(nowProp);
  const timeZone = timeZoneProp ?? runtimeZone();

  const [scale, setScale] = useState<TimelineScale>("day");
  const todayParts = calendarParts(now, timeZone);
  const today = toDayKey(todayParts.year, todayParts.month, todayParts.day);
  const [anchorDay, setAnchorDay] = useState<DayKey>(today);
  const [createOpen, setCreateOpen] = useState(false);

  const [localFilter, setLocalFilter] = useState<string | undefined>();
  const filterValue = onFilterChange ? filterParam : localFilter;
  const setFilterValue = useCallback(
    (value: string | undefined, options?: { replace?: boolean }) => {
      if (onFilterChange) onFilterChange(value, options);
      else setLocalFilter(value);
    },
    [onFilterChange],
  );
  const noLegacyFilter = useMemo(() => emptyKanbanFilter(), []);
  const {
    copy: filtersCopy,
    view,
    kanbanView,
  } = useBoardViewState({
    boardId,
    workspaceId,
    routeFilter: filterValue,
    onRouteFilterChange: setFilterValue,
  });
  const kanban = useBoardKanban(boardId, workspaceId, {
    filter: noLegacyFilter,
    view: kanbanView,
    isOverdue: NEVER_OVERDUE,
  });
  const { capabilities } = useBoardCapabilities(boardId, workspaceId);
  const canEdit = capabilities.canMoveCard && capabilities.canCreateCard;
  const updateCard = useUpdateCard(boardId, workspaceId);
  const dependencies = useTimelineDependencies(boardId, workspaceId);
  const sheet = useItemSheetActions({ boardId, workspaceId, kanban });

  const planQuery = useBoardPlanItems(boardId, workspaceId);
  const plan = planQuery.data;
  const { board } = kanban;
  const filtered = view.applied.filter.conditions.length > 0;
  const { groups, undated, cardsById, links } = useMemo(() => {
    // An item with no start day is listed only when a dependency names it;
    // one with a due day alone belongs to the Calendar.
    const linked = new Set(
      (plan?.links ?? []).flatMap((link) => [link.from, link.to]),
    );
    const byId = new Map<string, Card>();
    const byGroup = new Map<string, TimelineItem[]>();
    const undatedItems: TimelineUndated[] = [];
    for (const lane of kanban.lanes) {
      const tone = toneOf(lane.category);
      for (const card of lane.cards) {
        byId.set(card.id, card);
        const due = dayOf(cardDue(card), timeZone);
        const start = dayOf(card.startDate, timeZone);
        if (!due && !start) {
          undatedItems.push({
            id: card.id,
            itemKey: card.itemKey,
            title: card.title,
          });
          continue;
        }
        if (!start && !linked.has(card.id)) continue;
        const person = card.members[0];
        const item: TimelineItem = {
          id: card.id,
          kind: "item",
          itemKey: card.itemKey,
          title: card.title,
          tone,
          startDay: start?.day ?? null,
          dueDay: due?.day ?? null,
          percent: percentOf(card, tone, plan?.percents[card.id]),
          assignee: person
            ? {
                initials: person.initials,
                markClass: identityMarkClass(person.userId),
                name: person.name,
              }
            : null,
          movable: Boolean(due),
          href: buildCardLink(window.location, card.id),
        };
        const groupId = plan?.groups[card.id] ?? card.listId;
        const list = byGroup.get(groupId) ?? [];
        list.push(item);
        byGroup.set(groupId, list);
      }
    }
    const dayKeyOf = (item: TimelineItem) => item.startDay ?? item.dueDay ?? "";
    const markers = new Map<string, { index: number; item: TimelineItem }[]>();
    // Phases and milestones belong to nobody: any filter hides them with the
    // items it filters out.
    for (const marker of filtered ? [] : (plan?.items ?? [])) {
      const list = markers.get(marker.groupId) ?? [];
      list.push({
        index: marker.index,
        item: {
          id: marker.id,
          kind: marker.kind,
          title: marker.title,
          tone: marker.category,
          startDay: marker.startDay,
          dueDay: marker.endDay,
          percent: marker.percent,
          assignee: null,
          movable: false,
        },
      });
      markers.set(marker.groupId, list);
    }
    const built: TimelineGroup[] = [...kanban.groups]
      .sort((a, b) => a.position - b.position)
      .map((group) => {
        const rows = (byGroup.get(group.id) ?? []).sort((a, b) =>
          dayKeyOf(a).localeCompare(dayKeyOf(b)),
        );
        for (const { index, item } of (markers.get(group.id) ?? []).sort(
          (a, b) => a.index - b.index,
        )) {
          rows.splice(Math.min(index, rows.length), 0, item);
        }
        return { id: group.id, title: group.title, items: rows };
      });
    const shown = new Set(built.flatMap((g) => g.items.map((i) => i.id)));
    const shownLinks = (plan?.links ?? []).filter(
      (link) => shown.has(link.from) && shown.has(link.to),
    );
    const dueOf = new Map(
      built.flatMap((g) => g.items.map((i) => [i.id, i.dueDay ?? ""] as const)),
    );
    const critical = criticalIds(shownLinks, dueOf);
    const flagged = built.map((group) => ({
      ...group,
      items: group.items.map((item) =>
        critical.has(item.id) ? { ...item, critical: true } : item,
      ),
    }));
    return {
      groups: flagged,
      undated: undatedItems,
      cardsById: byId,
      links: shownLinks,
    };
  }, [kanban.lanes, kanban.groups, timeZone, plan, filtered]);

  const handleReschedule = (
    item: TimelineItem,
    change: { mode: "move" | "resize"; days: number },
  ) => {
    const card = cardsById.get(item.id);
    const due = card ? cardDue(card) : undefined;
    if (!card || !due || !canEdit || change.days === 0) return;
    // A move keeps the span's length (start and due shift together); a
    // resize changes only the due day.
    updateCard.mutate(
      {
        cardId: card.id,
        patch: {
          dueDate: shiftInstant(due, change.days),
          ...(change.mode === "move" && card.startDate
            ? { startDate: shiftInstant(card.startDate, change.days) }
            : {}),
        },
      },
      {
        onError: () =>
          toast.error(fill(copy.reschedule.failed, { title: card.title }), {
            id: `timeline-write-${card.id}`,
          }),
      },
    );
  };

  const nameOfItem = (id: string) => {
    const card = cardsById.get(id);
    return card?.itemKey ?? card?.title ?? id;
  };
  const linkNames = (link: TimelineLink) => ({
    from: nameOfItem(link.from),
    to: nameOfItem(link.to),
  });
  const isConflict = (error: unknown) =>
    (error as { kind?: unknown; status?: number } | null)?.kind ===
      "conflict" || (error as { status?: number } | null)?.status === 409;

  /** Creates or removes a link; the success toast undoes it with the inverse request. */
  const writeLink = (link: TimelineLink, kind: "create" | "remove") => {
    const names = linkNames(link);
    const text = copy.linkToast;
    // One toast per attempt: Thử lại makes Sonner remove the toast it sits on,
    // and a result toast reusing that id inherits the removal and never shows.
    const toastId = `timeline-link-${link.from}-${link.to}-${++linkToasts}`;
    const run =
      kind === "create"
        ? dependencies.createDependency
        : dependencies.removeDependency;
    const inverse =
      kind === "create"
        ? dependencies.removeDependency
        : dependencies.createDependency;
    void run(link).then(
      () =>
        toast.success(
          fill(kind === "create" ? text.created : text.removed, names),
          {
            id: toastId,
            duration: 8000,
            action: {
              label: text.undo,
              onClick: () =>
                void inverse(link).then(
                  () => toast.success(text.undone),
                  () => toast.error(text.undoFailed),
                ),
            },
          },
        ),
      (error: unknown) =>
        isConflict(error)
          ? toast.error(text.conflict, {
              id: toastId,
              action: {
                label: text.reload,
                onClick: () => void planQuery.refetch(),
              },
            })
          : toast.error(
              fill(
                kind === "create" ? text.createFailed : text.removeFailed,
                names,
              ),
              {
                id: toastId,
                action: {
                  label: text.retry,
                  onClick: () => writeLink(link, kind),
                },
              },
            ),
    );
  };

  /** One day for an undated item: start and due are both that day. */
  const handleSchedule = (id: string, day: DayKey) => {
    const card = cardsById.get(id);
    if (!card || !canEdit) return;
    updateCard.mutate(
      {
        cardId: id,
        patch: {
          startDate: zonedInstant(day, 8, 0, timeZone),
          dueDate: zonedInstant(day, 17, 0, timeZone),
        },
      },
      {
        onSuccess: () =>
          toast.success(
            fill(copy.tray.scheduled, {
              key: card.itemKey ?? card.title,
              day: `${day.slice(8)}/${day.slice(5, 7)}`,
            }),
            {
              id: `timeline-write-${id}`,
              duration: 8000,
              action: {
                label: copy.tray.undo,
                onClick: () =>
                  updateCard.mutate({
                    cardId: id,
                    patch: { startDate: null, dueDate: null },
                  }),
              },
            },
          ),
        onError: () =>
          toast.error(fill(copy.tray.failed, { title: card.title }), {
            id: `timeline-write-${id}`,
          }),
      },
    );
  };

  const restoreFocus = (cardId: string) => {
    const opener = rootRef.current?.querySelector<HTMLElement>(
      `[data-row-id="${cardId}"] a`,
    );
    (opener ?? rootRef.current)?.focus();
  };

  // The plan comes with the items, so the rows do not move when it arrives.
  if (kanban.isLoading || planQuery.isLoading) return null;

  const zoneName =
    timeZone && HANOI_ZONES.includes(timeZone)
      ? copy.zone.hanoi
      : copy.zone.local;
  const departmentMark: ItemSheetMark | undefined = header?.space
    ? { initials: header.space.initials, tone: header.space.markTone }
    : undefined;
  const sheetGroups: ItemSheetGroup[] = [...kanban.groups]
    .sort((a, b) => a.position - b.position)
    .map((group) => ({ id: group.id, title: group.title }));

  return (
    <div
      ref={rootRef}
      tabIndex={-1}
      className="relative h-full focus:outline-hidden"
    >
      <BoardTimelineSurface
        copy={copy}
        locale={locale}
        boardTitle={board?.title ?? ""}
        scale={scale}
        onScaleChange={setScale}
        anchorDay={anchorDay}
        onAnchorChange={setAnchorDay}
        today={today}
        zoneName={zoneName}
        groups={groups}
        links={links}
        undatedCount={undated.length}
        undatedItems={undated}
        onSchedule={handleSchedule}
        onCreateLink={(link) => writeLink(link, "create")}
        onRemoveLink={(link) => writeLink(link, "remove")}
        onOpenUndated={onOpenCalendar}
        canEdit={canEdit}
        onOpenItem={(id) => onSelectedItemChange?.(id)}
        onReschedule={handleReschedule}
        onAddItem={() => setCreateOpen(true)}
        viewBar={
          <ViewStateBar
            canSave={capabilities.canEditFields}
            copy={filtersCopy}
            view={view}
            fields={kanban.filterFields}
            memberName={kanban.memberName}
            people={kanban.filterChoices.assignees}
            counts={{ shown: kanban.visibleCount, total: kanban.totalCount }}
            now={now}
            savedFilters={(save) => (
              <SavedFiltersControl
                compact
                workspaceId={workspaceId}
                boardId={boardId}
                filter={serializeCompoundFilter(view.applied.filter)}
                canEdit={capabilities.canEditFields}
                onApply={view.applyCriteria}
                {...save}
              />
            )}
          />
        }
      />
      {board && capabilities.canCreateCard ? (
        <ItemCreateHost
          open={createOpen}
          onOpenChange={setCreateOpen}
          workspaceId={workspaceId}
          board={board}
          groups={kanban.groups}
          defaultGroupId={kanban.groups[0]?.id ?? ""}
          defaultStatusId={null}
          defaultStartDay={anchorDay}
          defaultDueDay={anchorDay}
          currentUserId={currentUserId}
        />
      ) : null}
      {selectedItemId && board ? (
        <KanbanSelectedItem
          board={board}
          cardId={selectedItemId}
          capabilities={capabilities}
          departmentMark={departmentMark}
          groups={sheetGroups}
          onClose={() => onSelectedItemChange?.(undefined, { replace: true })}
          onRestoreFocus={restoreFocus}
          onCopyLink={sheet.onCopyLink}
          onMoveToGroup={sheet.onMoveToGroup}
          onUpdateFieldValue={sheet.onUpdateFieldValue}
          onDuplicate={sheet.onDuplicate}
        />
      ) : null}
    </div>
  );
}
