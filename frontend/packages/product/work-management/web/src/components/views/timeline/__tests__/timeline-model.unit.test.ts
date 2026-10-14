import { describe, expect, it } from "vitest";
import {
  TIMELINE_DRAG_PREVIEW,
  TIMELINE_STORY_TODAY,
  timelineMockupGroups,
  timelineMockupLinks,
} from "../../../../verification/timeline-fixtures";
import {
  buildHeader,
  fmtDay,
  itemSpan,
  layoutTimeline,
  scaleSpec,
  stepTimelineAnchor,
  type TaskLayout,
  type TimelineGroup,
  type TimelineItem,
  type TimelineLayout,
  type TimelineScale,
} from "../timeline-model";
import { en, vi } from "../timeline.copy";

const TODAY = TIMELINE_STORY_TODAY;

function layoutOf(
  scale: TimelineScale,
  overrides: {
    groups?: readonly TimelineGroup[];
    drag?: Parameters<typeof layoutTimeline>[0]["drag"];
    criticalPath?: boolean;
    links?: Parameters<typeof layoutTimeline>[0]["links"];
    copy?: typeof vi;
  } = {},
): TimelineLayout {
  return layoutTimeline({
    scale,
    spec: scaleSpec(scale, TODAY),
    groups: overrides.groups ?? timelineMockupGroups(),
    links: overrides.links ?? timelineMockupLinks(),
    today: TODAY,
    copy: overrides.copy ?? vi,
    criticalPath: overrides.criticalPath ?? false,
    drag: overrides.drag ?? null,
  });
}

function task(layout: TimelineLayout, id: string): TaskLayout {
  const row = layout.rows.find(
    (candidate): candidate is TaskLayout =>
      candidate.type === "task" && candidate.id === id,
  );
  if (!row) throw new Error(`no row ${id}`);
  return row;
}

function item(overrides: Partial<TimelineItem> & { id: string }): TimelineItem {
  return {
    kind: "item",
    itemKey: "ERP-1",
    title: "An item",
    tone: "todo",
    startDay: "2026-10-13",
    dueDay: "2026-10-15",
    percent: 0,
    movable: true,
    ...overrides,
  };
}

const single = (items: TimelineItem[]): TimelineGroup[] => [
  { id: "g", title: "Group", items },
];

describe("scaleSpec", () => {
  it("shows four weeks and a day from the Monday before last (ngay)", () => {
    const spec = scaleSpec("day", TODAY);
    expect(fmtDay(spec.from)).toBe("05/10");
    expect(spec.days).toBe(29);
    expect(fmtDay(spec.end - 1)).toBe("02/11");
    expect(spec.ppd).toBe(26);
    expect(spec.barTop).toBe(8);
  });

  it("shows five weeks and four days from two Mondays back (tuan)", () => {
    const spec = scaleSpec("week", TODAY);
    expect(fmtDay(spec.from)).toBe("28/09");
    expect(spec.days).toBe(39);
    expect(spec.ppd).toBe(19);
  });

  it("shows four months from the first of the previous one (thang)", () => {
    const spec = scaleSpec("month", TODAY);
    expect(fmtDay(spec.from)).toBe("01/09");
    expect(spec.days).toBe(122);
    expect(spec.ppd).toBe(6);
    expect(spec.barTop).toBe(10);
  });

  it("builds the same window from any day of the same week", () => {
    expect(scaleSpec("day", "2026-10-18")).toEqual(scaleSpec("day", TODAY));
    expect(scaleSpec("month", "2026-10-31")).toEqual(scaleSpec("month", TODAY));
  });

  it("crosses a year end", () => {
    const spec = scaleSpec("month", "2026-12-15");
    expect(fmtDay(spec.from)).toBe("01/11");
    expect(spec.days).toBe(30 + 31 + 31 + 28);
  });
});

describe("stepTimelineAnchor", () => {
  it("moves a week, two weeks or a month", () => {
    expect(stepTimelineAnchor("day", TODAY, 1)).toBe("2026-10-19");
    expect(stepTimelineAnchor("day", TODAY, -1)).toBe("2026-10-05");
    expect(stepTimelineAnchor("week", TODAY, 1)).toBe("2026-10-26");
    expect(stepTimelineAnchor("month", TODAY, 1)).toBe("2026-11-12");
    expect(stepTimelineAnchor("month", "2026-03-31", -1)).toBe("2026-02-28");
  });
});

