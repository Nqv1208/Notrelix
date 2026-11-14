import { expect, test } from "@playwright/test";
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

const idOf = (data: unknown) =>
  typeof data === "string" ? data : (data as { id: string }).id;
const thumbs = "%F0%9F%91%8D";

test("a reply to a comment of another item, a made-up reply parent and a reaction outside the palette are refused and store nothing", async ({
  page,
}) => {
  const unique = Date.now().toString(36);
  await signIn(page, realUsers.workspaceAdmin);
  const { board } = await openBoard(page, "Workspace 3", "Engineering Tasks");
  const groupId = (
    await apiRead<{ lists: { id: string }[] }>(page, `/boards/${board.id}/full`)
  ).lists[0]!.id;
  const newItem = async (title: string) =>
    (
      await apiWrite<{ id: string }>(
        page,
        "POST",
        `/boards/${board.id}/items`,
        {
          groupId,
          title: `${title} ${unique}`,
        },
      )
    ).data!.id;
  const itemA = await newItem("Reply A");
  const itemB = await newItem("Reply B");
  const rootA = `/board-items/${itemA}/comments`;
  const rootB = `/board-items/${itemB}/comments`;
  const onB = idOf(
    (await apiWrite(page, "POST", rootB, { contentMd: `On B ${unique}` })).data,
  );
  const onA = idOf(
    (await apiWrite(page, "POST", rootA, { contentMd: `On A ${unique}` })).data,
  );
  const count = async (root: string) =>
    (
      await apiRead<unknown[] | { items: unknown[] }>(page, root).then((r) =>
        Array.isArray(r) ? r : r.items,
      )
    ).length;
  const before = await count(rootA);

  // The parent sits on another item: refused, nothing stored on either item.
  const crossTarget = await apiWrite(page, "POST", rootA, {
    contentMd: "wrong thread",
    parentCommentId: onB,
  });
  expect(crossTarget.status).toBeGreaterThanOrEqual(400);
  expect(crossTarget.status).toBeLessThan(500);
  expect(JSON.stringify(crossTarget.data)).toMatch(/ParentMustBeInSameTarget/i);
  // A parent that does not exist is the same NotFound as any made-up id.
  expect(
    (
      await apiWrite(page, "POST", rootA, {
        contentMd: "no parent",
        parentCommentId: crypto.randomUUID(),
      })
    ).status,
  ).toBe(404);
  expect(await count(rootA)).toBe(before);
  expect(await count(rootB)).toBe(1);

  // A reply to a deleted comment that has no replies is gone, so it is NotFound too.
  const lone = idOf(
    (await apiWrite(page, "POST", rootA, { contentMd: `Lone ${unique}` })).data,
  );
  const loneVersion = (
    await apiRead<{ id: string; version: number }[]>(page, rootA).then((r) =>
      Array.isArray(r) ? r : (r as unknown as { items: typeof r }).items,
    )
  ).find((c) => c.id === lone)!.version;
  expect(
    (
      await apiWrite(
        page,
        "DELETE",
        `/comments/${lone}?expectedVersion=${loneVersion}`,
      )
    ).status,
  ).toBeLessThan(300);
  expect(
    (
      await apiWrite(page, "POST", rootA, {
        contentMd: "too late",
        parentCommentId: lone,
      })
    ).status,
  ).toBeGreaterThanOrEqual(400);

  // Reactions: outside the palette is 400; a comment of another item is NotFound; the palette still works.
  expect(
    (await apiWrite(page, "PUT", `${rootA}/${onA}/reactions/%F0%9F%A5%94`))
      .status,
  ).toBe(400);
  expect(
    (await apiWrite(page, "PUT", `${rootA}/${onB}/reactions/${thumbs}`)).status,
  ).toBe(404);
  expect(
    (await apiWrite(page, "DELETE", `${rootA}/${onB}/reactions/${thumbs}`))
      .status,
  ).toBe(404);
  expect(
    (await apiWrite(page, "PUT", `${rootA}/${onA}/reactions/${thumbs}`)).status,
  ).toBeLessThan(300);
  // Withdrawing a reaction that is not there is a no-op, not an error.
  expect(
    (
      await apiWrite(
        page,
        "DELETE",
        `${rootA}/${onA}/reactions/%E2%9D%A4%EF%B8%8F`,
      )
    ).status,
  ).toBeLessThan(300);
});

