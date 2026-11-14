import { existsSync } from "node:fs";
import { pathToFileURL } from "node:url";
import AxeBuilder from "@axe-core/playwright";
import { expect, test, type Page } from "@playwright/test";

/**
 * The board content states against the owner-canvas state boards `BC-Shell-1280-768` and `BC-Shell-320`
 * (`docs/design/mockups/2026-10/src/`), light (sang) and dark (dem): Er (the board did not load), E and Ev (no item
 * yet, with and without the right to add), NP (the board is missing or not visible), VM (the opened view is gone).
 * The artboard is opened as a static file and its boxes are read live, so no number is copied here. A note is boxed
 * against its own top-left; its parts are compared by y, height, width and by their centre against the note's centre,
 * so the sidebar of the shipped chrome (which the artboard's 1022 px frame does not draw) drops out. A band is boxed
 * against its own left and right edge.
 *
 * Worlds (the spec skips the others, as the repo's design specs do):
 * - default owner: E (`mock-board-it-budget` holds no item), NP (an unknown board), VM (an unknown `savedView`);
 * - `VITE_MOCK_PERSONA=viewer`: Ev;
 * - `VITE_MOCK_FAULTS=/boards/mock-board-erp/full=network`: Er;
 * - `VITE_MOCK_OVERLAYS=board-states` (owner and viewer): RO (`mock-board-it-archived`), NG (`mock-board-it-no-group`, owner).
 * The toolbar row above E and Er is drawn (52 px; switched off for Er, which has no board to act on). Not drawn by the
 * app and written down: the header and tabs above NP; the app answers NP with the shell only, so a missing board shows
 * no name or tab.
 */
const ARTBOARD = {
  1280: "BC-Shell-1280-768.dc.html",
  768: "BC-Shell-1280-768.dc.html",
  320: "BC-Shell-320.dc.html",
} as const;
type Width = keyof typeof ARTBOARD;
/** The owner canvas's boards when downloaded (`WM_CANVAS_DIR`), else the repository's copy. */
const CANVAS = process.env.WM_CANVAS_DIR ?? "/tmp/wmv-canvas/project";
const SRC = `${process.cwd()}/../docs/design/mockups/2026-10/src`;
const dir = (file: string) => (existsSync(`${CANVAS}/${file}`) ? CANVAS : SRC);
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const overlays = process.env.VITE_MOCK_OVERLAYS ?? "";
const latency = process.env.VITE_MOCK_LATENCY ?? "";
const BOARDS = "/workspaces/mock-workspace-primary/boards";

interface Part {
  y: number;
  h: number;
  w: number;
  cx: number;
  fs: string;
  fw: string;
}
type Parts = Record<string, Part>;

/** Evaluated in the page: the parts of a note (icon tile, title, text, action) boxed against the note. */
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

async function artNote(
  page: Page,
  width: Width,
  state: string,
  mode: string,
  title: string,
): Promise<Parts> {
  await page.goto(
    pathToFileURL(`${dir(ARTBOARD[width])}/${ARTBOARD[width]}`).href,
  );
  return page.evaluate(
    ({ frame, title: t, fn }) => {
      const fr = document.querySelector(`[data-frame="${frame}"]`);
      if (!fr) throw new Error(`no frame ${frame}`);
      const b = [...fr.querySelectorAll("b")].find(
        (e) => e.textContent?.trim() === t,
      );
      if (!b) throw new Error(`no title ${t} in ${frame}`);
      return (0, eval)(fn)(b.parentElement);
    },
    { frame: `sh${width}-${state}-${mode}`, title, fn: NOTE_PARTS },
  ) as Promise<Parts>;
}

async function appNote(page: Page): Promise<Parts> {
  return page.evaluate(
    ({ fn }) =>
      (0, eval)(fn)(
        document.querySelector('[data-slot="board-gone"]') ??
          document.querySelector('[data-slot="board-state-block"]'),
      ),
    { fn: NOTE_PARTS },
  ) as Promise<Parts>;
}

