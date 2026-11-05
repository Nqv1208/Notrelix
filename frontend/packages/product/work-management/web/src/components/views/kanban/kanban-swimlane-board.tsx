import { useMemo, useState } from "react";
import {
  DndContext,
  DragOverlay,
  MouseSensor,
  TouchSensor,
  pointerWithin,
  closestCenter,
  useDroppable,
  useSensor,
  useSensors,
  type CollisionDetection,
  type DragEndEvent,
  type DragOverEvent,
  type DragStartEvent,
} from "@dnd-kit/core";
import {
  SortableContext,
  verticalListSortingStrategy,
} from "@dnd-kit/sortable";
import { ChevronDown, ChevronRight, Ban, ArrowRight } from "lucide-react";
import { cn } from "@notrelix/ui-web";
import { useHoldRealtimeRefetch } from "@notrelix/work-management-state";
import {
  planLaneDrop,
  type Card,
  type FilterField,
  type KanbanStatusLane,
  type LaneDropPlan,
  type LaneDropRefusal,
} from "@notrelix/work-management-core";
import { fillFiltersCopy, type FiltersCopy } from "../../filters/filters.copy";
import { KanbanCard, KanbanCardOverlay } from "./kanban-card";
import type { KanbanCardActions } from "./kanban-card-quick-actions";
import type { KanbanCardSelection } from "./kanban-selection";
import type { KanbanCardCopy } from "./kanban-card.copy";
import {
  SILENT_DND_ANNOUNCEMENTS,
  type KanbanMoveDirection,
} from "./kanban-dnd";

const CELL_TYPE = "swimlane-cell";
const keyOf = (optionId: string | null) => optionId ?? "none";
const cellId = (lane: string, column: string) => `cell:${lane}::${column}`;
/** One drag id per cell: the same card can show in several lanes. */
const dndIdOf = (lane: string, column: string, card: Card) =>
  `${lane}::${column}::${card.id}`;

export interface SwimlaneDrop {
  readonly card: Card;
  readonly plan: Extract<LaneDropPlan, { ok: true }>;
  /** The target cell: the column option (null: no value) and the lane's group key (null: no value). */
  readonly toColumn: string | null;
  readonly toLane: string | null;
}

export interface SwimlaneRefusal {
  readonly card: Card;
  readonly reason: LaneDropRefusal;
  readonly toColumnName: string;
  readonly limit?: number | undefined;
}

export interface KanbanSwimlaneBoardProps {
  copy: FiltersCopy;
  cardCopy: KanbanCardCopy;
  now: Date;
  /** The columns (Status or Select lanes) with the filter applied. */
  columns: readonly KanbanStatusLane[];
  /** The swimlanes: the visible cards split by the lane field (`groupCardsByField`). */
  swimlanes: readonly KanbanStatusLane[];
  laneField: FilterField;
  columnName: (column: KanbanStatusLane) => string;
  laneName: (lane: KanbanStatusLane) => string;
  wipLimits: Readonly<Record<string, number>>;
  selectedItemId: string | undefined;
  /** A filter or search is on: a lane with no card says nothing matches there instead of offering empty cells. */
  filterActive?: boolean;
  canDrag: boolean;
  cardActions: KanbanCardActions | undefined;
  /** Picking cards for the bulk bar. */
  cardSelection?: KanbanCardSelection | undefined;
  onOpenDetails: (cardId: string) => void;
  onDrop: (drop: SwimlaneDrop) => void;
  onRefused: (refusal: SwimlaneRefusal) => void;
  /** The swimlanes collapsed when the board opens (the member's own preference). */
  initialCollapsed?: ReadonlySet<string> | undefined;
  /** The collapsed swimlanes changed: the container remembers them per member and view. */
  onCollapsedChange?: ((collapsed: ReadonlySet<string>) => void) | undefined;
}

const collision: CollisionDetection = (args) => {
  const within = pointerWithin(args);
  return within.length > 0 ? within : closestCenter(args);
};

/**
 * The Kanban with swimlanes: the columns stay the Status (or Select) field,
 * the lanes are rows of a second field. Each lane collapses on its own and,
 * collapsed, still shows how many items each column holds. Dropping a card on
 * another cell writes the column field, the lane field, or both; a drop that
 * cannot be written is refused with its reason on the cell.
 */
