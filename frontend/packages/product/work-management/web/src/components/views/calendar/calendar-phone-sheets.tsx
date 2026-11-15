import { useState, type ReactNode } from "react";
import {
  ArrowRight,
  CalendarDays,
  Check,
  ChevronDown,
  ChevronLeft,
  ChevronRight,
  ExternalLink,
  Info,
  X,
} from "lucide-react";
import {
  cn,
  Sheet,
  SheetContent,
  SheetDescription,
  SheetTitle,
} from "@notrelix/ui-web";
import { calendarHeading, weekdayName } from "./calendar-format";
import {
  addDays,
  formatDayMonth,
  formatFullDay,
  formatMinutes,
  monthWeeks,
  parseDayKey,
  sameMonth,
  startOfWeek,
  stepAnchor,
  type CalendarEntry,
  type CalendarStatusMenu,
  type CalendarTone,
  type CalendarUndated,
  type DayKey,
} from "./calendar-model";
import type { CalendarCopy } from "./calendar.copy";

function fill(
  message: string,
  values: Readonly<Record<string, string | number>>,
) {
  return message.replace(/\{\{(\w+)\}\}/g, (match, key: string) =>
    key in values ? String(values[key]) : match,
  );
}

const DOT: Readonly<Record<CalendarTone, string>> = {
  todo: "border-2 border-work-status-todo",
  doing: "bg-work-status-doing",
  review: "bg-calendar-dot-review",
  done: "bg-work-status-done",
  overdue: "bg-destructive",
};

const SHEET =
  "box-border max-h-[92dvh] gap-0 overflow-y-auto rounded-t-[14px] border-0 data-[side=bottom]:border-t-0 bg-popover p-0 pt-1.5 pb-8.5 font-body text-[15px] leading-[1.45] text-foreground shadow-[0_-8px_32px_rgba(0,0,0,.18)] dark:bg-card";

const BUTTON =
  "box-border inline-flex min-h-11 flex-none cursor-pointer items-center justify-center gap-1.5 rounded-[12px] border border-line-strong bg-popover px-4 text-[15px] font-medium whitespace-nowrap text-foreground focus-visible:outline-2 focus-visible:outline-ring dark:bg-card";

const CHIP =
  "box-border inline-flex h-11 flex-none cursor-pointer items-center gap-1.5 rounded-[12px] border border-line-strong bg-popover px-3.5 text-[14px] font-medium whitespace-nowrap text-foreground focus-visible:outline-2 focus-visible:outline-ring aria-pressed:border-foreground aria-pressed:bg-foreground aria-pressed:font-semibold aria-pressed:text-background dark:bg-card dark:aria-pressed:bg-foreground";

const ICON_BUTTON =
  "grid size-11 flex-none cursor-pointer place-items-center rounded-[12px] border-0 bg-transparent p-0 text-foreground focus-visible:outline-2 focus-visible:outline-ring";

function Grabber() {
  return (
    <span
      aria-hidden="true"
      className="mx-auto mb-0.5 block h-1.25 w-9 rounded-[3px] bg-line-strong"
    />
  );
}

function CloseButton({
  label,
  onClick,
}: {
  label: string;
  onClick: () => void;
}) {
  return (
    <button
      type="button"
      aria-label={label}
      onClick={onClick}
      className={ICON_BUTTON}
    >
      <X className="size-5" aria-hidden="true" />
    </button>
  );
}

const TIMES = Array.from({ length: 21 }, (_, index) => 480 + index * 30);

