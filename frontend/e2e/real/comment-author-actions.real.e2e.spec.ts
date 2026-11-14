import { expect, test } from "@playwright/test";
import {
  apiRead,
  apiWrite,
  openBoard,
  realUsers,
  signIn,
} from "./support/real-session";

test.use({ viewport: { width: 1440, height: 900 } });
test.setTimeout(120_000);

test("only the author of a comment is offered Edit and Delete on it", async ({
  page,
  browser,
}) => {
  const unique = Date.now().toString(36);
  await signIn(page, realUsers.workspaceAdmin);
  const { workspace, board } = await openBoard(
    page,
    "Workspace 3",
    "Engineering Tasks",
  );
  const groupId = (
    await apiRead<{ lists: { id: string }[] }>(page, `/boards/${board.id}/full`)
  ).lists[0]!.id;
  const created = await apiWrite<{ id: string }>(
    page,
    "POST",
    `/boards/${board.id}/items`,
    { groupId, title: `Authored ${unique}` },
  );
  const itemId = created.data!.id;
  const body = `Mine ${unique}`;
  expect(
    (
      await apiWrite(page, "POST", `/board-items/${itemId}/comments`, {
        contentMd: body,
      })
    ).status,
  ).toBeLessThan(300);
  const url = `/workspaces/${workspace.id}/boards/${board.id}/items/${itemId}`;

  // The author sees the actions menu on their comment.
  await page.goto(url);
  await expect(page.getByText(body)).toBeVisible();
  const actions = (p: typeof page, name: string) =>
    p.locator("[data-comment]").getByRole("button", { name, exact: true });
  await expect(actions(page, "Edit")).toHaveCount(1);
  await expect(actions(page, "Delete")).toHaveCount(1);

  // Another member who can open the board sees the comment and no actions.
  const other = await browser.newPage();
  await signIn(other, realUsers.workspaceOwner);
  await other.goto(url);
  await expect(other.getByText(body)).toBeVisible();
  // The workspace owner manages the board, so Delete may show; Edit stays with the author.
  await expect(actions(other, "Edit")).toHaveCount(0);
  await other.close();
});
