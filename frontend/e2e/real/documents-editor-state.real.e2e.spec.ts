import AxeBuilder from "@axe-core/playwright";
import { expect, test, type Page } from "@playwright/test";
import {
  apiRead,
  apiWrite,
  findWorkspace,
  realUsers,
  signIn,
} from "./support/real-session";

test.use({ viewport: { width: 1440, height: 900 } });
test.setTimeout(240_000);

type BlockRow = {
  id: string;
  version: number;
  position: string;
  content?: { runs?: { text: string }[] };
};

const text = (value: string) => ({ v: 1, runs: [{ text: value, marks: [] }] });
const isBatch = (url: string) => /\/pages\/[^/]+\/blocks\/batch$/.test(url);

async function openDoc(page: Page, title: string, lines: string[]) {
  await signIn(page, realUsers.workspaceOwner);
  const workspace = await findWorkspace(page, "Workspace 3");
  const created = await apiWrite<string>(
    page,
    "POST",
    `/workspaces/${workspace.id}/pages`,
    { title },
  );
  expect(created.status).toBe(201);
  const pageId = created.data as string;
  for (const line of lines) {
    const block = await apiWrite(page, "POST", `/pages/${pageId}/blocks`, {
      type: "Text",
      content: text(line),
    });
    expect(block.status).toBe(201);
  }
  await page.goto(`/workspaces/${workspace.id}/docs/${pageId}`);
  await expect(page.locator("article h1")).toHaveText(title);
  return { pageId, workspaceId: workspace.id };
}

const stored = async (page: Page, pageId: string) =>
  (
    await apiRead<{ items: BlockRow[] }>(page, `/pages/${pageId}/blocks`)
  ).items.sort((a, b) => (a.position < b.position ? -1 : 1));

const storedText = async (page: Page, pageId: string) =>
  (await stored(page, pageId))
    .map((b) => (b.content?.runs ?? []).map((r) => r.text).join(""))
    .join("\n");

test("autosave says Saved, a refused save says Not saved and Retry sends the same text, offline says so, locks import and sends on reconnect", async ({
  page,
  context,
}) => {
  const stamp = Date.now();
  const { pageId } = await openDoc(page, `Autosave ${stamp}`, []);
  const editor = page.getByRole("textbox", {
    name: /Type \/ to insert a block/,
  });
  const leave = () => page.locator("article h1").click();

  // Saved: typing then leaving the block stores it.
  const first = page.waitForResponse(
    (r) => r.request().method() === "POST" && isBatch(r.url()),
  );
  await editor.click();
  await page.keyboard.type(`online ${stamp}`);
  await leave();
  expect((await first).status()).toBeLessThan(300);
  await expect(page.getByText("Saved", { exact: true })).toBeVisible();

  // A refused write keeps the text, says Not saved and offers Retry.
  const refuse = (url: URL) => isBatch(url.toString());
  await page.route(refuse, (route) => route.abort("failed"));
  await page.getByText(`online ${stamp}`).click();
  await page.keyboard.press("End");
  await page.keyboard.type(` refused ${stamp}`);
  await leave();
  await expect(page.getByText(/Not saved/)).toBeVisible();
  await expect(page.getByText(`refused ${stamp}`)).toBeVisible();
  await page.unroute(refuse);
  await page.getByRole("button", { name: "Retry" }).click();
  await expect(page.getByText("Saved", { exact: true })).toBeVisible();
  expect(await storedText(page, pageId)).toContain(`refused ${stamp}`);

  // Offline: the bar says so, import is off, the text stays; reconnecting sends it.
  await context.setOffline(true);
  await expect(page.getByText("Offline, changes not saved yet")).toBeVisible();
  await page.getByRole("menuitem", { name: "File" }).click();
  await expect(page.getByRole("menuitem", { name: /Import/ })).toHaveAttribute(
    "aria-disabled",
    "true",
  );
  await page.keyboard.press("Escape");
  await page.getByText(`online ${stamp}`).click();
  await page.keyboard.press("End");
  await page.keyboard.type(` offline ${stamp}`);
  await context.setOffline(false);
  await expect(page.getByText("Saved", { exact: true })).toBeVisible();
  expect(await storedText(page, pageId)).toContain(`offline ${stamp}`);
});

