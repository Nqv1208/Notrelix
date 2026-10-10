import { useMemo } from "react";
import {
  useWorkspaceHomeCopy,
  WorkspaceMyWorkSurface,
  type MyWorkGroupView,
  type SectionState,
} from "@notrelix/features-workspace/web";
import { useFormatters } from "@notrelix/i18n";
import { useWorkspaceContext } from "@/providers/workspace-provider";
import { useNow } from "@/shell/workspace/use-now";
import { useShellNavigation } from "@/shell/workspace/use-shell-navigation";
import { useWorkspaceResources } from "@/shell/workspace/use-workspace-resources";
import {
  flattenMyWork,
  useMyWorkItems,
} from "@/shell/workspace/work-management-bridge";
import { toMyWorkGroups, useMyWorkCompletion } from "./home";

/**
 * My work (WX-HOME-001): every open Item assigned to the member in this
 * workspace (WX-BE-005), grouped by due window, paged by cursor.
 */
export function MyWorkPage() {
  const { workspaceId } = useWorkspaceContext();
  const copy = useWorkspaceHomeCopy();
  const formatters = useFormatters();
  const navigation = useShellNavigation();
  const now = useNow();
  const resources = useWorkspaceResources(workspaceId);
  const query = useMyWorkItems(workspaceId);
  const { completingIds, onComplete } = useMyWorkCompletion(workspaceId);

  const boardNames = useMemo(
    () => new Map(resources.boards.map((board) => [board.id, board.name])),
    [resources.boards],
  );
  const state = useMemo<SectionState<readonly MyWorkGroupView[]>>(() => {
    if (query.isError) return { status: "error" };
    if (!query.data) return { status: "loading" };
    return {
      status: "ready",
      data: toMyWorkGroups(flattenMyWork(query.data), boardNames, now),
    };
  }, [boardNames, now, query.data, query.isError]);

  return (
    <WorkspaceMyWorkSurface
      copy={copy}
      navigation={navigation}
      formatters={formatters}
      state={state}
      completingIds={completingIds}
      onComplete={onComplete}
      onRetry={() => void query.refetch()}
      hasMore={Boolean(query.hasNextPage)}
      loadingMore={query.isFetchingNextPage}
      onLoadMore={() => void query.fetchNextPage()}
    />
  );
}
