import { expect, test, type Page } from "@playwright/test";
import { toNewest } from "./support/chat-newest";
import { vi as copy } from "../../packages/features/chat/src/web/components/chat.copy";

/**
 * The conversation header and the info panel against the Chat canvas boards `Chat-Info` (1440), `Chat-Info-M` (390) and
 * `Chat-Var-Info-Menu`, rendered light and dark (tweak mode=sang|dem), tolerance 1 px. Every number below was measured on
 * the rendered board: a 64 high header with 32 square icon buttons 4 apart and 20 from the right edge; a 360 panel with a
 * 32 square close cross at 12/12 (44 at 6/6 on a phone), a 72 avatar tile at 32, an 18/650 name at 116, a 13 px line at 144,
 * 44 round quick actions at 179 (columns 76 wide, 8 apart), 48 high section rows from 282 and 44 high member rows.
 * The two call buttons and the call quick actions are drawn on the boards but exist only behind the calls flag (off in
 * the app, so absent here; chat-header-actions.component.test.tsx proves them with the flag on).
 * Undrawn width 1024 is reviewed by assertion: the panel never overlaps the messages.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
test.skip(
  persona !== "owner" || scenario !== "default",
  "Needs the default owner world",
);

const CHANNEL =
  "/workspaces/mock-workspace-primary/chat/c1000000-0000-4000-8000-000000000001";

async function open(
  page: Page,
  width: number,
  height: number,
  mode: "light" | "dark",
) {
  await page.setViewportSize({ width, height });
  await page.addInitScript((m) => {
    try {
      window.localStorage.setItem("notrelix-locale", "vi");
      window.localStorage.setItem("notrelix-theme-mode", m);
    } catch {
      // Private mode: the defaults apply.
    }
  }, mode);
  await page.goto(CHANNEL);
  await expect(page.locator("[data-bubble]").first()).toBeVisible();
  await page.waitForTimeout(600);
  await toNewest(page);
}

async function box(page: Page, selector: string, nth = 0) {
  const b = await page.locator(selector).nth(nth).boundingBox();
  if (!b) throw new Error(`no box for ${selector}`);
  return { x: b.x, y: b.y, w: b.width, h: b.height };
}
async function css(page: Page, selector: string, prop: string) {
  return page
    .locator(selector)
    .first()
    .evaluate((el, p) => getComputedStyle(el).getPropertyValue(p), prop);
}
const near = (actual: number, expected: number, what: string) =>
  expect(Math.abs(actual - expected), what).toBeLessThanOrEqual(1);

const PANEL = "[data-testid=chat-side-panel]";
const INFO = "[data-testid=chat-info-panel]";
const HEADER = "main header";

/** Panel-relative box: the board numbers are relative to the panel's top-left corner. */
async function inPanel(page: Page, selector: string, nth = 0) {
  const p = await box(page, PANEL);
  const b = await box(page, selector, nth);
  return { x: b.x - p.x, y: b.y - p.y, w: b.w, h: b.h };
}

async function openInfo(page: Page) {
  await page
    .locator(HEADER)
    .getByRole("button", { name: copy.panel.channelInfo })
    .click();
  await expect(page.locator(INFO)).toBeVisible();
  await page.waitForTimeout(400);
}

