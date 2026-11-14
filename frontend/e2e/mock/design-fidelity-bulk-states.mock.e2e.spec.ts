import { expect, test, type Locator, type Page } from "@playwright/test";

/**
 * The bulk bar's own frames of the October 2026 canvas (artboard `BC-Bulk`), measured against the
 * artboard rendered in Chromium (box relative to the bar or the menu, font, radius, colour):
 * `bk-more-*` (the bar with its Thêm menu open) and `bk-save-*` (the bar while a change is on its
 * way, run with `VITE_MOCK_LATENCY=slow`). Tolerance is 1 px.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const latency = process.env.VITE_MOCK_LATENCY ?? "instant";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const NOW = new Date("2026-10-12T10:40:00+07:00");
const TOL = 1;

interface Box {
  x: number;
  y: number;
  w: number;
  h: number;
}

async function rel(el: Locator, origin: Locator): Promise<Box> {
  const [e, o] = await Promise.all([el.boundingBox(), origin.boundingBox()]);
  if (!e || !o) throw new Error("no box");
  return { x: e.x - o.x, y: e.y - o.y, w: e.width, h: e.height };
}

function expectBox(actual: Box, want: Box, what: string) {
  if (process.env.BOXLOG) {
    console.log(
      "BOX",
      what,
      JSON.stringify(actual, (k, v) =>
        typeof v === "number" ? +v.toFixed(1) : v,
      ),
      "want",
      JSON.stringify(want),
    );
    return;
  }
  for (const k of ["x", "y", "w", "h"] as const)
    expect(
      Math.abs(actual[k] - want[k]),
      `${what}.${k}: ${actual[k].toFixed(1)} vs ${want[k]}`,
    ).toBeLessThanOrEqual(TOL);
}

async function open(page: Page, dark: boolean) {
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
  await page.goto(`${BOARD}?view=table`);
  await expect(page.getByRole("rowgroup").first()).toBeVisible();
  for (const key of ["ERP-142", "ERP-144", "ERP-146"])
    await page.getByRole("checkbox", { name: `Chọn ${key}` }).click();
  await page.mouse.move(2, 2);
  return page.locator("[data-table-bulk-bar]");
}

/** Boxes relative to the bar (artboard x - 195, y - 275). */
const BAR = { x: 0, y: 0, w: 792, h: 44 };
const BUTTONS: readonly (readonly [string, Box])[] = [
  ["Trạng thái", { x: 102.2, y: 6, w: 104.9, h: 32 }],
  ["Ưu tiên", { x: 209.1, y: 6, w: 83.8, h: 32 }],
  ["Người phụ trách", { x: 294.9, y: 6, w: 141.2, h: 32 }],
  ["Hạn", { x: 438.1, y: 6, w: 64.4, h: 32 }],
  ["Thêm", { x: 504.5, y: 6, w: 77.8, h: 32 }],
  ["Lưu trữ", { x: 595.3, y: 6, w: 86.8, h: 32 }],
  ["Xoá", { x: 684.1, y: 6, w: 67.9, h: 32 }],
];

