import { expect, test } from "@playwright/test";
import {
  apiWrite,
  findWorkspace,
  realUsers,
  signIn,
} from "./support/real-session";

test.use({ viewport: { width: 1440, height: 900 } });
test.setTimeout(180_000);

const apiUrl = process.env.VITE_API_URL ?? "http://127.0.0.1:58000/api/v1";

test("an owner writes a document, renames it, reloads it and shares it with a member; a guest sees it only once shared", async ({
  browser,
  page,
}) => {
  const stamp = Date.now();
  const title = `Runbook ${stamp}`;
  const renamed = `Runbook renamed ${stamp}`;
  const line = `Rollback needs two people ${stamp}`;

  await signIn(page, realUsers.workspaceOwner);
  const workspace = await findWorkspace(page, "Workspace 3");

  // The document is created through the same producer write the shell's create menu uses.
  const created = await apiWrite<string>(
    page,
    "POST",
    `/workspaces/${workspace.id}/pages`,
    { title },
  );
  expect(created.status).toBe(201);
  const docId = created.data as string;

  await page.goto(`/workspaces/${workspace.id}/docs/${docId}`);
  await expect(page.locator("article h1")).toHaveText(title);

  // Write a line: the editor saves it as a block of the page (one batch).
  const saved = page.waitForResponse(
    (response) =>
      response.request().method() === "POST" &&
      /\/pages\/[^/]+\/blocks\/batch$/.test(response.url()),
  );
  await page
    .getByRole("textbox", { name: /Type \/ to insert a block/ })
    .click();
  await page.keyboard.type(line);
  await page.locator("article h1").click();
  expect((await saved).status()).toBeLessThan(300);

  // Rename the document.
  const renameSent = page.waitForResponse(
    (response) =>
      response.request().method() === "PATCH" &&
      /\/pages\/[^/]+$/.test(response.url()),
  );
  const heading = page.locator("article h1");
  await heading.click();
  await page.keyboard.press("ControlOrMeta+a");
  await page.keyboard.type(renamed);
  await page.getByRole("textbox").first().click();
  expect((await renameSent).status()).toBeLessThan(300);

  // After a reload the title and the line are still there.
  await page.reload();
  await expect(page.locator("article h1")).toHaveText(renamed);
  await expect(page.getByText(line)).toBeVisible();

  // A guest of the workspace cannot open the unshared document.
  const guestContext = await browser.newContext();
  const guest = await guestContext.newPage();
  await signIn(guest, realUsers.workspaceGuest);
  const guestWorkspace = await findWorkspace(guest, "Workspace 3");
  const before = await guest.request.get(`${apiUrl}/pages/${docId}`);
  expect(before.status()).toBe(404);
  await guest.goto(`/workspaces/${guestWorkspace.id}/docs/${docId}`);
  await expect(guest.getByText(line)).toHaveCount(0);

  // The owner shares it with the guest.
  await page
    .getByRole("button", { name: /share|chia sẻ/i })
    .first()
    .click();
  const dialog = page.getByRole("dialog");
  await expect(dialog).toBeVisible();
  const add = dialog.getByRole("combobox");
  await expect(add).toBeEnabled();
  await add.fill("User 6");
  const granted = page.waitForResponse(
    (response) =>
      response.request().method() === "POST" &&
      /\/resources\/documents\.page\/[^/]+\/permissions$/.test(response.url()),
  );
  await dialog.getByRole("option", { name: /User 6/ }).click();
  await dialog.getByRole("button", { name: /^(invite|mời)$/i }).click();
  expect((await granted).status()).toBe(201);

  // Now the guest reads it, and the line with it.
  await guest.reload();
  await expect(guest.getByText(line)).toBeVisible({ timeout: 15_000 });
  await guestContext.close();
});
