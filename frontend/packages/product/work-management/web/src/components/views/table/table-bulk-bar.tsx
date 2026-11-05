import { useState, type ReactNode } from "react";
import {
  Archive,
  ArrowRight,
  Calendar,
  Check,
  ChevronDown,
  Info,
  Loader2,
  MoreHorizontal,
  Trash2,
  TriangleAlert,
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
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
  Popover,
  PopoverAnchor,
  PopoverContent,
  PopoverTrigger,
} from "@notrelix/ui-web";
import {
  fillTableCopy,
  pluralTableCopy,
  type MainTableCopy,
} from "./main-table.copy";
import { PickRow, type TableEditorMember } from "./table-cell-editors";
import type { TableStatusOption } from "./table-model";
import { StatusDot } from "./table-parts";

const BAR_BASE =
  "z-20 mx-auto flex w-fit items-center gap-0.5 rounded-[12px] bg-inverse-surface py-1.5 pr-1.5 pl-3.5 whitespace-nowrap text-inverse-foreground shadow-bar flex-wrap max-xl:rounded-[14px] max-xl:py-1 max-xl:pl-4.5 max-md:py-2 max-md:pl-3";

const BAR_CLASS = `absolute inset-x-3 bottom-5.5 max-md:bottom-3 ${BAR_BASE}`;

const BAR_BUTTON_CLASS =
  "inline-flex h-8 cursor-pointer items-center gap-1.5 rounded-md border-0 bg-transparent px-2.5 text-compact font-medium text-inverse-foreground max-xl:h-11 max-xl:rounded-[10px] hover:bg-white/10 focus-visible:outline-2 focus-visible:outline-inverse-foreground disabled:cursor-default disabled:opacity-50 disabled:hover:bg-transparent";

export interface TableBulkBarProps {
  copy: MainTableCopy;
  count: number;
  /**
   * The most one change covers, and how many rows the selection could have
   * had: past the limit the actions are locked and the bar says so; at it,
   * with more rows left, it says how many were not picked.
   */
  limit?: { readonly max: number; readonly available: number };
  /** A change is on its way: the bar shows only the count and the progress line. */
  saving?: boolean;
  statusOptions: readonly TableStatusOption[];
  /** The bar sits in a flex column the surface anchors to the bottom (under the failure banner), not on its own. */
  stacked?: boolean;
  /** The viewer may change status and due date. */
  canEdit: boolean;
  /** The viewer may archive items. */
  canArchive: boolean;
  onStatus: (optionId: string) => void;
  /** "YYYY-MM-DD", or null to clear the due date. */
  onDue: (day: string | null) => void;
  onArchive: () => void;
  /** The viewer may delete items; absent: no "Xoá" button. */
  onDelete?: () => void;
  onClear: () => void;
  /** Absent: no "Ưu tiên" button (the board has no Priority field). */
  priority?: {
    readonly options: readonly {
      readonly id: string;
      readonly label: string;
    }[];
    readonly onPriority: (optionId: string) => void;
  };
  /** Absent: "Giao cho" stays disabled and says so. */
  assign?: {
    readonly members: readonly TableEditorMember[];
    /** `null` = nobody. */
    readonly onAssign: (userId: string | null) => void;
  };
  /** Absent: "Chuyển board" stays disabled and says so. */
  move?: {
    readonly boards: readonly { readonly id: string; readonly title: string }[];
    readonly onMove: (boardId: string) => void;
  };
}

/** A bar action the person may not use: it stays reachable by keyboard and says why, instead of going silent. */
function ReadOnlyButton({
  label,
  reason,
  children,
}: {
  label: string;
  reason: string;
  children: ReactNode;
}) {
  return (
    <button
      type="button"
      aria-disabled="true"
      title={reason}
      aria-label={`${label}: ${reason}`}
      className={cn(BAR_BUTTON_CLASS, "opacity-50")}
    >
      {children}
    </button>
  );
}

const MORE_ITEM =
  "h-9 gap-2.5 rounded-[7px] px-2.5 py-0 text-[13.5px] data-disabled:text-meta-foreground";

const CHEVRON = "size-[13px] opacity-80";