export function KanbanSwimlaneBoard(props: KanbanSwimlaneBoardProps) {
  const { copy, columns, swimlanes, laneField } = props;
  const [collapsed, setCollapsed] = useState<ReadonlySet<string>>(
    () => new Set(props.initialCollapsed),
  );
  // The card being dragged and the lane it was picked up from: a card of
  // several people shows in each person's lane (Q13), so the card alone does
  // not say which lane it leaves.
  const [drag, setDrag] = useState<{
    cardId: string;
    lane: string | null;
  } | null>(null);
  const dragId = drag?.cardId ?? null;
  useHoldRealtimeRefetch(dragId !== null);
  const [overCell, setOverCell] = useState<string | null>(null);
  const [live, setLive] = useState("");
  const sensors = useSensors(
    useSensor(MouseSensor, { activationConstraint: { distance: 5 } }),
    useSensor(TouchSensor, {
      activationConstraint: { delay: 220, tolerance: 8 },
    }),
  );

  const columnOf = useMemo(() => {
    const map = new Map<string, string | null>();
    for (const column of columns)
      for (const card of column.cards) map.set(card.id, column.optionId);
    return map;
  }, [columns]);
  const cardsById = useMemo(() => {
    const map = new Map<string, Card>();
    for (const lane of swimlanes)
      for (const card of lane.cards) map.set(card.id, card);
    return map;
  }, [swimlanes]);
  const countByColumn = useMemo(
    () => new Map(columns.map((c) => [keyOf(c.optionId), c.total] as const)),
    [columns],
  );

  const dragged = dragId ? cardsById.get(dragId) : undefined;

  /** What dropping the dragged card on a cell does; the refusal when it cannot be written. */
  const peopleOf = (card: Card): string[] | undefined => {
    if (laneField.field.fieldType !== "person") return undefined;
    const value = laneField.read(card);
    return Array.isArray(value) ? value.map(String) : [];
  };

  const planFor = (cell: {
    lane: string | null;
    column: string | null;
  }): LaneDropPlan | null => {
    if (!dragged) return null;
    const limit = cell.column ? props.wipLimits[cell.column] : undefined;
    return planLaneDrop({
      laneField,
      fromColumn: columnOf.get(dragged.id) ?? null,
      toColumn: cell.column,
      fromLane: drag?.lane ?? null,
      toLane: cell.lane,
      people: peopleOf(dragged),
      wip:
        limit !== undefined
          ? {
              limit,
              count: countByColumn.get(keyOf(cell.column)) ?? 0,
            }
          : null,
    });
  };

  const targetOf = (
    over: DragOverEvent["over"],
  ): { lane: string | null; column: string | null; id: string } | null => {
    const data = over?.data.current as
      | { type?: string; lane?: string; column?: string; laneKey?: string }
      | undefined;
    if (!data) return null;
    const raw =
      data.type === CELL_TYPE
        ? `${data.lane}::${data.column}`
        : data.laneKey?.replace(/^cell:/, "");
    if (!raw || !raw.includes("::")) return null;
    const [lane = "none", column = "none"] = raw.split("::");
    return {
      lane: lane === "none" ? null : lane,
      column: column === "none" ? null : column,
      id: cellId(lane, column),
    };
  };

  const columnLabel = (optionId: string | null) => {
    const column = columns.find((c) => c.optionId === optionId);
    return column ? props.columnName(column) : "";
  };
  const laneLabel = (optionId: string | null) => {
    const lane = swimlanes.find((l) => l.optionId === optionId);
    return lane ? props.laneName(lane) : "";
  };

  const onDragStart = (event: DragStartEvent) => {
    const data = event.active.data.current as
      { card?: Card; laneKey?: string } | undefined;
    const origin = data?.laneKey?.replace(/^cell:/, "").split("::")[0];
    if (!data?.card) return;
    setDrag({
      cardId: data.card.id,
      lane: !origin || origin === "none" ? null : origin,
    });
  };
  const onDragOver = (event: DragOverEvent) => {
    const target = targetOf(event.over);
    setOverCell(target?.id ?? null);
    const plan = target ? planFor(target) : null;
    if (target && plan?.ok) {
      setLive(
        fillFiltersCopy(copy.lane.kbdLive, {
          lane: laneLabel(target.lane),
          column: columnLabel(target.column),
          changes: [
            plan.columnChanged ? columnLabel(target.column) : null,
            plan.laneChanged ? laneField.name : null,
          ]
            .filter(Boolean)
            .join(" / "),
        }),
      );
    }
  };
  const onDragEnd = (event: DragEndEvent) => {
    const target = targetOf(event.over);
    const card = dragged;
    setDrag(null);
    setOverCell(null);
    if (!target || !card) return;
    const plan = planFor(target);
    if (!plan) return;
    if (!plan.ok) {
      props.onRefused({
        card,
        reason: plan.reason,
        toColumnName: columnLabel(target.column),
        limit: target.column ? props.wipLimits[target.column] : undefined,
      });
      return;
    }
    if (!plan.columnChanged && !plan.laneChanged) return;
    props.onDrop({
      card,
      plan,
      toColumn: target.column,
      toLane: target.lane,
    });
  };

  const moveByKey = (
    cardId: string,
    direction: KanbanMoveDirection,
    fromLane: string | null,
  ) => {
    const card = cardsById.get(cardId);
    if (!card) return;
    const columnKeys = columns.map((c) => c.optionId);
    const laneKeys = swimlanes.map((l) => l.optionId);
    const ci = columnKeys.indexOf(columnOf.get(cardId) ?? null);
    const li = laneKeys.indexOf(fromLane);
    const nextCi =
      direction === "left" ? ci - 1 : direction === "right" ? ci + 1 : ci;
    const nextLi =
      direction === "up" ? li - 1 : direction === "down" ? li + 1 : li;
    if (
      nextCi < 0 ||
      nextLi < 0 ||
      nextCi >= columnKeys.length ||
      nextLi >= laneKeys.length
    )
      return;
    const toColumn = columnKeys[nextCi] ?? null;
    const toLane = laneKeys[nextLi] ?? null;
    const limit = toColumn ? props.wipLimits[toColumn] : undefined;
    const plan = planLaneDrop({
      laneField,
      fromColumn: columnOf.get(cardId) ?? null,
      toColumn,
      fromLane,
      toLane,
      people: peopleOf(card),
      wip:
        limit !== undefined
          ? { limit, count: countByColumn.get(keyOf(toColumn)) ?? 0 }
          : null,
    });
    if (!plan.ok) {
      props.onRefused({
        card,
        reason: plan.reason,
        toColumnName: columnLabel(toColumn),
        limit,
      });
      return;
    }
    props.onDrop({ card, plan, toColumn, toLane });
  };

  const template = `repeat(${columns.length}, minmax(15rem, 1fr))`;
  const overPlan = (lane: string | null, column: string | null) =>
    overCell === cellId(keyOf(lane), keyOf(column))
      ? planFor({ lane, column })
      : null;

  return (
    <DndContext
      sensors={sensors}
      collisionDetection={collision}
      accessibility={{ announcements: SILENT_DND_ANNOUNCEMENTS }}
      onDragStart={onDragStart}
      onDragOver={onDragOver}
      onDragEnd={onDragEnd}
      onDragCancel={() => {
        setDrag(null);
        setOverCell(null);
      }}
    >
      <div
        data-kanban-swimlanes=""
        className="min-w-min overflow-clip rounded-xl border border-line bg-popover dark:bg-card"
      >
        <div
          data-swimlane-header=""
          className="grid border-b border-line"
          style={{ gridTemplateColumns: template }}
        >
          {columns.map((column) => (
            <div
              key={keyOf(column.optionId)}
              data-swimlane-column=""
              className="flex items-center gap-2 border-r border-line px-3 py-2 text-[12.5px] font-[650] last:border-r-0"
            >
              <span className="min-w-0 truncate">
                {props.columnName(column)}
              </span>
              <span className="rounded-md bg-border px-1.5 text-[11.5px] leading-5 text-muted-foreground">
                {column.cards.length}
              </span>
            </div>
          ))}
        </div>
        {swimlanes.map((lane) => {
          const laneKey = keyOf(lane.optionId);
          const isCollapsed = collapsed.has(laneKey);
          const name = props.laneName(lane);
          return (
            <section
              key={laneKey}
              data-swimlane={laneKey}
              aria-label={name}
              className="border-b border-line last:border-b-0"
            >
              <div className="sticky top-0 z-10 bg-popover dark:bg-card">
                <div className="flex min-h-10 items-center gap-2 border-b border-line bg-state-hover px-2.5 text-[13px] font-[650]">
                  <button
                    type="button"
                    aria-expanded={!isCollapsed}
                    aria-label={fillFiltersCopy(
                      isCollapsed ? copy.lane.expand : copy.lane.collapse,
                      { name },
                    )}
                    onClick={() =>
                      setCollapsed((current) => {
                        const next = new Set(current);
                        if (next.has(laneKey)) next.delete(laneKey);
                        else next.add(laneKey);
                        props.onCollapsedChange?.(next);
                        return next;
                      })
                    }
                    className="flex min-h-8 cursor-pointer items-center gap-2 rounded-md border-0 bg-transparent px-1 text-inherit focus-visible:outline-2 focus-visible:outline-ring"
                  >
                    {isCollapsed ? (
                      <ChevronRight className="size-4" aria-hidden="true" />
                    ) : (
                      <ChevronDown className="size-4" aria-hidden="true" />
                    )}
                    <span>{name}</span>
                    <span className="rounded-md bg-background px-1.5 text-crumb text-muted-foreground">
                      {lane.cards.length}
                    </span>
                  </button>
                  {isCollapsed ? (
                    <span className="ml-3 flex flex-wrap gap-x-3 text-crumb font-medium text-muted-foreground">
                      {columns.flatMap((column) => {
                        const n = lane.cards.filter(
                          (card) => columnOf.get(card.id) === column.optionId,
                        ).length;
                        return n > 0
                          ? [
                              <span key={keyOf(column.optionId)}>
                                {props.columnName(column)}{" "}
                                <b className="text-foreground">{n}</b>
                              </span>,
                            ]
                          : [];
                      })}
                    </span>
                  ) : null}
                </div>
              </div>
              {isCollapsed ? null : props.filterActive &&
                lane.cards.length === 0 ? (
                <div
                  data-swimlane-empty=""
                  className="flex min-h-[70px] items-center justify-center p-2"
                >
                  <span className="w-full rounded-[9px] border-[1.5px] border-dashed border-line-strong px-3.5 py-3 text-center text-crumb leading-[1.45] text-meta-foreground">
                    {copy.states.emptyLane}
                  </span>
                </div>
              ) : (
                <div className="grid" style={{ gridTemplateColumns: template }}>
                  {columns.map((column) => {
                    const colKey = keyOf(column.optionId);
                    const cards = lane.cards.filter(
                      (card) => columnOf.get(card.id) === column.optionId,
                    );
                    const plan = overPlan(lane.optionId, column.optionId);
                    return (
                      <Cell
                        key={colKey}
                        lane={laneKey}
                        column={colKey}
                        state={
                          plan === null ? "idle" : plan.ok ? "drop" : "refuse"
                        }
                        hint={
                          plan === null
                            ? null
                            : plan.ok
                              ? hintFor(copy, plan, {
                                  laneField,
                                  fromColumn: columnLabel(
                                    columnOf.get(dragged?.id ?? "") ?? null,
                                  ),
                                  toColumn: columnLabel(column.optionId),
                                  fromLane: laneLabel(drag?.lane ?? null),
                                  toLane: name,
                                })
                              : refusalText(
                                  copy,
                                  plan.reason,
                                  laneField.name,
                                  columnLabel(column.optionId),
                                  props.wipLimits[colKey],
                                )
                        }
                        empty={copy.states.laneEmpty}
                      >
                        <SortableContext
                          items={cards.map((card) =>
                            dndIdOf(laneKey, colKey, card),
                          )}
                          strategy={verticalListSortingStrategy}
                        >
                          {cards.map((card) => (
                            <KanbanCard
                              key={card.id}
                              card={card}
                              dndId={dndIdOf(laneKey, colKey, card)}
                              laneKey={cellId(laneKey, colKey)}
                              now={props.now}
                              copy={props.cardCopy}
                              selected={card.id === props.selectedItemId}
                              dragDisabled={!props.canDrag}
                              onMoveKey={
                                props.canDrag
                                  ? (cardId, direction) =>
                                      moveByKey(
                                        cardId,
                                        direction,
                                        lane.optionId,
                                      )
                                  : undefined
                              }
                              actions={props.cardActions}
                              selection={props.cardSelection}
                              onOpenDetails={props.onOpenDetails}
                            />
                          ))}
                        </SortableContext>
                      </Cell>
                    );
                  })}
                </div>
              )}
            </section>
          );
        })}
      </div>
      <div role="status" aria-live="polite" className="sr-only">
        {live}
      </div>
      <DragOverlay zIndex={1500}>
        {dragged ? (
          <KanbanCardOverlay
            card={dragged}
            now={props.now}
            copy={props.cardCopy}
          />
        ) : null}
      </DragOverlay>
    </DndContext>
  );
}

