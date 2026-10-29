import { useState, type ReactNode } from "react";
import { AlertCircle, Filter, RefreshCw, User, X } from "lucide-react";
import { Button, cn } from "@notrelix/ui-web";
import {
  conditionIssue,
  type FilterField,
  type MemberNames,
} from "@notrelix/work-management-core";
import { kanbanOutlineButtonClass } from "../views/kanban/kanban-chrome";
import { describeCondition, staleConditionCount } from "./condition-label";
import { FilterBuilder } from "./filter-builder";
import { FieldRefMenu } from "./field-ref-menu";
import { fillFiltersCopy, type FiltersCopy } from "./filters.copy";
import { SortMenu } from "./sort-menu";
import type { ViewStateControl } from "./use-view-state";

const MAX_CHIPS = 3;

export interface ViewStateBarProps {
  copy: FiltersCopy;
  view: ViewStateControl;
  fields: readonly FilterField[];
  memberName: MemberNames;
  people: readonly { id: string; name: string }[];
  /** Items the filter keeps and the items it was applied to. */
  counts: { shown: number; total: number };
  now: Date;
  /** Which of the sort, group and swimlane controls this view has. */
  sort?: boolean;
  groupFields?: readonly FilterField[];
  laneFields?: readonly FilterField[];
  /** The field that is the column of a Kanban; it cannot also be the lane. */
  blockedLaneFieldId?: string | undefined;
  /** The saved-filter controls, shown after the personal-state chip. */
  savedFilters?: ReactNode;
  /** Opens the host's "Lưu bộ lọc" form for the bar's filter (offered next to the removed-field warning). */
  onSaveAsNew?: (() => void) | undefined;
  /** The filter dialog is controlled so "Sửa điều kiện" can open it. */
  filterOpen?: boolean;
  onFilterOpenChange?: (open: boolean) => void;
}

/**
 * The view controls of a filter bar: "Lọc" with its condition chips, "Sắp
 * xếp", "Nhóm", "Làn" and the "Cách xem riêng · Về view chung" chip. It
 * renders into the host toolbar's flex row. Viewers get all of it: it is their
 * own view, never the shared one.
 */
export function ViewStateBar(props: ViewStateBarProps) {
  const { copy, view, fields, memberName } = props;
  const filter = view.state.filter;
  const known = fields.length > 0;
  const applied = filter.conditions.filter(
    (condition) => conditionIssue(condition) === null,
  );
  const shown = filter.conditions.slice(0, MAX_CHIPS);
  const hidden = filter.conditions.length - shown.length;
  const [openInner, setOpenInner] = useState(false);
  const open = props.filterOpen ?? openInner;
  const setOpen = props.onFilterOpenChange ?? setOpenInner;
  return (
    <>
      <FilterBuilder
        copy={copy}
        fields={fields}
        people={props.people}
        filter={filter}
        onChange={view.setFilter}
        counts={props.counts}
        now={props.now}
        activeCount={applied.length}
        open={open}
        onOpenChange={setOpen}
      />
      {shown.length > 0 ? (
        <ul
          aria-label={copy.filter.trigger}
          className="m-0 flex min-w-0 list-none flex-wrap items-center gap-1.5 p-0"
        >
          {shown.map((condition, index) => {
            const field = fields.find((f) => f.id === condition.fieldId);
            const stale = known && !field;
            const label = describeCondition(
              copy,
              condition,
              field,
              memberName,
            );
            const original = filter.conditions.indexOf(condition);
            return (
              <li key={`${condition.fieldId}-${index}`} className="contents">
                {index > 0 ? (
                  <span className="text-xs font-semibold text-muted-foreground">
                    {filter.join === "and" ? copy.filter.and : copy.filter.or}
                  </span>
                ) : null}
                <span
                  data-stale={stale || undefined}
                  className={cn(
                    "inline-flex h-6.5 max-w-full items-center gap-1 rounded-full pl-2.5 pr-1 text-compact font-semibold",
                    stale
                      ? "bg-warning-subtle text-warning-subtle-foreground ring-1 ring-warning/50"
                      : "bg-state-selected text-state-selected-foreground",
                  )}
                >
                  {stale ? (
                    <AlertCircle className="size-3.5" aria-hidden="true" />
                  ) : null}
                  <span className="min-w-0 truncate">{label}</span>
                  <button
                    type="button"
                    aria-label={fillFiltersCopy(copy.filter.clearChip, {
                      label,
                    })}
                    onClick={() =>
                      view.setFilter({
                        ...filter,
                        conditions: filter.conditions.filter(
                          (_, i) => i !== original,
                        ),
                      })
                    }
                    className="grid size-5 cursor-pointer place-items-center rounded-full border-0 bg-transparent text-inherit hover:bg-state-hover-strong focus-visible:outline-2 focus-visible:outline-ring"
                  >
                    <X className="size-3.5" aria-hidden="true" />
                  </button>
                </span>
              </li>
            );
          })}
          {hidden > 0 ? (
            <li>
              <button
                type="button"
                onClick={() => setOpen(true)}
                className="h-6.5 cursor-pointer rounded-full border-0 bg-state-hover px-2.5 text-compact font-medium text-muted-foreground hover:bg-state-hover-strong focus-visible:outline-2 focus-visible:outline-ring"
              >
                {fillFiltersCopy(copy.filter.more, { count: hidden })}
              </button>
            </li>
          ) : null}
        </ul>
      ) : null}
      {props.sort ? (
        <SortMenu
          copy={copy}
          fields={fields}
          levels={view.state.sorts}
          onChange={view.setSorts}
        />
      ) : null}
      {props.groupFields ? (
        <FieldRefMenu
          copy={copy}
          kind="group"
          fields={props.groupFields}
          value={view.state.group}
          onChange={view.setGroup}
        />
      ) : null}
      {props.laneFields ? (
        <FieldRefMenu
          copy={copy}
          kind="lane"
          fields={props.laneFields}
          value={view.state.lane}
          blockedFieldId={props.blockedLaneFieldId}
          onChange={view.setLane}
        />
      ) : null}
      {view.personal ? (
        <span
          role="group"
          aria-label={copy.personal.label}
          className="inline-flex h-8 items-center gap-1.5 rounded-lg border border-dashed border-line-strong pl-2.5 pr-0.75 text-compact text-muted-foreground"
        >
          <User className="size-4" aria-hidden="true" />
          {copy.personal.label}
          <button
            type="button"
            onClick={view.reset}
            className="inline-flex h-6.5 cursor-pointer items-center gap-1 rounded-md border-0 bg-transparent px-2 text-compact font-semibold text-state-selected-foreground hover:bg-state-hover-strong focus-visible:outline-2 focus-visible:outline-ring"
          >
            <RefreshCw className="size-3.5" aria-hidden="true" />
            {copy.personal.reset}
          </button>
        </span>
      ) : null}
      {props.savedFilters}
      <div className="basis-full empty:hidden">
        <ViewStateNotices
          copy={copy}
          view={view}
          fields={fields}
          onSaveAsNew={props.onSaveAsNew}
        />
      </div>
    </>
  );
}

