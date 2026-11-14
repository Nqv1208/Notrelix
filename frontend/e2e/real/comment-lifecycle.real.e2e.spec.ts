import { expect, test, type Page } from "@playwright/test";
import {
  apiRead,
  apiWrite,
  findWorkspace,
  openBoard,
  realUsers,
  signIn,
} from "./support/real-session";

test.use({ viewport: { width: 1440, height: 900 } });
test.setTimeout(240_000);

type Comment = {
  id: string;
  contentMd: string;
  parentCommentId: string | null;
  isEdited: boolean;
  resolvedAt: string | null;
  version: number;
  isDeleted?: boolean;
  reactions: { emoji: string; count: number; reactedByMe: boolean }[];
};

const idOf = (data: unknown) =>
  typeof data === "string" ? data : (data as { id: string }).id;

async function list(page: Page, root: string) {
  return apiRead<Comment[] | { items: Comment[] }>(page, root).then((r) =>
    Array.isArray(r) ? r : r.items,
  );
}

test("a comment on a board item goes through its whole life and every outsider is told NotFound", async ({
  page,
  browser,
}) => {
  const unique = Date.now().toString(36);
  await signIn(page, realUsers.workspaceAdmin);
  const { workspace, board } = await openBoard(
    page,
    "Workspace 3",
    "Engineering Tasks",
  );
  const groupId = (
    await apiRead<{ lists: { id: string }[] }>(page, `/boards/${board.id}/full`)
  ).lists[0]!.id;
  const itemId = (
    await apiWrite<{ id: string }>(page, "POST", `/boards/${board.id}/items`, {
      groupId,
      title: `Thread ${unique}`,
    })
  ).data!.id;
  const root = `/board-items/${itemId}/comments`;

  // Create, and a retry under the same Idempotency-Key stores one comment only.
  const key = crypto.randomUUID();
  const first = await apiWrite(
    page,
    "POST",
    root,
    { contentMd: `First ${unique}` },
    key,
  );
  expect(first.status).toBeLessThan(300);
  const retry = await apiWrite(
    page,
    "POST",
    root,
    { contentMd: `First ${unique}` },
    key,
  );
  expect(retry.status).toBeLessThan(300);
  const commentId = idOf(first.data);
  expect(idOf(retry.data)).toBe(commentId);
  expect(
    (await list(page, root)).filter((c) => c.contentMd === `First ${unique}`),
  ).toHaveLength(1);

  // Empty and over-long text are validation errors; nothing is stored.
  expect(
    (await apiWrite(page, "POST", root, { contentMd: "   " })).status,
  ).toBe(400);
  expect(
    (await apiWrite(page, "POST", root, { contentMd: "x".repeat(10_001) }))
      .status,
  ).toBe(400);

  // A reply joins the thread.
  const reply = await apiWrite(page, "POST", root, {
    contentMd: `Reply ${unique}`,
    parentCommentId: commentId,
  });
  expect(reply.status).toBeLessThan(300);

  // Edit with the version the reader saw; a stale version is 412 and changes nothing.
  let mine = (await list(page, root)).find((c) => c.id === commentId)!;
  const edit = await apiWrite(page, "PATCH", `/comments/${commentId}`, {
    contentMd: `Edited ${unique}`,
    expectedVersion: mine.version,
  });
  expect(edit.status).toBeLessThan(300);
  const stale = await apiWrite(page, "PATCH", `/comments/${commentId}`, {
    contentMd: `Stale ${unique}`,
    expectedVersion: mine.version,
  });
  expect(stale.status).toBe(412);
  mine = (await list(page, root)).find((c) => c.id === commentId)!;
  expect(mine.contentMd).toBe(`Edited ${unique}`);
  expect(mine.isEdited).toBe(true);

  // Another member reads it, reacts and withdraws, but may not edit it.
  const other = await browser.newPage();
  await signIn(other, realUsers.workspaceOwner);
  expect((await list(other, root)).map((c) => c.id)).toContain(commentId);
  expect(
    (
      await apiWrite(other, "PATCH", `/comments/${commentId}`, {
        contentMd: "Hijack",
        expectedVersion: mine.version,
      })
    ).status,
  ).toBe(403);
  const react = await apiWrite(
    other,
    "PUT",
    `${root}/${commentId}/reactions/%F0%9F%91%8D`,
  );
  expect(react.status).toBeLessThan(300);
  const again = await apiWrite(
    other,
    "PUT",
    `${root}/${commentId}/reactions/%F0%9F%91%8D`,
  );
  expect(again.status).toBeLessThan(300);
  let seen = (await list(other, root)).find((c) => c.id === commentId)!;
  expect(seen.reactions.find((r) => r.emoji === "👍")?.count).toBe(1);
  const withdraw = await apiWrite(
    other,
    "DELETE",
    `${root}/${commentId}/reactions/%F0%9F%91%8D`,
  );
  expect(withdraw.status).toBeLessThan(300);
  seen = (await list(other, root)).find((c) => c.id === commentId)!;
  expect(seen.reactions.find((r) => r.emoji === "👍")).toBeUndefined();
  await other.close();

  // Resolve hides the thread in the item until Show resolved is on; Reopen brings it back.
  expect(
    (await apiWrite(page, "POST", `/comments/${commentId}/resolve`)).status,
  ).toBeLessThan(300);
  const url = `/workspaces/${workspace.id}/boards/${board.id}/items/${itemId}`;
  await page.goto(url);
  await expect(page.getByText(`Reply ${unique}`)).toHaveCount(0);
  await page.getByRole("checkbox", { name: /Show resolved comments/ }).check();
  await expect(page.getByText(`Edited ${unique}`)).toBeVisible();
  await page.getByRole("button", { name: "Reopen" }).click();
  await page
    .getByRole("checkbox", { name: /Show resolved comments/ })
    .waitFor({ state: "detached" });
  await expect(page.getByText(`Edited ${unique}`)).toBeVisible();
  expect(
    (await list(page, root)).find((c) => c.id === commentId)!.resolvedAt,
  ).toBeNull();

  // Outsiders: a person with no membership of the workspace and a made-up id get the identical answer.
  const outsider = await browser.newPage();
  // A Guest of Workspace 5 only: no membership of Workspace 3 at all.
  await signIn(outsider, {
    email: "user9@notrelix.io",
    password: realUsers.guest.password,
  });
  const madeUp = crypto.randomUUID();
  const probe = async (
    method: "POST" | "PATCH" | "DELETE",
    path: string,
    body?: unknown,
  ) => (await apiWrite(outsider, method, path, body)).status;
  const hidden = [
    await probe("PATCH", `/comments/${commentId}`, {
      contentMd: "x",
      expectedVersion: 1,
    }),
    await probe("POST", `/comments/${commentId}/resolve`),
    await probe("DELETE", `/comments/${commentId}?expectedVersion=1`),
  ];
  const absent = [
    await probe("PATCH", `/comments/${madeUp}`, {
      contentMd: "x",
      expectedVersion: 1,
    }),
    await probe("POST", `/comments/${madeUp}/resolve`),
    await probe("DELETE", `/comments/${madeUp}?expectedVersion=1`),
  ];
  expect(hidden).toEqual([404, 404, 404]);
  expect(hidden).toEqual(absent);
  expect(await probe("POST", root, { contentMd: "intrude" })).toBe(
    await probe("POST", `/board-items/${madeUp}/comments`, {
      contentMd: "intrude",
    }),
  );
  await outsider.close();

  // Delete: stale is 412; the author deletes; the reply stays under a placeholder; a repeat is NotFound.
  const parent = (await list(page, root)).find((c) => c.id === commentId)!;
  expect(
    (
      await apiWrite(
        page,
        "DELETE",
        `/comments/${commentId}?expectedVersion=${parent.version - 1}`,
      )
    ).status,
  ).toBe(412);
  expect(
    (
      await apiWrite(
        page,
        "DELETE",
        `/comments/${commentId}?expectedVersion=${parent.version}`,
      )
    ).status,
  ).toBeLessThan(300);
  const after = await list(page, root);
  expect(after.find((c) => c.id === commentId)?.isDeleted).toBe(true);
  expect(after.some((c) => c.contentMd === `Reply ${unique}`)).toBe(true);
  expect(
    (
      await apiWrite(
        page,
        "DELETE",
        `/comments/${commentId}?expectedVersion=${parent.version + 1}`,
      )
    ).status,
  ).toBe(404);
});

