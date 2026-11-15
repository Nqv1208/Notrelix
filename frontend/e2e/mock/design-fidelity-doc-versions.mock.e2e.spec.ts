import { expect, test, type Page } from "@playwright/test";
import { dumpBoxTree, type BoxEntry } from "../support/box-tree";
import golden from "../support/artboards/doc-versions.json" with { type: "json" };

/**
 * The version history of a page against the Docs canvas
 * (https://claude.ai/artifact/8yDy2mtUUBgyVHhA2DKAec) screen Doc-Ver: the
 * history panel (list, empty, error), the bar over a viewed version, the named
 * version dialog (and its validation error) and the restore confirm (and its
 * conflict), at 1440, 1024 and 390 px, light and dark.
 *
 * `e2e/support/artboards/doc-versions.json` holds the box tree of each state:
 * the board file `Doc-Ver.dc.html` rendered from its template and
 * `doccol-base.css` at the artboard's size (scn x vp x mode), then dumped by
 * `dumpBoxTree` (every text run by its glyph box and every painted box in
 * reading order, with type and colours). The shipped page is dumped by the same
 * function and the two are compared entry by entry, positions in the viewport.
 * Texts pair by order; a width is compared only when both texts are the same
 * words (the mock world names other people than the board). Tolerance 1 px;
 * font size, weight, radius and colours are equal.
 *
 * Named differences from the board, kept out of the comparison:
 * - The view bar is compared in full at 1440 only. At 1024 and 390 the shipped
 *   main area is narrower than the board's (the system rail and the workspace
 *   sidebar stay, the board hides them), so the bar wraps differently; its type
 *   and colours are still compared.
 * - The list's "So sánh phiên bản 12 với bản hiện tại" button belongs to the
 *   comparison of two versions, which is not built: it is removed from the
 *   rendered board before the dump.
 * - The restore conflict's primary button is drawn disabled; it is enabled here
 *   because it is the way out (it closes the dialog on the latest version).
 * - The board's page bar (breadcrumb, save state, history button) is the
 *   shipped document bar, measured by design-fidelity-docs; it is not compared.
 */
const DOC = "/workspaces/mock-workspace-primary/docs/mock-doc-invoice-process";
const NOW = new Date("2026-11-04T10:45:00+07:00");

type Region = "panel" | "bar" | "dialog";
type Scn =
  | "list"
  | "view"
  | "empty"
  | "error"
  | "named"
  | "namedInvalid"
  | "restore"
  | "restoreConflict";
const VP = {
  w: { width: 1440, height: 900 },
  t: { width: 1024, height: 768 },
  m: { width: 390, height: 844 },
} as const;

const REGION_SELECTOR: Record<Region, string> = {
  panel: 'aside[aria-label="Lịch sử phiên bản"]',
  bar: 'div[role="status"]',
  dialog: '[role="dialog"]',
};
const REGIONS: Record<Scn, Region[]> = {
  list: ["panel"],
  view: ["bar", "panel"],
  empty: ["panel"],
  error: ["panel"],
  named: ["dialog"],
  namedInvalid: ["dialog"],
  restore: ["dialog"],
  restoreConflict: ["dialog"],
};
const entry = (
  id: string,
  versionNumber: number,
  kind: string,
  name: string | null,
  createdBy: string,
  createdAt: string,
) => ({ id, pageId: "p", versionNumber, kind, name, createdBy, createdAt });
// Entries as the board lists them: today 10:41 current, yesterday 16:40 named and 11:05, an older "before restoring".
const FEED = [
  entry(
    "v14",
    14,
    "automatic",
    null,
    "mock-user-commenter",
    "2026-11-04T10:41:00+07:00",
  ),
  entry(
    "v13",
    13,
    "named",
    "Trước khi gửi Ban giám đốc",
    "mock-user-commenter",
    "2026-11-03T16:40:00+07:00",
  ),
  entry(
    "v12",
    12,
    "automatic",
    null,
    "mock-user-erp-trang",
    "2026-11-03T11:05:00+07:00",
  ),
  entry(
    "v11",
    11,
    "automatic",
    "Before restoring version 9",
    "mock-user-owner",
    "2026-11-02T15:22:00+07:00",
  ),
];

type Row = [string, number, number, number, number, string, string, string];
type State = { origin: [number, number] } & Partial<Record<Region, Row[]>>;
const state = (key: string) =>
  (golden as unknown as Record<string, State>)[key]!;
