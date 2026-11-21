import { expect, test } from "@playwright/test";
import {
  apiRead,
  apiWrite,
  findWorkspace,
  realUsers,
  signIn,
} from "./support/real-session";

test.setTimeout(240_000);

const FILE_MENU = /^(Tệp|File)$/;
const DUPLICATE_ITEM = /^(Tạo bản sao|Make a copy)$/;

type BlockRow = { id: string };

const text = (value: string) => ({
  v: 1,
  runs: [{ text: value, marks: [] }],
});

test("File, Make a copy duplicates the page without its subpage and with new block ids, a retry by key makes one copy, and a guest or an unknown page is refused", async ({
  browser,
  page,
}) => {
  const stamp = Date.now();
  await signIn(page, realUsers.workspaceOwner);
  await page.evaluate(() => localStorage.setItem("notrelix-locale", "vi"));
  const workspace = await findWorkspace(page, "Workspace 3");
  const base = `/workspaces/${workspace.id}/pages`;
  const source = (
    await apiWrite<string>(page, "POST", base, { title: `Plan ${stamp}` })
  ).data as string;
  const child = await apiWrite<string>(page, "POST", base, {
    title: `Child ${stamp}`,
    parentId: source,
  });
  expect(child.status).toBe(201);
  const block = await apiWrite<BlockRow>(
    page,
    "POST",
    `/pages/${source}/blocks`,
    { id: crypto.randomUUID(), type: "Text", content: text("agenda") },
  );
  expect(block.status).toBe(201);

  // The UI step: File, Make a copy opens the copy, titled in Vietnamese.
  await page.goto(`/workspaces/${workspace.id}/docs/${source}`);
  await page.getByRole("menuitem", { name: FILE_MENU }).click();
  await page.getByRole("menuitem", { name: DUPLICATE_ITEM }).click();
  await expect(page.locator("article h1")).toHaveText(
    `Plan ${stamp} (bản sao)`,
  );
  const copyId = page.url().split("/").pop()!;
  expect(copyId).not.toBe(source);
  const copied = await apiRead<{ items: BlockRow[] }>(
    page,
    `/pages/${copyId}/blocks`,
  );
  expect(copied.items).toHaveLength(1);
  expect(copied.items[0]!.id).not.toBe(block.data!.id);

  // The subpage was not copied: one page of that title, still under the source.
  const all = await apiRead<
    { items: { title: string }[] } | { title: string }[]
  >(page, `${base}`);
  const rows = Array.isArray(all) ? all : all.items;
  expect(rows.filter((p) => p.title === `Child ${stamp}`)).toHaveLength(1);

  // A retry by key returns the same copy; a new key makes another.
  const key = crypto.randomUUID();
  const path = `${base}/${source}/duplicate`;
  const first = await apiWrite<{ pageId: string }>(page, "POST", path, {}, key);
  const replay = await apiWrite<{ pageId: string }>(
    page,
    "POST",
    path,
    {},
    key,
  );
  expect(first.status).toBe(201);
  expect(replay.data).toEqual(first.data);
  const another = await apiWrite<{ pageId: string }>(page, "POST", path, {});
  expect(another.data!.pageId).not.toBe(first.data!.pageId);

  // Rejections: an unknown page is not found, and a guest member is refused as for create page.
  expect(
    (
      await apiWrite(
        page,
        "POST",
        `${base}/${crypto.randomUUID()}/duplicate`,
        {},
      )
    ).status,
  ).toBe(404);
  const guestContext = await browser.newContext();
  const guest = await guestContext.newPage();
  await signIn(guest, realUsers.workspaceGuest);
  const refused = await apiWrite(guest, "POST", path, {});
  expect(refused.status).toBeGreaterThanOrEqual(403);
  expect(refused.status).toBeLessThanOrEqual(404);
  await guestContext.close();
});
