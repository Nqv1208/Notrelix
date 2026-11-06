import {
  useRef,
  type KeyboardEvent,
  type MouseEvent,
  type ReactNode,
} from "react";
import {
  ArrowDownUp,
  ArrowLeftRight,
  CalendarDays,
  ChevronLeft,
  ChevronRight,
  Link2,
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
  DropdownMenuSeparator,
  DropdownMenuTrigger,
  TABLET_SLOT,
  TabletSlot,
} from "@notrelix/ui-web";
import type {
  TimelineBarSpan,
  TimelineGroupBy,
} from "@notrelix/work-management-core";
import { kanbanPrimaryButtonClass } from "../kanban/kanban-chrome";
import {
  timelineIconButtonClass,
  timelineOutlineButtonClass,
} from "./timeline-chrome";
import { fill, plural, type TimelineScale } from "./timeline-model";
import type { TimelineCopy } from "./timeline.copy";

const SCALES = ["day", "week", "month"] as const;
const GROUPINGS = ["group", "status", "assignee", "none"] as const;
const BAR_SPANS = ["start-due", "created-due"] as const;

const TOGGLE_CLASS =
  "inline-flex h-[30px] shrink-0 cursor-pointer items-center gap-1.5 @max-[820px]/bar:size-8 @max-[820px]/bar:justify-center @max-[820px]/bar:px-0 rounded-[7px] border border-line-strong bg-popover px-2.5 text-crumb leading-[1.45] font-medium text-muted-foreground focus-visible:outline-2 focus-visible:outline-ring aria-disabled:cursor-default dark:bg-card";
const TOGGLE_ON_CLASS =
  "border-transparent bg-state-selected font-semibold text-state-selected-foreground dark:bg-state-selected";

export interface TimelineToolbarProps {
  copy: TimelineCopy;
  scale: TimelineScale;
  onScaleChange: (scale: TimelineScale) => void;
  onToday: () => void;
  /** The window is on today: "Hôm nay" is dimmed and says so. */
  atToday?: boolean;
  /** "Vừa khít": absent when no item has a date. */
  fit?: { tip: string; note: string; onFit: () => void };
  onStep: (direction: -1 | 1) => void;
  /**
   * The dependency lines: `available` is whether any item has a dependency.
   * Without one the toggle looks as in the design and says why it is idle.
   */
  links: { available: boolean; shown: boolean; onToggle: () => void };
  criticalPath: { available: boolean; shown: boolean; onToggle: () => void };
  undatedCount: number;
  /** Where the items without a date can be given one (the Calendar view). */
  onOpenUndated?: () => void;
  /** Set when the count opens the tray: whether it is open. */
  trayOpen?: boolean;
  undatedHref?: string;
  /** False for a viewer: the add button is not shown. */
  canAdd: boolean;
  /** Without it "Thêm công việc" is inert and says where to add an item. */
  onAddItem?: () => void;
  /** The grouping and bar span of the shared view, and how they are changed. */
  presentation: {
    groupBy: TimelineGroupBy;
    barSpan: TimelineBarSpan;
    onGroupByChange: (value: TimelineGroupBy) => void;
    onBarSpanChange: (value: TimelineBarSpan) => void;
  };
  /** The member's filter, personal-state chip and saved filters (`ViewStateBar`), after "Vừa khít". */
  viewBar?: ReactNode;
}

/**
 * The Timeline bar: today, back and forward, the day/week/month switch, the
 * grouping and bar-span pills, the dependency and critical-path toggles, the
 * count of items without a date and "Thêm công việc". It wraps onto a second
 * line when the width runs out, as the design's does at 1184 px. The two
 * toggles wrap together, so a line never ends up holding one of them alone.
 */
