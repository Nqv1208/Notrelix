import { expect, test, type Page } from "@playwright/test";

const WORKSPACE = "mock-workspace-primary";

/** Starts a group of the owner through the new-message dialog and sends 25 messages into it. */
async function openGroup(page: Page) {
  await page.goto(`/workspaces/${WORKSPACE}/chat`);
  await expect(page.getByRole("log")).toBeVisible();
  await page
    .getByRole("button", { name: /^(New message|Tin nhắn mới)$/ })
    .first()
    .click();
  const dialog = page.getByRole("dialog");
  const boxes = dialog.getByRole("checkbox");
  await boxes.nth(0).click();
  await boxes.nth(1).click();
  await dialog
    .getByRole("button", { name: /^(Create group|Tạo nhóm)$/ })
    .click();
  const composer = page.getByRole("combobox").last();
  for (let n = 1; n <= 25; n += 1) {
    await composer.fill(
      `Tin của tôi số ${n}, đủ dài để bong bóng rộng ra một chút`,
    );
    await composer.press("Enter");
  }
  await expect(page.locator("[data-message-id]").first()).toBeVisible();
}

for (const width of [1024, 1280, 1300]) {
  test(`a conversation's open details never clip the messages at ${width} px`, async ({
    page,
  }) => {
    await page.setViewportSize({ width, height: 800 });
    await openGroup(page);
    const log = page.getByRole("log");
    await expect(log).toBeVisible();
    await expect(page.locator("[data-message-id]").first()).toBeVisible();
    await page
      .getByRole("button", {
        name: /^(Conversation details|Thông tin cuộc trò chuyện)$/,
      })
      .first()
      .click();
    const panel = page.getByTestId("chat-side-panel");
    await expect(panel).toBeVisible();
    await page.waitForTimeout(400);

    const panelLeft = (await panel.boundingBox())!.x;
    const logBox = (await log.boundingBox())!;
    // The message column ends where the panel begins.
    expect(logBox.x + logBox.width).toBeLessThanOrEqual(panelLeft + 1);
    // No message row reaches under the panel.
    const rights = await page.evaluate(() =>
      Array.from(
        document.querySelectorAll<HTMLElement>("[data-message-id]"),
      ).map((el) => el.getBoundingClientRect().right),
    );
    expect(rights.length).toBeGreaterThan(0);
    for (const right of rights)
      expect(right).toBeLessThanOrEqual(panelLeft + 1);

    // The "latest" pill is centred on the visible message column.
    await log.evaluate((el) => {
      el.scrollTop = 0;
    });
    const pill = page.getByRole("button", {
      name: /Xem tin mới nhất|Jump to latest|latest/i,
    });
    if (await pill.isVisible()) {
      const box = (await pill.boundingBox())!;
      expect(
        Math.abs(box.x + box.width / 2 - (logBox.x + logBox.width / 2)),
      ).toBeLessThanOrEqual(3);
    }
    await page.screenshot({
      path: `test-results/chat-panel-layout-${width}.png`,
    });
  });
}
