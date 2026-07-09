"use client"
import { TaskDetailPanel } from "../../card-detail/task-detail-panel"

import { useCallback, useMemo, useState } from "react"
import {
  closestCenter,
  DndContext,
  KeyboardSensor,
  PointerSensor,
  useSensor,
  useSensors,
  type DragEndEvent,
} from "@dnd-kit/core"
import { sortableKeyboardCoordinates } from "@dnd-kit/sortable"
import { AlertCircle, EyeOff, Filter, Group, ListPlus, MoreHorizontal, Plus, Search, SortAsc } from "lucide-react"
import {
  ResizableHandle,
  ResizablePanel,
  ResizablePanelGroup,
} from "@notrelix/ui-web"
import { Skeleton } from "@notrelix/ui-web"
import { Button } from "@notrelix/ui-web"
import { Input } from "@notrelix/ui-web"
import {
  DropdownMenu,
  DropdownMenuCheckboxItem,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@notrelix/ui-web"
import { defaultTableViewConfig } from "@notrelix/work-management-state"
import { useBoardTable, useCreateColumn, useCreateGroup, useDeleteCard, useMoveRow, useSelectedCardPanel } from "@notrelix/work-management-state"
import type { Card } from "@notrelix/work-management-core"
import { generatePosition } from "@notrelix/work-management-core"
import { useIsMobile } from "@notrelix/ui-web"
import { getMainTableGridTemplate } from "./table-utils"
import { TableGroupSection } from "./table-group-section"
import { TableHeaderRow } from "./table-header-row"
import { TableScrollContainer } from "./table-scroll-container"
import { TableStickyHeader } from "./table-sticky-header"

export function MainTableView({ boardId, workspaceId }: { boardId: string; workspaceId: string }) {
  const table = useBoardTable(boardId, workspaceId)
  const moveRow = useMoveRow(boardId, workspaceId)
  const createGroup = useCreateGroup(boardId, workspaceId)
  const { selectedCardId, isOpen: isPanelOpen, openCard, closePanel } = useSelectedCardPanel()
  const [exitingCardId, setExitingCardId] = useState<string | null>(null)
  const isMobile = useIsMobile()
  const sensors = useSensors(
    useSensor(PointerSensor, { activationConstraint: { distance: 8 } }),
    useSensor(KeyboardSensor, { coordinateGetter: sortableKeyboardCoordinates })
  )

  const gridTemplate = useMemo(() => getMainTableGridTemplate(table.columns), [table.columns])
  const tableWidth = useMemo(
    () => table.columns.reduce((sum, column) => sum + column.width, 112),
    [table.columns]
  )
  const visibleGroups = table.groups
  const desktopPanelCardId = selectedCardId ?? exitingCardId
  const hasDesktopPanel = Boolean(desktopPanelCardId) && !isMobile

  const handleOpenDetail = useCallback(
    (cardId: string) => {
      setExitingCardId(null)
      openCard(cardId)
    },
    [openCard]
  )

  const handleClosePanel = useCallback(() => {
    setExitingCardId((current) => current ?? selectedCardId)
    closePanel()
  }, [closePanel, selectedCardId])

  const handleDetailExitComplete = useCallback(() => {
    setExitingCardId(null)
  }, [])

  function handleDragEnd(event: DragEndEvent) {
    const activeType = event.active.data.current?.type
    const overType = event.over?.data.current?.type
    if (!event.over || activeType !== "card") return

    const activeCard = event.active.data.current?.card as Card | undefined
    const targetGroupId =
      overType === "card"
        ? (event.over.data.current?.card as Card | undefined)?.listId
        : overType === "group"
          ? String(event.over.id)
          : undefined
    if (!activeCard || !targetGroupId) return

    const targetGroup = table.groups.find((group) => group.id === targetGroupId)
    if (!targetGroup) return
    const overCard = overType === "card" ? (event.over.data.current?.card as Card | undefined) : undefined
    const orderedCards = targetGroup.cards.filter((card) => card.id !== activeCard.id).sort((a, b) => a.position - b.position)
    const overIndex = overCard ? orderedCards.findIndex((card) => card.id === overCard.id) : orderedCards.length
    const before = orderedCards[overIndex - 1]?.position
    const after = orderedCards[overIndex]?.position

    moveRow.mutate({
      cardId: activeCard.id,
      listId: targetGroupId,
      position: generatePosition(before, after),
    })
  }

  if (table.isLoading) return <MainTableSkeleton />
  if (table.error || !table.board) return <MainTableError />
  const board = table.board

  return (
    <div data-slot="main-table" className="h-full min-h-0 overflow-hidden bg-card">
      <ResizablePanelGroup direction="horizontal" className="h-full min-h-0">
        <ResizablePanel id="main-table" defaultSize={hasDesktopPanel ? "60%" : "100%"} minSize="40%">
          <section className="flex h-full min-h-0 flex-col overflow-hidden bg-card" aria-label={`${board.title} main table`}>
            <MainTableToolbar
              table={table}
              boardId={boardId}
              workspaceId={workspaceId}
              onCreateGroup={() => {
                const lastPosition = table.groups.at(-1)?.position
                createGroup.mutate({
                  title: "New group",
                  color: "#579bfc",
                  position: generatePosition(lastPosition, undefined),
                })
              }}
            />
            <div className="min-h-0 flex-1">
              <DndContext sensors={sensors} collisionDetection={closestCenter} onDragEnd={handleDragEnd}>
                <TableScrollContainer>
                <div
                  role="grid"
                  aria-rowcount={visibleGroups.reduce((count, group) => count + group.cards.length, 0)}
                  aria-colcount={table.columns.length + 2}
                  className="min-h-full"
                  style={{ minWidth: tableWidth }}
                >
                  <TableStickyHeader>
                    <TableHeaderRow
                      boardId={board.id}
                      workspaceId={board.workspaceId}
                      columns={table.columns}
                      gridTemplate={gridTemplate}
                      isAllSelected={table.selectionState.isAllSelected}
                      onToggleAll={table.toggleAllSelection}
                      onResizeColumn={table.resizeColumn}
                      onHideColumn={table.columnVisibility.hideColumn}
                    />
                  </TableStickyHeader>
                  <div data-slot="table-body" role="rowgroup">
                    {visibleGroups.map((group) => (
                      <TableGroupSection
                        key={group.id}
                        board={board}
                        group={group}
                        columns={table.columns}
                        gridTemplate={gridTemplate}
                        selectedCardIdSet={table.selectedCardIdSet}
                        activeDetailCardId={selectedCardId}
                        onSetCardSelected={table.setCardSelected}
                        onOpenDetail={handleOpenDetail}
                        onToggleGroup={table.toggleGroup}
                      />
                    ))}
                    <AddGroupAction />
                  </div>
                </div>
                </TableScrollContainer>
              </DndContext>
            </div>
          </section>
        </ResizablePanel>

        {hasDesktopPanel ? (
          <>
            <ResizableHandle withHandle />
            <ResizablePanel id="task-detail" defaultSize="40%" minSize="30%" maxSize="60%">
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
        <TaskDetailPanel board={board} cardId={selectedCardId} open={isPanelOpen} onClose={handleClosePanel} />
      ) : null}
    </div>
  )
}

function MainTableToolbar({
  table,
  boardId,
  workspaceId,
  onCreateGroup,
}: {
  table: ReturnType<typeof useBoardTable>
  boardId: string
  workspaceId: string
  onCreateGroup: () => void
}) {
  const firstGroupId = table.groups[0]?.id
  const deleteCard = useDeleteCard(boardId, workspaceId)
  const createColumn = useCreateColumn(boardId, workspaceId)

  return (
    <div className="flex min-h-14 shrink-0 flex-wrap items-center gap-2 bg-card px-4 py-2 sm:px-6">
      <div className="relative w-full max-w-xs">
        <Search className="pointer-events-none absolute left-2.5 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
        <Input
          value={table.search.query}
          onChange={(event) => table.search.setQuery(event.target.value)}
          className="h-8 pl-8"
          placeholder="Search tasks"
          aria-label="Search tasks"
        />
      </div>

      <Button
        size="sm"
        onClick={() => {
          if (firstGroupId) document.getElementById(`add-card-${firstGroupId}`)?.focus()
        }}
      >
        <Plus className="size-4" />
        New task
      </Button>
      <Button size="sm" variant="outline" className="bg-card" onClick={onCreateGroup}>
        <ListPlus className="size-4" />
        New group
      </Button>
      <Button
        size="sm"
        variant="outline"
        className="bg-card"
        onClick={() => createColumn.mutate({ name: "New text", fieldType: "text", position: table.fieldDefinitions.length + 1 })}
      >
        <Plus className="size-4" />
        Column
      </Button>

      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button size="sm" variant="ghost"><Filter className="size-4" /> Filter</Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="start" className="w-52">
          <DropdownMenuLabel>Quick filters</DropdownMenuLabel>
          <DropdownMenuItem onClick={() => table.tableFilters.clearFilters()}>Clear filters</DropdownMenuItem>
          <DropdownMenuSeparator />
          {["status-working", "status-stuck", "status-done"].map((status) => (
            <DropdownMenuItem
              key={status}
              onClick={() => table.tableFilters.setFilters([{ fieldId: "status", operator: "is", value: status }])}
            >
              {status.replace("status-", "")}
            </DropdownMenuItem>
          ))}
        </DropdownMenuContent>
      </DropdownMenu>

      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button size="sm" variant="ghost"><SortAsc className="size-4" /> Sort</Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="start" className="w-52">
          <DropdownMenuItem onClick={() => table.tableSort.clearSort()}>Clear sort</DropdownMenuItem>
          {["title", "status", "priority", "dueDate", "progress", "createdAt"].map((fieldId) => (
            <DropdownMenuItem key={fieldId} onClick={() => table.tableSort.setSort([{ fieldId, direction: "asc" }])}>
              {fieldId}
            </DropdownMenuItem>
          ))}
        </DropdownMenuContent>
      </DropdownMenu>

      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button size="sm" variant="ghost"><EyeOff className="size-4" /> Hide</Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="start" className="w-56">
          {table.fieldDefinitions.map((field) => (
            <DropdownMenuCheckboxItem
              key={field.id}
              checked={!table.viewConfig.hiddenFields.includes(field.id)}
              onCheckedChange={(checked) => {
                if (checked) table.columnVisibility.showColumn(field.id)
                else table.columnVisibility.hideColumn(field.id)
              }}
            >
              {field.name}
            </DropdownMenuCheckboxItem>
          ))}
        </DropdownMenuContent>
      </DropdownMenu>

      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button size="sm" variant="ghost"><Group className="size-4" /> Group by</Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="start" className="w-48">
          {["list", "status", "priority", "assignee"].map((groupBy) => (
            <DropdownMenuItem key={groupBy} onClick={() => table.updateViewConfig({ groupBy })}>
              {groupBy}
            </DropdownMenuItem>
          ))}
        </DropdownMenuContent>
      </DropdownMenu>

      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button size="icon-sm" variant="ghost" aria-label="More table actions">
            <MoreHorizontal className="size-4" />
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="end">
          <DropdownMenuItem onClick={() => table.updateViewConfig(defaultTableViewConfig)}>Reset table view</DropdownMenuItem>
        </DropdownMenuContent>
      </DropdownMenu>

      {table.selectedCardIds.length > 0 ? (
        <Button
          size="sm"
          variant="outline"
          className="ml-auto bg-card text-destructive"
          onClick={() => {
            for (const cardId of table.selectedCardIds) deleteCard.mutate(cardId)
            table.clearSelection()
          }}
        >
          Delete {table.selectedCardIds.length}
        </Button>
      ) : null}
    </div>
  )
}

function MainTableSkeleton() {
  return (
    <div className="h-full min-h-0 bg-card p-4">
      <div className="border border-border bg-card p-4">
        <Skeleton className="mb-4 h-10 rounded-lg" />
        <div className="flex flex-col gap-2">
          {Array.from({ length: 8 }).map((_, index) => (
            <Skeleton key={index} className="h-12 rounded-lg" />
          ))}
        </div>
      </div>
    </div>
  )
}

function MainTableError() {
  return (
    <div className="p-4">
      <div className="rounded-lg border border-border bg-card p-8 text-center">
        <AlertCircle className="mx-auto mb-3 size-8 text-destructive" />
        <h2 className="text-lg font-semibold text-foreground">Table unavailable</h2>
        <p className="mt-2 text-sm text-muted-foreground">The board table could not be loaded.</p>
      </div>
    </div>
  )
}

function AddGroupAction() {
  return (
    <div className="sticky left-0 flex min-h-12 items-center border-b border-border/50 bg-table-bg px-4">
      <Button variant="ghost" size="sm" className="text-muted-foreground/60 hover:text-muted-foreground">
        <Plus className="size-4" />
        Add group
      </Button>
    </div>
  )
}
