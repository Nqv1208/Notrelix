import {
  closestCorners,
  DndContext,
  KeyboardSensor,
  PointerSensor,
  useSensor,
  useSensors,
  type DragEndEvent,
} from "@dnd-kit/core";
import { sortableKeyboardCoordinates } from "@dnd-kit/sortable";
import type { Board, BoardGroup } from "@notrelix/work-management-core";
import { generatePosition } from "@notrelix/work-management-core";
import { KanbanAddColumn } from "./kanban-add-column";
import { KanbanColumn } from "./kanban-column";
import { getKanbanCardMove } from "./kanban-dnd";
export function KanbanBoard({
  board,
  columns,
  onOpenDetails,
  onMoveCard,
  onReorderColumns: _onReorderColumns,
  onAdd,
  onRenameColumn,
  onColorChangeColumn,
  onDeleteColumn,
  onDuplicateCard,
  onDeleteCard,
  onCreateCard,
}: {
  board: Board;
  columns: BoardGroup[];
  onOpenDetails: (cardId: string) => void;
  onMoveCard: (cardId: string, listId: string, position: number) => void;
  onReorderColumns: (updated: BoardGroup[]) => void;
  onAdd: (title: string) => void;
  onRenameColumn: (listId: string, title: string) => void;
  onColorChangeColumn: (listId: string, color: string) => void;
  onDeleteColumn: (listId: string) => void;
  onDuplicateCard: (cardId: string) => void;
  onDeleteCard: (cardId: string) => void;
  onCreateCard: (listId: string, title: string, position: number) => void;
}) {
  const sensors = useSensors(
    useSensor(PointerSensor, { activationConstraint: { distance: 8 } }),
    useSensor(KeyboardSensor, {
      coordinateGetter: sortableKeyboardCoordinates,
    }),
  );

  function handleDragEnd(event: DragEndEvent) {
    const move = getKanbanCardMove(event, columns);
    if (!move) return;
    onMoveCard(move.cardId, move.listId, move.position);
  }

  if (columns.length === 0) {
    return <KanbanAddColumn onAdd={() => onAdd("New column")} />;
  }

  return (
    <DndContext
      sensors={sensors}
      collisionDetection={closestCorners}
      onDragEnd={handleDragEnd}
    >
      <div className="flex flex-1 gap-4 overflow-x-auto pb-4 select-none">
        {columns.map((group) => (
          <KanbanColumn
            key={group.id}
            board={board}
            group={group}
            onOpenDetails={onOpenDetails}
            onRename={(title) => onRenameColumn(group.id, title)}
            onColorChange={(color) => onColorChangeColumn(group.id, color)}
            onDelete={() => onDeleteColumn(group.id)}
            onDuplicateCard={onDuplicateCard}
            onDeleteCard={onDeleteCard}
            onCreateCard={(title) => {
              const lastPosition = group.cards.at(-1)?.position;
              onCreateCard(
                group.id,
                title,
                generatePosition(lastPosition, undefined),
              );
            }}
          />
        ))}

        <div className="w-[290px] shrink-0">
          <KanbanAddColumn onAdd={onAdd} />
        </div>
      </div>
    </DndContext>
  );
}
