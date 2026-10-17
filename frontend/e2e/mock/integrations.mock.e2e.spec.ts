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

test("the list shows the connections in use and the catalog, and opens a connection", async ({
  page,
}) => {
  await page.goto(base);
  await expect(page.getByText("Đang dùng 4")).toBeVisible();
  await expect(page.getByText("Cần uỷ quyền lại")).toBeVisible();
  await page
    .getByRole("link", { name: "Mở Microsoft OneDrive & SharePoint" })
    .click();
  await expect(
    page.getByRole("heading", { name: "Microsoft OneDrive & SharePoint" }),
  ).toBeVisible();
  await expect(page.getByText("214 liên kết tệp").first()).toBeVisible();
});

test("an expired Teams authorization is renewed from its page", async ({
  page,
}) => {
  await page.goto(`${base}?item=teams`);
  await expect(page.getByText(/Tin nhắn mới tạm giữ, không mất/)).toBeVisible();
  await page
    .getByRole("button", { name: /Uỷ quyền lại với Microsoft/ })
    .click();
  await expect(page.getByText(/Tin nhắn mới tạm giữ, không mất/)).toBeHidden();
});

test("a webhook can be paused and resumed", async ({ page }) => {
  await page.goto(`${base}?item=webhook`);
  await expect(
    page.getByText("Hệ thống nhận đang trả về 503 từ 10:28."),
  ).toBeVisible();
  await page.getByRole("button", { name: "Tạm dừng" }).click();
  await expect(page.getByRole("button", { name: "Tiếp tục" })).toBeVisible();
});

test("connecting Google Drive asks for the scope first and adds the connection", async ({
  page,
}) => {
  await page.goto(base);
  await page.getByRole("button", { name: "Kết nối Google Drive" }).click();
  const dialog = page.getByRole("dialog", { name: "Kết nối Google Drive" });
  await expect(
    dialog.getByRole("radio", { name: /Workspace Minh An Group/ }),
  ).toBeChecked();
  await dialog.getByRole("button", { name: /Tiếp tục với Google/ }).click();
  await expect(dialog).toBeHidden();
  await expect(page.getByText("Đang dùng 5")).toBeVisible();
});

test("an Enterprise entry opens the plan sheet, and disconnecting OneDrive asks first", async ({
  page,
}) => {
  await page.goto(base);
  await page.getByRole("button", { name: /Xem Power BI/ }).click();
  await expect(
    page.getByText(/Có trong gói Enterprise\. Gói hiện tại: Business\./),
  ).toBeVisible();
  await page.goto(`${base}?item=onedrive`);
  await page.getByRole("button", { name: "Ngắt kết nối" }).click();
  const dialog = page.getByRole("alertdialog");
  await expect(
    dialog.getByText(/Không việc, trang hay tệp nào bị xoá/),
  ).toBeVisible();
  await dialog.getByRole("button", { name: "Ngắt kết nối" }).click();
  await expect(page.getByText("Đang dùng 3")).toBeVisible();
});
