import { expect, test, type Page } from "@playwright/test";

const scenario = process.env.VITE_MOCK_STATE ?? "default";
const BASE = "/workspaces/mock-workspace-primary/reports";
const ERP = `${BASE}?report=mock-report-erp`;
const WEEKLY = `${BASE}?report=mock-report-weekly`;

test.beforeEach(async ({ page }) => {
  test.skip(scenario !== "default", "Assertions need the default world data");
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "vi");
    } catch {
      // Private mode: the default locale applies.
    }
  });
});

const widget = (page: Page, title: string) =>
  page.locator("section[aria-labelledby]").filter({
    has: page.getByRole("heading", { name: title, exact: true }),
  });

async function noOverflow(page: Page) {
  const overflow = await page.evaluate(
    () => document.documentElement.scrollWidth - window.innerWidth,
  );
  expect(overflow).toBeLessThanOrEqual(0);
}

test.describe("the report list", () => {
  test("lists the three reports with what the viewer may do and filters them", async ({
    page,
  }) => {
    await page.goto(BASE);
    await expect(page.getByRole("heading", { name: "Báo cáo" })).toBeVisible();
    const table = page.getByRole("table", { name: "Báo cáo" });
    await expect(table.getByRole("row")).toHaveCount(4);
    await expect(table).toContainText("Tổng quan triển khai ERP Q4");
    await expect(table).toContainText(
      "Nguồn: board Triển khai ERP Q4 · 6 widget · chia sẻ với Phòng Công nghệ, Ban giám đốc",
    );
    await expect(table).toContainText(
      "Nguồn: board Đóng sổ tháng 10 · chia sẻ với Phòng Kế toán và bạn",
    );
    await expect(table).toContainText(
      "Nguồn: board ở nhiều phòng ban · không tính 1 board bạn không có quyền xem",
    );
    await expect(table.getByText("Có thể sửa")).toHaveCount(1);
    await expect(table.getByText("Chỉ xem")).toHaveCount(2);
    await expect(table.getByText("10:38 hôm nay")).toBeVisible();
    const group = page.getByRole("group", { name: "Lọc báo cáo" });
    await expect(group.getByRole("button", { name: /Của tôi/ })).toContainText(
      "0",
    );
    await expect(
      group.getByRole("button", { name: /Được chia sẻ/ }),
    ).toContainText("3");
    await group.getByRole("button", { name: /Của tôi/ }).click();
    await expect(
      page.getByText("Chưa có báo cáo nào trong mục này."),
    ).toBeVisible();
    await group.getByRole("button", { name: /Tất cả/ }).click();
    await expect(table.getByRole("row")).toHaveCount(4);
    await noOverflow(page);
  });

  test("opens a report from its row", async ({ page }) => {
    await page.goto(BASE);
    await page
      .getByRole("row", { name: /Tổng quan triển khai ERP Q4/ })
      .click();
    await expect(page).toHaveURL(/report=mock-report-erp/);
    await expect(
      page.getByRole("heading", {
        level: 1,
        name: "Tổng quan triển khai ERP Q4",
      }),
    ).toBeVisible();
  });
});

