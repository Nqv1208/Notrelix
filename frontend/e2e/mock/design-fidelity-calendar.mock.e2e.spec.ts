import { expect, test, type Locator, type Page } from "@playwright/test";

/**
 * Board Calendar geometry against the October 2026 mockup. Every number comes
 * from `docs/design/mockups/2026-10/src/Final-Calendar.dc.html` rendered at
 * 1440x900 with `nav=an` and measured with the box dump (position, size,
 * padding, gap, font, radius, colour), for the states binh-thuong, keo-tha,
 * tao-nhanh and khong-ket-qua in the ranges thang, tuan and ngay. Boxes are
 * read relative to the main area's top-left, so the checks hold whatever sits
 * left of or above the main area (rail, sidebar, shell header). Tolerance is
 * 1 px for boxes and exact for font sizes, weights, radii and colours.
 *
 * Text and data differ between the mock world and the mockup world: the
 * mockup draws three phases and a milestone the board read has no kind for,
 * and ERP-146 and ERP-131 sit on other days. Only boxes that do not depend on
 * those differences are compared (the bar, the frame, the weekday header, the
 * week rows, the day numbers, the first row of entries that no phase moves,
 * the aside, the drag preview and the quick-create popover). The phase and
 * milestone markers are measured on the Storybook stories instead.
 *
 * The page clock is fixed to the fixtures' 12/10/2026 10:40 and the zone to
 * Asia/Ho_Chi_Minh, the zone of the mockup's "Giờ Hà Nội (GMT+7)".
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

/** Final-Calendar binh-thuong range=thang nav=an. Origin: main area (256,0). */
const MOCK = {
  bar: {
    today: { x: 24, y: 117, width: 82, height: 32 },
    previous: { x: 114, y: 117, width: 32, height: 32 },
    next: { x: 154, y: 117, width: 32, height: 32 },
    heading: { x: 200, y: 121, width: 150, height: 25 },
    segment: { x: 364, y: 117, width: 182, height: 32 },
    month: { x: 366, y: 119, width: 65, height: 28 },
    week: { x: 431, y: 119, width: 55, height: 28 },
    day: { x: 486, y: 119, width: 58, height: 28 },
    basis: { x: 554, y: 117, width: 220, height: 32 },
    filter: { x: 782, y: 117, width: 67, height: 32 },
    zone: { x: 885, y: 125, width: 117, height: 17 },
    primary: { x: 1010, y: 117, width: 150, height: 32 },
  },
  frame: {
    calendar: { x: 24, y: 159, width: 848, height: 721 },
    weekdays: { x: 25, y: 160, width: 846, height: 33 },
    column: { width: 121, height: 32 },
    columnX: [25, 146, 267, 388, 508, 629, 750],
    weekTops: [193, 331, 468, 605, 743],
    weeksHeight: 686,
  },
  number: { first: { x: 33, y: 201, width: 17, height: 18 } },
  today: {
    circle: { x: 33, y: 473, width: 24, height: 24 },
    marker: { x: 63, y: 477, width: 49, height: 17 },
  },
  event: {
    done: { x: 271, y: 226, width: 113, height: 24 },
    overdueStart: { x: 392, y: 226, width: 475, height: 24 },
    overdueCont: { x: 29, y: 364, width: 717, height: 24 },
    todoFirst: { x: 754, y: 501, width: 113, height: 24 },
    todoWeek4: { x: 271, y: 638, width: 113, height: 24 },
    todoWeek4Last: { x: 754, y: 638, width: 113, height: 24 },
    dot: { dx: 7, dy: 9, size: 7 },
    mark: { size: 18 },
  },
  aside: {
    undated: { x: 888, y: 159, width: 272, height: 174 },
    title: { x: 901, y: 172, width: 246, height: 20 },
    hint: { x: 901, y: 200, width: 246, height: 18 },
    item: { x: 901, y: 226, width: 246, height: 94 },
    itemTitle: { x: 936, y: 236, width: 152, height: 55 },
    setDue: { x: 1096, y: 260, width: 41, height: 26 },
    legend: { x: 888, y: 343, width: 272, height: 192 },
    swatch: { x: 901, y: 387, width: 22, height: 12 },
    legendPitch: 24,
  },
  /** Final-Calendar keo-tha: the drag of ERP-146 from 16/10 over 20/10. */
  drag: {
    tip: { x: 154, y: 591, width: 156, height: 27 },
    cell: { x: 146, y: 605, width: 121, height: 136 },
    ghost: { x: 150, y: 638, width: 113, height: 24 },
  },
  /** Final-Calendar tao-nhanh: the popover on 22/10. */
  quick: { x: 392, width: 113, height: 67 },
  /** Final-Calendar khong-ket-qua: the card over the empty grid. */
  noResults: { x: 257, y: 484, width: 382, height: 144 },
  /** Final-Calendar range=tuan. */
  week: {
    frame: { x: 25, y: 193, width: 846, height: 686 },
    today: { x: 33, y: 198, width: 24, height: 24 },
  },
  /** Final-Calendar range=ngay. */
  day: {
    heading: { x: 200, y: 121, width: 177, height: 25 },
    primary: { x: 24, y: 157, width: 150, height: 32 },
    calendar: { x: 24, y: 199, width: 848, height: 681 },
    overdue: { x: 25, y: 200, width: 846, height: 77 },
    overdueItem: { x: 37, y: 234, width: 822, height: 33 },
    slotHeight: 44,
    timeWidth: 64,
    nowOffset: 117,
    nowHeight: 2,
  },
} as const;

