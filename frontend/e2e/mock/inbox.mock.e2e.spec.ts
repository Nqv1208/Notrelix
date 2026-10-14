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

  test("lists the notifications newest first with the unread ones marked", async ({
    page,
  }) => {
    await openInbox(page);
    const count = await rows(page).count();
    expect(count).toBeGreaterThan(1);
    // Newest first: the times never increase down the list.
    const times = await rows(page)
      .locator('[data-f="time"]')
      .evaluateAll((nodes) =>
        nodes.map((node) => Date.parse(node.getAttribute("datetime") ?? "")),
      );
    expect([...times].sort((a, b) => b - a)).toEqual(times);
    // At least one is unread (a dot and the unread label) and one is read.
    expect(await page.locator('[data-f="ud"]').count()).toBeGreaterThan(0);
    expect(await page.locator('[data-f="ud"]').count()).toBeLessThan(count);
  });

  test("the notifications are the mockup's, in Vietnamese, three unread today", async ({
    page,
  }) => {
    await openInbox(page);
    await expect(page.locator('[data-f="ud"]')).toHaveCount(3);
    const first = rows(page).first();
    await expect(first).toContainText(
      "Trần Minh Quân gửi bạn duyệt Thiết lập đăng nhập một lần (SSO) cho toàn công ty",
    );
    await expect(first.locator('[data-f="time"]')).toContainText("12 phút");
    await expect(rows(page).nth(1)).toContainText("Lý Thu Trang nhắc tên bạn");
    await expect(rows(page).nth(2)).toContainText("đã quá hạn 2 ngày");
    // Today's three come first, then yesterday's.
    await expect(page.getByText("Hôm nay", { exact: true })).toBeVisible();
    await expect(page.getByText("Hôm qua", { exact: true })).toBeVisible();
  });

  test("the first notification is open in the reading pane", async ({
    page,
  }) => {
    await openInbox(page);
    const first = rows(page).first();
    await expect(first).toHaveAttribute("aria-current", "true");
    const pane = page.getByRole("main", { name: "Chi tiết thông báo" });
    const title = await first.locator('[data-f="what"] b').first().innerText();
    await expect(pane).toContainText(title);
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

  test("the unread toggle narrows the list to unread and back", async ({
    page,
  }) => {
    await openInbox(page);
    const all = await rows(page).count();
    const toggle = page.getByRole("button", { name: "Chỉ chưa đọc" });
    await toggle.click();
    await expect(toggle).toHaveAttribute("aria-pressed", "true");
    const unread = await rows(page).count();
    expect(unread).toBeLessThan(all);
    expect(unread).toBe(await page.locator('[data-f="ud"]').count());
    await toggle.click();
    await expect(rows(page)).toHaveCount(all);
  });

  test("Đã xử lý marks the open notification read through the producer", async ({
    page,
  }) => {
    await openInbox(page);
    const toggle = page.getByRole("button", { name: "Chỉ chưa đọc" });
    await toggle.click();
    const unread = await rows(page).count();
    expect(unread).toBeGreaterThan(0);
    await page.getByRole("button", { name: "Đã xử lý" }).click();
    // The write lands, the list is read again and one fewer is unread.
    await expect(rows(page)).toHaveCount(unread - 1);
  });

  test("E marks the open unread notification read", async ({ page }) => {
    await openInbox(page);
    await page.getByRole("button", { name: "Chỉ chưa đọc" }).click();
    const unread = await rows(page).count();
    await page.keyboard.press("e");
    await expect(rows(page)).toHaveCount(unread - 1);
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
    await page.getByRole("button", { name: "Chỉ chưa đọc" }).click();
    await expect(page.getByText("Không có thông báo chưa đọc")).toBeVisible();
    await page.getByRole("button", { name: "Hiện tất cả" }).click();
    expect(await rows(page).count()).toBeGreaterThan(0);
  });

  test("tabs without a producer are inert with the reason and the scope line is not claimed", async ({
    page,
  }) => {
    await openInbox(page);
    for (const name of ["Cần xử lý", "Đang theo dõi"]) {
      const tab = page.getByRole("tab", { name });
      await expect(tab).toHaveAttribute("aria-disabled", "true");
      await expect(tab).toHaveAttribute("title", /chưa có loại/);
    }
    await expect(page.getByRole("tab", { name: "Tất cả" })).toHaveAttribute(
      "aria-selected",
      "true",
    );
    // The read is not scoped to a workspace: no "Chỉ workspace …" claim.
    await expect(page.getByText(/Chỉ workspace/)).toHaveCount(0);
  });

  test("the Mentions and Assigned tabs filter by the notification type", async ({
    page,
  }) => {
    await openInbox(page);
    const all = await rows(page).count();
    await page.getByRole("tab", { name: /^Nhắc tên/ }).click();
    const mentions = await rows(page).count();
    expect(mentions).toBeGreaterThan(0);
    expect(mentions).toBeLessThan(all);
    for (const text of await rows(page).allInnerTexts()) {
      expect(text).toContain("nhắc tên bạn");
    }
    await page.getByRole("tab", { name: /^Được giao/ }).click();
    for (const text of await rows(page).allInnerTexts()) {
      expect(text).toMatch(/gửi bạn duyệt|giao cho bạn/);
    }
    await page.getByRole("tab", { name: "Tất cả" }).click();
    await expect(rows(page)).toHaveCount(all);
  });

  test("snooze and mute have no producer and say why; open-item links to the item", async ({
    page,
  }) => {
    await openInbox(page);
    for (const name of [/^Nhắc lại sau/, /^Tắt thông báo việc này/]) {
      const button = page.getByRole("button", { name });
      await expect(button).toHaveAttribute("aria-disabled", "true");
      await expect(button).toHaveAttribute("title", /.+/);
      await button.click({ force: true });
    }
    await expect(page).toHaveURL(new RegExp(`${INBOX}$`));
    await page.getByRole("link", { name: "Mở công việc" }).click();
    await expect(page).toHaveURL(
      /\/boards\/mock-board-erp\/items\/mock-erp-item-138/,
    );
  });

  test("the settings icon opens the notification settings page", async ({
    page,
  }) => {
    await openInbox(page);
    await page.getByRole("link", { name: "Cài đặt thông báo" }).click();
    await expect(page).toHaveURL(/\/account\/notifications$/);
  });
});
