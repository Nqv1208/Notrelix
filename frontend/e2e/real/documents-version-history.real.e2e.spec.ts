import { expect, test, type Page } from "@playwright/test";
import {
  apiRead,
  apiWrite,
  findWorkspace,
  realUsers,
  signIn,
} from "./support/real-session";

const apiUrl = process.env.VITE_API_URL ?? "http://127.0.0.1:58000/api/v1";

test.use({ viewport: { width: 1440, height: 900 } });
test.setTimeout(240_000);

type BlockRow = {
  id: string;
  version: number;
  content: { runs: Array<{ text: string }> };
};
type VersionRow = {
  id: string;
  versionNumber: number;
  kind: string;
  name: string | null;
};

const text = (value: string) => ({ v: 1, runs: [{ text: value, marks: [] }] });
const words = (block: BlockRow) =>
  block.content.runs.map((run) => run.text).join("");

async function blocks(page: Page, pageId: string) {
  return (await apiRead<{ items: BlockRow[] }>(page, `/pages/${pageId}/blocks`))
    .items;
}

async function openHistory(page: Page) {
  await page.getByRole("menuitem", { name: "File", exact: true }).click();
  await page.getByRole("menuitem", { name: /Version history/ }).click();
  await expect(
    page.getByRole("complementary", { name: "Version history" }),
  ).toBeVisible();
}

test("a named version is saved, listed, viewed read-only and restored: the page returns to what was saved and the history shows the restore", async ({
  browser,
  page,
}) => {
  const stamp = Date.now();
  const saved = `Saved wording ${stamp}`;
  const changed = `Changed wording ${stamp}`;
  const name = `Before launch ${stamp}`;

  await signIn(page, realUsers.workspaceOwner);
  const workspace = await findWorkspace(page, "Workspace 3");
  const created = await apiWrite<string>(
    page,
    "POST",
    `/workspaces/${workspace.id}/pages`,
    { title: `Versions ${stamp}` },
  );
  expect(created.status).toBe(201);
  const doc = created.data as string;
  const block = await apiWrite<BlockRow>(page, "POST", `/pages/${doc}/blocks`, {
    type: "Text",
    content: text(saved),
  });
  expect(block.status).toBe(201);

  await page.goto(`/workspaces/${workspace.id}/docs/${doc}`);
  await expect(page.getByText(saved)).toBeVisible();

  // Save the page as a named version: an empty name is refused in the dialog and nothing is sent.
  await openHistory(page);
  await page.getByRole("button", { name: "Save named version" }).click();
  const dialog = page.getByRole("dialog");
  await dialog.getByRole("button", { name: "Save", exact: true }).click();
  await expect(dialog.getByRole("alert")).toHaveText(
    "Give this version a name.",
  );
  await dialog.getByRole("textbox", { name: "Version name" }).fill(name);
  const posted = page.waitForResponse(
    (response) =>
      response.request().method() === "POST" &&
      /\/pages\/[^/]+\/versions$/.test(response.url()),
  );
  await dialog.getByRole("button", { name: "Save", exact: true }).click();
  expect((await posted).status()).toBe(201);
  await expect(dialog).toBeHidden();
  const panel = page.getByRole("complementary", { name: "Version history" });
  await expect(panel.getByText(name)).toBeVisible();

  // The page changes after it; the named entry still holds what was saved.
  const read = (await blocks(page, doc))[0]!;
  const patched = await apiWrite(
    page,
    "PATCH",
    `/pages/${doc}/blocks/${read.id}`,
    {
      expectedVersion: read.version,
      content: text(changed),
    },
  );
  expect(patched.status).toBeLessThan(300);
  await page.reload();
  await expect(page.getByText(changed)).toBeVisible();

  // Viewing the version shows the saved wording, with no way to edit it.
  await openHistory(page);
  await panel.getByRole("button", { name: new RegExp(name) }).click();
  await expect(page.getByText("This page is view only.")).toBeVisible();
  await expect(page.getByText(saved)).toBeVisible();
  await expect(page.getByText(changed)).toHaveCount(0);
  await expect(
    page.locator('article [contenteditable]:not([contenteditable="false"])'),
  ).toHaveCount(0);

  // The producer refuses an empty name too (the dialog is not the only way in), and adds no entry.
  const blank = await apiWrite(page, "POST", `/pages/${doc}/versions`, {
    name: "  ",
  });
  expect(blank.status).toBe(400);

  // A restore that names stale blocks is refused with 412 and changes nothing.
  const listed = await apiRead<{ items: VersionRow[] }>(
    page,
    `/pages/${doc}/versions`,
  );
  const target = listed.items.find((entry) => entry.name === name)!;
  const stale = await apiWrite(
    page,
    "POST",
    `/pages/${doc}/versions/${target.id}/restore`,
    { currentBlocks: [{ blockId: read.id, version: read.version }] },
  );
  expect(stale.status).toBe(412);
  expect(words((await blocks(page, doc))[0]!)).toBe(changed);

  // Restore it through the page: confirm, and the page holds the saved wording again.
  await page.getByRole("button", { name: "Restore this version" }).click();
  await expect(
    page.getByRole("heading", {
      name: `Restore version ${target.versionNumber}?`,
    }),
  ).toBeVisible();
  // A write after the spec's own API calls may meet one stale CSRF token first; the client recovers once, so the answer that counts is the one that is not that refusal.
  const restored = page.waitForResponse(
    (response) =>
      response.request().method() === "POST" &&
      /\/versions\/[^/]+\/restore$/.test(response.url()) &&
      response.status() !== 403,
  );
  await page
    .getByRole("dialog")
    .getByRole("button", { name: "Restore", exact: true })
    .click();
  expect((await restored).status()).toBeLessThan(300);
  await expect(page.getByText(saved)).toBeVisible();
  expect(words((await blocks(page, doc))[0]!)).toBe(saved);

  // The history is not rewritten: what the page held before is kept and the restore is an entry.
  await openHistory(page);
  await expect(
    panel.getByText(`Before restoring version ${target.versionNumber}`),
  ).toBeVisible();
  await expect(
    panel.getByText(`Restored from version ${target.versionNumber}`),
  ).toBeVisible();
  await expect(panel.getByText(name)).toBeVisible();

  // An entry that is not on the page is not found, and a person outside the page gets the same answer for the list and for an entry.
  const unknown = await page.request.get(
    `${apiUrl}/pages/${doc}/versions/${crypto.randomUUID()}`,
  );
  expect(unknown.status()).toBe(404);
  const outsider = await browser.newContext();
  const guest = await outsider.newPage();
  await signIn(guest, realUsers.workspaceGuest);
  const list = await guest.request.get(`${apiUrl}/pages/${doc}/versions`);
  const entry = await guest.request.get(
    `${apiUrl}/pages/${doc}/versions/${target.id}`,
  );
  expect([list.status(), entry.status()]).toEqual([404, 404]);
  await outsider.close();
});
