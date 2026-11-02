import { expect, test, type Locator, type Page } from "@playwright/test";

/**
 * The board table at tablet width against `Final-T-Table` (the 768-1279 band, drawn at 1180x820 with the sidebar
 * folded): frames binh-thuong, sua-o, chon-nhieu and khong-ket-qua. Every number was read from the rendered
 * artboard (`render.py Final-T-Table.dc.html out.html state=<state> sidebar=thu-gon`, boxes relative to `.tb-grid`
 * and, for the due picker and the bulk bar, to their own box). The app's boxes are read relative to the
 * `role=table` element, so what sits left of or above the table does not matter. Tolerance is 1 px.
 *
 * Not compared, because the app has no twin box for it yet or the wording differs: the toolbar chips, the
 * footer inside the card, the scroll fade and "next columns" button, the split pane, the page header, the title
 * column's text offset (the app adds a sub-item expander), the time select's width (a native select draws its own
 * caret) and the bulk bar's width (it follows the wording).
 *
 * Dark: the artboard's dark render holds every box of binh-thuong and khong-ket-qua within 1.5 px of its light
 * twin (text widths only), so the app's dark page is held to its own light page, box by box, within 0.5 px.
 * Colours belong to the token set (`check:design-tokens`).
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const NOW = new Date("2026-10-12T10:40:00+07:00");

test.skip(
  persona !== "owner" || scenario !== "default" || faults !== "",
  "Needs the default owner world without faults",
);
test.use({ viewport: { width: 1180, height: 820 } });

interface Rect {
  readonly x: number;
  readonly y: number;
  readonly width: number;
  readonly height: number;
}

async function openTable(page: Page, query = "", dark = false) {
  await page.clock.setFixedTime(NOW);
  await page.addInitScript(
    (mode) => {
      try {
        window.localStorage.setItem("notrelix-locale", "vi");
        window.localStorage.setItem("notrelix-theme-mode", mode);
      } catch {
        // Private mode: the defaults apply.
      }
    },
    dark ? "dark" : "light",
  );
  await page.goto(`${BOARD}?view=table${query}`);
  await expect(page.getByRole("table")).toBeVisible();
  await page.waitForTimeout(500);
}

/** A box relative to an origin box's top-left. */
async function within(origin: Locator, target: Locator): Promise<Rect> {
  const [o, b] = await Promise.all([
    origin.boundingBox(),
    target.boundingBox(),
  ]);
  if (!o || !b) throw new Error("element has no box");
  return { x: b.x - o.x, y: b.y - o.y, width: b.width, height: b.height };
}

function expectRect(actual: Rect, want: Partial<Rect>, label: string) {
  for (const key of ["x", "y", "width", "height"] as const) {
    const expected = want[key];
    if (expected === undefined) continue;
    expect(
      Math.abs(actual[key] - expected),
      `${label}.${key}: ${actual[key].toFixed(1)} vs ${expected}`,
    ).toBeLessThanOrEqual(1);
  }
}

const style = (locator: Locator, property: string) =>
  locator.evaluate(
    (element, name) => getComputedStyle(element).getPropertyValue(name).trim(),
    property,
  );

const ROW = '[data-row-id="mock-erp-item-151"]';

