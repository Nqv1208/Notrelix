import { expect, test, type Page } from "@playwright/test";

/**
 * Approval decisions on the item page and the Table's title edit in place, on
 * the mock world (owner persona, Vietnamese). The mock is one signed-in member
 * per run: the owner is the named approver of the seeded request on ERP-138
 * (approve, reject with a reason) and the requester of one they ask for
 * themselves on ERP-142 (cancel).
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const NOW = new Date("2026-10-12T10:40:00+07:00");

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

const approval = (page: Page) =>
  page.getByRole("region", { name: "Phê duyệt" });

test.describe("Item approval and Table title edit (owner)", () => {
  test.skip(
    persona !== "owner" || scenario !== "default" || faults !== "",
    "Needs the default owner world without faults",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  test("the named approver approves the request waiting for them", async ({
    page,
  }) => {
    await open(page, `${BOARD}/items/mock-erp-item-138`);
    await expect(approval(page)).toBeVisible();
    await expect(approval(page).getByText(/nhờ bạn duyệt/)).toBeVisible();
    // Not the requester: no Cancel.
    await expect(
      approval(page).getByRole("button", { name: "Hủy yêu cầu" }),
    ).toHaveCount(0);
    await approval(page).getByRole("button", { name: "Duyệt" }).click();
    await expect(approval(page).getByText(/đã duyệt/)).toBeVisible();
    await expect(
      approval(page).getByRole("button", { name: "Duyệt" }),
    ).toHaveCount(0);
  });

  test("the named approver rejects with a reason that everyone then reads", async ({
    page,
  }) => {
    await open(page, `${BOARD}/items/mock-erp-item-138`);
    await approval(page).getByRole("button", { name: "Từ chối" }).click();
    await approval(page)
      .getByRole("textbox", { name: "Lý do từ chối (không bắt buộc)" })
      .fill("Thiếu phạm vi áp dụng");
    await approval(page)
      .getByRole("button", { name: "Từ chối yêu cầu" })
      .click();
    await expect(approval(page).getByText(/đã từ chối/)).toBeVisible();
    await expect(
      approval(page).getByText("Thiếu phạm vi áp dụng"),
    ).toBeVisible();
  });

  test("the sheet offers the same decision", async ({ page }) => {
    await open(page, `${BOARD}?view=kanban&item=mock-erp-item-138`);
    const sheet = page.getByRole("dialog");
    await expect(
      sheet.getByRole("region", { name: "Phê duyệt" }),
    ).toBeVisible();
    await sheet.getByRole("button", { name: "Duyệt", exact: true }).click();
    await expect(
      sheet.getByRole("region", { name: "Phê duyệt" }).getByText(/đã duyệt/),
    ).toBeVisible();
  });

  test("the requester asks a person, then cancels the request", async ({
    page,
  }) => {
    await open(page, `${BOARD}/items/mock-erp-item-142`);
    await page.getByRole("button", { name: "Thao tác trên thẻ" }).click();
    await page
      .getByRole("menuitem", { name: "Yêu cầu phê duyệt" })
      .press("ArrowRight");
    await page.getByRole("menuitem", { name: /Alex Rivera/ }).click();
    await expect(approval(page).getByText(/Đang chờ/)).toBeVisible();
    // The requester cannot decide their own request.
    await expect(
      approval(page).getByRole("button", { name: "Duyệt" }),
    ).toHaveCount(0);
    await approval(page).getByRole("button", { name: "Hủy yêu cầu" }).click();
    await expect(approval(page).getByText(/Đã hủy yêu cầu/)).toBeVisible();
    await expect(
      approval(page).getByRole("button", { name: "Hủy yêu cầu" }),
    ).toHaveCount(0);
  });

  test("a single click on a table title opens the item; Enter saves a new title, Escape cancels", async ({
    page,
  }) => {
    await open(page, `${BOARD}?view=table`);
    const title = page.getByRole("button", {
      name: "Chuyển dữ liệu khách hàng từ hệ thống cũ sang ERP",
      exact: true,
    });
    await title.click();
    await expect(page.getByRole("dialog")).toBeVisible();
    await page.keyboard.press("Escape");
    await expect(page.getByRole("dialog")).toHaveCount(0);

    await title.dblclick();
    const input = page.getByRole("textbox", { name: "Sửa tên ERP-142" });
    await expect(input).toBeFocused();
    await input.fill("Tên đã đổi tại chỗ");
    await page.keyboard.press("Escape");
    await expect(input).toHaveCount(0);
    await expect(title).toBeVisible();

    await title.focus();
    await page.keyboard.press("F2");
    await input.fill("  Tên đã đổi tại chỗ  ");
    await page.keyboard.press("Enter");
    await expect(
      page.getByRole("button", { name: "Tên đã đổi tại chỗ", exact: true }),
    ).toBeVisible();

    // Blank input is refused: the title stays.
    await page
      .getByRole("button", { name: "Tên đã đổi tại chỗ", exact: true })
      .focus();
    await page.keyboard.press("Enter");
    await input.fill("   ");
    await page.keyboard.press("Enter");
    await expect(
      page.getByRole("button", { name: "Tên đã đổi tại chỗ", exact: true }),
    ).toBeVisible();
  });
});
