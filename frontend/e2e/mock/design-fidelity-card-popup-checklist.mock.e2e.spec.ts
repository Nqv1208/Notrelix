import { existsSync } from "node:fs";
import { pathToFileURL } from "node:url";
import { expect, test, type Page } from "@playwright/test";

/**
 * The Checklist section of the card popup against the owner canvas board `Kanban-CardPopup-Checklist-People`, the
 * section "Chuẩn bị dữ liệu tháng 9" (first state, 80%), on ERP-144 of the mock world. The artboard is opened as a
 * static file and every box of the section (the heading row, the title, the two buttons, the percentage and the bar,
 * each row with its checkbox and text, the add button) is read live; the app's boxes must match within 1 px, with
 * the font size and weight of each. The icon glyph and the rows' hover actions (hidden until hover) are not compared.
 */
const CANVAS = `${process.cwd()}/../docs/design/mockups/2026-10/src`;
const FILE = "Kanban-CardPopup-Checklist-People.dc.html";
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
test.skip(
  !existsSync(`${CANVAS}/${FILE}`) ||
    persona !== "owner" ||
    scenario !== "default" ||
    faults !== "",
  "Needs the default owner world",
);
test.use({ viewport: { width: 1440, height: 1000 } });

/** Any CSS colour as the opaque rgb channels it paints over white. */
const rgb = (page: Page, value: string) =>
  page.evaluate((v) => {
    const c = document.createElement("canvas").getContext("2d")!;
    c.fillStyle = "#fff";
    c.fillRect(0, 0, 1, 1);
    c.fillStyle = v;
    c.fillRect(0, 0, 1, 1);
    return [...c.getImageData(0, 0, 1, 1).data].slice(0, 3);
  }, value);

interface Box {
  tag: string;
  x: number;
  y: number;
  w: number;
  h: number;
  fs: string;
  fw: string;
  c: string;
  bg: string;
  text: string;
}

/** Evaluated in the page: the visible boxes of a section relative to it, without svg parts and hidden actions. */
const READ = `(root) => {
  const o = root.getBoundingClientRect();
  const out = [];
  const walk = (e) => {
    for (const c of e.children) {
      const r = c.getBoundingClientRect();
      if (c.tagName === "svg" || r.width === 0 || r.height === 0) continue;
      const cs = getComputedStyle(c);
      // The app wraps the rows in one plain div; the artboard's rows sit in the section directly.
      if (c.tagName === "DIV" && c.children.length > 1 && [...c.children].every((k) => k.tagName === "DIV")) { walk(c); continue; }
      out.push({ tag: c.tagName, x: r.x - o.x, y: r.y - o.y, w: r.width, h: r.height, fs: cs.fontSize, fw: cs.fontWeight, c: cs.color, bg: cs.backgroundColor, text: c.children.length ? "" : c.textContent.trim().slice(0, 30) });
      walk(c);
    }
  };
  out.push({ tag: "SEC", x: 0, y: 0, w: o.width, h: o.height, fs: "", fw: "", c: "", bg: "", text: "" });
  walk(root);
  return out;
}`;

for (const [mode, dark] of [
  ["light", false],
  ["dark", true],
] as const)
  test(`checklist section of the card popup, ${mode}`, async ({
    page,
    browser,
  }) => {
    const art = await browser.newPage();
    await art.setViewportSize({ width: 1500, height: 1000 });
    await art.goto(pathToFileURL(`${CANVAS}/${FILE}`).href);
    // The artboard's own dark rules (mode-dem) give the dark section.
    if (dark)
      await art.evaluate(() => {
        const root = document.querySelector("[class*='mode-']")!;
        root.classList.add("mode-dem");
      });
    const want = (await art.evaluate((fn) => {
      const h = [...document.querySelectorAll("h3")].find(
        (e) => e.textContent?.trim() === "Chuẩn bị dữ liệu tháng 9",
      )!;
      let n: HTMLElement = h;
      while (n.parentElement && n.querySelectorAll(".cli").length === 0)
        n = n.parentElement;
      return (0, eval)(fn)(n);
    }, READ)) as Box[];
    await art.close();

    await page.addInitScript((m) => {
      try {
        window.localStorage.setItem("notrelix-locale", "vi");
        window.localStorage.setItem("notrelix-theme-mode", m);
      } catch {
        // Private mode: the defaults apply.
      }
    }, mode);
    await page.goto(
      "/workspaces/mock-workspace-primary/boards/mock-board-erp?view=kanban&item=mock-erp-item-144",
    );
    const title = page.getByRole("heading", {
      name: "Chuẩn bị dữ liệu tháng 9",
    });
    await expect(title).toBeVisible({ timeout: 20_000 });
    await page.waitForTimeout(500);
    const got = (await title.evaluate((h, fn) => {
      let n: HTMLElement = h as HTMLElement;
      while (n.parentElement && n.tagName !== "SECTION") n = n.parentElement;
      return (0, eval)(fn)(n);
    }, READ)) as Box[];

    const sig = (b: Box[]) =>
      b.map((x) => `${x.tag}:${x.text}:${Math.round(x.y)}`);
    expect(sig(got), "boxes").toEqual(sig(want));
    for (const [i, e] of want.entries()) {
      const a = got[i]!;
      const label = `${i} ${e.tag} "${e.text}"`;
      expect(a.tag, `${label} tag`).toBe(e.tag);
      // The icon tile is an inline span on the artboard (a line high) and a 20 px box in the app: the glyph is 20 px in both.
      for (const k of i === 1
        ? (["x", "w"] as const)
        : (["x", "y", "w", "h"] as const))
        expect(
          Math.abs(a[k] - e[k]),
          `${label} ${k}: ${a[k]} vs ${e[k]}`,
        ).toBeLessThanOrEqual(1);
      expect(a.fs, `${label} font size`).toBe(e.fs);
      expect(a.fw, `${label} font weight`).toBe(e.fw);
      expect(a.text, `${label} text`).toBe(e.text);
      // Colours: the artboard writes hex, the app a token; both as rgb, each channel within 2.
      // Kept deviation (dark): the artboard paints the done checkbox's tick white on the light accent (2.4:1);
      // the app uses the accent's ink token, so the tick colour of the checkboxes is not compared in dark.
      const tick = dark && e.tag === "BUTTON" && e.text === "";
      for (const k of e.c
        ? tick
          ? (["bg"] as const)
          : (["c", "bg"] as const)
        : []) {
        const [g, w] = await Promise.all([rgb(page, a[k]), rgb(page, e[k])]);
        g.forEach((v, n) =>
          expect(
            Math.abs(v - w[n]!),
            `${label} ${k}: ${a[k]} vs ${e[k]}`,
          ).toBeLessThanOrEqual(2),
        );
      }
    }
  });
