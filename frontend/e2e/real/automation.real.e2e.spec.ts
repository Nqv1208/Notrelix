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

interface FullBoardColumns {
  readonly columns: readonly {
    readonly id: string;
    readonly fieldType: string;
    readonly options?: readonly {
      readonly id: string;
      readonly name: string;
      readonly category?: string;
    }[];
  }[];
}

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

test("a board rule runs several actions in order on the item that triggered it, then it is edited into a new version and deleted", async ({
  browser,
  page,
}) => {
  test.setTimeout(120_000);
  const unique = Date.now().toString(36);

  // The person the rule assigns and notifies.
  const context = await browser.newContext();
  const colleague = await context.newPage();
  await signIn(colleague, realUsers.admin);
  const colleagueWorkspace = await colleague.request
    .get(`${apiUrl}/workspaces`)
    .then(async (response) => {
      const list = (await response.json()) as { id: string; name: string }[];
      return list.find((candidate) => candidate.name === "Workspace 3")!;
    });
  const colleagueId = (
    await apiRead<{ userId: string }>(
      colleague,
      `/workspaces/${colleagueWorkspace.id}/members/me`,
    )
  ).userId;

  await signIn(page, realUsers.workspaceAdmin);
  const { workspace, board } = await openBoard(
    page,
    "Workspace 3",
    "Engineering Tasks",
  );
  // Rules left by an earlier run would also fire on the item below.
  const leftovers = await apiRead<{ id: string; name: string }[]>(
    page,
    `/workspaces/${workspace.id}/automations?boardId=${board.id}`,
  );
  for (const rule of leftovers.filter((r) => r.name.startsWith("Hand over"))) {
    const removed = await apiWrite(
      page,
      "DELETE",
      `/workspaces/${workspace.id}/automations/${rule.id}?boardId=${board.id}`,
    );
    expect(removed.status).toBeLessThan(300);
  }
  const full = await apiRead<FullBoard & FullBoardColumns>(
    page,
    `/boards/${board.id}/full`,
  );
  const watched = full.lists.find((list) => list.title === "Done")!;
  const start = full.lists.find((list) => list.title === "To Do")!;
  const status = full.columns.find(
    (column) => column.fieldType.toLowerCase() === "status",
  )!;
  const target =
    status.options!.find(
      (option) => option.category?.toLowerCase() === "inprogress",
    ) ??
    status.options!.find(
      (option) => option.category?.toLowerCase() !== "done",
    )!;

  // Settings > Automations: when an item is moved to Done, set its status, assign a person and notify them.
  await page.goto(
    `/workspaces/${workspace.id}/boards/${board.id}/settings?section=automations`,
  );
  const ruleName = `Hand over ${unique}`;
  await page.getByRole("button", { name: "New rule" }).click();
  const form = page.getByRole("form", { name: "New rule" });
  await form.getByLabel("Rule name").fill(ruleName);
  await form
    .getByLabel("Moved to the group")
    .selectOption({ label: watched.title });
  const action = (index: number) =>
    form.getByRole("group", { name: `Action ${index}` });
  await action(1)
    .getByLabel("Action 1", { exact: true })
    .selectOption({ label: "Change its status" });
  await action(1).getByLabel("Status").selectOption({ label: target.name });
  await form.getByRole("button", { name: "Add action" }).click();
  await action(2)
    .getByLabel("Action 2", { exact: true })
    .selectOption({ label: "Assign a person" });
  await action(2).getByLabel("Person").selectOption({ value: colleagueId });
  await form.getByRole("button", { name: "Add action" }).click();
  await action(3)
    .getByLabel("Action 3", { exact: true })
    .selectOption({ label: "Notify a person in the app" });
  await action(3).getByLabel("Person").selectOption({ value: colleagueId });
  await action(3)
    .getByLabel("Message (optional)")
    .fill(`Handed over ${unique}`);
  const created = page.waitForResponse(
    (response) =>
      response.request().method() === "POST" &&
      /\/workspaces\/[^/]+\/automations$/.test(response.url()),
  );
  await form.getByRole("button", { name: "Create rule" }).click();
  expect((await created).status()).toBe(201);

  const toggle = page.getByRole("switch", {
    name: `Turn on the rule ${ruleName}`,
  });
  const enabled = page.waitForResponse(
    (response) =>
      response.request().method() === "PATCH" &&
      /\/automations\/[^/]+\/enabled$/.test(response.url()),
  );
  await toggle.click();
  expect((await enabled).status()).toBeLessThan(300);
  await expect(toggle).toHaveAttribute("aria-checked", "true");

  // The trigger: an item of the board is moved into Done.
  const title = `Handed ${unique}`;
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
  expect(
    (
      await apiWrite(page, "POST", `/board-items/${itemId}/move`, {
        groupId: watched.id,
        expectedVersion: version,
      })
    ).status,
  ).toBeLessThan(300);

  // The colleague is told in the app.
  const inbox = () =>
    apiRead<{ items: { type?: string; target?: { id?: string } }[] }>(
      colleague,
      `/workspaces/${colleagueWorkspace.id}/inbox?limit=50`,
    );
  await expect
    .poll(
      async () =>
        (await inbox()).items.filter(
          (entry) =>
            entry.target?.id === itemId &&
            entry.type === "work.item.automation",
        ).length,
      { timeout: 60_000 },
    )
    .toBe(1);

  // The history shows the run on version 1 with its three actions done.
  await page.goto(
    `/workspaces/${workspace.id}/boards/${board.id}/settings?section=automations`,
  );
  const card = page.getByRole("listitem").filter({ hasText: ruleName });
  await card.getByRole("button", { name: "Run history" }).click();
  const history = card.getByRole("list", { name: "Run history" });
  await expect(history.getByText("Succeeded")).toBeVisible({ timeout: 30_000 });
  await expect(history.getByText("Version 1")).toBeVisible();
  await expect(history.getByText("Done")).toHaveCount(3);

  // Editing saves a new version.
  await card.getByRole("button", { name: `Edit the rule ${ruleName}` }).click();
  const edit = page.getByRole("form", { name: "Edit rule" });
  await edit.getByLabel("Rule name").fill(`${ruleName} v2`);
  const updated = page.waitForResponse(
    (response) =>
      response.request().method() === "PUT" &&
      /\/workspaces\/[^/]+\/automations\/[^/]+$/.test(response.url()),
  );
  await edit.getByRole("button", { name: "Save changes" }).click();
  expect((await updated).status()).toBeLessThan(300);
  const edited = page
    .getByRole("listitem")
    .filter({ hasText: `${ruleName} v2` });
  await expect(edited.getByText("Version 2")).toBeVisible();

  // Deleting removes it from the list.
  await edited
    .getByRole("button", { name: `Delete the rule ${ruleName} v2` })
    .click();
  const deleted = page.waitForResponse(
    (response) =>
      response.request().method() === "DELETE" &&
      /\/workspaces\/[^/]+\/automations\/[^/]+/.test(response.url()),
  );
  await page.getByRole("button", { name: "Delete rule" }).click();
  expect((await deleted).status()).toBeLessThan(300);
  await expect(page.getByText(`${ruleName} v2`)).toHaveCount(0);
  await context.close();
});
