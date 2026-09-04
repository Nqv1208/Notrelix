import { afterEach, describe, expect, it, vi } from "vitest";
import { renderPureUi, screen } from "@notrelix/testing";
import {
  calendarDefaultScenario,
  fixedClock,
} from "@notrelix/work-management-testing";
import { BoardCalendarView } from "../board-calendar-view";

describe("BoardCalendarView", () => {
  afterEach(() => {
    vi.useRealTimers();
  });

  it("renders a fixed reference week independent from system date", () => {
    const scenario = calendarDefaultScenario();
    const referenceDate = fixedClock();

    vi.useFakeTimers();
    vi.setSystemTime(new Date("2034-11-02T03:00:00.000Z"));
    const first = renderPureUi(
      <BoardCalendarView
        groups={scenario.groups}
        referenceDate={referenceDate}
      />,
    );
    const firstText = screen
      .getByText("Workspace calendar")
      .closest("section")?.textContent;
    first.unmount();

    vi.setSystemTime(new Date("2041-04-19T03:00:00.000Z"));
    renderPureUi(
      <BoardCalendarView
        groups={scenario.groups}
        referenceDate={referenceDate}
      />,
    );
    const secondText = screen
      .getByText("Workspace calendar")
      .closest("section")?.textContent;

    expect(secondText).toEqual(firstText);
    expect(secondText).toContain("Workspace calendar");
    expect(secondText).toContain("Card 1.1");
  });
});
