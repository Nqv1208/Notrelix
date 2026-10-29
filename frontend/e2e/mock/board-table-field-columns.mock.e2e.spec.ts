import { expect, test, type Page } from "@playwright/test";

/**
 * The custom field columns of the ERP board's Table on the mock world: a long
 * text, a link and a formula edited or read in the cell. Owner checks run in the default owner world; the viewer check runs when the
 * server was started with VITE_MOCK_PERSONA=viewer or commenter.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const NOW = new Date("2026-10-12T10:40:00+07:00");

async function openTable(page: Page) {
  await page.clock.setFixedTime(NOW);
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "vi");
    } catch {
      // Private mode: the default locale applies.
    }
  });
  await page.goto(`${BOARD}?view=table`);
  await expect(page.getByRole("rowgroup").first()).toBeVisible();
}

/** The custom field columns are part of the table by default; each header is there. */
async function expectColumns(page: Page, names: string[]) {
  for (const name of names) {
    await expect(
      page.getByRole("columnheader").filter({ hasText: name }).first(),
    ).toBeAttached();
  }
}

const row = (page: Page, key: string) =>
  page.locator("[data-row-id]").filter({ hasText: key }).first();

test.describe("Table custom field columns (owner)", () => {
  test.skip(
    persona !== "owner" || scenario !== "default" || faults !== "",
    "Needs the default owner world without faults",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  test("long text: Enter saves from the popover, Escape puts the old text back", async ({
    page,
  }) => {
    await openTable(page);
    await expectColumns(page, ["Ghi chú"]);
    const edit = row(page, "ERP-144").getByRole("button", {
      name: /^Sửa Ghi chú của ERP-144/,
    });
    await expect(edit).toContainText("Cần xuất bản Excel cho kế toán trưởng");

    await edit.click();
    const box = page.getByRole("textbox", { name: /Ghi chú/ });
    await box.fill("Đã gửi bản Excel cho kế toán trưởng");
    await page.keyboard.press("Escape");
    await expect(box).toHaveCount(0);
    await expect(edit).toContainText("Cần xuất bản Excel cho kế toán trưởng");

    await edit.click();
    await page
      .getByRole("textbox", { name: /Ghi chú/ })
      .fill("Đã gửi bản Excel cho kế toán trưởng");
    await page.keyboard.press("Enter");
    await expect(
      row(page, "ERP-144").getByRole("button", {
        name: /^Sửa Ghi chú của ERP-144/,
      }),
    ).toContainText("Đã gửi bản Excel");
    await expect(page.getByRole("status")).toHaveCount(0);
  });

  test("link: a script address is refused with its reason, a plain one is saved as https and opens in a new tab", async ({
    page,
  }) => {
    await openTable(page);
    await expectColumns(page, ["Liên kết"]);
    await row(page, "ERP-144")
      .getByRole("button", { name: /^Sửa Liên kết của ERP-144/ })
      .click();
    const input = page.getByRole("textbox", { name: /Liên kết/ });
    await input.fill("javascript:alert(1)");
    await page.keyboard.press("Enter");
    await expect(page.getByRole("alert")).toContainText("http");
    await expect(input).toHaveAttribute("aria-invalid", "true");

    await input.fill("erp.minhan.vn/t/999");
    await page.keyboard.press("Enter");
    const open = row(page, "ERP-144").getByRole("link", {
      name: "Mở liên kết trong thẻ mới",
    });
    await expect(open).toHaveAttribute("href", "https://erp.minhan.vn/t/999");
    await expect(open).toHaveAttribute("target", "_blank");
  });

  test("formula: shows ƒx with the result, has no editor and follows its source", async ({
    page,
  }) => {
    await openTable(page);
    await expectColumns(page, ["Điểm", "Điểm gấp đôi"]);
    const header = page
      .getByRole("columnheader")
      .filter({ hasText: "Điểm gấp đôi" });
    await expect(header).toContainText("ƒx");
    await expect(
      header.getByRole("img", { name: /Tự tính, chỉ xem/ }),
    ).toBeVisible();

    const double = row(page, "ERP-147").getByRole("img", {
      name: /^Tự tính: /,
    });
    await expect(double).toHaveAccessibleName("Tự tính: 10");
    await expect(double.getByRole("button")).toHaveCount(0);

    await row(page, "ERP-147")
      .getByRole("button", { name: /^Sửa Điểm của ERP-147/ })
      .click();
    const points = page.getByRole("textbox", { name: /Điểm/ });
    await points.fill("21");
    await page.keyboard.press("Enter");
    await expect(
      row(page, "ERP-147").getByRole("img", { name: "Tự tính: 42" }),
    ).toBeVisible();
  });
});

test.describe("Table custom field columns (viewer)", () => {
  test.skip(
    !["viewer", "commenter"].includes(persona) || scenario !== "default",
    "Needs VITE_MOCK_PERSONA=viewer or commenter",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  test("shows the values with a lock in each field header and no editor", async ({
    page,
  }) => {
    await openTable(page);
    await expectColumns(page, ["Ghi chú", "Điểm"]);
    await expect(row(page, "ERP-144")).toContainText(
      "Cần xuất bản Excel cho kế toán trưởng",
    );
    await expect(
      row(page, "ERP-144").getByRole("button", { name: /^Sửa/ }),
    ).toHaveCount(0);
    await expect(
      page
        .getByRole("columnheader")
        .filter({ hasText: "Điểm" })
        .first()
        .getByRole("img", { name: /bạn không có quyền sửa/ }),
    ).toBeVisible();
  });
});
