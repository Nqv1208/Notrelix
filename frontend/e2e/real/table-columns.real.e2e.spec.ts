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
 * The "Columns" menu of the board table on the real stack: a member hides, shows and reorders columns, the choice
 * is theirs alone and is kept after a reload; and a viewer of the board may arrange their own columns but cannot
 * save them to the shared view (the button is off and says why).
 */
const headerNames = async (page: Page) =>
  (await page.getByRole("columnheader").allInnerTexts()).map((text) =>
    text.trim(),
  );

const position = async (page: Page, name: string) =>
  (await headerNames(page)).findIndex((text) => text.startsWith(name));

const columnsButton = (page: Page) =>
  page.getByRole("button", { name: /^Columns/ });

const menuOf = (page: Page) => page.getByRole("dialog", { name: "Columns" });

async function openTable(page: Page, workspaceId: string, boardId: string) {
  await page.goto(`/workspaces/${workspaceId}/boards/${boardId}?view=table`);
  await expect(columnsButton(page)).toBeVisible();
}

const ownColumnsSaved = (page: Page) =>
  page.waitForResponse(
    (response) =>
      response.request().method() === "PUT" &&
      /\/preferences?\b/.test(response.url()),
  );

test("a member hides, shows and reorders a column, and the choice stays after a reload", async ({
  page,
}) => {
  await signIn(page, realUsers.workspaceAdmin);
  const { workspace, board } = await openBoard(
    page,
    "Workspace 3",
    "Engineering Tasks",
  );
  await openTable(page, workspace.id, board.id);
  await expect.poll(() => position(page, "Priority")).toBeGreaterThan(-1);

  try {
    // Hide.
    await columnsButton(page).click();
    const hidden = ownColumnsSaved(page);
    await menuOf(page).getByRole("checkbox", { name: "Priority" }).click();
    expect((await hidden).status()).toBeLessThan(300);
    await page.keyboard.press("Escape");
    await expect.poll(() => position(page, "Priority")).toBe(-1);

    // The choice is kept on the server, not in the page.
    await page.reload();
    await expect(columnsButton(page)).toBeVisible();
    await expect.poll(() => position(page, "Status")).toBeGreaterThan(-1);
    await expect.poll(() => position(page, "Priority")).toBe(-1);

    // Show again.
    await columnsButton(page).click();
    const shown = ownColumnsSaved(page);
    await menuOf(page).getByRole("checkbox", { name: "Priority" }).click();
    expect((await shown).status()).toBeLessThan(300);
    await page.keyboard.press("Escape");
    await expect.poll(() => position(page, "Priority")).toBeGreaterThan(-1);

    // Reorder: Alt+Down on the Status row moves the column one place to the right.
    const before = await position(page, "Status");
    await columnsButton(page).click();
    const moved = ownColumnsSaved(page);
    const status = menuOf(page).getByRole("checkbox", { name: "Status" });
    await status.focus();
    await page.keyboard.press("Alt+ArrowDown");
    expect((await moved).status()).toBeLessThan(300);
    await page.keyboard.press("Escape");
    await expect.poll(() => position(page, "Status")).toBe(before + 1);

    await page.reload();
    await expect(columnsButton(page)).toBeVisible();
    await expect.poll(() => position(page, "Status")).toBe(before + 1);
  } finally {
    // Leave the member's columns as found even when a step above failed: the menu's own way back.
    await openTable(page, workspace.id, board.id);
    await columnsButton(page).click();
    const back = menuOf(page).getByRole("button", {
      name: "Back to the shared view",
    });
    if ((await back.getAttribute("aria-disabled")) !== "true") {
      await back.click();
      await expect(back).toHaveAttribute("aria-disabled", "true");
    }
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
    await columnsButton(guest).click();
    const written = ownColumnsSaved(guest);
    await menuOf(guest).getByRole("checkbox", { name: "Priority" }).click();
    expect((await written).status()).toBeLessThan(300);

    const save = menuOf(guest).getByRole("button", {
      name: "Save to the shared view",
    });
    await expect(save).toHaveAttribute("aria-disabled", "true");
    const reason = menuOf(guest).getByText(
      "Only board editors can save to the shared view.",
    );
    await expect(reason).toBeVisible();
    // The reason is what the locked button is described by.
    const reasonId = await save.getAttribute("aria-describedby");
    await expect(guest.locator(`[id="${reasonId}"]`)).toHaveText(
      "Only board editors can save to the shared view.",
    );

    // Restore the viewer's own columns.
    await menuOf(guest).getByRole("checkbox", { name: "Priority" }).click();
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
