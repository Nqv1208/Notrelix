import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import {
  dropUnknownFilterIds,
  useBoardPreference,
  useBoardView,
  type KanbanFilterChoices,
} from "@notrelix/work-management-state";
import {
  effectiveFilter,
  emptyCompoundFilter,
  emptyViewState,
  isViewStateEmpty,
  legacyFacetsToCompound,
  parseCompoundFilter,
  serializeCompoundFilter,
  viewStateFromCriteria,
  viewStateToCriteria,
  type CompoundFilter,
  type FieldDefinition,
  type FieldRef,
  type SavedFilterCriteria,
  type SortLevel,
  type ViewState,
} from "@notrelix/work-management-core";
import { parseKanbanFilter } from "../views/kanban/kanban-filter";
import type { ViewStateControl } from "./view-state-control";

type ParamChange = (
  value: string | undefined,
  options?: { replace?: boolean },
) => void;

export interface UseViewStateOptions {
  boardId: string;
  workspaceId: string;
  /** The route `filter` parameter: a compound (`c2:`) string or an older facet string. */
  routeFilter: string | undefined;
  onRouteFilterChange: ParamChange;
  /** The route `groupBy` parameter (a field id) and its writer; absent for a view without one. */
  routeGroupBy?: string | undefined;
  onRouteGroupByChange?: ((fieldId: string) => void) | undefined;
  /** The board's field definitions; the older facet filter is converted once they are known. */
  fieldDefinitions: readonly FieldDefinition[] | undefined;
  /** What the board has (status options, people, labels): ids an older facet filter names beyond it are dropped. */
  filterChoices?: KanbanFilterChoices | undefined;
  /** A save made on a view the member already left failed: the host tells them. */
  onSaveLost?: (() => void) | undefined;
}

const SAVE_DELAY_MS = 400;

/**
 * The member's own state of the open view: the compound filter, up to three
 * sort levels, the grouping and the swimlane field.
 *
 * Sources: the route `filter` and `groupBy` parameters (so a link or reload
 * keeps them), else what the member last saved for this view (BoardPreference
 * criteria, any viewer may). A change updates the screen at once, rewrites the
 * route parameters and saves the criteria after a short pause; a failed save
 * leaves the screen as it is and offers "Thử lại". "Về view chung" clears all
 * of it, including the saved criteria. The shared view is never written.
 */
