import { ChevronDown, RefreshCw, User, X } from "lucide-react";
import {
  Button,
  cn,
  Sheet,
  SheetContent,
  SheetTitle,
  SheetTrigger,
} from "@notrelix/ui-web";
import {
  DATE_BUCKETS,
  type DateBucket,
  type FieldRef,
  type FilterField,
  type SortLevel,
} from "@notrelix/work-management-core";
import {
  compactIconButtonClass,
  compactPillClass,
  compactPillOnClass,
  compactSelectClass,
  sheetClass,
} from "./compact-chrome";
import type { FiltersCopy } from "./filters.copy";
import type { ViewStateControl } from "./view-state-control";

export interface ViewSheetProps {
  copy: FiltersCopy;
  view: ViewStateControl;
  fields: readonly FilterField[];
  /** The Table's sort level choice. */
  sort?: boolean | undefined;
  sortFallback?: string | undefined;
  groupFields?: readonly FilterField[] | undefined;
  groupFallback?: FieldRef | undefined;
  laneFields?: readonly FilterField[] | undefined;
  blockedLaneFieldId?: string | undefined;
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

const label = "text-xs font-semibold leading-[1.45] text-meta-foreground";

/**
 * "Cách xem" on a phone (`WM-Filter-320`): the member's own sort, grouping and
 * swimlane in one sheet that rises from the bottom, with "Về view chung" while
 * they have a view of their own. Writes the member's own view only.
 */
export function ViewSheet(props: ViewSheetProps) {
  const { copy, view, fields } = props;
  const t = copy.sheet;
  const close = () => props.onOpenChange(false);
  const first = view.state.sorts[0];
  const group = view.state.group ?? props.groupFallback;
  const groupField = props.groupFields?.find((f) => f.id === group?.fieldId);
  const lane = view.state.lane;
  return (
    <Sheet open={props.open} onOpenChange={props.onOpenChange}>
      <SheetTrigger asChild>
        <Button
          variant="ghost"
          size="sm"
          aria-haspopup="dialog"
          className={cn(compactPillClass, view.personal && compactPillOnClass)}
        >
          {t.trigger}
          <ChevronDown className="size-4" aria-hidden="true" />
        </Button>
      </SheetTrigger>
      <SheetContent
        side="bottom"
        showCloseButton={false}
        aria-label={t.title}
        aria-describedby={undefined}
        className={sheetClass}
      >
        <div className="flex items-center gap-2 px-3 pt-3">
          <SheetTitle className="m-0 flex-1 text-base leading-[1.45] font-[650]">
            {t.title}
          </SheetTitle>
          <button
            type="button"
            aria-label={t.close}
            onClick={close}
            className={compactIconButtonClass}
          >
            <X className="size-4" aria-hidden="true" />
          </button>
        </div>
        <div className="flex min-h-0 flex-1 flex-col overflow-y-auto">
          {view.personal ? (
            <div className="px-3 pt-2">
              <span
                role="group"
                aria-label={copy.personal.label}
                className="inline-flex h-9 w-full items-center justify-between gap-1.5 rounded-lg border border-dashed border-line-strong pr-0.75 pl-2.5 text-compact text-muted-foreground"
              >
                <span className="inline-flex items-center gap-1.5">
                  <User className="size-4" aria-hidden="true" />
                  {copy.personal.label}
                </span>
                <button
                  type="button"
                  onClick={view.reset}
                  className="inline-flex h-7.5 cursor-pointer items-center gap-1 rounded-md border-0 bg-transparent px-2 text-[12.5px] font-semibold text-state-selected-foreground hover:bg-state-hover-strong focus-visible:outline-2 focus-visible:outline-ring"
                >
                  <RefreshCw className="size-4" aria-hidden="true" />
                  {copy.personal.reset}
                </button>
              </span>
            </div>
          ) : null}
          <div className="flex flex-col gap-3 px-3 py-2">
            {props.sort ? (
              <div className="text-[14px] leading-[1.45]">
                <span className={label}>{t.sort}</span>
                <div className="mt-1.5 flex gap-2">
                  <select
                    aria-label={copy.sort.trigger}
                    className={cn(compactSelectClass, "flex-1")}
                    value={first?.fieldId ?? ""}
                    onChange={(event) => {
                      const id = event.target.value;
                      const rest = view.state.sorts.slice(1);
                      view.setSorts(
                        id === ""
                          ? []
                          : [
                              {
                                fieldId: id,
                                direction: first?.direction ?? "asc",
                              },
                              ...rest.filter((l) => l.fieldId !== id),
                            ],
                      );
                    }}
                  >
                    <option value="">
                      {props.sortFallback || t.sortDefault}
                    </option>
                    {fields.map((f) => (
                      <option key={f.id} value={f.id}>
                        {f.name}
                      </option>
                    ))}
                  </select>
                  <div
                    role="group"
                    aria-label={copy.sort.direction}
                    className="inline-flex gap-0.5 rounded-lg border border-line-strong bg-state-hover p-0.5"
                  >
                    {(["asc", "desc"] as const).map((direction) => (
                      <button
                        key={direction}
                        type="button"
                        disabled={!first}
                        aria-label={
                          direction === "asc" ? copy.sort.asc : copy.sort.desc
                        }
                        aria-pressed={first?.direction === direction}
                        onClick={() =>
                          first &&
                          view.setSorts([
                            { ...first, direction } satisfies SortLevel,
                            ...view.state.sorts.slice(1),
                          ])
                        }
                        className={cn(
                          "inline-flex h-8 min-w-8 cursor-pointer items-center justify-center rounded-md border-0 bg-transparent px-3 text-[12.5px] font-semibold text-muted-foreground focus-visible:outline-2 focus-visible:outline-ring disabled:cursor-not-allowed",
                          first?.direction === direction &&
                            "bg-popover font-bold text-foreground ring-1 ring-line-strong",
                        )}
                      >
                        {direction === "asc" ? "↑" : "↓"}
                      </button>
                    ))}
                  </div>
                </div>
              </div>
            ) : null}
            {props.groupFields ? (
              <div className="text-[14px] leading-[1.45]">
                <span className={label}>{t.group}</span>
                <div className="mt-1.5 flex flex-col gap-1.5">
                  <select
                    aria-label={copy.group.label}
                    className={compactSelectClass}
                    value={group?.fieldId ?? ""}
                    onChange={(event) => {
                      const id = event.target.value;
                      const next = props.groupFields?.find((f) => f.id === id);
                      view.setGroup(
                        next
                          ? {
                              fieldId: id,
                              ...(next.field.fieldType === "date"
                                ? { bucket: group?.bucket ?? "week" }
                                : {}),
                            }
                          : undefined,
                      );
                    }}
                  >
                    <option value="">{copy.group.none}</option>
                    {props.groupFields.map((f) => (
                      <option key={f.id} value={f.id}>
                        {f.name}
                      </option>
                    ))}
                  </select>
                  {groupField?.field.fieldType === "date" ? (
                    <select
                      aria-label={t.bucket}
                      className={compactSelectClass}
                      value={group?.bucket ?? "week"}
                      onChange={(event) =>
                        view.setGroup({
                          fieldId: groupField.id,
                          bucket:
                            DATE_BUCKETS.find(
                              (b) => b === event.target.value,
                            ) ?? "week",
                        })
                      }
                    >
                      {DATE_BUCKETS.map((bucket: DateBucket) => (
                        <option key={bucket} value={bucket}>
                          {copy.group.bucket[bucket]}
                        </option>
                      ))}
                    </select>
                  ) : null}
                </div>
              </div>
            ) : null}
            {props.laneFields ? (
              <div className="text-[14px] leading-[1.45]">
                <span className={label}>{t.lane}</span>
                <div className="mt-1.5">
                  <select
                    aria-label={copy.lane.label}
                    className={compactSelectClass}
                    value={lane?.fieldId ?? ""}
                    onChange={(event) => {
                      const id = event.target.value;
                      const next = props.laneFields?.find((f) => f.id === id);
                      view.setLane(
                        next
                          ? {
                              fieldId: id,
                              ...(next.field.fieldType === "date"
                                ? { bucket: lane?.bucket ?? "week" }
                                : {}),
                            }
                          : undefined,
                      );
                    }}
                  >
                    <option value="">{copy.lane.none}</option>
                    {props.laneFields.map((f) => (
                      <option
                        key={f.id}
                        value={f.id}
                        disabled={f.id === props.blockedLaneFieldId}
                      >
                        {f.id === props.blockedLaneFieldId
                          ? `${f.name} (${copy.lane.sameAsColumn})`
                          : f.name}
                      </option>
                    ))}
                  </select>
                </div>
              </div>
            ) : null}
            <p className="m-0 text-[12.5px] leading-[1.45] text-meta-foreground">
              {t.note}
            </p>
          </div>
        </div>
        <div className="flex gap-2 border-t border-line px-3 py-2.5">
          <Button
            variant="ghost"
            size="sm"
            className="h-9 px-3 text-compact text-muted-foreground"
            onClick={view.reset}
          >
            {t.reset}
          </Button>
          <span className="flex-1" />
          <Button
            size="sm"
            className="h-9 px-3 text-compact font-semibold"
            onClick={close}
          >
            {t.done}
          </Button>
        </div>
      </SheetContent>
    </Sheet>
  );
}
