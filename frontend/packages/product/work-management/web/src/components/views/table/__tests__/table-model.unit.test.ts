import { describe, expect, it } from "vitest";
import type { Card } from "@notrelix/work-management-core";
import { en, mainTableCopy, vi } from "../main-table.copy";
import {
  buildTableRow,
  formatUpdatedAt,
  type TableStatusOption,
} from "../table-model";

const NOW = new Date("2026-10-12T10:40:00+07:00");
const TZ = "Asia/Ho_Chi_Minh";
const ago = (ms: number) => new Date(NOW.getTime() - ms).toISOString();
const MIN = 60_000;
const HOUR = 60 * MIN;
const DAY = 24 * HOUR;

describe("formatUpdatedAt", () => {
  const f = (iso: string | undefined, copy = vi.row.updated) =>
    formatUpdatedAt(iso, NOW, copy, TZ);

  it("counts minutes and hours within the same day", () => {
    expect(f(ago(20_000))).toBe("Vừa xong");
    expect(f(ago(35 * MIN))).toBe("35 phút trước");
    expect(f(ago(2 * HOUR))).toBe("2 giờ trước");
  });

  it("says Hôm qua for the calendar day before, even a few hours ago", () => {
    // 10:40 minus 11 hours is 23:40 of the day before.
    expect(f(ago(11 * HOUR))).toBe("Hôm qua");
    expect(f(ago(DAY))).toBe("Hôm qua");
  });

  it("counts days up to six, then Tuần trước, then the date", () => {
    expect(f(ago(2 * DAY))).toBe("2 ngày trước");
    expect(f(ago(6 * DAY))).toBe("6 ngày trước");
    expect(f(ago(7 * DAY))).toBe("Tuần trước");
    expect(f(ago(13 * DAY))).toBe("Tuần trước");
    expect(f(ago(20 * DAY))).toBe("22/09");
  });

  it("has an English form and an empty one for no value", () => {
    expect(f(ago(2 * HOUR), en.row.updated)).toBe("2 hours ago");
    expect(f(ago(DAY), en.row.updated)).toBe("Yesterday");
    expect(f(undefined)).toBe("");
    expect(f("not a date")).toBe("");
  });
});

function card(patch: Partial<Card>): Card {
  return {
    id: "item-1",
    listId: "group-1",
    boardId: "board-1",
    workspaceId: "workspace-1",
    title: "Một việc",
    position: 0,
    status: "",
    isArchived: false,
    isDeleted: false,
    members: [],
    labels: [],
    checklists: [],
    fieldValues: {},
    _count: { comments: 0, attachments: 0, checklistItems: 0 },
    createdAt: "2026-10-01T03:00:00.000Z",
    ...patch,
  };
}

const status: TableStatusOption = {
  id: "status-doing",
  name: "Đang làm",
  category: "in_progress",
};

function row(patch: Partial<Card>, copy = mainTableCopy("vi")) {
  return buildTableRow(card(patch), {
    now: NOW,
    copy,
    timeZone: TZ,
    statuses: new Map([[status.id, status]]),
  });
}

describe("buildTableRow", () => {
  it("states the due date as overdue days, today or the date", () => {
    expect(row({ dueAt: "2026-10-10T10:00:00.000Z" }).due).toEqual({
      kind: "overdue",
      text: "Quá hạn 2 ngày",
    });
    expect(row({ dueAt: "2026-10-12T10:00:00.000Z" }).due).toEqual({
      kind: "today",
      text: "Hôm nay",
    });
    expect(row({ dueAt: "2026-10-18T10:00:00.000Z" }).due).toEqual({
      kind: "date",
      text: "18/10",
    });
    expect(row({}).due).toEqual({ kind: "none", text: "Chưa đặt" });
  });

  it("shows the plain date for a finished item, never overdue", () => {
    expect(
      row({ dueAt: "2026-10-01T10:00:00.000Z", statusCategory: "done" }).due,
    ).toEqual({ kind: "date", text: "01/10" });
  });

  it("resolves the status option and the first assignee", () => {
    const built = row({
      statusOptionId: "status-doing",
      members: [
        {
          id: "m1",
          userId: "user-1",
          name: "Trần Minh Quân",
          initials: "mq",
          color: "#000",
        },
        {
          id: "m2",
          userId: "user-2",
          name: "Vũ Thanh",
          initials: "VT",
          color: "#000",
        },
      ],
    });
    expect(built.status).toEqual(status);
    expect(built.assignee).toEqual({
      userId: "user-1",
      name: "Trần Minh Quân",
      initials: "MQ",
      more: 1,
    });
    expect(row({ statusOptionId: "unknown" }).status).toBeNull();
  });

  it("keeps the child progress only when there are child items", () => {
    expect(row({ childProgress: { done: 0, total: 0 } }).progress).toBeNull();
    expect(row({ childProgress: { done: 3, total: 5 } }).progress).toEqual({
      done: 3,
      total: 5,
      percent: 60,
    });
    // A bar never shrinks below a sliver, so 0 of 4 stays visible.
    expect(
      row({ childProgress: { done: 0, total: 4 } }).progress?.percent,
    ).toBe(4);
  });

  it("takes the label tone from the label", () => {
    expect(
      row({ labels: [{ id: "l1", name: "Bảo mật", color: "#C8143F" }] }).label,
    ).toEqual({ name: "Bảo mật", tone: "security" });
    expect(row({}).label).toBeNull();
  });
});
