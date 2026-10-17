import { describe, expect, it } from "vitest";
import { workspaceInboxCopy } from "../web/inbox/workspace-inbox.copy";
import {
  inboxDayLabel,
  inboxTimeLabel,
} from "../web/inbox/workspace-inbox-surface";

const vi = workspaceInboxCopy("vi");
const en = workspaceInboxCopy("en");
const NOW = new Date(2026, 9, 12, 10, 40).getTime();
const at = (day: number, hour: number, minute: number) =>
  new Date(2026, 9, day, hour, minute).toISOString();

describe("inboxTimeLabel", () => {
  it("says 'now' under a minute, minutes under an hour, one hour under two", () => {
    expect(inboxTimeLabel(vi, at(12, 10, 40), NOW)).toBe("vừa xong");
    expect(inboxTimeLabel(vi, at(12, 10, 28), NOW)).toBe("12 phút");
    expect(inboxTimeLabel(vi, at(12, 9, 40), NOW)).toBe("1 giờ");
    expect(inboxTimeLabel(en, at(12, 10, 28), NOW)).toBe("12 min");
    expect(inboxTimeLabel(en, at(12, 9, 40), NOW)).toBe("1 h");
  });

  it("falls back to the clock time later today and on other days", () => {
    expect(inboxTimeLabel(vi, at(12, 8, 0), NOW)).toBe("08:00");
    expect(inboxTimeLabel(vi, at(11, 17, 10), NOW)).toBe("17:10");
    expect(inboxTimeLabel(vi, at(5, 9, 5), NOW)).toBe("09:05");
  });

  it("never says 'ago' for a notification dated after now, and ignores an unreadable date", () => {
    expect(inboxTimeLabel(vi, at(12, 11, 5), NOW)).toBe("11:05");
    expect(inboxTimeLabel(vi, "nope", NOW)).toBe("");
  });
});

describe("inboxDayLabel", () => {
  it("names today and yesterday and writes dd/MM before that", () => {
    expect(inboxDayLabel(vi, at(12, 0, 0), NOW)).toBe("Hôm nay");
    expect(inboxDayLabel(vi, at(12, 23, 59), NOW)).toBe("Hôm nay");
    expect(inboxDayLabel(vi, at(11, 23, 59), NOW)).toBe("Hôm qua");
    expect(inboxDayLabel(en, at(11, 1, 0), NOW)).toBe("Yesterday");
    expect(inboxDayLabel(vi, at(9, 12, 0), NOW)).toBe("09/10");
    expect(inboxDayLabel(vi, "nope", NOW)).toBe("");
  });
});
