import { expect, test, type Page } from "@playwright/test";
import { apiRead, apiWrite, realUsers, signIn } from "./support/real-session";
import {
  createOwnedWorkspace,
  joinAs,
  membersOf,
} from "./support/real-workspace";

test.use({ viewport: { width: 1440, height: 900 } });
test.setTimeout(240_000);

/**
 * The board's life on the real stack, in a workspace made for each journey: a manager archives,
 * unarchives, deletes and restores a board; a workspace admin saves a template, publishes it and a
 * member starts a board from it; the board's access narrows to Private with its impact, a role
 * changes and the last manager stays. What each journey is about goes through the page; the scene
 * and the refusals go through the API as the person concerned.
 */

type Created = string | { id: string };
const idOf = (data: Created | null) =>
  typeof data === "string" ? data : data!.id;

async function createBoard(
  page: Page,
  workspaceId: string,
  title: string,
  visibility: "Workspace" | "Private" = "Workspace",
) {
  const created = await apiWrite<Created>(
    page,
    "POST",
    `/workspaces/${workspaceId}/boards`,
    { title, visibility },
  );
  expect(created.status, `create board ${title}`).toBeLessThan(300);
  return idOf(created.data);
}

const versionOf = async (page: Page, boardId: string) =>
  (await apiRead<{ version: number }>(page, `/boards/${boardId}`)).version;

const reply = (page: Page, method: string, route: RegExp) =>
  page.waitForResponse(
    (response) =>
      response.request().method() === method && route.test(response.url()),
  );

