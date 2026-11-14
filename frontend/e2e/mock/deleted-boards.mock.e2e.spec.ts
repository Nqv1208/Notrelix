import { expect, test, type Page } from "@playwright/test";

/**
 * UC-WM-A07 on the mock world as the workspace owner: the Deleted tab lists the boards deleted in the
 * last 30 days that the person can restore (one deleted by someone else included, because the owner
 * manages it), Restore brings one back, a board past its window is refused with the reason, and
 * `?spaceId=` narrows the list to one Space with a way back to every board. Deleted boards are seeded
 * through `globalThis.__mockDeletedBoards`; `__mockBoardRestoreWindowElapsed` makes restore refuse.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const WS = "/workspaces/mock-workspace-primary";
const ROADMAP = `${WS}/boards/mock-board-roadmap`;

test.skip(
  persona !== "owner" || scenario !== "default",
  "Owner checks need the default owner world",
);
test.use({ viewport: { width: 1440, height: 900 } });

const SEEDS = [
  { id: "mock-deleted-q3", title: "Q3 plan", daysAgo: 2, byOther: true },
  {
    id: "mock-deleted-hiring",
    title: "Hiring",
    daysAgo: 5,
    spaceId: "mock-space-people",
  },
  { id: "mock-deleted-old", title: "Old launch", daysAgo: 40 },
];

async function open(page: Page, flags: { windowElapsed?: boolean } = {}) {
  await page.clock.setFixedTime(new Date("2026-10-12T10:40:00+07:00"));
  await page.addInitScript(
    ({ seeds, windowElapsed }) => {
      try {
        window.localStorage.setItem("notrelix-locale", "en");
      } catch {
        // Private mode: the default locale applies.
      }
      (globalThis as Record<string, unknown>).__mockDeletedBoards = seeds;
      (globalThis as Record<string, unknown>).__mockBoardRestoreWindowElapsed =
        windowElapsed;
    },
    { seeds: SEEDS, windowElapsed: flags.windowElapsed === true },
  );
}

function rowOf(page: Page, title: string) {
  return page.getByRole("listitem").filter({ hasText: title });
}

test("the Deleted tab lists what the owner can restore inside 30 days, and Restore brings it back", async ({
  page,
}) => {
  await open(page);
  await page.goto(`${WS}/archived-boards?tab=deleted`);

  await expect(page.getByRole("tab", { name: "Deleted" })).toHaveAttribute(
    "aria-selected",
    "true",
  );
  await expect(rowOf(page, "Q3 plan")).toBeVisible();
  await expect(rowOf(page, "Hiring")).toBeVisible();
  await expect(rowOf(page, "Old launch")).toHaveCount(0);
  await expect(rowOf(page, "Q3 plan")).toContainText("Restorable until");

  await rowOf(page, "Q3 plan").getByRole("button", { name: "Restore" }).click();
  await expect(page.getByText("The board was restored.")).toBeVisible();
  await expect(rowOf(page, "Q3 plan")).toHaveCount(0);
  await expect(rowOf(page, "Hiring")).toBeVisible();
});

test("a restore refused after the window says why and keeps the row", async ({
  page,
}) => {
  await open(page, { windowElapsed: true });
  await page.goto(`${WS}/archived-boards?tab=deleted`);

  await rowOf(page, "Hiring").getByRole("button", { name: "Restore" }).click();
  await expect(rowOf(page, "Hiring").getByRole("alert")).toHaveText(
    "The 30-day window to restore this board has passed. It can no longer be restored.",
  );
  await expect(rowOf(page, "Hiring")).toBeVisible();
});

test("?spaceId= narrows the deleted list to one Space, and Show every board widens it again", async ({
  page,
}) => {
  await open(page);
  await page.goto(
    `${WS}/archived-boards?tab=deleted&spaceId=mock-space-people`,
  );

  await expect(page.getByTestId("retired-boards-space-scope")).toBeVisible();
  await expect(rowOf(page, "Hiring")).toBeVisible();
  await expect(rowOf(page, "Q3 plan")).toHaveCount(0);

  await page.getByRole("button", { name: "Show every board" }).click();
  await expect(rowOf(page, "Q3 plan")).toBeVisible();
  await expect(rowOf(page, "Hiring")).toBeVisible();
  await expect(page.getByTestId("retired-boards-space-scope")).toHaveCount(0);
  await expect(page).not.toHaveURL(/spaceId=/);
});

test("a board deleted from its settings lands in the Deleted tab and is restored from there", async ({
  page,
}) => {
  await open(page);
  await page.goto(`${ROADMAP}/settings?from=table&section=general`);
  await page
    .getByRole("textbox", { name: /Type the board name/ })
    .fill("Product Roadmap");
  await page.getByRole("button", { name: "Delete board" }).click();
  await expect(page).toHaveURL(/archived-boards\?tab=deleted$/);

  await rowOf(page, "Product Roadmap")
    .getByRole("button", { name: "Restore" })
    .click();
  await expect(page.getByText("The board was restored.")).toBeVisible();
  await expect(rowOf(page, "Product Roadmap")).toHaveCount(0);
});
