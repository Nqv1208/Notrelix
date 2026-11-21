import { expect, type Page } from "@playwright/test";

/** Opens the info panel at a section through the header's "…" menu (the pinned and following entries deep-open it). */
export async function openInfoSection(page: Page, key: "pins" | "following") {
  await page
    .locator("main header")
    .getByRole("button", { name: "Thêm", exact: true })
    .click();
  await page
    .getByRole("menuitem", {
      name: key === "pins" ? "Tin đã ghim" : "Đang theo dõi",
    })
    .click();
  await expect(
    page.getByTestId(
      key === "pins" ? "chat-pins-panel" : "chat-followed-panel",
    ),
  ).toBeVisible();
}
