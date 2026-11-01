import { expect, test, type Page } from "@playwright/test";
import {
  apiRead,
  apiWrite,
  findWorkspace,
  realUsers,
  signIn,
} from "./support/real-session";

test.use({ viewport: { width: 1440, height: 900 } });
test.setTimeout(180_000);

const apiUrl = process.env.VITE_API_URL ?? "http://127.0.0.1:58000/api/v1";

const FILE_MENU = /^(Tệp|File)$/;
const MOVE_TO_TRASH = /^(Chuyển vào thùng rác|Move to trash)$/;
const OPEN_TRASH = /^(Thùng rác|Trash)$/;

async function createDoc(
  page: Page,
  workspaceId: string,
  title: string,
): Promise<string> {
  const created = await apiWrite<string>(
    page,
    "POST",
    `/workspaces/${workspaceId}/pages`,
    { title },
  );
  expect(created.status).toBe(201);
  return created.data as string;
}

async function openFileMenu(page: Page) {
  await page.getByRole("menuitem", { name: FILE_MENU }).click();
}

test("a page moves to the trash, is restored from it, then deleted for good from the trash list", async ({
  page,
}) => {
  const stamp = Date.now();
  await signIn(page, realUsers.workspaceOwner);
  const workspace = await findWorkspace(page, "Workspace 3");
  const trashed = await createDoc(page, workspace.id, `Trash me ${stamp}`);
  const keeper = await createDoc(page, workspace.id, `Keeper ${stamp}`);

  // Move to trash from the File menu: the page leaves the readable pages.
  await page.goto(`/workspaces/${workspace.id}/docs/${trashed}`);
  await expect(page.locator("article h1")).toHaveText(`Trash me ${stamp}`);
  await openFileMenu(page);
  const archived = page.waitForResponse(
    (response) =>
      response.request().method() === "POST" &&
      response.url().endsWith(`/pages/${trashed}/archive`),
  );
  await page.getByRole("menuitem", { name: MOVE_TO_TRASH }).click();
  expect((await archived).status()).toBeLessThan(300);
  expect((await page.request.get(`${apiUrl}/pages/${trashed}`)).status()).toBe(
    404,
  );

  // The trash list shows it; Restore brings it back.
  await page.goto(`/workspaces/${workspace.id}/docs/${keeper}`);
  await openFileMenu(page);
  await page.getByRole("menuitem", { name: OPEN_TRASH }).click();
  const dialog = page.getByRole("dialog");
  await expect(dialog.getByText(`Trash me ${stamp}`)).toBeVisible();
  const restored = page.waitForResponse(
    (response) =>
      response.request().method() === "POST" &&
      response.url().endsWith(`/pages/${trashed}/restore`),
  );
  await dialog
    .getByRole("button", { name: new RegExp(`Trash me ${stamp}`) })
    .first()
    .click();
  expect((await restored).status()).toBeLessThan(300);
  await expect(dialog.getByText(`Trash me ${stamp}`)).toHaveCount(0);
  const back = await apiRead<{ id: string }>(page, `/pages/${trashed}`);
  expect(back.id).toBe(trashed);

  // Trash it again and delete it permanently (a confirm step guards it).
  const second = await apiWrite(page, "POST", `/pages/${trashed}/archive`, {});
  expect(second.status).toBeLessThan(300);
  await page.reload();
  await openFileMenu(page);
  await page.getByRole("menuitem", { name: OPEN_TRASH }).click();
  const list = page.getByRole("dialog");
  await expect(list.getByText(`Trash me ${stamp}`)).toBeVisible();
  await list
    .getByRole("button", {
      name: new RegExp(`(Xoá vĩnh viễn|Delete permanently): Trash me ${stamp}`),
    })
    .click();
  const deleted = page.waitForResponse(
    (response) =>
      response.request().method() === "DELETE" &&
      response.url().endsWith(`/pages/${trashed}`),
  );
  await list
    .getByRole("button", {
      name: /^(Xác nhận xoá vĩnh viễn|Confirm permanent delete)$/,
    })
    .click();
  expect((await deleted).status()).toBeLessThan(300);
  const gone = await page.request.get(`${apiUrl}/pages/${trashed}`);
  expect(gone.status()).toBe(404);
});

test("a page reference names another page, and that page lists the referrer as a backlink", async ({
  page,
}) => {
  const stamp = Date.now();
  await signIn(page, realUsers.workspaceOwner);
  const workspace = await findWorkspace(page, "Workspace 3");
  const target = await createDoc(page, workspace.id, `Target ${stamp}`);
  const referrer = await createDoc(page, workspace.id, `Referrer ${stamp}`);

  const block = await apiWrite<string>(
    page,
    "POST",
    `/pages/${referrer}/blocks`,
    {
      type: "PageReference",
      properties: { pageId: target },
    },
  );
  expect(block.status).toBe(201);

  // The referrer renders the reference with the target's name as a link.
  await page.goto(`/workspaces/${workspace.id}/docs/${referrer}`);
  await expect(
    page.getByRole("link", { name: new RegExp(`Target ${stamp}`) }),
  ).toBeVisible();

  // The producer derives the backlink; the target's links panel lists the referrer.
  const backlinks = await apiRead<{ docs: { id: string; title: string }[] }>(
    page,
    `/pages/${target}/backlinks`,
  );
  expect(backlinks.docs.map((doc) => doc.id)).toEqual([referrer]);
  await page.goto(`/workspaces/${workspace.id}/docs/${target}`);
  await expect(page.getByText(`Referrer ${stamp}`)).toBeVisible();

  // Retyping a block keeps its id and content carries over between text types.
  const text = await apiWrite<{ id: string; version: number }>(
    page,
    "POST",
    `/pages/${target}/blocks`,
    {
      type: "Text",
      content: { v: 1, runs: [{ text: "Keep this text", marks: [] }] },
    },
  );
  expect(text.status).toBe(201);
  const blockId = text.data?.id as string;
  const retyped = await apiWrite(
    page,
    "PATCH",
    `/pages/${target}/blocks/${blockId}`,
    { type: "callout", expectedVersion: text.data?.version },
  );
  expect(retyped.status).toBeLessThan(300);
  const blocks = await apiRead<
    { id: string; type: string; content: { runs: { text: string }[] } }[]
  >(page, `/pages/${target}/blocks`);
  const kept = blocks.find((entry) => entry.id === blockId);
  expect(kept?.type).toBe("Callout");
  expect(kept?.content.runs.map((run) => run.text).join("")).toBe(
    "Keep this text",
  );
});
