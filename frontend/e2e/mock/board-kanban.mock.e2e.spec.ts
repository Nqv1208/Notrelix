import { expect, test, type Page } from "@playwright/test";

/**
 * The status Kanban of the ERP board ("Triển khai ERP Q4") on the mock world:
 * lanes by Work Status, a drag (or the sheet's status pill on a phone) that
 * writes the status, the filter with its no-results state, the item sheet and
 * the commenter's read-only board. The page clock is fixed to the fixtures'
 * 12/10/2026 10:40 so "Quá hạn" is stable. The commenter checks run when the
 * server was started with VITE_MOCK_PERSONA=commenter; the owner checks run
 * for the default persona.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const NOW = new Date("2026-10-12T10:40:00+07:00");
const LANES = ["Cần làm", "Đang làm", "Chờ duyệt", "Hoàn thành"] as const;
const ERP_149 = "Viết tài liệu quy trình nhập hoá đơn đầu vào";
const ERP_142 = "Chuyển dữ liệu khách hàng từ hệ thống cũ sang ERP";

const VIEWPORTS = [
  { name: "1440", width: 1440, height: 900 },
  { name: "320", width: 320, height: 720 },
] as const;

function lane(page: Page, name: string) {
  return page.getByRole("region", { name, exact: true });
}

async function laneCounts(page: Page): Promise<string[]> {
  return Promise.all(
    LANES.map(
      async (name) =>
        (await lane(page, name).getAttribute("data-lane-count")) ?? "",
    ),
  );
}

function trackErrors(page: Page): string[] {
  const errors: string[] = [];
  page.on("pageerror", (error) => errors.push(error.message));
  page.on("console", (message) => {
    if (message.text().includes("MockUnhandledOperationError")) {
      errors.push(message.text());
    }
  });
  return errors;
}

async function openBoard(page: Page, query = "") {
  await page.clock.setFixedTime(NOW);
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "vi");
    } catch {
      // Private mode: the default locale applies.
    }
  });
  await page.goto(`${BOARD}${query}`);
  await expect(lane(page, "Cần làm")).toBeVisible();
}

/** Pointer drag from a card to a lane (dnd-kit starts after 8 px). */
async function dragCardToLane(page: Page, cardTitle: string, laneName: string) {
  const card = page.getByRole("button", { name: cardTitle, exact: true });
  const target = lane(page, laneName);
  await card.scrollIntoViewIfNeeded();
  const from = (await card.boundingBox())!;
  const to = (await target.boundingBox())!;
  await page.mouse.move(from.x + from.width / 2, from.y + from.height / 2);
  await page.mouse.down();
  await page.mouse.move(
    from.x + from.width / 2 + 12,
    from.y + from.height / 2,
    {
      steps: 4,
    },
  );
  await page.mouse.move(to.x + to.width / 2, to.y + 80, { steps: 20 });
  await page.mouse.up();
}