export function useViewState(options: UseViewStateOptions): ViewStateControl {
  const {
    boardId,
    workspaceId,
    routeFilter,
    onRouteFilterChange,
    routeGroupBy,
    onRouteGroupByChange,
    fieldDefinitions,
    filterChoices,
    onSaveLost,
  } = options;
  const { viewId } = useBoardView(boardId, workspaceId);
  const preference = useBoardPreference(
    boardId,
    workspaceId,
    viewId,
    onSaveLost,
  );
  const personal = useMemo(
    () =>
      preference.criteria ? viewStateFromCriteria(preference.criteria) : null,
    [preference.criteria],
  );
  const loaded = !preference.isLoading;

  // The route filter as a compound filter (an older facet string is converted).
  const routeCompound = useMemo<CompoundFilter | null>(() => {
    if (routeFilter === undefined || routeFilter === "") return null;
    const compound = parseCompoundFilter(routeFilter);
    if (compound) return compound;
    if (!fieldDefinitions) return null;
    const legacy = parseKanbanFilter(routeFilter);
    return legacyFacetsToCompound(
      filterChoices ? dropUnknownFilterIds(legacy, filterChoices) : legacy,
      fieldDefinitions,
    );
  }, [routeFilter, fieldDefinitions, filterChoices]);

  // The member's edits on screen. They belong to one workspace + board + view:
  // another view never sees them. `wrote` is the route filter the edit wrote,
  // `before` the one it replaced; `synced` once the route showed `wrote`. A
  // route value that is neither (browser Back or Forward) wins over the edit.
  const scope = `${workspaceId}:${boardId}:${viewId ?? ""}`;
  const [localState, setLocal] = useState<{
    scope: string;
    state: ViewState;
    wrote: string;
    before: string;
    synced: boolean;
  } | null>(null);
  const local = localState?.scope === scope ? localState : null;

  useEffect(() => {
    if (!localState) return;
    if (localState.scope !== scope) {
      setLocal(null);
      return;
    }
    const route = routeFilter ?? "";
    if (route === localState.wrote) {
      if (!localState.synced) setLocal({ ...localState, synced: true });
      return;
    }
    if (!localState.synced && route === localState.before) return;
    setLocal({
      ...localState,
      state: {
        ...localState.state,
        filter: routeCompound ?? emptyCompoundFilter(),
      },
      wrote: route,
      before: route,
      synced: true,
    });
  }, [localState, scope, routeFilter, routeCompound]);

  const state = useMemo<ViewState>(() => {
    if (local) return local.state;
    const base = personal ?? emptyViewState();
    const group = routeGroupBy
      ? {
          fieldId: routeGroupBy,
          ...(base.group?.fieldId === routeGroupBy && base.group.bucket
            ? { bucket: base.group.bucket }
            : {}),
        }
      : base.group;
    return {
      filter:
        routeCompound ?? (routeFilter ? emptyCompoundFilter() : base.filter),
      sorts: base.sorts,
      ...(group ? { group } : {}),
      ...(base.lane ? { lane: base.lane } : {}),
    };
  }, [local, personal, routeCompound, routeFilter, routeGroupBy]);

  // An older facet string in the route is rewritten once to its compound form.
  useEffect(() => {
    if (!routeFilter || parseCompoundFilter(routeFilter) || !routeCompound)
      return;
    onRouteFilterChange(serializeCompoundFilter(routeCompound) || undefined, {
      replace: true,
    });
  }, [routeFilter, routeCompound, onRouteFilterChange]);

  const timer = useRef<number | null>(null);
  // The pending save keeps the view it was made on, so a switch cannot redirect it.
  const pending = useRef<{
    state: ViewState;
    target: { workspaceId: string; boardId: string; viewId: string };
  } | null>(null);
  const { save } = preference;
  const saveRef = useRef(save);
  saveRef.current = save;
  const flush = useCallback(() => {
    if (timer.current !== null) window.clearTimeout(timer.current);
    timer.current = null;
    const next = pending.current;
    pending.current = null;
    if (next)
      saveRef.current(
        viewStateToCriteria(next.state, { withSort: true }),
        next.target,
      );
  }, []);
  // Leaving a view (or the page) saves what is pending to that view first.
  useEffect(() => flush, [flush, scope]);

  const commit = useCallback(
    (next: ViewState) => {
      const wrote = serializeCompoundFilter(next.filter);
      setLocal({
        scope,
        state: next,
        wrote,
        before: routeFilter ?? "",
        synced: false,
      });
      onRouteFilterChange(wrote || undefined);
      if (onRouteGroupByChange && next.group?.fieldId !== state.group?.fieldId)
        onRouteGroupByChange(next.group?.fieldId ?? "");
      if (viewId)
        pending.current = {
          state: next,
          target: { workspaceId, boardId, viewId },
        };
      if (timer.current !== null) window.clearTimeout(timer.current);
      timer.current = window.setTimeout(flush, SAVE_DELAY_MS);
    },
    [
      boardId,
      flush,
      onRouteFilterChange,
      onRouteGroupByChange,
      routeFilter,
      scope,
      state.group?.fieldId,
      viewId,
      workspaceId,
    ],
  );

  const setFilter = useCallback(
    (filter: CompoundFilter) => commit({ ...state, filter }),
    [commit, state],
  );
  const setSorts = useCallback(
    (sorts: SortLevel[]) => commit({ ...state, sorts }),
    [commit, state],
  );
  const setGroup = useCallback(
    (group: FieldRef | undefined) => {
      const { group: _old, ...rest } = state;
      commit(group ? { ...rest, group } : rest);
    },
    [commit, state],
  );
  const setLane = useCallback(
    (lane: FieldRef | undefined) => {
      const { lane: _old, ...rest } = state;
      commit(lane ? { ...rest, lane } : rest);
    },
    [commit, state],
  );
  /** Puts a saved filter on the bar: its filter, grouping and swimlane; the member's own sort stays. */
  const applyCriteria = useCallback(
    (criteria: SavedFilterCriteria) => {
      const next = viewStateFromCriteria(criteria);
      const legacy =
        criteria.filter && !parseCompoundFilter(criteria.filter)
          ? fieldDefinitions
            ? legacyFacetsToCompound(
                parseKanbanFilter(criteria.filter),
                fieldDefinitions,
              )
            : emptyCompoundFilter()
          : next.filter;
      commit({
        filter: legacy,
        sorts: state.sorts,
        ...(next.group ? { group: next.group } : {}),
        ...(next.lane ? { lane: next.lane } : {}),
      });
    },
    [commit, fieldDefinitions, state.sorts],
  );
  const reset = useCallback(() => {
    commit(emptyViewState());
    flush();
  }, [commit, flush]);

  const applied = useMemo<ViewState>(
    () => ({ ...state, filter: effectiveFilter(state.filter) }),
    [state],
  );

  return {
    /** The state with every condition, also incomplete rows (what the builder edits). */
    state,
    /** The state that applies: incomplete or invalid conditions left out. */
    applied,
    /** True once the saved criteria were read (or there is no saved view to read them from). */
    loaded,
    /** The member has a view of their own: any filter, sort, grouping or swimlane. */
    personal: !isViewStateEmpty(state),
    setFilter,
    setSorts,
    setGroup,
    setLane,
    applyCriteria,
    reset,
    persistent: preference.persistent,
    saveFailed: preference.saveFailed,
    saveIssue: preference.saveIssue,
    retrySave: preference.retry,
  };
}

export type { ViewStateControl };
