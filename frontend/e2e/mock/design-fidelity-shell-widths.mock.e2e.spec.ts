import { pathToFileURL } from "node:url";
import { expect, test, type Page } from "@playwright/test";

/**
 * The board shell at 1280, 768 and 320 px against the owner-canvas state boards `BC-Shell-1280-768` and
 * `BC-Shell-320` (`docs/design/mockups/2026-10/src/`), ready state R, light (sang) and dark (dem). The artboard is
 * opened as a static file and its boxes are read live (README, "State boards"), so no number is copied here.
 * Measured within 1 px: the view tab strip (each tab's width, and at 1280 and 768 its x and height relative to the
 * content column), and the board title's font. NOT measured, and different from the artboard: the header and the
 * toolbar rows at 1280 and 768 (the artboard draws, at 768, a 48 px icon rail left of the column, which the app does not have, and a 32 px mark and 32 px buttons from y=16; the app keeps the
 * 34 px controls of `Final-Board`), and the whole 320 px header (the artboard draws a back button and 44 px tab
 * and toolbar targets; the app keeps its breadcrumb row). See the UC-WSP-I01 State line.
 */
const ARTBOARDS = {
  1280: "BC-Shell-1280-768.dc.html",
  768: "BC-Shell-1280-768.dc.html",
  320: "BC-Shell-320.dc.html",
} as const;
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const scenario = process.env.VITE_MOCK_STATE ?? "default";

type Tab = readonly [number, number, number, number];

async function artboardTabs(
  page: Page,
  width: 1280 | 768 | 320,
  mode: "sang" | "dem",
): Promise<Tab[]> {
  // The runner's working directory is frontend/.
  await page.goto(
    pathToFileURL(
      `${process.cwd()}/../docs/design/mockups/2026-10/src/${ARTBOARDS[width]}`,
    ).href,
  );
  return page.evaluate((frame) => {
    const fr = document.querySelector(`[data-frame="${frame}"]`);
    if (!fr) throw new Error(`no frame ${frame}`);
    const strip = fr.querySelector(".tabs");
    if (!strip) throw new Error(`no tab strip in ${frame}`);
    // Boxes are relative to the content column (the strip's own box), so the 768 px frame's rail drops out.
    const o = strip.getBoundingClientRect();
    return [...strip.querySelectorAll('[role="tab"]')].map((e) => {
      const q = e.getBoundingClientRect();
      return [q.x - o.x, q.y - o.y, q.width, q.height] as const;
    });
  }, `sh${width}-R-${mode}`);
}

for (const width of [1280, 768, 320] as const) {
  for (const mode of ["sang", "dem"] as const) {
    test(`tab strip at ${width} px, ${mode}, equals the shell state board`, async ({
      page,
    }) => {
      test.skip(scenario !== "default", "Needs the default world data");
      await page.setViewportSize({ width, height: 900 });
      const expected = await artboardTabs(page, width, mode);
      expect(expected.length).toBeGreaterThanOrEqual(4);

      await page.addInitScript((dark) => {
        try {
          window.localStorage.setItem("notrelix-locale", "vi");
          if (dark) window.localStorage.setItem("notrelix-theme-mode", "dark");
        } catch {
          // Private mode: defaults apply.
        }
      }, mode === "dem");
      await page.goto(BOARD);
      await page.getByRole("tab", { name: "Bảng" }).click();
      await expect(
        page.getByRole("heading", { level: 1, name: /Triển khai ERP Q4/ }),
      ).toBeVisible();
      await page.waitForTimeout(400);

      const main = (await page.locator("main").boundingBox())!;
      const tabs = page.getByRole("tab");
      for (let i = 0; i < 4; i++) {
        const b = (await tabs.nth(i).boundingBox())!;
        expect(b.width, `tab ${i} width`).toBeCloseTo(expected[i]![2], 0);
        if (width !== 320) {
          expect(b.x - main.x, `tab ${i} x`).toBeCloseTo(expected[i]![0], 0);
          expect(b.height, `tab ${i} height`).toBeCloseTo(expected[i]![3], 0);
        }
      }
      expect(
        await page.evaluate(
          () => document.documentElement.scrollWidth <= window.innerWidth,
        ),
        "no horizontal page scroll",
      ).toBe(true);
    });
  }
}
