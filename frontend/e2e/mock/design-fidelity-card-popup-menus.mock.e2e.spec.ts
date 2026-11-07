import AxeBuilder from "@axe-core/playwright";
import { expect, test, type Locator, type Page } from "@playwright/test";

/**
 * The card popup's column picker against `Kanban-CardPopup-Menus` (owner artifact 18L4jUXGfdXh7Wn9YQvA2x), frame 2
 * "Chọn cột ở đầu popup", and `Kanban-CardPopup-M-Sheets-3` frame 2 (the 390 bottom sheet), light and dark: a panel
 * with "Chuyển sang cột" and a close button, one row per column with its count (or "n / limit"; the current column
 * draws a 16 px check and no count), "Đã đủ giới hạn" under a column at its limit, "Vị trí trong cột" with its
 * select, and the note. The other frames of the board (the "+ Thêm" menus, the "⋯" menu, the shortcut legend, the
 * board's own fields) are not part of this spec.
 *
 * Every number was read from the artboard rendered in Chromium (box relative to the panel's top-left, then font
 * size, weight, radius and colour). Tolerance is 1 px for boxes; font sizes, weights and radii are equal; colours
 * differ by at most 10 per channel. The mock world's Chờ duyệt column has 2 cards, so its limit is set to 2 through
 * the column's own menu (the artboard draws 3 / 3).
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const latency = process.env.VITE_MOCK_LATENCY ?? "";
const shots = process.env.WM_SHOTS_DIR ?? "";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const NOW = new Date("2026-10-12T10:40:00+07:00");
const TOL = 1;

interface Box {
  x: number;
  y: number;
  w: number;
  h: number;
}
const near = (actual: number, expected: number, what: string, tol = TOL) =>
  expect(
    Math.abs(actual - expected),
    `${what}: ${actual} vs ${expected}`,
  ).toBeLessThanOrEqual(tol);

async function box(el: Locator, origin: Locator): Promise<Box> {
  const [e, o] = await Promise.all([el.boundingBox(), origin.boundingBox()]);
  if (!e || !o) throw new Error("element is not laid out");
  return { x: e.x - o.x, y: e.y - o.y, w: e.width, h: e.height };
}
function expectBox(actual: Box, expected: Partial<Box>, what: string) {
  for (const k of ["x", "y", "w", "h"] as const)
    if (expected[k] !== undefined)
      near(actual[k], expected[k]!, `${what}.${k}`);
}
async function style(el: Locator) {
  return el.evaluate((node) => {
    const cs = getComputedStyle(node);
    const rgb = (value: string) => {
      const c = document.createElement("canvas").getContext("2d")!;
      c.fillStyle = "#000";
      c.fillRect(0, 0, 1, 1);
      c.fillStyle = value;
      c.fillRect(0, 0, 1, 1);
      const [r, g, b] = c.getImageData(0, 0, 1, 1).data;
      return [r, g, b];
    };
    return {
      fs: cs.fontSize,
      fw: cs.fontWeight,
      r: cs.borderTopLeftRadius,
      c: rgb(cs.color),
      bg: rgb(cs.backgroundColor),
    };
  });
}
function expectColor(
  actual: number[],
  expected: number[],
  what: string,
  tol = 10,
) {
  for (let i = 0; i < 3; i += 1)
    near(actual[i]!, expected[i]!, `${what} ${"rgb"[i]}`, tol);
}

const COLORS = {
  light: {
    panel: [255, 255, 255],
    text: [22, 26, 33],
    cap: [79, 88, 102],
    meta: [95, 104, 117],
    curBg: [231, 240, 252],
    curInk: [15, 86, 174],
    warn: [138, 90, 0],
  },
  dark: {
    panel: [26, 32, 40],
    text: [230, 233, 238],
    cap: [170, 179, 192],
    meta: [143, 153, 167],
    curBg: [36, 54, 74],
    curInk: [125, 187, 255],
    warn: [242, 193, 78],
  },
} as const;

async function open(page: Page, width: number, dark: boolean) {
  await page.setViewportSize({ width, height: 900 });
  await page.clock.setFixedTime(NOW);
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

/** A limit of 2 on Chờ duyệt (2 items): the column is at its limit. */
async function limitReview(page: Page) {
  const lane = page.getByRole("region", { name: "Chờ duyệt", exact: true });
  await lane.getByRole("button", { name: "Tuỳ chọn cột Chờ duyệt" }).click();
  await page.getByRole("menuitem", { name: "Đặt giới hạn số việc" }).click();
  await page
    .getByRole("textbox", { name: "Giới hạn số việc của Chờ duyệt" })
    .fill("2");
  await page.getByRole("button", { name: "Lưu giới hạn" }).click();
  await page.keyboard.press("Escape");
}