test("binh-thuong: header, group header, 52 px rows and 44 px targets", async ({
  page,
}) => {
  await openTable(page);
  const table = page.getByRole("table");

  // Header: 44 px, the nine columns of the artboard (48 312 124 116 200 128 112 128 140), 13 px / 600.
  const headers = table.getByRole("columnheader");
  const widths = [
    [0, 48],
    [48, 312],
    [360, 124],
    [484, 116],
    [600, 200],
    [800, 128],
    [928, 112],
    [1040, 128],
    [1168, 140],
  ] as const;
  for (const [index, [x, width]] of widths.entries()) {
    const cell = headers.nth(index);
    expectRect(
      await within(table, cell),
      { x, y: 0, width, height: 44 },
      `header ${index}`,
    );
    expect(await style(cell, "font-size"), `header ${index} size`).toBe("13px");
    expect(await style(cell, "font-weight"), `header ${index} weight`).toBe(
      "600",
    );
  }

  // Group header: 44 px under the header.
  const group = page.getByRole("rowgroup").first().locator("> div").first();
  expectRect(
    await within(table, group),
    { x: 0, y: 44, height: 44 },
    "group header",
  );

  // Rows: 52 px, one after the other.
  const first = page.locator(ROW);
  expectRect(await within(table, first), { y: 88, height: 52 }, "first row");
  const second = page.locator("[data-row-id]").nth(1);
  expectRect(await within(table, second), { y: 140, height: 52 }, "next row");
  const cells = first.getByRole("cell");
  for (const [index, [x, width]] of widths.entries())
    expectRect(
      await within(table, cells.nth(index)),
      { x, y: 88, width, height: 52 },
      `cell ${index}`,
    );

  // The checkbox: a 44 px button with a 22 px box of radius 6.
  const check = first.getByRole("checkbox", { name: "Chọn ERP-151" });
  expectRect(
    await within(table, check),
    { x: 2, y: 91.5, width: 44, height: 44 },
    "checkbox",
  );
  const box = check.locator("> span");
  expectRect(
    await within(table, box),
    { x: 13, y: 102.5, width: 22, height: 22 },
    "checkbox box",
  );
  expect(await style(box, "border-radius")).toBe("6px");

  // Key: 12.5 px / 500, 16 px high.
  const key = first.locator("[data-row-open] > span").first();
  expect((await within(table, key)).height).toBeCloseTo(16, 0);
  expect(await style(key, "font-size")).toBe("12.5px");

  // The status pill: 87.2x28 pill inside a 116x44 target (9 px radius), 13 px / 600.
  const status = first.getByRole("button", {
    name: "Đổi trạng thái của ERP-151",
  });
  expectRect(
    await within(table, status),
    { x: 372, y: 99.5, width: 87.2, height: 28 },
    "status pill",
  );
  expect(await style(status, "font-size")).toBe("13px");
  expect(await style(status, "font-weight")).toBe("600");

  // Due, assignee and priority triggers are 44 px high with 9 px radius.
  for (const [name, want] of [
    [/Đổi hạn của ERP-151/, { x: 488, y: 91.5, width: 108, height: 44 }],
    [/Đổi ưu tiên của ERP-151/, { x: 804, y: 91.5, width: 120, height: 44 }],
  ] as const) {
    const trigger = first.getByRole("button", { name });
    expectRect(await within(table, trigger), want, String(name));
    expect(await style(trigger, "border-radius")).toBe("9px");
  }
  const assignee = first.getByRole("button", {
    name: /Đổi người phụ trách của ERP-151/,
  });
  expectRect(
    await within(table, assignee),
    { y: 91.5, height: 44 },
    "assignee trigger",
  );
  // The avatar is 28 px, 8 px inside the cell.
  expectRect(
    await within(table, assignee.locator("> span").first()),
    { x: 612, y: 99.5, width: 28, height: 28 },
    "avatar",
  );

  // Child progress: a 48x6 track of radius 3 at the cell's left padding.
  const track = first.locator("[data-child-progress] > span").first();
  expectRect(
    await within(table, track),
    { x: 940, y: 110.5, width: 48, height: 6 },
    "progress track",
  );
  expect(await style(track, "border-radius")).toBe("3px");
});

