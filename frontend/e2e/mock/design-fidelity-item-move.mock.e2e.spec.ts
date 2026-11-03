import { expect, test, type Locator, type Page } from "@playwright/test";

/**
 * The item move dialog against the October 2026 canvas (owner artifact 18L4jUXGfdXh7Wn9YQvA2x), artboard
 * `WM-Actions-Move`, light and dark. Every number is read from the artboard rendered in Chromium (box relative to
 * the dialog, then type and colour). Tolerance is 1 px for boxes.
 *
 * Frames: `pick` (nothing chosen: search, the boards, the hint, Huỷ and the locked Chuyển board), `preview` (a board
 * chosen: the impact list, the tick when something is lost, the primary names the board or the loss), `busy`
 * (`VITE_MOCK_LATENCY=slow`: two skeleton lines, only the primary), `error`
 * (`VITE_MOCK_FAULTS=/board-items/mock-erp-item-142/move-to-board=500`: the alert, Huỷ and Thử lại) and the 320 px
 * sheet. The refusal frames (dependencies, archived), the empty and no-match frames and the conflict toast need
 * item or board states the mock does not offer or are covered by the component tests; the done toast uses the shared
 * Toaster. The mock's boards are not the canvas's three, so row boxes are measured per row of the list.
 * The artboard sheet sits in a 1 px frame (318 px), so phone x and widths read 2 px more here.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const latency = process.env.VITE_MOCK_LATENCY ?? "";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const ITEM = "mock-erp-item-142";
const NOW = new Date("2026-10-12T10:40:00+07:00");
const plain = persona === "owner" && scenario === "default";

interface Box {
  x: number;
  y: number;
  w: number;
  h: number;
}

const near = (actual: number, want: number, what: string) =>
  expect(
    Math.abs(actual - want),
    `${what}: ${actual.toFixed(1)} vs ${want}`,
  ).toBeLessThanOrEqual(1);

async function box(el: Locator, origin: Locator): Promise<Box> {
  const [e, o] = await Promise.all([el.boundingBox(), origin.boundingBox()]);
  if (!e || !o) throw new Error("element is not laid out");
  return { x: e.x - o.x, y: e.y - o.y, w: e.width, h: e.height };
}

function expectBox0(actual: Box, want: Partial<Box>, what: string) {
  for (const k of ["x", "y", "w", "h"] as const)
    if (want[k] !== undefined) near(actual[k], want[k]!, `${what}.${k}`);
}

async function expectBox(
  el: Locator,
  origin: Locator,
  want: Partial<Box>,
  what: string,
) {
  const actual = await box(el, origin);
  for (const k of ["x", "y", "w", "h"] as const)
    if (want[k] !== undefined) near(actual[k], want[k]!, `${what}.${k}`);
}

const css = (el: Locator, property: string) =>
  el.evaluate(
    (node, name) => getComputedStyle(node).getPropertyValue(name).trim(),
    property,
  );

type Rgb = readonly [number, number, number];

async function expectColour(
  el: Locator,
  property: string,
  want: Rgb,
  what: string,
) {
  const got = await el.evaluate((node, name) => {
    const c = document
      .createElement("canvas")
      .getContext("2d", { willReadFrequently: true })!;
    c.fillStyle = "#000";
    c.fillStyle = getComputedStyle(node).getPropertyValue(name);
    c.fillRect(0, 0, 1, 1);
    return [...c.getImageData(0, 0, 1, 1).data].slice(0, 3);
  }, property);
  want.forEach((channel, i) =>
    expect(
      Math.abs(got[i]! - channel),
      `${what} ${property}: ${got.join(",")} vs ${want.join(",")}`,
    ).toBeLessThanOrEqual(10),
  );
}

async function openMove(page: Page, dark: boolean): Promise<Locator> {
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
  await page.goto(`${BOARD}?item=${ITEM}`);
  await page.getByRole("button", { name: "Thao tác trên thẻ" }).click();
  await page.getByRole("menuitem", { name: /^Chuyển board/ }).click();
  const dialog = page.getByRole("dialog");
  await expect(dialog).toBeVisible();
  await page.waitForTimeout(700);
  await page.mouse.move(2, 2);
  return dialog;
}

/** The mock item's title is longer than the canvas's, so it may take a second line: everything below moves down. */
async function shift(dialog: Locator): Promise<number> {
  const h = (await box(dialog.getByRole("heading").first(), dialog)).h;
  return Math.round((h - 26.1) * 10) / 10;
}

