import { describe, expect, it } from "vitest";
import type { MyWorkItem } from "@notrelix/features-workspace/core";
import {
  greetingNameFor,
  toMyWorkGroups,
  toMyWorkItems,
} from "../my-work-view";

const NOW = new Date(2026, 9, 12, 10, 40).getTime();
const due = (day: number) => new Date(2026, 9, day, 9, 0).toISOString();

function item(id: string, boardId: string, dueAt: string | null): MyWorkItem {
  return {
    id,
    boardId,
    groupId: "g",
    name: id,
    dueAt,
    startedAt: null,
    updatedAt: null,
    version: 1,
  };
}

describe("toMyWorkGroups", () => {
  it("adds the Board name and department only where the member can see them", () => {
    const groups = toMyWorkGroups(
      [
        item("a", "b1", due(10)),
        item("b", "b2", due(12)),
        item("c", "gone", null),
      ],
      new Map([
        ["b1", "Triển khai ERP Q4"],
        ["b2", "Đóng sổ tháng 10"],
      ]),
      NOW,
      new Map([["b1", { name: "Công nghệ", markTone: "blue" as const }]]),
    );
    expect(groups.map((group) => group.range)).toEqual([
      "overdue",
      "today",
      "no-date",
    ]);
    const [overdue, today, noDate] = groups;
    expect(overdue?.items[0]).toMatchObject({
      boardName: "Triển khai ERP Q4",
      department: { name: "Công nghệ", markTone: "blue" },
    });
    expect(today?.items[0]).toMatchObject({
      boardName: "Đóng sổ tháng 10",
      department: null,
    });
    expect(noDate?.items[0]).toMatchObject({
      boardName: null,
      department: null,
    });
  });

  it("fills nothing the My work read does not carry", () => {
    const [group] = toMyWorkGroups([item("a", "b1", due(12))], new Map(), NOW);
    const row = group?.items[0];
    expect(row?.key).toBeUndefined();
    expect(row?.status).toBeUndefined();
    expect(row?.priority).toBeUndefined();
    expect(row?.reason).toBeUndefined();
  });
});

describe("toMyWorkItems", () => {
  it("keeps the read's order, adds the Board and department, and states only that the item is assigned", () => {
    const rows = toMyWorkItems(
      [item("a", "b1", due(10)), item("b", "gone", null)],
      new Map([["b1", "Triển khai ERP Q4"]]),
      new Map([["b1", { name: "Công nghệ", markTone: "blue" as const }]]),
    );
    expect(rows.map((row) => row.id)).toEqual(["a", "b"]);
    expect(rows[0]).toMatchObject({
      boardName: "Triển khai ERP Q4",
      department: { name: "Công nghệ", markTone: "blue" },
      reason: "mine",
    });
    expect(rows[1]).toMatchObject({
      boardName: null,
      department: null,
      reason: "mine",
    });
    expect(rows[0]?.key).toBeUndefined();
    expect(rows[0]?.status).toBeUndefined();
    expect(rows[0]?.priority).toBeUndefined();
  });
});

describe("greetingNameFor", () => {
  it("greets a Vietnamese name by its last word and an English one by its first", () => {
    expect(greetingNameFor("Nguyễn Thu Hà", "vi")).toBe("Hà");
    expect(greetingNameFor("Ada Lovelace", "en")).toBe("Ada");
  });

  it("drops a trailing role in parentheses and greets nobody without a name", () => {
    expect(greetingNameFor("UI Developer (Owner)", "en")).toBe("UI");
    expect(greetingNameFor("UI Developer (Owner)", "vi")).toBe("Developer");
    expect(greetingNameFor("   ", "vi")).toBeNull();
    expect(greetingNameFor(null, "vi")).toBeNull();
    expect(greetingNameFor(undefined, "en")).toBeNull();
  });
});
