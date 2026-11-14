import { expect, test, type Locator, type Page } from "@playwright/test";

/**
 * Document page geometry against the October 2026 mockup. The normal state
 * (light `NORMAL`, dark `DARK`) comes from
 * `docs/design/mockups/2026-10/src/Final-Docs-2.dc.html` and
 * `Final-Docs-2-Dem.dc.html`, the boards drawn from the shipped page, rendered
 * at 1440x1340 with `render.py` and measured with the box dump: position, size,
 * padding, gap, font, radius and colour of each element, relative to the main
 * area's top-left (rail pinned). The comment after each entry names the board
 * element it was read from. Entries the shipped drawing does not contain (a
 * status chip, a font select, a size box, a mode select, list items, a locked
 * side-panel row) were removed. Tolerance is 1 px for boxes; font sizes,
 * weights, radii and colours must be equal.
 *
 * Only the states Final-Docs-2 draws are measured here (normal, light and dark).
 * Menus, dialogs, tabs and the save error have no board: they are not measured
 * here. The hovered
 * second checklist line and the first table row's status and due date follow the
 * mock data (the artboard draws a hovered line, an "Đang làm" row and an overdue
 * date), so the status pill is measured without width or colour and the due date
 * without weight or colour.
 *
 * Text differs between the mock world and the mockup world, so an entry whose
 * width or position follows its wording lists only the keys that do not
 * (`keys`: x, y, w, h). The "why you see this page" line shows only to a workspace
 * admin who is not a department member (26 px); the spec accounts for it.
 *
 * The page clock is fixed to the fixtures' 12/10/2026 10:40.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const DOC = "/workspaces/mock-workspace-primary/docs/mock-doc-invoice-process";
const NOW = new Date("2026-10-12T10:40:00+07:00");
const RAIL_KEY = "notrelix-system-rail";
const THEME_KEY = "notrelix-theme-mode";
/** Height of the "why you see this page" line and its gap in the mockup. */
const WHY_LINE = 26.1;

type Key = "x" | "y" | "w" | "h";

interface Style {
  fs?: string;
  fw?: string;
  c?: string;
  r?: string;
  bg?: string;
}

interface Entry {
  name: string;
  /** CSS; `@i` picks the i-th match (-1 = last), ` >> ` chains a descendant. */
  at: string;
  /** x, y, width, height in px relative to the origin of the state. */
  box: [number, number, number, number];
  keys: string;
  style: Style;
}

/** `sel@i >> child@j` as a Playwright locator. */
function locate(page: Page, selector: string): Locator {
  let current: Locator | Page = page;
  for (const part of selector.split(" >> ")) {
    const match = /^(.*)@(-?\d+)$/.exec(part);
    const css = match ? (match[1] ?? part) : part;
    const index = match ? Number(match[2]) : 0;
    current = current.locator(css).nth(index);
  }
  return current as Locator;
}

async function openDoc(
  page: Page,
  options: { rail?: "pinned"; dark?: boolean } = {},
) {
  await page.clock.setFixedTime(NOW);
  await page.addInitScript(
    ({ rail, dark, railKey, themeKey }) => {
      try {
        window.localStorage.setItem("notrelix-locale", "vi");
        if (rail) window.localStorage.setItem(railKey, rail);
        if (dark) window.localStorage.setItem(themeKey, "dark");
      } catch {
        // Private mode: the defaults apply.
      }
    },
    {
      rail: options.rail,
      dark: options.dark,
      railKey: RAIL_KEY,
      themeKey: THEME_KEY,
    },
  );
  await page.goto(DOC);
  await expect(page.getByRole("heading", { level: 1 })).toBeVisible();
  await expect(page.getByRole("table")).toBeVisible();
  // Fonts settle before any text box is measured.
  await page.evaluate(() => document.fonts.ready);
}

async function mainOrigin(page: Page): Promise<{ x: number; y: number }> {
  const box = await page.locator("main").boundingBox();
  if (!box) throw new Error("main area has no box");
  return box;
}

