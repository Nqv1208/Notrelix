import {
  useEffect,
  useRef,
  useState,
  type ReactNode,
  type RefObject,
} from "react";
import {
  DndContext,
  KeyboardSensor,
  PointerSensor,
  closestCenter,
  useSensor,
  useSensors,
  type DragEndEvent,
} from "@dnd-kit/core";
import { restrictToHorizontalAxis } from "@dnd-kit/modifiers";
import {
  SortableContext,
  arrayMove,
  horizontalListSortingStrategy,
  sortableKeyboardCoordinates,
  useSortable,
} from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import { MoreHorizontal, Plus } from "lucide-react";
import { Button, cn } from "@notrelix/ui-web";

import type { WorkspaceView } from "../../core/types/workspace";

export interface WorkspaceViewTabsSurfaceProps {
  workspaceId: string;
  views: WorkspaceView[];
  activeViewId?: string;
  onSelectView?: (view: WorkspaceView) => void;
  onReorderViews?: (orderedViewIds: string[]) => void;
  onAddView?: () => void;
  addViewControl?: ReactNode;
  onMoreActions?: () => void;
}

export function WorkspaceViewTabsSurface({
  workspaceId: _workspaceId,
  views,
  activeViewId,
  onSelectView,
  onReorderViews,
  onAddView,
  addViewControl,
  onMoreActions,
}: WorkspaceViewTabsSurfaceProps) {
  const [items, setItems] = useState<WorkspaceView[]>(views);
  const isDraggingRef = useRef(false);
  const cleanupClickRef = useRef<(() => void) | null>(null);
  const sensors = useSensors(
    useSensor(PointerSensor, {
      activationConstraint: { distance: 8 },
    }),
    useSensor(KeyboardSensor, {
      coordinateGetter: sortableKeyboardCoordinates,
    }),
  );

  useEffect(() => {
    setItems(views);
  }, [views]);

  useEffect(() => {
    return () => cleanupClickRef.current?.();
  }, []);

  const handleDragStart = () => {
    isDraggingRef.current = true;
    cleanupClickRef.current?.();

    const preventClick = (event: MouseEvent) => {
      event.stopImmediatePropagation();
      event.preventDefault();
    };

    window.addEventListener("click", preventClick, true);
    cleanupClickRef.current = () => {
      window.removeEventListener("click", preventClick, true);
    };
  };

  const handleDragEnd = (event: DragEndEvent) => {
    const { active, over } = event;

    if (over && active.id !== over.id) {
      setItems((currentItems) => {
        const oldIndex = currentItems.findIndex(
          (item) => item.id === active.id,
        );
        const newIndex = currentItems.findIndex((item) => item.id === over.id);
        const reordered = arrayMove(currentItems, oldIndex, newIndex);
        onReorderViews?.(reordered.map((view) => view.id));
        return reordered;
      });
    }

    isDraggingRef.current = false;
    window.setTimeout(() => {
      cleanupClickRef.current?.();
      cleanupClickRef.current = null;
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
                    active={view.id === activeViewId}
                    isDraggingParentRef={isDraggingRef}
                    onSelectView={onSelectView}
                  />
                ))}
              </div>
            </SortableContext>
          </DndContext>
        </div>
        {addViewControl ??
          (onAddView ? (
            <Button
              variant="ghost"
              size="sm"
              className="h-9 rounded-full px-2.5"
              onClick={onAddView}
            >
              <Plus className="size-4" />
              <span className="sr-only sm:not-sr-only">Add view</span>
            </Button>
          ) : null)}
        {onMoreActions ? (
          <Button
            variant="ghost"
            size="icon"
            aria-label="More view actions"
            onClick={onMoreActions}
          >
            <MoreHorizontal className="size-4" />
          </Button>
        ) : null}
      </div>
    </div>
  );
}

function SortableTabItem({
  view,
  active,
  isDraggingParentRef,
  onSelectView,
}: {
  view: WorkspaceView;
  active: boolean;
  isDraggingParentRef: RefObject<boolean>;
  onSelectView?: (view: WorkspaceView) => void;
}) {
  const {
    attributes,
    listeners,
    setNodeRef,
    transform,
    transition,
    isDragging,
  } = useSortable({ id: view.id });

  return (
    <div
      ref={setNodeRef}
      style={{
        transform: CSS.Transform.toString(transform),
        transition,
        opacity: isDragging ? 0.6 : 1,
        zIndex: isDragging ? 50 : "auto",
      }}
      {...attributes}
      {...listeners}
      className={cn(
        "inline-flex cursor-grab touch-none select-none rounded-lg active:cursor-grabbing",
        isDragging && "bg-accent/40 shadow-md",
      )}
    >
      <button
        type="button"
        role="tab"
        aria-selected={active}
        onClick={(event) => {
          if (isDraggingParentRef.current) {
            event.preventDefault();
            event.stopPropagation();
            return;
          }
          onSelectView?.(view);
        }}
        className={cn(
          "relative inline-flex h-9 items-center gap-1.5 rounded-lg px-3 text-sm font-medium text-muted-foreground transition hover:bg-muted/80 hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring",
          active && "bg-muted/40 font-semibold text-foreground",
        )}
      >
        {view.name}
        {active ? (
          <span className="absolute inset-x-2 -bottom-1 h-0.5 rounded-full bg-primary" />
        ) : null}
      </button>
    </div>
  );
}
