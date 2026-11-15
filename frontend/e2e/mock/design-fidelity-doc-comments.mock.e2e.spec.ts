import { expect, test, type Locator, type Page } from "@playwright/test";

/**
 * The comments tab of the document side panel against the Docs canvas
 * (https://claude.ai/artifact/8yDy2mtUUBgyVHhA2DKAec, v23) screen Doc-Page,
 * state `comments`, 1440x900, light and dark. The numbers come from
 * `Doc-Page.dc.html` rendered with `render.py` (scn=comments vp=w mode=sang|dem)
 * and a box dump of the panel: position, size, font, radius, colour.
 *
 * Boxes are relative to the thread block (origin x 17, y 59 in the board) and
 * to the composer (origin 1, 729.5). The panel header is not measured: the
 * canvas draws a title row with a close button (45 px), the shipped page keeps
 * the Final-Docs-2 tab strip (44 px, see design-fidelity-docs) because the tabs
 * are how the other panels are reached. Text that follows the wording (names,
 * times, body height) lists only the keys that do not. Tolerance 1 px; font
 * size, weight, radius and colour must be equal (the dark quote fill is the
 * mockup rgba(242,193,78,.22) as the oklch token rounds it: 241,191,77).
 */
const DOC = "/workspaces/mock-workspace-primary/docs/mock-doc-invoice-process";
type Key = "x" | "y" | "w" | "h";
interface Entry {
  name: string;
  at: string;
  box: [number, number, number, number];
  keys: string;
  fs?: string;
  fw?: string;
  r?: string;
  light?: { c?: string; bg?: string };
  dark?: { c?: string; bg?: string };
}

const THREAD = "#workspace-main aside [role=tabpanel] > div";
const FIRST = `${THREAD} >> div@0`;
const COMPOSER = "#workspace-main aside > div@-1";

function locate(page: Page, selector: string): Locator {
  let current: Locator | Page = page;
  for (const part of selector.split(" >> ")) {
    const m = /^(.*)@(-?\d+)$/.exec(part);
    current = current
      .locator(m ? (m[1] ?? part) : part)
      .nth(m ? Number(m[2]) : 0);
  }
  return current as Locator;
}

async function colour(l: Locator, property: string): Promise<string> {
  return l.evaluate((el, name) => {
    const c = document.createElement("canvas");
    c.width = c.height = 1;
    const x = c.getContext("2d", { willReadFrequently: true })!;
    x.clearRect(0, 0, 1, 1);
    x.fillStyle = "#000";
    x.fillStyle = getComputedStyle(el).getPropertyValue(name);
    x.fillRect(0, 0, 1, 1);
    const [r, g, b, a] = x.getImageData(0, 0, 1, 1).data;
    return a === 255
      ? `rgb(${r}, ${g}, ${b})`
      : `rgba(${r}, ${g}, ${b}, ${((a ?? 0) / 255).toFixed(2)})`;
  }, property);
}

const css = (l: Locator, p: string) =>
  l.evaluate((el, n) => getComputedStyle(el).getPropertyValue(n).trim(), p);

// prettier-ignore
const THREAD_ENTRIES: Entry[] = [
  { name: "quote", at: `${FIRST} >> button@0`, box: [0, 0, 276, 26.1], keys: "xywh", fs: "12.5px", fw: "400", r: "5px", light: { c: "rgb(39, 45, 54)", bg: "rgb(252, 239, 196)" }, dark: { c: "rgb(211, 216, 224)", bg: "rgba(241, 191, 77, 0.22)" } },
  { name: "row", at: `${FIRST} >> div.group`, box: [0, 34.1, 276, 113.6], keys: "xyw", },
  { name: "avatar", at: `${FIRST} >> div.group > span`, box: [0, 34.1, 28, 28], keys: "xywh", fs: "10px", fw: "700" },
  { name: "head", at: `${FIRST} >> div.group > div.min-w-0 > div:nth-child(1)`, box: [38, 34.1, 238, 18.8], keys: "xywh", fs: "13px", fw: "400" },
  { name: "name", at: `${FIRST} >> div.group > div.min-w-0 > div:nth-child(1) >> b`, box: [38, 34.1, 0, 18.8], keys: "xyh", fs: "13px", fw: "600", light: { c: "rgb(22, 26, 33)" }, dark: { c: "rgb(230, 233, 238)" } },
  { name: "time", at: `${FIRST} >> div.group > div.min-w-0 > div:nth-child(1) >> time`, box: [0, 34.7, 0, 17.4], keys: "h", fs: "12px", fw: "400", light: { c: "rgb(95, 104, 117)" }, dark: { c: "rgb(143, 153, 167)" } },
  { name: "body", at: `${FIRST} >> div.group > div.min-w-0 > p`, box: [38, 55, 238, 62.8], keys: "xyw", fs: "13.5px", fw: "400", light: { c: "rgb(39, 45, 54)" }, dark: { c: "rgb(211, 216, 224)" } },
  { name: "actions", at: `${FIRST} >> div.group > div.min-w-0 > div:nth-child(3)`, box: [38, 0, 238, 26], keys: "xwh" },
  { name: "reply", at: `${FIRST} >> div.group > div.min-w-0 > div:nth-child(3) >> button@0`, box: [38, 0, 52.5, 26], keys: "xwh", fs: "12.5px", fw: "500", r: "6px", light: { c: "rgb(95, 104, 117)" }, dark: { c: "rgb(143, 153, 167)" } },
  { name: "resolve", at: `${FIRST} >> div.group > div.min-w-0 > div:nth-child(3) >> button@1`, box: [92.5, 0, 77.5, 26], keys: "xwh", fs: "12.5px", fw: "500", r: "6px" },
];