function DatePicker({
  copy,
  locale,
  today,
  itemKey,
  onSave,
  onCancel,
  zoneLabel,
}: {
  copy: CalendarCopy;
  locale: string;
  today: DayKey;
  itemKey: string;
  zoneLabel: string;
  onSave: (day: DayKey, minutes: number) => void;
  onCancel: () => void;
}) {
  const t = copy.phone;
  const nextMonday = addDays(startOfWeek(today), 7);
  const [picked, setPicked] = useState<DayKey>(nextMonday);
  const [shown, setShown] = useState<DayKey>(nextMonday);
  const [minutes, setMinutes] = useState(17 * 60);
  const heading = calendarHeading("month", shown, copy, locale);
  const quick: ReadonlyArray<readonly [DayKey, string]> = [
    [today, copy.today],
    [addDays(today, 1), t.tomorrow],
    [
      nextMonday,
      fill(t.nextMonday, {
        weekday: weekdayName(nextMonday, locale),
        date: formatDayMonth(nextMonday),
      }),
    ],
  ];
  const pick = (day: DayKey) => {
    setPicked(day);
    setShown(day);
  };
  return (
    <div
      id="calendar-due-picker"
      role="group"
      aria-label={fill(t.setDueFor, { key: itemKey })}
      className="px-4 pt-1.5"
    >
      <div
        role="group"
        aria-label={t.quickPick}
        className="-mx-4 mt-2.5 mb-1.5 flex gap-2 overflow-x-auto px-4 [scrollbar-width:none]"
      >
        {quick.map(([day, label]) => (
          <button
            key={day}
            type="button"
            aria-pressed={picked === day}
            onClick={() => pick(day)}
            className={CHIP}
          >
            {label}
          </button>
        ))}
      </div>
      <div className="-mx-1.5 flex h-11 items-center justify-between">
        <button
          type="button"
          aria-label={copy.previous.month}
          onClick={() => setShown(stepAnchor("month", shown, -1))}
          className={ICON_BUTTON}
        >
          <ChevronLeft className="size-5" aria-hidden="true" />
        </button>
        <h3
          id="calendar-due-month"
          className="m-0 text-[15px] leading-[1.45] font-[650] tabular-nums"
        >
          {heading}
        </h3>
        <button
          type="button"
          aria-label={copy.next.month}
          onClick={() => setShown(stepAnchor("month", shown, 1))}
          className={ICON_BUTTON}
        >
          <ChevronRight className="size-5" aria-hidden="true" />
        </button>
      </div>
      <div role="grid" aria-labelledby="calendar-due-month">
        <div role="row" className="grid h-5.5 grid-cols-7">
          {(["mon", "tue", "wed", "thu", "fri", "sat", "sun"] as const).map(
            (key) => (
              <span
                key={key}
                role="columnheader"
                className="grid place-items-center text-[12px] font-medium text-meta-foreground"
              >
                {t.weekdaysShort[key]}
              </span>
            ),
          )}
        </div>
        {monthWeeks(shown).map((days) => (
          <div key={days[0]} role="row" className="grid grid-cols-7">
            {days.map((day) => (
              <span
                key={day}
                role="gridcell"
                aria-selected={day === picked}
                className="grid place-items-center"
              >
                <button
                  type="button"
                  aria-label={`${weekdayName(day, locale)} ${formatFullDay(day)}${day === today ? `, ${copy.todayMarker.toLocaleLowerCase(locale)}` : ""}`}
                  aria-current={day === today ? "date" : undefined}
                  onClick={() => pick(day)}
                  className={cn(
                    "h-10.5 w-11 cursor-pointer rounded-[50%] border-0 bg-transparent p-0 text-[15px] tabular-nums focus-visible:outline-2 focus-visible:outline-ring",
                    !sameMonth(day, shown) && "text-meta-foreground",
                    day === today &&
                      "font-[650] text-state-selected-foreground ring-[1.5px] ring-primary ring-inset",
                    day === picked &&
                      "bg-primary font-[650] text-primary-foreground ring-0",
                  )}
                >
                  {parseDayKey(day).day}
                </button>
              </span>
            ))}
          </div>
        ))}
      </div>
      <div className="mt-1.5 flex items-center gap-2.5 pt-2.25 text-[14px]">
        <label htmlFor="calendar-due-time" className="font-semibold">
          {t.dueTime}
        </label>
        <span className="relative inline-flex">
          <select
            id="calendar-due-time"
            value={minutes}
            onChange={(event) => setMinutes(Number(event.target.value))}
            className={cn(
              BUTTON,
              "w-21.5 appearance-none pr-8 pl-3 tabular-nums",
            )}
          >
            {TIMES.map((value) => (
              <option key={value} value={value}>
                {formatMinutes(value)}
              </option>
            ))}
          </select>
          <ChevronDown
            className="pointer-events-none absolute top-1/2 right-3 size-3.5 -translate-y-1/2"
            aria-hidden="true"
          />
        </span>
        <small className="text-[13px] text-meta-foreground">{zoneLabel}</small>
      </div>
      <div className="mt-2.5 flex items-center gap-2.5">
        <span className="min-w-0 flex-1 text-[13px] leading-[1.3] text-muted-foreground">
          <b className="block text-[15px] font-[650] text-foreground">
            {fill(t.dueSummary, {
              weekday: weekdayName(picked, locale),
              date: formatDayMonth(picked),
              time: formatMinutes(minutes),
            })}
          </b>
          {t.showsOnSave}
        </span>
        <button type="button" onClick={onCancel} className={BUTTON}>
          {t.cancel}
        </button>
        <button
          type="button"
          onClick={() => onSave(picked, minutes)}
          className={cn(
            BUTTON,
            "min-w-26 border-primary bg-primary font-semibold text-primary-foreground dark:bg-primary",
          )}
        >
          {t.saveDue}
        </button>
      </div>
    </div>
  );
}