/**
 * The bar over the bottom edge of the table while rows are selected (BC-Bulk): the count, then
 * Trạng thái, Ưu tiên, Người phụ trách, Hạn, a Thêm menu (Xoá hạn, Bỏ hết người phụ trách, Chuyển
 * board…), Lưu trữ, Xoá and close. "Người phụ trách" and "Chuyển board…" are disabled, with the
 * reason, when the host gives no write for them. While a change is on its way the bar shows only
 * the count and "Đang áp dụng cho N việc…".
 */
export function TableBulkBar({
  copy,
  count,
  limit,
  saving = false,
  stacked = false,
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
}: TableBulkBarProps) {
  const b = copy.bulk;
  const over = limit !== undefined && count > limit.max;
  const left =
    limit !== undefined && count === limit.max
      ? Math.max(0, limit.available - count)
      : 0;
  const [dueOpen, setDueOpen] = useState(false);
  const [day, setDay] = useState("");
  const [assignOpen, setAssignOpen] = useState(false);
  const [moveOpen, setMoveOpen] = useState(false);
  const [confirmDelete, setConfirmDelete] = useState(false);
  const canMove = canEdit && move !== undefined && move.boards.length > 0;

  return (
    <div
      role="toolbar"
      aria-label={fillTableCopy(b.label, { count })}
      aria-busy={saving}
      data-table-bulk-bar=""
      className={
        stacked ? cn("pointer-events-auto max-w-full", BAR_BASE) : BAR_CLASS
      }
    >
      <b className="mr-1.5 text-dense-body font-semibold max-xl:text-[14.5px] max-xl:font-[650]">
        {fillTableCopy(b.selected, { count })}
      </b>
      {saving ? (
        <>
          <span
            aria-hidden="true"
            className="mx-1 h-4.5 w-px bg-white/22 max-md:hidden"
          />
          <span
            role="status"
            className="inline-flex items-center gap-2 px-2.5 text-dense-body"
          >
            <Loader2
              aria-hidden="true"
              className="size-4 animate-spin motion-reduce:animate-none"
            />
            {fillTableCopy(b.saving, { count })}
          </span>
        </>
      ) : (
        <>
          <span
            aria-hidden="true"
            className="mx-1 h-4.5 w-px bg-white/22 max-md:hidden"
          />
          {left > 0 && limit ? (
            <span
              role="status"
              className="mr-2 text-compact text-inverse-foreground/70"
            >
              {fillTableCopy(b.capped, { max: limit.max, rest: left })}
            </span>
          ) : null}
          {!canEdit ? (
            <span className="mr-2 text-compact text-inverse-foreground/70 max-md:basis-full max-md:whitespace-normal">
              {b.readOnly}
            </span>
          ) : null}
          <fieldset disabled={over} className="m-0 contents border-0 p-0">
            {canEdit ? (
              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <button type="button" className={BAR_BUTTON_CLASS}>
                    {b.status}
                    <ChevronDown className={CHEVRON} aria-hidden="true" />
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
            ) : (
              <ReadOnlyButton label={b.status} reason={b.readOnly}>
                {b.status}
                <ChevronDown className={CHEVRON} aria-hidden="true" />
              </ReadOnlyButton>
            )}

            {priority && canEdit ? (
              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <button type="button" className={BAR_BUTTON_CLASS}>
                    {b.priority}
                    <ChevronDown className={CHEVRON} aria-hidden="true" />
                  </button>
                </DropdownMenuTrigger>
                <DropdownMenuContent
                  align="start"
                  side="top"
                  aria-label={b.priorityLabel}
                  className="w-48"
                >
                  {priority.options.map((option) => (
                    <DropdownMenuItem
                      key={option.id}
                      onSelect={() => priority.onPriority(option.id)}
                    >
                      {option.label}
                    </DropdownMenuItem>
                  ))}
                </DropdownMenuContent>
              </DropdownMenu>
            ) : null}

            {assign && canEdit ? (
              <Popover open={assignOpen} onOpenChange={setAssignOpen}>
                <PopoverTrigger asChild>
                  <button type="button" className={BAR_BUTTON_CLASS}>
                    {b.assign}
                    <ChevronDown className={CHEVRON} aria-hidden="true" />
                  </button>
                </PopoverTrigger>
                <PopoverContent
                  align="start"
                  side="top"
                  aria-label={b.assignLabel}
                  className="w-64 gap-0 rounded-[10px] border border-line-strong bg-popover p-1.5 shadow-pop ring-0 dark:bg-card"
                >
                  <ul className="flex max-h-72 flex-col gap-0.5 overflow-y-auto">
                    <li>
                      <PickRow
                        pressed={false}
                        onClick={() => {
                          setAssignOpen(false);
                          assign.onAssign(null);
                        }}
                      >
                        {b.assignNobody}
                      </PickRow>
                    </li>
                    {assign.members.map((member) => (
                      <li key={member.userId}>
                        <PickRow
                          pressed={false}
                          onClick={() => {
                            setAssignOpen(false);
                            assign.onAssign(member.userId);
                          }}
                        >
                          {member.name}
                        </PickRow>
                      </li>
                    ))}
                  </ul>
                </PopoverContent>
              </Popover>
            ) : (
              <ReadOnlyButton
                label={b.assign}
                reason={canEdit ? b.unavailable : b.readOnly}
              >
                {b.assign}
                <ChevronDown className={CHEVRON} aria-hidden="true" />
              </ReadOnlyButton>
            )}

            {canEdit ? (
              <Popover open={dueOpen} onOpenChange={setDueOpen}>
                <PopoverTrigger asChild>
                  <button type="button" className={BAR_BUTTON_CLASS}>
                    {b.due}
                    <ChevronDown className={CHEVRON} aria-hidden="true" />
                  </button>
                </PopoverTrigger>
                <PopoverContent
                  align="start"
                  side="top"
                  aria-label={b.dueTitle}
                  className="w-60 gap-2 rounded-[10px] border border-line-strong bg-popover p-3 shadow-pop ring-0 dark:bg-card"
                >
                  <label className="flex flex-col gap-1 text-compact text-foreground">
                    {b.dueLabel}
                    <input
                      type="date"
                      value={day}
                      onChange={(event) => setDay(event.target.value)}
                      className="h-8 rounded-[7px] border border-line-strong bg-background px-2 max-md:h-11 text-compact text-foreground"
                    />
                  </label>
                  <div className="flex justify-end gap-2">
                    <button
                      type="button"
                      className="h-8 cursor-pointer rounded-[7px] max-md:h-11 border border-line-strong bg-popover px-3 text-compact font-medium text-foreground hover:bg-state-hover-strong dark:bg-card"
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
                      className="h-8 cursor-pointer rounded-[7px] max-md:h-11 bg-primary px-3 text-compact font-semibold text-primary-foreground disabled:cursor-default disabled:opacity-50"
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
            ) : (
              <ReadOnlyButton label={b.due} reason={b.readOnly}>
                {b.due}
                <ChevronDown className={CHEVRON} aria-hidden="true" />
              </ReadOnlyButton>
            )}

            {canEdit ? (
              <Popover open={moveOpen} onOpenChange={setMoveOpen}>
                <PopoverAnchor asChild>
                  <span className="inline-flex">
                    <DropdownMenu>
                      <DropdownMenuTrigger asChild>
                        <button type="button" className={BAR_BUTTON_CLASS}>
                          <MoreHorizontal
                            className="size-4"
                            aria-hidden="true"
                          />
                          {b.more}
                        </button>
                      </DropdownMenuTrigger>
                      <DropdownMenuContent
                        align="end"
                        side="top"
                        sideOffset={16}
                        // The board list opens under the button next: focus must not go back and dismiss it.
                        onCloseAutoFocus={(event) => {
                          if (moveOpen) event.preventDefault();
                        }}
                        aria-label={b.moreLabel}
                        className="flex w-80 flex-col gap-0.5 rounded-[12px] border border-line-strong bg-popover p-1.5 shadow-pop ring-0"
                      >
                        <DropdownMenuItem
                          onSelect={() => onDue(null)}
                          className={MORE_ITEM}
                        >
                          <Calendar className="size-4" aria-hidden="true" />
                          {b.moreDueClear}
                        </DropdownMenuItem>
                        <DropdownMenuItem
                          disabled={!assign}
                          onSelect={() => assign?.onAssign(null)}
                          className={MORE_ITEM}
                        >
                          <User className="size-4" aria-hidden="true" />
                          {b.moreUnassign}
                        </DropdownMenuItem>
                        <DropdownMenuItem
                          disabled={!canMove}
                          onSelect={() => setMoveOpen(true)}
                          className={cn(
                            MORE_ITEM,
                            !canMove &&
                              "h-auto items-center py-1.5 data-disabled:opacity-100",
                          )}
                        >
                          <ArrowRight
                            className="size-4 shrink-0"
                            aria-hidden="true"
                          />
                          <span className="flex flex-1 flex-col leading-[1.45]">
                            {b.moreMove}
                            {!canMove ? (
                              <span className="text-[12px] leading-[1.3] text-meta-foreground">
                                {b.moreMoveHint}
                              </span>
                            ) : null}
                          </span>
                        </DropdownMenuItem>
                      </DropdownMenuContent>
                    </DropdownMenu>
                  </span>
                </PopoverAnchor>
                <PopoverContent
                  align="start"
                  side="top"
                  aria-label={b.moveLabel}
                  className="w-64 gap-0 rounded-[10px] border border-line-strong bg-popover p-1.5 shadow-pop ring-0 dark:bg-card"
                >
                  {!move || move.boards.length === 0 ? (
                    <p className="px-2 py-1.5 text-compact text-meta-foreground">
                      {b.moveNone}
                    </p>
                  ) : (
                    <ul className="flex max-h-72 flex-col gap-0.5 overflow-y-auto">
                      {move.boards.map((board) => (
                        <li key={board.id}>
                          <PickRow
                            pressed={false}
                            onClick={() => {
                              setMoveOpen(false);
                              move.onMove(board.id);
                            }}
                          >
                            {board.title}
                          </PickRow>
                        </li>
                      ))}
                    </ul>
                  )}
                </PopoverContent>
              </Popover>
            ) : (
              <ReadOnlyButton label={b.more} reason={b.readOnly}>
                <MoreHorizontal className="size-4" aria-hidden="true" />
                {b.more}
              </ReadOnlyButton>
            )}

            <span
              aria-hidden="true"
              className="mx-1 h-4.5 w-px bg-white/22 max-xl:h-6 max-md:hidden"
            />

            <button
              type="button"
              disabled={!canArchive}
              onClick={onArchive}
              className={BAR_BUTTON_CLASS}
            >
              <Archive className="size-4" aria-hidden="true" />
              {b.archive}
            </button>

            {onDelete ? (
              <>
                <button
                  type="button"
                  onClick={() => setConfirmDelete(true)}
                  className={cn(BAR_BUTTON_CLASS, "text-inverse-danger")}
                >
                  <Trash2 className="size-4" aria-hidden="true" />
                  {b.delete}
                </button>
                <AlertDialog
                  open={confirmDelete}
                  onOpenChange={setConfirmDelete}
                >
                  <AlertDialogContent>
                    <AlertDialogHeader>
                      <AlertDialogTitle>
                        {fillTableCopy(b.deleteTitle, { count })}
                      </AlertDialogTitle>
                      <AlertDialogDescription>
                        {b.deleteBody}
                      </AlertDialogDescription>
                    </AlertDialogHeader>
                    <AlertDialogFooter>
                      <AlertDialogCancel>{b.deleteCancel}</AlertDialogCancel>
                      <AlertDialogAction
                        onClick={() => {
                          setConfirmDelete(false);
                          onDelete();
                        }}
                      >
                        {b.deleteConfirm}
                      </AlertDialogAction>
                    </AlertDialogFooter>
                  </AlertDialogContent>
                </AlertDialog>
              </>
            ) : null}
          </fieldset>

          <button
            type="button"
            aria-label={b.clear}
            onClick={onClear}
            className={cn(
              BAR_BUTTON_CLASS,
              "w-8 justify-center px-0 max-xl:w-11",
            )}
          >
            <X className="size-4" aria-hidden="true" />
          </button>
        </>
      )}
    </div>
  );
}

