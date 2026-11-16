import { expect, test, type Page } from "@playwright/test";

/**
 * The thread and pins side panels against the Chat canvas boards `Chat-Thread-T`/`-M` and `Chat-Pins`/`-T`/`-M`
 * (rendered with render.py, light and dark), at 1440, 1024 and 390, tolerance 1 px. Board facts: a 340 (360 from
 * 1280) wide panel with a 56 high header (the canvas set 56 at every width), a 56 high page header with a back arrow on a phone, a 24 high
 * lined reply separator, a 52 high pill reply composer, pin rows with 12 16 padding. Every frame was also reviewed by
 * eye: at 1024 the open panel left the conversation 160 px wide (one word per line, a vertical composer), so the
 * channel column is hidden below 1280 while a panel is open and the conversation keeps at least 360 px.
 * The thread archived state, the thread "more" menu and the pin unpin dialog are drawn on the boards; the menu and
 * dialog are not built (named gaps in the catalog), the archived banner is covered by a component test.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
test.skip(
  persona !== "owner" || scenario !== "default",
  "Needs the default owner world",
);

const CHANNEL =
  "/workspaces/mock-workspace-primary/chat/c1000000-0000-4000-8000-000000000001";
const HEADER = "[data-testid=chat-panel-header]";
const PANEL = "[data-testid=chat-side-panel]";

async function open(
  page: Page,
  width: number,
  height: number,
  mode: "light" | "dark",
) {
  await page.setViewportSize({ width, height });
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
  await page.waitForTimeout(600);
}

async function box(page: Page, selector: string) {
  const b = await page.locator(selector).first().boundingBox();
  if (!b) throw new Error(`no box for ${selector}`);
  return { x: b.x, y: b.y, w: b.width, h: b.height };
}
async function css(page: Page, selector: string, prop: string) {
  return page
    .locator(selector)
    .first()
    .evaluate((el, p) => getComputedStyle(el).getPropertyValue(p), prop);
}

async function pinThree(page: Page) {
  for (let i = 0; i < 3; i++) {
    const row = page.locator("[data-own=false]").nth(-1 - i * 2);
    await row.scrollIntoViewIfNeeded();
    await row.hover();
    await row
      .getByRole("button", { name: "Thao tác khác" })
      .click({ force: true });
    await page.getByRole("menuitem", { name: /Ghim tin nhắn/ }).click();
    await page.waitForTimeout(400);
  }
}

async function openThread(page: Page) {
  const row = page.locator("[data-own=false]").last();
  await row.scrollIntoViewIfNeeded();
  await row.hover();
  await row.getByRole("button", { name: "Thao tác khác" }).click();
  await page.getByRole("menuitem", { name: /Trả lời trong luồng/ }).click();
  const input = page.getByTestId("chat-thread-panel").getByRole("combobox");
  await input.fill("Phản hồi đầu tiên trong luồng");
  await input.press("Enter");
  await expect(page.getByTestId("chat-thread-separator")).toBeVisible();
}

const SIZES = [
  { width: 1440, height: 900, panel: 360, header: 56 },
  { width: 1024, height: 768, panel: 340, header: 56 },
  { width: 390, height: 844, panel: 390, header: 56 },
] as const;

for (const mode of ["light", "dark"] as const) {
  for (const size of SIZES) {
    test.describe(`${size.width} ${mode}`, () => {
      test("thread panel: header, separator, composer and room for the conversation", async ({
        page,
      }) => {
        await open(page, size.width, size.height, mode);
        await openThread(page);
        const panel = await box(page, PANEL);
        expect(panel.w).toBeCloseTo(size.panel, 0);
        expect((await box(page, HEADER)).h).toBeCloseTo(size.header, 0);
        expect(await css(page, `${HEADER} h2`, "font-size")).toBe(
          size.width === 390 ? "17px" : "16px",
        );
        const sep = "[data-testid=chat-thread-separator]";
        expect((await box(page, sep)).h).toBeCloseTo(24, 0);
        const form = "[data-testid=chat-thread-panel] form:has(textarea)";
        expect((await box(page, form)).h).toBeCloseTo(52, 0);
        expect(await css(page, form, "border-radius")).toBe("26px");
        // Nothing overflows the page sideways.
        expect(
          await page.evaluate(
            () =>
              document.documentElement.scrollWidth -
              document.documentElement.clientWidth,
          ),
        ).toBeLessThanOrEqual(0);
        if (size.width === 390) return;
        // The conversation beside the panel stays readable and its composer is a real field.
        const main = await box(page, "main");
        expect(main.w - panel.w).toBeGreaterThanOrEqual(0);
        const log = await box(page, "[data-testid=chat-message-list]");
        expect(log.w).toBeGreaterThanOrEqual(360);
        const mainForm = await box(page, "main form:has(textarea)");
        expect(mainForm.h).toBeCloseTo(52, 0);
        expect(mainForm.w).toBeGreaterThanOrEqual(300);
      });

      test("pins panel: header, summary strip, rows and room for the conversation", async ({
        page,
      }) => {
        await open(page, size.width, size.height, mode);
        await pinThree(page);
        await page.getByRole("button", { name: "Ghim" }).first().click();
        const pin = "[data-testid=chat-pin-item]";
        await expect(page.locator(pin)).toHaveCount(3);
        expect((await box(page, PANEL)).w).toBeCloseTo(size.panel, 0);
        expect((await box(page, HEADER)).h).toBeCloseTo(size.header, 0);
        expect(await css(page, pin, "padding")).toBe("12px 16px");
        expect((await box(page, `${pin} [data-slot=avatar]`)).w).toBeCloseTo(
          18,
          0,
        );
        expect(
          await page.evaluate(
            () =>
              document.documentElement.scrollWidth -
              document.documentElement.clientWidth,
          ),
        ).toBeLessThanOrEqual(0);
        if (size.width === 390) return;
        const log = await box(page, "[data-testid=chat-message-list]");
        expect(log.w).toBeGreaterThanOrEqual(360);
        expect(
          (await box(page, "main form:has(textarea)")).w,
        ).toBeGreaterThanOrEqual(300);
      });
    });
  }
}
