import { useRef, type KeyboardEvent, type ReactNode } from "react";
import {
  CalendarDays,
  ChevronDown,
  ChevronLeft,
  ChevronRight,
  Filter,
  Plus,
} from "lucide-react";
import {
  Button,
  cn,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuRadioGroup,
  DropdownMenuRadioItem,
  DropdownMenuTrigger,
} from "@notrelix/ui-web";
import { type KanbanFilterState } from "../kanban/kanban-filter";
import {
  KanbanFilterMenuContent,
  type KanbanToolbarFilterOptions,
} from "../kanban/kanban-filter-menu";
import {
  kanbanPillClass,
  kanbanPrimaryButtonClass,
} from "../kanban/kanban-chrome";
import { calendarOutlineButtonClass } from "./calendar-chrome";
import { filterSegments } from "../kanban/kanban-toolbar";
import type { CalendarCopy } from "./calendar.copy";
import type { CalendarDateBasis, CalendarRange } from "./calendar-model";

const RANGES = ["month", "week", "day"] as const;
const BASES = ["span", "due"] as const;

const ICON_BUTTON_CLASS =
  "size-8 rounded-[7px] border-line-strong bg-popover p-0 text-muted-foreground shadow-none hover:bg-state-hover-strong dark:border-line-strong dark:bg-card dark:hover:bg-state-hover-strong";

export interface CalendarToolbarProps {
  copy: CalendarCopy;
  /** The range heading ("Tháng 10, 2026"). */
  heading: string;
  range: CalendarRange;
  onRangeChange: (range: CalendarRange) => void;
  onToday: () => void;
  onStep: (direction: -1 | 1) => void;
  dateBasis: CalendarDateBasis;
  onDateBasisChange: (basis: CalendarDateBasis) => void;
  filter: KanbanFilterState;
  filterOptions: KanbanToolbarFilterOptions;
  onFilterChange: (filter: KanbanFilterState) => void;
  /** The time zone line ("Giờ Hà Nội (GMT+7)"). */
  zoneLabel: string;
  /** False for a viewer who cannot add items: the add button is not shown. */
  canAdd: boolean;
  onAddItem: () => void;
  /** The saved-filter controls, after the filter pill. */
  savedFilters?: ReactNode;
}

/**
 * The Calendar bar: today, previous and next, the range heading, the
 * month/week/day switch, the date basis and filter pills, the time zone and
 * "Thêm công việc". It wraps onto a second line when the width runs out.
 */
