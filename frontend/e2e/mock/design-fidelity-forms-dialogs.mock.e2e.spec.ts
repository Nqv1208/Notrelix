import { expect, test, type Locator, type Page } from "@playwright/test";

/**
 * The reject dialog of the response inbox at 1440 px against `WM-Forms-Inbox` (light) and `WM-Forms-Dem` (dark),
 * rendered with `docs/design/mockups/2026-10/render.py`. Boxes are relative to the dialog's top-left; tolerance 1 px.
 * Fills are compared as sRGB within 6 per channel (the app's tokens are oklch, the artboard's hex); the Night danger
 * fill within 16, as `design-fidelity-forms.mock.e2e.spec.ts` allows it.
 *
 * Not compared, each with its reason:
 * - the hint's wording: the artboard says "người quản lý board", the app "người chỉnh sửa board" (editors read the
 *   reason, as the inbox's permission says);
 * - "Tạo việc từ phản hồi": UC-WM-N05 opens the ordinary create-item dialog prefilled from the answers; the
 *   artboard's 640 px dialog with "Từ: <câu hỏi>" source chips is an owner decision, not built.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const latency = process.env.VITE_MOCK_LATENCY ?? "instant";
const RESPONSES =
  "/workspaces/mock-workspace-primary/boards/mock-board-erp/forms/mock-form-it-support/responses";
const NOW = new Date("2026-10-12T10:40:00+07:00");

test.skip(
  persona !== "owner" ||
    scenario !== "default" ||
    faults !== "" ||
    latency !== "instant",
  "Needs the default owner world without faults or latency",
);

interface Rect {
  readonly x: number;
  readonly y: number;
  readonly width: number;
  readonly height: number;
}

const PALETTE = {
  light: {
    panel: [255, 255, 255],
    danger: [196, 35, 64],
    dangerInk: [255, 255, 255],
  },
  dark: {
    panel: [26, 32, 40],
    danger: [255, 107, 129],
    dangerInk: null,
  },
} as const;

async function openReject(page: Page, dark: boolean) {
  await page.setViewportSize({ width: 1440, height: 900 });
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
  await page.goto(RESPONSES);
  await page.getByRole("option").first().click();
  await page.getByRole("button", { name: "Từ chối…" }).click();
  const dialog = page.getByRole("dialog", { name: "Từ chối phản hồi này?" });
  await expect(dialog).toBeVisible();
  await page.waitForTimeout(300);
  return dialog;
}

async function within(origin: Locator, target: Locator): Promise<Rect> {
  const [o, b] = await Promise.all([
    origin.boundingBox(),
    target.boundingBox(),
  ]);
  if (!o || !b) throw new Error("element has no box");
  return { x: b.x - o.x, y: b.y - o.y, width: b.width, height: b.height };
}

function expectRect(actual: Rect, want: Partial<Rect>, label: string) {
  for (const key of ["x", "y", "width", "height"] as const) {
    const expected = want[key];
    if (expected === undefined) continue;
    expect
      .soft(
        Math.abs(actual[key] - expected),
        `${label}.${key}: ${actual[key].toFixed(1)} vs ${expected}`,
      )
      .toBeLessThanOrEqual(1);
  }
}

const style = (locator: Locator, property: string) =>
  locator.evaluate(
    (element, name) => getComputedStyle(element).getPropertyValue(name).trim(),
    property,
  );

/** A colour property as sRGB channels. */
const rgb = (locator: Locator, property: string) =>
  locator.evaluate((element, name) => {
    const canvas = document.createElement("canvas");
    canvas.width = canvas.height = 1;
    const context = canvas.getContext("2d");
    if (!context) return [0, 0, 0];
    context.fillStyle = getComputedStyle(element).getPropertyValue(name);
    context.fillRect(0, 0, 1, 1);
    return Array.from(context.getImageData(0, 0, 1, 1).data.slice(0, 3));
  }, property);

async function expectColour(
  locator: Locator,
  property: string,
  want: readonly number[],
  label: string,
  tolerance = 6,
) {
  const got = await rgb(locator, property);
  want.forEach((channel, index) =>
    expect
      .soft(
        Math.abs((got[index] ?? 0) - channel),
        `${label} ${got.join(",")} vs ${want.join(",")}`,
      )
      .toBeLessThanOrEqual(tolerance),
  );
}

for (const dark of [false, true]) {
  const mode = dark ? "dark" : "light";
  test(`reject dialog as drawn, ${mode}`, async ({ page }) => {
    const dialog = await openReject(page, dark);
    const P = PALETTE[mode];

    expectRect(
      await within(dialog, dialog),
      { width: 480, height: 242.7 },
      "dialog",
    );
    expect.soft(await style(dialog, "border-top-left-radius")).toBe("14px");
    await expectColour(dialog, "background-color", P.panel, "dialog fill");

    const title = dialog.getByRole("heading", {
      name: "Từ chối phản hồi này?",
    });
    expectRect(
      await within(dialog, title),
      { x: 21, y: 21, width: 438, height: 23.2 },
      "title",
    );
    expect.soft(await style(title, "font-size")).toBe("16px");
    expect.soft(await style(title, "font-weight")).toBe("650");

    const label = dialog.getByText("Lý do (không bắt buộc)");
    expectRect(
      await within(dialog, label),
      { x: 21, y: 58.2, width: 438, height: 18.1 },
      "label",
    );
    const reason = dialog.getByRole("textbox");
    expectRect(
      await within(dialog, reason),
      { x: 21, y: 82.3, width: 438, height: 70 },
      "reason",
    );
    expect.soft(await style(reason, "border-top-left-radius")).toBe("7px");
    // The visible hint (the dialog description repeats it for screen readers).
    const hint = dialog
      .locator("span")
      .filter({ hasText: /^Lưu cùng phản hồi/ });
    expectRect(
      await within(dialog, hint),
      { x: 21, y: 158.3, width: 438 },
      "hint",
    );
    expect.soft(await style(hint, "font-size")).toBe("12px");

    const keep = dialog.getByRole("button", { name: "Giữ lại" });
    const confirm = dialog.getByRole("button", { name: "Từ chối" });
    expectRect(await within(dialog, keep), { y: 189.7, height: 32 }, "keep");
    expectRect(
      await within(dialog, confirm),
      { x: 385.5, y: 189.7, width: 73.5, height: 32 },
      "confirm",
    );
    // The buttons end at the dialog's 21 px right padding, 8 px apart.
    const keepBox = await within(dialog, keep);
    expectRect(
      { ...keepBox, x: keepBox.x + keepBox.width },
      { x: 377.5 },
      "keep right edge",
    );
    expect.soft(await style(confirm, "font-weight")).toBe("600");
    expect.soft(await style(confirm, "border-top-left-radius")).toBe("7px");
    await expectColour(
      confirm,
      "background-color",
      P.danger,
      "confirm fill",
      dark ? 16 : 6,
    );
    if (P.dangerInk)
      await expectColour(confirm, "color", P.dangerInk, "confirm ink");

    // No close cross: Escape and Giữ lại close it.
    await expect(
      dialog.getByRole("button", { name: /close|đóng/i }),
    ).toHaveCount(0);
    await page.keyboard.press("Escape");
    await expect(dialog).toHaveCount(0);
  });
}
