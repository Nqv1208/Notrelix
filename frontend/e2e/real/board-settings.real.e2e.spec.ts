import { expect, test, type Page } from "@playwright/test";
import { openBoard, realUsers, signIn } from "./support/real-session";

test.use({ viewport: { width: 1440, height: 900 } });

function written(page: Page, method: string, route: RegExp) {
  return page.waitForResponse(
    (response) =>
      response.request().method() === method && route.test(response.url()),
  );
}

test("a board manager renames the board, adds and renames a group and adds a field option from the settings page", async ({
  page,
}) => {
  await signIn(page, realUsers.workspaceAdmin);
  const { workspace, board } = await openBoard(
    page,
    "Workspace 3",
    "Engineering Tasks",
  );
  const unique = Date.now().toString(36);

  await page.goto(`/workspaces/${workspace.id}/boards/${board.id}/settings`);
  await expect(
    page.getByRole("heading", { level: 1, name: "Board settings" }),
  ).toBeVisible();

  // General: rename the board.
  await page.getByRole("button", { name: "General", exact: true }).click();
  const renamedTitle = `${board.title} ${unique}`;
  await page.getByLabel("Board name", { exact: true }).fill(renamedTitle);
  const renamed = written(page, "PATCH", /\/boards\/[^/]+$/);
  await page.getByRole("button", { name: "Save changes" }).click();
  expect((await renamed).status()).toBeLessThan(300);
  await expect(page.getByLabel("Board name", { exact: true })).toHaveValue(
    renamedTitle,
  );

  // Groups: add one, then rename it.
  await page.getByRole("button", { name: /^Task groups/ }).click();
  const groupName = `Group ${unique}`;
  await page.getByRole("button", { name: "Add group" }).click();
  await page.getByPlaceholder("Name of the new group").fill(groupName);
  const created = written(page, "POST", /\/boards\/[^/]+\/groups$/);
  await page.getByRole("button", { name: "Add", exact: true }).click();
  expect((await created).status()).toBeLessThan(300);
  await expect(page.getByText(groupName, { exact: true })).toBeVisible();

  const renamedGroup = `${groupName} renamed`;
  await page
    .getByRole("button", { name: `Rename the group ${groupName}` })
    .click();
  await page.getByLabel("Name of the group").fill(renamedGroup);
  const groupRenamed = written(page, "PATCH", /\/board-groups\/[^/]+$/);
  await page.keyboard.press("Enter");
  expect((await groupRenamed).status()).toBeLessThan(300);
  await expect(page.getByText(renamedGroup, { exact: true })).toBeVisible();

  // Fields: add an option to the Status field.
  await page.getByRole("button", { name: /^Data fields/ }).click();
  await page.getByRole("button", { name: "Edit the field Status" }).click();
  const optionName = `Option ${unique}`;
  await page.getByRole("button", { name: "Add option" }).click();
  const optionAdded = written(
    page,
    "POST",
    /\/boards\/[^/]+\/fields\/[^/]+\/options$/,
  );
  await page
    .getByRole("textbox", { name: "Name of the option" })
    .last()
    .fill(optionName);
  await page.keyboard.press("Enter");
  expect((await optionAdded).status()).toBeLessThan(300);
});
