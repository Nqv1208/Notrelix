import { ArrowUpDown, Check, Plus, X } from "lucide-react";
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

export interface SortMenuProps {
  copy: FiltersCopy;
  fields: readonly FilterField[];
  levels: readonly SortLevel[];
  onChange: (levels: SortLevel[]) => void;
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
export function SortMenu({ copy, fields, levels, onChange }: SortMenuProps) {
  const summary = sortSummary(copy, fields, levels);
  const used = new Set(levels.map((level) => level.fieldId));
  const next = fields.find((field) => !used.has(field.id));
  const update = (index: number, patch: Partial<SortLevel>) =>
    onChange(levels.map((l, i) => (i === index ? { ...l, ...patch } : l)));
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
          {copy.sort.trigger}
          {summary ? (
            <>
              {": "}
              <b className="max-w-56 truncate font-semibold">{summary}</b>
            </>
          ) : null}
        </Button>
      </PopoverTrigger>
      <PopoverContent
        align="start"
        aria-label={copy.sort.title}
        className="w-[min(28rem,calc(100vw-1.5rem))] gap-3 rounded-[12px] border border-line p-4"
      >
        <h2 className="text-base font-semibold">{copy.sort.title}</h2>
        {levels.map((level, index) => {
          const n = index + 1;
          return (
            <div
              key={`${level.fieldId}-${index}`}
              role="group"
              aria-label={fillFiltersCopy(copy.sort.level, { n })}
              className="grid grid-cols-[minmax(0,1fr)_auto_2rem] items-center gap-2"
            >
              <select
                aria-label={fillFiltersCopy(copy.sort.field, { n })}
                value={level.fieldId}
                onChange={(event) =>
                  update(index, { fieldId: event.target.value })
                }
                className="h-8.5 min-w-0 rounded-[7px] border border-line-strong bg-popover px-2.5 text-compact"
              >
                {fields.map((field) => (
                  <option
                    key={field.id}
                    value={field.id}
                    disabled={used.has(field.id) && field.id !== level.fieldId}
                  >
                    {field.name}
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
                    aria-pressed={level.direction === direction}
                    onClick={() => update(index, { direction })}
                    className={cn(
                      "inline-flex h-7 cursor-pointer items-center gap-1 rounded-md border-0 bg-transparent px-2.5 text-compact text-muted-foreground focus-visible:outline-2 focus-visible:outline-ring",
                      level.direction === direction &&
                        "bg-popover font-bold text-foreground ring-1 ring-line-strong",
                    )}
                  >
                    {level.direction === direction ? (
                      <Check className="size-3.5" aria-hidden="true" />
                    ) : null}
                    {direction === "asc" ? copy.sort.asc : copy.sort.desc}
                  </button>
                ))}
              </div>
              <button
                type="button"
                aria-label={fillFiltersCopy(copy.sort.remove, { n })}
                onClick={() => onChange(levels.filter((_, i) => i !== index))}
                className="grid size-8 cursor-pointer place-items-center rounded-md border-0 bg-transparent text-muted-foreground hover:bg-state-hover-strong focus-visible:outline-2 focus-visible:outline-ring"
              >
                <X className="size-4" aria-hidden="true" />
              </button>
            </div>
          );
        })}
        <Button
          variant="ghost"
          size="sm"
          className="h-7 self-start text-compact text-muted-foreground"
          disabled={!next || levels.length >= MAX_SORT_LEVELS}
          onClick={() =>
            next &&
            onChange([...levels, { fieldId: next.id, direction: "asc" }])
          }
        >
          <Plus className="size-4" aria-hidden="true" />
          {copy.sort.add}
        </Button>
        <p className="text-xs text-muted-foreground">{copy.sort.note}</p>
      </PopoverContent>
    </Popover>
  );
}
