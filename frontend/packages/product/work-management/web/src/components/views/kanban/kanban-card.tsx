import { useMemo, useState } from "react";
import { useSortable } from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import {
  AlertCircle,
  CalendarDays,
  CheckCircle2,
  ChevronUp,
  ChevronsUp,
  FileText,
  GripVertical,
  MessageSquareText,
  MoreHorizontal,
  Paperclip,
} from "lucide-react";
import { Badge, Button, cn } from "@notrelix/ui-web";
import type { Board, Card, CardMember } from "@notrelix/work-management-core";
import { KanbanCardMenu } from "./kanban-card-menu";
import {
  fillCopy,
  kanbanCardCopy,
  type KanbanCardCopy,
} from "./kanban-card.copy";

const defaultCopy = kanbanCardCopy("en");

/** Status roles, never identity colours (WX-DEC-006). */
const STATUS_PRESENTATION: Readonly<
  Record<string, { label: keyof KanbanCardCopy["status"]; className: string }>
> = {
  "status-not-started": {
    label: "notStarted",
    className: "bg-secondary text-secondary-foreground",
  },
  "status-working": {
    label: "working",
    className: "bg-warning-subtle text-warning-subtle-foreground",
  },
  "status-stuck": {
    label: "stuck",
    className: "bg-danger-subtle text-danger-subtle-foreground",
  },
  "status-done": {
    label: "done",
    className: "bg-success-subtle text-success-subtle-foreground",
  },
  "status-completed": {
    label: "completed",
    className: "bg-success-subtle text-success-subtle-foreground",
  },
};

const COMPLETE_STATUSES = new Set(["status-done", "status-completed"]);

/** The identity tones of the design tokens, as literal classes for Tailwind. */
const IDENTITY_MARK_CLASSES = [
  "bg-identity-blue text-identity-blue-foreground",
  "bg-identity-coral text-identity-coral-foreground",
  "bg-identity-gold text-identity-gold-foreground",
  "bg-identity-sage text-identity-sage-foreground",
  "bg-identity-ember text-identity-ember-foreground",
] as const;

/** A stable identity tone per person; the API's raw member colour is not used. */
function identityMarkClass(seed: string): string {
  let hash = 0;
  for (let index = 0; index < seed.length; index += 1) {
    hash = (hash * 31 + seed.charCodeAt(index)) | 0;
  }
  return (
    IDENTITY_MARK_CLASSES[Math.abs(hash) % IDENTITY_MARK_CLASSES.length] ??
    IDENTITY_MARK_CLASSES[0]
  );
}

function startOfDay(date: Date): Date {
  return new Date(date.getFullYear(), date.getMonth(), date.getDate());
}

function parseDate(value: string | undefined): Date | null {
  if (!value) return null;
  const parsed = new Date(value);
  return Number.isNaN(parsed.getTime()) ? null : parsed;
}

function markFor(member: CardMember): string {
  const initials = member.initials.trim() || Array.from(member.name)[0] || "?";
  return initials.toUpperCase();
}

