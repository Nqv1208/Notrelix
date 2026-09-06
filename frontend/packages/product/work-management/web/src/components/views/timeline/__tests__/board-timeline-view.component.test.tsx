import { afterEach, describe, expect, it, vi } from "vitest";
import { renderPureUi, screen } from "@notrelix/testing";
import {
  fixedClock,
  timelineDenseScenario,
  timelineEdgeScenario,
} from "@notrelix/work-management-testing";
import { BoardTimelineView } from "../board-timeline-view";

describe("BoardTimelineView", () => {
  afterEach(() => {
    vi.useRealTimers();
  });

  it("uses fixed referenceDate for stable bar placement", () => {
    const scenario = timelineEdgeScenario();
    const referenceDate = fixedClock();

    vi.useFakeTimers();
    vi.setSystemTime(new Date("2034-11-02T03:00:00.000Z"));
    const first = renderPureUi(
      <BoardTimelineView
        groups={scenario.groups}
        referenceDate={referenceDate}
      />,
    );
    const firstBar = screen.getByTestId("timeline-bar-card-timeline-edge-1-1");
    const firstStyle = {
      marginLeft: firstBar.style.marginLeft,
      width: firstBar.style.width,
    };
    first.unmount();

    vi.setSystemTime(new Date("2041-04-19T03:00:00.000Z"));
    renderPureUi(
      <BoardTimelineView
        groups={scenario.groups}
        referenceDate={referenceDate}
      />,
    );
    const secondBar = screen.getByTestId("timeline-bar-card-timeline-edge-1-1");

    expect({
      marginLeft: secondBar.style.marginLeft,
      width: secondBar.style.width,
    }).toEqual(firstStyle);
  });

  it("keeps the existing visible-item cap for high density input", () => {
    const scenario = timelineDenseScenario();

    renderPureUi(
      <BoardTimelineView
        groups={scenario.groups}
        referenceDate={fixedClock()}
      />,
    );

    expect(screen.getAllByTestId(/^timeline-bar-/)).toHaveLength(10);
  });
});
