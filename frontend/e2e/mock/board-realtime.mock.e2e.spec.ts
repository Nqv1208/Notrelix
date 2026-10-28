import { expect, test, type Page } from "@playwright/test";

/**
 * Realtime convergence of the open board on the mock world (WM-032). The mock
 * world is one browser's memory, so "someone else" is played through the two
 * handles the dev app exposes: a write on the mock API and a gateway-shaped
 * frame on the mock transport. A frame is notify-only: the board changes only
 * when its frame arrives, and only a frame of the open board counts.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const WORKSPACE = "mock-workspace-primary";
const BOARD_ID = "mock-board-erp";
const BOARD = `/workspaces/${WORKSPACE}/boards/${BOARD_ID}`;
const NOW = new Date("2026-10-12T10:40:00+07:00");
const RENAMED = "Đổi tên bởi đồng nghiệp";

async function open(page: Page) {
  await page.clock.setFixedTime(NOW);
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "vi");
    } catch {
      // Private mode: the default applies.
    }
  });
  await page.goto(BOARD);
  await expect(
    page.getByRole("heading", { level: 1, name: "Triển khai ERP Q4" }),
  ).toBeVisible();
}

/** A write by someone else on the mock API: no frame, so no refetch yet. */
async function otherPersonWrites(
  page: Page,
  itemId: string,
  method: "PATCH" | "DELETE",
  body?: Record<string, unknown>,
) {
  await page.evaluate(
    async ({ itemId, method, body }) => {
      const mock = (
        window as unknown as {
          __notrelixMock: { fetch: typeof fetch };
        }
      ).__notrelixMock;
      const response = await mock.fetch(
        `http://127.0.0.1:59999/api/v1/board-items/${itemId}`,
        {
          method,
          headers: { "content-type": "application/json" },
          body: body ? JSON.stringify(body) : undefined,
        },
      );
      if (!response.ok) throw new Error(`mock write failed ${response.status}`);
    },
    { itemId, method, body },
  );
}

let frameCounter = 0;

/** A gateway frame on a board stream, as the backend announces a change. */
async function deliverBoardFrame(
  page: Page,
  boardId: string,
  changeKind: string,
) {
  frameCounter += 1;
  await page.evaluate(
    ({ boardId, changeKind, n }) => {
      const mock = (
        window as unknown as {
          __notrelixMock: {
            realtime: { deliverEvent(envelope: unknown): void };
          };
        }
      ).__notrelixMock;
      mock.realtime.deliverEvent({
        schemaVersion: 1,
        eventId: `e2e-${n}`,
        eventType: "board.board",
        changeKind,
        subscriptionId: `board:${boardId}`,
        streamKey: `board:Board:${boardId}`,
        streamVersion: n,
        resourceKind: "Board",
        resourceId: boardId,
        aggregateId: boardId,
        workspaceId: "mock-workspace-primary",
        correlationId: "e2e",
        timestamp: new Date().toISOString(),
      });
    },
    { boardId, changeKind, n: frameCounter },
  );
}

test.describe("Board realtime convergence (owner)", () => {
  test.skip(
    persona !== "owner" || scenario !== "default" || faults !== "",
    "Needs the default owner world without faults",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  test("a rename by someone else shows on the open board when its frame arrives, not for a frame of another board", async ({
    page,
  }) => {
    await open(page);
    const lane = page.getByRole("region", { name: "Cần làm", exact: true });
    const card = lane.locator("button[data-card-id]").first();
    const itemId = (await card.getAttribute("data-card-id"))!;

    await otherPersonWrites(page, itemId, "PATCH", { title: RENAMED });
    const renamed = lane.locator(`button[data-card-id="${itemId}"]`);
    await expect(renamed).not.toHaveAttribute("aria-label", RENAMED);

    // A late frame of a board that is not open must not refetch this one.
    await deliverBoardFrame(page, "mock-board-previous", "UpdateBoardItem");
    await expect(renamed).not.toHaveAttribute("aria-label", RENAMED);

    await deliverBoardFrame(page, BOARD_ID, "UpdateBoardItem");
    await expect(renamed).toHaveAttribute("aria-label", RENAMED);
  });

  test("a delete by someone else removes the card from the open board when its frame arrives", async ({
    page,
  }) => {
    await open(page);
    const lane = page.getByRole("region", { name: "Cần làm", exact: true });
    const cards = lane.locator("button[data-card-id]");
    const before = await cards.count();
    const itemId = (await cards.first().getAttribute("data-card-id"))!;

    await otherPersonWrites(page, itemId, "DELETE");
    await expect(cards).toHaveCount(before);

    await deliverBoardFrame(page, BOARD_ID, "DeleteBoardItem");
    await expect(cards).toHaveCount(before - 1);
    await expect(lane.locator(`button[data-card-id="${itemId}"]`)).toHaveCount(
      0,
    );
  });
});
