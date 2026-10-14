import { expect, test, type Locator, type Page } from "@playwright/test";

/**
 * Board Timeline geometry against the October 2026 mockup. Every number comes
 * from `docs/design/mockups/2026-10/src/Final-Timeline.dc.html` rendered at
 * 1440x900 with `nav=an` and measured with the box dump (position, size,
 * padding, font, radius, colour) for the scales ngay, tuan and thang, the
 * states binh-thuong, keo-gian and duong-gang, and mode sang and dem. Boxes
 * are read relative to the top-left of the bar (the row under the tab
 * strip), so the checks hold whatever sits left of or above the board: the
 * system rail, the sidebar, the title row and the tab strip, which is 48 px
 * in every view (the mockup's 46 px leaves the bar 2 px higher; the frame
 * and legend heights are corrected by that). Tolerance is 1 px for boxes and
 * exact for font sizes, weights, radii and colours.
 *
 * Data differs between the mock world and the mockup world: the mockup draws
 * three phases, a milestone and seven dependency lines the board read has no
 * kind or contract for, and lists its rows in its own order (the mock board
 * keeps each group's board order, with the done items last in a group where
 * the mockup puts them first). The mock board has the same four groups and
 * the start days of the mockup's items. Only boxes
 * that do not depend on those differences are compared (the bar, the frame,
 * the headers of the three scales, the bands, the today line, the first row,
 * the ERP-142, ERP-146 and ERP-129 rows and the legend). The phase,
 * milestone, arrows, drag preview and critical path are measured on the
 * Storybook stories instead (every box of the three scales, the three
 * states, both themes).
 *
 * The page clock is fixed to the fixtures' 12/10/2026 10:40 and the zone to
 * Asia/Ho_Chi_Minh, the zone of the mockup's "giờ Hà Nội".
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const NOW = new Date("2026-10-12T10:40:00+07:00");

interface Rect {
  readonly x: number;
  readonly y: number;
  readonly width: number;
  readonly height: number;
}

/** Final-Timeline binh-thuong scale=ngay nav=an. Origin: the bar (0,107.4). */
const MOCK = {
  bar: {
    today: { x: 24, y: 10, width: 81.8, height: 32 },
    previous: { x: 113.8, y: 10, width: 32, height: 32 },
    next: { x: 153.8, y: 10, width: 32, height: 32 },
    scale: { x: 193.8, y: 10, width: 175.8, height: 32 },
    groupBy: { x: 377.6, y: 10, width: 163.4, height: 32 },
    barSpan: { x: 549, y: 10, width: 199.9, height: 32 },
    links: { x: 756.9, y: 11, width: 84.4, height: 30 },
    critical: { x: 849.3, y: 11, width: 97.1, height: 30 },
    undated: { x: 1039.3, y: 16.9, width: 120.7, height: 18.1 },
    primary: { x: 24, y: 50, width: 150.2, height: 32 },
  },
  frame: {
    wrap: { x: 24, y: 92, width: 1136 },
    /** Height 663.2 in the mockup; the 48 px tab strip leaves 661.2. */
    wrapHeight: 663.2,
    left: { x: 25, y: 93, width: 381 },
    header: { x: 25, y: 93, width: 380, height: 56 },
    key: { x: 37, y: 122.6, width: 62, height: 17.4 },
    title: { x: 105, y: 122.6, width: 258.3, height: 17.4 },
    due: { x: 369.3, y: 122.6, width: 23.7, height: 17.4 },
    right: { x: 406, y: 93, width: 754 },
    rh: { x: 406, y: 93, width: 754, height: 57 },
    month: { x: 406, y: 93, width: 702, height: 24 },
    monthNext: { x: 1108, y: 93, width: 52, height: 24 },
    dayFirst: { x: 406, y: 117, width: 26, height: 32 },
    dayLast: { x: 1134, y: 117, width: 26, height: 32 },
    todayDay: { x: 591, y: 120.1, width: 20, height: 13.8 },
    bandXs: [536, 562, 718, 744, 900, 926, 1082, 1108],
    bandTop: 150,
    todayLine: { x: 600, y: 150, width: 2 },
    group: { x: 25, y: 149, width: 380, height: 32 },
    groupCount: { x: 102.1, y: 155.1, height: 18.8 },
    /** Cells of the first item row (ERP-151 in the mock world). */
    cellKey: { x: 37, width: 62, height: 18.8 },
    cellTitle: { x: 105, width: 202, height: 18.8 },
    cellAvatar: { x: 313, width: 24, height: 22 },
    cellDue: { x: 343, width: 50, height: 17.4 },
    row: { x: 25, width: 380, height: 36 },
  },
  /** x and width of bars, labels and the hatch; y is row top + 8 (9 for text). */
  items: {
    erp142: { x: 407, width: 154, height: 20, progress: 92.4 },
    erp142Late: { x: 561, width: 52, height: 20 },
    erp142LateLabel: { x: 628, width: 93.5 },
    erp146: { x: 615, width: 102, height: 20, progress: 33.7 },
    /** 6 px after the bar; the mockup moves it right of an arrow line. */
    erp146Label: { x: 724, width: 280 },
    erp129Label: { x: 420, width: 118.1, height: 17.4 },
  },
  legend: {
    height: 37.4,
    swatch: { width: 18, height: 10 },
    swatchXs: [24, 110.4, 204.5, 303.5, 408.9],
    texts: [
      { x: 24, width: 72.4 },
      { x: 110.4, width: 80.1 },
      { x: 204.5, width: 84.9 },
      { x: 303.5, width: 91.5 },
      { x: 408.9, width: 73 },
    ],
  },
  /** Final-Timeline scale=tuan. */
  week: {
    months: [
      { x: 406, width: 57, label: "Th 9" },
      { x: 463, width: 589, label: "Tháng 10, 2026" },
      { x: 1052, width: 95, label: "Tháng 11" },
    ],
    weeks: [
      { x: 406, width: 133, label: "Tuần 40 · 28/09–04/10", weight: "500" },
      { x: 539, width: 133, label: "Tuần 41 · 05–11/10", weight: "500" },
      { x: 672, width: 133, label: "Tuần 42 · 12–18/10", weight: "600" },
      { x: 805, width: 133, label: "Tuần 43 · 19–25/10", weight: "500" },
      { x: 938, width: 133, label: "Tuần 44 · 26/10–01/11", weight: "500" },
      { x: 1071, width: 76, label: "Tuần 45", weight: "500" },
    ],
    weeksTop: 117,
    weekHeight: 32,
    bandXs: [501, 520, 634, 653, 767, 786, 900, 919, 1033, 1052],
    bandWidth: 19,
    nowTick: { x: 681, y: 141, width: 2, height: 8 },
    todayLine: { x: 681, y: 150, width: 2 },
    erp146: { x: 692, width: 74, height: 20 },
    erp142: { x: 464, width: 188, height: 20 },
  },
  /** Final-Timeline scale=thang. */
  month: {
    months: [
      { x: 406, width: 180, label: "Tháng 9, 2026" },
      { x: 586, width: 186, label: "Tháng 10" },
      { x: 772, width: 180, label: "Tháng 11" },
      { x: 952, width: 186, label: "Tháng 12" },
    ],
    tickXs: [
      442, 484, 526, 568, 610, 652, 694, 736, 778, 820, 862, 904, 946, 988,
      1030, 1072, 1114,
    ],
    tickDays: [
      "7",
      "14",
      "21",
      "28",
      "5",
      "12",
      "19",
      "26",
      "2",
      "9",
      "16",
      "23",
      "30",
      "7",
      "14",
      "21",
      "28",
    ],
    bandXs: [586, 772, 952],
    nowTick: { x: 655, y: 141, width: 2, height: 8 },
    todayLine: { x: 655, y: 150, width: 2 },
    barTop: 10,
    barHeight: 16,
    erp142: { x: 587, width: 58, height: 16 },
  },
} as const;