for (const mode of ["light", "dark"] as const) {
  test.describe(`1440 ${mode}`, () => {
    test("header: 64 high, 18/650 name, 32 square buttons 4 apart and 20 from the right", async ({
      page,
    }) => {
      await open(page, 1440, 900, mode);
      const head = await box(page, HEADER);
      near(head.h, 64, "header height");
      expect(await css(page, `${HEADER} h1`, "font-size")).toBe("18px");
      expect(await css(page, `${HEADER} h1`, "font-weight")).toBe("650");
      expect(await css(page, `${HEADER} h1 + p`, "font-size")).toBe("12.5px");
      const names = [copy.search.open, copy.panel.channelInfo, copy.panel.more];
      const boxes = [];
      for (const name of names) {
        const b = await page
          .locator(HEADER)
          .getByRole("button", { name, exact: true })
          .boundingBox();
        expect(b, name).not.toBeNull();
        boxes.push(b!);
      }
      for (const b of boxes) {
        near(b.width, 32, "button width");
        near(b.height, 32, "button height");
        near(b.y - head.y, 15.5, "button top");
      }
      near(boxes[1]!.x - (boxes[0]!.x + 32), 4, "gap search/info");
      near(boxes[2]!.x - (boxes[1]!.x + 32), 4, "gap info/more");
      near(head.x + head.w - (boxes[2]!.x + 32), 20, "right padding");
      // A member of a channel has the voice call button in front of these three (a group call starts from the channel), and
      // no key hint on any button.
      await expect(
        page
          .locator(HEADER)
          .getByRole("button", { name: copy.panel.voiceCall }),
      ).toHaveCount(1);
      await expect(page.locator(`${HEADER} kbd`)).toHaveCount(0);
    });

    test("info panel: 360 wide, close cross alone at 12/12, 72 tile, name, line, 44 round actions, 48 section rows", async ({
      page,
    }) => {
      await open(page, 1440, 900, mode);
      await openInfo(page);
      near((await box(page, PANEL)).w, 360, "panel width");
      const x = await inPanel(page, `${INFO} > button`);
      near(x.x, 316, "close x");
      near(x.y, 12, "close y");
      near(x.w, 32, "close w");
      near(x.h, 32, "close h");
      const tile = await inPanel(page, `${INFO} [role=region] > div > span`);
      near(tile.x, 144.5, "tile x");
      near(tile.y, 32, "tile y");
      near(tile.w, 72, "tile w");
      near(tile.h, 72, "tile h");
      const name = await inPanel(page, "[data-testid=chat-info-title]");
      near(name.y, 116, "name y");
      near(name.h, 26, "name h");
      expect(
        await css(page, "[data-testid=chat-info-title]", "font-size"),
      ).toBe("18px");
      expect(
        await css(page, "[data-testid=chat-info-title]", "font-weight"),
      ).toBe("650");
      const sub = await inPanel(page, "[data-testid=chat-info-title] + p");
      near(sub.y, 144, "line y");
      near(sub.h, 19, "line h");
      expect(
        await css(page, "[data-testid=chat-info-title] + p", "font-size"),
      ).toBe("13px");
      // Two quick actions here (search, mute): 76 wide columns 8 apart, 44 round circles from 179.
      const cols = `${INFO} [role=region] > div:nth-of-type(2) > button`;
      const first = await inPanel(page, cols, 0);
      const second = await inPanel(page, cols, 1);
      near(first.y, 179, "action y");
      near(first.w, 76, "action column");
      near(second.x - (first.x + first.w), 8, "action gap");
      const circle = await inPanel(page, `${cols} > span`, 0);
      near(circle.w, 44, "circle w");
      near(circle.h, 44, "circle h");
      expect(
        parseFloat(await css(page, `${cols} > span`, "border-radius")),
      ).toBeGreaterThanOrEqual(22);
      expect(await css(page, cols, "font-size")).toBe("12px");
      expect(await css(page, cols, "font-weight")).toBe("600");
      const row = "[data-section=members] h3 > button";
      const head = await inPanel(page, row);
      near(head.y, 282, "first section y");
      near(head.h, 48, "section row h");
      near(head.x, 1, "section row x");
      expect(await css(page, row, "font-size")).toBe("14px");
      expect(await css(page, row, "font-weight")).toBe("650");
      const member = await inPanel(page, "[data-section=members] li", 0);
      near(member.y, 330, "first member y");
      near(member.h, 44, "member row h");
      near(member.x, 17, "member row x");
      // The four sections stack 48 high rows, each 1 px border apart.
      const next = await inPanel(page, "[data-section=pins] h3 > button");
      near(next.h, 48, "pins row h");
    });

    test("the menu: 248 wide, 36 high items, right edge on the button", async ({
      page,
    }) => {
      await open(page, 1440, 900, mode);
      const trigger = page
        .locator(HEADER)
        .getByRole("button", { name: copy.panel.more });
      // Measured before the menu opens: an open menu hides the page behind it from the accessibility tree.
      const more = await trigger.boundingBox();
      await trigger.click();
      const menu = page.getByRole("menu");
      await expect(menu).toBeVisible();
      // The menu zooms in over 100 ms: measure it at rest.
      await page.waitForTimeout(400);
      const m = await menu.boundingBox();
      near(m!.width, 248, "menu width");
      near(m!.x + m!.width, more!.x + more!.width, "menu right edge");
      const item = await menu.getByRole("menuitem").first().boundingBox();
      near(item!.height, 36, "item height");
    });
  });
}

for (const mode of ["light", "dark"] as const) {
  test(`390 ${mode}: the panel is a page with a 44 close cross at 6/6 and the board's rows`, async ({
    page,
  }) => {
    await open(page, 390, 844, mode);
    await page
      .getByRole("button", { name: copy.panel.channelInfo })
      .first()
      .click();
    await expect(page.locator(INFO)).toBeVisible();
    await page.waitForTimeout(400);
    const panel = await box(page, PANEL);
    near(panel.w, 390, "page width");
    const x = await inPanel(page, `${INFO} > button`);
    near(x.x, 340, "close x");
    near(x.y, 6, "close y");
    near(x.w, 44, "close w");
    near(x.h, 44, "close h");
    const tile = await inPanel(page, `${INFO} [role=region] > div > span`);
    near(tile.x, 159, "tile x");
    near(tile.y, 32, "tile y");
    const name = await inPanel(page, "[data-testid=chat-info-title]");
    near(name.y, 116, "name y");
    const sub = await inPanel(page, "[data-testid=chat-info-title] + p");
    near(sub.y, 144, "line y");
    const cols = `${INFO} [role=region] > div:nth-of-type(2) > button`;
    const circle = await inPanel(page, `${cols} > span`, 0);
    near(circle.w, 44, "circle w");
    near(circle.y, 179, "circle y");
    const head = await inPanel(page, "[data-section=members] h3 > button");
    near(head.y, 282, "section y");
    near(head.h, 48, "section h");
    near(head.w, 390, "section w");
    const member = await inPanel(page, "[data-section=members] li", 0);
    near(member.x, 16, "member x");
    near(member.y, 330, "member y");
    near(member.h, 44, "member h");
    expect(
      await page.evaluate(
        () =>
          document.documentElement.scrollWidth -
          document.documentElement.clientWidth,
      ),
    ).toBeLessThanOrEqual(0);
  });
}

for (const width of [1024, 1280]) {
  test(`${width}: the open panel never overlaps the messages and Esc closes it`, async ({
    page,
  }) => {
    await open(page, width, 800, "light");
    await openInfo(page);
    const panel = await box(page, PANEL);
    const log = await box(page, "[data-testid=chat-message-list]");
    expect(log.x + log.w).toBeLessThanOrEqual(panel.x + 1);
    expect(log.w).toBeGreaterThanOrEqual(360);
    await page
      .locator(INFO)
      .getByRole("button", { name: copy.pins.close })
      .focus();
    await page.keyboard.press("Escape");
    await expect(page.locator(PANEL)).toHaveCount(0);
  });
}
