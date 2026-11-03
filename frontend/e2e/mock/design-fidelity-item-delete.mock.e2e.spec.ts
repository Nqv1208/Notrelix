import { expect, test, type Locator, type Page } from "@playwright/test";

/**
 * The item delete confirmation against the October 2026 canvas (owner artifact 18L4jUXGfdXh7Wn9YQvA2x), artboard
 * `WM-Actions-Trash`, light and dark. Every number is read from the artboard rendered in Chromium (box relative to the
 * dialog, then font size, weight, radius and colour). Tolerance is 1 px for boxes; font sizes, weights and radii are
 * equal; colours differ by at most 10 per channel.
 *
 * Frames measured (ERP-142 has five sub-items and linked items; the canvas names three sub-items and a link, so the
 * rows are measured on their own boxes and the text may wrap to the same two lines):
 * - `dl` (default world): the 540 px dialog with the impact box, the footer hint, Huỷ and Xoá;
 * - `dbz` (`VITE_MOCK_LATENCY=slow`): Đang xoá…, the body line "Đang xoá. Yêu cầu đã gửi…", no Huỷ, no impact box;
 * - `dle` (`VITE_MOCK_FAULTS=/board-items/mock-erp-item-142=500`): the alert with Thử lại;
 * - `phone` (320 px): the bottom sheet with the grabber, the 44 px close button and two 48 px buttons, Xoá first;
 * - `toast` (after the delete): the Khôi phục toast.
 *
 * Kept deviation: the canvas toast ends with a 26 px close X; the shared Toaster has none (every toast of the app
 * leaves on its own timer), so the toast is measured to its colours and its Khôi phục action only. The toast fill, the action size and the
 * danger fill follow the shared tokens and the shared Toaster, not the canvas values (see the comments at each check).
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const latency = process.env.VITE_MOCK_LATENCY ?? "";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const ITEM = "mock-erp-item-142";
const NOW = new Date("2026-10-12T10:40:00+07:00");
const TOL = 1;
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
  ).toBeLessThanOrEqual(TOL);

async function box(el: Locator, origin: Locator): Promise<Box> {
  const [e, o] = await Promise.all([el.boundingBox(), origin.boundingBox()]);
  if (!e || !o) throw new Error("element is not laid out");
  return { x: e.x - o.x, y: e.y - o.y, w: e.width, h: e.height };
}

function expectBox(actual: Box, want: Partial<Box>, what: string) {
  for (const k of ["x", "y", "w", "h"] as const)
    if (want[k] !== undefined) near(actual[k], want[k]!, `${what}.${k}`);
}

async function css(el: Locator, property: string): Promise<string> {
  return el.evaluate(
    (node, name) => getComputedStyle(node).getPropertyValue(name).trim(),
    property,
  );
}

async function expectType(el: Locator, size: string, weight: string) {
  expect(await css(el, "font-size")).toBe(size);
  expect(await css(el, "font-weight")).toBe(weight);
}

async function rgb(el: Locator, property: string): Promise<number[]> {
  return el.evaluate((node, name) => {
    const c = document.createElement("canvas").getContext("2d", {
      willReadFrequently: true,
    })!;
    c.fillStyle = "#000";
    c.fillStyle = getComputedStyle(node).getPropertyValue(name);
    c.fillRect(0, 0, 1, 1);
    return [...c.getImageData(0, 0, 1, 1).data].slice(0, 3);
  }, property);
}

type Rgb = readonly [number, number, number];

async function expectColour(
  el: Locator,
  property: string,
  want: Rgb,
  what: string,
) {
  const got = await rgb(el, property);
  want.forEach((channel, i) =>
    expect(
      Math.abs(got[i]! - channel),
      `${what} ${property}: ${got.join(",")} vs ${want.join(",")}`,
    ).toBeLessThanOrEqual(10),
  );
}

const INK: Record<
  "light" | "dark",
  { text: Rgb; meta: Rgb; surface: Rgb; danger: Rgb; dangerSurface: Rgb }
> = {
  light: {
    text: [22, 26, 33],
    meta: [79, 88, 102],
    surface: [255, 255, 255],
    danger: [196, 35, 64],
    dangerSurface: [253, 236, 239],
  },
  dark: {
    text: [230, 233, 238],
    meta: [170, 179, 192],
    surface: [26, 32, 40],
    danger: [255, 107, 129],
    dangerSurface: [58, 43, 53],
  },
};

async function openDelete(
  page: Page,
  dark: boolean,
  fromCard = false,
): Promise<Locator> {
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
  if (fromCard) {
    // The fault of the error frame also answers the item read, so the sheet cannot open: use the phone table's row
    // menu, then widen the window (the dialog is the same component, drawn by media query).
    await page.setViewportSize({ width: 390, height: 844 });
    await page.goto(`${BOARD}?view=table`);
    await page
      .getByRole("button", { name: "Thao tác khác cho ERP-142" })
      .click();
    await page.getByRole("button", { name: /^Xoá…/ }).click();
    await page.setViewportSize({ width: 1440, height: 900 });
  } else {
    await page.goto(`${BOARD}?item=${ITEM}`);
    await page.getByRole("button", { name: "Thao tác trên thẻ" }).click();
    await page.getByRole("menuitem", { name: /^Xoá/ }).click();
  }
  const dialog = page.getByRole("dialog", { name: /^Xoá “/ });
  await expect(dialog).toBeVisible();
  // The dialog zooms in: measure it at rest.
  await page.waitForTimeout(700);
  await page.mouse.move(2, 2);
  return dialog;
}

/** Lines the title takes: the canvas title is one line; a longer item title wraps and pushes the rest down. */
async function titleLines(dialog: Locator) {
  const t = await dialog.getByRole("heading").first().boundingBox();
  return Math.round(t!.height / 26.1);
}