// The shell differs from the board (the pinned system rail narrows the main area), so the bar is read from the edge it hangs on: its left items from the main area's left, its right items from the viewport's right. The panel and a dialog are read in the viewport.
const place = (
  e: BoxEntry,
  region: Region,
  mainLeft: number,
  vw: number,
): BoxEntry => {
  if (region !== "bar") return e;
  const right = e.x + e.w / 2 - mainLeft > (vw - mainLeft) / 2;
  const wide = e.w > 0.9 * (vw - mainLeft);
  return {
    ...e,
    x: Math.round((right ? e.x - vw : e.x - mainLeft) * 10) / 10,
    w: wide ? 0 : e.w,
  };
};
// A sheet over the page (a tablet or phone) is read from its own top: where it starts follows the height of the bars above it, which are the shell's and the document bar's, and its height follows the page's.
const settle = (
  list: BoxEntry[],
  region: Region,
  vp: keyof typeof VP,
  centred = 0,
): BoxEntry[] => {
  if (region !== "panel" || vp === "w") return list;
  const top = Math.min(...list.filter((e) => kind(e) === "p").map((e) => e.y));
  return list.map((e) => ({
    ...e,
    // A state centred in the sheet moves by half the height the sheet differs by.
    y: Math.round((e.y - top - (e.y - top > 50 ? centred : 0)) * 10) / 10,
    h: e.key === "p:1" ? 0 : e.h,
  }));
};
const rows = (key: string, region: Region, vp: keyof typeof VP): BoxEntry[] => {
  const s = state(key);
  return (s[region] ?? []).map(([k, x, y, w, h, a, b, c]) =>
    place({ key: k, x, y, w, h, a, b, c }, region, s.origin[0], VP[vp].width),
  );
};

