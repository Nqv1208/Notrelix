import { useId, useState } from "react";
import { useSortable } from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import {
  Ban,
  Calendar,
  ListChecks,
  MessageCircle,
  Paperclip,
  SquareCheck,
} from "lucide-react";
import { cn } from "@notrelix/ui-web";
import type { Card, CardMember } from "@notrelix/work-management-core";
import {
  fillCopy,
  kanbanCardCopy,
  pluralCopy,
  type KanbanCardCopy,
} from "./kanban-card.copy";
import {
  KanbanCardQuickActions,
  isAssignedTo,
  type KanbanCardActions,
} from "./kanban-card-quick-actions";
import { prefersReducedMotion } from "./kanban-chrome";
import { KANBAN_CARD_TYPE } from "./kanban-collision";
import type { KanbanCardSelection } from "./kanban-selection";
import { TableCheckbox } from "../table/table-parts";
import type { KanbanMoveDirection } from "./kanban-dnd";
import {
  PRIORITY_TONE_CLASS,
  PriorityGlyph,
} from "../../shared/priority-glyph";
import {
  ITEM_LABEL_TONE_CLASS,
  formatItemDate,
  identityMarkClass,
  itemDueState,
  itemLabelTone,
  type ItemDueState,
} from "../../shared/item-display";

const defaultCopy = kanbanCardCopy("en");

function markFor(member: CardMember): string {
  const initials = member.initials.trim() || Array.from(member.name)[0] || "?";
  return initials.toUpperCase();
}

const DUE_CLASS: Readonly<Record<ItemDueState["kind"], string>> = {
  overdue: "bg-danger-subtle font-semibold text-danger-subtle-foreground",
  today: "bg-warning-subtle font-semibold text-warning-subtle-foreground",
  date: "-ml-1.5 font-medium text-muted-foreground",
};

/** A count as shown on the face: 99+ above 99. */
function countText(count: number, copy: KanbanCardCopy): string {
  return count > 99 ? copy.overflowCount : String(count);
}

const ALT_ARROW: Readonly<Record<string, KanbanMoveDirection | undefined>> = {
  ArrowLeft: "left",
  ArrowRight: "right",
  ArrowUp: "up",
  ArrowDown: "down",
};

const CARD_CLASS =
  "relative flex flex-col gap-2 rounded-[11px] bg-popover px-3 pt-[11px] pb-2.5 dark:bg-card";

/** What a card shows, worked out once for its body and its open control. */
function cardParts(
  card: Card,
  copy: KanbanCardCopy,
  now: Date | undefined,
  timeZone: string | undefined,
  fallbackNow: Date,
) {
  const reference = now ?? fallbackNow;
  const isDone = card.statusCategory === "done" || Boolean(card.completedAt);
  const due = isDone
    ? null
    : itemDueState(card.dueAt ?? card.dueDate, reference, timeZone);
  const dueText =
    due?.kind === "overdue"
      ? pluralCopy(
          { one: copy.overdueDays_one, other: copy.overdueDays_other },
          due.days,
        )
      : due?.kind === "today"
        ? copy.dueToday
        : (due?.text ?? null);

  // Up to two labels, then "+N"; the first one still names the card's tone.
  const allLabels = card.labels ?? [];
  const label = allLabels[0];
  const labelTone = label ? itemLabelTone(label) : null;
  const shownLabels = allLabels.slice(0, 2);
  const moreLabels = Math.max(allLabels.length - shownLabels.length, 0);
  // A board with a key shows "ERP-142"; one without shows its number as "#142".
  const rawKey = card.itemKey?.trim() || null;
  const itemKey = rawKey && /^\d+$/.test(rawKey) ? `#${rawKey}` : rawKey;
  const commentCount = Math.max(card._count?.comments ?? 0, 0);
  const fileCount = Math.max(card._count?.attachments ?? 0, 0);
  const blockerCount = Math.max(card.openBlockerCount ?? 0, 0);
  const checklist =
    card.checklistProgress && card.checklistProgress.total > 0
      ? card.checklistProgress
      : null;

  const progress =
    card.childProgress && card.childProgress.total > 0
      ? card.childProgress
      : null;
  const progressPercent = progress
    ? Math.max(
        4,
        Math.min(100, Math.round((progress.done / progress.total) * 100)),
      )
    : 0;

  const priority = card.priority ?? null;

  // Row 2 never wraps. From four chips on it is crowded: only the due date,
  // no calendar icon, "Bị chặn" as an icon, and the avatars collapse to one
  // plus "+N". The start-to-due range needs three chips or fewer.
  const chipCount = [
    dueText,
    checklist,
    commentCount,
    fileCount,
    blockerCount,
  ].filter(Boolean).length;
  const crowded = chipCount >= 4;
  const startText = card.startDate
    ? formatItemDate(card.startDate, reference, timeZone)
    : null;
  const dueRange =
    !crowded && dueText && startText && due?.kind === "date"
      ? `${startText} → ${dueText}`
      : null;
  const dueLabel =
    dueText && startText && due
      ? fillCopy(copy.startAndDue, { start: startText, due: dueText })
      : null;

  const assignee = card.members?.[0];
  const shownAssignees = (card.members ?? []).slice(0, crowded ? 1 : 2);
  const moreAssignees = Math.max((card.members?.length ?? 0) - 1, 0);
  const hiddenAssignees = Math.max(
    (card.members?.length ?? 0) - shownAssignees.length,
    0,
  );
  const assigneeLabel = assignee
    ? moreAssignees > 0
      ? fillCopy(copy.assignedToMore, {
          name: assignee.name,
          count: moreAssignees,
        })
      : fillCopy(copy.assignedTo, { name: assignee.name })
    : "";

  return {
    isDone,
    due,
    dueText,
    label,
    labelTone,
    shownLabels,
    moreLabels,
    itemKey,
    progress,
    progressPercent,
    priority,
    assignee,
    shownAssignees,
    hiddenAssignees,
    assigneeLabel,
    commentCount,
    fileCount,
    blockerCount,
    checklist,
    crowded,
    dueRange,
    dueLabel,
    hasTop: shownLabels.length > 0 || itemKey !== null || priority !== null,
    hasMeta:
      dueText !== null ||
      checklist !== null ||
      commentCount > 0 ||
      fileCount > 0 ||
      blockerCount > 0 ||
      Boolean(assignee),
  };
}