async function expectHeader(dialog: Locator, dark: boolean) {
  const title = dialog.getByRole("heading").first();
  await expectBox(title, dialog, { x: 22, y: 20 }, "title");
  expect(await css(title, "font-size")).toBe("18px");
  expect(await css(title, "font-weight")).toBe("650");
  await expectBox(
    dialog.getByRole("button", { name: "Đóng" }),
    dialog,
    { x: 520, y: 14, w: 28, h: 28 },
    "close",
  );
  expect(await css(dialog, "border-radius")).toBe("14px");
  await expectColour(
    dialog,
    "background-color",
    dark ? [26, 32, 40] : [255, 255, 255],
    "dialog",
  );
}

test.describe("item move dialog, desktop", () => {
  test.skip(!plain, "Needs the default owner world");
  test.use({ viewport: { width: 1440, height: 900 } });

  for (const mode of ["light", "dark"] as const) {
    test(`pick: nothing chosen, ${mode}`, async ({ page }) => {
      test.skip(faults !== "" || latency === "slow", "Needs the plain world");
      const dark = mode === "dark";
      const dialog = await openMove(page, dark);
      const dy = await shift(dialog);
      await expectBox(dialog, dialog, { w: 560 }, "dialog");
      await expectHeader(dialog, dark);
      await expectBox(
        dialog.locator("p").first(),
        dialog,
        { x: 22, y: 52.1 + dy, w: 486, h: 39.1 },
        "body",
      );
      const search = dialog.getByRole("textbox");
      await expectBox(
        search.locator(".."),
        dialog,
        { x: 22, y: 107.2 + dy, w: 516, h: 34 },
        "search",
      );
      expect(await css(search.locator(".."), "border-radius")).toBe("8px");
      await expect(search).toBeFocused();
      const list = dialog.getByRole("listbox");
      const options = list.getByRole("option");
      const n = Math.min(await options.count(), 6);
      expect(n).toBeGreaterThan(0);
      await expectBox(
        list,
        dialog,
        { x: 22, y: 151.2 + dy, w: 516, h: n * 34 + 8 },
        "list",
      );
      for (let i = 0; i < n; i++)
        await expectBox(
          options.nth(i),
          dialog,
          { x: 27, y: 156.2 + 34 * i + dy, w: 506, h: 34 },
          `option ${i}`,
        );
      expect(await css(options.nth(0), "border-radius")).toBe("7px");
      expect(await css(options.nth(0), "font-size")).toBe("13.5px");
      const footTop = 151.2 + n * 34 + 8 + dy;
      const hint = dialog.getByText("Chọn một board để xem trước.");
      await expectBox(hint, dialog, { x: 22, h: 17.4 }, "hint");
      near((await box(hint, dialog)).y, footTop + 22.3, "hint.y");
      const ok = dialog.getByRole("button", {
        name: "Chuyển board",
        exact: true,
      });
      const cancel = dialog.getByRole("button", { name: "Huỷ" });
      await expectBox(
        ok,
        dialog,
        { x: 423.1, y: footTop + 16, w: 114.9, h: 30 },
        "primary",
      );
      await expectBox(
        cancel,
        dialog,
        { x: 366.7, y: footTop + 16, w: 48.4, h: 30 },
        "cancel",
      );
      await expect(ok).toHaveAttribute("aria-disabled", "true");
      near((await dialog.boundingBox())!.height, footTop + 64, "dialog.h");
    });

    test(`preview: a board chosen, ${mode}`, async ({ page }) => {
      test.skip(faults !== "" || latency === "slow", "Needs the plain world");
      const dark = mode === "dark";
      const dialog = await openMove(page, dark);
      const dy = await shift(dialog);
      const list = dialog.getByRole("listbox");
      const options = list.getByRole("option");
      const n = Math.min(await options.count(), 6);
      await options.nth(0).click();
      await page.waitForTimeout(500);
      await expect(options.nth(0)).toHaveAttribute("aria-selected", "true");
      await expectBox(
        options.nth(0),
        dialog,
        { x: 27, y: 156.2 + dy, w: 506, h: 34 },
        "chosen",
      );
      expect(await css(options.nth(0), "font-weight")).toBe("600");
      await expectColour(
        options.nth(0),
        "background-color",
        // Dark uses the shared brand-subtle token (the canvas tints the accent at 16 %, which reads 36,54,74).
        dark ? [22, 35, 52] : [231, 240, 252],
        "chosen",
      );
      const impact = dialog.locator("ul.border-line").last();
      const top = 151.2 + n * 34 + 8 + 10 + dy;
      await expectBox(impact, dialog, { x: 22, y: top, w: 516 }, "impact");
      expect(await css(impact, "border-radius")).toBe("10px");
      const rows = impact.getByRole("listitem");
      const count = await rows.count();
      expect(count).toBeGreaterThanOrEqual(3);
      let y = top + 1;
      for (let i = 0; i < count; i++) {
        const r = await box(rows.nth(i), dialog);
        expectBox0(r, { x: 23, y, w: 514 }, `row ${i}`);
        near(r.h, i === 0 ? 57 : i === 1 ? 58 : r.h, `row ${i}.h`);
        y += r.h;
        await expectBox(
          rows.nth(i).locator("b"),
          dialog,
          { x: 78, h: 18.8 },
          `lead ${i}`,
        );
        expect(await css(rows.nth(i).locator("b"), "font-weight")).toBe("600");
        expect(await css(rows.nth(i).locator("small"), "font-size")).toBe(
          "12.5px",
        );
      }
      const lost = rows.last();
      const losing = (await impact.getByText(/^Mất \d+ mục$/).count()) > 0;
      const cancel = dialog.getByRole("button", { name: "Huỷ" });
      if (losing) {
        await expectColour(
          lost.locator("span").first(),
          "background-color",
          dark ? [57, 40, 10] : [255, 244, 214],
          "warn tile" /* dark: the shared warning-subtle token */,
        );
        const tick = dialog.getByRole("checkbox");
        await expect(tick).toHaveCount(1);
        await expectBox(tick, dialog, { x: 22, w: 16, h: 16 }, "tick");
        await expect(
          dialog.getByText("Hết bước xác nhận mới chuyển được."),
        ).toBeVisible();
        await expect(
          dialog.getByRole("button", { name: /^Chuyển và bỏ \d+ mục$/ }),
        ).toHaveAttribute("aria-disabled", "true");
        await tick.click();
        await expect(
          dialog.getByRole("button", { name: /^Chuyển và bỏ \d+ mục$/ }),
        ).not.toHaveAttribute("aria-disabled", "true");
      } else {
        await expect(
          dialog.getByRole("button", { name: /^Chuyển sang / }),
        ).toBeVisible();
        await expect(dialog.getByRole("checkbox")).toHaveCount(0);
      }
      await expectBox(cancel, dialog, { h: 30 }, "cancel");
    });
  }

  test("busy: skeleton lines and only the primary", async ({ page }) => {
    test.skip(latency !== "slow", "Needs VITE_MOCK_LATENCY=slow");
    const dialog = await openMove(page, false);
    const dy = await shift(dialog);
    await dialog.getByRole("listbox").getByRole("option").nth(0).click();
    await expect(
      dialog.getByRole("button", { name: /^(Chuyển sang |Chuyển và bỏ )/ }),
    ).toBeVisible();
    const tick = dialog.getByRole("checkbox");
    if (await tick.count()) await tick.click();
    await dialog
      .getByRole("button", { name: /^(Chuyển sang |Chuyển và bỏ )/ })
      .click();
    const busy = dialog.getByRole("button", { name: "Đang chuyển…" });
    await expect(busy).toBeVisible();
    await expect(dialog.getByRole("button", { name: "Huỷ" })).toHaveCount(0);
    await expectHeader(dialog, false);
    await expectBox(
      dialog.locator("p").first(),
      dialog,
      { x: 22, y: 52.1 + dy, h: 19.6 },
      "body",
    );
    const lines = dialog.locator('[data-slot="skeleton"]');
    await expect(lines).toHaveCount(2);
    await expectBox(
      lines.nth(0),
      dialog,
      { x: 22, y: 87.7 + dy, w: 309.6, h: 12 },
      "line 0",
    );
    await expectBox(
      lines.nth(1),
      dialog,
      { x: 22, y: 107.7 + dy, w: 412.8, h: 12 },
      "line 1",
    );
    await expectBox(
      busy,
      dialog,
      { x: 418.7, y: 135.7 + dy, w: 119.3, h: 30 },
      "busy",
    );
    near((await dialog.boundingBox())!.height, 183.7 + dy, "dialog.h");
    await page.keyboard.press("Escape");
    await expect(dialog).toBeVisible();
  });

  test("error: the alert with Thử lại", async ({ page }) => {
    test.skip(
      !faults.includes(`/board-items/${ITEM}/move-to-board=`),
      "Needs the move fault",
    );
    const dialog = await openMove(page, false);
    const options = dialog.getByRole("listbox").getByRole("option");
    const n = Math.min(await options.count(), 6);
    await options.nth(0).click();
    await expect(
      dialog.getByRole("button", { name: /^(Chuyển sang |Chuyển và bỏ )/ }),
    ).toBeVisible();
    const tick = dialog.getByRole("checkbox");
    if (await tick.count()) await tick.click();
    await dialog
      .getByRole("button", { name: /^(Chuyển sang |Chuyển và bỏ )/ })
      .click();
    const retry = dialog.getByRole("button", { name: "Thử lại" });
    await expect(retry).toBeVisible();
    await page.waitForTimeout(300);
    const alert = dialog.getByRole("alert");
    await expect(alert).toHaveText(
      "Không chuyển được. Kiểm tra mạng rồi thử lại. Việc vẫn ở board cũ.",
    );
    await expectBox(alert, dialog, { x: 22, w: 516, h: 36.1 }, "alert");
    await expectColour(alert, "background-color", [253, 236, 239], "alert");
    near(
      (await box(alert, dialog)).y,
      (await box(dialog.getByRole("listbox"), dialog)).y +
        n * 34 +
        8 +
        10 +
        (await dialog
          .locator("ul.border-line")
          .last()
          .evaluate((e) => e.getBoundingClientRect().height)) +
        10 +
        ((await tick.count()) ? 28.8 : 0),
      "alert.y",
    );
    await expectBox(retry, dialog, { w: 67.2, h: 30 }, "retry");
  });
});

