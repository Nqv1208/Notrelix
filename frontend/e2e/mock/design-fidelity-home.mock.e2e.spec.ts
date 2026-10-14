import { expect, test, type Locator, type Page } from "@playwright/test";

/**
 * Home geometry against the October 2026 mockup. Every number comes from
 * `docs/design/mockups/2026-10/src/Final-Home.dc.html` rendered at 1440x900
 * with `nav=hien-day` (rail pinned, so the main area is 1108 px wide) and
 * measured with the box dump (position, size, padding, font, radius, colour)
 * for the states binh-thuong, xong-viec and dang-tai and the modes sang and
 * dem. Boxes are read relative to the top-left of the main area, so the
 * checks hold whatever sits left of it. Tolerance is 1 px for boxes and exact
 * for font sizes, weights, radii and colours.
 *
 * The mock world differs from the mockup's: it has no approval or mention
 * count, no schedule, no key, Status or Priority on an assigned item and no
 * recents until a Board is opened, so the right column, the Status pill, the
 * flag, the key and the two link chips are not drawn (they exist as boxes on
 * the Storybook stories, measured against the mockup in the same way: every
 * box of binh-thuong, xong-viec and dang-tai, light and dark, within 1 px).
 * Only boxes that do not depend on that data are compared here: the greeting,
 * the summary, the two buttons, the chips row, the section headings, the
 * rows (height, tick, title, due day) and the department mark.
 *
 * The page clock is fixed to 15/01/2025 10:40, the mock world's own "today"
 * (its items are dated from that day), in Asia/Ho_Chi_Minh.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const HOME = "/workspaces/mock-workspace-primary";
const NOW = new Date("2025-01-15T10:40:00+07:00");

interface Rect {
  readonly x: number;
  readonly y: number;
  readonly width: number;
  readonly height: number;
}

/** Final-Home binh-thuong nav=hien-day, origin: the main area (332, 0). */
const MOCK = {
  hello: { x: 32, y: 28 },
  h1: { x: 32, y: 28, height: 32.5 },
  date: { x: 32, y: 64.5, height: 20.3 },
  calendar: { y: 48.8, width: 123.23, height: 36 },
  add: { y: 50.8, width: 150.19, height: 34 },
  /** Right edge of the content: 32 + 1044. */
  contentRight: 1076,
  chips: { x: 32, y: 106.8, chipHeight: 36 },
  /** The mockup's chip row is 38 tall (two link chips); the mock world draws 36. */
  colsTop: 164.8,
  h2: { height: 20.3 },
  rows: { x: 32, radius: "12px" },
  row: { x: 33, height: 70, rowStep: 71 },
  tick: { x: 47, size: 20, dy: 25 },
  title: { x: 79, height: 20.3, dy: 15.16 },
  meta: { x: 79, height: 17.39, dy: 37.46 },
  mark: { size: 16 },
  due: { width: 100, height: 18.13, rightInset: 15 },
} as const;

const TYPE = {
  greeting: { size: "26px", weight: "650" },
  date: { size: "14px", weight: "400" },
  button: { size: "13px" },
  chip: { size: "13.5px", weight: "500" },
  heading: { size: "14px", weight: "650" },
  title: { size: "14px", weight: "500" },
  meta: { size: "12px" },
  due: { size: "12.5px", weight: "600" },
} as const;

const COLOUR = {
  accent: "rgb(22, 102, 204)",
  accentInk: "rgb(255, 255, 255)",
  panel: "rgb(255, 255, 255)",
  line: "rgb(230, 233, 238)",
  lineStrong: "rgb(210, 215, 222)",
  ink: "rgb(22, 26, 33)",
  meta: "rgb(95, 104, 117)",
  muted: "rgb(79, 88, 102)",
  danger: "rgb(196, 35, 64)",
  dangerSoft: "rgb(253, 236, 239)",
  warnText: "rgb(138, 90, 0)",
  warnSoft: "rgb(255, 244, 214)",
} as const;

async function openHome(page: Page) {
  await page.clock.setFixedTime(NOW);
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "vi");
      window.localStorage.setItem("notrelix-system-rail", "pinned");
    } catch {
      // Private mode: the defaults apply.
    }
  });
  await page.goto(HOME);
  await expect(
    page.getByRole("heading", { level: 1, name: /^Chào buổi sáng, / }),
  ).toBeVisible();
  await expect(page.locator('[data-f="row"]').first()).toBeVisible();
}

