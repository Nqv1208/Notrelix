import { expect, test, type Locator, type Page } from "@playwright/test";

/**
 * Design fidelity of the Notrelix system rail, the workspace sidebar and the
 * shell composition at 1440x900. Every number below is a box measured from the
 * mockup at the same viewport (light mode), tolerance +-1 px:
 *
 *   python3 docs/design/mockups/2026-10/render.py Final-Board.dc.html out.html \
 *     state=binh-thuong mode=sang nav=<an|hien-de|hien-day> detail=dong
 *
 * `nav=hien-day` is the pinned rail, `nav=hien-de` the overlay, `nav=an` no
 * rail. The mock world has two workspaces where the mockup has three and a
 * Private "Công nghệ" Space holding the ERP board where the mockup has
 * "Cong nghe"; the structure and geometry are asserted, not the text.
 */

const scenario = process.env.VITE_MOCK_STATE ?? "default";
const board = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const RAIL_KEY = "notrelix-system-rail";
const THEME_KEY = "notrelix-theme-mode";

test.use({ viewport: { width: 1440, height: 900 } });

test.beforeEach(async ({ page }) => {
  test.skip(scenario !== "default", "Assertions need the default world data");
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "vi");
    } catch {
      // Private mode: the default locale applies.
    }
  });
});

interface Box {
  readonly x: number;
  readonly y: number;
  readonly width: number;
  readonly height: number;
}

async function openBoard(
  page: Page,
  options: { rail?: "pinned"; dark?: boolean } = {},
) {
  await page.addInitScript(
    ({ rail, dark, railKey, themeKey }) => {
      try {
        if (rail) window.localStorage.setItem(railKey, rail);
        if (dark) window.localStorage.setItem(themeKey, "dark");
      } catch {
        // Private mode: defaults apply.
      }
    },
    {
      rail: options.rail,
      dark: options.dark,
      railKey: RAIL_KEY,
      themeKey: THEME_KEY,
    },
  );
  await page.goto(board);
  await expect(
    page.getByRole("heading", { level: 1, name: /Triển khai ERP Q4/ }),
  ).toBeVisible();
  // The shell's width transitions settle.
  await page.waitForTimeout(400);
}

async function box(locator: Locator): Promise<Box> {
  const result = await locator.first().boundingBox();
  expect(result, "element is laid out").not.toBeNull();
  return result as Box;
}

/** x, y, width and height within 1 px of the mockup. */
async function expectBox(
  locator: Locator,
  expected: Partial<Box>,
  tolerance = 1,
) {
  const actual = await box(locator);
  for (const key of ["x", "y", "width", "height"] as const) {
    const want = expected[key];
    if (want === undefined) continue;
    expect(
      Math.abs(actual[key] - want),
      `${key}: ${actual[key]} vs ${want}`,
    ).toBeLessThanOrEqual(tolerance);
  }
}

async function css(locator: Locator, property: string): Promise<string> {
  return locator
    .first()
    .evaluate(
      (element, name) => getComputedStyle(element).getPropertyValue(name),
      property,
    );
}

async function expectFont(
  locator: Locator,
  size: number,
  weight: number,
  tolerance = 0.05,
) {
  expect(
    Math.abs(parseFloat(await css(locator, "font-size")) - size),
    "font-size",
  ).toBeLessThanOrEqual(tolerance);
  expect(parseInt(await css(locator, "font-weight"), 10), "font-weight").toBe(
    weight,
  );
}

/** A fully rounded box (the mockup's `border-radius: 50%`). */
async function expectRound(locator: Locator) {
  const radius = parseFloat(await css(locator, "border-radius"));
  const { width } = await box(locator);
  expect(radius, "border-radius").toBeGreaterThanOrEqual(width / 2);
}

function rgbOf(value: string): [number, number, number] {
  // getComputedStyle may report oklch()/color(); resolve through a canvas.
  return value as unknown as [number, number, number];
}

