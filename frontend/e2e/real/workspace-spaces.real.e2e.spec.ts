import { expect, test, type Page } from "@playwright/test";
import { apiRead, apiWrite, realUsers, signIn } from "./support/real-session";
import { createOwnedWorkspace, joinAs } from "./support/real-workspace";

test.setTimeout(240_000);

const apiUrl = process.env.VITE_API_URL ?? "http://127.0.0.1:58000/api/v1";

interface SpaceRow {
  readonly id: string;
  readonly name: string;
  readonly visibility?: string;
  readonly spaceType?: string;
  readonly type?: string;
}
interface Paged<T> {
  readonly items: readonly T[];
  readonly nextCursor: string | null;
}
interface BoardRow {
  readonly id: string;
  readonly title: string;
  readonly version: number;
  readonly spaceId: string | null;
}

async function walk<T>(page: Page, path: string, size: number) {
  const rows: T[] = [];
  let pages = 0;
  let cursor: string | null = null;
  do {
    const query: string = `limit=${size}${cursor ? `&cursor=${encodeURIComponent(cursor)}` : ""}`;
    const read: Paged<T> = await apiRead<Paged<T>>(page, `${path}?${query}`);
    rows.push(...read.items);
    cursor = read.nextCursor;
    pages += 1;
  } while (cursor);
  return { rows, pages };
}

test("an Admin creates a Department from the tree; a clash, a different body under one key, a Member and a foreign parent create nothing, and a retry makes one", async ({
  browser,
  page,
}) => {
  await signIn(page, realUsers.admin);
  const workspace = await createOwnedWorkspace(page, "Spaces create");
  const member = await joinAs(
    browser,
    page,
    workspace.id,
    realUsers.workspaceAdmin,
    "Member",
  );
  const spacesPath = `/workspaces/${workspace.id}/spaces`;

  // K01 main flow: the tree's Create department button opens the dialog; a Department is Private.
  await page.goto(`/workspaces/${workspace.id}`);
  const nav = page.getByRole("navigation", { name: "Workspace navigation" });
  await nav.getByRole("button", { name: "Create department" }).click();
  const create = page.getByRole("dialog", { name: "New Space" });
  await create.getByLabel("Name").fill("IT");
  await expect(create.getByRole("radio", { name: /Private/ })).toBeChecked();
  await create.getByRole("button", { name: "Create Space" }).click();
  await expect(
    page.getByRole("heading", { level: 1, name: /^IT/ }),
  ).toBeVisible();
  await expect(
    nav
      .getByRole("tree", { name: "Departments" })
      .getByRole("treeitem", { name: /^IT\b.*Private/ }),
  ).toBeVisible();
  const { rows: made } = await walk<SpaceRow>(page, spacesPath, 50);
  const it = made.find((space) => space.name === "IT");
  expect(it, "the new Space is listed").toBeDefined();
  expect(it?.visibility).toBe("Private");

  // K01 rejection: a sibling with the same name in another case is refused and adds no row.
  const clash = await apiWrite(page, "POST", spacesPath, { name: "it" });
  expect([400, 409]).toContain(clash.status);
  // A Member lacks ManageSpaces.
  const byMember = await apiWrite(member.page, "POST", spacesPath, {
    name: "By member",
  });
  expect(byMember.status).toBe(403);
  // A parent that is not in this workspace is refused before anything is written.
  const foreign = await apiWrite(page, "POST", spacesPath, {
    name: "Under nothing",
    parentSpaceId: "00000000-0000-4000-8000-0000000000bb",
  });
  expect(foreign.status).toBeGreaterThanOrEqual(400);
  expect(foreign.status).toBeLessThan(500);

  // K01 retry: one key, one Space; another body under the same key is refused.
  const key = crypto.randomUUID();
  const once = await apiWrite<string>(
    page,
    "POST",
    spacesPath,
    { name: "Finance" },
    key,
  );
  const again = await apiWrite<string>(
    page,
    "POST",
    spacesPath,
    { name: "Finance" },
    key,
  );
  expect(once.status).toBe(201);
  expect(again.status).toBeLessThan(300);
  expect(again.data).toBe(once.data);
  const other = await apiWrite(
    page,
    "POST",
    spacesPath,
    { name: "Legal" },
    key,
  );
  expect(other.status).toBeGreaterThanOrEqual(400);
  expect(other.status).toBeLessThan(500);
  const { rows: after } = await walk<SpaceRow>(page, spacesPath, 50);
  expect(after.filter((space) => space.name === "Finance")).toHaveLength(1);
  expect(after.some((space) => space.name === "Legal")).toBe(false);
  expect(after.some((space) => space.name === "By member")).toBe(false);
  await member.context.close();
});

