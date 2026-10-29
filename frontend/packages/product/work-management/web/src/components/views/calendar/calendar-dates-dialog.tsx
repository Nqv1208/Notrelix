import { useId, useState } from "react";
import { AlertCircle } from "lucide-react";
import {
  Button,
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@notrelix/ui-web";
import type { CalendarCopy } from "./calendar.copy";
import {
  formatDayMonth,
  type CalendarEntry,
  type DayKey,
} from "./calendar-model";

export interface CalendarDatesDialogProps {
  copy: CalendarCopy;
  entry: CalendarEntry;
  onSave: (startDay: DayKey, dueDay: DayKey) => void;
  onClose: () => void;
}

/**
 * "Đổi ngày": the keyboard way to move or resize an item. Start and due are
 * two date inputs; a due before the start is refused in place and Save stays
 * off until it is fixed.
 */
export function CalendarDatesDialog({
  copy,
  entry,
  onSave,
  onClose,
}: CalendarDatesDialogProps) {
  const [start, setStart] = useState<string>(entry.startDay);
  const [due, setDue] = useState<string>(entry.endDay);
  const errorId = useId();
  const text = copy.dates;
  const complete = Boolean(start && due);
  const invalid = complete && due < start;
  const inputClass =
    "h-9 w-full rounded-[8px] border border-line-strong bg-transparent px-2.5 text-compact aria-invalid:border-danger-text aria-invalid:outline-danger-text focus-visible:outline-2 focus-visible:outline-ring";
  return (
    <Dialog open onOpenChange={(open) => (open ? null : onClose())}>
      <DialogContent className="sm:max-w-sm">
        <DialogHeader>
          <DialogTitle>
            {text.title}{" "}
            <span className="font-mono text-sm font-medium text-muted-foreground">
              {entry.itemKey ?? entry.title}
            </span>
          </DialogTitle>
        </DialogHeader>
        <div className="grid grid-cols-2 gap-3">
          <label className="grid gap-1 text-crumb font-semibold text-muted-foreground">
            {text.start}
            <input
              type="date"
              value={start}
              className={inputClass}
              onChange={(event) => setStart(event.target.value)}
            />
          </label>
          <label className="grid gap-1 text-crumb font-semibold text-muted-foreground">
            {text.due}
            <input
              type="date"
              value={due}
              aria-invalid={invalid || undefined}
              aria-describedby={invalid ? errorId : undefined}
              className={inputClass}
              onChange={(event) => setDue(event.target.value)}
            />
          </label>
        </div>
        {invalid ? (
          <p
            id={errorId}
            role="alert"
            className="flex items-start gap-2 rounded-[8px] bg-danger-subtle px-2.5 py-2 text-crumb leading-[1.45] text-danger-text"
          >
            <AlertCircle
              className="mt-0.5 size-4 flex-none"
              aria-hidden="true"
            />
            <span>
              <b className="block font-semibold">{text.invalid}</b>
              {text.invalidHint.replace(
                "{{start}}",
                formatDayMonth(start as DayKey),
              )}
            </span>
          </p>
        ) : null}
        <DialogFooter>
          <Button type="button" variant="ghost" onClick={onClose}>
            {text.cancel}
          </Button>
          <Button
            type="button"
            disabled={!complete || invalid}
            onClick={() => {
              onSave(start as DayKey, due as DayKey);
              onClose();
            }}
          >
            {text.save}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
