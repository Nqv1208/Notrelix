import { expect, test, type Page } from "@playwright/test";
import { toNewest } from "./support/chat-newest";

// What the reader SEES move in the Chat message list: the viewport top of the message under their eye, sampled every
// animation frame. Layout-shift scores cannot tell a visible jump from a row the virtual list repositions inside its
// scroller; this can. Each scenario prints its worst movement (`MOTION <scenario> <px>`), which the PR table quotes.
// (d) turns smooth wheel scrolling off so one wheel event is one frame of exactly its delta; the follow-scroll of (b)
// and (c) is a smooth scroll and runs with it on.
// Run with VITE_MOCK_LATENCY=slow to hold the older page and the send open long enough to be measured.

const WORKSPACE = "mock-workspace-primary";
const ROOM = "c1000000-0000-4000-8000-000000000005";
const OTHER_ROOM = "c1000000-0000-4000-8000-000000000001";
const OTHER_MEMBER = "mock-user-admin";
const TOLERANCE_PX = 1;

type Sample = { id: string | null; top: number | null; st: number };
type Host = {
  __notrelixMockInjectChat?: (s: string, a: string, c: string) => boolean;
  __trace?: { samples: Sample[]; run: boolean };
};

/** Messages of mixed length and shape, so row heights differ the way a real room's do. */
async function fill(page: Page, count: number) {
  await page.evaluate(
    ([space, author, n]) => {
      const inject = (globalThis as unknown as Host).__notrelixMockInjectChat!;
      for (let i = 0; i < Number(n); i++) {
        const body =
          i % 7 === 0
            ? `Long ${i}: ${"a wrapped line of text ".repeat(12 + (i % 5) * 6)}`
            : i % 5 === 0
              ? `Two ${i}\nlines ${i}`
              : `Short ${i}`;
        inject(String(space), String(author), body);
      }
    },
    [ROOM, OTHER_MEMBER, count] as const,
  );
}

/** The store lives in the page, so the room is opened by the channel list, never by a reload. */
async function openFilledRoom(page: Page) {
  await page.goto(`/workspaces/${WORKSPACE}/chat/${OTHER_ROOM}`);
  await expect(page.getByRole("log")).toBeVisible({ timeout: 30_000 });
  await fill(page, 320);
  await page
    .getByRole("navigation", { name: /^(Channels|Kênh)$/ })
    .getByRole("button", { name: /random/ })
    .click();
  await expect(page.getByRole("log")).toBeVisible({ timeout: 30_000 });
  await page.waitForTimeout(800);
}

/** The room opens at its first unread message: go to the true newest window (the last filled message is in the page). */
async function atNewest(page: Page) {
  await toNewest(page);
  await expect(page.getByRole("log").getByText("Short 319")).toBeVisible({
    timeout: 20_000,
  });
  await page.waitForTimeout(500);
}

/** The id of the message under the middle of the list, or the last one wholly above it. */
async function pick(page: Page, which?: "above") {
  return page.evaluate((w) => {
    const log = document.querySelector("[role=log]")!.getBoundingClientRect();
    const mid = (log.top + log.bottom) / 2;
    const rows = Array.from(document.querySelectorAll("[data-message-id]")).map(
      (el) => ({
        id: el.getAttribute("data-message-id")!,
        r: el.getBoundingClientRect(),
      }),
    );
    if (w === "above")
      return rows.filter((x) => x.r.bottom < mid && x.r.top > log.top).at(-1)!
        .id;
    return (rows.find((x) => x.r.top <= mid && x.r.bottom >= mid) ?? rows[0]!)
      .id;
  }, which);
}

async function startTrace(page: Page, id: string, repick = false) {
  await page.evaluate(
    ([anchor, again]) => {
      const scroller = document.querySelector("[role=log]") as HTMLElement;
      const trace = { samples: [] as Sample[], run: true };
      (globalThis as unknown as Host).__trace = trace;
      let current = String(anchor);
      const frame = () => {
        let el = document.querySelector(`[data-message-id="${current}"]`);
        if (!el && again) {
          const box = scroller.getBoundingClientRect();
          const mid = (box.top + box.bottom) / 2;
          el =
            Array.from(document.querySelectorAll("[data-message-id]")).sort(
              (a, b) =>
                Math.abs(a.getBoundingClientRect().top - mid) -
                Math.abs(b.getBoundingClientRect().top - mid),
            )[0] ?? null;
          if (el) current = el.getAttribute("data-message-id")!;
        }
        trace.samples.push({
          id: el ? current : null,
          top: el ? el.getBoundingClientRect().top : null,
          st: scroller.scrollTop,
        });
        if (trace.run) requestAnimationFrame(frame);
      };
      requestAnimationFrame(frame);
    },
    [id, repick] as const,
  );
}

async function stopTrace(page: Page): Promise<Sample[]> {
  return page.evaluate(() => {
    const trace = (globalThis as unknown as Host).__trace!;
    trace.run = false;
    return trace.samples;
  });
}

/** Per-frame change of the anchored top, skipping frames where it was not in the page or the anchor was swapped. */
function steps(samples: Sample[]): number[] {
  const out: number[] = [];
  for (let i = 1; i < samples.length; i++) {
    const a = samples[i - 1]!;
    const b = samples[i]!;
    if (a.top === null || b.top === null || a.id !== b.id) continue;
    out.push(b.top - a.top);
  }
  return out;
}

function report(scenario: string, px: number) {
  console.log(`MOTION ${scenario} ${px.toFixed(1)}`);
}

function maxDrift(samples: Sample[]): number {
  const tops = samples.flatMap((s) => (s.top === null ? [] : [s.top]));
  return Math.max(...tops.map((t) => Math.abs(t - tops[0]!)));
}

/** A follow-scroll only ever moves the anchor up, and never in one frame carries most of the trip. */
function expectSmoothFollow(scenario: string, samples: Sample[]) {
  const s = steps(samples);
  const backward = Math.max(0, ...s);
  const total = Math.abs(s.reduce((sum, x) => sum + x, 0));
  const biggest = Math.max(0, ...s.map(Math.abs));
  report(`${scenario}`, backward);
  console.log(
    `MOTION ${scenario}-total ${total.toFixed(1)} biggest-step ${biggest.toFixed(1)}`,
  );
  expect(backward).toBeLessThanOrEqual(TOLERANCE_PX);
  if (total > 30) expect(biggest).toBeLessThanOrEqual(total * 0.6);
}

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
  const drift = maxDrift(await stopTrace(page));
  report("a-prepend", drift);
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

test.describe("wheel", () => {
  test.use({ launchOptions: { args: ["--disable-smooth-scrolling"] } });

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
  await startTrace(page, id);
  await page.evaluate((rowId) => {
    const row = document.querySelector(`[data-message-id="${rowId}"]`)!;
    const img = document.createElement("img");
    // The app reserves the box from the stored size, so the row does not grow when the file arrives.
    img.width = 240;
    img.height = 180;
    img.style.display = "block";
    row.appendChild(img);
    const svg =
      "<svg xmlns='http://www.w3.org/2000/svg' width='240' height='180'><rect width='240' height='180' fill='%23888'/></svg>";
    setTimeout(() => {
      img.src = `data:image/svg+xml;utf8,${svg}`;
    }, 300);
  }, above);
  await page.waitForTimeout(1200);
  const drift = maxDrift(await stopTrace(page));
  report("e-image-growth", drift);
  expect(drift).toBeLessThanOrEqual(TOLERANCE_PX);
});
