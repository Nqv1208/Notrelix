import { expect, test, type Page } from "@playwright/test";

/**
 * The department (Space) page as a person uses it: the Create menu with a
 * keyboard, the settings (name, visibility, move, archive, delete), the first
 * steps of an empty department and what a member who does not manage the
 * department sees. The mock world resets on every page load, so each test
 * starts from the seeded departments (Engineering is Private with the Platform
 * folder inside, Accounting is Private, Marketing is open, Nhân sự is open and empty).
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const WORKSPACE = "/workspaces/mock-workspace-primary";
const ENGINEERING = `${WORKSPACE}/spaces/mock-space-engineering`;
const EMPTY_DEPARTMENT = `${WORKSPACE}/spaces/mock-space-hr`;

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
}

const create = (page: Page) =>
  page.getByRole("button", { name: "Tạo mới", exact: true });

test.describe("Department page (owner)", () => {
  test.skip(
    persona !== "owner" || scenario !== "default" || faults !== "",
    "Owner checks need the default owner world without faults",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  test("the Create menu works with a keyboard and gives focus back", async ({
    page,
  }) => {
    await open(page, ENGINEERING, "Engineering");
    const trigger = create(page);
    await expect(trigger).toHaveAttribute("aria-expanded", "false");
    await trigger.focus();
    await page.keyboard.press("Enter");
    const menu = page.getByRole("menu", { name: "Tạo mới trong Engineering" });
    await expect(menu).toBeVisible();
    await expect(trigger).toHaveAttribute("aria-expanded", "true");
    const items = menu.getByRole("menuitem");
    await expect(items).toHaveCount(4);
    await expect(items.nth(0)).toBeFocused();
    await page.keyboard.press("ArrowDown");
    await expect(items.nth(1)).toBeFocused();
    await page.keyboard.press("ArrowUp");
    await page.keyboard.press("ArrowUp");
    await expect(items.nth(3)).toBeFocused();
    // The template and sub-department items that cannot run say why.
    await expect(items.nth(3)).toHaveAttribute("aria-disabled", "true");
    await page.keyboard.press("Escape");
    await expect(menu).toBeHidden();
    await expect(trigger).toBeFocused();

    // A press outside closes it without taking focus.
    await trigger.click();
    await expect(menu).toBeVisible();
    await page.getByRole("heading", { level: 1 }).click();
    await expect(menu).toBeHidden();
    // Tab leaves the menu (it is not a focus trap).
    await trigger.click();
    await page.keyboard.press("Tab");
    await expect(menu).toBeHidden();
  });

  test("B and D create a Board and a Doc in the department", async ({
    page,
  }) => {
    await open(page, ENGINEERING, "Engineering");
    await create(page).click();
    await page.keyboard.press("b");
    const board = page.getByRole("dialog", { name: "Bảng mới" });
    await expect(board).toBeVisible();
    await expect(board.getByText(/Engineering/)).toBeVisible();
    await page.keyboard.press("Escape");
    await expect(board).toBeHidden();
    await expect(create(page)).toBeFocused();
    await create(page).click();
    await page.keyboard.press("d");
    await expect(
      page.getByRole("dialog", { name: "Tài liệu mới" }),
    ).toBeVisible();
  });

  test("the settings icon and the tabs move between the sections", async ({
    page,
  }) => {
    await open(page, ENGINEERING, "Engineering");
    const tabs = page.getByRole("navigation", { name: "Engineering" });
    await expect(
      tabs.getByRole("link", { name: /^Tổng quan/ }),
    ).toHaveAttribute("aria-current", "page");
    await page.getByRole("button", { name: "Thiết lập phòng ban" }).click();
    await expect(page).toHaveURL(/tab=settings/);
    await expect(tabs.getByRole("link", { name: "Thiết lập" })).toHaveAttribute(
      "aria-current",
      "page",
    );
    await expect(
      page.getByRole("button", { name: "Thiết lập phòng ban" }),
    ).toHaveAttribute("aria-pressed", "true");
    // The icon toggles back to the overview.
    await page.getByRole("button", { name: "Thiết lập phòng ban" }).click();
    await expect(page).not.toHaveURL(/tab=/);
    await tabs.getByRole("link", { name: /^Thành viên/ }).click();
    await expect(page).toHaveURL(/tab=members/);
    await expect(
      page.getByRole("table", { name: "Thành viên Engineering" }),
    ).toBeVisible();
    // Add members from the head goes to the same tab.
    await tabs.getByRole("link", { name: /^Tổng quan/ }).click();
    await page.getByRole("link", { name: "Thêm thành viên" }).click();
    await expect(page).toHaveURL(/tab=members/);
  });

  test("Boards and Docs has no page of its own: the tab is inert with the reason", async ({
    page,
  }) => {
    await open(page, ENGINEERING, "Engineering");
    const tab = page
      .getByRole("navigation", { name: "Engineering" })
      .getByText(/^Board và tài liệu/);
    await expect(tab).toHaveAttribute("aria-disabled", "true");
    await expect(tab).toHaveAttribute("title", /chưa có trang riêng/);
    const url = page.url();
    await tab.click({ force: true });
    expect(page.url()).toBe(url);
  });

  test("renaming saves, shows the new name and the sidebar follows", async ({
    page,
  }) => {
    await open(page, `${ENGINEERING}?tab=settings`, "Engineering");
    const save = page.getByRole("button", { name: "Lưu", exact: true });
    await expect(save).toHaveAttribute("aria-disabled", "true");
    await page.getByRole("textbox", { name: "Tên phòng ban" }).fill("Kỹ thuật");
    await expect(save).not.toHaveAttribute("aria-disabled", "true");
    await save.click();
    await expect(page.getByText("Đã lưu cài đặt")).toBeVisible();
    await expect(
      page.getByRole("heading", { level: 1, name: "Kỹ thuật" }),
    ).toBeVisible();
    await expect(save).toHaveAttribute("aria-disabled", "true");
  });

  test("visibility is chosen with a click or the arrow keys and saved at once", async ({
    page,
  }) => {
    await open(page, `${ENGINEERING}?tab=settings`, "Engineering");
    const radios = page.getByRole("radio");
    await expect(radios.nth(0)).toHaveAttribute("aria-checked", "true");
    await radios.nth(1).click();
    await expect(radios.nth(1)).toHaveAttribute("aria-checked", "true");
    await expect(radios.nth(0)).toHaveAttribute("aria-checked", "false");
    // The head chip follows the saved visibility.
    await expect(page.locator('[data-f="chip"]').first()).toHaveText(/Mở/);
    await radios.nth(1).focus();
    await page.keyboard.press("ArrowUp");
    await expect(radios.nth(0)).toHaveAttribute("aria-checked", "true");
    await expect(radios.nth(0)).toBeFocused();
  });

  test("Move opens the picker and Escape returns focus to the button", async ({
    page,
  }) => {
    await open(page, `${ENGINEERING}?tab=settings`, "Engineering");
    const move = page.getByRole("button", { name: "Chuyển", exact: true });
    await move.click();
    const dialog = page.getByRole("dialog");
    await expect(dialog).toBeVisible();
    await page.keyboard.press("Escape");
    await expect(dialog).toBeHidden();
    await expect(move).toBeFocused();
  });

  test("Archive is blocked while the department has content, with the reason", async ({
    page,
  }) => {
    await open(page, `${ENGINEERING}?tab=settings`, "Engineering");
    const archive = page.getByRole("button", { name: "Lưu trữ", exact: true });
    await expect(archive).toHaveAttribute("aria-disabled", "true");
    await expect(page.getByText(/còn 1 phòng ban con/)).toBeVisible();
    await archive.click({ force: true });
    await expect(page.getByRole("alertdialog")).toBeHidden();
  });

  test("Archive of an empty department asks first and Delete stays inert", async ({
    page,
  }) => {
    await open(page, `${EMPTY_DEPARTMENT}?tab=settings`, "Nhân sự");
    const archive = page.getByRole("button", { name: "Lưu trữ", exact: true });
    await expect(archive).not.toHaveAttribute("aria-disabled", "true");
    await archive.click();
    const confirm = page.getByRole("alertdialog", {
      name: "Lưu trữ Nhân sự?",
    });
    await expect(confirm).toBeVisible();
    await page.keyboard.press("Escape");
    await expect(confirm).toBeHidden();
    await expect(archive).toBeFocused();
    const remove = page.getByRole("button", { name: "Xoá phòng ban" });
    await expect(remove).toHaveAttribute("aria-disabled", "true");
    await remove.click({ force: true });
    await expect(page.getByRole("group")).toHaveCount(0);
  });

  test("an empty department shows the first steps and they lead somewhere", async ({
    page,
  }) => {
    await open(page, EMPTY_DEPARTMENT, "Nhân sự");
    await expect(page.getByText("Bắt đầu với Nhân sự")).toBeVisible();
    // Open to the workspace: the card does not claim it is private.
    await expect(
      page.getByText("Mở cho cả workspace, bạn là quản trị"),
    ).toBeVisible();
    await page.getByRole("button", { name: "Board trống" }).click();
    const board = page.getByRole("dialog", { name: "Bảng mới" });
    await expect(board).toBeVisible();
    await page.keyboard.press("Escape");
    await expect(board).toBeHidden();
    // Templates have no producer: the section is not drawn.
    await expect(page.getByText("Dùng mẫu")).toHaveCount(0);
    await page
      .getByRole("main")
      .getByRole("button", { name: "Thêm thành viên" })
      .click();
    await expect(page).toHaveURL(/tab=members/);
  });

  test("the keyboard order runs through the head and the tabs", async ({
    page,
  }) => {
    await open(page, ENGINEERING, "Engineering");
    await page.getByRole("link", { name: "Thêm thành viên" }).focus();
    await page.keyboard.press("Tab");
    await expect(create(page)).toBeFocused();
    await page.keyboard.press("Tab");
    await expect(
      page.getByRole("button", { name: "Thiết lập phòng ban" }),
    ).toBeFocused();
    await page.keyboard.press("Tab");
    await expect(
      page
        .getByRole("navigation", { name: "Engineering" })
        .getByRole("link", { name: /^Tổng quan/ }),
    ).toBeFocused();
  });
});

test.describe("Department page for a member who does not manage it", () => {
  test.skip(
    persona !== "member" || scenario !== "default" || faults !== "",
    "Needs the Member persona (Jordan Lee, a Member of Engineering)",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  test("offers Create but not Add members, Settings or sub-departments", async ({
    page,
  }) => {
    await open(page, ENGINEERING, "Engineering");
    await expect(
      page.getByRole("link", { name: "Thêm thành viên" }),
    ).toHaveCount(0);
    await expect(
      page.getByRole("button", { name: "Thiết lập phòng ban" }),
    ).toHaveCount(0);
    await expect(
      page
        .getByRole("navigation", { name: "Engineering" })
        .getByRole("link", { name: "Thiết lập" }),
    ).toHaveCount(0);
    await create(page).click();
    const child = page.getByRole("menuitem", { name: /^Phòng ban con/ });
    await expect(child).toHaveAttribute("aria-disabled", "true");
    await expect(child).toHaveAttribute("title", /Chỉ quản trị phòng/);
    await expect(
      page.getByRole("menuitem", { name: /^Board/ }),
    ).not.toHaveAttribute("aria-disabled", "true");
    await page.keyboard.press("Escape");
    // The settings route read through the URL is read-only.
    await page.goto(`${ENGINEERING}?tab=settings`);
    await expect(
      page.getByRole("textbox", { name: "Tên phòng ban" }),
    ).toBeDisabled();
    await expect(
      page.getByRole("button", { name: "Lưu", exact: true }),
    ).toHaveCount(0);
    await expect(page.getByRole("radio").first()).toHaveAttribute(
      "aria-disabled",
      "true",
    );
  });
});

test.describe("Department page for a guest", () => {
  test.skip(
    persona !== "viewer" || scenario !== "default" || faults !== "",
    "Needs the viewer persona (a guest of the workspace)",
  );

  test("a private department the guest cannot see reads as not found", async ({
    page,
  }) => {
    await page.addInitScript(() => {
      try {
        window.localStorage.setItem("notrelix-locale", "vi");
      } catch {
        // Private mode: the defaults apply.
      }
    });
    await page.goto(ENGINEERING);
    await expect(page.getByText("Không có phòng ban này")).toBeVisible();
    await expect(
      page.getByRole("link", { name: "Về Trang chủ" }),
    ).toBeVisible();
  });
});