function hintFor(
  copy: FiltersCopy,
  plan: Extract<LaneDropPlan, { ok: true }>,
  names: {
    laneField: FilterField;
    fromColumn: string;
    toColumn: string;
    fromLane: string;
    toLane: string;
  },
): CellHint {
  const lines: string[] = [];
  if (plan.columnChanged)
    lines.push(
      fillFiltersCopy(copy.lane.drop.column, {
        from: names.fromColumn,
        to: names.toColumn,
      }),
    );
  if (plan.laneChanged)
    lines.push(
      fillFiltersCopy(copy.lane.drop.lane, {
        field: names.laneField.name,
        from: names.fromLane,
        to: names.toLane,
      }),
    );
  return {
    title: plan.columnChanged && plan.laneChanged ? copy.lane.drop.both : null,
    lines,
  };
}

function refusalText(
  copy: FiltersCopy,
  reason: LaneDropRefusal,
  field: string,
  column: string,
  limit: number | undefined,
): CellHint {
  return {
    title: copy.lane.refused,
    lines: [
      fillFiltersCopy(copy.lane.refusal[reason], {
        field,
        column,
        limit: limit ?? "",
      }),
    ],
  };
}

/** What the cell under the dragged card says (WM-Swimlanes): a bold title, then one line per change or the reason. */
interface CellHint {
  readonly title: string | null;
  readonly lines: readonly string[];
}

