import { expect, test, type Locator, type Page } from "@playwright/test";

/**
 * The fields list of the board settings against the October 2026 mockup `Final-Settings` state `truong`
 * (rendered at 1440x900 with `nav=an`, measured with the box dump): the list column beside the options panel.
 * Boxes are read relative to the settings content area (the panel's parent). The list column is 571 px wide
 * (the panel is 401), so every x is the artboard's.
 *
 * Measured: title and Add button, the section line ("Trường của board", count, the drag note), the list box
 * and its header row, one field row (grip, 26 px mark, name, type, key in 12 px, usage), the system group
 * and its lock cell.
 *
 * Not measured: the artboard's row data (its eleven fields, per-field view usage "5 view", the rules count and
 * the key names): the mock world has 21 fields with generated keys and the page has no per-field view usage
 * source, so rows are compared on their layout (offsets inside the row), not on their text. The page draws no
 * tab bar until a field has been deleted.
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

async function open(page: Page, dark: boolean) {
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
  return panel.locator("xpath=..");
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
    return { fs: s.fontSize, fw: s.fontWeight, r: s.borderRadius };
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

test.describe("Board settings, fields list (Final-Settings truong), 1440 px", () => {
  test.skip(
    persona !== "owner" || scenario !== "default" || faults !== "",
    "Needs the default owner world without faults",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  for (const dark of [false, true]) {
    test(`title, section lines, list header, a field row and the system group (${dark ? "dem" : "sang"})`, async ({
      page,
    }) => {
      const content = await open(page, dark);
      const column = page.locator("section[aria-labelledby='bs-fields-h']");
      box(await rel(column, content), { x: 0, y: 0, width: 571 }, "column");
      await expect(page.getByRole("tablist")).toHaveCount(0);

      const title = page.getByRole("heading", { name: "Trường dữ liệu" });
      box(await rel(title, content), { x: 24, y: 20, height: 24.6 }, "title");
      expect(await style(title)).toMatchObject({ fs: "17px", fw: "650" });
      const add = page.getByRole("button", { name: "Thêm trường" });
      box(
        await rel(add, content),
        { x: 416.6, y: 20, width: 130.4, height: 32 },
        "add button",
      );
      expect(await style(add)).toMatchObject({ fs: "13px", r: "7px" });

      // Section line: 12.5 px, 18.1 px high at y 121.2, the note flush right at 547.
      const line = page.getByText("Kéo để đổi thứ tự mặc định", {
        exact: false,
      });
      const lineBox = await rel(line, content);
      box(lineBox, { y: 121.2, height: 18.1 }, "section note");
      near(lineBox.x + lineBox.width, 547, "section note right edge");

      const list = page.getByRole("list", { name: "Trường của board" });
      const l = await rel(list, content);
      box(l, { x: 24, y: 145.3, width: 523 }, "list box");
      expect((await style(list)).r).toBe("10px");

      // Header: 34.8 px, 12 px labels in 17.4 px lines, columns at 41, 104, 264 and 406 inside the list.
      const head = list.locator("div[aria-hidden='true']").first();
      box(
        await rel(head, list),
        { x: 1, y: 1, width: 521, height: 34.8 },
        "header",
      );
      for (const [name, x, w] of [
        ["Loại", 104, 150],
        ["Khoá", 264, 132],
        ["Đang dùng", 406, 104],
      ] as const) {
        const cell = head.getByText(name, { exact: true });
        box(
          await rel(cell, list),
          { x, y: 9.7, width: w, height: 17.4 },
          `header ${name}`,
        );
        expect(await style(cell)).toMatchObject({ fs: "12px", fw: "600" });
      }

      // A field row (the artboard's "Ước lượng" row, 45 px): grip at 8, mark and name at 40, type at 103, key at
      // 263 with a 17.4 px line, usage at 405.
      const row = list.getByRole("listitem").filter({
        has: page.getByRole("button", { name: "Sửa trường Ước lượng (giờ)" }),
      });
      const r = await rel(row, list);
      box(r, { x: 1, width: 521, height: 45 }, "row");
      const inRow = async (locator: Locator) => {
        const b = await rel(locator, row);
        return b;
      };
      const grip = row.getByRole("button", { name: /^Kéo để sắp xếp/ });
      const g = await inRow(grip);
      near(g.x, 8, "grip x");
      near(g.y + g.height / 2, 22.5, "grip centre");
      const name = row.getByRole("button", { name: /^Sửa trường/ });
      const n = await inRow(name);
      near(n.x, 40, "name x");
      near(n.y + n.height / 2, 22.5, "name centre");
      const mark = name.locator("span").first();
      box(await inRow(mark), { x: 40, width: 26, height: 26 }, "mark");
      expect((await style(mark)).r).toBe("6px");
      expect(await style(name)).toMatchObject({ fs: "13px", fw: "500" });
      const key = row.getByText("uoc-luong", { exact: true });
      const k = await inRow(key);
      box(k, { x: 263, width: 132, height: 17.4 }, "key");
      near(k.y + k.height / 2, 22.5, "key centre");
      expect(await style(key)).toMatchObject({ fs: "12px" });

      // System group: the lock cell of its first row sits at 405 in a 17.4 px line.
      const system = page
        .getByText("Trường hệ thống")
        .locator("xpath=following-sibling::div[1]");
      const lock = system
        .getByRole("listitem")
        .first()
        .locator("xpath=./span[last()]");
      const lk = await rel(lock, system);
      near(lk.x - 1, 405, "lock x");
      near(lk.height, 17.4, "lock height");
    });
  }
});