test.describe("the ERP dashboard", () => {
  test("counts the ERP board the way the mockup does", async ({ page }) => {
    await page.goto(ERP);
    const sum = page.getByRole("list", {
      name: /Tóm tắt board Triển khai ERP Q4/,
    });
    await expect(sum).toContainText("5/16 việc hoàn thành · 31%");
    await expect(sum).toContainText("1 việc quá hạn");
    await expect(sum).toContainText("4 việc đến hạn trong 7 ngày");
    await expect(sum).toContainText("20 ngày tới go-live 01/11");
    await expect(page.getByText("Lê Hoàng Phúc tạo")).toBeVisible();
    await expect(
      page.getByText(/Cập nhật 10:38 · số liệu đến 10:37/),
    ).toBeVisible();
    await expect(
      page.getByRole("img", {
        name: /Theo trạng thái: Cần làm 6, Đang làm 3, Chờ duyệt 2, Hoàn thành 5; tổng 16 việc/,
      }),
    ).toBeVisible();
    const velocity = widget(page, "Tốc độ hoàn thành");
    await expect(velocity.getByRole("img")).toHaveAttribute(
      "aria-label",
      /21–27\/09 2, 28\/09–04\/10 2, 05–11\/10 1, tuần này 12–18\/10 0 việc tính đến 10:37\. Trung bình 3 tuần gần nhất 1,7 việc mỗi tuần\. Nhịp cần từ tuần này tới 01\/11 là 3,7 việc mỗi tuần\./,
    );
    await expect(velocity.getByRole("note")).toContainText(
      "Có nguy cơ trễ go-live. 3 tuần gần nhất: trung bình 1,7 việc/tuần (2 + 2 + 1). Còn 11 việc và 3 tuần tới 01/11 nên cần khoảng 3,7 việc/tuần.",
    );
    await expect(widget(page, "Theo phân hệ").getByRole("img")).toHaveAttribute(
      "aria-label",
      "Việc chưa hoàn thành theo phân hệ: Kế toán 5, Kho 2, Mua hàng 2, Chưa đặt 2",
    );
    await expect(
      widget(page, "Theo mức ưu tiên").getByRole("img"),
    ).toHaveAttribute(
      "aria-label",
      "Tất cả việc theo mức ưu tiên: Khẩn 1, Cao 5, Trung bình 7, Thấp 3",
    );
    const due = widget(page, "Đến hạn trong 7 ngày");
    await expect(due.getByRole("link")).toHaveCount(4);
    await expect(due.getByRole("link").first()).toContainText("ERP-138");
    await expect(due.getByRole("link").first()).toContainText("14/10");
    await expect(due.getByRole("link").first()).toContainText("Thứ Tư");
    const load = widget(page, "Khối lượng theo người phụ trách");
    await expect(load.getByRole("listitem")).toHaveCount(8);
    await expect(
      load.getByRole("button", {
        name: /Trần Minh Quân · 2 việc chưa hoàn thành, 1 quá hạn/,
      }),
    ).toBeVisible();
    await noOverflow(page);
  });

  test("a due item opens its page on the board", async ({ page }) => {
    await page.goto(ERP);
    await widget(page, "Đến hạn trong 7 ngày")
      .getByRole("link", { name: /ERP-146/ })
      .click();
    await expect(page).toHaveURL(/boards\/mock-board-erp/);
    await expect(page).toHaveURL(/mock-erp-item-146/);
  });

  test("narrows to one person and shows it in the export scope", async ({
    page,
  }) => {
    await page.goto(ERP);
    await page.getByRole("button", { name: /Tất cả người phụ trách/ }).click();
    await page.getByRole("menuitemradio", { name: "Trần Minh Quân" }).click();
    await expect(page).toHaveURL(/assignee=mock-user-erp-quan/);
    const sum = page.getByRole("list", { name: /Tóm tắt board/ });
    await expect(sum).toContainText("0/2 việc hoàn thành · 0%");
    await expect(sum).toContainText("1 việc quá hạn");
    await page.getByRole("button", { name: "Xuất" }).click();
    await expect(
      page.getByRole("dialog", { name: "Xuất báo cáo" }),
    ).toContainText("người phụ trách Trần Minh Quân");
  });

  test("the filter button hides and shows the filter bar", async ({ page }) => {
    await page.goto(ERP);
    const bar = page.getByRole("group", { name: "Bộ lọc đang dùng" });
    await expect(bar).toBeVisible();
    await page.getByRole("button", { name: "Bộ lọc", exact: true }).click();
    await expect(bar).toBeHidden();
    await page.getByRole("button", { name: "Bộ lọc", exact: true }).click();
    await expect(bar).toBeVisible();
  });

  test("a widget says how it counts", async ({ page }) => {
    await page.goto(ERP);
    await page
      .getByRole("button", { name: "Tuỳ chọn widget Theo trạng thái" })
      .click();
    await page.getByRole("menuitem", { name: "Cách đếm số liệu" }).click();
    await expect(page.getByRole("dialog")).toContainText(
      "Mỗi việc chưa lưu trữ đếm một lần, theo trạng thái hiện tại.",
    );
  });
});

