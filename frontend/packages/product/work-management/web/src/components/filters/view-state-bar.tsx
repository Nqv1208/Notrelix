import { useId, useState, type ReactNode } from "react";
import { AlertCircle, Check, Lock, RefreshCw, User, X } from "lucide-react";
import {
  Button,
  Tooltip,
  TooltipContent,
  TooltipTrigger,
  cn,
  useMediaQuery,
} from "@notrelix/ui-web";
import {
  conditionIssue,
  isStaleCondition,
  OVERDUE_FIELD_ID,
  type FieldRef,
  type FilterField,
  type MemberNames,
  type SavedFilterCriteria,
  viewStateFromCriteria,
} from "@notrelix/work-management-core";
import { kanbanPillClass } from "../views/kanban/kanban-chrome";
import { describeCondition, staleConditionCount } from "./condition-label";
import { usePublishFilterChipLabels } from "./filter-chip-labels";
import { FilterBuilder } from "./filter-builder";
import { FieldRefMenu } from "./field-ref-menu";
import { fillFiltersCopy, type FiltersCopy } from "./filters.copy";
import type { SummaryPhrase } from "../saved-filters/saved-filters-menu";
import { SortMenu } from "./sort-menu";
import { ViewSheet } from "./view-sheet";
import type { ViewStateControl } from "./view-state-control";

const MAX_CHIPS = 3;

/** The "Lưu bộ lọc" form the bar controls for its saved-filter slot. */
export interface SaveFormState {
  saveOpen: boolean;
  onSaveOpenChange: (open: boolean) => void;
  /** A phone: the saved-filter trigger is the 36 px icon alone and "Lưu bộ lọc" is in the filter sheet. */
  phone?: boolean;
  /** What the bar shows now, one phrase per condition and the grouping: the save form's summary. */
  summary: readonly SummaryPhrase[];
  /** The same phrases for a saved filter's criteria: the subtitle of its row. */
  describe: (criteria: SavedFilterCriteria) => readonly SummaryPhrase[];
}

