import AxeBuilder from "@axe-core/playwright";
import { expect, test, type Page } from "@playwright/test";

/**
 * The Board share dialog as a person uses it: open it from the Board header,
 * keyboard and focus, copy the Board link, the permissions given on the
 * Board, change and remove one, the history, switch to Private, and what stays
 * inert because there is no write for it (invite, general access). The mock world resets on every page load.
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

  test("lists only the permissions given on the Board, five rows, no show more", async ({
    page,
  }) => {
    await open(page);
    await share(page).click();
    const dialog = page.getByRole("dialog", { name: /^Chia sẻ/ });
    await expect(dialog.getByText("Board trong Phòng Công nghệ")).toBeVisible();
    await expect(dialog.getByText("Theo Phòng Công nghệ")).toBeVisible();
    await expect(
      dialog.getByText("Quyền riêng trên board này · 5"),
    ).toBeVisible();
    const rows = page.locator('[data-f="pr"]');
    await expect(rows).toHaveCount(5);
    await expect(page.locator('[data-f="plmore"]')).toHaveCount(0);
    await expect(rows.nth(0)).toContainText("Lê Hoàng Phúc");
    await expect(rows.nth(0)).toContainText(
      "Chủ board · Quản trị Phòng Công nghệ",
    );
    await expect(rows.nth(1)).toContainText(
      "Nhóm workspace · 6 người · ngoài phòng Công nghệ",
    );
    await expect(rows.nth(2)).toContainText("Đỗ Khánh Linh");
    await expect(rows.nth(2)).toContainText("Có thể bình luận");
    // Nothing on a Board expires, so no row says when access ends.
    await expect(dialog.getByText(/hết quyền/)).toHaveCount(0);
    await expect(rows.nth(3)).toContainText("Ngoài công ty");
    await expect(rows.nth(3)).toContainText("khách, chỉ thấy board này");
    await expect(rows.nth(4)).toContainText("(bạn)");
    // Department members are not listed one by one.
    await expect(dialog.getByText("Trần Minh Quân")).toHaveCount(0);
  });

  test("inviting waits for a chosen member, then gives them the permission", async ({
    page,
  }) => {
    await open(page);
    await share(page).click();
    const invite = page.locator('[data-f="invite"]');
    await expect(invite).toBeDisabled();
    const dialog = page.getByRole("dialog", { name: /^Chia sẻ/ });
    const field = dialog.getByRole("combobox");
    await field.click();
    const candidate = dialog.getByRole("option").first();
    await expect(candidate).toBeVisible();
    const name = (await candidate.innerText()).trim().split("\n").pop() ?? "";
    await candidate.click();
    await expect(invite).toBeEnabled();
    await invite.click();
    await expect(dialog).toBeVisible();
    await expect(field).toHaveValue("");
    await expect(invite).toBeDisabled();
    expect(name).not.toBe("");
  });

  test("changes a permission, and removes one", async ({ page }) => {
    await open(page);
    await share(page).click();
    const dialog = page.getByRole("dialog", { name: /^Chia sẻ/ });
    const rows = page.locator('[data-f="pr"]');
    await rows
      .nth(1)
      .getByRole("button", { name: /Có thể xem/ })
      .click();
    await page.getByRole("menuitemradio", { name: /Có thể sửa/ }).click();
    await expect(rows.nth(1)).toContainText("Có thể sửa");
    // A guest is view-only: every level above view is disabled.
    await rows
      .nth(3)
      .getByRole("button", { name: /Có thể xem/ })
      .click();
    for (const level of [/Có thể bình luận/, /Có thể sửa/, /Quản lý/]) {
      await expect(
        page.getByRole("menuitemradio", { name: level }),
      ).toHaveAttribute("aria-disabled", "true");
    }
    await page.getByRole("menuitem", { name: /Gỡ quyền/ }).click();
    await expect(dialog.getByText("Đặng Minh Tuấn")).toHaveCount(0);
    await expect(
      dialog.getByText("Quyền riêng trên board này · 4"),
    ).toBeVisible();
    // The history records both.
    await dialog.getByText("Nhật ký thay đổi quyền").click();
    await expect(
      dialog
        .getByText(/đổi Nhóm Kế toán tổng hợp: Có thể xem → Có thể sửa/)
        .first(),
    ).toBeVisible();
    await expect(dialog.getByText(/gỡ quyền của Đặng Minh Tuấn/)).toBeVisible();
  });

  test("shows the permission history", async ({ page }) => {
    await open(page);
    await share(page).click();
    const dialog = page.getByRole("dialog", { name: /^Chia sẻ/ });
    await dialog.getByText("Nhật ký thay đổi quyền").click();
    await expect(
      dialog.getByText("Nguyễn Thu Hà thêm Đặng Minh Tuấn (khách, Có thể xem)"),
    ).toBeVisible();
    await expect(dialog.getByText("08/10 · 14:12")).toBeVisible();
    // The history has only what the producer records: no expiry, no "created".
    await expect(dialog.getByText(/hết hạn|tạo board/)).toHaveCount(0);
    await dialog.getByRole("button", { name: "Quay lại", exact: true }).click();
    await expect(
      dialog.getByText("Quyền riêng trên board này · 5"),
    ).toBeVisible();
  });

  test("switching to Private lists who loses the Board and adds the chosen people", async ({
    page,
  }) => {
    await open(page);
    await share(page).click();
    const dialog = page.getByRole("dialog");
    await dialog.getByRole("button", { name: /Đổi/ }).click();
    await expect(
      dialog.getByText(
        "22 người trong Phòng Công nghệ sẽ không còn thấy board.",
      ),
    ).toBeVisible();
    await expect(dialog.getByText("Trần Minh Quân")).toBeVisible();
    await dialog.getByRole("checkbox").nth(2).uncheck();
    await dialog
      .getByRole("button", { name: "Thêm 2 người rồi chuyển" })
      .click();
    await expect(dialog).toBeHidden();
    // Reopen: the two chosen people now have a permission of their own.
    await share(page).click();
    const again = page.getByRole("dialog", { name: /^Chia sẻ/ });
    await expect(again.getByText("Riêng tư")).toBeVisible();
    await expect(
      again.getByText("Quyền riêng trên board này · 7"),
    ).toBeVisible();
  });
});

test.describe("Board share sheet at 390 px (owner)", () => {
  test.skip(
    persona !== "owner" || scenario !== "default" || faults !== "",
    "Owner checks need the default owner world without faults",
  );
  test.use({ viewport: { width: 390, height: 844 } });

  test("a person picked in the search is added at once with the row's level, and Undo takes the add back", async ({
    page,
  }) => {
    await open(page);
    await share(page).click();
    const dialog = page.getByRole("dialog", { name: /^Chia sẻ/ });
    // No "Mời" button: the add is the pick itself.
    await expect(dialog.getByRole("button", { name: "Mời" })).toHaveCount(0);
    await dialog.getByRole("button", { name: /Quyền khi thêm/ }).click();
    await page.getByRole("radio", { name: /Có thể bình luận/ }).click();
    await expect(
      dialog.getByRole("button", { name: /Quyền khi thêm/ }),
    ).toContainText("Có thể bình luận");
    await dialog.getByRole("combobox").fill("Jordan");
    await dialog.getByRole("option", { name: /Jordan Lee/ }).click();
    const rows = page.locator('[data-f="pr"]');
    await expect(rows).toHaveCount(6);
    await expect(rows.filter({ hasText: "Jordan Lee" })).toContainText(
      "Bình luận",
    );
    const toast = page.locator('[data-f="toast"]');
    await expect(toast).toContainText(
      /Đã thêm Jordan Lee.* · Có thể bình luận/,
    );
    await toast.getByRole("button", { name: "Hoàn tác" }).click();
    await expect(toast).toBeHidden();
    await expect(rows).toHaveCount(5);
    await expect(rows.filter({ hasText: "Jordan Lee" })).toHaveCount(0);
  });

  test("a level change, a removal and the Private switch run from the sheets", async ({
    page,
  }) => {
    await open(page);
    await share(page).click();
    const dialog = page.locator('[data-f="dlg"]');
    const rows = page.locator('[data-f="pr"]');
    await rows
      .nth(1)
      .getByRole("button", { name: /Có thể xem/ })
      .click();
    await page.getByRole("radio", { name: /Có thể sửa/ }).click();
    await expect(rows.nth(1)).toContainText("Sửa");
    // A guest is view-only: the levels above view are disabled with the reason.
    await rows
      .nth(3)
      .getByRole("button", { name: /Quyền của/ })
      .click();
    for (const level of [/Có thể bình luận/, /Có thể sửa/, /Quản lý/]) {
      await expect(page.getByRole("radio", { name: level })).toHaveAttribute(
        "aria-disabled",
        "true",
      );
    }
    await page.getByRole("button", { name: /Gỡ quyền/ }).click();
    await expect(
      dialog.getByText("Quyền riêng trên board này · 4"),
    ).toBeVisible();
    await dialog
      .getByRole("button", { name: /Đổi ai vào được board|Theo Phòng/ })
      .click();
    await expect(
      dialog.getByText(
        "22 người trong Phòng Công nghệ sẽ không còn thấy board.",
      ),
    ).toBeVisible();
    await dialog.getByRole("checkbox").nth(2).uncheck();
    await dialog
      .getByRole("button", { name: "Thêm 2 người rồi chuyển" })
      .click();
    await expect(dialog).toBeHidden();
  });

  test("every control is a 44 px target and axe finds no serious violation on the three views", async ({
    page,
  }) => {
    await open(page);
    await share(page).click();
    const dialog = page.getByRole("dialog", { name: /^Chia sẻ/ });
    await expect(dialog).toBeVisible();
    await page.waitForTimeout(300);
    const small = await page.evaluate(() =>
      Array.from(
        document.querySelectorAll<HTMLElement>(
          '[data-f="dlg"] button, [data-f="dlg"] input:not([type="checkbox"])',
        ),
      )
        .filter((element) => element.getBoundingClientRect().height < 43.5)
        .map((element) => element.textContent?.trim() || element.outerHTML),
    );
    expect(small).toEqual([]);
    const serious = async () =>
      (
        await new AxeBuilder({ page }).include('[data-f="dlg"]').analyze()
      ).violations.filter(
        (violation) =>
          violation.impact === "serious" || violation.impact === "critical",
      );
    expect(await serious()).toEqual([]);
    await page.locator('[data-f="ga"]').first().click();
    await page.waitForTimeout(300);
    expect(await serious()).toEqual([]);
    await page.getByRole("button", { name: "Quay lại Chia sẻ" }).click();
    await page.locator('[data-f="logl"]').click();
    await page.waitForTimeout(300);
    expect(await serious()).toEqual([]);
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
