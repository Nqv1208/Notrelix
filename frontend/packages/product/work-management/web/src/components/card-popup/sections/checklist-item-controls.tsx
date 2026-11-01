import { useMemo, useState } from "react";
import { Calendar, Check, UserRound } from "lucide-react";
import { PopoverTrigger } from "@notrelix/ui-web";
import type { ChecklistItem } from "@notrelix/work-management-core";
import { calendarParts } from "../../shared/item-display";
import { zonedInstant } from "../../item-page/item-pickers";
import { fillPopupCopy } from "../card-popup.copy";
import { useCardPopup } from "../card-popup-context";
import {
  IconBtn,
  PanelBtn,
  PanelInput,
  PanelPopover,
  PersonMark,
} from "../popup-ui";
import { Hint, PickerHead } from "../pickers/picker-bits";

/** The default time of an item due date, as in the dates panel. */
const DUE_HOUR = 17;

interface Person {
  readonly userId: string;
  readonly name: string;
  readonly initials: string;
}

/** The people an item can be given to: the board's members and whoever the directory lists for it, you first. */
function usePeople(): Person[] {
  const { board, data, card, copy } = useCardPopup();
  const viewerId = data.viewerUserId ?? null;
  const you = copy.related.checklists.assignMe;
  return useMemo(() => {
    const byId = new Map<string, Person>();
    const add = (userId: string, name: string, initials?: string) =>
      byId.set(userId, {
        userId,
        name,
        initials: initials || Array.from(name)[0]?.toUpperCase() || "?",
      });
    for (const m of board.members) add(m.userId, m.name, m.initials);
    for (const p of data.people?.people ?? [])
      if (p.onBoard || byId.has(p.userId) || p.userId === viewerId)
        add(p.userId, p.name);
    for (const m of card.members)
      if (!byId.has(m.userId)) add(m.userId, m.name, m.initials);
    if (viewerId && !byId.has(viewerId)) add(viewerId, you);
    const me = viewerId ? byId.get(viewerId) : undefined;
    const rest = [...byId.values()].filter((p) => p.userId !== viewerId);
    return me ? [me, ...rest] : rest;
  }, [board.members, card.members, data.people, viewerId, you]);
}

/** `Giao mục này`: gives one checklist item to a person, or to nobody. */
export function ItemAssignButton({
  item,
  onAssign,
}: {
  item: ChecklistItem;
  onAssign: (assigneeId: string | null) => void;
}) {
  const { copy } = useCardPopup();
  const t = copy.related.checklists;
  const people = usePeople();
  const [open, setOpen] = useState(false);
  const pick = (id: string | null) => {
    onAssign(id);
    setOpen(false);
  };
  return (
    <PanelPopover
      open={open}
      onOpenChange={setOpen}
      width={300}
      trigger={
        <PopoverTrigger asChild>
          <IconBtn
            small
            aria-label={`${t.assignItem}: ${item.title}`}
            title={t.assignItem}
            aria-haspopup="dialog"
          >
            <UserRound aria-hidden="true" className="size-4" />
          </IconBtn>
        </PopoverTrigger>
      }
    >
      <PickerHead title={t.assignItem} onClose={() => setOpen(false)} />
      <div className="flex max-h-64 flex-col overflow-y-auto">
        {[{ userId: "", name: t.unassign, initials: "" }, ...people].map(
          (person) => {
            const id = person.userId || null;
            const selected = (item.assigneeId ?? null) === id;
            return (
              <button
                key={person.userId || "nobody"}
                type="button"
                aria-pressed={selected}
                onClick={() => pick(id)}
                className="flex min-h-9 w-full items-center gap-2.5 rounded-[8px] border-0 bg-transparent px-2 text-left text-[14px] text-foreground hover:bg-state-hover-strong focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary"
              >
                {id ? <PersonMark member={person} size={24} /> : null}
                <span className="min-w-0 flex-1 truncate">{person.name}</span>
                {selected ? (
                  <Check
                    aria-hidden="true"
                    className="size-4 text-primary"
                    strokeWidth={2.5}
                  />
                ) : null}
              </button>
            );
          },
        )}
      </div>
    </PanelPopover>
  );
}

interface Day {
  readonly year: number;
  readonly month: number;
  readonly day: number;
}

