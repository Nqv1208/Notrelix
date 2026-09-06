import type { Meta, StoryObj } from "@storybook/react";
import {
  calendarDefaultScenario,
  calendarDenseScenario,
  calendarEdgeScenario,
  calendarEmptyScenario,
} from "@notrelix/work-management-testing";
import { fixedClock } from "@notrelix/work-management-testing";
import { BoardCalendarView } from "./board-calendar-view";

const meta = {
  title: "Work Management/Calendar/Board Calendar",
  component: BoardCalendarView,
} satisfies Meta<typeof BoardCalendarView>;
export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  args: {
    groups: calendarDefaultScenario().groups,
    referenceDate: fixedClock(),
  },
  tags: ["fui-surface--wm.calendar.board", "fui-state--Default"],
};

export const Empty: Story = {
  args: {
    groups: calendarEmptyScenario().groups,
    referenceDate: fixedClock(),
  },
  tags: ["fui-surface--wm.calendar.board", "fui-state--Empty"],
};

export const EdgeData: Story = {
  args: {
    groups: calendarEdgeScenario().groups,
    referenceDate: fixedClock(),
  },
  tags: ["fui-surface--wm.calendar.board", "fui-state--EdgeData"],
};

export const HighDensity: Story = {
  args: {
    groups: calendarDenseScenario().groups,
    referenceDate: fixedClock(),
  },
  tags: ["fui-surface--wm.calendar.board", "fui-state--HighDensity"],
};
