import { expect, test, type Page } from "@playwright/test";
import {
  OTHER_MEMBER,
  ROOM,
  TOLERANCE_PX,
  atNewest,
  expectSmoothFollow,
  maxDrift,
  openFilledRoom,
  pick,
  report,
  startTrace,
  stopTrace,
  type Host,
} from "./support/chat-list-trace";

// What the reader SEES move in the Chat message list: the viewport top of the message under their eye, sampled every
// animation frame. Layout-shift scores cannot tell a visible jump from a row the virtual list repositions inside its
// scroller; this can. Each scenario prints its worst movement (`MOTION <scenario> <px>`), which the PR table quotes.
// (d) turns smooth wheel scrolling off so one wheel event is one frame of exactly its delta; the follow-scroll of (b)
// and (c) is a smooth scroll and runs with it on.
// Run with VITE_MOCK_LATENCY=slow to hold the older page and the send open long enough to be measured.

test.beforeEach(async ({ page }) => {
  await openFilledRoom(page);
});

test("(a) an older page loading at the top moves nothing the reader sees", async ({
  page,
}) => {
  await atNewest(page);
  const log = page.getByRole("log");
  // To the very top: the request for the older page starts there and lands a moment later.
  await log.evaluate((el) => {
    el.scrollTop = 0;
  });
  await page.waitForTimeout(100);
  const before = await page.locator("[data-message-id]").count();
  const id = await pick(page);
  await startTrace(page, id);
  await expect
    .poll(() => page.locator("[data-message-id]").count(), { timeout: 8_000 })
    .not.toBe(before);
  await page.waitForTimeout(800);
  const samples = await stopTrace(page);
  const drift = maxDrift(samples);
  report("a-prepend", drift);
  samples.forEach((x, i) => {
    const p = samples[i - 1];
    if (p && x.top !== null && p.top !== null && Math.abs(x.top - p.top) > 1)
      console.log(
        `DBGA frame ${i} id=${x.id === p.id} top ${p.top.toFixed(0)}->${x.top.toFixed(0)} st ${p.st.toFixed(0)}->${x.st.toFixed(0)}`,
      );
  });
  expect(drift).toBeLessThanOrEqual(TOLERANCE_PX);
});

test.fixme("(b) a new message while at the bottom follows smoothly, never snaps", async ({
  page,
}) => {
  await atNewest(page);
  await startTrace(page, await pick(page, "above"));
  await page.evaluate(
    ([space, author]) =>
      (globalThis as unknown as Host).__notrelixMockInjectChat!(
        String(space),
        String(author),
        "Incoming at the bottom\nwith a second line",
      ),
    [ROOM, OTHER_MEMBER] as const,
  );
  await page.waitForTimeout(1200);
  expectSmoothFollow("b-incoming", await stopTrace(page));
});

test.fixme("(c) an own send goes from pending to confirmed without a snap", async ({
  page,
}) => {
  await atNewest(page);
  await startTrace(page, await pick(page, "above"));
  const composer = page.getByRole("combobox", {
    name: /^(Message #random|Tin nhắn tới #random)$/,
  });
  await composer.fill("Own send under measure");
  await composer.press("Enter");
  await expect(page.getByTestId("chat-pending-message")).toHaveCount(0, {
    timeout: 20_000,
  });
  await page.waitForTimeout(800);
  expectSmoothFollow("c-own-send", await stopTrace(page));
});

test("(c2) a send from a room opened at its first unread is confirmed in place", async ({
  page,
}) => {
  // No jump to the newest first: the reader types at once, in the window the room opened at.
  const composer = page.getByRole("combobox", {
    name: /^(Message #random|Tin nhắn tới #random)$/,
  });
  await composer.fill("Sent from the first unread");
  await composer.press("Enter");
  const log = page.getByRole("log");
  await expect(page.getByTestId("chat-pending-message")).toHaveCount(0, {
    timeout: 8_000,
  });
  await expect(log.getByText("Sent from the first unread")).toHaveCount(1);
});

test("(e) an image loading in a row above the reader moves nothing", async ({
  page,
}) => {
  await atNewest(page);
  await page.getByRole("log").evaluate((el) => {
    el.scrollTop -= 900;
  });
  await page.waitForTimeout(600);
  const id = await pick(page);
  const above = await pick(page, "above");
  // The app reserves the box from the stored size: it is in the row before the file arrives, so loading changes no height.
  await page.evaluate((rowId) => {
    const row = document.querySelector(`[data-message-id="${rowId}"]`)!;
    const img = document.createElement("img");
    img.id = "motion-probe-image";
    img.width = 240;
    img.height = 180;
    img.style.display = "block";
    row.appendChild(img);
  }, above);
  await page.waitForTimeout(600);
  await startTrace(page, id);
  await page.evaluate(() => {
    const svg =
      "<svg xmlns='http://www.w3.org/2000/svg' width='240' height='180'><rect width='240' height='180' fill='%23888'/></svg>";
    (document.getElementById("motion-probe-image") as HTMLImageElement).src =
      `data:image/svg+xml;utf8,${svg}`;
  });
  await page.waitForTimeout(1200);
  const drift = maxDrift(await stopTrace(page));
  report("e-image-growth", drift);
  expect(drift).toBeLessThanOrEqual(TOLERANCE_PX);
});