/**
 * The inside of a card: label chip and key, title, child-item progress, due
 * wording, priority (urgent and high only) and the first assignee's avatar.
 * Shared by the card in its lane and by the copy that follows the pointer
 * while the card is dragged, so both look the same.
 */
function KanbanCardContent({
  card,
  copy,
  parts,
  idPrefix,
}: {
  card: Card;
  copy: KanbanCardCopy;
  parts: ReturnType<typeof cardParts>;
  idPrefix: string;
}) {
  const {
    isDone,
    due,
    dueText,
    shownLabels,
    moreLabels,
    itemKey,
    progress,
    progressPercent,
    priority,
    assignee,
    shownAssignees,
    hiddenAssignees,
    assigneeLabel,
    commentCount,
    fileCount,
    blockerCount,
    checklist,
    crowded,
    dueRange,
    dueLabel,
    hasTop,
    hasMeta,
  } = parts;
  const compactBlocked = crowded || due?.kind === "overdue";
  return (
    <>
      {hasTop ? (
        <div
          id={`${idPrefix}-top`}
          className="flex min-h-[22px] min-w-0 items-center gap-1.5 text-metadata text-meta-foreground"
        >
          {shownLabels.map((label, index) => {
            const tone = itemLabelTone(label);
            return (
              <span key={label.id} className="inline-flex min-w-0">
                {index === 0 ? (
                  <span className="sr-only">{copy.label} </span>
                ) : null}
                <span
                  data-label-tone={tone ?? undefined}
                  className={cn(
                    "inline-flex h-5 min-w-0 items-center truncate rounded-[5px] px-[7px] text-micro font-semibold",
                    tone
                      ? ITEM_LABEL_TONE_CLASS[tone]
                      : "bg-state-hover-strong text-muted-foreground",
                  )}
                >
                  {label.name}
                </span>
              </span>
            );
          })}
          {moreLabels > 0 ? (
            <span
              title={fillCopy(copy.moreLabels, { count: moreLabels })}
              className="inline-flex h-5 shrink-0 items-center rounded-[5px] bg-state-hover-strong px-[7px] text-micro font-semibold text-muted-foreground"
            >
              +{moreLabels}
            </span>
          ) : null}
          {priority ? (
            <span
              data-priority={priority}
              title={`${copy.priorityPrefix} ${copy.priority[priority]}`}
              className={cn(
                "ml-auto inline-flex shrink-0",
                PRIORITY_TONE_CLASS[priority],
              )}
            >
              <PriorityGlyph level={priority} />
              <span className="sr-only">{copy.priorityPrefix} </span>
              <span className="sr-only">{copy.priority[priority]}</span>
            </span>
          ) : null}
          {itemKey ? (
            <span
              className={cn(
                "shrink-0 font-mono text-metadata leading-[1.45]",
                priority ? null : "ml-auto",
              )}
            >
              {itemKey}
            </span>
          ) : null}
        </div>
      ) : null}

      <h3
        className={cn(
          "line-clamp-3 break-words text-nav leading-[1.4]",
          isDone ? "text-muted-foreground" : "text-foreground",
        )}
      >
        {card.title}
      </h3>

      {progress ? (
        <div
          data-child-progress=""
          className="flex items-center gap-[7px] text-micro tabular-nums text-meta-foreground"
        >
          <ListChecks className="size-3.25 shrink-0" aria-hidden="true" />
          <span
            aria-hidden="true"
            className="block h-1.25 flex-1 overflow-hidden rounded-[3px] bg-state-hover-strong"
          >
            <span
              data-progress-fill=""
              className="block h-full rounded-[3px] bg-primary"
              style={{ width: `${progressPercent}%` }}
            />
          </span>
          <span aria-hidden="true">
            {progress.done}/{progress.total}
          </span>
          <span id={`${idPrefix}-progress`} className="sr-only">
            {fillCopy(copy.childProgress, {
              done: progress.done,
              total: progress.total,
            })}
          </span>
        </div>
      ) : null}

      {hasMeta ? (
        <div
          id={`${idPrefix}-meta`}
          data-crowded={crowded ? "" : undefined}
          className="flex min-h-6 min-w-0 flex-nowrap items-center gap-1 overflow-hidden text-micro text-meta-foreground"
        >
          {due && dueText ? (
            <span
              data-due={due.kind}
              title={dueLabel ?? undefined}
              className={cn(
                "inline-flex h-5.5 shrink-0 items-center gap-1 rounded-sm px-1 whitespace-nowrap",
                DUE_CLASS[due.kind],
              )}
            >
              {crowded || due.kind === "overdue" ? null : (
                <Calendar className="size-3.25" aria-hidden="true" />
              )}
              {due.kind === "overdue" ? null : (
                <span className="sr-only">{copy.due} </span>
              )}
              {dueRange ?? dueText}
              {dueLabel ? <span className="sr-only"> ({dueLabel})</span> : null}
            </span>
          ) : null}
          {checklist ? (
            <span
              data-checklist-progress=""
              title={fillCopy(copy.checklistProgress, checklist)}
              className={cn(
                "inline-flex h-5 shrink-0 items-center gap-0.75 rounded-[5px] font-semibold tabular-nums",
                crowded
                  ? "px-0.5"
                  : "bg-state-hover-strong px-1 text-muted-foreground",
              )}
            >
              <SquareCheck className="size-3.25" aria-hidden="true" />
              <span aria-hidden="true">
                {checklist.done}/{checklist.total}
              </span>
              <span className="sr-only">
                {fillCopy(copy.checklistProgress, checklist)}
              </span>
            </span>
          ) : null}
          {commentCount > 0 ? (
            <span
              data-comment-count={commentCount}
              className="inline-flex shrink-0 items-center gap-0.75 tabular-nums"
            >
              <MessageCircle aria-hidden="true" className="size-3.25" />
              <span aria-hidden="true">{countText(commentCount, copy)}</span>
              <span className="sr-only">
                {fillCopy(copy.commentCount, { count: commentCount })}
              </span>
            </span>
          ) : null}
          {fileCount > 0 ? (
            <span
              data-file-count={fileCount}
              className="inline-flex shrink-0 items-center gap-0.75 tabular-nums"
            >
              <Paperclip aria-hidden="true" className="size-3.25" />
              <span aria-hidden="true">{countText(fileCount, copy)}</span>
              <span className="sr-only">
                {fillCopy(copy.fileCount, { count: fileCount })}
              </span>
            </span>
          ) : null}
          {blockerCount > 0 ? (
            <span
              data-blocked={blockerCount}
              title={fillCopy(copy.blockedBy, { count: blockerCount })}
              className={cn(
                "inline-flex h-5 min-w-5 shrink-0 items-center justify-center gap-1 rounded-[5px] bg-danger-subtle text-micro font-semibold text-danger-subtle-foreground",
                compactBlocked ? "px-0" : "px-1",
              )}
            >
              <Ban className="size-3.25" aria-hidden="true" />
              {compactBlocked ? null : (
                <span aria-hidden="true">{copy.blocked}</span>
              )}
              <span className="sr-only">
                {fillCopy(copy.blockedBy, { count: blockerCount })}
              </span>
            </span>
          ) : null}
          {assignee ? (
            <span
              role="group"
              aria-label={assigneeLabel}
              className="ml-auto flex shrink-0 items-center"
            >
              {shownAssignees.map((member, index) => (
                <span
                  key={member.id}
                  aria-hidden="true"
                  className={cn(
                    "flex size-5 items-center justify-center rounded-full text-mark-xs ring-2 ring-popover select-none dark:ring-card",
                    index > 0 && "-ml-1.5",
                    identityMarkClass(member.userId || member.id),
                  )}
                >
                  {markFor(member)}
                </span>
              ))}
              {hiddenAssignees > 0 ? (
                <span
                  aria-hidden="true"
                  className="-ml-1.5 flex size-5 items-center justify-center rounded-full bg-state-hover-strong text-mark-xs text-muted-foreground ring-2 ring-popover select-none dark:ring-card"
                >
                  +{hiddenAssignees}
                </span>
              ) : null}
            </span>
          ) : null}
        </div>
      ) : null}
    </>
  );
}