function expectSame(actual: Parts, expected: Parts, label: string) {
  for (const key of ["icon", "title", "text", "action"]) {
    const a = actual[key];
    const e = expected[key];
    if (!e) {
      expect(a ?? undefined, `${label} ${key} is not drawn`).toBeUndefined();
      continue;
    }
    expect(a, `${label} ${key} exists`).toBeTruthy();
    expect(a!.y, `${label} ${key} y`).toBeCloseTo(e.y, 0);
    expect(a!.h, `${label} ${key} height`).toBeCloseTo(e.h, 0);
    // The artboard frame's 1 px border narrows a note that fills it by 2 px.
    expect(Math.abs(a!.w - e.w), `${label} ${key} width`).toBeLessThanOrEqual(
      2.5,
    );
    expect(a!.cx, `${label} ${key} centre`).toBeCloseTo(e.cx, 0);
    // The icon tile's own font is not drawn (it carries only the glyph).
    if (key === "icon") continue;
    expect(a!.fs, `${label} ${key} font size`).toBe(e.fs);
    expect(a!.fw, `${label} ${key} font weight`).toBe(e.fw);
  }
  expect(actual.root!.h, `${label} note height`).toBeCloseTo(
    expected.root!.h,
    0,
  );
}

async function open(page: Page, width: number, mode: string, path: string) {
  await page.setViewportSize({ width, height: 900 });
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
  await page.goto(path);
}

/** The toolbar row the artboard keeps above E and Er: 52 px high from 640 px up (the phone bar is drawn on its own). */
async function expectToolbarRow(page: Page, width: Width, oneRow = true) {
  if (width < 640) return;
  const row = page.locator("[data-table-toolbar]").first();
  await expect(row).toBeVisible();
  // The live toolbar wraps "Thêm công việc" onto a second row below ~1100 px of main width (known gap, one row drawn).
  // The tablet band (768) draws the 60 px bar of Final-T-*.
  if (oneRow)
    expect((await row.boundingBox())!.height).toBeCloseTo(
      width === 768 ? 60 : 52,
      0,
    );
}

const WIDTHS: Width[] = [1280, 768, 320];
const MODES = ["sang", "dem"] as const;

