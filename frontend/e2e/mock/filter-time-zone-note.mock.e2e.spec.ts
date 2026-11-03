import AxeBuilder from "@axe-core/playwright";
import { expect, test, type Page } from "@playwright/test";

/**
 * The time-zone note on date filters and date buckets is an undrawn state: it grows in under the date row while it
 * has focus and sits last in the bucket menu, so the measured frames of the dialog and menus do not move. Reviewed at
 * 1440, 1024 and 390, light and dark, in Vietnamese: no overflow, axe clean, the screenshots go to REVIEW_DIR.
 */
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const REVIEW_DIR = process.env.REVIEW_DIR;
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const FILTER = "c2:and~mock-erp-col-golive.between.2026-10-12.2026-10-18";
const NOTE = /Ngày tính theo múi giờ của không gian làm việc: .+\./;

async function open(page: Page, width: number, height: number, dark: boolean) {
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
  await page.setViewportSize({ width, height });
  await page.goto(`${BOARD}?view=table&filter=${encodeURIComponent(FILTER)}`);
}

for (const [width, height] of [
  [1440, 900],
  [1024, 768],
  [390, 844],
] as const) {
  for (const dark of [false, true]) {
    test(`the note under the date row and in the bucket menu, ${width} px ${dark ? "dark" : "light"}`, async ({
      page,
    }) => {
      test.skip(
        scenario !== "default" || persona !== "owner" || faults !== "",
        "Needs the plain owner world",
      );
      await open(page, width, height, dark);
      await page.getByRole("button", { name: /^Lọc/ }).first().click();
      const dialog = page.getByRole("dialog", { name: "Lọc việc" });
      await expect(dialog).toBeVisible();
      await dialog.locator('input[type="date"]').first().focus();
      const note = dialog.getByText(NOTE);
      await expect(note).toBeVisible();
      const fits = await dialog.evaluate(
        (el) => el.scrollWidth <= el.clientWidth + 1,
      );
      expect(fits, "the dialog does not overflow sideways").toBe(true);
      expect(
        await page.evaluate(
          () => document.documentElement.scrollWidth <= window.innerWidth,
        ),
        "the page does not scroll sideways",
      ).toBe(true);
      const axe = await new AxeBuilder({ page })
        .disableRules(["scrollable-region-focusable"])
        .analyze();
      expect(
        axe.violations.filter((v) =>
          ["serious", "critical"].includes(v.impact ?? ""),
        ),
      ).toEqual([]);
      if (REVIEW_DIR)
        await page.screenshot({
          path: `${REVIEW_DIR}/filter-${width}-${dark ? "dark" : "light"}.png`,
        });

      await page.keyboard.press("Escape");
      await page.getByRole("button", { name: /^Nhóm/ }).first().click();
      const menu = page.getByRole("menu").first();
      await menu.getByRole("menuitem", { name: /Ngày/ }).first().hover();
      const bucket = page.getByRole("menu").last();
      await expect(bucket.getByText(NOTE)).toBeVisible();
      expect(
        await bucket.evaluate((el) => el.scrollWidth <= el.clientWidth + 1),
        "the bucket menu does not overflow sideways",
      ).toBe(true);
      if (REVIEW_DIR)
        await page.screenshot({
          path: `${REVIEW_DIR}/bucket-${width}-${dark ? "dark" : "light"}.png`,
        });
    });
  }
}