/** A colour property as `rgb(r, g, b)` / `rgba(r, g, b, a)`, whatever the colour space. */
async function colour(locator: Locator, property: string): Promise<string> {
  return locator.evaluate((element, name) => {
    const canvas = document.createElement("canvas");
    canvas.width = canvas.height = 1;
    const context = canvas.getContext("2d", { willReadFrequently: true })!;
    context.clearRect(0, 0, 1, 1);
    context.fillStyle = "#000";
    context.fillStyle = getComputedStyle(element).getPropertyValue(name);
    context.fillRect(0, 0, 1, 1);
    const [r, g, b, a] = context.getImageData(0, 0, 1, 1).data;
    return a === 255
      ? `rgb(${r}, ${g}, ${b})`
      : `rgba(${r}, ${g}, ${b}, ${((a ?? 0) / 255).toFixed(2)})`;
  }, property);
}

async function style(locator: Locator, property: string): Promise<string> {
  return locator.evaluate(
    (element, name) => getComputedStyle(element).getPropertyValue(name).trim(),
    property,
  );
}

interface CheckOptions {
  /** Where the box origin is: the main area, the viewport or an anchor element. */
  origin?: "main" | "viewport" | { anchor: string };
  /** Extra shift of an entry's expected y or height. */
  adjust?: (entry: Entry) => { dy?: number; dh?: number };
}

async function check(
  page: Page,
  entries: readonly Entry[],
  opts: CheckOptions = {},
) {
  let origin = { x: 0, y: 0 };
  if (opts.origin === undefined || opts.origin === "main") {
    origin = await mainOrigin(page);
  } else if (opts.origin !== "viewport") {
    const anchor = locate(page, opts.origin.anchor);
    // The caret scrolls the sheet; wait for the anchor to stop moving.
    let box = await anchor.boundingBox();
    for (let tries = 0; tries < 20; tries += 1) {
      await page.waitForTimeout(100);
      const next = await anchor.boundingBox();
      if (next && box && next.x === box.x && next.y === box.y) break;
      box = next;
    }
    if (!box) throw new Error("anchor has no box");
    origin = box;
  }
  const seen: string[] = [];
  for (const entry of entries) {
    const locator = locate(page, entry.at);
    await expect(locator, `${entry.name} exists`).toHaveCount(1);
    const box = await locator.boundingBox();
    if (!box) throw new Error(`${entry.name} has no box`);
    const { dy = 0, dh = 0 } = opts.adjust?.(entry) ?? {};
    const actual = {
      x: box.x - origin.x,
      y: box.y - origin.y,
      w: box.width,
      h: box.height,
    };
    const expected = {
      x: entry.box[0],
      y: entry.box[1] + dy,
      w: entry.box[2],
      h: entry.box[3] + dh,
    };
    for (const key of entry.keys.split("") as Key[]) {
      const diff = Math.abs(actual[key] - expected[key]);
      if (diff > 1.01) {
        seen.push(
          `${entry.name}.${key}: ${actual[key].toFixed(1)} vs ${expected[key]}`,
        );
      }
    }
    const s = entry.style;
    const wrong: string[] = [];
    if (s.fs && (await style(locator, "font-size")) !== s.fs)
      wrong.push("font-size");
    if (s.fw && (await style(locator, "font-weight")) !== s.fw)
      wrong.push("font-weight");
    if (s.r) {
      const radius = Math.min(
        parseFloat(await style(locator, "border-top-left-radius")),
        999,
      );
      if (Math.abs(radius - Math.min(parseFloat(s.r), 999)) > 0.5)
        wrong.push("radius");
    }
    if (s.c && (await colour(locator, "color")) !== s.c) wrong.push("color");
    if (s.bg && (await colour(locator, "background-color")) !== s.bg)
      wrong.push("background");
    if (wrong.length > 0) seen.push(`${entry.name}: ${wrong.join(", ")}`);
  }
  expect(seen, "boxes that differ from the mockup").toEqual([]);
}

/** The "why you see this page" line is present only for an unaffiliated admin. */
async function whyShift(page: Page): Promise<number> {
  const present = await page.getByText(/^Bạn thấy trang của/).count();
  return present > 0 ? 0 : -WHY_LINE;
}

