import { expect, test, type Locator, type Page } from "@playwright/test";

/**
 * The in-cell editors of the custom field types against the owner canvas board `WM-Field-Editors` (1440 wide,
 * rendered with the canvas `render.py`, `mode=sang` and `mode=dem`), measured as boxes. The artboard draws each
 * editor on its own one-column stage: a 36 px header and 40 px cells with a 1 px border and 10 px padding. The
 * app's cell is a grid cell of the Table (no side border), so a box inside a cell is compared at the artboard's
 * x minus the 1 px border; popovers hang from the cell's own left edge and 2 px under its bottom edge. Every
 * number is read from the render; tolerance is 1 px, font sizes, weights and radii are equal.
 *
 * Drawn and measured: Text (typing), Long text (popover), Number (typing, invalid with its message and unit),
 * Checkbox (unchecked, checked, read-only), Select, Multi select, Person and Date popovers, Link (typing,
 * invalid, display with its open icon). The artboard lists 4 options (5 in the multi list); the mock board
 * lists 6, so list heights are the artboard's formula `38 n + 97` and rows are compared one by one.
 * Not measured: the saving row (`.td.dim`) and the long text counter (a field with a length limit; none exists
 * in 0.1.0).
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const NOW = new Date("2026-10-12T10:40:00+07:00");
const TOL = 1;

interface Rect {
  readonly x: number;
  readonly y: number;
  readonly width: number;
  readonly height: number;
}

async function openBoard(page: Page, dark: boolean) {
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
  await page.goto(`${BOARD}?view=table`);
  await expect(page.locator("[data-row-id]").first()).toBeVisible();
}

/** The first row's cell of a column, scrolled into view. */
async function cellOf(page: Page, column: string, row?: Locator) {
  const cell = (row ?? page.locator("[data-row-id]").first()).locator(
    "[role=cell]",
  );
  const index = await page.evaluate((name) => {
    const heads = [...document.querySelectorAll("[role=columnheader]")];
    return heads.findIndex((head) => head.textContent?.includes(name));
  }, column);
  // Header 0 is the checkbox column; the row's first cell is the checkbox too.
  const target = cell.nth(index);
  await target.evaluate((el) =>
    el.scrollIntoView({ inline: "center", block: "nearest" }),
  );
  await page.waitForTimeout(150);
  return target;
}

async function rel(locator: Locator, origin: Locator): Promise<Rect> {
  const [e, o] = await Promise.all([
    locator.boundingBox(),
    origin.boundingBox(),
  ]);
  if (!e || !o) throw new Error("element is not laid out");
  return { x: e.x - o.x, y: e.y - o.y, width: e.width, height: e.height };
}

async function style(locator: Locator) {
  return locator.evaluate((element) => {
    const s = getComputedStyle(element);
    return { fs: s.fontSize, fw: s.fontWeight, r: s.borderRadius };
  });
}

function box(actual: Rect, want: Partial<Rect>, label: string) {
  for (const key of ["x", "y", "width", "height"] as const) {
    const value = want[key];
    if (value === undefined) continue;
    expect(
      Math.abs(actual[key] - value),
      `${label}.${key}: ${actual[key].toFixed(1)} vs ${value}`,
    ).toBeLessThanOrEqual(TOL);
  }
}

/** The popover of the open editor and its origin: boxes inside are read from its top-left. */
async function popover(page: Page) {
  const pop = page.getByRole("dialog").last();
  await expect(pop).toBeVisible();
  // The popover zooms in for 100 ms: boxes are read once it has settled.
  await page.waitForTimeout(400);
  return pop;
}

/** Rows of an option list (`n` rows): row k sits at 9 + 38 k (+ 39 with a search box), 32 px high, 7 px radius. */
async function rows(pop: Locator, from: number, count: number, width: number) {
  const options = pop.getByRole("option");
  await expect(options).toHaveCount(count);
  for (let k = 0; k < count; k += 1) {
    const option = options.nth(k);
    box(
      await rel(option, pop),
      { x: 9, y: from + 38 * k, width, height: 32 },
      `option ${k}`,
    );
    expect((await style(option)).r).toBe("7px");
  }
}

