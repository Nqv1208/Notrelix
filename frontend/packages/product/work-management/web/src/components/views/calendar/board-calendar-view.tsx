import { useCallback, useMemo, useRef, useState } from "react";
import { toast } from "sonner";
import { useLocale } from "@notrelix/i18n";
import {
  useBoardKanban,
  useBoardPlanItems,
  useBoardView,
  useCreateDatedItem,
  useUpdateCard,
} from "@notrelix/work-management-state";
import {
  resolveCalendarDateField,
  serializeCompoundFilter,
  type Card,
} from "@notrelix/work-management-core";
import { CardCreatePopup } from "../../card-popup/create/card-create-popup";
import type { ItemSheetGroup } from "../../card-detail/task-detail-types";
import { useBoardCapabilities } from "../../shared/board-capabilities";
import {
  HANOI_ZONES,
  runtimeZone,
  useNow,
  zoneOffset,
} from "../../shared/board-clock";
import { calendarParts } from "../../shared/item-display";
import { useItemSheetActions } from "../../shared/use-item-sheet-actions";
import {
  emptyKanbanFilter,
  isKanbanCardOverdue,
} from "../kanban/kanban-filter";
import { useBoardViewState } from "../../filters/use-board-view-state";
import { ViewStateBar } from "../../filters/view-state-bar";
import { KanbanSelectedItem } from "../kanban/kanban-view";
import { BoardCalendarSurface } from "./board-calendar-surface";
import { SavedFiltersControl } from "../../saved-filters/saved-filters-control";
import {
  buildCalendarEntries,
  cardDue,
  shiftInstant,
  zonedInstant,
} from "./calendar-entries";
import {
  addDays,
  diffDays,
  formatDayMonth,
  toDayKey,
  calendarDateFields,
  resolveCalendarDateBasis,
  type CalendarDateBasis,
  type CalendarEntry,
  type CalendarRange,
  type DayKey,
} from "./calendar-model";
import { useCalendarCopy } from "./use-calendar-copy";
import { useLimitMessage } from "../../shared/use-limit-message";

/** The hour a due date gets when the Calendar sets one from a day alone. */
const DEFAULT_DUE_HOUR = 17;
const DEFAULT_START_HOUR = 8;
let writeToasts = 0;

type ParamChange = (
  value: string | undefined,
  options?: { replace?: boolean },
) => void;

export interface BoardCalendarViewProps {
  boardId: string;
  workspaceId: string;
  /** The item the board route's `item` parameter names; its sheet opens. */
  selectedItemId?: string;
  /** The signed-in member (from the host's session): offers "Giao cho tôi". */
  currentUserId?: string | null;
  /**
   * Opening passes the id (push). Closing passes `replace` so Back does not
   * step through every item.
   */
  onSelectedItemChange?: (
    id: string | undefined,
    options?: { replace?: boolean },
  ) => void;
  /** Opens an item of any board (a link or a dependency of the open item); absent: only this board's items open. */
  onOpenItem?: (boardId: string, itemId: string) => void;
  /** The route `filter` parameter (`serializeKanbanFilter` form), shared with the Kanban. */
  filter?: string;
  /**
   * Writes the route `filter` parameter; `undefined` removes it. Without it
   * the view keeps the filter itself (stories, isolated tests).
   */
  onFilterChange?: ParamChange;
  /** The IANA time zone of the days; the runtime zone when omitted. */
  timeZone?: string;
  /** Injected clock; the system clock, once a minute, when omitted. */
  now?: Date;
}

/**
 * The Calendar of a board: dated items on their due day (over their span
 * when they have a start), a month, week and day range, the route filter
 * shared with the Kanban, and the writes a day needs. What the viewer may do
 * comes from the board access read: a commenter and a viewer cannot drag,
 * add or set a due date.
 */
