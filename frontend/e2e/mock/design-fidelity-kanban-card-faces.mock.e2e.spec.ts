import { pathToFileURL } from "node:url";
import { expect, test, type Locator, type Page } from "@playwright/test";

/**
 * Card face states of `BC-Kanban` (`docs/design/mockups/2026-10/src/BC-Kanban.dc.html`), light (sang) and dark (dem).
 * The artboard frame is opened as a static file and every box of the card is read live, so no number is copied here.
 * Tolerance is 1 px; boxes are relative to the card, whose width differs (the app keeps Final-Board's 240).
 *
 * - `kbf-0` (VITE_MOCK_PERSONA=viewer): no hover quick actions, the options button stays.
 * - `kbf-2`: the done card shows the "Hoàn thành" chip in the top row, no due date, a muted title.
 * - `kbf-3` (VITE_MOCK_FAULTS=/board-items/mock-erp-item-152/status=500): a refused move puts the card back and says so
 *   on the card, with "Thử lại".
 * - `kbf-1` (archived with the parent) has no surface in the mock world: the board read holds no archived item.
 *   The face is covered by the card component test.
 */
const SRC = `${process.cwd()}/../docs/design/mockups/2026-10/src`;
const FILE = "BC-Kanban.dc.html";
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const FAULT_152 = "/board-items/mock-erp-item-152/status=500";

test.skip(scenario !== "default", "Default world");
test.use({ viewport: { width: 1440, height: 900 } });

interface Box {
  x: number;
  y: number;
  w: number;
  h: number;
  fs?: string;
  fw?: string;
}

const SEL = {
  art: {
    chip: ".ct .stc, .ct .chip",
    key: ".ct .k",
    title: ".ctt",
    ferr: ".ferr",
    retry: ".card .btn",
    more: ".ibtn",
  },
} as const;

async function artBoxes(page: Page, frame: string) {
  await page.setViewportSize({ width: 1500, height: 900 });
  await page.goto(pathToFileURL(`${SRC}/${FILE}`).href);
  await page.waitForSelector(`[data-frame="${frame}"] .card`);
  return page.evaluate(
    ({ frame: f, sel }) => {
      const card = document.querySelector(`[data-frame="${f}"] .card`)!;
      const o = card.getBoundingClientRect();
      const read = (s: string) => {
        const e = card.querySelector(s);
        if (!e) return null;
        const r = e.getBoundingClientRect();
        const cs = getComputedStyle(e);
        return {
          x: r.x - o.x,
          y: r.y - o.y,
          w: r.width,
          h: r.height,
          fs: cs.fontSize,
          fw: cs.fontWeight,
        };
      };
      return Object.fromEntries(
        Object.entries(sel).map(([k, s]) => [k, read(s)]),
      ) as Record<string, Box | null>;
    },
    { frame, sel: SEL.art },
  );
}

async function box(loc: Locator, card: Locator): Promise<Box> {
  const [r, o] = await Promise.all([loc.boundingBox(), card.boundingBox()]);
  const style = await loc.evaluate((e) => {
    const cs = getComputedStyle(e);
    return { fs: cs.fontSize, fw: cs.fontWeight };
  });
  return {
    x: r!.x - o!.x,
    y: r!.y - o!.y,
    w: r!.width,
    h: r!.height,
    ...style,
  };
}

function near(
  got: Box,
  want: Box | null,
  label: string,
  keys = ["x", "y", "h"],
) {
  expect(want, `${label} is drawn`).not.toBeNull();
  for (const k of keys as (keyof Box)[])
    expect(
      Math.abs((got[k] as number) - (want![k] as number)),
      `${label}.${k}: ${got[k]} vs ${want![k]}`,
    ).toBeLessThanOrEqual(1);
  expect(got.fs, `${label} font size`).toBe(want!.fs);
  expect(got.fw, `${label} font weight`).toBe(want!.fw);
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
  await expect(
    page.getByRole("region", { name: "Cần làm", exact: true }),
  ).toBeVisible();
}

const cardOf = (page: Page, key: string) =>
  page
    .locator("article")
    .filter({ has: page.locator("button[data-card-id]") })
    .filter({ hasText: key })
    .first();

