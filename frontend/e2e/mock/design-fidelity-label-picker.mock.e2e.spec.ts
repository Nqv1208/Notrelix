import { expect, test } from "@playwright/test";

/**
 * The popup's label picker, typed with a name the board has not got, against `WM-Labels` ("Gắn nhãn trên việc · gõ tên
 * chưa có"), light and dark, tolerance 1 px, measured from the canvas render in Chromium: the "Chưa có nhãn" line 12.5 px,
 * the create row 36 px high with a 7 to 8 px radius, 13.5 px text and the 22 px / 6 px / 12 px / 600 preview chip on
 * its right, and the "Quản lý nhãn trong Thiết lập board" link 30 to 31 px high at 12.5 px / 500. The artboard
 * draws a 380 px card; the popup's picker is the 328 px panel shared with the other pickers (`Final-Card-Popup`), so the
 * widths and the panel's own header are that panel's, and what is measured is the rows.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const near = (a: number, w: number, what: string) =>
  expect(Math.abs(a - w), `${what}: ${a} vs ${w}`).toBeLessThanOrEqual(1);

for (const dark of [false, true]) {
  test(`typing a missing name offers to create it, ${dark ? "dark" : "light"}`, async ({
    page,
  }) => {
    test.skip(persona !== "owner" || scenario !== "default" || faults !== "");
    await page.setViewportSize({ width: 1440, height: 900 });
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
    await page.goto("/workspaces/mock-workspace-primary/boards/mock-board-erp");
    await page
      .getByRole("button", {
        name: "Chuyển dữ liệu khách hàng từ hệ thống cũ sang ERP",
        exact: true,
      })
      .click();
    await page.getByRole("button", { name: "Thêm nhãn" }).click();
    await page.getByPlaceholder(/nhãn/i).first().fill("Hạ tầng mới");
    const panel = page.locator("[data-slot=popover-content]").last();
    await expect(panel.getByText(/Chưa có nhãn/)).toBeVisible();
    await page.waitForTimeout(400);
    const create = panel.getByRole("button", { name: /Tạo nhãn “Hạ tầng/ });
    near((await create.boundingBox())!.height, 36, "create row height");
    const chip = create.getByText("Hạ tầng mới", { exact: true });
    const c = (await chip.boundingBox())!;
    near(c.height, 22, "chip height");
    const style = await chip.evaluate((el) => getComputedStyle(el));
    expect([style.borderRadius, style.fontSize, style.fontWeight]).toEqual([
      "6px",
      "12px",
      "600",
    ]);
    const link = panel.getByRole("link", { name: /Quản lý nhãn/ });
    const l = (await link.boundingBox())!;
    expect(l.height).toBeGreaterThanOrEqual(30);
    expect(l.height).toBeLessThanOrEqual(31.5);
  });
}