export interface CalendarDueSheetProps {
  copy: CalendarCopy;
  locale: string;
  today: DayKey;
  zoneLabel: string;
  open: boolean;
  onOpenChange: (open: boolean) => void;
  undatedTotal: number;
  undated: readonly CalendarUndated[];
  filterActive: boolean;
  canEdit: boolean;
  onSetDue: (id: string, day: DayKey, minutes: number) => void;
}

/**
 * `Final-M-Calendar` `chua-co-ngay`: the undated items in a sheet from the bottom. "Đặt hạn" on an item opens the
 * date picker under it (quick days, month, due time); "Lưu hạn" writes the day and time. A viewer reads the list only.
 */
export function CalendarDueSheet({
  copy,
  locale,
  today,
  zoneLabel,
  open,
  onOpenChange,
  undatedTotal,
  undated,
  filterActive,
  canEdit,
  onSetDue,
}: CalendarDueSheetProps) {
  const t = copy.phone;
  const [pickingId, setPickingId] = useState<string | null>(null);
  const hidden = undatedTotal - undated.length;
  const close = () => {
    setPickingId(null);
    onOpenChange(false);
  };
  return (
    <Sheet open={open} onOpenChange={(next) => (next ? null : close())}>
      <SheetContent
        side="bottom"
        showCloseButton={false}
        data-phone-tray=""
        className={SHEET}
      >
        <Grabber />
        <div className="flex min-h-13 items-center gap-2 pr-2 pl-5">
          <SheetTitle className="m-0 flex-1 text-[17px] leading-[1.45] font-[650]">
            {t.dueSheetTitle}{" "}
            <span
              data-phone-tray-count=""
              className="text-[15px] font-medium text-meta-foreground"
            >
              {undatedTotal}
            </span>
          </SheetTitle>
          <CloseButton label={t.close} onClick={close} />
        </div>
        <SheetDescription className="mx-5 -mt-1.5 mb-2 text-[13px] text-muted-foreground">
          {undated.length > 0 ? t.dueSheetLead : t.trayEmpty}
        </SheetDescription>
        {undated.map((item) => {
          const name = item.itemKey ?? item.title;
          const tone = item.tone ?? "todo";
          const picking = pickingId === item.id;
          return (
            <div key={item.id}>
              <div
                data-phone-tray-item={item.id}
                className="mx-4 box-border flex items-center gap-2.5 rounded-[12px] border border-line py-2.5 pr-2 pl-3.5"
              >
                <div className="min-w-0 flex-1">
                  <span className="flex min-h-5.5 items-center gap-1.5 text-[12px] text-muted-foreground">
                    <span
                      aria-hidden="true"
                      className={cn("size-2 flex-none rounded-full", DOT[tone])}
                    />
                    {item.itemKey ? (
                      <span className="font-mono text-[12px]">
                        {item.itemKey}
                      </span>
                    ) : null}
                  </span>
                  <span className="mt-0.5 block text-[15px] leading-[1.36] font-semibold">
                    {item.title}
                  </span>
                  <small className="mt-0.5 block text-[13px] text-meta-foreground">
                    {copy.legend[tone]}
                    {copy.separator}
                    {item.assigneeName ?? copy.undated.unassigned}
                  </small>
                </div>
                {canEdit ? (
                  <button
                    type="button"
                    aria-expanded={picking}
                    aria-controls={picking ? "calendar-due-picker" : undefined}
                    aria-label={fill(t.setDueFor, { key: name })}
                    onClick={() => setPickingId(picking ? null : item.id)}
                    className={cn(
                      BUTTON,
                      "px-3",
                      picking &&
                        "border-primary font-semibold text-state-selected-foreground",
                    )}
                  >
                    <CalendarDays className="size-3.5" aria-hidden="true" />
                    {t.setDueButton}
                  </button>
                ) : null}
              </div>
              {picking ? (
                <DatePicker
                  copy={copy}
                  locale={locale}
                  today={today}
                  itemKey={name}
                  zoneLabel={zoneLabel}
                  onCancel={() => setPickingId(null)}
                  onSave={(day, minutes) => {
                    onSetDue(item.id, day, minutes);
                    close();
                  }}
                />
              ) : null}
            </div>
          );
        })}
        {!canEdit && undated.length > 0 ? (
          <p
            role="note"
            data-phone-tray-note=""
            className="mx-4 mt-2 mb-0 rounded-[10px] bg-state-hover px-3 py-2.5 text-[13px] text-muted-foreground"
          >
            {t.trayReadOnly}
          </p>
        ) : null}
        {filterActive && hidden > 0 ? (
          <p className="mx-5 mt-2 mb-0 text-[12.5px] text-meta-foreground">
            {hidden === 1
              ? copy.undated.hiddenByFilter_one
              : copy.undated.hiddenByFilter_other}
          </p>
        ) : null}
      </SheetContent>
    </Sheet>
  );
}

