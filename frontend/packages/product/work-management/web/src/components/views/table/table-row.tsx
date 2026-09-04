import React, { type KeyboardEvent, type MouseEvent } from "react";
import { MoreHorizontal, GripVertical } from "lucide-react";
import { useSortable } from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import { Checkbox } from "@notrelix/ui-web";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@notrelix/ui-web";
import type {
  Board,
  BoardGroup,
  BoardTableColumn,
  Card,
  UpdateCardInput,
} from "@notrelix/work-management-core";
import { cn } from "@notrelix/ui-web";
import { TableCell, type TableFieldValueUpdate } from "./table-cell";

export function TableRow({
  board,
  group,
  card,
  columns,
  gridTemplate,
  groupColor,
  isChecked,
  isDetailSelected,
  onSelect,
  onOpenDetail,
  onDuplicateCard,
  onDeleteCard,
  onUpdateCard,
  onUpdateFieldValue,
}: {
  board: Board;
  group: BoardGroup;
  card: Card;
  columns: BoardTableColumn[];
  gridTemplate: string;
  groupColor?: string;
  isChecked: boolean;
  isDetailSelected: boolean;
  onSelect: (selected: boolean) => void;
  onOpenDetail: () => void;
  onDuplicateCard: (cardId: string) => void;
  onDeleteCard: (cardId: string) => void;
  onUpdateCard: (cardId: string, patch: UpdateCardInput) => void;
  onUpdateFieldValue: (update: TableFieldValueUpdate) => void;
}) {
  const {
    attributes,
    listeners,
    setNodeRef,
    transform,
    transition,
    isDragging,
  } = useSortable({
    id: card.id,
    data: { type: "card", card, group },
  });
  const accent = groupColor ?? "transparent";
  const rowTransition = [
    transition,
    "background-color 200ms ease",
    "box-shadow 200ms ease",
    "border-left-color 200ms ease",
  ]
    .filter(Boolean)
    .join(", ");

  return (
    <div
      ref={setNodeRef}
      tabIndex={0}
      onKeyDown={(event: KeyboardEvent<HTMLDivElement>) => {
        if (event.key === "Enter" || event.key === " ") {
          event.preventDefault();
          onOpenDetail();
        }
      }}
      aria-label={`${card.title} in ${group.title}`}
      data-selected={isDetailSelected ? "true" : "false"}
      data-dragging={isDragging ? "true" : "false"}
      className={cn(
        "group/row grid min-h-12 cursor-pointer items-center border-b border-l-[6px] border-border/70 bg-table-row text-sm duration-150 ease-out hover:bg-table-row-hover",
        isChecked && "bg-table-selected/40 hover:bg-table-selected/50",
        isDetailSelected &&
          "bg-table-selected ring-1 ring-inset ring-primary/40",
        isDragging && "relative z-10 shadow-xl shadow-black/30",
      )}
      style={
        {
          gridTemplateColumns: gridTemplate,
          transform: CSS.Transform.toString(transform),
          transition: rowTransition,
          opacity: isDragging ? 0.72 : 1,
          "--group-color": accent,
          borderLeftColor: `color-mix(in oklch, ${accent} 22%, transparent)`,
        } as React.CSSProperties & { "--group-color": string }
      }
      onMouseEnter={(event: MouseEvent<HTMLDivElement>) => {
        event.currentTarget.style.borderLeftColor = accent;
      }}
      onMouseLeave={(event: MouseEvent<HTMLDivElement>) => {
        event.currentTarget.style.borderLeftColor = `color-mix(in oklch, ${accent} 22%, transparent)`;
      }}
      onClick={onOpenDetail}
    >
      <div className="sticky left-0 z-10 flex h-full items-center gap-2 border-r border-border/70 bg-inherit px-3">
        <Checkbox
          checked={isChecked}
          onCheckedChange={(checked) => onSelect(Boolean(checked))}
          onClick={(event: MouseEvent<HTMLButtonElement>) =>
            event.stopPropagation()
          }
          aria-label={`Select ${card.title}`}
          className="opacity-0 transition-opacity group-hover/row:opacity-100 data-[state=checked]:opacity-100"
        />
        <button
          type="button"
          className="cursor-grab rounded p-0.5 text-muted-foreground/40 opacity-0 transition hover:text-muted-foreground active:cursor-grabbing group-hover/row:opacity-100"
          aria-label={`Move ${card.title}`}
          onClick={(event: MouseEvent<HTMLButtonElement>) =>
            event.stopPropagation()
          }
          {...attributes}
          {...listeners}
        >
          <GripVertical className="size-3.5" aria-hidden />
        </button>
      </div>
      {columns.map((column) => (
        <div
          key={column.id}
          className="flex min-w-0 items-center border-r border-border/70 px-3 py-2"
        >
          <TableCell
            board={board}
            card={card}
            field={column.field}
            onOpenDetail={onOpenDetail}
            onUpdateCard={onUpdateCard}
            onUpdateFieldValue={onUpdateFieldValue}
          />
        </div>
      ))}
      <div className="flex h-full items-center justify-center border-r border-border/70 px-2">
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <button
              type="button"
              className="rounded p-1 text-muted-foreground opacity-0 transition hover:bg-foreground/8 hover:text-foreground group-hover/row:opacity-100"
              aria-label={`${card.title} row menu`}
              onClick={(event: MouseEvent<HTMLButtonElement>) =>
                event.stopPropagation()
              }
            >
              <MoreHorizontal className="size-4" />
            </button>
          </DropdownMenuTrigger>
          <DropdownMenuContent
            align="end"
            onClick={(event: MouseEvent<HTMLDivElement>) =>
              event.stopPropagation()
            }
          >
            <DropdownMenuItem onClick={onOpenDetail}>
              Open details
            </DropdownMenuItem>
            <DropdownMenuItem onClick={() => onDuplicateCard(card.id)}>
              Duplicate task
            </DropdownMenuItem>
            <DropdownMenuItem
              className="text-destructive"
              onClick={() => onDeleteCard(card.id)}
            >
              Delete task
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </div>
    </div>
  );
}