test.describe("drill-down", () => {
  test("opens the open items behind a person, read-only, and closes with Esc", async ({
    page,
  }) => {
    await page.goto(ERP);
    await page
      .getByRole("button", {
        name: /Trần Minh Quân · 2 việc chưa hoàn thành, 1 quá hạn/,
      })
      .click();
    await expect(page).toHaveURL(/drill=mock-user-erp-quan/);
    const sheet = page.getByRole("dialog", {
      name: "Trần Minh Quân · 2 việc chưa hoàn thành",
    });
    await expect(sheet).toBeVisible();
    await expect(sheet).toContainText("ERP-142");
    await expect(sheet).toContainText("Quá hạn 2 ngày · hạn 10/10");
    await expect(sheet).toContainText("Khẩn");
    await expect(sheet).toContainText("Phân hệ Kế toán");
    await expect(sheet).toContainText("ERP-138");
    await expect(sheet).toContainText("Đang chờ bạn duyệt");
    await expect(sheet).toContainText("Báo cáo chỉ đọc. Sửa việc trên board.");
    await expect(
      sheet.getByRole("link", { name: "Mở trên board" }).first(),
    ).toHaveAttribute("href", /boards\/mock-board-erp\?item=mock-erp-item-142/);
    await page.keyboard.press("Escape");
    await expect(sheet).toBeHidden();
    await expect(page).not.toHaveURL(/drill=/);
  });

  test("the unassigned row lists the item nobody holds", async ({ page }) => {
    await page.goto(`${ERP}&drill=none`);
    const sheet = page.getByRole("dialog", { name: /Chưa giao · 1 việc/ });
    await expect(sheet).toContainText("ERP-153");
  });
});

test.describe("export", () => {
  test("makes a file in the background, finishes on a check and leaves a link", async ({
    page,
  }) => {
    await page.goto(ERP);
    await page.getByRole("button", { name: "Xuất", exact: true }).click();
    const dialog = page.getByRole("dialog", { name: "Xuất báo cáo" });
    await expect(dialog.getByRole("radio", { name: /PDF/ })).toBeChecked();
    await expect(dialog).toContainText(
      "Bộ lọc đang dùng: board Triển khai ERP Q4 · đến 01/11 · tất cả người phụ trách",
    );
    await dialog.getByRole("radio", { name: /CSV/ }).click();
    await dialog.getByRole("button", { name: "Tạo tệp" }).click();
    await expect(dialog).toContainText("Đang tạo tệp… thường dưới 1 phút");
    await expect(dialog).toContainText("Bắt đầu 10:41.");
    await expect(dialog).toContainText(
      "Liên kết tải xuống hết hạn sau 7 ngày.",
    );
    await expect(
      dialog.getByRole("button", { name: "Đang tạo…" }),
    ).toHaveAttribute("aria-disabled", "true");
    await dialog.getByRole("button", { name: "Kiểm tra lại" }).click();
    await expect(dialog).toContainText("Tệp đã sẵn sàng");
    const link = dialog.getByRole("link", {
      name: /Tải xuống tong-quan-trien-khai-erp-q4\.csv/,
    });
    await expect(link).toHaveAttribute(
      "download",
      "tong-quan-trien-khai-erp-q4.csv",
    );
    await expect(link).toHaveAttribute("href", /^data:text\/csv/);
    await page.keyboard.press("Escape");
    await expect(dialog).toBeHidden();
    await expect(page).not.toHaveURL(/export=/);
  });
});

