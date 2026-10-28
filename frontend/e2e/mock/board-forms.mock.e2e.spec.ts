import { expect, test, type Page } from "@playwright/test";

/**
 * Forms of the ERP board on the mock world. A manager lists the board's forms
 * in "Thiết lập board › Biểu mẫu", builds one, and reads its inbox, where
 * "Tạo việc từ phản hồi" opens the ordinary add-item dialog prefilled; a member
 * fills the form from its in-app link and the answers wait in the inbox. A
 * viewer neither sees the section nor can send the form.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const SUPPORT = `${BOARD}/forms/mock-form-it-support`;
const NOW = new Date("2026-10-12T10:40:00+07:00");

async function open(page: Page, path: string) {
  await page.clock.setFixedTime(NOW);
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "vi");
    } catch {
      // Private mode: the default locale applies.
    }
  });
  await page.goto(path);
}

test.describe("Board forms (owner)", () => {
  test.skip(
    persona !== "owner" || scenario !== "default" || faults !== "",
    "Needs the default owner world without faults",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  test("lists the forms with their status, and creates a draft that opens in the builder", async ({
    page,
  }) => {
    await open(page, `${BOARD}/settings?section=forms`);
    await expect(
      page.getByRole("heading", { name: "Biểu mẫu", level: 2 }),
    ).toBeVisible();
    await expect(page.getByText("Đang nhận", { exact: true })).toBeVisible();
    await expect(page.getByText("Bản nháp", { exact: true })).toBeVisible();
    await expect(page.getByText("Đã đóng", { exact: true })).toBeVisible();
    await expect(page.getByText("3 mới")).toBeVisible();

    await page.getByRole("button", { name: "Tạo biểu mẫu" }).click();
    await page.getByRole("button", { name: "Tạo", exact: true }).click();
    await expect(page.getByText("Nhập tên biểu mẫu.")).toBeVisible();
    await page
      .getByRole("textbox", { name: "Tên biểu mẫu mới" })
      .fill("Yêu cầu cấp quyền");
    await page.getByRole("button", { name: "Tạo", exact: true }).click();

    await expect(page).toHaveURL(/\/forms\/[^/]+\/edit$/);
    await expect(
      page.getByRole("textbox", { name: "Tên biểu mẫu" }),
    ).toHaveValue("Yêu cầu cấp quyền");
    // No question yet: it cannot be published.
    await expect(page.getByRole("button", { name: "Xuất bản" })).toBeDisabled();
    await page.getByRole("button", { name: "Thêm câu hỏi" }).click();
    await page.getByRole("button", { name: "Tiêu đề việc" }).click();
    await expect(page.getByRole("list", { name: "Câu hỏi" })).toContainText(
      "Tiêu đề việc",
    );
    await page.getByRole("button", { name: "Xuất bản" }).click();
    await expect(page.getByText("Đã xuất bản")).toBeVisible();
    await expect(
      page.getByRole("textbox", { name: "Liên kết biểu mẫu" }),
    ).toHaveValue(/\/forms\/[^/]+$/);
  });

  test("shows a mapped select's options read-only and moves a question down", async ({
    page,
  }) => {
    await open(page, `${BOARD}/forms/mock-form-it-support/edit`);
    const list = page.getByRole("list", { name: "Câu hỏi" });
    await expect(list.getByRole("listitem")).toHaveCount(5);
    await list.getByRole("button", { name: /Mức độ ưu tiên/ }).click();
    const panel = page.getByRole("complementary", {
      name: "Thiết lập câu hỏi",
    });
    await expect(panel).toContainText("Lựa chọn lấy từ trường Ưu tiên");
    await expect(
      panel.getByRole("button", { name: "Thêm lựa chọn" }),
    ).toHaveCount(0);

    await page
      .getByRole("button", { name: "Chuyển câu hỏi 'Bạn cần gì?' xuống" })
      .click();
    await expect(list.getByRole("listitem").first()).toContainText(
      "Mô tả chi tiết",
    );
  });

  test("a member fills the form: errors first, then the answers wait in the inbox", async ({
    page,
  }) => {
    await open(page, SUPPORT);
    await expect(
      page.getByRole("heading", { name: "Yêu cầu hỗ trợ IT" }),
    ).toBeVisible();
    await page.getByRole("button", { name: "Gửi yêu cầu" }).click();
    await expect(page.getByText("Chưa gửi được.")).toBeVisible();
    await expect(
      page
        .getByText("Nhập điều bạn cần.")
        .or(page.getByText("Nhập câu trả lời cho câu hỏi này.")),
    ).toBeVisible();

    await page
      .getByRole("textbox", { name: /Bạn cần gì/ })
      .fill("Cấp thêm chuột không dây");
    await page.getByLabel(/Mức độ ưu tiên/).selectOption({ label: "Cao" });
    await page.getByRole("button", { name: "Gửi yêu cầu" }).click();
    await expect(
      page.getByRole("heading", { name: "Đã gửi yêu cầu" }),
    ).toBeVisible();

    await open(page, `${SUPPORT}/responses`);
    await expect(page.getByRole("tab", { name: /Mới\s*4/ })).toBeVisible();
    await expect(
      page.getByRole("option", { name: /Cấp thêm chuột không dây/ }),
    ).toBeVisible();
  });

  test("turns a response into an item through the ordinary add-item dialog, prefilled", async ({
    page,
  }) => {
    await open(page, `${SUPPORT}/responses`);
    await expect(page.getByRole("tab", { name: /Mới\s*3/ })).toBeVisible();
    await page.getByRole("option", { name: /Làm mới tên máy kế toán/ }).click();
    await expect(
      page.getByRole("article", { name: "Chi tiết phản hồi" }),
    ).toContainText("→ Tiêu đề việc");

    await page.getByRole("button", { name: "Tạo việc từ phản hồi" }).click();
    const dialog = page.getByRole("dialog", { name: "Tạo việc từ phản hồi" });
    await expect(
      dialog.getByRole("textbox", { name: /Tên công việc/ }),
    ).toHaveValue("Làm mới tên máy kế toán Đà Nẵng");
    // The same validation as the ordinary dialog, and nothing typed is lost.
    await dialog.getByRole("textbox", { name: /Tên công việc/ }).fill("");
    await dialog.getByRole("button", { name: "Tạo công việc" }).click();
    await expect(
      dialog.getByText("Hãy đặt tên cho công việc để tiếp tục"),
    ).toBeVisible();
    await expect(dialog.getByRole("textbox", { name: /^Mô tả/ })).toHaveValue(
      /tên cũ còn nằm trong domain/,
    );
    await dialog
      .getByRole("textbox", { name: /Tên công việc/ })
      .fill("Đổi tên máy kế toán Đà Nẵng");
    await dialog.getByRole("button", { name: "Tạo công việc" }).click();

    await expect(dialog).toBeHidden();
    await expect(page.getByRole("tab", { name: /Mới\s*2/ })).toBeVisible();
    await page.getByRole("tab", { name: /Đã xử lý/ }).click();
    await expect(
      page.getByRole("option", { name: /Làm mới tên máy kế toán/ }),
    ).toBeVisible();
  });

  test("rejects a response without a reason, at once and without an undo", async ({
    page,
  }) => {
    await open(page, `${SUPPORT}/responses`);
    await page.getByRole("option", { name: /Cấp thêm 2 màn hình/ }).click();
    await page.getByRole("button", { name: "Từ chối…" }).click();
    await page
      .getByRole("dialog", { name: "Từ chối phản hồi này?" })
      .getByRole("button", { name: "Từ chối", exact: true })
      .click();
    await expect(page.getByText("Đã từ chối", { exact: true })).toBeVisible();
    await expect(page.getByRole("tab", { name: /Mới\s*2/ })).toBeVisible();
    await expect(page.getByRole("button", { name: "Hoàn tác" })).toHaveCount(0);
  });

  test("keeps the admin screens usable at 768 px", async ({ page }) => {
    await page.setViewportSize({ width: 768, height: 900 });
    await open(page, `${BOARD}/settings?section=forms`);
    await expect(
      page.getByRole("button", { name: "Tạo biểu mẫu" }),
    ).toBeVisible();
    await expect(page.getByText("Yêu cầu hỗ trợ IT")).toBeVisible();
    const overflow = await page.evaluate(
      () =>
        document.documentElement.scrollWidth -
        document.documentElement.clientWidth,
    );
    expect(overflow).toBeLessThanOrEqual(0);
    await open(page, `${SUPPORT}/responses`);
    await expect(page.getByRole("tab", { name: /Mới/ })).toBeVisible();
    await expect(
      page.evaluate(
        () =>
          document.documentElement.scrollWidth -
          document.documentElement.clientWidth,
      ),
    ).resolves.toBeLessThanOrEqual(0);
  });
});

test.describe("Board forms (viewer)", () => {
  test.skip(
    persona !== "viewer" || scenario !== "default",
    "Needs the viewer persona in the default world",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  test("does not see the Biểu mẫu section and cannot open the builder or the inbox", async ({
    page,
  }) => {
    await open(page, `${BOARD}/settings?section=forms`);
    await expect(page.getByRole("button", { name: /^Biểu mẫu/ })).toHaveCount(
      0,
    );
    await open(page, `${SUPPORT}/edit`);
    await expect(
      page.getByText(
        "Biểu mẫu không tồn tại hoặc bạn không quản lý board này.",
      ),
    ).toBeVisible();
    await open(page, `${SUPPORT}/responses`);
    await expect(page.getByText("Phản hồi")).toHaveCount(0);
  });
});
