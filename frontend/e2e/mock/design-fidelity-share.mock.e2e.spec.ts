import { expect, test, type Locator, type Page } from "@playwright/test";

/**
 * Board share dialog geometry against the October 2026 mockup. Every number
 * comes from `docs/design/mockups/2026-10/src/Final-Share.dc.html` rendered at
 * 1440x900 and measured with the box dump (position, size, padding, font,
 * radius, colour) for the states chia-se, doi-quyen, rieng-tu and nhat-ky and
 * the modes sang and dem. Boxes are read relative to the top-left of the
 * dialog (the dialog itself against the window: 640 wide, centred, 56 px from
 * the top). Tolerance is 1 px for boxes and exact for font sizes, weights,
 * radii and colours.
 *
 * The mock world differs from the mockup's: the ERP Board is in Công nghệ
 * with ten members, a team and a guest, and the access read gives no
 * permission level, expiry or history per entry. So the levels, the "outside
 * the company" badge with its address, the permission menu, the switch to
 * Private and the history are measured on the Storybook stories fed with the
 * mockup's data (four states, light and dark, within 1 px); here only boxes
 * that do not depend on that data are compared.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const NOW = new Date("2026-10-12T10:40:00+07:00");

interface Rect {
  readonly x: number;
  readonly y: number;
  readonly width: number;
  readonly height: number;
}

const COLOUR = {
  accent: "rgb(22, 102, 204)",
  accentText: "rgb(15, 86, 174)",
  panel: "rgb(255, 255, 255)",
  page: "rgb(247, 248, 250)",
  hover: "rgb(230, 233, 238)",
  line: "rgb(225, 229, 234)",
  lineStrong: "rgb(210, 215, 222)",
  ink: "rgb(22, 26, 33)",
  meta: "rgb(95, 104, 117)",
  muted: "rgb(79, 88, 102)",
} as const;

async function openShare(page: Page, dark = false) {
  await page.clock.setFixedTime(NOW);
  await page.addInitScript(
    (mode) => {
      try {
        window.localStorage.setItem("notrelix-locale", "vi");
        window.localStorage.setItem("notrelix-theme-mode", mode);
        window.localStorage.setItem("notrelix-system-rail", "pinned");
      } catch {
        // Private mode: the defaults apply.
      }
    },
    dark ? "dark" : "light",
  );
  await page.goto(BOARD);
  await page
    .getByRole("button", { name: /^Chia sẻ/ })
    .first()
    .click();
  await expect(page.locator('[data-f="dlg"]')).toBeVisible();
  // The dialog zooms in over 100 ms; measure it at rest.
  await page.waitForTimeout(250);
}

async function box(locator: Locator): Promise<Rect> {
  const found = await locator.boundingBox();
  if (!found) throw new Error("element has no box");
  return found;
}

/** A locator's box relative to the top-left of the dialog. */
async function rel(page: Page, locator: Locator): Promise<Rect> {
  const [o, found] = await Promise.all([
    box(page.locator('[data-f="dlg"]')),
    box(locator),
  ]);
  return {
    x: found.x - o.x,
    y: found.y - o.y,
    width: found.width,
    height: found.height,
  };
}

async function style(locator: Locator, property: string): Promise<string> {
  return locator.evaluate(
    (element, name) => getComputedStyle(element).getPropertyValue(name).trim(),
    property,
  );
}

async function colour(locator: Locator, property: string): Promise<string> {
  return locator.evaluate((element, name) => {
    const canvas = document.createElement("canvas");
    canvas.width = canvas.height = 1;
    const context = canvas.getContext("2d", { willReadFrequently: true })!;
    context.fillStyle = "#000";
    context.fillStyle = getComputedStyle(element).getPropertyValue(name);
    context.fillRect(0, 0, 1, 1);
    const [r, g, b] = context.getImageData(0, 0, 1, 1).data;
    return `rgb(${r}, ${g}, ${b})`;
  }, property);
}

function expectRect(actual: Rect, expected: Partial<Rect>, label: string) {
  for (const key of ["x", "y", "width", "height"] as const) {
    const want = expected[key];
    if (want === undefined) continue;
    expect(
      Math.abs(actual[key] - want),
      `${label}.${key}: ${actual[key].toFixed(1)} vs ${want}`,
    ).toBeLessThanOrEqual(1);
  }
}

const f = (page: Page, name: string) => page.locator(`[data-f="${name}"]`);

