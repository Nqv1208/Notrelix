import { useId, useState } from "react";
import { Check } from "lucide-react";
import {
  Button,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Input,
  cn,
} from "@notrelix/ui-web";
import {
  fill,
  findLinkCycle,
  hasLink,
  type TimelineItem,
  type TimelineLink,
} from "./timeline-model";
import type { TimelineCopy } from "./timeline.copy";

type Direction = "waits" | "blocks";

const nameOf = (item: TimelineItem) => item.itemKey ?? item.title;

export interface TimelineLinkDialogProps {
  copy: TimelineCopy;
  /** The item the dialog was opened from. */
  item: TimelineItem;
  /** Every linkable item of the board. */
  items: readonly TimelineItem[];
  links: readonly TimelineLink[];
  onCreate: (link: TimelineLink) => void;
  onRemove: (link: TimelineLink) => void;
  onClose: () => void;
}

/**
 * "Liên kết với…": the keyboard way to the finish-to-start link. A segment
 * says which way the dependency runs, the list holds the other items (a row
 * that would close a loop is disabled with its reason and the loop's chain
 * is announced), and the item's existing links can be removed here.
 */
export function TimelineLinkDialog({
  copy,
  item,
  items,
  links,
  onCreate,
  onRemove,
  onClose,
}: TimelineLinkDialogProps) {
  const [direction, setDirection] = useState<Direction>("waits");
  const [query, setQuery] = useState("");
  const [chosen, setChosen] = useState<string | null>(null);
  const alertId = useId();
  const text = copy.link;
  const byId = new Map(items.map((candidate) => [candidate.id, candidate]));
  byId.set(item.id, item);
  const nameById = (id: string) => {
    const found = byId.get(id);
    return found ? nameOf(found) : id;
  };
  // `waits`: the chosen item finishes first (chosen -> item).
  const linkFor = (other: string): TimelineLink =>
    direction === "waits"
      ? { from: other, to: item.id }
      : { from: item.id, to: other };
  const needle = query.trim().toLowerCase();
  const rows = items
    .filter((candidate) => candidate.id !== item.id)
    .filter((candidate) =>
      `${candidate.itemKey ?? ""} ${candidate.title}`
        .toLowerCase()
        .includes(needle),
    )
    .map((candidate) => {
      const link = linkFor(candidate.id);
      const cycle = findLinkCycle(links, link.from, link.to);
      const linked = hasLink(links, link.from, link.to);
      return { candidate, link, cycle, linked };
    });
  const chosenRow = rows.find((row) => row.candidate.id === chosen);
  const refused = chosenRow?.cycle ?? null;
  const mine = links.filter(
    (link) => link.from === item.id || link.to === item.id,
  );
  const canCreate = chosenRow && !chosenRow.cycle && !chosenRow.linked;

  return (
    <Dialog open onOpenChange={(open) => (open ? null : onClose())}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>
            {text.dialogTitle}{" "}
            <span className="font-mono text-sm font-medium text-muted-foreground">
              {nameOf(item)}
            </span>
          </DialogTitle>
          <DialogDescription>{text.dialogDescription}</DialogDescription>
        </DialogHeader>
        <div
          role="radiogroup"
          aria-label={text.direction}
          className="inline-flex w-fit rounded-[8px] bg-state-hover-strong p-0.5"
        >
          {(["waits", "blocks"] as const).map((value) => (
            <button
              key={value}
              type="button"
              role="radio"
              aria-checked={direction === value}
              className={cn(
                "h-7 cursor-pointer rounded-[6px] px-2.5 text-compact font-medium text-muted-foreground focus-visible:outline-2 focus-visible:outline-ring",
                direction === value &&
                  "bg-popover font-semibold text-foreground shadow-segment dark:bg-card",
              )}
              onClick={() => {
                setDirection(value);
                setChosen(null);
              }}
            >
              {fill(text[value], { key: nameOf(item) })}
            </button>
          ))}
        </div>
        <Input
          autoFocus
          role="combobox"
          aria-expanded="true"
          aria-controls={`${alertId}-list`}
          aria-haspopup="listbox"
          aria-describedby={refused ? alertId : undefined}
          value={query}
          placeholder={text.search}
          aria-label={text.search}
          onChange={(event) => setQuery(event.target.value)}
        />
        {refused && chosenRow ? (
          <p
            id={alertId}
            role="alert"
            className="rounded-[8px] bg-danger-subtle px-2.5 py-2 text-crumb leading-[1.45] text-danger-text"
          >
            {fill(text.cycleAlert, {
              key: nameOf(chosenRow.candidate),
              chain: refused.map(nameById).join(" → "),
            })}
          </p>
        ) : null}
        <ul
          id={`${alertId}-list`}
          role="listbox"
          aria-label={text.list}
          className="max-h-56 overflow-y-auto"
        >
          {rows.length === 0 ? (
            <li role="presentation" className="px-2.5 py-2 text-compact">
              {text.noMatch}
            </li>
          ) : null}
          {rows.map(({ candidate, cycle, linked }) => {
            const disabled = Boolean(cycle) || linked;
            return (
              <li key={candidate.id} role="presentation">
                <button
                  type="button"
                  role="option"
                  aria-selected={chosen === candidate.id}
                  aria-disabled={disabled ? "true" : undefined}
                  className={cn(
                    "flex w-full cursor-pointer items-center gap-2 rounded-md px-2.5 py-1.5 text-left text-compact hover:bg-state-hover-strong focus-visible:outline-2 focus-visible:outline-ring",
                    chosen === candidate.id &&
                      "bg-brand-subtle font-semibold text-brand-subtle-foreground",
                    disabled && "text-meta-foreground",
                  )}
                  onClick={() => {
                    // A refused row stays selectable so its alert can say why.
                    setChosen(candidate.id);
                  }}
                >
                  {chosen === candidate.id && !disabled ? (
                    <Check className="size-4" aria-hidden="true" />
                  ) : null}
                  <span className="font-mono text-crumb">
                    {candidate.itemKey}
                  </span>
                  <span className="min-w-0 flex-1 truncate">
                    {candidate.title}
                  </span>
                  {cycle ? (
                    <small className="text-danger-text">
                      {text.cycleReason}
                    </small>
                  ) : linked ? (
                    <small>{text.linked}</small>
                  ) : null}
                </button>
              </li>
            );
          })}
        </ul>
        {mine.length > 0 ? (
          <section className="grid gap-1 border-t border-border pt-2">
            <h3 className="text-crumb font-semibold text-muted-foreground">
              {fill(text.existing, { count: mine.length })}
            </h3>
            {mine.map((link) => {
              const values = {
                from: nameById(link.from),
                to: nameById(link.to),
              };
              return (
                <div
                  key={`${link.from}>${link.to}`}
                  className="flex items-center gap-2 text-compact"
                >
                  <span className="min-w-0 flex-1 truncate">
                    {fill(text.existingRow, values)}
                  </span>
                  <Button
                    type="button"
                    variant="ghost"
                    size="sm"
                    className="min-h-6 min-w-6"
                    aria-label={fill(text.removeLabel, values)}
                    onClick={() => onRemove(link)}
                  >
                    {text.remove}
                  </Button>
                </div>
              );
            })}
          </section>
        ) : null}
        <DialogFooter>
          <Button type="button" variant="ghost" onClick={onClose}>
            {text.cancel}
          </Button>
          <Button
            type="button"
            disabled={!canCreate}
            onClick={() => {
              if (chosenRow && canCreate) {
                onCreate(chosenRow.link);
                onClose();
              }
            }}
          >
            {text.create}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
