import {
  AlertCircle,
  CornerDownRight,
  Loader2,
  Search,
  Undo2,
  X,
} from "lucide-react";
import {
  Sheet,
  SheetClose,
  SheetContent,
  SheetTitle,
  cn,
} from "@notrelix/ui-web";
import {
  fillLifecycleCopy,
  type ItemLifecycleCopy,
} from "./item-lifecycle.copy";

export interface BoardTrashRow {
  readonly id: string;
  readonly name: string;
  readonly itemKey: string | null;
  /** `YYYY-MM-DD` of the last day the item can be restored (used when there is no `note`). */
  readonly restorableUntilDay?: string;
  /** The line under the name: who did it and when. */
  readonly note?: string;
  /** How many sub-items come back with the item. */
  readonly subItemCount?: number;
  /** Why the last try to bring it back was refused; shown under the line and kept until the next try. */
  readonly refusal?: string;
}

/** The words of one list: the same sheet shows the deleted items and the archived ones. */
export interface BoardTrashLabels {
  readonly title: string;
  readonly body: string;
  readonly empty: string;
  /** The bold line of the empty state; `empty` is the line under it. */
  readonly emptyTitle?: string;
  readonly loading: string;
  readonly error: string;
  readonly action: string;
  readonly searchPlaceholder?: string;
}

const trashLabels = (copy: ItemLifecycleCopy): BoardTrashLabels => ({
  title: copy.trashTitle,
  body: copy.trashBody,
  empty: copy.trashEmpty,
  emptyTitle: copy.listEmptyTrashTitle,
  loading: copy.trashLoading,
  error: copy.trashError,
  action: copy.trashRestore,
  searchPlaceholder: copy.listSearchTrash,
});

const PAGE_SIZE = 50;

/**
 * The board's deleted (or archived) items with the way back, a page at a time: a count beside the title, a search,
 * who did it and when, the sub-items that come back with the item and the reason when a try was refused. A right
 * panel from 640 px, the whole screen under it with 44 px buttons; under 360 px the button sits under the line.
 */