/** Article entries sit left of the side panel (x < 780) below the meta chips. */
function articleAdjust(shift: number) {
  return (entry: Entry) => ({
    dy: entry.box[0] < 780 && entry.box[1] >= 250 ? shift : 0,
  });
}

/** Final-Docs-2, light. Origin: main area (332,0). */
// prettier-ignore
const NORMAL: Entry[] = [
  { name: "bar", at: "#workspace-main header", box: [0, 0, 1108, 49], keys: "xywh", style: {  } }, // .dbar
  { name: "crumb", at: "#workspace-main header nav", box: [24, 14.6, 283.6, 18.8], keys: "xyh", style: {  } }, // .dbar .crumb
  { name: "edited", at: "#workspace-main header > span.text-crumb@0", box: [328.6, 14.9, 226.1, 18.1], keys: "yh", style: { fs: "12.5px", fw: "400", c: "rgb(95, 104, 117)" } }, // .edited
  { name: "share", at: "#workspace-main header button[aria-haspopup=\"dialog\"]", box: [855.4, 8, 73.5, 32], keys: "xywh", style: { fs: "13px", fw: "500", c: "rgb(22, 26, 33)", r: "7px", bg: "rgb(255, 255, 255)" } }, // .dbar .btn@0
  { name: "approve", at: "#workspace-main header button[title=\"Duyệt tài liệu chưa khả dụng.\"]", box: [938.8, 8, 111.2, 32], keys: "xywh", style: { fs: "13px", fw: "600", c: "rgb(255, 255, 255)", r: "7px", bg: "rgb(22, 102, 204)" } }, // .dbar .btn.pri
  { name: "more", at: "#workspace-main header button[aria-label=\"Thêm tuỳ chọn\"]", box: [1060, 8, 32, 32], keys: "xywh", style: { r: "7px" } }, // .dbar .ib@1
  { name: "tbar", at: "#workspace-main header + div", box: [0, 49, 1108, 77], keys: "xywh", style: { bg: "rgb(255, 255, 255)" } }, // .tbar
  { name: "menus", at: "#workspace-main [role=menubar]", box: [0, 49, 1108, 30], keys: "xywh", style: {  } }, // .menus
  { name: "menuFile", at: "#workspace-main [role=menubar] button@0", box: [16, 51, 42.8, 28], keys: "xywh", style: { fs: "13px", fw: "400", c: "rgb(79, 88, 102)", r: "6px" } }, // .menus button@0
  { name: "menuTools", at: "#workspace-main [role=menubar] button@5", box: [318.7, 51, 71, 28], keys: "xywh", style: { fs: "13px", fw: "400", c: "rgb(79, 88, 102)", r: "6px" } }, // .menus button@5
  { name: "tools", at: "#workspace-main [role=toolbar]", box: [0, 79, 1108, 46], keys: "xywh", style: {  } }, // .tools
  { name: "tbUndo", at: "#workspace-main [role=toolbar] button[aria-label^=\"Hoàn tác\"]", box: [12, 86, 30, 30], keys: "xywh", style: { r: "6px" } }, // .tools [aria-label="Hoàn tác"]
  { name: "tbPrint", at: "#workspace-main [role=toolbar] button[aria-label^=\"In (\"]", box: [76, 86, 30, 30], keys: "xywh", style: { r: "6px" } }, // .tools [aria-label="In"]
  { name: "tsep1", at: "#workspace-main [role=toolbar] span[aria-hidden]@0", box: [112, 91, 1, 20], keys: "xywh", style: { bg: "rgb(210, 215, 222)" } }, // .tools .tsep@0
  { name: "selStyle", at: "#workspace-main [role=toolbar] button[aria-label=\"Kiểu đoạn\"]", box: [119, 86, 150, 30], keys: "xyh", style: { fs: "13px", fw: "400", c: "rgb(22, 26, 33)", r: "6px", bg: "rgb(255, 255, 255)" } }, // .tools [aria-label="Kiểu đoạn"]
  { name: "tbBold", at: "#workspace-main [role=toolbar] button[aria-label^=\"In đậm\"]", box: [282, 86, 30, 30], keys: "ywh", style: { fs: "13px", fw: "400", r: "6px" } }, // .tools [aria-label="In đậm"]
  { name: "tbAlignLeft", at: "#workspace-main [role=toolbar] button[aria-label=\"Căn trái\"]", box: [592, 86, 30, 30], keys: "ywh", style: { r: "6px" } }, // .tools [aria-label="Căn trái"]
  { name: "article", at: "#workspace-main article", box: [49.5, 162, 700, 1122], keys: "xyw", style: {  } }, // .doc
  { name: "h1", at: "#workspace-main article h1", box: [49.5, 162, 700, 40], keys: "xywh", style: { fs: "32px", fw: "650", c: "rgb(22, 26, 33)" } }, // .doc h1
  { name: "chip0", at: "#workspace-main article > div.flex.flex-wrap > :nth-child(1)", box: [49.5, 214, 186.4, 26], keys: "xyh", style: { fs: "12.5px", fw: "500", c: "rgb(79, 88, 102)", r: "7px", bg: "rgb(241, 243, 246)" } }, // .dmeta .chip@0
  { name: "chipDate", at: "#workspace-main article > div.flex.flex-wrap > :nth-child(2)", box: [243.9, 214, 157.4, 26], keys: "ywh", style: { fs: "12.5px", fw: "500", c: "rgb(79, 88, 102)", r: "7px", bg: "rgb(241, 243, 246)" } }, // .dmeta .chip@1
  { name: "chipBoard", at: "#workspace-main article > div.flex.flex-wrap > :nth-child(3)", box: [409.3, 214, 146.5, 26], keys: "ywh", style: { fs: "12.5px", fw: "500", c: "rgb(79, 88, 102)", r: "7px", bg: "rgb(241, 243, 246)" } }, // .dmeta .chip@2
  { name: "h2a", at: "#workspace-main article h2@0", box: [49.5, 298.1, 700, 27], keys: "xywh", style: { fs: "20px", fw: "650", c: "rgb(22, 26, 33)" } }, // .doc h2@0
  { name: "prose", at: "#workspace-main article p[data-block]@0", box: [49.5, 337.1, 700, 80.6], keys: "xywh", style: { fs: "15.5px", fw: "400", c: "rgb(39, 45, 54)" } }, // .doc .prose@0
  { name: "taskChip", at: "#workspace-main article p[data-block] [data-task-id]@0", box: [49.5, 391.7, 368.8, 26], keys: "xywh", style: { r: "6px", bg: "rgb(241, 243, 246)" } }, // .doc .prose .task
  { name: "h2b", at: "#workspace-main article h2@1", box: [49.5, 449.7, 700, 27], keys: "xywh", style: { fs: "20px", fw: "650", c: "rgb(22, 26, 33)" } }, // .doc h2@1
  { name: "h2c", at: "#workspace-main article h2@2", box: [49.5, 669.1, 700, 27], keys: "xywh", style: { fs: "20px", fw: "650", c: "rgb(22, 26, 33)" } }, // .doc h2@2
  { name: "ck1box", at: "#workspace-main article [role=checkbox]@0", box: [49.5, 717.6, 17, 17], keys: "xywh", style: { r: "5px", bg: "rgb(46, 139, 87)" } }, // .ck .box@0
  { name: "ck2", at: "#workspace-main article div.group@1", box: [41.5, 744.1, 716, 36], keys: "xywh", style: { r: "8px" } }, // .ck@1
  { name: "ck2box", at: "#workspace-main article [role=checkbox]@1", box: [49.5, 753.6, 17, 17], keys: "xywh", style: { r: "5px" } }, // .ck .box@1
  { name: "ck2t", at: "#workspace-main article div.group@1 >> .doc-todo-text", box: [76.5, 751.2, 507.9, 21.8], keys: "xywh", style: { fs: "15px", fw: "400", c: "rgb(39, 45, 54)" } }, // .ck .t@1
  { name: "h2d", at: "#workspace-main article h2@3", box: [49.5, 848.1, 700, 27], keys: "xywh", style: { fs: "20px", fw: "650", c: "rgb(22, 26, 33)" } }, // .doc h2@3
  { name: "live", at: "#workspace-main article div.overflow-hidden.rounded-\\[12px\\]", box: [49.5, 887.1, 700, 255.8], keys: "xywh", style: { r: "12px", bg: "rgb(255, 255, 255)" } }, // .live
  { name: "liveHead", at: "#workspace-main article div.overflow-hidden.rounded-\\[12px\\] > div@0", box: [50.5, 888.1, 698, 39.8], keys: "xywh", style: { fs: "13px", fw: "400", c: "rgb(95, 104, 117)" } }, // .live-h
  { name: "lrow1", at: "#workspace-main article [role=table] [role=row]@0", box: [50.5, 927.9, 698, 43], keys: "xywh", style: {  } }, // .live .lrow@0
  { name: "lrow1key", at: "#workspace-main article [role=table] [role=row]@0 >> a.font-mono", box: [64.5, 941.2, 50.4, 15], keys: "xywh", style: { fs: "12px", fw: "400", c: "rgb(95, 104, 117)" } }, // .live .lrow@0 >> a.mono
  { name: "lrow1st", at: "#workspace-main article [role=table] [role=row]@0 >> [role=cell]:nth-child(3) > span", box: [470.5, 937.9, 86.9, 22], keys: "xyh", style: { fs: "12px", fw: "600", c: "rgb(22, 26, 33)", r: "999px" } }, // .live .lrow@0 >> .st
  { name: "lrow1av", at: "#workspace-main article [role=table] [role=row]@0 >> [role=cell]:nth-child(4) > span", box: [584.5, 936.9, 24, 24], keys: "xywh", style: {  } }, // .live .lrow@0 >> .av
  { name: "lrow1due", at: "#workspace-main article [role=table] [role=row]@0 >> [role=cell]:nth-child(5)", box: [626.5, 939.9, 108, 18.1], keys: "xywh", style: { fs: "12.5px" } }, // .live .lrow@0 >> .due
  { name: "lrowLocked", at: "#workspace-main article [role=table] [role=row]@-1", box: [50.5, 1099.9, 698, 42], keys: "xywh", style: {  } }, // .live .lrow.locked
  { name: "liveNote", at: "#workspace-main article p[id^=\"live-note\"]", box: [49.5, 1154.9, 700, 37.5], keys: "xywh", style: { fs: "12.5px", fw: "400", c: "rgb(95, 104, 117)" } }, // .live-note
  { name: "callout", at: "#workspace-main article div.bg-doc-notice", box: [49.5, 1212.4, 700, 74.4], keys: "xywh", style: { r: "10px", bg: "rgb(238, 244, 253)" } }, // .callout
  { name: "rp", at: "#workspace-main aside[aria-label=\"Bảng bên của tài liệu\"]", box: [799, 126, 309, 1214], keys: "xyw", style: { bg: "rgb(255, 255, 255)" } }, // .rp
  { name: "rtabs", at: "#workspace-main aside [role=tablist]", box: [800, 126, 308, 44], keys: "xywh", style: {  } }, // .rtabs
  { name: "rtab1", at: "#workspace-main aside [role=tab]@0", box: [812, 126, 83.4, 44], keys: "xywh", style: { fs: "13px", fw: "600", c: "rgb(22, 26, 33)" } }, // .rtab@0
  { name: "rtab3", at: "#workspace-main aside [role=tab]@2", box: [968.6, 126, 89.3, 44], keys: "xywh", style: { fs: "13px", fw: "500", c: "rgb(79, 88, 102)" } }, // .rtab@2
  { name: "rbody", at: "#workspace-main aside [role=tabpanel]", box: [800, 170, 308, 1170], keys: "xyw", style: {  } }, // .rbody
  { name: "rh3", at: "#workspace-main aside [role=tabpanel] h3@0", box: [816, 184, 276, 18.1], keys: "xywh", style: { fs: "12.5px", fw: "600", c: "rgb(79, 88, 102)" } }, // .rbody h3@0
  { name: "rli1", at: "#workspace-main aside [role=tabpanel] a@0", box: [808, 210.1, 292, 71.8], keys: "xywh", style: { r: "8px" } }, // .rbody .li@0
  { name: "rli1b", at: "#workspace-main aside [role=tabpanel] a@0 >> b", box: [833, 218.1, 259, 36.4], keys: "xywh", style: { fs: "13px", fw: "500", c: "rgb(22, 26, 33)" } }, // .rbody .li@0 >> b
  { name: "rli1small", at: "#workspace-main aside [role=tabpanel] a@0 >> small", box: [833, 256.5, 259, 17.4], keys: "xywh", style: { fs: "12px", fw: "400", c: "rgb(95, 104, 117)" } }, // .rbody .li@0 >> small
];

