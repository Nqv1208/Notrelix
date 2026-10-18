import { describe, expect, it } from "vitest";
import {
  addDays,
  addMonths,
  agendaHours,
  buildAgenda,
  diffDays,
  layoutWeek,
  monthWeeks,
  nowLineOffset,
  rescheduledDays,
  startOfWeek,
  stepAnchor,
  weekDays,
  weekRowCapacity,
  weekdayIndex,
  type CalendarEntry,
} from "../calendar-model";

function entry(
  id: string,
  startDay: string,
  endDay: string,
  overrides: Partial<CalendarEntry> = {},
): CalendarEntry {
  return {
    id,
    kind: "item",
    title: id,
    tone: "todo",
    startDay,
    endDay,
    note: "",
    movable: true,
    ...overrides,
  };
}

describe("calendar days", () => {
  it("counts weekdays from Monday and finds the week start", () => {
    expect(weekdayIndex("2026-10-12")).toBe(0);
    expect(weekdayIndex("2026-10-18")).toBe(6);
    expect(startOfWeek("2026-10-15")).toBe("2026-10-12");
    expect(startOfWeek("2026-10-18")).toBe("2026-10-12");
  });

  it("moves across month and year ends", () => {
    expect(addDays("2026-10-31", 1)).toBe("2026-11-01");
    expect(addDays("2026-01-01", -1)).toBe("2025-12-31");
    expect(diffDays("2026-10-01", "2026-10-10")).toBe(9);
    expect(diffDays("2026-10-10", "2026-10-01")).toBe(-9);
  });

  it("clamps the day when a month is shorter", () => {
    expect(addMonths("2026-01-31", 1)).toBe("2026-02-28");
    expect(addMonths("2026-12-15", 1)).toBe("2027-01-15");
    expect(addMonths("2026-01-15", -1)).toBe("2025-12-15");
  });

  it("steps by the range", () => {
    expect(stepAnchor("month", "2026-10-12", 1)).toBe("2026-11-12");
    expect(stepAnchor("week", "2026-10-12", -1)).toBe("2026-10-05");
    expect(stepAnchor("day", "2026-10-12", 1)).toBe("2026-10-13");
  });
});

describe("month and week grids", () => {
  it("draws October 2026 as five Monday-first weeks from 28/09 to 01/11", () => {
    const weeks = monthWeeks("2026-10-12");
    expect(weeks).toHaveLength(5);
    expect(weeks[0]?.[0]).toBe("2026-09-28");
    expect(weeks[4]?.[6]).toBe("2026-11-01");
    expect(weeks.every((week) => week.length === 7)).toBe(true);
  });

  it("draws six weeks when a month needs them", () => {
    // 1 March 2026 is a Sunday: the grid starts on 23/02 and ends on 05/04.
    expect(monthWeeks("2026-03-10")).toHaveLength(6);
  });

  it("returns the seven days of a week", () => {
    expect(weekDays("2026-10-15")).toEqual([
      "2026-10-12",
      "2026-10-13",
      "2026-10-14",
      "2026-10-15",
      "2026-10-16",
      "2026-10-17",
      "2026-10-18",
    ]);
  });
});