const TYPE = {
  control: { size: "13px" },
  toggle: { size: "12.5px", on: "600", off: "500" },
  head: { size: "12px", weight: "600" },
  group: { size: "13px", weight: "600" },
  count: { size: "13px", weight: "500" },
  row: { size: "13px" },
  rowKey: { size: "11.5px" },
  rowTitle: { weight: "500" },
  rowDue: { size: "12px" },
  month: { size: "12px", weight: "600" },
  day: { size: "10.5px", number: "12px", numberWeight: "600" },
  bar: { size: "12px", weight: "600" },
  barMonth: { size: "10.5px" },
  label: { size: "12px", weight: "500" },
  late: { size: "12px", weight: "700" },
  legend: { size: "12px" },
} as const;

const COLOUR = {
  accent: "rgb(22, 102, 204)",
  accentInk: "rgb(255, 255, 255)",
  accentSoft: "rgb(231, 240, 252)",
  accentText: "rgb(15, 86, 174)",
  panel: "rgb(255, 255, 255)",
  band: "rgb(243, 245, 248)",
  weekend: "rgb(244, 245, 248)",
  line: "rgb(230, 233, 238)",
  lineStrong: "rgb(210, 215, 222)",
  ink: "rgb(22, 26, 33)",
  meta: "rgb(95, 104, 117)",
  muted: "rgb(79, 88, 102)",
  danger: "rgb(196, 35, 64)",
  barTodo: "rgb(236, 238, 242)",
  barTodoBorder: "rgb(201, 206, 214)",
  barDoing: "rgb(214, 229, 249)",
  barReview: "rgb(247, 230, 186)",
  barDone: "rgb(211, 235, 220)",
  progressDoing: "rgb(30, 123, 224)",
  progressReview: "rgb(217, 154, 0)",
  progressDone: "rgb(46, 139, 87)",
  link: "rgb(138, 147, 160)",
} as const;

