import { expect, test, type Page } from "@playwright/test";

/**
 * The card popup does every everyday edit, on the mock world: a file is attached and deleted after a
 * confirm, a label is created with a colour, and a custom property is created in place and given its value.
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

test.describe("Card popup, everyday edits (owner)", () => {
  test.skip(
    persona !== "owner" || scenario !== "default" || faults !== "",
    "Needs the default owner world without faults",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  test("attaches a file, then deletes it after a confirm", async ({ page }) => {
    await open(page);
    const popup = page.getByRole("dialog");
    await popup.getByRole("button", { name: "Thêm đính kèm" }).click();
    await page.locator('input[type="file"]').setInputFiles({
      name: "doi-chieu.txt",
      mimeType: "text/plain",
      buffer: Buffer.from("so lieu"),
    });
    const options = popup.getByRole("button", {
      name: "Tuỳ chọn doi-chieu.txt",
    });
    await expect(options).toBeVisible();
    await options.click();
    await page.getByRole("menuitem", { name: "Xoá", exact: true }).click();
    await expect(page.getByText(/Không hoàn tác được/)).toBeVisible();
    await page.getByRole("button", { name: "Xoá", exact: true }).click();
    await expect(options).toHaveCount(0);
    await expect(
      popup.getByRole("button", { name: "Tải xuống doi-chieu.txt" }),
    ).toHaveCount(0);
  });

  test("creates a label with a colour from the picker", async ({ page }) => {
    await open(page);
    const popup = page.getByRole("dialog");
    await popup
      .getByRole("button", { name: "Nhãn", exact: true })
      .first()
      .click();
    await page.getByRole("button", { name: "Tạo nhãn mới" }).click();
    await page
      .getByRole("textbox", { name: "Tên nhãn" })
      .fill("Kiểm kê cuối kỳ");
    await page.getByRole("button", { name: "Lục", exact: true }).click();
    await page.getByRole("button", { name: "Tạo nhãn", exact: true }).click();
    await expect(popup.getByText("Kiểm kê cuối kỳ").first()).toBeVisible();
  });

  test("creates a property in place and sets its value", async ({ page }) => {
    await open(page);
    const popup = page.getByRole("dialog");
    await popup.getByRole("button", { name: "Thêm trường" }).click();
    await page.getByRole("button", { name: "Thêm thuộc tính" }).click();
    await page.getByRole("button", { name: "Thuộc tính mới" }).click();
    await page.getByRole("textbox", { name: "Tên thuộc tính" }).fill("Ghi chú");
    await page.getByRole("combobox", { name: "Loại" }).selectOption("text");
    await page.getByRole("button", { name: "Tạo thuộc tính" }).click();
    const box = popup.getByRole("textbox", { name: "Ghi chú" });
    await expect(box).toBeVisible();
    await box.fill("Đã khớp");
    await box.press("Enter");
    await expect(box).toHaveValue("Đã khớp");
  });
});
