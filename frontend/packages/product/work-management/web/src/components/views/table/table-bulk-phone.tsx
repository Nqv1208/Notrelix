import { useState, type ReactNode } from "react";
import {
  Archive,
  ArrowRight,
  Calendar,
  Check,
  ChevronLeft,
  Flag,
  Loader2,
  Trash2,
  User,
  X,
} from "lucide-react";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  cn,
  Sheet,
  SheetContent,
  SheetTitle,
} from "@notrelix/ui-web";
import { fillTableCopy } from "./main-table.copy";
import type { TableBulkBarProps } from "./table-bulk-bar";
import { StatusDot } from "./table-parts";

type View = "menu" | "status" | "priority" | "assign" | "due" | "move";

/** A 44 px row of the action sheet (BC-Bulk `bk320-sheet`): an icon and the label. */
function ActionRow({
  icon,
  label,
  danger,
  disabled,
  onClick,
}: {
  icon?: ReactNode;
  label: string;
  danger?: boolean;
  disabled?: boolean;
  onClick: () => void;
}) {
  return (
    <button
      type="button"
      disabled={disabled}
      onClick={onClick}
      className={cn(
        "flex h-11 w-full cursor-pointer items-center gap-3 rounded-[7px] border-0 bg-transparent px-2.5 py-1.5 text-left text-[13.5px] text-foreground hover:bg-state-hover focus-visible:outline-2 focus-visible:outline-ring disabled:cursor-default disabled:opacity-50",
        danger && "text-danger-text",
      )}
    >
      <span
        className="grid size-4 flex-none place-items-center"
        aria-hidden="true"
      >
        {icon}
      </span>
      <span className="min-w-0 flex-1 truncate">{label}</span>
    </button>
  );
}

/**
 * The phone drawing of the bulk bar (BC-Bulk `bk320-R`, `bk320-cap`, `bk320-sheet`): the count, one
 * "Hành động" button and close on a dark strip at the foot; the button opens a bottom sheet with
 * every action as a 44 px row. Past the limit the button is disabled. Picking a value (status,
 * priority, assignee, due date, board) shows its list in the same sheet, with a way back.
 */