test("a manager archives, unarchives, deletes and restores a board; a member is refused and a stale write changes nothing", async ({
  browser,
  page,
}) => {
  await signIn(page, realUsers.admin);
  const workspace = await createOwnedWorkspace(page, "Board lifecycle");
  const member = await joinAs(
    browser,
    page,
    workspace.id,
    realUsers.workspaceAdmin,
    "Member",
  );
  const title = `Lifecycle ${Date.now().toString(36)}`;
  const boardId = await createBoard(page, workspace.id, title);
  const settings = `/workspaces/${workspace.id}/boards/${boardId}/settings`;

  try {
    // Archive from Settings, General: the dialog names the effect, the page lists the board as archived.
    await page.goto(settings);
    await page.getByRole("button", { name: "General", exact: true }).click();
    await page.getByRole("button", { name: "Archive board" }).click();
    await expect(page.getByText(/^Archive the board/)).toBeVisible();
    const archived = reply(page, "POST", /\/boards\/[^/]+\/archive$/);
    await page.getByRole("button", { name: "Archive board" }).last().click();
    expect((await archived).status()).toBeLessThan(300);
    await expect(page).toHaveURL(/archived-boards/);
    const row = page.getByRole("listitem").filter({ hasText: title });
    await expect(row).toBeVisible();

    // Archived: the board still reads for a member, every write is refused with the reason and nothing changes.
    const archivedVersion = await versionOf(page, boardId);
    expect(
      (await apiRead<unknown>(member.page, `/boards/${boardId}/full`)) !== null,
    ).toBe(true);
    const groupId = (
      await apiRead<{ lists: { id: string }[] }>(
        member.page,
        `/boards/${boardId}/full`,
      )
    ).lists[0]!.id;
    const refused = await apiWrite(
      member.page,
      "POST",
      `/boards/${boardId}/items`,
      {
        groupId,
        title: "Written while archived",
      },
    );
    expect(refused.status).toBeGreaterThanOrEqual(400);
    expect(JSON.stringify(refused.data)).toContain("Archived");
    expect(await versionOf(page, boardId)).toBe(archivedVersion);
    // A second archive of an archived board is a no-op.
    const again = await apiWrite(page, "POST", `/boards/${boardId}/archive`, {
      expectedVersion: archivedVersion,
    });
    expect(again.status).toBeLessThan(300);
    expect(await versionOf(page, boardId)).toBe(archivedVersion);

    // Unarchive from the list; a member cannot.
    const memberUnarchive = await apiWrite(
      member.page,
      "POST",
      `/boards/${boardId}/unarchive`,
      { expectedVersion: archivedVersion },
    );
    expect([403, 404]).toContain(memberUnarchive.status);
    // The API writes above rotated the CSRF cookie the open page shares: reload before the page writes.
    await page.reload();
    const unarchived = reply(page, "POST", /\/boards\/[^/]+\/unarchive$/);
    await row.getByRole("button", { name: "Unarchive" }).click();
    expect((await unarchived).status()).toBeLessThan(300);
    await expect(page.getByText("The board was unarchived.")).toBeVisible();
    const memberItem = await apiWrite(
      member.page,
      "PATCH",
      `/boards/${boardId}`,
      {
        expectedVersion: await versionOf(page, boardId),
        title,
      },
    );
    // The member still may not rename; the board is writable again for those who may.
    expect(memberItem.status).toBeGreaterThanOrEqual(400);

    // Delete with the typed name; a stale delete changes nothing; the board is NotFound to a member.
    await page.goto(settings);
    await page.getByRole("button", { name: "General", exact: true }).click();
    const staleDelete = await apiWrite(
      page,
      "DELETE",
      `/boards/${boardId}?expectedVersion=1`,
    );
    expect(staleDelete.status).toBe(412);
    // The API write rotated the CSRF cookie the open page shares: reload before the page writes.
    await page.reload();
    await page.getByRole("button", { name: "General", exact: true }).click();
    await page.getByPlaceholder(title).fill(title);
    const deleted = reply(page, "DELETE", /\/boards\/[^/]+\?expectedVersion=/);
    await page.getByRole("button", { name: "Delete board" }).last().click();
    expect((await deleted).status()).toBeLessThan(300);
    await expect(page).toHaveURL(/archived-boards/);
    const gone = await member.page.request.get(
      `${process.env.VITE_API_URL ?? "http://127.0.0.1:58000/api/v1"}/boards/${boardId}/full`,
    );
    expect(gone.status()).toBe(404);

    // Restore from the deleted list; the board reads again for the member.
    await page.getByRole("tab", { name: "Deleted" }).click();
    const restoreRow = page.getByRole("listitem").filter({ hasText: title });
    const restored = reply(page, "POST", /\/boards\/[^/]+\/restore$/);
    await restoreRow.getByRole("button", { name: "Restore" }).click();
    expect((await restored).status()).toBeLessThan(300);
    await expect(page.getByText("The board was restored.")).toBeVisible();
    await apiRead(member.page, `/boards/${boardId}/full`);
    // Restoring an active board bumps nothing.
    const before = await versionOf(page, boardId);
    const noop = await apiWrite(page, "POST", `/boards/${boardId}/restore`, {
      expectedVersion: before,
    });
    expect(noop.status).toBeLessThan(300);
    expect(await versionOf(page, boardId)).toBe(before);
  } finally {
    await member.context.close();
  }
});

test("a member's open board turns read-only when a manager archives it and writable again when it is unarchived, without a reload", async ({
  browser,
  page,
}) => {
  await signIn(page, realUsers.admin);
  const workspace = await createOwnedWorkspace(page, "Board live archive");
  const member = await joinAs(
    browser,
    page,
    workspace.id,
    realUsers.workspaceAdmin,
    "Member",
  );
  const boardId = await createBoard(
    page,
    workspace.id,
    `Live ${Date.now().toString(36)}`,
  );

  try {
    await member.page.goto(`/workspaces/${workspace.id}/boards/${boardId}`);
    const banner = member.page.getByText(
      "You can view it but not change it. A manager can unarchive it in Settings.",
    );
    await expect(
      member.page.getByRole("heading", { level: 1 }).first(),
    ).toBeVisible();
    await expect(banner).toHaveCount(0);

    // The manager archives through the API; the member's page, not reloaded, shows the banner.
    const version = await versionOf(page, boardId);
    const archived = await apiWrite(
      page,
      "POST",
      `/boards/${boardId}/archive`,
      {
        expectedVersion: version,
      },
    );
    expect(archived.status).toBeLessThan(300);
    await expect(banner).toBeVisible({ timeout: 30_000 });

    // Unarchive: the banner goes away on the same page, without a reload.
    const unarchived = await apiWrite(
      page,
      "POST",
      `/boards/${boardId}/unarchive`,
      { expectedVersion: await versionOf(page, boardId) },
    );
    expect(unarchived.status).toBeLessThan(300);
    await expect(banner).toHaveCount(0, { timeout: 30_000 });
  } finally {
    await member.context.close();
  }
});

