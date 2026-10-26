import { useEffect, useId, useState } from "react";
import { CalendarDays, Check, Plus, X } from "lucide-react";
import {
  Calendar,
  Command,
  CommandEmpty,
  CommandInput,
  CommandItem,
  CommandList,
  Popover,
  PopoverContent,
  PopoverTrigger,
  cn,
} from "@notrelix/ui-web";
import {
  ITEM_LABEL_TONE_CLASS,
  identityMarkClass,
  itemLabelTone,
} from "../shared/item-display";
import type { ItemCreateCopy } from "./item-create.copy";
import type { ItemCreateLabel, ItemCreateMember } from "./item-create-dialog";

const FIELD_CLASS =
  "box-border h-10 w-full rounded-[9px] border border-line-strong bg-popover px-3 text-label text-foreground outline-none placeholder:text-meta-foreground focus-visible:border-primary focus-visible:ring-3 focus-visible:ring-brand-subtle";

/** `YYYY-MM-DD` of a local calendar day. */
export function toDayString(date: Date): string {
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  return `${date.getFullYear()}-${month}-${day}`;
}

/** The local calendar day a `YYYY-MM-DD` names (noon, so no zone moves it). */
export function fromDayString(day: string): Date | null {
  const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(day);
  if (!match) return null;
  const date = new Date(
    Number(match[1]),
    Number(match[2]) - 1,
    Number(match[3]),
    12,
  );
  return Number.isNaN(date.getTime()) ? null : date;
}

export function addDays(day: string, amount: number): string {
  const date = fromDayString(day) ?? new Date();
  date.setDate(date.getDate() + amount);
  return toDayString(date);
}

/** `16/10/2026` for a `YYYY-MM-DD`. */
export function displayDay(day: string): string {
  const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(day);
  return match ? `${match[3]}/${match[2]}/${match[1]}` : "";
}

/** The `YYYY-MM-DD` a `d/m/yyyy` text names, or null when it is no real day. */
export function parseDisplayDay(text: string): string | null {
  const match = /^\s*(\d{1,2})[/.-](\d{1,2})[/.-](\d{4})\s*$/.exec(text);
  if (!match) return null;
  const day = Number(match[1]);
  const month = Number(match[2]);
  const year = Number(match[3]);
  const date = new Date(year, month - 1, day, 12);
  if (
    date.getFullYear() !== year ||
    date.getMonth() !== month - 1 ||
    date.getDate() !== day
  ) {
    return null;
  }
  return toDayString(date);
}

export function daysBetween(from: string, to: string): number {
  const a = fromDayString(from);
  const b = fromDayString(to);
  if (!a || !b) return 0;
  return Math.round((b.getTime() - a.getTime()) / 86_400_000);
}

function fill(template: string, values: Record<string, string | number>) {
  return template.replace(/\{\{(\w+)\}\}/g, (_, key: string) =>
    String(values[key] ?? ""),
  );
}

/** A day as a typed `dd/mm/yyyy` field with a calendar to pick it from. */
export function DayField({
  id,
  label,
  copy,
  value,
  min,
  invalid,
  describedBy,
  onChange,
  onInvalidText,
}: {
  id: string;
  label: string;
  copy: ItemCreateCopy;
  value: string;
  /** `YYYY-MM-DD`: days before it are greyed in the calendar. */
  min?: string;
  invalid: boolean;
  describedBy?: string;
  onChange: (day: string) => void;
  /** The text typed is not a day (so the form can refuse to save it). */
  onInvalidText: (invalid: boolean) => void;
}) {
  const [text, setText] = useState(displayDay(value));
  const [open, setOpen] = useState(false);
  // A day set from outside (a quick pick, the calendar) shows in the text.
  useEffect(() => {
    setText((current) =>
      parseDisplayDay(current) === value ? current : displayDay(value),
    );
    onInvalidText(false);
  }, [value, onInvalidText]);

  return (
    <div className="relative">
      <input
        id={id}
        type="text"
        inputMode="numeric"
        autoComplete="off"
        value={text}
        placeholder={copy.dateFormat}
        aria-invalid={invalid ? true : undefined}
        aria-describedby={describedBy}
        onChange={(event) => {
          const next = event.target.value;
          setText(next);
          if (next.trim() === "") {
            onInvalidText(false);
            onChange("");
            return;
          }
          const parsed = parseDisplayDay(next);
          onInvalidText(parsed === null);
          if (parsed !== null) onChange(parsed);
        }}
        className={cn(
          FIELD_CLASS,
          "pr-10",
          invalid &&
            "border-danger ring-3 ring-danger-subtle focus-visible:border-danger focus-visible:ring-danger-subtle",
        )}
      />
      <Popover open={open} onOpenChange={setOpen}>
        <PopoverTrigger asChild>
          <button
            type="button"
            aria-label={`${copy.pickDate}: ${label}`}
            className="absolute top-1 right-1 grid size-8 place-items-center rounded-[7px] text-meta-foreground hover:bg-state-hover-strong hover:text-foreground focus-visible:outline-hidden focus-visible:ring-2 focus-visible:ring-focus-ring"
          >
            <CalendarDays aria-hidden="true" className="size-4" />
          </button>
        </PopoverTrigger>
        <PopoverContent align="end" className="w-auto p-0">
          <Calendar
            mode="single"
            selected={fromDayString(value) ?? undefined}
            defaultMonth={fromDayString(value) ?? undefined}
            disabled={
              min ? { before: fromDayString(min) ?? new Date(0) } : undefined
            }
            onSelect={(date) => {
              if (date) onChange(toDayString(date));
              setOpen(false);
            }}
          />
        </PopoverContent>
      </Popover>
    </div>
  );
}