async function openTimeline(page: Page, query = "") {
  await page.clock.setFixedTime(NOW);
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "vi");
    } catch {
      // Private mode: the default locale applies.
    }
  });
  await page.goto(`${BOARD}?view=timeline${query}`);
  await expect(page.getByRole("grid")).toBeVisible();
  await expect(page.locator('[data-tl="gb"]').first()).toBeVisible();
}

async function box(locator: Locator): Promise<Rect> {
  const found = await locator.boundingBox();
  if (!found) throw new Error("element has no box");
  return found;
}

/** The bar's top-left, the origin of every box below. */
async function origin(page: Page): Promise<Rect> {
  return box(page.locator('[data-tl="bar"]'));
}

/** A locator's box relative to the top-left of the bar. */
async function rel(page: Page, locator: Locator): Promise<Rect> {
  const [o, found] = await Promise.all([origin(page), box(locator)]);
  return {
    x: found.x - o.x,
    y: found.y - o.y,
    width: found.width,
    height: found.height,
  };
}

async function style(locator: Locator, property: string): Promise<string> {
  return locator.evaluate(
    (element, name) => getComputedStyle(element).getPropertyValue(name).trim(),
    property,
  );
}

/** A colour property as `rgb(r, g, b)`, whatever colour space the page uses. */
async function colour(locator: Locator, property: string): Promise<string> {
  return locator.evaluate((element, name) => {
    const canvas = document.createElement("canvas");
    canvas.width = canvas.height = 1;
    const context = canvas.getContext("2d", { willReadFrequently: true })!;
    context.fillStyle = "#000";
    context.fillStyle = getComputedStyle(element).getPropertyValue(name);
    context.fillRect(0, 0, 1, 1);
    const [r, g, b] = context.getImageData(0, 0, 1, 1).data;
    return `rgb(${r}, ${g}, ${b})`;
  }, property);
}

/** A full circle or pill, however the radius is written (50%, 999px, 3.4e+07px). */
function pillRadius(radius: string): boolean {
  return radius.endsWith("%")
    ? parseFloat(radius) >= 50
    : parseFloat(radius) >= 100;
}

function expectRect(actual: Rect, expected: Partial<Rect>, label: string) {
  for (const key of ["x", "y", "width", "height"] as const) {
    const want = expected[key];
    if (want === undefined) continue;
    expect(
      Math.abs(actual[key] - want),
      `${label}.${key}: ${actual[key].toFixed(1)} vs ${want}`,
    ).toBeLessThanOrEqual(1);
  }
}

const bar = (page: Page) => ({
  today: page.getByRole("button", { name: "Cuộn tới hôm nay" }),
  previous: page.getByRole("button", { name: "Lùi", exact: true }),
  next: page.getByRole("button", { name: "Tiến", exact: true }),
  scale: page.getByRole("radiogroup", { name: "Thang thời gian" }),
  groupBy: page.getByRole("button", { name: /^Nhóm theo/ }),
  barSpan: page.getByRole("button", { name: /^Thanh dài từ/ }),
  links: page.getByRole("button", { name: /^Phụ thuộc/ }),
  critical: page.getByRole("button", { name: /^Đường găng/ }),
  undated: page.getByRole("link", { name: "1 việc chưa có ngày" }),
  primary: page.getByRole("button", { name: /^Thêm công việc/ }),
});

const listRow = (page: Page, key: string) =>
  page.locator('[data-tl="lrow"]').filter({ hasText: key }).first();

async function trackOf(page: Page, key: string): Promise<Locator> {
  const id = await listRow(page, key).getAttribute("data-row-id");
  return page.locator(`[data-tl="trow"][data-row-id="${id}"]`);
}

