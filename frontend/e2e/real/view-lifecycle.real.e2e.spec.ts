import { expect, test, type Page } from "@playwright/test";
import {
  apiRead,
  apiWrite,
  openBoard,
  realUsers,
  signIn,
} from "./support/real-session";

test.use({ viewport: { width: 1440, height: 900 } });
test.setTimeout(150_000);

/**
 * The saved views of a board on the real stack, driven from the tab strip: an editor sets the default, archives and
 * restores a view and deletes the default view by naming its successor; a stale version is refused and changes
 * nothing (the strip says so and the server holds the view as it was); the shared view's PUT upsert names the version it
 * read; a private view carries its badge for its creator and is absent from another member's list.
 */
interface SavedView {
  readonly id: string;
  readonly name: string;
  readonly visibility: string;
  readonly isDefault: boolean;
  readonly version: number;
}

const answered = (page: Page, method: string, route: RegExp) =>
  page.waitForResponse(
    (response) =>
      response.request().method() === method &&
      route.test(response.url()) &&
      response.status() !== 403,
  );

async function makeView(
  page: Page,
  boardId: string,
  name: string,
  visibility: "Board" | "Private" = "Board",
) {
  const made = await apiWrite<{ id: string }>(
    page,
    "POST",
    `/boards/${boardId}/views`,
    { name, viewMode: "Table", config: "{}", visibility },
  );
  expect(made.status, `create view ${name}`).toBeLessThan(300);
  return made.data!.id;
}

const menuOf = (page: Page, name: string) =>
  page.getByRole("button", { name: `Options of the view ${name}` });

test("the tab strip sets the default, archives and restores a view, deletes the default with its successor, and a stale version changes nothing", async ({
  page,
}) => {
  await signIn(page, realUsers.workspaceAdmin);
  const { workspace, board } = await openBoard(
    page,
    "Workspace 3",
    "Engineering Tasks",
  );
  const unique = Date.now().toString(36);
  const first = `Alpha ${unique}`;
  const second = `Beta ${unique}`;
  const listRoute = `/boards/${board.id}/views/saved`;
  const firstId = await makeView(page, board.id, first);
  const secondId = await makeView(page, board.id, second);
  const viewOf = async (id: string) =>
    (await apiRead<SavedView[]>(page, listRoute)).find(
      (view) => view.id === id,
    );
  const open = (id: string) =>
    page.goto(
      `/workspaces/${workspace.id}/boards/${board.id}?view=table&savedView=${id}`,
    );

  // Set as default: the server holds exactly this one as the default.
  await open(secondId);
  await expect(page.getByRole("tab", { name: second })).toBeVisible();
  await menuOf(page, second).click();
  const defaulted = answered(page, "POST", /\/views\/[^/]+\/set-default$/);
  await page.getByRole("menuitem", { name: "Set as default view" }).click();
  expect((await defaulted).status()).toBeLessThan(300);
  const afterDefault = await apiRead<SavedView[]>(page, listRoute);
  expect(
    afterDefault.filter((view) => view.isDefault).map((v) => v.id),
  ).toEqual([secondId]);

  // A stale version is refused: someone else wrote the view after this page read it.
  const current = (await viewOf(firstId))!;
  const bumped = await apiWrite(
    page,
    "PATCH",
    `/boards/${board.id}/views/${firstId}`,
    { configJson: '{"hidden":[]}', expectedVersion: current.version },
  );
  expect(bumped.status).toBeLessThan(300);
  await page.getByRole("tab", { name: first }).click();
  await menuOf(page, first).click();
  await page.getByRole("menuitem", { name: "Archive view…" }).click();
  // The page already took the version the other write made (realtime refreshes it), so the request it sends is
  // made stale on the wire: it names the version before the other person's write.
  await page.route(/\/views\/[^/]+\/archive$/, async (route) => {
    const sent = route.request().postDataJSON() as { expectedVersion: number };
    await route.continue({
      postData: JSON.stringify({ ...sent, expectedVersion: current.version }),
    });
  });
  const refused = answered(page, "POST", /\/views\/[^/]+\/archive$/);
  await page
    .getByRole("alertdialog")
    .getByRole("button", { name: "Archive view", exact: true })
    .click();
  expect((await refused).status()).toBe(412);
  await expect(page.getByRole("alert")).toContainText(
    "Someone changed this view first",
  );
  expect((await viewOf(firstId))!.version).toBe(current.version + 1);
  await page.unroute(/\/views\/[^/]+\/archive$/);
  await page.keyboard.press("Escape");

  // Archive with the version it has read now, then restore it from the same menu.
  await page.reload();
  await menuOf(page, first).click();
  await page.getByRole("menuitem", { name: "Archive view…" }).click();
  const archived = answered(page, "POST", /\/views\/[^/]+\/archive$/);
  await page
    .getByRole("alertdialog")
    .getByRole("button", { name: "Archive view", exact: true })
    .click();
  expect((await archived).status()).toBeLessThan(300);
  await expect(page.getByRole("tab", { name: first })).toHaveCount(0);
  expect(await viewOf(firstId)).toBeUndefined();
  await menuOf(page, second).click();
  const restored = answered(page, "POST", /\/views\/[^/]+\/unarchive$/);
  await page.getByRole("menuitem", { name: `Restore ${first}` }).click();
  expect((await restored).status()).toBeLessThan(300);
  await expect(page.getByRole("tab", { name: first })).toBeVisible();

  // The default view leaves only with its successor named; nobody sees a "gone" notice.
  await open(secondId);
  await menuOf(page, second).click();
  await page.getByRole("menuitem", { name: "Delete view…" }).click();
  const dialog = page.getByRole("alertdialog");
  await dialog.getByRole("combobox", { name: "Next default view" }).click();
  await page.getByRole("option", { name: first }).click();
  const deleted = answered(page, "DELETE", /\/views\/[^/]+/);
  await dialog
    .getByRole("button", { name: "Delete view", exact: true })
    .click();
  expect((await deleted).status()).toBeLessThan(300);
  await expect(page.getByRole("tab", { name: second })).toHaveCount(0);
  await expect(
    page.getByText("The view you opened no longer exists"),
  ).toHaveCount(0);
  expect(await viewOf(secondId)).toBeUndefined();
  expect(await viewOf(firstId)).toMatchObject({ isDefault: true });
});

