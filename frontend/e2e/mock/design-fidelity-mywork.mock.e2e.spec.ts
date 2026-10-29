import { expect, test, type Locator, type Page } from "@playwright/test";

/**
 * My work geometry against the October 2026 mockup. Every number comes from
 * `docs/design/mockups/2026-10/src/Final-MyWork.dc.html` rendered at 1440x900
 * with `nav=hien-day` (rail pinned, so the main area is 1108 px wide) and
 * measured with the box dump (position, size, padding, font, radius, colour)
 * for the states tat-ca, theo-board, chon-nhieu, da-xong, trong, chi-tiet,
 * chon-nhom, doi-trang-thai and them-viec and the modes sang and dem. Boxes
 * are read relative to the top-left of the page (the main area), so the
 * checks hold whatever sits left of it. Tolerance is 1 px for boxes and exact
 * for font sizes, weights, radii and colours.
 *
 * The mock world differs from the mockup's: items carry no key, Status,
 * Priority, person or approval, there is no waiting-on card, and 32 items
 * from one department are open instead of 7 from three. The column cells,
 * the approval line, the done rows, the bulk bar's tooltip, the Status
 * popover, the waiting-on card and the sheet are therefore measured on the
 * Storybook stories (every state above, light and dark, within 1 px of the
 * mockup); here only boxes that do not depend on that data are compared: the
 * title row, the tabs, the bar and its pills, the search, the group heading,
 * the row's select box, tick, title and Board line, the due column, the week
 * card and the foot.
 *
 * The page clock is fixed to 12/10/2026 10:40, the mockups' own "today"
 * (its items are dated from that day), in Asia/Ho_Chi_Minh.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const PAGE = "/workspaces/mock-workspace-primary/my-work";
const NOW = new Date("2026-10-12T10:40:00+07:00");

interface Rect {
  readonly x: number;
  readonly y: number;
  readonly width: number;
  readonly height: number;
}

/** Final-MyWork tat-ca nav=hien-day, origin: the main area (332, 0). */
const MOCK = {
  h1: { x: 28, y: 22, height: 30 },
  sub: { x: 28, y: 56, height: 19.56 },
  calendar: { y: 43.56, width: 123.23, height: 32 },
  /** Right edge of the title row: 1108 - 28. */
  contentRight: 1080,
  tabs: { x: 0, y: 75.56, width: 1108, height: 48 },
  tab: { x: 28, y: 87.56, height: 36 },
  tabCount: { y: 95.56, height: 18 },
  bar: { y: 123.56, height: 52 },
  pills: [
    { x: 28, y: 133.56, width: 164.09, height: 32 },
    { x: 213.09, y: 133.56, width: 128.44, height: 32 },
    { x: 347.53, y: 133.56, width: 77.45, height: 32 },
    { x: 430.98, y: 133.56, width: 110.94, height: 32 },
  ],
  field: { y: 133.56, width: 220, height: 32 },
  add: { y: 133.56, width: 114.55, height: 32 },
  // 683.44 + the 41 px hint strip, which is gone (owner rule: no shortcut hints).
  body: { y: 175.56, height: 724.44 },
  lcol: { x: 28, y: 175.56, width: 728 },
  group: { x: 30, y: 181.56, width: 724, height: 24 },
  toggle: { x: 26, width: 24, height: 24 },
  rows: { x: 30, y: 213.56, width: 724 },
  row: { x: 31, width: 722, minHeight: 60 },
  select: { x: 45, size: 16 },
  tick: { x: 73, size: 20 },
  title: { x: 105 },
  /** The due column: 112 wide, 232 px before the right edge of the row. */
  due: { width: 112 },
  rail: { x: 780, y: 175.56, width: 300 },
  weekCard: { x: 780, y: 181.56, width: 300 },
  weekHeading: { x: 795, y: 194.56, width: 270, height: 19.56 },
  weekGrid: { x: 795, y: 224.13, width: 270, height: 105.45 },
  day: { y: 224.13, height: 105.45 },
  foot: { x: 0, y: 859, width: 1108, height: 41 },
} as const;

