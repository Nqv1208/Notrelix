import { execFileSync } from "node:child_process";
import { expect, test } from "@playwright/test";
import {
  apiRead,
  apiWrite,
  findWorkspace,
  openBoard,
  realUsers,
  signIn,
} from "./support/real-session";

/**
 * The seeded members have not confirmed their email, and creating a Space needs a
 * confirmed one: the development database stands in for the mail link.
 */
function confirmEmailOf(email: string) {
  execFileSync("docker", [
    "exec",
    "notrelix-fe-real-e2e-postgres",
    "psql",
    "-U",
    "postgres",
    "-d",
    "notrelix_real_e2e",
    "-c",
    `update identity.users set email_confirmed = true, email_confirmed_at = now() where email = '${email}'`,
  ]);
}

test.use({ viewport: { width: 1440, height: 900 } });
test.setTimeout(120_000);

interface InboxEntry {
  readonly id: string;
  readonly type: string;
  readonly excerpt: string | null;
  readonly target: { readonly id: string } | null;
}

test("an assignee follows the item and is told, as a follower, about a later comment", async ({
  browser,
  page,
}) => {
  const unique = Date.now().toString(36);
  const context = await browser.newContext();
  const reader = await context.newPage();
  await signIn(reader, realUsers.admin);
  const readerWorkspace = await findWorkspace(reader, "Workspace 3");
  const me = await apiRead<{ userId: string }>(
    reader,
    `/workspaces/${readerWorkspace.id}/members/me`,
  );

  await signIn(page, realUsers.workspaceAdmin);
  const { workspace, board } = await openBoard(
    page,
    "Workspace 3",
    "Engineering Tasks",
  );
  const groupId = (
    await apiRead<{ lists: { id: string }[] }>(page, `/boards/${board.id}/full`)
  ).lists[0]!.id;
  const created = await apiWrite<{ id: string }>(
    page,
    "POST",
    `/boards/${board.id}/items`,
    { groupId, title: `Followed ${unique}` },
  );
  const itemId = created.data!.id;
  await expect
    .poll(
      async () =>
        (
          await apiRead<{ following: boolean }>(
            page,
            `/board-items/${itemId}/following`,
          )
        ).following,
      { message: "the creator follows the item they create", timeout: 30_000 },
    )
    .toBe(true);

  expect(
    (
      await apiWrite(page, "POST", `/board-items/${itemId}/assignees`, {
        userId: me.userId,
      })
    ).status,
  ).toBeLessThan(300);
  await expect
    .poll(
      async () =>
        (
          await apiRead<{ following: boolean }>(
            reader,
            `/board-items/${itemId}/following`,
          )
        ).following,
      { message: "the assignee follows", timeout: 30_000 },
    )
    .toBe(true);
  expect(
    (
      await apiWrite(page, "POST", `/board-items/${itemId}/comments`, {
        contentMd: `Remember the <b>plan</b> ${unique}`,
      })
    ).status,
  ).toBeLessThan(300);

  const inbox = () =>
    apiRead<{ items: InboxEntry[] }>(
      reader,
      `/workspaces/${readerWorkspace.id}/inbox?limit=50`,
    );
  await expect
    .poll(
      async () =>
        (await inbox()).items
          .filter((entry) => entry.target?.id === itemId)
          .map((entry) => entry.type)
          .sort(),
      { timeout: 60_000 },
    )
    .toEqual(["work.item.assigned", "work.item.commented"]);
  const comment = (await inbox()).items.find(
    (entry) => entry.type === "work.item.commented",
  );
  expect(comment?.excerpt).toBe(`Remember the plan ${unique}`);
  expect(
    (
      await apiRead<{ following: boolean }>(
        reader,
        `/board-items/${itemId}/following`,
      )
    ).following,
    "the assignee follows",
  ).toBe(true);

  // The Inbox page draws both entries.
  await reader.goto(`/workspaces/${readerWorkspace.id}/inbox`);
  await expect(reader.getByText(`Followed ${unique}`).first()).toBeVisible();
  void workspace;
  await context.close();
});

test("a board placed in a department shows under it in My work, and moving the board moves the group", async ({
  page,
}) => {
  const unique = Date.now().toString(36);
  confirmEmailOf(realUsers.workspaceOwner.email);
  await signIn(page, realUsers.workspaceOwner);
  const workspace = await findWorkspace(page, "Workspace 3");
  const space = async (name: string) =>
    (
      await apiWrite<string>(
        page,
        "POST",
        `/workspaces/${workspace.id}/spaces`,
        { name, visibility: "Workspace" },
      )
    ).data!;
  const first = await space(`Sales ${unique}`);
  const second = await space(`Support ${unique}`);

  const board = await apiWrite(
    page,
    "POST",
    `/workspaces/${workspace.id}/boards`,
    { title: `Dept board ${unique}`, spaceId: first },
  );
  expect(board.status).toBeLessThan(300);
  const boardId = (
    await apiRead<{ id: string; title: string }[]>(
      page,
      `/workspaces/${workspace.id}/boards`,
    )
  ).find((candidate) => candidate.title === `Dept board ${unique}`)!.id;
  await apiWrite(page, "POST", `/boards/${boardId}/groups`, { title: "Todo" });
  const groupId = (
    await apiRead<{ lists: { id: string }[] }>(page, `/boards/${boardId}/full`)
  ).lists[0]!.id;
  const item = await apiWrite<{ id: string }>(
    page,
    "POST",
    `/boards/${boardId}/items`,
    { groupId, title: `Dept item ${unique}` },
  );
  const me = await apiRead<{ userId: string }>(
    page,
    `/workspaces/${workspace.id}/members/me`,
  );
  await apiWrite(page, "POST", `/board-items/${item.data!.id}/assignees`, {
    userId: me.userId,
  });

  const departments = async () =>
    (
      await apiRead<{ departments: Record<string, string | null> }>(
        page,
        `/workspaces/${workspace.id}/my-work/extended`,
      )
    ).departments[boardId];
  expect(await departments()).toBe(first);

  const version = (
    await apiRead<{ version: number }>(page, `/boards/${boardId}/full`)
  ).version;
  expect(
    (
      await apiWrite(page, "POST", `/boards/${boardId}/move-to-space`, {
        spaceId: second,
        expectedVersion: version,
      })
    ).status,
  ).toBeLessThan(300);
  expect(await departments()).toBe(second);

  await page.goto(`/workspaces/${workspace.id}/my-work`);
  await expect(page.getByText(`Dept item ${unique}`).first()).toBeVisible();
});
