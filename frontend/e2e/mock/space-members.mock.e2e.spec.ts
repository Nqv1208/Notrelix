import { expect, test, type Page } from "@playwright/test";

/**
 * The department members tab as a person uses it: add people and teams, change
 * a role, remove a person or a team, filter, and the keyboard and focus of the
 * role menu and the two dialogs. The mock world resets on every page load.
 * Kế toán holds the owner and Mai Quốc (Admin) and the Helpdesk Team (Viewer: the owner,
 * Jordan and Taylor); Công nghệ holds the owner (Admin), Jordan (Member) and
 * the admin (Viewer).
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const WORKSPACE = "/workspaces/mock-workspace-primary";
const ACCOUNTING = `${WORKSPACE}/spaces/mock-space-accounting?tab=members`;
const ENGINEERING = `${WORKSPACE}/spaces/mock-space-engineering?tab=members`;

async function open(page: Page, path: string, heading: string) {
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "vi");
      window.localStorage.setItem("notrelix-system-rail", "pinned");
    } catch {
      // Private mode: the defaults apply.
    }
  });
  await page.goto(path);
  await expect(
    page.getByRole("heading", { level: 1, name: heading }),
  ).toBeVisible();
  await expect(page.locator('[data-f="mr"]').first()).toBeVisible();
}

const row = (page: Page, name: string | RegExp) =>
  page.locator('[data-f="mr"]').filter({ hasText: name });

test.describe("Department members (owner)", () => {
  test.skip(
    persona !== "owner" || scenario !== "default" || faults !== "",
    "Owner checks need the default owner world without faults",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  test("adds a person: type, pick with Enter, choose the role, add", async ({
    page,
  }) => {
    await open(page, ACCOUNTING, "Kế toán");
    await page.getByRole("button", { name: "Thêm thành viên" }).click();
    const dialog = page.getByRole("dialog", { name: "Thêm vào Phòng Kế toán" });
    await expect(dialog).toBeVisible();
    // Focus starts in the field and Tab stays inside the dialog.
    const field = dialog.getByRole("combobox", {
      name: "Thêm người hoặc nhóm",
    });
    await expect(field).toBeFocused();
    const submit = dialog.getByRole("button", { name: /^Thêm/ });
    await expect(submit).toHaveAttribute("aria-disabled", "true");
    await field.fill("jordan");
    await expect(
      dialog.getByRole("option", { name: /Jordan Lee/ }),
    ).toBeVisible();
    await page.keyboard.press("Enter");
    await expect(
      dialog.getByRole("button", { name: /Bỏ Jordan Lee/ }),
    ).toBeVisible();
    await dialog.getByRole("button", { name: /Thành viên/ }).click();
    await page.getByRole("option", { name: /Người xem/ }).click();
    await expect(dialog.getByText("1 người sẽ thấy ngay")).toBeVisible();
    await submit.click();
    await expect(dialog).toBeHidden();
    await expect(page.getByText("Đã thêm Jordan Lee (Member).")).toBeVisible();
    const jordan = row(page, "Jordan Lee");
    await expect(jordan).toBeVisible();
    await expect(
      jordan.getByRole("button", { name: /Người xem/ }),
    ).toBeVisible();
    await expect(
      page.getByText(/2 quản trị · 0 thành viên · 2 người xem/),
    ).toBeVisible();
    // Focus returned to the button that opened the dialog.
    await expect(
      page.getByRole("button", { name: "Thêm thành viên" }),
    ).toBeFocused();
  });

  test("an email that is not a workspace member is skipped with the reason", async ({
    page,
  }) => {
    await open(page, ACCOUNTING, "Kế toán");
    await page.getByRole("button", { name: "Thêm thành viên" }).click();
    const dialog = page.getByRole("dialog");
    const field = dialog.getByRole("combobox");
    await field.fill("khach@doitac.vn");
    await page.keyboard.press("Enter");
    await expect(dialog.getByRole("alert")).toContainText(
      "chưa là thành viên workspace",
    );
    await expect(
      dialog.getByRole("link", { name: "Mời vào workspace" }),
    ).toHaveAttribute("href", /members/);
    // Nothing valid to add: the button stays inert.
    await expect(dialog.getByRole("button", { name: /^Thêm/ })).toHaveAttribute(
      "aria-disabled",
      "true",
    );
    await dialog.getByRole("button", { name: /Bỏ khach@doitac.vn/ }).click();
    await expect(dialog.getByRole("alert")).toHaveCount(0);
    await dialog.getByRole("button", { name: "Huỷ" }).click();
    await expect(dialog).toBeHidden();
  });

  test("changes a role with the keyboard and focus returns to the role button", async ({
    page,
  }) => {
    await open(page, ENGINEERING, "Công nghệ");
    // The list is long and sorted by role: narrow it so the row stays in view.
    await page.getByRole("searchbox").fill("jordan");
    const jordan = row(page, "Jordan Lee");
    const trigger = jordan.getByRole("button", { name: /Thành viên/ });
    await trigger.focus();
    await page.keyboard.press("Enter");
    const menu = page.getByRole("menu", { name: /Đổi vai trò của Jordan Lee/ });
    await expect(menu).toBeVisible();
    const items = menu.getByRole("menuitemradio");
    await expect(items.nth(1)).toBeFocused();
    await expect(items.nth(1)).toHaveAttribute("aria-checked", "true");
    await page.keyboard.press("ArrowDown");
    await expect(items.nth(2)).toBeFocused();
    await page.keyboard.press("Enter");
    await expect(menu).toBeHidden();
    await expect(
      page.getByText(/Jordan Lee \(Member\) .*Người xem/),
    ).toBeVisible();
    await expect(
      jordan.getByRole("button", { name: /Người xem/ }),
    ).toBeFocused();
  });

  test("Escape closes the role menu and a press outside does too", async ({
    page,
  }) => {
    await open(page, ENGINEERING, "Công nghệ");
    const trigger = row(page, "Jordan Lee").getByRole("button", {
      name: /Thành viên/,
    });
    await trigger.click();
    await expect(page.getByRole("menu")).toBeVisible();
    await page.keyboard.press("Escape");
    await expect(page.getByRole("menu")).toBeHidden();
    await expect(trigger).toBeFocused();
    await trigger.click();
    await page.getByRole("heading", { level: 1 }).click();
    await expect(page.getByRole("menu")).toBeHidden();
  });

  test("the only admin cannot be lowered or removed, and the menu says why", async ({
    page,
  }) => {
    // Phòng Nhân sự has one admin: the owner.
    await open(
      page,
      `${WORKSPACE}/spaces/mock-space-hr?tab=members`,
      "Nhân sự",
    );
    await row(page, "Nguyễn Thu Hà")
      .getByRole("button", { name: /Quản trị/ })
      .click();
    const menu = page.getByRole("menu");
    await expect(menu.getByRole("menuitemradio").nth(1)).toHaveAttribute(
      "aria-disabled",
      "true",
    );
    await expect(
      menu.getByRole("menuitem", { name: /Gỡ khỏi phòng/ }),
    ).toHaveAttribute("aria-disabled", "true");
    await expect(
      menu.getByText(/Phòng ban phải còn ít nhất 1 quản trị/),
    ).toBeVisible();
    await menu
      .getByRole("menuitem", { name: /Gỡ khỏi phòng/ })
      .click({ force: true });
    await expect(page.getByRole("dialog")).toHaveCount(0);
  });

  test("removing a team lists who loses access, then removes it", async ({
    page,
  }) => {
    await open(page, ACCOUNTING, "Kế toán");
    const team = row(page, "Helpdesk");
    await team.getByRole("button", { name: "Mở rộng Helpdesk" }).click();
    await expect(page.locator('[data-f="sub"]')).toBeVisible();
    await team.getByRole("button", { name: "Thao tác với Helpdesk" }).click();
    await page.getByRole("menuitem", { name: /Gỡ khỏi phòng/ }).click();
    const dialog = page.getByRole("dialog", {
      name: "Gỡ Helpdesk khỏi phòng?",
    });
    await expect(dialog).toBeVisible();
    // The owner stays: added directly as the admin.
    await expect(
      dialog.getByText(/vẫn là Quản trị, thêm trực tiếp/),
    ).toBeVisible();
    await expect(dialog.getByText(/mất quyền vào phòng/).first()).toBeVisible();
    await page.keyboard.press("Escape");
    await expect(dialog).toBeHidden();
    await team.getByRole("button", { name: "Thao tác với Helpdesk" }).click();
    await page.getByRole("menuitem", { name: /Gỡ khỏi phòng/ }).click();
    await page
      .getByRole("dialog")
      .getByRole("button", { name: "Gỡ nhóm" })
      .click();
    await expect(page.getByText("Đã xóa Helpdesk.")).toBeVisible();
    await expect(row(page, "Helpdesk")).toHaveCount(0);
  });

  test("removing a person asks first", async ({ page }) => {
    await open(page, ENGINEERING, "Công nghệ");
    const jordan = row(page, "Jordan Lee");
    await jordan
      .getByRole("button", { name: "Thao tác với Jordan Lee (Member)" })
      .click();
    await page.getByRole("menuitem", { name: /Gỡ khỏi phòng/ }).click();
    const dialog = page.getByRole("dialog", {
      name: "Gỡ Jordan Lee (Member) khỏi Phòng Công nghệ?",
    });
    await expect(dialog).toBeVisible();
    await dialog.getByRole("button", { name: "Gỡ", exact: true }).click();
    await expect(jordan).toHaveCount(0);
  });

  test("search and the filters narrow the list", async ({ page }) => {
    await open(page, ENGINEERING, "Công nghệ");
    await page.getByRole("searchbox").fill("jordan");
    await expect(page.locator('[data-f="mr"]')).toHaveCount(1);
    await page.getByRole("searchbox").fill("khong co ai ten the");
    await expect(page.getByText("Không có ai khớp.")).toBeVisible();
    await page.getByRole("searchbox").fill("");
    await page.getByRole("button", { name: /Vai trò/ }).click();
    await page.getByRole("menuitemradio", { name: "Người xem" }).click();
    await expect(page.locator('[data-f="mr"]')).toHaveCount(1);
    await expect(
      page.getByRole("button", { name: /Vai trò Người xem/ }),
    ).toBeVisible();
    // The viewers come in through the team Ban giám đốc, not one by one.
    await expect(row(page, "Ban giám đốc")).toBeVisible();
    await page.getByRole("button", { name: /Vào qua/ }).click();
    await page.getByRole("menuitemradio", { name: "Trực tiếp" }).click();
    await expect(page.getByText("Không có ai khớp.")).toBeVisible();
  });

  test("the Add members button, the tab link and the settings icon agree", async ({
    page,
  }) => {
    await open(page, ENGINEERING, "Công nghệ");
    await expect(
      page
        .getByRole("navigation", { name: "Phòng Công nghệ" })
        .getByRole("link", { name: /^Thành viên/ }),
    ).toHaveAttribute("aria-current", "page");
    await page.getByRole("button", { name: "Thiết lập phòng ban" }).click();
    await expect(page).toHaveURL(/tab=settings/);
  });
});

test.describe("Department members for a member who does not manage the department", () => {
  test.skip(
    persona !== "member" || scenario !== "default" || faults !== "",
    "Needs the Member persona (Jordan Lee, a Member of Công nghệ)",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  test("sees the list and the roles but cannot change them", async ({
    page,
  }) => {
    await open(page, ENGINEERING, "Công nghệ");
    await expect(
      page.getByRole("button", { name: "Thêm thành viên" }),
    ).toHaveCount(0);
    const role = page
      .locator('[data-f="mr"]')
      .first()
      .locator('[data-f="rsel"]');
    await expect(role).toHaveAttribute("aria-disabled", "true");
    await role.click({ force: true });
    await expect(page.getByRole("menu")).toHaveCount(0);
    await expect(
      page
        .locator('[data-f="mr"]')
        .first()
        .getByRole("button", { name: /Thao tác với/ }),
    ).toHaveCount(0);
  });
});