export function BoardTrashSheet({
  copy,
  labels = trashLabels(copy),
  open,
  rows,
  total,
  loading,
  failed,
  restoringId,
  hasMore = false,
  loadingMore = false,
  search,
  onSearch,
  onRetry,
  onLoadMore,
  onRestore,
  onClose,
}: {
  copy: ItemLifecycleCopy;
  /** The words of the list; the deleted items' when absent. */
  labels?: BoardTrashLabels;
  open: boolean;
  rows: readonly BoardTrashRow[];
  /** The size of the whole list (the page holds `rows`); the rows' count when absent. */
  total?: number;
  loading: boolean;
  failed: boolean;
  restoringId?: string;
  hasMore?: boolean;
  loadingMore?: boolean;
  /** The text in the search box; the box is shown when `onSearch` is given. */
  search?: string;
  onSearch?: (value: string) => void;
  onRetry?: () => void;
  onLoadMore?: () => void;
  onRestore: (row: BoardTrashRow) => void;
  onClose: () => void;
}) {
  const size = total ?? rows.length;
  const searching = Boolean(search?.trim());
  const empty = !loading && !failed && rows.length === 0;
  return (
    <Sheet open={open} onOpenChange={(next) => (next ? null : onClose())}>
      <SheetContent
        side="right"
        showCloseButton={false}
        aria-describedby={undefined}
        className="gap-0 border-line-strong p-0 data-[side=right]:w-full data-[side=right]:sm:max-w-[440px]"
      >
        <div className="flex items-center gap-2 pt-3 pr-2 pb-1 pl-4">
          <SheetTitle className="m-0 min-w-0 flex-1 text-[17px] font-[650]">
            {labels.title}
            {rows.length > 0 || size > 0 ? (
              <span className="ml-2 inline-flex h-[22px] items-center rounded-[6px] bg-state-hover-strong px-2 align-[2px] text-[12px] font-semibold text-muted-foreground">
                {size}
              </span>
            ) : null}
          </SheetTitle>
          <SheetClose asChild>
            <button
              type="button"
              aria-label={copy.trashClose}
              title={copy.trashClose}
              className="grid size-11 shrink-0 place-items-center rounded-[7px] text-muted-foreground hover:bg-state-hover focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary sm:size-8"
            >
              <X aria-hidden="true" className="size-4" />
            </button>
          </SheetClose>
        </div>
        {labels.searchPlaceholder && onSearch && !(empty && !searching) ? (
          <div className="px-4 py-2">
            <label className="box-border flex h-11 w-full items-center gap-2 rounded-[7px] border border-line-strong bg-popover px-2.5 text-meta-foreground sm:h-9">
              <Search aria-hidden="true" className="size-4 shrink-0" />
              <input
                type="search"
                value={search ?? ""}
                onChange={(event) => onSearch(event.target.value)}
                placeholder={labels.searchPlaceholder}
                aria-label={labels.searchPlaceholder}
                className="h-full w-full border-0 bg-transparent text-[14px] text-foreground outline-none"
              />
            </label>
          </div>
        ) : null}
        <div className="min-h-0 flex-1 overflow-y-auto">
          {loading ? (
            <div role="status" aria-label={labels.loading}>
              {[0, 1, 2].map((n) => (
                <div
                  key={n}
                  className="flex flex-col gap-2 border-b border-line px-4 py-3"
                >
                  <span className="block h-2.5 w-[60px] rounded bg-state-hover-strong" />
                  <span className="block h-3 w-[70%] rounded bg-state-hover-strong" />
                  <span className="block h-2.5 w-1/2 rounded bg-state-hover-strong" />
                </div>
              ))}
            </div>
          ) : failed ? (
            <div className="p-4">
              <div
                role="alert"
                className="flex flex-wrap items-start gap-2.5 rounded-[10px] bg-danger-subtle px-3.5 py-2.5 text-[13px] leading-[1.4] text-danger-text dark:bg-danger-text/14"
              >
                <AlertCircle
                  aria-hidden="true"
                  className="mt-0.5 size-4 shrink-0"
                />
                <span className="flex min-w-0 flex-1 flex-col gap-px">
                  <b className="font-semibold">{copy.listErrorTitle}</b>
                  <span>{copy.listErrorBody}</span>
                </span>
                {onRetry ? (
                  <ListButton onClick={onRetry} className="max-[359px]:w-full">
                    {copy.listRetry}
                  </ListButton>
                ) : null}
              </div>
            </div>
          ) : empty ? (
            <div className="flex flex-col items-center gap-1.5 px-6 py-12 text-center">
              <span className="grid size-11 place-items-center rounded-[12px] bg-state-hover-strong text-muted-foreground">
                <Undo2 aria-hidden="true" className="size-5" />
              </span>
              <b className="text-[14px] font-bold">
                {searching
                  ? copy.listNoResultsTitle
                  : (labels.emptyTitle ?? labels.empty)}
              </b>
              <span className="max-w-[32ch] text-[14px] text-muted-foreground">
                {searching
                  ? fillLifecycleCopy(copy.listNoResultsBody, {
                      query: search?.trim() ?? "",
                    })
                  : labels.empty}
              </span>
            </div>
          ) : (
            <>
              <ul aria-label={labels.title}>
                {rows.map((row) => (
                  <li
                    key={row.id}
                    className="grid grid-cols-1 items-center gap-x-3 gap-y-1.5 border-b border-line px-4 py-3 min-[360px]:grid-cols-[minmax(0,1fr)_auto] min-[360px]:gap-y-1 min-[360px]:py-2.5"
                  >
                    <span className="min-w-0">
                      <span className="flex items-center gap-2">
                        {row.itemKey ? (
                          <span className="font-mono text-[11.5px] text-meta-foreground">
                            {row.itemKey}
                          </span>
                        ) : null}
                        {row.subItemCount ? (
                          <span className="inline-flex h-[22px] items-center gap-1 rounded-[6px] bg-state-hover-strong px-2 text-[12px] font-semibold text-muted-foreground">
                            <CornerDownRight
                              aria-hidden="true"
                              className="size-[13px]"
                            />
                            {fillLifecycleCopy(
                              row.subItemCount === 1
                                ? copy.listSubItems_one
                                : copy.listSubItems_other,
                              { count: row.subItemCount },
                            )}
                          </span>
                        ) : null}
                      </span>
                      <span className="block text-[14px] font-medium wrap-break-word">
                        {row.name}
                      </span>
                      <span className="block">
                        <span className="text-[12.5px] text-muted-foreground max-[359px]:block">
                          {row.note ??
                            fillLifecycleCopy(copy.trashUntil, {
                              day: row.restorableUntilDay ?? "",
                            })}
                        </span>
                      </span>
                      {row.refusal ? (
                        <span
                          role="alert"
                          className="mt-1 flex items-start gap-1.5 text-[12.5px] leading-[1.45] font-medium text-danger-text"
                        >
                          <AlertCircle
                            aria-hidden="true"
                            className="mt-0.5 size-[13px] shrink-0"
                          />
                          {row.refusal}
                        </span>
                      ) : null}
                    </span>
                    <ListButton
                      disabled={restoringId === row.id}
                      onClick={() => onRestore(row)}
                      className={cn(
                        "justify-self-start min-[360px]:justify-self-auto",
                        restoringId === row.id && "sm:h-8 sm:px-3",
                      )}
                    >
                      {restoringId === row.id ? (
                        <>
                          <Loader2
                            aria-hidden="true"
                            className="size-4 animate-spin motion-reduce:animate-none"
                          />
                          {copy.listBusy}
                        </>
                      ) : (
                        <>
                          <Undo2 aria-hidden="true" className="size-4" />
                          {labels.action}
                        </>
                      )}
                    </ListButton>
                  </li>
                ))}
              </ul>
              {hasMore && onLoadMore ? (
                <div className="flex justify-center px-4 py-3">
                  <ListButton disabled={loadingMore} onClick={onLoadMore}>
                    {loadingMore
                      ? copy.listBusy
                      : fillLifecycleCopy(copy.listMore, {
                          count: Math.min(PAGE_SIZE, size - rows.length),
                        })}
                  </ListButton>
                </div>
              ) : null}
              {size > rows.length ? (
                <p
                  role="status"
                  className="m-0 pb-2.5 text-center text-[12px] text-meta-foreground"
                >
                  {fillLifecycleCopy(copy.listShown, {
                    shown: rows.length,
                    total: size,
                  })}
                </p>
              ) : null}
            </>
          )}
        </div>
      </SheetContent>
    </Sheet>
  );
}

/** The drawn 44 px outlined button of the list. */
function ListButton({
  disabled,
  onClick,
  className,
  children,
}: {
  disabled?: boolean;
  onClick: () => void;
  className?: string;
  children: React.ReactNode;
}) {
  return (
    <button
      type="button"
      disabled={disabled}
      aria-disabled={disabled || undefined}
      onClick={onClick}
      className={cn(
        "box-border inline-flex h-11 shrink-0 items-center justify-center gap-1.5 rounded-[7px] border border-line-strong bg-popover px-3.5 text-[13px] font-medium whitespace-nowrap text-foreground hover:bg-state-hover focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary disabled:cursor-not-allowed disabled:bg-state-hover-strong disabled:text-meta-foreground max-sm:text-sm",
        className,
      )}
    >
      {children}
    </button>
  );
}
