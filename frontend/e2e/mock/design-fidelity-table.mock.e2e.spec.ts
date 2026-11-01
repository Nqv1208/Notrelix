import { expect, test, type Locator, type Page } from "@playwright/test";

/**
 * Board table geometry against the October 2026 mockup. Every number comes
 * from `docs/design/mockups/2026-10/src/Final-Table.dc.html` rendered at
 * 1440x900 with `nav=an` and measured with the box dump (position, size,
 * padding, gap, font, radius, colour). Boxes are read relative to the main
 * area's top-left, so the checks hold whatever sits left of the main area
 * (rail, sidebar). Tolerance is 1 px for boxes and exact for font sizes,
 * radii and colours.
 *
 * Text differs between the mock world and the mockup world, so only boxes
 * that do not depend on the wording are compared; the ERP rows share their
 * keys and the Vietnamese labels, so the controls keep their widths.
 *
 * Two known, intended differences are written down where they matter:
 *  - the mockup's `chon-nhieu` state overflows its 900 px frame by 2 px, so
 *    its grid is 700 px high and its bulk bar sits 2 px low; `da-luu-tru` and
 *    the app fit the window (grid 697.6 px in the mockup, 695.6 under the unified 48 px tab strip, bar bottom 22 px above the edge);
 *  - the mock world's people and label colours come from the identity
 *    palette, not the mockup's hand-picked colours.
 *
 * The page clock is fixed to the fixtures' 12/10/2026 10:40. The checks need
 * the default owner world.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const NOW = new Date("2026-10-12T10:40:00+07:00");
const VIEWPORT = { width: 1440, height: 900 };

interface Rect {
  readonly x: number;
  readonly y: number;
  readonly width: number;
  readonly height: number;
}

/** Final-Table state=chon-nhieu density=thoai-mai nav=an. Origin: main area (256,0). */
const MOCK = {
  toolbar: {
    search: { x: 24, y: 117.4, width: 200, height: 32 },
    doneChip: { x: 230, y: 119.4, width: 175.3, height: 28 },
    doneChipClear: { width: 20, height: 20 },
    filter: { x: 411.3, y: 117.4, width: 66.6, height: 32 },
    sort: { x: 483.9, y: 117.4, width: 138.9, height: 32 },
    group: { x: 628.8, y: 117.4, width: 162.2, height: 32 },
    separator: { x: 801, y: 123.4, width: 1, height: 20 },
    columns: { x: 812, y: 117.4, width: 102, height: 32 },
    primary: { x: 1009.8, y: 117.4, width: 150.2, height: 32 },
  },
  /** `da-luu-tru` frames the main area at 900 px: grid 697.6 px high in the mockup, 695.6 with the 48 px strip. */
  grid: { x: 24, y: 159.4, width: 1136, height: 695.6, radius: "12px" },
  head: { y: 160.4, height: 36, background: "rgb(243, 245, 248)" },
  /** Header cells of the nine default columns, left edge and width. */
  headCells: [
    { name: "select", x: 25, width: 40 },
    { name: "Mã", x: 65, width: 84 },
    { name: "Tên công việc", x: 149, width: 340 },
    { name: "Trạng thái", x: 489, width: 140 },
    { name: "Người phụ trách", x: 629, width: 184 },
    { name: "Hạn", x: 813, width: 132 },
    { name: "Ưu tiên", x: 945, width: 96 },
    { name: "Nhãn", x: 1041, width: 120 },
    { name: "Việc con", x: 1161, width: 136 },
    { name: "Cập nhật", x: 1297, width: 120 },
  ],
  group: {
    first: { x: 25, y: 196.4, width: 1392, height: 39 },
    toggle: { x: 35, y: 203.4, width: 24, height: 24, radius: "5px" },
    dot: { x: 67, y: 211.4, size: 8 },
    countWeight: "500",
  },
  row: {
    first: { x: 25, y: 235.4, width: 1392, height: 40 },
    pitch: 40,
    compactPitch: 32,
    checkbox: { x: 39, y: 247.4, size: 15, radius: "4px" },
    key: { x: 75, width: 50.4 },
    title: { x: 159, width: 319 },
    pill: { x: 499, y: 242.9, width: 83.1, height: 24, radius: "999px" },
    avatar: { x: 639, y: 242.9, size: 24 },
    priority: { x: 955 },
    tag: { x: 1051, y: 243.9, height: 22, radius: "6px" },
    track: { x: 1171, y: 252.4, width: 80, height: 5, radius: "3px" },
    updated: { x: 1307 },
  },
  add: { y: 475.4, height: 35, iconX: 79, iconSize: 14 },
  /** state=sua-o: the status list under ERP-144's pill. */
  list: {
    box: { x: 495, y: 623.4, width: 234, height: 176, radius: "10px" },
    input: { x: 502, y: 630.4, width: 220, height: 30 },
    option: { x: 502, y: 664.4, width: 220, height: 32 },
    key: { width: 18.6, height: 20 },
  },
  /** state=chon-nhieu: the bar's boxes (x from main's left edge). */
  bulk: {
    width: 681.5,
    height: 44,
    radius: "12px",
    gapFromBottom: 22,
    status: { width: 99.9, height: 32, radius: "8px" },
    move: { width: 132, height: 32 },
    archive: { width: 86.8, height: 32 },
    close: { width: 36, height: 32 },
  },
  /** state=da-luu-tru. */
  notice: {
    x: 460.4,
    y: 834,
    width: 263.2,
    height: 44,
    undo: { width: 78.7, height: 32 },
  },
  /** state=khong-ket-qua. */
  noResults: {
    x: 25,
    width: 1100,
    title: { size: "15px", weight: "600" },
    button: { width: 66.4, height: 32, radius: "7px" },
  },
  footer: { x: 24, text: { height: 18.1 }, key: { height: 16, radius: "4px" } },
} as const;

