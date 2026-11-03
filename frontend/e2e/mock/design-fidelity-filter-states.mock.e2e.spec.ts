import { existsSync } from "node:fs";
import { pathToFileURL } from "node:url";
import { expect, test, type Page } from "@playwright/test";

/**
 * Filter states against the owner-canvas board `WM-Filter-States` (read live from `WM_CANVAS_DIR`): the card of a
 * filter that keeps no item ("Đang lọc mà không có kết quả": filter icon, title, the count line and the two buttons
 * "Sửa điều kiện" and "Xoá bộ lọc"). Boxes are relative to the card's own top-left, within 1 px, fonts equal. The
 * artboard tile is a grid cell (434 px) and the app's card is at most 436 px and centred in the board, so the card's
 * x and width are not compared; heights, y and the fonts are, and each child's x relative to the card's centre line.
 *
 * Not measured here: the empty swimlane note (needs a personal swimlane state, covered by the component test of the
 * lane), the stale-field menu row and warning box (need a saved filter on a removed field; the row has a component
 * test and the box is the one of `ViewStateNotices`), the viewer lock tip (see the tooltip note in the lane report).
 */
const CANVAS = process.env.WM_CANVAS_DIR ?? "/tmp/wmv-canvas/project";
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const NOW = new Date("2026-10-30T05:00:00+07:00");

type Probe = { box: number[]; fs: string; fw: string };

async function probe(page: Page, root: string, sels: Record<string, string>) {
  return page.evaluate(
    ({ root: r, sels: s }) => {
      const rootEl = document.querySelector(r);
      if (!rootEl) throw new Error(`no root ${r}`);
      const o = rootEl.getBoundingClientRect();
      const out: Record<string, Probe> = {};
      for (const [key, sel] of Object.entries(s)) {
        const el = key === "root" ? rootEl : rootEl.querySelector(sel);
        if (!el) continue;
        const b = el.getBoundingClientRect();
        const cs = getComputedStyle(el);
        out[key] = {
          box: [
            b.x - o.x + b.width / 2 - o.width / 2,
            b.y - o.y,
            b.width,
            b.height,
          ],
          fs: cs.fontSize,
          fw: cs.fontWeight,
        };
      }
      return out;
    },
    { root, sels },
  );
}

test.describe("Filter states against the canvas", () => {
  test.skip(
    persona !== "owner" ||
      scenario !== "default" ||
      faults !== "" ||
      !existsSync(`${CANVAS}/WM-Filter-States.dc.html`),
    "Needs the default owner world and the downloaded canvas boards",
  );
  test.use({ viewport: { width: 1440, height: 1000 } });

  test("the no-result card equals the tile", async ({ page }) => {
    await page.goto(pathToFileURL(`${CANVAS}/WM-Filter-States.dc.html`).href);
    await page.evaluate(() => document.fonts.ready);
    // Mark the tile's card, then read it (centre x relative to the card's centre line).
    await page.evaluate(() => {
      const card = [...document.querySelectorAll(".card")].find((c) =>
        c.textContent?.includes("Không có việc nào khớp bộ lọc"),
      );
      card?.setAttribute("data-fid", "tile");
    });
    const art = await probe(page, '[data-fid="tile"]', {
      root: "",
      icon: "span > svg",
      title: "b",
      note: ".note",
      buttons: "div",
      edit: "button:nth-child(1)",
      clear: "button:nth-child(2)",
    });

    await page.clock.setFixedTime(NOW);
    await page.addInitScript(() => {
      try {
        window.localStorage.setItem("notrelix-locale", "vi");
      } catch {
        // Private mode: the default locale applies.
      }
    });
    await page.goto(`${BOARD}?view=kanban&filter=p.low~o`);
    await expect(page.locator("[data-filter-no-results]")).toBeVisible();
    await page.waitForTimeout(400);
    const app = await probe(page, "[data-filter-no-results]", {
      root: "",
      icon: "svg",
      title: "b",
      note: "span:not([aria-hidden])",
      buttons: "div",
      edit: "button:nth-child(1)",
      clear: "button:nth-child(2)",
    });
    console.log("NORESULT", JSON.stringify({ art, app }));

    for (const key of Object.keys(art)) {
      const a = app[key];
      expect(a, `${key} exists in the app`).toBeDefined();
      (["cx", "y", "w", "h"] as const).forEach((axis, i) => {
        if (key === "root" && (axis === "cx" || axis === "w")) return;
        // The count line carries the board's own numbers (the tile says 3 and 24).
        if (key === "note" && axis === "w") return;
        expect(
          Math.abs(a!.box[i]! - art[key]!.box[i]!),
          `${key}.${axis}: ${a!.box[i]} vs ${art[key]!.box[i]}`,
        ).toBeLessThanOrEqual(1);
      });
      if (key !== "icon" && key !== "buttons" && key !== "root") {
        expect(a!.fs, `${key} font size`).toBe(art[key]!.fs);
        expect(a!.fw, `${key} font weight`).toBe(art[key]!.fw);
      }
    }
  });
});
