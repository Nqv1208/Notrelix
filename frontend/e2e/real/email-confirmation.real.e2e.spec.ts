import { randomUUID } from "node:crypto";
import { expect, test } from "@playwright/test";
import { CONFIRMATION_LINK, devMails } from "./support/dev-mail";

async function confirmationLinkFor(email: string): Promise<string> {
  return (await devMails(email, CONFIRMATION_LINK)).at(-1)!;
}

test("a new member confirms the email from the link and then creates the workspace", async ({
  page,
}) => {
  const suffix = randomUUID().slice(0, 8);
  const email = `confirm-${suffix}@example.com`;
  const password = `Pw-${randomUUID()}`;

  await page.goto("/sign-up");
  await page.waitForLoadState("networkidle");
  await page.getByLabel("First name").fill("Casey");
  await page.getByLabel("Last name").fill("Confirm");
  await page.getByLabel("Work email").fill(email);
  await page.locator("#password").fill(password);
  await page.locator("#confirmPassword").fill(password);
  await page.getByRole("checkbox").check();
  await page.getByRole("button", { name: /Create account/ }).click();
  // Registering opens the app: the personal workspace once its provisioning has run (UC-WSP-A02, an
  // event consumer, so eventual), the onboarding page while the list is still empty. Either is right.
  await expect(page).toHaveURL(/\/(workspaces\/[^/]+|onboarding)/);

  // The address is not confirmed yet: creating a further workspace is refused and the page says why.
  await page.goto("/onboarding");
  await page.getByLabel("Company name").fill(`Confirm ${suffix}`);
  await page.getByRole("button", { name: "Continue", exact: true }).click();
  await expect(page.getByRole("alert")).toContainText("Confirm your email");

  const link = await confirmationLinkFor(email);
  await page.goto(new URL(link).pathname + new URL(link).hash);
  await expect(
    page.getByRole("heading", { name: "Your email is confirmed" }),
  ).toBeVisible();

  // The link is single use.
  await page.goto(new URL(link).pathname + new URL(link).hash);
  await page.reload();
  await expect(
    page.getByRole("heading", { name: "This link no longer works" }),
  ).toBeVisible();

  await page.goto("/onboarding");
  await page.getByLabel("Company name").fill(`Confirm ${suffix}`);
  await page.getByRole("button", { name: "Continue", exact: true }).click();
  await expect(
    page.getByRole("heading", { name: "Departments" }),
  ).toBeVisible();
});
