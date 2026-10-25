import { expect, test } from "@playwright/test";
import {
  apiRead,
  apiWrite,
  openBoard,
  realUsers,
  signIn,
} from "./support/real-session";

test.use({ viewport: { width: 1440, height: 900 } });

interface Following {
  readonly following: boolean;
  readonly followers: readonly { userId: string; name: string }[];
}

test("a member follows an item from its page, finds it under Following in My work, and stops following", async ({
  page,
}) => {
  await signIn(page, realUsers.workspaceAdmin);
  const { workspace, board } = await openBoard(
    page,
    "Workspace 3",
    "Engineering Tasks",
  );
  const unique = Date.now().toString(36);
  const groupId = (
    await apiRead<{ lists: { id: string }[] }>(page, `/boards/${board.id}/full`)
  ).lists[0]!.id;
  const created = await apiWrite<{ id: string }>(
    page,
    "POST",
    `/boards/${board.id}/items`,
    { groupId, title: `Followed ${unique}` },
  );
  expect(created.status).toBeLessThan(300);
  const itemId = created.data!.id;

  await page.goto(
    `/workspaces/${workspace.id}/boards/${board.id}/items/${itemId}`,
  );
  const followed = page.waitForResponse(
    (response) =>
      response.request().method() === "PUT" &&
      response.url().includes(`/board-items/${itemId}/following`),
  );
  await page.getByRole("button", { name: "Follow", exact: true }).click();
  expect((await followed).status()).toBeLessThan(300);
  await expect(
    page.getByRole("button", { name: "Following", exact: true }),
  ).toBeVisible();

  const state = await apiRead<Following>(
    page,
    `/board-items/${itemId}/following`,
  );
  expect(state.following).toBe(true);
  expect(state.followers).toHaveLength(1);

  // My work lists it with the following relation.
  const mine = await apiRead<{
    items: { id: string; relations: string[] }[];
  }>(page, `/workspaces/${workspace.id}/my-work/extended`);
  expect(mine.items.find((item) => item.id === itemId)?.relations).toEqual([
    "following",
  ]);
  await page.goto(`/workspaces/${workspace.id}/my-work`);
  await page.getByRole("tab", { name: /Following/ }).click();
  await expect(page.getByText(`Followed ${unique}`)).toBeVisible();

  // Stopping from the item page removes it.
  await page.goto(
    `/workspaces/${workspace.id}/boards/${board.id}/items/${itemId}`,
  );
  const unfollowed = page.waitForResponse(
    (response) =>
      response.request().method() === "DELETE" &&
      response.url().includes(`/board-items/${itemId}/following`),
  );
  await page.getByRole("button", { name: "Following", exact: true }).click();
  expect((await unfollowed).status()).toBeLessThan(300);
  expect(
    (await apiRead<Following>(page, `/board-items/${itemId}/following`))
      .following,
  ).toBe(false);
});