const WEEKDAY_KEYS = ["sun", "mon", "tue", "wed", "thu", "fri", "sat"] as const;

function weekdayName(copy: ItemCreateCopy, index: number): string {
  return copy.weekdays[WEEKDAY_KEYS[index] ?? "sun"];
}

/** What the due-date hint says: weekday and days left, or that it is late. */
export function dueHint(
  copy: ItemCreateCopy,
  today: string,
  due: string,
): string {
  const date = fromDayString(due);
  if (!date) return "";
  const days = daysBetween(today, due);
  if (days === 0) return copy.dueTodayHint;
  if (days === 1) return copy.dueTomorrowHint;
  if (days < 0) return fill(copy.dueOverdueHint, { days: -days });
  return fill(copy.dueIn, { weekday: weekdayName(copy, date.getDay()), days });
}

/** The quick picks for the due date: today, tomorrow, the coming Friday, next week. */
export function QuickDue({
  copy,
  today,
  value,
  onPick,
}: {
  copy: ItemCreateCopy;
  today: string;
  value: string;
  onPick: (day: string) => void;
}) {
  const now = fromDayString(today) ?? new Date();
  const untilFriday = (5 - now.getDay() + 7) % 7 || 7;
  const friday = addDays(today, untilFriday);
  const fridayDate = fromDayString(friday) ?? now;
  const picks: ReadonlyArray<{ id: string; label: string; day: string }> = [
    { id: "today", label: copy.today, day: today },
    { id: "tomorrow", label: copy.tomorrow, day: addDays(today, 1) },
    {
      id: "friday",
      label: fill(copy.weekdayChip, {
        weekday: copy.weekdays.fri,
        date: `${String(fridayDate.getDate()).padStart(2, "0")}/${String(fridayDate.getMonth() + 1).padStart(2, "0")}`,
      }),
      day: friday,
    },
    { id: "next-week", label: copy.nextWeek, day: addDays(today, 7) },
  ];
  return (
    <>
      <span className="text-crumb text-meta-foreground">{copy.quickDue}</span>
      {picks.map((pick) => (
        <button
          key={pick.id}
          type="button"
          aria-pressed={value === pick.day}
          onClick={() => onPick(pick.day)}
          className={cn(
            "h-7 rounded-full border px-2.5 text-crumb font-medium focus-visible:outline-hidden focus-visible:ring-2 focus-visible:ring-focus-ring",
            value === pick.day
              ? "border-primary bg-brand-subtle text-brand-subtle-foreground"
              : "border-line-strong bg-popover text-muted-foreground hover:bg-state-hover-strong hover:text-foreground",
          )}
        >
          {pick.label}
        </button>
      ))}
    </>
  );
}

function PersonMark({
  member,
  size,
}: {
  member: ItemCreateMember;
  size: string;
}) {
  return (
    <span
      aria-hidden="true"
      className={cn(
        "grid shrink-0 place-items-center rounded-full text-mark-xs select-none",
        size,
        identityMarkClass(member.userId),
      )}
    >
      {member.initials}
    </span>
  );
}

