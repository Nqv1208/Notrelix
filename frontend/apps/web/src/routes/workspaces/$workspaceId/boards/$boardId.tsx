import { useEffect, useState } from "react";
import { useParams, useSearch, useNavigate } from "@tanstack/react-router";
import { useCurrentUser } from "@notrelix/features-auth";
import {
  BoardHeaderCopyContext,
  BoardHeaderDataContext,
  BoardLayoutShell,
  BoardTrashHost,
  BoardWorkspaceViewContent,
  useBoardHeaderCopy,
} from "@notrelix/work-management-web";
import {
  BoardViewSelectionProvider,
  pickSavedView,
  useFullBoard,
  useSavedViews,
} from "@notrelix/work-management-state";
import {
  boardSearchSchema,
  type BoardSearch,
} from "../../../../router/schemas/board-search-schema";
import { BoardShareDialog } from "../../../../shell/workspace/board-share-dialog";
import { useBoardHeaderData } from "../../../../shell/workspace/use-board-header-data";

const OPTIMISTIC_ITEM_ID_PREFIX = "optimistic-";

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

  // The tabs are the board's saved views; a layout without one keeps its own
  // tab and shows the defaults until the first change saves a view for it.
  const saved = useSavedViews(boardId, workspaceId);
  const activeSaved = pickSavedView(saved.views, {
    viewId: search.savedView,
    mode: search.view,
  });
  const viewType = activeSaved?.viewMode ?? search.view;
  const view = { type: viewType, name: viewType.toUpperCase() };

  const { board } = useFullBoard(boardId, workspaceId);
  const currentUser = useCurrentUser();
  const headerCopy = useBoardHeaderCopy();
  // `share` in the address opens the share dialog (the settings page's
  // "Quyền truy cập" lands here); the dialog also opens from the header.
  const [shareOpen, setShareOpen] = useState(search.share === true);
  const [trashOpen, setTrashOpen] = useState(false);
  useEffect(() => {
    if (search.share !== true) return;
    setShareOpen(true);
    void navigate({
      search: (prev: BoardSearch) => ({ ...prev, share: undefined }),
      replace: true,
    });
  }, [navigate, search.share]);
  const headerData = useBoardHeaderData(workspaceId, boardId, board, () =>
    setShareOpen(true),
  );

  const showView = (mode: BoardSearch["view"], savedView?: string) => {
    // The item belongs to the view it was opened in; filter, sort and
    // groupBy survive the switch.
    navigate({
      search: (prev: BoardSearch) => ({
        ...prev,
        view: mode,
        savedView,
        item: undefined,
      }),
    });
  };

  // `viewKey` is a saved view's id, or a layout name when none is saved for it.
  const handleViewChange = (viewKey: string) => {
    const savedTarget = saved.views.find((entry) => entry.id === viewKey);
    const nextView = boardSearchSchema.shape.view.safeParse(
      savedTarget?.viewMode ?? viewKey,
    );
    if (!nextView.success) return;
    showView(nextView.data, savedTarget?.id);
  };

  // "Add view" saves a new shared view of the layout and opens it.
  const handleAddView = async (layout: string) => {
    const mode = boardSearchSchema.shape.view.safeParse(layout);
    if (!mode.success) return;
    const base = headerCopy.views[mode.data];
    const taken = new Set(saved.views.map((entry) => entry.name));
    let name = base;
    for (let n = 2; taken.has(name); n += 1) name = `${base} ${n}`;
    try {
      const made = await saved.create({ name, viewMode: mode.data });
      showView(made.viewMode, made.id);
    } catch {
      // The write failed: the error toast already told why.
    }
  };

  // Opening a card pushes history; closing or clearing a stale item passes
  // `replace` so Back does not reopen the sheet.
  const handleSelectedItemChange = (
    item: string | undefined,
    options?: { replace?: boolean },
  ) => {
    // A card the server has not confirmed yet has no real id to open.
    if (item?.startsWith(OPTIMISTIC_ITEM_ID_PREFIX)) return;
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
          activeView={activeSaved?.id ?? viewType}
          onViewChange={handleViewChange}
          savedViews={saved.views}
          onAddView={(layout) => void handleAddView(layout)}
          onOpenTrash={() => setTrashOpen(true)}
          onOpenSettings={() =>
            navigate({
              to: "/workspaces/$workspaceId/boards/$boardId/settings",
              params: { workspaceId, boardId },
              search: { from: viewType },
            })
          }
        >
          <BoardViewSelectionProvider viewId={activeSaved?.id} mode={viewType}>
            <BoardWorkspaceViewContent
              workspaceId={workspaceId}
              boardId={boardId}
              view={view}
              selectedItemId={search.item}
              currentUserId={currentUser?.id ?? null}
              onSelectedItemChange={handleSelectedItemChange}
              filter={search.filter}
              onFilterChange={handleFilterChange}
              groupBy={search.groupBy}
              onGroupByChange={handleGroupByChange}
              onViewChange={handleViewChange}
            />
          </BoardViewSelectionProvider>
        </BoardLayoutShell>
        <BoardTrashHost
          workspaceId={workspaceId}
          boardId={boardId}
          open={trashOpen}
          onClose={() => setTrashOpen(false)}
        />
        <BoardShareDialog
          workspaceId={workspaceId}
          boardId={boardId}
          board={board}
          open={shareOpen}
          onClose={() => setShareOpen(false)}
        />
      </BoardHeaderDataContext.Provider>
    </BoardHeaderCopyContext.Provider>
  );
}
