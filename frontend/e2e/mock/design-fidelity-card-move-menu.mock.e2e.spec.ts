import AxeBuilder from "@axe-core/playwright";
import { expect, test, type Locator, type Page } from "@playwright/test";

/**
 * Moving a card between columns from the card's own "⋯" menu, against the owner canvas (artifact
 * 18L4jUXGfdXh7Wn9YQvA2x): `Final-T-Board` state `menu-the` (1180; the same menu at 1440) draws the menu with
 * "Chuyển sang", the current column on the item and a submenu of the columns with a check; `Final-M-Board` state
 * `chuyen-cot` (390) draws the "Chuyển sang" radio sheet with "Hiện tại". Numbers were read from both artboards
 * rendered in Chromium (box relative to the menu or sheet top-left, then font, radius, colour), light and dark.
 *
 * Measured here: the menu panel (272 wide, 14 px radius), its "Chuyển sang" item, the submenu (210 px, 14 px radius,
 * 6 px right of the menu, level with its top), its four rows and the check; at 390 the sheet's grab bar, header,
 * close, caption and the four rows. Not measured: the artboard menu's other items (Giao cho…, Đổi hạn…, Mở bên
 * cạnh, Mở toàn màn hình, Sao chép liên kết, Lưu trữ) and the sheet's rows under the columns (Giao cho…, Đổi hạn…,
 * Mở toàn trang): they belong to other use cases and the app's menu holds its own actions. Tolerance is 1 px for
 * boxes, colours differ by at most 10 per channel unless a looser bound is written.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const latency = process.env.VITE_MOCK_LATENCY ?? "";
const shots = process.env.WM_SHOTS_DIR ?? "";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const NOW = new Date("2026-10-12T10:40:00+07:00");
const CARD = "mock-erp-item-144";
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
    sheet: [255, 255, 255],
    text: [22, 26, 33],
    meta: [95, 104, 117],
    icon: [79, 88, 102],
    ink: [15, 86, 174],
  },
  dark: {
    panel: [28, 35, 44],
    sheet: [21, 26, 33],
    text: [230, 233, 238],
    meta: [143, 153, 167],
    icon: [170, 179, 192],
    ink: [125, 187, 255],
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

const cardOf = (page: Page, id = CARD) =>
  page
    .locator("article")
    .filter({ has: page.locator(`button[data-card-id="${id}"]`) });

/** Opens the card's "⋯" (it shows on hover and focus) and returns the menu. */
async function openMenu(page: Page, id = CARD) {
  const card = cardOf(page, id);
  await card.hover();
  const more = card.getByRole("button", { name: "Thêm tuỳ chọn" });
  await more.click();
  const menu = page.getByRole("menu", { name: "Thêm tuỳ chọn" });
  await expect(menu).toBeVisible();
  // The menu zooms in for 100 ms: measure it at rest.
  await page.waitForTimeout(300);
  return { menu, more };
}

/** The "Chuyển sang" item (not "Chuyển sang board khác…"). */
const moveItem = (scope: Page | Locator) =>
  scope.getByRole("menuitem", { name: /^Chuyển sang (?!board)/ });

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