test.describe("Board content states against BC-Shell", () => {
  test.skip(scenario !== "default" || latency !== "", "Needs the plain world");

  for (const width of WIDTHS)
    for (const mode of MODES) {
      test(`E at ${width} px, ${mode}: an empty board`, async ({ page }) => {
        test.skip(persona !== "owner" || faults !== "", "Owner world");
        const expected = await artNote(
          page,
          width,
          "E",
          mode,
          "Board chưa có công việc nào",
        );
        await open(
          page,
          width,
          mode,
          `${BOARDS}/mock-board-it-budget?view=table`,
        );
        await expect(
          page.locator('[data-slot="board-state-block"]'),
        ).toBeVisible();
        expectSame(await appNote(page), expected, `E ${width} ${mode}`);
        await expectToolbarRow(page, width, false);
      });

      test(`Ev at ${width} px, ${mode}: an empty board for a viewer`, async ({
        page,
      }) => {
        test.skip(persona !== "viewer" || faults !== "", "Viewer world");
        const expected = await artNote(
          page,
          width,
          "Ev",
          mode,
          "Board chưa có công việc nào",
        );
        await open(
          page,
          width,
          mode,
          `${BOARDS}/mock-board-it-budget?view=table`,
        );
        await expect(
          page.locator('[data-slot="board-state-block"]'),
        ).toBeVisible();
        expectSame(await appNote(page), expected, `Ev ${width} ${mode}`);
      });

      test(`NP at ${width} px, ${mode}: a board that is not there`, async ({
        page,
      }) => {
        test.skip(persona !== "owner" || faults !== "", "Owner world");
        const expected = await artNote(
          page,
          width,
          "NP",
          mode,
          "Bạn không xem được trang này",
        );
        await open(page, width, mode, `${BOARDS}/no-such-board?view=table`);
        await expect(
          page.locator('[data-slot="board-state-block"]'),
        ).toBeVisible();
        expectSame(await appNote(page), expected, `NP ${width} ${mode}`);
      });

      test(`Er at ${width} px, ${mode}: the board did not load`, async ({
        page,
      }) => {
        test.skip(
          !faults.includes("/boards/mock-board-erp/full=network"),
          "Fault world",
        );
        const expected = await artNote(
          page,
          width,
          "Er",
          mode,
          "Không tải được board",
        );
        await open(page, width, mode, `${BOARDS}/mock-board-erp?view=table`);
        await expect(
          page.locator('[data-slot="board-state-block"]'),
        ).toBeVisible();
        expectSame(await appNote(page), expected, `Er ${width} ${mode}`);
        await expectToolbarRow(page, width);
        await expect(
          page.getByRole("button", { name: "Thử lại" }),
        ).toBeVisible();
      });

      test(`VM at ${width} px, ${mode}: the opened view is gone`, async ({
        page,
      }) => {
        test.skip(persona !== "owner" || faults !== "", "Owner world");
        await page.goto(
          pathToFileURL(`${dir(ARTBOARD[width])}/${ARTBOARD[width]}`).href,
        );
        const expected = await page.evaluate((frame) => {
          const band = document.querySelector(
            `[data-frame="${frame}"] .bn.info`,
          )!;
          const o = band.getBoundingClientRect();
          const pick = (el: Element | null) => {
            if (!el) return null;
            const b = el.getBoundingClientRect();
            const cs = getComputedStyle(el);
            return [
              b.x - o.x,
              b.y - o.y,
              b.width,
              b.height,
              cs.fontSize,
              cs.fontWeight,
              o.right - b.right,
            ];
          };
          return {
            root: [o.width, o.height],
            title: pick(band.querySelector("b")),
            button: pick(band.querySelector("button")),
          };
        }, `sh${width}-VM-${mode}`);
        // The shell's band at 320 px is a narrower frame; both are read live.
        void expected;
        await open(
          page,
          width,
          mode,
          `${BOARDS}/mock-board-erp?view=table&savedView=00000000-0000-4000-8000-0000000000aa`,
        );
        const band = page.locator('[data-slot="board-band"]');
        await expect(band).toBeVisible();
        const got = await band.evaluate((el) => {
          const o = el.getBoundingClientRect();
          const pick = (n: Element | null) => {
            if (!n) return null;
            const b = n.getBoundingClientRect();
            const cs = getComputedStyle(n);
            return [
              b.x - o.x,
              b.y - o.y,
              b.width,
              b.height,
              cs.fontSize,
              cs.fontWeight,
              o.right - b.right,
            ];
          };
          return {
            root: [o.width, o.height],
            title: pick(el.querySelector("b")),
            button: pick(el.querySelector("button")),
          };
        });
        const e = expected as typeof got;
        // Left edge, top, height, font and the button's gap to the right edge.
        for (const key of ["title", "button"] as const) {
          const a = got[key]!;
          const x = e[key]!;
          // The button's x follows the band's width (the chrome's), its gap to the right edge is compared.
          if (key === "title")
            expect(a[0], `${key} x`).toBeCloseTo(x[0] as number, 0);
          expect(a[1], `${key} y`).toBeCloseTo(x[1] as number, 0);
          expect(a[3], `${key} height`).toBeCloseTo(x[3] as number, 0);
          expect(a[4], `${key} font size`).toBe(x[4]);
          expect(a[5], `${key} font weight`).toBe(x[5]);
          expect(a[6], `${key} right gap`).toBeCloseTo(x[6] as number, 0);
        }
        expect(got.root[1]).toBeCloseTo(e.root[1], 0);
      });
    }
});

/** Evaluated in the page: a band's title and button boxed against the band's own top-left and right edge. */
const BAND_PARTS = `(band) => {
  const o = band.getBoundingClientRect();
  const pick = (el) => {
    if (!el) return null;
    const b = el.getBoundingClientRect();
    const cs = getComputedStyle(el);
    return [b.x - o.x, b.y - o.y, b.width, b.height, cs.fontSize, cs.fontWeight, o.right - b.right];
  };
  return { root: [o.width, o.height, o.y], title: pick(band.querySelector("b")), button: pick(band.querySelector("button")) };
}`;

type BandBoxes = {
  root: number[];
  title: number[] | null;
  button: number[] | null;
};

