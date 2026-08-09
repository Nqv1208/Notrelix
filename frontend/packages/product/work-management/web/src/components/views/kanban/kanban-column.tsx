import { useState, useRef, useEffect } from "react";
import { useDroppable } from "@dnd-kit/core";
import {
  SortableContext,
  verticalListSortingStrategy,
} from "@dnd-kit/sortable";
import { Plus } from "lucide-react";
import { Badge } from "@notrelix/ui-web";
import { Button } from "@notrelix/ui-web";
import { ScrollArea } from "@notrelix/ui-web";
import { Input } from "@notrelix/ui-web";
import { cn } from "@notrelix/ui-web";
import type { Board, BoardGroup } from "@notrelix/work-management-core";
import { KanbanAddCard } from "./kanban-add-card";
import { KanbanCard } from "./kanban-card";
import { KanbanColumnMenu } from "./kanban-column-menu";

export function KanbanColumn({
  board,
  group,
  workspaceId,
  onOpenDetails,
  onRename,
  onColorChange,
  onDelete,
  onDuplicateCard,
  onDeleteCard,
}: {
  board: Board;
  group: BoardGroup;
  workspaceId: string;
  onOpenDetails: (cardId: string) => void;
  onRename: (title: string) => void;
  onColorChange: (color: string) => void;
  onDelete: () => void;
  onDuplicateCard: (cardId: string) => void;
  onDeleteCard: (cardId: string) => void;
}) {
  const [isAdding, setIsAdding] = useState(false);
  const [isEditing, setIsEditing] = useState(false);
  const [titleInput, setTitleInput] = useState(group.title);
  const editInputRef = useRef<HTMLInputElement>(null);

  const { setNodeRef, isOver } = useDroppable({
    id: group.id,
    data: { type: "kanban-column", group },
  });

  useEffect(() => {
    if (isEditing) {
      requestAnimationFrame(() => editInputRef.current?.focus());
    }
  }, [isEditing]);

  const handleRenameSubmit = () => {
    const next = titleInput.trim();
    if (next && next !== group.title) {
      onRename(next);
    }
    setIsEditing(false);
  };

  return (
    <section
      ref={setNodeRef}
      className={cn(
        "flex w-[290px] shrink-0 flex-col overflow-hidden rounded-2xl border border-border/80 bg-muted/20 transition-all duration-150",
        isOver && "border-primary bg-accent/10",
      )}
      aria-label={`${group.title} kanban column`}
    >
      {/* Column Header */}
      <div className="flex shrink-0 items-center justify-between gap-3 border-b border-border bg-card/60 px-3.5 py-3">
        <div className="flex min-w-0 items-center gap-2 flex-1">
          <span
            className="size-2.5 rounded-full shrink-0"
            style={{ backgroundColor: group.color ?? "var(--primary)" }}
          />

          {isEditing ? (
            <Input
              ref={editInputRef}
              value={titleInput}
              onChange={(e) => setTitleInput(e.target.value)}
              onBlur={handleRenameSubmit}
              onKeyDown={(e) => {
                if (e.key === "Enter") handleRenameSubmit();
                if (e.key === "Escape") {
                  setTitleInput(group.title);
                  setIsEditing(false);
                }
              }}
              className="h-7 text-sm font-semibold px-1 py-0.5 focus-visible:ring-1 focus-visible:ring-offset-0"
            />
          ) : (
            <h2
              onDoubleClick={() => setIsEditing(true)}
              className="truncate text-sm font-semibold text-foreground cursor-pointer font-display hover:text-primary transition-colors"
            >
              {group.title}
            </h2>
          )}

          <Badge
            variant="secondary"
            className="rounded-full px-2 py-0.5 text-[10px] font-bold shrink-0 font-body"
          >
            {group.cards.length}
          </Badge>
        </div>

        <div className="flex items-center gap-0.5 shrink-0">
          <Button
            variant="ghost"
            size="icon-xs"
            className="size-7"
            aria-label={`Add card to ${group.title}`}
            onClick={() => setIsAdding(true)}
          >
            <Plus className="size-4" />
          </Button>
          <KanbanColumnMenu
            onRename={() => setIsEditing(true)}
            onColorChange={onColorChange}
            onDelete={onDelete}
          />
        </div>
      </div>

      {/* Cards Scroller */}
      <ScrollArea className="min-h-0 flex-1">
        <SortableContext
          items={group.cards.map((card) => card.id)}
          strategy={verticalListSortingStrategy}
        >
          <div className="space-y-3 p-3">
            {group.cards.map((card) => (
              <KanbanCard
                key={card.id}
                board={board}
                card={card}
                onOpenDetails={onOpenDetails}
                onDuplicate={() => onDuplicateCard(card.id)}
                onDelete={() => onDeleteCard(card.id)}
              />
            ))}
            <KanbanAddCard
              boardId={board.id}
              workspaceId={workspaceId}
              group={group}
              isAdding={isAdding}
              onToggleAdding={setIsAdding}
            />
          </div>
        </SortableContext>
      </ScrollArea>
    </section>
  );
}
