import { expect, test, type Locator, type Page } from "@playwright/test";

/**
 * Item page geometry against the October 2026 mockup. Every number comes from
 * `docs/design/mockups/2026-10/src/Final-Detail.dc.html` rendered at 1440x900
 * (`nav=hien-day`, `mode=sang`) and measured on the live DOM: position, size,
 * font, radius and colour of each element. The comment after each entry names
 * the mockup selector it was read from, and the block comment above each list
 * names the mockup state (`state=` prop) it comes from. Boxes are relative to
 * the main area's top-left, or to the `from` element's top-left for entries
 * inside a section; so the pinned and the hidden rail give the same numbers.
 * Tolerance is 1 px for boxes; font sizes, weights, radii and colours must be
 * equal.
 *
 * Text differs between the mock world and the mockup world, so an entry whose
 * width or position follows its wording lists only the keys that do not
 * (`keys`: x, y, w, h). Three data gaps shift the page and the spec accounts
 * for them instead of hiding them:
 * - the mock item has no "Phân hệ ERP", "Ước lượng", "Phụ thuộc" and "Phòng
 *   ban yêu cầu" properties (no producer), so rows below "Mức ưu tiên" sit
 *   `dy` higher (36 px per missing row);
 * - the activity read has no creator line, so the system list has no "Người
 *   tạo" row;
 * - the mock backend returns no sub-items, links or history events, so those
 *   rows are measured in Storybook, not here.
 *
 * The page clock is fixed to the fixtures' 12/10/2026 10:40. The conflict
 * checks run when the server was started with
 * `VITE_MOCK_FAULTS=/board-items/mock-erp-item-142/status=409`, the read-only
 * checks with `VITE_MOCK_PERSONA=commenter`.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const ITEM =
  "/workspaces/mock-workspace-primary/boards/mock-board-erp/items/mock-erp-item-142";
const NOW = new Date("2026-10-12T10:40:00+07:00");
const RAIL_KEY = "notrelix-system-rail";
/** Height of one property row: dt 34 + row gap 2. */
const ROW = 36;

type Key = "x" | "y" | "w" | "h";

interface Style {
  fs?: string;
  fw?: string;
  c?: string;
  r?: string;
  bg?: string;
  bd?: string;
}