// prettier-ignore
const COMPOSER_ENTRIES: Entry[] = [
  { name: "composer", at: COMPOSER, box: [0, 0, 308, 121.5], keys: "xwh" },
  { name: "field", at: `${COMPOSER} >> textarea`, box: [16, 13, 276, 58.5], keys: "xywh", fs: "13.5px", fw: "400", r: "8px", light: { c: "rgb(22, 26, 33)", bg: "rgb(255, 255, 255)" }, dark: { c: "rgb(230, 233, 238)", bg: "rgb(21, 26, 33)" } },
  { name: "send", at: `${COMPOSER} >> button`, box: [244.3, 77.5, 47.7, 32], keys: "xywh", fs: "13px", fw: "600", r: "7px", light: { c: "rgb(255, 255, 255)", bg: "rgb(22, 102, 204)" }, dark: { c: "rgb(4, 17, 31)", bg: "rgb(90, 169, 255)" } },
];

async function check(
  page: Page,
  origin: string,
  entries: Entry[],
  dark: boolean,
) {
  const o = await locate(page, origin).boundingBox();
  if (!o) throw new Error("origin has no box");
  const seen: string[] = [];
  for (const e of entries) {
    const l = locate(page, e.at);
    await expect(l, `${e.name} exists`).toHaveCount(1);
    const b = (await l.boundingBox())!;
    const actual = { x: b.x - o.x, y: b.y - o.y, w: b.width, h: b.height };
    const want = { x: e.box[0], y: e.box[1], w: e.box[2], h: e.box[3] };
    for (const k of e.keys.split("") as Key[])
      if (Math.abs(actual[k] - want[k]) > 1.01)
        seen.push(`${e.name}.${k}: ${actual[k].toFixed(1)} vs ${want[k]}`);
    if (!dark) {
      if (e.fs && (await css(l, "font-size")) !== e.fs)
        seen.push(`${e.name}: font-size`);
      if (e.fw && (await css(l, "font-weight")) !== e.fw)
        seen.push(`${e.name}: font-weight`);
      if (
        e.r &&
        Math.abs(
          parseFloat(await css(l, "border-top-left-radius")) - parseFloat(e.r),
        ) > 0.5
      )
        seen.push(`${e.name}: radius`);
    }
    const s = dark ? e.dark : e.light;
    if (s?.c && (await colour(l, "color")) !== s.c)
      seen.push(`${e.name}: color`);
    if (s?.bg && (await colour(l, "background-color")) !== s.bg)
      seen.push(`${e.name}: background ${await colour(l, "background-color")}`);
  }
  expect(seen, "boxes that differ from the mockup").toEqual([]);
}

for (const dark of [false, true]) {
  test(`doc comments tab at 1440, ${dark ? "dark" : "light"}`, async ({
    page,
  }) => {
    await page.setViewportSize({ width: 1440, height: 900 });
    await page.clock.setFixedTime(new Date("2026-10-12T10:40:00+07:00"));
    await page.addInitScript(
      ({ dark }) => {
        try {
          localStorage.setItem("notrelix-locale", "vi");
          localStorage.setItem("notrelix-system-rail", "pinned");
          if (dark) localStorage.setItem("notrelix-theme-mode", "dark");
        } catch {
          /* defaults apply */
        }
      },
      { dark },
    );
    await page.goto(DOC);
    await expect(page.getByRole("heading", { level: 1 })).toBeVisible();
    await page.getByRole("tab", { name: /^Bình luận/ }).click();
    await page.evaluate(() => document.fonts.ready);
    await check(page, FIRST, THREAD_ENTRIES, dark);
    await check(page, COMPOSER, COMPOSER_ENTRIES, dark);
  });
}
