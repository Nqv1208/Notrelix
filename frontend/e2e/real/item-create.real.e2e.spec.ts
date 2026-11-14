import { expect, test } from "@playwright/test";
import {
  addItemToLane,
  apiRead,
  apiWrite,
  openBoard,
  realUsers,
  signIn,
} from "./support/real-session";

test.use({ viewport: { width: 1440, height: 900 } });

/**
 * Creating an item on the real stack: the lane's quick add ends in one `POST /boards/{id}/items` and the item survives a
 * reload. The create popup keeps a queued link and file until the item exists, says where it was created, and the item
 * reads back with them; a lane over its WIP limit still takes the item and says so.
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

interface Full {
  lists: { id: string; boardItems: { id: string; name: string }[] }[];
}

test("the create popup writes a queued item link and a file once the item exists and says where it was created", async ({
  page,
}) => {
  test.setTimeout(120_000);
  await signIn(page, realUsers.workspaceAdmin);
  const { board } = await openBoard(page, "Workspace 3", "Engineering Tasks");
  const unique = Date.now().toString(36);
  const full = await apiRead<Full>(page, `/boards/${board.id}/full`);
  const target = (
    await apiWrite<{ id: string }>(page, "POST", `/boards/${board.id}/items`, {
      groupId: full.lists[0]!.id,
      title: `Link target ${unique}`,
    })
  ).data!;
  const targetKey = (
    await apiRead<{ itemKey: string }>(page, `/board-items/${target.id}`)
  ).itemKey;
  await page.reload();

  await page
    .getByRole("button", { name: "Add item", exact: true })
    .first()
    .click();
  const popup = page.getByRole("dialog").first();
  const title = `Created in popup ${unique}`;
  await popup.getByRole("textbox").first().fill(title);

  // A queued item link (Related), shown in the popup before anything is sent.
  await popup.getByRole("button", { name: "Add link", exact: true }).click();
  await page.getByLabel("Item on this board").fill(targetKey);
  await page
    .getByRole("option", { name: new RegExp(targetKey) })
    .first()
    .click();
  await page
    .getByRole("button", { name: "Add link", exact: true })
    .last()
    .click();

  // A queued file.
  await popup.getByRole("button", { name: "Add an attachment" }).click();
  await page
    .locator("input[type=file]")
    .first()
    .setInputFiles({
      name: `plan-${unique}.txt`,
      mimeType: "text/plain",
      buffer: Buffer.from("plan"),
    });
  await expect(popup.getByText(`plan-${unique}.txt`)).toBeVisible();

  const created = page.waitForResponse(
    (response) =>
      response.request().method() === "POST" &&
      /\/boards\/[^/]+\/items$/.test(response.url()),
  );
  await popup.getByRole("button", { name: "Create card" }).click();
  expect((await created).status()).toBeLessThan(300);
  await expect(page.getByText(/^Created \S+ in /)).toBeVisible({
    timeout: 20_000,
  });

  const after = await apiRead<Full>(page, `/boards/${board.id}/full`);
  const item = after.lists
    .flatMap((list) => list.boardItems)
    .find((candidate) => candidate.name === title)!;
  expect(item).toBeDefined();
  await expect
    .poll(
      async () =>
        JSON.stringify(
          await apiRead<unknown>(page, `/board-items/${item.id}/links`),
        ),
      { timeout: 20_000 },
    )
    .toContain(target.id);
  await expect
    .poll(
      async () =>
        JSON.stringify(
          await apiRead<unknown>(page, `/board-items/${item.id}/attachments`),
        ),
      { timeout: 20_000 },
    )
    .toContain(`plan-${unique}.txt`);
});

test("a quick add over a lane's WIP limit still creates the item and the column warns", async ({
  page,
}) => {
  await signIn(page, realUsers.workspaceAdmin);
  await openBoard(page, "Workspace 3", "Engineering Tasks");
  const add = page.getByRole("button", { name: /^Add to / }).first();
  const lane = ((await add.innerText()) ?? "").trim().replace(/^Add to /, "");
  const region = page.getByRole("region", { name: lane, exact: true });
  // A limit of one: the column is at or over it already.
  await region
    .getByRole("button", { name: `Options for column ${lane}` })
    .click();
  await page.getByRole("menuitem", { name: "Set item limit" }).click();
  await page.getByLabel(`Item limit of ${lane}`).fill("1");
  await page.getByRole("button", { name: "Save limit" }).click();

  const title = `Over limit ${Date.now().toString(36)}`;
  expect(await addItemToLane(page, lane, title)).toBeLessThan(300);
  await expect(
    page.getByRole("article").filter({ hasText: title }),
  ).toBeVisible();
  await expect(region.locator("[data-wip-note=over]")).toContainText(
    /over the limit of 1/,
  );
  // Leave the board as it was.
  await region
    .getByRole("button", { name: `Options for column ${lane}` })
    .click();
  await page.getByRole("menuitem", { name: "Set item limit" }).click();
  await page.getByLabel(`Item limit of ${lane}`).fill("");
  await page.getByRole("button", { name: "Remove limit" }).click();
  await expect(region.getByText("Limit of 1 items")).toHaveCount(0);
});