const TYPE = {
  title: { size: "24px", weight: "650", letterSpacing: "-0.36px" },
  sub: { size: "13.5px", weight: "400" },
  button: { size: "13px" },
  tab: { size: "13.5px", on: "600", off: "500" },
  tabCount: { size: "12px", weight: "600" },
  pill: { size: "13px", weight: "400" },
  group: { size: "14px", weight: "650" },
  count: { size: "14px", weight: "500" },
  rowTitle: { size: "14px", weight: "500" },
  meta: { size: "12px" },
  key: { size: "11.5px" },
  due: { size: "12.5px", weight: "600" },
  duePrefix: { size: "11.5px" },
  cardHeading: { size: "13.5px", weight: "650" },
  dayLabel: { size: "11px", weight: "600" },
  dayNumber: { size: "12.5px", weight: "600" },
  dayCount: { size: "12px", weight: "650" },
  foot: { size: "12.5px" },
} as const;

const COLOUR = {
  accent: "rgb(22, 102, 204)",
  accentInk: "rgb(255, 255, 255)",
  accentSoft: "rgb(231, 240, 252)",
  accentText: "rgb(15, 86, 174)",
  panel: "rgb(255, 255, 255)",
  line: "rgb(230, 233, 238)",
  lineStrong: "rgb(210, 215, 222)",
  hover2: "rgb(230, 233, 238)",
  ink: "rgb(22, 26, 33)",
  meta: "rgb(95, 104, 117)",
  muted: "rgb(79, 88, 102)",
  danger: "rgb(196, 35, 64)",
  control: "rgb(122, 131, 145)",
} as const;

async function openMyWork(page: Page) {
  await page.clock.setFixedTime(NOW);
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "vi");
      window.localStorage.setItem("notrelix-system-rail", "pinned");
    } catch {
      // Private mode: the defaults apply.
    }
  });
  await page.goto(PAGE);
  await expect(
    page.getByRole("heading", { level: 1, name: "Việc của tôi" }),
  ).toBeVisible();
  await expect(page.locator('[data-f="row"]').first()).toBeVisible();
}

async function box(locator: Locator): Promise<Rect> {
  const found = await locator.boundingBox();
  if (!found) throw new Error("element has no box");
  return found;
}