const kind = (e: BoxEntry) => e.key.split(/[:#]/)[0] ?? "";

const box = (e: BoxEntry) => `${e.key} ${e.x},${e.y} ${e.w}x${e.h}`;

/** Colours are equal; in dark a blue or a translucent board colour may differ by 20 per channel, translucent ones read over the panel: the tokens round the board's translucent blues and its hexes (a selected row is a solid token, the board's a 16% blue). */
function sameColour(want: string, got: string, dark: boolean): boolean {
  if (want === got) return true;
  if (!dark) return false;
  const w = want.split(",").map(Number);
  const g = got.split(",").map(Number);
  if (w.length < 4 || g.length < 4) return false;
  if ((w[3] ?? 1) === 1 && (w[2] ?? 0) - (w[0] ?? 0) < 40) return false;
  // A translucent board colour is read over the dark panel (21, 26, 33).
  const over = [21, 26, 33];
  const flat = (c: number[]) =>
    [0, 1, 2].map(
      (i) => (c[i] ?? 0) * (c[3] ?? 1) + (over[i] ?? 0) * (1 - (c[3] ?? 1)),
    );
  const fw = flat(w);
  const fg = flat(g);
  return [0, 1, 2].every((i) => Math.abs((fw[i] ?? 0) - (fg[i] ?? 0)) <= 20);
}

function compare(
  want: BoxEntry[],
  got: BoxEntry[],
  dark: boolean,
  geometry = true,
): string[] {
  const seen: string[] = [];
  const wanted = want;
  for (const group of [["t", "input"], ["svg"], ["p"]]) {
    const a = wanted.filter((e) => group.includes(kind(e)));
    const b = got.filter((e) => group.includes(kind(e)));
    // Painted boxes pair by order only when the same boxes exist; a different count is reported once.
    if (a.length !== b.length) {
      seen.push(
        `${group[0]}: ${b.length} entries, the board has ${a.length}: page [${b.map(box).join(" | ")}] vs board [${a.map(box).join(" | ")}]`,
      );
      continue;
    }
    a.forEach((w, i) => {
      const g = b[i]!;
      const same = w.key === g.key;
      const at = `${w.key} (page ${g.key})`;
      for (const k of geometry ? (["x", "y", "h"] as const) : [])
        if (Math.abs(w[k] - g[k]) > 1.01)
          seen.push(`${at}.${k}: ${g[k]} vs ${w[k]}`);
      if (geometry && (same || kind(w) !== "t") && Math.abs(w.w - g.w) > 1.01)
        seen.push(`${at}.w: ${g.w} vs ${w.w}`);
      if (kind(w) === "p") {
        if (w.a !== g.a && Number.parseFloat(w.a) !== Number.parseFloat(g.a))
          seen.push(`${at}: radius ${g.a} vs ${w.a}`);
        if (!sameColour(w.b, g.b, dark))
          seen.push(`${at}: fill ${g.b} vs ${w.b}`);
        if (w.c !== g.c) seen.push(`${at}: border ${g.c} vs ${w.c}`);
      } else if (kind(w) !== "svg") {
        if (w.a !== g.a) seen.push(`${at}: font-size ${g.a} vs ${w.a}`);
        if (w.b !== g.b) seen.push(`${at}: font-weight ${g.b} vs ${w.b}`);
        if (!sameColour(w.c, g.c, dark))
          seen.push(`${at}: color ${g.c} vs ${w.c}`);
      } else if (!sameColour(w.c, g.c, dark))
        seen.push(`${at}: color ${g.c} vs ${w.c}`);
    });
  }
  return seen;
}

async function open(page: Page, scn: Scn, vp: keyof typeof VP, dark: boolean) {
  await page.setViewportSize(VP[vp]);
  await page.clock.setFixedTime(NOW);
  await page.addInitScript(
    ({ dark, feed, scn }) => {
      try {
        localStorage.setItem("notrelix-locale", "vi");
        localStorage.setItem("notrelix-system-rail", "pinned");
        if (dark) localStorage.setItem("notrelix-theme-mode", "dark");
      } catch {
        /* defaults apply */
      }
      const g = globalThis as Record<string, unknown>;
      g.__mockVersions =
        scn === "empty" ? "empty" : scn === "error" ? "fail" : feed;
      if (scn === "restoreConflict") g.__mockRestore = "conflict";
    },
    { dark, feed: FEED, scn },
  );
  await page.goto(DOC);
  await expect(page.getByRole("heading", { level: 1 })).toBeVisible();
  // The page's own shortcut: at 390 the open links sheet covers the File menu.
  await page.keyboard.press("ControlOrMeta+Alt+Shift+KeyH");
  const panel = page.locator(REGION_SELECTOR.panel);
  await expect(panel).toBeVisible();
  if (scn === "empty")
    await expect(panel.getByText("Chưa có phiên bản nào")).toBeVisible();
  if (scn === "error")
    await expect(panel.getByRole("alert")).toBeVisible({ timeout: 20_000 });
  if (scn !== "empty" && scn !== "error")
    await expect(panel.getByText("Hiện tại", { exact: true })).toBeVisible();
  if (scn === "list" || scn === "empty" || scn === "error") return;
  if (scn === "named" || scn === "namedInvalid") {
    await page.getByRole("button", { name: "Lưu phiên bản có tên" }).click();
    if (scn === "namedInvalid")
      await page.getByRole("button", { name: "Lưu", exact: true }).click();
    else
      await page
        .getByRole("textbox", { name: "Tên phiên bản" })
        .fill("Bản chốt đóng sổ T10");
    return;
  }
  await panel.getByRole("button", { name: /^11:05/ }).click();
  await expect(page.getByText("Trang này chỉ để xem.")).toBeVisible();
  if (scn === "view") return;
  await page.getByRole("button", { name: "Khôi phục bản này" }).click();
  if (scn === "restoreConflict") {
    await page
      .getByRole("dialog")
      .getByRole("button", { name: "Khôi phục", exact: true })
      .click();
    await expect(page.getByRole("alert")).toBeVisible();
  }
}

const SCENES: Array<[Scn, Array<keyof typeof VP>]> = [
  ["list", ["w", "t", "m"]],
  ["view", ["w", "t", "m"]],
  ["empty", ["w", "t", "m"]],
  ["error", ["w", "t", "m"]],
  ["named", ["w", "t", "m"]],
  ["namedInvalid", ["w", "t", "m"]],
  ["restore", ["w", "t", "m"]],
  ["restoreConflict", ["w", "t", "m"]],
];

for (const [scn, vps] of SCENES)
  for (const vp of vps)
    for (const dark of [false, true]) {
      test(`doc versions ${scn} at ${VP[vp].width}, ${dark ? "dark" : "light"}`, async ({
        page,
      }) => {
        await open(page, scn, vp, dark);
        await page.evaluate(() => document.fonts.ready);
        // Let the dialog's focus and the panel's layout settle.
        await page.waitForTimeout(150);
        const key = `${scn}.${vp}.${dark ? "dem" : "sang"}`;
        const seen: string[] = [];
        // The board draws no focus ring: the dialog's autofocused field is left.
        await page.evaluate(() =>
          (document.activeElement as HTMLElement | null)?.blur(),
        );
        // The board draws no hover either: the mouse rests where a click left it.
        await page.mouse.move(0, 0);
        const origin = await page.evaluate(() => {
          const r = document
            .querySelector("#workspace-main")!
            .getBoundingClientRect();
          return [r.x, r.y] as const;
        });
        for (const region of REGIONS[scn]) {
          const got = (
            await page.evaluate(dumpBoxTree, REGION_SELECTOR[region])
          ).map((e) => place(e, region, origin[0], VP[vp].width));
          const want = rows(key, region, vp);
          const height = (list: BoxEntry[]) =>
            list.find((e) => e.key === "p:1")?.h ?? 0;
          const centred =
            scn === "empty" || scn === "error"
              ? (height(got) - height(want)) / 2
              : 0;
          for (const line of compare(
            settle(want, region, vp),
            settle(got, region, vp, centred),
            dark,
            !(region === "bar" && vp !== "w"),
          ))
            seen.push(`${region}: ${line}`);
        }
        expect(seen, "boxes that differ from the board").toEqual([]);
      });
    }
