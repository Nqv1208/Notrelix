import { describe, expect, it } from "vitest";
import { bucketKeyOf, dayOf } from "./view-state";

// 2026-10-18T20:00:00Z is 03:00 on Monday 19/10 in Hà Nội (UTC+7) but still Sunday 18/10 in UTC.
const SUNDAY_EVENING_UTC = "2026-10-18T20:00:00Z";

describe("a day boundary in the workspace time zone", () => {
  it("reads the day in the workspace zone, not UTC", () => {
    expect(dayOf(SUNDAY_EVENING_UTC, "Asia/Ho_Chi_Minh")).toBe("2026-10-19");
    expect(dayOf(SUNDAY_EVENING_UTC)).toBe("2026-10-18");
  });

  it("puts the same instant in the next week bucket once the zone is applied", () => {
    const hanoi = dayOf(SUNDAY_EVENING_UTC, "Asia/Ho_Chi_Minh")!;
    const utc = dayOf(SUNDAY_EVENING_UTC)!;
    expect(bucketKeyOf(hanoi, "week")).toBe("2026-10-19");
    expect(bucketKeyOf(utc, "week")).toBe("2026-10-12");
  });

  it("keeps a plain date as it is in every zone", () => {
    expect(dayOf("2026-10-19", "Asia/Ho_Chi_Minh")).toBe("2026-10-19");
    expect(dayOf("2026-10-19", "America/Los_Angeles")).toBe("2026-10-19");
  });
});
