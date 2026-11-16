import { execFileSync } from "node:child_process";
import { expect, type Page } from "@playwright/test";

/** Runs a statement on the real-run database and returns its single value. */
export function sqlValue(statement: string): string {
  return execFileSync(
    "docker",
    [
      "exec",
      "notrelix-fe-real-e2e-postgres",
      "psql",
      "-U",
      "postgres",
      "-d",
      "notrelix_real_e2e",
      "-v",
      "ON_ERROR_STOP=1",
      "-At",
      "-c",
      statement,
    ],
    { encoding: "utf8" },
  ).trim();
}

/**
 * The provider test double shows its own sign-in form; the person is whoever the form says. The
 * double signs the ID token, so the backend checks it like Google's.
 */
export async function signInAtProvider(
  page: Page,
  subject: string,
  claims: Record<string, unknown>,
) {
  await page.waitForURL(/\/google\/authorize/);
  await page.locator('input[name="username"]').fill(subject);
  await page.locator('textarea[name="claims"]').fill(JSON.stringify(claims));
  await page.locator('input[type="submit"]').click();
}

export async function continueWithGoogle(page: Page) {
  await page.goto("/sign-in");
  await page.waitForLoadState("networkidle");
  await page.getByRole("button", { name: /Continue with Google/i }).click();
}

export async function expectSignedIn(page: Page) {
  await expect(page).toHaveURL(
    /\/(workspaces\/[^/]+|home|onboarding)(\/|$|\?)/,
    {
      timeout: 20_000,
    },
  );
}
