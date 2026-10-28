import { expect, test, type Page } from "@playwright/test";

/**
 * The item side sheet does every everyday edit, on the mock world: a file is
 * attached and deleted after a confirm, a label is created with a colour, and
 * a custom property is created in place and given its value.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";

async function open(page: Page) {
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "vi");
    } catch {
      // Private mode: the default applies.
    }
  });
  await page.goto(`${BOARD}?item=mock-erp-item-142`);
  await expect(page.getByRole("dialog")).toBeVisible();
}

test.describe("Item side sheet, everyday edits (owner)", () => {
  test.skip(
    persona !== "owner" || scenario !== "default" || faults !== "",
    "Needs the default owner world without faults",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  test("attaches a file, then deletes it after a confirm", async ({ page }) => {
    await open(page);
    const sheet = page.getByRole("dialog");
    await sheet.locator('input[type="file"]').setInputFiles({
      name: "doi-chieu.txt",
      mimeType: "text/plain",
      buffer: Buffer.from("so lieu"),
    });
    // The history also names the file, so the row is found by its actions.
    const remove = sheet.getByRole("button", { name: "Xoá doi-chieu.txt" });
    await remove.click();
    await expect(sheet.getByText(/Không hoàn tác được/)).toBeVisible();
    await sheet.getByRole("button", { name: "Xoá tệp" }).click();
    await expect(remove).toHaveCount(0);
    await expect(
      sheet.getByRole("button", { name: "Tải doi-chieu.txt" }),
    ).toHaveCount(0);
  });

  test("creates a label with a colour from the picker", async ({ page }) => {
    await open(page);
    const sheet = page.getByRole("dialog");
    await sheet.getByRole("button", { name: /Nhãn/ }).first().click();
    await page.getByRole("button", { name: "Tạo nhãn", exact: true }).click();
    await page
      .getByRole("textbox", { name: "Tên nhãn" })
      .fill("Kiểm kê cuối kỳ");
    await page.getByRole("radio", { name: "Lục" }).click();
    await page.getByRole("button", { name: "Tạo và gắn" }).click();
    await expect(sheet.getByText("Kiểm kê cuối kỳ").first()).toBeVisible();
  });

  test("creates a property in place and sets its value", async ({ page }) => {
    await open(page);
    const sheet = page.getByRole("dialog");
    await sheet.getByRole("button", { name: "Thêm thuộc tính" }).click();
    await page.getByRole("button", { name: "Thuộc tính mới" }).click();
    await page.getByRole("textbox", { name: "Tên thuộc tính" }).fill("Ghi chú");
    await page.getByRole("combobox", { name: "Loại" }).selectOption("text");
    await page.getByRole("button", { name: "Tạo thuộc tính" }).click();
    const box = page.getByRole("textbox", { name: "Sửa Ghi chú" });
    await expect(box).toBeVisible();
    await box.fill("Đã khớp");
    await box.press("Enter");
    await expect(sheet.getByText("Ghi chú: Đã khớp")).toBeVisible();
  });
});
