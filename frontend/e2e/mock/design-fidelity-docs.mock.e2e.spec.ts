import { expect, test, type Locator, type Page } from "@playwright/test";

/**
 * Document page geometry against the approved Final-Docs-2 canvas (light,
 * dark, 320 and 320 sheet; numbers from Final-Docs-2-Notes, rendered at
 * 1440x900, relative to the main area's top-left so the rail pinned and
 * hidden give the same numbers). Tolerance is 1 px for boxes; font sizes,
 * weights, radii and colours must be equal. Known gaps: the mock world's
 * first live-table row is a "Cần làm" task due 18/10 (the canvas shows an
 * overdue one), and an inline task chip makes its line 1.2 px shorter than the
 * canvas, so everything below the numbered list sits 1.2 px higher than the
 * Notes. The File menu, slash, mention, comments and version-history states
 * still carry numbers from the older Final-Docs and drift from the shipped
 * menus and panel; they need their own pass.
 *
 * Text differs between the mock world and the mockup world, so an entry whose
 * width or position follows its wording lists only the keys that do not
 * (`keys`: x, y, w, h). Two data gaps shift the page, and the spec accounts
 * for both instead of hiding them: the "why you see this page" line shows only
 * to a workspace admin who is not a department member (26 px), and the share
 * dialog lists the people the mock world has (the page creator) instead of the
 * mockup's four (50 px each).
 *
 * The page clock is fixed to the fixtures' 12/10/2026 10:40. The save-error
 * checks run when the server was started with
 * `VITE_MOCK_FAULTS=/blocks/block-m-00010=500`.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const DOC = "/workspaces/mock-workspace-primary/docs/mock-doc-invoice-process";
const NOW = new Date("2026-10-12T10:40:00+07:00");
const RAIL_KEY = "notrelix-system-rail";
const THEME_KEY = "notrelix-theme-mode";
/** Height of the "why you see this page" line and its gap in the mockup. */
const WHY_LINE = 26.1;
/** One row of the share dialog's people list. */
const PERSON_ROW = 50;
/** People the mockup lists in the share dialog. */
const MOCK_PEOPLE = 4;

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
    if ((await locator.count()) !== 1) {
      seen.push(`${entry.name}: ${await locator.count()} matches`);
      continue;
    }
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