/**
 * What a bulk change did (BC-Bulk `bk-ok`): a dark pill over the foot of the table with the count
 * and, where the change can be taken back as one request, "Hoàn tác". It stands in the place the
 * bar left.
 */
export function TableBulkDone({
  text,
  undoLabel,
  onUndo,
}: {
  text: string;
  undoLabel: string;
  onUndo?: () => void;
}) {
  return (
    <div
      role="status"
      aria-live="polite"
      data-table-bulk-done=""
      className="absolute inset-x-3 bottom-5.5 z-20 mx-auto flex w-fit max-w-full items-center gap-3 rounded-[10px] bg-inverse-surface py-2.5 pr-3 pl-3.5 text-[13px] leading-[1.45] text-inverse-foreground shadow-bar max-md:bottom-4"
    >
      <Check className="size-4 shrink-0" aria-hidden="true" />
      <span>{text}</span>
      {onUndo ? (
        <button
          type="button"
          onClick={onUndo}
          className="h-7 cursor-pointer rounded-[6px] border-0 bg-transparent px-1.5 text-[13px] max-md:h-11 font-semibold text-inverse-foreground hover:bg-white/10 focus-visible:outline-2 focus-visible:outline-inverse-foreground"
        >
          {undoLabel}
        </button>
      ) : null}
    </div>
  );
}

