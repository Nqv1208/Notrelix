import { describe, expect, it } from "vitest";
import {
  EMPTY_KANBAN_FILTER,
  countKanbanFilter,
  isKanbanFilterActive,
  parseKanbanFilter,
  serializeKanbanFilter,
  type KanbanFilterState,
} from "../kanban-filter";

const FULL: KanbanFilterState = {
  statusOptionIds: ["0b7c3a52-1f7e-4d55-9a0e-2f1e6c0d9a11", "status-doing"],
  priorities: ["urgent", "low"],
  assigneeIds: ["user-an"],
  labelIds: ["label.with~separators", "nhãn có dấu/và khoảng trắng"],
  overdue: true,
};

describe("serializeKanbanFilter / parseKanbanFilter", () => {
  it("serialises an empty filter to an empty string", () => {
    expect(serializeKanbanFilter(EMPTY_KANBAN_FILTER)).toBe("");
    expect(parseKanbanFilter("")).toEqual(EMPTY_KANBAN_FILTER);
  });

  it("round-trips every facet, including ids that contain separators", () => {
    const serialized = serializeKanbanFilter(FULL);
    expect(parseKanbanFilter(serialized)).toEqual(FULL);
  });

  it("produces a string that needs no escaping in a URL query", () => {
    const serialized = serializeKanbanFilter(FULL);
    expect(serialized).toMatch(/^[A-Za-z0-9\-._~%]+$/);
    expect(new URLSearchParams({ filter: serialized }).get("filter")).toBe(
      serialized,
    );
    const viaUrl = new URLSearchParams(
      new URLSearchParams({ filter: serialized }).toString(),
    ).get("filter");
    expect(parseKanbanFilter(viaUrl)).toEqual(FULL);
  });

  it("is canonical: priority order and duplicates do not change the string", () => {
    const a = serializeKanbanFilter({
      ...EMPTY_KANBAN_FILTER,
      priorities: ["low", "urgent", "low"],
      assigneeIds: ["user-an", "user-an"],
    });
    const b = serializeKanbanFilter({
      ...EMPTY_KANBAN_FILTER,
      priorities: ["urgent", "low"],
      assigneeIds: ["user-an"],
    });
    expect(a).toBe(b);
  });

  it("serialises the overdue-and-low filter of the no-results state readably", () => {
    expect(
      serializeKanbanFilter({
        ...EMPTY_KANBAN_FILTER,
        priorities: ["low"],
        overdue: true,
      }),
    ).toBe("p.low~o");
  });

  it.each([
    ["undefined", undefined],
    ["null", null],
    ["a number", 42],
    ["an object", { overdue: true }],
    ["random text", "%%%garbage"],
    ["unknown facets", "x.1~y~zz.2"],
    ["an unknown priority", "p.critical"],
    ["a malformed escape", "s.%E0%A4%A"],
    ["separators only", "~~..~"],
  ])("tolerates %s by returning only what it can read", (_name, value) => {
    expect(parseKanbanFilter(value)).toEqual(EMPTY_KANBAN_FILTER);
  });

  it("keeps the readable facets when others are garbage", () => {
    expect(parseKanbanFilter("p.high.critical~bogus~o~s.%ZZ.doing")).toEqual({
      ...EMPTY_KANBAN_FILTER,
      statusOptionIds: ["doing"],
      priorities: ["high"],
      overdue: true,
    });
  });
});

describe("isKanbanFilterActive / countKanbanFilter", () => {
  it("is inactive and zero for the empty filter", () => {
    expect(isKanbanFilterActive(EMPTY_KANBAN_FILTER)).toBe(false);
    expect(countKanbanFilter(EMPTY_KANBAN_FILTER)).toBe(0);
  });

  it("counts every selected value and the overdue flag", () => {
    expect(isKanbanFilterActive(FULL)).toBe(true);
    expect(countKanbanFilter(FULL)).toBe(8);
  });
});
