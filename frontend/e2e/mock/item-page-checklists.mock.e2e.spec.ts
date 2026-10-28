import { expect, test, type Page } from "@playwright/test";

/**
 * The Checklist section of an item on the mock world, on the full page and in
 * the side sheet (one component): the empty row, a list, items added with
 * Enter, ticking, progress, editing in place, delete with undo, keyboard
 * reorder, converting an item to a sub-item and the read-only view of a
 * viewer. ERP-142 starts without a checklist.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const ITEM = `${BOARD}/items/mock-erp-item-142`;

async function open(page: Page, path = ITEM) {
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "vi");
    } catch {
      // Private mode: the default locale applies.
    }
  });
  await page.goto(path);
  await expect(page.getByRole("heading", { level: 1 })).toContainText(
    "Chuyển dữ liệu khách hàng",
    { timeout: 20_000 },
  );
}

async function addList(page: Page, name: string) {
  await page.getByRole("button", { name: "Thêm checklist" }).click();
  const input = page.getByRole("textbox", { name: "Tên checklist" });
  await expect(input).toBeFocused();
  await input.fill(name);
  await input.press("Enter");
  await expect(page.getByRole("group", { name })).toBeVisible();
}

test.describe("Checklists (owner)", () => {
  test.skip(
    persona !== "owner" || scenario !== "default" || faults !== "",
    "Needs the default owner world without faults",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  test("item page: add a list and items with Enter, tick, edit, undo a delete", async ({
    page,
  }) => {
    await open(page);
    const section = page.getByRole("region", { name: "Checklist" });
    await expect(
      section.getByRole("button", { name: "Thêm checklist" }),
    ).toBeVisible();

    await addList(page, "Chuẩn bị go-live");
    const list = page.getByRole("group", { name: "Chuẩn bị go-live" });
    await list.getByRole("button", { name: "Thêm mục" }).click();
    const item = list.getByRole("textbox", { name: "Mục checklist" });
    for (const text of ["Sao lưu dữ liệu", "Chạy thử UAT", "Báo phòng ban"]) {
      await item.fill(text);
      await item.press("Enter");
      await expect(list.getByText(text, { exact: true })).toBeVisible();
    }
    await item.press("Escape");
    await expect(item).toHaveCount(0);

    // Tick with Space on the focused checkbox: progress reads 1/3.
    const box = list.getByRole("checkbox", {
      name: "Đánh dấu xong: Sao lưu dữ liệu",
    });
    await box.focus();
    await box.press("Space");
    await expect(
      list.getByRole("progressbar", { name: "Xong 1 trên 3" }),
    ).toBeVisible();
    await expect(
      list.getByRole("checkbox", { name: "Bỏ đánh dấu: Sao lưu dữ liệu" }),
    ).toHaveAttribute("aria-checked", "true");

    // Edit in place: Enter saves.
    await list
      .getByRole("button", { name: "Tuỳ chọn mục Chạy thử UAT" })
      .press("Enter");
    await page.getByRole("menuitem", { name: "Sửa" }).click();
    const edit = list.getByRole("textbox", { name: "Mục checklist" });
    await edit.fill("Chạy thử UAT vòng 2");
    await edit.press("Enter");
    await expect(list.getByText("Chạy thử UAT vòng 2")).toBeVisible();

    // Delete, then take it back.
    await list
      .getByRole("button", { name: "Tuỳ chọn mục Báo phòng ban" })
      .press("Enter");
    await page.getByRole("menuitem", { name: "Xoá", exact: true }).click();
    await expect(list.getByText("Báo phòng ban")).toHaveCount(0);
    await section.getByRole("button", { name: "Hoàn tác" }).click();
    await expect(list.getByText("Báo phòng ban")).toBeVisible();
  });

  test("keyboard reorder of items", async ({ page }) => {
    await open(page);
    await addList(page, "Thứ tự");
    const list = page.getByRole("group", { name: "Thứ tự" });
    await list.getByRole("button", { name: "Thêm mục" }).click();
    const input = list.getByRole("textbox", { name: "Mục checklist" });
    for (const text of ["Một", "Hai", "Ba"]) {
      await input.fill(text);
      await input.press("Enter");
      await expect(list.getByText(text, { exact: true })).toBeVisible();
    }
    await input.press("Escape");

    const handle = list.getByRole("button", { name: "Di chuyển mục Một" });
    await handle.focus();
    // The sensor measures between key presses.
    await page.keyboard.press("Space");
    await page.waitForTimeout(200);
    await page.keyboard.press("ArrowDown");
    await page.waitForTimeout(200);
    await page.keyboard.press("Space");
    const rows = list.getByRole("listitem");
    await expect(rows.nth(0)).toContainText("Hai");
    await expect(rows.nth(1)).toContainText("Một");
  });

  test("convert an item to a sub-item", async ({ page }) => {
    await open(page);
    await addList(page, "Việc cần tách");
    const list = page.getByRole("group", { name: "Việc cần tách" });
    await list.getByRole("button", { name: "Thêm mục" }).click();
    const input = list.getByRole("textbox", { name: "Mục checklist" });
    await input.fill("Dọn dữ liệu trùng");
    await input.press("Enter");
    await expect(list.getByText("Dọn dữ liệu trùng")).toBeVisible();
    await input.press("Escape");

    await list
      .getByRole("button", { name: "Tuỳ chọn mục Dọn dữ liệu trùng" })
      .press("Enter");
    await page.getByRole("menuitem", { name: "Chuyển thành việc con" }).click();
    await expect(list.getByText("Dọn dữ liệu trùng")).toHaveCount(0);
    await expect(
      page
        .getByRole("region", { name: /Việc con/ })
        .getByText("Dọn dữ liệu trùng"),
    ).toBeVisible();
  });

  test("side sheet: the Checklist section sits between Mô tả and Việc con", async ({
    page,
  }) => {
    await page.addInitScript(() => {
      try {
        window.localStorage.setItem("notrelix-locale", "vi");
      } catch {
        // Private mode: the default locale applies.
      }
    });
    await page.goto(`${BOARD}?item=mock-erp-item-142`);
    const sheet = page.getByRole("dialog");
    await expect(
      sheet.getByRole("button", { name: "Thêm checklist" }),
    ).toBeVisible({ timeout: 20_000 });
    const headings = await sheet
      .getByRole("heading", { level: 3 })
      .allTextContents();
    const at = (label: RegExp) => headings.findIndex((h) => label.test(h));
    expect(at(/^Mô tả/)).toBeLessThan(at(/^Checklist/));
    expect(at(/^Checklist/)).toBeLessThan(at(/^Việc con/));
    await sheet.getByRole("button", { name: "Thêm checklist" }).click();
    const name = sheet.getByRole("textbox", { name: "Tên checklist" });
    await name.fill("Trong sheet");
    await name.press("Enter");
    await expect(
      sheet.getByRole("group", { name: "Trong sheet" }),
    ).toBeVisible();
  });
});

test.describe("Checklists (viewer)", () => {
  test.skip(
    persona !== "viewer" || scenario !== "default",
    "Needs the default viewer scenario",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  test("read-only: no add row", async ({ page }) => {
    await open(page);
    const section = page.getByRole("region", { name: "Checklist" });
    await expect(section.getByText("Chưa có checklist.")).toBeVisible();
    await expect(
      section.getByRole("button", { name: "Thêm checklist" }),
    ).toHaveCount(0);
  });
});
