import { existsSync } from "node:fs";
import { pathToFileURL } from "node:url";
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
 * - `VITE_MOCK_FAULTS=/boards/mock-board-erp/full=network`: Er.
 * Not drawn by the app and written down: the artboard keeps the toolbar row above E and Er and the header and tabs
 * above NP; the app has no toolbar while the board is unreadable and none worth showing above a board without items
 * (its toolbar filters and sorts items), and answers NP with the shell only, so a missing board shows no name or tab.
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

async function open(page: Page, width: Width, mode: string, path: string) {
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