export function KanbanCard({
  board: _board,
  card,
  now,
  copy = defaultCopy,
  onOpenDetails,
  onCopyLink,
  onDuplicate,
  onDelete,
}: {
  board: Board;
  card: Card;
  /** The reference time for the overdue state; read once by the container. */
  now?: Date;
  copy?: KanbanCardCopy;
  onOpenDetails: (cardId: string) => void;
  onCopyLink: (cardId: string) => void;
  onDuplicate: () => void;
  onDelete: () => void;
}) {
  const {
    attributes,
    listeners,
    setNodeRef,
    transform,
    transition,
    isDragging,
  } = useSortable({
    id: card.id,
    data: { type: "kanban-card", card },
  });

  const [fallbackNow] = useState(() => new Date());
  const reference = now ?? fallbackNow;

  const checklistTotal = useMemo(
    () =>
      (card.checklists || []).reduce(
        (count, checklist) => count + (checklist.items?.length || 0),
        0,
      ),
    [card.checklists],
  );
  const checklistDone = useMemo(
    () =>
      (card.checklists || []).reduce(
        (count, checklist) =>
          count + (checklist.items?.filter((item) => item.isDone).length || 0),
        0,
      ),
    [card.checklists],
  );

  const due = parseDate(card.dueDate);
  const isComplete =
    Boolean(card.completedAt) || COMPLETE_STATUSES.has(card.status);
  const isOverdue =
    due !== null &&
    !isComplete &&
    due.getTime() < startOfDay(reference).getTime();
  const dueLabel = due
    ? new Intl.DateTimeFormat(copy.locale, {
        month: "short",
        day: "numeric",
      }).format(due)
    : null;

  const known = STATUS_PRESENTATION[card.status];
  const statusLabel = known ? copy.status[known.label] : card.status;
  const statusClassName =
    known?.className ?? "bg-secondary text-secondary-foreground";

  const PriorityIcon =
    card.priority === "urgent"
      ? ChevronsUp
      : card.priority === "high"
        ? ChevronUp
        : null;

  const assignee = card.members?.[0];
  const moreAssignees = Math.max((card.members?.length ?? 0) - 1, 0);
  const assigneeName = assignee?.name ?? "";
  const assigneeLabel =
    moreAssignees > 0
      ? fillCopy(copy.assignedToMore, {
          name: assigneeName,
          count: moreAssignees,
        })
      : fillCopy(copy.assignedTo, { name: assigneeName });

  const hasMetadata =
    due !== null ||
    checklistTotal > 0 ||
    card._count.comments > 0 ||
    card._count.attachments > 0;

  return (
    <article
      ref={setNodeRef}
      style={{
        transform: CSS.Transform.toString(transform),
        transition,
      }}
      className={cn(
        "group relative flex flex-col gap-2 rounded-xl border border-border bg-card p-3 shadow-xs transition-shadow hover:shadow-md cursor-pointer",
        isDragging && "z-dropdown border-primary opacity-60",
      )}
      aria-label={card.title}
    >
      <button
        type="button"
        className="absolute inset-0 z-10 rounded-xl focus-visible:outline-hidden focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-ring"
        aria-label={card.title}
        onClick={() => onOpenDetails(card.id)}
        onKeyDown={(event) => {
          if (event.key === "Enter" || event.key === " ") {
            event.preventDefault();
            onOpenDetails(card.id);
          }
        }}
      />

      <div className="flex items-start gap-2">
        <button
          type="button"
          className="relative z-20 mt-0.5 cursor-grab rounded-sm p-0.5 text-muted-foreground transition-colors hover:bg-state-hover active:cursor-grabbing focus-visible:outline-hidden focus-visible:ring-2 focus-visible:ring-ring"
          aria-label={fillCopy(copy.moveCard, { title: card.title })}
          onClick={(event) => event.stopPropagation()}
          {...attributes}
          {...listeners}
        >
          <GripVertical className="size-3.5" aria-hidden="true" />
        </button>

        <div className="min-w-0 flex-1">
          <p className="line-clamp-2 break-words text-label font-medium text-foreground">
            {card.title}
          </p>
          {card.linkedPageId ? (
            <div className="mt-1 flex items-center gap-1 text-caption text-muted-foreground">
              <FileText className="size-3 shrink-0" aria-hidden="true" />
              <span className="truncate">{copy.docLinked}</span>
            </div>
          ) : null}
        </div>

        <div
          className="relative z-20 opacity-0 transition-opacity group-hover:opacity-100 group-focus-within:opacity-100"
          onClick={(event) => event.stopPropagation()}
        >
          <KanbanCardMenu
            onCopyLink={() => onCopyLink(card.id)}
            onDuplicate={onDuplicate}
            onDelete={onDelete}
          >
            <Button
              variant="ghost"
              size="icon-xs"
              className="size-6"
              aria-label={fillCopy(copy.cardMenu, { title: card.title })}
            >
              <MoreHorizontal className="size-3.5" aria-hidden="true" />
            </Button>
          </KanbanCardMenu>
        </div>
      </div>

      {card.status || isOverdue || PriorityIcon ? (
        <div className="flex min-w-0 flex-wrap items-center gap-1.5">
          {card.status ? (
            <Badge
              variant="secondary"
              className={cn(
                "inline-block max-w-full truncate leading-4",
                statusClassName,
              )}
            >
              {statusLabel}
            </Badge>
          ) : null}
          {isOverdue ? (
            <span
              data-overdue=""
              className="inline-flex h-5 items-center gap-1 rounded-4xl bg-danger-subtle px-2 text-caption font-medium text-danger-subtle-foreground"
            >
              <AlertCircle className="size-3 shrink-0" aria-hidden="true" />
              {copy.overdue}
            </span>
          ) : null}
          {PriorityIcon && card.priority ? (
            <span
              role="img"
              aria-label={copy.priority[card.priority as "urgent" | "high"]}
              className={cn(
                "inline-flex size-5 items-center justify-center",
                card.priority === "urgent"
                  ? "text-danger-text"
                  : "text-info-text",
              )}
            >
              <PriorityIcon className="size-3.5" aria-hidden="true" />
            </span>
          ) : null}
        </div>
      ) : null}

      {hasMetadata || assignee ? (
        <div className="flex min-w-0 flex-wrap items-center justify-between gap-x-3 gap-y-1.5">
          <div className="flex shrink-0 items-center gap-2.5 text-metadata text-muted-foreground">
            {due && dueLabel ? (
              <span
                className={cn(
                  "inline-flex items-center gap-1",
                  isOverdue && "text-danger-text",
                )}
              >
                <CalendarDays className="size-3" aria-hidden="true" />
                <span className="sr-only">{copy.due}</span>
                <time dateTime={card.dueDate}>{dueLabel}</time>
              </span>
            ) : null}
            {checklistTotal > 0 ? (
              <span className="inline-flex items-center gap-1 tabular-nums">
                <CheckCircle2 className="size-3" aria-hidden="true" />
                <span aria-hidden="true">
                  {checklistDone}/{checklistTotal}
                </span>
                <span className="sr-only">
                  {fillCopy(copy.checklist, {
                    done: checklistDone,
                    total: checklistTotal,
                  })}
                </span>
              </span>
            ) : null}
            {card._count.comments > 0 ? (
              <span className="inline-flex items-center gap-1 tabular-nums">
                <MessageSquareText className="size-3" aria-hidden="true" />
                <span aria-hidden="true">{card._count.comments}</span>
                <span className="sr-only">
                  {fillCopy(copy.comments, { count: card._count.comments })}
                </span>
              </span>
            ) : null}
            {card._count.attachments > 0 ? (
              <span className="inline-flex items-center gap-1 tabular-nums">
                <Paperclip className="size-3" aria-hidden="true" />
                <span aria-hidden="true">{card._count.attachments}</span>
                <span className="sr-only">
                  {fillCopy(copy.attachments, {
                    count: card._count.attachments,
                  })}
                </span>
              </span>
            ) : null}
          </div>

          {assignee ? (
            <span
              role="group"
              aria-label={assigneeLabel}
              className="ml-auto flex min-w-0 max-w-full items-center gap-1.5"
            >
              <span
                aria-hidden="true"
                className={cn(
                  "flex size-5 shrink-0 items-center justify-center rounded-full text-caption font-semibold select-none",
                  identityMarkClass(assignee.userId || assignee.id),
                )}
              >
                {markFor(assignee)}
              </span>
              <span
                aria-hidden="true"
                className="truncate text-metadata text-foreground"
              >
                {assigneeName}
              </span>
              {moreAssignees > 0 ? (
                <span
                  aria-hidden="true"
                  className="shrink-0 text-caption tabular-nums text-muted-foreground"
                >
                  +{moreAssignees}
                </span>
              ) : null}
            </span>
          ) : null}
        </div>
      ) : null}
    </article>
  );
}
