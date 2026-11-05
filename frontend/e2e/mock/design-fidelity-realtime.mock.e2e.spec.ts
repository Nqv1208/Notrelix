import { expect, test, type Page } from "@playwright/test";

/**
 * The live-connection status of the board in the design canvas artboards BC-Realtime-1440 and
 * BC-Realtime-768-320: quiet while connected, the "Đang kết nối lại…" pill, and the lost-connection notice
 * with Reconnect. Numbers were read from the artboards rendered with docs/design/mockups/2026-10/render.py;
 * tolerance is 1 px. The mock transport is driven through `globalThis.__notrelixMockRealtime`.
 *
 * Not measured: the board-updated, item-deleted, lowered-role and lost-access frames; the first is not built
 * (nothing shows while connected), the others change the board's content and are covered by the component
 * and integration tests.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const NOW = new Date("2026-10-12T09:41:00+07:00");

test.beforeEach(() => {
  test.skip(
    scenario !== "default" || persona !== "owner" || faults !== "",
    "Assertions need the default owner world without faults",
  );
});

async function open(page: Page, width: number, dark: boolean) {
  await page.setViewportSize({ width, height: 900 });
  await page.clock.install({ time: NOW });
  await page.addInitScript(
    (mode) => {
      try {
        window.localStorage.setItem("notrelix-locale", "vi");
        window.localStorage.setItem("notrelix-theme-mode", mode);
      } catch {
        // Private mode: the defaults apply.
      }
    },
    dark ? "dark" : "light",
  );
  await page.goto(BOARD, { waitUntil: "domcontentloaded" });
  await expect(page.getByRole("heading", { level: 1 }).first()).toBeVisible();
}

const drive = (page: Page, state: string) =>
  page.evaluate((next) => {
    const transport = (
      globalThis as unknown as {
        __notrelixMockRealtime?: { simulateState(s: string): void };
      }
    ).__notrelixMockRealtime;
    if (!transport) throw new Error("no mock realtime transport");
    transport.simulateState(next);
  }, state);

function near(actual: number, want: number, label: string) {
  expect
    .soft(Math.abs(actual - want), `${label}: ${actual.toFixed(1)} vs ${want}`)
    .toBeLessThanOrEqual(1);
}

for (const dark of [false, true]) {
  const mode = dark ? "dark" : "light";

  test(`quiet while connected, 1440 px, ${mode}`, async ({ page }) => {
    await open(page, 1440, dark);
    await drive(page, "connected");
    await expect(
      page.getByRole("status").filter({ hasText: /kết nối/i }),
    ).toHaveCount(0);
  });

  test(`reconnecting pill, 1440 px, ${mode}`, async ({ page }) => {
    await open(page, 1440, dark);
    await drive(page, "reconnecting");
    const pill = page
      .getByRole("status")
      .filter({ hasText: "Đang kết nối lại…" });
    await expect(pill).toBeVisible();
    const b = (await pill.boundingBox())!;
    // BC-Realtime-1440 "Đang kết nối lại": 150.1 x 28, 13 px text, its right edge on the header's right edge.
    near(b.width, 150.1, "width");
    near(b.height, 28, "height");
    expect(await pill.evaluate((e) => getComputedStyle(e).fontSize)).toBe(
      "13px",
    );
    const share = (await page
      .getByRole("button", { name: "Chia sẻ" })
      .first()
      .boundingBox())!;
    expect.soft(b.x + b.width).toBeGreaterThan(share.x);
  });

  test(`connection lost notice, 1440 px, ${mode}`, async ({ page }) => {
    await open(page, 1440, dark);
    await drive(page, "reconnecting");
    await page.clock.runFor(11_000);
    const notice = page
      .getByRole("status")
      .filter({ hasText: "Mất kết nối từ" });
    await expect(notice).toBeVisible();
    const n = (await notice.boundingBox())!;
    const title = (await notice.locator("b").boundingBox())!;
    const body = (await notice.getByText(/^Bạn vẫn đọc được/).boundingBox())!;
    const reconnect = (await notice
      .getByRole("button", { name: "Kết nối lại" })
      .boundingBox())!;
    // BC-Realtime-1440 "Mất kết nối": 64 high, padding 10 14, gap 10, 13 px lines 18.2 apart by 1, button 93.7 x 44 flush right.
    near(n.height, 64, "height");
    near(title.x - n.x, 40, "text x");
    near(title.y - n.y, 10, "title y");
    near(title.height, 18.2, "title height");
    near(body.y - (title.y + title.height), 1, "line gap");
    near(reconnect.height, 44, "button height");
    near(reconnect.width, 93.7, "button width");
    near(n.x + n.width - (reconnect.x + reconnect.width), 14, "right padding");
    near(reconnect.y - n.y, 10, "button y");
    expect(
      await notice.evaluate((e) => getComputedStyle(e).borderTopLeftRadius),
    ).toBe("10px");
  });

  test(`reconnected hides the status again, ${mode}`, async ({ page }) => {
    await open(page, 1440, dark);
    await drive(page, "reconnecting");
    await page.clock.runFor(11_000);
    await expect(
      page.getByRole("status").filter({ hasText: "Mất kết nối từ" }),
    ).toBeVisible();
    await drive(page, "connected");
    await expect(
      page.getByRole("status").filter({ hasText: /kết nối/i }),
    ).toHaveCount(0);
  });

  test(`reconnecting pill, 320 px, ${mode}`, async ({ page }) => {
    await open(page, 320, dark);
    await drive(page, "reconnecting");
    const pill = page
      .getByRole("status")
      .filter({ hasText: "Đang kết nối lại…" });
    await expect(pill).toBeVisible();
    const b = (await pill.boundingBox())!;
    // BC-Realtime-768-320 at 320: the pill is a 44 px target, 150.1 wide, at the left page margin (13 in the frame, whose border is 1 px).
    near(b.width, 150.1, "width");
    near(b.height, 44, "height");
    near(b.x, 12, "x");
  });

  test(`connection lost notice, 320 px, ${mode}`, async ({ page }) => {
    await open(page, 320, dark);
    await drive(page, "reconnecting");
    await page.clock.runFor(11_000);
    const notice = page
      .getByRole("status")
      .filter({ hasText: "Mất kết nối từ" });
    await expect(notice).toBeVisible();
    const n = (await notice.boundingBox())!;
    const title = (await notice.locator("b").boundingBox())!;
    const reconnect = (await notice
      .getByRole("button", { name: "Kết nối lại" })
      .boundingBox())!;
    // BC-Realtime-768-320 at 320: the frame's 294 x 148 at x 13 is 296 x 148 at x 12 in a 320 px viewport, text 242 wide at +40, the button the full 268 width, 44 high, 10 below the text.
    near(n.x, 12, "x");
    near(n.width, 296, "width");
    near(n.height, 148, "height");
    near(title.x - n.x, 40, "text x");
    near(title.width, 242, "text width");
    near(reconnect.width, 268, "button width");
    near(reconnect.height, 44, "button height");
    near(reconnect.x - n.x, 14, "button x");
  });
}
