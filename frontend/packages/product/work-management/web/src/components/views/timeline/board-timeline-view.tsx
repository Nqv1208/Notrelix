import { useContext, useMemo, useRef, useState } from "react";
import { toast } from "sonner";
import { useLocale } from "@notrelix/i18n";
import {
  useBoardAccess,
  useBoardKanban,
  useUpdateCard,
} from "@notrelix/work-management-state";
import type { Card, WorkStatusCategory } from "@notrelix/work-management-core";
import { BoardHeaderDataContext } from "../../board-layout/board-header-data-context";
import type {
  ItemSheetGroup,
  ItemSheetMark,
} from "../../card-detail/task-detail-types";
import { boardCapabilities } from "../../shared/board-capabilities";
import { HANOI_ZONES, runtimeZone, useNow } from "../../shared/board-clock";
import { calendarParts, identityMarkClass } from "../../shared/item-display";
import { useItemSheetActions } from "../../shared/use-item-sheet-actions";
import { cardDue, dayOf, shiftInstant } from "../calendar/calendar-entries";
import { toDayKey, type DayKey } from "../calendar/calendar-model";
import { emptyKanbanFilter } from "../kanban/kanban-filter";
import { KanbanSelectedItem } from "../kanban/kanban-view";
import { buildCardLink } from "../kanban/kanban-view-helpers";
import { BoardTimelineSurface } from "./board-timeline-surface";
import type {
  TimelineGroup,
  TimelineItem,
  TimelineLink,
  TimelineScale,
  TimelineTone,
} from "./timeline-model";
import { fill } from "./timeline-model";
import { useTimelineCopy } from "./use-timeline-copy";

const NO_LINKS: readonly TimelineLink[] = [];
const NEVER_OVERDUE = () => false;

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

function percentOf(card: Card, tone: TimelineTone): number {
  if (tone === "done") return 100;
  const progress = card.childProgress;
  if (!progress || progress.total <= 0) return 0;
  return Math.round((progress.done / progress.total) * 100);
}

/**
 * The Timeline of a board: dated items as bars from start to due on a day,
 * week or month scale, grouped by the board's groups, with the item side
 * sheet shared with the other views. Dependencies and the critical path have
 * no producer in the board read, so the surface shows none. What the viewer
 * may do comes from the board access read: only an editor drags a bar.
 */
export function BoardTimelineView({
  boardId,
  workspaceId,
  selectedItemId,
  onSelectedItemChange,
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

  const filter = useMemo(() => emptyKanbanFilter(), []);
  const kanban = useBoardKanban(boardId, workspaceId, {
    filter,
    isOverdue: NEVER_OVERDUE,
  });
  const { access } = useBoardAccess(boardId, workspaceId);
  const capabilities = boardCapabilities(access?.myLevel);
  const canEdit = capabilities.canMoveCard && capabilities.canCreateCard;
  const updateCard = useUpdateCard(boardId, workspaceId);
  const sheet = useItemSheetActions({ boardId, workspaceId, kanban });

  const { board } = kanban;
  const { groups, undated, cardsById } = useMemo(() => {
    const byId = new Map<string, Card>();
    const byGroup = new Map<string, TimelineItem[]>();
    let undatedCount = 0;
    for (const lane of kanban.allLanes) {
      const tone = toneOf(lane.category);
      for (const card of lane.cards) {
        byId.set(card.id, card);
        const due = dayOf(cardDue(card), timeZone);
        const start = dayOf(card.startDate, timeZone);
        if (!due && !start) {
          undatedCount += 1;
          continue;
        }
        const person = card.members[0];
        const item: TimelineItem = {
          id: card.id,
          kind: "item",
          itemKey: card.itemKey,
          title: card.title,
          tone,
          startDay: start?.day ?? null,
          dueDay: due?.day ?? null,
          percent: percentOf(card, tone),
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
        const list = byGroup.get(card.listId) ?? [];
        list.push(item);
        byGroup.set(card.listId, list);
      }
    }
    const built: TimelineGroup[] = [...kanban.groups]
      .sort((a, b) => a.position - b.position)
      .map((group) => ({
        id: group.id,
        title: group.title,
        items: (byGroup.get(group.id) ?? []).sort(
          (a, b) =>
            (byId.get(a.id)?.position ?? 0) - (byId.get(b.id)?.position ?? 0),
        ),
      }));
    return { groups: built, undated: undatedCount, cardsById: byId };
  }, [kanban.allLanes, kanban.groups, timeZone]);

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

  const restoreFocus = (cardId: string) => {
    const opener = rootRef.current?.querySelector<HTMLElement>(
      `[data-row-id="${cardId}"] a`,
    );
    (opener ?? rootRef.current)?.focus();
  };

  if (kanban.isLoading) return null;

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
        links={NO_LINKS}
        undatedCount={undated}
        onOpenUndated={onOpenCalendar}
        canEdit={canEdit}
        onOpenItem={(id) => onSelectedItemChange?.(id)}
        onReschedule={handleReschedule}
      />
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