/** Resolved sRGB of a CSS property (the app styles with oklch tokens). */
async function rgb(
  locator: Locator,
  property: string,
): Promise<[number, number, number]> {
  const channels = await locator.first().evaluate((element, name) => {
    const canvas = document.createElement("canvas");
    canvas.width = canvas.height = 1;
    const context = canvas.getContext("2d", { willReadFrequently: true })!;
    context.fillStyle = "#000";
    context.fillStyle = getComputedStyle(element).getPropertyValue(name);
    context.fillRect(0, 0, 1, 1);
    const [r, g, b] = context.getImageData(0, 0, 1, 1).data;
    return [r, g, b];
  }, property);
  return rgbOf(channels as unknown as string) as unknown as [
    number,
    number,
    number,
  ];
}

async function expectRgb(
  locator: Locator,
  property: string,
  expected: readonly [number, number, number],
  tolerance = 1,
) {
  const actual = await rgb(locator, property);
  actual.forEach((channel, index) =>
    expect(
      Math.abs(channel - expected[index]!),
      `${property} ${actual.join(",")} vs ${expected.join(",")}`,
    ).toBeLessThanOrEqual(tolerance),
  );
}

const rail = (page: Page) =>
  page.getByRole("navigation", { name: "Hệ thống Notrelix" });
const sidebar = (page: Page) => page.locator('[data-slot="sidebar-inner"]');
const sidebarNav = (page: Page) =>
  page.getByRole("navigation", { name: "Điều hướng không gian làm việc" });

