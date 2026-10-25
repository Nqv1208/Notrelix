import { expect, test, type Page } from "@playwright/test";
import {
  addItemToLane,
  openBoard,
  realUsers,
  signIn,
} from "./support/real-session";

test.use({ viewport: { width: 1440, height: 900 } });

const apiUrl = process.env.VITE_API_URL ?? "http://127.0.0.1:58000/api/v1";

interface FullBoard {
  readonly lists: readonly {
    readonly title: string;
    readonly boardItems: readonly { readonly name: string }[];
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

test("a workspace admin creates a rule on a board, turns it on, and the next item created runs it and moves the item", async ({
  page,
}) => {
  await signIn(page, realUsers.workspaceAdmin);
  const { workspace, board } = await openBoard(
    page,
    "Workspace 3",
    "Engineering Tasks",
  );
  const unique = Date.now().toString(36);

  // The item the rule will move, added on the board.
  const target = `Rule target ${unique}`;
  expect(await addItemToLane(page, "Backlog", target)).toBeLessThan(300);
  await expect.poll(() => groupOf(page, board.id, target)).not.toBeNull();
  const startsIn = (await groupOf(page, board.id, target))!;
  const destination = startsIn === "Done" ? "Backlog" : "Done";

  // Settings > Automations: create the rule.
  await page.goto(
    `/workspaces/${workspace.id}/boards/${board.id}/settings?section=automations`,
  );
  await expect(
    page.getByRole("heading", { level: 2, name: "Automations" }),
  ).toBeVisible();
  const ruleName = `Move ${unique}`;
  await page.getByRole("button", { name: "New rule" }).click();
  const form = page.getByRole("form", { name: "New rule" });
  await form.getByLabel("Rule name").fill(ruleName);
  await form.getByLabel("Item to move").selectOption({ label: target });
  await form.getByLabel("To the group").selectOption({ label: destination });
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

  // The trigger: another item is created on the board.
  await page.goto(`/workspaces/${workspace.id}/boards/${board.id}`);
  await expect(
    page.getByRole("heading", { level: 1, name: board.title }),
  ).toBeVisible();
  expect(
    await addItemToLane(page, "To Do", `Rule trigger ${unique}`),
  ).toBeLessThan(300);

  // The worker runs the rule: the item changes group, and the run is listed.
  await expect
    .poll(() => groupOf(page, board.id, target), { timeout: 30_000 })
    .toBe(destination);

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
