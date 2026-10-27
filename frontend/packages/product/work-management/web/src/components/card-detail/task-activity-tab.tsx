import { Skeleton } from "@notrelix/ui-web";
import type { CardActivity } from "@notrelix/work-management-core";
import { describeActivityEvent } from "./activity-words";
import { describeSheetTime } from "./item-sheet-format";
import type { ItemSheetCopy } from "./item-sheet.copy";

/**
 * "Lịch sử" subtab: one line per recorded change, newest last: the actor, what they did in the viewer's
 * language (from the line's type and change, or its recorded action for older lines) and the time. While the
 * history loads it shows placeholders; when it cannot be read it says so and offers a retry.
 */
export function TaskActivityTab({
  activity,
  isLoading,
  isError,
  copy,
  now,
  timeZone,
  onRefresh,
}: {
  activity: readonly CardActivity[];
  isLoading: boolean;
  isFetching?: boolean;
  isError?: boolean;
  copy: ItemSheetCopy;
  now: Date;
  timeZone?: string;
  onRefresh?: () => void;
}) {
  if (isLoading) {
    return (
      <div className="flex flex-col gap-2" aria-busy="true">
        <Skeleton className="h-6 rounded-md" />
        <Skeleton className="h-6 rounded-md" />
      </div>
    );
  }
  if (activity.length === 0 && isError) {
    return (
      <div
        role="alert"
        className="flex items-center gap-3 text-sm text-muted-foreground"
      >
        <span>{copy.activityEvents.loadError}</span>
        {onRefresh ? (
          <button
            type="button"
            onClick={onRefresh}
            className="font-medium text-brand-subtle-foreground underline-offset-2 hover:underline focus-visible:ring-2 focus-visible:ring-focus-ring"
          >
            {copy.activityEvents.retry}
          </button>
        ) : null}
      </div>
    );
  }
  if (activity.length === 0) {
    return (
      <p className="m-0 text-sm text-muted-foreground">
        {copy.sections.noHistory}
      </p>
    );
  }
  return (
    <ul className="m-0 flex list-none flex-col p-0">
      {activity.map((item) => {
        const described = describeActivityEvent(item, copy.activityEvents);
        const sentence = described?.verb ?? item.verb ?? item.action;
        return (
          <li
            key={item.id}
            className="flex items-center gap-2 py-1.5 text-crumb text-meta-foreground"
          >
            <span
              aria-hidden="true"
              className="size-2.25 shrink-0 rounded-full bg-work-status-doing"
            />
            <span>
              <span className="font-medium text-foreground">{item.actor}</span>{" "}
              {sentence} ·{" "}
              <time dateTime={item.createdAt}>
                {describeSheetTime(item.createdAt, now, copy, timeZone)}
              </time>
            </span>
          </li>
        );
      })}
    </ul>
  );
}
