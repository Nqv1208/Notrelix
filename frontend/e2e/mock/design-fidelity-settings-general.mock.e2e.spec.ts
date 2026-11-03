import { expect, test, type Locator, type Page } from "@playwright/test";

/**
 * The General section of the board settings against the October 2026 mockup
 * `docs/design/mockups/2026-10/src/Final-Settings.dc.html`, state `chung`, rendered at 1440x900 with
 * `nav=an` (light `sang` and dark `dem`) and measured with the box dump. Boxes are read relative to the
 * settings content area's top-left (the artboard's `.content`, 972 x 823.6) and to the page's `main`
 * for the header and the section list; each is compared with 1 px of tolerance. Font sizes, weights,
 * radii and the text colours must be equal.
 *
 * Measured: the page header (mark, trail, title, hint, "Về board"), the first four section buttons, the
 * section header (title, intro, "Huỷ", "Lưu thay đổi"), the seven rows of the card (name, description,
 * icon and colour, department, item key, deadline time zone, first day of the week), and the danger
 * zone (title, archive row, delete row, the typed-name confirmation and its button).
 *
 * Not measured, and why:
 *   - Text widths: the app's body text carries a 0.005em letter spacing the artboard does not; the
 *     right edge of right-aligned text is compared instead.
 *   - The danger zone's body copy: the app says more (an archived board is read only, a deleted board
 *     leaves every list, the counts of the board). A row's height is compared as its 12 px padding, the
 *     19.6 px title and the lines of the body at 18.75 px each.
 *   - Surface and danger colours: the app's tokens are the product's, not the artboard's hexes (card
 *     surface 255 vs 255 in light, 26/32/40 vs 21/26/33 in dark; danger 176/12/21 vs 196/35/64). The
 *     text colours, which are the same in both, are compared.
 *   - The rows after "Nhóm việc" in the section list: the app has Nhãn, Biểu mẫu and Mẫu board between
 *     "Nhóm việc" and the separator, the artboard does not draw them.
 *   - Deadline time zone and first day of the week are the workspace's, which the board inherits: the
 *     rows are drawn as read-only boxes (34 px, same place) in place of the artboard's selects.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const NOW = new Date("2026-10-12T10:40:00+07:00");
const TOL = 1;

interface Rect {
  readonly x: number;
  readonly y: number;
  readonly width: number;
  readonly height: number;
}

async function openGeneral(page: Page, dark: boolean) {
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
  await page.goto(`${BOARD}/settings?from=table&section=general`);
  const section = page.getByRole("region", { name: "Chung" });
  await expect(section).toBeVisible();
  await expect(
    section.getByRole("textbox", { name: "Tên board", exact: true }),
  ).toBeVisible();
  await page.waitForTimeout(500);
  return section;
}

async function rel(locator: Locator, origin: Locator): Promise<Rect> {
  const [e, o] = await Promise.all([
    locator.boundingBox(),
    origin.boundingBox(),
  ]);
  if (!e || !o) throw new Error("element is not laid out");
  return { x: e.x - o.x, y: e.y - o.y, width: e.width, height: e.height };
}

async function style(locator: Locator) {
  return locator.evaluate((element) => {
    const s = getComputedStyle(element);
    const canvas = document.createElement("canvas");
    canvas.width = canvas.height = 1;
    const context = canvas.getContext("2d", { colorSpace: "srgb" })!;
    const rgb = (css: string) => {
      context.clearRect(0, 0, 1, 1);
      context.fillStyle = "#000";
      context.fillStyle = css;
      context.fillRect(0, 0, 1, 1);
      const d = context.getImageData(0, 0, 1, 1).data;
      return `${d[0]},${d[1]},${d[2]}`;
    };
    return {
      fs: s.fontSize,
      fw: s.fontWeight,
      r: s.borderRadius,
      gap: s.gap,
      color: rgb(s.color),
      bg: rgb(s.backgroundColor),
    };
  });
}

function near(actual: number, want: number, label: string) {
  expect(
    Math.abs(actual - want),
    `${label}: ${actual.toFixed(1)} vs ${want}`,
  ).toBeLessThanOrEqual(TOL);
}

function box(actual: Rect, want: Partial<Rect>, label: string) {
  for (const key of ["x", "y", "width", "height"] as const) {
    const value = want[key];
    if (value !== undefined) near(actual[key], value, `${label}.${key}`);
  }
}

// The artboard's text colours: the foreground, the muted text and the quiet hint.
const INK = { light: "22,26,33", dark: "230,233,238" } as const;
const MUTED = { light: "79,88,102", dark: "170,179,192" } as const;
const QUIET = { light: "95,104,117", dark: "143,153,167" } as const;

test.describe("Board settings, General (Final-Settings chung), 1440 px", () => {
  test.skip(
    persona !== "owner" || scenario !== "default" || faults !== "",
    "Needs the default owner world without faults",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  for (const dark of [false, true]) {
    const mode = dark ? "dark" : "light";

    test(`page header, section list and section header (${dark ? "dem" : "sang"})`, async ({
      page,
    }) => {
      const section = await openGeneral(page, dark);
      const main = page.locator("main");

      const header = page.locator("header").first();
      box(await rel(header, main), { y: 0, height: 76.4 }, "header");
      const mark = header.locator("span[aria-hidden='true']").first();
      box(
        await rel(mark, main),
        { x: 24, y: 21.7, width: 34, height: 34 },
        "board mark",
      );
      expect((await style(mark)).r).toBe("9px");
      const title = header.getByRole("heading", { name: "Thiết lập board" });
      box(await rel(title, main), { x: 70, y: 34.1, height: 27.3 }, "title");
      expect(await style(title)).toMatchObject({ fs: "21px", fw: "650" });
      const trail = header.locator("nav").first();
      box(await rel(trail, main), { x: 70, y: 16, height: 18.1 }, "trail");
      expect((await style(trail)).fs).toBe("12.5px");
      const hint = header.getByText(/^Chỉ quản lý board mở được trang này/);
      const h = await rel(hint, main);
      near(h.y, 30, "hint y");
      near(h.height, 17.4, "hint height");
      near(h.x + h.width, 1041.1, "hint right edge");
      expect(await style(hint)).toMatchObject({
        fs: "12px",
        color: QUIET[mode],
      });
      const back = header.getByRole("link", { name: "Về board" });
      box(
        await rel(back, main),
        { x: 1053.1, y: 22.7, width: 106.9, height: 32 },
        "Về board",
      );
      expect(await style(back)).toMatchObject({
        fs: "13px",
        fw: "500",
        r: "7px",
      });

      // The section list: 212 px wide, 34 px buttons 187 px wide with a 7 px radius, 2 px apart.
      const nav = page.getByRole("navigation", { name: "Mục thiết lập" });
      box(
        await rel(nav, main),
        { x: 0, y: 76.4, width: 212, height: 823.6 },
        "section list",
      );
      const names = ["Chung", "Trường dữ liệu", "Chế độ xem", "Nhóm việc"];
      for (const [i, name] of names.entries()) {
        const item = nav.getByRole("button", { name: new RegExp(`^${name}`) });
        box(
          await rel(item, nav),
          { x: 12, y: 16 + 36 * i, width: 187, height: 34 },
          name,
        );
        expect(await style(item)).toMatchObject({
          fs: "13.5px",
          fw: i === 0 ? "600" : "500",
          r: "7px",
          gap: "9px",
        });
      }

      // The section header: 17 px title, 13 px intro, two 32 px buttons 12 px apart.
      const h2 = section.getByRole("heading", { name: "Chung", level: 2 });
      box(await rel(h2, section), { x: 24, y: 20, height: 24.6 }, "h2");
      expect(await style(h2)).toMatchObject({
        fs: "17px",
        fw: "650",
        color: INK[mode],
      });
      const intro = section.getByText(/^Thông tin nhận diện của board/);
      box(await rel(intro, section), { x: 24, y: 46.6, height: 18.8 }, "intro");
      expect(await style(intro)).toMatchObject({
        fs: "13px",
        color: QUIET[mode],
      });
      const cancel = section.getByRole("button", { name: "Huỷ", exact: true });
      box(
        await rel(cancel, section),
        { x: 635.7, y: 20, width: 50.4, height: 32 },
        "Huỷ",
      );
      expect(await style(cancel)).toMatchObject({
        fs: "13px",
        fw: "500",
        r: "7px",
      });
      const save = section.getByRole("button", { name: "Lưu thay đổi" });
      box(
        await rel(save, section),
        { x: 698.1, y: 20, width: 105.9, height: 32 },
        "Lưu thay đổi",
      );
      expect(await style(save)).toMatchObject({
        fs: "13px",
        fw: "600",
        r: "7px",
      });
    });

    test(`the seven rows of the card (${dark ? "dem" : "sang"})`, async ({
      page,
    }) => {
      const section = await openGeneral(page, dark);
      const card = section.locator("div.rounded-\\[10px\\]").first();
      box(
        await rel(card, section),
        { x: 24, y: 79.5, width: 780, height: 477.4 },
        "card",
      );
      expect((await style(card)).r).toBe("10px");

      // Row tops and heights in the artboard: the label column is 190 px, the control column starts
      // 16 px after it and is 540 px wide; a row has 10 px of padding above and below.
      const rows = card.locator(":scope > div");
      await expect(rows).toHaveCount(7);
      const wantRows: Array<[number, number]> = [
        [80.5, 54],
        [134.5, 77],
        [211.5, 51],
        [262.5, 95.8],
        [358.3, 78.4],
        [436.7, 64.2],
        [500.9, 55],
      ];
      for (const [i, [y, height]] of wantRows.entries()) {
        const r = await rel(rows.nth(i), section);
        near(r.x, 25, `row ${i} x`);
        near(r.width, 778, `row ${i} width`);
        near(r.y, y, `row ${i} y`);
        near(r.height, height, `row ${i} height`);
        const label = rows.nth(i).locator("label, span").first();
        const l = await rel(label, section);
        near(l.x, 41, `row ${i} label x`);
        near(l.width, 190, `row ${i} label width`);
        // Rows after the first start under a 1 px rule.
        near(l.y, y + (i === 0 ? 10 : 11), `row ${i} label y`);
        expect(await style(label)).toMatchObject({
          fs: "13px",
          fw: "600",
          color: INK[mode],
        });
      }
      // The two-line labels carry a 12 px sub line under the 13 px name.
      for (const [i, text] of [
        [1, "Hiện ở đầu board"],
        [4, "Tiền tố của mã tự tăng"],
        [5, "Áp dụng cho mọi người xem"],
      ] as const) {
        const sub = rows.nth(i).getByText(text, { exact: true });
        const s = await rel(sub, section);
        near(s.y, wantRows[i]![0] + 11 + 25.8, `row ${i} sub y`);
        near(s.height, 17.4, `row ${i} sub height`);
        expect(await style(sub)).toMatchObject({
          fs: "12px",
          color: QUIET[mode],
        });
      }

      // Name and description: 34 px box and 56 px box, 540 px wide, 7 px radius, 13.5 px text.
      const name = section.getByRole("textbox", {
        name: "Tên board",
        exact: true,
      });
      box(
        await rel(name, section),
        { x: 247, y: 90.5, width: 540, height: 34 },
        "name",
      );
      expect(await style(name)).toMatchObject({
        fs: "13.5px",
        r: "7px",
        color: INK[mode],
      });
      const description = section.getByRole("textbox", { name: /^Mô tả/ });
      box(
        await rel(description, section),
        { x: 247, y: 145.5, width: 540, height: 56 },
        "description",
      );

      // Icon and colour: a 30 px mark (8 px radius), the "Màu" hint, six 22 px swatches 8 px apart.
      const icon = section.getByRole("button", { name: "Đổi biểu tượng" });
      box(
        await rel(icon, section),
        { x: 247, y: 222.5, width: 30, height: 30 },
        "icon button",
      );
      expect((await style(icon)).r).toBe("8px");
      expect((await style(icon)).bg).toBe("30,111,208");
      const colour = rows.nth(2).getByText("Màu", { exact: true });
      const c = await rel(colour, section);
      near(c.x, 285, "Màu x");
      near(c.y, 228.8, "Màu y");
      expect((await style(colour)).fs).toBe("12px");
      const swatches = rows.nth(2).locator("button[aria-pressed=false]");
      await expect(swatches).toHaveCount(5);
      const pressed = rows.nth(2).locator("button[aria-pressed=true]");
      await expect(pressed).toHaveCount(1);
      expect((await style(pressed)).bg).toBe("30,111,208");
      const all = rows.nth(2).locator("button[aria-pressed]");
      for (let i = 0; i < 6; i += 1) {
        box(
          await rel(all.nth(i), section),
          { x: 324 + 30 * i, y: 226.5, width: 22, height: 22 },
          `swatch ${i}`,
        );
        expect((await style(all.nth(i))).r).toBe("6px");
      }

      // Department: the read-only box (345 px) with its 20 px mark, the 32 px button, the hint.
      const department = rows.nth(3).locator("span.box-border").first();
      const d = await rel(department, section);
      near(d.x, 247, "department x");
      near(d.y, 273.5, "department y");
      near(d.height, 34, "department height");
      near(d.width, 345.4, "department width");
      expect(await style(department)).toMatchObject({
        fs: "13.5px",
        r: "7px",
        bg: dark ? "14,17,22" : "247,248,250",
      });
      await expect(department).toContainText("Phòng Công nghệ");
      const deptMark = department.locator("span").first();
      box(
        await rel(deptMark, section),
        { x: 258, y: 280.5, width: 20, height: 20 },
        "department mark",
      );
      expect((await style(deptMark)).r).toBe("6px");
      const move = rows.nth(3).getByRole("button", {
        name: "Chuyển sang phòng khác",
      });
      box(
        await rel(move, section),
        { x: 600.4, y: 274.5, width: 186.6, height: 32 },
        "move button",
      );
      const moveHint = rows.nth(3).getByText(/^Cần quyền Quản lý trên board/);
      box(
        await rel(moveHint, section),
        { x: 247, y: 313.5, width: 540, height: 34.8 },
        "move hint",
      );
      expect(await style(moveHint)).toMatchObject({
        fs: "12px",
        color: QUIET[mode],
      });

      // Item key: a 120 px mono box and its 12 px hint.
      const key = section.getByRole("textbox", { name: /^Mã việc/ });
      box(
        await rel(key, section),
        { x: 247, y: 369.3, width: 120, height: 34 },
        "key",
      );
      expect(await style(key)).toMatchObject({ fs: "13px", r: "7px" });
      const keyHint = rows.nth(4).getByText(/^Đổi mã không đổi mã việc cũ/);
      box(
        await rel(keyHint, section),
        { x: 247, y: 409.3, height: 17.4 },
        "key hint",
      );

      // Time zone and week start: 34 px boxes, 540 px and 200 px wide.
      const zone = rows.nth(5).locator("input, button").first();
      box(
        await rel(zone, section),
        { x: 247, y: 447.7, width: 540, height: 34 },
        "time zone",
      );
      expect((await style(zone)).fs).toBe("13.5px");
      const week = rows.nth(6).locator("input, button").first();
      box(
        await rel(week, section),
        { x: 247, y: 511.9, width: 200, height: 34 },
        "week start",
      );
    });

    test(`the danger zone (${dark ? "dem" : "sang"})`, async ({ page }) => {
      const section = await openGeneral(page, dark);
      const zone = page.getByRole("region", { name: "Vùng nguy hiểm" });
      const z = await rel(zone, section);
      near(z.x, 24, "danger zone x");
      near(z.width, 780, "danger zone width");
      near(z.y, 572.9, "danger zone y");
      expect((await style(zone)).r).toBe("10px");

      // Title: 33.6 px bar (12 px above, 2 px below), 13.5 px, 650.
      const title = zone.getByRole("heading", { name: "Vùng nguy hiểm" });
      box(
        await rel(title, section),
        { x: 25, y: 573.9, height: 33.6 },
        "danger title",
      );
      expect(await style(title)).toMatchObject({ fs: "13.5px", fw: "650" });

      // The archive and delete rows: 12 px of padding, the 19.6 px title and the body lines at 18.75.
      const archiveRow = zone
        .getByText("Lưu trữ board", { exact: true })
        .first()
        .locator("xpath=ancestor::div[contains(@class,'items-center')][1]");
      const deleteRow = zone
        .getByText("Xoá board", { exact: true })
        .first()
        .locator("xpath=ancestor::div[contains(@class,'items-center')][1]");
      for (const [row, label] of [
        [archiveRow, "archive row"],
        [deleteRow, "delete row"],
      ] as const) {
        const r = await rel(row, section);
        near(r.x, 25, `${label} x`);
        near(r.width, 778, `${label} width`);
        const heading = row.locator("b").first();
        const hb = await rel(heading, section);
        near(hb.x, 41, `${label} title x`);
        near(hb.y, r.y + 12, `${label} title y`);
        near(hb.height, 19.6, `${label} title height`);
        expect(await style(heading)).toMatchObject({
          fs: "13.5px",
          fw: "600",
          color: INK[mode],
        });
        const body = row.locator("small").first();
        const bb = await rel(body, section);
        near(bb.y, hb.y + 19.6, `${label} body y`);
        const lines = Math.round(bb.height / 18.75);
        near(bb.height, lines * 18.75, `${label} body height (${lines} lines)`);
        expect(await style(body)).toMatchObject({
          fs: "12.5px",
          color: MUTED[mode],
        });
        // The delete row starts under a 1 px rule.
        const rule = label === "delete row" ? 1 : 0;
        near(r.height, rule + 12 + 19.6 + bb.height + 12, `${label} height`);
      }
      const archive = zone.getByRole("button", {
        name: "Lưu trữ board",
        exact: true,
      });
      const a = await rel(archive, section);
      const ar = await rel(archiveRow, section);
      near(a.x + a.width, 787, "archive button right edge");
      near(a.width, 112.3, "archive button width");
      near(a.height, 32, "archive button height");
      near(a.y + 16, ar.y + ar.height / 2, "archive button centred");
      expect(await style(archive)).toMatchObject({
        fs: "13px",
        fw: "500",
        r: "7px",
        color: dark ? "255,107,129" : "196,35,64",
      });

      // The delete row sits under a 1 px rule: the zone's rows are 1 px apart in the artboard too.
      const dr = await rel(deleteRow, section);
      near(dr.y, ar.y + ar.height, "delete row follows the archive row");

      // The confirmation: label 18.1 px, 6 px gap, 34 px input and the 94.3 px button, 14 px below.
      const confirm = zone.getByText(/^Nhập tên board để xác nhận/);
      const cl = await rel(confirm, section);
      near(cl.x, 41, "confirm label x");
      near(cl.y, dr.y + dr.height, "confirm label y");
      near(cl.height, 18.1, "confirm label height");
      expect(await style(confirm)).toMatchObject({
        fs: "12.5px",
        fw: "600",
        color: MUTED[mode],
      });
      const typed = zone.getByRole("textbox");
      const t = await rel(typed, section);
      near(t.x, 41, "confirm input x");
      near(t.y, cl.y + cl.height + 6, "confirm input y");
      near(t.width, 643.7, "confirm input width");
      near(t.height, 34, "confirm input height");
      const del = zone.getByRole("button", { name: "Xoá board", exact: true });
      const d = await rel(del, section);
      near(d.x + d.width, 787, "delete button right edge");
      near(d.width, 94.3, "delete button width");
      near(d.height, 32, "delete button height");
      await expect(del).toBeDisabled();
      expect(await del.evaluate((e) => getComputedStyle(e).opacity)).toBe(
        "0.5",
      );
      near(z.y + z.height, t.y + 34 + 14 + 1, "danger zone bottom");
    });
  }
});
