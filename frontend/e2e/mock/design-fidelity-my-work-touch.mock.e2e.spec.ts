import { expect, test, type Locator, type Page } from "@playwright/test";

/**
 * My work on a tablet and a phone against `Final-T-MyWork` (1180x820, the 768-1279 band) and `Final-M-MyWork`
 * (390 px). Every number was read from the rendered artboard (`render.py`, boxes relative to the tool row `.t-tool`
 * for the tablet and to `.mw-head` for the phone) and the app's boxes are read relative to the same origin.
 * Tolerance is 1 px. The three groups and the first six rows are compared: the app's mock world and the artboard
 * carry the same items there.
 *
 * Not compared, because the shell owns it or the wording differs: the sidebar and the shell's top bar (the app draws
 * the page title row itself, without the sidebar toggle and the search), the later groups (other items), the phone's
 * subline (the app says "cập nhật" where the artboard says the date), the swipe frame and the split pane.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const PAGE = "/workspaces/mock-workspace-primary/my-work";
const NOW = new Date("2026-10-12T10:40:00+07:00");

test.skip(
  persona !== "owner" || scenario !== "default" || faults !== "",
  "Needs the default owner world without faults",
);

type Box = readonly [x: number, y: number, w: number, h: number];

async function open(page: Page, width: number, height: number, dark = false) {
  await page.setViewportSize({ width, height });
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
  await page.goto(PAGE);
  await expect(
    page.locator('[data-f="row"][data-item-id]').first(),
  ).toBeVisible();
  await page.waitForTimeout(500);
}

async function boxes(origin: Locator, target: Locator, count: number) {
  const o = await origin.boundingBox();
  if (!o) throw new Error("origin has no box");
  const out: Box[] = [];
  const n = Math.min(await target.count(), count);
  for (let i = 0; i < n; i += 1) {
    const b = await target.nth(i).boundingBox();
    if (!b) throw new Error("element has no box");
    out.push([b.x - o.x, b.y - o.y, b.width, b.height]);
  }
  return out;
}

function expectBoxes(
  actual: readonly Box[],
  want: readonly Box[],
  label: string,
) {
  expect(actual.length, `${label}: count`).toBeGreaterThanOrEqual(want.length);
  want.forEach((box, i) => {
    box.forEach((value, k) => {
      expect(
        Math.abs((actual[i] as Box)[k] - value),
        `${label}[${i}].${"xywh"[k]}: ${(actual[i] as Box)[k].toFixed(1)} vs ${value}`,
      ).toBeLessThanOrEqual(1);
    });
  });
}

test.describe("tablet 1180x820 (Final-T-MyWork binh-thuong)", () => {
  test.use({ viewport: { width: 1180, height: 820 } });

  test("tool row, group heads, rows and the approval line", async ({
    page,
  }) => {
    await open(page, 1180, 820);
    const bar = page.locator('[data-f="bar"]');
    const f = (name: string) => page.locator(`[data-f="${name}"]`);

    expectBoxes(await boxes(bar, bar, 1), [[0, 0, 932, 60]], "tool row");
    expectBoxes(
      await boxes(bar, f("tabs"), 1),
      [[16, 6, 560.8, 48]],
      "segment",
    );
    expectBoxes(
      await boxes(bar, f("tab"), 5),
      [
        [18, 8, 84.8, 44],
        [102.8, 8, 124.2, 44],
        [226.9, 8, 129.7, 44],
        [356.6, 8, 136.9, 44],
        [493.5, 8, 81.2, 44],
      ],
      "tabs",
    );
    expectBoxes(
      await boxes(bar, f("pill"), 2),
      [
        [631.6, 8, 197.8, 44],
        [837.4, 8, 78.6, 44],
      ],
      "group and filter chips",
    );
    expectBoxes(
      await boxes(bar, f("tog"), 3),
      [
        [8, 60, 137.6, 44],
        [8, 176.5, 140.1, 44],
        [8, 354.6, 143, 44],
      ],
      "group toggles",
    );
    expectBoxes(
      await boxes(bar, f("rows"), 3),
      [
        [16, 104, 900, 62.5],
        [16, 220.5, 900, 124.1],
        [16, 398.6, 900, 196.8],
      ],
      "group cards",
    );
    expectBoxes(
      await boxes(bar, f("row"), 5),
      [
        [17, 105, 898, 60.5],
        [17, 221.5, 898, 60.5],
        [17, 282.1, 898, 61.5],
        [17, 399.6, 898, 133.2],
        [17, 532.8, 898, 61.5],
      ],
      "rows",
    );
    expectBoxes(
      await boxes(bar, f("tick"), 5),
      [
        [23, 113.3, 44, 44],
        [23, 229.8, 44, 44],
        [23, 291.3, 44, 44],
        [23, 418.2, 44, 44],
        [23, 542.1, 44, 44],
      ],
      "ticks",
    );
    expectBoxes(
      await boxes(bar, f("rt"), 5),
      [
        [71, 114, 378, 20.7],
        [71, 230.5, 378, 20.7],
        [71, 292.1, 378, 20.7],
        [71, 408.6, 378, 41.4],
        [71, 542.8, 378, 20.7],
      ],
      "titles",
    );
    expectBoxes(
      await boxes(bar, f("rc"), 5),
      [
        [71, 137.7, 378, 18.8],
        [71, 254.2, 378, 18.8],
        [71, 315.8, 378, 18.8],
        [71, 453, 378, 18.8],
        [71, 566.5, 378, 18.8],
      ],
      "board lines",
    );
    expectBoxes(
      await boxes(bar, f("why"), 5),
      [
        [463, 121.3, 153.4, 28],
        [463, 237.8, 153.4, 28],
        [463, 299.3, 119, 28],
        [463, 426.2, 127.6, 28],
        [463, 550.1, 119, 28],
      ].map(([x, y, w, h]) => [x, y, w, h] as Box),
      "reason pills",
    );
    expectBoxes(
      await boxes(bar, f("stp"), 5),
      [
        [619, 121.3, 95.6, 28],
        [619, 237.8, 95.6, 28],
        [619, 299.3, 87.2, 28],
        [619, 426.2, 100.9, 28],
        [619, 550.1, 87.2, 28],
      ],
      "status pills",
    );
    expectBoxes(
      await boxes(bar, f("cdu"), 5),
      [
        [737, 119, 128, 32.5],
        [737, 243, 128, 17.5],
        [737, 297.1, 128, 32.5],
        [737, 431.5, 128, 17.5],
        [737, 555.3, 128, 17.5],
      ],
      "due days",
    );
    expectBoxes(
      await boxes(bar, f("more"), 5),
      [
        [865, 113.3, 44, 44],
        [865, 229.8, 44, 44],
        [865, 291.3, 44, 44],
        [865, 418.2, 44, 44],
        [865, 542.1, 44, 44],
      ],
      "row menus",
    );
    expectBoxes(
      await boxes(bar, f("apr"), 1),
      [[69, 474.8, 846, 52]],
      "approval line",
    );
    expectBoxes(
      [
        ...(await boxes(
          bar,
          page.getByRole("button", { name: "Duyệt ERP-138" }),
          1,
        )),
        ...(await boxes(
          bar,
          page.getByRole("button", { name: "Yêu cầu sửa ERP-138" }),
          1,
        )),
      ],
      [
        [685.4, 478.8, 95, 44],
        [788.4, 478.8, 114.6, 44],
      ],
      "approval buttons",
    );
    expectBoxes(
      await boxes(bar, f("foot"), 1),
      [[16, 810.1, 900, 18.8]],
      "foot note",
    );
  });

  test("title row: 56 px, the title 17 px", async ({ page }) => {
    await open(page, 1180, 820);
    const head = page.locator('[data-f="head"]');
    const h = await head.boundingBox();
    expect(h?.height).toBeCloseTo(56, 0);
    const h1 = await page.locator('[data-f="h1"]').boundingBox();
    const sub = await page.locator('[data-f="sub"]').boundingBox();
    expect(Math.abs((h1?.y ?? 0) - (h?.y ?? 0) - 15.2)).toBeLessThanOrEqual(1);
    expect(Math.abs((h1?.height ?? 0) - 24.6)).toBeLessThanOrEqual(1);
    expect(Math.abs((sub?.y ?? 0) - (h?.y ?? 0) - 19.2)).toBeLessThanOrEqual(1);
    expect(Math.abs((sub?.height ?? 0) - 19.6)).toBeLessThanOrEqual(1);
  });
});

test.describe("phone 390 (Final-M-MyWork)", () => {
  test.use({ viewport: { width: 390, height: 844 } });

  test("title, chips, section heads, card rows and the approval buttons", async ({
    page,
  }) => {
    await open(page, 390, 844);
    const head = page.locator('[data-f="head"]');
    const f = (name: string) => page.locator(`[data-f="${name}"]`);

    const h1 = await boxes(head, f("h1"), 1);
    expectBoxes(h1, [[16, 2, 358, 33.6]], "title");
    expectBoxes(
      await boxes(head, f("tab"), 5),
      [
        [16, 76.4, 90.3, 44],
        [114.3, 76.4, 130.3, 44],
        [252.6, 76.4, 113.5, 44],
        [374.2, 76.4, 105.7, 44],
        [487.8, 76.4, 87.2, 44],
      ],
      "chips",
    );
    expectBoxes(
      await boxes(head, f("gh"), 3),
      [
        [16, 138.4, 358, 40],
        [16, 307.6, 358, 40],
        [16, 583, 358, 40],
      ],
      "section heads",
    );
    expectBoxes(
      await boxes(head, f("rows"), 3),
      [
        [16, 182.4, 358, 107.2],
        [16, 351.6, 358, 213.3],
        [16, 627, 358, 286.9],
      ],
      "cards",
    );
    expectBoxes(
      await boxes(head, f("row"), 5),
      [
        [17, 183.4, 356, 105.2],
        [17, 352.6, 356, 105.2],
        [17, 457.8, 356, 106.2],
        [17, 628, 356, 178.7],
        [17, 806.7, 356, 106.2],
      ],
      "rows",
    );
    expectBoxes(
      await boxes(head, f("tick"), 5),
      [
        [19, 189.4, 44, 44],
        [19, 358.6, 44, 44],
        [19, 464.8, 44, 44],
        [19, 634, 44, 44],
        [19, 813.7, 44, 44],
      ],
      "ticks",
    );
    expectBoxes(
      await boxes(head, f("rt"), 5),
      [
        [63, 194.4, 186, 40.8],
        [63, 363.6, 186, 40.8],
        [63, 469.8, 186, 40.8],
        [63, 639, 186, 40.8],
        [63, 818.7, 186, 40.8],
      ],
      "titles",
    );
    expectBoxes(
      await boxes(head, f("rc"), 5),
      [
        [63, 238.2, 186, 18.8],
        [63, 407.4, 186, 18.8],
        [63, 513.6, 186, 18.8],
        [63, 682.7, 186, 18.8],
        [63, 862.5, 186, 18.8],
      ],
      "board lines",
    );
    expectBoxes(
      await boxes(head, f("why"), 5),
      [
        [63, 260.1, 266, 17.5],
        [63, 429.2, 266, 17.5],
        [63, 535.4, 266, 17.5],
        [63, 704.6, 266, 35.1],
        [63, 884.3, 266, 17.5],
      ],
      "reason lines",
    );
    expectBoxes(
      await boxes(head, f("more"), 5),
      [
        [329, 189.4, 44, 44],
        [329, 358.6, 44, 44],
        [329, 464.8, 44, 44],
        [329, 634, 44, 44],
        [329, 813.7, 44, 44],
      ],
      "row menus",
    );
    expectBoxes(
      [
        ...(await boxes(
          head,
          page.getByRole("button", { name: "Duyệt ERP-138" }),
          1,
        )),
        ...(await boxes(
          head,
          page.getByRole("button", { name: "Yêu cầu sửa ERP-138" }),
          1,
        )),
      ],
      [
        [63, 750.7, 75.5, 44],
        [146.5, 750.7, 115.7, 44],
      ],
      "approval buttons",
    );
  });
});
