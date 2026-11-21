import { expect, test, type Page } from "@playwright/test";

/**
 * The undrawn Chat states of mentions, channel links, reactions and files at 1440, 1024 and 390, light and dark, in
 * Vietnamese. No board draws them: they are built from the design-system components and tokens, so the spec proves
 * that each state opens, stays inside the viewport and never makes the page scroll sideways. Set CHAT_STATE_SHOTS to a
 * directory to also write one screenshot per state for the visual review (no baseline is kept).
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
test.skip(
  persona !== "owner" || scenario !== "default",
  "Needs the default owner world",
);

const CHANNEL =
  "/workspaces/mock-workspace-primary/chat/c1000000-0000-4000-8000-000000000001";
const SHOTS = process.env.CHAT_STATE_SHOTS;

const sizes = [
  { width: 1440, height: 900 },
  { width: 1024, height: 768 },
  { width: 390, height: 844 },
] as const;

async function open(page: Page, size: (typeof sizes)[number], theme: string) {
  await page.setViewportSize(size);
  await page.addInitScript((mode) => {
    try {
      window.localStorage.setItem("notrelix-locale", "vi");
      window.localStorage.setItem("notrelix-theme-mode", mode);
    } catch {
      // Private mode: the defaults apply.
    }
  }, theme);
  await page.goto(CHANNEL);
  await expect(page.getByRole("log")).toBeVisible({ timeout: 20_000 });
}

const composer = (page: Page) =>
  page.getByRole("combobox", { name: /^Tin nhắn tới #/ });

async function inside(page: Page, locator: ReturnType<Page["locator"]>) {
  const b = (await locator.first().boundingBox())!;
  const v = page.viewportSize()!;
  expect(b.x).toBeGreaterThanOrEqual(-0.5);
  expect(b.x + b.width).toBeLessThanOrEqual(v.width + 0.5);
  expect(b.y).toBeGreaterThanOrEqual(-0.5);
  expect(b.y + b.height).toBeLessThanOrEqual(v.height + 0.5);
}

async function settle(page: Page, name: string) {
  const overflow = await page.evaluate(
    () =>
      document.documentElement.scrollWidth -
      document.documentElement.clientWidth,
  );
  expect(overflow).toBeLessThanOrEqual(0);
  if (SHOTS) {
    // Capture only: the popovers fade in, and a shot taken mid-fade shows a half-drawn state.
    await page.waitForTimeout(500);
    await page.screenshot({ path: `${SHOTS}/${name}.png` });
  }
}

for (const theme of ["light", "dark"]) {
  for (const size of sizes) {
    test(`mention, link, reaction and file states at ${size.width} ${theme}`, async ({
      page,
    }) => {
      const tag = `${size.width}-${theme}`;
      await open(page, size, theme);
      const box = composer(page);

      // The @ picker: people, with the people outside the channel marked, and the broadcast entries.
      await box.click();
      await box.pressSequentially("@");
      const picker = page.getByRole("listbox", { name: "Gợi ý nhắc đến" });
      await expect(picker).toBeVisible();
      await inside(page, picker);
      await settle(page, `${tag}-mention-picker`);

      // Choosing a person outside the channel warns before the send: the first seeded channel that has such a person.
      const outsideOption = () =>
        picker
          .getByRole("option")
          .filter({ hasText: "Chưa ở trong kênh" })
          .first();
      for (const n of [5, 6, 4, 2, 3]) {
        if (await outsideOption().isVisible()) break;
        await page.goto(
          `/workspaces/mock-workspace-primary/chat/c1000000-0000-4000-8000-00000000000${n}`,
        );
        await expect(box).toBeVisible({ timeout: 20_000 });
        await box.click();
        await box.pressSequentially("@");
        await expect(picker).toBeVisible();
      }
      await expect(outsideOption()).toBeVisible();
      await outsideOption().click();
      const warning = page.getByTestId("chat-mention-warnings");
      await expect(warning).toBeVisible();
      await inside(page, warning);
      await settle(page, `${tag}-mention-warning`);

      // The # picker lists channels.
      await box.fill("");
      await box.pressSequentially("#");
      await expect(picker).toBeVisible();
      await inside(page, picker);
      await settle(page, `${tag}-channel-picker`);
      await box.fill("");

      // A file waiting to send.
      await page.getByTestId("chat-file-input").setInputFiles({
        name: "bao-cao.txt",
        mimeType: "text/plain",
        buffer: Buffer.from("noi dung"),
      });
      await expect(page.getByTestId("chat-pending-files")).toBeVisible();
      await inside(page, page.getByTestId("chat-pending-files"));
      await settle(page, `${tag}-pending-file`);
      await page
        .getByRole("button", { name: /^Bỏ bao-cao.txt/ })
        .first()
        .click();

      // The reaction picker and the who-reacted list.
      const last = page.locator("[data-own=false]").last();
      await last.scrollIntoViewIfNeeded();
      await last.hover();
      const react = last.getByRole("button", { name: "Biểu cảm" });
      if (await react.isVisible()) await react.click();
      else {
        await last.click({ button: "right" });
      }
      const palette = page.getByRole("group", { name: "Biểu cảm" });
      if (await palette.isVisible()) {
        await inside(page, palette);
        await settle(page, `${tag}-reaction-picker`);
        await palette.getByRole("button").first().click();
      }
      await page.mouse.move(2, 2);
      const who = page.getByRole("button", { name: "Ai đã bày tỏ" }).first();
      if (await who.isVisible()) {
        await who.click();
        const list = page.getByRole("list", { name: "Ai đã bày tỏ" });
        await expect(list).toBeVisible();
        await inside(page, list);
        await settle(page, `${tag}-who-reacted`);
      }
    });
  }
}
