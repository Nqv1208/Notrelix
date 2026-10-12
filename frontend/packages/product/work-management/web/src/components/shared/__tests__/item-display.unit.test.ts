import { describe, expect, it } from "vitest";
import {
  calendarDaysFromToday,
  formatItemDate,
  itemDueState,
  itemLabelTone,
} from "../item-display";

const HCM = "Asia/Ho_Chi_Minh";
const NOW = new Date("2026-10-12T10:40:00+07:00");

describe("itemDueState", () => {
  it("reads overdue, today and later dates by calendar day", () => {
    expect(itemDueState("2026-10-10T17:00:00+07:00", NOW, HCM)).toEqual({
      kind: "overdue",
      days: 2,
      text: "10/10",
    });
    expect(itemDueState("2026-10-12T08:00:00+07:00", NOW, HCM)).toEqual({
      kind: "today",
      text: "12/10",
    });
    expect(itemDueState("2026-10-18T17:00:00+07:00", NOW, HCM)).toEqual({
      kind: "date",
      text: "18/10",
    });
    expect(itemDueState("2027-01-18T17:00:00+07:00", NOW, HCM)).toEqual({
      kind: "date",
      text: "18/01/2027",
    });
  });

  it("judges near-midnight times in the given time zone, not UTC", () => {
    // 00:10 and 23:30 on 12/10 in Hồ Chí Minh are 11/10 17:10Z and 12/10 16:30Z.
    const justAfterMidnight = new Date("2026-10-12T00:10:00+07:00");
    const lateTonight = "2026-10-12T23:30:00+07:00";
    expect(itemDueState(lateTonight, justAfterMidnight, HCM)?.kind).toBe(
      "today",
    );
    expect(itemDueState(lateTonight, justAfterMidnight, "UTC")).toEqual({
      kind: "date",
      text: "12/10",
    });
    // 23:50 on 11/10 is overdue by one day once it is past midnight.
    expect(
      itemDueState("2026-10-11T23:50:00+07:00", justAfterMidnight, HCM),
    ).toEqual({ kind: "overdue", days: 1, text: "11/10" });
  });

  it("ignores absent or unparseable dates", () => {
    expect(itemDueState(null, NOW, HCM)).toBeNull();
    expect(itemDueState("not a date", NOW, HCM)).toBeNull();
    expect(calendarDaysFromToday("not a date", NOW, HCM)).toBeNull();
    expect(formatItemDate("not a date", NOW, HCM)).toBeNull();
  });
});

describe("itemLabelTone", () => {
  it("maps names first, then the colour hue, then doc", () => {
    expect(itemLabelTone({ name: "Dữ liệu" })).toBe("data");
    expect(itemLabelTone({ name: "Bảo mật", color: "#00ff00" })).toBe(
      "security",
    );
    expect(itemLabelTone({ name: "Khác", color: "#2F6FEB" })).toBe("data");
    expect(itemLabelTone({ name: "Khác", color: "#888888" })).toBe("doc");
    expect(itemLabelTone({ name: "Khác" })).toBe("doc");
  });
});
