import { useState } from "react";
import { ArrowUpDown, ChevronDown, GripVertical, Plus, X } from "lucide-react";
import {
  Button,
  cn,
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@notrelix/ui-web";
import {
  MAX_SORT_LEVELS,
  type FilterField,
  type SortLevel,
} from "@notrelix/work-management-core";
import { kanbanPillClass } from "../views/kanban/kanban-chrome";
import { fillFiltersCopy, type FiltersCopy } from "./filters.copy";
import { activePillClass } from "./filter-builder";
import { fieldTypeIcon, fieldTypeKind } from "./field-type-meta";

export interface SortMenuProps {
  copy: FiltersCopy;
  fields: readonly FilterField[];
  levels: readonly SortLevel[];
  onChange: (levels: SortLevel[]) => void;
  /** The shared view's sort as text, shown while the member has none of their own. */
  fallbackSummary?: string | undefined;
}

/** "Sắp xếp: Hạn ↑, Ưu tiên ↓" for the pill. */
export function sortSummary(
  copy: FiltersCopy,
  fields: readonly FilterField[],
  levels: readonly SortLevel[],
): string {
  return levels
    .flatMap((level) => {
      const field = fields.find((f) => f.id === level.fieldId);
      return field
        ? [
            fillFiltersCopy(
              level.direction === "asc"
                ? copy.sort.summaryAsc
                : copy.sort.summaryDesc,
              { field: field.name },
            ),
          ]
        : [];
    })
    .join(", ");
}

/**
 * The "Sắp xếp" pill and its dialog: up to three levels of field and
 * direction, the first level winning. The null rule is the field type's own
 * (empty last ascending, first descending). Changes the member's own view only.
 */
export function SortMenu({
  copy,
  fields,
  levels,
  onChange,
  fallbackSummary,
}: SortMenuProps) {
  const summary = sortSummary(copy, fields, levels);
  const used = new Set(levels.map((level) => level.fieldId));
  const next = fields.find((field) => !used.has(field.id));
  const update = (index: number, patch: Partial<SortLevel>) =>
    onChange(levels.map((l, i) => (i === index ? { ...l, ...patch } : l)));
  const [dragging, setDragging] = useState<number | null>(null);
  const move = (from: number, to: number) => {
    if (to < 0 || to >= levels.length || from === to) return;
    const next = [...levels];
    const [level] = next.splice(from, 1);
    if (level) next.splice(to, 0, level);
    onChange(next);
  };
  return (
    <Popover>
      <PopoverTrigger asChild>
        <Button
          variant="ghost"
          size="sm"
          className={cn(kanbanPillClass, summary && activePillClass)}
          aria-haspopup="dialog"
        >
          <ArrowUpDown className="size-4" aria-hidden="true" />
          <span
            className={
              summary || fallbackSummary ? "@max-[1100px]/bar:sr-only" : ""
            }
          >
            {copy.sort.trigger}
            {summary || fallbackSummary ? ": " : ""}
          </span>
          {summary || fallbackSummary ? (
            <>
              <b className="max-w-56 truncate font-semibold @max-[1100px]/bar:max-w-24 @max-[820px]/bar:max-w-14">
                {summary || fallbackSummary}
              </b>
            </>
          ) : null}
        </Button>
      </PopoverTrigger>
      <PopoverContent
        align="start"
        aria-label={copy.sort.title}
        className="w-[min(28.75rem,calc(100vw-1.5rem))] gap-3 rounded-[12px] border border-line p-4"
      >
        <h2 className="text-base font-semibold leading-[1.45]">
          {copy.sort.title}
        </h2>
        {levels.map((level, index) => {
          const n = index + 1;
          const current = fields.find((field) => field.id === level.fieldId);
          return (
            <div
              key={`${level.fieldId}-${index}`}
              role="group"
              aria-label={fillFiltersCopy(copy.sort.level, { n })}
              draggable
              onDragStart={() => setDragging(index)}
              onDragOver={(event) => event.preventDefault()}
              onDrop={() => {
                if (dragging !== null) move(dragging, index);
                setDragging(null);
              }}
              onDragEnd={() => setDragging(null)}
              className="grid grid-cols-[1.5rem_minmax(0,1fr)_auto_1.875rem] items-center gap-2"
            >
              <button
                type="button"
                aria-label={fillFiltersCopy(copy.sort.reorder, { n })}
                onKeyDown={(event) => {
                  if (event.key === "ArrowUp") {
                    event.preventDefault();
                    move(index, index - 1);
                  } else if (event.key === "ArrowDown") {
                    event.preventDefault();
                    move(index, index + 1);
                  }
                }}
                className="grid h-4 w-6 cursor-grab place-items-center border-0 bg-transparent p-0 text-muted-foreground focus-visible:outline-2 focus-visible:outline-ring"
              >
                <GripVertical className="size-4" aria-hidden="true" />
              </button>
              <SortFieldPicker
                copy={copy}
                fields={fields}
                used={used}
                value={level.fieldId}
                label={fillFiltersCopy(copy.sort.field, { n })}
                name={current?.name ?? ""}
                onPick={(fieldId) => update(index, { fieldId })}
              />
              <div
                role="group"
                aria-label={copy.sort.direction}
                className="inline-flex gap-0.5 rounded-lg border border-line-strong bg-state-hover p-0.5"
              >
                {(["asc", "desc"] as const).map((direction) => (
                  <button
                    key={direction}
                    type="button"
                    aria-pressed={level.direction === direction}
                    onClick={() => update(index, { direction })}
                    className={cn(
                      "inline-flex h-7 cursor-pointer items-center rounded-md border-0 bg-transparent px-3 text-[12.5px] font-semibold text-muted-foreground focus-visible:outline-2 focus-visible:outline-ring",
                      level.direction === direction &&
                        "bg-popover font-bold text-foreground ring-1 ring-line-strong",
                    )}
                  >
                    {direction === "asc" ? copy.sort.asc : copy.sort.desc}
                  </button>
                ))}
              </div>
              <button
                type="button"
                aria-label={fillFiltersCopy(copy.sort.remove, { n })}
                onClick={() => onChange(levels.filter((_, i) => i !== index))}
                className="grid size-7.5 cursor-pointer place-items-center rounded-[7px] border-0 bg-transparent text-muted-foreground hover:bg-state-hover-strong focus-visible:outline-2 focus-visible:outline-ring"
              >
                <X className="size-4" aria-hidden="true" />
              </button>
            </div>
          );
        })}
        <Button
          variant="ghost"
          size="sm"
          className="h-7 gap-1.5 self-start border border-transparent px-2.5 text-[12.5px] font-medium text-muted-foreground"
          disabled={!next || levels.length >= MAX_SORT_LEVELS}
          onClick={() =>
            next &&
            onChange([...levels, { fieldId: next.id, direction: "asc" }])
          }
        >
          <Plus className="size-4" aria-hidden="true" />
          {copy.sort.add}
        </Button>
        <p className="text-[12.5px] leading-[1.45] text-muted-foreground">
          {copy.sort.note}
        </p>
      </PopoverContent>
    </Popover>
  );
}

/** The field of one sort level: a 34 px button that opens the list of every field with its type word. */
function SortFieldPicker({
  copy,
  fields,
  used,
  value,
  label,
  name,
  onPick,
}: {
  copy: FiltersCopy;
  fields: readonly FilterField[];
  used: ReadonlySet<string>;
  value: string;
  label: string;
  name: string;
  onPick: (fieldId: string) => void;
}) {
  const [open, setOpen] = useState(false);
  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <button
          type="button"
          aria-label={label}
          aria-haspopup="listbox"
          className="flex h-8.5 min-w-0 cursor-pointer items-center justify-between gap-1.5 rounded-[7px] border border-line-strong bg-popover px-2.5 text-[13.5px] text-foreground focus-visible:outline-2 focus-visible:outline-ring"
        >
          <span className="truncate">{name}</span>
          <ChevronDown className="size-4 shrink-0" aria-hidden="true" />
        </button>
      </PopoverTrigger>
      <PopoverContent
        align="start"
        className="w-[min(15.625rem,calc(100vw-1.5rem))] gap-0 rounded-[12px] border border-line p-1.5"
      >
        <ul
          role="listbox"
          aria-label={copy.sort.fieldHint}
          className="m-0 flex max-h-80 list-none flex-col gap-0.5 overflow-auto p-0"
        >
          {fields.map((field) => {
            const kind = fieldTypeKind(field);
            const Icon = fieldTypeIcon[kind];
            const taken = used.has(field.id) && field.id !== value;
            return (
              <li
                key={field.id}
                role="option"
                aria-selected={field.id === value}
                aria-disabled={taken || undefined}
                tabIndex={taken ? -1 : 0}
                onClick={() => {
                  if (taken) return;
                  onPick(field.id);
                  setOpen(false);
                }}
                onKeyDown={(event) => {
                  if (taken || (event.key !== " " && event.key !== "Enter"))
                    return;
                  event.preventDefault();
                  onPick(field.id);
                  setOpen(false);
                }}
                className={cn(
                  "flex min-h-9 cursor-pointer items-center gap-2.5 rounded-lg px-2.5 py-1 text-[13.5px] hover:bg-state-hover focus-visible:outline-2 focus-visible:outline-ring",
                  field.id === value && "bg-state-selected",
                  taken && "cursor-not-allowed text-muted-foreground",
                )}
              >
                <Icon className="size-4 shrink-0" aria-hidden="true" />
                <span className="min-w-0 flex-1 truncate">{field.name}</span>
                <small className="whitespace-nowrap text-xs text-muted-foreground">
                  {copy.group[kind]}
                </small>
              </li>
            );
          })}
        </ul>
      </PopoverContent>
    </Popover>
  );
}
