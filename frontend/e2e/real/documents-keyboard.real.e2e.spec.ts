import { expect, test } from "@playwright/test";
import {
  apiRead,
  apiWrite,
  findWorkspace,
  realUsers,
  signIn,
} from "./support/real-session";

test.use({ viewport: { width: 1440, height: 900 } });
test.setTimeout(180_000);

type BlockRow = {
  id: string;
  position: string;
  content: { runs: { text: string }[] };
};

const text = (value: string) => ({ v: 1, runs: [{ text: value, marks: [] }] });

test("an editor clicks a block and moves it down and up with Alt+Shift+Arrow; the place is announced and stored", async ({
  page,
}) => {
  const stamp = Date.now();
  await signIn(page, realUsers.workspaceOwner);
  const workspace = await findWorkspace(page, "Workspace 3");
  const created = await apiWrite<string>(
    page,
    "POST",
    `/workspaces/${workspace.id}/pages`,
    { title: `Keyboard ${stamp}` },
  );
  expect(created.status).toBe(201);
  const docId = created.data as string;
  const lines = ["alpha", "bravo", "charlie"].map((word) => `${word} ${stamp}`);
  for (const line of lines) {
    const block = await apiWrite(page, "POST", `/pages/${docId}/blocks`, {
      type: "Text",
      content: text(line),
    });
    expect(block.status).toBe(201);
  }
  const stored = async () =>
    (await apiRead<{ items: BlockRow[] }>(page, `/pages/${docId}/blocks`)).items
      .sort((a, b) => (a.position < b.position ? -1 : 1))
      .map((row) => row.content.runs.map((run) => run.text).join(""));

  await page.goto(`/workspaces/${workspace.id}/docs/${docId}`);
  const editor = page.getByRole("textbox", {
    name: /Type \/ to insert a block/,
  });
  const shown = () => editor.locator("> *").allTextContents();
  await expect.poll(shown).toEqual(lines);

  // The key follows the click at once: no wait between them.
  await editor.getByText(lines[0], { exact: true }).click();
  await page.keyboard.press("Alt+Shift+ArrowDown");
  await expect.poll(shown).toEqual([lines[1], lines[0], lines[2]]);
  await expect(page.getByRole("status").filter({ hasText: /2/ })).toBeVisible();
  await expect.poll(stored).toEqual([lines[1], lines[0], lines[2]]);

  // The caret stayed in the moved block, so the next key moves the same block.
  await page.keyboard.press("Alt+Shift+ArrowDown");
  await expect.poll(shown).toEqual([lines[1], lines[2], lines[0]]);
  await expect.poll(stored).toEqual([lines[1], lines[2], lines[0]]);

  await editor.getByText(lines[0], { exact: true }).click();
  await page.keyboard.press("Alt+Shift+ArrowUp");
  await expect.poll(shown).toEqual([lines[1], lines[0], lines[2]]);
  await expect.poll(stored).toEqual([lines[1], lines[0], lines[2]]);

  // A reload keeps the order.
  await page.reload();
  await expect.poll(shown).toEqual([lines[1], lines[0], lines[2]]);
});
