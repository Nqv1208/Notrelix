import { Skeleton } from "@notrelix/ui-web";
import type { CardActivity } from "@notrelix/work-management-core";
import { describeSheetTime } from "./item-sheet-format";
import type { ItemSheetCopy } from "./item-sheet.copy";

/**
 * "Lịch sử" subtab: one line per recorded change, newest last. The activity
 * contract carries no before/after values yet, so a line is the actor, the
 * recorded action and the time.
 */
export function TaskActivityTab({
  activity,
  isLoading,
  copy,
  now,
  timeZone,
}: {
  activity: readonly CardActivity[];
  isLoading: boolean;
  isFetching?: boolean;
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
  if (activity.length === 0) {
    return (
      <p className="m-0 text-sm text-muted-foreground">
        {copy.sections.noHistory}
      </p>
    );
  }
  return (
    <ul className="m-0 flex list-none flex-col p-0">
      {activity.map((item) => (
        <li
          key={item.id}
          className="flex items-center gap-2 py-1.5 text-xs text-muted-foreground"
        >
          <span
            aria-hidden="true"
            className="size-1.5 shrink-0 rounded-full bg-muted-foreground"
          />
          <span>
            <span className="font-medium text-foreground">{item.actor}</span>{" "}
            {item.action} ·{" "}
            <time dateTime={item.createdAt}>
              {describeSheetTime(item.createdAt, now, copy, timeZone)}
            </time>
          </span>
        </li>
      ))}
    </ul>
  );
}
