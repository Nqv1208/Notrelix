import { expect, test, type Page } from "@playwright/test";
import {
  apiRead,
  apiWrite,
  openBoard,
  realUsers,
  signIn,
} from "./support/real-session";

test.use({ viewport: { width: 1440, height: 900 } });

const apiUrl = process.env.VITE_API_URL ?? "http://127.0.0.1:58000/api/v1";

interface FullBoard {
  readonly lists: readonly {
    readonly id: string;
    readonly title: string;
    readonly boardItems: readonly {
      readonly id: string;
      readonly name: string;
      readonly version?: number;
    }[];
  }[];
}

/** The title of the group the board lists an item under. */
async function groupOf(page: Page, boardId: string, itemTitle: string) {
  const response = await page.request.get(`${apiUrl}/boards/${boardId}/full`);
  expect(response.ok()).toBe(true);
  const board = (await response.json()) as FullBoard;
  return (
    board.lists.find((list) =>
      list.boardItems.some((item) => item.name === itemTitle),
    )?.title ?? null
  );
}

test("a workspace admin creates a board rule that moves an item to another group when it is moved into one, and the item follows it", async ({
  page,
}) => {
  await signIn(page, realUsers.workspaceAdmin);
  const { workspace, board } = await openBoard(
    page,
    "Workspace 3",
    "Engineering Tasks",
  );
  const unique = Date.now().toString(36);
  const full = await apiRead<FullBoard>(page, `/boards/${board.id}/full`);
  const watched = full.lists.find((list) => list.title === "Done")!;
  const destination = full.lists.find((list) => list.title === "Backlog")!;
  const start = full.lists.find((list) => list.title === "To Do")!;

  // Settings > Automations: "when an item is moved to Done, move it to Backlog".
  await page.goto(
    `/workspaces/${workspace.id}/boards/${board.id}/settings?section=automations`,
  );
  await expect(
    page.getByRole("heading", { level: 2, name: "Automations" }),
  ).toBeVisible();
  const ruleName = `Bounce ${unique}`;
  await page.getByRole("button", { name: "New rule" }).click();
  const form = page.getByRole("form", { name: "New rule" });
  await form.getByLabel("Rule name").fill(ruleName);
  await form
    .getByLabel("Moved to the group")
    .selectOption({ label: watched.title });
  await form
    .getByLabel("Move it to the group")
    .selectOption({ label: destination.title });
  const created = page.waitForResponse(
    (response) =>
      response.request().method() === "POST" &&
      /\/workspaces\/[^/]+\/automations$/.test(response.url()),
  );
  await form.getByRole("button", { name: "Create rule" }).click();
  expect((await created).status()).toBe(201);

  // A new rule is off; turning it on is one write.
  const toggle = page.getByRole("switch", {
    name: `Turn on the rule ${ruleName}`,
  });
  await expect(toggle).toHaveAttribute("aria-checked", "false");
  const enabled = page.waitForResponse(
    (response) =>
      response.request().method() === "PATCH" &&
      /\/automations\/[^/]+\/enabled$/.test(response.url()),
  );
  await toggle.click();
  expect((await enabled).status()).toBeLessThan(300);
  await expect(toggle).toHaveAttribute("aria-checked", "true");

  // The trigger: an item of the board is moved into Done.
  const title = `Rule target ${unique}`;
  const made = await apiWrite<{ id: string }>(
    page,
    "POST",
    `/boards/${board.id}/items`,
    { groupId: start.id, title },
  );
  expect(made.status).toBeLessThan(300);
  const itemId = made.data!.id;
  const fresh = await apiRead<FullBoard>(page, `/boards/${board.id}/full`);
  const version =
    fresh.lists
      .flatMap((list) => list.boardItems)
      .find((item) => item.id === itemId)?.version ?? 1;
  const moved = await apiWrite(page, "POST", `/board-items/${itemId}/move`, {
    groupId: watched.id,
    expectedVersion: version,
  });
  expect(moved.status).toBeLessThan(300);

  // The worker runs the rule: the item ends in the rule's group, and the run is listed.
  await expect
    .poll(() => groupOf(page, board.id, title), { timeout: 30_000 })
    .toBe(destination.title);

  await page.goto(
    `/workspaces/${workspace.id}/boards/${board.id}/settings?section=automations`,
  );
  await expect(page.getByText(ruleName, { exact: true })).toBeVisible();
  const card = page.getByRole("listitem").filter({ hasText: ruleName });
  await card.getByRole("button", { name: "Run history" }).click();
  await expect(
    card.getByRole("list", { name: "Run history" }).getByText("Succeeded"),
  ).toBeVisible();
});
