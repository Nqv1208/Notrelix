import { expect, test, type Locator, type Page } from "@playwright/test";

/**
 * Bulk bar and bulk failure geometry against the October 2026 canvas (owner artifact
 * 18L4jUXGfdXh7Wn9YQvA2x), artboard `BC-Bulk`. Every number is read from the artboard rendered in
 * Chromium (box relative to the frame, then font size, weight, radius and colour). Tolerance is 1 px
 * for boxes; font sizes, weights and radii must be equal.
 *
 * Measured here:
 * - `bk-err-sang` and `bk-err-dem`: the in-page red banner under the table with "Thử lại" (the mock
 *   server refuses the batch: run with `VITE_MOCK_FAULTS=/boards/mock-board-erp/items/bulk=409`);
 * - `bk-tbl-*`: the bar's own boxes that `design-fidelity-table` does not hold (height, radius,
 *   gap above the window edge, the 32 px actions);
 * - `bk-ro-*`: a viewer's bar (`VITE_MOCK_PERSONA=viewer`): the canvas draws no bar and no tick for
 *   a viewer; the app keeps the tick and the bar with every write disabled and the reason written
 *   on the bar, so the check is the reason, the dimming and the 44 px targets at 390 px;
 * - the 390 px bar: the clear-selection button is 44 x 44.
 *
 * Not measured (the canvas draws them; the app does not yet): `bk-cap-*` (the amber banner above a
 * locked bar for a pick over 200: the app writes the limit on the bar itself and the mock world has
 * no board of 201 items), `bk320-*` (the compact bar with one "Hành động" button and its sheet),
 * `bk-save-*`, `bk-gone-*`, `bk-ok-*`, `bk-grp-*` and `bk-more-*`.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const NOW = new Date("2026-10-12T10:40:00+07:00");
const TOL = 1;

interface Rect {
  readonly x: number;
  readonly y: number;
  readonly width: number;
  readonly height: number;
}

async function openTable(page: Page, dark = false) {
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
}

async function pick(page: Page, keys: readonly string[]) {
  for (const key of keys)
    await page.getByRole("checkbox", { name: `Chọn ${key}` }).click();
  await page.mouse.move(2, 2);
}

async function box(locator: Locator): Promise<Rect> {
  const found = await locator.boundingBox();
  if (!found) throw new Error("element has no box");
  return found;
}

/** A box relative to the top-left of `origin`. */
async function rel(locator: Locator, origin: Locator): Promise<Rect> {
  const [e, o] = await Promise.all([box(locator), box(origin)]);
  return { x: e.x - o.x, y: e.y - o.y, width: e.width, height: e.height };
}

async function style(locator: Locator, property: string): Promise<string> {
  return locator.evaluate(
    (element, name) => getComputedStyle(element).getPropertyValue(name).trim(),
    property,
  );
}

async function colour(locator: Locator, property: string): Promise<string> {
  return locator.evaluate((element, name) => {
    const canvas = document.createElement("canvas");
    canvas.width = canvas.height = 1;
    const context = canvas.getContext("2d", { willReadFrequently: true })!;
    context.fillStyle = "#000";
    context.fillStyle = getComputedStyle(element).getPropertyValue(name);
    context.fillRect(0, 0, 1, 1);
    const [r, g, b] = context.getImageData(0, 0, 1, 1).data;
    return `rgb(${r}, ${g}, ${b})`;
  }, property);
}

function near(actual: number, want: number, label: string) {
  expect(
    Math.abs(actual - want),
    `${label}: ${actual.toFixed(1)} vs ${want}`,
  ).toBeLessThanOrEqual(TOL);
}

async function refuseOnce(page: Page, dark: boolean) {
  await openTable(page, dark);
  await pick(page, ["ERP-142", "ERP-144", "ERP-146"]);
  await page.getByRole("button", { name: "Đặt hạn", exact: true }).click();
  await page.getByLabel("Hạn", { exact: true }).fill("2026-11-03");
  await page.getByRole("button", { name: "Áp dụng", exact: true }).click();
  const banner = page.locator("[data-table-bulk-failure]");
  await expect(banner).toBeVisible();
  return banner;
}

