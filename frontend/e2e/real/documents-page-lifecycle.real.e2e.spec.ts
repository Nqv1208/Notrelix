import { expect, test, type Page } from "@playwright/test";
import {
  apiRead,
  apiWrite,
  findWorkspace,
  realUsers,
  signIn,
} from "./support/real-session";

test.setTimeout(240_000);

type PageRow = { id: string; title: string; version: number };
type Crumb = { id?: string | null; title?: string | null };

const text = (value: string) => ({ v: 1, runs: [{ text: value, marks: [] }] });

async function createDoc(page: Page, workspaceId: string, body: object) {
  return apiWrite<string>(
    page,
    "POST",
    `/workspaces/${workspaceId}/pages`,
    body,
  );
}

test("a page is created once per key, renamed against its version, shown under its parents, and an outsider gets the same not found", async ({
  browser,
  page,
}) => {
  const stamp = Date.now();
  await signIn(page, realUsers.workspaceOwner);
  const workspace = await findWorkspace(page, "Workspace 3");

  // Create: the same key and body answers the same page and stores one; the same key with another body is refused.
  const key = crypto.randomUUID();
  const body = { title: `Lifecycle ${stamp}` };
  const first = await apiWrite<string>(
    page,
    "POST",
    `/workspaces/${workspace.id}/pages`,
    body,
    key,
  );
  expect(first.status).toBe(201);
  const replay = await apiWrite<string>(
    page,
    "POST",
    `/workspaces/${workspace.id}/pages`,
    body,
    key,
  );
  expect(replay.status).toBeLessThan(300);
  expect(replay.data).toBe(first.data);
  const clash = await apiWrite(
    page,
    "POST",
    `/workspaces/${workspace.id}/pages`,
    { title: `Other ${stamp}` },
    key,
  );
  expect(clash.status).toBeGreaterThanOrEqual(400);
  expect(clash.status).toBeLessThan(500);
  const docId = first.data as string;

  // A title out of bounds is refused.
  expect((await createDoc(page, workspace.id, { title: "" })).status).toBe(400);

  // Rename against the version read: +1; the same title is a no-op; a stale version is 412 and changes nothing.
  const read = await apiRead<PageRow>(page, `/pages/${docId}`);
  const renamed = await apiWrite(page, "PATCH", `/pages/${docId}`, {
    title: `Renamed ${stamp}`,
    expectedVersion: read.version,
  });
  expect(renamed.status).toBeLessThan(300);
  const after = await apiRead<PageRow>(page, `/pages/${docId}`);
  expect(after.title).toBe(`Renamed ${stamp}`);
  expect(after.version).toBe(read.version + 1);
  const noop = await apiWrite(page, "PATCH", `/pages/${docId}`, {
    title: `Renamed ${stamp}`,
    expectedVersion: after.version,
  });
  expect(noop.status).toBeLessThan(300);
  expect((await apiRead<PageRow>(page, `/pages/${docId}`)).version).toBe(
    after.version,
  );
  const stale = await apiWrite(page, "PATCH", `/pages/${docId}`, {
    title: `Stale ${stamp}`,
    expectedVersion: read.version,
  });
  expect(stale.status).toBe(412);
  expect((await apiRead<PageRow>(page, `/pages/${docId}`)).title).toBe(
    `Renamed ${stamp}`,
  );

  // Breadcrumb: root, parent, page.
  const parent = (
    await createDoc(page, workspace.id, { title: `Parent ${stamp}` })
  ).data as string;
  const child = (
    await createDoc(page, workspace.id, {
      title: `Child ${stamp}`,
      parentId: parent,
    })
  ).data as string;
  const crumbs = await apiRead<Crumb[]>(page, `/pages/${child}/breadcrumb`);
  expect(crumbs.map((c) => c.id)).toEqual([parent, child]);

  // An outsider (a guest, and a member of another workspace) gets the identical not found.
  const guestContext = await browser.newContext();
  const guest = await guestContext.newPage();
  await signIn(guest, realUsers.workspaceGuest);
  const missing = await apiRead<unknown>(
    guest,
    `/pages/${crypto.randomUUID()}`,
  ).catch((error: Error) => error.message);
  const hidden = await apiRead<unknown>(guest, `/pages/${docId}`).catch(
    (error: Error) => error.message,
  );
  expect(hidden).toBe(missing);
  const guestRename = await apiWrite(guest, "PATCH", `/pages/${docId}`, {
    title: "nope",
    expectedVersion: after.version + 1,
  });
  expect(guestRename.status).toBe(404);
  const outsiderCreate = await apiWrite(
    guest,
    "POST",
    `/workspaces/${crypto.randomUUID()}/pages`,
    { title: "nope" },
  );
  expect(outsiderCreate.status).toBe(404);
  await guestContext.close();
});

