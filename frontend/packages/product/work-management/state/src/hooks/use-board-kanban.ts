import { useMemo, useState } from "react";
import {
  findStatusField,
  groupCardsByStatus,
  type BoardGroup,
  type Card,
  type FieldDefinition,
  type KanbanStatusLane,
} from "@notrelix/work-management-core";
import { useFullBoard } from "../queries/use-full-board";
import { useDebounce } from "./use-debounce";
import {
  dropUnknownFilterIds,
  kanbanFilterChoices,
  useKanbanFilters,
  type KanbanLaneFilter,
} from "./use-kanban-filters";
import { useDropItemInLane } from "../mutations/use-drop-item-in-lane";
import { useMoveCard } from "../mutations/use-move-card";
import { useUpdateFieldValue } from "../mutations/use-update-field-value";
import {
  useCreateItemInLane,
  useUpdateItemStatus,
} from "../mutations/use-update-item-status";

/** A field the Kanban can be grouped by ("Nhóm theo"). */
export interface KanbanGroupByField {
  readonly id: string;
  readonly name: string;
  readonly kind: "status" | "select";
}

export interface UseBoardKanbanOptions {
  /** Field id from the route `groupBy` parameter; unknown ids fall back to Status. */
  readonly groupBy?: string;
  readonly filter: KanbanLaneFilter;
  /** Whether a card is overdue (same calendar rule as the card's due wording). */
  readonly isOverdue: (card: Card) => boolean;
  /**
   * Called for every failed write, also when a later write of the same kind
   * has started or the view has unmounted (hook-level, not per call).
   */
  readonly onWriteError?: (failure: KanbanWriteFailure) => void;
}

/** A failed Kanban write; the card is already back where it was. */
export interface KanbanWriteFailure {
  readonly kind: "status" | "field" | "move" | "create" | "place";
  /** The card, or `null` for an item that was being created. */
  readonly cardId: string | null;
  /** The title of the item being created. */
  readonly title?: string;
  readonly error: unknown;
}

function optionValue(value: unknown): string | null {
  if (typeof value === "string" && value) return value;
  if (Array.isArray(value) && typeof value[0] === "string") return value[0];
  return null;
}

/**
 * Lanes for a Select field: one lane per option in option order, then a lane
 * for cards without a value when there are any. Same lane model and card rules
 * as the Status lanes (child, archived and deleted items are left out).
 */
export function groupCardsBySelectField(
  field: FieldDefinition,
  groups: readonly BoardGroup[],
): KanbanStatusLane[] {
  const byOption = new Map<string, Card[]>(
    field.options.map((option) => [option.id, []]),
  );
  const withoutValue: Card[] = [];
  for (const group of groups) {
    for (const card of group.cards) {
      if (card.parentItemId || card.isArchived || card.isDeleted) continue;
      const optionId = optionValue(card.fieldValues[field.id]);
      const lane = optionId ? byOption.get(optionId) : undefined;
      if (lane) lane.push(card);
      else withoutValue.push(card);
    }
  }
  const lanes: KanbanStatusLane[] = field.options.map((option) => {
    const cards = byOption.get(option.id) ?? [];
    return {
      optionId: option.id,
      name: option.label,
      category: null,
      color: option.color || null,
      cards,
      total: cards.length,
    };
  });
  if (withoutValue.length > 0) {
    lanes.push({
      optionId: null,
      name: "",
      category: null,
      color: null,
      cards: withoutValue,
      total: withoutValue.length,
    });
  }
  return lanes;
}

/** The Status field first, then the other Select fields. */
export function kanbanGroupByFields(
  fields: readonly FieldDefinition[],
): KanbanGroupByField[] {
  const status = findStatusField(fields);
  const selects = fields.filter(
    (field) =>
      field.id !== status?.id &&
      field.fieldType === "select" &&
      field.options.length > 0,
  );
  return [
    ...(status
      ? [{ id: status.id, name: status.name, kind: "status" as const }]
      : []),
    ...selects.map((field) => ({
      id: field.id,
      name: field.name,
      kind: "select" as const,
    })),
  ];
}