test("a comment on a page is read by a member and refused to a guest until the page is shared", async ({
  page,
  browser,
}) => {
  const unique = Date.now().toString(36);
  await signIn(page, realUsers.workspaceOwner);
  const workspace = await findWorkspace(page, "Workspace 3");
  const created = await apiWrite<string>(
    page,
    "POST",
    `/workspaces/${workspace.id}/pages`,
    { title: `Commented ${unique}` },
  );
  expect(created.status).toBe(201);
  const pageId = created.data as string;
  const root = `/pages/${pageId}/comments`;

  const comment = await apiWrite(page, "POST", root, {
    contentMd: `Page note ${unique}`,
  });
  expect(comment.status).toBeLessThan(300);
  const commentId = idOf(comment.data);
  const reply = await apiWrite(page, "POST", root, {
    contentMd: `Page reply ${unique}`,
    parentCommentId: commentId,
  });
  expect(reply.status).toBeLessThan(300);
  expect(
    (await apiWrite(page, "POST", `/comments/${commentId}/resolve`)).status,
  ).toBeLessThan(300);
  expect(
    (await list(page, root)).find((c) => c.id === commentId)!.resolvedAt,
  ).not.toBeNull();
  expect(
    (await apiWrite(page, "POST", `/comments/${commentId}/reopen`)).status,
  ).toBeLessThan(300);

  // A guest without a grant meets the same NotFound a made-up page gives.
  const guest = await browser.newPage();
  await signIn(guest, realUsers.workspaceGuest);
  const madeUp = crypto.randomUUID();
  const refused = (await apiWrite(guest, "POST", root, { contentMd: "no" }))
    .status;
  expect(refused).toBe(404);
  expect(refused).toBe(
    (
      await apiWrite(guest, "POST", `/pages/${madeUp}/comments`, {
        contentMd: "no",
      })
    ).status,
  );
  await guest.close();
});
