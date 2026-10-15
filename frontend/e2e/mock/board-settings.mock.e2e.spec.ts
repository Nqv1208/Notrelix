import { expect, test, type Page } from "@playwright/test";

/**
 * The board settings page on the mock world (owner persona = board manager):
 * how it opens from each view and goes back, the fields (edit, options, add,
 * archive, restore), the views, the general settings and the danger zone, the
 * groups, the automations, and what a commenter sees. The mock world resets on
 * every page load, so nothing here reloads to prove a write. The failed-write
 * and conflict runs need a fault: VITE_MOCK_FAULTS=<path>=500 or =409.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const SETTINGS_PATH = "/boards/mock-board-erp/settings";
const failedWrite = faults.includes(`${SETTINGS_PATH}/fields/sf-priority=500`);
const conflictWrite = faults.includes(
  `${SETTINGS_PATH}/fields/sf-priority=409`,
);
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const SETTINGS = `${BOARD}/settings`;
const NOW = new Date("2026-10-12T10:40:00+07:00");

async function openSettings(page: Page, query = "") {
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.clock.setFixedTime(NOW);
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "vi");
      window.localStorage.setItem("notrelix-system-rail", "pinned");
    } catch {
      // Private mode: the defaults apply.
    }
  });
  await page.goto(`${SETTINGS}${query}`);
}

const heading = (page: Page) =>
  page.getByRole("heading", { level: 1, name: "Thiết lập board" });
const menu = (page: Page) =>
  page.getByRole("navigation", { name: "Mục thiết lập" });
const fieldPanel = (page: Page, name: string) =>
  page.getByRole("complementary", { name: `Sửa trường ${name}` });
const viewPanel = (page: Page, name: string) =>
  page.getByRole("complementary", { name: `Sửa chế độ xem ${name}` });
const fieldRow = (page: Page, name: string) =>
  page.getByRole("button", { name: `Sửa trường ${name}`, exact: true });

test.describe("Board settings (owner)", () => {
  test.skip(
    persona !== "owner" || scenario !== "default" || faults !== "",
    "Owner checks need the default owner world without faults",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  test("opens from the header of every view and goes back to the same view", async ({
    page,
  }) => {
    await page.setViewportSize({ width: 1440, height: 900 });
    await page.clock.setFixedTime(NOW);
    for (const view of ["kanban", "table", "calendar", "timeline"] as const) {
      await page.goto(`${BOARD}?view=${view}`);
      await page
        .getByRole("button", { name: "Thiết lập", exact: true })
        .click();
      await expect(heading(page)).toBeVisible();
      await expect(page).toHaveURL(new RegExp(`/settings\\?.*from=${view}`));
      await page.getByRole("link", { name: "Về board" }).click();
      await expect(page).toHaveURL(
        new RegExp(`/boards/mock-board-erp\\?.*view=${view}`),
      );
    }
  });

  test("the menu names every section with its count and the page title and crumb", async ({
    page,
  }) => {
    await openSettings(page);
    await expect(heading(page)).toBeVisible();
    const crumb = page.getByRole("navigation", { name: "Vị trí" });
    await expect(crumb).toContainText("Triển khai ERP Q4");
    await expect(crumb).toContainText("Thiết lập");
    await expect(
      page.getByText(
        "Chỉ quản lý board mở được trang này · Mọi thay đổi được ghi vào lịch sử board",
      ),
    ).toBeVisible();
    const counts: Record<string, string> = {
      "Trường dữ liệu": "16",
      "Chế độ xem": "7",
      "Nhóm việc": "4",
      "Tự động hoá": "3",
      "Đã lưu trữ": "1",
    };
    for (const [name, count] of Object.entries(counts)) {
      await expect(
        menu(page).getByRole("button", { name: new RegExp(`^${name}`) }),
      ).toContainText(count);
    }
    // 11 board fields, 5 system fields and 1 archived field.
    await expect(page.getByText("Trường của board")).toBeVisible();
    for (const key of [
      "status",
      "assignee",
      "priority",
      "estimate_hours",
      "title",
      "item_key",
      "created_by…",
    ]) {
      await expect(page.getByText(key, { exact: true })).toBeVisible();
    }
    await expect(
      page.getByText("handover_date", { exact: true }),
    ).toBeVisible();
  });

  test("a field opens in its panel from the row and from the address, and closes", async ({
    page,
  }) => {
    await openSettings(page);
    await fieldRow(page, "Hạn chót").click();
    const panel = fieldPanel(page, "Hạn chót");
    await expect(panel).toBeVisible();
    await expect(panel).toContainText("due_date");
    await expect(page).toHaveURL(/field=sf-due/);
    await panel.getByRole("button", { name: "Đóng" }).click();
    await expect(panel).toBeHidden();

    await openSettings(page, "?field=sf-priority");
    await expect(fieldPanel(page, "Ưu tiên")).toBeVisible();
  });

  test("renaming a field is shown on the board; a name already in use is refused", async ({
    page,
  }) => {
    await openSettings(page, "?field=sf-priority&from=table");
    const panel = fieldPanel(page, "Ưu tiên");
    const name = panel.getByLabel("Tên hiển thị");
    await name.fill("Hạn chót");
    await panel.getByRole("button", { name: "Lưu thay đổi" }).click();
    await expect(
      panel.getByText("Đã có trường khác dùng tên này."),
    ).toBeVisible();

    await name.fill("Mức ưu tiên");
    await panel.getByRole("button", { name: "Lưu thay đổi" }).click();
    // With the panel open the name column is as narrow as in the mockup, so the row is attached, not measurable.
    await expect(fieldRow(page, "Mức ưu tiên")).toBeAttached();
    await expect(fieldPanel(page, "Mức ưu tiên")).toBeVisible();
    // "Về board" returns to the Table the page was opened from.
    await page.getByRole("link", { name: "Về board" }).click();
    await expect(page).toHaveURL(/view=table/);
  });

  test("options: add, rename, recolour, reorder with the keyboard and delete, with their item counts", async ({
    page,
  }) => {
    await openSettings(page, "?field=sf-priority");
    const panel = fieldPanel(page, "Ưu tiên");
    const options = panel.getByLabel("Tên lựa chọn");
    await expect(options).toHaveCount(4);
    await expect(panel.getByText("1 việc", { exact: true })).toBeVisible();
    await expect(panel.getByText("5 việc", { exact: true })).toBeVisible();
    await expect(panel.getByText("7 việc", { exact: true })).toBeVisible();
    await expect(panel.getByText("3 việc", { exact: true })).toBeVisible();

    await panel.getByRole("button", { name: "Thêm lựa chọn" }).click();
    await panel.getByPlaceholder("Lựa chọn mới").fill("Chưa xác định");
    await page.keyboard.press("Enter");
    await expect(options).toHaveCount(5);
    await expect(panel.getByText("0 việc", { exact: true })).toBeVisible();

    const low = options.nth(3);
    await low.fill("Rất thấp");
    await low.blur();
    await expect(options.nth(3)).toHaveValue("Rất thấp");

    await panel
      .getByRole("button", { name: "Màu của lựa chọn Rất thấp" })
      .click();
    await page.getByRole("button", { name: "#2C5E4F" }).click();

    // Space picks the handle up, an arrow moves it, Space drops it.
    const handle = panel.getByRole("button", {
      name: "Kéo để sắp xếp: Rất thấp",
    });
    await handle.focus();
    await page.keyboard.press("Space");
    await expect(handle).toHaveAttribute("aria-pressed", "true");
    // The keyboard sensor listens after the pick-up settles.
    await page.waitForTimeout(250);
    await page.keyboard.press("ArrowUp");
    await page.waitForTimeout(250);
    await page.keyboard.press("Space");
    await expect(options.nth(2)).toHaveValue("Rất thấp");

    await panel
      .getByRole("button", { name: "Xoá lựa chọn Chưa xác định" })
      .click();
    await expect(options).toHaveCount(4);
  });

  test("the default value, the required switch and the guidance are saved with the panel and Huỷ drops them", async ({
    page,
  }) => {
    await openSettings(page, "?field=sf-priority");
    const panel = fieldPanel(page, "Ưu tiên");
    await panel
      .getByRole("combobox", { name: "Giá trị mặc định cho việc mới" })
      .click();
    await page.getByRole("option", { name: "Cao" }).click();
    await panel.getByRole("switch", { name: "Bắt buộc khi tạo việc" }).click();
    await panel.getByRole("button", { name: "Huỷ" }).click();
    await expect(
      panel.getByRole("switch", { name: "Bắt buộc khi tạo việc" }),
    ).toHaveAttribute("aria-checked", "false");

    await panel
      .getByRole("combobox", { name: "Giá trị mặc định cho việc mới" })
      .click();
    await page.getByRole("option", { name: "Cao" }).click();
    await panel.getByRole("switch", { name: "Bắt buộc khi tạo việc" }).click();
    await panel
      .getByLabel("Hướng dẫn cho người điền")
      .fill("Chọn Cao khi việc cần xong trong tuần.");
    await panel.getByRole("button", { name: "Lưu thay đổi" }).click();
    await expect(
      panel.getByRole("switch", { name: "Bắt buộc khi tạo việc" }),
    ).toHaveAttribute("aria-checked", "true");
    await expect(panel.getByLabel("Hướng dẫn cho người điền")).toHaveValue(
      "Chọn Cao khi việc cần xong trong tuần.",
    );
  });

  test("adding a field: the picker filters, Enter adds, Escape closes", async ({
    page,
  }) => {
    await openSettings(page);
    await page.getByRole("button", { name: "Thêm trường" }).click();
    const picker = page.getByRole("dialog", { name: "Chọn loại trường" });
    await expect(picker).toBeVisible();
    await expect(picker.getByText("Cơ bản")).toBeVisible();
    await page.keyboard.press("Escape");
    await expect(picker).toBeHidden();

    await page.getByRole("button", { name: "Thêm trường" }).click();
    await page
      .getByRole("dialog", { name: "Chọn loại trường" })
      .getByLabel("Tìm loại trường…")
      .fill("hộp");
    await page.keyboard.press("Enter");
    // The new field opens in its panel to be named.
    await expect(fieldPanel(page, "Trường mới")).toBeVisible();
    await expect(fieldRow(page, "Trường mới")).toBeAttached();
    await expect(
      page.getByText("Trường của board").locator(".."),
    ).toContainText("12");
  });

  test("archiving a field lists what it touches, moves it to Đã lưu trữ and restoring brings it back", async ({
    page,
  }) => {
    await openSettings(page, "?field=sf-priority");
    const panel = fieldPanel(page, "Ưu tiên");
    await panel.getByRole("button", { name: "Lưu trữ trường" }).click();
    const dialog = page.getByRole("alertdialog");
    await expect(dialog).toContainText('Lưu trữ trường "Ưu tiên"?');
    await expect(dialog).toContainText("16 việc đang có giá trị");
    await expect(dialog).toContainText("3 chế độ xem dùng trường này");
    await expect(dialog).toContainText("1 quy tắc tự động sẽ tạm dừng");
    await dialog.getByRole("button", { name: "Huỷ" }).click();
    await expect(dialog).toBeHidden();

    await panel.getByRole("button", { name: "Lưu trữ trường" }).click();
    await page
      .getByRole("alertdialog")
      .getByRole("button", { name: "Lưu trữ trường" })
      .click();
    await expect(fieldRow(page, "Ưu tiên")).toHaveCount(0);
    await expect(panel).toBeHidden();
    await expect(
      menu(page).getByRole("button", { name: /^Đã lưu trữ/ }),
    ).toContainText("2");
    await expect(
      page.getByRole("button", { name: "Khôi phục trường Ưu tiên" }),
    ).toBeVisible();

    // A view that places by an archived field is marked, a view that filters by it is too.
    await menu(page)
      .getByRole("button", { name: /^Chế độ xem/ })
      .click();
    await expect(page.getByText("Cần sửa")).toHaveCount(2);

    await menu(page)
      .getByRole("button", { name: /^Trường dữ liệu/ })
      .click();
    await page
      .getByRole("button", { name: "Khôi phục trường Ưu tiên" })
      .click();
    await expect(fieldRow(page, "Ưu tiên")).toBeVisible();
  });

  test("the Status field cannot be archived", async ({ page }) => {
    await openSettings(page, "?field=sf-status");
    await expect(
      fieldPanel(page, "Trạng thái").getByRole("button", {
        name: "Lưu trữ trường",
      }),
    ).toHaveAttribute("aria-disabled", "true");
  });

  test("dragging a field changes the default order", async ({ page }) => {
    await openSettings(page);
    const names = async () =>
      page
        .getByRole("button", { name: /^Sửa trường / })
        .evaluateAll((nodes) =>
          nodes.map((node) => node.getAttribute("aria-label")),
        );
    await expect(fieldRow(page, "Hạn chót")).toBeVisible();
    const before = await names();
    const handle = page.getByRole("button", {
      name: "Kéo để sắp xếp: Hạn chót",
    });
    await handle.focus();
    await page.keyboard.press("Space");
    await expect(handle).toHaveAttribute("aria-pressed", "true");
    // The keyboard sensor listens after the pick-up settles.
    await page.waitForTimeout(250);
    await page.keyboard.press("ArrowUp");
    await page.waitForTimeout(250);
    await page.keyboard.press("Space");
    await expect.poll(names).not.toEqual(before);
    const after = await names();
    expect(after.indexOf("Sửa trường Hạn chót")).toBe(
      before.indexOf("Sửa trường Hạn chót") - 1,
    );
  });

  test("views: the Kanban panel edits the card fields and who sees it, the broken view is fixed by restoring", async ({
    page,
  }) => {
    await openSettings(page, "?section=views&view=sv-kanban");
    await expect(page.getByText(/^Chung cho board\s*5/)).toBeVisible();
    await expect(page.getByText(/^Chỉ mình bạn\s*2/)).toBeVisible();
    const panel = viewPanel(page, "Kanban");
    await expect(panel).toContainText("sửa lần cuối bởi Trần Minh Quân");
    await expect(
      panel.getByRole("checkbox", { name: "Ước lượng (giờ)" }),
    ).not.toBeChecked();
    await panel.getByText("Ước lượng (giờ)").click();
    await expect(
      panel.getByRole("checkbox", { name: "Ước lượng (giờ)" }),
    ).toBeChecked();
    await panel.getByRole("radio", { name: "Chỉ mình tôi" }).click();
    await panel.getByRole("button", { name: "Lưu", exact: true }).click();
    await expect(page.getByText(/^Chỉ mình bạn\s*3/)).toBeVisible();
    await expect(
      page.getByRole("button", { name: "Sửa chế độ xem Kanban" }),
    ).toBeVisible();

    await page
      .getByRole("button", { name: "Sửa chế độ xem Lịch bàn giao" })
      .click();
    const broken = viewPanel(page, "Lịch bàn giao");
    await expect(broken.getByRole("alert")).toContainText(
      "Chế độ xem này đang trống vì trường ngày của nó đã bị lưu trữ.",
    );
    await expect(
      broken.getByRole("button", { name: "Lưu", exact: true }),
    ).toBeDisabled();
    await broken
      .getByRole("button", { name: "Khôi phục", exact: true })
      .click();
    await expect(broken.getByRole("alert")).toHaveCount(0);
    await expect(page.getByText("Cần sửa")).toHaveCount(0);
  });

  test("views: a new view is created private and opens, duplicate and delete work, the default cannot be deleted", async ({
    page,
  }) => {
    await openSettings(page, "?section=views");
    await page.getByRole("button", { name: "Chế độ xem mới" }).click();
    await page.getByRole("menuitem", { name: "Lịch" }).click();
    const panel = viewPanel(page, "Chế độ xem mới");
    await expect(panel).toBeVisible();
    await expect(
      menu(page).getByRole("button", { name: /^Chế độ xem/ }),
    ).toContainText("8");

    await panel.getByLabel("Tên", { exact: true }).fill("Lịch của tôi");
    await panel.getByRole("button", { name: "Lưu", exact: true }).click();
    await expect(
      page.getByRole("button", { name: "Sửa chế độ xem Lịch của tôi" }),
    ).toBeVisible();

    await viewPanel(page, "Lịch của tôi")
      .getByRole("button", { name: "Nhân bản" })
      .click();
    await expect(
      page.getByRole("button", {
        name: "Sửa chế độ xem Lịch của tôi (bản sao)",
      }),
    ).toBeVisible();

    await viewPanel(page, "Lịch của tôi")
      .getByRole("button", { name: "Xoá", exact: true })
      .click();
    await viewPanel(page, "Lịch của tôi")
      .getByRole("alert")
      .getByRole("button", { name: "Xoá", exact: true })
      .click();
    await expect(
      page.getByRole("button", {
        name: "Sửa chế độ xem Lịch của tôi",
        exact: true,
      }),
    ).toHaveCount(0);

    await page.getByRole("button", { name: "Sửa chế độ xem Bảng" }).click();
    await expect(
      viewPanel(page, "Bảng").getByRole("button", { name: "Xoá", exact: true }),
    ).toHaveAttribute("aria-disabled", "true");
  });

  test("the Kanban view's own settings sit in its panel and still decide the lanes", async ({
    page,
  }) => {
    await openSettings(page, "?section=views&view=sv-kanban&from=kanban");
    const panel = viewPanel(page, "Kanban");
    await panel
      .getByRole("spinbutton", { name: "Số việc hiện trong mỗi cột" })
      .fill("2");
    await page.getByRole("link", { name: "Về board" }).click();
    await expect(
      page
        .getByRole("region", { name: "Cần làm", exact: true })
        .locator("button[data-card-id]"),
    ).toHaveCount(2);
  });

  test("general: identity, colour, key and zone are saved; the danger zone asks for the board name", async ({
    page,
  }) => {
    await openSettings(page, "?section=general");
    await expect(page.getByLabel("Tên board", { exact: true })).toHaveValue(
      "Triển khai ERP Q4",
    );
    await expect(page.getByText("Phòng Công nghệ")).toBeVisible();
    await expect(
      page.getByText("Đổi mã không đổi mã việc cũ; ERP-142 vẫn mở được."),
    ).toBeVisible();

    await page
      .getByLabel("Tên board", { exact: true })
      .fill("Triển khai ERP Q1 2027");
    await page.getByRole("button", { name: "Tím" }).click();
    await page.getByLabel(/^Mã việc/).fill("erq");
    await page.getByLabel(/^Múi giờ hạn chót/).selectOption("Asia/Singapore");
    await page.getByLabel("Ngày bắt đầu tuần").selectOption("sunday");
    await page.getByRole("button", { name: "Lưu thay đổi" }).click();
    await expect(
      page.getByRole("navigation", { name: "Vị trí" }),
    ).toContainText("Triển khai ERP Q1 2027");
    await expect(page.getByLabel(/^Mã việc/)).toHaveValue("ERQ");
    await expect(page.getByRole("button", { name: "Tím" })).toHaveAttribute(
      "aria-pressed",
      "true",
    );

    const remove = page.getByRole("button", { name: "Xoá board" });
    await expect(remove).toBeDisabled();
    await page.getByLabel(/^Nhập tên board/).fill("Triển khai ERP");
    await expect(remove).toBeDisabled();
    await page.getByLabel(/^Nhập tên board/).fill("Triển khai ERP Q1 2027");
    await expect(remove).toBeEnabled();
  });

  test("general: archiving the board asks first and leaves the page", async ({
    page,
  }) => {
    await openSettings(page, "?section=general");
    await page.getByRole("button", { name: "Lưu trữ board" }).click();
    await expect(page.getByRole("alert")).toContainText(
      'Lưu trữ board "Triển khai ERP Q4"?',
    );
    await page
      .getByRole("alert")
      .getByRole("button", { name: "Lưu trữ board" })
      .click();
    await expect(heading(page)).toBeHidden();
  });

  test("groups: rename, add, delete with a move and the last group stays", async ({
    page,
  }) => {
    await openSettings(page, "?section=groups");
    await expect(page.getByText("4 nhóm · 16 việc")).toBeVisible();
    await expect(page.getByText("Nhóm việc khác Trạng thái")).toBeVisible();

    await page.getByRole("button", { name: "Đổi tên nhóm Chuẩn bị" }).click();
    await page.getByLabel("Tên nhóm", { exact: true }).fill("Khởi động");
    await page.keyboard.press("Enter");
    await expect(page.getByText("Khởi động", { exact: true })).toBeVisible();

    await page.getByRole("button", { name: "Thêm nhóm" }).click();
    await page.getByPlaceholder("Tên nhóm mới").fill("Nghiệm thu");
    await page
      .getByRole("region", { name: "Nhóm việc" })
      .getByRole("button", { name: "Thêm", exact: true })
      .click();
    await expect(page.getByText("Nghiệm thu", { exact: true })).toBeVisible();
    await expect(page.getByText("5 nhóm · 16 việc")).toBeVisible();

    await page
      .getByRole("button", { name: "Thao tác khác với nhóm Kiểm thử" })
      .click();
    await page.getByRole("menuitem", { name: "Xoá nhóm" }).click();
    const confirm = page.getByRole("listitem", {
      name: 'Xoá nhóm "Kiểm thử"?',
    });
    await expect(confirm).toContainText(
      "2 việc trong nhóm được chuyển sang nhóm bạn chọn",
    );
    // The items move to the next group (Đào tạo và vận hành) unless another is chosen.
    await expect(
      confirm.getByRole("combobox").locator("option:checked"),
    ).toHaveText("Đào tạo và vận hành");
    await confirm.getByRole("combobox").selectOption({ label: "Dữ liệu" });
    await confirm
      .getByRole("button", { name: "Chuyển việc và xoá nhóm" })
      .click();
    await expect(page.getByText("4 nhóm · 16 việc")).toBeVisible();
    await expect(
      page.getByRole("button", { name: "Đổi tên nhóm Dữ liệu" }),
    ).toBeVisible();
    await expect(
      page.getByText("5 việc", { exact: true }).first(),
    ).toBeVisible();
  });

  test("automations: a running rule can be turned off and a paused one with no runner stays off", async ({
    page,
  }) => {
    await openSettings(page, "?section=automations");
    await expect(page.getByText("Đang chạy").first()).toBeVisible();
    await expect(
      page.getByText("Tạm dừng", { exact: false }).first(),
    ).toBeVisible();
    await expect(page.getByText("lỗi một phần")).toBeVisible();

    const overdue = page.getByRole("switch", {
      name: "Bật quy tắc Nhắc việc quá hạn",
    });
    await expect(overdue).toHaveAttribute("aria-checked", "true");
    await overdue.click();
    await expect(overdue).toHaveAttribute("aria-checked", "false");
    await expect(
      menu(page).getByRole("button", { name: /^Tự động hoá/ }),
    ).toContainText("2");

    await overdue.click();
    await expect(overdue).toHaveAttribute("aria-checked", "true");

    await expect(
      page.getByRole("switch", {
        name: "Bật quy tắc Gắn nhóm Dữ liệu cho việc kho",
      }),
    ).toBeDisabled();
    // The Automation screen is not built yet: its entries say so instead of leading nowhere.
    await expect(
      page.getByRole("button", { name: "Tạo quy tắc" }),
    ).toHaveAttribute("aria-disabled", "true");
  });

  test("Quyền truy cập goes back to the board and opens the share dialog; Đã lưu trữ lists the archived fields", async ({
    page,
  }) => {
    await openSettings(page, "?section=groups&from=table");
    await menu(page).getByRole("button", { name: "Quyền truy cập" }).click();
    await expect(page).toHaveURL(/\/boards\/mock-board-erp\?.*view=table/);
    await expect(page.getByRole("dialog", { name: /^Chia sẻ/ })).toBeVisible();

    await openSettings(page, "?section=groups");
    await menu(page)
      .getByRole("button", { name: /^Đã lưu trữ/ })
      .click();
    await expect(
      page.getByRole("button", { name: "Khôi phục trường Ngày bàn giao" }),
    ).toBeVisible();
  });

  test("the page works at 1280 px and in the dark theme", async ({ page }) => {
    await page.setViewportSize({ width: 1280, height: 800 });
    await page.addInitScript(() => {
      try {
        window.localStorage.setItem("notrelix-theme-mode", "dark");
      } catch {
        // Private mode: the light theme applies.
      }
    });
    await openSettings(page, "?field=sf-priority");
    await page.setViewportSize({ width: 1280, height: 800 });
    await expect(fieldPanel(page, "Ưu tiên")).toBeVisible();
    const overflow = await page.evaluate(
      () =>
        document.documentElement.scrollWidth -
        document.documentElement.clientWidth,
    );
    expect(overflow).toBeLessThanOrEqual(0);
  });
});

test.describe("Board settings (failed write)", () => {
  test.skip(
    persona !== "owner" || !failedWrite,
    `Needs VITE_MOCK_FAULTS=${SETTINGS_PATH}/fields/sf-priority=500`,
  );

  test("a field write the server refused says so and keeps the draft", async ({
    page,
  }) => {
    await openSettings(page, "?field=sf-priority");
    const panel = fieldPanel(page, "Ưu tiên");
    await panel.getByLabel("Tên hiển thị").fill("Độ ưu tiên");
    await panel.getByRole("button", { name: "Lưu thay đổi" }).click();
    await expect(page.getByRole("alert")).toContainText(
      "Chưa lưu được thay đổi. Chưa có gì bị đổi, hãy thử lại.",
    );
    await expect(panel.getByLabel("Tên hiển thị")).toHaveValue("Độ ưu tiên");
    await expect(fieldRow(page, "Ưu tiên")).toBeAttached();
  });
});

test.describe("Board settings (conflict)", () => {
  test.skip(
    persona !== "owner" || !conflictWrite,
    `Needs VITE_MOCK_FAULTS=${SETTINGS_PATH}/fields/sf-priority=409`,
  );

  test("a stale write is told as a conflict and the page reloads the settings", async ({
    page,
  }) => {
    await openSettings(page, "?field=sf-priority");
    const panel = fieldPanel(page, "Ưu tiên");
    await panel.getByLabel("Tên hiển thị").fill("Độ ưu tiên");
    await panel.getByRole("button", { name: "Lưu thay đổi" }).click();
    const alert = page.getByRole("alert");
    await expect(alert).toContainText(
      "Thiết lập vừa được người khác thay đổi.",
    );
    await alert.getByRole("button", { name: "Tải lại thiết lập" }).click();
    await expect(alert).toBeHidden();
  });
});

test.describe("Board settings (commenter)", () => {
  test.skip(persona !== "commenter", "Needs VITE_MOCK_PERSONA=commenter");
  test.use({ viewport: { width: 1440, height: 900 } });

  test("a person who cannot manage the board gets the no-access card and the header has no Thiết lập", async ({
    page,
  }) => {
    await openSettings(page);
    await expect(
      page.getByRole("heading", {
        level: 2,
        name: "Chỉ người quản lý board mở được thiết lập.",
      }),
    ).toBeVisible();
    await expect(page.getByText("Hỏi Lê Hoàng Phúc")).toBeVisible();
    await expect(menu(page)).toHaveCount(0);
    await page.getByRole("link", { name: "Về board" }).last().click();
    await expect(page).toHaveURL(/\/boards\/mock-board-erp/);
    await expect(
      page.getByRole("button", { name: "Thiết lập", exact: true }),
    ).toHaveCount(0);
  });
});
