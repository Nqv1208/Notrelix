import { describe, expect, it } from "vitest";
import {
  createFormatters,
  formatDate,
  formatList,
  formatNumber,
  formatPercent,
  formatRelativeTime,
} from "../format";

const date = new Date(Date.UTC(2026, 9, 10, 12, 0, 0));

describe("Intl formatting helpers", () => {
  it("formats numbers per locale (vi groups with dots, en with commas)", () => {
    expect(formatNumber("vi", 1234567.5)).toBe("1.234.567,5");
    expect(formatNumber("en", 1234567.5)).toBe("1,234,567.5");
    expect(formatPercent("en", 0.42)).toBe("42%");
  });

  it("formats dates per locale", () => {
    const options = {
      timeZone: "UTC",
      day: "numeric",
      month: "numeric",
      year: "numeric",
    } as const;
    expect(formatDate("vi", date, options)).toBe("10/10/2026");
    expect(formatDate("en", date, options)).toBe("10/10/2026");
    expect(
      formatDate("en", date, {
        timeZone: "UTC",
        month: "long",
        day: "numeric",
      }),
    ).toBe("October 10");
    expect(formatDate("vi", date, { timeZone: "UTC", month: "long" })).toMatch(
      /tháng 10/i,
    );
  });

  it("formats relative time with the largest whole unit", () => {
    const now = date.getTime();
    expect(formatRelativeTime("en", now - 3 * 24 * 3600 * 1000, now)).toBe(
      "3 days ago",
    );
    expect(formatRelativeTime("vi", now - 3 * 24 * 3600 * 1000, now)).toBe(
      "3 ngày trước",
    );
    expect(formatRelativeTime("en", now + 2 * 3600 * 1000, now)).toBe(
      "in 2 hours",
    );
  });

  it("formats lists and binds formatters to a locale", () => {
    expect(formatList("en", ["A", "B", "C"])).toBe("A, B, and C");
    expect(formatList("vi", ["A", "B"])).toBe("A và B");
    const f = createFormatters("vi");
    expect(f.locale).toBe("vi");
    expect(f.number(1000)).toBe("1.000");
  });
});
