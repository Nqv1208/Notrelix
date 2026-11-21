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

test("a block create the producer refuses is not stored, the person is told, and the typed line stays on the page", async ({
  page,
}) => {
  const stamp = Date.now();
  const line = `Refused line ${stamp}`;
  await signIn(page, realUsers.workspaceOwner);
  const workspace = await findWorkspace(page, "Workspace 3");
  const created = await apiWrite<string>(
    page,
    "POST",
    `/workspaces/${workspace.id}/pages`,
    { title: `Refused ${stamp}` },
  );
  expect(created.status).toBe(201);
  const docId = created.data as string;

  await page.goto(`/workspaces/${workspace.id}/docs/${docId}`);
  const editor = page.getByRole("textbox", {
    name: /Type \/ to insert a block/,
  });
  await expect(editor).toBeVisible();

  // The page goes to the trash from another tab; the next write is refused.
  const archived = await apiWrite(page, "POST", `/pages/${docId}/archive`, {});
  expect(archived.status).toBeLessThan(300);

  const refused = page.waitForResponse(
    (response) =>
      response.request().method() === "POST" &&
      /\/pages\/[^/]+\/blocks\/batch$/.test(response.url()),
  );
  await editor.click();
  await page.keyboard.type(line);
  await page.locator("article h1").click();
  expect((await refused).status()).toBeGreaterThanOrEqual(400);

  // The person is told: the page turns read-only and hands the unsaved text back.
  await expect(editor).toHaveAttribute("aria-readonly", "true");
  await expect(
    page.getByRole("blockquote", { name: "Your unsaved text" }),
  ).toHaveText(line);
  // The typed text is never taken back from the person.
  await expect(editor.getByText(line)).toBeVisible();

  // Nothing was stored.
  const restored = await apiWrite(page, "POST", `/pages/${docId}/restore`, {});
  expect(restored.status).toBeLessThan(300);
  const { items } = await apiRead<{ items: unknown[] }>(
    page,
    `/pages/${docId}/blocks`,
  );
  expect(items).toHaveLength(0);
});
