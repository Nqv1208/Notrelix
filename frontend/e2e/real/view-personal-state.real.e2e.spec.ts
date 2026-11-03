import { expect, test, type Page } from "@playwright/test";
import {
  apiRead,
  apiWrite,
  openBoard,
  realUsers,
  signIn,
} from "./support/real-session";

test.use({ viewport: { width: 1440, height: 900 } });
test.setTimeout(120_000);

/**
 * The personal state of a board view on the real stack: a member sorts for themselves, the shared view is not written,
 * "Back to shared view" clears it on the server; the tabs write the layout to the address and Back and Forward follow
 * it; an address that names a view that is gone (an unknown id, or another person's Private view) opens the default
 * view with one notice, and the other person's list never carries the Private view.
 */
interface SavedView {
  readonly id: string;
  readonly name: string;
  readonly type: string;
  readonly visibility: string;
  readonly isDefault: boolean;
  readonly version: number;
}

const preferenceWritten = (page: Page) =>
  page.waitForResponse(
    (response) =>
      ["PUT", "PATCH"].includes(response.request().method()) &&
      /\/preferences\//.test(response.url()),
  );

test("a member sorts for themselves, the shared view stays as it was, and Back to shared view clears it", async ({
  page,
}) => {
  await signIn(page, realUsers.workspaceAdmin);
  const { workspace, board } = await openBoard(
    page,
    "Workspace 3",
    "Engineering Tasks",
  );
  const listRoute = `/boards/${board.id}/views/saved`;
  await page.goto(`/workspaces/${workspace.id}/boards/${board.id}?view=table`);
  const before = await apiRead<SavedView[]>(page, listRoute);
  const back = page.getByRole("button", {
    name: "Back to shared view",
    exact: true,
  });
  try {
    await page.getByRole("button", { name: /^Sort/ }).click();
    const written = preferenceWritten(page);
    await page.getByRole("button", { name: "Add sort" }).click();
    expect((await written).status()).toBeLessThan(300);
    await page.keyboard.press("Escape");
    await expect(back).toBeVisible();

    // Kept on the server: it survives a reload, and the shared view was not written.
    await page.reload();
    await expect(back).toBeVisible();
    expect(await apiRead<SavedView[]>(page, listRoute)).toEqual(before);

    const cleared = preferenceWritten(page);
    await back.click();
    expect((await cleared).status()).toBeLessThan(300);
    await expect(back).toHaveCount(0);
    await page.reload();
    await expect(page.getByRole("button", { name: /^Sort/ })).toBeVisible();
    await expect(back).toHaveCount(0);
  } finally {
    // Leave the member's state as found when a step above failed.
    if (await back.count()) await back.click();
  }
});

test("tabs write the layout to the address, Back and Forward follow it, and a gone view opens the default with one notice", async ({
  browser,
  page,
}) => {
  await signIn(page, realUsers.workspaceAdmin);
  const { workspace, board } = await openBoard(
    page,
    "Workspace 3",
    "Engineering Tasks",
  );
  const boardPath = `/workspaces/${workspace.id}/boards/${board.id}`;
  await page.goto(`${boardPath}?view=table`);
  await expect(page.getByRole("tab", { name: "Table" })).toBeVisible();
  await page.getByRole("tab", { name: "Kanban" }).click();
  await expect(page).toHaveURL(/view=kanban/);
  await page.goBack();
  await expect(page).toHaveURL(/view=table/);
  await page.goForward();
  await expect(page).toHaveURL(/view=kanban/);

  // An id that names no view: the default opens, with one notice.
  const gone = "That view no longer exists. Showing the default view.";
  await page.goto(
    `${boardPath}?view=table&savedView=00000000-0000-4000-8000-000000000000`,
  );
  await expect(page.getByText(gone)).toBeVisible();
  await expect(page).not.toHaveURL(/savedView=/);

  // Another person's Private view is the same: it is absent from their list and their address says nothing.
  const created = await apiWrite(page, "POST", `/boards/${board.id}/views`, {
    name: `Private ${Date.now().toString(36)}`,
    viewMode: "Table",
    config: "{}",
    visibility: "Private",
  });
  expect(created.status).toBeLessThan(300);
  const privateView = (
    await apiRead<SavedView[]>(page, `/boards/${board.id}/views/saved`)
  ).find((view) => view.visibility === "Private");
  expect(privateView).toBeDefined();
  const other = await browser.newContext();
  const owner = await other.newPage();
  try {
    await signIn(owner, realUsers.workspaceOwner);
    const theirs = await apiRead<SavedView[]>(
      owner,
      `/boards/${board.id}/views/saved`,
    );
    expect(theirs.some((view) => view.id === privateView!.id)).toBe(false);
    await owner.goto(`${boardPath}?view=table&savedView=${privateView!.id}`);
    await expect(owner.getByText(gone)).toBeVisible();
    await expect(owner).not.toHaveURL(/savedView=/);
  } finally {
    await apiWrite(
      page,
      "DELETE",
      `/boards/${board.id}/views/${privateView!.id}?expectedVersion=${privateView!.version}`,
    );
    await other.close();
  }
});