export function BoardCalendarView({
  boardId,
  workspaceId,
  selectedItemId,
  currentUserId,
  onSelectedItemChange,
  onOpenItem,
  filter: filterParam,
  onFilterChange,
  timeZone: timeZoneProp,
  now: nowProp,
}: BoardCalendarViewProps) {
  const copy = useCalendarCopy();
  const rootRef = useRef<HTMLDivElement>(null);
  const { locale } = useLocale();
  const now = useNow(nowProp);
  const timeZone = timeZoneProp ?? runtimeZone();

  const [range, setRange] = useState<CalendarRange>("month");
  // The basis is the shared view's (`calendarDateField`); a member who cannot
  // edit the view picks one for this visit only.
  const { viewConfig, updateViewConfig } = useBoardView(boardId, workspaceId);
  const [localBasis, setLocalBasis] = useState<CalendarDateBasis | null>(null);
  const todayParts = calendarParts(now, timeZone);
  const today = toDayKey(todayParts.year, todayParts.month, todayParts.day);
  const nowMinutes = todayParts.hour * 60 + todayParts.minute;
  const [anchorDay, setAnchorDay] = useState<DayKey>(today);
  const [createDay, setCreateDay] = useState<DayKey | null>(null);

  const [localFilter, setLocalFilter] = useState<string | undefined>();
  const filterValue = onFilterChange ? filterParam : localFilter;
  const setFilterValue: ParamChange = useCallback(
    (value, options) => {
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
  const isOverdue = useCallback(
    (card: Card) => isKanbanCardOverdue(card, now, timeZone),
    [now, timeZone],
  );

  const kanban = useBoardKanban(boardId, workspaceId, {
    filter: noLegacyFilter,
    view: kanbanView,
    isOverdue,
    allCards: true,
  });
  const { capabilities } = useBoardCapabilities(boardId, workspaceId);
  const canEdit = capabilities.canMoveCard && capabilities.canCreateCard;

  const { board } = kanban;
  const dateFields = useMemo(
    () => calendarDateFields(kanban.filterFields),
    [kanban.filterFields],
  );
  const savedBasis = resolveCalendarDateField(viewConfig);
  const dateBasis = resolveCalendarDateBasis(
    localBasis ?? savedBasis,
    dateFields,
  );
  const setDateBasis = (basis: CalendarDateBasis) => {
    if (capabilities.canEditFields) {
      setLocalBasis(null);
      updateViewConfig({ calendarDateField: basis });
    } else setLocalBasis(basis);
  };

  // Phases and milestones belong to nobody: any filter hides them with the
  // items it filters out, as Final-Calendar's empty state shows.
  const planItems = useBoardPlanItems(boardId, workspaceId).data?.items;
  const shownPlanItems =
    view.applied.filter.conditions.length > 0 ? undefined : planItems;
  const built = useMemo(
    () =>
      buildCalendarEntries({
        lanes: kanban.lanes,
        today,
        copy,
        timeZone,
        planItems: shownPlanItems,
        dateFieldId:
          dateBasis === "span" || dateBasis === "due" ? undefined : dateBasis,
      }),
    [kanban.lanes, today, copy, timeZone, shownPlanItems, dateBasis],
  );
  // A refused write keeps the new dates on screen until it is retried or the
  // item is reloaded: never a silent revert.
  const [unsaved, setUnsaved] = useState<
    ReadonlyMap<string, { startDay: DayKey; endDay: DayKey }>
  >(new Map());
  const setUnsavedDays = (
    id: string,
    days: { startDay: DayKey; endDay: DayKey } | null,
  ) =>
    setUnsaved((current) => {
      const next = new Map(current);
      if (days) next.set(id, days);
      else next.delete(id);
      return next;
    });

  // Dates a refused write left on screen replace the stored ones.
  const shownEntries = useMemo(
    () =>
      unsaved.size === 0
        ? built.entries
        : built.entries.map((entry) => {
            const days = unsaved.get(entry.id);
            return days ? { ...entry, ...days } : entry;
          }),
    [built.entries, unsaved],
  );
  const undatedTotal = useMemo(
    () =>
      buildCalendarEntries({ lanes: kanban.allLanes, today, copy, timeZone })
        .undated.length,
    [kanban.allLanes, today, copy, timeZone],
  );
  const cardsById = useMemo(
    () =>
      new Map(
        kanban.allLanes.flatMap((lane) =>
          lane.cards.map((card) => [card.id, card] as const),
        ),
      ),
    [kanban.allLanes],
  );

  const updateCard = useUpdateCard(boardId, workspaceId);
  const limitMessage = useLimitMessage();
  const createItem = useCreateDatedItem(boardId, workspaceId, {
    // Over the board's published item limit (D78.8) the limit is named instead.
    onError: (error, input) =>
      toast.error(
        limitMessage(error) ??
          fillCopy(copy.quickCreate.failed, { title: input.title }),
      ),
  });

  type DatePatch = { startDate: string | null; dueDate: string | null };
  const nameOf = (card: Card) => card.itemKey ?? card.title;
  const isConflict = (error: unknown) =>
    (error as { kind?: unknown } | null)?.kind === "conflict" ||
    (error as { status?: number } | null)?.status === 409;
  /**
   * Writes an item's dates. Success says what moved and offers Hoàn tác (the
   * previous dates); failure keeps the new position with Thử lại, a version
   * conflict offers Tải lại instead.
   */
  const writeDates = (
    card: Card,
    patch: DatePatch,
    previous: DatePatch,
    shown: { startDay: DayKey; endDay: DayKey },
    message: string,
    isUndo = false,
  ) => {
    const text = copy.scheduleToast;
    // One toast per attempt: pressing Hoàn tác or Thử lại makes Sonner start
    // removing the toast it sits on, and a result toast that reuses its id
    // inherits that removal and never shows.
    const toastId = `calendar-write-${card.id}-${++writeToasts}`;
    updateCard.mutate(
      { cardId: card.id, patch },
      {
        onSuccess: () => {
          setUnsavedDays(card.id, null);
          if (isUndo) {
            toast.success(text.undone, { id: toastId });
            return;
          }
          toast.success(message, {
            id: toastId,
            duration: 8000,
            action: {
              label: text.undo,
              onClick: () =>
                writeDates(card, previous, patch, shown, message, true),
            },
          });
        },
        onError: (error: unknown) => {
          if (isConflict(error)) {
            setUnsavedDays(card.id, null);
            // The refetch brings the other person's version, which says who changed it.
            void Promise.resolve(kanban.refetch()).then(() => {
              const actor = kanban.lastEditorOf(card.id);
              toast.error(
                actor
                  ? fillCopy(text.conflictBy, { actor, key: nameOf(card) })
                  : fillCopy(text.conflict, { key: nameOf(card) }),
                {
                  id: toastId,
                  description: text.conflictNote,
                  action: {
                    label: text.reload,
                    onClick: () => void kanban.refetch(),
                  },
                },
              );
            });
            return;
          }
          setUnsavedDays(card.id, shown);
          toast.error(fillCopy(text.failed, { key: nameOf(card) }), {
            id: toastId,
            description: text.failedNote,
            action: {
              label: text.retry,
              onClick: () =>
                writeDates(card, patch, previous, shown, message, isUndo),
            },
          });
        },
      },
    );
  };

  const datesOf = (card: Card): DatePatch => ({
    startDate: card.startDate ?? null,
    dueDate: cardDue(card) ?? null,
  });

  /** New start and due days of an item that has a due (a move or a resize). */
  const handleChangeDates = (
    entry: CalendarEntry,
    days: { startDay: DayKey; dueDay: DayKey },
  ) => {
    const card = cardsById.get(entry.id);
    const due = card ? cardDue(card) : undefined;
    if (!card || !due || !canEdit) return;
    if (days.startDay === entry.startDay && days.dueDay === entry.endDay)
      return;
    const startShift = diffDays(entry.startDay, days.startDay);
    const dueShift = diffDays(entry.endDay, days.dueDay);
    const patch: DatePatch = {
      dueDate: dueShift === 0 ? due : shiftInstant(due, dueShift),
      startDate: card.startDate
        ? startShift === 0
          ? card.startDate
          : shiftInstant(card.startDate, startShift)
        : days.startDay === days.dueDay
          ? null
          : zonedInstant(days.startDay, DEFAULT_START_HOUR, 0, timeZone),
    };
    writeDates(
      card,
      patch,
      datesOf(card),
      { startDay: days.startDay, endDay: days.dueDay },
      fillCopy(copy.scheduleToast.changed, {
        key: nameOf(card),
        from: formatDayMonth(days.startDay),
        to: formatDayMonth(days.dueDay),
      }),
    );
  };

  /** A drag of the body or an Alt-arrow step: the span keeps its length. */
  const handleReschedule = (entry: CalendarEntry, toDay: DayKey) => {
    if (diffDays(entry.endDay, toDay) === 0) return;
    const shift = diffDays(entry.endDay, toDay);
    handleChangeDates(entry, {
      startDay: addDays(entry.startDay, shift),
      dueDay: toDay,
    });
  };

  /** "Chuyển về Chưa lên lịch": both dates are cleared. */
  const handleUnschedule = (entry: CalendarEntry) => {
    const card = cardsById.get(entry.id);
    if (!card || !canEdit) return;
    writeDates(
      card,
      { startDate: null, dueDate: null },
      datesOf(card),
      { startDay: entry.startDay, endDay: entry.endDay },
      fillCopy(copy.scheduleToast.unscheduled, { key: nameOf(card) }),
    );
  };

  /** An undated item gets one day: start and due are both that day. */
  const handleSetDue = (id: string, day: DayKey) => {
    const card = cardsById.get(id);
    if (!card || !canEdit) return;
    writeDates(
      card,
      {
        startDate: zonedInstant(day, DEFAULT_START_HOUR, 0, timeZone),
        dueDate: zonedInstant(day, DEFAULT_DUE_HOUR, 0, timeZone),
      },
      datesOf(card),
      { startDay: day, endDay: day },
      fillCopy(copy.scheduleToast.scheduled, {
        key: nameOf(card),
        day: formatDayMonth(day),
      }),
    );
  };

  const handleCreate = (title: string, day: DayKey) => {
    const listId = [...kanban.groups].sort((a, b) => a.position - b.position)[0]
      ?.id;
    if (!listId || !canEdit) return;
    createItem.mutate({
      listId,
      title,
      dueDate: zonedInstant(day, DEFAULT_DUE_HOUR, 0, timeZone),
    });
  };

  const sheet = useItemSheetActions({ boardId, workspaceId, kanban });
  const restoreFocus = (cardId: string) => {
    const opener = rootRef.current?.querySelector<HTMLElement>(
      `[data-calendar-event="${cardId}"]`,
    );
    (opener ?? rootRef.current)?.focus();
  };

  if (kanban.isLoading) return null;

  const zoneName =
    timeZone && HANOI_ZONES.includes(timeZone)
      ? copy.zone.hanoi
      : copy.zone.local;
  const zoneLabel = fillCopy(copy.zone.label, {
    zone: zoneName,
    offset: zoneOffset(now, timeZone),
  });

  const sheetGroups: ItemSheetGroup[] = [...kanban.groups]
    .sort((a, b) => a.position - b.position)
    .map((group) => ({ id: group.id, title: group.title }));

  return (
    <div
      ref={rootRef}
      tabIndex={-1}
      className="relative h-full focus:outline-hidden"
    >
      <BoardCalendarSurface
        copy={copy}
        locale={locale}
        range={range}
        onRangeChange={setRange}
        anchorDay={anchorDay}
        onAnchorChange={setAnchorDay}
        today={today}
        nowMinutes={nowMinutes}
        zoneLabel={zoneLabel}
        zoneName={zoneName}
        dateBasis={dateBasis}
        onDateBasisChange={setDateBasis}
        dateFields={dateFields}
        entries={shownEntries}
        undatedTotal={undatedTotal}
        undated={built.undated}
        filterActive={kanban.filterActive}
        onClearFilter={() =>
          view.setFilter({ ...view.state.filter, conditions: [] })
        }
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
        hiddenCount={Math.max(0, kanban.totalCount - kanban.visibleCount)}
        canEdit={canEdit}
        onOpenItem={(id) => onSelectedItemChange?.(id)}
        onReschedule={handleReschedule}
        onSetDue={handleSetDue}
        onChangeDates={handleChangeDates}
        onUnschedule={handleUnschedule}
        onCreateItem={handleCreate}
        onAddItem={setCreateDay}
      />
      {board && capabilities.canCreateCard ? (
        <CardCreatePopup
          open={createDay !== null}
          onOpenChange={(next) => {
            if (!next) setCreateDay(null);
          }}
          workspaceId={workspaceId}
          board={board}
          groups={kanban.groups}
          defaultGroupId={kanban.groups[0]?.id ?? ""}
          defaultStatusId={null}
          defaultDueDay={createDay ?? undefined}
          currentUserId={currentUserId}
          onOpenItem={(id) => onSelectedItemChange?.(id)}
        />
      ) : null}
      {selectedItemId && board ? (
        <KanbanSelectedItem
          board={board}
          onOpenItem={(targetBoardId, itemId) => {
            if (onOpenItem) onOpenItem(targetBoardId, itemId);
            else if (targetBoardId === boardId) onSelectedItemChange?.(itemId);
          }}
          cardId={selectedItemId}
          capabilities={capabilities}
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

function fillCopy(
  message: string,
  values: Readonly<Record<string, string | number>>,
) {
  return message.replace(/\{\{(\w+)\}\}/g, (match, key: string) =>
    key in values ? String(values[key]) : match,
  );
}