test.describe("rail pinned (nav=hien-day)", () => {
  test.beforeEach(async ({ page }) => {
    await openBoard(page, { rail: "pinned" });
  });

  test("the rail is 76 x 900 on the dark rail colour with the logo at 21,12", async ({
    page,
  }) => {
    const nav = rail(page);
    await expectBox(nav, { x: 0, y: 0, width: 76, height: 900 });
    await expectRgb(nav, "background-color", [22, 32, 46]);
    expect(await css(nav, "padding-top")).toBe("12px");
    expect(await css(nav, "padding-bottom")).toBe("12px");
    expect(await css(nav, "row-gap")).toBe("4px");
    await expectBox(nav.locator("svg").first(), {
      x: 21,
      y: 12,
      width: 34,
      height: 34,
    });
  });

  test("workspace tiles are 64 x 71 with a 40 x 40 r11 mark, a 10.5/500 label and a 75 px pitch", async ({
    page,
  }) => {
    const tiles = rail(page).locator(":scope > button");
    const first = tiles.nth(0);
    const second = tiles.nth(1);
    await expectBox(first, { x: 6, y: 62, width: 64, height: 71 });
    await expectBox(second, { x: 6, y: 137, width: 64, height: 71 });
    expect(await css(first, "border-radius")).toBe("10px");
    expect(await css(first, "padding-top")).toBe("7px");
    expect(await css(first, "padding-bottom")).toBe("6px");
    expect(await css(first, "row-gap")).toBe("3px");
    const mark = first.locator("span").first();
    await expectBox(mark, { x: 18, y: 69, width: 40, height: 40 });
    expect(await css(mark, "border-radius")).toBe("11px");
    await expectFont(mark, 13, 700);
    await expectFont(first.locator("span").nth(1), 10.5, 500);
    // The current workspace label is white, the others the rail ink.
    await expectRgb(first, "color", [255, 255, 255]);
    await expectRgb(second, "color", [175, 186, 203], 2);
  });

  test("Add is 64 x 65 with a 40 x 34 r9 dashed outline", async ({ page }) => {
    const add = rail(page).locator(":scope > button").nth(3);
    await expectBox(add, { x: 6, y: 287, width: 64, height: 65 });
    const outline = add.locator("span").first();
    await expectBox(outline, { x: 18, y: 294, width: 40, height: 34 });
    expect(await css(outline, "border-radius")).toBe("9px");
    expect(await css(outline, "border-top-style")).toBe("dashed");
    await expectBox(outline.locator("svg"), { width: 20, height: 20 });
  });

  test("the divider is 36 x 1, the all-workspace entries are 64 x 51 on a 55 px pitch and Search stays inert", async ({
    page,
  }) => {
    const nav = rail(page);
    const tiles = nav.locator(":scope > button");
    const add = await box(tiles.nth(3));
    const divider = nav.locator(":scope > span[aria-hidden]").first();
    await expectBox(divider, {
      x: 20,
      y: add.y + add.height + 4 + 6,
      width: 36,
      height: 1,
    });
    const inbox = tiles.nth(4);
    const search = tiles.nth(5);
    await expectBox(inbox, { x: 6, width: 64, height: 51 });
    await expectBox(search, { x: 6, width: 64, height: 51 });
    const inboxBox = await box(inbox);
    const dividerBox = await box(divider);
    expect(inboxBox.y - (dividerBox.y + 1)).toBeGreaterThanOrEqual(9);
    expect(inboxBox.y - (dividerBox.y + 1)).toBeLessThanOrEqual(11);
    expect((await box(search)).y - inboxBox.y).toBeGreaterThanOrEqual(54);
    expect((await box(search)).y - inboxBox.y).toBeLessThanOrEqual(56);
    await expectBox(inbox.locator("svg"), { width: 20, height: 20 });
    // The all-workspace inbox has a producer now (unread summary); search has
    // none yet: active look, no "unavailable" label inside the tile.
    await expect(inbox).not.toHaveAttribute("aria-disabled", "true");
    for (const tile of [search]) {
      await expect(tile).toHaveAttribute("aria-disabled", "true");
      expect(await css(tile, "opacity")).toBe("1");
      await expect(tile).toHaveAttribute("title", /.+\..+/);
      expect(await box(tile.locator("span").first())).toBeTruthy();
    }
    await expect(rail(page).getByText("Chưa khả dụng")).toHaveCount(1);
    for (const reason of await rail(page).getByText("Chưa khả dụng").all()) {
      const reasonBox = await box(reason);
      expect(reasonBox.width).toBeLessThanOrEqual(1);
    }
  });

  test("Settings is 64 x 51 at the bottom and the 32 x 32 round user avatar ends 12 px above the edge", async ({
    page,
  }) => {
    const nav = rail(page);
    const settings = nav
      .locator(":scope > button")
      .filter({ hasText: "Cài đặt" });
    await expectBox(settings, { x: 6, y: 795, width: 64, height: 51 });
    // The mock user has a photo; initials would use 10.5/700 (rail-label bold).
    const avatar = nav.locator('[role="img"]').last();
    await expectBox(avatar, { x: 22, y: 856, width: 32, height: 32 });
    await expectRound(avatar.locator("img, span").first());
  });

  test("the pin control keeps the design's positions: it sits above Settings in the free space", async ({
    page,
  }) => {
    const pin = rail(page).getByRole("button", {
      name: "Giữ thanh hệ thống Notrelix luôn mở",
    });
    const settings = rail(page)
      .locator(":scope > button")
      .filter({ hasText: "Cài đặt" });
    expect((await box(pin)).y + (await box(pin)).height).toBeLessThanOrEqual(
      (await box(settings)).y,
    );
    await expectBox(pin, { width: 32, height: 32 });
  });

  test("the sidebar sits right of the rail, 256 wide, on the sidebar colour with its 1 px line", async ({
    page,
  }) => {
    const inner = sidebar(page);
    const container = page.locator('[data-slot="sidebar-container"]');
    await expectBox(container, { x: 76, y: 0, width: 256, height: 900 });
    await expectRgb(inner, "background-color", [239, 241, 244]);
    await expectRgb(container, "border-right-color", [225, 229, 234]);
    expect(await css(container, "border-right-width")).toBe("1px");
  });

  test("the workspace switcher is 235 x 54 at 86,10 with a 28 x 28 r8 mark and a 16 px chevron", async ({
    page,
  }) => {
    const chip = page.locator("[data-workspace-chip]");
    await expectBox(chip, { x: 86, y: 10, width: 235, height: 54 });
    expect(await css(chip, "padding-left")).toBe("8px");
    expect(await css(chip, "column-gap")).toBe("10px");
    expect(await css(chip, "border-radius")).toBe("8px");
    const mark = chip.locator("span").first();
    await expectBox(mark, { x: 94, y: 23, width: 28, height: 28 });
    expect(await css(mark, "border-radius")).toBe("8px");
    await expectFont(mark, 11, 700);
    const name = chip.locator("span").nth(2);
    await expectFont(name, 14, 600);
    await expectFont(chip.locator("span").nth(3), 12, 400);
    await expectBox(chip.locator("svg"), { x: 297, width: 16, height: 16 });
    await expectRgb(chip.locator("span").nth(3), "color", [95, 104, 117]);
  });

  test("search is 235 x 34 at y=72 with a 1 px line border, 13/400 text and a 25 x 20 r4 key", async ({
    page,
  }) => {
    const search = sidebarNav(page)
      .locator("xpath=..")
      .getByRole("button", { name: /Tìm hoặc chạy lệnh/ });
    await expectBox(search, { x: 86, y: 72, width: 235, height: 34 });
    expect(await css(search, "border-radius")).toBe("8px");
    expect(await css(search, "padding-left")).toBe("10px");
    expect(await css(search, "column-gap")).toBe("8px");
    await expectRgb(search, "border-top-color", [225, 229, 234]);
    await expectRgb(search, "background-color", [255, 255, 255]);
    await expectFont(search, 13, 400);
    await expectRgb(search, "color", [95, 104, 117]);
    const key = search.locator("kbd");
    await expectBox(key, { x: 285, y: 79, width: 25, height: 20 });
    expect(await css(key, "border-radius")).toBe("4px");
    await expectFont(key, 11, 400);
  });

  test("the five navigation rows are 235 x 34 on a 36 px pitch from y=116 with 13.5/500 muted text", async ({
    page,
  }) => {
    const rows = page.getByRole("list", { name: "Chính" }).locator("a");
    await expect(rows).toHaveCount(5);
    for (let index = 0; index < 5; index += 1) {
      const row = rows.nth(index);
      await expectBox(row, {
        x: 86,
        y: 116 + index * 36,
        width: 235,
        height: 34,
      });
      expect(await css(row, "border-radius")).toBe("7px");
      expect(await css(row, "padding-left")).toBe("10px");
      expect(await css(row, "column-gap")).toBe("10px");
      await expectFont(row, 13.5, 500);
      await expectBox(row.locator("svg").first(), {
        x: 96,
        width: 16,
        height: 16,
      });
    }
    await expectRgb(rows.nth(1), "color", [79, 88, 102]);
    // The count is plain 12/500 meta text 17 high.
    const count = rows.nth(1).locator("span").last();
    await expectFont(count, 12, 500);
    expect((await box(count)).height).toBeGreaterThanOrEqual(16);
    expect((await box(count)).height).toBeLessThanOrEqual(18);
  });

  test("the section label is 215 x 24 at 96,312 with a 24 x 24 r6 add button at x=287", async ({
    page,
  }) => {
    const label = page.getByText("Phòng ban", { exact: true });
    await expectFont(label, 12, 600);
    await expectRgb(label, "color", [95, 104, 117]);
    const row = label.locator("xpath=..");
    await expectBox(row, { x: 96, y: 312, width: 215, height: 24 });
    const add = page.getByRole("button", { name: "Tạo phòng ban" });
    await expectBox(add, { x: 287, y: 312, width: 24, height: 24 });
    expect(await css(add, "border-radius")).toBe("6px");
  });

  test("the expanded Space shows 209 x 32 row + 24 x 24 toggle, 235 x 30 board rows indented 39 px and 16 px sub-Space marks", async ({
    page,
  }) => {
    const tree = page.getByRole("tree", { name: "Phòng ban" });
    const space = tree.getByRole("treeitem").nth(0);
    await expect(space).toHaveAttribute("aria-expanded", "true");
    await expectBox(space, { x: 86, y: 342, width: 209, height: 32 });
    expect(await css(space, "border-radius")).toBe("7px");
    expect(await css(space, "padding-left")).toBe("10px");
    expect(await css(space, "column-gap")).toBe("9px");
    await expectFont(space, 13.5, 500);
    const mark = space.locator("span").first();
    await expectBox(mark, { x: 96, y: 348, width: 20, height: 20 });
    expect(await css(mark, "border-radius")).toBe("6px");
    await expectFont(mark, 9, 700);
    // The Private lock is 13 x 13 and ends 10 px inside the row.
    await expectBox(space.locator("svg"), { x: 272, width: 13, height: 13 });
    const toggle = tree.locator("[data-tree-toggle]").first();
    await expectBox(toggle, { x: 297, y: 346, width: 24, height: 24 });
    expect(await css(toggle, "border-radius")).toBe("6px");

    const current = tree.getByRole("treeitem", { name: /Triển khai ERP Q4/ });
    await expectBox(current, { x: 86, y: 376, width: 235, height: 30 });
    expect(await css(current, "padding-left")).toBe("39px");
    expect(await css(current, "border-radius")).toBe("7px");
    await expectFont(current, 13, 600);

    const sub = tree.getByRole("treeitem", { name: /Hạ tầng & bảo mật/ });
    const subBox = await box(sub);
    expect(subBox.height).toBe(30);
    expect(subBox.width).toBe(235);
    // Boards come first (ERP, Hỗ trợ nội bộ, Ngân sách CNTT 2027), then the
    // sub-Spaces: three 32 px rows below the current Board.
    expect(subBox.y - (await box(current)).y).toBe(96);
    expect(await css(sub, "padding-left")).toBe("39px");
    await expectFont(sub, 13, 400);
    const subMark = sub.locator("span").first();
    await expectBox(subMark, { x: 125, width: 16, height: 16 });
    expect(await css(subMark, "border-radius")).toBe("5px");
    await expectFont(subMark, 7, 700);

    // The next Space starts 2 px below the last child row (two more sub-Spaces
    // follow this one: Nhóm Hạ tầng and Nhóm Ứng dụng).
    const next = tree.getByRole("treeitem", { name: /Kế toán/ });
    expect((await box(next)).y - (subBox.y + subBox.height)).toBe(2 + 2 * 32);
    // Kế toán holds a Board (KT-88's), so its row has a toggle like the
    // expanded Space's: 209 wide, the lock 10 px inside it.
    await expectBox(next, { x: 86, width: 209, height: 32 });
    await expectBox(next.locator("svg"), { x: 272, width: 13, height: 13 });
  });

  test("the footer has Workspace setup 235 x 34 at y=796 and the user card 235 x 52 at y=836 with a 26 x 26 round avatar", async ({
    page,
  }) => {
    const setup = sidebarNav(page).getByRole("button", {
      name: "Thiết lập workspace",
    });
    await expectBox(setup, { x: 86, y: 796, width: 235, height: 34 });
    await expectFont(setup, 13.5, 500);
    const card = page.getByRole("button", { name: /Menu người dùng|menu/i });
    await expectBox(card, { x: 86, y: 836, width: 235, height: 52 });
    expect(await css(card, "padding-left")).toBe("8px");
    expect(await css(card, "column-gap")).toBe("9px");
    const avatar = card.locator("img, span").first();
    await expectBox(avatar, { x: 94, y: 849, width: 26, height: 26 });
    await expectRound(avatar);
    // Name 13/600, role line 12/400 meta.
    await expectFont(card.locator("span span").nth(0), 13, 600);
    await expectFont(card.locator("span span").nth(1), 12, 400);
    await expectRgb(card.locator("span span").nth(1), "color", [95, 104, 117]);
  });

  test("the main area starts at the top edge: there is no desktop header bar", async ({
    page,
  }) => {
    await expect(page.locator("header")).toHaveCount(0);
    await expect(page.getByRole("banner")).toHaveCount(0);
    await expectBox(page.locator("#workspace-main"), {
      x: 332,
      y: 0,
      width: 1108,
      height: 900,
    });
  });
});

