import { useCallback, useMemo } from "react";
import { useNavigate, useParams, useSearch } from "@tanstack/react-router";
import { toast } from "sonner";
import { initialsFor } from "@notrelix/features-workspace/core";
import { useCurrentUser } from "@notrelix/features-auth";
import { useFullBoard } from "@notrelix/work-management-state";
import {
  BoardHeaderCopyContext,
  ItemPage,
  useBoardHeaderCopy,
  useKanbanLaneCopy,
  type ItemPageTab,
  type ItemPageViewKind,
} from "@notrelix/work-management-web";
import type { ItemSearch } from "../../../../router/schemas/item-search-schema";
import { useBoardHeaderData } from "../../../../shell/workspace/use-board-header-data";

const ROUTE = "/workspaces/$workspaceId/boards/$boardId/items/$itemId";

/**
 * The full page of one item, under its board. The side sheet and this page
 * show the same item: the page is reached from the sheet's "Mở toàn trang" and
 * folds back to the board with the sheet open ("Thu về bảng bên").
 */
export function BoardItemPage() {
  const { workspaceId, boardId, itemId } = useParams({ from: ROUTE });
  const search: ItemSearch = useSearch({ from: ROUTE });
  const navigate = useNavigate({ from: ROUTE });
  const user = useCurrentUser();
  const laneCopy = useKanbanLaneCopy();
  const headerCopy = useBoardHeaderCopy();
  const { board } = useFullBoard(boardId, workspaceId);
  const headerData = useBoardHeaderData(workspaceId, boardId, board);

  const goToBoard = useCallback(
    (options: { view?: ItemPageViewKind; item?: string } = {}) =>
      void navigate({
        to: "/workspaces/$workspaceId/boards/$boardId",
        params: { workspaceId, boardId },
        search: { view: options.view ?? "kanban", item: options.item },
      }),
    [boardId, navigate, workspaceId],
  );

  const boardPath = `/workspaces/${workspaceId}/boards/${encodeURIComponent(boardId)}`;
  const viewHrefs = useMemo(
    () => ({
      kanban: `${boardPath}?view=kanban&item=${encodeURIComponent(itemId)}`,
      table: `${boardPath}?view=table&item=${encodeURIComponent(itemId)}`,
      calendar: `${boardPath}?view=calendar&item=${encodeURIComponent(itemId)}`,
      timeline: `${boardPath}?view=timeline`,
    }),
    [boardPath, itemId],
  );

  const viewer = useMemo(
    () =>
      user
        ? {
            id: user.id,
            userId: user.id,
            initials: initialsFor(user.name ?? user.email ?? ""),
          }
        : undefined,
    [user],
  );

  return (
    <BoardHeaderCopyContext.Provider value={headerCopy}>
      <ItemPage
        workspaceId={workspaceId}
        boardId={boardId}
        itemId={itemId}
        activeTab={search.tab}
        onSelectTab={(tab: ItemPageTab) =>
          void navigate({
            search: (prev: ItemSearch) => ({ ...prev, tab }),
            replace: true,
          })
        }
        space={
          headerData.space
            ? {
                name: headerData.space.label ?? headerData.space.name,
                mark: {
                  initials: headerData.space.initials,
                  tone: headerData.space.markTone,
                },
                href: headerData.space.href,
              }
            : undefined
        }
        viewer={viewer}
        boardHref={boardPath}
        viewHrefs={viewHrefs}
        onBack={() => goToBoard()}
        onOpenBoard={() => goToBoard()}
        onOpenSpace={headerData.onOpenSpace}
        onCollapse={() => goToBoard({ item: itemId })}
        onOpenView={(view) =>
          goToBoard({ view, item: view === "timeline" ? undefined : itemId })
        }
        onOpenItem={(next) =>
          void navigate({
            to: ROUTE,
            params: { workspaceId, boardId, itemId: next },
            search: (prev: ItemSearch) => prev,
            replace: true,
          })
        }
        onCopyLink={() => {
          void Promise.resolve()
            .then(() => navigator.clipboard.writeText(window.location.href))
            .then(
              () => toast.success(laneCopy.linkCopied),
              () => toast.error(laneCopy.linkCopyFailed),
            );
        }}
      />
    </BoardHeaderCopyContext.Provider>
  );
}
