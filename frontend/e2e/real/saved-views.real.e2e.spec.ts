import { expect, test, type Page } from "@playwright/test";
import { apiRead, openBoard, realUsers, signIn } from "./support/real-session";

test.use({ viewport: { width: 1440, height: 900 } });
test.setTimeout(120_000);

interface SavedView {
  readonly id: string;
  readonly name: string;
  readonly type: string;
  readonly visibility: string;
  readonly isDefault: boolean;
}

function written(page: Page, method: string, route: RegExp) {
  return page.waitForResponse(
    (response) =>
      response.request().method() === method && route.test(response.url()),
  );
}

test("a board editor saves a view as a tab, renames it, keeps it private and makes it the default", async ({
  page,
}) => {
  await signIn(page, realUsers.workspaceAdmin);
  const { workspace, board } = await openBoard(
    page,
    "Workspace 3",
    "Engineering Tasks",
  );
  const unique = Date.now().toString(36);
  const listRoute = `/boards/${board.id}/views/saved`;

  // The tabs: "Add view" saves a Calendar view and opens it.
  const created = written(page, "POST", /\/boards\/[^/]+\/views$/);
  await page.getByRole("button", { name: "Add view" }).click();
  await page.getByRole("menuitem", { name: "Calendar" }).click();
  expect((await created).status()).toBeLessThan(300);
  await expect(page).toHaveURL(/savedView=/);
  const viewId = new URL(page.url()).searchParams.get("savedView")!;
  const afterCreate = await apiRead<SavedView[]>(page, listRoute);
  const made = afterCreate.find((view) => view.id === viewId);
  expect(made).toMatchObject({ type: "Calendar", visibility: "Board" });
  const editLabel = `Edit the view ${made!.name}`;

  // The settings' Views section: rename it and keep it private.
  await page.goto(`/workspaces/${workspace.id}/boards/${board.id}/settings`);
  await page.getByRole("button", { name: /^Views/ }).click();
  const renamedTo = `Plan ${unique}`;
  await page.getByRole("button", { name: editLabel, exact: true }).click();
  const panel = page.getByRole("complementary", { name: /^Edit the view / });
  await panel.getByLabel("Name", { exact: true }).fill(renamedTo);
  await panel.getByRole("radio", { name: "Only me" }).click();
  const renamed = written(page, "PATCH", /\/views\/[^/]+\/rename$/);
  const hidden = written(page, "PATCH", /\/views\/[^/]+\/visibility$/);
  await panel.getByRole("button", { name: "Save", exact: true }).click();
  expect((await renamed).status()).toBeLessThan(300);
  expect((await hidden).status()).toBeLessThan(300);
  const afterEdit = await apiRead<SavedView[]>(page, listRoute);
  expect(afterEdit.find((view) => view.id === viewId)).toMatchObject({
    name: renamedTo,
    visibility: "Private",
  });

  // Shared again, it can be the default (a private view cannot).
  await expect(panel.getByRole("button", { name: "Make default" })).toHaveCount(
    0,
  );
  await panel.getByRole("radio", { name: "Everyone on the board" }).click();
  const shared = written(page, "PATCH", /\/views\/[^/]+\/visibility$/);
  await panel.getByRole("button", { name: "Save", exact: true }).click();
  expect((await shared).status()).toBeLessThan(300);

  // Make it the default, then the board opens on it and the tab survives a reload.
  const defaulted = written(page, "POST", /\/views\/[^/]+\/set-default$/);
  await panel.getByRole("button", { name: "Make default" }).click();
  expect((await defaulted).status()).toBeLessThan(300);
  await page.goto(
    `/workspaces/${workspace.id}/boards/${board.id}?view=calendar`,
  );
  await expect(page.getByRole("tab", { name: renamedTo })).toBeVisible();
  const afterDefault = await apiRead<SavedView[]>(page, listRoute);
  expect(afterDefault[0]).toMatchObject({ id: viewId, isDefault: true });
});
