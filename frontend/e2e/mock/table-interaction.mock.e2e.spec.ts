import { expect, test, type Locator, type Page } from "@playwright/test";

/**
 * Board table behaviour in the mock world: what a person does with the rows
 * and columns (open, hover, scroll sideways, resize). Geometry against the
 * mockup lives in `design-fidelity-table.mock.e2e.spec.ts`; the table is
 * deliberately not the mockup in three places the product owner decided:
 * no "Mở" button on hover, no docked columns, and a per-row left edge in the
 * group's colour that deepens on hover.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const NOW = new Date("2026-10-12T10:40:00+07:00");

async function openTable(page: Page, query = "") {
  await page.clock.setFixedTime(NOW);
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "vi");
    } catch {
      // Private mode: the default locale applies.
    }
  });
  await page.goto(`${BOARD}?view=table${query}`);
  await expect(page.getByRole("rowgroup").first()).toBeVisible();
}

function row(page: Page, key: string) {
  return page.locator("[data-row-id]").filter({ hasText: key }).first();
}

/** A colour property as `[r, g, b, alpha]`, whatever colour space the page uses. */
async function rgba(
  locator: Locator,
  property: string,
): Promise<[number, number, number, number]> {
  return locator.evaluate((element, name) => {
    const canvas = document.createElement("canvas");
    canvas.width = canvas.height = 1;
    const context = canvas.getContext("2d", { willReadFrequently: true })!;
    context.clearRect(0, 0, 1, 1);
    context.fillStyle = getComputedStyle(element).getPropertyValue(name);
    context.fillRect(0, 0, 1, 1);
    const [r, g, b, a] = context.getImageData(0, 0, 1, 1).data;
    return [r!, g!, b!, a! / 255] as [number, number, number, number];
  }, property);
}

