import { expect, test } from "@playwright/test";

// A visitor with no session opens these pages directly: a reload, a link from an email, a new tab.
// The session check answers 401 there, and that must not send the page to sign-in as "session ended".
// `/auth/callback` is not listed: with no session it has nowhere to stay and moves on to the home page.
const publicPages = [
  "/forgot-password",
  "/reset-password",
  "/verify-email",
  "/invite#token=not-a-real-token",
  "/invite/not-a-real-token",
];

for (const path of publicPages) {
  test(`a signed-out visitor opens ${path} directly, reloads it and stays on the page`, async ({
    page,
  }) => {
    const pathname = new URL(path, "http://app.invalid").pathname;
    const sessionEnded = page.getByText(/Your session ended/i);

    for (const step of ["open", "reload"]) {
      if (step === "open") await page.goto(path);
      else await page.reload();
      await page.waitForLoadState("networkidle");
      expect(new URL(page.url()).pathname, step).toBe(pathname);
      expect(page.url(), step).not.toContain("reason=session-ended");
      await expect(sessionEnded, step).toHaveCount(0);
      await expect(page.locator("body"), step).not.toBeEmpty();
    }
  });
}