const TYPE = {
  control: { size: "13px" },
  heading: { size: "17px", weight: "650" },
  weekday: { size: "12px", weight: "600" },
  number: { size: "12.5px", weight: "700" },
  marker: { size: "11.5px", weight: "500" },
  event: { size: "12px", weight: "500" },
  mark: { size: "7.5px", weight: "700" },
  asideTitle: { size: "13.5px", weight: "650" },
  legend: { size: "12.5px" },
  zone: { size: "12px" },
  tip: { size: "12px", weight: "600" },
} as const;

const COLOUR = {
  weekend: "rgb(244, 245, 248)",
  outside: "rgb(250, 251, 252)",
  today: "rgb(237, 244, 253)",
  doing: "rgb(226, 236, 250)",
  review: "rgb(251, 240, 214)",
  done: "rgb(226, 241, 231)",
  overdue: "rgb(253, 236, 239)",
  overdueText: "rgb(196, 35, 64)",
  doneText: "rgb(95, 104, 117)",
  line: "rgb(230, 233, 238)",
  lineStrong: "rgb(210, 215, 222)",
  accent: "rgb(22, 102, 204)",
  accentText: "rgb(15, 86, 174)",
  tip: "rgb(22, 26, 33)",
  tipText: "rgb(242, 244, 247)",
  selected: "rgb(231, 240, 252)",
} as const;

async function openCalendar(page: Page, query = "") {
  await page.clock.setFixedTime(NOW);
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "vi");
    } catch {
      // Private mode: the default locale applies.
    }
  });
  await page.goto(`${BOARD}?view=calendar${query}`);
  await expect(page.getByRole("grid")).toBeVisible();
  await expect(page.locator("[data-calendar-event]").first()).toBeVisible();
}

async function main(page: Page): Promise<Rect> {
  const box = await page.locator("main").boundingBox();
  if (!box) throw new Error("main area has no box");
  return box;
}

/** A locator's box relative to the main area's top-left. */
async function rel(page: Page, locator: Locator): Promise<Rect> {
  const [origin, box] = await Promise.all([main(page), locator.boundingBox()]);
  if (!box) throw new Error("element has no box");
  return {
    x: box.x - origin.x,
    y: box.y - origin.y,
    width: box.width,
    height: box.height,
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
  today: page.getByRole("button", { name: "Hôm nay", exact: true }),
  previous: page.getByRole("button", { name: /^(Tháng|Tuần|Ngày) trước$/ }),
  next: page.getByRole("button", { name: /^(Tháng|Tuần|Ngày) sau$/ }),
  heading: page.getByRole("heading", { level: 2 }).first(),
  segment: page.getByRole("radiogroup", { name: "Khoảng hiển thị" }),
  basis: page.getByRole("button", { name: /^Xếp theo/ }),
  zone: page.getByText("Giờ Hà Nội (GMT+7)"),
  primary: page.getByRole("button", { name: "Thêm công việc" }),
});

const week = (page: Page, monday: string) =>
  page.locator(`[data-calendar-week="${monday}"]`);

