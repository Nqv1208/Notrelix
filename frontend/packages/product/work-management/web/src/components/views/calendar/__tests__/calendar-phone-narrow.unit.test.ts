import { describe, expect, it } from "vitest";
import { agendaGroups, agendaPeriod } from "../calendar-phone-narrow";
import type { CalendarEntry, DayKey } from "../calendar-model";

function entry(
  id: string,
  startDay: string,
  endDay: string,
  title = id,
): CalendarEntry {
  return {
    id,
    kind: "item",
    itemKey: id,
    title,
    tone: "doing",
    startDay: startDay as DayKey,
    endDay: endDay as DayKey,
    note: "",
    movable: true,
  };
}

describe("agendaPeriod", () => {
  it("lists the anchor's month, its week or the day", () => {
    const month = agendaPeriod("month", "2026-10-12" as DayKey);
    expect(month[0]).toBe("2026-10-01");
    expect(month.at(-1)).toBe("2026-10-31");
    expect(month).toHaveLength(31);
    expect(agendaPeriod("week", "2026-10-14" as DayKey)).toEqual([
      "2026-10-12",
      "2026-10-13",
      "2026-10-14",
      "2026-10-15",
      "2026-10-16",
      "2026-10-17",
      "2026-10-18",
    ]);
    expect(agendaPeriod("day", "2026-10-14" as DayKey)).toEqual(["2026-10-14"]);
  });
});

describe("agendaGroups", () => {
  const week = agendaPeriod("week", "2026-10-12" as DayKey);

  it("puts each entry under the day it starts, in day order", () => {
    const groups = agendaGroups(
      [
        entry("ERP-145", "2026-10-14", "2026-10-17"),
        entry("ERP-142", "2026-10-12", "2026-10-16"),
      ],
      week,
    );
    expect(groups.map((g) => [g.day, g.entries.map((e) => e.id)])).toEqual([
      ["2026-10-12", ["ERP-142"]],
      ["2026-10-14", ["ERP-145"]],
    ]);
  });

  it("lists an entry that began before the period under its first day", () => {
    const groups = agendaGroups(
      [
        entry("B", "2026-10-12", "2026-10-12", "Beta"),
        entry("A", "2026-10-01", "2026-10-13", "Alpha"),
      ],
      week,
    );
    expect(groups).toHaveLength(1);
    expect(groups[0]!.day).toBe("2026-10-12");
    // By start, so the earlier one comes first.
    expect(groups[0]!.entries.map((e) => e.id)).toEqual(["A", "B"]);
  });

  it("leaves out entries outside the period", () => {
    expect(
      agendaGroups(
        [
          entry("old", "2026-10-01", "2026-10-11"),
          entry("later", "2026-10-19", "2026-10-20"),
        ],
        week,
      ),
    ).toEqual([]);
  });
});