test("a workspace admin saves a board as a template and publishes it, a member starts a board from it and cannot publish, and an archived template is no longer offered", async ({
  browser,
  page,
}) => {
  await signIn(page, realUsers.admin);
  const workspace = await createOwnedWorkspace(page, "Board templates");
  const member = await joinAs(
    browser,
    page,
    workspace.id,
    realUsers.workspaceAdmin,
    "Member",
  );
  const stamp = Date.now().toString(36);
  const sourceId = await createBoard(page, workspace.id, `Source ${stamp}`);
  const templateName = `Template ${stamp}`;

  try {
    // Save from Settings, Board templates: a draft nobody else sees.
    await page.goto(`/workspaces/${workspace.id}/boards/${sourceId}/settings`);
    await page
      .getByLabel("Settings sections")
      .getByRole("button", { name: /^Board templates/ })
      .click();
    await page
      .getByRole("button", { name: "Save this board as a template" })
      .click();
    await page.getByLabel("Name of the template").fill(templateName);
    const saved = reply(page, "POST", /\/boards\/[^/]+\/templates$/);
    await page.getByRole("button", { name: "Save template" }).click();
    expect((await saved).status()).toBeLessThan(300);
    await expect(page.getByText(/as a draft/)).toBeVisible();
    const listed = async (who: Page) =>
      (
        await apiRead<{ id: string; name: string; status: string }[]>(
          who,
          `/workspaces/${workspace.id}/templates`,
        )
      ).find((entry) => entry.name === templateName);
    expect(await listed(member.page)).toBeUndefined();
    const draft = (await listed(page))!;
    expect(draft.status).toBe("Draft");

    // A member neither publishes nor starts a board from a draft (the same NotFound for the draft).
    expect(
      (await apiWrite(member.page, "PATCH", `/templates/${draft.id}/publish`))
        .status,
    ).toBe(403);
    expect(
      (
        await apiWrite(
          member.page,
          "POST",
          `/templates/${draft.id}/create-board`,
          { workspaceId: workspace.id, name: "From a draft" },
        )
      ).status,
    ).toBe(404);

    // The admin publishes from the list; publishing again changes nothing.
    const published = reply(page, "PATCH", /\/templates\/[^/]+\/publish$/);
    await page
      .getByRole("button", { name: `Publish the template ${templateName}` })
      .click();
    expect((await published).status()).toBeLessThan(300);
    expect(
      (await apiWrite(page, "PATCH", `/templates/${draft.id}/publish`)).status,
    ).toBeLessThan(300);
    // The API write rotated the CSRF cookie the open page shares: reload before the page writes.
    await page.reload();
    await page
      .getByLabel("Settings sections")
      .getByRole("button", { name: /^Board templates/ })
      .click();
    expect((await listed(member.page))?.status).toBe("Published");

    // A member starts a board from the dialog on the library: the copy has the fields, no link to the template.
    await member.page.goto(`/workspaces/${workspace.id}/library`);
    await member.page
      .getByRole("button", { name: "New Board" })
      .first()
      .click();
    const dialog = member.page.getByRole("dialog", { name: "New Board" });
    await dialog.getByText(templateName, { exact: true }).click();
    const copyName = `Copy ${stamp}`;
    await dialog.getByLabel("Board name").fill(copyName);
    const made = reply(
      member.page,
      "POST",
      /\/templates\/[^/]+\/create-board$/,
    );
    await dialog.getByRole("button", { name: "Create Board" }).click();
    await expect(member.page).toHaveURL(/\/boards\//);
    const copyId = /\/boards\/([^/?]+)/.exec(member.page.url())![1]!;
    const copy = await apiRead<{ title: string }>(
      member.page,
      `/boards/${copyId}`,
    );
    expect(copy.title).toBe(copyName);

    // Archive the template: it leaves the dialog and cannot be used; the board made from it is unchanged.
    const archived = reply(page, "PATCH", /\/templates\/[^/]+\/archive$/);
    await page
      .getByRole("button", { name: `Archive the template ${templateName}` })
      .click();
    const archivedReply = await archived;
    expect(archivedReply.status(), await archivedReply.text()).toBeLessThan(
      300,
    );
    expect(await listed(member.page)).toBeUndefined();
    expect(
      (
        await apiWrite(
          member.page,
          "POST",
          `/templates/${draft.id}/create-board`,
          { workspaceId: workspace.id, name: "From an archived one" },
        )
      ).status,
    ).toBe(404);
    expect(
      (await apiRead<{ title: string }>(member.page, `/boards/${copyId}`))
        .title,
    ).toBe(copyName);

    // Delete it: a member cannot, the admin does after the question.
    expect(
      (await apiWrite(member.page, "DELETE", `/templates/${draft.id}`)).status,
    ).toBe(403);
    await page
      .getByRole("button", { name: `Delete the template ${templateName}` })
      .click();
    const removed = reply(page, "DELETE", /\/templates\/[^/]+$/);
    await page
      .getByRole("button", { name: "Delete the template", exact: true })
      .click();
    expect((await removed).status()).toBeLessThan(300);
  } finally {
    await member.context.close();
  }
});

test("a manager makes a board Private with its impact, changes a role, and the last manager stays; an outsider meets the same NotFound", async ({
  browser,
  page,
}) => {
  await signIn(page, realUsers.admin);
  const workspace = await createOwnedWorkspace(page, "Board access");
  const member = await joinAs(
    browser,
    page,
    workspace.id,
    realUsers.workspaceAdmin,
    "Member",
  );
  const other = await joinAs(
    browser,
    page,
    workspace.id,
    realUsers.workspaceOwner,
    "Member",
  );
  const stamp = Date.now().toString(36);
  const boardId = await createBoard(page, workspace.id, `Access ${stamp}`);
  const people = (await membersOf(page, workspace.id)).filter(
    (row) => row.role === "Member",
  );
  const memberId = people.find((row) => row.name === "User 4")!.userId;
  const otherId = people.find((row) => row.userId !== memberId)!.userId;
  const owner = (await membersOf(page, workspace.id)).find(
    (row) => row.role === "Owner",
  )!.userId;
  const api = process.env.VITE_API_URL ?? "http://127.0.0.1:58000/api/v1";
  const listOf = async (who: Page) => {
    const read = await apiRead<{ id: string }[] | { items: { id: string }[] }>(
      who,
      `/workspaces/${workspace.id}/boards?limit=100`,
    );
    return (Array.isArray(read) ? read : read.items).map((row) => row.id);
  };

  try {
    // Workspace-visible: both members list it; the Share dialog names who reaches it.
    expect(await listOf(member.page)).toContain(boardId);
    expect(await listOf(other.page)).toContain(boardId);
    await page.goto(`/workspaces/${workspace.id}/boards/${boardId}?share=true`);
    const share = page.getByRole("dialog");
    await expect(share).toBeVisible();
    await expect(share.getByText(/workspace/i).first()).toBeVisible();

    // Make it Private from the dialog: the impact is shown first, nobody is added, so both members lose it.
    await share.getByRole("button", { name: "Change" }).click();
    await expect(page.getByText("Make the Board Private?")).toBeVisible();
    await expect(page.getByText(/will no longer see the Board/)).toBeVisible();
    const switched = reply(page, "POST", /\/boards\/[^/]+\/sharing\/private$/);
    await page.getByRole("button", { name: /^Switch, add no one/ }).click();
    const madePrivate = await switched;
    expect(madePrivate.status()).toBeLessThan(300);
    expect(await listOf(member.page)).not.toContain(boardId);
    expect(await listOf(other.page)).not.toContain(boardId);
    const hidden = await other.page.request.get(
      `${api}/boards/${boardId}/full`,
    );
    const unknown = await other.page.request.get(
      `${api}/boards/00000000-0000-7000-8000-000000000000/full`,
    );
    expect(hidden.status()).toBe(404);
    expect(unknown.status()).toBe(404);
    expect(await hidden.json()).toMatchObject({
      status: 404,
    });

    // Added by name, the first member reaches it again.
    const addedFirst = await apiWrite(
      page,
      "POST",
      `/boards/${boardId}/members`,
      {
        userId: memberId,
        role: "Member",
      },
    );
    expect(addedFirst.status, JSON.stringify(addedFirst.data)).toBeLessThan(
      300,
    );
    expect(await listOf(member.page)).toContain(boardId);

    // Add the second person as a Member, then lower the role: a stale write changes nothing.
    const added = await apiWrite(page, "POST", `/boards/${boardId}/members`, {
      userId: otherId,
      role: "Member",
    });
    expect(added.status, JSON.stringify(added.data)).toBeLessThan(300);
    await apiRead(other.page, `/boards/${boardId}/full`);
    const version = await versionOf(page, boardId);
    const stale = await apiWrite(
      page,
      "PATCH",
      `/boards/${boardId}/members/${otherId}/role`,
      { role: "Observer", expectedVersion: version - 1 },
    );
    expect(stale.status).toBe(412);
    expect(await versionOf(page, boardId)).toBe(version);
    const lowered = await apiWrite(
      page,
      "PATCH",
      `/boards/${boardId}/members/${otherId}/role`,
      { role: "Observer", expectedVersion: version },
    );
    expect(lowered.status, JSON.stringify(lowered.data)).toBeLessThan(300);

    // A viewer reads the board and writes nothing.
    const viewerWrite = await apiWrite(
      other.page,
      "PATCH",
      `/boards/${boardId}`,
      { expectedVersion: await versionOf(page, boardId), title: "Not mine" },
    );
    expect([403, 404]).toContain(viewerWrite.status);

    // The only Manager stays: lowering or removing the board's only Owner is refused and nothing changes.
    const settled = await versionOf(page, boardId);
    const demote = await apiWrite<{ ruleCode?: string }>(
      page,
      "PATCH",
      `/boards/${boardId}/members/${owner}/role`,
      { role: "Member", expectedVersion: settled },
    );
    expect(demote.status).toBe(400);
    expect(JSON.stringify(demote.data)).toMatch(/LastOwner|LastManager/);
    const remove = await apiWrite(
      page,
      "DELETE",
      `/boards/${boardId}/members/${owner}`,
    );
    expect(remove.status).toBe(400);
    expect(await versionOf(page, boardId)).toBe(settled);
  } finally {
    await member.context.close();
    await other.context.close();
  }
});

test("a member creates a blank board from the library and lands on it; a guest and an unknown Space create nothing, and one key makes one board", async ({
  browser,
  page,
}) => {
  await signIn(page, realUsers.admin);
  const workspace = await createOwnedWorkspace(page, "Board create");
  const member = await joinAs(
    browser,
    page,
    workspace.id,
    realUsers.workspaceAdmin,
    "Member",
  );
  const guest = await joinAs(
    browser,
    page,
    workspace.id,
    realUsers.workspaceGuest,
    "Guest",
  );
  const title = `Blank ${Date.now().toString(36)}`;
  const countOf = async () =>
    await apiRead<{ title: string }[] | { items: { title: string }[] }>(
      page,
      `/workspaces/${workspace.id}/boards?limit=100`,
    );
  const titles = async () => {
    const read = await countOf();
    return (Array.isArray(read) ? read : read.items).map((row) => row.title);
  };

  try {
    // Main flow: New Board on the library, a name, Create Board; the board opens on its default view.
    await member.page.goto(`/workspaces/${workspace.id}/library`);
    await member.page
      .getByRole("button", { name: "New Board" })
      .first()
      .click();
    const dialog = member.page.getByRole("dialog", { name: "New Board" });
    await dialog.getByLabel("Board name").fill(title);
    await dialog.getByRole("button", { name: "Create Board" }).click();
    await expect(member.page).toHaveURL(/\/boards\//);
    await expect(
      member.page.getByRole("heading", { name: title }).first(),
    ).toBeVisible();
    expect(await titles()).toContain(title);

    // The same key with the same body makes one board.
    const key = crypto.randomUUID();
    const body = { title: `Keyed ${title}`, visibility: "Workspace" };
    const first = await apiWrite(
      page,
      "POST",
      `/workspaces/${workspace.id}/boards`,
      body,
      key,
    );
    const second = await apiWrite(
      page,
      "POST",
      `/workspaces/${workspace.id}/boards`,
      body,
      key,
    );
    expect(first.status).toBeLessThan(300);
    expect(second.status).toBeLessThan(300);
    expect((await titles()).filter((name) => name === body.title)).toHaveLength(
      1,
    );
    // Rejections: a guest is refused, an unknown Space and an empty name create nothing.
    const before = (await titles()).length;
    expect(
      (
        await apiWrite(
          guest.page,
          "POST",
          `/workspaces/${workspace.id}/boards`,
          {
            title: "Guest board",
            visibility: "Workspace",
          },
        )
      ).status,
    ).toBe(403);
    expect(
      (
        await apiWrite(page, "POST", `/workspaces/${workspace.id}/boards`, {
          title: "Nowhere",
          visibility: "Workspace",
          spaceId: "00000000-0000-7000-8000-000000000001",
        })
      ).status,
    ).toBe(404);
    expect(
      (
        await apiWrite(page, "POST", `/workspaces/${workspace.id}/boards`, {
          title: "",
          visibility: "Workspace",
        })
      ).status,
    ).toBe(400);
    expect((await titles()).length).toBe(before);
  } finally {
    await member.context.close();
    await guest.context.close();
  }
});

test("twenty creates in parallel on one board give twenty distinct item keys, and a prefix change applies to new items only", async ({
  page,
}) => {
  await signIn(page, realUsers.admin);
  const workspace = await createOwnedWorkspace(page, "Item keys");
  const boardId = await createBoard(page, workspace.id, "Keys");
  const groupId = (
    await apiRead<{ lists: { id: string }[] }>(page, `/boards/${boardId}/full`)
  ).lists[0]!.id;
  const keyOf = async (itemId: string) => {
    const detail = await apiRead<Record<string, unknown>>(
      page,
      `/board-items/${itemId}`,
    );
    const key = detail.key ?? detail.itemKey ?? detail.displayKey;
    expect(key, JSON.stringify(Object.keys(detail))).toBeTruthy();
    return String(key);
  };

  // One CSRF token for the burst: every `apiWrite` rotates the cookie, so parallel ones would refuse each other.
  const api = process.env.VITE_API_URL ?? "http://127.0.0.1:58000/api/v1";
  const csrf = (await (await page.request.get(`${api}/auth/csrf`)).json()) as {
    token: string;
  };
  const made = await Promise.all(
    Array.from({ length: 20 }, async (_, index) => {
      const response = await page.request.post(
        `${api}/boards/${boardId}/items`,
        {
          headers: {
            "X-CSRF-Token": csrf.token,
            "Idempotency-Key": crypto.randomUUID(),
          },
          data: { groupId, title: `Item ${index}` },
        },
      );
      return {
        status: response.status(),
        data: (await response.json()) as Created,
      };
    }),
  );
  expect(made.map((answer) => answer.status)).toEqual(Array(20).fill(200));
  const keys = await Promise.all(
    made.map((answer) => keyOf(idOf(answer.data))),
  );
  expect(new Set(keys).size).toBe(20);

  // A new prefix keeps the old keys and numbers on from the sequence.
  const version = await versionOf(page, boardId);
  const changed = await apiWrite(page, "PATCH", `/boards/${boardId}`, {
    expectedVersion: version,
    itemKeyPrefix: "NEWK",
  });
  expect(changed.status).toBeLessThan(300);
  const next = await apiWrite<Created>(
    page,
    "POST",
    `/boards/${boardId}/items`,
    {
      groupId,
      title: "After the change",
    },
  );
  expect(await keyOf(idOf(made[0]!.data))).toBe(keys[0]);
  expect(await keyOf(idOf(next.data))).toMatch(/^NEWK-\d+$/);
});
