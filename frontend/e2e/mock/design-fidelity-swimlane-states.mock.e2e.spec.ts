import { pathToFileURL } from "node:url";
import { expect, test, type Page } from "@playwright/test";

/**
 * Swimlane states of `WM-Swimlanes` (`docs/design/mockups/2026-10/src/WM-Swimlanes.dc.html`), light and dark (the
 * artboard's own `mode-dem`). The artboard is opened as a static file and read live; tolerance is 1 px.
 *
 * - "Đang kéo ... sang ô": the tip over the cell under the dragged card (left/right/top 8 px, 8 x 10 padding, 8 px radius,
 *   12.5 px text, a bold title and one line per change). Kept deviation: its fill and text are the inverse-surface tokens
 *   the app's toasts use (the artboard's toast hex is a few steps off them), so colours are not compared.
 * - "Từ chối · ...": the same tip on a refused cell, titled "Không thả được". The mock board holds no read-only lane
 *   field, so the refusal measured is the derived one (lane by a date bucket); the drawn text names a formula field.
 *   Kept deviation: the refusal fill is the destructive token, the artboard's toast-danger maroon is not a token.
 * - "Người chỉ xem" (VITE_MOCK_PERSONA=viewer): the warning band: 9 x 12 padding, 9 px radius, 12.5 px text, 16 px lock.
 *   Kept deviation: the band sits inside the app's 24 px main padding, the artboard's inside a 14 px card margin, so x
 *   and width are not compared.
 */
const SRC = `${process.cwd()}/../docs/design/mockups/2026-10/src`;
const FILE = "WM-Swimlanes.dc.html";
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
test.skip(scenario !== "default" || faults !== "", "Default world");
test.use({ viewport: { width: 1440, height: 1100 } });

const RGB = `(v) => {
  const c = document.createElement("canvas").getContext("2d");
  c.fillStyle = "#fff"; c.fillRect(0, 0, 1, 1); c.fillStyle = v; c.fillRect(0, 0, 1, 1);
  return [...c.getImageData(0, 0, 1, 1).data].slice(0, 3);
}`;

/** Evaluated in the page: a tip or band relative to its parent, with type, radius, padding and colours. */
const FACTS = `(e) => {
  const rgb = ${RGB};
  const o = e.parentElement.getBoundingClientRect();
  const r = e.getBoundingClientRect();
  const cs = getComputedStyle(e);
  const b = e.querySelector("b");
  const svg = e.querySelector("svg");
  return {
    x: r.x - o.x, y: r.y - o.y, w: r.width, h: r.height, rw: o.width,
    fs: cs.fontSize, lh: parseFloat(cs.lineHeight) / parseFloat(cs.fontSize), radius: cs.borderTopLeftRadius,
    pad: [cs.paddingTop, cs.paddingRight, cs.paddingBottom, cs.paddingLeft].join(" "),
    bg: rgb(cs.backgroundColor), color: rgb(cs.color),
    bold: b ? getComputedStyle(b).fontWeight : null, boldText: b ? b.textContent : null,
    icon: svg ? svg.getBoundingClientRect().width : null,
  };
}`;
interface Facts {
  x: number;
  y: number;
  w: number;
  h: number;
  rw: number;
  fs: string;
  lh: number;
  radius: string;
  pad: string;
  bg: number[];
  color: number[];
  bold: string | null;
  boldText: string | null;
  icon: number | null;
}

const near = (a: number[], b: number[], label: string) =>
  a.forEach((v, i) =>
    expect(Math.abs(v - b[i]!), `${label}: ${a} vs ${b}`).toBeLessThanOrEqual(
      2,
    ),
  );

async function art(page: Page, selector: string, dark: boolean) {
  await page.setViewportSize({ width: 1500, height: 900 });
  await page.goto(pathToFileURL(`${SRC}/${FILE}`).href);
  if (dark)
    await page.evaluate(() =>
      document.querySelector(".nx")!.classList.add("mode-dem"),
    );
  return page.evaluate(
    ({ fn, sel }) => (0, eval)(fn)(document.querySelector(sel)!),
    { fn: FACTS, sel: selector },
  ) as Promise<Facts>;
}

async function open(
  page: Page,
  dark: boolean,
  lane: RegExp,
  role: "menuitemradio" | "menuitem" = "menuitemradio",
) {
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
  await page.getByRole("button", { name: "Làn", exact: true }).click();
  await page.getByRole(role, { name: lane }).first().click();
  if (role === "menuitem")
    await page.getByRole("menuitemradio", { name: /^Tuần/ }).click();
  await page.waitForSelector("[data-swimlane-header]");
}

