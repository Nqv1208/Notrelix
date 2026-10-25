import { expect, test } from "@playwright/test";

const scenario = process.env.VITE_MOCK_STATE ?? "default";
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const base = "/workspaces/mock-workspace-primary/integrations";

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

test("the page starts with no connection and lists what can be added; a provider without configuration cannot be connected", async ({
  page,
}) => {
  await page.goto(base);
  await expect(page.getByText("Đang dùng 0")).toBeVisible();
  await expect(page.getByText("Chưa cấu hình")).toHaveCount(1);
  await expect(
    page.getByRole("button", { name: "Kết nối Google Calendar" }),
  ).toHaveCount(0);
  await expect(
    page.getByRole("button", { name: "Kết nối Webhook gửi đi" }),
  ).toBeVisible();
});

test("an outgoing webhook is connected, shows its secret once, and can be paused and resumed", async ({
  page,
}) => {
  await page.goto(base);
  await page.getByRole("button", { name: "Kết nối Webhook gửi đi" }).click();
  const dialog = page.getByRole("dialog", { name: "Kết nối Webhook gửi đi" });
  await dialog
    .getByLabel("Địa chỉ nhận")
    .fill("https://hooks.example.com/notrelix");
  await dialog.getByRole("button", { name: "Kết nối", exact: true }).click();
  const secret = page.getByRole("dialog", { name: "Secret ký" });
  await expect(secret).toBeVisible();
  await secret.getByRole("button", { name: "Xong" }).click();

  await page.getByRole("link", { name: "Mở Webhook gửi đi" }).first().click();
  await page.getByRole("button", { name: "Tạm dừng" }).click();
  await expect(page.getByRole("button", { name: "Tiếp tục" })).toBeVisible();
  await page.getByRole("button", { name: "Tiếp tục" }).click();
  await expect(page.getByRole("button", { name: "Tạm dừng" })).toBeVisible();
});
