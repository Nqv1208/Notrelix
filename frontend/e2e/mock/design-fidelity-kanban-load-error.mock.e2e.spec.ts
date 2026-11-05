import { existsSync } from "node:fs";
import { pathToFileURL } from "node:url";
import { expect, test, type Page } from "@playwright/test";

/**
 * Kanban loading and load-error frames against the owner canvas board `BC-Kanban`
 * (`docs/design/mockups/2026-10/src/BC-Kanban.dc.html`), light (sang) and dark (dem). The artboard is opened as a
 * static file and read live, so no number is copied here. Tolerance is 1 px.
 *
 * - `kb-L` (VITE_MOCK_LATENCY=slow): the board header, tabs and toolbar stay and the content is four skeleton lanes:
 *   lane header 32 px high with a 90 x 12 bar, two cards 69 px high with 10/12/10 px bars, the bars' and the cards'
 *   colour and the lane tint.
 * - `kb-Er` (VITE_MOCK_FAULTS=/boards/mock-board-erp/full=network): the block "Không tải được board" (icon tile,
 *   title, text, "Thử lại") against the block, and the switched-off toolbar above it.
 *
 * Kept deviations (lead decision): `Final-Board` wins over this board for lane padding (8 px, not 12) and card width
 * (240, not 232), so lane and card widths and x offsets are not compared; the lane height follows from the padding.
 * The artboard toolbar shows an applied filter, a saved-filter bookmark and a sort that the app's idle toolbar does not
 * (they depend on the view's state, measured by the toolbar and filter specs), so only its presence is checked.
 */
const CANVAS = process.env.WM_CANVAS_DIR ?? "/tmp/wmv-canvas/project";
const SRC = `${process.cwd()}/../docs/design/mockups/2026-10/src`;
const FILE = "BC-Kanban.dc.html";
const dir = existsSync(`${CANVAS}/${FILE}`) ? CANVAS : SRC;
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const latency = process.env.VITE_MOCK_LATENCY ?? "";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";

test.skip(persona !== "owner" || scenario !== "default", "Owner world");
test.use({ viewport: { width: 1440, height: 900 } });

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

/** Evaluated in the page: any CSS colour as the opaque rgb() it paints over the page background. */
const RGB = `(v) => {
  const c = document.createElement("canvas").getContext("2d");
  c.fillStyle = "#fff";
  c.fillRect(0, 0, 1, 1);
  c.fillStyle = v;
  c.fillRect(0, 0, 1, 1);
  const d = c.getImageData(0, 0, 1, 1).data;
  return "rgb(" + d[0] + ", " + d[1] + ", " + d[2] + ")";
}`;

interface Skeleton {
  lanes: number;
  laneTints: string[];
  header: { height: number; barW: number; barH: number; barBg: string };
  cards: number[];
  cardBg: string;
  cardRadius: string;
  bars: number[][];
}

/** The skeleton lanes as the page shows them, with the selectors of each side. */
const READ_SKELETON = `(sel) => {
  const rgb = ${RGB};
  const lanes = [...sel.lanes()];
  const first = lanes[0];
  const box = (e) => e.getBoundingClientRect();
  const hdr = sel.header(first);
  const bar = hdr.firstElementChild;
  const cards = sel.cards(first);
  const bg = (e) => rgb(getComputedStyle(e).backgroundColor);
  return {
    lanes: lanes.length,
    laneTints: lanes.map(bg),
    header: { height: box(hdr).height, barW: box(bar).width, barH: box(bar).height, barBg: bg(bar) },
    cards: cards.map((c) => box(c).height),
    cardBg: bg(cards[0]),
    cardRadius: getComputedStyle(cards[0]).borderRadius,
    bars: cards.map((c) => [...c.children].map((b) => box(b).height)),
  };
}`;

async function artSkeleton(page: Page, frame: string): Promise<Skeleton> {
  await page.setViewportSize({ width: 1500, height: 900 });
  await page.goto(pathToFileURL(`${dir}/${FILE}`).href);
  await page.waitForSelector(`[data-frame="${frame}"] .lane`);
  return page.evaluate(
    ({ fn, frame: f }) => {
      const fr = document.querySelector(`[data-frame="${f}"]`)!;
      return (0, eval)(fn)({
        lanes: () => fr.querySelectorAll(".lane"),
        header: (lane: Element) => lane.querySelector(".lh"),
        cards: (lane: Element) => [...lane.querySelectorAll(".card")],
      });
    },
    { fn: READ_SKELETON, frame },
  ) as Promise<Skeleton>;
}

async function open(page: Page, dark: boolean) {
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
  await page.goto(`${BOARD}?view=kanban`);
}

