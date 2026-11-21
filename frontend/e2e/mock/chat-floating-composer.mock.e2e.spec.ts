import { expect, test } from "@playwright/test";
import { toNewest } from "./support/chat-newest";

const CHANNEL =
  "/workspaces/mock-workspace-primary/chat/c1000000-0000-4000-8000-000000000001";
const SHOTS = process.env.CHAT_SHOTS ?? "test-results";

for (const mode of ["light", "dark"] as const) {
  for (const width of [1440, 1280, 1024]) {
    test(`${width} ${mode}: a wide column, a composer floating on a fade, the last message readable above it`, async ({
      page,
    }) => {
      await page.setViewportSize({ width, height: 800 });
      await page.addInitScript((m) => {
        try {
          window.localStorage.setItem("notrelix-locale", "vi");
          window.localStorage.setItem("notrelix-theme-mode", m);
        } catch {
          // Private mode: the defaults apply.
        }
      }, mode);
      await page.goto(CHANNEL);
      await expect(page.locator("[data-bubble]").first()).toBeVisible();
      await page.waitForTimeout(600);
      await toNewest(page);

      const log = (await page.getByRole("log").boundingBox())!;
      const column = (await page.locator("[role=log] ol").boundingBox())!;
      // The column uses the room (capped at 1120): no wide empty margins either side.
      expect(column.width).toBeCloseTo(Math.min(1120, log.width), 0);

      // Messages and composer share the same left and right edges.
      const edges = await page.evaluate(() => {
        const row = document.querySelector("[data-message-id]")!;
        const style = getComputedStyle(row);
        const rect = row.getBoundingClientRect();
        const form = document
          .querySelector("form:has(textarea)")!
          .getBoundingClientRect();
        return {
          rowLeft: rect.left + parseFloat(style.paddingLeft),
          rowRight: rect.right - parseFloat(style.paddingRight),
          formLeft: form.left,
          formRight: form.right,
          formBottom: form.bottom,
          formTop: form.top,
          viewport: window.innerHeight,
        };
      });
      expect(Math.abs(edges.formLeft - edges.rowLeft)).toBeLessThanOrEqual(1);
      expect(Math.abs(edges.formRight - edges.rowRight)).toBeLessThanOrEqual(1);

      // The composer floats: lifted off the bottom, with no background band of its own.
      expect(edges.viewport - edges.formBottom).toBeGreaterThanOrEqual(12);
      const dock = page.getByTestId("chat-composer-dock");
      await expect(dock).toHaveCSS("position", "absolute");
      await expect(dock).toHaveCSS("background-color", "rgba(0, 0, 0, 0)");

      // A fade of the theme's background sits above it; the list's last message ends above the composer.
      const fade = page.getByTestId("chat-list-fade");
      await expect(fade).toHaveCSS("background-image", /linear-gradient/);
      const last = await page.evaluate(() => {
        const rows = Array.from(document.querySelectorAll("[data-message-id]"));
        const form = document.querySelector("form:has(textarea)")!;
        return {
          rowBottom: rows[rows.length - 1]!.getBoundingClientRect().bottom,
          formTop: form.getBoundingClientRect().top,
        };
      });
      expect(last.rowBottom).toBeLessThanOrEqual(last.formTop + 1);

      await page.screenshot({
        path: `${SHOTS}/chat-floating-${width}-${mode}.png`,
      });
    });
  }
}
