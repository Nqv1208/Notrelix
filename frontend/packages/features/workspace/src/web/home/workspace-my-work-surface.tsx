import { UserRoundCheck } from "lucide-react";
import {
  Button,
  Empty,
  EmptyDescription,
  EmptyHeader,
  EmptyMedia,
  EmptyTitle,
  Skeleton,
} from "@notrelix/ui-web";
import type { ShellNavigation } from "../shell/shell-models";
import {
  pluralForm,
  type MyWorkGroupView,
  type MyWorkItemView,
  type SectionState,
  type SurfaceFormatters,
} from "./home-models";
import { MyWorkList } from "./my-work-list";
import type { WorkspaceHomeCopy } from "./workspace-home.copy";

export interface WorkspaceMyWorkSurfaceProps {
  readonly copy: WorkspaceHomeCopy;
  readonly navigation: ShellNavigation;
  readonly formatters: SurfaceFormatters;
  readonly state: SectionState<readonly MyWorkGroupView[]>;
  readonly completingIds: ReadonlySet<string>;
  readonly onComplete: ((item: MyWorkItemView) => void) | null;
  readonly onRetry: () => void;
  /** More pages exist on the producer (cursor paging). */
  readonly hasMore: boolean;
  readonly loadingMore: boolean;
  readonly onLoadMore: () => void;
}

export function MyWorkSkeleton({ rows = 5 }: { rows?: number }) {
  return (
    <div aria-hidden="true" className="flex flex-col gap-1">
      <Skeleton className="mb-2 h-3 w-20" />
      {Array.from({ length: rows }, (_, index) => (
        <div key={index} className="flex h-10 items-center gap-3 px-1">
          <Skeleton className="size-5 rounded-full" />
          <Skeleton
            className={index % 2 === 0 ? "h-3.5 w-1/2" : "h-3.5 w-2/5"}
          />
          <Skeleton className="ml-auto h-3 w-14" />
        </div>
      ))}
    </div>
  );
}

/**
 * My work (WX-HOME-001): every open Item assigned to the member in this
 * workspace, grouped by due window, with inline completion and paging.
 */
export function WorkspaceMyWorkSurface({
  copy,
  navigation,
  formatters,
  state,
  completingIds,
  onComplete,
  onRetry,
  hasMore,
  loadingMore,
  onLoadMore,
}: WorkspaceMyWorkSurfaceProps) {
  const total =
    state.status === "ready"
      ? state.data.reduce((sum, group) => sum + group.items.length, 0)
      : 0;

  return (
    <div className="mx-auto flex w-full max-w-4xl flex-col gap-6 px-4 py-6 md:px-8 md:py-8">
      <header className="flex flex-col gap-1">
        <h1 className="text-page-title text-foreground">{copy.myWork.title}</h1>
        <p className="text-body text-muted-foreground">
          {copy.myWork.description}
          {state.status === "ready" && total > 0 ? (
            <span className="tabular-nums">
              {" · "}
              {pluralForm(
                {
                  one: copy.myWork.itemCount_one,
                  other: copy.myWork.itemCount_other,
                },
                total,
              )}
              {hasMore ? "+" : ""}
            </span>
          ) : null}
        </p>
      </header>

      {state.status === "loading" ? (
        <div role="status" aria-label={copy.myWork.title}>
          <MyWorkSkeleton rows={7} />
        </div>
      ) : state.status === "error" ? (
        <div
          role="alert"
          className="flex flex-wrap items-center justify-between gap-3 rounded-lg border border-border bg-surface-panel px-4 py-3"
        >
          <p className="text-body text-muted-foreground">
            {copy.sections.error}
          </p>
          <Button variant="outline" size="sm" onClick={onRetry}>
            {copy.sections.retry}
          </Button>
        </div>
      ) : state.data.length === 0 ? (
        <Empty className="border border-dashed border-border bg-surface-panel">
          <EmptyHeader>
            <EmptyMedia variant="icon">
              <UserRoundCheck aria-hidden="true" className="text-icon" />
            </EmptyMedia>
            <EmptyTitle className="text-subheading">
              {copy.myWork.empty}
            </EmptyTitle>
            <EmptyDescription className="text-body">
              {copy.myWork.emptyHint}
            </EmptyDescription>
          </EmptyHeader>
        </Empty>
      ) : (
        <>
          <MyWorkList
            copy={copy}
            navigation={navigation}
            formatters={formatters}
            groups={state.data}
            completingIds={completingIds}
            onComplete={onComplete}
            headingLevel={2}
          />
          {hasMore ? (
            <div className="flex justify-center">
              <Button
                variant="outline"
                size="sm"
                disabled={loadingMore}
                onClick={onLoadMore}
              >
                {loadingMore ? copy.myWork.loadingMore : copy.myWork.loadMore}
              </Button>
            </div>
          ) : null}
        </>
      )}
    </div>
  );
}
