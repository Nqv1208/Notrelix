import { describe, expect, it } from "vitest";
import { groupCardsByStatus } from "@notrelix/work-management-core";
import { erpBoardScenario, erpItemId } from "@notrelix/work-management-testing";
import { vi as copy } from "../calendar.copy";
import {
  buildCalendarEntries,
  dayOf,
  shiftInstant,
  zonedInstant,
} from "../calendar-entries";

const ZONE = "Asia/Ho_Chi_Minh";

function built(today = "2026-10-12") {
  const { board, groups } = erpBoardScenario();
  const lanes = groupCardsByStatus({
    fieldDefinitions: board.fieldDefinitions,
    groups,
  });
  return buildCalendarEntries({ lanes, today, copy, timeZone: ZONE });
}

function entryOf(key: string, today?: string) {
  const entry = built(today).entries.find(
    (candidate) => candidate.id === erpItemId(key),
  );
  if (!entry) throw new Error(`no entry ${key}`);
  return entry;
}

describe("buildCalendarEntries on the ERP board", () => {
  it("places every dated item and lists the undated one", () => {
    const { entries, undated } = built();
    expect(entries).toHaveLength(15);
    expect(undated.map((item) => item.itemKey)).toEqual(["ERP-153"]);
    expect(undated[0]?.assigneeName).toBeNull();
  });

  it("puts an item on its due day in the zone of the view", () => {
    expect(entryOf("ERP-144")).toMatchObject({
      startDay: "2026-10-12",
      endDay: "2026-10-12",
      dueMinutes: 17 * 60,
    });
    // 17:00 in Hanoi is 10:00 UTC; another zone moves the day, not the data.
    const { board, groups } = erpBoardScenario();
    const lanes = groupCardsByStatus({
      fieldDefinitions: board.fieldDefinitions,
      groups,
    });
    const kiritimati = buildCalendarEntries({
      lanes,
      today: "2026-10-12",
      copy,
      timeZone: "Pacific/Kiritimati",
    });
    expect(
      kiritimati.entries.find((entry) => entry.id === erpItemId("ERP-144"))
        ?.endDay,
    ).toBe("2026-10-13");
  });

  it("spans an item from its start day to its due day", () => {
    expect(entryOf("ERP-142")).toMatchObject({
      startDay: "2026-10-01",
      endDay: "2026-10-10",
    });
    expect(entryOf("ERP-146")).toMatchObject({
      startDay: "2026-10-13",
      endDay: "2026-10-16",
    });
  });

  it("colours by Status category and marks unfinished past items overdue", () => {
    expect(entryOf("ERP-142").tone).toBe("overdue");
    expect(entryOf("ERP-144").tone).toBe("doing");
    expect(entryOf("ERP-138").tone).toBe("review");
    expect(entryOf("ERP-151").tone).toBe("todo");
    // Done stays done even when its due day is long past.
    expect(entryOf("ERP-129").tone).toBe("done");
  });

  it("is overdue only from the day after the due day", () => {
    expect(entryOf("ERP-144", "2026-10-12").tone).toBe("doing");
    expect(entryOf("ERP-144", "2026-10-13").tone).toBe("overdue");
  });

  it("describes the entry for assistive technology", () => {
    expect(entryOf("ERP-144").note).toBe("hạn hôm nay");
    expect(entryOf("ERP-138").note).toBe("chờ duyệt, hạn 14/10");
    expect(entryOf("ERP-151").note).toBe("hạn 18/10");
    expect(entryOf("ERP-142").note).toBe(
      "bắt đầu 01/10, hạn 10/10, quá hạn 2 ngày",
    );
    expect(entryOf("ERP-129").note).toBe("hoàn thành 30/09");
  });

  it("carries the assignee as initials with a stable tone", () => {
    const first = entryOf("ERP-142").assignee;
    expect(first?.initials).toBeTruthy();
    expect(entryOf("ERP-142").assignee?.markClass).toBe(first?.markClass);
    expect(entryOf("ERP-142").movable).toBe(true);
  });
});

describe("calendar instants", () => {
  it("reads the wall-clock day and minutes of an instant", () => {
    expect(dayOf("2026-10-12T10:00:00.000Z", ZONE)).toEqual({
      day: "2026-10-12",
      minutes: 17 * 60,
    });
    expect(dayOf("2026-10-12T20:00:00.000Z", ZONE)?.day).toBe("2026-10-13");
    expect(dayOf(null, ZONE)).toBeNull();
    expect(dayOf("not a date", ZONE)).toBeNull();
  });

  it("builds the instant of a wall-clock time in a zone", () => {
    expect(zonedInstant("2026-10-20", 17, 0, ZONE)).toBe(
      "2026-10-20T10:00:00.000Z",
    );
    expect(zonedInstant("2026-10-20", 0, 0, ZONE)).toBe(
      "2026-10-19T17:00:00.000Z",
    );
    // New York is on daylight-saving time until 1 November 2026.
    expect(zonedInstant("2026-10-20", 17, 0, "America/New_York")).toBe(
      "2026-10-20T21:00:00.000Z",
    );
    expect(zonedInstant("2026-11-20", 17, 0, "America/New_York")).toBe(
      "2026-11-20T22:00:00.000Z",
    );
  });

  it("shifts an instant by whole days", () => {
    expect(shiftInstant("2026-10-16T10:00:00.000Z", 4)).toBe(
      "2026-10-20T10:00:00.000Z",
    );
    expect(shiftInstant("2026-10-16T10:00:00.000Z", -2)).toBe(
      "2026-10-14T10:00:00.000Z",
    );
  });
});
