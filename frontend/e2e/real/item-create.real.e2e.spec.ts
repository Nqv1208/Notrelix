import { expect, test } from "@playwright/test";
import {
  addItemToLane,
  apiRead,
  openBoard,
  realUsers,
  signIn,
} from "./support/real-session";

test.use({ viewport: { width: 1440, height: 900 } });

/**
 * Creating an item on the real stack: the lane's quick add ends in one `POST /boards/{id}/items` and the item survives a
 * reload. (The create popup journey is not here yet: its post-create state did not settle in two runs.)
 */
test("a member quick-adds an item in a lane and it is still there after a reload", async ({
  page,
}) => {
  await signIn(page, realUsers.workspaceAdmin);
  const { board } = await openBoard(page, "Workspace 3", "Engineering Tasks");
  const title = `Quick ${Date.now().toString(36)}`;
  // The first lane that offers a quick add.
  const add = page.getByRole("button", { name: /^Add to / }).first();
  const lane = ((await add.innerText()) ?? "").trim().replace(/^Add to /, "");
  expect(lane).not.toBe("");
  expect(await addItemToLane(page, lane, title)).toBeLessThan(300);
  await expect(
    page.getByRole("article").filter({ hasText: title }),
  ).toBeVisible();
  await page.reload();
  const names = (
    await apiRead<{ lists: { boardItems: { name: string }[] }[] }>(
      page,
      `/boards/${board.id}/full`,
    )
  ).lists.flatMap((list) => list.boardItems.map((item) => item.name));
  expect(names).toContain(title);
});