test("a viewer reads item and page comments and every write is refused with 403", async ({
  page,
  browser,
}) => {
  const unique = Date.now().toString(36);
  await signIn(page, realUsers.workspaceOwner);
  const workspace = await findWorkspace(page, "Workspace 3");
  const { board } = await openBoard(page, "Workspace 3", "Engineering Tasks");
  const groupId = (
    await apiRead<{ lists: { id: string }[] }>(page, `/boards/${board.id}/full`)
  ).lists[0]!.id;
  const itemId = (
    await apiWrite<{ id: string }>(page, "POST", `/boards/${board.id}/items`, {
      groupId,
      title: `Viewer ${unique}`,
    })
  ).data!.id;
  const pageId = (
    await apiWrite<string>(page, "POST", `/workspaces/${workspace.id}/pages`, {
      title: `Viewer page ${unique}`,
    })
  ).data as string;
  const itemRoot = `/board-items/${itemId}/comments`;
  const pageRoot = `/pages/${pageId}/comments`;
  const itemComment = idOf(
    (await apiWrite(page, "POST", itemRoot, { contentMd: `Item ${unique}` }))
      .data,
  );
  const pageComment = idOf(
    (await apiWrite(page, "POST", pageRoot, { contentMd: `Page ${unique}` }))
      .data,
  );

  const viewerContext = await browser.newContext();
  const viewer = await viewerContext.newPage();
  await signIn(viewer, realUsers.workspaceGuest);
  const me = await apiRead<{ id: string }>(viewer, "/auth/me");
  const grants: { type: string; id: string; row: string }[] = [];
  try {
    for (const [type, id] of [
      ["work-management.board", board.id],
      ["documents.page", pageId],
    ] as const) {
      const granted = await apiWrite<{ id: string }>(
        page,
        "POST",
        `/resources/${type}/${id}/permissions`,
        { subjectType: "User", subjectId: me.id, level: "Viewer" },
      );
      expect(granted.status).toBeLessThan(300);
    }
    for (const [type, id] of [
      ["work-management.board", board.id],
      ["documents.page", pageId],
    ] as const) {
      const rows = await apiRead<
        { id: string; subjectId: string; isRevoked: boolean }[]
      >(page, `/resources/${type}/${id}/permissions`);
      for (const r of rows.filter((x) => x.subjectId === me.id && !x.isRevoked))
        grants.push({ type, id, row: r.id });
    }

    // Reads succeed.
    for (const [root, id] of [
      [itemRoot, itemComment],
      [pageRoot, pageComment],
    ] as const) {
      const read = await apiRead<
        { id: string }[] | { items: { id: string }[] }
      >(viewer, root);
      const rows = Array.isArray(read) ? read : read.items;
      expect(rows.map((c) => c.id)).toContain(id);
    }

    // Writes: comment, reply, react, withdraw, resolve, each a 403 on both targets.
    for (const [root, id] of [
      [itemRoot, itemComment],
      [pageRoot, pageComment],
    ] as const) {
      expect(
        (await apiWrite(viewer, "POST", root, { contentMd: "viewer note" }))
          .status,
      ).toBe(403);
      expect(
        (
          await apiWrite(viewer, "POST", root, {
            contentMd: "viewer reply",
            parentCommentId: id,
          })
        ).status,
      ).toBe(403);
      expect(
        (await apiWrite(viewer, "POST", `/comments/${id}/resolve`)).status,
      ).toBe(403);
    }
    expect(
      (
        await apiWrite(
          viewer,
          "PUT",
          `${itemRoot}/${itemComment}/reactions/${thumbs}`,
        )
      ).status,
    ).toBe(403);

    // Nothing was stored, and the item shows no composer to the viewer.
    const after = await apiRead<
      { contentMd: string }[] | { items: { contentMd: string }[] }
    >(page, itemRoot);
    const afterRows = Array.isArray(after) ? after : after.items;
    expect(afterRows.map((c) => c.contentMd)).toEqual([`Item ${unique}`]);
    await viewer.goto(
      `/workspaces/${workspace.id}/boards/${board.id}/items/${itemId}`,
    );
    await expect(viewer.getByText(`Item ${unique}`)).toBeVisible();
    await expect(viewer.getByPlaceholder("Add a comment...")).toHaveCount(0);
  } finally {
    for (const g of grants)
      await apiWrite(
        page,
        "DELETE",
        `/resources/${g.type}/${g.id}/permissions/${g.row}`,
      );
    await viewerContext.close();
  }
});