test.describe("item move dialog, phone", () => {
  test.skip(!plain, "Needs the default owner world");
  test.use({ viewport: { width: 320, height: 740 }, hasTouch: true });

  for (const mode of ["light", "dark"] as const) {
    test(`phone: the bottom sheet, ${mode}`, async ({ page }) => {
      test.skip(faults !== "" || latency === "slow", "Needs the plain world");
      const dark = mode === "dark";
      const dialog = await openMove(page, dark);
      const s = (await dialog.boundingBox())!;
      near(s.width, 320, "sheet.w");
      const title = dialog.getByRole("heading", { name: "Chuyển board…" });
      await expect(title).toBeVisible();
      await expectBox(title, dialog, { x: 20, y: 26.7, h: 24.6 }, "title");
      expect(await css(title, "font-size")).toBe("17px");
      await expectBox(
        dialog.getByRole("button", { name: "Đóng" }),
        dialog,
        { x: 268, y: 17, w: 44, h: 44 },
        "close",
      );
      await expect(dialog.getByRole("textbox")).toBeHidden();
      const options = dialog.getByRole("listbox").getByRole("option");
      const n = Math.min(await options.count(), 4);
      for (let i = 0; i < n; i++)
        await expectBox(
          options.nth(i),
          dialog,
          { x: 0, y: 65 + 52 * i, w: 320, h: 52 },
          `option ${i}`,
        );
      expect(await css(options.nth(0), "font-size")).toBe("15px");
      expect(await css(options.nth(0), "font-weight")).toBe("600");
      const cancel = dialog.getByRole("button", { name: "Huỷ" });
      const k = await box(cancel, dialog);
      near(k.h, 48, "cancel.h");
      near(k.w, 288, "cancel.w");
      near(k.x, 16, "cancel.x");
      await expectColour(
        dialog.getByRole("button", { name: "Chuyển board", exact: true }),
        "background-color",
        dark ? [90, 169, 255] : [22, 102, 204],
        "primary",
      );
    });
  }
});

test.describe("item move dialog, refusal", () => {
  test.skip(!plain, "Needs the default owner world");
  test.use({ viewport: { width: 1440, height: 900 } });

  test("dependencies: the refusal names the blocker and offers its way out", async ({
    page,
  }) => {
    test.skip(faults !== "" || latency === "slow", "Needs the plain world");
    await page.goto(`${BOARD}?item=mock-erp-item-146`);
    await page.getByRole("button", { name: "Thao tác trên thẻ" }).click();
    await page.getByRole("menuitem", { name: /^Chuyển board/ }).click();
    const dialog = page.getByRole("dialog");
    await dialog.getByRole("option").first().click();
    if ((await dialog.getByRole("checkbox").count()) > 0)
      await dialog.getByRole("checkbox").click();
    await dialog.getByRole("button", { name: /^Chuyển (sang|và bỏ)/ }).click();
    await expect(dialog.getByText(/^Chưa chuyển được: /)).toBeVisible();
    await expect(dialog.getByText(/phụ thuộc|đang chặn/)).toBeVisible();
  });
});
