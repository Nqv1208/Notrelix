import { useMemo } from "react";
import {
  closestCenter,
  DndContext,
  KeyboardSensor,
  PointerSensor,
  useSensor,
  useSensors,
  type DragEndEvent,
} from "@dnd-kit/core";
import { sortableKeyboardCoordinates } from "@dnd-kit/sortable";
import {
  EyeOff,
  Filter,
  Group,
  ListPlus,
  MoreHorizontal,
  Plus,
  Search,
  SortAsc,
} from "lucide-react";
import { Button, Input } from "@notrelix/ui-web";
import {
  DropdownMenu,
  DropdownMenuCheckboxItem,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@notrelix/ui-web";
import type {
  Board,
  BoardGroup,
  BoardTableColumn,
  Card,
  FieldDefinition,
  FilterConfig,
  SortConfig,
  UpdateCardInput,
} from "@notrelix/work-management-core";
import { generatePosition } from "@notrelix/work-management-core";
import { getMainTableGridTemplate } from "./table-utils";
import { TableGroupSection } from "./table-group-section";
import { TableHeaderRow } from "./table-header-row";
import { TableScrollContainer } from "./table-scroll-container";
import { TableStickyHeader } from "./table-sticky-header";

export type TableFieldValueUpdate = {
  cardId: string;
  fieldDefinitionId: string;
  value: unknown;
};

export type TableMoveRow = {
  cardId: string;
  listId: string;
  position: number;
};

export type MainTableSurfaceProps = {
  board: Board;
  columns: BoardTableColumn[];
  groups: BoardGroup[];
  fieldDefinitions: FieldDefinition[];
  selectedCardIds: string[];
  selectedCardIdSet: ReadonlySet<string>;
  isAllSelected: boolean;
  activeDetailCardId?: string | null;
  searchQuery: string;
  hiddenFieldIds: string[];
  onSearchChange: (query: string) => void;
  onNewTaskIntent: () => void;
  onCreateGroup: () => void;
  onCreateColumn: () => void;
  onClearFilters: () => void;
  onSetFilters: (filters: FilterConfig[]) => void;
  onClearSort: () => void;
  onSetSort: (sort: SortConfig[]) => void;
  onSetGroupBy: (groupBy: string) => void;
  onResetTableView: () => void;
  onToggleFieldVisible: (fieldId: string, visible: boolean) => void;
  onDeleteSelectedCards: () => void;
  onToggleAll: () => void;
  onResizeColumn: (columnId: string, width: number) => void;
  onHideColumn: (columnId: string) => void;
  onRenameColumn: (columnId: string, name: string) => void;
  onDeleteColumn: (columnId: string) => void;
  onSetCardSelected: (cardId: string, selected: boolean) => void;
  onOpenDetail: (cardId: string) => void;
  onToggleGroup: (groupId: string) => void;
  onCreateTask: (groupId: string, title: string, position: number) => void;
  onRenameGroup: (groupId: string, title: string) => void;
  onUpdateGroupColor: (groupId: string, color: string) => void;
  onDuplicateGroup: (groupId: string) => void;
  onDeleteGroup: (groupId: string) => void;
  onDuplicateCard: (cardId: string) => void;
  onDeleteCard: (cardId: string) => void;
  onUpdateCard: (cardId: string, patch: UpdateCardInput) => void;
  onUpdateFieldValue: (update: TableFieldValueUpdate) => void;
  onMoveRow: (move: TableMoveRow) => void;
};

export function MainTableSurface({
  board,
  columns,
  groups,
  fieldDefinitions,
  selectedCardIds,
  selectedCardIdSet,
  isAllSelected,
  activeDetailCardId,
  searchQuery,
  hiddenFieldIds,
  onSearchChange,
  onNewTaskIntent,
  onCreateGroup,
  onCreateColumn,
  onClearFilters,
  onSetFilters,
  onClearSort,
  onSetSort,
  onSetGroupBy,
  onResetTableView,
  onToggleFieldVisible,
  onDeleteSelectedCards,
  onToggleAll,
  onResizeColumn,
  onHideColumn,
  onRenameColumn,
  onDeleteColumn,
  onSetCardSelected,
  onOpenDetail,
  onToggleGroup,
  onCreateTask,
  onRenameGroup,
  onUpdateGroupColor,
  onDuplicateGroup,
  onDeleteGroup,
  onDuplicateCard,
  onDeleteCard,
  onUpdateCard,
  onUpdateFieldValue,
  onMoveRow,
}: MainTableSurfaceProps) {
  const sensors = useSensors(
    useSensor(PointerSensor, { activationConstraint: { distance: 8 } }),
    useSensor(KeyboardSensor, {
      coordinateGetter: sortableKeyboardCoordinates,
    }),
  );
  const gridTemplate = useMemo(
    () => getMainTableGridTemplate(columns),
    [columns],
  );
  const tableWidth = useMemo(
    () => columns.reduce((sum, column) => sum + column.width, 112),
    [columns],
  );

  function handleDragEnd(event: DragEndEvent) {
    const activeType = event.active.data.current?.type;
    const overType = event.over?.data.current?.type;
    if (!event.over || activeType !== "card") return;

    const activeCard = event.active.data.current?.card as Card | undefined;
    const targetGroupId =
      overType === "card"
        ? (event.over.data.current?.card as Card | undefined)?.listId
        : overType === "group"
          ? String(event.over.id)
          : undefined;
    if (!activeCard || !targetGroupId) return;

    const targetGroup = groups.find((group) => group.id === targetGroupId);
    if (!targetGroup) return;
    const overCard =
      overType === "card"
        ? (event.over.data.current?.card as Card | undefined)
        : undefined;
    const orderedCards = targetGroup.cards
      .filter((card) => card.id !== activeCard.id)
      .sort((a, b) => a.position - b.position);
    const overIndex = overCard
      ? orderedCards.findIndex((card) => card.id === overCard.id)
      : orderedCards.length;
    const before = orderedCards[overIndex - 1]?.position;
    const after = orderedCards[overIndex]?.position;

    onMoveRow({
      cardId: activeCard.id,
      listId: targetGroupId,
      position: generatePosition(before, after),
    });
  }

  return (
    <section
      className="flex h-full min-h-0 flex-col overflow-hidden bg-card"
      aria-label={`${board.title} main table`}
      data-slot="main-table-surface"
    >
      <MainTableToolbar
        fieldDefinitions={fieldDefinitions}
        firstGroupId={groups[0]?.id}
        hiddenFieldIds={hiddenFieldIds}
        searchQuery={searchQuery}
        selectedCardCount={selectedCardIds.length}
        onClearFilters={onClearFilters}
        onClearSort={onClearSort}
        onCreateColumn={onCreateColumn}
        onCreateGroup={onCreateGroup}
        onDeleteSelectedCards={onDeleteSelectedCards}
        onNewTaskIntent={onNewTaskIntent}
        onResetTableView={onResetTableView}
        onSearchChange={onSearchChange}
        onSetFilters={onSetFilters}
        onSetGroupBy={onSetGroupBy}
        onSetSort={onSetSort}
        onToggleFieldVisible={onToggleFieldVisible}
      />
      <div className="min-h-0 flex-1">
        <DndContext
          sensors={sensors}
          collisionDetection={closestCenter}
          onDragEnd={handleDragEnd}
        >
          <TableScrollContainer>
            <div
              aria-label="Board table rows"
              className="min-h-full"
              style={{ minWidth: tableWidth }}
            >
              <TableStickyHeader>
                <TableHeaderRow
                  columns={columns}
                  gridTemplate={gridTemplate}
                  isAllSelected={isAllSelected}
                  onDeleteColumn={onDeleteColumn}
                  onHideColumn={onHideColumn}
                  onRenameColumn={onRenameColumn}
                  onResizeColumn={onResizeColumn}
                  onToggleAll={onToggleAll}
                />
              </TableStickyHeader>
              <div data-slot="table-body">
                {groups.map((group) => (
                  <TableGroupSection
                    key={group.id}
                    board={board}
                    group={group}
                    columns={columns}
                    gridTemplate={gridTemplate}
                    selectedCardIdSet={selectedCardIdSet}
                    activeDetailCardId={activeDetailCardId}
                    onSetCardSelected={onSetCardSelected}
                    onOpenDetail={onOpenDetail}
                    onToggleGroup={onToggleGroup}
                    onCreateTask={onCreateTask}
                    onRenameGroup={onRenameGroup}
                    onUpdateGroupColor={onUpdateGroupColor}
                    onDuplicateGroup={onDuplicateGroup}
                    onDeleteGroup={onDeleteGroup}
                    onDuplicateCard={onDuplicateCard}
                    onDeleteCard={onDeleteCard}
                    onUpdateCard={onUpdateCard}
                    onUpdateFieldValue={onUpdateFieldValue}
                  />
                ))}
                <AddGroupAction onCreateGroup={onCreateGroup} />
              </div>
            </div>
          </TableScrollContainer>
        </DndContext>
      </div>
    </section>
  );
}

function MainTableToolbar({
  fieldDefinitions,
  firstGroupId,
  hiddenFieldIds,
  searchQuery,
  selectedCardCount,
  onClearFilters,
  onClearSort,
  onCreateColumn,
  onCreateGroup,
  onDeleteSelectedCards,
  onNewTaskIntent,
  onResetTableView,
  onSearchChange,
  onSetFilters,
  onSetGroupBy,
  onSetSort,
  onToggleFieldVisible,
}: {
  fieldDefinitions: FieldDefinition[];
  firstGroupId?: string;
  hiddenFieldIds: string[];
  searchQuery: string;
  selectedCardCount: number;
  onClearFilters: () => void;
  onClearSort: () => void;
  onCreateColumn: () => void;
  onCreateGroup: () => void;
  onDeleteSelectedCards: () => void;
  onNewTaskIntent: () => void;
  onResetTableView: () => void;
  onSearchChange: (query: string) => void;
  onSetFilters: (filters: FilterConfig[]) => void;
  onSetGroupBy: (groupBy: string) => void;
  onSetSort: (sort: SortConfig[]) => void;
  onToggleFieldVisible: (fieldId: string, visible: boolean) => void;
}) {
  return (
    <div className="flex min-h-14 shrink-0 flex-wrap items-center gap-2 bg-card px-4 py-2 sm:px-6">
      <div className="relative w-full max-w-xs">
        <Search className="pointer-events-none absolute left-2.5 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
        <Input
          value={searchQuery}
          onChange={(event) => onSearchChange(event.target.value)}
          className="h-8 pl-8"
          placeholder="Search tasks"
          aria-label="Search tasks"
        />
      </div>

      <Button
        size="sm"
        variant="outline"
        className="bg-card"
        onClick={onNewTaskIntent}
        disabled={!firstGroupId}
      >
        <Plus className="size-4" />
        New task
      </Button>
      <Button
        size="sm"
        variant="outline"
        className="bg-card"
        onClick={onCreateGroup}
      >
        <ListPlus className="size-4" />
        New group
      </Button>
      <Button
        size="sm"
        variant="outline"
        className="bg-card"
        onClick={onCreateColumn}
      >
        <Plus className="size-4" />
        Column
      </Button>

      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button size="sm" variant="ghost">
            <Filter className="size-4" /> Filter
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="start" className="w-52">
          <DropdownMenuLabel>Quick filters</DropdownMenuLabel>
          <DropdownMenuItem onClick={onClearFilters}>
            Clear filters
          </DropdownMenuItem>
          <DropdownMenuSeparator />
          {["status-working", "status-stuck", "status-done"].map((status) => (
            <DropdownMenuItem
              key={status}
              onClick={() =>
                onSetFilters([
                  { fieldId: "status", operator: "is", value: status },
                ])
              }
            >
              {status.replace("status-", "")}
            </DropdownMenuItem>
          ))}
        </DropdownMenuContent>
      </DropdownMenu>

      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button size="sm" variant="ghost">
            <SortAsc className="size-4" /> Sort
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="start" className="w-52">
          <DropdownMenuItem onClick={onClearSort}>Clear sort</DropdownMenuItem>
          {[
            "title",
            "status",
            "priority",
            "dueDate",
            "progress",
            "createdAt",
          ].map((fieldId) => (
            <DropdownMenuItem
              key={fieldId}
              onClick={() => onSetSort([{ fieldId, direction: "asc" }])}
            >
              {fieldId}
            </DropdownMenuItem>
          ))}
        </DropdownMenuContent>
      </DropdownMenu>

      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button size="sm" variant="ghost">
            <EyeOff className="size-4" /> Hide
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="start" className="w-56">
          {fieldDefinitions.map((field) => (
            <DropdownMenuCheckboxItem
              key={field.id}
              checked={!hiddenFieldIds.includes(field.id)}
              onCheckedChange={(checked) =>
                onToggleFieldVisible(field.id, Boolean(checked))
              }
            >
              {field.name}
            </DropdownMenuCheckboxItem>
          ))}
        </DropdownMenuContent>
      </DropdownMenu>

      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button size="sm" variant="ghost">
            <Group className="size-4" /> Group by
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="start" className="w-48">
          {["list", "status", "priority", "assignee"].map((groupBy) => (
            <DropdownMenuItem
              key={groupBy}
              onClick={() => onSetGroupBy(groupBy)}
            >
              {groupBy}
            </DropdownMenuItem>
          ))}
        </DropdownMenuContent>
      </DropdownMenu>

      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button
            size="icon-sm"
            variant="ghost"
            aria-label="More table actions"
          >
            <MoreHorizontal className="size-4" />
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="end">
          <DropdownMenuItem onClick={onResetTableView}>
            Reset table view
          </DropdownMenuItem>
        </DropdownMenuContent>
      </DropdownMenu>

      {selectedCardCount > 0 ? (
        <Button
          size="sm"
          variant="outline"
          className="ml-auto bg-card text-destructive"
          onClick={onDeleteSelectedCards}
        >
          Delete {selectedCardCount}
        </Button>
      ) : null}
    </div>
  );
}

function AddGroupAction({ onCreateGroup }: { onCreateGroup: () => void }) {
  return (
    <div className="sticky left-0 flex min-h-12 items-center border-b border-border/50 bg-table-bg px-4">
      <Button
        variant="ghost"
        size="sm"
        className="text-muted-foreground hover:text-foreground"
        onClick={onCreateGroup}
      >
        <Plus className="size-4" />
        Add group
      </Button>
    </div>
  );
}
