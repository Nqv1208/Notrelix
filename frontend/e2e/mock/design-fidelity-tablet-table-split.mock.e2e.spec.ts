import { expect, test, type Locator, type Page } from "@playwright/test";

/**
 * The board table at 1180x796 with an item open, against `Final-T-Table` state `chia-doi`, sidebar open (`mo`),
 * light (`sang`) and dark (`dem`): `render.py Final-T-Table.dc.html out.html state=chia-doi mode=<m> sidebar=mo`.
 * Boxes are relative to the split's top-left (the artboard's `.t-left`, under the 56 px header). Tolerance is 1 px.
 *
 * Measured: the table side (502.3 px), the 1 px divider in the strong line colour, the 428.7 px pane (46 % of the
 * 932 px main column) over the full height, the table box that loses its margin, side borders and radius (1 px top
 * border only), and the 36 px fade over its right edge.
 *
 * Not compared, each with its reason:
 * - the pane's contents (the 56 px head with close, full screen, crumb, K/J and copy link; the 20 px title; the 44 px
 *   property chips; the comment bar): the pane holds the card popup's own sections in one column, which the popup
 *   specs measure, not the artboard's compact detail;
 * - the resize grip on the divider: not built, the split keeps 46 %;
 * - the open row's 2 px accent ring: the table marks the open row with its existing active style.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const ITEM = "mock-erp-item-142";
const NOW = new Date("2026-10-12T10:40:00+07:00");

test.skip(
  persona !== "owner" || scenario !== "default" || faults !== "",
  "Needs the default owner world without faults",
);
test.use({ viewport: { width: 1180, height: 796 } });

interface Rect {
  readonly x: number;
  readonly y: number;
  readonly width: number;
  readonly height: number;
}

const DIVIDER = { sang: [210, 215, 222], dem: [46, 55, 67] } as const;

async function openSplit(page: Page, mode: "sang" | "dem") {
  await page.clock.setFixedTime(NOW);
  await page.addInitScript((dark) => {
    try {
      window.localStorage.setItem("notrelix-locale", "vi");
      window.localStorage.setItem(
        "notrelix-theme-mode",
        dark ? "dark" : "light",
      );
    } catch {
      // Private mode: the defaults apply.
    }
  }, mode === "dem");
  await page.goto(`${BOARD}?view=table&item=${ITEM}`);
  await expect(page.locator("[data-slot=table-split-pane]")).toBeVisible();
  await expect(page.locator("[data-popup-pane]")).toBeVisible();
  await page.waitForTimeout(600);
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
    expect(
      Math.abs(actual[key] - expected),
      `${label}.${key}: ${actual[key].toFixed(1)} vs ${expected}`,
    ).toBeLessThanOrEqual(1);
  }
}

/** The element's background as sRGB channels (the app's tokens are oklch; the artboard's are hex). */
const fillRgb = (locator: Locator) =>
  locator.evaluate((element) => {
    const canvas = document.createElement("canvas");
    canvas.width = canvas.height = 1;
    const context = canvas.getContext("2d");
    if (!context) return [0, 0, 0];
    context.fillStyle = getComputedStyle(element).backgroundColor;
    context.fillRect(0, 0, 1, 1);
    return Array.from(context.getImageData(0, 0, 1, 1).data.slice(0, 3));
  });

const style = (locator: Locator, property: string) =>
  locator.evaluate(
    (element, name) => getComputedStyle(element).getPropertyValue(name).trim(),
    property,
  );

for (const mode of ["sang", "dem"] as const) {
  test(`chia-doi ${mode}: the table, the divider and the 46 % pane`, async ({
    page,
  }) => {
    await openSplit(page, mode);
    const split = page.locator("[data-main-table]");
    const pane = page.locator("[data-slot=table-split-pane]");
    const divider = pane.locator("xpath=preceding-sibling::span[1]");
    const table = split.locator("> div").first();
    const box = page.locator("[data-slot=tablet-table-box]");

    expectRect(await within(split, split), { width: 932 }, "split");
    expectRect(
      await within(split, table),
      { x: 0, y: 0, width: 502.3 },
      "table side",
    );
    expectRect(
      await within(split, divider),
      { x: 502.3, y: 0, width: 1 },
      "divider",
    );
    // Token tolerance: 6 per channel.
    const rgb = await fillRgb(divider);
    DIVIDER[mode].forEach((channel, index) =>
      expect(
        Math.abs((rgb[index] ?? 0) - channel),
        `divider ${rgb.join(",")} vs ${DIVIDER[mode].join(",")}`,
      ).toBeLessThanOrEqual(6),
    );
    const paneBox = await within(split, pane);
    expectRect(paneBox, { x: 503.3, y: 0, width: 428.7 }, "pane");
    expect(paneBox.height).toBeCloseTo((await within(split, split)).height, 0);

    expectRect(await within(split, box), { x: 0, width: 502.3 }, "box");
    expect(await style(box, "border-radius")).toBe("0px");
    expect(await style(box, "border-top-width")).toBe("1px");
    expect(await style(box, "border-left-width")).toBe("0px");
    expect(await style(box, "border-right-width")).toBe("0px");

    const fade = page.locator("[data-slot=tablet-table-fade]");
    if (await fade.count())
      expectRect(await within(split, fade), { x: 466.3, width: 36 }, "fade");
  });
}

test("the pane is a named region, not a dialog, and Escape closes it", async ({
  page,
}) => {
  await openSplit(page, "sang");
  const pane = page.locator("[data-popup-pane]");
  await expect(pane).toHaveAttribute("role", "region");
  await expect(page.getByRole("dialog")).toHaveCount(0);
  // The table beside it stays usable: nothing is inert or hidden.
  await expect(page.locator("[data-slot=tablet-table-box]")).toBeVisible();
  await pane.getByRole("button").first().focus();
  await page.keyboard.press("Escape");
  await expect(page.locator("[data-slot=table-split-pane]")).toHaveCount(0);
  await expect(page).not.toHaveURL(/item=/);
  // Without the pane the box is the full 14 px card again.
  await expect(page.locator("[data-slot=tablet-table-box]")).toHaveCSS(
    "border-radius",
    "14px",
  );
});

test("at 1440 the open item is still the dialog", async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.clock.setFixedTime(NOW);
  await page.goto(`${BOARD}?view=table&item=${ITEM}`);
  await expect(page.getByRole("dialog")).toBeVisible();
  await expect(page.locator("[data-popup-pane]")).toHaveCount(0);
});

test("nothing in the pane runs past its right edge", async ({ page }) => {
  await openSplit(page, "sang");
  const over = await page.evaluate(() => {
    const pane = document.querySelector("[data-popup-pane]");
    if (!pane) return ["no pane"];
    const edge = pane.getBoundingClientRect().right + 0.5;
    return Array.from(pane.querySelectorAll("button, input, h1, h2, h3, p"))
      .filter((element) => {
        const box = element.getBoundingClientRect();
        return box.width > 0 && box.right > edge;
      })
      .map((element) => element.outerHTML.slice(0, 80));
  });
  expect(over).toEqual([]);
});
