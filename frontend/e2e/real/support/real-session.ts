import { expect, type Page } from "@playwright/test";

export const realUsers = {
  admin: {
    email: process.env.REAL_E2E_ADMIN_EMAIL ?? "admin@notrelix.test",
    password: process.env.REAL_E2E_ADMIN_PASSWORD ?? "real-e2e-admin-password",
  },
  guest: {
    email: process.env.REAL_E2E_GUEST_EMAIL ?? "guest@notrelix.test",
    password: process.env.REAL_E2E_GUEST_PASSWORD ?? "real-e2e-guest-password",
  },
} as const;

export async function signIn(
  page: Page,
  user: (typeof realUsers)[keyof typeof realUsers],
) {
  await page.goto("/sign-in");
  await page.waitForLoadState("networkidle");
  await page.getByLabel("Email").fill(user.email);
  await page.locator('input[name="password"]').fill(user.password);
  await page.getByRole("button", { name: "Sign in" }).click();
  await expect(page).toHaveURL(/\/home$/);
  await page.reload();
  await expect(
    page.getByRole("heading", { name: "Your workspaces" }),
  ).toBeVisible();
}

export async function openWorkspace(page: Page, name: string) {
  await page.getByRole("heading", { name }).click();
  await expect(page).toHaveURL(/\/workspaces\/[^/]+$/);
  await expect(page.getByText("Active Boards", { exact: true })).toBeVisible();
}

export async function openBoard(page: Page, title: RegExp) {
  await page.getByText(title).first().click();
  await expect(page).toHaveURL(/\/workspaces\/[^/]+\/boards\/[^/]+$/);
  await expect(page.getByRole("button", { name: "Board" })).toBeVisible();
}
