import { useState, type ReactNode } from "react";
import { Bookmark, Calendar, Check, Filter, Plus, User, X } from "lucide-react";
import {
  Button,
  cn,
  Popover,
  PopoverContent,
  PopoverTrigger,
  Sheet,
  SheetContent,
  SheetTitle,
  SheetTrigger,
} from "@notrelix/ui-web";
import {
  NONE_VALUE,
  conditionIssue,
  newCondition,
  operatorTakesValue,
  operatorsFor,
  presetCondition,
  withOperator,
  type CompoundFilter,
  type ConditionOperator,
  type DatePreset,
  type FilterCondition,
  type FilterField,
} from "@notrelix/work-management-core";
import { kanbanPillClass } from "../views/kanban/kanban-chrome";
import {
  compactIconButtonClass,
  compactPillClass,
  compactPillOnClass,
  sheetClass,
} from "./compact-chrome";
import { fillFiltersCopy, type FiltersCopy } from "./filters.copy";

const controlClass =
  "h-8.5 min-w-0 rounded-[7px] border border-line-strong bg-popover px-2.5 text-[13.5px] text-foreground focus-visible:outline-2 focus-visible:outline-ring";
export const activePillClass =
  "bg-state-selected text-state-selected-foreground hover:bg-state-selected";

export interface FilterBuilderProps {
  copy: FiltersCopy;
  fields: readonly FilterField[];
  filter: CompoundFilter;
  onChange: (filter: CompoundFilter) => void;
  /** Items the filter keeps and the items it was applied to. */
  counts: { shown: number; total: number };
  /** The people a person field's picker lists (board members, then other assignees). */
  people: readonly { id: string; name: string }[];
  /** The day "Hôm nay" and "Tuần này" are judged against. */
  now: Date;
  /** The board's IANA zone: presets pick "today" in it. */
  timeZone?: string;
  /** The caption under the date row: which zone the days are judged in. */
  timeZoneNote?: string;
  /** A phone (under 640 px): the dialog is a sheet that rises from the bottom, each condition a stacked card. */
  compact?: boolean;
  /** The number the trigger shows: conditions that apply. */
  activeCount: number;
  /** Opens the dialog (controlled by the host so "Sửa điều kiện" can open it). */
  open?: boolean;
  onOpenChange?: (open: boolean) => void;
  /** Shows "Lưu bộ lọc" in the footer; the host closes the dialog and opens its save form. */
  onSaveFilter?: () => void;
}

/**
 * The "Lọc" pill and its dialog: rows of field, operator and value joined by
 * one AND or OR. Changes apply as they are typed; "Xong" only closes. The
 * dialog is modal, so the board behind it is inert and hidden from screen
 * readers while it is open.
 */