test.describe("no rail (nav=an)", () => {
  test.beforeEach(async ({ page }) => {
    await openBoard(page);
  });

  test("the sidebar starts at x=0, its contents at x=10, and the main area at x=256", async ({
    page,
  }) => {
    await expect(rail(page)).toHaveCount(0);
    await expectBox(page.locator('[data-slot="sidebar-container"]'), {
      x: 0,
      y: 0,
      width: 256,
      height: 900,
    });
    await expectBox(page.locator("[data-workspace-chip]"), {
      x: 10,
      y: 10,
      width: 235,
      height: 54,
    });
    const rows = page.getByRole("list", { name: "Chính" }).locator("a");
    await expectBox(rows.first(), { x: 10, y: 116, width: 235, height: 34 });
    await expectBox(page.locator("#workspace-main"), {
      x: 256,
      y: 0,
      width: 1184,
      height: 900,
    });
  });
});

test.describe("rail overlay (nav=hien-de)", () => {
  test("the rail covers the sidebar at x=0 over a scrim and the sidebar keeps x=0", async ({
    page,
  }) => {
    await openBoard(page);
    await page.locator("[data-workspace-chip]").click();
    const nav = rail(page);
    await expect(nav).toHaveAttribute("data-presentation", "overlay");
    await page.waitForTimeout(400);
    await expectBox(nav, { x: 0, y: 0, width: 76, height: 900 });
    await expectRgb(nav, "background-color", [22, 32, 46]);
    const tiles = nav.locator(":scope > button");
    await expectBox(tiles.nth(0), { x: 6, y: 62, width: 64, height: 71 });
    await expectBox(page.locator('[data-slot="sidebar-container"]'), {
      x: 0,
      y: 0,
      width: 256,
    });
    // The scrim is the mockup's rgba(15,23,42,.14) over everything else.
    const scrim = page.locator('[data-slot="system-rail-scrim"]');
    await expectBox(scrim, { x: 0, y: 0, width: 1440, height: 900 });
    const alpha = await scrim.evaluate((element) => {
      const canvas = document.createElement("canvas");
      canvas.width = canvas.height = 1;
      const context = canvas.getContext("2d", { willReadFrequently: true })!;
      context.fillStyle = getComputedStyle(element).backgroundColor;
      context.fillRect(0, 0, 1, 1);
      return Array.from(context.getImageData(0, 0, 1, 1).data);
    });
    expect(Math.abs(alpha[3]! - Math.round(0.14 * 255))).toBeLessThanOrEqual(2);
    expect(Math.abs(alpha[0]! - 15)).toBeLessThanOrEqual(2);
    expect(Math.abs(alpha[1]! - 23)).toBeLessThanOrEqual(2);
    expect(Math.abs(alpha[2]! - 42)).toBeLessThanOrEqual(2);
  });
});