test("a page of 1 100 blocks opens its first 1 000 at once and the web reads the rest", async ({
  page,
}) => {
  const stamp = Date.now();
  await signIn(page, realUsers.workspaceOwner);
  const workspace = await findWorkspace(page, "Workspace 3");
  const doc = (await createDoc(page, workspace.id, { title: `Big ${stamp}` }))
    .data as string;

  let previous: string | undefined;
  for (let batch = 0; batch < 3; batch += 1) {
    const items = Array.from({ length: batch < 2 ? 400 : 300 }, (_, i) => {
      const id = crypto.randomUUID();
      const item = {
        op: "upsert",
        id,
        type: "Text",
        content: text(`line ${batch}-${i}`),
        ...(previous ? { afterBlockId: previous } : {}),
      };
      previous = id;
      return item;
    });
    const saved = await apiWrite(page, "POST", `/pages/${doc}/blocks/batch`, {
      blocks: items,
    });
    expect(saved.status).toBeLessThan(300);
  }

  const window1 = await apiRead<{
    items: { id: string }[];
    nextCursor: string | null;
  }>(page, `/pages/${doc}/blocks`);
  expect(window1.items).toHaveLength(1000);
  expect(window1.nextCursor).toBeTruthy();
  const window2 = await apiRead<{
    items: { id: string }[];
    nextCursor: string | null;
  }>(
    page,
    `/pages/${doc}/blocks?cursor=${encodeURIComponent(window1.nextCursor!)}`,
  );
  expect(window2.items).toHaveLength(100);
  expect(window2.nextCursor).toBeNull();
  const tooMany = await page.request.get(
    `${process.env.VITE_API_URL ?? "http://127.0.0.1:58000/api/v1"}/pages/${doc}/blocks?limit=1001`,
  );
  expect(tooMany.status()).toBe(400);

  // The web shows the last line, which sits in the second window.
  await page.goto(`/workspaces/${workspace.id}/docs/${doc}`);
  await expect(page.getByText("line 2-299")).toBeVisible({ timeout: 30_000 });
});

test("an editor who loses the connection sees that changes are not saved and gets them saved on return", async ({
  page,
}) => {
  const stamp = Date.now();
  await signIn(page, realUsers.workspaceOwner);
  const workspace = await findWorkspace(page, "Workspace 3");
  const doc = (
    await createDoc(page, workspace.id, { title: `Offline ${stamp}` })
  ).data as string;
  await page.goto(`/workspaces/${workspace.id}/docs/${doc}`);
  await expect(page.locator("article h1")).toHaveText(`Offline ${stamp}`);

  await page.context().setOffline(true);
  await expect(
    page.getByText(
      /Ngoại tuyến, thay đổi chưa được lưu|Offline, changes not saved yet/,
    ),
  ).toBeVisible();

  await page.context().setOffline(false);
  await expect(
    page.getByText(
      /Ngoại tuyến, thay đổi chưa được lưu|Offline, changes not saved yet/,
    ),
  ).toHaveCount(0);

  const line = `back online ${stamp}`;
  const saved = page.waitForResponse(
    (response) =>
      response.request().method() === "POST" &&
      /\/pages\/[^/]+\/blocks\/batch$/.test(response.url()),
  );
  await page
    .getByRole("textbox", { name: /Type \/ to insert a block|Nhập/ })
    .click();
  await page.keyboard.type(line);
  await page.locator("article h1").click();
  expect((await saved).status()).toBeLessThan(300);
  await page.reload();
  await expect(page.getByText(line)).toBeVisible();
});
