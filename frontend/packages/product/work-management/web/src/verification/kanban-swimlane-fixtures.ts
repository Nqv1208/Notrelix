/**
 * Deterministic data for the swimlane Kanban: the ERP board's four status
 * columns split into lanes by a second field (the person by default, the
 * priority, or a read-only field). The clock is injected (12/10/2026 10:40).
 */
import {
  buildFilterFields,
  groupCardsByField,
  type FilterField,
  type KanbanStatusLane,
} from "@notrelix/work-management-core";
import { erpBoardScenario } from "@notrelix/work-management-testing";
import { kanbanCardCopy } from "../components/views/kanban/kanban-card.copy";
import type { KanbanSwimlaneBoardProps } from "../components/views/kanban/kanban-swimlane-board";
import { bucketLabel } from "../components/filters/condition-label";
import { filtersCopy } from "../components/filters/filters.copy";
import { erpKanbanLanes, KANBAN_STORY_NOW } from "./kanban-board-fixtures";
import { VIEW_FIELD_DEFINITIONS } from "./view-state-fixtures";

export type SwimlaneLaneKind = "person" | "priority" | "overdue";

export interface SwimlaneScenarioOptions {
  locale?: "vi" | "en";
  lane?: SwimlaneLaneKind;
  canDrag?: boolean;
  wipLimits?: Readonly<Record<string, number>>;
  /** Fewer cards per column, to keep the story small (edge and empty). */
  columns?: readonly KanbanStatusLane[];
}

export type SwimlaneScenario = Omit<
  KanbanSwimlaneBoardProps,
  "onOpenDetails" | "onDrop" | "onRefused"
>;

export function erpSwimlaneScenario(
  options: SwimlaneScenarioOptions = {},
): SwimlaneScenario {
  const locale = options.locale ?? "vi";
  const copy = filtersCopy(locale);
  const { board } = erpBoardScenario();
  const memberName = (id: string) =>
    board.members.find((m) => m.userId === id)?.name ?? id;
  const fields: FilterField[] = buildFilterFields({
    // The ERP world has no person field of its own: add the one a real board carries.
    fieldDefinitions: [
      ...board.fieldDefinitions,
      ...VIEW_FIELD_DEFINITIONS.filter((f) => f.fieldType === "person"),
    ],
    members: board.members,
    labels: [],
    names: {
      title: copy.field.title,
      label: copy.field.label,
      overdue: copy.filter.presets.overdue,
    },
    // Half the cards count as overdue, so the read-only "Quá hạn" lane field has two lanes.
    isOverdue: (card) => card.id.charCodeAt(card.id.length - 1) % 2 === 0,
  });
  const kind = options.lane ?? "person";
  const laneField =
    kind === "person"
      ? fields.find((f) => f.field.fieldType === "person")
      : kind === "priority"
        ? fields.find((f) => f.field.semanticRole === "priority")
        : fields.find((f) => f.id === "$overdue");
  if (!laneField) throw new Error(`ERP board has no ${kind} field`);
  const columns = [...(options.columns ?? erpKanbanLanes())];
  const swimlanes = groupCardsByField(
    columns.flatMap((column) => column.cards),
    laneField,
    undefined,
    {
      person: memberName,
      bucket: (key, bucket) => bucketLabel(copy, key, bucket),
    },
  );
  return {
    copy,
    cardCopy: kanbanCardCopy(locale),
    now: KANBAN_STORY_NOW,
    columns,
    swimlanes,
    laneField,
    columnName: (column) => column.name || copy.lane.none_,
    laneName: (lane) =>
      lane.name ||
      (laneField.field.fieldType === "person"
        ? copy.lane.unassigned
        : copy.lane.none_),
    wipLimits: options.wipLimits ?? {},
    selectedItemId: undefined,
    canDrag: options.canDrag ?? true,
    cardActions: undefined,
  };
}
