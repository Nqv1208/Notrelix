import { expect, test, type Locator, type Page } from "@playwright/test";

/**
 * The item duplicate dialog against the October 2026 canvas (owner artifact 18L4jUXGfdXh7Wn9YQvA2x), artboard
 * `WM-Actions-Duplicate`, light and dark. Every number is read from the artboard rendered in Chromium (box relative
 * to the dialog, then type, radius and colour). Tolerance is 1 px for boxes.
 *
 * Frames: `dl` (default: 520 px dialog, name field with its counter, five parts with their notes, three locked rows,
 * the hint, Huỷ and Nhân bản), `none` (nothing ticked: the hint and the primary label change), `dbz`
 * (`VITE_MOCK_LATENCY=slow`: the parts stay, disabled, no name field, Đang nhân bản…), `dle`
 * (`VITE_MOCK_FAULTS=/board-items/mock-erp-item-142/duplicate=500`: the alert with Thử lại, the parts are left out),
 * `long` (a name over the limit: the warning, the parts are left out) and `phone` (the 320 px bottom sheet).
 *
 * Kept decisions: the name limit is 500 (the Domain and the catalog), where the canvas counter reads /200; the
 * canvas names the parts of a made-up item, so the notes are measured on their own boxes; the toast frames use the
 * shared Toaster (see the delete spec).
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

function expectBox(actual: Box, want: Partial<Box>, what: string) {
  for (const k of ["x", "y", "w", "h"] as const)
    if (want[k] !== undefined) near(actual[k], want[k]!, `${what}.${k}`);
}

const css = (el: Locator, property: string) =>
  el.evaluate(
    (node, name) => getComputedStyle(node).getPropertyValue(name).trim(),
    property,
  );

async function expectType(el: Locator, size: string, weight: string) {
  expect(await css(el, "font-size")).toBe(size);
  expect(await css(el, "font-weight")).toBe(weight);
}

type Rgb = readonly [number, number, number];

async function expectColour(
  el: Locator,
  property: string,
  want: Rgb,
  what: string,
) {
  const got = await el.evaluate((node, name) => {
    const c = document.createElement("canvas").getContext("2d", {
      willReadFrequently: true,
    })!;
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

async function openDuplicate(page: Page, dark: boolean): Promise<Locator> {
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
  await page.getByRole("menuitem", { name: /^Nhân bản/ }).click();
  const dialog = page.getByRole("dialog", { name: "Nhân bản việc" });
  await expect(dialog).toBeVisible();
  await page.waitForTimeout(700);
  await page.mouse.move(2, 2);
  return dialog;
}

async function expectHeader(dialog: Locator, dark: boolean) {
  const title = dialog.getByRole("heading").first();
  expectBox(await box(title, dialog), { x: 22, y: 20, h: 26.1 }, "title");
  await expectType(title, "18px", "650");
  expectBox(
    await box(dialog.getByRole("button", { name: "Đóng" }), dialog),
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

test.describe("item duplicate dialog, desktop", () => {
  test.skip(!plain, "Needs the default owner world");
  test.use({ viewport: { width: 1440, height: 900 } });

  for (const mode of ["light", "dark"] as const) {
    test(`dl: the default frame, ${mode}`, async ({ page }) => {
      test.skip(faults !== "" || latency === "slow", "Needs the plain world");
      const dialog = await openDuplicate(page, mode === "dark");
      expectBox(await box(dialog, dialog), { w: 520 }, "dialog");
      await expectHeader(dialog, mode === "dark");
      const body = dialog.getByText(/^Bản sao nằm ngay sau/);
      expectBox(await box(body, dialog), { x: 22, y: 52.1, h: 19.6 }, "body");
      await expectType(body, "13.5px", "400");

      const label = dialog.locator("label[for=dup-name]");
      expectBox(
        await box(label, dialog),
        { x: 22, y: 87.7, w: 476, h: 17.4 },
        "label",
      );
      await expectType(label, "12px", "600");
      const input = dialog.getByRole("textbox");
      expectBox(
        await box(input, dialog),
        { x: 22, y: 115, w: 476, h: 34 },
        "input",
      );
      expect(await css(input, "border-radius")).toBe("8px");
      await expectType(input, "13.5px", "400");
      await expect(input).toBeFocused();
      const count = dialog.getByText(/^\d+\/500$/);
      await expectType(count, "12px", "400");
      near(
        (await box(count, dialog)).x + (await box(count, dialog)).w,
        487,
        "counter right",
      );

      const parts = dialog.getByRole("group");
      expectBox(
        await box(parts, dialog),
        { x: 22, y: 159, w: 476, h: 362 },
        "parts",
      );
      expect(await css(parts, "border-radius")).toBe("10px");
      const rows = parts.locator("> label, > [role=checkbox]");
      await expect(rows).toHaveCount(8);
      for (let i = 0; i < 8; i++) {
        const r = await box(rows.nth(i), dialog);
        expectBox(r, { x: 27, y: 164 + 44 * i, w: 466, h: 44 }, `row ${i}`);
        expect(await css(rows.nth(i), "border-radius")).toBe("7px");
      }
      for (let i = 0; i < 5; i++) {
        const cb = rows.nth(i).getByRole("checkbox");
        expectBox(
          await box(cb, dialog),
          { x: 35, w: 16, h: 16 },
          `checkbox ${i}`,
        );
        expect(await css(cb, "border-radius")).toBe("4px");
        near(
          (await box(rows.nth(i).locator("b"), dialog)).x,
          61,
          `label ${i}.x`,
        );
        await expectType(rows.nth(i).locator("b"), "13.5px", "400");
      }
      for (let i = 5; i < 8; i++) {
        near(
          (await box(rows.nth(i).locator("b"), dialog)).x,
          60,
          `locked ${i}.x`,
        );
        await expectType(rows.nth(i).locator("small"), "11.5px", "400");
      }
      await expectType(rows.nth(0).locator("small"), "11.5px", "400");

      const hintLine = dialog.getByText(/^Mặc định chọn hết/);
      expectBox(
        await box(hintLine, dialog),
        { x: 22, y: 543.3, h: 17.4 },
        "hint",
      );
      await expectType(hintLine, "12px", "400");
      const ok = dialog.getByRole("button", { name: "Nhân bản", exact: true });
      const cancel = dialog.getByRole("button", { name: "Huỷ" });
      expectBox(await box(ok, dialog), { y: 537, w: 85.5, h: 30 }, "primary");
      expectBox(
        await box(cancel, dialog),
        { y: 537, w: 48.4, h: 30 },
        "cancel",
      );
      near(
        (await box(ok, dialog)).x + (await box(ok, dialog)).w,
        498,
        "primary right",
      );
      near(
        (await box(cancel, dialog)).x + (await box(cancel, dialog)).w,
        (await box(ok, dialog)).x - 8,
        "gap",
      );
      await expectType(ok, "13px", "600");
      await expectType(cancel, "13px", "500");
      expect(await css(ok, "border-radius")).toBe("7px");
      near((await dialog.boundingBox())!.height, 585, "dialog.h");
    });
  }

  test("none: nothing ticked changes the hint and the primary label", async ({
    page,
  }) => {
    test.skip(faults !== "" || latency === "slow", "Needs the plain world");
    const dialog = await openDuplicate(page, false);
    for (const box of await dialog.getByRole("checkbox").all()) {
      if ((await box.getAttribute("aria-disabled")) !== "true")
        await box.click();
    }
    await expect(
      dialog.getByText("Chỉ tên, mô tả và ngày được chép."),
    ).toBeVisible();
    const ok = dialog.getByRole("button", {
      name: "Nhân bản chỉ tên, mô tả, ngày",
    });
    await expect(ok).toBeVisible();
    expectBox(await box(ok, dialog), { w: 216.4, h: 30 }, "primary");
    near((await dialog.boundingBox())!.height, 589.8, "dialog.h");
  });

  test("dbz: busy keeps the parts, disabled", async ({ page }) => {
    test.skip(latency !== "slow", "Needs VITE_MOCK_LATENCY=slow");
    const dialog = await openDuplicate(page, false);
    await dialog.getByRole("button", { name: "Nhân bản", exact: true }).click();
    const busy = dialog.getByRole("button", { name: "Đang nhân bản…" });
    await expect(busy).toBeVisible();
    await expect(dialog.getByRole("button", { name: "Huỷ" })).toHaveCount(0);
    await expect(dialog.getByRole("textbox")).toHaveCount(0);
    await expectHeader(dialog, false);
    await expect(dialog.getByText(/^Đang tạo bản sao/)).toBeVisible();
    expectBox(
      await box(dialog.getByRole("group"), dialog),
      { x: 22, y: 87.7, w: 476, h: 362 },
      "parts",
    );
    expectBox(
      await box(busy, dialog),
      { x: 366, y: 465.7, w: 132, h: 30 },
      "busy",
    );
    near((await dialog.boundingBox())!.height, 513.7, "dialog.h");
    await page.keyboard.press("Escape");
    await expect(dialog).toBeVisible();
  });

  test("dle: the alert with Thử lại", async ({ page }) => {
    test.skip(
      !faults.includes(`/board-items/${ITEM}/duplicate=`),
      "Needs the duplicate fault",
    );
    const dialog = await openDuplicate(page, false);
    await dialog.getByRole("button", { name: "Nhân bản", exact: true }).click();
    const retry = dialog.getByRole("button", { name: "Thử lại" });
    await expect(retry).toBeVisible();
    await page.waitForTimeout(300);
    const alert = dialog.getByRole("alert");
    expectBox(
      await box(alert, dialog),
      { x: 22, y: 159, w: 476, h: 36.1 },
      "alert",
    );
    expect(await css(alert, "border-radius")).toBe("8px");
    await expectColour(alert, "background-color", [253, 236, 239], "alert");
    await expect(dialog.getByRole("group")).toHaveCount(0);
    expectBox(
      await box(retry, dialog),
      { x: 430.8, y: 211.2, w: 67.2, h: 30 },
      "retry",
    );
    expectBox(
      await box(dialog.getByRole("button", { name: "Huỷ" }), dialog),
      { x: 374.4, y: 211.2, w: 48.4, h: 30 },
      "cancel",
    );
    near((await dialog.boundingBox())!.height, 259.2, "dialog.h");
  });

  test("long: a name over the limit shows the warning", async ({ page }) => {
    test.skip(faults !== "" || latency === "slow", "Needs the plain world");
    const dialog = await openDuplicate(page, false);
    const input = dialog.getByRole("textbox");
    await input.fill("x".repeat(512));
    const warn = dialog.getByRole("alert");
    await expect(warn).toHaveText(
      "Tên tối đa 500 ký tự. Rút bớt 12 ký tự để nhân bản.",
    );
    expectBox(
      await box(warn, dialog),
      { x: 22, y: 159, w: 476, h: 36.1 },
      "warn",
    );
    await expectColour(warn, "background-color", [255, 244, 214], "warn");
    await expectColour(warn, "color", [138, 90, 0], "warn");
    await expect(dialog.getByRole("group")).toHaveCount(0);
    await expect(
      dialog.getByRole("button", { name: "Nhân bản", exact: true }),
    ).toBeDisabled();
    expectBox(
      await box(dialog.getByText("512/500"), dialog),
      { h: 17.4 },
      "counter",
    );
    await expectColour(
      dialog.getByText("512/500"),
      "color",
      [196, 35, 64],
      "counter",
    );
    near((await dialog.boundingBox())!.height, 259.2, "dialog.h");
  });
});

test.describe("item duplicate dialog, phone", () => {
  test.skip(
    !plain || faults !== "" || latency === "slow",
    "Needs the plain world",
  );
  test.use({ viewport: { width: 320, height: 740 }, hasTouch: true });

  for (const mode of ["light", "dark"] as const) {
    test(`phone: the bottom sheet, ${mode}`, async ({ page }) => {
      const sheet = await openDuplicate(page, mode === "dark");
      const s = (await sheet.boundingBox())!;
      near(s.width, 320, "sheet.w");
      near(s.y + s.height, 740, "sheet sits on the bottom edge");
      expect(await css(sheet, "border-top-left-radius")).toBe("14px");
      const title = sheet.getByRole("heading").first();
      await expectType(title, "17px", "650");
      near((await box(title, sheet)).x, 20, "title.x");
      const close = await box(
        sheet.getByRole("button", { name: "Đóng" }),
        sheet,
      );
      near(close.w, 44, "close.w");
      near(close.x + close.w, 312, "close right");
      const input = sheet.getByRole("textbox");
      expectBox(await box(input, sheet), { x: 20, w: 278 + 2, h: 44 }, "input");
      const rows = sheet
        .getByRole("group")
        .locator("> label, > [role=checkbox]");
      for (let i = 0; i < 5; i++) {
        expectBox(
          await box(rows.nth(i), sheet),
          { x: 0, w: 320, h: 52 },
          `row ${i}`,
        );
        near(
          (await box(rows.nth(i).getByRole("checkbox"), sheet)).x,
          20,
          `checkbox ${i}`,
        );
        near((await box(rows.nth(i).locator("b"), sheet)).x, 48, `label ${i}`);
        await expectType(rows.nth(i).locator("b"), "15px", "600");
        await expect(rows.nth(i).locator("small")).toBeHidden();
      }
      await expect(rows.nth(5)).toBeVisible();
      await expect(rows.nth(6)).toBeHidden();
      await expect(rows.nth(7)).toBeHidden();
      near((await box(rows.nth(5).locator("b"), sheet)).x, 56, "files label");
      const ok = await box(
        sheet.getByRole("button", { name: "Nhân bản", exact: true }),
        sheet,
      );
      const cancel = await box(
        sheet.getByRole("button", { name: "Huỷ" }),
        sheet,
      );
      for (const [b, name] of [
        [ok, "primary"],
        [cancel, "cancel"],
      ] as const) {
        near(b.w, 288, `${name}.w`);
        near(b.h, 48, `${name}.h`);
        near(b.x, 16, `${name}.x`);
      }
      expect(ok.y).toBeLessThan(cancel.y);
      near(cancel.y - ok.y, 56, "buttons rhythm");
      expect(
        await css(
          sheet.getByRole("button", { name: "Nhân bản", exact: true }),
          "border-radius",
        ),
      ).toBe("12px");
    });
  }
});
