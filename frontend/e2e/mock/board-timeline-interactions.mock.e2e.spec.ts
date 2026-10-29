import { expect, test, type Page } from "@playwright/test";

/**
 * Timeline interactions on the mock world (Hướng chốt, WM-TL-Interactions):
 * the zoom presets, linking by connector drag and by the "Liên kết với…"
 * dialog, the loop refusal, unlinking, the undo toasts, the Chưa lên lịch tray
 * and the viewer's disabled state. The seven links the mock board starts with
 * are drawn as seven arrowheads.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const NOW = new Date("2026-10-12T10:40:00+07:00");

async function openTimeline(page: Page) {
  await page.clock.setFixedTime(NOW);
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "vi");
    } catch {
      // Private mode: the default locale applies.
    }
  });
  await page.goto(`${BOARD}?view=timeline`);
  await expect(page.getByRole("grid")).toBeVisible();
  await expect(page.locator('[data-tl="gb"]').first()).toBeVisible();
}

async function trackOf(page: Page, key: string) {
  const id = await page
    .locator('[data-tl="lrow"]')
    .filter({ hasText: key })
    .first()
    .getAttribute("data-row-id");
  return page.locator(`[data-tl="trow"][data-row-id="${id}"]`);
}

const arrows = (page: Page) => page.locator('[data-tl="ah"]');

async function openLinkDialog(page: Page, key: string) {
  const track = await trackOf(page, key);
  await track.hover();
  const connector = track.locator('[data-tl="connector"]');
  await connector.focus();
  await page.keyboard.press("Enter");
  const dialog = page.getByRole("dialog", { name: /Liên kết với/ });
  await expect(dialog).toBeVisible();
  return dialog;
}

test.describe("Board Timeline interactions (owner)", () => {
  test.skip(
    persona !== "owner" || scenario !== "default" || faults !== "",
    "Owner checks need the default owner world without faults",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  test("Hôm nay is dim at today and primary away from it; Vừa khít refits the window", async ({
    page,
  }) => {
    await openTimeline(page);
    const today = page.getByRole("button", { name: "Cuộn tới hôm nay" });
    await expect(today).toHaveAttribute("aria-current", "date");
    await page.getByRole("button", { name: "Tiến", exact: true }).click();
    await expect(today).not.toHaveAttribute("aria-current", "date");
    await page.getByRole("button", { name: "Vừa khít" }).click();
    await expect(
      page
        .getByRole("radiogroup", { name: "Thang thời gian" })
        .getByRole("radio", {
          checked: true,
        }),
    ).toHaveCount(1);
    await expect(page.locator('[data-tl="gb"]').first()).toBeVisible();
  });

  test("Tab to the connector and Enter links two items, and Hoàn tác removes the link", async ({
    page,
  }) => {
    await openTimeline(page);
    await expect(arrows(page)).toHaveCount(7);
    const dialog = await openLinkDialog(page, "ERP-144");
    await dialog.getByRole("combobox").fill("ERP-151");
    await dialog.getByRole("option", { name: /ERP-151/ }).click();
    await dialog.getByRole("button", { name: "Tạo liên kết" }).click();
    await expect(page.getByText("Đã nối: ERP-144 chờ ERP-151")).toBeVisible();
    await expect(arrows(page)).toHaveCount(8);
    await page.getByRole("button", { name: "Hoàn tác" }).click();
    await expect(arrows(page)).toHaveCount(7);
  });

  test("dragging a connector onto a bar draws the dashed path and links on drop", async ({
    page,
  }) => {
    await openTimeline(page);
    const from = await trackOf(page, "ERP-144");
    const to = await trackOf(page, "ERP-151");
    await from.hover();
    const connector = (await from
      .locator('[data-tl="connector"]')
      .boundingBox())!;
    const bar = (await to.locator('[data-tl="gb"]').boundingBox())!;
    await page.mouse.move(connector.x + 12, connector.y + 12);
    await page.mouse.down();
    await page.mouse.move(bar.x + 20, bar.y + bar.height / 2, { steps: 6 });
    await expect(page.locator('[data-tl="linkdrag"]')).toBeVisible();
    await expect(page.locator('[data-tl="linktarget"]')).toHaveAttribute(
      "data-state",
      "ok",
    );
    await expect(to.getByRole("status")).toContainText(
      "Thả để ERP-151 chờ ERP-144",
    );
    await page.mouse.up();
    await expect(page.getByText("Đã nối: ERP-151 chờ ERP-144")).toBeVisible();
    await expect(arrows(page)).toHaveCount(8);
  });

  test("a drop that would close a loop is refused before the drop", async ({
    page,
  }) => {
    await openTimeline(page);
    // ERP-142 -> ERP-146 -> ERP-150: ERP-150 waiting for ERP-142's chain back is a loop.
    const from = await trackOf(page, "ERP-150");
    const to = await trackOf(page, "ERP-142");
    await from.locator('[data-tl="gb"]').hover();
    const connector = (await from
      .locator('[data-tl="connector"]')
      .boundingBox())!;
    const bar = (await to.locator('[data-tl="gb"]').boundingBox())!;
    await page.mouse.move(connector.x + 12, connector.y + 12);
    await page.mouse.down();
    await page.mouse.move(bar.x + 20, bar.y + bar.height / 2, { steps: 6 });
    await expect(page.locator('[data-tl="linktarget"]')).toHaveAttribute(
      "data-state",
      "cycle",
    );
    await expect(to.getByRole("status")).toContainText("vòng phụ thuộc");
    await page.mouse.up();
    await expect(arrows(page)).toHaveCount(7);
  });

  test("the dialog disables a row that closes a loop and announces the chain", async ({
    page,
  }) => {
    await openTimeline(page);
    const dialog = await openLinkDialog(page, "ERP-142");
    const row = dialog.getByRole("option", { name: /ERP-150/ });
    await expect(row).toHaveAttribute("aria-disabled", "true");
    await row.click({ force: true });
    await expect(dialog.getByRole("alert")).toContainText("vòng phụ thuộc");
    await expect(
      dialog.getByRole("button", { name: "Tạo liên kết" }),
    ).toBeDisabled();
  });

  test("Gỡ in the dialog unlinks, and Hoàn tác links again", async ({
    page,
  }) => {
    await openTimeline(page);
    const dialog = await openLinkDialog(page, "ERP-146");
    await dialog
      .getByRole("button", { name: /Gỡ liên kết ERP-146 chờ ERP-142/ })
      .click();
    await expect(page.getByText(/Đã gỡ liên kết/)).toBeVisible();
    await expect(arrows(page)).toHaveCount(6);
    await page.keyboard.press("Escape");
    await page.getByRole("button", { name: "Hoàn tác" }).click();
    await expect(arrows(page)).toHaveCount(7);
  });

  test("the Chưa lên lịch tray opens over the chart and Đặt ngày schedules the item", async ({
    page,
  }) => {
    await openTimeline(page);
    const wrap = page.locator('[data-tl="wrap"]');
    const before = (await wrap.boundingBox())!;
    await page.getByRole("button", { name: "Mở khay Chưa lên lịch" }).click();
    const tray = page.getByRole("region", { name: "Chưa lên lịch" });
    await expect(tray).toBeVisible();
    // The overlay does not move the chart.
    expect((await wrap.boundingBox())!.height).toBeCloseTo(before.height, 0);
    await tray
      .getByRole("button", { name: /^Đặt ngày cho/ })
      .first()
      .click();
    await tray.locator('input[type="date"]').fill("2026-10-15");
    await expect(page.getByText(/Đã đặt lịch/)).toBeVisible();
    await expect(page.getByRole("button", { name: "Hoàn tác" })).toBeVisible();
  });

  test("dragging a tray chip onto the chart tips the day and sets start and due", async ({
    page,
  }) => {
    await openTimeline(page);
    await page.getByRole("button", { name: "Mở khay Chưa lên lịch" }).click();
    const chip = page.locator("[data-tl-undated]").first();
    const body = page.locator('[data-tl="body"]');
    const box = (await body.boundingBox())!;
    await chip.dragTo(body, {
      targetPosition: { x: 26 * 10 + 5, y: Math.min(60, box.height - 4) },
    });
    await expect(page.getByText(/Đã đặt lịch/)).toBeVisible();
  });
});

test.describe("Board Timeline interactions (viewer)", () => {
  test.skip(
    persona !== "viewer" || scenario !== "default",
    "Needs the server started with VITE_MOCK_PERSONA=viewer",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  test("connectors show disabled with the reason, and the tray cannot schedule", async ({
    page,
  }) => {
    await openTimeline(page);
    const track = await trackOf(page, "ERP-146");
    await track.hover();
    const connector = track.locator('[data-tl="connector"]');
    await expect(connector).toHaveAttribute("aria-disabled", "true");
    await expect(connector).toHaveAttribute(
      "title",
      "Bạn chỉ có quyền xem board này",
    );
    await connector.click({ force: true });
    await expect(page.getByRole("dialog")).toHaveCount(0);
    await page.getByRole("button", { name: "Mở khay Chưa lên lịch" }).click();
    const tray = page.getByRole("region", { name: "Chưa lên lịch" });
    await expect(tray).toContainText("không thể đặt lịch");
    await expect(tray.getByRole("button", { name: /^Đặt ngày/ })).toHaveCount(
      0,
    );
    await expect(page.locator("[data-tl-undated]").first()).toHaveAttribute(
      "draggable",
      "false",
    );
  });
});
