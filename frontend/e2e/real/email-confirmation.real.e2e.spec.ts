import { execFileSync } from "node:child_process";
import { randomUUID } from "node:crypto";
import { expect, test } from "@playwright/test";

const backendContainer = "notrelix-fe-real-e2e-backend";

/** No mail provider runs on a developer machine: the development backend logs the message text. */
async function confirmationLinkFor(email: string): Promise<string> {
  const pattern = new RegExp(
    `Development email to ${email.replace(/[.+]/g, "\\$&")}: .*?(http\\S*/verify-email#token=\\S+)`,
  );
  for (let attempt = 0; attempt < 40; attempt += 1) {
    const logs = execFileSync("docker", ["logs", backendContainer], {
      encoding: "utf8",
      maxBuffer: 64 * 1024 * 1024,
    });
    const match = pattern.exec(logs);
    if (match) return match[1];
    await new Promise((resolve) => setTimeout(resolve, 1000));
  }
  throw new Error(`no confirmation email was logged for ${email}`);
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
  await expect(page).toHaveURL(/\/onboarding/);

  // The address is not confirmed yet: creating the workspace is refused and the page says why.
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
