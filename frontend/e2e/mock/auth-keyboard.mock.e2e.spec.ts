import { expect, test, type Page } from "@playwright/test";
import { MOCK_AUTH } from "../../packages/dev/mock-backend/src/state/mock-auth-directory";

/**
 * UC-IDN-B06, B07, I01: the reset and the second step finish with the keyboard alone (Tab to reach
 * a control, typing, Enter to submit), so no step needs a pointer.
 */
const scenario = process.env.VITE_MOCK_STATE ?? "default";
test.beforeEach(({}, testInfo) => {
  test.skip(scenario !== "default", "Assertions need the default world data");
  void testInfo;
});

async function english(page: Page) {
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "en");
    } catch {
      // Private mode: defaults apply.
    }
  });
}

/** Tab until the focused element satisfies `match`; the stop the page already holds counts. */
async function tabTo(page: Page, label: string, match: string) {
  for (let stop = 0; stop < 15; stop += 1) {
    const name = await page.evaluate(() => {
      const el = document.activeElement as HTMLElement | null;
      if (!el || el === document.body) return "";
      return (
        el?.getAttribute("aria-label") ||
        el?.id ||
        el?.textContent?.trim() ||
        ""
      );
    });
    if (name.includes(match)) return;
    await page.keyboard.press("Tab");
  }
  throw new Error(`${label}: focus never reached "${match}"`);
}

test("reset by keyboard: ask for a code, enter it with a new password, land on sign-in", async ({
  page,
}) => {
  await english(page);
  await page.goto("/forgot-password");
  await expect(page.locator("#auth-reset")).toBeVisible();
  await tabTo(page, "email", "auth-reset");
  await page.keyboard.type(MOCK_AUTH.personalEmail);
  await page.keyboard.press("Enter");
  await expect(
    page.getByRole("heading", { level: 1, name: "Check your email" }),
  ).toBeVisible();

  await tabTo(page, "enter the code", "Enter the code");
  await page.keyboard.press("Enter");
  await expect(page).toHaveURL(/\/reset-password\?email=/);
  await tabTo(page, "code", "auth-reset-code");
  await page.keyboard.type(MOCK_AUTH.resetCode);
  await page.keyboard.press("Tab");
  await page.keyboard.type(`Pw-${Date.now()}-keys`);
  await page.keyboard.press("Enter");
  await expect(page).toHaveURL(/\/sign-in\?reason=password-reset/);
});

test("second step by keyboard: six digits type through the boxes and Enter signs in", async ({
  page,
}) => {
  await english(page);
  await page.goto("/sign-in");
  await expect(page.locator("#auth-email")).toBeVisible();
  await tabTo(page, "email", "auth-email");
  await page.keyboard.type(MOCK_AUTH.personalEmail);
  await page.keyboard.press("Enter");
  await expect(page.locator("#auth-password")).toBeVisible();
  // The password field opens over 300 ms; it takes the keyboard when it is enabled.
  await expect(page.locator("#auth-password")).toBeEnabled();
  await tabTo(page, "password", "auth-password");
  await page.keyboard.type(MOCK_AUTH.personalPassword);
  await page.keyboard.press("Enter");
  await expect(
    page.getByRole("heading", { level: 1, name: "Two-step verification" }),
  ).toBeVisible();
  // The first box takes the focus; the digits move on by themselves.
  await tabTo(page, "digit", "1 of 6");
  await page.keyboard.type(MOCK_AUTH.otpCode);
  await page.keyboard.press("Enter");
  await expect(page).toHaveURL(/\/workspaces\/mock-workspace-primary/);
});
