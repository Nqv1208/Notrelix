import { expect, test, type Locator, type Page } from "@playwright/test";

/**
 * Department (Space) page geometry against the October 2026 mockup. Every
 * number comes from `docs/design/mockups/2026-10/src/Final-Space.dc.html`
 * rendered at 1440x900 with `nav=hien-day` (rail pinned: the main area starts
 * at x 332 and is 1108 wide) and measured with the box dump (position, size,
 * padding, font, radius, colour) for the states tong-quan, tao-moi, phong-moi
 * and thiet-lap and the modes sang and dem. Boxes are read relative to the
 * top-left of the main area, so the checks hold whatever sits left of it.
 * Tolerance is 1 px for boxes and exact for font sizes, weights, radii and
 * colours.
 *
 * The mock world differs from the mockup's: its departments are Công nghệ,
 * Kế toán and Marketing, a Board has no progress, attention or people
 * read, Docs have no editor, and the access card knows people and teams, not
 * the head count. So the columns of the Boards list, the attention and
 * workload blocks and the "Khách ở board" row are not drawn here (the grid
 * closes over them); they are measured on the Storybook stories fed with the
 * mockup's own data (all five states, light and dark, within 1 px). Here only
 * boxes that do not depend on that data are compared: the head, the tab strip,
 * the section heads, a Board row and its icon and name, a sub-department card,
 * the access card, the Create menu, the settings cards and the first steps.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const WORKSPACE = "/workspaces/mock-workspace-primary";
const ENGINEERING = `${WORKSPACE}/spaces/mock-space-engineering`;
const EMPTY_DEPARTMENT = `${WORKSPACE}/spaces/mock-space-hr`;
const NOW = new Date("2026-10-12T10:40:00+07:00");

interface Rect {
  readonly x: number;
  readonly y: number;
  readonly width: number;
  readonly height: number;
}

const TYPE = {
  h1: { size: "24px", weight: "650", letterSpacing: "-0.36px" },
  crumb: { size: "12.5px" },
  chip: { size: "12px", weight: "500" },
  button: { size: "13px", weight: "500", primary: "600" },
  tab: { size: "13.5px", on: "600", off: "500" },
  section: { size: "15px", weight: "650" },
  count: { size: "13px" },
  link: { size: "12.5px", weight: "500" },
  row: { size: "14px", weight: "600" },
  sub: { size: "12px" },
  heading3: { size: "13px", weight: "600" },
  input: { size: "13.5px" },
  small: { size: "12.5px" },
} as const;

const COLOUR = {
  accent: "rgb(22, 102, 204)",
  accentSoft: "rgb(231, 240, 252)",
  accentText: "rgb(15, 86, 174)",
  panel: "rgb(255, 255, 255)",
  page: "rgb(247, 248, 250)",
  hover: "rgb(230, 233, 238)",
  line: "rgb(225, 229, 234)",
  lineStrong: "rgb(210, 215, 222)",
  ink: "rgb(22, 26, 33)",
  meta: "rgb(95, 104, 117)",
  muted: "rgb(79, 88, 102)",
  danger: "rgb(196, 35, 64)",
  dangerSoft: "rgb(253, 236, 239)",
  done: "rgb(46, 139, 87)",
} as const;

async function openSpace(page: Page, path: string, heading: string | RegExp) {
  await page.clock.setFixedTime(NOW);
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "vi");
      window.localStorage.setItem("notrelix-system-rail", "pinned");
    } catch {
      // Private mode: the defaults apply.
    }
  });
  await page.goto(path);
  await expect(
    page.getByRole("heading", { level: 1, name: heading }),
  ).toBeVisible();
}

async function box(locator: Locator): Promise<Rect> {
  const found = await locator.boundingBox();
  if (!found) throw new Error("element has no box");
  return found;
}

/** A locator's box relative to the top-left of the main area. */
async function rel(page: Page, locator: Locator): Promise<Rect> {
  const [o, found] = await Promise.all([
    box(page.locator("#workspace-main")),
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

/** A colour property as `rgb(r, g, b)`, whatever colour space the page uses. */
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

test.describe("Department page geometry at 1440 px (owner)", () => {
  test.skip(
    persona !== "owner" || scenario !== "default" || faults !== "",
    "Owner checks need the default owner world without faults",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  test("the head: mark, crumb, title, chip, actions and lede", async ({
    page,
  }) => {
    await openSpace(page, ENGINEERING, "Công nghệ");
    expectRect(
      await rel(page, f(page, "dm")),
      { x: 28, y: 24.06, width: 40, height: 40 },
      "mark",
    );
    expect(await style(f(page, "dm"), "border-radius")).toBe("11px");
    expect(await style(f(page, "dm"), "font-size")).toBe("13px");
    expectRect(
      await rel(page, f(page, "crumb")),
      { x: 82, y: 20, height: 18.13 },
      "crumb",
    );
    expect(await style(f(page, "crumb"), "font-size")).toBe(TYPE.crumb.size);
    expect(await colour(f(page, "crumb"), "color")).toBe(COLOUR.meta);
    const h1 = f(page, "h1");
    expectRect(await rel(page, h1), { x: 82, y: 38.13, height: 30 }, "title");
    expect(await style(h1, "font-size")).toBe(TYPE.h1.size);
    expect(await style(h1, "font-weight")).toBe(TYPE.h1.weight);
    expect(await style(h1, "letter-spacing")).toBe(TYPE.h1.letterSpacing);
    expect(await colour(h1, "color")).toBe(COLOUR.ink);

    const chip = f(page, "chip").first();
    const chipBox = await rel(page, chip);
    expectRect(chipBox, { y: 31.06, height: 26 }, "chip");
    expect(await style(chip, "border-radius")).toBe("6px");
    expect(await style(chip, "font-size")).toBe(TYPE.chip.size);
    expect(await colour(chip, "color")).toBe(COLOUR.muted);
    expect(await colour(chip, "background-color")).toBe(COLOUR.panel);
    expect(await colour(chip, "border-top-color")).toBe(COLOUR.line);
    // 14 px between the title block and the chip.
    const crumbt = await rel(page, f(page, "crumbt"));
    expect(
      Math.abs(chipBox.x - (crumbt.x + crumbt.width + 14)),
    ).toBeLessThanOrEqual(1);

    const add = f(page, "btn-add");
    expectRect(await rel(page, add), { y: 28.06, height: 32 }, "add members");
    expect(await style(add, "font-size")).toBe(TYPE.button.size);
    expect(await style(add, "font-weight")).toBe(TYPE.button.weight);
    expect(await style(add, "border-radius")).toBe("7px");
    expect(await colour(add, "border-top-color")).toBe(COLOUR.lineStrong);
    const create = f(page, "btn-new");
    expectRect(await rel(page, create), { y: 28.06, height: 32 }, "create");
    expect(await style(create, "font-weight")).toBe(TYPE.button.primary);
    expect(await colour(create, "background-color")).toBe(COLOUR.accent);
    expect(await colour(create, "color")).toBe(COLOUR.panel);
    const gear = f(page, "ib");
    const gearBox = await rel(page, gear);
    expectRect(gearBox, { y: 28.06, width: 32, height: 32 }, "settings icon");
    // The actions end 28 px from the right edge of the main area.
    expect(
      Math.abs(gearBox.x + gearBox.width - (1108 - 28)),
    ).toBeLessThanOrEqual(1);
    // 8 px between the buttons.
    const addBox = await rel(page, add);
    const createBox = await rel(page, create);
    expect(
      Math.abs(addBox.x + addBox.width + 8 - createBox.x),
    ).toBeLessThanOrEqual(1);

    const lede = f(page, "lede");
    expectRect(
      await rel(page, lede),
      { x: 82, y: 76.13, height: 20.3 },
      "lede",
    );
    expect(await colour(lede, "color")).toBe(COLOUR.muted);
    expect(await style(lede, "font-size")).toBe("14px");
  });

  test("the tab strip", async ({ page }) => {
    await openSpace(page, ENGINEERING, "Công nghệ");
    const strip = f(page, "tabs");
    expectRect(
      await rel(page, strip),
      { x: 0, y: 96.42, width: 1108, height: 54 },
      "tab strip",
    );
    expect(await colour(strip, "border-bottom-color")).toBe(COLOUR.line);
    const tabs = f(page, "tab");
    await expect(tabs).toHaveCount(4);
    const first = tabs.nth(0);
    const firstBox = await rel(page, first);
    expectRect(firstBox, { x: 28, y: 110.42, height: 40 }, "first tab");
    expect(await style(first, "font-size")).toBe(TYPE.tab.size);
    expect(await style(first, "font-weight")).toBe(TYPE.tab.on);
    expect(await colour(first, "color")).toBe(COLOUR.ink);
    expect(await colour(first, "border-bottom-color")).toBe(COLOUR.accent);
    expect(await style(first, "border-bottom-width")).toBe("2px");
    const second = tabs.nth(1);
    expect(await style(second, "font-weight")).toBe(TYPE.tab.off);
    expect(await colour(second, "color")).toBe(COLOUR.muted);
    const secondBox = await rel(page, second);
    // Tabs sit 2 px apart.
    expect(
      Math.abs(firstBox.x + firstBox.width + 2 - secondBox.x),
    ).toBeLessThanOrEqual(1);
    const count = f(page, "tabn").first();
    expect(await style(count, "font-weight")).toBe("400");
    expect(await colour(count, "color")).toBe(COLOUR.meta);
    // The body starts under the strip; the aside is 340 wide with a line.
    expectRect(await rel(page, f(page, "body")), { y: 150.42 }, "body");
    const aside = f(page, "aside");
    expectRect(
      await rel(page, aside),
      { x: 768, y: 150.42, width: 340 },
      "aside",
    );
    expect(await colour(aside, "background-color")).toBe(COLOUR.panel);
    expect(await colour(aside, "border-left-color")).toBe(COLOUR.line);
  });

  test("the sections: heads, Board row, sub-department cards", async ({
    page,
  }) => {
    await openSpace(page, ENGINEERING, "Công nghệ");
    const head = f(page, "sh").first();
    expectRect(
      await rel(page, head),
      { x: 28, y: 172.42, width: 712, height: 21.75 },
      "section head",
    );
    const h2 = f(page, "sh-h2").first();
    expect(await style(h2, "font-size")).toBe(TYPE.section.size);
    expect(await style(h2, "font-weight")).toBe(TYPE.section.weight);
    expect(await colour(h2, "color")).toBe(COLOUR.ink);
    const n = f(page, "sh-n").first();
    expect(await style(n, "font-size")).toBe(TYPE.count.size);
    expect(await colour(n, "color")).toBe(COLOUR.meta);
    const link = f(page, "sh-a").first();
    const linkBox = await rel(page, link);
    expect(Math.abs(linkBox.x + linkBox.width - 740)).toBeLessThanOrEqual(1);
    expect(await style(link, "font-size")).toBe(TYPE.link.size);
    expect(await style(link, "font-weight")).toBe(TYPE.link.weight);
    expect(await colour(link, "color")).toBe(COLOUR.accentText);

    const list = f(page, "list").first();
    expectRect(await rel(page, list), { x: 28, y: 204.17, width: 712 }, "list");
    expect(await style(list, "border-radius")).toBe("12px");
    expect(await colour(list, "background-color")).toBe(COLOUR.panel);
    expect(await colour(list, "border-top-color")).toBe(COLOUR.line);
    const lh = f(page, "lh");
    expectRect(
      await rel(page, lh),
      { x: 29, y: 205.17, width: 710, height: 35 },
      "list head",
    );
    expect(await colour(lh, "background-color")).toBe(COLOUR.page);
    expect(await style(lh, "font-size")).toBe("11.5px");
    expect(await style(lh, "font-weight")).toBe("600");
    const row = f(page, "br").first();
    expectRect(
      await rel(page, row),
      { x: 29, y: 240.17, width: 710, height: 59 },
      "Board row",
    );
    const icon = f(page, "bi").first();
    const iconBox = await rel(page, icon);
    expectRect(iconBox, { x: 45, width: 30, height: 30 }, "Board icon");
    expect(await style(icon, "border-radius")).toBe("8px");
    expect(await colour(icon, "background-color")).toBe(COLOUR.accentSoft);
    expect(await colour(icon, "color")).toBe(COLOUR.accentText);
    const name = f(page, "bname").first();
    expect(Math.abs((await rel(page, name)).x - 85)).toBeLessThanOrEqual(1);
    expect(await style(name, "font-size")).toBe(TYPE.row.size);
    expect(await style(name, "font-weight")).toBe(TYPE.row.weight);
    expect(await colour(name, "color")).toBe(COLOUR.ink);

    const card = f(page, "sub").first();
    const cardBox = await rel(page, card);
    expectRect(cardBox, { x: 28, width: 350 }, "sub-department card");
    expect(await style(card, "border-radius")).toBe("12px");
    expect(await colour(card, "border-top-color")).toBe(COLOUR.line);
    const mark = f(page, "subdm").first();
    expectRect(
      await rel(page, mark),
      { x: cardBox.x + 15, y: cardBox.y + 15, width: 32, height: 32 },
      "sub-department mark",
    );
    expect(await style(mark, "border-radius")).toBe("9px");
    const why = f(page, "why").first();
    expect(await style(why, "font-size")).toBe(TYPE.sub.size);
    expect(await colour(why, "color")).toBe(COLOUR.muted);
    const add = f(page, "subadd");
    // Two sub-departments (Nhóm Hạ tầng, Nhóm Ứng dụng) and the add card: the
    // add card starts the second row.
    expectRect(await rel(page, add), { x: 28, width: 350 }, "add card");
    expect(await style(add, "border-top-style")).toBe("dashed");
    expect(await style(add, "border-radius")).toBe("12px");
    expect(await style(add, "font-size")).toBe(TYPE.tab.size);
    expect(await colour(add, "color")).toBe(COLOUR.muted);
    // The note sits 8 px under the cards.
    const noteBox = await rel(page, f(page, "note").first());
    expect(Math.abs(noteBox.x - 28)).toBeLessThanOrEqual(1);
    expect(await style(f(page, "note").first(), "font-size")).toBe(
      TYPE.small.size,
    );
    expect(await colour(f(page, "note").first(), "color")).toBe(COLOUR.meta);
  });

  test("the access card", async ({ page }) => {
    await openSpace(page, ENGINEERING, "Công nghệ");
    const heading = f(page, "abh").last();
    const headingBox = await rel(page, heading);
    expectRect(
      headingBox,
      { x: 789, y: 170.42, height: 18.84 },
      "aside heading",
    );
    expect(await style(heading, "font-size")).toBe(TYPE.heading3.size);
    expect(await style(heading, "font-weight")).toBe(TYPE.heading3.weight);
    expect(await colour(heading, "color")).toBe(COLOUR.muted);
    const card = f(page, "acc");
    const cardBox = await rel(page, card);
    expectRect(cardBox, { x: 789, width: 299 }, "access card");
    expect(
      Math.abs(cardBox.y - (headingBox.y + headingBox.height + 8)),
    ).toBeLessThanOrEqual(1);
    expect(await style(card, "border-radius")).toBe("12px");
    expect(await colour(card, "background-color")).toBe(COLOUR.page);
    expect(await colour(card, "border-top-color")).toBe(COLOUR.line);
    const vic = f(page, "vic");
    expectRect(
      await rel(page, vic),
      { x: cardBox.x + 15, y: cardBox.y + 15, width: 32, height: 32 },
      "visibility icon",
    );
    expect(await style(vic, "border-radius")).toBe("8px");
    expect(await colour(vic, "background-color")).toBe(COLOUR.panel);
    const dt = f(page, "dt").first();
    expect((await rel(page, dt)).width).toBeCloseTo(96, 0);
    expect(await style(dt, "font-size")).toBe(TYPE.small.size);
    expect(await colour(dt, "color")).toBe(COLOUR.meta);
    const dd = f(page, "dd").first();
    expect(Math.abs((await rel(page, dd)).x - 910)).toBeLessThanOrEqual(1);
    expect(await colour(dd, "color")).toBe(COLOUR.ink);
    const more = f(page, "more");
    expect(await style(more, "font-weight")).toBe("600");
    expect(await colour(more, "color")).toBe(COLOUR.accentText);
  });

  test("the Create menu", async ({ page }) => {
    await openSpace(page, ENGINEERING, "Công nghệ");
    await f(page, "btn-new").click();
    const menu = f(page, "menu");
    await expect(menu).toBeVisible();
    expectRect(await rel(page, menu), { x: 738, y: 68.06, width: 302 }, "menu");
    expect(await style(menu, "border-radius")).toBe("10px");
    expect(await colour(menu, "background-color")).toBe(COLOUR.panel);
    expect(await colour(menu, "border-top-color")).toBe(COLOUR.lineStrong);
    expect(await style(menu, "box-shadow")).not.toBe("none");
    const items = f(page, "mi");
    await expect(items).toHaveCount(4);
    const first = items.nth(0);
    expectRect(
      await rel(page, first),
      { x: 743, y: 73.06, width: 292 },
      "item",
    );
    expect(await style(first, "border-radius")).toBe("7px");
    // The first item is the focused one and reads highlighted.
    expect(await colour(first, "background-color")).toBe(COLOUR.hover);
    await expect(first).toBeFocused();
    const icon = f(page, "mic").first();
    expectRect(
      await rel(page, icon),
      { x: 753, y: 86.23, width: 28, height: 28 },
      "item icon",
    );
    expect(await colour(icon, "background-color")).toBe(COLOUR.accentSoft);
    const second = items.nth(1);
    expectRect(await rel(page, second), { x: 743, height: 44 }, "second item");
    expect(await colour(f(page, "mic").nth(2), "background-color")).toBe(
      COLOUR.hover,
    );
    const sep = f(page, "msep");
    expectRect(
      await rel(page, sep),
      { x: 739, width: 300, height: 1 },
      "separator",
    );
    const key = f(page, "kbd").first();
    expect((await rel(page, key)).height).toBeCloseTo(20, 0);
    expect(await style(key, "font-size")).toBe("11px");
    expect(await colour(key, "border-top-color")).toBe(COLOUR.lineStrong);
    const foot = f(page, "mfoot");
    expectRect(await rel(page, foot), { x: 739, width: 300 }, "footer");
    expect(await colour(foot, "border-top-color")).toBe(COLOUR.line);
    expect(await style(foot, "font-size")).toBe("12px");
  });

  test("the settings tab", async ({ page }) => {
    await openSpace(page, `${ENGINEERING}?tab=settings`, "Công nghệ");
    const tab = f(page, "tab").nth(3);
    await expect(tab).toHaveAttribute("aria-current", "page");
    const col = f(page, "col");
    expectRect(await rel(page, col), { x: 0, y: 150.42, width: 816 }, "column");
    const note = f(page, "note").first();
    expectRect(
      await rel(page, note),
      { x: 28, y: 172.42, width: 760, height: 19.38 },
      "note",
    );
    const sets = f(page, "set");
    await expect(sets).toHaveCount(4);
    const name = sets.nth(0);
    expectRect(
      await rel(page, name),
      { x: 28, y: 209.8, width: 760, height: 264 },
      "name card",
    );
    expect(await style(name, "border-radius")).toBe("12px");
    expect(await colour(name, "background-color")).toBe(COLOUR.panel);
    expect(await colour(name, "border-top-color")).toBe(COLOUR.line);
    const h2 = f(page, "seth").first();
    expectRect(
      await rel(page, h2),
      { x: 47, y: 226.8, height: 21.75 },
      "card title",
    );
    expect(await style(h2, "font-size")).toBe(TYPE.section.size);
    const input = page.getByRole("textbox", { name: "Tên phòng ban" });
    expectRect(
      await rel(page, input),
      { x: 47, y: 283.67, width: 722, height: 34 },
      "name input",
    );
    expect(await style(input, "font-size")).toBe(TYPE.input.size);
    expect(await style(input, "font-weight")).toBe("400");
    expect(await style(input, "border-radius")).toBe("7px");
    expect(await colour(input, "border-top-color")).toBe(COLOUR.lineStrong);
    const area = page.getByRole("textbox", { name: "Mô tả" });
    expectRect(
      await rel(page, area),
      { x: 47, y: 352.8, width: 722, height: 64 },
      "description",
    );
    const save = page.getByRole("button", { name: "Lưu", exact: true });
    expectRect(
      await rel(page, save),
      { x: 47, y: 428.8, width: 44.56, height: 28 },
      "save",
    );
    expect(await colour(save, "background-color")).toBe(COLOUR.accent);
    expect(await style(save, "font-size")).toBe(TYPE.small.size);
    expect(await style(save, "font-weight")).toBe("600");

    const radios = page.getByRole("radio");
    const priv = radios.nth(0);
    expectRect(
      await rel(page, priv),
      { x: 47, y: 542.55, width: 722, height: 60.31 },
      "Private option",
    );
    await expect(priv).toHaveAttribute("aria-checked", "true");
    expect(await colour(priv, "border-top-color")).toBe(COLOUR.accent);
    expect(await colour(priv, "background-color")).toBe(COLOUR.accentSoft);
    expect(await style(priv, "border-radius")).toBe("10px");
    expectRect(
      await rel(page, radios.nth(1)),
      { x: 47, y: 610.86, width: 722, height: 60.31 },
      "Open option",
    );
    const place = f(page, "place");
    expectRect(
      await rel(page, place),
      { x: 47, y: 807.67, width: 722, height: 50 },
      "placement",
    );
    expect(await style(place, "border-radius")).toBe("10px");
    const move = f(page, "place").getByRole("button", { name: "Chuyển" });
    expectRect(
      await rel(page, move),
      { x: 687.47, y: 818.67, width: 68.53, height: 28 },
      "move",
    );
    const danger = sets.nth(3);
    expect(await colour(danger, "border-top-color")).toBe(COLOUR.danger);
    expect(await colour(f(page, "seth").nth(3), "color")).toBe(COLOUR.danger);
    const archive = page.getByRole("button", { name: "Lưu trữ", exact: true });
    // Công nghệ still has Platform inside: Archive is drawn inert with the reason.
    await expect(archive).toHaveAttribute("aria-disabled", "true");
    await expect(page.getByText(/còn \d+ phòng ban con/)).toBeVisible();
    const remove = page.getByRole("button", { name: "Xoá phòng ban" });
    await expect(remove).toHaveAttribute("aria-disabled", "true");
    expect(await colour(remove, "border-top-color")).toBe(COLOUR.danger);
    expect(await colour(remove, "color")).toBe(COLOUR.danger);
  });

  test("the first steps of a department with nothing in it", async ({
    page,
  }) => {
    await openSpace(page, EMPTY_DEPARTMENT, "Nhân sự");
    const setup = f(page, "setup");
    const setupBox = await rel(page, setup);
    expectRect(setupBox, { x: 28, width: 712 }, "setup card");
    expect(await style(setup, "border-radius")).toBe("12px");
    expect(await colour(setup, "background-color")).toBe(COLOUR.panel);
    const top = f(page, "setup-top");
    expectRect(
      await rel(page, top),
      { x: 29, width: 710, height: 75.59 },
      "setup head",
    );
    expect(await style(top, "border-bottom-width")).toBe("1px");
    const steps = f(page, "step");
    await expect(steps).toHaveCount(3);
    const first = steps.nth(0);
    expectRect(
      await rel(page, first),
      { x: 29, width: 710, height: 68.42 },
      "step",
    );
    expect(await style(first, "border-top-width")).toBe("1px");
    const check = f(page, "ck").first();
    expectRect(
      await rel(page, check),
      { x: 47, width: 20, height: 20 },
      "done check",
    );
    expect(await colour(check, "background-color")).toBe(COLOUR.done);
    expect(await colour(check, "color")).toBe(COLOUR.panel);
    const empty = f(page, "ck").nth(1);
    // 1.5 px computes to 1 px at the window scale, as in the mockup.
    expect(await style(empty, "border-top-width")).toBe("1px");
    expect(await colour(empty, "border-top-color")).toBe(COLOUR.lineStrong);
    const addMembers = steps
      .nth(1)
      .getByRole("button", { name: "Thêm thành viên" });
    expect((await rel(page, addMembers)).height).toBeCloseTo(28, 0);
    expect(await colour(addMembers, "background-color")).toBe(COLOUR.accent);
    // The head's Add members is the primary action; Create is a plain button.
    expect(await colour(f(page, "btn-add"), "background-color")).toBe(
      COLOUR.accent,
    );
    expect(await colour(f(page, "btn-new"), "background-color")).toBe(
      COLOUR.panel,
    );
    expect((await rel(page, f(page, "col"))).width).toBeCloseTo(768, 0);
    // The access card has only the visibility, the admin and the empty members.
    await expect(f(page, "dd").nth(1)).toHaveText("Chưa có ai");
    expect(await colour(f(page, "dd").nth(1), "color")).toBe(COLOUR.meta);
  });
});

test.describe("Department page at smaller windows (owner)", () => {
  test.skip(
    persona !== "owner" || scenario !== "default" || faults !== "",
    "Owner checks need the default owner world without faults",
  );

  for (const size of [
    { width: 1280, height: 800 },
    { width: 1100, height: 760 },
  ]) {
    test(`the page fits ${size.width}x${size.height} without scrolling the window`, async ({
      page,
    }) => {
      await page.setViewportSize(size);
      await openSpace(page, ENGINEERING, "Công nghệ");
      const overflow = await page.evaluate(() => ({
        x: document.documentElement.scrollWidth - window.innerWidth,
        y: document.documentElement.scrollHeight - window.innerHeight,
      }));
      expect(overflow.x).toBeLessThanOrEqual(0);
      expect(overflow.y).toBeLessThanOrEqual(0);
      const main = await box(page.locator("#workspace-main"));
      const aside = await box(f(page, "aside"));
      expect(
        Math.abs(aside.x + aside.width - (main.x + main.width)),
      ).toBeLessThanOrEqual(1);
      const actions = await box(f(page, "acts"));
      expect(actions.x + actions.width).toBeLessThanOrEqual(
        main.x + main.width,
      );
      // The actions stay on the title row at 1100: no wrap under the title.
      const title = await box(f(page, "tr"));
      expect(actions.y).toBeLessThan(title.y + title.height);
    });
  }
});

test.describe("Department page in the dark theme (owner)", () => {
  test.skip(
    persona !== "owner" || scenario !== "default" || faults !== "",
    "Owner checks need the default owner world without faults",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  test("keeps the geometry and uses the dark surfaces", async ({ page }) => {
    await page.addInitScript(() => {
      try {
        window.localStorage.setItem("notrelix-theme-mode", "dark");
      } catch {
        // Private mode: the light theme applies.
      }
    });
    await openSpace(page, ENGINEERING, "Công nghệ");
    expectRect(
      await rel(page, f(page, "tabs")),
      { y: 96.42, height: 54 },
      "tab strip",
    );
    const list = f(page, "list").first();
    expectRect(await rel(page, list), { x: 28, width: 712 }, "list");
    expect(await colour(list, "background-color")).not.toBe(COLOUR.panel);
    expect(await colour(f(page, "h1"), "color")).not.toBe(COLOUR.ink);
    expect(await colour(f(page, "aside"), "background-color")).not.toBe(
      COLOUR.panel,
    );
  });
});