test.describe("Board table interaction (owner)", () => {
  test.skip(
    persona !== "owner" || scenario !== "default" || faults !== "",
    "Owner checks need the default owner world without faults",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  test("hover shows no Mở button; a click on the row opens the item", async ({
    page,
  }) => {
    await openTable(page);
    const target = row(page, "ERP-144");
    await target.hover();
    await expect(page.getByRole("button", { name: /^Mở/ })).toHaveCount(0);
    await expect(page.getByText("Mở", { exact: true })).toHaveCount(0);

    // A cell that is not a control opens the item side sheet.
    await target.getByRole("cell").nth(1).click();
    await expect(page).toHaveURL(/item=/);
    await expect(page.getByRole("dialog")).toBeVisible();
    await page.keyboard.press("Escape");
    await expect(page.getByRole("dialog")).toHaveCount(0);

    // Enter on the focused row opens it too.
    await row(page, "ERP-147").focus();
    await page.keyboard.press("Enter");
    await expect(page.getByRole("dialog")).toBeVisible();
  });

  test("the left edge of a row is faint at rest and takes the full group colour on hover", async ({
    page,
  }) => {
    await openTable(page);
    const target = row(page, "ERP-151");
    const edge = target.locator("[data-row-accent]");
    const box = (await edge.boundingBox())!;
    expect(box.width).toBeCloseTo(6, 0);
    const rowBox = (await target.boundingBox())!;
    expect(box.height).toBeCloseTo(rowBox.height, 0);

    const rest = await rgba(edge, "background-color");
    expect(rest[3]).toBeGreaterThan(0.1);
    expect(rest[3]).toBeLessThan(0.35);

    // The cells keep their x positions: the edge sits over the row, not in it.
    const checkbox = target.getByRole("checkbox");
    const checkboxX = (await checkbox.boundingBox())!.x;

    await target.hover();
    await expect
      .poll(async () => (await rgba(edge, "background-color"))[3])
      .toBeGreaterThan(0.95);
    const hover = await rgba(edge, "background-color");
    // Same hue at both alphas (8-bit premultiplied rounding allows 2 units).
    for (const channel of [0, 1, 2] as const) {
      expect(Math.abs(hover[channel] - rest[channel])).toBeLessThanOrEqual(3);
    }
    expect((await checkbox.boundingBox())!.x).toBeCloseTo(checkboxX, 1);

    await page.mouse.move(0, 0);
    await expect
      .poll(async () => (await rgba(edge, "background-color"))[3])
      .toBeLessThan(0.35);
  });

  test("no column is docked: every column scrolls sideways together", async ({
    page,
  }) => {
    await page.setViewportSize({ width: 1000, height: 800 });
    await openTable(page);
    const scroll = page.locator("[data-table-scroll]");
    const target = row(page, "ERP-151");
    const tracked = [
      target.getByRole("checkbox"),
      target.getByRole("cell").nth(1),
      target.getByRole("cell").nth(2),
      page.getByRole("columnheader").nth(0),
      page.getByRole("columnheader").nth(2),
      page.getByRole("rowgroup").first().getByRole("row").first(),
    ];
    const xs = async () =>
      Promise.all(tracked.map(async (l) => (await l.boundingBox())!.x));
    const before = await xs();
    const header = page.getByRole("columnheader").nth(1);
    const headerY = (await header.boundingBox())!.y;

    const moved = await scroll.evaluate((element) => {
      element.scrollLeft = 160;
      element.scrollTop = 40;
      return { left: element.scrollLeft, top: element.scrollTop };
    });
    expect(moved.left).toBeGreaterThan(100);
    const after = await xs();
    for (const [index, x] of before.entries()) {
      // Cells and header cells move with the scroll; the group band (last)
      // spans the scroll width and moves too.
      expect(x - after[index]!, `column ${index}`).toBeCloseTo(moved.left, 0);
    }
    // Only the header row stays: sticky at the top while the rows scroll.
    expect((await header.boundingBox())!.y).toBeCloseTo(headerY, 0);
  });

  test("dragging a header edge resizes the column and the width is saved with the view", async ({
    page,
  }) => {
    await openTable(page);
    const head = page.getByRole("columnheader", { name: /^Tên công việc/ });
    const handle = page.getByRole("separator", {
      name: "Đổi độ rộng cột Tên công việc",
    });
    const widthOf = async (l: Locator) => (await l.boundingBox())!.width;
    expect(await widthOf(head)).toBeCloseTo(340, 0);
    const titleCell = row(page, "ERP-151").getByRole("cell").nth(2);
    expect(await widthOf(titleCell)).toBeCloseTo(340, 0);

    const edge = (await handle.boundingBox())!;
    const x = edge.x + edge.width / 2;
    const y = edge.y + edge.height / 2;
    await page.mouse.move(x, y);
    await expect(handle).toHaveCSS("cursor", "col-resize");
    await page.mouse.down();
    await page.mouse.move(x + 40, y, { steps: 4 });
    // Live: the header and the rows follow before the button is released.
    expect(await widthOf(head)).toBeCloseTo(380, 0);
    expect(await widthOf(titleCell)).toBeCloseTo(380, 0);
    await page.mouse.up();
    await expect(handle).toHaveAttribute("aria-valuenow", "380");
    // The mock backend answers inside the page and keeps views in memory per
    // page load, so a reload cannot show persistence. The saved width is read
    // back from the view by leaving the table and returning to it.
    await page.getByRole("tab", { name: "Kanban" }).click();
    await expect(page.getByRole("tab", { name: "Kanban" })).toHaveAttribute(
      "aria-selected",
      "true",
    );
    await page.getByRole("tab", { name: "Table" }).click();
    await expect(
      page.getByRole("separator", { name: "Đổi độ rộng cột Tên công việc" }),
    ).toHaveAttribute("aria-valuenow", "380");

    // Keyboard alternative and reset.
    const again = page.getByRole("separator", {
      name: "Đổi độ rộng cột Tên công việc",
    });
    await again.focus();
    await page.keyboard.press("ArrowLeft");
    await expect(again).toHaveAttribute("aria-valuenow", "372");
    await again.dblclick();
    await expect(again).toHaveAttribute("aria-valuenow", "340");
  });

  test("adding a status creates a group in the table and a lane in the Kanban", async ({
    page,
  }) => {
    await openTable(page);
    const groups = page.getByRole("rowgroup");
    await expect(groups).toHaveCount(3);
    const control = page.getByRole("button", { name: "Thêm trạng thái" });
    // A quiet full-width row under the last group.
    const last = (await groups.last().boundingBox())!;
    const box = (await control.boundingBox())!;
    expect(box.y).toBeGreaterThanOrEqual(last.y + last.height - 1);
    expect(box.height).toBeCloseTo(36, 0);
    const grid = (await page.locator("[data-table-scroll]").boundingBox())!;
    // A compact control at the table's left edge, not a full-width row.
    expect(box.x - grid.x).toBeLessThan(64);
    expect(box.width).toBeLessThan(grid.width / 2);

    await control.click();
    await page
      .getByRole("textbox", { name: "Tên trạng thái" })
      .fill("Kiểm thử");
    await page.getByRole("radio", { name: "Hoàn thành" }).click();
    await page.getByRole("button", { name: "Thêm trạng thái" }).last().click();
    const added = page.getByRole("rowgroup", { name: "Kiểm thử" });
    await expect(added).toBeVisible();
    await expect(groups).toHaveCount(4);
    await expect(groups.last()).toHaveAccessibleName("Kiểm thử");

    await page.getByRole("tab", { name: "Kanban" }).click();
    await expect(
      page
        .getByRole("region", { name: /Kiểm thử/ })
        .or(page.getByText("Kiểm thử", { exact: true }))
        .first(),
    ).toBeVisible();
  });
});