export function FilterBuilder(props: FilterBuilderProps) {
  const { copy, filter, onChange, activeCount } = props;
  const [innerOpen, setInnerOpen] = useState(false);
  const open = props.open ?? innerOpen;
  const setOpen = props.onOpenChange ?? setInnerOpen;
  if (props.compact)
    return (
      <Sheet open={open} onOpenChange={setOpen}>
        <SheetTrigger asChild>
          <Button
            variant="ghost"
            size="sm"
            className={cn(
              compactPillClass,
              activeCount > 0 && compactPillOnClass,
            )}
            aria-haspopup="dialog"
          >
            <Filter className="size-4" aria-hidden="true" />
            {copy.filter.trigger}
            {activeCount > 0 ? ` · ${activeCount}` : ""}
          </Button>
        </SheetTrigger>
        <SheetContent
          side="bottom"
          showCloseButton={false}
          aria-label={copy.filter.title}
          aria-describedby={undefined}
          className={sheetClass}
        >
          <div className="flex items-center gap-2 px-3 pt-3">
            <SheetTitle className="m-0 flex-1 text-base leading-[1.45] font-[650]">
              {copy.filter.title}
            </SheetTitle>
            <button
              type="button"
              aria-label={copy.sheet.close}
              onClick={() => setOpen(false)}
              className={compactIconButtonClass}
            >
              <X className="size-4" aria-hidden="true" />
            </button>
          </div>
          <div className="flex min-h-0 flex-1 flex-col overflow-y-auto">
            <BuilderBody {...props} filter={filter} onChange={onChange} />
            {props.onSaveFilter && activeCount > 0 ? (
              <div className="px-3 pb-2">
                <Button
                  variant="outline"
                  size="sm"
                  className="h-9 gap-1.5 rounded-[7px] border-line-strong bg-popover px-2.5 text-[12.5px] font-medium shadow-none dark:bg-card"
                  onClick={props.onSaveFilter}
                >
                  <Bookmark className="size-4" aria-hidden="true" />
                  {copy.filter.saveFilter}
                </Button>
              </div>
            ) : null}
          </div>
          <div className="flex items-center gap-2 border-t border-line px-3 py-2.5">
            <span
              role="status"
              aria-live="polite"
              className="flex-1 text-[12.5px] text-foreground"
            >
              <MatchCount
                template={copy.filter.count}
                shown={props.counts.shown}
                total={props.counts.total}
              />
            </span>
            <Button
              variant="ghost"
              size="sm"
              className="h-9 px-3 text-compact text-muted-foreground"
              onClick={() => onChange({ ...filter, conditions: [] })}
            >
              {copy.filter.clearAll}
            </Button>
            <Button
              size="sm"
              className="h-9 px-3 text-compact font-semibold"
              onClick={() => setOpen(false)}
            >
              {copy.filter.done}
            </Button>
          </div>
        </SheetContent>
      </Sheet>
    );
  return (
    <Popover open={open} onOpenChange={setOpen} modal>
      <PopoverTrigger asChild>
        <Button
          variant="ghost"
          size="sm"
          className={cn(kanbanPillClass, activeCount > 0 && activePillClass)}
          aria-haspopup="dialog"
        >
          <Filter className="size-4" aria-hidden="true" />
          {copy.filter.trigger}
          {activeCount > 0 ? ` · ${activeCount}` : ""}
        </Button>
      </PopoverTrigger>
      <PopoverContent
        align="start"
        aria-label={copy.filter.title}
        collisionPadding={12}
        className="max-h-[var(--radix-popover-content-available-height)] w-[min(47.5rem,calc(100vw-1.5rem))] gap-3 overflow-y-auto rounded-[12px] border border-line p-4"
      >
        <BuilderBody {...props} filter={filter} onChange={onChange} />
        <div className="flex items-center gap-2.5 border-t border-line pt-3">
          <span
            role="status"
            aria-live="polite"
            className="flex-1 text-[12.5px] text-muted-foreground"
          >
            <MatchCount
              template={copy.filter.count}
              shown={props.counts.shown}
              total={props.counts.total}
            />
          </span>
          <Button
            variant="ghost"
            size="sm"
            className="h-7 text-[12.5px]"
            onClick={() => onChange({ ...filter, conditions: [] })}
          >
            {copy.filter.clearAll}
          </Button>
          {props.onSaveFilter && activeCount > 0 ? (
            <Button
              variant="outline"
              size="sm"
              className="h-7 gap-1.5 rounded-[7px] border-line-strong bg-popover px-2.5 text-[12.5px] font-medium shadow-none dark:bg-card"
              onClick={props.onSaveFilter}
            >
              <Bookmark className="size-4" aria-hidden="true" />
              {copy.filter.saveFilter}
            </Button>
          ) : null}
          <Button
            size="sm"
            className="h-7 text-[12.5px] font-semibold"
            onClick={() => setOpen(false)}
          >
            {copy.filter.done}
          </Button>
        </div>
      </PopoverContent>
    </Popover>
  );
}

