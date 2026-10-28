import {
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
} from "react";
import { toast } from "sonner";
import { useLocale } from "@notrelix/i18n";
import {
  dropUnknownFilterIds,
  useBoardAccess,
  useBoardKanban,
  useBoardPlanItems,
  useCreateDatedItem,
  useUpdateCard,
} from "@notrelix/work-management-state";
import { type Card } from "@notrelix/work-management-core";
import { ItemCreateHost } from "../../item-create/item-create-host";
import { BoardHeaderDataContext } from "../../board-layout/board-header-data-context";
import type {
  ItemSheetGroup,
  ItemSheetMark,
} from "../../card-detail/task-detail-types";
import { boardCapabilities } from "../../shared/board-capabilities";
import {
  HANOI_ZONES,
  runtimeZone,
  useNow,
  zoneOffset,
} from "../../shared/board-clock";
import { calendarParts } from "../../shared/item-display";
import { useItemSheetActions } from "../../shared/use-item-sheet-actions";
import {
  isKanbanCardOverdue,
  parseKanbanFilter,
  serializeKanbanFilter,
  type KanbanFilterState,
} from "../kanban/kanban-filter";
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
  diffDays,
  toDayKey,
  type CalendarDateBasis,
  type CalendarEntry,
  type CalendarRange,
  type DayKey,
} from "./calendar-model";
import { useCalendarCopy } from "./use-calendar-copy";

/** The hour a due date gets when the Calendar sets one from a day alone. */
const DEFAULT_DUE_HOUR = 17;

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
  filter: filterParam,
  onFilterChange,
  timeZone: timeZoneProp,
  now: nowProp,
}: BoardCalendarViewProps) {
  const copy = useCalendarCopy();
  const header = useContext(BoardHeaderDataContext);
  const rootRef = useRef<HTMLDivElement>(null);
  const { locale } = useLocale();
  const now = useNow(nowProp);
  const timeZone = timeZoneProp ?? runtimeZone();

  const [range, setRange] = useState<CalendarRange>("month");
  const [dateBasis, setDateBasis] = useState<CalendarDateBasis>("span");
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
  const parsedFilter = useMemo(
    () => parseKanbanFilter(filterValue),
    [filterValue],
  );
  const isOverdue = useCallback(
    (card: Card) => isKanbanCardOverdue(card, now, timeZone),
    [now, timeZone],
  );

  const kanban = useBoardKanban(boardId, workspaceId, {
    filter: parsedFilter,
    isOverdue,
  });
  const { access } = useBoardAccess(boardId, workspaceId);
  const capabilities = boardCapabilities(access?.myLevel);
  const canEdit = capabilities.canMoveCard && capabilities.canCreateCard;

  const { board, filterChoices } = kanban;
  // The filter as it applies: ids the board no longer has are dropped, and
  // the route parameter is rewritten (history replace) to match.
  const filter = useMemo(
    () =>
      board ? dropUnknownFilterIds(parsedFilter, filterChoices) : parsedFilter,
    [board, parsedFilter, filterChoices],
  );
  useEffect(() => {
    if (!board || filterValue === undefined) return;
    const canonical = serializeKanbanFilter(filter);
    if (canonical !== filterValue) {
      setFilterValue(canonical || undefined, { replace: true });
    }
  }, [board, filter, filterValue, setFilterValue]);

  // Phases and milestones belong to nobody: any filter hides them with the
  // items it filters out, as Final-Calendar's empty state shows.
  const planItems = useBoardPlanItems(boardId, workspaceId).data?.items;
  const shownPlanItems = serializeKanbanFilter(filter) ? undefined : planItems;
  const built = useMemo(
    () =>
      buildCalendarEntries({
        lanes: kanban.lanes,
        today,
        copy,
        timeZone,
        planItems: shownPlanItems,
      }),
    [kanban.lanes, today, copy, timeZone, shownPlanItems],
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
  const createItem = useCreateDatedItem(boardId, workspaceId, {
    onError: (_error, input) =>
      toast.error(fillCopy(copy.quickCreate.failed, { title: input.title })),
  });

  const reportMoveFailure = (title: string) =>
    toast.error(fillCopy(copy.reschedule.failed, { title }), {
      id: `calendar-write-${title}`,
    });

  const handleReschedule = (entry: CalendarEntry, toDay: DayKey) => {
    const card = cardsById.get(entry.id);
    const due = card ? cardDue(card) : undefined;
    if (!card || !due || !canEdit) return;
    // The span keeps its length: the start moves by as many days as the due.
    const shift = diffDays(entry.endDay, toDay);
    if (shift === 0) return;
    updateCard.mutate(
      {
        cardId: card.id,
        patch: {
          dueDate: shiftInstant(due, shift),
          ...(card.startDate
            ? { startDate: shiftInstant(card.startDate, shift) }
            : {}),
        },
      },
      { onError: () => reportMoveFailure(card.title) },
    );
  };

  const handleSetDue = (id: string, day: DayKey) => {
    const card = cardsById.get(id);
    if (!card || !canEdit) return;
    updateCard.mutate(
      {
        cardId: id,
        patch: {
          dueDate: zonedInstant(day, DEFAULT_DUE_HOUR, 0, timeZone),
        },
      },
      { onError: () => reportMoveFailure(card.title) },
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
        entries={built.entries}
        undatedTotal={undatedTotal}
        undated={built.undated}
        filter={filter}
        filterActive={kanban.filterActive}
        filterOptions={{
          statuses: [...filterChoices.statuses],
          assignees: [...filterChoices.assignees],
          labels: [...filterChoices.labels],
        }}
        onFilterChange={(next: KanbanFilterState) =>
          setFilterValue(serializeKanbanFilter(next) || undefined)
        }
        savedFilters={
          <SavedFiltersControl
            workspaceId={workspaceId}
            boardId={boardId}
            filter={filterValue}
            canEdit={capabilities.canEditFields}
            onApply={(criteria) => setFilterValue(criteria.filter || undefined)}
          />
        }
        hiddenCount={Math.max(0, kanban.totalCount - kanban.visibleCount)}
        canEdit={canEdit}
        onOpenItem={(id) => onSelectedItemChange?.(id)}
        onReschedule={handleReschedule}
        onSetDue={handleSetDue}
        onCreateItem={handleCreate}
        onAddItem={setCreateDay}
      />
      {board && capabilities.canCreateCard ? (
        <ItemCreateHost
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
          onArchive={sheet.onArchive}
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
