import { expect, test, type Page } from "@playwright/test";

/**
 * The tiles of the design canvas artboard WM-Forms-Inbox-States on the response inbox page. Numbers were read
 * from the artboard rendered with docs/design/mockups/2026-10/render.py; tolerance is 1 px.
 *
 * Run once per world:
 *  - plain: empty tab, delete confirm, keyboard focus row, long text;
 *  - `VITE_MOCK_LATENCY=slow`: loading;
 *  - `VITE_MOCK_FAULTS="GET /submissions=500"`: load error;
 *  - `VITE_MOCK_FAULTS="POST /spam=500"`: action error;
 *  - `VITE_MOCK_FAULTS="POST /spam=409"`: conflict.
 *
 * Not built: the spam "Hoàn tác" toast (spam, reject and delete have no reopen command on the server yet).
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const latency = process.env.VITE_MOCK_LATENCY ?? "instant";
const RESPONSES =
  "/workspaces/mock-workspace-primary/boards/mock-board-erp/forms/mock-form-it-support/responses";
const NOW = new Date("2026-10-12T10:40:00+07:00");

async function open(page: Page, dark: boolean, path = RESPONSES) {
  await page.setViewportSize({ width: 1440, height: 900 });
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
  await page.goto(path, { waitUntil: "domcontentloaded" });
}

function near(actual: number, want: number, label: string) {
  expect
    .soft(Math.abs(actual - want), `${label}: ${actual.toFixed(1)} vs ${want}`)
    .toBeLessThanOrEqual(1);
}

const plain =
  persona === "owner" &&
  scenario === "default" &&
  faults === "" &&
  latency === "instant";

for (const dark of [false, true]) {
  const mode = dark ? "dark" : "light";

  test(`empty tab tile, ${mode}`, async ({ page }) => {
    test.skip(!plain, "Needs the plain owner world");
    await open(page, dark, RESPONSES.replace("it-support", "purchase"));
    await page.getByRole("tab", { name: /^Từ chối/ }).click();
    const empty = page.locator("[role=tabpanel] > div").first();
    const b = empty.locator("b");
    await expect(b).toBeVisible();
    const e = (await empty.boundingBox())!;
    const bb = (await b.boundingBox())!;
    const sp = (await empty.locator("span").boundingBox())!;
    // Tile "Tab … trống": dashed 10 px radius box, padding 20/16, 13.5 px title, 12.5 px text 2 px under it.
    near(bb.y - e.y, 21, "title y");
    near(bb.x - e.x, 17, "title x");
    near(bb.height, 19.6, "title height");
    near(sp.y - (bb.y + bb.height), 2, "text gap");
    near(e.y + e.height - (sp.y + sp.height), 23.3, "bottom");
    const style = await empty.evaluate((x) => {
      const c = getComputedStyle(x);
      return [c.borderTopStyle, c.borderTopLeftRadius];
    });
    expect(style).toEqual(["dashed", "10px"]);
    expect(
      (await sp.valueOf()) &&
        (await empty
          .locator("span")
          .evaluate((x) => getComputedStyle(x).fontSize)),
    ).toBe("12.5px");
  });

  test(`delete confirm tile, ${mode}`, async ({ page }) => {
    test.skip(!plain, "Needs the plain owner world");
    await open(page, dark);
    await page.getByRole("option").first().click();
    await page.getByRole("button", { name: "Xoá phản hồi" }).click();
    const confirm = page.getByRole("alertdialog");
    await expect(confirm).toBeVisible();
    const c = (await confirm.boundingBox())!;
    const text = (await confirm.locator("span").first().boundingBox())!;
    const keep = (await confirm
      .getByRole("button", { name: "Giữ lại" })
      .boundingBox())!;
    const del = (await confirm
      .getByRole("button", { name: "Xoá", exact: true })
      .boundingBox())!;
    // Tile "Xoá phản hồi · xác nhận": card padding 14 + 1, text 24 right of the icon, buttons 28 high 10 px under it, 8 apart.
    near(text.x - c.x, 39, "text x");
    near(text.y - c.y, 15, "text y");
    near(keep.x - c.x, 15, "keep x");
    near(keep.y - (text.y + text.height), 10, "buttons gap");
    near(keep.height, 28, "keep height");
    near(keep.width, 60.5, "keep width");
    near(del.width, 47.3, "delete width");
    near(del.x - (keep.x + keep.width), 8, "button gap");
    near(c.y + c.height - (keep.y + keep.height), 15, "bottom");
    await expect(
      confirm.getByRole("button", { name: "Giữ lại" }),
    ).toBeFocused();
    await page.keyboard.press("Escape");
  });

  test(`keyboard focus row tile, ${mode}`, async ({ page }) => {
    test.skip(!plain, "Needs the plain owner world");
    await open(page, dark);
    const first = page.getByRole("option").first();
    await first.focus();
    await page.keyboard.press("ArrowDown");
    const row = page.getByRole("option").nth(1);
    await expect(row).toBeFocused();
    await expect(row).toHaveAttribute("aria-selected", "true");
    // The 2 px focus ring sits inside the row (inset), in the accent.
    const shadow = await row.evaluate((x) => getComputedStyle(x).boxShadow);
    expect(shadow).not.toBe("none");
    expect(shadow).toContain("inset");
  });

  test(`loading tile, ${mode}`, async ({ page }) => {
    test.skip(
      latency !== "slow" || faults !== "",
      "Needs VITE_MOCK_LATENCY=slow",
    );
    await open(page, dark);
    const status = page.getByRole("status", { name: "Đang tải phản hồi" });
    await expect(status).toBeVisible();
    const rows = await status.locator("> *").all();
    expect(rows).toHaveLength(3);
    const y0 = (await rows[0]!.boundingBox())!;
    const y1 = (await rows[1]!.boundingBox())!;
    near(y0.height, 44, "row height");
    near(y1.y - (y0.y + y0.height), 8, "row gap");
  });

  test(`load error tile, ${mode}`, async ({ page }) => {
    test.skip(!faults.includes("GET /submissions"), "Needs the load fault");
    await open(page, dark);
    const alert = page
      .getByRole("alert")
      .filter({ hasText: "Chưa tải được phản hồi" });
    await expect(alert).toBeVisible();
    const card = alert.locator("xpath=..");
    const c = (await card.boundingBox())!;
    const text = (await alert
      .getByText("Chưa tải được phản hồi")
      .boundingBox())!;
    const retry = (await alert
      .getByRole("button", { name: "Thử lại" })
      .boundingBox())!;
    // Tile "Tải lỗi": card padding 14 + 1, text at 39, retry 28 high at 15.
    near(text.x - c.x, 39, "text x");
    near(text.y - c.y, 15, "text y");
    near(retry.x - c.x, 15, "retry x");
    near(retry.y - (text.y + text.height), 8, "retry gap");
    near(retry.height, 28, "retry height");
    near(retry.width, 84.9, "retry width");
    near(c.y + c.height - (retry.y + retry.height), 15, "bottom");
  });

  test(`action error tile, ${mode}`, async ({ page }) => {
    test.skip(!faults.includes("POST /spam=500"), "Needs the action fault");
    await open(page, dark);
    await page.getByRole("option").first().click();
    await page.getByRole("button", { name: "Đánh dấu spam" }).click();
    const alert = page
      .getByRole("alert")
      .filter({ hasText: "Chưa xử lý được phản hồi" });
    await expect(alert).toBeVisible();
    const text = (await alert
      .getByText("Chưa xử lý được phản hồi")
      .boundingBox())!;
    const retry = (await alert
      .getByRole("button", { name: "Thử lại" })
      .boundingBox())!;
    const a = (await alert.boundingBox())!;
    near(text.x - a.x, 24, "text x");
    near(text.height, 18.1, "text height");
    near(retry.y - (text.y + text.height), 8, "retry gap");
    near(retry.height, 28, "retry height");
    near(retry.width, 84.9, "retry width");
    await expect(page.getByRole("tab", { name: /^Mới/ })).toHaveAttribute(
      "aria-selected",
      "true",
    );
  });

  test(`conflict frame, ${mode}`, async ({ page }) => {
    test.skip(!faults.includes("POST /spam=409"), "Needs the conflict fault");
    await open(page, dark);
    await page.getByRole("option").first().click();
    await page.getByRole("button", { name: "Đánh dấu spam" }).click();
    const alert = page
      .getByRole("alert")
      .filter({ hasText: "vừa được người khác xử lý" });
    await expect(alert).toBeVisible();
    const text = (await alert.locator("span").boundingBox())!;
    const a = (await alert.boundingBox())!;
    // Frame "Xung đột": the warning is an inline 12.5 px line with a 16 px icon, 24 px to the text, inside the response card.
    near(text.x - a.x, 24, "text x");
    near(a.height, 18.1, "alert height");
    expect(await alert.evaluate((x) => x.closest("article") !== null)).toBe(
      true,
    );
    const before = (await alert.boundingBox())!;
    const buttons = (await page
      .getByRole("button", { name: "Tạo việc từ phản hồi" })
      .boundingBox())!;
    near(buttons.y - (before.y + before.height), 14, "alert to actions");
  });
}
