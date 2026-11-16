import { expect, test, type Page } from "@playwright/test";

/**
 * The side-panel tab strip of a group conversation's details (no Chat board draws it): at the 340 px panel every label
 * stays on one line, the close cross stays in view, the page does not scroll sideways and the arrows move between tabs.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
test.skip(
  persona !== "owner" || scenario !== "default",
  "Needs the default owner world",
);

const CHANNEL =
  "/workspaces/mock-workspace-primary/chat/c1000000-0000-4000-8000-000000000001";
const PANEL = "[data-testid=chat-side-panel]";

/** Starts a group, opens its details and leaves the panel 340 px wide (viewport 1180). */
async function openGroupDetails(
  page: Page,
  width = 1180,
  mode: "light" | "dark" = "light",
) {
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.addInitScript((m) => {
    try {
      window.localStorage.setItem("notrelix-locale", "vi");
      window.localStorage.setItem("notrelix-theme-mode", m);
    } catch {
      // Private mode: the defaults apply.
    }
  }, mode);
  await page.goto(CHANNEL);
  await expect(page.locator("[data-bubble]").first()).toBeVisible();
  await page.getByRole("button", { name: "Tin nhắn mới" }).click();
  const dialog = page.getByRole("dialog");
  const boxes = dialog.getByRole("checkbox");
  await expect(boxes.nth(1)).toBeVisible();
  await boxes.nth(0).click();
  await boxes.nth(1).click();
  await dialog.getByRole("button", { name: "Tạo nhóm" }).click();
  await page.getByRole("button", { name: "Thông tin cuộc trò chuyện" }).click();
  await page.setViewportSize({ width, height: 900 });
  await expect(page.locator(PANEL)).toBeVisible();
  await page.waitForTimeout(400);
}

test("every tab label is one line and the close cross stays in view at 340 px", async ({
  page,
}) => {
  await openGroupDetails(page);
  const panel = await page.locator(PANEL).boundingBox();
  expect(panel?.width).toBe(340);
  const tabs = page.locator(PANEL).getByRole("tab");
  await expect(tabs).toHaveCount(4);
  const lines = await tabs.evaluateAll((els) =>
    els.map((el) => {
      const range = document.createRange();
      range.selectNodeContents(el);
      const tops = new Set(
        Array.from(range.getClientRects()).map((r) => Math.round(r.top)),
      );
      return { name: el.textContent, lines: tops.size };
    }),
  );
  for (const tab of lines)
    expect(tab, tab.name ?? "").toMatchObject({ lines: 1 });

  const close = page.locator(PANEL).getByRole("button", { name: "Đóng" });
  const box = await close.boundingBox();
  expect(box).not.toBeNull();
  expect(box!.x + box!.width).toBeLessThanOrEqual(panel!.x + panel!.width);
  const strip = await page.locator(PANEL).getByRole("tablist").boundingBox();
  expect(strip!.x + strip!.width).toBeLessThanOrEqual(box!.x + 0.5);
  expect(
    await page.evaluate(
      () => document.documentElement.scrollWidth <= window.innerWidth,
    ),
  ).toBe(true);
});

test("arrows, Home and End move focus between the tabs", async ({ page }) => {
  await openGroupDetails(page);
  const tabs = page.locator(PANEL).getByRole("tab");
  await tabs.first().focus();
  await page.keyboard.press("ArrowRight");
  await expect(tabs.nth(1)).toBeFocused();
  await page.keyboard.press("End");
  await expect(tabs.last()).toBeFocused();
  await page.keyboard.press("ArrowRight");
  await expect(tabs.first()).toBeFocused();
  await page.keyboard.press("ArrowLeft");
  await expect(tabs.last()).toBeFocused();
  await page.keyboard.press("Home");
  await expect(tabs.first()).toBeFocused();
});

// Set CHAT_TABS_SHOTS to a directory to write the strip at 1440, 1024 and 390, light and dark (no baseline is kept).
const SHOTS = process.env.CHAT_TABS_SHOTS;
for (const mode of ["light", "dark"] as const) {
  for (const width of [1440, 1024, 390]) {
    test(`shot ${width} ${mode}: one line per label, no sideways page scroll`, async ({
      page,
    }) => {
      test.skip(!SHOTS, "Set CHAT_TABS_SHOTS to write screenshots");
      await openGroupDetails(page, width, mode);
      expect(
        await page.evaluate(
          () => document.documentElement.scrollWidth <= window.innerWidth,
        ),
      ).toBe(true);
      await page.screenshot({ path: `${SHOTS}/tabs-${width}-${mode}.png` });
    });
  }
}
