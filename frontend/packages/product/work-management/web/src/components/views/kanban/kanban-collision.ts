import {
  closestCenter,
  type ClientRect,
  type CollisionDetection,
  type DroppableContainer,
} from "@dnd-kit/core";

/** `data.current.type` of the lane droppables and the card sortables. */
export const KANBAN_LANE_TYPE = "kanban-lane";
export const KANBAN_CARD_TYPE = "kanban-card";

function typeOf(container: DroppableContainer): unknown {
  return container.data.current?.type;
}

/** The lane under a horizontal coordinate: the one holding it, else the nearest. */
function laneAtX(
  lanes: readonly DroppableContainer[],
  rects: ReadonlyMap<string | number, ClientRect>,
  x: number,
): DroppableContainer | undefined {
  let nearest: { lane: DroppableContainer; distance: number } | undefined;
  for (const lane of lanes) {
    const rect = rects.get(lane.id);
    if (!rect) continue;
    const distance =
      x < rect.left ? rect.left - x : x > rect.right ? x - rect.right : 0;
    if (distance === 0) return lane;
    if (!nearest || distance < nearest.distance) nearest = { lane, distance };
  }
  return nearest?.lane;
}

/**
 * Collisions for the Kanban: the lane is chosen by the horizontal position of
 * the pointer (a lane is only as tall as its cards, so a drag below a short
 * lane still belongs to it), then the card of that lane nearest to the dragged
 * card, or the lane itself when it holds no other card.
 */
export const kanbanCollisionDetection: CollisionDetection = (args) => {
  const {
    droppableContainers,
    droppableRects,
    pointerCoordinates,
    collisionRect,
  } = args;
  const lanes = droppableContainers.filter(
    (container) => typeOf(container) === KANBAN_LANE_TYPE,
  );
  const x =
    pointerCoordinates?.x ?? collisionRect.left + collisionRect.width / 2;
  const lane = laneAtX(lanes, droppableRects, x);
  if (!lane) return [];
  const laneCards = droppableContainers.filter(
    (container) =>
      typeOf(container) === KANBAN_CARD_TYPE &&
      container.data.current?.laneKey === lane.data.current?.laneKey,
  );
  if (laneCards.length === 0) return [{ id: lane.id }];
  return closestCenter({ ...args, droppableContainers: laneCards });
};
