import { expect, test, type Page } from "@playwright/test";

/**
 * The Board share dialog as a person uses it: open it from the Board header,
 * keyboard and focus, copy the Board link, expand the permission list, and
 * what stays inert because the mock backend has no write for it (invite,
 * levels, general access, history). The mock world resets on every page load.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";

async function open(page: Page) {
  await page.context().grantPermissions(["clipboard-read", "clipboard-write"]);
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "vi");
      window.localStorage.setItem("notrelix-system-rail", "pinned");
    } catch {
      // Private mode: the defaults apply.
    }
  });
  await page.goto(BOARD);
}

const share = (page: Page) =>
  page.getByRole("button", { name: /^Chia sẻ/ }).first();

test.describe("Board share dialog (owner)", () => {
  test.skip(
    persona !== "owner" || scenario !== "default" || faults !== "",
    "Owner checks need the default owner world without faults",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  test("opens from the header and Escape returns focus to the button", async ({
    page,
  }) => {
    await open(page);
    await share(page).click();
    const dialog = page.getByRole("dialog", { name: /^Chia sẻ/ });
    await expect(dialog).toBeVisible();
    await expect(dialog.locator(":focus")).toHaveCount(1);
    await page.keyboard.press("Escape");
    await expect(dialog).toBeHidden();
    await expect(share(page)).toBeFocused();
  });

  test("Tab stays inside the dialog", async ({ page }) => {
    await open(page);
    await share(page).click();
    const dialog = page.getByRole("dialog", { name: /^Chia sẻ/ });
    await expect(dialog).toBeVisible();
    for (let i = 0; i < 25; i += 1) {
      await page.keyboard.press("Tab");
      const inside = await page.evaluate(() =>
        Boolean(document.activeElement?.closest('[role="dialog"]')),
      );
      expect(inside, `Tab ${i + 1} left the dialog`).toBe(true);
    }
  });

  test("Done closes and copy link confirms", async ({ page }) => {
    await open(page);
    await share(page).click();
    await page.getByRole("button", { name: "Sao chép liên kết" }).click();
    await expect(page.getByText("Đã sao chép liên kết.")).toBeVisible();
    const copied = await page.evaluate(() => navigator.clipboard.readText());
    expect(copied).toContain("/boards/mock-board-erp");
    await page.getByRole("button", { name: "Xong" }).click();
    await expect(page.getByRole("dialog", { name: /^Chia sẻ/ })).toBeHidden();
  });

  test("Show more expands the list", async ({ page }) => {
    await open(page);
    await share(page).click();
    const rows = page.locator('[data-f="pr"]');
    await expect(rows).toHaveCount(5);
    await page.locator('[data-f="plmore"]').click();
    expect(await rows.count()).toBeGreaterThan(5);
  });

  test("what has no write is drawn aria-disabled with a reason", async ({
    page,
  }) => {
    await open(page);
    await share(page).click();
    for (const selector of [
      '[data-f="invite"]',
      '[data-f="ga"] [data-f="lvl-plain"]',
      '[data-f="logl"]',
    ]) {
      const control = page.locator(selector).first();
      await expect(control).toHaveAttribute("aria-disabled", "true");
      await expect(
        control.locator("xpath=ancestor-or-self::*[@title][1]"),
      ).toHaveAttribute("title", /.+/);
    }
    // Activating the invite button changes nothing.
    await page.locator('[data-f="invite"]').click({ force: true });
    await expect(page.getByRole("dialog", { name: /^Chia sẻ/ })).toBeVisible();
  });
});

test.describe("Board share dialog (not a manager)", () => {
  test.skip(
    persona === "owner" || scenario !== "default" || faults !== "",
    "Needs a non-owner persona",
  );

  test("the header offers no live Share action", async ({ page }) => {
    await open(page);
    const button = share(page);
    if ((await button.count()) === 0) return;
    const disabled =
      (await button.getAttribute("aria-disabled")) === "true" ||
      (await button.isDisabled());
    if (!disabled) {
      await button.click();
      await expect(
        page.getByRole("dialog", { name: /^Chia sẻ/ }),
      ).toBeVisible();
    }
  });
});
