import { expect, test, type Page } from "@playwright/test";

/**
 * The automation rules of a board on the mock world (owner persona = board
 * manager): a draft in its editor, the trigger and action pickers, turning a
 * rule on, a rule that cannot be turned on until a field is restored, the
 * schedule of a running rule, and the run log with one failed run. The mock
 * world resets on every page load.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const SETTINGS =
  "/workspaces/mock-workspace-primary/boards/mock-board-erp/settings";
const NOW = new Date("2026-10-12T10:40:00+07:00");

async function open(page: Page, query: string) {
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.clock.setFixedTime(NOW);
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "vi");
      window.localStorage.setItem("notrelix-system-rail", "pinned");
    } catch {
      // Private mode: the defaults apply.
    }
  });
  await page.goto(`${SETTINGS}?section=automations${query}`);
}

const name = (page: Page) => page.getByRole("textbox", { name: "Tên quy tắc" });

test.describe("Board automations (owner)", () => {
  test.skip(
    persona !== "owner" || scenario !== "default" || faults !== "",
    "Owner checks need the default owner world without faults",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  test("creates a draft from the list and finds it under Nháp", async ({
    page,
  }) => {
    await open(page, "");
    await page.getByRole("button", { name: "Tạo quy tắc" }).click();
    await expect(page).toHaveURL(/rule=sa-new-1/);
    await expect(name(page)).toHaveValue("Quy tắc mới");
    await expect(
      page.getByRole("button", { name: "Bật quy tắc", exact: true }),
    ).toBeDisabled();
    await page.getByRole("link", { name: "Tự động hoá" }).first().click();
    await expect(
      page.getByRole("article", { name: "Quy tắc mới" }),
    ).toBeVisible();
  });

  test("edits a draft, picks an event and an action, and turns it on", async ({
    page,
  }) => {
    await open(page, "&rule=sa-draft");
    await expect(name(page)).toHaveValue(
      "Báo kế toán khi việc Kế toán hoàn thành",
    );
    await expect(page.getByText("Nháp", { exact: true }).first()).toBeVisible();

    await page.getByRole("button", { name: /^Trạng thái thay đổi/ }).click();
    const events = page.getByRole("dialog", { name: "Chọn sự kiện kích hoạt" });
    await events
      .getByRole("textbox", { name: "Tìm sự kiện" })
      .fill("bình luận");
    await events.getByRole("button", { name: /Có bình luận mới/ }).click();
    await expect(
      page.getByRole("button", { name: "Có bình luận mới" }).first(),
    ).toBeVisible();

    await page.getByRole("button", { name: "Thêm hành động" }).click();
    const actions = page.getByRole("dialog", { name: "Chọn hành động" });
    await actions.getByRole("button", { name: /Lưu trữ việc/ }).click();
    await expect(
      page.getByRole("button", { name: "Thao tác với bước 2" }),
    ).toBeVisible();

    await page.getByRole("button", { name: "Lưu nháp" }).click();
    await expect(
      page.getByText('Sự kiện "Có bình luận mới" hợp lệ'),
    ).toBeVisible();

    await page
      .getByRole("button", { name: "Bật quy tắc", exact: true })
      .click();
    await expect(
      page.getByRole("switch", {
        name: "Bật quy tắc Báo kế toán khi việc Kế toán hoàn thành",
      }),
    ).toHaveAttribute("aria-checked", "true");
    await expect(page.getByText("Đang bật", { exact: true })).toBeVisible();
    await expect(page.getByText(/^Phiên bản 1 ·/)).toBeVisible();
  });

  test("a rule that writes into an archived field turns on after the field is restored", async ({
    page,
  }) => {
    await open(page, "&rule=sa-urgent-copy");
    const enable = page.getByRole("button", {
      name: "Bật quy tắc",
      exact: true,
    });
    await expect(enable).toBeDisabled();
    await expect(
      page.getByText("Không bật được", { exact: true }),
    ).toBeVisible();
    await expect(page.getByRole("alert").first()).toContainText(
      "Ngày bàn giao đã được lưu trữ",
    );
    await page
      .getByRole("complementary", { name: "Thông tin chạy của quy tắc" })
      .getByRole("button", { name: "Khôi phục trường" })
      .click();
    await expect(enable).toBeEnabled();
    await enable.click();
    await expect(page.getByText("Đang bật", { exact: true })).toBeVisible();
  });

  test("a running rule keeps Save disabled until it changes, then saves a version", async ({
    page,
  }) => {
    await open(page, "&rule=sa-review");
    const save = page.getByRole("button", {
      name: "Lưu thành phiên bản 4",
    });
    await expect(save).toBeDisabled();
    await name(page).fill("Gửi duyệt khi chờ duyệt");
    await expect(save).toBeEnabled();
    await save.click();
    await expect(page.getByText(/^Phiên bản 4 ·/)).toBeVisible();
    await expect(
      page.getByRole("button", { name: "Xem phiên bản 3" }),
    ).toBeVisible();
  });

  test("a schedule rule edits its hour and lists its recent runs", async ({
    page,
  }) => {
    await open(page, "&rule=sa-overdue");
    await expect(page.getByText("Lần chạy tới:")).toBeVisible();
    await expect(
      page.getByText("09:00 hôm nay", { exact: true }),
    ).toBeVisible();
    await page.getByLabel("Giờ", { exact: true }).fill("08:30");
    await page.getByRole("button", { name: "Lưu thành phiên bản 3" }).click();
    await expect(page.getByText(/lúc 08:30 hằng ngày/)).toBeVisible();
    await page.getByRole("button", { name: "Mở nhật ký chạy" }).click();
    await expect(page).toHaveURL(/log=true/);
  });

  test("the run log filters by result and opens the failed run with how to fix it", async ({
    page,
  }) => {
    await open(page, "&log=true");
    const table = page.getByRole("table", {
      name: "Các lần chạy, mới nhất trước",
    });
    await expect(table.getByRole("row")).toHaveCount(10);
    await page.getByRole("tab", { name: /^Lỗi/ }).click();
    await expect(table.getByRole("row")).toHaveCount(2);
    await page
      .getByRole("button", { name: /Xem chi tiết lần chạy của Khẩn/ })
      .click();
    await expect(page).toHaveURL(/run=run-4/);
    const sheet = page.getByRole("dialog", { name: "Khẩn → báo Trưởng phòng" });
    await expect(sheet).toBeVisible();
    await expect(
      sheet.getByText("Kết nối Microsoft Teams cần uỷ quyền lại", {
        exact: false,
      }),
    ).toBeVisible();
    await expect(
      sheet.getByRole("button", { name: "Chạy lại bước lỗi" }),
    ).toBeDisabled();
    await page.keyboard.press("Escape");
    await expect(sheet).toBeHidden();
    await expect(page).not.toHaveURL(/run=/);
  });

  test("works at 1280 px and in the dark theme without horizontal overflow", async ({
    page,
  }) => {
    await page.addInitScript(() => {
      try {
        window.localStorage.setItem("notrelix-theme-mode", "dark");
      } catch {
        // Private mode: the light theme applies.
      }
    });
    for (const query of ["&rule=sa-overdue", "&log=true&run=run-4"]) {
      await open(page, query);
      await page.setViewportSize({ width: 1280, height: 800 });
      await expect(page.locator("html")).toHaveClass(/dark/);
      const overflow = await page.evaluate(
        () =>
          document.documentElement.scrollWidth -
          document.documentElement.clientWidth,
      );
      expect(overflow).toBeLessThanOrEqual(0);
    }
  });
});