describe("buildHeader", () => {
  it("draws 29 days, the weekend bands and the today line for ngay", () => {
    const header = buildHeader({
      scale: "day",
      spec: scaleSpec("day", TODAY),
      today: TODAY,
      copy: vi,
      locale: "vi",
    });
    expect(header.chartWidth).toBe(754);
    expect(header.days).toHaveLength(29);
    expect(header.days[0]).toMatchObject({ n: 5, weekday: "T2", today: false });
    expect(header.days[7]).toMatchObject({ n: 12, today: true });
    expect(header.days[5]).toMatchObject({ weekday: "T7", weekend: true });
    expect(header.bands.map((band) => band.x)).toEqual([
      130, 156, 312, 338, 494, 520, 676, 702,
    ]);
    expect(header.bands.every((band) => band.width === 26)).toBe(true);
    expect(header.months.map((month) => [month.width, month.label])).toEqual([
      [702, "Tháng 10, 2026"],
      [52, "Th 11"],
    ]);
    expect(header.todayX).toBe(194);
  });

  it("draws weeks with their ISO number and the current one marked for tuan", () => {
    const header = buildHeader({
      scale: "week",
      spec: scaleSpec("week", TODAY),
      today: TODAY,
      copy: vi,
      locale: "vi",
    });
    expect(header.weeks.map((week) => week.width)).toEqual([
      133, 133, 133, 133, 133, 76,
    ]);
    expect(header.weeks[0]?.label).toBe("Tuần 40 · 28/09–04/10");
    expect(header.weeks[4]?.label).toBe("Tuần 44 · 26/10–01/11");
    expect(header.weeks[5]?.label).toBe("Tuần 45");
    expect(header.weeks[2]?.current).toBe(true);
    expect(header.weeks.filter((week) => week.current)).toHaveLength(1);
    expect(header.months.map((month) => month.label)).toEqual([
      "Th 9",
      "Tháng 10, 2026",
      "Tháng 11",
    ]);
    expect(header.todayX).toBe(275);
  });

  it("draws Monday ticks and month rules for thang", () => {
    const header = buildHeader({
      scale: "month",
      spec: scaleSpec("month", TODAY),
      today: TODAY,
      copy: vi,
      locale: "vi",
    });
    expect(header.ticks).toHaveLength(17);
    expect(header.ticks[0]).toMatchObject({ x: 36, n: 7 });
    expect(header.ticks.find((tick) => tick.today)?.n).toBe(12);
    expect(header.bands.map((band) => [band.x, band.width])).toEqual([
      [180, 1],
      [366, 1],
      [546, 1],
    ]);
    expect(header.months.map((month) => month.label)).toEqual([
      "Tháng 9, 2026",
      "Tháng 10",
      "Tháng 11",
      "Tháng 12",
    ]);
  });

  it("names months in the locale", () => {
    const header = buildHeader({
      scale: "day",
      spec: scaleSpec("day", TODAY),
      today: TODAY,
      copy: en,
      locale: "en",
    });
    expect(header.months[0]?.label).toBe("October, 2026");
    expect(header.months[1]?.label).toBe("Nov");
    expect(header.days[0]?.weekday).toBe("Mon");
  });
});