/** "Khớp 5 / 24 việc": the two numbers in bold, as the canvas draws them. */
function MatchCount({
  template,
  shown,
  total,
}: {
  template: string;
  shown: number;
  total: number;
}) {
  const parts = template.split(/(\{\{shown\}\}.*?\{\{total\}\})/);
  return (
    <>
      {parts.map((part, index) =>
        index % 2 === 1 ? (
          <b key={index} className="font-bold text-foreground">
            {fillFiltersCopy(part, { shown, total })}
          </b>
        ) : (
          part
        ),
      )}
    </>
  );
}

function BuilderBody({
  copy,
  fields,
  filter,
  onChange,
  now,
  timeZone,
  timeZoneNote,
  people,
  compact,
}: FilterBuilderProps) {
  const update = (index: number, next: FilterCondition) =>
    onChange({
      ...filter,
      conditions: filter.conditions.map((c, i) => (i === index ? next : c)),
    });
  const remove = (index: number) =>
    onChange({
      ...filter,
      conditions: filter.conditions.filter((_, i) => i !== index),
    });
  const first = fields[0];
  return (
    <>
      <div
        className={cn("flex items-center gap-3", compact && "px-3 pt-2 pb-2")}
      >
        {compact ? null : (
          <h2 className="flex-1 text-base leading-[1.45] font-[650]">
            {copy.filter.title}
          </h2>
        )}
        <div
          role="group"
          aria-label={copy.filter.matchLabel}
          className={cn(
            "inline-flex gap-0.5 rounded-lg border border-line-strong bg-state-hover p-0.5",
            compact && "w-full",
          )}
        >
          {(["and", "or"] as const).map((join) => (
            <button
              key={join}
              type="button"
              aria-pressed={filter.join === join}
              onClick={() => onChange({ ...filter, join })}
              className={cn(
                "inline-flex h-7 cursor-pointer items-center justify-center gap-1 rounded-md border-0 bg-transparent px-3 text-[12.5px] font-semibold text-muted-foreground focus-visible:outline-2 focus-visible:outline-ring",
                compact && "h-9 flex-1",
                filter.join === join &&
                  "bg-popover font-bold text-foreground ring-1 ring-line-strong",
              )}
            >
              {join === "and" ? copy.filter.matchAll : copy.filter.matchAny}
            </button>
          ))}
        </div>
      </div>
      <div className={cn("flex flex-col gap-3", compact && "gap-2 px-3")}>
        {filter.conditions.map((condition, index) => (
          <ConditionRow
            key={index}
            compact={compact}
            index={index}
            copy={copy}
            fields={fields}
            condition={condition}
            join={filter.join}
            now={now}
            timeZone={timeZone}
            timeZoneNote={timeZoneNote}
            people={people}
            onChange={(next) => update(index, next)}
            onRemove={() => remove(index)}
          />
        ))}
      </div>
      <Button
        variant="ghost"
        size="sm"
        className={cn(
          "h-7 gap-[6px] self-start text-[12.5px] text-muted-foreground",
          compact && "mx-3 my-2 h-9",
        )}
        disabled={!first}
        onClick={() =>
          first &&
          onChange({
            ...filter,
            conditions: [...filter.conditions, newCondition(first.field)],
          })
        }
      >
        <Plus className="size-4" aria-hidden="true" />
        {copy.filter.add}
      </Button>
    </>
  );
}

