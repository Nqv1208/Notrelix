import AxeBuilder from "@axe-core/playwright";
import { expect, test, type Page } from "@playwright/test";

/**
 * The version conflict of a list item on a phone (lead decision 2026-11-14 (b)): a bottom sheet shows both values
 * with "Giữ của bạn" and "Dùng bản mới"; there is no toast-only path. No artboard draws it, so it is built from the
 * design system's bottom sheet (14 px top radius, 36 x 5 grab handle, 20 px side padding) and measured at 390 px in
 * light and dark: 44 px buttons, no overflow, axe clean. At 1024 px the in-cell note stays (no sheet).
 *
 * Run with `VITE_MOCK_FAULTS=/board-items/mock-erp-item-142/status=409`.
 */
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const TABLE =
  "/workspaces/mock-workspace-primary/boards/mock-board-erp?view=table";
const TOL = 1;
const near = (a: number, w: number, what: string) =>
  expect(
    Math.abs(a - w),
    `${what}: ${a.toFixed(1)} vs ${w}`,
  ).toBeLessThanOrEqual(TOL);

async function refuse(page: Page, width: number, dark: boolean) {
  await page.setViewportSize({ width, height: width < 700 ? 844 : 768 });
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
  await page.goto(TABLE);
  await page.waitForTimeout(800);
  const more = page.getByRole("button", { name: /^Xem thêm/ });
  while (
    !(await page
      .getByRole("button", { name: "Đánh dấu xong ERP-142" })
      .count()) &&
    (await more.count())
  )
    await more.first().click();
  await page.getByRole("button", { name: "Đánh dấu xong ERP-142" }).click();
}

for (const dark of [false, true]) {
  test(`the phone conflict sheet, ${dark ? "dark" : "light"}`, async ({
    page,
  }) => {
    test.skip(persona !== "owner" || !faults.endsWith("status=409"));
    await refuse(page, 390, dark);
    const sheet = page.getByRole("alertdialog");
    await expect(sheet).toBeVisible();
    await expect(sheet).toContainText("vừa đổi Trạng thái thành");
    await expect(sheet).toContainText("Giá trị của bạn: Hoàn thành");
    await page.waitForTimeout(500);
    const box = (await sheet.boundingBox())!;
    near(box.x, 0, "sheet x");
    near(box.width, 390, "sheet width");
    near(box.y + box.height, 844, "sheet sits on the bottom edge");
    expect(
      await sheet.evaluate((el) => {
        const s = getComputedStyle(el);
        return [s.borderTopLeftRadius, s.paddingLeft];
      }),
    ).toEqual(["14px", "20px"]);
    const keep = sheet.getByRole("button", { name: "Giữ của bạn" });
    const use = sheet.getByRole("button", { name: "Dùng bản mới" });
    for (const button of [keep, use]) {
      const b = (await button.boundingBox())!;
      near(b.height, 44, "button height");
      near(b.width, 350, "button width");
    }
    const [k, u] = [(await keep.boundingBox())!, (await use.boundingBox())!];
    near(u.y - (k.y + k.height), 8, "gap between the buttons");
    expect(
      await page.evaluate(
        () => document.documentElement.scrollWidth <= window.innerWidth,
      ),
    ).toBe(true);
    const results = await new AxeBuilder({ page })
      .disableRules(["scrollable-region-focusable"])
      .analyze();
    expect(
      results.violations.filter((v) =>
        ["serious", "critical"].includes(v.impact ?? ""),
      ),
    ).toEqual([]);
    // "Dùng bản mới" drops the written value and closes the sheet.
    await use.click();
    await expect(sheet).toBeHidden();
  });
}