export interface ViewStateNoticesProps {
  copy: FiltersCopy;
  view: ViewStateControl;
  fields: readonly FilterField[];
  /** Saves the bar's filter as a new saved filter (the host opens its save form). */
  onSaveAsNew?: (() => void) | undefined;
}

/** Under the bar: the removed-field warning and the "could not save your view" error with Thử lại. */
export function ViewStateNotices({
  copy,
  view,
  fields,
  onSaveAsNew,
}: ViewStateNoticesProps) {
  const stale =
    fields.length > 0 ? staleConditionCount(view.state.filter, fields) : 0;
  if (stale === 0 && !view.saveFailed) return null;
  const known = new Set(fields.map((f) => f.id));
  return (
    <div className="flex flex-col gap-2 pb-2">
      {stale > 0 ? (
        <div
          role="status"
          className="flex flex-wrap items-center gap-2 rounded-[9px] bg-warning-subtle px-3 py-2 text-compact text-warning-subtle-foreground ring-1 ring-warning/50"
        >
          <AlertCircle className="size-4 shrink-0" aria-hidden="true" />
          <span className="min-w-0 flex-1">
            {fillFiltersCopy(copy.stale.hint, { count: stale })}
          </span>
          <Button
            variant="outline"
            size="sm"
            className={kanbanOutlineButtonClass}
            onClick={() =>
              view.setFilter({
                ...view.state.filter,
                conditions: view.state.filter.conditions.filter((c) =>
                  known.has(c.fieldId),
                ),
              })
            }
          >
            {copy.stale.removeIgnored}
          </Button>
          {onSaveAsNew ? (
            <Button
              variant="outline"
              size="sm"
              className={kanbanOutlineButtonClass}
              onClick={onSaveAsNew}
            >
              {copy.stale.saveNew}
            </Button>
          ) : null}
        </div>
      ) : null}
      {view.saveFailed ? (
        <div
          role="alert"
          className="flex flex-wrap items-center gap-2 rounded-[9px] bg-destructive/10 px-3 py-2 text-compact text-destructive"
        >
          <AlertCircle className="size-4 shrink-0" aria-hidden="true" />
          <span className="min-w-0 flex-1">{copy.personal.saveFailed}</span>
          <Button
            variant="outline"
            size="sm"
            className={kanbanOutlineButtonClass}
            onClick={view.retrySave}
          >
            <RefreshCw className="size-4" aria-hidden="true" />
            {copy.personal.retry}
          </Button>
        </div>
      ) : null}
    </div>
  );
}

export interface FilterNoResultsProps {
  copy: FiltersCopy;
  conditions: number;
  total: number;
  onEdit: () => void;
  onClear: () => void;
}

/** The empty-by-filter state: not an error and not an empty board. */
export function FilterNoResults({
  copy,
  conditions,
  total,
  onEdit,
  onClear,
}: FilterNoResultsProps) {
  return (
    <div
      role="status"
      className="mx-auto flex max-w-sm flex-col items-center gap-1.5 rounded-xl border border-line bg-popover px-5 py-8 text-center dark:bg-card"
    >
      <Filter className="size-5 text-muted-foreground" aria-hidden="true" />
      <b className="text-sm font-semibold">{copy.states.noResults}</b>
      <span className="text-compact text-muted-foreground">
        {fillFiltersCopy(copy.states.noResultsHint, {
          count: conditions,
          total,
        })}
      </span>
      <div className="mt-2 flex gap-2">
        <Button
          variant="outline"
          size="sm"
          className={kanbanOutlineButtonClass}
          onClick={onEdit}
        >
          {copy.states.editConditions}
        </Button>
        <Button size="sm" className="h-8 text-compact" onClick={onClear}>
          {copy.states.clear}
        </Button>
      </div>
    </div>
  );
}
