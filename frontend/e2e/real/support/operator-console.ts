import { expect, type Page } from "@playwright/test";

const operatorUrl =
  process.env.REAL_E2E_OPERATOR_URL ?? "http://127.0.0.1:5180";
const operator = {
  email: process.env.REAL_E2E_OPERATOR_EMAIL ?? "operator@notrelix.test",
  password:
    process.env.REAL_E2E_OPERATOR_PASSWORD ?? "real-e2e-operator-password",
};

/** The operator console is a separate origin with its own sign-in; no tenant session is involved. */
export async function openTenantInConsole(page: Page) {
  await page.goto(operatorUrl);
  await page.getByLabel("Email").fill(operator.email);
  await page.getByLabel("Password").fill(operator.password);
  await page.getByRole("button", { name: "Sign in" }).click();
  await page.getByLabel("Search tenants by name or slug").fill("notrelix");
  await page.getByRole("button", { name: "Search" }).click();
  await page.getByRole("link", { name: "Notrelix", exact: true }).click();
  await expect(page.getByRole("heading", { name: "Notrelix" })).toBeVisible();
}
