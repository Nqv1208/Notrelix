import { expect, test, type Locator, type Page } from "@playwright/test";

/**
 * The item remind dialog against the October 2026 canvas (owner artifact 18L4jUXGfdXh7Wn9YQvA2x), artboard
 * `WM-Actions-Remind`, light and dark. Every number is read from the artboard rendered in Chromium (box relative to
 * the dialog, then type and colour). Tolerance is 1 px for boxes.
 *
 * The artboard sheet sits in a 1 px frame (318 px), so phone x and widths read 2 px more here.
 *
 * Frames: the confirm (520 px dialog, two impact rows, Huỷ and Gửi nhắc), the 24 hour refusal (second request in
 * the same clock), the busy frame (`VITE_MOCK_LATENCY=slow`), the network error
 * (`VITE_MOCK_FAULTS=/board-items/mock-erp-item-142/remind=500`) and the 320 px bottom sheet. The "nobody else"
 * frame needs an item assigned only to the caller and has its own component test; the toast frame uses the shared
 * Toaster.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const latency = process.env.VITE_MOCK_LATENCY ?? "";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const ITEM = "mock-erp-item-142";
const NOW = new Date("2026-10-12T14:30:00+07:00");
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

async function openRemind(page: Page, dark: boolean): Promise<Locator> {
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
  await page.getByRole("menuitem", { name: /^Nhắc người phụ trách/ }).click();
  const dialog = page.getByRole("dialog", { name: "Nhắc người phụ trách?" });
  await expect(dialog).toBeVisible();
  await page.waitForTimeout(700);
  await page.mouse.move(2, 2);
  return dialog;
}

async function expectHeader(dialog: Locator, dark: boolean) {
  const title = dialog.getByRole("heading").first();
  await expectBox(title, dialog, { x: 22, y: 20, h: 26.1 }, "title");
  expect(await css(title, "font-size")).toBe("18px");
  expect(await css(title, "font-weight")).toBe("650");
  await expectBox(
    dialog.getByRole("button", { name: "Đóng" }),
    dialog,
    { x: 480, y: 14, w: 28, h: 28 },
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

test.describe("item remind dialog, desktop", () => {
  test.skip(!plain, "Needs the default owner world");
  test.use({ viewport: { width: 1440, height: 900 } });

  for (const mode of ["light", "dark"] as const) {
    test(`confirm: the default frame, ${mode}`, async ({ page }) => {
      test.skip(faults !== "" || latency === "slow", "Needs the plain world");
      const dark = mode === "dark";
      const dialog = await openRemind(page, dark);
      await expectBox(dialog, dialog, { w: 520, h: 268.6 }, "dialog");
      await expectHeader(dialog, dark);
      const body = dialog.locator("p").first();
      await expectBox(body, dialog, { x: 22, y: 52.1, h: 19.6 }, "body");
      expect(await css(body, "font-size")).toBe("13.5px");
      const list = dialog.getByRole("list");
      await expectBox(
        list,
        dialog,
        { x: 22, y: 87.7, w: 476, h: 116.9 },
        "list",
      );
      expect(await css(list, "border-radius")).toBe("10px");
      const rows = list.getByRole("listitem");
      await expect(rows).toHaveCount(2);
      await expectBox(
        rows.nth(0),
        dialog,
        { x: 23, y: 88.7, w: 474, h: 57 },
        "row 0",
      );
      await expectBox(
        rows.nth(1),
        dialog,
        { x: 23, y: 145.6, w: 474, h: 58 },
        "row 1",
      );
      await expectBox(
        rows.nth(0).locator("b"),
        dialog,
        { x: 78, y: 98.7, h: 18.8 },
        "lead 0",
      );
      await expectBox(
        rows.nth(0).locator("small"),
        dialog,
        { x: 78, y: 117.5, h: 18.1 },
        "note 0",
      );
      await expectBox(
        rows.nth(1).locator("b"),
        dialog,
        { x: 78, y: 156.6, w: 264.6, h: 18.8 },
        "lead 1",
      );
      await expectBox(
        rows.nth(1).locator("small"),
        dialog,
        { x: 78, y: 175.5, w: 264.6, h: 18.1 },
        "note 1",
      );
      expect(await css(rows.nth(0).locator("b"), "font-weight")).toBe("600");
      expect(await css(rows.nth(0).locator("small"), "font-size")).toBe(
        "12.5px",
      );
      const ok = dialog.getByRole("button", { name: "Gửi nhắc" });
      const cancel = dialog.getByRole("button", { name: "Huỷ" });
      await expectBox(
        ok,
        dialog,
        { x: 417.1, y: 220.6, w: 80.9, h: 30 },
        "primary",
      );
      await expectBox(
        cancel,
        dialog,
        { x: 360.7, y: 220.6, w: 48.4, h: 30 },
        "cancel",
      );
      expect(await css(ok, "font-weight")).toBe("600");
      expect(await css(cancel, "font-weight")).toBe("500");
      await expect(ok).toBeFocused();
      await expectColour(
        ok,
        "background-color",
        dark ? [90, 169, 255] : [22, 102, 204],
        "primary",
      );
    });
  }

  test("tooSoon: the second reminder says when the next is possible", async ({
    page,
  }) => {
    test.skip(faults !== "" || latency === "slow", "Needs the plain world");
    const dialog = await openRemind(page, false);
    await dialog.getByRole("button", { name: "Gửi nhắc" }).click();
    await expect(dialog).toBeHidden();
    await page.getByRole("button", { name: "Thao tác trên thẻ" }).click();
    await page.getByRole("menuitem", { name: /^Nhắc người phụ trách/ }).click();
    await dialog.getByRole("button", { name: "Gửi nhắc" }).click();
    const note = dialog.getByRole("status");
    await expect(note).toHaveText(
      /^Bạn đã nhắc việc này lúc \d\d:\d\d hôm nay\. Nhắc lại được sau \d\d:\d\d ngày 13\/10\.$/,
    );
    await page.waitForTimeout(300);
    await expectBox(note, dialog, { x: 22, y: 62.1, w: 476 }, "banner");
    expect(await css(note, "border-radius")).toBe("8px");
    await expectColour(note, "background-color", [255, 244, 214], "banner");
    await expectColour(note, "color", [138, 90, 0], "banner");
    await expectBox(
      dialog.getByRole("button", { name: "Đóng", exact: true }).last(),
      dialog,
      { y: 132.3, w: 58.7, h: 30 },
      "close button",
    );
    await expect(dialog.getByRole("button", { name: "Gửi nhắc" })).toHaveCount(
      0,
    );
    near((await dialog.boundingBox())!.height, 180.3, "dialog.h");
  });

  for (const dark of [false, true]) {
    test(`nobody else: an info note, Đóng and Giao việc, ${dark ? "dark" : "light"}`, async ({
      page,
    }) => {
      test.skip(faults !== "" || latency === "slow", "Needs the plain world");
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
      // ERP-153 has no assignee, so nobody but the caller could be reminded.
      await page.goto(`${BOARD}?item=mock-erp-item-153`);
      await page.getByRole("button", { name: "Thao tác trên thẻ" }).click();
      await page
        .getByRole("menuitem", { name: /^Nhắc người phụ trách/ })
        .click();
      const dialog = page.getByRole("dialog", { name: "Chưa có ai để nhắc" });
      await expect(dialog).toBeVisible();
      await page.waitForTimeout(700);
      await page.mouse.move(2, 2);
      const title = dialog.getByRole("heading").first();
      await expectBox(title, dialog, { x: 22, y: 20, h: 26.1 }, "title");
      expect(await css(title, "font-size")).toBe("18px");
      expect(await css(dialog, "border-radius")).toBe("14px");
      await expectBox(
        dialog.getByRole("button", { name: "Đóng", exact: true }).first(),
        dialog,
        { x: 480, y: 14, w: 28, h: 28 },
        "close X",
      );
      const note = dialog.getByRole("status");
      await expect(note).toHaveText(
        "Việc này chưa giao cho ai khác ngoài bạn nên chưa có ai để nhắc.",
      );
      await expectBox(
        note,
        dialog,
        { x: 22, y: 62.1, w: 476, h: 36.1 },
        "note",
      );
      expect(await css(note, "border-radius")).toBe("8px");
      expect(await css(note, "font-size")).toBe("12.5px");
      await expectColour(
        note,
        "background-color",
        dark ? [14, 17, 22] : [247, 248, 250],
        "note",
      );
      const close = dialog
        .getByRole("button", { name: "Đóng", exact: true })
        .last();
      await expectBox(close, dialog, { y: 114.2, w: 58.7, h: 30 }, "Đóng");
      const assign = dialog.getByRole("button", { name: "Giao việc" });
      await expectBox(
        assign,
        dialog,
        { x: 412.6, y: 114.2, w: 85.4, h: 30 },
        "Giao việc",
      );
      await expectColour(
        assign,
        "background-color",
        dark ? [90, 169, 255] : [22, 102, 204],
        "Giao việc",
      );
      near((await dialog.boundingBox())!.height, 162.2, "dialog.h");
      // Giao việc opens the item where its assignees are set.
      await assign.click();
      await expect(dialog).toBeHidden();
    });
  }

  test("busy: no Huỷ, the close is dimmed", async ({ page }) => {
    test.skip(latency !== "slow", "Needs VITE_MOCK_LATENCY=slow");
    const dialog = await openRemind(page, false);
    await dialog.getByRole("button", { name: "Gửi nhắc" }).click();
    const busy = dialog.getByRole("button", { name: "Đang gửi…" });
    await expect(busy).toBeVisible();
    await expect(dialog.getByRole("button", { name: "Huỷ" })).toHaveCount(0);
    await expectHeader(dialog, false);
    await expectBox(
      busy,
      dialog,
      { x: 405.6, y: 93.7, w: 92.4, h: 30 },
      "busy",
    );
    near((await dialog.boundingBox())!.height, 141.7, "dialog.h");
    await page.keyboard.press("Escape");
    await expect(dialog).toBeVisible();
  });

  test("error: the alert with Thử lại", async ({ page }) => {
    test.skip(
      !faults.includes(`/board-items/${ITEM}/remind=`),
      "Needs the remind fault",
    );
    const dialog = await openRemind(page, false);
    await dialog.getByRole("button", { name: "Gửi nhắc" }).click();
    const retry = dialog.getByRole("button", { name: "Thử lại" });
    await expect(retry).toBeVisible();
    await page.waitForTimeout(300);
    const alert = dialog.getByRole("alert");
    await expectBox(
      alert,
      dialog,
      { x: 22, y: 62.1, w: 476, h: 36.1 },
      "alert",
    );
    await expectColour(alert, "background-color", [253, 236, 239], "alert");
    await expectBox(
      retry,
      dialog,
      { x: 430.8, y: 114.2, w: 67.2, h: 30 },
      "retry",
    );
    await expectBox(
      dialog.getByRole("button", { name: "Huỷ" }),
      dialog,
      { x: 374.4, y: 114.2, w: 48.4, h: 30 },
      "cancel",
    );
    near((await dialog.boundingBox())!.height, 162.2, "dialog.h");
  });
});

test.describe("item remind dialog, phone", () => {
  test.skip(!plain, "Needs the default owner world");
  test.use({ viewport: { width: 320, height: 740 }, hasTouch: true });

  for (const mode of ["light", "dark"] as const) {
    test(`phone: the bottom sheet, ${mode}`, async ({ page }) => {
      test.skip(faults !== "" || latency === "slow", "Needs the plain world");
      const dark = mode === "dark";
      const dialog = await openRemind(page, dark);
      const s = (await dialog.boundingBox())!;
      near(s.width, 320, "sheet.w");
      near(s.height, 271.9, "sheet.h");

      await expectBox(
        dialog.getByRole("heading").first(),
        dialog,
        { x: 20, y: 26.7, h: 24.6 },
        "title",
      );
      expect(await css(dialog.getByRole("heading").first(), "font-size")).toBe(
        "17px",
      );
      await expectBox(
        dialog.getByRole("button", { name: "Đóng" }),
        dialog,
        { x: 268, y: 17, w: 44, h: 44 },
        "close",
      );
      await expect(dialog.getByRole("list")).toBeHidden();
      const ok = dialog.getByRole("button", { name: "Gửi nhắc" });
      const cancel = dialog.getByRole("button", { name: "Huỷ" });
      await expectBox(
        ok,
        dialog,
        { x: 16, y: 143.9, w: 288, h: 48 },
        "primary",
      );
      await expectBox(
        cancel,
        dialog,
        { x: 16, y: 199.9, w: 288, h: 48 },
        "cancel",
      );
      await expectColour(
        ok,
        "background-color",
        dark ? [90, 169, 255] : [22, 102, 204],
        "primary",
      );
    });
  }
});