const eventOf = (page: Page, key: string, monday?: string) =>
  (monday ? week(page, monday) : page)
    .locator("[data-calendar-event]")
    .filter({ hasText: key })
    .first();

test.describe("Board Calendar geometry at 1440 px (owner)", () => {
  test.skip(
    persona !== "owner" || scenario !== "default" || faults !== "",
    "Owner checks need the default owner world without faults",
  );
  test.use({
    viewport: { width: 1440, height: 900 },
    timezoneId: "Asia/Ho_Chi_Minh",
  });

  test("bar: every control, size, radius and type", async ({ page }) => {
    await openCalendar(page);
    const b = MOCK.bar;
    const c = bar(page);

    const today = c.today;
    expectRect(await rel(page, today), b.today, "Hôm nay");
    expect(await style(today, "font-size")).toBe(TYPE.control.size);
    expect(await style(today, "font-weight")).toBe("500");
    expect(await style(today, "border-radius")).toBe("7px");
    expect(await style(today, "padding-left")).toBe("12px");
    expect(await colour(today, "border-top-color")).toBe(COLOUR.lineStrong);

    expectRect(await rel(page, c.previous), b.previous, "previous");
    expectRect(await rel(page, c.next), b.next, "next");
    expect(await style(c.previous, "border-radius")).toBe("7px");

    const heading = c.heading;
    expectRect(await rel(page, heading), b.heading, "heading");
    expect(await style(heading, "font-size")).toBe(TYPE.heading.size);
    expect(await style(heading, "font-weight")).toBe(TYPE.heading.weight);
    expect(await style(heading, "margin-left")).toBe("6px");

    const segment = c.segment;
    expectRect(await rel(page, segment), b.segment, "segment");
    expect(await style(segment, "border-radius")).toBe("8px");
    expect(await style(segment, "padding-left")).toBe("2px");
    const month = page.getByRole("radio", { name: "Tháng" });
    expectRect(await rel(page, month), b.month, "Tháng");
    expectRect(
      await rel(page, page.getByRole("radio", { name: "Tuần" })),
      b.week,
      "Tuần",
    );
    expectRect(
      await rel(page, page.getByRole("radio", { name: "Ngày" })),
      b.day,
      "Ngày",
    );
    expect(await style(month, "font-size")).toBe(TYPE.control.size);
    expect(await style(month, "font-weight")).toBe("600");
    expect(await style(month, "border-radius")).toBe("6px");
    expect(
      await style(page.getByRole("radio", { name: "Tuần" }), "font-weight"),
    ).toBe("500");

    const basis = c.basis;
    expectRect(await rel(page, basis), b.basis, "Xếp theo");
    expect(await style(basis, "font-size")).toBe(TYPE.control.size);
    expect(await style(basis, "font-weight")).toBe("400");
    expect(await style(basis.locator("b"), "font-weight")).toBe("600");

    const filter = page.getByRole("button", { name: "Lọc", exact: true });
    expectRect(await rel(page, filter), b.filter, "Lọc");
    expect(await style(filter, "font-weight")).toBe("400");

    const zone = c.zone;
    expectRect(await rel(page, zone), b.zone, "time zone");
    expect(await style(zone, "font-size")).toBe(TYPE.zone.size);

    const primary = c.primary;
    expectRect(await rel(page, primary), b.primary, "Thêm công việc");
    expect(await style(primary, "font-weight")).toBe("600");
    expect(await style(primary, "border-radius")).toBe("7px");
    expect(await colour(primary, "background-color")).toBe(COLOUR.accent);
  });

  test("frame: the grid, weekday header and the five week rows", async ({
    page,
  }) => {
    await openCalendar(page);
    const f = MOCK.frame;
    const grid = page.getByRole("grid");
    expectRect(await rel(page, grid), f.calendar, "calendar");
    expect(await style(grid, "border-radius")).toBe("12px");
    expect(await colour(grid, "border-top-color")).toBe(COLOUR.line);

    const header = page.getByRole("row").first();
    expectRect(await rel(page, header), f.weekdays, "weekday header");
    expect(await colour(header, "background-color")).toBe(COLOUR.weekend);

    const names = [
      "Thứ 2",
      "Thứ 3",
      "Thứ 4",
      "Thứ 5",
      "Thứ 6",
      "Thứ 7",
      "Chủ nhật",
    ];
    for (const [index, name] of names.entries()) {
      const cell = page.getByRole("columnheader", { name, exact: true });
      expectRect(
        await rel(page, cell),
        { x: f.columnX[index], y: 160, ...f.column },
        name,
      );
      expect(await style(cell, "font-size")).toBe(TYPE.weekday.size);
      expect(await style(cell, "font-weight")).toBe(TYPE.weekday.weight);
      expect(await style(cell, "padding-left")).toBe("10px");
    }

    const weeks = page.locator("[data-calendar-week]");
    await expect(weeks).toHaveCount(5);
    for (const [index, top] of f.weekTops.entries()) {
      const box = await rel(page, weeks.nth(index));
      expectRect(box, { x: 25, y: top, width: 846 }, `week ${index + 1}`);
      expect(box.height).toBeGreaterThanOrEqual(136);
      expect(box.height).toBeLessThanOrEqual(138);
    }
    const last = await rel(page, weeks.nth(4));
    expect(
      Math.abs(last.y + last.height - (f.weekTops[0] + f.weeksHeight)),
      "the weeks fill the grid",
    ).toBeLessThanOrEqual(1);
  });

  test("days: numbers, the today circle and the column tints", async ({
    page,
  }) => {
    await openCalendar(page);
    const first = page.locator("[data-day='2026-09-28'] b");
    expectRect(await rel(page, first), MOCK.number.first, "number 28");
    expect(await style(first, "font-size")).toBe(TYPE.number.size);
    expect(await style(first, "font-weight")).toBe(TYPE.number.weight);
    expect(await colour(first, "color")).toBe("rgb(138, 146, 158)");
    const regular = page.locator("[data-day='2026-10-13'] b");
    expect(await colour(regular, "color")).toBe("rgb(79, 88, 102)");

    const circle = page.locator("[data-day='2026-10-12'] b");
    expectRect(await rel(page, circle), MOCK.today.circle, "today circle");
    // Fully round: any radius past half the box.
    expect(parseFloat(await style(circle, "border-radius"))).toBeGreaterThan(
      12,
    );
    expect(await colour(circle, "background-color")).toBe(COLOUR.accent);
    expect(await colour(circle, "color")).toBe("rgb(255, 255, 255)");
    const marker = page.locator("[data-day='2026-10-12'] small");
    expectRect(await rel(page, marker), MOCK.today.marker, "today marker");
    expect(await style(marker, "font-size")).toBe(TYPE.marker.size);
    expect(await style(marker, "font-weight")).toBe(TYPE.marker.weight);
    expect(await colour(marker, "color")).toBe(COLOUR.accentText);

    const tint = (day: string) =>
      colour(
        page.locator(`[data-calendar-day-bg='${day}']`),
        "background-color",
      );
    expect(await tint("2026-09-28")).toBe(COLOUR.outside);
    expect(await tint("2026-10-03")).toBe(COLOUR.weekend);
    // An outside day that is also a Sunday keeps the outside tint.
    expect(await tint("2026-11-01")).toBe(COLOUR.outside);
    expect(await tint("2026-10-12")).toBe(COLOUR.today);
    const plain = await tint("2026-10-13");
    expect([COLOUR.weekend, COLOUR.today, COLOUR.outside]).not.toContain(plain);
    expectRect(
      await rel(page, page.locator("[data-calendar-day-bg='2026-10-12']")),
      { x: 25, y: 468, width: 121, height: 136 },
      "today column",
    );
  });

  test("entries: box, padding, dot, mark and the tone colours", async ({
    page,
  }) => {
    await openCalendar(page);
    const e = MOCK.event;

    const done = eventOf(page, "ERP-129", "2026-09-28");
    expectRect(await rel(page, done), e.done, "done entry");
    expect(await style(done, "font-size")).toBe(TYPE.event.size);
    expect(await style(done, "font-weight")).toBe(TYPE.event.weight);
    expect(await style(done, "border-radius")).toBe("6px");
    expect(await style(done, "padding-left")).toBe("6px");
    expect(await style(done, "column-gap")).toBe("6px");
    expect(await colour(done, "background-color")).toBe(COLOUR.done);
    expect(await colour(done, "color")).toBe(COLOUR.doneText);
    expect(
      await style(done.locator("span").nth(1), "text-decoration-line"),
    ).toContain("line-through");

    const dot = done.locator("span").first();
    const box = await rel(page, done);
    expectRect(
      await rel(page, dot),
      {
        x: box.x + e.dot.dx,
        y: box.y + e.dot.dy,
        width: e.dot.size,
        height: e.dot.size,
      },
      "dot",
    );
    const mark = done.locator("span").last();
    const markBox = await rel(page, mark);
    expect(markBox.width).toBeCloseTo(e.mark.size, 0);
    expect(markBox.height).toBeCloseTo(e.mark.size, 0);
    expect(await style(mark, "font-size")).toBe(TYPE.mark.size);
    expect(await style(mark, "font-weight")).toBe(TYPE.mark.weight);
    // The mark sits at the right edge, 6 px padding and 1 px border away.
    expect(
      Math.abs(box.x + box.width - (markBox.x + markBox.width) - 7),
    ).toBeLessThanOrEqual(1);

    const overdue = eventOf(page, "ERP-142", "2026-09-28");
    expectRect(await rel(page, overdue), e.overdueStart, "overdue start");
    expect(await colour(overdue, "background-color")).toBe(COLOUR.overdue);
    expect(await colour(overdue, "color")).toBe(COLOUR.overdueText);
    expect(await style(overdue, "font-weight")).toBe("600");
    expectRect(
      await rel(page, eventOf(page, "ERP-142", "2026-10-05")),
      e.overdueCont,
      "overdue continued",
    );

    const todo = eventOf(page, "ERP-151", "2026-10-12");
    expectRect(await rel(page, todo), e.todoFirst, "to-do entry");
    expect(await colour(todo, "border-top-color")).toBe(COLOUR.lineStrong);
    expect(await colour(todo, "background-color")).toBe("rgb(255, 255, 255)");
    expectRect(
      await rel(page, eventOf(page, "ERP-147", "2026-10-19")),
      e.todoWeek4,
      "ERP-147",
    );
    expectRect(
      await rel(page, eventOf(page, "ERP-149", "2026-10-19")),
      e.todoWeek4Last,
      "ERP-149",
    );

    expect(
      await colour(eventOf(page, "ERP-144", "2026-10-12"), "background-color"),
    ).toBe(COLOUR.doing);
    expect(
      await colour(eventOf(page, "ERP-138", "2026-10-12"), "background-color"),
    ).toBe(COLOUR.review);
  });

  test("aside: the undated card, its item and the legend", async ({ page }) => {
    await openCalendar(page);
    const a = MOCK.aside;
    const aside = page.getByRole("complementary", {
      name: "Công việc chưa có ngày",
    });
    const box = aside.locator("> div").first();
    expectRect(await rel(page, box), a.undated, "undated card");
    expect(await style(box, "border-radius")).toBe("12px");
    expect(await style(box, "padding-top")).toBe("12px");
    expect(await style(box, "row-gap")).toBe("8px");
    expect(await colour(box, "border-top-color")).toBe(COLOUR.line);

    const title = aside.getByRole("heading", { name: "1 việc chưa có ngày" });
    expectRect(await rel(page, title), a.title, "undated title");
    expect(await style(title, "font-size")).toBe(TYPE.asideTitle.size);
    expect(await style(title, "font-weight")).toBe(TYPE.asideTitle.weight);
    expectRect(
      await rel(page, aside.getByText("Kéo vào lịch hoặc bấm Đặt hạn.")),
      a.hint,
      "hint",
    );

    const item = aside.locator("[data-calendar-undated]");
    expectRect(await rel(page, item), a.item, "undated item");
    expect(await style(item, "border-radius")).toBe("9px");
    expect(await style(item, "padding-left")).toBe("10px");
    expect(await style(item, "column-gap")).toBe("8px");
    expectRect(
      await rel(page, item.locator("b")),
      a.itemTitle,
      "undated item title",
    );
    // The mockup lets the button shrink to 41 px, so its label wraps.
    const setDue = item.getByRole("button", { name: /^Đặt hạn cho/ });
    expectRect(await rel(page, setDue), a.setDue, "Đặt hạn");
    expect(await style(setDue, "font-size")).toBe(TYPE.event.size);
    expect(await style(setDue, "border-radius")).toBe("7px");

    const legend = aside.locator("> div").nth(1);
    expectRect(await rel(page, legend), a.legend, "legend card");
    await expect(legend.locator("> div > span")).toHaveCount(6);
    for (const [index, name] of [
      "Cần làm",
      "Đang làm",
      "Chờ duyệt",
      "Hoàn thành",
      "Quá hạn",
    ].entries()) {
      const row = legend.getByText(name, { exact: true });
      const swatch = row.locator("span").first();
      expectRect(
        await rel(page, swatch),
        {
          x: a.swatch.x,
          y: a.swatch.y + index * a.legendPitch,
          width: a.swatch.width,
          height: a.swatch.height,
        },
        `${name} swatch`,
      );
      expect(await style(swatch, "border-radius")).toBe("4px");
      expect(await style(row, "font-size")).toBe(TYPE.legend.size);
    }
    expect(
      await colour(
        legend.getByText("Đang làm").locator("span").first(),
        "background-color",
      ),
    ).toBe(COLOUR.doing);
  });

  test("keo-tha: the lifted entry, the drop cell, the ghost and the tooltip", async ({
    page,
  }) => {
    await openCalendar(page);
    const chip = eventOf(page, "ERP-146", "2026-10-12");
    const target = page.locator("[data-calendar-day-bg='2026-10-20']");
    const [from, to] = await Promise.all([
      chip.boundingBox(),
      target.boundingBox(),
    ]);
    if (!from || !to) throw new Error("no boxes to drag between");
    await chip.hover();
    await page.mouse.down();
    await page.mouse.move(from.x + 12, from.y + 12);
    await page.mouse.move(to.x + 30, to.y + 60, { steps: 8 });

    const d = MOCK.drag;
    expect(await style(chip, "opacity")).toBe("0.4");
    const cell = page.locator("[data-calendar-day-bg='2026-10-20']");
    expectRect(await rel(page, cell), d.cell, "drop cell");
    expect(await colour(cell, "background-color")).toBe(COLOUR.selected);
    expect(await style(cell, "box-shadow")).toContain("2px");

    const ghost = page.locator("[data-calendar-ghost]");
    expectRect(await rel(page, ghost), d.ghost, "ghost");
    expect(await style(ghost, "border-top-style")).toBe("dashed");
    // 1.5px is laid out as 1px at a device pixel ratio of 1, in the mockup too.
    expect(await style(ghost, "border-top-width")).toBe("1px");
    expect(await colour(ghost, "border-top-color")).toBe(COLOUR.accent);
    expect(await colour(ghost, "color")).toBe(COLOUR.accentText);

    const tip = page.locator("[data-calendar-tip]");
    await expect(tip).toHaveText("Đổi hạn: 16/10 → 20/10");
    expectRect(await rel(page, tip), d.tip, "tooltip");
    expect(await colour(tip, "background-color")).toBe(COLOUR.tip);
    expect(await colour(tip, "color")).toBe(COLOUR.tipText);
    expect(await style(tip, "font-size")).toBe(TYPE.tip.size);
    expect(await style(tip, "font-weight")).toBe(TYPE.tip.weight);
    expect(await style(tip, "border-radius")).toBe("7px");

    await page.mouse.up();
    // The write lands: the entry is on 20/10 and the ghost is gone.
    await expect(eventOf(page, "ERP-146", "2026-10-19")).toBeVisible();
    await expect(ghost).toHaveCount(0);
  });

  test("tao-nhanh: the quick-create popover", async ({ page }) => {
    await openCalendar(page);
    const cell = page.locator("[data-calendar-day-bg='2026-10-22']");
    const box = await cell.boundingBox();
    if (!box) throw new Error("no cell");
    await page.mouse.click(box.x + 60, box.y + 110);

    const q = MOCK.quick;
    const popover = page.locator("[data-calendar-quick-create]");
    expectRect(await rel(page, popover), q, "quick create");
    expect(await style(popover, "border-top-width")).toBe("1px");
    expect(await colour(popover, "border-top-color")).toBe(COLOUR.accent);
    expect(await style(popover, "border-radius")).toBe("7px");
    expect(await style(popover, "padding-left")).toBe("6px");
    expect(await style(popover, "row-gap")).toBe("3px");
    const input = popover.getByRole("textbox");
    await expect(input).toBeFocused();
    expect((await rel(page, input)).height).toBeCloseTo(20, 0);
    expect(await style(input, "font-size")).toBe("12.5px");
    const hint = popover.getByText("Enter để tạo · hạn 22/10");
    expect(await style(hint, "font-size")).toBe("11px");
    expect(await colour(hint, "color")).toBe("rgb(95, 104, 117)");

    await input.fill("Họp chốt phạm vi UAT");
    await input.press("Enter");
    await expect(popover).toHaveCount(0);
    await expect(
      week(page, "2026-10-19")
        .locator("[data-calendar-event]")
        .filter({ hasText: "Họp chốt phạm vi UAT" }),
    ).toBeVisible();
  });

  test("mode dem: the dark panel, tints and tones", async ({ page }) => {
    await page.addInitScript(() => {
      try {
        window.localStorage.setItem("notrelix-theme-mode", "dark");
      } catch {
        // Private mode: the system theme applies.
      }
    });
    await openCalendar(page);
    await expect(page.locator("html")).toHaveClass(/dark/);
    // Final-Calendar mode=dem, binh-thuong, thang.
    const grid = page.getByRole("grid");
    expect(await colour(grid, "background-color")).toBe("rgb(21, 26, 33)");
    expect(await colour(grid, "border-top-color")).toBe("rgb(35, 43, 53)");
    expect(
      await colour(page.getByRole("row").first(), "background-color"),
    ).toBe("rgb(18, 23, 29)");
    const tint = (day: string) =>
      colour(
        page.locator(`[data-calendar-day-bg='${day}']`),
        "background-color",
      );
    expect(await tint("2026-09-28")).toBe("rgb(17, 21, 27)");
    expect(await tint("2026-10-03")).toBe("rgb(18, 23, 29)");
    expect(await tint("2026-10-12")).toBe("rgb(19, 33, 58)");
    const fill = (key: string) =>
      colour(eventOf(page, key), "background-color");
    expect(await fill("ERP-144")).toBe("rgb(22, 40, 74)");
    expect(await fill("ERP-138")).toBe("rgb(48, 40, 26)");
    expect(await fill("ERP-129")).toBe("rgb(21, 48, 36)");
    expect(await fill("ERP-151")).toBe("rgb(30, 37, 47)");
    // The overdue fill is the mockup's translucent rgba(255,107,129,.14);
    // premultiplied alpha costs the canvas a level per channel.
    const overdue = eventOf(page, "ERP-142");
    const channels = (await colour(overdue, "background-color"))
      .match(/\d+/g)!
      .map(Number);
    [255, 107, 129].forEach((want, index) =>
      expect(Math.abs((channels[index] ?? 0) - want)).toBeLessThanOrEqual(1),
    );
    expect(await style(overdue, "background-color")).toContain("0.14");
    expect(await colour(bar(page).heading, "color")).toBe("rgb(230, 233, 238)");
    expect(await colour(bar(page).zone, "color")).toBe("rgb(143, 153, 167)");
  });

  test("khong-ket-qua: the card over the empty grid", async ({ page }) => {
    await page.clock.setFixedTime(NOW);
    await page.addInitScript(() => {
      try {
        window.localStorage.setItem("notrelix-locale", "vi");
      } catch {
        // Private mode: the default locale applies.
      }
    });
    // "Quá hạn · Ưu tiên Thấp" hides all 16 items.
    await page.goto(`${BOARD}?view=calendar&filter=p.low~o`);
    await expect(page.getByRole("grid")).toBeVisible();
    const card = page.getByRole("status");
    await expect(card).toBeVisible();
    await expect(page.locator("[data-calendar-event]")).toHaveCount(0);
    expectRect(await rel(page, card), MOCK.noResults, "no-results card");
    expect(await style(card, "border-radius")).toBe("12px");
    expect(await style(card, "padding-left")).toBe("20px");
    expect(await colour(card, "border-top-color")).toBe(COLOUR.lineStrong);
    const clear = card.getByRole("button", { name: "Bỏ lọc" });
    expect((await rel(page, clear)).height).toBeCloseTo(32, 0);
    await expect(page.getByText("1 việc chưa có ngày")).toBeVisible();
    await expect(
      page.getByText("Việc này không khớp bộ lọc nên đang ẩn."),
    ).toBeVisible();
    await clear.click();
    await expect(card).toHaveCount(0);
    await expect(page.locator("[data-calendar-event]").first()).toBeVisible();
  });

  test("tuan: one week fills the grid and its entries wrap", async ({
    page,
  }) => {
    await openCalendar(page);
    await page.getByRole("radio", { name: "Tuần" }).click();
    const heading = bar(page).heading;
    await expect(heading).toHaveText("12/10 – 18/10/2026");
    expect((await rel(page, heading)).width).toBeCloseTo(169, 0);
    const weeks = page.locator("[data-calendar-week]");
    await expect(weeks).toHaveCount(1);
    expectRect(await rel(page, weeks.first()), MOCK.week.frame, "week frame");
    expectRect(
      await rel(page, page.locator("[data-day='2026-10-12'] b")),
      MOCK.week.today,
      "today circle",
    );
    // The title wraps and the entry grows with it: 4 px padding, 26 px least.
    const entry = eventOf(page, "ERP-138");
    expect(await style(entry, "padding-top")).toBe("4px");
    expect(await style(entry, "white-space")).toBe("normal");
    expect((await rel(page, entry)).height).toBeGreaterThanOrEqual(26);
    expect(await style(entry.locator("span").nth(1), "white-space")).toBe(
      "normal",
    );
    expect(await style(entry.locator("span").nth(1), "line-height")).toBe(
      "16.2px",
    );
  });

  test("ngay: overdue section, hour slots and the now line", async ({
    page,
  }) => {
    await openCalendar(page);
    await page.getByRole("radio", { name: "Ngày" }).click();
    const d = MOCK.day;
    const heading = bar(page).heading;
    await expect(heading).toHaveText("Thứ Hai, 12/10/2026");
    expectRect(await rel(page, heading), d.heading, "day heading");
    // The bar wraps: "Thêm công việc" goes to a second line.
    expectRect(await rel(page, bar(page).primary), d.primary, "primary");
    expectRect(await rel(page, page.getByRole("grid")), d.calendar, "calendar");

    const overdue = page
      .getByRole("row")
      .filter({ has: page.getByRole("heading", { name: "Quá hạn" }) });
    expectRect(await rel(page, overdue), d.overdue, "overdue section");
    expect(await colour(overdue, "background-color")).toBe(COLOUR.overdue);
    expect(await style(overdue, "padding-left")).toBe("12px");
    expect(await style(overdue, "row-gap")).toBe("6px");
    const heading3 = overdue.getByRole("heading", { name: "Quá hạn" });
    expect(await style(heading3, "font-size")).toBe("12px");
    expect(await colour(heading3, "color")).toBe(COLOUR.overdueText);
    const item = overdue.locator("[data-calendar-event]");
    expectRect(await rel(page, item), d.overdueItem, "overdue item");
    expect(await style(item, "border-radius")).toBe("7px");
    expect(await style(item, "padding-left")).toBe("8px");
    expect(await style(item, "column-gap")).toBe("8px");
    expect(await style(item, "font-size")).toBe("13px");
    expect(await colour(item, "border-top-color")).toBe(COLOUR.lineStrong);

    const times = page.locator("time");
    await expect(times).toHaveCount(11);
    const first = await rel(page, times.first());
    expect(first.width).toBeCloseTo(d.timeWidth, 0);
    expect(await style(times.first(), "font-size")).toBe("11.5px");
    expect(await style(times.first(), "padding-left")).toBe("10px");
    for (let hour = 1; hour < 10; hour += 1) {
      const box = await rel(page, times.nth(hour));
      expect(
        Math.abs(box.y - first.y - hour * d.slotHeight),
        `slot ${hour}`,
      ).toBeLessThanOrEqual(1);
    }
    const now = page.locator("[data-calendar-now]");
    const nowBox = await rel(page, now);
    expect(nowBox.height).toBeCloseTo(d.nowHeight, 0);
    expect(nowBox.x - 25).toBeCloseTo(d.timeWidth, 0);
    expect(
      Math.abs(nowBox.y - first.y - d.nowOffset),
      "the now line at 10:40",
    ).toBeLessThanOrEqual(1);
    expect(await colour(now, "background-color")).toBe(COLOUR.accent);
    await expect(now.locator("b")).toHaveText("10:40");
    expect(await style(now.locator("b"), "font-size")).toBe("11px");
    expect(await style(now.locator("b"), "font-weight")).toBe("600");
  });
});
