import { openInfoSection } from "./support/chat-info";
import { expect, test, type Page } from "@playwright/test";
import { toNewest } from "./support/chat-newest";

/**
 * The followed-threads list and the quote reply against the Chat canvas boards `Chat-Followed-M`/`-T` and
 * `Chat-Var-Quote` (rendered with render.py, light and dark), at 1440, 1180, 1024 and 390, tolerance 1 px.
 * Board facts: a 56 high header with a 16/700 (17/650 on a phone) title and a 12 px count line, a tab row of 44 high
 * tabs (13.5/600, a 2 px underline on the open one), rows of 12 16 padding with a 36 avatar and three 20.3 high lines
 * (12 px channel, 14 px text, 12.5 px replies and time), a 20 px unread count, a 56 square empty badge with an 18/650
 * title; the quote strip in the composer (3 px bar, 8 14 padding, 12.5 px, a 28 px cancel) and the quote block of a
 * sent reply (3 px bar, 2 8 padding, 12.5 px at 0.9 opacity, 6 px below). Chat-Followed-T draws the list as the page
 * of the conversation column, so from 768 to 1279 it fills the column beside the channel list; at 1440 it stays the side
 * panel, and the row geometry is the M board's at every width. The archived tab and its Reopen button need an archived thread, which the
 * mock world does not hold: the component test covers them. Every frame was also reviewed by eye.
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
const PANEL = "[data-testid=chat-followed-panel]";

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
  await toNewest(page);
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

async function replyInThread(page: Page) {
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

async function openFollowed(page: Page) {
  // On a phone the open thread is a page: its back arrow returns to the conversation and its toolbar.
  const back = page.getByRole("button", { name: "Đóng luồng trả lời" }); // the close control: a back arrow on a phone
  if (page.viewportSize()!.width < 768 && (await back.isVisible())) {
    await back.click();
  }
  // The "…" menu deep-opens the info panel at its Following section: a thread panel open is replaced by it.
  await openInfoSection(page, "following");
}

const SIZES = [
  { width: 1440, height: 900, title: "16px", weight: "700" },
  { width: 1180, height: 820, title: "16px", weight: "700" },
  { width: 1024, height: 768, title: "16px", weight: "700" },
  { width: 390, height: 844, title: "17px", weight: "650" },
] as const;

for (const mode of ["light", "dark"] as const) {
  for (const size of SIZES) {
    test.describe(`${size.width} ${mode}`, () => {
      test("followed empty: header, tabs and the empty state", async ({
        page,
      }) => {
        await open(page, size.width, size.height, mode);
        await openFollowed(page);
        // The Chat-Followed page header (56 high) and the Chat-Followed-T full-column page are gone: the list is a section
        // of the info panel (Chat-Info boards), so those assertions are removed; the tabs and the empty state keep their numbers.
        const tab = `${PANEL} [role=tab]`;
        expect((await box(page, tab)).h).toBeCloseTo(44, 0);
        expect(await css(page, tab, "font-size")).toBe("13.5px");
        expect(await css(page, tab, "font-weight")).toBe("600");
        expect(await css(page, tab, "border-bottom-width")).toBe("2px");
        const badge = await box(
          page,
          `${PANEL} [data-testid=chat-followed-empty] > span`,
        );
        expect(badge.w).toBeCloseTo(56, 0);
        expect(badge.h).toBeCloseTo(56, 0);
        expect(
          await css(
            page,
            `${PANEL} [data-testid=chat-followed-empty] > span`,
            "border-radius",
          ),
        ).toBe("16px");
        const h = `${PANEL} [data-testid=chat-followed-empty] h3`;
        expect(await css(page, h, "font-size")).toBe("18px");
        expect(await css(page, h, "font-weight")).toBe("650");
        expect(
          await page.evaluate(
            () =>
              document.documentElement.scrollWidth -
              document.documentElement.clientWidth,
          ),
        ).toBeLessThanOrEqual(0);
      });

      test("followed row: 12 16 padding, a 36 avatar and three 20.3 high lines", async ({
        page,
      }) => {
        await open(page, size.width, size.height, mode);
        await replyInThread(page);
        await openFollowed(page);
        const row = `${PANEL} [data-testid=chat-followed-item]`;
        await expect(page.locator(row)).toHaveCount(1);
        expect(await css(page, row, "padding")).toBe("12px 16px");
        expect((await box(page, `${row} [data-slot=avatar]`)).w).toBeCloseTo(
          36,
          0,
        );
        expect(
          await css(page, `${row} [data-slot=avatar] > span`, "font-size"),
        ).toBe("13px");
        const lines = `${row} button > span`;
        expect(await css(page, lines, "font-size")).toBe("12px");
        expect(await css(page, lines, "line-height")).toBe("20.3px");
        const text = `${row} button > span:nth-child(2)`;
        expect(await css(page, text, "font-size")).toBe("14px");
        const meta = `${row} button > span:nth-child(3)`;
        expect(await css(page, meta, "font-size")).toBe("12.5px");
        // The row is 24 of padding around the title lines and the two others, each 20.3 high.
        const titleLines = Math.round((await box(page, text)).h / 20.3);
        expect((await box(page, row)).h).toBeCloseTo(
          24 + 20.3 * (2 + titleLines),
          0,
        );
        // The tab underline sits on the tab row's rule.
        const tab = await box(page, `${PANEL} [role=tab][aria-selected=true]`);
        expect(tab.h).toBeCloseTo(44, 0);
        expect(
          await page.evaluate(
            () =>
              document.documentElement.scrollWidth -
              document.documentElement.clientWidth,
          ),
        ).toBeLessThanOrEqual(0);
      });

      test("quote reply: the strip in the composer and the block of the sent reply", async ({
        page,
      }) => {
        await open(page, size.width, size.height, mode);
        const row = page.locator("[data-own=false]").last();
        await row.scrollIntoViewIfNeeded();
        await row.hover();
        await row.getByRole("button", { name: "Trả lời", exact: true }).click();
        const strip = "[data-testid=chat-reply-chip]";
        await expect(page.locator(strip)).toBeVisible();
        const s = await box(page, strip);
        // 8 + 8 of padding around the two 18.1 high lines, and the 1 px rule under them.
        expect(s.h).toBeCloseTo(8 + 8 + 2 * 18.125 + 1, 0);
        expect(await css(page, strip, "padding")).toBe("8px 14px");
        expect(await css(page, strip, "font-size")).toBe("12.5px");
        expect(await css(page, strip, "border-radius")).toBe(
          "26px 26px 0px 0px",
        );
        expect(await css(page, `${strip} > span:first-child`, "width")).toBe(
          "3px",
        );
        const cancel = await box(page, `${strip} button`);
        expect(cancel.w).toBeCloseTo(28, 0);
        expect(cancel.h).toBeCloseTo(28, 0);
        expect(await css(page, `${strip} b`, "font-weight")).toBe("700");
        await page
          .getByRole("combobox")
          .last()
          .fill("Đã chốt, anh ký giúp chị.");
        await page.getByRole("combobox").last().press("Enter");
        const quote = page.locator("[data-bubble] [data-testid=chat-quote]");
        await expect(quote.last()).toBeVisible();
        const q = "[data-bubble] [data-testid=chat-quote]";
        expect(await css(page, q, "padding")).toBe("2px 8px");
        expect(await css(page, q, "font-size")).toBe("12.5px");
        expect(await css(page, q, "border-inline-start-width")).toBe("3px");
        expect(await css(page, q, "margin-bottom")).toBe("6px");
        expect(await css(page, q, "opacity")).toBe("0.9");
        expect(await css(page, `${q} b`, "font-weight")).toBe("700");
      });
    });
  }
}
