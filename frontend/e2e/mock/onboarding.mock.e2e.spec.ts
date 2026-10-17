import { expect, test, type Page } from "@playwright/test";

/**
 * The setup pages of the Onboarding mockup on the mock world: the company's
 * name and address, departments, invitations, the first board, the done page
 * with the Home card, and the invited person's welcome page.
 */
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

test("the address step holds a taken address and offers the next free one", async ({
  page,
}) => {
  await open(page, "/onboarding");
  await page.getByLabel("Tên công ty").fill("Công ty CP Thực phẩm Sao Mai");
  await page.getByLabel("Địa chỉ workspace").fill("minhan");
  await expect(page.getByText(/minhan đã có người dùng, thử/)).toBeVisible();
  await expect(
    page.getByRole("button", { name: "Tiếp tục", exact: true }),
  ).toBeDisabled();
  await page.getByRole("button", { name: /^Dùng / }).click();
  await expect(page.getByText(/còn trống/).first()).toBeVisible();
  await expect(
    page.getByRole("button", { name: "Tiếp tục", exact: true }),
  ).toBeEnabled();
});

test("a new company goes through all four steps and lands on Home with its checklist", async ({
  page,
}) => {
  await open(page, "/onboarding");
  await page.getByLabel("Tên công ty").fill("Công ty CP Thực phẩm Sao Mai");
  await expect(page.getByText(/còn trống/).first()).toBeVisible();
  await page.getByRole("button", { name: "Tiếp tục", exact: true }).click();
  await page.getByRole("button", { name: /^Tạo \d+ phòng ban/ }).click();
  const emails = page.getByRole("textbox", { name: /^Email \d/ });
  await emails.nth(0).fill("khoa.hoang@saomaifood.vn");
  await page.getByRole("button", { name: /^Gửi \d+ lời mời/ }).click();
  await page.getByLabel("Tên board").fill("Ra mắt nước chấm Sao Mai");
  await page.getByRole("button", { name: "Tạo board" }).click();
  await expect(page.getByRole("heading", { level: 1 })).toContainText(
    "đã sẵn sàng",
  );
  await page.getByRole("button", { name: "Vào Trang chủ" }).click();
  await expect(page).toHaveURL(/\/workspaces\/[^/]+/);
  await expect(page.getByText(/0\/3|1\/3/).first()).toBeVisible();
});

test("the invited person sets a name and the look and starts", async ({
  page,
}) => {
  await open(page, "/welcome/mock-workspace-primary");
  await expect(page.getByRole("heading", { level: 1 })).toContainText(
    "bạn đã vào",
  );
  const name = page.getByLabel("Tên hiển thị");
  await name.fill("");
  await page.getByRole("button", { name: "Bắt đầu" }).click();
  await expect(page.getByRole("alert")).toBeVisible();
  await name.fill("Lý Trang");
  await page.getByRole("radio", { name: "Tối" }).click();
  await expect(page.locator("html")).toHaveClass(/dark/);
  await page.getByRole("button", { name: "Bắt đầu" }).click();
  await expect(page).not.toHaveURL(/\/welcome\//);
});