for (const viewport of VIEWPORTS) {
  test.describe(`status Kanban at ${viewport.name} px (owner)`, () => {
    test.skip(
      persona !== "owner" || scenario !== "default",
      "Owner checks need the default owner world",
    );
    test.use({ viewport: { width: viewport.width, height: viewport.height } });

    test("groups the ERP items into the status lanes 6/3/2/5", async ({
      page,
    }) => {
      const errors = trackErrors(page);
      await openBoard(page);
      await expect.poll(() => laneCounts(page)).toEqual(["6", "3", "2", "5"]);
      await expect(
        lane(page, "Cần làm").getByRole("button", {
          name: /^Xem thêm 2 việc$/,
        }),
      ).toBeVisible();
      expect(errors).toEqual([]);
    });

    test("moves ERP-149 to Đang làm and keeps it there after the board is read again", async ({
      page,
    }) => {
      const errors = trackErrors(page);
      await openBoard(page);
      if (viewport.width >= 1024) {
        await dragCardToLane(page, ERP_149, "Đang làm");
      } else {
        // On a phone the lanes scroll sideways; the keyboard and touch
        // alternative to a drag is the status pill in the item sheet.
        await page.getByRole("button", { name: ERP_149, exact: true }).click();
        const sheet = page.getByRole("dialog", { name: ERP_149 });
        await sheet
          .getByRole("button", { name: /^Trạng thái: Cần làm/ })
          .click();
        await page.getByRole("menuitem", { name: "Đang làm" }).click();
        await sheet.getByRole("button", { name: "Đóng (Esc)" }).click();
      }
      await expect.poll(() => laneCounts(page)).toEqual(["5", "4", "2", "5"]);
      await expect(
        lane(page, "Đang làm").getByRole("button", {
          name: ERP_149,
          exact: true,
        }),
      ).toBeVisible();

      // Leave the Kanban and come back: the lanes are read from the mock
      // server again and the new status is still there.
      await page.getByRole("tab", { name: "Bảng" }).click();
      await expect(lane(page, "Cần làm")).toHaveCount(0);
      await page.getByRole("tab", { name: "Kanban" }).click();
      await expect.poll(() => laneCounts(page)).toEqual(["5", "4", "2", "5"]);
      expect(errors).toEqual([]);
    });

    test("filters by Quá hạn and Ưu tiên Thấp, shows the no-results bar and clears it with Bỏ lọc", async ({
      page,
    }) => {
      await openBoard(page);
      await page.getByRole("button", { name: "Lọc", exact: true }).click();
      await page.getByRole("menuitemcheckbox", { name: "Quá hạn" }).click();
      await page.getByRole("menuitemcheckbox", { name: "Thấp" }).click();
      await page.keyboard.press("Escape");

      const bar = page
        .getByRole("status")
        .filter({ hasText: "Không có việc khớp bộ lọc" });
      await expect(bar).toContainText("16 việc của board đang bị ẩn");
      expect(new URL(page.url()).searchParams.get("filter")).toBe("p.low~o");
      await expect.poll(() => laneCounts(page)).toEqual(["0", "0", "0", "0"]);

      await bar.getByRole("button", { name: "Bỏ lọc" }).click();
      await expect(bar).toHaveCount(0);
      await expect.poll(() => laneCounts(page)).toEqual(["6", "3", "2", "5"]);
      expect(new URL(page.url()).searchParams.has("filter")).toBe(false);
    });

    test("opens the item sheet and returns focus to the card when it closes", async ({
      page,
    }) => {
      await openBoard(page);
      const card = page.getByRole("button", { name: ERP_142, exact: true });
      await card.click();
      const sheet = page.getByRole("dialog", { name: ERP_142 });
      await expect(sheet).toBeVisible();
      expect(new URL(page.url()).searchParams.get("item")).toBeTruthy();
      await page.keyboard.press("Escape");
      await expect(sheet).toHaveCount(0);
      await expect(card).toBeFocused();
      expect(new URL(page.url()).searchParams.has("item")).toBe(false);
    });
  });

  test.describe(`status Kanban at ${viewport.name} px (commenter)`, () => {
    test.skip(
      persona !== "commenter",
      "Needs the server started with VITE_MOCK_PERSONA=commenter",
    );
    test.use({ viewport: { width: viewport.width, height: viewport.height } });

    test("shows the commenter banner, offers no add and no drag, and a read-only sheet", async ({
      page,
    }) => {
      const errors = trackErrors(page);
      await openBoard(page);
      await expect(page.getByRole("note")).toHaveText(
        "Bạn có quyền bình luận trên board này · được Lê Hoàng Phúc thêm riêng",
      );
      await expect.poll(() => laneCounts(page)).toEqual(["6", "3", "2", "5"]);
      await expect(
        page.getByRole("button", { name: /^Thêm vào / }),
      ).toHaveCount(0);
      await expect(
        page.getByRole("button", { name: "Thêm công việc" }),
      ).toHaveCount(0);

      if (viewport.width >= 1024) {
        await dragCardToLane(page, ERP_149, "Đang làm");
        await expect.poll(() => laneCounts(page)).toEqual(["6", "3", "2", "5"]);
      }

      await page.getByRole("button", { name: ERP_142, exact: true }).click();
      const sheet = page.getByRole("dialog", { name: ERP_142 });
      await expect(
        sheet.getByRole("button", { name: /^Trạng thái: / }),
      ).toHaveAttribute("aria-disabled", "true");
      expect(errors).toEqual([]);
    });
  });
}