/**
 * The Kanban of a board: lanes by the grouping field (Status unless the
 * route names another Select field), the view filter and search applied, and
 * the writes a lane needs (status or field value, reorder, create in lane).
 */
export function useBoardKanban(
  boardId: string,
  workspaceId: string,
  { groupBy, filter, isOverdue, onWriteError }: UseBoardKanbanOptions,
) {
  const { board, groups, isLoading, error, refetch } = useFullBoard(
    boardId,
    workspaceId,
  );
  const [query, setQuery] = useState("");
  const debouncedQuery = useDebounce(query, 250);

  const fields = board?.fieldDefinitions;
  const statusField = useMemo(
    () => (fields ? findStatusField(fields) : undefined),
    [fields],
  );
  const groupByFields = useMemo(
    () => (fields ? kanbanGroupByFields(fields) : []),
    [fields],
  );
  const groupByField = useMemo(() => {
    const named = groupBy
      ? groupByFields.find((field) => field.id === groupBy)
      : undefined;
    const chosen = named ?? groupByFields[0];
    return chosen ? fields?.find((field) => field.id === chosen.id) : undefined;
  }, [fields, groupBy, groupByFields]);

  const allLanes = useMemo(() => {
    if (!board) return [];
    const sortedGroups = [...groups].sort((a, b) => a.position - b.position);
    if (!groupByField || groupByField.id === statusField?.id) {
      return groupCardsByStatus({
        fieldDefinitions: board.fieldDefinitions,
        groups: sortedGroups,
      });
    }
    return groupCardsBySelectField(groupByField, sortedGroups);
  }, [board, groups, groupByField, statusField]);

  const filterChoices = useMemo(
    () =>
      board
        ? kanbanFilterChoices(board, groups)
        : { statuses: [], assignees: [], labels: [] },
    [board, groups],
  );
  // Ids the board no longer has never narrow the board (a stale URL).
  const appliedFilter = useMemo(
    () => (board ? dropUnknownFilterIds(filter, filterChoices) : filter),
    [board, filter, filterChoices],
  );

  const filtered = useKanbanFilters(allLanes, appliedFilter, {
    query: debouncedQuery,
    isOverdue,
  });

  const updateStatus = useUpdateItemStatus(boardId, workspaceId, {
    onError: (error, input) =>
      onWriteError?.({ kind: "status", cardId: input.cardId, error }),
  });
  const updateFieldValue = useUpdateFieldValue(boardId, workspaceId, {
    onError: (error, input) =>
      onWriteError?.({ kind: "field", cardId: input.cardId, error }),
  });
  const moveCard = useMoveCard(boardId, workspaceId, {
    onError: (error, input) =>
      onWriteError?.({ kind: "move", cardId: input.cardId, error }),
  });
  const dropItemInLane = useDropItemInLane(boardId, workspaceId, {
    onError: (error, input) =>
      onWriteError?.({ kind: "place", cardId: input.cardId, error }),
  });
  const createItemInLane = useCreateItemInLane(boardId, workspaceId, {
    onError: (error, input) =>
      onWriteError?.({
        kind: "create",
        cardId: null,
        title: input.title,
        error,
      }),
  });

  return {
    board,
    groups,
    /** Reads the board again (the "Tải lại" of a conflict). */
    refetch,
    statusField,
    groupByField,
    groupByFields,
    /** Lanes before the filter (lane totals, K/J order without a filter). */
    allLanes,
    /** Lanes with the filter and search applied; `total` is the lane total. */
    lanes: filtered.lanes,
    visibleCount: filtered.visibleCount,
    totalCount: filtered.totalCount,
    filterActive: filtered.active,
    /** The status options, people and labels the filter menu offers. */
    filterChoices,
    isLoading,
    error,
    search: { query, setQuery, debouncedQuery },
    updateStatus,
    updateFieldValue,
    moveCard,
    dropItemInLane,
    createItemInLane,
  };
}