test("a member writes a page comment and a reply in the page's comments panel and a viewer is told they cannot comment", async ({
  page,
  browser,
}) => {
  const unique = Date.now().toString(36);
  await signIn(page, realUsers.workspaceOwner);
  const workspace = await findWorkspace(page, "Workspace 3");
  const pageId = (
    await apiWrite<string>(page, "POST", `/workspaces/${workspace.id}/pages`, {
      title: `Panel ${unique}`,
    })
  ).data as string;
  const url = `/workspaces/${workspace.id}/docs/${pageId}`;
  const openPanel = async (p: typeof page) => {
    await p.goto(url);
    await expect(p.locator("article h1")).toBeVisible();
    const tab = p.getByRole("tab", { name: /^Comments/ });
    if (!(await tab.isVisible()))
      await p
        .getByRole("button", { name: /^Comments/ })
        .first()
        .click();
    await tab.click();
  };

  await openPanel(page);
  await page
    .getByRole("textbox", { name: "Write a comment" })
    .fill(`Note ${unique}`);
  const sent = page.waitForResponse(
    (r) =>
      r.request().method() === "POST" &&
      /\/pages\/[^/]+\/comments$/.test(r.url()),
  );
  await page.getByRole("button", { name: "Send", exact: true }).click();
  expect((await sent).status()).toBeLessThan(300);
  await expect(page.getByText(`Note ${unique}`)).toBeVisible();

  // A reply sits under its thread and survives a reload.
  await page.getByRole("button", { name: "Reply", exact: true }).click();
  await page
    .getByRole("textbox", { name: "Reply to comment" })
    .fill(`Answer ${unique}`);
  await page.getByRole("textbox", { name: "Reply to comment" }).press("Enter");
  await expect(page.getByText(`Answer ${unique}`)).toBeVisible();
  await openPanel(page);
  await expect(page.getByText(`Answer ${unique}`)).toBeVisible();
  const listed = await apiRead<
    | { contentMd: string; parentCommentId: string | null }[]
    | { items: { contentMd: string; parentCommentId: string | null }[] }
  >(page, `/pages/${pageId}/comments`);
  const rows = Array.isArray(listed) ? listed : listed.items;
  expect(
    rows.find((c) => c.contentMd === `Answer ${unique}`)?.parentCommentId,
  ).toBeTruthy();

  // A viewer reads both and has no field, only the note.
  const viewerContext = await browser.newContext();
  const viewer = await viewerContext.newPage();
  await signIn(viewer, realUsers.workspaceGuest);
  const me = await apiRead<{ id: string }>(viewer, "/auth/me");
  const grant = await apiWrite(
    page,
    "POST",
    `/resources/documents.page/${pageId}/permissions`,
    { subjectType: "User", subjectId: me.id, level: "Viewer" },
  );
  expect(grant.status).toBeLessThan(300);
  try {
    await openPanel(viewer);
    await expect(viewer.getByText(`Note ${unique}`)).toBeVisible();
    await expect(viewer.getByText(`Answer ${unique}`)).toBeVisible();
    await expect(
      viewer.getByText("You can only view this page, so you cannot comment."),
    ).toBeVisible();
    await expect(
      viewer.getByRole("textbox", { name: "Write a comment" }),
    ).toHaveCount(0);
  } finally {
    const perms = await apiRead<
      { id: string; subjectId: string; isRevoked: boolean }[]
    >(page, `/resources/documents.page/${pageId}/permissions`);
    for (const r of perms.filter((x) => x.subjectId === me.id && !x.isRevoked))
      await apiWrite(
        page,
        "DELETE",
        `/resources/documents.page/${pageId}/permissions/${r.id}`,
      );
    await viewerContext.close();
  }
});