const TYPE = {
  head: { size: "12px", weight: "600" },
  group: { size: "13.5px", weight: "600" },
  cell: { size: "13.5px" },
  title: { size: "13.5px", weight: "500" },
  key: { size: "12px" },
  pill: { size: "12.5px", weight: "600" },
  avatar: { size: "9.5px", weight: "700" },
  priority: { size: "12.5px", weight: "600" },
  tag: { size: "11.5px", weight: "600" },
  updated: { size: "12.5px" },
  control: { size: "13px" },
  chip: { size: "12.5px", weight: "600" },
  bulk: { size: "13px", weight: "500", countSize: "14px" },
} as const;

const COLOUR = {
  ink: "rgb(22, 26, 33)",
  muted: "rgb(79, 88, 102)",
  meta: "rgb(95, 104, 117)",
  accent: "rgb(22, 102, 204)",
  selectedRow: "rgb(238, 244, 253)",
  danger: "rgb(196, 35, 64)",
  lineStrong: "rgb(210, 215, 222)",
  bulk: "rgb(22, 26, 33)",
} as const;

async function openTable(
  page: Page,
  query = "",
  ready: "rows" | "grid" = "rows",
) {
  await page.clock.setFixedTime(NOW);
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "vi");
    } catch {
      // Private mode: the default locale applies.
    }
  });
  await page.goto(`${BOARD}?view=table${query}`);
  await expect(
    ready === "rows"
      ? page.getByRole("rowgroup").first()
      : page.getByRole("table"),
  ).toBeVisible();
}

async function main(page: Page): Promise<Rect> {
  const box = await page.locator("main").boundingBox();
  if (!box) throw new Error("main area has no box");
  return box;
}

/**
 * The tab strip is 48 px on every view so switching views moves nothing: that
 * is Final-Board's height, while Final-Table, Final-Calendar and Final-Timeline
 * draw it 46 px high. Boxes under the strip (from the mockup's content start,
 * y = 107) therefore sit 2 px lower than in this mockup; they are shifted back
 * before they are compared with the mockup numbers.
 */
const STRIP_EXTRA = 2;
const CONTENT_START = 107;

/**
 * A locator's box relative to the main area's top-left. A box anchored to the
 * window's bottom edge (a toast, the bulk bar) does not move with the strip:
 * pass `{ fixedToBottom: true }` to keep its raw position.
 */