export function TimelineToolbar({
  copy,
  scale,
  onScaleChange,
  onToday,
  atToday = false,
  fit,
  onStep,
  links,
  criticalPath,
  undatedCount,
  onOpenUndated,
  trayOpen,
  undatedHref,
  canAdd,
  onAddItem,
  viewBar,
  presentation,
}: TimelineToolbarProps) {
  const scaleButtons = useRef<Array<HTMLButtonElement | null>>([]);

  const onScaleKeyDown = (event: KeyboardEvent<HTMLButtonElement>) => {
    const step =
      event.key === "ArrowRight" || event.key === "ArrowDown"
        ? 1
        : event.key === "ArrowLeft" || event.key === "ArrowUp"
          ? -1
          : 0;
    if (step === 0) return;
    event.preventDefault();
    const next =
      SCALES[(SCALES.indexOf(scale) + step + SCALES.length) % SCALES.length];
    if (!next) return;
    onScaleChange(next);
    scaleButtons.current[SCALES.indexOf(next)]?.focus();
  };

  const openUndated = (event: MouseEvent<HTMLAnchorElement>) => {
    if (!onOpenUndated) return;
    event.preventDefault();
    onOpenUndated();
  };

  return (
    <div className="@container/bar">
      <div
        data-tl="bar"
        className="flex flex-wrap items-center gap-2 px-4 py-2.5 @max-[1030px]/bar:gap-1.5 sm:px-6"
      >
        <Button
          variant={atToday ? "outline" : "default"}
          size="sm"
          className={cn(
            timelineOutlineButtonClass,
            atToday && "text-meta-foreground",
          )}
          aria-label={copy.todayLabel}
          aria-current={atToday ? "date" : undefined}
          onClick={onToday}
        >
          {copy.today}
        </Button>
        <Button
          variant="outline"
          size="icon-sm"
          className={timelineIconButtonClass}
          aria-label={copy.previous}
          onClick={() => onStep(-1)}
        >
          <ChevronLeft className="size-4" aria-hidden="true" />
        </Button>
        <Button
          variant="outline"
          size="icon-sm"
          className={timelineIconButtonClass}
          aria-label={copy.next}
          onClick={() => onStep(1)}
        >
          <ChevronRight className="size-4" aria-hidden="true" />
        </Button>

        <div
          role="radiogroup"
          aria-label={copy.scale.label}
          className="inline-flex rounded-[8px] bg-state-hover-strong p-0.5"
        >
          {SCALES.map((value, index) => {
            const on = value === scale;
            return (
              <button
                key={value}
                ref={(element) => {
                  scaleButtons.current[index] = element;
                }}
                type="button"
                role="radio"
                aria-checked={on}
                tabIndex={on ? 0 : -1}
                className={cn(
                  "h-7 cursor-pointer rounded-[6px] px-2.75 text-compact leading-[1.45] font-medium text-muted-foreground focus-visible:outline-2 focus-visible:outline-ring",
                  on &&
                    "bg-popover font-semibold text-foreground shadow-segment dark:bg-card",
                )}
                onClick={() => onScaleChange(value)}
                onKeyDown={onScaleKeyDown}
              >
                {copy.scale[value]}
              </button>
            );
          })}
        </div>

        {fit ? (
          <Button
            variant="outline"
            size="sm"
            className={cn(
              timelineOutlineButtonClass,
              "@max-[820px]/bar:w-8 @max-[820px]/bar:px-0!",
            )}
            title={`${fit.tip}. ${fit.note}`}
            aria-label={copy.fit.label}
            onClick={fit.onFit}
          >
            <ArrowLeftRight
              className="hidden size-4 @max-[820px]/bar:block"
              aria-hidden="true"
            />
            <span className="@max-[820px]/bar:sr-only">{copy.fit.label}</span>
          </Button>
        ) : null}

        {viewBar}

        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button
              variant="outline"
              size="icon-sm"
              className={timelineIconButtonClass}
              aria-label={copy.overflow}
            >
              <MoreHorizontal className="size-4" aria-hidden="true" />
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="start" className="w-56">
            <DropdownMenuLabel>{copy.groupBy.label}</DropdownMenuLabel>
            <DropdownMenuRadioGroup
              value={presentation.groupBy}
              onValueChange={(id) => {
                const next = GROUPINGS.find((option) => option === id);
                if (next && next !== presentation.groupBy)
                  presentation.onGroupByChange(next);
              }}
            >
              {GROUPINGS.map((id) => (
                <DropdownMenuRadioItem key={id} value={id}>
                  {copy.groupBy[id]}
                </DropdownMenuRadioItem>
              ))}
            </DropdownMenuRadioGroup>
            <DropdownMenuSeparator />
            <DropdownMenuLabel>{copy.barSpan.label}</DropdownMenuLabel>
            <DropdownMenuRadioGroup
              value={presentation.barSpan}
              onValueChange={(id) => {
                const next = BAR_SPANS.find((option) => option === id);
                if (next && next !== presentation.barSpan)
                  presentation.onBarSpanChange(next);
              }}
            >
              {BAR_SPANS.map((id) => (
                <DropdownMenuRadioItem key={id} value={id}>
                  {copy.barSpan[id]}
                </DropdownMenuRadioItem>
              ))}
            </DropdownMenuRadioGroup>
          </DropdownMenuContent>
        </DropdownMenu>

        {/* The toggles and the undated link wrap as one group, so a narrow bar
          never leaves the link alone on a line of its own. */}
        <div data-tl="tail" className="flex flex-auto items-center gap-2">
          <div data-tl="toggles" className="flex flex-none items-center gap-2">
            <button
              type="button"
              aria-pressed={links.available ? links.shown : true}
              aria-disabled={links.available ? undefined : "true"}
              title={links.available ? undefined : copy.unavailable.links}
              aria-label={
                links.available
                  ? undefined
                  : `${copy.links}: ${copy.unavailable.links}`
              }
              className={cn(
                TOGGLE_CLASS,
                (links.available ? links.shown : true) && TOGGLE_ON_CLASS,
              )}
              onClick={links.available ? links.onToggle : undefined}
            >
              <Link2
                className="hidden size-4 @max-[820px]/bar:block"
                aria-hidden="true"
              />
              <span className="@max-[820px]/bar:sr-only">{copy.links}</span>
            </button>
            <button
              type="button"
              aria-pressed={criticalPath.available ? criticalPath.shown : false}
              aria-disabled={criticalPath.available ? undefined : "true"}
              title={
                criticalPath.available
                  ? undefined
                  : copy.unavailable.criticalPath
              }
              aria-label={
                criticalPath.available
                  ? undefined
                  : `${copy.criticalPath}: ${copy.unavailable.criticalPath}`
              }
              className={cn(
                TOGGLE_CLASS,
                criticalPath.available && criticalPath.shown && TOGGLE_ON_CLASS,
              )}
              onClick={
                criticalPath.available ? criticalPath.onToggle : undefined
              }
            >
              <ArrowDownUp
                className="hidden size-4 @max-[820px]/bar:block"
                aria-hidden="true"
              />
              <span className="@max-[820px]/bar:sr-only">
                {copy.criticalPath}
              </span>
            </button>
          </div>

          <span className="flex-1" />

          {undatedCount > 0 ? (
            <a
              href={undatedHref ?? "#"}
              aria-expanded={trayOpen}
              className="inline-flex items-center gap-1 text-crumb leading-[1.45] font-semibold text-state-selected-foreground no-underline @max-[1030px]/bar:h-8 @max-[1030px]/bar:rounded-[7px] @max-[1030px]/bar:bg-state-selected @max-[1030px]/bar:px-2.5"
              onClick={openUndated}
            >
              <CalendarDays
                className="hidden size-4 @max-[1030px]/bar:block"
                aria-hidden="true"
              />
              <span
                className="hidden @max-[1030px]/bar:inline"
                aria-hidden="true"
              >
                {undatedCount}
              </span>
              <span className="@max-[1030px]/bar:sr-only">
                {fill(
                  plural(copy.undated_one, copy.undated_other, undatedCount),
                  { count: undatedCount },
                )}
              </span>
            </a>
          ) : null}
        </div>

        {canAdd ? (
          <Button
            size="sm"
            className={cn(
              kanbanPrimaryButtonClass,
              "border-primary @max-[820px]/bar:w-8 @max-[820px]/bar:px-0",
            )}
            aria-disabled={onAddItem ? undefined : "true"}
            title={onAddItem ? undefined : copy.unavailable.addItem}
            aria-label={
              onAddItem
                ? undefined
                : `${copy.addItem}: ${copy.unavailable.addItem}`
            }
            onClick={onAddItem}
          >
            <Plus className="size-4" aria-hidden="true" />
            <span className="@max-[820px]/bar:sr-only">{copy.addItem}</span>
          </Button>
        ) : null}
        {/* The tablet row ends with the view menu, Add view and the avatar stack (the board header portals them in). */}
        <TabletSlot
          slot={TABLET_SLOT.toolbarTrail}
          className="hidden items-center gap-2 tablet:flex"
        />
      </div>
    </div>
  );
}
