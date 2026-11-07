import { expect, test, type Page } from "@playwright/test";

/**
 * The card popup's attachments, by who reads them.
 *
 * UC-WM-R04: an editor gets "Thêm đính kèm"; a Viewer (`VITE_MOCK_PERSONA=viewer`) lists the files with no control
 * to add one (the producer and the mock answer 403 to a Viewer's upload, proved in their own tests).
 * UC-WM-R05: a linked page the viewer may not read is only counted by the producer; the popup shows it as
 * "Tài liệu hạn chế" with no title and no link (`globalThis.__mockHiddenLinkedPages`).
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";

async function open(page: Page, hidden = 0) {
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.addInitScript((count) => {
    (globalThis as Record<string, unknown>).__mockHiddenLinkedPages = count;
    try {
      window.localStorage.setItem("notrelix-locale", "vi");
    } catch {
      // Private mode: the defaults apply.
    }
  }, hidden);
  await page.goto(`${BOARD}?item=mock-erp-item-142`);
  const heading = page.getByRole("heading", { name: "Đính kèm", exact: true });
  await expect(heading).toBeVisible();
  return heading.locator("xpath=ancestor::section[1]");
}

test("an editor can add an attachment", async ({ page }) => {
  test.skip(persona !== "owner", "the owner world");
  const section = await open(page);
  await expect(
    section.getByRole("button", { name: "Thêm đính kèm" }).first(),
  ).toBeVisible();
});

test("a Viewer lists the attachments with no control to add one", async ({
  page,
}) => {
  test.skip(persona !== "viewer", "run with VITE_MOCK_PERSONA=viewer");
  const section = await open(page);
  await expect(
    section.getByRole("button", { name: "Thêm đính kèm" }),
  ).toHaveCount(0);
  await expect(section.locator('input[type="file"]')).toHaveCount(0);
});

test("a linked page the viewer cannot read shows as restricted, with no title", async ({
  page,
}) => {
  test.skip(persona !== "owner", "the owner world");
  const section = await open(page, 2);
  const restricted = section.getByText("Tài liệu hạn chế", { exact: true });
  await expect(restricted).toHaveCount(2);
  await expect(
    section.getByText(
      "Bạn không xem được. Hỏi chủ tài liệu để được cấp quyền.",
    ),
  ).toHaveCount(2);
  // A restricted row names nothing and opens nothing.
  const row = restricted
    .first()
    .locator("xpath=ancestor::*[.//*[contains(@class,'size-5')]][1]");
  await expect(row.locator("a")).toHaveCount(0);
});