async function openPicker(page: Page, itemId = "mock-erp-item-144") {
  await page.locator(`button[data-card-id="${itemId}"]`).first().click();
  await page.getByTitle("Chuyển thẻ sang cột khác").click();
  const picker = page.getByRole("dialog", { name: "Chuyển sang cột" });
  await expect(picker).toBeVisible();
  await page.waitForTimeout(300);
  return picker;
}

const shot = async (page: Page, name: string) => {
  if (shots) await page.screenshot({ path: `${shots}/${name}.png` });
};

const cardsOf = (page: Page, lane: string) =>
  page
    .getByRole("region", { name: lane, exact: true })
    .locator("button[data-card-id]");

test.beforeEach(() => {
  test.skip(
    persona !== "owner" ||
      scenario !== "default" ||
      faults !== "" ||
      latency === "slow",
    "Needs the default owner world",
  );
});

/**
 * The two frames of the picker, boxes relative to the panel's top-left. 1440 is `Kanban-CardPopup-Menus` frame 2; 390
 * is `Kanban-CardPopup-M-Sheets-3` frame 2 (a 16 px radius bottom sheet with a 40 x 4 grab bar). The sheet's own
 * rows are 44 px, not the drawn 40: the 44 px touch target rule beats the drawn row, an accepted +16 px on the
 * sheet from the rows down (rows start at y 72 as drawn; caption, select and note sit 16 px lower, the sheet is
 * 397.6 px high, not 381.6).
 */
const FRAMES = [
  {
    name: "1440",
    width: 1440,
    sheet: false,
    panel: { w: 300, h: 343.6, r: "12px" },
    header: { x: 13, y: 13, w: 274, h: 32 },
    close: { x: 259, y: 13, w: 32, h: 32 },
    list: { x: 13, y: 53, w: 274, h: 160 },
    row: { x: 13, y: 53, w: 274, h: 40 },
    dotY: 15.5,
    limitNoteY: 21.1,
    caption: { x: 13, y: 225, w: 274, h: 17.4 },
    select: { x: 13, y: 248.4, w: 274, h: 36 },
    note: { x: 15, y: 294.4, w: 270, h: 36.3 },
  },
  {
    name: "390",
    width: 390,
    sheet: true,
    panel: { w: 390, h: 397.6, r: "16px" },
    header: { x: 16, y: 20, w: 358, h: 44 },
    close: { x: 334, y: 20, w: 44, h: 44 },
    list: { x: 16, y: 72, w: 358, h: 176 },
    row: { x: 16, y: 72, w: 358, h: 44 },
    dotY: 17.5,
    limitNoteY: 23.1,
    caption: { x: 16, y: 260, w: 358, h: 17.4 },
    select: { x: 16, y: 283.4, w: 358, h: 44 },
    note: { x: 18, y: 337.4, w: 354, h: 36.3 },
  },
] as const;

