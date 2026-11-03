import { expect, test, type Locator } from "@playwright/test";

/**
 * Picking cards on the Kanban against the October 2026 canvas (owner artifact 18L4jUXGfdXh7Wn9YQvA2x), artboard
 * `BC-Bulk`, frame `bk-kan-sang` ("Kanban: cùng thanh, chọn bằng ô tích trên thẻ"). Numbers are read from the artboard
 * rendered in Chromium, relative to the card: the pick box is 18 x 18, 5 px radius, at (12, 13), filled with the accent
 * when picked; while any card is picked every card shows its box and the top row starts after it (26 px), so the label
 * and the key are never covered. The bar is the Table's own (measured in `design-fidelity-bulk`).
 *
 * Kept deviation: the canvas draws its picked cards 36 px high (a layout slip of the artboard); the app keeps the full
 * card, so only the pick box, the top row and the bar are measured. Keyboard: Space picks the focused card, Shift+click
 * extends the pick through the cards in board order.
 */
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";

const near = (actual: number, want: number, what: string) =>
  expect(
    Math.abs(actual - want),
    `${what}: ${actual.toFixed(1)} vs ${want}`,
  ).toBeLessThanOrEqual(1);

async function rel(el: Locator, origin: Locator) {
  const [e, o] = await Promise.all([el.boundingBox(), origin.boundingBox()]);
  if (!e || !o) throw new Error("element is not laid out");
  return { x: e.x - o.x, y: e.y - o.y, w: e.width, h: e.height };
}

test.describe("Kanban pick box and bar", () => {
  test.skip(
    persona !== "owner" || scenario !== "default",
    "Needs the default owner world",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  for (const mode of ["light", "dark"] as const) {
    test(`pick three cards, ${mode}`, async ({ page }) => {
      await page.addInitScript((theme) => {
        try {
          window.localStorage.setItem("notrelix-locale", "vi");
          window.localStorage.setItem("notrelix-theme-mode", theme);
        } catch {
          // Private mode: the defaults apply.
        }
      }, mode);
      await page.goto(BOARD);
      await expect(page.getByRole("region", { name: "Cần làm" })).toBeVisible();
      const lane = page.getByRole("region", { name: "Cần làm" });
      const cards = lane.getByRole("article");
      const first = cards.nth(0);
      const box = first.getByRole("checkbox");

      // Nothing picked: the box shows on focus and on hover only.
      await expect(box).toHaveCSS("opacity", "0");
      await box.focus();
      await box.press("Space");
      await expect(box).toHaveAttribute("aria-checked", "true");
      await cards.nth(1).getByRole("checkbox").click();
      await cards
        .nth(2)
        .getByRole("checkbox")
        .click({ modifiers: ["Shift"] });
      await page.mouse.move(2, 2);
      await page.waitForTimeout(400);
      for (const i of [0, 1, 2])
        await expect(cards.nth(i).getByRole("checkbox")).toHaveAttribute(
          "aria-checked",
          "true",
        );

      const b = await rel(box, first);
      near(b.x, 12, "box.x");
      near(b.y, 13, "box.y");
      near(b.w, 18, "box.w");
      near(b.h, 18, "box.h");
      expect(await box.evaluate((e) => getComputedStyle(e).borderRadius)).toBe(
        "5px",
      );
      // The unpicked cards show their box too, and the top row starts after it.
      const other = cards.nth(3);
      await expect(other.getByRole("checkbox")).toHaveCSS("opacity", "1");
      const row = other.locator("[id$='-top']");
      near(
        (await rel(row.locator("> *").first(), other)).x,
        12 + 26 - 1,
        "top row starts after the box",
      );

      const bar = page.getByRole("toolbar", {
        name: /^Thao tác với 3 công việc đã chọn/,
      });
      await expect(bar).toBeVisible();
      await expect(bar.getByText("3 đã chọn")).toBeVisible();
      const r = (await bar.boundingBox())!;
      near(r.height, 44, "bar.h");
      const viewport = page.viewportSize()!;
      near(
        r.y + r.height,
        viewport.height - 22,
        "bar sits 22 px above the window edge",
      );
      for (const name of [
        "Trạng thái",
        "Ưu tiên",
        "Người phụ trách",
        "Hạn",
        "Thêm",
        "Lưu trữ",
        "Xoá",
      ])
        await expect(
          bar.getByRole("button", { name, exact: true }),
        ).toBeVisible();

      await bar.getByRole("button", { name: "Bỏ chọn (Esc)" }).click();
      await expect(box).toHaveAttribute("aria-checked", "false");
      await expect(bar).toBeHidden();
    });
  }
});
