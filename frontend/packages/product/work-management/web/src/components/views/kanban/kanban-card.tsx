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

const defaultCopy = kanbanCardCopy("en");

/** The six label tones of the design tokens (`--label-<tone>`). */
export type KanbanLabelTone =
  "data" | "security" | "training" | "report" | "doc" | "test";

/** Literal tone classes, so Tailwind sees every one of them. */
export const KANBAN_LABEL_TONE_CLASS: Readonly<
  Record<KanbanLabelTone, string>
> = {
  data: "bg-label-data text-label-data-foreground",
  security: "bg-label-security text-label-security-foreground",
  training: "bg-label-training text-label-training-foreground",
  report: "bg-label-report text-label-report-foreground",
  doc: "bg-label-doc text-label-doc-foreground",
  test: "bg-label-test text-label-test-foreground",
};

/** Whole-word name keywords (normalised: lower case, no diacritics). */
const LABEL_TONE_KEYWORDS: ReadonlyArray<
  readonly [KanbanLabelTone, readonly string[]]
> = [
  ["data", ["du lieu", "data"]],
  ["security", ["bao mat", "an ninh", "security"]],
  ["training", ["dao tao", "training"]],
  ["report", ["bao cao", "report", "reports", "reporting"]],
  ["test", ["kiem thu", "test", "tests", "testing", "qa"]],
  [
    "doc",
    ["tai lieu", "doc", "docs", "documentation", "ha tang", "infrastructure"],
  ],
];

function normaliseLabelName(name: string): string {
  return name
    .normalize("NFD")
    .replace(/[̀-ͯ]/g, "")
    .replace(/đ/g, "d")
    .replace(/Đ/g, "d")
    .toLowerCase()
    .trim();
}

function hasWord(text: string, phrase: string): boolean {
  return ` ${text.replace(/[^a-z0-9]+/g, " ")} `.includes(` ${phrase} `);
}

/** Hue (0–360) and saturation (0–1) of a `#rgb` / `#rrggbb` colour. */
function hueOf(color: string): { hue: number; saturation: number } | null {
  const match = /^#([0-9a-f]{3}|[0-9a-f]{6})$/i.exec(color.trim());
  if (!match?.[1]) return null;
  const hex =
    match[1].length === 3
      ? Array.from(match[1], (digit) => digit + digit).join("")
      : match[1];
  const [r, g, b] = [0, 2, 4].map(
    (offset) => parseInt(hex.slice(offset, offset + 2), 16) / 255,
  ) as [number, number, number];
  const max = Math.max(r, g, b);
  const min = Math.min(r, g, b);
  const delta = max - min;
  const lightness = (max + min) / 2;
  const saturation =
    delta === 0 ? 0 : delta / (1 - Math.abs(2 * lightness - 1));
  if (delta === 0) return { hue: 0, saturation };
  let hue: number;
  if (max === r) hue = ((g - b) / delta) % 6;
  else if (max === g) hue = (b - r) / delta + 2;
  else hue = (r - g) / delta + 4;
  hue *= 60;
  if (hue < 0) hue += 360;
  return { hue, saturation };
}

/**
 * The label tone of a label, deterministic:
 * 1. by name — normalised (lower case, diacritics removed, đ → d) and matched
 *    as whole words against the keyword table (Dữ liệu/data, Bảo mật/security,
 *    Đào tạo/training, Báo cáo/report, Kiểm thử/test, Tài liệu/Hạ tầng/doc);
 * 2. otherwise by the hue of the label colour: grey (saturation < 0.15) → doc,
 *    red/pink → security, orange/yellow → report, green → training,
 *    blue → data, purple → test;
 * 3. otherwise doc. The raw label colour is never painted.
 */
export function kanbanLabelTone(label: {
  readonly name: string;
  readonly color?: string | null;
}): KanbanLabelTone {
  const name = normaliseLabelName(label.name);
  for (const [tone, keywords] of LABEL_TONE_KEYWORDS) {
    if (keywords.some((keyword) => hasWord(name, keyword))) return tone;
  }
  const colour = label.color ? hueOf(label.color) : null;
  if (!colour || colour.saturation < 0.15) return "doc";
  const { hue } = colour;
  if (hue < 20 || hue >= 330) return "security";
  if (hue < 70) return "report";
  if (hue < 170) return "training";
  if (hue < 250) return "data";
  return "test";
}

export type KanbanDueState =
  | { readonly kind: "overdue"; readonly days: number }
  | { readonly kind: "today" }
  | { readonly kind: "date"; readonly text: string };

function startOfDay(date: Date): number {
  return new Date(
    date.getFullYear(),
    date.getMonth(),
    date.getDate(),
  ).getTime();
}

function pad(value: number): string {
  return String(value).padStart(2, "0");
}

/**
 * Due state by calendar day in the runtime time zone against `now`: before
 * today → overdue by N days; the same day → today (even after the hour);
 * later → "dd/MM", with "/yyyy" when the year differs from `now`. An absent
 * or unparseable date gives `null`.
 */
export function kanbanDueState(
  dueIso: string | null | undefined,
  now: Date,
): KanbanDueState | null {
  if (!dueIso) return null;
  const due = new Date(dueIso);
  if (Number.isNaN(due.getTime())) return null;
  const days = Math.round((startOfDay(now) - startOfDay(due)) / 86_400_000);
  if (days > 0) return { kind: "overdue", days };
  if (days === 0) return { kind: "today" };
  const dayMonth = `${pad(due.getDate())}/${pad(due.getMonth() + 1)}`;
  return {
    kind: "date",
    text:
      due.getFullYear() === now.getFullYear()
        ? dayMonth
        : `${dayMonth}/${due.getFullYear()}`,
  };
}

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

const DUE_CLASS: Readonly<Record<KanbanDueState["kind"], string>> = {
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
  copy = defaultCopy,
  selected = false,
  onOpenDetails,
}: {
  card: Card;
  /** The reference time for the due wording; read once by the container. */
  now?: Date;
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
    : kanbanDueState(card.dueAt ?? card.dueDate, reference);
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
  const labelTone = label ? kanbanLabelTone(label) : null;
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
      aria-label={card.title}
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
                  KANBAN_LABEL_TONE_CLASS[labelTone],
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