for (const width of [1440, 1180]) {
  for (const dark of [false, true]) {
    const mode = dark ? "dark" : "light";
    test(`${width} ${mode}: the card menu has "Chuyển sang" with the current column and a submenu of the columns, as drawn`, async ({
      page,
    }) => {
      await open(page, width, dark);
      const c = COLORS[mode];
      const { menu } = await openMenu(page);
      const mb = await menu.boundingBox();
      near(mb!.width, 272, "menu width");
      const ms = await style(menu);
      expect(ms.r).toBe("14px");
      expectColor(ms.bg, [...c.panel], "menu bg", 14);

      const item = moveItem(menu);
      expectBox(
        await box(item, menu),
        { x: 7, y: 7, w: 258, h: 44 },
        "Chuyển sang item",
      );
      const is = await style(item);
      expect([is.fs, is.fw, is.r]).toEqual(["15px", "500", "9px"]);
      expectColor(is.c, [...c.text], "item colour");
      // The 20 px icon sits 12 px from the item's left; the current column and a 13 px chevron end the row.
      const icon = item.locator("svg").first();
      expectBox(await box(icon, item), { x: 12, w: 20, h: 20 }, "item icon");
      const current = item.getByText("Đang làm", { exact: true });
      const cs = await style(current);
      expect([cs.fs, cs.fw]).toEqual(["13.5px", "500"]);
      expectColor(cs.c, [...c.meta], "current column colour");
      const chevron = item.locator("svg").last();
      const kb = await box(chevron, item);
      near(kb.w, 13, "chevron width");
      near(kb.x + kb.w, 246, "chevron right edge");
      const cb = await box(current, item);
      near(cb.x + cb.w + 4, kb.x, "current column and chevron gap 4");

      await item.focus();
      await page.keyboard.press("ArrowRight");
      const sub = page.locator('[data-slot="dropdown-menu-sub-content"]');
      await expect(sub).toBeVisible();
      await page.waitForTimeout(300);
      const sb = await box(sub, menu);
      expectBox(sb, { x: 278, y: 0, w: 210, h: 190 }, "submenu");
      const ss = await style(sub);
      expect(ss.r).toBe("14px");
      expectColor(ss.bg, [...c.panel], "submenu bg", 14);
      const rows = sub.getByRole("menuitemradio");
      await expect(rows).toHaveText([
        "Cần làm",
        "Đang làm",
        "Chờ duyệt",
        "Hoàn thành",
      ]);
      for (const [i, name] of [
        "Cần làm",
        "Đang làm",
        "Chờ duyệt",
        "Hoàn thành",
      ].entries()) {
        const row = rows.nth(i);
        expectBox(
          await box(row, sub),
          { x: 7, y: 7 + 44 * i, w: 196, h: 44 },
          `row ${name}`,
        );
        const rs = await style(row);
        expect([rs.fs, rs.fw, rs.r]).toEqual(["15px", "500", "9px"]);
        expectColor(rs.c, [...c.text], `row ${name} colour`);
        expect(await row.getAttribute("aria-checked")).toBe(
          name === "Đang làm" ? "true" : "false",
        );
        const dot = row.locator("span[aria-hidden]").first();
        expectBox(
          await box(dot, row),
          { x: 12, y: 17, w: 10, h: 10 },
          `dot ${name}`,
        );
        if (name === "Đang làm") {
          const check = row.locator("svg");
          const ckb = await box(check, row);
          near(ckb.w, 20, "check width");
          near(ckb.h, 20, "check height");
          near(ckb.x + ckb.w, 184, "check right edge");
          expectColor((await style(check)).c, [...c.ink], "check colour", 24);
        } else await expect(row.locator("svg")).toHaveCount(0);
      }
      await shot(page, `card-move-menu-${width}-${mode}`);
      // A modal menu hides the board behind it (aria-hidden over focusable cards): the menu and its submenu are checked.
      const violations = (
        await new AxeBuilder({ page }).include('[role="menu"]').analyze()
      ).violations.filter((v) =>
        ["serious", "critical"].includes(v.impact ?? ""),
      );
      expect(violations.map((v) => `${v.id}: ${v.nodes.length}`)).toEqual([]);
    });
  }
}

for (const dark of [false, true]) {
  const mode = dark ? "dark" : "light";
  test(`390 ${mode}: the "Chuyển sang" sheet is drawn as the artboard, 44 px or taller rows, no serious axe violation`, async ({
    page,
  }) => {
    await open(page, 390, dark);
    const c = COLORS[mode];
    const { menu } = await openMenu(page);
    await moveItem(menu).click();
    const sheet = page.getByRole("dialog", {
      name: /^Kiểm thử phân hệ công nợ/,
    });
    await expect(sheet).toBeVisible();
    await page.waitForTimeout(400);
    const sb = await sheet.boundingBox();
    near(sb!.width, 390, "sheet width");
    const ss = await style(sheet);
    expect(ss.r).toBe("14px");
    expectColor(ss.bg, [...c.sheet], "sheet bg", 10);
    expectBox(
      await box(sheet.locator("> div").first(), sheet),
      { x: 177, y: 6, w: 36, h: 5 },
      "grab bar",
    );
    const key = sheet.getByText(/^ERP-144 · Đang làm$/);
    expectBox(await box(key, sheet), { x: 20, y: 23, w: 310, h: 17.4 }, "key");
    const ks = await style(key);
    expect(ks.fs).toBe("12px");
    expectColor(ks.c, [...c.meta], "key colour");
    const title = sheet.getByRole("heading", {
      name: /^Kiểm thử phân hệ công nợ/,
    });
    expectBox(
      await box(title, sheet),
      { x: 20, y: 40.4, w: 310, h: 43.2 },
      "title",
    );
    const ts = await style(title);
    expect([ts.fs, ts.fw]).toEqual(["16px", "650"]);
    const close = sheet.getByRole("button", { name: "Đóng" });
    expectBox(
      await box(close, sheet),
      { x: 338, y: 17, w: 44, h: 44 },
      "close",
    );
    expect((await style(close)).r).toBe("12px");
    const caption = sheet.getByText("Chuyển sang", { exact: true });
    expectBox(
      await box(caption, sheet),
      { x: 0, y: 83.6, w: 390, h: 28.8 },
      "caption",
    );
    const cps = await style(caption);
    expect([cps.fs, cps.fw]).toEqual(["13px", "600"]);
    expectColor(cps.c, [...c.meta], "caption colour");

    const rows = sheet.getByRole("radio");
    await expect(rows).toHaveCount(4);
    for (const [i, name] of [
      "Cần làm",
      "Đang làm",
      "Chờ duyệt",
      "Hoàn thành",
    ].entries()) {
      const row = rows.nth(i);
      expectBox(
        await box(row, sheet),
        { x: 0, y: 112.4 + 52 * i, w: 390, h: 52 },
        `row ${name}`,
      );
      const current = name === "Đang làm";
      expect(await row.getAttribute("aria-checked")).toBe(String(current));
      const label = row.getByText(name, { exact: true });
      const ls = await style(label);
      expect([ls.fs, ls.fw]).toEqual(["15px", current ? "650" : "500"]);
      expectColor(ls.c, [...c.text], `row ${name} colour`);
      const lb = await box(label, row);
      near(lb.x, 68, `row ${name} label x`);
      expect((await row.boundingBox())!.height).toBeGreaterThanOrEqual(44);
      if (current) {
        const here = row.getByText("Hiện tại", { exact: true });
        const hs = await style(here);
        expect(hs.fs).toBe("13px");
        expectColor(hs.c, [...c.ink], "Hiện tại colour", 24);
        const check = row.locator("svg");
        const ckb = await box(check, row);
        near(ckb.w, 20, "check width");
        near(ckb.x + ckb.w, 370, "check right edge");
      }
    }
    await shot(page, `card-move-sheet-${mode}`);
    const violations = (
      await new AxeBuilder({ page }).analyze()
    ).violations.filter((v) =>
      ["serious", "critical"].includes(v.impact ?? ""),
    );
    expect(violations.map((v) => `${v.id}: ${v.nodes.length}`)).toEqual([]);
  });
}