test("sua-o: the due picker is 340 px wide with 44 px presets, days and time", async ({
  page,
}) => {
  await openTable(page);
  await page.getByRole("button", { name: "Đổi hạn của ERP-144" }).click();
  const pop = page.locator("[data-slot=popover-content]");
  await expect(pop).toBeVisible();
  await page.waitForTimeout(400);
  expectRect(
    await within(pop, pop),
    { x: 0, y: 0, width: 340, height: 498 },
    "popover",
  );
  expect(await style(pop, "border-radius")).toBe("14px");
  const picker = pop.locator("[data-slot=table-due-picker]");

  expectRect(
    await within(pop, picker.locator("> p")),
    { x: 9, y: 9, width: 322, height: 33 },
    "title",
  );
  const presets = picker.getByRole("button", {
    name: /^(Hôm nay|Ngày mai|Thứ Hai tới|Không có hạn)/,
  });
  await expect(presets).toHaveCount(4);
  for (const [index, [x, y]] of [
    [9, 42],
    [173, 42],
    [9, 92],
    [173, 92],
  ].entries()) {
    const preset = presets.nth(index);
    expectRect(
      await within(pop, preset),
      { x, y, width: 158, height: 44 },
      `preset ${index}`,
    );
    expect(await style(preset, "border-radius")).toBe("10px");
  }

  // Month header: two 44 px arrows around the month.
  expectRect(
    await within(pop, picker.getByRole("button", { name: "Tháng trước" })),
    { x: 9, y: 142, width: 44, height: 44 },
    "previous month",
  );
  expectRect(
    await within(pop, picker.getByRole("button", { name: "Tháng sau" })),
    { x: 287, y: 142, width: 44, height: 44 },
    "next month",
  );
  expectRect(
    await within(pop, picker.locator("> div").nth(2)),
    { x: 9, y: 186, width: 322, height: 26 },
    "weekdays",
  );

  // October 2026 draws five weeks of 44 px days; the 12th (today, the chosen day) is the filled one.
  const days = picker.locator("[role=group] > button");
  await expect(days).toHaveCount(35);
  expectRect(
    await within(pop, days.first()),
    { x: 10, y: 212, width: 44, height: 44 },
    "first day",
  );
  expectRect(
    await within(pop, days.last()),
    { x: 286, y: 388, width: 44, height: 44 },
    "last day",
  );
  const chosen = picker.locator("[role=group] > button[aria-pressed=true]");
  expectRect(
    await within(pop, chosen),
    { x: 10, y: 300, width: 44, height: 44 },
    "chosen day",
  );
  expect(await style(chosen, "border-radius")).not.toBe("0px");

  // Time row: 51 px under the days; the control is 44 px high and ends at the row's right edge (331).
  expectRect(
    await within(pop, picker.locator("> div").nth(4)),
    { x: 9, y: 438, width: 322, height: 51 },
    "time row",
  );
  const select = picker.getByRole("combobox");
  const time = await within(pop, select);
  expect(Math.abs(time.height - 44)).toBeLessThanOrEqual(1);
  expect(Math.abs(time.x + time.width - 331)).toBeLessThanOrEqual(1);
  expect(await style(select, "border-radius")).toBe("10px");
});

test("chon-nhieu: two rows checked, a 52 px bar of 44 px buttons 22 px above the foot", async ({
  page,
}) => {
  await openTable(page);
  const table = page.getByRole("table");
  await page.getByRole("checkbox", { name: "Chọn ERP-142" }).click();
  await page.getByRole("checkbox", { name: "Chọn ERP-146" }).click();
  const bar = page.getByRole("toolbar", { name: /đã chọn|Thao tác/ });
  await expect(bar).toBeVisible();
  await page.waitForTimeout(500);

  for (const key of ["ERP-142", "ERP-146"]) {
    const box = page
      .getByRole("checkbox", { name: `Chọn ${key}` })
      .locator("> span");
    const rect = await within(table, box);
    expectRect(rect, { x: 13, width: 22, height: 22 }, `${key} box`);
    expect(await style(box, "border-radius")).toBe("6px");
    expect(await style(box, "background-color")).not.toBe("rgba(0, 0, 0, 0)");
  }

  const surface = page.locator("[data-slot=main-table-surface]");
  const frame = (await surface.boundingBox())!;
  const barBox = (await bar.boundingBox())!;
  expect(Math.abs(barBox.height - 52), "bar height").toBeLessThanOrEqual(1);
  expect(await style(bar, "border-radius")).toBe("14px");
  expect(
    Math.abs(frame.y + frame.height - (barBox.y + barBox.height) - 22),
    "gap under the bar",
  ).toBeLessThanOrEqual(1);
  // Centred over the table region.
  expect(
    Math.abs(barBox.x + barBox.width / 2 - (frame.x + frame.width / 2)),
    "bar centre",
  ).toBeLessThanOrEqual(1);
  const buttons = bar.getByRole("button");
  const count = await buttons.count();
  expect(count).toBeGreaterThanOrEqual(4);
  for (let index = 0; index < count; index += 1) {
    const rect = (await buttons.nth(index).boundingBox())!;
    expect(rect.height, `bar button ${index}`).toBeGreaterThanOrEqual(43.5);
    expect(
      Math.abs(rect.y - barBox.y - 4),
      `bar button ${index} inset`,
    ).toBeLessThanOrEqual(1);
    expect(await style(buttons.nth(index), "border-radius")).toBe("10px");
  }
});

