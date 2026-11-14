import { expect, test, type Locator, type Page } from "@playwright/test";

/**
 * The board Calendar at 1180x820 (the touch band) against `Final-T-Calendar`, state `thang`, sidebar `mo`
 * (`render.py Final-T-Calendar.dc.html out.html state=thang sidebar=mo mode=sang|dem`). The numbers were read from
 * the rendered artboard, boxes relative to the bar's (or the tray's) top-left. Tolerance is 1 px.
 *
 * Measured: the 56 px bar (Hôm nay, the two arrows, the range switch, 44 px hit areas), the grid's origin and
 * weekday header, the 91.7 px day columns, and the 256 px tray (title, card, 44 px set-date button, legend).
 *
 * Not compared, each with its reason:
 * - the "Lọc" chip, the saved-filter button and the add button: the app's bar carries the filter, saved-filter and
 *   add controls the artboard puts in its separate 56 px page bar, so the range switch sits further left;
 * - the page bar (`t-top`), which this lane does not rebuild: week-row heights follow the taller grid and are not
 *   compared;
 * - the 40 px month chips (`me`) and the 48x44 "+n" button: the month grid keeps the desktop 24 px chips;
 * - the card's tag chip ("Mua sắm") and status line: the card has no label or status in the undated list;
 * - the artboard's hint wording ("Đặt hạn"): the app's reads "Đặt ngày" (the date field is the board's pick).
 * Dark holds the same boxes, since the artboard's dark render moves colours only.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const NOW = new Date("2026-10-12T10:40:00+07:00");

test.skip(
  persona !== "owner" || scenario !== "default" || faults !== "",
  "Needs the default owner world without faults",
);
test.use({ viewport: { width: 1180, height: 820 } });

interface Rect {
  readonly x: number;
  readonly y: number;
  readonly width: number;
  readonly height: number;
}

async function openCalendar(page: Page, dark = false) {
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
  await page.goto(`${BOARD}?view=calendar`);
  await expect(page.locator("[data-calendar-bar][data-touch]")).toBeVisible();
  await page.waitForTimeout(500);
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

const style = (locator: Locator, property: string) =>
  locator.evaluate(
    (element, name) => getComputedStyle(element).getPropertyValue(name).trim(),
    property,
  );

for (const dark of [false, true]) {
  test(`thang ${dark ? "dark" : "light"}: the 56 px bar of 44 px controls and the grid under it`, async ({
    page,
  }) => {
    await openCalendar(page, dark);
    const bar = page.locator("[data-calendar-bar][data-touch]");
    expectRect(await within(bar, bar), { width: 676, height: 56 }, "bar");

    const today = bar.getByRole("button", { name: "Hôm nay", exact: true });
    expectRect(
      await within(bar, today),
      { x: 16, y: 6, width: 92.2, height: 44 },
      "Hôm nay",
    );
    expect(await style(today, "border-radius")).toBe("10px");
    expect(await style(today, "font-size")).toBe("14.5px");

    const previous = bar.getByRole("button", { name: "Tháng trước" });
    const next = bar.getByRole("button", { name: "Tháng sau" });
    expectRect(
      await within(bar, previous),
      { x: 114, y: 6, width: 44, height: 44 },
      "previous",
    );
    expectRect(
      await within(bar, next),
      { x: 164, y: 6, width: 44, height: 44 },
      "next",
    );

    const heading = bar.getByRole("heading", { level: 2 });
    expect((await within(bar, heading)).x).toBeGreaterThanOrEqual(219);
    expect(await style(heading, "font-weight")).toBe("650");

    const segment = bar.getByRole("radiogroup", { name: "Khoảng hiển thị" });
    const box = await within(bar, segment);
    expectRect(box, { width: 202.6, height: 48 }, "range switch");
    expect(await style(segment, "border-radius")).toBe("12px");
    for (const [name, width] of [
      ["Tháng", 72.9],
      ["Tuần", 61.4],
      ["Ngày", 64.4],
    ] as const) {
      const radio = segment.getByRole("radio", { name });
      expectRect(
        await within(segment, radio),
        { y: 2, width, height: 44 },
        name,
      );
    }

    const grid = page.locator("[data-calendar-view] [role=grid]");
    expectRect(
      await within(bar, grid),
      { x: 16, y: 56, width: 644 },
      "month grid",
    );
    expect(await style(grid, "border-radius")).toBe("12px");
  });

  test(`thang ${dark ? "dark" : "light"}: the 256 px tray, its card and the legend`, async ({
    page,
  }) => {
    await openCalendar(page, dark);
    const bar = page.locator("[data-calendar-bar][data-touch]");
    const tray = page.getByRole("complementary", {
      name: "Công việc chưa có ngày",
    });
    expectRect(await within(bar, tray), { x: 676, y: 0, width: 256 }, "tray");
    expect(await style(tray, "padding-left")).toBe("14px");
    expect(await style(tray, "border-left-width")).toBe("1px");

    const title = tray.getByRole("heading", { name: "1 việc chưa có ngày" });
    expectRect(
      await within(tray, title),
      { x: 15, y: 16, width: 227, height: 22 },
      "title",
    );

    const card = tray.locator("[data-calendar-undated]");
    expect((await within(tray, card)).x).toBe(15);
    expectRect(await within(tray, card), { x: 15, width: 227 }, "card");
    expect(await style(card, "border-radius")).toBe("12px");
    expect(await style(card, "padding-left")).toBe("12px");

    const set = card.getByRole("button", { name: /^Đặt ngày/ });
    expectRect(
      await within(card, set),
      { x: 13, width: 201, height: 44 },
      "set-date button",
    );
    expect(await style(set, "border-radius")).toBe("10px");

    const legend = tray.getByRole("heading", { name: "Chú thích" });
    expect((await within(tray, legend)).x).toBe(15);
    const swatch = tray.locator("span[aria-hidden=true].h-3\\.5").first();
    expectRect(
      await within(tray, swatch),
      { x: 15, width: 24, height: 14 },
      "legend swatch",
    );
  });
}