async function rel(
  page: Page,
  locator: Locator,
  { fixedToBottom = false }: { fixedToBottom?: boolean } = {},
): Promise<Rect> {
  const [origin, box] = await Promise.all([main(page), locator.boundingBox()]);
  if (!box) throw new Error("element has no box");
  const y = box.y - origin.y;
  return {
    x: box.x - origin.x,
    y:
      !fixedToBottom && y >= CONTENT_START + STRIP_EXTRA - 1
        ? y - STRIP_EXTRA
        : y,
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

function row(page: Page, key: string) {
  return page.locator("[data-row-id]").filter({ hasText: key }).first();
}

function cells(rowLocator: Locator) {
  return rowLocator.getByRole("cell");
}

test.describe("Board table geometry at 1440 px (owner)", () => {
  test.skip(
    persona !== "owner" || scenario !== "default" || faults !== "",
    "Owner checks need the default owner world without faults",
  );
  test.use({ viewport: VIEWPORT });

  test("toolbar: search, done chip, Lọc, Sắp xếp, Nhóm theo, Cột and the primary action", async ({
    page,
  }) => {
    await openTable(page);
    const t = MOCK.toolbar;
    const search = page.getByRole("searchbox", { name: "Tìm trong bảng" });
    const field = search.locator("xpath=..");
    expectRect(await rel(page, field), t.search, "search field");
    expect(await style(field, "border-radius")).toBe("7px");
    expect(await style(search, "font-size")).toBe(TYPE.control.size);

    const chip = page.locator('[data-chip="done"]');
    expectRect(await rel(page, chip), t.doneChip, "done chip");
    expect(await style(chip, "font-size")).toBe(TYPE.chip.size);
    expect(await style(chip, "font-weight")).toBe(TYPE.chip.weight);
    expect(await style(chip, "border-radius")).toBe("7px");
    expect(await colour(chip, "background-color")).toBe("rgb(231, 240, 252)");
    expect(await colour(chip, "color")).toBe("rgb(15, 86, 174)");
    const clear = chip.getByRole("button");
    expectRect(await rel(page, clear), t.doneChipClear, "chip clear");
    expect(await style(clear, "border-radius")).toBe("5px");

    // The filter, sort, group and saved-filter controls are the member's view bar shared with the
    // Kanban toolbar (WM-View-Bar): its labels carry a colon ("Sắp xếp: Hạn ↑") and a width that
    // follows the wording, so a width is not compared; the left edge is compared where only
    // wording-independent boxes sit to its left, and every later box is read as a 6 px gap after
    // the one before it.
    const controls: Locator[] = [];
    for (const [name, want] of [
      [/^Lọc$/, t.filter],
      [/^Sắp xếp: Hạn ↑$/, { x: t.sort.x, y: t.sort.y, height: t.sort.height }],
      [/^Nhóm: Trạng thái$/, { y: t.group.y, height: t.group.height }],
      [/^Bộ lọc đã lưu$/, { y: t.group.y, height: t.group.height }],
      // The board carries the field columns (WM-Table-Columns): the count is the live one.
      [/^Cột \d+\/\d+$/, { y: t.columns.y, height: t.columns.height }],
    ] as const) {
      const button = page.getByRole("button", { name });
      expectRect(await rel(page, button), want, String(name));
      controls.push(button);
      if (name.source.startsWith("^Bộ lọc")) {
        // The saved filters sit in the slack the artboard leaves before the separator: an icon button.
        expect((await rel(page, button)).width).toBeLessThanOrEqual(40);
        continue;
      }
      expect(await style(button, "font-size")).toBe(TYPE.control.size);
      expect(await style(button, "font-weight")).toBe("400");
      expect(await style(button, "border-radius")).toBe("7px");
      expect(await colour(button, "color")).toBe(COLOUR.muted);
    }
    const [, sort, group, saved] = controls as [
      Locator,
      Locator,
      Locator,
      Locator,
      Locator,
    ];
    for (const [before, after, label] of [
      [sort, group, "group after sort"],
      [group, saved, "saved filters after group"],
    ] as const) {
      const a = await rel(page, before);
      const b = await rel(page, after);
      expect(Math.abs(b.x - (a.x + a.width + 6)), label).toBeLessThanOrEqual(1);
    }

    const separator = page.locator("[data-table-toolbar] > span.w-px");
    const sep = await rel(page, separator);
    expectRect(
      sep,
      {
        y: t.separator.y,
        width: t.separator.width,
        height: t.separator.height,
      },
      "separator",
    );
    expect(await colour(separator, "background-color")).toBe(
      "rgb(230, 233, 238)",
    );
    const columns = await rel(page, controls[4]!);
    expect(
      Math.abs(columns.x - (sep.x + sep.width + 10)),
      "columns after the separator",
    ).toBeLessThanOrEqual(1);

    const add = page.getByRole("button", { name: "Thêm công việc" });
    expectRect(await rel(page, add), t.primary, "Thêm công việc");
    expect(await style(add, "font-weight")).toBe("600");
    expect(await colour(add, "background-color")).toBe(COLOUR.accent);
    expect(await style(add, "border-radius")).toBe("7px");
  });

  test("grid: frame, sticky header row and the nine columns", async ({
    page,
  }) => {
    await openTable(page);
    const grid = page.locator("[data-table-scroll]");
    expectRect(await rel(page, grid), MOCK.grid, "grid frame");
    expect(await style(grid, "border-radius")).toBe(MOCK.grid.radius);
    expect(await colour(grid, "border-top-color")).toBe("rgb(230, 233, 238)");

    const heads = page.getByRole("columnheader");
    // The board shows its field columns after the nine drawn ones (WM-Table-Columns).
    expect(await heads.count()).toBeGreaterThanOrEqual(MOCK.headCells.length);
    for (const [index, want] of MOCK.headCells.entries()) {
      const head = heads.nth(index);
      const box = await rel(page, head);
      expectRect(
        box,
        { x: want.x, width: want.width, y: MOCK.head.y, height: 36 },
        `header ${want.name}`,
      );
      expect(await style(head, "font-size")).toBe(TYPE.head.size);
      expect(await style(head, "font-weight")).toBe(TYPE.head.weight);
      expect(await colour(head, "background-color")).toBe(MOCK.head.background);
    }
    // The sorted column reads in the ink colour, the others in the meta colour.
    expect(await colour(heads.nth(5), "color")).toBe(COLOUR.ink);
    expect(await colour(heads.nth(4), "color")).toBe(COLOUR.meta);
    await expect(heads.nth(5)).toHaveAttribute("aria-sort", "ascending");
  });

  test("group header: toggle, dot, count, warning and the add row", async ({
    page,
  }) => {
    await openTable(page);
    const g = MOCK.group;
    const first = page.getByRole("rowgroup", { name: "Cần làm" });
    const header = first.getByRole("row").first();
    expectRect(
      await rel(page, header),
      { x: g.first.x, y: g.first.y, height: g.first.height },
      "group header",
    );
    expect(await colour(header, "background-color")).toBe(MOCK.head.background);

    const toggle = first.getByRole("button", { name: "Thu gọn nhóm Cần làm" });
    expectRect(await rel(page, toggle), g.toggle, "group toggle");
    expect(await style(toggle, "border-radius")).toBe(g.toggle.radius);
    const name = header.getByRole("cell");
    expect(await style(name, "font-size")).toBe(TYPE.group.size);
    expect(await style(name, "font-weight")).toBe(TYPE.group.weight);
    const dot = header.locator("span[aria-hidden]").first();
    expectRect(
      await rel(page, dot),
      { x: g.dot.x, y: g.dot.y, width: g.dot.size, height: g.dot.size },
      "status dot",
    );
    const count = header.getByText("6", { exact: true });
    expect(await style(count, "font-weight")).toBe(g.countWeight);
    expect(await colour(count, "color")).toBe(COLOUR.meta);

    const warn = page
      .getByRole("rowgroup", { name: "Đang làm" })
      .getByText(/· 1 quá hạn/);
    expect(await style(warn, "font-size")).toBe("12.5px");
    expect(await style(warn, "font-weight")).toBe("600");
    expect(await colour(warn, "color")).toBe(COLOUR.danger);

    const add = first.getByRole("button", { name: "Thêm vào Cần làm" });
    const addRow = add.locator("xpath=ancestor::*[@role='row'][1]");
    expectRect(
      await rel(page, addRow),
      // The add row spans every column, like a row: its width is not compared.
      { x: 25, y: MOCK.add.y, height: MOCK.add.height },
      "add row",
    );
    expect(await style(add, "font-size")).toBe(TYPE.control.size);
    expect(await colour(add, "color")).toBe(COLOUR.meta);
    const icon = add.locator("svg");
    expectRect(
      await rel(page, icon),
      { x: MOCK.add.iconX, width: 14, height: 14 },
      "add icon",
    );
  });

  test("row: height, checkbox, key, title, status pill, person, due, priority, label, progress, updated", async ({
    page,
  }) => {
    await openTable(page);
    const r = MOCK.row;
    const target = row(page, "ERP-151");
    // A row spans every column; the board now shows its field columns after the nine drawn ones, so
    // the artboard's 1392 px is the width of the first nine only and is not compared here.
    expectRect(
      await rel(page, target),
      { x: r.first.x, y: r.first.y, height: r.first.height },
      "first row",
    );
    const columns = cells(target);

    const box = columns.nth(0).getByRole("checkbox");
    expectRect(
      await rel(page, box),
      { x: r.checkbox.x, y: r.checkbox.y, width: 15, height: 15 },
      "checkbox",
    );
    expect(await style(box, "border-radius")).toBe(r.checkbox.radius);
    // The mockup's 1.5 px border computes to 1 px at a device pixel ratio of 1.
    expect(await style(box, "border-top-width")).toBe("1px");
    expect(await colour(box, "border-top-color")).toBe(COLOUR.lineStrong);

    const key = columns.nth(1).getByText("ERP-151");
    expect((await rel(page, key)).x).toBeCloseTo(r.key.x, 0);
    expect(await style(key, "font-size")).toBe(TYPE.key.size);
    expect(await style(key, "font-family")).toContain("IBM Plex Mono");
    expect(await colour(key, "color")).toBe(COLOUR.meta);

    // ERP-151 has sub-items, so its first button is now the expand chevron
    // (card-face table); the title is the row's open button.
    const chevron = columns.nth(2).getByRole("button").first();
    expect((await rel(page, chevron)).x).toBeCloseTo(r.title.x, 0);
    const title = columns.nth(2).locator("[data-row-open]");
    expect(await style(title, "font-size")).toBe(TYPE.title.size);
    expect(await style(title, "font-weight")).toBe(TYPE.title.weight);
    expect(await colour(title, "color")).toBe(COLOUR.ink);
    // No docking rule: the title cell has no right border (the columns are
    // not docked, they scroll together).
    expect(await style(columns.nth(2), "border-right-width")).toBe("0px");

    const pill = columns.nth(3).getByRole("button");
    expectRect(await rel(page, pill), r.pill, "status pill");
    expect(await style(pill, "font-size")).toBe(TYPE.pill.size);
    expect(await style(pill, "font-weight")).toBe(TYPE.pill.weight);
    expect(await style(pill, "border-radius")).not.toBe("0px");
    expect(await colour(pill, "background-color")).toBe("rgb(236, 238, 242)");
    const doing = row(page, "ERP-142")
      .getByRole("cell")
      .nth(3)
      .getByRole("button");
    expect(await colour(doing, "background-color")).toBe("rgb(226, 236, 250)");
    const review = row(page, "ERP-138")
      .getByRole("cell")
      .nth(3)
      .getByRole("button");
    expect(await colour(review, "background-color")).toBe("rgb(251, 240, 214)");
    expect(
      await colour(review.locator("span[aria-hidden]"), "background-color"),
    ).toBe("rgb(217, 154, 0)");

    const avatar = columns.nth(4).locator("span[aria-hidden]").first();
    expectRect(
      await rel(page, avatar),
      { x: r.avatar.x, y: r.avatar.y, width: 24, height: 24 },
      "avatar",
    );
    expect(await style(avatar, "font-size")).toBe(TYPE.avatar.size);
    expect(await style(avatar, "font-weight")).toBe(TYPE.avatar.weight);
    expect(await style(avatar, "border-radius")).not.toBe("0px");

    const due = columns.nth(5).getByText("18/10");
    expect(await style(due, "font-size")).toBe(TYPE.cell.size);
    expect(await colour(due, "color")).toBe(COLOUR.muted);
    const overdue = row(page, "ERP-142")
      .getByRole("cell")
      .nth(5)
      .getByText(/Quá hạn 2 ngày/);
    expect(await style(overdue, "font-weight")).toBe("600");
    expect(await colour(overdue, "color")).toBe(COLOUR.danger);
    const unset = row(page, "ERP-153")
      .getByRole("cell")
      .nth(5)
      .getByText("Chưa đặt");
    expect(await style(unset, "font-style")).toBe("italic");
    expect(await colour(unset, "color")).toBe(COLOUR.meta);

    const priority = columns.nth(6).getByText("Cao");
    expect((await rel(page, priority)).x).toBeCloseTo(r.priority.x, 0);
    expect(await style(priority, "font-size")).toBe(TYPE.priority.size);
    expect(await style(priority, "font-weight")).toBe(TYPE.priority.weight);
    expect(await colour(priority, "color")).toBe("rgb(180, 80, 11)");
    const urgent = row(page, "ERP-142")
      .getByRole("cell")
      .nth(6)
      .getByText("Khẩn");
    expect(await colour(urgent, "color")).toBe(COLOUR.danger);
    // WM-Card-Face: every priority level is drawn, so a medium item reads
    // "Trung bình" (500, muted) where Final-Table drew a dash.
    const none = row(page, "ERP-147")
      .getByRole("cell")
      .nth(6)
      .getByText("Trung bình");
    expect(await style(none, "font-weight")).toBe("500");
    expect(await colour(none, "color")).toBe(COLOUR.muted);

    const tag = columns.nth(7).locator("[data-label-tone]");
    const tagBox = await rel(page, tag);
    expectRect(
      tagBox,
      { x: r.tag.x, y: r.tag.y, height: r.tag.height },
      "label",
    );
    expect(await style(tag, "font-size")).toBe(TYPE.tag.size);
    expect(await style(tag, "font-weight")).toBe(TYPE.tag.weight);
    expect(await style(tag, "border-radius")).toBe(r.tag.radius);
    expect(await style(tag, "padding-left")).toBe("8px");

    const progress = columns.nth(8).locator("[data-child-progress]");
    const track = progress.locator("> span").first();
    expectRect(
      await rel(page, track),
      {
        x: r.track.x,
        y: r.track.y,
        width: r.track.width,
        height: r.track.height,
      },
      "progress track",
    );
    expect(await style(track, "border-radius")).toBe(r.track.radius);
    expect(await colour(track, "background-color")).toBe("rgb(230, 233, 238)");
    expect(
      await colour(
        progress.locator("[data-progress-fill]"),
        "background-color",
      ),
    ).toBe(COLOUR.accent);

    const updated = columns.nth(9).locator("span").first();
    expect((await rel(page, updated)).x).toBeCloseTo(r.updated.x, 0);
    expect(await style(updated, "font-size")).toBe(TYPE.updated.size);
    expect(await colour(updated, "color")).toBe(COLOUR.meta);

    // Rows keep the 40 px pitch inside a group.
    const second = await rel(page, row(page, "ERP-147"));
    expect(Math.abs(second.y - (r.first.y + r.pitch))).toBeLessThanOrEqual(1);
  });

  test("hovered row keeps its geometry and shows no Mở button", async ({
    page,
  }) => {
    await openTable(page);
    const target = row(page, "ERP-144");
    const before = await rel(page, target);
    await expect(
      target.getByRole("button", { name: /^Mở chi tiết/ }),
    ).toHaveCount(0);
    await target.hover();
    await expect(
      target.getByRole("button", { name: /^Mở chi tiết/ }),
    ).toHaveCount(0);
    expectRect(await rel(page, target), before, "hovered row");
    // Hover tints the row's cells.
    expect(await colour(cells(target).nth(1), "background-color")).toBe(
      "rgb(241, 243, 246)",
    );
    // The title stays plain text, with no underline.
    const title = target.locator("[data-row-open]");
    expect(await style(title, "text-decoration-line")).toBe("none");
  });

  test("selecting rows: selected tint, mixed header checkbox and the bulk bar", async ({
    page,
  }) => {
    await openTable(page);
    await page.getByRole("checkbox", { name: "Chọn ERP-142" }).click();
    await page.getByRole("checkbox", { name: "Chọn ERP-146" }).click();
    await page.mouse.move(2, 2);

    const header = page.getByRole("checkbox", { name: "Chọn tất cả" });
    await expect(header).toHaveAttribute("aria-checked", "mixed");
    expect(await colour(header, "background-color")).toBe(COLOUR.accent);
    expectRect(
      await rel(page, header),
      { x: 39, y: 170.4, width: 15, height: 15 },
      "header checkbox",
    );
    const selected = row(page, "ERP-142");
    expect(await colour(cells(selected).nth(2), "background-color")).toBe(
      COLOUR.selectedRow,
    );
    const checked = page.getByRole("checkbox", { name: "Chọn ERP-142" });
    expect(await colour(checked, "background-color")).toBe(COLOUR.accent);

    const bar = page.getByRole("toolbar", {
      name: "Thao tác với 2 công việc đã chọn",
    });
    const box = await rel(page, bar, { fixedToBottom: true });
    const b = MOCK.bulk;
    expect(Math.abs(box.height - b.height)).toBeLessThanOrEqual(1);
    // Centred on the main area; its bottom edge 22 px above the window edge.
    const origin = await main(page);
    expect(
      Math.abs(box.x + box.width / 2 - origin.width / 2),
      "bar is centred on the main area",
    ).toBeLessThanOrEqual(1);
    expect(
      Math.abs(VIEWPORT.height - (box.y + box.height) - b.gapFromBottom),
    ).toBeLessThanOrEqual(1);
    // The mockup's bar has Trạng thái, Giao cho, Đặt hạn, Chuyển board and
    // Lưu trữ; Ưu tiên and Xoá are added to it, each a button plus the 4 px gap.
    let added = 0;
    for (const name of ["Ưu tiên", "Xoá"]) {
      const extra = await bar.getByRole("button", { name }).boundingBox();
      added += (extra?.width ?? 0) + 4;
    }
    expect(Math.abs(box.width - added - b.width)).toBeLessThanOrEqual(1);
    expect(await style(bar, "border-radius")).toBe(b.radius);
    expect(await colour(bar, "background-color")).toBe(COLOUR.bulk);
    expect(await colour(bar, "color")).toBe("rgb(242, 244, 247)");
    expect(await style(bar.locator("b"), "font-size")).toBe(
      TYPE.bulk.countSize,
    );
    expect(await style(bar.locator("b"), "font-weight")).toBe("600");

    const statusButton = bar.getByRole("button", { name: "Trạng thái" });
    expectRect(
      await rel(page, statusButton),
      { width: b.status.width, height: 32 },
      "Trạng thái",
    );
    expect(await style(statusButton, "border-radius")).toBe(b.status.radius);
    expect(await style(statusButton, "font-size")).toBe(TYPE.bulk.size);
    expect(await style(statusButton, "font-weight")).toBe(TYPE.bulk.weight);
    expectRect(
      await rel(page, bar.getByRole("button", { name: /Chuyển board/ })),
      b.move,
      "Chuyển board",
    );
    const archive = bar.getByRole("button", { name: "Lưu trữ" });
    expectRect(await rel(page, archive), b.archive, "Lưu trữ");
    expect(await colour(archive, "color")).toBe("rgb(255, 154, 170)");
    expectRect(
      await rel(page, bar.getByRole("button", { name: "Bỏ chọn (Esc)" })),
      b.close,
      "close",
    );
  });

  test("status list opens under the pill at the mockup position", async ({
    page,
  }) => {
    await openTable(page);
    await row(page, "ERP-144")
      .getByRole("button", { name: "Đổi trạng thái của ERP-144" })
      .click();
    const list = page.locator('[data-slot="popover-content"]');
    await expect(list).toBeVisible();
    // The list zooms in; measure once it has settled.
    await page.waitForTimeout(400);
    const l = MOCK.list;
    expectRect(await rel(page, list), l.box, "status list");
    expect(await style(list, "border-radius")).toBe(l.box.radius);
    expect(await colour(list, "border-top-color")).toBe(COLOUR.lineStrong);
    const input = page.getByRole("combobox", { name: "Tìm trạng thái" });
    expectRect(await rel(page, input), l.input, "list input");
    expect(await style(input, "border-radius")).toBe("7px");
    const options = page.getByRole("option");
    expect(await options.count()).toBe(4);
    expectRect(await rel(page, options.nth(0)), l.option, "first option");
    expect(
      Math.abs((await rel(page, options.nth(1))).y - (l.option.y + 32)),
    ).toBeLessThanOrEqual(1);
    expect(await style(options.nth(0), "border-radius")).toBe("7px");
    // The number key chips are gone (owner rule: no visible shortcut hints).
    await expect(options.nth(0).locator("span.border")).toHaveCount(0);
    // The pill carries the focus ring while its list is open.
    const pill = row(page, "ERP-144").getByRole("button", {
      name: "Đổi trạng thái của ERP-144",
    });
    expect(await style(pill, "box-shadow")).toContain("2px");
  });

  test("density gon: 32 px rows, header and groups unchanged", async ({
    page,
  }) => {
    await openTable(page);
    await page.getByRole("button", { name: /^Cột \d+\/\d+$/ }).click();
    await page.getByRole("menuitemradio", { name: "Gọn" }).click();
    await page.keyboard.press("Escape");
    await expect(
      page.locator("[data-slot=main-table-surface]"),
    ).toHaveAttribute("data-density", "compact");
    const r = MOCK.row;
    const first = await rel(page, row(page, "ERP-151"));
    expectRect(first, { y: r.first.y, height: 32 }, "compact row");
    const second = await rel(page, row(page, "ERP-147"));
    expect(
      Math.abs(second.y - (r.first.y + r.compactPitch)),
    ).toBeLessThanOrEqual(1);
    expectRect(
      await rel(page, page.getByRole("columnheader").nth(1)),
      { y: MOCK.head.y, height: 36 },
      "header",
    );
    const group = await rel(
      page,
      page.getByRole("rowgroup", { name: "Cần làm" }).getByRole("row").first(),
    );
    expectRect(group, { height: 39 }, "group header");
    // Six rows of 32 plus the header and the add row (mockup gon: 427.4).
    const add = await rel(
      page,
      page
        .getByRole("button", { name: "Thêm vào Cần làm" })
        .locator("xpath=ancestor::*[@role='row'][1]"),
    );
    expect(Math.abs(add.y - 427.4)).toBeLessThanOrEqual(1);
  });

  test("no results: note, count line and Bỏ lọc", async ({ page }) => {
    // Priority Thấp and overdue match no open item: the mockup's khong-ket-qua.
    await openTable(page, "&filter=p.low~o", "grid");
    const n = MOCK.noResults;
    const note = page.getByRole("status").filter({
      hasText: "Không có việc khớp bộ lọc",
    });
    const box = await rel(page, note);
    expectRect(box, { x: n.x, width: n.width }, "no-results note");
    const title = note.locator("b");
    expect(await style(title, "font-size")).toBe(n.title.size);
    expect(await style(title, "font-weight")).toBe(n.title.weight);
    expect(await colour(title, "color")).toBe(COLOUR.ink);
    const clear = note.getByRole("button", { name: "Bỏ lọc" });
    const clearBox = await rel(page, clear);
    expect(Math.abs(clearBox.width - n.button.width)).toBeLessThanOrEqual(1);
    expect(Math.abs(clearBox.height - n.button.height)).toBeLessThanOrEqual(1);
    expect(await style(clear, "border-radius")).toBe(n.button.radius);
    // The note is centred in the grid (1100 px wide).
    expect(
      Math.abs(clearBox.x + clearBox.width / 2 - (n.x + n.width / 2)),
    ).toBeLessThanOrEqual(1);
    await expect(
      page.getByText("0 công việc đang hiện · 16 ẩn bởi bộ lọc"),
    ).toBeVisible();
    await expect(
      page.getByRole("button", { name: "Hiện việc đã xong" }),
    ).toBeHidden();
  });

  test("footer: counts, link and key hints", async ({ page }) => {
    await openTable(page);
    const f = MOCK.footer;
    const summary = page.getByText("11 công việc đang hiện · 5 ẩn bởi bộ lọc");
    const box = await rel(page, summary);
    expect(Math.abs(box.x - f.x)).toBeLessThanOrEqual(1);
    expect(Math.abs(box.height - f.text.height)).toBeLessThanOrEqual(1);
    expect(await style(summary, "font-size")).toBe("12.5px");
    expect(await colour(summary, "color")).toBe(COLOUR.meta);
    const link = page.getByRole("button", { name: "Hiện việc đã xong" });
    expect(await style(link, "font-weight")).toBe("600");
    expect(await colour(link, "color")).toBe("rgb(15, 86, 174)");
    // 16 px gap between the count and the link.
    const linkBox = await rel(page, link);
    expect(Math.abs(linkBox.x - (box.x + box.width + 16))).toBeLessThanOrEqual(
      1,
    );
    // The Enter key chip and the key-hint strip are gone (owner rule).
    await expect(
      page.locator("span.border").filter({ hasText: /^Enter$/ }),
    ).toHaveCount(0);
    await expect(
      page.locator("[data-slot=main-table-surface]").getByText("di chuyển"),
    ).toHaveCount(0);
  });

  test("archive notice: Đã lưu trữ 2 việc with Hoàn tác", async ({ page }) => {
    await openTable(page);
    await page.getByRole("checkbox", { name: "Chọn ERP-142" }).click();
    await page.getByRole("checkbox", { name: "Chọn ERP-146" }).click();
    await page.getByRole("button", { name: "Lưu trữ" }).click();
    const notice = page.getByRole("status").filter({
      hasText: "Đã lưu trữ 2 việc",
    });
    await expect(notice).toBeVisible();
    const n = MOCK.notice;
    expectRect(
      await rel(page, notice, { fixedToBottom: true }),
      { x: n.x, y: n.y, width: n.width, height: n.height },
      "notice",
    );
    expect(await style(notice, "border-radius")).toBe("12px");
    expectRect(
      await rel(page, notice.getByRole("button", { name: "Hoàn tác" }), {
        fixedToBottom: true,
      }),
      n.undo,
      "Hoàn tác",
    );
    // The rows have left the table; undoing brings them back.
    await expect(row(page, "ERP-142")).toHaveCount(0);
    await notice.getByRole("button", { name: "Hoàn tác" }).click();
    await expect(row(page, "ERP-142")).toHaveCount(1);
    await expect(notice).toBeHidden();
  });
});
