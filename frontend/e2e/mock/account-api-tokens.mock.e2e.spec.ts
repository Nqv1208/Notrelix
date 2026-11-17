import AxeBuilder from "@axe-core/playwright";
import { expect, test, type Page } from "@playwright/test";
import { MOCK_AUTH } from "../../packages/dev/mock-backend/src/state/mock-auth-directory";

/**
 * Workspace API tokens (UC-IDN-L01, L02, L03): the list, issuing one behind a step-up proof with the
 * secret shown once, and revoking one after a confirmation. The page offers no scopes: the scope
 * vocabulary (UC-IDN-L05) waits on a product decision.
 */
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const tokens = "/workspaces/mock-workspace-primary/account/tokens";

test.beforeEach(async ({ page }) => {
  test.skip(scenario !== "default", "Assertions need the default world data");
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "vi");
    } catch {
      // Private mode: the default locale applies.
    }
  });
});

async function open(page: Page) {
  await page.goto(tokens);
  await expect(
    page.getByRole("heading", { level: 2, name: "Token API" }),
  ).toBeVisible();
}

async function issue(page: Page, name: string) {
  await page.getByRole("button", { name: "Tạo token", exact: true }).click();
  const form = page.getByRole("dialog", { name: "Tạo token API" });
  await form.getByLabel("Tên token").fill(name);
  await form.getByRole("button", { name: "Tạo token" }).click();
  const proof = page.getByRole("dialog", { name: "Xác nhận lại danh tính" });
  await proof
    .getByLabel("Mã 6 số từ ứng dụng xác thực")
    .fill(MOCK_AUTH.otpCode);
  await proof.getByRole("button", { name: "Xác nhận" }).click();
  return page.getByRole("dialog", { name: "Sao chép token của bạn" });
}

test("the list shows each token by name and its last four characters, never a secret", async ({
  page,
}) => {
  await open(page);
  const rows = page.getByRole("row");
  await expect(rows.filter({ hasText: "CI triển khai ERP" })).toContainText(
    "••••••a41f",
  );
  await expect(page.getByText("Chưa dùng")).toBeVisible();
  await expect(page.getByText(/ntk_v1\.[A-Za-z]/)).toHaveCount(0);
});

test("a token is issued behind a proof, its secret is shown once, then only its suffix remains", async ({
  page,
  context,
}) => {
  await context.grantPermissions(["clipboard-read", "clipboard-write"]);
  await open(page);
  const reveal = await issue(page, "Báo cáo tồn kho");
  const secret = await reveal.getByTestId("token-secret").innerText();
  expect(secret).toMatch(/^ntk_v1\./);
  await expect(reveal).toContainText("Đây là lần duy nhất bạn thấy token");

  // Only the button leaves the step; Escape does not drop the secret by accident.
  await page.keyboard.press("Escape");
  await expect(reveal).toBeVisible();
  await reveal.getByRole("button", { name: "Sao chép và đóng" }).click();
  await expect(reveal).toBeHidden();
  expect(await page.evaluate(() => navigator.clipboard.readText())).toBe(
    secret,
  );

  const row = page.getByRole("row").filter({ hasText: "Báo cáo tồn kho" });
  await expect(row).toContainText(`••••••${secret.slice(-4)}`);
  await expect(page.locator("body")).not.toContainText(secret);
  await page.reload();
  await expect(page.locator("body")).not.toContainText(secret);
});

test("a token with no name asks for one before any proof is bought", async ({
  page,
}) => {
  await open(page);
  await page.getByRole("button", { name: "Tạo token", exact: true }).click();
  const form = page.getByRole("dialog", { name: "Tạo token API" });
  await form.getByRole("button", { name: "Tạo token" }).click();
  await expect(form.getByRole("alert")).toContainText("Hãy đặt tên");
  await expect(
    page.getByRole("dialog", { name: "Xác nhận lại danh tính" }),
  ).toHaveCount(0);
});

test("a wrong code keeps the proof step open and names the problem", async ({
  page,
}) => {
  await open(page);
  await page.getByRole("button", { name: "Tạo token", exact: true }).click();
  const form = page.getByRole("dialog", { name: "Tạo token API" });
  await form.getByLabel("Tên token").fill("Thử");
  await form.getByRole("button", { name: "Tạo token" }).click();
  const proof = page.getByRole("dialog", { name: "Xác nhận lại danh tính" });
  await proof.getByLabel("Mã 6 số từ ứng dụng xác thực").fill("000000");
  await proof.getByRole("button", { name: "Xác nhận" }).click();
  await expect(proof.getByRole("alert")).toContainText("Mã chưa đúng");
  await expect(page.getByRole("row").filter({ hasText: "Thử" })).toHaveCount(0);
});

test("revoking asks first, takes effect at once and a second revoke is a quiet success", async ({
  page,
}) => {
  await open(page);
  await page
    .getByRole("button", { name: "Thu hồi Script dọn dẹp thẻ" })
    .click();
  const confirm = page.getByRole("alertdialog", { name: "Thu hồi token này?" });
  await confirm.getByRole("button", { name: "Giữ token" }).click();
  await expect(
    page.getByRole("button", { name: "Thu hồi Script dọn dẹp thẻ" }),
  ).toBeVisible();

  await page
    .getByRole("button", { name: "Thu hồi Script dọn dẹp thẻ" })
    .click();
  await confirm.getByRole("button", { name: "Thu hồi token" }).click();
  await expect(
    page.getByRole("status").filter({ hasText: "Đã thu hồi token" }),
  ).toBeVisible();
  const row = page.getByRole("row").filter({ hasText: "Script dọn dẹp thẻ" });
  await expect(row).toContainText("Đã thu hồi");
  await expect(
    page.getByRole("button", { name: "Thu hồi Script dọn dẹp thẻ" }),
  ).toHaveCount(0);
});

test("the issue walk keeps to the page at every width and has no serious accessibility fault", async ({
  page,
}) => {
  for (const [width, height] of [
    [1440, 900],
    [1024, 768],
    [390, 844],
  ] as const) {
    await page.setViewportSize({ width, height });
    await open(page);
    expect(
      await page.evaluate(
        () => document.documentElement.scrollWidth - window.innerWidth,
      ),
      `${width}: sideways scroll`,
    ).toBeLessThanOrEqual(0);
    await page.getByRole("button", { name: "Tạo token", exact: true }).click();
    await page.waitForTimeout(400);
    const results = await new AxeBuilder({ page }).analyze();
    expect(
      results.violations.filter(
        (v) => v.impact === "serious" || v.impact === "critical",
      ),
      `${width}: axe`,
    ).toEqual([]);
    await page.keyboard.press("Escape");
  }
});
