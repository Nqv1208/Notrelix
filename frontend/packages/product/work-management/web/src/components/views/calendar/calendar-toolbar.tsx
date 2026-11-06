import { useRef, type KeyboardEvent, type ReactNode } from "react";
import {
  CalendarDays,
  ChevronDown,
  ChevronLeft,
  ChevronRight,
  MoreHorizontal,
  Plus,
} from "lucide-react";
import {
  Button,
  cn,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuLabel,
  DropdownMenuRadioGroup,
  DropdownMenuRadioItem,
  DropdownMenuTrigger,
  TABLET_SLOT,
  TabletPortal,
  TabletSlot,
  useIsTablet,
} from "@notrelix/ui-web";
import {
  kanbanPillClass,
  kanbanPrimaryButtonClass,
} from "../kanban/kanban-chrome";
import { calendarOutlineButtonClass } from "./calendar-chrome";
import type { CalendarCopy } from "./calendar.copy";
import type { CalendarDateBasis, CalendarRange } from "./calendar-model";

const RANGES = ["month", "week", "day"] as const;

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
  /** The board's Date fields an item can be placed by, after "Bắt đầu → Hạn" and "Hạn". */
  dateFields?: readonly { id: string; name: string }[];
  /** The time zone line ("Giờ Hà Nội (GMT+7)"). */
  zoneLabel: string;
  /** False for a viewer who cannot add items: the add button is not shown. */
  canAdd: boolean;
  onAddItem: () => void;
  /** The view controls: filter, personal-state chip and saved filters. */
  viewBar?: ReactNode;
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
  dateFields = [],
  zoneLabel,
  canAdd,
  onAddItem,
  viewBar,
}: CalendarToolbarProps) {
  const basisLabel =
    dateBasis === "span" || dateBasis === "due"
      ? copy.basis[dateBasis]
      : (dateFields.find((field) => field.id === dateBasis)?.name ??
        copy.basis.span);
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

  const basisMenu = (
    <DropdownMenuRadioGroup
      value={dateBasis}
      onValueChange={(value) => {
        if (value !== dateBasis) onDateBasisChange(value);
      }}
    >
      {(["span", "due"] as const).map((value) => (
        <DropdownMenuRadioItem key={value} value={value}>
          {copy.basis[value]}
        </DropdownMenuRadioItem>
      ))}
      {dateFields.map((field) => (
        <DropdownMenuRadioItem key={field.id} value={field.id}>
          {field.name}
        </DropdownMenuRadioItem>
      ))}
    </DropdownMenuRadioGroup>
  );
  const touch = useIsTablet();
  if (touch) {
    // Final-T-Calendar: a 56 px bar of 44 px targets. The date basis moves to the
    // tray; the view controls keep 44 px hit areas.
    const touchButton =
      "h-11 rounded-[10px] border-line-strong bg-popover px-3.5 text-[14.5px] font-medium text-foreground shadow-none hover:bg-state-hover-strong dark:border-line-strong dark:bg-card";
    return (
      <div
        data-calendar-bar=""
        data-touch=""
        className="flex h-14 flex-none items-center gap-1.5 px-4 lg:col-start-1 lg:row-start-1 [&_[data-slot=button]]:min-h-11"
      >
        <Button variant="outline" className={touchButton} onClick={onToday}>
          {copy.today}
        </Button>
        {(["previous", "next"] as const).map((key) => (
          <Button
            key={key}
            variant="ghost"
            className="size-11 rounded-[10px] p-0 text-muted-foreground [&_svg]:size-5"
            aria-label={copy[key][range]}
            onClick={() => onStep(key === "previous" ? -1 : 1)}
          >
            {key === "previous" ? (
              <ChevronLeft aria-hidden="true" />
            ) : (
              <ChevronRight aria-hidden="true" />
            )}
          </Button>
        ))}
        <h2
          aria-live="polite"
          className="ml-1.5 min-w-0 truncate text-calendar-title leading-[1.45] text-foreground"
        >
          {heading}
        </h2>
        <div className="min-w-0 flex-1" />
        <div
          role="radiogroup"
          aria-label={copy.ranges.label}
          className="inline-flex h-12 items-center rounded-[12px] bg-state-hover-strong p-0.5"
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
                  "h-11 cursor-pointer rounded-[10px] px-3.5 text-sm font-medium text-muted-foreground focus-visible:outline-2 focus-visible:outline-ring",
                  on &&
                    "bg-popover font-[650] text-foreground shadow-segment dark:bg-card",
                )}
                onClick={() => onRangeChange(value)}
                onKeyDown={onRangeKeyDown}
              >
                {copy.ranges[value]}
              </button>
            );
          })}
        </div>
        {viewBar}
        {/* Final-T-Calendar draws the add button in the page bar, beside "Chia sẻ". */}
        {canAdd ? (
          <TabletPortal slot={TABLET_SLOT.headerAdd}>
            <Button
              size="icon"
              aria-label={copy.addItem}
              className="size-11 shrink-0 rounded-[10px]"
              onClick={onAddItem}
            >
              <Plus className="size-5" aria-hidden="true" />
            </Button>
          </TabletPortal>
        ) : null}
        {/* The tablet board header has no tab row: the view menu, Add view and the avatar stack are portalled here. */}
        <TabletSlot
          slot={TABLET_SLOT.toolbarTrail}
          className="flex items-center gap-2"
        />
      </div>
    );
  }
  return (
    <div className="@container/bar">
      <div
        data-calendar-bar=""
        className="flex flex-wrap items-center gap-2 px-4 py-2.5 untablet:@max-[1030px]/bar:gap-1.5 sm:px-6"
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
          className="mx-1.5 min-w-37.5 text-calendar-title untablet:@max-[820px]/bar:min-w-33.5 leading-[1.45] text-foreground"
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
              className={cn(
                kanbanPillClass,
                "leading-[1.45] font-normal untablet:@max-[1030px]/bar:hidden",
              )}
            >
              <CalendarDays className="size-4" aria-hidden="true" />
              <span className="untablet:@max-[1200px]/bar:sr-only">
                {copy.basis.label}{" "}
              </span>
              <b className="font-semibold text-foreground">{basisLabel}</b>
              <ChevronDown className="size-3.25" aria-hidden="true" />
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="start" className="w-56">
            {basisMenu}
          </DropdownMenuContent>
        </DropdownMenu>

        {viewBar}

        {/* Under 1000 px the date basis moves into "Thêm", as WM-Toolbar-Calendar draws. */}
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button
              variant="outline"
              size="icon-sm"
              className={cn(ICON_BUTTON_CLASS, "hidden untablet:@max-[1030px]/bar:grid")}
              aria-label={copy.overflow}
            >
              <MoreHorizontal className="size-4" aria-hidden="true" />
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end" className="w-56">
            <DropdownMenuLabel>{copy.basis.label}</DropdownMenuLabel>
            {basisMenu}
          </DropdownMenuContent>
        </DropdownMenu>

        {/* The zone line gives way (clipped, full text in the title) so the bar stays on one row. */}
        <span
          title={zoneLabel}
          className="ml-auto min-w-0 truncate text-right text-xs leading-[1.45] text-meta-foreground untablet:@max-[1030px]/bar:hidden"
        >
          {/* WM-Toolbar-Calendar: the bar says "GMT+7"; the title keeps the full line. */}
          {/\(([^)]+)\)/.exec(zoneLabel)?.[1] ?? zoneLabel}
        </span>

        {canAdd ? (
          <Button
            size="sm"
            className={cn(
              kanbanPrimaryButtonClass,
              "border-primary untablet:@max-[820px]/bar:w-8 untablet:@max-[820px]/bar:px-0",
            )}
            onClick={onAddItem}
          >
            <Plus className="size-4" aria-hidden="true" />
            <span className="untablet:@max-[820px]/bar:sr-only">{copy.addItem}</span>
          </Button>
        ) : null}
      </div>
    </div>
  );
}
