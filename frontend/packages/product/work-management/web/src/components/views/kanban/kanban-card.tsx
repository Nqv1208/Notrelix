import { useId, useState, type PointerEventHandler } from "react";
import { useSortable } from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import { Flag } from "lucide-react";
import { cn } from "@notrelix/ui-web";
import type { Card, CardMember } from "@notrelix/work-management-core";
import {
  fillCopy,
  kanbanCardCopy,
  pluralCopy,
  type KanbanCardCopy,
} from "./kanban-card.copy";
import {
  ITEM_LABEL_TONE_CLASS,
  itemDueState,
  itemLabelTone,
  type ItemDueState,
} from "../../shared/item-display";

const defaultCopy = kanbanCardCopy("en");

/** The identity tones of the design tokens, as literal classes for Tailwind. */
const IDENTITY_MARK_CLASSES = [
  "bg-identity-blue text-identity-blue-foreground",
  "bg-identity-coral text-identity-coral-foreground",
  "bg-identity-gold text-identity-gold-foreground",
  "bg-identity-sage text-identity-sage-foreground",
  "bg-identity-ember text-identity-ember-foreground",
] as const;

/**
 * A stable identity tone per person; the API's raw member colour is not used.
 * Local copy of `IdentityMark`/`identityToneFor` in features/workspace
 * (`web/shell/shell-link.tsx`, `core/model/navigation.ts`), the shared origin.
 */
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

function markFor(member: CardMember): string {
  const initials = member.initials.trim() || Array.from(member.name)[0] || "?";
  return initials.toUpperCase();
}

const DUE_CLASS: Readonly<Record<ItemDueState["kind"], string>> = {
  overdue: "bg-danger-subtle font-semibold text-danger-subtle-foreground",
  today: "bg-warning-subtle font-semibold text-warning-subtle-foreground",
  date: "font-medium text-muted-foreground",
};

/**
 * A Kanban card: label chip and key, title, child-item progress, due wording,
 * priority (urgent and high only) and the first assignee's avatar. The whole
 * card opens the item (pointer, Enter, Space) and is the pointer drag handle;
 * moving with the keyboard happens from the item sheet. Actions such as copy
 * link, duplicate and archive live in the sheet, not on the card.
 */