for (const frame of FRAMES) {
  for (const dark of [false, true]) {
    const mode = dark ? "dark" : "light";
    test(`${frame.name} ${mode}: the picker as drawn, with the count, the check on the current row, the limit note and the position select`, async ({
      page,
    }) => {
      await open(page, frame.width, dark);
      await limitReview(page);
      // ERP-151 sits in Cần làm and its group also has cards in Hoàn thành, so the position select is drawn.
      const picker = await openPicker(page, "mock-erp-item-151");
      const c = COLORS[mode];
      const pb = await picker.boundingBox();
      near(pb!.width, frame.panel.w, "panel width");
      near(pb!.height, frame.panel.h, "panel height");
      const ps = await style(picker);
      expect(ps.r).toBe(frame.panel.r);
      expectColor(ps.bg, [...c.panel], "panel bg");
      if (frame.sheet) {
        // The grab bar: 40 x 4 at 8 px from the top, centred.
        const grab = picker.locator("> div").first();
        expectBox(
          await box(grab, picker),
          { x: 175, y: 8, w: 40, h: 4 },
          "grab bar",
        );
      }

      const title = picker
        .locator("b")
        .filter({ hasText: /^Chuyển sang cột$/ });
      expectBox(await box(title.locator(".."), picker), frame.header, "header");
      const tb = await box(title, picker);
      near(tb.x + tb.w / 2, pb!.width / 2, "title is centred");
      const ts = await style(title);
      expect([ts.fs, ts.fw]).toEqual(["14px", "650"]);
      const close = picker.getByRole("button", { name: "Đóng" });
      expectBox(await box(close, picker), frame.close, "close");
      expect((await style(close)).r).toBe("8px");

      const list = picker.getByRole("listbox", { name: "Chuyển sang cột" });
      expectBox(await box(list, picker), frame.list, "list");
      const options = list.getByRole("option");
      await expect(options).toHaveCount(4);
      // The current row draws a 16 px check and no count (as drawn); the others keep their count.
      const expected = [
        { name: /^Cần làm/, current: true },
        { name: /^Đang làm/, count: "3" },
        { name: /^Chờ duyệt/, count: "2 / 2", full: true },
        { name: /^Hoàn thành/, count: "5" },
      ];
      for (const [i, want] of expected.entries()) {
        const row = options.nth(i);
        await expect(row).toHaveAccessibleName(want.name);
        expectBox(
          await box(row, picker),
          {
            x: frame.row.x,
            y: frame.row.y + frame.row.h * i,
            w: frame.row.w,
            h: frame.row.h,
          },
          `row ${i}`,
        );
        const rs = await style(row);
        expect(rs.r).toBe("8px");
        expect(rs.fs).toBe("14px");
        expect(rs.fw).toBe(want.current ? "600" : "400");
        expect(await row.getAttribute("aria-selected")).toBe(
          want.current ? "true" : "false",
        );
        if (want.current) {
          // The selected-row token: its blue sits up to 30 off the artboard accent-soft in dark.
          expectColor(rs.bg, [...c.curBg], "current bg", 30);
          expectColor(rs.c, [...c.curInk], "current ink", 24);
        }
        // Dot 9 px at 10 px from the row's left, the count (or the check) 10 px from its right edge.
        const dot = row.locator("> span").first();
        expectBox(
          await box(dot, row),
          { x: 10, y: frame.dotY, w: 9, h: 9 },
          `dot ${i}`,
        );
        const count = row.locator("small");
        if (want.current) {
          await expect(count).toHaveCount(0);
          const check = row.locator("svg");
          await expect(check).toHaveCount(1);
          const kb = await box(check, row);
          near(kb.x + kb.w, frame.row.w - 10, "check right edge");
          near(kb.w, 16, "check width");
          near(kb.h, 16, "check height");
          near(kb.y + kb.h / 2, frame.row.h / 2, "check is centred");
          expectColor(
            (await style(check)).c,
            [...c.curInk],
            "check colour",
            24,
          );
        } else {
          await expect(count).toHaveText(want.count!);
          const cb = await box(count, row);
          near(cb.x + cb.w, frame.row.w - 10, `count ${i} right edge`);
          const cs = await style(count);
          expect([cs.fs, cs.fw]).toEqual(["12.5px", "400"]);
          expectColor(cs.c, [...c.meta], `count ${i} colour`);
        }
        const note = row.getByText("Đã đủ giới hạn", { exact: true });
        if (want.full) {
          await expect(note).toBeVisible();
          const ns = await style(note);
          expect([ns.fs, ns.fw]).toEqual(["12px", "500"]);
          expectColor(ns.c, [...c.warn], "limit note colour", 40);
          // Under the name: 15.6 px high at 29 px from the row's left.
          expectBox(
            await box(note, row),
            { x: 29, y: frame.limitNoteY, h: 15.6 },
            "limit note",
          );
        } else await expect(note).toHaveCount(0);
      }

      const cap = picker.getByText("Vị trí trong cột", { exact: true });
      expectBox(await box(cap, picker), frame.caption, "caption");
      const cps = await style(cap);
      expect([cps.fs, cps.fw]).toEqual(["12px", "600"]);
      expectColor(cps.c, [...c.cap], "caption colour");
      const select = picker.getByRole("combobox", { name: "Vị trí trong cột" });
      expectBox(await box(select, picker), frame.select, "select");
      const ss = await style(select);
      expect([ss.fs, ss.r]).toEqual(["14px", "8px"]);
      await expect(select).toHaveValue("bottom");
      await expect(select.locator("option:checked")).toHaveText("Cuối cột");
      const note = picker.getByText(/^Chuyển cột là lưu ngay/);
      expectBox(await box(note, picker), frame.note, "note");
      const nts = await style(note);
      expect(nts.fs).toBe("12.5px");
      expectColor(nts.c, [...c.meta], "note colour");
      await shot(page, `card-popup-picker-${frame.name}-${mode}`);
    });
  }
}

