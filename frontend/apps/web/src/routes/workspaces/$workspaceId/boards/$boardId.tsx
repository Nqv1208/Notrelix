import { useState } from "react";
import { useParams, useSearch, useNavigate } from "@tanstack/react-router";
import {
  BoardHeaderCopyContext,
  BoardHeaderDataContext,
  BoardLayoutShell,
  BoardWorkspaceViewContent,
  KanbanSettingsDialog,
  useBoardHeaderCopy,
} from "@notrelix/work-management-web";
import { useFullBoard } from "@notrelix/work-management-state";
import {
  boardSearchSchema,
  type BoardSearch,
} from "../../../../router/schemas/board-search-schema";
import { useBoardHeaderData } from "../../../../shell/workspace/use-board-header-data";

export function BoardPage() {
  const { workspaceId, boardId } = useParams({
    from: "/workspaces/$workspaceId/boards/$boardId",
  });
  const navigate = useNavigate({
    from: "/workspaces/$workspaceId/boards/$boardId",
  });
  const search: BoardSearch = useSearch({
    from: "/workspaces/$workspaceId/boards/$boardId",
  });

  const viewType = search.view;
  const view = { type: viewType, name: viewType.toUpperCase() };

  const { board } = useFullBoard(boardId, workspaceId);
  const headerCopy = useBoardHeaderCopy();
  // The Kanban view's settings open from the header's "Thiết lập".
  const [settingsOpen, setSettingsOpen] = useState(false);
  const headerData = useBoardHeaderData(workspaceId, boardId, board);

  const handleViewChange = (newView: string) => {
    const nextView = boardSearchSchema.shape.view.safeParse(newView);
    if (!nextView.success) return;
    // The item belongs to the view it was opened in; filter, sort and
    // groupBy survive the switch.
    navigate({
      search: (prev: BoardSearch) => ({
        ...prev,
        view: nextView.data,
        item: undefined,
      }),
    });
  };

  // Opening a card pushes history; closing or clearing a stale item passes
  // `replace` so Back does not reopen the sheet.
  const handleSelectedItemChange = (
    item: string | undefined,
    options?: { replace?: boolean },
  ) => {
    navigate({
      search: (prev: BoardSearch) => ({ ...prev, item }),
      replace: options?.replace ?? false,
    });
  };

  // The Kanban filter (`serializeKanbanFilter` form). A user change pushes
  // history; the view's own cleanup of stale ids replaces it.
  const handleFilterChange = (
    filter: string | undefined,
    options?: { replace?: boolean },
  ) => {
    navigate({
      search: (prev: BoardSearch) => ({ ...prev, filter }),
      replace: options?.replace ?? false,
    });
  };

  // "Nhóm theo" names a field; persisting it to the view is a later step.
  const handleGroupByChange = (groupBy: string) => {
    navigate({ search: (prev: BoardSearch) => ({ ...prev, groupBy }) });
  };

  return (
    <BoardHeaderCopyContext.Provider value={headerCopy}>
      <BoardHeaderDataContext.Provider value={headerData}>
        <BoardLayoutShell
          workspaceId={workspaceId}
          boardId={boardId}
          boardTitle={board?.title ?? headerCopy.untitledBoard}
          activeView={viewType}
          onViewChange={handleViewChange}
          onOpenSettings={
            viewType === "kanban" ? () => setSettingsOpen(true) : undefined
          }
        >
          <BoardWorkspaceViewContent
            workspaceId={workspaceId}
            boardId={boardId}
            view={view}
            selectedItemId={search.item}
            onSelectedItemChange={handleSelectedItemChange}
            filter={search.filter}
            onFilterChange={handleFilterChange}
            groupBy={search.groupBy}
            onGroupByChange={handleGroupByChange}
            onViewChange={handleViewChange}
            onOpenItemPage={(itemId) =>
              navigate({
                to: "/workspaces/$workspaceId/boards/$boardId/items/$itemId",
                params: { workspaceId, boardId, itemId },
              })
            }
          />
        </BoardLayoutShell>
        {viewType === "kanban" ? (
          <KanbanSettingsDialog
            boardId={boardId}
            workspaceId={workspaceId}
            open={settingsOpen}
            onOpenChange={setSettingsOpen}
          />
        ) : null}
      </BoardHeaderDataContext.Provider>
    </BoardHeaderCopyContext.Provider>
  );
}