/**
 * The copy of a card that follows the pointer while it is dragged: the card
 * as it looks in its lane (same width, padding and radius), lifted by a
 * slight tilt and a stronger shadow. It is not interactive and hidden from
 * assistive technology; the card itself stays in its lane as the placeholder.
 */
export function KanbanCardOverlay({
  card,
  now,
  timeZone,
  copy = defaultCopy,
}: {
  card: Card;
  now?: Date;
  timeZone?: string;
  copy?: KanbanCardCopy;
}) {
  const [fallbackNow] = useState(() => new Date());
  const reducedMotion = prefersReducedMotion();
  const parts = cardParts(card, copy, now, timeZone, fallbackNow);
  return (
    <article
      aria-hidden="true"
      data-kanban-drag-overlay=""
      data-card-id={card.id}
      className={cn(
        CARD_CLASS,
        "h-full cursor-grabbing shadow-popover ring-1 ring-primary/30",
        reducedMotion
          ? "scale-[1.02]"
          : "scale-[1.03] rotate-2 transition-transform duration-150 ease-out starting:scale-100 starting:rotate-0",
      )}
    >
      <KanbanCardContent
        card={card}
        copy={copy}
        parts={parts}
        idPrefix={`${card.id}-overlay`}
      />
    </article>
  );
}

