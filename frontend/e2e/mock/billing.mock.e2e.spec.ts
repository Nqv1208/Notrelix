import { expect, test } from "@playwright/test";

const scenario = process.env.VITE_MOCK_STATE ?? "default";
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const billing = "/workspaces/mock-workspace-primary/billing";

test.beforeEach(async ({ page }) => {
  test.skip(
    scenario !== "default" || persona !== "owner",
    "Assertions need the default world and the owner persona",
  );
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "vi");
    } catch {
      // Private mode: the default locale applies.
    }
  });
});

test("the overview shows the plan, the usage of both workspaces and the payment boxes", async ({
  page,
}) => {
  await page.goto(billing);
  await expect(
    page.getByRole("heading", { name: "Business", exact: true }),
  ).toBeVisible();
  await expect(page.getByText("Còn 6 chỗ")).toBeVisible();
  await expect(page.getByText("Visa •••• 4821")).toBeVisible();
  await expect(
    page.getByRole("meter", { name: /Chỗ đã dùng: 104 trên 110/ }),
  ).toBeVisible();
  await expect(page.getByText("Trịnh Quốc Hưng")).toBeVisible();
});

test("the plan comparison, the invoices and the downgrade preview open from the overview", async ({
  page,
}) => {
  await page.goto(billing);
  await page.getByRole("link", { name: "Đổi gói" }).click();
  await expect(
    page.getByRole("heading", { name: "So sánh các gói" }),
  ).toBeVisible();
  await expect(
    page.getByRole("button", { name: "Không chọn được" }),
  ).toBeDisabled();
  await page.getByRole("link", { name: "Xem thay đổi khi hạ gói" }).click();
  await expect(
    page.getByRole("heading", { name: "Hạ từ Business xuống Pro" }),
  ).toBeVisible();
  await page.getByRole("button", { name: "Xác nhận hạ gói" }).click();
  await expect(page.getByText("Hạ xuống Pro từ 01/01/2027")).toBeVisible();
  await page.getByRole("link", { name: "Xem tất cả" }).click();
  await expect(
    page.getByRole("heading", { name: "Hoá đơn", level: 2 }),
  ).toBeVisible();
  await expect(page.getByText("2.664.719 đ")).toBeVisible();
  await page.getByRole("link", { name: "HĐ-2026-0047" }).click();
  await expect(page.getByText("Thêm 6 chỗ gói Business")).toBeVisible();
});

test("buying seats waits for the bank, then adds them", async ({ page }) => {
  await page.goto(billing);
  await page.getByRole("button", { name: "Mua thêm chỗ" }).click();
  const dialog = page.getByRole("dialog", { name: "Mua thêm chỗ" });
  await expect(dialog.getByRole("cell", { name: "4.572.927 đ" })).toBeVisible();
  await dialog.getByRole("button", { name: "Xác nhận mua" }).click();
  await expect(
    dialog.getByText(/Đang xác nhận thanh toán với ngân hàng/),
  ).toBeVisible();
  await dialog.getByRole("button", { name: "Kiểm tra lại" }).click();
  await dialog.getByRole("button", { name: "Kiểm tra lại" }).click();
  await expect(dialog).toBeHidden();
  await expect(
    page.getByRole("meter", { name: /Chỗ đã dùng: 104 trên 120/ }),
  ).toBeVisible();
});

test("an invitation beyond the seats left is stopped with a way to buy more", async ({
  page,
}) => {
  await page.goto("/workspaces/mock-workspace-primary/members?invite=seats");
  const dialog = page.getByRole("dialog", { name: "Mời vào Minh An Group" });
  await dialog
    .getByPlaceholder("Thêm email")
    .fill(
      "a1@minhan.vn, a2@minhan.vn, a3@minhan.vn, a4@minhan.vn, a5@minhan.vn, a6@minhan.vn, a7@minhan.vn",
    );
  await dialog.getByPlaceholder("Thêm email").press("Enter");
  await expect(dialog.getByText(/Còn 6 chỗ\. Mua thêm chỗ/)).toBeVisible();
  await expect(
    dialog.getByRole("button", { name: "Gửi 7 lời mời" }),
  ).toBeDisabled();
  await dialog.getByRole("button", { name: "Bớt người" }).click();
  await expect(
    dialog.getByRole("button", { name: "Gửi 6 lời mời" }),
  ).toBeEnabled();
});