type NoticeTone = "warning" | "info";

/**
 * A note over the bulk bar (BC-Bulk `bk-cap`, `bk-grp`, `bk-gone`): what the pick is up against, in
 * a title and a sentence, with at most one button. Amber when the pick is locked, blue when it only
 * informs.
 */
export function TableBulkNotice({
  tone,
  title,
  detail,
  action,
}: {
  tone: NoticeTone;
  title: string;
  detail: string;
  action?: { label: string; onClick: () => void };
}) {
  const Icon = tone === "warning" ? TriangleAlert : Info;
  return (
    <div
      role="status"
      data-table-bulk-notice={tone}
      className={cn(
        "box-border flex items-start gap-2.5 rounded-[10px] px-3.5 py-2.5 text-compact leading-[1.4] max-md:flex-wrap",
        tone === "warning"
          ? "bg-warning-subtle text-warning-text"
          : "bg-brand-subtle text-brand-subtle-foreground",
      )}
    >
      <Icon className="mt-0.5 size-4 shrink-0" aria-hidden="true" />
      <span className="flex min-w-0 flex-1 flex-col gap-px">
        <b className="font-semibold text-foreground">{title}</b>
        <span>{detail}</span>
      </span>
      {action ? (
        <button
          type="button"
          onClick={action.onClick}
          className="inline-flex h-11 shrink-0 cursor-pointer items-center justify-center rounded-[7px] border border-line-strong bg-popover px-3.5 text-compact font-medium whitespace-nowrap text-foreground hover:bg-state-hover-strong focus-visible:outline-2 focus-visible:outline-ring max-md:w-full dark:bg-card"
        >
          {action.label}
        </button>
      ) : null}
    </div>
  );
}

