import { test } from "@playwright/test";
import { expect } from "@playwright/test";
import {
  TOLERANCE_PX,
  atNewest,
  openFilledRoom,
  pick,
  report,
  startTrace,
  stopTrace,
  steps,
} from "./support/chat-list-trace";

// Wheel scrolling moves a message by the wheel delta and nothing more. Smooth wheel scrolling is off so one wheel event
// is one frame of exactly its delta. Run with VITE_MOCK_LATENCY=slow like the other visible-motion specs.

test.use({ launchOptions: { args: ["--disable-smooth-scrolling"] } });

test.beforeEach(async ({ page }) => {
  await openFilledRoom(page);
});

test.fixme("(d) wheel scrolling moves the message by the wheel delta and nothing more", async ({
  page,
}) => {
  await atNewest(page);
  const box = (await page.getByRole("log").boundingBox())!;
  await page.mouse.move(box.x + box.width / 2, box.y + box.height / 2);
  const DELTA = 120;
  await startTrace(page, await pick(page), true);
  // Up first (rows above were never measured), then down again.
  for (let i = 0; i < 40; i++) {
    await page.mouse.wheel(0, -DELTA);
    await page.waitForTimeout(70);
  }
  for (let i = 0; i < 20; i++) {
    await page.mouse.wheel(0, DELTA);
    await page.waitForTimeout(70);
  }
  const extra = Math.max(
    0,
    ...steps(await stopTrace(page)).map((x) => {
      const k = Math.round(Math.abs(x) / DELTA);
      return Math.abs(Math.abs(x) - k * DELTA);
    }),
  );
  report("d-wheel", extra);
  expect(extra).toBeLessThanOrEqual(TOLERANCE_PX);
});
