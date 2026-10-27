import { expect, test, type Page } from "@playwright/test";
import { closeSync, openSync, ftruncateSync, mkdtempSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";

/**
 * The Detail page in the states the States mockup lists, on the mock world:
 * not found (by key), a new item with nothing on it, a done item, an archived
 * item, a very long one, the property pickers (Status with its undo notice,
 * Assignee with who sees the board, the day picker), the @mention list, the
 * offline notice and the files being sent. Geometry of the Detail page itself
 * lives in `design-fidelity-detail.mock.e2e.spec.ts`.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp/items";
const NOW = new Date("2026-10-12T10:40:00+07:00");

async function open(page: Page, itemId: string) {
  await page.clock.setFixedTime(NOW);
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "vi");
    } catch {
      // Private mode: the default locale applies.
    }
  });
  await page.goto(`${BOARD}/${itemId}`);
}

test.describe("Item page states (owner)", () => {
  test.skip(
    persona !== "owner" || scenario !== "default" || faults !== "",
    "Needs the default owner world without faults",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  test("an item that is not there is named by its key, and the way out is search or home", async ({
    page,
  }) => {
    await open(page, "ERP-171");
    await expect(
      page.getByRole("heading", { level: 1, name: "Không tìm thấy ERP-171" }),
    ).toBeVisible();
    await expect(
      page.getByRole("button", { name: "Tìm kiếm", exact: true }),
    ).toBeVisible();
    await page.getByRole("button", { name: "Về trang chủ" }).click();
    await expect(page).toHaveURL(/\/workspaces\/mock-workspace-primary\/?$/);
  });

  test("a new item says what to do next and always offers its dates", async ({
    page,
  }) => {
    await open(page, "mock-erp-item-154");
    await expect(page.getByRole("heading", { level: 1 })).toContainText(
      "Kiểm tra quyền xem báo cáo thuế",
    );
    await expect(
      page.getByText("Thêm mô tả…", { exact: true }).first(),
    ).toBeVisible();
    // Sub-items: an empty list that says so and offers a way to add one.
    await expect(page.getByText("Chưa có việc con.")).toBeVisible();
    await expect(page.getByText("vừa xong", { exact: false })).toHaveCount(0);
    const rail = page.getByRole("complementary");
    // The due and start dates are always shown, empty or not, so a new item
    // can be given its dates straight away; the other empty fields fold.
    await expect(
      rail.getByRole("button", { name: "Hạn chót", exact: true }),
    ).toBeVisible();
    await expect(
      page.getByRole("button", { name: "Thêm việc con" }),
    ).toBeVisible();
  });

  test("a done item says when it was finished and how early, and offers to reopen it", async ({
    page,
  }) => {
    await open(page, "mock-erp-item-131");
    const banner = page.locator("[data-item-banner=completed]");
    await expect(banner).toContainText("Hoàn thành ngày 05/10");
    await expect(
      page.getByRole("complementary").getByText("xong sớm 1 ngày"),
    ).toBeVisible();
    // Reopening returns the item to the status it had before it was completed.
    await expect(banner.getByRole("button", { name: "Mở lại" })).toBeVisible();
  });

  test("an archived item is read-only and comes back with Khôi phục", async ({
    page,
  }) => {
    await open(page, "mock-erp-item-118");
    const banner = page.locator("[data-item-banner=archived]");
    await expect(banner).toContainText("Đã lưu trữ ngày 08/10");
    await expect(banner).toContainText("Chỉ xem, không sửa");
    await expect(
      page.getByRole("textbox", { name: "Viết bình luận" }),
    ).toBeDisabled();
    await expect(
      page.getByRole("complementary").getByRole("button", {
        name: "Trạng thái",
      }),
    ).toHaveAttribute("aria-disabled", "true");
    await banner.getByRole("button", { name: "Khôi phục" }).click();
    await expect(banner).toHaveCount(0);
    await expect(
      page.getByRole("textbox", { name: "Viết bình luận" }),
    ).toBeEnabled();
  });

  test("a long item folds its description and shows its sub-item progress without a list", async ({
    page,
  }) => {
    await open(page, "mock-erp-item-160");
    await expect(page.getByText("12/48", { exact: true })).toHaveCount(0);
    await expect(
      page.getByRole("button", { name: "Xem toàn bộ mô tả" }),
    ).toBeVisible();
    await expect(
      page.getByRole("button", { name: "Hiện 40 việc con còn lại" }),
    ).toHaveCount(0);
    await expect(page.getByRole("checkbox")).toHaveCount(0);
  });

  test("Status: type to filter, Enter chooses, and the notice undoes it", async ({
    page,
  }) => {
    await open(page, "mock-erp-item-142");
    const status = page
      .getByRole("complementary")
      .getByRole("button", { name: "Trạng thái" });
    await status.click();
    const search = page.getByRole("combobox", { name: "Đổi trạng thái" });
    await search.fill("duy");
    await search.press("Enter");
    await expect(status).toContainText("Chờ duyệt");
    const notice = page.locator("[data-sonner-toast]").filter({
      hasText: "Đã chuyển",
    });
    await expect(notice).toContainText("Đã chuyển ERP-142 sang Chờ duyệt");
    await notice.getByRole("button", { name: /Hoàn tác/ }).click();
    await expect(status).toContainText("Đang làm");
  });

  test("S opens the Status picker and a number chooses", async ({ page }) => {
    await open(page, "mock-erp-item-142");
    await expect(page.getByRole("heading", { level: 1 })).toBeVisible();
    await page.locator("body").press("s");
    const search = page.getByRole("combobox", { name: "Đổi trạng thái" });
    await expect(search).toBeFocused();
    await search.press("4");
    await expect(
      page.getByRole("complementary").getByRole("button", {
        name: "Trạng thái",
      }),
    ).toContainText("Hoàn thành");
  });

  test("Assignee: people of the board first, the others say they cannot see the board", async ({
    page,
  }) => {
    await open(page, "mock-erp-item-142");
    await page
      .getByRole("complementary")
      .getByRole("button", { name: "Người phụ trách" })
      .click();
    const dialog = page.getByRole("dialog");
    await expect(dialog.getByText("Thành viên board")).toBeVisible();
    await expect(dialog.getByText("Nguyễn Thu Hà")).toBeVisible();
    await expect(dialog.getByText("Chưa vào board")).toBeVisible();
    await expect(dialog.getByText("Alex Rivera (Admin)")).toBeVisible();
    // Adding a person to the board is the board's share dialog, not this list.
    await expect(dialog.getByText("Nhờ quản lý board mời")).toHaveCount(3);
    await expect(page.getByRole("button", { name: "Mời vào" })).toHaveCount(0);
  });

  test("Due: quick chips and the month with the span from the start date", async ({
    page,
  }) => {
    await open(page, "mock-erp-item-142");
    const due = page
      .getByRole("complementary")
      .getByRole("button", { name: "Hạn chót", exact: true });
    await due.click();
    await expect(page.getByText("Tháng 10, 2026")).toBeVisible();
    await expect(page.getByRole("gridcell", { selected: true })).toHaveText(
      "10",
    );
    await page.getByRole("button", { name: "Ngày mai" }).click();
    await expect(due).toContainText("13/10");
  });

  test("@ suggests only people who can see the board: someone outside it is not offered", async ({
    page,
  }) => {
    await open(page, "mock-erp-item-142");
    const field = page.getByRole("textbox", { name: "Viết bình luận" });
    await field.scrollIntoViewIfNeeded();
    await field.click();
    // The author is the only person on this board, and Jordan Lee cannot see
    // it: a mention of them would not notify, so nobody is suggested.
    await field.pressSequentially("Chị @Jo");
    await expect(field).toHaveValue("Chị @Jo");
    await expect(
      page.getByRole("listbox", { name: "Người và nhóm để nhắc tên" }),
    ).toHaveCount(0);
  });

  test("offline: the notice counts what waits, and goes when the network is back", async ({
    page,
    context,
  }) => {
    await open(page, "mock-erp-item-142");
    await expect(page.getByRole("heading", { level: 1 })).toBeVisible();
    await context.setOffline(true);
    const notice = page.locator("[data-item-banner=offline]");
    await expect(notice).toContainText("Mất kết nối.");
    await page
      .getByRole("complementary")
      .getByRole("button", { name: "Trạng thái" })
      .click();
    await page.getByRole("option", { name: /Chờ duyệt/ }).click();
    await expect(notice).toContainText("1 thay đổi đang chờ");
    await expect(
      page.getByRole("complementary").getByText("chờ lưu"),
    ).toBeVisible();
    await context.setOffline(false);
    await expect(notice).toHaveCount(0);
  });

  test("files: one over the limit is refused with the reason, the other is sent", async ({
    page,
  }) => {
    const directory = mkdtempSync(join(tmpdir(), "notrelix-upload-"));
    const big = join(directory, "du-lieu-goc-48k.zip");
    const fd = openSync(big, "w");
    ftruncateSync(fd, 101 * 1024 * 1024);
    closeSync(fd);
    await open(page, "mock-erp-item-142");
    await page.locator("input[type=file][hidden]").first().setInputFiles(big);
    const row = page.locator("[data-upload-status=error]");
    await expect(row).toContainText("Vượt giới hạn 100 MB mỗi tệp");
    await row.getByRole("button", { name: "Bỏ qua" }).click();
    await expect(row).toHaveCount(0);
    await page
      .locator("input[type=file][hidden]")
      .first()
      .setInputFiles({
        name: "loi-gop-trung.png",
        mimeType: "image/png",
        buffer: Buffer.alloc(840 * 1024, 2),
      });
    await expect(page.getByText("loi-gop-trung.png")).toBeVisible();
  });
});

test.describe("Item page states (commenter)", () => {
  test.skip(persona !== "commenter", "Needs the commenter persona");
  test.use({ viewport: { width: 1440, height: 900 } });

  test("someone who cannot edit cannot reopen or restore", async ({ page }) => {
    await open(page, "mock-erp-item-131");
    const reopen = page
      .locator("[data-item-banner=completed]")
      .getByRole("button", { name: "Mở lại" });
    await expect(reopen).toHaveAttribute("aria-disabled", "true");
    await open(page, "mock-erp-item-118");
    await expect(page.locator("[data-item-banner=archived]")).toContainText(
      "Chỉ xem, không sửa",
    );
    await expect(
      page
        .locator("[data-item-banner=archived]")
        .getByRole("button", { name: "Khôi phục" }),
    ).toHaveCount(0);
  });
});