export function TableBulkPhoneBar({
  copy,
  count,
  over,
  saving,
  statusOptions,
  canEdit,
  canArchive,
  onStatus,
  onDue,
  onArchive,
  onDelete,
  onClear,
  priority,
  assign,
  move,
}: Pick<
  TableBulkBarProps,
  | "copy"
  | "count"
  | "statusOptions"
  | "canEdit"
  | "canArchive"
  | "onStatus"
  | "onDue"
  | "onArchive"
  | "onDelete"
  | "onClear"
  | "priority"
  | "assign"
  | "move"
> & { over: boolean; saving: boolean }) {
  const b = copy.bulk;
  const [open, setOpen] = useState(false);
  const [view, setView] = useState<View>("menu");
  const [day, setDay] = useState("");
  const [confirmDelete, setConfirmDelete] = useState(false);
  const canMove = canEdit && move !== undefined && move.boards.length > 0;
  const close = () => {
    setOpen(false);
    setView("menu");
    setDay("");
  };
  const done = (act: () => void) => () => {
    close();
    act();
  };
  const titles: Record<View, string> = {
    menu: fillTableCopy(b.sheetTitle, { count }),
    status: b.sheetStatus,
    priority: b.sheetPriority,
    assign: b.sheetAssign,
    due: b.sheetDue,
    move: b.move,
  };

  return (
    <div
      role="toolbar"
      aria-label={fillTableCopy(b.label, { count })}
      aria-busy={saving}
      data-table-bulk-bar=""
      data-table-bulk-phone=""
      className="pointer-events-auto flex h-15 w-full items-center gap-2 bg-inverse-surface py-2 pr-2 pl-4 text-inverse-foreground shadow-bar"
    >
      <b className="min-w-0 flex-1 truncate text-[14px] font-semibold">
        {fillTableCopy(b.selected, { count })}
      </b>
      {saving ? (
        <span
          role="status"
          className="inline-flex items-center gap-2 text-compact"
        >
          <Loader2
            aria-hidden="true"
            className="size-4 animate-spin motion-reduce:animate-none"
          />
          {fillTableCopy(b.saving, { count })}
        </span>
      ) : (
        <>
          <button
            type="button"
            disabled={over}
            aria-haspopup="dialog"
            onClick={() => setOpen(true)}
            className="inline-flex h-11 cursor-pointer items-center rounded-lg border-0 bg-primary px-4 text-[13px] font-semibold text-primary-foreground focus-visible:outline-2 focus-visible:outline-inverse-foreground disabled:cursor-default disabled:bg-transparent disabled:text-inverse-foreground/70"
          >
            {b.sheetOpen}
          </button>
          <button
            type="button"
            aria-label={b.clear}
            onClick={onClear}
            className="grid size-11 cursor-pointer place-items-center rounded-lg border-0 bg-transparent text-inverse-foreground focus-visible:outline-2 focus-visible:outline-inverse-foreground"
          >
            <X className="size-4" aria-hidden="true" />
          </button>
        </>
      )}

      <Sheet
        open={open}
        onOpenChange={(next) => (next ? setOpen(true) : close())}
      >
        <SheetContent
          side="bottom"
          showCloseButton={false}
          aria-describedby={undefined}
          className="max-h-[94%] gap-0 overflow-y-auto rounded-t-[16px] bg-popover px-2 pt-2 pb-4 text-popover-foreground data-[side=bottom]:border-t-0"
        >
          <div
            aria-hidden="true"
            className="mx-auto mb-1.5 h-1 w-9 shrink-0 rounded-[2px] bg-line-strong"
          />
          <div className="flex h-11 shrink-0 items-center gap-1 pr-2 pl-3">
            {view !== "menu" ? (
              <button
                type="button"
                aria-label={b.sheetBack}
                onClick={() => setView("menu")}
                className="-ml-3 grid size-11 flex-none cursor-pointer place-items-center rounded-[7px] border-0 bg-transparent text-muted-foreground focus-visible:outline-2 focus-visible:outline-ring"
              >
                <ChevronLeft className="size-5" aria-hidden="true" />
              </button>
            ) : null}
            <SheetTitle className="m-0 min-w-0 flex-1 truncate text-[16px] leading-[1.45] font-bold">
              {titles[view]}
            </SheetTitle>
            <button
              type="button"
              aria-label={b.sheetClose}
              onClick={close}
              className="grid size-11 flex-none cursor-pointer place-items-center rounded-[7px] border-0 bg-transparent text-muted-foreground focus-visible:outline-2 focus-visible:outline-ring"
            >
              <X className="size-4" aria-hidden="true" />
            </button>
          </div>
          <div className="flex flex-col">
            {view === "menu" ? (
              <>
                <ActionRow
                  icon={<Check className="size-4" />}
                  label={b.sheetStatus}
                  disabled={!canEdit}
                  onClick={() => setView("status")}
                />
                {priority ? (
                  <ActionRow
                    icon={<Flag className="size-4" />}
                    label={b.sheetPriority}
                    disabled={!canEdit}
                    onClick={() => setView("priority")}
                  />
                ) : null}
                <ActionRow
                  icon={<User className="size-4" />}
                  label={b.sheetAssign}
                  disabled={!canEdit || !assign}
                  onClick={() => setView("assign")}
                />
                <ActionRow
                  icon={<Calendar className="size-4" />}
                  label={b.sheetDue}
                  disabled={!canEdit}
                  onClick={() => setView("due")}
                />
                <ActionRow
                  icon={<Calendar className="size-4" />}
                  label={b.moreDueClear}
                  disabled={!canEdit}
                  onClick={done(() => onDue(null))}
                />
                <ActionRow
                  icon={<User className="size-4" />}
                  label={b.moreUnassign}
                  disabled={!canEdit || !assign}
                  onClick={done(() => assign?.onAssign(null))}
                />
                <ActionRow
                  icon={<ArrowRight className="size-4" />}
                  label={b.moreMove}
                  disabled={!canMove}
                  onClick={() => setView("move")}
                />
                <ActionRow
                  icon={<Archive className="size-4" />}
                  label={b.archive}
                  disabled={!canArchive}
                  onClick={done(onArchive)}
                />
                {onDelete ? (
                  <ActionRow
                    danger
                    icon={<Trash2 className="size-4" />}
                    label={b.delete}
                    onClick={() => setConfirmDelete(true)}
                  />
                ) : null}
              </>
            ) : null}
            {view === "status"
              ? statusOptions.map((option) => (
                  <ActionRow
                    key={option.id}
                    icon={<StatusDot category={option.category} />}
                    label={option.name}
                    onClick={done(() => onStatus(option.id))}
                  />
                ))
              : null}
            {view === "priority" && priority
              ? priority.options.map((option) => (
                  <ActionRow
                    key={option.id}
                    icon={<Flag className="size-4" />}
                    label={option.label}
                    onClick={done(() => priority.onPriority(option.id))}
                  />
                ))
              : null}
            {view === "assign" && assign ? (
              <>
                <ActionRow
                  icon={<User className="size-4" />}
                  label={b.assignNobody}
                  onClick={done(() => assign.onAssign(null))}
                />
                {assign.members.map((member) => (
                  <ActionRow
                    key={member.userId}
                    icon={<User className="size-4" />}
                    label={member.name}
                    onClick={done(() => assign.onAssign(member.userId))}
                  />
                ))}
              </>
            ) : null}
            {view === "move" && move ? (
              move.boards.length === 0 ? (
                <p className="m-0 px-2.5 py-1.5 text-compact text-meta-foreground">
                  {b.moveNone}
                </p>
              ) : (
                move.boards.map((board) => (
                  <ActionRow
                    key={board.id}
                    icon={<ArrowRight className="size-4" />}
                    label={board.title}
                    onClick={done(() => move.onMove(board.id))}
                  />
                ))
              )
            ) : null}
            {view === "due" ? (
              <div className="flex flex-col gap-3 px-2.5 pt-1">
                <label className="flex flex-col gap-1 text-compact text-foreground">
                  {b.dueLabel}
                  <input
                    type="date"
                    value={day}
                    onChange={(event) => setDay(event.target.value)}
                    className="h-11 rounded-[7px] border border-line-strong bg-background px-2 text-compact text-foreground"
                  />
                </label>
                <div className="flex justify-end gap-2">
                  <button
                    type="button"
                    className="h-11 cursor-pointer rounded-[7px] border border-line-strong bg-popover px-3 text-compact font-medium text-foreground"
                    onClick={done(() => onDue(null))}
                  >
                    {b.dueClear}
                  </button>
                  <button
                    type="button"
                    disabled={day === ""}
                    className="h-11 cursor-pointer rounded-[7px] bg-primary px-3 text-compact font-semibold text-primary-foreground disabled:cursor-default disabled:opacity-50"
                    onClick={done(() => onDue(day))}
                  >
                    {b.dueApply}
                  </button>
                </div>
              </div>
            ) : null}
          </div>
        </SheetContent>
      </Sheet>

      {onDelete ? (
        <AlertDialog open={confirmDelete} onOpenChange={setConfirmDelete}>
          <AlertDialogContent>
            <AlertDialogHeader>
              <AlertDialogTitle>
                {fillTableCopy(b.deleteTitle, { count })}
              </AlertDialogTitle>
              <AlertDialogDescription>{b.deleteBody}</AlertDialogDescription>
            </AlertDialogHeader>
            <AlertDialogFooter>
              <AlertDialogCancel>{b.deleteCancel}</AlertDialogCancel>
              <AlertDialogAction
                onClick={() => {
                  setConfirmDelete(false);
                  close();
                  onDelete();
                }}
              >
                {b.deleteConfirm}
              </AlertDialogAction>
            </AlertDialogFooter>
          </AlertDialogContent>
        </AlertDialog>
      ) : null}
    </div>
  );
}
