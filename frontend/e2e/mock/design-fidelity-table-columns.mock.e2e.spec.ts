import AxeBuilder from "@axe-core/playwright";
import { expect, test, type Locator, type Page } from "@playwright/test";

/**
 * The table's "Cột" menu against `BC-View-Personal` (owner artifact 18L4jUXGfdXh7Wn9YQvA2x), all 14 frames:
 * `pv-ed` (editor), `pv-vw` (viewer, the save locked with its reason), `pv-S` (saving), `pv-Er` (save failed),
 * `pv-C` (someone saved first), `pv-ok` (saved) and `pv-tbl` (the toolbar with the own-view indicator), each in
 * light and dark. `WM-Table-Columns` drew the same menu before the columns became the member's own (328 px,
 * switches, drag handles, no buttons); `BC-View-Personal` says it supersedes that menu, so the menu is built once,
 * at the later frame's numbers, and keeps drag and Alt+Arrow to reorder.
 *
 * Every number was read from the artboard rendered in Chromium (box relative to the menu's own top-left, then font
 * size, weight, radius and colour). Tolerance is 1 px for boxes; font sizes, weights and radii are equal; colours
 * differ by at most 10 per channel (the tokens are OKLCH, the artboard hex). Text differs between the artboard and
 * the mock world ("hiện 9 trên 12" against the mock's own count), so the heading is measured by box only, and the
 * rows by their first nine (all rows are 34 px). The "Chiều cao hàng" row under the buttons is not drawn by any
 * frame and is left out of the measured height.
 *
 * Which world runs which frame (the others skip):
 * - default owner world: pv-ed, pv-ok, pv-tbl, keyboard, axe, 390 px;
 * - `VITE_MOCK_PERSONA=viewer`: pv-vw;
 * - `VITE_MOCK_LATENCY=slow`: pv-S;
 * - `VITE_MOCK_FAULTS=/boards/mock-board-erp/views=network`: pv-Er; `=409`: pv-C.
 * `WM_SHOTS_DIR` writes a screenshot of each state for review.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const latency = process.env.VITE_MOCK_LATENCY ?? "";
const shots = process.env.WM_SHOTS_DIR ?? "";
const TABLE =
  "/workspaces/mock-workspace-primary/boards/mock-board-erp?view=table";
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
/** Compares the keys given; `w` and `h` of a wording-driven box are simply left out. */
function expectBox(actual: Box, expected: Partial<Box>, what: string) {
  for (const k of ["x", "y", "w", "h"] as const)
    if (expected[k] !== undefined)
      near(actual[k], expected[k]!, `${what}.${k}`);
}