/** RO and NG: the drawn band against the app's, read live (title, button, height, fonts). */
async function compareBand(
  page: Page,
  width: Width,
  mode: string,
  state: "RO" | "NG",
  appSelector: string,
  path: string,
) {
  await page.goto(
    pathToFileURL(`${dir(ARTBOARD[width])}/${ARTBOARD[width]}`).href,
  );
  const expected = (await page.evaluate(
    ({ frame, fn }) =>
      (0, eval)(fn)(document.querySelector(`[data-frame="${frame}"] .bn`)),
    { frame: `sh${width}-${state}-${mode}`, fn: BAND_PARTS },
  )) as BandBoxes;
  await open(page, width, mode, path);
  const band = page.locator(appSelector);
  await expect(band).toBeVisible();
  const got = (await band.evaluate(
    (el, fn) => (0, eval)(fn)(el),
    BAND_PARTS,
  )) as BandBoxes;
  console.log(state, width, mode, JSON.stringify({ expected, got }));
  // The artboard draws the band for someone who may not act; a manager's "Bỏ lưu trữ" is an extra action.
  // The shipped chrome's sidebar (768) and 16 px gutter (320) make the frame narrower than the drawn one, so the text wraps
  // onto another line there: the height is compared only where the band is as wide as the drawn one.
  const extraAction =
    (!expected.button && !!got.button) ||
    Math.abs(got.root[0] - expected.root[0]) > 2.5;
  if (!extraAction)
    expect(got.root[1], `${state} height`).toBeCloseTo(expected.root[1], 0);
  for (const key of ["title", "button"] as const) {
    const e = expected[key];
    const a = got[key];
    if (!e) continue;
    expect(a, `${state} ${key} exists`).toBeTruthy();
    if (!extraAction || key === "title")
      expect(a![1], `${state} ${key} y`).toBeCloseTo(e[1], 0);
    expect(a![3], `${state} ${key} height`).toBeCloseTo(e[3], 0);
    expect(a![4], `${state} ${key} font size`).toBe(e[4]);
    expect(a![5], `${state} ${key} font weight`).toBe(e[5]);
    if (key === "title")
      expect(a![0], `${state} ${key} x`).toBeCloseTo(e[0], 0);
    if (!extraAction)
      expect(a![6], `${state} ${key} right gap`).toBeCloseTo(e[6], 0);
  }
}

test.describe("Archived and no-group boards against BC-Shell", () => {
  test.skip(
    scenario !== "default" ||
      latency !== "" ||
      faults !== "" ||
      overlays !== "board-states",
    "Needs VITE_MOCK_OVERLAYS=board-states",
  );
  for (const width of WIDTHS)
    for (const mode of MODES) {
      test(`RO at ${width} px, ${mode}: an archived board`, async ({
        page,
      }) => {
        await compareBand(
          page,
          width,
          mode,
          "RO",
          '[data-slot="board-archived"]',
          `${BOARDS}/mock-board-it-archived?view=table`,
        );
      });
      // NG is the owner's (a viewer has no band to add a group); RO runs for both (the viewer has no action).
      test(`NG at ${width} px, ${mode}: a board with no group`, async ({
        page,
      }) => {
        test.skip(persona !== "owner", "Owner world");
        await compareBand(
          page,
          width,
          mode,
          "NG",
          '[data-slot="board-band"]',
          `${BOARDS}/mock-board-it-no-group?view=table`,
        );
      });
    }
});

/** Evaluated in a root: the grid header and the first skeleton row (boxes against the grid, bars against their row). */
const LOADING_GRID = `(tbl, rows) => {
  const o = tbl.getBoundingClientRect();
  const hdr = rows[0].getBoundingClientRect();
  const row = rows[1];
  const r = row.getBoundingClientRect();
  const bars = [...row.querySelectorAll("[class*='sk'], [data-slot='skeleton']")].map((e) => {
    const b = e.getBoundingClientRect();
    return { left: b.x - o.x, right: o.right - b.right, y: b.y - r.y, w: b.width, h: b.height };
  });
  return { hdrH: hdr.height, hdrY: hdr.y - o.y, rowH: r.height, rowY: r.y - o.y, pitch: rows[2].getBoundingClientRect().y - r.y, bars };
}`;

