import { useState, type ReactNode } from "react";
import { Calendar, Check, Filter, Plus, X } from "lucide-react";
import {
  Button,
  cn,
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@notrelix/ui-web";
import {
  MAX_CONDITIONS,
  MAX_VALUE_LENGTH,
  NONE_VALUE,
  conditionIssue,
  newCondition,
  operatorTakesValue,
  operatorsFor,
  presetCondition,
  resolveOperand,
  withOperator,
  type CompoundFilter,
  type ConditionOperator,
  type DatePreset,
  type FilterCondition,
  type FilterField,
} from "@notrelix/work-management-core";
import { kanbanPillClass } from "../views/kanban/kanban-chrome";
import { fillFiltersCopy, type FiltersCopy } from "./filters.copy";

const controlClass =
  "h-8.5 min-w-0 rounded-[7px] border border-line-strong bg-popover px-2.5 text-compact text-foreground focus-visible:outline-2 focus-visible:outline-ring";
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
  /** The number the trigger shows: conditions that apply. */
  activeCount: number;
  /** Opens the dialog (controlled by the host so "Sửa điều kiện" can open it). */
  open?: boolean;
  onOpenChange?: (open: boolean) => void;
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
        className="max-h-[var(--radix-popover-content-available-height)] w-[min(46rem,calc(100vw-1.5rem))] gap-3 overflow-y-auto rounded-[12px] border border-line p-4"
      >
        <BuilderBody {...props} filter={filter} onChange={onChange} />
        <div className="flex items-center gap-2 border-t border-line pt-3">
          <span
            role="status"
            aria-live="polite"
            className="flex-1 text-compact text-muted-foreground"
          >
            {fillFiltersCopy(copy.filter.count, {
              shown: props.counts.shown,
              total: props.counts.total,
            })}
          </span>
          <Button
            variant="ghost"
            size="sm"
            className="h-7 text-compact"
            onClick={() => onChange({ ...filter, conditions: [] })}
          >
            {copy.filter.clearAll}
          </Button>
          <Button
            size="sm"
            className="h-7 text-compact"
            onClick={() => setOpen(false)}
          >
            {copy.filter.done}
          </Button>
        </div>
      </PopoverContent>
    </Popover>
  );
}