test.describe("Bulk failure banner at 1440 px (bk-err)", () => {
  test.skip(
    persona !== "owner" ||
      scenario !== "default" ||
      faults !== "/boards/mock-board-erp/items/bulk=409",
    "Needs the owner world with VITE_MOCK_FAULTS=/boards/mock-board-erp/items/bulk=409",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  for (const dark of [false, true]) {
    test(`the banner, its icon, title, reason and Thử lại (bk-err-${dark ? "dem" : "sang"})`, async ({
      page,
    }) => {
      const banner = await refuseOnce(page, dark);
      const g = await box(page.locator("main"));
      const b = await box(banner);
      // 1132 wide at x 25 inside the 1182 frame: the table's 24 px margin on both sides of the main area.
      near(b.x - g.x, 24, "banner.x");
      near(b.width, g.width - 48, "banner.width");
      near(b.height, 64, "banner.height");
      expect(await style(banner, "border-radius")).toBe("10px");
      expect(await style(banner, "padding")).toBe("10px 14px");
      expect(await style(banner, "gap")).toBe("10px");
      expect(await style(banner, "font-size")).toBe("13px");
      expect(await banner.getAttribute("role")).toBe("alert");
      if (!dark) {
        expect(await colour(banner, "background-color")).toBe(
          "rgb(253, 236, 239)",
        );
        expect(await colour(banner, "color")).toBe("rgb(196, 35, 64)");
      }

      const icon = banner.locator("svg").first();
      const i = await rel(icon, banner);
      near(i.x, 14, "icon.x");
      near(i.y, 12, "icon.y");
      near(i.width, 16, "icon.width");
      near(i.height, 16, "icon.height");

      const title = banner.locator("b");
      const t = await rel(title, banner);
      near(t.x, 40, "title.x");
      near(t.y, 10, "title.y");
      near(t.height, 18.2, "title.height");
      expect(await style(title, "font-size")).toBe("13px");
      expect(await style(title, "font-weight")).toBe("600");
      const reason = title.locator("xpath=following-sibling::span");
      const r = await rel(reason, banner);
      near(r.y, 29.2, "reason.y");
      near(r.height, 18.2, "reason.height");
      expect(await style(reason, "font-weight")).toBe("400");

      const retry = banner.getByRole("button", { name: "Thử lại" });
      const k = await rel(retry, banner);
      near(k.width, 72.6, "Thử lại.width");
      near(k.height, 44, "Thử lại.height");
      near(b.width - (k.x + k.width), 14, "Thử lại: gap to the right edge");
      near(k.y, 10, "Thử lại.y");
      expect(await style(retry, "border-radius")).toBe("7px");
      expect(await style(retry, "font-size")).toBe("13px");
      expect(await style(retry, "font-weight")).toBe("500");
    });
  }

  test("the banner sits 24 px over the bar and the bar stays where it was (bk-err bar)", async ({
    page,
  }) => {
    const banner = await refuseOnce(page, false);
    const bar = page.getByRole("toolbar", {
      name: "Thao tác với 3 công việc đã chọn",
    });
    const b = await box(banner);
    const t = await box(bar);
    near(t.y - (b.y + b.height), 24, "banner to bar");
    near(900 - (t.y + t.height), 22, "bar to the window edge");
    near(t.height, 44, "bar.height");
    expect(await style(bar, "border-radius")).toBe("12px");
  });

  test("Thử lại can be pressed again: the banner and the pick stay while the server keeps refusing", async ({
    page,
  }) => {
    const banner = await refuseOnce(page, false);
    await banner.getByRole("button", { name: "Thử lại" }).click();
    await expect(banner).toBeVisible();
    await expect(
      page.getByRole("toolbar", { name: "Thao tác với 3 công việc đã chọn" }),
    ).toBeVisible();
  });
});

test.describe("Bulk bar at 390 px", () => {
  test.skip(
    persona !== "owner" || scenario !== "default" || faults !== "",
    "Owner checks need the default owner world without faults",
  );
  test.use({ viewport: { width: 390, height: 844 } });

  for (const dark of [false, true]) {
    test(`every bar target is 44 px, the clear button 44 x 44 (${dark ? "dem" : "sang"})`, async ({
      page,
    }) => {
      await openTable(page, dark);
      await pick(page, ["ERP-142", "ERP-144", "ERP-146"]);
      const bar = page.getByRole("toolbar", {
        name: "Thao tác với 3 công việc đã chọn",
      });
      await expect(bar).toBeVisible();
      const clear = await box(
        bar.getByRole("button", { name: "Bỏ chọn (Esc)" }),
      );
      near(clear.width, 44, "clear.width");
      near(clear.height, 44, "clear.height");
      for (const button of await bar.getByRole("button").all()) {
        const b = await box(button);
        expect(
          b.height,
          "a bar button is at least 44 px high",
        ).toBeGreaterThanOrEqual(43.5);
        expect(
          b.width,
          "a bar button is at least 44 px wide",
        ).toBeGreaterThanOrEqual(43.5);
      }
      const overflow = await page.evaluate(
        () => document.documentElement.scrollWidth - window.innerWidth,
      );
      expect(overflow, "no sideways scroll").toBeLessThanOrEqual(0);
    });
  }
});

test.describe("A viewer's bulk bar (bk-ro)", () => {
  test.skip(
    persona !== "viewer" || scenario !== "default" || faults !== "",
    "Needs VITE_MOCK_PERSONA=viewer",
  );
  test.use({ viewport: { width: 390, height: 844 } });

  test("Assign to and Move to board look disabled and the reason is written on the bar", async ({
    page,
  }) => {
    await openTable(page);
    await pick(page, ["ERP-142"]);
    const bar = page.getByRole("toolbar", {
      name: "Thao tác với 1 công việc đã chọn",
    });
    const reason = "Bạn chỉ xem được board này, không đổi được các việc";
    await expect(bar.getByText(reason, { exact: true })).toBeVisible();
    for (const name of ["Giao cho", "Chuyển board", "Trạng thái", "Đặt hạn"]) {
      const control = bar.getByRole("button", { name: `${name}: ${reason}` });
      await expect(control).toHaveAttribute("aria-disabled", "true");
      expect(await style(control, "opacity")).toBe("0.5");
      near((await box(control)).height, 44, `${name}.height`);
    }
  });
});