test.describe("Bulk bar frames at 1440 px", () => {
  test.skip(
    persona !== "owner" || scenario !== "default" || faults !== "",
    "Needs the default owner world",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  for (const dark of [false, true]) {
    const mode = dark ? "dem" : "sang";

    test(`bk-more-${mode}: the bar and its Thêm menu`, async ({ page }) => {
      test.skip(latency === "slow", "Needs instant writes");
      const bar = await open(page, dark);
      expectBox({ ...(await rel(bar, bar)), x: 0, y: 0 }, BAR, "bar");
      const b = (await bar.boundingBox())!;
      const count = bar.getByText("3 đã chọn");
      expectBox(
        await rel(count, bar),
        { x: 14, y: 11.8, w: 69.2, h: 20.3 },
        "count",
      );
      for (const [name, want] of BUTTONS)
        expectBox(
          await rel(bar.getByRole("button", { name, exact: true }), bar),
          want,
          name,
        );
      expect(
        await bar
          .getByRole("button", { name: "Xoá", exact: true })
          .evaluate((e) => getComputedStyle(e).borderTopLeftRadius),
      ).toBe("8px");
      expect(b.width).toBeGreaterThan(0);

      await bar.getByRole("button", { name: "Thêm", exact: true }).click();
      const menu = page.getByRole("menu");
      await expect(menu).toBeVisible();
      await page.waitForTimeout(300);
      // The artboard places the menu by hand; the app opens it under the Thêm button, right-aligned.
      const placed = await rel(menu, bar);
      placed.x = 0;
      expectBox(
        { ...placed, y: placed.y + placed.h, h: 0 },
        { x: 0, y: -10, w: 320, h: 0 },
        "menu width and gap above the bar",
      );
      // Three 36 px rows 2 px apart in a 6 px padding and a 1 px border: 126 px (137.2 with the two-line row).
      expect(Math.abs(placed.h - 126)).toBeLessThanOrEqual(TOL);
      expectBox(
        await rel(menu.getByRole("menuitem", { name: "Xoá hạn" }), menu),
        { x: 7, y: 7, w: 306, h: 36 },
        "Xoá hạn",
      );
      expectBox(
        await rel(
          menu.getByRole("menuitem", { name: "Bỏ hết người phụ trách" }),
          menu,
        ),
        { x: 7, y: 45, w: 306, h: 36 },
        "Bỏ hết người phụ trách",
      );
      const move = menu.getByRole("menuitem", { name: /^Chuyển board…/ });
      // The mock world has a board to move to, so the row reads as an ordinary 36 px row; the
      // drawn disabled row (two lines, 47.2 px) shows when no board accepts items.
      expectBox(
        await rel(move, menu),
        { x: 7, y: 83, w: 306, h: 36 },
        "Chuyển board…",
      );
      expectBox(
        await rel(move.locator("svg"), move),
        { x: 10, y: 10, w: 16, h: 16 },
        "move icon",
      );
    });

    test(`bk-ok-${mode}: the toast with the count and Hoàn tác`, async ({
      page,
    }) => {
      test.skip(latency === "slow", "Needs instant writes");
      const bar = await open(page, dark);
      await bar
        .getByRole("button", { name: "Trạng thái", exact: true })
        .click();
      await page.getByRole("menuitem", { name: /Hoàn thành/ }).click();
      const toast = page.locator("[data-table-bulk-done]");
      await expect(toast).toContainText("Đã đổi trạng thái cho 3 việc");
      await page.waitForTimeout(500);
      expectBox(
        { ...(await rel(toast, toast)), x: 0, y: 0 },
        { x: 0, y: 0, w: 313.8, h: 48 },
        "toast",
      );
      expectBox(
        await rel(toast.locator("svg").first(), toast),
        { x: 14, y: 16, w: 16, h: 16 },
        "check",
      );
      const undo = toast.getByRole("button", { name: "Hoàn tác" });
      expectBox(
        await rel(undo, toast),
        { x: 230.5, y: 10, w: 71.3, h: 28 },
        "Hoàn tác",
      );
      expect(
        await undo.evaluate((e) => {
          const c = getComputedStyle(e);
          return [c.fontSize, c.fontWeight, c.borderTopLeftRadius];
        }),
      ).toEqual(["13px", "600", "6px"]);
      expect(
        await toast.evaluate((e) => getComputedStyle(e).borderTopLeftRadius),
      ).toBe("10px");
    });

    test(`bk-save-${mode}: the bar while the change is on its way`, async ({
      page,
    }) => {
      test.skip(latency !== "slow", "Needs VITE_MOCK_LATENCY=slow");
      const bar = await open(page, dark);
      await bar
        .getByRole("button", { name: "Trạng thái", exact: true })
        .click();
      await page.getByRole("menuitem", { name: /Hoàn thành/ }).click();
      const progress = bar.getByRole("status");
      await expect(progress).toHaveText("Đang áp dụng cho 3 việc…");
      expectBox(await rel(bar, bar), { x: 0, y: 0, w: 332, h: 32.3 }, "bar");
    });
  }
});
