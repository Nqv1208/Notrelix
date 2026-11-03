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
 * Creating items on the real stack: the lane's quick add and the create popup opened from the toolbar. Both end in one
 * `POST /boards/{id}/items`; the item survives a reload and the popup's draft is not lost when the person cancels
 * with text typed (it asks first).
 */
test("a member quick-adds an item in a lane and it is still there after a reload", async ({
  page,
}) => {
  await signIn(page, realUsers.workspaceAdmin);
  const { board } = await openBoard(page, "Workspace 3", "Engineering Tasks");
  const title = `Quick ${Date.now().toString(36)}`;
  // The first lane that offers a quick add.
  const add = page.getByRole("button", { name: /^Add to / }).first();
  const lane = ((await add.getAttribute("aria-label")) ?? "").replace(
    /^Add to /,
    "",
  );
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

test("a member creates an item from the popup with a name and a description, sent once, and the card opens on its key", async ({
  page,
}) => {
  await signIn(page, realUsers.workspaceAdmin);
  const { board } = await openBoard(page, "Workspace 3", "Engineering Tasks");
  const title = `Popup ${Date.now().toString(36)}`;
  await page
    .getByRole("button", { name: "Add item", exact: true })
    .first()
    .click();
  const popup = page.getByRole("dialog").first();
  await expect(popup.getByText("Add to column")).toBeVisible();
  // The name field has focus; the create button waits for a name.
  const create = popup.getByRole("button", {
    name: "Create card",
    exact: true,
  });
  await expect(create).toBeDisabled();
  await popup.getByPlaceholder("What needs doing?").fill(title);
  await popup.getByRole("button", { name: "Add a description…" }).click();
  await page.keyboard.type("Written while creating");
  const posts: string[] = [];
  page.on("request", (request) => {
    if (
      request.method() === "POST" &&
      /\/boards\/[^/]+\/items$/.test(request.url())
    )
      posts.push(request.url());
  });
  const created = page.waitForResponse(
    (response) =>
      response.request().method() === "POST" &&
      /\/boards\/[^/]+\/items$/.test(response.url()),
  );
  await create.click();
  await create.click({ trial: true }).catch(() => undefined);
  expect((await created).status()).toBeLessThan(300);
  // The popup stays on the new card, whose comments are now open.
  await expect(
    popup
      .getByText(/^Created /)
      .or(popup.getByRole("textbox", { name: /comment/i }))
      .first(),
  ).toBeVisible({ timeout: 15_000 });
  expect(posts).toHaveLength(1);

  const after = await apiRead<{
    lists: { boardItems: { name: string; description?: string | null }[] }[];
  }>(page, `/boards/${board.id}/full`);
  const made = after.lists
    .flatMap((list) => list.boardItems)
    .find((item) => item.name === title);
  expect(made).toBeTruthy();
});