/** A locator's box relative to the top-left of the page (the main area). */
async function rel(page: Page, locator: Locator): Promise<Rect> {
  const [o, found] = await Promise.all([
    box(page.locator('[data-f="main"]')),
    box(locator),
  ]);
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

test.describe("My work geometry at 1440 px (owner)", () => {
  test.skip(
    persona !== "owner" || scenario !== "default" || faults !== "",
    "Owner checks need the default owner world without faults",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  test("the title row: title, summary line and My calendar", async ({
    page,
  }) => {
    await openMyWork(page);
    const h1 = page.locator('[data-f="h1"]');
    expectRect(await rel(page, h1), MOCK.h1, "title");
    expect(await style(h1, "font-size")).toBe(TYPE.title.size);
    expect(await style(h1, "font-weight")).toBe(TYPE.title.weight);
    expect(await style(h1, "letter-spacing")).toBe(TYPE.title.letterSpacing);
    expect(await colour(h1, "color")).toBe(COLOUR.ink);

    const sub = page.locator('[data-f="sub"]');
    expectRect(await rel(page, sub), MOCK.sub, "summary");
    expect(await style(sub, "font-size")).toBe(TYPE.sub.size);
    expect(await style(sub, "font-weight")).toBe(TYPE.sub.weight);
    expect(await colour(sub, "color")).toBe(COLOUR.muted);
    await expect(sub).toHaveText(
      /^\d+\+? việc đang mở trên \d+ phòng ban · cập nhật 10:40$/,
    );

    const calendar = page.getByRole("button", { name: "Lịch của tôi" });
    const box_ = await rel(page, calendar);
    expectRect(box_, MOCK.calendar, "calendar");
    expect(
      Math.abs(box_.x + box_.width - MOCK.contentRight),
    ).toBeLessThanOrEqual(1);
    expect(await style(calendar, "font-size")).toBe(TYPE.button.size);
    expect(await style(calendar, "border-radius")).toBe("8px");
    expect(await colour(calendar, "border-top-color")).toBe(COLOUR.lineStrong);
    expect(await colour(calendar, "background-color")).toBe(COLOUR.panel);
  });

  test("the tabs: strip, selected tab, counts", async ({ page }) => {
    await openMyWork(page);
    const strip = page.getByRole("tablist", { name: "Lọc việc của tôi" });
    expectRect(await rel(page, strip), MOCK.tabs, "tab strip");
    expect(await colour(strip, "border-bottom-color")).toBe(COLOUR.line);
    const tabs = strip.getByRole("tab");
    await expect(tabs).toHaveCount(5);
    const first = tabs.nth(0);
    expectRect(
      await rel(page, first),
      { x: MOCK.tab.x, y: MOCK.tab.y, height: MOCK.tab.height },
      "first tab",
    );
    expect(await style(first, "font-size")).toBe(TYPE.tab.size);
    expect(await style(first, "font-weight")).toBe(TYPE.tab.on);
    expect(await colour(first, "color")).toBe(COLOUR.ink);
    expect(await colour(first, "border-bottom-color")).toBe(COLOUR.accent);
    expect(await style(first, "border-bottom-width")).toBe("2px");
    expect(await style(tabs.nth(1), "font-weight")).toBe(TYPE.tab.off);
    expect(await colour(tabs.nth(1), "color")).toBe(COLOUR.muted);

    const count = first.locator('[data-f="tabn"]');
    expectRect(
      await rel(page, count),
      { y: MOCK.tabCount.y, height: MOCK.tabCount.height },
      "tab count",
    );
    expect(await style(count, "font-size")).toBe(TYPE.tabCount.size);
    expect(await style(count, "font-weight")).toBe(TYPE.tabCount.weight);
    expect(await colour(count, "background-color")).toBe(COLOUR.accentSoft);
    expect(await colour(count, "color")).toBe(COLOUR.accentText);
    // 7 px between a tab's name and its count; tabs touch 2 px apart.
    const a = await rel(page, tabs.nth(0));
    const b = await rel(page, tabs.nth(1));
    expect(Math.abs(a.x + a.width + 2 - b.x)).toBeLessThanOrEqual(1);
  });

  test("the bar: group, filters, search and Thêm việc", async ({ page }) => {
    await openMyWork(page);
    const bar = page.locator('[data-f="bar"]');
    expectRect(
      await rel(page, bar),
      { y: MOCK.bar.y, height: MOCK.bar.height },
      "bar",
    );
    const pills = bar.locator('[data-f="pill"]');
    await expect(pills).toHaveCount(4);
    for (const [index, want] of MOCK.pills.entries()) {
      const pill = pills.nth(index);
      expectRect(await rel(page, pill), want, `pill ${index}`);
      expect(await style(pill, "font-size")).toBe(TYPE.pill.size);
      expect(await style(pill, "font-weight")).toBe(TYPE.pill.weight);
      expect(await style(pill, "border-radius")).toBe("7px");
      expect(await colour(pill, "color")).toBe(COLOUR.muted);
    }
    // The selected grouping is the bold value.
    expect(await style(pills.nth(0).locator("b"), "font-weight")).toBe("600");
    expect(await colour(pills.nth(0).locator("b"), "color")).toBe(COLOUR.ink);

    const field = page.locator('[data-f="field"]');
    const fieldBox = await rel(page, field);
    expectRect(fieldBox, MOCK.field, "search");
    expect(await style(field, "border-radius")).toBe("7px");
    expect(await colour(field, "border-top-color")).toBe(COLOUR.lineStrong);
    expect(await colour(field, "background-color")).toBe(COLOUR.panel);
    const add = page.getByRole("button", { name: "Thêm việc" });
    const addBox = await rel(page, add);
    expectRect(addBox, MOCK.add, "add");
    expect(
      Math.abs(addBox.x + addBox.width - MOCK.contentRight),
    ).toBeLessThanOrEqual(1);
    // 6 px between the search and the button.
    expect(
      Math.abs(fieldBox.x + fieldBox.width + 6 - addBox.x),
    ).toBeLessThanOrEqual(1);
    expect(await style(add, "font-weight")).toBe("600");
    expect(await colour(add, "background-color")).toBe(COLOUR.accent);
    expect(await colour(add, "color")).toBe(COLOUR.accentInk);
  });

  test("the body: list column, first group heading and the panel under it", async ({
    page,
  }) => {
    await openMyWork(page);
    const lcol = page.locator('[data-f="lcol"]');
    expectRect(
      await rel(page, lcol),
      { x: MOCK.lcol.x, y: MOCK.lcol.y, width: MOCK.lcol.width },
      "list column",
    );
    expectRect(
      await rel(page, page.locator('[data-f="body"]')),
      { y: MOCK.body.y, height: MOCK.body.height },
      "body",
    );

    const heading = page.locator('[data-f="gh"]').first();
    const headingBox = await rel(page, heading);
    expectRect(headingBox, MOCK.group, "group heading");
    expect(await style(heading, "font-size")).toBe(TYPE.group.size);
    expect(await style(heading, "font-weight")).toBe(TYPE.group.weight);
    expect(await colour(heading, "color")).toBe(COLOUR.danger);
    expect(await style(heading, "column-gap")).toBe("8px");
    const toggle = heading.locator('[data-f="tog"]');
    expectRect(
      await rel(page, toggle),
      {
        x: MOCK.toggle.x,
        width: MOCK.toggle.width,
        height: MOCK.toggle.height,
      },
      "collapse toggle",
    );
    expect(await style(toggle, "border-radius")).toBe("6px");
    const count = heading.locator('[data-f="gn"]');
    expect(await style(count, "font-size")).toBe(TYPE.count.size);
    expect(await style(count, "font-weight")).toBe(TYPE.count.weight);
    expect(await colour(count, "color")).toBe(COLOUR.meta);

    const rows = page.locator('[data-f="rows"]').first();
    const rowsBox = await rel(page, rows);
    expectRect(rowsBox, MOCK.rows, "panel");
    // 8 px under the heading.
    expect(
      Math.abs(rowsBox.y - (headingBox.y + headingBox.height + 8)),
    ).toBeLessThanOrEqual(1);
    expect(await style(rows, "border-radius")).toBe("12px");
    expect(await colour(rows, "border-top-color")).toBe(COLOUR.line);
    expect(await colour(rows, "background-color")).toBe(COLOUR.panel);
  });

  test("a row: select box, tick, title, Board line and due day", async ({
    page,
  }) => {
    await openMyWork(page);
    // Rows 1 and 2 are the two of the "Hôm nay" group (row 0 is the only
    // overdue one): the first group with a rule between rows.
    const row = page.locator('[data-f="row"]').nth(1);
    const second = page.locator('[data-f="row"]').nth(2);
    const rowBox = await rel(page, row);
    expectRect(rowBox, { x: MOCK.row.x, width: MOCK.row.width }, "row");
    expect(rowBox.height).toBeGreaterThanOrEqual(MOCK.row.minHeight - 1);
    expect(await style(row, "column-gap")).toBe("10px");
    expect(await style(row, "padding-left")).toBe("14px");
    expect(await colour(second, "border-top-color")).toBe(COLOUR.line);
    // The first row has no rule, the others a 1 px one.
    expect(await style(row, "border-top-width")).toBe("0px");
    expect(await style(second, "border-top-width")).toBe("1px");

    const centre = rowBox.y + rowBox.height / 2;
    const select = row.getByRole("checkbox", { name: /^Chọn / });
    const selectBox = await rel(page, select);
    expectRect(
      selectBox,
      { x: MOCK.select.x, width: MOCK.select.size, height: MOCK.select.size },
      "select box",
    );
    expect(
      Math.abs(selectBox.y + selectBox.height / 2 - centre),
    ).toBeLessThanOrEqual(1);
    // Hidden until the row is hovered, focused or something is selected.
    expect(await style(select, "opacity")).toBe("0");
    expect(await style(select, "border-radius")).toBe("4px");
    expect(await colour(select, "border-top-color")).toBe(COLOUR.control);
    await row.hover();
    await expect(select).toHaveCSS("opacity", "1");

    const tick = row.getByRole("button", { name: /^Đánh dấu xong / });
    const tickBox = await rel(page, tick);
    expectRect(
      tickBox,
      { x: MOCK.tick.x, width: MOCK.tick.size, height: MOCK.tick.size },
      "tick",
    );
    expect(
      Math.abs(tickBox.y + tickBox.height / 2 - centre),
    ).toBeLessThanOrEqual(1);
    expect(await colour(tick, "border-top-color")).toBe(COLOUR.control);

    const title = row.locator('[data-f="rt"]');
    expectRect(await rel(page, title), { x: MOCK.title.x }, "title");
    expect(await style(title, "font-size")).toBe(TYPE.rowTitle.size);
    expect(await style(title, "font-weight")).toBe(TYPE.rowTitle.weight);
    expect(await style(title, "line-height")).toBe("19.6px");
    expect(await colour(title, "color")).toBe(COLOUR.ink);
    const meta = row.locator('[data-f="rc"]');
    expect(await style(meta, "font-size")).toBe(TYPE.meta.size);
    expect(await style(meta, "column-gap")).toBe("6px");
    expect(await colour(meta, "color")).toBe(COLOUR.meta);
    const why = row.locator('[data-f="why"]');
    expect(await style(why, "font-size")).toBe(TYPE.key.size);
    expect(await style(why, "font-weight")).toBe("600");
    expect(await style(why, "height")).toBe("20px");

    const due = row.locator('[data-f="cdu"]');
    const dueBox = await rel(page, due);
    expect(Math.abs(dueBox.width - MOCK.due.width)).toBeLessThanOrEqual(1);
    const line = due.locator('[data-f="du"]');
    expect(await style(line, "font-size")).toBe(TYPE.due.size);
    expect(await style(line, "font-weight")).toBe(TYPE.due.weight);
    // Today: the warning colour (the first row's own window is overdue).
    expect(await colour(line, "color")).toBe("rgb(138, 90, 0)");
    const overdue = page
      .locator('[data-f="row"]')
      .first()
      .locator('[data-f="du"]');
    expect(await colour(overdue, "color")).toBe(COLOUR.danger);
    await expect(overdue).toContainText("Quá hạn 2 ngày");
    // The overdue day's own date sits under it, small and quiet.
    const sub = overdue.locator("small");
    expect(await style(sub, "font-size")).toBe(TYPE.duePrefix.size);
    expect(await style(sub, "font-weight")).toBe("400");
    expect(await colour(sub, "color")).toBe(COLOUR.meta);
    // The person in charge's column ends the row: 14 px padding.
    const rowRight = rowBox.x + rowBox.width;
    const avatarBox = await rel(page, row.locator('[data-f="cav"]'));
    expect(
      Math.abs(rowRight - (avatarBox.x + avatarBox.width) - 14),
    ).toBeLessThanOrEqual(1);
  });

  test("the department mark in the Board line", async ({ page }) => {
    await openMyWork(page);
    const meta = page
      .locator('[data-f="rc"]')
      .filter({ hasText: "Triển khai ERP Q4" })
      .first();
    const mark = meta.locator('span[aria-hidden="true"]').first();
    const markBox = await rel(page, mark);
    expect(markBox.width).toBeCloseTo(16, 0);
    expect(markBox.height).toBeCloseTo(16, 0);
    expect(markBox.x).toBeCloseTo(MOCK.title.x, 0);
    expect(await style(mark, "border-radius")).toBe("4px");
    expect(await style(mark, "font-size")).toBe("7.5px");
    expect(await style(mark, "font-weight")).toBe("700");
  });

  test("the right rail: week card, its days and the department card", async ({
    page,
  }) => {
    await openMyWork(page);
    const rail = page.getByRole("complementary", {
      name: "Tóm tắt việc của tôi",
    });
    expectRect(await rel(page, rail), MOCK.rail, "rail");
    const cards = rail.locator('[data-f="rcard"]');
    // The week, the departments and who the member waits on.
    await expect(cards).toHaveCount(3);
    const week = cards.nth(0);
    expectRect(await rel(page, week), MOCK.weekCard, "week card");
    expect(await style(week, "border-radius")).toBe("12px");
    expect(await colour(week, "border-top-color")).toBe(COLOUR.line);
    expect(await colour(week, "background-color")).toBe(COLOUR.panel);
    const heading = week.locator('[data-f="rh2"]');
    expectRect(await rel(page, heading), MOCK.weekHeading, "week heading");
    expect(await style(heading, "font-size")).toBe(TYPE.cardHeading.size);
    expect(await style(heading, "font-weight")).toBe(TYPE.cardHeading.weight);

    const grid = week.locator('[data-f="wk"]');
    expectRect(await rel(page, grid), MOCK.weekGrid, "week grid");
    expect(await style(grid, "column-gap")).toBe("2px");
    const days = grid.locator('[data-f="day"]');
    await expect(days).toHaveCount(7);
    const dayBox = await rel(page, days.nth(0));
    expectRect(dayBox, { y: MOCK.day.y, height: MOCK.day.height }, "day");
    expect(Math.abs(dayBox.width - 36.84)).toBeLessThanOrEqual(1);
    // Today is the first day, on the accent soft tone.
    expect(await colour(days.nth(0), "background-color")).toBe(
      COLOUR.accentSoft,
    );
    expect(await colour(days.nth(1), "background-color")).not.toBe(
      COLOUR.accentSoft,
    );
    const label = days.nth(0).locator('[data-f="dl"]');
    expect(await style(label, "font-size")).toBe(TYPE.dayLabel.size);
    expect(await style(label, "font-weight")).toBe(TYPE.dayLabel.weight);
    expect(await colour(label, "color")).toBe(COLOUR.accentText);
    const number = days.nth(0).locator('[data-f="dd"]');
    expect(await style(number, "font-size")).toBe(TYPE.dayNumber.size);
    expect(await style(number, "font-weight")).toBe(TYPE.dayNumber.weight);
    const total = days.nth(0).locator('[data-f="dn"]');
    expect(await style(total, "font-size")).toBe(TYPE.dayCount.size);
    expect(await style(total, "font-weight")).toBe(TYPE.dayCount.weight);
    const bar = days.nth(0).locator('[data-f="bv"] i');
    const barBox = await box(bar);
    expect(barBox.width).toBeCloseTo(14, 0);
    expect(await style(bar, "border-radius")).toBe("3px 3px 1px 1px");
    expect(await colour(bar, "background-color")).toBe(COLOUR.accent);

    // The overdue line under the grid.
    const note = week.locator('[data-f="wknote"]');
    expect(await colour(note, "color")).toBe(COLOUR.danger);
    expect(await style(note, "font-size")).toBe("12.5px");
    expect(await style(note, "font-weight")).toBe("600");
    expect(await colour(note, "border-top-color")).toBe(COLOUR.line);

    const departments = cards.nth(1);
    const weekBox = await rel(page, week);
    const departmentsBox = await rel(page, departments);
    // 14 px between the cards.
    expect(
      Math.abs(departmentsBox.y - (weekBox.y + weekBox.height + 14)),
    ).toBeLessThanOrEqual(1);
    expectRect(departmentsBox, { x: 780, width: 300 }, "department card");
    const row = departments.locator('[data-f="deprow"]').first();
    const rowBox = await rel(page, row);
    expect(Math.abs(rowBox.x - 787)).toBeLessThanOrEqual(1);
    expect(Math.abs(rowBox.width - 286)).toBeLessThanOrEqual(1);
    expect(Math.abs(rowBox.height - 41)).toBeLessThanOrEqual(1);
    const track = row.locator('[data-f="trk"]');
    const trackBox = await rel(page, track);
    expect(Math.abs(trackBox.width - 240)).toBeLessThanOrEqual(1);
    expect(Math.abs(trackBox.height - 4)).toBeLessThanOrEqual(1);
    expect(await colour(track, "background-color")).toBe(COLOUR.hover2);
    expect(await style(track, "border-radius")).toBe("2px");
  });

  test("the keyboard hint under the list is gone", async ({ page }) => {
    await openMyWork(page);
    // Owner rule: no visible shortcut hints; the body takes the strip's 41 px.
    await expect(page.locator('[data-f="foot"]')).toHaveCount(0);
  });

  test("the group menu and the create popover hang under their buttons", async ({
    page,
  }) => {
    await openMyWork(page);
    await page.getByRole("button", { name: /^Nhóm/ }).click();
    const menu = page.getByRole("menu", { name: "Nhóm việc" });
    // Popovers are content-box: 236 + 2 x 6 padding + 2 x 1 border.
    expectRect(
      await rel(page, menu),
      { x: 28, y: 171.56, width: 250, height: 143.39 },
      "group menu",
    );
    expect(await style(menu, "border-radius")).toBe("10px");
    expect(await colour(menu, "border-top-color")).toBe(COLOUR.lineStrong);
    const items = menu.getByRole("menuitemradio");
    await expect(items).toHaveCount(3);
    expectRect(
      await rel(page, items.nth(0)),
      { x: 35, width: 236, height: 34 },
      "menu item",
    );
    await page.keyboard.press("Escape");
    await expect(menu).toHaveCount(0);

    await page.getByRole("button", { name: "Thêm việc" }).click();
    const create = page.getByRole("dialog", { name: "Thêm việc" });
    const createBox = await rel(page, create);
    expect(Math.abs(createBox.width - 370)).toBeLessThanOrEqual(1);
    // Right edges line up with the button: 1108 - 28.
    expect(Math.abs(createBox.x + createBox.width - 1080)).toBeLessThanOrEqual(
      1,
    );
    expect(Math.abs(createBox.y - 173.56)).toBeLessThanOrEqual(1);
    const submit = create.getByRole("button", { name: /^Tạo việc/ });
    await expect(submit).toHaveAttribute("aria-disabled", "true");
  });
});
