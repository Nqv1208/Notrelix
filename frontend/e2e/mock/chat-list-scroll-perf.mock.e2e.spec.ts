import { expect, test } from "@playwright/test";
import { toNewest } from "./support/chat-newest";
import {
  WORKSPACE,
  OTHER_ROOM,
  fill,
} from "./support/chat-list-trace";

// The list draws every loaded row (up to 10 pages): scrolling through a 500-message channel must not stall a frame.
const LONGEST_FRAME_MS = 50;

test("scrolling through a 500-message channel never holds a frame past 50 ms", async ({
  page,
}) => {
  await page.goto(`/workspaces/${WORKSPACE}/chat/${OTHER_ROOM}`);
  await expect(page.getByRole("log")).toBeVisible({ timeout: 30_000 });
  await fill(page, 500);
  await page
    .getByRole("navigation", { name: /^(Channels|Kênh)$/ })
    .getByRole("button", { name: /random/ })
    .click();
  await expect(page.getByRole("log")).toBeVisible({ timeout: 30_000 });
  await toNewest(page);
  await expect(page.getByRole("log").getByText("Short 499")).toBeVisible({
    timeout: 20_000,
  });
  const box = (await page.getByRole("log").boundingBox())!;
  await page.mouse.move(box.x + box.width / 2, box.y + box.height / 2);
  await page.evaluate(() => {
    const g = globalThis as unknown as { __gaps: number[]; __run: boolean };
    g.__gaps = [];
    g.__run = true;
    let prev = performance.now();
    const tick = (now: number) => {
      g.__gaps.push(now - prev);
      prev = now;
      if (g.__run) requestAnimationFrame(tick);
    };
    requestAnimationFrame(tick);
  });
  for (let i = 0; i < 120; i++) {
    await page.mouse.wheel(0, -400);
    await page.waitForTimeout(30);
  }
  const gaps = await page.evaluate(() => {
    const g = globalThis as unknown as { __gaps: number[]; __run: boolean };
    g.__run = false;
    return g.__gaps;
  });
  const worst = Math.max(...gaps);
  console.log(`PERF slow ${gaps.map((g, i) => [i, g] as const).filter(([, g]) => g > 33).map(([i, g]) => `${i}:${g.toFixed(0)}`).join(" ")}`);
  console.log(`PERF longest frame ${worst.toFixed(1)} ms over ${gaps.length}`);
  expect(worst).toBeLessThanOrEqual(LONGEST_FRAME_MS);
});
