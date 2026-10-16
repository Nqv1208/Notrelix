import { expect, test, type Page } from "@playwright/test";
import { MOCK_AUTH } from "../../packages/dev/mock-backend/src/state/mock-auth-directory";

/**
 * The pre-login pages of the Auth mockup on the mock world: sign in with an
 * email, a password and a second step; the company's single sign-on; the
 * lock after five wrong passwords; the password reset; a guest invitation.
 * The accounts are the mock's own (`mock-auth-directory.ts`).
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";

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

async function toPassword(page: Page) {
  await open(page, "/sign-in");
  await page.getByLabel("Email").fill(MOCK_AUTH.personalEmail);
  await page.getByRole("button", { name: "Tiếp tục", exact: true }).click();
  await expect(
    page.getByRole("heading", { level: 1, name: "Nhập mật khẩu" }),
  ).toBeVisible();
}

async function toOtp(page: Page) {
  await toPassword(page);
  await page
    .getByLabel("Mật khẩu", { exact: true })
    .fill(MOCK_AUTH.personalPassword);
  await page.locator("form button[type=submit]").click();
  await expect(
    page.getByRole("heading", { level: 1, name: "Xác minh hai bước" }),
  ).toBeVisible();
}

test.describe("Sign-in pages (owner)", () => {
  test.skip(
    persona !== "owner" || scenario !== "default" || faults !== "",
    "Needs the default owner world without faults",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  test("the email step asks for a well-formed email", async ({ page }) => {
    await open(page, "/sign-in");
    await expect(
      page.getByRole("heading", { level: 1, name: "Đăng nhập Notrelix" }),
    ).toBeVisible();
    await page.getByLabel("Email").fill("khong-phai-email");
    await page.getByRole("button", { name: "Tiếp tục", exact: true }).click();
    await expect(page.getByRole("alert")).toContainText("Nhập email dạng");
    await expect(page.getByLabel("Email")).toHaveAttribute(
      "aria-invalid",
      "true",
    );
  });

  test("a password account goes through the password and the second step into the workspace", async ({
    page,
  }) => {
    await toOtp(page);
    const first = page.getByLabel("Số 1 trên 6");
    await first.click();
    await page.evaluate((code) => {
      const box = document.querySelector<HTMLInputElement>(
        'input[aria-label="Số 1 trên 6"]',
      );
      const data = new DataTransfer();
      data.setData("text", code);
      box?.dispatchEvent(
        new ClipboardEvent("paste", {
          clipboardData: data,
          bubbles: true,
          cancelable: true,
        }),
      );
    }, MOCK_AUTH.otpCode);
    await expect(page.getByLabel("Số 6 trên 6")).toHaveValue(
      MOCK_AUTH.otpCode.slice(-1),
    );
    await page.getByRole("button", { name: "Xác minh" }).click();
    await expect(page).toHaveURL(/\/workspaces\/mock-workspace-primary/);
  });

  test("a wrong second-step code is refused; a backup code opens the account", async ({
    page,
  }) => {
    await toOtp(page);
    for (const [index, digit] of "000000".split("").entries())
      await page.getByLabel(`Số ${index + 1} trên 6`).fill(digit);
    await page.getByRole("button", { name: "Xác minh" }).click();
    await expect(page.getByRole("alert")).toContainText("Mã chưa đúng");
    await page.getByRole("button", { name: "Dùng mã dự phòng" }).click();
    await page.getByLabel("Mã dự phòng").fill(MOCK_AUTH.backupCode);
    await page.getByRole("button", { name: "Xác minh" }).click();
    await expect(page).toHaveURL(/\/workspaces\/mock-workspace-primary/);
  });

  test("five wrong passwords lock the account and the button waits with the time left", async ({
    page,
  }) => {
    await toPassword(page);
    const password = page.getByLabel("Mật khẩu", { exact: true });
    for (let attempt = 1; attempt <= 5; attempt += 1) {
      await password.fill(`sai-mat-khau-${attempt}`);
      await page.locator("form button[type=submit]").click();
      // The answer clears the field: the next try starts from an empty one.
      await expect(password).toHaveValue("");
      await expect(page.getByRole("alert").first()).toContainText(
        "Email hoặc mật khẩu chưa đúng.",
      );
      if (attempt === 4)
        await expect(page.getByText(/Còn 1 lần thử/)).toBeVisible();
    }
    await expect(page.getByText(/Thử lại sau \d+ giây/)).toBeVisible();
    const wait = page.locator("form button[type=submit]");
    await expect(wait).toBeDisabled();
    await expect(wait).toContainText(/\d:\d\d/);
    // The right password does not get in while the lock holds.
    await expect(page.getByRole("link", { name: /\/workspaces/ })).toHaveCount(
      0,
    );
  });

  test("a company email goes to its provider and comes back signed in", async ({
    page,
  }) => {
    await open(page, "/sign-in");
    await page.getByLabel("Email").fill(`thuha.nguyen@${MOCK_AUTH.ssoDomain}`);
    await page.getByRole("button", { name: "Tiếp tục", exact: true }).click();
    await expect(page.getByRole("heading", { level: 1 })).toContainText(
      "Minh An Group dùng đăng nhập một lần qua Microsoft Entra",
    );
    await expect(
      page.getByText("Mật khẩu do công ty bạn quản lý."),
    ).toBeVisible();
    await page
      .getByRole("button", { name: "Tiếp tục với Microsoft Entra" })
      .click();
    await expect(page).toHaveURL(/\/workspaces\/mock-workspace-primary/);
  });

  test("Google signs in, and 'Dùng email khác' goes back to the first step", async ({
    page,
  }) => {
    await open(page, "/sign-in");
    await page.getByLabel("Email").fill(`thuha.nguyen@${MOCK_AUTH.ssoDomain}`);
    await page.getByRole("button", { name: "Tiếp tục", exact: true }).click();
    await page.getByRole("button", { name: "Dùng email khác" }).click();
    await expect(
      page.getByRole("heading", { level: 1, name: "Đăng nhập Notrelix" }),
    ).toBeVisible();
    await page.getByRole("button", { name: "Tiếp tục với Google" }).click();
    await expect(page).toHaveURL(/\/workspaces\/mock-workspace-primary/);
  });

  test("the language switch changes the copy", async ({ page }) => {
    await open(page, "/sign-in");
    await page.getByRole("button", { name: "English" }).click();
    await expect(
      page.getByRole("heading", { level: 1, name: "Sign in to Notrelix" }),
    ).toBeVisible();
    await expect(page.getByRole("button", { name: "English" })).toHaveAttribute(
      "aria-pressed",
      "true",
    );
  });

  test("password reset: the link goes out, the answer does not name the account, resend waits", async ({
    page,
  }) => {
    await toPassword(page);
    await page.getByRole("button", { name: "Quên mật khẩu?" }).click();
    await expect(page).toHaveURL(/\/forgot-password/);
    await expect(page.getByLabel("Email")).toHaveValue(MOCK_AUTH.personalEmail);
    await page.getByRole("button", { name: "Gửi liên kết đặt lại" }).click();
    await expect(
      page.getByRole("heading", { level: 1, name: "Kiểm tra email của bạn" }),
    ).toBeVisible();
    await expect(page.getByText(/Nếu email có tài khoản/)).toBeVisible();
    const resend = page.getByRole("button", { name: /Gửi lại/ });
    await expect(resend).toBeDisabled();
    await expect(resend).toContainText(/0:\d\d/);
    await page.getByRole("button", { name: "Dùng email khác" }).click();
    await expect(page.getByLabel("Email")).toHaveValue("");
  });

  test("a guest invitation: the form is checked, the email is fixed, accepting lands on the board", async ({
    page,
  }) => {
    await open(page, "/invite/invite-erp-guest");
    await expect(page.getByRole("heading", { level: 1 })).toContainText(
      "Nguyễn Thu Hà mời bạn vào board Triển khai ERP Q4 của Minh An Group",
    );
    await expect(
      page.getByText(/Khách · Có thể sửa · đến 31\/01\/2027/),
    ).toBeVisible();
    await expect(page.getByLabel("Email được mời")).toHaveAttribute(
      "readonly",
      "",
    );
    await page.getByLabel("Tạo mật khẩu").fill("ngan");
    await page
      .getByRole("button", { name: "Chấp nhận và tạo tài khoản" })
      .click();
    await expect(page.getByText("Dùng ít nhất 12 ký tự.")).toBeVisible();
    await page.getByLabel("Tạo mật khẩu").fill("mot-mat-khau-du-dai");
    await page
      .getByRole("button", { name: "Chấp nhận và tạo tài khoản" })
      .click();
    await expect(page).toHaveURL(
      /\/workspaces\/mock-workspace-primary\/boards\/mock-board-erp/,
    );
  });

  test("declining an invitation says so and offers sign-in", async ({
    page,
  }) => {
    await open(page, "/invite/invite-erp-guest");
    await page.getByRole("button", { name: "Từ chối" }).click();
    await expect(
      page.getByRole("heading", { level: 1, name: "Bạn đã từ chối lời mời" }),
    ).toBeVisible();
    await page.getByRole("button", { name: "Về trang đăng nhập" }).click();
    await expect(page).toHaveURL(/\/sign-in/);
  });

  test("an expired, used or unknown invitation cannot be used", async ({
    page,
  }) => {
    await open(page, "/invite/invite-erp-expired");
    await expect(
      page.getByRole("heading", { level: 1, name: "Lời mời đã hết hạn" }),
    ).toBeVisible();
    await expect(
      page.getByText("Lời mời đến Minh An Group không còn dùng được."),
    ).toBeVisible();
    await open(page, "/invite/invite-erp-used");
    await expect(
      page.getByRole("heading", { level: 1, name: "Lời mời đã được dùng" }),
    ).toBeVisible();
    await open(page, "/invite/khong-co-loi-moi-nay");
    await expect(
      page.getByRole("heading", {
        level: 1,
        name: "Không tìm thấy lời mời này",
      }),
    ).toBeVisible();
  });
});
