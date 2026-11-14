import { expect, test, type Page } from "@playwright/test";

/**
 * UC-WM-H13 and UC-WM-E05 on the mock backend, 1440 px, Vietnamese.
 * - H13: the tabs write the layout into the address, Back and Forward follow the address every time, and an
 *   address naming a layout or a saved view that does not exist shows the default view with one notice.
 * - E05: a collapsed Table group and a collapsed Kanban column stay collapsed after a refetch (a write that
 *   re-reads the board) and after the page is opened again on this device; nothing is written to the server.
 */
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const NOW = new Date("2026-10-12T10:40:00+07:00");

test.skip(
  scenario !== "default" || persona !== "owner" || faults !== "",
  "Needs the default owner world",
);
test.use({ viewport: { width: 1440, height: 900 } });

async function open(page: Page, query: string) {
  await page.clock.setFixedTime(NOW);
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "vi");
    } catch {
      // Private mode: the defaults apply.
    }
  });
  await page.goto(`${BOARD}${query}`);
  await expect(page.getByRole("tablist", { name: "Chế độ xem" })).toBeVisible();
}

const layoutOf = (page: Page) => new URL(page.url()).searchParams.get("view");
const selected = (page: Page, name: string) =>
  expect(
    page.getByRole("tab", { name, exact: true, selected: true }),
  ).toBeVisible();

test.describe("UC-WM-H13 views in the address", () => {
  test("Back and Forward follow the address through every switch", async ({
    page,
  }) => {
    await open(page, "?view=table");
    await selected(page, "Bảng");
    const path: [string, string][] = [
      ["Kanban", "kanban"],
      ["Lịch", "calendar"],
      ["Dòng thời gian", "timeline"],
    ];
    for (const [tab, layout] of path) {
      await page.getByRole("tab", { name: tab, exact: true }).click();
      await expect.poll(() => layoutOf(page)).toBe(layout);
      await selected(page, tab);
    }
    // Back walks the switches in reverse, each time showing the layout the address names.
    for (const [tab, layout] of [
      ["Lịch", "calendar"],
      ["Kanban", "kanban"],
      ["Bảng", "table"],
    ] as const) {
      await page.goBack();
      await expect.poll(() => layoutOf(page)).toBe(layout);
      await selected(page, tab);
    }
    // Forward replays them.
    for (const [tab, layout] of path) {
      await page.goForward();
      await expect.poll(() => layoutOf(page)).toBe(layout);
      await selected(page, tab);
    }
    // A reload keeps what the address says.
    await page.reload();
    await selected(page, "Dòng thời gian");
  });

  test("an unknown layout shows the default view with one notice", async ({
    page,
  }) => {
    await open(page, "?view=gantt");
    const notice = page.getByText("Chế độ xem này không còn nữa");
    await expect(notice).toHaveCount(1);
    await expect.poll(() => layoutOf(page)).toBe("kanban");
    await selected(page, "Kanban");
    // The address no longer names the unknown layout, so Back does not bring it back.
    expect(page.url()).not.toContain("gantt");
    // The notice closes and does not return on a switch.
    await page.getByRole("button", { name: "Đóng", exact: true }).click();
    await expect(notice).toHaveCount(0);
    await page.getByRole("tab", { name: "Bảng", exact: true }).click();
    await selected(page, "Bảng");
    await expect(notice).toHaveCount(0);
  });

  test("an unknown saved view shows the default view with one notice", async ({
    page,
  }) => {
    await open(page, "?view=table&savedView=view-that-was-deleted");
    await expect(page.getByText("Chế độ xem này không còn nữa")).toHaveCount(1);
    await expect
      .poll(() => new URL(page.url()).searchParams.get("savedView"))
      .toBeNull();
  });
});

test.describe("UC-WM-E05 collapsed groups", () => {
  test("a collapsed Table group stays collapsed after a refetch and a reopen", async ({
    page,
  }) => {
    const writes: string[] = [];
    page.on("request", (request) => {
      if (request.method() !== "GET") writes.push(request.url());
    });
    await open(page, "?view=table");
    await expect(page.getByRole("rowgroup").first()).toBeVisible();
    await page.getByRole("button", { name: "Thu gọn nhóm Chờ duyệt" }).click();
    const expand = page.getByRole("button", { name: "Mở nhóm Chờ duyệt" });
    await expect(expand).toBeVisible();
    // Collapsing is personal and client-side: nothing goes to the server.
    expect(writes.filter((url) => !url.includes("/telemetry"))).toEqual([]);

    // A write elsewhere re-reads the board: the group stays collapsed.
    await page.getByRole("button", { name: "Đổi ưu tiên của ERP-153" }).click();
    await page.getByRole("menuitem", { name: "Khẩn" }).click();
    await expect(
      page.getByRole("button", { name: "Đổi ưu tiên của ERP-153" }),
    ).toContainText("Khẩn");
    await page.waitForTimeout(600);
    await expect(expand).toBeVisible();

    // Another view and back, then a reload: still collapsed on this device.
    await page.getByRole("tab", { name: "Kanban", exact: true }).click();
    await selected(page, "Kanban");
    await page.getByRole("tab", { name: "Bảng", exact: true }).click();
    await expect(expand).toBeVisible();
    await page.reload();
    await expect(page.getByRole("rowgroup").first()).toBeVisible();
    await expect(expand).toBeVisible();
    await expect(
      page.getByRole("button", { name: "Thu gọn nhóm Cần làm" }),
    ).toBeVisible();

    // Expanding is remembered too.
    await expand.click();
    await page.reload();
    await expect(
      page.getByRole("button", { name: "Thu gọn nhóm Chờ duyệt" }),
    ).toBeVisible();
  });

  test("a collapsed Kanban column stays collapsed after a refetch and a reopen", async ({
    page,
  }) => {
    await open(page, "?view=kanban");
    await page.getByRole("button", { name: "Thu gọn cột Hoàn thành" }).click();
    await expect(
      page.getByRole("button", { name: "Thu gọn cột Hoàn thành" }),
    ).toHaveCount(0);
    // A write re-reads the board: switch to the Table, change an item, and come back.
    await page.getByRole("tab", { name: "Bảng", exact: true }).click();
    await page.getByRole("button", { name: "Đổi ưu tiên của ERP-153" }).click();
    await page.getByRole("menuitem", { name: "Cao" }).click();
    await page.getByRole("tab", { name: "Kanban", exact: true }).click();
    await selected(page, "Kanban");
    await expect(
      page.getByRole("button", { name: "Thu gọn cột Cần làm" }),
    ).toBeVisible();
    await expect(
      page.getByRole("button", { name: "Thu gọn cột Hoàn thành" }),
    ).toHaveCount(0);
    await page.reload();
    await expect(
      page.getByRole("button", { name: "Thu gọn cột Cần làm" }),
    ).toBeVisible();
    await expect(
      page.getByRole("button", { name: "Thu gọn cột Hoàn thành" }),
    ).toHaveCount(0);
  });
});