/**
 * A refused or lost bulk change, in the page under the table (BC-Bulk `bk-err`): it names the
 * first item the server refused and says nothing changed and the selection stays. "Thử lại"
 * sends the same change again; while the outcome is unknown the request keeps its idempotency
 * key and versions, so the server never applies it twice.
 */
export function TableBulkFailure({
  copy,
  count,
  detail,
  onRetry,
}: {
  copy: MainTableCopy;
  count: number;
  detail: string;
  onRetry: () => void;
}) {
  const b = copy.bulk;
  return (
    <div
      role="alert"
      data-table-bulk-failure=""
      className="box-border flex items-start gap-2.5 rounded-[10px] bg-danger-subtle px-3.5 py-2.5 text-compact leading-[1.4] text-danger-text max-md:flex-wrap"
    >
      <TriangleAlert className="mt-0.5 size-4 shrink-0" aria-hidden="true" />
      <span className="flex min-w-0 flex-1 flex-col gap-px">
        <b className="font-semibold text-foreground">
          {pluralTableCopy(
            { one: b.failedTitle_one, other: b.failedTitle_other },
            count,
          )}
        </b>
        <span>{detail}</span>
      </span>
      <span className="flex shrink-0 items-center gap-1.5 max-md:w-full">
        <button
          type="button"
          onClick={onRetry}
          className="inline-flex h-11 cursor-pointer items-center justify-center rounded-[7px] border border-line-strong bg-popover px-3.5 text-compact font-medium whitespace-nowrap text-foreground hover:bg-state-hover-strong focus-visible:outline-2 focus-visible:outline-ring max-md:flex-1 dark:bg-card"
        >
          {b.retry}
        </button>
      </span>
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
      className={cn(BAR_CLASS, "gap-1")}
    >
      <b className="mr-2 text-dense-body font-semibold max-xl:text-[14.5px] max-xl:font-[650]">
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
