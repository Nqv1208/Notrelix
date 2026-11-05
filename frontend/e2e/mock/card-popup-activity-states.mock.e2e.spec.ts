import AxeBuilder from "@axe-core/playwright";
import { expect, test, type Page } from "@playwright/test";

/**
 * Review of the history of an item in the card popup in the states no artboard draws (the item page's
 * artboard Final-Detail is retired): the feed with its lines, loading, and a failed read with Retry. At 1440,
 * 1024 and 390 px, light and dark, in Vietnamese: the feed stays inside the popup (no sideways scroll), no
 * serious axe violation, and at 390 px every control of the feed is a 44 px target. `WM_SHOTS_DIR` writes a
 * screenshot of each state for review.
 *
 * Run once per world: plain (feed); `VITE_MOCK_LATENCY=slow` (loading);
 * `VITE_MOCK_FAULTS="GET /activity=500"` (error with Retry).
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const latency = process.env.VITE_MOCK_LATENCY ?? "instant";
const shots = process.env.WM_SHOTS_DIR ?? "";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const CARD = "Chuyển dữ liệu khách hàng từ hệ thống cũ sang ERP";

const world = faults.includes("GET /activity")
  ? "error"
  : latency === "slow"
    ? "loading"
    : faults === "" && scenario === "default" && persona === "owner"
      ? "feed"
      : "other";

async function open(page: Page, width: number, dark: boolean) {
  await page.setViewportSize({ width, height: width < 600 ? 844 : 900 });
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
  await page.goto(BOARD);
  await page.getByRole("button", { name: CARD, exact: true }).click();
  const dialog = page.getByRole("dialog", { name: CARD });
  await expect(dialog).toBeVisible();
  return dialog;
}

for (const width of [1440, 1024, 390]) {
  for (const dark of [false, true]) {
    const name = `${width} px ${dark ? "dark" : "light"}`;
    test(`history ${world}, ${name}`, async ({ page }) => {
      test.skip(world === "other", "Needs a known world");
      const dialog = await open(page, width, dark);
      if (world === "error") {
        const alert = dialog
          .getByRole("alert")
          .filter({ hasText: /lịch sử|hoạt động|tải/i });
        await expect(alert.first()).toBeVisible();
        await expect(
          alert.first().getByRole("button", { name: /Thử lại/ }),
        ).toBeVisible();
      }
      if (world === "loading")
        await expect(dialog.getByRole("status").first()).toBeVisible({
          timeout: 3000,
        });
      await page.waitForTimeout(700);
      if (shots)
        await page.screenshot({
          path: `${shots}/activity-${world}-${width}-${dark ? "dark" : "light"}.png`,
        });

      const overflow = await page.evaluate(
        () => document.documentElement.scrollWidth - window.innerWidth,
      );
      expect(overflow, "no sideways scroll").toBeLessThanOrEqual(0);
      const clipped = await dialog.evaluate((root) => {
        const frame = root.getBoundingClientRect();
        return [...root.querySelectorAll<HTMLElement>("p, span, b, button")]
          .filter((el) => el.offsetParent !== null)
          .filter((el) => el.getBoundingClientRect().right > frame.right + 1)
          .map((el) => el.textContent?.slice(0, 30));
      });
      expect(clipped, "nothing runs past the popup").toEqual([]);

      const serious = (
        await new AxeBuilder({ page }).analyze()
      ).violations.filter((v) =>
        ["serious", "critical"].includes(v.impact ?? ""),
      );
      expect(serious.map((v) => v.id)).toEqual([]);

      if (width === 390 && world !== "loading") {
        const small = await dialog.evaluate((root) => {
          const retry = [
            ...root.querySelectorAll<HTMLElement>("button"),
          ].filter((b) => /Thử lại|Xem thêm/.test(b.textContent ?? ""));
          return retry
            .map((b) => b.getBoundingClientRect())
            .filter((r) => r.height < 43.5)
            .map((r) => r.height);
        });
        expect(small, "44 px targets").toEqual([]);
      }
    });
  }
}
