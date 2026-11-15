import { expect, test, type Locator, type Page } from "@playwright/test";
import { vi as copy } from "../../packages/features/chat/src/web/components/chat.copy";

/**
 * The Chat pin and thread surfaces against the owner canvas, light and dark, tolerance 1 px, in Vietnamese:
 * the unpin confirmation (`Chat-Pins` 1440 dialog 440, `Chat-Pins-T` 1180 dialog 400, `Chat-Pins-M` 390 sheet), the pin row
 * menu (`Chat-Pins` popup 220 with 40 rows, `Chat-Pins-M` sheet), the thread "more" menu (`Chat-Thread-T` popup 220 with 36
 * rows, `Chat-Thread-M` sheet) and the pin-limit banner (`Chat-Pins` 1440, 44 high). Numbers are measured on the boards
 * rendered with the reference tool; positions are relative to the surface. At 1024 no board is drawn, so the surface must stay
 * inside the viewport and the page must not scroll sideways. Set CHAT_STATE_SHOTS to a directory for one screenshot per state.
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
const SIZES = [
  { width: 1440, height: 900 },
  { width: 1180, height: 820 },
  { width: 1024, height: 768 },
  { width: 390, height: 844 },
] as const;
type Size = (typeof SIZES)[number];
const phone = (size: Size) => size.width < 768;

async function open(
  page: Page,
  size: Size,
  theme: "light" | "dark",
  pinLimit?: number,
) {
  await page.setViewportSize(size);
  await page.addInitScript(
    ([mode, limit]) => {
      try {
        window.localStorage.setItem("notrelix-locale", "vi");
        window.localStorage.setItem("notrelix-theme-mode", mode as string);
      } catch {
        // Private mode: the defaults apply.
      }
      if (limit)
        (globalThis as Record<string, unknown>).__mockChatPinLimit = limit;
    },
    [theme, pinLimit] as const,
  );
  await page.goto(CHANNEL);
  await expect(page.getByRole("log")).toBeVisible({ timeout: 20_000 });
  await page.waitForTimeout(400);
}

/** Opens the More menu of the nth message from the end (the other person's messages) and picks an entry. */
async function pickFromRow(page: Page, index: number, item: string) {
  const row = page.locator("[data-own=false]").nth(-1 - index * 2);
  await row.scrollIntoViewIfNeeded();
  await row.hover();
  const more = row.getByRole("button", { name: copy.toolbar.moreActions });
  if (await more.count()) await more.first().click({ force: true });
  else await row.getByTestId("chat-message-more-touch").click();
  await page.getByRole("menuitem", { name: item }).click();
  await page.waitForTimeout(500);
}
const pinNthMessage = (page: Page, index: number) =>
  pickFromRow(page, index, copy.toolbar.pin);

async function openPinsPanel(page: Page) {
  await page
    .locator("main header")
    .getByRole("button", { name: new RegExp(`^${copy.pins.tab}$`) })
    .click();
  await expect(page.getByTestId("chat-pins-panel")).toBeVisible();
}

/** The box of an element relative to a surface. */
async function rel(surface: Locator, inner: Locator) {
  const s = (await surface.boundingBox())!;
  const b = (await inner.first().boundingBox())!;
  return { x: b.x - s.x, y: b.y - s.y, w: b.width, h: b.height };
}

const close = (a: number, b: number, tolerance = 1) =>
  expect(Math.abs(a - b), `${a} vs ${b}`).toBeLessThanOrEqual(tolerance);

async function settle(page: Page, name: string, surface: Locator) {
  const overflow = await page.evaluate(
    () =>
      document.documentElement.scrollWidth -
      document.documentElement.clientWidth,
  );
  expect(overflow).toBeLessThanOrEqual(0);
  const v = page.viewportSize()!;
  const b = (await surface.boundingBox())!;
  expect(b.x).toBeGreaterThanOrEqual(-0.5);
  expect(b.x + b.width).toBeLessThanOrEqual(v.width + 0.5);
  expect(b.y + b.height).toBeLessThanOrEqual(v.height + 0.5);
  if (SHOTS) {
    await page.waitForTimeout(500);
    await page.screenshot({ path: `${SHOTS}/${name}.png` });
  }
}

