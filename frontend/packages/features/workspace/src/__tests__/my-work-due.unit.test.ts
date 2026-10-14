import { describe, expect, it } from "vitest";
import { describeMyWorkDue } from "../core/model/my-work";
import { greetingPeriod } from "../web/home/workspace-home-surface";
import { dueWords } from "../web/home/work-row-parts";
import { homeCopyFixture } from "../verification/workspace-home-fixtures";

/** Monday 12 October 2026, 10:40 in the member's calendar. */
const NOW = new Date(2026, 9, 12, 10, 40).getTime();
const at = (day: number, hour = 0, minute = 0) =>
  new Date(2026, 9, day, hour, minute).toISOString();

describe("describeMyWorkDue", () => {
  it("has no due day for a missing or unreadable due date", () => {
    expect(describeMyWorkDue(null, NOW).kind).toBe("none");
    expect(describeMyWorkDue("not a date", NOW).kind).toBe("none");
  });

  it("counts whole local days overdue, however late in the day", () => {
    const due = describeMyWorkDue(at(10, 23, 59), NOW);
    expect(due.kind).toBe("overdue");
    expect(due.daysOverdue).toBe(2);
    expect(describeMyWorkDue(at(11), NOW).daysOverdue).toBe(1);
  });

  it("calls any moment of today today, earlier or later than now", () => {
    expect(describeMyWorkDue(at(12, 0, 0), NOW).kind).toBe("today");
    expect(describeMyWorkDue(at(12, 8, 0), NOW).kind).toBe("today");
    expect(describeMyWorkDue(at(12, 17, 0), NOW).kind).toBe("today");
  });

  it("keeps a time of day only when the due moment has one", () => {
    expect(describeMyWorkDue(at(12), NOW).time).toBeNull();
    expect(describeMyWorkDue(at(12, 17, 5), NOW).time).toBe("17:05");
  });

  it("gives the weekday and dd/MM of an upcoming day", () => {
    const due = describeMyWorkDue(at(14), NOW);
    expect(due.kind).toBe("upcoming");
    expect(due.weekday).toBe(3);
    expect(due.shortDate).toBe("14/10");
    expect(describeMyWorkDue(at(30), NOW).shortDate).toBe("30/10");
  });
});

describe("dueWords", () => {
  const vi = homeCopyFixture("vi");
  const en = homeCopyFixture("en");

  it("writes the overdue, today, time and upcoming forms as the design does", () => {
    expect(
      dueWords(vi, describeMyWorkDue(at(10), NOW), { timeOnToday: true }),
    ).toEqual({ text: "Quá hạn 2 ngày", tone: "over" });
    expect(
      dueWords(vi, describeMyWorkDue(at(12), NOW), { timeOnToday: true }),
    ).toEqual({ text: "Hôm nay", tone: "today" });
    expect(
      dueWords(vi, describeMyWorkDue(at(12, 17, 0), NOW), {
        timeOnToday: true,
      }),
    ).toEqual({ text: "17:00", tone: "today" });
    expect(
      dueWords(vi, describeMyWorkDue(at(12, 17, 0), NOW), {
        timeOnToday: false,
      }).text,
    ).toBe("Hôm nay");
    expect(
      dueWords(vi, describeMyWorkDue(at(14), NOW), { timeOnToday: true }),
    ).toEqual({ text: "Thứ Tư 14/10", tone: "normal" });
    expect(
      dueWords(vi, describeMyWorkDue(null, NOW), { timeOnToday: true }).text,
    ).toBe("Không có hạn");
  });

  it("uses the singular for one day in English", () => {
    expect(
      dueWords(en, describeMyWorkDue(at(11), NOW), { timeOnToday: true }).text,
    ).toBe("Overdue 1 day");
    expect(
      dueWords(en, describeMyWorkDue(at(9), NOW), { timeOnToday: true }).text,
    ).toBe("Overdue 3 days");
  });
});

describe("greetingPeriod", () => {
  const hour = (h: number) => new Date(2026, 9, 12, h, 0).getTime();
  it("follows the member's local hour", () => {
    expect(greetingPeriod(hour(6))).toBe("morning");
    expect(greetingPeriod(hour(10))).toBe("morning");
    expect(greetingPeriod(hour(11))).toBe("noon");
    expect(greetingPeriod(hour(13))).toBe("afternoon");
    expect(greetingPeriod(hour(17))).toBe("afternoon");
    expect(greetingPeriod(hour(18))).toBe("evening");
    expect(greetingPeriod(hour(23))).toBe("evening");
  });
});