/** Closes the picker and the popup so the lanes are readable again (the popup is modal). */
async function closePopup(page: Page) {
  // The popup ignores Escape until the move has settled: press again until it is gone.
  await expect(async () => {
    await page.keyboard.press("Escape");
    await expect(page.getByRole("dialog")).toHaveCount(0, { timeout: 1_000 });
  }).toPass();
}

test("the position choice puts the card at the top or the bottom of its group in the column", async ({
  page,
}) => {
  await open(page, 1440, false);
  const ids = () =>
    cardsOf(page, "Cần làm").evaluateAll((nodes) =>
      nodes.map((node) => node.getAttribute("data-card-id")),
    );
  // ERP-129 and ERP-136 are prep cards of Hoàn thành; Cần làm holds the prep card ERP-151 (then the training cards).
  const picker = await openPicker(page, "mock-erp-item-129");
  await picker
    .getByRole("combobox", { name: "Vị trí trong cột" })
    .selectOption("top");
  await picker.getByRole("option", { name: /^Cần làm/ }).click();
  await closePopup(page);
  await expect
    .poll(async () => (await ids()).slice(0, 2))
    .toEqual(["mock-erp-item-129", "mock-erp-item-151"]);

  const second = await openPicker(page, "mock-erp-item-136");
  await expect(
    second.getByRole("combobox", { name: "Vị trí trong cột" }),
  ).toHaveValue("bottom");
  await second.getByRole("option", { name: /^Cần làm/ }).click();
  await closePopup(page);
  // The bottom of the prep cards: after ERP-151, before the training cards.
  await expect
    .poll(async () => (await ids()).slice(0, 3))
    .toEqual(["mock-erp-item-129", "mock-erp-item-151", "mock-erp-item-136"]);
});

test("the select is not drawn when no other column holds a card of the item's group", async ({
  page,
}) => {
  await open(page, 1440, false);
  // The three cards of ERP-144's group all sit in Đang làm: no other column could name a place for it.
  const picker = await openPicker(page);
  await expect(picker.getByRole("option")).toHaveCount(4);
  await expect(
    picker.getByRole("combobox", { name: "Vị trí trong cột" }),
  ).toHaveCount(0);
  await expect(picker.getByText("Vị trí trong cột")).toHaveCount(0);
});

