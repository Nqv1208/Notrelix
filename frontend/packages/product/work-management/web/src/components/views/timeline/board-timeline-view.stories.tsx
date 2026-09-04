import type { Meta, StoryObj } from "@storybook/react";
import {
  fixedClock,
  timelineDefaultScenario,
  timelineEdgeScenario,
  timelineEmptyScenario,
} from "@notrelix/work-management-testing";
import { BoardTimelineView } from "./board-timeline-view";

const meta = {
  title: "Work Management/Timeline/Board Timeline",
  component: BoardTimelineView,
} satisfies Meta<typeof BoardTimelineView>;
export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  args: {
    groups: timelineDefaultScenario().groups,
    referenceDate: fixedClock(),
  },
  tags: ["fui-surface--wm.timeline.board", "fui-state--Default"],
};

export const Empty: Story = {
  args: {
    groups: timelineEmptyScenario().groups,
    referenceDate: fixedClock(),
  },
  tags: ["fui-surface--wm.timeline.board", "fui-state--Empty"],
};

export const EdgeData: Story = {
  args: {
    groups: timelineEdgeScenario().groups,
    referenceDate: fixedClock(),
  },
  tags: ["fui-surface--wm.timeline.board", "fui-state--EdgeData"],
};