export function KanbanCard({
  card,
  now,
  timeZone,
  copy = defaultCopy,
  selected = false,
  onOpenDetails,
}: {
  card: Card;
  /** The reference time for the due wording; read once by the container. */
  now?: Date;
  /** IANA time zone for calendar days; the runtime zone when omitted. */
  timeZone?: string;
  copy?: KanbanCardCopy;
  /** The card whose item sheet is open. */
  selected?: boolean;
  onOpenDetails: (cardId: string) => void;
}) {
  const { listeners, setNodeRef, transform, transition, isDragging } =
    useSortable({
      id: card.id,
      data: { type: "kanban-card", card },
    });
  // Pointer drag only: Enter and Space belong to opening the card.
  const onPointerDown = listeners?.onPointerDown as
    PointerEventHandler<HTMLElement> | undefined;

  const [fallbackNow] = useState(() => new Date());
  const reference = now ?? fallbackNow;
  const descriptionId = useId();

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

  const label = card.labels?.[0];
  const labelTone = label ? itemLabelTone(label) : null;
  const itemKey = card.itemKey?.trim() || null;

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

  const priority =
    card.priority === "urgent" || card.priority === "high"
      ? card.priority
      : null;

  const assignee = card.members?.[0];
  const moreAssignees = Math.max((card.members?.length ?? 0) - 1, 0);
  const assigneeLabel = assignee
    ? moreAssignees > 0
      ? fillCopy(copy.assignedToMore, {
          name: assignee.name,
          count: moreAssignees,
        })
      : fillCopy(copy.assignedTo, { name: assignee.name })
    : "";

  const hasTop = Boolean(label && labelTone) || itemKey !== null;
  const hasMeta = dueText !== null || priority !== null || Boolean(assignee);
  const describedBy = [
    hasTop ? `${descriptionId}-top` : null,
    progress ? `${descriptionId}-progress` : null,
    hasMeta ? `${descriptionId}-meta` : null,
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
      onPointerDown={onPointerDown}
      aria-current={selected ? "true" : undefined}
      className={cn(
        "group relative flex cursor-pointer flex-col gap-2 rounded-xl bg-card p-3 shadow-xs transition-shadow hover:shadow-md",
        selected && "shadow-md ring-2 ring-inset ring-primary",
        isDragging && "z-dropdown opacity-60",
      )}
    >
      <button
        type="button"
        className="absolute inset-0 z-10 rounded-xl focus-visible:outline-hidden focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-ring"
        aria-label={card.title}
        aria-describedby={describedBy || undefined}
        data-card-id={card.id}
        onClick={() => onOpenDetails(card.id)}
        onKeyDown={(event) => {
          if (event.key === "Enter" || event.key === " ") {
            event.preventDefault();
            onOpenDetails(card.id);
          }
        }}
      />

      {hasTop ? (
        <div
          id={`${descriptionId}-top`}
          className="flex min-w-0 items-center gap-1.5 text-metadata text-muted-foreground"
        >
          {label && labelTone ? (
            <>
              <span className="sr-only">{copy.label} </span>
              <span
                data-label-tone={labelTone}
                className={cn(
                  "inline-flex h-5.5 min-w-0 items-center truncate rounded-md px-2 text-caption font-semibold",
                  ITEM_LABEL_TONE_CLASS[labelTone],
                )}
              >
                {label.name}
              </span>
            </>
          ) : null}
          {itemKey ? (
            <span className="ml-auto shrink-0 font-mono">{itemKey}</span>
          ) : null}
        </div>
      ) : null}

      <h3
        className={cn(
          "line-clamp-3 break-words text-label font-medium",
          isDone ? "text-muted-foreground" : "text-foreground",
        )}
      >
        {card.title}
      </h3>

      {progress ? (
        <div
          data-child-progress=""
          className="flex items-center gap-2 text-caption tabular-nums text-muted-foreground"
        >
          <span
            aria-hidden="true"
            className="block h-1.25 flex-1 overflow-hidden rounded-full bg-muted"
          >
            <span
              data-progress-fill=""
              className="block h-full rounded-full bg-primary"
              style={{ width: `${progressPercent}%` }}
            />
          </span>
          <span aria-hidden="true">
            {progress.done}/{progress.total}
          </span>
          <span id={`${descriptionId}-progress`} className="sr-only">
            {fillCopy(copy.childProgress, {
              done: progress.done,
              total: progress.total,
            })}
          </span>
        </div>
      ) : null}

      {hasMeta ? (
        <div
          id={`${descriptionId}-meta`}
          className="flex min-w-0 flex-wrap items-center gap-1.5 text-metadata text-muted-foreground"
        >
          {due && dueText ? (
            <span
              data-due={due.kind}
              className={cn(
                "inline-flex h-5.5 items-center rounded-md px-1.5",
                DUE_CLASS[due.kind],
              )}
            >
              {due.kind === "overdue" ? null : (
                <span className="sr-only">{copy.due} </span>
              )}
              {dueText}
            </span>
          ) : null}
          {priority ? (
            <span
              data-priority={priority}
              className={cn(
                "inline-flex items-center gap-1 font-semibold",
                priority === "urgent"
                  ? "text-danger-text"
                  : "text-priority-high-text",
              )}
            >
              <Flag className="size-3.25" aria-hidden="true" />
              <span className="sr-only">{copy.priorityPrefix} </span>
              {copy.priority[priority]}
            </span>
          ) : null}
          {assignee ? (
            <span
              role="group"
              aria-label={assigneeLabel}
              className="ml-auto flex shrink-0 items-center"
            >
              <span
                aria-hidden="true"
                className={cn(
                  "flex size-5.5 items-center justify-center rounded-full text-caption font-semibold select-none",
                  identityMarkClass(assignee.userId || assignee.id),
                )}
              >
                {markFor(assignee)}
              </span>
            </span>
          ) : null}
        </div>
      ) : null}
    </article>
  );
}