function ConditionRow({
  index,
  copy,
  fields,
  condition,
  join,
  now,
  timeZone,
  timeZoneNote,
  people,
  compact,
  onChange,
  onRemove,
}: {
  compact?: boolean | undefined;
  people: readonly { id: string; name: string }[];
  index: number;
  copy: FiltersCopy;
  fields: readonly FilterField[];
  condition: FilterCondition;
  join: "and" | "or";
  now: Date;
  /** The board's IANA zone: presets pick "today" in it. */
  timeZone?: string;
  /** The caption under the date row: which zone the days are judged in. */
  timeZoneNote?: string;
  onChange: (condition: FilterCondition) => void;
  onRemove: () => void;
}) {
  const label = fillFiltersCopy(copy.filter.condition, { n: index + 1 });
  const field = fields.find((f) => f.id === condition.fieldId);
  const operators = field ? operatorsFor(field.field) : [];
  const issue = conditionIssue(condition);
  const lead =
    index === 0
      ? copy.filter.when
      : join === "and"
        ? copy.filter.and
        : copy.filter.or;
  return (
    <div
      role="group"
      aria-label={label}
      className={cn(
        "group/row flex flex-col gap-1",
        compact && "rounded-[10px] border border-line p-2.5",
      )}
    >
      <div
        className={
          compact
            ? "grid grid-cols-[minmax(2.125rem,auto)_minmax(0,1fr)_2.25rem] items-center gap-x-2 gap-y-1.5"
            : "grid grid-cols-[2.75rem_11.875rem_10.625rem_minmax(0,1fr)_1.875rem] items-start gap-2 max-sm:grid-cols-1"
        }
      >
        <span
          className={cn(
            "flex h-8.5 items-center justify-end text-xs font-semibold text-muted-foreground max-sm:justify-start",
            compact && "h-9 justify-start",
          )}
        >
          {lead}
        </span>
        <select
          aria-label={`${copy.filter.field} (${label})`}
          className={cn(controlClass, compact && "w-full")}
          value={field ? field.id : "__stale__"}
          onChange={(event) => {
            const next = fields.find((f) => f.id === event.target.value);
            if (next) onChange(newCondition(next.field));
          }}
        >
          {field ? null : (
            <option value="__stale__" disabled>
              {copy.stale.unknownField}
            </option>
          )}
          {fields.map((f) => (
            <option key={f.id} value={f.id}>
              {f.name}
            </option>
          ))}
        </select>
        {field?.field.fieldType === "checkbox" ? (
          <span
            className={cn(
              "col-span-2 max-sm:col-span-1",
              compact && "col-span-3",
            )}
          >
            <Segmented
              label={copy.filter.value}
              value={condition.operator}
              options={[
                ["is_true", copy.filter.checked],
                ["is_false", copy.filter.unchecked],
              ]}
              onChange={(value) =>
                onChange(withOperator(condition, value as ConditionOperator))
              }
            />
          </span>
        ) : (
          <>
            {field?.id === "$overdue" ? (
              <span
                className={cn(
                  "text-compact text-muted-foreground",
                  compact && "col-span-3",
                )}
              >
                {copy.filter.overdueFixed}
              </span>
            ) : (
              <select
                aria-label={`${copy.filter.operator} (${label})`}
                className={cn(controlClass, compact && "col-span-3 w-full")}
                value={condition.operator}
                disabled={!field}
                onChange={(event) =>
                  onChange(
                    withOperator(
                      condition,
                      event.target.value as ConditionOperator,
                    ),
                  )
                }
              >
                {operators.map((operator) => (
                  <option key={operator} value={operator}>
                    {copy.operators[operator as keyof FiltersCopy["operators"]]}
                  </option>
                ))}
              </select>
            )}
            <div
              className={cn("min-w-0", compact && "col-span-3 empty:hidden")}
            >
              {field && field.id !== "$overdue" ? (
                <ValueEditor
                  copy={copy}
                  field={field}
                  condition={condition}
                  label={label}
                  now={now}
                  timeZone={timeZone}
                  timeZoneNote={timeZoneNote}
                  people={people}
                  invalid={issue !== null}
                  compact={compact}
                  onChange={onChange}
                />
              ) : null}
            </div>
          </>
        )}
        <button
          type="button"
          aria-label={fillFiltersCopy(copy.filter.removeCondition, {
            n: index + 1,
          })}
          onClick={onRemove}
          className={cn(
            "mt-0.5 grid size-7.5 cursor-pointer place-items-center rounded-[7px] border-0 bg-transparent text-muted-foreground hover:bg-state-hover-strong focus-visible:outline-2 focus-visible:outline-ring",
            compact && "col-start-3 row-start-1 mt-0 size-9",
          )}
        >
          <X className="size-4" aria-hidden="true" />
        </button>
      </div>
      {issue ? (
        <p
          role="alert"
          className={cn(
            "mt-1.5 pl-13 text-[12.5px] text-destructive",
            compact && "pl-0",
          )}
        >
          {issue === "range" ? copy.filter.rangeError : copy.filter.incomplete}
        </p>
      ) : null}
    </div>
  );
}