test.describe("partial numbers", () => {
  test("shows what was left out and a stale widget until it is recomputed", async ({
    page,
  }) => {
    await page.goto(WEEKLY);
    await expect(
      page.getByRole("heading", {
        level: 1,
        name: "Báo cáo tuần Ban giám đốc",
      }),
    ).toBeVisible();
    await expect(page.getByText("Bạn: Chỉ xem")).toBeVisible();
    await expect(page.getByRole("button", { name: "Sửa" })).toHaveCount(0);
    await expect(page.getByRole("note").first()).toContainText(
      "Không tính 1 board bạn không có quyền xem. Số liệu có thể khác với bản người khác thấy.",
    );
    const done = widget(page, "Việc hoàn thành theo phòng");
    await expect(done.getByRole("img")).toHaveAttribute(
      "aria-label",
      "Việc hoàn thành tuần 05–11/10 theo phòng: Kế toán 4, Marketing 3, Nhân sự 2, Công nghệ 1, Ban giám đốc 0",
    );
    await expect(done).toContainText(
      "0 nghĩa là không có việc nào hoàn thành, không phải thiếu số liệu.",
    );
    const over = widget(page, "Việc quá hạn theo phòng");
    await expect(over).toHaveAttribute("aria-busy", "true");
    await expect(over.getByRole("status")).toContainText(
      "Đang tính lại · số liệu đến 10:20, cũ hơn phần còn lại của báo cáo",
    );
    await expect(widget(page, "Ghi chú tuần")).toContainText(
      "Trịnh Quốc Hưng viết · 09/10",
    );
    await expect(widget(page, "Tiến độ Triển khai ERP Q4")).toContainText(
      "5/16 · 31%",
    );
    await over.getByRole("button", { name: "Tải lại" }).click();
    await expect(over.getByRole("status")).toHaveCount(0);
    await expect(over).not.toHaveAttribute("aria-busy", "true");
  });

  test("the progress widget opens the detailed report's board", async ({
    page,
  }) => {
    await page.goto(WEEKLY);
    await widget(page, "Tiến độ Triển khai ERP Q4")
      .getByRole("link", { name: "Mở báo cáo chi tiết" })
      .click();
    await expect(page).toHaveURL(/boards\/mock-board-erp/);
  });
});

test.describe("a new report", () => {
  test("starts empty, takes a layout, a name, sharing and can be deleted", async ({
    page,
  }) => {
    await page.goto(BASE);
    await page.getByRole("button", { name: "Tạo báo cáo" }).click();
    await expect(page).toHaveURL(/report=mock-report-new-/);
    await expect(
      page.getByRole("heading", { level: 1, name: "Báo cáo chưa đặt tên" }),
    ).toBeVisible();
    await expect(page.getByText("Bạn tạo lúc 10:40")).toBeVisible();
    await expect(page.getByText("Chưa chia sẻ, chỉ bạn thấy")).toBeVisible();
    await expect(page.getByText("Chưa có widget")).toBeVisible();
    await expect(
      page.getByRole("heading", { name: "Bắt đầu từ một bố cục" }),
    ).toBeVisible();
    await expect(page.getByRole("button", { name: "Dùng bố cục" })).toHaveCount(
      3,
    );

    // Name it.
    await page.getByRole("button", { name: "Đổi tên báo cáo" }).click();
    const rename = page.getByRole("dialog", { name: "Đổi tên" });
    await rename.getByLabel("Tên báo cáo").fill("Theo dõi ERP của tôi");
    await rename.getByRole("button", { name: "Lưu" }).click();
    await expect(
      page.getByRole("heading", { level: 1, name: "Theo dõi ERP của tôi" }),
    ).toBeVisible();

    // Share it with a department.
    await page.getByRole("button", { name: "Chia sẻ", exact: true }).click();
    const share = page.getByRole("dialog", { name: "Chia sẻ báo cáo" });
    await share.getByRole("checkbox", { name: /Ban giám đốc/ }).click();
    await share.getByRole("button", { name: "Lưu" }).click();
    await expect(share).toBeHidden();

    // Use a starting layout.
    await page.getByRole("button", { name: "Dùng bố cục" }).first().click();
    await expect(widget(page, "Theo trạng thái")).toBeVisible();
    await expect(widget(page, "Tốc độ hoàn thành")).toBeVisible();
    await expect(widget(page, "Đến hạn trong 7 ngày")).toBeVisible();
    await expect(page.getByText("Chia sẻ với Ban giám đốc")).toBeVisible();

    // It is in the list as mine.
    await page
      .getByRole("link", { name: "Báo cáo", exact: true })
      .first()
      .click();
    const group = page.getByRole("group", { name: "Lọc báo cáo" });
    await expect(group.getByRole("button", { name: /Của tôi/ })).toContainText(
      "1",
    );
    await expect(page.getByRole("table", { name: "Báo cáo" })).toContainText(
      "Theo dõi ERP của tôi",
    );

    // Delete it.
    await page.getByRole("row", { name: /Theo dõi ERP của tôi/ }).click();
    await page
      .getByRole("button", { name: "Thêm thao tác cho báo cáo" })
      .click();
    await page.getByRole("menuitem", { name: "Xoá báo cáo" }).click();
    await page
      .getByRole("alertdialog")
      .getByRole("button", { name: "Xoá báo cáo" })
      .click();
    await expect(page).not.toHaveURL(/report=/);
    await expect(
      page.getByRole("table", { name: "Báo cáo" }),
    ).not.toContainText("Theo dõi ERP của tôi");
  });
});

