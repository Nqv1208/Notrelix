import { expect, test, type Page } from "@playwright/test";

/**
 * Teams inside "Thiết lập workspace" (Final-Teams): the list with its people,
 * lead and where each Team is used, create with a lead and people, one Team's
 * people and roles, archive and restore, search, the sharing and security
 * page and what a member who cannot manage Teams sees. The mock world resets
 * on every page load, so the flows navigate inside the app.
 */
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const workspace = "/workspaces/mock-workspace-primary";

async function open(page: Page, path: string) {
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "vi");
      window.localStorage.setItem("notrelix-system-rail", "pinned");
    } catch {
      // Private mode: the defaults apply.
    }
  });
  await page.goto(`${workspace}${path}`);
}

const table = (page: Page) =>
  page.getByRole("table", { name: "Nhóm trong Minh An Group" });

test.describe("Teams (owner)", () => {
  test.skip(
    scenario !== "default" || persona !== "owner" || faults !== "",
    "Manages Teams: needs the default world and a workspace owner",
  );
  test.use({ viewport: { width: 1440, height: 1000 } });

  test("lists the four Teams in use and the archived one, with people, lead and where they are used", async ({
    page,
  }) => {
    await open(page, "/teams");
    await expect(
      page.getByRole("heading", { level: 1, name: "Thiết lập workspace" }),
    ).toBeVisible();
    await expect(
      page.getByRole("heading", { name: "Nhóm", level: 2 }),
    ).toBeVisible();
    await expect(
      page.getByText("4 nhóm đang dùng · 1 đã lưu trữ"),
    ).toBeVisible();
    const rows = table(page).getByRole("row");
    await expect(rows).toHaveCount(5);
    await expect(rows.nth(1)).toContainText("Nhóm Kỹ sư ERP");
    await expect(rows.nth(1)).toContainText("4 người");
    await expect(rows.nth(1)).toContainText("Trần Minh Quân");
    await expect(rows.nth(1)).toContainText("Phòng Công nghệ · Thành viên");
    await expect(rows.nth(2)).toContainText("Ban giám đốc");
    await expect(rows.nth(2)).toContainText("Phòng Kế toán · Người xem");
    await expect(rows.nth(3)).toContainText("Nhóm Kế toán tổng hợp");
    await expect(rows.nth(3)).toContainText("6 người");
    await expect(rows.nth(3)).toContainText(
      "Board Triển khai ERP Q4 · Có thể xem",
    );
    await expect(rows.nth(4)).toContainText("Chưa dùng ở đâu");
    await expect(page.getByText("Đã lưu trữ 1")).toBeVisible();
    const archived = page.getByRole("table", { name: "Nhóm đã lưu trữ" });
    await expect(archived).toContainText("Lưu trữ 20/09 · Nguyễn Thu Hà");
    await expect(archived).toContainText("Hạ tầng & bảo mật · tạm ngừng");
    // The left menu of the setup frame, with its counts.
    const menu = page.getByRole("navigation", {
      name: "Mục thiết lập workspace",
    });
    await expect(
      menu.getByRole("link", { name: /^Nhóm\s*4$/ }),
    ).toHaveAttribute("aria-current", "page");
    await expect(
      menu.getByRole("link", { name: /^Thành viên\s*86$/ }),
    ).toBeVisible();
    await expect(
      menu.getByRole("link", { name: "Chia sẻ và bảo mật" }),
    ).toBeVisible();
  });

  test("searches by Team name or person, ignoring marks", async ({ page }) => {
    await open(page, "/teams");
    await page.getByRole("searchbox").fill("vo thi hanh");
    await expect(table(page).getByRole("row")).toHaveCount(2);
    await expect(table(page)).toContainText("Nhóm Kế toán tổng hợp");
    await page.getByRole("searchbox").fill("khong co nhom nay");
    await expect(page.getByText("Không có nhóm nào khớp.")).toBeVisible();
  });

  test("creates a Team with a lead and people, then manages its people, archives and restores it", async ({
    page,
  }) => {
    await open(page, "/teams");
    await page.getByRole("button", { name: "Tạo nhóm" }).click();
    const dialog = page.getByRole("dialog", { name: "Tạo nhóm" });
    await dialog.getByLabel("Tên nhóm").fill("Nhóm Triển khai kho");
    await dialog.getByRole("combobox", { name: /Trưởng nhóm/ }).click();
    await page
      .getByRole("option", { name: "Đặng Quốc Bảo · bao.dang@minhan.vn" })
      .click();
    const members = dialog.getByPlaceholder("Gõ tên hoặc email");
    await members.fill("minh.lam");
    await page.keyboard.press("Enter");
    await expect(
      dialog.getByText("2 người · Đặng Quốc Bảo là trưởng nhóm"),
    ).toBeVisible();
    await dialog.getByRole("button", { name: "Tạo nhóm" }).click();
    await expect(page.getByText("Đã tạo Nhóm Triển khai kho.")).toBeVisible();
    await expect(page).toHaveURL(/\/teams\/[^/]+$/);
    await expect(
      page.getByRole("heading", { level: 2, name: "Nhóm Triển khai kho" }),
    ).toBeVisible();
    // The lead and the person added; the creator who made it is not in it.
    await expect(
      page.getByText("Đặng Quốc Bảo", { exact: true }),
    ).toBeVisible();
    await expect(page.getByText("Lâm Gia Minh", { exact: true })).toBeVisible();
    await expect(
      page
        .getByRole("region", { name: /Người trong nhóm/ })
        .getByText("Nguyễn Thu Hà"),
    ).toHaveCount(0);
    // The only lead cannot be lowered or removed.
    await expect(
      page.getByRole("button", { name: "Gỡ Đặng Quốc Bảo khỏi nhóm" }),
    ).toBeDisabled();
    // Promote the other person, then remove the first lead.
    await page
      .getByRole("button", { name: "Vai trò của Lâm Gia Minh: Thành viên" })
      .click();
    await page.getByRole("menuitemradio", { name: /Trưởng nhóm/ }).click();
    await expect(
      page.getByText("Lâm Gia Minh giờ là Trưởng nhóm."),
    ).toBeVisible();
    await page
      .getByRole("button", { name: "Gỡ Đặng Quốc Bảo khỏi nhóm" })
      .click();
    await expect(
      page.getByText("Đã xóa Đặng Quốc Bảo khỏi nhóm."),
    ).toBeVisible();
    // Add a person by name.
    await page.getByRole("combobox").fill("Nguyễn Văn Hùng");
    await page.keyboard.press("Enter");
    await expect(
      page.getByText("Đã thêm Nguyễn Văn Hùng vào nhóm."),
    ).toBeVisible();
    // Archive with a confirmation, find it among the archived, restore it.
    await page.getByRole("button", { name: "Lưu trữ nhóm" }).click();
    await page
      .getByRole("dialog", { name: "Lưu trữ Nhóm Triển khai kho?" })
      .getByRole("button", { name: "Lưu trữ nhóm" })
      .click();
    await expect(
      page.getByText("Đã lưu trữ Nhóm Triển khai kho."),
    ).toBeVisible();
    await page.getByRole("link", { name: "Nhóm", exact: true }).first().click();
    await expect(page.getByText("Đã lưu trữ 2")).toBeVisible();
    await page
      .getByRole("button", { name: "Khôi phục Nhóm Triển khai kho" })
      .click();
    await expect(
      page.getByText("Đã bỏ lưu trữ Nhóm Triển khai kho."),
    ).toBeVisible();
    await expect(table(page)).toContainText("Nhóm Triển khai kho");
  });

  test("archives a Team from the row menu after saying who loses access", async ({
    page,
  }) => {
    await open(page, "/teams");
    await page
      .getByRole("button", { name: "Thao tác với Nhóm Kỹ sư ERP" })
      .click();
    await page.getByRole("menuitem", { name: "Lưu trữ nhóm" }).click();
    const dialog = page.getByRole("dialog", {
      name: "Lưu trữ Nhóm Kỹ sư ERP?",
    });
    await expect(
      dialog.getByText(/3 người sẽ mất quyền vào Phòng Công nghệ/),
    ).toBeVisible();
    await expect(dialog.getByText(/Trần Minh Quân vẫn còn/)).toBeVisible();
    await dialog.getByRole("button", { name: "Lưu trữ nhóm" }).click();
    await expect(page.getByText("Đã lưu trữ Nhóm Kỹ sư ERP.")).toBeVisible();
    await expect(
      page.getByText("3 nhóm đang dùng · 2 đã lưu trữ"),
    ).toBeVisible();
  });

  test("one Team shows its people with email and job, and where it has access", async ({
    page,
  }) => {
    await open(page, "/teams");
    await table(page)
      .getByRole("link", { name: /Nhóm Kỹ sư ERP/ })
      .click();
    await expect(
      page.getByRole("heading", { level: 2, name: "Nhóm Kỹ sư ERP" }),
    ).toBeVisible();
    await expect(
      page.getByText(
        "Kỹ sư triển khai và dữ liệu ERP · tạo 01/09 bởi Nguyễn Thu Hà",
      ),
    ).toBeVisible();
    await expect(
      page.getByText("quan.tran@minhan.vn · Kỹ sư dữ liệu"),
    ).toBeVisible();
    await expect(
      page.getByText("bao.dang@minhan.vn · Kỹ sư tích hợp"),
    ).toBeVisible();
    await expect(page.getByText("từ 15/09")).toBeVisible();
    await expect(
      page.getByText("Vai trò Thành viên · thêm 01/09"),
    ).toBeVisible();
    // The role menu of the only lead says why it cannot change.
    await page
      .getByRole("button", { name: "Vai trò của Trần Minh Quân: Trưởng nhóm" })
      .click();
    await expect(
      page.getByText("Không hạ được: Quân là trưởng nhóm duy nhất"),
    ).toBeVisible();
    await page.keyboard.press("Escape");
    await expect(page.getByRole("menu")).toHaveCount(0);
  });

  test("the sharing and security page switches the sharing links through the workspace policy", async ({
    page,
  }) => {
    await open(page, "/teams");
    await page.getByRole("link", { name: "Chia sẻ và bảo mật" }).click();
    await expect(page).toHaveURL(/\/security$/);
    await expect(
      page.getByRole("heading", { level: 2, name: "Chia sẻ và bảo mật" }),
    ).toBeVisible();
    await expect(page.getByText("2 khách đang có")).toBeVisible();
    await expect(page.getByLabel("Tên miền công ty")).toHaveValue("minhan.vn");
    const toggle = page.getByRole("switch", { name: "Liên kết chia sẻ" });
    await expect(toggle).toHaveAttribute("aria-checked", "false");
    await expect(
      page.getByText(
        /Đang tắt từ 15\/09 · Không ai tạo được liên kết chia sẻ mới/,
      ),
    ).toBeVisible();
    await toggle.click();
    await expect(toggle).toHaveAttribute("aria-checked", "true");
    await expect(
      page.getByText("Liên kết chia sẻ tạo và dùng được."),
    ).toBeVisible();
  });
});

test.describe("Teams (member)", () => {
  test.skip(
    scenario !== "default" || persona !== "member" || faults !== "",
    "Needs the Member persona",
  );
  test.use({ viewport: { width: 1440, height: 1000 } });

  test("a member who cannot manage Teams is told whom to ask", async ({
    page,
  }) => {
    await open(page, "/teams");
    await expect(
      page.getByRole("heading", {
        name: "Chỉ quản trị workspace quản lý nhóm.",
      }),
    ).toBeVisible();
    await expect(page.getByRole("table")).toHaveCount(0);
    await expect(
      page.getByRole("link", { name: "Về Trang chủ" }),
    ).toBeVisible();
  });
});
