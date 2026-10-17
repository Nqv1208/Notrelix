import { test, expect } from "@playwright/test";

test.describe("Production Auth & App Smoke Flow", () => {
  test("redirects unauthenticated users to sign-in page", async ({ page }) => {
    await page.goto("/workspaces/ws-1");
    await expect(page).toHaveURL(/\/sign-in/);
  });

  test("sign in form renders correctly", async ({ page }) => {
    await page.goto("/sign-in");
    await expect(
      page.locator('input[type="email"], input[name="email"]'),
    ).toBeVisible();
    // Sign-in is two steps: the password field only appears after the email.
    await expect(
      page.getByRole("button", { name: /Tiếp tục|Continue/ }).first(),
    ).toBeVisible();
  });
});