test.describe("editing the layout", () => {
  test("moves with the keyboard, cancels, resizes, adds a widget and saves", async ({
    page,
  }) => {
    await page.goto(BASE);
    await page.getByRole("button", { name: "Tạo báo cáo" }).click();
    await page.getByRole("button", { name: "Dùng bố cục" }).first().click();
    await expect(widget(page, "Theo trạng thái")).toBeVisible();

    await page.getByRole("button", { name: "Sửa", exact: true }).click();
    await expect(page).toHaveURL(/edit=true/);
    const bar = page.getByRole("region", { name: "Sửa bố cục báo cáo" });
    await expect(bar).toContainText("Đang sửa bố cục");
    await expect(bar).toContainText(
      "Sắp xếp widget không đổi thứ tự việc trên board.",
    );
    const order = () =>
      page
        .locator("section[aria-labelledby] h2")
        .evaluateAll((nodes) => nodes.map((node) => node.textContent));
    expect(await order()).toEqual([
      "Theo trạng thái",
      "Tốc độ hoàn thành",
      "Đến hạn trong 7 ngày",
    ]);

    // Keyboard move: Esc puts it back.
    const velocity = widget(page, "Tốc độ hoàn thành");
    await velocity
      .getByRole("button", { name: "Di chuyển", exact: true })
      .click();
    await expect(velocity.getByRole("status")).toContainText(
      "Đang di chuyển · vị trí 2/3",
    );
    await page.keyboard.press("ArrowUp");
    await expect(velocity.getByRole("status")).toContainText("vị trí 1/3");
    expect((await order())[0]).toBe("Tốc độ hoàn thành");
    await page.keyboard.press("Escape");
    await expect(velocity.getByRole("status")).toHaveCount(0);
    expect(await order()).toEqual([
      "Theo trạng thái",
      "Tốc độ hoàn thành",
      "Đến hạn trong 7 ngày",
    ]);

    // Move and place.
    await velocity
      .getByRole("button", { name: "Di chuyển", exact: true })
      .click();
    await page.keyboard.press("ArrowUp");
    await page.keyboard.press("Enter");
    expect((await order()).slice(0, 2)).toEqual([
      "Tốc độ hoàn thành",
      "Theo trạng thái",
    ]);

    // Resize the status widget to the side column.
    await widget(page, "Theo trạng thái")
      .getByRole("button", { name: "Đổi kích thước", exact: true })
      .click();
    await page.getByRole("menuitemradio", { name: "Cột bên" }).click();
    await expect(
      page.locator("section[aria-labelledby]").nth(0).getByRole("heading"),
    ).toHaveText("Tốc độ hoàn thành");

    // Add a widget: a line needs the week dimension.
    await bar.getByRole("button", { name: "Thêm widget" }).click();
    await expect(page).toHaveURL(/add=true/);
    const dialog = page.getByRole("dialog", { name: "Thêm widget" });
    await expect(dialog).toContainText("Vào báo cáo Báo cáo chưa đặt tên");
    await expect(dialog.getByRole("radio", { name: "Đường" })).toHaveAttribute(
      "aria-disabled",
      "true",
    );
    await expect(dialog.getByLabel("Tên widget")).toHaveValue(
      "Việc chưa hoàn thành theo người phụ trách",
    );
    await expect(dialog).toContainText(
      "Số việc · định nghĩa: mỗi việc chưa lưu trữ đếm 1",
    );
    await expect(
      dialog.getByRole("img", { name: /Trần Minh Quân 2/ }),
    ).toBeVisible();
    await dialog.getByLabel("Nhóm theo").selectOption("week");
    await expect(
      dialog.getByRole("radio", { name: "Đường" }),
    ).not.toHaveAttribute("aria-disabled", "true");
    await dialog.getByRole("radio", { name: "Cột" }).click();
    await dialog.getByLabel("Nhóm theo").selectOption("assignee");
    await dialog.getByLabel("Tên widget").fill("Việc mở theo người");
    await dialog
      .getByRole("button", { name: "Bỏ điều kiện lọc Trạng thái" })
      .click();
    await dialog.getByRole("button", { name: "Thêm điều kiện" }).click();
    await page.getByRole("menuitem", { name: "Quá hạn" }).click();
    await dialog.getByRole("button", { name: "Thêm", exact: true }).click();
    await expect(dialog).toBeHidden();
    await expect(page).not.toHaveURL(/add=true/);
    expect(await order()).toEqual([
      "Tốc độ hoàn thành",
      "Việc mở theo người",
      "Theo trạng thái",
      "Đến hạn trong 7 ngày",
    ]);

    // Cancel drops the draft; the saved layout is still the old one.
    await bar.getByRole("button", { name: "Huỷ" }).click();
    await expect(page).not.toHaveURL(/edit=true/);
    expect(await order()).toEqual([
      "Theo trạng thái",
      "Tốc độ hoàn thành",
      "Đến hạn trong 7 ngày",
    ]);

    // Do it again and save.
    await page.getByRole("button", { name: "Sửa", exact: true }).click();
    await widget(page, "Tốc độ hoàn thành")
      .getByRole("button", { name: "Di chuyển", exact: true })
      .click();
    await page.keyboard.press("ArrowUp");
    await page.keyboard.press("Enter");
    await page
      .getByRole("region", { name: "Sửa bố cục báo cáo" })
      .getByRole("button", { name: "Lưu", exact: true })
      .click();
    await expect(page).not.toHaveURL(/edit=true/);
    expect((await order()).slice(0, 2)).toEqual([
      "Tốc độ hoàn thành",
      "Theo trạng thái",
    ]);
  });

  test("adds a note and removes a widget from the menu", async ({ page }) => {
    await page.goto(BASE);
    await page.getByRole("button", { name: "Tạo báo cáo" }).click();
    await page.getByRole("button", { name: "Thêm widget" }).first().click();
    const dialog = page.getByRole("dialog", { name: "Thêm widget" });
    await dialog.getByRole("radio", { name: "Ghi chú" }).click();
    await dialog.getByLabel("Tên widget").fill("Lưu ý tuần này");
    await dialog.getByRole("button", { name: "Thêm", exact: true }).click();
    await expect(dialog.getByText("Hãy viết nội dung ghi chú.")).toBeVisible();
    await dialog
      .getByLabel("Nội dung ghi chú")
      .fill("Chốt dữ liệu trước thứ Sáu.");
    await dialog.getByRole("button", { name: "Thêm", exact: true }).click();
    await page
      .getByRole("region", { name: "Sửa bố cục báo cáo" })
      .getByRole("button", { name: "Lưu", exact: true })
      .click();
    const note = widget(page, "Lưu ý tuần này");
    await expect(note).toContainText("Chốt dữ liệu trước thứ Sáu.");
    await expect(note).toContainText("Nguyễn Thu Hà viết · 12/10");
    await page
      .getByRole("button", { name: "Tuỳ chọn widget Lưu ý tuần này" })
      .click();
    await page.getByRole("menuitem", { name: "Xoá khỏi báo cáo" }).click();
    await page
      .getByRole("alertdialog")
      .getByRole("button", { name: "Xoá widget" })
      .click();
    await expect(note).toHaveCount(0);
    await expect(
      page.getByRole("heading", { name: "Bắt đầu từ một bố cục" }),
    ).toBeVisible();
  });
});

test.describe("a narrower window", () => {
  test("the dashboard has no horizontal overflow at 1280", async ({ page }) => {
    await page.setViewportSize({ width: 1280, height: 800 });
    await page.goto(ERP);
    await expect(widget(page, "Tốc độ hoàn thành")).toBeVisible();
    await noOverflow(page);
    await page.goto(BASE);
    await expect(page.getByRole("table", { name: "Báo cáo" })).toBeVisible();
    await noOverflow(page);
  });
});
