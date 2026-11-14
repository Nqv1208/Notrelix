import { existsSync } from "node:fs";
import { pathToFileURL } from "node:url";
import { expect, test, type Page } from "@playwright/test";

/**
 * The saved-filter menu's states against the owner-canvas board `WM-SavedFilter-States` (owner artifact
 * 18L4jUXGfdXh7Wn9YQvA2x), read live from `WM_CANVAS_DIR`: "Chưa có bộ lọc nào" (empty), "Đang tải" (two 40 px bars) and
 * "Tải lỗi" (the failure line and "Thử lại"). Boxes are relative to the menu's own top-left, within 1 px, fonts equal.
 * The artboard tile is a grid cell (434 px wide) and the app's menu is a 380 px popover, so the menu's width and the
 * x of centred text are not compared; heights, y and the fonts are.
 *
 * Worlds (the spec skips the others): default owner for the empty menu on `mock-board-accounting`;
 * `VITE_MOCK_LATENCY=slow` for the loading bars; `VITE_MOCK_FAULTS=/boards/mock-board-accounting/saved-filters=network`
 * for the load error.
 */
const CANVAS = process.env.WM_CANVAS_DIR ?? "/tmp/wmv-canvas/project";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const latency = process.env.VITE_MOCK_LATENCY ?? "";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-accounting";
const FAULT = "/boards/mock-board-accounting/saved-filters=network";

type Probe = { box: number[]; fs: string; fw: string };

async function probe(
  page: Page,
  rootSel: string,
  sels: Record<string, string>,
): Promise<Record<string, Probe>> {
  return page.evaluate(
    ({ rootSel: r, sels: s }) => {
      const root = document.querySelector(r);
      if (!root) throw new Error(`no root ${r}`);
      const o = root.getBoundingClientRect();
      const out: Record<string, Probe> = {};
      for (const [key, sel] of Object.entries(s)) {
        const el = key === "root" ? root : root.querySelector(sel);
        if (!el) continue;
        const b = el.getBoundingClientRect();
        const cs = getComputedStyle(el);
        out[key] = {
          box: [b.x - o.x, b.y - o.y, b.width, b.height],
          fs: cs.fontSize,
          fw: cs.fontWeight,
        };
      }
      return out;
    },
    { rootSel, sels },
  );
}

function show(
  label: string,
  app: Record<string, Probe>,
  art: Record<string, Probe>,
) {
  console.log(label, JSON.stringify({ art, app }));
}

function same(
  app: Record<string, Probe>,
  art: Record<string, Probe>,
  skip: Record<string, string> = {},
) {
  for (const key of Object.keys(art)) {
    const a = app[key];
    expect(a, `${key} exists in the app`).toBeDefined();
    const skipped = skip[key] ?? "";
    (["x", "y", "w", "h"] as const).forEach((axis, i) => {
      if (skipped.includes(axis)) return;
      expect(
        Math.abs(a!.box[i]! - art[key]!.box[i]!),
        `${key}.${axis}: ${a!.box[i]} vs ${art[key]!.box[i]}`,
      ).toBeLessThanOrEqual(1);
    });
    if (!skipped.includes("f")) {
      expect(a!.fs, `${key} font size`).toBe(art[key]!.fs);
      expect(a!.fw, `${key} font weight`).toBe(art[key]!.fw);
    }
  }
}

async function artboard(
  page: Page,
  selector: string,
  sels: Record<string, string>,
) {
  await page.goto(
    pathToFileURL(`${CANVAS}/WM-SavedFilter-States.dc.html`).href,
  );
  await page.evaluate(() => document.fonts.ready);
  return probe(page, selector, sels);
}

async function openMenu(page: Page) {
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "vi");
    } catch {
      // Private mode: the default locale applies.
    }
  });
  await page.goto(`${BOARD}?view=table`);
  await page.getByRole("button", { name: /Bộ lọc đã lưu/ }).click();
  const menu = page.locator('[aria-label="Bộ lọc đã lưu"][role="dialog"]');
  await expect(menu).toBeVisible();
  if (faults !== "") await expect(menu.getByRole("alert")).toBeVisible();
  await page.waitForTimeout(500);
  await menu.evaluate((el) => el.setAttribute("data-fid", "menu"));
}

test.describe("Saved-filter menu states against the canvas", () => {
  test.skip(
    persona !== "owner" ||
      scenario !== "default" ||
      !existsSync(`${CANVAS}/WM-SavedFilter-States.dc.html`),
    "Needs the default owner world and the downloaded canvas boards",
  );
  test.use({ viewport: { width: 1440, height: 1000 } });

  test("the empty menu equals the empty tile", async ({ page }) => {
    test.skip(latency === "slow" || faults !== "", "Plain world");
    const art = await artboard(
      page,
      '.menu[aria-label="Bộ lọc đã lưu, trống"]',
      {
        root: "",
        title: "b",
        hint: "div > span",
      },
    );
    await openMenu(page);
    const app = await probe(page, '[data-fid="menu"]', {
      root: "",
      title: "b",
      hint: "b + span",
    });
    show("EMPTY", app, art);
    same(app, art, { root: "w", title: "xw", hint: "xw" });
  });

  test("the loading menu equals the loading tile", async ({ page }) => {
    test.skip(latency !== "slow", "Needs VITE_MOCK_LATENCY=slow");
    const art = await artboard(page, '[role="status"].menu', {
      root: "",
      bar1: "div:nth-child(1)",
      bar2: "div:nth-child(2)",
    });
    await page.addInitScript(() => {
      try {
        window.localStorage.setItem("notrelix-locale", "vi");
      } catch {
        // Private mode: the default locale applies.
      }
    });
    await page.goto(`${BOARD}?view=table`);
    await expect(page.getByRole("rowgroup").first()).toBeVisible();
    await page.getByRole("button", { name: /Bộ lọc đã lưu/ }).click();
    const status = page.locator(
      '[role="status"][aria-label="Đang tải bộ lọc đã lưu"]',
    );
    await expect(status).toBeVisible();
    const menu = status.locator("xpath=..");
    // The popover scales in until its animation ends.
    await expect(menu).toHaveCSS("transform", "none");
    await menu.evaluate((el) => el.setAttribute("data-fid", "menu"));
    const app = await probe(page, '[data-fid="menu"]', {
      root: "",
      bar1: "[role=status] > div:nth-child(1)",
      bar2: "[role=status] > div:nth-child(2)",
    });
    show("LOADING", app, art);
    // The popover's 6 px padding and the status's 4 px sit around the bars; the bars' own height and gap are compared.
    expect(app.bar1!.box[3]).toBeCloseTo(art.bar1!.box[3]!, 0);
    expect(app.bar2!.box[1]! - app.bar1!.box[1]!).toBeCloseTo(
      art.bar2!.box[1]! - art.bar1!.box[1]!,
      0,
    );
  });

  test("the load error equals the error tile", async ({ page }) => {
    test.skip(!faults.includes(FAULT), "Needs the saved-filters fault");
    const art = await artboard(page, '.menu[role="alert"]', {
      root: "",
      line: "div > span",
      retry: "button",
    });
    await openMenu(page);
    const app = await probe(page, '[data-fid="menu"]', {
      root: "",
      line: "p",
      retry: "button",
    });
    show("ERROR", app, art);
    same(app, art, { root: "w", line: "w", retry: "w" });
  });
});