test.describe("Custom field editors (WM-Field-Editors), 1440 px", () => {
  test.skip(
    persona !== "owner" || scenario !== "default" || faults !== "",
    "Needs the default owner world without faults",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  for (const dark of [false, true]) {
    const theme = dark ? "dem" : "sang";

    test(`Text and Number in place, Link, invalid draft and its message (${theme})`, async ({
      page,
    }) => {
      await openBoard(page, dark);
      // Text: a 28 px input with a 6 px radius at the cell's padding, 13 px type.
      const text = await cellOf(page, "Khách hàng");
      await text.getByRole("button").click();
      const input = text.getByRole("textbox");
      const t = await rel(input, text);
      const cell = (await text.boundingBox())!;
      box(
        t,
        { x: 10, y: 5.5, width: cell.width - 20, height: 28 },
        "text input",
      );
      expect(await style(input)).toMatchObject({ fs: "13px", r: "6px" });
      await page.keyboard.press("Escape");

      // Number: right aligned, the unit after the input 7 px away; an invalid draft keeps the cell open.
      const number = await cellOf(page, "Ước lượng");
      await number.getByRole("button").click();
      const n = number.getByRole("textbox");
      const unit = number.getByText("giờ");
      const nb = (await number.boundingBox())!;
      box(await rel(n, number), { x: 10, y: 5.5, height: 28 }, "number input");
      expect(await n.evaluate((el) => getComputedStyle(el).textAlign)).toBe(
        "right",
      );
      box(
        await rel(unit, number),
        { x: nb.width - 10 - 19.5, y: 10.4, height: 18.1 },
        "unit",
      );
      const gap =
        (await rel(unit, number)).x -
        ((await rel(n, number)).x + (await rel(n, number)).width);
      expect(Math.abs(gap - 7)).toBeLessThanOrEqual(TOL);
      await n.fill("13a");
      await n.press("Enter");
      const message = page
        .getByRole("alert")
        .filter({ hasText: "Nhập một số" });
      await expect(message).toBeVisible();
      await page.waitForTimeout(400);
      // The message: 12 px, 600, 16 px icon, 17.4 px line, 6 px under the cell, 10 px in.
      box(
        await rel(message, number),
        { x: 10, y: 46, height: 17.4 },
        "number message",
      );
      expect(await style(message)).toMatchObject({ fs: "12px", fw: "600" });
      box(
        await rel(message.locator("svg"), number),
        { y: 46.6, width: 16, height: 16 },
        "message icon",
      );
      expect(
        await n.evaluate((el) => getComputedStyle(el).borderTopColor),
      ).not.toBe(
        await n.evaluate(
          (el) => getComputedStyle(el.parentElement!).borderTopColor,
        ),
      );
      await page.keyboard.press("Escape");

      // Link: the display keeps the 16 px open icon at the right edge; typing is the same input.
      const withLink = page
        .locator("[data-row-id]")
        .filter({
          has: page.getByRole("link", { name: "Mở liên kết trong thẻ mới" }),
        })
        .first();
      const rowId = await withLink.getAttribute("data-row-id");
      const link = await cellOf(
        page,
        "Liên kết",
        page.locator(`[data-row-id="${rowId}"]`),
      );
      const icon = link.getByRole("link");
      const lb = (await link.boundingBox())!;
      box(
        await rel(icon, link),
        { x: lb.width - 10 - 16, y: 11.5, width: 16, height: 16 },
        "open icon",
      );
      await link.getByRole("button").click();
      const l = link.getByRole("textbox");
      box(
        await rel(l, link),
        { x: 10, y: 5.5, width: lb.width - 20, height: 28 },
        "link input",
      );
      await l.fill("javascript:alert(1)");
      await l.press("Enter");
      const bad = page.getByRole("alert").filter({ hasText: "http" });
      await expect(bad).toBeVisible();
      await page.waitForTimeout(400);
      box(await rel(bad, link), { x: 10, y: 46, height: 17.4 }, "link message");
    });

    test(`Long text popover (${theme})`, async ({ page }) => {
      await openBoard(page, dark);
      const cell = await cellOf(page, "Ghi chú");
      await cell.getByRole("button").click();
      const pop = await popover(page);
      box(
        await rel(pop, cell),
        { x: 0, y: 42, width: 340, height: 160 },
        "popover",
      );
      expect((await style(pop)).r).toBe("10px");
      const area = pop.getByRole("textbox");
      box(
        await rel(area, pop),
        { x: 9, y: 9, width: 322, height: 110 },
        "textarea",
      );
      expect((await style(area)).r).toBe("7px");
      const cancel = pop.getByRole("button", { name: "Huỷ" });
      const save = pop.getByRole("button", { name: "Lưu" });
      box(
        await rel(cancel, pop),
        { x: 244.8, y: 125, width: 40.5, height: 26 },
        "cancel",
      );
      box(
        await rel(save, pop),
        { x: 291.3, y: 125, width: 39.7, height: 26 },
        "save",
      );
      expect((await style(save)).r).toBe("7px");
    });

    test(`Checkbox: unchecked, checked (${theme})`, async ({ page }) => {
      await openBoard(page, dark);
      const cell = await cellOf(page, "Sẵn sàng bàn giao");
      const checkbox = cell.getByRole("checkbox");
      box(
        await rel(checkbox, cell),
        { x: 10, y: 11.5, width: 16, height: 16 },
        "box",
      );
      expect((await style(checkbox)).r).toBe("4px");
      const caption = cell.locator("span.truncate");
      const c = await rel(caption, cell);
      near(c.x, 34, "caption x");
      // The cell shows "Chưa" or "Đã …" beside the box.
      await expect(caption).toHaveText(/^(Chưa|Đã )/);
    });

    test(`Select popover (${theme})`, async ({ page }) => {
      await openBoard(page, dark);
      const cell = await cellOf(page, "Phân hệ ERP");
      await cell.getByRole("button").click();
      const pop = await popover(page);
      const n = 6;
      box(
        await rel(pop, cell),
        { x: 0, y: 42, width: 250, height: 38 * n + 97 },
        "popover",
      );
      const search = pop.getByRole("searchbox");
      box(
        await rel(search.locator(".."), pop),
        { x: 9, y: 9, width: 232, height: 30 },
        "search",
      );
      expect((await style(search.locator(".."))).r).toBe("6px");
      await rows(pop, 45, n, 232);
      box(
        await rel(pop.getByRole("separator"), pop),
        { x: 1, y: 38 * n + 47, width: 248, height: 1 },
        "rule",
      );
      const clear = pop.getByRole("button", { name: "Bỏ chọn" });
      box(
        await rel(clear, pop),
        { x: 9, y: 38 * n + 56, width: 232, height: 32 },
        "clear row",
      );
      const chip = pop.getByRole("option").first().locator("span.inline-flex");
      box(await rel(chip, pop), { x: 17, y: 50, height: 22 }, "first chip");
      expect((await style(chip)).r).toBe("6px");
    });

    test(`Multi select popover (${theme})`, async ({ page }) => {
      await openBoard(page, dark);
      const cell = await cellOf(page, "Phân hệ liên quan");
      await cell.getByRole("button").click();
      const pop = await popover(page);
      const n = 6;
      box(
        await rel(pop, cell),
        { x: 0, y: 42, width: 260, height: 38 * n + 55 },
        "popover",
      );
      await rows(pop, 9, n, 242);
      const tick = pop
        .getByRole("option")
        .first()
        .locator("span[aria-hidden=true]")
        .first();
      box(
        await rel(tick, pop.getByRole("option").first()),
        { x: 8, y: 8, width: 16, height: 16 },
        "tick box",
      );
      const done = pop.getByRole("button", { name: "Xong" });
      box(
        await rel(done, pop),
        { x: 201.8, y: 38 * n + 20, width: 49.2, height: 26 },
        "done",
      );
    });

    test(`Person popover (${theme})`, async ({ page }) => {
      await openBoard(page, dark);
      const cell = await cellOf(page, "Người nghiệm thu");
      await cell.getByRole("button").click();
      const pop = await popover(page);
      const count = await pop.getByRole("option").count();
      box(
        await rel(pop, cell),
        { x: 0, y: 42, width: 270, height: 38 * count + 97 },
        "popover",
      );
      await rows(pop, 45, count, 252);
      const first = pop.getByRole("option").first();
      const mark = first.locator("span[aria-hidden=true]").nth(1);
      box(
        await rel(mark, first),
        { x: 32, y: 5, width: 22, height: 22 },
        "avatar",
      );
      expect((await style(mark)).fs).toBe("9px");
      const clear = pop.getByRole("button", { name: "Bỏ hết" });
      box(
        await rel(clear, pop),
        { x: 9, y: 38 * count + 56, width: 252, height: 32 },
        "clear row",
      );
    });

    test(`Date popover (${theme})`, async ({ page }) => {
      await openBoard(page, dark);
      const cell = await cellOf(page, "Ngày go-live");
      await cell.getByRole("button").click();
      const pop = await popover(page);
      // October 2026 starts on a Thursday: five weeks.
      box(
        await rel(pop, cell),
        { x: 0, y: 42, width: 270, height: 283 },
        "popover",
      );
      box(
        await rel(pop.getByRole("button", { name: "Tháng trước" }), pop),
        { x: 9, y: 9, width: 34, height: 26 },
        "previous",
      );
      box(
        await rel(pop.getByRole("button", { name: "Tháng sau" }), pop),
        { x: 227, y: 9, width: 34, height: 26 },
        "next",
      );
      const month = pop.getByText("Tháng 10, 2026");
      expect(await style(month)).toMatchObject({ fs: "13px", fw: "700" });
      box(
        await rel(pop.getByRole("grid"), pop),
        { x: 9, y: 41, width: 252, height: 190 },
        "grid",
      );
      const day = (n: string, offset = 0) =>
        pop
          .getByRole("gridcell", { name: new RegExp(`^${n} Tháng 10 2026`) })
          .nth(offset);
      // 1 Oct: Thursday, column 3, first week row (below the weekday row).
      box(
        await rel(day("1"), pop),
        { x: 17 + 34 * 3, y: 41 + 32, width: 32, height: 30 },
        "1 Oct",
      );
      box(
        await rel(day("15"), pop),
        { x: 17 + 34 * 3, y: 41 + 32 * 3, width: 32, height: 30 },
        "15 Oct",
      );
      expect((await style(day("15"))).r).toBe("7px");
      const dow = pop.getByRole("columnheader").first();
      box(
        await rel(dow, pop),
        { x: 17, y: 41, width: 32, height: 30 },
        "weekday",
      );
      box(
        await rel(pop.getByRole("separator"), pop),
        { x: 1, y: 239, width: 268, height: 1 },
        "rule",
      );
      box(
        await rel(pop.getByRole("button", { name: "Xoá ngày" }), pop),
        { x: 9, y: 248, width: 74, height: 26 },
        "clear",
      );
      box(
        await rel(pop.getByRole("button", { name: "Hôm nay" }), pop),
        { x: 191.5, y: 248, width: 69.5, height: 26 },
        "today",
      );
    });
  }
});

function near(actual: number, want: number, label: string) {
  expect(
    Math.abs(actual - want),
    `${label}: ${actual.toFixed(1)} vs ${want}`,
  ).toBeLessThanOrEqual(TOL);
}
