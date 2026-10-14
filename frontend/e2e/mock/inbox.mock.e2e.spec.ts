import { expect, test, type Page } from "@playwright/test";

/**
 * Inbox (Final-Inbox) behaviour on the mock backend: the member's
 * notifications by day, the selection and the reading pane, the unread
 * toggle, "Đã xử lý" and "Đánh dấu tất cả đã đọc" (the real mark-read
 * writes), the keyboard, and the controls without a producer. The mock world
 * has the mockup's notifications for the signed-in member (three unread of
 * today, the rest read and older) and the page clock is the mockup's, so
 * their times are stable.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const INBOX = "/workspaces/mock-workspace-primary/inbox";
const NOW = new Date("2026-10-12T10:40:00+07:00");

async function openInbox(page: Page) {
  await page.clock.setFixedTime(NOW);
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "vi");
    } catch {
      // Private mode: the default locale applies.
    }
  });
  await page.goto(INBOX);
  await expect(
    page.getByRole("heading", { level: 1, name: "Hộp thư" }),
  ).toBeVisible();
  await expect(
    page.locator('[data-f="items"] [data-f="it"]').first(),
  ).toBeVisible();
}

const rows = (page: Page) => page.locator('[data-f="items"] [data-f="it"]');

test.describe("Inbox (owner)", () => {
  test.skip(
    persona !== "owner" || scenario !== "default" || faults !== "",
    "Needs the default owner world without faults",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  test("lists the need tab: three unread of today, newest first", async ({
    page,
  }) => {
    await openInbox(page);
    await expect(rows(page)).toHaveCount(3);
    await expect(page.locator('[data-f="ud"]')).toHaveCount(3);
    const times = await rows(page)
      .locator('[data-f="time"]')
      .evaluateAll((nodes) =>
        nodes.map((node) => Date.parse(node.getAttribute("datetime") ?? "")),
      );
    expect([...times].sort((a, b) => b - a)).toEqual(times);
  });

  test("the notifications are the mockup's, in Vietnamese, with their counts", async ({
    page,
  }) => {
    await openInbox(page);
    const first = rows(page).first();
    await expect(first).toContainText(
      "Trần Minh Quân gửi bạn duyệt Thiết lập đăng nhập một lần (SSO) cho toàn công ty",
    );
    await expect(first.locator('[data-f="time"]')).toContainText("12 phút");
    await expect(rows(page).nth(1)).toContainText("Lý Thu Trang nhắc tên bạn");
    await expect(rows(page).nth(2)).toContainText("đã quá hạn 2 ngày");
    const tabs = page.getByRole("tablist", { name: "Loại thông báo" });
    await expect(tabs.getByRole("tab", { name: /^Cần xử lý/ })).toContainText(
      "3",
    );
    await expect(tabs.getByRole("tab", { name: /^Nhắc tên/ })).toContainText(
      "1",
    );
    await expect(tabs.getByRole("tab", { name: /^Được giao/ })).toContainText(
      "1",
    );
    await expect(page.getByText(/Chỉ workspace Minh An Group/)).toBeVisible();
    await tabs.getByRole("tab", { name: "Tất cả" }).click();
    await expect(page.getByText("Hôm nay", { exact: true })).toBeVisible();
    await expect(page.getByText("Hôm qua", { exact: true })).toBeVisible();
    await expect(page.getByText("6 thay đổi", { exact: true })).toBeVisible();
  });

  test("the first notification is open in the reading pane with its card", async ({
    page,
  }) => {
    await openInbox(page);
    const first = rows(page).first();
    await expect(first).toHaveAttribute("aria-current", "true");
    const pane = page.getByRole("main", { name: "Chi tiết thông báo" });
    await expect(pane).toContainText(
      "Trần Minh Quân gửi bạn duyệt cấu hình đăng nhập một lần.",
    );
    await expect(pane).toContainText("bạn là người duyệt duy nhất");
    await expect(pane).toContainText("Microsoft Entra ID");
    await expect(pane).toContainText("Đã thử với 5 tài khoản phòng Kế toán");
    await expect(pane.getByRole("button", { name: "Duyệt" })).toBeVisible();
  });

  test("selecting a notification opens it, and J, K and the buttons move", async ({
    page,
  }) => {
    await openInbox(page);
    await rows(page).nth(1).click();
    await expect(rows(page).nth(1)).toHaveAttribute("aria-current", "true");
    await page.keyboard.press("k");
    await expect(rows(page).nth(0)).toHaveAttribute("aria-current", "true");
    await page.keyboard.press("j");
    await expect(rows(page).nth(1)).toHaveAttribute("aria-current", "true");
    await page.getByRole("button", { name: "Thông báo trước (K)" }).click();
    await expect(rows(page).nth(0)).toHaveAttribute("aria-current", "true");
    await page.getByRole("button", { name: "Thông báo sau (J)" }).click();
    await expect(rows(page).nth(1)).toHaveAttribute("aria-current", "true");
  });

  test("the unread toggle narrows the All tab to unread and back", async ({
    page,
  }) => {
    await openInbox(page);
    await page.getByRole("tab", { name: "Tất cả" }).click();
    const all = await rows(page).count();
    expect(all).toBeGreaterThan(3);
    const toggle = page.getByRole("button", { name: "Chỉ chưa đọc" });
    await toggle.click();
    await expect(toggle).toHaveAttribute("aria-pressed", "true");
    await expect(rows(page)).toHaveCount(3);
    await toggle.click();
    await expect(rows(page)).toHaveCount(all);
  });

  test("Đã xử lý hands the entry over: the list, the counts and the badge drop", async ({
    page,
  }) => {
    await openInbox(page);
    await expect(
      page.getByRole("navigation").getByText("3", { exact: true }).first(),
    ).toBeVisible();
    await page.getByRole("button", { name: "Đã xử lý" }).click();
    await expect(rows(page)).toHaveCount(2);
    await expect(page.getByRole("tab", { name: /^Cần xử lý/ })).toContainText(
      "2",
    );
    await expect(page.getByText("Đã đánh dấu đã xử lý.")).toBeVisible();
  });

  test("E handles the open notification", async ({ page }) => {
    await openInbox(page);
    await page.keyboard.press("e");
    await expect(rows(page)).toHaveCount(2);
  });

  test("Duyệt approves the item, clears the entry and says so", async ({
    page,
  }) => {
    await openInbox(page);
    await page.getByRole("button", { name: "Duyệt", exact: true }).click();
    await expect(page.getByText("Đã duyệt ERP-138.")).toBeVisible();
    await expect(rows(page)).toHaveCount(2);
  });

  test("Yêu cầu sửa sends the item back and clears the entry", async ({
    page,
  }) => {
    await openInbox(page);
    await page.getByRole("button", { name: "Yêu cầu sửa" }).click();
    await expect(page.getByText("Đã yêu cầu sửa ERP-138.")).toBeVisible();
    await expect(rows(page)).toHaveCount(2);
  });

  test("the reply box posts to the thread", async ({ page }) => {
    await openInbox(page);
    await rows(page).nth(1).click();
    await page
      .getByRole("textbox", { name: "Trả lời" })
      .fill("Em đồng ý đề xuất 3 ngày.");
    await page.getByRole("button", { name: "Gửi", exact: true }).click();
    await expect(
      page.getByText("Đã gửi trả lời cho Lý Thu Trang."),
    ).toBeVisible();
    await expect(rows(page)).toHaveCount(2);
  });

  test("Nhắc lại sau hides the entry; H opens the same menu", async ({
    page,
  }) => {
    await openInbox(page);
    await page.keyboard.press("h");
    const menu = page.getByRole("menu", { name: "Nhắc lại sau" });
    await expect(menu).toBeVisible();
    await menu.getByRole("menuitem", { name: /1 giờ nữa/ }).click();
    await expect(page.getByText(/Sẽ nhắc lại/)).toBeVisible();
    await expect(rows(page)).toHaveCount(2);
    await expect(page.getByRole("tab", { name: /^Cần xử lý/ })).toContainText(
      "2",
    );
  });

  test("Tắt thông báo việc này mutes it", async ({ page }) => {
    await openInbox(page);
    await page.getByRole("button", { name: "Tắt thông báo việc này" }).click();
    await expect(page.getByText("Đã tắt thông báo việc này.")).toBeVisible();
    await expect(rows(page)).toHaveCount(2);
  });

  test("Đánh dấu tất cả đã đọc empties the unread list and says so", async ({
    page,
  }) => {
    await openInbox(page);
    await page.getByRole("button", { name: "Đánh dấu tất cả đã đọc" }).click();
    await expect(
      page.getByText("Đã đánh dấu đã đọc mọi thông báo."),
    ).toBeVisible();
    await expect(page.locator('[data-f="ud"]')).toHaveCount(0);
    await page.getByRole("tab", { name: "Tất cả" }).click();
    await page.getByRole("button", { name: "Chỉ chưa đọc" }).click();
    await expect(page.getByText("Không có thông báo chưa đọc")).toBeVisible();
    await page.getByRole("button", { name: "Hiện tất cả" }).click();
    expect(await rows(page).count()).toBeGreaterThan(0);
  });

  test("the Mentions, Assigned and Following tabs filter by type", async ({
    page,
  }) => {
    await openInbox(page);
    await page.getByRole("tab", { name: /^Nhắc tên/ }).click();
    await page.getByRole("button", { name: "Chỉ chưa đọc" }).click();
    for (const text of await rows(page).allInnerTexts()) {
      expect(text).toContain("nhắc tên bạn");
    }
    await page.getByRole("tab", { name: /^Được giao/ }).click();
    // The one assignment is already read: show the read ones too.
    await page.getByRole("button", { name: "Chỉ chưa đọc" }).click();
    await expect(rows(page)).toHaveCount(1);
    await expect(rows(page).first()).toContainText("giao cho bạn");
    await page.getByRole("tab", { name: /^Đang theo dõi/ }).click();
    await expect(rows(page).first()).toContainText("đã quá hạn");
  });

  test("a bundle opens its changes", async ({ page }) => {
    await openInbox(page);
    await page.getByRole("tab", { name: "Tất cả" }).click();
    await page.getByRole("button", { name: /Mở 6 thay đổi/ }).click();
    await expect(page.locator('[data-f="children"] li')).toHaveCount(6);
    await page.getByRole("button", { name: /Ẩn 6 thay đổi/ }).click();
    await expect(page.locator('[data-f="children"]')).toHaveCount(0);
  });

  test("open-item links to the item", async ({ page }) => {
    await openInbox(page);
    await page.getByRole("link", { name: "Mở công việc" }).click();
    await expect(page).toHaveURL(/\/boards\/mock-board-erp\?.*item=/);
  });

  test("the settings icon opens the notification settings page", async ({
    page,
  }) => {
    await openInbox(page);
    await page.getByRole("link", { name: "Cài đặt thông báo" }).click();
    await expect(page).toHaveURL(/\/account\/notifications$/);
  });
});