function Cell({
  lane,
  column,
  state,
  hint,
  empty,
  children,
}: {
  lane: string;
  column: string;
  state: "idle" | "drop" | "refuse";
  hint: CellHint | null;
  empty: string;
  children: React.ReactNode;
}) {
  const { setNodeRef } = useDroppable({
    id: cellId(lane, column),
    data: { type: CELL_TYPE, lane, column },
  });
  return (
    <div
      ref={setNodeRef}
      data-cell={`${lane}::${column}`}
      data-state={state}
      className={cn(
        "relative flex min-h-16 min-w-0 flex-col gap-1.5 border-r border-line p-2 last:border-r-0",
        state === "drop" && "bg-state-selected ring-2 ring-inset ring-primary",
        state === "refuse" &&
          "bg-destructive/10 ring-2 ring-inset ring-destructive",
      )}
    >
      {hint ? (
        // Drawn over the cell's top, on the cards: left, right and top 8 px, 8 x 10 padding, 8 px radius.
        <div
          role={state === "refuse" ? "alert" : "status"}
          className={cn(
            "absolute inset-x-2 top-2 z-[2] rounded-[8px] px-2.5 py-2 text-crumb leading-[1.4] shadow-popover",
            state === "refuse"
              ? "bg-destructive text-white"
              : "bg-inverse-surface text-inverse-foreground",
          )}
        >
          {state === "refuse" ? (
            <Ban className="mr-1 inline size-4" aria-hidden="true" />
          ) : (
            <ArrowRight className="mr-1 inline size-4" aria-hidden="true" />
          )}
          {hint.title ? <b className="font-[650]">{hint.title}</b> : null}
          {hint.lines.map((line, index) => (
            <span key={line}>
              {index > 0 || hint.title ? <br /> : null}
              {line}
            </span>
          ))}
        </div>
      ) : null}
      {children}
      <span className="sr-only">{empty}</span>
    </div>
  );
}
