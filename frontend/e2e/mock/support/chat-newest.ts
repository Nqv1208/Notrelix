import { expect, type Page } from "@playwright/test";

/**
 * A room opens at its first unread message, and only the rows near the view are in the page. Checks that act on the newest
 * messages go there first, through the jump control, and wait for the list to settle.
 */
export async function toNewest(page: Page) {
  const jump = page.getByRole("button", {
    name: /^(Jump to latest|Xem tin mới nhất)$/,
  });
  if (await jump.isVisible()) await jump.click();
  await expect(jump).toBeHidden();
  await page.waitForTimeout(300);
}
