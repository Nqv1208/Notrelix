import { useMemo, useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import {
  buildFilterFields,
  filterLanesByCompound,
  findStatusField,
  groupableFields,
  groupCardsByField,
  groupCardsByStatus,
  serializeCompoundFilter,
  type BoardGroup,
  type Card,
  type DateBucket,
  type FieldDefinition,
  type FilterFieldInput,
  type KanbanStatusLane,
  type ViewState,
} from "@notrelix/work-management-core";
import { findBoardCard } from "../cache/card-restore";
import { wmQueryKeys } from "../queries/keys";
import { isVersionConflict } from "../queries/use-item-conflict";
import { useFullBoard } from "../queries/use-full-board";
import { useBoardReadNeed, useLanePager } from "../queries/board-read";
import { useBoardLabels } from "../queries/use-board-labels";
import { laneKeyOf } from "../cache/lane-pages";
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

/** The member's own view state and what the hook needs to name things (copy stays with the view). */
export interface UseBoardKanbanView {
  readonly state: ViewState;
  readonly names: FilterFieldInput["names"];
  /** The label of a date bucket key ("Tuần 12/10 – 18/10/2026"). */
  readonly bucketLabel: (key: string, bucket: DateBucket) => string;
  readonly timeZone?: string;
  /** False for the Kanban: its lanes are columns of a Status or Select field, never a person or date split. */
  readonly flatGroups?: boolean;
}

export interface UseBoardKanbanOptions {
  /** The compound filter, grouping by person or date and the sort levels; absent: only the legacy `filter`. */
  readonly view?: UseBoardKanbanView;
  /** Field id from the route `groupBy` parameter; unknown ids fall back to Status. */
  readonly groupBy?: string;
  readonly filter: KanbanLaneFilter;
  /**
   * The view lays cards out by something the server's Status lanes cannot serve (a calendar, a timeline): the
   * board is read whole, not in pages.
   */
  readonly allCards?: boolean;
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
  /**
   * Set when the write lost to someone else's change (a stale version): the
   * card as the board read now holds it, so the view can say who moved it and
   * where it is.
   */
  readonly conflict?: { readonly card: Card } | undefined;
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
  {
    groupBy,
    filter,
    isOverdue,
    onWriteError,
    view,
    allCards = false,
  }: UseBoardKanbanOptions,
) {
  const { board, groups, paging, isLoading, error, refetch } = useFullBoard(
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
  // The cards of a paged read hold only some of the board's labels: the filter offers the board's own list.
  const boardLabels = useBoardLabels(boardId, workspaceId, Boolean(paging));
  const pagedLabels = useMemo(
    () =>
      paging && boardLabels.data
        ? boardLabels.data.map((label) => ({ id: label.id, name: label.name }))
        : undefined,
    [paging, boardLabels.data],
  );
  const labelChoices = useMemo(
    () =>
      pagedLabels ?? (board ? kanbanFilterChoices(board, groups).labels : []),
    [board, groups, pagedLabels],
  );
  const filterFields = useMemo(
    () =>
      board && view
        ? buildFilterFields({
            fieldDefinitions: board.fieldDefinitions,
            members: board.members,
            labels: labelChoices,
            names: view.names,
            isOverdue,
          })
        : [],
    [board, view, labelChoices, isOverdue],
  );
  const memberName = useMemo(() => {
    const names = new Map<string, string>();
    for (const member of board?.members ?? [])
      names.set(member.userId, member.name);
    for (const group of groups)
      for (const card of group.cards)
        for (const member of card.members)
          if (!names.has(member.userId)) names.set(member.userId, member.name);
    return (userId: string) => names.get(userId) ?? userId;
  }, [board, groups]);
  /** The field a view can group by besides the Status and Select fields: a person or a date field. */
  const flatGroupField = useMemo(
    () =>
      groupBy &&
      view?.flatGroups !== false &&
      !groupByFields.some((field) => field.id === groupBy)
        ? groupableFields(filterFields).find((field) => field.id === groupBy)
        : undefined,
    [filterFields, groupBy, groupByFields, view?.flatGroups],
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
    if (flatGroupField && view) {
      const topLevel = sortedGroups.flatMap((group) =>
        group.cards.filter(
          (card) => !card.parentItemId && !card.isArchived && !card.isDeleted,
        ),
      );
      return groupCardsByField(
        topLevel,
        flatGroupField,
        view.state.group?.fieldId === flatGroupField.id
          ? view.state.group.bucket
          : undefined,
        {
          person: memberName,
          bucket: view.bucketLabel,
          ...(view.timeZone ? { timeZone: view.timeZone } : {}),
        },
      );
    }
    if (!groupByField || groupByField.id === statusField?.id) {
      return groupCardsByStatus({
        fieldDefinitions: board.fieldDefinitions,
        groups: sortedGroups,
        paging,
      });
    }
    return groupCardsBySelectField(groupByField, sortedGroups);
  }, [
    board,
    groups,
    groupByField,
    statusField,
    flatGroupField,
    view,
    memberName,
    paging,
  ]);

  const filterChoices = useMemo(() => {
    if (!board) return { statuses: [], assignees: [], labels: [] };
    const choices = kanbanFilterChoices(board, groups);
    return pagedLabels ? { ...choices, labels: pagedLabels } : choices;
  }, [board, groups, pagedLabels]);
  // Ids the board no longer has never narrow the board (a stale URL).
  const appliedFilter = useMemo(
    () => (board ? dropUnknownFilterIds(filter, filterChoices) : filter),
    [board, filter, filterChoices],
  );

  // Pages of Status lanes serve a view that lays the board out by Status and filters by what the server can
  // evaluate; anything else (another grouping, a swimlane, a search, a facet filter) reads the whole board.
  const compoundFilter = view?.state.filter;
  const needsWholeBoard =
    allCards ||
    Boolean(flatGroupField) ||
    (groupByField !== undefined && groupByField.id !== statusField?.id) ||
    Boolean(view?.state.lane) ||
    debouncedQuery.trim() !== "" ||
    filter.statusOptionIds.length > 0 ||
    filter.priorities.length > 0 ||
    filter.assigneeIds.length > 0 ||
    filter.labelIds.length > 0 ||
    filter.overdue;
  useBoardReadNeed(boardId, workspaceId, {
    full: needsWholeBoard,
    filter:
      !needsWholeBoard && compoundFilter
        ? serializeCompoundFilter(compoundFilter)
        : "",
    sort: "",
  });
  const lanePager = useLanePager(boardId, workspaceId);

  const legacyFiltered = useKanbanFilters(allLanes, appliedFilter, {
    query: debouncedQuery,
    isOverdue,
  });
  const compound = view?.state.filter;
  const filtered = useMemo(() => {
    if (!compound || compound.conditions.length === 0) return legacyFiltered;
    const lanes = filterLanesByCompound(
      legacyFiltered.lanes,
      compound,
      filterFields,
      memberName,
    );
    return {
      ...legacyFiltered,
      lanes,
      visibleCount: lanes.reduce((sum, lane) => sum + lane.cards.length, 0),
      active: true,
    };
  }, [compound, filterFields, legacyFiltered, memberName]);

  const queryClient = useQueryClient();
  // A refused stale write has already refetched the board (the write's own
  // cleanup), so the card read from the cache is the other person's version.
  const conflictOf = (error: unknown, cardId: string) => {
    if (!isVersionConflict(error) || !workspaceId) return undefined;
    const card = findBoardCard(
      queryClient.getQueryData(wmQueryKeys.fullBoard(workspaceId, boardId)),
      cardId,
    );
    return card ? { card } : undefined;
  };
  // Who last changed a card, read from the cache after a refetch (the other person once a write lost).
  const lastEditorOf = (cardId: string) =>
    workspaceId
      ? findBoardCard(
          queryClient.getQueryData(wmQueryKeys.fullBoard(workspaceId, boardId)),
          cardId,
        )?.updatedByName ?? undefined
      : undefined;
  const updateStatus = useUpdateItemStatus(boardId, workspaceId, {
    onError: (error, input) =>
      onWriteError?.({
        kind: "status",
        cardId: input.cardId,
        error,
        conflict: conflictOf(error, input.cardId),
      }),
  });
  const updateFieldValue = useUpdateFieldValue(boardId, workspaceId, {
    onError: (error, input) =>
      onWriteError?.({
        kind: "field",
        cardId: input.cardId,
        error,
        conflict: conflictOf(error, input.cardId),
      }),
  });
  const moveCard = useMoveCard(boardId, workspaceId, {
    onError: (error, input) =>
      onWriteError?.({
        kind: "move",
        cardId: input.cardId,
        error,
        conflict: conflictOf(error, input.cardId),
      }),
  });
  const dropItemInLane = useDropItemInLane(boardId, workspaceId, {
    onError: (error, input) =>
      onWriteError?.({
        kind: "place",
        cardId: input.cardId,
        error,
        conflict: conflictOf(error, input.cardId),
      }),
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
    lastEditorOf,
    statusField,
    groupByField,
    /** The person or date field the view is grouped by, when it is one (lanes are then built from card values). */
    flatGroupField,
    groupByFields,
    /** The fields a filter, sort, group or swimlane can name (empty without `view`). */
    filterFields,
    memberName,
    /** Lanes before the filter (lane totals, K/J order without a filter). */
    allLanes,
    /** Lanes with the filter and search applied; `total` is the lane total. */
    lanes: filtered.lanes,
    visibleCount: filtered.visibleCount,
    totalCount: filtered.totalCount,
    filterActive: filtered.active,
    /** The status options, people and labels the filter menu offers. */
    filterChoices,
    /** A paged read: the sizes of the board and of each lane; absent when every item is loaded. */
    paging,
    /** Items of the board, and those that pass the server filter (what lanes hold plus what they could still load). */
    boardTotals: {
      total: allLanes.reduce((sum, lane) => sum + lane.total, 0),
      filtered: allLanes.reduce(
        (sum, lane) => sum + (lane.filteredTotal ?? lane.cards.length),
        0,
      ),
    },
    /** Reads the next page of a Status lane (`optionId` null: the lane without a status). */
    loadMoreLane: (optionId: string | null) =>
      lanePager.loadMore(laneKeyOf({ optionId })),
    /** Lanes (by option id, `none` for no status) whose next page is being read, or failed to read. */
    lanePages: { loading: lanePager.loading, failed: lanePager.failed },
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
