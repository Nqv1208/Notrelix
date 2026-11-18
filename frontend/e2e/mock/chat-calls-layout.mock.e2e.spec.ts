import { expect, test, type Page } from "@playwright/test";
import {
  hook,
  openDm,
  sendMessage,
  startCall,
  SURFACES,
} from "./support/chat-calls";

/**
 * Owner rule: whenever a call is live (or ringing in) and the person is in the conversation view, the composer and the latest
 * message stay visible and usable at every width. At 1440, 1024 and 390, light and dark, the composer's box and the last
 * message's box do not intersect any call surface (toast, bar, dock), and the composer is clickable (`elementFromPoint` at its
 * centre lands inside it). The full-screen call view and the phone's full-screen ring are the calls themselves, not a cover:
 * they are checked in design-fidelity-chat-calls.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
test.skip(persona !== "owner", "Needs the default owner world");

const SIZES = [
  { width: 1440, height: 900 },
  { width: 1024, height: 768 },
  { width: 390, height: 844 },
] as const;

type Rect = { x: number; y: number; w: number; h: number };

async function rects(page: Page) {
  return page.evaluate((surfaces) => {
    const r = (el: Element | null): Rect | null => {
      if (!el) return null;
      const b = el.getBoundingClientRect();
      return { x: b.x, y: b.y, w: b.width, h: b.height };
    };
    const composer = document.querySelector("main form:has(textarea)");
    const bubbles = document.querySelectorAll("[data-bubble]");
    const c = r(composer);
    const centre = c
      ? document.elementFromPoint(c.x + c.w / 2, c.y + c.h / 2)
      : null;
    return {
      composer: c,
      last: r(bubbles[bubbles.length - 1] ?? null),
      surfaces: [...document.querySelectorAll(surfaces)].map((el) => ({
        id: el.getAttribute("data-testid"),
        box: r(el),
      })),
      composerClickable: Boolean(
        composer && centre && composer.contains(centre),
      ),
    };
  }, SURFACES);
}

const overlap = (a: Rect, b: Rect) =>
  Math.min(a.x + a.w, b.x + b.w) - Math.max(a.x, b.x) > 0.5 &&
  Math.min(a.y + a.h, b.y + b.h) - Math.max(a.y, b.y) > 0.5;

async function expectClear(page: Page, what: string) {
  await page.waitForTimeout(450);
  const seen = await rects(page);
  expect(
    seen.surfaces.length,
    `${what}: a call surface is on screen`,
  ).toBeGreaterThan(0);
  expect(seen.composer, `${what}: the composer is on screen`).not.toBeNull();
  expect(seen.last, `${what}: the last message is on screen`).not.toBeNull();
  for (const surface of seen.surfaces) {
    expect(
      overlap(surface.box!, seen.composer!),
      `${what}: ${surface.id} covers the composer`,
    ).toBe(false);
    expect(
      overlap(surface.box!, seen.last!),
      `${what}: ${surface.id} covers the last message`,
    ).toBe(false);
  }
  expect(seen.composerClickable, `${what}: the composer is clickable`).toBe(
    true,
  );
  const field = page.locator("main form textarea").first();
  await field.click();
  await field.fill("");
  await field.pressSequentially("ok");
  await expect(field).toHaveValue("ok");
  await field.fill("");
}

for (const mode of ["light", "dark"] as const) {
  for (const size of SIZES) {
    test.describe(`${size.width} ${mode}`, () => {
      test.use({ viewport: { width: size.width, height: size.height } });

      test("a live call and the conversation view: composer and latest message stay clear", async ({
        page,
      }) => {
        const { space, peer } = await openDm(page, mode);
        await sendMessage(page, "Chiều nay mình gọi nhanh nhé");
        await startCall(page, "video");
        const id = (await hook(page, "live", space)) as string;
        await hook(page, "act", id, peer, "answer");
        // Back to the conversation (the call view offers it; an older build had no such control).
        await page.locator(SURFACES).first().waitFor();
        const back = page.getByRole("button", {
          name: "Quay lại cuộc trò chuyện",
        });
        if (await back.count()) await back.first().click();
        await expectClear(page, "connected");
      });

      test("a ringing call, back in the conversation", async ({ page }) => {
        const { space } = await openDm(page, mode);
        await sendMessage(page, "Gọi cho mình nhé");
        await startCall(page, "voice");
        expect(await hook(page, "live", space)).not.toBeNull();
        await page.locator(SURFACES).first().waitFor();
        const back = page.getByRole("button", {
          name: "Quay lại cuộc trò chuyện",
        });
        if (await back.count()) await back.first().click();
        await expectClear(page, "ringing out");
      });

      test("an incoming ring: the toast leaves the composer and the latest message alone", async ({
        page,
      }) => {
        test.skip(
          size.width < 1024,
          "A phone shows the ring as a full-screen sheet (Chat-Call-Incoming-M)",
        );
        const { space, peer } = await openDm(page, mode);
        await sendMessage(page, "Mình đây");
        await hook(page, "ring", space, peer, "voice");
        await expect(page.getByTestId("chat-call-incoming")).toBeVisible();
        await expectClear(page, "incoming ring");
      });
    });
  }
}
