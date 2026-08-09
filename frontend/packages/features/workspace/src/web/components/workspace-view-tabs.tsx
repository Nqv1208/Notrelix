import { useState, useEffect, useRef, useMemo } from "react";
import { Link } from "@tanstack/react-router";
import { MoreHorizontal } from "lucide-react";
import { Button, cn } from "@notrelix/ui-web";
import type { WorkspaceView } from "../../core/types/workspace";
import { createUseReorderWorkspaceViews } from "..";
import { WorkspaceAddViewMenu } from "./workspace-add-view-menu";

import {
  DndContext,
  closestCenter,
  KeyboardSensor,
  PointerSensor,
  useSensor,
  useSensors,
  type DragEndEvent,
} from "@dnd-kit/core";
import { restrictToHorizontalAxis } from "@dnd-kit/modifiers";
import {
  arrayMove,
  SortableContext,
  sortableKeyboardCoordinates,
  horizontalListSortingStrategy,
  useSortable,
} from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";

function getViewLink(
  workspaceId: string,
  view: WorkspaceView,
  currentBoardId?: string,
): { to: string; params: Record<string, string> } {
  switch (view.type) {
    case "kanban":
    case "table":
    case "calendar":
    case "timeline": {
      const boardId = view.target.boardId || currentBoardId || "";
      return {
        to: "/workspaces/$workspaceId/boards/$boardId",
        params: { workspaceId, boardId },
      };
    }
    case "doc": {
      const docId = view.target.pageId || "";
      return {
        to: "/workspaces/$workspaceId/docs/$docId",
        params: { workspaceId, docId },
      };
    }
    case "dashboard":
      return {
        to: "/workspaces/$workspaceId/dashboard",
        params: { workspaceId },
      };
    default:
      return {
        to: "/workspaces/$workspaceId",
        params: { workspaceId },
      };
  }
}

export function WorkspaceViewTabs({
  workspaceId,
  views,
  activeViewId,
  currentBoardId,
  reorderHook: customReorderHook,
  api,
}: {
  workspaceId: string;
  views: WorkspaceView[];
  activeViewId?: string;
  currentBoardId?: string;
  reorderHook?: ReturnType<typeof createUseReorderWorkspaceViews>;
  api?: any;
}) {
  const defaultReorderHook = useMemo(
    () => createUseReorderWorkspaceViews({ api }),
    [api],
  );
  const reorderHook = customReorderHook || defaultReorderHook;

  const [items, setItems] = useState<WorkspaceView[]>(views);
  const isDraggingRef = useRef(false);
  const cleanupClickRef = useRef<(() => void) | null>(null);
  const reorderMutation = reorderHook(workspaceId);

  useEffect(() => {
    setItems(views);
  }, [views]);

  useEffect(() => {
    return () => {
      if (cleanupClickRef.current) {
        cleanupClickRef.current();
      }
    };
  }, []);

  const sensors = useSensors(
    useSensor(PointerSensor, {
      activationConstraint: { distance: 8 },
    }),
    useSensor(KeyboardSensor, {
      coordinateGetter: sortableKeyboardCoordinates,
    }),
  );

  const handleDragStart = () => {
    isDraggingRef.current = true;
    if (cleanupClickRef.current) {
      cleanupClickRef.current();
      cleanupClickRef.current = null;
    }
    const preventClick = (e: MouseEvent) => {
      e.stopImmediatePropagation();
      e.preventDefault();
    };
    window.addEventListener("click", preventClick, true);
    cleanupClickRef.current = () => {
      window.removeEventListener("click", preventClick, true);
    };
  };

  const handleDragEnd = (event: DragEndEvent) => {
    const { active, over } = event;
    if (over && active.id !== over.id) {
      setItems((prevItems) => {
        const oldIndex = prevItems.findIndex((item) => item.id === active.id);
        const newIndex = prevItems.findIndex((item) => item.id === over.id);
        const updated = arrayMove(prevItems, oldIndex, newIndex);
        reorderMutation.mutate(updated.map((v) => v.id));
        return updated;
      });
    }
    isDraggingRef.current = false;
    setTimeout(() => {
      if (cleanupClickRef.current) {
        cleanupClickRef.current();
        cleanupClickRef.current = null;
      }
    }, 100);
  };

  return (
    <div className="border-b border-border bg-card">
      <div className="flex min-w-0 items-center gap-2 px-4 sm:px-6">
        <div className="min-w-0 flex-1 overflow-x-auto whitespace-nowrap scrollbar-none">
          <DndContext
            sensors={sensors}
            collisionDetection={closestCenter}
            onDragStart={handleDragStart}
            onDragEnd={handleDragEnd}
            modifiers={[restrictToHorizontalAxis]}
          >
            <SortableContext
              items={items.map((item) => item.id)}
              strategy={horizontalListSortingStrategy}
            >
              <div
                role="tablist"
                aria-label="Workspace views"
                className="flex h-12 items-center gap-1.5 py-1"
              >
                {items.map((view) => (
                  <SortableTabItem
                    key={view.id}
                    view={view}
                    workspaceId={workspaceId}
                    active={view.id === activeViewId}
                    currentBoardId={currentBoardId}
                    isDraggingParentRef={isDraggingRef}
                  />
                ))}
              </div>
            </SortableContext>
          </DndContext>
        </div>
        <WorkspaceAddViewMenu workspaceId={workspaceId} api={api} />
        <Button variant="ghost" size="icon" aria-label="More view actions">
          <MoreHorizontal className="size-4" />
        </Button>
      </div>
    </div>
  );
}

function SortableTabItem({
  view,
  workspaceId,
  active,
  currentBoardId,
  isDraggingParentRef,
}: {
  view: WorkspaceView;
  workspaceId: string;
  active: boolean;
  currentBoardId?: string;
  isDraggingParentRef: React.RefObject<boolean | null>;
}) {
  const {
    attributes,
    listeners,
    setNodeRef,
    transform,
    transition,
    isDragging,
  } = useSortable({ id: view.id });

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
    opacity: isDragging ? 0.6 : 1,
    zIndex: isDragging ? 50 : ("auto" as const),
  };

  const handleClick = (e: React.MouseEvent) => {
    if (isDraggingParentRef.current) {
      e.preventDefault();
      e.stopPropagation();
    }
  };

  const link = getViewLink(workspaceId, view, currentBoardId);

  return (
    <div
      ref={setNodeRef}
      style={style}
      {...attributes}
      {...listeners}
      className={cn(
        "inline-flex cursor-grab active:cursor-grabbing touch-none select-none rounded-lg",
        isDragging && "shadow-md bg-accent/40",
      )}
    >
      <Link
        to={link.to as never}
        params={link.params as never}
        role="tab"
        aria-selected={active}
        onClick={handleClick}
        className={cn(
          "relative inline-flex h-9 items-center gap-1.5 rounded-lg px-3 text-sm font-medium text-muted-foreground transition hover:bg-muted/80 hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring",
          active && "text-foreground bg-muted/40 font-semibold",
        )}
      >
        {view.name}
        {active ? (
          <span className="absolute inset-x-2 -bottom-1 h-0.5 rounded-full bg-primary" />
        ) : null}
      </Link>
    </div>
  );
}