/** The computed values of an element; colours are painted over `backdrop` (a translucent one needs the surface it sits on). */
async function style(el: Locator, backdrop = "#000") {
  return el.evaluate((node, back) => {
    const cs = getComputedStyle(node);
    const rgb = (value: string) => {
      const c = document.createElement("canvas").getContext("2d")!;
      c.fillStyle = back;
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
  }, backdrop);
}
/**
 * `rgb(r, g, b)` or `rgba(r, g, b, a)` from the artboard against a computed colour. A translucent artboard colour
 * is compared as what it makes over `backdrop` (the surface `style` painted it on). The tolerance is 10 per
 * channel, the distance between the OKLCH status tokens and the artboard's hex.
 */
function expectColor(
  actual: number[],
  expected: string,
  what: string,
  backdrop: readonly number[] = [0, 0, 0],
  tol = 10,
) {
  const [r, g, b, a = 1] = expected
    .replace(/rgba?\(|\)/g, "")
    .split(",")
    .map(Number);
  const over = (c: number, back: number) => c * a + back * (1 - a);
  near(actual[0]!, over(r!, backdrop[0]!), `${what} r`, tol);
  near(actual[1]!, over(g!, backdrop[1]!), `${what} g`, tol);
  near(actual[2]!, over(b!, backdrop[2]!), `${what} b`, tol);
}

async function open(page: Page, width: number, dark: boolean) {
  await page.setViewportSize({ width, height: 900 });
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
  await page.goto(TABLE);
  // The phone draws a list, not row groups: the toolbar is what every width shares.
  await expect(columnsButton(page)).toBeVisible({ timeout: 30_000 });
  await page.waitForTimeout(400);
}

const columnsButton = (page: Page) =>
  page.getByRole("button", { name: /^Cột \d+\/\d+$/ });
const menuOf = (page: Page) => page.getByRole("dialog", { name: "Cột" });

async function openMenu(page: Page) {
  await columnsButton(page).click();
  const menu = menuOf(page);
  await expect(menu).toBeVisible();
  await page.waitForTimeout(250);
  return menu;
}

/** A column of the member's own, so "Lưu vào view chung" is live. */
async function hideLabel(menu: Locator) {
  await menu.getByRole("checkbox", { name: "Nhãn" }).click();
}

/** Colours per mode, read from the artboard (light `sang`, dark `dem`). */
const COLORS = {
  light: {
    menuBg: "rgb(255, 255, 255)",
    text: "rgb(22, 26, 33)",
    heading: "rgb(95, 104, 117)",
    hint: "rgb(79, 88, 102)",
    cbxOn: "rgb(22, 102, 204)",
    primary: "rgb(22, 102, 204)",
    primaryInk: "rgb(255, 255, 255)",
    lockedBg: "rgb(230, 233, 238)",
    lockedInk: "rgb(95, 104, 117)",
    err: ["rgb(253, 236, 239)", "rgb(196, 35, 64)"],
    warn: ["rgb(255, 244, 214)", "rgb(138, 90, 0)"],
    ok: ["rgb(229, 244, 236)", "rgb(30, 122, 76)"],
    indicatorBg: "rgb(231, 240, 252)",
    indicatorInk: "rgb(15, 86, 174)",
    dot: "rgb(22, 102, 204)",
  },
  dark: {
    menuBg: "rgb(26, 32, 40)",
    text: "rgb(230, 233, 238)",
    heading: "rgb(143, 153, 167)",
    hint: "rgb(170, 179, 192)",
    cbxOn: "rgb(90, 169, 255)",
    primary: "rgb(90, 169, 255)",
    primaryInk: "rgb(4, 17, 31)",
    lockedBg: "rgb(35, 42, 52)",
    lockedInk: "rgb(143, 153, 167)",
    err: ["rgba(255, 107, 129, 0.14)", "rgb(255, 107, 129)"],
    warn: ["rgba(242, 193, 78, 0.14)", "rgb(242, 193, 78)"],
    ok: ["rgba(86, 201, 138, 0.14)", "rgb(86, 201, 138)"],
    indicatorBg: "rgba(90, 169, 255, 0.16)",
    indicatorInk: "rgb(125, 187, 255)",
    dot: "rgb(90, 169, 255)",
  },
} as const;

/** What every frame shares: the card, the heading, the list (nine rows read), the own-view line. */
async function expectFrame(menu: Locator, dark: boolean) {
  const c = COLORS[dark ? "dark" : "light"];
  const mb = await menu.boundingBox();
  near(mb!.width, 380, "menu width");
  const ms = await style(menu);
  expect(ms.r).toBe("12px");
  expectColor(ms.bg, c.menuBg, "menu bg");
  expectColor(ms.c, c.text, "menu text");

  const heading = menu.locator("> div").first();
  expectBox(
    await box(heading, menu),
    { x: 7, y: 7, w: 366, h: 25.4 },
    "heading",
  );
  const hs = await style(heading);
  expect([hs.fs, hs.fw]).toEqual(["12px", "600"]);
  expectColor(hs.c, c.heading, "heading colour");

  const list = menu.locator("> div").nth(1);
  expectBox(await box(list, menu), { x: 7, y: 34.4, w: 366, h: 300 }, "list");
  const rows = list.locator("> label");
  for (let i = 0; i < 9; i += 1) {
    const row = rows.nth(i);
    expectBox(
      await box(row, menu),
      { x: 7, y: 34.4 + 34 * i, w: 366, h: 34 },
      `row ${i}`,
    );
    const rs = await style(row);
    expect([rs.fs, rs.fw, rs.r]).toEqual(["13.5px", "400", "7px"]);
    const cb = row.getByRole("checkbox");
    expectBox(
      await box(cb, row),
      { x: 10, y: 8, w: 18, h: 18 },
      `checkbox ${i}`,
    );
    expect((await style(cb)).r).toBe("5px");
    expectBox(
      await box(row.locator("> span").first(), row),
      { x: 38 },
      `name ${i}`,
    );
  }
  // Mã and Tên công việc are fixed: checked, locked, with the lock at the right.
  for (const i of [0, 1]) {
    const row = rows.nth(i);
    expect(await row.getByRole("checkbox").getAttribute("aria-checked")).toBe(
      "true",
    );
    expectBox(
      await box(row.getByRole("img", { name: "Cột cố định" }), row),
      { x: 343, w: 13 },
      `lock ${i}`,
    );
  }
  expectColor(
    (await style(rows.nth(2).getByRole("checkbox"))).bg,
    c.cbxOn,
    "checked box",
  );

  const own = menu.locator("> div").nth(2);
  expectBox(
    await box(own, menu),
    { x: 7, y: 340.4, w: 366, h: 35.1 },
    "own-view line",
  );
  const os = await style(own);
  expect(os.fs).toBe("12.5px");
  expectColor(os.c, c.hint, "own-view colour");
  return { c, own };
}

async function expectButtons(
  menu: Locator,
  y: number,
  dark: boolean,
  locked = false,
) {
  const c = COLORS[dark ? "dark" : "light"];
  const back = menu.getByRole("button", { name: "Về view chung" });
  const save = menu.getByRole("button", { name: "Lưu vào view chung" });
  expectBox(
    await box(back, menu),
    { x: locked ? 32.9 : 53.6, y, w: 144.5, h: 44 },
    "Về view chung",
  );
  expectBox(
    await box(save, menu),
    { x: locked ? 185.4 : 206.1, y, w: locked ? 177.6 : 156.9, h: 44 },
    "Lưu vào view chung",
  );
  const bs = await style(back);
  expect([bs.fs, bs.fw, bs.r]).toEqual(["13px", "500", "7px"]);
  const ss = await style(save);
  expect([ss.fs, ss.r]).toEqual(["13px", "7px"]);
  if (locked) {
    expect(ss.fw).toBe("500");
    expectColor(ss.bg, c.lockedBg, "locked bg");
    expectColor(ss.c, c.lockedInk, "locked ink");
  } else {
    expect(ss.fw).toBe("600");
    expectColor(ss.bg, c.primary, "primary bg");
    expectColor(ss.c, c.primaryInk, "primary ink");
  }
}

async function expectNotice(
  menu: Locator,
  tone: "err" | "warn" | "ok",
  dark: boolean,
  size: { h: number },
  action?: { w: number },
) {
  const c = COLORS[dark ? "dark" : "light"];
  const notice = menu.getByRole(tone === "ok" ? "status" : "alert");
  expectBox(
    await box(notice, menu),
    { x: 11, y: 381.5, w: 358, h: size.h },
    "notice",
  );
  const surface = dark ? "rgb(26, 32, 40)" : "rgb(255, 255, 255)";
  const ns = await style(notice, surface);
  expect([ns.fs, ns.r]).toEqual(["13px", "10px"]);
  expectColor(
    ns.bg,
    c[tone][0],
    "notice bg",
    dark ? [26, 32, 40] : [255, 255, 255],
    tone === "err" ? 10 : 40,
  );
  // The success tone is the product's token: the status tokens of success and warning sit up to 40 off the artboard hex on one channel.
  expectColor(
    ns.c,
    c[tone][1],
    "notice ink",
    [0, 0, 0],
    tone === "err" ? 10 : 40,
  );
  const text = notice.locator("b");
  near((await box(text, menu)).x, 51, "notice text x");
  if (action) {
    const button = notice.getByRole("button");
    const b = await box(button, menu);
    near(b.h, 44, "notice action height");
    near(b.w, action.w, "notice action width");
  }
}

const shot = async (page: Page, name: string) => {
  if (shots) await page.screenshot({ path: `${shots}/${name}.png` });
};

const MODES = [false, true] as const;
const label = (dark: boolean) => (dark ? "dark" : "light");

test.describe("owner world", () => {
  test.skip(
    persona !== "owner" ||
      scenario !== "default" ||
      faults !== "" ||
      latency === "slow",
    "Needs the default owner world",
  );

  for (const dark of MODES) {
    test(`pv-ed ${label(dark)}: the editor's menu, Về view chung and Lưu vào view chung`, async ({
      page,
    }) => {
      await open(page, 1440, dark);
      const menu = await openMenu(page);
      await hideLabel(menu);
      await expectFrame(menu, dark);
      await expectButtons(menu, 385.5, dark);
      await shot(page, `columns-ed-${label(dark)}`);
    });

    test(`pv-ok ${label(dark)}: saved to the shared view, a short notice`, async ({
      page,
    }) => {
      await open(page, 1440, dark);
      const menu = await openMenu(page);
      await hideLabel(menu);
      await menu.getByRole("button", { name: "Lưu vào view chung" }).click();
      await expect(menu.getByRole("status")).toContainText(
        "Đã lưu vào view chung",
      );
      await expectFrame(menu, dark);
      await expectNotice(menu, "ok", dark, { h: 57.4 });
      await expectButtons(menu, 452.9, dark);
      await shot(page, `columns-ok-${label(dark)}`);
    });

    test(`pv-tbl ${label(dark)}: the toolbar says the columns are the member's own`, async ({
      page,
    }) => {
      await open(page, 1440, dark);
      const c = COLORS[label(dark) as "light" | "dark"];
      const indicator = page.getByRole("button", {
        name: "Cách xem riêng. Về view chung",
      });
      await expect(indicator).toHaveCount(0);
      const menu = await openMenu(page);
      await hideLabel(menu);
      await page.keyboard.press("Escape");
      await expect(indicator).toBeVisible();
      const pill = columnsButton(page);
      const bi = await indicator.boundingBox();
      const bp = await pill.boundingBox();
      near(bi!.width, 32, "indicator width");
      near(bi!.height, 32, "indicator height");
      near(bi!.y, bp!.y, "indicator y against the Cột pill");
      // The toolbar row of the artboard: 8 px between boxes and a 4 px margin each side of the separator.
      const bs = await page
        .locator("[data-table-toolbar] > span.w-px")
        .boundingBox();
      near(
        bs!.x - (bi!.x + bi!.width),
        12,
        "gap from the indicator to the separator",
      );
      near(
        bp!.x - (bs!.x + bs!.width),
        12,
        "gap from the separator to the Cột pill",
      );
      const page_ = dark ? [14, 17, 22] : [247, 248, 250];
      const st = await style(indicator, `rgb(${page_.join(",")})`);
      expect(st.r).toBe("7px");
      expectColor(st.bg, c.indicatorBg, "indicator bg", page_);
      // The selected-chip token, the same as the done chip: its blue is up to 24 off the artboard's accent text.
      expectColor(st.c, c.indicatorInk, "indicator ink", [0, 0, 0], 24);
      const dot = indicator.locator("span");
      const bd = await box(dot, indicator);
      expectBox(bd, { x: 19, y: 5, w: 8, h: 8 }, "indicator dot");
      expectColor((await style(dot)).bg, c.dot, "indicator dot colour");
      await shot(page, `columns-tbl-${label(dark)}`);
    });
  }

  test("keyboard: Space toggles, Alt+Arrow reorders and keeps focus, Escape closes and returns focus", async ({
    page,
  }) => {
    await open(page, 1440, false);
    await columnsButton(page).focus();
    await page.keyboard.press("Enter");
    const menu = menuOf(page);
    await expect(menu).toBeVisible();
    const names = () =>
      menu
        .locator("> div")
        .nth(1)
        .locator("> label")
        .evaluateAll((labels) => labels.map((l) => l.textContent?.trim()));
    const before = await names();
    const status = menu.getByRole("checkbox", { name: "Trạng thái" });
    await status.focus();
    await page.keyboard.press("Space");
    await expect(status).toHaveAttribute("aria-checked", "false");
    await page.keyboard.press("Space");
    await expect(status).toHaveAttribute("aria-checked", "true");
    await page.keyboard.press("Alt+ArrowDown");
    await expect(status).toBeFocused();
    const i = before.indexOf("Trạng thái");
    await expect.poll(async () => (await names())[i + 1]).toBe("Trạng thái");
    expect((await names())[i]).toBe(before[i + 1]);
    await page.keyboard.press("Alt+ArrowUp");
    await expect.poll(names).toEqual(before);
    await page.keyboard.press("Escape");
    await expect(menu).toHaveCount(0);
    await expect(columnsButton(page)).toBeFocused();
  });

  test("a row dragged onto another takes its place", async ({ page }) => {
    await open(page, 1440, false);
    const menu = await openMenu(page);
    const rows = menu.locator("> div").nth(1).locator("> label");
    const names = () =>
      rows.evaluateAll((labels) => labels.map((l) => l.textContent?.trim()));
    const before = await names();
    await rows
      .filter({ hasText: /^Ưu tiên$/ })
      .dragTo(rows.filter({ hasText: /^Trạng thái$/ }));
    const after = await names();
    expect(after.indexOf("Ưu tiên")).toBe(before.indexOf("Trạng thái"));
    expect(after.slice(0, 2)).toEqual(before.slice(0, 2));
  });

  for (const [width, mobile] of [
    [1440, false],
    [1024, false],
    [390, true],
  ] as const) {
    for (const dark of MODES) {
      test(`${width} ${label(dark)}: stays inside the page, no serious axe violation${mobile ? ", 44 px rows and buttons" : ""}`, async ({
        page,
      }) => {
        await open(page, width, dark);
        const menu = await openMenu(page);
        await hideLabel(menu);
        const mb = await menu.boundingBox();
        expect(mb!.x).toBeGreaterThanOrEqual(-0.5);
        expect(mb!.x + mb!.width).toBeLessThanOrEqual(width + 0.5);
        if (mobile) {
          const rows = menu.locator("> div").nth(1).locator("> label");
          for (let i = 0; i < 4; i += 1)
            expect(
              (await rows.nth(i).boundingBox())!.height,
            ).toBeGreaterThanOrEqual(44);
          for (const name of ["Về view chung", "Lưu vào view chung"])
            expect(
              (await menu.getByRole("button", { name }).boundingBox())!.height,
            ).toBeGreaterThanOrEqual(44);
        }
        await shot(page, `columns-${width}-${label(dark)}`);
        const violations = (
          await new AxeBuilder({ page }).analyze()
        ).violations.filter((v) =>
          ["serious", "critical"].includes(v.impact ?? ""),
        );
        expect(violations.map((v) => `${v.id}: ${v.nodes.length}`)).toEqual([]);
      });
    }
  }
});

test.describe("viewer world", () => {
  test.skip(persona !== "viewer", "Needs VITE_MOCK_PERSONA=viewer");
  for (const dark of MODES) {
    test(`pv-vw ${label(dark)}: the viewer still arranges columns; the shared view is locked with its reason`, async ({
      page,
    }) => {
      await open(page, 1440, dark);
      const menu = await openMenu(page);
      await expectFrame(menu, dark);
      await expectButtons(menu, 385.5, dark, true);
      const save = menu.getByRole("button", { name: "Lưu vào view chung" });
      await expect(save).toHaveAttribute("aria-disabled", "true");
      const reason = menu.getByText(
        "Chỉ người sửa board mới lưu được vào view chung.",
      );
      expectBox(
        await box(reason, menu),
        { x: 7, y: 437.5, w: 366, h: 25.4 },
        "reason",
      );
      const hint = await style(reason);
      expect(hint.fs).toBe("12px");
      expectColor(hint.c, COLORS[dark ? "dark" : "light"].heading, "reason");
      // The reason is what the locked button is described by, and pressing it changes nothing.
      const id = await save.getAttribute("aria-describedby");
      await expect(page.locator(`[id="${id}"]`)).toHaveText(
        "Chỉ người sửa board mới lưu được vào view chung.",
      );
      await save.click({ force: true });
      await expect(menu.getByRole("status")).toHaveCount(0);
      // Their own columns still work.
      await hideLabel(menu);
      await expect(
        menu.getByRole("checkbox", { name: "Nhãn" }),
      ).toHaveAttribute("aria-checked", "false");
      await shot(page, `columns-vw-${label(dark)}`);
    });
  }
});

test.describe("slow world", () => {
  test.skip(latency !== "slow" || persona !== "owner", "Needs slow latency");
  for (const dark of MODES) {
    test(`pv-S ${label(dark)}: saving to the shared view`, async ({ page }) => {
      await open(page, 1440, dark);
      const menu = await openMenu(page);
      await hideLabel(menu);
      await menu.getByRole("button", { name: "Lưu vào view chung" }).click();
      const saving = menu.getByRole("status");
      await expect(saving).toContainText("Đang lưu vào view chung…");
      expectBox(
        await box(saving, menu),
        { x: 7, y: 377.5, w: 366, h: 34.1 },
        "saving row",
      );
      const ss = await style(saving);
      expect(ss.fs).toBe("12.5px");
      expectColor(ss.c, COLORS[dark ? "dark" : "light"].hint, "saving colour");
      await expectButtons(menu, 421.6, dark);
      await shot(page, `columns-S-${label(dark)}`);
    });
  }
});

test.describe("save refused", () => {
  const network = faults.includes("/boards/mock-board-erp/views=network");
  const conflict = faults.includes("/boards/mock-board-erp/views=409");
  test.skip(!network && !conflict, "Needs a fault on the board's views");
  for (const dark of MODES) {
    test(`${network ? "pv-Er" : "pv-C"} ${label(dark)}: ${network ? "the save failed, the own columns stay, Thử lại" : "someone saved first, Ghi đè"}`, async ({
      page,
    }) => {
      await open(page, 1440, dark);
      const menu = await openMenu(page);
      await hideLabel(menu);
      await menu.getByRole("button", { name: "Lưu vào view chung" }).click();
      const tone = network ? "err" : "warn";
      await expect(menu.getByRole("alert")).toBeVisible();
      // The server names who saved first; the notice says so.
      if (conflict)
        await expect(menu.getByRole("alert")).toContainText(
          "Hà Nguyễn vừa đổi cột của view chung",
        );
      await expectFrame(menu, dark);
      await expectNotice(
        menu,
        tone,
        dark,
        { h: 75.6 },
        { w: network ? 72.6 : 71.1 },
      );
      await expectButtons(menu, 471.1, dark);
      // The member's own columns are kept.
      await expect(
        menu.getByRole("checkbox", { name: "Nhãn" }),
      ).toHaveAttribute("aria-checked", "false");
      await shot(page, `columns-${network ? "Er" : "C"}-${label(dark)}`);
    });
  }
});
