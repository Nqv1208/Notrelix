import { test, expect } from "@playwright/test";

test.describe("Production Auth & App Smoke Flow", () => {
  test("redirects unauthenticated users to sign-in page", async ({ page }) => {
    // Unauthenticated means the API refuses the session. An API that cannot be reached says nothing
    // about the session, and the app keeps the page and offers a retry instead of signing out.
    await page.route(/\/auth\/(me|refresh)$/, (route) =>
      route.fulfill({
        status: 401,
        contentType: "application/problem+json",
        body: JSON.stringify({
          status: 401,
          code: "identity.auth.unauthenticated",
        }),
      }),
    );
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
