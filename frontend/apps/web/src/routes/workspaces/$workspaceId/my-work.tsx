import { useCallback, useMemo } from "react";
import { useNavigate, useSearch } from "@tanstack/react-router";
import { initialsFor } from "@notrelix/features-workspace/core";
import {
  formatCopy,
  useWorkspaceHomeCopy,
  WorkspaceMyWorkSurface,
  type MyWorkItemView,
  type SectionState,
} from "@notrelix/features-workspace/web";
import { useFormatters } from "@notrelix/i18n";
import { BoardItemSheetHost } from "@notrelix/work-management-web";
import { useWorkspaceContext } from "@/providers/workspace-provider";
import { toMyWorkItems } from "@/shell/workspace/my-work-view";
import { useBoardDepartments } from "@/shell/workspace/use-board-departments";
import { useMyWorkCompletion } from "@/shell/workspace/use-my-work-completion";
import { useNow } from "@/shell/workspace/use-now";
import { useShellNavigation } from "@/shell/workspace/use-shell-navigation";
import { useWorkspaceResources } from "@/shell/workspace/use-workspace-resources";
import {
  flattenMyWork,
  useMyWorkItems,
} from "@/shell/workspace/work-management-bridge";
import type { MyWorkSearch } from "@/router/schemas/my-work-search-schema";

/**
 * My work (WX-HOME-001, Final-MyWork): every open Item assigned to the member
 * in this workspace (WX-BE-005), by due window or by Board, paged by cursor,
 * with the item open in the shared side sheet through the `item` search
 * parameter. The read carries no Status, Priority, key or relationship beyond
 * "assigned", so the page draws none of them; approvals, followed items, done
 * items, waiting-on, adding an item and a personal calendar have no producer
 * and are drawn inert or left out.
 */
export function MyWorkPage() {
  const { workspaceId } = useWorkspaceContext();
  const copy = useWorkspaceHomeCopy();
  const formatters = useFormatters();
  const navigation = useShellNavigation();
  const now = useNow();
  const navigate = useNavigate({
    from: "/workspaces/$workspaceId/my-work",
  });
  const search: MyWorkSearch = useSearch({
    from: "/workspaces/$workspaceId/my-work",
  });
  const resources = useWorkspaceResources(workspaceId);
  const departments = useBoardDepartments(workspaceId);
  const query = useMyWorkItems(workspaceId);
  const { completingIds, onComplete } = useMyWorkCompletion(workspaceId);

  const boardNames = useMemo(
    () => new Map(resources.boards.map((board) => [board.id, board.name])),
    [resources.boards],
  );
  const state = useMemo<SectionState<readonly MyWorkItemView[]>>(() => {
    if (query.isError) return { status: "error" };
    if (!query.data) return { status: "loading" };
    return {
      status: "ready",
      data: toMyWorkItems(flattenMyWork(query.data), boardNames, departments),
    };
  }, [boardNames, departments, query.data, query.isError]);

  const items = state.status === "ready" ? state.data : [];
  const openItem = search.item
    ? (items.find((item) => item.id === search.item) ?? null)
    : null;

  const setItem = useCallback(
    (item: string | undefined, options?: { replace?: boolean }) => {
      void navigate({
        search: (previous: MyWorkSearch) => ({ ...previous, item }),
        replace: options?.replace ?? false,
      });
    },
    [navigate],
  );

  const createBoards = useMemo(
    () =>
      resources.boards.map((board) => {
        const department = departments.get(board.id);
        return {
          id: board.id,
          name: board.name,
          departmentName: department?.name ?? null,
          detail: department
            ? formatCopy(copy.list.departmentOf, { name: department.name })
            : null,
        };
      }),
    [copy.list.departmentOf, departments, resources.boards],
  );

  const sheet = openItem ? (
    <BoardItemSheetHost
      workspaceId={workspaceId}
      boardId={openItem.boardId}
      itemId={openItem.id}
      departmentMark={
        openItem.department
          ? {
              initials: initialsFor(openItem.department.name),
              tone: openItem.department.markTone,
            }
          : undefined
      }
      onClose={() => setItem(undefined, { replace: true })}
      onRestoreFocus={(itemId) =>
        document
          .querySelector<HTMLElement>(`[data-item-id="${itemId}"] a`)
          ?.focus()
      }
      onOpenFullPage={(itemId) =>
        void navigate({
          to: "/workspaces/$workspaceId/boards/$boardId/items/$itemId",
          params: { workspaceId, boardId: openItem.boardId, itemId },
        } as never)
      }
    />
  ) : undefined;

  return (
    <WorkspaceMyWorkSurface
      copy={copy}
      navigation={navigation}
      formatters={formatters}
      now={now}
      state={state}
      updatedAt={query.dataUpdatedAt || null}
      hasMore={Boolean(query.hasNextPage)}
      loadingMore={query.isFetchingNextPage}
      onLoadMore={() => void query.fetchNextPage()}
      onRetry={() => void query.refetch()}
      completingIds={completingIds}
      onComplete={(item) => onComplete(item)}
      createBoards={createBoards}
      onCreate={null}
      openItemId={openItem?.id ?? null}
      onOpenItem={(item) => setItem(item.id)}
      sheet={sheet}
      firstDepartment={[...departments.values()][0] ?? null}
    />
  );
}