test("khong-ket-qua: a 56 px icon tile, a 17 px title and a 44 px Bỏ lọc, centred", async ({
  page,
}) => {
  // Priority Thấp and overdue match no open item.
  await openTable(page, "&filter=p.low~o");
  const note = page
    .getByRole("status")
    .filter({ hasText: "Không có việc khớp bộ lọc" });
  await expect(note).toBeVisible();
  const tile = note.locator("> span").first();
  const tileBox = (await tile.boundingBox())!;
  expect(Math.abs(tileBox.width - 56)).toBeLessThanOrEqual(1);
  expect(Math.abs(tileBox.height - 56)).toBeLessThanOrEqual(1);
  expect(await style(tile, "border-radius")).toBe("16px");
  const title = note.locator("b");
  expect(await style(title, "font-size")).toBe("17px");
  expect(await style(title, "font-weight")).toBe("650");
  const clear = note.getByRole("button", { name: "Bỏ lọc" });
  const clearBox = (await clear.boundingBox())!;
  expect(Math.abs(clearBox.height - 44)).toBeLessThanOrEqual(1);
  expect(await style(clear, "border-radius")).toBe("10px");
  expect(await style(clear, "font-size")).toBe("14.5px");
  // Icon, title and button share one axis.
  const axis = tileBox.x + tileBox.width / 2;
  const titleBox = (await title.boundingBox())!;
  expect(Math.abs(titleBox.x + titleBox.width / 2 - axis)).toBeLessThanOrEqual(
    1,
  );
  expect(Math.abs(clearBox.x + clearBox.width / 2 - axis)).toBeLessThanOrEqual(
    1,
  );
  // The note is in view: its axis is the middle of the scroll area, not of the 3000 px grid.
  const scroll = (await page.locator("[data-table-scroll]").boundingBox())!;
  expect(
    Math.abs(axis - (scroll.x + scroll.width / 2)),
    "note axis vs the scroll area",
  ).toBeLessThanOrEqual(2);
  // The icon sits above the title, the button below the note's text.
  expect(titleBox.y).toBeGreaterThan(tileBox.y + tileBox.height);
  expect(clearBox.y).toBeGreaterThan(titleBox.y + titleBox.height);
});

type Boxes = [string, number, number, number, number][];

async function surfaceBoxes(
  page: Page,
  dark: boolean,
  state: "default" | "due" | "selected" | "no-results",
): Promise<Boxes> {
  await openTable(page, state === "no-results" ? "&filter=p.low~o" : "", dark);
  if (state === "due") {
    await page.getByRole("button", { name: "Đổi hạn của ERP-144" }).click();
    await expect(page.locator("[data-slot=popover-content]")).toBeVisible();
  }
  if (state === "selected") {
    await page.getByRole("checkbox", { name: "Chọn ERP-142" }).click();
    await page.getByRole("checkbox", { name: "Chọn ERP-146" }).click();
    await expect(page.getByRole("toolbar", { name: /đã chọn/ })).toBeVisible();
  }
  await page.waitForTimeout(700);
  return page.evaluate(() =>
    [
      ...document.querySelectorAll(
        "[data-slot=main-table-surface] *, [data-slot=popover-content] *, [data-slot=popover-content]",
      ),
    ]
      .filter((element) => element.getClientRects().length > 0)
      .map((element): [string, number, number, number, number] => {
        const r = element.getBoundingClientRect();
        return [element.tagName, r.x, r.y, r.width, r.height];
      }),
  );
}

for (const state of ["default", "due", "selected", "no-results"] as const) {
  test(`dark keeps every box of the tablet table where light has it: ${state}`, async ({
    browser,
  }) => {
    const page = async () =>
      (await browser.newContext({ timezoneId: "Asia/Ho_Chi_Minh" })).newPage();
    const light = await surfaceBoxes(await page(), false, state);
    const dark = await surfaceBoxes(await page(), true, state);
    expect(dark.length, "elements").toBe(light.length);
    expect(light.length).toBeGreaterThan(30);
    light.forEach((box, index) => {
      const twin = dark[index]!;
      expect(twin[0], `element ${index}`).toBe(box[0]);
      for (const k of [1, 2, 3, 4] as const)
        expect(
          Math.abs((twin[k] as number) - (box[k] as number)),
          `${box[0]} #${index} box ${k}`,
        ).toBeLessThanOrEqual(0.5);
    });
  });
}
