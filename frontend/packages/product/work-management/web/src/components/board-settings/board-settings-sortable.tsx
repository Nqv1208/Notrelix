import { useEffect, useState, type ReactNode } from "react";
import {
  DndContext,
  KeyboardSensor,
  PointerSensor,
  closestCenter,
  useSensor,
  useSensors,
  type DragEndEvent,
} from "@dnd-kit/core";
import {
  SortableContext,
  arrayMove,
  sortableKeyboardCoordinates,
  useSortable,
  verticalListSortingStrategy,
} from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import { restrictToVerticalAxis } from "@dnd-kit/modifiers";
import { GripVertical } from "lucide-react";
import { cn } from "@notrelix/ui-web";

/**
 * A vertical list the member reorders by dragging a row's handle or with the
 * keyboard (focus the handle, Space, arrows, Space). The order shown follows
 * `ids` and is kept locally while the write is on its way.
 */
export function SortableList({
  ids,
  onReorder,
  disabled,
  children,
}: {
  ids: readonly string[];
  onReorder: (ids: string[]) => void;
  disabled?: boolean;
  children: (order: readonly string[]) => ReactNode;
}) {
  const [order, setOrder] = useState<readonly string[]>(ids);
  const key = ids.join("|");
  useEffect(() => setOrder(key === "" ? [] : key.split("|")), [key]);
  const sensors = useSensors(
    useSensor(PointerSensor, { activationConstraint: { distance: 4 } }),
    useSensor(KeyboardSensor, {
      coordinateGetter: sortableKeyboardCoordinates,
    }),
  );

  function handleDragEnd({ active, over }: DragEndEvent) {
    if (!over || active.id === over.id) return;
    const from = order.indexOf(String(active.id));
    const to = order.indexOf(String(over.id));
    if (from < 0 || to < 0) return;
    const next = arrayMove([...order], from, to);
    setOrder(next);
    onReorder(next);
  }

  return (
    <DndContext
      sensors={disabled ? [] : sensors}
      collisionDetection={closestCenter}
      modifiers={[restrictToVerticalAxis]}
      // The live region and the hidden instructions render outside the list, so
      // the list owns only its rows.
      accessibility={{
        container: typeof document === "undefined" ? undefined : document.body,
      }}
      onDragEnd={handleDragEnd}
    >
      <SortableContext
        items={[...order]}
        strategy={verticalListSortingStrategy}
      >
        {children(order)}
      </SortableContext>
    </DndContext>
  );
}

/** One sortable row: `handle` is the node to drop into the grip cell. */
export function SortableRow({
  id,
  handleLabel,
  className,
  children,
  ...rest
}: {
  id: string;
  handleLabel: string;
  className?: string;
  children: (handle: ReactNode) => ReactNode;
} & Omit<React.HTMLAttributes<HTMLDivElement>, "children">) {
  const {
    attributes,
    listeners,
    setNodeRef,
    setActivatorNodeRef,
    transform,
    transition,
    isDragging,
  } = useSortable({ id });
  const { onPointerDown, onKeyDown } = (listeners ?? {}) as {
    onPointerDown?: React.PointerEventHandler<HTMLButtonElement>;
    onKeyDown?: React.KeyboardEventHandler<HTMLButtonElement>;
  };
  const handle = (
    <button
      type="button"
      ref={setActivatorNodeRef}
      {...attributes}
      onPointerDown={onPointerDown}
      onKeyDown={onKeyDown}
      aria-label={handleLabel}
      className="grid size-7 cursor-grab place-items-center rounded-[6px] border-0 bg-transparent p-0 text-meta-foreground opacity-70 outline-none hover:bg-state-hover-strong focus-visible:ring-2 focus-visible:ring-focus-ring"
    >
      <GripVertical aria-hidden="true" className="size-3.5" />
    </button>
  );
  return (
    <div
      ref={setNodeRef}
      style={{ transform: CSS.Transform.toString(transform), transition }}
      className={cn(className, isDragging && "relative z-10 shadow-md")}
      {...rest}
    >
      {children(handle)}
    </div>
  );
}
