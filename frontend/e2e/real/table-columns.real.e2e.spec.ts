import { expect, test, type Page } from "@playwright/test";
import {
  apiRead,
  apiWrite,
  openBoard,
  realUsers,
  signIn,
} from "./support/real-session";

test.use({ viewport: { width: 1440, height: 900 } });

/**
 * The "Columns" menu of the board table on the real stack: a member hides a column, the choice is theirs alone
 * and is kept after a reload; showing it again brings it back; and a viewer of the board may arrange their own
 * columns but cannot save them to the shared view (the menu item is off and says why).
 */
const header = (page: Page, name: string) =>
  page.getByRole("columnheader", { name: new RegExp(`^${name}\\b`) });

const columnsMenu = (page: Page) =>
  page.getByRole("button", { name: /^Columns/ });

async function openTable(page: Page, workspaceId: string, boardId: string) {
  await page.goto(`/workspaces/${workspaceId}/boards/${boardId}?view=table`);
  await expect(columnsMenu(page)).toBeVisible();
}

test("a member hides a column, it stays hidden after a reload, and showing it again brings it back", async ({
  page,
}) => {
  await signIn(page, realUsers.workspaceAdmin);
  const { workspace, board } = await openBoard(
    page,
    "Workspace 3",
    "Engineering Tasks",
  );
  await openTable(page, workspace.id, board.id);
  await expect(header(page, "Priority")).toBeVisible();

  try {
    await columnsMenu(page).click();
    const written = page.waitForResponse(
      (response) =>
        response.request().method() === "PUT" &&
        /\/preferences?\b/.test(response.url()),
    );
    await page.getByRole("menuitemcheckbox", { name: "Priority" }).click();
    expect((await written).status()).toBeLessThan(300);
    await page.keyboard.press("Escape");
    await expect(header(page, "Priority")).toHaveCount(0);

    // The choice is kept on the server, not in the page.
    await page.reload();
    await expect(columnsMenu(page)).toBeVisible();
    await expect(header(page, "Status")).toBeVisible();
    await expect(header(page, "Priority")).toHaveCount(0);

    await columnsMenu(page).click();
    await page.getByRole("menuitemcheckbox", { name: "Priority" }).click();
    await page.keyboard.press("Escape");
    await expect(header(page, "Priority")).toBeVisible();
    await page.reload();
    await expect(header(page, "Priority")).toBeVisible();
  } finally {
    // Leave the member's columns as found even when a step above failed.
    await openTable(page, workspace.id, board.id);
    await columnsMenu(page).click();
    const priority = page.getByRole("menuitemcheckbox", { name: "Priority" });
    if ((await priority.getAttribute("aria-checked")) === "false")
      await priority.click();
  }
});

test("a viewer of the board arranges their own columns but cannot save them to the shared view", async ({
  browser,
  page,
}) => {
  await signIn(page, realUsers.workspaceAdmin);
  const { workspace, board } = await openBoard(
    page,
    "Workspace 3",
    "Engineering Tasks",
  );
  const guestContext = await browser.newContext();
  const guest = await guestContext.newPage();
  await signIn(guest, realUsers.workspaceGuest);
  const me = await apiRead<{ id: string }>(guest, "/auth/me");
  const grant = await apiWrite(
    page,
    "POST",
    `/resources/work-management.board/${board.id}/permissions`,
    { subjectType: "User", subjectId: me.id, level: "Viewer" },
  );
  expect(grant.status).toBeLessThan(300);
  try {
    await openTable(guest, workspace.id, board.id);
    await columnsMenu(guest).click();
    const written = guest.waitForResponse(
      (response) =>
        response.request().method() === "PUT" &&
        /\/preferences?\b/.test(response.url()),
    );
    await guest.getByRole("menuitemcheckbox", { name: "Priority" }).click();
    expect((await written).status()).toBeLessThan(300);

    const save = guest.getByRole("menuitem", {
      name: "Save to shared view",
    });
    await expect(save).toHaveAttribute("aria-disabled", "true");
    await expect(
      guest.getByText("Only editors can change the columns of the shared view"),
    ).toBeVisible();

    // Restore the viewer's own columns.
    await guest.getByRole("menuitemcheckbox", { name: "Priority" }).click();
  } finally {
    // Leave the board as found: the grant would break the next spec.
    const rows = await apiRead<
      { id: string; subjectId: string; isRevoked: boolean }[]
    >(page, `/resources/work-management.board/${board.id}/permissions`);
    for (const row of rows.filter(
      (entry) => entry.subjectId === me.id && !entry.isRevoked,
    ))
      await apiWrite(
        page,
        "DELETE",
        `/resources/work-management.board/${board.id}/permissions/${row.id}`,
      );
    await guestContext.close();
  }
});
