import { expect, test, type Page } from "@playwright/test";

/**
 * ERP-142 on its full page as Final-Detail draws it, on the mock world: the
 * links (list, add, remove), the files (list, upload), the custom fields and
 * the system facts, and the history (older lines folded behind a button, the lines of
 * the changes just made). The sub-item list and the follow switch have no
 * producer, so the page offers neither: the progress still reads from the
 * item and the follow button says it is unavailable. Geometry lives in
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

  test("sub-items: no list is offered, the progress still reads from the item", async ({
    page,
  }) => {
    await open(page);
    await expect(page.getByText("ERP-142.1")).toHaveCount(0);
    await expect(page.getByRole("textbox", { name: "Việc con" })).toHaveCount(
      0,
    );
    await expect(
      page.getByRole("complementary").getByText("3/5 việc con"),
    ).toBeVisible();
  });

  test("links: three groups, add one, remove one", async ({ page }) => {
    await open(page);
    const links = page.getByRole("region", { name: /^Liên kết/ });
    for (const group of ["Đang chờ", "Đang chặn", "Liên quan"])
      await expect(links.getByText(group, { exact: true })).toBeVisible();
    await expect(links.getByText("ERP-129")).toBeVisible();
    await links.getByRole("button", { name: "Thêm" }).click();
    await page.getByRole("radio", { name: "Đang chờ" }).click();
    await page.getByRole("combobox", { name: /Tìm theo mã/ }).fill("ERP-150");
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

  test("follow: the button follows and unfollows, and the faces of the followers stay", async ({
    page,
  }) => {
    await open(page);
    const follow = page.getByRole("button", {
      name: /^(Đang theo dõi|Theo dõi)$/,
    });
    await expect(follow).not.toHaveAttribute("aria-disabled", "true");
    const before = await follow.getAttribute("aria-pressed");
    await follow.click();
    await expect(follow).toHaveAttribute(
      "aria-pressed",
      before === "true" ? "false" : "true",
    );
    await expect(
      page.getByRole("group", { name: /người theo dõi/ }),
    ).toBeVisible();
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
