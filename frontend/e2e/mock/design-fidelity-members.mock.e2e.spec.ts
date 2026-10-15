import { expect, test, type Locator, type Page } from "@playwright/test";

/**
 * Department members tab geometry against the October 2026 mockup. Every
 * number comes from `docs/design/mockups/2026-10/src/Final-Members.dc.html`
 * rendered at 1440x900 with `nav=hien-day` (rail pinned: the main area starts
 * at x 332 and is 1108 wide) and measured with the box dump (position, size,
 * padding, font, radius, colour) for the states danh-sach, doi-vai-tro,
 * them-nguoi, quan-tri-cuoi and go-nhom and the modes sang and dem. Boxes are
 * read relative to the top-left of the main area (for the two dialogs, to the
 * window). Tolerance is 1 px for boxes and exact for font sizes, weights,
 * radii and colours.
 *
 * The mock world differs from the mockup's: Kế toán has the Helpdesk Team
 * (Viewer) and the owner (Admin); Công nghệ has the owner (Admin), Jordan
 * (Member) and the admin (Viewer); a workspace member has no email or job
 * title, so a row's second line is the workspace role; there is no waiting
 * invitation, "via a team" chip, activity time or "Guests per Board" block.
 * Those, the long-text boxes and the mockup's 24-entry list are measured on
 * the Storybook stories (five states, light and dark, within 1 px); here only
 * boxes that do not depend on that data are compared.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const WORKSPACE = "/workspaces/mock-workspace-primary";
const ACCOUNTING = `${WORKSPACE}/spaces/mock-space-accounting?tab=members`;
const ENGINEERING = `${WORKSPACE}/spaces/mock-space-engineering?tab=members`;
const NOW = new Date("2026-10-12T10:40:00+07:00");

interface Rect {
  readonly x: number;
  readonly y: number;
  readonly width: number;
  readonly height: number;
}

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
  admin: "rgb(22, 102, 204)",
  member: "rgb(46, 139, 87)",
  viewer: "rgb(122, 131, 145)",
} as const;

async function openMembers(page: Page, path: string, heading: string) {
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
  await expect(page.locator('[data-f="mr"]').first()).toBeVisible();
}

async function box(locator: Locator): Promise<Rect> {
  const found = await locator.boundingBox();
  if (!found) throw new Error("element has no box");
  return found;
}

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

test.describe("Department members geometry at 1440 px (owner)", () => {
  test.skip(
    persona !== "owner" || scenario !== "default" || faults !== "",
    "Owner checks need the default owner world without faults",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  test("the head has no lede, Add members is primary and the tab is current", async ({
    page,
  }) => {
    await openMembers(page, ENGINEERING, "Công nghệ");
    // Công nghệ has a description, the members tab leaves the lede out.
    await expect(f(page, "lede")).toHaveCount(0);
    expectRect(
      await rel(page, f(page, "head")),
      { y: 0, height: 68.13 },
      "head",
    );
    const add = f(page, "btn-add");
    expectRect(await rel(page, add), { y: 28.06, height: 32 }, "add members");
    expect(await colour(add, "background-color")).toBe(COLOUR.accent);
    expect(await style(add, "font-weight")).toBe("600");
    const gear = await rel(page, f(page, "ib").first());
    const addBox = await rel(page, add);
    expect(Math.abs(addBox.x + addBox.width + 8 - gear.x)).toBeLessThanOrEqual(
      1,
    );
    await expect(f(page, "btn-new")).toHaveCount(0);
    const tabs = f(page, "tabs");
    expectRect(await rel(page, tabs), { y: 68.13, height: 54 }, "tabs");
    const current = f(page, "tab").nth(2);
    await expect(current).toHaveAttribute("aria-current", "page");
    expect(await style(current, "font-weight")).toBe("600");
    expect(await colour(current, "border-bottom-color")).toBe(COLOUR.accent);
    expectRect(await rel(page, f(page, "body")), { y: 122.13 }, "body");
  });

  test("the toolbar", async ({ page }) => {
    await openMembers(page, ENGINEERING, "Công nghệ");
    const field = f(page, "field");
    expectRect(
      await rel(page, field),
      { x: 28, y: 140.13, width: 220, height: 32 },
      "search",
    );
    expect(await style(field, "border-radius")).toBe("7px");
    expect(await colour(field, "border-top-color")).toBe(COLOUR.lineStrong);
    expect(await colour(field, "background-color")).toBe(COLOUR.panel);
    expect(await colour(field, "color")).toBe(COLOUR.meta);
    const input = field.locator("input");
    expect(await style(input, "font-size")).toBe("13px");
    expect(await style(input, "font-weight")).toBe("400");
    const pills = f(page, "pill");
    await expect(pills).toHaveCount(2);
    expectRect(
      await rel(page, pills.nth(0)),
      { x: 256, y: 140.13, height: 32 },
      "role pill",
    );
    expect(await style(pills.nth(0), "font-size")).toBe("13px");
    expect(await colour(pills.nth(0), "color")).toBe(COLOUR.muted);
    const value = f(page, "pillv").first();
    expect(await style(value, "font-weight")).toBe("600");
    expect(await colour(value, "color")).toBe(COLOUR.ink);
    const pillTwo = await rel(page, pills.nth(1));
    const pillOne = await rel(page, pills.nth(0));
    expect(
      Math.abs(pillOne.x + pillOne.width + 8 - pillTwo.x),
    ).toBeLessThanOrEqual(1);
    const sum = f(page, "sum");
    expect(await style(sum, "font-size")).toBe("12.5px");
    expect(await colour(sum, "color")).toBe(COLOUR.meta);
  });

  test("the list, its head and a person's row", async ({ page }) => {
    await openMembers(page, ENGINEERING, "Công nghệ");
    const list = f(page, "list");
    expectRect(await rel(page, list), { x: 28, width: 722 }, "list");
    expect(await style(list, "border-radius")).toBe("12px");
    expect(await colour(list, "background-color")).toBe(COLOUR.panel);
    expect(await colour(list, "border-top-color")).toBe(COLOUR.line);
    const head = f(page, "lh");
    expectRect(
      await rel(page, head),
      { x: 29, width: 720, height: 37 },
      "list head",
    );
    expect(await colour(head, "background-color")).toBe(COLOUR.page);
    expect(await style(head, "font-size")).toBe("11.5px");
    expect(await style(head, "font-weight")).toBe("600");
    expect(await style(head, "border-top-left-radius")).toBe("12px");
    const firstRow = f(page, "mr").first();
    const firstBox = await rel(page, firstRow);
    expectRect(firstBox, { x: 29, width: 720 }, "row");
    expect(firstBox.height).toBeGreaterThanOrEqual(56);
    const headBox = await rel(page, head);
    // The first row touches the head with no border of its own.
    expect(
      Math.abs(firstBox.y - (headBox.y + headBox.height)),
    ).toBeLessThanOrEqual(1);
    // The teams lead the list: the person's row is the first with an avatar.
    const row = f(page, "mr")
      .filter({ has: page.locator('[data-f="who"] > [data-f="av"]') })
      .first();
    const avatar = row.locator('[data-f="who"] > [data-f="av"]');
    expectRect(
      await rel(page, avatar),
      { x: 43, width: 30, height: 30 },
      "avatar",
    );
    expect(await style(avatar, "font-size")).toBe("10.5px");
    const name = row.locator('[data-f="wname"]');
    expectRect(await rel(page, name), { x: 84, height: 20.3 }, "name");
    expect(await style(name, "font-size")).toBe("14px");
    expect(await style(name, "font-weight")).toBe("600");
    expect(await colour(name, "color")).toBe(COLOUR.ink);
    const sub = row.locator('[data-f="wsub"]');
    expect(await style(sub, "font-size")).toBe("12px");
    expect(await colour(sub, "color")).toBe(COLOUR.meta);
    const role = row.locator('[data-f="rsel"]');
    expectRect(await rel(page, role), { x: 193, height: 30 }, "role");
    expect(await style(role, "font-size")).toBe("13px");
    expect(await style(role, "font-weight")).toBe("500");
    expect(await style(role, "border-radius")).toBe("7px");
    const tag = role.locator('[data-f="rtag"]');
    expectRect(
      await rel(page, tag),
      { x: 203, width: 8, height: 8 },
      "role tag",
    );
    expect(await style(tag, "border-radius")).toBe("2px");
    // Admin is blue, Member green, Viewer grey.
    expect(await colour(tag, "background-color")).toBe(COLOUR.admin);
    const src = row.locator('[data-f="src"]');
    expectRect(
      await rel(page, src),
      { x: 357, width: 210, height: 22 },
      "source",
    );
    const chip = row.locator('[data-f="srcc"]').first();
    expect(await style(chip, "border-radius")).toBe("5px");
    expect(await colour(chip, "background-color")).toBe(COLOUR.hover);
    expect(await colour(chip, "color")).toBe(COLOUR.muted);
    expect(await style(chip, "font-size")).toBe("12px");
    const act = row.locator('[data-f="act"]');
    expectRect(await rel(page, act), { x: 581, width: 104 }, "activity");
    expect(await colour(act, "color")).toBe(COLOUR.meta);
    const more = row.locator('[data-f="ib"]');
    expectRect(
      await rel(page, more),
      { x: 699, width: 32, height: 32 },
      "row menu",
    );
    expect(await style(more, "border-radius")).toBe("7px");
    // Member and Viewer tags carry their colours.
    const rows = f(page, "mr");
    expect(
      await colour(
        rows.filter({ hasText: "Jordan Lee" }).locator('[data-f="rtag"]'),
        "background-color",
      ),
    ).toBe(COLOUR.member);
    expect(
      await colour(
        rows.filter({ hasText: "Ban giám đốc" }).locator('[data-f="rtag"]'),
        "background-color",
      ),
    ).toBe(COLOUR.viewer);
    // "You" marks the signed-in member.
    const you = f(page, "you");
    expect(await style(you, "font-size")).toBe("11px");
    expect(await style(you, "font-weight")).toBe("600");
    expect(await colour(you, "color")).toBe(COLOUR.accentText);
    expect(await colour(you, "background-color")).toBe(COLOUR.accentSoft);
    expect(await style(you, "border-radius")).toBe("4px");
  });

  test("a team row expands to its people", async ({ page }) => {
    await openMembers(page, ACCOUNTING, "Kế toán");
    const row = f(page, "mr").first();
    const toggle = row.locator('[data-f="tw"]');
    expectRect(
      await rel(page, toggle),
      { x: 43, width: 22, height: 22 },
      "expand",
    );
    expect(await style(toggle, "border-radius")).toBe("6px");
    await expect(toggle).toHaveAttribute("aria-expanded", "false");
    const mark = row.locator('[data-f="tm"]');
    expectRect(
      await rel(page, mark),
      { x: 70, width: 30, height: 30 },
      "team mark",
    );
    expect(await style(mark, "border-radius")).toBe("9px");
    expect(await colour(mark, "background-color")).toBe(COLOUR.hover);
    expect(await colour(mark, "color")).toBe(COLOUR.muted);
    expect(
      Math.abs((await rel(page, row.locator('[data-f="wname"]'))).x - 111),
    ).toBeLessThanOrEqual(1);
    await toggle.click();
    await expect(toggle).toHaveAttribute("aria-expanded", "true");
    const sub = f(page, "sub");
    await expect(sub).toBeVisible();
    const subBox = await rel(page, sub);
    const rowBox = await rel(page, row);
    expectRect(subBox, { x: 29, width: 720 }, "people");
    expect(Math.abs(subBox.y - (rowBox.y + rowBox.height))).toBeLessThanOrEqual(
      1,
    );
    expect(await colour(sub, "background-color")).toBe(COLOUR.page);
    expect(await colour(sub, "border-top-color")).toBe(COLOUR.line);
    const first = f(page, "sp").first();
    expectRect(
      await rel(page, first),
      { x: 85, y: subBox.y + 8, width: 650, height: 30 },
      "person",
    );
    const av = first.locator('[data-f="av"]');
    expectRect(
      await rel(page, av),
      { x: 85, width: 22, height: 22 },
      "person avatar",
    );
    expect(await style(av, "font-size")).toBe("8.5px");
    const hint = f(page, "hint");
    expect(await style(hint, "font-size")).toBe("12px");
    await toggle.click();
    await expect(sub).toHaveCount(0);
  });

  test("the aside explains the roles", async ({ page }) => {
    await openMembers(page, ENGINEERING, "Công nghệ");
    const aside = f(page, "aside");
    expectRect(
      await rel(page, aside),
      { x: 778, y: 122.13, width: 330 },
      "aside",
    );
    expect(await colour(aside, "background-color")).toBe(COLOUR.panel);
    expect(await colour(aside, "border-left-color")).toBe(COLOUR.line);
    const heading = f(page, "abh").first();
    expectRect(
      await rel(page, heading),
      { x: 799, y: 142.13, height: 18.84 },
      "heading",
    );
    expect(await style(heading, "font-size")).toBe("13px");
    expect(await style(heading, "font-weight")).toBe("600");
    expect(await colour(heading, "color")).toBe(COLOUR.muted);
    const roles = f(page, "rl");
    await expect(roles).toHaveCount(3);
    expectRect(
      await rel(page, roles.first()),
      { x: 799, y: 170.97, width: 289 },
      "role row",
    );
    const tag = roles.first().locator('[data-f="rtag"]');
    expectRect(
      await rel(page, tag),
      { x: 799, width: 8, height: 8 },
      "role tag",
    );
    const callout = f(page, "callout").first();
    expectRect(await rel(page, callout), { x: 799, width: 289 }, "callout");
    expect(await style(callout, "border-radius")).toBe("10px");
    expect(await colour(callout, "background-color")).toBe(COLOUR.page);
    expect(await colour(callout, "border-top-color")).toBe(COLOUR.line);
    expect(await style(callout, "font-size")).toBe("12.5px");
    const map = f(page, "map");
    expect(await style(map, "border-top-width")).toBe("1px");
    expect(await colour(map, "color")).toBe(COLOUR.muted);
  });

  test("the role menu", async ({ page }) => {
    await openMembers(page, ENGINEERING, "Công nghệ");
    const row = f(page, "mr").nth(1);
    await row.locator('[data-f="rsel"]').click();
    const menu = f(page, "rmenu");
    await expect(menu).toBeVisible();
    const rowBox = await rel(page, row);
    const menuBox = await rel(page, menu);
    expectRect(
      menuBox,
      { x: rowBox.x + 228, y: rowBox.y + 52, width: 322 },
      "menu",
    );
    expect(await style(menu, "border-radius")).toBe("10px");
    expect(await colour(menu, "background-color")).toBe(COLOUR.panel);
    expect(await colour(menu, "border-top-color")).toBe(COLOUR.lineStrong);
    expect(await style(menu, "box-shadow")).not.toBe("none");
    // The row reads selected while its menu is open.
    expect(await colour(row, "background-color")).toBe(COLOUR.accentSoft);
    const items = f(page, "ro");
    await expect(items).toHaveCount(4);
    expectRect(
      await rel(page, items.first()),
      { x: menuBox.x + 5, y: menuBox.y + 5, width: 312 },
      "item",
    );
    expect(await style(items.first(), "border-radius")).toBe("7px");
    const title = f(page, "rot").first();
    expect(await style(title, "font-size")).toBe("13.5px");
    expect(await style(title, "font-weight")).toBe("600");
    const hint = f(page, "roh").first();
    expect(await style(hint, "font-size")).toBe("12px");
    expect(await colour(hint, "color")).toBe(COLOUR.meta);
    expect(await colour(f(page, "rot").last(), "color")).toBe(COLOUR.danger);
    expectRect(
      await rel(page, f(page, "rsep")),
      { width: 320, height: 1 },
      "separator",
    );
    const note = f(page, "rnote");
    expect(await colour(note, "border-top-color")).toBe(COLOUR.line);
    expect(await style(note, "font-size")).toBe("12px");
    expect(await colour(note, "color")).toBe(COLOUR.muted);
  });

  test("the add dialog and the remove dialog sit 96 px under the top, 600 and 460 wide", async ({
    page,
  }) => {
    await openMembers(page, ENGINEERING, "Công nghệ");
    await f(page, "btn-add").click();
    const dialog = f(page, "dlg");
    await expect(dialog).toBeVisible();
    // The dialog zooms in over 100 ms; measure it at rest.
    await page.waitForTimeout(250);
    const add = await box(dialog);
    expectRect(add, { x: 420, y: 96, width: 600 }, "add dialog");
    expect(await style(dialog, "border-radius")).toBe("14px");
    expect(await colour(dialog, "background-color")).toBe(COLOUR.panel);
    const title = f(page, "dt");
    expect(await style(title, "font-size")).toBe("18px");
    expect(await style(title, "font-weight")).toBe("650");
    const tok = f(page, "tok");
    const tokBox = await box(tok);
    expect(Math.abs(tokBox.x - (add.x + 20))).toBeLessThanOrEqual(1);
    expect(tokBox.width).toBeCloseTo(560, 0);
    expect(await style(tok, "border-radius")).toBe("10px");
    expect(await colour(tok, "border-top-color")).toBe(COLOUR.accent);
    const foot = f(page, "df");
    expect(await colour(foot, "background-color")).toBe(COLOUR.page);
    expect(await colour(foot, "border-top-color")).toBe(COLOUR.line);
    expect((await box(foot)).width).toBeCloseTo(600, 0);
    const submit = f(page, "btn-pri");
    expect(await colour(submit, "background-color")).toBe(COLOUR.accent);
    await expect(submit).toHaveAttribute("aria-disabled", "true");
    // Escape closes it and focus returns to the button that opened it.
    await page.keyboard.press("Escape");
    await expect(dialog).toBeHidden();
    await expect(f(page, "btn-add")).toBeFocused();
  });
});

test.describe("Department members at smaller windows (owner)", () => {
  test.skip(
    persona !== "owner" || scenario !== "default" || faults !== "",
    "Owner checks need the default owner world without faults",
  );
  for (const size of [
    { width: 1280, height: 800 },
    { width: 1100, height: 760 },
  ]) {
    test(`the tab fits ${size.width}x${size.height}`, async ({ page }) => {
      await page.setViewportSize(size);
      await openMembers(page, ENGINEERING, "Công nghệ");
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
      const list = await box(f(page, "list"));
      expect(list.x + list.width).toBeLessThanOrEqual(aside.x + 1);
    });
  }
});
