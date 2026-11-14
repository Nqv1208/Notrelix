import { expect, test, type Locator } from "@playwright/test";

/**
 * The "Việc đã xoá" entry of the board menu against the October 2026 canvas (owner artifact 18L4jUXGfdXh7Wn9YQvA2x),
 * artboard `WM-Actions-Trash`, frames "Menu board (chi tiết)" and "Người chỉ xem". Numbers are read from the artboard
 * rendered in Chromium, relative to the 240 px menu: the entry is 230 x 32 at x 5, its icon 16 px at x 15, the label
 * (13.5 px, 500) at x 41 and the count badge 18 px high, radius 9, 11 px 700, right-aligned 10 px from the entry's edge.
 *
 * Kept decisions: the other entries keep the code's own rows (the canvas says "các mục còn lại giữ nguyên"), so only
 * this entry is measured; and the locked entry with its reason for a Viewer is not built: the catalog says Viewer and
 * Commenter do not see the entry (owner decision Q6), so the viewer world has no "Việc đã xoá" at all.
 * The world's ERP board has one deleted item (ERP-121), so the badge reads 1 where the canvas reads 2.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const TOL = 1;

const near = (actual: number, want: number, what: string) =>
  expect(
    Math.abs(actual - want),
    `${what}: ${actual.toFixed(1)} vs ${want}`,
  ).toBeLessThanOrEqual(TOL);

async function within(el: Locator, origin: Locator) {
  const [e, o] = await Promise.all([el.boundingBox(), origin.boundingBox()]);
  if (!e || !o) throw new Error("element is not laid out");
  return { x: e.x - o.x, y: e.y - o.y, w: e.width, h: e.height };
}

const css = (el: Locator, property: string) =>
  el.evaluate(
    (node, name) => getComputedStyle(node).getPropertyValue(name).trim(),
    property,
  );

test.describe("the Việc đã xoá entry of the board menu", () => {
  test.skip(
    persona !== "owner" || scenario !== "default",
    "Needs the default owner world",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  for (const mode of ["light", "dark"] as const) {
    test(`entry with its count, ${mode}`, async ({ page }) => {
      await page.addInitScript((m) => {
        try {
          window.localStorage.setItem("notrelix-locale", "vi");
          window.localStorage.setItem("notrelix-theme-mode", m);
        } catch {
          // Private mode: the defaults apply.
        }
      }, mode);
      await page.goto(`${BOARD}?view=table`);
      await page.getByRole("button", { name: "Thêm tuỳ chọn" }).click();
      const menu = page.getByRole("menu");
      await expect(menu).toBeVisible();
      await page.waitForTimeout(400);
      near((await within(menu, menu)).w, 240, "menu.w");
      const entry = menu.getByRole("menuitem", { name: /^Việc đã xoá/ });
      await expect(entry).toBeVisible();
      await expect(entry).toHaveAccessibleName(
        /^Việc đã xoá, \d+ việc khôi phục được$/,
      );
      const e = await within(entry, menu);
      // The canvas menu has a 1 px border outside the 4 px gap; the shared menu has none, so 4 and 232.
      near(e.x, 4, "entry.x");
      near(e.w, 232, "entry.w");
      near(e.h, 32, "entry.h");
      expect(await css(entry, "border-radius")).toBe("7px");
      expect(await css(entry, "font-size")).toBe("13.5px");
      expect(await css(entry, "font-weight")).toBe("500");
      const icon = entry.locator("svg").first();
      const i = await within(icon, menu);
      near(i.x - e.x, 10, "icon.x");
      near(i.w, 16, "icon.w");
      near(i.y - e.y, 8, "icon.y");
      const badge = entry.locator("span[aria-hidden]");
      const b = await within(badge, menu);
      near(b.h, 18, "badge.h");
      near(e.x + e.w - (b.x + b.w), 10, "badge right gap");
      expect(await css(badge, "border-radius")).toBe("9px");
      expect(await css(badge, "font-size")).toBe("11px");
      expect(await css(badge, "font-weight")).toBe("700");
    });
  }
});
