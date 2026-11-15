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

test("a member follows an item from its card, finds it under Following in My work, and stops following", async ({
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

  // An item has no page of its own since its link opens the card popup on the board, where Follow
  // and Unfollow sit in the card actions menu.
  const cardAction = async (name: RegExp) => {
    await page
      .getByRole("dialog", { name: `Followed ${unique}` })
      .getByRole("button", { name: "Card actions" })
      .click();
    await page.getByRole("menuitem", { name }).click();
  };
  await page.goto(
    `/workspaces/${workspace.id}/boards/${board.id}?item=${itemId}`,
  );
  // The creator follows the item from the start; stopping and following again
  // are both written from the card.
  const unfollowedFirst = page.waitForResponse(
    (response) =>
      response.request().method() === "DELETE" &&
      response.url().includes(`/board-items/${itemId}/following`),
  );
  await cardAction(/^Unfollow card/);
  expect((await unfollowedFirst).status()).toBeLessThan(300);
  const followed = page.waitForResponse(
    (response) =>
      response.request().method() === "PUT" &&
      response.url().includes(`/board-items/${itemId}/following`),
  );
  await cardAction(/^Follow card/);
  expect((await followed).status()).toBeLessThan(300);

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

  // Stopping from the card removes it.
  await page.goto(
    `/workspaces/${workspace.id}/boards/${board.id}?item=${itemId}`,
  );
  const unfollowed = page.waitForResponse(
    (response) =>
      response.request().method() === "DELETE" &&
      response.url().includes(`/board-items/${itemId}/following`),
  );
  await cardAction(/^Unfollow card/);
  expect((await unfollowed).status()).toBeLessThan(300);
  expect(
    (await apiRead<Following>(page, `/board-items/${itemId}/following`))
      .following,
  ).toBe(false);
});
