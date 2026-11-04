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
 * The preview row ("Xem thử với ERP-145:", the 16 px info mark, the 14 px bold value after the text, 36.3 px
 * high at y 266.2, 518 wide) and the rollup dialog (398.4 px high without its preview row, the 34 px name input at y 93.4, the 32 px
 * calculation buttons at y 160.8, the 34 px field select, the info note) are measured too; the mock answers the
 * preview with a sample value. Not built: the "(Điểm = 13)" detail after the key and the field chips limited to
 * what the drawn list shows.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const latency = process.env.VITE_MOCK_LATENCY ?? "";
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
    const save = await at(dlg.getByRole("button", { name: "Lưu trường" }), dlg);
    near(cancel.h, 32, "cancel height");
    near(cancel.w, 50.4, "cancel width");
    near(save.w, 95.8, "save width");
    near(save.x - (cancel.x + cancel.w), 8, "gap between the buttons");
    near(
      d.width - 1 - (save.x + save.w),
      21 - 1 + 1 - 1,
      "save from the right edge",
    );
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
      near(
        host!.x + host!.width - 10 - (c!.x + c!.width),
        0,
        `${state} right padding`,
      );
      expect(await cell.locator("text=ƒx").count()).toBe(0);
      expect(await cell.evaluate((el) => getComputedStyle(el).fontSize)).toBe(
        "13.5px",
      );
    }
  });

  test(`the table's error and stale cells, ${mode}`, async ({ page }) => {
    // WM-Field-Computed "Cột tính trong View Bảng": a 40 px cell with 10 px padding; the error is a 16 px mark and
    // "Lỗi" in 12 px / 600 5 px apart (39.9 x 17.4); the stale value keeps its right-aligned text and is followed
    // 7 px later by an 18 px ring. The cell is dimmed by italic, not opacity (the text keeps 4.5:1).
    await page.setViewportSize({ width: 1440, height: 900 });
    await theme(page, dark);
    await page.goto(`${BOARD}/settings?from=table&section=fields`);
    await page.getByText("Điểm gấp đôi", { exact: true }).first().click();
    await page.getByRole("button", { name: /Sửa công thức/ }).click();
    const dlg = page.getByRole("dialog");
    await dlg.getByRole("textbox", { name: "Công thức" }).fill("[Điểm] / 0");
    await dlg.getByRole("button", { name: "Lưu trường" }).click();
    await expect(dlg).toBeHidden({ timeout: 10_000 });
    await page.getByText("Về board", { exact: true }).click();
    const error = page.locator('[data-computed-state="error"]').first();
    await expect(error).toBeVisible({ timeout: 10_000 });
    const host = error.locator("xpath=..");
    const [c, h] = await Promise.all([error.boundingBox(), host.boundingBox()]);
    near(c!.width, 39.9, "error width");
    near(c!.height, 17.4, "error height");
    near(h!.x + h!.width - 10 - (c!.x + c!.width), 0, "error right padding");
    const mark = error.locator("svg");
    near((await mark.boundingBox())!.width, 16, "error mark");
    expect(await error.evaluate((el) => getComputedStyle(el).fontSize)).toBe(
      "12px",
    );
    expect(await error.evaluate((el) => getComputedStyle(el).fontWeight)).toBe(
      "600",
    );
    // The tokens are oklch; the canvas turns the colour into the sRGB the artboard states.
    const rgb = await error.evaluate((el) => {
      const ctx = document.createElement("canvas").getContext("2d")!;
      ctx.fillStyle = getComputedStyle(el).color;
      ctx.fillRect(0, 0, 1, 1);
      return [...ctx.getImageData(0, 0, 1, 1).data].slice(0, 3);
    });
    const want = dark ? [255, 107, 129] : [196, 35, 64];
    rgb.forEach((channel, n) =>
      expect(Math.abs(channel - want[n]!)).toBeLessThanOrEqual(3),
    );
  });

  test(`the table's stale cell, ${mode}`, async ({ page }) => {
    test.skip(
      latency !== "slow",
      "Needs VITE_MOCK_LATENCY=slow to hold the state",
    );
    await page.setViewportSize({ width: 1440, height: 900 });
    await theme(page, dark);
    await page.goto(`${BOARD}?view=table`);
    await page.waitForTimeout(900);
    const row = page
      .locator('[data-computed-state="value"]')
      .first()
      .locator("xpath=ancestor::*[@data-row-id][1]");
    await row.getByRole("button", { name: /^Sửa Điểm/ }).click();
    await page.getByRole("textbox").last().fill("7");
    await page.keyboard.press("Enter");
    const stale = page.locator('[data-computed-state="stale"]').first();
    await expect(stale).toBeVisible({ timeout: 5_000 });
    const ring = stale.locator("svg");
    const [v, r, h] = await Promise.all([
      stale.locator("span").first().boundingBox(),
      ring.boundingBox(),
      stale.locator("xpath=..").boundingBox(),
    ]);
    near(r!.width, 18, "ring size");
    near(r!.x - (v!.x + v!.width), 7, "gap to the ring");
    near(h!.x + h!.width - 10 - (r!.x + r!.width), 0, "ring right padding");
    expect(await stale.evaluate((el) => getComputedStyle(el).fontStyle)).toBe(
      "italic",
    );
  });

  test(`the formula preview row, ${mode}`, async ({ page }) => {
    await page.setViewportSize({ width: 1440, height: 900 });
    await theme(page, dark);
    await page.goto(`${BOARD}/settings?from=table&section=fields`);
    await page.getByText("Điểm gấp đôi", { exact: true }).first().click();
    await page.getByRole("button", { name: /Sửa công thức/ }).click();
    const dlg = page.getByRole("dialog");
    const row = dlg.locator("[data-preview-state]");
    await expect(row).toHaveText(/Xem thử với .+:/);
    await expect(row).not.toHaveAttribute("data-preview-state", "checking");
    await page.waitForTimeout(700);
    const r = await at(row, dlg);
    near(r.x, 21, "preview x");
    near(r.y, 266.2, "preview y");
    near(r.w, 518, "preview width");
    near(r.h, 36.3, "preview height");
    const mark = await at(row.locator("svg"), row);
    near(mark.w, 16, "info mark width");
    near(mark.x, 10, "info mark x");
    const value = row.locator("span").last();
    expect(await value.evaluate((el) => getComputedStyle(el).fontSize)).toBe(
      "14px",
    );
    expect(await value.evaluate((el) => getComputedStyle(el).fontWeight)).toBe(
      "700",
    );
    expect(await row.evaluate((el) => getComputedStyle(el).borderRadius)).toBe(
      "8px",
    );
  });

  test(`the rollup dialog, ${mode}`, async ({ page }) => {
    await page.setViewportSize({ width: 1440, height: 900 });
    await theme(page, dark);
    await page.goto(`${BOARD}/settings?from=table&section=fields`);
    await page.getByRole("button", { name: "Thêm trường" }).click();
    await page.getByRole("button", { name: /^Tổng hợp Cộng/ }).click();
    const dlg = page.getByRole("dialog");
    await page.waitForTimeout(700);
    const d = (await dlg.boundingBox())!;
    near(d.width, 560, "dialog width");
    // 446.7 drawn with the rollup preview row (36.3 + the 12 gap), which is not built.
    near(d.height, 398.4, "dialog height");
    near((await at(dlg.getByLabel("Tên trường"), dlg)).y, 93.4, "name y");
    near((await at(dlg.getByLabel("Tên trường"), dlg)).h, 34, "name height");
    const calc = await at(dlg.getByRole("radio", { name: "Đếm" }), dlg);
    near(calc.y, 160.8, "calculation y");
    near(calc.h, 32, "calculation height");
    const footer = await at(
      dlg.getByRole("button", { name: "Lưu trường" }),
      dlg,
    );
    near(footer.h, 32, "save height");
    near(footer.y, 353.4, "save y");
  });
}
