import { expect, test, type Locator, type Page } from "@playwright/test";

/**
 * The stale-save frames of the form builder in the design canvas artboard WM-FormConflict that need a second
 * person's change: "Xem bản mới" (the difference table with an outcome chip per row and the save bar under
 * it), "Một người khác" (the saver's name withheld) and the 1024 px notice. Numbers were read from the artboard
 * rendered with docs/design/mockups/2026-10/render.py; tolerance is 1 px. The mock lets a browser check have
 * another person save the form first (`__notrelixMockEditFormFirst`), so the real stale-version refusal answers.
 *
 * Positions are relative to the table (or the notice): the artboard frame holds the card alone while the app
 * adds its shell and the open question's side panel, so absolute positions and the table's width differ by
 * layout. The 150 / 1fr / 1fr / 140 px column template, 12 px gaps, 15 px side padding, 38.4 px header and
 * 56 px rows are measured. Button label widths run about 5 percent wider than drawn (the app's Inter against
 * the artboard's render), so widths are compared within 8 px.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const latency = process.env.VITE_MOCK_LATENCY ?? "instant";
const FORM =
  "/workspaces/mock-workspace-primary/boards/mock-board-erp/forms/mock-form-it-support/edit";
const NOW = new Date("2026-10-12T10:40:00+07:00");

test.skip(
  persona !== "owner" ||
    scenario !== "default" ||
    faults !== "" ||
    latency !== "instant",
  "Needs the default owner world without faults or latency",
);

async function open(page: Page, dark: boolean, width: number) {
  await page.setViewportSize({ width, height: 900 });
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
  await page.goto(FORM, { waitUntil: "domcontentloaded" });
}

/** Another person saves first; the rename of question 3 (or, with no panel open, of the description) is refused. */
async function stale(page: Page, panel = true) {
  await page.getByRole("textbox", { name: /^Mô tả/ }).waitFor();
  if (panel) {
    await page.getByText("Mức độ ưu tiên").first().click();
    await page.waitForTimeout(300);
  }
  await page.evaluate(() =>
    (
      window as unknown as {
        __notrelixMockEditFormFirst: (id: string) => boolean;
      }
    ).__notrelixMockEditFormFirst("mock-form-it-support"),
  );
  const label = panel
    ? page.getByRole("textbox", { name: /^Nhãn câu hỏi/ })
    : page.getByRole("textbox", { name: /^Mô tả/ });
  await label.fill(panel ? "Mức độ khẩn cấp" : "Mô tả mới của người soạn");
  await label.blur();
  const notice = page.getByRole("alert").filter({ hasText: "vừa lưu" });
  await expect(notice).toBeVisible();
  await page.waitForTimeout(250);
  return notice;
}

async function box(locator: Locator) {
  return (await locator.boundingBox())!;
}

function near(actual: number, want: number, label: string, tol = 1) {
  expect
    .soft(Math.abs(actual - want), `${label}: ${actual.toFixed(1)} vs ${want}`)
    .toBeLessThanOrEqual(tol);
}

for (const dark of [false, true]) {
  const mode = dark ? "dark" : "light";

  test(`difference table and save bar, ${mode}`, async ({ page }) => {
    await open(page, dark, 1440);
    const notice = await stale(page);
    await notice.getByRole("button", { name: "Xem bản mới" }).click();
    const table = page.getByRole("table", {
      name: "Khác biệt giữa bản của bạn và bản mới nhất",
    });
    await expect(table).toBeVisible();
    await page.waitForTimeout(250);
    const t = await box(table);
    const header = await box(table.locator("> div").first());
    const rows = table.locator("> div").nth(1);
    near(header.height, 38.4, "header height");
    near(header.x - t.x, 1, "header inset");
    const cells = table.locator("> div").first().locator("> span");
    const c = [0, 1, 2, 3].map((i) => cells.nth(i));
    const [c0, c1, c2, c3] = await Promise.all(c.map(box));
    near(c0.x - t.x, 15, "col 1 x");
    near(c0.width, 150, "col 1 width");
    near(c1.x - (c0.x + c0.width), 12, "gap 1");
    near(c2.x - (c1.x + c1.width), 12, "gap 2");
    near(c3.x - (c2.x + c2.width), 12, "gap 3");
    near(c3.width, 140, "col 4 width");
    near(t.x + t.width - (c3.x + c3.width), 15, "right padding");
    near(c1.width, c2.width, "columns 2 and 3 equal");
    const body = table.locator("> div");
    expect(await body.count()).toBe(4);
    for (let i = 1; i < 4; i++) {
      const r = await box(body.nth(i));
      near(r.height, 56, `row ${i} height`);
      near(r.y - (t.y + 1 + 38.4 + (i - 1) * 56), 0, `row ${i} y`);
    }
    near(t.height, 208.4, "table height");
    void rows;
    const chips = table.locator("> div").nth(1).locator("> span").last();
    near((await box(chips.locator("> span").first())).height, 22, "chip h");
    expect(
      await table.evaluate((e) => getComputedStyle(e).borderTopLeftRadius),
    ).toBe("12px");
    // Outcome chips: question 3 keeps mine, the new question and the order take the new version.
    await expect(table.getByText("Giữ của bạn", { exact: true })).toHaveCount(
      1,
    );
    await expect(table.getByText("Lấy bản mới", { exact: true })).toHaveCount(
      2,
    );
    // The save bar sits 12 px under the table; buttons are 32 high, 8 apart, flush right.
    const keep = page.getByRole("button", { name: "Giữ của tôi và lưu" });
    const take = page.getByRole("button", { name: "Dùng bản mới" }).last();
    const kb = await box(keep);
    const tb = await box(take);
    near(kb.height, 32, "keep height");
    near(tb.height, 32, "take height");
    near(kb.x - (tb.x + tb.width), 8, "bar gap");
    near(t.x + t.width - (kb.x + kb.width), 0, "bar flush right");
    near(kb.width, 136.5, "keep width", 8);
    near(tb.width, 113.5, "take width", 8);
    // The bar's hint wraps to two lines in the app's narrower column, so its height differs; its top and
    // the buttons' centring are compared.
    const bar = await box(keep.locator(".."));
    near(bar.y - (t.y + t.height), 12, "bar under table");
    near(
      kb.y + kb.height / 2,
      bar.y + bar.height / 2,
      "buttons centred in bar",
    );
  });

  test(`notice at 1024 px, ${mode}`, async ({ page }) => {
    await open(page, dark, 1024);
    const notice = await stale(page, false);
    const n = await box(notice);
    const title = await box(notice.locator("b").first());
    near(title.x - n.x, 40, "text x");
    near(title.y - n.y, 10, "title y");
    for (const [name, w] of [
      ["Xem bản mới", 101.6],
      ["Dùng bản mới", 106.2],
      ["Giữ của tôi", 89.1],
    ] as const) {
      const b = await box(notice.getByRole("button", { name }));
      near(b.height, 28, `${name} height`);
      near(b.width, w, `${name} width`, 8);
    }
    // The shell's sidebar leaves the notice about 600 px, so the three buttons may wrap under the text; the
    // artboard's 940 px card holds them on the title's line. Their 6 px gap is the same.
    const [a, b, c] = await Promise.all(
      ["Xem bản mới", "Dùng bản mới", "Giữ của tôi"].map((name) =>
        box(notice.getByRole("button", { name })),
      ),
    );
    near(b.x - (a.x + a.width), 6, "gap 1");
    near(c.x - (b.x + b.width), 6, "gap 2");
    expect(n.x + n.width).toBeGreaterThanOrEqual(c.x + c.width);
  });
}
