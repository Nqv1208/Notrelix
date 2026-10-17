import { expect, test, type Locator, type Page } from "@playwright/test";

/**
 * Board Kanban geometry against the October 2026 mockup. Every number comes
 * from `docs/design/mockups/2026-10/src/Final-Board.dc.html` rendered at
 * 1440x900 with `nav=an` and measured with the box dump (position, size,
 * padding, gap, font, radius). Boxes are read relative to the main area's
 * top-left, so the checks hold whatever sits left of or above the main area
 * (rail, sidebar, shell header). Tolerance is 1 px for boxes and exact for
 * font sizes and radii.
 *
 * Text differs between the mock world and the mockup world, so only boxes that
 * do not depend on the wording are compared (a tab or a button keeps its
 * width because the Vietnamese label is the same; a chip whose text differs
 * is checked by height only).
 *
 * The page clock is fixed to the fixtures' 12/10/2026 10:40. The viewer
 * checks run when the server was started with VITE_MOCK_PERSONA=viewer.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const NOW = new Date("2026-10-12T10:40:00+07:00");
const ERP_142 = "Chuyển dữ liệu khách hàng từ hệ thống cũ sang ERP";

interface Rect {
  readonly x: number;
  readonly y: number;
  readonly width: number;
  readonly height: number;
}

/** Final-Board state=binh-thuong detail=dong nav=an. Origin: main area (256,0). */
const MOCK = {
  header: {
    spaceMark: { x: 24, y: 22, width: 34, height: 34 },
    title: { x: 70, y: 34, height: 27 },
    settings: { x: 926, y: 22, width: 105, height: 34 },
    share: { x: 1043, y: 22, width: 73, height: 34 },
    more: { x: 1128, y: 23, width: 32, height: 32 },
    chipHeight: 26,
  },
  tabs: {
    kanban: { x: 108, y: 73, width: 98, height: 36 },
    add: { x: 432, y: 73, width: 40, height: 36 },
    underline: 109,
  },
  bar: {
    search: { x: 24, y: 119, width: 200, height: 32 },
    filter: { x: 230, y: 119, width: 67, height: 32 },
    group: { y: 119, height: 32 },
    primary: { x: 1010, y: 119, width: 150, height: 32 },
  },
  lane: {
    first: { x: 24, y: 165, width: 264 },
    pitch: 276,
    padding: 8,
    gap: 8,
    radius: 14,
    header: { x: 32, y: 173, height: 30 },
    count: { height: 22, minWidth: 22 },
    menu: { width: 28, height: 28 },
  },
  card: {
    x: 32,
    y: 211,
    width: 248,
    padding: 12,
    gap: 9,
    radius: 11,
    tag: { x: 44, y: 223, height: 22, radius: 6 },
    track: { x: 44, y: 307, width: 195, height: 5, radius: 3 },
    due: { x: 44, y: 327, height: 22, radius: 6 },
    assignee: { x: 246, y: 327, size: 22 },
  },
  seeMore: { width: 248, height: 30, radius: 8 },
  add: { width: 248, height: 34, radius: 9 },
  banner: { x: 24, y: 73, height: 37, radius: 9 },
  noResults: { x: 24, y: 161, height: 54, radius: 10 },
  /** Final-Board detail=side, in viewport coordinates (the sheet hugs the right edge). */
  sheet: {
    width: 561,
    headerHeight: 49,
    titleY: 67,
    titleWidth: 516,
    status: { x: 902, y: 116, width: 120, height: 30 },
    composerHeight: 67,
  },
} as const;

const TYPE = {
  boardTitle: { size: "21px", weight: "650" },
  crumb: { size: "12.5px" },
  tab: { size: "13.5px", activeWeight: "600" },
  control: { size: "13px" },
  laneTitle: { size: "13.5px", weight: "600" },
  count: { size: "12px", weight: "600" },
  cardTitle: { size: "13.5px", weight: "500" },
  tag: { size: "11.5px", weight: "600" },
  key: { size: "12px" },
  sheetTitle: { size: "20px", weight: "650" },
  sheetSection: { size: "13px", weight: "600" },
} as const;

async function openBoard(page: Page, query = "") {
  await page.clock.setFixedTime(NOW);
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "vi");
    } catch {
      // Private mode: the default locale applies.
    }
  });
  await page.goto(`${BOARD}${query}`);
  await expect(page.getByRole("region", { name: "Cần làm" })).toBeVisible();
}

