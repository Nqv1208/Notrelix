import { expect, test, type Page } from "@playwright/test";

/**
 * The Table, Calendar and Timeline bars at 1440, 1280 and 1024 against the owner canvas boards
 * `WM-Toolbar-Calendar` and `WM-Toolbar-Timeline` (levels Vừa, Chặt, Hẹp) and `BC-Table` (the 1280 frame).
 * Numbers were read from the boards rendered with `docs/design/mockups/2026-10/render.py` and measured in a
 * browser at the board's own width: the bar is one 52 px row (10 px padding, 32 px controls) at every level, the
 * range segment is 182 px (Calendar) and 176 px (Timeline), "Hôm nay" 82 px, the arrows 32 px, "Vừa khít" 79 px at
 * Vừa and Chặt, and at Hẹp the Calendar heading is 134 px and "Thêm công việc", "Vừa khít", "Phụ thuộc" and
 * "Đường găng" are 32 px icon buttons. Tolerance is 1 px. The Table has no frame at 1024 in the canvas: it must
 * hold the one-row bar of its 1280 frame, so the same 52 px row is the target.
 *
 * Not compared: the saved-filter and filter widths (the boards draw the filter chip with its count), the
 * Timeline "Lọc" (the boards leave it out; the app keeps it as an icon), the toggle height (30 px in the app's
 * Final-Timeline measurement, 32 px in the board) and every text width. The date-field and grouping lists are
 * undrawn states built from the design system's menu.
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

type View = "table" | "calendar" | "timeline";
const BAR: Record<View, string> = {
  table: "[data-table-toolbar]",
  calendar: "[data-calendar-bar]",
  timeline: "[data-tl=bar]",
};

async function open(page: Page, view: View, width: number, dark: boolean) {
  await page.setViewportSize({ width, height: 900 });
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
  await page.goto(`${BOARD}?view=${view}`);
  await expect(page.locator(BAR[view])).toBeVisible();
  await page.waitForTimeout(500);
}

/** The box of the first control whose accessible name starts with `name`, inside the bar. */
async function box(page: Page, view: View, name: RegExp) {
  const target = page.locator(BAR[view]).getByRole("button", { name }).first();
  const b = await target.boundingBox();
  if (!b) throw new Error(`no control ${name}`);
  return b;
}

const CASES = [
  { width: 1440, level: "vua" },
  { width: 1280, level: "chat" },
  { width: 1024, level: "hep" },
] as const;

for (const dark of [false, true]) {
  for (const { width, level } of CASES) {
    for (const view of ["table", "calendar", "timeline"] as const) {
      test(`${view} bar at ${width} (${level}, ${dark ? "dark" : "light"}) is one 52 px row`, async ({
        page,
      }) => {
        await open(page, view, width, dark);
        const bar = await page.locator(BAR[view]).boundingBox();
        expect(bar?.height).toBeCloseTo(52, 0);
        const add = await box(page, view, /^Thêm công việc/);
        expect(add.height).toBeCloseTo(32, 0);
        if (view !== "table" && level === "hep")
          expect(add.width).toBeCloseTo(32, 0);
        else expect(add.width).toBeCloseTo(150, 0);
        // The add button is the right-most control and stays inside the bar.
        expect(add.x + add.width).toBeLessThanOrEqual(
          (bar?.x ?? 0) + (bar?.width ?? 0) - (view === "table" ? 0 : 24) + 1,
        );
        if (view === "calendar") {
          const today = await box(page, view, /^Hôm nay/);
          expect(today.width).toBeCloseTo(82, 0);
          const segment = await page
            .locator(BAR[view])
            .getByRole("radiogroup")
            .boundingBox();
          expect(segment?.width).toBeCloseTo(182, 0);
          expect(segment?.height).toBeCloseTo(32, 0);
          if (level === "vua") {
            const basis = await box(page, view, /^Xếp theo/);
            expect(basis.width).toBeCloseTo(156, 0);
          } else {
            await expect(
              page
                .locator(BAR[view])
                .getByRole("button", { name: "Thêm", exact: true }),
            ).toBeVisible();
            const more = await box(page, view, /^Thêm$/);
            expect(more.width).toBeCloseTo(32, 0);
          }
          if (level === "hep") {
            const heading = await page
              .locator(BAR[view])
              .getByRole("heading")
              .boundingBox();
            expect(heading?.width).toBeCloseTo(134, 0);
          }
        }
        if (view === "timeline") {
          const segment = await page
            .locator(BAR[view])
            .getByRole("radiogroup")
            .boundingBox();
          expect(segment?.width).toBeCloseTo(176, 0);
          const more = await box(page, view, /^Thêm$/);
          expect(more.width).toBeCloseTo(32, 0);
          const fit = await box(page, view, /^Vừa khít/);
          expect(fit.width).toBeCloseTo(level === "hep" ? 32 : 79, 0);
          if (level === "hep") {
            const links = await box(page, view, /^Phụ thuộc/);
            expect(links.width).toBeCloseTo(32, 0);
          }
        }
      });
    }
  }
}
