import { TaskDetailPanel } from "../../card-detail/task-detail-panel";

import { useCallback, useState } from "react";
import {
  ResizableHandle,
  ResizablePanel,
  ResizablePanelGroup,
} from "@notrelix/ui-web";
import { defaultTableViewConfig } from "@notrelix/work-management-state";
import {
  useBoardTable,
  useCreateColumn,
  useCreateCard,
  useCreateGroup,
  useDeleteCard,
  useDeleteColumn,
  useDeleteGroup,
  useDuplicateCard,
  useDuplicateGroup,
  useMoveRow,
  useSelectedCardPanel,
  useUpdateCard,
  useUpdateColumn,
  useUpdateFieldValue,
  useUpdateGroup,
} from "@notrelix/work-management-state";
import { generatePosition } from "@notrelix/work-management-core";
import { useIsMobile } from "@notrelix/ui-web";
import { MainTableError } from "./main-table-error";
import { MainTableSkeleton } from "./main-table-skeleton";
import { MainTableSurface } from "./main-table-surface";

export function MainTableView({
  boardId,
  workspaceId,
}: {
  boardId: string;
  workspaceId: string;
}) {
  const table = useBoardTable(boardId, workspaceId);
  const createCard = useCreateCard(boardId, workspaceId);
  const createColumn = useCreateColumn(boardId, workspaceId);
  const moveRow = useMoveRow(boardId, workspaceId);
  const createGroup = useCreateGroup(boardId, workspaceId);
  const updateGroup = useUpdateGroup(boardId, workspaceId);
  const deleteGroup = useDeleteGroup(boardId, workspaceId);
  const duplicateGroup = useDuplicateGroup(boardId, workspaceId);
  const updateColumn = useUpdateColumn(boardId, workspaceId);
  const deleteColumn = useDeleteColumn(boardId, workspaceId);
  const updateCard = useUpdateCard(boardId, workspaceId);
  const updateFieldValue = useUpdateFieldValue(boardId, workspaceId);
  const deleteCard = useDeleteCard(boardId, workspaceId);
  const duplicateCard = useDuplicateCard(boardId, workspaceId);
  const {
    selectedCardId,
    isOpen: isPanelOpen,
    openCard,
    closePanel,
  } = useSelectedCardPanel();
  const [exitingCardId, setExitingCardId] = useState<string | null>(null);
  const isMobile = useIsMobile();
  const desktopPanelCardId = selectedCardId ?? exitingCardId;
  const hasDesktopPanel = Boolean(desktopPanelCardId) && !isMobile;

  const handleOpenDetail = useCallback(
    (cardId: string) => {
      setExitingCardId(null);
      openCard(cardId);
    },
    [openCard],
  );

  const handleClosePanel = useCallback(() => {
    setExitingCardId((current) => current ?? selectedCardId);
    closePanel();
  }, [closePanel, selectedCardId]);

  const handleDetailExitComplete = useCallback(() => {
    setExitingCardId(null);
  }, []);

  if (table.isLoading) return <MainTableSkeleton />;
  if (table.error || !table.board) return <MainTableError />;
  const board = table.board;

  const handleCreateGroup = () => {
    const lastPosition = table.groups.at(-1)?.position;
    createGroup.mutate({
      title: "New group",
      color: "#579bfc",
      position: generatePosition(lastPosition, undefined),
    });
  };

  return (
    <div
      data-slot="main-table"
      className="h-full min-h-0 overflow-hidden bg-card"
    >
      <ResizablePanelGroup direction="horizontal" className="h-full min-h-0">
        <ResizablePanel
          id="main-table"
          defaultSize={hasDesktopPanel ? "60%" : "100%"}
          minSize="40%"
        >
          <MainTableSurface
            board={board}
            columns={table.columns}
            groups={table.groups}
            fieldDefinitions={table.fieldDefinitions}
            selectedCardIds={table.selectedCardIds}
            selectedCardIdSet={table.selectedCardIdSet}
            isAllSelected={table.selectionState.isAllSelected}
            activeDetailCardId={selectedCardId}
            searchQuery={table.search.query}
            hiddenFieldIds={table.viewConfig.hiddenFields}
            onSearchChange={table.search.setQuery}
            onNewTaskIntent={() => {
              const firstGroupId = table.groups[0]?.id;
              if (firstGroupId)
                document.getElementById(`add-card-${firstGroupId}`)?.focus();
            }}
            onCreateGroup={handleCreateGroup}
            onCreateColumn={() =>
              createColumn.mutate({
                name: "New text",
                fieldType: "text",
                position: table.fieldDefinitions.length + 1,
              })
            }
            onClearFilters={table.tableFilters.clearFilters}
            onSetFilters={table.tableFilters.setFilters}
            onClearSort={table.tableSort.clearSort}
            onSetSort={table.tableSort.setSort}
            onSetGroupBy={(groupBy) => table.updateViewConfig({ groupBy })}
            onResetTableView={() =>
              table.updateViewConfig(defaultTableViewConfig)
            }
            onToggleFieldVisible={(fieldId, visible) => {
              if (visible) table.columnVisibility.showColumn(fieldId);
              else table.columnVisibility.hideColumn(fieldId);
            }}
            onDeleteSelectedCards={() => {
              for (const cardId of table.selectedCardIds)
                deleteCard.mutate(cardId);
              table.clearSelection();
            }}
            onToggleAll={table.toggleAllSelection}
            onResizeColumn={table.resizeColumn}
            onHideColumn={table.columnVisibility.hideColumn}
            onRenameColumn={(columnId, name) =>
              updateColumn.mutate({ columnId, name })
            }
            onDeleteColumn={(columnId) => deleteColumn.mutate(columnId)}
            onSetCardSelected={table.setCardSelected}
            onOpenDetail={handleOpenDetail}
            onToggleGroup={table.toggleGroup}
            onCreateTask={(groupId, title, position) =>
              createCard.mutate({ listId: groupId, title, position })
            }
            onRenameGroup={(groupId, title) =>
              updateGroup.mutate({ groupId, title })
            }
            onUpdateGroupColor={(groupId, color) =>
              updateGroup.mutate({ groupId, color })
            }
            onDuplicateGroup={(groupId) => duplicateGroup.mutate(groupId)}
            onDeleteGroup={(groupId) => deleteGroup.mutate(groupId)}
            onDuplicateCard={(cardId) => duplicateCard.mutate(cardId)}
            onDeleteCard={(cardId) => deleteCard.mutate(cardId)}
            onUpdateCard={(cardId, patch) =>
              updateCard.mutate({ cardId, patch })
            }
            onUpdateFieldValue={updateFieldValue.mutate}
            onMoveRow={moveRow.mutate}
          />
        </ResizablePanel>

        {hasDesktopPanel ? (
          <>
            <ResizableHandle withHandle />
            <ResizablePanel
              id="task-detail"
              defaultSize="40%"
              minSize="30%"
              maxSize="60%"
            >
              <TaskDetailPanel
                board={board}
                cardId={desktopPanelCardId}
                open={Boolean(selectedCardId)}
                onClose={handleClosePanel}
                onExitComplete={handleDetailExitComplete}
              />
            </ResizablePanel>
          </>
        ) : null}
      </ResizablePanelGroup>

      {isMobile ? (
        <TaskDetailPanel
          board={board}
          cardId={selectedCardId}
          open={isPanelOpen}
          onClose={handleClosePanel}
        />
      ) : null}
    </div>
  );
}