/** People on the item: chips with a remove button, and a searchable picker. */
export function PeoplePicker({
  copy,
  members,
  value,
  currentUserId,
  onChange,
}: {
  copy: ItemCreateCopy;
  members: readonly ItemCreateMember[];
  value: readonly string[];
  currentUserId?: string | null;
  onChange: (next: string[]) => void;
}) {
  const labelId = useId();
  const [open, setOpen] = useState(false);
  const chosen = value
    .map((id) => members.find((member) => member.userId === id))
    .filter((member): member is ItemCreateMember => member !== undefined);
  const me = currentUserId
    ? members.find((member) => member.userId === currentUserId)
    : undefined;
  const toggle = (id: string) =>
    onChange(
      value.includes(id)
        ? value.filter((entry) => entry !== id)
        : [...value, id],
    );
  return (
    <div className="flex flex-col gap-1.5">
      <span
        id={labelId}
        className="text-crumb font-semibold text-muted-foreground"
      >
        {copy.assignees}{" "}
        <span className="font-normal text-meta-foreground">
          {copy.optional}
        </span>
      </span>
      {members.length === 0 ? (
        <p className="m-0 text-crumb text-muted-foreground">{copy.noMembers}</p>
      ) : (
        <div
          role="group"
          aria-labelledby={labelId}
          className="flex min-h-10 flex-wrap items-center gap-1.5"
        >
          {chosen.map((member) => (
            <span
              key={member.userId}
              className="inline-flex h-8 items-center gap-1.5 rounded-full border border-line-strong bg-popover pr-1 pl-1.5 text-label font-medium text-foreground"
            >
              <PersonMark member={member} size="size-5.5" />
              {member.name}
              <button
                type="button"
                aria-label={fill(copy.removePerson, { name: member.name })}
                onClick={() => toggle(member.userId)}
                className="grid size-6 place-items-center rounded-full text-meta-foreground hover:bg-state-hover-strong hover:text-foreground focus-visible:outline-hidden focus-visible:ring-2 focus-visible:ring-focus-ring"
              >
                <X aria-hidden="true" className="size-3.5" />
              </button>
            </span>
          ))}
          <Popover open={open} onOpenChange={setOpen}>
            <PopoverTrigger asChild>
              <button
                type="button"
                className="inline-flex h-8 items-center gap-1.5 rounded-full border border-dashed border-line-strong px-3 text-label font-semibold text-brand-subtle-foreground hover:bg-brand-subtle focus-visible:outline-hidden focus-visible:ring-2 focus-visible:ring-focus-ring"
              >
                <Plus aria-hidden="true" className="size-4" />
                {copy.addPerson}
              </button>
            </PopoverTrigger>
            <PopoverContent align="start" className="w-72 p-0">
              <Command>
                <CommandInput placeholder={copy.searchPeople} />
                <CommandList className="max-h-60">
                  <CommandEmpty>{copy.noPeopleFound}</CommandEmpty>
                  {members.map((member) => {
                    const on = value.includes(member.userId);
                    return (
                      <CommandItem
                        key={member.userId}
                        value={`${member.name} ${member.userId}`}
                        onSelect={() => toggle(member.userId)}
                      >
                        <PersonMark member={member} size="size-6" />
                        <span className="min-w-0 flex-1 truncate">
                          {member.name}
                        </span>
                        {on ? (
                          <Check
                            aria-label="✓"
                            className="size-4 text-primary"
                          />
                        ) : null}
                      </CommandItem>
                    );
                  })}
                </CommandList>
              </Command>
            </PopoverContent>
          </Popover>
          {me && !value.includes(me.userId) ? (
            <button
              type="button"
              onClick={() => toggle(me.userId)}
              className="h-8 rounded-full border border-line-strong px-3 text-label font-medium text-muted-foreground hover:bg-state-hover-strong focus-visible:outline-hidden focus-visible:ring-2 focus-visible:ring-focus-ring"
            >
              {copy.assignMe}
            </button>
          ) : null}
        </div>
      )}
    </div>
  );
}

/** The board's labels as toggles, each with the colour the board gave it. */
export function LabelPicker({
  copy,
  labels,
  value,
  onChange,
}: {
  copy: ItemCreateCopy;
  labels: readonly ItemCreateLabel[];
  value: readonly string[];
  onChange: (next: string[]) => void;
}) {
  const labelId = useId();
  if (labels.length === 0) return null;
  return (
    <div className="flex flex-col gap-1.5">
      <span
        id={labelId}
        className="text-crumb font-semibold text-muted-foreground"
      >
        {copy.labels}{" "}
        <span className="font-normal text-meta-foreground">
          {copy.optional}
        </span>
      </span>
      <div
        role="group"
        aria-labelledby={labelId}
        className="flex flex-wrap gap-1.5"
      >
        {labels.map((label) => {
          const on = value.includes(label.id);
          const tone = itemLabelTone({ name: label.name, color: label.color });
          return (
            <button
              key={label.id}
              type="button"
              aria-pressed={on}
              onClick={() =>
                onChange(
                  on
                    ? value.filter((entry) => entry !== label.id)
                    : [...value, label.id],
                )
              }
              className={cn(
                "inline-flex h-8 items-center gap-1.5 rounded-full border pr-3 pl-2.5 text-label focus-visible:outline-hidden focus-visible:ring-2 focus-visible:ring-focus-ring",
                on
                  ? "border-primary bg-brand-subtle font-semibold text-brand-subtle-foreground"
                  : "border-line-strong bg-popover font-medium text-foreground hover:bg-state-hover-strong",
              )}
            >
              {on ? (
                <Check aria-hidden="true" className="size-3.5" />
              ) : (
                <span
                  aria-hidden="true"
                  className={cn(
                    "size-2.5 rounded-full ring-1 ring-foreground/15 ring-inset",
                    // The tone's text colour (dark enough to read on its chip),
                    // not its pale chip background, which vanishes on the page.
                    tone
                      ? cn(
                          ITEM_LABEL_TONE_CLASS[tone].split(" ")[1],
                          "bg-current",
                        )
                      : "bg-muted-foreground",
                  )}
                />
              )}
              {label.name}
            </button>
          );
        })}
      </div>
    </div>
  );
}
