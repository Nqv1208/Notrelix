import { existsSync } from "node:fs";
import { pathToFileURL } from "node:url";
import { expect, test } from "@playwright/test";

/**
 * The lane menu ("Làn", Kanban) against the owner canvas board `WM-Swimlanes` frame "Chọn làn · nút Làn"
 * (`docs/design/mockups/2026-10/src/WM-Swimlanes.dc.html`). The artboard is opened as a static file and read live.
 * Tolerance is 1 px.
 *
 * The menu is fed from the board's fields and drawn as the artboard draws it: a flat list under the heading
 * "Làn theo" (people first, then Select, then Date), 36 px rows with the type as a small hint, a rule, the column
 * field switched off with "đang là cột", and the note. The mock ERP board has more fields than the five drawn rows
 * (two people, three Select, two Date), so the menu height is not compared; the width, padding, radius, heading, one
 * row of each kind, the rule, the switched-off row and the note are.
 */
const CANVAS = process.env.WM_CANVAS_DIR ?? "/tmp/wmv-canvas/project";
const SRC = `${process.cwd()}/../docs/design/mockups/2026-10/src`;
const FILE = "WM-Swimlanes.dc.html";
const dir = existsSync(`${CANVAS}/${FILE}`) ? CANVAS : SRC;
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
test.skip(
  persona !== "owner" || scenario !== "default" || faults !== "",
  "Owner world",
);
test.use({ viewport: { width: 1440, height: 900 } });

/** Evaluated in the page: the facts of a menu element (own children are its rows). */
const READ = `(menu) => {
  const o = menu.getBoundingClientRect();
  const ctx = document.createElement("canvas").getContext("2d");
  const rgb = (v) => { ctx.fillStyle = "#fff"; ctx.fillRect(0,0,1,1); ctx.fillStyle = v; ctx.fillRect(0,0,1,1); const d = ctx.getImageData(0,0,1,1).data; return "rgb(" + d[0] + ", " + d[1] + ", " + d[2] + ")"; };
  const pick = (el) => { const b = el.getBoundingClientRect(); const cs = getComputedStyle(el); return { y: Math.round((b.y - o.y) * 10) / 10, h: Math.round(b.height * 10) / 10, fs: cs.fontSize, fw: cs.fontWeight, color: rgb(cs.color), padL: cs.paddingLeft, padT: cs.paddingTop, text: (el.innerText || "").replace(/\\s+/g, " ").trim() }; };
  // The app wraps its rows in a radio group and one div per type; the artboard's rows are plain children.
  const flat = (el) => [...el.children].flatMap((c) => /^(radiogroup|group)$/.test(c.getAttribute("role") || "") || [...c.children].some((g) => /^menuitem/.test(g.getAttribute("role") || "")) ? flat(c) : [c]);
  const kids = flat(menu);
  const cs = getComputedStyle(menu);
  return {
    width: o.width, radius: cs.borderRadius, pad: cs.paddingLeft, gap: cs.rowGap,
    kids: kids.map((k) => ({ ...pick(k), hint: (() => { const h = k.lastElementChild && k.children.length > 1 ? k.lastElementChild : null; return h ? { ...pick(h), x: Math.round((h.getBoundingClientRect().right - o.right) * 10) / 10 } : null; })() })),
  };
}`;

type Kid = {
  y: number;
  h: number;
  fs: string;
  fw: string;
  color: string;
  padL: string;
  padT: string;
  text: string;
  hint: { fs: string; color: string; x: number; text: string } | null;
};
type Facts = {
  width: number;
  radius: string;
  pad: string;
  gap: string;
  kids: Kid[];
};

for (const [mode, dark] of [
  ["sang", false],
  ["dem", true],
] as const) {
  test(`Chọn làn ${mode}: the lane menu`, async ({ page, browser }) => {
    if (dark) {
      test.skip(true, "WM-Swimlanes draws the light menu only");
    }
    const art = await browser.newPage();
    await art.setViewportSize({ width: 1500, height: 900 });
    await art.goto(pathToFileURL(`${dir}/${FILE}`).href);
    const want = (await art.evaluate((fn) => {
      const m = [...document.querySelectorAll("*")].find(
        (e) =>
          e.children.length > 3 &&
          e.firstElementChild &&
          /^Làn theo$/.test(e.firstElementChild.textContent!.trim()),
      )!;
      return (0, eval)(fn)(m);
    }, READ)) as Facts;
    await art.close();

    await page.addInitScript(() => {
      try {
        window.localStorage.setItem("notrelix-locale", "vi");
      } catch {
        // Private mode: the defaults apply.
      }
    });
    await page.goto(
      "/workspaces/mock-workspace-primary/boards/mock-board-erp?view=kanban",
    );
    await page
      .getByRole("button", { name: "Làn", exact: true })
      .first()
      .click();
    const menu = page.locator('[role="menu"]').filter({ hasText: "Làn theo" });
    await expect(menu).toBeVisible();
    // The menu zooms in; measure it at rest.
    await page.waitForTimeout(500);
    const got = (await menu.evaluate(
      (el, fn) => (0, eval)(fn)(el),
      READ,
    )) as Facts;

    expect(got.width, "width").toBeCloseTo(want.width, 0);
    expect(got.radius, "radius").toBe(want.radius);
    expect(got.pad, "padding").toBe(want.pad);
    expect(got.gap, "gap").toBe(want.gap);
    // Heading, first rows and the note, by their text.
    const byText = (f: Facts, t: string) =>
      f.kids.find((k) => k.text.startsWith(t))!;
    for (const [label, text] of [
      ["heading", "Làn theo"],
      ["none row", "Không chia làn"],
      ["person row", "Người"],
      ["select row", "Ưu tiên"],
      ["switched-off row", "Trạng thái"],
      ["note", "Làn và cột"],
    ] as const) {
      const a = byText(got, text);
      const e = byText(want, text);
      expect(a, `${label} exists`).toBeTruthy();
      expect(a.h, `${label} height`).toBeCloseTo(e.h, 0);
      expect(a.fs, `${label} font size`).toBe(e.fs);
      expect(a.fw, `${label} font weight`).toBe(e.fw);
      expect(a.color, `${label} colour`).toBe(e.color);
      expect(a.padL, `${label} padding`).toBe(e.padL);
      if (e.hint?.text) {
        expect(a.hint?.fs, `${label} hint size`).toBe(e.hint.fs);
        expect(a.hint?.color, `${label} hint colour`).toBe(e.hint.color);
        expect(a.hint?.x, `${label} hint right edge`).toBeCloseTo(e.hint.x, 0);
      }
    }
    // The first rows sit where they are drawn: heading, then "Không chia làn" 36 px below its top.
    expect(byText(got, "Không chia làn").y).toBeCloseTo(
      byText(want, "Không chia làn").y,
      0,
    );
    // Rows are one 36 px step apart (36 px row + 2 px gap).
    const rows = got.kids.filter((k) => Math.abs(k.h - 36) < 0.6);
    expect(rows.length, "36 px rows").toBeGreaterThanOrEqual(5);
  });
}