async function box(locator: Locator): Promise<Rect> {
  const found = await locator.boundingBox();
  if (!found) throw new Error("element has no box");
  return found;
}

/** A locator's box relative to the top-left of the main area. */
async function rel(page: Page, locator: Locator): Promise<Rect> {
  const [o, found] = await Promise.all([
    box(page.locator("#workspace-main")),
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

test.describe("Home geometry at 1440 px (owner)", () => {
  test.skip(
    persona !== "owner" || scenario !== "default" || faults !== "",
    "Owner checks need the default owner world without faults",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  test("the greeting, the summary and the two buttons", async ({ page }) => {
    await openHome(page);
    const h1 = page.locator('[data-f="h1"]');
    expectRect(await rel(page, h1), MOCK.h1, "greeting");
    expect(await style(h1, "font-size")).toBe(TYPE.greeting.size);
    expect(await style(h1, "font-weight")).toBe(TYPE.greeting.weight);
    expect(await style(h1, "letter-spacing")).toBe("-0.39px");
    expect(await colour(h1, "color")).toBe(COLOUR.ink);

    const date = page.locator('[data-f="date"]');
    expectRect(await rel(page, date), MOCK.date, "summary");
    expect(await style(date, "font-size")).toBe(TYPE.date.size);
    expect(await colour(date, "color")).toBe(COLOUR.muted);

    const calendar = page.getByRole("button", { name: "Lịch của tôi" });
    const add = page.getByRole("button", { name: "Thêm công việc" });
    const calendarBox = await rel(page, calendar);
    const addBox = await rel(page, add);
    expectRect(calendarBox, MOCK.calendar, "calendar");
    expectRect(addBox, MOCK.add, "add");
    // Both sit at the right edge of the 1044 px content, 12 px apart.
    expect(
      Math.abs(addBox.x + addBox.width - MOCK.contentRight),
    ).toBeLessThanOrEqual(1);
    expect(
      Math.abs(calendarBox.x + calendarBox.width + 12 - addBox.x),
    ).toBeLessThanOrEqual(1);
    for (const control of [calendar, add]) {
      expect(await style(control, "font-size")).toBe(TYPE.button.size);
      expect(await style(control, "border-radius")).toBe("8px");
    }
    expect(await style(calendar, "font-weight")).toBe("500");
    expect(await colour(calendar, "border-top-color")).toBe(COLOUR.lineStrong);
    expect(await colour(calendar, "background-color")).toBe(COLOUR.panel);
    expect(await style(add, "font-weight")).toBe("600");
    expect(await colour(add, "background-color")).toBe(COLOUR.accent);
    expect(await colour(add, "color")).toBe(COLOUR.accentInk);
  });

  test("the quick filters: overdue and due today", async ({ page }) => {
    await openHome(page);
    const chips = page.getByRole("group", { name: "Lọc nhanh việc cần chú ý" });
    expectRect(
      await rel(page, chips),
      { x: MOCK.chips.x, y: MOCK.chips.y, height: MOCK.chips.chipHeight },
      "chips row",
    );
    const overdue = chips.locator('[data-f="chip"]').nth(0);
    const today = chips.locator('[data-f="chip"]').nth(1);
    const overdueBox = await rel(page, overdue);
    expectRect(
      overdueBox,
      { x: MOCK.chips.x, y: MOCK.chips.y, height: MOCK.chips.chipHeight },
      "overdue chip",
    );
    const todayBox = await rel(page, today);
    // 8 px between chips.
    expect(
      Math.abs(overdueBox.x + overdueBox.width + 8 - todayBox.x),
    ).toBeLessThanOrEqual(1);
    for (const chip of [overdue, today]) {
      expect(await style(chip, "font-size")).toBe(TYPE.chip.size);
      expect(await style(chip, "font-weight")).toBe(TYPE.chip.weight);
      expect(await style(chip, "border-radius")).toBe("10px");
    }
    expect(await colour(overdue, "background-color")).toBe(COLOUR.dangerSoft);
    expect(await colour(overdue, "color")).toBe(COLOUR.danger);
    expect(await colour(today, "background-color")).toBe(COLOUR.warnSoft);
    expect(await colour(today, "color")).toBe(COLOUR.warnText);
    // The count is the bold tabular number.
    expect(await style(overdue.locator("b"), "font-weight")).toBe("700");
  });

  test("the section headings and the panel they sit over", async ({ page }) => {
    await openHome(page);
    const headings = page.locator('[data-f="h2"]');
    const first = headings.first();
    const firstBox = await rel(page, first);
    expectRect(
      firstBox,
      { x: MOCK.chips.x, y: MOCK.colsTop, height: MOCK.h2.height },
      "first heading",
    );
    expect(await style(first, "font-size")).toBe(TYPE.heading.size);
    expect(await style(first, "font-weight")).toBe(TYPE.heading.weight);
    expect(await colour(first, "color")).toBe(COLOUR.danger);
    expect(await style(first, "column-gap")).toBe("8px");
    // The heading is 8 px above its panel.
    const rows = page.locator('[data-f="rows"]').first();
    const rowsBox = await rel(page, rows);
    expect(
      Math.abs(rowsBox.y - (firstBox.y + firstBox.height + 8)),
    ).toBeLessThanOrEqual(1);
    expect(rowsBox.x).toBeCloseTo(MOCK.rows.x, 0);
    expect(await style(rows, "border-radius")).toBe(MOCK.rows.radius);
    expect(await colour(rows, "border-top-color")).toBe(COLOUR.line);
    expect(await colour(rows, "background-color")).toBe(COLOUR.panel);
    // Panels are 18 px apart.
    const second = headings.nth(1);
    expect(await colour(second, "color")).toBe(COLOUR.ink);
    const secondBox = await rel(page, second);
    expect(
      Math.abs(secondBox.y - (rowsBox.y + rowsBox.height + 18)),
    ).toBeLessThanOrEqual(1);
  });

  test("a row: height, tick, title, Board line and due day", async ({
    page,
  }) => {
    await openHome(page);
    const rows = page.locator('[data-f="rows"]').first();
    const rowsBox = await rel(page, rows);
    const row = rows.locator('[data-f="row"]').first();
    const second = rows.locator('[data-f="row"]').nth(1);
    const rowBox = await rel(page, row);
    expectRect(rowBox, { x: MOCK.row.x, height: MOCK.row.height }, "row");
    expect(rowBox.y).toBeCloseTo(rowsBox.y + 1, 0);
    // Rows after the first carry a 1 px rule.
    const secondBox = await rel(page, second);
    expect(Math.abs(secondBox.y - (rowBox.y + MOCK.row.rowStep))).toBeLessThan(
      1.01,
    );
    expect(await colour(second, "border-top-color")).toBe(COLOUR.line);
    expect(await style(row, "column-gap")).toBe("12px");
    expect(await style(row, "padding-left")).toBe("14px");

    const tick = row.getByRole("button", { name: /^Đánh dấu hoàn thành / });
    expectRect(
      await rel(page, tick),
      {
        x: MOCK.tick.x,
        y: rowBox.y + MOCK.tick.dy,
        width: MOCK.tick.size,
        height: MOCK.tick.size,
      },
      "tick",
    );
    expect(await style(tick, "border-radius")).toMatch(
      /^(50%|[0-9.]+e\+\d+px|9999px|10px)$/,
    );
    // 1.5 px rounds down to a device pixel.
    expect(await style(tick, "border-top-width")).toBe("1px");
    expect(await colour(tick, "border-top-color")).toBe(COLOUR.lineStrong);

    const title = row.locator('[data-f="rt"]');
    expectRect(
      await rel(page, title),
      {
        x: MOCK.title.x,
        y: rowBox.y + MOCK.title.dy,
        height: MOCK.title.height,
      },
      "title",
    );
    expect(await style(title, "font-size")).toBe(TYPE.title.size);
    expect(await style(title, "font-weight")).toBe(TYPE.title.weight);
    expect(await colour(title, "color")).toBe(COLOUR.ink);

    const meta = row.locator('[data-f="rc"]');
    expectRect(
      await rel(page, meta),
      { x: MOCK.meta.x, y: rowBox.y + MOCK.meta.dy, height: MOCK.meta.height },
      "Board line",
    );
    expect(await style(meta, "font-size")).toBe(TYPE.meta.size);
    expect(await colour(meta, "color")).toBe(COLOUR.meta);

    const due = row.locator('[data-f="due"]');
    const dueBox = await rel(page, due);
    expect(Math.abs(dueBox.width - MOCK.due.width)).toBeLessThanOrEqual(1);
    expect(
      Math.abs(
        rowsBox.x +
          rowsBox.width -
          (dueBox.x + dueBox.width) -
          MOCK.due.rightInset,
      ),
    ).toBeLessThanOrEqual(1);
    expect(await style(due, "font-size")).toBe(TYPE.due.size);
    expect(await style(due, "font-weight")).toBe(TYPE.due.weight);
    expect(await style(due, "text-align")).toBe("right");
    // The first window is overdue: the danger colour.
    expect(await colour(due, "color")).toBe(COLOUR.danger);
  });

  test("the department mark in the Board line", async ({ page }) => {
    await openHome(page);
    const meta = page
      .locator('[data-f="rc"]')
      .filter({ hasText: "Triển khai ERP Q4" })
      .first();
    const mark = meta.locator('span[aria-hidden="true"]').first();
    const metaBox = await rel(page, meta);
    const markBox = await rel(page, mark);
    expectRect(
      markBox,
      { x: MOCK.meta.x, width: MOCK.mark.size, height: MOCK.mark.size },
      "mark",
    );
    // Vertically centred in the 17.4 px line (0.7 px lower in the mockup).
    expect(Math.abs(markBox.y - (metaBox.y + 0.68))).toBeLessThanOrEqual(1);
    expect(await style(mark, "border-radius")).toBe("4px");
    expect(await style(mark, "font-size")).toBe("7.5px");
    expect(await style(mark, "font-weight")).toBe("700");
    // 6 px between the mark and the Board name.
    expect(await style(meta, "column-gap")).toBe("6px");
  });

  test("Continue: heading, Board card and its icon tile", async ({ page }) => {
    await page.clock.setFixedTime(NOW);
    await page.addInitScript(() => {
      try {
        window.localStorage.setItem("notrelix-locale", "vi");
        window.localStorage.setItem("notrelix-system-rail", "pinned");
      } catch {
        // Private mode: the defaults apply.
      }
    });
    // Opening a Board records it as a recent; Home then lists it.
    await page.goto(`${HOME}/boards/mock-board-erp`);
    await page
      .getByRole("navigation", { name: "Điều hướng không gian làm việc" })
      .getByRole("link", { name: "Trang chủ" })
      .click();
    const heading = page.locator('[data-f="conth2"]');
    await expect(heading).toBeVisible();
    // The right column: 300.2 px at x 775.8, 22 px right of the 715.8 px main column.
    const column = page.locator('[data-f="colr"]');
    expectRect(
      await rel(page, column),
      { x: 775.8, y: MOCK.colsTop, width: 300.2 },
      "right column",
    );
    expect(await style(heading, "font-size")).toBe(TYPE.heading.size);
    expect(await style(heading, "font-weight")).toBe(TYPE.heading.weight);
    const card = page.locator('[data-f="cont"]').first();
    const cardBox = await rel(page, card);
    const headingBox = await rel(page, heading);
    expectRect(
      cardBox,
      { x: 767.8, width: 316.2, height: 50.95 },
      "Continue card",
    );
    // 8 px under the heading.
    expect(
      Math.abs(cardBox.y - (headingBox.y + headingBox.height + 8)),
    ).toBeLessThanOrEqual(1);
    expect(await style(card, "border-radius")).toBe("8px");
    const tile = page.locator('[data-f="contic"]').first();
    expectRect(
      await rel(page, tile),
      { x: 775.8, width: 32, height: 32 },
      "icon tile",
    );
    expect(await style(tile, "border-radius")).toBe("8px");
    expect(await colour(tile, "border-top-color")).toBe(COLOUR.line);
    const title = card.locator("b");
    expect(await style(title, "font-size")).toBe("13.5px");
    expect(await style(title, "font-weight")).toBe("500");
    const sub = card.locator("small");
    expect(await style(sub, "font-size")).toBe("12px");
    expect(await colour(sub, "color")).toBe(COLOUR.meta);
  });
});