async function expectShell(dialog: Locator, ink: (typeof INK)["light"]) {
  const title = dialog.getByRole("heading").first();
  const lines = Math.round((await box(title, dialog)).h / 26.1);
  expectBox(
    await box(title, dialog),
    { x: 22, y: 20, h: 26.1 * lines },
    "title",
  );
  await expectType(title, "18px", "650");
  await expectColour(title, "color", ink.text, "title");
  const close = dialog.getByRole("button", { name: "Đóng" });
  expectBox(await box(close, dialog), { x: 500, y: 14, w: 28, h: 28 }, "close");
  expect(await css(close, "border-radius")).toBe("7px");
  expect(await css(dialog, "border-radius")).toBe("14px");
  await expectColour(dialog, "background-color", ink.surface, "dialog");
}

test.describe("item delete confirmation, desktop", () => {
  test.skip(!plain, "Needs the default owner world");
  test.use({ viewport: { width: 1440, height: 900 } });

  for (const mode of ["light", "dark"] as const) {
    test(`dl: the confirm with the impact box, ${mode}`, async ({ page }) => {
      test.skip(faults !== "" || latency === "slow", "Needs the plain world");
      const ink = INK[mode];
      const dialog = await openDelete(page, mode === "dark");
      expectBox(await box(dialog, dialog), { w: 540 }, "dialog");
      await expectShell(dialog, ink);
      const extra = ((await titleLines(dialog)) - 1) * 26.1;
      const body = dialog.getByText(/^Việc vào Việc đã xoá/);
      expectBox(
        await box(body, dialog),
        { x: 22, y: 52.1 + extra, h: 19.6 },
        "body",
      );
      await expectType(body, "13.5px", "400");
      await expectColour(body, "color", ink.meta, "body");

      const list = dialog.locator("ul");
      const rows = list.locator("> li");
      await expect(rows).toHaveCount(2);
      expectBox(
        await box(list, dialog),
        { x: 22, y: 87.7 + extra, w: 496 },
        "impact",
      );
      expect(await css(list, "border-radius")).toBe("10px");
      const first = rows.nth(0);
      expectBox(
        await box(first, dialog),
        { x: 23, y: 88.7 + extra, w: 494 },
        "row 1",
      );
      near((await box(first, dialog)).h, 57, "row 1.h");
      const second = rows.nth(1);
      near((await box(second, dialog)).h, 58, "row 2.h");
      for (const [row, top] of [
        [first, 98.7 + extra],
        [second, 156.6 + extra],
      ] as const) {
        const tile = row.locator("span").first();
        expectBox(
          await box(tile, dialog),
          { x: 37, y: top, w: 30, h: 30 },
          "tile",
        );
        expect(await css(tile, "border-radius")).toBe("7px");
        const lead = row.locator("b");
        near((await box(lead, dialog)).x, 78, "lead.x");
        near((await box(lead, dialog)).h, 18.8, "lead.h");
        await expectType(lead, "13px", "600");
        const note = row.locator("small");
        near((await box(note, dialog)).h, 18.1, "note.h");
        await expectType(note, "12.5px", "400");
      }

      const cancel = dialog.getByRole("button", { name: "Huỷ" });
      const del = dialog.getByRole("button", { name: "Xoá", exact: true });
      const d = await box(del, dialog);
      const c = await box(cancel, dialog);
      near(d.h, 30, "delete.h");
      near(c.h, 30, "cancel.h");
      near(d.w, 50.3, "delete.w");
      near(c.w, 48.4, "cancel.w");
      near(d.x + d.w, 518, "delete.right");
      near(c.x + c.w, d.x - 8, "cancel gap");
      near(d.y, 220.6 + extra, "delete.y");
      await expectType(del, "13px", "600");
      await expectType(cancel, "13px", "500");
      expect(await css(del, "border-radius")).toBe("7px");
      // Kept deviation: the canvas danger fill is rgb(196,35,64) (dark 255,107,129); the shared --danger token is used.
      await expectColour(
        del,
        "background-color",
        mode === "light" ? [176, 12, 21] : [253, 116, 105],
        "delete",
      );
      const hint = dialog.getByText(
        "Chỉ người có quyền sửa việc xoá và khôi phục.",
      );
      expectBox(
        await box(hint, dialog),
        { x: 22, y: 226.9 + extra, h: 17.4 },
        "hint",
      );
      await expectType(hint, "12px", "400");
      near((await dialog.boundingBox())!.height, 268.6 + extra, "dialog.h");
      await expect(del).toBeFocused();
    });
  }

  test("toast: the delete is undone from the toast", async ({ page }) => {
    test.skip(faults !== "" || latency === "slow", "Needs the plain world");
    const dialog = await openDelete(page, false);
    await dialog.getByRole("button", { name: "Xoá", exact: true }).click();
    const toast = page.locator("[data-sonner-toast]").first();
    await expect(toast).toBeVisible();
    await page.waitForTimeout(600);
    expect(await css(toast, "border-radius")).toBe("10px");
    // Kept deviation: the canvas toast is rgb(27,36,48); the shared Toaster paints the --toast token, rgb(22,26,33).
    await expectColour(toast, "background-color", [22, 26, 33], "toast");
    const action = toast.getByRole("button", { name: "Khôi phục" });
    await expect(action).toBeVisible();
    // Kept deviation: the shared Toaster action is 24 px tall and 12 px type where the canvas draws 26.8 and 13.
    near((await box(action, toast)).h, 24, "action.h");
    await expectType(action, "12px", "600");
    // Kept deviation: the shared Toaster action takes the toast ink, rgb(242,244,247); the canvas draws rgb(140,196,255).
    await expectColour(action, "color", [242, 244, 247], "action");
  });

  test("dbz: busy, no cancel and no impact box", async ({ page }) => {
    test.skip(latency !== "slow", "Needs VITE_MOCK_LATENCY=slow");
    const dialog = await openDelete(page, false);
    await dialog.getByRole("button", { name: "Xoá", exact: true }).click();
    const busy = dialog.getByRole("button", { name: "Đang xoá…" });
    await expect(busy).toBeVisible();
    await expect(dialog.getByRole("button", { name: "Huỷ" })).toHaveCount(0);
    await expect(dialog.locator("ul")).toHaveCount(0);
    await expectShell(dialog, INK.light);
    const extra = ((await titleLines(dialog)) - 1) * 26.1;
    expect(
      await dialog
        .getByRole("button", { name: "Đóng" })
        .getAttribute("aria-disabled"),
    ).toBe("true");
    expectBox(
      await box(busy, dialog),
      { x: 421.3, y: 93.7 + extra, w: 96.7, h: 30 },
      "busy",
    );
    expectBox(await box(dialog, dialog), { w: 540 }, "dialog");
    near((await dialog.boundingBox())!.height, 141.7 + extra, "dialog.h");
    const body = dialog.getByText(/^Đang xoá\. Yêu cầu đã gửi/);
    expectBox(
      await box(body, dialog),
      { x: 22, y: 52.1 + extra, h: 19.6 },
      "body",
    );
    // The dialog cannot be dismissed while the write is out.
    await page.keyboard.press("Escape");
    await expect(dialog).toBeVisible();
  });

  test("dle: network error with Thử lại", async ({ page }) => {
    test.skip(
      !faults.includes(`/board-items/${ITEM}=`),
      `Needs VITE_MOCK_FAULTS=/board-items/${ITEM}=500`,
    );
    const dialog = await openDelete(page, false, true);
    await dialog.getByRole("button", { name: "Xoá", exact: true }).click();
    const retry = dialog.getByRole("button", { name: "Thử lại" });
    await expect(retry).toBeVisible();
    await page.waitForTimeout(400);
    await expectShell(dialog, INK.light);
    const extra = ((await titleLines(dialog)) - 1) * 26.1;
    const alert = dialog.getByRole("alert");
    expectBox(
      await box(alert, dialog),
      { x: 22, y: 62.1 + extra, w: 496, h: 36.1 },
      "alert",
    );
    expect(await css(alert, "border-radius")).toBe("8px");
    await expectType(alert, "12.5px", "400");
    await expectColour(
      alert,
      "background-color",
      INK.light.dangerSurface,
      "alert",
    );
    await expectColour(alert, "color", INK.light.danger, "alert");
    await expect(dialog.locator("ul")).toHaveCount(0);
    expectBox(
      await box(retry, dialog),
      { x: 451.5, y: 114.2 + extra, w: 66.5, h: 30 },
      "retry",
    );
    expectBox(
      await box(dialog.getByRole("button", { name: "Huỷ" }), dialog),
      { x: 394.5, y: 114.2 + extra, w: 49.1, h: 30 },
      "cancel",
    );
    near((await dialog.boundingBox())!.height, 162.2 + extra, "dialog.h");
  });
});

