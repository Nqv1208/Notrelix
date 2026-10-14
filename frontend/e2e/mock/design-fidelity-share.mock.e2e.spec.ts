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
 * The mock world differs from the mockup's: the ERP Board is in Engineering
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

async function openShare(page: Page) {
  await page.clock.setFixedTime(NOW);
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "vi");
      window.localStorage.setItem("notrelix-system-rail", "pinned");
    } catch {
      // Private mode: the defaults apply.
    }
  });
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

  test("the add row is drawn but inert", async ({ page }) => {
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
    await expect(invite).toHaveAttribute("aria-disabled", "true");
    await expect(invite).toHaveAttribute("title", /chưa có thao tác ghi/);
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
    await expect(change).toHaveAttribute("aria-disabled", "true");
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
    // A member who is not the owner has a level button, inert while there is no write.
    const level = rows.nth(2).locator('[data-f="lvl-plain"]');
    expectRect(await rel(page, level), { height: 30 }, "level");
    expect(await style(level, "border-radius")).toBe("9px");
    expect(await style(level, "font-weight")).toBe("500");
    await expect(level).toHaveAttribute("aria-disabled", "true");
    const second = await rel(page, rows.nth(1));
    expect(Math.abs(second.y - (303.67 + 50))).toBeLessThanOrEqual(1);
    // Ten entries: five show, the rest sit behind "Show more".
    await expect(rows).toHaveCount(5);
    await f(page, "plmore").click();
    expect(await rows.count()).toBeGreaterThan(5);
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
    await expect(log).toHaveAttribute("aria-disabled", "true");
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
