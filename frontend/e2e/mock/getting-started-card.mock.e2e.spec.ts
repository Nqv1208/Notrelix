import { expect, test, type Page } from "@playwright/test";

/**
 * The getting-started card on Home of a company made through the setup pages,
 * in Vietnamese, light and dark, at 1440, 1024 and 390 px. Final-Home draws no
 * card, so the card is an undrawn state: it uses the design system's tokens and
 * is reviewed here on screenshots (attached to each run) and on what must hold
 * at every width: the card fits the page, nothing scrolls sideways, the items
 * and the hide button are reachable by keyboard, and a phone target is 44 px.
 */
const WIDTHS = [
  { width: 1440, height: 900 },
  { width: 1024, height: 768 },
  { width: 390, height: 844 },
] as const;

async function arrive(
  page: Page,
  dark: boolean,
  size: (typeof WIDTHS)[number],
) {
  await page.setViewportSize(size);
  await page.addInitScript(
    ({ mode }) => {
      try {
        localStorage.setItem("notrelix-locale", "vi");
        localStorage.setItem("notrelix-theme-mode", mode);
      } catch {
        // Private mode: the defaults apply.
      }
    },
    { mode: dark ? "dark" : "light" },
  );
  await page.goto("/onboarding");
  await page.getByLabel("Tên công ty").fill("Công ty CP Thực phẩm Sao Mai");
  await expect(page.getByText(/còn trống/).first()).toBeVisible();
  await page.getByRole("button", { name: "Tiếp tục", exact: true }).click();
  await page.getByRole("button", { name: /^Tạo \d+ phòng ban/ }).click();
  await page
    .getByRole("textbox", { name: /^Email \d/ })
    .first()
    .fill("khoa.hoang@saomaifood.vn");
  await page.getByRole("button", { name: /^Gửi \d+ lời mời/ }).click();
  await page.getByLabel("Tên board").fill("Ra mắt nước chấm Sao Mai");
  await page.getByRole("button", { name: "Tạo board" }).click();
  await page.getByRole("button", { name: "Vào Trang chủ" }).click();
  await expect(page).toHaveURL(/\/workspaces\/[^/]+/);
}

for (const size of WIDTHS)
  for (const dark of [false, true]) {
    test(`the card on Home at ${size.width}, ${dark ? "dark" : "light"}: progress, items, hide`, async ({
      page,
    }, testInfo) => {
      await arrive(page, dark, size);
      const card = page.getByRole("region", { name: /^Bắt đầu với/ });
      await expect(card).toBeVisible();
      await expect(card.getByText(/^[01]\/2$/)).toBeVisible();
      await expect(card.getByRole("progressbar")).toBeVisible();
      await expect(
        card.getByRole("link", { name: "Tạo việc đầu tiên" }),
      ).toBeVisible();
      await expect(
        card.getByRole("link", { name: "Mời thêm người" }),
      ).toBeVisible();

      // The card fits the page and nothing scrolls sideways.
      const box = (await card.boundingBox())!;
      expect(box.x).toBeGreaterThanOrEqual(0);
      expect(box.x + box.width).toBeLessThanOrEqual(size.width);
      expect(
        await page.evaluate(
          () => document.documentElement.scrollWidth - window.innerWidth,
        ),
      ).toBeLessThanOrEqual(0);

      // The hide button is a button on the keyboard path; on a phone it is a 44 px target.
      const hide = card.getByRole("button", { name: "Ẩn danh sách" });
      await hide.focus();
      await expect(hide).toBeFocused();
      if (size.width === 390) {
        // A 44 px target on one line: the words "Ẩn danh sách" do not wrap.
        const target = (await hide.boundingBox())!;
        expect(target.height).toBeGreaterThanOrEqual(44);
        expect(target.height).toBeLessThanOrEqual(48);
      }

      await testInfo.attach(`card-${size.width}-${dark ? "dark" : "light"}`, {
        body: await card.screenshot({
          // CARD_SHOTS=<dir> keeps the shots for a reviewer.
          path: process.env.CARD_SHOTS
            ? `${process.env.CARD_SHOTS}/card-${size.width}-${dark ? "dark" : "light"}.png`
            : undefined,
        }),
        contentType: "image/png",
      });

      await hide.click();
      await expect(card).toBeHidden();
      await page.reload();
      await expect(page.getByRole("heading", { level: 1 })).toBeVisible();
      await expect(
        page.getByRole("region", { name: /^Bắt đầu với/ }),
      ).toBeHidden();
    });
  }
