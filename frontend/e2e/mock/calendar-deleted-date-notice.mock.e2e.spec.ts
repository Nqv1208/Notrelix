import AxeBuilder from "@axe-core/playwright";
import { expect, test } from "@playwright/test";

/**
 * The Calendar whose saved date field was deleted tells the member: a warning line above the view, items placed by
 * Bắt đầu → Hạn. Needs `VITE_MOCK_OVERLAYS=calendar-deleted-date-field` (the ERP board's saved view names a field that
 * no longer exists). Reviewed at 1440, 1024 and 390, light and dark: the notice is on screen inside the viewport, the
 * calendar still draws under it, and axe (WCAG A and AA, colour contrast included) finds nothing on the notice.
 * Screenshots are attached to each run.
 */
const overlays = process.env.VITE_MOCK_OVERLAYS ?? "";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const NOW = new Date("2026-10-12T10:40:00+07:00");

test.skip(
  !overlays.split(",").includes("calendar-deleted-date-field"),
  "Needs VITE_MOCK_OVERLAYS=calendar-deleted-date-field",
);

for (const [width, height] of [
  [1440, 900],
  [1024, 768],
  [390, 844],
] as const) {
  for (const dark of [false, true]) {
    test(`the deleted date field notice at ${width} px, ${dark ? "dark" : "light"}`, async ({
      page,
    }, info) => {
      await page.setViewportSize({ width, height });
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
      await page.goto(`${BOARD}?view=calendar`);
      const notice = page.getByRole("status").filter({
        hasText: "Trường ngày mà Lịch này đã chọn không còn",
      });
      await expect(notice).toBeVisible();
      await expect(page.locator("[data-calendar-event]").first()).toBeVisible();
      await page.waitForTimeout(400);

      const box = await notice.boundingBox();
      expect(box).not.toBeNull();
      expect(box!.x).toBeGreaterThanOrEqual(0);
      expect(box!.x + box!.width).toBeLessThanOrEqual(width);
      // Nothing of the notice sits under the calendar: the first week row starts below it.
      const view = page.locator("[data-calendar-view], [data-phone-calendar]");
      const viewBox = await view.first().boundingBox();
      expect(viewBox!.y).toBeGreaterThanOrEqual(box!.y + box!.height - 1);

      const results = await new AxeBuilder({ page })
        .include("[role=status]")
        .withTags(["wcag2a", "wcag2aa"])
        .analyze();
      expect(results.violations).toEqual([]);

      await info.attach(`notice-${width}-${dark ? "dark" : "light"}`, {
        body: await page.screenshot(),
        contentType: "image/png",
      });
    });
  }
}
