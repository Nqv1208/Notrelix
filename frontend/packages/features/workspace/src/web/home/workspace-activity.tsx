import { Skeleton, cn } from "@notrelix/ui-web";
import type { WorkspaceActivityEntry } from "../../core/api/activity.service";
import { initialsFor } from "../../core/model/navigation";
import { formatCopy } from "../shell/shell-models";
import type { SectionState, SurfaceFormatters } from "./home-models";
import type { WorkspaceHomeCopy } from "./workspace-home.copy";

/** What the Home activity section is given: the loaded entries and how the feed continues. */
export interface HomeActivityView {
  readonly state: SectionState<readonly WorkspaceActivityEntry[]>;
  readonly hasMore: boolean;
  readonly loadingMore: boolean;
  /** The next page failed; the entries already shown stay. */
  readonly loadMoreFailed: boolean;
  readonly onLoadMore: () => void;
  readonly onRetry: () => void;
}

const PANEL_CLASS =
  "rounded-[12px] border border-border bg-popover dark:bg-card";
const SMALL_BUTTON_CLASS =
  "inline-flex h-7 shrink-0 items-center gap-1.5 rounded-[8px] border border-line-strong bg-popover px-2.5 text-crumb leading-[1.45] font-medium whitespace-nowrap text-foreground no-underline outline-none hover:bg-state-hover focus-visible:ring-2 focus-visible:ring-focus-ring disabled:cursor-not-allowed disabled:opacity-60 dark:bg-card";

/** The line after the actor's name: the type's own words in the member's language, else the producer's English phrase. */
export function describeActivity(
  copy: WorkspaceHomeCopy,
  entry: WorkspaceActivityEntry,
): string {
  const text = copy.home.activity;
  const subject = entry.subjectName ?? text.untitled;
  const known = (text.types as Record<string, string | undefined>)[entry.type];
  if (known) return formatCopy(known, { subject });
  // Any other Item change reads as an update; a type we know nothing about shows the producer's phrase.
  if (entry.type.startsWith("work.item-"))
    return formatCopy(text.updated, { subject });
  return entry.title;
}

/**
 * Recent workspace activity (Home): what Collaboration recorded, newest first, a page at a time. The producer already
 * leaves out facts about Boards and Docs the member cannot read. Loading, failure, a refused read, an empty feed and a
 * failed next page each speak for themselves.
 */
export function WorkspaceActivitySection({
  copy,
  formatters,
  now,
  view,
}: {
  copy: WorkspaceHomeCopy;
  formatters: SurfaceFormatters;
  now: number;
  view: HomeActivityView;
}) {
  const text = copy.home.activity;
  const { state } = view;
  return (
    <section aria-labelledby="home-activity" data-f="activity">
      <h2
        id="home-activity"
        className="mb-2 flex items-center gap-2 text-sm leading-[1.45] font-[650]"
      >
        {text.title}
      </h2>
      {state.status === "loading" ? (
        <div
          role="status"
          aria-label={text.loading}
          className={cn(PANEL_CLASS, "overflow-hidden")}
        >
          {Array.from({ length: 3 }, (_, index) => (
            <div
              key={index}
              className="box-content flex min-h-[44px] items-center gap-3 border-t border-border px-3.5 py-2 first:border-t-0"
            >
              <Skeleton className="size-7 shrink-0 rounded-full" />
              <Skeleton className="h-3 w-[52%] rounded-md" />
            </div>
          ))}
        </div>
      ) : state.status === "error" ? (
        state.reason === "forbidden" ? (
          <p role="note" className="m-0 text-compact text-muted-foreground">
            {text.forbidden}
          </p>
        ) : (
          <div
            role="alert"
            className={cn(
              PANEL_CLASS,
              "flex flex-wrap items-center justify-between gap-2 px-3.5 py-3",
            )}
          >
            <p className="m-0 text-compact text-muted-foreground">
              {text.error}
            </p>
            <button
              type="button"
              className={SMALL_BUTTON_CLASS}
              onClick={view.onRetry}
            >
              {text.retry}
            </button>
          </div>
        )
      ) : state.data.length === 0 && !view.hasMore ? (
        <div
          data-f="empty"
          className="rounded-[12px] border border-dashed border-line-strong bg-popover p-4 text-nav font-normal text-muted-foreground dark:bg-card"
        >
          <b className="block text-sm font-bold text-foreground">
            {text.empty}
          </b>
          {text.emptyHint}
        </div>
      ) : (
        <div className={cn(PANEL_CLASS, "overflow-hidden")}>
          <ul aria-label={text.label} className="m-0 list-none p-0">
            {state.data.map((entry) => {
              const actor = entry.actorName ?? text.someone;
              return (
                <li
                  key={entry.id}
                  className="box-content flex min-h-[44px] items-center gap-3 border-t border-border px-3.5 py-2 first:border-t-0"
                >
                  <span
                    aria-hidden="true"
                    className="grid size-7 shrink-0 place-items-center rounded-full bg-muted text-xs font-semibold text-muted-foreground"
                  >
                    {entry.actorName ? initialsFor(entry.actorName) : "?"}
                  </span>
                  <span className="min-w-0 flex-1 text-compact leading-[1.45] text-muted-foreground">
                    <b className="font-medium text-foreground">{actor}</b>{" "}
                    {describeActivity(copy, entry)}
                  </span>
                  <time
                    dateTime={entry.occurredAt}
                    className="shrink-0 text-xs text-meta-foreground"
                  >
                    {formatters.relativeTime(entry.occurredAt, now)}
                  </time>
                </li>
              );
            })}
          </ul>
          {view.hasMore || view.loadMoreFailed ? (
            <div className="flex flex-wrap items-center gap-3 border-t border-border px-3.5 py-2">
              {view.loadMoreFailed ? (
                <p
                  role="alert"
                  className="m-0 text-compact text-muted-foreground"
                >
                  {text.loadMoreError}
                </p>
              ) : null}
              <button
                type="button"
                className={SMALL_BUTTON_CLASS}
                disabled={view.loadingMore}
                onClick={view.onLoadMore}
              >
                {view.loadingMore
                  ? text.loadingMore
                  : view.loadMoreFailed
                    ? text.retry
                    : text.loadMore}
              </button>
            </div>
          ) : null}
        </div>
      )}
    </section>
  );
}
