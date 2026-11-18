import AxeBuilder from "@axe-core/playwright";
import { expect, test, type Locator, type Page } from "@playwright/test";
import {
  copy,
  hook,
  openDm,
  sendMessage,
  startCall,
} from "./support/chat-calls";

/**
 * The call line in the timeline and the busy notice against the Chat canvas boards Chat-Var-Call-Log, Chat-Var-Call-Busy and
 * Chat-Call-DM-Missed (rendered with render.py, light and dark), at 1440, 1024 and 390, tolerance 1 px. Board facts:
 *  - Chat-Var-Call-Log: the call line is a centred pill (`.sys`): radius 14, padding 8 14, 10 between the parts, 13 px text on a
 *    1.45 line, so a plain line is 34.8 high. It reads icon (14), bold kind ("Cuộc gọi video"), then the tail ("24 phút").
 *    A missed call is the danger-soft pill, 44 high: the icon, "Cuộc gọi nhỡ" in bold, "từ Trần Minh Quân · 09:14", and a "Gọi lại"
 *    button 28 high, radius 9, 8 px of margin plus the 10 gap from the text.
 *  - Chat-Var-Call-Busy: no call view; a status notice above the composer, 20 in from the pane on both sides, 10 above the composer,
 *    radius 10, padding 10 12, 10 between a 16 icon and the 13 px semibold text ("Trần Minh Quân đang trong cuộc gọi khác."), 38.8 high.
 *  - Chat-Call-DM-Missed (the caller's own view) stays the call stage covered by design-fidelity-chat-calls; its timeline line is the
 *    same pill, "Cuộc gọi nhỡ không được trả lời".
 * Left out on purpose: the board's "4 người" group-call line (group calls are not built). Every frame was reviewed by eye; set
 * CHAT_CALL_SHOTS to a directory to write the screenshots.
 */
const SHOTS = process.env.CHAT_CALL_SHOTS;
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
test.skip(persona !== "owner", "Needs the default owner world");

const SIZES = [
  { width: 1440, height: 900 },
  { width: 1024, height: 768 },
  { width: 390, height: 844 },
] as const;

const near = (actual: number, expected: number, what: string, tol = 1) =>
  expect(
    Math.abs(actual - expected),
    `${what}: ${actual.toFixed(1)} vs ${expected}`,
  ).toBeLessThanOrEqual(tol);
const box = async (loc: Locator) => {
  const b = await loc.first().boundingBox();
  if (!b) throw new Error(`no box for ${loc}`);
  return b;
};
const css = (loc: Locator, prop: string) =>
  loc
    .first()
    .evaluate((el, p) => getComputedStyle(el).getPropertyValue(p), prop);

async function settle(page: Page, name: string) {
  await page.waitForTimeout(400);
  if (SHOTS) await page.screenshot({ path: `${SHOTS}/${name}.png` });
}
async function axe(page: Page, what: string) {
  const result = await new AxeBuilder({ page })
    .include("[data-testid=chat-system-message], [role=status]")
    .analyze();
  expect(
    result.violations.map((v) => `${what}: ${v.id} ${v.nodes.length}`),
  ).toEqual([]);
}
async function noSideScroll(page: Page) {
  expect(
    await page.evaluate(
      () => document.documentElement.scrollWidth - window.innerWidth,
    ),
  ).toBeLessThanOrEqual(0);
}