test("a block changed elsewhere is replaced by the stored text and the person's text stays in the notice", async ({
  page,
}) => {
  const stamp = Date.now();
  const { pageId } = await openDoc(page, `Conflict ${stamp}`, [
    `original ${stamp}`,
  ]);
  const [block] = await stored(page, pageId);

  // Another session changes the block; this editor still holds the first version.
  const other = await apiWrite(
    page,
    "PATCH",
    `/pages/${pageId}/blocks/${block.id}`,
    { expectedVersion: block.version, content: text(`theirs ${stamp}`) },
  );
  expect(other.status).toBe(200);

  await page.getByText(`original ${stamp}`).click();
  await page.keyboard.press("End");
  await page.keyboard.type(` mine ${stamp}`);
  await page.locator("article h1").click();

  const notice = page.getByRole("region", { name: "Your unsaved edit" });
  await expect(notice).toContainText("Someone else just changed this block");
  await expect(notice).toContainText(`mine ${stamp}`);
  await expect(page.getByText(`theirs ${stamp}`)).toBeVisible();
  const [after] = await stored(page, pageId);
  expect(after.version).toBe(block.version + 1);
});

test("find and replace changes three matches in one save and says when nothing matches", async ({
  page,
}) => {
  const stamp = Date.now();
  const { pageId } = await openDoc(page, `Find ${stamp}`, [
    `cat one ${stamp}`,
    `two cat ${stamp}`,
    `Cat three ${stamp}`,
  ]);

  await page.getByText(`cat one ${stamp}`).click();
  await page.keyboard.press("ControlOrMeta+h");
  const search = page.getByRole("search", { name: "Find and replace" });
  await search.getByRole("textbox", { name: "Find", exact: true }).fill("cat");
  await expect(search.getByRole("status")).toContainText("3");
  await search.getByRole("textbox", { name: "Replace with" }).fill("dog");
  let batches = 0;
  page.on("request", (r) => {
    if (r.method() === "POST" && isBatch(r.url())) batches += 1;
  });
  const sent = page.waitForResponse(
    (r) => r.request().method() === "POST" && isBatch(r.url()),
  );
  await search.getByRole("button", { name: "Replace all" }).click();
  expect((await sent).status()).toBeLessThan(300);
  await expect(page.getByText(`dog one ${stamp}`)).toBeVisible();
  expect(batches).toBe(1);
  await expect
    .poll(() => storedText(page, pageId))
    .toBe(`dog one ${stamp}\ntwo dog ${stamp}\ndog three ${stamp}`);

  // The panel floats over the text: Close hides it. No matches shows the empty state.
  await search.getByRole("button", { name: /^Close/ }).click();
  await expect(search).toHaveCount(0);
  await page.getByText(`two dog ${stamp}`).click();
  await page.keyboard.press("ControlOrMeta+f");
  await search.getByRole("textbox", { name: "Find", exact: true }).fill("zzzz");
  await expect(search.getByText("No results")).toBeVisible();
  await page.keyboard.press("Escape");
});

async function noSeriousAxe(page: Page) {
  const axe = await new AxeBuilder({ page }).analyze();
  expect(
    axe.violations
      .filter((v) => ["serious", "critical"].includes(v.impact ?? ""))
      .map((v) => `${v.id}: ${v.nodes[0]?.html.slice(0, 120)}`),
  ).toEqual([]);
}

test("the open editor with its menus, toolbar and side panel has no serious accessibility violation", async ({
  page,
}) => {
  const stamp = Date.now();
  await openDoc(page, `Axe ${stamp}`, [`one ${stamp}`, `two ${stamp}`]);
  await noSeriousAxe(page);
});
