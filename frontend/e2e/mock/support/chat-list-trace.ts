import { expect, type Page } from "@playwright/test";
import { toNewest } from "./chat-newest";

// Shared by the visible-motion specs: the room is filled, and the anchored message is sampled every animation frame.

export const WORKSPACE = "mock-workspace-primary";
export const ROOM = "c1000000-0000-4000-8000-000000000005";
export const OTHER_ROOM = "c1000000-0000-4000-8000-000000000001";
export const OTHER_MEMBER = "mock-user-admin";
export const TOLERANCE_PX = 1;

export type Sample = { id: string | null; top: number | null; st: number };
export type Host = {
  __notrelixMockInjectChat?: (s: string, a: string, c: string) => boolean;
  __trace?: { samples: Sample[]; run: boolean };
};

/** Messages of mixed length and shape, so row heights differ the way a real room's do. */
export async function fill(page: Page, count: number) {
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
export async function openFilledRoom(page: Page) {
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
export async function atNewest(page: Page) {
  await toNewest(page);
  await expect(page.getByRole("log").getByText("Short 319")).toBeVisible({
    timeout: 20_000,
  });
  await page.waitForTimeout(500);
}

/** The id of the message under the middle of the list, or the last one wholly above it. */
export async function pick(page: Page, which?: "above") {
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
    // The row under the middle; between two bubbles, the one nearest to it (the whole history is in the page now).
    const dist = (x: (typeof rows)[number]) =>
      x.r.height === 0 ? Infinity : Math.abs((x.r.top + x.r.bottom) / 2 - mid);
    return rows.reduce((a, b) => (dist(b) < dist(a) ? b : a)).id;
  }, which);
}

export async function startTrace(page: Page, id: string, repick = false) {
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

export async function stopTrace(page: Page): Promise<Sample[]> {
  return page.evaluate(() => {
    const trace = (globalThis as unknown as Host).__trace!;
    trace.run = false;
    return trace.samples;
  });
}

/** Per-frame change of the anchored top, skipping frames where it was not in the page or the anchor was swapped. */
export function steps(samples: Sample[]): number[] {
  const out: number[] = [];
  for (let i = 1; i < samples.length; i++) {
    const a = samples[i - 1]!;
    const b = samples[i]!;
    if (a.top === null || b.top === null || a.id !== b.id) continue;
    out.push(b.top - a.top);
  }
  return out;
}

export function report(scenario: string, px: number) {
  console.log(`MOTION ${scenario} ${px.toFixed(1)}`);
}

export function maxDrift(samples: Sample[]): number {
  const tops = samples.flatMap((s) => (s.top === null ? [] : [s.top]));
  return Math.max(...tops.map((t) => Math.abs(t - tops[0]!)));
}

/** A follow-scroll only ever moves the anchor up, and never in one frame carries most of the trip. */
export function expectSmoothFollow(scenario: string, samples: Sample[]) {
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
