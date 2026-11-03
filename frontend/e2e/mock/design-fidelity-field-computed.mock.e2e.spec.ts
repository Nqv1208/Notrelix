import { expect, test, type Locator, type Page } from "@playwright/test";

/**
 * The calculated fields against the owner canvas board `WM-Field-Computed` (rendered with the canvas `render.py` and
 * measured in Chromium), light and dark, tolerance 1 px, boxes relative to the 560 px dialog (its 1 px border
 * included): the formula dialog (12 px radius, a 55 px header with the 23 x 18 "ƒx" mark and a 15 px / 650 title,
 * 20 px side padding, the 34 px name input at y 93, the 64 px formula box at y 161 with a 7 px radius and 13 px
 * mono text, the "n / 200 phần tử" count 12 px under it and right-aligned, the 12.5 px hint, a 57 px footer with
 * two 32 px buttons, "Huỷ" 50 px and "Lưu trường" 96 px wide, 8 px apart, 21 px from the right edge) and the table's
 * calculated cell (the value, or the dash, right-aligned at the cell's 10 px padding; the ƒx mark and the lock sit in
 * the column header only, as drawn).
 * Not built: "Xem thử với ERP-145" (the preview row needs the server to evaluate a draft formula, no such read
 * exists), the field chips limited to what the drawn list shows, and the rollup dialog's frames (the mock board
 * has no rollup field to open).
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const TOL = 1;

test.skip(
  persona !== "owner" || scenario !== "default" || faults !== "",
  "Needs the default owner world",
);

const near = (a: number, w: number, what: string) =>
  expect(Math.abs(a - w), `${what}: ${a} vs ${w}`).toBeLessThanOrEqual(TOL);

async function theme(page: Page, dark: boolean) {
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
}

async function at(el: Locator, origin: Locator) {
  const [e, o] = await Promise.all([el.boundingBox(), origin.boundingBox()]);
  if (!e || !o) throw new Error("not laid out");
  return { x: e.x - o.x, y: e.y - o.y, w: e.width, h: e.height };
}

for (const dark of [false, true]) {
  const mode = dark ? "dark" : "light";

  test(`the formula dialog, ${mode}`, async ({ page }) => {
    await page.setViewportSize({ width: 1440, height: 900 });
    await theme(page, dark);
    await page.goto(`${BOARD}/settings?from=table&section=fields`);
    await page.getByText("Điểm gấp đôi", { exact: true }).first().click();
    await page.getByRole("button", { name: /Sửa công thức/ }).click();
    const dlg = page.getByRole("dialog");
    await page.waitForTimeout(700);
    const d = (await dlg.boundingBox())!;
    near(d.width, 560, "dialog width");
    expect(await dlg.evaluate((el) => getComputedStyle(el).borderRadius)).toBe(
      "12px",
    );
    const title = dlg.getByRole("heading");
    expect(await title.evaluate((el) => getComputedStyle(el).fontSize)).toBe(
      "15px",
    );
    const mark = dlg.getByText("ƒx", { exact: true });
    const m = await at(mark, dlg);
    near(m.x, 21, "ƒx x");
    near(m.h, 18, "ƒx height");
    near((await at(title, dlg)).x, 52.2, "title x");
    const header = await at(dlg.locator("> div").first(), dlg);
    near(header.h, 55, "header height (the border included)");
    const name = await at(dlg.getByLabel("Tên trường"), dlg);
    near(name.x, 21, "name x");
    near(name.y, 93.4, "name y");
    near(name.h, 34, "name height");
    const code = dlg.getByLabel("Công thức", { exact: true });
    const c = await at(code, dlg);
    near(c.x, 21, "formula x");
    near(c.y, 160.8, "formula y");
    near(c.h, 64, "formula height");
    near(c.w, 518, "formula width");
    expect(await code.evaluate((el) => getComputedStyle(el).borderRadius)).toBe(
      "7px",
    );
    const count = await at(dlg.getByText(/\/ 200 phần tử/), dlg);
    near(count.y, 236.8, "count y");
    near(count.x + count.w, 539, "count right edge");
    const cancel = await at(dlg.getByRole("button", { name: "Huỷ" }), dlg);
    const save = await at(
      dlg.getByRole("button", { name: "Lưu trường" }),
      dlg,
    );
    near(cancel.h, 32, "cancel height");
    near(cancel.w, 50.4, "cancel width");
    near(save.w, 95.8, "save width");
    near(save.x - (cancel.x + cancel.w), 8, "gap between the buttons");
    near(d.width - 1 - (save.x + save.w), 21 - 1 + 1 - 1, "save from the right edge");
  });

  test(`the table's calculated cell, ${mode}`, async ({ page }) => {
    await page.setViewportSize({ width: 1440, height: 900 });
    await theme(page, dark);
    await page.goto(`${BOARD}?view=table`);
    await page.waitForTimeout(900);
    const cells = page.locator("[data-computed-state]");
    await expect(cells.first()).toBeVisible();
    for (const state of ["value", "empty"]) {
      const cell = page.locator(`[data-computed-state="${state}"]`).first();
      const [c, host] = await Promise.all([
        cell.boundingBox(),
        cell.locator("xpath=..").boundingBox(),
      ]);
      near(host!.x + host!.width - 10 - (c!.x + c!.width), 0, `${state} right padding`);
      expect(await cell.locator("text=ƒx").count()).toBe(0);
      expect(await cell.evaluate((el) => getComputedStyle(el).fontSize)).toBe(
        "13.5px",
      );
    }
  });
}
