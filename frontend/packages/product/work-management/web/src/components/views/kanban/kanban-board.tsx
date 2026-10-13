import { useState } from "react";
import {
  closestCorners,
  DndContext,
  PointerSensor,
  useSensor,
  useSensors,
  type DragEndEvent,
} from "@dnd-kit/core";
import type { KanbanStatusLane } from "@notrelix/work-management-core";
import { pluralCopy, type KanbanCardCopy } from "./kanban-card.copy";
import { KanbanColumn } from "./kanban-column";
import { getKanbanLaneDrop, type KanbanLaneDrop } from "./kanban-dnd";
import { KanbanEmptyState } from "./kanban-empty-state";
import type { KanbanLaneCopy } from "./kanban-lane.copy";

export interface KanbanBoardProps {
  /** Lanes in option order, filter applied (`total` is the lane total). */
  lanes: readonly KanbanStatusLane[];
  copy: KanbanLaneCopy;
  cardCopy?: KanbanCardCopy;
  /** Reference time for card due wording; read once by the container. */
  now?: Date;
  timeZone?: string;
  selectedItemId?: string;
  /** A filter or search narrows the board. */
  filterActive: boolean;
  /** "Thêm vào {lane}" is offered. */
  canAdd: boolean;
  /** Cards can be dragged between and within lanes (pointer only). */
  canDrag: boolean;
  /**
   * The grouping field's kind: names the lane without a value ("Không có
   * trạng thái" for Status, "Chưa chọn" for another Select field).
   */
  groupKind?: "status" | "select";
  /** Changing this number opens the add field of the first open lane. */
  addRequest?: number;
  onOpenDetails: (cardId: string) => void;
  /** A card was dropped: another lane's value, or a reorder in its group. */
  onDropCard: (drop: KanbanLaneDrop) => void;
  onCreateItem: (lane: KanbanStatusLane, title: string) => void;
}

function laneKey(lane: KanbanStatusLane): string {
  return lane.optionId ?? "none";
}

/**
 * The Kanban lanes, side by side. Collapsing and hiding a lane are view
 * conveniences kept on this screen only; "Hiện N cột đã ẩn" brings hidden
 * lanes back. A drag is pointer-only; the keyboard alternative is the item
 * sheet's status pill.
 */
export function KanbanBoard({
  lanes,
  copy,
  cardCopy,
  now,
  timeZone,
  selectedItemId,
  filterActive,
  canAdd,
  canDrag,
  groupKind = "status",
  addRequest,
  onOpenDetails,
  onDropCard,
  onCreateItem,
}: KanbanBoardProps) {
  const [collapsed, setCollapsed] = useState<ReadonlySet<string>>(new Set());
  const [hidden, setHidden] = useState<ReadonlySet<string>>(new Set());
  const pointer = useSensors(
    useSensor(PointerSensor, { activationConstraint: { distance: 8 } }),
  );
  const none = useSensors();

  function handleDragEnd(event: DragEndEvent) {
    const drop = getKanbanLaneDrop(event, lanes);
    if (drop) onDropCard(drop);
  }

  const toggle = (setter: typeof setCollapsed) => (key: string, on: boolean) =>
    setter((current) => {
      const next = new Set(current);
      if (on) next.add(key);
      else next.delete(key);
      return next;
    });
  const setLaneCollapsed = toggle(setCollapsed);
  const setLaneHidden = toggle(setHidden);

  if (lanes.length === 0) {
    return <KanbanEmptyState message={copy.empty} />;
  }

  const noValueName = groupKind === "status" ? copy.noStatus : copy.noValue;
  const shown = lanes.filter((lane) => !hidden.has(laneKey(lane)));
  const hiddenCount = lanes.length - shown.length;
  const addLane = shown.find((lane) => !collapsed.has(laneKey(lane)));

  return (
    <DndContext
      sensors={canDrag ? pointer : none}
      collisionDetection={closestCorners}
      onDragEnd={handleDragEnd}
    >
      <div className="flex min-w-min items-start gap-3">
        {shown.map((lane) => {
          const key = laneKey(lane);
          return (
            <KanbanColumn
              key={key}
              lane={lane}
              name={lane.name || noValueName}
              copy={copy}
              cardCopy={cardCopy}
              now={now}
              timeZone={timeZone}
              selectedItemId={selectedItemId}
              filterActive={filterActive}
              canAdd={canAdd}
              canDrag={canDrag}
              collapsed={collapsed.has(key)}
              openAddRequest={lane === addLane ? addRequest : undefined}
              onCollapsedChange={(on) => setLaneCollapsed(key, on)}
              onHide={() => setLaneHidden(key, true)}
              onOpenDetails={onOpenDetails}
              onCreateItem={(title) => onCreateItem(lane, title)}
            />
          );
        })}
        {hiddenCount > 0 ? (
          <button
            type="button"
            onClick={() => setHidden(new Set())}
            className="h-8 shrink-0 rounded-lg border border-dashed border-border px-3 text-metadata font-medium text-muted-foreground hover:bg-state-hover hover:text-foreground focus-visible:outline-hidden focus-visible:ring-2 focus-visible:ring-focus-ring"
          >
            {pluralCopy(
              { one: copy.showHidden_one, other: copy.showHidden_other },
              hiddenCount,
            )}
          </button>
        ) : null}
      </div>
    </DndContext>
  );
}