/**
 * A Kanban card. The whole card opens the item (pointer, Enter, Space) and is
 * the drag handle: a mouse drag starts after a few pixels of movement, so a
 * plain click still opens, and a touch drag starts after a long press. While
 * it is dragged the card stays in its lane as a gap of its own height at the
 * place it would be dropped at; moving with the keyboard happens from the item
 * sheet. The hover actions (assign to me, due date, options) are a shortcut
 * to the commonest sheet actions.
 */
export function KanbanCard({
  card,
  laneKey,
  now,
  timeZone,
  copy = defaultCopy,
  selected = false,
  dragDisabled = false,
  onMoveKey,
  onOpenDetails,
  actions,
  selection,
}: {
  card: Card;
  /** The lane this card is shown in (the lane it is dragged over while dragging). */
  laneKey?: string;
  /** The reference time for the due wording; read once by the container. */
  now?: Date;
  /** IANA time zone for calendar days; the runtime zone when omitted. */
  timeZone?: string;
  copy?: KanbanCardCopy;
  /** The card whose item sheet is open. */
  selected?: boolean;
  /** No pointer drag (a read-only board); the card still opens. */
  dragDisabled?: boolean;
  /**
   * Alt+ArrowLeft/Right/Up/Down asks to move the card (to the adjacent lane,
   * or within its lane). Absent: those keys do nothing.
   */
  onMoveKey?: (cardId: string, direction: KanbanMoveDirection) => void;
  onOpenDetails: (cardId: string) => void;
  /** Hover and focus actions; absent: the card has none. */
  actions?: KanbanCardActions;
  /** Picking cards for the bulk bar (checkbox, Space, Shift+click); absent: the card cannot be picked. */
  selection?: KanbanCardSelection;
}) {
  const [dueOpen, setDueOpen] = useState(false);
  const picked = selection?.selectedIds.has(card.id) ?? false;
  const picking = (selection?.selectedIds.size ?? 0) > 0;
  const { listeners, setNodeRef, transform, transition, isDragging } =
    useSortable({
      id: card.id,
      data: { type: KANBAN_CARD_TYPE, card, laneKey },
      disabled: dragDisabled,
      // The sortable animation of the cards that make room is the smooth part
      // of the drag; reduced motion drops it.
      transition: prefersReducedMotion()
        ? null
        : { duration: 180, easing: "cubic-bezier(0.2, 0, 0, 1)" },
    });
  // Pointer and touch only: Enter and Space belong to opening the card.
  const { onKeyDown: _keyboard, ...dragListeners } = (listeners ??
    {}) as Record<string, unknown>;
  void _keyboard;

  const [fallbackNow] = useState(() => new Date());
  const descriptionId = useId();
  const parts = cardParts(card, copy, now, timeZone, fallbackNow);
  const describedBy = [
    parts.hasTop ? `${descriptionId}-top` : null,
    parts.progress ? `${descriptionId}-progress` : null,
    parts.hasMeta ? `${descriptionId}-meta` : null,
  ]
    .filter(Boolean)
    .join(" ");

  return (
    <article
      ref={setNodeRef}
      style={{
        transform: CSS.Transform.toString(transform),
        transition,
      }}
      {...dragListeners}
      aria-current={selected ? "true" : undefined}
      data-picked={picked ? "" : undefined}
      data-dragging={isDragging ? "" : undefined}
      className={cn(
        CARD_CLASS,
        "group cursor-pointer shadow-card transition-shadow hover:shadow-card-hover",
        !dragDisabled && "touch-manipulation",
        selected && "shadow-card-hover ring-2 ring-inset ring-primary",
        picked && "bg-state-selected ring-2 ring-inset ring-primary/60",
        // The slot the card leaves is the gap it will fill: an empty, tinted
        // box of the card's own height (its content stays, hidden).
        isDragging &&
          "z-dropdown bg-state-selected shadow-none ring-1 ring-inset ring-primary/40 hover:shadow-none dark:bg-state-selected [&>*]:invisible",
      )}
    >
      <button
        type="button"
        className="absolute inset-0 z-10 rounded-[11px] focus-visible:outline-hidden focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-ring"
        aria-label={card.title}
        aria-describedby={describedBy || undefined}
        data-card-id={card.id}
        aria-keyshortcuts={
          [
            onMoveKey
              ? "Alt+ArrowLeft Alt+ArrowRight Alt+ArrowUp Alt+ArrowDown"
              : null,
            actions?.onAssignMe ? "A" : null,
            actions?.onSetDue ? "D" : null,
          ]
            .filter(Boolean)
            .join(" ") || undefined
        }
        onClick={(event) => {
          if (selection && event.shiftKey) {
            selection.onToggle(card.id, { range: true });
            return;
          }
          if (selection && (event.metaKey || event.ctrlKey)) {
            selection.onToggle(card.id);
            return;
          }
          onOpenDetails(card.id);
        }}
        onKeyDown={(event) => {
          const direction = onMoveKey ? ALT_ARROW[event.key] : undefined;
          if (direction && event.altKey && !event.ctrlKey && !event.metaKey) {
            event.preventDefault();
            onMoveKey?.(card.id, direction);
            return;
          }
          const plain = !event.altKey && !event.ctrlKey && !event.metaKey;
          if (plain && actions && !event.shiftKey) {
            const key = event.key.toLowerCase();
            if (
              key === "a" &&
              actions.onAssignMe &&
              !isAssignedTo(card, actions.currentUserId)
            ) {
              event.preventDefault();
              actions.onAssignMe(card);
              return;
            }
            if (key === "d" && actions.onSetDue) {
              event.preventDefault();
              setDueOpen(true);
              return;
            }
          }
          // With picking on, Space picks the card and Enter opens it.
          if (selection && event.key === " ") {
            event.preventDefault();
            selection.onToggle(card.id, { range: event.shiftKey });
            return;
          }
          if (event.key === "Enter" || event.key === " ") {
            event.preventDefault();
            onOpenDetails(card.id);
          }
        }}
      />
      {selection && !isDragging ? (
        <span
          className={cn(
            "absolute top-2 left-2 z-20 grid place-items-center rounded-[5px] bg-popover/90 p-0.5 dark:bg-card/90",
            !picked &&
              !picking &&
              "opacity-0 focus-within:opacity-100 group-hover:opacity-100",
          )}
        >
          <TableCheckbox
            checked={picked}
            label={fillCopy(copy.selectCard, { title: card.title })}
            onChange={() => selection.onToggle(card.id)}
          />
        </span>
      ) : null}
      {actions && !isDragging ? (
        <KanbanCardQuickActions
          card={card}
          copy={copy}
          actions={actions}
          dueOpen={dueOpen}
          onDueOpenChange={setDueOpen}
        />
      ) : null}
      <KanbanCardContent
        card={card}
        copy={copy}
        parts={parts}
        idPrefix={descriptionId}
      />
    </article>
  );
}