test("a column picked in the submenu moves the card and closes the menu", async ({
  page,
}) => {
  await open(page, 1440, false);
  const { menu } = await openMenu(page);
  await moveItem(menu).focus();
  await page.keyboard.press("ArrowRight");
  const sub = page.locator('[data-slot="dropdown-menu-sub-content"]');
  await expect(
    sub.getByRole("menuitemradio", { name: "Cần làm" }),
  ).toBeFocused();
  await page.keyboard.press("ArrowDown");
  await expect(
    sub.getByRole("menuitemradio", { name: "Đang làm" }),
  ).toBeFocused();
  await page.keyboard.press("ArrowDown");
  await expect(
    sub.getByRole("menuitemradio", { name: "Chờ duyệt" }),
  ).toBeFocused();
  await page.keyboard.press("Enter");
  await expect(menu).toHaveCount(0);
  await expect(cardsOf(page, "Chờ duyệt")).toHaveCount(3);
  await expect(
    page
      .getByRole("region", { name: "Chờ duyệt", exact: true })
      .locator(`[data-card-id="${CARD}"]`),
  ).toHaveCount(1);
  await expect(cardsOf(page, "Đang làm")).toHaveCount(2);
});

test("keyboard: ArrowLeft closes the submenu, Escape closes the menu and the focus returns to the ⋯ button", async ({
  page,
}) => {
  await open(page, 1440, false);
  const { menu, more } = await openMenu(page);
  await moveItem(menu).focus();
  await page.keyboard.press("ArrowRight");
  const sub = page.locator('[data-slot="dropdown-menu-sub-content"]');
  await expect(sub).toBeVisible();
  await page.keyboard.press("ArrowLeft");
  await expect(sub).toHaveCount(0);
  await expect(moveItem(menu)).toBeFocused();
  await page.keyboard.press("Escape");
  await expect(menu).toHaveCount(0);
  await expect(more).toBeFocused();
});

test("390: Escape closes the sheet and returns focus to the ⋯ button, a row moves the card", async ({
  page,
}) => {
  await open(page, 390, false);
  const { menu, more } = await openMenu(page);
  await moveItem(menu).click();
  const sheet = page.getByRole("dialog", { name: /^Kiểm thử phân hệ công nợ/ });
  await expect(sheet).toBeVisible();
  await page.keyboard.press("Escape");
  await expect(sheet).toHaveCount(0);
  await expect(more).toBeFocused();

  await openMenu(page);
  await moveItem(page).click();
  await page.getByRole("radio", { name: /^Chờ duyệt/ }).click();
  await expect(sheet).toHaveCount(0);
  await expect(
    page
      .getByRole("region", { name: "Chờ duyệt", exact: true })
      .locator(`[data-card-id="${CARD}"]`),
  ).toHaveCount(1);
});