describe("layoutTimeline (ngay)", () => {
  const layout = layoutOf("day");

  it("stacks 32 px group rows and 36 px item rows", () => {
    expect(layout.rows).toHaveLength(4 + 12);
    expect(layout.bodyHeight).toBe(4 * 32 + 12 * 36);
    const first = task(layout, "erp-129");
    expect(first.top).toBe(32);
    expect(first.cy).toBe(50);
    expect(task(layout, "erp-151").top).toBe(68);
    const groups = layout.rows.filter((row) => row.type === "group");
    expect(groups.map((row) => row.type === "group" && row.count)).toEqual([
      2, 3, 3, 4,
    ]);
  });

  it("places a bar by its start and due days, 1 px inside its cells", () => {
    const bar = task(layout, "erp-146").bar;
    expect(bar).toMatchObject({
      x: 209,
      width: 102,
      tone: "doing",
      percent: 33,
    });
    expect(bar?.clipStart).toBe(false);
    // 13 to 16 October: four days of 26 px, less the 1 px inset each side.
    expect(task(layout, "erp-151").bar?.width).toBe(154);
  });

  it("keeps the title inside a bar of 120 px or more and beside a shorter one", () => {
    expect(task(layout, "erp-151").bar?.label).toMatch(/^Cấu hình quyền/);
    expect(task(layout, "erp-151").label).toBeNull();
    expect(task(layout, "erp-146").bar?.label).toBe("");
    expect(task(layout, "erp-146").label?.text).toMatch(/^Đồng bộ tồn kho/);
  });

  it("clips a bar that starts before the window and hatches the overdue days", () => {
    const row = task(layout, "erp-142");
    expect(row.bar).toMatchObject({ x: 1, width: 154, clipStart: true });
    expect(row.late).toMatchObject({
      x: 155,
      width: 52,
      textLeft: 222,
      text: "Quá hạn 2 ngày",
    });
    expect(row.dateState).toBe("over");
    expect(row.aria).toBe(
      "ERP-142 Chuyển dữ liệu khách hàng từ hệ thống cũ sang ERP, 01/10–10/10, 60%, quá hạn 2 ngày, bắt đầu trước khoảng đang xem",
    );
  });

  it("draws a done item with only a finish date as a note before the window", () => {
    const row = task(layout, "erp-129");
    expect(row.bar).toBeNull();
    expect(row.label).toMatchObject({ text: "◂ Hoàn thành 30/09", left: 14 });
    expect(row.aria).toBe(
      "ERP-129 Chốt danh mục tài khoản kế toán dùng chung, hoàn thành 30/09, trước khoảng đang xem",
    );
    expect(row.late).toBeNull();
  });

  it("draws a done item with only a finish date inside the window as a dot", () => {
    const row = task(
      layoutOf("day", {
        groups: single([
          item({
            id: "done",
            tone: "done",
            startDay: null,
            dueDay: "2026-10-08",
          }),
        ]),
        links: [],
      }),
      "done",
    );
    // 08/10 is the fourth day: centre 91, the dot 10 px wide, the note 11 px on.
    expect(row.fin).toMatchObject({ x: 86 });
    expect(row.label).toMatchObject({ text: "Hoàn thành 08/10", left: 102 });
    expect(row.bar).toBeNull();
  });

  it("draws the milestone as a diamond with its title, not a bar", () => {
    const row = task(layout, "milestone-golive");
    expect(row.bar).toBeNull();
    expect(row.milestone).toMatchObject({ x: 708, labelLeft: 728 });
    expect(row.tag).toBe("Mốc");
    expect(row.hasKey).toBe(false);
    expect(row.aria).toBe("Mốc Go-live ERP, 01/11");
  });

  it("tags a phase and draws it as a bar", () => {
    const row = task(layout, "phase-uat");
    expect(row.tag).toBe("Giai đoạn");
    expect(row.bar).toMatchObject({ x: 183, width: 128, tone: "doing" });
  });

  it("routes the seven dependencies like the design", () => {
    expect(layout.segments).toHaveLength(17);
    expect(layout.heads).toHaveLength(7);
    expect(layout.heads.filter((head) => head.down)).toHaveLength(4);
    expect(layout.segments[0]).toEqual({
      x: 0,
      y: 50,
      w: 6,
      h: 1.5,
      critical: false,
    });
    expect(layout.segments[1]).toMatchObject({ x: 6, y: 50, w: 1.5, h: 88 });
    expect(layout.heads[0]).toMatchObject({ x: 3, y: 138, down: true });
  });

  it("moves a label clear of an arrow line in its row", () => {
    const row = task(layout, "erp-146");
    // 6 px after the bar would be 318; an arrow line crosses there.
    expect(row.label?.left).toBeGreaterThan(318);
  });

  it("draws no arrows when the dependencies are hidden", () => {
    const plain = layoutOf("day", { links: [] });
    expect(plain.segments).toHaveLength(0);
    expect(plain.heads).toHaveLength(0);
    expect(task(plain, "erp-146").label?.left).toBe(318);
  });

  it("counts items without a date and leaves them out", () => {
    const result = layoutOf("day", {
      groups: single([
        item({ id: "a" }),
        item({ id: "b", startDay: null, dueDay: null }),
        item({ id: "c", startDay: null, dueDay: null }),
      ]),
    });
    expect(result.undated).toBe(2);
    expect(result.rows.filter((row) => row.type === "task")).toHaveLength(1);
  });

  it("leaves a group with no dated item out", () => {
    const result = layoutOf("day", {
      groups: [
        { id: "empty", title: "Empty", items: [] },
        ...single([item({ id: "a" })]),
      ],
    });
    expect(result.rows.filter((row) => row.type === "group")).toHaveLength(1);
  });

  it("draws an item with only a due date as a one-day bar", () => {
    const row = task(
      layoutOf("day", {
        groups: single([
          item({ id: "a", startDay: null, dueDay: "2026-10-20" }),
        ]),
        links: [],
      }),
      "a",
    );
    expect(row.bar).toMatchObject({ x: 391, width: 24 });
    expect(row.dates).toBe("20/10");
  });

  it("draws an item with only a start date as a one-day bar on that day", () => {
    const row = task(
      layoutOf("day", {
        groups: single([
          item({ id: "a", startDay: "2026-10-20", dueDay: null }),
        ]),
        links: [],
      }),
      "a",
    );
    expect(row.bar).toMatchObject({ x: 391, width: 24 });
  });

  it("clips a bar at the end of the window", () => {
    const row = task(
      layoutOf("day", {
        groups: single([
          item({ id: "a", startDay: "2026-10-30", dueDay: "2026-11-20" }),
        ]),
        links: [],
      }),
      "a",
    );
    expect(row.bar?.clipEnd).toBe(true);
    expect((row.bar?.x ?? 0) + (row.bar?.width ?? 0)).toBeLessThanOrEqual(754);
  });

  it("notes an item due before the window and one that starts after it", () => {
    const before = task(
      layoutOf("day", {
        groups: single([
          item({ id: "a", startDay: "2026-09-01", dueDay: "2026-09-10" }),
        ]),
        links: [],
      }),
      "a",
    );
    expect(before.bar).toBeNull();
    expect(before.label?.text).toBe("◂ Hạn 10/09");
    expect(before.late).toBeNull();
    const after = task(
      layoutOf("day", {
        groups: single([
          item({ id: "a", startDay: "2026-12-20", dueDay: "2026-12-28" }),
        ]),
        links: [],
      }),
      "a",
    );
    expect(after.bar).toBeNull();
    expect(after.label).toMatchObject({ text: "Bắt đầu 20/12 ▸", right: 6 });
  });

  it("does not call a done item overdue", () => {
    const row = task(
      layoutOf("day", {
        groups: single([
          item({
            id: "a",
            tone: "done",
            startDay: "2026-10-06",
            dueDay: "2026-10-08",
            percent: 100,
          }),
        ]),
        links: [],
      }),
      "a",
    );
    expect(row.late).toBeNull();
    expect(row.dateState).toBe("");
  });

  it("is the same for the same input", () => {
    expect(layoutOf("day")).toEqual(layoutOf("day"));
  });

  it("speaks English", () => {
    const result = layoutOf("day", { copy: en });
    expect(task(result, "erp-142").late?.text).toBe("Overdue 2 days");
    expect(task(result, "erp-142").aria).toContain("2 days overdue");
    expect(task(result, "erp-129").label?.text).toBe("◂ Done 30/09");
  });
});

