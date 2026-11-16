import { expect, test, type Page } from "@playwright/test";

/**
 * The document page of the mock world (owner persona, the Kế toán process
 * page): what the header and the meta line say, the links and contents panels,
 * the comments, turning a checklist line into a work item, the version history
 * and the share dialog's page permissions. Geometry against the mockup is
 * `design-fidelity-docs`; the mock world resets on every page load.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const DOC = "/workspaces/mock-workspace-primary/docs/mock-doc-invoice-process";
const NOW = new Date("2026-10-12T10:40:00+07:00");

async function openDoc(page: Page) {
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
  await page.goto(DOC);
  await expect(page.getByRole("heading", { level: 1 })).toBeVisible();
  await expect(page.getByRole("table")).toBeVisible();
}

const tab = (page: Page, name: RegExp) => page.getByRole("tab", { name });

test.describe("Document page (owner)", () => {
  test.skip(
    persona !== "owner" || scenario !== "default" || faults !== "",
    "Owner checks need the default owner world without faults",
  );

  test("the header says who edited the page last and who is on it (a page has no review status)", async ({
    page,
  }) => {
    await openDoc(page);
    const crumb = page
      .getByRole("navigation")
      .filter({ hasText: "Kế toán" })
      .first();
    await expect(crumb).toContainText("Quy trình");
    await expect(crumb).toContainText("Nhập hoá đơn đầu vào");
    await expect(page.getByText("Chờ duyệt", { exact: true })).toHaveCount(0);
    await expect(
      page.getByText("Đỗ Khánh Linh sửa lúc 16:40 hôm qua"),
    ).toBeVisible();
    await expect(
      page.getByLabel("Đang xem: Đỗ Khánh Linh, Lý Thu Trang"),
    ).toBeVisible();
    await expect(page.getByRole("heading", { level: 1 })).toHaveText(
      "Quy trình nhập hoá đơn đầu vào",
    );
  });

  test("under the title: the department, the date, the board, and why the viewer sees it", async ({
    page,
  }) => {
    await openDoc(page);
    await expect(page.getByText("Phòng Kế toán · Riêng tư")).toBeVisible();
    await expect(page.getByText("Áp dụng từ 01/11/2026")).toBeVisible();
    await expect(
      page.getByRole("link", { name: "Triển khai ERP Q4" }).first(),
    ).toBeVisible();
    await expect(
      page.getByText(
        "Bạn thấy trang của phòng Kế toán vì bạn là quản trị workspace.",
      ),
    ).toBeVisible();
  });

  test("the toolbar shows the style of the heading the page opens on", async ({
    page,
  }) => {
    await openDoc(page);
    await expect(
      page
        .getByRole("combobox", { name: /Kiểu/ })
        .or(page.getByText("Tiêu đề 2"))
        .first(),
    ).toBeVisible();
    await expect(page.getByText("Tiêu đề 2").first()).toBeVisible();
    await expect(page.getByRole("combobox", { name: "Kiểu đoạn" })).toHaveText(
      "Tiêu đề 2",
    );
  });

  test("links: the work items mentioned in the text in reading order, then what mentions the page", async ({
    page,
  }) => {
    await openDoc(page);
    // The board block lists its board live; only mentions in the text count.
    await expect(tab(page, /^Liên kết 2/)).toBeVisible();
    const panel = page.getByRole("tabpanel");
    const text = await panel.innerText();
    const order = ["ERP-144", "ERP-151"].map((key) => text.indexOf(key));
    expect(order.every((index) => index >= 0)).toBe(true);
    expect([...order].sort((a, b) => a - b)).toEqual(order);
    await expect(panel.getByText("Được nhắc tới ở")).toBeVisible();
    await expect(panel.getByText("Đặc tả triển khai ERP")).toBeVisible();
    await expect(panel.getByText("Tài liệu · Công nghệ")).toBeVisible();
  });

  test("a checklist line turns into a work item that joins the links", async ({
    page,
  }) => {
    await openDoc(page);
    const line = page
      .locator("article")
      .getByText("Đào tạo nhóm Công nợ về đối chiếu tự động");
    await line.hover();
    const convert = page.getByRole("button", {
      name: "Chuyển thành việc: Đào tạo nhóm Công nợ về đối chiếu tự động",
    });
    await expect(convert).toBeVisible();
    await convert.click();
    // The writer picks a board they can edit and a group; the item is created there.
    const dialog = page.getByRole("dialog", { name: "Chuyển thành việc" });
    await dialog.getByRole("combobox", { name: "Board" }).click();
    await page.getByRole("option", { name: "Triển khai ERP Q4" }).click();
    const group = dialog.getByRole("combobox", { name: "Nhóm" });
    await expect(group).toBeEnabled();
    await group.click();
    await page.getByRole("option").first().click();
    await dialog.getByRole("button", { name: "Tạo việc" }).click();
    await expect(tab(page, /^Liên kết 3/)).toBeVisible();
    await expect(
      page
        .getByRole("tabpanel")
        .getByText("Đào tạo nhóm Công nợ về đối chiếu tự động"),
    ).toBeVisible();
    await line.hover();
    await expect(convert).toHaveCount(0);
    // A ticked line and a line already linked offer no conversion.
    await expect(
      page.getByRole("button", { name: /^Chuyển thành việc: Chốt danh mục/ }),
    ).toHaveCount(0);
  });

  test("contents follow the reading position and jump to a heading", async ({
    page,
  }) => {
    await openDoc(page);
    await tab(page, /^Mục lục/).click();
    const nav = page.getByRole("navigation", { name: "Mục lục tài liệu" });
    await expect(
      nav.getByRole("link", { name: "Các bước xử lý" }),
    ).toHaveAttribute("aria-current", "location");
    await nav.getByRole("link", { name: "Việc liên quan" }).click();
    await expect(
      nav.getByRole("link", { name: "Việc liên quan" }),
    ).toHaveAttribute("aria-current", "location");
    // Reading again: scrolling back up hands the current entry back to the
    // reading position.
    await page.getByRole("article").hover();
    await page.mouse.wheel(0, -2000);
    await expect(
      nav.getByRole("link", { name: "Các bước xử lý" }),
    ).toHaveAttribute("aria-current", "location");
  });

  test("comments name Lý Thu Trang and Đỗ Khánh Linh and mark the anchored text", async ({
    page,
  }) => {
    await openDoc(page);
    await tab(page, /^Bình luận/).click();
    const panel = page.getByRole("tabpanel");
    await expect(panel.getByText("Lý Thu Trang")).toBeVisible();
    await expect(panel.getByText("Đỗ Khánh Linh")).toBeVisible();
    await expect(page.locator(".comment-anchor")).toHaveText(
      "trong tối đa 2 ngày làm việc",
    );
  });

  test("versions: the panel lists the history, a version opens read-only, a named one is saved and a restore is confirmed", async ({
    page,
  }) => {
    await openDoc(page);
    await page.getByRole("menuitem", { name: "Tệp", exact: true }).click();
    await page.getByRole("menuitem", { name: /Lịch sử phiên bản/ }).click();
    const panel = page.getByRole("complementary", {
      name: "Lịch sử phiên bản",
    });
    await expect(panel.getByText("Hiện tại", { exact: true })).toBeVisible();
    const bar = page.locator('div[role="status"]', {
      hasText: "Trang này chỉ để xem.",
    });
    await expect(bar).toHaveCount(0);

    // The named version, and only it when the filter is on.
    await expect(panel.getByText("Bản trình Ban giám đốc")).toBeVisible();
    const filter = panel.getByRole("checkbox", { name: "Chỉ bản đã đặt tên" });
    await filter.check();
    await expect(panel.getByText("Bản trình Ban giám đốc")).toBeVisible();
    await expect(panel.getByRole("listitem")).toHaveCount(1);
    await filter.uncheck();

    // An older version opens as the page was: read-only, no editor and no marks of change.
    await panel.getByRole("button", { name: /^15:42 / }).click();
    await expect(bar).toContainText("phiên bản");
    await expect(bar).toContainText("15:42 ngày 10/10");
    await expect(page.locator("article ins, article del")).toHaveCount(0);
    await expect(
      page.locator('article [contenteditable]:not([contenteditable="false"])'),
    ).toHaveCount(0);

    // Back to the current page, and the newest entry is the page as it stands.
    await bar.getByRole("button", { name: "Quay lại bản hiện tại" }).click();
    await expect(bar).toHaveCount(0);

    // Save the page under a name: it is listed first on its day.
    await panel.getByRole("button", { name: "Lưu phiên bản có tên" }).click();
    const dialog = page.getByRole("dialog");
    await dialog.getByRole("button", { name: "Lưu", exact: true }).click();
    await expect(dialog.getByRole("alert")).toHaveText(
      "Hãy đặt tên cho phiên bản này.",
    );
    await dialog
      .getByRole("textbox", { name: "Tên phiên bản" })
      .fill("Bản chốt đóng sổ T10");
    await dialog.getByRole("button", { name: "Lưu", exact: true }).click();
    await expect(dialog).toBeHidden();
    await expect(panel.getByText("Bản chốt đóng sổ T10")).toBeVisible();

    // Restore the older version after a confirm; the panel closes.
    await panel.getByRole("button", { name: /^15:42 / }).click();
    await bar.getByRole("button", { name: "Khôi phục bản này" }).click();
    await expect(
      page
        .getByRole("dialog")
        .getByRole("heading", { name: /^Khôi phục phiên bản \d+\?$/ }),
    ).toBeVisible();
    await page
      .getByRole("dialog")
      .getByRole("button", { name: "Khôi phục", exact: true })
      .click();
    await expect(panel).toHaveCount(0);
    await expect(bar).toHaveCount(0);
  });

  test("share: the page's own permissions beside the department's", async ({
    page,
  }) => {
    await openDoc(page);
    await page
      .getByRole("button", { name: /^Chia sẻ/ })
      .first()
      .click();
    const dialog = page.getByRole("dialog");
    await expect(dialog).toContainText("Trang trong Phòng Kế toán");
    await expect(dialog).toContainText("Theo Phòng Kế toán");
    await expect(dialog).toContainText("Quyền riêng trên trang này · 4");
    for (const name of [
      "Đỗ Khánh Linh",
      "Nguyễn Thu Hà",
      "Nhóm Kỹ sư ERP",
      "Lâm Gia Bảo",
    ]) {
      await expect(dialog.getByText(name)).toBeVisible();
    }
    await expect(dialog.getByText("Ngoài công ty")).toBeVisible();
    await expect(
      dialog.getByText("Người tạo trang · Quản trị Phòng Kế toán"),
    ).toBeVisible();
    await page.keyboard.press("Escape");
    await expect(dialog).toBeHidden();
  });

  test("the department tree names the page by its short label", async ({
    page,
  }) => {
    await openDoc(page);
    await expect(
      page
        .getByRole("link", { name: "Nhập hoá đơn đầu vào", exact: true })
        .first(),
    ).toHaveAttribute("aria-current", "page");
  });
});