async function main(page: Page): Promise<Rect> {
  const box = await page.locator("main").boundingBox();
  if (!box) throw new Error("main area has no box");
  return box;
}

/** A locator's box relative to the main area's top-left. */
async function rel(page: Page, locator: Locator): Promise<Rect> {
  const [origin, box] = await Promise.all([main(page), locator.boundingBox()]);
  if (!box) throw new Error("element has no box");
  return {
    x: box.x - origin.x,
    y: box.y - origin.y,
    width: box.width,
    height: box.height,
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

function lane(page: Page, name: string) {
  return page.getByRole("region", { name, exact: true });
}

test.describe("Board Kanban geometry at 1440 px (owner)", () => {
  test.skip(
    persona !== "owner" || scenario !== "default" || faults !== "",
    "Owner checks need the default owner world without faults",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  test("board header: mark, title, buttons and more menu", async ({ page }) => {
    await openBoard(page);
    const h = MOCK.header;
    const heading = page.getByRole("heading", { level: 1 });
    expectRect(await rel(page, heading), h.title, "h1");
    expect(await style(heading, "font-size")).toBe(TYPE.boardTitle.size);
    expect(await style(heading, "font-weight")).toBe(TYPE.boardTitle.weight);

    const mark = heading.locator("xpath=../preceding-sibling::span[1]");
    expectRect(await rel(page, mark), h.spaceMark, "space mark");
    expect(await style(mark, "border-radius")).toBe("9px");

    const crumb = heading.locator("xpath=../*[1]");
    expect(await style(crumb, "font-size")).toBe(TYPE.crumb.size);

    const settings = page.getByRole("button", {
      name: "Thiết lập",
      exact: true,
    });
    expectRect(await rel(page, settings), h.settings, "Thiết lập");
    expect(await style(settings, "font-size")).toBe(TYPE.control.size);
    expect(await style(settings, "border-radius")).toBe("7px");
    expectRect(
      await rel(
        page,
        page.getByRole("button", { name: "Chia sẻ", exact: true }),
      ),
      h.share,
      "Chia sẻ",
    );
    expectRect(
      await rel(page, page.getByRole("button", { name: "Thêm tuỳ chọn" })),
      h.more,
      "more options",
    );

    const chip = page.getByRole("button", { name: /· có 1 khách$/ });
    expect((await rel(page, chip)).height).toBeCloseTo(h.chipHeight, 0);
    expect(await style(chip, "border-radius")).toBe("6px");
  });

  test("view tabs: row, tab size, type and the underline", async ({ page }) => {
    await openBoard(page);
    const kanban = page.getByRole("tab", { name: "Kanban" });
    expectRect(await rel(page, kanban), MOCK.tabs.kanban, "Kanban tab");
    expect(await style(kanban, "font-size")).toBe(TYPE.tab.size);
    expect(await style(kanban, "font-weight")).toBe(TYPE.tab.activeWeight);
    expect(await style(kanban, "padding-left")).toBe("12px");
    expect(await style(kanban, "column-gap")).toBe("7px");
    expect(await style(kanban, "border-bottom-width")).toBe("2px");

    const other = page.getByRole("tab", { name: "Bảng" });
    expect(await style(other, "font-size")).toBe(TYPE.tab.size);
    expect(await style(other, "font-weight")).toBe("500");

    expectRect(
      await rel(page, page.getByRole("button", { name: "Thêm chế độ xem" })),
      MOCK.tabs.add,
      "add view",
    );
    const box = await rel(page, kanban);
    expect(
      Math.abs(box.y + box.height - MOCK.tabs.underline),
      "the underline sits on the tab row's bottom edge",
    ).toBeLessThanOrEqual(1);
  });

  test("toolbar: search, filter, group-by and the primary action", async ({
    page,
  }) => {
    await openBoard(page);
    const b = MOCK.bar;
    const search = page.getByRole("searchbox");
    const label = search.locator("xpath=..");
    expectRect(await rel(page, label), b.search, "search field");
    expect(await style(label, "border-radius")).toBe("7px");
    expect(await style(search, "font-size")).toBe(TYPE.control.size);

    const filter = page.getByRole("button", { name: "Lọc", exact: true });
    expectRect(await rel(page, filter), b.filter, "Lọc");
    expect(await style(filter, "font-size")).toBe(TYPE.control.size);

    const group = page.getByRole("button", { name: /^Nhóm theo/ });
    expectRect(await rel(page, group), b.group, "Nhóm theo");
    expect(await style(group, "font-size")).toBe(TYPE.control.size);
    expect(
      await style(group.locator("b"), "font-weight"),
      "the grouped field is set in semibold",
    ).toBe("600");

    const primary = page.getByRole("button", { name: "Thêm công việc" });
    expectRect(await rel(page, primary), b.primary, "Thêm công việc");
    expect(await style(primary, "font-size")).toBe(TYPE.control.size);
    expect(await style(primary, "font-weight")).toBe("600");
    expect(await style(primary, "border-radius")).toBe("7px");
  });

  test("lanes: width, gap, padding, header, count and menu", async ({
    page,
  }) => {
    await openBoard(page);
    const l = MOCK.lane;
    const todo = lane(page, "Cần làm");
    const doing = lane(page, "Đang làm");
    const box = await rel(page, todo);
    expectRect(box, { ...l.first }, "first lane");
    expectRect(
      await rel(page, doing),
      { x: l.first.x + l.pitch },
      "second lane",
    );
    expect(await style(todo, "padding-top")).toBe(`${l.padding}px`);
    expect(await style(todo, "padding-left")).toBe(`${l.padding}px`);
    expect(await style(todo, "row-gap")).toBe(`${l.gap}px`);
    expect(await style(todo, "border-radius")).toBe(`${l.radius}px`);

    const title = todo.getByRole("heading", { level: 2 });
    expect(await style(title, "font-size")).toBe(TYPE.laneTitle.size);
    expect(await style(title, "font-weight")).toBe(TYPE.laneTitle.weight);
    const head = title.locator("xpath=..");
    expectRect(await rel(page, head), l.header, "lane header");
    expect(await style(head, "padding-left")).toBe("6px");
    expect(await style(head, "column-gap")).toBe("8px");

    const count = head.locator("span", { hasText: /^6$/ });
    const countBox = await rel(page, count);
    expect(countBox.height).toBeCloseTo(l.count.height, 0);
    expect(countBox.width).toBeGreaterThanOrEqual(l.count.minWidth - 1);
    expect(await style(count, "font-size")).toBe(TYPE.count.size);
    expect(await style(count, "font-weight")).toBe(TYPE.count.weight);
    expect(await style(count, "border-radius")).toBe("11px");

    const menu = todo.getByRole("button", { name: "Tuỳ chọn cột Cần làm" });
    const menuBox = await rel(page, menu);
    expectRect(menuBox, l.menu, "lane menu");
    expect(await style(menu, "border-radius")).toBe("7px");

    // The second lane starts one pitch after the first: lane width + 12 px.
    const second = await rel(page, doing);
    expect(second.x - box.x - box.width).toBeCloseTo(12, 0);
  });

  test("card: padding, gap, radius and every inner box", async ({ page }) => {
    await openBoard(page);
    const c = MOCK.card;
    const card = lane(page, "Cần làm").locator("article").first();
    const box = await rel(page, card);
    expectRect(box, { x: c.x, y: c.y, width: c.width }, "card");
    expect(box.height).toBeGreaterThanOrEqual(148);
    expect(box.height).toBeLessThanOrEqual(151);
    expect(await style(card, "padding-top")).toBe(`${c.padding}px`);
    expect(await style(card, "padding-left")).toBe(`${c.padding}px`);
    expect(await style(card, "row-gap")).toBe(`${c.gap}px`);
    expect(await style(card, "border-radius")).toBe(`${c.radius}px`);
    expect(await colour(card, "background-color")).toBe("rgb(255, 255, 255)");

    const tag = card.locator("[data-label-tone]");
    expectRect(await rel(page, tag), c.tag, "tag");
    expect(await style(tag, "font-size")).toBe(TYPE.tag.size);
    expect(await style(tag, "font-weight")).toBe(TYPE.tag.weight);
    expect(await style(tag, "border-radius")).toBe(`${c.tag.radius}px`);
    expect(await style(tag, "padding-left")).toBe("8px");

    const key = card.getByText("ERP-151", { exact: true });
    expect(await style(key, "font-size")).toBe(TYPE.key.size);
    expect(await style(key, "font-family")).toContain("IBM Plex Mono");
    const keyBox = await rel(page, key);
    expect(
      Math.abs(keyBox.x + keyBox.width - (c.x + c.width - c.padding)),
      "the key is right-aligned in the card",
    ).toBeLessThanOrEqual(1);

    const title = card.getByRole("heading", { level: 3 });
    const titleBox = await rel(page, title);
    expect(titleBox.x).toBeCloseTo(c.x + c.padding, 0);
    expect(titleBox.height).toBeGreaterThanOrEqual(37);
    expect(titleBox.height).toBeLessThanOrEqual(39);
    expect(await style(title, "font-size")).toBe(TYPE.cardTitle.size);
    expect(await style(title, "font-weight")).toBe(TYPE.cardTitle.weight);

    const track = card.locator("[data-child-progress] > span").first();
    expectRect(await rel(page, track), c.track, "progress track");
    expect(await style(track, "border-radius")).toBe(`${c.track.radius}px`);

    const due = card.locator("[data-due]");
    expectRect(await rel(page, due), c.due, "date chip");
    expect(await style(due, "border-radius")).toBe(`${c.due.radius}px`);
    expect(await style(due, "padding-left")).toBe("6px");

    const priority = card.locator("[data-priority]");
    expect(await style(priority, "font-size")).toBe("12px");
    expect(await style(priority, "font-weight")).toBe("600");
    expect((await rel(page, priority.locator("svg"))).width).toBeCloseTo(13, 0);

    const assignee = card.getByRole("group").first();
    const avatar = assignee.locator("span").first();
    expectRect(
      await rel(page, avatar),
      {
        x: c.assignee.x,
        y: c.assignee.y,
        width: c.assignee.size,
        height: c.assignee.size,
      },
      "assignee",
    );
    expect(await style(avatar, "border-radius")).toMatch(
      /^(50%|[\d.]+e\+\d+px|\d{4,}px)$/,
    );
  });

  test("lane footer: Xem thêm and Thêm vào", async ({ page }) => {
    await openBoard(page);
    const todo = lane(page, "Cần làm");
    const cards = todo.locator("article");
    const last = await rel(page, cards.last());
    const more = todo.getByRole("button", { name: /^Xem thêm \d+ việc$/ });
    const moreBox = await rel(page, more);
    expectRect(moreBox, MOCK.seeMore, "Xem thêm");
    expect(
      Math.abs(moreBox.y - (last.y + last.height + 8)),
    ).toBeLessThanOrEqual(1);
    expect(await style(more, "border-radius")).toBe(`${MOCK.seeMore.radius}px`);
    expect(await style(more, "font-size")).toBe("12.5px");
    expect(await style(more, "font-weight")).toBe("500");

    const add = todo.getByRole("button", { name: "Thêm vào Cần làm" });
    const addBox = await rel(page, add);
    expectRect(addBox, MOCK.add, "Thêm vào");
    expect(
      Math.abs(addBox.y - (moreBox.y + moreBox.height + 8)),
    ).toBeLessThanOrEqual(1);
    expect(await style(add, "border-radius")).toBe(`${MOCK.add.radius}px`);
    expect(await style(add, "font-size")).toBe(TYPE.control.size);
  });

  test("the lanes sit 24 px from the main area's sides, with no extra inner padding", async ({
    page,
  }) => {
    await openBoard(page);
    const origin = await main(page);
    const first = await rel(page, lane(page, "Cần làm"));
    expect(first.x).toBeCloseTo(24, 0);
    const primary = await rel(
      page,
      page.getByRole("button", { name: "Thêm công việc" }),
    );
    expect(
      Math.abs(origin.width - 24 - (primary.x + primary.width)),
    ).toBeLessThanOrEqual(1);
    const board = page.locator("[data-kanban-board]");
    const boardBox = await rel(page, board);
    expect(boardBox.x).toBeCloseTo(0, 0);
    expect(boardBox.width).toBeCloseTo(origin.width, 0);
    // The lane area starts 4 px below the toolbar row.
    const toolbar = await rel(page, page.locator("[data-kanban-toolbar]"));
    expect(
      Math.abs(first.y - (toolbar.y + toolbar.height + 4)),
    ).toBeLessThanOrEqual(1);
  });

  test("no results: the bar and the empty lanes", async ({ page }) => {
    await openBoard(page);
    await page.getByRole("searchbox").fill("zzzz");
    const bar = page.locator("[data-kanban-toolbar] [role=status]");
    await expect(bar).toBeVisible();
    const n = MOCK.noResults;
    const box = await rel(page, bar);
    expectRect(box, { x: n.x, y: n.y, height: n.height }, "no-results bar");
    expect(await style(bar, "border-radius")).toBe(`${n.radius}px`);
    expect(await style(bar, "padding-left")).toBe("12px");
    const todo = lane(page, "Cần làm");
    // Header 30 + gap 8 + dashed box 48 + 2 x 8 padding = 102.
    expect((await rel(page, todo)).height).toBeCloseTo(102, 0);
  });

  test("item sheet: full height at the right edge, header, title, pills and composer", async ({
    page,
  }) => {
    await openBoard(page, "?item=mock-erp-item-142");
    const sheet = page.getByRole("dialog", { name: ERP_142 });
    await expect(sheet).toBeVisible();
    const s = MOCK.sheet;
    const box = (await sheet.boundingBox())!;
    const frame = (await sheet.locator("xpath=..").boundingBox())!;
    expect(Math.abs(frame.x + frame.width - 1440)).toBeLessThanOrEqual(1);
    expect(Math.abs(frame.width - s.width)).toBeLessThanOrEqual(1);
    expect(frame.y).toBeCloseTo(0, 0);
    expect(Math.abs(frame.height - 900)).toBeLessThanOrEqual(1);
    expect(box.x).toBeGreaterThanOrEqual(frame.x);

    const header = sheet.locator("header");
    expect(
      Math.abs((await header.boundingBox())!.height - s.headerHeight),
    ).toBeLessThanOrEqual(1);
    expect(await style(header, "padding-left")).toBe("18px");
    expect(await style(header, "column-gap")).toBe("6px");
    const icon = sheet.getByRole("button", { name: /^Đóng/ });
    expect((await icon.boundingBox())!.width).toBeCloseTo(32, 0);
    expect(await style(icon, "border-radius")).toBe("7px");

    const title = sheet.getByRole("heading", { level: 2 });
    const titleBox = (await title.boundingBox())!;
    expect(Math.abs(titleBox.y - s.titleY)).toBeLessThanOrEqual(1);
    expect(Math.abs(titleBox.width - s.titleWidth)).toBeLessThanOrEqual(1);
    expect(await style(title, "font-size")).toBe(TYPE.sheetTitle.size);
    expect(await style(title, "font-weight")).toBe(TYPE.sheetTitle.weight);

    const status = sheet.getByRole("button", { name: /^Trạng thái:/ });
    const statusBox = (await status.boundingBox())!;
    expect(Math.abs(statusBox.x - s.status.x)).toBeLessThanOrEqual(1);
    expect(Math.abs(statusBox.y - s.status.y)).toBeLessThanOrEqual(1);
    expect(Math.abs(statusBox.height - s.status.height)).toBeLessThanOrEqual(1);
    expect(Math.abs(statusBox.width - s.status.width)).toBeLessThanOrEqual(2);
    expect(await style(status, "border-radius")).toBe("8px");
    expect(await style(status, "font-size")).toBe(TYPE.control.size);
    expect(await style(status, "padding-left")).toBe("10px");
    expect(await style(status, "column-gap")).toBe("7px");

    const section = sheet.getByRole("heading", { name: "Mô tả", level: 3 });
    expect(await style(section, "font-size")).toBe(TYPE.sheetSection.size);
    expect(await style(section, "font-weight")).toBe(TYPE.sheetSection.weight);

    const composer = sheet.locator(":scope > div").last();
    const composerBox = (await composer.boundingBox())!;
    expect(Math.abs(composerBox.height - s.composerHeight)).toBeLessThanOrEqual(
      1,
    );
    expect(
      Math.abs(composerBox.y + composerBox.height - 900),
    ).toBeLessThanOrEqual(1);
    expect(await style(composer, "padding-left")).toBe("18px");
  });
});

test.describe("Board Kanban geometry at 1440 px (viewer)", () => {
  test.skip(
    persona !== "viewer" || scenario !== "default",
    "Needs the server started with VITE_MOCK_PERSONA=viewer",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  test("the read-only banner: box, radius and the lane area below it", async ({
    page,
  }) => {
    await openBoard(page);
    const banner = page.getByRole("note");
    const box = await rel(page, banner);
    expectRect(box, { x: MOCK.banner.x, height: MOCK.banner.height }, "banner");
    expect(await style(banner, "border-radius")).toBe(
      `${MOCK.banner.radius}px`,
    );
    expect(await style(banner, "padding-left")).toBe("12px");
    expect(await style(banner, "font-size")).toBe(TYPE.control.size);
    expect(await style(banner, "margin-top")).toBe("12px");
    // The viewer sees no add action in the toolbar or in a lane.
    await expect(
      page.getByRole("button", { name: "Thêm công việc" }),
    ).toHaveCount(0);
    await expect(page.getByRole("button", { name: /^Thêm vào / })).toHaveCount(
      0,
    );
  });
});