describe("layoutTimeline (tuan and thang)", () => {
  it("uses 19 px a day for tuan", () => {
    const layout = layoutOf("week");
    expect(task(layout, "erp-146").bar).toMatchObject({ x: 286, width: 74 });
    expect(task(layout, "erp-142").bar).toMatchObject({ x: 58, width: 188 });
  });

  it("uses 6 px a day and compact bars for thang", () => {
    const layout = layoutOf("month");
    expect(task(layout, "erp-142").bar).toMatchObject({ x: 181, width: 58 });
    // Too narrow for the title: the key, in mono, when it fits.
    const narrow = task(layout, "erp-146").bar;
    expect(narrow?.label).toBe("");
    expect(task(layout, "erp-142").bar?.label).toBe("ERP-142");
    expect(task(layout, "erp-142").bar?.mono).toBe(true);
    expect(task(layout, "erp-151").bar?.label).toBe("");
    // 30/09 is inside the September-to-December window: a dot, not a note.
    expect(task(layout, "erp-129").fin).toMatchObject({ x: 172 });
    expect(task(layout, "erp-129").label?.text).toBe("Hoàn thành 30/09");
  });
});

describe("layoutTimeline (the drag preview)", () => {
  const layout = layoutOf("day", { drag: TIMELINE_DRAG_PREVIEW });

  it("shows the dragged due day in the list, its handle, ghost and tip", () => {
    const row = task(layout, "erp-147");
    expect(row.dates).toBe("23/10");
    expect(row.dateState).toBe("moved");
    expect(row.handle).toEqual({ x: 490 });
    expect(row.ghost).toEqual({ x: 441, width: 52 });
    expect(row.tip).toMatchObject({
      x: 382,
      text: "Hạn: 21/10 → 23/10",
      note: "Đào tạo phòng Kế toán tự lùi 2 ngày, sang 24/10",
    });
    // The bar itself stays where it was until the drop.
    expect(row.bar?.width).toBe(154);
  });

  it("pushes the item that waits for it back by the same days", () => {
    const dependent = task(layout, "phase-training");
    expect(dependent.dates).toBe("25/10");
    expect(dependent.dateState).toBe("moved");
    expect(dependent.ghost).toEqual({ x: 494, width: 52 });
    // The next one starts after the new end already: it stays.
    expect(task(layout, "phase-parallel").dateState).toBe("");
  });

  it("moves a whole span when moving", () => {
    const moved = layoutOf("day", {
      drag: { id: "erp-146", mode: "move", days: 1 },
      links: [],
    });
    const row = task(moved, "erp-146");
    expect(row.dates).toBe("17/10");
    expect(row.ghost).toMatchObject({ width: 104 });
    expect(row.tip?.text).toBe("13–16/10 → 14–17/10");
  });

  it("cannot pull a due day before the start", () => {
    const shrunk = layoutOf("day", {
      drag: { id: "erp-146", mode: "resize", days: -10 },
      links: [],
    });
    expect(task(shrunk, "erp-146").dates).toBe("13/10");
  });
});

