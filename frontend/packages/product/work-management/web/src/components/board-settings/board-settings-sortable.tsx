import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from "react";
import { createPortal } from "react-dom";
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

/** What a write of the new order answered; `ok: false` puts the old order back. */
type ReorderOutcome = { readonly ok: boolean } | void;

interface SortableControls {
  /** Moves a row one place (Alt+Arrow); nothing happens at either end. */
  readonly move: (id: string, delta: -1 | 1) => void;
  readonly registerHandle: (id: string, node: HTMLButtonElement | null) => void;
}

const SortableControlsContext = createContext<SortableControls | null>(null);

/**
 * A vertical list the member reorders by dragging a row's handle, with the
 * keyboard (focus the handle, Space, arrows, Space) or with Alt+Arrow on the
 * handle. The order shown follows `ids` and is kept locally while the write
 * is on its way; when the write fails (`ok: false` or a rejection) the order
 * the list had comes back. A second move waits for the first to finish, so
 * every write names the order the server last answered.
 */
export function SortableList({
  ids,
  onReorder,
  announce,
  children,
}: {
  ids: readonly string[];
  onReorder: (ids: string[]) => ReorderOutcome | Promise<ReorderOutcome>;
  /** What a screen reader hears after a move by Alt+Arrow (position is 1-based). */
  announce?: (id: string, position: number, total: number) => string;
  children: (order: readonly string[]) => ReactNode;
}) {
  const [order, setOrder] = useState<readonly string[]>(ids);
  const [message, setMessage] = useState("");
  const key = ids.join("|");
  useEffect(() => setOrder(key === "" ? [] : key.split("|")), [key]);
  const writing = useRef(false);
  const handles = useRef(new Map<string, HTMLButtonElement>());
  const sensors = useSensors(
    useSensor(PointerSensor, { activationConstraint: { distance: 4 } }),
    useSensor(KeyboardSensor, {
      coordinateGetter: sortableKeyboardCoordinates,
    }),
  );

  const commit = useCallback(
    async (previous: readonly string[], next: string[], movedId: string) => {
      if (writing.current) return;
      writing.current = true;
      setOrder(next);
      let failed = false;
      try {
        const outcome = await onReorder(next);
        failed = typeof outcome === "object" && outcome !== null && !outcome.ok;
      } catch {
        failed = true;
      } finally {
        writing.current = false;
      }
      if (failed) setOrder(previous);
      else if (announce)
        setMessage(announce(movedId, next.indexOf(movedId) + 1, next.length));
      // The row was moved in the DOM; its handle keeps the keyboard focus.
      requestAnimationFrame(() => handles.current.get(movedId)?.focus());
    },
    [announce, onReorder],
  );

  const controls = useMemo<SortableControls>(
    () => ({
      move: (id, delta) => {
        const from = order.indexOf(id);
        const to = from + delta;
        if (from < 0 || to < 0 || to >= order.length) return;
        void commit(order, arrayMove([...order], from, to), id);
      },
      registerHandle: (id, node) => {
        if (node) handles.current.set(id, node);
        else handles.current.delete(id);
      },
    }),
    [commit, order],
  );

  function handleDragEnd({ active, over }: DragEndEvent) {
    if (!over || active.id === over.id) return;
    const from = order.indexOf(String(active.id));
    const to = order.indexOf(String(over.id));
    if (from < 0 || to < 0) return;
    void commit(order, arrayMove([...order], from, to), String(active.id));
  }

  return (
    <SortableControlsContext.Provider value={controls}>
      <DndContext
        sensors={sensors}
        collisionDetection={closestCenter}
        modifiers={[restrictToVerticalAxis]}
        // The live region and the hidden instructions render outside the list, so
        // the list owns only its rows.
        accessibility={{
          container:
            typeof document === "undefined" ? undefined : document.body,
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
      {announce && typeof document !== "undefined"
        ? createPortal(
            <span role="status" aria-live="polite" className="sr-only">
              {message}
            </span>,
            document.body,
          )
        : null}
    </SortableControlsContext.Provider>
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
  const controls = useContext(SortableControlsContext);
  const { onPointerDown, onKeyDown } = (listeners ?? {}) as {
    onPointerDown?: React.PointerEventHandler<HTMLButtonElement>;
    onKeyDown?: React.KeyboardEventHandler<HTMLButtonElement>;
  };
  const handle = (
    <button
      type="button"
      ref={(node) => {
        setActivatorNodeRef(node);
        controls?.registerHandle(id, node);
      }}
      {...attributes}
      onPointerDown={onPointerDown}
      onKeyDown={(event) => {
        if (
          controls &&
          event.altKey &&
          (event.key === "ArrowUp" || event.key === "ArrowDown")
        ) {
          event.preventDefault();
          controls.move(id, event.key === "ArrowUp" ? -1 : 1);
          return;
        }
        onKeyDown?.(event);
      }}
      aria-keyshortcuts="Alt+ArrowUp Alt+ArrowDown"
      aria-label={handleLabel}
      className="grid size-7 max-md:size-11 cursor-grab place-items-center rounded-[6px] border-0 bg-transparent p-0 text-meta-foreground opacity-70 outline-none hover:bg-state-hover-strong focus-visible:ring-2 focus-visible:ring-focus-ring"
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