export interface ViewStateBarProps {
  copy: FiltersCopy;
  view: ViewStateControl;
  fields: readonly FilterField[];
  memberName: MemberNames;
  people: readonly { id: string; name: string }[];
  /** Items the filter keeps and the items it was applied to. */
  counts: { shown: number; total: number };
  now: Date;
  /** The board's IANA zone (its workspace's). */
  timeZone?: string;
  /** True while the workspace setting could not be read and the default zone stands in. */
  timeZoneIsDefault?: boolean;
  /** Which of the sort, group and swimlane controls this view has. */
  sort?: boolean;
  /** The shared view's sort as text, shown by the sort pill while the member has none. */
  sortFallback?: string;
  groupFields?: readonly FilterField[];
  /** The grouping the view has by default (Status), shown until the member picks one. */
  groupFallback?: FieldRef | undefined;
  laneFields?: readonly FilterField[];
  /** The field that is the column of a Kanban; it cannot also be the lane. */
  blockedLaneFieldId?: string | undefined;
  /**
   * The saved-filter controls, shown after the personal-state chip. A function
   * receives the state of the "Lưu bộ lọc" form, so the removed-field warning
   * can offer "Lưu thành bộ lọc mới".
   */
  savedFilters?: ReactNode | ((save: SaveFormState) => ReactNode);
  /**
   * False for a viewer: filtering, sorting and grouping still work for them
   * alone, but saving a filter is shown disabled with the reason.
   */
  canSave?: boolean;
  /** Opens the host's "Lưu bộ lọc" form when `savedFilters` is a plain node. */
  onSaveAsNew?: (() => void) | undefined;
  /** The filter dialog is controlled so "Sửa điều kiện" can open it. */
  filterOpen?: boolean;
  onFilterOpenChange?: (open: boolean) => void;
  /** Rendered right after the group control, before the swimlane control (the tablet Kanban's avatar stack). */
  afterGroup?: ReactNode;
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
  usePublishFilterChipLabels(
    applied.map((condition) =>
      describeCondition(
        copy,
        condition,
        fields.find((f) => f.id === condition.fieldId),
        memberName,
      ),
    ),
  );
  const byId = new Map(fields.map((f) => [f.id, f]));
  const shown = filter.conditions.slice(0, MAX_CHIPS);
  const hidden = filter.conditions.length - shown.length;
  const [openInner, setOpenInner] = useState(false);
  const open = props.filterOpen ?? openInner;
  const setOpen = props.onFilterOpenChange ?? setOpenInner;
  const [saveOpen, setSaveOpen] = useState(false);
  const [viewOpen, setViewOpen] = useState(false);
  // Under 640 px the bar is the phone bar of WM-Filter-320: Lọc, Cách xem and the saved filters.
  const compact = useMediaQuery("(max-width: 639px)");
  const reasonId = useId();
  const describe = (criteria: SavedFilterCriteria): SummaryPhrase[] => {
    const state = viewStateFromCriteria(criteria);
    const groupField = state.group
      ? fields.find((f) => f.id === state.group?.fieldId)
      : undefined;
    return [
      ...state.filter.conditions
        .filter((condition) => conditionIssue(condition) === null)
        .map((condition) => {
          const field = fields.find((f) => f.id === condition.fieldId);
          return {
            text: describeCondition(copy, condition, field, memberName),
            ...(known && (!field || isStaleCondition(condition, byId))
              ? { stale: true }
              : {}),
          };
        }),
      ...(groupField
        ? [{ text: `${copy.group.label} ${groupField.name}`, group: true }]
        : []),
    ];
  };
  const saved =
    typeof props.savedFilters === "function"
      ? props.savedFilters({
          saveOpen,
          onSaveOpenChange: setSaveOpen,
          ...(compact ? { phone: true } : {}),
          summary: [
            ...applied.map((condition) => ({
              text: describeCondition(
                copy,
                condition,
                fields.find((f) => f.id === condition.fieldId),
                memberName,
              ),
            })),
            ...(view.state.group
              ? [
                  {
                    text: `${copy.group.label} ${
                      fields.find((f) => f.id === view.state.group?.fieldId)
                        ?.name ?? ""
                    }`,
                    group: true,
                  },
                ]
              : []),
          ],
          describe,
        })
      : props.savedFilters;
  const onSaveAsNew =
    typeof props.savedFilters === "function"
      ? () => setSaveOpen(true)
      : props.onSaveAsNew;
  const zoneNote = props.timeZone
    ? fillFiltersCopy(
        props.timeZoneIsDefault
          ? copy.filter.timeZoneDefaultNote
          : copy.filter.timeZoneNote,
        { zone: props.timeZone },
      )
    : undefined;
  const chips =
    shown.length > 0 ? (
      <ul
        aria-label={copy.filter.trigger}
        className="m-0 flex min-w-0 list-none flex-wrap items-center gap-1.5 p-0"
      >
        {shown.map((condition, index) => {
          const field = fields.find((f) => f.id === condition.fieldId);
          const stale = known && (!field || isStaleCondition(condition, byId));
          const label = describeCondition(copy, condition, field, memberName);
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
                  "inline-flex h-6.5 max-w-full items-center gap-1 rounded-full pl-2.5 pr-1 text-[12.5px] font-semibold tablet:h-11 tablet:gap-0 tablet:rounded-[10px] tablet:pr-0 tablet:pl-3.5 tablet:text-[14px] tablet:font-medium",
                  stale
                    ? "bg-warning-subtle text-warning-subtle-foreground ring-1 ring-warning/50"
                    : "bg-state-selected text-state-selected-foreground",
                )}
              >
                {stale ? (
                  <AlertCircle className="size-4" aria-hidden="true" />
                ) : null}
                <span className="min-w-0 truncate tablet:pr-0.5">{label}</span>
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
                  className="grid size-4.5 cursor-pointer place-items-center rounded-full border-0 bg-transparent text-inherit hover:bg-state-hover-strong focus-visible:outline-2 focus-visible:outline-ring tablet:size-11 tablet:rounded-[10px]"
                >
                  <X className="size-3.5 tablet:size-4" aria-hidden="true" />
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
              className="h-6.5 cursor-pointer rounded-full border-0 bg-state-hover px-2.5 text-[12.5px] tablet:h-11 tablet:rounded-[10px] tablet:text-[14px] font-semibold text-muted-foreground hover:bg-state-hover-strong focus-visible:outline-2 focus-visible:outline-ring"
            >
              {fillFiltersCopy(copy.filter.more, { count: hidden })}
            </button>
          </li>
        ) : null}
      </ul>
    ) : null;
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
        timeZone={props.timeZone}
        timeZoneNote={zoneNote}
        activeCount={applied.length}
        compact={compact}
        open={open}
        onOpenChange={setOpen}
        onSaveFilter={
          typeof props.savedFilters === "function"
            ? () => {
                setOpen(false);
                setSaveOpen(true);
              }
            : undefined
        }
      />
      {compact ? null : chips}
      {compact && (props.sort || props.groupFields || props.laneFields) ? (
        <>
          <ViewSheet
            copy={copy}
            view={view}
            fields={fields}
            sort={props.sort}
            sortFallback={props.sortFallback}
            groupFields={props.groupFields}
            groupFallback={props.groupFallback}
            laneFields={props.laneFields}
            blockedLaneFieldId={props.blockedLaneFieldId}
            open={viewOpen}
            onOpenChange={setViewOpen}
          />
        </>
      ) : null}
      {compact ? <span className="flex-1" /> : null}
      {!compact && props.sort ? (
        <SortMenu
          copy={copy}
          fields={fields}
          levels={view.state.sorts}
          onChange={view.setSorts}
          fallbackSummary={props.sortFallback}
        />
      ) : null}
      {!compact && props.groupFields ? (
        <FieldRefMenu
          copy={copy}
          kind="group"
          fields={props.groupFields}
          unavailableFields={fields.filter(
            (field) =>
              field.id !== OVERDUE_FIELD_ID &&
              !props.groupFields?.some((group) => group.id === field.id),
          )}
          value={view.state.group}
          fallback={props.groupFallback}
          timeZoneNote={zoneNote}
          onChange={view.setGroup}
        />
      ) : null}
      {!compact ? props.afterGroup : null}
      {!compact && props.laneFields ? (
        <FieldRefMenu
          copy={copy}
          kind="lane"
          fields={props.laneFields}
          value={view.state.lane}
          blockedFieldId={props.blockedLaneFieldId}
          onChange={view.setLane}
        />
      ) : null}
      {!compact && view.personal ? (
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
      {props.canSave === false && applied.length > 0 ? (
        <>
          <Tooltip>
            <TooltipTrigger asChild>
              <span
                tabIndex={0}
                className="inline-flex rounded-md outline-none focus-visible:ring-2 focus-visible:ring-focus-ring"
              >
                <Button
                  variant="ghost"
                  size="sm"
                  disabled
                  aria-describedby={reasonId}
                  className={kanbanPillClass}
                >
                  <Lock className="size-4" aria-hidden="true" />
                  {copy.viewer.saveLabel}
                </Button>
              </span>
            </TooltipTrigger>
            {/* Drawn without an arrow (WM-Filter-States, "Người chỉ xem"): a lock and the reason, 340 px wide. */}
            <TooltipContent
              arrow={false}
              side="bottom"
              align="start"
              sideOffset={6}
              className="block min-h-13.5 w-85 max-w-85 rounded-[8px] px-2.5 py-2 text-crumb leading-[1.4] shadow-popover"
            >
              <Lock
                className="mr-1.5 inline size-4 align-top"
                aria-hidden="true"
              />
              {copy.viewer.saveDisabled}
            </TooltipContent>
          </Tooltip>
          <span id={reasonId} className="sr-only">
            {copy.viewer.saveDisabled}
          </span>
        </>
      ) : null}
      {saved}
      {compact ? chips : null}
      <div className="basis-full empty:hidden">
        <ViewStateNotices
          copy={copy}
          view={view}
          fields={fields}
          onSaveAsNew={onSaveAsNew}
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
  if (!view.loaded)
    return <RestoringSkeleton label={copy.personal.restoring} />;
  const restored = view.restored === true && view.personal;
  if (stale === 0 && !view.saveFailed && !restored) return null;
  const known = new Map(fields.map((f) => [f.id, f]));
  const goneFields = view.state.filter.conditions.filter(
    (c) => !known.has(c.fieldId),
  ).length;
  const goneValues = stale - goneFields;
  const small =
    "h-7 cursor-pointer gap-1.5 rounded-[7px] px-2.5 text-[12.5px] font-medium";
  return (
    <div className="flex flex-col gap-2 pb-2">
      {restored ? (
        <RestoredNotice copy={copy} view={view} fields={fields} />
      ) : null}
      {stale > 0 ? (
        <div className="flex flex-col gap-2">
          <div
            role="status"
            className="flex items-start gap-2 rounded-[9px] bg-warning-subtle px-3 py-[9px] text-[12.5px] leading-[1.45] text-warning-subtle-foreground ring-1 ring-warning/50 ring-inset"
          >
            <AlertCircle className="size-4 shrink-0" aria-hidden="true" />
            <span className="min-w-0 flex-1">
              {[
                goneFields > 0
                  ? fillFiltersCopy(copy.stale.hint, { count: goneFields })
                  : "",
                goneValues > 0
                  ? fillFiltersCopy(copy.stale.valueHint, { count: goneValues })
                  : "",
              ]
                .filter(Boolean)
                .join(" ")}
            </span>
          </div>
          <div className="flex flex-wrap gap-2">
            <Button
              variant="outline"
              size="sm"
              className={small}
              onClick={() =>
                view.setFilter({
                  ...view.state.filter,
                  conditions: view.state.filter.conditions.filter(
                    (c) => !isStaleCondition(c, known),
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
                className={small}
                onClick={onSaveAsNew}
              >
                {copy.stale.saveNew}
              </Button>
            ) : null}
          </div>
        </div>
      ) : null}
      {view.saveFailed ? (
        <div
          role="alert"
          className="flex items-start gap-2 rounded-[9px] bg-destructive/10 px-3 py-[9px] text-[12.5px] leading-[1.45] text-destructive"
        >
          <AlertCircle className="size-4 shrink-0" aria-hidden="true" />
          <span className="min-w-0 flex-1">{copy.personal.saveFailed}</span>
          <Button
            variant="outline"
            size="sm"
            className={small}
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

/** While the member's saved view is read back: three bars where the bar's notices will be (WM-Filter-States "Đang khôi phục"). */
function RestoringSkeleton({ label }: { label: string }) {
  return (
    <div
      role="status"
      aria-label={label}
      className="mb-2 flex flex-col gap-2 rounded-xl border border-line bg-card p-3"
    >
      <div className="h-6.5 w-3/5 animate-pulse rounded-lg bg-state-hover-strong" />
      <div className="h-6.5 animate-pulse rounded-lg bg-state-hover-strong" />
      <div className="h-6.5 animate-pulse rounded-lg bg-state-hover-strong" />
    </div>
  );
}

/** After a reload: the view the member's last visit saved is back, and "Về view chung" clears it (WM-Filter-States "Đã khôi phục"). */
function RestoredNotice({
  copy,
  view,
  fields,
}: Pick<ViewStateNoticesProps, "copy" | "view" | "fields">) {
  const t = copy.personal;
  const name = (id: string | undefined) =>
    fields.find((f) => f.id === id)?.name ?? "";
  const { filter, sorts, group, lane } = view.state;
  const parts = [
    filter.conditions.length > 0
      ? fillFiltersCopy(t.restoredFilter, { count: filter.conditions.length })
      : "",
    sorts.length > 0
      ? fillFiltersCopy(t.restoredSort, { count: sorts.length })
      : "",
    group
      ? fillFiltersCopy(t.restoredGroup, { field: name(group.fieldId) })
      : "",
    lane ? fillFiltersCopy(t.restoredLane, { field: name(lane.fieldId) }) : "",
  ].filter(Boolean);
  return (
    <div className="flex flex-col gap-2.5">
      <div className="flex flex-col gap-2 rounded-xl border border-line bg-card px-3.5 py-3">
        <span role="status" className="flex items-center gap-2 text-[13px]">
          <Check
            className="size-4 shrink-0 text-success-text"
            aria-hidden="true"
          />
          <span className="min-w-0">
            {fillFiltersCopy(t.restored, { parts: parts.join(", ") })}
          </span>
          <Button
            variant="ghost"
            size="sm"
            className="ml-auto h-7 cursor-pointer rounded-[7px] px-2.5 text-[12.5px] font-medium text-muted-foreground"
            onClick={view.reset}
          >
            {t.reset}
          </Button>
        </span>
      </div>
      <p className="m-0 text-[12.5px] leading-[1.45] text-meta-foreground">
        {t.restoredNote}
      </p>
    </div>
  );
}
