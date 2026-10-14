import { expect, test, type Page } from "@playwright/test";

/**
 * ERP-142 on its full page as Final-Detail draws it, on the mock world: the
 * sub-items (tick, add), the links (list, add, remove), the files (list,
 * upload), the custom fields and the system facts, the follow button and its
 * faces, and the history (older lines folded behind a button, the lines of
 * the changes just made). Geometry lives in
 * `design-fidelity-detail.mock.e2e.spec.ts`.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const ITEM =
  "/workspaces/mock-workspace-primary/boards/mock-board-erp/items/mock-erp-item-142";
const NOW = new Date("2026-10-12T10:40:00+07:00");

async function open(page: Page, query = "") {
  await page.clock.setFixedTime(NOW);
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "vi");
    } catch {
      // Private mode: the default locale applies.
    }
  });
  await page.goto(`${ITEM}${query}`);
  await expect(page.getByRole("heading", { level: 1 })).toContainText(
    "Chuyển dữ liệu khách hàng",
  );
}

test.describe("Item page data (owner)", () => {
  test.skip(
    persona !== "owner" || scenario !== "default" || faults !== "",
    "Needs the default owner world without faults",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  test("the head, the system facts and the custom fields", async ({ page }) => {
    await open(page);
    await expect(page.getByText("Nguyễn Thu Hà tạo ngày 23/09")).toBeVisible();
    await expect(page.getByText("sửa lần cuối 10:06 hôm nay")).toBeVisible();
    const system = page
      .getByRole("complementary")
      .getByText("23/09/2026, 15:10");
    await expect(system).toBeVisible();
    for (const name of ["Phân hệ ERP", "Đổi Ước lượng", "Phòng ban yêu cầu"])
      await expect(
        page.getByRole("button", { name, exact: true }),
      ).toBeVisible();
    await expect(
      page.getByRole("button", { name: "Đổi Ước lượng", exact: true }),
    ).toContainText("40 giờ");
    await expect(page.getByText("chờ", { exact: true })).toBeVisible();
    await expect(page.locator("[data-doc-chip]")).toContainText(
      "Kế hoạch chuyển đổi dữ liệu",
    );
  });

  test("sub-items: five rows, tick one, add one with Enter", async ({
    page,
  }) => {
    await open(page);
    const rows = page.getByRole("list").filter({ hasText: "ERP-142.1" });
    await expect(rows.getByRole("listitem")).toHaveCount(5);
    await expect(page.getByText("ERP-142.4")).toBeVisible();
    await page
      .getByRole("checkbox", { name: "Đối chiếu công nợ đầu kỳ với Kế toán" })
      .click();
    await expect(
      page.getByRole("checkbox", {
        name: "Đối chiếu công nợ đầu kỳ với Kế toán",
      }),
    ).toHaveAttribute("aria-checked", "true");
    await expect(
      page.getByText("Tiến độ 80% tự tính từ việc con"),
    ).toBeVisible();
    await page.getByRole("textbox", { name: "Việc con" }).fill("Kiểm tra lại");
    await page.keyboard.press("Enter");
    await expect(
      page.getByRole("list").filter({ hasText: "ERP-142.1" }),
    ).toContainText("ERP-142.6");
    await expect(page.getByText("Kiểm tra lại").first()).toBeVisible();
  });

  test("links: three groups, add one, remove one", async ({ page }) => {
    await open(page);
    const links = page.getByRole("region", { name: /^Liên kết/ });
    for (const group of ["Đang chờ", "Đang chặn", "Liên quan"])
      await expect(links.getByText(group, { exact: true })).toBeVisible();
    await expect(links.getByText("ERP-129")).toBeVisible();
    await links.getByRole("button", { name: "Thêm" }).click();
    await page.getByRole("radio", { name: "Chờ việc" }).click();
    await page.getByRole("option", { name: /ERP-150/ }).click();
    await expect(links.getByText("ERP-150")).toBeVisible();
    await links.getByText("ERP-150").hover();
    await links
      .getByRole("button", { name: "Gỡ liên kết với ERP-150" })
      .click();
    await expect(links.getByText("ERP-150")).toHaveCount(0);
  });

  test("files: three, and one registered from the drop zone", async ({
    page,
  }) => {
    await open(page);
    const files = page.getByRole("region", { name: /^Tệp/ });
    await expect(files.getByText("khach-hang-da-gop.xlsx")).toBeVisible();
    await expect(
      files.getByText("18,4 MB · Trần Minh Quân · 03/10"),
    ).toBeVisible();
    await expect(
      files.getByText(/2,1 MB · Đỗ Khánh Linh · hôm nay, kèm bình luận/),
    ).toBeVisible();
    await files.locator('input[type="file"]').setInputFiles({
      name: "ghi-chu.pdf",
      mimeType: "application/pdf",
      buffer: Buffer.from("noi dung"),
    });
    await expect(files.getByText("ghi-chu.pdf")).toBeVisible();
  });

  test("follow: the button and the faces", async ({ page }) => {
    await open(page);
    const follow = page.getByRole("button", { name: "Đang theo dõi" });
    await expect(follow).toHaveAttribute("aria-pressed", "true");
    await expect(
      page.getByRole("group", { name: "4 người theo dõi" }),
    ).toBeVisible();
    await follow.click();
    await expect(
      page.getByRole("button", { name: "Theo dõi" }),
    ).toHaveAttribute("aria-pressed", "false");
  });

  test("history: older lines fold and a number field edit is recorded", async ({
    page,
  }) => {
    await open(page);
    await expect(
      page.getByRole("button", { name: "Hiện 12 hoạt động cũ hơn" }),
    ).toBeVisible();
    await page
      .getByRole("button", { name: "Hiện 12 hoạt động cũ hơn" })
      .click();
    await expect(page.getByText("tạo việc").first()).toBeVisible();
    await page
      .getByRole("button", { name: "Đổi Ước lượng", exact: true })
      .click();
    await page.getByRole("spinbutton", { name: "Đổi Ước lượng" }).fill("56");
    await page.keyboard.press("Enter");
    await expect(
      page.getByRole("button", { name: "Đổi Ước lượng", exact: true }),
    ).toContainText("56 giờ");
  });
});
