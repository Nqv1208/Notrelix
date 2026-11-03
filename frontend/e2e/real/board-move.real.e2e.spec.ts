import { expect, test, type Page } from "@playwright/test";
import { apiRead, apiWrite, realUsers, signIn } from "./support/real-session";
import { createOwnedWorkspace, joinAs } from "./support/real-workspace";

test.use({ viewport: { width: 1440, height: 1000 } });
test.setTimeout(240_000);

/**
 * Moving a board between departments on the real stack, in a workspace made for the journey: the manager opens
 * the board's settings, picks the other department in the card list, reads who gains and loses access and moves
 * the board; the place sticks after a reload. The current department cannot be picked, a member's write is
 * refused and a stale version changes nothing.
 */

interface BoardRow {
  readonly id: string;
  readonly version: number;
  readonly spaceId: string | null;
}

const placeOf = async (page: Page, workspaceId: string, boardId: string) =>
  (await apiRead<BoardRow[]>(page, `/workspaces/${workspaceId}/boards`)).find(
    (row) => row.id === boardId,
  )!;

test("a manager moves a board to another department from the card list; the same place, a member and a stale version change nothing", async ({
  browser,
  page,
}) => {
  await signIn(page, realUsers.admin);
  const workspace = await createOwnedWorkspace(page, "Board move");
  const member = await joinAs(
    browser,
    page,
    workspace.id,
    realUsers.workspaceAdmin,
    "Member",
  );
  const space = async (name: string) => {
    const made = await apiWrite<string>(
      page,
      "POST",
      `/workspaces/${workspace.id}/spaces`,
      { name, visibility: "Workspace", spaceType: "Department" },
    );
    expect(made.status, `create ${name}`).toBe(201);
    return made.data as string;
  };
  const from = await space("Engineering");
  const to = await space("Accounting");
  const made = await apiWrite<string | { id: string }>(
    page,
    "POST",
    `/workspaces/${workspace.id}/boards`,
    { title: "Board to move", spaceId: from },
  );
  expect(made.status).toBe(201);
  const boardId = typeof made.data === "string" ? made.data : made.data!.id;

  try {
    await page.goto(
      `/workspaces/${workspace.id}/boards/${boardId}/settings?section=general`,
    );
    await page
      .getByRole("button", { name: "Move to another department" })
      .click();
    const dialog = page.getByRole("dialog", {
      name: "Move board to a department",
    });
    await expect(dialog).toBeVisible();

    // The department the board sits in is listed and cannot be picked; nothing is picked, nothing moves.
    const here = dialog.getByRole("radio", { name: /Engineering/ });
    await expect(here).toHaveAttribute("aria-disabled", "true");
    await expect(dialog.getByText("Here", { exact: true })).toBeVisible();
    await expect(
      dialog.getByRole("button", { name: "Move board" }),
    ).toHaveAttribute("aria-disabled", "true");
    expect((await placeOf(page, workspace.id, boardId)).spaceId).toBe(from);

    // Picking the other department reads who gains and loses access, then the move goes through.
    const impact = page.waitForResponse(
      (response) =>
        response.request().method() === "GET" &&
        /\/boards\/[^/]+\/move-impact/.test(response.url()),
    );
    await dialog.getByRole("radio", { name: /Accounting/ }).click();
    expect((await impact).status()).toBe(200);
    await expect(page.locator('[data-f="sections"]')).toBeVisible();
    const moved = page.waitForResponse(
      (response) =>
        response.request().method() === "POST" &&
        /\/boards\/[^/]+\/move-to-space$/.test(response.url()),
    );
    await dialog.getByRole("button", { name: /^Move to .*Accounting/ }).click();
    expect((await moved).status()).toBeLessThan(300);
    await expect(dialog).toBeHidden();
    const placed = await placeOf(page, workspace.id, boardId);
    expect(placed.spaceId).toBe(to);

    // The place sticks after a reload: the dialog now marks Accounting as the current place.
    await page.reload();
    await page
      .getByRole("button", { name: "Move to another department" })
      .click();
    await expect(
      page
        .getByRole("dialog", { name: "Move board to a department" })
        .getByRole("radio", { name: /Accounting/ }),
    ).toHaveAttribute("aria-disabled", "true");
    await page.keyboard.press("Escape");

    // A member may not move the board, and a stale version moves nothing.
    const refused = await apiWrite(
      member.page,
      "POST",
      `/boards/${boardId}/move-to-space`,
      { spaceId: from, expectedVersion: placed.version },
    );
    expect(refused.status).toBeGreaterThanOrEqual(400);
    const stale = await apiWrite(
      page,
      "POST",
      `/boards/${boardId}/move-to-space`,
      {
        spaceId: from,
        expectedVersion: placed.version - 1,
      },
    );
    expect(stale.status).toBe(412);
    expect((await placeOf(page, workspace.id, boardId)).spaceId).toBe(to);
  } finally {
    await member.context.close();
  }
});
