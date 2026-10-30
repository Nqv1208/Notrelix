import { useId } from "react";
import { cn } from "@notrelix/ui-web";
import {
  fillRetiredBoardsCopy,
  type RetiredBoardsCopy,
} from "./retired-boards.copy";

export type RetiredBoardsTab = "archived" | "deleted";

export type RetiredBoardFailure =
  "conflict" | "window" | "denied" | "gone" | "failed";

export interface RetiredBoardRow {
  readonly id: string;
  readonly title: string;
  readonly background: string;
  /** When it was archived or deleted. */
  readonly retiredAt: string;
  /** Deleted boards only: the end of the restore window. */
  readonly restorableUntil: string | null;
  readonly href: string | null;
}

export interface RetiredBoardsSurfaceProps {
  readonly copy: RetiredBoardsCopy;
  readonly tab: RetiredBoardsTab;
  readonly onTab: (tab: RetiredBoardsTab) => void;
  readonly rows: readonly RetiredBoardRow[];
  readonly loading: boolean;
  readonly error: boolean;
  readonly onRetry: () => void;
  /** The row a write is running for, and the one whose last write was refused. */
  readonly pendingId: string | null;
  readonly failed: {
    readonly id: string;
    readonly reason: RetiredBoardFailure;
  } | null;
  readonly formatDate: (iso: string) => string;
  /** Archived: Unarchive. Deleted: Restore. Absent: the action is not offered. */
  readonly onAct?: (id: string) => void;
  readonly onOpen?: (id: string) => void;
}

const BUTTON =
  "inline-flex h-8 items-center rounded-[7px] border border-line bg-popover px-3 text-item-small font-medium text-foreground outline-none hover:bg-state-hover focus-visible:ring-2 focus-visible:ring-focus-ring disabled:cursor-default disabled:opacity-60";

/** The archived and deleted boards of a workspace, with Unarchive and Restore. */
export function RetiredBoardsSurface({
  copy,
  tab,
  onTab,
  rows,
  loading,
  error,
  onRetry,
  pendingId,
  failed,
  formatDate,
  onAct,
  onOpen,
}: RetiredBoardsSurfaceProps) {
  const headingId = useId();
  const tabs: readonly RetiredBoardsTab[] = ["archived", "deleted"];
  return (
    <section
      aria-labelledby={headingId}
      className="mx-auto w-full max-w-[880px] px-4 py-6 sm:px-6"
    >
      <h1 id={headingId} className="m-0 text-board-title text-foreground">
        {copy.title}
      </h1>
      <p className="mt-1 mb-4 max-w-[640px] text-compact text-muted-foreground">
        {copy.intro}
      </p>

      <div
        role="tablist"
        aria-label={copy.tabs.label}
        className="mb-3 flex gap-1 border-b border-line"
      >
        {tabs.map((id) => (
          <button
            key={id}
            type="button"
            role="tab"
            id={`${headingId}-${id}`}
            aria-selected={tab === id}
            aria-controls={`${headingId}-panel`}
            onClick={() => onTab(id)}
            className={cn(
              "-mb-px h-9 border-b-2 px-3 text-item-small font-semibold outline-none focus-visible:ring-2 focus-visible:ring-focus-ring",
              tab === id
                ? "border-foreground text-foreground"
                : "border-transparent text-muted-foreground hover:text-foreground",
            )}
          >
            {copy.tabs[id]}
          </button>
        ))}
      </div>

      <div
        id={`${headingId}-panel`}
        role="tabpanel"
        aria-labelledby={`${headingId}-${tab}`}
      >
        {loading ? (
          <p
            role="status"
            className="py-8 text-center text-compact text-muted-foreground"
          >
            {copy.loading}
          </p>
        ) : error ? (
          <div
            role="alert"
            className="flex flex-col items-center gap-2 py-8 text-compact text-danger-text"
          >
            <span>{copy.loadFailed}</span>
            <button type="button" onClick={onRetry} className={BUTTON}>
              {copy.retry}
            </button>
          </div>
        ) : rows.length === 0 ? (
          <p className="py-8 text-center text-compact text-muted-foreground">
            {tab === "archived" ? copy.emptyArchived : copy.emptyDeleted}
          </p>
        ) : (
          <ul className="m-0 flex list-none flex-col gap-2 p-0">
            {rows.map((row) => {
              const busy = pendingId === row.id;
              const failure = failed?.id === row.id ? failed.reason : null;
              return (
                <li
                  key={row.id}
                  className="flex flex-wrap items-center gap-3 rounded-[10px] border border-line bg-popover px-4 py-3"
                >
                  <span
                    aria-hidden="true"
                    className="size-3 shrink-0 rounded-[4px]"
                    style={{ backgroundColor: row.background }}
                  />
                  <span className="min-w-0 flex-1">
                    <b className="block truncate text-item-ui font-semibold">
                      {row.title}
                    </b>
                    <small className="block text-crumb text-muted-foreground">
                      {fillRetiredBoardsCopy(
                        tab === "archived"
                          ? copy.archivedSince
                          : copy.deletedSince,
                        { date: formatDate(row.retiredAt) },
                      )}
                      {row.restorableUntil
                        ? ` · ${fillRetiredBoardsCopy(copy.restorableUntil, { date: formatDate(row.restorableUntil) })}`
                        : ""}
                    </small>
                  </span>
                  {tab === "archived" && onOpen ? (
                    <button
                      type="button"
                      onClick={() => onOpen(row.id)}
                      className={BUTTON}
                    >
                      {copy.open}
                    </button>
                  ) : null}
                  {onAct ? (
                    <button
                      type="button"
                      disabled={busy}
                      aria-describedby={
                        failure ? `${headingId}-${row.id}-error` : undefined
                      }
                      onClick={() => onAct(row.id)}
                      className={BUTTON}
                    >
                      {busy
                        ? copy.pending
                        : tab === "archived"
                          ? copy.unarchive
                          : copy.restore}
                    </button>
                  ) : null}
                  {failure ? (
                    <span
                      id={`${headingId}-${row.id}-error`}
                      role="alert"
                      className="basis-full text-crumb text-danger-text"
                    >
                      {copy.failures[failure]}
                    </span>
                  ) : null}
                </li>
              );
            })}
          </ul>
        )}
      </div>
    </section>
  );
}