test.describe("item delete confirmation, phone", () => {
  test.skip(
    !plain || faults !== "" || latency === "slow",
    "Needs the plain world",
  );
  test.use({ viewport: { width: 320, height: 640 }, hasTouch: true });

  for (const mode of ["light", "dark"] as const) {
    test(`phone: the bottom sheet, ${mode}`, async ({ page }) => {
      const ink = INK[mode];
      const sheet = await openDelete(page, mode === "dark");
      const s = (await sheet.boundingBox())!;
      near(s.width, 320, "sheet.w");
      near(s.y + s.height, 640, "sheet sits on the bottom edge");
      expect(await css(sheet, "border-top-left-radius")).toBe("14px");
      expect(await css(sheet, "border-bottom-left-radius")).toBe("0px");
      await expectColour(sheet, "background-color", ink.surface, "sheet");

      const title = sheet.getByRole("heading").first();
      await expectType(title, "17px", "650");
      near((await box(title, sheet)).x, 20, "title.x");
      const close = sheet.getByRole("button", { name: "Đóng" });
      const c = await box(close, sheet);
      near(c.w, 44, "close.w");
      near(c.h, 44, "close.h");
      near(c.x + c.w, 320 - 8, "close right edge");
      const del = sheet.getByRole("button", { name: "Xoá", exact: true });
      const cancel = sheet.getByRole("button", { name: "Huỷ" });
      const d = await box(del, sheet);
      const k = await box(cancel, sheet);
      for (const [b, name] of [
        [d, "delete"],
        [k, "cancel"],
      ] as const) {
        near(b.w, 288, `${name}.w`);
        near(b.h, 48, `${name}.h`);
        near(b.x, 16, `${name}.x`);
      }
      expect(d.y).toBeLessThan(k.y);
      near(k.y - d.y, 56, "buttons rhythm");
      await expectType(del, "15px", "600");
      await expectType(cancel, "15px", "500");
      expect(await css(del, "border-radius")).toBe("12px");
      // No impact box on a phone: the body names the sub-items.
      await expect(sheet.locator("ul")).toBeHidden();
      await expect(sheet.getByText(/việc con xoá cùng\.$/)).toBeVisible();
      const body = sheet.getByText(/^Việc vào Việc đã xoá/);
      await expectType(body, "14px", "400");
      await expectColour(body, "color", ink.meta, "body");
    });
  }
});