/** Final-Docs state=binh-thuong, nav=hien-day, mode=sang. Origin: main area (332,0). */
// prettier-ignore
const NORMAL: Entry[] = [
  { name: "bar", at: "#workspace-main header", box: [0, 0, 1108, 49], keys: "xywh", style: {  } }, // .dbar
  { name: "crumb", at: "#workspace-main header nav", box: [24, 14.6, 289.2, 18.8], keys: "xyh", style: {  } }, // .dbar .crumb
  { name: "edited", at: "#workspace-main header > span.text-crumb@0", box: [448.6, 14.9, 226.1, 18.1], keys: "yh", style: { fs: "12.5px", fw: "400", c: "rgb(95, 104, 117)" } }, // .edited
  { name: "share", at: "#workspace-main header button[aria-haspopup=\"dialog\"]", box: [855.4, 8, 73.5, 32], keys: "xywh", style: { fs: "13px", fw: "500", c: "rgb(22, 26, 33)", r: "7px", bg: "rgb(255, 255, 255)" } }, // .dbar .btn@0
  { name: "approve", at: "#workspace-main header button[title=\"Duyệt tài liệu chưa khả dụng.\"]", box: [938.8, 8, 111.2, 32], keys: "xywh", style: { fs: "13px", fw: "600", c: "rgb(255, 255, 255)", r: "7px", bg: "rgb(22, 102, 204)" } }, // .dbar .btn.pri
  { name: "more", at: "#workspace-main header button[aria-label=\"Thêm tuỳ chọn\"]", box: [1060, 8, 32, 32], keys: "xywh", style: { r: "7px" } }, // .dbar .ib
  { name: "tbar", at: "#workspace-main header + div", box: [0, 49, 1108, 76], keys: "xywh", style: { bg: "rgb(255, 255, 255)" } }, // .tbar
  { name: "menus", at: "#workspace-main [role=menubar]", box: [0, 49, 1108, 30], keys: "xywh", style: {  } }, // .menus
  { name: "menuFile", at: "#workspace-main [role=menubar] button@0", box: [16, 51, 42.8, 28], keys: "xywh", style: { fs: "13px", fw: "400", c: "rgb(79, 88, 102)", r: "6px" } }, // .menus button@0
  { name: "menuTools", at: "#workspace-main [role=menubar] button@5", box: [318.7, 51, 71, 28], keys: "xywh", style: { fs: "13px", fw: "400", c: "rgb(79, 88, 102)", r: "6px" } }, // .menus button@5
  { name: "tools", at: "#workspace-main [role=toolbar]", box: [0, 79, 1108, 46], keys: "xywh", style: {  } }, // .tools
  { name: "tbUndo", at: "#workspace-main [role=toolbar] [data-tool]@0", box: [12, 86, 30, 30], keys: "xywh", style: { r: "6px" } }, // .tools > .tb@0
  { name: "tbPrint", at: "#workspace-main [role=toolbar] [data-tool]@2", box: [76, 86, 30, 30], keys: "xywh", style: { r: "6px" } }, // .tools > .tb@2
  { name: "tsep1", at: "#workspace-main [role=toolbar] [data-group]@1 >> span[aria-hidden]", box: [112, 91, 1, 20], keys: "xywh", style: { bg: "rgb(210, 215, 222)" } }, // .tools .tsep@0
  { name: "selStyle", at: "#workspace-main [role=toolbar] button[aria-label=\"Kiểu đoạn\"]", box: [119, 86, 150, 30], keys: "xywh", style: { fs: "13px", fw: "400", c: "rgb(22, 26, 33)", r: "6px", bg: "rgb(255, 255, 255)" } }, // .tools .tb.sel@0
  { name: "tbBold", at: "#workspace-main [role=toolbar] button[aria-label^=\"In đậm\"]", box: [443.2, 86, 30, 30], keys: "ywh", style: { r: "6px" } }, // .tools .tb[aria-label^="In đậm"]
  { name: "tbAlignLeft", at: "#workspace-main [role=toolbar] button[aria-label=\"Căn trái\"]", box: [821.2, 86, 30, 30], keys: "ywh", style: { r: "6px" } }, // .tools .tb[aria-label="Căn trái"]
  { name: "article", at: "#workspace-main article", box: [49.5, 162, 700, 1113.2], keys: "xyw", style: {  } }, // .doc
  { name: "h1", at: "#workspace-main article h1", box: [49.5, 162, 700, 40], keys: "xywh", style: { fs: "32px", fw: "650", c: "rgb(22, 26, 33)" } }, // .doc h1
  { name: "chip0", at: "#workspace-main article > div.flex.flex-wrap > :nth-child(1)", box: [49.5, 214, 186.4, 26], keys: "xyh", style: { fs: "12.5px", fw: "500", c: "rgb(79, 88, 102)", r: "7px", bg: "rgb(241, 243, 246)" } }, // .dmeta .chip@0
  { name: "chipDate", at: "#workspace-main article > div.flex.flex-wrap > :nth-child(2)", box: [243.9, 214, 157.4, 26], keys: "ywh", style: { fs: "12.5px", fw: "500", c: "rgb(79, 88, 102)", r: "7px", bg: "rgb(241, 243, 246)" } }, // .dmeta .chip@1
  { name: "chipBoard", at: "#workspace-main article > div.flex.flex-wrap > :nth-child(3)", box: [409.3, 214, 146.5, 26], keys: "ywh", style: { fs: "12.5px", fw: "500", c: "rgb(79, 88, 102)", r: "7px", bg: "rgb(241, 243, 246)" } }, // .dmeta .chip@2
  { name: "h2a", at: "#workspace-main article h2@0", box: [49.5, 298.1, 700, 27], keys: "xywh", style: { fs: "20px", fw: "650", c: "rgb(22, 26, 33)" } }, // .doc h2@0
  { name: "prose", at: "#workspace-main article p[data-block]@0", box: [49.5, 337.1, 700, 80.1], keys: "xywh", style: { fs: "15.5px", fw: "400", c: "rgb(39, 45, 54)" } }, // .doc .prose@0
  { name: "taskChip", at: "#workspace-main article p[data-block] [data-task-id]@0", box: [49.5, 390.8, 368.8, 26], keys: "xywh", style: { r: "6px", bg: "rgb(241, 243, 246)" } }, // .doc .prose .task
  { name: "h2b", at: "#workspace-main article h2@1", box: [49.5, 449.2, 700, 27], keys: "xywh", style: { fs: "20px", fw: "650", c: "rgb(22, 26, 33)" } }, // .doc h2@1
  { name: "li1", at: "#workspace-main article [data-num]@0", box: [49.5, 488.2, 700, 27.1], keys: "xywh", style: { fs: "15.5px", fw: "400", c: "rgb(39, 45, 54)" } }, // .doc ol li@0
  { name: "li4", at: "#workspace-main article [data-num]@3", box: [49.5, 581.6, 700, 54.3], keys: "xywh", style: { fs: "15.5px", fw: "400", c: "rgb(39, 45, 54)" } }, // .doc ol li@3
  { name: "li4task", at: "#workspace-main article [data-num]@3 >> [data-task-id]", box: [77.5, 609.3, 385.3, 26], keys: "xywh", style: { r: "6px", bg: "rgb(241, 243, 246)" } }, // .doc ol li@3 >> .task
  { name: "h2c", at: "#workspace-main article h2@2", box: [49.5, 667.4, 700, 27], keys: "xywh", style: { fs: "20px", fw: "650", c: "rgb(22, 26, 33)" } }, // .doc h2@2
  { name: "ck1box", at: "#workspace-main article [role=checkbox]@0", box: [49.5, 715.9, 17, 17], keys: "xywh", style: { r: "5px", bg: "rgb(46, 139, 87)" } }, // .ck .box@0
  { name: "ck2", at: "#workspace-main article div.group@1", box: [41.5, 742.4, 716, 36], keys: "xywh", style: { r: "8px" } }, // .ck@1
  { name: "ck2box", at: "#workspace-main article [role=checkbox]@1", box: [49.5, 751.9, 17, 17], keys: "xywh", style: { r: "5px" } }, // .ck .box@1
  { name: "ck2t", at: "#workspace-main article div.group@1 >> .doc-todo-text", box: [76.5, 742.4, 507.9, 36], keys: "xywh", style: { fs: "15px", fw: "400", c: "rgb(39, 45, 54)" } }, // .ck .t@1
  { name: "ck2mk", at: "#workspace-main article button[aria-label^=\"Chuyển thành việc\"]@1", box: [594.4, 746.4, 155.1, 28], keys: "xywh", style: { fs: "12.5px", fw: "600", c: "rgb(15, 86, 174)", r: "7px", bg: "rgb(255, 255, 255)" } }, // .ck .mk@1
  { name: "h2d", at: "#workspace-main article h2@3", box: [49.5, 846.4, 700, 27], keys: "xywh", style: { fs: "20px", fw: "650", c: "rgb(22, 26, 33)" } }, // .doc h2@3
  { name: "live", at: "#workspace-main article div.overflow-hidden.rounded-\\[12px\\]", box: [49.5, 885.4, 700, 256.8], keys: "xywh", style: { r: "12px", bg: "rgb(255, 255, 255)" } }, // .live
  { name: "liveHead", at: "#workspace-main article div.overflow-hidden.rounded-\\[12px\\] > div@0", box: [50.5, 886.4, 698, 39.8], keys: "xywh", style: { fs: "13px", fw: "400", c: "rgb(95, 104, 117)" } }, // .live-h
  { name: "lrow1", at: "#workspace-main article [role=table] [role=row]@0", box: [50.5, 926.3, 698, 43], keys: "xywh", style: {  } }, // .live .lrow@0
  { name: "lrow1key", at: "#workspace-main article [role=table] [role=row]@0 >> a.font-mono", box: [64.5, 939.5, 50.4, 15], keys: "xywh", style: { fs: "12px", fw: "400", c: "rgb(95, 104, 117)" } }, // .live .lrow@0 >> a.mono
  { name: "lrow1st", at: "#workspace-main article [role=table] [role=row]@0 >> [role=cell]:nth-child(3) > span", box: [470.5, 936.3, 79.1, 22], keys: "xywh", style: { fs: "12px", fw: "600", c: "rgb(22, 26, 33)", r: "999px", bg: "rgb(236, 238, 242)" } }, // .live .lrow@0 >> .st
  { name: "lrow1av", at: "#workspace-main article [role=table] [role=row]@0 >> [role=cell]:nth-child(4) > span", box: [584.5, 935.3, 24, 24], keys: "xywh", style: {  } }, // .live .lrow@0 >> .av
  { name: "lrow1due", at: "#workspace-main article [role=table] [role=row]@0 >> [role=cell]:nth-child(5)", box: [626.5, 938.2, 108, 18.1], keys: "xywh", style: { fs: "12.5px", fw: "400", c: "rgb(95, 104, 117)" } }, // .live .lrow@0 >> .due
  { name: "lrowLocked", at: "#workspace-main article [role=table] [role=row]@-1", box: [50.5, 1098.3, 698, 42], keys: "xywh", style: {  } }, // .live .lrow.locked
  { name: "liveNote", at: "#workspace-main article p[id^=\"live-note\"]", box: [49.5, 1154.4, 700, 37.5], keys: "xywh", style: { fs: "12.5px", fw: "400", c: "rgb(95, 104, 117)" } }, // .live-note
  { name: "callout", at: "#workspace-main article div.bg-doc-notice", box: [49.5, 1211.9, 700, 74.4], keys: "xywh", style: { r: "10px", bg: "rgb(238, 244, 253)" } }, // .callout
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

/** Final-Docs state=binh-thuong, nav=an, mode=sang. Origin: main area (256,0); the side panel hugs the right edge. */
// prettier-ignore
const NORMAL_HIDDEN: Entry[] = [
  { name: "bar", at: "#workspace-main header", box: [0, 0, 1184, 49], keys: "xywh", style: {  } }, // .dbar
  { name: "crumb", at: "#workspace-main header nav", box: [24, 14.6, 289.2, 18.8], keys: "xyh", style: {  } }, // .dbar .crumb
  { name: "edited", at: "#workspace-main header > span.text-crumb@0", box: [448.6, 14.9, 226.1, 18.1], keys: "yh", style: {  } }, // .edited
  { name: "share", at: "#workspace-main header button[aria-haspopup=\"dialog\"]", box: [931.4, 8, 73.5, 32], keys: "xywh", style: {  } }, // .dbar .btn@0
  { name: "approve", at: "#workspace-main header button[title=\"Duyệt tài liệu chưa khả dụng.\"]", box: [1014.8, 8, 111.2, 32], keys: "xywh", style: {  } }, // .dbar .btn.pri
  { name: "more", at: "#workspace-main header button[aria-label=\"Thêm tuỳ chọn\"]", box: [1136, 8, 32, 32], keys: "xywh", style: {  } }, // .dbar .ib
  { name: "tbar", at: "#workspace-main header + div", box: [0, 49, 1184, 76], keys: "xywh", style: {  } }, // .tbar
  { name: "menus", at: "#workspace-main [role=menubar]", box: [0, 49, 1184, 30], keys: "xywh", style: {  } }, // .menus
  { name: "menuFile", at: "#workspace-main [role=menubar] button@0", box: [16, 51, 42.8, 28], keys: "xywh", style: {  } }, // .menus button@0
  { name: "menuTools", at: "#workspace-main [role=menubar] button@5", box: [318.7, 51, 71, 28], keys: "xywh", style: {  } }, // .menus button@5
  { name: "tools", at: "#workspace-main [role=toolbar]", box: [0, 79, 1184, 46], keys: "xywh", style: {  } }, // .tools
  { name: "tbUndo", at: "#workspace-main [role=toolbar] [data-tool]@0", box: [12, 86, 30, 30], keys: "xywh", style: {  } }, // .tools > .tb@0
  { name: "tbPrint", at: "#workspace-main [role=toolbar] [data-tool]@2", box: [76, 86, 30, 30], keys: "xywh", style: {  } }, // .tools > .tb@2
  { name: "tsep1", at: "#workspace-main [role=toolbar] [data-group]@1 >> span[aria-hidden]", box: [112, 91, 1, 20], keys: "xywh", style: {  } }, // .tools .tsep@0
  { name: "selStyle", at: "#workspace-main [role=toolbar] button[aria-label=\"Kiểu đoạn\"]", box: [119, 86, 150, 30], keys: "xywh", style: {  } }, // .tools .tb.sel@0
  { name: "tbBold", at: "#workspace-main [role=toolbar] button[aria-label^=\"In đậm\"]", box: [443.2, 86, 30, 30], keys: "ywh", style: { r: "6px" } }, // .tools .tb[aria-label^="In đậm"]
  { name: "tbAlignLeft", at: "#workspace-main [role=toolbar] button[aria-label=\"Căn trái\"]", box: [821.2, 86, 30, 30], keys: "ywh", style: {  } }, // .tools .tb[aria-label="Căn trái"]
  { name: "article", at: "#workspace-main article", box: [87.5, 162, 700, 1113.2], keys: "xyw", style: {  } }, // .doc
  { name: "h1", at: "#workspace-main article h1", box: [87.5, 162, 700, 40], keys: "xywh", style: {  } }, // .doc h1
  { name: "chip0", at: "#workspace-main article > div.flex.flex-wrap > :nth-child(1)", box: [87.5, 214, 186.4, 26], keys: "xyh", style: {  } }, // .dmeta .chip@0
  { name: "chipDate", at: "#workspace-main article > div.flex.flex-wrap > :nth-child(2)", box: [281.9, 214, 157.4, 26], keys: "ywh", style: {  } }, // .dmeta .chip@1
  { name: "chipBoard", at: "#workspace-main article > div.flex.flex-wrap > :nth-child(3)", box: [447.3, 214, 146.5, 26], keys: "ywh", style: {  } }, // .dmeta .chip@2
  { name: "h2a", at: "#workspace-main article h2@0", box: [87.5, 298.1, 700, 27], keys: "xywh", style: {  } }, // .doc h2@0
  { name: "prose", at: "#workspace-main article p[data-block]@0", box: [87.5, 337.1, 700, 80.1], keys: "xywh", style: {  } }, // .doc .prose@0
  { name: "taskChip", at: "#workspace-main article p[data-block] [data-task-id]@0", box: [87.5, 390.8, 368.8, 26], keys: "xywh", style: {  } }, // .doc .prose .task
  { name: "h2b", at: "#workspace-main article h2@1", box: [87.5, 449.2, 700, 27], keys: "xywh", style: {  } }, // .doc h2@1
  { name: "li1", at: "#workspace-main article [data-num]@0", box: [87.5, 488.2, 700, 27.1], keys: "xywh", style: {  } }, // .doc ol li@0
  { name: "li4", at: "#workspace-main article [data-num]@3", box: [87.5, 581.6, 700, 54.3], keys: "xywh", style: {  } }, // .doc ol li@3
  { name: "li4task", at: "#workspace-main article [data-num]@3 >> [data-task-id]", box: [115.5, 609.3, 385.3, 26], keys: "xywh", style: {  } }, // .doc ol li@3 >> .task
  { name: "h2c", at: "#workspace-main article h2@2", box: [87.5, 667.4, 700, 27], keys: "xywh", style: {  } }, // .doc h2@2
  { name: "ck1box", at: "#workspace-main article [role=checkbox]@0", box: [87.5, 715.9, 17, 17], keys: "xywh", style: {  } }, // .ck .box@0
  { name: "ck2", at: "#workspace-main article div.group@1", box: [79.5, 742.4, 716, 36], keys: "xywh", style: {  } }, // .ck@1
  { name: "ck2box", at: "#workspace-main article [role=checkbox]@1", box: [87.5, 751.9, 17, 17], keys: "xywh", style: {  } }, // .ck .box@1
  { name: "ck2t", at: "#workspace-main article div.group@1 >> .doc-todo-text", box: [114.5, 742.4, 507.9, 36], keys: "xywh", style: {  } }, // .ck .t@1
  { name: "ck2mk", at: "#workspace-main article button[aria-label^=\"Chuyển thành việc\"]@1", box: [632.4, 746.4, 155.1, 28], keys: "xywh", style: {  } }, // .ck .mk@1
  { name: "h2d", at: "#workspace-main article h2@3", box: [87.5, 846.4, 700, 27], keys: "xywh", style: {  } }, // .doc h2@3
  { name: "live", at: "#workspace-main article div.overflow-hidden.rounded-\\[12px\\]", box: [87.5, 885.4, 700, 256.8], keys: "xywh", style: {  } }, // .live
  { name: "liveHead", at: "#workspace-main article div.overflow-hidden.rounded-\\[12px\\] > div@0", box: [88.5, 886.4, 698, 39.8], keys: "xywh", style: {  } }, // .live-h
  { name: "lrow1", at: "#workspace-main article [role=table] [role=row]@0", box: [88.5, 926.3, 698, 43], keys: "xywh", style: {  } }, // .live .lrow@0
  { name: "lrow1key", at: "#workspace-main article [role=table] [role=row]@0 >> a.font-mono", box: [102.5, 939.5, 50.4, 15], keys: "xywh", style: {  } }, // .live .lrow@0 >> a.mono
  { name: "lrow1st", at: "#workspace-main article [role=table] [role=row]@0 >> [role=cell]:nth-child(3) > span", box: [508.5, 936.3, 79.1, 22], keys: "xywh", style: {  } }, // .live .lrow@0 >> .st
  { name: "lrow1av", at: "#workspace-main article [role=table] [role=row]@0 >> [role=cell]:nth-child(4) > span", box: [622.5, 935.3, 24, 24], keys: "xywh", style: {  } }, // .live .lrow@0 >> .av
  { name: "lrow1due", at: "#workspace-main article [role=table] [role=row]@0 >> [role=cell]:nth-child(5)", box: [664.5, 938.2, 108, 18.1], keys: "xywh", style: { fs: "12.5px", fw: "400", c: "rgb(95, 104, 117)" } }, // .live .lrow@0 >> .due
  { name: "lrowLocked", at: "#workspace-main article [role=table] [role=row]@-1", box: [88.5, 1098.3, 698, 42], keys: "xywh", style: {  } }, // .live .lrow.locked
  { name: "liveNote", at: "#workspace-main article p[id^=\"live-note\"]", box: [87.5, 1154.4, 700, 37.5], keys: "xywh", style: {  } }, // .live-note
  { name: "callout", at: "#workspace-main article div.bg-doc-notice", box: [87.5, 1211.9, 700, 74.4], keys: "xywh", style: {  } }, // .callout
  { name: "rp", at: "#workspace-main aside[aria-label=\"Bảng bên của tài liệu\"]", box: [875, 126, 309, 1214], keys: "xyw", style: {  } }, // .rp
  { name: "rtabs", at: "#workspace-main aside [role=tablist]", box: [876, 126, 308, 44], keys: "xywh", style: {  } }, // .rtabs
  { name: "rtab1", at: "#workspace-main aside [role=tab]@0", box: [888, 126, 83.4, 44], keys: "xywh", style: {  } }, // .rtab@0
  { name: "rtab3", at: "#workspace-main aside [role=tab]@2", box: [1044.6, 126, 89.3, 44], keys: "xywh", style: {  } }, // .rtab@2
  { name: "rbody", at: "#workspace-main aside [role=tabpanel]", box: [876, 170, 308, 1170], keys: "xyw", style: {  } }, // .rbody
  { name: "rh3", at: "#workspace-main aside [role=tabpanel] h3@0", box: [892, 184, 276, 18.1], keys: "xywh", style: {  } }, // .rbody h3@0
  { name: "rli1", at: "#workspace-main aside [role=tabpanel] a@0", box: [884, 210.1, 292, 71.8], keys: "xywh", style: {  } }, // .rbody .li@0
  { name: "rli1b", at: "#workspace-main aside [role=tabpanel] a@0 >> b", box: [909, 218.1, 259, 36.4], keys: "xywh", style: {  } }, // .rbody .li@0 >> b
  { name: "rli1small", at: "#workspace-main aside [role=tabpanel] a@0 >> small", box: [909, 256.5, 259, 17.4], keys: "xywh", style: {  } }, // .rbody .li@0 >> small
];

/** Final-Docs state=binh-thuong, mode=dem: surfaces and ink only. */
// prettier-ignore
const DARK: Entry[] = [
  { name: "bar", at: "#workspace-main header", box: [0, 0, 1108, 49], keys: "", style: {  } }, // .dbar
  { name: "crumb", at: "#workspace-main header nav", box: [24, 14.6, 289.2, 18.8], keys: "", style: {  } }, // .dbar .crumb
  { name: "edited", at: "#workspace-main header > span.text-crumb@0", box: [448.6, 14.9, 226.1, 18.1], keys: "", style: { c: "rgb(143, 153, 167)" } }, // .edited
  { name: "share", at: "#workspace-main header button[aria-haspopup=\"dialog\"]", box: [855.4, 8, 73.5, 32], keys: "", style: { c: "rgb(230, 233, 238)", bg: "rgb(21, 26, 33)" } }, // .dbar .btn@0
  { name: "approve", at: "#workspace-main header button[title=\"Duyệt tài liệu chưa khả dụng.\"]", box: [938.8, 8, 111.2, 32], keys: "", style: {  } }, // .dbar .btn.pri
  { name: "more", at: "#workspace-main header button[aria-label=\"Thêm tuỳ chọn\"]", box: [1060, 8, 32, 32], keys: "", style: {  } }, // .dbar .ib
  { name: "tbar", at: "#workspace-main header + div", box: [0, 49, 1108, 76], keys: "", style: { bg: "rgb(18, 22, 28)" } }, // .tbar
  { name: "menus", at: "#workspace-main [role=menubar]", box: [0, 49, 1108, 30], keys: "", style: {  } }, // .menus
  { name: "menuFile", at: "#workspace-main [role=menubar] button@0", box: [16, 51, 42.8, 28], keys: "", style: { c: "rgb(170, 179, 192)" } }, // .menus button@0
  { name: "menuTools", at: "#workspace-main [role=menubar] button@5", box: [318.7, 51, 71, 28], keys: "", style: { c: "rgb(170, 179, 192)" } }, // .menus button@5
  { name: "tools", at: "#workspace-main [role=toolbar]", box: [0, 79, 1108, 46], keys: "", style: {  } }, // .tools
  { name: "tbUndo", at: "#workspace-main [role=toolbar] [data-tool]@0", box: [12, 86, 30, 30], keys: "", style: {  } }, // .tools > .tb@0
  { name: "tbPrint", at: "#workspace-main [role=toolbar] [data-tool]@2", box: [76, 86, 30, 30], keys: "", style: {  } }, // .tools > .tb@2
  { name: "tsep1", at: "#workspace-main [role=toolbar] [data-group]@1 >> span[aria-hidden]", box: [112, 91, 1, 20], keys: "", style: { bg: "rgb(46, 55, 67)" } }, // .tools .tsep@0
  { name: "selStyle", at: "#workspace-main [role=toolbar] button[aria-label=\"Kiểu đoạn\"]", box: [119, 86, 150, 30], keys: "xywh", style: { c: "rgb(230, 233, 238)", bg: "rgb(21, 26, 33)" } }, // .tools .tb.sel@0
  { name: "tbBold", at: "#workspace-main [role=toolbar] button[aria-label^=\"In đậm\"]", box: [443.2, 86, 30, 30], keys: "", style: { r: "6px" } }, // .tools .tb[aria-label^="In đậm"]
  { name: "tbAlignLeft", at: "#workspace-main [role=toolbar] button[aria-label=\"Căn trái\"]", box: [821.2, 86, 30, 30], keys: "", style: {  } }, // .tools .tb[aria-label="Căn trái"]
  { name: "article", at: "#workspace-main article", box: [49.5, 162, 700, 1113.2], keys: "", style: {  } }, // .doc
  { name: "h1", at: "#workspace-main article h1", box: [49.5, 162, 700, 40], keys: "", style: { c: "rgb(230, 233, 238)" } }, // .doc h1
  { name: "chip0", at: "#workspace-main article > div.flex.flex-wrap > :nth-child(1)", box: [49.5, 214, 186.4, 26], keys: "", style: { c: "rgb(170, 179, 192)", bg: "rgb(28, 35, 44)" } }, // .dmeta .chip@0
  { name: "chipDate", at: "#workspace-main article > div.flex.flex-wrap > :nth-child(2)", box: [243.9, 214, 157.4, 26], keys: "", style: { c: "rgb(170, 179, 192)", bg: "rgb(28, 35, 44)" } }, // .dmeta .chip@1
  { name: "chipBoard", at: "#workspace-main article > div.flex.flex-wrap > :nth-child(3)", box: [409.3, 214, 146.5, 26], keys: "", style: { c: "rgb(170, 179, 192)", bg: "rgb(28, 35, 44)" } }, // .dmeta .chip@2
  { name: "h2a", at: "#workspace-main article h2@0", box: [49.5, 298.1, 700, 27], keys: "", style: { c: "rgb(230, 233, 238)" } }, // .doc h2@0
  { name: "prose", at: "#workspace-main article p[data-block]@0", box: [49.5, 337.1, 700, 80.1], keys: "", style: { c: "rgb(211, 216, 224)" } }, // .doc .prose@0
  { name: "taskChip", at: "#workspace-main article p[data-block] [data-task-id]@0", box: [49.5, 390.8, 368.8, 26], keys: "", style: { bg: "rgb(28, 35, 44)" } }, // .doc .prose .task
  { name: "h2b", at: "#workspace-main article h2@1", box: [49.5, 449.2, 700, 27], keys: "", style: { c: "rgb(230, 233, 238)" } }, // .doc h2@1
  { name: "li1", at: "#workspace-main article [data-num]@0", box: [49.5, 488.2, 700, 27.1], keys: "", style: { c: "rgb(211, 216, 224)" } }, // .doc ol li@0
  { name: "li4", at: "#workspace-main article [data-num]@3", box: [49.5, 581.6, 700, 54.3], keys: "", style: { c: "rgb(211, 216, 224)" } }, // .doc ol li@3
  { name: "li4task", at: "#workspace-main article [data-num]@3 >> [data-task-id]", box: [77.5, 609.3, 385.3, 26], keys: "", style: { bg: "rgb(28, 35, 44)" } }, // .doc ol li@3 >> .task
  { name: "h2c", at: "#workspace-main article h2@2", box: [49.5, 667.4, 700, 27], keys: "", style: { c: "rgb(230, 233, 238)" } }, // .doc h2@2
  { name: "ck1box", at: "#workspace-main article [role=checkbox]@0", box: [49.5, 715.9, 17, 17], keys: "", style: { bg: "rgb(86, 201, 138)" } }, // .ck .box@0
  { name: "ck2", at: "#workspace-main article div.group@1", box: [41.5, 742.4, 716, 36], keys: "", style: {  } }, // .ck@1
  { name: "ck2box", at: "#workspace-main article [role=checkbox]@1", box: [49.5, 751.9, 17, 17], keys: "", style: {  } }, // .ck .box@1
  { name: "ck2t", at: "#workspace-main article div.group@1 >> .doc-todo-text", box: [76.5, 742.4, 507.9, 36], keys: "", style: { c: "rgb(211, 216, 224)" } }, // .ck .t@1
  { name: "ck2mk", at: "#workspace-main article button[aria-label^=\"Chuyển thành việc\"]@1", box: [594.4, 746.4, 155.1, 28], keys: "", style: { bg: "rgb(21, 26, 33)" } }, // .ck .mk@1
  { name: "h2d", at: "#workspace-main article h2@3", box: [49.5, 846.4, 700, 27], keys: "", style: { c: "rgb(230, 233, 238)" } }, // .doc h2@3
  { name: "live", at: "#workspace-main article div.overflow-hidden.rounded-\\[12px\\]", box: [49.5, 885.4, 700, 256.8], keys: "", style: { bg: "rgb(21, 26, 33)" } }, // .live
  { name: "liveHead", at: "#workspace-main article div.overflow-hidden.rounded-\\[12px\\] > div@0", box: [50.5, 886.4, 698, 39.8], keys: "", style: { c: "rgb(143, 153, 167)" } }, // .live-h
  { name: "lrow1", at: "#workspace-main article [role=table] [role=row]@0", box: [50.5, 926.3, 698, 43], keys: "", style: {  } }, // .live .lrow@0
  { name: "lrow1key", at: "#workspace-main article [role=table] [role=row]@0 >> a.font-mono", box: [64.5, 939.5, 50.4, 15], keys: "", style: { c: "rgb(143, 153, 167)" } }, // .live .lrow@0 >> a.mono
  { name: "lrow1st", at: "#workspace-main article [role=table] [role=row]@0 >> [role=cell]:nth-child(3) > span", box: [470.5, 936.3, 79.1, 22], keys: "", style: { c: "rgb(230, 233, 238)", bg: "rgb(30, 37, 47)" } }, // .live .lrow@0 >> .st
  { name: "lrow1av", at: "#workspace-main article [role=table] [role=row]@0 >> [role=cell]:nth-child(4) > span", box: [584.5, 935.3, 24, 24], keys: "", style: {  } }, // .live .lrow@0 >> .av
  { name: "lrow1due", at: "#workspace-main article [role=table] [role=row]@0 >> [role=cell]:nth-child(5)", box: [626.5, 938.2, 108, 18.1], keys: "", style: { fs: "12.5px", fw: "400", c: "rgb(143, 153, 167)" } }, // .live .lrow@0 >> .due
  { name: "lrowLocked", at: "#workspace-main article [role=table] [role=row]@-1", box: [50.5, 1098.3, 698, 42], keys: "", style: {  } }, // .live .lrow.locked
  { name: "liveNote", at: "#workspace-main article p[id^=\"live-note\"]", box: [49.5, 1154.4, 700, 37.5], keys: "", style: { c: "rgb(143, 153, 167)" } }, // .live-note
  { name: "callout", at: "#workspace-main article div.bg-doc-notice", box: [49.5, 1211.9, 700, 74.4], keys: "", style: { bg: "rgba(88, 167, 255, 0.10)" } }, // .callout
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

/** Final-Docs state=menu-tep. Origin: main area. */
// prettier-ignore
const FILE_MENU: Entry[] = [
  { name: "menuBtnOpen", at: "#workspace-main [role=menubar] button@0", box: [16, 51, 42.8, 28], keys: "xywh", style: { fs: "13px", fw: "400", c: "rgb(22, 26, 33)", r: "6px", bg: "rgb(230, 233, 238)" } }, // .menus button@0
  { name: "menu", at: "#workspace-main [role=menu][aria-label=\"Tệp\"]", box: [16, 79, 294, 353], keys: "xywh", style: { r: "10px", bg: "rgb(255, 255, 255)" } }, // .menu
  { name: "mi0", at: "#workspace-main [role=menu][aria-label=\"Tệp\"] [role=menuitem]@0", box: [23, 86, 280, 34], keys: "xywh", style: { fs: "13.5px", fw: "400", c: "rgb(22, 26, 33)", r: "7px" } }, // .menu .mi@0
  { name: "mi0k", at: "#workspace-main [role=menu][aria-label=\"Tệp\"] [role=menuitem]@0 >> span", box: [272.5, 94.3, 20.5, 17.4], keys: "xywh", style: { fs: "12px", fw: "400", c: "rgb(95, 104, 117)" } }, // .menu .mi@0 >> .k
  { name: "mi2", at: "#workspace-main [role=menu][aria-label=\"Tệp\"] [role=menuitem]@2", box: [23, 154, 280, 34], keys: "xywh", style: { fs: "13.5px", fw: "400", c: "rgb(22, 26, 33)", r: "7px" } }, // .menu .mi@2
  { name: "sep0", at: "#workspace-main [role=menu][aria-label=\"Tệp\"] [role=separator]@0", box: [27, 193, 272, 1], keys: "xywh", style: { bg: "rgb(230, 233, 238)" } }, // .menu .msep@0
  { name: "mi3dl", at: "#workspace-main [role=menu][aria-label=\"Tệp\"] [role=menuitem]@3", box: [23, 199, 280, 34], keys: "xywh", style: { fs: "13.5px", fw: "400", c: "rgb(22, 26, 33)", r: "7px", bg: "rgb(230, 233, 238)" } }, // .menu .mi@3
  { name: "mi4", at: "#workspace-main [role=menu][aria-label=\"Tệp\"] [role=menuitem]@4", box: [23, 233, 280, 34], keys: "xywh", style: { fs: "13.5px", fw: "400", c: "rgb(22, 26, 33)", r: "7px" } }, // .menu .mi@4
  { name: "mi6", at: "#workspace-main [role=menu][aria-label=\"Tệp\"] [role=menuitem]@6", box: [23, 312, 280, 34], keys: "xywh", style: { fs: "13.5px", fw: "400", c: "rgb(22, 26, 33)", r: "7px" } }, // .menu .mi@6
  { name: "mi8", at: "#workspace-main [role=menu][aria-label=\"Tệp\"] [role=menuitem]@8", box: [23, 391, 280, 34], keys: "xywh", style: { fs: "13.5px", fw: "400", c: "rgb(196, 35, 64)", r: "7px" } }, // .menu .mi@8
  { name: "miDanger", at: "#workspace-main [role=menu][aria-label=\"Tệp\"] [role=menuitem]@-1", box: [23, 391, 280, 34], keys: "xywh", style: { fs: "13.5px", fw: "400", c: "rgb(196, 35, 64)", r: "7px" } }, // .menu .mi.danger
  { name: "sub", at: "#workspace-main [role=menu][aria-label=\"Tải xuống\"]", box: [282, 199, 264, 116], keys: "xywh", style: { r: "10px", bg: "rgb(255, 255, 255)" } }, // .sub
  { name: "sub0", at: "#workspace-main [role=menu][aria-label=\"Tải xuống\"] [role=menuitem]@0", box: [289, 206, 250, 34], keys: "xywh", style: { fs: "13.5px", fw: "400", c: "rgb(22, 26, 33)", r: "7px", bg: "rgb(230, 233, 238)" } }, // .sub .mi@0
  { name: "ft0", at: "#workspace-main [role=menu][aria-label=\"Tải xuống\"] [role=menuitem]@0 >> span", box: [299, 210, 26, 26], keys: "xywh", style: { fs: "9.5px", fw: "700", c: "rgb(255, 255, 255)", r: "6px", bg: "rgb(43, 87, 154)" } }, // .sub .ft@0
  { name: "soon", at: "#workspace-main [role=menu][aria-label=\"Tải xuống\"] [role=menuitem]@0 >> span@1", box: [476.7, 214, 52.3, 17.9], keys: "xywh", style: { fs: "11px", fw: "600", c: "rgb(138, 90, 0)", r: "999px", bg: "rgb(251, 240, 214)" } }, // .sub .soon
  { name: "sub1", at: "#workspace-main [role=menu][aria-label=\"Tải xuống\"] [role=menuitem]@1", box: [289, 240, 250, 34], keys: "xywh", style: { fs: "13.5px", fw: "400", c: "rgb(22, 26, 33)", r: "7px" } }, // .sub .mi@1
  { name: "sub2", at: "#workspace-main [role=menu][aria-label=\"Tải xuống\"] [role=menuitem]@2", box: [289, 274, 250, 34], keys: "xywh", style: { fs: "13.5px", fw: "400", c: "rgb(22, 26, 33)", r: "7px" } }, // .sub .mi@2
];

/** Final-Docs state=go-lenh. Origin: the line the menu opens under (`.slashline`). */
// prettier-ignore
const SLASH: Entry[] = [
  { name: "pop", at: "[role=listbox][aria-label=\"Chèn khối\"]", box: [0, 36, 334, 212.3], keys: "xywh", style: { r: "12px", bg: "rgb(255, 255, 255)" } }, // .pop
  { name: "ph0", at: "[role=listbox][aria-label=\"Chèn khối\"] > span@0", box: [15, 47, 304, 16.1], keys: "xywh", style: { fs: "11.5px", fw: "600", c: "rgb(95, 104, 117)" } }, // .pop .ph@0
  { name: "opt0", at: "[role=listbox][aria-label=\"Chèn khối\"] [role=option]@0", box: [7, 67.1, 320, 44], keys: "xywh", style: { r: "8px", bg: "rgb(230, 233, 238)" } }, // .pop .opt@0
  { name: "opt0ic", at: "[role=listbox][aria-label=\"Chèn khối\"] [role=option]@0 >> span", box: [15, 73.1, 32, 32], keys: "xywh", style: { r: "7px", bg: "rgb(255, 255, 255)" } }, // .pop .opt@0 >> .ic
  { name: "opt0small", at: "[role=listbox][aria-label=\"Chèn khối\"] [role=option]@0 >> small", box: [57, 90.1, 203.3, 16.8], keys: "xywh", style: { fs: "12px", fw: "400", c: "rgb(95, 104, 117)" } }, // .pop .opt@0 >> small
  { name: "opt1", at: "[role=listbox][aria-label=\"Chèn khối\"] [role=option]@1", box: [7, 111.1, 320, 44], keys: "xywh", style: { r: "8px" } }, // .pop .opt@1
  { name: "opt2", at: "[role=listbox][aria-label=\"Chèn khối\"] [role=option]@2", box: [7, 155.1, 320, 44], keys: "xywh", style: { r: "8px" } }, // .pop .opt@2
  { name: "ph1", at: "[role=listbox][aria-label=\"Chèn khối\"] > span@1", box: [15, 205.1, 304, 32.2], keys: "xywh", style: { fs: "11.5px", fw: "600", c: "rgb(95, 104, 117)" } }, // .pop .ph@1
];

/** Final-Docs state=nhac-viec. Origin: the paragraph the list opens under. */
// prettier-ignore
const MENTION: Entry[] = [
  { name: "pop", at: "[role=listbox][aria-label=\"Gợi ý công việc\"]", box: [280, 34, 334, 249.3], keys: "xywh", style: { r: "12px", bg: "rgb(255, 255, 255)" } }, // .pop
  { name: "ph0", at: "[role=listbox][aria-label=\"Gợi ý công việc\"] > span@0", box: [295, 45, 304, 16.1], keys: "xywh", style: { fs: "11.5px", fw: "600", c: "rgb(95, 104, 117)" } }, // .pop .ph@0
  { name: "opt0", at: "[role=listbox][aria-label=\"Gợi ý công việc\"] [role=option]@0", box: [287, 65.1, 320, 45.8], keys: "xywh", style: { fs: "13.5px", fw: "400", c: "rgb(22, 26, 33)", r: "8px" } }, // .pop .opt@0
  { name: "opt0dot", at: "[role=listbox][aria-label=\"Gợi ý công việc\"] [role=option]@0 >> span", box: [295, 84, 8, 8], keys: "xywh", style: { r: "999px" } }, // .pop .opt@0 >> .dot
  { name: "opt0mono", at: "[role=listbox][aria-label=\"Gợi ý công việc\"] [role=option]@0 >> span@1", box: [313, 71.2, 35.9, 33.6], keys: "xyh", style: { fs: "12px", fw: "400", c: "rgb(95, 104, 117)" } }, // .pop .opt@0 >> .mono
  { name: "opt1", at: "[role=listbox][aria-label=\"Gợi ý công việc\"] [role=option]@1", box: [287, 110.9, 320, 45.8], keys: "xywh", style: { fs: "13.5px", fw: "400", c: "rgb(22, 26, 33)", r: "8px" } }, // .pop .opt@1
  { name: "opt3", at: "[role=listbox][aria-label=\"Gợi ý công việc\"] [role=option]@3", box: [287, 202.4, 320, 45.8], keys: "xywh", style: { fs: "13.5px", fw: "400", c: "rgb(22, 26, 33)", r: "8px" } }, // .pop .opt@3
  { name: "ph1", at: "[role=listbox][aria-label=\"Gợi ý công việc\"] > span@-1", box: [295, 256.2, 304, 16.1], keys: "xywh", style: { fs: "11.5px", fw: "600", c: "rgb(95, 104, 117)" } }, // .pop .ph@1
];

/** Final-Docs state=binh-luan. Origin: main area. */
// prettier-ignore
const COMMENTS: Entry[] = [
  { name: "rtabOn", at: "#workspace-main aside [role=tab][aria-selected=true]", box: [968.1, 154, 90.3, 44], keys: "xywh", style: { fs: "13px", fw: "600", c: "rgb(22, 26, 33)" } }, // .rtab.on
  { name: "rbody", at: "#workspace-main aside [role=tabpanel]", box: [800, 198, 308, 1169.2], keys: "xyw", style: {  } }, // .rbody
  { name: "thread", at: "#workspace-main aside [role=tabpanel] > div", box: [816, 212, 276, 262.7], keys: "xywh", style: {  } }, // .thread
  { name: "quote", at: "#workspace-main aside [role=tabpanel] > div > div@0", box: [816, 212, 276, 26.1], keys: "xywh", style: { fs: "12.5px", fw: "400", c: "rgb(39, 45, 54)", r: "5px", bg: "rgb(252, 239, 196)" } }, // .quote
  { name: "cm0", at: "#workspace-main aside [role=tabpanel] > div > div@1", box: [816, 250.1, 276, 62.3], keys: "xywh", style: {  } }, // .cm@0
  { name: "cm0av", at: "#workspace-main aside [role=tabpanel] > div > div@1 >> span", box: [816, 250.1, 26, 26], keys: "xywh", style: {  } }, // .cm@0 >> .av
  { name: "cm0b", at: "#workspace-main aside [role=tabpanel] > div > div@1 >> b", box: [851, 252.1, 79.9, 16], keys: "xyh", style: { fs: "12.5px", fw: "600", c: "rgb(22, 26, 33)" } }, // .cm@0 >> b
  { name: "cm0time", at: "#workspace-main aside [role=tabpanel] > div > div@1 >> time", box: [935.9, 253.1, 50, 15], keys: "yh", style: { fs: "11.5px", fw: "400", c: "rgb(95, 104, 117)" } }, // .cm@0 >> time
  { name: "cm0p", at: "#workspace-main aside [role=tabpanel] > div > div@1 >> p", box: [851, 273.4, 241, 39], keys: "xywh", style: { fs: "13px", fw: "400", c: "rgb(39, 45, 54)" } }, // .cm@0 >> p
  { name: "cm1", at: "#workspace-main aside [role=tabpanel] > div > div@2", box: [816, 324.4, 276, 62.3], keys: "xywh", style: {  } }, // .cm@1
  { name: "reply", at: "#workspace-main aside [role=tabpanel] input", box: [816, 398.7, 276, 34], keys: "xywh", style: { r: "8px", bg: "rgb(247, 248, 250)" } }, // .reply input
  { name: "btn0", at: "#workspace-main aside [role=tabpanel] button@0", box: [816, 444.7, 77, 30], keys: "xywh", style: { fs: "13px", fw: "500", c: "rgb(22, 26, 33)", r: "7px", bg: "rgb(255, 255, 255)" } }, // .thread .btn@0
  { name: "btn1", at: "#workspace-main aside [role=tabpanel] button@1", box: [899, 444.7, 158.4, 30], keys: "xywh", style: { fs: "13px", fw: "500", c: "rgb(22, 26, 33)", r: "7px", bg: "rgb(255, 255, 255)" } }, // .thread .btn@1
];

/** Final-Docs state=muc-luc. Origin: main area. */
// prettier-ignore
const CONTENTS: Entry[] = [
  { name: "rtabOn", at: "#workspace-main aside [role=tab][aria-selected=true]", box: [896.9, 126, 69.8, 44], keys: "xywh", style: { fs: "13px", fw: "600", c: "rgb(22, 26, 33)" } }, // .rtab.on
  { name: "toc", at: "#workspace-main aside nav[aria-label=\"Mục lục tài liệu\"]", box: [816, 184, 276, 196.7], keys: "xywh", style: {  } }, // .toc
  { name: "toc0", at: "#workspace-main aside nav a@0", box: [808, 184, 292, 30.2], keys: "xywh", style: { fs: "13px", fw: "600", c: "rgb(22, 26, 33)", r: "7px" } }, // .toc a@0
  { name: "toc1", at: "#workspace-main aside nav a@1", box: [808, 215.2, 292, 30.2], keys: "xywh", style: { fs: "13px", fw: "400", c: "rgb(79, 88, 102)", r: "7px" } }, // .toc a@1
  { name: "toc2on", at: "#workspace-main aside nav a[aria-current=location]", box: [808, 246.4, 292, 30.2], keys: "xywh", style: { fs: "13px", fw: "600", c: "rgb(15, 86, 174)", r: "7px", bg: "rgb(231, 240, 252)" } }, // .toc a.on
  { name: "toc4", at: "#workspace-main aside nav a@4", box: [808, 308.8, 292, 30.2], keys: "xywh", style: { fs: "13px", fw: "400", c: "rgb(79, 88, 102)", r: "7px" } }, // .toc a@4
  { name: "tocSmall", at: "#workspace-main aside nav small", box: [816, 345.9, 276, 34.8], keys: "xywh", style: { fs: "12px", fw: "400", c: "rgb(95, 104, 117)" } }, // .toc small
];

const AFTER_PEOPLE = new Set([
  "pnote",
  "lk",
  "lkIc",
  "lkB",
  "lkOff",
  "df",
  "dfA",
  "dfBtn0",
  "dfBtnPri",
]);

/** Final-Docs state=chia-se. Origin: the viewport (the dialog sits over the whole window). */
// prettier-ignore
const SHARE: Entry[] = [
  { name: "scrim", at: "div.z-modal", box: [0, 0, 1440, 900], keys: "xywh", style: { bg: "rgba(16, 24, 42, 0.38)" } }, // .scrim
  { name: "dlg", at: "[role=dialog]", box: [400, 56, 640, 740.5], keys: "xywh", style: { r: "14px", bg: "rgb(255, 255, 255)" } }, // .dlg
  { name: "dh", at: "[role=dialog] > div@0", box: [400, 56, 640, 54], keys: "xywh", style: {  } }, // .dh
  { name: "dh2", at: "#doc-share-title", box: [420, 77.8, 359.7, 24.3], keys: "xyh", style: { fs: "18px", fw: "650", c: "rgb(22, 26, 33)" } }, // .dh h2
  { name: "dclose", at: "[role=dialog] button[aria-label=\"Đóng\"]", box: [988, 74, 32, 32], keys: "xywh", style: { r: "7px" } }, // .dh .ib
  { name: "dsub", at: "[role=dialog] > p@0", box: [400, 110, 640, 18.8], keys: "xywh", style: { fs: "13px", fw: "400", c: "rgb(95, 104, 117)" } }, // .dsub
  { name: "dsubMark", at: "[role=dialog] > p@0 >> span", box: [420, 111.4, 16, 16], keys: "xywh", style: { fs: "7px", fw: "700", c: "rgb(255, 255, 255)", r: "4px" } }, // .dsub .dm
  { name: "db", at: "[role=dialog] > div@1", box: [400, 128.8, 640, 592.7], keys: "xywh", style: {  } }, // .db
  { name: "addIn", at: "label[for=doc-share-add]", box: [420, 144.8, 424.7, 38], keys: "xywh", style: { r: "9px" } }, // .add .in
  { name: "addLvl", at: "button[aria-label^=\"Quyền khi mời\"]", box: [852.7, 144.8, 109.7, 38], keys: "xywh", style: { fs: "13px", fw: "500", c: "rgb(22, 26, 33)", r: "9px", bg: "rgb(255, 255, 255)" } }, // .add .lvl
  { name: "addBtn", at: "button[aria-label^=\"Quyền khi mời\"] + button", box: [970.4, 144.8, 49.6, 38], keys: "xywh", style: { fs: "13px", fw: "600", c: "rgb(255, 255, 255)", r: "7px", bg: "rgb(22, 102, 204)" } }, // .add .btn
  { name: "lbl0", at: "#doc-share-access", box: [420, 198.8, 600, 17.4], keys: "xywh", style: { fs: "12px", fw: "600", c: "rgb(95, 104, 117)" } }, // .lbl@0
  { name: "ga", at: "[role=radiogroup]", box: [420, 222.2, 600, 96], keys: "xywh", style: { r: "10px" } }, // .ga
  { name: "gaIc", at: "[role=radiogroup] > span@0", box: [433, 235.2, 34, 34], keys: "xywh", style: { fs: "11px", fw: "700", c: "rgb(255, 255, 255)", r: "9px" } }, // .ga .ic
  { name: "gaB", at: "[role=radiogroup] b", box: [479, 235.2, 383.3, 20.3], keys: "xyh", style: { fs: "14px", fw: "600", c: "rgb(22, 26, 33)" } }, // .ga b
  { name: "gaSmall", at: "[role=radiogroup] small", box: [479, 256.5, 383.3, 18.8], keys: "xyh", style: { fs: "12.5px", fw: "400", c: "rgb(95, 104, 117)" } }, // .ga small
  { name: "gaMap", at: "[role=radiogroup] [role=radio] > span@-1", box: [479, 283.3, 383.3, 22], keys: "xyh", style: {  } }, // .ga .map
  { name: "gaMap0", at: "[role=radiogroup] [role=radio] > span@-1 >> span", box: [479, 283.3, 117.4, 22], keys: "xywh", style: { fs: "11.5px", fw: "400", c: "rgb(79, 88, 102)", r: "5px", bg: "rgb(230, 233, 238)" } }, // .ga .map span@0
  { name: "gaCh", at: "button[aria-label=\"Đổi ai vào được trang\"]", box: [943.7, 235.2, 63.3, 30], keys: "xywh", style: { fs: "13px", fw: "500", c: "rgb(22, 26, 33)", r: "9px", bg: "rgb(255, 255, 255)" } }, // .ga .ch
  { name: "pr0", at: "[role=dialog] .min-h-\\[50px\\]@0", box: [420, 357.7, 600, 50], keys: "xywh", style: {  } }, // .pr@0
  { name: "pr0av", at: "[role=dialog] .min-h-\\[50px\\]@0 >> span", box: [420, 367.7, 30, 30], keys: "xywh", style: {  } }, // .pr@0 >> .av
  { name: "pr0who", at: "[role=dialog] .min-h-\\[50px\\]@0 >> span@1", box: [461, 364.2, 483.8, 37], keys: "xywh", style: {  } }, // .pr@0 >> .who
  { name: "pr0b", at: "[role=dialog] .min-h-\\[50px\\]@0 >> b", box: [461, 364.2, 483.8, 19.6], keys: "xywh", style: { fs: "13.5px", fw: "600", c: "rgb(22, 26, 33)" } }, // .pr@0 >> .who b
  { name: "pr0small", at: "[role=dialog] .min-h-\\[50px\\]@0 >> small", box: [461, 383.8, 483.8, 17.4], keys: "xywh", style: { fs: "12px", fw: "400", c: "rgb(95, 104, 117)" } }, // .pr@0 >> .who small
  { name: "pr0from", at: "[role=dialog] .min-h-\\[50px\\]@0 >> span.px-2\\.5", box: [955.8, 374, 64.2, 17.4], keys: "xywh", style: { fs: "12px", fw: "400", c: "rgb(95, 104, 117)" } }, // .pr@0 >> .from
  { name: "pnote", at: "[role=dialog] p.bg-doc-notice", box: [420, 563.7, 600, 57.5], keys: "xywh", style: { r: "9px", bg: "rgb(238, 244, 253)" } }, // .pnote
  { name: "lk", at: "[role=dialog] > div@1 >> :scope > div:last-child", box: [420, 637.2, 600, 78.3], keys: "xywh", style: { r: "10px", bg: "rgb(247, 248, 250)" } }, // .dlg .lk
  { name: "lkIc", at: "[role=dialog] > div@1 >> :scope > div:last-child >> span", box: [432, 658.3, 36, 36], keys: "xywh", style: { r: "9px", bg: "rgb(255, 255, 255)" } }, // .dlg .lk .ic
  { name: "lkB", at: "[role=dialog] > div@1 >> :scope > div:last-child >> b", box: [480, 649.2, 323, 19.6], keys: "xywh", style: { fs: "13.5px", fw: "600", c: "rgb(22, 26, 33)" } }, // .dlg .lk b
  { name: "lkOff", at: "[role=dialog] > div@1 >> :scope > div:last-child >> span.ml-auto", box: [815, 659, 193, 34.8], keys: "xywh", style: { fs: "12px", fw: "400", c: "rgb(95, 104, 117)" } }, // .dlg .lk .off
  { name: "df", at: "[role=dialog] > div@-1", box: [400, 735.5, 640, 61], keys: "xywh", style: {  } }, // .df
  { name: "dfA", at: "[role=dialog] > div@-1 >> a", box: [420, 757.5, 141.8, 18.1], keys: "xywh", style: { fs: "12.5px", fw: "500", c: "rgb(15, 86, 174)" } }, // .df a
  { name: "dfBtn0", at: "[role=dialog] > div@-1 >> button@0", box: [795.6, 750.5, 157.9, 32], keys: "xywh", style: { fs: "13px", fw: "500", c: "rgb(22, 26, 33)", r: "7px", bg: "rgb(255, 255, 255)" } }, // .df .btn@0
  { name: "dfBtnPri", at: "[role=dialog] > div@-1 >> button@1", box: [961.5, 750.5, 58.5, 32], keys: "xywh", style: { fs: "12.5px", fw: "600", c: "rgb(255, 255, 255)", r: "7px", bg: "rgb(22, 102, 204)" } }, // .df .btn.pri
];

/** Final-Docs state=phien-ban. Origin: main area. */
// prettier-ignore
const VERSIONS: Entry[] = [
  { name: "vbar", at: "#workspace-main header + div", box: [0, 49, 1108, 48], keys: "xywh", style: { bg: "rgb(238, 244, 253)" } }, // .vbar
  { name: "vbarIc", at: "#workspace-main header + div > svg", box: [24, 64.5, 16, 16], keys: "xywh", style: {  } }, // .vbar > .i
  { name: "vbarSpan", at: "#workspace-main header + div > span@0", box: [50, 63.1, 417.8, 18.8], keys: "xyh", style: { fs: "13px", fw: "400", c: "rgb(39, 45, 54)" } }, // .vbar > span@0
  { name: "vbarB", at: "#workspace-main header + div b@0", box: [181.9, 64.1, 74.7, 16], keys: "xyh", style: { fs: "13px", fw: "600", c: "rgb(22, 26, 33)" } }, // .vbar b@0
  { name: "vleg", at: "#workspace-main header + div > span@1", box: [483.8, 63.4, 226.4, 18.1], keys: "yhw", style: { fs: "12.5px", fw: "400", c: "rgb(95, 104, 117)" } }, // .vleg
  { name: "vlegIns", at: "#workspace-main header + div ins", box: [607.4, 63.4, 52.7, 18.1], keys: "ywh", style: { fs: "12.5px", fw: "400", c: "rgb(95, 104, 117)", r: "2px", bg: "rgb(226, 241, 231)" } }, // .vleg ins
  { name: "vbarPri", at: "#workspace-main header + div button@0", box: [837.8, 57.5, 183.6, 30], keys: "xywh", style: { fs: "13px", fw: "600", c: "rgb(255, 255, 255)", r: "7px", bg: "rgb(22, 102, 204)" } }, // .vbar .btn.pri
  { name: "vbarClose", at: "#workspace-main header + div button@1", box: [1031.3, 57.5, 60.7, 30], keys: "ywh", style: { fs: "13px", fw: "500", c: "rgb(22, 26, 33)", r: "7px", bg: "rgb(255, 255, 255)" } }, // .vbar .btn@1
  { name: "rp", at: "#workspace-main aside[aria-label=\"Lịch sử phiên bản\"]", box: [799, 97, 309, 1213.2], keys: "xyw", style: { bg: "rgb(255, 255, 255)" } }, // .rp
  { name: "vrh", at: "#workspace-main aside[aria-label=\"Lịch sử phiên bản\"] > div@0", box: [800, 97, 308, 45], keys: "xywh", style: {  } }, // .vrh
  { name: "vrh2", at: "#doc-versions-title", box: [816, 108.8, 120.8, 20.3], keys: "xywh", style: { fs: "14px", fw: "600", c: "rgb(22, 26, 33)" } }, // .vrh h2
  { name: "vrhIb", at: "#workspace-main aside button[aria-label=\"Đóng lịch sử phiên bản\"]", box: [1068, 103, 32, 32], keys: "xywh", style: { r: "7px" } }, // .vrh .ib
  { name: "rbody", at: "#workspace-main aside [role=region]", box: [800, 142, 308, 1168.2], keys: "xyw", style: {  } }, // .rbody
  { name: "vnote", at: "#workspace-main aside [role=region] > p", box: [816, 156, 276, 36], keys: "xywh", style: { fs: "12px", fw: "400", c: "rgb(95, 104, 117)" } }, // .vnote
  { name: "vfil", at: "#workspace-main aside [role=region] label", box: [816, 204, 276, 18.1], keys: "xywh", style: { fs: "12.5px", fw: "400", c: "rgb(79, 88, 102)" } }, // .vfil
  { name: "vfilIn", at: "#workspace-main aside [role=region] label input", box: [816, 205.6, 15, 15], keys: "xywh", style: {  } }, // .vfil input
  { name: "vdayH3", at: "#workspace-main aside [role=region] h3@0", box: [816, 240.1, 276, 18.1], keys: "xywh", style: { fs: "12.5px", fw: "600", c: "rgb(79, 88, 102)" } }, // .vday h3@0
  { name: "ver0", at: "#workspace-main aside [role=region] li button@0", box: [808, 262.3, 292, 72.6], keys: "xywh", style: { r: "8px" } }, // .ver@0
  { name: "ver0av", at: "#workspace-main aside [role=region] li button@0 >> span", box: [816, 271.3, 24, 24], keys: "xywh", style: {  } }, // .ver@0 >> .av
  { name: "ver0vt", at: "#workspace-main aside [role=region] li button@0 >> span@2", box: [849, 270.3, 243, 18.8], keys: "xywh", style: { fs: "13px", fw: "600", c: "rgb(22, 26, 33)" } }, // .ver@0 >> .vt
  { name: "ver0tag", at: "#workspace-main aside [role=region] li button@0 >> span@2 >> span", box: [891.9, 270.7, 54.1, 18], keys: "xywh", style: { fs: "11px", fw: "600", c: "rgb(79, 88, 102)", r: "4px", bg: "rgb(230, 233, 238)" } }, // .ver@0 >> .vtag
  { name: "ver0who", at: "#workspace-main aside [role=region] li button@0 >> span.block.mt-px", box: [849, 290.1, 243, 17.4], keys: "xywh", style: { fs: "12px", fw: "400", c: "rgb(79, 88, 102)" } }, // .ver@0 >> .who
  { name: "ver0small", at: "#workspace-main aside [role=region] li button@0 >> small", box: [849, 309.5, 243, 17.4], keys: "xywh", style: { fs: "12px", fw: "400", c: "rgb(95, 104, 117)" } }, // .ver@0 >> small
  { name: "verOn", at: "#workspace-main aside [role=region] li button[aria-current=true]", box: [808, 449.6, 292, 90], keys: "xw", style: { r: "8px", bg: "rgb(231, 240, 252)" } }, // .ver.on
];

/** Final-Docs state=loi-luu. Origin: main area. */
// prettier-ignore
const SAVE_ERROR: Entry[] = [
  { name: "bar", at: "#workspace-main header", box: [0, 0, 1108, 75], keys: "xw", style: {  } }, // .dbar
  { name: "saveBad", at: "#workspace-main header [role=alert]", box: [684.7, 6.9, 163.4, 18.1], keys: "wh", style: { fs: "12.5px", fw: "600", c: "rgb(196, 35, 64)" } }, // .save.bad
  { name: "saveBadIc", at: "#workspace-main header [role=alert] svg", box: [684.7, 9.5, 13, 13], keys: "wh", style: {  } }, // .save.bad svg
  { name: "retry", at: "#workspace-main header [role=alert] button", box: [802.5, 6.9, 45.6, 18.1], keys: "wh", style: { fs: "12.5px", fw: "600", c: "rgb(15, 86, 174)" } }, // .save.bad button
  { name: "share", at: "#workspace-main header button[aria-haspopup=\"dialog\"]", box: [1018.5, 0, 73.5, 32], keys: "wh", style: { fs: "13px", fw: "500", c: "rgb(22, 26, 33)", r: "7px", bg: "rgb(255, 255, 255)" } }, // .dbar .btn@0
];

test.describe("Document page geometry at 1440 px (owner)", () => {
  test.skip(
    persona !== "owner" || scenario !== "default",
    "Owner checks need the default owner world",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  for (const rail of ["pinned", undefined] as const) {
    test(`normal state, light, rail ${rail ?? "hidden"}: bar, tools, sheet, table and side panel`, async ({
      page,
    }) => {
      await openDoc(page, { rail });
      const shift = await whyShift(page);
      await check(page, rail ? NORMAL : NORMAL_HIDDEN, {
        adjust: articleAdjust(shift),
      });
      // An open checklist box: 1.5 px border in the --st-todo token.
      const box = page.locator("article [role=checkbox]").nth(1);
      // The 1.5 px width computes as 1 px at a device pixel ratio of 1.
      await expect(box).toHaveClass(/border-\[1\.5px\]/);
      expect(await colour(box, "border-top-color")).toBe("rgb(122, 131, 145)");
    });
  }

  test("normal state, dark: surfaces and ink", async ({ page }) => {
    await openDoc(page, { rail: "pinned", dark: true });
    const shift = await whyShift(page);
    await check(page, DARK, { adjust: articleAdjust(shift) });
  });

  test("File menu with the Download submenu", async ({ page }) => {
    await openDoc(page, { rail: "pinned" });
    await page.getByRole("menuitem", { name: "Tệp", exact: true }).click();
    await page.getByRole("menuitem", { name: /Tải xuống/ }).click();
    await check(page, FILE_MENU);
  });

  test("slash menu opened in a new paragraph", async ({ page }) => {
    await openDoc(page, { rail: "pinned" });
    await page.locator("p[data-block]").first().click();
    await page.keyboard.press("End");
    await page.keyboard.press("Enter");
    await expect(page.locator("p[data-block]")).toHaveCount(2);
    await page.keyboard.type("/danh");
    await expect(
      page.getByRole("listbox", { name: "Chèn khối" }),
    ).toBeVisible();
    await page.mouse.move(720, 20);
    await check(page, SLASH, { origin: { anchor: "[data-active-block]" } });
  });

  test("task mention list opened after @", async ({ page }) => {
    await openDoc(page, { rail: "pinned" });
    await page.locator("p[data-block]").first().click();
    await page.keyboard.press("End");
    await page.keyboard.type(" @ERP-14");
    await expect(
      page.getByRole("listbox", { name: "Gợi ý công việc" }),
    ).toBeVisible();
    await page.mouse.move(720, 20);
    await check(page, MENTION, { origin: { anchor: "[data-active-block]" } });
  });

  test("comments tab: thread, reply box and the marked text", async ({
    page,
  }) => {
    await openDoc(page, { rail: "pinned" });
    await page.getByRole("tab", { name: /Bình luận/ }).click();
    await check(page, COMMENTS);
    const mark = page.locator("article mark");
    await expect(mark).toHaveCount(1);
    expect(await colour(mark, "background-color")).toBe("rgb(252, 239, 196)");
    expect(await style(mark, "border-top-left-radius")).toBe("3px");
  });

  test("contents tab: the entry of the clicked heading is current", async ({
    page,
  }) => {
    await openDoc(page, { rail: "pinned" });
    await page.getByRole("tab", { name: /Mục lục/ }).click();
    await page.locator("aside nav a").nth(2).click();
    await page.mouse.move(720, 20);
    await check(page, CONTENTS);
  });

  test("share dialog", async ({ page }) => {
    await openDoc(page, { rail: "pinned" });
    await page.getByRole("button", { name: "Chia sẻ", exact: true }).click();
    const dialog = page.getByRole("dialog");
    await expect(dialog).toBeVisible();
    const rows = await dialog.locator(".min-h-\\[50px\\]").count();
    const missing = (MOCK_PEOPLE - rows) * PERSON_ROW;
    await check(page, SHARE, {
      origin: "viewport",
      adjust: (entry) => ({
        dy: AFTER_PEOPLE.has(entry.name) ? -missing : 0,
        dh: entry.name === "dlg" || entry.name === "db" ? -missing : 0,
      }),
    });
    // The page behind stays put under the scrim and Escape closes the dialog.
    await page.keyboard.press("Escape");
    await expect(dialog).toHaveCount(0);
  });

  test("version history replaces the tools and the side tabs", async ({
    page,
  }) => {
    await openDoc(page, { rail: "pinned" });
    await page.getByRole("menuitem", { name: "Tệp", exact: true }).click();
    await page.getByRole("menuitem", { name: /Lịch sử phiên bản/ }).click();
    await expect(
      page.getByRole("region", { name: "Đang xem phiên bản cũ" }),
    ).toBeVisible();
    await page.mouse.move(720, 20);
    await check(page, VERSIONS);
    await page.getByRole("button", { name: "Đóng", exact: true }).click();
    await expect(page.getByRole("toolbar")).toBeVisible();
  });

  test("save error: the bar reads the failure and offers a retry", async ({
    page,
  }) => {
    test.skip(
      !faults.includes("/blocks/block-m-00010=500"),
      "Needs VITE_MOCK_FAULTS=/blocks/block-m-00010=500",
    );
    await openDoc(page, { rail: "pinned" });
    const text = page
      .getByRole("checkbox", { name: /Đào tạo nhóm Công nợ/ })
      .locator("xpath=following-sibling::*[1]");
    await text.click();
    await page.keyboard.press("End");
    await page.keyboard.type("!");
    await page.getByRole("heading", { level: 1 }).click();
    await expect(page.getByRole("alert")).toBeVisible();
    await check(page, SAVE_ERROR);
  });
});

/**
 * The 320 px page against Final-Docs-2-320 and Final-Docs-2-320-Sheet (follows
 * Final-M-Docs): the 52 px bar, a closed panel that opens as a bottom sheet,
 * rows that scroll sideways, targets of at least 44 px and no sideways page
 * scroll.
 */
test.describe("Document page at 320 px (owner)", () => {
  test.skip(
    persona !== "owner" || scenario !== "default",
    "Owner checks need the default owner world",
  );
  test.use({
    viewport: { width: 320, height: 800 },
    hasTouch: true,
    isMobile: true,
  });

  const box = async (locator: Locator) => {
    const found = await locator.boundingBox();
    if (!found) throw new Error("no box");
    return found;
  };

  test("fits the width: bar, article, rows and panel", async ({ page }) => {
    await openDoc(page);
    const overflow = await page.evaluate(() => ({
      page: document.documentElement.scrollWidth,
      main: document.querySelector("#workspace-main")!.scrollWidth,
      view: document.documentElement.clientWidth,
    }));
    expect(overflow.page).toBeLessThanOrEqual(overflow.view);
    expect(overflow.main).toBeLessThanOrEqual(overflow.view);

    // The side panel is closed: no column and no sheet.
    await expect(page.locator("aside")).toHaveCount(0);
    await expect(page.getByRole("dialog")).toHaveCount(0);
    // The global bottom navigation does not exist on this page.
    await expect(
      page.getByRole("navigation", { name: /dưới|bottom/i }),
    ).toHaveCount(0);

    const bar = await box(page.locator("#workspace-main header"));
    expect(bar.y).toBeCloseTo(0, 0);
    expect(bar.height).toBeCloseTo(52, 0);
    const back = await box(page.getByRole("button", { name: /^Quay lại/ }));
    expect([back.width, back.height]).toEqual([44, 44]);
    expect(back.x).toBeCloseTo(6, 0);
    await expect(
      page.getByRole("button", { name: /^Bình luận, \d+ chuỗi đang mở$/ }),
    ).toBeVisible();
    await expect(
      page.getByRole("button", { name: /^Thêm: chia sẻ/ }),
    ).toBeVisible();
    // The meta line reads whole: saved, then who edited.
    const meta = page.getByRole("status").filter({ hasText: "Đã lưu" });
    await expect(meta).toBeVisible();
    const metaRow = meta.locator("xpath=..");
    expect(
      await metaRow.evaluate((el) => el.scrollWidth <= el.clientWidth),
    ).toBe(true);
    await expect(metaRow).toContainText("sửa lúc");

    const article = await box(page.locator("#workspace-main article"));
    expect(article.x).toBeCloseTo(16, 0);
    expect(article.width).toBeCloseTo(288, 0);
    const h1 = await box(page.getByRole("heading", { level: 1 }));
    expect(h1.width).toBeGreaterThan(280);
    expect(h1.height).toBeLessThanOrEqual(32 * 3);
    expect(
      await style(page.getByRole("heading", { level: 1 }), "font-size"),
    ).toBe("26px");
    expect(await style(page.locator("article h2").first(), "font-size")).toBe(
      "18px",
    );

    // The menu and tool rows scroll sideways with no scrollbar.
    for (const row of [
      page.locator('nav[aria-label="Menu tài liệu"], [role=menubar]').first(),
      page.getByRole("toolbar"),
    ]) {
      expect(await style(row, "overflow-x")).toBe("auto");
      expect(await style(row, "scrollbar-width")).toBe("none");
    }
    const tools = await box(page.getByRole("toolbar"));
    expect(tools.height).toBeCloseTo(52, 0);
  });

  test("every control is at least 44 px", async ({ page }) => {
    await openDoc(page);
    const small: string[] = [];
    const measure = async (name: string, locator: Locator) => {
      const total = await locator.count();
      for (let i = 0; i < total; i += 1) {
        const item = locator.nth(i);
        if (!(await item.isVisible())) continue;
        const found = await item.boundingBox();
        if (found && (found.height < 43.9 || found.width < 43.9)) {
          small.push(
            `${name}[${i}] ${found.width.toFixed(1)}x${found.height.toFixed(1)}`,
          );
        }
      }
    };
    await measure(
      "header buttons",
      page.locator("#workspace-main header button"),
    );
    await measure("menus", page.locator("[role=menubar] button"));
    await measure("tools", page.locator("[role=toolbar] [data-tool]"));
    await measure(
      "convert",
      page.locator('article button[aria-label^="Chuyển thành việc"]'),
    );
    await measure("live keys", page.locator("article [role=row] a.font-mono"));
    await measure(
      "live titles",
      page.locator("article [role=row] [role=cell] a:not(.font-mono)"),
    );
    expect(small, "controls under 44 px").toEqual([]);

    // The checkbox is 17 px drawn inside a 44 px target.
    const checkbox = page.locator("article [role=checkbox]").first();
    await checkbox.scrollIntoViewIfNeeded();
    const found = await box(checkbox);
    for (const [dx, dy] of [
      [-13, 0],
      [27, 0],
      [8, -13],
      [8, 28],
    ] as const) {
      const hit = await page.evaluate(
        ([x, y]) =>
          document.elementFromPoint(x, y)?.getAttribute("role") ?? null,
        [found.x + dx, found.y + dy] as const,
      );
      expect(hit, `hit area at ${dx},${dy}`).toBe("checkbox");
    }

    // An open checklist row shows "Chuyển thành việc" on its own line.
    const convert = await box(
      page
        .locator('article button[aria-label^="Chuyển thành việc"]:visible')
        .first(),
    );
    const row = page
      .locator("article div.group")
      .filter({
        has: page.locator('button[aria-label^="Chuyển thành việc"]:visible'),
      })
      .first();
    const text = await box(
      row.locator("[role=textbox], .doc-todo-text").first(),
    );
    expect(convert.y).toBeGreaterThanOrEqual(text.y + text.height - 1);
    expect(convert.height).toBeCloseTo(44, 0);
    expect((await box(row)).height).toBeGreaterThanOrEqual(44);
  });

  test("live table rows are two-line cards", async ({ page }) => {
    await openDoc(page);
    const row = page.locator("article [role=table] [role=row]").first();
    const key = await box(row.locator("a.font-mono"));
    const status = await box(row.locator("[role=cell]").nth(2));
    expect(status.y).toBeGreaterThanOrEqual(key.y + key.height - 1);
    const live = await box(
      page.locator("article div.overflow-hidden.rounded-\\[12px\\]"),
    );
    expect(live.x + live.width).toBeLessThanOrEqual(320 - 16 + 1);
  });

  test("Bình luận opens the panel as a bottom sheet", async ({ page }) => {
    await openDoc(page);
    const trigger = page.getByRole("button", {
      name: /^Bình luận, \d+ chuỗi đang mở$/,
    });
    await trigger.click();
    const sheet = page.getByRole("dialog", { name: "Bảng bên của tài liệu" });
    await expect(sheet).toBeVisible();
    const found = await box(sheet);
    expect(found.x).toBeCloseTo(0, 0);
    expect(found.width).toBeCloseTo(320, 0);
    expect(found.y + found.height).toBeCloseTo(800, 0);
    expect(found.height).toBeLessThanOrEqual(0.85 * 800 + 1);
    const close = await box(
      sheet.getByRole("button", { name: "Đóng bảng bên" }),
    );
    expect([close.width, close.height]).toEqual([44, 44]);
    const tabs = sheet.getByRole("tab");
    await expect(tabs.nth(2)).toHaveAttribute("aria-selected", "true");
    await expect(tabs.nth(0)).toHaveAttribute("aria-selected", "false");
    for (let i = 0; i < 3; i += 1) {
      expect((await box(tabs.nth(i))).height).toBeCloseTo(44, 0);
    }
    await tabs.nth(0).click();
    await expect(tabs.nth(0)).toHaveAttribute("aria-selected", "true");
    // The page behind does not scroll sideways under the sheet.
    await page.keyboard.press("Escape");
    await expect(sheet).toHaveCount(0);
    await expect(trigger).toBeFocused();
  });

  test("Thêm opens the overflow sheet with Share, Approve, Contents and History", async ({
    page,
  }) => {
    await openDoc(page);
    await expect(
      page.getByRole("button", { name: "Chia sẻ", exact: true }),
    ).toHaveCount(0);
    await page.getByRole("button", { name: /^Thêm: chia sẻ/ }).click();
    const sheet = page.getByRole("dialog", { name: "Thêm" });
    await expect(sheet).toBeVisible();
    for (const name of [
      "Chia sẻ",
      "Duyệt tài liệu",
      "Mục lục",
      "Lịch sử phiên bản",
    ]) {
      const item = sheet.getByRole("button", { name, exact: true });
      await expect(item).toBeVisible();
      expect((await box(item)).height).toBeGreaterThanOrEqual(44);
    }
    await sheet.getByRole("button", { name: "Mục lục", exact: true }).click();
    const panel = page.getByRole("dialog", { name: "Bảng bên của tài liệu" });
    await expect(panel.getByRole("tab", { name: /Mục lục/ })).toHaveAttribute(
      "aria-selected",
      "true",
    );
  });
});
