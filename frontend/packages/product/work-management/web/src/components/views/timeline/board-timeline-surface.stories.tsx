import { useState } from "react";
import type { Meta, StoryObj } from "@storybook/react";
import {
  timelineDenseScenario,
  timelineEdgeScenario,
  timelineEmptyScenario,
  timelineReadOnlyScenario,
  timelineScenario,
  type TimelineScenario,
} from "../../../verification/timeline-fixtures";
import { BoardTimelineSurface } from "./board-timeline-surface";
import type { DayKey } from "../calendar/calendar-model";
import type { TimelineScale } from "./timeline-model";

/**
 * Holds the navigation state the container owns (scale, window) so every
 * story can be driven; data and writes are the scenario's.
 */
function TimelineStory({ scenario }: { scenario: TimelineScenario }) {
  const [scale, setScale] = useState<TimelineScale>(scenario.scale);
  const [anchorDay, setAnchorDay] = useState<DayKey>(scenario.anchorDay);
  return (
    <div className="h-[793px] min-h-0">
      <BoardTimelineSurface
        {...scenario}
        scale={scale}
        onScaleChange={setScale}
        anchorDay={anchorDay}
        onAnchorChange={setAnchorDay}
        onOpenItem={() => undefined}
        onReschedule={() => undefined}
      />
    </div>
  );
}

const meta: Meta = {
  title: "Work Management/Timeline/Board Timeline",
  parameters: { layout: "fullscreen" },
};
export default meta;
type Story = StoryObj;

/** Final-Timeline binh-thuong, scale ngay: 05/10 to 02/11 with today on 12/10. */
export const Default: Story = {
  render: () => <TimelineStory scenario={timelineScenario()} />,
  tags: ["fui-surface--wm.timeline.board", "fui-state--Default"],
};

/** A board with no dated item: the header and a note. */
export const Empty: Story = {
  render: () => <TimelineStory scenario={timelineEmptyScenario()} />,
  tags: ["fui-surface--wm.timeline.board", "fui-state--Empty"],
};

/** A long title, an item without key or assignee, bars clipped by the window. */
export const EdgeData: Story = {
  render: () => <TimelineStory scenario={timelineEdgeScenario()} />,
  tags: ["fui-surface--wm.timeline.board", "fui-state--EdgeData"],
};

/** Forty-two items: the chart scrolls inside its frame. */
export const HighDensity: Story = {
  render: () => <TimelineStory scenario={timelineDenseScenario()} />,
  tags: ["fui-surface--wm.timeline.board", "fui-state--HighDensity"],
};

/** A viewer: no drag, no keyboard move, no add. */
export const ReadOnly: Story = {
  render: () => <TimelineStory scenario={timelineReadOnlyScenario()} />,
  tags: ["fui-surface--wm.timeline.board", "fui-state--ReadOnly"],
};

/** Final-Timeline binh-thuong, scale tuan. */
export const Week: Story = {
  render: () => (
    <TimelineStory scenario={timelineScenario({ scale: "week" })} />
  ),
};

/** Final-Timeline binh-thuong, scale thang. */
export const Month: Story = {
  render: () => (
    <TimelineStory scenario={timelineScenario({ scale: "month" })} />
  ),
};

/** Final-Timeline keo-gian: ERP-147's due day pulled from 21/10 to 23/10. */
export const Dragging: Story = {
  render: () => (
    <TimelineStory scenario={timelineScenario({ state: "keo-gian" })} />
  ),
};

/** Final-Timeline duong-gang: the critical path in danger, the rest faded. */
export const CriticalPath: Story = {
  render: () => (
    <TimelineStory scenario={timelineScenario({ state: "duong-gang" })} />
  ),
};

export const DraggingWeek: Story = {
  render: () => (
    <TimelineStory
      scenario={timelineScenario({ state: "keo-gian", scale: "week" })}
    />
  ),
};

export const DraggingMonth: Story = {
  render: () => (
    <TimelineStory
      scenario={timelineScenario({ state: "keo-gian", scale: "month" })}
    />
  ),
};

export const CriticalPathMonth: Story = {
  render: () => (
    <TimelineStory
      scenario={timelineScenario({ state: "duong-gang", scale: "month" })}
    />
  ),
};

export const English: Story = {
  render: () => <TimelineStory scenario={timelineScenario({ locale: "en" })} />,
};