/** The luminance of a CSS colour, to tell a light surface from a dark one. */
async function luminance(locator: Locator, prop: string) {
  return locator.first().evaluate((el, p) => {
    const c = document.createElement("canvas").getContext("2d")!;
    c.fillStyle = "#000";
    c.fillStyle = getComputedStyle(el).getPropertyValue(p);
    c.fillRect(0, 0, 1, 1);
    const [r, g, b] = c.getImageData(0, 0, 1, 1).data;
    return (0.2126 * r! + 0.7152 * g! + 0.0722 * b!) / 255;
  }, prop);
}

for (const theme of ["light", "dark"] as const) {
  for (const size of SIZES) {
    const tag = `${size.width}-${theme}`;

    test.describe(tag, () => {
      test("the unpin confirmation matches the board", async ({ page }) => {
        await open(page, size, theme);
        await pinNthMessage(page, 0);
        await openPinsPanel(page);
        await page.getByTestId("chat-pin-more").first().click();
        if (phone(size)) {
          await page
            .getByTestId("chat-pin-sheet")
            .getByRole("button", { name: copy.pins.unpinAction })
            .click();
          const sheet = page.getByTestId("chat-unpin-sheet");
          await expect(sheet).toBeVisible();
          await page.waitForTimeout(500);
          const box = (await sheet.boundingBox())!;
          const v = page.viewportSize()!;
          // Board: 390 wide at the bottom edge, 236.7 high, two stacked 46 buttons 346 wide.
          close(box.width, 390);
          close(box.height, 236.7, 1.5);
          close(box.y + box.height, v.height);
          const confirm = await rel(
            sheet,
            sheet.getByRole("button", { name: copy.pins.unpinAction }),
          );
          const cancel = await rel(
            sheet,
            sheet.getByRole("button", { name: copy.list.cancel }),
          );
          close(confirm.w, 346);
          close(confirm.h, 46);
          close(confirm.x, 22);
          close(confirm.y, 108.7, 1.5);
          close(cancel.y, 162.7, 1.5);
          close(cancel.h, 46);
          await expect(
            sheet.getByText(copy.pins.unpinBody, { exact: true }),
          ).toBeVisible();
          await settle(page, `unpin-${tag}`, sheet);
          return;
        }
        await page
          .getByRole("menuitem", { name: copy.pins.unpinAction })
          .click();
        const dialog = page.getByTestId("chat-unpin-dialog");
        await expect(dialog).toBeVisible();
        await page.waitForTimeout(400);
        const box = (await dialog.boundingBox())!;
        const v = page.viewportSize()!;
        // Board: 440 wide from 1280 px (with the announcement line), 400 below; 204 high; centred.
        const wide = size.width >= 1280;
        close(box.width, wide ? 440 : 400);
        if (size.width !== 1024) close(box.height, 204, 1.5);
        close(box.x + box.width / 2, v.width / 2);
        close(box.y + box.height / 2, v.height / 2);
        const style = await dialog.evaluate((el) => {
          const cs = getComputedStyle(el);
          return { radius: cs.borderTopLeftRadius };
        });
        expect(style.radius).toBe("16px");
        const badge = await rel(dialog, dialog.locator("span.size-11"));
        close(badge.w, 44);
        close(badge.h, 44);
        close(badge.x, 21);
        close(badge.y, 84, 1.5);
        const cancel = await rel(
          dialog,
          dialog.getByRole("button", { name: copy.list.cancel }),
        );
        const confirm = await rel(
          dialog,
          dialog.getByRole("button", { name: copy.pins.unpinAction }),
        );
        close(cancel.h, 34);
        close(confirm.h, 34);
        close(confirm.x + confirm.w, box.width - 21);
        close(cancel.x + cancel.w + 8, confirm.x, 1.5);
        const title = await rel(
          dialog,
          dialog.getByRole("heading", { name: copy.pins.unpinTitle }),
        );
        close(title.x, 21);
        const fs = await dialog
          .getByRole("heading", { name: copy.pins.unpinTitle })
          .evaluate((el) => getComputedStyle(el).fontSize);
        expect(fs).toBe("17px");
        const bg = await luminance(dialog, "background-color");
        if (theme === "dark") expect(bg).toBeLessThan(0.3);
        else expect(bg).toBeGreaterThan(0.8);
        await settle(page, `unpin-${tag}`, dialog);
      });

      test("the pin row menu matches the board", async ({ page }) => {
        await open(page, size, theme);
        await pinNthMessage(page, 0);
        await openPinsPanel(page);
        await page.getByTestId("chat-pin-more").first().click();
        if (phone(size)) {
          const sheet = page.getByTestId("chat-pin-sheet");
          await expect(sheet).toBeVisible();
          await page.waitForTimeout(500);
          const box = (await sheet.boundingBox())!;
          // Board: 390 x 231, three 48 rows whose labels sit 2 px below the row centre.
          close(box.width, 390);
          close(box.height, 231, 1.5);
          const rows = sheet.getByRole("button", { name: /./ });
          const names = [
            copy.pins.goToMessage,
            copy.pins.copyLink,
            copy.pins.unpinAction,
          ];
          for (const [i, name] of names.entries()) {
            const row = await rel(
              sheet,
              sheet.getByRole("button", { name, exact: true }),
            );
            close(row.h, 48);
            close(row.y, 67 + i * 48, 1.5);
          }
          expect(await rows.count()).toBeGreaterThanOrEqual(4);
          await settle(page, `pin-menu-${tag}`, sheet);
          return;
        }
        const menu = page.getByRole("menu");
        await expect(menu).toBeVisible();
        await page.waitForTimeout(300);
        const box = (await menu.boundingBox())!;
        // Board: 220 wide, 139 high (three 40 rows, a rule, 4 padding), radius 12.
        close(box.width, 220);
        close(box.height, 139, 1.5);
        expect(
          await menu.evaluate((el) => getComputedStyle(el).borderTopLeftRadius),
        ).toBe("12px");
        const items = menu.getByRole("menuitem");
        expect(await items.count()).toBe(3);
        for (const [i, y] of [5, 45, 94].entries()) {
          const item = await rel(menu, items.nth(i));
          close(item.h, 40);
          close(item.y, y, 1.5);
          close(item.x, 5, 1.5);
        }
        const icon = await rel(menu, items.nth(0).locator("svg"));
        close(icon.x, 19, 1.5);
        close(icon.w, 14);
        await expect(items.nth(2)).toHaveAttribute(
          "data-variant",
          "destructive",
        );
        await settle(page, `pin-menu-${tag}`, menu);
      });

      test("the thread more menu matches the board", async ({ page }) => {
        await open(page, size, theme);
        await pickFromRow(page, 2, copy.toolbar.replyInThread);
        await expect(page.getByTestId("chat-thread-panel")).toBeVisible();
        // A thread exists from its first reply on: reply, then follow.
        const reply = page
          .getByTestId("chat-thread-panel")
          .getByRole("combobox");
        await reply.fill("Phản hồi đầu tiên trong luồng");
        await reply.press("Enter");
        await expect(page.getByTestId("chat-thread-separator")).toBeVisible();
        // Follow first, so the mute entry takes the place of the archive row the board draws.
        const state = page.getByTestId("chat-thread-follow-state");
        if ((await state.textContent()) !== copy.thread.following) {
          await page.getByTestId("chat-thread-more").click();
          await page.getByText(copy.thread.followThread).click();
        }
        await expect(state).toHaveText(copy.thread.following);
        // The first menu or sheet is still closing: wait, or the second opening is swallowed.
        await page.waitForTimeout(600);
        // Board: the status chip is 22 high and fully rounded.
        close((await state.boundingBox())!.height, 22);
        expect(
          parseFloat(
            await state.evaluate(
              (el) => getComputedStyle(el).borderTopLeftRadius,
            ),
          ),
        ).toBeGreaterThanOrEqual(11);
        await page.getByTestId("chat-thread-more").click();
        if (phone(size)) {
          const sheet = page.getByTestId("chat-thread-sheet");
          await expect(sheet).toBeVisible();
          await page.waitForTimeout(500);
          const box = (await sheet.boundingBox())!;
          close(box.width, 390);
          close(box.height, 231, 1.5);
          const rows = sheet.getByRole("button");
          for (const [i, name] of [
            copy.thread.unfollowThread,
            copy.thread.mute,
            copy.thread.copyLink,
          ].entries()) {
            const r = await rel(
              sheet,
              sheet.getByRole("button", { name, exact: true }),
            );
            close(r.h, 48);
            close(r.y, 67 + i * 48, 1.5);
          }
          expect(await rows.count()).toBeGreaterThanOrEqual(4);
          await settle(page, `thread-menu-${tag}`, sheet);
          return;
        }
        const menu = page.getByRole("menu");
        await expect(menu).toBeVisible();
        await page.waitForTimeout(300);
        const box = (await menu.boundingBox())!;
        // Board: 220 wide, 122 high (three 36 rows, 6 padding), radius 12, 13.5 labels.
        close(box.width, 220);
        close(box.height, 122, 1.5);
        expect(
          await menu.evaluate((el) => getComputedStyle(el).borderTopLeftRadius),
        ).toBe("12px");
        const items = menu.getByRole("menuitem");
        expect(await items.count()).toBe(3);
        for (let i = 0; i < 3; i++) {
          const item = await rel(menu, items.nth(i));
          close(item.h, 36);
          close(item.y, 7 + i * 36, 1.5);
          const icon = await rel(menu, items.nth(i).locator("svg"));
          close(icon.x, 21, 1.5);
          close(icon.w, 14);
        }
        expect(
          await items.nth(0).evaluate((el) => getComputedStyle(el).fontSize),
        ).toBe("13.5px");
        await settle(page, `thread-menu-${tag}`, menu);
      });

      test("the pin limit banner matches the board", async ({ page }) => {
        test.skip(phone(size), "Chat-Pins draws the banner at 1440 only");
        await open(page, size, theme, 1);
        await pinNthMessage(page, 0);
        await pinNthMessage(page, 1);
        const banner = page.getByTestId("chat-pin-limit-banner");
        await expect(banner).toBeVisible();
        await expect(banner).toContainText(copy.pins.limit);
        const box = (await banner.boundingBox())!;
        // Board: 44 high (8 padding, a 28 button), 16 gutters, a 14 icon, 13/600 text on the danger tint.
        // At 1024 no board is drawn and the text may wrap to a second line.
        if (size.width >= 1180) close(box.height, 44);
        const button = await rel(
          banner,
          banner.getByRole("button", { name: copy.pins.limitAction }),
        );
        close(button.h, 28);
        close(box.width - (button.x + button.w), 16);
        const icon = await rel(banner, banner.locator("svg").first());
        close(icon.x, 16);
        close(icon.w, 14);
        const text = banner.locator("span").first();
        expect(await text.evaluate((el) => getComputedStyle(el).fontSize)).toBe(
          "13px",
        );
        expect(
          await text.evaluate((el) => getComputedStyle(el).fontWeight),
        ).toBe("600");
        const bg = await luminance(banner, "background-color");
        if (theme === "dark") expect(bg).toBeLessThan(0.3);
        else expect(bg).toBeGreaterThan(0.8);
        await banner
          .getByRole("button", { name: copy.pins.limitAction })
          .click();
        await expect(page.getByTestId("chat-pins-panel")).toBeVisible();
        await settle(page, `pin-limit-${tag}`, banner);
      });
    });
  }
}
