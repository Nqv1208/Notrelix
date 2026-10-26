import { expect, test, type Page } from "@playwright/test";

/**
 * The document page of the mock world (owner persona, the Kế toán process
 * page): what the header and the meta line say, the links and contents panels,
 * the comments, turning a checklist line into a work item, the version compare
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
    await expect(page.getByText("Chờ duyệt")).toHaveCount(0);
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
    await expect(page.getByRole("button", { name: /^In đậm/ })).toHaveAttribute(
      "aria-pressed",
      "true",
    );
  });

  test("links: the mentioned work items in reading order and the private one, then what mentions the page", async ({
    page,
  }) => {
    await openDoc(page);
    await expect(tab(page, /^Liên kết 6/)).toBeVisible();
    const panel = page.getByRole("tabpanel");
    await expect(panel).toContainText("ERP-129");
    const text = await panel.innerText();
    const order = ["ERP-144", "ERP-142", "ERP-151", "ERP-149", "ERP-129"].map(
      (key) => text.indexOf(key),
    );
    expect(order.every((index) => index >= 0)).toBe(true);
    expect([...order].sort((a, b) => a - b)).toEqual(order);
    await expect(panel.getByText("Việc riêng tư")).toBeVisible();
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
    await dialog.getByRole("combobox", { name: "Board" }).selectOption({
      label: "Triển khai ERP Q4",
    });
    const group = dialog.getByRole("combobox", { name: "Nhóm" });
    await expect(group).toBeEnabled();
    await group.selectOption({ index: 1 });
    await dialog.getByRole("button", { name: "Tạo việc" }).click();
    await expect(tab(page, /^Liên kết 7/)).toBeVisible();
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
  });

  test("comments name Lý Thu Trang and Đỗ Khánh Linh and mark the anchored text", async ({
    page,
  }) => {
    await openDoc(page);
    await tab(page, /^Bình luận/).click();
    const panel = page.getByRole("tabpanel");
    await expect(panel.getByText("Lý Thu Trang")).toBeVisible();
    await expect(panel.getByText("Đỗ Khánh Linh")).toBeVisible();
    await expect(page.locator("mark")).toHaveText(
      "trong tối đa 2 ngày làm việc",
    );
  });

  test("versions: the compare opens on the 10/10 15:42 version with what changed since", async ({
    page,
  }) => {
    await openDoc(page);
    await page.getByRole("button", { name: "Tệp", exact: true }).click();
    await page.getByRole("menuitem", { name: /Lịch sử phiên bản/ }).click();
    const bar = page.getByRole("region", { name: /phiên bản/i }).first();
    await expect(bar).toContainText("10/10 15:42");
    await expect(bar).toContainText("Lý Thu Trang");
    // Step 3 as it read, and the two checklist lines added after it.
    const step = page
      .getByRole("listitem")
      .filter({ hasText: "Hoá đơn sai lệch" });
    await expect(step.locator("del")).toHaveText(["1%", "kế toán trưởng"]);
    await expect(step.locator("ins")).toHaveText(["0,5%", "trưởng nhóm"]);
    await expect(page.getByText("Đã thêm sau phiên bản này:")).toHaveCount(2);

    // The named version, and only it when the filter is on.
    await expect(page.getByText("Bản trình Ban giám đốc")).toBeVisible();
    await page
      .getByRole("checkbox", { name: "Chỉ hiện phiên bản đã đặt tên" })
      .check();
    await expect(page.getByText("Bản trình Ban giám đốc")).toBeVisible();
    await expect(page.getByText("Sửa bước 3")).toHaveCount(0);

    // An older version has fewer blocks: the compare follows it.
    await page
      .getByRole("checkbox", { name: "Chỉ hiện phiên bản đã đặt tên" })
      .uncheck();
    await page
      .getByRole("button", { name: /05\/10|14:20/ })
      .last()
      .click();
    await expect(bar).toContainText("05/10 14:20");
    await page.getByRole("button", { name: "Đóng" }).first().click();
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
