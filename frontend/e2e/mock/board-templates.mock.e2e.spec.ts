import { expect, test, type Page } from "@playwright/test";

/**
 * Board templates on the mock world: saving a board as a template from Board
 * settings (a draft), publishing it, then starting a new board from it in the
 * create-board dialog; and the built-in starters offered with no template saved.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const WORKSPACE = "/workspaces/mock-workspace-primary";
const BOARD = `${WORKSPACE}/boards/mock-board-erp`;

async function open(page: Page, path: string) {
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "vi");
    } catch {
      // Private mode: the default locale applies.
    }
  });
  await page.goto(path);
}

test.describe("Board templates (owner)", () => {
  test.skip(
    persona !== "owner" || scenario !== "default" || faults !== "",
    "Needs the default owner world without faults",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  test("saves a board as a draft template and publishes it", async ({
    page,
  }) => {
    await open(page, `${BOARD}/settings?section=templates`);
    await expect(page.getByText("Workspace chưa có mẫu nào.")).toBeVisible();

    await page.getByRole("button", { name: "Lưu board này thành mẫu" }).click();
    const form = page.getByRole("form");
    await form.getByLabel("Tên mẫu").fill("Triển khai ERP");
    await form.getByRole("button", { name: "Lưu mẫu" }).click();
    await expect(page.getByRole("status")).toContainText("Triển khai ERP");

    const list = page.getByRole("list", { name: "Mẫu của workspace" });
    await expect(list.getByText("Bản nháp")).toBeVisible();
    await page
      .getByRole("button", { name: "Xuất bản mẫu Triển khai ERP" })
      .click();
    await expect(list.getByText("Đã xuất bản")).toBeVisible();
  });

  test("the new-board dialog offers the starters and creates a board from the chosen one", async ({
    page,
  }) => {
    await open(page, `${WORKSPACE}/library`);
    await page.getByRole("button", { name: "Bảng mới", exact: true }).click();
    const dialog = page.getByRole("dialog", { name: "Bảng mới" });
    await expect(
      dialog.getByRole("radio", { name: /Bảng trống/ }),
    ).toHaveAttribute("aria-checked", "true");
    await expect(dialog.getByText(/Workspace chưa có mẫu nào/)).toBeVisible();
    await dialog.getByRole("radio", { name: /Theo dõi dự án/ }).click();
    await dialog.getByLabel("Tên Bảng").fill("Lộ trình quý 4");
    await dialog.getByRole("button", { name: "Tạo Bảng" }).click();
    await expect(page).toHaveURL(/\/boards\//);
  });
});
