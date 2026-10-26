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
const faults = process.env.VITE_MOCK_FAULTS ?? "";
/** The fault run: the mock answers 500 to ERP-149's status write. */
const statusFault = faults.includes("/board-items/mock-erp-item-149/status");
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
      persona !== "owner" || scenario !== "default" || faults !== "",
      "Owner checks need the default owner world without faults",
    );
    test.use({ viewport: { width: viewport.width, height: viewport.height } });

    test("groups the ERP items into the status lanes 6/3/2/5", async ({
      page,
    }) => {
      const errors = trackErrors(page);
      await openBoard(page);
      await expect.poll(() => laneCounts(page)).toEqual(["6", "3", "2", "5"]);
      // A lane scrolls and shows all its cards: paging starts only after 20.
      await expect(
        lane(page, "Cần làm").locator("button[data-card-id]"),
      ).toHaveCount(6);
      await expect(
        lane(page, "Cần làm").getByRole("button", { name: /^Xem thêm/ }),
      ).toHaveCount(0);
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
      // The lane shows every card, so the card that arrived is already there.
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

    test("reorders a card inside its lane and keeps the order after the board is read again", async ({
      page,
    }) => {
      test.skip(viewport.width < 1024, "Lanes scroll sideways on a phone");
      await openBoard(page);
      const todo = lane(page, "Cần làm");
      const titles = () =>
        todo
          .locator("[data-card-id]")
          .evaluateAll((buttons) =>
            buttons.map((button) => button.getAttribute("aria-label")),
          );
      // The lane runs through the board's groups (Chuẩn bị, then Đào tạo và
      // vận hành, ...); an order change is made between cards of one group:
      // the second to fourth card of the lane belong to the same group.
      const third = "Chuẩn bị dữ liệu mẫu cho buổi đào tạo người dùng";
      const first = (await titles())[1]!;
      const from = (await todo
        .getByRole("button", { name: third, exact: true })
        .boundingBox())!;
      const to = (await todo
        .getByRole("button", { name: first, exact: true })
        .boundingBox())!;
      await page.mouse.move(from.x + from.width / 2, from.y + from.height / 2);
      await page.mouse.down();
      await page.mouse.move(
        from.x + from.width / 2,
        from.y + from.height / 2 - 12,
        {
          steps: 4,
        },
      );
      await page.mouse.move(to.x + to.width / 2, to.y + 4, { steps: 20 });
      await page.mouse.up();
      await expect.poll(async () => (await titles())[1]).toBe(third);

      await page.getByRole("tab", { name: "Bảng" }).click();
      await expect(lane(page, "Cần làm")).toHaveCount(0);
      await page.getByRole("tab", { name: "Kanban" }).click();
      await expect.poll(async () => (await titles())[1]).toBe(third);
      await expect.poll(() => laneCounts(page)).toEqual(["6", "3", "2", "5"]);
    });

    test("keeps keyboard focus out of the board the sheet covers", async ({
      page,
    }) => {
      test.skip(viewport.width >= 768, "The sheet covers the board on a phone");
      await openBoard(page);
      await page.getByRole("button", { name: ERP_142, exact: true }).click();
      await expect(page.getByRole("dialog", { name: ERP_142 })).toBeVisible();
      for (let step = 0; step < 25; step += 1) {
        await page.keyboard.press("Tab");
        const inCoveredBoard = await page.evaluate(() => {
          const active = document.activeElement;
          return Boolean(
            active?.closest("[data-kanban-board]") &&
            !active.closest("[role='dialog']"),
          );
        });
        expect(inCoveredBoard).toBe(false);
      }
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

  test.describe(`status Kanban at ${viewport.name} px (failed write)`, () => {
    test.skip(
      persona !== "owner" || !statusFault,
      "Needs VITE_MOCK_FAULTS=/board-items/mock-erp-item-149/status=500",
    );
    test.use({ viewport: { width: viewport.width, height: viewport.height } });

    test("puts ERP-149 back in Cần làm and says so when its status write fails", async ({
      page,
    }) => {
      await openBoard(page);
      if (viewport.width >= 1024) {
        await dragCardToLane(page, ERP_149, "Đang làm");
      } else {
        await page.getByRole("button", { name: ERP_149, exact: true }).click();
        const sheet = page.getByRole("dialog", { name: ERP_149 });
        await sheet
          .getByRole("button", { name: /^Trạng thái: Cần làm/ })
          .click();
        await page.getByRole("menuitem", { name: "Đang làm" }).click();
      }
      await expect(
        page.getByText(
          "Không chuyển được “Viết tài liệu quy trình nhập hoá đơn đầu vào”. Việc đã về lại Cần làm.",
        ),
      ).toBeVisible();
      if (viewport.width < 1024) {
        // The sheet covers the board on a phone; close it to read the lanes.
        await page.keyboard.press("Escape");
        await expect(page.getByRole("dialog")).toHaveCount(0);
      }
      await expect.poll(() => laneCounts(page)).toEqual(["6", "3", "2", "5"]);
      await expect(
        lane(page, "Cần làm").getByRole("button", {
          name: ERP_149,
          exact: true,
        }),
      ).toBeAttached();
    });
  });

  test.describe(`status Kanban at ${viewport.name} px (viewer)`, () => {
    test.skip(
      persona !== "viewer" || scenario !== "default",
      "Needs the server started with VITE_MOCK_PERSONA=viewer",
    );
    test.use({ viewport: { width: viewport.width, height: viewport.height } });

    test("gives a viewer a read-only board and sheet", async ({ page }) => {
      await openBoard(page);
      await expect(page.getByRole("note")).toContainText(
        "Bạn có quyền xem board này",
      );
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
      await expect(
        sheet.getByRole("textbox", { name: /bình luận/i }),
      ).toBeDisabled();
    });
  });
}