function Segmented({
  label,
  value,
  options,
  onChange,
}: {
  label: string;
  value: string;
  options: readonly (readonly [string, string])[];
  onChange: (value: string) => void;
}) {
  return (
    <div
      role="group"
      aria-label={label}
      className="inline-flex gap-0.5 rounded-lg border border-line-strong bg-state-hover p-0.5"
    >
      {options.map(([id, text]) => (
        <button
          key={id}
          type="button"
          aria-pressed={value === id}
          onClick={() => onChange(id)}
          className={cn(
            "inline-flex h-7 cursor-pointer items-center gap-1 rounded-md border-0 bg-transparent px-3 text-[12.5px] font-semibold text-muted-foreground focus-visible:outline-2 focus-visible:outline-ring",
            value === id &&
              "bg-popover font-bold text-foreground ring-1 ring-line-strong",
          )}
        >
          {text}
        </button>
      ))}
    </div>
  );
}

const DATE_PRESETS: readonly DatePreset[] = [
  "today",
  "week",
  "month",
  "next7",
  "overdue",
];

function ValueEditor({
  copy,
  field,
  condition,
  label,
  now,
  timeZone,
  timeZoneNote,
  people,
  invalid,
  compact,
  onChange,
}: {
  compact?: boolean | undefined;
  people: readonly { id: string; name: string }[];
  copy: FiltersCopy;
  field: FilterField;
  condition: FilterCondition;
  label: string;
  now: Date;
  /** The board's IANA zone: presets pick "today" in it. */
  timeZone?: string;
  /** The caption under the date row: which zone the days are judged in. */
  timeZoneNote?: string;
  invalid: boolean;
  onChange: (condition: FilterCondition) => void;
}): ReactNode {
  const type = field.field.fieldType;
  const { operator, values } = condition;
  const valueLabelText = `${copy.filter.value} (${label})`;
  if (!operatorTakesValue(operator)) return null;

  if (type === "date") {
    const input = (id: string, at: number, text: string) => (
      <label className="relative inline-flex min-w-0 flex-1 items-center">
        <Calendar
          className="pointer-events-none absolute left-2 z-10 size-4 text-muted-foreground"
          aria-hidden="true"
        />
        <input
          id={id}
          type="date"
          aria-label={`${text} (${label})`}
          aria-invalid={invalid || undefined}
          value={values[at] ?? ""}
          onChange={(event) => {
            const next = [...values];
            next[at] = event.target.value;
            onChange({
              ...condition,
              values: operator === "between" ? next : [next[0] ?? ""],
            });
          }}
          className={cn(
            controlClass,
            "relative w-full pr-0.5 pl-7 text-left [&::-webkit-datetime-edit]:p-0 [&::-webkit-datetime-edit-text]:p-0 [&::-webkit-datetime-edit-month-field]:p-0 [&::-webkit-datetime-edit-day-field]:p-0 [&::-webkit-datetime-edit-year-field]:p-0 [&::-webkit-datetime-edit-fields-wrapper]:p-0 [&::-webkit-calendar-picker-indicator]:absolute [&::-webkit-calendar-picker-indicator]:inset-0 [&::-webkit-calendar-picker-indicator]:size-full [&::-webkit-calendar-picker-indicator]:opacity-0",
            invalid && "border-destructive",
          )}
        />
      </label>
    );
    return (
      <div className="flex flex-col gap-2">
        {operator === "between" ? (
          // The presets grow in under the row while it is being edited; the canvas draws the quiet row without them.
          <div
            role="group"
            aria-label={copy.filter.presets.week}
            className="hidden flex-wrap gap-1.5 group-focus-within/row:flex"
          >
            {DATE_PRESETS.map((preset) => (
              <button
                key={preset}
                type="button"
                onClick={() =>
                  onChange(presetCondition(field.id, preset, now, timeZone))
                }
                className="h-7 cursor-pointer rounded-full border border-line-strong bg-popover px-2.5 text-xs font-medium text-muted-foreground hover:bg-state-hover-strong focus-visible:outline-2 focus-visible:outline-ring"
              >
                {copy.filter.presets[preset]}
              </button>
            ))}
          </div>
        ) : null}
        <div
          className={cn(
            "flex items-center gap-1.5",
            compact && "flex-col items-stretch",
          )}
        >
          {input(`${condition.fieldId}-from`, 0, copy.filter.from)}
          {operator === "between" ? (
            <>
              <span
                aria-hidden="true"
                className={cn(
                  "text-[14px] text-muted-foreground",
                  compact && "sr-only",
                )}
              >
                {copy.filter.rangeTo}
              </span>
              {input(`${condition.fieldId}-to`, 1, copy.filter.to)}
            </>
          ) : null}
        </div>
        {timeZoneNote ? (
          // Under the row, growing in with the presets: the canvas draws the quiet row without it.
          <p
            data-f="tz-note"
            className="m-0 hidden text-xs leading-[1.45] text-muted-foreground group-focus-within/row:block"
          >
            {timeZoneNote}
          </p>
        ) : null}
      </div>
    );
  }

  const options: { id: string; name: string; hint?: string }[] | null =
    type === "select" || type === "multi_select"
      ? field.field.options.map((o) => ({ id: o.id, name: o.label }))
      : type === "person"
        ? [
            {
              id: NONE_VALUE,
              name: copy.filter.unassigned,
              hint: copy.filter.unassignedHint,
            },
            ...people,
          ]
        : null;

  if (options && (operator === "has_any" || operator === "has_all")) {
    const toggle = (id: string) =>
      onChange({
        ...condition,
        values: values.includes(id)
          ? values.filter((v) => v !== id)
          : [...values, id],
      });
    return options.length === 0 ? (
      <p className="text-compact text-muted-foreground">
        {copy.filter.noOptions}
      </p>
    ) : (
      <ValuePicker
        label={valueLabelText}
        placeholder={copy.filter.valuePlaceholder}
        options={options}
        values={values}
        person={type === "person"}
        compact={compact}
        onToggle={toggle}
      />
    );
  }

  if (type === "select" && options) {
    return (
      <select
        aria-label={valueLabelText}
        className={cn(controlClass, "w-full")}
        value={values[0] ?? ""}
        onChange={(event) =>
          onChange({ ...condition, values: [event.target.value] })
        }
      >
        <option value="" disabled>
          {copy.filter.valuePlaceholder}
        </option>
        {options.map((option) => (
          <option key={option.id} value={option.id}>
            {option.name}
          </option>
        ))}
      </select>
    );
  }

  const numeric =
    type === "number" ||
    ((type === "formula" || type === "rollup") &&
      operator !== "contains" &&
      operator !== "equals");
  return (
    <input
      type={numeric ? "number" : "text"}
      inputMode={numeric ? "decimal" : undefined}
      aria-label={valueLabelText}
      placeholder={
        numeric ? copy.filter.numberPlaceholder : copy.filter.valuePlaceholder
      }
      value={values[0] ?? ""}
      onChange={(event) =>
        onChange({ ...condition, values: [event.target.value] })
      }
      aria-invalid={invalid || undefined}
      className={cn(controlClass, "w-full", invalid && "border-destructive")}
    />
  );
}