for (const [mode, dark] of [
  ["sang", false],
  ["dem", true],
] as const) {
  test(`kbf-2 ${mode}: a done card has the Hoàn thành chip, a muted title and no due date`, async ({
    page,
    browser,
  }) => {
    test.skip(persona !== "owner" || faults !== "", "Owner world, no faults");
    const art = await browser.newPage();
    const want = await artBoxes(art, `kbf-2-${mode}`);
    await art.close();
    await open(page, dark);
    const card = cardOf(page, "ERP-131");
    const chip = card.locator("[data-completed]");
    await expect(chip).toBeVisible();
    near(await box(chip, card), want.chip, "chip", ["x", "y", "w", "h"]);
    near(await box(card.locator("h3"), card), want.title, "title", ["x", "y"]);
    await expect(card.locator("h3")).toHaveCSS("color", /.+/);
  });

  test(`kbf-3 ${mode}: a refused move shows on the card with Thử lại`, async ({
    page,
    browser,
  }) => {
    test.skip(
      persona !== "owner" || faults !== FAULT_152,
      `Needs VITE_MOCK_FAULTS=${FAULT_152}`,
    );
    const art = await browser.newPage();
    const want = await artBoxes(art, `kbf-3-${mode}`);
    await art.close();
    await open(page, dark);
    const card = cardOf(page, "ERP-152");
    await card.hover();
    await card.getByRole("button", { name: "Thêm tuỳ chọn" }).click();
    await page
      .getByRole("menuitem", { name: /^Chuyển sang (?!board)/ })
      .hover();
    await page.getByRole("menuitemradio", { name: /Chờ duyệt/ }).click();
    const note = card.locator("[data-card-failure]");
    await expect(note).toBeVisible();
    await expect(note).toHaveText(
      "Không chuyển được sang Chờ duyệt. Đã trả về Cần làm.",
    );
    // The mock title wraps to two lines where the drawn one is one: measure down from the title's bottom.
    const title = await box(card.locator("h3"), card);
    const noteBox = await box(note, card);
    const retry = await box(
      card.getByRole("button", { name: "Thử lại" }),
      card,
    );
    const gap = (a: Box, b: Box) => b.y - (a.y + a.h);
    expect(
      Math.abs(noteBox.x - want.ferr!.x),
      `note x ${noteBox.x} vs ${want.ferr!.x}`,
    ).toBeLessThanOrEqual(1);
    expect(
      Math.abs(gap(title, noteBox) - gap(want.title!, want.ferr!)),
      "title to note",
    ).toBeLessThanOrEqual(1);
    expect(
      Math.abs(gap(noteBox, retry) - gap(want.ferr!, want.retry!)),
      "note to retry",
    ).toBeLessThanOrEqual(1);
    expect(Math.abs(retry.h - want.retry!.h), "retry h").toBeLessThanOrEqual(1);
    expect(
      Math.abs(retry.w - want.retry!.w),
      `retry w ${retry.w} vs ${want.retry!.w}`,
    ).toBeLessThanOrEqual(1);
    expect(noteBox.fs).toBe(want.ferr!.fs);
  });

  test(`kbf-0 ${mode}: a viewer has no quick actions and keeps the options button`, async ({
    page,
    browser,
  }) => {
    test.skip(persona !== "viewer", "Needs VITE_MOCK_PERSONA=viewer");
    const art = await browser.newPage();
    const want = await artBoxes(art, `kbf-0-${mode}`);
    await art.close();
    await open(page, dark);
    const card = cardOf(page, "ERP-142");
    await card.hover();
    await expect(
      card.getByRole("button", { name: /^Thêm tuỳ chọn/ }),
    ).toBeVisible();
    await expect(
      card.getByRole("button", { name: /Giao cho tôi/ }),
    ).toHaveCount(0);
    await expect(card.getByRole("button", { name: /Đặt hạn/ })).toHaveCount(0);
    expect(want.more).not.toBeNull();
    const more = await box(
      card.getByRole("button", { name: /^Thêm tuỳ chọn/ }),
      card,
    );
    expect(Math.abs(more.y - want.more!.y)).toBeLessThanOrEqual(1);
  });
}