test.describe("Board share geometry at 1440 px (owner)", () => {
  test.skip(
    persona !== "owner" || scenario !== "default" || faults !== "",
    "Owner checks need the default owner world without faults",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  test("the dialog sits 56 px under the top, 640 wide and centred", async ({
    page,
  }) => {
    await openShare(page);
    const dialog = f(page, "dlg");
    expectRect(await box(dialog), { x: 400, y: 56, width: 640 }, "dialog");
    expect(await style(dialog, "border-radius")).toBe("14px");
    expect(await colour(dialog, "background-color")).toBe(COLOUR.panel);
    expect(await style(dialog, "box-shadow")).not.toBe("none");
    const head = f(page, "dh");
    expectRect(
      await rel(page, head),
      { x: 0, y: 0, width: 640, height: 54 },
      "head",
    );
    const title = f(page, "dt");
    expectRect(
      await rel(page, title),
      { x: 20, y: 20.95, height: 26.09 },
      "title",
    );
    expect(await style(title, "font-size")).toBe("18px");
    expect(await style(title, "font-weight")).toBe("650");
    expect(await colour(title, "color")).toBe(COLOUR.ink);
    const close = f(page, "ib").first();
    expectRect(
      await rel(page, close),
      { x: 588, y: 18, width: 32, height: 32 },
      "close",
    );
    expect(await style(close, "border-radius")).toBe("7px");
    const sub = f(page, "dsub");
    expectRect(
      await rel(page, sub),
      { x: 0, y: 54, width: 640, height: 18.84 },
      "subtitle",
    );
    expect(await style(sub, "font-size")).toBe("13px");
    expect(await colour(sub, "color")).toBe(COLOUR.meta);
    expectRect(
      await rel(page, f(page, "db")),
      { x: 0, y: 72.84, width: 640 },
      "body",
    );
  });

  test("the add row is live and the invite waits for a chosen member", async ({
    page,
  }) => {
    await openShare(page);
    const add = f(page, "add");
    expectRect(
      await rel(page, add),
      { x: 20, y: 88.84, width: 600, height: 40 },
      "row",
    );
    const field = f(page, "in");
    expectRect(
      await rel(page, field),
      { x: 20, y: 88.84, height: 40 },
      "field",
    );
    expect(await style(field, "border-radius")).toBe("9px");
    expect(await colour(field, "border-top-color")).toBe(COLOUR.lineStrong);
    expect(await style(field, "font-size")).toBe("13.5px");
    expect(await colour(field, "color")).toBe(COLOUR.meta);
    const level = f(page, "lvl");
    expectRect(
      await rel(page, level),
      { y: 88.84, width: 109.7, height: 38 },
      "level",
    );
    expect(await style(level, "border-radius")).toBe("9px");
    expect(await style(level, "font-size")).toBe("13px");
    expect(await style(level, "font-weight")).toBe("500");
    const invite = f(page, "invite");
    expectRect(
      await rel(page, invite),
      { y: 88.84, width: 49.64, height: 38 },
      "invite",
    );
    expect(await colour(invite, "background-color")).toBe(COLOUR.accent);
    expect(await style(invite, "font-weight")).toBe("600");
    // Live: the field takes typing and the invite stays off until a member
    // of the workspace is picked.
    await expect(invite).toBeDisabled();
    await expect(invite).not.toHaveAttribute("title", /chưa có thao tác ghi/);
    await expect(page.getByRole("combobox", { name: /./ })).toBeEditable();
    // 8 px between the three parts.
    const a = await rel(page, field);
    const b = await rel(page, level);
    expect(Math.abs(a.x + a.width + 8 - b.x)).toBeLessThanOrEqual(1);
  });

  test("who can open the Board through the department", async ({ page }) => {
    await openShare(page);
    const label = f(page, "lbl").first();
    expectRect(
      await rel(page, label),
      { x: 20, y: 144.84, width: 600, height: 17.39 },
      "label",
    );
    expect(await style(label, "font-size")).toBe("12px");
    expect(await style(label, "font-weight")).toBe("600");
    expect(await colour(label, "color")).toBe(COLOUR.meta);
    const card = f(page, "ga");
    expectRect(
      await rel(page, card),
      { x: 20, y: 168.23, width: 600, height: 96.05 },
      "card",
    );
    expect(await style(card, "border-radius")).toBe("10px");
    expect(await colour(card, "border-top-color")).toBe(COLOUR.accent);
    const mark = f(page, "gaic");
    expectRect(
      await rel(page, mark),
      { x: 33, y: 181.23, width: 34, height: 34 },
      "mark",
    );
    expect(await style(mark, "border-radius")).toBe("9px");
    const title = f(page, "gat");
    expectRect(
      await rel(page, title),
      { x: 79, y: 181.23, height: 20.3 },
      "name",
    );
    expect(await style(title, "font-size")).toBe("14px");
    expect(await style(title, "font-weight")).toBe("600");
    const hint = f(page, "gas");
    expect(await style(hint, "font-size")).toBe("12.5px");
    expect(await colour(hint, "color")).toBe(COLOUR.meta);
    const chips = f(page, "mapc");
    await expect(chips).toHaveCount(3);
    expectRect(
      await rel(page, chips.first()),
      { x: 79, height: 22 },
      "role map",
    );
    expect(await style(chips.first(), "border-radius")).toBe("5px");
    expect(await style(chips.first(), "font-size")).toBe("11.5px");
    expect(await colour(chips.first(), "background-color")).toBe(COLOUR.hover);
    expect(await colour(chips.first(), "color")).toBe(COLOUR.muted);
    const change = f(page, "ga").locator('[data-f="lvl-plain"]');
    expectRect(
      await rel(page, change),
      { x: 543.69, y: 181.23, width: 63.31, height: 30 },
      "change",
    );
    // Who can open the Board can be changed (to Private).
    await expect(change).not.toHaveAttribute("aria-disabled", "true");
  });

  test("the permission rows", async ({ page }) => {
    await openShare(page);
    const list = f(page, "pl");
    expectRect(await rel(page, list), { x: 12, y: 303.67, width: 616 }, "list");
    const rows = f(page, "pr");
    const first = rows.first();
    expectRect(
      await rel(page, first),
      { x: 20, y: 303.67, width: 600, height: 50 },
      "row",
    );
    const avatar = first.locator('[data-f="av"]');
    expectRect(
      await rel(page, avatar),
      { x: 20, y: 313.67, width: 30, height: 30 },
      "avatar",
    );
    expect(await style(avatar, "font-size")).toBe("10.5px");
    const name = first.locator('[data-f="wname"]');
    expectRect(
      await rel(page, name),
      { x: 61, y: 310.19, height: 19.56 },
      "name",
    );
    expect(await style(name, "font-size")).toBe("13.5px");
    expect(await style(name, "font-weight")).toBe("600");
    const sub = first.locator('[data-f="wsub"]');
    expect(await style(sub, "font-size")).toBe("12px");
    expect(await colour(sub, "color")).toBe(COLOUR.meta);
    const fixed = first.locator('[data-f="from"]');
    expect(await style(fixed, "font-size")).toBe("12px");
    expect(await colour(fixed, "color")).toBe(COLOUR.meta);
    const fixedBox = await rel(page, fixed);
    expect(Math.abs(fixedBox.x + fixedBox.width - 620)).toBeLessThanOrEqual(1);
    // A member who is not the owner has a live level button.
    const level = rows.nth(2).locator('[data-f="lvl-plain"]');
    expectRect(await rel(page, level), { height: 30 }, "level");
    expect(await style(level, "border-radius")).toBe("9px");
    expect(await style(level, "font-weight")).toBe("500");
    await expect(level).not.toHaveAttribute("aria-disabled", "true");
    const second = await rel(page, rows.nth(1));
    expect(Math.abs(second.y - (303.67 + 50))).toBeLessThanOrEqual(1);
    // Only the permissions given on the Board are listed: the five of the mockup.
    await expect(rows).toHaveCount(5);
    await expect(f(page, "plmore")).toHaveCount(0);
  });

  test("the Board link, the footer and the buttons", async ({ page }) => {
    await openShare(page);
    const link = f(page, "lk");
    const linkBox = await rel(page, link);
    expectRect(linkBox, { x: 20, width: 600, height: 78.34 }, "link block");
    expect(await style(link, "border-radius")).toBe("10px");
    expect(await colour(link, "background-color")).toBe(COLOUR.page);
    const icon = f(page, "lkic");
    expectRect(
      await rel(page, icon),
      { x: 32, y: linkBox.y + 21.17, width: 36, height: 36 },
      "link icon",
    );
    expect(await style(icon, "border-radius")).toBe("9px");
    expect(await colour(icon, "border-top-color")).toBe(COLOUR.line);
    const title = f(page, "lkt");
    expect(await style(title, "font-size")).toBe("13.5px");
    expect(await style(title, "font-weight")).toBe("600");
    const off = f(page, "off");
    expect(await style(off, "font-size")).toBe("12px");
    expect(await colour(off, "color")).toBe(COLOUR.meta);
    await expect(off).toContainText("quản trị workspace đã tắt");
    const foot = f(page, "df");
    const footBox = await rel(page, foot);
    expectRect(footBox, { x: 0, width: 640, height: 61 }, "footer");
    expect(await colour(foot, "border-top-color")).toBe(COLOUR.line);
    // The footer sits 14 px under the link block (its margin) and the dialog ends under it.
    expect(
      Math.abs(footBox.y - (linkBox.y + linkBox.height + 6 + 14)),
    ).toBeLessThanOrEqual(1);
    const dialogBox = await box(f(page, "dlg"));
    const footAbs = await box(foot);
    expect(
      Math.abs(dialogBox.y + dialogBox.height - (footAbs.y + footAbs.height)),
    ).toBeLessThanOrEqual(1);
    const log = f(page, "logl");
    expectRect(await rel(page, log), { x: 20, height: 18.13 }, "history link");
    expect(await style(log, "font-size")).toBe("12.5px");
    expect(await style(log, "font-weight")).toBe("500");
    expect(await colour(log, "color")).toBe(COLOUR.accentText);
    await expect(log).not.toHaveAttribute("aria-disabled", "true");
    const copy = f(page, "df").locator('[data-f="btn"]');
    expectRect(
      await rel(page, copy),
      { width: 157.86, height: 32 },
      "copy link",
    );
    expect(await style(copy, "font-size")).toBe("13px");
    expect(await style(copy, "border-radius")).toBe("7px");
    const done = f(page, "btn-pri");
    expectRect(
      await rel(page, done),
      { x: 561.48, width: 58.52, height: 32 },
      "done",
    );
    expect(await style(done, "font-size")).toBe("12.5px");
    expect(await style(done, "font-weight")).toBe("600");
    expect(await colour(done, "background-color")).toBe(COLOUR.accent);
    const copyBox = await rel(page, copy);
    const doneBox = await rel(page, done);
    expect(
      Math.abs(copyBox.x + copyBox.width + 8 - doneBox.x),
    ).toBeLessThanOrEqual(1);
  });
});

test.describe("Board share at smaller windows (owner)", () => {
  test.skip(
    persona !== "owner" || scenario !== "default" || faults !== "",
    "Owner checks need the default owner world without faults",
  );
  for (const size of [
    { width: 1280, height: 800 },
    { width: 1100, height: 760 },
  ]) {
    test(`the dialog stays inside ${size.width}x${size.height}`, async ({
      page,
    }) => {
      await page.setViewportSize(size);
      await openShare(page);
      const dialog = await box(f(page, "dlg"));
      expect(dialog.x).toBeGreaterThanOrEqual(0);
      expect(dialog.x + dialog.width).toBeLessThanOrEqual(size.width);
      expect(
        Math.abs(dialog.x - (size.width - dialog.width) / 2),
      ).toBeLessThanOrEqual(1);
      expect(Math.abs(dialog.y - 56)).toBeLessThanOrEqual(1);
      expect(dialog.y + dialog.height).toBeLessThanOrEqual(size.height);
    });
  }
});

/**
 * Final-M-Share (390 x 844), light (sang) and dark (dem), every drawn frame: `chia-se`, `doi-quyen` (the level
 * sheet), `rieng-tu`, `nhat-ky` and `da-sao-chep` (the copied toast). Each row is one box of the artboard rendered
 * with `render.py` in Chromium and read with the box dump: [x, y, width, height, font size, font weight, radius,
 * background, colour, border colour] relative to the sheet's top-left (the toast against the window). The mock
 * world carries the mockup's people, so the boxes are the artboard's own; the rows marked `data` compare x and
 * width only (the artboard's expiry text and a guest's level are not in the product: D76.4, GOV-030), `tone`
 * skips the colour (an avatar tone comes from the person), and the colours of the warning card and the "outside"
 * badge are skipped in dark because the mockup draws them with an alpha the design tokens do not carry. The
 * "Mời" button is not drawn at 390: a person picked in the search is added at once.
 */
type Measure = readonly [
  number,
  number,
  number,
  number,
  string,
  string,
  string,
  string,
  string,
  string,
];
interface FrameRow {
  readonly sel: string;
  readonly kind: string;
  readonly sang: Measure;
  readonly dem: Measure;
}
// prettier-ignore
const SHEET_FRAMES: Record<string, { readonly rows: readonly FrameRow[] }> = {"chia-se": {"rows": [{"sel": "[data-f=\"dlg\"]", "kind": "", "sang": [0, 0, 390, 756, "15px", "400", "14px 14px 0px 0px", "rgb(247, 248, 250)", "rgb(22, 26, 33)", ""], "dem": [0, 0, 390, 756, "15px", "400", "14px 14px 0px 0px", "rgb(14, 17, 22)", "rgb(230, 233, 238)", ""]}, {"sel": "[data-f=\"grab\"]", "kind": "", "sang": [177, 6, 36, 5, "15px", "400", "3px", "rgb(210, 215, 222)", "rgb(22, 26, 33)", ""], "dem": [177, 6, 36, 5, "15px", "400", "3px", "rgb(46, 55, 67)", "rgb(230, 233, 238)", ""]}, {"sel": "[data-f=\"dh\"]", "kind": "", "sang": [0, 13, 390, 52, "15px", "400", "0px", "transparent", "rgb(22, 26, 33)", ""], "dem": [0, 13, 390, 52, "15px", "400", "0px", "transparent", "rgb(230, 233, 238)", ""]}, {"sel": "[data-f=\"dt\"]", "kind": "", "sang": [20, 26.67, 314, 24.64, "17px", "650", "0px", "transparent", "rgb(22, 26, 33)", ""], "dem": [20, 26.67, 314, 24.64, "17px", "650", "0px", "transparent", "rgb(230, 233, 238)", ""]}, {"sel": "[data-f=\"ib\"]", "kind": "", "sang": [338, 17, 44, 44, "15px", "400", "12px", "transparent", "rgb(22, 26, 33)", ""], "dem": [338, 17, 44, 44, "15px", "400", "12px", "transparent", "rgb(230, 233, 238)", ""]}, {"sel": "[data-f=\"in\"]", "kind": "", "sang": [16, 71, 358, 50, "16px", "400", "12px", "rgb(255, 255, 255)", "rgb(22, 26, 33)", "rgb(210, 215, 222)"], "dem": [16, 71, 358, 50, "16px", "400", "12px", "rgb(21, 26, 33)", "rgb(230, 233, 238)", "rgb(46, 55, 67)"]}, {"sel": "#board-share-add-level", "kind": "", "sang": [20, 137.58, 97.88, 18.84, "13px", "400", "0px", "transparent", "rgb(95, 104, 117)", ""], "dem": [20, 137.58, 97.88, 18.84, "13px", "400", "0px", "transparent", "rgb(143, 153, 167)", ""]}, {"sel": "[data-f=\"lvl\"]", "kind": "", "sang": [125.88, 125, 106.48, 44, "14px", "600", "10px", "transparent", "rgb(15, 86, 174)", ""], "dem": [125.88, 125, 106.48, 44, "14px", "600", "10px", "transparent", "rgb(125, 187, 255)", ""]}, {"sel": "#board-share-acc", "kind": "", "sang": [20, 191, 350, 18.84, "13px", "600", "0px", "transparent", "rgb(95, 104, 117)", ""], "dem": [20, 191, 350, 18.84, "13px", "600", "0px", "transparent", "rgb(143, 153, 167)", ""]}, {"sel": "[data-f=\"ga\"]", "kind": "", "sang": [16, 217.84, 358, 133.75, "15px", "400", "14px", "rgb(255, 255, 255)", "rgb(22, 26, 33)", "rgb(230, 233, 238)"], "dem": [16, 217.84, 358, 133.75, "15px", "400", "14px", "rgb(21, 26, 33)", "rgb(230, 233, 238)", "rgb(35, 43, 53)"]}, {"sel": "[data-f=\"gaic\"]", "kind": "", "sang": [31, 266.72, 36, 36, "12px", "700", "10px", "rgb(30, 111, 208)", "rgb(255, 255, 255)", ""], "dem": [31, 266.72, 36, 36, "12px", "700", "10px", "rgb(30, 111, 208)", "rgb(255, 255, 255)", ""]}, {"sel": "[data-f=\"gat\"]", "kind": "", "sang": [79, 230.84, 252, 21.75, "15px", "600", "0px", "transparent", "rgb(22, 26, 33)", ""], "dem": [79, 230.84, 252, 21.75, "15px", "600", "0px", "transparent", "rgb(230, 233, 238)", ""]}, {"sel": "[data-f=\"map\"]", "kind": "", "sang": [79, 258.59, 252, 80, "15px", "400", "0px", "transparent", "rgb(22, 26, 33)", ""], "dem": [79, 258.59, 252, 80, "15px", "400", "0px", "transparent", "rgb(230, 233, 238)", ""]}, {"sel": "[data-f=\"mapc\"]:nth-child(1)", "kind": "", "sang": [79, 258.59, 128.31, 24, "12.5px", "400", "6px", "rgb(230, 233, 238)", "rgb(79, 88, 102)", ""], "dem": [79, 258.59, 128.31, 24, "12.5px", "400", "6px", "rgb(35, 42, 52)", "rgb(170, 179, 192)", ""]}, {"sel": "[data-f=\"mapc\"]:nth-child(3)", "kind": "", "sang": [79, 314.59, 124.17, 24, "12.5px", "400", "6px", "rgb(230, 233, 238)", "rgb(79, 88, 102)", ""], "dem": [79, 314.59, 124.17, 24, "12.5px", "400", "6px", "rgb(35, 42, 52)", "rgb(170, 179, 192)", ""]}, {"sel": "#board-share-people", "kind": "", "sang": [20, 373.59, 350, 18.84, "13px", "600", "0px", "transparent", "rgb(95, 104, 117)", ""], "dem": [20, 373.59, 350, 18.84, "13px", "600", "0px", "transparent", "rgb(143, 153, 167)", ""]}, {"sel": "[data-f=\"pl\"]", "kind": "data", "sang": [16, 400.44, 358, 416.13, "15px", "400", "14px", "rgb(255, 255, 255)", "rgb(22, 26, 33)", "rgb(230, 233, 238)"], "dem": [16, 400.44, 358, 416.13, "15px", "400", "14px", "rgb(21, 26, 33)", "rgb(230, 233, 238)", "rgb(35, 43, 53)"]}, {"sel": "[data-f=\"pr\"]:nth-child(1)", "kind": "", "sang": [17, 401.44, 356, 76, "15px", "400", "0px", "transparent", "rgb(22, 26, 33)", ""], "dem": [17, 401.44, 356, 76, "15px", "400", "0px", "transparent", "rgb(230, 233, 238)", ""]}, {"sel": "[data-f=\"pr\"]:nth-child(1) [data-f=\"av\"]", "kind": "tone", "sang": [31, 421.44, 36, 36, "12px", "700", "50%", "rgb(180, 80, 11)", "rgb(255, 255, 255)", ""], "dem": [31, 421.44, 36, 36, "12px", "700", "50%", "rgb(180, 80, 11)", "rgb(255, 255, 255)", ""]}, {"sel": "[data-f=\"pr\"]:nth-child(1) [data-f=\"wname\"]", "kind": "", "sang": [79, 410.88, 204.44, 20.25, "15px", "600", "0px", "transparent", "rgb(22, 26, 33)", ""], "dem": [79, 410.88, 204.44, 20.25, "15px", "600", "0px", "transparent", "rgb(230, 233, 238)", ""]}, {"sel": "[data-f=\"pr\"]:nth-child(1) [data-f=\"wsub\"]", "kind": "", "sang": [79, 432.13, 204.44, 35.88, "13px", "400", "0px", "transparent", "rgb(95, 104, 117)", ""], "dem": [79, 432.13, 204.44, 35.88, "13px", "400", "0px", "transparent", "rgb(143, 153, 167)", ""]}, {"sel": "[data-f=\"pr\"]:nth-child(1) [data-f=\"from\"]", "kind": "", "sang": [295.44, 429.28, 73.56, 20.3, "14px", "400", "0px", "transparent", "rgb(95, 104, 117)", ""], "dem": [295.44, 429.28, 73.56, 20.3, "14px", "400", "0px", "transparent", "rgb(143, 153, 167)", ""]}, {"sel": "[data-f=\"pr\"]:nth-child(2)", "kind": "", "sang": [17, 477.44, 356, 77, "15px", "400", "0px", "transparent", "rgb(22, 26, 33)", "rgb(230, 233, 238)"], "dem": [17, 477.44, 356, 77, "15px", "400", "0px", "transparent", "rgb(230, 233, 238)", "rgb(35, 43, 53)"]}, {"sel": "[data-f=\"pr\"]:nth-child(2) [data-f=\"tm\"]", "kind": "", "sang": [31, 498.44, 36, 36, "12px", "400", "10px", "rgb(230, 233, 238)", "rgb(79, 88, 102)", ""], "dem": [31, 498.44, 36, 36, "12px", "400", "10px", "rgb(35, 42, 52)", "rgb(170, 179, 192)", ""]}, {"sel": "[data-f=\"pr\"]:nth-child(2) [data-f=\"lvl-plain\"]", "kind": "", "sang": [305.98, 494.44, 63.02, 44, "14px", "600", "10px", "transparent", "rgb(15, 86, 174)", ""], "dem": [305.98, 494.44, 63.02, 44, "14px", "600", "10px", "transparent", "rgb(125, 187, 255)", ""]}, {"sel": "[data-f=\"pr\"]:nth-child(3) [data-f=\"lvl-plain\"]", "kind": "data", "sang": [273.72, 578.97, 95.28, 44, "14px", "600", "10px", "transparent", "rgb(15, 86, 174)", ""], "dem": [273.72, 578.97, 95.28, 44, "14px", "600", "10px", "transparent", "rgb(125, 187, 255)", ""]}, {"sel": "[data-f=\"pr\"]:nth-child(4) [data-f=\"badge\"]", "kind": "data", "sang": [79, 676.75, 95.61, 16.55, "12px", "600", "5px", "rgb(253, 236, 239)", "rgb(196, 35, 64)", ""], "dem": [79, 676.75, 95.61, 16.55, "12px", "600", "5px", "rgb(255, 106, 128)", "rgb(255, 107, 129)", ""]}, {"sel": "[data-f=\"pr\"]:nth-child(5) [data-f=\"from\"]", "kind": "data", "sang": [295.44, 767.41, 73.56, 20.3, "14px", "400", "0px", "transparent", "rgb(95, 104, 117)", ""], "dem": [295.44, 767.41, 73.56, 20.3, "14px", "400", "0px", "transparent", "rgb(143, 153, 167)", ""]}, {"sel": "[data-f=\"lk\"]", "kind": "data", "sang": [16, 838.56, 358, 117.11, "15px", "400", "14px", "rgb(255, 255, 255)", "rgb(22, 26, 33)", "rgb(230, 233, 238)"], "dem": [16, 838.56, 358, 117.11, "15px", "400", "14px", "rgb(21, 26, 33)", "rgb(230, 233, 238)", "rgb(35, 43, 53)"]}, {"sel": "[data-f=\"lkic\"]", "kind": "data", "sang": [31, 853.56, 36, 36, "15px", "400", "10px", "rgb(247, 248, 250)", "rgb(79, 88, 102)", "rgb(230, 233, 238)"], "dem": [31, 853.56, 36, 36, "15px", "400", "10px", "rgb(14, 17, 22)", "rgb(170, 179, 192)", "rgb(35, 43, 53)"]}, {"sel": "[data-f=\"lkt\"]", "kind": "data", "sang": [79, 853.56, 280, 21.75, "15px", "600", "0px", "transparent", "rgb(22, 26, 33)", ""], "dem": [79, 853.56, 280, 21.75, "15px", "600", "0px", "transparent", "rgb(230, 233, 238)", ""]}, {"sel": "[data-f=\"lks\"]", "kind": "data", "sang": [79, 877.31, 280, 36.91, "13px", "400", "0px", "transparent", "rgb(95, 104, 117)", ""], "dem": [79, 877.31, 280, 36.91, "13px", "400", "0px", "transparent", "rgb(143, 153, 167)", ""]}, {"sel": "[data-f=\"off\"]", "kind": "data", "sang": [79, 922.22, 280, 18.45, "13px", "500", "0px", "transparent", "rgb(79, 88, 102)", ""], "dem": [79, 922.22, 280, 18.45, "13px", "500", "0px", "transparent", "rgb(170, 179, 192)", ""]}, {"sel": "[data-f=\"df\"]", "kind": "", "sang": [0, 607, 390, 149, "15px", "400", "0px", "rgb(247, 248, 250)", "rgb(22, 26, 33)", "rgb(230, 233, 238)"], "dem": [0, 607, 390, 149, "15px", "400", "0px", "rgb(14, 17, 22)", "rgb(230, 233, 238)", "rgb(35, 43, 53)"]}, {"sel": "[data-f=\"btn-pri\"]", "kind": "", "sang": [16, 620, 358, 50, "15px", "600", "12px", "rgb(22, 102, 204)", "rgb(255, 255, 255)", "rgb(22, 102, 204)"], "dem": [16, 620, 358, 50, "15px", "600", "12px", "rgb(90, 169, 255)", "rgb(4, 17, 31)", "rgb(90, 169, 255)"]}, {"sel": "[data-f=\"logl\"]", "kind": "", "sang": [16, 678, 358, 44, "15px", "600", "12px", "transparent", "rgb(15, 86, 174)", "transparent"], "dem": [16, 678, 358, 44, "15px", "600", "12px", "transparent", "rgb(125, 187, 255)", "transparent"]}]}, "rieng-tu": {"rows": [{"sel": "[data-f=\"dlg\"]", "kind": "", "sang": [0, 0, 390, 756, "15px", "400", "14px 14px 0px 0px", "rgb(247, 248, 250)", "rgb(22, 26, 33)", ""], "dem": [0, 0, 390, 756, "15px", "400", "14px 14px 0px 0px", "rgb(14, 17, 22)", "rgb(230, 233, 238)", ""]}, {"sel": "[data-f=\"grab\"]", "kind": "", "sang": [177, 6, 36, 5, "15px", "400", "3px", "rgb(210, 215, 222)", "rgb(22, 26, 33)", ""], "dem": [177, 6, 36, 5, "15px", "400", "3px", "rgb(46, 55, 67)", "rgb(230, 233, 238)", ""]}, {"sel": "[data-f=\"dh\"]", "kind": "", "sang": [0, 13, 390, 52, "15px", "400", "0px", "transparent", "rgb(22, 26, 33)", ""], "dem": [0, 13, 390, 52, "15px", "400", "0px", "transparent", "rgb(230, 233, 238)", ""]}, {"sel": "[data-f=\"dt\"]", "kind": "", "sang": [54, 26.67, 280, 24.64, "17px", "650", "0px", "transparent", "rgb(22, 26, 33)", ""], "dem": [54, 26.67, 280, 24.64, "17px", "650", "0px", "transparent", "rgb(230, 233, 238)", ""]}, {"sel": "[data-f=\"ib\"]", "kind": "", "sang": [338, 17, 44, 44, "15px", "400", "12px", "transparent", "rgb(22, 26, 33)", ""], "dem": [338, 17, 44, 44, "15px", "400", "12px", "transparent", "rgb(230, 233, 238)", ""]}, {"sel": "[data-f=\"dh\"] button:first-child", "kind": "", "sang": [6, 17, 44, 44, "15px", "400", "12px", "transparent", "rgb(22, 26, 33)", ""], "dem": [6, 17, 44, 44, "15px", "400", "12px", "transparent", "rgb(230, 233, 238)", ""]}, {"sel": "[data-f=\"pdesc\"]", "kind": "", "sang": [20, 71, 350, 60.89, "14px", "400", "0px", "transparent", "rgb(79, 88, 102)", ""], "dem": [20, 71, 350, 60.89, "14px", "400", "0px", "transparent", "rgb(170, 179, 192)", ""]}, {"sel": "[data-f=\"warn\"]", "kind": "", "sang": [16, 145.89, 358, 86.89, "14px", "400", "12px", "rgb(255, 244, 214)", "rgb(138, 90, 0)", "rgb(235, 203, 122)"], "dem": [16, 145.89, 358, 86.89, "14px", "400", "12px", "rgb(239, 189, 74)", "rgb(242, 193, 78)", "rgb(242, 192, 77)"]}, {"sel": "[data-f=\"warn\"] b", "kind": "", "sang": [61, 159.89, 295.22, 38.3, "14px", "650", "0px", "transparent", "rgb(138, 90, 0)", ""], "dem": [61, 159.89, 295.22, 38.3, "14px", "650", "0px", "transparent", "rgb(242, 193, 78)", ""]}, {"sel": "[data-f=\"plegend\"]", "kind": "", "sang": [20, 246.78, 215.81, 18.84, "13px", "600", "0px", "transparent", "rgb(95, 104, 117)", ""], "dem": [20, 246.78, 215.81, 18.84, "13px", "600", "0px", "transparent", "rgb(143, 153, 167)", ""]}, {"sel": "[data-f=\"imp-list\"]", "kind": "", "sang": [16, 273.63, 358, 172, "15px", "400", "14px", "rgb(255, 255, 255)", "rgb(22, 26, 33)", "rgb(230, 233, 238)"], "dem": [16, 273.63, 358, 172, "15px", "400", "14px", "rgb(21, 26, 33)", "rgb(230, 233, 238)", "rgb(35, 43, 53)"]}, {"sel": "[data-f=\"ck\"]:nth-child(1)", "kind": "", "sang": [17, 274.63, 356, 56, "15px", "400", "0px", "transparent", "rgb(22, 26, 33)", ""], "dem": [17, 274.63, 356, 56, "15px", "400", "0px", "transparent", "rgb(230, 233, 238)", ""]}, {"sel": "[data-f=\"ck\"]:nth-child(1) [data-f=\"hit\"]", "kind": "", "sang": [21, 280.63, 44, 44, "15px", "400", "0px", "transparent", "rgb(22, 26, 33)", ""], "dem": [21, 280.63, 44, 44, "15px", "400", "0px", "transparent", "rgb(230, 233, 238)", ""]}, {"sel": "[data-f=\"ck\"]:nth-child(1) [data-f=\"av\"]", "kind": "tone", "sang": [75, 284.63, 36, 36, "12px", "700", "50%", "rgb(30, 111, 208)", "rgb(255, 255, 255)", ""], "dem": [75, 284.63, 36, 36, "12px", "700", "50%", "rgb(30, 111, 208)", "rgb(255, 255, 255)", ""]}, {"sel": "[data-f=\"ck\"]:nth-child(1) b", "kind": "", "sang": [121, 282.33, 238, 21.75, "15px", "600", "0px", "transparent", "rgb(22, 26, 33)", ""], "dem": [121, 282.33, 238, 21.75, "15px", "600", "0px", "transparent", "rgb(230, 233, 238)", ""]}, {"sel": "[data-f=\"ck\"]:nth-child(1) small", "kind": "", "sang": [121, 304.08, 238, 18.84, "13px", "400", "0px", "transparent", "rgb(196, 35, 64)", ""], "dem": [121, 304.08, 238, 18.84, "13px", "400", "0px", "transparent", "rgb(255, 107, 129)", ""]}, {"sel": "[data-f=\"ck\"]:nth-child(2)", "kind": "", "sang": [17, 330.63, 356, 57, "15px", "400", "0px", "transparent", "rgb(22, 26, 33)", "rgb(230, 233, 238)"], "dem": [17, 330.63, 356, 57, "15px", "400", "0px", "transparent", "rgb(230, 233, 238)", "rgb(35, 43, 53)"]}, {"sel": "[data-f=\"ck\"]:nth-child(3)", "kind": "", "sang": [17, 387.63, 356, 57, "15px", "400", "0px", "transparent", "rgb(22, 26, 33)", "rgb(230, 233, 238)"], "dem": [17, 387.63, 356, 57, "15px", "400", "0px", "transparent", "rgb(230, 233, 238)", "rgb(35, 43, 53)"]}, {"sel": "[data-f=\"pkeep\"]", "kind": "", "sang": [20, 459.63, 350, 63, "14px", "400", "0px", "transparent", "rgb(79, 88, 102)", ""], "dem": [20, 459.63, 350, 63, "14px", "400", "0px", "transparent", "rgb(170, 179, 192)", ""]}, {"sel": "[data-f=\"pnote\"]", "kind": "", "sang": [20, 530.63, 350, 58.5, "13px", "400", "0px", "transparent", "rgb(95, 104, 117)", ""], "dem": [20, 530.63, 350, 58.5, "13px", "400", "0px", "transparent", "rgb(143, 153, 167)", ""]}, {"sel": "[data-f=\"df\"]", "kind": "", "sang": [0, 607, 390, 149, "15px", "400", "0px", "rgb(247, 248, 250)", "rgb(22, 26, 33)", "rgb(230, 233, 238)"], "dem": [0, 607, 390, 149, "15px", "400", "0px", "rgb(14, 17, 22)", "rgb(230, 233, 238)", "rgb(35, 43, 53)"]}, {"sel": "[data-f=\"btn-pri\"]", "kind": "", "sang": [16, 620, 358, 50, "15px", "600", "12px", "rgb(22, 102, 204)", "rgb(255, 255, 255)", "rgb(22, 102, 204)"], "dem": [16, 620, 358, 50, "15px", "600", "12px", "rgb(90, 169, 255)", "rgb(4, 17, 31)", "rgb(90, 169, 255)"]}, {"sel": "[data-f=\"btn-cancel\"]", "kind": "", "sang": [16, 678, 96, 44, "15px", "500", "12px", "rgb(255, 255, 255)", "rgb(22, 26, 33)", "rgb(210, 215, 222)"], "dem": [16, 678, 96, 44, "15px", "500", "12px", "rgb(21, 26, 33)", "rgb(230, 233, 238)", "rgb(46, 55, 67)"]}, {"sel": "[data-f=\"btn-switch\"]", "kind": "", "sang": [122, 678, 252, 44, "15px", "500", "12px", "rgb(255, 255, 255)", "rgb(22, 26, 33)", "rgb(210, 215, 222)"], "dem": [122, 678, 252, 44, "15px", "500", "12px", "rgb(21, 26, 33)", "rgb(230, 233, 238)", "rgb(46, 55, 67)"]}]}, "nhat-ky": {"rows": [{"sel": "[data-f=\"dlg\"]", "kind": "", "sang": [0, 0, 390, 756, "15px", "400", "14px 14px 0px 0px", "rgb(247, 248, 250)", "rgb(22, 26, 33)", ""], "dem": [0, 0, 390, 756, "15px", "400", "14px 14px 0px 0px", "rgb(14, 17, 22)", "rgb(230, 233, 238)", ""]}, {"sel": "[data-f=\"grab\"]", "kind": "", "sang": [177, 6, 36, 5, "15px", "400", "3px", "rgb(210, 215, 222)", "rgb(22, 26, 33)", ""], "dem": [177, 6, 36, 5, "15px", "400", "3px", "rgb(46, 55, 67)", "rgb(230, 233, 238)", ""]}, {"sel": "[data-f=\"dh\"]", "kind": "", "sang": [0, 13, 390, 52, "15px", "400", "0px", "transparent", "rgb(22, 26, 33)", ""], "dem": [0, 13, 390, 52, "15px", "400", "0px", "transparent", "rgb(230, 233, 238)", ""]}, {"sel": "[data-f=\"dt\"]", "kind": "", "sang": [54, 26.67, 280, 24.64, "17px", "650", "0px", "transparent", "rgb(22, 26, 33)", ""], "dem": [54, 26.67, 280, 24.64, "17px", "650", "0px", "transparent", "rgb(230, 233, 238)", ""]}, {"sel": "[data-f=\"ib\"]", "kind": "", "sang": [338, 17, 44, 44, "15px", "400", "12px", "transparent", "rgb(22, 26, 33)", ""], "dem": [338, 17, 44, 44, "15px", "400", "12px", "transparent", "rgb(230, 233, 238)", ""]}, {"sel": "[data-f=\"dh\"] button:first-child", "kind": "", "sang": [6, 17, 44, 44, "15px", "400", "12px", "transparent", "rgb(22, 26, 33)", ""], "dem": [6, 17, 44, 44, "15px", "400", "12px", "transparent", "rgb(230, 233, 238)", ""]}, {"sel": "[data-f=\"lsub\"]", "kind": "", "sang": [20, 71, 350, 18.84, "13px", "400", "0px", "transparent", "rgb(95, 104, 117)", ""], "dem": [20, 71, 350, 18.84, "13px", "400", "0px", "transparent", "rgb(143, 153, 167)", ""]}, {"sel": "[data-f=\"log\"]", "kind": "data", "sang": [16, 103.84, 358, 439.36, "15px", "400", "14px", "rgb(255, 255, 255)", "rgb(22, 26, 33)", "rgb(230, 233, 238)"], "dem": [16, 103.84, 358, 439.36, "15px", "400", "14px", "rgb(21, 26, 33)", "rgb(230, 233, 238)", "rgb(35, 43, 53)"]}, {"sel": "[data-f=\"le\"]:nth-child(1)", "kind": "data", "sang": [31, 104.84, 328, 107.39, "14px", "400", "0px", "transparent", "rgb(22, 26, 33)", ""], "dem": [31, 104.84, 328, 107.39, "14px", "400", "0px", "transparent", "rgb(230, 233, 238)", ""]}, {"sel": "[data-f=\"le\"]:nth-child(1) [data-f=\"av\"]", "kind": "tone", "sang": [31, 116.84, 32, 32, "11.5px", "700", "50%", "rgb(78, 133, 116)", "rgb(255, 255, 255)", ""], "dem": [31, 116.84, 32, 32, "11.5px", "700", "50%", "rgb(78, 133, 116)", "rgb(255, 255, 255)", ""]}, {"sel": "[data-f=\"le\"]:nth-child(2)", "kind": "data", "sang": [31, 212.23, 328, 87.67, "14px", "400", "0px", "transparent", "rgb(22, 26, 33)", "rgb(230, 233, 238)"], "dem": [31, 212.23, 328, 87.67, "14px", "400", "0px", "transparent", "rgb(230, 233, 238)", "rgb(35, 43, 53)"]}]}, "da-sao-chep": {"rows": [{"sel": "[data-f=\"toast\"]", "kind": "", "sang": [16, 622.22, 358, 61.78, "14px", "500", "12px", "rgb(22, 26, 33)", "rgb(255, 255, 255)", ""], "dem": [16, 622.22, 358, 61.78, "14px", "500", "12px", "rgb(230, 233, 238)", "rgb(14, 17, 22)", ""]}, {"sel": "[data-f=\"toast\"] span", "kind": "", "sang": [60, 634.22, 300, 37.78, "14px", "500", "0px", "transparent", "rgb(255, 255, 255)", ""], "dem": [60, 634.22, 300, 37.78, "14px", "500", "0px", "transparent", "rgb(14, 17, 22)", ""]}, {"sel": "[data-f=\"toast\"] svg", "kind": "tone", "sang": [30, 643.11, 20, 20, "14px", "500", "0px", "transparent", "rgb(127, 214, 163)", ""], "dem": [30, 643.11, 20, 20, "14px", "500", "0px", "transparent", "rgb(31, 122, 74)", ""]}, {"sel": "[data-f=\"dh\"]", "kind": "", "sang": [0, 101, 390, 52, "15px", "400", "0px", "transparent", "rgb(22, 26, 33)", ""], "dem": [0, 101, 390, 52, "15px", "400", "0px", "transparent", "rgb(230, 233, 238)", ""]}]}, "doi-quyen": {"rows": [{"sel": "[data-f=\"lsheet\"]", "kind": "data", "sang": [0, 0, 390, 480, "15px", "400", "14px 14px 0px 0px", "rgb(255, 255, 255)", "rgb(22, 26, 33)", ""], "dem": [0, 0, 390, 480, "15px", "400", "14px 14px 0px 0px", "rgb(21, 26, 33)", "rgb(230, 233, 238)", ""]}, {"sel": "[data-f=\"lsheet\"] > div:first-child", "kind": "", "sang": [177, 6, 36, 5, "15px", "400", "3px", "rgb(210, 215, 222)", "rgb(22, 26, 33)", ""], "dem": [177, 6, 36, 5, "15px", "400", "3px", "rgb(46, 55, 67)", "rgb(230, 233, 238)", ""]}, {"sel": "[data-f=\"lsheet\"] > div:nth-child(2)", "kind": "", "sang": [0, 13, 390, 52, "15px", "400", "0px", "transparent", "rgb(22, 26, 33)", ""], "dem": [0, 13, 390, 52, "15px", "400", "0px", "transparent", "rgb(230, 233, 238)", ""]}, {"sel": "[data-f=\"lt\"]", "kind": "", "sang": [20, 16.75, 310, 24.64, "17px", "650", "0px", "transparent", "rgb(22, 26, 33)", ""], "dem": [20, 16.75, 310, 24.64, "17px", "650", "0px", "transparent", "rgb(230, 233, 238)", ""]}, {"sel": "[data-f=\"lsheet\"] [data-f=\"ib\"]", "kind": "", "sang": [338, 17, 44, 44, "15px", "400", "12px", "transparent", "rgb(22, 26, 33)", ""], "dem": [338, 17, 44, 44, "15px", "400", "12px", "transparent", "rgb(230, 233, 238)", ""]}, {"sel": "[role=\"radiogroup\"]", "kind": "", "sang": [0, 65, 390, 248, "15px", "400", "0px", "transparent", "rgb(22, 26, 33)", ""], "dem": [0, 65, 390, 248, "15px", "400", "0px", "transparent", "rgb(230, 233, 238)", ""]}, {"sel": "[role=\"radio\"]:nth-child(1)", "kind": "", "sang": [0, 65, 390, 62, "15px", "400", "0px", "transparent", "rgb(22, 26, 33)", ""], "dem": [0, 65, 390, 62, "15px", "400", "0px", "transparent", "rgb(230, 233, 238)", ""]}, {"sel": "[role=\"radio\"]:nth-child(1) b", "kind": "tone", "sang": [20, 76.16, 314, 21.75, "15px", "600", "0px", "transparent", "rgb(22, 26, 33)", ""], "dem": [20, 76.16, 314, 21.75, "15px", "600", "0px", "transparent", "rgb(230, 233, 238)", ""]}, {"sel": "[role=\"radio\"]:nth-child(1) small", "kind": "", "sang": [20, 97.91, 314, 17.94, "13px", "400", "0px", "transparent", "rgb(95, 104, 117)", ""], "dem": [20, 97.91, 314, 17.94, "13px", "400", "0px", "transparent", "rgb(143, 153, 167)", ""]}, {"sel": "[role=\"radio\"]:nth-child(2)", "kind": "", "sang": [0, 127, 390, 62, "15px", "400", "0px", "transparent", "rgb(22, 26, 33)", ""], "dem": [0, 127, 390, 62, "15px", "400", "0px", "transparent", "rgb(230, 233, 238)", ""]}, {"sel": "[role=\"radio\"]:nth-child(3)", "kind": "", "sang": [0, 189, 390, 62, "15px", "400", "0px", "transparent", "rgb(22, 26, 33)", ""], "dem": [0, 189, 390, 62, "15px", "400", "0px", "transparent", "rgb(230, 233, 238)", ""]}, {"sel": "[role=\"radio\"]:nth-child(4)", "kind": "", "sang": [0, 251, 390, 62, "15px", "400", "0px", "transparent", "rgb(22, 26, 33)", ""], "dem": [0, 251, 390, 62, "15px", "400", "0px", "transparent", "rgb(230, 233, 238)", ""]}, {"sel": "[role=\"radio\"]:nth-child(4) b", "kind": "data", "sang": [20, 253.19, 314, 21.75, "15px", "600", "0px", "transparent", "rgb(79, 88, 102)", ""], "dem": [20, 253.19, 314, 21.75, "15px", "600", "0px", "transparent", "rgb(170, 179, 192)", ""]}, {"sel": "[role=\"separator\"]", "kind": "", "sang": [20, 319, 350, 1, "15px", "400", "0px", "rgb(230, 233, 238)", "rgb(22, 26, 33)", ""], "dem": [20, 319, 350, 1, "15px", "400", "0px", "rgb(35, 43, 53)", "rgb(230, 233, 238)", ""]}]}};

const FRAME_ROOT: Record<string, string> = {
  "chia-se": '[data-f="dlg"]',
  "rieng-tu": '[data-f="dlg"]',
  "nhat-ky": '[data-f="dlg"]',
  "doi-quyen": '[data-f="lsheet"]',
  "da-sao-chep": "html",
};

const DARK_COLOUR_SKIP = new Set([
  '[data-f="warn"]',
  '[data-f="warn"] b',
  '[data-f="pr"]:nth-child(4) [data-f="badge"]',
]);

async function openFrame(page: Page, state: string, dark: boolean) {
  await page.context().grantPermissions(["clipboard-read", "clipboard-write"]);
  await openShare(page, dark);
  if (state === "rieng-tu") await f(page, "ga").first().click();
  if (state === "nhat-ky") await f(page, "logl").click();
  if (state === "doi-quyen") await f(page, "lvl-plain").nth(2).click();
  if (state === "da-sao-chep") await f(page, "btn-pri").click();
  await page.waitForTimeout(500);
}

async function measureAll(
  page: Page,
  root: string,
  selectors: readonly string[],
): Promise<(Measure | null)[]> {
  return page.evaluate(
    ({ root, selectors }) => {
      const origin = document.querySelector(root)!.getBoundingClientRect();
      const rgb = (value: string) => {
        const canvas = document.createElement("canvas");
        canvas.width = canvas.height = 1;
        const context = canvas.getContext("2d", { willReadFrequently: true })!;
        context.clearRect(0, 0, 1, 1);
        context.fillStyle = "#000";
        context.fillStyle = value;
        context.fillRect(0, 0, 1, 1);
        const [r, g, b, a] = context.getImageData(0, 0, 1, 1).data;
        return a === 0 ? "transparent" : `rgb(${r}, ${g}, ${b})`;
      };
      return selectors.map((selector) => {
        const element = document.querySelector(selector);
        if (!element) return null;
        const box = element.getBoundingClientRect();
        const style = getComputedStyle(element);
        return [
          box.x - origin.x,
          box.y - origin.y,
          box.width,
          box.height,
          style.fontSize,
          style.fontWeight,
          style.borderRadius,
          rgb(style.backgroundColor),
          rgb(style.color),
          style.borderTopWidth === "0px" ? "" : rgb(style.borderTopColor),
        ] as const;
      });
    },
    { root, selectors },
  );
}

test.describe("Board share at 390 px (Final-M-Share, owner)", () => {
  test.skip(
    persona !== "owner" || scenario !== "default" || faults !== "",
    "Owner checks need the default owner world without faults",
  );
  test.use({ viewport: { width: 390, height: 844 } });

  for (const state of Object.keys(SHEET_FRAMES)) {
    for (const dark of [false, true]) {
      const mode = dark ? "dem" : "sang";
      test(`${state} (${mode}) matches the artboard within 1 px`, async ({
        page,
      }) => {
        await openFrame(page, state, dark);
        const { rows } = SHEET_FRAMES[state]!;
        const actual = await measureAll(
          page,
          FRAME_ROOT[state]!,
          rows.map((row) => row.sel),
        );
        const wrong: string[] = [];
        rows.forEach((row, index) => {
          const want = row[mode];
          const got = actual[index];
          if (!got) {
            wrong.push(`${row.sel}: missing`);
            return;
          }
          const keys = row.kind === "data" ? [0, 2] : [0, 1, 2, 3];
          for (const key of keys) {
            if (Math.abs((got[key] as number) - (want[key] as number)) > 1)
              wrong.push(
                `${row.sel}: box[${key}] ${(got[key] as number).toFixed(1)} vs ${want[key]}`,
              );
          }
          if (got[4] !== want[4] || got[5] !== want[5])
            wrong.push(
              `${row.sel}: font ${got[4]}/${got[5]} vs ${want[4]}/${want[5]}`,
            );
          const round = want[6] === "50%" && parseFloat(got[6]) > 1e6;
          if (got[6] !== want[6] && !round)
            wrong.push(`${row.sel}: radius ${got[6]} vs ${want[6]}`);
          if (row.kind === "tone") return;
          if (dark && DARK_COLOUR_SKIP.has(row.sel)) return;
          // The toast's text is the page background token (247, 248, 250), the mockup's white.
          const soft = state === "da-sao-chep" && !dark;
          for (const key of [7, 8, 9]) {
            if (key === 8 && soft) continue;
            // The mockup's amber border (235, 203, 122) has no design token: the card uses warning-text at 40%.
            if (key === 9 && row.sel === '[data-f="warn"]') continue;
            if (got[key] !== want[key])
              wrong.push(
                `${row.sel}: colour[${key}] ${got[key]} vs ${want[key]}`,
              );
          }
        });
        expect(wrong, wrong.join("\n")).toEqual([]);
        const overflow = await page.evaluate(
          () => document.documentElement.scrollWidth - window.innerWidth,
        );
        expect(overflow).toBeLessThanOrEqual(0);
      });
    }
  }
});