test("a column at its limit says so and still takes the card; the count is read from the board", async ({
  page,
}) => {
  await open(page, 1440, false);
  await limitReview(page);
  const picker = await openPicker(page);
  const review = picker.getByRole("option", { name: /^Chờ duyệt/ });
  await expect(review).toContainText("Đã đủ giới hạn");
  await expect(review).toContainText("2 / 2");
  await review.click();
  await closePopup(page);
  await expect(cardsOf(page, "Chờ duyệt")).toHaveCount(3);
  // The column keeps its limit (it only warns) and reads as over it on the next pick.
  const again = await openPicker(page, "mock-erp-item-142");
  await expect(again.getByRole("option", { name: /^Chờ duyệt/ })).toContainText(
    "3 / 2",
  );
});

test("keyboard: the picker opens from the keyboard, the select takes arrow keys and the close button returns focus", async ({
  page,
}) => {
  await open(page, 1440, false);
  await page
    .locator('button[data-card-id="mock-erp-item-151"]')
    .first()
    .click();
  const trigger = page.getByTitle("Chuyển thẻ sang cột khác");
  // The popup moves focus into itself as it opens: focus the trigger only after that, or the picker returns focus there.
  await expect(trigger).toBeVisible();
  await page.waitForTimeout(500);
  await trigger.focus();
  await page.keyboard.press("Enter");
  const picker = page.getByRole("dialog", { name: "Chuyển sang cột" });
  await expect(picker).toBeVisible();
  const select = picker.getByRole("combobox", { name: "Vị trí trong cột" });
  await select.focus();
  await expect(select).toBeFocused();
  if (process.platform === "darwin")
    // macOS Chromium opens a native list on the first key and headless never commits it: pick through the select itself.
    await select.selectOption("top");
  else {
    // The first arrow can open the list, the second reaches "top"; Enter commits.
    await page.keyboard.press("ArrowUp");
    await page.keyboard.press("ArrowUp");
    await page.keyboard.press("Enter");
  }
  await expect(select).toHaveValue("top");
  await picker.getByRole("button", { name: "Đóng" }).focus();
  await page.keyboard.press("Enter");
  await expect(picker).toHaveCount(0);
  await expect(trigger).toBeFocused();
});

for (const [width, tablet] of [
  [1440, false],
  [1024, false],
  [390, true],
] as const) {
  for (const dark of [false, true]) {
    const mode = dark ? "dark" : "light";
    test(`${width} ${mode}: no serious axe violation${tablet ? ", 44 px rows, select and close" : ""}`, async ({
      page,
    }) => {
      await open(page, width, dark);
      await limitReview(page);
      const picker = await openPicker(page, "mock-erp-item-151");
      if (tablet) {
        // At 390 the panel is a bottom sheet: every target is at least 44 px.
        // The select's own <option> elements are options too: the column rows are the listbox's.
        for (const row of await picker
          .getByRole("listbox")
          .getByRole("option")
          .all())
          expect((await row.boundingBox())!.height).toBeGreaterThanOrEqual(44);
        expect(
          (await picker.getByRole("combobox").boundingBox())!.height,
        ).toBeGreaterThanOrEqual(44);
        const close = await picker
          .getByRole("button", { name: "Đóng" })
          .boundingBox();
        expect(close!.width).toBeGreaterThanOrEqual(44);
        expect(close!.height).toBeGreaterThanOrEqual(44);
      }
      await shot(page, `card-popup-picker-${width}-${mode}`);
      const violations = (
        await new AxeBuilder({ page }).analyze()
      ).violations.filter((v) =>
        ["serious", "critical"].includes(v.impact ?? ""),
      );
      expect(violations.map((v) => `${v.id}: ${v.nodes.length}`)).toEqual([]);
    });
  }
}
