import { describe, expect, it } from "vitest";
import {
  applyFilters,
  departmentCounts,
  groupByBoard,
  groupByDone,
  groupByDue,
  matchesQuery,
  NO_MY_WORK_FILTERS,
  overdueSummary,
  tabCounts,
  weekDays,
  weekRange,
  weekWindowEnd,
} from "../web/home/my-work-model";
import {
  HOME_MOCKUP_NOW,
  homeMockupItems,
  myWorkDoneItems,
} from "../verification/workspace-home-fixtures";

const NOW = HOME_MOCKUP_NOW;
const items = homeMockupItems();

describe("tabCounts", () => {
  it("counts every item and each relationship the items state", () => {
    expect(tabCounts(items)).toEqual({
      all: 7,
      mine: 4,
      approve: 1,
      follow: 2,
    });
  });
});

describe("matchesQuery", () => {
  const kt88 = items.find((item) => item.key === "KT-88")!;
  it("matches the title, the Board and the key without case or diacritics", () => {
    expect(matchesQuery(kt88, "SAO KE")).toBe(true);
    expect(matchesQuery(kt88, "dong so thang")).toBe(true);
    expect(matchesQuery(kt88, "kt-88")).toBe(true);
    expect(matchesQuery(kt88, "ERP")).toBe(false);
    expect(matchesQuery(kt88, "   ")).toBe(true);
  });
});

describe("applyFilters", () => {
  it("keeps the items of the tab, the departments, the Boards, the day and the query together", () => {
    const base = NO_MY_WORK_FILTERS;
    expect(applyFilters(items, "follow", base, NOW).map((i) => i.key)).toEqual([
      "ERP-142",
      "ERP-144",
    ]);
    expect(
      applyFilters(
        items,
        "all",
        { ...base, departments: ["Kế toán"] },
        NOW,
      ).map((i) => i.key),
    ).toEqual(["KT-88", "KT-95"]);
    expect(
      applyFilters(items, "all", { ...base, boards: ["board-mk"] }, NOW).map(
        (i) => i.key,
      ),
    ).toEqual(["MK-21"]);
    expect(
      applyFilters(items, "all", { ...base, dueOn: "12/10" }, NOW).map(
        (i) => i.key,
      ),
    ).toEqual(["ERP-144", "KT-88"]);
    expect(
      applyFilters(items, "all", { ...base, query: "sso" }, NOW).map(
        (i) => i.key,
      ),
    ).toEqual(["ERP-138"]);
  });

  it("leaves nothing in the Done tab (completed items come from another read)", () => {
    expect(applyFilters(items, "done", NO_MY_WORK_FILTERS, NOW)).toEqual([]);
  });

  it("drops an item without a department when a department is chosen", () => {
    const orphan = { ...items[0]!, department: null };
    expect(
      applyFilters(
        [orphan],
        "all",
        { ...NO_MY_WORK_FILTERS, departments: ["Công nghệ"] },
        NOW,
      ),
    ).toEqual([]);
  });
});

describe("groupByDue and groupByBoard", () => {
  it("puts the seven items into the four due windows of the mockup", () => {
    expect(
      groupByDue(items, NOW).map((group) => [group.id, group.items.length]),
    ).toEqual([
      ["overdue", 1],
      ["today", 2],
      ["this-week", 2],
      ["later", 2],
    ]);
  });

  it("groups by Board in the order the Boards first appear", () => {
    const groups = groupByBoard(items);
    expect(groups.map((group) => [group.name, group.items.length])).toEqual([
      ["Triển khai ERP Q4", 4],
      ["Đóng sổ tháng 10", 2],
      ["Ra mắt sản phẩm Q4", 1],
    ]);
    expect(groups[0]!.department?.name).toBe("Công nghệ");
  });
});

describe("the week", () => {
  it("lists seven local days from today and counts the items due on each", () => {
    const days = weekDays(items, NOW);
    expect(days.map((day) => day.shortDate)).toEqual([
      "12/10",
      "13/10",
      "14/10",
      "15/10",
      "16/10",
      "17/10",
      "18/10",
    ]);
    expect(days.map((day) => day.count)).toEqual([2, 0, 1, 0, 1, 0, 0]);
    expect(days[0]!.today).toBe(true);
    expect(days.filter((day) => day.today)).toHaveLength(1);
    expect(weekRange(days)).toBe("12–18/10");
  });

  it("writes a range across two months in full", () => {
    const late = new Date(2026, 9, 29, 9, 0).getTime();
    expect(weekRange(weekDays([], late))).toBe("29/10–04/11");
  });

  it("ends the window six days after today", () => {
    expect(weekWindowEnd(NOW)).toEqual({ weekday: 0, shortDate: "18/10" });
  });

  it("sums the overdue items and the earliest overdue day", () => {
    const summary = overdueSummary(items, NOW);
    expect(summary.count).toBe(1);
    expect(summary.since?.shortDate).toBe("10/10");
    expect(summary.since?.weekday).toBe(6);
    expect(overdueSummary([], NOW)).toEqual({ count: 0, since: null });
  });
});

describe("departmentCounts", () => {
  it("counts open items per department, the most first, and skips items without one", () => {
    const counts = departmentCounts([
      ...items,
      { ...items[0]!, id: "x", department: null },
    ]);
    expect(counts.map((entry) => [entry.department.name, entry.count])).toEqual(
      [
        ["Công nghệ", 4],
        ["Kế toán", 2],
        ["Marketing", 1],
      ],
    );
  });
});

describe("groupByDone", () => {
  it("groups completed items by calendar week, newest first", () => {
    const groups = groupByDone(myWorkDoneItems(), NOW);
    expect(
      groups.map((group) => [group.id, group.items.map((i) => i.key)]),
    ).toEqual([
      ["today", ["KT-86"]],
      ["last-week", ["ERP-136"]],
    ]);
  });

  it("calls earlier days of the same calendar week 'week' and older ones 'earlier'", () => {
    const tuesday = new Date(2026, 9, 14, 12, 0).getTime();
    const [item] = myWorkDoneItems();
    const monday = {
      ...item!,
      completedAt: new Date(2026, 9, 12, 9, 0).toISOString(),
    };
    const older = {
      ...item!,
      id: "old",
      completedAt: new Date(2026, 8, 1, 9, 0).toISOString(),
    };
    expect(
      groupByDone([monday, older], tuesday).map((group) => group.id),
    ).toEqual(["week", "earlier"]);
  });
});