describe("layoutWeek", () => {
  const week = weekDays("2026-10-12");

  it("puts a single-day entry in its column and a span across columns", () => {
    const layout = layoutWeek(
      week,
      [
        entry("a", "2026-10-14", "2026-10-14"),
        entry("range", "2026-10-12", "2026-10-16"),
      ],
      null,
    );
    const byId = Object.fromEntries(
      layout.placements.map((placement) => [placement.entry.id, placement]),
    );
    expect(byId.a).toMatchObject({ col: 3, span: 1, row: 1 });
    expect(byId.range).toMatchObject({ col: 1, span: 5, row: 0 });
  });

  it("clips an entry that crosses the week to this week", () => {
    const layout = layoutWeek(
      week,
      [entry("long", "2026-10-01", "2026-10-14")],
      null,
    );
    expect(layout.placements[0]).toMatchObject({ col: 1, span: 3 });
    const next = layoutWeek(
      weekDays("2026-10-19"),
      [entry("long", "2026-10-15", "2026-10-21")],
      null,
    );
    expect(next.placements[0]).toMatchObject({ col: 1, span: 3 });
  });

  it("leaves out an entry outside the week", () => {
    expect(
      layoutWeek(week, [entry("x", "2026-10-20", "2026-10-20")], null)
        .placements,
    ).toHaveLength(0);
  });

  it("orders by start column, longer span first, then board order", () => {
    const layout = layoutWeek(
      week,
      [
        entry("single-1", "2026-10-12", "2026-10-12"),
        entry("span", "2026-10-12", "2026-10-14"),
        entry("single-2", "2026-10-12", "2026-10-12"),
      ],
      null,
    );
    expect(layout.placements.map((placement) => placement.entry.id)).toEqual([
      "span",
      "single-1",
      "single-2",
    ]);
    expect(layout.placements.map((placement) => placement.row)).toEqual([
      0, 1, 2,
    ]);
  });

  it("reuses a free row for entries in different columns", () => {
    const layout = layoutWeek(
      week,
      [
        entry("a", "2026-10-12", "2026-10-12"),
        entry("b", "2026-10-13", "2026-10-13"),
      ],
      null,
    );
    expect(layout.placements.map((placement) => placement.row)).toEqual([0, 0]);
  });

  it("collapses rows that do not fit into one +N per crowded column", () => {
    const crowd = [1, 2, 3, 4].map((n) =>
      entry(`c${n}`, "2026-10-14", "2026-10-14"),
    );
    const layout = layoutWeek(
      week,
      [...crowd, entry("other", "2026-10-15", "2026-10-15")],
      3,
    );
    // Rows 0 and 1 stay, the last visible row goes to "+2" (c3 and c4).
    expect(layout.placements.map((placement) => placement.entry.id)).toEqual([
      "c1",
      "c2",
      "other",
    ]);
    expect(layout.more).toEqual([{ col: 3, count: 2, day: "2026-10-14" }]);
  });

  it("shows every row when they fit", () => {
    const layout = layoutWeek(
      week,
      [1, 2, 3].map((n) => entry(`c${n}`, "2026-10-14", "2026-10-14")),
      3,
    );
    expect(layout.placements).toHaveLength(3);
    expect(layout.more).toEqual([]);
  });

  it("fits three rows in the 136 px week of the 1440 x 900 layout", () => {
    expect(weekRowCapacity(136)).toBe(3);
    expect(weekRowCapacity(90)).toBe(2);
    expect(weekRowCapacity(10)).toBe(1);
  });
});

describe("buildAgenda", () => {
  const today = "2026-10-12";

  it("lists overdue entries on today only", () => {
    const overdue = entry("late", "2026-10-01", "2026-10-10", {
      tone: "overdue",
    });
    expect(buildAgenda([overdue], today, today).overdue).toHaveLength(1);
    expect(buildAgenda([overdue], "2026-10-13", today).overdue).toHaveLength(0);
  });

  it("puts spans and phases in all-day and timed items in their hour slot", () => {
    const phase = entry("phase", "2026-10-12", "2026-10-16", {
      kind: "phase",
    });
    const timed = entry("timed", today, today, { dueMinutes: 17 * 60 });
    const early = entry("early", today, today, { dueMinutes: 6 * 60 });
    const late = entry("late-night", today, today, { dueMinutes: 22 * 60 });
    const agenda = buildAgenda([phase, timed, early, late], today, today);
    expect(agenda.allDay.map((e) => e.id)).toEqual(["phase"]);
    expect(agenda.byHour.get(17)?.map((e) => e.id)).toEqual(["timed"]);
    expect(agenda.byHour.get(8)?.map((e) => e.id)).toEqual(["early"]);
    expect(agenda.byHour.get(18)?.map((e) => e.id)).toEqual(["late-night"]);
  });

  it("has the eleven slots from 08:00 to 18:00", () => {
    expect(agendaHours()).toEqual([8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18]);
  });

  it("places the now line 117 px down at 10:40", () => {
    expect(Math.round(nowLineOffset(10 * 60 + 40) ?? -1)).toBe(117);
    expect(nowLineOffset(7 * 60)).toBeNull();
    expect(nowLineOffset(19 * 60)).toBeNull();
  });
});

describe("rescheduledDays", () => {
  it("moves the due day and shifts the start by as many days", () => {
    expect(
      rescheduledDays(
        { startDay: "2026-10-13", endDay: "2026-10-16" },
        "2026-10-20",
      ),
    ).toEqual({ startDay: "2026-10-17", endDay: "2026-10-20", shift: 4 });
    expect(
      rescheduledDays(
        { startDay: "2026-10-16", endDay: "2026-10-16" },
        "2026-10-14",
      ),
    ).toEqual({ startDay: "2026-10-14", endDay: "2026-10-14", shift: -2 });
  });
});
