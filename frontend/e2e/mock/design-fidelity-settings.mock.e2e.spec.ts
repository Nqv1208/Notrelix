import { expect, test, type Locator, type Page } from "@playwright/test";

/**
 * The options panel of a Select field in the board settings against the October 2026 mockup
 * `docs/design/mockups/2026-10/src/Final-Settings.dc.html`, state `truong` (the field panel of "Mức ưu
 * tiên" at the right of the fields list), rendered at 1440x900 with `nav=an` and measured with the box
 * dump. Boxes are read relative to the panel's top-left; the artboard's panel is 401 px wide (400 and
 * its left border), the app's 400, so every width is compared with 1 px of tolerance. Font sizes,
 * weights and radii must be equal.
 *
 * Measured: the header (mark, name, "Lựa chọn · n việc có giá trị"), the form groups (display name,
 * key, type) with their hints, "Các lựa chọn" (the list box, a row with its grip, colour square, name
 * and count, "Thêm lựa chọn") and the footer.
 *
 * Not measured: the groups the artboard draws below the options (default value, required toggle,
 * guidance, "Đang được dùng ở"): the mock world's Select fields have no board column, and the panel
 * shows those groups only for a field that has one. The where-items-go dialog is not drawn.
 *
 * The artboard's name is `Mức ưu tiên` and the mock world's is `Ưu tiên` (the same four options: Khẩn
 * 1, Cao 5, Trung bình 7, Thấp 3 items in the artboard, 1/5/7/3 here), so only boxes that do not
 * follow the name are compared.
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

async function openPanel(page: Page, dark = false) {
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
  await page.goto(`${BOARD}/settings?from=table&section=fields`);
  await page.getByText("Ưu tiên", { exact: true }).first().click();
  const panel = page.getByRole("complementary", { name: /^Sửa trường/ });
  await expect(panel).toBeVisible();
  await page.waitForTimeout(400);
  return panel;
}

async function rel(locator: Locator, origin: Locator): Promise<Rect> {
  const [e, o] = await Promise.all([
    locator.boundingBox(),
    origin.boundingBox(),
  ]);
  if (!e || !o) throw new Error("element is not laid out");
  return {
    x: e.x - o.x,
    y: e.y - o.y,
    width: e.width,
    height: e.height,
  };
}

async function style(locator: Locator) {
  return locator.evaluate((element) => {
    const s = getComputedStyle(element);
    return {
      fs: s.fontSize,
      fw: s.fontWeight,
      r: s.borderRadius,
      gap: s.gap,
      pad: s.padding,
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

test.describe("Board settings, options panel (Final-Settings truong), 1440 px", () => {
  test.skip(
    persona !== "owner" || scenario !== "default" || faults !== "",
    "Needs the default owner world without faults",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  for (const dark of [false, true]) {
    test(`header, form groups, options list and footer (${dark ? "dem" : "sang"})`, async ({
      page,
    }) => {
      const panel = await openPanel(page, dark);

      const panelBox = (await panel.boundingBox())!;
      near(panelBox.width, 401, "panel width");
      near(panelBox.height, 823.6, "panel height");
      near(panelBox.x + panelBox.width, 1440, "panel reaches the window edge");

      // Header: 68.1 px, the 32 px mark (8 px radius), the name and the sub line.
      const mark = panel.locator("span[aria-hidden='true']").first();
      box(
        await rel(mark, panel),
        { x: 19, y: 17.6, width: 32, height: 32 },
        "mark",
      );
      expect((await style(mark)).r).toBe("8px");
      const name = panel.locator("b").first();
      const n = await rel(name, panel);
      near(n.x, 61, "name x");
      // The artboard's line box is 19 px, the app's 21.8 with a 1.45 line height: the text is centred alike.
      near(n.y + n.height / 2, 24.5, "name centre");
      expect(await style(name)).toMatchObject({ fs: "15px", fw: "650" });
      const sub = panel.locator("small").first();
      box(await rel(sub, panel), { x: 61, y: 35.8, height: 17.4 }, "sub");
      expect((await style(sub)).fs).toBe("12px");

      // Form groups: 12.5 px labels, 34 px boxes with a 7 px radius, 12 px hints, 6 px apart.
      const display = panel.getByLabel("Tên hiển thị");
      box(
        await rel(display, panel),
        { x: 19, y: 106.3, width: 364, height: 34 },
        "display name",
      );
      expect(await style(display)).toMatchObject({ fs: "13.5px", r: "7px" });
      const label = panel.getByText("Tên hiển thị", { exact: true });
      box(await rel(label, panel), { x: 19, y: 82.1, height: 18.1 }, "label");
      expect(await style(label)).toMatchObject({ fs: "12.5px", fw: "600" });
      const keyHint = panel.getByText(/^Giữ nguyên khi đổi tên/);
      box(
        await rel(keyHint, panel),
        { x: 19, y: 220.4, width: 364, height: 34.8 },
        "key hint",
      );
      expect((await style(keyHint)).fs).toBe("12px");
      const typeHint = panel.getByText(/^Khi đổi loại/);
      box(await rel(typeHint, panel), { y: 335.3, height: 34.8 }, "type hint");
      const typeLabel = panel.getByText("Loại", { exact: true });
      box(
        await rel(typeLabel, panel),
        { y: 271.2, height: 18.1 },
        "type label",
      );

      // The options: a 184 px box with an 8 px radius holding four rows (36, 37, 37, 37) and the add row (35).
      const options = panel.getByText("Các lựa chọn", { exact: true });
      box(
        await rel(options, panel),
        { y: 386.1, height: 18.1 },
        "options label",
      );
      const list = panel.locator("div.rounded-\\[8px\\]").filter({
        has: page.getByRole("button", { name: "Thêm lựa chọn" }),
      });
      box(
        await rel(list, panel),
        { x: 19, y: 410.2, width: 364, height: 184 },
        "options box",
      );
      expect((await style(list)).r).toBe("8px");

      const rows = list.getByRole("textbox", { name: "Tên lựa chọn" });
      await expect(rows).toHaveCount(4);
      const rowHeights = [36, 37, 37, 37];
      const swatchColours = [
        "rgb(196, 35, 64)",
        "rgb(180, 80, 11)",
        "rgb(30, 123, 224)",
        "rgb(122, 131, 145)",
      ];
      const counts = ["1 việc", "5 việc", "7 việc", "3 việc"];
      for (let i = 0; i < 4; i += 1) {
        const row = list.locator("div.group").nth(i);
        const r = await rel(row, panel);
        near(r.y, 411.2 + [0, 36, 73, 110][i]!, `row ${i} y`);
        near(r.height, rowHeights[i]!, `row ${i} height`);
        expect(await style(row)).toMatchObject({ fs: "13px", gap: "9px" });
        // The grip sits 6 px in from the row, the colour square 9 px after it, the name 9 px after that.
        const grip = row.locator("svg").first();
        const g = await rel(grip, panel);
        near(g.x, 26, `row ${i} grip x`);
        near(g.width, 14, `row ${i} grip width`);
        const square = row.locator("span[style]").first();
        const s = await rel(square, panel);
        near(s.x, 49, `row ${i} square x`);
        near(s.width, 14, `row ${i} square width`);
        near(s.height, 14, `row ${i} square height`);
        expect((await style(square)).r).toBe("4px");
        expect(
          await square.evaluate((e) => getComputedStyle(e).backgroundColor),
        ).toBe(swatchColours[i]);
        const input = row.getByRole("textbox", { name: "Tên lựa chọn" });
        near((await rel(input, panel)).x, 72, `row ${i} name x`);
        const count = row.getByText(counts[i]!, { exact: true });
        const c = await rel(count, panel);
        // The count ends 10 px from the row's right edge (371 / 372 in the 400 / 401 px panel).
        near(c.x + c.width, 372, `row ${i} count right edge`);
        expect(await style(count)).toMatchObject({ fs: "12px" });
      }
      const add = list.getByRole("button", { name: "Thêm lựa chọn" });
      box(
        await rel(add, panel),
        { x: 20, y: 558.2, width: 362, height: 35 },
        "add option",
      );
      expect(await style(add)).toMatchObject({
        fs: "13px",
        fw: "500",
        gap: "7px",
      });
      const orderHint = panel.getByText(/^Thứ tự ở đây/);
      box(
        await rel(orderHint, panel),
        { x: 19, y: 600.2, height: 17.4 },
        "hint",
      );

      // Footer: a 57 px bar under the form with 32 px buttons.
      for (const buttonName of ["Huỷ", "Lưu thay đổi"]) {
        near(
          (await rel(panel.getByRole("button", { name: buttonName }), panel))
            .height,
          32,
          `${buttonName} height`,
        );
      }
      const save = panel.getByRole("button", { name: "Lưu thay đổi" });
      near(
        panelBox.width -
          1 -
          ((await rel(save, panel)).x + (await save.boundingBox())!.width),
        18,
        "Lưu thay đổi: gap to the right edge",
      );
    });
  }

  test("the remove button of a row appears over the count on hover and keeps the row's layout", async ({
    page,
  }) => {
    const panel = await openPanel(page);
    const row = panel.locator("div.group").first();
    const remove = row.getByRole("button", { name: /^Xoá lựa chọn/ });
    const before = await rel(row, panel);
    await row.hover();
    await expect(remove).toBeVisible();
    expect(await rel(row, panel)).toEqual(before);
    near((await rel(remove, panel)).x + 20, 372, "remove right edge");
  });
});