const pad = (n: number) => String(n).padStart(2, "0");
const format = (d: Day) => `${pad(d.day)}/${pad(d.month)}/${d.year}`;

function parse(text: string): Day | null {
  const m = /^\s*(\d{1,2})\/(\d{1,2})\/(\d{4})\s*$/.exec(text);
  if (!m) return null;
  const day = { year: +m[3]!, month: +m[2]!, day: +m[1]! };
  const check = new Date(Date.UTC(day.year, day.month - 1, day.day));
  return check.getUTCFullYear() === day.year &&
    check.getUTCMonth() + 1 === day.month &&
    check.getUTCDate() === day.day
    ? day
    : null;
}

function addDays(day: Day, days: number): Day {
  const d = new Date(Date.UTC(day.year, day.month - 1, day.day + days));
  return {
    year: d.getUTCFullYear(),
    month: d.getUTCMonth() + 1,
    day: d.getUTCDate(),
  };
}

const dayOf = (iso: string | null | undefined, tz?: string): Day | null => {
  if (!iso) return null;
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) return null;
  const p = calendarParts(date, tz);
  return { year: p.year, month: p.month, day: p.day };
};

/** The date an item shows beside its text: `Hạn 31/10`. */
export function itemDueText(
  copyText: string,
  iso: string,
  tz?: string,
): string | null {
  const day = dayOf(iso, tz);
  return day
    ? fillPopupCopy(copyText, { date: `${pad(day.day)}/${pad(day.month)}` })
    : null;
}

/** `Đặt hạn cho mục`: sets or clears the item's own due date. */
export function ItemDueButton({
  item,
  onDue,
}: {
  item: ChecklistItem;
  onDue: (dueAt: string | null) => void;
}) {
  const { copy, now, timeZone } = useCardPopup();
  const t = copy.related.checklists;
  const today = dayOf(now.toISOString(), timeZone) as Day;
  const current = dayOf(item.dueAt, timeZone);
  const [open, setOpen] = useState(false);
  const [text, setText] = useState(format(current ?? today));
  const [touched, setTouched] = useState(false);
  const parsed = parse(text);
  const bad = touched && parsed === null;

  const write = (day: Day | null) => {
    onDue(day ? zonedInstant(day, DUE_HOUR, 0, timeZone).toISOString() : null);
    setOpen(false);
  };
  return (
    <PanelPopover
      open={open}
      onOpenChange={(next) => {
        setOpen(next);
        if (next) {
          setText(format(current ?? today));
          setTouched(false);
        }
      }}
      width={300}
      trigger={
        <PopoverTrigger asChild>
          <IconBtn
            small
            aria-label={`${t.dueItem}: ${item.title}`}
            title={t.dueItem}
            aria-haspopup="dialog"
          >
            <Calendar aria-hidden="true" className="size-4" />
          </IconBtn>
        </PopoverTrigger>
      }
    >
      <PickerHead title={t.dueItem} onClose={() => setOpen(false)} />
      <div className="mb-2 flex gap-1.5">
        {(
          [
            [t.today, 0],
            [t.tomorrow, 1],
            [t.nextWeek, 7],
          ] as const
        ).map(([label, days]) => (
          <PanelBtn
            key={label}
            small
            className="flex-1"
            onClick={() => write(addDays(today, days))}
          >
            {label}
          </PanelBtn>
        ))}
      </div>
      <label className="mb-1 block text-[12px] font-semibold text-muted-foreground">
        {t.dueLabel}
      </label>
      <PanelInput
        data-autofocus=""
        value={text}
        invalid={bad}
        aria-invalid={bad || undefined}
        aria-label={t.dueLabel}
        inputMode="numeric"
        onChange={(event) => {
          setText(event.target.value);
          setTouched(true);
        }}
        onKeyDown={(event) => {
          if (event.key === "Enter" && parsed) write(parsed);
        }}
      />
      <Hint error={bad}>{bad ? t.dueBad : t.dueHint}</Hint>
      <div className="mt-3 flex flex-col gap-2">
        <PanelBtn
          variant="primary"
          disabled={parsed === null}
          onClick={() => parsed && write(parsed)}
        >
          {t.dueSave}
        </PanelBtn>
        {item.dueAt ? (
          <PanelBtn onClick={() => write(null)}>{t.dueClear}</PanelBtn>
        ) : null}
      </div>
    </PanelPopover>
  );
}
