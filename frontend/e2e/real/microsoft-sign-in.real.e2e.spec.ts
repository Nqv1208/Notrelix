import { expect, test } from "@playwright/test";

test("a provider that is not set up sends the person back to the sign-in page and points to email and password", async ({
  page,
}) => {
  await page.goto("/sign-in");
  await page.waitForLoadState("networkidle");
  await page.getByRole("button", { name: /Continue with Microsoft/i }).click();

  // The browser leaves for the start endpoint; with no Microsoft set up it comes straight back.
  await expect(page).toHaveURL(
    /\/sign-in\?.*error=identity\.oauth\.provider-unavailable/,
  );
  await expect(page.getByRole("alert")).toBeVisible();
  await expect(page.getByLabel("Email")).toBeVisible();
});