function compare(got: Facts, want: Facts, label: string, colours = true) {
  for (const k of ["x", "y"] as const)
    expect(Math.abs(got[k] - want[k]), `${label} ${k}`).toBeLessThanOrEqual(1);
  // The tip spans the cell less 16 px on both sides.
  expect(
    Math.abs(got.rw - got.w - (want.rw - want.w)),
    `${label} inset`,
  ).toBeLessThanOrEqual(1);
  expect(got.fs, `${label} font size`).toBe(want.fs);
  expect(got.lh, `${label} line height`).toBeCloseTo(want.lh, 1);
  expect(got.radius, `${label} radius`).toBe(want.radius);
  expect(got.pad, `${label} padding`).toBe(want.pad);
  expect(got.bold, `${label} title weight`).toBe(want.bold);
  expect(got.icon, `${label} icon`).toBe(want.icon);
  if (colours) {
    near(got.bg, want.bg, `${label} fill`);
    near(got.color, want.color, `${label} text`);
  }
}

for (const [mode, dark] of [
  ["light", false],
  ["dark", true],
] as const) {
  test(`drag tip ${mode}: a drop that changes column and lane`, async ({
    page,
    browser,
  }) => {
    test.skip(persona !== "owner", "Owner world");
    const a = await browser.newPage();
    const want = await art(a, ".cell.drop .tip", dark);
    await a.close();
    await open(page, dark, /Người thực hiện/);
    const cards = page.locator("[data-swimlane] article");
    const from = cards.first();
    const grab = (await from.boundingBox())!;
    await page.mouse.move(grab.x + grab.width / 2, grab.y + 20);
    await page.mouse.down();
    await page.mouse.move(grab.x + 20, grab.y + 40, { steps: 4 });
    // Over a cell of another lane and another column.
    const target = page.locator("[data-cell]").nth(7);
    const tb = (await target.boundingBox())!;
    await page.mouse.move(tb.x + tb.width / 2, tb.y + 30, { steps: 12 });
    const tip = target.locator("[role=status]");
    await expect(tip).toBeVisible();
    await page.waitForTimeout(150);
    const got = (await tip.evaluate(
      (e, fn) => (0, eval)(fn)(e),
      FACTS,
    )) as Facts;
    compare(got, want, "tip", false);
    expect(got.boldText).toBe(want.boldText);
    await page.mouse.up();
  });

  test(`reject ${mode}: a refused cell says why`, async ({ page, browser }) => {
    test.skip(persona !== "owner", "Owner world");
    const a = await browser.newPage();
    const want = await art(a, ".cell.refuse .tip", dark);
    await a.close();
    await open(page, dark, /Ngày nghiệm thu/, "menuitem");
    const cards = page.locator("[data-swimlane] article");
    const grab = (await cards.first().boundingBox())!;
    await page.mouse.move(grab.x + grab.width / 2, grab.y + 20);
    await page.mouse.down();
    await page.mouse.move(grab.x + 20, grab.y + 40, { steps: 4 });
    const target = page.locator("[data-cell][data-state=refuse]").first();
    const cell = page.locator("[data-cell]").nth(5);
    const cb = (await cell.boundingBox())!;
    await page.mouse.move(cb.x + cb.width / 2, cb.y + 30, { steps: 12 });
    await expect(target).toBeVisible();
    await page.waitForTimeout(150);
    const got = (await target
      .locator("[role=alert]")
      .evaluate((e, fn) => (0, eval)(fn)(e), FACTS)) as Facts;
    compare(got, want, "refusal", false);
    expect(got.boldText).toBe(want.boldText);
    await page.mouse.up();
  });

  test(`viewer banner ${mode}: the warning band above the toolbar`, async ({
    page,
    browser,
  }) => {
    test.skip(persona !== "viewer", "Needs VITE_MOCK_PERSONA=viewer");
    const a = await browser.newPage();
    const want = await art(a, ".warnbox", dark);
    await a.close();
    await open(page, dark, /Người thực hiện/);
    const band = page.locator("[data-lane-viewer-note]");
    await expect(band).toBeVisible();
    const got = (await band.evaluate(
      (e, fn) => (0, eval)(fn)(e),
      FACTS,
    )) as Facts;
    expect(Math.abs(got.h - want.h), "height").toBeLessThanOrEqual(1);
    expect(got.fs).toBe(want.fs);
    expect(got.radius).toBe(want.radius);
    expect(got.pad).toBe(want.pad);
    expect(got.icon).toBe(want.icon);
    // Kept deviation (dark): the warning text token is a lighter amber than the artboard's hex.
    if (!dark) near(got.color, want.color, "text");
  });
}
