import { useId } from "react";
import { CircleAlert, Link2, X } from "lucide-react";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogTitle,
  cn,
} from "@notrelix/ui-web";
import { parseDayKey, type DayKey } from "../calendar/calendar-model";
import { linkNameOf, useTimelineLinkChoices } from "./timeline-link-dialog";
import {
  fill,
  type TimelineGroup,
  type TimelineItem,
  type TimelineLink,
  type TimelineTone,
} from "./timeline-model";
import type { TimelineCopy } from "./timeline.copy";

/** "dd/MM" of a day, as the rows of `WM-TLCal-320` write it. */
function dayMonth(day: DayKey | null): string {
  if (!day) return "";
  const { month, day: date } = parseDayKey(day);
  return `${String(date).padStart(2, "0")}/${String(month).padStart(2, "0")}`;
}

const GLYPH_TONE: Readonly<Record<TimelineTone, string>> = {
  todo: "text-work-status-todo",
  doing: "text-work-status-doing",
  review: "text-work-status-review",
  done: "text-work-status-done",
};

/** The 13 px status glyph of the phone rows (empty, half, dot, check). */
export function TimelineStatusGlyph({ tone }: { tone: TimelineTone }) {
  return (
    <svg
      data-tl="sg"
      viewBox="0 0 24 24"
      aria-hidden="true"
      className={cn(
        "size-[13px] shrink-0 fill-none stroke-current stroke-2 [stroke-linecap:round] [stroke-linejoin:round]",
        GLYPH_TONE[tone],
      )}
    >
      {tone === "done" ? (
        <>
          <circle cx="12" cy="12" r="9" fill="currentColor" />
          <path
            d="m8.5 12 2.5 2.5 4.5-5"
            className="stroke-popover dark:stroke-card"
          />
        </>
      ) : (
        <>
          <circle cx="12" cy="12" r="8" />
          {tone === "doing" ? (
            <path d="M12 4a8 8 0 0 1 0 16z" fill="currentColor" />
          ) : tone === "review" ? (
            <circle cx="12" cy="12" r="3" fill="currentColor" />
          ) : null}
        </>
      )}
    </svg>
  );
}

export interface TimelinePhoneListProps {
  copy: TimelineCopy;
  groups: readonly TimelineGroup[];
  /** Absent: no link button (a viewer, or links are not offered). */
  onLink?: (item: TimelineItem) => void;
  onOpenItem: (id: string) => void;
}

/**
 * The Timeline under 640 px (`WM-TLCal-320`): no chart and no drag. One 44 px
 * row per dated item in group order (status, key, title, due day) that opens
 * the item, and a 44 px "Liên kết với…" button at its end.
 */
export function TimelinePhoneList({
  copy,
  groups,
  onLink,
  onOpenItem,
}: TimelinePhoneListProps) {
  const items = groups.flatMap((group) => group.items);
  if (items.length === 0)
    return (
      <div role="status" className="grid gap-1 px-4 py-10 leading-[1.45]">
        <b className="text-sm font-semibold">{copy.empty.title}</b>
        <p className="text-compact text-meta-foreground">
          {copy.empty.description}
        </p>
      </div>
    );
  return (
    <ul
      data-tl="phone-list"
      aria-label={copy.phone.list}
      className="m-0 list-none p-0"
    >
      {items.map((item) => {
        const name = linkNameOf(item);
        return (
          <li
            key={item.id}
            data-tl="mrow"
            data-row-id={item.id}
            className="box-border flex min-h-11 items-center gap-2 border-b border-line bg-popover px-3 py-1.5 dark:bg-card"
          >
            <button
              type="button"
              onClick={() => onOpenItem(item.id)}
              className="flex min-h-11 min-w-0 flex-1 cursor-pointer items-center gap-2 border-0 bg-transparent p-0 text-left focus-visible:outline-2 focus-visible:outline-ring"
            >
              <TimelineStatusGlyph tone={item.tone} />
              {item.itemKey ? (
                <span
                  data-tl="mkey"
                  className="shrink-0 font-mono text-[11.5px] leading-[1.45] text-meta-foreground"
                >
                  {item.itemKey}
                </span>
              ) : null}
              <span
                data-tl="mtitle"
                className="min-w-0 flex-1 truncate text-[13px] leading-[1.45] font-medium text-foreground"
              >
                {item.title}
              </span>
              <span
                data-tl="mdate"
                className="shrink-0 text-xs leading-[1.45] text-meta-foreground tabular-nums"
              >
                {dayMonth(item.dueDay ?? item.startDay)}
              </span>
            </button>
            {onLink && item.kind === "item" ? (
              <button
                type="button"
                data-tl="mlink"
                aria-label={fill(copy.phone.linkButton, { key: name })}
                onClick={() => onLink(item)}
                className="grid size-11 shrink-0 cursor-pointer place-items-center rounded-[8px] border-0 bg-transparent text-link focus-visible:outline-2 focus-visible:outline-ring"
              >
                <Link2
                  className="size-4"
                  strokeWidth={1.8}
                  aria-hidden="true"
                />
              </button>
            ) : null}
          </li>
        );
      })}
    </ul>
  );
}

