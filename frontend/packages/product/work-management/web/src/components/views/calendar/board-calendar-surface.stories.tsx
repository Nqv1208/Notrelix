import { useState } from "react";
import type { Meta, StoryObj } from "@storybook/react";
import {
  calendarDenseScenario,
  calendarEdgeScenario,
  calendarEmptyScenario,
  calendarReadOnlyScenario,
  calendarScenario,
  type CalendarScenario,
} from "../../../verification/calendar-fixtures";
import type { KanbanFilterState } from "../kanban/kanban-filter";
import { BoardCalendarSurface } from "./board-calendar-surface";
import type {
  CalendarDateBasis,
  CalendarRange,
  DayKey,
} from "./calendar-model";

/**
 * Holds the navigation state the container owns (range, day, basis, filter)
 * so every story can be driven; data and writes are the scenario's.
 */
function CalendarStory({ scenario }: { scenario: CalendarScenario }) {
  const [range, setRange] = useState<CalendarRange>(scenario.range);
  const [anchorDay, setAnchorDay] = useState<DayKey>(scenario.anchorDay);
  const [dateBasis, setDateBasis] = useState<CalendarDateBasis>(
    scenario.dateBasis,
  );
  const [filter, setFilter] = useState<KanbanFilterState>(scenario.filter);
  return (
    <div className="h-[793px] min-h-0">
      <BoardCalendarSurface
        {...scenario}
        range={range}
        onRangeChange={setRange}
        anchorDay={anchorDay}
        onAnchorChange={setAnchorDay}
        dateBasis={dateBasis}
        onDateBasisChange={setDateBasis}
        filter={filter}
        onFilterChange={setFilter}
        onOpenItem={() => undefined}
        onReschedule={() => undefined}
        onSetDue={() => undefined}
        onCreateItem={() => undefined}
      />
    </div>
  );
}

const meta: Meta = {
  title: "Work Management/Calendar/Board Calendar",
  parameters: { layout: "fullscreen" },
};
export default meta;
type Story = StoryObj;

/** Final-Calendar binh-thuong, range thang: October 2026 with today on 12/10. */
export const Default: Story = {
  render: () => <CalendarStory scenario={calendarScenario()} />,
  tags: ["fui-surface--wm.calendar.board", "fui-state--Default"],
};

/** A board with no item: empty weeks, no undated list. */
export const Empty: Story = {
  render: () => <CalendarStory scenario={calendarEmptyScenario()} />,
  tags: ["fui-surface--wm.calendar.board", "fui-state--Empty"],
};

/** A long title, an item without key or assignee, a span over the month end. */
export const EdgeData: Story = {
  render: () => <CalendarStory scenario={calendarEdgeScenario()} />,
  tags: ["fui-surface--wm.calendar.board", "fui-state--EdgeData"],
};

/** Nine items on two days: the rest collapse into "+N". */
export const HighDensity: Story = {
  render: () => <CalendarStory scenario={calendarDenseScenario()} />,
  tags: ["fui-surface--wm.calendar.board", "fui-state--HighDensity"],
};

/** A viewer: no add, no drag, no quick create, no "Đặt hạn". */
export const ReadOnly: Story = {
  render: () => <CalendarStory scenario={calendarReadOnlyScenario()} />,
  tags: ["fui-surface--wm.calendar.board", "fui-state--ReadOnly"],
};

/** Final-Calendar binh-thuong, range tuan. */
export const Week: Story = {
  render: () => (
    <CalendarStory scenario={calendarScenario({ range: "week" })} />
  ),
};

/** Final-Calendar binh-thuong, range ngay: overdue, all day, hour slots, now line. */
export const Day: Story = {
  render: () => <CalendarStory scenario={calendarScenario({ range: "day" })} />,
};

/** Final-Calendar keo-tha: ERP-146 lifted from 16/10 over 20/10, with the drop tooltip. */
export const Dragging: Story = {
  render: () => (
    <CalendarStory scenario={calendarScenario({ state: "keo-tha" })} />
  ),
};

/** Final-Calendar tao-nhanh: the quick-create popover on 22/10. */
export const QuickCreate: Story = {
  render: () => (
    <CalendarStory scenario={calendarScenario({ state: "tao-nhanh" })} />
  ),
};

/** Final-Calendar khong-ket-qua: the filter hides every item. */
export const NoResults: Story = {
  render: () => (
    <CalendarStory scenario={calendarScenario({ state: "khong-ket-qua" })} />
  ),
};

export const NoResultsWeek: Story = {
  render: () => (
    <CalendarStory
      scenario={calendarScenario({ state: "khong-ket-qua", range: "week" })}
    />
  ),
};

export const NoResultsDay: Story = {
  render: () => (
    <CalendarStory
      scenario={calendarScenario({ state: "khong-ket-qua", range: "day" })}
    />
  ),
};

export const English: Story = {
  render: () => <CalendarStory scenario={calendarScenario({ locale: "en" })} />,
};
