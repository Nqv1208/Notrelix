import { expect, test } from "@playwright/test";
import { apiRead, apiWrite, realUsers, signIn } from "./support/real-session";
import { createOwnedWorkspace } from "./support/real-workspace";

test.use({ viewport: { width: 1440, height: 900 } });
test.setTimeout(1_800_000);

/**
 * A board of 5,000 items opens from the server's first pages: the board read returns at most 200 items per lane
 * whatever page size is asked, a lane page is clamped to 200, the filtered totals stay exact, a filter that matches
 * nothing answers zero totals and not an error, and the Table and the Kanban draw their first rows in under 2 s.
 * The numbers are printed (`LARGE-BOARD`) for the pull request.
 */
interface Lane {
  readonly itemCount: number;
  readonly filteredCount: number;
  readonly nextCursor?: string | null;
}
interface Full {
  readonly lists: readonly {
    readonly id: string;
    readonly boardItems: readonly { readonly id: string }[];
  }[];
  readonly lanes?: readonly Lane[];
}
const TOTAL = 5000;
const idOf = (data: string | { id: string } | null) =>
  typeof data === "string" ? data : data!.id;

test("a board of 5,000 items returns at most 200 items per lane and draws its first rows in under 2 s", async ({
  page,
}) => {
  await signIn(page, realUsers.admin);
  const workspace = await createOwnedWorkspace(page, "Large board");
  const board = idOf(
    (
      await apiWrite<string | { id: string }>(
        page,
        "POST",
        `/workspaces/${workspace.id}/boards`,
        { title: `Large ${Date.now().toString(36)}`, visibility: "Workspace" },
      )
    ).data,
  );
  const start = await apiRead<Full>(page, `/boards/${board}/full`);
  const lists = start.lists.map((list) => list.id);
  // The CSRF issue rate is limited (120 a minute): one token serves the whole seeding on a quiet page and is
  // fetched again only when the server refuses it.
  await page.goto("about:blank");
  const seeding = Date.now();
  const apiUrl = process.env.VITE_API_URL ?? "http://127.0.0.1:58000/api/v1";
  let token = "";
  const fresh = async () => {
    token = (
      (await (await page.request.get(`${apiUrl}/auth/csrf`)).json()) as {
        token: string;
      }
    ).token;
  };
  await fresh();
  const seed = async (index: number, dueDate?: string) => {
    for (let attempt = 1; ; attempt += 1) {
      const response = await page.request.fetch(
        `${apiUrl}/boards/${board}/items`,
        {
          method: "POST",
          headers: {
            "X-CSRF-Token": token,
            "Idempotency-Key": crypto.randomUUID(),
            "Content-Type": "application/json",
          },
          data: JSON.stringify({
            groupId: lists[index % lists.length],
            title: `Item ${String(index).padStart(4, "0")}`,
            ...(dueDate ? { dueDate } : {}),
          }),
        },
      );
      if (response.status() === 403 && attempt < 5) {
        await fresh();
        continue;
      }
      expect(response.status(), `item ${index}`).toBeLessThan(300);
      return;
    }
  };
  // The first twenty are due on day 1..20 of November 2026: the week of 2..8 holds seven of them.
  for (let index = 0; index < TOTAL; index += 1)
    await seed(
      index,
      index < 20
        ? new Date(Date.UTC(2026, 10, index + 1, 12)).toISOString()
        : undefined,
    );
  console.log(
    `LARGE-BOARD seeded ${TOTAL} items in ${Date.now() - seeding} ms`,
  );

  // The page size is clamped: asking for 500 returns at most 200 per lane, and the lane totals add up to the board.
  const timed = async (query: string) => {
    const begun = Date.now();
    const read = await apiRead<Full>(page, `/boards/${board}/full${query}`);
    return { read, ms: Date.now() - begun };
  };
  const clamped = await timed("?pageSize=500");
  const perLane = clamped.read.lists.map((list) => list.boardItems.length);
  console.log(
    `LARGE-BOARD full?pageSize=500 ${clamped.ms} ms, items per lane ${perLane.join("/")}`,
  );
  for (const count of perLane) expect(count).toBeLessThanOrEqual(200);
  const lanes = clamped.read.lanes ?? [];
  expect(lanes.reduce((sum, lane) => sum + lane.itemCount, 0)).toBe(TOTAL);

  const first = await timed("?pageSize=50");
  for (const list of first.read.lists)
    expect(list.boardItems.length).toBeLessThanOrEqual(50);
  console.log(`LARGE-BOARD full?pageSize=50 ${first.ms} ms`);

  // A filter that matches nothing is zero totals, not an error.
  const none = await apiRead<Full>(
    page,
    `/boards/${board}/full?pageSize=50&filter=${encodeURIComponent("c2:and~%24title.contains.zzzznothing")}`,
  );
  expect(
    (none.lanes ?? []).reduce((sum, lane) => sum + lane.filteredCount, 0),
  ).toBe(0);
  expect(none.lists.flatMap((list) => list.boardItems)).toHaveLength(0);

  // A date window reads only the items whose days touch it: the week of 2026-11-02..08 holds seven dated items.
  const inWeek = 7;
  const windowed = await apiRead<Full>(
    page,
    `/boards/${board}/full?pageSize=200&filter=${encodeURIComponent("c2:and~%24span.overlaps.2026-11-02.2026-11-08")}`,
  );
  const windowTotal = (windowed.lanes ?? []).reduce(
    (sum, lane) => sum + lane.filteredCount,
    0,
  );
  console.log(
    `LARGE-BOARD window 2026-11-02..08 filtered total ${windowTotal} (expected ${inWeek})`,
  );
  expect(windowTotal).toBe(inWeek);
  const emptyWindow = await apiRead<Full>(
    page,
    `/boards/${board}/full?pageSize=50&filter=${encodeURIComponent("c2:and~%24span.overlaps.2030-01-01.2030-01-31")}`,
  );
  expect(
    (emptyWindow.lanes ?? []).reduce(
      (sum, lane) => sum + lane.filteredCount,
      0,
    ),
  ).toBe(0);

  // The Calendar on the month of 2026-11 reads that month, not the board.
  const fullReads: string[] = [];
  page.on("request", (request) => {
    if (/\/boards\/[^/]+\/full/.test(request.url()))
      fullReads.push(request.url());
  });
  const calendarOpened = Date.now();
  await page.goto(`/workspaces/${workspace.id}/boards/${board}?view=calendar`);
  await expect(page.locator("[data-calendar-event]").first()).toBeVisible({
    timeout: 30_000,
  });
  const calendarMs = Date.now() - calendarOpened;
  const chips = await page.locator("[data-calendar-event]").count();
  console.log(
    `LARGE-BOARD calendar first chip ${calendarMs} ms, ${chips} chips drawn, ${fullReads.length} board reads, unwindowed reads ${fullReads.filter((url) => !url.includes("pageSize")).length}`,
  );
  expect(fullReads.filter((url) => !url.includes("pageSize"))).toHaveLength(0);

  // The first paint of each view that reads pages.
  for (const view of ["table", "kanban"] as const) {
    const opened = Date.now();
    await page.goto(`/workspaces/${workspace.id}/boards/${board}?view=${view}`);
    await expect(
      page
        .locator(view === "table" ? "[data-row-id]" : "[data-card-id]")
        .first(),
    ).toBeVisible({ timeout: 30_000 });
    const ms = Date.now() - opened;
    const drawn = await page
      .locator(view === "table" ? "[data-row-id]" : "[data-card-id]")
      .count();
    const nodes = await page.evaluate(
      () => document.querySelectorAll("*").length,
    );
    console.log(
      `LARGE-BOARD ${view} first row ${ms} ms, ${drawn} rows drawn, ${nodes} DOM nodes`,
    );
    expect(ms).toBeLessThan(2000);
  }
});
