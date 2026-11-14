import { existsSync } from "node:fs";
import { pathToFileURL } from "node:url";
import { expect, test, type Page } from "@playwright/test";

/**
 * Board Kanban state frames against the owner canvas board `BC-Kanban`
 * (`docs/design/mockups/2026-10/src/BC-Kanban.dc.html`), light (sang) and dark (dem). The artboard is opened as a
 * static file and its boxes and computed styles are read live, so no number is copied here. Tolerance is 1 px.
 *
 * Measured (frame `kb-empty`, "Kanban trống: bốn cột trống, có lời nhắc thêm việc", on `mock-board-it-budget`, which
 * holds no item): the plain dashed hint of an empty lane (height, font, colour, border, radius), the lane's header
 * height, the info banner under the lanes (gap to the lanes, height, radius, padding, colours, title and body copy)
 * and the lane header height.
 *
 * Kept deviations, named with their reason:
 * - the artboard's lane has 12 px padding and a 14 px radius (cards 232 px wide); the app's lane keeps the 8 px padding
 *   and 12 px radius `Final-Board` draws (cards 240), which the full-board spec measures, so lane widths and the x of
 *   the hint are not compared;
 * - the artboard's lane has no "Thêm vào …" add row; the app keeps it (quick add is the way a viewer-free board gets its
 *   first item, and the no-group notice hangs on it), so lane heights are not compared;
 * - the artboard's 1182 px frame is narrower than the app's main area, so the banner's width is not compared.
 */
const CANVAS = process.env.WM_CANVAS_DIR ?? "/tmp/wmv-canvas/project";
const SRC = `${process.cwd()}/../docs/design/mockups/2026-10/src`;
const FILE = "BC-Kanban.dc.html";
const dir = existsSync(`${CANVAS}/${FILE}`) ? CANVAS : SRC;
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const BOARDS = "/workspaces/mock-workspace-primary/boards";

test.skip(
  persona !== "owner" || scenario !== "default" || faults !== "",
  "Needs the default owner world without faults",
);
test.use({ viewport: { width: 1440, height: 900 } });

/** Equal strings; rgb colours may differ by 2 per channel (the app writes oklch, the artboard hex). */
function same(got: string | number, want: string | number, label: string) {
  const a = String(got).match(/\d+/g);
  const b = String(want).match(/\d+/g);
  if (String(got).startsWith("rgb(") && a && b) {
    a.forEach((v, i) =>
      expect(
        Math.abs(Number(v) - Number(b[i])),
        `${label}: ${got} vs ${want}`,
      ).toBeLessThanOrEqual(2),
    );
    return;
  }
  expect(got, label).toBe(want);
}

interface Facts {
  laneHeights: number[];
  headerHeight: number;
  hint: Record<string, string | number>;
  note: Record<string, string | number>;
  addRows: number;
}

/** Evaluated in the page, with the selectors of each side. */
const READ = `(sel) => {
  const q = (root, s) => (s ? root.querySelector(s) : null);
  const row = sel.row();
  const lanes = [...row.children].filter((c) => c.matches(sel.lane));
  const ctx = document.createElement("canvas").getContext("2d");
  const px = (v) => {
    ctx.clearRect(0, 0, 1, 1);
    ctx.fillStyle = "#000";
    ctx.fillStyle = v;
    ctx.fillRect(0, 0, 1, 1);
    // Drawn over opaque black, so alpha is read back from a second, white pass.
    const a = [...ctx.getImageData(0, 0, 1, 1).data];
    ctx.clearRect(0, 0, 1, 1);
    ctx.fillStyle = "#fff";
    ctx.fillRect(0, 0, 1, 1);
    ctx.fillStyle = v;
    ctx.fillRect(0, 0, 1, 1);
    const b = [...ctx.getImageData(0, 0, 1, 1).data];
    return { a, b };
  };
  // The colour as painted over the nearest opaque ancestor background.
  const under = (el) => {
    for (let n = el; n; n = n.parentElement) {
      const bg = getComputedStyle(n).backgroundColor;
      const { a, b } = px(bg);
      // Opaque when painting over black and over white agree.
      if (a[0] === b[0] && a[1] === b[1] && a[2] === b[2] && !/^rgba?\(0, 0, 0, 0\)|transparent/.test(bg)) return bg;
    }
    return "#fff";
  };
  const paint = (v, el) => {
    ctx.clearRect(0, 0, 1, 1);
    ctx.fillStyle = "#000";
    ctx.fillStyle = under(el);
    ctx.fillRect(0, 0, 1, 1);
    ctx.fillStyle = v;
    ctx.fillRect(0, 0, 1, 1);
    const d = ctx.getImageData(0, 0, 1, 1).data;
    return "rgb(" + d[0] + ", " + d[1] + ", " + d[2] + ")";
  };
  const rgb = (v, el) => paint(v, el);
  const cs = (el, keys) => Object.fromEntries(keys.map((k) => {
    const v = getComputedStyle(el).getPropertyValue(k);
    return [k, k.includes("color") ? rgb(v, k === "background-color" ? el.parentElement : el) : v];
  }));
  const box = (el) => el.getBoundingClientRect();
  const first = lanes[0];
  const hint = q(first, sel.hint);
  const note = sel.note();
  const hb = box(hint);
  const nb = box(note);
  const title = note.querySelector("b");
  const body = [...note.querySelectorAll("span")].pop();
  return {
    laneHeights: lanes.map((l) => Math.round(box(l).height * 10) / 10),
    headerHeight: box(q(first, sel.header)).height,
    hint: {
      height: hb.height,
      ...cs(hint, ["font-size", "color", "border-top-style", "border-top-color", "border-top-width", "border-radius", "padding-top", "padding-left", "text-align"]),
      text: hint.innerText.replace(/\\s+/g, " ").trim(),
    },
    note: {
      gap: nb.top - Math.max(...lanes.map((l) => box(l).bottom)),
      height: nb.height,
      ...cs(note, ["border-radius", "padding-top", "padding-left", "background-color", "color", "font-size", "column-gap"]),
      titleColor: rgb(getComputedStyle(title).color, title),
      titleWeight: getComputedStyle(title).fontWeight,
      text: note.innerText.replace(/\\s+/g, " ").trim(),
    },
    addRows: lanes.filter((l) => q(l, sel.add)).length,
  };
}`;