test.describe("dark appearance (mode=dem) keeps token-driven colours", () => {
  test("rail, sidebar, line, meta text and key follow the dark tokens (the dark panel token is #151A21, not the mockup's #1A2028)", async ({
    page,
  }) => {
    await openBoard(page, { rail: "pinned", dark: true });
    await expectRgb(rail(page), "background-color", [5, 7, 10]);
    await expectRgb(sidebar(page), "background-color", [19, 24, 30]);
    const container = page.locator('[data-slot="sidebar-container"]');
    await expectRgb(container, "border-right-color", [35, 43, 53]);
    const search = sidebarNav(page)
      .locator("xpath=..")
      .getByRole("button", { name: /Tìm hoặc chạy lệnh/ });
    await expectRgb(search, "color", [143, 153, 167]);
    await expectRgb(search.locator("kbd"), "background-color", [35, 43, 53]);
    const rows = page.getByRole("list", { name: "Chính" }).locator("a");
    await expectRgb(rows.nth(0), "color", [170, 179, 192]);
    // Geometry is theme independent.
    await expectBox(search, { x: 86, y: 72, width: 235, height: 34 });
  });
});

test.describe("below the desktop breakpoint", () => {
  test("a narrow screen keeps the top bar that owns the navigation drawer", async ({
    page,
  }) => {
    await page.setViewportSize({ width: 390, height: 800 });
    await openBoard(page);
    await expect(page.locator("header")).toHaveCount(1);
    await expect(
      page.getByRole("button", { name: "Hiện hoặc ẩn điều hướng" }),
    ).toBeVisible();
  });
});
