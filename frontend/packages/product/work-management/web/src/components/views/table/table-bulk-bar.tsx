import { useState } from "react";
import { ArrowRight, Calendar, Trash2, User, X } from "lucide-react";
import {
  cn,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@notrelix/ui-web";
import {
  fillTableCopy,
  pluralTableCopy,
  type MainTableCopy,
} from "./main-table.copy";
import type { TableStatusOption } from "./table-model";
import { StatusDot } from "./table-parts";

const BAR_CLASS =
  "absolute bottom-5.5 left-1/2 z-20 flex -translate-x-1/2 items-center gap-1 rounded-[12px] bg-inverse-surface py-1.5 pr-1.5 pl-3.5 whitespace-nowrap text-inverse-foreground shadow-bar";

const BAR_BUTTON_CLASS =
  "inline-flex h-8 cursor-pointer items-center gap-1.5 rounded-md border-0 bg-transparent px-2.5 text-compact font-medium text-inverse-foreground hover:bg-white/10 focus-visible:outline-2 focus-visible:outline-inverse-foreground disabled:cursor-default disabled:opacity-50 disabled:hover:bg-transparent";

export interface TableBulkBarProps {
  copy: MainTableCopy;
  count: number;
  statusOptions: readonly TableStatusOption[];
  /** The viewer may change status and due date. */
  canEdit: boolean;
  /** The viewer may archive items. */
  canArchive: boolean;
  onStatus: (optionId: string) => void;
  /** "YYYY-MM-DD", or null to clear the due date. */
  onDue: (day: string | null) => void;
  onArchive: () => void;
  onClear: () => void;
}

/**
 * The bar over the bottom edge of the table while rows are selected: the
 * count, then Trạng thái, Giao cho, Đặt hạn, Chuyển board, Lưu trữ and close.
 * "Giao cho" and "Chuyển board" have no write behind them yet, so they stay
 * disabled and say so.
 */
export function TableBulkBar({
  copy,
  count,
  statusOptions,
  canEdit,
  canArchive,
  onStatus,
  onDue,
  onArchive,
  onClear,
}: TableBulkBarProps) {
  const b = copy.bulk;
  const [dueOpen, setDueOpen] = useState(false);
  const [day, setDay] = useState("");

  return (
    <div
      role="toolbar"
      aria-label={fillTableCopy(b.label, { count })}
      data-table-bulk-bar=""
      className={BAR_CLASS}
    >
      <b className="mr-2 text-dense-body font-semibold">
        {fillTableCopy(b.selected, { count })}
      </b>

      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <button
            type="button"
            disabled={!canEdit}
            className={BAR_BUTTON_CLASS}
          >
            <StatusDot category="in_progress" />
            {b.status}
          </button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="start" side="top" className="w-48">
          {statusOptions.map((option) => (
            <DropdownMenuItem
              key={option.id}
              onSelect={() => onStatus(option.id)}
            >
              <StatusDot category={option.category} />
              {option.name}
            </DropdownMenuItem>
          ))}
        </DropdownMenuContent>
      </DropdownMenu>

      <button
        type="button"
        aria-disabled="true"
        title={b.unavailable}
        aria-label={`${b.assign}: ${b.unavailable}`}
        className={BAR_BUTTON_CLASS}
      >
        <User className="size-4" aria-hidden="true" />
        {b.assign}
      </button>

      <Popover open={dueOpen} onOpenChange={setDueOpen}>
        <PopoverTrigger asChild>
          <button
            type="button"
            disabled={!canEdit}
            className={BAR_BUTTON_CLASS}
          >
            <Calendar className="size-4" aria-hidden="true" />
            {b.due}
          </button>
        </PopoverTrigger>
        <PopoverContent
          align="start"
          side="top"
          aria-label={b.due}
          className="w-60 gap-2 rounded-[10px] border border-line-strong bg-popover p-3 shadow-pop ring-0 dark:bg-card"
        >
          <label className="flex flex-col gap-1 text-compact text-foreground">
            {b.dueLabel}
            <input
              type="date"
              value={day}
              onChange={(event) => setDay(event.target.value)}
              className="h-8 rounded-[7px] border border-line-strong bg-background px-2 text-compact text-foreground"
            />
          </label>
          <div className="flex justify-end gap-2">
            <button
              type="button"
              className="h-8 cursor-pointer rounded-[7px] border border-line-strong bg-popover px-3 text-compact font-medium text-foreground hover:bg-state-hover-strong dark:bg-card"
              onClick={() => {
                setDueOpen(false);
                setDay("");
                onDue(null);
              }}
            >
              {b.dueClear}
            </button>
            <button
              type="button"
              disabled={day === ""}
              className="h-8 cursor-pointer rounded-[7px] bg-primary px-3 text-compact font-semibold text-primary-foreground disabled:cursor-default disabled:opacity-50"
              onClick={() => {
                setDueOpen(false);
                onDue(day);
                setDay("");
              }}
            >
              {b.dueApply}
            </button>
          </div>
        </PopoverContent>
      </Popover>

      <button
        type="button"
        aria-disabled="true"
        title={b.unavailable}
        aria-label={`${b.move}: ${b.unavailable}`}
        className={BAR_BUTTON_CLASS}
      >
        <ArrowRight className="size-4" aria-hidden="true" />
        {b.move}
      </button>

      <span aria-hidden="true" className="mx-1 h-4.5 w-px bg-white/18" />

      <button
        type="button"
        disabled={!canArchive}
        onClick={onArchive}
        className={cn(BAR_BUTTON_CLASS, "text-inverse-danger")}
      >
        <Trash2 className="size-4" aria-hidden="true" />
        {b.archive}
      </button>

      <button
        type="button"
        aria-label={b.clear}
        onClick={onClear}
        className={BAR_BUTTON_CLASS}
      >
        <X className="size-4" aria-hidden="true" />
      </button>
    </div>
  );
}

/** The notice after archiving: the count, "Hoàn tác" when it can be undone, close. */
export function TableArchivedNotice({
  copy,
  count,
  onUndo,
  onClose,
}: {
  copy: MainTableCopy;
  count: number;
  /** Undoing is offered only when the archive can still be taken back. */
  onUndo?: () => void;
  onClose: () => void;
}) {
  const a = copy.archived;
  return (
    <div
      role="status"
      aria-live="polite"
      data-table-archived=""
      className={BAR_CLASS}
    >
      <b className="mr-2 text-dense-body font-semibold">
        {pluralTableCopy({ one: a.done_one, other: a.done_other }, count)}
      </b>
      {onUndo ? (
        <button type="button" onClick={onUndo} className={BAR_BUTTON_CLASS}>
          {a.undo}
        </button>
      ) : null}
      <button
        type="button"
        aria-label={a.close}
        onClick={onClose}
        className={BAR_BUTTON_CLASS}
      >
        <X className="size-4" aria-hidden="true" />
      </button>
    </div>
  );
}