async function art(page: Page, frame: string): Promise<Facts> {
  await page.setViewportSize({ width: 1500, height: 900 });
  await page.goto(pathToFileURL(`${dir}/${FILE}`).href);
  await page.waitForSelector(`[data-frame="${frame}"] .lane`);
  return page.evaluate(
    ({ fn, frame: f }) => {
      const fr = document.querySelector(`[data-frame="${f}"]`)!;
      return (0, eval)(fn)({
        row: () => fr.querySelector(".lane")!.parentElement,
        lane: ".lane",
        header: ".lh",
        hint: ".hint",
        add: ".never",
        note: () => fr.querySelector(".bn"),
      });
    },
    { fn: READ, frame },
  ) as Promise<Facts>;
}

async function app(page: Page, dark: boolean): Promise<Facts> {
  await page.setViewportSize({ width: 1440, height: 900 });
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
  await page.goto(`${BOARDS}/mock-board-it-budget?view=kanban`);
  await page.waitForSelector("[data-kanban-empty-note]", { timeout: 20_000 });
  await page.waitForTimeout(400);
  return page.evaluate(
    (fn) =>
      (0, eval)(fn)({
        row: () => document.querySelector("[data-kanban-lanes]"),
        lane: "section",
        header: "div",
        hint: "[data-lane-empty]",
        add: "[data-add-card], form, button[aria-label^='Thêm vào']",
        note: () => document.querySelector("[data-kanban-empty-note]"),
      }),
    READ,
  ) as Promise<Facts>;
}

for (const [mode, dark] of [
  ["sang", false],
  ["dem", true],
] as const) {
  test(`kb-empty ${mode}: lane hint and the empty-board banner`, async ({
    page,
    browser,
  }) => {
    const art_ = await browser.newPage();
    const want = await art(art_, `kb-empty-${mode}`);
    await art_.close();
    const got = await app(page, dark);

    expect(got.laneHeights.length, "four lanes").toBeGreaterThanOrEqual(4);
    expect(got.headerHeight, "lane header height").toBeCloseTo(32, 0);
    // The hint: two centred lines in a 1.5 px dashed box.
    expect(got.hint.height, "hint height").toBeCloseTo(
      Number(want.hint.height),
      0,
    );
    for (const key of [
      "font-size",
      "color",
      "border-top-style",
      "border-top-color",
      "border-radius",
      "padding-top",
      "padding-left",
      "text-align",
      "text",
    ]) {
      same(got.hint[key], want.hint[key], `hint ${key}`);
    }
    // The banner under the lanes.
    expect(Math.abs(Number(got.note.gap) - Number(want.note.gap))).toBeLessThan(
      1.01,
    );
    expect(got.note.height, "banner height").toBeCloseTo(
      Number(want.note.height),
      0,
    );
    for (const key of [
      "border-radius",
      "padding-top",
      "padding-left",
      "background-color",
      "color",
      "font-size",
      "column-gap",
      "titleColor",
      "titleWeight",
      "text",
    ]) {
      same(got.note[key], want.note[key], `banner ${key}`);
    }
  });
}
