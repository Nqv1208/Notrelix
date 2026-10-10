import { useId } from "react";
import { Check, Circle } from "lucide-react";
import { Button, Spinner, cn } from "@notrelix/ui-web";
import type { MyWorkWindow } from "../../core/model/my-work";
import { ShellLink } from "../shell/shell-link";
import { formatCopy, type ShellNavigation } from "../shell/shell-models";
import type {
  MyWorkGroupView,
  MyWorkItemView,
  SurfaceFormatters,
} from "./home-models";
import type { WorkspaceHomeCopy } from "./workspace-home.copy";

export interface MyWorkListProps {
  readonly copy: WorkspaceHomeCopy;
  readonly navigation: ShellNavigation;
  readonly formatters: SurfaceFormatters;
  readonly groups: readonly MyWorkGroupView[];
  /** Items with a completion request in flight. */
  readonly completingIds: ReadonlySet<string>;
  /** `null` hides the inline complete control (no permission or no producer). */
  readonly onComplete: ((item: MyWorkItemView) => void) | null;
  /** Heading level for the window headings (Home nests them deeper). */
  readonly headingLevel?: 3 | 2;
  /** Show per-window counts (off for a truncated preview). */
  readonly showCounts?: boolean;
}

const DUE_FORMAT: Intl.DateTimeFormatOptions = {
  month: "short",
  day: "numeric",
};

function dueTone(range: MyWorkWindow): string {
  // Status color only for the state that needs attention (DESIGN.md §9).
  return range === "overdue" ? "text-danger-text" : "text-muted-foreground";
}

function ItemRow({
  item,
  range,
  copy,
  navigation,
  formatters,
  completing,
  onComplete,
}: {
  item: MyWorkItemView;
  range: MyWorkWindow;
  copy: WorkspaceHomeCopy;
  navigation: ShellNavigation;
  formatters: SurfaceFormatters;
  completing: boolean;
  onComplete: MyWorkListProps["onComplete"];
}) {
  const due = item.dueAt
    ? formatCopy(copy.myWork.due, {
        date: formatters.date(item.dueAt, DUE_FORMAT),
      })
    : copy.myWork.noDue;

  return (
    <li className="group/item relative flex min-h-10 items-center gap-2 rounded-md px-1 hover:bg-state-hover">
      {onComplete ? (
        <Button
          type="button"
          variant="ghost"
          size="icon-sm"
          disabled={completing}
          aria-label={formatCopy(
            completing ? copy.myWork.completing : copy.myWork.complete,
            { name: item.name },
          )}
          onClick={() => onComplete(item)}
          className="relative z-10 shrink-0 rounded-full text-muted-foreground hover:bg-success-subtle hover:text-success-text focus-visible:ring-focus-ring active:bg-success-subtle"
        >
          {completing ? (
            <Spinner aria-hidden="true" />
          ) : (
            <>
              <Circle aria-hidden="true" className="group-hover/item:hidden" />
              <Check
                aria-hidden="true"
                className="hidden group-hover/item:block"
              />
            </>
          )}
        </Button>
      ) : (
        <span aria-hidden="true" className="w-1 shrink-0" />
      )}
      <ShellLink
        destination={{
          section: "board",
          boardId: item.boardId,
          item: item.id,
        }}
        navigation={navigation}
        aria-label={formatCopy(copy.myWork.openItem, { name: item.name })}
        className={cn(
          "flex min-w-0 flex-1 items-center gap-3 rounded-md py-2 pr-2 outline-none",
          "focus-visible:ring-2 focus-visible:ring-focus-ring",
          completing && "text-muted-foreground",
        )}
      >
        <span className="min-w-0 flex-1">
          <span className="block truncate text-label text-foreground">
            {item.name}
          </span>
          {item.boardName ? (
            <span className="block truncate text-metadata text-muted-foreground">
              {formatCopy(copy.myWork.onBoard, { board: item.boardName })}
            </span>
          ) : null}
        </span>
        <span
          className={cn("shrink-0 text-metadata tabular-nums", dueTone(range))}
        >
          {due}
        </span>
      </ShellLink>
    </li>
  );
}

/** Assigned Items grouped by due window (shared by Home and My work). */
export function MyWorkList({
  copy,
  navigation,
  formatters,
  groups,
  completingIds,
  onComplete,
  headingLevel = 3,
  showCounts = true,
}: MyWorkListProps) {
  const Heading = headingLevel === 2 ? "h2" : "h3";
  const idPrefix = useId();
  return (
    <div className="flex flex-col gap-4">
      {groups.map((group) => {
        const headingId = `${idPrefix}-${group.range}`;
        return (
          <section key={group.range} aria-labelledby={headingId}>
            <Heading
              id={headingId}
              className={cn(
                "mb-1 flex items-baseline gap-2 px-1 text-metadata font-medium",
                group.range === "overdue"
                  ? "text-danger-text"
                  : "text-muted-foreground",
              )}
            >
              {copy.myWork.windows[group.range]}
              {showCounts ? (
                <span className="tabular-nums text-muted-foreground">
                  {group.items.length}
                </span>
              ) : null}
            </Heading>
            <ul className="flex flex-col">
              {group.items.map((item) => (
                <ItemRow
                  key={item.id}
                  item={item}
                  range={group.range}
                  copy={copy}
                  navigation={navigation}
                  formatters={formatters}
                  completing={completingIds.has(item.id)}
                  onComplete={onComplete}
                />
              ))}
            </ul>
          </section>
        );
      })}
    </div>
  );
}