export interface TimelineLinkSheetProps {
  copy: TimelineCopy;
  item: TimelineItem;
  items: readonly TimelineItem[];
  links: readonly TimelineLink[];
  onCreate: (link: TimelineLink) => void;
  onRemove: (link: TimelineLink) => void;
  onClose: () => void;
}

const STATUS_COPY = (copy: TimelineCopy, tone: TimelineTone) =>
  copy.legend[tone];

/**
 * "Liên kết với…" as the bottom sheet of `WM-TLCal-320`: the same choices as
 * the dialog, with 44 px targets. A row that would close a loop stays in the
 * list, disabled, with the item it already waits on; choosing it names the
 * whole chain in the alert.
 */
export function TimelineLinkSheet({
  copy,
  item,
  items,
  links,
  onCreate,
  onRemove,
  onClose,
}: TimelineLinkSheetProps) {
  const alertId = useId();
  const text = copy.link;
  const choices = useTimelineLinkChoices(item, items, links);
  const name = linkNameOf(item);
  const { chosenRow, refused } = choices;

  return (
    <Dialog open onOpenChange={(open) => (open ? null : onClose())}>
      <DialogContent
        showCloseButton={false}
        overlayClassName="bg-modal-scrim backdrop-blur-none"
        aria-label={fill(text.sheetLabel, { key: name })}
        aria-labelledby={undefined}
        data-tl="link-sheet"
        className="top-auto right-0 bottom-0 left-0 box-border flex max-h-[85dvh] [&>*]:shrink-0 w-full max-w-none translate-x-0 translate-y-0 flex-col gap-2.5 overflow-y-auto rounded-none rounded-t-[14px] border border-line bg-popover px-4 pt-2.5 pb-4 text-foreground shadow-popover ring-0 sm:max-w-none dark:bg-card data-open:zoom-in-100 data-closed:zoom-out-100"
      >
        <span
          data-tl="grab"
          aria-hidden="true"
          className="h-1 w-9 shrink-0 self-center rounded-[2px] bg-line-strong"
        />
        <div className="flex items-center gap-2">
          <DialogTitle className="m-0 flex-1 text-[14px] leading-[1.45] font-bold">
            {text.dialogTitle}
          </DialogTitle>
          <DialogDescription className="sr-only">
            {text.dialogDescription}
          </DialogDescription>
          <span
            data-tl="sheet-chip"
            className="inline-flex h-5.5 items-center rounded-[6px] bg-state-hover-strong px-2 text-[12px] font-semibold whitespace-nowrap text-muted-foreground"
          >
            {name}
          </span>
          <button
            type="button"
            data-tl="sheet-close"
            aria-label={text.close}
            onClick={onClose}
            className="grid size-11 shrink-0 cursor-pointer place-items-center rounded-[8px] border-0 bg-transparent text-muted-foreground focus-visible:outline-2 focus-visible:outline-ring"
          >
            <X className="size-4" strokeWidth={1.8} aria-hidden="true" />
          </button>
        </div>
        <div
          role="radiogroup"
          aria-label={text.direction}
          data-tl="sheet-seg"
          className="inline-flex w-fit self-start rounded-[8px] bg-state-hover-strong p-0.5"
        >
          {(["waits", "blocks"] as const).map((value) => (
            <button
              key={value}
              type="button"
              role="radio"
              aria-checked={choices.direction === value}
              onClick={() => choices.setDirection(value)}
              className={cn(
                "h-7 cursor-pointer rounded-[6px] border-0 bg-transparent px-[11px] text-[13px] font-medium text-muted-foreground focus-visible:outline-2 focus-visible:outline-ring",
                choices.direction === value &&
                  "bg-popover font-semibold text-foreground shadow-segment dark:bg-card",
              )}
            >
              {value === "waits"
                ? fill(text.waits, { key: name })
                : text.blocksShort}
            </button>
          ))}
        </div>
        <input
          role="combobox"
          aria-expanded="true"
          aria-controls={`${alertId}-list`}
          aria-haspopup="listbox"
          aria-describedby={refused ? alertId : undefined}
          aria-label={text.search}
          placeholder={text.search}
          value={choices.query}
          onChange={(event) => choices.setQuery(event.target.value)}
          data-tl="sheet-input"
          className="box-border h-11 w-full rounded-[7px] border border-line-strong bg-popover px-2.5 text-[13.5px] text-foreground outline-none focus-visible:border-primary focus-visible:outline-2 focus-visible:outline-offset-1 focus-visible:outline-primary dark:bg-card"
        />
        {refused && chosenRow ? (
          <div
            id={alertId}
            role="alert"
            data-tl="sheet-alert"
            className="flex items-start gap-2 rounded-[8px] bg-danger-subtle px-3 py-2.5 dark:bg-destructive/14 text-[12.5px] leading-[1.45] text-danger-text"
          >
            <CircleAlert
              className="size-4 shrink-0"
              strokeWidth={1.8}
              aria-hidden="true"
            />
            <span className="min-w-0 flex-1 text-foreground">
              <b className="text-danger-text">{text.cycleTitle}</b>{" "}
              {refused.map(choices.nameById).join(" → ")}.
            </span>
          </div>
        ) : null}
        <ul
          id={`${alertId}-list`}
          role="listbox"
          aria-label={text.list}
          data-tl="sheet-list"
          className="m-0 flex list-none flex-col p-0"
        >
          {choices.rows.length === 0 ? (
            <li role="presentation" className="px-2.5 py-2 text-[13.5px]">
              {text.noMatch}
            </li>
          ) : null}
          {choices.rows.map(({ candidate, cycle, linked }) => {
            const disabled = Boolean(cycle) || linked;
            const picked = choices.chosen === candidate.id;
            // Which of the two already waits on the other, closing the loop.
            const loop =
              choices.direction === "waits"
                ? { key: linkNameOf(candidate), item: name }
                : { key: name, item: linkNameOf(candidate) };
            return (
              <li key={candidate.id} role="presentation">
                <button
                  type="button"
                  role="option"
                  aria-selected={picked}
                  aria-disabled={disabled ? "true" : undefined}
                  data-tl="sheet-option"
                  onClick={() => choices.setChosen(candidate.id)}
                  className={cn(
                    "box-border flex min-h-[34px] w-full cursor-pointer items-start gap-2.5 rounded-[7px] border-0 bg-transparent px-2.5 py-[7px] text-left text-[13.5px] leading-[1.45] text-foreground hover:bg-state-hover focus-visible:outline-2 focus-visible:-outline-offset-2 focus-visible:outline-ring",
                    picked && !disabled && "bg-brand-subtle",
                    disabled && "text-meta-foreground hover:bg-transparent",
                  )}
                >
                  <span className="w-4 shrink-0" aria-hidden="true" />
                  {candidate.itemKey ? (
                    <span className="shrink-0 pt-0.5 font-mono text-[11.5px] text-meta-foreground">
                      {candidate.itemKey}
                    </span>
                  ) : null}
                  <span className="min-w-0 flex-1">
                    <b className="block font-medium">{candidate.title}</b>
                    <small className="block text-[12px] leading-[1.35] text-meta-foreground">
                      {cycle
                        ? fill(text.optionCycle, loop)
                        : linked
                          ? text.linked
                          : fill(text.optionMeta, {
                              status: STATUS_COPY(copy, candidate.tone),
                              from: dayMonth(
                                candidate.startDay ?? candidate.dueDay,
                              ),
                              to: dayMonth(
                                candidate.dueDay ?? candidate.startDay,
                              ),
                            })}
                    </small>
                  </span>
                </button>
              </li>
            );
          })}
        </ul>
        <button
          type="button"
          data-tl="sheet-create"
          aria-disabled={choices.canCreate ? undefined : "true"}
          onClick={() => {
            if (!chosenRow || !choices.canCreate) return;
            onCreate(chosenRow.link);
            onClose();
          }}
          className={cn(
            "box-border inline-flex h-11 w-full shrink-0 cursor-pointer items-center justify-center rounded-[7px] border border-primary bg-primary px-3 text-[13px] font-semibold text-primary-foreground focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ring",
            !choices.canCreate &&
              "cursor-not-allowed border-line-strong bg-state-hover-strong text-muted-foreground",
          )}
        >
          {text.create}
        </button>
        {choices.mine.length > 0 ? (
          <section className="grid gap-1 border-t border-line pt-2">
            <h3 className="m-0 text-crumb font-semibold text-muted-foreground">
              {fill(text.existing, { count: choices.mine.length })}
            </h3>
            {choices.mine.map((link) => {
              const values = {
                from: choices.nameById(link.from),
                to: choices.nameById(link.to),
              };
              return (
                <div
                  key={`${link.from}>${link.to}`}
                  className="flex items-center gap-2 text-compact"
                >
                  <span className="min-w-0 flex-1 truncate">
                    {fill(text.existingRow, values)}
                  </span>
                  <button
                    type="button"
                    aria-label={fill(text.removeLabel, values)}
                    onClick={() => onRemove(link)}
                    className="h-11 cursor-pointer rounded-[7px] border-0 bg-transparent px-3 text-compact font-medium text-muted-foreground focus-visible:outline-2 focus-visible:outline-ring"
                  >
                    {text.remove}
                  </button>
                </div>
              );
            })}
          </section>
        ) : null}
      </DialogContent>
    </Dialog>
  );
}
