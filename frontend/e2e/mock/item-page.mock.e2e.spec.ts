import { expect, test, type Page } from "@playwright/test";

/**
 * The item page on the mock world: reached from the Kanban sheet's "Mở toàn
 * trang", folds back to the board with the sheet open, steps through the lane
 * with K and J, keeps the activity tab in the URL, posts a comment and says
 * when the item is not there. The Table sheet has no page of its own to open
 * yet and keeps the button disabled with the reason. The page clock is fixed to
 * the fixtures' 12/10/2026 10:40. The owner checks run for the default persona.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const ITEM = `${BOARD}/items/mock-erp-item-142`;
const NOW = new Date("2026-10-12T10:40:00+07:00");
const ERP_142 = "Chuyển dữ liệu khách hàng từ hệ thống cũ sang ERP";

async function open(page: Page, path: string) {
  await page.clock.setFixedTime(NOW);
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "vi");
    } catch {
      // Private mode: the default locale applies.
    }
  });
  await page.goto(path);
}

function trackErrors(page: Page): string[] {
  const errors: string[] = [];
  page.on("pageerror", (error) => errors.push(error.message));
  page.on("console", (message) => {
    if (message.text().includes("MockUnhandledOperationError")) {
      errors.push(message.text());
    }
  });
  return errors;
}

test.describe("Item page", () => {
  test.use({ viewport: { width: 1440, height: 900 } });
  test.skip(
    persona !== "owner" || scenario !== "default",
    "Needs the default owner scenario",
  );

  test("opens from the Kanban sheet and folds back to the board with the sheet open", async ({
    page,
  }) => {
    const errors = trackErrors(page);
    await open(page, `${BOARD}?view=kanban&item=mock-erp-item-142`);
    const sheet = page.getByRole("dialog", { name: ERP_142 });
    await expect(sheet).toBeVisible();

    await sheet.getByRole("button", { name: "Mở toàn trang" }).click();
    await expect(page).toHaveURL(
      /\/boards\/mock-board-erp\/items\/mock-erp-item-142/,
    );
    await expect(
      page.getByRole("heading", { level: 1, name: ERP_142 }),
    ).toBeVisible();
    await expect(page.getByRole("dialog")).toHaveCount(0);
    // The sidebar still shows the board the item belongs to.
    await expect(
      page.getByRole("link", { name: "Triển khai ERP Q4" }).first(),
    ).toBeVisible();

    await page.getByRole("button", { name: "Thu về bảng bên" }).click();
    await expect(page).toHaveURL(
      /\/boards\/mock-board-erp\?.*item=mock-erp-item-142/,
    );
    await expect(page.getByRole("dialog", { name: ERP_142 })).toBeVisible();
    expect(errors).toEqual([]);
  });

  test("goes back to the board with the arrow", async ({ page }) => {
    await open(page, ITEM);
    await expect(page.getByRole("heading", { level: 1 })).toBeVisible();
    await page.getByRole("link", { name: "Về board" }).click();
    await expect(page).toHaveURL(/\/boards\/mock-board-erp(\?|$)/);
    await expect(page.getByRole("dialog")).toHaveCount(0);
  });

  test("steps to the next item of its lane with J and the button", async ({
    page,
  }) => {
    await open(page, ITEM);
    await expect(
      page.getByRole("heading", { level: 1, name: ERP_142 }),
    ).toBeVisible();
    await expect(page.getByText("Việc 1/3 trong Đang làm")).toBeVisible();
    // First of its lane: no previous.
    await expect(
      page.getByRole("button", { name: /^Không có việc trước/ }),
    ).toHaveAttribute("aria-disabled", "true");

    await page.keyboard.press("j");
    await expect(page.getByText("Việc 2/3 trong Đang làm")).toBeVisible();
    await expect(page).toHaveURL(/items\/mock-erp-item-/);
    await expect(page).not.toHaveURL(/mock-erp-item-142/);
    await page.getByRole("button", { name: "Công việc trước (K)" }).click();
    await expect(
      page.getByRole("heading", { level: 1, name: ERP_142 }),
    ).toBeVisible();
    // K and J stepped in place: Back leaves the page, not the last item.
    await page.goBack();
    await expect(page).not.toHaveURL(/items\//);
  });

  test("keeps the activity tab in the URL and posts a comment", async ({
    page,
  }) => {
    await open(page, ITEM);
    const tabs = page.getByRole("navigation", { name: "Lọc hoạt động" });
    await tabs.getByRole("link", { name: /^Lịch sử/ }).click();
    await expect(page).toHaveURL(/tab=history/);
    await expect(tabs.getByRole("link", { name: /^Lịch sử/ })).toHaveAttribute(
      "aria-current",
      "page",
    );
    await expect(page.getByLabel("Viết bình luận")).toHaveCount(0);

    await page.goto(`${ITEM}?tab=comments`);
    await expect(
      tabs.getByRole("link", { name: /^Bình luận/ }),
    ).toHaveAttribute("aria-current", "page");
    await page.getByLabel("Viết bình luận").fill("Đã nhận file đối chiếu");
    await page.getByRole("button", { name: "Gửi", exact: true }).click();
    await expect(page.getByText("Đã nhận file đối chiếu")).toBeVisible();
    await expect(
      tabs.getByRole("link", { name: /^Bình luận 5/ }),
    ).toBeVisible();
  });

  test("changes the status from the properties and shows it in the lane position", async ({
    page,
  }) => {
    await open(page, ITEM);
    await page.getByRole("button", { name: "Trạng thái" }).click();
    await page.getByRole("menuitem", { name: "Chờ duyệt" }).click();
    await expect(
      page.getByRole("button", { name: "Trạng thái" }),
    ).toContainText("Chờ duyệt");
    await expect(page.getByText(/trong Chờ duyệt$/)).toBeVisible();
  });

  test("says the item is not there and goes back", async ({ page }) => {
    await open(page, `${BOARD}/items/mock-erp-item-does-not-exist`);
    await expect(page.getByText("Không mở được công việc")).toBeVisible();
    await page.getByRole("button", { name: "Về board" }).click();
    await expect(page).toHaveURL(/\/boards\/mock-board-erp(\?|$)/);
  });

  test("the Table sheet keeps the full page button disabled with the reason", async ({
    page,
  }) => {
    await open(page, `${BOARD}?view=table&item=mock-erp-item-142`);
    const sheet = page.getByRole("dialog", { name: ERP_142 });
    await expect(sheet).toBeVisible();
    const button = sheet.getByRole("button", { name: "Mở toàn trang" });
    await expect(button).toHaveAttribute("aria-disabled", "true");
    await expect(sheet.getByText("Công việc chưa có trang riêng")).toHaveCount(
      1,
    );
  });
});