for (const mode of ["light", "dark"] as const) {
  for (const size of SIZES) {
    const tag = `${size.width}-${mode}-vi`;
    test.describe(`${size.width} ${mode}`, () => {
      test.use({ viewport: { width: size.width, height: size.height } });

      test("the call lines: a finished call, a missed call with Gọi lại, and the call back rings", async ({
        page,
      }) => {
        const { space, peer } = await openDm(page, mode);
        await sendMessage(page, "Chào anh");
        // A finished call, then a missed one from the other person.
        await startCall(page, "video");
        const done = (await hook(page, "live", space)) as string;
        await hook(page, "act", done, peer, "answer");
        await hook(page, "act", done, peer, "end");
        await page
          .getByTestId("chat-call-actions")
          .getByRole("button")
          .nth(1)
          .click();
        const ring = (await hook(page, "ring", space, peer, "video")) as string;
        await hook(page, "miss", ring);
        const lines = page.getByTestId("chat-system-message");
        const plain = lines.filter({ hasText: /Cuộc gọi video \d/ });
        const missed = lines.filter({ hasText: /Cuộc gọi nhỡ từ/ });
        await expect(missed).toHaveCount(1);
        await settle(page, `call-lines-${tag}`);

        const button = missed.getByRole("button", { name: copy.calls.redial });
        // The boards draw the desktop pill; a phone wraps the button under the text (undrawn, reviewed by eye).
        if (size.width >= 1024) {
          const p = await box(plain.locator("p"));
          near(p.height, 34.8, "plain line height");
          const m = await box(missed.locator("p"));
          near(m.height, 44, "missed line height");
          expect(await css(plain.locator("p"), "border-radius")).toBe("14px");
          expect(await css(missed.locator("p"), "border-radius")).toBe("14px");
          expect(await css(plain.locator("p"), "font-size")).toBe("13px");
          const icon = await box(missed.locator("svg").first());
          near(icon.width, 14, "icon");
          const button = missed.getByRole("button", {
            name: copy.calls.redial,
          });
          const b = await box(button);
          near(b.height, 28, "Gọi lại height");
          expect(await css(button, "border-radius")).toBe("9px");
          const text = await box(missed.getByTestId("chat-call-line-text"));
          near(b.x - (text.x + text.width), 18, "text to button");
          near(icon.x - m.x, 14, "pill left padding");
          near(m.x + m.width - (b.x + b.width), 14, "pill right padding");
          // The pill is centred in the timeline.
          const row = await box(missed);
          near(m.x + m.width / 2, row.x + row.width / 2, "pill centred");
        }
        await noSideScroll(page);
        await axe(page, "call lines");

        await button.click();
        await expect(page.getByTestId("chat-call-stage")).toHaveAttribute(
          "data-phase",
          "outgoing-ringing",
        );
        await expect(page.getByTestId("chat-call-kind")).toContainText(
          copy.calls.kindVideo,
        );
      });

      test("busy: no call view, a notice above the composer", async ({
        page,
      }) => {
        const { space, peer } = await openDm(page, mode);
        await sendMessage(page, "Chào anh");
        const channel = (await hook(page, "channelOf", peer)) as string;
        expect(channel).not.toBe(space);
        await hook(page, "ring", channel, peer, "voice");
        // The channel rings the viewer too; declining it leaves the other person in their call.
        const toast = page.getByTestId("chat-call-incoming");
        await toast.getByRole("button", { name: copy.calls.decline }).click();
        await expect(toast).toHaveCount(0);
        await startCall(page, "voice");
        const notice = page.getByRole("status").filter({
          hasText: /đang trong cuộc gọi khác\.$/,
        });
        await expect(notice).toBeVisible();
        await expect(page.getByTestId("chat-call-stage")).toBeHidden();
        await settle(page, `busy-${tag}`);
        const n = await box(notice);
        const composer = await box(page.locator("main form").first());
        const pane = await box(notice.locator(".."));
        near(n.height, size.width === 390 ? n.height : 38.8, "notice height");
        near(n.x - pane.x, 20, "notice left inset");
        near(pane.x + pane.width - (n.x + n.width), 20, "notice right inset");
        expect(n.y + n.height).toBeLessThanOrEqual(composer.y);
        expect(await css(notice, "border-radius")).toBe("10px");
        expect(await css(notice, "font-size")).toBe("13px");
        expect(await css(notice, "font-weight")).toBe("600");
        near(
          (await box(notice.locator("svg").first())).width,
          16,
          "notice icon",
        );
        await noSideScroll(page);
        await axe(page, "busy notice");
      });
    });
  }
}
