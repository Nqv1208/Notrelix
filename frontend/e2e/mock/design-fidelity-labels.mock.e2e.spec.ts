import { expect, test, type Locator, type Page } from "@playwright/test";

/**
 * The Labels section of the board settings against the October 2026 mockup `WM-Labels` (the settings card:
 * the add row opened in place, the label list with its header, a row in edit, a row asking to be deleted),
 * rendered at 1440x1000 and measured with the box dump, light and dark.
 *
 * The artboard is a 948 px miniature of the page (its card is 677 px wide, the page's 780), so widths that
 * follow the page are not compared; everything inside a row or the add card is: heights, offsets from the
 * card edge, the 26 px swatches, 22 px chips in 12 px type, 30 px icon buttons, 28 px small buttons.
 *
 * Not measured: the item label popover on the right of the artboard (see the item popover spec) and the
 * 390 px layout (screenshot review).
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const TOL = 1;

interface Rect {
  readonly x: number;
  readonly y: number;
  readonly width: number;
  readonly height: number;
}

async function open(page: Page, dark: boolean) {
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
  await page.goto(`${BOARD}/settings?from=table&section=labels`);
  await expect(
    page.getByRole("list", { name: "Nhãn của board" }),
  ).toBeVisible();
  await page.waitForTimeout(300);
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

test.describe("Board settings, labels (WM-Labels), 1440 px", () => {
  test.skip(
    persona !== "owner" || scenario !== "default" || faults !== "",
    "Needs the default owner world without faults",
  );
  test.use({ viewport: { width: 1440, height: 1000 } });

  for (const dark of [false, true]) {
    test(`add row, list header, rows, a row in edit and the delete question (${dark ? "dem" : "sang"})`, async ({
      page,
    }) => {
      await open(page, dark);
      const mode = dark ? "dem" : "sang";

      // The add row opens in place: a 12 px card, the caption, "Tên", a 260 px input, six 26 px swatches.
      await page.getByRole("button", { name: "Thêm nhãn" }).click();
      const form = page.getByRole("form", { name: "Nhãn mới" });
      await expect(form).toBeVisible();
      expect((await style(form)).r).toBe("12px");
      const caption = form.getByText("Nhãn mới", { exact: true });
      box(
        await rel(caption, form),
        { x: 15, y: 15, height: 17.4 },
        `${mode} caption`,
      );
      expect(await style(caption)).toMatchObject({ fs: "12px", fw: "600" });
      const input = form.getByRole("textbox", { name: "Tên nhãn" });
      box(
        await rel(input, form),
        { x: 91, y: 44.4, width: 260, height: 34 },
        "name input",
      );
      await input.fill("Mới");
      const swatches = form.getByRole("radio");
      await expect(swatches).toHaveCount(6);
      for (let i = 0; i < 6; i += 1) {
        const b = await rel(swatches.nth(i), form);
        near(b.width, 26, `swatch ${i} width`);
        near(b.height, 26, `swatch ${i} height`);
        if (i > 0)
          near(
            b.x - (await rel(swatches.nth(i - 1), form)).x,
            32,
            `swatch ${i} pitch`,
          );
        expect((await style(swatches.nth(i))).r).toBe("6px");
      }
      const preview = form.locator("span").filter({ hasText: /^Mới$/ });
      box(await rel(preview, form), { height: 22 }, "preview chip");
      expect(await style(preview)).toMatchObject({
        fs: "12px",
        fw: "600",
        r: "6px",
      });
      for (const [name, height] of [
        ["Huỷ", 32],
        ["Thêm nhãn", 32],
      ] as const) {
        box(
          await rel(form.getByRole("button", { name }), form),
          { height },
          name,
        );
      }
      await form.getByRole("button", { name: "Huỷ" }).click();

      // The list card: 12 px radius, a 34 px header (Nhãn, Đang gắn), 47 px rows.
      const list = page.getByRole("list", { name: "Nhãn của board" });
      const card = list.locator("xpath=..");
      expect((await style(card)).r).toBe("12px");
      const head = card.locator("div[aria-hidden='true']").first();
      box(await rel(head, card), { x: 1, y: 1, height: 34 }, "header");
      box(
        await rel(head.getByText("Nhãn", { exact: true }), card),
        { x: 13, height: 17.4 },
        "header Nhãn",
      );
      const rows = list.locator("> li");
      const first = rows.first();
      box(await rel(first, card), { y: 35, height: 47 }, "row");
      const chip = first.locator("span span").first();
      box(await rel(chip, first), { x: 12, height: 22 }, "chip");
      expect(await style(chip)).toMatchObject({
        fs: "12px",
        fw: "600",
        r: "6px",
      });
      const count = first.getByText(/ việc$/);
      box(await rel(count, first), { height: 18.1 }, "count");
      expect((await style(count)).fs).toBe("12.5px");
      const edit = first.getByRole("button", { name: /^Đổi tên nhãn/ });
      const del = first.getByRole("button", { name: /^Xoá nhãn/ });
      box(await rel(edit, first), { width: 30, height: 30 }, "edit button");
      box(await rel(del, first), { width: 30, height: 30 }, "delete button");
      const firstBox = (await first.boundingBox())!;
      const delBox = (await del.boundingBox())!;
      near(
        firstBox.x + firstBox.width - (delBox.x + delBox.width),
        12,
        "delete button right gap",
      );

      // A row in edit: 53 px, a 220 px input, the 26 px colour square 8 px after it, Huỷ and Lưu 28 px.
      await rows
        .nth(2)
        .getByRole("button", { name: /^Đổi tên nhãn/ })
        .click();
      const editing = rows.nth(2);
      box(await rel(editing, card), { height: 53 }, "editing row");
      const name = editing.getByRole("textbox", { name: "Tên nhãn" });
      box(
        await rel(name, editing),
        { x: 12, width: 220, height: 34 },
        "editing input",
      );
      const colour = editing.getByRole("button", { name: /^Đổi màu/ });
      box(
        await rel(colour, editing),
        { width: 26, height: 26 },
        "colour square",
      );
      near((await rel(colour, editing)).x - 232, 8, "colour square gap");
      for (const label of ["Huỷ", "Lưu"]) {
        const b = editing.getByRole("button", { name: label, exact: true });
        box(await rel(b, editing), { height: 28 }, `${label} height`);
        expect((await style(b)).fs).toBe("12.5px");
      }
      await editing.getByRole("button", { name: "Huỷ", exact: true }).click();

      // The delete question: the row turns 107 px, the question sits in a 10 px radius box 54 px high.
      await rows
        .nth(3)
        .getByRole("button", { name: /^Xoá nhãn/ })
        .click();
      const asking = rows.nth(3);
      box(await rel(asking, card), { height: 107 }, "delete row");
      const question = asking.getByRole("alertdialog");
      box(await rel(question, asking), { x: 12, height: 54 }, "question box");
      expect((await style(question)).r).toBe("10px");
      for (const label of ["Giữ lại", "Xoá nhãn"]) {
        box(
          await rel(question.getByRole("button", { name: label }), question),
          { height: 28 },
          `${label} height`,
        );
      }
    });
  }
});