test("the shared view's PUT upsert names the version it read: a stale or unnamed save is refused and the view is untouched", async ({
  page,
}) => {
  await signIn(page, realUsers.workspaceAdmin);
  const { board } = await openBoard(page, "Workspace 3", "Engineering Tasks");
  const listRoute = `/boards/${board.id}/views/saved`;
  await makeView(page, board.id, `Put ${Date.now().toString(36)}`);
  const shared = async () =>
    (await apiRead<SavedView[]>(page, listRoute)).find(
      (view) => view.isDefault && view.visibility === "Board",
    )!;
  const before = await shared();
  const body = (expectedVersion?: number) => ({
    viewMode: "Table",
    config: '{"hidden":["due"]}',
    ...(expectedVersion === undefined ? {} : { expectedVersion }),
  });

  const unnamed = await apiWrite(
    page,
    "PUT",
    `/boards/${board.id}/views`,
    body(),
  );
  const stale = await apiWrite(
    page,
    "PUT",
    `/boards/${board.id}/views`,
    body(before.version + 5),
  );
  expect([unnamed.status, stale.status]).toEqual([412, 412]);
  expect((await shared()).version).toBe(before.version);

  const saved = await apiWrite(
    page,
    "PUT",
    `/boards/${board.id}/views`,
    body(before.version),
  );
  expect(saved.status).toBeLessThan(300);
  expect((await shared()).version).toBeGreaterThan(before.version);
});

test("a private view carries its badge for its creator and is absent from another member's list", async ({
  browser,
  page,
}) => {
  await signIn(page, realUsers.workspaceAdmin);
  const { workspace, board } = await openBoard(
    page,
    "Workspace 3",
    "Engineering Tasks",
  );
  const name = `Mine ${Date.now().toString(36)}`;
  const id = await makeView(page, board.id, name, "Private");
  await page.goto(
    `/workspaces/${workspace.id}/boards/${board.id}?view=table&savedView=${id}`,
  );
  await expect(
    page.getByRole("tab", { name }).getByLabel("Private view"),
  ).toBeVisible();

  const other = await browser.newContext();
  const otherPage = await other.newPage();
  try {
    await signIn(otherPage, realUsers.workspaceOwner);
    const theirs = await apiRead<SavedView[]>(
      otherPage,
      `/boards/${board.id}/views/saved`,
    );
    expect(theirs.map((view) => view.id)).not.toContain(id);
  } finally {
    await other.close();
  }
});
