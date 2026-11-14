import { execFileSync } from "node:child_process";
import { mkdtempSync } from "node:fs";
import { tmpdir } from "node:os";
import { pathToFileURL } from "node:url";
import { expect, test, type Page } from "@playwright/test";

/**
 * The phone search against `Final-M-Search` (`docs/design/mockups/2026-10/src/`), rendered with `render.py` for
 * `state=ket-qua` and read live, light (sang) and dark (dem), 390 x 844. The device's status bar, the shell's tab bar
 * and the keyboard are not the sheet's, so boxes are read against the top-left of the sheet's header (the artboard's
 * header starts under its 47 px status bar). Compared: the query field, "Huỷ", the first chip and its count font, the
 * result count line, the first group heading, the first result card's row, its icon tile, title and meta line. The
 * mock world has other results than the artboard (7 instead of 6, other titles), so widths of text-sized parts and the
 * list below the first row are not compared; heights, y, x and the fonts are, within 1 px.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";

type Box = readonly [number, number, number, number, string, string];
type Boxes = Record<string, Box | null>;

/** Evaluated in the page: boxes of the named parts against the header's top-left, with the font. */
const PROBE = `(origin, picks) => {
  const o = origin.getBoundingClientRect();
  const out = {};
  for (const [name, el] of Object.entries(picks)) {
    if (!el) { out[name] = null; continue; }
    const b = el.getBoundingClientRect();
    const cs = getComputedStyle(el);
    out[name] = [b.x - o.x, b.y - o.y, b.width, b.height, cs.fontSize, cs.fontWeight];
  }
  return out;
}`;

function render(mode: string): string {
  const dir = mkdtempSync(`${tmpdir()}/wmv-msearch-`);
  const out = `${dir}/search.html`;
  execFileSync("python3", [
    "-I",
    `${process.cwd()}/../docs/design/mockups/2026-10/render.py`,
    "Final-M-Search.dc.html",
    out,
    "state=ket-qua",
    `mode=${mode}`,
  ]);
  return out;
}

async function artBoxes(page: Page, mode: string): Promise<Boxes> {
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto(pathToFileURL(render(mode)).href);
  await page.evaluate(() => document.fonts.ready);
  return page.evaluate((fn) => {
    const q = (s: string, root: ParentNode = document) => root.querySelector(s);
    const head = q("header.shead")!;
    const sec = q("section.sec")!;
    const row = q(".rrow", sec)!;
    return (0, eval)(fn)(head, {
      field: q(".sfield", head),
      cancel: q(".cancel", head),
      chip: q(".chip.on", head),
      chipCount: q(".chip.on .c", head),
      count: q(".rcount"),
      heading: q(".sec-h", sec),
      row,
      tile: q(".ric", row),
      title: q(".rt", row),
      meta: q(".rm", row),
    });
  }, PROBE) as Promise<Boxes>;
}

async function appBoxes(page: Page, mode: string): Promise<Boxes> {
  await page.setViewportSize({ width: 390, height: 844 });
  await page.addInitScript(
    (dark) => {
      try {
        window.localStorage.setItem("notrelix-locale", "vi");
        window.localStorage.setItem("notrelix-theme-mode", dark);
      } catch {
        // Private mode: the defaults apply.
      }
    },
    mode === "dem" ? "dark" : "light",
  );
  await page.goto("/workspaces/mock-workspace-primary/home");
  await page.waitForLoadState("networkidle");
  await expect(async () => {
    await page.keyboard.press("Control+k");
    await expect(page.getByRole("combobox")).toBeVisible({ timeout: 1000 });
  }).toPass({ timeout: 15000 });
  await page.getByRole("combobox").fill("đối chiếu");
  await expect(page.locator('[data-f="r"]').first()).toBeVisible();
  // The dialog's open animation scales it until it ends.
  await expect(page.locator('[data-f="pal"]')).toHaveCSS("transform", "none");
  await page.evaluate(() => document.fonts.ready);
  return page.evaluate((fn) => {
    const q = (s: string, root: ParentNode = document) => root.querySelector(s);
    const head = q('[data-f="pal"] header')!;
    const sec = q('[data-f="pal"] section')!;
    const row = q('[data-f="r"]', sec)!;
    return (0, eval)(fn)(head, {
      field: q('[data-f="q"]', head),
      cancel: q('[data-f="cancel"]', head),
      chip: q('[data-f="fl"][aria-selected="true"]', head),
      chipCount: q('[data-f="fl"][aria-selected="true"] span', head),
      count: q('[data-f="rcount"]'),
      heading: q("h2", sec),
      row,
      tile: q('[data-f="ic"]', row),
      title: q('[data-f="t"]', row),
      meta: q('[data-f="ctx"]', row),
    });
  }, PROBE) as Promise<Boxes>;
}

test.describe("Phone search against Final-M-Search", () => {
  test.skip(
    persona !== "owner" || scenario !== "default" || faults !== "",
    "Owner world",
  );
  for (const mode of ["sang", "dem"] as const)
    test(`results at 390 px, ${mode}`, async ({ page }) => {
      const expected = await artBoxes(page, mode);
      const got = await appBoxes(page, mode);
      console.log("SEARCH", mode, JSON.stringify({ expected, got }));
      // Text-sized widths follow the data (other titles, other counts); x, y, height and the fonts do not.
      const textWide = new Set(["chip", "chipCount", "title", "meta", "count"]);
      for (const [name, e] of Object.entries(expected)) {
        const a = got[name];
        expect(a, `${name} exists`).toBeTruthy();
        if (!e || !a) continue;
        // The result count line and the first row sit under the artboard's own scroll: compared from the header down.
        // A title that wraps onto two lines (the artboard's) moves the meta line down: its y follows the data.
        if (name !== "meta") expect(a[1], `${name} y`).toBeCloseTo(e[1], 0);
        expect(a[0], `${name} x`).toBeCloseTo(e[0], 0);
        if (!["title", "meta", "row"].includes(name))
          expect(a[3], `${name} height`).toBeCloseTo(e[3], 0);
        if (!textWide.has(name))
          expect(a[2], `${name} width`).toBeCloseTo(e[2], 0);
        // The field, the row and the tile are containers: their own font is not drawn.
        if (["field", "row", "tile"].includes(name)) continue;
        expect(a[4], `${name} font size`).toBe(e[4]);
        expect(a[5], `${name} font weight`).toBe(e[5]);
      }
    });
});