function Row({
  icon,
  label,
  end,
  onClick,
  haspopup,
  checked,
}: {
  icon: ReactNode;
  label: string;
  end: ReactNode;
  onClick: () => void;
  haspopup?: boolean;
  checked?: boolean;
}) {
  return (
    <button
      type="button"
      role={checked === undefined ? "menuitem" : "menuitemradio"}
      aria-checked={checked}
      aria-haspopup={haspopup ? "dialog" : undefined}
      onClick={onClick}
      className="box-border flex min-h-13 w-full cursor-pointer items-center gap-3 border-0 bg-transparent px-5 text-left text-foreground hover:bg-state-hover focus-visible:outline-2 focus-visible:outline-ring"
    >
      <span
        aria-hidden="true"
        className="box-border grid size-9 flex-none place-items-center rounded-[10px] border border-line bg-background text-muted-foreground"
      >
        {icon}
      </span>
      <span className="min-w-0 flex-1 text-[15px] font-medium">{label}</span>
      <span className="inline-flex flex-none items-center gap-1.5 text-[13px] text-meta-foreground">
        {end}
      </span>
    </button>
  );
}

export interface CalendarActionSheetProps {
  copy: CalendarCopy;
  locale: string;
  entry: CalendarEntry | null;
  canEdit: boolean;
  onClose: () => void;
  onOpenItem: (id: string) => void;
  /** "Đổi hạn…": the container opens the dates dialog. */
  onChangeDue?: ((entry: CalendarEntry) => void) | undefined;
  /** "Chuyển sang…": absent when the board has no status field to write. */
  statusMenu?: CalendarStatusMenu | undefined;
}

/**
 * `Final-M-Calendar` `doi-han`: the "···" of an agenda row opens the item's actions from the bottom: "Đổi hạn…"
 * and "Mở việc", and the note that a due date does not move by dragging on a phone.
 */
