import { useId } from "react";
import { cn } from "@notrelix/ui-web";
import { SPACE_RESTORE_DAYS } from "../../core/model/spaces";
import type { SectionState } from "../home/home-models";
import { formatCopy } from "../shell/shell-models";
import type { WorkspaceSpacesCopy } from "./workspace-spaces.copy";

export interface DeletedSpaceRow {
  readonly id: string;
  readonly name: string;
  /** ISO instants. */
  readonly deletedAt: string;
  readonly restorableUntil: string;
}

export type DeletedSpaceFailure = "conflict" | "forbidden" | "gone" | "failed";

export interface SpaceTrashSurfaceProps {
  readonly copy: WorkspaceSpacesCopy;
  /** `error` with reason `forbidden`: the person is not a workspace Owner or Admin. */
  readonly rows: SectionState<readonly DeletedSpaceRow[]>;
  readonly pendingId: string | null;
  /** The row whose last restore was refused, with the producer's own words for a rule. */
  readonly failed: {
    readonly id: string;
    readonly reason: DeletedSpaceFailure | "rule";
    readonly detail: string | null;
  } | null;
  readonly formatDate: (iso: string) => string;
  readonly onRestore: (id: string) => void;
  readonly onRetry: () => void;
  readonly onBack: () => void;
}

const BUTTON =
  "inline-flex h-8 items-center rounded-[7px] max-sm:h-11 border border-line bg-popover px-3 text-item-small font-medium text-foreground outline-none hover:bg-state-hover focus-visible:ring-2 focus-visible:ring-focus-ring aria-disabled:cursor-default aria-disabled:opacity-60";

/**
 * The deleted departments of a workspace and their Restore (the same shape as the deleted Boards list):
 * each row names when it was deleted and until when it can come back.
 */
export function SpaceTrashSurface({
  copy,
  rows,
  pendingId,
  failed,
  formatDate,
  onRestore,
  onRetry,
  onBack,
}: SpaceTrashSurfaceProps) {
  const t = copy.trash;
  const headingId = useId();
  const days = String(SPACE_RESTORE_DAYS);
  return (
    <section
      aria-labelledby={headingId}
      className="mx-auto w-full max-w-[880px] px-4 py-6 sm:px-6"
    >
      <h1 id={headingId} className="m-0 text-board-title text-foreground">
        {t.title}
      </h1>
      <p className="mt-1 mb-4 max-w-[640px] text-compact text-muted-foreground">
        {formatCopy(t.intro, { days })}
      </p>
      {rows.status === "loading" ? (
        <p
          role="status"
          className="py-8 text-center text-compact text-muted-foreground"
        >
          {t.loading}
        </p>
      ) : rows.status === "error" && rows.reason === "forbidden" ? (
        <div
          role="alert"
          className="flex flex-col items-center gap-2 py-8 text-center text-compact text-muted-foreground"
        >
          <span>{t.forbidden}</span>
          <button type="button" onClick={onBack} className={BUTTON}>
            {t.back}
          </button>
        </div>
      ) : rows.status === "error" ? (
        <div
          role="alert"
          className="flex flex-col items-center gap-2 py-8 text-compact text-danger-text"
        >
          <span>{t.error}</span>
          <button type="button" onClick={onRetry} className={BUTTON}>
            {t.retry}
          </button>
        </div>
      ) : rows.data.length === 0 ? (
        <p className="py-8 text-center text-compact text-muted-foreground">
          {formatCopy(t.empty, { days })}
        </p>
      ) : (
        <ul className="m-0 flex list-none flex-col gap-2 p-0">
          {rows.data.map((row) => {
            const busy = pendingId === row.id;
            const failure = failed?.id === row.id ? failed : null;
            return (
              <li
                key={row.id}
                className="flex flex-wrap items-center gap-3 rounded-[10px] border border-line bg-popover px-4 py-3 dark:bg-card"
              >
                <span className="min-w-0 flex-1">
                  <b className="block truncate text-item-ui font-semibold">
                    {row.name}
                  </b>
                  <small className="block text-crumb text-muted-foreground">
                    {formatCopy(t.deletedOn, {
                      date: formatDate(row.deletedAt),
                    })}
                    {" · "}
                    {formatCopy(t.restorableUntil, {
                      date: formatDate(row.restorableUntil),
                    })}
                  </small>
                </span>
                <button
                  type="button"
                  aria-disabled={busy ? true : undefined}
                  aria-describedby={
                    failure ? `${headingId}-${row.id}-error` : undefined
                  }
                  onClick={busy ? undefined : () => onRestore(row.id)}
                  className={cn(BUTTON)}
                >
                  {busy ? t.restoring : t.restore}
                </button>
                {failure ? (
                  <span
                    id={`${headingId}-${row.id}-error`}
                    role="alert"
                    className="basis-full text-crumb text-danger-text"
                  >
                    {failure.reason === "rule" && failure.detail
                      ? failure.detail
                      : t.failures[
                          failure.reason === "rule" ? "failed" : failure.reason
                        ]}
                  </span>
                ) : null}
              </li>
            );
          })}
        </ul>
      )}
    </section>
  );
}
