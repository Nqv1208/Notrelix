import { expect, test, type Page } from "@playwright/test";
import {
  apiRead,
  apiWrite,
  findWorkspace,
  realUsers,
  signIn,
} from "./support/real-session";

const apiUrl = process.env.VITE_API_URL ?? "http://127.0.0.1:58000/api/v1";

interface Pending {
  readonly id: string;
}
interface PendingPage {
  readonly items?: readonly Pending[];
}

async function status(page: Page, path: string): Promise<number> {
  return (await page.request.get(`${apiUrl}${path}`)).status();
}

async function listedIds(page: Page): Promise<readonly string[]> {
  const rows = await apiRead<readonly { id: string }[]>(page, "/workspaces");
  return rows.map((row) => row.id);
}

test.setTimeout(180_000);

test("an Owner deletes a workspace, a member loses it at once, and the Owner restores it from Recently deleted", async ({
  browser,
  page,
}) => {
  const name = `Delete me ${Date.now()}`;

  // Scene: the seeded Account owner makes a workspace (its Owner) and a member joins it.
  await signIn(page, realUsers.admin);
  const { accountId } = await apiRead<{ accountId: string }>(
    page,
    "/auth/bootstrap",
  );
  const created = await apiWrite<string>(
    page,
    "POST",
    `/accounts/${accountId}/workspaces`,
    {
      name,
      slug: `delete-me-${Date.now()}`,
      size: "1-10",
      language: "en",
    },
  );
  expect(created.status).toBe(201);
  const workspaceId = created.data as string;
  expect(
    (
      await apiWrite(page, "POST", `/workspaces/${workspaceId}/members`, {
        email: realUsers.workspaceAdmin.email,
        role: "Member",
      })
    ).status,
  ).toBe(201);

  const memberContext = await browser.newContext();
  const memberPage = await memberContext.newPage();
  await signIn(memberPage, realUsers.workspaceAdmin);
  const waiting = await apiRead<PendingPage | readonly Pending[]>(
    memberPage,
    "/invitations/pending",
  );
  const invitations = Array.isArray(waiting)
    ? waiting
    : ((waiting as PendingPage).items ?? []);
  expect(invitations.length).toBeGreaterThan(0);
  expect(
    (
      await apiWrite(
        memberPage,
        "POST",
        `/invitations/${invitations[0]!.id}/accept`,
      )
    ).status,
  ).toBeLessThan(300);
  expect(await status(memberPage, `/workspaces/${workspaceId}`)).toBe(200);
  expect(await listedIds(memberPage)).toContain(workspaceId);

  // The Owner deletes it from the danger zone, confirming by typing its name.
  await page.goto(`/workspaces/${workspaceId}/settings`);
  await page.getByRole("button", { name: "Delete workspace" }).click();
  const dialog = page.getByRole("alertdialog");
  const confirm = dialog.getByRole("button", { name: "Delete workspace" });
  await expect(confirm).toBeDisabled();
  await dialog.getByLabel("Workspace name").fill(name.slice(0, -1));
  await expect(confirm).toBeDisabled();
  await dialog.getByLabel("Workspace name").fill(name);
  await expect(confirm).toBeEnabled();
  const deleted = page.waitForResponse(
    (response) =>
      response.request().method() === "DELETE" &&
      response.url().includes(`/workspaces/${workspaceId}`),
  );
  await confirm.click();
  expect((await deleted).status()).toBeLessThan(300);
  await expect(page).not.toHaveURL(new RegExp(workspaceId));

  // Gone at once for everyone: the answer is the same NotFound for the Owner and the member, in the lists too.
  expect(await status(page, `/workspaces/${workspaceId}`)).toBe(404);
  expect(await status(memberPage, `/workspaces/${workspaceId}`)).toBe(404);
  expect(await status(memberPage, `/workspaces/${workspaceId}/boards`)).toBe(
    404,
  );
  expect(await listedIds(page)).not.toContain(workspaceId);
  expect(await listedIds(memberPage)).not.toContain(workspaceId);
  // A member cannot restore it, and cannot tell it exists.
  expect(
    (
      await apiWrite(memberPage, "POST", `/workspaces/${workspaceId}/restore`, {
        expectedVersion: 1,
      })
    ).status,
  ).toBe(404);

  // Only the Owner sees it in Recently deleted, with the days left, and restores it.
  const home = await findWorkspace(page, "Workspace 2");
  await page.goto(`/workspaces/${home.id}/account/workspaces`);
  await expect(
    page.getByRole("heading", { level: 2, name: "Recently deleted" }),
  ).toBeVisible();
  const row = page.getByRole("region", { name }).first();
  await expect(row).toContainText("30 days left");
  const restored = page.waitForResponse(
    (response) =>
      response.request().method() === "POST" &&
      response.url().endsWith(`/workspaces/${workspaceId}/restore`),
  );
  await page
    .getByRole("button", { name: `Restore the workspace ${name}` })
    .click();
  expect((await restored).status()).toBeLessThan(300);

  // Everything is back as it was, for the member too.
  await expect(
    page.getByRole("heading", { level: 2, name: "Recently deleted" }),
  ).toHaveCount(0);
  expect(await status(page, `/workspaces/${workspaceId}`)).toBe(200);
  expect(await status(memberPage, `/workspaces/${workspaceId}`)).toBe(200);
  expect(await listedIds(memberPage)).toContain(workspaceId);
  expect(
    (
      await memberPage.request.get(
        `${apiUrl}/workspaces/${workspaceId}/members/me`,
      )
    ).ok(),
  ).toBe(true);
  await memberContext.close();
});