/** Final-Docs-2-Dem: surfaces and ink only. */
// prettier-ignore
const DARK: Entry[] = [
  { name: "bar", at: "#workspace-main header", box: [0, 0, 1108, 49], keys: "", style: {  } }, // .dbar
  { name: "crumb", at: "#workspace-main header nav", box: [24, 14.6, 283.6, 18.8], keys: "", style: {  } }, // .dbar .crumb
  { name: "edited", at: "#workspace-main header > span.text-crumb@0", box: [328.6, 14.9, 226.1, 18.1], keys: "", style: { c: "rgb(143, 153, 167)" } }, // .edited
  { name: "share", at: "#workspace-main header button[aria-haspopup=\"dialog\"]", box: [855.4, 8, 73.5, 32], keys: "", style: { c: "rgb(230, 233, 238)", bg: "rgb(21, 26, 33)" } }, // .dbar .btn@0
  { name: "approve", at: "#workspace-main header button[title=\"Duyệt tài liệu chưa khả dụng.\"]", box: [938.8, 8, 111.2, 32], keys: "", style: {  } }, // .dbar .btn.pri
  { name: "more", at: "#workspace-main header button[aria-label=\"Thêm tuỳ chọn\"]", box: [1060, 8, 32, 32], keys: "", style: {  } }, // .dbar .ib@1
  { name: "tbar", at: "#workspace-main header + div", box: [0, 49, 1108, 77], keys: "", style: { bg: "rgb(18, 22, 28)" } }, // .tbar
  { name: "menus", at: "#workspace-main [role=menubar]", box: [0, 49, 1108, 30], keys: "", style: {  } }, // .menus
  { name: "menuFile", at: "#workspace-main [role=menubar] button@0", box: [16, 51, 42.8, 28], keys: "", style: { c: "rgb(170, 179, 192)" } }, // .menus button@0
  { name: "menuTools", at: "#workspace-main [role=menubar] button@5", box: [318.7, 51, 71, 28], keys: "", style: { c: "rgb(170, 179, 192)" } }, // .menus button@5
  { name: "tools", at: "#workspace-main [role=toolbar]", box: [0, 79, 1108, 46], keys: "", style: {  } }, // .tools
  { name: "tbUndo", at: "#workspace-main [role=toolbar] button[aria-label^=\"Hoàn tác\"]", box: [12, 86, 30, 30], keys: "", style: {  } }, // .tools [aria-label="Hoàn tác"]
  { name: "tbPrint", at: "#workspace-main [role=toolbar] button[aria-label^=\"In (\"]", box: [76, 86, 30, 30], keys: "", style: {  } }, // .tools [aria-label="In"]
  { name: "tsep1", at: "#workspace-main [role=toolbar] span[aria-hidden]@0", box: [112, 91, 1, 20], keys: "", style: { bg: "rgb(46, 55, 67)" } }, // .tools .tsep@0
  { name: "selStyle", at: "#workspace-main [role=toolbar] button[aria-label=\"Kiểu đoạn\"]", box: [119, 86, 150, 30], keys: "", style: { c: "rgb(230, 233, 238)", bg: "rgb(21, 26, 33)" } }, // .tools [aria-label="Kiểu đoạn"]
  { name: "tbBold", at: "#workspace-main [role=toolbar] button[aria-label^=\"In đậm\"]", box: [282, 86, 30, 30], keys: "", style: {  } }, // .tools [aria-label="In đậm"]
  { name: "tbAlignLeft", at: "#workspace-main [role=toolbar] button[aria-label=\"Căn trái\"]", box: [592, 86, 30, 30], keys: "", style: {  } }, // .tools [aria-label="Căn trái"]
  { name: "article", at: "#workspace-main article", box: [49.5, 162, 700, 1122], keys: "", style: {  } }, // .doc
  { name: "h1", at: "#workspace-main article h1", box: [49.5, 162, 700, 40], keys: "", style: { c: "rgb(230, 233, 238)" } }, // .doc h1
  { name: "chip0", at: "#workspace-main article > div.flex.flex-wrap > :nth-child(1)", box: [49.5, 214, 186.4, 26], keys: "", style: { c: "rgb(170, 179, 192)", bg: "rgb(28, 35, 44)" } }, // .dmeta .chip@0
  { name: "chipDate", at: "#workspace-main article > div.flex.flex-wrap > :nth-child(2)", box: [243.9, 214, 157.4, 26], keys: "", style: { c: "rgb(170, 179, 192)", bg: "rgb(28, 35, 44)" } }, // .dmeta .chip@1
  { name: "chipBoard", at: "#workspace-main article > div.flex.flex-wrap > :nth-child(3)", box: [409.3, 214, 146.5, 26], keys: "", style: { c: "rgb(170, 179, 192)", bg: "rgb(28, 35, 44)" } }, // .dmeta .chip@2
  { name: "h2a", at: "#workspace-main article h2@0", box: [49.5, 298.1, 700, 27], keys: "", style: { c: "rgb(230, 233, 238)" } }, // .doc h2@0
  { name: "prose", at: "#workspace-main article p[data-block]@0", box: [49.5, 337.1, 700, 80.6], keys: "", style: { c: "rgb(211, 216, 224)" } }, // .doc .prose@0
  { name: "taskChip", at: "#workspace-main article p[data-block] [data-task-id]@0", box: [49.5, 391.7, 368.8, 26], keys: "", style: { bg: "rgb(28, 35, 44)" } }, // .doc .prose .task
  { name: "h2b", at: "#workspace-main article h2@1", box: [49.5, 449.7, 700, 27], keys: "", style: { c: "rgb(230, 233, 238)" } }, // .doc h2@1
  { name: "h2c", at: "#workspace-main article h2@2", box: [49.5, 669.1, 700, 27], keys: "", style: { c: "rgb(230, 233, 238)" } }, // .doc h2@2
  { name: "ck1box", at: "#workspace-main article [role=checkbox]@0", box: [49.5, 717.6, 17, 17], keys: "", style: { bg: "rgb(86, 201, 138)" } }, // .ck .box@0
  { name: "ck2", at: "#workspace-main article div.group@1", box: [41.5, 744.1, 716, 36], keys: "", style: {  } }, // .ck@1
  { name: "ck2box", at: "#workspace-main article [role=checkbox]@1", box: [49.5, 753.6, 17, 17], keys: "", style: {  } }, // .ck .box@1
  { name: "ck2t", at: "#workspace-main article div.group@1 >> .doc-todo-text", box: [76.5, 751.2, 507.9, 21.8], keys: "", style: { c: "rgb(211, 216, 224)" } }, // .ck .t@1
  { name: "h2d", at: "#workspace-main article h2@3", box: [49.5, 848.1, 700, 27], keys: "", style: { c: "rgb(230, 233, 238)" } }, // .doc h2@3
  { name: "live", at: "#workspace-main article div.overflow-hidden.rounded-\\[12px\\]", box: [49.5, 887.1, 700, 255.8], keys: "", style: { bg: "rgb(21, 26, 33)" } }, // .live
  { name: "liveHead", at: "#workspace-main article div.overflow-hidden.rounded-\\[12px\\] > div@0", box: [50.5, 888.1, 698, 39.8], keys: "", style: { c: "rgb(143, 153, 167)" } }, // .live-h
  { name: "lrow1", at: "#workspace-main article [role=table] [role=row]@0", box: [50.5, 927.9, 698, 43], keys: "", style: {  } }, // .live .lrow@0
  { name: "lrow1key", at: "#workspace-main article [role=table] [role=row]@0 >> a.font-mono", box: [64.5, 941.2, 50.4, 15], keys: "", style: { c: "rgb(143, 153, 167)" } }, // .live .lrow@0 >> a.mono
  { name: "lrow1st", at: "#workspace-main article [role=table] [role=row]@0 >> [role=cell]:nth-child(3) > span", box: [470.5, 937.9, 86.9, 22], keys: "", style: { c: "rgb(230, 233, 238)" } }, // .live .lrow@0 >> .st
  { name: "lrow1av", at: "#workspace-main article [role=table] [role=row]@0 >> [role=cell]:nth-child(4) > span", box: [584.5, 936.9, 24, 24], keys: "", style: {  } }, // .live .lrow@0 >> .av
  { name: "lrow1due", at: "#workspace-main article [role=table] [role=row]@0 >> [role=cell]:nth-child(5)", box: [626.5, 939.9, 108, 18.1], keys: "", style: {  } }, // .live .lrow@0 >> .due
  { name: "lrowLocked", at: "#workspace-main article [role=table] [role=row]@-1", box: [50.5, 1099.9, 698, 42], keys: "", style: {  } }, // .live .lrow.locked
  { name: "liveNote", at: "#workspace-main article p[id^=\"live-note\"]", box: [49.5, 1154.9, 700, 37.5], keys: "", style: { c: "rgb(143, 153, 167)" } }, // .live-note
  { name: "callout", at: "#workspace-main article div.bg-doc-notice", box: [49.5, 1212.4, 700, 74.4], keys: "", style: { bg: "rgba(88, 167, 255, 0.10)" } }, // .callout
  { name: "rp", at: "#workspace-main aside[aria-label=\"Bảng bên của tài liệu\"]", box: [799, 126, 309, 1214], keys: "", style: { bg: "rgb(21, 26, 33)" } }, // .rp
  { name: "rtabs", at: "#workspace-main aside [role=tablist]", box: [800, 126, 308, 44], keys: "", style: {  } }, // .rtabs
  { name: "rtab1", at: "#workspace-main aside [role=tab]@0", box: [812, 126, 83.4, 44], keys: "", style: { c: "rgb(230, 233, 238)" } }, // .rtab@0
  { name: "rtab3", at: "#workspace-main aside [role=tab]@2", box: [968.6, 126, 89.3, 44], keys: "", style: { c: "rgb(170, 179, 192)" } }, // .rtab@2
  { name: "rbody", at: "#workspace-main aside [role=tabpanel]", box: [800, 170, 308, 1170], keys: "", style: {  } }, // .rbody
  { name: "rh3", at: "#workspace-main aside [role=tabpanel] h3@0", box: [816, 184, 276, 18.1], keys: "", style: { c: "rgb(170, 179, 192)" } }, // .rbody h3@0
  { name: "rli1", at: "#workspace-main aside [role=tabpanel] a@0", box: [808, 210.1, 292, 71.8], keys: "", style: {  } }, // .rbody .li@0
  { name: "rli1b", at: "#workspace-main aside [role=tabpanel] a@0 >> b", box: [833, 218.1, 259, 36.4], keys: "", style: { c: "rgb(230, 233, 238)" } }, // .rbody .li@0 >> b
  { name: "rli1small", at: "#workspace-main aside [role=tabpanel] a@0 >> small", box: [833, 256.5, 259, 17.4], keys: "", style: { c: "rgb(143, 153, 167)" } }, // .rbody .li@0 >> small
];

test.describe("Document page geometry at 1440 px (owner)", () => {
  test.skip(
    persona !== "owner" || scenario !== "default",
    "Owner checks need the default owner world",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  test("normal state, light: bar, tools, sheet, table and side panel", async ({
    page,
  }) => {
    await openDoc(page, { rail: "pinned" });
    // The artboard draws the second checklist line hovered, with its convert button shown.
    await page.locator("article div.group").nth(1).hover();
    const shift = await whyShift(page);
    await check(page, NORMAL, { adjust: articleAdjust(shift) });
  });

  test("normal state, dark: surfaces and ink", async ({ page }) => {
    await openDoc(page, { rail: "pinned", dark: true });
    const shift = await whyShift(page);
    await check(page, DARK, { adjust: articleAdjust(shift) });
  });
});
