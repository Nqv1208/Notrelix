import { useParams, useSearch, useNavigate } from "@tanstack/react-router";
import { BoardWorkspaceViewContent } from "@notrelix/work-management-web";
import { BoardLayoutShell } from "@notrelix/work-management-web";
import { useFullBoard } from "@notrelix/work-management-state";
import {
  boardSearchSchema,
  type BoardSearch,
} from "../../../../router/schemas/board-search-schema";

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

  const handleSelectedItemChange = (item: string | undefined) => {
    navigate({ search: (prev: BoardSearch) => ({ ...prev, item }) });
  };

  return (
    <BoardLayoutShell
      workspaceId={workspaceId}
      boardId={boardId}
      boardTitle={board?.title ?? "Board"}
      activeView={viewType}
      onViewChange={handleViewChange}
    >
      <BoardWorkspaceViewContent
        workspaceId={workspaceId}
        boardId={boardId}
        view={view}
        selectedItemId={search.item}
        onSelectedItemChange={handleSelectedItemChange}
      />
    </BoardLayoutShell>
  );
}