interface Entry {
  name: string;
  /** CSS; `@i` picks the i-th match (-1 = last), ` >> ` chains a descendant. */
  at: string;
  /** Origin of the box: this element's top-left; the main area when absent. */
  from?: string;
  /** Extra shift of the expected y (rows the mock item has and this one lacks). */
  dy?: number;
  /** x, y, width, height in px relative to the origin. */
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

async function openItem(page: Page, search = "") {
  await page.clock.setFixedTime(NOW);
  await page.addInitScript(
    ({ railKey }) => {
      try {
        window.localStorage.setItem("notrelix-locale", "vi");
        window.localStorage.setItem(railKey, "pinned");
      } catch {
        // Private mode: the defaults apply.
      }
    },
    { railKey: RAIL_KEY },
  );
  await page.goto(`${ITEM}${search}`);
  await expect(page.getByRole("heading", { level: 1 })).toBeVisible();
  await expect(page.locator("#workspace-main aside")).toBeVisible();
  // Fonts settle before any text box is measured.
  await page.evaluate(() => document.fonts.ready);
}

async function mainOrigin(page: Page): Promise<{ x: number; y: number }> {
  const box = await page.locator("#workspace-main").boundingBox();
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

async function check(page: Page, entries: readonly Entry[]) {
  const main = await mainOrigin(page);
  const seen: string[] = [];
  for (const entry of entries) {
    const locator = locate(page, entry.at);
    await expect(locator, `${entry.name} exists`).toHaveCount(1);
    let origin = main;
    if (entry.from) {
      const anchor = await locate(page, entry.from).boundingBox();
      if (!anchor) throw new Error(`${entry.name}: anchor has no box`);
      origin = anchor;
    }
    const box = await locator.boundingBox();
    if (!box) throw new Error(`${entry.name} has no box`);
    const actual = {
      x: box.x - origin.x,
      y: box.y - origin.y,
      w: box.width,
      h: box.height,
    };
    const expected = {
      x: entry.box[0],
      y: entry.box[1] + (entry.dy ?? 0),
      w: entry.box[2],
      h: entry.box[3],
    };
    for (const key of entry.keys.split("") as Key[]) {
      if (Math.abs(actual[key] - expected[key]) > 1.01) {
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
    if (s.bd && (await colour(locator, "border-bottom-color")) !== s.bd)
      wrong.push("border");
    if (wrong.length > 0) seen.push(`${entry.name}: ${wrong.join(", ")}`);
  }
  expect(seen, "boxes that differ from the mockup").toEqual([]);
}

/**
 * Final-Detail state=tong-quan, nav=hien-day, mode=sang. Chrome shared by every
 * state: the page bar, the title block and the right column.
 */
// prettier-ignore
const COMMON: Entry[] = [
  { name: "topbar", at: "#workspace-main header@0", box: [0, 0, 1108, 53], keys: "xywh", style: { bg: "rgb(247, 248, 250)" } }, // main > .top
  { name: "back", at: "#workspace-main header a[aria-label=\"Về board\"]", box: [20, 10, 32, 32], keys: "xywh", style: { r: "7px" } }, // .top > a.icon-btn@0
  { name: "crumbMark", at: "#workspace-main header nav span[aria-hidden]@0", box: [58, 17, 18, 18], keys: "ywh", style: { fs: "8px", fw: "700", r: "6px" } }, // .crumb .dm
  { name: "crumbKey", at: "#workspace-main header nav [aria-current=page]", box: [353.3, 16.6, 54.6, 18.8], keys: "yh", style: { fs: "13px", fw: "500", c: "rgb(22, 26, 33)" } }, // .crumb .mono
  // y and h: the 20 px line was the K chip; without it the line is the 17.4 px text.
  { name: "hint", at: "#workspace-main header span.mr-1", box: [691.4, 17.3, 187.6, 17.4], keys: "yh", style: { fs: "12px", c: "rgb(95, 104, 117)" } }, // .top .hint
  { name: "prev", at: "#workspace-main header button[aria-label^=\"Không có việc trước\"]", box: [889, 10, 32, 32], keys: "xywh", style: { r: "7px" } }, // .top .icon-btn@1
  { name: "next", at: "#workspace-main header button[aria-label=\"Công việc sau (J)\"]", box: [927, 10, 32, 32], keys: "xywh", style: { r: "7px" } }, // .top .icon-btn@2
  { name: "sep", at: "#workspace-main header span.w-px", box: [971, 16, 1, 20], keys: "xywh", style: { bg: "rgb(225, 229, 234)" } }, // .top .vr
  { name: "collapse", at: "#workspace-main header button[aria-label=\"Thu về bảng bên\"]", box: [984, 10, 32, 32], keys: "xywh", style: { r: "7px" } }, // .top .icon-btn@3
  { name: "copy", at: "#workspace-main header button[aria-label=\"Sao chép liên kết\"]", box: [1022, 10, 32, 32], keys: "xywh", style: { r: "7px" } }, // .top .icon-btn@4
  { name: "more", at: "#workspace-main header button[aria-label=\"Thêm thao tác\"]", box: [1060, 10, 32, 32], keys: "xywh", style: { r: "7px" } }, // .top .icon-btn@5
  { name: "tag", at: "#workspace-main header@1 >> span@0", box: [40, 81, 54.3, 22], keys: "xyh", style: { fs: "11.5px", fw: "600", r: "6px", c: "rgb(18, 76, 142)" } }, // .ttl .tag
  { name: "h1", at: "#workspace-main h1", box: [40, 113, 708, 35.8], keys: "xywh", style: { fs: "28px", fw: "650", c: "rgb(22, 26, 33)" } }, // h1
  { name: "aside", at: "#workspace-main aside", box: [788, 53, 320, 1347], keys: "xyw", style: { bg: "rgb(255, 255, 255)" } }, // .rside
  { name: "propsHead", at: "#workspace-main aside > div@1 >> :scope > div@0", box: [807, 119, 283, 17.4], keys: "xywh", style: { fs: "12px", fw: "600", c: "rgb(95, 104, 117)" } }, // .rside .rh@0
  { name: "dtStatus", at: "#workspace-main aside dl@0 >> dt@0", box: [807, 140.4, 118, 34], keys: "xywh", style: { fs: "12.5px", c: "rgb(95, 104, 117)" } }, // .props dt@0
  { name: "pvStatus", at: "#workspace-main aside dl@0 >> button[aria-label=\"Trạng thái\"]", box: [917, 141.4, 116, 32], keys: "xywh", style: { fs: "13.5px", fw: "600", c: "rgb(22, 26, 33)", bg: "rgb(230, 239, 251)", r: "7px" } }, // .props dd@0 >> .pv
  { name: "dotStatus", at: "#workspace-main aside dl@0 >> button[aria-label=\"Trạng thái\"] > span[aria-hidden]", box: [925, 152.9, 9, 9], keys: "xywh", style: { bg: "rgb(30, 123, 224)", r: "999px" } }, // .props dd@0 >> .dot
  { name: "dtAssignee", at: "#workspace-main aside dl@0 >> dt@1", box: [807, 176.4, 118, 34], keys: "xywh", style: {  } }, // .props dt@1
  { name: "pvAssignee", at: "#workspace-main aside dl@0 >> button[aria-label=\"Người phụ trách\"]", box: [917, 177.4, 181, 32], keys: "xywh", style: { fs: "13.5px", r: "7px" } }, // .props dd@1 >> .pv
  { name: "dtStart", at: "#workspace-main aside dl@0 >> dt@2", box: [807, 212.4, 118, 34], keys: "xywh", style: {  } }, // .props dt@2
  { name: "pvStart", at: "#workspace-main aside dl@0 >> button[aria-label=\"Ngày bắt đầu\"]", box: [917, 213.4, 181, 32], keys: "xywh", style: { fs: "13.5px", fw: "500", c: "rgb(22, 26, 33)", r: "7px" } }, // .props dd@2 >> .pv
  { name: "pvPriority", at: "#workspace-main aside dl@0 >> button[aria-label=\"Mức ưu tiên\"]", box: [917, 285.4, 181, 32], keys: "xywh", style: { r: "7px" } }, // .props dd@4 >> .pv
  { name: "prio", at: "#workspace-main aside dl@0 >> button[aria-label=\"Mức ưu tiên\"] > span@0", box: [925, 292, 50.2, 18.8], keys: "yh", style: { fs: "13px", fw: "600" } }, // .props dd@4 >> .prio
  { name: "pvLabel", at: "#workspace-main aside dl@0 >> button[aria-label=\"Nhãn\"]", box: [917, 357.4, 181, 32], keys: "xwh", style: { r: "7px" } }, // .props dd@6 >> .pv
  { name: "pvProgress", at: "#workspace-main aside dl@0 >> [data-testid=\"item-page-progress\"]", box: [917, 429.4, 181, 32], keys: "xwh", style: { fs: "13.5px", fw: "500", r: "7px" } }, // .props dd@8 >> .pv
  { name: "mini", at: "#workspace-main aside dl@0 >> [data-testid=\"item-page-progress\"] > span@0", box: [925, 442.9, 60, 5], keys: "xwh", style: { bg: "rgb(230, 233, 238)", r: "3px" } }, // .props dd@8 >> .mini
  { name: "calc", at: "#workspace-main aside dl@0 >> [data-testid=\"item-page-progress\"] > span@1", box: [1029.1, 429.5, 60.9, 31.9], keys: "wh", style: { fs: "11px", c: "rgb(95, 104, 117)" } }, // .props dd@8 >> .calc
  { name: "rsep1", at: "#workspace-main aside > div.h-px@0", box: [807, 554.4, 283, 1], keys: "xywh", style: { bg: "rgb(225, 229, 234)" } }, // .rside > .rsep@0
  { name: "viewsHead", at: "#workspace-main aside > div@3 >> :scope > div@0", box: [807, 575.4, 283, 17.4], keys: "xywh", style: { fs: "12px", fw: "600", c: "rgb(95, 104, 117)" } }, // .views .rh
  { name: "viewKanban", at: "#workspace-main aside a[href*=\"view=kanban\"]", box: [799, 596.8, 299, 34], keys: "xywh", style: { fs: "13px", fw: "400", c: "rgb(79, 88, 102)", r: "7px" } }, // .views a@0
  { name: "viewTimeline", at: "#workspace-main aside a[href*=\"view=timeline\"]", box: [799, 698.8, 299, 34], keys: "xywh", style: { fs: "13px", c: "rgb(79, 88, 102)", r: "7px" } }, // .views a@3
  { name: "viewHint", at: "#workspace-main aside small", box: [807, 738.8, 283, 36], keys: "xywh", style: { fs: "12px", c: "rgb(95, 104, 117)" } }, // .views small
  { name: "rsep2", at: "#workspace-main aside > div.h-px@1", box: [807, 794.8, 283, 1], keys: "xywh", style: { bg: "rgb(225, 229, 234)" } }, // .rside > .rsep@1
  { name: "sysHead", at: "#workspace-main aside > div@5 >> :scope > div@0", box: [807, 815.8, 283, 17.4], keys: "xywh", style: { fs: "12px", fw: "600", c: "rgb(95, 104, 117)" } }, // .rside > div@5 >> .rh
  { name: "sysKeyDt", at: "#workspace-main aside dl@1 >> dt@0", box: [807, 837.2, 118, 18.1], keys: "xywh", style: { fs: "12.5px", c: "rgb(95, 104, 117)" } }, // .sysp dt@0
  { name: "sysKeyDd", at: "#workspace-main aside dl@1 >> dd@0", box: [925, 837.2, 165, 18.1], keys: "xywh", style: { fs: "12.5px", c: "rgb(79, 88, 102)" } }, // .sysp dd@0
];

/** Final-Detail state=tong-quan: the body sections and the activity with one comment. */
// prettier-ignore
const OVERVIEW: Entry[] = [
  { name: "descHead", at: "#workspace-main section[aria-labelledby=\"item-page-description\"] h2", box: [40, 178.8, 708, 21.8], keys: "xywh", style: { fs: "15px", fw: "650", c: "rgb(22, 26, 33)" } }, // #h-desc
  { name: "childHead", at: "#workspace-main section[aria-labelledby=\"item-page-children\"] h2", from: "#workspace-main section[aria-labelledby=\"item-page-children\"]", box: [0, 0, 708, 21.8], keys: "xywh", style: { fs: "15px", fw: "650", c: "rgb(22, 26, 33)" } }, // #h-sub
  { name: "childCount", at: "#workspace-main section[aria-labelledby=\"item-page-children\"] h2 > span", from: "#workspace-main section[aria-labelledby=\"item-page-children\"]", box: [73.9, 1.5, 23.6, 18.8], keys: "yh", style: { fs: "13px", c: "rgb(95, 104, 117)" } }, // #h-sub .n
  { name: "sbar", at: "#workspace-main section[aria-labelledby=\"item-page-children\"] > div@0", from: "#workspace-main section[aria-labelledby=\"item-page-children\"]", box: [0, 29.8, 708, 18.1], keys: "xywh", style: { fs: "12.5px", c: "rgb(95, 104, 117)" } }, // .sbar
  { name: "track", at: "#workspace-main section[aria-labelledby=\"item-page-children\"] [role=progressbar]", from: "#workspace-main section[aria-labelledby=\"item-page-children\"]", box: [0, 35.8, 160, 6], keys: "xywh", style: { bg: "rgb(230, 233, 238)", r: "3px" } }, // .sbar .track
  { name: "trackFill", at: "#workspace-main section[aria-labelledby=\"item-page-children\"] [role=progressbar] > span", from: "#workspace-main section[aria-labelledby=\"item-page-children\"]", box: [0, 35.8, 96, 6], keys: "xywh", style: { bg: "rgb(46, 139, 87)", r: "3px" } }, // .sbar .track span
  { name: "list", at: "#workspace-main section[aria-labelledby=\"item-page-children\"] [role=list]", from: "#workspace-main section[aria-labelledby=\"item-page-children\"]", box: [0, 55.9, 708, 259], keys: "xyw", style: { bg: "rgb(255, 255, 255)", r: "10px" } }, // .list
  { name: "linksSec", at: "#workspace-main section[aria-labelledby=\"item-page-links\"]", box: [40, 783.9, 342, 266.2], keys: "xw", style: {  } }, // .two > .sec@0
  { name: "linksHead", at: "#workspace-main section[aria-labelledby=\"item-page-links\"] h2", from: "#workspace-main section[aria-labelledby=\"item-page-links\"]", box: [0, 0, 342, 28], keys: "xywh", style: { fs: "15px", fw: "650", c: "rgb(22, 26, 33)" } }, // #h-rel
  { name: "linksAdd", at: "#workspace-main section[aria-labelledby=\"item-page-links\"] h2 button", from: "#workspace-main section[aria-labelledby=\"item-page-links\"]", box: [263.6, 0, 78.4, 28], keys: "xywh", style: { fs: "12.5px", fw: "500", c: "rgb(79, 88, 102)", r: "7px" } }, // #h-rel .btn
  { name: "filesSec", at: "#workspace-main section[aria-labelledby=\"item-page-files\"]", box: [406, 783.9, 342, 266.2], keys: "xw", style: {  } }, // .two > .sec@1
  { name: "filesHead", at: "#workspace-main section[aria-labelledby=\"item-page-files\"] h2", from: "#workspace-main section[aria-labelledby=\"item-page-files\"]", box: [0, 0, 342, 21.8], keys: "xywh", style: { fs: "15px", fw: "650", c: "rgb(22, 26, 33)" } }, // #h-file
  { name: "drop", at: "#workspace-main section[aria-labelledby=\"item-page-files\"] div[role=button]", from: "#workspace-main section[aria-labelledby=\"item-page-files\"]", box: [0, 184.1, 342, 42], keys: "xwh", style: { fs: "13px", c: "rgb(95, 104, 117)", r: "8px" } }, // .drop
  { name: "dropLink", at: "#workspace-main section[aria-labelledby=\"item-page-files\"] div[role=button] u", from: "#workspace-main section[aria-labelledby=\"item-page-files\"]", box: [228.2, 195.7, 56.9, 18.8], keys: "h", style: { fs: "13px", fw: "500", c: "rgb(15, 86, 174)" } }, // .drop u
  { name: "actBar", at: "#workspace-main #item-page-activity > section > div@0", from: "#workspace-main #item-page-activity > section", box: [0, 0, 708, 38], keys: "xywh", style: {  } }, // .act-h
  { name: "actHead", at: "#workspace-main #item-page-activity > section > div@0 >> h2", from: "#workspace-main #item-page-activity > section", box: [0, 7.6, 79.6, 21.8], keys: "xywh", style: { fs: "15px", fw: "650", c: "rgb(22, 26, 33)" } }, // .act-h h2
  { name: "tabOn", at: "#workspace-main #item-page-activity > section nav a[aria-current=page]", from: "#workspace-main #item-page-activity > section", box: [99.6, 0, 83.5, 38], keys: "xyh", style: { fs: "13.5px", fw: "600", c: "rgb(22, 26, 33)" } }, // .subtabs a.on
  { name: "tabOff", at: "#workspace-main #item-page-activity > section nav a@1", from: "#workspace-main #item-page-activity > section", box: [185.2, 0, 95.7, 38], keys: "yh", style: { fs: "13.5px", fw: "500", c: "rgb(79, 88, 102)" } }, // .subtabs a@1
  { name: "pill", at: "#workspace-main #item-page-activity > section > div@0 >> :scope > button", from: "#workspace-main #item-page-activity > section", box: [605.5, 4.5, 102.5, 28], keys: "xywh", style: { fs: "12.5px", fw: "400", c: "rgb(79, 88, 102)", r: "7px" } }, // .act-h .pill
  { name: "cmAvatar", at: "#workspace-main #item-page-activity > section div.group\\/comment@2 >> :scope > span", from: "#workspace-main #item-page-activity > section div.group\\/comment@2", box: [0, 8, 30, 30], keys: "xywh", style: { fs: "10.5px", fw: "700", r: "999px" } }, // .cm@2 >> .av
  { name: "cmBody", at: "#workspace-main #item-page-activity > section div.group\\/comment@2 >> :scope > div", from: "#workspace-main #item-page-activity > section div.group\\/comment@2", box: [42, 8, 666, 121.2], keys: "xywh", style: { bg: "rgb(255, 255, 255)", r: "10px" } }, // .cm@2 >> .body
  { name: "cmName", at: "#workspace-main #item-page-activity > section div.group\\/comment@2 >> b", from: "#workspace-main #item-page-activity > section div.group\\/comment@2", box: [57, 20, 108.5, 18], keys: "xyh", style: { fs: "14px", fw: "600", c: "rgb(22, 26, 33)" } }, // .cm@2 >> b
  { name: "cmTime", at: "#workspace-main #item-page-activity > section div.group\\/comment@2 >> time", from: "#workspace-main #item-page-activity > section div.group\\/comment@2", box: [171.5, 22, 32.2, 15], keys: "yh", style: { fs: "12px", c: "rgb(95, 104, 117)" } }, // .cm@2 >> time
  { name: "cmText", at: "#workspace-main #item-page-activity > section div.group\\/comment@2 >> p", from: "#workspace-main #item-page-activity > section div.group\\/comment@2", box: [57, 43.3, 636, 44.8], keys: "xywh", style: { fs: "14px", fw: "400", c: "rgb(22, 26, 33)" } }, // .cm@2 >> p
  { name: "cmReply", at: "#workspace-main #item-page-activity > section div.group\\/comment@2 >> button@1", from: "#workspace-main #item-page-activity > section div.group\\/comment@2", box: [51, 96.1, 49.8, 22.1], keys: "xywh", style: { fs: "12.5px", c: "rgb(95, 104, 117)", r: "5px" } }, // .cm@2 >> .re button@0
  { name: "cmReact", at: "#workspace-main #item-page-activity > section div.group\\/comment@2 >> button@2", from: "#workspace-main #item-page-activity > section div.group\\/comment@2", box: [102.8, 96.1, 90.4, 22.1], keys: "xywh", style: { fs: "12.5px", c: "rgb(95, 104, 117)", r: "5px" } }, // .cm@2 >> .re button@1
  { name: "cpAvatar", at: "#workspace-main #item-page-activity > section div.mt-3 > span", from: "#workspace-main #item-page-activity > section div.mt-3", box: [0, 0, 30, 30], keys: "xywh", style: { fs: "10.5px", fw: "700", r: "999px" } }, // .composer .av
  { name: "cpBox", at: "#workspace-main #item-page-activity > section div.mt-3 > div", from: "#workspace-main #item-page-activity > section div.mt-3", box: [42, 0, 666, 108], keys: "xywh", style: { bg: "rgb(255, 255, 255)", r: "10px" } }, // .composer .in
  { name: "cpText", at: "#workspace-main #item-page-activity > section div.mt-3 textarea", from: "#workspace-main #item-page-activity > section div.mt-3", box: [55, 11, 640, 48], keys: "xywh", style: { fs: "14px" } }, // .composer textarea
  { name: "cpAttach", at: "#workspace-main #item-page-activity > section div.mt-3 button[aria-label=\"Đính kèm tệp vào bình luận\"]", from: "#workspace-main #item-page-activity > section div.mt-3", box: [55, 69, 28, 28], keys: "xywh", style: { r: "7px" } }, // .composer .row .icon-btn
  { name: "cpSend", at: "#workspace-main #item-page-activity > section div.mt-3 button[class*=\"bg-primary\"]", from: "#workspace-main #item-page-activity > section div.mt-3", box: [652.1, 69, 42.9, 28], keys: "xywh", style: { fs: "12.5px", fw: "600", c: "rgb(255, 255, 255)", bg: "rgb(22, 102, 204)", r: "7px" } }, // .composer .btn
];

/** Final-Detail state=xung-dot: the conflict notice and the refused property. */
// prettier-ignore
const CONFLICT: Entry[] = [
  { name: "note", at: "#workspace-main div[role=alert]", box: [40, 178.8, 708, 104.5], keys: "xywh", style: { bg: "rgb(255, 244, 214)", r: "10px", bd: "rgb(235, 203, 122)", fs: "13.5px", c: "rgb(22, 26, 33)" } }, // .note
  { name: "noteIcon", at: "#workspace-main div[role=alert] > svg", box: [55, 193.8, 16, 16], keys: "xywh", style: {  } }, // .note > .i
  { name: "noteTitle", at: "#workspace-main div[role=alert] b", box: [83, 192.8, 458.8, 18], keys: "xyh", style: { fs: "13.5px", fw: "600", c: "rgb(22, 26, 33)" } }, // .note b
  { name: "btnTake", at: "#workspace-main div[role=alert] button@0", box: [83, 242.3, 156.7, 28], keys: "yh", style: { fs: "12.5px", fw: "600", c: "rgb(255, 255, 255)", bg: "rgb(22, 102, 204)", r: "7px", bd: "rgb(22, 102, 204)" } }, // .note .btn@0
  { name: "btnKeep", at: "#workspace-main div[role=alert] button@1", box: [245.7, 242.3, 135.6, 28], keys: "yh", style: { fs: "12.5px", fw: "500", c: "rgb(22, 26, 33)", bg: "rgb(255, 255, 255)", r: "7px", bd: "rgb(210, 215, 222)" } }, // .note .btn@1
  { name: "btnView", at: "#workspace-main div[role=alert] button@2", box: [387.3, 242.3, 103.2, 28], keys: "yh", style: { fs: "12.5px", fw: "500", c: "rgb(79, 88, 102)", r: "7px" } }, // .note .btn@2
  { name: "descHead", at: "#workspace-main section[aria-labelledby=\"item-page-description\"] h2", box: [40, 313.3, 708, 21.8], keys: "xywh", style: { fs: "15px", fw: "650", c: "rgb(22, 26, 33)" } }, // #h-desc
  { name: "pvConflict", at: "#workspace-main aside dl@0 >> button[aria-label=\"Trạng thái\"]", dy: -108, box: [917, 248.4, 181, 39.1], keys: "xw", style: { fs: "13.5px", fw: "600", c: "rgb(138, 90, 0)", bg: "rgb(255, 244, 214)", r: "7px" } }, // .props dd@3 >> .pv
];

/** Final-Detail state=chi-xem: the read-only notice and the disabled properties. */
// prettier-ignore
const READONLY: Entry[] = [
  { name: "note", at: "#workspace-main div[role=status]", box: [40, 178.8, 708, 86.8], keys: "xywh", style: { bg: "rgb(255, 255, 255)", r: "10px", bd: "rgb(210, 215, 222)", fs: "13.5px", c: "rgb(22, 26, 33)" } }, // .note
  { name: "noteIcon", at: "#workspace-main div[role=status] > svg", box: [55, 193.8, 16, 16], keys: "xywh", style: {  } }, // .note > .i
  { name: "noteLink", at: "#workspace-main div[role=status] button", box: [418.9, 232.3, 92.1, 20.3], keys: "wh", style: { fs: "13.5px", fw: "500", c: "rgb(15, 86, 174)" } }, // .note .lnk
  { name: "descHead", at: "#workspace-main section[aria-labelledby=\"item-page-description\"] h2", box: [40, 295.6, 708, 21.8], keys: "xywh", style: { fs: "15px", fw: "650", c: "rgb(22, 26, 33)" } }, // #h-desc
  { name: "pvStatus", at: "#workspace-main aside dl@0 >> button[aria-label=\"Trạng thái\"]", box: [917, 141.4, 116, 32], keys: "xywh", style: { fs: "13.5px", fw: "600", c: "rgb(22, 26, 33)", bg: "rgb(230, 239, 251)", r: "7px" } }, // .props dd@0 >> .pv
  { name: "pvDue", at: "#workspace-main aside dl@0 >> button[aria-label=\"Hạn chót\"]", box: [917, 249.4, 181, 32], keys: "xywh", style: { fs: "13.5px", fw: "600", c: "rgb(22, 26, 33)", r: "7px" } }, // .props dd@3 >> .pv
];

/** Final-Detail state=lich-su: the activity bar with the history tab selected. */
// prettier-ignore
const HISTORY: Entry[] = [
  { name: "actBar", at: "#workspace-main #item-page-activity > section > div@0", from: "#workspace-main #item-page-activity > section", box: [0, 0, 708, 38], keys: "xywh", style: {  } }, // .act-h
  { name: "tabOn", at: "#workspace-main #item-page-activity > section nav a[aria-current=page]", from: "#workspace-main #item-page-activity > section", box: [282.4, 0, 87.5, 38], keys: "yh", style: { fs: "13.5px", fw: "600", c: "rgb(22, 26, 33)", bd: "rgb(22, 102, 204)" } }, // .subtabs a.on
  { name: "tabAll", at: "#workspace-main #item-page-activity > section nav a@0", from: "#workspace-main #item-page-activity > section", box: [99.6, 0, 83.1, 38], keys: "xyh", style: { fs: "13.5px", fw: "500", c: "rgb(79, 88, 102)" } }, // .subtabs a@0
  { name: "pill", at: "#workspace-main #item-page-activity > section > div@0 >> :scope > button", from: "#workspace-main #item-page-activity > section", box: [608.2, 4.5, 99.8, 28], keys: "xywh", style: { fs: "12.5px", fw: "400", c: "rgb(79, 88, 102)", r: "7px" } }, // .act-h .pill
  { name: "pillChevron", at: "#workspace-main #item-page-activity > section > div@0 >> :scope > button svg", from: "#workspace-main #item-page-activity > section", box: [687, 12, 13, 13], keys: "xywh", style: {  } }, // .act-h .pill svg
];

/** Final-Detail state=binh-luan: the activity bar with the comments tab and one comment. */
// prettier-ignore
const COMMENTS: Entry[] = [
  { name: "actBar", at: "#workspace-main #item-page-activity > section > div@0", from: "#workspace-main #item-page-activity > section", box: [0, 0, 708, 38], keys: "xywh", style: {  } }, // .act-h
  { name: "tabOn", at: "#workspace-main #item-page-activity > section nav a[aria-current=page]", from: "#workspace-main #item-page-activity > section", box: [184.8, 0, 96.6, 38], keys: "yh", style: { fs: "13.5px", fw: "600", c: "rgb(22, 26, 33)", bd: "rgb(22, 102, 204)" } }, // .subtabs a.on
  { name: "pill", at: "#workspace-main #item-page-activity > section > div@0 >> :scope > button", from: "#workspace-main #item-page-activity > section", box: [605.5, 4.5, 102.5, 28], keys: "xywh", style: { fs: "12.5px", fw: "400", c: "rgb(79, 88, 102)", r: "7px" } }, // .act-h .pill
  { name: "cmAvatar", at: "#workspace-main #item-page-activity > section div.group\\/comment@3 >> :scope > span", from: "#workspace-main #item-page-activity > section div.group\\/comment@3", box: [0, 8, 30, 30], keys: "xywh", style: { fs: "10.5px", fw: "700", r: "999px" } }, // .cm@3 >> .av
  { name: "cmBody", at: "#workspace-main #item-page-activity > section div.group\\/comment@3 >> :scope > div", from: "#workspace-main #item-page-activity > section div.group\\/comment@3", box: [42, 8, 666, 121.2], keys: "xywh", style: { bg: "rgb(255, 255, 255)", r: "10px" } }, // .cm@3 >> .body
  { name: "cpBox", at: "#workspace-main #item-page-activity > section div.mt-3 > div", from: "#workspace-main #item-page-activity > section div.mt-3", box: [42, 0, 666, 108], keys: "xywh", style: { bg: "rgb(255, 255, 255)", r: "10px" } }, // .composer .in
];

test.describe("Item page geometry (Final-Detail, 1440x900)", () => {
  test.use({ viewport: { width: 1440, height: 900 } });
  test.skip(
    persona !== "owner" || scenario !== "default",
    "Needs the default owner scenario",
  );

  test("tong-quan: bar, title, sections, activity and right column", async ({
    page,
  }) => {
    await openItem(page);
    await check(page, [...COMMON, ...OVERVIEW]);
  });

  test("lich-su: history tab", async ({ page }) => {
    await openItem(page, "?tab=history");
    await check(page, [...COMMON, ...HISTORY]);
    // The history tab has no composer.
    await expect(page.getByLabel("Viết bình luận")).toHaveCount(0);
  });

  test("binh-luan: comments tab", async ({ page }) => {
    await openItem(page, "?tab=comments");
    await check(page, [...COMMON, ...COMMENTS]);
  });

  test("xung-dot: a refused write keeps both values and asks", async ({
    page,
  }) => {
    test.skip(
      !faults.includes("/board-items/mock-erp-item-142/status=409"),
      "Needs the server started with VITE_MOCK_FAULTS=/board-items/mock-erp-item-142/status=409",
    );
    await openItem(page);
    await page.getByLabel("Trạng thái").first().click();
    await page.getByRole("menuitem", { name: "Chờ duyệt" }).click();
    await expect(page.getByRole("alert")).toBeVisible();
    await check(page, [
      ...COMMON.filter((e) => e.name === "topbar"),
      ...CONFLICT,
    ]);
    // Taking the stored value drops the notice without a second write.
    await page.getByRole("button", { name: /^Lấy giá trị đã lưu/ }).click();
    await expect(page.getByRole("alert")).toHaveCount(0);
  });
});

test.describe("Item page geometry, read-only (Final-Detail chi-xem)", () => {
  test.use({ viewport: { width: 1440, height: 900 } });
  test.skip(
    persona !== "commenter",
    "Needs the server started with VITE_MOCK_PERSONA=commenter",
  );

  test("chi-xem: notice, disabled properties and no edit controls", async ({
    page,
  }) => {
    await openItem(page);
    await check(page, [
      ...COMMON.filter((e) => e.name === "topbar"),
      ...READONLY,
    ]);
    // The edit-only controls are not there, as in the mockup.
    await expect(
      page.getByRole("button", { name: "Quản lý trường" }),
    ).toHaveCount(0);
    await expect(
      page.getByPlaceholder("Thêm việc con, Enter để lưu"),
    ).toHaveCount(0);
    await expect(page.locator("#workspace-main div[role=button]")).toHaveCount(
      0,
    );
    // A commenter keeps the composer.
    await expect(page.getByLabel("Viết bình luận")).toBeEnabled();
  });
});