test("the Space tree and list show a Member only what they can reach, in pages; and a Board is placed in a Space only of its own workspace", async ({
  browser,
  page,
}) => {
  await signIn(page, realUsers.admin);
  const workspace = await createOwnedWorkspace(page, "Spaces read");
  const member = await joinAs(
    browser,
    page,
    workspace.id,
    realUsers.workspaceAdmin,
    "Member",
  );
  const spacesPath = `/workspaces/${workspace.id}/spaces`;
  const makeSpace = async (name: string, visibility: string) => {
    const made = await apiWrite<string>(page, "POST", spacesPath, {
      name,
      visibility,
      spaceType: "Folder",
    });
    expect(made.status, `create ${name}`).toBe(201);
    return made.data as string;
  };
  const open = await makeSpace("Open space", "Workspace");
  const secret = await makeSpace("Secret space", "Private");
  const archived = await makeSpace("Archived space", "Workspace");

  // K02: the Owner reads both; the Member's tree and list leave out the Private one, and nothing counts it.
  const ownerList = await walk<SpaceRow>(page, spacesPath, 50);
  expect(ownerList.rows.map((row) => row.id)).toEqual(
    expect.arrayContaining([open, secret]),
  );
  const memberList = await walk<SpaceRow>(member.page, spacesPath, 50);
  expect(memberList.rows.map((row) => row.id)).toContain(open);
  expect(memberList.rows.map((row) => row.id)).not.toContain(secret);
  const memberTree = await walk<SpaceRow>(
    member.page,
    `${spacesPath}/tree`,
    50,
  );
  expect(memberTree.rows.map((row) => row.id)).toContain(open);
  expect(memberTree.rows.map((row) => row.id)).not.toContain(secret);
  expect(
    (
      await member.page.request.get(`${apiUrl}${spacesPath}/${secret}`)
    ).status(),
  ).toBe(404);

  // Pages: every Space once at any page size; sizes and cursors the producer did not issue are refused.
  const small = await walk<SpaceRow>(page, spacesPath, 1);
  expect(small.rows.map((row) => row.id).sort()).toEqual(
    ownerList.rows.map((row) => row.id).sort(),
  );
  expect(small.pages).toBe(ownerList.rows.length);
  for (const path of [spacesPath, `${spacesPath}/tree`]) {
    for (const bad of ["limit=0", "limit=201", "cursor=not-a-cursor"]) {
      expect((await page.request.get(`${apiUrl}${path}?${bad}`)).status()).toBe(
        400,
      );
    }
  }

  // The UI shows the Member the open Space and not the Private one.
  await member.page.goto(`/workspaces/${workspace.id}`);
  const tree = member.page
    .getByRole("navigation", { name: "Workspace navigation" })
    .getByRole("tree", { name: "Departments" });
  await expect(
    tree.getByRole("treeitem", { name: /^Open space/ }),
  ).toBeVisible();
  await expect(
    tree.getByRole("treeitem", { name: /^Secret space/ }),
  ).toHaveCount(0);

  // K15: a Board created in a Space is placed there; the Space fact refuses a Space of another workspace
  // and an archived one, and the Board stays where it was.
  const board = await apiWrite<string>(
    page,
    "POST",
    `/workspaces/${workspace.id}/boards`,
    { title: "Placed board", spaceId: open },
  );
  expect(board.status).toBe(201);
  const boardOf = async () =>
    (
      await apiRead<BoardRow[]>(page, `/workspaces/${workspace.id}/boards`)
    ).find((row) => row.id === board.data) as BoardRow;
  const placed = await boardOf();
  expect(placed.spaceId).toBe(open);

  const other = await createOwnedWorkspace(page, "Spaces foreign");
  const foreignSpace = (
    await apiWrite<string>(page, "POST", `/workspaces/${other.id}/spaces`, {
      name: "Elsewhere",
      visibility: "Workspace",
      spaceType: "Folder",
    })
  ).data as string;
  const toForeign = await apiWrite(
    page,
    "POST",
    `/boards/${placed.id}/move-to-space`,
    { spaceId: foreignSpace, expectedVersion: placed.version },
  );
  expect(toForeign.status).toBeGreaterThanOrEqual(400);
  expect(toForeign.status).toBeLessThan(500);
  expect((await boardOf()).spaceId).toBe(open);

  const archive = await apiRead<{ version: number }>(
    page,
    `${spacesPath}/${archived}`,
  );
  expect(
    (
      await apiWrite(page, "POST", `${spacesPath}/${archived}/archive`, {
        expectedVersion: archive.version,
      })
    ).status,
  ).toBeLessThan(300);
  const toArchived = await apiWrite(
    page,
    "POST",
    `/boards/${placed.id}/move-to-space`,
    { spaceId: archived, expectedVersion: placed.version },
  );
  expect(toArchived.status).toBeGreaterThanOrEqual(400);
  expect(toArchived.status).toBeLessThan(500);
  expect((await boardOf()).spaceId).toBe(open);

  // Main flow of the placement: a Space of its own workspace takes it, and the Board reads there.
  const moved = await apiWrite(
    page,
    "POST",
    `/boards/${placed.id}/move-to-space`,
    { spaceId: secret, expectedVersion: placed.version },
  );
  expect(moved.status).toBeLessThan(300);
  expect((await boardOf()).spaceId).toBe(secret);
  await member.context.close();
});