export function CalendarActionSheet({
  copy,
  locale,
  entry,
  canEdit,
  onClose,
  onOpenItem,
  onChangeDue,
  statusMenu,
}: CalendarActionSheetProps) {
  const t = copy.phone;
  const [picking, setPicking] = useState(false);
  const current = entry ? statusMenu?.currentOf(entry.id) : null;
  const currentOption = statusMenu?.options.find((o) => o.id === current);
  const close = () => {
    setPicking(false);
    onClose();
  };
  const due = entry
    ? entry.dueMinutes != null
      ? fill(t.dueAtTime, {
          day: `${weekdayName(entry.endDay, locale)} ${formatDayMonth(entry.endDay)}`,
          time: formatMinutes(entry.dueMinutes),
        })
      : fill(t.dueAt, {
          day: `${weekdayName(entry.endDay, locale)} ${formatDayMonth(entry.endDay)}`,
        })
    : "";
  const action = t.actionChangeDue;
  const [before, after] = fill(t.noDragNote, { action: "\u0000" }).split(
    "\u0000",
  );
  return (
    <Sheet
      open={entry !== null}
      onOpenChange={(next) => (next ? null : close())}
    >
      <SheetContent
        side="bottom"
        showCloseButton={false}
        data-phone-actions=""
        className={SHEET}
      >
        <Grabber />
        {entry ? (
          <>
            <div className="flex min-h-13 items-start gap-2 pt-1 pr-2 pl-5">
              <div className="min-w-0 flex-1 pt-1.5">
                <span className="flex items-center gap-1.5 text-[13px] text-meta-foreground">
                  {entry.itemKey ? (
                    <span className="font-mono text-[12px]">
                      {entry.itemKey}
                    </span>
                  ) : null}
                  <span
                    aria-hidden="true"
                    className={cn(
                      "size-2 flex-none rounded-full",
                      DOT[entry.tone],
                    )}
                  />
                  {copy.legend[entry.tone]}
                  {copy.separator}
                  {due}
                </span>
                <SheetTitle className="m-0 text-[16px] leading-[1.35] font-[650]">
                  {entry.title}
                </SheetTitle>
              </div>
              <CloseButton label={t.close} onClick={close} />
            </div>
            <SheetDescription className="sr-only">{due}</SheetDescription>
            {picking && statusMenu ? (
              <div role="menu" aria-label={t.actionMove}>
                {statusMenu.options.map((option) => (
                  <Row
                    key={option.id}
                    checked={option.id === current}
                    icon={
                      <span
                        aria-hidden="true"
                        className="size-2.5 rounded-full"
                        style={{ background: option.color }}
                      />
                    }
                    label={option.label}
                    end={
                      option.id === current ? (
                        <Check className="size-5" aria-hidden="true" />
                      ) : null
                    }
                    onClick={() => {
                      if (option.id !== current)
                        statusMenu.onChange(entry, option.id);
                      close();
                    }}
                  />
                ))}
              </div>
            ) : (
              <div role="menu" aria-label={entry.title}>
                {canEdit && onChangeDue ? (
                  <Row
                    haspopup
                    icon={<CalendarDays className="size-5" />}
                    label={action}
                    end={
                      entry.dueMinutes != null
                        ? `${formatDayMonth(entry.endDay)}${copy.separator}${formatMinutes(entry.dueMinutes)}`
                        : formatDayMonth(entry.endDay)
                    }
                    onClick={() => {
                      onChangeDue(entry);
                      close();
                    }}
                  />
                ) : null}
                {canEdit && statusMenu ? (
                  <Row
                    haspopup
                    icon={<ArrowRight className="size-5" />}
                    label={t.actionMove}
                    end={
                      currentOption ? (
                        <>
                          <span
                            aria-hidden="true"
                            className="size-2 rounded-full"
                            style={{ background: currentOption.color }}
                          />
                          {currentOption.label}
                        </>
                      ) : null
                    }
                    onClick={() => setPicking(true)}
                  />
                ) : null}
                <Row
                  icon={<ExternalLink className="size-5" />}
                  label={t.actionOpen}
                  end={<ChevronRight className="size-5" aria-hidden="true" />}
                  onClick={() => {
                    close();
                    onOpenItem(entry.id);
                  }}
                />
              </div>
            )}
            {!picking && canEdit && onChangeDue ? (
              <p className="mx-5 mt-1.5 mb-2 flex items-start gap-2 rounded-[10px] bg-state-hover px-3 py-2.5 text-[13px] leading-[1.4] text-muted-foreground">
                <Info className="mt-px size-3.5 flex-none" aria-hidden="true" />
                <span>
                  {before}
                  <b className="font-bold">{action}</b>
                  {after}
                </span>
              </p>
            ) : null}
          </>
        ) : null}
      </SheetContent>
    </Sheet>
  );
}