test.describe("Board Timeline geometry at 1440 px (owner)", () => {
  test.skip(
    persona !== "owner" || scenario !== "default" || faults !== "",
    "Owner checks need the default owner world without faults",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  test("the bar: today, back, forward, scale, pills, toggles, undated, add", async ({
    page,
  }) => {
    await openTimeline(page);
    const b = bar(page);
    expectRect(await rel(page, b.today), MOCK.bar.today, "today");
    expectRect(await rel(page, b.previous), MOCK.bar.previous, "previous");
    expectRect(await rel(page, b.next), MOCK.bar.next, "next");
    expectRect(await rel(page, b.scale), MOCK.bar.scale, "scale");
    expectRect(await rel(page, b.groupBy), MOCK.bar.groupBy, "group by");
    expectRect(await rel(page, b.barSpan), MOCK.bar.barSpan, "bar span");
    expectRect(await rel(page, b.links), MOCK.bar.links, "dependencies");
    expectRect(await rel(page, b.critical), MOCK.bar.critical, "critical");
    expectRect(await rel(page, b.undated), MOCK.bar.undated, "undated link");
    // The bar wraps: "Thêm công việc" goes to a second line.
    expectRect(await rel(page, b.primary), MOCK.bar.primary, "primary");

    expect(await style(b.today, "font-size")).toBe(TYPE.control.size);
    expect(await style(b.today, "font-weight")).toBe("500");
    expect(await style(b.today, "border-radius")).toBe("7px");
    expect(await colour(b.today, "border-top-color")).toBe(COLOUR.lineStrong);
    expect(await style(b.primary, "font-weight")).toBe("600");
    expect(await colour(b.primary, "background-color")).toBe(COLOUR.accent);
    expect(await colour(b.primary, "color")).toBe(COLOUR.accentInk);

    const radios = b.scale.getByRole("radio");
    await expect(radios).toHaveCount(3);
    expect(await style(radios.nth(0), "font-weight")).toBe("600");
    expect(await style(radios.nth(1), "font-weight")).toBe("500");
    expect((await rel(page, radios.nth(0))).height).toBeCloseTo(28, 0);
    expect(await style(b.scale, "border-radius")).toBe("8px");

    expect(await style(b.links, "font-size")).toBe(TYPE.toggle.size);
    expect(await style(b.links, "font-weight")).toBe(TYPE.toggle.on);
    expect(await colour(b.links, "background-color")).toBe(COLOUR.accentSoft);
    expect(await colour(b.links, "color")).toBe(COLOUR.accentText);
    expect(await style(b.critical, "font-weight")).toBe(TYPE.toggle.off);
    expect(await colour(b.critical, "background-color")).toBe(COLOUR.panel);
    expect(await colour(b.critical, "border-top-color")).toBe(
      COLOUR.lineStrong,
    );
    expect(await style(b.undated, "font-size")).toBe("12.5px");
    expect(await style(b.undated, "font-weight")).toBe("600");
    expect(await colour(b.undated, "color")).toBe(COLOUR.accentText);
  });

  test("the frame, the list header and the group row", async ({ page }) => {
    await openTimeline(page);
    const f = MOCK.frame;
    const wrap = page.locator('[data-tl="wrap"]');
    const wrapBox = await rel(page, wrap);
    expectRect(wrapBox, f.wrap, "frame");
    // The 48 px tab strip leaves the frame 2 px lower than the mockup's.
    expect(Math.abs(wrapBox.height - (f.wrapHeight - 2))).toBeLessThanOrEqual(
      1,
    );
    expect(await style(wrap, "border-radius")).toBe("12px");
    expect(await colour(wrap, "border-top-color")).toBe(COLOUR.line);
    expect(await colour(wrap, "background-color")).toBe(COLOUR.panel);

    const left = page.locator('[data-tl="left"]');
    expectRect(await rel(page, left), f.left, "list");
    expect(await colour(left, "border-right-color")).toBe(COLOUR.lineStrong);
    const header = page.locator('[data-tl="lh"]');
    expectRect(await rel(page, header), f.header, "list header");
    expect(await colour(header, "background-color")).toBe(COLOUR.band);
    expect(await colour(header, "border-bottom-color")).toBe(COLOUR.lineStrong);
    const heads = header.locator("span");
    expectRect(await rel(page, heads.nth(0)), f.key, "Mã");
    expectRect(await rel(page, heads.nth(1)), f.title, "Công việc");
    expectRect(await rel(page, heads.nth(2)), f.due, "Hạn");
    expect(await style(heads.nth(0), "font-size")).toBe(TYPE.head.size);
    expect(await style(heads.nth(0), "font-weight")).toBe(TYPE.head.weight);
    expect(await colour(heads.nth(0), "color")).toBe(COLOUR.meta);

    const group = page.locator('[data-tl="lgrp"]').first();
    expectRect(await rel(page, group), f.group, "group row");
    expect(await colour(group, "background-color")).toBe(COLOUR.band);
    expect(await style(group, "font-size")).toBe(TYPE.group.size);
    expect(await style(group, "font-weight")).toBe(TYPE.group.weight);
    expect(await style(group, "column-gap")).toBe("8px");
    const count = group.locator("span");
    // The title differs from the mockup's "Chuẩn bị": only the row matches.
    expectRect(
      await rel(page, count),
      { y: f.groupCount.y, height: f.groupCount.height },
      "group count",
    );
    expect(await style(count, "font-weight")).toBe(TYPE.count.weight);
    expect(await colour(count, "color")).toBe(COLOUR.meta);
  });

  test("the first item row: key, title, assignee and due day", async ({
    page,
  }) => {
    await openTimeline(page);
    const f = MOCK.frame;
    const row = page.locator('[data-tl="lrow"]').first();
    const rowBox = await rel(page, row);
    expectRect(rowBox, f.row, "row");
    expect(await style(row, "font-size")).toBe(TYPE.row.size);
    expect(await style(row, "column-gap")).toBe("6px");
    expect(await style(row, "padding-left")).toBe("12px");
    expect(await colour(row, "border-bottom-color")).toBe(COLOUR.line);
    const cells = row.locator(":scope > span");
    expect(await cells.count()).toBe(4);
    const at = async (index: number) => {
      const found = await rel(page, cells.nth(index));
      return { ...found, y: found.y - rowBox.y };
    };
    expectRect(await at(0), { x: f.cellKey.x, width: f.cellKey.width }, "key");
    expectRect(
      await at(1),
      { x: f.cellTitle.x, width: f.cellTitle.width },
      "title",
    );
    expectRect(
      await at(2),
      { x: f.cellAvatar.x, width: f.cellAvatar.width },
      "assignee",
    );
    expectRect(await at(3), { x: f.cellDue.x, width: f.cellDue.width }, "due");
    const key = cells.nth(0).locator("a");
    expect(await style(key, "font-size")).toBe(TYPE.rowKey.size);
    expect(await style(key, "font-family")).toMatch(/IBM Plex Mono/);
    expect(await colour(key, "color")).toBe(COLOUR.meta);
    const title = cells.nth(1).locator("a");
    expect(await style(cells.nth(1), "font-weight")).toBe(TYPE.rowTitle.weight);
    expect(await style(cells.nth(1), "text-overflow")).toBe("ellipsis");
    expect(await colour(title, "color")).toBe(COLOUR.ink);
    const due = cells.nth(3);
    expect(await style(due, "font-size")).toBe(TYPE.rowDue.size);
    expect(await style(due, "text-align")).toBe("right");
    expect(await colour(due, "color")).toBe(COLOUR.meta);
    const avatar = cells.nth(2).locator("span");
    const mark = await box(avatar);
    expect(mark.width).toBeCloseTo(22, 0);
    expect(mark.height).toBeCloseTo(22, 0);
  });

  test("ngay: the time header, the weekend bands and the today line", async ({
    page,
  }) => {
    await openTimeline(page);
    const f = MOCK.frame;
    expectRect(
      await rel(page, page.locator('[data-tl="right"]')),
      f.right,
      "chart",
    );
    const rh = page.locator('[data-tl="rh"]');
    expectRect(await rel(page, rh), f.rh, "time header");
    expect(await colour(rh, "background-color")).toBe(COLOUR.band);
    expect(await colour(rh, "border-bottom-color")).toBe(COLOUR.lineStrong);
    const months = page.locator('[data-tl="month"]');
    await expect(months).toHaveCount(2);
    expectRect(await rel(page, months.nth(0)), f.month, "month");
    expectRect(await rel(page, months.nth(1)), f.monthNext, "next month");
    await expect(months.nth(0)).toHaveText("Tháng 10, 2026");
    await expect(months.nth(1)).toHaveText("Th 11");
    expect(await style(months.nth(0), "font-size")).toBe(TYPE.month.size);
    expect(await style(months.nth(0), "font-weight")).toBe(TYPE.month.weight);
    expect(await style(months.nth(0), "padding-left")).toBe("8px");
    expect(await colour(months.nth(0), "color")).toBe(COLOUR.muted);
    expect(await colour(months.nth(1), "border-left-color")).toBe(COLOUR.line);

    const days = page.locator('[data-tl="day"]');
    await expect(days).toHaveCount(29);
    expectRect(await rel(page, days.first()), f.dayFirst, "first day");
    expectRect(await rel(page, days.last()), f.dayLast, "last day");
    expect(await style(days.first(), "font-size")).toBe(TYPE.day.size);
    const today = page.locator('[data-tl="dayb"]').nth(7);
    await expect(today).toHaveText("12");
    expectRect(await rel(page, today), f.todayDay, "today number");
    expect(await style(today, "font-size")).toBe(TYPE.day.number);
    expect(await style(today, "font-weight")).toBe(TYPE.day.numberWeight);
    expect(await colour(today, "background-color")).toBe(COLOUR.accent);
    expect(await colour(today, "color")).toBe(COLOUR.accentInk);
    expect(pillRadius(await style(today, "border-radius"))).toBe(true);

    const bands = page.locator('[data-tl="band"]');
    await expect(bands).toHaveCount(f.bandXs.length);
    for (let index = 0; index < f.bandXs.length; index += 1) {
      const found = await rel(page, bands.nth(index));
      expectRect(
        found,
        { x: f.bandXs[index], y: f.bandTop, width: 26 },
        `band ${index}`,
      );
    }
    expect(await colour(bands.first(), "background-color")).toBe(
      COLOUR.weekend,
    );
    const line = page.locator('[data-tl="todayline"]');
    expectRect(await rel(page, line), f.todayLine, "today line");
    expect(await colour(line, "background-color")).toBe(COLOUR.accent);
  });

  test("ngay: bars, progress, the overdue hatch and the labels", async ({
    page,
  }) => {
    await openTimeline(page);
    const it = MOCK.items;

    // ERP-142: starts before the window, overdue by 2 days.
    const track142 = await trackOf(page, "ERP-142");
    const trackBox = await rel(page, track142);
    const bar142 = track142.locator('[data-tl="gb"]');
    const found142 = await rel(page, bar142);
    expectRect(
      { ...found142, y: found142.y - trackBox.y },
      {
        x: it.erp142.x,
        y: 8,
        width: it.erp142.width,
        height: it.erp142.height,
      },
      "ERP-142 bar",
    );
    expect(await style(bar142, "font-size")).toBe(TYPE.bar.size);
    expect(await style(bar142, "font-weight")).toBe(TYPE.bar.weight);
    expect(await style(bar142, "border-radius")).toBe("0px 6px 6px 0px");
    expect(await style(bar142, "padding-left")).toBe("8px");
    expect(await colour(bar142, "background-color")).toBe(COLOUR.barDoing);
    const progress = bar142.locator('[data-tl="pg"]');
    const strip = await box(progress);
    expect(strip.height).toBeCloseTo(3, 0);
    expect(Math.abs(strip.width - it.erp142.progress)).toBeLessThanOrEqual(1);
    expect(await colour(progress, "background-color")).toBe(
      COLOUR.progressDoing,
    );
    const late = track142.locator('[data-tl="late"]');
    const lateBox = await rel(page, late);
    expectRect(
      { ...lateBox, y: lateBox.y - trackBox.y },
      { x: it.erp142Late.x, y: 8, width: it.erp142Late.width, height: 20 },
      "overdue hatch",
    );
    expect(await style(late, "border-top-style")).toBe("dashed");
    expect(await colour(late, "border-top-color")).toBe(COLOUR.danger);
    expect(await style(late, "border-radius")).toBe("0px 6px 6px 0px");
    const lateLabel = track142.locator('[data-tl="latel"]');
    await expect(lateLabel).toHaveText("Quá hạn 2 ngày");
    expectRect(
      await rel(page, lateLabel),
      { x: it.erp142LateLabel.x, width: it.erp142LateLabel.width },
      "overdue label",
    );
    expect(await style(lateLabel, "font-weight")).toBe(TYPE.late.weight);
    expect(await colour(lateLabel, "color")).toBe(COLOUR.danger);
    const due142 = listRow(page, "ERP-142").locator("span").last();
    await expect(due142).toHaveText("10/10");
    expect(await colour(due142, "color")).toBe(COLOUR.danger);
    expect(await style(due142, "font-weight")).toBe("600");

    // ERP-146: 13/10 to 16/10, a short bar with its title beside it.
    const track146 = await trackOf(page, "ERP-146");
    const track146Box = await rel(page, track146);
    const bar146 = track146.locator('[data-tl="gb"]');
    const found146 = await rel(page, bar146);
    expectRect(
      { ...found146, y: found146.y - track146Box.y },
      { x: it.erp146.x, y: 8, width: it.erp146.width, height: 20 },
      "ERP-146 bar",
    );
    expect(await style(bar146, "border-radius")).toBe("6px");
    expect(await style(bar146.locator('[data-tl="gt"]'), "overflow")).toBe(
      "hidden",
    );
    expect(
      Math.abs(
        (await box(bar146.locator('[data-tl="pg"]'))).width -
          it.erp146.progress,
      ),
    ).toBeLessThanOrEqual(1);
    const label146 = track146.locator('[data-tl="lab"]');
    const labelBox = await rel(page, label146);
    expectRect(
      { ...labelBox, y: labelBox.y - track146Box.y },
      { x: it.erp146Label.x, y: 9 },
      "ERP-146 label",
    );
    expect(labelBox.width).toBeLessThanOrEqual(it.erp146Label.width + 1);
    expect(await style(label146, "font-size")).toBe(TYPE.label.size);
    expect(await style(label146, "font-weight")).toBe(TYPE.label.weight);
    expect(await colour(label146, "color")).toBe(COLOUR.muted);
    expect(
      await colour(track146.locator('[data-tl="pg"]'), "background-color"),
    ).toBe(COLOUR.progressDoing);

    // ERP-129: done before the window, a note at the left edge.
    const track129 = await trackOf(page, "ERP-129");
    const track129Box = await rel(page, track129);
    const note = track129.locator('[data-tl="lab"]');
    await expect(note).toHaveText("◂ Hoàn thành 30/09");
    const noteBox = await rel(page, note);
    expectRect(
      { ...noteBox, y: noteBox.y - track129Box.y },
      { x: it.erp129Label.x, y: 9, width: it.erp129Label.width },
      "ERP-129 note",
    );
    await expect(track129.locator('[data-tl="gb"]')).toHaveCount(0);

    // Status fills: todo with a border, review and the done dot.
    const todoBar = (await trackOf(page, "ERP-151")).locator('[data-tl="gb"]');
    expect(await colour(todoBar, "background-color")).toBe(COLOUR.barTodo);
    expect(await colour(todoBar, "border-top-color")).toBe(
      COLOUR.barTodoBorder,
    );
    expect(await style(todoBar, "border-top-width")).toBe("1px");
    const reviewBar = (await trackOf(page, "ERP-138")).locator(
      '[data-tl="gb"]',
    );
    expect(await colour(reviewBar, "background-color")).toBe(COLOUR.barReview);
    expect(
      await colour(reviewBar.locator('[data-tl="pg"]'), "background-color"),
    ).toBe(COLOUR.progressReview);
    const dot = (await trackOf(page, "ERP-136")).locator('[data-tl="fin"]');
    expect(await colour(dot, "background-color")).toBe(COLOUR.progressDone);
    const dotBox = await box(dot);
    expect(dotBox.width).toBeCloseTo(10, 0);
    expect(pillRadius(await style(dot, "border-radius"))).toBe(true);
  });

  test("the legend row", async ({ page }) => {
    await openTimeline(page);
    const l = MOCK.legend;
    const legend = page.locator('[data-tl="legendbar"]');
    const legendBox = await rel(page, legend);
    expect(legendBox.height).toBeCloseTo(l.height, 0);
    expect(legendBox.x).toBeCloseTo(0, 0);
    // The frame ends where the legend row starts.
    const wrapBox = await rel(page, page.locator('[data-tl="wrap"]'));
    expect(
      Math.abs(wrapBox.y + wrapBox.height - legendBox.y),
    ).toBeLessThanOrEqual(1);
    const swatches = page.locator('[data-tl="sw"]');
    await expect(swatches).toHaveCount(5);
    for (let index = 0; index < 5; index += 1) {
      const found = await rel(page, swatches.nth(index));
      expect(found.width).toBeCloseTo(l.swatch.width, 0);
      expect(found.height).toBeCloseTo(l.swatch.height, 0);
      expect(Math.abs(found.x - l.swatchXs[index]!)).toBeLessThanOrEqual(1);
    }
    expect(await colour(swatches.nth(1), "background-color")).toBe(
      COLOUR.barDoing,
    );
    expect(await colour(swatches.nth(2), "background-color")).toBe(
      COLOUR.barReview,
    );
    expect(await colour(swatches.nth(3), "background-color")).toBe(
      COLOUR.barDone,
    );
    const texts = page.locator('[data-tl="legend"] > span');
    await expect(texts).toHaveCount(8);
    for (let index = 0; index < l.texts.length; index += 1) {
      const found = await rel(page, texts.nth(index));
      expectRect(found, l.texts[index]!, `legend ${index}`);
    }
    expect(await style(texts.first(), "font-size")).toBe(TYPE.legend.size);
    expect(await colour(texts.first(), "color")).toBe(COLOUR.meta);
    await expect(texts.last()).toHaveText(
      "Vạch dọc màu lam: hôm nay 12/10, giờ Hà Nội",
    );
  });

  test("tuan: months, weeks with their ticks, bands and the today tick", async ({
    page,
  }) => {
    await openTimeline(page);
    await bar(page).scale.getByRole("radio", { name: "Tuần" }).click();
    const w = MOCK.week;
    const months = page.locator('[data-tl="month"]');
    await expect(months).toHaveCount(w.months.length);
    for (let index = 0; index < w.months.length; index += 1) {
      const found = await rel(page, months.nth(index));
      expect(Math.abs(found.x - w.months[index]!.x)).toBeLessThanOrEqual(1);
      expect(
        Math.abs(found.width - w.months[index]!.width),
      ).toBeLessThanOrEqual(1);
      await expect(months.nth(index)).toHaveText(w.months[index]!.label);
    }
    const weeks = page.locator('[data-tl="wk"]');
    await expect(weeks).toHaveCount(w.weeks.length);
    for (let index = 0; index < w.weeks.length; index += 1) {
      const found = await rel(page, weeks.nth(index));
      expectRect(
        found,
        {
          x: w.weeks[index]!.x,
          y: w.weeksTop,
          width: w.weeks[index]!.width,
          height: w.weekHeight,
        },
        `week ${index}`,
      );
      await expect(weeks.nth(index)).toHaveText(w.weeks[index]!.label);
      expect(await style(weeks.nth(index), "font-weight")).toBe(
        w.weeks[index]!.weight,
      );
      expect(await style(weeks.nth(index), "font-size")).toBe("10.5px");
    }
    expect(await colour(weeks.nth(2), "color")).toBe(COLOUR.accentText);
    expect(await style(weeks.first(), "background-size")).toBe("19px 5px");
    const bands = page.locator('[data-tl="band"]');
    await expect(bands).toHaveCount(w.bandXs.length);
    for (let index = 0; index < w.bandXs.length; index += 1) {
      const found = await rel(page, bands.nth(index));
      expectRect(
        found,
        { x: w.bandXs[index], width: w.bandWidth },
        `week band ${index}`,
      );
    }
    expectRect(
      await rel(page, page.locator('[data-tl="hnow"]')),
      w.nowTick,
      "today tick",
    );
    expectRect(
      await rel(page, page.locator('[data-tl="todayline"]')),
      w.todayLine,
      "today line",
    );
    await expect(page.locator('[data-tl="day"]')).toHaveCount(0);

    const track146 = await trackOf(page, "ERP-146");
    const track146Box = await rel(page, track146);
    const found146 = await rel(page, track146.locator('[data-tl="gb"]'));
    expectRect(
      { ...found146, y: found146.y - track146Box.y },
      { x: w.erp146.x, y: 8, width: w.erp146.width, height: w.erp146.height },
      "ERP-146 bar",
    );
    const track142 = await trackOf(page, "ERP-142");
    const found142 = await rel(page, track142.locator('[data-tl="gb"]'));
    expect(Math.abs(found142.x - w.erp142.x)).toBeLessThanOrEqual(1);
    expect(Math.abs(found142.width - w.erp142.width)).toBeLessThanOrEqual(1);
  });

  test("thang: months, Monday ticks, month rules and compact bars", async ({
    page,
  }) => {
    await openTimeline(page);
    await bar(page).scale.getByRole("radio", { name: "Tháng" }).click();
    const m = MOCK.month;
    const months = page.locator('[data-tl="month"]');
    await expect(months).toHaveCount(m.months.length);
    for (let index = 0; index < m.months.length; index += 1) {
      const found = await rel(page, months.nth(index));
      expect(Math.abs(found.x - m.months[index]!.x)).toBeLessThanOrEqual(1);
      expect(
        Math.abs(found.width - m.months[index]!.width),
      ).toBeLessThanOrEqual(1);
      await expect(months.nth(index)).toHaveText(m.months[index]!.label);
    }
    const ticks = page.locator('[data-tl="mt"]');
    await expect(ticks).toHaveCount(m.tickXs.length);
    for (let index = 0; index < m.tickXs.length; index += 1) {
      const found = await rel(page, ticks.nth(index));
      expect(Math.abs(found.x - m.tickXs[index]!)).toBeLessThanOrEqual(1);
      await expect(ticks.nth(index)).toHaveText(m.tickDays[index]!);
    }
    expect(await style(ticks.first(), "font-size")).toBe("10.5px");
    expect(await colour(ticks.first(), "border-left-color")).toBe(COLOUR.line);
    const bands = page.locator('[data-tl="band"]');
    await expect(bands).toHaveCount(m.bandXs.length);
    for (let index = 0; index < m.bandXs.length; index += 1) {
      const found = await rel(page, bands.nth(index));
      expect(Math.abs(found.x - m.bandXs[index]!)).toBeLessThanOrEqual(1);
      expect(found.width).toBeCloseTo(1, 0);
    }
    expect(await colour(bands.first(), "background-color")).toBe(COLOUR.line);
    expectRect(
      await rel(page, page.locator('[data-tl="hnow"]')),
      m.nowTick,
      "today tick",
    );
    expectRect(
      await rel(page, page.locator('[data-tl="todayline"]')),
      m.todayLine,
      "today line",
    );

    const track142 = await trackOf(page, "ERP-142");
    const trackBox = await rel(page, track142);
    const bar142 = track142.locator('[data-tl="gb"]');
    const found142 = await rel(page, bar142);
    expectRect(
      { ...found142, y: found142.y - trackBox.y },
      {
        x: m.erp142.x,
        y: m.barTop,
        width: m.erp142.width,
        height: m.barHeight,
      },
      "ERP-142 bar",
    );
    expect(await style(bar142, "font-size")).toBe(TYPE.barMonth.size);
    // It starts 01/10, inside the window, so only its corners are rounded 4 px.
    expect(await style(bar142, "border-radius")).toBe("4px");
    expect(await style(bar142, "padding-left")).toBe("3px");
  });

  test("mode dem: the dark panel, band, bars and tones", async ({ page }) => {
    await page.addInitScript(() => {
      try {
        window.localStorage.setItem("notrelix-theme-mode", "dark");
      } catch {
        // Private mode: the light theme applies.
      }
    });
    await openTimeline(page);
    await expect(page.locator("html")).toHaveClass(/dark/);
    // Final-Timeline mode=dem.
    expect(
      await colour(page.locator('[data-tl="wrap"]'), "background-color"),
    ).toBe("rgb(21, 26, 33)");
    expect(
      await colour(page.locator('[data-tl="lh"]'), "background-color"),
    ).toBe("rgb(18, 23, 29)");
    expect(
      await colour(
        page.locator('[data-tl="band"]').first(),
        "background-color",
      ),
    ).toBe("rgb(18, 23, 29)");
    const bar142 = (await trackOf(page, "ERP-142")).locator('[data-tl="gb"]');
    expect(await colour(bar142, "background-color")).toBe("rgb(23, 52, 94)");
    expect(
      await colour(bar142.locator('[data-tl="pg"]'), "background-color"),
    ).toBe("rgb(90, 169, 255)");
    const todoBar = (await trackOf(page, "ERP-151")).locator('[data-tl="gb"]');
    expect(await colour(todoBar, "background-color")).toBe("rgb(37, 45, 56)");
    expect(await colour(todoBar, "border-top-color")).toBe("rgb(58, 68, 82)");
    const reviewBar = (await trackOf(page, "ERP-138")).locator(
      '[data-tl="gb"]',
    );
    expect(await colour(reviewBar, "background-color")).toBe("rgb(58, 48, 24)");
    const dot = (await trackOf(page, "ERP-136")).locator('[data-tl="fin"]');
    expect(await colour(dot, "background-color")).toBe("rgb(86, 201, 138)");
  });
});