export function CalendarToolbar({
  copy,
  heading,
  range,
  onRangeChange,
  onToday,
  onStep,
  dateBasis,
  onDateBasisChange,
  filter,
  filterOptions,
  onFilterChange,
  zoneLabel,
  canAdd,
  onAddItem,
  savedFilters,
}: CalendarToolbarProps) {
  const segments = filterSegments(copy.filter, filter, filterOptions);
  const filtered = segments.length > 0;
  const rangeButtons = useRef<Array<HTMLButtonElement | null>>([]);

  const onRangeKeyDown = (event: KeyboardEvent<HTMLButtonElement>) => {
    const step =
      event.key === "ArrowRight" || event.key === "ArrowDown"
        ? 1
        : event.key === "ArrowLeft" || event.key === "ArrowUp"
          ? -1
          : 0;
    if (step === 0) return;
    event.preventDefault();
    const next =
      RANGES[(RANGES.indexOf(range) + step + RANGES.length) % RANGES.length];
    if (!next) return;
    onRangeChange(next);
    rangeButtons.current[RANGES.indexOf(next)]?.focus();
  };

  return (
    <div
      data-calendar-bar=""
      className="flex flex-wrap items-center gap-2 px-4 py-2.5 sm:px-6"
    >
      <Button
        variant="outline"
        size="sm"
        className={calendarOutlineButtonClass}
        onClick={onToday}
      >
        {copy.today}
      </Button>
      <Button
        variant="outline"
        size="icon-sm"
        className={ICON_BUTTON_CLASS}
        aria-label={copy.previous[range]}
        onClick={() => onStep(-1)}
      >
        <ChevronLeft className="size-4" aria-hidden="true" />
      </Button>
      <Button
        variant="outline"
        size="icon-sm"
        className={ICON_BUTTON_CLASS}
        aria-label={copy.next[range]}
        onClick={() => onStep(1)}
      >
        <ChevronRight className="size-4" aria-hidden="true" />
      </Button>
      <h2
        aria-live="polite"
        className="mx-1.5 min-w-37.5 text-calendar-title leading-[1.45] text-foreground"
      >
        {heading}
      </h2>

      <div
        role="radiogroup"
        aria-label={copy.ranges.label}
        className="inline-flex rounded-[8px] bg-state-hover-strong p-0.5"
      >
        {RANGES.map((value, index) => {
          const on = value === range;
          return (
            <button
              key={value}
              ref={(element) => {
                rangeButtons.current[index] = element;
              }}
              type="button"
              role="radio"
              aria-checked={on}
              tabIndex={on ? 0 : -1}
              className={cn(
                "h-7 cursor-pointer rounded-[6px] px-3 text-compact font-medium text-muted-foreground focus-visible:outline-2 focus-visible:outline-ring",
                on &&
                  "bg-popover font-semibold text-foreground shadow-segment dark:bg-card",
              )}
              onClick={() => onRangeChange(value)}
              onKeyDown={onRangeKeyDown}
            >
              {copy.ranges[value]}
            </button>
          );
        })}
      </div>

      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button
            variant="ghost"
            size="sm"
            className={cn(kanbanPillClass, "leading-[1.45] font-normal")}
          >
            <CalendarDays className="size-4" aria-hidden="true" />
            {copy.basis.label}{" "}
            <b className="font-semibold text-foreground">
              {copy.basis[dateBasis]}
            </b>
            <ChevronDown className="size-3.25" aria-hidden="true" />
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="start" className="w-56">
          <DropdownMenuRadioGroup
            value={dateBasis}
            onValueChange={(value) => {
              const basis = BASES.find((candidate) => candidate === value);
              if (basis && basis !== dateBasis) onDateBasisChange(basis);
            }}
          >
            {BASES.map((value) => (
              <DropdownMenuRadioItem key={value} value={value}>
                {copy.basis[value]}
              </DropdownMenuRadioItem>
            ))}
          </DropdownMenuRadioGroup>
        </DropdownMenuContent>
      </DropdownMenu>

      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button
            variant="ghost"
            size="sm"
            className={cn(
              kanbanPillClass,
              "leading-[1.45] font-normal",
              filtered &&
                "bg-state-selected text-state-selected-foreground hover:bg-state-selected hover:text-state-selected-foreground",
            )}
          >
            <Filter className="size-4" aria-hidden="true" />
            {filtered ? (
              <>
                {copy.filter.activePrefix}{" "}
                <b className="max-w-64 truncate font-semibold">
                  {segments.join(copy.filterSeparator)}
                </b>
              </>
            ) : (
              copy.filter.trigger
            )}
          </Button>
        </DropdownMenuTrigger>
        <KanbanFilterMenuContent
          copy={copy.filter}
          filter={filter}
          options={filterOptions}
          onFilterChange={onFilterChange}
        />
      </DropdownMenu>

      {savedFilters}

      <span className="flex-1" />

      <span className="text-xs leading-[1.45] text-meta-foreground">
        {zoneLabel}
      </span>

      {canAdd ? (
        <Button
          size="sm"
          className={cn(kanbanPrimaryButtonClass, "border-primary")}
          onClick={onAddItem}
        >
          <Plus className="size-4" aria-hidden="true" />
          {copy.addItem}
        </Button>
      ) : null}
    </div>
  );
}