describe("layoutTimeline (the critical path)", () => {
  it("rings critical items and fades the rest", () => {
    const layout = layoutOf("day", { criticalPath: true });
    expect(task(layout, "erp-142").bar).toMatchObject({
      critical: true,
      fade: false,
    });
    expect(task(layout, "erp-146").bar).toMatchObject({
      critical: false,
      fade: true,
    });
    // The link between two critical items is critical, the others are not.
    expect(layout.segments.filter((segment) => segment.critical).length).toBe(
      layoutOf("day", { criticalPath: true }).segments.filter(
        (segment) => segment.critical,
      ).length,
    );
    expect(layout.segments.some((segment) => segment.critical)).toBe(true);
    expect(layout.segments.some((segment) => !segment.critical)).toBe(true);
  });

  it("changes nothing when the path is off", () => {
    const layout = layoutOf("day");
    expect(task(layout, "erp-146").bar?.fade).toBe(false);
    expect(layout.segments.some((segment) => segment.critical)).toBe(false);
  });
});

describe("itemSpan", () => {
  it("reads the first and last day", () => {
    const span = itemSpan(item({ id: "a" }));
    expect(fmtDay(span.start ?? 0)).toBe("13/10");
    expect(fmtDay(span.end)).toBe("15/10");
  });

  it("has no start for a done item with only a due day", () => {
    expect(
      itemSpan(item({ id: "a", tone: "done", startDay: null })).start,
    ).toBeNull();
  });

  it("puts the earlier day first", () => {
    const span = itemSpan(
      item({ id: "a", startDay: "2026-10-20", dueDay: "2026-10-15" }),
    );
    expect(fmtDay(span.start ?? 0)).toBe("15/10");
    expect(fmtDay(span.end)).toBe("20/10");
  });
});
