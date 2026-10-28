import { expect, test, type Page } from "@playwright/test";

/**
 * Board settings on the mock world: the Labels section (create, rename,
 * recolour, delete), creating a label from an item's label picker, the live
 * item count of each status option, and a failed write that keeps what the
 * person typed, says so under the control and offers Retry (the fault run:
 * the mock answers 500 to the group write).
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const groupFault = faults.includes("/board-groups/mock-erp-list-test");
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";

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

test.describe("Board settings (owner)", () => {
  test.skip(
    persona !== "owner" || scenario !== "default" || faults !== "",
    "Needs the default owner world without faults",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  test("labels: creates, renames, recolours and deletes a label", async ({
    page,
  }) => {
    await open(page, `${BOARD}/settings?section=labels`);
    const list = page.getByRole("list", { name: "Nhãn của board" });
    await expect(list.getByText("Dữ liệu").first()).toBeVisible();

    await page.getByRole("button", { name: "Thêm nhãn" }).click();
    const name = page.getByRole("textbox", { name: "Tên nhãn" });
    await expect(name).toBeFocused();
    await name.fill("Nhãn thử");
    await page.getByRole("radio", { name: "Tím" }).click();
    await page.getByRole("button", { name: "Thêm", exact: true }).click();
    await expect(list.getByText("Nhãn thử")).toBeVisible();
    await expect(name).toHaveCount(0);

    await page.getByRole("button", { name: "Đổi tên nhãn Nhãn thử" }).click();
    const rename = page.getByRole("textbox", { name: "Tên nhãn" });
    await rename.fill("Nhãn đã đổi");
    await rename.press("Enter");
    await expect(list.getByText("Nhãn đã đổi")).toBeVisible();
    await expect(list.getByText("Nhãn thử", { exact: true })).toHaveCount(0);

    await page.getByRole("button", { name: "Đổi màu của Nhãn đã đổi" }).click();
    await page.getByRole("button", { name: "Xanh lá" }).click();

    await page.getByRole("button", { name: "Xoá nhãn Nhãn đã đổi" }).click();
    const confirm = page.getByRole("group", {
      name: "Xoá nhãn “Nhãn đã đổi”?",
    });
    await expect(confirm).toContainText("Chưa có việc nào dùng nhãn này.");
    await confirm.getByRole("button", { name: "Xoá nhãn" }).click();
    await expect(list.getByText("Nhãn đã đổi")).toHaveCount(0);
  });

  test("item picker: creates a label from the typed text and puts it on the item", async ({
    page,
  }) => {
    await open(page, `${BOARD}/items/mock-erp-item-142`);
    await page.getByRole("button", { name: "Nhãn", exact: true }).click();
    const search = page.getByRole("textbox", { name: "Tìm hoặc tạo nhãn" });
    await search.fill("Gấp");
    await page.getByRole("button", { name: "Tạo nhãn “Gấp”" }).click();
    await expect(
      page.getByRole("list", { name: "Nhãn" }).getByRole("button", {
        name: "Gấp",
        pressed: true,
      }),
    ).toBeVisible();
  });

  test("options show how many live items hold them", async ({ page }) => {
    await open(
      page,
      `${BOARD}/settings?section=fields&field=mock-erp-col-status`,
    );
    const panel = page.getByRole("complementary").last();
    // Cần làm 8, Đang làm 3, Chờ duyệt 2, Hoàn thành 8 (sub-items included).
    await expect(panel.getByText("8 việc")).toHaveCount(2);
    await expect(panel.getByText("3 việc")).toHaveCount(1);
    await expect(panel.getByText("2 việc")).toHaveCount(1);
  });
});

test.describe("Board settings, failed write (fault run)", () => {
  test.skip(
    persona !== "owner" || !groupFault,
    "Needs VITE_MOCK_FAULTS=/board-groups/mock-erp-list-test=500",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  test("a group rename that fails keeps the typed name, says so and offers Retry", async ({
    page,
  }) => {
    await open(page, `${BOARD}/settings?section=groups`);
    await page.getByRole("button", { name: "Đổi tên nhóm Kiểm thử" }).click();
    const input = page.getByRole("textbox", { name: "Tên nhóm" });
    await input.fill("Kiểm thử lại");
    await input.press("Enter");
    const alert = page.getByRole("alert");
    await expect(alert).toContainText("Chưa lưu được");
    await expect(page.getByRole("textbox", { name: "Tên nhóm" })).toHaveValue(
      "Kiểm thử lại",
    );
    await expect(alert.getByRole("button", { name: "Thử lại" })).toBeVisible();
  });
});
