import { expect, test, type Page } from "@playwright/test";

/**
 * The Priority options of the field panel against the owner canvas board `Final-Settings` state `truong`
 * (rendered with `render.py`, 1440 px): four options in the drawn order, each a 36 px row (the first; 37 px with its
 * line for the others) with a 14 px swatch of radius 4 in the option's colour, the name at 13 px and the count of
 * items at 12 px. Colours are the artboard's: Khẩn #C42340, Cao #B4500B, Trung bình #1E7BE0, Thấp #7A8391. Light
 * and dark, tolerance 1 px. The counts differ from the artboard's (the mock board has its own items), so the text
 * is compared on its words' form ("N việc"), not its numbers.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const NOW = new Date("2026-10-12T10:40:00+07:00");
const TOL = 1;
const PRIORITY = [
  ["Khẩn", "rgb(196, 35, 64)"],
  ["Cao", "rgb(180, 80, 11)"],
  ["Trung bình", "rgb(30, 123, 224)"],
  ["Thấp", "rgb(122, 131, 145)"],
] as const;

async function open(page: Page, dark: boolean) {
  await page.clock.setFixedTime(NOW);
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
  await page.goto(`${BOARD}/settings?from=table&section=fields`);
  await page.getByText("Ưu tiên", { exact: true }).first().click();
  await expect(
    page.getByRole("complementary", { name: /^Sửa trường/ }),
  ).toBeVisible();
  await page.waitForTimeout(400);
}

test.describe("Priority options (Final-Settings truong), 1440 px", () => {
  test.skip(
    persona !== "owner" || scenario !== "default" || faults !== "",
    "Needs the default owner world without faults",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  for (const dark of [false, true]) {
    test(`names, colours, row heights and count text (${dark ? "dem" : "sang"})`, async ({
      page,
    }) => {
      await open(page, dark);
      const panel = page.getByRole("complementary", { name: /^Sửa trường/ });
      let previousBottom: number | null = null;
      for (const [index, [name, colour]] of PRIORITY.entries()) {
        const trigger = panel.getByRole("button", {
          name: `Màu của lựa chọn ${name}`,
        });
        await expect(trigger, name).toBeVisible();
        const swatch = trigger.locator("span").first();
        const style = await swatch.evaluate((element) => {
          const s = getComputedStyle(element);
          const r = element.getBoundingClientRect();
          return {
            bg: s.backgroundColor,
            r: s.borderRadius,
            w: r.width,
            h: r.height,
          };
        });
        expect(style.bg, `${name} colour`).toBe(colour);
        expect(style.r, `${name} radius`).toBe("4px");
        expect(Math.abs(style.w - 14), `${name} swatch`).toBeLessThanOrEqual(
          TOL,
        );
        expect(Math.abs(style.h - 14), `${name} swatch`).toBeLessThanOrEqual(
          TOL,
        );

        const row = trigger.locator(
          "xpath=ancestor::*[contains(@class,'group')][1]",
        );
        const box = (await row.boundingBox())!;
        expect(
          Math.abs(box.height - (index === 0 ? 36 : 37)),
          `${name} row height`,
        ).toBeLessThanOrEqual(TOL);
        if (previousBottom !== null)
          expect(
            Math.abs(box.y - previousBottom),
            `${name} follows`,
          ).toBeLessThanOrEqual(TOL);
        previousBottom = box.y + box.height;

        const input = row.getByRole("textbox", { name: "Tên lựa chọn" });
        await expect(input).toHaveValue(name);
        expect(await input.evaluate((e) => getComputedStyle(e).fontSize)).toBe(
          "13px",
        );
        const count = row.getByText(/^\d+ việc$|^Chưa dùng$/);
        await expect(count, `${name} count`).toBeVisible();
        expect(await count.evaluate((e) => getComputedStyle(e).fontSize)).toBe(
          "12px",
        );
      }
    });
  }
});