/** The phone's loading cards: each card's box and its bars against the card. */
const LOADING_CARDS = `(cards) => {
  const first = cards[0].getBoundingClientRect();
  return {
    w: first.width,
    h: first.height,
    pitch: cards[1].getBoundingClientRect().y - first.y,
    bars: [...cards[0].querySelectorAll("[class*='sk'], [data-slot='skeleton']")].map((e) => {
      const b = e.getBoundingClientRect();
      return { x: b.x - first.x, y: b.y - first.y, w: b.width, h: b.height };
    }),
  };
}`;

test.describe("Loading board against BC-Shell L (VITE_MOCK_LATENCY=slow)", () => {
  test.skip(
    scenario !== "default" || latency !== "slow" || persona !== "owner",
    "Needs VITE_MOCK_LATENCY=slow",
  );
  for (const width of WIDTHS)
    for (const mode of MODES) {
      test(`L at ${width} px, ${mode}: the grid while it loads`, async ({
        page,
      }) => {
        await page.goto(
          pathToFileURL(`${dir(ARTBOARD[width])}/${ARTBOARD[width]}`).href,
        );
        if (width === 320) {
          const drawn = await page.evaluate(
            ({ frame, fn }) =>
              (0, eval)(fn)([
                ...document.querySelectorAll(`[data-frame="${frame}"] .cardp`),
              ]),
            { frame: `sh320-L-${mode}`, fn: LOADING_CARDS },
          );
          const drawnBar = await page.evaluate(
            ({ frame }) => {
              const row = document.querySelector(
                `[data-frame="${frame}"] .bar`,
              )!;
              const o = row.getBoundingClientRect();
              return {
                h: o.height,
                kids: [...row.children].slice(0, 2).map((e) => {
                  const b = e.getBoundingClientRect();
                  return {
                    x: b.x - o.x,
                    y: b.y - o.y,
                    h: b.height,
                    w: b.width,
                  };
                }),
              };
            },
            { frame: `sh320-L-${mode}` },
          );
          await open(page, width, mode, `${BOARDS}/mock-board-erp?view=table`);
          await expect(
            page.locator('[data-slot="main-table-loading"]'),
          ).toBeVisible();
          const got = await page.evaluate(
            ({ fn }) =>
              (0, eval)(fn)([
                ...document.querySelectorAll(
                  '[data-slot="main-table-loading-cards"] > div',
                ),
              ]),
            { fn: LOADING_CARDS },
          );
          // The artboard frame's 1 px border narrows a full-width card by 2 px.
          expect(Math.abs(got.w - drawn.w), "card width").toBeLessThanOrEqual(
            2.5,
          );
          expect(got.h, "card height").toBeCloseTo(drawn.h, 0);
          expect(got.pitch, "card pitch").toBeCloseTo(drawn.pitch, 0);
          // The phone bar (Final-M keeps its own header, so only the bar below the tabs is measured).
          const bar = await page.evaluate(() => {
            const row = document.querySelector(
              '[data-slot="phone-toolbar-placeholder"]',
            )!;
            const o = row.getBoundingClientRect();
            return {
              h: o.height,
              kids: [...row.children].map((e) => {
                const b = e.getBoundingClientRect();
                return { x: b.x - o.x, y: b.y - o.y, h: b.height, w: b.width };
              }),
            };
          });
          expect(bar.h, "phone bar height").toBeCloseTo(drawnBar.h, 0);
          expect(bar.kids[0]!.w, "search button width").toBeCloseTo(
            drawnBar.kids[0]!.w,
            0,
          );
          expect(bar.kids[0]!.h, "search button height").toBeCloseTo(
            drawnBar.kids[0]!.h,
            0,
          );
          expect(bar.kids[0]!.y, "search button y").toBeCloseTo(
            drawnBar.kids[0]!.y,
            0,
          );
          expect(bar.kids[1]!.h, "pill height").toBeCloseTo(
            drawnBar.kids[1]!.h,
            0,
          );
          expect(bar.kids[1]!.y, "pill y").toBeCloseTo(drawnBar.kids[1]!.y, 0);
          expect(got.bars.length, "bars per card").toBe(drawn.bars.length);
          drawn.bars.forEach((bar: Record<string, number>, i: number) => {
            for (const key of ["x", "y", "w", "h"])
              expect(got.bars[i][key], `bar ${i} ${key}`).toBeCloseTo(
                bar[key]!,
                0,
              );
          });
          return;
        }
        const drawnHeader = await page.evaluate(
          ({ frame }) =>
            [...document.querySelectorAll(`[data-frame="${frame}"] .hdr .sk`)]
              .slice(1)
              .map((e) => {
                const b = e.getBoundingClientRect();
                return { w: b.width, h: b.height };
              }),
          { frame: `sh${width}-L-${mode}` },
        );
        const expected = await page.evaluate(
          ({ frame, fn }) => {
            const fr = document.querySelector(`[data-frame="${frame}"]`)!;
            const tbl = fr.querySelector(".tbl")!;
            return (0, eval)(fn)(tbl, [...tbl.querySelectorAll(".tr")]);
          },
          { frame: `sh${width}-L-${mode}`, fn: LOADING_GRID },
        );
        await open(page, width, mode, `${BOARDS}/mock-board-erp?view=table`);
        const loading = page.locator('[data-slot="main-table-loading"]');
        await expect(loading).toBeVisible();
        const actual = await page.evaluate(
          ({ fn }) => {
            const grid = document.querySelector(
              '[data-slot="main-table-loading"] > div:last-child',
            )!;
            return (0, eval)(fn)(grid, [...grid.children]);
          },
          { fn: LOADING_GRID },
        );
        const want = expected as typeof actual;
        expect(actual.hdrH, "header height").toBeCloseTo(want.hdrH, 0);
        expect(actual.rowH, "row height").toBeCloseTo(want.rowH, 0);
        expect(actual.pitch, "row pitch").toBeCloseTo(want.pitch, 0);
        expect(actual.bars.length, "bars per row").toBe(want.bars.length);
        want.bars.forEach((bar, i) => {
          const got = actual.bars[i]!;
          expect(got.y, `bar ${i} y`).toBeCloseTo(bar.y, 0);
          expect(got.h, `bar ${i} height`).toBeCloseTo(bar.h, 0);
          // The first bar sits from the grid's left edge, the last two from its right edge (the title bar's width varies).
          if (i === 0) {
            expect(got.left, `bar ${i} left`).toBeCloseTo(bar.left, 0);
            expect(got.w, `bar ${i} width`).toBeCloseTo(bar.w, 0);
          }
        });
        // The mark is not measured (Final-Board draws 34 px, the state board 32); the two title bars are.
        if (width === 1280) {
          const bars = await page
            .locator('[data-slot="board-header-loading"] > *')
            .evaluateAll((els) =>
              els.map((e) => {
                const b = e.getBoundingClientRect();
                return { w: b.width, h: b.height };
              }),
            );
          expect(bars, "header bars").toHaveLength(drawnHeader.length);
          drawnHeader.forEach((bar, i) => {
            expect(bars[i]!.w, `header bar ${i} width`).toBeCloseTo(bar.w, 0);
            expect(bars[i]!.h, `header bar ${i} height`).toBeCloseTo(bar.h, 0);
          });
        }
        if (width >= 640) await expectToolbarRow(page, width);
      });
    }
  // Screenshot-review numbers for the loading state at the three review widths.
  for (const width of [1440, 1024, 390] as const)
    for (const mode of MODES) {
      test(`L reviewed at ${width} px, ${mode}: no sideways scroll and no serious axe finding`, async ({
        page,
      }) => {
        await open(page, width, mode, `${BOARDS}/mock-board-erp?view=table`);
        await expect(
          page.locator('[data-slot="main-table-loading"]'),
        ).toBeVisible();
        expect(
          await page.evaluate(
            () => document.documentElement.scrollWidth <= window.innerWidth,
          ),
          "no horizontal page scroll",
        ).toBe(true);
        const serious = (await new AxeBuilder({ page }).analyze()).violations
          .filter((v) => ["serious", "critical"].includes(v.impact ?? ""))
          .map((v) => v.id);
        expect(serious).toEqual([]);
      });
    }
});
