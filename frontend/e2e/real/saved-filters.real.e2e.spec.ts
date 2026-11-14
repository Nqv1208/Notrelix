import { expect, test } from "@playwright/test";
import { apiRead, openBoard, realUsers, signIn } from "./support/real-session";

test.use({ viewport: { width: 1440, height: 900 } });
test.setTimeout(150_000);

/**
 * A saved filter on the real stack: a board editor saves the bar's filter from the bar, returns to the shared view,
 * applies the filter again from the menu (only the member's own state is written, the board's views stay as they
 * were), shares it with the board so a colleague sees it with the sharer's name and applies it for themselves, and
 * deletes it (the list no longer carries it).
 */
interface SavedFilter {
  readonly id: string;
  readonly name: string;
  readonly visibility: string;
  readonly createdByName?: string | null;
}
interface SavedView {
  readonly id: string;
  readonly version: number;
}

test("saving, applying, sharing and deleting a saved filter", async ({
  page,
  browser,
}) => {
  const name = `Mine ${Date.now().toString(36)}`;
  const word = `zz${Date.now().toString(36)}`;
  await signIn(page, realUsers.workspaceAdmin);
  const { workspace, board } = await openBoard(
    page,
    "Workspace 3",
    "Engineering Tasks",
  );
  const boardPath = `/workspaces/${workspace.id}/boards/${board.id}`;
  // The menu button carries the name of the applied filter.
  const menuButton = page.getByRole("button", {
    name: new RegExp(`^(Saved filters|${name})`),
  });
  const filters = () =>
    apiRead<SavedFilter[]>(page, `/boards/${board.id}/saved-filters`);
  const viewsBefore = await apiRead<SavedView[]>(
    page,
    `/boards/${board.id}/views/saved`,
  );
  await page.goto(
    `${boardPath}?view=table&filter=${encodeURIComponent(`c2:and~%24title.contains.${word}`)}`,
  );

  // Save the bar's filter as a private one.
  await page.getByRole("button", { name: "Save filter", exact: true }).click();
  const form = page.getByRole("form", { name: "Save the current filter" });
  await form.getByRole("textbox", { name: "Filter name" }).fill(name);
  await form.getByRole("button", { name: "Save filter", exact: true }).click();
  await expect(page.getByText(`Saved filter ${name}`)).toBeVisible();
  const saved = (await filters()).find((filter) => filter.name === name);
  expect(saved, "the saved filter is on the server").toBeDefined();
  expect(saved!.visibility).toBe("Private");

  // Back to the shared view, then apply it from the menu: the chip returns.
  const chip = page
    .getByLabel("Filter", { exact: true })
    .getByText(`Title contains “${word}”`);
  await page
    .getByRole("button", { name: "Back to shared view", exact: true })
    .click();
  await expect(chip).toHaveCount(0);
  await menuButton.click();
  await page.getByRole("button", { name: `Apply filter ${name}` }).click();
  await expect(chip).toBeVisible();
  expect(
    await apiRead<SavedView[]>(page, `/boards/${board.id}/views/saved`),
    "applying writes the member's own state, not the board's views",
  ).toEqual(viewsBefore);

  // Share it: a colleague lists it with the sharer's name and applies it on their own bar.
  await page.keyboard.press("Escape");
  await menuButton.click();
  await page
    .getByRole("button", { name: `Share ${name} with the board` })
    .click();
  await expect
    .poll(
      async () => (await filters()).find((f) => f.id === saved!.id)?.visibility,
    )
    .toBe("Public");
  const other = await browser.newContext();
  const colleague = await other.newPage();
  try {
    await signIn(colleague, realUsers.workspaceOwner);
    const theirs = (
      await apiRead<SavedFilter[]>(
        colleague,
        `/boards/${board.id}/saved-filters`,
      )
    ).find((filter) => filter.id === saved!.id);
    expect(theirs?.createdByName, "the sharer is named").toBeTruthy();
    await colleague.goto(`${boardPath}?view=table`);
    await colleague.getByRole("button", { name: /^Saved filters/ }).click();
    await colleague
      .getByRole("button", { name: `Apply filter ${name}` })
      .click();
    await expect(
      colleague
        .getByLabel("Filter", { exact: true })
        .getByText(`Title contains “${word}”`),
    ).toBeVisible();
  } finally {
    await other.close();
  }

  // Delete it from the menu: the list no longer carries it.
  await page.keyboard.press("Escape");
  await menuButton.click();
  await page.getByRole("button", { name: `Delete ${name}` }).click();
  await expect(page.getByText(`Deleted filter ${name}`)).toBeVisible();
  await expect
    .poll(async () => (await filters()).some((f) => f.id === saved!.id))
    .toBe(false);
});
