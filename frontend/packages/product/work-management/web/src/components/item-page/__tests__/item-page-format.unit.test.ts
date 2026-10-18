import { describe, expect, it } from "vitest";
import type { CardActivity, CardUpdate } from "@notrelix/work-management-core";
import { en, vi } from "../item-page.copy";
import {
  buildActivityDays,
  countActivity,
  describeFile,
  formatDateTime,
  formatDayHeading,
  formatEditedAt,
  formatFileSize,
  formatFullDate,
} from "../item-page-format";

const NOW = new Date("2026-10-12T10:40:00+07:00");
const ZONE = "Asia/Ho_Chi_Minh";

const member = {
  id: "m",
  userId: "u",
  name: "Quân",
  initials: "MQ",
  color: "#000",
};

const update = (id: string, createdAt: string): CardUpdate => ({
  id,
  cardId: "c",
  author: member,
  body: id,
  mentionUserIds: [],
  attachmentIds: [],
  createdAt,
});

const event = (id: string, createdAt: string): CardActivity => ({
  id,
  cardId: "c",
  actor: "Quân",
  action: id,
  createdAt,
});

describe("dates", () => {
  it("writes the full date and the date with the time in the given zone", () => {
    expect(formatFullDate("2026-10-01T01:00:00+07:00", ZONE)).toBe(
      "01/10/2026",
    );
    expect(formatDateTime("2026-09-23T15:10:00+07:00", ZONE)).toBe(
      "23/09/2026, 15:10",
    );
    expect(formatFullDate(null)).toBeNull();
    expect(formatFullDate("not a date")).toBeNull();
  });

  it("words the last edit as today, yesterday or a date", () => {
    expect(formatEditedAt("2026-10-12T10:06:00+07:00", NOW, vi, ZONE)).toBe(
      "10:06 hôm nay",
    );
    expect(formatEditedAt("2026-10-11T16:40:00+07:00", NOW, vi, ZONE)).toBe(
      "16:40 hôm qua",
    );
    expect(formatEditedAt("2026-09-30T08:00:00+07:00", NOW, vi, ZONE)).toBe(
      "30/09",
    );
    expect(formatEditedAt("2026-10-12T10:06:00+07:00", NOW, en, ZONE)).toBe(
      "10:06 today",
    );
  });

  it("heads a day of the activity list", () => {
    expect(formatDayHeading("2026-10-12T09:12:00+07:00", NOW, vi, ZONE)).toBe(
      "Hôm nay",
    );
    expect(formatDayHeading("2026-10-11T16:40:00+07:00", NOW, vi, ZONE)).toBe(
      "Hôm qua",
    );
    expect(formatDayHeading("2026-10-05T14:20:00+07:00", NOW, vi, ZONE)).toBe(
      "05/10",
    );
  });
});

describe("files", () => {
  it("writes sizes with the locale's decimal mark", () => {
    expect(formatFileSize(19_293_798, "vi")).toBe("18,4 MB");
    expect(formatFileSize(19_293_798, "en")).toBe("18.4 MB");
    expect(formatFileSize(348_160, "vi")).toBe("340 KB");
    expect(formatFileSize(12, "vi")).toBe("12 B");
  });

  it("names the type chip and its tone", () => {
    expect(describeFile({ name: "a.xlsx", contentType: "" })).toEqual({
      label: "XLSX",
      kind: "sheet",
    });
    expect(describeFile({ name: "a.pdf", contentType: "" })).toEqual({
      label: "PDF",
      kind: "pdf",
    });
    expect(describeFile({ name: "a.docx", contentType: "x" })).toEqual({
      label: "DOCX",
      kind: "other",
    });
    expect(describeFile({ name: "README", contentType: "" }).label).toBe(
      "FILE",
    );
  });
});

describe("activity", () => {
  const updates = [
    update("c2", "2026-10-12T10:05:00+07:00"),
    update("c1", "2026-10-11T16:40:00+07:00"),
  ];
  const activity = [
    event("e1", "2026-10-11T08:00:00+07:00"),
    event("e2", "2026-10-12T10:06:00+07:00"),
  ];
  const base = { updates, activity, now: NOW, copy: vi, timeZone: ZONE };

  it("counts every tab", () => {
    expect(countActivity(updates, activity)).toEqual({
      all: 4,
      comments: 2,
      history: 2,
    });
  });

  it("merges comments and history by day, oldest first", () => {
    const { days } = buildActivityDays({
      ...base,
      tab: "all",
      newestFirst: false,
    });
    expect(days.map((day) => day.heading)).toEqual(["Hôm qua", "Hôm nay"]);
    expect(days.flatMap((day) => day.entries).map((entry) => entry.at)).toEqual(
      [
        "2026-10-11T08:00:00+07:00",
        "2026-10-11T16:40:00+07:00",
        "2026-10-12T10:05:00+07:00",
        "2026-10-12T10:06:00+07:00",
      ],
    );
  });

  it("filters by tab and can run newest first", () => {
    const { days: comments } = buildActivityDays({
      ...base,
      tab: "comments",
      newestFirst: true,
    });
    expect(
      comments.flatMap((day) => day.entries).map((entry) => entry.kind),
    ).toEqual(["comment", "comment"]);
    expect(comments[0]?.heading).toBe("Hôm nay");
    const { days: history } = buildActivityDays({
      ...base,
      tab: "history",
      newestFirst: false,
    });
    expect(
      history.flatMap((day) => day.entries).map((entry) => entry.kind),
    ).toEqual(["event", "event"]);
  });

  it("is empty without entries", () => {
    expect(
      buildActivityDays({
        ...base,
        updates: [],
        activity: [],
        tab: "all",
        newestFirst: false,
      }),
    ).toEqual({ days: [], hidden: [] });
  });

  it("folds the older entries behind a limit and hands them back", () => {
    const { days, hidden } = buildActivityDays({
      ...base,
      tab: "all",
      newestFirst: false,
      limit: 3,
    });
    expect(hidden.map((entry) => entry.at)).toEqual([
      "2026-10-11T08:00:00+07:00",
    ]);
    expect(
      days.flatMap((day) => day.entries).map((entry) => entry.at),
    ).toHaveLength(3);
  });
});
