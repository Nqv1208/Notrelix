import { useMemo } from "react";
import { useSortable } from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import {
  CalendarDays,
  CheckCircle2,
  FileText,
  GripVertical,
  MessageSquareText,
  MoreHorizontal,
  Paperclip,
} from "lucide-react";
import { format } from "date-fns";
import { Avatar, AvatarFallback } from "@notrelix/ui-web";
import { Badge } from "@notrelix/ui-web";
import { Progress } from "@notrelix/ui-web";
import { Button } from "@notrelix/ui-web";
import { cn } from "@notrelix/ui-web";
import type { Board, Card } from "@notrelix/work-management-core";
import { KanbanCardMenu } from "./kanban-card-menu";

const priorityColors: Record<
  string,
  { bg: string; text: string; border: string }
> = {
  urgent: {
    bg: "rgba(246, 73, 50, 0.1)",
    text: "#f64932",
    border: "rgba(246, 73, 50, 0.3)",
  },
  high: {
    bg: "rgba(97, 97, 255, 0.1)",
    text: "#6161ff",
    border: "rgba(97, 97, 255, 0.3)",
  },
  medium: {
    bg: "rgba(255, 201, 94, 0.15)",
    text: "#ffb110",
    border: "rgba(255, 201, 94, 0.4)",
  },
  low: {
    bg: "rgba(103, 104, 121, 0.1)",
    text: "#676879",
    border: "rgba(103, 104, 121, 0.3)",
  },
};

export function KanbanCard({
  board: _board,
  card,
  onOpenDetails,
  onDuplicate,
  onDelete,
}: {
  board: Board;
  card: Card;
  onOpenDetails: (cardId: string) => void;
  onDuplicate: () => void;
  onDelete: () => void;
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
    data: { type: "kanban-card", card },
  });

  const priorityStyle = card.priority ? priorityColors[card.priority] : null;

  const checklistProgress = useMemo(() => {
    const checklists = card.checklists || [];
    const total = checklists.reduce(
      (count, checklist) => count + (checklist.items?.length || 0),
      0,
    );
    const done = checklists.reduce(
      (count, checklist) =>
        count + (checklist.items?.filter((item) => item.isDone)?.length || 0),
      0,
    );
    return total === 0 ? 0 : Math.round((done / total) * 100);
  }, [card.checklists]);

  const checklistTotalItems = useMemo(() => {
    return (card.checklists || []).reduce(
      (count, cl) => count + (cl.items?.length || 0),
      0,
    );
  }, [card.checklists]);

  return (
    <article
      ref={setNodeRef}
      style={{
        transform: CSS.Transform.toString(transform),
        transition,
        zIndex: isDragging ? 1000 : undefined,
      }}
      className={cn(
        "group relative flex flex-col rounded-xl border border-border bg-card p-3 shadow-xs transition hover:-translate-y-0.5 hover:shadow-md cursor-pointer",
        isDragging && "opacity-60 border-primary",
      )}
      aria-label={card.title}
      aria-grabbed={isDragging}
    >
      <button
        type="button"
        className="absolute inset-0 z-10 rounded-xl focus-visible:outline-hidden focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
        aria-label={card.title}
        onClick={() => onOpenDetails(card.id)}
        onKeyDown={(event) => {
          if (event.key === "Enter" || event.key === " ") {
            event.preventDefault();
            onOpenDetails(card.id);
          }
        }}
      />

      <div className="relative mb-2 flex items-start gap-2">
        {/* Grip Handle */}
        <button
          type="button"
          className="relative z-20 mt-0.5 cursor-grab rounded p-0.5 text-muted-foreground/40 transition-colors hover:bg-muted/50 group-hover:text-muted-foreground/80 active:cursor-grabbing"
          aria-label={`Move ${card.title}`}
          onClick={(e) => e.stopPropagation()}
          {...attributes}
          {...listeners}
        >
          <GripVertical className="size-3.5" aria-hidden="true" />
        </button>

        {/* Card Title */}
        <div className="min-w-0 flex-1">
          <p className="line-clamp-2 text-sm font-semibold leading-5 text-foreground font-display">
            {card.title}
          </p>
          {card.linkedPageId ? (
            <div className="mt-1 flex items-center gap-1 text-[11px] text-primary font-medium">
              <FileText className="size-3" aria-hidden="true" />
              <span>Doc Linked</span>
            </div>
          ) : null}
        </div>

        {/* Actions Button */}
        <div
          className="relative z-20 opacity-0 transition-opacity group-hover:opacity-100"
          onClick={(e) => e.stopPropagation()}
        >
          <KanbanCardMenu
            cardId={card.id}
            onDuplicate={onDuplicate}
            onDelete={onDelete}
          >
            <Button
              variant="ghost"
              size="icon-xs"
              className="size-6"
              aria-label={`Open ${card.title} menu`}
            >
              <MoreHorizontal className="size-3.5" aria-hidden="true" />
            </Button>
          </KanbanCardMenu>
        </div>
      </div>

      {/* Priority Badge */}
      {card.priority && priorityStyle ? (
        <div className="relative z-20 mb-2">
          <Badge
            variant="secondary"
            className="rounded-md border px-2 py-0 h-5 text-[10px] font-semibold uppercase tracking-wider font-display"
            style={{
              backgroundColor: priorityStyle.bg,
              color: priorityStyle.text,
              borderColor: priorityStyle.border,
            }}
          >
            {card.priority}
          </Badge>
        </div>
      ) : null}

      {/* Checklist Progress */}
      {checklistTotalItems > 0 ? (
        <div className="relative z-20 mb-3 space-y-1.5">
          <div className="flex items-center justify-between text-[11px] text-muted-foreground">
            <span className="inline-flex items-center gap-1 font-body">
              <CheckCircle2
                className="size-3 text-muted-foreground/60"
                aria-hidden="true"
              />
              Checklist
            </span>
            <span className="font-semibold">{checklistProgress}%</span>
          </div>
          <Progress value={checklistProgress} className="h-1.5" />
        </div>
      ) : null}

      {/* Bottom Metadata row */}
      <div className="relative z-20 mt-auto flex items-center justify-between gap-2 border-t border-border/40 pt-2 text-xs text-muted-foreground font-body">
        <div className="flex items-center gap-2.5">
          {card.dueDate ? (
            <span className="inline-flex items-center gap-1 text-[11px]">
              <CalendarDays
                className="size-3 text-muted-foreground/60"
                aria-hidden="true"
              />
              {format(new Date(card.dueDate), "MMM d")}
            </span>
          ) : null}
          {card._count.comments > 0 ? (
            <span className="inline-flex items-center gap-1 text-[11px]">
              <MessageSquareText
                className="size-3 text-muted-foreground/60"
                aria-hidden="true"
              />
              {card._count.comments}
            </span>
          ) : null}
          {card._count.attachments > 0 ? (
            <span className="inline-flex items-center gap-1 text-[11px]">
              <Paperclip
                className="size-3 text-muted-foreground/60"
                aria-hidden="true"
              />
              {card._count.attachments}
            </span>
          ) : null}
        </div>

        {/* Assignee Avatar */}
        <div className="-space-x-1.5 flex shrink-0">
          {card.members?.slice(0, 3).map((member) => (
            <Avatar
              key={member.id}
              className="inline-flex size-6 border-2 border-card ring-1 ring-border/20"
            >
              <AvatarFallback
                className="text-[9px] font-bold text-primary-foreground"
                style={{ backgroundColor: member.color }}
              >
                {member.initials}
              </AvatarFallback>
            </Avatar>
          ))}
        </div>
      </div>
    </article>
  );
}