for (const [mode, dark] of [
  ["sang", false],
  ["dem", true],
] as const) {
  test(`kb-L ${mode}: the shell stays and four skeleton lanes are drawn`, async ({
    page,
    browser,
  }) => {
    test.skip(
      latency !== "slow" || faults !== "",
      "Needs VITE_MOCK_LATENCY=slow",
    );
    const art = await browser.newPage();
    const want = await artSkeleton(art, `kb-L-${mode}`);
    await art.close();

    // The bars pulse for people who allow motion; the artboard draws them still.
    await page.emulateMedia({ reducedMotion: "reduce" });
    await open(page, dark);
    await page.waitForSelector("[data-skeleton-lane]");
    // One synchronous read: the 1.4 s window of the slow world is short.
    const { shell, got } = (await page.evaluate(
      (fn) => ({
        // The header, tabs and toolbar stay while the lanes load.
        shell: [
          "h1",
          "[role=tab]",
          "input[placeholder*='Tìm trong board']",
        ].map((q) => Boolean(document.querySelector(q))),
        got: (0, eval)(fn)({
          lanes: () => document.querySelectorAll("[data-skeleton-lane]"),
          header: (lane: Element) => lane.firstElementChild,
          cards: (lane: Element) => [
            ...lane.querySelectorAll("[data-skeleton-card]"),
          ],
        }),
      }),
      READ_SKELETON,
    )) as { shell: boolean[]; got: Skeleton };
    expect(shell, "header, tabs and toolbar stay").toEqual([true, true, true]);

    expect(got.lanes, "four lanes").toBe(want.lanes);
    // The dark lane tints follow Final-Board (the full-board spec measures them), not this board.
    if (!dark)
      got.laneTints.forEach((tint, i) =>
        same(tint, want.laneTints[i]!, `lane ${i} tint`),
      );
    expect(got.header.height, "lane header height").toBeCloseTo(
      want.header.height,
      0,
    );
    expect(got.header.barW, "header bar width").toBeCloseTo(
      want.header.barW,
      0,
    );
    expect(got.header.barH, "header bar height").toBeCloseTo(
      want.header.barH,
      0,
    );
    same(got.header.barBg, want.header.barBg, "bar colour");
    expect(got.cards, "card heights").toEqual(want.cards);
    got.bars.forEach((bars, i) =>
      expect(bars, `card ${i} bar heights`).toEqual(want.bars[i]),
    );
    // Dark card and lane surfaces follow Final-Board (lead decision), as the full-board spec measures them.
    if (!dark) same(got.cardBg, want.cardBg, "card colour");
  });
}

interface Part {
  y: number;
  h: number;
  w: number;
  cx: number;
  fs: string;
  fw: string;
}

const NOTE_PARTS = `(root) => {
  const o = root.getBoundingClientRect();
  const pick = (el) => {
    if (!el) return null;
    const b = el.getBoundingClientRect();
    const cs = getComputedStyle(el);
    return { y: b.y - o.y, h: b.height, w: b.width, cx: b.x + b.width / 2 - (o.x + o.width / 2), fs: cs.fontSize, fw: cs.fontWeight };
  };
  const kids = [...root.children];
  const act = kids.find((k) => k.querySelector("button") || k.tagName === "BUTTON");
  return {
    root: pick(root),
    icon: pick(kids[0]),
    title: pick(root.querySelector("b")),
    text: pick(kids[2]),
    action: pick(act ? act.querySelector("button") ?? act : null),
  };
}`;

for (const [mode, dark] of [
  ["sang", false],
  ["dem", true],
] as const) {
  test(`kb-Er ${mode}: the board did not load`, async ({ page, browser }) => {
    test.skip(
      !faults.includes("/boards/mock-board-erp/full=network"),
      "Fault world",
    );
    const art = await browser.newPage();
    await art.setViewportSize({ width: 1500, height: 900 });
    await art.goto(pathToFileURL(`${dir}/${FILE}`).href);
    const want = (await art.evaluate(
      ({ frame, fn }) => {
        const fr = document.querySelector(`[data-frame="${frame}"]`)!;
        const b = [...fr.querySelectorAll("b")].find(
          (e) => e.textContent?.trim() === "Không tải được board",
        )!;
        return (0, eval)(fn)(b.parentElement);
      },
      { frame: `kb-Er-${mode}`, fn: NOTE_PARTS },
    )) as Record<string, Part>;
    await art.close();

    await open(page, dark);
    const block = page.locator('[data-slot="board-state-block"]');
    await expect(block).toBeVisible({ timeout: 30_000 });
    const got = (await block.evaluate(
      (el, fn) => (0, eval)(fn)(el),
      NOTE_PARTS,
    )) as Record<string, Part>;
    for (const key of ["icon", "title", "text", "action"]) {
      const a = got[key]!;
      const e = want[key]!;
      expect(a.y, `${key} y`).toBeCloseTo(e.y, 0);
      expect(a.h, `${key} height`).toBeCloseTo(e.h, 0);
      expect(a.cx, `${key} centre`).toBeCloseTo(e.cx, 0);
      if (key === "icon") continue;
      expect(a.fs, `${key} font size`).toBe(e.fs);
      expect(a.fw, `${key} font weight`).toBe(e.fw);
    }
    expect(got.root!.h, "block height").toBeCloseTo(want.root!.h, 0);
    await expect(page.getByRole("button", { name: "Thử lại" })).toBeVisible();
    await expect(page.locator("[data-table-toolbar]")).toBeVisible();
  });
}
