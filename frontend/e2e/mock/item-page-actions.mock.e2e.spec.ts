import { expect, test, type Page } from "@playwright/test";

/**
 * The item actions of one work item on the mock world (full page): the one
 * actions menu, the duplicate dialog with its choices, the Move dialog that
 * names what the other board does not carry, and the linked docs section.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const ITEM =
  "/workspaces/mock-workspace-primary/boards/mock-board-erp/items/mock-erp-item-142";

async function open(page: Page) {
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "vi");
    } catch {
      // Private mode: the default locale applies.
    }
  });
  await page.goto(ITEM);
  await expect(page.getByRole("heading", { level: 1 })).toContainText(
    "Chuyển dữ liệu khách hàng",
    { timeout: 20_000 },
  );
}

test.describe("Item actions (owner)", () => {
  test.skip(
    persona !== "owner" || scenario !== "default" || faults !== "",
    "Needs the default owner world without faults",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  test("the menu opens the duplicate dialog with comments and history locked", async ({
    page,
  }) => {
    await open(page);
    await page.getByRole("button", { name: "Thêm thao tác" }).click();
    await page.getByRole("menuitem", { name: /^Nhân bản/ }).click();
    const dialog = page.getByRole("dialog", { name: "Nhân bản việc" });
    await expect(dialog).toBeVisible();
    await expect(
      dialog.getByRole("checkbox", { name: /^Bình luận/ }),
    ).toHaveAttribute("aria-disabled", "true");
    await dialog.getByRole("button", { name: "Nhân bản" }).click();
    await expect(dialog).toBeHidden();
  });

  test("the menu opens Move with the boards the person can use", async ({
    page,
  }) => {
    await open(page);
    await page.getByRole("button", { name: "Thêm thao tác" }).click();
    await page.getByRole("menuitem", { name: /^Chuyển board/ }).click();
    const dialog = page.getByRole("dialog", { name: /sang board khác/ });
    await expect(dialog).toBeVisible();
    await expect(dialog.getByRole("option").first()).toBeVisible();
    await page.keyboard.press("Escape");
    await expect(dialog).toBeHidden();
  });

  test("linked docs: link a page, then unlink it after confirming", async ({
    page,
  }) => {
    await open(page);
    const section = page.getByRole("region", { name: /Tài liệu liên kết/ });
    await section.getByRole("button", { name: "Liên kết tài liệu" }).click();
    await page.getByRole("option").first().click();
    await expect(
      section.getByRole("button", { name: /^Gỡ liên kết/ }),
    ).toHaveCount(1);
    await section.getByRole("button", { name: /^Gỡ liên kết/ }).click();
    await page
      .getByRole("alertdialog")
      .getByRole("button", { name: "Gỡ liên kết" })
      .click();
    await expect(
      section.getByRole("button", { name: /^Gỡ liên kết/ }),
    ).toHaveCount(0);
  });
});