function BuilderBody({
  copy,
  fields,
  filter,
  onChange,
  now,
  people,
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
      <div className="flex items-center gap-3">
        <h2 className="flex-1 text-base font-semibold">{copy.filter.title}</h2>
        <div
          role="group"
          aria-label={copy.filter.matchLabel}
          className="inline-flex gap-0.5 rounded-lg border border-line-strong bg-state-hover p-0.5"
        >
          {(["and", "or"] as const).map((join) => (
            <button
              key={join}
              type="button"
              aria-pressed={filter.join === join}
              onClick={() => onChange({ ...filter, join })}
              className={cn(
                "inline-flex h-7 cursor-pointer items-center gap-1 rounded-md border-0 bg-transparent px-3 text-compact text-muted-foreground focus-visible:outline-2 focus-visible:outline-ring",
                filter.join === join &&
                  "bg-popover font-bold text-foreground ring-1 ring-line-strong",
              )}
            >
              {filter.join === join ? (
                <Check className="size-3.5" aria-hidden="true" />
              ) : null}
              {join === "and" ? copy.filter.matchAll : copy.filter.matchAny}
            </button>
          ))}
        </div>
      </div>
      <div className="flex flex-col gap-2">
        {filter.conditions.map((condition, index) => (
          <ConditionRow
            key={index}
            index={index}
            copy={copy}
            fields={fields}
            condition={condition}
            join={filter.join}
            now={now}
            people={people}
            onChange={(next) => update(index, next)}
            onRemove={() => remove(index)}
          />
        ))}
      </div>
      <Button
        variant="ghost"
        size="sm"
        className="h-7 self-start text-compact text-muted-foreground"
        disabled={!first || filter.conditions.length >= MAX_CONDITIONS}
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
  people,
  onChange,
  onRemove,
}: {
  people: readonly { id: string; name: string }[];
  index: number;
  copy: FiltersCopy;
  fields: readonly FilterField[];
  condition: FilterCondition;
  join: "and" | "or";
  now: Date;
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
    <div role="group" aria-label={label} className="flex flex-col gap-1">
      <div className="grid grid-cols-[2.75rem_minmax(0,11.5rem)_minmax(0,10.5rem)_minmax(0,1fr)_2rem] items-center gap-2 max-sm:grid-cols-1">
        <span className="text-right text-xs font-semibold text-muted-foreground max-sm:text-left">
          {lead}
        </span>
        <select
          aria-label={`${copy.filter.field} (${label})`}
          className={controlClass}
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
          <span className="col-span-2 max-sm:col-span-1">
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
              <span className="text-compact text-muted-foreground">
                {copy.filter.overdueFixed}
              </span>
            ) : (
              <select
                aria-label={`${copy.filter.operator} (${label})`}
                className={controlClass}
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
            <div className="min-w-0">
              {field && field.id !== "$overdue" ? (
                <ValueEditor
                  copy={copy}
                  field={field}
                  condition={condition}
                  label={label}
                  now={now}
                  people={people}
                  invalid={issue === "range"}
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
          className="grid size-8 cursor-pointer place-items-center rounded-md border-0 bg-transparent text-muted-foreground hover:bg-state-hover-strong focus-visible:outline-2 focus-visible:outline-ring"
        >
          <X className="size-4" aria-hidden="true" />
        </button>
      </div>
      {issue ? (
        <p role="alert" className="pl-13 text-xs text-destructive">
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
            "inline-flex h-7 cursor-pointer items-center gap-1 rounded-md border-0 bg-transparent px-3 text-compact text-muted-foreground focus-visible:outline-2 focus-visible:outline-ring",
            value === id &&
              "bg-popover font-bold text-foreground ring-1 ring-line-strong",
          )}
        >
          {value === id ? (
            <Check className="size-3.5" aria-hidden="true" />
          ) : null}
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
  people,
  invalid,
  onChange,
}: {
  people: readonly { id: string; name: string }[];
  copy: FiltersCopy;
  field: FilterField;
  condition: FilterCondition;
  label: string;
  now: Date;
  invalid: boolean;
  onChange: (condition: FilterCondition) => void;
}): ReactNode {
  const type = field.field.fieldType;
  const { operator, values } = condition;
  const valueLabelText = `${copy.filter.value} (${label})`;
  if (!operatorTakesValue(operator)) return null;

  if (type === "date") {
    const input = (id: string, at: number, text: string) => (
      <label className="relative inline-flex items-center">
        <Calendar
          className="pointer-events-none absolute left-2 size-4 text-muted-foreground"
          aria-hidden="true"
        />
        <input
          id={id}
          type="date"
          aria-label={`${text} (${label})`}
          aria-invalid={invalid || undefined}
          value={resolveOperand(values[at] ?? "", { now })}
          onChange={(event) => {
            const next = values.map((value) => resolveOperand(value, { now }));
            next[at] = event.target.value;
            onChange({
              ...condition,
              values: operator === "between" ? next : [next[0] ?? ""],
            });
          }}
          className={cn(
            controlClass,
            "w-40 pl-8",
            invalid && "border-destructive",
          )}
        />
      </label>
    );
    return (
      <div className="flex flex-col gap-2">
        {operator === "between" ? (
          <div
            role="group"
            aria-label={copy.filter.presets.week}
            className="flex flex-wrap gap-1.5"
          >
            {DATE_PRESETS.map((preset) => (
              <button
                key={preset}
                type="button"
                onClick={() => onChange(presetCondition(field.id, preset))}
                className="h-7 cursor-pointer rounded-full border border-line-strong bg-popover px-2.5 text-xs font-medium text-muted-foreground hover:bg-state-hover-strong focus-visible:outline-2 focus-visible:outline-ring"
              >
                {copy.filter.presets[preset]}
              </button>
            ))}
          </div>
        ) : null}
        <div className="flex flex-wrap items-center gap-1.5">
          {input(`${condition.fieldId}-from`, 0, copy.filter.from)}
          {operator === "between" ? (
            <>
              <span aria-hidden="true" className="text-muted-foreground">
                {copy.filter.rangeTo}
              </span>
              {input(`${condition.fieldId}-to`, 1, copy.filter.to)}
            </>
          ) : null}
        </div>
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
      <ul
        role="listbox"
        aria-multiselectable="true"
        aria-label={valueLabelText}
        className="m-0 flex max-h-44 list-none flex-col gap-0.5 overflow-auto rounded-lg border border-line p-1"
      >
        {options.map((option) => {
          const on = values.includes(option.id);
          return (
            <li
              key={option.id}
              role="option"
              aria-selected={on}
              tabIndex={0}
              onClick={() => toggle(option.id)}
              onKeyDown={(event) => {
                if (event.key === " " || event.key === "Enter") {
                  event.preventDefault();
                  toggle(option.id);
                }
              }}
              className={cn(
                "flex min-h-8 cursor-pointer items-center gap-2 rounded-md px-2 text-compact hover:bg-state-hover focus-visible:outline-2 focus-visible:outline-ring",
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
      maxLength={MAX_VALUE_LENGTH}
      placeholder={
        numeric ? copy.filter.numberPlaceholder : copy.filter.valuePlaceholder
      }
      value={values[0] ?? ""}
      onChange={(event) =>
        onChange({ ...condition, values: [event.target.value] })
      }
      className={cn(controlClass, "w-full")}
    />
  );
}