interface PickOption {
  id: string;
  name: string;
  hint?: string;
}

/** The round mark of a person: the first letter, or a dashed person for "Chưa giao" (BC filter card). */
function PersonMark({ option }: { option: PickOption }) {
  return option.id === NONE_VALUE ? (
    <span
      aria-hidden="true"
      className="grid size-5.5 shrink-0 place-items-center rounded-full border-[1.5px] border-dashed border-line-strong text-meta-foreground"
    >
      <User className="size-3" />
    </span>
  ) : (
    <span
      aria-hidden="true"
      className="grid size-5.5 shrink-0 place-items-center rounded-full bg-state-selected text-[11px] font-bold text-state-selected-foreground"
    >
      {option.name.trim().charAt(0).toUpperCase()}
    </span>
  );
}

/**
 * The chosen values of a "một trong" or "tất cả" condition as chips in the row; the whole area opens the list of
 * choices (a listbox with a tick per value) so the row stays one line tall.
 */
function ValuePicker({
  label,
  placeholder,
  options,
  values,
  person,
  compact,
  onToggle,
}: {
  compact?: boolean | undefined;
  label: string;
  placeholder: string;
  options: readonly PickOption[];
  values: readonly string[];
  person: boolean;
  onToggle: (id: string) => void;
}) {
  const chosen = options.filter((option) => values.includes(option.id));
  return (
    <Popover>
      <PopoverTrigger asChild>
        <button
          type="button"
          aria-label={label}
          aria-haspopup="listbox"
          className={cn(
            "flex min-h-8.5 w-full cursor-pointer flex-wrap items-center gap-1.5 rounded-[7px] border-0 bg-transparent p-0 text-left focus-visible:outline-2 focus-visible:outline-ring",
            // The sheet's card is as tall as its chips.
            compact && chosen.length > 0 && "min-h-0",
            chosen.length === 0 &&
              "h-8.5 border border-line-strong bg-popover px-2.5 text-[13.5px] text-muted-foreground",
          )}
        >
          {chosen.length === 0
            ? placeholder
            : chosen.map((option) => (
                <span
                  key={option.id}
                  className="inline-flex h-6.5 items-center gap-1.5 rounded-md bg-state-hover-strong px-2 text-[11.5px] font-semibold text-foreground"
                >
                  {person ? <PersonMark option={option} /> : null}
                  {option.name}
                </span>
              ))}
        </button>
      </PopoverTrigger>
      <PopoverContent
        align="start"
        className="w-[min(20rem,calc(100vw-1.5rem))] gap-0 rounded-[12px] border border-line p-1.5"
      >
        <ul
          role="listbox"
          aria-multiselectable="true"
          aria-label={label}
          className="m-0 flex max-h-56 list-none flex-col gap-0.5 overflow-auto p-0"
        >
          {options.map((option) => {
            const on = values.includes(option.id);
            return (
              <li
                key={option.id}
                role="option"
                aria-selected={on}
                tabIndex={0}
                onClick={() => onToggle(option.id)}
                onKeyDown={(event) => {
                  if (event.key === " " || event.key === "Enter") {
                    event.preventDefault();
                    onToggle(option.id);
                  }
                }}
                className={cn(
                  "flex min-h-9 cursor-pointer items-center gap-2 rounded-md px-2 text-[13.5px] hover:bg-state-hover focus-visible:outline-2 focus-visible:outline-ring",
                  on && "bg-state-selected",
                )}
              >
                <span
                  aria-hidden="true"
                  className={cn(
                    "grid size-4 shrink-0 place-items-center rounded-[4px] border-[1.5px] border-line-strong",
                    on && "border-primary bg-primary text-primary-foreground",
                  )}
                >
                  {on ? <Check className="size-3" /> : null}
                </span>
                {person ? <PersonMark option={option} /> : null}
                <span className="min-w-0 flex-1 truncate">{option.name}</span>
                {option.hint ? (
                  <span className="text-xs text-muted-foreground">
                    {option.hint}
                  </span>
                ) : null}
              </li>
            );
          })}
        </ul>
      </PopoverContent>
    </Popover>
  );
}
