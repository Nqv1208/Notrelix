import type { Page } from "@playwright/test";

/**
 * The mock world starts with a live session, and a document opened on /sign-in forwards a live
 * session to Home. The pre-login pages are measured from a signed-out start: the mock backend
 * reads this key once at boot (mock only, nothing in the production auth path).
 */
export async function startSignedOut(page: Page) {
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-mock-signed-out", "1");
    } catch {
      // Private mode: the world starts signed in.
    }
  });
}
